using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Loyc.Syntax.Les;
using Loyc.Collections.Impl;
using Loyc.Utilities;

namespace Loyc.Syntax.Lexing
{
	using TT = TokenType;
	using System.Diagnostics;

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
			Expect(list[1].Children[1].Children, A(TT.Id, TT.Not, TT.RBrace), _("frack"), _("#!"));
		}

		[DebuggerStepThrough] static TokenType[] A(params TokenType[] list) { return list; }
		[DebuggerStepThrough] static Symbol _(string s) { return GSymbol.Get(s); }

		List<Token> Lex(string input, bool skipWS = true)
		{
			var lexer = new LesLexer(input, MessageSink.Trace);
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
				Assert.AreEqual(tokenTypes[i], list[i].Type());
				if (i < values.Length)
					Assert.AreEqual(values[i], list[i].Value);
			}
		}
	}
}
