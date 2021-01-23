using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using S = Loyc.Ecs.EcsCodeSymbols;
using EP = Loyc.Ecs.EcsPrecedence;
using Loyc.Collections;
using Loyc.Collections.Impl;

namespace Loyc.Ecs
{
	/// <summary>A collection of miscellaneous information about the Enhanced C# 
	/// language.</summary>
	/// <seealso cref="EcsPrecedence"/>
	/// <seealso cref="EcsCodeSymbols"/>
	/// <seealso cref="EcsValidators"/>
	public class EcsFacts
	{
		#region Lists of statement types (used mainly by the printer)

		#if !DotNet45
		/// <summary>Returns a list of the Names of all space definition statements, 
		/// namely: #struct #class #interface #namespace #enum #trait #alias.</summary>
		public static IReadOnlyCollection<Symbol> SpaceDefinitionStatements => SpaceDefinitionStmts;
		
		/// <summary>Returns a list of the Names of non-space definition statements
		/// that can be found outside method definitions, namely: 
		/// #var #fn #cons (constructor) #delegate #event #property.</summary>
		public static IReadOnlyCollection<Symbol> OtherDefinitionStatements => OtherDefinitionStmts;
		#endif

		internal static readonly HashSet<Symbol> SpaceDefinitionStmts = new HashSet<Symbol>(new[] {
			S.Struct, S.Class, S.Trait, S.Enum, S.Alias, S.Interface, S.Namespace
		});
		// Definition statements define types, spaces, methods, properties, events and variables
		internal static readonly HashSet<Symbol> OtherDefinitionStmts = new HashSet<Symbol>(new[] {
			S.Var, S.Fn, S.Constructor, S.Delegate, S.Event, S.Property
		});
		// Block statements take block(s) as arguments
		internal static readonly HashSet<Symbol> TwoArgBlockStmts = new HashSet<Symbol>(new[] {
			S.DoWhile, S.Fixed, S.Lock, S.SwitchStmt, S.UsingStmt, S.While
		});
		internal static readonly HashSet<Symbol> OtherBlockStmts = new HashSet<Symbol>(new[] {
			S.If, S.Checked, S.For, S.ForEach, S.If, S.Try, S.Unchecked
		});
		internal static readonly HashSet<Symbol> LabelStmts = new HashSet<Symbol>(new[] {
			S.Label, S.Case
		});
		// Simple statements with the syntax "keyword;" or "keyword expr;" can also be
		// used as expressions, except for `using` (S.Import).
		internal static readonly HashSet<Symbol> SimpleStmts = new HashSet<Symbol>(new[] {
			S.Break, S.Continue, S.Goto, S.GotoCase, S.Return, S.Throw, S.Import
		});

		#endregion

		#region Lists of operators (used mainly by the printer)

		/// <summary>Returns a table of prefix operator symbols. It maps the unary 
		/// prefix operators of EC# to their <see cref="Precedence"/>.</summary>
		/// <remarks>
		/// Does not include the binary prefix operator 'cast or the unary suffix 
		/// operators ++ and --. 
		/// <para/>
		/// The dot `'.` can be a prefix operator in EC# and is included. The `'not`
		/// operator, which can only appear in patterns, is also included.
		/// </remarks>
		public static IReadOnlyDictionary<Symbol, Precedence> PrefixOperatorPrecedenceTable => PrefixOperators;
		
		internal static readonly Dictionary<Symbol, Precedence> PrefixOperators = Dictionary(
			// Printer-related notes:
			//
			// Although @`.` can be a prefix operator, it is not included in this list
			// because it needs special treatment because its precedence is higher
			// than EP.Primary (i.e. above prefix notation). Therefore, it's printed
			// as an identifier if possible (e.g. @`.`(a)(x) is printed ".a(x)") and
			// uses prefix notation if not (e.g. @`.`(a(x)) must be in prefix form.)
			//
			// The substitute operator $ also has higher precedence than Primary, 
			// but its special treatment is in the parser: the parser produces the
			// same tree for $(x) and $x, unlike e.g. ++(x) and ++x which are 
			// different trees. Therefore we can treat $ as a normal operator in
			// the printer except that we must emit parenthesis around the argument
			// if it is anything but a simple identifier (CanAppearIn detects when
			// this is necessary.)
			P(S._Negate, EP.Prefix), P(S._UnaryPlus, EP.Prefix), P(S.NotBits, EP.Prefix),
			P(S.Not, EP.Prefix), P(S.PreInc, EP.Prefix), P(S.PreDec, EP.Prefix),
			P(S._AddressOf, EP.Prefix), P(S._Dereference, EP.Prefix), P(S.Forward, EP.Forward),
			P(S.DotDot, EP.Prefix), P(S.DotDotDot, EP.Prefix),
			P(S.Dot, EP.Substitute), P(S.Substitute, EP.Substitute),
			P(S.LT, EP.Compare), P(S.GT, EP.Compare),
			P(S.LE, EP.Compare), P(S.GE, EP.Compare),
			P(S.PatternNot, EP.PatternNot)
		);

