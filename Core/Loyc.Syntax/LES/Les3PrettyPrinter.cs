using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Loyc.Syntax.Impl;

namespace Loyc.Syntax.Les
{
	/// <summary>
	/// A variant of <see cref="Les3Printer"/> that adds syntax highlighting 
	/// in one of three ways: as console output, as HTML output, or as 
	/// <see cref="LesColorCode"/> control codes. 
	/// </summary>
	/// <remarks>
	/// Create an instance by invoking the constructor, then call 
	/// <see cref="PrintToConsole"/> for console output, <see cref="PrintToHtml"/> 
	/// for HTML output, or <see cref="Les3Printer.Print(IEnumerable{ILNode})"/> 
	/// for just control codes.
	/// </remarks>
	public sealed class Les3PrettyPrinter : Les3Printer
	{
		/// <summary>The lookup table of strings for control codes (<see cref="LesColorCode"/> 
		/// values) to HTML classes, used by <see cref="PrintToHtml(IEnumerable{ILNode}, StringBuilder, bool)"/>.</summary>
		/// <remarks>This property is null by default, which causes the default 
		/// table to be used. See <see cref="GetDefaultCssClassTable()"/> for more 
		/// information.</remarks>
		public string[] ColorCodesToCssClasses { get; set; }

		/// <summary>Creates an instance of this class, which produces plain LES 
		/// augmented with control codes.</summary>
		public Les3PrettyPrinter(IMessageSink sink = null, ILNodePrinterOptions options = null) : this(null, sink, options) { }
		/// <summary>Creates an instance of this class, which produces plain LES 
		/// augmented with control codes.</summary>
		public Les3PrettyPrinter(StringBuilder target, IMessageSink sink, ILNodePrinterOptions options) : base(target, sink, options)
		{
		}

		protected override void StartToken(LesColorCode kind)
		{
			SB.Append((char)kind);
		}
		protected override LesColorCode ColorCodeForId(UString name)
		{
			if (LNode.IsSpecialName(name.ToString()) || Continuators.ContainsKey((Symbol) name.ToString()))
				return LesColorCode.SpecialId;
			else
				return LesColorCode.Id;
		}

		#region Console pretty printer

		public void PrintToConsole(ILNode node, bool endWithNewline = true)
		{
			base.Print(node, Options.UseRedundantSemicolons ? ";" : null);
			PrintToConsoleCore(SB, endWithNewline);
		}

		public void PrintToConsole(IEnumerable<ILNode> nodes, bool endWithNewline = true)
		{
			base.Print(nodes);
			PrintToConsoleCore(SB, endWithNewline);
		}

