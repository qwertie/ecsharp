using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.ComponentModel;
using Loyc;
using Loyc.Syntax;
using Loyc.Utilities;
using Loyc.Math;
using S = Loyc.Syntax.CodeSymbols;
using EP = Loyc.Ecs.EcsPrecedence;
using System.IO;
using Loyc.Syntax.Les;
using Loyc.Syntax.Lexing;

namespace Loyc.Ecs
{
	// This file contains enumerations (ICI, SpaceOpt, NewlineOpt) and miscellaneous
	// code of EcsNodePrinter:
	// - User-configurable options
	// - Sets and dictionaries of keywords and tokens
	// - Syntax validators to check for valid structure from the perspective of EC#
	//   (when a construct has invalid structure, the statement or expression 
	//   printers fall back on prefix notation.)
	// - Code for printing attributes
	// - Code for printing simple identifiers
	// The code for printing expressions and statements is in separate source files
	// (EcsNodePrinter--expressions.cs and EcsNodePrinter--statements.cs).

	/// <summary>Prints a Loyc tree to EC# source code.</summary>
	/// <remarks>
	/// This class is designed to faithfully represent Loyc trees by default; any
	/// Loyc tree that can be represented as EC# source code will be represented 
	/// properly by this class, so that it is possible to parse the output text 
	/// back into a Loyc tree equivalent to the one that was printed. In other 
	/// words, EcsNodePrinter is designed to support round-tripping. For round-
	/// tripping to work, there are a couple of restrictions on the input tree:
	/// <ol>
	/// <li>The Value property must only be used in <see cref="LiteralNode"/>s,
	///     and only literals that can exist in C# source code are allowed. For 
	///     example, Values of type int, string, and double are acceptable, but
	///     Values of type Regex or int[] are not, because single tokens cannot
	///     represent these types in C# source code. The printer ignores Values of 
	///     non-literal nodes, and non-representable literals are printed out
	///     using ToString().</li>
	/// <li>Names must come from the global symbol pool (<see cref="GSymbol.Pool"/>).
	///     The printer will happily print Symbols from other pools, but there is
	///     no way to indicate the pool in source code, so the parser always 
	///     recreates symbols in the global pool. Non-global symbols are used
	///     after semantic analysis, so there is no way to faithfully represent
	///     the results of semantic analysis.</li>
	/// </ol>
	/// Only the attributes, head (<see cref="LiteralNode.Value"/>, 
	/// <see cref="IdNode.Name"/> or <see cref="CallNode.Target"/>), and arguments 
	/// of nodes are round-trippable. Superficial properties such as original 
	/// source code locations and the <see cref="LNode.Style"/> are, in 
	/// general, lost, although the printer can faithfully reproduce some (not 
	/// all) <see cref="NodeStyle"/>s. Also, any attribute whose Name starts with 
	/// "#trivia_" will be dropped, because these attributes are considered 
	/// extensions of the NodeStyle. However, the style indicated by the 
	/// #trivia_* attribute will be used if the printer recognizes it.
	/// <para/>
	/// Because EC# is based on C# which has some tricky ambiguities, there is a
	/// lot of code in this class dedicated to special cases and ambiguities. Even 
	/// so, it is likely that some cases have been missed--that some unusual trees 
	/// will not round-trip properly. Any failure to round-trip is a bug, and your 
	/// bug reports are welcome. If this class uses prefix notation (with 
	/// #specialNames) unnecessarily, that's also a bug, but it has low priority 
	/// unless it affects plain C# output (where #specialNames are illegal.)
	/// <para/>
	/// This class contains some configuration options that will defeat round-
	/// tripping but will make the output look better. For example,
	/// <see cref="AllowExtraBraceForIfElseAmbig"/> will print a tree such as 
	/// <c>#if(a, #if(b, f()), g())</c> as <c>if (a) { if (b) f(); } else g();</c>,
	/// by adding braces to eliminate prefix notation, even though braces make the 
	/// Loyc tree different.
	/// <para/>
	/// To avoid printing EC# syntax that does not exist in C#, you can call
	/// <see cref="SetPlainCSharpMode"/>, but this only works if the syntax tree
	/// does not contain invalid structure or EC#-specific code such as "==>", 
	/// "alias", and template arguments ($T).
	/// </remarks>
	public partial class EcsNodePrinter
	{
		LNode _n;
		INodePrinterWriter _out;
		Symbol _spaceName; // for detecting constructor ambiguity
		IMessageSink _errors;

		public LNode Node { get { return _n; } set { _n = value ?? LNode.Missing; } }
		public INodePrinterWriter Writer { get { return _out; } set { _out = value; } }

		/// <summary>Any error that occurs during printing is printed to this object.</summary>
		public IMessageSink Errors { get { return _errors; } set { _errors = value ?? MessageSink.Null; } }

		#region Constructors, New(), and default Printer

		public static EcsNodePrinter New(LNode node, StringBuilder target, string indentString = "\t", string lineSeparator = "\n")
		{
			return New(node, new StringWriter(target), indentString, lineSeparator);
		}
		public static EcsNodePrinter New(LNode node, TextWriter target, string indentString = "\t", string lineSeparator = "\n")
		{
			var wr = new EcsNodePrinterWriter(target, indentString, lineSeparator);
			return new EcsNodePrinter(node, wr);
		}
		
		[ThreadStatic]
		static EcsNodePrinter _printer;
		public static readonly LNodePrinter Printer = PrintECSharp;
		static bool _isDebugging = System.Diagnostics.Debugger.IsAttached;

		public static void PrintPlainCSharp(LNode node, StringBuilder target, IMessageSink errors, object mode, string indentString, string lineSeparator)
		{
			var p = new EcsNodePrinter(node, new EcsNodePrinterWriter(target, indentString, lineSeparator));
			p.Errors = errors;
			p.SetPlainCSharpMode();
			p.PrintWithMode(node, mode);
		}
		static void PrintECSharp(LNode node, StringBuilder target, IMessageSink errors, object mode, string indentString, string lineSeparator)
		{
			var p = _printer;
			// When debugging the node printer itself, calling LNode.ToString() 
			// inside the debugger will trash our current state, unless we 
			// create a new printer for each printing operation.
			if (p == null || _isDebugging)
				_printer = p = new EcsNodePrinter(null, null);

			p.Print(node, target, errors, mode, indentString, lineSeparator);
			p._n = null;
			p._out = null;
			p.Errors = null;
		}
		public void Print(LNode node, StringBuilder target, IMessageSink errors, object mode, string indentString, string lineSeparator)
		{
			Writer = new EcsNodePrinterWriter(target, indentString, lineSeparator);
			Errors = errors;
			PrintWithMode(node, mode);
		}
		void PrintWithMode(LNode node, object mode)
		{
			this.Node = node;
			var style = (mode is NodeStyle ? (NodeStyle)mode : NodeStyle.Default);
			if (mode == ParsingService.Exprs)
				style = NodeStyle.Expression;

			switch (style & NodeStyle.BaseStyleMask) {
				case NodeStyle.Expression: this.PrintExpr(); break;
				case NodeStyle.PrefixNotation: this.PrintPrefixNotation(StartExpr, true, 0); break;
				default: this.PrintStmt(); break;
			}
		}

