using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using Loyc;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Syntax;
using Loyc.Threading;

namespace LeMP
{
	using S = CodeSymbols;

	/// <summary>Holds the transient state of the macro processor. Since one
	/// <see cref="MacroProcessor"/> object can process multiple files in 
	/// parallel, we need an inner class to hold the state of each individual 
	/// transformation task.</summary>
	/// <remarks>
	/// This is a flowchart showing how MacroProcessorTask applies macros.
	/// <pre>
	///    ProcessRoot
	///        |      
	///        v      
	///     Process   // initializes _s, _ancestorStack, root scope, etc.
	///        |      
	///        v      
	/// +->ApplyMacrosToList // uses _s implicitly                     
	/// |      |                                                       
	/// |      |  +----------------------------------------------------------+
	/// |      |  |                                                          |
	/// |      v  v                                                          |
	/// |  ApplyMacros----1----->GetApplicableMacros                         |
	/// |      |    |                                                        |
	/// |      |*OR*|                                                        |
	/// |      |    +-----2----->ApplyMacrosFound                            |
	/// |      |     (macro(s)          |                                    |
	/// |      |      found)            v                                    |
	/// |      |                 ApplyMacrosFound2                           |
	/// |      |                         |                                   |
	/// |      |                         v                                   |
	/// |      |                 ApplyMacrosFound3---1---->invoke macro(s)   |
	/// |      |                         |    |  |         (SimpleMacro fn)  |
	/// |      |                         3    3  +---2---->PrintMessages     |
	/// |      |                         |    |                              |
	/// |      |                         |*OR*|  Process same node again     |
	/// |      |                  Process|    +------------------------------+
	/// |      |(no macros       Children|    (if a macro was applied here)
	/// |      | applicable)             |                                 
	/// |      +--------2------->ApplyMacrosToChildrenOf                   
	/// |                                +                                 
	/// |  Attrs and Args of child node  |                                 
	/// +--------------------------------+                                 
	/// </pre>
	/// Legend:
	/// - Each arrow represents a function call; minor helper functions are left out
	/// - If a function calls multiple other functions, --1--, --2-- show the order of calls
	/// - The starting point is at the top and time flows downward; an arrow that 
	///   flows back upward represents recursion.
	/// - Edges are labeled to indicate what parameters are sent (or under what 
	///   condition this path is taken)
	/// <para/>
	/// Meanwhile, a stack of <see cref="Scope"/> objects keep track of information
	/// local to each pair of braces (Scope also serves as an implementation of 
	/// <see cref="IMacroContext"/>). <see cref="CurNodeState"/> is an object 
	/// that holds state specifically regarding the node currently being processed; 
	/// usually the object called <c>_s</c> is re-used for all the different nodes, 
	/// but sometimes a macro will ask for its child nodes to be processed, in 
	/// which case a second <see cref="CurNodeState"/> must be introduced to
	/// avoid destroying the original state. Some of the fields of <see cref=
	/// "CurNodeState"/> would have just been local variables, if not for the 
	/// fact that <see cref="IMacroContext"/> allows a currently-running macro to 
	/// view or even modify some of this information.
	/// </remarks>
	internal class MacroProcessorTask
	{
		static readonly Symbol _importMacros = GSymbol.Get("#importMacros");
		static readonly Symbol _unimportMacros = GSymbol.Get("#unimportMacros");
		static readonly Symbol _noLexicalMacros = GSymbol.Get("#noLexicalMacros");
			
