using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Loyc;

namespace Loyc.Syntax
{
	using ParseFunc = Func<UString, Symbol, Either<object, LogMessage>>;
	using PrintFunc = Func<object, Symbol, Either<UString, LogMessage>>;

	/// <summary>
	/// A <see cref="LiteralHandlerTable"/> that is preinitialized with all standard 
	/// literal parsers and printers.
	/// </summary>
	/// <remarks>
	/// The following types are fully supported:
	/// <ul>
	/// <li>Int8 (type marker: _i8)			  </li>
	/// <li>Int16 (type marker: _i16)		  </li>
	/// <li>Int32 (type marker: _i32)		  </li>
	/// <li>Int64 (type markers: _i64, _L)	  </li>
	/// <li>UInt8 (type marker: _u8)		  </li>
	/// <li>UInt16 (type marker: _u16)		  </li>
	/// <li>UInt32 (type marker: _u32)		  </li>
	/// <li>UInt64 (type markers: _u64, _uL)  </li>
	/// <li>BigInteger (type marker: _z)	  </li>
	/// <li>Single (type markerS: _r32, _f)	  </li>
	/// <li>Double (type markerS: _r64, _d)	  </li>
	/// <li>Char (type marker: c)			  </li>
	/// <li>String (type marker: empty string)</li>
	/// <li>Symbol (type marker: s)			  </li>
	/// <li>Loyc.@void (type marker: void)	  </li>
	/// <li>Boolean (type marker: bool)		  </li>
	/// <li>UString (no specific type marker) </li>
	/// </ul>
	/// There are also two general type markers, _ for a number of unspecified type,
	/// and _u for an unsigned number of unspecified size. In LES, any type marker 
	/// that is not on this list is legal, but will be left uninterpreted by default;
	/// <see cref="LNode.Value"/> will return a string of type <see cref="UString"/>.
	/// <para/>
	/// The syntax corresponding to each type marker is standardized, meaning that 
	/// all implementations of LES2 and LES3 that can parse these types must do so 
	/// in exactly the same way.
	/// <para/>
	/// The Decimal and RegEx types have printers (with default type markers _m and 
	/// re) but no parser, because these type markers are not standardized.
	/// It's worth noting that regular expressions that are valid in one language 
	/// may be invalid in another, so to avoid parsing issues, re strings are not
	/// parsed to RegEx by default.
	/// <para/>
	/// The type marker "null" represents null, and the only valid value for the
	/// null and void type markers is an empty string (""). The only valid values
	/// for the bool type marker are "true" and "false" and case variations of
	/// these (e.g. "TRUE", "faLSE").
	/// <para/>
	/// Character literals are special in .NET because they parse into one of two 
	/// types, either Char or String, depending on whether the code point is less than
	/// 0x10000 or not. Code points of 0x10000 or greater, sometimes called "astral"
	/// characters, do not fit in the .NET Char type so String is used instead. The
	/// type marker "c" indicates that the literal is not really a String.
	/// The character parser does return an error if the input is not a single code 
	/// point (i.e. two code units are fine if and only if they are a surrogate pair).
	/// However, the printer keyed to type string doesn't care if the type marker is
	/// "c" or not.
	/// <para/>
	/// Type markers begin with an underscore (_) for numeric types only. The 
	/// underscore enables special syntax in LES2 and LES3. For example, in LES3, 
	/// 12345z is equivalent to _z"12345", but strings like re"123" can never be 
	/// printed in numeric form because their type marker does not start with an 
	/// underscore.
	/// <para/>
	/// The syntax of all integer types corresponds to the following case-
	/// insensitive regex:
	/// <code>
	///		/^[\-\u2212]?({Digits}|0x{HexDigits}|0b{BinDigits})$/
	///	</code>
	///	where {Digits} means "[_']*[0-9][0-9_']*", {HexDigits} means 
	///	"[_']*[0-9a-f][0-9a-f_']*", and {BinDigits} means "[_']*[01][01_']*".
	/// While negative numbers can be indicated with the usual dash character '-',
	/// the minus character '\x2212' is also allowed. Numbers cannot contain spaces.
	/// Parsers will fail in case of overflow, but a BigInteger cannot overflow.
	/// <para/>
	/// The syntax of a floating-point type corresponds to one of the following 
	/// case-insensitive regexes for decimal, hexadecimal, binary and non-numbers 
	/// respectively:
	/// <code>
	///		/^[\-\u2212]?({Digits}(\.[0-9_']*)?|\.{Digits})(e[+-]?{Digits})?$/
	///		/^[\-\u2212]?0b({BinDigits}(\.[01_']*)?|\.{BinDigits})(p[+-]?{BinDigits})?$/
	///		/^[\-\u2212]?0x({HexDigits}(\.[0-9a-f_']*)?|\.{HexDigits})(p[+-]?{HexDigits})?$/
	///		/nan|[\-\u2212]?inf/i
	///	</code>
	///	These require that any number contains at least one digit, but this digit can
	///	appear after the decimal point (.) if there is one. There can be any quantity
	///	of separator characters, but no spaces. The final regex allows NaN and 
	///	infinities to be parsed and printed.
	/// <para/>
	/// Given these patterns, integers are also detected as floating-point numbers. 
	/// If a floating-point number is printed without a type marker, or if the type 
	/// marker is _, the printer recognizes that it must add the suffix ".0" if 
	/// necessary so that the number will be treated as floating point when it is 
	/// parsed again later.
	/// <para/>
	/// Finally, I will point out that LES3 parsers and printers have some special 
	/// behavior related to string parsing and printing in environments that use
	/// UTF-16. There are two kinds of numeric escape sequences in LES3 strings and
	/// identifiers: two-digit sequences which look like \xFF, and unicode escape
	/// sequences with four to six digits, i.e. \u1234 or \U012345. LES3 strings 
	/// and identifiers must be interpreted as byte sequences, with \x escape 
	/// sequences representing raw bytes and \u escape sequences representing 
	/// proper UTF-8 characters. For UTF-16 environments like .NET, Loyc defines a 
	/// reversible (lossless) transformation from these byte sequences into UTF-16.
	/// \x sequences below \x80 are treated as normal ASCII characters, while \x 
	/// sequences above \x7F represent raw bytes that may or may not be valid 
	/// UTF-8. When a sequence is valid UTF-8, e.g. "\xE2\x80\xA2", it is 
	/// translated into the appropriate UTF-16 character (in this case "•" or 
	/// "\u2022"). Otherwise, the byte becomes an invalid (unmatched surrogate) 
	/// code unit in the range 0xDC80 to 0xDCFF, e.g. "\x99" becomes 0xDC99 
	/// inside a UTF-16 string. However, this implies that the UTF-8 byte sequence 
	/// "\xEF\xBF\xBD", which normally represents the same invalid surrogate 
	/// \uDC99, cannot also be translated to 0xDC99 in UTF-16. Instead it is 
	/// treated as three independent bytes which become a sequence of three 
	/// UTF-16 code units, 0xDCEF 0xDCBF 0xDCBD. Upon translation back to UTF-8,
	/// these become "\xEF\xBF\xBD" as expected. Furthermore, an LES3 ASCII 
	/// escape sequence like "\uDC99", which is equivalent to "\xEF\xBF\xBD",
	/// should actually produce three code units in a UTF-16 environment 
	/// (printed as "\uDCEF\uDCBF\uDCBD" in C# or Enhanced C#).
	/// <para/>
	/// However, none of this trickery necessarily needs to be handled by the 
	/// parsers and printers in this class, because the challenge appears at
	/// a different level. Namely, this trickery applies in the LES3 parser when 
	/// ASCII escape sequences are converted to invalid surrogates in UTF-16,
	/// which happens before the string reaches a parser in this class. Also,
	/// some trickery may be done by the LES3 printer after a string is printed 
	/// by this class. Finally, this special behavior applies only to UTF-16 
	/// environments (and UTF-8 environments like Rust that prohibit byte 
	/// sequences that are not valid UTF-8). No special-case code is necessary 
	/// in enviroments that use byte arrays for strings, because the purpose of
	/// this trickery is to allow LES3 strings to faithfully represent arbitrary 
	/// byte sequences in addition to Unicode strings.
	/// </remarks>
	/// <seealso cref="ParseHelpers"/>
	/// <seealso cref="PrintHelpers"/>
	public class StandardLiteralHandlers : LiteralHandlerTable
	{
		private static StandardLiteralHandlers _value = null;
		public static StandardLiteralHandlers Value => _value = _value ?? new StandardLiteralHandlers();

