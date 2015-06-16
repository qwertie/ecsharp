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
	/// Although I refer to LES's "Python mode", its rules are substantially 
	/// different from Python. For one thing, I want the parser to be able to parse
	/// input such as 
	/// <code>
	/// if foo: if bar: print(foo + bar);
	///     print(foo only);
	/// </code>
	/// as equivalent to
	/// <code>
	/// if foo { if bar { print(foo + bar);
	///     print(foo only); }; };
	/// </code>
	/// Also, I want LES with Python mode to be able to parse <i>most</i> of the 
	/// code that was designed for LES without Python mode. For example, in LES 
	/// (without indentation processing) you could write a statement like
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
	/// Also, I'd like to avoid modifying the LES parser to support Python mode--
	/// this postprocessor will strip out colons and insert semicolons in the right
	/// places so that the output looks like the original LES without Python mode.
	/// <para/>
	/// For dedents, <see cref="MakeDedentToken"/> is overridden, so that code
	/// like
	/// <code>
	///   if condition:
	///      then();
	///   if condition:
	///      then();
	///   :else:
	///      otherwise();
	/// </code>
	/// is translated to
	/// <code>
	///   if condition {
	///      then(); };
	///   if condition {
	///      then();
	///   } else {
	///      otherwise(); };
	/// </code>
	/// Notice that the colons are suppressed, including the one before <c>else</c> 
	/// (which is suppressed and then <i>not</i> treated as an indent trigger.)
	/// </remarks>
	public class LesIndentTokenGenerator : IndentTokenGenerator
	{
		readonly static int[] IndentTrigger  = new int[] { (int)TokenType.Colon };

		public LesIndentTokenGenerator(ILexer<Token> lexer) 
			: base(lexer, IndentTrigger, 
			       new Token((int)TokenType.Semicolon, 0, 0, CodeSymbols.Semicolon),
			       new Token((int)TokenType.LBrace, 0, 0, null),
			       new Token((int)TokenType.RBrace, 0, 0, null)) { }

		protected override IEnumerator<Token> MakeDedentToken(Token tokenBeforeDedent, ref Maybe<Token> tokenAfterDedent)
		{
			if (!EolToken.HasValue)
				return base.MakeDedentToken(tokenBeforeDedent, ref tokenAfterDedent);
			var next = tokenAfterDedent.Or(default(Token));
			if ((TokenType)next.TypeInt == TokenType.Colon) {
				tokenAfterDedent = new Token(DedentToken.TypeInt, next.StartIndex, next.Length, next.Style, null);
				return EmptyEnumerator<Token>.Value;
			} else {
				return ((IEnumerable<Token>)new Token[] { 
					DedentToken.WithStartIndex(tokenBeforeDedent.EndIndex),
					EolToken.Value.WithStartIndex(tokenBeforeDedent.EndIndex), 
				}).GetEnumerator();
			}
		}
		protected override bool IndentChangedUnexpectedly(Token tokenBeforeNewline, ref Maybe<Token> tokenAfterNewline, ref int deltaIndent)
		{
			return false;
		}
		protected override Maybe<Token> MakeEndOfLineToken(Token tokenBeforeNewline, ref Maybe<Token> tokenAfterNewline, int deltaIndent)
		{
			if (!EolToken.HasValue)
				return Maybe<Token>.NoValue;
			if (tokenBeforeNewline.TypeInt == EolToken.Value.TypeInt || tokenAfterNewline.Or(default(Token)).TypeInt == (int)TokenType.Colon)
				return Maybe<Token>.NoValue;
			else {
				ErrorSink.Write(Severity.Warning, tokenBeforeNewline, "Expected ';' here. Proceeding as if the semicolon were present.");
				return EolToken.Value.WithStartIndex(tokenBeforeNewline.EndIndex);
			}
		}
	}
}