		/// <summary>Returns a table of infix operator symbols. It maps the binary 
		/// infix operators of EC# to their <see cref="Precedence"/>.</summary>
		/// <remarks>This does not include the conditional operator `?` or non-infix 
		/// binary operators such as a[i]. Nor does it include the C# 9 pattern 
		/// operators `'and`, `'or`, `'not`. Comma is not an operator at all in C#.
		/// <para/>
		/// The list does include the lambda operator, as well as `'switch`, `'with`,
		/// `'when`, `'where`; the pattern operators `'and` and `'or`
		/// </remarks>
		public static IReadOnlyDictionary<Symbol, Precedence> InfixOperatorPrecedenceTable => InfixOperators;

		internal static readonly Dictionary<Symbol,Precedence> InfixOperators = Dictionary(
			P(S.Dot, EP.Primary),      P(S.ColonColon, EP.Primary), P(S.QuickBind, EP.Primary), 
			P(S.RightArrow, EP.Primary), P(S.NullDot, EP.NullDot),
			P(S.Exp, EP.Power),        P(S.Mul, EP.Multiply),
			P(S.Div, EP.Multiply),     P(S.Mod, EP.Multiply),
			P(S.Add, EP.Add),          P(S.Sub, EP.Add),        P(S.NotBits, EP.Add),
			P(S.Shl, EP.Shift),        P(S.Shr, EP.Shift),
			P(S.DotDot, EP.Range),     P(S.DotDotDot, EP.Range),
			P(S.LE, EP.Compare),       P(S.GE, EP.Compare),
			P(S.LT, EP.Compare),       P(S.GT, EP.Compare),
			P(S.Is, EP.Is),            P(S.As, EP.AsUsing),       P(S.UsingCast, EP.AsUsing),
			P(S.Eq, EP.Equals),        P(S.NotEq, EP.Equals),     P(S.In, EP.Equals),
			P(S.AndBits, EP.AndBits),  P(S.XorBits, EP.XorBits),  P(S.OrBits, EP.OrBits), 
			P(S.And, EP.And),          P(S.Or, EP.Or),            P(S.Xor, EP.Or),
			P(S.Assign, EP.Assign),    P(S.MulAssign, EP.Assign),      P(S.DivAssign, EP.Assign),
			P(S.ModAssign, EP.Assign),      P(S.SubAssign, EP.Assign), P(S.AddAssign, EP.Assign), 
			P(S.ConcatAssign, EP.Assign),   P(S.ShlAssign, EP.Assign), P(S.ShrAssign, EP.Assign), 
			P(S.ExpAssign, EP.Assign),      P(S.XorBitsAssign, EP.Assign),
			P(S.AndBitsAssign, EP.Assign),  P(S.OrBitsAssign, EP.Assign), 
			P(S.NullCoalesce, EP.OrIfNull), P(S.NullCoalesceAssign, EP.Assign),
			P(S.Compare, EP.Compare3Way),   P(S.ForwardPipeArrow, EP.PipeArrow),
			P(S.ForwardAssign, EP.PipeArrow),
			P(S.NullForwardPipeArrow, EP.PipeArrow),
			P(S.ForwardNullCoalesceAssign, EP.PipeArrow),
			P(S.When, EP.WhenWhere),        P(S.WhereOp, EP.WhenWhere),
			P(S.With, EP.Switch),           P(S.SwitchOp, EP.Switch),
			P(S.Lambda, EP.Lambda),         
			P(S.PatternOr, EP.PatternOr),   P(S.PatternAnd, EP.PatternAnd)
		);