		protected static Symbol __u = GSymbol.Get("_u"); // uint or ulong
		protected static Symbol __i8 = GSymbol.Get("_i8"); // sbyte
		protected static Symbol __u8 = GSymbol.Get("_u8"); // byte
		protected static Symbol __i16 = GSymbol.Get("_i16"); // short
		protected static Symbol __u16 = GSymbol.Get("_u16"); // ushort
		protected static Symbol __i32 = GSymbol.Get("_i32"); // int
		protected static Symbol __u32 = GSymbol.Get("_u32"); // uint
		protected static Symbol __i64 = GSymbol.Get("_i64"); // long
		protected static Symbol __u64 = GSymbol.Get("_u64"); // ulong
		protected static Symbol __z = GSymbol.Get("_z"); // BigInt
		protected static Symbol __r32 = GSymbol.Get("_r32"); // float
		protected static Symbol __r64 = GSymbol.Get("_r64"); // double
		protected static Symbol __L = GSymbol.Get("_L"); // long
		protected static Symbol __uL = GSymbol.Get("_uL"); // ulong
		protected static Symbol __f = GSymbol.Get("_f"); // float
		protected static Symbol __d = GSymbol.Get("_d"); // double
		protected static Symbol _s = GSymbol.Get("s"); // symbol (any string is valid)
		protected static Symbol _void = GSymbol.Get("void"); // void""
		protected static Symbol _bool = GSymbol.Get("bool"); // bool"true" or bool"false" (case insensitive)
		protected static Symbol _c = GSymbol.Get("c"); // character (Char or Int32 with 21-bit UCS-4 value)
		protected static Symbol _number = GSymbol.Get("_"); // number without explicit type marker
		protected static Symbol _string = GSymbol.Empty; // string without explicit type marker
		protected static Symbol _re = GSymbol.Get("re"); // regular expression