		public MacroProcessorTask(MacroProcessor parent)
		{
			_macros = parent.Macros.Clone();
			// Braces must be handled specially by ApplyMacros itself, but we need 
			// a macro method in order to get the special treatment, because as an 
			// optimization, we ignore all symbols that are not in the macro table.
			MacroProcessor.AddMacro(_macros, new MacroInfo(null, S.Braces,         OnBraces, MacroMode.Normal | MacroMode.Passive));
			MacroProcessor.AddMacro(_macros, new MacroInfo(null, S.Import,         OnImport, MacroMode.Normal | MacroMode.Passive));
			MacroProcessor.AddMacro(_macros, new MacroInfo(null, _importMacros,    OnImportMacros, MacroMode.Normal));
			MacroProcessor.AddMacro(_macros, new MacroInfo(null, _unimportMacros,  OnUnimportMacros, MacroMode.Normal | MacroMode.Passive));
			MacroProcessor.AddMacro(_macros, new MacroInfo(null, _noLexicalMacros, NoLexicalMacros, MacroMode.NoReprocessing));
			_parent = parent;
		}

		#region Fields

		MacroProcessor _parent;
		IMessageSink _sink { get { return _parent.Sink; } }
		int MaxExpansions { get { return _parent.MaxExpansions; } }
		MMap<Symbol, List<MacroInfo>> _macros;
		
		// Statistics
		public int MacrosInvoked { get; set; }
		public int NodesReplaced { get; set; }

		// Ancestors of current node, and the current node itself as the final item
		DList<LNode> _ancestorStack;
		CurNodeState _s;
		int _reentrancyCounter = 0;
		
		// null entries inherit parent scope (null means "nothing new in this scope")
		InternalList<Scope> _scopes = InternalList<Scope>.Empty; 
		Scope _curScope; // current scope, or parent scope if inherited
		Scope AutoInitScope()
		{
			if (_scopes.Last == null)
				_curScope = _scopes.Last = _curScope.Clone();
			return _curScope;
		}

		#endregion

		#region Public entry points: ProcessFileWithThreadAbort(), ProcessFile(), ProcessRoot()

		public RVList<LNode> ProcessFileWithThreadAbort(InputOutput io, Action<InputOutput> onProcessed, TimeSpan timeout)
		{
			if (timeout == TimeSpan.Zero || timeout == TimeSpan.MaxValue)
				return ProcessFile(io, onProcessed);
			else {
				Exception ex = null;
				var thread = new ThreadEx(() =>
				{
					try { ProcessFile(io, null); } 
					catch (Exception e) { ex = e; ex.PreserveStackTrace(); }
				});
				thread.Start();
				if (thread.Join(timeout)) {
					onProcessed(io);
				} else {
					io.Output = new RVList<LNode>(F.Id("processing_thread_timed_out"));
					thread.Abort();
					thread.Join(timeout);
				}
				if (ex != null)
					throw ex;
				return io.Output;
			}
		}
		public RVList<LNode> ProcessFile(InputOutput io, Action<InputOutput> onProcessed)
		{
			using (ParsingService.PushCurrent(io.InputLang ?? ParsingService.Current)) {
				var input = ParsingService.Current.Parse(io.Text, io.FileName, _sink);
				var inputRV = new RVList<LNode>(input);

				io.Output = ProcessRoot(inputRV);
				if (onProcessed != null)
					onProcessed(io);
				return io.Output;
			}
		}

		/// <summary>Top-level macro applicator.</summary>
		public RVList<LNode> ProcessRoot(RVList<LNode> stmts)
		{
			Process(ref stmts, null, true, true, false);
			return stmts;
		}
		public LNode ProcessRoot(LNode stmt)
		{
			RVList<LNode> empty = new RVList<LNode>();
			return Process(ref empty, null, true, true, false);
		}

