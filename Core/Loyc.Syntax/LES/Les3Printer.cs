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

	// TODO: Implement smart line breaks o that PreferredLineWidth works
	// TODO: Support .dot expressions
	// TODO: Add comments and line breaks to tree produced by parser (multi-language algorithm?)
	// TODO: Verify # is treated as ident char or not depending on feedback
	// TODO: Switch over from `#` to `.` prefix as the keyword marker?
	public class Les3Printer : ILNodeVisitor
	{
		#region Properties

		public StringBuilder SB
		{ 
			get { return PS.S; }
			set { CheckParam.IsNotNull("value", value); PS.S = value; }
		}

		/// <summary>The printer tries to keep the line width under this size (note: 
		/// it won't always succeed and it is only willing to put line breaks in 
		/// certain locations).</summary>
		public int PreferredLineWidth
		{
			get { return _preferredLineWidth; }
			set { _preferredLineWidth = value; }
		}
		int _preferredLineWidth = 80;

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
		/// add the trivia attribute #trivia_inParens.)</remarks>
		public bool AllowExtraParenthesis { get; set; }

		/// <summary>
		/// Print purely in prefix notation, e.g. <c>`'+`(2,3)</c> instead of <c>2 + 3</c>.
		/// </summary>
		public bool PrefixNotationOnly { get; set; }

		public int SpaceAroundInfixStopPrecedence = LesPrecedence.Power.Lo;
		public int SpaceAfterPrefixStopPrecedence = LesPrecedence.Prefix.Lo;
		
		/// <summary>When this flag is set, space trivia attributes are ignored
		/// (e.g. <see cref="CodeSymbols.TriviaSpaceAfter"/>).</summary>
		public bool OmitSpaceTrivia { get; set; }

		/// <summary>When this flag is set, comment trivia attributes are ignored
		/// (e.g. <see cref="CodeSymbols.TriviaSLCommentAfter"/>).</summary>
		public bool OmitComments { get; set; }

		/// <summary>Whether to print a warning when an "unprintable" literal is 
		/// encountered. In any case the literal is converted to a string, placed 
		/// in double quotes and prefixed by the unqualified Type of the Value.</summary>
		/// <remarks>Initial value: true</remarks>
		public bool WarnAboutUnprintableLiterals { get; set; }
		
		/// <summary>Causes unknown trivia (other than comments, spaces and raw 
		/// text) to be dropped from the output.</summary>
		public bool OmitUnknownTrivia { get; set; }

		/// <summary>Causes comments and spaces to be printed as attributes in order 
		/// to ensure faithful round-trip parsing. By default, only "raw text" and
		/// unrecognized trivia is printed this way. Note: #trivia_inParens is 
		/// always printed as parentheses.</summary>
		public bool PrintTriviaExplicitly { get; set; }

		/// <summary>Causes raw text to be printed verbatim, as the EC# printer does.
		/// When this option is false, raw text trivia is printed as a normal 
		/// attribute.</summary>
		public bool ObeyRawText { get; set; }

		/// <summary>Target for warning and error messages.</summary>
		public IMessageSink MessageSink { get; set; }

		#endregion

		#region New() / constructor, fields, and Print()

		public static Les3Printer New(StringBuilder target = null, IMessageSink messageSink = null, string indentString = "\t", string lineSeparator = "\n")
		{
			return new Les3Printer(new PrinterState(target, indentString, lineSeparator), messageSink);
		}
		public Les3Printer(PrinterState ps, IMessageSink messageSink)
		{
			PS = ps;
			if (PS.S == null)
				PS.S = new StringBuilder();
			MessageSink = messageSink;
			WarnAboutUnprintableLiterals = true;
			SpaceAfterComma = true;
		}

		protected PrinterState PS;
		protected LNode _n;
		protected Precedence _context = Precedence.MinValue;
		protected Chars _curSet = 0;

		public static readonly LNodePrinter Printer = Print;

		public static void Print(LNode node, StringBuilder target, IMessageSink errors, object mode = null, string indentString = "\t", string lineSeparator = "\n")
		{
			CheckParam.IsNotNull("target", target);
			var p = new Les3Printer(new PrinterState(target, indentString, lineSeparator), errors);
			p.Print(node);
		}

		public StringBuilder Print(LNode node, string terminator = null)
		{
			_n = node;
			int parenCount = PrintPrefixTriviaAndAttrs(node);
			node.Call(this);
			PrintSuffixTrivia(node, parenCount, terminator);
			_n = null;
			return SB;
		}

		protected void Print(LNode node, Precedence context, string terminator = null)
		{
			var oldContext = _context;
			_context = context;
			Print(node, terminator);
			_context = oldContext;
		}

		public override string ToString()
		{
			return SB.ToString();
		}

		#endregion

		#region Helper functions

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
			Minus = 8,
			Dot = 16,
			NumberStart = IdStart | Minus | Dot, // Numbers can start minus/dot (- .) and charset overlaps Id
			NegativeNumberStart = Punc | Minus,  // Numbers that starts with minus (-) must not touch other Punc
			NumberEnd   = Id | BQId | Dot, // Numbers can end in backquotes (``), letters or digits, and '.' afterward could create ambiguity
			IdAndPunc = Id | Punc | Minus | Dot,
			DoubleQuote = 32,
			BQId = 64,                     // `backquoted identifier`
			StringStart = DoubleQuote | Id | BQId, // A string can start with an Id so we can't put a string right after an Id
			At = 128,
		}

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
		protected void StartToken(LesColorCode kind, Chars charSet) { StartToken(kind, charSet, charSet); }
		protected void StartToken(LesColorCode kind, Chars startSet, Chars endSet)
		{
			Space((startSet & _curSet) != 0);
			_curSet = endSet;
			StartToken(kind);
		}

		protected virtual void StartToken(LesColorCode kind)
		{
		}

		protected void EndToken()
		{
			_curSet = Chars.Delimiter;
			StartToken(LesColorCode.None);
		}

		protected void Newline()
		{
			EndToken();
			PS.Newline();
		}

		protected void Space(bool condition = true)
		{
			if (condition) {
				EndToken();
				SB.Append(' ');
			}
		}

		private bool HasSimpleTarget(LNode node)
		{
			if (PrintTriviaExplicitly)
				return node.HasSimpleHead() && node.Target.AttrCount == 0;
			else
				return node.HasSimpleHeadWithoutPAttrs();
		}
		
		#endregion

		#region Printing of identifiers

		static readonly Symbol sy_true = (Symbol)"true", sy_false = (Symbol)"false", sy_null = (Symbol)"null";

		public void Visit(IdNode node)
		{
			PrintIdCore(node.Name, startToken: true);
		}

		public static bool IsNormalIdentifier(Symbol name)
		{
			return LesNodePrinter.IsNormalIdentifier(name) && !name.Name.StartsWith("#");
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
					int c = s.PopFront(out fail);
					if (fail) break;

					EscapeC flags = EscapeC.Control;
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
					ParseHelpers.EscapeCStyle(c, SB, flags, quoteType);
				}
			}
			SB.Append(quoteType);
		}

		#endregion

		#region Printing of literals

		public void Visit(LiteralNode node)
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
		protected static Symbol _number = GSymbol.Get("number");

		#endregion

		private void PrintLiteralCore(object value, NodeStyle style)
		{
			Symbol tmarker = null;
			if (value is CustomLiteral) {
				var sp = (CustomLiteral)value;
				tmarker = sp.TypeMarker;
				value = sp.Value;
				if (tmarker == null)
					MessageSink.Write(Severity.Warning, _n, "Les3Printer: SpecialLiteral.LiteralType is null");
			}

			Action<Les3Printer, object, NodeStyle, Symbol> printHelper;
			if (value == null) {
				if (tmarker != null)
					MessageSink.Write(Severity.Warning, _n, "Les3Printer: Null SpecialLiteral will print as 'null'");
				WriteToken("null", LesColorCode.KeywordLiteral, Chars.Id);
			} else if (LiteralPrinters.TryGetValue(value.GetType().TypeHandle, out printHelper)) {
				printHelper(this, value, style, tmarker);
			} else {
				if (tmarker == null)
					tmarker = (Symbol)MemoizedTypeName.Get(value.GetType());
				if (WarnAboutUnprintableLiterals)
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
					} else if (prefix.Name == "@@" && LesPrecedenceMap.IsExtendedOperator(text, "")) {
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
				StartToken(LesColorCode.Number, Chars.NegativeNumberStart, Chars.NumberEnd);
				value = -value;
				SB.Append('-');
			} else
				StartToken(LesColorCode.Number, Chars.NumberStart, Chars.NumberEnd);
			PrintIntegerCore((ulong)value, style, suffix);
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
				ParseHelpers.AppendIntegerTo(SB, value, "0x", 16, 4, '_');
				forceQuote |= suffix0 >= 'a' && suffix0 <= 'f' || suffix0 >= 'A' && suffix0 <= 'F';
			} else if ((style & NodeStyle.BaseStyleMask) == NodeStyle.BinaryLiteral)
				ParseHelpers.AppendIntegerTo(SB, value, "0b", 2, 8, '_');
			else
				ParseHelpers.AppendIntegerTo(SB, value, "", 10, 3, '_');
			if (suffix != null)
				PrintIdCore(suffix, startToken: false, forceQuote: forceQuote);
		}

		const string NaNPrefix = "@@nan.";
		const string PositiveInfinityPrefix = "@@inf.";
		const string NegativeInfinityPrefix = "@@-inf.";

		void PrintFloat(float value, NodeStyle style, Symbol suffix)
		{
			if (float.IsNaN(value)) {
				WriteToken(NaNPrefix, LesColorCode.KeywordLiteral, Chars.At, Chars.IdAndPunc);
			} else if (float.IsPositiveInfinity(value)) {
				WriteToken(PositiveInfinityPrefix, LesColorCode.KeywordLiteral, Chars.At, Chars.IdAndPunc);
			} else if (float.IsNegativeInfinity(value)) {
				WriteToken(NegativeInfinityPrefix, LesColorCode.KeywordLiteral, Chars.At, Chars.IdAndPunc);
			} else {
				StartToken(LesColorCode.Number, value < 0 ? Chars.NegativeNumberStart : Chars.NumberStart, Chars.NumberEnd);
				// TODO: support hex & binary floats, and digit separators
				// The "R" round-trip specifier makes sure that no precision is lost, and
				// that parsing a printed version of double.MaxValue is possible.
				SB.Append(value.ToString("R", CultureInfo.InvariantCulture));
			}
			PrintIdCore(suffix, startToken: false);
		}

		void PrintDouble(double value, NodeStyle style, Symbol suffix)
		{
			if (double.IsNaN(value)) {
				WriteToken(NaNPrefix, LesColorCode.KeywordLiteral, Chars.At, Chars.IdAndPunc);
			} else if (double.IsPositiveInfinity(value)) {
				WriteToken(PositiveInfinityPrefix, LesColorCode.KeywordLiteral, Chars.At, Chars.IdAndPunc);
			} else if (double.IsNegativeInfinity(value)) {
				WriteToken(NegativeInfinityPrefix, LesColorCode.KeywordLiteral, Chars.At, Chars.IdAndPunc);
			} else {
				// TODO: support hex & binary floats, and digit separators
				// The "R" round-trip specifier makes sure that no precision is lost, and
				// that parsing a printed version of double.MaxValue is possible.
				StartToken(LesColorCode.Number, value < 0 ? Chars.NegativeNumberStart : Chars.NumberStart, Chars.NumberEnd);
				var asStr = value.ToString("R", CultureInfo.InvariantCulture);
				SB.Append(asStr);
				if (suffix == _D) {
					if (!asStr.Contains(".") && !asStr.Contains("e"))
						SB.Append(".0");
					return;
				}
			}
			PrintIdCore(suffix, startToken: false);
		}

		#endregion

		#region Printing calls: main Visit() method

		public void Visit(CallNode node)
		{
			// Note: Attributes, if any, have already been printed by this point
			bool parens = false;
			switch (PrefixNotationOnly ? NodeStyle.PrefixNotation : node.BaseStyle)
			{
				case NodeStyle.Operator:
				case NodeStyle.Default:
					// Figure out if this node can be treated as an operator and if 
					// so, whether it's a suffix operator.
					if (!HasSimpleTarget(node))
						goto default;

					Symbol opName = node.Name;
					if (!TryToPrintCallAsSpecialOperator(opName, node))
					{
						if (!node.ArgCount.IsInRange(1, 2) || !LesPrecedenceMap.IsExtendedOperator(opName.Name))
							goto default;
						var shape = (OperatorShape)node.ArgCount;
						if (node.ArgCount == 1 && LesPrecedenceMap.IsSuffixOperatorName(opName, out opName))
							shape = OperatorShape.Suffix;

						parens = PrintCallAsNormalOpOrPrefixNotation(shape, opName, node);
					}
					break;
				case NodeStyle.Special:
					if (!TryToPrintCallAsKeywordExpression(node))
						goto default;
					break;
				case NodeStyle.PrefixNotation:
					parens = PrintPrefixNotation(node, purePrefix: true);
					break;
				default:
					parens = PrintPrefixNotation(node, purePrefix: false);
					break;
			}
			if (parens) WriteToken(')', LesColorCode.Closer, Chars.Delimiter);
		}

		private bool TryToPrintCallAsKeywordExpression(CallNode node)
		{
			if (!LesPrecedence.SuperExpr.CanAppearIn(_context) || !IsKeywordExpression(node))
				return false;

			var args = node.Args;
			WriteToken(node.Name.Name, LesColorCode.Keyword, Chars.IdAndPunc);
			if (args.Count == 0)
				return true;

			Space();
			Print(args[0], LesPrecedence.SuperExpr);
			if (args.Count <= 1)
				return true;

			Space();
			PrintBracedBlock(args[1]);
			for (int i = 2; i < args.Count; i++) {
				Space();
				PrintContinuator(args[i]);
			}
			return true;
		}

		private bool IsKeywordExpression(CallNode node)
		{
			if (!HasSimpleTarget(node) || !LesPrecedenceMap.IsExtendedOperator(node.Name.Name, "#"))
				return false;
			var args = node.Args;
			if (args.Count <= 1)
				return true;
			var block = args[1];
			if (!block.Calls(S.Braces) || !HasSimpleTarget(block))
				return false;
			for (int i = 2; i < args.Count; i++)
				if (!IsContinuator(args[i]))
					return false;

			Debug.Assert(node.Target.IsId);
			return true;
		}

		#endregion

		#region Printing calls: in prefix notation or as a block call

		bool PrintPrefixNotation(CallNode node, bool purePrefix)
		{
			bool parens = AddParenIf(!IsAllowedHere(LesPrecedence.Primary));

			Print(node.Target, LesPrecedence.Primary.LeftContext(_context));
			PrintArgList(node, purePrefix);

			return parens;
		}

		internal static readonly HashSet<Symbol> ContinuatorOps = Les3Parser.ContinuatorOps;
		internal static readonly Dictionary<object, Symbol> Continuators = Les3Parser.Continuators;

		bool IsContinuator(LNode lastArg)
		{
			if (lastArg.ArgCount > 0 && HasSimpleTarget(lastArg) && ContinuatorOps.Contains(lastArg.Name))
				return true;
			return false;
		}

		void PrintArgList(LNode node, bool purePrefix, bool ignoreContinuators = false)
		{
			var args = node.Args;
			int numContinuators = 0;
			LNode braces = null;

			if (!purePrefix && args.Count > 0) {
				// Detect a block call, which should have something in 
				// braces, followed by continuators.
				if (!ignoreContinuators)
					while (args.Count > 1 && IsContinuator(args.Last)) {
						numContinuators++;
						args.Pop();
					}
				if (args.Count == 0 || !(braces = args.Pop()).Calls(CodeSymbols.Braces) || !HasSimpleTarget(braces)) {
					braces = null;
					numContinuators = 0; // braces are required prior to continuators
					args = node.Args;
				}
			}

			if (!(braces != null && args.Count == 0)) {
				if (braces != null) {
					// In accordance with LES tradition, prefer to print
					// `if (x > y) {...}` instead of `if(x > y) {...}`
					Space(true);
				}
				var style = (node.Style & NodeStyle.Alternate) != 0 ? ArgListStyle.BracedBlock : ArgListStyle.Normal;
				PrintArgListCore(args, '(', ')', style, SpaceInsideArgLists);
			}

			if (braces != null) {
				Space();
				PrintBracedBlock(braces);
				args = node.Args;
				for (int i = args.Count - numContinuators; i < args.Count; i++) {
					Space();
					PrintContinuator(args[i]);
				}
			}
		}

		private void PrintContinuator(LNode continuator)
		{
			Debug.Assert(continuator.Name.Name.StartsWith("'"));
			WriteToken(continuator.Name.Name.Substring(1), LesColorCode.Keyword, Chars.Id);
			PrintArgList(continuator, false, ignoreContinuators: true);
		}

		enum ArgListStyle
		{
			Normal = 0,     // normal arg list
			Semicolons = 1, // semicolons as separator (tuple)
			BracedBlock = 2 // semicolons and newlines (braced block)
		}
		void PrintArgListCore(VList<LNode> args, char leftDelim, char rightDelim, ArgListStyle style, bool spacesInside, int startIndex = 0)
		{
			WriteToken(leftDelim, LesColorCode.Opener, Chars.Delimiter);
			Space(spacesInside);
			if (style == ArgListStyle.BracedBlock)
				PS.Indent();

			string semicolon = (style == ArgListStyle.Normal ? null : ";");
			for (int i = startIndex; i < args.Count; i++) {
				if (style == ArgListStyle.BracedBlock)
					PS.Newline();
				else if (i != startIndex)
					Space(SpaceAfterComma);
				var stmt = args[i];
				Print(stmt, Precedence.MinValue, semicolon ?? (i + 1 != args.Count ? "," : null));
			}

			if (style == ArgListStyle.BracedBlock)
				PS.Newline(-1);
			Space(spacesInside);
			WriteToken(rightDelim, LesColorCode.Closer, Chars.Delimiter);
		}

		#endregion

		#region Printing normal operators: Prefix, suffix, infix

		// Prints an operator with a "normal" shape (infix, prefix or suffix)
		// or in prefix notation if that fails. Returns true if closing ')' needed.
		private bool PrintCallAsNormalOpOrPrefixNotation(OperatorShape shape, Symbol opName, CallNode node)
		{
			// Check if this operator is allowed here. For example, if operator '+ 
			// appears within an argument to '*, as in (2 + 3) * 5, it's not 
			// allowed without parentheses. Also, unary extended ops (e.g. 'foo) 
			// are not supported.
			Precedence prec = LesPrecedenceMap.Default.Find(shape, opName);
			if (shape != OperatorShape.Infix && (prec == LesPrecedence.Other || !LesPrecedenceMap.IsNaturalOperator(opName.Name)))
				return PrintPrefixNotation(node, false);
			bool allowed = IsAllowedHere(prec);
			if (!allowed && !AllowExtraParenthesis && IsAllowedHere(LesPrecedence.Primary))
				return PrintPrefixNotation(node, false);

			bool parens = AddParenIf(!allowed);

			switch (shape) {
				case OperatorShape.Prefix:
					Debug.Assert(node.ArgCount == 1);
					var inner = node.Args[0];
					PrintOpName(opName);
					Space(prec.Lo < SpaceAfterPrefixStopPrecedence);
					Print(inner, prec.RightContext(_context));
					break;
				case OperatorShape.Suffix:
					Debug.Assert(node.ArgCount == 1);
					Print(node.Args[0], prec.LeftContext(_context));
					PrintOpName(opName);
					break;
				default:
					Debug.Assert(node.ArgCount == 2);
					Print(node.Args[0], prec.LeftContext(_context));
					Space(prec.Lo < SpaceAroundInfixStopPrecedence);
					PrintOpName(opName);
					Space(prec.Lo < SpaceAroundInfixStopPrecedence);
					Print(node.Args[1], prec.RightContext(_context));
					break;
			}
			return parens;
		}
		bool PrintOpName(Symbol opName)
		{
			Debug.Assert(opName.Name.StartsWith("'"));
			if (LesPrecedenceMap.IsNaturalOperator(opName.Name)) {
				char first = opName.Name[1], last = opName.Name[opName.Name.Length - 1];
				StartToken(LesColorCode.Operator,
					Chars.Punc | (first == '-' ? Chars.Minus : 
					              first == '.' ? Chars.Dot : 0),
					Chars.Punc | (last == '-' ? Chars.Minus : 
					              last == '.' ? Chars.Dot : 0));
				SB.Append(opName.Name, 1, opName.Name.Length - 1);
				return true;
			} else {
				Debug.Assert(LesPrecedenceMap.IsExtendedOperator(opName.Name));
				WriteToken(opName.Name, LesColorCode.Operator, Chars.IdAndPunc);
				return false;
			}
		}

		private bool AddParenIf(bool cond)
		{
			if (cond) {
				WriteToken('(', LesColorCode.Opener, Chars.Delimiter);
				Space(SpaceInsideGroupingParens);
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

		private bool TryToPrintCallAsSpecialOperator(Symbol opName, CallNode node)
		{
			if (opName == CodeSymbols.Braces) { // {...}
				PrintBracedBlock(node);
			} else if (opName == CodeSymbols.Array) { // [...]
				PrintArgListCore(node.Args, '[', ']', 
					ArgListStyle.Normal, SpaceInsideListBrackets);
			} else if (opName == CodeSymbols.Tuple) { // (;;)
				PrintArgListCore(node.Args, '(', ')', ArgListStyle.Semicolons, SpaceInsideTuples);
			} else if (opName == CodeSymbols.IndexBracks && node.ArgCount > 0) { // foo[...]
				Print(node.Args[0], LesPrecedence.Primary.LeftContext(_context));
				PrintArgListCore(node.Args, '[', ']', (node.Style & NodeStyle.Alternate) != 0
					? ArgListStyle.Semicolons : ArgListStyle.Normal, SpaceInsideArgLists, startIndex: 1);
			} else if (opName == CodeSymbols.Of) {
				var args = node.Args;
				Print(args[0], LesPrecedence.Primary.LeftContext(_context));
				WriteToken('!', LesColorCode.Operator, Chars.Punc);
				if (args.Count == 2 && args[1].IsId && args[1].AttrCount == 0)
					Visit((IdNode)args[1]);
				else
					PrintArgListCore(args, '(', ')', ArgListStyle.Normal, SpaceInsideArgLists, startIndex: 1);
			} else
				return false;
			return true;
		}

		private void PrintBracedBlock(LNode braces)
		{
			Debug.Assert(braces.Calls(CodeSymbols.Braces));
			PrintArgListCore(braces.Args, '{', '}', ArgListStyle.BracedBlock, false);
		}

		#endregion

		#region Printing attributes (with trivia)

		private int PrintPrefixTriviaAndAttrs(LNode node)
		{
			var A = node.Attrs;
			int parenCount = 0;
			bool needParensForAttribute = !_context.CanParse(LesPrecedence.SuperExpr);
			for (int i = 0; i < A.Count; i++) {
				var attr = A[i];

				if (!DetectAndMaybePrintTrivia(attr, false, ref parenCount)) {
					// Print as normal attribute
					parenCount += AddParenIf(needParensForAttribute) ? 1 : 0;
					needParensForAttribute = false;
					WriteToken('@', LesColorCode.Attribute, Chars.Delimiter);
					Print(attr, Precedence.MaxValue);
					Space();
				}
			}
			return parenCount;
		}

		private void PrintSuffixTrivia(LNode node, int parenCount, string terminator)
		{
			// Putting comments inside parens allows them to be associated with an
			// inner node rather than some outer node, which may preserve information.
			// But if there's a semicolon, it's aesthetically better to have comments 
			// afterward. 
			if (parenCount == 0 && terminator == ";") {
				WriteToken(terminator, LesColorCode.Separator, Chars.Delimiter);
				terminator = null;
			}
			var A = node.Attrs;
			for (int i = 0; i < A.Count; i++) {
				var attr = A[i];
				int _ = 0;
				DetectAndMaybePrintTrivia(attr, true, ref _);
			}
			for (int i = 0; i < parenCount; i++) {
				Space(SpaceInsideGroupingParens);
				WriteToken(')', LesColorCode.Closer, Chars.Delimiter);
			}
			if (!string.IsNullOrEmpty(terminator))
				WriteToken(terminator, LesColorCode.Separator, Chars.Delimiter);
		}


		// Checks if the specified attribute is trivia and, if so, prints it if we're
		// at the matching location (e.g. suffixMode=true means we're after the node 
		// right now and want to print only suffix trivia). Returns true if the 
		// attribute is trivia and should NOT printed as a normal attribute.
		bool DetectAndMaybePrintTrivia(LNode attr, bool suffixMode, ref int parenCount)
		{
			var name = attr.Name;
			bool suffix;
			if ((suffix = name == S.TriviaRawTextAfter) || name == S.TriviaRawTextBefore) {
				if (!ObeyRawText)
					return false;
				if (suffix == suffixMode)
					WriteToken(GetRawText(attr), LesColorCode.Unknown, Chars.Delimiter);
				return true;
			} else if (name == S.TriviaInParens) {
				if (!suffixMode) {
					if (!_context.CanParse(LesPrecedence.Substitute)) {
						// Inside @: outer parens are expected. Add a second 
						// pair so that reparsing preserves the in-parens trivia.
						WriteToken('(', LesColorCode.Opener, Chars.Delimiter);
						parenCount++;
					}
					WriteToken('(', LesColorCode.Opener, Chars.Delimiter);
					parenCount++;
					Space(SpaceInsideGroupingParens);
				}
				return true;
			} else if (!PrintTriviaExplicitly) {
				if ((suffix = name == S.TriviaSpaceAfter) || name == S.TriviaSpaceBefore) {
					if (!OmitSpaceTrivia && suffix == suffixMode)
						PrintSpaces(GetRawText(attr));
					return true;
				} else if ((suffix = name == S.TriviaSLCommentBefore) || name == S.TriviaSLCommentAfter) {
					if (!OmitComments && suffix == suffixMode) {
						Space(suffixMode);
						WriteToken("//", LesColorCode.Comment, Chars.Punc, Chars.Delimiter);
						SB.Append(GetRawText(attr));
						Newline();
					}
					return true;
				} else if ((suffix = name == S.TriviaMLCommentAfter) || name == S.TriviaMLCommentBefore) {
					if (!OmitComments && suffix == suffixMode) {
						Space(suffixMode);
						StartToken(LesColorCode.Comment, Chars.Punc, Chars.Punc);
						SB.Append("/*");
						SB.Append(GetRawText(attr));
						SB.Append("*/");
					}
					return true;
				} else if (S.IsTriviaSymbol(name)) {
					return OmitUnknownTrivia;
				}
			}

			return false;
		}

		private void PrintSpaces(string spaces)
		{
			bool first = true;
			for (int i = 0; i < spaces.Length; i++) {
				char c = spaces[i];
				if (c == ' ' || c == '\t') {
					SB.Append(c);
				} else if (c == '\n')
					Newline();
				else if (c == '\r') {
					Newline();
					if (spaces.TryGet(i + 1) == '\r')
						i++;
				} else
					continue;
				if (first)
					EndToken();
				first = false;
			}
		}

		/*static bool IsKnownTrivia(Symbol name)
		{
			return name == S.TriviaRawTextBefore || name == S.TriviaRawTextAfter ||
				   name == S.TriviaSLCommentBefore || name == S.TriviaSLCommentAfter ||
				   name == S.TriviaMLCommentBefore || name == S.TriviaMLCommentAfter ||
				   name == S.TriviaSpaceBefore || name == S.TriviaSpaceAfter;
		}*/
		
		static string GetRawText(LNode rawTextNode)
		{
			object value = rawTextNode.Value;
			if (value == null || value == NoValue.Value) {
				var node = rawTextNode.Args[0, null];
				if (node != null)
					value = node.Value;
			}
			return (value ?? rawTextNode.Name).ToString();
		}

		#endregion
	}
}