		char _digitSeparator;
		/// <summary>Gets or sets a character used to separate groups of digits.
		/// It must be must be _ or ' or null, and it is inserted every 3 digits in 
		/// decimal numbers (e.g. 1_234_567), every 4 digits in hex numbers (e.g. 
		/// 0x1234_5678), or every 8 digits in binary numbers (e.g. 11_10111000).
		/// </summary>
		public char? DigitSeparator { 
			get => _digitSeparator == '\0' ? (char?)null : _digitSeparator;
			set {
				if (value != null && value != '\'' && value != '_')
					CheckParam.ThrowBadArgument("DigitSeparatorChar must be _ or ' or null");
				_digitSeparator = value ?? '\0';
			}
		}

		public StandardLiteralHandlers(char? digitSeparatorChar = '_')
		{
			AddStandardParsers();
			AddStandardPrinters();
		}

		private void AddStandardParsers()
		{
			ParseFunc i8  = (s, tm) => { return ParseSigned(s, out long n) ? ((sbyte)n == n ? OK((sbyte)n) : Overflow(s, tm)) : SyntaxError(s, tm); };
			ParseFunc u8  = (s, tm) => { return ParseSigned(s, out long n) ? ((byte)n == n ? OK((byte)n) : Overflow(s, tm)) : SyntaxError(s, tm); };
			ParseFunc i16 = (s, tm) => { return ParseSigned(s, out long n) ? ((short)n == n ? OK((short)n) : Overflow(s, tm)) : SyntaxError(s, tm); };
			ParseFunc u16 = (s, tm) => { return ParseSigned(s, out long n) ? ((ushort)n == n ? OK((ushort)n) : Overflow(s, tm)) : SyntaxError(s, tm); };
			ParseFunc i32 = (s, tm) => { return ParseSigned(s, out long n) ? ((int)n == n ? OK((int)n) : Overflow(s, tm)) : SyntaxError(s, tm); };
			ParseFunc u32 = (s, tm) => { return ParseSigned(s, out long n) ? ((uint)n == n ? OK((uint)n) : Overflow(s, tm)) : SyntaxError(s, tm); };
			ParseFunc i64 = (s, tm) => { return ParseSigned(s, out long n) ? OK(n) : SyntaxError(s, tm); };
			ParseFunc u64 = (s, tm) => { return ParseULong(s, out ulong n) ? OK(n) : SyntaxError(s, tm); };
			ParseFunc u   = (s, tm) => { return ParseULong(s, out ulong n) ? ((uint)n == n ? OK((uint)n) : OK((ulong)n)) : SyntaxError(s, tm); };
			ParseFunc big = (s, tm) => { return ParseBigInt(s, out BigInteger n) ? OK((object)n) : SyntaxError(s, tm); };
			ParseFunc f32 = (s, tm) => { return ParseDouble(s, out double n) ? OK((float)n) : SyntaxError(s, tm); };
			ParseFunc f64 = (s, tm) => { return ParseDouble(s, out double n) ? OK(n) : SyntaxError(s, tm); };

			AddParser(true, _string, (s, tm) => OK(s.ToString()));
			AddParser(true, _number, GeneralNumberParser);
			AddParser(true, __u, u);
			AddParser(true, __uL, u64);
			AddParser(true, __L, i64);
			AddParser(true, __f, f32);
			AddParser(true, __d, f64);
			AddParser(true, __u8, u8);
			AddParser(true, __i8, i8);
			AddParser(true, __u16, u16);
			AddParser(true, __i16, i16);
			AddParser(true, __u32, u32);
			AddParser(true, __i32, i32);
			AddParser(true, __u64, u64);
			AddParser(true, __i64, i64);
			AddParser(true, __r32, f32);
			AddParser(true, __r64, f64);
			AddParser(true, __z, big);
			AddParser(true, _s, (s, tm) => (Symbol)s);
			AddParser(true, _void, (s, tm) =>
				s.IsEmpty ? OK(G.BoxedVoid) : new LogMessage(Severity.Error, s, "void literal should be an empty string"));
			AddParser(true, (Symbol)"null", (s, tm) =>
				s.IsEmpty ? OK(null) : new LogMessage(Severity.Error, s, "null literal should be an empty string"));
			AddParser(true, _bool, (s, tm) => {
				if (s.Equals((UString)"true", true))
					return OK(G.BoxedTrue);
				if (s.Equals((UString)"false", true))
					return OK(G.BoxedFalse);
				return new LogMessage(Severity.Error, s, "bool literal should be 'true' or 'false'");
			});
			AddParser(true, _c, (s, tm) => {
				UString s2 = s;	
				int ch = s2.PopFirst(out bool fail);
				if (fail || s2.Length > 0)
					return new LogMessage(Severity.Error, s, "character literal should be a single character");
				return OK(ch < 0x10000 ? (char)ch : (object)s.ToString());
			});
		}