		LNode Process(ref RVList<LNode> list, LNode single, bool asRoot, bool resetOpenNamespaces, bool areAttributesOrIsTarget)
		{
			if (single == null && list.Count == 0)
				return null; // no-op requested
			var oldS = _s;
			var oldAncestors = _ancestorStack;
			var oldMP = MacroProcessor._current;
			MacroProcessor._current = _parent;
			bool newScope = false;
			try {
				bool reentrant = _reentrancyCounter++ != 0;
				if (!reentrant)
					asRoot = true;
				Debug.Assert(reentrant || _scopes.Count == 0);
				Debug.Assert(reentrant || _ancestorStack == null);
				
				if (asRoot)
					_ancestorStack = new DList<LNode>();
				_s = new CurNodeState();
				if (asRoot || resetOpenNamespaces) {
					var namespaces = !reentrant || resetOpenNamespaces ? _parent.PreOpenedNamespaces.Clone() : _curScope.OpenNamespaces.Clone();
					var properties = asRoot ? new MMap<object,object>() : _curScope.ScopedProperties;
					newScope = true;
					_curScope = new Scope(namespaces, properties, this, true);
					_scopes.Add(_curScope);
				}
				int maxExpansions = asRoot ? MaxExpansions : _s.MaxExpansions - 1;
				if (single != null) {
					return ApplyMacros(single, maxExpansions, areAttributesOrIsTarget);
				} else {
					int oldStackCount = _ancestorStack.Count;
					LNode splice = null;
					if (asRoot) {
						splice = list.AsLNode(S.Splice);
						_ancestorStack.PushLast(splice);
					}
					list = ApplyMacrosToList(list, maxExpansions, areAttributesOrIsTarget);
					_ancestorStack.PopLast();
					Debug.Assert(_ancestorStack.Count == oldStackCount);
					return splice;
				}
			} finally {
				_reentrancyCounter--;
				MacroProcessor._current = oldMP;
				_ancestorStack = oldAncestors;
				_s = oldS;
				if (newScope)
					PopScope();
			}
		}
	
		#endregion

		#region Scope (IMacroContext) definition

		class Scope : ICloneable<Scope>, IMacroContext
		{
			public Scope(MSet<Symbol> openNamespaces, MMap<object, object> scopedProperties, MacroProcessorTask task, bool isRoot = false)
				{ OpenNamespaces = openNamespaces; _scopedProperties = scopedProperties; _task = task; _propertiesCopied = _namespacesCopied = isRoot; }
			public MSet<Symbol> OpenNamespaces;
			MMap<object, object> _scopedProperties;
			MacroProcessorTask _task;
			bool _namespacesCopied;
			bool _propertiesCopied;
				
			public void BeforeImport()
			{
				if (!_namespacesCopied) {
					_namespacesCopied = true;
					OpenNamespaces = OpenNamespaces.Clone();
				}
			}
			public Scope Clone()
			{
				return new Scope(OpenNamespaces, _scopedProperties, _task);
			}

			#region IMacroContext Members

			IDictionary<object, object> IMacroContext.ScopedProperties { get { return ScopedProperties; } }
			public MMap<object, object> ScopedProperties
			{
				get { 
					if (!_propertiesCopied) {
						_propertiesCopied = true;
						_scopedProperties = _scopedProperties.Clone();
					}
					return _scopedProperties;
				}
			}

			public IReadOnlyList<LNode> Ancestors
			{
				get { return _task._ancestorStack; }
			}
			public LNode Parent
			{
				get { var st = _task._ancestorStack; return st[st.Count - 2, null]; }
			}

			public RVList<LNode> PreProcess(RVList<LNode> input, bool asRoot = false, bool resetOpenNamespaces = false, bool areAttributes = false)
			{
				_task.Process(ref input, null, asRoot, resetOpenNamespaces, areAttributes);
				return input;
			}
			public LNode PreProcess(LNode input, bool asRoot = false, bool resetOpenNamespaces = false, bool isTarget = false)
			{
				RVList<LNode> empty = new RVList<LNode>();
				return _task.Process(ref empty, input, asRoot, resetOpenNamespaces, isTarget);
			}
			
			public LNode PreProcessChildren()
			{
				return _task.ProcessChildrenOfCurrentNode();
			}

