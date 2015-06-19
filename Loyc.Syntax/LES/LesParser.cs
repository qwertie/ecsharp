using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc;
using Loyc.Utilities;
using Loyc.Syntax.Lexing;
using Loyc.Collections;
using Loyc.Syntax;

namespace Loyc.Syntax.Les
{
	using TT = TokenType;
	using S = CodeSymbols;

	/// <summary>Parses LES (Loyc Expression Syntax) code into a sequence of Loyc 
	/// trees (<see cref="LNode"/>), one per top-level statement.</summary>
	/// <remarks>
	/// You can use <see cref="LesLanguageService.Value"/> with <see cref="ParsingService.Parse"/>
	/// to easily parse a text string (holding zero or more LES statements) into a Loyc tree.
	/// <para/>
	/// This class expects to receive tokens from <see cref="LesLexer"/> that have been 
	/// preprocessed by <see cref="TokensToTree"/>, with whitespace tokens filtered out.
	/// </remarks>
	public partial class LesParser : BaseParserForList<Token, int>
	{
		protected LNodeFactory F;
		protected LNode _missingExpr = null; // used by MissingExpr
		protected LesPrecedenceMap _prec = LesPrecedenceMap.Default;
		protected IList<Token> _tokensRoot;
		public IList<Token> TokenTree { get { return _tokensRoot; } }

		new const TT EOF = TT.EOF;

		public LesParser(IListAndListSource<Token> tokens, ISourceFile file, IMessageSink messageSink) : this((IList<Token>)tokens, file, messageSink) {}
		public LesParser(IListSource<Token> tokens, ISourceFile file, IMessageSink messageSink) : this(tokens.AsList(), file, messageSink) {}
		public LesParser(IList<Token> tokens, ISourceFile file, IMessageSink messageSink, int startIndex = 0) 
			: base(tokens, default(Token), file, startIndex)
		{
			ErrorSink = messageSink;
		}

		public void Reset(IList<Token> list, ISourceFile file, int startIndex = 0)
		{
			Reset(list, default(Token), file, startIndex);
		}
		protected override void Reset(IList<Token> list, Token eofToken, ISourceFile file, int startIndex = 0)
		{
			CheckParam.IsNotNull("file", file);
			base.Reset(list, eofToken, file, startIndex);
			_tokensRoot = TokenList;
			F = new LNodeFactory(file);
			_missingExpr = null;
		}

		// Method required by base class for error messages
		protected override string ToString(int type_)
		{
			var type = (TokenType)type_;
			return type.ToString();
		}
		
		protected bool Down(int li)
		{
			return Down(LT(li).Children);
		}

		protected LNode MissingExpr { get { return _missingExpr = _missingExpr ?? F.Id(S.Missing); } }

		static readonly int MinPrec = Precedence.MinValue.Lo;
		public static readonly Precedence StartStmt = new Precedence(MinPrec, MinPrec, MinPrec);

		protected virtual RWList<LNode> ParseAttributes(Token group, RWList<LNode> list)
		{
			return AppendExprsInside(group, list);
		}
		protected RWList<LNode> AppendExprsInside(Token group, RWList<LNode> list)
		{
			if (Down(group.Children)) {
				ExprList(ref list);
				return Up(list);
			}
			return list;
		}
		protected RWList<LNode> ExprListInside(Token t)
		{
			return AppendExprsInside(t, new RWList<LNode>());
		}
		protected virtual LNode ParseBraces(Token t, int endIndex)
		{
			RWList<LNode> list = new RWList<LNode>();
			if (Down(t.Children)) {
				StmtList(ref list);
				Up();
			}
			return F.Braces(list.ToRVList(), t.StartIndex, endIndex);
		}
		protected virtual LNode ParseParens(Token t, int endIndex)
		{
			var list = ExprListInside(t);
			if (list.Count == 1)
				return F.InParens(list[0], t.StartIndex, endIndex);
			if (list.Count == 2 && (object)list[1] == MissingExpr)
				return F.Call(S.Tuple, list[0], t.StartIndex, endIndex);
			return F.Call(S.Tuple, list.ToRVList(), t.StartIndex, endIndex);
		}
		protected virtual LNode ParseCall(Token target, Token paren, int endIndex)
		{
			Debug.Assert(target.Type() == TT.Id);
			return F.Call((Symbol)target.Value, ExprListInside(paren).ToRVList(), target.StartIndex, endIndex);
		}
		protected virtual LNode ParseCall(LNode target, Token paren, int endIndex)
		{
			return F.Call(target, ExprListInside(paren).ToRVList(), target.Range.StartIndex, endIndex);
		}
		
		private Symbol ToSuffixOpName(object symbol)
			{ return _prec.ToSuffixOpName(symbol); }
		private Precedence PrefixPrecedenceOf(Token t)
		{
			if (t.TypeInt == (int)TT.BQString)
				return LesPrecedence.Prefix;
			return _prec.Find(OperatorShape.Prefix, t.Value);
		}
		private Precedence SuffixPrecedenceOf(Token t)
		{ 
			return _prec.Find(OperatorShape.Suffix, t.Value);
		}
		private Precedence InfixPrecedenceOf(Token t) 
		{
			if (t.TypeInt == (int)TT.BQString)
				return LesPrecedence.Backtick;
			return _prec.Find(OperatorShape.Infix, t.Value);
		}

		// This is virtual so that a syntax highlighter can easily override and colorize it
		protected virtual void MarkSpecial(LNode primary)
		{
			primary.BaseStyle = NodeStyle.Special;
		}

		protected virtual LNode MakeSuperExpr(LNode lhs, ref LNode primary, RVList<LNode> rhs)
		{
			if (primary == null)
				return lhs; // an error should have been printed already

			if (lhs == primary) {
				if (primary.BaseStyle == NodeStyle.Operator)
					primary = F.Call(primary, rhs);
				else
					primary = lhs.WithArgs(lhs.Args.AddRange(rhs));
				MarkSpecial(primary);
				return primary;
			} else {
				Debug.Assert(lhs != null && lhs.IsCall && lhs.ArgCount > 0);
				Debug.Assert(lhs.BaseStyle != NodeStyle.Special);
				int c = lhs.ArgCount-1;
				LNode ce = MakeSuperExpr(lhs.Args[c], ref primary, rhs);
				return lhs.WithArgChanged(c, ce);
			}
		}
		public IListAndListSource<LNode> ParseExprs()
		{
			var list = new RWList<LNode>();
			ExprList(ref list);
			return list;
		}
		public IListSource<LNode> ParseStmtsGreedy()
		{
			var list = ParseStmtsLazy().Buffered();
			var _ = list.Count; // force greedy parse
			return list;
		}
		public IEnumerator<LNode> ParseStmtsLazy()
		{
			TT la0;
			var next = SuperExprOptUntil(TT.Semicolon);
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Semicolon) {
					yield return next;
					Skip();
					next = SuperExprOptUntil(TT.Semicolon);
				} else
					break;
			}
			if (next != (object)MissingExpr) yield return next;
		}
	}
}