		public EcsNodePrinter(LNode node, INodePrinterWriter target)
		{
			_n = node;
			_out = target;
			SpaceOptions = SpaceOpt.Default;
			NewlineOptions = NewlineOpt.Default;
			SpaceAroundInfixStopPrecedence = EP.Power.Lo;
			SpaceAfterPrefixStopPrecedence = EP.Prefix.Lo;
			AllowChangeParentheses = true;
		}

		#endregion

		#region Configuration properties

		/// <summary>Allows operators to be mixed that will cause the parser to 
		/// produce a warning. An example is <c>x &amp; @==(y, z)</c>: if you enable 
		/// this option, it will be printed as <c>x &amp; y == z</c>, which the parser
		/// will complain about because mixing those operators is deprecated.
		/// </summary>
		public bool MixImmiscibleOperators { get; set; }
		
		/// <summary>Permits extra parentheses to express precedence, instead of
		/// resorting to prefix notation (defaults to true). Also permits removal
		/// of parenthesis if necessary to print special constructs.</summary>
		/// <remarks>For example, the Loyc tree <c>x * @+(a, b)</c> will be printed 
		/// <c>x * (a + b)</c>. Originally, the second tree had a significantly 
		/// different structure from the first, as parenthesis were represented
		/// by a call to the empty symbol @``. This was annoyingly restrictive, so 
		/// I reconsidered the design; now, parenthesis will be represented only by 
		/// a trivia attribute #trivia_inParens, so adding new parenthesis no longer
		/// changes the Loyc tree in an important way, so the default has changed
		/// from false to true (except in the test suite).
		/// </remarks>
		public bool AllowChangeParentheses { get; set; }

		/// <summary>Solve if-else ambiguity by adding braces rather than reverting 
		/// to prefix notation.</summary>
		/// <remarks>
		/// For example, the tree <c>#if(c1, #if(c2, x++), y++)</c> will be parsed 
		/// incorrectly if it is printed <c>if (c1) if (c2) x++; else y++;</c>. This
		/// problem can be resolved either by adding braces around <c>if (c2) x++;</c>,
		/// or by printing <c>#if(c2, x++)</c> in prefix notation.
		/// </remarks>
		public bool AllowExtraBraceForIfElseAmbig { get; set; }

		/// <summary>Suppresses printing of all attributes that are not on 
		/// declaration or definition statements (such as classes, methods and 
		/// variable declarations at statement level). Also, avoids prefix notation 
		/// when the attributes would have required it, e.g. <c>@+([Foo] a, b)</c> 
		/// can be printed "a+b" instead.</summary>
		/// <remarks>This also affects the validation methods such as <see 
		/// cref="IsVariableDecl"/>. With this flag, validation methods will ignore
		/// attributes in locations where they don't belong instead of returning
		/// false.</remarks>
		public bool DropNonDeclarationAttributes { get; set; }
		
		/// <summary>When an argument to a method or macro has an empty name (@``),
		/// it will be omitted completely if this flag is set.</summary>
		public bool OmitMissingArguments { get; set; }

		/// <summary>When this flag is set, space trivia attributes are ignored
		/// (e.g. <see cref="CodeSymbols.TriviaSpaceAfter"/>).</summary>
		/// <remarks>Note: since EcsNodePrinter inserts its own spaces 
		/// automatically, space trivia (if any) may be redundant unless you set 
		/// <see cref="SpaceOptions"/> and/or <see cref="NewlineOptions"/> to zero.</remarks>
		public bool OmitSpaceTrivia { get; set; }

		/// <summary>When this flag is set, comment trivia attributes are ignored
		/// (e.g. <see cref="CodeSymbols.TriviaSLCommentAfter"/>).</summary>
		public bool OmitComments { get; set; }

		/// <summary>When this flag is set, raw text trivia attributes are ignored
		/// (e.g. <see cref="CodeSymbols.TriviaRawTextBefore"/>).</summary>
		public bool OmitRawText { get; set; }

		/// <summary>When the printer encounters an unprintable literal, it calls
		/// Value.ToString(). When this flag is set, the string is placed in double
		/// quotes; when this flag is clear, it is printed as raw text.</summary>
		public bool QuoteUnprintableLiterals { get; set; }

		/// <summary>Causes the ambiguity between constructors and method calls to
		/// be ignored; see <see cref="EcsPrinterAndParserTests.ConstructorAmbiguities()"/>.</summary>
		public bool AllowConstructorAmbiguity { get; set; }

		/// <summary>Prints statements like "foo (...) bar()" in the equivalent form
		/// "foo (..., bar())" instead. Does not affect foo {...} because property
		/// and event definitions require this syntax (get {...}, set {...}).</summary>
		public bool AvoidMacroSyntax { get; set; }

		/// <summary>Prefers plain C# syntax for certain other things (not covered
		/// by the other options), even when the syntax tree requests a different 
		/// style, e.g. EC# cast operators are blocked so x(->int) becomes (int) x.</summary>
		public bool PreferPlainCSharp { get; set; }

		/// <summary>Controls the locations where spaces should be emitted.</summary>
		public SpaceOpt SpaceOptions { get; set; }
		/// <summary>Controls the locations where newlines should be emitted.</summary>
		public NewlineOpt NewlineOptions { get; set; }
		
		public int SpaceAroundInfixStopPrecedence { get; set; }
		public int SpaceAfterPrefixStopPrecedence { get; set; }

		/// <summary>Sets <see cref="AllowChangeParentheses"/>, <see cref="PreferPlainCSharp"/> 
		/// and <see cref="DropNonDeclarationAttributes"/> to true.</summary>
		/// <returns>this.</returns>
		public EcsNodePrinter SetPlainCSharpMode()
		{
			AllowChangeParentheses = true;
			AllowExtraBraceForIfElseAmbig = true;
			DropNonDeclarationAttributes = true;
			AllowConstructorAmbiguity = true;
			AvoidMacroSyntax = true;
			PreferPlainCSharp = true;
			return this;
		}
		
