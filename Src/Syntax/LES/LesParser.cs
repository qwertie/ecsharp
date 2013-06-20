using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.LLParserGenerator;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc;

namespace Loyc.Syntax.Les
{
	using TT = TokenType;
	using S = CodeSymbols;
	using P = LesPrecedence;
	using System.Diagnostics;

	public partial class LesParser : BaseParser<Token, TokenType>
	{
		protected TokenTree _tokens;
		protected LNodeFactory F;

		public LesParser(TokenTree tokens)
		{
			_tokens = tokens; F = new LNodeFactory(_tokens.File);
			MissingExpr = MissingExpr ?? F.Id(S.Missing);
		}

		protected sealed override TT EOF
		{
			get { return TT.EOF; }
		}
		protected sealed override TT LA(int i)
		{
			bool fail = false;
			return _tokens.TryGet(InputPosition + i, ref fail).Type;
		}
		protected sealed override Token LT(int i)
		{
			bool fail = false;
			return _tokens.TryGet(InputPosition + i, ref fail);
		}
		protected override TT ToLA(Token lt)
		{
			return lt.Type;
		}
		protected override bool LT0Equals(TT b)
		{
			return LT0.Type == b;
		}
		protected override string PositionToString(int inputPosition)
		{
			return _tokens.File.IndexToLine(inputPosition).ToString();
		}

		static readonly int MinPrec = Precedence.MinValue.Lo;
		public static readonly Precedence StartStmt = new Precedence(MinPrec, MinPrec, MinPrec);
		static LNode MissingExpr;


		Stack<Pair<TokenTree, int>> _parents;

		bool Down(int li)
		{
			return Down(LT(li).Children);
		}
		bool Down(TokenTree children)
		{
			if (children != null) {
				_parents.Push(Pair.Create(_tokens, InputPosition));
				_tokens = children;
				InputPosition = 0;
				return true;
			}
			return false;
		}
		T Up<T>(T value)
		{
			Up();
			return value;
		}
		void Up()
		{
			Debug.Assert(_parents.Count > 0);
			var pair = _parents.Pop();
			_tokens = pair.A;
			InputPosition = pair.B;
		}

		RWList<LNode> AppendExprsInside(Token group, RWList<LNode> list)
		{
			if (Down(group.Children)) {
				ExprList(ref list);
				return Up(list);
			}
			return list;
		}
		private RWList<LNode> ExprListInside(Token t)
		{
			return AppendExprsInside(t, new RWList<LNode>());
		}
		private LNode InterpretBraces(Token t)
		{
			RWList<LNode> list = new RWList<LNode>();
			if (Down(t.Children)) {
				StmtList(ref list);
				Up();
			}
			return F.Braces(list.ToRVList());
		}
		private LNode InterpretParens(Token t)
		{
			return F.Call(S.Missing, ExprListInside(t).ToRVList());
		}
		private Precedence UnaryPrecedenceOf(Token t)
		{
			throw new NotImplementedException();
		}
		private Precedence InfixPrecedenceOf(Token t)
		{
			throw new NotImplementedException();
		}
		private Symbol ToSuffixOpName(Symbol symbol)
		{
			throw new NotImplementedException();
		}
		private LNode MakeSuperExpr(LNode e, LNode primary, RVList<LNode> otherExprs)
		{
			if (otherExprs.IsEmpty)
				return e;
			throw new NotImplementedException();
		}
	}
}