using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Math;
using Loyc.Utilities;

namespace ecs
{
	/// <summary>Represents the precedence and miscibility of an operator.</summary>
	/// <remarks>
	/// Certain operators should not be mixed because their precedence was originally 
	/// chosen incorrectly, e.g. x & 3 == 1 should be parsed (x & 3) == 1 but is 
	/// actually parsed x & (3 == 1). To allow the precedence to be repaired 
	/// eventually, expressions like x & y == z are deprecated: the parser will 
	/// warn you if you have mixed operators improperly. PrecedenceRange describes 
	/// both precedence and miscibility (mixability) with a simple range of integers.
	/// <para/>
	/// This class contains four numbers. The first two, Lo and Hi, are a 
	/// precedence range that describes how the operator can be mixed with other 
	/// operators. If one operator's range overlaps another AND the ranges are not 
	/// equal, then the two operators are immiscible. For example, == and != have 
	/// the same precedence, 38..39, so they can be mixed with each other, but they 
	/// cannot be mixed with & which has the overlapping range 32..43.
	/// <para/>
	/// The "actual" precedence is encoded in the other two numbers, 
	/// <see cref="Left"/> and <see cref="Right"/>. These numbers encode the 
	/// knowledge that although deprecated, <c>x & y == z</c> will be parsed as 
	/// <c>x & (y == z)</c>. Normally, Left and Right are the same. However, some
	/// operators have different precedence on the left than on the right, a prime
	/// example being the => operator: <c>x = a => y = a</c> is parsed 
	/// <c>x = (a => (y = a))</c>; it has very high precedence on the left, but
	/// very low precedence on the right.
	/// <para/>
	/// To understand how this works, remember that a parser scans from left to 
	/// right. Each time it encounters a new operator, it needs to figure out 
	/// whether to include that operator in the current (inner) expression or 
	/// whether to "complete" the inner expression and bind the operator to an
	/// outer expression instead. The concept of a "precedence floor" can be used 
	/// to make this decision.
	/// <para/>
	/// For example, suppose we start parsing the expression
	/// <c>-a.b + c * d + e</c>. The parser sees "-" first, which must be a prefix 
	/// operator since there is no expression on the left. The <see cref="Right"/>
	/// precedence of unary '-' is 90 in EC#, so that will be the "precedence 
	/// floor" to parse the right-hand side. Operators above 90 will be permitted 
	/// in the right-hand side; operators at 90 or below will not.
	/// <para/>
	/// The next token is 'a', which is an expression by itself and doesn't have 
	/// any precedence, so it becomes the initial right-hand expression of '-'.
	/// Next we have '.', which has a <see cref="Left"/> precedence of 100, which 
	/// is above the precedence floor of 90 so it can be bound to 'a'. The 
	/// precedence floor (PF) is raised to 100, and the next token 'b' is bound to 
	/// '.'.
	/// <para/>
	/// However, the next token '+' (which must be the binary operator rather than 
	/// the prefix operator, because there is an expression on the left) cannot be 
	/// accepted with its precedence of 60. Therefore the expression "a.b" is 
	/// deemed complete, and the PF is lowered back to 90. Again 60 is less than 
	/// 90, so the expression "-a.b" is also deemed complete and the PF drops to 
	/// int.MinValue. This expression becomes the left-hand side of the '+' 
	/// operator. The PF rises to 60, and "c * d" becomes a subexpression because
	/// the precedence of '*' is 70 > 60. However, next we have '+' with 
	/// precedence 60, which is not above the PF of 60. Therefore, the 
	/// subexpression "c * d" is deemed complete and the PF lowers to int.MinValue
	/// again. Now the '+' can be accepted with a left-hand side of <c>(-(a.b)) + 
	/// (c * d)</c>, and the right-hand side is, of course, 'e', so the completed
	/// expression is <c>((-(a.b)) + (c * d)) + e</c>. Hope that helps!
	/// <para/>
	/// Notice that <c>a + b + c</c> is parsed <c>(a + b) + c</c>, not 
	/// <c>a + (b + c)</c>. This is the natural result when the operator's 
	/// precedence is the same on the left and on the right. However, <c>a = b = c</c>
	/// is parsed <c>a = (b = c)</c>, because its precedence is 1 on the left and 
	/// 0 on the right. When the parser sees the first '=' it sets the PF to 0 
	/// because it is about to parse the right side. When it encounters the second 
	/// '=', the left-hand precedence of that operator is 1 which is higher than 
	/// the current PF (0) so it is included in the right-hand side of the first 
	/// '='. This behavior is called "right associativity"; <see cref="IsRightAssociative"/> 
	/// returns true when <c>Left > Right</c>.
	/// <para/>
	/// Prefix and suffix operators only have one "side"; you can imagine that the 
	/// unused side (e.g. the left side of prefix -) has infinite precedence, so 
	/// that EC# can parse \-x as \(-x) even though the precedence of '-' is 
	/// supposedly lower than '\'.
	/// <para/>
	/// The conditional operator (a?b:c) has three parts. In the middle part, the 
	/// PF must drop to Precedence.MinValue so that it is possible to parse 
	/// <c>a?b=x:c</c> even though '=' supposedly has lower precedence than the 
	/// conditional operator. Note that <c>a=b ? c=d : e=f</c> is interpreted
	/// <c>a=(b ? c=d : e)=f</c>, so you can see that the precedence of the 
	/// conditional operator is higher at the "edges".
	/// <para/>
	/// The above explanation illustrates the meaning of Left and Right from the
	/// perspective of a parser, but the actual EC# parser may or may not use the 
	/// PF concept and PrecedenceRange objects.
	/// <para/>
	/// The printer (<see cref="EcsNodePrinter"/>) has a different way of analyzing
	/// precedence. It starts with a known parse tree and then has to figure out 
	/// how to output something that the parser will reconstruct into the original
	/// tree. Making this more difficult is the fact that in Loyc trees, parens are
	/// significant; therefore the printer cannot simply put expressions in parens
	/// "just to be safe"--extra parenthesis will change the syntax tree, so round-
	/// tripping will fail.
	/// <para/>
	/// Generally, the printer has two ways of printing any expression tree: (1) 
	/// with operators (e.g. a+b), and (2) with prefix notation (e.g. #+(a, b)).
	/// The tree <c>#+(#*(a, b), c)</c> will be printed as "a*b+c" (unless prefix
	/// notation is specifically requested) because the precedence rules allow it,
	/// but <c>#*(#+(a, b), c)</c> will be printed as <c>#+(a, b)*c</c> because 
	/// both "a+b*c" and "(a+b)*c" are different from the original tree.
	/// <para/>
	/// While a parser proceeds from left to right, a printer proceeds from parents
	/// to children. So the printer for #*(#+(a, b), c) starts at #* with no 
	/// precedence restrictions, and roughly speaking will set the precedence floor
	/// to <see cref="EcsPrecedence"/>.Multiply in order to print its two children.
	/// Since the precedence of #+ (Add) is below Multiply, the + operator is not
	/// allowed in that context and prefix notation is used as a fallback (unless
	/// you set the <see cref="EcsNodePrinter.AllowExtraParenthesis"/> option to
	/// permit <c>(a+b)*c</c>.
	/// <para/>
	/// Printing has numerous "gotchas"; the ones related to precedence are
	/// <ol>
	/// <li>Although <see cref="EcsPrecedence"/>.Add has the "same" precedence on the
	///     Left and Right, <c>#-(#-(a, b), c)</c> can be printed <c>a - b - c</c> but
	///     <c>#-(a, #-(b, c))</c> would have to be printed <c>a - #-(b, c)</c> 
	///     instead. Clearly, the left and right sides must be treated somehow
	///     differently.</li>
	/// <li>Similarly, the different arguments in <c>a?b:c</c> and <c>a=>b</c> must
	///     be treated differently. And careful handling is needed for the dot 
	///     operator in particular due to its high precedence; e.g. <c>#.(a(b))</c> 
	///     cannot be printed <c>.a(b)</c> because that would mean <c>#.(a)(b)</c>.</li>
	/// <li>The EC# parser, at least, allows a prefix operator to appear on the 
	///     right-hand side of any infix or prefix operator, regardless of the 
	///     precedence of the two operators. \++x is permitted even though ++ has
	///     lower precedence than \. Another example is that <c>a.-b.c</c> can be 
	///     parsed with the interpretation <c>a.(-b).c</c>, even though #- has 
	///     lower precedence than #\. Ideally the printer would replicate this 
	///     rule, but whether it does ot not, it also must take care that 
	///     <c>#.(a, -b.c)</c> is not printed as <c>a.-b.c</c> even though the 
	///     similar expression <c>#*(a, #-(b.c))</c> can be printed as <c>a*-b.c</c>.</li>
	/// <li>Prefix notation is needed when an operator's arguments have attributes;
	///     <c>#+([Foo] a, b)</c> cannot be printed <c>[Foo] a + b</c> because
	///     that would mean <c>[Foo] #+(a, b)</c>.</li>
	/// </ol>
	/// </remarks>
	public struct Precedence : IEquatable<Precedence>
	{
		public Precedence(int lo, int hi, int actual) : this(lo, hi, actual, actual) { }
		public Precedence(int lo, int hi, int left, int right)
		{
			Debug.Assert(MathEx.IsInRange(left, lo, hi) || MathEx.IsInRange(right, lo, hi));
			Lo = checked((sbyte)lo); Hi = checked((sbyte)hi); 
			Left = checked((sbyte)left); Right = checked((sbyte)right);
		}
		public Precedence(sbyte lo, sbyte hi, sbyte left, sbyte right)
		{
			Lo = lo; Hi = hi; Left = left; Right = right;
		}
		/// <summary>Lo and Hi specify the miscibility of an operator; see the 
		/// remarks of <see cref="Precedence"/> for details.</summary>
		public readonly sbyte Lo, Hi;
		/// <summary>Left and Right denote the precedence level on the left and 
		/// right sides of an operator; see the remarks of <see cref="Precedence"/> 
		/// for details.</summary>
		public readonly sbyte Left, Right;

