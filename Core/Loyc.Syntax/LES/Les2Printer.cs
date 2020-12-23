using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Numerics;
using Loyc.Utilities;
using S = Loyc.Syntax.CodeSymbols;
using System.Diagnostics;
using Loyc.Syntax.Lexing;
using Loyc.Collections;
using System.Globalization;

namespace Loyc.Syntax.Les
{
	/// <summary>Prints a Loyc tree in LES (Loyc Expression Syntax) format.</summary>
	/// <remarks>Unless otherwise noted, the default value of all options is false.</remarks>
	public class Les2Printer
	{
		INodePrinterWriter _out;
		IMessageSink _errors;

		public INodePrinterWriter Writer { get { return _out; } set { _out = value; } }
		public IMessageSink ErrorSink { get { return _errors; } set { _errors = value ?? MessageSink.Null; } }

		Les2PrinterOptions _o;
		public Les2PrinterOptions Options { get { return _o; } }
		public void SetOptions(ILNodePrinterOptions options)
		{
			_o = options as Les2PrinterOptions ?? new Les2PrinterOptions(options);
		}

		#region Constructors and default Printer
		
		[ThreadStatic]
		static Les2Printer _printer;

		internal static void Print(ILNode node, StringBuilder target, IMessageSink sink, ParsingMode mode, ILNodePrinterOptions options = null)
		{
			var p = _printer = _printer ?? new Les2Printer(TextWriter.Null, null);
			var oldOptions = p._o;
			var oldWriter = p.Writer;
			var oldSink = p.ErrorSink;
			p.ErrorSink = sink;
			p.SetOptions(options);
			p.Writer = new Les2PrinterWriter(target, p.Options.IndentString ?? "\t", p.Options.NewlineString ?? "\n");

			if (mode == ParsingMode.Expressions)
				p.Print(node, StartStmt, "");
			else
				p.Print(node, StartStmt, ";");

			p.Writer = oldWriter;
			p._o = oldOptions;
			p.ErrorSink = oldSink;
		}

		internal Les2Printer(TextWriter target, ILNodePrinterOptions options = null)
		{
			SetOptions(options);
			Writer = new Les2PrinterWriter(target, _o.IndentString ?? "\t", _o.NewlineString ?? "\n");
		}
		internal Les2Printer(StringBuilder target, ILNodePrinterOptions options = null)
		{
			SetOptions(options);
			Writer = new Les2PrinterWriter(target, _o.IndentString ?? "\t", _o.NewlineString ?? "\n");
		}

		#endregion

		internal void Print(ILNode node, Precedence context, string terminator = null)
		{
			int parenCount = WriteAttrs(node, ref context);

			if (!node.IsCall() || node.BaseStyle() == NodeStyle.PrefixNotation)
				PrintPrefixNotation(node, context);
			else do {
				if (AutoPrintBracesOrBracks(node))
					break;
				if (!LesPrecedence.Primary.CanAppearIn(context)) {
					_out.Write("(@[] ", true);
					parenCount++;
					context = StartStmt;
				}
				int args = node.ArgCount();
				if (args == 1 && AutoPrintPrefixOrSuffixOp(node, context))
					break;
				if (args == 2 && AutoPrintInfixOp(node, context))
					break;
				PrintPrefixNotation(node, context);
			} while (false);
			
			PrintSuffixTrivia(node, parenCount, terminator);
		}

		#region Infix, prefix and suffix operators

		private bool AutoPrintInfixOp(ILNode node, Precedence context)
		{
			var prec = GetPrecedenceIfOperator(node, node.Name, OperatorShape.Infix, context);
			if (prec == null)
				return false;
			Print(node[0], prec.Value.LeftContext(context));
			SpaceIf(prec.Value.Lo < _o.SpaceAroundInfixStopPrecedence);
			bool sa = prec.Value.Lo < _o.SpaceAroundInfixStopPrecedence;
			WriteOpName(node.Name, node.Target, prec.Value, spaceAfter: sa);
			Print(node[1], prec.Value.RightContext(context));
			return true;
		}

