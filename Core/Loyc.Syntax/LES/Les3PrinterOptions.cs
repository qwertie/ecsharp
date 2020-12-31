using System;

namespace Loyc.Syntax.Les
{
	/// <summary>A set of extended options supported when printing in LES3.</summary>
	/// <remarks>This type can be used, for example, in a call to 
	/// <see cref="Les3LanguageService.Print(ILNode, IMessageSink, ParsingMode, ILNodePrinterOptions)"/>.</remarks>
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
					SpaceAroundInfixStopPrecedence = LesPrecedence.Multiply.Hi + 1;
					SpaceAfterPrefixStopPrecedence = LesPrecedence.Multiply.Hi + 1;
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
		/// (e.g. <see cref="CodeSymbols.TriviaNewline"/>).</summary>
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
		/// (2) ignoring the <see cref="CodeSymbols.TriviaAppendStatement"/> 
		/// attribute when an expression within a braced block starts after this 
		/// column on a line.
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
					CheckParam.ThrowBadArgument("DigitSeparator must be '_' or single quote.");
				_digitSeparator = value;
			}
		}
	}
}