		/// <summary>For use in parsers</summary>
		public Precedence LeftPrecedence { get { return new Precedence(Lo, Hi, Left); } }
		public Precedence RightPrecedence { get { return new Precedence(Lo, Hi, Right); } }

		/// <summary>For use in printers. Auto-raises the precedence floor to 
		/// prepare to print an expression on the left side of an operator.</summary>
		/// <param name="oldContext"></param>
		/// <returns></returns>
		public Precedence LeftContext(Precedence outerContext) {
			return new Precedence(this.Lo, this.Hi, outerContext.Left, this.Left);
		}
		/// <summary>For use in printers. Auto-raises the precedence floor to 
		/// prepare to print an expression on the right side of an operator.</summary>
		/// <param name="oldContext"></param>
		/// <returns></returns>
		public Precedence RightContext(Precedence outerContext) {
			return new Precedence(this.Lo, this.Hi, this.Right, outerContext.Right);
		}

		/// <summary>Returns true if this object represents a right-associative 
		/// operator such as equals (x = (y = z)), in contrast to left-
		/// associative operators such as division ((x / y) / z).</summary>
		public bool IsRightAssociative { get { return Left > Right; } }
		
		/// <summary>For use in printers. Returns true if an infix operator 
		/// with this precedence can appear in the specified context.</summary>
		/// <remarks>Miscibility must be checked separately (<see cref="ShouldAppearIn"/>).</remarks>
		public bool CanAppearIn(Precedence context) {
			return context.Left < Left && Right >= context.Right;
		}
		/// <summary>Returns true if a prefix operator with this precedence can 
		/// appear in the specified context's right-hand precedence floor.</summary>
		/// <remarks>It is assumed that the left side of a prefix operator has 
		/// "infinite" precedence so only the right side is checked. This rule is 
		/// used by the EC# printer but may not be needed or allowed in all 
		/// languages (if in doubt, use <see cref="InfixCanAppearIn"/> instead).</remarks>
		public bool PrefixCanAppearIn(Precedence context) {
			return Right >= context.Right;
		}

