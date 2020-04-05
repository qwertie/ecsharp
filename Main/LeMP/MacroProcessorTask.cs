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
using Loyc.Collections.MutableListExtensionMethods;
using Loyc.Syntax;
using Loyc.Threading;

namespace LeMP
{
	using System.IO;
	using S = CodeSymbols;

	/// <summary>Holds the transient state of the macro processor. Since one
	/// <see cref="MacroProcessor"/> object can process multiple files in 
	/// parallel, we need an inner class to hold the state of each individual 
	/// transformation task.</summary>
	/// <remarks>
	/// The following simplified pseudocode summarizes the essential elements
	/// of how MacroProcessorTask works in typical cases.
	/// <pre>
	///  ProcessRoot(stmts) { 
	///     PreProcess(ref stmts, ...);
	///  }
	///  PreProcess(ref list, ...) {
	///     Save _s, _ancestorStack, etc. and restore on exit
	///     Initialize _s, _ancestorStack, root scope
	///     list = ApplyMacrosToList(list, ...);
	///  }
	///  ApplyMacrosToList(list, ...) {
	///    Call ApplyMacros(...) to apply macros to each item in the list.
	///    If no macros produce a new result, return the original list.
	///    otherwise, initialize the result list and node queue.
	///    For each remaining node in the node queue, 
	///      call ApplyMacros(...) on that node and add result to results.
	///    Return results.
	///  }
	///  ApplyMacros(LNode input, ...) {
	///    for (;;) {
	///      the usual sequence of events is 
	///      1. Call GetApplicableMacros(...) to find any relevant macros
	///      2. If any macros apply, call ApplyMacrosFound(...)
	///      3. If no macros produced output, call ApplyMacrosToChildrenOf(...) and return.
	///         otherwise, restart the loop using the output as the next iteration's input
	///    }
	///  }
	///  ApplyMacrosFound(CurNodeState s) {
	///    Separate the applicable macros (s.FoundMacros) into groups by priority.
	///    For each priority level starting at the highest priority,
	///      Call ApplyMacrosFound2(s, foundMacros)
	///  }
	///  ApplyMacrosFound2(CurNodeState s, foundMacros) {
	///    For each macro in foundMacros,
	///      Preprocess child nodes if the macro uses MacroMode.ProcessChildrenBefore
	///      Invoke the macro and save its result for later, including any messages
	///    Print messages if applicable
	///    Return the result of the first macro to have produced a result,
	///      or null if no macro produced a result.
	///  }
	///  ApplyMacrosToChildrenOf(LNode node, ...) {
	///    Apply macros to attributes: ApplyMacrosToList(node.Attrs, ...)
	///    Apply macros to Target, if any: ApplyMacros(node.Target, ...); 
	///    Apply macros to Args: ApplyMacrosToList(node.Args, ...);
	///  }
	/// </pre>
	/// Not shown: a stack of <see cref="Scope"/> objects keep track of information
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
		public MacroProcessorTask(MacroProcessor parent)
		{
			_parent = parent;
			_macros = parent.Macros.Clone();
			foreach (var mi in MacroInfo.GetMacros(this.GetType(), _sink, GSymbol.Empty, this))
				MacroProcessor.AddMacro(_macros, mi);
			_macroNamespaces = new MSet<Symbol>(_macros.SelectMany(ms => ms.Value).SelectMany(mi => mi.Namespaces).Where(ns => ns != null));
			_rootMacros = _macros.Clone();
			_rootScopedProperties = parent.DefaultScopedProperties.Clone();
		}

		#region Fields

		MacroProcessor _parent;
		IMessageSink _sink { get { return _parent.Sink; } }
		int MaxExpansions { get { return _parent.MaxExpansions; } }
		MMap<Symbol, InternalList<InternalMacroInfo>> _macros;
		MSet<Symbol> _macroNamespaces; // A list of namespaces that contain macros
		MMap<object, object> _rootScopedProperties;
		// macros defined from the beginning (not including e.g. macros added with `define`)
		MMap<Symbol, InternalList<InternalMacroInfo>> _rootMacros;
		
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