		/// <summary>Returns a list of the three list operators, `'tuple`, `'{}` (braced 
		/// block), and `#arrayInit` (which is only valid as an initializer for an array 
		/// variable).</summary>
		#if !DotNet45
		public static IReadOnlyCollection<Symbol> ListOperators => _listOperators;
		#endif
		internal static readonly HashSet<Symbol> _listOperators = new HashSet<Symbol>(new[] {
			S.Tuple, S.Braces, S.ArrayInit
		});

		static Dictionary<Symbol, Precedence> _otherOperators;
		/// <summary>
		/// Returns a list of "other" operators, i.e. operators that are not prefix,
		/// suffix, binary or list operators. This includes the cast operators
		/// `'cast`, `'as` and `'using`, the generic parameter operator `'of`,
		/// the ternary operator `'?` (as in c?a:b), the index brackets `'[]` and 
		/// its nullable variant, the post-increment and post-decrement operators
		/// `'suf++` and `'suf--`, the `'new` operator, and the call operators
		/// `'typeof`, `'checked`, `'unchecked`, `'default`, and `'sizeof`.
		/// </summary>
		public static IReadOnlyDictionary<Symbol, Precedence> OtherOperators
		{
			get {
				if (_otherOperators == null) {
					var merged = new Dictionary<Symbol, Precedence>(SpecialCaseOperators);
					merged.AddRange(CastOperators);
					merged.AddRange(CallOperators.Select(sym => new KeyValuePair<Symbol, Precedence>(sym, EP.Primary)));
					merged[S.Of] = EP.Of;
					_otherOperators = merged;
				}
				return _otherOperators;
			}
		}

		internal static readonly Dictionary<Symbol, Precedence> CastOperators = Dictionary(
			P(S.Cast, EP.Prefix),       // (Foo)x      (preferred form)
			P(S.As, EP.AsUsing),        // x as Foo    (preferred form)
			P(S.UsingCast, EP.AsUsing)  // x using Foo (preferred form)
		);

		internal static readonly Dictionary<Symbol, Precedence> SpecialCaseOperators = Dictionary(
			// Operators that need special treatment (neither prefix nor infix nor casts)
			// ?  []  suf++  suf--  'of  .  'isLegal  'new
			P(S.QuestionMark, EP.IfElse),  // a?b:c
			P(S.IndexBracks, EP.Primary), // a[]
			P(S.NullIndexBracks, EP.Primary), // a?[] (C# 6 feature)
			P(S.PostInc, EP.Primary), // x++
			P(S.PostDec, EP.Primary), // x--
			P(S.IsLegal, EP.Compare),  // x is legal
			P(S.New,     EP.Substitute)
			//P(S.Lambda,      EP.Substitute) // delegate(int x) { return x+1; }
		);

		internal static readonly HashSet<Symbol> CallOperators = new HashSet<Symbol>(new[] {
			S.Typeof, S.Checked, S.Unchecked, S.Default, S.Sizeof
		});

		internal static readonly HashSet<Symbol> OperatorIdentifiers = new HashSet<Symbol> {
			// >>, << and ** are special: the lexer provides them as two separate tokens

			// Standard C# operators:
			S.NotBits, S.Not, S.Mod, S.XorBits, S.AndBits, S.And, S.Mul, S.Exp, S.Add, S.PreInc,
			S.Sub, S.PreDec, S.Eq, S.NotEq, S.Sub, S.PreDec, S.Eq, S.NotEq, /*"{}", "[]",*/ S.OrBits, S.Or,
			S.Semicolon, S.Colon, S.Comma, S.Dot, S.DotDot, S.LT, S.Shl, S.GT, S.Shr, S.Div,
			S.QuestionMark, S.NullCoalesce, S.NullDot, S.LE, S.GE, S.Lambda, S.RightArrow,
			// Standard C# assignment operators:
			S.Assign, S.MulAssign, S.SubAssign, S.AddAssign, S.DivAssign, S.ModAssign,
			S.ShrAssign, S.ShlAssign, S.XorBitsAssign, S.AndBitsAssign, S.OrBitsAssign,
			S.NullCoalesceAssign,
			// EC#-specific operators, starting with assignment operators
			S.ExpAssign, S.QuickBindAssign, S.ConcatAssign, S.QuickBind, S.UsingCast,
			S.DotDotDot, S.Backslash, S.Forward, S.Substitute, S.Compare,
			S.ForwardPipeArrow, S.ForwardAssign, S.NullForwardPipeArrow,
			S.ForwardNullCoalesceAssign, S.When, S.WhereOp
		};

