using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Loyc.Utilities;
using S = Loyc.Syntax.CodeSymbols;
using System.Diagnostics;

namespace Loyc.Syntax.Les
{
	/// <summary>Prints a Loyc tree in LES (Loyc Expression Syntax) format.</summary>
	public class LesNodePrinter
	{
		INodePrinterWriter _out;
		IMessageSink _errors;

		public INodePrinterWriter Writer { get { return _out; } set { _out = value; } }
		public IMessageSink Errors { get { return _errors; } set { _errors = value ?? MessageSink.Null; } }

		/// <summary>Introduces extra parenthesis to express precedence, without
		/// using an empty attribute list @() to allow perfect round-tripping.</summary>
		/// <remarks>For example, the Loyc tree <c>x * #+(a, b)</c> will be printed 
		/// <c>x * (a + b)</c>, which is a different tree (due to the parenthesis, 
		/// <c>a + b</c> is nested in a call to the empty identifier \\``, which
		/// represents parenthesis.)</remarks>
		public bool AllowExtraParenthesis { get; set; }

		/// <summary>When an argument to a method or macro has the value #missing,
		/// it will be omitted completely if this flag is set.</summary>
		public bool OmitMissingArguments { get; set; }

		/// <summary>When this flag is set, space trivia attributes are ignored
		/// (e.g. <see cref="CodeSymbols.TriviaSpaceAfter"/>).</summary>
		public bool OmitSpaceTrivia { get; set; }

		/// <summary>When this flag is set, comment trivia attributes are ignored
		/// (e.g. <see cref="CodeSymbols.TriviaSLCommentAfter"/>).</summary>
		public bool OmitComments { get; set; }

		/// <summary>When this flag is set, raw text trivia attributes are ignored
		/// (e.g. <see cref="CodeSymbols.TriviaRawTextBefore"/>).</summary>
		public bool OmitRawText { get; set; }

		/// <summary>When the printer encounters an unprintable literal, it calls
		/// Value.ToString(). When this flag is set, the string is placed in double
		/// quotes; when this flag is clear, it is printed as raw text.</summary>
		public bool QuoteUnprintableLiterals { get; set; }

		#region Constructors, New(), and default Printer

		public static LesNodePrinter New(StringBuilder target, string indentString = "\t", string lineSeparator = "\n")
		{
			return new LesNodePrinter(new LesNodePrinterWriter(target, indentString, lineSeparator));
		}
		public static LesNodePrinter New(TextWriter target, string indentString = "\t", string lineSeparator = "\n")
		{
			return new LesNodePrinter(new LesNodePrinterWriter(target, indentString, lineSeparator));
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

			if (object.Equals(mode, NodeStyle.Expression) || mode == ParsingService.Exprs)
				p.Print(node, 0, StartExpr);
			else
				p.Print(node, 0, StartStmt);

			p._out = null;
			p.Errors = null;
		}

		public LesNodePrinter(INodePrinterWriter target, IMessageSink errors = null)
		{
			Writer = target;
			Errors = errors;
		}

		#endregion

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

		public void Print(LNode node, Mode mode, Precedence context)
		{
			PrintPrefixNotation(node, mode, context);
			if (context == StartStmt)
				_out.Write(';', true);
		}

		static readonly int MinPrec = Precedence.MinValue.Lo;
		/// <summary>Context: beginning of statement (';' printed at the end)</summary>
		public static readonly Precedence StartStmt      = new Precedence(MinPrec, MinPrec, MinPrec);
		/// <summary>Context: beginning of main expression (potential superexpression)</summary>
		public static readonly Precedence StartExpr      = new Precedence(MinPrec+1, MinPrec+1, MinPrec+1);
		/// <summary>Context: second, third, etc. expression in a superexpression.</summary>
		public static readonly Precedence ContinueExpr   = new Precedence(MinPrec+2, MinPrec+2, MinPrec+2);

		void PrintPrefixNotation(LNode node, Mode mode, Precedence context)
		{
			bool needCloseParen = WriteAttrs(node, context);
			
			switch(node.Kind) {
				case NodeKind.Id:
					PrintIdOrSymbol(node.Name, false); break;
				case NodeKind.Literal:
					PrintLiteral(node); break;
				case NodeKind.Call: default:
					Print(node.Target, mode, LesPrecedence.Primary.LeftContext(context));
					_out.Write('(', true);
					var a = node.Args;
					if (a.Count != 0)
						for (int i = 0;;) {
							Print(a[i], mode, StartExpr);
							if (++i >= a.Count)
								break;
							_out.Write(',', true);
							_out.Space();
						}
					_out.Write(')', true);
					break;
			}

			if (needCloseParen)
				_out.Write(')', true);
		}