		/// <summary>Returns true if an operator with this precedence is miscible
		/// without parenthesis within the specified parent operator.</summary>
		/// <remarks>CanAppearIn is for parsing, ShouldAppearIn is for validation.</remarks>
		public bool ShouldAppearIn(Precedence context) { return this > context || RangeEquals(context); }
		
		public bool RangeEquals(Precedence b) { return Lo == b.Lo && Hi == b.Hi; }

		public static bool operator ==(Precedence a, Precedence b)
		{
			return a.RangeEquals(b) && a.Left == b.Left && a.Right == b.Right;
		}
		public static bool operator !=(Precedence a, Precedence b) { return !(a == b); }
		public static bool operator > (Precedence a, Precedence b) { return a.Lo > b.Hi; }
		public static bool operator < (Precedence a, Precedence b) { return a.Hi < b.Lo; }
		public override bool Equals(object obj) { return (obj is Precedence) && this == ((Precedence)obj); }
		public bool Equals(Precedence other) { return this == other; }
		public override int  GetHashCode() { return Lo ^ (Hi << 4); }
		public static readonly Precedence MinValue = new Precedence(sbyte.MinValue, sbyte.MinValue, sbyte.MinValue);
		public static readonly Precedence MaxValue = new Precedence(sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue);
	}

