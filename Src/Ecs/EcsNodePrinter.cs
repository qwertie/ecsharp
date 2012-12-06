using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Utilities;
using Loyc.Essentials;
using System.Diagnostics;
using System.IO;
using Loyc.Math;
using Loyc.CompilerCore;
using S = Loyc.CompilerCore.CodeSymbols;
using EP = ecs.EcsPrecedence;

namespace ecs
{
	/// <summary>This interface is implemented by helper objects that handle the 
	/// low-level details of node printing. It is used by <see cref="EcsNodePrinter"/>.</summary>
	public interface INodePrinterWriter
	{
		void Write(char c, bool finishToken);
		void Write(string s, bool finishToken);
		int Indent();
		int Dedent();
		void Space();
		void Newline();
		void BeginStatement();
		void Push(INodeReader newNode);
		void Pop(INodeReader oldNode);
	}

	public class SimpleNodePrinterWriter : INodePrinterWriter
	{
		int _indentLevel;
		string _indentString;
		string _lineSeparator;
		char _lastCh = '\n';
		bool _startingToken = true;
		TextWriter _out;

		public SimpleNodePrinterWriter(StringBuilder sb, string indentString = "\t", string lineSeparator = "\n") : this(new StringWriter(sb), indentString, lineSeparator) { }
		public SimpleNodePrinterWriter(TextWriter @out, string indentString = "\t", string lineSeparator = "\n")
		{
			_indentString = indentString;
			_lineSeparator = lineSeparator;
			_out = @out;
			_indentLevel = 0;
		}

		public void Write(char c, bool finishToken)
		{
			if (_startingToken)
				StartToken(c);
			_out.Write(c);
			if (finishToken) FinishToken(_lastCh = c);
			_startingToken = finishToken;
		}

		public void Write(string s, bool finishToken)
		{
			if (s != "") {
				if (_startingToken)
					StartToken(s[s.Length-1]);
				_out.Write(s);
				if (finishToken) FinishToken(_lastCh = s[s.Length-1]);
			} else if (finishToken)
				FinishToken(_lastCh);
			_startingToken = finishToken;
		}

		void FinishToken(char lastCh)
		{
			if (lastCh == ',')
				_out.Write(' ');
		}
		void StartToken(char nextCh)
		{
			if ((EcsNodePrinter.IsIdentContChar(_lastCh) || _lastCh == '#')
				&& (EcsNodePrinter.IsIdentContChar(nextCh) || nextCh == '@'))
				_out.Write(' ');
			else if ((_lastCh == '-' && nextCh == '-') || (_lastCh == '+' && nextCh == '+') || (_lastCh == '.' && nextCh == '.'))
				_out.Write(' ');
			else if ((_lastCh == ']' || _lastCh == ')' || _lastCh == '}') && EcsNodePrinter.IsIdentContChar(nextCh))
				_out.Write(' ');
		}

		public int Indent()
		{
			return ++_indentLevel;
		}
		public int Dedent()
		{
			return --_indentLevel;
		}
		public void Space()
		{
			Write(' ', true);
		}
		public void BeginStatement()
		{
			if (_lastCh == '\n')
				return;
			Newline();
		}
		public void Newline()
		{
			_lastCh = '\n';

			_out.Write(_lineSeparator);
			for (int i = 0; i < _indentLevel; i++)
				_out.Write(_indentString);
		}
		public void Push(INodeReader n) { }
		public void Pop(INodeReader n) { }
	}

	public class EcsNodePrinter
	{
		INodeReader _n;
		INodePrinterWriter _out;

		public EcsNodePrinter(INodeReader node, INodePrinterWriter target)
		{
			_n = node;
			_out = target;
			target.Push(node);
		}

		struct Indented_ : IDisposable
		{
			EcsNodePrinter _self;
			public Indented_(EcsNodePrinter self) { _self = self; self._out.Indent(); }
			public void Dispose() { _self._out.Dedent(); }
		}
		struct With_ : IDisposable
		{
			EcsNodePrinter _self;
			INodeReader _old;
			public With_(EcsNodePrinter self, INodeReader inner)
				{ _self = self; self._out.Push(_old = self._n); self._n = inner; }
			public void Dispose() { _self._out.Pop(_self._n = _old); }
		}

		Indented_ Indented { get { return new Indented_(this); } }

		private With_ With(INodeReader inner)
		{
			return new With_(this, inner);
		}

		#region Keyword sets and other symbol sets

		static readonly HashSet<Symbol> PreprocessorCollisions = SymbolSet(
			"#if", "#else", "#elif", "#endif", "#define", "#region", "#endregion", 
			"#pragma", "#warning", "#error"
		);

