using System;
using System.Collections.Generic;
using Loyc.Essentials;
using Loyc.Utilities;
using System.Collections;
using Loyc.CompilerCore.ExprParsing;

namespace Loyc.CompilerCore.ExprNodes
{
	public class BaseMatchOp<Tok> : AbstractOperator<Tok>, IOneOperator<Tok>
		where Tok : ITokenValueAndPos
	{
		public BaseMatchOp(string name, Symbol type) : base(name, type) {}
		public BaseMatchOp(string name, Symbol type, IOperatorPartMatcher[] tokens) : base(name, type, tokens) { }
		object IOneOperator<Tok>.Generate(OneOperatorMatch<Tok> match) { return Generate(match); }
		public OneOperatorMatch<Tok> Generate(OneOperatorMatch<Tok> match) { return match; }
	}

	public class BinaryMatchOp<Tok> : BaseMatchOp<Tok>
		where Tok : ITokenValueAndPos
	{
		public BinaryMatchOp(string tokenText, int precedence)
			: this(tokenText, precedence, DefaultBinaryOpName(tokenText), DefaultBinaryOpType(tokenText), null) { }
		public BinaryMatchOp(string tokenText, int precedence, string name)
			: this(tokenText, precedence, name, DefaultBinaryOpType(tokenText), null) { }
		public BinaryMatchOp(string tokenText, int precedence, string name, Symbol exprType)
			: this(tokenText, precedence, name, exprType, null) { }
		public BinaryMatchOp(string tokenText, int precedence, string name, Symbol exprType, Symbol tokenType)
			: base(name, exprType, new OneOperatorPart[] {
				new OneOperatorPart(precedence),
				new OneOperatorPart(tokenType, tokenText),
				new OneOperatorPart(precedence),
			}) { }
	}

	/// <summary>For infix ternary operators such as e?e:e.</summary>
	/// <remarks>The TernaryOperator constructors work in a similar way to the 
	/// <see cref="BinaryMatchOp{T}"/> constructors, except that (1) an infix ternary 
	/// operator is generated, of course, so two token strings are required between
	/// the three expressions, and (2) the auto-generated names and types have forms 
	/// such as "ternary ? :" and :e\?e\:e instead of "binary ?" and :e\?e.</remarks>
	public class TernaryMatchOp<Tok> : BaseMatchOp<Tok>
		where Tok : ITokenValueAndPos
	{
		public TernaryMatchOp(string tokenText1, string tokenText2, int precedence)
			: this(tokenText1, tokenText2, precedence, DefaultTernaryOpName(tokenText1, tokenText2), DefaultTernaryOpType(tokenText1, tokenText2)) { }
		public TernaryMatchOp(string tokenText1, string tokenText2, int precedence, string name)
			: this(tokenText1, tokenText2, precedence, name, DefaultTernaryOpType(tokenText1, tokenText2)) { }
		public TernaryMatchOp(string tokenText1, string tokenText2, int precedence, string name, Symbol exprType)
			: this(tokenText1, tokenText2, precedence, name, exprType, null, null) { }
		public TernaryMatchOp(string tokenText1, string tokenText2, int precedence, string name, Symbol exprType, Symbol tokenType1, Symbol tokenType2)
			: base(name, exprType, new OneOperatorPart[] {
				new OneOperatorPart(precedence),
				new OneOperatorPart(tokenType1, tokenText1),
				new OneOperatorPart(Math.Max(precedence, 100)),
				new OneOperatorPart(tokenType2, tokenText2),
				new OneOperatorPart(precedence),
			}) { }
	}

	public abstract class UnaryMatchOp<Tok> : BaseMatchOp<Tok>
		where Tok : ITokenValueAndPos
	{
		public UnaryMatchOp(string name, Symbol type) : base(name, type) { }
		public UnaryMatchOp(string name, Symbol type, IOperatorPartMatcher[] tokens) : base(name, type, tokens) { }
	}

