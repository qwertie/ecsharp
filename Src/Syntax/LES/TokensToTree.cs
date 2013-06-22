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
using NUnit.Framework;
	using System.Diagnostics;
	using Loyc.Collections.Impl;

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
		bool _closerMatched;
		Token? _closer;

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
		public void Reset()
		{
			_source.Reset();
		}

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

		public Token? NextToken()
		{
			_current = LLNextToken();
			if (_current == null)
				return null;

			TokenType tt = _current.Value.Type;
			if (tt == TT.LParen || tt == TT.LBrack || tt == TT.LBrace || tt == TT.OpenOf) {
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

			TokenType ott = openToken.Type;
			int oldIndentLevel = _source.IndentLevel;
			TokenTree children = new TokenTree(_source.Source);

			for (;;) {
				Token? t = LLNextToken(); // handles LBrace, LParen, LBrack internally
				if (t == null) {
					OnError(openToken.StartIndex, Localize.From("Reached end-of-file before '{0}' was closed", openToken.ToString()));
					break;
				}
				TokenType tt = t.Value.Type;
				if (tt == TT.LParen || tt == TT.LBrack || tt == TT.LBrace || tt == TT.OpenOf) {
					var v = t.Value;
					GatherChildren(ref v);
					children.Add(v);
					if (_closer != null && _closerMatched) {
						children.Add(_closer.Value);
						_closer = null;
					}
				} else if (tt == TT.RBrace || tt == TT.RParen || tt == TT.RBrack) {
					// '{' must match '}' (the parser can complain about "(]" and "[)" if it wants)
					if ((ott == TT.LBrace) != (tt == TT.RBrace)) {
						OnError(openToken.StartIndex, Localize.From("Opening '{0}' does not match closing '{1}' on line {2}", 
							openToken.ToString(), t.Value.ToString(), _source.Source.IndexToLine(t.Value.StartIndex)));
						// - If the closer is more indented than the opener, do not close.
						// - If the closer is less indented than the opener, close but do not match.
						// - If the closer is the same indentation as the opener, close and match.
						if (IndentLevel <= oldIndentLevel) {
							_closer = t.Value;
							_closerMatched = (IndentLevel == oldIndentLevel);
							break;
						} else
							children.Add(t.Value);
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

		Token? _current;
		void IDisposable.Dispose() {}
		Token IEnumerator<Token>.Current { get { return _current.Value; } }
		object System.Collections.IEnumerator.Current { get { return _current; } }
		bool System.Collections.IEnumerator.MoveNext()
		{
			NextToken();
			return _current.HasValue;
		}
	}

	[TestFixture]
	public class TokensToTreeTests : TestHelpers
	{
		[Test]
		public void Test1()
		{
			var list = Lex("() {} []");
			Expect(list, A(TT.LParen, TT.RParen, TT.LBrace, TT.RBrace, TT.LBrack, TT.RBrack));
			Expect(list[0].Children, A());
			Expect(list[2].Children, A());
			Expect(list[4].Children, A());
			list = Lex("a (b ) c { d } e [123 f] ..");
			Expect(list, A(TT.Id, TT.LParen, TT.RParen, TT.Id, TT.LBrace, TT.RBrace, TT.Id, TT.LBrack, TT.RBrack, TT.NormalOp), _("a"));
			Expect(list[1].Children, A(TT.Id), _("b"));
			Expect(list[4].Children, A(TT.Id), _("d"));
			Expect(list[7].Children, A(TT.Number, TT.Id), 123, _("f"));
		}

		[Test]
		public void Test2()
		{
			var list = Lex("{b (c)} ([]) [{123}++]");
			Expect(list, A(TT.LBrace, TT.RBrace, TT.LParen, TT.RParen, TT.LBrack, TT.RBrack));
			Expect(list[0].Children, A(TT.Id, TT.LParen, TT.RParen), _("b"));
			Expect(list[0].Children[1].Children, A(TT.Id), _("c"));
			Expect(list[2].Children, A(TT.LBrack, TT.RBrack));
			Expect(list[4].Children, A(TT.LBrace, TT.RBrace, TT.PreSufOp));
			Expect(list[4].Children[0].Children, A(TT.Number), 123);
			
			list = Lex("(x] + [123)", false);
			Expect(list, A(TT.LParen, TT.RBrack, TT.Spaces, TT.NormalOp, TT.Spaces, TT.LBrack, TT.RParen));
			Expect(list[0].Children, A(TT.Id), _("x"));
			Expect(list[5].Children, A(TT.Number), 123);
		}

		[Test]
		public void Errors1()
		{
			var list = Lex(@"
				namespace {
					foo { get ] // mismatch. ] is matched with { because they're indented the same amount
				} // matched with 'namespace {'
				""Fin""
			");
			Expect(list, A(TT.Id, TT.LBrace, TT.RBrace, TT.String), _("namespace"));
			Expect(list[1].Children, A(TT.Id, TT.LBrace, TT.RBrack), _("foo"));

			list = Lex(@"
				namespace {
					bar {
						foo(... // unclosed
					}
					(...)
				}
			");
			Expect(list, A(TT.Id, TT.LBrace, TT.RBrace), _("namespace"));
			Expect(list[1].Children, A(TT.Id, TT.LBrace, TT.RBrace, TT.LParen, TT.RParen), _("bar"));
			Expect(list[1].Children[1].Children, A(TT.Id, TT.LParen), _("foo"));
			
			list = Lex(@"
				namespace {
					(bar
						{ foo
					) // ')' is matched with '(' due to matching indentation
				}
			");
			Expect(list, A(TT.Id, TT.LBrace, TT.RBrace), _("namespace"));
			Expect(list[1].Children, A(TT.LParen, TT.RParen));
			Expect(list[1].Children[0].Children, A(TT.Id, TT.LBrace), _("bar"));
			Expect(list[1].Children[0].Children[1].Children, A(TT.Id), _("foo"));
		}
		
		[Test]
		public void Errors2()
		{
			var list = Lex(@"
				namespace {
					(a {
						b
				} 123 // matches 'a {'; indentation is irrelevant during proper match
				} // matches 'namespace {' due to indentation
			");
			Expect(list, A(TT.Id, TT.LBrace, TT.RBrace), _("namespace"));
			Expect(list[1].Children, A(TT.LParen));
			Expect(list[1].Children[0].Children, A(TT.Id, TT.LBrace, TT.RBrace, TT.Number), _("a"));

			list = Lex(@"
				namespace {
					(a {
						b
				} 123 // matches 'a {'; indentation is irrelevant during proper match
					} // matches '(' due to indentation level
			");
			Expect(list, A(TT.Id, TT.LBrace), _("namespace"));
			Expect(list[1].Children, A(TT.LParen, TT.RBrace));
			Expect(list[1].Children[0].Children, A(TT.Id, TT.LBrace, TT.RBrace, TT.Number), _("a"));

			list = Lex(@"
				a {
					b( }; // '}' paired with '(' due to extra indentation
				};
			");
			Expect(list, A(TT.Id, TT.LBrace, TT.RBrace, TT.Semicolon), _("a"));
			Expect(list[1].Children, A(TT.Id, TT.LParen, TT.RBrace, TT.Semicolon), _("b"));

			list = Lex(@"
				a {
					b(
						frack! } // } does not close anything due to extra indentation
					);
				};
			");
			Expect(list, A(TT.Id, TT.LBrace, TT.RBrace, TT.Semicolon), _("a"));
			Expect(list[1].Children, A(TT.Id, TT.LParen, TT.RParen, TT.Semicolon), _("b"));
			Expect(list[1].Children[1].Children, A(TT.Id, TT.NormalOp, TT.RBrace), _("frack"), _("#!"));
		}

		[DebuggerStepThrough] static TokenType[] A(params TokenType[] list) { return list; }
		[DebuggerStepThrough] static Symbol _(string s) { return GSymbol.Get(s); }

		List<Token> Lex(string input, bool skipWS = true)
		{
			var lexer = new LesLexer(input, (_, msg) => { Trace.WriteLine(msg); });
			var lexer2 = new TokensToTree(lexer, skipWS);
			var list = new List<Token>();
			Token? token;
			while ((token = lexer2.NextToken()) != null)
				list.Add(token.Value);
			return list;
		}
		void Expect(IList<Token> list, TokenType[] tokenTypes, params object[] values)
		{
			Debug.Assert(values.Length <= tokenTypes.Length);
			Assert.AreEqual(tokenTypes.Length, list.Count);
			for (int i = 0; i < tokenTypes.Length; i++)
			{
				Assert.AreEqual(tokenTypes[i], list[i].Type);
				if (i < values.Length)
					Assert.AreEqual(values[i], list[i].Value);
			}
		}
	}
}