		static readonly HashSet<Symbol> TokenHashKeywords = SymbolSet(
			"#~", "#!", "#%", "#^", "#&", "#&&", "#*", "#**", "#+", "#++", 
			"#-", "#--", "#=", "#==", "#{}", "#[]", "#|", "#||", @"#\", 
			"#;", "#:", "#,", "#.", "#..", "#<", "#<<", "#>", "#>>", "#/", 
			"#?", "#??", "#??.", "#??=", "#%=", "#^=", "#&=", "#*=", "#-=", 
			"#+=", "#|=", "#<=", "#>=", "#=>", "#==>", "#->"
		);

		static readonly Dictionary<Symbol,Precedence> PrefixOperators = Dictionary( 
			// This is a list of unary prefix operators only. Does not include the
			// binary prefix operator "#cast" and "#." (the latter has #missing as
			// its first argument when used as a prefix operator, so it's binary).
			// Does not include the unary suffix operators ++ and --.
			P(S.NotBits,      EP.Prefix), P(S.Not,        EP.Prefix), P(S._AddressOf, EP.Prefix), 
			P(S._Dereference, EP.Prefix), P(S._UnaryPlus, EP.Prefix), P(S.PreInc,     EP.Prefix),
			P(S.PreDec,       EP.Prefix), P(S.Forward,    EP.Prefix), P(S.Substitute, EP.Substitute)
		);

		static readonly Dictionary<Symbol,Precedence> InfixOperators = Dictionary(
			// This is a list of infix binary opertors only. Does not include the
			// conditional operator #? or non-infix binary operators such as a[i].
			// #, is not an operator at all and generally should not occur.
			P(S._Concat, EP.Add),      P(S.Mod, EP.Multiply),  P(S.XorBits, EP.XorBits), 
			P(S.AndBits, EP.AndBits),  P(S.And, EP.And),       P(S.Mul, EP.Multiply), 
			P(S.Exp, EP.Power),        P(S.Add, EP.Add),       P(S.Sub, EP.Add),
			P(S.Set, EP.Assign),       P(S.Eq, EP.Equals),     P(S.Neq, EP.Equals),
			P(S.OrBits, EP.OrBits),    P(S.Or, EP.Or),         P(S.Dot, EP.Primary),
			P(S.DotDot, EP.Range),     P(S.LT, EP.Compare),    P(S.Shl, EP.Shift),
			P(S.GT, EP.Compare),       P(S.Shr, EP.Shift),     P(S.Div, EP.Multiply),
			P(S.MulSet, EP.Assign),    P(S.DivSet, EP.Assign), P(S.ModSet, EP.Assign),
			P(S.SubSet, EP.Assign),    P(S.AddSet, EP.Assign), P(S.ConcatSet, EP.Assign),
			P(S.ExpSet, EP.Assign),    P(S.ShlSet, EP.Assign), P(S.ShrSet, EP.Assign),
			P(S.XorBitsSet, EP.Assign), P(S.AndBitsSet, EP.Assign), P(S.OrBitsSet, EP.Assign),
			P(S.NullCoalesce, EP.OrIfNull), P(S.NullDot, EP.NullDot), P(S.NullCoalesceSet, EP.Assign),
			P(S.LE, EP.Compare),       P(S.GE, EP.Compare),    P(S._Arrow, EP.Custom),  
			P(S.PtrArrow, EP.Primary), P(S.Is, EP.Compare),    P(S.As, EP.Compare),
			P(S.UsingCast, EP.Compare)
		);

		static readonly HashSet<Symbol> SpecialCaseOperators = new HashSet<Symbol>(new[] {
			// Operators that are neither prefix nor infix, or that have an alternate 
			// form with special syntax: #? #[] #cast #as #usingCast, #postInc, #postDec.
			S.QuestionMark, S.Bracks, S.Cast, S.As, S.UsingCast, S.PostInc, S.PostDec, S.Dot
		});

		static readonly HashSet<Symbol> CsKeywords = SymbolSet(
			"abstract",  "event",     "new",        "struct", 
			"as",        "explicit",  "null",       "switch", 
			"base",      "extern",    "object",     "this", 
			"bool",      "false",     "operator",   "throw", 
			"break",     "finally",   "out",        "true", 
			"byte",      "fixed",     "override",   "try", 
			"case",      "float",     "params",     "typeof", 
			"catch",     "for",       "private",    "uint", 
			"char",      "foreach",   "protected",  "ulong", 
			"checked",   "goto",      "public",     "unchecked", 
			"class",     "if",        "readonly",   "unsafe", 
			"const",     "implicit",  "ref",        "ushort", 
			"continue",  "in",        "return",     "using", 
			"decimal",   "int",       "sbyte",      "virtual", 
			"default",   "interface", "sealed",     "volatile", 
			"delegate",  "internal",  "short",      "void", 
			"do",        "is",        "sizeof",     "while", 
			"double",    "lock",      "stackalloc",
			"else",      "long",      "static",
			"enum",      "namespace", "string");

