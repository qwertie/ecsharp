using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.MiniTest;
using Loyc.Collections;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections.Impl;
using TK = Loyc.Syntax.Lexing.TokenKind;
using Loyc.Utilities;

namespace Loyc.Syntax.Lexing
{
	/// <summary>
	/// Converts a token list into a token tree. Everything inside brackets, parens
	/// or braces is made a child of the open bracket's Block.
	/// </summary>
	public class TokensToTree : LexerWrapper
	{
		public TokensToTree(ILexer source, bool skipWhitespace) : base(source)
			{ _skipWhitespace = skipWhitespace; }

		bool _skipWhitespace;
		bool _closerMatched;
		Token? _closer;

		Token? LLNextToken()
		{
			Token? t;
			if (_closer != null) {
				t = _closer;
				_closer = null;
				return t;
			}
			do
				t = _source.NextToken();
			while (_skipWhitespace && t != null && t.Value.IsWhitespace);
			return t;
		}

		public override Token? NextToken()
		{
			_current = LLNextToken();
			if (_current == null)
				return null;

			TK tt = _current.Value.Kind;
			if (Token.IsOpener(tt)) {
				var v = _current.Value;
				GatherChildren(ref v);
				return _current = v;
			} else
				return _current;
		}

		void GatherChildren(ref Token openToken)
		{
			Debug.Assert(openToken.Value == null);
			if (openToken.Value != null && openToken.Children != null)
				return; // wtf, it's already a tree

			TK ott = openToken.Kind;
			int oldIndentLevel = _source.IndentLevel;
			TokenTree children = new TokenTree(_source.SourceFile);

			for (;;) {
				Token? t = LLNextToken(); // handles LBrace, LParen, LBrack internally
				if (t == null) {
					WriteError(openToken.StartIndex, "Reached end-of-file before '{0}' was closed", openToken);
					break;
				}
				TK tt = t.Value.Kind;
				if (Token.IsOpener(tt)) {
					var v = t.Value;
					GatherChildren(ref v);
					children.Add(v);
					if (_closer != null && _closerMatched) {
						children.Add(_closer.Value);
						_closer = null;
					}
				} else if (Token.IsCloser(tt)) {
					// indent must match dedent, '{' must match '}' (the parser 
					// can complain itself about "(]" and "[)" if it wants; we 
					// allow these to match because some languages might want it.)
					bool dentMismatch = (ott == TK.Indent) != (tt == TK.Dedent);
					if (dentMismatch || (ott == TK.LBrace) != (tt == TK.RBrace))
					{
						WriteError(openToken.StartIndex, "Opening '{0}' does not match closing '{1}' on line {2}", 
							openToken.ToString(), t.Value.ToString(), SourceFile.IndexToLine(t.Value.StartIndex).Line);
						// - If dentMismatch and ott == TK.Indent, do not close.
						// - If dentMismatch and tt = TK.Dedent, close but do not match.
						// - If the closer is more indented than the opener, do not close.
						// - If the closer is less indented than the opener, close but do not match.
						// - If the closer is the same indentation as the opener, close and match.
						if (dentMismatch ? tt == TK.Dedent : IndentLevel <= oldIndentLevel) {
							// close
							_closer = t.Value;
							_closerMatched = !dentMismatch && (IndentLevel == oldIndentLevel);
							break;
						} else
							children.Add(t.Value); // do not close
					} else {
						_closer = t.Value;
						_closerMatched = true;
						break;
					}
				} else
					children.Add(t.Value);
			}
			openToken.Value = children;
		}
	}
}
