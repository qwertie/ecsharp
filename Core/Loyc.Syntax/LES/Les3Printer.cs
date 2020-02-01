using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Numerics;
using System.Globalization;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax.Impl;
using Loyc.Syntax.Lexing;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Syntax.Les
{
	// TODO: Verify # is treated as ident char or not depending on feedback
	// TODO: Switch over from `#` to `.` prefix as the keyword marker?
	public class Les3Printer
	{
		#region Properties

		public StringBuilder SB
		{ 
			get { return PS.S; }
			set { CheckParam.IsNotNull("value", value); PS.S = value; }
		}

		IMessageSink _messageSink;
		/// <summary>Target for warning messages.</summary>
		public IMessageSink MessageSink {
			get { return _messageSink ?? Loyc.MessageSink.Default; }
			set { _messageSink = value; }
		}

		private Les3PrinterOptions _o;
		public Les3PrinterOptions Options { get { return _o; } }
		public void SetOptions(ILNodePrinterOptions options)
		{
			_o = options as Les3PrinterOptions ?? new Les3PrinterOptions(options);
		}

		#endregion

		#region Constructor, fields, and Print()

		internal Les3Printer(StringBuilder target = null, IMessageSink sink = null, ILNodePrinterOptions options = null)
		{
			MessageSink = sink;
			SetOptions(options);
			var newline = string.IsNullOrEmpty(_o.NewlineString) ? "\n" : _o.NewlineString;
			PS = new PrinterState(target ?? new StringBuilder(), _o.IndentString ?? "\t", newline);
		}

		protected PrinterState PS;
		protected ILNode _n;
		protected Precedence _context = Precedence.MinValue;
		protected NewlineContext _nlContext = NewlineContext.NewlineUnsafe;
		protected bool _inParensOrBracks = false;
		/// <summary>Indicates whether the <see cref="NodeStyle.OneLiner"/> 
		/// flag is present on the current node or any of its parents. It 
		/// suppresses newlines within braced blocks.</summary>
		protected bool _isOneLiner = false;

		internal StringBuilder Print(IEnumerable<ILNode> list)
		{
			var neglist = list as INegListSource<ILNode> ?? list.ToList().AsNegList(0);
			PrintStmtList(new NegListSlice<ILNode>(neglist), false);

			if (PS.AtStartOfLine)
				PS.RevokeNewlinesSince(_newlineCheckpoint); // if optional newline not yet committed
			return SB;
		}

		public StringBuilder Print(ILNode node, string suffix = null)
		{
			_avoidExtraNewline = false;
			Print(node, Precedence.MinValue, suffix, NewlineContext.NewlineSafeBefore | NewlineContext.NewlineSafeAfter);

			if (PS.AtStartOfLine)
				PS.RevokeNewlinesSince(_newlineCheckpoint); // if optional newline not yet committed
			return SB;
		}

		static readonly Precedence AttributeContext = new Precedence(120);

		protected void Print(ILNode node, Precedence context, string suffix = null, NewlineContext nlContext = NewlineContext.AutoDetect)
		{
			if (nlContext == NewlineContext.AutoDetect)
			{
				// Detect beginning or end of expression
				if (context.Left == Precedence.MinValue.Left)  nlContext |= NewlineContext.NewlineSafeBefore;
				if (context.Right == Precedence.MinValue.Right) nlContext |= NewlineContext.NewlineSafeAfter;
			}
			var oldContext = _context;
			var oldNLContext = _nlContext;
			var oldIsOneLiner = _isOneLiner;
			try {
				_context = context;
				_nlContext = nlContext;
				_isOneLiner |= (node.Style & NodeStyle.OneLiner) != 0;
				PrintCore(node, suffix);
			} finally {
				_context = oldContext;
				_nlContext = oldNLContext;
				_isOneLiner = oldIsOneLiner;
			}
		}

		protected void PrintCore(ILNode node, string suffix, bool avoidKwExprBraceAmbiguity = false)
		{
			_n = node;
			int parenCount = PrintAttrsAndLeadingTrivia(node, _nlContext);
			switch (node.Kind)
			{
				case LNodeKind.Id: VisitId(node); break;
				case LNodeKind.Literal: VisitLiteral(node); break;
				case LNodeKind.Call:
					VisitCall(node);
					if (_endIndexOfKeywordExpr == SB.Length && avoidKwExprBraceAmbiguity) {

					}
					break;
			}
			PrintTrailingTrivia(node, parenCount, suffix, _nlContext);
			_n = null;
		}

		public override string ToString()
		{
			return SB.ToString();
		}

		#endregion

		#region Special stuff to deal with tricky facts about LESv3
		// Challenges of LESv3 (only some of which are dealt with in this section):
		// - Spaces are required between certain tokens that would be merged without
		//   the space character. For example, an @@at-at-literal requires a space
		//   afterward if it is followed by a binary operator, and a number followed
		//   by a dot operator should conservatively have a space before the dot. 
		//   The need for a space is detected using flags in the Chars enum.
		// - If a space appears before a dot operator, a space may also be required 
		//   after the dot so that the parser doesn't see a keyword token.
		// - If one statement ends with a keyword expression and the next begins
		//   with braces, a semicolon is required after the first statement to avoid
		//   the possibility that the braces will be parsed as part of the keyword
		//   expression (alternative: begin the second statement with @@).
		// - %newline must be ignored in some cases because a newline in 
		//   certain places will inadvertantly end the current statement early.
		//   Also, single-line comments sometimes cannot end with a newline and
		//   must use two backslashes to end the comment instead: \\
		// - If a keyword expression has a braced block (second argument) or 
		//   continuators (possibly with their own braced blocks), only a single 
		//   newline is allowed before each continuator or braced block.
		// - And more: e.g. must deal with precedence; must print operators in
		//   prefix notation if the target has attributes; a space is required 
		//   after any word operator or combo operator...

		protected Chars _curSet = 0;

		/// <summary>
		/// Based on these flags, StartToken() and WriteToken() ensure that two 
		/// adjacent tokens aren't treated like a single token when reparsed, by 
		/// printing a space between them if necessary.
		/// </summary>
		[Flags]
		protected enum Chars {
			Delimiter = 0,     // Spaces, brackets and separators
			IdStart = 1,
			SingleQuote = 2,
			Id = IdStart | SingleQuote,
			Punc = 4,          // Operator characters in general: ~ ! % ^ * - + = | < > / ? : . &
			Dot = 16,
			NumberStart = IdStart | Dot, // Numbers can start with dot (.) and charset overlaps Id. 
			//NegativeNumberStart = Punc | Minus,  // Negative numbers no longer start with `-`, they start with Id
			NumberEnd   = Id | BQId | Dot, // Numbers can end in backquotes (``), letters or digits, and '.' afterward could create ambiguity
			IdAndPunc = Id | Punc | Dot,
			DoubleQuote = 32,
			BQId = 64,                     // `backquoted identifier`
			StringStart = DoubleQuote | Id | BQId, // A string can start with an Id so we can't put a string right after an Id
			At = 128,
			Space = 256,
			SLComment = 512, // SPECIAL CASE: next token causes \\ to be written unless a newline is added
		}

		/// <summary>Used to help keep track of where newline trivia (and single-
		/// line comments ending in a newline) are permitted, to avoid printing
		/// newline trivia where it would count as "end of expression".</summary>
		[Flags]
		protected enum NewlineContext
		{
			NewlineUnsafe = 0,
			NewlineSafeAfter = 1,
			NewlineSafeBefore = 2,
			StatementLevel = NewlineSafeBefore | NewlineSafeAfter,
			AutoDetect = 8,
		}

		// Conservatively returns true if `first` and `second` may need a semicolon 
		// between them to avoid the ambiguity where the parser could consider braces 
		// in `second` to be a continuation of a keyword expression in `first`.
		private bool NeedSemicolonBetween(ILNode first, ILNode second)
		{
			if (StartsWithBraces(second)) {
				Func<ILNode, bool> hasKeywordExpr = null;
				return first.Any(hasKeywordExpr = n => {
					return n.IsId() && n.Name.Name.StartsWith("#") || n.Any(hasKeywordExpr);
				});
			}
			return false;
		}
		
		// Conservatively returns true if `node` may begin with a '{' token
		private bool StartsWithBraces(ILNode node)
		{
			if (node == null || !node.IsCall())
				return false;
			return node.Name == S.Braces || StartsWithBraces(node.Target) || StartsWithBraces(node.TryGet(0, null));
		}

		protected void StartToken(LesColorCode kind, Chars charSet) { StartToken(kind, charSet, charSet); }
		protected void StartToken(LesColorCode kind, Chars startSet, Chars endSet)
		{
			if (_curSet == Chars.SLComment)
				SB.Append(@"\\");
			Space((startSet & _curSet) != 0);
			_curSet = endSet;
			StartToken(kind);
		}

		#endregion

		#region Helper functions

		protected void WriteToken(char firstChar, LesColorCode kind, Chars tokenSet)
		{
			StartToken(kind, tokenSet);
			SB.Append(firstChar);
		}
		protected void WriteToken(string text, LesColorCode kind, Chars tokenSet)
		{
			StartToken(kind, tokenSet);
			SB.Append(text);
		}
		protected void WriteToken(string text, LesColorCode kind, Chars startSet, Chars endSet)
		{
			StartToken(kind, startSet, endSet);
			SB.Append(text);
		}

		protected virtual void StartToken(LesColorCode kind)
		{
		}

		protected void WriteOutsideToken(char space)
		{
			if (_curSet == Chars.SLComment)
				SB.Append(@"\\");
			if (_curSet != Chars.Space) {
				_curSet = Chars.Space;
				StartToken(LesColorCode.None);
			}
			SB.Append(space);
		}

		bool _avoidExtraNewline = false;
		PrinterState.Checkpoint _newlineCheckpoint;

		protected void Newline(bool avoidExtraNewline = false)
		{
			if (!(_avoidExtraNewline && PS.AtStartOfLine)) {
				if (_curSet != Chars.Space) {
					_curSet = Chars.Space;
					StartToken(LesColorCode.None);
				}
				_newlineCheckpoint = PS.Newline();
			}
			_avoidExtraNewline = avoidExtraNewline;
			if (!avoidExtraNewline)
				PS.CommitNewlines();
		}

		protected void Space(bool condition = true)
		{
			if (condition)
				WriteOutsideToken(' ');
		}

		private bool HasTargetIdWithoutPAttrs(ILNode node)
		{
			var t = node.Target;
			if (!t.IsId())
				return false;
			return !HasPAttrs(t);
		}

		#endregion

		#region Printing of identifiers

		static readonly Symbol sy_true = (Symbol)"true", sy_false = (Symbol)"false", sy_null = (Symbol)"null";

		public void VisitId(ILNode node)
		{
			PrintIdCore(node.Name, startToken: true);
		}

		public static bool IsNormalIdentifier(Symbol name)
		{
			return Les2Printer.IsNormalIdentifier(name);
		}

		public void PrintIdCore(Symbol name, bool startToken, bool forceQuote = false)
		{
			if (forceQuote || !IsNormalIdentifier(name) || name == sy_true || name == sy_false || name == sy_null) {
				if (startToken)
					StartToken(ColorCodeForId(name), Chars.BQId, Chars.BQId);
				PrintStringCore('`', false, name.Name);
			} else {
				if (startToken)
					StartToken(ColorCodeForId(name), Chars.Id, Chars.Id);
				SB.Append(name.Name);
			}
		}

		protected virtual LesColorCode ColorCodeForId(Symbol name)
		{
			return LesColorCode.Id;
		}

		private void PrintStringCore(char quoteType, bool tripleQuoted, string text, Symbol prefix = null)
		{
			if (prefix != null)
				PrintIdCore(prefix, startToken: false);
		restart:
			int startAt = SB.Length;
			SB.Append(quoteType);
			if (tripleQuoted)
			{
				// Print triple-quoted
				SB.Append(quoteType);
				SB.Append(quoteType);

				char a = '\0', b = '\0';
				for (int i = 0; i < text.Length; i++)
				{
					char c = text[i], d;
					if (c == quoteType && ((b == quoteType && a == quoteType) || i + 1 == text.Length)) {
						// Escape triple quote, or a quote at end-of-string
						SB.Append('\\');
						SB.Append(c);
						SB.Append('/');
					} else if (c == '\0') {
						SB.Append(@"\0/");
					} else if (c == '\r') {
						SB.Append(@"\r/");
					} else if (c == '\\' && i + 2 < text.Length && text[i + 2] == '/' &&
						((d = text[i + 1]) == 'r' || d == 'n' || d == 't' || d == '0' || d == '\\' || d == '\'' || d == '"')) {
						// avoid writing a false escape sequence
						SB.Append(@"\\/");
					} else if (c == '\n') {
						PS.Newline();
						PS.CommitNewlines();
						SB.Append(PS.IndentString.EndsWith("\t") ? "\t" : "   ");
					} else if (c >= 0xDC80 && c <= 0xDCFF && !(b >= 0xD800 && b <= 0xDBFF)) {
						// invalid UTF8 byte encoded as UTF16: CANNOT print in 
						// triple-quoted string! Start over as DQ string.
						SB.Length = startAt;
						quoteType = '"';
						tripleQuoted = false;
						goto restart;
					} else {
						SB.Append(c);
					}
					a = b; b = c;
				}

				SB.Append(quoteType);
				SB.Append(quoteType);
			}
			else 
			{
				// Normal double-quoted string
				UString s = text;
				for (;;) {
					bool fail;
					int c = s.PopFirst(out fail);
					if (fail) break;

					EscapeC flags = EscapeC.Control | EscapeC.UnicodeNonCharacters | EscapeC.UnicodePrivateUse;
					// Enable \x selectively because \x represents bytes, not characters
					if (c <= 31)
						flags |= EscapeC.BackslashX;
					else if (c >= 0xD800 && (c <= 0xDFFF || c >= 0xFFF0)) {
						// force escape sequence for noncharacters and astral characters
						flags |= EscapeC.NonAscii;
						if (c.IsInRange(0xDC80, 0xDCFF)) {
							c &= 0xFF; // detected invalid byte encoded as invalid UTF16
							flags |= EscapeC.BackslashX;
						}
					}
					PrintHelpers.EscapeCStyle(c, SB, flags, quoteType);
				}
			}
			SB.Append(quoteType);
		}

		#endregion

		#region Printing of literals

		public void VisitLiteral(ILNode node)
		{
			PrintLiteralCore(node.Value, node.Style);
		}

		#region Table of literal printer helper lambdas

		static Pair<RuntimeTypeHandle, Action<Les3Printer, object, NodeStyle, Symbol>> P<T>(Action<Les3Printer, object, NodeStyle, Symbol> handler) 
			{ return Pair.Create(typeof(T).TypeHandle, handler); }
		static Dictionary<K,V> Dictionary<K,V>(params Pair<K,V>[] input)
		{
			var d = new Dictionary<K,V>();
			for (int i = 0; i < input.Length; i++)
				d.Add(input[i].Key, input[i].Value);
			return d;
		}

		static Dictionary<RuntimeTypeHandle,Action<Les3Printer, object, NodeStyle, Symbol>> LiteralPrinters = Dictionary(
			// TODO: improve BigInteger support (ie. efficiency, support hex & binary, support digit separators
			P<BigInteger>((p, value, style, tmarker) => p.PrintString(((BigInteger)value).ToString(), style, _Z, detectFormat: true)),
			P<int>    ((p, value, style, tmarker) => p.PrintInteger((int)value, style, tmarker)),
			P<long>   ((p, value, style, tmarker) => p.PrintInteger((long)value, style, tmarker ?? _L)),
			P<uint>   ((p, value, style, tmarker) => p.PrintInteger((uint)value, style, tmarker ?? _U)),
			P<ulong>  ((p, value, style, tmarker) => p.PrintInteger((ulong)value, style, tmarker ?? _UL)),
			P<short>  ((p, value, style, tmarker) => p.PrintInteger((short)value,  style, tmarker ?? (Symbol)"i16")),
			P<ushort> ((p, value, style, tmarker) => p.PrintInteger((ushort)value, style, tmarker ?? (Symbol)"u16")),
			P<sbyte>  ((p, value, style, tmarker) => p.PrintInteger((sbyte)value,  style, tmarker ?? (Symbol)"i8")),
			P<byte>   ((p, value, style, tmarker) => p.PrintInteger((byte)value,   style, tmarker ?? (Symbol)"u8")),
			P<double> ((p, value, style, tmarker) => p.PrintDouble((double)value, style, tmarker ?? _D)),
			P<float>  ((p, value, style, tmarker) => p.PrintFloat ((float)value, style, tmarker ?? _F)),
			P<decimal>((p, value, style, tmarker) => p.PrintDouble((double)(decimal)value, style, tmarker ?? _M)),
			P<bool>   ((p, value, style, tmarker) => p.WriteToken((bool)value? "true" : "false", LesColorCode.KeywordLiteral, Chars.Id)),
			P<@void>  ((p, value, style, tmarker) => {
				p.WriteToken("@@void", LesColorCode.CustomLiteral, Chars.At, Chars.IdAndPunc);
			}),
			P<char>   ((p, value, style, tmarker) => {
				p.StartToken(LesColorCode.String, Chars.SingleQuote);
				p.PrintStringCore('\'', false, value.ToString());
			}),
			P<Symbol> ((p, value, style, tmarker) => {
				p.StartToken(LesColorCode.CustomLiteral, Chars.StringStart, Chars.DoubleQuote);
				p.PrintStringCore('"', false, value.ToString(), tmarker ?? _s);
			}),
			P<TokenTree> ((p, value, style, tmarker) => {
				// TODO: change/remove this. LESv3 has no syntax for TokenTree
				p.WriteToken("@@{", LesColorCode.Opener, Chars.At, Chars.Delimiter);
				p.Space();
				p.StartToken(LesColorCode.CustomLiteral);
				p.SB.Append(((TokenTree)value).ToString(TokenExt.ToString));
				p.Space();
				p.WriteToken("}", LesColorCode.Closer, Chars.Delimiter, Chars.Delimiter);
			}),
			P<string>((p, value, style, tmarker) => p.PrintString((string)value, style, tmarker, detectFormat: true)));

		protected static Symbol _s = GSymbol.Get("s");
		protected static Symbol _F = GSymbol.Get("f");
		protected static Symbol _D = GSymbol.Get("d");
		protected static Symbol _M = GSymbol.Get("m");
		protected static Symbol _U = GSymbol.Get("u");
		protected static Symbol _L = GSymbol.Get("L");
		protected static Symbol _UL = GSymbol.Get("uL");
		protected static Symbol _Z = GSymbol.Get("z");
		protected static Symbol _number = GSymbol.Get("n");

		#endregion

		private void PrintLiteralCore(object value, NodeStyle style)
		{
			Symbol tmarker = null;
			if (value is CustomLiteral) {
				var sp = (CustomLiteral)value;
				tmarker = sp.TypeMarker;
				value = sp.Value;
				if (tmarker == null)
					MessageSink.Write(Severity.Warning, _n, "Les3Printer: CustomLiteral.TypeMarker is null");
			}

			Action<Les3Printer, object, NodeStyle, Symbol> printHelper;
			if (value == null) {
				if (tmarker != null)
					MessageSink.Write(Severity.Warning, _n, "Les3Printer: type marker ('{0}') attached to 'null' is unprintable", tmarker);
				WriteToken("null", LesColorCode.KeywordLiteral, Chars.Id);
			} else if (LiteralPrinters.TryGetValue(value.GetType().TypeHandle, out printHelper)) {
				printHelper(this, value, style, tmarker);
			} else {
				if (tmarker == null)
					tmarker = (Symbol)MemoizedTypeName.Get(value.GetType());
				if (_o.WarnAboutUnprintableLiterals)
					MessageSink.Write(Severity.Warning, _n, "Les3Printer: Encountered unprintable literal of type {0}",
						MemoizedTypeName.Get(value.GetType()));
				string text;
				try {
					text = value.ToString();
				} catch (Exception ex) {
					text = ex.ExceptionMessageAndType();
				}
				PrintString(text, style, tmarker, detectFormat: false);
			}
		}

		static Regex IsNumber = new Regex(@"^[0-9]+([.][0-9]+)?([eE][+\-]?[0-9]+)?$");

		private void PrintString(string text, NodeStyle style, Symbol prefix, bool detectFormat)
		{
			var kind = LesColorCode.String;
			if (prefix != null) {
				kind = LesColorCode.CustomLiteral;
				if (detectFormat) {
					// Detect if we should print it as a number or @@literal instead
					if (IsNumber.IsMatch(text)) {
						WriteToken(text, kind = LesColorCode.Number, Chars.NumberStart, Chars.NumberEnd);
						char firstCh = prefix.Name.TryGet(0, '\0');
						if (prefix != _number)
							PrintIdCore(prefix, startToken: false, forceQuote: firstCh.IsOneOf('e', 'E'));
						return;
					} else if (prefix.Name == "@@" && Les3PrecedenceMap.IsExtendedOperatorToken(text)) {
						WriteToken("@@", kind, Chars.At, Chars.IdAndPunc);
						SB.Append(text);
						return;
					}
				}
			}
			NodeStyle bs = (style & NodeStyle.BaseStyleMask);
			if (bs == NodeStyle.TQStringLiteral) {
				StartToken(kind, Chars.SingleQuote);
				PrintStringCore('\'', true, text, prefix);
			} else {
				StartToken(kind, Chars.StringStart, Chars.DoubleQuote);
				PrintStringCore('"', bs == NodeStyle.TDQStringLiteral, text, prefix);
			}
		}

		void PrintInteger(long value, NodeStyle style, Symbol suffix)
		{
			bool negative = value < 0;
			if (negative) {
				StartToken(LesColorCode.Number, Chars.Id, Chars.NumberEnd);
				PrintIdCore(suffix ?? _number, startToken: false);
				SB.Append("\"-");
				PrintIntegerCore((ulong)-value, style, suffix: null);
				SB.Append('\"');
			} else {
				StartToken(LesColorCode.Number, Chars.NumberStart, Chars.NumberEnd);
				PrintIntegerCore((ulong)value, style, suffix);
			}
		}

		void PrintInteger(ulong value, NodeStyle style, Symbol suffix)
		{
			StartToken(LesColorCode.Number, Chars.NumberStart, Chars.NumberEnd);
			PrintIntegerCore(value, style, suffix);
		}
		void PrintIntegerCore(ulong value, NodeStyle style, Symbol suffix)
		{
			bool forceQuote = false;
			char suffix0 = '\0';
			if (suffix != null && suffix.Name != "") {
				suffix0 = suffix.Name[0];
				forceQuote = suffix0 == '_';
			}
			if ((style & NodeStyle.BaseStyleMask) == NodeStyle.HexLiteral) {
				PrintHelpers.AppendIntegerTo(SB, value, "0x", 16, 4, '_');
				forceQuote |= suffix0 >= 'a' && suffix0 <= 'f' || suffix0 >= 'A' && suffix0 <= 'F';
			} else if ((style & NodeStyle.BaseStyleMask) == NodeStyle.BinaryLiteral)
				PrintHelpers.AppendIntegerTo(SB, value, "0b", 2, 8, '_');
			else
				PrintHelpers.AppendIntegerTo(SB, value, "", 10, 3, '_');
			if (suffix != null)
				PrintIdCore(suffix, startToken: false, forceQuote: forceQuote);
		}

		const string NaNPrefix = "@@nan.";
		const string PositiveInfinityPrefix = "@@inf.";
		const string NegativeInfinityPrefix = "@@-inf.";

		void PrintFloat(float value, NodeStyle style, Symbol suffix)
		{
			PrintDouble((double)value, style, suffix ?? _F);
		}
		void PrintDouble(double value, NodeStyle style, Symbol suffix)
		{
			bool isHex = false;
			bool isNumber = false;
			if (double.IsNaN(value)) {
				WriteToken(NaNPrefix, LesColorCode.KeywordLiteral, Chars.At, Chars.IdAndPunc);
			} else if (double.IsPositiveInfinity(value)) {
				WriteToken(PositiveInfinityPrefix, LesColorCode.KeywordLiteral, Chars.At, Chars.IdAndPunc);
			} else if (double.IsNegativeInfinity(value)) {
				WriteToken(NegativeInfinityPrefix, LesColorCode.KeywordLiteral, Chars.At, Chars.IdAndPunc);
			} else {
				isNumber = true;
				StartToken(LesColorCode.Number, value < 0 ? Chars.Id : Chars.NumberStart, Chars.NumberEnd);
				string asStr;
				if ((style & NodeStyle.BaseStyleMask) == NodeStyle.HexLiteral)
					asStr = DoubleToString_HexOrBinary(new StringBuilder(), value, "0x", 4, suffix == _F).ToString();
				else if ((style & NodeStyle.BaseStyleMask) == NodeStyle.BinaryLiteral)
					asStr = DoubleToString_HexOrBinary(new StringBuilder(), value, "0b", 1, suffix == _F).ToString();
				else
					asStr = DoubleToString_Decimal(value);

				if (value < 0) {
					PrintIdCore(suffix ?? _D, startToken: false);
					SB.Append('\"');
					SB.Append(asStr);
					SB.Append('\"');
					return;
				} else {
					SB.Append(asStr);
					if (suffix == _D) {
						if (!asStr.Contains(".") && !asStr.Contains("e"))
							SB.Append(".0");
						return;
					}
				}
			}
			if (isNumber) {
				char suffix0;
				PrintIdCore(suffix, startToken: false, forceQuote: isHex && 
					((suffix0 = suffix.Name.TryGet(0, '\0')) >= 'a' && suffix0 <= 'f' || suffix0 >= 'A' && suffix0 <= 'F'));
			} else {
				if (suffix == _F || IsNormalIdentifier(suffix))
					SB.Append(suffix.Name);
				else
					MessageSink.Write(Severity.Warning, _n, "Les3Printer: cannot print type marker '{0}' on non-finite number", suffix);
			}
		}

		protected int HexNegativeExponentThreshold = -8;
		const int MantissaBits = 52;

		private StringBuilder DoubleToString_HexOrBinary(StringBuilder result, double value, string prefix, int bitsPerDigit, bool isFloat = false, bool forcePNotation = false)
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
			int precision = isFloat ? 23 : MantissaBits;

			char separator = _o.DigitSeparator ?? '\0';
			int separatorInterval = 0;
			if (_o.DigitSeparator.HasValue)
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
			PrintHelpers.AppendIntegerTo(result, (ulong)wholePart, prefix, 1 << bitsPerDigit, separatorInterval, separator);

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
						result.Append(separator);
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

			return result;
		}

		private string DoubleToString_Decimal(double value)
		{
			// The "R" round-trip specifier makes sure that no precision is lost, and
			// that parsing a printed version of double.MaxValue is possible.
			var asStr = value.ToString("R", CultureInfo.InvariantCulture);
			if (_o.DigitSeparator.HasValue && asStr.Length > 3) {
				int iDot = asStr.IndexOf('.');
				if (iDot <= -1) iDot = asStr.IndexOf('E');
				if (iDot <= -1) iDot = asStr.Length;
				// Add thousands separators
				if (iDot > 3) {
					StringBuilder sb = new StringBuilder(asStr);
					for (int i = iDot - 3; i > 0; i -= 3)
						sb.Insert(i, '_');
					return sb.ToString();
				}
			}
			return asStr;
		}

		#endregion

		#region Printing calls: main Visit() method

		public void VisitCall(ILNode node)
		{
			// Note: Attributes, if any, have already been printed by this point
			bool parens = false;

			switch (_o.PrefixNotationOnly ? NodeStyle.PrefixNotation : node.BaseStyle())
			{
				case NodeStyle.Operator:
				case NodeStyle.Statement:
				case NodeStyle.Default:
					// Figure out if this node can be treated as an operator and if 
					// so, whether it's a suffix operator.
					if (!HasTargetIdWithoutPAttrs(node))
						goto default;

					Symbol opName = node.Name;
					if (!TryToPrintCallAsSpecialOperator(opName, node))
					{
						if (!node.ArgCount().IsInRange(1, 2))
							goto default;
						var shape = (OperatorShape)node.ArgCount();
						if (node.ArgCount() == 1 && Les3PrecedenceMap.IsSuffixOperatorName(opName, out opName))
							shape = OperatorShape.Suffix;

						if (!PrintCallAsNormalOp(shape, opName, node, ref parens))
							PrintPrefixNotation(node, ref parens);
					}
					break;
				case NodeStyle.Special:
					if (!TryToPrintCallAsKeywordExpression(node))
						goto default;
					break;
				case NodeStyle.PrefixNotation:
					PrintPrefixNotation(node, ref parens);
					break;
				default:
					PrintPrefixNotation(node, ref parens);
					break;
			}
			if (parens)
				WriteToken(')', LesColorCode.Closer, Chars.Delimiter);
		}

		#endregion

		#region Printing calls: in prefix notation (i.e. foo(...))

		void PrintPrefixNotation(ILNode node, ref bool parens)
		{
			parens |= AddParenIf(!IsAllowedHere(LesPrecedence.Primary));
			Debug.Assert(node.IsCall());
			Print(node.Target, LesPrecedence.Primary.LeftContext(_context));
			PrintArgList(node);
		}

		internal static readonly HashSet<Symbol> ContinuatorOps = Les3Parser.ContinuatorOps;
		internal static readonly Dictionary<object, Symbol> Continuators = Les3Parser.Continuators;

		bool IsContinuator(ILNode candidate)
		{
			int argc = candidate.ArgCount();
			if ((argc == 1 || argc == 2) && ContinuatorOps.Contains(candidate.Name) && HasTargetIdWithoutPAttrs(candidate)) {
				if (argc == 2)
					return candidate[1].Calls(S.Braces) && HasTargetIdWithoutPAttrs(candidate[1]);
				return true;
			}
			return false;
		}

		void PrintArgList(ILNode node)
		{
			var args = node.Args();
			PrintArgListCore(args, '(', ')', ArgListStyle.Normal, _o.SpaceInsideArgLists);
		}

		enum ArgListStyle
		{
			Normal = 0,     // normal arg list
			Semicolons = 1, // semicolons as separator (tuple)
			BracedBlock = 2 // semicolons and newlines (braced block)
		}
		void PrintArgListCore(NegListSlice<ILNode> args, char leftDelim, char rightDelim, ArgListStyle style, bool spacesInside = false, ILNode leftBracket = null)
		{
			var outerInParensOrBracks = _inParensOrBracks;
			_inParensOrBracks = style == ArgListStyle.BracedBlock;

			if (leftBracket != null) {
				Debug.Assert(!HasPAttrs(leftBracket));
				PrintAttrsAndLeadingTrivia(leftBracket, NewlineContext.NewlineUnsafe);
			}
			WriteToken(leftDelim, LesColorCode.Opener, Chars.Delimiter);
			if (leftBracket != null)
				PrintTrailingTrivia(leftBracket, 0, null, NewlineContext.NewlineSafeAfter);
			Space(spacesInside);
			PS.Indent();
			if (style == ArgListStyle.BracedBlock) {
				if (PrintStmtList(args, initialNewline: true)) {
					PS.Dedent();
					Newline();
				} else {
					PS.Dedent();
					Space(_o.SpacesBetweenAppendedStatements);
				}
			} else {
				string semicolon = (style == ArgListStyle.Normal ? null : ";");
				for (int i = 0; i < args.Count; i++) {
					if (i != 0)
						Space(_o.SpaceAfterComma && !PS.AtStartOfLine);
					var stmt = args[i];
					Print(stmt, Precedence.MinValue, suffix: semicolon ?? (i + 1 != args.Count ? "," : null));
					MaybeForceLineBreak();
				}
				PS.Dedent();
			}
			Space(spacesInside);
			WriteToken(rightDelim, LesColorCode.Closer, Chars.Delimiter);

			_inParensOrBracks = outerInParensOrBracks;
		}

		bool ShouldAppendStmt(ILNode node)
		{
			return (node.AttrNamed(S.TriviaAppendStatement) != null || _isOneLiner) &&
				PS.IndexInCurrentLine < _o.ForcedLineBreakThreshold;
		}

		bool PrintStmtList(NegListSlice<ILNode> args, bool initialNewline)
		{
			bool anyNewlines = false;
			if (args.Count > 0) {
				var next = args[0];
				bool append = ShouldAppendStmt(next);
				for (int i = 0; next != null; i++) {
					if (initialNewline || i != 0) {
						if (append)
							Space(_o.SpacesBetweenAppendedStatements);
						else {
							Newline();
							anyNewlines = true;
						}
					}
					var stmt = next;
					next = args[i + 1, null];
					append = next != null ? ShouldAppendStmt(next) : false;
					bool semicolon = append || _o.UseRedundantSemicolons || NeedSemicolonBetween(stmt, next);
					Print(stmt, Precedence.MinValue, suffix: semicolon ? ";" : null);
				}
			}
			return anyNewlines;
		}

		#endregion

		#region Printing normal operators: Prefix, suffix, infix

		/// <summary>Returns true if the given name can be printed as a binary operator in LESv3.</summary>
		private static bool CanBeBinaryOperator(string name)
		{
			if (name.Length <= 1 || name[0] != '\'')
				return false;
			int i;
			for (i = 1; ;) {
				if (!IsComboOpPrefixChar(name[i]))
					break;
				if (++i == name.Length)
					return true;
			}
			var opToken = name.Slice(i);
			if (!Les3PrecedenceMap.IsNaturalOperatorToken(opToken))
				return false;
			TokenType tt = Les3Lexer.GetOperatorTokenType(opToken);
			return tt != TokenType.PreOrSufOp && tt != TokenType.PrefixOp;
		}
		public static bool IsComboOpPrefixChar(char c) => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_';

		/// <summary>Returns true if the given name can be printed as a prefix operator in LESv3.</summary>
		private static bool CanBePrefixOperator(string name)
		{
			if (name.Length <= 1 || name[0] != '\'')
				return false; // optimized path

			int i = 1;
			if (name[1] == '$') {
				i = 2;
				if (name.Length == 2)
					return true;
			}
			return Les3PrecedenceMap.IsNaturalOperatorToken(name.Slice(i));
		}

		// Prints an operator with a "normal" shape (infix, prefix or suffix)
		// or in prefix notation if that fails. Returns true if closing ')' needed.
		private bool PrintCallAsNormalOp(OperatorShape shape, Symbol opName, ILNode node, ref bool parens)
		{
			// Check if this operator is allowed here. For example, if operator '+ 
			// appears within an argument to '*, as in (2 + 3) * 5, it's not 
			// allowed without parentheses. Also, unary word operators (e.g. 'foo) 
			// are not supported.
			Precedence prec = Les3PrecedenceMap.Default.Find(shape, opName, cacheWordOp: true, les3InfixOp: shape == OperatorShape.Infix);
			if (shape == OperatorShape.Infix) {
				if (!CanBeBinaryOperator(opName.Name))
					return false;
			} else {
				if (prec == LesPrecedence.Other || !CanBePrefixOperator(opName.Name))
					return false;
			}
			bool allowed = IsAllowedHere(prec);
			if (!allowed && IsAllowedHere(LesPrecedence.Primary))
				return false;
			parens |= AddParenIf(!allowed);

			switch (shape) {
				case OperatorShape.Prefix:
					Debug.Assert(node.ArgCount() == 1);
					var inner = node[0];
					PrintOpName(opName, node.Target, isBinaryOp: false);
					Space(prec.Lo < _o.SpaceAfterPrefixStopPrecedence);
					Print(inner, prec.RightContext(_context));
					break;
				case OperatorShape.Suffix:
					Debug.Assert(node.ArgCount() == 1);
					Print(node[0], prec.LeftContext(_context));
					PrintOpName(opName, node.Target, isBinaryOp: false);
					break;
				default:
					Debug.Assert(node.ArgCount() == 2);
					Print(node[0], prec.LeftContext(_context));
					Space(prec.Lo < _o.SpaceAroundInfixStopPrecedence);
					bool newlineSafe = PrintOpName(opName, node.Target, isBinaryOp: true);
					if (SB.Last() != '\n')
						Space(prec.Lo < _o.SpaceAroundInfixStopPrecedence);
					var nlContext = NewlineContext.AutoDetect;
					if (newlineSafe)
						nlContext |= NewlineContext.NewlineSafeBefore;
					Print(node[1], prec.RightContext(_context), nlContext: nlContext);
					break;
			}
			return true;
		}
		bool PrintOpName(Symbol opName, ILNode target, bool isBinaryOp)
		{
			if (target != null && target.AttrCount() == 0)
				target = null; // optimize usual case
			if (target != null)
				PrintAttrsAndLeadingTrivia(target, NewlineContext.NewlineUnsafe); // must all be trivia

			Debug.Assert(opName.Name.StartsWith("'"));

			// Determine Chars values which control sace characters before and after
			char first = opName.Name[1], last = opName.Name[opName.Name.Length - 1];
			Chars startSet;
			if (Les3PrecedenceMap.IsOpChar(first) || first == '$')
				startSet = Chars.Punc | (first == '.' ? Chars.Dot : 0);
			else {
				Debug.Assert(Les2Lexer.IsIdStartChar(first));
				startSet = Chars.IdStart;
			}
			StartToken(LesColorCode.Operator, startSet,
				Chars.Punc | (last == '.' ? Chars.Dot : 0));

			if (opName == S.Dot && SB.LastOrDefault(' ').IsOneOf(' ', '\t', '\n')) {
				// If there is a space before the dot and there is an identifier 
				// after the dot, we will need another space so it doesn't get 
				// reparsed as a keyword.
				_curSet |= Chars.Id;
			}

			SB.Append(opName.Name, 1, opName.Name.Length - 1);

			bool newlineSafe = isBinaryOp && opName.Name.Any(c => Les3PrecedenceMap.IsOpChar(c));
			if (target != null) {
				var nlContext = newlineSafe ? NewlineContext.NewlineSafeAfter : NewlineContext.NewlineUnsafe;
				PrintTrailingTrivia(target, 0, null, nlContext);
			}

			return newlineSafe;
		}

		private bool AddParenIf(bool cond, bool forAttribute = false)
		{
			if (cond) {
				WriteToken('(', LesColorCode.Opener, Chars.Delimiter);
				if (!forAttribute && !_o.AllowExtraParenthesis && _context != AttributeContext) {
					WriteToken("@@", LesColorCode.Attribute, Chars.At);
					WriteOutsideToken(' ');
				}
				Space(_o.SpaceInsideGroupingParens);
				_context = Precedence.MinValue;
			}
			return cond;
		}

		private bool IsAllowedHere(Precedence precedence)
		{
			return precedence.CanAppearIn(_context) && precedence.CanMixWith(_context);
		}

		#endregion

		#region Printing special operators: {braces}, [list], (tuple;), indexer[], generic!type

		private bool TryToPrintCallAsSpecialOperator(Symbol opName, ILNode node)
		{
			if (opName == CodeSymbols.Braces) { // {...}
				PrintBracedBlock(node);
			} else if (opName == CodeSymbols.Array) { // [...]
				PrintArgListCore(node.Args(), '[', ']', 
					ArgListStyle.Normal, _o.SpaceInsideListBrackets, leftBracket: node.Target);
			} else if (opName == CodeSymbols.Tuple) { // (;;)
				PrintArgListCore(node.Args(), '(', ')', 
					ArgListStyle.Semicolons, _o.SpaceInsideTuples, leftBracket: node.Target);
			} else if (opName == CodeSymbols.IndexBracks && node.ArgCount() > 0) { // foo[...]
				Print(node[0], LesPrecedence.Primary.LeftContext(_context));
				PrintArgListCore(node.Args().Slice(1), '[', ']', (node.Style & NodeStyle.Alternate) != 0
					? ArgListStyle.Semicolons : ArgListStyle.Normal, _o.SpaceInsideArgLists, leftBracket: node.Target);
			} else if (opName == CodeSymbols.Of) {
				var args = node.Args();
				Print(args[0], LesPrecedence.Of.LeftContext(_context));
				PrintOpName(S.Not, node.Target, true);
				if (args.Count == 2 && args[1].IsId() && args[1].AttrCount() == 0)
					VisitId(args[1]);
				else
					PrintArgListCore(args.Slice(1), '(', ')', ArgListStyle.Normal, _o.SpaceInsideArgLists);
			} else
				return false;
			return true;
		}

		private void PrintBracedBlock(ILNode braces)
		{
			Debug.Assert(braces.Calls(CodeSymbols.Braces));
			PrintArgListCore(braces.Args(), '{', '}', ArgListStyle.BracedBlock, leftBracket: braces.Target);
		}

		#endregion

		#region Printing .keyword expressions

		// Index where we just finished printing a keyword expression.
		// This is used to avoid ambiguity by adding a semicolon if the 
		// statement afterward begins with braces.
		int _endIndexOfKeywordExpr;

		private bool TryToPrintCallAsKeywordExpression(ILNode node)
		{
			if (!LesPrecedence.SuperExpr.CanAppearIn(_context) || !IsKeywordExpression(node))
				return false;

			var args = node.Args();
			var target = node.Target;
			int parens = PrintAttrsAndLeadingTrivia(target, NewlineContext.NewlineUnsafe);
			WriteToken('.', LesColorCode.Keyword, Chars.IdAndPunc);
			SB.Append(node.Name.Name, 1, node.Name.Name.Length-1);
			PrintTrailingTrivia(target, 0, null, NewlineContext.NewlineUnsafe);
			if (args.Count == 0)
				return true;

			Space();
			Print(args[0], LesPrecedence.SuperExpr);

			if (args.Count > 1)
			{
				int i = 1;
				var part = args[i];
				if (part.Calls(CodeSymbols.Braces)) {
					Space();
					PrintBracedBlock(part);
					i++;
				}
				for (; i < args.Count; i++) {
					Space();
					PrintContinuator(part = args[i]);
				}
			}

			_endIndexOfKeywordExpr = SB.Length;
			return true;
		}

		private void PrintContinuator(ILNode continuator)
		{
			Debug.Assert(IsContinuator(continuator));
			var target = continuator.Target;
			Debug.Assert(!target.HasPAttrs());
			PrintAttrsAndLeadingTrivia(target, NewlineContext.NewlineUnsafe);
			WriteToken(continuator.Name.Name.Substring(1), LesColorCode.Keyword, Chars.Id);
			PrintTrailingTrivia(target, 0, null, NewlineContext.NewlineUnsafe);

			int argc = continuator.ArgCount();
			var expr = continuator[0];
			Space();
			Print(continuator[0], Precedence.MinValue, nlContext: NewlineContext.NewlineUnsafe);
			if (argc >= 2) {
				Space();
				PrintBracedBlock(continuator[1]);
			}
		}

		static bool IsAcceptableKeyword(string name)
		{
			if (string.IsNullOrEmpty(name) || name[0] != '#')
				return false;
			for (int i = 1; i < name.Length; i++)
			{
				char c = name[i];
				if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_'))
					return false;
			}
			return true;
		}

		private bool IsKeywordExpression(ILNode node)
		{
			if (!HasTargetIdWithoutPAttrs(node) || !IsAcceptableKeyword(node.Name.Name))
				return false;
			var args = node.Args();
			if (args.Count <= 1)
				return true;
			var block = args[1];
			if (!HasTargetIdWithoutPAttrs(block))
				return false;
			int i = block.Calls(S.Braces) ? 2 : 1;
			for (; i < args.Count; i++)
				if (!IsContinuator(args[i]))
					return false;

			Debug.Assert(node.Target.IsId());
			return true;
		}

		#endregion

		#region Printing attributes (with trivia)

		private int PrintAttrsAndLeadingTrivia(ILNode node, NewlineContext nlContext)
		{
			bool newlineSafePoint = (nlContext & NewlineContext.NewlineSafeBefore) != 0;
			var A = node.Attrs();
			int parenCount = 0;
			bool needParensForAttribute = !_context.CanParse(LesPrecedence.SuperExpr);
			int normalAttrs = 0;
			for (int i = 0; i < A.Count; i++) {
				var attr = A[i];

				if (!MaybePrintTrivia(attr, false, newlineSafePoint || normalAttrs > 0)) {
					if (attr.Name == S.TriviaInParens)
					{
						if (!_context.CanParse(LesPrecedence.Substitute))
						{
							// Inside $ or @: outer parens are expected. Add a second 
							// pair so that reparsing preserves the in-parens trivia.
							WriteToken('(', LesColorCode.Opener, Chars.Delimiter);
							parenCount++;
						}
						WriteToken('(', LesColorCode.Opener, Chars.Delimiter);
						parenCount++;
						Space(_o.SpaceInsideGroupingParens);
					}
					else
					{
						// Print as normal attribute
						parenCount += AddParenIf(needParensForAttribute, forAttribute: true) ? 1 : 0;
						needParensForAttribute = false;
						normalAttrs++;
						WriteToken('@', LesColorCode.Attribute, Chars.Delimiter);
						Print(attr, AttributeContext, nlContext: NewlineContext.NewlineSafeAfter);
						if (!PS.AtStartOfLine && !MaybeForceLineBreak())
							Space();
					}
				}
			}
			return parenCount;
		}

		private bool MaybeForceLineBreak()
		{
			if (PS.IndexInCurrentLineAfterIndent > _o.ForcedLineBreakThreshold) {
				Newline(true);
				return true;
			}
			return false;
		}

		private void PrintTrailingTrivia(ILNode node, int parenCount, string suffix, NewlineContext nlContext)
		{
			bool newlineSafePoint = (nlContext & NewlineContext.NewlineSafeAfter) != 0;
			// Putting comments inside parens allows them to be associated with an
			// inner node rather than some outer node, which may preserve information.
			// But if there's a semicolon, it's aesthetically better to have 
			// comments afterward. 
			if (parenCount == 0 && !string.IsNullOrEmpty(suffix)) {
				WriteToken(suffix, LesColorCode.Separator, Chars.Delimiter);
				suffix = null;
			}
			var trivia = node.GetTrailingTrivia();
			for (int i = 0; i < trivia.Count; i++)
				MaybePrintTrivia(trivia[i], true, newlineSafePoint);
			for (int i = 0; i < parenCount; i++) {
				Space(_o.SpaceInsideGroupingParens);
				WriteToken(')', LesColorCode.Closer, Chars.Delimiter);
			}
			if (!string.IsNullOrEmpty(suffix))
				WriteToken(suffix, LesColorCode.Separator, Chars.Delimiter);
		}

		bool HasPAttrs(ILNode node)
		{
			foreach (var attr in node.Attrs())
				if (!IsConsumedTrivia(attr))
					return true;
			return false;
		}

		private bool IsConsumedTrivia(ILNode attr)
		{
			return MaybePrintTrivia(attr, false, testOnly: true);
		}
		private bool MaybePrintTrivia(ILNode attr, bool trailing, bool newlineSafePoint = false, bool testOnly = false)
		{
			var name = attr.Name;
			if (!S.IsTriviaSymbol(name))
				return false;
			
			if ((name == S.TriviaRawText) && _o.ObeyRawText)
			{
				if (!testOnly)
					WriteToken(GetRawText(attr), LesColorCode.Unknown, 0);
				return true;
			}
			else if (_o.PrintTriviaExplicitly)
			{
				return false;
			}
			else if (name == S.TriviaNewline)
			{
				if (!testOnly && !_o.OmitSpaceTrivia)
					PrintNewlineTriviaIfPossible(newlineSafePoint, false);
				return true;
			}
			else if ((name == S.TriviaSpaces))
			{
				if (!testOnly && !_o.OmitSpaceTrivia)
					PrintSpaces(GetRawText(attr));
				return true;
			}
			else if (name == S.TriviaSLComment)
			{
				if (!testOnly && !_o.OmitComments) {
					if (trailing && !SB.TryGet(SB.Length-1, ' ').IsOneOf(' ', '\t'))
						WriteOutsideToken('\t');

					var text = GetRawText(attr);
					if (text.Contains('\n') || text.Contains('\r'))
						WriteMLComment(text);
					else {
						WriteToken("//", LesColorCode.Comment, Chars.Punc, Chars.SLComment);
						// Insert zero-width space in the middle of "\\" to avoid ending the comment early
						SB.Append(text.Replace(@"\\", "\\\u200B\\"));
						if (!PrintNewlineTriviaIfPossible(newlineSafePoint, true))
							if (text.EndsWith(@"\"))
								SB.Append(" "); // in case the comment is closed with `\\`, avoid ending in `\\\`
					}
				}
				return true;
			}
			else if (name == S.TriviaMLComment)
			{
				if (!testOnly && !_o.OmitComments) {
					Space(trailing);
					WriteMLComment(GetRawText(attr));
				}
				return true;
			}
			else if (name == S.TriviaAppendStatement)
				return true; // obeyed elsewhere
			if (_o.OmitUnknownTrivia)
				return true; // block printing
			if (!trailing && name == S.TriviaTrailing)
				return true;
			return false;
		}

		private bool PrintNewlineTriviaIfPossible(bool newlineSafePoint, bool avoidExtraNewline)
		{
			if (newlineSafePoint || _inParensOrBracks) {
				Newline(avoidExtraNewline);
				return true;
			}
			return false;
		}

		private void WriteMLComment(string text)
		{
			StartToken(LesColorCode.Comment, Chars.Punc, Chars.Delimiter);
			SB.Append("/*");
			// Print carefully, changing "*/" to "*\" if necessary to avoid ending 
			// the comment early, or adding extra "*/"s to un-nest nested comments.
			int nesting = 0;
			char c, prev_c = '\0';
			for (int i = 0; i < text.Length; i++, prev_c = c) {
				c = text[i];
				if (c == '/' && prev_c == '*' && nesting == 0)
					c = '\\';
				SB.Append(c);
				if (c == '*' && prev_c == '/') {
					nesting++;
					c = '\0'; // ensure `/*/` is not treated the same as `/**/`
				}
				if (c == '/' && prev_c == '*') {
					nesting--;
					c = '\0'; // ensure `*/*` is not treated the same as `*//*`
				}
			}
			for (; nesting >= 0; nesting--)
				SB.Append("*/");
		}

		private void PrintSpaces(string spaces)
		{
			for (int i = 0; i < spaces.Length; i++) {
				char c = spaces[i];
				if (c == '\n') {
					WriteOutsideToken('\n');
				} else if (c == ' ' || c == '\t') {
					WriteOutsideToken(c);
				} else if (c == '\r') {
					Newline();
					if (spaces.TryGet(i + 1) == '\n')
						i++;
				} else
					continue;
			}
		}
		
		static string GetRawText(ILNode rawTextNode)
		{
			object value = rawTextNode.Value;
			if (value == null || value == NoValue.Value) {
				var node = rawTextNode.TryGet(0, null);
				if (node != null)
					value = node.Value;
			}
			return (value ?? rawTextNode.Name).ToString();
		}

		#endregion
	}

	public class Les3PrinterOptions : LNodePrinterOptions
	{
		public Les3PrinterOptions() : this(null) { }
		public Les3PrinterOptions(ILNodePrinterOptions options)
		{
			WarnAboutUnprintableLiterals = true;
			SpaceAfterComma = true;
			ForcedLineBreakThreshold = 120;
			if (options != null)
				CopyFrom(options);
		}

		public override bool CompactMode
		{
			get { return base.CompactMode; }
			set {
				if (base.CompactMode = value) {
					SpacesBetweenAppendedStatements = false;
					SpaceAroundInfixStopPrecedence = LesPrecedence.SuperExpr.Lo;
					SpaceAfterPrefixStopPrecedence = LesPrecedence.SuperExpr.Lo;
					SpaceInsideArgLists = false;
					SpaceInsideGroupingParens = false;
					SpaceInsideTuples = false;
					SpaceInsideListBrackets = false;
				} else {
					SpacesBetweenAppendedStatements = true;
					SpaceAroundInfixStopPrecedence = LesPrecedence.Range.Lo;
					SpaceAfterPrefixStopPrecedence = LesPrecedence.Range.Lo;
				}
				SpaceAfterComma = value;
				SpacesBetweenAppendedStatements = value;
			}
		}

		/// <summary>Whether to print a space inside square brackets for lists <c>[ ... ]</c>.</summary>
		public bool SpaceInsideListBrackets { get; set; }

		/// <summary>Whether to print a space inside argument lists like <c>f( ... )</c>.</summary>
		public bool SpaceInsideArgLists { get; set; }

		/// <summary>Whether to print a space inside grouping parentheses <c>( ... )</c>.</summary>
		public bool SpaceInsideGroupingParens { get; set; }

		/// <summary>Whether to print a space inside tuples like <c>f( ...; )</c>.</summary>
		public bool SpaceInsideTuples { get; set; }

		/// <summary>Whether to print a space after each comma in an argument list.</summary>
		/// <remarks>Initial value: true</remarks>
		public bool SpaceAfterComma { get; set; }

		/// <summary>Introduces extra parenthesis to express precedence, without
		/// using an empty attribute list [] to allow perfect round-tripping.</summary>
		/// <remarks>For example, the Loyc tree <c>x * @+(a, b)</c> will be printed 
		/// <c>x * (a + b)</c>, which is a slightly different tree (the parenthesis
		/// add the trivia attribute %inParens.)</remarks>
		public bool AllowExtraParenthesis {
			get { return base.AllowChangeParentheses; }
			set { base.AllowChangeParentheses = value; }
		}

		/// <summary>When this flag is set, space trivia attributes are ignored
		/// (e.g. <see cref="CodeSymbols.TriviaSpaceAfter"/>).</summary>
		public bool OmitSpaceTrivia { get; set; }

		/// <summary>Whether to print a warning when an "unprintable" literal is 
		/// encountered. In any case the literal is converted to a string, placed 
		/// in double quotes and prefixed by the unqualified Type of the Value.</summary>
		/// <remarks>Initial value: true</remarks>
		public bool WarnAboutUnprintableLiterals { get; set; }
		
		/// <summary>Causes raw text to be printed verbatim, as the EC# printer does.
		/// When this option is false, raw text trivia is printed as a normal 
		/// attribute.</summary>
		public bool ObeyRawText { get; set; }

		/// <summary>Whether to add a space between multiple statements printed on
		/// one line (initial value: true).</summary>
		public bool SpacesBetweenAppendedStatements = true;

		/// <summary>If true, a semicolon is used in addition to the usual newline to 
		/// terminate each expression inside braced blocks and at the top level.</summary>
		/// <remarks>Regardless of this flag, a semicolon is forced to appear when a 
		/// node uses <see cref="CodeSymbols.TriviaAppendStatement"/> to put multiple 
		/// expressions on one line.</remarks>
		public bool UseRedundantSemicolons { get; set; }

		/// <summary>
		/// Print purely in prefix notation, e.g. <c>`'+`(2,3)</c> instead of <c>2 + 3</c>.
		/// </summary>
		public bool PrefixNotationOnly { get; set; }

		/// <summary>The printer avoids printing spaces around infix (binary) 
		/// operators that have the specified precedence or higher.</summary>
		/// <seealso cref="LesPrecedence"/>
		public int SpaceAroundInfixStopPrecedence = LesPrecedence.Multiply.Hi + 1;

		/// <summary>The printer avoids printing spaces after prefix operators 
		/// that have the specified precedence or higher.</summary>
		public int SpaceAfterPrefixStopPrecedence = LesPrecedence.Multiply.Hi + 1;

		/// <summary>Although the LES3 printer is not designed to insert line breaks
		/// mid-expression or to keep lines under a certain length, this option can 
		/// avoid extremely long lines in some cases, by (1) inserting line breaks 
		/// after commas in argument lists, or after very long attribute lists, and 
		/// (2) ignoring the <see cref="NodeStyle.OneLiner"/> flag or 
		/// <see cref="CodeSymbols.TriviaAppendStatement"/> attribute when an 
		/// expression within a braced block starts after this column on a line.
		/// </summary>
		/// <remarks>
		/// The default value is 120.
		/// <para/>
		/// Setting the threshold to zero forces all "statements" (expressions 
		/// in braces) to appear on a new line. Lines can still be arbitrarily long 
		/// with this option, since breaks are only added at the end of expressions 
		/// within a braced block.
		/// </remarks>
		public int ForcedLineBreakThreshold { get; set; }

		char? _digitSeparator = '\'';
		/// <summary>Sets the "thousands" or other digit separator for numeric 
		/// literals. Valid values are null (to disable the separator), underscore (_) 
		/// and single quote (').</summary>
		/// <exception cref="ArgumentException">Invalid property value.</exception>
		/// <remarks>
		/// For decimal numbers, this value separates thousands (e.g. 12'345'678).
		/// For hex numbers, it separates groups of four digits (e.g. 0x1234'5678).
		/// For binary numbers, it separates groups of eight digits.
		/// </remarks>
		public char? DigitSeparator {
			get { return _digitSeparator; }
			set {
				if (value.HasValue && value.Value != '_' && value.Value != '\'')
					throw new ArgumentException("DigitSeparator must be '_' or single quote.".Localized());
				_digitSeparator = value;
			}
		}
	}
}