		static readonly Dictionary<Symbol, string> AttributeKeywords = KeywordDict(
			"abstract", "const", "explicit", "extern", "implicit", "internal", "new",
			"override", "params", "private", "protected", "public", "readonly", "ref",
			"sealed", "static", "unsafe", "virtual", "volatile", "out");

		static readonly Dictionary<Symbol, string> TypeKeywords = KeywordDict(
			"bool", "byte", "char", "decimal", "double", "float", "int", "long",
			"object", "sbyte", "short", "string", "uint", "ulong", "ushort", "void");

		static readonly Dictionary<Symbol, string> KeywordStmts = KeywordDict(
			"break", "case", "checked", "continue", "default",  "do", "fixed", 
			"for", "foreach", "goto", "if", "lock", "return", "switch", "throw", "try",
			"unchecked", "using", "while", "enum", "struct", "class", "interface", 
			"namespace", "trait", "alias", "event", "delegate", "goto case");

		// Syntactic categories of statements:
		//
		// | Category            | Syntax example(s)      | Detection method          |
		// |---------------------|------------------------|---------------------------|
		// | Space definition    | struct X : Y {...}     | IsSpaceStatement()        |
		// | Variable decl       | int x = 2;             | IsVariableDecl()          |
		// | Other definitions   | delegate void f();     | Check DefinitionStmts     |
		// | Simple stmt         | goto label;            | Check SimpleStmts list    |
		// | Block stmt with or  | for (...) {...}        | Check BlockStmts list     |
		// |   without args      | try {...} catch {...}  |                           |
		// | Label stmt          | case 2: ... label:     | IsLabelStmt()             |
		// | Block or list       | { ... } or #{ ... }    | Name in (S.List,S.Braces) |
		// | Expression stmt     | x += y;                | When none of the above    |

		// Simple statements have the syntax "keyword;" or "keyword expr;"
		static readonly HashSet<Symbol> SimpleStmts = new HashSet<Symbol>(new[] {
			S.Break, S.Continue, S.Goto, S.GotoCase, S.Return, S.Throw,
		});
		// Block statements take block(s) as arguments
		static readonly HashSet<Symbol> BlockStmts = new HashSet<Symbol>(new[] {
			S.If, S.Checked, S.DoWhile, S.Fixed, S.For, S.ForEach, S.If, S.Lock, 
			S.Switch, S.Try, S.Unchecked, S.UsingStmt, S.While
		});
		// Space definitions are containers for other definitions
		static readonly HashSet<Symbol> SpaceDefinitionStmts = new HashSet<Symbol>(new[] {
			S.Struct, S.Class, S.Trait, S.Enum, S.Alias, S.Interface, S.Namespace
		});
		// Definition statements define types, spaces, methods, properties, events and variables
		static readonly HashSet<Symbol> DefinitionStmts = new HashSet<Symbol>(
			SpaceDefinitionStmts.Concat(new[] {
				S.Def, S.Var, S.Event, S.Delegate, S.Property
			}));

		static readonly HashSet<Symbol> AllNonExprStmts = new HashSet<Symbol>(
			SimpleStmts.Concat(BlockStmts).Concat(DefinitionStmts)
			.Concat(new[] { S.Case, S.Label, S.Braces, S.List }));

		static readonly HashSet<Symbol> StmtsWithWordAttrs = AllNonExprStmts;
			

		
		static HashSet<Symbol> SymbolSet(params string[] input)
		{
			return new HashSet<Symbol>(input.Select(s => GSymbol.Get(s)));
		}
		static Dictionary<Symbol, string> KeywordDict(params string[] input)
		{
			//int x;
			//int y = (3>4?x=5:(x=6));

			var d = new Dictionary<Symbol, string>(input.Length);
			for (int i = 0; i < input.Length; i++)
			{
				string name = input[i], text = name;
				if (name == "goto case")
					name = "#gotoCase";
				else
					name = "#" + name;
				d[GSymbol.Get(name)] = text;
			}
			return d;
		}
		static Pair<K,V> P<K,V>(K key, V value) 
			{ return G.Pair(key, value); }
		static Dictionary<K,V> Dictionary<K,V>(params Pair<K,V>[] input)
		{
			var d = new Dictionary<K,V>();
			for (int i = 0; i < input.Length; i++)
				d.Add(input[i].Key, input[i].Value);
			return d;
		}

		#endregion