	/// <summary>Contains <see cref="Precedence"/> objects that represent the 
	/// precedence rules of EC#.</summary>
	/// <remarks>
	/// Summary:
	/// <br/>100+: Primary: x.y x::y f(x) a[i] etc.
	/// <br/>90+: Prefix: +  -  !  ~  ++x  --x  (T)x 
	/// <br/>80+: Power: x**y
	/// <br/>70+: Mult: * / %
	/// <br/>60+: Add: + -     (Shift is 56 but ideally would be 70)
	/// <br/>50+: Range: ..    (`custom operators` are 28 to 55)
	/// <br/>40+: Compare: < > <= >= is as using == !=
	/// <br/>30+: Bitwise &^|  (Ideally would be 54..59)
	/// <br/>20+: Conditional && || ^^
	/// <br/>10+: Ternary
	/// <br/> 1:  Assignment
	/// <para/>
	/// When printing an expression, we avoid emitting <c>x & y == z</c> because 
	/// the ranges of == and & overlap. Instead <see cref="EcsNodePrinter"/> prints 
	/// <c>#&(x, y == z)</c>. Admittedly this is rather ugly, so TODO: add an 
	/// option that allows parenthesis to be added so that a Loyc tree with the 
	/// structure <c>#&(x, y == z)</c> is emitted as <c>x & (y == z)</c>, even 
	/// though the latter is a slightly different tree.
	/// <para/>
	/// Most of the operators use a range of two adjacent numbers, e.g. 10..11. 
	/// This represents a couple of ideas for future use in a compiler that allows
	/// you to define new operators; one idea is, you could give new operators the
	/// "same" precedence as existing operators, but make them immiscible with 
	/// those operators... yet still make them miscible with another new operator.
	/// For instance, suppose you define two new operators `glob` and `fup` with
	/// PrecedenceRange 41..41 and 40..40 respectively. Then neither can be mixed
	/// with + and -, but they can be mixed with each other and `fup` has higher
	/// precedence. Maybe this is not very useful, but hey, why not? If simply
	/// incrementing a number opens up new extensibility features, I'm happy to
	/// do it. (A non-numeric partial ordering system could do the same thing but 
	/// would be more complex.)
	/// </remarks>
	public static class EcsPrecedence
	{
		public static readonly Precedence TightAttr  = Precedence.MaxValue;
		public static readonly Precedence Substitute = new Precedence(102,103,102);    // \x  .x
		public static readonly Precedence Primary    = new Precedence(100,101,100);    // x.y x::y x:::y x->y f(x) x(->y) a[x] x++ x-- typeof() checked() unchecked() new
		public static readonly Precedence NullDot    = new Precedence(98,  99, 99);    // ??.
		public static readonly Precedence Prefix     = new Precedence(90,  91, 90);    // +  -  !  ~  ++x  --x  (T)x
		public static readonly Precedence Forward    = new Precedence(88,  89, 88);    // ==>x
		public static readonly Precedence Power      = new Precedence(80,  81, 80);    // **
		public static readonly Precedence Multiply   = new Precedence(70,  71, 70);    // *, /, %
		public static readonly Precedence Add        = new Precedence(60,  61, 60);    // +, -, ~
		public static readonly Precedence Shift      = new Precedence(56,  70, 56);    // >> << (for printing purposes, immiscible with * / + -)
		public static readonly Precedence Range      = new Precedence(50,  51, 50);    // ..
		public static readonly Precedence Backtick     = new Precedence(45,  73, 46,72); // `custom operator` (immiscible with * / + - << >> ..)
		public static readonly Precedence Compare    = new Precedence(40,  41, 40);    // < > <= >= is as using
		public static readonly new Precedence Equals = new Precedence(38,  39, 38);    // == !=
		public static readonly Precedence AndBits    = new Precedence(32,  45, 32);    // &   (^ and | should not be mixed with Compare/Equals 
		public static readonly Precedence XorBits    = new Precedence(30,  31, 30);    // ^    either, but the low-high system cannot express this
		public static readonly Precedence OrBits     = new Precedence(28,  29, 28);    // |    while allowing & ^ | to be mixed with each other.)
		public static readonly Precedence And        = new Precedence(22,  23, 22);    // &&
		public static readonly Precedence Xor        = new Precedence(20,  21, 20);    // ^^
		public static readonly Precedence Or         = new Precedence(18,  19, 18);    // ||
		public static readonly Precedence OrIfNull   = new Precedence(16,  17, 16);    // ??
		public static readonly Precedence IfElse     = new Precedence(10,  11, 11,10); // x ? y : z
		public static readonly Precedence Assign     = new Precedence( 0,   1, 1,0);   // =  *=  /=  %=  +=  -=  <<=  >>=  &=  ^=  |= ??= ~=
		public static readonly Precedence Lambda     = new Precedence(-2,  -1, 85,-1); // =>
	}
}
