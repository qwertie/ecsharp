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
	/// You can use <see cref="Les2LanguageService.Value"/> with <see cref="ParsingService.Parse"/>
	/// to easily parse a text string (holding zero or more LES statements) into a Loyc tree.
	/// <para/>
	/// This class expects to receive tokens from <see cref="Les2Lexer"/> that have been 
	/// preprocessed by <see cref="TokensToTree"/>, with whitespace tokens filtered out.
	/// </remarks>
	public partial class Les2Parser : BaseParserForList<Token, int>
	{
		protected LNodeFactory F;
		protected LesPrecedenceMap _prec = LesPrecedenceMap.Default;

		new const TT EOF = TT.EOF;

		public Les2Parser(IListAndListSource<Token> tokens, ISourceFile file, IMessageSink messageSink) : this((IList<Token>)tokens, file, messageSink) {}
		public Les2Parser(IListSource<Token> tokens, ISourceFile file, IMessageSink messageSink) : this(tokens.AsList(), file, messageSink) {}
		public Les2Parser(IList<Token> tokens, ISourceFile file, IMessageSink messageSink, int startIndex = 0) 
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
			if (list is TokenTree) {
				// Token trees can come from token literals, and the user of a
				// token tree expects to be able to parse that tree, but this parser
				// expects a flat token list, so we need to flatten the tree again.
				list = ((TokenTree)list).Flatten();
			}
			CheckParam.IsNotNull("file", file);
			base.Reset(list, eofToken, file, startIndex);
			F = new LNodeFactory(file);
		}

		// Method required by base class for error messages
		protected override string ToString(int type)
		{
			switch ((TokenType)type) {
				case TT.SpaceLParen: return "' ('";
				case TT.LParen: return "'('";
				case TT.RParen: return "')'";
				case TT.LBrack: return "'['";
				case TT.RBrack: return "']'";
				case TT.LBrace: return "'{'";
				case TT.RBrace: return "'}'";
				case TT.Comma:  return "','";
				case TT.Semicolon: return "';'";
			}
			return ((TokenType)type).ToString();
		}
		
		protected LNode MissingExpr(Token tok) { return F.Id(S.Missing, tok.StartIndex, tok.EndIndex); }

		static readonly int MinPrec = Precedence.MinValue.Lo;
		public static readonly Precedence StartStmt = Precedence.MinValue;

		protected Symbol ToSuffixOpName(object symbol)
			{ return _prec.ToSuffixOpName(symbol); }
		protected Precedence PrefixPrecedenceOf(Token t)
		{
			if (t.TypeInt == (int)TT.BQOperator)
				return LesPrecedence.Prefix;
			return _prec.Find(OperatorShape.Prefix, t.Value);
		}
		protected Precedence SuffixPrecedenceOf(Token t)
		{ 
			return _prec.Find(OperatorShape.Suffix, t.Value);
		}
		protected Precedence InfixPrecedenceOf(Token t) 
		{
			return _prec.Find(OperatorShape.Infix, t.Value);
		}

		// This is virtual so that a syntax highlighter can easily override and colorize it
		protected virtual LNode MarkSpecial(LNode n)
		{
			return n.SetBaseStyle(NodeStyle.Special);
		}
		// This is virtual so that a syntax highlighter can easily override and colorize it
		protected virtual LNode MarkCall(LNode n)
		{
			return n.SetBaseStyle(NodeStyle.PrefixNotation);
		}


		/// <summary>Top-level rule: expects a sequence of statements followed by EOF</summary>
		public IEnumerable<LNode> Start(Holder<TokenType> separator)
		{
			foreach (var stmt in ExprListLazy(separator))
				yield return stmt;
			Match((int) EOF, (int) separator.Value);
		}

		protected override void Error(bool inverted, IEnumerable<int> expected_)
		{
			TT expected = (TT)expected_.First();
			bool expEnder = expected == TT.Semicolon || expected == TT.Comma || expected == TT.EOF;
			if (expEnder && LA0 == (int)TT.SpaceLParen)
				Error(0, "Syntax error. If a function call was intended, remove the space(s) before '('.");
			else
				base.Error(inverted, expected_);
			
			// If an ender or closer was expected...
			if (expEnder || Token.IsCloser((TokenKind)expected)) {
				// Skip forward until reaching the expected closer, or a closing brace
				while ((TT)LA0 != TT.EOF && (TT)LA0 != expected && (TT)LA0 != TT.RBrace) {
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
				if ((TT)LA0 == expected)
					Skip();
			}
		}
	}
}