			public IMessageSink Sink { get { return _task._s.MessageHolder; } }
			public IListSource<LNode> RemainingNodes { get { return _task._s.RemainingNodes; } }
			public bool IsAttribute { get { return _task._s.IsAttribute; } }
			public bool IsTarget { get { return _task._s.IsTarget; } }
			public bool DropRemainingNodes { get { return _task._s.DropRemainingNodes; } set { _task._s.DropRemainingNodes = value; } }

			public void Write(Severity type, object context, string format)
			{
				Sink.Write(type, context ?? Ancestors.Last(), format);
			}
			public void Write(Severity type, object context, string format, object arg0, object arg1 = null)
			{
				Sink.Write(type, context ?? Ancestors.Last(), format, arg0, arg1);
			}
			public void Write(Severity type, object context, string format, params object[] args)
			{
				Sink.Write(type, context ?? Ancestors.Last(), format, args);
			}
			public bool IsEnabled(Severity type)
			{
				return Sink.IsEnabled(type);
			}

			#endregion
		}

		#endregion

		// State variables related to the current node or current macro invocation.
		class CurNodeState
		{
			public LNode Input;
			public bool IsAttribute, IsTarget;
			
			// Number of nested macro expansions that can be rooted at current  
			// node (the total number of expansions is often higher)
			public int MaxExpansions; 

			// Optimization: these lists are re-used on each call to ApplyMacros,
			// to avoid allocating unnecessary garbage.
			public List<MacroInfo> FoundMacros = new List<MacroInfo>();
			public MessageHolder MessageHolder = new MessageHolder();
			public InternalList<MacroResult> Results = InternalList<MacroResult>.Empty;

			// These three fields suport the IMacroContext.RemainingNodes property
			public RVList<LNode> CurrentNodeList;
			public int CurrentNodeIndex;
			private IListSource<LNode> _remainingNodes;
			public IListSource<LNode> RemainingNodes { 
				get { 
					if (_remainingNodes == null)
						_remainingNodes = CurrentNodeList.Slice(CurrentNodeIndex + 1); 
					return _remainingNodes; 
				}
			}

			// Cached result of applying macros to the children of the current node.
			public LNode Preprocessed;

			public bool DropRemainingNodes; // A macro can set this flag

			public void StartNextNode(LNode input, int maxExpansions, bool isTargetNode)
			{
				Input = input;
				IsTarget = isTargetNode;
				FoundMacros.Clear();
				_remainingNodes = null;
				Preprocessed = null;
				MaxExpansions = maxExpansions;
				Debug.Assert(!IsTarget || !IsAttribute);
				DropRemainingNodes = false;
			}
			public void StartNextListItem(RVList<LNode> currentNodeList, int index, bool isAttribute)
			{
				CurrentNodeList = currentNodeList;
				CurrentNodeIndex = index;
				IsAttribute = isAttribute;
			}
			public void StartApplyMacros()
			{
				MessageHolder.List.Clear();
				Results.Resize(0);
			}
		}

		#region Find macros by name: GetApplicableMacros

		public int GetApplicableMacros(ICollection<Symbol> openNamespaces, Symbol name, ICollection<MacroInfo> found)
		{
			List<MacroInfo> candidates;
			if (_macros.TryGetValue(name, out candidates)) {
				int count = 0;
				foreach (var info in candidates) {
					if (openNamespaces.Contains(info.Namespace) || info.Namespace == null) {
						count++;
						found.Add(info);
					}
				}
				return count;
			} else
				return 0;
		}
		public int GetApplicableMacros(Symbol @namespace, Symbol name, ICollection<MacroInfo> found)
		{
			List<MacroInfo> candidates;
			if (_macros.TryGetValue(name, out candidates)) {
				int count = 0;
				foreach (var info in candidates) {
					if (info.Namespace == @namespace) {
						count++;
						found.Add(info);
					}
				}
				return count;
			} else
				return 0;
		}

		#endregion

