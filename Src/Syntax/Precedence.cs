using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Math;

namespace Loyc.Syntax
{
	/// <summary>Represents the precedence and miscibility of an operator.</summary>
	/// <remarks>
	/// This class contains four numbers. The first two, Lo and Hi, are a 
	/// precedence range that describes how the operator can be mixed with other 
	/// operators. If one operator's range overlaps another AND the ranges are not 
	/// equal, then the two operators are immiscible. For example, == and != have 
	/// the same precedence in EC#, 38..39, so they can be mixed with each other, 
	/// but they cannot be mixed with & which has the overlapping range 32..45 
	/// (this will be explained below.)
	/// <para/>
	/// The "actual" precedence is encoded in the other two numbers, 
	/// <see cref="Left"/> and <see cref="Right"/>. These numbers encode the 
	/// knowledge that, for example, <c>x & y == z</c> will be parsed as 
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
	/// For example, suppose we start parsing the expression <c>-a.b + c * d + e</c>.
	/// The parser sees "-" first, which must be a prefix operator since there is 
	/// no expression on the left. The <see cref="Right"/> precedence of unary 
	/// '-' is 90 in EC#, so that will be the "precedence floor" to parse the 
	/// right-hand side. Operators above 90 will be permitted in the right-hand 
	/// side; operators at 90 or below will not.
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
	/// that EC# can parse $-x as $(-x) even though the precedence of '-' is 
	/// supposedly lower than '$'.
	/// <para/>
	/// Some languages have a conditional operator (a?b:c) with three parts. In 
	/// the middle part, the PF must drop to Precedence.MinValue so that it is 
	/// possible to parse <c>a?b=x:c</c> even though '=' supposedly has lower 
	/// precedence than the conditional operator. Note that <c>a=b ? c=d : e=f</c> 
	/// is interpreted <c>a=(b ? c=d : e)=f</c>, so you can see that the precedence 
	/// of the conditional operator is higher at the "edges".
	/// <para/>
	/// The above explanation illustrates the meaning of Left and Right from the
	/// perspective of a parser, but an actual parser may or may not use the PF 
	/// concept and PrecedenceRange objects.
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
	/// <li>Although <see cref="LesPrecedence"/>.Add has the "same" precedence on the
	///     Left and Right, <c>#-(#-(a, b), c)</c> can be printed <c>a - b - c</c> but
	///     <c>#-(a, #-(b, c))</c> would have to be printed <c>a - #-(b, c)</c> 
	///     instead. Clearly, the left and right sides must be treated somehow
	///     differently.</li>
	/// <li>Similarly, the different arguments in <c>a?b:c</c> and <c>a=>b</c> must
	///     be treated differently. And careful handling is needed for the dot 
	///     operator in particular due to its high precedence; e.g. <c>#.(a(b))</c> 
	///     cannot be printed <c>.a(b)</c> because that would mean <c>#.(a)(b)</c>.</li>
	/// <li>The LES parser, at least, allows a prefix operator to appear on the 
	///     right-hand side of any infix or prefix operator, regardless of the 
	///     precedence of the two operators; "$ ++x" is permitted even though ++ has
	///     lower precedence than $. Another example is that <c>a.-b.c</c> can be 
	///     parsed with the interpretation <c>a.(-b).c</c>, even though #- has 
	///     lower precedence than #$. Ideally the printer would replicate this 
	///     rule, but whether it does ot not, it also must take care that 
	///     <c>#.(a, -b.c)</c> is not printed as <c>a.-b.c</c> even though the 
	///     similar expression <c>#*(a, #-(b.c))</c> can be printed as <c>a*-b.c</c>.</li>
	/// <li>Prefix notation is needed when an operator's arguments have attributes;
	///     <c>#+([Foo] a, b)</c> cannot be printed <c>[Foo] a + b</c> because
	///     that would mean <c>[Foo] #+(a, b)</c>.</li>
	/// </ol>
	/// 
	/// <h3>Printing and parsing are different</h3>
	/// 
	/// This type contains different methods for printers and parsers. A basic 
	/// difference between them is that printers must make decisions (of whether
	/// an operator is allowed or not in a given context) based on both sides of
	/// the operator and both sides of the context (Left and Right), while parsers
	/// only have to worry about one side. For example, consider the following 
	/// expression:
	/// <code>
	///     a = b + c ?? d
	/// </code>
	/// When the parser encounters the "+" operator, it only has to consider 
	/// whether the precedence of the <i>left-hand side</i> of the "+" operator
	/// is above the <i>right-hand side</i> of the "=" operator. The fact that
	/// there is a "??" later on is irrelevant. In contrast, when printing the 
	/// expression "b + c", both sides of the "+" operator and both sides of the 
	/// context must be considered. The right-hand side is relevant because if 
	/// the right-hand operator was "*" instead of "??", the following printout 
	/// would be wrong:
	/// <code>
	///     a = b + c * d   // actual syntax tree: a = #+(b, c) * d
	/// </code>
	/// The same reasoning applies to the left-hand side (imagine if "=" was 
	/// "*" instead.)
	/// <para/>
	/// So, naturally there are different methods for parsing and printing.
	/// For printing you can use <see cref="CanAppearIn"/>, <see 
	/// cref="LeftContext"/> and <see cref="RightContext"/>, while for parsing you 
	/// only need <see cref="CanParse"/> (to raise the precedence floor, simply
	/// replace the current <see cref="Precedence"/> value with that of the new 
	/// operator). In a parser, the "current" precedence is represented by 
	/// <see cref="Right"/>; the value of <see cref="Left"/> doesn't matter.
	/// <para/>
	/// Both printers and parsers can use <see cref="CanMixWith"/>.
	/// 
	/// <h3>Miscibility (mixability)</h3>
	/// 
	/// Certain operators should not be mixed because their precedence was originally 
	/// chosen incorrectly, e.g. x & 3 == 1 should be parsed (x & 3) == 1 but is 
	/// actually parsed x & (3 == 1). To allow the precedence to be repaired 
	/// eventually, expressions like x & y == z are deprecated in EC#: the parser will 
	/// warn you if you have mixed operators improperly. PrecedenceRange describes 
	/// both precedence and miscibility with a simple range of integers. As mentioned
	/// before, two operators are immiscible if their ranges overlap but are not 
	/// identical.
	/// <para/>
	/// In LES, the precedence range feature (a.k.a. immiscibility) is used to 
	/// indicate that a specific precedence has not been chosen for an operator. 
	/// If a precedence is chosen in the future, it will be somewhere within the 
	/// range.
	/// </remarks>
	public struct Precedence : IEquatable<Precedence>
	{
		public Precedence(int actual) : this(actual, actual, actual, actual) { }
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
		/// <remarks>Miscibility must be checked separately (<see cref="CanMixWith"/>).</remarks>
		public bool CanAppearIn(Precedence context) {
			return context.Left < Left && Right >= context.Right;
		}
		/// <summary>For use in printers. Returns true if a prefix operator with 
		/// this precedence can appear in the specified context's right-hand 
		/// precedence floor.</summary>
		/// <remarks>It is assumed that the left side of a prefix operator has 
		/// "infinite" precedence so only the right side is checked. This rule is 
		/// used by the EC# printer but may not be needed or allowed in all 
		/// languages (if in doubt, use <see cref="CanAppearIn"/> instead).</remarks>
		public bool CanAppearIn(Precedence context, bool prefix) {
			return (prefix || context.Left < Left) && Right >= context.Right;
		}