		#endregion

		#region Printing helpers: Indented, With(), WithType(), Space(), etc.

		struct Indented_ : IDisposable
		{
			EcsNodePrinter _self;
			public Indented_(EcsNodePrinter self) { _self = self; self._out.Indent(); }
			public void Dispose() { _self._out.Dedent(); }
		}
		struct With_ : IDisposable
		{
			internal EcsNodePrinter _self;
			LNode _old;
			public With_(EcsNodePrinter self, LNode inner)
			{
				_self = self;
				self._out.Push(_old = self._n); 
				if (inner == null) {
					self.Errors.Write(Severity.Error, self._n, "EcsNodePrinter: Encountered null LNode");
					self._n = LNode.Id("(null)");
				} else
					self._n = inner;
			}
			public void Dispose()
			{
				_self._out.Pop(_self._n = _old);
			}
		}
		struct WithSpace_ : IDisposable
		{
			internal EcsNodePrinter _self;
			Symbol _oldSpace;
			public WithSpace_(EcsNodePrinter self, Symbol spaceName)
			{
				_self = self;
				_oldSpace = self._spaceName;
				_self._spaceName = spaceName;
			}
			public void Dispose()
			{
				_self._spaceName = _oldSpace;
			}
		}

		Indented_ Indented { get { return new Indented_(this); } }
		With_ With(LNode inner) { return new With_(this, inner); }
		WithSpace_ WithSpace(Symbol spaceName) { return new WithSpace_(this, spaceName); }
		
		void PrintInfixWithSpace(Symbol name, Precedence p, Ambiguity flags)
		{
			if (p.Lo < SpaceAroundInfixStopPrecedence && (name != S.DotDot || (SpaceOptions & SpaceOpt.SuppressAroundDotDot) == 0)) {
				_out.Space();
				WriteOperatorName(name, flags);
				_out.Space();
			} else {
				WriteOperatorName(name, flags);
			}
		}

		void PrefixSpace(Precedence p)
		{
			if (p.Lo < SpaceAfterPrefixStopPrecedence)
				_out.Space();
		}
		void Space(SpaceOpt context)
		{
			if ((SpaceOptions & context) != 0)
				_out.Space();
		}
		void WriteThenSpace(char ch, SpaceOpt option)
		{
			_out.Write(ch, true);
			if ((SpaceOptions & option) != 0)
				_out.Space();
		}
		enum ParenFor {
			Grouping = (int)SpaceOpt.OutsideParens,
			MethodCall = (int)SpaceOpt.BeforeMethodCall,
			MacroCall = (int)SpaceOpt.BeforePossibleMacroArgs,
			KeywordCall = (int)SpaceOpt.BeforeKeywordStmtArgs,
			NewCast = (int)SpaceOpt.BeforeNewCastCall,
			MethodDecl = (int)SpaceOpt.BeforeMethodDeclArgList,
		}
		bool WriteOpenParen(ParenFor type, bool confirm = true)
		{
			if (confirm) {
				if (((int)SpaceOptions & (int)type) != 0)
					_out.Space();
				_out.Write('(', true);
				WriteInnerSpace(type);
			}
			return confirm;
		}
		void WriteCloseParen(ParenFor type, bool confirm = true)
		{
			if (confirm)
			{
				WriteInnerSpace(type);
				_out.Write(')', true);
				if (((int)SpaceOptions & (int)SpaceOpt.OutsideParens & (int)type) != 0)
					_out.Space();
			}
		}
		void PrintWithinParens(ParenFor type, LNode n, Ambiguity flags = 0)
		{
			WriteOpenParen(type);
			PrintExpr(n, StartExpr, flags);
			WriteCloseParen(type);
		}

		private void WriteInnerSpace(ParenFor type)
		{
			if ((SpaceOptions & (SpaceOpt.InsideParens | SpaceOpt.InsideCallParens)) != 0)
				if ((type & (ParenFor.Grouping | ParenFor.MethodCall | ParenFor.MacroCall)) != 0)
					if ((SpaceOptions & (type == ParenFor.Grouping ? SpaceOpt.InsideParens : SpaceOpt.InsideCallParens)) != 0)
						_out.Space();
		}
		private bool Newline(NewlineOpt context)
		{
			if ((NewlineOptions & context) != 0) {
				_out.Newline();
				return true;
			}
			return false;
		}

		#endregion

		/// <summary>Internal enum (marked public for an obscure technical reason). 
		/// These are flags that represent special situations in EC# syntax.</summary>
		[Flags] public enum Ambiguity
		{
			/// <summary>The expression can contain uninitialized variable 
			/// declarations, e.g. because it is the subject of an assignment.
			/// In the tree "(x + y, int z) = (a, b)", this flag is passed down to 
			/// "(x + y, int z)" and then down to "int y" and "x + y", but it 
			/// doesn't propagate down to "x", "y" and "int".</summary>
			AllowUnassignedVarDecl = 0x0001,
			/// <summary>The expression is the right side of a traditional cast, so 
			/// the printer must avoid ambiguity in case of the following prefix 
			/// operators: <c>(Foo)&amp;x, (Foo)*x, (Foo)++(x), (Foo)--(x)</c> 
			/// (the (Foo)++(x) case is parsed as a post-increment and a call).</summary>
			CastRhs = 0x0002,
			/// <summary>The expression is in a location where, if it is parenthesized
			/// and has the syntax of a data type inside, it will be treated as a cast.
			/// This occurs when a call that is printed with prefix notation has a 
			/// parenthesized target node, e.g. (target)(arg). The target node can avoid 
			/// the syntax of a data type by adding "[ ]" (an empty set of 
			/// attributes) at the beginning of the expression.</summary>
			IsCallTarget = 0x0004,
			/// <summary>No braced block permitted directly here (inside "if" clause)</summary>
			NoBracedBlock = 0x0008,
			/// <summary>The current statement is the last one in the enclosing 
			/// block, so #result can be represented by omitting a semicolon.</summary>
			FinalStmt = 0x0010,
			/// <summary>An expression is being printed in a context where a type
			/// is expected (its syntax has been verified in advance.)</summary>
			TypeContext = 0x0020,
			/// <summary>The expression being printed is a complex identifier that
			/// may contain special attributes, e.g. <c>Foo&lt;out T></c>.</summary>
			InDefinitionName = 0x0040,
			/// <summary>Inside angle brackets or (of ...).</summary>
			InOf = 0x0080,
			/// <summary>Allow pointer notation (when combined with TypeContext). 
			/// Also, a pointer is always allowed at the beginning of a statement,
			/// which is detected by the precedence context (StartStmt).</summary>
			AllowPointer = 0x0100,
			/// <summary>Used to communicate to the operator printers that a binary 
			/// call should be expressed with the backtick operator.</summary>
			UseBacktick = 0x0400,
			/// <summary>Drop attributes only on the immediate expression being 
			/// printed. Used when printing the return type on a method, whose 
			/// attributes were already described by <c>[return: ...]</c>.</summary>
			DropAttributes = 0x0800,
			/// <summary>Forces a variable declaration to be allowed as the 
			/// initializer of a foreach loop.</summary>
			ForEachInitializer = 0x1000,
			/// <summary>After 'else', valid 'if' statements are not indented.</summary>
			ElseClause = 0x2000,
			/// <summary>Print #this(...) as this(...) inside a method</summary>
			AllowThisAsCallTarget = 0x8000,
			/// <summary>This location is the 'true' side of an if-else statement.
			/// At this location, no 'if' without 'else' is allowed because the
			/// outer else would, upon parsing, be associated with the inner 'if'.</summary>
			NoIfWithoutElse = 0x10000,
			/// <summary>Avoids printing illegal opening paren at statement level</summary>
			NoParenthesis = 0x20000,
		}