		#region Built-in commands
		// These aren't really macros, but they are installed like macros so that
		// no extra overhead is required to detect them.

		static readonly LNodeFactory F = new LNodeFactory(EmptySourceFile.Default);

		public LNode OnImportMacros(LNode node, IMessageSink sink)
		{
			OnImport(node, sink);
			return F.Call(S.Splice);
		}
		public LNode OnImport(LNode node, IMessageSink sink)
		{
			AutoInitScope().BeforeImport();
			foreach (var arg in node.Args)
				_curScope.OpenNamespaces.Add(NamespaceToSymbol(arg));
			return null;
		}
		public LNode OnUnimportMacros(LNode node, IMessageSink sink)
		{
			AutoInitScope().BeforeImport();
			foreach (var arg in node.Args) {
				var sym = NamespaceToSymbol(arg);
				if (!_curScope.OpenNamespaces.Remove(sym))
					sink.Write(Severity.Debug, arg, "Namespace not found to remove: {0}", sym);
			}
			return null;
		}
		public static LNode NoLexicalMacros(LNode node, IMessageSink sink)
		{
			if (!node.IsCall)
				return null;
			return node.WithTarget(S.Splice);
		}
		public LNode OnBraces(LNode node, IMessageSink sink)
		{
			// This no-op macro exists to ensure ApplyMacrosFound2() is called, 
			return null; // since that function is in charge of creating scopes.
		}

		//public LNode OnBraces(LNode node, IMessageSink sink)
		//{
		//	_scopes.Add(null);
		//	return null; // ApplyMacros will take care of popping the scope
		//}
		private void PopScope()
		{
			_scopes.Pop();
			bool fail = false;
			for (int i = _scopes.Count - 1; !fail; i--)
				if ((_curScope = _scopes.TryGet(i, out fail)) != null)
					break;
		}

		#endregion

		#region Lower-level processing: ApplyMacros, PrintMessages, etc.

		Symbol NamespaceToSymbol(LNode node)
		{
			return GSymbol.Get(node.Print(NodeStyle.Expression)); // quick & dirty
		}

		/// <summary>Recursively applies macros in scope to <c>input</c>.</summary>
		/// <param name="maxExpansions">Maximum number of opportunities given 
		/// to macros to transform a given subtree. The output of any macro is
		/// transformed again (as if by calling this method) with 
		/// <c>maxExpansions = maxExpansions - 1</c> to encourage the 
		/// expansion process to terminate eventually.</param>
		/// <returns>Returns a transformed tree or null if the macros did not 
		/// change the syntax tree at any level, paired with a flag that is
		/// true if the remainder of the nodes in the current list of nodes
		/// should be dropped.</returns>
		LNode ApplyMacros(LNode input, int maxExpansions, bool isTargetNode)
		{
			if (maxExpansions <= 0)
				return null;
			_s.StartNextNode(input, maxExpansions, isTargetNode);
			
			// Find macros...
			LNode target;
			if (input.HasSimpleHead()) {
				GetApplicableMacros(_curScope.OpenNamespaces, input.Name, _s.FoundMacros);
			} else if ((target = input.Target).Calls(S.Dot, 2) && target.Args[1].IsId) {
				Symbol name = target.Args[1].Name, @namespace = NamespaceToSymbol(target.Args[0]);
				GetApplicableMacros(@namespace, name, _s.FoundMacros);
			}

			_ancestorStack.PushLast(input);
			try {
				if (_s.FoundMacros.Count != 0)
					return ApplyMacrosFound(_s);
				else
					return ApplyMacrosToChildrenOf(input, maxExpansions);
			} finally {
				_ancestorStack.PopLast(1);
			}
		}