		#region Syntax validators
		// These are validators for printing purposes: they check that each node 
		// that shouldn't have attributes, doesn't; if attributes are present in
		// strange places then we print with prefix notation instead to avoid 
		// losing them when round-tripping.

		public static bool IsSpaceStatement(INodeReader _n) // for printing purposes
		{
			// All space declarations and space definitions have the form
			// #def_spacetype(Name, #(BaseList), { ... }) and the syntax
			// spacetype Name : Bases { ... }, with optional "where" and "if" clauses
			// e.g. enum Foo : ushort { A, B, C }
			// For printing purposes,
			// - A declaration has 1 or 2 args; a definition has 3 args
			// - Name must be a simple symbol without attributes
			// - #(BaseList) can be #missing; the bases can be any expressions
			// - { ... } can be #{ ... } instead, but nothing else
			// - the arguments do not have attributes
			if (SpaceDefinitionStmts.Contains(_n.Name) && _n.HasSimpleHeadWithoutPAttrs() && MathEx.IsInRange(_n.ArgCount, 1, 3))
			{
				INodeReader name = _n.TryGetArg(0), bases = _n.TryGetArg(1), body = _n.TryGetArg(2);
				if (name.HasPAttrs()) return false;
				if (bases == null) return true;
				if (bases.HasPAttrs()) return false;
				if (bases.IsSimpleSymbol(S.Missing) || bases.Calls(S.List))
				{
					if (body == null) return true;
					if (body.HasPAttrs()) return false;
					return (body.CallsWPAIH(S.Braces) ||
						   body.CallsWPAIH(S.List));
				}
			}
			return false;
		}

		public static bool IsVariableDecl(INodeReader _n, bool allowMultiple, bool allowNoAssignment) // for printing purposes
		{
			// e.g. #var(#int, x(0)) <=> int x = 0
			// For printing purposes in EC#,
			// - Head and args do not have attributes
			// - First argument must have the syntax of a type name
			// - Other args must have the form foo or foo(expr), where expr does not have attributes
			// - Must define a single variable unless allowMultiple
			// - Must immediately assign the variable unless allowNoAssignment
			if (_n.CallsMinWPAIH(S.Var, 2))
			{
				var a = _n.Args;
				if (!IsComplexIdentifier(a[0]))
					return false;
				for (int i = 1; i < a.Count; i++)
				{
					if (a[i].HasPAttrs())
						return false;
					if (a[i].IsCall && (a[i].ArgCount != 1 || a[i].Args[0].HasPAttrs()))
						return false;
				}
				if (a.Count > 2 && !allowMultiple)
					return false;
				if (!a[1].IsCall && !allowNoAssignment)
					return false;
				return true;
			}
			return false;
		}

		public static bool IsComplexIdentifierOrNull(INodeReader n)
		{
			if (n == null)
				return true;
			return IsComplexIdentifier(n);
		}
		public static bool IsComplexIdentifier(INodeReader n, ICI f = ICI.Default)
		{
			// Returns true if 'n' is printable as a complex identifier.
			//
			// To be printable, a complex identifier in EC# must not contain 
			// attributes (ICI.IgnoreAttrs to override)
			// 1. A simple symbol without attributes
			// 2. A substitution expression without attributes
			// 3. A dotted expr (a.b.c) without attributes, where 'a' is a simple 
			//    identifier or #of call, while 'b' and 'c' are (1) or (2); 
			//    structures like (a.b).c and a.(b<c>) do not count as complex 
			//    identifiers, although the former is legal as an ordinary 
			//    expression. Note that a.b<c> is structured #of(#.(a, b), c), 
			//    not #.(a, #of(b, c)).
			// 4. An #of expr (a<b>) without attributes, where 
			//    - 'a' is a complex identifier and not itself an #of expr
			//    - 'b' is a complex identifier or a list #(...)
			// 
			// Type names have the same structure, with the following patterns for
			// arrays, pointers, nullables and typeof<>:
			// 
			// Foo*      <=> #of(#*, Foo)
			// Foo[]     <=> #of(#[], Foo)
			// Foo[,]    <=> #of(#`[,]`, Foo)
			// Foo?      <=> #of(#?, Foo)
			// typeof<X> <=> #of(#typeof, X)
			//
			// Note that we can't just use #of(Nullable, Foo) for Foo? because it
			// doesn't work if System is not imported. It's reasonable to allow #? 
			// as a synonym for global::System.Nullable, since we have special 
			// symbols for types like #int anyway.
			// 
			// (a.b<c>.d<e>.f is structured ((((a.b)<c>).d)<e>).f or #.(#of(#.(#of(#.(a,b), c), d), e), f)
			if ((f & (ICI.AllowAttrsRecursive | ICI.AllowAttrs)) == 0 && n.HasPAttrs())
				return false;

			if (n.IsSimpleSymbol) // !IsCall && !IsLiteral && Head == null
					return true;
			if (n.CallsWPAIH(S.Substitute, 1))
				return true;

			if (n.IsParenthesizedExpr()) // !self.IsCall && self.Head != null
			{
				// TODO: detect subexpressions that are legal in typeof
				return (f & ICI.AllowSubexpr) != 0;
			}
			if (n.CallsWPAIH(S.List, 1))
				return (f & ICI.InOf) != 0;

			if (n.CallsMinWPAIH(S.Of, 1) && (f & ICI.AllowOf) != 0) {
				bool accept = true;
				ICI childFlags = ICI.AllowDotted | (f & ICI.AllowAttrsRecursive);
				bool allowSubexpr = n.Args[0].IsSimpleSymbol(S.Typeof);
				for (int i = 0; i < n.ArgCount; i++) {
					if (!IsComplexIdentifier(n.Args[i], childFlags)) {
						accept = false;
						break;
					}
					childFlags |= ICI.InOf | ICI.AllowOf;
					if (allowSubexpr)
						childFlags |= ICI.AllowSubexpr;
				}
				return accept;
			}
			if (n.CallsMinWPAIH(S.Dot, 1) && (f & ICI.AllowDotted) != 0) {
				bool accept = true;
				if (n.ArgCount == 1) {
					// Left-hand argument was omitted
					return IsComplexIdentifier(n.Args[0], f & ICI.AllowAttrsRecursive);
				} else if (IsComplexIdentifier(n.Args[0], ICI.AllowOf | (f & (ICI.AllowSubexpr | ICI.AllowAttrsRecursive)))) {
					for (int i = 1; i < n.ArgCount; i++) {
						// Allow only simple symbols or substitution
						if (!IsComplexIdentifier(n.Args[i], f & ICI.AllowAttrsRecursive)) {
							accept = false;
							break;
						}
					}
				} else
					accept = false;
				return accept;
			}
			return false;
		}

