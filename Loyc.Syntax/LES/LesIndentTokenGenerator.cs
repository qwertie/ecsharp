using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Syntax.Lexing;

namespace Loyc.Syntax.Les
{
	/// <summary>Indent postprocessor for Loyc Expression Syntax</summary>
	/// <remarks>
	/// LES's "Python mode" is comparable to Python's rules, but I want LES with 
	/// Python mode to be able to parse <i>most</i> of the code that was designed 
	/// for LES without Python mode. For example, in LES (without indentation 
	/// processing) you could write a statement like
	/// <code>
	/// if foo "foo "
	///     + "detected " +
	///     "here";
	/// </code>
	/// And I'd like this code to still parse OK when <c>IndentTokenGenerator</c> 
	/// is inserted into the pipeline. Therefore, <see cref="LesIndentTokenGenerator"/>
	/// will allow unexpected indentation and treat it as a way of continuing 
	/// the previous statement.
	/// <para/>
	/// Code like
	/// <code>
	///   if condition:
	///      then();
	///   if condition:
	///      then();
	///   :else:
	///      otherwise();
	///   foo
	///   : bar;
	/// </code>
	/// is translated to this (where [ and ] represent Indent and Dedent):
	/// <code>
	///   if condition: [
	///      then(); };
	///   ] ;
	///   if condition: [
	///      then();
	///   ] else [
	///      otherwise(); 
	///   ] ;
	///   foo bar;
	/// </code>
	/// Notice that dedents are normally followed by semicolons, and that colons 
	/// are suppressed if they appear at the beginning of a line AND block the 
	/// generation of a semicolon. A colon is not suppressed if it does not have
	/// this effect; forexample
	/// <code>
	/// :
	///     hi();
	///     : Foo;
	/// </code>
	/// means
	/// <code>
	/// {
	///     hi();
	///     `:` Foo;
	/// };
	/// </code>
	/// <para/>
	/// Note: originally I allowed code such as
	/// <code>
	/// if foo: if bar: print(foo + bar);
	///     print(foo only);
	/// </code>
	/// as equivalent to
	/// <code>
	/// if foo { if bar { print(foo + bar);
	///     print(foo only); }; };
	/// </code>
	/// But I decided it would be better to treat code such as
	/// <code>
	/// x = a:b;
	/// </code>
	/// as a simple statement instead. This means, however, that
	/// <code>
	/// if Foo: x = 5;
	/// </code>
	/// does not have the meaning you would expect! You must use braces for this:
	/// <code>
	/// if Foo { x = 5; };
	/// </code>
	/// </remarks>
	class LesIndentTokenGenerator : IndentTokenGenerator<Token>
	{
		public LesIndentTokenGenerator(ILexer<Token> lexer) : base(lexer) { }

		public override TokenCategory GetTokenCategory(Token token)
		{
			TokenType t = token.Type();
			if (Token.IsOpenerOrCloser((TokenKind)t))
				return Token.IsOpener((TokenKind)t) ? TokenCategory.OpenBracket : TokenCategory.CloseBracket;
			if (t == TokenType.Colon)
				return TokenCategory.IndentTrigger;
			if (token.Value == WhitespaceTag.Value)
				return TokenCategory.Whitespace;
			return TokenCategory.Other;
		}

		protected override Maybe<Token> MakeIndentToken(Token indentTrigger, ref Maybe<Token> tokenAfterward, bool newlineAfter)
		{
			if (newlineAfter)
				return new Token((int)TokenType.Indent, indentTrigger.EndIndex, 0, null);
			else
				return Maybe<Token>.NoValue;
		}
		protected override IEnumerator<Token> MakeDedentToken(Token tokenBeforeDedent, ref Maybe<Token> tokenAfterDedent)
		{
			var next = tokenAfterDedent.Or(default(Token));
			if (next.Type() == TokenType.Colon) {
				// Note: we shouldn't delete the colon by setting tokenAfterDedent=NoValue. Consider:
				//   line1:
				//     line2:
				//       line3()
				//   :line4
				// Here, two dedents are generated. If the first dedent deletes 
				// the colon, the second one will not see the colon and will not
				// be aware that it should suppress a semicolon.
				return Range.Single(new Token((int)TokenType.Dedent, next.StartIndex, 0, 0, null)).GetEnumerator();
			} else {
				return ((IEnumerable<Token>)new Token[] { 
					new Token((int)TokenType.Dedent,    next.StartIndex, 0, 0, null),
					new Token((int)TokenType.Semicolon, next.StartIndex, 0, 0, CodeSymbols.Semicolon),
				}).GetEnumerator();
			}
		}

		protected override bool IndentChangedUnexpectedly(Token tokenBeforeNewline, ref Maybe<Token> tokenAfterNewline, ref int deltaIndent)
		{
			if (deltaIndent > 0) { // indent means "continue current statement on next line"
				deltaIndent = 0;   // avoid changing official indentation level
				return false;      // avoid generating a semicolon
			}
			base.IndentChangedUnexpectedly(tokenBeforeNewline, ref tokenAfterNewline, ref deltaIndent);
			return true;
		}
		protected override Maybe<Token> MakeEndOfLineToken(Token tokenBeforeNewline, ref Maybe<Token> tokenAfterNewline, int? deltaIndent)
		{
			if (tokenBeforeNewline.Type() != TokenType.Semicolon &&
				deltaIndent <= 0 && tokenAfterNewline.HasValue)
			{
				if (tokenAfterNewline.Value.Type() == TokenType.Colon)
					tokenAfterNewline = Maybe<Token>.NoValue;
				else {
					ErrorSink.Write(Severity.Warning, Lexer.IndexToLine(tokenBeforeNewline.EndIndex),
						"Possibly missing semicolon. Proceeding as if the ';' were present.");
					return new Token((int)TokenType.Semicolon, tokenBeforeNewline.EndIndex, 0, CodeSymbols.Semicolon);
				}
			}
			return Maybe<Token>.NoValue;
		}
	}
}
