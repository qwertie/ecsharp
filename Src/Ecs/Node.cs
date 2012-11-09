using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Essentials;
using Loyc.CompilerCore;
using Loyc.Collections;
using Loyc.Utilities;

namespace ecs
{
	using F = CodeFactory;
	using SourceLocation = String;

	class CompilerMessage : Exception
	{
		public CompilerMessage(SourceLocation location, string msg) : base(Format(location, msg)) { }
		public CompilerMessage(SourceLocation location, string msg, Exception innerException) : base(Format(location, msg), innerException) { }
		public CompilerMessage(string msg) : base(msg) { }
		public CompilerMessage(string msg, Exception innerException) : base(msg, innerException) { }
		protected static string Format(SourceLocation location, string msg)
		{
			return string.Format("{0}: {1}", location ?? node.UnknownLocation, Localize.From(msg));
		}
	}
	class CompilerWarning : CompilerMessage
	{
		public CompilerWarning(SourceLocation location, string msg) : base(location, msg) { }
		public CompilerWarning(SourceLocation location, string msg, Exception innerException) : base(location, msg, innerException) { }
		public CompilerWarning(string msg) : base(msg) { }
		public CompilerWarning(string msg, Exception innerException) : base(msg, innerException) { }
	}
	class CompilerError : CompilerMessage
	{
		public CompilerError(SourceLocation location, string msg) : base(location, msg) { }
		public CompilerError(SourceLocation location, string msg, Exception innerException) : base(location, msg, innerException) { }
		public CompilerError(string msg) : base(msg) { }
		public CompilerError(string msg, Exception innerException) : base(msg, innerException) { }
	}
	class InternalCompilerError : CompilerError
	{
		public InternalCompilerError(SourceLocation location) : base(Format(location, "")) { }
		public InternalCompilerError(SourceLocation location, string msg) : base(Format(location, msg)) { }
		public InternalCompilerError(SourceLocation location, Exception innerException) : base(Format(location, ""), innerException) { }
		public InternalCompilerError(SourceLocation location, string msg, Exception innerException) : base(Format(location, msg), innerException) { }
		static new string Format(SourceLocation location, string msg)
		{
			return Localize.From("{0}: Internal compiler error. {1}", location ?? node.UnknownLocation, Localize.From(msg));
		}
	}
	
	// LISP style possibility
	public abstract partial class node : IListSource<node>
	{
		public const SourceLocation UnknownLocation = "Unknown:0";

		#region Construction

		public static readonly node[] EmptyArray = new node[0];

		public static node New(string name, SourceLocation location = null)
		{
			return new SymbolNode(GSymbol.Get(name), null);
		}
		public static node New(Symbol name, SourceLocation location = null)
		{
			return new SymbolNode(name, location);
		}
		public static node New(node _0, node _1, SourceLocation location = null)
		{
			return new ListNode(new node[] { _0, _1 }, location);
		}
		public static node New(node _0, node _1, node _2, SourceLocation location = null)
		{
			return new ListNode(new node[] { _0, _1, _2 }, location);
		}
		public static node New(params node[] nodes)
		{
			return new ListNode(nodes, null);
		}
		public static node New(node[] nodes, SourceLocation location)
		{
			return new ListNode(nodes, location);
		}
		public static node New(Symbol name, node _1, SourceLocation location = null)
		{
			return new ListNode(new node[] { New(name, location), _1 }, location);
		}
		public static node New(Symbol name, node _1, node _2, SourceLocation location = null)
		{
			return new ListNode(new node[] { New(name, location), _1, _2 }, location);
		}
		public static node New(Symbol name, node _1, node _2, node _3, SourceLocation location = null)
		{
			return new ListNode(new node[] { New(name, location), _1, _2, _3 }, location);
		}
		public static node New(Symbol name, node _1, node _2, node _3, node _4, SourceLocation location = null)
		{
			return new ListNode(new node[] { New(name, location), _1, _2, _3, _4 }, location);
		}
		public static node New(Symbol name, params node[] nodes)
		{
			node[] nodes2 = new node[nodes.Length+1];
			nodes2[0] = New(name, (SourceLocation)null);
			for (int i = 0; i < nodes.Length; i++)
				nodes2[i+1]=nodes[i];
			return New(nodes);
		}
		public static node NewLiteral(object value, SourceLocation location = null)
		{
			return new LiteralNode(value, location);
		}

		#endregion