		private void AddStandardPrinters()
		{
			AddPrinter(true, typeof(sbyte), (lit, sb) =>
				{ PrintInteger((sbyte)lit.Value, lit.Style, sb); return __i8; });
			AddPrinter(true, typeof(byte), (lit, sb) =>
				{ PrintInteger((byte)lit.Value, lit.Style, sb); return __u8; });
			AddPrinter(true, typeof(short), (lit, sb) =>
				{ PrintInteger((short)lit.Value, lit.Style, sb); return __i16; });
			AddPrinter(true, typeof(ushort), (lit, sb) =>
				{ PrintInteger((ushort)lit.Value, lit.Style, sb); return __u16; });
			AddPrinter(true, typeof(int), (lit, sb) =>
				{ PrintInteger((int)lit.Value, lit.Style, sb); return _number; });
			AddPrinter(true, typeof(uint), (lit, sb) =>
				{ PrintInteger((uint)lit.Value, lit.Style, sb); return __u; });
			AddPrinter(true, typeof(long), (lit, sb) =>
				{ PrintInteger((long)lit.Value, lit.Style, sb); return lit.TypeMarker ?? __L; });
			AddPrinter(true, typeof(ulong), (lit, sb) =>
				{ PrintInteger((ulong)lit.Value, lit.Style, sb); return lit.TypeMarker ?? __uL; });
			// TODO: improve BigInteger support (ie. support hex & binary, support digit separators)
			AddPrinter(true, typeof(BigInteger), (lit, sb) =>
				{ sb.Append(((BigInteger)lit.Value).ToString()); return __z; });
			AddPrinter(true, typeof(float), (lit, sb) =>
				{ PrintFloat((float)lit.Value, lit.TypeMarker, lit.Style, false, sb); return __f; });
			AddPrinter(true, typeof(double), (lit, sb) =>
				{ PrintFloat((double)lit.Value, lit.TypeMarker, lit.Style, true, sb); return lit.TypeMarker ?? _number; });
			// output for void is intentionally left blank
			AddPrinter(true, typeof(@void), (lit, sb) => { return _void; });
			AddPrinter(true, typeof(bool), (lit, sb) =>
				{ sb.Append((bool)lit.Value ? "true" : "false"); return _bool; });
			AddPrinter(true, typeof(char), (lit, sb) =>
				{ sb.Append((char)lit.Value); return _c; });
			AddPrinter(true, typeof(string), (lit, sb) =>
				{ sb.Append((string)lit.Value); return _string; });
			AddPrinter(true, typeof(Symbol), (lit, sb) =>
				{ sb.Append(((Symbol)lit.Value).Name); return _s; });
			AddPrinter(true, typeof(UString), (lit, sb) =>
				{ sb.Append((UString)lit.Value); return lit.TypeMarker; });

			// NON-STANDARD TYPES

			// TODO: improve decimal support (we're just converting it to double right now)
			AddPrinter(true, typeof(decimal), (lit, sb) =>
				{ PrintFloat((double)(decimal)lit.Value, lit.TypeMarker, lit.Style, true, sb); return (Symbol)"_m"; });
			AddPrinter(true, typeof(System.Text.RegularExpressions.Regex), (lit, sb) =>
				{ sb.Append(lit.Value.ToString()); return _re; });
		}