		public static bool IsDefinitionStmt(INodeReader n)
		{
			if (DefinitionStmts.Contains(n.Name)) {
				if (n.Name == S.Var)
					return IsVariableDecl(n, true, true);
				return true;
			}
			return false;
		}

		public static bool IsBlockStmt(INodeReader n)
		{
			return BlockStmts.Contains(n.Name);
		}

		public static bool IsBlockOfStmts(INodeReader n)
		{
			return n.Name == S.Braces || n.Name == S.List;
		}

		public static bool IsSimpleStmt(INodeReader n)
		{
			if (SimpleStmts.Contains(n.Name))
			{
				return true;
			}
			return false;
		}

		public static bool IsLabelStmt(INodeReader n)
		{
			return n.Name == S.Label || n.CallsWPAIH(S.Case);
		}

		public static bool IsNamedArgument(INodeReader n)
		{
 			return n.CallsWPAIH(S.NamedArg, 2) && n.Args[0].IsSimpleSymbolWithoutPAttrs();
		}

		#endregion

		#region PrintStmt and related

		public void PrintStmt(bool inCommaSeparatedList = false)
		{
			_out.BeginStatement();

			bool writeTerminator = true;
			if (AllNonExprStmts.Contains(_n.Name)) {
				// Special code exists to handle this kind of node
				if (IsDefinitionStmt(_n))
					PrintDefinitionStmt();
				else if (IsBlockOfStmts(_n))
					PrintBlockOfStmts(inCommaSeparatedList);
				else if (IsBlockStmt(_n))
					PrintBlockStmt();
				else if (IsSimpleStmt(_n))
					PrintSimpleStmt();
				else if (IsLabelStmt(_n)) {
					PrintLabelStmt();
					writeTerminator = false;
				} else
					PrintExpr(true);
			} else
				PrintExpr(true);
			
			if (writeTerminator)
				_out.Write(';', true);
		}

		// Printers for various statement types follow. The syntax tree is 
		// validated in advance, so each printer assumes that it is given 
		// something that it knows how to print (except that if _n.Name is
		// not recognized, PrintExpr is used as a fallback).

		internal void PrintDefinitionStmt()
		{
			// Definition statements:
			// Spaces: S.Struct, S.Class, S.Trait, S.Enum, S.Alias, S.Interface, S.Namespace
			// Other:  S.Def, S.Var, S.Event, S.Delegate, S.Property
			if (SpaceDefinitionStmts.Contains(_n.Name))
				PrintSpaceStmt();
			else
				PrintExpr(true);
		}

		internal void PrintSpaceStmt()
		{
			PrintExpr(true);
		}

