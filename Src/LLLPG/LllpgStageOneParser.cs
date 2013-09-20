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
	using Loyc.Collections.Impl;

	/// <summary>
	/// Parses a <see cref="TokenTree"/> from LES or EC# into an LNode.
	/// </summary>
	/// <remarks>
	/// LLLPG grammars are parsed in two stages. First, a token tree is parsed into 
	/// an <see cref="LNode"/>, e.g. <c>a b | c*</c> is parsed into the tree 
	/// <c>#seq(a, b) | #*(c)</c>. This class handles the first stage. Next, the 
	/// <see cref="LNode"/> is parsed into a tree of <see cref="Pred"/> objects.
	/// <para/>
	/// I was going to use LLLPG to generate a parser for this, but writing 
	/// bootstrap grammars in C# is rather laborious, so I found a shortcut; I 
	/// realized that if I modified the LES parser slightly, I would be able to
	/// write a derived class of <see cref="LesParser"/> that would parse LLLPG's
	/// input language: two different languages with one parser!
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
	/// LES), or <c>(x ɸ y) | z</c> if juxtaposition has higher precedence (as in 
	/// LLLPG). So in <see cref="LesPrecedence"/> I added a precedence value for 
	/// SuperExpr, which is, of course, lower than all other operators; but now
	/// LesParser doesn't use this value directly, it stores it in the P_SuperExpr
	/// field so that the derived class can change its precedence.
	/// <para/>
	/// There was already a method <c>MakeSuperExpr</c> to interpret the 
	/// juxtaposition operator, I just changed it to a virtual method. I also 
	/// created some new virtual methods for handling calls and braces, again so
	/// I could override them.
	/// <para/>
	/// <see cref="LesParser"/> already contains precedence tables; by changing
	/// them, we can change the precedence rules.
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
	/// and LES both share the same <see cref="TokenKind"/> though, and we can use
	/// that to derive an LES TokenType artificially.</li>
	/// </ol>
	/// We can handle both cases in the same way, by using TokenKind to choose an
	/// TokenType, with special handling for operators like *, +, |, /. We can also
	/// handle LLLPG's greedy, nongreedy, default and error keywords by labeling 
	/// them <c>PrefixOp</c>.
	/// <para/>
	/// We should not transform the entire token tree. Many of the tokens are to be
	/// interpreted by the host language; luckily such tokens are always in 
	/// parenthesis or braces and it's not hard to avoid touching them.
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
		public LllpgStageOneParser(IListSource<Token> tokenTree, ISourceFile file, IMessageSink messages) : base(ReclassifyTokens(tokenTree), file, messages)
		{
			_currentLanguage = LanguageService.Current;
			_suffixPrecedence = PredefinedSuffixPrecedence.AsMutable();
			_prefixPrecedence = PredefinedPrefixPrecedence.AsMutable();
			_infixPrecedence = PredefinedInfixPrecedence.AsMutable();
			P_SuperExpr = P.AndBits;
		}

		ILanguageService _currentLanguage;

		public LNode Parse()
		{
			var buffer = ParseStmtsUntilEnd().Buffered();
			if (buffer.Count == 0)
				return F.Call(_Seq);
			else if (buffer.Count == 1)
				return buffer[0];
			else
				return F.Call(_Seq, buffer);
		}

		#region Overrides

		protected override LNode MakeSuperExpr(LNode lhs, ref LNode primary, RVList<LNode> rhs)
		{
			rhs.Insert(0, lhs);
			return F.Call(_Seq, rhs);
		}
		protected override LNode ParseBraces(Token t, int endIndex)
		{
			var ch = t.Children;
			if (ch == null)
				return F.Braces();
			else
				return F.Braces(
					_currentLanguage.Parse(ch, ch.File, MessageSink, LanguageService.Stmts).Buffered());
		}

		internal static readonly Symbol _Seq = GSymbol.Get("#seq");

		protected override LNode ParseParens(Token t, int endIndex)
		{
			var ch = t.Children;
			if (ch == null)
				return F.Call(_Seq);
			else {
				var newList = ReclassifyTokens(ch);
				G.Verify(Down(newList));
				RWList<LNode> list = new RWList<LNode>();
				ExprList(ref list);
				if (list.Count == 1)
					return Up(list[0]);
				else
					return Up(F.Call(_Seq, list.ToRVList()));
			}
		}

		#endregion

		#region Code for remapping operators

		static TokenType[] _kindToTT = get_kindToTT();
		static TokenType[] get_kindToTT()
		{
			var map = new TT[((int)TokenKind.KindMask >> 8) + 1];
			for (int i = 0; i < map.Length; i++)
				map[i] = (TT)(i << 8);
			map[(int)TokenKind.Spaces >> 8] = TokenType.Spaces;
			return map;
		}
		static TokenType SwitchType(TokenKind kind) { return _kindToTT[(int)kind >> 8]; }

		static IListSource<Token> ReclassifyTokens(IListSource<Token> oldList)
		{
			// Only reclassifies tokens on the current level. Child tokens are untouched.
			InternalList<Token> newList = new InternalList<Token>(oldList.Count);
			for (int i = 0; i < oldList.Count; i++)
				newList.Add(Reclassify(oldList[i]));
			return newList;
		}

		private static Token Reclassify(Token token)
		{
			var newType = SwitchType(token.Kind);
			if (token.Kind == TokenKind.Operator) {
				// Decide what kind of operator it is
				if (PredefinedSuffixPrecedence.ContainsKey(token.Value))
					newType = TokenType.SuffixOp;
				else if (token.Value == S.NotBits || token.Value == S.Not || token.Value == S.AndBits)
					newType = TokenType.PrefixOp;
				else
					newType = TokenType.NormalOp;
			} else if (token.Kind == TokenKind.Id || token.Kind == TokenKind.OtherKeyword || token.Kind == TokenKind.AttrKeyword) {
				// Is it one of our "prefix keywords"?
				if (PredefinedPrefixPrecedence.ContainsKey(token.Value)) {
					var sym = token.Value as Symbol;
					// we're only looking for words here: "greedy", "default", etc.
					if (sym != null && char.IsLetter(sym.Name[sym.Name.Length - 1]))
						newType = TokenType.PrefixOp;
				}
			}
			return token.WithType((int)newType);
		}

		new static readonly Map<object, Precedence> PredefinedSuffixPrecedence =
			LesParser.PredefinedSuffixPrecedence.Union(
				new MMap<object, Precedence>() {
					{ S.Mul,          P.Primary  }, // (...)*
					{ S.Add,          P.Primary  }, // (...)+
					{ S.QuestionMark, P.Primary  }, // (...)?
				}, true);
		new static readonly Map<object, Precedence> PredefinedInfixPrecedence =
			LesParser.PredefinedInfixPrecedence.Union(
				new MMap<object, Precedence>() {
					{ S.OrBits,       P.Or  },     // a | b
					{ S.Div,          P.Or  },     // a / b
					{ S.Lambda,       P.Compare }, // prediction => match
				}, true);
		new static readonly Map<object, Precedence> PredefinedPrefixPrecedence =
			LesParser.PredefinedPrefixPrecedence.Union(
				new MMap<object, Precedence>() {
					{ S.AndBits, P.Prefix },
					{ GSymbol.Get("greedy"), P.Prefix },
					{ GSymbol.Get("nongreedy"), P.Prefix },
					{ GSymbol.Get("default"), P.Prefix },
					{ GSymbol.Get("#default"), P.Prefix }, // in EC# it's a keyword, hence the #
					{ GSymbol.Get("error"), P.Prefix },
				}, true);
		
		#endregion // remapping operators
	}
}