		/// <summary>Returns true if an operator with this precedence is miscible
		/// without parenthesis with the specified other operator.</summary>
		/// <remarks><see cref="CanAppearIn"/> is for parsability, CanMix
		/// is to detect a deprecated mixing of operators.
		/// </remarks>
		public bool CanMixWith(Precedence context) { return this.Lo > context.Hi || this.Hi < context.Lo || RangeEquals(context); }

		/// <summary>For use in parsers. Returns true if 'rightOp', an operator
		/// on the right, has higher precedence than the current operator 'this'.</summary>
		/// <returns><c>rightOp.Left > this.Right</c></returns>
		public bool CanParse(Precedence rightOp) { return rightOp.Left > this.Right; }

		public bool RangeEquals(Precedence b) { return Lo == b.Lo && Hi == b.Hi; }

		public static bool operator ==(Precedence a, Precedence b)
		{
			return a.RangeEquals(b) && a.Left == b.Left && a.Right == b.Right;
		}
		public static bool operator !=(Precedence a, Precedence b) { return !(a == b); }
		public override bool Equals(object obj) { return (obj is Precedence) && this == ((Precedence)obj); }
		public bool Equals(Precedence other) { return this == other; }
		public override int  GetHashCode() { return Lo ^ (Hi << 4); }
		public static readonly Precedence MinValue = new Precedence(sbyte.MinValue, sbyte.MinValue, sbyte.MinValue);
		public static readonly Precedence MaxValue = new Precedence(sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue);
	}
}
