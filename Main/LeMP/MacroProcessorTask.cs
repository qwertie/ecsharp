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
			_macroNamespaces = new MSet<Symbol>(
				_macros.SelectMany(ms => ms.Value)
				       .SelectMany(mi => mi.Namespaces.Select(p => p.A))
				       .Where(ns => ns != null));
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
		MSet<Symbol> _preOpenedNamespaces;

		// Statistics
		public int MacrosInvoked { get; set; }
		public int NodesReplaced { get; set; }

		// Ancestors of current node with their previous siblings, with the current node itself as the final item
		DList<Pair<LNodeList, LNode>> _ancestorStack;
		// Previously-processed Siblings of current node
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

		#region Main entry points: ProcessFileWithThreadAbort(), ProcessFile(), ProcessRoot()

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
					io.Output = new LNodeList(F.Id("processing_thread_timed_out"));
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
					_rootScopedProperties[(Symbol)"#inputPath"] = io.FileName;
					_rootScopedProperties[(Symbol)"#inputFileName"] = Path.GetFileName(io.FileName);
					_rootScopedProperties[(Symbol)"#inputFolder"] = Path.GetDirectoryName(io.FileName);
				} catch (ArgumentException) { }    // Path.* may throw
				  catch (PathTooLongException) { } // Path.* may throw

				var input = ParsingService.Default.Parse(io.Text, io.FileName, _sink, io.ParsingMode, io.PreserveComments ?? true);
				var inputRV = new LNodeList(input);

				io.Output = ProcessRoot(inputRV, io.PreOpenedNamespaces.Union(_parent._preOpenedNamespaces));
				if (onProcessed != null)
					onProcessed(io);
				return io.Output;
			}
		}

		/// <summary>Top-level macro applicator.</summary>
		public LNodeList ProcessRoot(LNodeList stmts, MSet<Symbol> preOpenedNamespaces)
		{
			_preOpenedNamespaces = preOpenedNamespaces ?? new MSet<Symbol>();
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
					_ancestorStack = new DList<Pair<LNodeList, LNode>>();
				_s = new CurNodeState();
				if (asRoot || resetOpenNamespaces || resetProperties) {
					var namespaces = !reentrant || resetOpenNamespaces ? _preOpenedNamespaces : _curScope._openNamespaces;
					var properties = resetProperties ? _rootScopedProperties : _curScope.ScopedProperties;
					var macros = resetOpenNamespaces ? _rootMacros : _macros;
					newScope = true;
					_curScope = new Scope(namespaces, properties, macros, this);
					_scopes.Add(_curScope);
				}
				if (single != null) {
					DList<Pair<LNode, int>> _ = null;
					return ApplyMacros(single, LNode.List(), maxExpansions, areAttributesOrIsTarget, true, ref _) ?? single;
				} else {
					int oldStackCount = _ancestorStack.Count;
					LNode splice = null;
					if (asRoot) {
						splice = list.AsLNode(S.Splice);
						_ancestorStack.PushLast(Pair.Create(LNode.List(), splice));
					}
					list = ApplyMacrosToList(list, maxExpansions, areAttributesOrIsTarget);
					if (asRoot)
						_ancestorStack.PopLast();
					Debug.Assert(_ancestorStack.Count == oldStackCount);
					return null; // caller ignores return value
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
			internal Scope(MSet<Symbol> openNamespaces, MMap<object, object> scopedProperties, MMap<Symbol, InternalList<InternalMacroInfo>> macros, MacroProcessorTask task)
			{
				_openNamespaces = openNamespaces;
				_openNamespaces.Add(null);
				_scopedProperties = scopedProperties;
				_macros = macros;
				_task = task;
				_modified = false;
			}

			internal MSet<Symbol> _openNamespaces; // ALWAYS includes null. Won't change unless _modified
			MacroProcessorTask _task;
			MMap<object, object> _scopedProperties;
			// A key of null is used for macros that match literals and macros that match every call
			internal MMap<Symbol, InternalList<InternalMacroInfo>> _macros;
			
			bool _modified; // copy-on-write behavior occurs when this is false

			public void BeforeModify()
			{
				if (!_modified) {
					_modified = true;
					_openNamespaces = _openNamespaces.Clone();
					_scopedProperties = _scopedProperties.Clone();
					_macros = _macros.Clone();
				}
			}
			public Scope Clone()
			{
				return new Scope(_openNamespaces, _scopedProperties, _macros, _task);
			}

			#region IMacroContext implementation

			IDictionary<object, object> IMacroContext.ScopedProperties { get { return ScopedProperties; } }
			public MMap<object, object> ScopedProperties
			{
				get {
					BeforeModify();
					return _scopedProperties;
				}
			}

			public IListSource<LNode> PreviousSiblings
			{
				get => _task._ancestorStack.Last.A;
			}
			public IListSource<LNode> Ancestors
			{
				get => _task._ancestorStack.Select(p => p.B);
			}
			public IListSource<Pair<IListSource<LNode>, LNode>> AncestorsAndPreviousSiblings
			{
				get => _task._ancestorStack.Select(p => new Pair<IListSource<LNode>, LNode>(p.A, p.B));
			}

			public LNode Parent
			{
				get { var st = _task._ancestorStack; return st[st.Count - 2, default].B; }
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
							(info, ns) => new MacroInfo(ns.A, info.Attr, info.Macro)));
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

			public ICollection<Symbol> OpenMacroNamespaces
			{
				get {
					BeforeModify();
					return _openNamespaces;
				}
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
			public List<QualifiedMacroInfo> FoundMacros = new List<QualifiedMacroInfo>();
			public MessageHolder MessageHolder = new MessageHolder();
			public InternalList<MacroResult> Results = InternalList<MacroResult>.Empty;

			#region RemainingNodes property and the NodeQueue

			// These fields support the IMacroContext.RemainingNodes property.
			// OldAndRemainingNodes contains (A) old nodes that been processed, 
			// (B) the old version of the current node that is being processed now,
			// and (C) remaining nodes. Only (C) matters, but we keep the whole 
			// list here to avoid computing a separate sublist for each iteration 
			// through a given LNodeList.
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
			public LNodeList OldAndRemainingNodes;
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
			public LNode EnqueueSplice(LNode splice, int maxExpansionsInner, int maxExpansions)
			{
				Debug.Assert(splice.Calls(S.Splice));
				//Debug.Assert(!IsTarget); should be true except that IsTarget may not have been set yet
				MaybeCreateNodeQueue(maxExpansions, ref NodeQueue);

				// Enqueue spliced items
				var items = splice.Args.IncludingAttributes(splice.Attrs);
				foreach (var item in items.ToFVList())
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

		private void GetApplicableMacros(LNode curNode, List<QualifiedMacroInfo> foundMacros)
		{
			LNode target;

			var kind = curNode.Kind;
			if (kind == LNodeKind.Call)
			{
				if (curNode.HasSimpleHead())
				{
					GetApplicableMacros2(_curScope._openNamespaces, curNode.Name, false, foundMacros);
				}
				else // complex call
				{
					if ((target = curNode.Target).Calls(S.Dot, 2) && target.Args[1].IsId)
					{
						Symbol name = target.Args[1].Name;
						if (_macros.ContainsKey(name))
						{
							Symbol @namespace = NamespaceToSymbol(target.Args[0]);
							GetApplicableMacros2(ListExt.Single(@namespace), name, false, foundMacros);
						}
					}
				}

				GetApplicableMacros2(_curScope._openNamespaces, MacroProcessor.MatchEveryCall, false, foundMacros);
			}
			else if (kind == LNodeKind.Id)
			{
				GetApplicableMacros2(_curScope._openNamespaces, curNode.Name, true, foundMacros);

				GetApplicableMacros2(_curScope._openNamespaces, MacroProcessor.MatchEveryIdentifier, false, foundMacros);
			}
			else // LNodeKind.Literal
			{
				GetApplicableMacros2(_curScope._openNamespaces, MacroProcessor.MatchEveryLiteral, false, foundMacros);
			}
		}

		public void GetApplicableMacros2(IReadOnlyCollection<Symbol> openNamespaces, Symbol name, bool isIdentifier, ICollection<QualifiedMacroInfo> found)
		{
			if (_curScope._macros.TryGetValue(name, out var candidates)) {
				foreach (var info in candidates) {
					if (isIdentifier == ((info.Attr.Mode & MacroMode.MatchIdentifierOnly) != 0)
					    || (info.Attr.Mode & MacroMode.MatchIdentifierOrCall) != 0)
					{
						bool isDeprecated = true;
						Symbol viaNamespace = null;
						foreach (var ns in info.Namespaces)
							if (openNamespaces.Contains(ns.A)) {
								viaNamespace = ns.A ?? GSymbol.Empty;
								if (!(isDeprecated &= ns.B))
									break;
							}
						if (viaNamespace != null)
							found.Add(new QualifiedMacroInfo(info, viaNamespace, isDeprecated));
					}
				}
			}
		}

		#endregion

		#region Built-in commands
		// These aren't really macros, but they are installed like macros so that
		// no extra overhead is required to detect them.

		static readonly LNodeFactory F = new LNodeFactory(EmptySourceFile.Synthetic);

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
				_curScope.OpenMacroNamespaces.Add(namespaceSym);
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
				if (!_curScope.OpenMacroNamespaces.Remove(sym))
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
		/// <remarks>EnqueueSplice is used if the input is #splice(...), but this
		/// function doesn't notice if the output is #splice().</remarks>
		LNode ApplyMacros(LNode input, LNodeList previousSiblings, int maxExpansions, bool isTargetNode, bool isSingleNode, ref DList<Pair<LNode, int>> nodeQueue)
		{
			_s.NodeQueue = nodeQueue;
			int maxExpansionsBefore = maxExpansions;
			LNode resultNode = null;
			for(LNode curNode = input; maxExpansions > 0; curNode = resultNode)
			{
				// If #splice, expand it
				if (!isSingleNode && curNode.Calls(S.Splice)) {
					if (curNode.ArgCount == 0)
						return curNode; // empty #splice()
					curNode = resultNode = _s.EnqueueSplice(curNode, maxExpansions, maxExpansionsBefore);
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
				_ancestorStack.PushLast(Pair.Create(previousSiblings, curNode));
				try {
					if (_s.FoundMacros.Count == 0) {
						nodeQueue = _s.NodeQueue;
						bool skipTarget = curNode.HasSimpleHeadWithoutPAttrs()
							&& !_curScope._macros.ContainsKey(curNode.Name)
							&& !_curScope._macros.ContainsKey(MacroProcessor.MatchEveryIdentifier);
						return ApplyMacrosToChildrenOf(curNode, maxExpansions, skipTarget) ?? resultNode;
					}

					// USER MACROS RUN HERE!
					var result_ = ApplyMacrosFound(_s);
					if (result_ == null) {
						// Macro(s) had no effect (not in this iteration, anyway), 
						// so move on to processing children.
						nodeQueue = _s.NodeQueue;
						return _s.Preprocessed ?? ApplyMacrosToChildrenOf(curNode, maxExpansions, false) ?? resultNode;
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
					// Apply macros to the output of the previous macro.
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

		private MacroResult? ApplyMacrosFound(CurNodeState s)
		{
			var foundMacros = s.FoundMacros;
			// if any of the macros use a priority flag, group by priority.
			if (foundMacros.Count > 1) {
				var p = foundMacros[0].IMI.Attr.Priority;
				for (int x = 1; x < foundMacros.Count; x++) {
					if (foundMacros[x].IMI.Attr.Priority != p) {
						// need to make an independent list because _s.foundMacros may be cleared and re-used for descendant nodes
						foundMacros = new List<QualifiedMacroInfo>(foundMacros);
						foundMacros.Sort(QualifiedMacroInfo.CompareDescendingByPriority); // descending by priority
						for (int i = 0, j; i < foundMacros.Count; i = j) {
							p = foundMacros[i].IMI.Attr.Priority;
							for (j = i + 1; j < foundMacros.Count; j++)
								if (foundMacros[j].IMI.Attr.Priority != p)
									break;
							var newNode = ApplyMacrosFound2(s, ((IList<QualifiedMacroInfo>)foundMacros).Slice(i, j - i));
							if (newNode != null)
								return newNode;
						}
						return null;
					}
				}
			}
			return ApplyMacrosFound2(s, ((IList<QualifiedMacroInfo>)foundMacros).Slice(0));
		}

		private MacroResult? ApplyMacrosFound2(CurNodeState s, ListSlice<QualifiedMacroInfo> foundMacros)
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
					output = macro.IMI.Macro(macroInput, scope);
					
					if (output != null) {
						accepted++;
						if (acceptedIndex <= -1 || !macro.IsDeprecated)
							acceptedIndex = i;
					}
				} catch (ThreadAbortException e) {
					_sink.Write(Severity.Error, "Macro-processing thread aborted in {0}", QualifiedNameOf(macro));
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

			PrintMessages(s.Results, input, accepted,
				s.MessageHolder.List.MaxItemOrDefault(msg => (int)msg.Severity).Severity);

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
					Debug.Assert(s.Input == _ancestorStack.Last.B);
					s.Preprocessed = ApplyMacrosToChildrenOf(s.Input, s.MaxExpansions - 1) ?? s.Input;
				} finally {
					_s = s;
				}
			}
			return _s.Preprocessed;
		}

		struct MacroResult
		{
			public MacroResult(QualifiedMacroInfo macro, LNode newNode, ListSlice<LogMessage> msgs, bool dropRemaining)
			{
				Macro = macro; NewNode = newNode; Msgs = msgs; DropRemainingNodes = dropRemaining;
			}
			public QualifiedMacroInfo Macro; 
			public LNode NewNode;
			public ListSlice<LogMessage> Msgs;
			public bool DropRemainingNodes; // delete rest of nodes in current Args/Attrs list

			public MacroResult MaybeWith(LNode newNode)
			{
				return new MacroResult(Macro, newNode ?? NewNode, Msgs, DropRemainingNodes);
			}
		}

		LNodeList ApplyMacrosToList(LNodeList list, int maxExpansions, bool areAttributes)
		{
			DList<Pair<LNode, int>> nodeQueue = null;
			LNodeList results;
			LNode result = null;
			LNode emptySplice = null; // an empty #splice() may have trivia we need to preserve
			
			// Share as much of the original LNodeList as is left unchanged
			for (int i = 0, count = list.Count;; ++i)
			{
				if (i >= count)
					return list; // Entire list did not change

				_s.StartListItem(list, i, areAttributes);
				LNode input = list[i];
				results = list.Initial(i);
				result = ApplyMacros(input, results, maxExpansions, false, false, ref nodeQueue);
				if (result != null)
				{
					Add(ref results, ref emptySplice, result);
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
				if (emptySplice != null) {
					input2.A = input2.A.PlusAttrsBefore(TriviaFromEmptySplice(emptySplice));
					emptySplice = null;
				}
				result = ApplyMacros(input2.A, results, input2.B, false, false, ref nodeQueue);
				Add(ref results, ref emptySplice, result ?? input2.A);
			}
			_s.NodeQueue = null;

			// If final result was empty splice, transfer its trivia to remaining nodes, or put
			// it in results if results.IsEmpty, in which case the caller should deal with it.
			// Sometimes the caller can't deal with it (e.g. top level) and the final output
			// will contain an empty #splice() with trivia.
			LNodeList trivia;
			if (emptySplice != null && !(trivia = TriviaFromEmptySplice(emptySplice)).IsEmpty) {
				if (results.IsEmpty)
					results.Add(emptySplice);
				else
					results[results.Count - 1] = results.Last.PlusTrailingTrivia(trivia);
			}
			return results;
		}
		private void Add(ref LNodeList results, ref LNode emptySplice, LNode result)
		{
			if (result.Calls(S.Splice)) {
				if (result.ArgCount == 0)
					emptySplice = result;
				else
					results.AddRange(result.Unsplice());
			} else
				results.Add(result);
		}
		private LNodeList TriviaFromEmptySplice(LNode emptySplice)
		{
			var trivia = emptySplice.GetTrivia().WithoutTrailingTrivia(out var trailing);
			if (!trailing.IsEmpty)
				trivia.AddRange(trailing);
			return trivia;
		}

		LNode ApplyMacrosToChildrenOf(LNode node, int maxExpansions, bool skipTarget = false)
		{
			if (maxExpansions <= 0)
				return null;

			bool changed = false;
			
			// Process attributes
			LNodeList old;
			var newAttrs = ApplyMacrosToList(old = node.Attrs, maxExpansions, true);
			if (newAttrs.Count == 1 && newAttrs.Last.Calls(S.Splice, 0))
				newAttrs = TriviaFromEmptySplice(newAttrs.Last);

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
					LNode newTarget = ApplyMacros(target, LNode.List(), maxExpansions, true, true, ref _);
					if (newTarget != null)
					{
						node = node.WithTarget(newTarget.Unwrap(S.Splice));
						changed = true;
					}
				}
			}

			LNodeList newArgs = ApplyMacrosToList(old = node.Args, maxExpansions, false);
			if (newArgs != old)
			{
				if (newArgs.Count == 1 && newArgs.Last.Calls(S.Splice, 0)) {
					var trivia = TriviaFromEmptySplice(newArgs[0]);
					node = node.WithArgs().PlusTrailingTrivia(trivia);
				} else
					node = node.WithArgs(newArgs);
				changed = true;
			}

			return changed ? node : null;
		}

		void PrintMessages(InternalList<MacroResult> results, LNode input, int accepted, Severity maxSeverity)
		{
			bool blockDeprecationWarning = false;
			if (accepted > 1) {
				// Multiple macros accepted the input. If AllowDuplicates is used, 
				// this is fine if as long as they produced the same result.
				var relevantResults = new InternalList<MacroResult>(results.Where(r => r.NewNode != null));
				bool allowed, equal = AreAllOutcomesEqual(relevantResults, out allowed);
				if (!equal || !allowed)
				{
					string list = relevantResults.Select(r => QualifiedNameOf(r.Macro)).Join(", ");
					if (equal)
						_sink.Warning(input, "Ambiguous macro call. {0} macros accepted the input and produced equal results: {1}", accepted, list);
					else
						_sink.Error(input, "Ambiguous macro call. {0} macros accepted the input and produced different results: {1}", accepted, list);
				}
				blockDeprecationWarning = !relevantResults.All(r => r.Macro.IsDeprecated);
			}

			bool macroStyleCall = input.BaseStyle == NodeStyle.Special;
			var passive = MacroMode.Passive | MacroMode.MatchEveryCall | MacroMode.MatchEveryLiteral | MacroMode.MatchEveryIdentifier;
			var rejected = results.Where(r => r.NewNode == null && (r.Macro.Mode & passive) == 0);
			if (macroStyleCall && maxSeverity < Severity.Warning)
				maxSeverity = Severity.Warning;
			if (maxSeverity < Severity.Note)
				maxSeverity = Severity.Note;
			if (accepted == 0 && input.IsCall && _sink.IsEnabled(maxSeverity) && rejected.Any(r => r.Msgs.Count == 0))
			{
				_sink.Write(maxSeverity, input, "{0} macro(s) saw the input and declined to process it: {1}", 
					results.Count, rejected.Select(r => QualifiedNameOf(r.Macro)).Join(", "));
			}
			
			foreach (var result in results)
			{
				var severityRequired = Severity.Warning;
				if (result.NewNode != null) // Show all messages
					severityRequired = default(Severity);
				if (accepted == 0) // Show info from rejecting macros
					severityRequired = macroStyleCall ? Severity.InfoDetail : Severity.NoteDetail;

				foreach (var msg in result.Msgs) {
					if (_sink.IsEnabled(msg.Severity) && msg.Severity >= severityRequired)
					{
						var msg2 = new LogMessage(msg.Severity, msg.Context,
							QualifiedNameOf(result.Macro) + ": " + msg.Format, msg.Args);
						msg2.WriteTo(_sink);
					}
				}

				// Report deprecation with alternate name if available
				if (result.NewNode != null && result.Macro.IsDeprecated && !blockDeprecationWarning) {
					var qName = QualifiedNameOf(result.Macro, true);
					var imi = result.Macro.IMI;
					
					if (!string.IsNullOrEmpty(imi.Attr.DeprecationMessage)) {
						_sink.Warning(input, "{0} is deprecated: {1}.", qName, imi.Attr.DeprecationMessage);
					} else {
						Symbol acceptableNamespace = imi.Namespaces.Where(p => !p.B).FirstOrDefault().A;
						if (acceptableNamespace != null) {
							_sink.Warning(input,
								"{0} is deprecated in this namespace. Please import {1}.",
								qName, acceptableNamespace);
						} else {
							string newName = string.Join(", ", new Set<string>(imi.Attr.Names).Except(imi.Attr.DeprecatedNames));
							_sink.Warning(input, "{0} is deprecated. {1}", qName,
								newName != "" ? "Please use the new name: {0}".Localized(newName) : "");
						}
					}
				}
			}
		}

		private static bool AreAllOutcomesEqual(InternalList<MacroResult> results, out bool allowed)
		{
			allowed = false;
			LNode firstResult = default;
			int resultCount = 0, allowDuplicateFlags = 0;
			for (int i = 0; i < results.Count; i++) {
				MacroResult r = results[i];
				resultCount++;
				if ((r.Macro.Mode & MacroMode.AllowDuplicates) != 0)
					allowDuplicateFlags++;
				if (firstResult == null)
					firstResult = r.NewNode;
				else {
					if (!r.NewNode.Equals(firstResult, LNode.CompareMode.IgnoreTrivia))
						return false;
				}
			}
			allowed = allowDuplicateFlags > 0;
			return true;
		}

		private string QualifiedNameOf(QualifiedMacroInfo macro, bool forceLogicalName = false)
		{
			if (forceLogicalName || (macro.Mode & MacroMode.UseLogicalNameInErrorMessages) != 0) {
				if (GSymbol.IsNullOrEmpty(macro.Namespace))
					return macro.IMI.Name.Name;
				return string.Format("{0}.{1}", macro.Namespace, macro.IMI.Name);
			} else {
				var method = macro.IMI.Macro.Method;
				return string.Format("{0}.{1}.{2}", method.DeclaringType.Namespace, method.DeclaringType.Name, method.Name).WithoutPrefix(".");
			}
		}

		#endregion
	}
}