		#endregion

		#region Keyword lists

		internal static readonly HashSet<Symbol> PreprocessorKeywordNames = SymbolSet(
			"#if", "#else", "#elif", "#endif", "#define", "#undef",
			"#region", "#endregion", "#pragma", "#error", "#warning", "#note", "#line",
			"#nullable", "#r", "#load", "#cls", "#clear", "#reset", "#help"
		);

		internal static readonly HashSet<Symbol> CsKeywords = SymbolSet(
			"abstract", "event", "new", "struct",
			"as", "explicit", "null", "switch",
			"base", "extern", "object", "this",
			"bool", "false", "operator", "throw",
			"break", "finally", "out", "true",
			"byte", "fixed", "override", "try",
			"case", "float", "params", "typeof",
			"catch", "for", "private", "uint",
			"char", "foreach", "protected", "ulong",
			"checked", "goto", "public", "unchecked",
			"class", "if", "readonly", "unsafe",
			"const", "implicit", "ref", "ushort",
			"continue", "in", "return", "using",
			"decimal", "int", "sbyte", "virtual",
			"default", "interface", "sealed", "volatile",
			"delegate", "internal", "short", "void",
			"do", "is", "sizeof", "while",
			"double", "lock", "stackalloc",
			"else", "long", "static",
			"enum", "namespace", "string");

		internal static readonly Dictionary<Symbol, string> AttributeKeywords = KeywordDict(
			"abstract", "const", "explicit", "extern", "implicit", "internal",
			"override", "params", "private", "protected", "public", "readonly", "ref",
			"sealed", "static", "unsafe", "virtual", "volatile", "out");

		internal static readonly Dictionary<Symbol, string> TypeKeywords = Dictionary(
			P(S.Void, "void"), P(S.Object, "object"), P(S.Bool, "bool"), P(S.Char, "char"),
			P(S.Int8, "sbyte"), P(S.UInt8, "byte"), P(S.Int16, "short"), P(S.UInt16, "ushort"),
			P(S.Int32, "int"), P(S.UInt32, "uint"), P(S.Int64, "long"), P(S.UInt64, "ulong"),
			P(S.Single, "float"), P(S.Double, "double"), P(S.String, "string"), P(S.Decimal, "decimal")
		);

		internal static readonly Dictionary<Symbol, string> KeywordStmts = KeywordDict(
			"break", "case", "checked", "continue", "default", "do", "fixed",
			"for", "foreach", "goto", "if", "lock", "return", "switch", "throw", "try",
			"unchecked", "using", "while", "enum", "struct", "class", "interface",
			"namespace", "trait", "alias", "event", "delegate", "goto case");

		internal static readonly HashSet<Symbol> KnownTrivia = new HashSet<Symbol> {
			S.TriviaInParens, S.TriviaTrailing,
			S.TriviaNewline, S.TriviaAppendStatement, S.TriviaSpaces,
			S.TriviaSLComment, S.TriviaMLComment,
			S.TriviaRawText, S.TriviaCsRawText, S.TriviaCsPPRawText,
			S.TriviaUseOperatorKeyword, S.TriviaForwardedProperty,
			S.TriviaRegion, S.TriviaEndRegion,
		};

		#endregion

		#region Helper functions

		internal static HashSet<Symbol> SymbolSet(params string[] input)
		{
			return new HashSet<Symbol>(input.Select(s => GSymbol.Get(s)));
		}
		static Dictionary<Symbol, string> KeywordDict(params string[] input)
		{
			var d = new Dictionary<Symbol, string>(input.Length);
			for (int i = 0; i < input.Length; i++) {
				string name = input[i], text = name;
				if (name == "goto case")
					name = "#gotoCase";
				else
					name = "#" + name;
				d[GSymbol.Get(name)] = text;
			}
			return d;
		}
		static Pair<K, V> P<K, V>(K key, V value)
		{ return Pair.Create(key, value); }
		internal static Dictionary<K, V> Dictionary<K, V>(params Pair<K, V>[] input)
		{
			var d = new Dictionary<K, V>();
			for (int i = 0; i < input.Length; i++)
				d.Add(input[i].Key, input[i].Value);
			return d;
		}

		#endregion
	}
}
