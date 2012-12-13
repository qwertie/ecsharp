using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Utilities;
using Loyc.Essentials;
using System.Diagnostics;
using Loyc.Math;
using Loyc.CompilerCore;
using System.Reflection;
using System.ComponentModel;
using S = Loyc.CompilerCore.CodeSymbols;
using EP = ecs.EcsPrecedence;

namespace ecs
{
	public class EcsNodePrinter
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
			SpacingOptions = SpaceOpt.Default;
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
		/// <summary>Allows pointer types, which prohibits multiplication at 
		/// statement level. It should only be enabled in an unsafe block.</summary>
		/// <remarks>If pointers are not allowed, a pointer type like int* will be 
		/// printed "#*&lt;int>" instead. If this option is enabled, it only allows
		/// pointers at statement level; an expression like "(Foo* x) = 2" will be
		/// printed "(#*&lt;Foo> x) = 2" because the parser would interpret it as
		/// a multiplication otherwise.</remarks>
		public bool AllowPointers { get; set; }
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
		
		//TODO
		// - test & prevent Foo<a><b>
		// - Remove TypeContext? To ensure it doesn't stick around in e.g. Foo<(...)>
		// - Support x `suffix backtick` properly
		// - Print backtick properly
		// - Statements!

		
		/// <summary>Causes the selected node to be printed as a data type, so 
		/// certain patterns are treated specially, e.g. <c>#of(#[], #int)</c> is 
		/// printed "int[]".</summary>
		public bool TypeContext { get; set; }
		/// <summary>Controls the locations where spaces should be emitted.</summary>
		public SpaceOpt SpacingOptions { get; set; }
		public int SpaceAroundInfixStopPrecedence { get; set; }
		public int SpaceAfterPrefixStopPrecedence { get; set; }