		private LNode ApplyMacrosFound(CurNodeState s)
		{
			var foundMacros = s.FoundMacros;
			// if any of the macros use a priority flag, group by priority.
			if (foundMacros.Count > 1) {
				var p = foundMacros[0].Mode & MacroMode.PriorityMask;
				for (int x = 1; x < foundMacros.Count; x++) {
					if ((foundMacros[x].Mode & MacroMode.PriorityMask) != p) {
						// need to make an independent list because _s.foundMacros may be cleared and re-used for descendant nodes
						foundMacros = new List<MacroInfo>(foundMacros);
						foundMacros.Sort();
						for (int i = 0, j; i < foundMacros.Count; i = j) {
							p = foundMacros[i].Mode & MacroMode.PriorityMask;
							for (j = i + 1; j < foundMacros.Count; j++)
								if ((foundMacros[j].Mode & MacroMode.PriorityMask) != p)
									break;
							var newNode = ApplyMacrosFound2(s, foundMacros.Slice(i, j - i));
							if (newNode != null)
								return newNode;
						}
						return null;
					}
				}
			}
			return ApplyMacrosFound2(s, foundMacros.Slice(0));
		}

		private LNode ApplyMacrosFound2(CurNodeState s, ListSlice<MacroInfo> foundMacros)
		{
			s.StartApplyMacros();

			if (!s.Input.Calls(S.Braces))
				return ApplyMacrosFound3(s, foundMacros);
			else {
				_scopes.Add(null);
				try {
					return ApplyMacrosFound3(s, foundMacros);
				} finally {
					PopScope();
				}
			}
		}
		private LNode ApplyMacrosFound3(CurNodeState s, ListSlice<MacroInfo> foundMacros)
		{
			LNode input = s.Input;
			Debug.Assert(s.Results.IsEmpty && s.MessageHolder.List.Count == 0);
			IList<MessageHolder.Message> messageList = s.MessageHolder.List;

			int accepted = 0, acceptedIndex = -1;
			for (int i = 0; i < foundMacros.Count; i++)
			{
				var macro = foundMacros[i];
				var macroInput = input;
				if ((macro.Mode & MacroMode.ProcessChildrenBefore) != 0)
					macroInput = ProcessChildrenOfCurrentNode();

				LNode output = null;
				s.DropRemainingNodes = (macro.Mode & MacroMode.DropRemainingListItems) != 0;
				int mhi = messageList.Count;
				try {
					Scope scope = AutoInitScope();
					MacrosInvoked++;
					output = macro.Macro(macroInput, scope);
					if (output != null) { accepted++; acceptedIndex = i; }
				} catch (ThreadAbortException e) {
					_sink.Write(Severity.Error, input, "Macro-processing thread aborted in {0}", QualifiedName(macro.Macro.Method));
					_sink.Write(Severity.Detail, input, e.StackTrace);
					s.Results.Add(new MacroResult(macro, output, messageList.Slice(mhi, messageList.Count - mhi), s.DropRemainingNodes));
					PrintMessages(s.Results, input, accepted, Severity.Error);
					throw;
				} catch (Exception e) {
					s.MessageHolder.Write(Severity.Error, input, "{0}: {1}", e.GetType().Name, e.Message);
					s.MessageHolder.Write(Severity.Detail, input, e.StackTrace);
				}
				s.Results.Add(new MacroResult(macro, output, messageList.Slice(mhi, messageList.Count - mhi), s.DropRemainingNodes));
			}

			s.DropRemainingNodes = false;

			PrintMessages(s.Results, input, accepted,
				s.MessageHolder.List.MaxOrDefault(msg => (int)msg.Severity).Severity);

			if (accepted >= 1) {
				var result = s.Results[acceptedIndex];
				NodesReplaced++;
					
				Debug.Assert(result.NewNode != null);
				if ((result.Macro.Mode & MacroMode.ProcessChildrenBefore) != 0)
					s.MaxExpansions--;

				LNode result2;
				if ((result.Macro.Mode & MacroMode.Normal) != 0) {
					if (result.NewNode == input)
						result2 = ApplyMacrosToChildrenOf(result.NewNode, s.MaxExpansions - 1);
					else {
						result2 = ApplyMacros(result.NewNode, s.MaxExpansions - 1, s.IsTarget);
						if (result2 != null)
							result.DropRemainingNodes |= _s.DropRemainingNodes;
					}
				} else if ((result.Macro.Mode & MacroMode.ProcessChildrenAfter) != 0) {
					result2 = ApplyMacrosToChildrenOf(result.NewNode, s.MaxExpansions - 1);
				} else
					return result.NewNode;

				s.DropRemainingNodes = result.DropRemainingNodes;
				return result2 ?? result.NewNode;
			} else {
				return s.Preprocessed ?? ApplyMacrosToChildrenOf(input, s.MaxExpansions);
			}
		}

