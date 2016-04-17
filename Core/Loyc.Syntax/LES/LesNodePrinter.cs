using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
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
	public class LesNodePrinter
	{
		INodePrinterWriter _out;
		IMessageSink _errors;

		public INodePrinterWriter Writer { get { return _out; } set { _out = value; } }
		public IMessageSink Errors { get { return _errors; } set { _errors = value ?? MessageSink.Null; } }

		/// <summary>Introduces extra parenthesis to express precedence, without
		/// using an empty attribute list [] to allow perfect round-tripping.</summary>
		/// <remarks>For example, the Loyc tree <c>x * @+(a, b)</c> will be printed 
		/// <c>x * (a + b)</c>, which is a slightly different tree (the parenthesis
		/// add the trivia attribute #trivia_inParens.)</remarks>
		public bool AllowExtraParenthesis { get; set; }

		/// <summary>When an argument to a method or macro has the value <c>@``</c>,
		/// it will be omitted completely if this flag is set.</summary>
		public bool OmitMissingArguments { get; set; }

		/// <summary>When this flag is set, space trivia attributes are ignored
		/// (e.g. <see cref="CodeSymbols.TriviaSpaceAfter"/>).</summary>
		public bool OmitSpaceTrivia { get; set; }

		/// <summary>When this flag is set, comment trivia attributes are ignored
		/// (e.g. <see cref="CodeSymbols.TriviaSLCommentAfter"/>).</summary>
		public bool OmitComments { get; set; }

		/// <summary>When the printer encounters an unprintable literal, it calls
		/// Value.ToString(). When this flag is set, the string is placed in double
		/// quotes; when this flag is clear, it is printed as raw text.</summary>
		public bool QuoteUnprintableLiterals { get; set; }

		/// <summary>Causes unknown trivia (other than comments, spaces and raw 
		/// text) to be dropped from the output.</summary>
		public bool OmitUnknownTrivia { get; set; }

		/// <summary>Causes comments and spaces to be printed as attributes in order 
		/// to ensure faithful round-trip parsing. By default, only "raw text" and
		/// unrecognized trivia is printed this way. Note: #trivia_inParens is 
		/// always printed as parentheses.</summary>
		public bool PrintExplicitTrivia { get; set; }

		/// <summary>Causes raw text to be printed verbatim, as the EC# printer does.
		/// When this option is false, raw text trivia is printed as a normal 
		/// attribute.</summary>
		public bool ObeyRawText { get; set; }

		#region Constructors, New(), and default Printer

		public static LesNodePrinter New(StringBuilder target, string indentString = "\t", string lineSeparator = "\n", IMessageSink sink = null)
		{
			return new LesNodePrinter(new LesNodePrinterWriter(target, indentString, lineSeparator), sink);
		}
		public static LesNodePrinter New(TextWriter target, string indentString = "\t", string lineSeparator = "\n", IMessageSink sink = null)
		{
			return new LesNodePrinter(new LesNodePrinterWriter(target, indentString, lineSeparator), sink);
		}
		
		[ThreadStatic]
		static LesNodePrinter _printer;
		public static readonly LNodePrinter Printer = Print;

		public static void Print(LNode node, StringBuilder target, IMessageSink errors, object mode, string indentString, string lineSeparator)
		{
			var w = new LesNodePrinterWriter(target, indentString, lineSeparator);
			var p = _printer = _printer ?? new LesNodePrinter(w);
			p.Writer = w;
			p.Errors = errors;

			if (object.Equals(mode, NodeStyle.Expression) || mode == ParsingMode.Exprs)
				p.Print(node, 0, StartStmt, "");
			else
				p.Print(node, 0, StartStmt, ";");

			p.Writer = null;
			p.Errors = null;
		}

		public LesNodePrinter(INodePrinterWriter target, IMessageSink errors = null)
		{
			Writer = target;
			Errors = errors;
		}

		#endregion

		/// <summary>TODO: these modes no longer apply, remove them</summary>
		[Flags]
		public enum Mode
		{
			/// <summary>Whitespace agnostic mode. ':' cannot be used to begin a 
			/// python-style code block.</summary>
			Wsa = 8,
			/// <summary>Inside parenthesis (implies Wsa, and additionally allows 
			/// ':' as an operator).</summary>
			InParens = 24,
		}

		public void Print(LNode node)
		{
			Print(node, 0, StartStmt, ";");
		}
		public void Print(LNode node, Mode mode, Precedence context, string terminator = null)
		{
			int parenCount = PrintPrefixTrivia(node);
			if (parenCount != 0)
				context = StartStmt;
			if (WriteAttrs(node, context)) {
				Debug.Assert(parenCount == 0);
				parenCount++;
			}

			if (node.BaseStyle == NodeStyle.PrefixNotation)
				PrintPrefixNotation(node, mode, context);
			else do {
				if (node.IsCall) {
					if (AutoPrintBracesOrBracks(node, mode))
						break;
					int args = node.ArgCount;
					if (args == 1 && AutoPrintPrefixOrSuffixOp(node, mode, context))
						break;
					if (args == 2 && AutoPrintInfixOp(node, mode, context))
						break;
				}
				PrintPrefixNotation(node, mode, context);
			} while (false);
			
			PrintSuffixTrivia(node, parenCount, terminator);
		}

		#region Infix, prefix and suffix operators

		private bool AutoPrintInfixOp(LNode node, Mode mode, Precedence context)
		{
			var prec = GetPrecedenceIfOperator(node, OperatorShape.Infix, context);
			if (prec == null)
				return false;
			var a = node.Args;
			Print(a[0], mode, prec.Value.LeftContext(context));
			SpaceIf(prec.Value.Lo < SpaceAroundInfixStopPrecedence);
			WriteOpName(node.Name, prec.Value);
			SpaceIf(prec.Value.Lo < SpaceAroundInfixStopPrecedence);
			Print(a[1], mode, prec.Value.RightContext(context));
			return true;
		}

		private bool AutoPrintPrefixOrSuffixOp(LNode node, Mode mode, Precedence context)
		{
			Symbol bareName;
			if (LesPrecedenceMap.IsSuffixOperatorName(node.Name, out bareName, false)) {
				var prec = GetPrecedenceIfOperator(node, OperatorShape.Suffix, context);
				if (prec == null || prec.Value == LesPrecedence.Backtick)
					return false;
				Print(node.Args[0], mode, prec.Value.LeftContext(context));
				SpaceIf(prec.Value.Lo < SpaceAfterPrefixStopPrecedence);
				WriteOpName(bareName, prec.Value);
			} else {
				var prec = GetPrecedenceIfOperator(node, OperatorShape.Prefix, context);
				if (prec == null)
					return false;
				WriteOpName(node.Name, prec.Value);
				SpaceIf(prec.Value.Lo < SpaceAfterPrefixStopPrecedence);
				Print(node.Args[0], mode, prec.Value.RightContext(context));
			}
			return true;
		}
		
		private void WriteOpName(Symbol op, Precedence prec)
		{
			if (prec == LesPrecedence.Backtick || !LesPrecedenceMap.IsNaturalOperator(op))
				PrintStringCore('`', false, op.Name);
			else
				_out.Write(op.Name, true);
			//else {
			//	_out.Write('\\', false);
			//	_out.Write(op.Name, true);
			//	_out.Space(); // lest the next char to be printed be treated as part of the operator name
			//}
		}

		public int SpaceAroundInfixStopPrecedence = LesPrecedence.Power.Lo;
		public int SpaceAfterPrefixStopPrecedence = LesPrecedence.Prefix.Lo;

		private void SpaceIf(bool cond)
		{
			if (cond) _out.Space();
		}

		protected LesPrecedenceMap _prec = LesPrecedenceMap.Default;

		private Precedence? GetPrecedenceIfOperator(LNode node, OperatorShape shape, Precedence context)
		{
			int ac = node.ArgCount;
			if ((ac == (int)shape || ac == -(int)shape) && HasSimpleTargetWithoutPAttrs(node))
			{
				var bs = node.BaseStyle;
				var op = node.Name;
				bool naturalOp = LesPrecedenceMap.IsNaturalOperator(op);
				if (bs == NodeStyle.Operator || (naturalOp && bs != NodeStyle.PrefixNotation))
				{
					var result = _prec.Find(shape, op);
					if (bs == NodeStyle.Operator && !naturalOp)
						result = LesPrecedence.Backtick;
					else if (!result.CanAppearIn(context))
						result = LesPrecedence.Backtick;
					if (!result.CanAppearIn(context) || !result.CanMixWith(context))
						return null;
					return result;
				}
			}
			return null;
		}

		private bool HasSimpleTargetWithoutPAttrs(LNode node)
		{
			if (node.HasSimpleHead())
				return true;
			var t = node.Target;
			return !t.IsCall && !HasPAttrs(t);
		}
		
		#endregion

		#region Other stuff: braces, TODO: superexpressions, indexing, generics, tuples

		private bool AutoPrintBracesOrBracks(LNode node, Mode mode)
		{
			var name = node.Name;
			if (name == S.Array && node.IsCall)
				PrintArgList(node.Args, node.BaseStyle == NodeStyle.Statement, '[', ']');
			else if (name == S.Braces && node.IsCall)
				PrintArgList(node.Args, node.BaseStyle == NodeStyle.Statement, '{', '}');
			else
				return false;
			return true;
		}

		private void PrintArgList(VList<LNode> args, bool stmtMode, char leftDelim, char rightDelim)
		{
			_out.Write(leftDelim, true);
			if (stmtMode) {
				_out.Indent();
				_out.Newline();
				foreach (var stmt in args)
					Print(stmt, Mode.Wsa, StartStmt, ";");
				_out.Dedent();
			} else {
				for (int i = 0; i < args.Count; )
					Print(args[i], Mode.Wsa, StartStmt, ++i == args.Count ? "" : ", ");
			}
			_out.Write(rightDelim, true);
		}

		#endregion

		/// <summary>Context: beginning of main expression (potential superexpression)</summary>
		public static readonly Precedence StartStmt      = Precedence.MinValue;

		void PrintPrefixNotation(LNode node, Mode mode, Precedence context)
		{
			switch(node.Kind) {
				case LNodeKind.Id:
					PrintIdOrSymbol(node.Name, false); break;
				case LNodeKind.Literal:
					PrintLiteral(node); break;
				case LNodeKind.Call: default:
					Print(node.Target, mode, LesPrecedence.Primary.LeftContext(context), null);
					PrintArgList(node.Args, node.BaseStyle == NodeStyle.Statement, '(', ')');
					break;
			}
		}

		bool HasPAttrs(LNode node)
		{
			var A = node.Attrs;
			for (int i = 0; i < A.Count; i++) {
				var a = A[i];
				if (!a.IsId || ShouldPrintAttribute(a.Name))
					return true;
			}
			return false;
		}

		private bool WriteAttrs(LNode node, Precedence context)
		{
			var A = node.Attrs;
			if (A.Count == 0)
				return false;

			bool wroteBrack = false, extraParen = false;
			for (int i = 0; i < A.Count; i++) {
				var a = A[i];
				if (a.ArgCount <= 1 && !ShouldPrintAttribute(a.Name))
					continue;
				if (!wroteBrack) {
					wroteBrack = true;
					if (context != StartStmt) {
						extraParen = true;
						_out.Write('(', true);
					}
					_out.Write("@[", true);
				} else {
					_out.Write(',', true);
					_out.Space();
				}
				Print(A[i], Mode.InParens, StartStmt);
			}
			if (wroteBrack)
				_out.Write("] ", true);
			return extraParen;
		}

		#region Trivia printing
		
		private int PrintPrefixTrivia(LNode _n)
		{
			int parenCount = 0;
			foreach (var attr in _n.Attrs) {
				var name = attr.Name;
				if (name.Name.TryGet(0, '\0') == '#') {
					if (name == S.TriviaInParens) {
						_out.Write('(', true);
						parenCount++;
					} else if (name == S.TriviaRawTextBefore && ObeyRawText) {
						_out.Write(GetRawText(attr), true);
					} else if (!PrintExplicitTrivia) {
						if (name == S.TriviaSpaceBefore && !OmitSpaceTrivia) {
							PrintSpaces((attr.HasValue ? attr.Value ?? "" : "").ToString());
						} else if (name == S.TriviaSLCommentBefore && !OmitComments) {
							_out.Write("//", false);
							_out.Write(GetRawText(attr), true);
							_out.Newline(true);
						} else if (name == S.TriviaMLCommentBefore && !OmitComments) {
							_out.Write("/*", false);
							_out.Write(GetRawText(attr), false);
							_out.Write("*/", false);
							_out.Space();
						}
					}
				}
			}
			return parenCount;
		}

		private void PrintSuffixTrivia(LNode _n, int parenCount, string terminator)
		{
			while (parenCount-- > 0)
				_out.Write(')', true);
			if (terminator != null) {
				_out.Write(terminator, true);
				if (terminator == ";")
					_out.Newline(true);
			}

			bool spaces = false;
			foreach (var attr in _n.Attrs) {
				var name = attr.Name;
				if (name.Name.TryGet(0, '\0') == '#') {
					if (name == S.TriviaRawTextAfter && ObeyRawText) {
						_out.Write(GetRawText(attr), true);
					} else if (!PrintExplicitTrivia) {
						if (name == S.TriviaSpaceAfter && !OmitSpaceTrivia) {
							PrintSpaces((attr.HasValue ? attr.Value ?? "" : "").ToString());
							spaces = true;
						} else if (name == S.TriviaSLCommentAfter && !OmitComments) {
							if (!spaces)
								_out.Space();
							_out.Write("//", false);
							_out.Write((attr.Value ?? "").ToString(), true);
							_out.Newline(true);
							spaces = true;
						} else if (name == S.TriviaMLCommentAfter && !OmitComments) {
							if (!spaces)
								_out.Space();
							_out.Write("/*", false);
							_out.Write((attr.Value ?? "").ToString(), false);
							_out.Write("*/", false);
							spaces = false;
						}
					}
				}
			}
		}

		private bool ShouldPrintAttribute(Symbol name)
		{
			if (!S.IsTriviaSymbol(name))
				return true;

			if (name == S.TriviaRawTextBefore || name == S.TriviaRawTextAfter)
				return !ObeyRawText && (!OmitUnknownTrivia || PrintExplicitTrivia);
			else if (name == S.TriviaInParens)
				return false;
			else {
				bool known = name == S.TriviaSpaceBefore || name == S.TriviaSpaceAfter ||
					   name == S.TriviaSLCommentBefore || name == S.TriviaSLCommentAfter ||
					   name == S.TriviaMLCommentBefore || name == S.TriviaMLCommentAfter;
				if (known)
					return PrintExplicitTrivia;
				else
					return !OmitUnknownTrivia;
			}
		}
		static bool IsKnownTrivia(Symbol name)
		{
			return name == S.TriviaRawTextBefore || name == S.TriviaRawTextAfter ||
				   name == S.TriviaSLCommentBefore || name == S.TriviaSLCommentAfter ||
				   name == S.TriviaMLCommentBefore || name == S.TriviaMLCommentAfter ||
				   name == S.TriviaSpaceBefore || name == S.TriviaSpaceAfter;
		}
		
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
		static LesNodePrinterWriter _staticWriter = new LesNodePrinterWriter(_staticStringBuilder);
		static LesNodePrinter _staticPrinter = new LesNodePrinter(_staticWriter);

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
			_staticPrinter.PrintLiteralCore(value, style);
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
		public static bool IsNormalIdentifier(Symbol name)
		{
			bool bq;
			return !IsSpecialIdentifier(name, out bq);
		}
		
		static bool IsSpecialIdentifier(Symbol name, out bool backquote)
		{
			backquote = name.Name.Length == 0;
			bool special = false, first = true;
			foreach (char c in name.Name)
			{
				if (!LesLexer.IsIdContChar(c)) {
					if (LesLexer.IsSpecialIdChar(c))
						special = true;
					else
						backquote = true;
				} else if (first && !LesLexer.IsIdStartChar(c))
					special = true;
				first = false;
			}
			
			// Watch out for @`-inf_d` and @`-inf_f`, because they will be
			// interpreted as named literals if we don't backquote them.
			if (special && !backquote && (name.Name == "-inf_d" || name.Name == "-inf_f"))
				backquote = true;
			return special || backquote;
		}

		private void PrintIdOrSymbol(Symbol name, bool isSymbol)
		{
			// Figure out what style we need to use: plain, @special, or @`backquoted`
			bool backquote, special = IsSpecialIdentifier(name, out backquote); 

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
				_out.Write(G.EscapeCStyle(text, EscapeC.Control, quoteType), false);
			}
			_out.Write(quoteType, true);
		}

		static Pair<RuntimeTypeHandle,Action<LesNodePrinter, object, NodeStyle>> P<T>(Action<LesNodePrinter, object, NodeStyle> handler) 
			{ return G.Pair(typeof(T).TypeHandle, handler); }
		static Pair<K,V> P<K,V>(K key, V value) 
			{ return G.Pair(key, value); }
		static Dictionary<K,V> Dictionary<K,V>(params Pair<K,V>[] input)
		{
			var d = new Dictionary<K,V>();
			for (int i = 0; i < input.Length; i++)
				d.Add(input[i].Key, input[i].Value);
			return d;
		}
		static Dictionary<RuntimeTypeHandle,Action<LesNodePrinter, object, NodeStyle>> LiteralPrinters = Dictionary(
			P<int>    ((np, value, style) => np.PrintIntegerToString(value, style, "")),
			P<long>   ((np, value, style) => np.PrintIntegerToString(value, style, "L")),
			P<uint>   ((np, value, style) => np.PrintIntegerToString(value, style, "u")),
			P<ulong>  ((np, value, style) => np.PrintIntegerToString(value, style, "uL")),
			P<short>  ((np, value, style) => np.PrintShortInteger(value, style, "Int16")), // Unnatural. Not produced by parser.
			P<ushort> ((np, value, style) => np.PrintShortInteger(value, style, "UInt16")), // Unnatural. Not produced by parser.
			P<sbyte>  ((np, value, style) => np.PrintShortInteger(value, style, "Int8")), // Unnatural. Not produced by parser.
			P<byte>   ((np, value, style) => np.PrintShortInteger(value, style, "UInt8")), // Unnatural. Not produced by parser.
			P<double> ((np, value, style) => np.PrintDoubleToString((double)value)),
			P<float>  ((np, value, style) => np.PrintFloatToString((float)value)),
			P<decimal>((np, value, style) => np.PrintValueToString(value, "m")),
			P<bool>   ((np, value, style) => np._out.Write((bool)value? "@true" : "@false", true)),
			P<@void>  ((np, value, style) => np._out.Write("@void", true)),
			P<char>   ((np, value, style) => np.PrintStringCore('\'', false, value.ToString())),
			P<string> ((np, value, style) => {
				bool tripleQuoted = (style & NodeStyle.Alternate) != 0;
				np.PrintStringCore('"', tripleQuoted, value.ToString());
			}),
			P<Symbol> ((np, value, style) => np.PrintIdOrSymbol((Symbol)value, true)),
			P<TokenTree> ((np, value, style) => {
				np._out.Write("@{ ", true);
				np._out.Write(((TokenTree)value).ToString(TokenExt.ToString), true);
				np._out.Write(" }", true);
			}));

		private void PrintShortInteger(object value, NodeStyle style, string type)
		{
			Errors.Write(Severity.Warning, null, "LesNodePrinter: Encountered literal of type '{0}'. It will be printed as 'Int32'.", type);
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
			if ((style & NodeStyle.Alternate) != 0) {
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

		private void PrintLiteral(LNode node)
		{
			PrintLiteralCore(node.Value, node.Style);
		}
		private void PrintLiteralCore(object value, NodeStyle style)
		{
			Action<LesNodePrinter, object, NodeStyle> p;
			if (value == null)
				_out.Write("@null", true);
			else if (LiteralPrinters.TryGetValue(value.GetType().TypeHandle, out p))
				p(this, value, style);
			else {
				// TODO: add current LNode as context to error message
				Errors.Write(Severity.Error, null, "LesNodePrinter: Encountered unprintable literal of type {0}", value.GetType().Name);
				bool quote = QuoteUnprintableLiterals;
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

		#endregion
	}
}