		protected node(SourceLocation location) { Location = location ?? UnknownLocation; }
		protected Symbol _name;
		public Symbol Name { get { return _name; } } // If IsList, returns the Kind of the first item
		public virtual Symbol Kind { get { return Name; } } // #list for a list, Name otherwise
		public virtual object LiteralValue { get { return null; } }
		public SourceLocation Location { get; protected set; }
		public virtual node SkipAttrs { get { return this; } }

		public bool IsList { get { return Count >= 0; } }
		public bool IsLiteral { get { return Kind == F._Literal; } }
		public bool IsSymbol { get { return this is SymbolNode; } } // IsSymbol => IsIdent or IsKeyword
		public bool IsIdent { get { return Kind.Name[0] != '#'; } }
		public bool IsKeyword { get { return Kind.Name[0] == '#'; } }
		public bool NameIsKeyword { get { return Name.Name[0] == '#'; } }
		public bool Calls(Symbol name, int argCount) { return Name == name && Count == argCount + 1; }
		public bool CallsMin(Symbol name, int argCount) { return Name == name && Count >= argCount + 1; }
		
		// List access
		public abstract int Count { get; } // -1 if not a list
		public abstract node this[int index] { get; }
		public abstract node this[int index, node @default] { get; }
		public abstract IEnumerator<node> GetEnumerator();
		public Iterator<node> GetIterator() { return GetEnumerator().AsIterator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		public node TryGet(int index, ref bool fail) {
			node result = this[index, null];
			if (result == null) fail = true;
			return result;
		}

		public node Head { get { return this[0]; } }
		public IListSource<node> Args { get { return this.Slice(1); } }

		public virtual node AddArg(node value)
		{
			throw new InternalCompilerError(Localize.From("{0}: A list or call was expected here", Location));
		}
	}

	public class TerminalNode : node
	{
		public TerminalNode(SourceLocation location) : base(location) { }
		public override int Count { get { return -1; } }
		public override node this[int index] { get {
			throw new IndexOutOfRangeException(Localize.From(
				"{0}: Error: expected a list, got '{1}' node", Location, Name));
		} }
		public override node this[int index, node @default] { get { return @default; } }
		public override IEnumerator<node> GetEnumerator() { return EmptyEnumerator<node>.Value; }
	}
	public sealed class SymbolNode : TerminalNode
	{
		public SymbolNode(Symbol name, SourceLocation location) : base(location)
		{
			_name = name;
			if (_name == null || _name.Name.Length == 0)
				throw new ArgumentException(Localize.From(
					"{0}: Error: a null or zero-length name symbol is not allowed in a syntax tree.", location));
		}
		public override object LiteralValue { get { return null; } }
	}
	public sealed class LiteralNode : TerminalNode
	{
		public LiteralNode(object value, SourceLocation location) : base(location)
		{
			_value = value;
			_name = F._Literal;
		}
		object _value;
		public override object LiteralValue { get { return _value; } }
	}
	public sealed class ListNode : node // TODO: optimize for common amounts of list items
	{
		public ListNode(node[] list, SourceLocation location) : base(location)
		{
			if (list.Length == 0) throw new InternalCompilerError(location, 
				"Creating an empty list node is not allowed (so that you can always safely get the first element). By convention, an empty list is represented with #(), which is a single-element list with a child named $'#'");
			_list = list;
			_name = _list[0].Kind;
			
			//// Conservative checks on special constructs: check only the number of args
			//if (_name.Name.Length != 0 && _name.Name[0] == '#') {
			//    if (_name == F._Var) // e.g. #var(int, i(0), j)
			//        G.Require(_list.Length >= 3, "List for #var requires 2+ args"));
			//    if (_name == F._Last || _name == F._Label)
			//        G.Require(_list.Length >= 2, "List for #last or #label requires 1+ args");
			//    if (_name == F._If || _name == F._NamedArg)
			//        G.Require(_list.Length >= 3, "List for #if or #namedarg requires 2+ args");
			//    if (_name == F._Def) // e.g. 
			//        G.Require(_list.Length >= 3, "List for #if or #namedarg requires 2+ args");
			//}
		}
		public node[] _list; // never null
	
		public override Symbol Kind { get { return F._ListKind; } }
		public override node SkipAttrs
		{ 
			get { return _name == F._Attr && _list.Length > 1 ? _list[1].SkipAttrs : this; }
		}

		public override int Count
		{
			get { return _list.Length; }
		}
		public override node this[int index]
		{
			get {
				try {
					return _list[index];
				} catch(IndexOutOfRangeException) {
					// Dress it up in the form of a compiler error
					throw new IndexOutOfRangeException(Localize.From(
						"{0}: Error: Argument {1} not found in '{2}' node", Location, index, Name));
				}
			}
		}
		public override node this[int index, node @default]
		{
			get { 
				if ((uint)index < (uint)_list.Length)
					return _list[index];
				return @default;
			}
		}
		public override IEnumerator<node> GetEnumerator()
		{
 			return (_list as IEnumerable<node>).GetEnumerator();
		}

