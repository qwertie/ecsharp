using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Syntax.Les
{
	/// <summary>Contains <see cref="Precedence"/> objects that represent the 
	/// precedence levels of LES.</summary>
	/// <remarks>
	/// In LES, the precedence of an operator is decided based simply on the text 
	/// of the operator. The precedence of each one-character operator is 
	/// predefined; the precedence of any operator with two or more characters 
	/// is decided based on the first and last characters (according to
	/// the rules below). Other characters, if any, do not affect precedence.
	/// <para/>
	/// The LES precedence table mostly matches popular programming languages,
	/// i.e. those in the C family.
	/// <para/>
	/// An operator consists of a sequence of the following characters:
	/// <pre>
	///    ~ ! % ^ * - + = | &lt; > / ? : . &amp;
	/// </pre>
	/// In addition, the $ character is allowed as the first character and, if
	/// present, it forces the operator to be interpreted as a prefix operator.
	/// <para/>
	/// LESv3 also has operators that start with a single quote, which can include
	/// both letters and punctuation (e.g. <c>'|foo|</c>). The quote itself is 
	/// ignored for the purpose of choosing precedence. LESv2 has <c>`backquoted`</c> 
	/// operators instead, whereas in LESv3 backquoted strings are simply 
	/// identifiers. 
	/// <para/>
	/// It is notable that the following punctuation cannot be used in operators:
	/// <ul>
	/// <li>"@" is used for multiple other purposes.</li>
	/// <li>"#" is conventionally used to mark "keywords" (although in LESv2, the 
	/// parser treats it like an underscore or a letter.)</li>
	/// <li>"," and ";" are separators, so for example, "?,!" is parsed as three 
	/// separate tokens.</li>
	/// <li>The backslash "\" is reserved for future use.</li>
	/// </ul>
	/// <para/>
	/// The following table shows all the precedence levels and associativities
	/// of the "built-in" LES operators, except a couple of special operators such
	/// as the "lambda" operator =>, whose precedence is different on the left side 
	/// than on the right side. Each precedence level has a name, which corresponds 
	/// to a static field of this class. All binary operators are left-associative 
	/// unless otherwise specified.
	/// <ol>
	/// <li>Substitute: prefix $ . : (note: prefix-dot is not allowed in LES3)</li>
	/// <li>Primary: binary . =:, generic arguments List!(int), suffix ++ --, method calls f(x), indexers a[i]</li>
	/// <li>NullDot: binary ?. :: (in LESv2, :: is NullDot, in LESv3 it's Primary)</li>
	/// <li>DoubleBang: binary right-associative !!</li>
	/// <li>Prefix: prefix ~ ! % ^ * / - + &amp; `backtick` (LESv2 only)</li>
	/// <li>Power: binary **</li>
	/// <li>Multiply: binary * / % \ >> &lt;&lt;</li>
	/// <li>Add: binary + -</li>
	/// <li>Arrow: binary right-associative -> &lt;-</li>
	/// <li>PrefixDots: prefix ..</li>
	/// <li>Range: binary right-associative ..</li>
	/// <li>Compare: binary != == >= > &lt; &lt;=</li>
	/// <li>And: binary &amp;&amp;</li>
	/// <li>Or: binary || ^^</li>
	/// <li>IfElse: binary right-associative ? :</li>
	/// <li>LowerKeyword: a lowercase keyword</li>
	/// <li>PrefixOr: |</li>
	/// </ol>
	/// Not listed in table: binary <c>=> ~ = ?? >> ^ | &amp; &lt;&lt; </c>; prefix <c>? = > &lt;</c>;
	/// non-lowercase keywords.
	/// <para/>
	/// Notice that the precedence of an operator depends on how it is used. The 
	/// prefix operator <c>-</c> has higher precedence than the binary operator 
	/// <c>-</c>, so for example <c>- y * z</c> is parsed as <c>(- y) * z</c>, 
	/// while <c>x - y * z</c> is parsed as <c>x - (y * z)</c>.
	/// <para/>
	/// Programmers often use the shift operators <c>>></c> and <c>&lt;&lt;</c> 
	/// in place of multiplication or division, so their <i>natural</i> precedence
	/// is the same as <c>*</c> and <c>/</c>. However, traditionally the C
	/// family of languages confusingly give the shift operators a precedence 
	/// below <c>+</c>. Therefore, LES does not allow mixing of shift operators
	/// with <c>+ - * /</c>; <c>a >> b + c</c> should produce a parse error.
	/// This is called immiscibility as explained in the documentation of 
	/// <see cref="Precedence"/>. Parsing may still complete, but the exact 
	/// output tree is unspecified (may be <c>(a >> b) + c</c> or 
	/// <c>a >> (b + c)</c>).
	/// <para/>
	/// Likewise, the bitwise <c>^ | &amp;</c> operators cannot be mixed with
	/// comparison operators as in <c>a | 1 == 3</c>.
	/// <para/>
	/// The Lambda operator =>, which is right-associative, has a precedence 
	/// level above Multiply on the left side, but below Assign on the right 
	/// side. For example, the expression <c>a = b => c = d</c> is parsed as 
	/// <c>a = (b => (c = d))</c>, and similarly <c>a + b => c + d</c> is parsed 
	/// as <c>a + (b => (c + d))</c>, but <c>a ** b => c ** d</c> is parsed
	/// <c>(a ** b) => (c ** d)</c>. The idea of two different precedences on the
	/// two sides of an operator may seem strange; see the documentation of 
	/// <see cref="Precedence"/> for more explanation.
	/// <para/>
	/// Similarly, all assignment operators (including compound assignments like
	/// +=) have a high precedence on the left side and low precedence on the 
	/// right. This decision was made for WebAssembly, in which an expression like
	/// <c>2 * i32_store[$f(),4] = 3 * $g()</c> is best parsed as 
	/// <c>2 * (i32_store[$f(),4] = (3 * $g()))</c> (NOTE: this example will 
	/// surely be wrong by the time Wasm is released).
	/// <para/>
	/// As a nod to functional languages, the arrow operator "->" is right-
	/// associative and has a precedence below '*' so that <c>int * int -> int</c>
	/// parses as <c>(int * int) -> int</c> rather than <c>int * (int -> int)</c> 
	/// as in the C family of languages.
	/// <para/>
	/// Some operators like <c>'this-one</c> do not begin with punctuation. These
	/// "keyword operators" must be used as binary operators. They either start
	/// with a lowercase letter or they don't. If they do start with a lowercase
	/// letter, their precedence is LowerKeyword, which is very low, below 
	/// assignment, so that <c>a = b 'then x = y</c> parses like 
	/// <c>(a = b) 'then (x = y)</c>.
	/// <para/>
	/// If they do not start with a lowercase letter (as in <c>'Foo</c> or 
	/// <c>'123</c>) then they have an indeterminate precedence, below power
	/// (**) but above comparison (==). This means that an operator like 'XOR
	/// or 'Mod cannot be mixed with operators of precedence Multiply, Add, 
	/// Arrow, AndBits, OrBits, OrIfNull, PrefixDots, and Range operators.
	/// Mixing operators illegally (e.g. <c>x 'Mod y + z</c>) will produce a 
	/// parse error.
	/// <para/>
	/// After constructing an initial table based on common operators from other
	/// languages, I noticed that 
	/// <ul>
	/// <li>None of the high-precedence operators were right-associative, so I 
	/// added the !! operator to "fill in the gap".</li>
	/// <li>There were no prefix operators with low precedence, so I added ".." 
	/// whose precedence is just above binary "..", and "|" which has a precedence 
	/// lower than anything except attributes (this "operator" is inspired by
	/// Nemerle, which uses "|" in pattern matching and variants.)</li>
	/// </ul>
	/// I also wanted to have a little "room to grow"--to defer the precedence 
	/// decision to a future time for some operators. So the precedence of the 
	/// binary operator ~ has a range of operators with which it cannot be
	/// mixed, the same range as for uppercase operators without punctuation;
	/// for example, <c>x ~ y + z</c> is invalid but <c>x ~ y == z</c> is allowed.
	/// <para/>
	/// The operators <c>\ ? = > &lt;</c> cannot be used as prefix operators.
	/// <para/>
	/// The way that low-precedence prefix operators are parsed deserves some 
	/// discussion... TODO.
	/// <para/>
	/// Most operators can have two roles. Most operators can either be 
	/// binary operators or prefix operators; for example, <c>!*!</c> is a 
	/// binary operator in <c>x !*! y</c> but a prefix operator in <c>x + !*! y</c>.
	/// <para/>
	/// The operators <c>++ --</c> also have two roles, but different roles: 
	/// they can be either prefix or suffix operators, but not binary operators.
	/// For example, <c>-*-</c> is a suffix operator in <c>x -*- + y</c> and a 
	/// prefix operator in <c>x + -*- y</c>. Please note that <c>x -*- y</c> is 
	/// ambiguous (it could be parsed as either of two superexpressions, 
	/// <c>(x -*-) (y)</c> or <c>(x) (-*- y)</c>) and it is illegal.
	/// <para/>
	/// Operators that start with $ can only be prefix operators (not binary or 
	/// suffix). Having only a single role makes these operators unambiguous 
	/// inside superexpressions (LESv2) or with juxtaposition (LESv3).
	/// <para/>
	/// An operator cannot have all three roles (suffix, prefix and binary); that 
	/// would be overly ambiguous. For example, if "-" could also be a suffix 
	/// operator then <c>x - + y</c> could be parsed as <c>(x -) + y</c> as well 
	/// as <c>x - (+ y)</c>. More subtly, LES does not define any operators that
	/// could take binary or suffix roles, because that would also be ambiguous. 
	/// For example, suppose <c>|?|</c> is a binary or suffix operator, but not a 
	/// prefix operator. Clearly <c>x |?| y</c> and <c>x |?| |?| y</c> are 
	/// unambiguous, but <c>x |?| + y</c> is ambiguous: it could be parsed as 
	/// <c>(x |?|) + y</c> or <c>x |?| (+ y)</c>. It turns out that a computer 
	/// language can contain operators that serve as binary and prefix operators, 
	/// OR it can contain operators that serve as binary and suffix operators, 
	/// but a language is ambiguous if it has both kinds of operators at the 
	/// same time.
	///
	/// <h3>How to detect an operator's precedence</h3>
	/// 
	/// To determine the precedence of any given operator, first you must
	/// decide, mainly based on the context in which the operator appears and the
	/// text of the operator, whether it is a prefix, binary, or suffix operator. 
	/// Suffix operators can only be derived from the operators <c>++, --</c>
	/// ("derived" means that you can add additional operator characters in the 
	/// middle, e.g. <c>+++</c> and <c>-%-</c> are can be prefix or suffix 
	/// operators.)
	/// <para/>
	/// If an operator starts with a single quote in LESv3 ('), the quote is not
	/// considered for the purpose of choosing precedence (rather, it is used to 
	/// allow letters and digits in the operator name).
	/// <para/>
	/// Next, if the operator is only one character, simply find it in the list
	/// of operators in the previous section to learn its precedence. If the 
	/// operator is two or more characters, take the first character A and the 
	/// and the last character Z, and of the following rules, use the <b>first</b>
	/// rule that applies:
	/// <ol>
	/// <li>If AZ is "!=" or "==", or if the operator is exactly two characters 
	/// long (ignoring the initial single quote) and equal to ">=", or "&lt;=",
	/// its precedence is Compare. This rule separates comparison operators from 
	/// assignment operators, so that ">>=" is different from ">=", and "===" 
	/// counts as a comparison operator.</li>
	/// <li>If it's an infix operator and Z is '=', the precedence is Assign.</li>
	/// <li>Look for an operator named AZ from the section above. If it is defined,
	/// the operator will have the same precedence. For example, binary <c>=|></c>
	/// has the same precedence as binary "=>".</li>
	/// <li>Otherwise, look for an entry in the table for Z. For example,
	/// binary "%+" has the same precedence as binary "+" and unary "-*" has
	/// the same precedence as unary "*".</li>
	/// <li>If the operator is not an infix operator, it is illegal
	/// (e.g. prefix ?? doesn't exist).</li>
	/// <li>If A is a lowercase letter, the precedence is LowerKeyword.</li>
	/// <li>Otherwise, the operator's precedence is Other.</li>
	/// </ol>
	/// The double-colon :: has the "wrong" precedence according to C# and C++
	/// rules; <c>a.b::c.d</c> is parsed <c>(a.b)::(c.d)</c> although it would 
	/// be parsed <c>((a.b)::c).d</c> in C# and C++. The change in precedence 
	/// allows double colon to be used for variable declarations in LeMP, as 
	/// in <c>x::System.Drawing.Point</c>. The lower precedence allows this
	/// to be parsed properly, but it sacrifices full fidelity with C#/C++.
	/// <para/>
	/// There are no ternary operators in LES. '?' and ':' are right-associative 
	/// binary operators, so <c>c ? a : b</c> is parsed as <c>c ? (a : b)</c>.
	/// The lack of an official ternary operator reduces the complexity of the
	/// parser.
	/// <para/>
	/// LES represents Loyc trees, which do not distinguish operators and 
	/// functions except by name; <c>x += y</c> is equivalent to the function 
	/// call <c>`'+=`(x, y)</c> in LESv3 (<c>@'+=(x, y)</c> in LESv2), and 
	/// the actual name of the function is <c>'+=</c>. Operators that do not
	/// start with a single quote in LES <b>do</b> start with a single quote
	/// in the final output (e.g. <c>2 + 2</c> is equivalent to <c>2 '+ 2</c>).
	/// There is an exception: While prefix ++ and -- are named <c>'++</c> and 
	/// <c>'--</c>, the suffix versions are named <c>'suf++</c> and 
	/// <c>'suf--</c> in the output tree. For LESv2 operators surrounded by 
	/// `backquotes`, the backquotes are not included in the output tree (e.g.
	/// <c>`sqrt` x</c> is equivalent to <c>sqrt(x)</c>).
	/// </remarks>
	/// <seealso cref="Precedence"/>
	public static class LesPrecedence
	{
		// Precedence levels are listed in descending order by precedence.
		//
		// Note: Prefix operators effectively have infinite precedence on the left 
		// side. Increasing the precedence of Prefix.Left to 111 allows the printer 
		// to print things like - -x without a special notation; it has no effect 
		// on the parser.

		public static readonly Precedence Substitute   = new Precedence(111, 110);        // $ : (prefix)
		public static readonly Precedence Of           = new Precedence(106, 105);        // List!T
		public static readonly Precedence Primary      = new Precedence(100);             // . :: x() x[] x++ x--
		public static readonly Precedence NullDot      = new Precedence(95);              // ?. (in LESv2, :: is NullDot, in LESv3 it's Primary)
		public static readonly Precedence Power        = new Precedence(91, 90);          // ** (right-associative as in Python)
		public static readonly Precedence Prefix       = new Precedence(111, 85, 85, 85); // most prefix operators, e.g. - ~ *
		public static readonly Precedence SuffixWord   = new Precedence(80, 111, 80, 80); // LES3 suffix 'word operators and `units`
		public static readonly Precedence RangePrefix  = new Precedence(111, 75, 75, 75); // .. ..< (prefix)
		public static readonly Precedence Range        = new Precedence(75);              // .. ..< ^^
		public static readonly Precedence Multiply     = new Precedence(70);              // * / %
		public static readonly Precedence UpperWord    = new Precedence(65);              // LES3 uppercase WORD_OP or unary 'WORD_OP
		public static readonly Precedence Shift        = new Precedence(65, 65, 60, 65);  // >> <<
		public static readonly Precedence Other        = new Precedence(65, 65, 55, 70);  // LES2 `WORD` or `word` operator
		public static readonly Precedence Add          = new Precedence(60);              // + -
		public static readonly Precedence Squiggly     = new Precedence(55);              // ~ ~> <~
		public static readonly Precedence SquigglyPrefix = new Precedence(111,55,55,55);  // ~> <~ (prefix)
		public static readonly Precedence AndBits      = new Precedence(52, 52, 35, 52);  // &   (LES3 precedence)
		public static readonly Precedence OrBits       = new Precedence(50, 50, 35, 52);  // | ^ (LES3 precedence)
		public static readonly Precedence OrIfNull     = new Precedence(45);              // ??
		public static readonly Precedence Compare      = new Precedence(40);              // == != > < >= <=
		public static readonly Precedence AndBitsLES2  = new Precedence(37, 37, 35, 52);  // &   (LES2 precedence)
		public static readonly Precedence OrBitsLES2   = new Precedence(35, 35, 35, 52);  // | ^ (LES2 precedence)
		public static readonly Precedence Arrow        = new Precedence(31, 30);          // -> <-
		public static readonly Precedence ArrowPrefix  = new Precedence(111, 30, 30, 30); // -> <- (prefix)
		public static readonly Precedence And          = new Precedence(25);              // &&
		public static readonly Precedence Or           = new Precedence(20);              // || ^^
		public static readonly Precedence IfElse       = new Precedence(16, 15);          // ? : :> <:   a 'is (b ? (c 'is d)), a ? (b 'is (c : d))
		public static readonly Precedence ColonArrowPrefix = new Precedence(111, 15);     // :> <: (prefix)
		public static readonly Precedence Assign       = new Precedence(28, 10, 10, 10);  // =      label : (b = (c ? (d : e)))
		public static readonly Precedence LowerKeyword = new Precedence(6, 5, 5, 5);      // keyword, e.g. (a = b) implies (a knows (b = c));
		public static readonly Precedence Lambda       = new Precedence(52, 0, 0, 0);     // =>
		public static readonly Precedence LambdaPrefix = new Precedence(111, 0, 0, 0);    // > => (prefix)
		public static readonly Precedence Triangle     = new Precedence(-5);              // |> <|
		public static readonly Precedence TrianglePrefix = new Precedence(111, -5);       // |> <| (prefix)
		public static readonly Precedence SuperExpr    = new Precedence(-10);             // LES2 only
		public static readonly Precedence Illegal      = new Precedence(58);              // Used to reserve prefix operators for future use
	}
}