		public static void PrintToConsoleCore(StringBuilder input, bool endWithNewline = true)
		{
			var originalColor = Console.ForegroundColor;
			int depth = 0;
			char c, next = input.TryGet(0, '\0');
			for (int i = 0; i < input.Length; i++) {
				c = next;
				next = input.TryGet(i + 1, '\0');
				if (c < ' ') {
					switch(c) {
					case '\n': Console.WriteLine(); break;
					case (char)LesColorCode.None:      Console.ForegroundColor = ConsoleColor.Gray; break;
					case (char)LesColorCode.Comment:   Console.ForegroundColor = ConsoleColor.Green; break;
					case (char)LesColorCode.Id:        Console.ForegroundColor = ConsoleColor.Gray; break;
					case (char)LesColorCode.SpecialId: Console.ForegroundColor = ConsoleColor.Green; break;
					case (char)LesColorCode.Number:    Console.ForegroundColor = ConsoleColor.Magenta; break;
					case (char)LesColorCode.String:    Console.ForegroundColor = ConsoleColor.Yellow; break;
					case (char)LesColorCode.CustomLiteral: Console.ForegroundColor = ConsoleColor.Magenta; break;
					case (char)LesColorCode.KeywordLiteral: Console.ForegroundColor = ConsoleColor.Red; break;
					case (char)LesColorCode.Operator:  Console.ForegroundColor = ConsoleColor.White; break;
					case (char)LesColorCode.Separator: Console.ForegroundColor = ConsoleColor.Gray; break;
					case (char)LesColorCode.Attribute: Console.ForegroundColor = ConsoleColor.Blue; break;
					case (char)LesColorCode.Keyword:   Console.ForegroundColor = ConsoleColor.Cyan; break;
					case (char)LesColorCode.Unknown:   Console.ForegroundColor = ConsoleColor.DarkRed; break;
					case (char)LesColorCode.Opener:
						Console.ForegroundColor = ConsoleColor.Gray;
						if ((next == '(' || next == '[') && (++depth & 1) == 0)
							Console.ForegroundColor = ConsoleColor.White;
						break;
					case (char)LesColorCode.Closer:
						Console.ForegroundColor = ConsoleColor.Gray;
						if ((next == ')' || next == ']') && (--depth & 1) != 0)
							Console.ForegroundColor = ConsoleColor.White;
						break;
					default:
						Console.Write(c);
						break;
					}
				} else {
					Console.Write(c);
				}
			}
			Console.ForegroundColor = originalColor;
			if (endWithNewline)
				Console.WriteLine();
		}

		#endregion

		#region HTML pretty printer

		/// <summary>The lookup table of strings for control codes (<see cref="LesColorCode"/> values) to HTML classes.</summary>
		/// <remarks>
		/// For example, <c>GetDefaultCssClassTable()[(int)LesColorCode.Number]</c> indicates 
		/// the default CSS class to use for numbers.
		/// <para/>
		/// If the entry for a given code is null, no span element is emitted, which 
		/// shortens the output.
		/// <para/>
		/// The default class names are shared with the Pygments syntax highlighting
		/// system. A list of the CSS classes available in Pygments is available 
		/// <a href="https://github.com/zeke/pygments-tokens">at this link</a>.
		/// Only a small subset of these classes are used in this table.
		/// <para/>
		/// Here is some suitable CSS:
		/// <pre>
		/// .highlight { background-color: #f8f8f8; color: #111; }
		/// .highlight .c  { color: #5A5; } /* Comment */
		/// .highlight .n  { color: #111; } /* Name (omitted by default) */
		/// .highlight .m  { color: #909; } /* Number */
		/// .highlight .s  { color: #B44; } /* String */
		/// .highlight .l  { color: #B04; } /* Literal (other) */
		/// .highlight .kc { color: #41F; } /* Keyword.Constant */
		/// .highlight .o  { color: #940; } /* Operator */
		/// .highlight .p  { color: #111; } /* Punctuation (omitted by default) */
		/// .highlight .kp { color: #33A; } /* Keyword.Pseudo (@attribute) */
		/// .highlight .nb { color: #007; } /* Name.Builtin (#specialId) */
		/// .highlight .k  { color: #11F; } /* Keyword (.dotId) */
		/// .highlight .x  { color: #D00; } /* Other */
		/// .highlight .pi { color: #B50; } /* Parenthesis Inner (()) */
		/// </pre>
		/// <para/>
		/// Note: LesTokenCode.Opener and LesTokenCode.Closer are handled 
		/// specially. An opener and its matching closer (e.g. '(' and ')') are always 
		/// given the same color, but nested parens/brackets are given alternating 
		/// colors (CSS classes), with the entry for LesTokenCode.Opener used for outer
		/// parens and the entry for LesTokenCode.Closer used for inner parens. The
		/// default class name is "pi" for inner parentheses; no class name is used
		/// for outer parens. "pi" is not a standard name, so if you're using a 
		/// standard Pygment stylesheet you should add an extra line, e.g.
		/// <pre>
		/// .highlight .p { color: #111; } /* Punctuation (includes , ; { }) */
		/// .highlight .pi { color: #B50; } /* Parenthesis Inner */
		/// </pre>
		/// </remarks>
		public static string[] GetDefaultCssClassTable()
		{
			var names = new string[32];
			names[(int)LesColorCode.Comment       ] = "c";  // Comment
			names[(int)LesColorCode.Id            ] = null;  // Name is "n" but use null to shorten output
			names[(int)LesColorCode.Number        ] = "m";  // Number
			names[(int)LesColorCode.String        ] = "s";  // String
			names[(int)LesColorCode.CustomLiteral ] = "l";  // Literal
			names[(int)LesColorCode.KeywordLiteral] = "kc"; // Keyword.Constant
			names[(int)LesColorCode.Operator      ] = "o";  // Operator
			names[(int)LesColorCode.Separator     ] = null;  // Punctuation is "p" but use null to shorten output
			names[(int)LesColorCode.Attribute     ] = "kp"; // Keyword.Pseudo
			names[(int)LesColorCode.SpecialId     ] = "nb"; // Name.Builtin
			names[(int)LesColorCode.Keyword       ] = "k"; // Keyword
			names[(int)LesColorCode.Unknown       ] = "x"; // Other
			names[(int)LesColorCode.Opener        ] = null; // Outer parenthesis
			names[(int)LesColorCode.Closer        ] = "pi"; // Inner parenthesis
			return names;
		}

