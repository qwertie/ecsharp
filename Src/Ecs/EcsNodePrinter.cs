using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.ComponentModel;
using Loyc;
using Loyc.Utilities;
using Loyc.Math;
using Loyc.CompilerCore;
using S = ecs.CodeSymbols;
using EP = ecs.EcsPrecedence;
using Loyc.Essentials;

namespace ecs
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
	/// <li>The Value property must only be used in literal nodes (Name #literal),
	///     and only literals that can exist in C# source code are allowed. For 
	///     example, Values of type int, string, and double are acceptable, but
	///     Values of type Regex or int[] are not, because single tokens cannot
	///     represent these types in C# source code. The printer ignores Values of 
	///     non-#literal nodes, and non-representable literals are printed as 
	///     #literal.</li>
	/// <li>Names must come from the global symbol pool (<see cref="GSymbol.Pool"/>).
	///     The printer will happily print Symbols from other pools, but there is
	///     no way to indicate the pool in source code, so the parser always 
	///     recreates symbols in the global pool. Non-global symbols are used
	///     after semantic analysis, so there is no way to faithfully represent
	///     the results of semantic analysis.</li>
	/// </ol>
	/// Only the attributes, head and arguments of nodes are round-trippable. 
	/// Superficial properties such as original source code locations and the 
	/// <see cref="INodeReader.Style"/> are, in general, lost, although the 
	/// printer can faithfully reproduce some (not all) <see cref="NodeStyle"/>s.
	/// Also, any attribute whose Name starts with "#trivia_" will be dropped, 
	/// because these attributes are considered extensions of the NodeStyle.
	/// However, the style indicated by the #trivia_* attribute will be used if 
	/// the printer recognizes it.
	/// <para/>
	/// Because EC# is based on C# which has some tricky ambiguities, it is rather
	/// likely that some cases have been missed--that some unusual trees will not 
	/// round-trip properly. Any failure to round-trip is a bug, and your bug 
	/// reports are welcome. Unnecessary use of prefix notation is also considered
	/// a bug.
	/// <para/>
	/// This class contains some configuration options that will defeat round-
	/// tripping but will make the output look better. For example,
	/// <see cref="AllowExtraParenthesis"/> will print a tree such as <c>#*(a + b, c)</c> 
	/// as <c>(a + b) * c</c>, by adding parenthesis to eliminate prefix notation,
	/// even though parenthesis make the Loyc tree slightly different.
	/// <para/>
	/// To avoid printing EC# syntax that does not exist in C#, you can call
	/// <see cref="SetPlainCSharpMode"/>, but this only works if the syntax tree
	/// does not contain invalid structure or EC#-specific code such as "==>", 
	/// "alias", and template arguments (\T).
	/// </remarks>
	public partial class EcsNodePrinter
	{
		INodeReader _n;
		INodePrinterWriter _out;

		public INodeReader Node { get { return _n; } set { _n = value; } }
		public INodePrinterWriter Writer { get { return _out; } set { _out = value; } }

		public EcsNodePrinter(INodeReader node, INodePrinterWriter target)
		{
			_n = node;
			_out = target;
			if (target != null)
				target.Push(node);
			SpaceOptions = SpaceOpt.Default;
			NewlineOptions = NewlineOpt.Default;
			SpaceAroundInfixStopPrecedence = EP.Power.Lo;
			SpaceAfterPrefixStopPrecedence = EP.Prefix.Lo;
		}

		/// <summary>Allows operators to be mixed that will cause the parser to 
		/// produce a warning. An example is <c>x & #==(y, z)</c>: if you enable 
		/// this option, it will be printed as <c>x & y == z</c>, which the parser
		/// will complain about because mixing those operators is deprecated.
		/// </summary>
		public bool MixImmiscibleOperators { get; set; }
		/// <summary>Introduces extra parenthesis to express precedence, instead of
		/// resorting to prefix notation.</summary>
		/// <remarks>For example, the Loyc tree <c>x * #+(a, b)</c> will be printed 
		/// <c>x * (a + b)</c>, which is a different tree (due to the parenthesis, 
		/// <c>a + b</c> is nested in an extra node that has no arguments or 
		/// attributes.)</remarks>
		public bool AllowExtraParenthesis { get; set; }
		/// <summary>Prefers plain C# syntax for cast operators even when the 
		/// syntax tree has the new cast style, e.g. x(->int) becomes (int) x.</summary>
		public bool PreferOldStyleCasts { get; set; }
		/// <summary>Suppresses printing of all attributes that are not on 
		/// declaration or definition statements (such as classes, methods and 
		/// variable declarations at statement level). Also, avoids prefix notation 
		/// when the attributes would have required it, e.g. <c>#+([Foo] a, b)</c> 
		/// can be printed "a+b" instead.</summary>
		/// <remarks>This also affects the validation methods such as <see 
		/// cref="IsVariableDecl"/>. With this flag, validation methods will ignore
		/// attributes in locations where they don't belong instead of returning
		/// false.</remarks>
		public bool DropNonDeclarationAttributes { get; set; }
		
		/// <summary>When an argument to a method or macro has the value #missing,
		/// it will be omitted completely if this flag is set.</summary>
		public bool OmitMissingArguments { get; set; }

		/// <summary>Controls the locations where spaces should be emitted.</summary>
		public SpaceOpt SpaceOptions { get; set; }
		/// <summary>Controls the locations where newlines should be emitted.</summary>
		public NewlineOpt NewlineOptions { get; set; }
		
		public int SpaceAroundInfixStopPrecedence { get; set; }
		public int SpaceAfterPrefixStopPrecedence { get; set; }

		/// <summary>Sets <see cref="AllowExtraParenthesis"/>, <see cref="PreferOldStyleCasts"/> 
		/// and <see cref="DropNonDeclarationAttributes"/> to true.</summary>
		/// <returns>this.</returns>
		public EcsNodePrinter SetPlainCSharpMode()
		{
			AllowExtraParenthesis = true;
			PreferOldStyleCasts = true;
			DropNonDeclarationAttributes = true;
			return this;
		}
		
		#region Indented, With(), WithType(); Space() and other writing helpers

		struct Indented_ : IDisposable
		{
			EcsNodePrinter _self;
			public Indented_(EcsNodePrinter self) { _self = self; self._out.Indent(); }
			public void Dispose() { _self._out.Dedent(); }
		}
		struct With_ : IDisposable
		{
			internal EcsNodePrinter _self;
			INodeReader _old;
			public With_(EcsNodePrinter self, INodeReader inner)
			{
				_self = self;
				self._out.Push(_old = self._n); 
				self._n = inner; 
			}
			public void Dispose()
			{
				_self._out.Pop(_self._n = _old);
			}
		}

		Indented_ Indented { get { return new Indented_(this); } }
		With_ With(INodeReader inner) { return new With_(this, inner); }
		
		void PrintInfixWithSpace(Symbol name, Precedence p, Ambiguity flags)
		{
			if (p.Lo < SpaceAroundInfixStopPrecedence) {
				_out.Space();
				WriteOperatorName(name, flags);
				_out.Space();
			} else
				WriteOperatorName(name, flags);
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
		void PrintWithinParens(ParenFor type, INodeReader n, Ambiguity flags = 0)
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

		// Creates an open delegate (technically it could create a closed delegate
		// too, but there's no need to use reflection for that)
		static D OpenDelegate<D>(string name)
		{
			return (D)(object)Delegate.CreateDelegate(typeof(D), typeof(EcsNodePrinter).GetMethod(name));
		}

		#region Sets and dictionaries of keywords and tokens

		static readonly HashSet<Symbol> PreprocessorCollisions = SymbolSet(
			"#if", "#else", "#elif", "#endif", "#define", "#region", "#endregion", 
			"#pragma", "#warning", "#error", "#line"
		);

		static readonly HashSet<Symbol> TokenHashKeywords = SymbolSet(
			// >>, << and ** are special: the lexer provides them as two separate tokens
			"#~", "#!", "#%", "#^", "#&", "#&&", "#*", "#**", "#+", "#++", 
			"#-", "#--", "#=", "#==", "#!=", "#{}", "#[]", "#|", "#||", @"#\", 
			"#;", "#:", "#,", "#.", "#..", "#<", "#<<", "#>", "#>>", "#/", 
			"#?", "#??", "#??.", "#??=", "#%=", "#^=", "#&=", "#*=", "#-=", 
			"#+=", "#|=", "#<=", "#>=", "#=>", "#==>", "#->"
		);

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

		bool HasPAttrs(INodeReader node) // for use in expression context
		{
			return !DropNonDeclarationAttributes && node.HasPAttrs();
		}
		bool HasSimpleHeadWPA(INodeReader self)
		{
			return DropNonDeclarationAttributes ? self.HasSimpleHead : self.HasSimpleHeadWithoutPAttrs();
		}
		bool CallsWPAIH(INodeReader self, Symbol name)
		{
			return self.Name == name && self.IsCall && HasSimpleHeadWPA(self);
		}
		public bool CallsMinWPAIH(INodeReader self, Symbol name, int argCount)
		{
			return self.Name == name && self.ArgCount >= argCount && HasSimpleHeadWPA(self);
		}
		public bool CallsWPAIH(INodeReader self, Symbol name, int argCount)
		{
			return self.Name == name && self.ArgCount == argCount && HasSimpleHeadWPA(self);
		}
		public bool IsSimpleSymbolWPA(INodeReader self)
		{
			return self.IsSimpleSymbol && (DropNonDeclarationAttributes || !HasPAttrs(self));
		}
		public bool IsSimpleSymbolWPA(INodeReader self, Symbol name)
		{
			return self.Name == name && IsSimpleSymbolWPA(self);
		}

		public bool IsSpaceStatement()
		{
			// All space declarations and space definitions have the form
			// #spacetype(Name, #(BaseList), { ... }) and the syntax
			// spacetype Name : BaseList { ... }, with optional "where" and "if" clauses
			// e.g. enum Foo : ushort { A, B, C }
			// The "if" clause is attached as an attribute on the statement;
			// "where" clauses are attached as attributes of the generic parameters.
			// For printing purposes,
			// - A declaration always has 2 args; a definition always has 3 args
			// - Name must be a complex (definition) identifier without attributes for
			//   normal spaces, or a #= expression for aliases.
			// - #(BaseList) can be #missing; the bases can be any expressions
			// - the arguments do not have attributes
			var type = _n.Name;
			if (SpaceDefinitionStmts.Contains(type) && HasSimpleHeadWPA(_n) && MathEx.IsInRange(_n.ArgCount, 2, 3))
			{
				INodeReader name = _n.TryGetArg(0), bases = _n.TryGetArg(1), body = _n.TryGetArg(2);
				if (type == S.Alias) {
					if (!CallsWPAIH(name, S.Set, 2))
						return false;
					if (!IsComplexIdentifier(name.TryGetArg(0), ICI.Default | ICI.NameDefinition) ||
						!IsComplexIdentifier(name.TryGetArg(1)))
						return false;
				} else {
					if (!IsComplexIdentifier(name, ICI.Default | ICI.NameDefinition))
						return false;
				}
				if (bases == null) return true;
				if (HasPAttrs(bases)) return false;
				if (bases.IsSimpleSymbol(S.Missing) || bases.Calls(S.List))
				{
					if (body == null) return true;
					if (HasPAttrs(body)) return false;
					return CallsWPAIH(body, S.Braces);
				}
			}
			return false;
		}

		public bool IsMethodDefinition(bool orDelegate)
		{
			var def = _n.Name;
			if ((def != S.Def && def != S.Delegate) || !HasSimpleHeadWPA(_n))
				return false;
			if (!MathEx.IsInRange(_n.ArgCount, 3, def == S.Delegate ? 3 : 4))
				return false;

			INodeReader retType = _n.TryGetArg(0), name = _n.TryGetArg(1), args = _n.TryGetArg(2), body = _n.TryGetArg(3);
			// Note: the parser doesn't require that the argument list have a 
			// particular format, so the printer doesn't either.
			if (!CallsWPAIH(args, S.List) || 
				!(body == null || CallsWPAIH(body, S.Braces) || CallsWPAIH(body, S.Forward, 1)))
				return false;
			if (IsComplexIdentifier(name, ICI.Default | ICI.NameDefinition)) {
				return IsComplexIdentifier(retType, ICI.Default | ICI.AllowAttrs);
			} else {
				// Check for a destructor
				return retType.IsSimpleSymbol && retType.Name == S.Missing
					&& CallsWPAIH(name, S._Destruct, 1) 
					&& IsComplexIdentifier(name.TryGetArg(0), ICI.Simple);
			}
		}

		public bool IsPropertyDefinition()
		{
			var argCount = _n.ArgCount;
			if (!CallsMinWPAIH(_n, S.Property, 2) || _n.ArgCount > 3)
				return false;

			INodeReader retType = _n.TryGetArg(0), name = _n.TryGetArg(1), body = _n.TryGetArg(2);
			return IsComplexIdentifier(retType, ICI.Default) &&
			       IsComplexIdentifier(name, ICI.Default | ICI.NameDefinition) &&
			       (body == null || CallsWPAIH(body, S.Braces) || CallsWPAIH(body, S.Forward, 1));
		}

		public bool IsEventDefinition() { return EventDefinitionType() != null; }

		static readonly Symbol EventWithBody = GSymbol.Get("EventWithBody");
		static readonly Symbol EventList     = GSymbol.Get("EventList");
		Symbol EventDefinitionType()
		{
			if (!CallsMinWPAIH(_n, S.Event, 2))
				return null;

			INodeReader type = _n.TryGetArg(0), name = _n.TryGetArg(1);
			if (!IsComplexIdentifier(type, ICI.Default) ||
				!IsComplexIdentifier(name, ICI.Simple))
				return null;

			int argCount = _n.ArgCount;
			if (argCount == 3) {
				var body = _n.TryGetArg(2);
				if (CallsWPAIH(body, S.Braces) || CallsWPAIH(body, S.Forward))
					return EventWithBody;
			}

			for (int i = 2; i < argCount; i++)
				if (!IsComplexIdentifier(_n.TryGetArg(i), ICI.Simple))
					return null;
			return EventList;
		}

		public bool IsVariableDecl(bool allowMultiple, bool allowNoAssignment) // for printing purposes
		{
			// e.g. #var(#int, x(0)) <=> int x = 0
			// For printing purposes in EC#,
			// - Head and args do not have attributes
			// - First argument must have the syntax of a type name
			// - Other args must have the form foo or foo(expr), where expr does not have attributes
			// - Must define a single variable unless allowMultiple
			// - Must immediately assign the variable unless allowNoAssignment
			if (CallsMinWPAIH(_n, S.Var, 2))
			{
				var a = _n.Args;
				if (!IsComplexIdentifier(a[0]))
					return false;
				for (int i = 1; i < a.Count; i++)
				{
					if (HasPAttrs(a[i]))
						return false;
					if (a[i].IsCall && (a[i].ArgCount != 1 || HasPAttrs(a[i].Args[0])))
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

		public bool IsComplexIdentifierOrNull(INodeReader n)
		{
			if (n == null)
				return true;
			return IsComplexIdentifier(n);
		}
		public bool IsComplexIdentifier(INodeReader n, ICI f = ICI.Default)
		{
			// Returns true if 'n' is printable as a complex identifier.
			//
			// To be printable, a complex identifier in EC# must not contain 
			// attributes (DropNonDeclarationAttributes to override) and must be
			// 1. A simple symbol
			// 2. A substitution expression
			// 3. A dotted expr (a.b.c), where 'a' is a simple identifier or #of 
			//    call, while 'b' and 'c' are (1) or (2); structures like (a.b).c 
			//    and a.(b<c>) do not count as complex identifiers, although the 
			//    former is legal as an ordinary expression. Note that a.b<c> is 
			//    structured #of(#.(a, b), c), not #.(a, #of(b, c)).
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
			if ((f & ICI.AllowAttrs) == 0 && HasPAttrs(n))
			{
				// Attribute(s) are illegal, except 'in', 'out' and 'where' when 
				// TypeParamDefinition inside <...>
				return (f & (ICI.NameDefinition | ICI.InOf)) == (ICI.NameDefinition | ICI.InOf) && IsPrintableTypeParam(n);
			}

			if (n.IsSimpleSymbol) // !IsCall && !IsLiteral && Head == null
				return true;
			if (CallsWPAIH(n, S.Substitute, 1))
				return true;
			if (CallsWPAIH(n, S._TemplateArg, 1))
				return (f & ICI.NameDefinition) != 0;

			if (n.IsParenthesizedExpr()) // !self.IsCall && self.Head != null
			{
				// TODO: detect subexpressions that are legal in typeof
				return (f & ICI.AllowSubexpr) != 0;
			}
			if (CallsWPAIH(n, S.List, 1))
				return (f & ICI.InOf) != 0;

			if (CallsMinWPAIH(n, S.Of, 1) && (f & ICI.AllowOf) != 0) {
				bool accept = true;
				ICI childFlags = ICI.AllowDotted;
				bool allowSubexpr = n.Args[0].IsSimpleSymbol(S.Typeof);
				for (int i = 0; i < n.ArgCount; i++) {
					if (!IsComplexIdentifier(n.Args[i], childFlags)) {
						accept = false;
						break;
					}
					childFlags |= ICI.InOf | ICI.AllowOf | (f & ICI.NameDefinition);
					if (allowSubexpr)
						childFlags |= ICI.AllowSubexpr;
				}
				return accept;
			}
			if (CallsMinWPAIH(n, S.Dot, 1) && (f & ICI.AllowDotted) != 0) {
				bool accept = true;
				if (n.ArgCount == 1) {
					// Left-hand argument was omitted
					return IsComplexIdentifier(n.Args[0], 0);
				} else if (IsComplexIdentifier(n.Args[0], ICI.AllowOf | (f & ICI.AllowSubexpr))) {
					for (int i = 1; i < n.ArgCount; i++) {
						// Allow only simple symbols or substitution
						if (!IsComplexIdentifier(n.Args[i], 0)) {
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

		/// <summary>Checks if 'n' is a legal type parameter definition.</summary>
		/// <remarks>A type parameter definition must be a simple symbol with at 
		/// most one #in or #out attribute, and at most one #where attribute with
		/// an argument list consisting of complex identifiers.</remarks>
		public bool IsPrintableTypeParam(INodeReader n)
		{
			for (int i = 0, c = n.AttrCount; i < c; i++)
			{
				var attr = n.TryGetAttr(i);
				var name = attr.Name;
				if (attr.Head != null || HasPAttrs(attr))
					return false;
				if (attr.IsCall) {
					if (name == S.Where) {
						for (int j = 0; j < attr.ArgCount; j++)
							if (!IsComplexIdentifier(attr.TryGetArg(j)))
								return false;
					} else if (!DropNonDeclarationAttributes) 
						return false;
				} else {
					if (!DropNonDeclarationAttributes && name != S.In && name != S.Out)
						return false;
				}
			}
			return true;
		}

		public bool IsBlockStmt() { return BlockStmtType() != null; }
		Symbol BlockStmtType()
		{
			return TwoArgBlockStmtType() ?? OtherBlockStmtType();
		}
		Symbol TwoArgBlockStmtType()
		{
			// S.Do:                     #do(stmt, expr)
			// S.Switch:                 #switch(expr, #{}(...))
			// S.While (S.Using, etc.):  #while(expr, stmt), #using(expr, stmt), #lock(expr, stmt), #fixed(expr, stmt)
			var argCount = _n.ArgCount;
			if (argCount != 2)
				return null;
			var name = _n.Name;
			if (name == S.Switch)
				return CallsWPAIH(_n.TryGetArg(1), S.Braces) ? name : null;
			else if (name == S.Do)
				return name;
			else if (name == S.While || name == S.UsingStmt || name == S.Lock || name == S.Fixed)
				return S.While; // all four can be printed in the same style as while()
			return null;
		}
		Symbol OtherBlockStmtType()
		{
			// S.If:                     #if(expr, stmt [, stmt])
			// S.For:                    #for(expr1, expr2, expr3, stmt)
			// S.ForEach:                #foreach(decl, list, stmt)
			// S.Try:                    #try(stmt, #catch(expr | #missing, stmt) | #finally(stmt), ...)
			// S.Checked (S.Unchecked):  #checked(#{}(...))       // if no braces, it's a checked(expr)
			var argCount = _n.ArgCount;
			if (!HasSimpleHeadWPA(_n) || argCount < 1)
				return null;

			var name = _n.Name;
			if (name == S.If)
				return argCount == 2 || argCount == 3 ? name : null;
			else if (name == S.For)
				return argCount == 4 ? name : null;
			else if (name == S.ForEach)
				return argCount == 3 ? name : null;
			else if (name == S.Checked || name == S.Unchecked)
				return argCount == 1 && CallsWPAIH(_n.TryGetArg(0), S.Braces) ? S.Checked : null;
			else if (name == S.Try)
			{
				if (argCount < 2) return null;
				for (int i = 1; i < argCount; i++)
				{
					var clause = _n.TryGetArg(i);
					if (!clause.HasSimpleHeadWithoutPAttrs())
						return null;
					var n = clause.Name;
					int c = clause.ArgCount;
					if (n == S.Finally) {
						if (c != 1 || i + 1 != argCount)
							return null;
					} else if (n != S.Catch || c != 2)
						return null;
				}
				return name;
			}
			return null;
		}

		public static bool IsBlockOfStmts(INodeReader n)
		{
			return n.Name == S.Braces || n.Name == S.List;
		}

		public bool IsSimpleKeywordStmt()
		{
			var name = _n.Name;
			int argC = _n.ArgCount;
			return _n.IsCall && SimpleStmts.Contains(_n.Name) && HasSimpleHeadWPA(_n) && 
				(argC == 1 || (argC > 1 && name == S.Import) || 
				(argC == 0 && (name == S.Break || name == S.Continue || name == S.Return || name == S.Throw)));
		}

		public bool IsLabelStmt()
		{
			if (_n.Name == S.Label)
				return _n.ArgCount == 1 && IsSimpleSymbolWPA(_n.TryGetArg(0));
			return CallsWPAIH(_n, S.Case);
		}

		public bool IsNamedArgument()
		{
 			return CallsWPAIH(_n, S.NamedArg, 2) && IsSimpleSymbolWPA(_n.Args[0]);
		}
		
		public bool IsResultExpr(INodeReader n, bool allowAttrs = false)
		{
			return CallsWPAIH(n, S.Result, 1) && (allowAttrs || !HasPAttrs(n));
		}

		#endregion

		#region Parts of expressions: attributes, identifiers, literals

		enum AttrStyle {
			AllowKeywordAttrs, // e.g. [#public, #const] written as "public const", allowed on any expression
			NoKeywordAttrs,    // Put all attributes in square brackets
			AllowWordAttrs,    // e.g. [#partial, #phat] written as "partial phat", allowed on keyword-stmts (for, if, etc.)
			IsDefinition,      // allows word attributes plus "new" and "out" (only on definitions: methods, var decls, events...)
		};
		// Returns true if an opening "##(" was printed that requires a corresponding ")".
		private bool PrintAttrs(Precedence context, AttrStyle style, Ambiguity flags, INodeReader skipClause = null, string label = null)
		{
			Debug.Assert(label == null || style == AttrStyle.NoKeywordAttrs);
			if ((flags & Ambiguity.DropAttributes) != 0)
				return false;
			if (DropNonDeclarationAttributes && style < AttrStyle.IsDefinition)
				return false;

			bool isTypeParamDefinition = (flags & (Ambiguity.InDefinitionName|Ambiguity.InOf)) 
			                                   == (Ambiguity.InDefinitionName|Ambiguity.InOf);
			int div = _n.AttrCount, attrCount = div;
			if (div == 0)
				return false;

			bool beginningOfStmt = context.RangeEquals(StartStmt);
			bool needParens = !beginningOfStmt && !context.RangeEquals(StartExpr);
			if (isTypeParamDefinition) {
				for (; div > 0; div--) {
					var a = _n.TryGetAttr(div-1);
					var n = a.Name;
					if (!IsSimpleSymbolWPA(a) || (n != S.In && n != S.Out))
						if (n != S.Where)
							break;
				}
				needParens = div != 0;
			} else if (needParens) {
				if (!HasPAttrs(_n))
					return false;
			} else {
				if (style != AttrStyle.NoKeywordAttrs) {
					// Choose how much of the attributes to treat as simple words, 
					// e.g. we prefer to print [Flags, #public] as "[Flags] public"
					bool isVarDecl = _n.Name == S.Var;
					for (; div > 0; div--)
						if (!IsWordAttribute(_n.TryGetAttr(div-1), style))
							break;
				}
			}

			// When an attribute appears mid-expression (which the printer 
			// tries hard to avoid through the use of prefix notation), we
			// need to write "##(" in order to group the attribute(s) with the
			// expression to which they apply, e.g. while "[A] x + y" has an
			// attribute attached to #+, the attribute in "##([A] x) + y" is
			// attached to x. The "##" tells the parser to discard the 
			// parenthesis instead of encoding them into the Loyc tree. Of 
			// course, we only print "##(" if there are attributes to print.
			if (needParens)
				_out.Write("##(", true);
			
			bool any = false;
			if (div > 0)
			{
				for (int i = 0; i < div; i++) {
					var a = _n.TryGetAttr(i);
					if (!a.IsPrintableAttr() || a == skipClause)
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
			for (int i = div; i < attrCount; i++)
			{
				var a = _n.TryGetAttr(i);
				string text;
				if (AttributeKeywords.TryGetValue(a.Name, out text)) {
					_out.Write(text, true);
				} else {
					Debug.Assert(a.IsKeyword);
					if (!a.IsPrintableAttr())
						continue;
					if (isTypeParamDefinition) {
						if (a.Name == S.In)
							_out.Write("in", true); // "out" is listed in AttributeKeywords
						else {
							Debug.Assert(a.Name == S.Where);
							continue;
						}
					} else
						PrintSimpleIdent(GSymbol.Get(a.Name.Name.Substring(1)), 0, false);
				}
				//any = true;
				Space(SpaceOpt.Default);
			}

			return needParens;
		}

		static bool IsWordAttribute(INodeReader node, AttrStyle style)
		{
			if (node.IsCall || node.HasAttrs() || !node.IsKeyword)
				return false;
			else {
				if (AttributeKeywords.ContainsKey(node.Name))
					return style >= AttrStyle.IsDefinition || (node.Name != S.New && node.Name != S.Out);
				else 
					return style >= AttrStyle.AllowWordAttrs && !CsKeywords.Contains(GSymbol.Get(node.Name.Name.Substring(1)));
			}
		}

		static readonly Symbol Var = GSymbol.Get("var"), Def = GSymbol.Get("def");

		private void PrintSimpleIdent(Symbol name, Ambiguity flags, bool inSymbol = false, bool useOperatorKeyword = false)
		{
 			if (name.Name == "") {
				_out.Write("``", true);
				Debug.Assert(inSymbol);
				return;
			}
			
			// Find out if the symbol is a valid identifier
			char first = name.Name[0];
			bool isNormal = true;
			if (first == '#' && !inSymbol) {
				string text;
				if ((flags & Ambiguity.TypeContext)!=0 && TypeKeywords.TryGetValue(name, out text)) {
					_out.Write(text, true);
					return;
				}
				if (TokenHashKeywords.Contains(name)) {
					if (useOperatorKeyword) {
						_out.Write("operator", true);
						Space(SpaceOpt.AfterOperatorKeyword);
						_out.Write(name.Name.Substring(1), true);
					} else
						_out.Write(name.Name, true);
					return;
				}
				if (name == S.This) {
					_out.Write("this", true);
					return;
				}
				if (name == S.Base) {
					_out.Write("base", true);
					return;
				}
				first = name.Name[1];
			}
			if (useOperatorKeyword)
			{
				_out.Write("operator", true);
				Space(SpaceOpt.AfterOperatorKeyword);
				PrintString('`', null, name.Name, true);
				return;
			}
			if (!IsIdentStartChar(first))
				isNormal = false;
			else for (int i = 1; i < name.Name.Length; i++)
				if (!IsIdentContChar(name.Name[i])) {
					isNormal = false;
					break;
				}
			if (isNormal) {
				if ((CsKeywords.Contains(name) || name == Var || name == Def) && !inSymbol)
					_out.Write("@", false);
				if (PreprocessorCollisions.Contains(name) && !inSymbol) {
					_out.Write("@#", false);
					_out.Write(name.Name.Substring(1), true);
				} else
					_out.Write(name.Name, true);
			} else {
				PrintString('`', inSymbol ? null : _Verbatim, name.Name, true);
			}
		}

		static readonly Symbol _Verbatim = GSymbol.Get("#trivia_verbatim");
		static readonly Symbol _DoubleVerbatim = S.TriviaDoubleVerbatim;
		private void PrintString(char quoteType, Symbol verbatim, string text, bool includeAtSign = false)
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
			P<@void>  (np => np._out.Write("void", true)),
			P<char>   (np => np.PrintString('\'', null, np._n.Value.ToString())),
			P<string> (np => {
				var v1 = np._n.TryGetAttr(_DoubleVerbatim);
				var v2 = v1 != null ? v1.Name : ((np._n.Style & NodeStyle.Alternate) != 0 ? _Verbatim : null);
				np.PrintString('"', v2, np._n.Value.ToString(), true);
			}),
			P<Symbol> (np => {
				np._out.Write('$', false);
				np.PrintSimpleIdent((Symbol)np._n.Value, 0, true);
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
			return (c >= '0' && c <= '9') || IsIdentStartChar(c) || c == '\'';
		}

		#endregion
	}

	/// <summary>Flags for <see cref="EcsNodePrinter.IsComplexIdentifier"/>.</summary>
	[Flags] public enum ICI
	{
		Simple = 0,
		Default = AllowOf | AllowDotted,
		AllowAttrs = 2,
		AllowOf = 8,
		AllowDotted = 16,
		AllowSubexpr = 32,
		InOf = 64,
		NameDefinition = 128, // allows in out *: e.g. Foo<in A, out B, *c>
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
			| BeforeBaseListColon | BeforeForwardArrow | BeforeConstructorColon,
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