		internal void PrintLabelStmt()
		{
			if (_n.Name == S.Label) {
				PrintExpr(true, false);
			} else if (_n.Name == S.Case) {
				for (int i = 0; i < _n.ArgCount; i++)
					using (var _ = With(_n.Args[i]))
						PrintExpr(false, false);
			}
			_out.Write(':', true);
		}

		internal void PrintBlockStmt()
		{
			// Block statements:
			// S.If, S.Checked, S.DoWhile, S.Fixed, S.For, S.ForEach, S.If, S.Lock, 
			// S.Switch, S.Try, S.Unchecked, S.Using, S.While
			PrintExpr(true, false); // TODO
		}

		internal void PrintSimpleStmt()
		{
			// Simple statements:
			// S.Break, S.Continue, S.Goto, S.GotoCase, S.Return, S.Throw
			PrintExpr(true, false); // TODO
		}

		internal void PrintBlockOfStmts(bool inCommaSeparatedList)
		{
			PrintAttrs(false, false);

			bool isList = _n.Name == S.List;
			if (isList && _n.TryGetAttr(S.StyleCommaSeparatedStmts) != null && !inCommaSeparatedList)
			{
				// print as a comma-separated list
				using (Indented)
				{
					var a = _n.Args;
					for (int i = 0, c = a.Count; i < c; i++)
						using (With(a[i]))
							PrintStmt(true);
				}
			}
			else
			{
				_out.Write(isList ? "#{" : "{", true);
				using (Indented)
				{
					var a = _n.Args;
					for (int i = 0, c = a.Count; i < c; i++)
						using (With(a[i]))
							PrintStmt();
				}
				_out.Write('}', true);
			}
		}

		#endregion

		#region PrintExpr and related

		public void PrintExpr(bool allowNoAssignmentVarDecls = false, bool allowNamedArg = true, int precedenceFloor = int.MinValue)
		{
			PrintAttrs(false, false);

			if (allowNamedArg) {
				if (IsNamedArgument(_n)) {
					using (var _ = With(_n.Args[0]))
						PrintExpr(false);
					_out.Write(':', true);
					using (var _ = With(_n.Args[1]))
						PrintExpr(false);
					return;
				}
			}

			if (IsVariableDecl(_n, false, allowNoAssignmentVarDecls))
				PrintVariableDecl();
			else if (!AutoPrintOperator(precedenceFloor))
				PrintPrefixNotation(false, false, true);
		}

		private bool AutoPrintOperator(int PF)
		{
			if (InfixOperators.ContainsKey(_n.Name))
			{
			}
			return false;
		}

		internal void PrintExprOrPrefixNotation(bool prefix, bool purePrefixNotation)
		{
			if (prefix)
				PrintPrefixNotation(true, purePrefixNotation);
			else
				PrintExpr();
		}

		public void PrintPrefixNotation(bool recursive, bool purePrefixNotation, bool skipAttrs = false)
		{
			if (!skipAttrs)
				PrintAttrs(false, purePrefixNotation);

			if (!purePrefixNotation && IsComplexIdentifier(_n, ICI.Default | ICI.AllowAttrs))
			{
				PrintTypeOrMethodName(false);
				return;
			}

			// Print the head
			if (_n.Head == null) {
				if (_n.IsLiteral)
					PrintLiteral();
				else
					PrintIdent(_n.Name, false);
			} else if (_n.IsParenthesizedExpr()) {
				_out.Write('(', true);
				using (var _ = With(_n.Head))
					PrintPrefixNotation(recursive, purePrefixNotation);
				_out.Write(')', false);
			} else if (!purePrefixNotation && IsComplexIdentifier(_n.Head)) {
				using (With(_n.Head))
					PrintTypeOrMethodName(false);
			} else
				using (var _ = With(_n.Head))
					PrintPrefixNotation(recursive, purePrefixNotation);

			// Print args, if any
			if (_n.IsCall) {
				_out.Write('(', true);
				for (int i = 0, c = _n.ArgCount; i < c; i++) {
					using (var _ = With(_n.Args[i]))
						PrintExprOrPrefixNotation(recursive, purePrefixNotation);
					if (i + 1 == c)
						_out.Write(')', true);
					else
						_out.Write(',', true);
				}
			}
		}

