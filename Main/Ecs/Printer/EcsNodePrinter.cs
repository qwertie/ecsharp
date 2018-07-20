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
using System.Collections;
using Loyc.Collections;

namespace Loyc.Ecs
{
	// This file contains enumerations (ICI, SpaceOpt, NewlineOpt) and miscellaneous
	// code of EcsNodePrinter:
	// - User-configurable options
	// - Sets and dictionaries of keywords and tokens
	// - Code for printing attributes and trivia
	// - Code for printing simple identifiers
	// - Code for printing literals
	// The code for printing expressions and statements is in separate source files
	// (EcsNodePrinter--expressions.cs and EcsNodePrinter--statements.cs).

	/// <summary>Encapsulates the code for printing an <see cref="LNode"/> 
	/// to EC# source code.</summary>
	/// <remarks>
	/// To print EC# code, you not use this class directly. Instead, call 
	/// <see cref="EcsLanguageService.Print(LNode, StringBuilder, IMessageSink, ParsingMode, ILNodePrinterOptions)"/> via 
	/// <see cref="EcsLanguageService.WithPlainCSharpPrinter"/> or via
	/// <see cref="EcsLanguageService.Value"/>. This class does have some static
	/// methods like <see cref="PrintLiteral(object, NodeStyle)"/> and 
	/// <see cref="PrintId"/> that are useful for printing tokens efficiently.
	/// <para/>
	/// This class is designed to faithfully preserve most Loyc trees; almost any 
	/// Loyc tree that can be represented as EC# source code will be represented 
	/// properly by this class, so that it is possible to parse the output text 
	/// back into a Loyc tree equivalent to the one that was printed. Originally 
	/// this class was meant to provide round-tripping from Loyc trees to text and 
	/// back, but Enhanced C# is syntactically very complicated and as a result 
	/// this printer may contain bugs or (for the sake of practicality) minor 
	/// intentional limitations that cause round-tripping not to work in rare 
	/// cases. If you need perfect round-tripping, use LES instead 
	/// (<see cref="LesLanguageService"/>).
	/// <para/>
	/// Only the attributes, head (<see cref="LiteralNode.Value"/>, 
	/// <see cref="IdNode.Name"/> or <see cref="CallNode.Target"/>), and arguments 
	/// of nodes are round-trippable. Superficial properties such as original 
	/// source code locations and the <see cref="LNode.Style"/> are, in 
	/// general, lost, although the printer can faithfully reproduce some (not 
	/// all) <see cref="NodeStyle"/>s (this caveat applies equally to LES). Also, 
	/// any attribute whose Name starts with "#trivia_" may be dropped, because 
	/// these attributes are considered extensions of the NodeStyle. However, the 
	/// style indicated by the #trivia_* attribute will be used if the printer 
	/// recognizes it.
	/// <para/>
	/// For round-tripping to work, there are a couple of restrictions on the 
	/// input tree:
	/// <ol>
	/// <li>Only literals that can exist in C# source code are allowed. For 
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
	/// </remarks>
	public partial class EcsNodePrinter
	{
		LNode _n;
		Symbol _name;
		Precedence _context;
		Ambiguity _flags;
		INodePrinterWriter _out;
		Symbol _spaceName; // for detecting constructor ambiguity
		IMessageSink _errors;

		public INodePrinterWriter Writer { get { return _out; } set { _out = value; } }

		/// <summary>Any error that occurs during printing is printed to this object.</summary>
		public IMessageSink ErrorSink { get { return _errors; } set { _errors = value ?? MessageSink.Null; } }

		EcsPrinterOptions _o;
		public EcsPrinterOptions Options { get { return _o; } }
		public void SetOptions(ILNodePrinterOptions options)
		{
			_o = options as EcsPrinterOptions ?? new EcsPrinterOptions(options);
		}

		#region Constructors, New(), and default Printer

		[ThreadStatic]
		static EcsNodePrinter _printer;
		static bool _isDebugging = System.Diagnostics.Debugger.IsAttached;

		/// <summary>Prints a node while avoiding syntax specific to Enhanced C#.</summary>
		/// <remarks>This does not perform a conversion from EC# to C#. If the 
		/// syntax tree contains code that has no direct C# representation, the EC# 
		/// representation will be printed.</remarks>
		public static void PrintPlainCSharp(LNode node, StringBuilder target, IMessageSink sink, ParsingMode mode, ILNodePrinterOptions options = null)
		{
			var p = new EcsNodePrinter(options);
			p.Options.SetPlainCSharpMode();
			p.Print(node, target, sink, mode);
		}
		
		/// <summary>Prints a node as EC# code.</summary>
		public static void PrintECSharp(LNode node, StringBuilder target, IMessageSink sink, ParsingMode mode, ILNodePrinterOptions options = null)
		{
			var p = _printer;
			// When debugging the node printer itself, calling LNode.ToString() 
			// inside the debugger will trash our current state, unless we 
			// create a new printer for each printing operation.
			if (p == null || _isDebugging)
				_printer = p = new EcsNodePrinter(options);
			else
				p.SetOptions(options);

			p.Print(node, target, sink, mode);
			// ensure stuff is GC'd
			p._n = null;
			p.Writer = null;
		}

