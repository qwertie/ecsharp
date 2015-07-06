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
		protected LesPrecedenceMap _prec = LesPrecedenceMap.Default;

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
			F = new LNodeFactory(file);
		}

		// Method required by base class for error messages
		protected override string ToString(int type)
		{
			switch ((TokenType)type) {
				case TT.LParen: return "'('";
				case TT.RParen: return "')'";
				case TT.LBrack: return "'['";
				case TT.RBrack: return "']'";
				case TT.LBrace: return "'{'";
				case TT.RBrace: return "'}'";
				case TT.Colon:  return "':'";
				case TT.Comma:  return "','";
				case TT.Semicolon: return "';'";
			}
			return ((TokenType)type).ToString();
		}
		
		protected LNode MissingExpr() { return F.Id(S.Missing, InputPosition, InputPosition).SetStyle(NodeStyle.Alternate2); }

		static readonly int MinPrec = Precedence.MinValue.Lo;
		public static readonly Precedence StartStmt = new Precedence(MinPrec, MinPrec, MinPrec);

		/*
		protected RVList<LNode> ParseStmtsInside(Token group, RVList<LNode> list = null)
		{
			if (Down(group.Children)) {
				StmtList(ref list);
				return Up(list);
			}
			return list;
		}
		protected RVList<LNode> ParseExprsInside(Token group, RVList<LNode> list = default(RVList<LNode>))
		{
			if (Down(group.Children)) {
				ExprList(ref list);
				return Up(list);
			}
			return list;
		}
		protected virtual LNode ParseBraces(Token t, int endIndex)
		{
			RWList<LNode> list = ParseStmtsInside(t);
			return F.Braces(list.ToRVList(), t.StartIndex, endIndex).SetStyle(NodeStyle.Statement);
		}
		protected virtual LNode ParseCallBraces(LNode target, Token t, int endIndex)
		{
			RWList<LNode> list = ParseStmtsInside(t);
			return F.Call(target, list.ToRVList(), t.StartIndex, endIndex).SetStyle(NodeStyle.Statement);
		}
		protected virtual LNode ParseParens(Token t, int endIndex)
		{
			var list = ParseExprsInside(t);
			if (list.Count == 1)
				return F.InParens(list[0], t.StartIndex, endIndex);
			if (list.Count == 2 && (object)list[1] == MissingExpr)
				return F.Call(S.Tuple, list[0], t.StartIndex, endIndex);
			return F.Call(S.Tuple, list.ToRVList(), t.StartIndex, endIndex);
		}
		protected virtual LNode ParseCall(Token target, Token paren, int endIndex)
		{
			Debug.Assert(target.Type() == TT.Id);
			RVList<LNode> list = ParseExprsInside(paren).ToRVList();
			return F.Call((Symbol)target.Value, list, target.StartIndex, endIndex).SetStyle(NodeStyle.PrefixNotation);
		}
		protected virtual LNode ParseCall(LNode target, Token paren, int endIndex)
		{
			RVList<LNode> list = ParseExprsInside(paren).ToRVList();
			return F.Call(target, list, target.Range.StartIndex, endIndex).SetStyle(NodeStyle.PrefixNotation);
		}*/
		
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
		protected virtual LNode MarkSpecial(LNode primary)
		{
			primary.BaseStyle = NodeStyle.Special;
			return primary;
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
				// This situation is no longer officially supported
				Debug.Assert(lhs != null && lhs.IsCall && lhs.ArgCount > 0);
				Debug.Assert(lhs.BaseStyle != NodeStyle.Special);
				int c = lhs.ArgCount-1;
				LNode ce = MakeSuperExpr(lhs.Args[c], ref primary, rhs);
				return lhs.WithArgChanged(c, ce);
			}
		}

		/// <summary>Top-level rule: expects a sequence of statements followed by EOF</summary>
		public IEnumerable<LNode> Start(Holder<TokenType> separator)
		{
			foreach (var stmt in ExprListLazy(separator))
				yield return stmt;
			if (LA0 != (int)EOF)
				Error(0, "Expected {0}", ToString((int)separator.Value));
		}

		protected override void Error(bool inverted, IEnumerable<int> expected_)
		{
			base.Error(inverted, expected_);
			
			// If a closer was expected...
			int expected = expected_.First();
			if (Token.IsCloser((TokenKind)expected)) {
				// Skip forward until reaching the expected closer, or a closing brace
				while (LA0 != (int)TT.EOF && LA0 != expected && LA0 != (int)TT.RBrace && LA0 != (int)TT.Dedent) {
					if (Token.IsOpener((TokenKind)LA0)) {
						int depth = 1;
						do {
							Skip();
							if (Token.IsCloser((TokenKind)LA0))
								depth--;
						} while (depth > 0);
					}
					Skip();
				}
			} else
				Skip(); // in general, skip to avoid a potential infinite loop
		}
	}
}