		private void PrintTypeName() { PrintTypeOrMethodName(true); }
		private void PrintTypeOrMethodName(bool isType)
		{
			if (_n.Name == S.Dot)
			{
				for (int i = 0; i < _n.ArgCount; i++) {
					if (i != 0)
						_out.Write('.', true);
					using (var _ = With(_n.Args[i]))
						PrintTypeOrMethodName(isType);
				}
			}
			else if (_n.Name == S.Of)
			{
				if (_n.ArgCount == 2 && _n.Args[0].IsSimpleSymbol && isType)
				{
					var name = _n.Args[0].Name;
					if (name == S.QuestionMark) // nullable
					{
						using (var _ = With(_n.Args[1]))
							PrintTypeOrMethodName(true);
						_out.Write("? ", true);
						return;
					}
					else if (name == S.Mul) // pointer
					{
						using (var _ = With(_n.Args[1]))
							PrintTypeOrMethodName(true);
						_out.Write("* ", true);
						return;
					}
					else if (S.IsArrayKeyword(name))
					{
						using (var _ = With(_n.Args[1]))
							PrintTypeOrMethodName(true);
						_out.Write(name.Name.Substring(1), true); // e.g. [] or [,]
						return;
					}
				}
				for (int i = 0; i < _n.ArgCount; i++) {
					if (i > 1)
						_out.Write(',', true);
					using (var _ = With(_n.Args[i]))
						PrintTypeOrMethodName(i != 0);
					if (i == 0)
						_out.Write('<', true);
				}
				_out.Write('>', true);
			}
			else
				PrintIdent(_n.Name, isType);
		}

		private void PrintVariableDecl() // skips attributes
		{
			Debug.Assert(_n.Name == S.Var);
			var a = _n.Args;
			using (var _ = With(a[0]))
				PrintTypeName();
			for (int i = 1; i < a.Count; i++) {
				if (i > 1)
					_out.Write(',', true);
				PrintIdent(a[i].Name, false);
				if (a[i].IsCall) {
					_out.Write('=', true);
					using (var _ = With(a[i].Args[0]))
						PrintExpr(false, false);
				}
			}
		}

		#endregion

		#region Parts of expressions: attributes, identifiers, literals

		private void PrintAttrs(bool allowWordAttrs, bool pureAttributeNotation)
		{
			var a = _n.Attrs;
			int div = a.Count;

			if (!pureAttributeNotation)
			{
				// Choose how much of the attributes to treat as simple words, 
				// e.g. we prefer to print [Flags, #public] as "[Flags] public"
				bool isVarDecl = _n.Name == S.Var;
				for (; div > 0; div--)
					if (!IsWordAttribute(a[div-1], allowWordAttrs || isVarDecl, isVarDecl))
						break;
			}
			if (div > 0)
			{
				bool first = true;
				for (int i = 0; i < div; i++) {
					if (!a[i].IsPrintableAttr())
						continue;
					if (first)
						_out.Write('[', true);
					else
						_out.Write(',', true);
					first = false;
					using (var _ = With(a[i]))
						PrintExpr();
				}
				if (!first) {
					_out.Write(']', true);
					_out.Space();
				}
			}
			for (int i = div; i < a.Count; i++) {
				string text;
				if (AttributeKeywords.TryGetValue(a[i].Name, out text))
					_out.Write(text, true);
				else {
					Debug.Assert(a[i].IsKeyword);
					if (a[i].IsPrintableAttr())
						PrintIdent(GSymbol.Get(a[i].Name.Name.Substring(1)), false);
				}
			}
		}

		private void PrintIdent(Symbol name, bool isType, bool inSymbol = false)
		{
 			if (name.Name == "") {
				Debug.Assert(false);
				return;
			}
			
			// Find out if the symbol is a valid identifier
			char first = name.Name[0];
			bool isNormal = true;
			if (first == '#') {
				string text;
				if (isType && TypeKeywords.TryGetValue(name, out text)) {
					_out.Write(text, true);
					return;
				}
				if (TokenHashKeywords.Contains(name)) {
					_out.Write(name.Name, true);
					return;
				}
				first = name.Name[1];
			}
			if (!IsIdentStartChar(first))
				isNormal = false;
			else for (int i = 1; i < name.Name.Length; i++)
				if (!IsIdentContChar(name.Name[i])) {
					isNormal = false;
					break;
				}
			if (isNormal) {
				if (CsKeywords.Contains(name) && !inSymbol)
					_out.Write("@", false);
				if (PreprocessorCollisions.Contains(name) && !inSymbol) {
					_out.Write("#@", false);
					_out.Write(name.Name.Substring(1), true);
				} else
					_out.Write(name.Name, true);
			} else {
				PrintString('\'', _Verbatim, name.Name, !inSymbol);
			}
		}