		#region Parsing code

		static Either<object, LogMessage> OK(object result) => new Either<object, LogMessage>(result);

		static Either<object, LogMessage> SyntaxError(UString input, Symbol typeMarker)
		{
		
			return new LogMessage(Severity.Error, input, "Syntax error in '{0}' literal", typeMarker);
		}

		static Either<object, LogMessage> Overflow(UString input, Symbol typeMarker)
		{
			return new LogMessage(Severity.Error, input, "Number is out of range for its associated type");
		}

		static Either<object, LogMessage> GeneralNumberParser(UString s, Symbol tm)
		{
			long n;
			BigInteger z;
			double d;
			if (ParseSigned(s, out n))
			{
				if ((int)n == n)
					return (int)n;
				else if ((uint)n == n)
					return (uint)n;
				else
					return n;
			}
			else if (s.Length >= 18 && ParseBigInt(s, out z))
			{
				// (The length check is an optimization: the shortest number that 
				// does not fit in a long is 0x8000000000000000.)
				if (z >= Int64.MinValue && z <= UInt64.MaxValue)
					return z > Int64.MaxValue ? OK((ulong)z) : OK((long)z);
				return z;
			}
			else if (ParseDouble(s, out d))
			{
				return d;
			}
			else
				return SyntaxError(s, tm);
		}

		static bool ParseSigned(UString s, out long n)
		{
			n = 0;
			bool negative;
			int radix = GetSignAndRadix(ref s, out negative);
			if (radix == 0)
				return false;
			var flags = ParseNumberFlag.SkipSingleQuotes | ParseNumberFlag.SkipUnderscores | ParseNumberFlag.StopBeforeOverflow;
			ulong u;
			if (ParseHelpers.TryParseUInt(ref s, out u, radix, flags) && s.Length == 0)
			{
				n = negative ? -(long)u : (long)u;
				return (long)u >= 0;
			}
			return false;
		}

		static bool ParseBigInt(UString s, out BigInteger n)
		{
			n = BigInteger.Zero;
			bool negative;
			int radix = GetSignAndRadix(ref s, out negative);
			if (radix == 0)
				return false;
			var flags = ParseNumberFlag.SkipSingleQuotes | ParseNumberFlag.SkipUnderscores | ParseNumberFlag.StopBeforeOverflow;
			if (ParseHelpers.TryParseUInt(ref s, out n, radix, flags) && s.Length == 0)
			{
				if (negative) n = -n;
				return true;
			}
			return false;
		}

		static bool ParseULong(UString s, out ulong u)
		{
			u = 0;
			bool negative;
			int radix = GetSignAndRadix(ref s, out negative);
			if (radix == 0 || negative)
				return false;
			var flags = ParseNumberFlag.SkipSingleQuotes | ParseNumberFlag.SkipUnderscores | ParseNumberFlag.StopBeforeOverflow;
			return ParseHelpers.TryParseUInt(ref s, out u, radix, flags) && s.Length == 0;
		}