		public LNodeList ProcessFileWithThreadAbort(InputOutput io, Action<InputOutput> onProcessed, TimeSpan timeout)
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
					if (ex == null && onProcessed != null)
						onProcessed(io);
				} else {
					io.Output = new VList<LNode>(F.Id("processing_thread_timed_out"));
					thread.Abort();
					thread.Join(timeout);
				}
				if (ex != null)
					throw ex;
				return io.Output;
			}
		}
		public LNodeList ProcessFile(InputOutput io, Action<InputOutput> onProcessed)
		{
			using (ParsingService.SetDefault(io.InputLang ?? ParsingService.Default)) {
				try {
					string dir = Path.GetDirectoryName(io.FileName);
					if (!string.IsNullOrEmpty(dir))
						_rootScopedProperties[(Symbol)"#inputFolder"] = dir;
					_rootScopedProperties[(Symbol)"#inputFileName"] = Path.GetFileName(io.FileName);
				} catch (ArgumentException) { }    // Path.* may throw
				  catch (PathTooLongException) { } // Path.* may throw

				var input = ParsingService.Default.Parse(io.Text, io.FileName, _sink, io.ParsingMode, io.PreserveComments ?? true);
				var inputRV = new VList<LNode>(input);

				io.Output = ProcessRoot(inputRV);
				if (onProcessed != null)
					onProcessed(io);
				return io.Output;
			}
		}

		/// <summary>Top-level macro applicator.</summary>
		public LNodeList ProcessRoot(LNodeList stmts)
		{
			PreProcess(ref stmts, null, true, true, true, false);
			return stmts;
		}

		// This is called either at the root node, or by a macro that wants to 
		// preprocess its children (see IMacroContext.PreProcess()).
		LNode PreProcess(ref LNodeList list, LNode single, bool asRoot, bool resetOpenNamespaces, bool resetProperties, bool areAttributesOrIsTarget)
		{
			if (single == null && list.Count == 0)
				return null; // no-op requested
			var oldS = _s;
			var oldAncestors = _ancestorStack;
			var oldMP = MacroProcessor._current;
			MacroProcessor._current = _parent;
			bool newScope = false;
			int maxExpansions = asRoot ? MaxExpansions : _s.MaxExpansions - 1;
			try {
				bool reentrant = _reentrancyCounter++ != 0;
				if (!reentrant)
					asRoot = true;
				Debug.Assert(reentrant || _scopes.Count == 0);
				Debug.Assert(reentrant || _ancestorStack == null);
				
				if (asRoot)
					_ancestorStack = new DList<LNode>();
				_s = new CurNodeState();
				if (asRoot || resetOpenNamespaces || resetProperties) {
					var namespaces = !reentrant || resetOpenNamespaces ? _parent._preOpenedNamespaces : _curScope.OpenNamespaces;
					var properties = resetProperties ? _rootScopedProperties : _curScope.ScopedProperties;
					var macros = resetOpenNamespaces ? _rootMacros : _macros;
					newScope = true;
					_curScope = new Scope(namespaces, properties, macros, this);
					_scopes.Add(_curScope);
				}
				if (single != null) {
					DList<Pair<LNode, int>> _ = null;
					return ApplyMacros(single, maxExpansions, areAttributesOrIsTarget, true, ref _) ?? single;
				} else {
					int oldStackCount = _ancestorStack.Count;
					LNode splice = null;
					if (asRoot) {
						splice = list.AsLNode(S.Splice);
						_ancestorStack.PushLast(splice);
					}
					list = ApplyMacrosToList(list, maxExpansions, areAttributesOrIsTarget);
					if (asRoot)
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
			public Scope(MSet<Symbol> openNamespaces, MMap<object, object> scopedProperties, MMap<Symbol, InternalList<InternalMacroInfo>> macros, MacroProcessorTask task)
				{ OpenNamespaces = openNamespaces; _scopedProperties = scopedProperties; _macros = macros; _task = task; _modified = false; }
			public MSet<Symbol> OpenNamespaces;
			MacroProcessorTask _task;
			MMap<object, object> _scopedProperties;
			internal MMap<Symbol, InternalList<InternalMacroInfo>> _macros;
			bool _modified; // copy-on-write behavior occurs when this is false
				
			public void BeforeModify()
			{
				if (!_modified) {
					_modified = true;
					OpenNamespaces = OpenNamespaces.Clone();
					_scopedProperties = _scopedProperties.Clone();
					_macros = _macros.Clone();
				}
			}
			public Scope Clone()
			{
				return new Scope(OpenNamespaces, _scopedProperties, _macros, _task);
			}

			#region IMacroContext Members

			IDictionary<object, object> IMacroContext.ScopedProperties { get { return ScopedProperties; } }
			public MMap<object, object> ScopedProperties
			{
				get {
					BeforeModify();
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

			public LNodeList PreProcess(LNodeList input, bool asRoot = false, bool resetOpenNamespaces = false, bool resetProperties = false, bool areAttributes = false)
			{
				_task.PreProcess(ref input, null, asRoot, resetOpenNamespaces, resetProperties, areAttributes);
				return input;
			}
			public LNode PreProcess(LNode input, bool asRoot = false, bool resetOpenNamespaces = false, bool resetProperties = false, bool isTarget = false)
			{
				LNodeList empty = LNodeList.Empty;
				return _task.PreProcess(ref empty, input, asRoot, resetOpenNamespaces, resetProperties, isTarget);
			}
			
			public LNode PreProcessChildren()
			{
				return _task.ProcessChildrenOfCurrentNode();
			}

			public IMessageSink Sink { get { return _task._s.MessageHolder; } }
			public IListSource<LNode> RemainingNodes { get { return _task._s.RemainingNodes; } }
			public bool IsAttribute { get { return _task._s.IsAttribute; } }
			public bool IsTarget { get { return _task._s.IsTarget; } }
			public bool DropRemainingNodes { get { return _task._s.DropRemainingNodesRequested; } set { _task._s.DropRemainingNodesRequested = value; } }

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

			public IReadOnlyDictionary<Symbol, VList<MacroInfo>> AllKnownMacros =>
				_macros.Keys.AsReadOnly().AsReadOnlyDictionary<Symbol, VList<MacroInfo>>(k => {
					if (_macros.TryGetValue(k, out var infos)) {
						return new VList<MacroInfo>(infos.SelectMany(
							info => info.Namespaces,
							(info, ns) => new MacroInfo(ns, info.Attr, info.Macro)));
					}
					return NoValue.Value;
				});

			public int NextTempCounter { get { return MacroProcessor.NextTempCounter; } }
			public int IncrementTempCounter() { return MacroProcessor.IncrementTempCounter(); }

			public void RegisterMacro(MacroInfo macroInfo)
			{
				BeforeModify();
				MacroProcessor.AddMacro(_macros, macroInfo);
			}

			#endregion
		}

		#endregion

		// State variables related to the current node and current macro invocation.
		// Note: the way state is managed here is a recurring source of bugs, but I 
		// can't think of a less-brittle-but-still-efficient code structure that 
		// doesn't produce tons of garbage objects.
		class CurNodeState
		{
			public LNode Input;
			public bool IsAttribute, IsTarget;
			
			// Number of nested macro expansions that can be rooted at current  
			// node (the total number of expansions is often higher)
			public int MaxExpansions; 

			// Optimization: these lists are re-used on each call to ApplyMacros,
			// to avoid allocating unnecessary garbage.
			public List<InternalMacroInfo> FoundMacros = new List<InternalMacroInfo>();
			public MessageHolder MessageHolder = new MessageHolder();
			public InternalList<MacroResult> Results = InternalList<MacroResult>.Empty;

			#region RemainingNodes property and the NodeQueue

			// These fields support the IMacroContext.RemainingNodes property.
			// OldAndRemainingNodes contains (A) old nodes that been processed, 
			// (B) the old version of the current node that is being processed now,
			// and (C) remaining nodes. Only (C) matters, but we keep the whole 
			// list here to avoid computing a separate sublist for each iteration 
			// through a given VList<LNode>.
			//     If a macro returns #splice($(...SpliceNodes)), the situation 
			// gets more complex, because we have to treat the spliced nodes plus 
			// the remaining nodes (C) as a single list if a macro ever asks for 
			// RemainingNodes (the #ecs macro relies on this fact). Plus, the 
			// spliced items will have a lower value of maxExpansions than items 
			// that are not yet processed. The info for this complex case is 
			// stored in NodeQueue.
			//     To simplify the implementation of ApplyMacrosToList, the 
			// NodeQueue is created after a macro produces any modified output
			// or after a macro sets DropRemainingNodesRequested.
			public VList<LNode> OldAndRemainingNodes;
			public int CurrentNodeIndex;
			public DList<Pair<LNode, int>> NodeQueue;

			private IListSource<LNode> _remainingNodes;
			public IListSource<LNode> RemainingNodes { 
				get {
					if (_remainingNodes == null && !IsTarget) {
						if (NodeQueue != null)
							_remainingNodes = NodeQueue.Select(p => p.A);
						else
							_remainingNodes = OldAndRemainingNodes.Slice(CurrentNodeIndex + 1);
					}
					return _remainingNodes; 
				}
			}
			public bool HasRemainingNodes {
				get {
					return CurrentNodeIndex + 1 < OldAndRemainingNodes.Count || !NodeQueue.IsEmpty;
				}
			}
			// Puts contents of a #splice into NodeQueue and returns the first item of the splice.
			public LNode EnqueueSplice(VList<LNode> spliced, int maxExpansionsInner, int maxExpansions)
			{
				//Debug.Assert(!IsTarget); should be true except that IsTarget may not have been set yet
				MaybeCreateNodeQueue(maxExpansions, ref NodeQueue);

				// Enqueue spliced items
				foreach (var item in spliced.ToFVList())
					NodeQueue.PushFirst(Pair.Create(item, maxExpansionsInner));

				return NodeQueue.PopFirst().A;
			}
			// Converts the list of remaining nodes from an implicit sublist of 
			// OldAndRemainingNodes into an explicit queue, if it wasn't done yet.
			public void MaybeCreateNodeQueue(int maxExpansions, ref DList<Pair<LNode, int>> nodeQueue)
			{
				//Debug.Assert(!IsTarget); should be true except that IsTarget may not have been set yet
				if (nodeQueue == null) {
					// Transfer OldAndRemainingNodes into NodeQueue
					nodeQueue = new DList<Pair<LNode, int>>();
					if (!DropRemainingNodesIfRequested()) {
						for (int left = OldAndRemainingNodes.Count - (CurrentNodeIndex + 1); left > 0; left--)
							nodeQueue.PushFirst(Pair.Create(OldAndRemainingNodes.Pop(), maxExpansions));
					}
					OldAndRemainingNodes = LNode.List();
					_remainingNodes = null;
				}
			}

			#endregion

			// Cached result of applying macros to the children of the current node.
			// If the current macro has no effect, this result is returned from 
			// ApplyMacros so it doesn't need to reprocess subtrees a second time.
			public LNode Preprocessed;

			public bool DropRemainingNodesRequested; // Any macro can set this flag
			public bool DropRemainingNodesIfRequested()
			{
				if (DropRemainingNodesRequested && !IsTarget) {
					NodeQueue = new DList<Pair<LNode, int>>(); // informs
					OldAndRemainingNodes = LNode.List();
					_remainingNodes = null;
				}
				return DropRemainingNodesRequested;
			}
			
			public void StartListItem(LNodeList list, int index, bool areAttributes)
			{
				OldAndRemainingNodes = list;
				CurrentNodeIndex = index;
				IsAttribute = areAttributes;
				DropRemainingNodesRequested = false;
			}
			public void StartNextNode(LNode input, int maxExpansions, bool isTargetNode)
			{
				Input = input;
				FoundMacros.Clear();
				_remainingNodes = null;
				Preprocessed = null;
				MaxExpansions = maxExpansions;
				IsTarget = isTargetNode;
				Debug.Assert(!IsTarget || !IsAttribute);
				DropRemainingNodesRequested = false;
			}
			public void BeforeApplyMacros()
			{
				MessageHolder.List.Clear();
				Results.Resize(0);
			}
		}

		#region Find macros by name: GetApplicableMacros

		public int GetApplicableMacros(IReadOnlyCollection<Symbol> openNamespaces, Symbol name, ICollection<InternalMacroInfo> found, bool isIdentifier)
		{
			if (_curScope._macros.TryGetValue(name, out var candidates)) {
				int count = 0;
				foreach (var info in candidates) {
					if (!isIdentifier || (info.Attr.Mode & MacroMode.MatchIdentifier) != 0)
					{
						foreach (var ns in info.Namespaces)
							if (openNamespaces.Contains(ns) || ns == null)
								goto available;
						continue;
					 available:
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

		[LexicalMacro("#importMacros(namespace);", 
			"LeMP will look for macros in the specified namespace. Only applies within the current braced block. Note: normal C# `using` statements also import macros.", 
			"#importMacros", Mode = MacroMode.Normal)]
		public LNode importMacros(LNode node, IMacroContext context)
		{
			OnImport(node, context, true);
			return F.Call(S.Splice);
		}
		[LexicalMacro("#import(namespace);", 
			"LeMP will look for macros in the specified namespace.", 
			"#import", Mode = MacroMode.Normal | MacroMode.Passive)]
		public LNode OnImport(LNode node, IMacroContext context) { return OnImport(node, context, false); }
		public LNode OnImport(LNode node, IMacroContext context, bool expectMacros)
		{
			AutoInitScope().BeforeModify();
			foreach (var arg in node.Args) {
				var namespaceSym = NamespaceToSymbol(arg);
				_curScope.OpenNamespaces.Add(namespaceSym);
				if (expectMacros && !_macroNamespaces.Contains(namespaceSym))
					context.Sink.Warning(node, "Namespace '{0}' does not contain any macros. Use #printKnownMacros to put a list of known macros in the output.", namespaceSym);
			}
			return null;
		}

		[LexicalMacro("#registerMacro(<literal of type MacroInfo>);",
			"Defines a new macro. Typically, the argument is produced by another macro",
			"#registerMacro", Mode = MacroMode.Normal)]
		public LNode registerMacro(LNode node, IMacroContext context)
		{
			MacroInfo macroInfo;
			if (node.ArgCount == 1 && (macroInfo = node.Args[0].Value as MacroInfo) != null) {
				AutoInitScope().RegisterMacro(macroInfo);
				return F.Call(S.Splice);
			}
			context.Sink.Error(node, "Expected a single literal argument of type MacroInfo");
			return null;
		}

		[LexicalMacro("#unimportMacros(namespace1, namespace2)",
			"Tells LeMP to stop looking for macros in the specified namespace(s). Only applies within the current braced block.", 
			"#unimportMacros", Mode = MacroMode.Normal | MacroMode.Passive)]
		public LNode unimportMacros(LNode node, IMacroContext context)
		{
			AutoInitScope().BeforeModify();
			foreach (var arg in node.Args) {
				var sym = NamespaceToSymbol(arg);
				if (!_curScope.OpenNamespaces.Remove(sym))
					context.Write(Severity.Debug, arg, "Namespace not found to remove: {0}", sym);
			}
			return LNode.Call(S.Splice);
		}
		[LexicalMacro("#noLexicalMacros(expr)", 
			"Suppresses macro invocations inside the specified expression. The word `#noLexicalMacros` is removed from the output. Note: `noMacro` (in LeMP.Prelude) is a shortened synonym for this macro.",
			"#noLexicalMacros", Mode = MacroMode.NoReprocessing)]
		public static LNode noLexicalMacros(LNode node, IMacroContext context)
		{
			if (!node.IsCall)
				return null;
			return node.Args.AsLNode(S.Splice);
		}

		[LexicalMacro("#setScopedProperty(keyLiteral, valueLiteral);", 
			"Sets the value of a scoped property, using the first parameter (usually a @@symbol) as a key in the property dictionary. The key and value must both be literals; expressions are not supported.", 
			"#setScopedProperty", Mode = MacroMode.Normal)]
		public static LNode setScopedProperty(LNode node, IMacroContext context)
		{
			LNode key;
			if (node.ArgCount == 2 && (key = context.PreProcess(node[0])).IsLiteral && node[1].IsLiteral) {
				context.ScopedProperties[key.Value] = node[1].Value;
				return F.Call(S.Splice);
            }
			context.Sink.Error(node, "Expected two literals as parameters (key, value).");
			return null;
		}

		[LexicalMacro("#setScopedPropertyQuote(keyLiteral, valueCode);", 
			"Sets the value of a scoped property to an LNode, using the first parameter (usually a @@symbol) as a key in the property dictionary. The key must be a literal, while the value can be _any_ expression.", 
			"#setScopedPropertyQuote", Mode = MacroMode.Normal)]
		public static LNode setScopedPropertyQuote(LNode node, IMacroContext context)
		{
			LNode key;
			if (node.ArgCount == 2 && (key = context.PreProcess(node[0])).IsLiteral) {
				context.ScopedProperties[key.Value] = node[1];
				return F.Call(S.Splice);
            }
			context.Sink.Error(node, "Expected two parameters (key, value), of which the first is a literal.");
			return null;
		}

		[LexicalMacro("#getScopedProperty(keyLiteral, defaultCode);", 
			"Replaces the current node with the value of a scoped property. The key must be a literal or an identifier. "+
			"If the key is an identifier, it is treated as a symbol instead, e.g. `KEY` is equivalent to `@@KEY`. "+
			"If the scoped property is an LNode, the code it represents is expanded in-place. "+
			"If the scoped property is anything else, its value is inserted as a literal. "+
			"If the property does not exist, the second parameter is used instead. "+
			"The second parameter is optional; if there is no second parameter and the "+
			"requested property does not exist, an error is printed.",
			"#getScopedProperty", Mode = MacroMode.Normal)]
		public static LNode getScopedProperty(LNode node, IMacroContext context)
		{
			LNode key;
			if (node.ArgCount >= 1 && !(key = context.PreProcess(node.Args[0])).IsCall)
			{
				var keyValue = key.IsId ? key.Name : key.Value;
				var @default = node.Args[1, node];
				var result = context.ScopedProperties.TryGetValue(keyValue, @default);
				if (result == node)
					context.Sink.Error(key, "The specified property does not exist.");
				if (result is LNode)
					return (LNode)result;
				else
					return LNode.Literal(result, node);
			}
			context.Sink.Error(node, "Expected one argument, a key literal, with the default code as an optional second argument.");
			return null;
		}

		private void PopScope()
		{
			_scopes.Pop();
			bool fail = false;
			for (int i = _scopes.Count - 1; !fail; i--)
				if ((_curScope = _scopes.TryGet(i, out fail)) != null)
					break;
		}

		#endregion

		#region Algorithm for applying macros to individual nodes, lists & children

		Symbol NamespaceToSymbol(LNode node)
		{
			// quick & dirty, probably not cheap
			return GSymbol.Get(node.Print(ParsingMode.Expressions));
		}

		/// <summary>Recursively applies macros in scope to <c>input</c>.</summary>
		/// <param name="maxExpansions">Maximum number of opportunities given 
		/// to macros to transform a given subtree. The output of any macro is
		/// transformed again (as if by calling this method) with 
		/// <c>maxExpansions = maxExpansions - 1</c> to encourage the 
		/// expansion process to terminate eventually.</param>
		/// <param name="nodeQueue">The act of processing child nodes (by calling 
		/// ApplyMacrosToChildrenOf) invalidates most members of _s including 
		/// _s.NodeQueue. But when ApplyMacrosToList calls this method it needs
		/// the node queue, so this method saves _s.NodeQueue in nodeQueue before
		/// doing something that will destroy _s.NodeQueue. It also sets 
		/// _s.NodeQueue = nodeQueue when it starts.</param>
		/// <returns>Returns a transformed tree (or null if the macros did not 
		/// change the syntax tree at any level).</returns>
		/// <remarks>EnqueueSplice is used if a #splice(...) is encountered.</remarks>
		LNode ApplyMacros(LNode input, int maxExpansions, bool isTargetNode, bool isSingleNode, ref DList<Pair<LNode, int>> nodeQueue)
		{
			_s.NodeQueue = nodeQueue;
			int maxExpansionsBefore = maxExpansions;
			LNode resultNode = null;
			for(LNode curNode = input; maxExpansions > 0; curNode = resultNode)
			{
				// If #splice, expand it
				if (!isSingleNode && curNode.Calls(S.Splice)) {
					if (curNode.ArgCount == 0)
						return curNode; // #splice()
					curNode = resultNode = _s.EnqueueSplice(curNode.Args, maxExpansions, maxExpansionsBefore);
					Debug.Assert(curNode != null);
				}

				_s.StartNextNode(curNode, maxExpansions, isTargetNode);
				GetApplicableMacros(curNode, _s.FoundMacros);
				if (_s.FoundMacros.Count == 0 && curNode.ArgCount == 0 
						&& curNode.HasSimpleHead() && !curNode.HasAttrs) {
					nodeQueue = _s.NodeQueue;
					return resultNode; // most common case: a boring leaf node
				}

				bool braces = curNode.Calls(S.Braces);
				if (braces)
					_scopes.Add(null);

				MacroResult result;
				_ancestorStack.PushLast(curNode);
				try {
					if (_s.FoundMacros.Count == 0) {
						nodeQueue = _s.NodeQueue;
						bool skipTarget = curNode.HasSimpleHeadWithoutPAttrs();
						return ApplyMacrosToChildrenOf(curNode, maxExpansions, skipTarget) ?? resultNode;
					}

					// USER MACROS RUN HERE!
					var result_ = ApplyMacrosFound(_s);
					if (result_ == null) {
						// Macro(s) had no effect (not in this iteration, anyway), 
						// so move on to processing children.
						nodeQueue = _s.NodeQueue;
						bool skipTarget = curNode.HasSimpleHeadWithoutPAttrs() && !_s.FoundMacros.Any(m => (m.Mode & MacroMode.MatchIdentifier) != 0);
						return _s.Preprocessed ?? ApplyMacrosToChildrenOf(curNode, maxExpansions, skipTarget) ?? resultNode;
					}
					result = result_.Value;
				} finally {
					_ancestorStack.PopLast(1);
					if (braces)
						PopScope();
				}

				// Deal with result produced by the macro
				NodesReplaced++;
				Debug.Assert(result.NewNode != null);
				_s.DropRemainingNodesIfRequested();
				
				resultNode = result.NewNode;
				nodeQueue = _s.NodeQueue;
				if ((result.Macro.Mode & MacroMode.ProcessChildrenAfter) != 0) {
					return ApplyMacrosToChildrenOf(resultNode, maxExpansions - 1) ?? resultNode;
				} else if ((result.Macro.Mode & (MacroMode.NoReprocessing | MacroMode.ProcessChildrenBefore)) != 0) {
					return resultNode; // we're done!
				} else if (resultNode == curNode) {
					// node is unchanged, so reprocessing would produce the 
					// same result. Don't do that, just process children.
					return ApplyMacrosToChildrenOf(resultNode, maxExpansions - 1) ?? resultNode;
				} else {
					// Avoid deepening the call stack like we used to do...
					//   result2 = ApplyMacros(result.NewNode, s.MaxExpansions - 1, s.IsTarget);
					//   if (result2 != null) result.DropRemainingNodesRequested |= _s.DropRemainingNodesRequested;
					// instead, iterate, changing our own parameters.
					maxExpansions--;
					Debug.Assert(isTargetNode == _s.IsTarget);
				}
			}
			nodeQueue = _s.NodeQueue;
			return resultNode;
		}

		private void GetApplicableMacros(LNode curNode, List<InternalMacroInfo> foundMacros)
		{
			LNode target;
			if (curNode.HasSimpleHead()) {
				GetApplicableMacros(_curScope.OpenNamespaces, curNode.Name, foundMacros, curNode.IsId);
			} else if ((target = curNode.Target).Calls(S.Dot, 2) && target.Args[1].IsId) {
				Symbol name = target.Args[1].Name;
				if (_macros.ContainsKey(name)) {
					Symbol @namespace = NamespaceToSymbol(target.Args[0]);
					GetApplicableMacros(ListExt.Single(@namespace), name, foundMacros, false);
				}
			}
		}

		private MacroResult? ApplyMacrosFound(CurNodeState s)
		{
			var foundMacros = s.FoundMacros;
			// if any of the macros use a priority flag, group by priority.
			if (foundMacros.Count > 1) {
				var p = foundMacros[0].Attr.Priority;
				for (int x = 1; x < foundMacros.Count; x++) {
					if (foundMacros[x].Attr.Priority != p) {
						// need to make an independent list because _s.foundMacros may be cleared and re-used for descendant nodes
						foundMacros = new List<InternalMacroInfo>(foundMacros);
						foundMacros.Sort(InternalMacroInfo.CompareDescendingByPriority); // descending by priority
						for (int i = 0, j; i < foundMacros.Count; i = j) {
							p = foundMacros[i].Attr.Priority;
							for (j = i + 1; j < foundMacros.Count; j++)
								if (foundMacros[j].Attr.Priority != p)
									break;
							var newNode = ApplyMacrosFound2(s, ((IList<InternalMacroInfo>)foundMacros).Slice(i, j - i));
							if (newNode != null)
								return newNode;
						}
						return null;
					}
				}
			}
			return ApplyMacrosFound2(s, ((IList<InternalMacroInfo>)foundMacros).Slice(0));
		}

		private MacroResult? ApplyMacrosFound2(CurNodeState s, ListSlice<InternalMacroInfo> foundMacros)
		{
			s.BeforeApplyMacros();
			LNode input = s.Input;
			Debug.Assert(s.Results.IsEmpty && s.MessageHolder.List.Count == 0);
			IList<LogMessage> messageList = s.MessageHolder.List;

			int accepted = 0, acceptedIndex = -1;
			for (int i = 0; i < foundMacros.Count; i++)
			{
				var macro = foundMacros[i];
				var macroInput = input;
				if ((macro.Mode & MacroMode.ProcessChildrenBefore) != 0)
					macroInput = ProcessChildrenOfCurrentNode();

				LNode output = null;
				s.DropRemainingNodesRequested = (macro.Mode & MacroMode.DropRemainingListItems) != 0;
				int mhi = messageList.Count;
				try {
					Scope scope = AutoInitScope();
					MacrosInvoked++;
					
					// CALL THE MACRO!
					output = macro.Macro(macroInput, scope);
					
					if (output != null) {
						accepted++;
						acceptedIndex = i;
					}
				} catch (ThreadAbortException e) {
					_sink.Write(Severity.Error, "Macro-processing thread aborted in {0}", QualifiedName(macro.Macro.Method));
					_sink.Write(Severity.ErrorDetail, input, e.StackTrace);
					s.Results.Add(new MacroResult(macro, output, messageList.Slice(mhi, messageList.Count - mhi), s.DropRemainingNodesRequested));
					PrintMessages(s.Results, input, accepted, Severity.Error);
					throw;
				} catch (LogException e) {
					e.Msg.WriteTo(s.MessageHolder);
				} catch (Exception e) {
					s.MessageHolder.Write(Severity.Error, input, "{0}: {1}", e.GetType().Name, e.Message);
					s.MessageHolder.Write(Severity.ErrorDetail, input, e.StackTrace);
				}
				s.Results.Add(new MacroResult(macro, output, messageList.Slice(mhi, messageList.Count - mhi), s.DropRemainingNodesRequested));
			}

			s.DropRemainingNodesRequested = false;

			PrintMessages(s.Results, input, accepted,
				s.MessageHolder.List.MaxOrDefault(msg => (int)msg.Severity).Severity);

			s.DropRemainingNodesRequested = false;
			if (accepted >= 1) {
				var result = s.Results[acceptedIndex];
				_s.DropRemainingNodesRequested = result.DropRemainingNodes;
				return result;
			} else
				return null;
		}

		internal LNode ProcessChildrenOfCurrentNode()
		{
			if (_s.Preprocessed == null) {
				// ApplyMacros and ApplyMacrosFound() are already on the call 
				// stack. Make a new CurNodeState so that we do not disturb the 
				// state currently in use.
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
			public MacroResult(InternalMacroInfo macro, LNode newNode, ListSlice<LogMessage> msgs, bool dropRemaining)
			{
				Macro = macro; NewNode = newNode; Msgs = msgs; DropRemainingNodes = dropRemaining;
			}
			public MacroResult(LNode lNode)
			{
				Macro = null; NewNode = lNode; Msgs = ListSlice<LogMessage>.Empty; DropRemainingNodes = false;
			}
			public InternalMacroInfo Macro; 
			public LNode NewNode;
			public ListSlice<LogMessage> Msgs;
			public bool DropRemainingNodes; // delete rest of nodes in current Args/Attrs list

			public MacroResult MaybeWith(LNode newNode)
			{
				return new MacroResult(Macro, newNode ?? NewNode, Msgs, DropRemainingNodes);
			}
		}

		VList<LNode> ApplyMacrosToList(VList<LNode> list, int maxExpansions, bool areAttributes)
		{
			DList<Pair<LNode, int>> nodeQueue = null;
			VList<LNode> results;
			LNode result = null;
			
			// Share as much of the original VList as is left unchanged
			for (int i = 0, count = list.Count;; ++i)
			{
				if (i >= count)
					return list; // Entire list did not change

				_s.StartListItem(list, i, areAttributes);
				LNode input = list[i];
				result = ApplyMacros(input, maxExpansions, false, false, ref nodeQueue);
				if (result != null)
				{
					results = list.First(i);
					Add(ref results, result);
					// restore possibly-clobbered state
					_s.StartListItem(list, i, areAttributes);
					_s.MaybeCreateNodeQueue(maxExpansions, ref nodeQueue);
					break;
				}
			}
			
			// The rest of the list is in _s.NodeQueue. Process that.
			while (!nodeQueue.IsEmpty) {
				_s.StartListItem(LNode.List(), 0, areAttributes);
				Pair<LNode,int> input2 = nodeQueue.PopFirst();
				result = ApplyMacros(input2.A, input2.B, false, false, ref nodeQueue);
				Add(ref results, result ?? input2.A);
			}
			_s.NodeQueue = null;
			return results;
		}
		private void Add(ref VList<LNode> results, LNode result)
		{
			if (result.Calls(S.Splice))
				results.AddRange(result.Args);
			else
				results.Add(result);
		}

		LNode ApplyMacrosToChildrenOf(LNode node, int maxExpansions, bool skipTarget = false)
		{
			if (maxExpansions <= 0)
				return null;

			bool changed = false;
			VList<LNode> old;
			var newAttrs = ApplyMacrosToList(old = node.Attrs, maxExpansions, true);
			_s.IsAttribute = false;
			if (newAttrs != old)
			{
				node = node.WithAttrs(newAttrs);
				changed = true;
			}
			if (!skipTarget)
			{
				LNode target = node.Target;
				if (target != null && target.Kind != LNodeKind.Literal)
				{
					DList<Pair<LNode, int>> _ = null;
					LNode newTarget = ApplyMacros(target, maxExpansions, true, true, ref _);
					if (newTarget != null)
					{
						if (newTarget.Calls(S.Splice, 1))
							newTarget = newTarget.Args[0];
						node = node.WithTarget(newTarget);
						changed = true;
					}
				}
			}
			var newArgs = ApplyMacrosToList(old = node.Args, maxExpansions, false);
			if (newArgs != old)
			{
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
						_sink.Warning(input, "Ambiguous macro call. {0} macros accepted the input and produced equal results: {1}", accepted, list);
					else
						_sink.Error(input, "Ambiguous macro call. {0} macros accepted the input: {1}", accepted, list);
				}
			}

			bool macroStyleCall = input.BaseStyle == NodeStyle.Special;
			var rejected = results.Where(r => r.NewNode == null && (r.Macro.Mode & MacroMode.Passive) == 0);
			if (macroStyleCall && maxSeverity < Severity.Warning)
				maxSeverity = Severity.Warning;
			if (maxSeverity < Severity.Note)
				maxSeverity = Severity.Note;
			if (accepted == 0 && input.IsCall && _sink.IsEnabled(maxSeverity) && rejected.Any(r => r.Msgs.Count == 0))
			{
				_sink.Write(maxSeverity, input, "{0} macro(s) saw the input and declined to process it: {1}", 
					results.Count, rejected.Select(r => QualifiedName(r.Macro.Macro.Method)).Join(", "));
			}
			
			foreach (var result in results)
			{
				foreach(var msg in result.Msgs) {
					// Print all messages from macros that accepted the input. 
					// For rejecting macros, print warning/error messages, and 
					// other messages when macroStyleCall.
					if (_sink.IsEnabled(msg.Severity) && (result.NewNode != null
						|| msg.Severity >= Severity.WarningDetail
						|| macroStyleCall))
					{
						var msg2 = new LogMessage(msg.Severity, msg.Context,
							QualifiedName(result.Macro.Macro.Method) + ": " + msg.Format, msg.Args);
						msg2.WriteTo(_sink);
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
							if (!r.NewNode.Equals(r2.NewNode, LNode.CompareMode.IgnoreTrivia))
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