		// Creates an open delegate (technically it could create a closed delegate
		// too, but there's no need to use reflection for that)
		static D OpenDelegate<D>(string name)
		{
			return (D)(object)Delegate.CreateDelegate(typeof(D), typeof(EcsNodePrinter).GetMethod(name));
		}

		#region Sets and dictionaries of keywords and tokens

		static readonly HashSet<Symbol> PreprocessorCollisions = SymbolSet(
			"#if", "#else", "#elif", "#endif", "#define", "#undef",
			"#region", "#endregion", "#pragma", "#error", "#warning", "#note", "#line"
		);

		internal static readonly HashSet<Symbol> OperatorIdentifiers = SymbolSet(
			// >>, << and ** are special: the lexer provides them as two separate tokens
			"~", "!", "%", "^", "&", "&&", "*", "**", "+", "++", 
			"-", "--", "=", "==", "!=", /*"{}", "[]",*/ "|", "||", @"\", 
			";", ":", ",", ".", "..", "<", "<<", ">", ">>", "/", 
			"?", "??", "?.", "??=", "%=", "^=", "&=", "*=", "-=", 
			"+=", "|=", "<=", ">=", "=>", "==>", "->", "$", ">>=", "<<="
		);

		internal static readonly HashSet<Symbol> CsKeywords = SymbolSet(
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

		internal static readonly Dictionary<Symbol, string> TypeKeywords = Dictionary(
			P(S.Void, "void"),  P(S.Object, "object"), P(S.Bool, "bool"), P(S.Char, "char"), 	
			P(S.Int8, "sbyte"), P(S.UInt8, "byte"), P(S.Int16, "short"), P(S.UInt16, "ushort"), 
			P(S.Int32, "int"), P(S.UInt32, "uint"), P(S.Int64, "long"), P(S.UInt64, "ulong"), 
			P(S.Single, "float"), P(S.Double, "double"), P(S.String, "string"), P(S.Decimal, "decimal")
		);

		static readonly Dictionary<Symbol, string> KeywordStmts = KeywordDict(
			"break", "case", "checked", "continue", "default",  "do", "fixed", 
			"for", "foreach", "goto", "if", "lock", "return", "switch", "throw", "try",
			"unchecked", "using", "while", "enum", "struct", "class", "interface", 
			"namespace", "trait", "alias", "event", "delegate", "goto case");

		
		internal static HashSet<Symbol> SymbolSet(params string[] input)
		{
			return new HashSet<Symbol>(input.Select(s => GSymbol.Get(s)));
		}
		static Dictionary<Symbol, string> KeywordDict(params string[] input)
		{
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

		#region Validation helpers (note: most of them were moved to EcsValidators)
		// These are validators for printing purposes: they check that each node 
		// that shouldn't have attributes, doesn't; if attributes are present in
		// strange places then we print with prefix notation instead to avoid 
		// losing them when round-tripping.

		bool HasPAttrs(LNode node) // for use in expression context
		{
			return !DropNonDeclarationAttributes && node.HasPAttrs();
		}
		bool HasSimpleHeadWPA(LNode self)
		{
			return DropNonDeclarationAttributes ? self.HasSimpleHead() : self.HasSimpleHeadWithoutPAttrs();
		}
		bool CallsWPAIH(LNode self, Symbol name)
		{
			return self.Calls(name) && HasSimpleHeadWPA(self);
		}
		bool CallsMinWPAIH(LNode self, Symbol name, int argCount)
		{
			return self.CallsMin(name, argCount) && HasSimpleHeadWPA(self);
		}
		bool CallsWPAIH(LNode self, Symbol name, int argCount)
		{
			return self.Calls(name, argCount) && HasSimpleHeadWPA(self);
		}
		bool IsSimpleSymbolWPA(LNode self)
		{
			return self.IsId && !HasPAttrs(self);
		}
		bool IsSimpleSymbolWPA(LNode self, Symbol name)
		{
			return self.Name == name && IsSimpleSymbolWPA(self);
		}

		EcsValidators.Pedantics Pedantics {
			get {
				return
					(DropNonDeclarationAttributes ? EcsValidators.Pedantics.IgnoreWeirdAttributes : 0) |
					(AllowChangeParentheses ? EcsValidators.Pedantics.IgnoreIllegalParentheses : 0);
			}
		}

		bool IsVariableDecl(bool allowMultiple, bool allowNoAssignment) // for printing purposes
		{
			return EcsValidators.IsVariableDecl(_n, allowMultiple, allowNoAssignment, Pedantics);
		}

		bool IsComplexIdentifier(LNode n, ICI f = ICI.Default)
		{
			return EcsValidators.IsComplexIdentifier(n, f, Pedantics);
		}

		bool IsResultExpr(LNode n, bool allowAttrs = false)
		{
			return CallsWPAIH(n, S.Result, 1) && (allowAttrs || !HasPAttrs(n));
		}

		#endregion

		#region Parts of expressions: attributes, identifiers, literals, trivia

		enum AttrStyle {
			NoKeywordAttrs,    // Put all attributes in square brackets
			AllowKeywordAttrs, // e.g. [#public, #const] written as "public const", allowed on any expression
			IsConstructor,     // same as AllowKeywordAttrs, except that attrs are not blocked by DropNonDeclarationAttributes
			AllowWordAttrs,    // e.g. [#partial, #phat] written as "partial phat", allowed on keyword-stmts (for, if, etc.); also allows [#this]
			IsDefinition,      // allows word attributes plus "new" (only on definitions: methods, var decls, events...)
		};
		// Returns the number of opening "("s printed that require a corresponding ")".
		private int PrintAttrs(Precedence context, AttrStyle style, Ambiguity flags, LNode skipClause = null, string label = null)
		{
			return PrintAttrs(ref context, style, flags, skipClause, label);
		}
		private int PrintAttrs(ref Precedence context, AttrStyle style, Ambiguity flags, LNode skipClause = null, string label = null)
		{
			int haveParens = PrintPrefixTrivia(flags);

			do {
				Debug.Assert(label == null || style == AttrStyle.NoKeywordAttrs);

				if ((flags & Ambiguity.DropAttributes) != 0)
					break;

				bool isTypeParamDefinition = (flags & (Ambiguity.InDefinitionName | Ambiguity.InOf))
				                                   == (Ambiguity.InDefinitionName | Ambiguity.InOf);
				// 'div' will be used to divide normal attributes from keyword/word 
				// attributes (the word attributes will be `_n.Attrs.Slice(div)`)
				int div = _n.AttrCount, attrCount = div;
				if (div == 0)
					break;

				bool beginningOfStmt = context.RangeEquals(StartStmt);
				bool needParens = !beginningOfStmt && !context.RangeEquals(StartExpr);
				if (isTypeParamDefinition) {
					for (; div > 0; div--) {
						var a = _n.Attrs[div - 1];
						var n = a.Name;
						if (!IsSimpleSymbolWPA(a) || (n != S.In && n != S.Out))
							if (n != S.Where)
								break;
					}
					needParens = div != 0;
				} else if (needParens) {
					if (!HasPAttrs(_n))
						break;
				} else {
					if (style != AttrStyle.NoKeywordAttrs) {
						// Choose how much of the attributes to treat as simple words, 
						// e.g. we prefer to print [Flags, #public] as "[Flags] public"
						bool isVarDecl = _n.Name == S.Var;
						for (; div > 0; div--)
							if (!IsWordAttribute(_n.Attrs[div - 1], style))
								break;
					}
				}

				// When an attribute appears mid-expression (which the printer 
				// may try to avoid through the use of prefix notation), we need 
				// to write "(" in order to group the attribute(s) with the
				// expression to which they apply, e.g. while "[A] x + y" has an
				// attribute attached to "+", the attribute in "([A] x) + y" is
				// attached to x.
				if (needParens && haveParens == 0) {
					haveParens++;
					context = StartExpr;
					WriteOpenParen(ParenFor.Grouping);
				}

				bool any = false;
				bool dropAttrs = false;
				if (DropNonDeclarationAttributes && style < AttrStyle.IsDefinition && style != AttrStyle.IsConstructor) {
					// Careful: avoid dropping attributes from get; set; and things that look like macro calls
					if (!beginningOfStmt || _n.IsCall && (_n.ArgCount == 0 || !_n.Args.Last.Calls(S.Braces)))
						dropAttrs = true;
				}
				if (!dropAttrs && div > 0) {
					for (int i = 0; i < div; i++) {
						var a = _n.Attrs[i];
						if (a.IsTrivia || a == skipClause)
							continue;
						if (any)
							WriteThenSpace(',', SpaceOpt.AfterComma);
						else {
							WriteThenSpace('[', SpaceOpt.InsideAttribute);
							if (label != null) {
								_out.Write(label, true);
								_out.Write(':', true);
								Space(SpaceOpt.AfterColon);
							}
						}
						any = true;
						PrintExpr(a, StartExpr);
					}
					if (any) {
						Space(SpaceOpt.InsideAttribute);
						_out.Write(']', true);
						if (!beginningOfStmt || !Newline(NewlineOpt.AfterAttributes))
							Space(SpaceOpt.AfterAttribute);
					}
				}

				// And now the word attributes...
				for (int i = div; i < attrCount; i++) {
					var a = _n.Attrs[i];
					if (a == skipClause)
						continue;
					string text;
					if (AttributeKeywords.TryGetValue(a.Name, out text)) {
						if (dropAttrs && a.Name != S.Out && a.Name != S.Ref)
							continue;
						_out.Write(text, true);
					} else if (!dropAttrs || a.Name == S.Yield || a.Name == S.In) {
						Debug.Assert(a.HasSpecialName);
						if (a.IsTrivia)
							continue;
						if (isTypeParamDefinition) {
							if (a.Name == S.In)
								_out.Write("in", true); // "out" is listed in AttributeKeywords
							else {
								Debug.Assert(a.Name == S.Where);
								continue;
							}
						} else {
							if (a.Name == S.This) // special case: avoid printing "@this"
								_out.Write("this", true);
							else
								PrintSimpleIdent(GSymbol.Get(a.Name.Name.Substring(1)), 0, false);
						}
					}
					//any = true;
					Space(SpaceOpt.Default);
				}
			} while(false);

			if (haveParens != 0) {
				context = StartExpr;

				// Avoid cast ambiguity, e.g. for the method call x(y), represented 
				// (x)(y) where x is in parenthesis, must not be printed like that 
				// because it would be parsed as a cast. Use ([] x)(y) or ((x))(y) 
				// instead.
				if (haveParens == 1 && (flags & Ambiguity.IsCallTarget) != 0
					&& IsComplexIdentifier(_n, ICI.Default | ICI.AllowAnyExprInOf | ICI.AllowParensAround)) {
					if (AllowChangeParentheses) {
						haveParens++;
						_out.Write('(', true);
					} else
						_out.Write("[ ] ", true);
				}
			}
			return haveParens;
		}

		private int PrintPrefixTrivia(Ambiguity flags)
		{
			int haveParens = 0;
			foreach (var attr in _n.Attrs) {
				var name = attr.Name;
				if (name.Name.TryGet(0, '\0') == '#') {
					if (name == S.TriviaSpaceBefore && !OmitSpaceTrivia) {
						var tValue = attr.TriviaValue;
						PrintSpaces(GetRawText(attr));
					} else if (name == S.TriviaInParens) {
						if ((flags & Ambiguity.NoParenthesis) == 0) {
							WriteOpenParen(ParenFor.Grouping);
							haveParens++;
						}
					} else if (name == S.TriviaRawTextBefore && !OmitRawText) {
						WriteRawText(GetRawText(attr));
					} else if (name == S.TriviaSLCommentBefore && !OmitComments) {
						_out.Write("//", false);
						_out.Write(GetRawText(attr), true);
						_out.Newline(true);
					} else if (name == S.TriviaMLCommentBefore && !OmitComments) {
						_out.Write("/*", false);
						_out.Write(GetRawText(attr), false);
						_out.Write("*/", false);
						Space(SpaceOpt.BetweenCommentAndNode);
					}
				}
			}
			return haveParens;
		}

		private void PrintSuffixTrivia(bool needSemicolon)
		{
			if (needSemicolon)
				_out.Write(';', true);

			bool spaces = false;
			foreach (var attr in _n.Attrs) {
				var name = attr.Name;
				if (name.Name.TryGet(0, '\0') == '#') {
					if (name == S.TriviaSpaceAfter && !OmitSpaceTrivia) {
						PrintSpaces((attr.HasValue ? attr.Value ?? "" : "").ToString());
						spaces = true;
					} else if (name == S.TriviaRawTextAfter && !OmitRawText) {
						WriteRawText(GetRawText(attr));
					} else if (name == S.TriviaSLCommentAfter && !OmitComments) {
						if (!spaces)
							Space(SpaceOpt.BeforeCommentOnSameLine);
						_out.Write("//", false);
						_out.Write(GetRawText(attr), true);
						_out.Newline(true);
						spaces = true;
					} else if (name == S.TriviaMLCommentAfter && !OmitComments) {
						if (!spaces)
							Space(SpaceOpt.BeforeCommentOnSameLine);
						_out.Write("/*", false);
						_out.Write((attr.Value ?? "").ToString(), false);
						_out.Write("*/", false);
						spaces = false;
					}
				}
			}
		}

		private void WriteRawText(string text)
		{
			if (text.EndsWith("\n")) {
				// use our own newline logic so indentation works
				_out.Write(text.Substring(0, text.Length - 1), true);
				_out.Newline(true);
			} else
				_out.Write(text, true);
		}

		private void PrintSpaces(string spaces)
		{
			for (int i = 0; i < spaces.Length; i++) {
				char c = spaces[i];
				if (c == ' ' || c == '\t')
					_out.Write(c, false);
				else if (c == '\n')
					_out.Newline();
				else if (c == '\r') {
					_out.Newline();
					if (spaces.TryGet(i + 1) == '\r')
						i++;
				}
			}
		}

		static bool IsWordAttribute(LNode node, AttrStyle style)
		{
			if (node.IsCall || node.HasPAttrs() || !node.HasSpecialName)
				return false;
			else {
				if (AttributeKeywords.ContainsKey(node.Name))
					return node.Name != S.New || style >= AttrStyle.IsDefinition;
				else
					return style >= AttrStyle.AllowWordAttrs && (node.Name == S.This || 
						!CsKeywords.Contains(GSymbol.Get(node.Name.Name.Substring(1))));
			}
		}

		static readonly Symbol Var = GSymbol.Get("var"), Def = GSymbol.Get("def");

		[ThreadStatic] static StringBuilder _staticStringBuilder;
		[ThreadStatic] static EcsNodePrinterWriter _staticWriter;
		[ThreadStatic] static EcsNodePrinter _staticPrinter;

		private static void InitStaticInstance()
		{
			if (_staticPrinter == null) {
				var sb = _staticStringBuilder = new StringBuilder();
				var wr = _staticWriter = new EcsNodePrinterWriter(sb);
				_staticPrinter = new EcsNodePrinter(null, wr);
			} else {
				_staticWriter.Reset();
				_staticStringBuilder.Length = 0; // Clear() is new in .NET 4
			}
		}
		public static string PrintId(Symbol name, bool useOperatorKeyword = false)
		{
			InitStaticInstance();
			_staticPrinter.PrintSimpleIdent(name, 0, false, useOperatorKeyword);
			return _staticStringBuilder.ToString();
		}

		public static string PrintSymbolLiteral(Symbol name)
		{
			InitStaticInstance();
			_staticStringBuilder.Append("@@");
			_staticPrinter.PrintSimpleIdent(name, 0, true);
			return _staticStringBuilder.ToString();
		}

		public static string PrintLiteral(object value, NodeStyle style)
		{
			InitStaticInstance();
			_staticPrinter._n = LNode.Literal(value, null, -1, -1, style);
			_staticPrinter.PrintLiteral();
			return _staticStringBuilder.ToString();
		}

		public static string PrintString(string value, char quoteType, bool verbatim = false, bool includeAtSign = true)
		{
			InitStaticInstance();
			_staticPrinter.PrintString(value, quoteType, verbatim ? _Verbatim : null, includeAtSign);
			return _staticStringBuilder.ToString();
		}

		private void PrintSimpleIdent(Symbol name, Ambiguity flags, bool inSymbol = false, bool useOperatorKeyword = false)
		{
			if (useOperatorKeyword)
			{
				_out.Write("operator", true);
				Space(SpaceOpt.AfterOperatorKeyword);
				if (OperatorIdentifiers.Contains(name)) {
					_out.Write(name.Name, true);
				} else
					PrintString(name.Name, '`', null, true);
				return;
			}

			if (name.Name == "") {
				_out.Write(inSymbol ? "``" : "@``", true);
				return;
			}

			// Check if this is a 'normal' identifier and not a keyword:
			char first = name.Name[0];
			if (first == '#' && !inSymbol) {
				// Check for keywords like #this and #int32 that should be printed without '#'
				if (name == S.This && ((flags & Ambiguity.IsCallTarget) == 0 || (flags & Ambiguity.AllowThisAsCallTarget) != 0)) {
					_out.Write("this", true);
					return;
				}
				if (name == S.Base) {
					_out.Write("base", true);
					return;
				}
				string keyword;
				if (TypeKeywords.TryGetValue(name, out keyword)) {
					_out.Write(keyword, true);
					return;
				}
			}

			// Detect special characters that demand backquotes
			for (int i = 0; i < name.Name.Length; i++)
			{
				char c = name.Name[i];
				// NOTE: I tried printing things like @* without backquotes, but
				// then @* <Foo> printed like @*<Foo>, which lexes wrong. 
				if (!IsIdentContChar(c)) {
					// Backquote required for this identifier.
					if (!inSymbol)
						_out.Write("@", false);
					PrintString(name.Name, '`', null, true);
					return;
				}
			}
			
			// Print @ if needed, then the identifier
			if (!inSymbol) {
				if (!IsIdentStartChar(first) || PreprocessorCollisions.Contains(name) || CsKeywords.Contains(name) || name == Var || name == Def)
					_out.Write("@", false);
			}
			_out.Write(name.Name, true);
		}

		static readonly Symbol _Verbatim = GSymbol.Get("#trivia_verbatim");
		static readonly Symbol _DoubleVerbatim = S.TriviaDoubleVerbatim;
		private void PrintString(string text, char quoteType, Symbol verbatim, bool includeAtSign = false)
		{
			if (includeAtSign && verbatim != null)
				_out.Write(verbatim == S.TriviaDoubleVerbatim ? "@@" : "@", false);

			_out.Write(quoteType, false);
			if (verbatim != null) {
				for (int i = 0; i < text.Length; i++) {
					if (text[i] == quoteType)
						_out.Write(quoteType, false);
					if (verbatim == S.TriviaDoubleVerbatim && text[i] == '\n')
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
			{ return Pair.Create(typeof(T).TypeHandle, value); }
		static Dictionary<RuntimeTypeHandle,Action<EcsNodePrinter>> LiteralPrinters = Dictionary(
			P<int>    (np => np.PrintIntegerToString("")),
			P<long>   (np => np.PrintIntegerToString("L")),
			P<uint>   (np => np.PrintIntegerToString("u")),
			P<ulong>  (np => np.PrintIntegerToString("uL")),
			P<short>  (np => np.PrintIntegerToString("(->short)")),  // Unnatural. Not produced by parser.
			P<ushort> (np => np.PrintIntegerToString("(->ushort)")), // Unnatural. Not produced by parser.
			P<sbyte>  (np => np.PrintIntegerToString("(->sbyte)")),  // Unnatural. Not produced by parser.
			P<byte>   (np => np.PrintIntegerToString("(->byte)")),   // Unnatural. Not produced by parser.
			P<double> (np => {
				double n = ((double)np._n.Value);
				np.PrintValueToString(System.Math.Floor(n) == n ? "d" : "");
			}),
			P<float>  (np => np.PrintValueToString("f")),
			P<decimal>(np => np.PrintValueToString("m")),
			P<bool>   (np => np._out.Write((bool)np._n.Value ? "true" : "false", true)),
			P<@void>  (np => np._out.Write("default(void)", true)),
			P<char>   (np => np.PrintString(np._n.Value.ToString(), '\'', null)),
			P<string> (np => {
				var v1 = np._n.AttrNamed(_DoubleVerbatim);
				var v2 = v1 != null ? v1.Name : ((np._n.Style & NodeStyle.Alternate) != 0 ? _Verbatim : null);
				np.PrintString(np._n.Value.ToString(), '"', v2, true);
			}),
			P<Symbol> (np => {
				np._out.Write("@@", false);
				np.PrintSimpleIdent((Symbol)np._n.Value, 0, true);
			}),
			P<TokenTree> (np => {
				np._out.Write("@[", true);
				np._out.Write(((TokenTree)np._n.Value).ToString(Ecs.Parser.TokenExt.ToString), true);
				np._out.Write(" ]", true);
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
			else {
				Errors.Write(Severity.Error, _n, "EcsNodePrinter: Encountered unprintable literal of type '{0}'", _n.Value.GetType().Name);
				bool quote = QuoteUnprintableLiterals;
				string unprintable;
				try {
					unprintable = _n.Value.ToString();
				} catch (Exception ex) {
					unprintable = ex.Message;
					quote = true;
				}
				if (quote)
					PrintString(unprintable, '"', null);
				else
					_out.Write(unprintable, true);
			}
		}

		public static bool IsPlainCsIdentStartChar(char c)
		{
 			return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_' || (c > 128 && char.IsLetter(c));
		}
		public static bool IsIdentStartChar(char c)
		{
			return IsPlainCsIdentStartChar(c) || c == '#';
		}
		public static bool IsIdentContChar(char c)
		{
			return IsIdentStartChar(c) || (c >= '0' && c <= '9') || c == '\'';
		}
		//static readonly char[] _operatorChars =
		//{
		//    '~', '!', '%', '^', '&', '*', '\\', '-', '+', '=', '|', '<', '>', '/', '?', ':', '.', '$'
		//};
		//public static bool IsOperatorChar(char c)
		//{
		//    return _operatorChars.Contains(c);
		//}

		/// <summary>Eliminates punctuation and special characters from a string so
		/// that the string can be used as a plain C# identifier, e.g. 
		/// "I'd" => "I_aposd", "123" => "_123", "+5" => "_plus5".</summary>
		/// <remarks>The empty string "" becomes "__empty__", ASCII punctuation becomes 
		/// "_xyz" where xyz is an HTML entity name, e.g. '!' becomes "_excl",
		/// and all other characters become "Xxx" where xx is the hexadecimal 
		/// representation of the code point. Designed for the Unicode BMP only.</remarks>
		public static string SanitizeIdentifier(string id)
		{
			if (id == "")
				return "__empty__";
			int i = 0;
			if (IsPlainCsIdentStartChar(id[0])) {
				for (i = 1; i < id.Length; i++)
					if (!IsPlainCsIdentStartChar(id[i]) && !char.IsDigit(id[i]))
						break;
			}
			if (i >= id.Length)
				return id; // it's a normal identifier, do not change
			
			var sb = new StringBuilder(id.Left(i));
			for (; i < id.Length; i++) {
				char c = id[i];
				if (IsPlainCsIdentStartChar(c))
					sb.Append(c);
				else if (c >= '0' && c <= '9') {
					if (i == 0) sb.Append('_');
					sb.Append(c);
				} else {
					char prefix = '_';
					string ent = G.BareHtmlEntityNameForAscii(c);
					if (ent == null || (c < 256 && ent.Length > 5)) {
						prefix = 'x';
						ent = ((int)c).ToString("X2");
					}
					sb.Append(prefix);
					sb.Append(ent);
				}
			}
			return sb.ToString();
		}


		#endregion
	}

	/// <summary>Flags for <see cref="EcsNodePrinter.IsComplexIdentifier"/>.</summary>
	[Flags] public enum ICI
	{
		Default = 0,
		AllowAttrs = 2, // outer level only. e.g. this flag is used on return types, where
			// #fn([Attr] int, Foo, #()) is printed "[return: Attr] int Foo();"
		// For internal use
		DisallowOf = 8,
		DisallowDotted = 16,
		InOf = 32,
		// hmm
		AllowAnyExprInOf = 64,
		// Allows in out $, e.g. Foo<in A, out B, $c>, but requires type params 
		// to be simple (e.g. Foo<A.B, C<D>> is illegal)
		NameDefinition = 128,
		// allows parentheses around the outside of the complex identifier.
		AllowParensAround = 256,
		// allows expressions like x().y and (x + y).Foo<z>, in which the left side 
		// is an expression but the right side uses the #of or . operator. This 
		// flag also permits any expr in parens (as if AllowParensAround specified)
		ExprMode = 512,
	}

	/// <summary>Controls the locations where spaces appear as <see cref="EcsNodePrinter"/> 
	/// is printing.</summary>
	/// <remarks>
	/// Note: Spaces around prefix and infix operators are controlled by 
	/// <see cref="EcsNodePrinter.SpaceAroundInfixStopPrecedence"/> and
	/// <see cref="EcsNodePrinter.SpaceAfterPrefixStopPrecedence"/>.
	/// </remarks>
	[Flags] public enum SpaceOpt
	{
		Default = AfterComma | AfterCast | AfterAttribute | AfterColon | BeforeKeywordStmtArgs 
			| BeforePossibleMacroArgs | BeforeNewInitBrace | InsideNewInitializer
			| BeforeBaseListColon | BeforeForwardArrow | BeforeConstructorColon
			| BeforeCommentOnSameLine | SuppressAroundDotDot,
		AfterComma              = 0x00000002, // Spaces after normal commas (and ';' in for loop)
		AfterCommaInOf          = 0x00000004, // Spaces after commas between type arguments
		AfterCast               = 0x00000008, // Space after cast target: (Foo) x
		AfterCastArrow          = 0x00000010, // Space after arrow in new-style cast: x(-> Foo)
		AfterAttribute          = 0x00000020, // Space after attribute [Foo] void f();
		InsideParens            = 0x00000080, // Spaces inside non-call parenthesis: ( x )
		InsideCallParens        = 0x00000100, // Spaces inside call parenthesis and indexers: foo( x ) foo[ x ]
		InsideAttribute         = 0x00000200, // Space inside attribute list: [ Foo ] void f();
		OutsideParens           = 0x00000400, // Spaces outside non-call parenthesis: x+ (x) +y
		BeforeKeywordStmtArgs   = 0x00000800, // Space before arguments of keyword statement: while (true)
		BeforePossibleMacroArgs = 0x00001000, // Space before argument list of possible macro: foo (x)
		BeforeMethodCall        = 0x00002000, // Space before argument list of all method calls
		BeforeNewCastCall       = 0x00004000, // Space before target of new-style cast: x (->Foo)
		BeforeNewInitBrace      = 0x00008000, // Space before opening brace in new-expr: new int[] {...}
		AfterColon              = 0x00010000, // Space after colon (named arg)
		InsideNewInitializer    = 0x00020000, // Spaces within braces of new Xyz {...}
		BeforeBaseListColon     = 0x00040000, // Space before colon of list of base classes
		BeforeWhereClauseColon  = 0x00080000, // e.g. where Foo : Bar
		BeforeConstructorColon  = 0x00100000, // e.g. Foo(int x) : base(x)
		BeforeMethodDeclArgList = 0x00200000, // e.g. int Foo (...) { ... }
		BeforeForwardArrow      = 0x00400000, // Space before ==> in method/property definition
		AfterOperatorKeyword    = 0x00800000, // Space after 'operator' keyword: operator ==
		MissingAfterComma       = 0x01000000, // Space after missing node in arg list, e.g. for(; ; ) or foo(, , )
		BeforeCommentOnSameLine = 0x04000000, // Space between a node and a comment printed afterward
		BetweenCommentAndNode   = 0x08000000, // Space between a multiline comment and the node it's attached to
		SuppressAroundDotDot    = 0x10000000, // Override SpaceAroundInfixStopPrecedence and suppress spaces around ..
	}
	[Flags]
	public enum NewlineOpt
	{
		Default = BeforeSpaceDefBrace | BeforeMethodBrace | BeforePropBrace 
			| AfterOpenBraceInNewExpr | BeforeCloseBraceInNewExpr | BeforeCloseBraceInExpr,
		BeforeSpaceDefBrace       = 0x000001, // Newline before opening brace of type definition
		BeforeMethodBrace         = 0x000002, // Newline before opening brace of method body
		BeforePropBrace           = 0x000004, // Newline before opening brace of property definition
		BeforeGetSetBrace         = 0x000008, // Newline before opening brace of property getter/setter
		BeforeSimpleStmtBrace     = 0x000020, // Newline before opening brace of try, get, set, checked, do, etc.
		BeforeExecutableBrace     = 0x000010, // Newline before opening brace of other executable statements
		BeforeSingleSubstmt       = 0x000080, // Newline before single substatement of if, for, while, etc.
		BeforeOpenBraceInExpr     = 0x000100, // Newline before '{' inside an expression, including x=>{...}
		BeforeCloseBraceInExpr    = 0x000200, // Newline before '}' returning to an expression, including x=>{...}
		AfterCloseBraceInExpr     = 0x000400, // Newline after '}' returning to an expression, including x=>{...}
		BeforeOpenBraceInNewExpr  = 0x000800, // Newline before '{' inside a 'new' expression
		AfterOpenBraceInNewExpr   = 0x001000, // Newline after '{' inside a 'new' expression
		BeforeCloseBraceInNewExpr = 0x002000, // Newline before '}' at end of 'new' expression
		AfterCloseBraceInNewExpr  = 0x004000, // Newline after '}' at end of 'new' expression
		AfterEachInitializerInNew = 0x008000, // Newline after each initializer of 'new' expression
		AfterAttributes           = 0x010000, // Newline after attributes attached to a statement
		BeforeWhereClauses        = 0x020000, // Newline before opening brace of type definition
		BeforeEachWhereClause     = 0x040000, // Newline before opening brace of type definition
		BeforeIfClause            = 0x080000, // Newline before "if" clause
		AfterEachEnumValue        = 0x100000, // Newline after each value of an enum
		BeforeConstructorColon    = 0x200000, // Newline before ': this(...)' clause
	}
}