		internal static readonly string[] DefaultCssClassTable = GetDefaultCssClassTable();

		/// <summary>Prints an LNode as LESv3 with HTML syntax highlighting elements.</summary>
		/// <param name="nodes">Syntax trees to print.</param>
		/// <param name="output">Output StringBuilder for HTML code.</param>
		/// <param name="addPreCode">Whether to wrap the output in "&lt;pre class='highlight'>&lt;code>" tags.</param>
		/// <param name="options">Options to control the style for code printing.</param>
		/// <returns>The output StringBuilder</returns>
		public static StringBuilder PrintToHtml(
				IEnumerable<ILNode> nodes, StringBuilder output = null, 
				bool addPreCode = true, IMessageSink sink = null,
				ILNodePrinterOptions options = null)
		{
			var pp = new Les3PrettyPrinter(sink, options);
			return pp.PrintToHtml(nodes, output, addPreCode);
		}

		/// <inheritdoc cref="PrintToHtml(IEnumerable{ILNode}, StringBuilder, bool, IMessageSink, ILNodePrinterOptions)"/>
		public StringBuilder PrintToHtml(IEnumerable<ILNode> nodes, StringBuilder output = null, bool addPreCode = true)
		{
			var newline = Options.NewlineString;
			Options.NewlineString = "\n";
			Print(nodes);
			Options.NewlineString = newline;
			return PrintToHtmlCore(SB, output, addPreCode, newline, ColorCodesToCssClasses);
		}

		/// <inheritdoc cref="PrintToHtml(IEnumerable{ILNode}, StringBuilder, bool, IMessageSink, ILNodePrinterOptions)"/>
		public StringBuilder PrintToHtml(ILNode node, StringBuilder output = null, bool addPreCode = true)
		{
			var newline = Options.NewlineString;
			Options.NewlineString = "\n";
			Print(node);
			Options.NewlineString = newline;
			return PrintToHtmlCore(SB, output, addPreCode, newline, ColorCodesToCssClasses);
		}