		public sealed override node AddArg(node value)
		{
			G.Require(IsList, Localize.From("{0}: A list or call was expected here", Location));
			
			var list = _list;
			var newList = new node[list.Length+1];
			for (int i = 0; i < list.Length; i++)
				newList[i] = list[i];
			newList[list.Length] = value;
			return New(newList, Location);
		}

		public override string ToString()
		{
			return new NodePrinter(this, 0).PrintStmts(false, false).Result();
		}
	}
	
	/*public partial class Node : IListSource<Node>
	{
		#region Construction

		public static readonly Node[] EmptyArray = new Node[0];
		public static Node New(string name)
		{
			return New(GSymbol.Get(name), (Node[])null);
		}
		public static Node New(string name, params Node[] args)
		{
			return New(GSymbol.Get(name), args);
		}
		public static Node New(Symbol name, object value = null, Node basis = null, string location = null)
		{
			return New(name, (Node[])null, value, basis, location);
		}
		public static Node New(Symbol name, Node _1, object value = null, Node basis = null, string location = null)
		{
			return New(name, new[] { _1 }, value, basis, location);
		}
		public static Node New(Symbol name, Node _1, Node _2, object value = null, Node basis = null, string location = null)
		{
			return New(name, new[] { _1, _2 }, value, basis, location);
		}
		public static Node New(Symbol name, Node _1, Node _2, Node _3, object value = null, Node basis = null, string location = null)
		{
			return New(name, new[] { _1, _2, _3 }, value, basis, location);
		}
		public static Node New(Symbol name, Node _1, Node _2, Node _3, Node _4, object value = null, Node basis = null, string location = null)
		{
			return New(name, new[] { _1, _2, _3, _4 }, value, basis, location);
		}
		public static Node New(Symbol name, Node _1, Node _2, Node _3, Node _4, Node _5, object value = null, Node basis = null, string location = null)
		{
			return New(name, new[] { _1, _2, _3, _4, _5 }, value, basis, location);
		}
		public static Node New(Symbol name, Node[] args, object value = null, Node basis = null, string location = null)
		{
			return new Node(name, args, value, basis, location);
		}
		private Node(Symbol name, Node[] args, object value, Node basis, string location)
		{
			Name = name; _args = args; Value = value;
			Location = location ?? (basis != null ? basis.Location : "Unknown:0");
		}

		#endregion

		public Symbol Name { get; private set; }
		public Node[] _args; // null if no argument list
		public string Location { get; private set; }
		public object Value { get; private set; }
		public Node Head { get { return Value as Node; } }
		// TODO: Trivia (syntactic style, whitespace, comments, analysis results...)

		public Node this[int index]
		{
			get {
				Node h;
				if (index == -1 && (h=Head) != null)
					return h;
				return _args[index];
			}
		}
		public Node this[int index, Node @default]
		{
			get {
				if ((uint)index < (uint)_args.Length)
					return _args[index];
				return (index == -1 ? Head ?? @default : @default);
			}
		}
		public Node TryGet(int index, ref bool fail)
		{
			Node n = this[index, null];
			if (n == null)
				fail = true;
			return n;
		}
		public int Count
		{
			get { return _args != null ? _args.Length : -1; }
		}
		public Iterator<Node> GetIterator()
		{
 			return GetEnumerator().AsIterator();
		}
		public IEnumerator<Node> GetEnumerator()
		{
 			if (_args != null)
				return (_args as IEnumerable<Node>).GetEnumerator();
			return EmptyEnumerator<Node>.Value;
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
 			return GetEnumerator();
		}

		// Operators are standing by
		public static Node operator-(Node a) { return New(F._Sub, a); }
		public static Node operator-(Node a, Node b) { return New(F._Sub, a, b); }
		public static Node operator+(Node a) { return New(F._Add, a); }
		public static Node operator+(Node a, Node b) { return New(F._Add, a, b); }
		public static Node operator*(Node a, Node b) { return New(F._Mul, a); }
		public static Node operator/(Node a, Node b) { return New(F._Div, a); }
		public static Node operator%(Node a, Node b) { return New(F._Mod, a, b); }
		public static Node operator|(Node a, Node b) { return New(F._OrBits, a, b); }
		public static Node operator&(Node a, Node b) { return New(F._AndBits, a, b); }
		public static Node operator^(Node a, Node b) { return New(F._XorBits, a, b); }
		public static Node operator>(Node a, Node b) { return New(F._GT, a, b); }
		public static Node operator<(Node a, Node b) { return New(F._LT, a, b); }
		public static Node operator>=(Node a, Node b) { return New(F._GE, a, b); }
		public static Node operator<=(Node a, Node b) { return New(F._LE, a, b); }
		public static Node operator>>(Node a, Node b) { return New(F._Shr, a, b); }
		public static Node operator<<(Node a, Node b) { return New(F._Shl, a, b); }
		public static Node operator!(Node a) { return New(F._Not, a); }
		public static Node operator~(Node a) { return New(F._NotBits, a); }
		public static bool operator true(Node a) { return a != null; }
		public static bool operator false(Node a) { return a == null; }

		public bool Calls(Symbol name, int count)
		{
			return Name == name && Count == count;
		}
		public Node AddArg(Node value)
		{
			var args = _args ?? EmptyArray;
			var newArgs = new Node[args.Length+1];
			for (int i = 0; i < args.Length; i++)
				newArgs[i] = args[i];
			newArgs[args.Length] = value;
			return New(Name, newArgs, value, this);
		}
		public Node AddNamedArg(Symbol name, Node value)
		{
			return AddArg(New(F._NamedArg, New(name, value)));
		}
		public Node With(Symbol name = null, Node _1 = null, Node _2 = null, Node _3 = null, Node _4 = null, Node _5 = null, object value = null)
		{
			name = name ?? Name;
			value = value ?? Value;
			var args = _args ?? EmptyArray;

			if (args.Length > 0) {
				if (args.Length > 1) {
					if (args.Length > 2) {
						if (args.Length > 3) {
							if (args.Length > 4)
								_5 = _5 ?? args[4];
							_4 = _4 ?? args[3];
						}
						_3 = _3 ?? args[2];
					}
					_2 = _2 ?? args[1];
				}
				_1 = _1 ?? args[0];
			}
			if (_1 != null) {
				if (_2 != null) {
					if (_3 != null) {
						if (_4 != null) {
							if (_5 != null) {
								if (args.Length > 5)
									return New(name, new Node[] { _1, _2, _3, _4, _5 }.Concat(args.Slice(), value)
								return New(name, _1, _2, _3, _4, _5, value, this, Location);
							}
							return New(name, _1, _2, _3, _4, value, this, Location);
						}
						return New(name, _1, _2, _3, value, this, Location);
					}
					return New(name, _1, _2, value, this, Location);
				}
				return New(name, _1, value, this, Location);
			} else {
				// No new args were provided
				if (_args == null)
					return New(name, value);
				else
					return New(name, args);
			}
		}

		public override string ToString()
		{
			return new NodePrinter(this, 0).PrintStmts(false, false).Result();
		}
	}*/

