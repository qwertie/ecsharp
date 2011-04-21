namespace Loyc.CompilerCore
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Diagnostics;
	using Loyc.Math;
	using Loyc.Utilities;
	using Loyc.Essentials;
	using Loyc.Collections;
	using NUnit.Framework;

	/// <summary>
	/// Encapsulates a set of parsers for common token types.
	/// </summary>
	public class ParseToken
	{
		protected const char EOF = '\uFFFF';
		static protected bool IsNEWLINE_CHAR(char LA) { return LA == '\n' || LA == '\r'; }
		static protected bool IsWS_CHAR(char LA) { return LA == ' ' || LA == '\t'; }
		static protected bool IsLETTER_CHAR(char LA) { return (LA >= 'A' && LA <= 'Z') || (LA >= 'a' && LA <= 'z') || (LA >= '\u0080' && Char.IsLetter((char)LA)); }
		static protected bool IsDIGIT_CHAR(char LA) { return LA >= '0' && LA <= '9'; }

		static public object Parse(Symbol type, ICharSource source, ref int position)
		{
			throw new NotImplementedException();
		}

		/// <summary>Parses a string.</summary>
		/// <returns>The parsed string.</returns>
		/// <remarks>Throws CompilerException on error. Newline (in a single-quoted 
		/// string) or EOF would be considered an error; a bad escape sequence is 
		/// not.</remarks>
		static public string ParseString(ICharSource source, ref int pos)
		{
			StringBuilder sb = new StringBuilder();
			CompilerMsg msg = ParseString(source, ref pos, int.MaxValue, "", false, sb);
			if (msg != null && msg.IsError)
				throw msg.ToException();
			return sb.ToString();
		}

		static public string ParseString(ICharSource source, ref int pos, out CompilerMsg msg)
		{
			StringBuilder sb = new StringBuilder();
			msg = ParseString(source, ref pos, int.MaxValue, "", false, sb);
			return sb.ToString();
		}

		static public string ParseString(ICharSource source, ref int pos, int endAt, string indentToStrip, bool verbatim, out CompilerMsg msg)
		{
			StringBuilder sb = new StringBuilder();
			msg = ParseString(source, ref pos, endAt, indentToStrip, verbatim, sb);
			return sb.ToString();
		}

		/// <summary>Parses a string.</summary>
		/// <param name="source">File that contains a string</param>
		/// <param name="pos">Position of the beginning of the string</param>
		/// <param name="indentToStrip">If the string is triple-quoted and spans 
		/// multiple lines, characters that match this prefix are stripped from the 
		/// beginning of each line.</param>
		/// <param name="verbatim">Forces verbatim interpretation of a single-
		/// quoted string (i.e. escape sequences are not allowed). Without this 
		/// flag, a string is still considered verbatim if it begins with "@".</param>
		/// <param name="sb"></param>
		/// <returns>The parsed string.</returns>
		/// <remarks>Throws CompilerException on error. Newline (in a single-quoted 
		/// string) or EOF would be considered an error; a bad escape sequence is 
		/// not.</remarks>
		static public CompilerMsg ParseString(ICharSource source, ref int pos, int endAt, string indentToStrip, bool verbatim, StringBuilder sb)
		{
			// Determine the string type
			char quote = source.TryGet(pos, EOF);
			if (quote == '@') {
				verbatim = true;
				quote = source.TryGet(++pos, EOF);
			}
			if (quote == '"' || quote == '\'' || quote == '`' || quote == '/')
			{
				if (source.TryGet(pos + 1, EOF) == quote)
				{
					if (source.TryGet(pos + 2, EOF) == quote)
						return ParseString2(source, ref pos, endAt, quote, indentToStrip ?? "", verbatim, sb);
					else
						return null;
				}
				return ParseString2(source, ref pos, endAt, quote, null, verbatim, sb);
			}
			return CompilerMsg.Error(source.IndexToLine(pos), "String expected");
		}
		static private CompilerMsg ParseString2(ICharSource source, ref int pos, int endAt, char quote, string tqIndent, bool verbatim, StringBuilder sb)
		{
			pos += (tqIndent != null ? 3 : 1);
			CompilerMsg error = null;

			char c;
			int i;
			for (i = pos; (c = source.TryGet(i, EOF)) != EOF && i < endAt;) {
				if (c == '\\' && !verbatim)
					sb.Append(ParseCharEscape(source, ref i, ref error));
				else if (c == '\n' || c == '\r') {
					if (tqIndent == null) {
						pos = i;
						return CompilerMsg.Error(source.IndexToLine(i), "Newline in string constant");
					} else {
						sb.Append('\n');
						// Advance past CR, LF or CRLF
						if (c == '\n' || (c = source.TryGet(++i, EOF)) == '\n')
							c = source.TryGet(++i, EOF);

						for (int ind = 0; ind < tqIndent.Length && c == tqIndent[ind]; ind++)
							c = source.TryGet(++i, EOF);
					}
				} else if (c != quote) {
					sb.Append(c);
					i++;
				} else // c == quote
				{
					if (tqIndent == null)
					{
						pos = i + 1;
						return error; // end of string
					}
					else if (source.TryGet(i + 1, EOF) == quote && source.TryGet(i + 2, EOF) == quote)
					{
						pos = i + 3;
						return error; // end of triple-quoted string
					}
				}
			}
			error = CompilerMsg.Error(source.IndexToLine(pos), c == EOF ? 
					"End of file in string" : "Token ended unexpectedly");
			pos = i;
			return error;
		}

		/// <summary>Parses an escape sequence, and ignores any error.</summary>
		public static char ParseCharEscape(ICharSource source, ref int pos)
		{
			CompilerMsg warning = null;
			return ParseCharEscape(source, ref pos, ref warning);
		}

		/// <summary>Parses an escape sequence.</summary>
		/// <param name="error">A warning is placed in 'error' if the escape 
		/// sequence is not valid for a string.</param>
		/// <remarks>
		/// This method assumes source[pos] is a backslash; it does not check.
		/// <para/>
		/// In case of an invalid escape sequence, this method returns a backslash 
		/// and pos is increased by one.
		/// </remarks>
		public static char ParseCharEscape(ICharSource source, ref int pos, ref CompilerMsg error)
		{
			Debug.Assert(source[pos] == '\\');
			pos++;
			int code, digits;
			bool invalid = false;
			char c;
			switch (c = source.TryGet(pos, EOF)) {
			case 'u':
				digits = G.TryParseHex(source.Substring(pos + 1, 4), out code);
				if (digits > 0)
					c = (char)code;
				else
					invalid = true;
				pos += digits;
				break;
			case 'x':
				digits = G.TryParseHex(source.Substring(pos + 1, 2), out code);
				if (digits > 0)
					c = (char)code;
				else
					invalid = true;
				pos += digits;
				break;
			case '\\':
				c = '\\'; break;
			case '\"':
				c = '\"'; break;
			case '\'':
				c = '\''; break;
			case 'n':
				c = '\n'; break;
			case 'r':
				c = '\r'; break;
			case 't':
				c = '\t'; break;
			case 'a':
				c = '\a'; break;
			case 'b':
				c = '\b'; break;
			case 'f':
				c = '\f'; break;
			default:
				invalid = true; break;
			}
			
			if (invalid) {
				error = error ?? CompilerMsg.Warning(source.IndexToLine(pos - 1), "Invalid escape sequence");
				c = '\\';
			} else
				pos++;
			return c;
		}

		/// <summary>Parses string as a single character. Throws a CompilerException 
		/// if the string is more than a single character or has some other problem.</summary>
		static public char ParseChar(ICharSource source, ref int pos)
		{
			CompilerMsg msg;
			char c = ParseChar(source, ref pos, out msg);
			if (msg != null)
				throw msg.ToException();
			return c;
		}
		/// <summary>Parses string as a single character. Produces a CompilerMsg
		/// if the string is more than a single character or has some other 
		/// problem.</summary>
		static public char ParseChar(ICharSource source, ref int pos, out CompilerMsg error)
		{
			char quote = source.TryGet(pos, EOF), c;
			if ((quote == '"' || quote == '\'') && source.TryGet(pos + 2, EOF) == quote &&
				(c = source.TryGet(pos + 1, EOF)) != '\\' && !IsNEWLINE_CHAR(c) && c != quote)
			{	// Optimize the common case
				error = null;
				return c;
			} else {
				StringBuilder sb = new StringBuilder(1);
				int start = pos;
				error = ParseString(source, ref pos, int.MaxValue, "", false, sb);
				if (error == null && sb.Length != 1)
					error = CompilerMsg.Error(source.IndexToLine(start), "Single character expected");
				if (sb.Length == 0)
					return '\0';
				return sb[0];
			}
		}

		/// <summary>Parses a Loyc or C# identifier, converting, for example, @int 
		/// to int and \"+" to +.</summary>
		/// <param name="sourceText">Original text from source code</param>
		/// <returns>The ID after parsing</returns>
		/// <remarks>
		/// Loyc places no inherent restrictions on the characters of an identifier.
		/// Identifiers in source code can contain punctuation and other exotic 
		/// characters using a special syntax, although not all characters are 
		/// valid when it comes time to compile the code into an assembly.
		/// <para/>
		/// This method recognizes three syntaxes for identifiers that have special
		/// names or contain special characters:
		/// <para/>
		/// (1) @notation: Microsoft invented this syntax for C#. An "@" sign at 
		///     the beginning of an identifier allows a normal identifier to have 
		///     the same name as a keyword, e.g. @int. This function will simply 
		///     remove the "@" sign.
		/// (2) \"notation": If a keyword begins with \" and ends with a non-
		///     escaped quote, this method will parse the contents like a C-style
		///     string.
		/// (3) nota\+tion: If the identifier does not follow the first two 
		///     notations, this method will parse the entire string like a C
		///     string, removing backslashes and handling character escapes like
		///     \xA9 and \u00A9. Even spaces can be escaped.
		/// <para/>
		/// Be sure to strip any leading or trailing spaces before calling 
		/// ParseID, otherwise the first two cases will not be recognized.
		/// <para/>
		/// This method assumes the entire input string is an identifier, and 
		/// will not throw an exception if the syntax is not normal.
		/// </remarks>
		public static string ParseID(string sourceText)
		{
			if (string.IsNullOrEmpty(sourceText))
				return sourceText;
			if (sourceText[0] == '@')
				return sourceText.Substring(1);
			if (sourceText[sourceText.Length - 1] == '\"' &&
				sourceText.StartsWith("\\\"") &&
				sourceText[sourceText.Length - 2] != '\\')
				return G.UnescapeCStyle(sourceText, 2, sourceText.Length - 3, true);
			for (int i = 0; i < sourceText.Length; i++)
				if (sourceText[i] == '\\')
					return G.UnescapeCStyle(sourceText);
			return sourceText;
		}

		/// <summary>Parses a Loyc or C# identifier, converting, for example, @int 
		/// to int and \"+" to +.</summary>
		/// <param name="source">File that contains the identifier</param>
		/// <param name="pos">Position in source of the beginning of the identifier;
		/// on exit, source[pos] is the character after the end of the identifier</param>
		/// <remarks>
		/// Unlike the ParseID(string) overload, this method detects the end of the 
		/// identifier, and will throw an exception in case of an error like an EOF 
		/// or newline in an ID that was expected to use \"notation". It won't 
		/// throw an exception just for an invalid escape sequence.
		/// </remarks>
		public static string ParseID(ICharSource source, ref int pos)
		{
			StringBuilder sb = new StringBuilder();
			CompilerMsg error = ParseID(source, ref pos, int.MaxValue, sb);
			if (error != null && error.IsError)
				throw error.ToException();
			return sb.ToString();
		}

		/// <summary>Parses a Loyc or C# identifier, converting, for example, @int 
		/// to int and \"+" to +.</summary>
		/// <param name="source">File that contains the identifier</param>
		/// <param name="pos">Position in source of the beginning of the identifier;
		/// on exit, source[pos] is the character after the end of the identifier</param>
		/// <param name="error">An error or warning that occurred while parsing</param>
		/// <remarks>
		/// Unlike the ParseID(string) overload, this method detects the end of the 
		/// identifier, and will save an error or warning if there is a problem with
		/// the input.
		/// </remarks>
		public static string ParseID(ICharSource source, ref int pos, out CompilerMsg error)
		{
			StringBuilder sb = new StringBuilder();
			error = ParseID(source, ref pos, int.MaxValue, sb);
			return sb.ToString();
		}

		/// <summary>Parses a Loyc or C# identifier, converting, for example, @int 
		/// to int and \"+" to +.</summary>
		/// <param name="pos">Position in source of the beginning of the identifier;
		/// on exit, source[pos] is the character after the end of the identifier</param>
		/// <param name="sb">The parsed identifier is appended to this StringBuilder.</param>
		public static CompilerMsg ParseID(ICharSource source, ref int pos, int endAt, StringBuilder sb)
		{
			char c = source.TryGet(pos, EOF);
			if (c == '\\' && source.TryGet(pos + 1, EOF) == '"')
			{
				pos++;
				return ParseString(source, ref pos, endAt, "", false, sb);
			}
			else
			{
				if (c == '@')
					c = source.TryGet(++pos, EOF);
				for (; pos < endAt; c = source.TryGet(++pos, EOF))
				{
					if (c == '\\') {
						int oldPos = pos;
						c = ParseCharEscape(source, ref pos);
						if (pos == oldPos + 1) {
							// Not a valid escape sequence for a STRING, but it's
							// probably fine for an ID token. Just ignore the \ and 
							// include the character after it, as long as it's not
							// a newline or space...
							Debug.Assert(c == '\\');
							c = source.TryGet(pos, EOF);
							Debug.Assert(c != '\\');
							if (c == ' ' || IsNEWLINE_CHAR(c))
							{
								pos--;
								break; // ID token ends at the backslash
							}
						} else
							pos--;
						sb.Append(c);
					} else if (IsLETTER_CHAR(c) || IsDIGIT_CHAR(c) || c == '_') {
						sb.Append(c);
					} else // End of ID token
						break;
				}
				return null;
			}
		}

		static public int ParseInt(ICharSource source, ref int pos)
		{
			CompilerMsg msg;
			int result = ParseInt(source, ref pos, int.MaxValue, false, out msg);
			if (msg != null && msg.IsError)
				throw msg.ToException();
			return result;
		}

		/// <summary>Parses a 32-bit integer, allowing for underscores for digit
		/// grouping. Supports VB hex notation (&amp;H1234) as well as C notation 
		/// (0x1234). Stops at, and ignores, the type suffix, if any.</summary>
		/// <remarks>An error is provided if the number overflows 32 bits, but
		/// a warning is used instead if the number was signed when an the 
		/// 'unsigned' flag is true, or if the number exceeds int.MaxValue bits 
		/// when the 'unsigned' flag is false.</remarks>
		public static int ParseInt(ICharSource source, ref int pos, out CompilerMsg error)
		{
			return ParseInt(source, ref pos, int.MaxValue, false, out error);
		}
		public static int ParseInt(ICharSource source, ref int pos, int endAt, bool unsigned, out CompilerMsg error)
		{
			int startPos = pos;
			long result = ParseLong(source, ref pos, int.MaxValue, unsigned, out error);
			if (!MathEx.IsInRange(result, (long)int.MinValue, (long)uint.MaxValue))
				error = CompilerMsg.Error(source.IndexToLine(startPos), "This number is larger than 32 bits");
			else if (unsigned) {
				if (result < 0)
					error = CompilerMsg.Warning(source.IndexToLine(startPos), "Expected an unsigned number");
			} else {
				if (result > int.MaxValue)
					error = CompilerMsg.Warning(source.IndexToLine(startPos), "This number is larger than 31 bits, and must be considered unsigned");
			}
			return (int)result;
		}

		public static long ParseLong(ICharSource source, ref int pos, out CompilerMsg error)
		{
			return ParseLong(source, ref pos, int.MaxValue, false, out error);
		}

		/// <summary>Parses an integer, allowing for underscores for digit 
		/// grouping. Supports VB hex notation (&amp;H1234) as well as C notation 
		/// (0x1234). Stops at, and ignores, the type suffix, if any.</summary>
		/// <remarks>
		/// Expected syntax: 
		/// '-'?
		/// (('0' 'x' | '&' 'H') (HEX_DIGIT | (HEX_DIGIT '_'?) HEX_DIGIT
		/// |                    (DIGIT | (DIGIT '_'?)* DIGIT)
		/// )
		/// </remarks>
		public static long ParseLong(ICharSource source, ref int pos, int endAt, bool unsigned, out CompilerMsg error)
		{
			int startPos = pos;
			error = null;
			
			long result;
			bool negative = false;
			char c = source.TryGet(pos, EOF);
			if (c == '-') {
				c = source.TryGet(++pos, EOF);
				negative = true;
				if (unsigned)
					error = CompilerMsg.Warning(source.IndexToLine(startPos), "Expected an unsigned number");
			}
			if (c == '&') {
				// VB style?
				if ((c = source.TryGet(pos + 1, EOF)) == 'H' || c == 'h') {
					pos += 2;
					result = ParseHex(source, ref pos, endAt, ref error);
					goto finish;
				}
			} else if (c >= '0' && c <= '9') {
				if (c == '0') {
					if ((c = source.TryGet(pos + 1, EOF)) == 'X' || c == 'x') {
						pos += 2;
						result = ParseHex(source, ref pos, endAt, ref error);
						goto finish;
					} else
						c = '0';
				}
				result = ParseDec(source, ref pos, endAt, ref error);
				goto finish;
			}
			error = _expectedNumber;
			result = 0;

		finish:
			if (negative)
			{
				if (result < 0)
					// 63-bit overflow, but with minus sign it's a 64-bit overflow
					error = CompilerMsg.Error(source.IndexToLine(startPos), "This number is larger than 64 bits");
				result = -result;
			}
			if (error == _signedOverflow && unsigned)
				error = null;
			if (error == _expectedNumber || error == _signedOverflow)
				error = error.With(source.IndexToLine(startPos));
			return result;
		}

		// Used when a number overflows exceeds long.MaxValue but not ulong.MaxValue
		static readonly CompilerMsg _signedOverflow = CompilerMsg.Warning(SourcePos.Nowhere, "This number is larger than 63 bits, and must be considered unsigned");
		static readonly CompilerMsg _expectedNumber = CompilerMsg.Error(SourcePos.Nowhere, "Expected a number");

		private static long ParseDec(ICharSource source, ref int pos, int endAt, ref CompilerMsg error)
		{
			int startPos = pos;
			char c = source.TryGet(pos, EOF);
			if (!IsDIGIT_CHAR(c) || pos > endAt) {
				error = _expectedNumber;
				return 0;
			}
			ulong result = (ulong)(c - '0');
			bool lastUnderscore = false;
			while (++pos < endAt) {
				c = source.TryGet(pos, EOF);
				if (!IsDIGIT_CHAR(c)) {
					if (lastUnderscore)
						break;
					if (c == '_') {
						lastUnderscore = true;
						continue;
					} else
						break;
				} else {
					int digit = c - '0';
					if (error == null)
						try {
							result = checked((result * 10) + (uint)digit);
						} catch(OverflowException) {
							error = CompilerMsg.Error(source.IndexToLine(startPos), "This number is larger than 64 bits");
						}
					if (error != null)
						result = (result * 10) + (uint)digit;
				}
				lastUnderscore = false;
			}
			if (lastUnderscore)
				pos--;
			if (result > (ulong)long.MaxValue)
				error = error ?? _signedOverflow;
			return (long)result;
		}

		private static long ParseHex(ICharSource source, ref int pos, int endAt, ref CompilerMsg error)
		{
			int startPos = pos;
			char c = source.TryGet(pos, EOF);
			int digit = G.HexDigitValue(c);
			if (digit < 0 || pos > endAt) {
				error = _expectedNumber;
				return 0;
			}
			ulong result = (ulong)digit;
			int numDigits = 1;
			bool lastUnderscore = false;
			while (++pos < endAt) {
				c = source.TryGet(pos, EOF);
				digit = G.HexDigitValue(c);
				if (digit < 0) {
					if (lastUnderscore)
						break;
					if (c == '_') {
						lastUnderscore = true;
						continue;
					} else
						break;
				} else {
					result = (result << 4) + (uint)digit;
					if (++numDigits == 17)
						error = CompilerMsg.Error(source.IndexToLine(startPos), "This number is larger than 64 bits");
				}
				lastUnderscore = false;
			}
			if (lastUnderscore)
				pos--;
			if (result > (ulong)long.MaxValue)
				error = error ?? _signedOverflow;
			return (long)result;
		}

		public static int SkipWhitespace(ICharSource source, ref int pos, bool stopAtNewline)
		{
			for (int count = 0; ; count++, pos++)
			{
				char c = source.TryGet(pos, EOF);
				if (!IsWS_CHAR(c) && (stopAtNewline || !IsNEWLINE_CHAR(c)))
					return count;
			}
		}
	}

	[TestFixture]
	public class ParseTokenTests
	{
		// TODO: tests that throw exceptions

		[Test]
		public void ParseStrings()
		{
			CompilerMsg msg;

			int pos = 0;
			Assert.AreEqual("sq", ParseToken.ParseString(S("'sq'"), ref pos));
			Assert.AreEqual(4, pos);
			
			pos = 2;
			Assert.AreEqual("?", ParseToken.ParseString(S(@"\/'?'\/"), ref pos));
			Assert.AreEqual(5, pos);
			
			pos = 1;
			Assert.AreEqual("a A", ParseToken.ParseString(S("<\"a A\">"), ref pos, out msg));
			Assert.AreEqual(null, msg);
			Assert.AreEqual(6, pos);

			pos = 0;
			Assert.AreEqual("hello", ParseToken.ParseString(S("'''hello'''''"), ref pos, out msg));
			Assert.AreEqual(null, msg);
			Assert.AreEqual(11, pos);

			pos = 0;
			Assert.AreEqual("'\n'", ParseToken.ParseString(S("```'\n'```"), ref pos, out msg));
			Assert.AreEqual(msg, null);
			Assert.AreEqual(9, pos);

			pos = 0;
			ICharSource input = S("\"\"\" A \n   B \"\"\"");
			string output = ParseToken.ParseString(input, ref pos, int.MaxValue, "  ", false, out msg);
			Assert.AreEqual(" A \n B ", output);
			Assert.AreEqual(null, msg);
			Assert.AreEqual(input.Count, pos);

			pos = 0;
			input = S(@"`Wh\y`");
			output = ParseToken.ParseString(input, ref pos, int.MaxValue, null, false, out msg);
			Assert.AreEqual(@"Wh\y", output);
			Assert.That(msg != null && msg.Type == CompilerMsg._Warning);
			Assert.AreEqual(input.Count, pos);

			pos = 0;
			input = S(@"'G\u\n'");
			output = ParseToken.ParseString(input, ref pos, int.MaxValue, null, true, out msg);
			Assert.AreEqual(@"G\u\n", output);
			Assert.AreEqual(null, msg);
			Assert.AreEqual(input.Count, pos);

			pos = 1;
			input = S("_\"Testing");
			output = ParseToken.ParseString(input, ref pos, int.MaxValue, null, true, out msg);
			Assert.AreEqual(@"Testing", output);
			Assert.That(msg != null && msg.IsError);
			Assert.AreEqual(input.Count, pos);

			pos = 0;
			input = S("\"hop\n on pop");
			output = ParseToken.ParseString(input, ref pos, int.MaxValue, null, false, out msg);
			Assert.AreEqual(@"hop", output);
			Assert.That(msg != null && msg.IsError);
			Assert.AreEqual(4, pos);

			pos = 0;
			output = ParseToken.ParseString(S("\"hi\""), ref pos, 3, null, true, out msg);
			Assert.AreEqual(@"hi", output);
			Assert.AreEqual(3, pos);
			Assert.That(msg != null && msg.IsError);

			pos = 0;
			output = ParseToken.ParseString(S("'''\"\"\"'''"), ref pos, 9, null, true, out msg);
			Assert.AreEqual("\"\"\"", output);
			Assert.AreEqual(9, pos);
			Assert.AreEqual(null, msg);
		}

		private ICharSource S(string text)
		{
			return new StringCharSource(text);
		}

		[Test]
		public void ParseIDs()
		{
			CompilerMsg msg;

			int pos = 0;
			Assert.AreEqual("f", ParseToken.ParseID(S("f"), ref pos));
			Assert.AreEqual(1, pos);
			pos = 0;
			Assert.AreEqual("foo", ParseToken.ParseID(S("@foo"), ref pos));
			Assert.AreEqual(4, pos);
			pos = 0;
			Assert.AreEqual("foo", ParseToken.ParseID(S("\\\"foo\""), ref pos));
			Assert.AreEqual(6, pos);
			pos = 0;
			Assert.AreEqual("y", ParseToken.ParseID(S("\\y"), ref pos));
			Assert.AreEqual(2, pos);
			pos = 1;
			Assert.AreEqual("art", ParseToken.ParseID(S("Bart"), ref pos));
			Assert.AreEqual(4, pos);
			pos = 3;
			Assert.AreEqual("oops", ParseToken.ParseID(S("...\\\"oops"), ref pos, out msg));
			Assert.AreEqual(9, pos);
			Assert.That(msg != null && msg.IsError);
			
			pos = 1;
			Assert.AreEqual("Can't", ParseToken.ParseID(S(@"<Can\'t\ escape>"), ref pos, out msg));
			Assert.AreEqual(7, pos);
			Assert.That(msg == null);

			StringBuilder output = new StringBuilder();
			pos = 1;
			Assert.AreEqual(null, ParseToken.ParseID(S(@"<Can\'t\ escape>"), ref pos, 4, output));
			Assert.AreEqual("Can", output.ToString());
			Assert.AreEqual(4, pos);
		}

		[Test]
		public void ParseInts()
		{
			CompilerMsg msg;

			int pos = 1;
			Assert.AreEqual(0, ParseToken.ParseInt(S("< 9 >"), ref pos, out msg));
			Assert.AreEqual(pos, 1);
			Assert.That(msg != null && msg.IsError);
			pos = 1;
			Assert.AreEqual(234, ParseToken.ParseInt(S("12_34"), ref pos, out msg));
			Assert.That(msg == null && pos == 5);
			pos = 0;
			Assert.AreEqual(-1234, ParseToken.ParseInt(S("-1234"), ref pos, out msg));
			Assert.That(msg == null && pos == 5);
			pos = 0;
			Assert.AreEqual(-1234, ParseToken.ParseInt(S("-1234"), ref pos, 99, true, out msg));
			Assert.AreEqual(5, pos);
			Assert.That(msg != null && !msg.IsError);
			pos = 0;
			Assert.AreEqual(-0x12345678, ParseToken.ParseInt(S("-0x12345678"), ref pos, out msg));
			Assert.AreEqual(11, pos);
			Assert.That(msg == null);
			pos = 0;
			Assert.AreEqual(-0x80000000, ParseToken.ParseInt(S("-0x80000000"), ref pos, out msg));
			Assert.AreEqual(11, pos);
			Assert.That(msg == null);
			pos = 0;
			Assert.AreEqual(unchecked((int)0x80000000), ParseToken.ParseInt(S("0x80000000!"), ref pos, out msg));
			Assert.AreEqual(10, pos);
			Assert.That(msg != null && !msg.IsError);
			pos = 0;
			Assert.AreEqual(unchecked((int)0xBAADF00D), ParseToken.ParseInt(S("0xBAAD_F00D!"), ref pos, 99, true, out msg));
			Assert.AreEqual(11, pos);
			Assert.That(msg == null);
			pos = 0;
			Assert.AreEqual(0x23456789, ParseToken.ParseInt(S("0x123456789"), ref pos, out msg));
			Assert.AreEqual(11, pos);
			Assert.That(msg != null && msg.IsError);
			pos = 0;
			Assert.AreEqual(unchecked((int)-0x80000001), ParseToken.ParseInt(S("-0x8000_0001"), ref pos, out msg));
			Assert.AreEqual(12, pos);
			Assert.That(msg != null && msg.IsError);
		}
		
		[Test]
		public void ParseLongs()
		{
			CompilerMsg msg;

			int pos = 0;
			Assert.AreEqual(0xE, ParseToken.ParseLong(S("0xEVIL"), ref pos, out msg));
			Assert.AreEqual(3, pos);
			Assert.That(msg == null);
			
			pos = 0;
			Assert.AreEqual(-1234, ParseToken.ParseLong(S("-1_2_3_4__"), ref pos, 99, true, out msg));
			Assert.AreEqual(8, pos);
			Assert.That(msg != null && !msg.IsError);
			
			pos = 1;
			Assert.AreEqual(0x1234, ParseToken.ParseLong(S("-&H1_2_3_4_\n"), ref pos, 99, true, out msg));
			Assert.AreEqual(10, pos);
			Assert.That(msg == null);

			pos = 4;
			Assert.AreEqual(0x1234567812345678, ParseToken.ParseLong(
				S("*** 0x1234567812345678 ***"), ref pos, out msg));
			Assert.AreEqual(22, pos);
			Assert.That(msg == null);
			
			pos = 0;
			ICharSource input = S("1_234_567_890_123_456_789");
			Assert.AreEqual(1234567890123456789, ParseToken.ParseLong(input, ref pos, out msg));
			Assert.AreEqual(input.Count, pos);
			Assert.That(msg == null);

			pos = 0;
			input = S("12345678901234567890"); // 63-bit overflow
			Assert.AreEqual(0xAB54A98CEB1F0AD2ul, (ulong)ParseToken.ParseLong(input, ref pos, out msg));
			Assert.AreEqual(input.Count, pos);
			Assert.That(msg != null && !msg.IsError);

			pos = 0;
			input = S("98765432109876543210..."); // 64-bit overflow
			Assert.AreEqual(0x5AA54D38E5267EEAul, (ulong)ParseToken.ParseLong(input, ref pos, out msg));
			Assert.AreEqual(input.Count-3, pos);
			Assert.That(msg != null && msg.IsError);

			pos = 0;
			input = S("0xF2345678_12345678");
			Assert.AreEqual(0xF234567812345678ul, (ulong)ParseToken.ParseLong(input, ref pos, out msg));
			Assert.AreEqual(input.Count, pos);
			Assert.That(msg != null && !msg.IsError);
			
			pos = 2;
			input = S("->0xF2345678_12345678");
			Assert.AreEqual(0xF234567812345678ul, (ulong)
				ParseToken.ParseLong(input, ref pos, int.MaxValue, true, out msg));
			Assert.AreEqual(input.Count, pos);
			Assert.That(msg == null);

			pos = 0;
			input = S("0x8_12345678_12345678");
			Assert.AreEqual(0x1234567812345678,
				ParseToken.ParseLong(input, ref pos, 99, true, out msg));
			Assert.AreEqual(input.Count, pos);
			Assert.That(msg != null && msg.IsError);

			pos = 0;
			input = S("-0xF2345678_12345678");
			Assert.AreEqual(unchecked(-(long)0xF234567812345678ul),
				ParseToken.ParseLong(input, ref pos, 99, false, out msg));
			Assert.AreEqual(input.Count, pos);
			Assert.That(msg != null && msg.IsError);
		}
	}
}