		/// <summary>Converts a StringBuilder with <see cref="LesColorCode"/> 
		/// control codes to HTML with Pygments CSS class codes.</summary>
		/// <param name="input">Input containing <see cref="LesColorCode"/> control characters.</param>
		/// <param name="output">Output StringBuilder for HTML code. If null, a new one is created.</param>
		/// <param name="addPreCode">Whether to wrap the output in "&lt;pre class='highlight'>&lt;code>" tags.</param>
		/// <param name="newline">What to write to <c>output</c> when '\n' is encountered.</param>
		/// <param name="colorCodesToCssClasses">CSS class table for span tags, 
		/// see <see cref="GetDefaultCssClassTable"/>.</param>
		/// <returns>The output StringBuilder.</returns>
		public static StringBuilder PrintToHtmlCore(
				StringBuilder input, StringBuilder output = null, 
				bool addPreCode = true, string newline = "\n",
				string[] colorCodesToCssClasses = null)
		{
			CheckParam.Arg("output", output != input);
			colorCodesToCssClasses = colorCodesToCssClasses ?? DefaultCssClassTable;
			output = output ?? new StringBuilder(input.Length);
			if (addPreCode)
				output.Append("<pre class='highlight'><code>");

			string cssClass = null;
			int depth = 0;
			char c, next = input.TryGet(0, '\0');
			for (int i = 0; i < input.Length; i++) {
				c = next;
				next = input.TryGet(i + 1, '\0');
				if (c < colorCodesToCssClasses.Length) {
					if (c <= '\n') {        // \n is 10
						if (c == '\n') {
							output.Append(newline);
						} else if (c != '\0') {
							// If the LNode contains control codes, printer should have \escaped them
							Debug.Assert(c == '\t'); // \t is 9 
							output.Append(c);
						}
					} else if (c == (char)LesColorCode.Opener) {
						if (next == '(' || next == '[')
							c = (char)((++depth & 1) == 0 ? LesColorCode.Closer : LesColorCode.Opener);
						else
							c = (char)LesColorCode.Separator;
					} else if (c == (char)LesColorCode.Closer) {
						if (next == ')' || next == ']')
							c = (char)((--depth & 1) == 1 ? LesColorCode.Closer : LesColorCode.Opener);
						else
							c = (char)LesColorCode.Separator;
					}

					var newClass = colorCodesToCssClasses[(int)c];
					if (newClass != cssClass) {
						if (cssClass != null)
							output.Append("</span>");
						if (newClass != null)
							output.Append("<span class='").Append(newClass).Append("'>");
						cssClass = newClass;
					}
					if (c == (char)LesColorCode.Attribute && next == '@' && 
						input.TryGet(i + 2, '\0').IsOneOf((char)LesColorCode.Id, (char)LesColorCode.Number, (char)LesColorCode.KeywordLiteral, (char)LesColorCode.CustomLiteral, (char)LesColorCode.String))
					{
						output.Append('@');
						// skip over @ and the next control code to extend attribute coloring over it
						i += 2;
					}
				} else if (c == '<') {
					output.Append("&lt;");
				} else if (c == '&') {
					output.Append("&amp;");
				} else {
					output.Append(c);
				}
			}
			if (cssClass != null)
				output.Append("</span>");

			if (addPreCode)
				output.Append("</code></pre>");
			return output;
		}

		#endregion
	}

	/// <summary>These codes are produced as control characters (i.e. cast to char)
	/// in the output of <see cref="Les3PrettyPrinter"/>.</summary>
	/// <remarks>
	/// A note about implementation coupling: <see cref="Les3PrettyPrinter"/> relies 
	/// on color codes provided to it by the base class <see cref="Les3Printer"/>.
	/// </remarks>
	public enum LesColorCode
	{
		None          = 0,   // Space or newline
		Comment       = 14,
		Id            = 15,
		Number        = 16,
		String        = 17,
		CustomLiteral = 18,  // e.g. re"custom", @@custom
		KeywordLiteral = 19, // e.g. true, null, @@nan.d
		Operator      = 21,  // e.g. ++, '>s
		Separator     = 22,  //      , ;
		Attribute     = 23,  //      @
		Keyword       = 25,  // .dot-keyword
		SpecialId     = 26,  // only produced by Les3PrettyPrinter
		Unknown       = 28,  // raw text
		Opener        = 29,  // ( { [
		Closer        = 30,  // ) } ]
	}
}
