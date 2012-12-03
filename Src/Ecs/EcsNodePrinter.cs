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
		void BeginStatement();
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
			if (EcsNodePrinter.IsIdentContChar(_lastCh) && (EcsNodePrinter.IsIdentContChar(nextCh) || nextCh == '@'))
				_out.Write(' ');
			else if ((_lastCh == '-' && nextCh == '-') || (_lastCh == '+' && nextCh == '+'))
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
		public void BeginStatement()
		{
			if (_lastCh == '\n')
				return;
			Newline();
		}
		private void Newline()
		{
			_lastCh = '\n';

			_out.Write(_lineSeparator);
			for (int i = 0; i < _indentLevel; i++)
				_out.Write(_indentString);
		}
	}

	public struct EcsNodePrinter
	{
		INodeReader _n;
		INodePrinterWriter _out;

		public EcsNodePrinter(INodeReader node, INodePrinterWriter target)
		{
			_n = node;
			_out = target;
		}

		struct Indent : IDisposable
		{
			EcsNodePrinter _self;
			public Indent(EcsNodePrinter self) { _self = self; self._out.Indent(); }
			public void Dispose() { _self._out.Dedent(); }
		}
		Indent Indented { get { return new Indent(this); } }

		private EcsNodePrinter With(INodeReader node)
		{
			EcsNodePrinter p2 = this; p2._n = node; return p2;
		}

		#region Keyword sets and other symbol sets

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

		static readonly Dictionary<Symbol, string> KeywordStmts = KeywordDict(
			"break", "case", "checked", "continue", "default",  "do", "fixed", 
			"for", "foreach", "goto", "if", "lock", "return", "switch", "throw", "try",
			"unchecked", "using", "while", "#def_enum", "#def_struct", "#def_class", 
			"#def_interface", "#def_namespace", "#def_trait", "#def_alias", 
			"#def_event", "#def_delegate", "goto case");

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
			S.Switch, S.Try, S.Unchecked, S.Using, S.While
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
			var d = new Dictionary<Symbol, string>(input.Length);
			for (int i = 0; i < input.Length; i++)
			{
				string name = input[i], text = name;
				if (name.StartsWith("#def_"))
					text = name.Substring("#def_".Length);
				else if (name == "goto case")
					name = "#gotoCase";
				else
					name = "#" + name;
				d[GSymbol.Get(name)] = text;
			}
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

		public static bool IsComplexIdentifier(INodeReader n)
		{
			bool isCall;
			return IsComplexIdentifier(n, false, out isCall);
		}
		public static bool IsComplexIdentifier(INodeReader n, bool allowCall, out bool isCall, bool inOf = false, bool allowOf = true, bool allowDotted = true, bool allowSubexpr = false)
		{
			// To be printable, a complex identifier in EC# must be one of...
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
			if (!n.HasPAttrs())
			{
				isCall = false;

				if (n.IsSimpleSymbol) // !IsCall && !IsLiteral && Head == null
					return true;
				if (n.CallsWPAIH(S.Substitute, 1))
					return true;

				if (n.IsParenthesizedExpr()) // !self.IsCall && self.Head != null
				{
					// TODO: detect subexpressions that are legal in typeof
					return allowSubexpr;
				}
				if (n.CallsWPAIH(S.List, 1))
					return inOf;

				if (n.CallsWPAIH(S.Of, 1)) {
					bool accept = true;
					allowSubexpr = n.Args[0].IsSimpleSymbol(S.Typeof);
					for (int i = 0; i < n.ArgCount; i++)
						if (!IsComplexIdentifier(n.Args[i], false, out isCall, i != 0, i != 0, true, i != 0 && allowSubexpr)) {
							accept = false;
							break;
						}
					return allowOf && accept;
				}
				if (n.CallsMinWPAIH(S.Dot, 2)) {
					bool accept = true;
					if (IsComplexIdentifier(n.Args[0], false, out isCall, false, true, false, allowSubexpr)) {
						for (int i = 1; i < n.ArgCount; i++) {
							// Allow only simple symbols or substitution
							if (!IsComplexIdentifier(n.Args[i], false, out isCall, false, false, false)) {
								accept = false;
								break;
							}
						}
					} else
						accept = false;
					return allowDotted && accept;
				}
			}

			if (isCall = n.IsCall)
			{
				if (!allowCall)
					return false;
				// allow A(X) - Head == null && !IsLiteral
				// or    A.B<C>(X) - Head is a complex identifier
				if (n.Head == null)
					return !n.IsLiteral;
				else
					return IsComplexIdentifier(n.Head);
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
					With(_n.Args[i]).PrintExpr(false, false);
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
				using (var _ = Indented)
				{
					var a = _n.Args;
					for (int i = 0, c = a.Count; i < c; i++)
						With(a[i]).PrintStmt(true);
				}
			}
			else
			{
				_out.Write(isList ? "#{" : "{", true);
				using (var _ = Indented)
				{
					var a = _n.Args;
					for (int i = 0, c = a.Count; i < c; i++)
						With(a[i]).PrintStmt();
				}
				_out.Write('}', true);
			}
		}

		#endregion

		#region PrintExpr and related

		public void PrintExpr(bool allowNoAssignmentVarDecls = false, bool allowNamedArg = true)
		{
			PrintAttrs(false, false);

			// (stmtContext implies that the attributes were already printed 
			// and that a named argument is not allowed here)
			if (allowNamedArg) {
				if (IsNamedArgument(_n)) {
					With(_n.Args[0]).PrintExpr(false);
					_out.Write(':', true);
					With(_n.Args[1]).PrintExpr(false);
					return;
				}
			}

			if (IsVariableDecl(_n, false, allowNoAssignmentVarDecls))
				PrintVariableDecl();
			else
				PrintPrefixNotation(false, false);
		}

		internal void PrintExprOrPrefixNotation(bool prefix, bool purePrefixNotation)
		{
			if (prefix)
				PrintPrefixNotation(true, purePrefixNotation);
			else
				PrintExpr();
		}

		public void PrintPrefixNotation(bool recursive, bool purePrefixNotation)
		{
			PrintAttrs(false, purePrefixNotation);

			// Print the head
			bool isCall = _n.IsCall;
			if (!purePrefixNotation && IsComplexIdentifier(_n, true, out isCall))
				PrintTypeOrMethodName();
			else if (_n.Head == null) {
				if (_n.IsLiteral)
					PrintLiteral();
				else
					PrintIdent(_n.Name);
			} else if (_n.IsParenthesizedExpr()) {
				_out.Write('(', true);
				With(_n.Head).PrintPrefixNotation(recursive, purePrefixNotation);
				_out.Write(')', false);
			} else
				With(_n.Head).PrintPrefixNotation(recursive, purePrefixNotation);

			// Print args, if any
			if (isCall) {
				_out.Write('(', true);
				for (int i = 0, c = _n.ArgCount; i < c; i++) {
					PrintExprOrPrefixNotation(recursive, purePrefixNotation);
					if (i + 1 == c)
						_out.Write(')', true);
					else
						_out.Write(',', true);
				}
			}
		}

		private void PrintTypeOrMethodName()
		{
			if (_n.Name == S.Dot)
			{
				for (int i = 0; i < _n.ArgCount; i++) {
					if (i != 0)
						_out.Write('.', true);
					With(_n.Args[i]).PrintTypeOrMethodName();
				}
			}
			else if (_n.Name == S.Of)
			{
				if (_n.ArgCount == 2 && _n.Args[0].IsSimpleSymbol)
				{
					var name = _n.Args[0].Name;
					if (name == S.QuestionMark) // nullable
					{
						With(_n.Args[1]).PrintTypeOrMethodName();
						_out.Write("? ", true);
						return;
					}
					else if (name == S.Mul) // pointer
					{
						With(_n.Args[1]).PrintTypeOrMethodName();
						_out.Write("* ", true);
						return;
					}
					else if (S.IsArrayKeyword(name))
					{
						With(_n.Args[1]).PrintTypeOrMethodName();
						_out.Write(name.Name.Substring(1), true); // e.g. [] or [,]
						return;
					}
				}
				for (int i = 0; i < _n.ArgCount; i++) {
					if (i > 1)
						_out.Write(',', true);
					With(_n.Args[i]).PrintTypeOrMethodName();
					if (i == 0)
						_out.Write('<', true);
				}
				_out.Write('>', true);
			}
			else
				PrintIdent(_n.Name);
		}

		private void PrintVariableDecl() // skips attributes
		{
			Debug.Assert(_n.Name == S.Var);
			var a = _n.Args;
			With(a[0]).PrintTypeOrMethodName();
			for (int i = 1; i < a.Count; i++) {
				if (i > 1)
					_out.Write(',', true);
				PrintIdent(a[i].Name);
				if (a[i].IsCall) {
					_out.Write('=', true);
					With(a[i].Args[0]).PrintExpr(false, false);
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
				_out.Write('[', true);
				for (int i = 0; i < div; i++) {
					if (i != 0)
						_out.Write(',', true);
					With(a[div]).PrintExpr();
				}
				_out.Write(']', true);
			}
			for (int i = div; i < a.Count; i++) {
				string text;
				if (AttributeKeywords.TryGetValue(a[i].Name, out text))
					_out.Write(text, true);
				else
					PrintIdent(a[i].Name);
			}
		}

		private void PrintIdent(Symbol name)
		{
 			if (name.Name == "") {
				Debug.Assert(false);
				return;
			}
			bool isKW = name.Name[0] == '#';
			
			// Find out if the symbol is a valid identifier
			char first = name.Name[0];
			bool isNormal = true;
			if (first != '#' && !IsIdentStartChar(first))
				isNormal = false;
			else for (int i = 1; i < name.Name.Length; i++)
				if (!IsIdentContChar(name.Name[i])) {
					isNormal = false;
					break;
				}
			if (isNormal) {
				if (CsKeywords.Contains(name))
					_out.Write("@", false);
				_out.Write(name.Name, true);
			} else {
				_out.Write("@", false);
				PrintString('`', false, name.Name);
			}
		}

		private void PrintString(char quoteType, bool isVerbatim, string text)
		{
			_out.Write(quoteType, false);
			if (isVerbatim) {
				for (int i = 0; i < text.Length; i++) {
					if (text[i] == quoteType)
						_out.Write(quoteType, false);
					_out.Write(text[i], false);
				}
			} else {
				var flags = EscapeC.Control | EscapeC.SingleQuotes;
				_out.Write(G.EscapeCStyle(text, EscapeC.Control, quoteType), false);
			}
			_out.Write(quoteType, true);
		}

		private void PrintLiteral()
		{
			Debug.Assert(_n.IsLiteral);
			throw new NotImplementedException();
		}

		public static bool IsIdentStartChar(char c)
		{
 			return c == '_' || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c > 128 && char.IsLetter(c));
		}
		public static bool IsIdentContChar(char c)
		{
			return (c >= '0' && c <= '9') || IsIdentStartChar(c);
		}
		bool IsWordAttribute(INodeReader node, bool allowNonKeyword, bool isVarDecl)
		{
			if (node.IsCall || node.HasAttrs())
				return false;
			else if (MathEx.IsInRange(node.Name.Name[0], 'a', 'z') || node.Name.Name[0] == '_')
				return true;
			else if (AttributeKeywords.ContainsKey(node.Name))
				return isVarDecl || (node.Name != S.New && node.Name != S.Out);
			else
				return false;
		}

		#endregion











		//private static string ToIdent(Symbol sym, bool keywordPrintingMode)
		//{
		//    Debug.Assert(sym.Name != "");
		//    if (keywordPrintingMode) {
		//        if (sym.Name[0] != '#')
		//            return sym.Name;
		//        Symbol word = GSymbol.GetIfExists(sym.Name.Substring(1));
		//        if (CsKeywords.Contains(word ?? GSymbol.Empty))
		//            return word.Name;
		//    } else {
		//        if (CsKeywords.Contains(sym))
		//            return "@" + sym.Name;
		//    }
		//    return sym.Name;
		//}
		//bool IsOneWordIdent(bool allowKeywords)
		//{
		//    if (_n.Name.Name == "")
		//        return false;
		//    if (_n.IsCall)
		//        return false;
		//    if (allowKeywords)
		//        return true;
		//    else
		//        return !CsKeywords.Contains(_n.Name) && !_n.IsKeyword;
		//}
		//bool IsIdentAtThisLevel(bool allowKeywords, bool allowDot, bool allowTypeArgs)
		//{
		//    if (_n.Name.Name == "")
		//        return false;
		//    if (_n.Name == S.Dot)
		//        return allowDot && _n.IsCall;
		//    if (_n.Name == _TypeArgs)
		//        return allowTypeArgs && _n.ArgCount >= 1;
		//    if (!allowKeywords && CsKeywords.Contains(_n.Name))
		//        return false;
		//    return !_n.IsKeyword;
		//}


		//public void PrintPrefixNotation(bool recursive)
		//{
		//    if (_n.IsCall) {
		//        if (recursive)
		//            With(_n.Head).PrintPrefixNotation(recursive);
		//        else
		//            With(_n.Head).PrintExpr();
		//        _sb.Append('(');
		//        for (int i = 0, c = _n.ArgCount; i < c; i++) {
		//            if (recursive)
		//                With(_n.Args[i]).PrintPrefixNotation(recursive);
		//            else
		//                With(_n.Args[i]).PrintExpr();
		//            if (i + 1 == c)
		//                _sb.Append(')');
		//            else
		//                _sb.Append(", ");
		//        }
		//    } else {
		//        if (!_n.IsCall)
		//            With(_n).PrintAtom();
		//    }
		//}

		//private void PrintAtom()
		//{
		//    if (_n.IsLiteral) {
		//        var v = _n.Value;
		//        if (v == null)
		//            _sb.Append("null");
		//        else if (v is string) {
		//            _sb.Append("\"");
		//            _sb.Append(G.EscapeCStyle(v as string, EscapeC.Control | EscapeC.DoubleQuotes));
		//            _sb.Append("\"");
		//        } else if (v is Symbol) {
		//            _sb.Append("$");
		//            _sb.Append(v.ToString()); // TODO
		//        } else {
		//            _sb.Append(v);
		//            if (v is float)
		//                _sb.Append('f');
		//        }
		//    } else {
		//        _sb.Append(_n.Name);
		//    }
		//}

		//public EcsNodePrinter PrintStmts(bool autoBraces, bool initialNewline, bool forceTreatAsList = false)
		//{
		//    if (_n.Name == _List || forceTreatAsList) {
		//        // Presumably we are starting in-line with some expression
		//        bool braces = autoBraces && _n.ArgCount != 1;
		//        if (braces) _sb.Append(" {");
				
		//        var ind = autoBraces ? Indented : this;
		//        for (int i = 0; i < _n.ArgCount; i++) {
		//            if (initialNewline || i != 0 || _n.ArgCount > 1)
		//                ind.Newline();
		//            ind.PrintStmt(false);
		//        }
		//        if (braces)
		//            Newline()._sb.Append('}');
		//    } else {
		//        PrintStmt(initialNewline);
		//    }
		//    return this;
		//}

		//public EcsNodePrinter PrintExpr()
		//{
		//    if (_n.Calls(_NamedArg, 2) && _n.Args[0].IsSimpleSymbol)
		//    {
		//        string ident = ToIdent(_n.Args[0].Name, false);
		//        _sb.Append(ident.EndsWith(":") ? " : " : ": ");
		//        With(_n.Args[1]).PrintExpr();
		//    }
		//    else if (_n.CallsMin(_Dot, 2) && _n.Args.All(n => IsIdentAtThisLevel(false, false, true)))
		//    {
		//        for (int i = 0; i < _n.ArgCount; i++) {
		//            if (i > 0) _sb.Append(".");
		//            With(_n.Args[i]).PrintExpr();
		//        }
		//    }
		//    else if (_n.CallsMin(_TypeArgs, 1) && _n.Args.All(n => IsIdentAtThisLevel(false, true, false)))
		//    {
		//        for (int i = 0; i < _n.ArgCount; i++) {
		//            if (i == 1) _sb.Append('<');
		//            else _sb.Append(", ");
		//            With(_n.Args[i]).PrintExpr();
		//        }
		//        _sb.Append('>');
		//    }
		//    else
		//        PrintPrefixNotation(false);
		//    return this;
		//}

		//private void PrintAttr(bool stmtMode)
		//{
		//    var attrs = _n.Attrs;
		//    Debug.Assert(attrs.Count > 0);
		//	
		//    _sb.Append('[');
		//    for (int i = 0; i < attrs.Count; i++)
		//    {
		//        With(attrs[i]).PrintExpr();
		//        if (i + 1 != attrs.Count)
		//            _sb.Append(", ");
		//    }
		//    _sb.Append("] ");
		//}
	}
}