		internal LNode ProcessChildrenOfCurrentNode()
		{
			if (_s.Preprocessed == null) {
				// ApplyMacrosFound2() is using the lists _foundMacros, _results,
				// and _messageHolder; make new lists so that processing the 
				// children won't disturb them.
				var s = _s;
				_s = new CurNodeState();
				try {
					Debug.Assert(s.Input == _ancestorStack.Last);					
					s.Preprocessed = ApplyMacrosToChildrenOf(s.Input, s.MaxExpansions - 1) ?? s.Input;
				} finally {
					_s = s;
				}
			}
			return _s.Preprocessed;
		}

		struct MacroResult
		{
			public MacroResult(MacroInfo macro, LNode newNode, ListSlice<MessageHolder.Message> msgs, bool dropRemaining)
			{
				Macro = macro; NewNode = newNode; Msgs = msgs; DropRemainingNodes = dropRemaining;
			}
			public MacroResult(LNode lNode)
			{
				Macro = null; NewNode = lNode; Msgs = ListSlice<MessageHolder.Message>.Empty; DropRemainingNodes = false;
			}
			public MacroInfo Macro; 
			public LNode NewNode;
			public ListSlice<MessageHolder.Message> Msgs;
			public bool DropRemainingNodes; // delete rest of nodes in current Args/Attrs list

			public MacroResult MaybeWith(LNode newNode)
			{
				return new MacroResult(Macro, newNode ?? NewNode, Msgs, DropRemainingNodes);
			}
		}

		RVList<LNode> ApplyMacrosToList(RVList<LNode> list, int maxExpansions, bool areAttributes)
		{
			RVList<LNode> results = list;
			LNode result = null;
			int i, count;
			// Share as much of the original RVList as is left unchanged
			for (i = 0, count = list.Count; i < count; i++) {
				_s.StartNextListItem(list, i, areAttributes);
				LNode input = list[i];
				result = ApplyMacros(input, maxExpansions, false);
				if (result != null || (result = input).Calls(S.Splice)) {
					results = list.WithoutLast(count - i);
					Add(ref results, result);
					break;
				}
			}
			// Prepare a modified list from now on
			for (i++; i < count && !_s.DropRemainingNodes; i++) {
				_s.StartNextListItem(list, i, areAttributes);
				LNode input = list[i];
				result = ApplyMacros(input, maxExpansions, false);
				if (result != null || (result = input).Calls(S.Splice))
					Add(ref results, result);
				else
					results.Add(input);
			}
			_s.IsAttribute = false;
			return results;
		}
		private void Add(ref RVList<LNode> results, LNode result)
		{
			if (result.Calls(S.Splice))
				results.AddRange(result.Args);
			else
				results.Add(result);
		}

