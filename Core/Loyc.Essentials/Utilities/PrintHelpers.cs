using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Numerics;
using Loyc.Math;
using Loyc.Threading;

namespace Loyc.Syntax
{
	/// <summary>Static methods that help to print literals, such as 
	/// <see cref="EscapeCStyle"/> which escapes special characters with backslashes.</summary>
	public static class PrintHelpers
	{
		/// <summary>Gets the hex digit character for the specified value, 
		/// or '?' if the value is not in the range 0...15. Uses uppercase.</summary>
		public static char HexDigitChar(int value)
		{
			if ((uint)value < 10)
				return (char)('0' + value);
			else if ((uint)value < 16)
				return (char)('A' - 10 + value);
			else
				return '?';
		}

		/// <summary>Escapes characters in a string using C style, e.g. the string 
		/// <c>"Foo\"\n"</c> maps to <c>"Foo\\\"\\\n"</c> by default.</summary>
		public static string EscapeCStyle(UString s, EscapeC flags = EscapeC.Default)
		{
			return EscapeCStyle(s, flags, '\0');
		}
		/// <summary>Escapes characters in a string using C style.</summary>
		/// <param name="flags">Specifies which characters should be escaped.</param>
		/// <param name="quoteType">Specifies a character that should always be 
		/// escaped (typically one of <c>' " `</c>)</param>
		public static string EscapeCStyle(UString s, EscapeC flags, char quoteType)
		{
			StringBuilder s2 = new StringBuilder(s.Length+1);
			bool usedEscapes = false, fail;
			for (;;) {
				int c = s.PopFirst(out fail);
				if (fail) break;
				usedEscapes |= EscapeCStyle(c, s2, flags, quoteType);
			}
			if (!usedEscapes && s.InternalString?.Length == s.Length)
				return s.InternalString;
			return s2.ToString();
		}

		static void EscapeU(int c, StringBuilder @out, EscapeC flags)
		{
			if (c <= 255 && (flags & EscapeC.BackslashX) != 0) {
				@out.Append(@"\x");
			} else if (c > 0xFFFF || (flags & EscapeC.HasLongEscape) != 0) {
				@out.Append(@"\U");
				Debug.Assert(c <= 0x10FFFF);
				@out.Append(HexDigitChar((c >> 20) & 0xF));
				@out.Append(HexDigitChar((c >> 16) & 0xF));
				@out.Append(HexDigitChar((c >> 12) & 0xF));
				@out.Append(HexDigitChar((c >> 8) & 0xF));
			} else {
				@out.Append(@"\u");
				@out.Append(HexDigitChar((c >> 12) & 0xF));
				@out.Append(HexDigitChar((c >> 8) & 0xF));
			}
			@out.Append(HexDigitChar((c >> 4) & 0xF));
			@out.Append(HexDigitChar(c & 0xF));
		}