	// Suggested symbols for EC# node names
	// - $'.' as the dot operator, but it's usually written A.B.C instead of #.(A, B, C)
	// - $'#invoke' as the delegate call operator, WHEN THE LEFT HAND SIDE IS AN EXPRESSION.
	//   i.e. f(x) is stored exactly that way, but f(x)(y) is stored as #invoke(f(x), (y))
	// - $'#' holds a list of expressions. If executed, the last expr gives the overall value,
	//   like LISP's progn).
	// - $'#{}' for explicitly-represented code blocks. This different than $'', in that it 
	//   introduces a new scope.
	// - $'#[]' for the indexing operator (as a tag, it holds one or more attributes)
	// - $'#<>' to represent an identifier with type arguments
	// - $'#tuple' for tuples.
	// - $'#literal' for all primitive literals including symbols 
	//   (Value == null for null, otherwise Value.GetType() to find out what kind)
	// - Data type names: $'int' for the int keyword, $'double' for the double keyword, etc.
	// - $'#$' for the list-count symbol "$"
	// - $'#\\' (that's a single backslash) for the substitution operator.
	// - $'#::' for the binding operator
	// - $'#var' for other variable declarations: int i=0, j; ==> #var(int, i(0), j)
	// - $'#def' for methods: [Test] void F([required] List<int> list) { return; }
	//   ==> #def(F, #(#var(#<>(List, int), list, #[]: required)), void, #(return)))
	// A.B(arg)       ==> #.(A, B(arg))       ==> #.(A, #(B, arg))
	// A.B(arg)(parm) ==> #.(A, B(arg)(parm)) ==> #.(A, #(#(B, arg), parm)
}