	public class PrefixMatchOp<Tok> : UnaryMatchOp<Tok>
		where Tok : ITokenValueAndPos
	{
		public PrefixMatchOp(string tokenText, int precedence)
			: this(tokenText, precedence, DefaultPrefixOpName(tokenText), DefaultPrefixOpType(tokenText), null) { }
		public PrefixMatchOp(string tokenText, int precedence, string name)
			: this(tokenText, precedence, name, DefaultPrefixOpType(tokenText), null) { }
		public PrefixMatchOp(string tokenText, int precedence, string name, Symbol exprType)
			: this(tokenText, precedence, name, exprType, null) { }
		public PrefixMatchOp(string tokenText, int precedence, string name, Symbol exprType, Symbol tokenType)
			: base(name, exprType, new OneOperatorPart[] {
				new OneOperatorPart(tokenType, tokenText),
				new OneOperatorPart(precedence),
			}) {}
	}

	public class PostfixMatchOp<Tok> : UnaryMatchOp<Tok>
		where Tok : ITokenValueAndPos
	{
		public PostfixMatchOp(string tokenText, int precedence)
			: this(tokenText, precedence, DefaultPostfixOpName(tokenText), DefaultPostfixOpType(tokenText), null) { }
		public PostfixMatchOp(string tokenText, int precedence, string name)
			: this(tokenText, precedence, name, DefaultPostfixOpType(tokenText), null) { }
		public PostfixMatchOp(string tokenText, int precedence, string name, Symbol exprType)
			: this(tokenText, precedence, name, exprType, null) { }
		public PostfixMatchOp(string tokenText, int precedence, string name, Symbol exprType, Symbol tokenType)
			: base(name, exprType, new OneOperatorPart[] {
				new OneOperatorPart(precedence),
				new OneOperatorPart(tokenType, tokenText),
			}) {}
	}

	/// <summary>
	/// Base class for a low-priority nullary operator. To work well with BasicOneParser,
	/// it is necessary that single-token operators such as Id and Int claim lower 
	/// priority because they are easily involved in "false" ambiguity, wherein, due to
	/// limitations of BasicOneParser, it always thinks that an operator that matches just 
	/// one token (e.g. ID) is ambiguous with an operator that matches that token followed 
	/// by something else. For example, if there is an operator ID 'loves' ID, there may 
	/// only be one interpretation for the input "John loves Sue", but BasicOneParser will 
	/// nevertheless try to disambiguate ID from ID 'loves' ID (see Doc/onep.html for
	/// more information).
	/// </summary>
	/// <typeparam name="Tok">A kind of token, usually IToken</typeparam>
	public class SingleTokenMatchOp<Tok> : BaseMatchOp<Tok>
		where Tok : ITokenValueAndPos
	{
		public SingleTokenMatchOp(string name, Symbol exprType, Symbol tokenType)
			: base(name, exprType, new OneOperatorPart[] {
				new OneOperatorPart(tokenType),
			}) {}
		public override int ComparePriority(IOneOperator<Tok> other)
		{
			// Lower priority than anything different.
			if (other.Type != this.Type)
				return -1;
			else
				return 0;
		}
	}
	public class IDMatchOp<Tok> : SingleTokenMatchOp<Tok>
		where Tok : ITokenValueAndPos
	{
		public IDMatchOp() : base(Localize.From("identifier"), GSymbol.Get("Id"), Tokens.ID) { }
		public IDMatchOp(string name, Symbol exprType, Symbol tokenType) : base(name, exprType, tokenType) { }
	}
	public class INTMatchOp<Tok> : SingleTokenMatchOp<Tok>
		where Tok : ITokenValueAndPos
	{
		public INTMatchOp() : this(Localize.From("integer"), GSymbol.Get("Int"), Tokens.INT) { }
		public INTMatchOp(string name, Symbol exprType, Symbol tokenType) : base(name, exprType, tokenType) { }
	}
	public class BracketsMatchOp<Tok> : BaseMatchOp<Tok>
		where Tok : ITokenValueAndPos
	{
		public BracketsMatchOp() : this(Localize.From("parenthesis"), GSymbol.Get("( )"), Tokens.LPAREN, Tokens.RPAREN) { }
		public BracketsMatchOp(string name, Symbol exprType, Symbol openBracket, Symbol closeBracket)
			: base(name, exprType, new OneOperatorPart[] {
				new OneOperatorPart(openBracket),
				new OneOperatorPart(closeBracket),
			}) {}
		public override int ComparePriority(IOneOperator<Tok> other)
		{
			// Lower priority than anything different.
			if (other.Type != this.Type)
				return -1;
			else
				return 0;
		}
	}
}