		private bool AutoPrintPrefixOrSuffixOp(ILNode node, Precedence context)
		{
			Symbol bareName;
			if (Les2PrecedenceMap.ResemblesSuffixOperator(node.Name, out bareName) && Les2PrecedenceMap.IsNaturalOperator(bareName.Name)) {
				var prec = GetPrecedenceIfOperator(node, bareName, OperatorShape.Suffix, context);
				if (prec == null)
					return false;
				Print(node[0], prec.Value.LeftContext(context));
				SpaceIf(prec.Value.Lo < _o.SpaceAfterPrefixStopPrecedence);
				WriteOpName(bareName, node.Target, prec.Value);
			} else {
				var prec = GetPrecedenceIfOperator(node, bareName, OperatorShape.Prefix, context);
				if (prec == null)
					return false;
				var spaceAfter = prec.Value.Lo < _o.SpaceAfterPrefixStopPrecedence;
				WriteOpName(node.Name, node.Target, prec.Value, spaceAfter);
				Print(node[0], prec.Value.RightContext(context));
			}
			return true;
		}
		
		private void WriteOpName(Symbol op, ILNode target, Precedence prec, bool spaceAfter = false)
		{
			// Note: if the operator has a space after it, there's a subtle reason why 
			// we want to print that space before the trivia and not after. Consider
			// the input "a == //comment\n    b". After trivia injection this becomes
			// (@[@%trailing(@%SLComment("comment"))] @==)(a, @[@%newline] b).
			// Because the injector associates the newline with a different node than the
			// single-line comment, there's no easy way to strip out the newline during
			// the parsing process. So, to make the trivia round-trip, we use 
			// _out.Newline(pending: true) when printing a single-line comment, which 
			// suppresses the newline if it is followed immediately by another newline.
			// But if we print a space after the trivia, then this suppression does not
			// occur and we end up with two newlines. Therefore, we must print the space first.
			if (target.AttrCount() == 0)
				target = null; // optimize the usual case
			if (target != null)
				PrintPrefixTrivia(target);
			if (!Les2PrecedenceMap.IsNaturalOperator(op.Name))
				PrintStringCore('`', false, op.Name);
			else {
				Debug.Assert(op.Name.StartsWith("'"));
				_out.Write(op.Name.Substring(1), true);
			}
			SpaceIf(spaceAfter);
			if (target != null)
				PrintSuffixTrivia(target, 0, null);
		}

		private void SpaceIf(bool cond)
		{
			if (cond) _out.Space();
		}

		protected Les2PrecedenceMap _prec = Les2PrecedenceMap.Default;

		private Precedence? GetPrecedenceIfOperator(ILNode node, Symbol opName, OperatorShape shape, Precedence context)
		{
			int ac = node.ArgCount();
			if ((ac == (int)shape || ac == -(int)shape) && HasTargetIdWithoutPAttrs(node))
			{
				var bs = node.BaseStyle();
				bool naturalOp = Les2PrecedenceMap.IsNaturalOperator(opName.Name);
				if ((naturalOp && bs != NodeStyle.PrefixNotation) ||
					(bs == NodeStyle.Operator && node.Name != null))
				{
					var result = _prec.Find(shape, opName);
					if (!result.CanAppearIn(context) || !result.CanMixWith(context))
						return null;
					return result;
				}
			}
			return null;
		}

		private bool HasTargetIdWithoutPAttrs(ILNode node)
		{
			var t = node.Target;
			return t.IsId() && !HasPAttrs(t);
		}
		
		#endregion

		#region Other stuff: braces, TODO: superexpressions, indexing, generics, tuples

		private bool AutoPrintBracesOrBracks(ILNode node)
		{
			var name = node.Name;
			if ((name == S.Array || name == S.Braces) && node.IsCall() && !HasPAttrs(node.Target)) {
				if (name == S.Array) {
					PrintArgList(node.Args(), node.BaseStyle() == NodeStyle.StatementBlock, "[", ']', node.Target);
					return true;
				} else if (name == S.Braces) {
					PrintArgList(node.Args(), node.BaseStyle() != NodeStyle.Expression, "{", '}', node.Target);
					return true;
				}
			}
			return false;
		}