		private bool WriteAttrs(LNode node, Precedence context)
		{
			var a = node.Attrs;
			if (a.Count == 0)
				return false;

			bool extraParen = false;
			if (!ContinueExpr.CanAppearIn(context)) {
				extraParen = true;
				_out.Write('(', true);
			}

			_out.Write('[', true);
			for (int i = 0;;) {
				Print(a[i], Mode.InParens, StartExpr);
				if (++i >= a.Count) break;
				_out.Write(',', true);
				_out.Space();
			}
			_out.Write(']', true);

			return extraParen;
		}

		#region Parts of expressions: attributes, identifiers, literals, trivia

		private void PrintPrefixTrivia(LNode node)
		{
			foreach (var attr in node.Attrs) {
				var name = attr.Name;
				if (name.Name.TryGet(0, '\0') == '#') {
					if (name == S.TriviaSpaceBefore && !OmitSpaceTrivia) {
						PrintSpaces((attr.Value ?? "").ToString());
					} else if (name == S.TriviaRawTextBefore && !OmitRawText) {
						_out.Write((attr.Value ?? "").ToString(), true);
					} else if (name == S.TriviaSLCommentBefore && !OmitComments) {
						_out.Write("//", false);
						_out.Write((attr.Value ?? "").ToString(), true);
						_out.Newline(true);
					} else if (name == S.TriviaMLCommentBefore && !OmitComments) {
						_out.Write("/*", false);
						_out.Write((attr.Value ?? "").ToString(), false);
						_out.Write("*/", false);
					}
				}
			}
		}

		private void PrintSuffixTrivia(LNode node, bool needSemicolon)
		{
			if (needSemicolon)
				_out.Write(';', true);

			foreach (var attr in node.Attrs) {
				var name = attr.Name;
				if (name.Name[0] == '#') {
					if (name == S.TriviaSpaceAfter && !OmitSpaceTrivia) {
						PrintSpaces((attr.Value ?? "").ToString());
					} else if (name == S.TriviaRawTextAfter && !OmitRawText) {
						_out.Write((attr.Value ?? "").ToString(), true);
					} else if (name == S.TriviaSLCommentAfter && !OmitComments) {
						_out.Write("//", false);
						_out.Write((attr.Value ?? "").ToString(), true);
						_out.Newline(true);
					} else if (name == S.TriviaMLCommentAfter && !OmitComments) {
						_out.Write("/*", false);
						_out.Write((attr.Value ?? "").ToString(), false);
						_out.Write("*/", false);
					}
				}
			}
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

		private void PrintIdOrSymbol(Symbol name, bool isSymbol)
		{
			// Figure out what style we need to use: plain, \\special, or \\`backquoted`
			bool special = isSymbol, backquote = name.Name.Length == 0, first = true;
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

			if (special || backquote)
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
			P<double> ((np, value, style) => np.PrintValueToString(value, "d")),
			P<float>  ((np, value, style) => np.PrintValueToString(value, "f")),
			P<decimal>((np, value, style) => np.PrintValueToString(value, "m")),
			P<bool>   ((np, value, style) => np._out.Write((bool)value? "@true" : "@false", true)),
			P<@void>  ((np, value, style) => np._out.Write("@void", true)),
			P<char>   ((np, value, style) => np.PrintStringCore('\'', false, value.ToString())),
			P<string> ((np, value, style) => {
				bool tripleQuoted = (style & NodeStyle.Alternate) != 0;
				np.PrintStringCore('"', tripleQuoted, value.ToString());
			}),
			P<Symbol> ((np, value, style) => np.PrintIdOrSymbol((Symbol)value, true)));

		private void PrintShortInteger(object value, NodeStyle style, string type)
		{
			Errors.Write(Severity.Warning, "Encountered literal of type '{0}'. It will be printed as 'Int32'.", type);
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
				Errors.Write(Severity.Error, "Encountered unprintable literal of type {0}", value.GetType().Name);
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
