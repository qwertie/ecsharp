using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc;
using Loyc.Syntax;

namespace Loyc.Syntax.Les
{
	using TT = TokenType;

	/// <summary>
	/// Converts a token list into a token tree. Everything inside brackets, parens
	/// or braces is made a child of the open bracket's Block.
	/// </summary>
	public class TokensToTree : ILexer
	{
		public TokensToTree(ILexer source, bool skipWhitespace)
			{ _source = source; _skipWhitespace = skipWhitespace; }

		ILexer _source;
		bool _skipWhitespace;
		public Token? _next;

		public ISourceFile Source
		{
			get { return _source.Source; }
		}
		public Action<int, string> OnError
		{
			get { return _source.OnError; }
			set { _source.OnError = value; }
		}
		public int IndentLevel
		{
			get { return _source.IndentLevel; }
		}
		public int LineNumber
		{
			get { return _source.LineNumber; }
		}
		public void Restart()
		{
			_source.Restart();
			_next = null;
		}

		public Token? NextToken()
		{
			Token? t;
			do {
				if (_next != null) {
					t = _next;
					_next = null;
				} else {
					t = _source.NextToken();
					if (t == null)
						return null;

					TokenType tt = t.Value.Type, ctt;
					if (tt == TT.LParen || tt == TT.LBrack || tt == TT.LBrace) {
						int oldIndentLevel = _source.IndentLevel;
						TokenTree children = new TokenTree(_source.Source);
						for (; ; ) {
							Token? ct = NextToken();
							if (ct == null) {
								OnError(t.Value.StartIndex, Localize.From("Reached end-of-file before '{0}' was closed", tt));
								break;
							}
							ctt = ct.Value.Type;
							if (ctt == TT.LBrace || ctt == TT.LParen || ctt == TT.LBrack) {
								if ((tt == TT.LBrace) != (ctt == TT.RBrace)) {
									// Braces cannot match with non-braces.
									OnError(t.Value.StartIndex, Localize.From("Closing '{0}' does not match opening '{1}'", ctt, tt));
									// But if the closer is not more indented than the opener, close anyway.
									if (IndentLevel <= oldIndentLevel) {
										_next = ct.Value;
										break;
									}
								} else {
									_next = ct.Value;
									break;
								}
							}
							children.Add(ct.Value);
						}
						var tValue = t.Value;
						tValue.Value = children;
						t = tValue;
					}
				}
			} while (_skipWhitespace && t.Value.IsWhitespace);
			return t;
		}
	}
}