		/// <summary>Writes a character <c>c</c> to a StringBuilder, either as a normal 
		/// character or as a C-style escape sequence.</summary>
		/// <param name="flags">Specifies which characters should be escaped.</param>
		/// <param name="quoteType">Specifies a character that should always be 
		/// escaped (typically one of <c>' " `</c>)</param>
		/// <returns>true if an escape sequence was emitted, false if not.</returns>
		/// <remarks><see cref="EscapeC.HasLongEscape"/> can be used to output a 
		/// 6-digit unicode escape. <see cref="EscapeC.UnicodeNonCharacters"/> causes
		/// individual surrogate code units to be escaped, but a combined character 
		/// like U+1F300 is not escaped.</remarks>
		public static bool EscapeCStyle(int c, StringBuilder @out, EscapeC flags = EscapeC.Default, char quoteType = '\0')
		{
			for(;;) {
				if (c >= 128) {
					if ((flags & EscapeC.NonAscii) != 0) {
						EscapeU(c, @out, flags);
					} else if (c >= 0xD800) {
						if ((flags & EscapeC.UnicodeNonCharacters) != 0 && (
							c >= 0xFDD0 && c <= 0xFDEF || // 0xFDD0...0xFDEF 
							(c & 0xFFFE) == 0xFFFE) || // 0xFFFE, 0xFFFF, 0x1FFFE, 0x1FFFF, etc.
							(c & 0xF800) == 0xD800) { // 0xD800...0xDFFF 
							EscapeU(c, @out, flags);
						} else if ((flags & EscapeC.UnicodePrivateUse) != 0 && (
							c >= 0xE000 && c <= 0xF8FF ||
							c >= 0xF0000 && c <= 0xFFFFD ||
							c >= 0x100000 && c <= 0x10FFFD)) {
							EscapeU(c, @out, flags);
						} else
							break;
					} else
						break;
				} else if (c < 32) {
					if (c == '\n')
						@out.Append(@"\n");
					else if (c == '\r')
						@out.Append(@"\r");
					else if (c == '\0')
						@out.Append(@"\0");
					else {
						if ((flags & EscapeC.ABFV) != 0) {
							if (c == '\a') { // 7 (alert)
								@out.Append(@"\a");
								return true;
							}
							if (c == '\b') { // 8 (backspace)
								@out.Append(@"\b");
								return true;
							}
							if (c == '\f') { // 12 (form feed)
								@out.Append(@"\f");
								return true;
							}
							if (c == '\v') { // 11 (vertical tab)
								@out.Append(@"\v");
								return true;
							}
						}
						if ((flags & EscapeC.Control) != 0) {
							if (c == '\t')
								@out.Append(@"\t");
							else
								EscapeU(c, @out, flags);
						} else
							@out.Append(c);
					}
				} else if (c == '\"' && (flags & EscapeC.DoubleQuotes) != 0) {
					@out.Append("\\\"");
				} else if (c == '\'' && (flags & EscapeC.SingleQuotes) != 0) {
					@out.Append("\\'");
				} else if (c == '\\')
					@out.Append(@"\\");
				else
					break;
				return true;
			}

			if (c == quoteType) {
				@out.Append('\\');
				@out.Append((char)c);
				return true;
			} else 
				@out.AppendCodePoint(c);
			return false;
		}

		/// <summary>Converts an integer to a string, optionally with separator characters for readability.</summary>
		/// <param name="value">Integer to be converted</param>
		/// <param name="prefix">A prefix to insert before the number, but after the '-' sign, if any (e.g. "0x" for hex). Use "" for no prefix.</param>
		/// <param name="base">Number base (e.g. 10 for decimal, 2 for binary, 16 for hex). Must be in the range 2 to 36.</param>
		/// <param name="separatorInterval">Number of digits in a group (use 0 or less to disable digit separators)</param>
		/// <param name="separatorChar">Digit group separator</param>
		/// <returns>The number as a string.</returns>
		/// <remarks>Example: <c>IntegerToString(-1234567, "0", 10, 3, '\'') == "-01'234'567"</c></remarks>
		public static string IntegerToString(long value, string prefix = "", int @base = 10, int separatorInterval = 3, char separatorChar = '_')
		{
			return AppendIntegerTo(new StringBuilder(), value, prefix, @base, separatorInterval, separatorChar).ToString();
		}
		public static string IntegerToString(ulong value, string prefix = "", int @base = 10, int separatorInterval = 3, char separatorChar = '_')
		{
			return AppendIntegerTo(new StringBuilder(), value, prefix, @base, separatorInterval, separatorChar).ToString();
		}

		/// <summary>Same as <see cref="IntegerToString(long, string, int, int, char)"/> 
		/// except that the target StringBuilder must be provided as a parameter.</summary>
		/// <param name="value">Integer to be converted</param>
		/// <param name="prefix">A prefix to insert before the number, but after the '-' sign, if any (e.g. "0x" for hex). Use "" for no prefix.</param>
		/// <param name="base">Number base (e.g. 10 for decimal, 2 for binary, 16 for hex). Must be in the range 2 to 36.</param>
		/// <param name="separatorInterval">Number of digits in a group (use 0 or less to disable digit separators)</param>
		/// <param name="separatorChar">Digit group separator</param>
		/// <returns>The target StringBuilder.</returns>
		public static StringBuilder AppendIntegerTo(StringBuilder target, long value, string prefix = "", int @base = 10, int separatorInterval = 3, char separatorChar = '_')
		{
			if (value < 0) {
				CheckParam.IsInRange("base", @base, 2, 36);
				target.Append('-');
				target.Append(prefix);
				return AppendIntegerTo(target, (ulong)-value, "", @base, separatorInterval, separatorChar);
			} else 
				return AppendIntegerTo(target, (ulong)value, prefix, @base, separatorInterval, separatorChar);
		}