		private static bool ParseDouble(UString s, out double d)
		{
			d = double.NaN;
			char first = s[0, '\0'];
			if (!(first >= '0' && first <= '9'))
			{
				if (s.Equals("nan", ignoreCase: true))
					return true;
				else if (s.Equals("inf", ignoreCase: true))
				{
					d = double.PositiveInfinity;
					return true;
				}
				else if (s.Equals("-inf", ignoreCase: true) || s.Equals("\u2212inf", ignoreCase: true))
				{
					d = double.NegativeInfinity;
					return true;
				}
			}
			bool negative;
			int radix = GetSignAndRadix(ref s, out negative);
			if (radix == 0)
				return false;
			var flags = ParseNumberFlag.SkipSingleQuotes | ParseNumberFlag.SkipUnderscores | ParseNumberFlag.StopBeforeOverflow;
			d = ParseHelpers.TryParseDouble(ref s, radix, flags);
			if (negative) d = -d;
			return !double.IsNaN(d) && s.Length == 0;
		}

		static int GetSignAndRadix(ref UString s, out bool negative)
		{
			negative = false;
			if (s.Length == 0)
				return 0;
			char s0 = s[0];
			if (s0 == '-' || s0 == '\x2212')
			{
				negative = true;
				s = s.Slice(1);
				if (s.Length == 0)
					return 0;
			}
			int radix = 10;
			if (s[0] == '0')
			{
				var x = s[1, '\0'];
				if ((radix = x == 'x' ? 16 : x == 'b' ? 2 : 10) != 10)
				{
					s = s.Substring(2);
					if (s.Length == 0)
						return 0;
				}
			}
			return radix;
		}

		#endregion

		#region Printing code

		private StringBuilder PrintInteger(long value, NodeStyle style, StringBuilder sb)
		{
			if ((style & NodeStyle.BaseStyleMask) == NodeStyle.HexLiteral)
				return PrintHelpers.AppendIntegerTo(sb, value, "0x", 16, _digitSeparator != '\0' ? 4 : 0, _digitSeparator);
			else if ((style & NodeStyle.BaseStyleMask) == NodeStyle.BinaryLiteral)
				return PrintHelpers.AppendIntegerTo(sb, value, "0b", 2, _digitSeparator != '\0' ? 8 : 0, _digitSeparator);
			else
				return PrintHelpers.AppendIntegerTo(sb, value, "", 10, _digitSeparator != '\0' ? 3 : 0, _digitSeparator);
		}

		private StringBuilder PrintInteger(ulong value, NodeStyle style, StringBuilder sb)
		{
			if ((style & NodeStyle.BaseStyleMask) == NodeStyle.HexLiteral)
				return PrintHelpers.AppendIntegerTo(sb, value, "0x", 16, _digitSeparator != '\0' ? 4 : 0, _digitSeparator);
			else if ((style & NodeStyle.BaseStyleMask) == NodeStyle.BinaryLiteral)
				return PrintHelpers.AppendIntegerTo(sb, value, "0b", 2, _digitSeparator != '\0' ? 8 : 0, _digitSeparator);
			else
				return PrintHelpers.AppendIntegerTo(sb, value, "", 10, _digitSeparator != '\0' ? 3 : 0, _digitSeparator);
		}

		private void PrintFloat(double value, Symbol typeMarker, NodeStyle style, bool isDouble, StringBuilder sb)
		{
			int oldLength = sb.Length;
			if (double.IsNaN(value))
				sb.Append("nan");
			else if (double.IsPositiveInfinity(value))
				sb.Append("inf");
			else if (double.IsNegativeInfinity(value))
				sb.Append("-inf");
			else {
				char exponentMarker = 'E';
				if ((style & NodeStyle.BaseStyleMask) == NodeStyle.HexLiteral) {
					DoubleToString_HexOrBinary(sb, value, "0x", 4, isDouble);
					exponentMarker = 'p';
				} else if ((style & NodeStyle.BaseStyleMask) == NodeStyle.BinaryLiteral) {
					DoubleToString_HexOrBinary(sb, value, "0b", 1, isDouble);
					exponentMarker = 'p';
				} else
					DoubleToString_Base10(sb, value);

				if (typeMarker == null || typeMarker == _number)
				{
					// ensure the string doesn't look like an integer
					if (sb.FirstIndexOf('.', oldLength) <= -1 && sb.FirstIndexOf(exponentMarker, oldLength) <= -1)
						sb.Append(".0");
				}
			}
		}