		static readonly Symbol _Verbatim = GSymbol.Get("#style_verbatim");
		static readonly Symbol _DoubleVerbatim = S.StyleDoubleVerbatim;
		private void PrintString(char quoteType, Symbol verbatim, string text, bool includeAtSign = false)
		{
			if (includeAtSign && verbatim != null)
				_out.Write(verbatim == S.StyleDoubleVerbatim ? "@@" : "@", false);

			_out.Write(quoteType, false);
			if (verbatim != null) {
				for (int i = 0; i < text.Length; i++) {
					if (text[i] == quoteType)
						_out.Write(quoteType, false);
					if (verbatim == S.StyleDoubleVerbatim && text[i] == '\n')
						_out.Newline(); // includes indentation
					else
						_out.Write(text[i], false);
				}
			} else {
				_out.Write(G.EscapeCStyle(text, EscapeC.Control, quoteType), false);
			}
			_out.Write(quoteType, true);
		}

		static Pair<RuntimeTypeHandle,Action<EcsNodePrinter>> P<T>(Action<EcsNodePrinter> value) 
			{ return G.Pair(typeof(T).TypeHandle, value); }
		static Dictionary<RuntimeTypeHandle,Action<EcsNodePrinter>> LiteralPrinters = Dictionary(
			P<int>    (np => np.PrintIntegerToString("")),
			P<long>   (np => np.PrintIntegerToString("L")),
			P<uint>   (np => np.PrintIntegerToString("u")),
			P<ulong>  (np => np.PrintIntegerToString("uL")),
			P<short>  (np => np.PrintIntegerToString("(->short)")),  // Unnatural. Not produced by parser.
			P<ushort> (np => np.PrintIntegerToString("(->ushort)")), // Unnatural. Not produced by parser.
			P<sbyte>  (np => np.PrintIntegerToString("(->sbyte)")),  // Unnatural. Not produced by parser.
			P<byte>   (np => np.PrintIntegerToString("(->byte)")),   // Unnatural. Not produced by parser.
			P<double> (np => np.PrintValueToString("d")),
			P<float>  (np => np.PrintValueToString("f")),
			P<decimal>(np => np.PrintValueToString("m")),
			P<bool>   (np => np._out.Write((bool)np._n.Value ? "true" : "false", true)),
			P<@void>  (np => np._out.Write("()", true)),
			P<char>   (np => np.PrintString('\'', null, np._n.Value.ToString())),
			P<string> (np => {
				var v1 = np._n.TryGetAttr(_DoubleVerbatim);
				var v2 = v1 != null ? v1.Name : ((np._n.Style & NodeStyle.Alternate) != 0 ? _Verbatim : null);
				np.PrintString('"', v2, np._n.Value.ToString(), true);
			}),
			P<Symbol> (np => {
				np._out.Write('$', false);
				np.PrintIdent((Symbol)np._n.Value, false, true);
			}));
		
		void PrintValueToString(string suffix)
		{
			_out.Write(_n.Value.ToString(), false);
			_out.Write(suffix, true);
		}
		void PrintIntegerToString(string suffix)
		{
			string asStr;
			if ((_n.Style & NodeStyle.Alternate) != 0) {
				var value = (IFormattable)_n.Value;
				_out.Write("0x", false);
				asStr = value.ToString("x", null);
			} else
				asStr = _n.Value.ToString();
			if (suffix == "")
				_out.Write(asStr, true);
			else {
				_out.Write(asStr, false);
				_out.Write(suffix, true);
			}
		}
		void PrintValueFormatted(string format)
		{
			_out.Write(string.Format(format, _n.Value), true);
		}

		private void PrintLiteral()
		{
			Debug.Assert(_n.IsLiteral);
			Action<EcsNodePrinter> p;
			if (_n.Value == null)
				_out.Write("null", true);
			else if (LiteralPrinters.TryGetValue(_n.Value.GetType().TypeHandle, out p))
				p(this);
			else // NOT SUPPORTED
				_out.Write("#literal", true);
		}

		public static bool IsIdentStartChar(char c)
		{
 			return c == '_' || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c > 128 && char.IsLetter(c));
		}
		public static bool IsIdentContChar(char c)
		{
			return (c >= '0' && c <= '9') || IsIdentStartChar(c);
		}
		bool IsWordAttribute(INodeReader node, bool allowNonReserved, bool isVarDecl)
		{
			if (node.IsCall || node.HasAttrs())
				return false;
			else if (allowNonReserved && node.IsKeyword)
				return true;
			else if (AttributeKeywords.ContainsKey(node.Name))
				return isVarDecl || (node.Name != S.New && node.Name != S.Out);
			else
				return false;
		}

		#endregion
	}

	/// <summary>Flags for <see cref="EcsNodePrinter.IsComplexIdentifier"/>.</summary>
	[Flags] public enum ICI
	{
		Simple = 0,
		Default = AllowOf | AllowDotted,
		AllowAttrs = 2,
		AllowAttrsRecursive = 4, // recursive
		AllowOf = 8,
		AllowDotted = 16,
		AllowSubexpr = 32,
		InOf = 64,
	}

}