		private void PrintArgList(NegListSlice<ILNode> args, bool stmtMode, string leftDelim, char rightDelim, ILNode target = null)
		{
			if (target != null)
				PrintPrefixTrivia(target);
			_out.Write(leftDelim, true);
			if (target != null)
				PrintSuffixTrivia(target, 0, "");
			if (stmtMode) {
				_out.Indent();
				bool anyNewlines = false;
				foreach (var stmt in args) {
					if ((_o.PrintTriviaExplicitly || stmt.AttrNamed(S.TriviaAppendStatement) == null)) {
						_out.Newline();
						anyNewlines = true;
					} else
						SpaceIf(_o.SpacesBetweenAppendedStatements);
					Print(stmt, StartStmt, ";");
				}
				_out.Dedent();
				if (anyNewlines)
					_out.Newline();
				else
					SpaceIf(_o.SpacesBetweenAppendedStatements);
			} else {
				for (int i = 0; i < args.Count; )
					Print(args[i], StartStmt, ++i == args.Count ? "" : ", ");
			}
			_out.Write(rightDelim, true);
		}

		#endregion

		/// <summary>Context: beginning of main expression (potential superexpression)</summary>
		protected static readonly Precedence StartStmt      = Precedence.MinValue;

		void PrintPrefixNotation(ILNode node, Precedence context)
		{
			switch(node.Kind) {
				case LNodeKind.Id:
					PrintIdOrSymbol(node.Name, false); break;
				case LNodeKind.Literal:
					PrintLiteralCore(node); break;
				case LNodeKind.Call: default:
					Print(node.Target, LesPrecedence.Primary.LeftContext(context), "(");
					PrintArgList(node.Args(), node.BaseStyle() == NodeStyle.StatementBlock, "", ')', null);
					break;
			}
		}

		bool HasPAttrs(ILNode node)
		{
			foreach (var attr in node.Attrs())
				if (!IsConsumedTrivia(attr))
					return true;
			return false;
		}

		private int WriteAttrs(ILNode node, ref Precedence context)
		{
			bool wroteBrack = false, needParen = (context != StartStmt);
			int parenCount = 0;
			foreach (var attr in node.Attrs()) {
				if (attr.IsIdNamed(S.TriviaInParens)) {
					MaybeCloseBrack(ref wroteBrack);
					parenCount++;
					_out.Write('(', true);
					// need extra paren when writing @[..] because (@[..] ...) doesn't count as %inParens
					needParen = true;
					continue;
				}
				if (MaybePrintTrivia(attr, needSpace: false, testOnly: wroteBrack)) {
					if (wroteBrack) {
						MaybeCloseBrack(ref wroteBrack);
						MaybePrintTrivia(attr, needSpace: false);
					}
					continue;
				}
				if (!wroteBrack) {
					wroteBrack = true;
					if (needParen) {
						needParen = false;
						parenCount++;
						_out.Write('(', true);
					}
					_out.Write("@[", true);
				} else {
					_out.Write(',', true);
					_out.Space();
				}
				Print(attr, StartStmt);
			}
			MaybeCloseBrack(ref wroteBrack);
			if (parenCount != 0)
				context = StartStmt;
			return parenCount;
		}

		private void MaybeCloseBrack(ref bool wroteBrack)
		{
			if (wroteBrack) {
				_out.Write("] ", true);
				wroteBrack = false;
			}
		}

		#region Trivia printing

		private void PrintPrefixTrivia(ILNode _n)
		{
			foreach (var attr in _n.Attrs())
				MaybePrintTrivia(attr, needSpace: false);
		}
		private void PrintSuffixTrivia(ILNode _n, int parenCount, string terminator)
		{
			while (--parenCount >= 0)
				_out.Write(')', true);
			if (terminator != null)
				_out.Write(terminator, true);
			foreach (var attr in _n.GetTrailingTrivia())
				MaybePrintTrivia(attr, needSpace: true);
		}