		private void DoubleToString_Base10(StringBuilder sb, double value)
		{
			int oldLength = sb.Length;
			// The "R" round-trip specifier makes sure that no precision is lost, and
			// that parsing a printed version of double.MaxValue is possible.
			string asStr = value.ToString("R", CultureInfo.InvariantCulture);
			if (_digitSeparator != '\0' && asStr.Length > 3)
			{
				int iDot = asStr.IndexOf('.');
				if (iDot <= -1) iDot = asStr.IndexOf('E');
				if (iDot <= -1) iDot = asStr.Length;
				// Insert thousands separators in integer part (not implemented on fraction part)
				if (iDot > 3)
				{
					sb.Append(asStr);
					for (int i = iDot - 3; i > 0; i -= 3)
						sb.Insert(i + oldLength, '_');
					return;
				}
			}
			sb.Append(asStr);
		}

		protected int HexNegativeExponentThreshold = -8;
		const int MantissaBits = 52;

		private void DoubleToString_HexOrBinary(StringBuilder result, double value, string prefix, int bitsPerDigit, bool isDouble = false, bool forcePNotation = false)
		{
			Debug.Assert(!double.IsInfinity(value) && !double.IsNaN(value));
			long bits = G.DoubleToInt64Bits(value);
			long mantissa = bits & ((1L << MantissaBits) - 1);
			int exponent = (int)(bits >> MantissaBits) & 0x7FF;
			if (exponent == 0) // subnormal (a.k.a. denormal)
				exponent = 1;
			else
				mantissa |= 1L << MantissaBits;
			exponent -= 0x3FF;
			int precision = isDouble ? MantissaBits : 23;

			int separatorInterval = 0;
			if (_digitSeparator != '\0')
				separatorInterval = bitsPerDigit == 1 ? 8 : 4;

			if (bits < 0)
				result.Append('-');

			// Choose a scientific notation shift (the number that comes after "p") 
			int scientificNotationShift = 0;
			if (exponent > precision) {
				scientificNotationShift = exponent & ~(bitsPerDigit - 1);
			} else if (exponent < HexNegativeExponentThreshold) {
				scientificNotationShift = -((-exponent | (bitsPerDigit - 1)) + 1);
				// equal to this?
				//scientificNotationShift = (exponent - 1) & ~(bitsPerDigit - 1);
			}
			
			// Calculate the exponent on the "non-scientific" part of the number
			exponent -= scientificNotationShift;

			// Calculate the number of bits after the point (dot), then print the 
			// whole and fractional parts of the number separately.
			int fracBits = MantissaBits - exponent;
			long wholePart, fracPart;
			if (exponent >= 0) {
				wholePart = mantissa >> fracBits;
				fracPart = mantissa & ((1L << fracBits) - 1);
			} else {
				wholePart = 0;
				fracPart = mantissa;
			}

			// Print whole-number part
			PrintHelpers.AppendIntegerTo(result, (ulong)wholePart, prefix, 1 << bitsPerDigit, separatorInterval, _digitSeparator);

			// Print fractional part
			if (fracPart == 0 && scientificNotationShift == 0)
				result.Append(".0");
			else {
				result.Append('.');
				int counter = 0;
				int digitMask = ((1 << bitsPerDigit) - 1); // 1 or 0xF
				int shift;
				for (shift = fracBits - bitsPerDigit; shift >= 0; shift -= bitsPerDigit) {
					int digit = (int)(fracPart >> shift) & digitMask;
					result.Append(PrintHelpers.HexDigitChar(digit));
					fracPart &= (1L << shift) - 1;
					if (fracPart == 0)
						break;
					if (++counter == separatorInterval && shift != 0) {
						counter = 0;
						result.Append(_digitSeparator);
					}
				}
				if (fracPart != 0) {
					int digit = ((int)fracPart << (shift + bitsPerDigit)) & digitMask;
					result.Append(PrintHelpers.HexDigitChar(digit));
				}
			}

			// Add shift suffix ("p0")
			if (forcePNotation || scientificNotationShift != 0)
				PrintHelpers.AppendIntegerTo(result, scientificNotationShift, "p", @base: 10, separatorInterval: 0);
		}

		#endregion
	}
}