		/// <summary>Attaches a new <see cref="StringBuilder"/>, then prints a node 
		/// with <see cref="Print(LNode, ParsingMode)"/>.</summary>
		internal void Print(LNode node, StringBuilder target, IMessageSink sink, ParsingMode mode)
		{
			Writer = new EcsNodePrinterWriter(target, _o.IndentString ?? "\t", _o.NewlineString ?? "\n");
			ErrorSink = sink;
			Print(node, mode);
		}

		/// <summary>Attaches a new <see cref="TextWriter"/>, then prints a node 
		/// with <see cref="Print(LNode, ParsingMode)"/>.</summary>
		internal void Print(LNode node, TextWriter target, IMessageSink sink, ParsingMode mode)
		{
			Writer = new EcsNodePrinterWriter(target, _o.IndentString ?? "\t", _o.NewlineString ?? "\n");
			ErrorSink = sink;
			Print(node, mode);
		}

		/// <summary>Print a node to the writer attached to this printer.</summary>
		/// <param name="mode">A value of <see cref="ParsingMode"/> or <see cref="NodeStyle"/>.
		/// Only three modes are recognized: <see cref="NodeStyle.Expression"/>,
		/// <see cref="NodeStyle.Expression"/>, and <see cref="NodeStyle.Default"/> 
		/// which prints the node as a statement and is the default (i.e. null and
		/// most other values have the same effect)</param>
		internal void Print(LNode node, ParsingMode mode = null)
		{
			if (mode == ParsingMode.Expressions)
				this.PrintExpr(node, StartExpr, 0);
			else
				this.PrintStmt(node, 0);
		}

		internal EcsNodePrinter(ILNodePrinterOptions options = null)
		{
			SetOptions(options);
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
			LNode _old_n;
			Symbol _old_name;
			Precedence _old_context;
			Ambiguity _old_flags;

			public With_(EcsNodePrinter self, LNode inner, Precedence context, Ambiguity flags)
			{
				_self = self;
				self._out.Push(_old_n = self._n);
				_old_name = self._name;
				_old_context = self._context;
				_old_flags = self._flags;
				if (inner == null) {
					self.ErrorSink.Error(self._n, "EC# Printer: Encountered null LNode");
					self._n = LNode.Id("(null)");
					self._name = null;
				} else {
					self._n = inner;
					self._name = inner.Name;
				}
				self._context = context;
				self._flags = flags;
			}
			public void Dispose()
			{
				_self._out.Pop(_self._n = _old_n);
				_self._name = _old_name;
				_self._context = _old_context;
				_self._flags = _old_flags;
			}
		}
		struct WithFlags_ : IDisposable
		{
			internal EcsNodePrinter _self;
			Ambiguity _old_flags;

