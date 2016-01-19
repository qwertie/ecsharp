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
	/// is decided based on the last character, or the first and last character;
	/// the middle characters, if any, do not affect precedence.
	/// <para/>
	/// The LES precedence table is designed to be comparable with popular
	/// programming languages, with a couple of "corrections" that I felt were 
	/// appropriate; for example, in LES, <c>x ^ 7 != 0</c> is parsed 
	/// <c>(x ^ 7) != 0</c>, rather than <c>x ^ (7 != 0)</c> as in the C family
	/// of languages. Similarly, <c>x >> 1 + y</c> is parsed <c>(x >> 1) + y</c>
	/// rather than <c>x >> (1 + y)</c>. Shifting is often used as a substitute
	/// for multiplication and division, so it has the same precedence.
	/// <para/>
	/// As a nod to functional languages, the arrow operator "->" is right-
	/// associative and has a precedence below '*' so that <c>int * int -> int</c>
	/// parses as <c>(int * int) -> int</c> rather than <c>int * (int -> int)</c> 
	/// as in the C family of languages.
	/// <para/>
	/// An operator consists of a sequence of the following characters:
	/// <pre>
	///    ~ ! % ^ &amp; * \ - + = | &lt; > / ? : . $
	/// </pre>
	/// Or a backslash (\) followed by a sequence of the above characters and/or 
	/// letters, numbers, underscores or #s. Or a string with `backtick quotes`.
	/// <para/>
	/// "@" is not considered an operator. It is used to mark a sequence of 
	/// punctuation and/or non-punctuation characters as an identifier, a symbol,
	/// or a special literal. "#" is not an operator; like an underscore, the 
	/// hash sign is considered to be an identifier character, and while it is 
	/// conventionally used to mark "keywords", the parser does not assign any 
	/// special meaning to it.
	/// <para/>
	/// "," and ";" are not considered operators; rather they are separators, and
	/// they cannot be combined with operators. For example, "?,!" is parsed as 
	/// three separate tokens.
	/// <para/>
	/// The following table shows all the precedence levels and associativities
	/// of the "built-in" LES operators, except `backtick` and the "lambda" 
	/// operator =>, which is special. Each precedence level has a name, which 
	/// corresponds to a static field of this class. All binary operators are 
	/// left-associative unless otherwise specified.
	/// <ol>
	/// <li>Substitute: prefix $ . :</li>
	/// <li>Primary: binary . =:, generic arguments List!(int), suffix ++ --, method calls f(x), indexers a[i]</li>
	/// <li>NullDot: binary ?. ::</li>
	/// <li>DoubleBang: binary right-associative !!</li>
	/// <li>Prefix: prefix ~ ! % ^ &amp; * - + `backtick`</li>
	/// <li>Power: binary **</li>
	/// <li>Suffix2: suffix \\</li>
	/// <li>Multiply: binary * / % \ >> &lt;&lt;</li>
	/// <li>Add: binary + -</li>
	/// <li>Arrow: binary right-associative -> &lt;-</li>
	/// <li>AndBits: binary &amp;</li>
	/// <li>OrBits: binary | ^</li>
	/// <li>OrIfNull: binary ??</li>
	/// <li>PrefixDots: prefix ..</li>
	/// <li>Range: binary right-associative ..</li>
	/// <li>Compare: binary != == >= > &lt; &lt;=</li>
	/// <li>And: binary &amp;&amp;</li>
	/// <li>Or: binary || ^^</li>
	/// <li>IfElse: binary right-associative ? :</li>
	/// <li>Assign: binary right-associative =</li>
	/// <li>PrefixOr: |</li>
	/// </ol>
	/// Not listed in table: binary => ~ &lt;> `backtick`; prefix / \ &lt; > ? =
	/// <para/>
	/// Notice that the precedence of an operator depends on how it is used. The 
	/// prefix operator '-' has higher precedence than the binary operator '-', 
	/// so for example <c>- y * z</c> is parsed as <c>(- y) * z</c>, while 
	/// <c>x - y * z</c> is parsed as <c>x - (y * z)</c>.
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
	/// In addition to these, the binary `backtick` operators have a 
	/// "precedence range" that is above Compare and below Power. This means that 
	/// they are immiscible with the Multiply, Add, Arrow, AndBits, OrBits, 
	/// OrIfNull, PrefixDots, and Range operators, as explained in the 
	/// documentation of <see cref="Precedence"/>. 
	/// <para/>
	/// After constructing an initial table based on common operators from other
	/// languages, I noticed that 
	/// <ul>
	/// <li>All the suffix operators (++ --) had the same precedence, so
	/// I added \...\ as an extra suffix operator with a lower precedence
	/// (but, not seeing a purpose for low-precedence suffixes, it's still
	/// above * and /.)</li>
	/// <li>None of the high-precedence operators were right-associative, so I 
	/// added the !! operator to "fill in the gap".</li>
	/// <li>There were no prefix operators with low precedence, so I added ".." 
	/// whose precedence is just above binary "..", and "|" which has a precedence 
	/// lower than anything except attributes (this "operator" is inspired by
	/// Nemerle, which uses "|" in pattern matching and variants.)</li>
	/// </ul>
	/// I also wanted to have a little "room to grow"--to defer the precedence 
	/// decision to a future time for some operators. So the precedence of the 
	/// binary operators ~ and &lt;> is constrained to be above Compare and below 
	/// NullDot; mixing one of these operators with any operator in this range 
	/// will produce a "soft" parse error (meaning that parsing still proceeds 
	/// but the exact precedence is undefined.)
	/// <para/>
	/// The operators / \ &lt; > ? = can be used as prefix operators, but their
	/// precedence is is similarly undefined (but definitely above Compare and
	/// below NullDot).
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
	/// Operators that end with $ can only be prefix operators (not binary or 
	/// suffix). Operators that start and end with \ can only be suffix (not 
	/// binary or prefix) operators. Having only a single role makes these 
	/// operators unambiguous inside superexpressions.
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
	/// Suffix operators can only be derived from the operators <c>++, --, \\</c>
	/// ("derived" means that you can add additional operator characters in the 
	/// middle, e.g. <c>+++</c> and <c>-%-</c> are can be prefix or suffix 
	/// operators.)
	/// <para/>
	/// If an operator starts with a backslash (\), the backslash is not considered 
	/// part of the operator name and it not used for the purpose of choosing 
	/// precedence either (rather, it is used to allow letters and digits in the 
	/// operator name). A `backquoted` operator always has precedence of 
	/// <see cref="Backtick"/> and again, the backticks are not considered part
	/// of the operator name.
	/// <para/>
	/// Next, if the operator is only one character, simply find it 
	/// in the above table. If the operator is two or more characters, take the 
	/// first character A and the last character Z, and apply the following rules 
	/// in order:
	/// <ol>
	/// <li>If the operator is binary and it is exactly equal to ">=" or "&lt;=" 
	/// or "!=", the precedence is Compare.</li>
	/// <li>If the operator is binary, A is NOT '=', and Z is '=', then the 
	/// precedence is Assign.</li>
	/// <li>Look for an operator named AZ. If it is defined, the operator 
	/// will have the same precedence. For example, binary "=|>" has the same 
	/// precedence as binary "=>".</li>
	/// <li>Otherwise, look for an entry in the table for Z. For example,
	/// binary "%+" has the same precedence as binary "+" and unary "%+" has
	/// the same precedence as unary "+".</li>
	/// <li>If no other rule applies (e.g. \word-operator), use Reserved for
	/// punctuation-based operators with undefined precedence and BackslashWord
	/// for all other operators (which will have started with a backslash).</li>
	/// </ol>
	/// The first two rules are special cases that exist for the sake of the 
	/// shift operators, so that ">>=" has the same precedence as "=" instead 
	/// of ">=".
	/// <para/>
	/// Please note that the plain colon ':' is not treated as an operator at
	/// statement level; it is assumed to introduce a nested block, as in the 
	/// languages Python and boo (e.g. in "if x: y();" is interpreted as 
	/// "if x { y(); }"). However, ':' is allowed as an operator inside a 
	/// parenthesized expression. ([June 2014] Python-style blocks are not
	/// yet implemented.)
	/// <para/>
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
	/// parser; C-style conditional expressions could still be parsed in LEL 
	/// with the help of a macro, but they are generally not necessary since the 
	/// if-else superexpression is preferred: <c>if c a else b</c>.
	/// <para/>
	/// I suppose I should also mention the way operators map to function names.
	/// In LES, there is no semantic distinction between operators and functions;
	/// <c>x += y</c> is equivalent to the function call <c>@+=(x, y)</c>, and 
	/// the actual name of the function is "+=" (the @ character informs the 
	/// lexer that a special identifier name follows.) Thus, the name of most 
	/// operators exactly matches the operator; the + operator is named "+",
	/// the |*| operator is named "|*|", and so forth. There are a couple of 
	/// exceptions:
	/// <ul>
	/// <li>While prefix ++ and -- are named "++" and "--", the suffix versions
	/// are named "suf++" and "suf--" to distinguish them.</li>
	/// <li>The backslash is stripped from operators that start with a backslash.
	/// So \+ is named "+" and \foo\ is named "foo\". \+ means the same thing as 
	/// the normal + operator, it just has a different precedence. However, a 
	/// single backslash (\) followed by whitespace is named "\".</li>
	/// <li>For operators surrounded by `backquotes`, the backquotes are not 
	/// part of the name either; \> and `>` and > differ only in precedence.</li>
	/// </ul>
	/// </remarks>
	/// <seealso cref="Precedence"/>
	public static class LesPrecedence
	{
		public static readonly Precedence Substitute = new Precedence(106, 105); // special prefix ops $ . :
		public static readonly Precedence Primary     = new Precedence(100);
		public static readonly Precedence NullDot     = new Precedence(95);
		public static readonly Precedence DoubleBang  = new Precedence(91, 90);
		public static readonly Precedence Prefix      = new Precedence(85);      // most prefix/suffix ops
		public static readonly Precedence Power       = new Precedence(80);
		public static readonly Precedence Suffix2     = new Precedence(75);      // no longer used
		public static readonly Precedence Multiply    = new Precedence(70);
		public static readonly Precedence Arrow       = new Precedence(65);
		public static readonly Precedence Add         = new Precedence(60);
		public static readonly Precedence Shift       = new Precedence(55, 55, 55, 70);
		public static readonly Precedence PrefixDots  = new Precedence(50);      // prefix ..
		public static readonly Precedence Range       = new Precedence(45);
		public static readonly Precedence OrIfNull    = new Precedence(40, 40, 40, 76);
		public static readonly Precedence Backtick    = new Precedence(40, 40, 40, 75);
		public static readonly Precedence Reserved    = new Precedence(40, 40, 40, 90);
		public static readonly Precedence Compare     = new Precedence(35);
		public static readonly Precedence AndBits     = new Precedence(30, 30, 25, 50);
		public static readonly Precedence OrBits      = new Precedence(25, 25, 25, 50);
		public static readonly Precedence And         = new Precedence(20);
		public static readonly Precedence Or          = new Precedence(15);
		public static readonly Precedence IfElse      = new Precedence(11, 10);
		public static readonly Precedence Assign      = new Precedence(6, 5);
		public static readonly Precedence Lambda      = new Precedence(77, 0, -1, -1);
		public static readonly Precedence PrefixOr    = new Precedence(0);       // prefix
		public static readonly Precedence SuperExpr   = new Precedence(-5);
	}
}
