using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;
using Loyc.Syntax.Les;

namespace Loyc.Syntax.Lexing
{
	[TestFixture]
	public class TokenTests : Assert
	{
		[Test]
		public void ToLiteralLNodeTests()
		{
			var file = EmptySourceFile.Unknown;
			TestToLNode(EmptySourceFile.Unknown, new List<Pair<Token, string>>() {
				P(new Token((int)TokenKind.Literal, 3, 7, 0, "hello!"), @"Literal(""hello!"")"),
				P(new Token((int)TokenKind.Literal, 3, 7, 0, 12345),    @"Literal(12345)"),
				P(new Token((int)TokenKind.Literal, 3, 7, 0, GSymbol.Get("foo")),  @"Literal(@@foo)"),
			});
		}

		[Test]
		public void ToIdLNodeTests()
		{
			var file = EmptySourceFile.Unknown;
			TestToLNode(EmptySourceFile.Unknown, new List<Pair<Token, string>>() {
				P(new Token((int)TokenKind.Dot,          5, 11, 0, CodeSymbols.ColonColon), @"Dot(@@'::)"),
				P(new Token((int)TokenKind.Assignment,   5, 11, 0, CodeSymbols.AddAssign),  @"Assignment(@@'+=)"),
				P(new Token((int)TokenKind.Operator,     5, 11, 0, CodeSymbols.Mul),     @"Operator(@@'*)"),
				P(new Token((int)TokenKind.Separator,    5, 11, 0, CodeSymbols.Comma),   @"Separator(@@`',`)"),
				P(new Token((int)TokenKind.AttrKeyword,  5, 11, 0, CodeSymbols.Public),  @"AttrKeyword(@@#public)"),
				P(new Token((int)TokenKind.TypeKeyword,  5, 11, 0, CodeSymbols.Int32),   @"TypeKeyword(@@#int32)"),
				P(new Token((int)TokenKind.OtherKeyword, 5, 11, 0, CodeSymbols.While),   @"OtherKeyword(@@#while)"),
				P(new Token((int)TokenKind.Other,        5, 11, 0, GSymbol.Get("test")), @"Other(@@test)"),
				P(new Token((int)TokenKind.Other,        5, 11, 0, "test"),              @"Other(""test"")"),
			});
		}

		[Test]
		public void ToBracketsLNodeTests()
		{
			var file = EmptySourceFile.Unknown;
			var child = new TokenTree(file, new[] { new Token((int)TokenKind.Id, 6, 1, NodeStyle.Default, GSymbol.Get("x")) });
			TestToLNode(EmptySourceFile.Unknown, new List<Pair<Token, string>>() {
				P(new Token((int)TokenKind.LParen, 5, 1, 0, child), @"LParen(x)"),
				P(new Token((int)TokenKind.RParen, 7, 1, 0, null),  @"RParen()"),
				P(new Token((int)TokenKind.LParen, 5, 1, 0, null),  @"LParen()"),
				P(new Token((int)TokenKind.RParen, 7, 1, 0, null),  @"RParen()"),
				P(new Token((int)TokenKind.LBrack, 5, 1, 0, child), @"LBrack(x)"),
				P(new Token((int)TokenKind.RBrack, 7, 1, 0, null),  @"RBrack()"),
				P(new Token((int)TokenKind.LBrace, 5, 1, 0, child), @"LBrace(x)"),
				P(new Token((int)TokenKind.RBrace, 7, 1, 0, null),  @"RBrace()"),
				P(new Token((int)TokenKind.Indent, 5, 1, 0, child), @"Indent(x)"),
				P(new Token((int)TokenKind.Dedent, 7, 1, 0, null),  @"Dedent()"),
				P(new Token((int)TokenKind.LOther, 5, 1, 0, child), @"LOther(x)"),
				P(new Token((int)TokenKind.ROther, 7, 1, 0, null),  @"ROther()"),
			});
		}

		static Pair<K, V> P<K, V>(K key, V value)
			{ return Pair.Create(key, value); }
		private void TestToLNode(ISourceFile file, IList<Pair<Token, string>> pairs)
		{
			for (int i = 0; i < pairs.Count; i++)
				TestToLNode(pairs[i].A, file, pairs[i].B);
		}
		private void TestToLNode(Token t, ISourceFile file, string lesString)
		{
			LNode n = t.ToLNode(file);
			AreEqual(lesString, Les2LanguageService.Value.Print(n, MessageSink.Current, ParsingMode.Expressions, "", ""));
			AreEqual(file, n.Source);
			AreEqual(t.StartIndex, n.Range.StartIndex);
			AreEqual((t.Children != null && t.Children.Count > 0 ? t.Children.Last : t).EndIndex, n.Range.EndIndex);
		}
	}
}