			public WithFlags_(EcsNodePrinter self, Ambiguity flags)
			{
				_self = self;
				_old_flags = self._flags;
				self._flags = flags;
			}
			public void Dispose()
			{
				_self._flags = _old_flags;
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
		With_ With(LNode inner, Precedence context, Ambiguity flags) { return new With_(this, inner, context, flags); }
		With_ With(LNode inner, Precedence context) {
			var oneliner = CheckOneLiner(_flags & Ambiguity.OneLiner, inner);
			return new With_(this, inner, context, oneliner);
		}
		WithFlags_ WithFlags(Ambiguity flags) { return new WithFlags_(this, flags); }
		WithSpace_ WithSpace(Symbol spaceName) { return new WithSpace_(this, spaceName); }
		Ambiguity CheckOneLiner(Ambiguity flags, LNode n) {
			if ((n.Style & NodeStyle.OneLiner) != 0)
				flags |= Ambiguity.OneLiner;
			return flags;
		}

		void PrintInfixWithSpace(Symbol name, LNode target, Precedence p, bool useBacktick = false)
		{
			if (p.Lo < _o.SpaceAroundInfixStopPrecedence && (name != S.DotDot || (_o.SpaceOptions & SpaceOpt.SuppressAroundDotDot) == 0)) {
				_out.Space();
				WriteOperatorNameWithTrivia(name, target, useBacktick);
				_out.Space();
			} else {
				WriteOperatorNameWithTrivia(name, target, useBacktick);
			}
		}

		private void WriteOperatorNameWithTrivia(Symbol name, LNode target, bool useBacktick = false)
		{
			if (target != null)
				PrintTrivia(target, trailingTrivia: false);
			WriteOperatorName(name, useBacktick);
			if (target != null)
				PrintTrivia(target, trailingTrivia: true);
		}

		void PrefixSpace(Precedence p)
		{
			if (p.Lo < _o.SpaceAfterPrefixStopPrecedence)
				_out.Space();
		}
		void Space(SpaceOpt context)
		{
			if ((_o.SpaceOptions & context) != 0)
				_out.Space();
		}
		void WriteThenSpace(char ch, SpaceOpt option)
		{
			_out.Write(ch, true);
			if ((_o.SpaceOptions & option) != 0)
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
				if (((int)_o.SpaceOptions & (int)type) != 0)
					_out.Space();
				_out.Write('(', true);
				WriteInnerSpace(type);
			}
			return confirm;
		}
		void WriteCloseParen(ParenFor type, bool confirm = true)
		{
			if (confirm) {
				WriteInnerSpace(type);
				_out.Write(')', true);
				if (((int)_o.SpaceOptions & (int)SpaceOpt.OutsideParens & (int)type) != 0)
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
			if ((_o.SpaceOptions & (SpaceOpt.InsideParens | SpaceOpt.InsideCallParens)) != 0)
				if ((type & (ParenFor.Grouping | ParenFor.MethodCall | ParenFor.MacroCall)) != 0)
					if ((_o.SpaceOptions & (type == ParenFor.Grouping ? SpaceOpt.InsideParens : SpaceOpt.InsideCallParens)) != 0)
						_out.Space();
		}
		private bool Newline(NewlineOpt context)
		{
			if ((_o.NewlineOptions & context) != 0) {
				_out.Newline();
				return true;
			}
			return false;
		}
		private bool NewlineOrSpace(NewlineOpt context, bool forceSpace = false, SpaceOpt spaceOpt = SpaceOpt.Default)
		{
			if (forceSpace || !Newline(context)) {
				Space(spaceOpt);
				return false;
			} else
				return true;
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
			/// <remarks>This is also used in the case of @`~`((Foo), x) which must not 
			/// be printed as `(Foo) ~ x`</remarks>
			IsCallTarget = 0x0004,
			/// <summary>No braced block permitted directly here (inside "if" clause)</summary>
			NoBracedBlock = 0x0008,
			/// <summary>The current statement is the last one in the enclosing 
			/// block, so #result can be represented by omitting a semicolon.</summary>
			FinalStmt = 0x0010,
			/// <summary>An expression is being printed in a context where a type is expected.</summary>
			TypeContext = 0x0020,
			/// <summary>The expression being printed is a complex identifier that
			/// may contain special attributes, e.g. <c>Foo&lt;out T></c>.</summary>
			InDefinitionName = 0x0040,
			/// <summary>Printing a type inside angle brackets or !(...).</summary>
			InOf = 0x0080,
			/// <summary>Allow pointer notation (when combined with TypeContext). 
			/// Also, a pointer is always allowed at the beginning of a statement,
			/// which is detected by the precedence context (StartStmt).</summary>
			AllowPointer = 0x0100,
			/// <summary>Used to communicate to the operator printers that a binary 
			/// call should be expressed with the backtick operator.</summary>
			UseBacktick = 0x0400,
			/// <summary>Forces a variable declaration to be allowed as the 
			/// initializer of a foreach loop.</summary>
			ForEachInitializer = 0x1000,
			/// <summary>After 'else', valid 'if' statements are not indented.</summary>
			ElseClause = 0x2000,
			/// <summary>A statement is being printed, or the return value or name
			/// of a method, so parentheses cannot be emitted here.</summary>
			NoParentheses = 0x4000,
			/// <summary>Print #this(...) as this(...) inside a method</summary>
			AllowThisAsCallTarget = 0x8000,
			/// <summary>This location is the 'true' side of an if-else statement.
			/// At this location, no 'if' without 'else' is allowed because the
			/// outer else would, upon parsing, be associated with the inner 'if'.</summary>
			NoIfWithoutElse = 0x10000,
			/// <summary>Force PrintAttrs to print an empty attribute list `[]` which 
			/// has the effect of allowing unassigned variable declarations anywhere.</summary>
			ForceAttributeList = 0x40000,
			/// <summary>A signal that IF there is no trivia indicating whether or not 
			/// to print a newline before the current statement, one should be printed.</summary>
			NewlineBeforeChildStmt = 0x80000,
			/// <summary>Indicates that this node, or one of its parents, has the style
			/// <see cref="NodeStyle.OneLiner"/> which suppresses newlines.</summary>
			OneLiner = 0x100000,
		}

		bool Flagged(Ambiguity flag)
		{
			return (_flags & flag) != 0;
		}

		// Creates an open delegate (technically it could create a closed delegate
		// too, but there's no need to use reflection for that)
		static D OpenDelegate<D>(string name)
		{
			D result = (D)(object)Delegate.CreateDelegate(typeof(D), typeof(EcsNodePrinter).GetMethod(name));
			Debug.Assert(result != null);
			return result;
		}

		#region Sets and dictionaries of keywords and tokens

		static readonly HashSet<Symbol> PreprocessorCollisions = SymbolSet(
			"#if", "#else", "#elif", "#endif", "#define", "#undef",
			"#region", "#endregion", "#pragma", "#error", "#warning", "#note", "#line"
		);

		internal static readonly HashSet<Symbol> OperatorIdentifiers = new HashSet<Symbol> {
			// >>, << and ** are special: the lexer provides them as two separate tokens
			S.NotBits, S.Not, S.Mod, S.XorBits, S.AndBits, S.And, S.Mul, S.Exp, S.Add, S.PreInc,
			S.Sub, S.PreDec, S.Eq, S.Neq, S.Sub, S.PreDec, S.Eq, S.Neq, /*"{}", "[]",*/ S.OrBits, S.Or, S.Backslash,
			S.Semicolon, S.Colon, S.Comma, S.Dot, S.DotDot, S.LT, S.Shl, S.GT, S.Shr, S.Div,
			S.QuestionMark, S.NullCoalesce, S.NullDot, S.LE, S.GE, S.Lambda, S.Forward, S.PtrArrow, S.Substitute,
			S.Assign, S.MulAssign, S.SubAssign, S.AddAssign, S.DivAssign, S.ModAssign, S.ShrAssign, S.ShlAssign,
			S.XorBitsAssign, S.AndBitsAssign, S.OrBitsAssign, S.NullCoalesceAssign, S.QuickBindAssign,
			S.ConcatAssign, S.ExpAssign
		};

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

		static readonly HashSet<Symbol> KnownTrivia = new HashSet<Symbol> {
			S.TriviaInParens, S.TriviaTrailing,
			S.TriviaNewline, S.TriviaAppendStatement, S.TriviaSpaces,
			S.TriviaSLComment, S.TriviaMLComment, 
			S.TriviaRawText, S.TriviaCsRawText, S.TriviaCsPPRawText,
			S.TriviaUseOperatorKeyword, S.TriviaForwardedProperty,
			S.TriviaRegion, S.TriviaEndRegion,
		};

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
			{ return Pair.Create(key, value); }
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
			return !_o.DropNonDeclarationAttributes && node.HasPAttrs();
		}
		bool HasSimpleHeadWPA(LNode self)
		{
			return _o.DropNonDeclarationAttributes ? self.HasSimpleHead() : self.HasSimpleHeadWithoutPAttrs();
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
					(_o.DropNonDeclarationAttributes ? EcsValidators.Pedantics.IgnoreWeirdAttributes : 0) |
					(_o.AllowChangeParentheses ? EcsValidators.Pedantics.IgnoreIllegalParentheses : 0);
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

		// Used with PrintAttrs()
		enum AttrStyle {
			NoKeywordAttrs,    // Put all attributes in square brackets
			AllowKeywordAttrs, // e.g. [#public, #const] written as "public const", allowed on any expression
			IsConstructor,     // same as AllowKeywordAttrs, except that attrs are not blocked by DropNonDeclarationAttributes
			AllowWordAttrs,    // e.g. [#partial, #phat] written as "partial phat", allowed on keyword-stmts (for, if, etc.); also allows [#this]
			IsDefinition,      // allows word attributes plus "new" (only on definitions: methods, var decls, events...)
		};

		// Returns the number of opening "("s printed that require a corresponding ")".
		private int PrintAttrs(AttrStyle style, LNode skipClause = null, string label = null)
		{
			var attrs = _n.Attrs;
			if (Flagged(Ambiguity.NewlineBeforeChildStmt))
			{
				if (attrs.Count == 0 || !attrs.Any(a => a.Name.IsOneOf(S.TriviaNewline, S.TriviaAppendStatement)))
					NewlineOrSpace(NewlineOpt.BeforeSingleSubstmt, false, SpaceOpt.Minimal);
				else if (attrs.NodeNamed(S.TriviaAppendStatement) != null)
					Space(SpaceOpt.Default);
			}
			if (attrs.Count == 0 && !Flagged(Ambiguity.ForceAttributeList))
				return 0; // optimize common case

			// To identify "word attributes", scan attributes in reverse until 
			// seeing an attribute that is not a word attribute nor a trivia attr.
			// Special cases:
			// - `in` and `this` only allowed sometimes and must be last non-trivia attribute
			// - `out`, `ref`, `yield`, and `in` are not dropped by DropNonDeclarationAttributes
			// - #where(...) inside a type parameter definition is ignored (printed elsewhere)
			bool isTypeParamDefinition = (_flags & (Ambiguity.InDefinitionName | Ambiguity.InOf))
			                                    == (Ambiguity.InDefinitionName | Ambiguity.InOf);
			if (isTypeParamDefinition)
				attrs = attrs.SmartWhere(n => !n.Calls(S.Where));
			int attrCount = attrs.Count;
			int div = attrCount; // index of first word attribute
			int i = attrCount;
			foreach (LNode attr in attrs.ToFVList()) {
				i--;
				if (!S.IsTriviaSymbol(attr.Name)) {
					if (!IsWordAttribute(attr, style) && !(isTypeParamDefinition && attr.IsIdNamed(S.In)))
						break;
					else
						div = i;
				}
			}

			// Check if we should ignore most attributes
			bool beginningOfStmt = _context.RangeEquals(StartStmt);
			bool mayNeedParens  = !_context.RangeEquals(StartExpr) && !beginningOfStmt;
			bool dropMostAttrs = false;
			if (_o.DropNonDeclarationAttributes && style < AttrStyle.IsDefinition && style != AttrStyle.IsConstructor) {
				// Careful: avoid dropping attributes from get; set; and things that look like macro calls
				if (!beginningOfStmt || _n.IsCall && (_n.ArgCount == 0 || !_n.Args.Last.Calls(S.Braces)))
					dropMostAttrs = true;
			} else if (mayNeedParens && Flagged(Ambiguity.NoParentheses))
				dropMostAttrs = true; // e.g. this happens for a method's return type

			int parenCount = 0;
			//if (mayNeedParens && (flags & Ambiguity.ForceAttributeList) != 0)
			//	OpenParenIf(true, ref parenCount, ref context);

			// Scanning forward, print normal attributes and trivia
			bool any = false, inBrackets = false;
			for (i = 0; i < div; i++) {
				var attr = attrs[i];
				if (inBrackets && attr.IsTrivia) {
					// It looks better if trivia is not printed inside [...]. Write ']' first.
					inBrackets = false;
					WriteThenSpace(']', SpaceOpt.AfterAttribute);
				}
				if (attr == skipClause || DetectAndMaybePrintTrivia(attr, false, ref parenCount))
					continue;

				if (!dropMostAttrs) {
					if (inBrackets)
						WriteThenSpace(',', SpaceOpt.AfterComma);
					else {
						OpenParenIf(mayNeedParens, ref parenCount, ref _context);
						WriteThenSpace('[', SpaceOpt.InsideAttribute);
						if (label != null) {
							_out.Write(label, true);
							_out.Write(':', true);
							Space(SpaceOpt.AfterColon);
						}
					}
					any = inBrackets = true;
					PrintExpr(attr, StartExpr);
				}
			}

			if (inBrackets) {
				Space(SpaceOpt.InsideAttribute);
				_out.Write(']', true);
				if (!beginningOfStmt || !Newline(NewlineOpt.AfterAttributes))
					Space(SpaceOpt.AfterAttribute);
			} else if (!any && (_flags & Ambiguity.ForceAttributeList) != 0) {
				OpenParenIf(mayNeedParens, ref parenCount, ref _context);
				_out.Write("[]", true);
				Space(SpaceOpt.AfterAttribute);
			}

			if (parenCount != 0 && !any) {
				_context = StartExpr;

				// Avoid cast ambiguity, e.g. for the method call x(y), represented 
				// (x)(y) where x is in parenthesis, must not be printed like that 
				// because it would be parsed as a cast. Use ((x))(y) instead, or 
				// (([] x))(y) to tell the EC# parser to ignore the extra parens.
				if (parenCount == 1 && (_flags & Ambiguity.IsCallTarget) != 0
					&& IsComplexIdentifier(_n, ICI.Default | ICI.AllowAnyExprInOf | ICI.AllowParensAround)) {
					parenCount++;
					if (_o.AllowChangeParentheses) {
						_out.Write('(', true);
					} else {
						_out.Write("([]", true);
						Space(SpaceOpt.AfterAttribute);
					}
				}
			}
			
			// Now print word attributes and trivia
			for (i = div; i < attrCount; i++) {
				var attr = attrs[i];
				if (attr == skipClause || DetectAndMaybePrintTrivia(attr, false, ref parenCount) || attr.IsTrivia)
					continue;

				PrintTrivia(attr, trailingTrivia: false);

				string text;
				var name = attr.Name;
				if (AttributeKeywords.TryGetValue(name, out text)) {
					if (dropMostAttrs && name != S.Out && name != S.Ref)
						continue;
					OpenParenIf(mayNeedParens, ref parenCount, ref _context);
					_out.Write(text, true);
				} else if (name == S.This) {
					Debug.Assert(!mayNeedParens);
					_out.Write("this", true);
				} else if (name == S.In) {
					Debug.Assert(!mayNeedParens);
					_out.Write("in", true);
				} else if (!dropMostAttrs || name == S.Yield) {
					OpenParenIf(mayNeedParens, ref parenCount, ref _context);
					Debug.Assert(attr.HasSpecialName);
					PrintSimpleIdent(GSymbol.Get(name.Name.Substring(1)), 0, false);
				}

				Space(SpaceOpt.Default);
				PrintTrivia(attr, trailingTrivia: true);
			}

			return parenCount;
		}

		private void OpenParenIf(bool flag, ref int parenCount, ref Precedence context)
		{
			if (flag && parenCount == 0) {
				parenCount++;
				context = StartExpr;
				WriteOpenParen(ParenFor.Grouping);
			}
		}

		// Checks if the specified attribute is trivia and, if so, prints it. 
		// Returns true if the attribute is trivia and should NOT printed as 
		// a normal attribute.
		bool DetectAndMaybePrintTrivia(LNode attr, bool trailingMode, ref int parenCount)
		{
			var name = attr.Name;
			if (!KnownTrivia.Contains(name))
				return _o.OmitUnknownTrivia && S.IsTriviaSymbol(name);

			if (name == S.TriviaRawText || name == S.TriviaCsRawText || name == S.TriviaCsPPRawText) {
				if (!_o.ObeyRawText)
					return _o.OmitUnknownTrivia;
				WriteRawText(GetRawText(attr), name == S.TriviaCsPPRawText);
			} else if (name == S.TriviaInParens) {
				if (!trailingMode && !Flagged(Ambiguity.InDefinitionName | Ambiguity.NoParentheses)) {
					if (!_context.CanParse(LesPrecedence.Substitute)) {
						// Inside $: outer parens are expected. Add a second pair of parens 
						// so that reparsing preserves the in-parens trivia.
						_out.Write('(', true);
						parenCount++;
					}
					_out.Write('(', true);
					parenCount++;
					Space(SpaceOpt.InsideParens);
				}
			} else if (name == S.TriviaNewline) {
				_out.Newline();
			} else if (name == S.TriviaSpaces) {
				if (!_o.OmitSpaceTrivia)
					PrintSpaces(GetRawText(attr));
			} else if (name == S.TriviaSLComment) {
				if (!_o.OmitComments) {
					if (trailingMode && !_out.LastCharWritten.IsOneOf(' ', '\t') && (_o.SpaceOptions & SpaceOpt.BeforeCommentOnSameLine) != 0)
						_out.Write('\t', true);
					_out.Write("//", false);
					_out.Write(GetRawText(attr), false);
					_out.Newline(true);
				}
			} else if (name == S.TriviaMLComment) {
				if (!_o.OmitComments) {
					if (trailingMode && !_out.LastCharWritten.IsOneOf(' ', '\t', '\n'))
						Space(SpaceOpt.BeforeCommentOnSameLine);
					_out.Write("/*", false);
					_out.Write(GetRawText(attr), false);
					_out.Write("*/", true);
				}
			} else if (name == S.TriviaRegion || name == S.TriviaEndRegion) {
				string arg = (attr.TriviaValue ?? "").ToString();
				string prefix = name == S.TriviaRegion ? "#region" : "#endregion";
				string prefix2 = char.IsLetterOrDigit(arg.TryGet(0, ' ')) ? prefix + " " : prefix;
				WriteRawText(prefix2 + arg, true);
			}
			return true;
		}

		private void PrintTrivia(bool trailingTrivia, bool needSemicolon = false)
		{
			PrintTrivia(_n, trailingTrivia, needSemicolon);
		}
		private void PrintTrivia(LNode node, bool trailingTrivia, bool needSemicolon = false)
		{
			if (needSemicolon)
				_out.Write(';', true);
			var attrs = trailingTrivia ? node.GetTrailingTrivia() : node.Attrs;
			foreach (var attr in attrs) {
				int _ = 0;
				DetectAndMaybePrintTrivia(attr, trailingTrivia, ref _);
			}
		}

		private void WriteRawText(string text, bool isPreprocessorText)
		{
			if (isPreprocessorText && _out.LastCharWritten != '\n')
				_out.Newline();
			if (text.EndsWith("\n")) {
				// use our own newline logic so indentation works
				_out.Write(text.Substring(0, text.Length - 1), true);
				_out.Newline(pending: true);
			} else {
				_out.Write(text, true);
				if (isPreprocessorText)
					_out.Newline(pending: true);
			}
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
				_staticPrinter = new EcsNodePrinter() { Writer = wr };
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
			_staticPrinter._n = LNode.Literal(value, null, style);
			_staticPrinter.PrintLiteral();
			_staticPrinter._n = null;
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
					Debug.Assert(name.Name.StartsWith("'"));
					_out.Write(name.Name.Substring(1), true);
				} else
					PrintString(name.Name, '`', null, true);
				return;
			}