		public static StringBuilder AppendIntegerTo(StringBuilder target, ulong value, string prefix = "", int @base = 10, int separatorInterval = 3, char separatorChar = '_')
		{
			CheckParam.IsInRange("base", @base, 2, 36);
			target.Append(prefix);
			int iStart = target.Length;
			int counter = 0;
			int shift = @base > 8 ? (@base <= 16 ? 4 : 5)
				  : (@base <= 2 ? 1 : @base <= 4 ? 2 : 3);
			int mask = (1 << shift == @base ? (1 << shift) - 1 : 0);
			for (;;) {
				uint digit;
				if (mask != 0) {
					digit = (uint)value & (uint)mask;
					value >>= shift;
				} else {
					digit = (uint)(value % (uint)@base);
					value /= (uint)@base;
				}
				target.Append(HexDigitChar((int)digit));
				if (value == 0)
					break;
				if (++counter == separatorInterval) {
					counter = 0;
					target.Append(separatorChar);
				}
			}

			// Reverse the appended characters
			for (int i = ((target.Length - iStart) >> 1) - 1; i >= 0; i--)
			{
				int i1 = iStart + i, i2 = target.Length - 1 - i;
				char temp = target[i1];
				target[i1] = target[i2];
				target[i2] = temp;
			}
			return target;
		}
	}

	/// <summary>Flags to control <seealso cref="PrintHelpers.EscapeCStyle(UString, EscapeC)"/>
	/// and the reverse operation in ParseHelpers.</summary>
	[Flags()]
	public enum EscapeC
	{
		/// <summary>Only \r, \n, \0 and backslash are escaped.</summary>
		Minimal = 0,  
		/// <summary>Default option for escaping</summary>
		Default = Control | DoubleQuotes | UnicodeNonCharacters | UnicodePrivateUse,
		/// <summary>Escape ALL characters with codes above 127 as \xNN or \uNNNN</summary>
		NonAscii = 1,
		/// <summary>Use \xNN instead of \u00NN for characters 1-31 and 127-255</summary>
		BackslashX = 2,
		/// <summary>Escape all characters with codes below 32, including \t</summary>
		Control = 4, 
		/// <summary>Use \a \b \f and \v (rather than \xNN or \xNN)</summary>
		ABFV = 8,
		/// <summary>Escape double quotes as \"</summary>
		DoubleQuotes = 16, 
		/// <summary>Escape single quotes as \'</summary>
		SingleQuotes = 32, 
		/// <summary>Escape single and double quotes</summary>
		Quotes = 48,
		/// <summary>Escape non-character unicode code points such as the Byte Order Mark 
		/// and unpaired surrogate pair characters.</summary>
		UnicodeNonCharacters = 64,
		/// <summary>Escape unicode private-use code points.</summary>
		UnicodePrivateUse = 128,
		/// <summary>While unescaping, a backslash was encountered.</summary>
		HasEscapes = 0x100, 
		/// <summary>While unescaping, an unrecognized escape was encountered .</summary>
		Unrecognized = 0x200,
		/// <summary>While unescaping, a valid \U escape was encountered.</summary>
		HasLongEscape = 0x400,
		/// <summary>While unescaping, a valid \U escape was encountered with 6 digits, but the
		/// number was more than 0x10FFFF and had to be treated as 5 digits to make it valid.</summary>
		/// <remarks>Always appears with HasLongEscape | HasEscapes</remarks>
		HasInvalid6DigitEscape = 0x800,
	}
}
