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
	public partial class LesParser : BaseParser<Token>
	{
		protected IMessageSink _messages;
		protected LNodeFactory F;
		protected ISourceFile _sourceFile;
		protected IListSource<Token> _tokensRoot;
		protected IListSource<Token> _tokens;
		// index into source text of the first token at the current depth (inside 
		// parenthesis, etc.). Used if we need to print an error inside empty {} [] ()
		protected int _startTextIndex = 0;
		protected LNode _missingExpr = null; // used by MissingExpr
		protected LesPrecedenceMap _prec = LesPrecedenceMap.Default;
		public IListSource<Token> TokenTree { get { return _tokensRoot; } }
		public ISourceFile SourceFile { get { return _sourceFile; } }

		static readonly Severity _Error = Severity.Error;

		public LesParser(IListSource<Token> tokens, ISourceFile file, IMessageSink messageSink)
		{
			MessageSink = messageSink;
			Reset(tokens, file);
		}

		public virtual void Reset(IListSource<Token> tokens, ISourceFile file)
		{
			_tokensRoot = _tokens = tokens;
			_sourceFile = file;
			F = new LNodeFactory(file);
			InputPosition = 0; // reads LT(0)
			_missingExpr = null;
		}

		public IMessageSink MessageSink
		{
			get { return _messages; } 
			set { _messages = value ?? Loyc.MessageSink.Current; }
		}

		#region Methods required by base class and by LLLPG

		protected sealed override int EofInt() { return 0; }
		protected sealed override int LA0Int { get { return _lt0.TypeInt; } }
		protected sealed override Token LT(int i)
		{
			bool fail;
			return _tokens.TryGet(InputPosition + i, out fail);
		}
		protected override void Error(int li, string message)
		{
			int iPos = GetTextPosition(InputPosition + li);
			SourcePos pos = _sourceFile.IndexToLine(iPos);
			_messages.Write(_Error, pos, message);
		}
		protected int GetTextPosition(int tokenPosition)
		{
			bool fail;
			var token = _tokens.TryGet(tokenPosition, out fail);
			if (!fail)
				return token.StartIndex;
			else if (_tokens.Count == 0 || tokenPosition < 0)
				return _startTextIndex;
			else
				return _tokens[_tokens.Count - 1].EndIndex;
		}
		protected override string ToString(int type_)
		{
			var type = (TokenType)type_;
			return type.ToString();
		}

		protected TT LA0 { [DebuggerStepThrough] get { return _lt0.Type(); } }
		protected TT LA(int i)
		{
			bool fail;
			return _tokens.TryGet(InputPosition + i, out fail).Type();
		}
		const TokenType EOF = TT.EOF;
		
		#endregion
		
		#region Down & Up
		// These are used to traverse into token subtrees, e.g. given w=(x+y)*z, 
		// the outer token list is w=()*z, and the 3 tokens x+y are children of '('
		// So the parser calls something like Down(lparen) to begin parsing inside,
		// then it calls Up() to return to the parent tree.

		Stack<Pair<IListSource<Token>, int>> _parents = new Stack<Pair<IListSource<Token>, int>>();

		protected bool Down(int li)
		{
			return Down(LT(li).Children);
		}
		protected bool Down(IListSource<Token> children)
		{
			if (children != null) {
				_parents.Push(Pair.Create(_tokens, InputPosition));
				_tokens = children;
				InputPosition = 0;
				return true;
			}
			return false;
		}
		protected T Up<T>(T value)
		{
			Up();
			return value;
		}
		protected void Up()
		{
			Debug.Assert(_parents.Count > 0);
			var pair = _parents.Pop();
			_tokens = pair.A;
			InputPosition = pair.B;
		}

		#endregion

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
				la0 = LA0;
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