		/// <summary>Sets <see cref="AllowExtraParenthesis"/>, <see cref="AllowPointers"/>,
		/// <see cref="PreferOldStyleCasts"/> and <see cref="DropNonDeclarationAttributes"/> to true.</summary>
		/// <returns>this.</returns>
		public EcsNodePrinter SetPlainCSharpMode()
		{
			AllowPointers = true;
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
		struct WithType_ : IDisposable
		{
		    With_ with;
		    bool _oldTypeContext;
		    public WithType_(EcsNodePrinter self, INodeReader inner, bool isType)
		    {
		        with = new With_(self, inner);
		        _oldTypeContext = self.TypeContext;
		        self.TypeContext = isType;
		    }
		    public void Dispose()
		    {
		        with._self.TypeContext = _oldTypeContext;
		        with.Dispose();
		    }
		}

		Indented_ Indented { get { return new Indented_(this); } }
		With_ With(INodeReader inner) { return new With_(this, inner); }
		WithType_ WithType(INodeReader inner, bool isType = true) { return new WithType_(this, inner, isType); }
		
		void PrintInfixWithSpace(Symbol name, Precedence p, Ambiguity flags)
		{
			if ((SpacingOptions & SpaceOpt.AroundInfix) != 0 && p.Lo < SpaceAroundInfixStopPrecedence) {
				_out.Space();
				PrintOperatorName(name, flags);
				_out.Space();
			} else
				PrintOperatorName(name, flags);
		}

		void PrefixSpace(Precedence p)
		{
			if ((SpacingOptions & SpaceOpt.AfterPrefix) != 0 && p.Lo < SpaceAfterPrefixStopPrecedence)
				_out.Space();
		}
		void Space(SpaceOpt context)
		{
			if ((SpacingOptions & context) != 0)
				_out.Space();
		}
		void WriteThenSpace(char ch, SpaceOpt option)
		{
			_out.Write(ch, true);
			if ((SpacingOptions & option) != 0)
				_out.Space();
		}
		enum ParenFor {
			Grouping = (int)SpaceOpt.OutsideParens,
			MethodCall = (int)SpaceOpt.BeforeMethodCall,
			MacroCall = (int)SpaceOpt.BeforePossibleMacroArgs,
			KeywordCall = (int)SpaceOpt.BeforeKeywordStmtArgs,
			NewCast = (int)SpaceOpt.BeforeNewCastCall,
		}
		bool WriteOpenParen(ParenFor type, bool confirm = true)
		{
			if (confirm) {
				if (((int)SpacingOptions & (int)type) != 0)
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
				if (((int)SpacingOptions & (int)SpaceOpt.OutsideParens & (int)type) != 0)
					_out.Space();
			}
		}
		private void WriteInnerSpace(ParenFor type)
		{
			if ((SpacingOptions & (SpaceOpt.InsideParens | SpaceOpt.InsideCallParens)) != 0)
				if ((type & (ParenFor.Grouping | ParenFor.MethodCall | ParenFor.MacroCall)) != 0)
					if ((SpacingOptions & (type == ParenFor.Grouping ? SpaceOpt.InsideParens : SpaceOpt.InsideCallParens)) != 0)
						_out.Space();
		}

		#endregion

		#region Sets and dictionaries of keywords, operators and statements

		static readonly HashSet<Symbol> PreprocessorCollisions = SymbolSet(
			"#if", "#else", "#elif", "#endif", "#define", "#region", "#endregion", 
			"#pragma", "#warning", "#error", "#line"
		);

		static readonly HashSet<Symbol> TokenHashKeywords = SymbolSet(
			// >>, << and ** are special: the lexer provides them as two separate tokens
			"#~", "#!", "#%", "#^", "#&", "#&&", "#*", "#**", "#+", "#++", 
			"#-", "#--", "#=", "#==", "#{}", "#[]", "#|", "#||", @"#\", 
			"#;", "#:", "#,", "#.", "#..", "#<", "#<<", "#>", "#>>", "#/", 
			"#?", "#??", "#??.", "#??=", "#%=", "#^=", "#&=", "#*=", "#-=", 
			"#+=", "#|=", "#<=", "#>=", "#=>", "#==>", "#->"
		);

		static readonly Dictionary<Symbol,Precedence> PrefixOperators = Dictionary( 
			// This is a list of unary prefix operators only. Does not include the
			// binary prefix operator "#cast" or the unary suffix operators ++ and --.
			// Although #. can be a prefix operator, it is not included in this list
			// because it needs special treatment because its precedence is higher
			// than EP.Primary (i.e. above prefix notation). Therefore, it's printed
			// as an identifier if possible (e.g. #.(a)(x) is printed ".a(x)") and
			// uses prefix notation if not (e.g. #.(a(x)) must be in prefix form.)
			//
			// The substitute operator \ also has higher precedence than Primary, 
			// but its special treatment is in the parser: the parser produces the
			// same tree for \(x) and \x, unlike e.g. ++(x) and ++x which are 
			// different trees. Therefore we can treat \ as a normal operator in
			// the printer except that we must emit parenthesis around the argument
			// if it is anything but a simple identifier (CanAppearIn detects when
			// this is necessary.)
			P(S._Negate,    EP.Prefix), P(S._UnaryPlus,   EP.Prefix), P(S.NotBits, EP.Prefix), 
			P(S.Not,        EP.Prefix), P(S.PreInc,       EP.Prefix), P(S.PreDec,  EP.Prefix),
			P(S._AddressOf, EP.Prefix), P(S._Dereference, EP.Prefix), P(S.Forward, EP.Forward), 
			P(S.Substitute, EP.Substitute) 
		);

		static readonly Dictionary<Symbol,Precedence> InfixOperators = Dictionary(
			// This is a list of infix binary opertors only. Does not include the
			// conditional operator #? or non-infix binary operators such as a[i].
			// #, is not an operator at all and generally should not occur.
			// Note: I cancelled my plan to add a binary ~ operator because it would
			//       change the meaning of (x)~y from a type cast to concatenation.
			P(S.Mod, EP.Multiply),      P(S.XorBits, EP.XorBits), 
			P(S.AndBits, EP.AndBits),   P(S.And, EP.And),       P(S.Mul, EP.Multiply), 
			P(S.Exp, EP.Power),         P(S.Add, EP.Add),       P(S.Sub, EP.Add),
			P(S.Set, EP.Assign),        P(S.Eq, EP.Equals),     P(S.Neq, EP.Equals),
			P(S.OrBits, EP.OrBits),     P(S.Or, EP.Or),         P(S.Lambda, EP.Lambda),
			P(S.DotDot, EP.Range),      P(S.LT, EP.Compare),    P(S.Shl, EP.Shift),
			P(S.GT, EP.Compare),        P(S.Shr, EP.Shift),     P(S.Div, EP.Multiply),
			P(S.MulSet, EP.Assign),     P(S.DivSet, EP.Assign), P(S.ModSet, EP.Assign),
			P(S.SubSet, EP.Assign),     P(S.AddSet, EP.Assign), P(S.ConcatSet, EP.Assign),
			P(S.ExpSet, EP.Assign),     P(S.ShlSet, EP.Assign), P(S.ShrSet, EP.Assign),
			P(S.XorBitsSet, EP.Assign), P(S.AndBitsSet, EP.Assign), P(S.OrBitsSet, EP.Assign),
			P(S.NullCoalesce, EP.OrIfNull), P(S.NullDot, EP.NullDot), P(S.NullCoalesceSet, EP.Assign),
			P(S.LE, EP.Compare),        P(S.GE, EP.Compare),    P(S._Arrow, EP.Backtick),  
			P(S.PtrArrow, EP.Primary),  P(S.Is, EP.Compare),    P(S.As, EP.Compare),
			P(S.UsingCast, EP.Compare)
		);

		static readonly Dictionary<Symbol,Precedence> CastOperators = Dictionary(
			P(S.Cast, EP.Prefix),      // (Foo)x      (preferred form)
			P(S.As, EP.Compare),       // x as Foo    (preferred form)
			P(S.UsingCast, EP.Compare) // x using Foo (preferred form)
		);

		static readonly HashSet<Symbol> ListOperators = new HashSet<Symbol>(new[] {
			S.List, S.Tuple, S.CodeQuote, S.CodeQuoteSubstituting});

		static readonly Dictionary<Symbol,Precedence> SpecialCaseOperators = Dictionary(
			// Operators that need special treatment (neither prefix nor infix nor casts)
			// #. #of #[] #postInc, #postDec, #, #'@', #'@@'. #tuple #?
			P(S.QuestionMark,EP.IfElse),  // a?b:c
			P(S.Bracks,      EP.Primary), // a[]
			P(S.PostInc,     EP.Primary), // x++
			P(S.PostDec,     EP.Primary), // x--
			P(S.Of,          EP.Primary), // List<int>, int[], int?, int*
			P(S.Dot,         EP.Primary), // a.b.c
			P(S.New,         EP.Prefix)   // new A()
		);

		static readonly HashSet<Symbol> CallOperators = new HashSet<Symbol>(new[] {
			S.Typeof, S.Checked, S.Unchecked, S.Default,
		});

		delegate bool OperatorPrinter(EcsNodePrinter @this, Precedence mainPrec, Precedence context, Ambiguity flags);
		static Dictionary<Symbol, Pair<Precedence, OperatorPrinter>> OperatorPrinters = OperatorPrinters_();
		static Dictionary<Symbol, Pair<Precedence, OperatorPrinter>> OperatorPrinters_()
		{
			// Build a dictionary of printers for each operator name.
			var d = new Dictionary<Symbol, Pair<Precedence, OperatorPrinter>>();
			
			// Create open delegates to the printers for various kinds of operators
			var prefix = (OperatorPrinter)Delegate.CreateDelegate(typeof(OperatorPrinter),
				typeof(EcsNodePrinter).GetMethod("AutoPrintPrefixOperator"));
			var infix = (OperatorPrinter)Delegate.CreateDelegate(typeof(OperatorPrinter),
				typeof(EcsNodePrinter).GetMethod("AutoPrintInfixOperator"));
			var both = (OperatorPrinter)Delegate.CreateDelegate(typeof(OperatorPrinter),
				typeof(EcsNodePrinter).GetMethod("AutoPrintPrefixOrInfixOperator"));
			var cast = (OperatorPrinter)Delegate.CreateDelegate(typeof(OperatorPrinter),
				typeof(EcsNodePrinter).GetMethod("AutoPrintCastOperator"));
			var list = (OperatorPrinter)Delegate.CreateDelegate(typeof(OperatorPrinter),
				typeof(EcsNodePrinter).GetMethod("AutoPrintListOperator"));
			var ident = (OperatorPrinter)Delegate.CreateDelegate(typeof(OperatorPrinter),
				typeof(EcsNodePrinter).GetMethod("AutoPrintComplexIdentOperator"));
			var other = (OperatorPrinter)Delegate.CreateDelegate(typeof(OperatorPrinter),
				typeof(EcsNodePrinter).GetMethod("AutoPrintOtherSpecialOperator"));
			
			foreach (var p in PrefixOperators)
				d.Add(p.Key, G.Pair(p.Value, prefix));
			foreach (var p in InfixOperators)
				if (d.ContainsKey(p.Key))
					d[p.Key] = G.Pair(p.Value, both); // both prefix and infix
				else
					d.Add(p.Key, G.Pair(p.Value, infix));
			foreach (var p in CastOperators)
				d[p.Key] = G.Pair(p.Value, cast);
			foreach (var op in ListOperators)
				d[op] = G.Pair(Precedence.MaxValue, list);
			foreach (var p in SpecialCaseOperators) {
				var handler = p.Key == S.Of || p.Key == S.Dot ? ident : other;
				d.Add(p.Key, G.Pair(p.Value, handler));
			}

			return d;
		}

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
		public bool IsSimpleSymbolWithoutPAttrs(INodeReader self)
		{
			return self.IsSimpleSymbol && (DropNonDeclarationAttributes || !HasPAttrs(self));
		}
		public bool IsSimpleSymbolWithoutPAttrs(INodeReader self, Symbol name)
		{
			return self.Name == name && IsSimpleSymbolWithoutPAttrs(self);
		}

		public bool IsSpaceStatement()
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
			if (SpaceDefinitionStmts.Contains(_n.Name) && HasSimpleHeadWPA(_n) && MathEx.IsInRange(_n.ArgCount, 1, 3))
			{
				INodeReader name = _n.TryGetArg(0), bases = _n.TryGetArg(1), body = _n.TryGetArg(2);
				if (HasPAttrs(name)) return false;
				if (bases == null) return true;
				if (HasPAttrs(bases)) return false;
				if (bases.IsSimpleSymbol(S.Missing) || bases.Calls(S.List))
				{
					if (body == null) return true;
					if (HasPAttrs(body)) return false;
					return (CallsWPAIH(body, S.Braces) ||
						   CallsWPAIH(body, S.List));
				}
			}
			return false;
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

		static INodeReader SinglePAttr(INodeReader self)
		{
			// Returns the single printable attribute, or null if none/multiple
			INodeReader result = null, a;
			for (int i = 0, c = self.AttrCount; i < c; i++)
				if ((a=self.Attrs[i]).IsPrintableAttr())
					if (result != null)
						return null;
					else
						result = a;
			return result;
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
			if ((f & (ICI.AllowAttrsRecursive | ICI.AllowAttrs)) == 0 && HasPAttrs(n))
			{
				// Attribute(s) are illegal, except 'in' and 'out' when TypeParamDefinition
				INodeReader single;
				if ((f & ICI.TypeParamDefinition) == 0 || ((single=SinglePAttr(n)) != S.In && single != S.Out))
					return false;
			}

			if (n.IsSimpleSymbol) // !IsCall && !IsLiteral && Head == null
				return true;
			if (CallsWPAIH(n, S.Substitute, 1))
				return true;
			if (CallsWPAIH(n, S._TemplateArg, 1))
				return (f & ICI.TypeParamDefinition) != 0;

			if (n.IsParenthesizedExpr()) // !self.IsCall && self.Head != null
			{
				// TODO: detect subexpressions that are legal in typeof
				return (f & ICI.AllowSubexpr) != 0;
			}
			if (CallsWPAIH(n, S.List, 1))
				return (f & ICI.InOf) != 0;

			if (CallsMinWPAIH(n, S.Of, 1) && (f & ICI.AllowOf) != 0) {
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
			if (CallsMinWPAIH(n, S.Dot, 1) && (f & ICI.AllowDotted) != 0) {
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

		public bool IsDefinitionStmt()
		{
			if (DefinitionStmts.Contains(_n.Name)) {
				if (_n.Name == S.Var)
					return IsVariableDecl(true, true);
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

		public bool IsLabelStmt()
		{
			return _n.Name == S.Label || CallsWPAIH(_n, S.Case);
		}

		public bool IsNamedArgument()
		{
 			return CallsWPAIH(_n, S.NamedArg, 2) && IsSimpleSymbolWithoutPAttrs(_n.Args[0]);
		}

		#endregion

		#region PrintStmt and related

		public void PrintStmt(bool inCommaSeparatedList = false)
		{
			_out.BeginStatement();

			bool writeTerminator = true;
			if (AllNonExprStmts.Contains(_n.Name)) {
				// Special code exists to handle this kind of node
				if (IsDefinitionStmt())
					PrintDefinitionStmt();
				else if (IsBlockOfStmts(_n))
					PrintBlockOfStmts(inCommaSeparatedList);
				else if (IsBlockStmt(_n))
					PrintBlockStmt();
				else if (IsSimpleStmt(_n))
					PrintSimpleStmt();
				else if (IsLabelStmt()) {
					PrintLabelStmt();
					writeTerminator = false;
				} else
					PrintExpr(StartStmt);
			} else
				PrintExpr(StartStmt);
			
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
				PrintExpr(StartStmt);
		}

		internal void PrintSpaceStmt()
		{
			PrintExpr(StartStmt);
		}

		internal void PrintLabelStmt()
		{
			if (_n.Name == S.Label) {
				PrintExpr(StartStmt);
			} else if (_n.Name == S.Case) {
				for (int i = 0; i < _n.ArgCount; i++)
					using (var _ = With(_n.Args[i]))
						PrintExpr(StartStmt);
			}
			_out.Write(':', true);
		}

		internal void PrintBlockStmt()
		{
			// Block statements:
			// S.If, S.Checked, S.DoWhile, S.Fixed, S.For, S.ForEach, S.If, S.Lock, 
			// S.Switch, S.Try, S.Unchecked, S.Using, S.While
			PrintExpr(StartStmt); // TODO
		}

		internal void PrintSimpleStmt()
		{
			// Simple statements:
			// S.Break, S.Continue, S.Goto, S.GotoCase, S.Return, S.Throw
			PrintExpr(StartStmt); // TODO
		}

		internal void PrintBlockOfStmts(bool inCommaSeparatedList)
		{
			PrintAttrs(StartStmt, AttrStyle.AllowKeywordAttrs);

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

		static readonly int MinPrec = Precedence.MinValue.Lo;
		/// <summary>Context: beginning of expression (#var must have initial value)</summary>
		public static readonly Precedence StartExpr      = new Precedence(MinPrec, MinPrec, MinPrec);
		/// <summary>Context: beginning of statement (#namedArg not supported, allow multiple #var decl)</summary>
		public static readonly Precedence StartStmt      = new Precedence(MinPrec+2, MinPrec+2, MinPrec+2);
		/// <summary>Context: middle of expression, top level (#var and #namedArg not supported)</summary>
		public static readonly Precedence ContinueExpr   = new Precedence(MinPrec+3, MinPrec+3, MinPrec+3);

		/// <summary>Flags that represent special situations in EC# syntax.</summary>
		[Flags] public enum Ambiguity
		{
			/// <summary>The expression can contain uninitialized variable 
			/// declarations because it is the subject of an assignment or is a 
			/// statement, e.g. in the tree "(x + y, int z) = (a, b)", this flag is 
			/// passed down to "(x + y, int z)" and then down to "int y" and 
			/// "x + y", but it doesn't propagate down to "x", "y" and "int".</summary>
			AssignmentLhs = 1,
			/// <summary>The expression is the right side of a traditional cast, so 
			/// the printer must avoid ambiguity in case of the following prefix 
			/// operators: (Foo)-x, (Foo)+x, (Foo)&x, (Foo)*x, (Foo)~x, (Foo)==>x,
			/// (Foo)++(x), (Foo)--(x) (the (Foo)++(x) case is parsed as a post-
			/// increment and a call).</summary>
			CastRhs = 2,
			/// <summary>The expression is in a location where, if it has the syntax 
			/// of a data type, it will be treated as a cast. This occurs when a 
			/// call that is printed with prefix notation has a parenthesized head
			/// node, e.g. (head)(arg). The head node can avoid the syntax of a data 
			/// type by adding "[ ]" (an empty set of attributes) at the beginning
			/// of the expression.</summary>
			AvoidCastAppearance = 4,
			/// <summary>Used to communicate to the operator printers that a call
			/// should be expressed with the backtick operator.</summary>
			UseBacktick = 256,
		}

		public void PrintExpr()
		{
			PrintExpr(StartExpr, Ambiguity.AssignmentLhs);
		}
		protected internal void PrintExpr(Precedence context, Ambiguity flags = 0)
		{
			if (context > EP.Primary)
			{
				Debug.Assert((flags & Ambiguity.AssignmentLhs) == 0);
				// Above EP.Primary (inside '\' or unary '.'), we can't use prefix 
				// notation or most other operators so we're very limited in what
				// we can print. If we have no attributes we can try to print as
				// an operator (this will work for prefix operators such as '++') 
				// and if that doesn't work, write the expr in parenthesis.
				if (!HasPAttrs(_n))
				{
					if (_n.IsSimpleSymbol) {
						PrintSimpleSymbolOrLiteral();
						return;
					} else if (AutoPrintOperator(context, flags))
						return;
				}
				WriteOpenParen(ParenFor.Grouping);
				PrintExpr(StartExpr, flags);
				WriteCloseParen(ParenFor.Grouping);
				return;
			}

			bool startStmt = context.RangeEquals(StartStmt);
			bool startExpr = context.RangeEquals(StartExpr);
			bool isVarDecl = (startStmt || startExpr) && 
				 IsVariableDecl(startStmt, startStmt || (flags & Ambiguity.AssignmentLhs) != 0);
			
			bool needCloseParen = PrintAttrs(context, isVarDecl ? AttrStyle.IsDefinition : AttrStyle.AllowKeywordAttrs);

			if (startExpr) {
				if (IsNamedArgument()) {
					using (With(_n.TryGetArg(0)))
						PrintExpr(EP.Primary.LeftContext(context));
					_out.Write(':', true);
					using (With(_n.TryGetArg(1)))
						PrintExpr(StartExpr);
					return;
				}
			}

			if (isVarDecl)
				PrintVariableDecl(false);
			else if (!AutoPrintOperator(context, flags))
				PrintPrefixNotation(context, false, true, flags, true);

			if (needCloseParen)
				_out.Write(')', true);
		}

		// Checks if an operator with precedence 'prec' can appear in this context.
		bool CanAppearIn(Precedence prec, Precedence context, out bool extraParens, bool prefix = false)
		{
			extraParens = false;
			if (prefix ? prec.PrefixCanAppearIn(context) 
				       : prec.CanAppearIn(context) && (MixImmiscibleOperators || prec.ShouldAppearIn(context)))
				return true;
			if (AllowExtraParenthesis || !EP.Primary.CanAppearIn(context))
				return extraParens = true;
			return false;
		}
		// Checks if an operator that may or may not be configured to output in 
		// `backtick notation` can appear in this context; this method may toggle
		// backtick notation to make it acceptable (in terms of precedence).
		bool CanAppearIn(ref Precedence prec, Precedence context, out bool extraParens, ref bool backtick, bool prefix = false)
		{
			var altPrec = EP.Backtick;
			if (backtick) MathEx.Swap(ref prec, ref altPrec);
			if (CanAppearIn(prec, context, out extraParens, prefix && !backtick))
				return true;

			backtick = !backtick;
			MathEx.Swap(ref prec, ref altPrec);
			return CanAppearIn(prec, context, out extraParens, prefix && !backtick);
		}

		private bool AutoPrintOperator(Precedence context, Ambiguity flags)
		{
			if (!_n.IsCall || !_n.HasSimpleHead)
				return false;
			Pair<Precedence, OperatorPrinter> info;
			if (OperatorPrinters.TryGetValue(_n.Name, out info))
				return info.Item2(this, info.Item1, context, flags);
			else if (_n.BaseStyle == NodeStyle.Operator)
			{
				if (_n.ArgCount == 2)
					return AutoPrintInfixOperator(EP.Backtick, context, flags | Ambiguity.UseBacktick);
				//if (_n.ArgCount == 1)
				//	return AutoPrintPrefixOperator(EP.Backtick, context, flags | Ambiguity.UseBacktick);
			}
			return false;
		}

		// These methods are public but hidden because they are found by reflection 
		// and they should be compatible with a partial-trust environment.
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintPrefixOperator(Precedence precedence, Precedence context, Ambiguity flags)
		{
			if (_n.ArgCount != 1)
				return false;
			// Attributes on the child disqualify operator notation (except \)
			var name = _n.Name;
			var arg = _n.TryGetArg(0);
			if (HasPAttrs(arg) && name != S.Substitute)
				return false;

			bool needParens;
			if (CanAppearIn(precedence, context, out needParens, true))
			{
				// Check for the ambiguous case of (Foo)-x, (Foo)*x, etc.
				if ((flags & Ambiguity.CastRhs) != 0 && !needParens && (
					name == S._Dereference || name == S.PreInc || name == S.PreDec || 
					name == S._UnaryPlus || name == S._Negate || name == S.NotBits ||
					name == S._AddressOf || name == S.Forward))
				{
					if (AllowExtraParenthesis)
						needParens = true; // Resolve ambiguity with extra parens
					else
						return false; // Fallback to prefix notation
				}

				if (WriteOpenParen(ParenFor.Grouping, needParens))
					context = StartExpr;
				_out.Write(_n.Name.Name.Substring(1), true);
				PrefixSpace(precedence);
				PrintExpr(arg, precedence.RightContext(context));
				//if (backtick) {
				//    Debug.Assert(precedence == EP.Backtick);
				//    if ((SpacingOptions & SpaceOpt.AroundInfix) != 0 && precedence.Lo < SpaceAroundInfixStopPrecedence)
				//        _out.Space();
				//    PrintOperatorName(_n.Name, Ambiguity.UseBacktick);
				//}
				WriteCloseParen(ParenFor.Grouping, needParens);
				return true;
			}
			return false;
		}
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintInfixOperator(Precedence prec, Precedence context, Ambiguity flags)
		{
			var name = _n.Name;
			Debug.Assert(!CastOperators.ContainsKey(name)); // not called for cast operators
			if (_n.ArgCount != 2)
				return false;
			// Attributes on the children disqualify operator notation
			INodeReader left = _n.TryGetArg(0), right = _n.TryGetArg(1);
			if (HasPAttrs(left) || HasPAttrs(right))
				return false;

			bool needParens, backtick = (_n.Style & NodeStyle.Alternate) != 0;
			if (CanAppearIn(ref prec, context, out needParens, ref backtick))
			{
				if (AllowPointers && name == S.Mul && context.Lo == StartStmt.Lo)
					return false;
				if (WriteOpenParen(ParenFor.Grouping, needParens))
					context = StartExpr;
				PrintExpr(left, prec.LeftContext(context), (name == S.Set || name == S.Lambda ? Ambiguity.AssignmentLhs : 0));
				if (backtick)
					flags |= Ambiguity.UseBacktick;
				PrintInfixWithSpace(_n.Name, prec, flags);
				PrintExpr(right, prec.RightContext(context));
				WriteCloseParen(ParenFor.Grouping, needParens);
				return true;
			}
			return false;
		}
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintPrefixOrInfixOperator(Precedence infixPrec, Precedence context, Ambiguity flags)
		{
			if (_n.ArgCount == 2)
				return AutoPrintInfixOperator(infixPrec, context, flags);
			else
				return AutoPrintPrefixOperator(PrefixOperators[_n.Name], context, flags);
		}
		private void PrintOperatorName(Symbol name, Ambiguity flags)
		{
			if ((flags & Ambiguity.UseBacktick) != 0)
				PrintString('`', null, name.Name);
			else {
				Debug.Assert(name.Name[0] == '#');
				string opName = name.Name.Substring(1);
				_out.Write(opName, true);
			}
		}
		
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintCastOperator(Precedence precedence, Precedence context, Ambiguity flags)
		{
			if (_n.ArgCount != 2)
				return false;

			// Cast operators can have attributes on the second argument using 
			// alternate notation, e.g. x(as [A] Foo) is legal but "x as [A] Foo"
			// is not, because attributes must only appear at the beginning of an 
			// expression and only the second case treats the text after 'as' as 
			// the beginning of a new expression. Also, because a standard cast 
			// like (Foo)(x) is ambiguous (is x being cast to type Foo, or is a
			// delegate named Foo being called with x as an argument?), an 
			// attribute list can be used to resolve the ambiguity. So (Foo)(x) 
			// is considered a cast, while ([ ] Foo)(x) is a call to Foo in which 
			// Foo happens to be placed in parenthesis. Thus, if target type of a 
			// cast has attributes, it must be expressed in alternate form, e.g.
			// (x)(->[A] Foo), or in prefix form.
			//
			// There is an extra rule for (X)Y casts: X must be a complex (or 
			// simple) identifier, since anything else won't be parsed as a cast.
			Symbol name = _n.Name;
			bool alternate = (_n.Style & NodeStyle.Alternate) != 0 && !PreferOldStyleCasts;
			INodeReader subject = _n.TryGetArg(0), target = _n.TryGetArg(1);
			if (HasPAttrs(subject))
				return false;
			if (HasPAttrs(target) || (name == S.Cast && !IsComplexIdentifier(target)))
				alternate = true;
			
			bool needParens;
			if (alternate)
				precedence = EP.Primary;
			if (!CanAppearIn(precedence, context, out needParens)) {
				// There are two different precedences for cast operators; we prefer 
				// the traditional forms (T)x, x as T, x using T which have lower 
				// precedence, but they don't work in this context so consider using 
				// x(->T), x(as T) or x(using T) instead.
				alternate = true;
				precedence = EP.Primary;
				if (!CanAppearIn(precedence, context, out needParens))
					return false;
			}

			if (alternate && PreferOldStyleCasts)
				return false; // old-style cast is impossible here

			if (WriteOpenParen(ParenFor.Grouping, needParens))
				context = StartExpr;

			if (alternate) {
				PrintExpr(subject, precedence.LeftContext(context));
				WriteOpenParen(ParenFor.NewCast);
				_out.Write(GetCastText(_n.Name), true);
				Space(SpaceOpt.AfterCastArrow);
				PrintType(target, StartExpr);
				WriteCloseParen(ParenFor.NewCast);
			} else {
				if (_n.Name == S.Cast) {
					WriteOpenParen(ParenFor.Grouping);
					PrintType(target, ContinueExpr);
					WriteCloseParen(ParenFor.Grouping);
					Space(SpaceOpt.AfterCast);
					PrintExpr(subject, precedence.RightContext(context), Ambiguity.CastRhs);
				} else {
					// "x as y" or "x using y"
					PrintExpr(subject, precedence.LeftContext(context));
					_out.Write(GetCastText(_n.Name), true);
					PrintType(target, precedence.RightContext(context));
				}
			}

			WriteCloseParen(ParenFor.Grouping, needParens);
			return true;
		}
		private string GetCastText(Symbol name)
		{
			if (name == S.UsingCast) return "using";
			if (name == S.As) return "as";
			return "->";
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintListOperator(Precedence precedence, Precedence context, Ambiguity flags)
		{
			// Handles one of: #tuple #'@' #'@@' #.
			int argCount = _n.ArgCount;
			Symbol name = _n.Name;
			Debug.Assert(_n.IsCall);
			
			if (name == S.Tuple) {
				// #tuple requires an argument because () is the #literal for void
				if (name == S.Tuple && argCount < 1)
					return false;

				WriteOpenParen(ParenFor.Grouping);
				int c = _n.ArgCount;
				for (int i = 0; i < c; i++)
				{
					if (i != 0) WriteThenSpace(',', SpaceOpt.AfterComma);
					PrintExpr(_n.TryGetArg(i), StartExpr, flags & Ambiguity.AssignmentLhs);
				}
				if (c == 1)
					_out.Write(',', true);
				WriteCloseParen(ParenFor.Grouping);
			} else {
				Debug.Assert(name == S.CodeQuote || name == S.CodeQuoteSubstituting || name == S.List);
				// TODO: support @{...}, @@{...}, #{...}
				_out.Write(name == S.CodeQuote ? "@" : name == S.CodeQuoteSubstituting ? "@@" : "#", false);
				WriteOpenParen(ParenFor.Grouping);
				for (int i = 0, c = _n.ArgCount; i < c; i++)
				{
					if (i != 0) WriteThenSpace(',', SpaceOpt.AfterComma);
					PrintExpr(_n.TryGetArg(i), StartExpr);
				}
				WriteCloseParen(ParenFor.Grouping);
			}
			return true;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintComplexIdentOperator(Precedence precedence, Precedence context, Ambiguity flags)
		{
			// Handles #of and #.
			int argCount = _n.ArgCount;
			Symbol name = _n.Name;
			Debug.Assert((name == S.Of || name == S.Dot) && _n.IsCall);
			var first = _n.TryGetArg(0);

			if (first == null)
				return false; // no args
			bool needParens;
			if (!CanAppearIn(precedence, context, out needParens) || needParens)
				return false; // this only happens inside \ operator, e.g. \(a.b)

			if (name == S.Dot) {
				if (argCount < 1)
					return false;
				// The trouble with the dot is its high precedence; because of 
				// this, arguments after a dot cannot use prefix notation as a 
				// fallback. For example "#.(a, b(c))" cannot be printed "a.b(c)"
				// since that means #.(a, b)(c)". The first argument to non-
				// unary "#." can use prefix notation safely though, e.g. 
				// "#.(b(c), a)" can (and must) be printed "b(c).a". Also,
				// #. must not directly contain other dotted expressions.
				// So: each argument after a dot must not be any kind of call 
				// and must not have attributes.
				if (argCount == 1) {
					if (first.IsCall || HasPAttrs(first))
						return false;
				} else {
					if (first.CallsMin(S.Dot, 1) || HasPAttrs(first))
						return false;
					for (int i = 1; i < argCount; i++) {
						var arg = _n.TryGetArg(i);
						if (arg.IsCall || HasPAttrs(arg))
							return false;
					}
				}
			} else if (name == S.Of) {
				if (!IsComplexIdentifier(_n, ICI.Default | ICI.AllowAttrs))
					return false;
			}

			if (name == S.Dot)
			{
				if (argCount == 1) {
					_out.Write('.', true);
					PrintExpr(first, EP.Substitute);
				} else {
					PrintExpr(first, precedence.LeftContext(context));
					for (int i = 1; i < argCount; i++) {
						_out.Write('.', true);
						PrintExpr(_n.TryGetArg(i), precedence);
					}
				}
			}
			else if (_n.Name == S.Of)
			{
				if (_n.ArgCount == 2 && _n.Args[0].IsSimpleSymbol && TypeContext)
				{
					var kind = first.Name;
					bool array = S.IsArrayKeyword(kind);
					if (array || kind == S.QuestionMark || (kind == S.Mul && AllowPointers))
					{
						PrintType(_n.TryGetArg(1), EP.Primary.LeftContext(context));
						if (array)
							_out.Write(kind.Name.Substring(1), true); // e.g. [] or [,]
						else
							_out.Write(kind == S.Mul ? '*' : '?', true);
						return true;
					}
				}

				PrintExpr(first, precedence.LeftContext(context));
				_out.Write('<', true);
				for (int i = 1; i < argCount; i++) {
					if (i > 1)
						WriteThenSpace(',', SpaceOpt.AfterCommaInOf);
					PrintType(_n.TryGetArg(i), ContinueExpr);
				}
				_out.Write('>', true);
			}
			else 
			{
				Debug.Assert(_n.Name == S.Substitute);
				G.Verify(AutoPrintOperator(ContinueExpr, 0));
			}
			return true;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintOtherSpecialOperator(Precedence precedence, Precedence context, Ambiguity flags)
		{
			// Handles one of: #? #[] #postInc #postDec
			int argCount = _n.ArgCount;
			Symbol name = _n.Name;
			if (argCount < 1)
				return false; // no args
			bool needParens;
			if (!CanAppearIn(precedence, context, out needParens))
				return false; // precedence fail

			bool newArrayOf = false;
			// Verify that the special operator can appear at this precedence 
			// level and that its arguments fit the operator's constraints.
			var first = _n.TryGetArg(0);
			if (name == S.Bracks) {
				// Careful: a[] means #of(#[], a) in a type context, #[](a) otherwise
				int minArgs = TypeContext ? 2 : 1;
				if (argCount < minArgs || HasPAttrs(first))
					return false;
			} else if (name == S.QuestionMark) {
				if (argCount != 3 || HasPAttrs(first) || HasPAttrs(_n.TryGetArg(1)) || HasPAttrs(_n.TryGetArg(2)))
					return false;
			} else if (name == S.New) {
				if (HasPAttrs(first))
					return false;
				// There are two basic uses of new:
				// 1. Init an object: new Foo<Bar>() { ... }
				// 2. Init an array:  new int[] { ... }, new[] { ... }
				if (first.Calls(S.Of, 2) && first.TryGetArg(0).Name == S.Bracks) { // e.g. int[]
					newArrayOf = true;
					if (!IsComplexIdentifier(first))
						return false;
				} else {
					if (first.IsCall) {
						if (!IsComplexIdentifierOrNull(first.Head))
							return false;
					} else {
						// If there is only one argument and it's not a call, it must be "new[] {}"
						if (argCount == 1 && !(IsSimpleSymbolWithoutPAttrs(first, S.Bracks)))
							return false;
					}
				}
			} else {
				Debug.Assert(name == S.PostInc || name == S.PostDec);
				if (argCount != 1 || HasPAttrs(first))
					return false;
			}

			// Print the thing!
			WriteOpenParen(ParenFor.Grouping, needParens);

			if (name == S.Bracks)
			{
				PrintExpr(first, precedence.LeftContext(context));
				Space(SpaceOpt.BeforeMethodCall);
				_out.Write('[', true);
				Space(SpaceOpt.InsideCallParens);
				for (int i = 1, c = _n.ArgCount; i < c; i++)
				{
					if (i != 1) WriteThenSpace(',', SpaceOpt.AfterComma);
					PrintExpr(_n.TryGetArg(i), StartExpr);
				}
				Space(SpaceOpt.InsideCallParens);
				_out.Write(']', true);
			}
			else if (name == S.QuestionMark)
			{
				PrintExpr(_n.TryGetArg(0), precedence.LeftContext(context));
				PrintInfixWithSpace(S.QuestionMark, EP.IfElse, 0);
				PrintExpr(_n.TryGetArg(1), ContinueExpr);
				PrintInfixWithSpace(S.Colon, EP.IfElse, 0);
				PrintExpr(_n.TryGetArg(2), precedence.RightContext(context));
			}
			else if (name == S.New)
			{
				_out.Write("new", true);
				_out.Space();
				
				if (newArrayOf)
					PrintType(first, EP.Primary.LeftContext(context));
				else if (first.Name == S.Bracks && first.IsSimpleSymbol)
					_out.Write("[]", true);
				else {
					PrintExpr(first, EP.Primary.LeftContext(context));
					if (argCount == 1)
						goto noBraces;
				}

				Space(SpaceOpt.BeforeNewInitBrace);
				WriteThenSpace('{', SpaceOpt.InsideBraceInExpr);
				for (int i = 1; i < argCount; i++) {
					if (i != 1)
						WriteThenSpace(',', SpaceOpt.AfterComma);
					PrintExpr(_n.TryGetArg(i), StartExpr);
				}
				Space(SpaceOpt.InsideBraceInExpr);
				_out.Write('}', true);
				noBraces: ;
			}
			else
			{
				Debug.Assert(name == S.PostInc || name == S.PostDec);
				PrintExpr(first, precedence.LeftContext(context));
				_out.Write(name == S.PostInc ? "++" : "--", true);
			}

			WriteCloseParen(ParenFor.Grouping, needParens);
			return true;
		}

		
		
		void PrintExpr(INodeReader n, Precedence context, Ambiguity flags = 0)
		{
			using (With(n))
				PrintExpr(context, flags);
		}
		void PrintType(INodeReader n, Precedence context, bool isType = true)
		{
			using (WithType(n, isType))
				PrintExpr(context);
		}

		public void PrintPrefixNotation(bool recursive = true, bool purePrefixNotation = false)
		{
			PrintPrefixNotation(StartExpr, recursive, purePrefixNotation);
		}
		internal void PrintPrefixNotation(Precedence context, bool recursive, bool purePrefixNotation, Ambiguity flags = 0, bool skipAttrs = false)
		{
			Debug.Assert(!(context > EP.Primary));
			bool needCloseParen = false;
			if (!skipAttrs)
				needCloseParen = PrintAttrs(context, purePrefixNotation ? AttrStyle.NoKeywordAttrs : AttrStyle.AllowKeywordAttrs);

			if (!purePrefixNotation && IsComplexIdentifier(_n, ICI.Default | ICI.AllowAttrs))
			{
				PrintExpr(context);
				return;
			}

			// Print the head
			bool isArgType = false;
			if (HasSimpleHeadWPA(_n))
			{
				if (CallOperators.Contains(_n.Name)) {
					PrintOperatorName(_n.Name, 0);
					isArgType = (_n.Name == S.Default || _n.Name == S.Typeof);
				} else
					PrintSimpleSymbolOrLiteral();
			} 
			else if (_n.IsParenthesizedExpr())
			{
				WriteOpenParen(ParenFor.Grouping);
				bool extraClose = false;
				if ((flags & Ambiguity.AvoidCastAppearance) != 0) {
					if (AllowExtraParenthesis) {
						extraClose = true;
						_out.Write('(', true);
					} else
						_out.Write("[ ] ", true);
				}
				PrintExprOrPrefixNotation(_n.Head, StartExpr, recursive, purePrefixNotation, flags & Ambiguity.AssignmentLhs);
				if (extraClose)
					_out.Write(')', true);
				WriteCloseParen(ParenFor.Grouping);
			}
			else if (!purePrefixNotation && IsComplexIdentifier(_n.Head)) {
				PrintExpr(_n.Head, EP.Primary.LeftContext(context));
			} else {
				Debug.Assert(_n.IsCall);
				PrintExprOrPrefixNotation(_n.Head, EP.Primary.LeftContext(context), recursive, purePrefixNotation, Ambiguity.AvoidCastAppearance);
			}

			// Print args, if any
			if (_n.IsCall) {
				WriteOpenParen(ParenFor.MethodCall);
				var args = _n.Args;
				for (int i = 0, c = _n.ArgCount; i < c; i++) {
					if (i != 0)
						WriteThenSpace(',', SpaceOpt.AfterComma);
					if (isArgType && !recursive)
						PrintType(args[i], StartExpr);
					else
						PrintExprOrPrefixNotation(args[i], StartExpr, recursive, recursive ? purePrefixNotation : false);
				}
				WriteCloseParen(ParenFor.MethodCall);
			}
			if (needCloseParen)
				_out.Write(')', true);
		}

		private void PrintSimpleSymbolOrLiteral()
		{
			Debug.Assert(_n.HasSimpleHead);
			if (_n.IsLiteral)
				PrintLiteral();
			else
				PrintIdent(_n.Name, false);
		}
		internal void PrintExprOrPrefixNotation(INodeReader expr, Precedence context, bool prefix, bool purePrefixNotation, Ambiguity flags = 0)
		{
			using (With(expr))
				PrintExprOrPrefixNotation(context, prefix, purePrefixNotation, flags);
		}
		internal void PrintExprOrPrefixNotation(Precedence context, bool prefix, bool purePrefixNotation, Ambiguity flags)
		{
			if (prefix)
				PrintPrefixNotation(context, true, purePrefixNotation, flags);
			else
				PrintExpr(context, flags);
		}

		//private void PrintTypeName() { PrintTypeOrMethodName(true); }
		//private void PrintTypeOrMethodName(bool isType)
		//{
		//    if (!_n.IsCall)
		//    {
		//        PrintIdent(_n.Name, isType);
		//        return;
		//    }

		//    if (_n.Name == S.Dot)
		//    {
		//        for (int i = 0; i < _n.ArgCount; i++) {
		//            if (i != 0)
		//                _out.Write('.', true);
		//            using (With(_n.Args[i]))
		//                PrintTypeOrMethodName(isType);
		//        }
		//    }
		//    else if (_n.Name == S.Of)
		//    {
		//        if (_n.ArgCount == 2 && _n.Args[0].IsSimpleSymbol && isType)
		//        {
		//            var name = _n.Args[0].Name;
		//            if (name == S.QuestionMark) // nullable
		//            {
		//                using (With(_n.Args[1]))
		//                    PrintTypeOrMethodName(true);
		//                _out.Write("? ", true);
		//                return;
		//            }
		//            else if (name == S.Mul && AllowPointers) // pointer
		//            {
		//                using (With(_n.Args[1]))
		//                    PrintTypeOrMethodName(true);
		//                _out.Write("* ", true);
		//                return;
		//            }
		//            else if (S.IsArrayKeyword(name))
		//            {
		//                using (var _ = With(_n.Args[1]))
		//                    PrintTypeOrMethodName(true);
		//                _out.Write(name.Name.Substring(1), true); // e.g. [] or [,]
		//                return;
		//            }
		//        }
		//        for (int i = 0; i < _n.ArgCount; i++) {
		//            if (i > 1)
		//                _out.Write(',', true);
		//            using (var _ = With(_n.Args[i]))
		//                PrintTypeOrMethodName(i != 0);
		//            if (i == 0)
		//                _out.Write('<', true);
		//        }
		//        _out.Write('>', true);
		//    }
		//    else 
		//    {
		//        Debug.Assert(_n.Name == S.Substitute);
		//        G.Verify(AutoPrintPrefixOperator(EP.Substitute, ContinueExpr));
		//    }
		//}

		private void PrintVariableDecl(bool andAttrs) // skips attributes
		{
			if (andAttrs)
				PrintAttrs(StartExpr, AttrStyle.IsDefinition);

			Debug.Assert(_n.Name == S.Var);
			var a = _n.Args;
			PrintType(a[0], ContinueExpr);
			_out.Space();
			for (int i = 1; i < a.Count; i++) {
				var var = a[i];
				if (i > 1)
					WriteThenSpace(',', SpaceOpt.AfterComma);
				PrintIdent(var.Name, false);
				if (var.IsCall) {
					PrintInfixWithSpace(S.Set, EP.Assign, 0);
					PrintExpr(var.Args[0], EP.Assign.RightContext(ContinueExpr));
				}
			}
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
		private bool PrintAttrs(Precedence context, AttrStyle style)
		{
			if (DropNonDeclarationAttributes && style != AttrStyle.IsDefinition)
				return false;

			int div = _n.AttrCount, attrCount = div;
			if (div == 0)
				return false;

			bool any = false;

			bool beginningOfExpr = context.RangeEquals(StartExpr) || context.RangeEquals(StartStmt);
			if (beginningOfExpr) {
				if (style != AttrStyle.NoKeywordAttrs) {
					// Choose how much of the attributes to treat as simple words, 
					// e.g. we prefer to print [Flags, #public] as "[Flags] public"
					bool isVarDecl = _n.Name == S.Var;
					for (; div > 0; div--)
						if (!IsWordAttribute(_n.TryGetAttr(div-1), style))
							break;
				}
			} else {
				// When an attribute appears mid-expression (which the printer 
				// tries hard to avoid through the use of prefix notation), we
				// need to write "##(" in order to group the attribute(s) with the
				// expression to which they apply, e.g. while "[A] x + y" has an
				// attribute attached to #+, the attribute in "##([A] x) + y" is
				// attached to x. The "##" tells the parser to discard the 
				// parenthesis instead of encoding them into the Loyc tree. Of 
				// course, only print "##(" if there are attributes to print.
				if (!HasPAttrs(_n))
					return false;
				_out.Write("##(", true);
			}
			
			if (div > 0)
			{
				for (int i = 0; i < div; i++) {
					var a = _n.TryGetAttr(i);
					if (!a.IsPrintableAttr())
						continue;
					if (any)
						WriteThenSpace(',', SpaceOpt.AfterComma);
					else
						WriteThenSpace('[', SpaceOpt.InsideAttribute);
					any = true;
					PrintExpr(a, StartExpr);
				}
				if (any) {
					Space(SpaceOpt.InsideAttribute);
					_out.Write(']', true);
					Space(SpaceOpt.AfterAttribute);
				}
			}
			for (int i = div; i < attrCount; i++)
			{
				var a = _n.TryGetAttr(i);
				string text;
				if (AttributeKeywords.TryGetValue(a.Name, out text)) {
					_out.Write(text, true);
					any = true;
				} else {
					Debug.Assert(a.IsKeyword);
					if (a.IsPrintableAttr()) {
						PrintIdent(GSymbol.Get(a.Name.Name.Substring(1)), false);
						any = true;
					}
				}
			}
			return !beginningOfExpr;
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

		private void PrintIdent(Symbol name, bool inSymbol = false)
		{
 			if (name.Name == "") {
				Debug.Assert(false);
				return;
			}
			
			// Find out if the symbol is a valid identifier
			char first = name.Name[0];
			bool isNormal = true;
			if (first == '#' && !inSymbol) {
				string text;
				if (TypeContext && TypeKeywords.TryGetValue(name, out text)) {
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
					_out.Write("@#", false);
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
				np.PrintIdent((Symbol)np._n.Value, true);
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
		TypeParamDefinition = 64, // allows in out *: e.g. Foo<in A, out B, *c>
		InOf = 64,
	}

	[Flags] public enum SpaceOpt
	{
		Default = AfterComma | AfterCommaInOf | AroundInfix | AfterCast | AfterAttribute | BeforeKeywordStmtArgs | BeforePossibleMacroArgs 
		        | NewLineForSpaceDefBrace | NewLineForMethodBrace | NewLineForPropBrace | BeforeNewInitBrace | InsideBraceInExpr,
		AroundInfix             = 0x0001, // Spaces around infix operators (by default, except . :: ::: ->; see SpaceAroundInfixMaxPrecedence
		AfterComma              = 0x0002, // Spaces after normal commas (and ';' in for loop)
		AfterCommaInOf          = 0x0004, // Spaces after commas between type arguments
		AfterCast               = 0x0008, // Space after cast target: (Foo) x
		AfterCastArrow          = 0x0010, // Space after arrow in new-style cast: x(-> Foo)
		AfterAttribute          = 0x0020, // Space after attribute [Foo] void f();
		AfterPrefix             = 0x0040, // Spaces after prefix operators: - x
		InsideParens            = 0x0080, // Spaces inside non-call parenthesis: ( x )
		InsideCallParens        = 0x0100, // Spaces inside call parenthesis and indexers: foo( x ) foo[ x ]
		InsideAttribute         = 0x0200, // Space inside attribute list: [ Foo ] void f();
		OutsideParens           = 0x0400, // Spaces outside non-call parenthesis: x+ (x) +y
		BeforeKeywordStmtArgs   = 0x0800, // Space before arguments of keyword statement: while (true)
		BeforePossibleMacroArgs = 0x1000, // Space before argument list of possible macro: foo (x)
		BeforeMethodCall        = 0x2000, // Space before argument list of all method calls
		BeforeNewCastCall       = 0x4000, // Space before target of new-style cast: x (->Foo)
		BeforeNewInitBrace      = 0x8000, // Space before opening brace in new-expr: new int[] {...}
		InsideBraceInExpr       = 0x010000, // Spaces inside braces inside expressions: new[] { x }
		
		NewLineForSpaceDefBrace = 0x080000, // Newline before opening brace of type definition
		NewLineForMethodBrace   = 0x100000, // Newline before opening brace of method body
		NewLineForPropBrace     = 0x200000, // Newline before opening brace of property definition
		NewLineForGetSetBrace   = 0x400000, // Newline before opening brace of property getter/setter
		NewLineForBlockInExpr   = 0x800000, // Newline before opening brace inside an expression, including x=>{...}
	}
}