		private bool IsConsumedTrivia(ILNode attr)
		{
			return MaybePrintTrivia(attr, false, testOnly: true);
		}
		private bool MaybePrintTrivia(ILNode attr, bool needSpace, bool testOnly = false)
		{
			var name = attr.Name;
			if (S.IsTriviaSymbol(name)) {
				if ((name == S.TriviaRawText) && _o.ObeyRawText) {
					if (!testOnly)
						_out.Write(GetRawText(attr), true);
					return true;
				} else if (_o.PrintTriviaExplicitly) {
					return false;
				} else {
					if (name == S.TriviaNewline) {
						if (!testOnly && !_o.OmitSpaceTrivia)
							_out.Newline();
						return true;
					} else if ((name == S.TriviaSpaces)) {
						if (!testOnly && !_o.OmitSpaceTrivia)
							PrintSpaces(GetRawText(attr));
						return true;
					} else if (name == S.TriviaSLComment) {
						if (!testOnly && !_o.OmitComments) {
							if (needSpace && !_out.LastCharWritten.IsOneOf(' ', '\t'))
								_out.Write('\t', true);
							_out.Write("//", false);
							_out.Write(GetRawText(attr), true);
							_out.Newline(pending: true);
						}
						return true;
					} else if (name == S.TriviaMLComment) {
						if (!testOnly && !_o.OmitComments) {
							if (needSpace && !_out.LastCharWritten.IsOneOf(' ', '\t', '\n'))
								_out.Space();
							_out.Write("/*", false);
							_out.Write(GetRawText(attr), false);
							_out.Write("*/", false);
						}
						return true;
					} else if (name == S.TriviaAppendStatement)
						return true; // obeyed elsewhere
					if (_o.OmitUnknownTrivia)
						return true; // block printing
					if (!needSpace && name == S.TriviaTrailing)
						return true;
				}
			}
			return false;
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

		private void PrintSpaces(string spaces)
		{
			for (int i = 0; i < spaces.Length; i++) {
				char c = spaces[i];
				if (c == ' ' || c == '\t')
					_out.Write(c, false);
				else if (c == '\n')
					_out.Newline();
				else if (c == '\r') {
					_out.Newline();
					if (spaces.TryGet(i + 1) == '\r')
						i++;
				}
			}
		}

		#endregion

		#region Static methods for printing literals, identifiers and strings

		[ThreadStatic] static StringBuilder _staticStringBuilder = new StringBuilder();
		[ThreadStatic] static Les2PrinterWriter _staticWriter;
		[ThreadStatic] static Les2Printer _staticPrinter;

		static void MaybeInitThread()
		{
			_staticPrinter = _staticPrinter ?? new Les2Printer(_staticStringBuilder);
			_staticWriter = _staticWriter ?? new Les2PrinterWriter(_staticStringBuilder);
			_staticStringBuilder = _staticStringBuilder ?? new StringBuilder();
		}
		public static string PrintId(Symbol name)
		{
			MaybeInitThread();
			_staticWriter.Reset();
			_staticStringBuilder.Length = 0; // Clear() only exists in .NET 4
			_staticPrinter.PrintIdOrSymbol(name, false);
			return _staticStringBuilder.ToString();
		}
		public static string PrintLiteral(object value, NodeStyle style = 0) => PrintLiteral(LNode.Literal(value, null, style));
		public static string PrintLiteral(ILNode literal)
		{
			MaybeInitThread();
			_staticWriter.Reset();
			_staticStringBuilder.Length = 0;
			_staticPrinter.PrintLiteralCore(literal);
			return _staticStringBuilder.ToString();
		}
		public static string PrintString(string text, char quoteType, bool tripleQuoted)
		{
			MaybeInitThread();
			_staticWriter.Reset();
			_staticStringBuilder.Length = 0;
			_staticPrinter.PrintStringCore(quoteType, tripleQuoted, text);
			return _staticStringBuilder.ToString();
		}

		#endregion

		#region Printing of identifiers and literals

		/// <summary>Returns true if the given symbol can be printed as a 
		/// normal identifier, without an "@" prefix. Note: identifiers 
		/// starting with "#" still count as normal; call <see cref="LNode.HasSpecialName"/> 
		/// to detect this.</summary>
		public static bool IsNormalIdentifier(Symbol name) => IsNormalIdentifier(name.Name);
		public static bool IsNormalIdentifier(UString name)
		{
			bool bq;
			return !IsSpecialIdentifier(name, out bq);
		}
		
		static bool IsSpecialIdentifier(UString name, out bool backquote)
		{
			backquote = name.Length == 0;
			bool special = false, first = true;
			foreach (char c in name)
			{
				if (!Les2Lexer.IsIdContChar(c)) {
					if (Les2Lexer.IsSpecialIdChar(c))
						special = true;
					else
						backquote = true;
				} else if (first && !Les2Lexer.IsIdStartChar(c))
					special = true;
				first = false;
			}
			
			// Watch out for named literals with punctuation e.g. @`-inf_d` and @`-inf_f`:
			// they will be interpreted as named literals if we don't backquote them.
			if (special && !backquote && Les2Lexer.NamedLiterals.ContainsKey(name))
				backquote = true;
			return special || backquote;
		}

		private void PrintIdOrSymbol(Symbol name, bool isSymbol)
		{
			// Figure out what style we need to use: plain, @special, or @`backquoted`
			bool backquote, special = IsSpecialIdentifier(name.Name, out backquote); 

			if (special || isSymbol)
				_out.Write(isSymbol ? "@@" : "@", false);
			if (backquote)
				PrintStringCore('`', false, name.Name);
			else
				_out.Write(name.Name, true);
		}

		private void PrintString(Symbol typeMarker, char quoteType, bool tripleQuoted, UString text)
		{
			if (typeMarker != null && typeMarker.Name.Length != 0)
				PrintIdOrSymbol(typeMarker, isSymbol: false);
			PrintStringCore(quoteType, tripleQuoted, text);
		}

		private void PrintStringCore(char quoteType, bool tripleQuoted, UString text)
		{
			_out.Write(quoteType, false);
			if (tripleQuoted) {
				_out.Write(quoteType, false);
				_out.Write(quoteType, false);

				char a = '\0', b = '\0';
				foreach (char c in text) {
					if (c == quoteType && b == quoteType && a == quoteType)
						_out.Write(@"\\", false);
					// prevent false escape sequences
					if (a == '\\' && b == '\\' && (c == quoteType || c == 'n' || c == 'r' || c == '\\'))
						_out.Write(@"\\", false);
					_out.Write(c, false);
					a = b; b = c;
				}
				
				_out.Write(quoteType, false);
				_out.Write(quoteType, false);
			} else {
				_out.Write(PrintHelpers.EscapeCStyle(text, EscapeC.Control, quoteType), false);
			}
			_out.Write(quoteType, true);
		}

		static readonly Symbol sy_bool = (Symbol)"bool", sy_c = (Symbol)"c", sy_s = (Symbol)"s", sy__ = (Symbol)"_";

		internal static Dictionary<object, string> NamedLiterals = new Dictionary<object, string>()
		{
			[true] = "@true",
			[false] = "@false",
			[@void.Value] = "@void",
			[float.NaN] = "@nan.f",
			[double.NaN] = "@nan.d",
			[float.PositiveInfinity] = "@inf.f",
			[double.PositiveInfinity] = "@inf.d",
			[float.NegativeInfinity] = "@-inf.f",
			[double.NegativeInfinity] = "@-inf.d",
		};

		private void PrintLiteralCore(ILNode node)
		{
			var typeMarker = node.TypeMarker;
			var textValue = node.TextValue;
			var value = node.Value;

			if (value == null)
			{
				_out.Write("@null", true);
			}
			else if (NamedLiterals.TryGetValue(value, out var text))
			{
				_out.Write(text, true);
			}
			else if (value is char c && (typeMarker == null || typeMarker == sy_c))
			{
				PrintStringCore('\'', false, c.ToString());
			}
			else if (value is Symbol s && (typeMarker == null || typeMarker == sy_s))
			{
				PrintIdOrSymbol(s, isSymbol: true);
			}
			else if (typeMarker == null || textValue.IsNull) // Convert to string for printing
			{
				var printer = _o.LiteralPrinter ?? StandardLiteralHandlers.Value;
				var sb = new StringBuilder();
				var result = printer.TryPrint(node, sb);
				if (result.Right.HasValue)
				{
					ErrorSink.Error(node, "Les2Printer: Encountered unprintable literal of type {0}", value.GetType().Name);
					ErrorSink.Write(result.Right.Value);

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
								ErrorSink.Write(Severity.Error, node, "Exception in Value.ToString: {0}", e.Description());
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
			if (typeMarker != null && typeMarker.Name.StartsWith("_"))
			{
				// Detect if we should print it as a number instead. One-digit numbers optimized:
				if (typeMarker == sy__ && text.Length == 1 && text[0] >= '0' && text[0] <= '9')
				{
					_out.Write(text[0], true);
					return;
				}
				else if (Les3Printer.CanPrintAsNumber(text, typeMarker))
				{
					_out.Write(text.ToString(), false);
					_out.Write(typeMarker.Name.Slice(1).ToString(), true);
					return;
				}
			}

			NodeStyle bs = (stringStyle & NodeStyle.BaseStyleMask);
			if (bs == NodeStyle.TQStringLiteral)
				PrintString(typeMarker, '\'', true, text);
			else
				PrintString(typeMarker, '"', bs == NodeStyle.TDQStringLiteral, text);
		}

		#endregion
	}

	/// <summary>Options to control the way Loyc trees are printed by <see cref="Les2Printer"/>.</summary>
	public sealed class Les2PrinterOptions : LNodePrinterOptions
	{
		public Les2PrinterOptions() { }
		public Les2PrinterOptions(ILNodePrinterOptions options)
		{
			if (options != null)
				CopyFrom(options);
		}

		/// <summary>Introduces extra parenthesis to express precedence, without
		/// using an empty attribute list [] to allow perfect round-tripping.</summary>
		/// <remarks>For example, the Loyc tree <c>x * @+(a, b)</c> will be printed 
		/// <c>x * (a + b)</c>, which is a slightly different tree (the parenthesis
		/// add the trivia attribute <c>%inParens</c>.)</remarks>
		public override bool AllowChangeParentheses { get { return base.AllowChangeParentheses; } set { base.AllowChangeParentheses = value; } }

		/// <summary>Causes comments and spaces to be printed as attributes in order 
		/// to ensure faithful round-trip parsing. By default, only "raw text" and
		/// unrecognized trivia is printed this way. Note: <c>%inParens</c> is 
		/// always printed as parentheses, and <see cref="ILNodePrinterOptions.OmitUnknownTrivia"/> 
		/// has no effect when this flag is true.</summary>
		public override bool PrintTriviaExplicitly { get { return base.PrintTriviaExplicitly; } set { base.PrintTriviaExplicitly = value; } }

		/// <summary>When an argument to a method or macro has the value <c>@``</c>,
		/// it will be omitted completely if this flag is set.</summary>
		public bool OmitMissingArguments { get; set; }

		/// <summary>When this flag is set, space trivia attributes are ignored
		/// (e.g. <see cref="CodeSymbols.TriviaNewline"/>).</summary>
		public bool OmitSpaceTrivia { get; set; }

		public override bool CompactMode {
			get { return base.CompactMode; }
			set {
				if (base.CompactMode = value) {
					SpacesBetweenAppendedStatements = false;
					SpaceAroundInfixStopPrecedence = LesPrecedence.SuperExpr.Lo;
					SpaceAfterPrefixStopPrecedence = LesPrecedence.SuperExpr.Lo;
				} else {
					SpacesBetweenAppendedStatements = true;
					SpaceAroundInfixStopPrecedence = LesPrecedence.Range.Lo;
					SpaceAfterPrefixStopPrecedence = LesPrecedence.Range.Lo;
				}
			}
		}

		/// <summary>When the printer encounters an unprintable literal, it calls
		/// Value.ToString(). When this flag is set, the string is placed in double
		/// quotes; when this flag is clear, it is printed as raw text.</summary>
		public bool QuoteUnprintableLiterals { get; set; }

		/// <summary>Causes raw text to be printed verbatim, as the EC# printer does.
		/// When this option is false, raw text trivia is printed as a normal 
		/// attribute.</summary>
		public bool ObeyRawText { get; set; }

		/// <summary>Whether to add a space between multiple statements printed on
		/// one line (initial value: true).</summary>
		public bool SpacesBetweenAppendedStatements = true;

		/// <summary>The printer avoids printing spaces around infix (binary) 
		/// operators that have the specified precedence or higher.</summary>
		/// <seealso cref="LesPrecedence"/>
		public int SpaceAroundInfixStopPrecedence = LesPrecedence.Multiply.Hi + 1;

		/// <summary>The printer avoids printing spaces after prefix operators 
		/// that have the specified precedence or higher.</summary>
		public int SpaceAfterPrefixStopPrecedence = LesPrecedence.Multiply.Hi + 1;
	}
}