		LNode ApplyMacrosToChildrenOf(LNode node, int maxExpansions)
		{
			if (maxExpansions <= 0)
				return null;

			bool changed = false;
			RVList<LNode> old;
			var newAttrs = ApplyMacrosToList(old = node.Attrs, maxExpansions, true);
			if (newAttrs != old) {
				node = node.WithAttrs(newAttrs);
				changed = true;
			}
			LNode target = node.Target;
			if (target != null && target.Kind != NodeKind.Literal) {
				LNode newTarget = ApplyMacros(target, maxExpansions, true);
				if (newTarget != null) {
					if (newTarget.Calls(S.Splice, 1))
						newTarget = newTarget.Args[0];
					node = node.WithTarget(newTarget);
					changed = true;
				}
			}
			var newArgs = ApplyMacrosToList(old = node.Args, maxExpansions, false);
			if (newArgs != old) {
				node = node.WithArgs(newArgs);
				changed = true;
			}
			return changed ? node : null;
		}

		void PrintMessages(InternalList<MacroResult> results, LNode input, int accepted, Severity maxSeverity)
		{
			if (accepted > 1) {
				// Multiple macros accepted the input. If AllowDuplicates is used, 
				// this is fine if as long as they produced the same result.
				bool allowed, equal = AreAllOutcomesEqual(results, out allowed);
				if (!equal || !allowed)
				{
					string list = results.Where(r => r.NewNode != null).Select(r => QualifiedName(r.Macro.Macro.Method)).Join(", ");
					if (equal)
						_sink.Write(Severity.Warning, input, "Ambiguous macro call. {0} macros accepted the input and produced identical results: {1}", accepted, list);
					else
						_sink.Write(Severity.Error, input, "Ambiguous macro call. {0} macros accepted the input: {1}", accepted, list);
				}
			}

			bool macroStyleCall = input.BaseStyle == NodeStyle.Special;

			if (accepted > 0 || macroStyleCall || maxSeverity >= Severity.Warning)
			{
				if (macroStyleCall && maxSeverity < Severity.Warning)
					maxSeverity = Severity.Warning;
				var rejected = results.Where(r => r.NewNode == null && (r.Macro.Mode & MacroMode.Passive) == 0);
				if (accepted == 0 && macroStyleCall && _sink.IsEnabled(maxSeverity) && rejected.Any())
				{
					_sink.Write(maxSeverity, input, "{0} macro(s) saw the input and declined to process it: {1}", 
						results.Count, rejected.Select(r => QualifiedName(r.Macro.Macro.Method)).Join(", "));
				}
			
				foreach (var result in results)
				{
					bool printedLast = true;
					foreach(var msg in result.Msgs) {
						// Print all messages from macros that accepted the input. 
						// For rejecting macros, print warning/error messages, and 
						// other messages when macroStyleCall.
						if (_sink.IsEnabled(msg.Severity) && (result.NewNode != null
							|| (msg.Severity == Severity.Detail && printedLast)
							|| msg.Severity >= Severity.Warning
							|| macroStyleCall))
						{
							var msg2 = new MessageHolder.Message(msg.Severity, msg.Context,
								QualifiedName(result.Macro.Macro.Method) + ": " + msg.Format, msg.Args);
							msg2.WriteTo(_sink);
							printedLast = true;
						} else
							printedLast = false;
					}
				}
			}
		}

		private static bool AreAllOutcomesEqual(InternalList<MacroResult> results, out bool allowed)
		{
			allowed = false;
			for (int i = 0; i < results.Count; i++) {
				MacroResult r, r2;
				if ((r = results[i]).NewNode != null) {
					allowed = (r.Macro.Mode & MacroMode.AllowDuplicates) != 0;
					for (int i2 = i + 1; i2 < results.Count; i2++) {
						if ((r2 = results[i2]).NewNode != null) {
							allowed |= (r2.Macro.Mode & MacroMode.AllowDuplicates) != 0;
							if (!r.NewNode.Equals(r2.NewNode))
								return false;
						}
					}
					break;
				}
			}
			return true;
		}

		private string QualifiedName(MethodInfo method)
		{
			return string.Format("{0}.{1}.{2}", method.DeclaringType.Namespace, method.DeclaringType.Name, method.Name);
		}

		#endregion
	}
}
