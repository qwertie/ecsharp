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

		/// <summary>Indicates whether the <see cref="NodeStyle.OneLiner"/> 
		/// flag is present on the current node or any of its parents. It 
		/// suppresses newlines within braced blocks.</summary>
		bool _isOneLiner;

		internal void Print(ILNode node, Precedence context, string terminator = null)
		{
			bool old_isOneLiner = _isOneLiner;
			_isOneLiner |= (node.Style & NodeStyle.OneLiner) != 0;
			try {
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
			} finally {
				_isOneLiner = old_isOneLiner;
			}
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
					if ((_o.PrintTriviaExplicitly || stmt.AttrNamed(S.TriviaAppendStatement) == null) && !_isOneLiner) {
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
					PrintLiteral(node); break;
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

		#endregion

		#region Parts of expressions: identifiers, literals (strings, numbers)

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


		static readonly Symbol Var = GSymbol.Get("var"), Def = GSymbol.Get("def");

		static StringBuilder _staticStringBuilder = new StringBuilder();
		static Les2PrinterWriter _staticWriter = new Les2PrinterWriter(_staticStringBuilder);
		static Les2Printer _staticPrinter = new Les2Printer(_staticStringBuilder);

		public static string PrintId(Symbol name)
		{
			_staticWriter.Reset();
			_staticStringBuilder.Length = 0; // Clear() only exists in .NET 4
			_staticPrinter.PrintIdOrSymbol(name, false);
			return _staticStringBuilder.ToString();
		}
		public static string PrintLiteral(object value, NodeStyle style = 0)
		{
			_staticWriter.Reset();
			_staticStringBuilder.Length = 0;
			if (!_staticPrinter.PrintLiteralCore(value, style))
				return null;
			return _staticStringBuilder.ToString();
		}
		public static string PrintString(string text, char quoteType, bool tripleQuoted)
		{
			_staticWriter.Reset();
			_staticStringBuilder.Length = 0;
			_staticPrinter.PrintStringCore(quoteType, tripleQuoted, text);
			return _staticStringBuilder.ToString();
		}

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
			
			// Watch out for @`-inf_d` and @`-inf_f`, because they will be
			// interpreted as named literals if we don't backquote them.
			if (special && !backquote && (name == "-inf_d" || name == "-inf_f"))
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

		private void PrintStringCore(char quoteType, bool tripleQuoted, string text)
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

		static Pair<RuntimeTypeHandle,Action<Les2Printer, object, NodeStyle>> P<T>(Action<Les2Printer, object, NodeStyle> handler) 
			{ return Pair.Create(typeof(T).TypeHandle, handler); }
		static Pair<K,V> P<K,V>(K key, V value) 
			{ return Pair.Create(key, value); }
		static Dictionary<K,V> Dictionary<K,V>(params Pair<K,V>[] input)
		{
			var d = new Dictionary<K,V>();
			for (int i = 0; i < input.Length; i++)
				d.Add(input[i].Key, input[i].Value);
			return d;
		}
		static Dictionary<RuntimeTypeHandle,Action<Les2Printer, object, NodeStyle>> LiteralPrinters = Dictionary(
			P<int>       ((np, value, style) => np.PrintIntegerToString(value, style, "")),
			P<long>      ((np, value, style) => np.PrintIntegerToString(value, style, "L")),
			P<uint>      ((np, value, style) => np.PrintIntegerToString(value, style, "u")),
			P<ulong>     ((np, value, style) => np.PrintIntegerToString(value, style, "uL")),
			P<BigInteger>((np, value, style) => np.PrintIntegerToString(value, style, "z")),
			P<short>     ((np, value, style) => np.PrintShortInteger(value, style, "Int16")), // Unnatural. Not produced by parser.
			P<ushort>    ((np, value, style) => np.PrintShortInteger(value, style, "UInt16")), // Unnatural. Not produced by parser.
			P<sbyte>     ((np, value, style) => np.PrintShortInteger(value, style, "Int8")), // Unnatural. Not produced by parser.
			P<byte>      ((np, value, style) => np.PrintShortInteger(value, style, "UInt8")), // Unnatural. Not produced by parser.
			P<double>    ((np, value, style) => np.PrintDoubleToString((double)value)),
			P<float>     ((np, value, style) => np.PrintFloatToString((float)value)),
			P<decimal>   ((np, value, style) => np.PrintValueToString(value, "m")),
			P<bool>      ((np, value, style) => np._out.Write((bool)value? "@true" : "@false", true)),
			P<@void>     ((np, value, style) => np._out.Write("@void", true)),
			P<char>      ((np, value, style) => np.PrintStringCore('\'', false, value.ToString())),
			P<string>    ((np, value, style) => {
				NodeStyle bs = (style & NodeStyle.BaseStyleMask);
				if (bs == NodeStyle.TQStringLiteral)
					np.PrintStringCore('\'', true, value.ToString());
				else
					np.PrintStringCore('"', bs == NodeStyle.TDQStringLiteral, value.ToString());
			}),
			P<Symbol> ((np, value, style) => np.PrintIdOrSymbol((Symbol)value, true)),
			P<TokenTree> ((np, value, style) => {
				np._out.Write("@{ ", true);
				np._out.Write(((TokenTree)value).ToString(TokenExt.ToString), true);
				np._out.Write(" }", true);
			}));

		private void PrintShortInteger(object value, NodeStyle style, string type)
		{
			ErrorSink.Write(Severity.Warning, null, "LesNodePrinter: Encountered literal of type '{0}'. It will be printed as 'Int32'.", type);
			PrintIntegerToString(value, style, "");
		}
		void PrintValueToString(object value, string suffix)
		{
			_out.Write(value.ToString(), false);
			_out.Write(suffix, true);
		}
		void PrintIntegerToString(object value, NodeStyle style, string suffix)
		{
			string asStr;
			if ((style & NodeStyle.BaseStyleMask) == NodeStyle.HexLiteral) {
				var valuef = (IFormattable)value;
				_out.Write("0x", false);
				asStr = valuef.ToString("x", null);
			} else
				asStr = value.ToString();
			
			if (suffix == "")
				_out.Write(asStr, true);
			else {
				_out.Write(asStr, false);
				_out.Write(suffix, true);
			}
		}

		const string NaNPrefix = "@nan_";
		const string PositiveInfinityPrefix = "@inf_";
		const string NegativeInfinityPrefix = "@-inf_";

		void PrintFloatToString(float value)
		{
			if (float.IsNaN(value))
				_out.Write(NaNPrefix, false);
			else if (float.IsPositiveInfinity(value))
				_out.Write(PositiveInfinityPrefix, false);
			else if (float.IsNegativeInfinity(value))
				_out.Write(NegativeInfinityPrefix, false);
			else
			{
				// The "R" round-trip specifier makes sure that no precision is lost, and
				// that parsing a printed version of float.MaxValue is possible.
				_out.Write(value.ToString("R", CultureInfo.InvariantCulture), false);
			}
			_out.Write("f", true);
		}
		void PrintDoubleToString(double value)
		{
			if (double.IsNaN(value))
				_out.Write(NaNPrefix, false);
			else if (double.IsPositiveInfinity(value))
				_out.Write(PositiveInfinityPrefix, false);
			else if (double.IsNegativeInfinity(value))
				_out.Write(NegativeInfinityPrefix, false);
			else
			{
				// The "R" round-trip specifier makes sure that no precision is lost, and
				// that parsing a printed version of double.MaxValue is possible.
				_out.Write(value.ToString("R", CultureInfo.InvariantCulture), false);
			}
			_out.Write("d", true);
		}

		private void PrintLiteral(ILNode node)
		{
			object value = node.Value;
			if (!PrintLiteralCore(value, node.Style))
			{
				ErrorSink.Error(node, "LesNodePrinter: Encountered unprintable literal of type {0}", value.GetType().Name);

				bool quote = _o.QuoteUnprintableLiterals;
				string unprintable;
				try {
					unprintable = value.ToString();
				} catch (Exception ex) {
					unprintable = ex.Message;
					quote = true;
				}
				if (quote)
					PrintStringCore('"', true, unprintable);
				else
					_out.Write(unprintable, true);
			}
		}
		private bool PrintLiteralCore(object value, NodeStyle style)
		{
			Action<Les2Printer, object, NodeStyle> p;
			if (value == null)
				_out.Write("@null", true);
			else if (LiteralPrinters.TryGetValue(value.GetType().TypeHandle, out p))
				p(this, value, style);
			else 
				return false;
			return true;
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
