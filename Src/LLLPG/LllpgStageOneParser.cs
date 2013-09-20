using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Syntax.Les;
using Loyc.Syntax;
using Loyc.Utilities;
using Loyc.Collections;
using Loyc.Syntax.Lexing;

namespace Loyc.LLParserGenerator
{
	using TT = TokenType;
	using S = CodeSymbols;
	using P = LesPrecedence;

	/// <summary>
	/// Parses a <see cref="TokenTree"/> from LES or EC# into an LNode.
	/// </summary>
	/// <remarks>
	/// I was going to write a separate parser in LLLPG for this, but writing 
	/// bootstrap grammars in C# is rather laborious, so I found a shortcut; I 
	/// realized that if I modified the LES parser slightly, I would be able to
	/// write a derived class of <see cref="LesParser"/> that would parse LLLPG's
	/// input language: two different languages, but only one parser!
	/// <para/>
	/// The first thing I did was to change superexpression parsing so that a
	/// "superexpression" is treated more like an ordinary operator. Previously,
	/// there was a SuperExpr rule that simply called Expr repeatedly. I changed
	/// this so that superexpression parsing occurs inside Expr itself, alongside
	/// the other operators, which allowed me to control its precedence.
	/// <para/>
	/// If you think about "juxtaposition" as a special operator ɸ, then it can be 
	/// assigned a precedence like all the other operators. So "x y | z" can be 
	/// parsed as <c>x ɸ (y | z)</c> if juxtaposition has low precedence (normal 
	/// LES), or <c>(x ɸ y) | z</c> if juxtaposition has high precedence (as in 
	/// LLLPG). So in <see cref="LesPrecedence"/> I added a precedence value for 
	/// SuperExpr, which is, of course, lower than all other operators; but now
	/// LesParser doesn't use this value directly, it stores it in the P_SuperExpr
	/// field so that the derived class can change its precedence.
	/// <para/>
	/// There was already a method <c>MakeSuperExpr</c> to interpret the 
	/// juxtaposition operator, I just changed it to a virtual method.
	/// <para/>
	/// <see cref="LesParser"/> already contains precedence tables, so by changing
	/// them we can change the precedence rules.
	/// <para/>
	/// The final problem is the input tokens. The original LES parser expects a
	/// certain set of token types produced by the LES lexer. There are two problems 
	/// to overcome:
	/// <ol>
	/// <li>The LES lexer has built-in knowledge about which operators are prefix
	/// and/or suffix operators, and assigns <see cref="TokenType"/> values such as
	/// NormalOp, PreSufOp, SuffixOp and PrefixOp to indicate what kind of operator
	/// a token can be. We need to change these token types for certain operators,
	/// e.g. we need to "reprogram" *, + and ? to be suffix operators.</li>
	/// <li>The Ecs lexer produces token type codes that are almost completely
	/// different, so we can't feed EC# tokens directly into the LES parser. EC#
	/// and LES both share the same <see cref="TokenKind"/>
	/// 
	/// TODO
	/// </li>
	/// </ol>
	/// 
	/// </remarks>
	public class LllpgStageOneParser : LesParser
	{
		/// <summary>Parses a token tree into a sequence of LNodes, one per top-
		/// level statement in the input.</summary>
		public static IEnumerator<LNode> Parse(IListSource<Token> tokenTree, ISourceFile file, IMessageSink messages)
		{
			var parser = new LllpgStageOneParser(tokenTree, file, messages);
			return parser.ParseStmtsUntilEnd();
		}
		public LllpgStageOneParser(IListSource<Token> tokenTree, ISourceFile file, IMessageSink messages) : base(tokenTree, file, messages)
		{
		}
		protected override LNode MakeSuperExpr(LNode lhs, ref LNode primary, RVList<LNode> rhs)
		{
			return base.MakeSuperExpr(lhs, ref primary, rhs);
		}
	}
}
