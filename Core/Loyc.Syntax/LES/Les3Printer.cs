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
using Loyc.Collections.MutableListExtensionMethods;
using Loyc.Syntax.Impl;
using Loyc.Syntax.Lexing;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Syntax.Les
{
	public partial class Les3Printer
	{
		#region Properties

		public StringBuilder SB
		{ 
			get { return _out.StringBuilder; }
			set { CheckParam.IsNotNull("value", value); _out.StringBuilder = value; }
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
			_out = new LNodePrinterHelper(target ?? new StringBuilder(), _o.SaveRange, true, _o.IndentString ?? "\t", newline);
		}

		protected LNodePrinterHelper _out;
		protected ILNode _n;
		protected Precedence _context = Precedence.MinValue;
		protected NewlineContext _nlContext = NewlineContext.NewlineUnsafe;
		protected bool _inParensOrBracks = false;

		internal StringBuilder Print(IEnumerable<ILNode> list)
		{
			var neglist = list as INegListSource<ILNode> ?? list.ToList().AsNegList(0);
			PrintStmtList(new NegListSlice<ILNode>(neglist), false);

			if (_out.IsAtStartOfLine)
				_out.RevokeNewlinesSince(_newlineCheckpoint); // if optional newline not yet committed
			_out.CommitNewlines();
			return SB;
		}

		public StringBuilder Print(ILNode node, string suffix = null)
		{
			Print(node, Precedence.MinValue, suffix, NewlineContext.NewlineSafeBefore | NewlineContext.NewlineSafeAfter);

			if (_out.IsAtStartOfLine)
				_out.RevokeNewlinesSince(_newlineCheckpoint); // if optional newline not yet committed
			_out.CommitNewlines();
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
			try {
				_context = context;
				_nlContext = nlContext;
				PrintCore(node, suffix);
			} finally {
				_context = oldContext;
				_nlContext = oldNLContext;
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
		//   the space character. For example, a number followed by a dot operator 
		//   should conservatively have a space before the dot. The need for a space 
		//   is detected using flags in the Chars enum.
		// - If a space appears before a dot operator, a space may also be required 
		//   after the dot so that the parser doesn't see a keyword token.
		// - If one statement ends with a keyword expression and the next begins
		//   with braces, a semicolon is required after the first statement to avoid
		//   the possibility that the braces will be parsed as part of the keyword
		//   expression (alternative: begin the second statement with `@ `).
		// - `%newline` must be ignored in some cases because a newline in 
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
			NumberEnd = Id | Dot, // Numbers can end in letters or digits, and '.' afterward could create ambiguity
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
			NewlineSensitive = 4,
			StatementLevel = NewlineSafeBefore | NewlineSafeAfter | NewlineSensitive,
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
				_out.Write(@"\\"); // terminate single-line comment in order to print something after it
			Space((startSet & _curSet) != 0);
			_curSet = endSet;
			StartToken(kind);
		}

		#endregion

		#region Helper functions

		protected void WriteToken(char firstChar, LesColorCode kind, Chars tokenSet)
		{
			StartToken(kind, tokenSet);
			_out.Write(firstChar);
		}
		protected void WriteToken(string text, LesColorCode kind, Chars tokenSet)
		{
			StartToken(kind, tokenSet);
			_out.Write(text);
		}
		protected void WriteToken(string text, LesColorCode kind, Chars startSet, Chars endSet)
		{
			StartToken(kind, startSet, endSet);
			_out.Write(text);
		}

		protected virtual void StartToken(LesColorCode kind)
		{
		}

		protected void WriteOutsideToken(char space)
		{
			if (_curSet == Chars.SLComment)
				_out.Write(@"\\");
			if (_curSet != Chars.Space) {
				_curSet = Chars.Space;
				StartToken(LesColorCode.None);
			}
			_out.Write(space);
		}

		LNodePrinterHelperLocation _newlineCheckpoint;

		protected void Newline(bool avoidExtraNewline = false)
		{
			if (_curSet != Chars.Space) {
				_curSet = Chars.Space;
				StartToken(LesColorCode.None);
			}
			if (avoidExtraNewline) {
				_out.NewlineIsRequiredHere();
			} else {
				_out.CommitNewlines();
				_newlineCheckpoint = _out.NewlineAfterCheckpoint();
			}
		}

		protected void Space(bool condition = true)
		{
			if (condition)
				WriteOutsideToken(' ');
		}

		private bool HasTargetIdWithoutPAttrs(ILNode node)
		{
			Debug.Assert(node.IsCall());
			var t = node.Target;
			if (!t.IsId())
				return false;
			return !HasPAttrs(t);
		}

		#endregion

		#region Printing of identifiers

		static readonly Symbol sy_true = (Symbol)"true", sy_false = (Symbol)"false", sy__ = (Symbol)"_";
		static readonly Symbol sy_null = (Symbol)"null", sy_bool = (Symbol)"bool", sy_c = (Symbol)"c";

		public void VisitId(ILNode node)
		{
			PrintIdCore(node.Name.Name, startToken: true);
		}

		public static bool IsNormalIdentifier(UString name) => Les2Printer.IsNormalIdentifier(name);

		public void PrintIdCore(UString name, bool startToken, bool forceQuote = false)
		{
			if (forceQuote || !IsNormalIdentifier(name) || name == sy_true.Name || name == sy_false.Name || name == sy_null.Name) {
				if (startToken)
					StartToken(ColorCodeForId(name), Chars.BQId, Chars.BQId);
				PrintStringCore('`', false, name);
			} else {
				if (startToken)
					StartToken(ColorCodeForId(name), Chars.Id, Chars.Id);
				_out.Write(name);
			}
		}

		protected virtual LesColorCode ColorCodeForId(UString name) => LesColorCode.Id;

		private void PrintStringCore(char quoteType, bool tripleQuoted, UString text, UString prefix = default(UString))
		{
			if (prefix != null)
				PrintIdCore(prefix, startToken: false);
		restart:
			int startAt = SB.Length;
			_out.Write(quoteType);
			if (tripleQuoted)
			{
				// Print triple-quoted
				_out.Write(quoteType);
				_out.Write(quoteType);

				UString fullText = text;
				char a = '\0', b = '\0';
				for (; text.Length > 0; text = text.Slice(1)) {
					char c = text[0], d;
					if (c == quoteType && ((b == quoteType && a == quoteType) || text.Length == 1)) {
						// Escape triple quote, or a quote at end-of-string
						_out.Write('\\');
						_out.Write(c);
						_out.Write('/');
					} else if (c == '\0') {
						_out.Write(@"\0/");
					} else if (c == '\r') {
						_out.Write(@"\r/");
					} else if (c == '\\' && text.Length >= 3 && text[2] == '/' &&
						((d = text[1]) == 'r' || d == 'n' || d == 't' || d == '0' || d == '\\' || d == '\'' || d == '"')) {
						// avoid writing a false escape sequence
						_out.Write(@"\\/");
					} else if (c == '\n') {
						_out.Newline();
						_out.CommitNewlines();
						_out.Write(_out.IndentString.EndsWith("\t") ? "\t" : "   ");
					} else if (c >= 0xDC80 && c <= 0xDCFF && !(b >= 0xD800 && b <= 0xDBFF)) {
						// invalid UTF8 byte encoded as UTF16: CANNOT print in 
						// triple-quoted string! Start over as DQ string.
						SB.Length = startAt;
						quoteType = '"';
						tripleQuoted = false;
						text = fullText;
						goto restart;
					} else {
						_out.Write(c);
					}
					a = b; b = c;
				}

				_out.Write(quoteType);
				_out.Write(quoteType);
			}
			else 
			{
				// Normal double-quoted string
				for (;;) {
					bool fail;
					int c = text.PopFirst(out fail);
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
			_out.Write(quoteType);
		}

		#endregion

		#region Printing of literals

		public void VisitLiteral(ILNode node)
		{
			var typeMarker = node.TypeMarker;
			var textValue = node.TextValue;
			var value = node.Value;
			if (value == null && (typeMarker == null || typeMarker == sy_null))
			{
				WriteToken(sy_null.Name, LesColorCode.KeywordLiteral, Chars.Id, Chars.Id);
			}
			else if (value is bool b && (typeMarker == null || typeMarker == sy_bool))
			{
				WriteToken(b ? "true" : "false", LesColorCode.KeywordLiteral, Chars.Id, Chars.Id);
				return;
			}
			else if (value is char c && (typeMarker == null || typeMarker == sy_c))
			{
				StartToken(LesColorCode.String, Chars.SingleQuote);
				PrintStringCore('\'', false, c.ToString());
			}
			else if (typeMarker == sy_c && value is string s && ((UString)s).Do(s0 => { 
				s0.PopFirst(out bool fail); 
				return !fail && s0.Length == 0;
			}))
			{
				StartToken(LesColorCode.String, Chars.SingleQuote);
				PrintStringCore('\'', false, s);
			}
			else if (typeMarker == null || textValue.IsNull) // Convert to string for printing
			{
				var printer = _o.LiteralPrinter ?? StandardLiteralHandlers.Value;
				var sb = new StringBuilder();
				var result = printer.TryPrint(node, sb);
				if (result.Right.HasValue)
				{
					if (_o.WarnAboutUnprintableLiterals)
						MessageSink.Write(result.Right.Value);

					typeMarker = typeMarker ?? (Symbol)MemoizedTypeName.Get(value.GetType());
					if (textValue.IsEmpty)
					{
						if (sb.Length != 0)
							textValue = sb.ToString();
						else if (node.Value != null)
						{
							try
							{
								textValue = node.Value.ToString();
							}
							catch (Exception e)
							{
								MessageSink.Write(Severity.Error, node, "Exception in Value.ToString: {0}", e.Description());
							}
						}
					}
				}
				else
				{
					typeMarker = typeMarker ?? result.Left.Value;
					textValue = sb.ToString();
				}
				PrintStringOrNumber(textValue, node.Style, typeMarker);
			}
			else // Serialized form is already provided
			{
				// Note: other languages should not store TextValue for any standard 
				// TypeMarker unless they use the same syntax as LES, or a subset.
				// Accepting the TextValue without verifying that it matches the Value
				// should work fine as long as the creator of the node followed this rule.
				PrintStringOrNumber(textValue, node.Style, typeMarker);
			}
		}

		private void PrintStringOrNumber(UString text, NodeStyle stringStyle, Symbol typeMarker)
		{
			var kind = LesColorCode.String;
			if (typeMarker != null)
			{
				// Detect if we should print it as a number instead. One-digit numbers optimized:
				if (typeMarker == sy__ && text.Length == 1 && text[0] >= '0' && text[0] <= '9')
				{
					StartToken(LesColorCode.Number, Chars.NumberStart, Chars.NumberEnd);
					_out.Write(text[0]);
					return;
				}
				else if (CanPrintAsNumber(text, typeMarker))
				{
					WriteToken(text.ToString(), kind = LesColorCode.Number, Chars.NumberStart, Chars.NumberEnd);
					_out.Write(typeMarker.Name.Slice(1));
					return;
				}
				else if (typeMarker != GSymbol.Empty)
					kind = LesColorCode.CustomLiteral;
			}

			NodeStyle bs = (stringStyle & NodeStyle.BaseStyleMask);
			if (bs == NodeStyle.TQStringLiteral) {
				StartToken(kind, Chars.SingleQuote);
				PrintStringCore('\'', true, text, typeMarker?.Name);
			} else {
				StartToken(kind, Chars.StringStart, Chars.DoubleQuote);
				PrintStringCore('"', bs == NodeStyle.TDQStringLiteral, text, typeMarker?.Name);
			}
		}

		#endregion

		#region Printing calls: main Visit() method

		public void VisitCall(ILNode node)
		{
			// Note: Attributes, if any, have already been printed by this point
			bool parens = false;

			switch (_o.PrefixNotationOnly ? NodeStyle.PrefixNotation : node.BaseStyle())
			{
				case NodeStyle.Special:
					if (!TryToPrintCallAsKeywordExpression(node, node.IsParenthesizedExpr() || (_nlContext & NewlineContext.NewlineSensitive) != 0))
						goto case NodeStyle.PrefixNotation;
					break;
				case NodeStyle.PrefixNotation:
					PrintPrefixNotation(node, ref parens);
					break;
				default:
					// Figure out if this node can be treated as an operator and if 
					// so, whether it's a suffix operator.
					if (!HasTargetIdWithoutPAttrs(node))
						goto case NodeStyle.PrefixNotation;

					Symbol opName = node.Name;
					if (!TryToPrintCallAsSpecialOperator(opName, node))
					{
						if (!node.ArgCount().IsInRange(1, 2))
							goto case NodeStyle.PrefixNotation;
						var shape = (OperatorShape)node.ArgCount();
						if (node.ArgCount() == 1 && Les3PrecedenceMap.ResemblesSuffixOperator(opName, out opName))
							shape = OperatorShape.Suffix;

						if (!PrintCallAsNormalOp(shape, opName, node, ref parens))
							PrintPrefixNotation(node, ref parens);
					}
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
			_out.Indent();
			if (style == ArgListStyle.BracedBlock) {
				if (PrintStmtList(args, initialNewline: true)) {
					// There were newlines in the block
					_out.Dedent();
					Newline();
				} else { // no newlines in the block
					_out.Dedent();
					Space(_o.SpacesBetweenAppendedStatements);
				}
			} else {
				string semicolon = (style == ArgListStyle.Normal ? null : ";");
				for (int i = 0; i < args.Count; i++) {
					if (i != 0)
						Space(_o.SpaceAfterComma && !_out.IsAtStartOfLine);
					var stmt = args[i];
					Print(stmt, Precedence.MinValue, suffix: semicolon ?? (i + 1 != args.Count ? "," : null));
					MaybeForceLineBreak();
				}
				_out.Dedent();
				Space(spacesInside);
			}
			WriteToken(rightDelim, LesColorCode.Closer, Chars.Delimiter);

			_inParensOrBracks = outerInParensOrBracks;
		}

		bool ShouldAppendStmt(ILNode node)
		{
			return (!_o.PrintTriviaExplicitly && node.AttrNamed(S.TriviaAppendStatement) != null) &&
				_out.IndexInCurrentLine < _o.ForcedLineBreakThreshold;
		}

		// For printing at the top level, or in a braced block where newlines are significant
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
					Print(stmt, Precedence.MinValue, semicolon ? ";" : null, NewlineContext.StatementLevel);
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
			return Les3PrecedenceMap.IsNaturalOperatorToken(name.Slice(i)) || IsNormalIdentifier(name.Slice(1));
		}

		// Prints an operator with a "normal" shape (infix, prefix or suffix)
		// or in prefix notation if that fails. Returns true if closing ')' needed.
		private bool PrintCallAsNormalOp(OperatorShape shape, Symbol opName, ILNode node, ref bool parens)
		{
			// Check if this operator is allowed here. For example, if operator '+ 
			// appears within an argument to '*, as in (2 + 3) * 5, it's not 
			// allowed without parentheses.
			Precedence prec = Les3PrecedenceMap.Default.Find(shape, opName, cacheWordOp: true);
			if (shape == OperatorShape.Infix) {
				if (!CanBeBinaryOperator(opName.Name))
					return false;
			} else {
				if (prec == LesPrecedence.Illegal || !CanBePrefixOperator(opName.Name))
					return false;
			}
			bool allowed = IsAllowedHere(prec);
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
					if (!_out.IsAtStartOfLine)
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
			Chars startSet, endSet = (last == '.' ? Chars.Dot | Chars.Punc : Chars.Punc);
			int skipApostrophe = 1;
			if (Les3PrecedenceMap.IsOpChar(first) || first == '$')
				startSet = Chars.Punc | (first == '.' ? Chars.Dot : 0);
			else {
				Debug.Assert(Les2Lexer.IsIdStartChar(first));
				startSet = Chars.IdStart;
				endSet = Chars.IdStart;
				skipApostrophe = isBinaryOp ? 1 : 0;
			}
			
			StartToken(LesColorCode.Operator, startSet, endSet);

			_out.Write(new UString(opName.Name, skipApostrophe, opName.Name.Length - skipApostrophe));

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
					WriteToken("@", LesColorCode.Attribute, Chars.At);
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

		private bool TryToPrintCallAsKeywordExpression(ILNode node, bool allowComma)
		{
			if (!LesPrecedence.SuperExpr.CanAppearIn(_context) 
				|| !IsKeywordExpression(node, out int exprListSize, out ILNode braces)
				|| !allowComma && exprListSize > 1)
				return false;
			
			// Print @attributes and .keyword
			var args = node.Args();
			var target = node.Target;
			int parens = PrintAttrsAndLeadingTrivia(target, NewlineContext.NewlineUnsafe);
			WriteToken('.', LesColorCode.Keyword, Chars.Dot | Chars.Id);
			_out.Write(new UString(node.Name.Name, 1, node.Name.Name.Length-1));
			PrintTrailingTrivia(target, 0, null, NewlineContext.NewlineUnsafe);
			if (args.Count == 0)
				return true;

			// Print the rest: arg1, arg2 { $(..braces.Args()) } continuators
			int i;
			for (i = 0; i < exprListSize; i++) {
				Space();
				Print(args[i], Precedence.MinValue, suffix: i + 1 != exprListSize ? "," : null);
			}
			if (braces != null) {
				Space();
				Print(braces, LesPrecedence.Substitute);
				i++;
			}
			for (; i < args.Count; i++) {
				Space();
				PrintContinuator(args[i]);
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

		private bool IsKeywordExpression(ILNode node, out int exprListSize, out ILNode bracedBlock)
		{
			bracedBlock = null;
			exprListSize = 1;

			if (!HasTargetIdWithoutPAttrs(node) || !IsAcceptableKeyword(node.Name.Name))
				return false;
			var args = node.Args();
			if (args.Count <= 1)
				return true;

			int i;
			for (i = args.Count - 1; i > 0; i--)
				if (!IsContinuator(args[i]))
					break;
			if (i > 0 && args[i].Calls(S.Braces) && HasTargetIdWithoutPAttrs(args[i])) {
				bracedBlock = args[i];
				i--;
			}
			exprListSize = i + 1;
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
						if (!_out.IsAtStartOfLine && !MaybeForceLineBreak())
							Space();
					}
				}
			}
			return parenCount;
		}

		private bool MaybeForceLineBreak()
		{
			if (_out.IndexInCurrentLineAfterIndent > _o.ForcedLineBreakThreshold) {
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
						_out.Write(text.Replace(@"\\", "\\\u200B\\"));
						if (!PrintNewlineTriviaIfPossible(newlineSafePoint, true))
							if (text.EndsWith(@"\"))
								_out.Write(' '); // in case the comment is closed with `\\`, avoid ending in `\\\`
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
			_out.Write("/*");
			// Print carefully, changing "*/" to "*\" if necessary to avoid ending 
			// the comment early, or adding extra "*/"s to un-nest nested comments.
			int nesting = 0;
			char c, prev_c = '\0';
			for (int i = 0; i < text.Length; i++, prev_c = c) {
				c = text[i];
				if (c == '/' && prev_c == '*' && nesting == 0)
					c = '\\';
				_out.Write(c);
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
				_out.Write("*/");
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
}