			// Check if this is a 'normal' identifier and not a keyword:
			char first = name.Name.TryGet(0, '\0');
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

			if (_o.PreferPlainCSharp && !inSymbol) {
				if (!EcsValidators.IsPlainCsIdentifier(name.Name))
					name = (Symbol)EcsValidators.SanitizeIdentifier(name.Name);
			}

			if (name.Name == "") {
				_out.Write(inSymbol ? "``" : "@``", true);
				return;
			}

			// Detect special characters that demand backquotes
			for (int i = 0; i < name.Name.Length; i++)
			{
				char c = name.Name[i];
				if (!EcsValidators.IsIdentContChar(c)) {
					// Backquote required for this identifier.
					if (!inSymbol)
						_out.Write("@", false);
					PrintString(name.Name, '`', null, true);
					return;
				}
			}
			
			// Print @ if needed, then the identifier
			if (!inSymbol) {
				if (!EcsValidators.IsIdentStartChar(first) || PreprocessorCollisions.Contains(name) || CsKeywords.Contains(name) || name == Var || name == Def)
					_out.Write("@", false);
			}
			_out.Write(name.Name, true);
		}

		static readonly Symbol _Verbatim = GSymbol.Get("#trivia_verbatim");

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
				_out.Write(PrintHelpers.EscapeCStyle(text, EscapeC.Control | EscapeC.UnicodeNonCharacters | EscapeC.UnicodePrivateUse, quoteType), false);
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
				var n = np._n;
				// TODO: add ability to print triple-quoted strings when !PreferPlainCSharp
				var v = n.BaseStyle == NodeStyle.VerbatimStringLiteral || n.BaseStyle == NodeStyle.TQStringLiteral || n.BaseStyle == NodeStyle.TDQStringLiteral ? _Verbatim : null;
				np.PrintString(n.Value.ToString(), '"', v, true);
			}),
			P<Symbol> (np => {
				np._out.Write("@@", false);
				np.PrintSimpleIdent((Symbol)np._n.Value, 0, true);
			}),
			P<TokenTree> (np => {
				np._out.Write("@{", true);
				np._out.Write(((TokenTree)np._n.Value).ToString(Ecs.Parser.TokenExt.ToString), true);
				np._out.Write(" }", true);
			}),
			P<IEnumerable<byte>> (np => { // Unnatural. Not produced by parser.
				var data = (IEnumerable<byte>)np._n.Value;
				if (np._n.BaseStyle == NodeStyle.HexLiteral)
					np.WriteArrayLiteral("byte", data, (num, _out) => {
						_out.Write("0x", false);
						_out.Write(num.ToString("X", null), false);
					});
				else
					np.WriteArrayLiteral("byte", data, (num, _out) => 
						_out.Write(num.ToString(), false));
			})
		);
		
		void PrintValueToString(string suffix)
		{
			_out.Write(_n.Value.ToString(), false);
			_out.Write(suffix, true);
		}
		void PrintIntegerToString(string suffix)
		{
			string asStr;
			if (_n.BaseStyle == NodeStyle.HexLiteral) {
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

		void WriteArrayLiteral<T>(string elementType, IEnumerable<T> data, Action<T, INodePrinterWriter> writeElement)
		{
			WriteOpenParen(ParenFor.Grouping, !_context.CanParse(EP.Primary));
			_out.Write("new ", true);
			_out.Write(elementType, true);
			_out.Write("[] {", true);
			NewlineOrSpace(NewlineOpt.AfterOpenBraceInNewExpr);
			int count = 0;
			foreach (T datum in data)
			{
				if (count++ != 0) {
					_out.Write(',', true);
					if ((count & 0xF) == 0)
						NewlineOrSpace(NewlineOpt.Minimal);
				}
				writeElement(datum, _out);
			}
			NewlineOrSpace(NewlineOpt.BeforeCloseBraceInExpr);
			_out.Write("}", true);
			WriteOpenParen(ParenFor.Grouping, !_context.CanParse(EP.Primary));
		}

		private void PrintLiteral()
		{
			Debug.Assert(_n.IsLiteral);
			Action<EcsNodePrinter> p;
			if (_n.Value == null)
				_out.Write("null", true);
			else if (LiteralPrinters.TryGetValue(_n.Value.GetType().TypeHandle, out p)
				|| (p = LiteralPrinters.FirstOrDefault(pair => Type.GetTypeFromHandle(pair.Key).IsAssignableFrom(_n.Value.GetType())).Value) != null)
				p(this);
			else {
				ErrorSink.Error(_n, "EcsNodePrinter: Encountered unprintable literal of type '{0}'", _n.Value.GetType().Name);
				bool quote = _o.QuoteUnprintableLiterals;
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

#endregion
	}

	/// <summary>Flags for <see cref="EcsNodePrinter.IsComplexIdentifier"/>.</summary>
	[Flags] public enum ICI
	{
		Default = 0,
		AllowAttrs = 2, // outer level only. e.g. this flag is used on return types, where
			// #fn([Attr] int, Foo, #()) is printed "[return: Attr] int Foo();"
		// For internal use
		DisallowDotted = 8, // inside right-hand side of dot
		InOf = 32,          // inside <...>
		// hmm
		AllowAnyExprInOf = 64,
		// Allows in out, e.g. Foo<in A, out B, c>, but requires type params 
		// to be simple (e.g. Foo<A.B, C<D>> is illegal)
		NameDefinition = 128,
		// allows parentheses around the outside of the complex identifier.
		AllowParensAround = 256,
	}

	/// <summary>Controls the locations where spaces appear as <see cref="EcsNodePrinter"/> 
	/// is printing.</summary>
	/// <remarks>
	/// Note: Spaces around prefix and infix operators are controlled by 
	/// <see cref="EcsPrinterOptions.SpaceAroundInfixStopPrecedence"/> and
	/// <see cref="EcsPrinterOptions.SpaceAfterPrefixStopPrecedence"/>.
	/// </remarks>
	[Flags] public enum SpaceOpt
	{
		Default = Minimal | AfterComma | AfterCommaInOf | AfterCast | AfterAttribute | AfterColon 
			| BeforeKeywordStmtArgs | BeforePossibleMacroArgs | BeforeNewInitBrace 
			| InsideNewInitializer | BeforeBaseListColon | BeforeForwardArrow 
			| BeforeConstructorColon | BeforeCommentOnSameLine | SuppressAroundDotDot,
		Minimal                 = 0x00000001, // Spaces in places where almost everyone puts spaces
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
	/// <summary>Flags to control situations in which newlines should be added automatically by the EC# printer.</summary>
	[Flags]
	public enum NewlineOpt
	{
		/// <summary>Default value of EcsNodePrinter.NewlineOptions</summary>
		/// <remarks>Oct 2016: Some defaults have been turned off because newlines can 
		/// now be added with #trivia_newline, and turning on some NewlineOpts currently
		/// causes double newlines (i.e. a blank line) when the LNode uses #trivia_newline
		/// at the same time.</remarks>
		Default = Minimal | //BeforeSpaceDefBrace | BeforeMethodBrace | BeforePropBrace | AfterAttributes |
			AfterOpenBraceInNewExpr | BeforeCloseBraceInNewExpr | BeforeCloseBraceInExpr |
			BeforeConstructorColon | BeforeSingleSubstmt | BeforeEachEnumItem,
		Minimal                   = 0x000001, // Newline between statements in braces
		BeforeSpaceDefBrace       = 0x000002, // Newline before opening brace of type definition
		BeforeMethodBrace         = 0x000004, // Newline before opening brace of method body
		BeforePropBrace           = 0x000008, // Newline before opening brace of property definition
		BeforeGetSetBrace         = 0x000010, // Newline before opening brace of property getter/setter
		BeforeSimpleStmtBrace     = 0x000020, // Newline before opening brace of try, get, set, checked, do, etc.
		BeforeExecutableBrace     = 0x000010, // Newline before opening brace of other executable statements
		BeforeSingleSubstmt       = 0x000040, // Newline before single substatement of if, for, while, etc.
		BeforeOpenBraceInExpr     = 0x000100, // Newline before '{' inside an expression, including x=>{...}
		BeforeCloseBraceInExpr    = 0x000200, // Newline before '}' returning to an expression, including x=>{...}
		AfterCloseBraceInExpr     = 0x000400, // Newline after '}' returning to an expression, including x=>{...}
		BeforeOpenBraceInNewExpr  = 0x000800, // Newline before '{' inside a 'new' expression
		AfterOpenBraceInNewExpr   = 0x001000, // Newline after '{' inside a 'new' expression
		BeforeCloseBraceInNewExpr = 0x002000, // Newline before '}' at end of 'new' expression
		AfterCloseBraceInNewExpr  = 0x004000, // Newline after '}' at end of 'new' expression
		AfterEachInitializer      = 0x008000, // Newline after each initializer of 'new' expression
		AfterAttributes           = 0x010000, // Newline after attributes attached to a statement
		BeforeWhereClauses        = 0x020000, // Newline before opening brace of type definition
		BeforeEachWhereClause     = 0x040000, // Newline before opening brace of type definition
		BeforeIfClause            = 0x080000, // Newline before "if" clause
		BeforeConstructorColon    = 0x100000, // Newline before ': this(...)' clause
		BeforeEachEnumItem        = 0x200000, // Newline before each item in an enum (`Minimal` puts a newline in front of the first item only)
	}

	/// <summary>Options to control the way <see cref="EcsNodePrinter"/>'s output is formatted.</summary>
	/// <remarks>
	/// <see cref="EcsPrinterOptions"/> has some configuration options that will 
	/// defeat round-tripping (from <see cref="LNode"/> to text and back) but will 
	/// make the output look better. For example, 
	/// <see cref="AllowExtraBraceForIfElseAmbig"/> will print a tree such as 
	/// <c>#if(a, #if(b, f()), g())</c> as <c>if (a) { if (b) f(); } else g();</c>,
	/// by adding braces to eliminate prefix notation, even though braces make the 
	/// Loyc tree different.
	/// </remarks>
	public sealed class EcsPrinterOptions : LNodePrinterOptions
	{
		public EcsPrinterOptions() : this(null) { }
		public EcsPrinterOptions(ILNodePrinterOptions options)
		{
			if (options != null)
				CopyFrom(options);
			else
				AllowChangeParentheses = true;
			SpaceOptions = SpaceOpt.Default;
			NewlineOptions = NewlineOpt.Default;
			SpaceAroundInfixStopPrecedence = EP.Power.Lo;
			SpaceAfterPrefixStopPrecedence = EP.Prefix.Lo;
			ObeyRawText = true;
		}

		public EcsPrinterOptions SetPlainCSharpMode(bool flag = true)
		{
			CompatibilityMode = flag;
			return this;
		}

		public override bool CompatibilityMode
		{
			get { return base.CompatibilityMode; }
			set {
				if (base.CompatibilityMode = value) {
					base.AllowChangeParentheses = true;
					AllowExtraBraceForIfElseAmbig = true;
					PrintTriviaExplicitly = false;
				}
				OmitUnknownTrivia = value;
				DropNonDeclarationAttributes = value;
				AllowConstructorAmbiguity = value;
				AvoidMacroSyntax = value;
				PreferPlainCSharp = value;
			}
		}

		public override bool CompactMode
		{
			get { return base.CompactMode; }
			set {
				if (base.CompactMode = value) {
					SpaceOptions = SpaceOpt.Minimal;
					NewlineOptions = NewlineOpt.Minimal;
				} else {
					SpaceOptions = SpaceOpt.Default;
					NewlineOptions = NewlineOpt.Default;
				}
			}
		}

		/// <summary>Allows operators to be mixed that will cause the parser to 
		/// produce a warning. An example is <c>x &amp; @==(y, z)</c>: if you enable 
		/// this option, it will be printed as <c>x &amp; y == z</c>, which the parser
		/// will complain about because mixing those operators is deprecated.
		/// </summary>
		public bool MixImmiscibleOperators { get; set; }

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

		/// <summary>When this flag is set, raw text trivia attributes (e.g. <see 
		/// cref="CodeSymbols.TriviaRawTextBefore"/>) are obeyed (cause raw text 
		/// output); otherwise such attributes are treated as unknown trivia and, 
		/// if <see cref="LNodePrinterOptions.OmitUnknownTrivia"/> is false, 
		/// printed as attributes.</summary>
		/// <remarks>Initial value: true</remarks>
		public bool ObeyRawText { get; set; }

		/// <summary>When the printer encounters an unprintable literal, it calls
		/// Value.ToString(). When this flag is set, the string is placed in double
		/// quotes; when this flag is clear, it is printed as raw text.</summary>
		public bool QuoteUnprintableLiterals { get; set; }

		/// <summary>Causes the ambiguity between constructors and method calls to
		/// be ignored; see <see cref="Loyc.Ecs.Tests.EcsPrinterAndParserTests.ConstructorAmbiguities()"/>.</summary>
		public bool AllowConstructorAmbiguity { get; set; }

		/// <summary>Prints statements like "foo (...) bar()" in the equivalent form
		/// "foo (..., bar())" instead. Does not affect foo {...} because property
		/// and event definitions require this syntax (get {...}, set {...}).</summary>
		public bool AvoidMacroSyntax { get; set; }

		/// <summary>Prefers plain C# syntax for certain other things (not covered
		/// by the other options), even when the syntax tree requests a different 
		/// style, e.g. EC# cast operators are blocked so x(->int) becomes (int) x,
		/// and @`at-identifiers` are sanitized.</summary>
		public bool PreferPlainCSharp { get; set; }

		/// <summary>Controls the locations where spaces should be emitted.</summary>
		public SpaceOpt SpaceOptions { get; set; }
		
		/// <summary>Controls the locations where newlines should be emitted.</summary>
		public NewlineOpt NewlineOptions { get; set; }

		/// <summary>The printer avoids printing spaces around infix (binary) 
		/// operators that have the specified precedence or higher.</summary>
		/// <seealso cref="EcsPrecedence"/>
		public int SpaceAroundInfixStopPrecedence { get; set; }
		
		/// <summary>The printer avoids printing spaces after prefix operators 
		/// that have the specified precedence or higher.</summary>
		public int SpaceAfterPrefixStopPrecedence { get; set; }
	}
}
