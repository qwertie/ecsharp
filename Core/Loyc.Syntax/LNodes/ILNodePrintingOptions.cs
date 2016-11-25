using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Syntax
{
	/// <summary>A set of relatively universal printing options that 
	/// <see cref="LNodePrinter"/>s should understand.</summary>
	/// <seealso cref="LNodePrinterOptions"/>
	public interface ILNodePrinterOptions
	{
		/// <summary>Indicates that it is preferable to add (or remove) parenthesis 
		/// to produce good-looking output, rather than to express faithfully whether 
		/// or not parentheses were present in the Loyc tree being printed.</summary>
		/// <remarks>For example, the Loyc tree <c>x * `+`(a, b)</c> (LESv3 notation)
		/// will be printed <c>x * (a + b)</c>, which is a slightly different tree 
		/// (the parenthesis add the trivia attribute #trivia_inParens.)</remarks>
		bool AllowChangeParentheses { get; }

		/// <summary>When this flag is set, comment trivia attributes are suppressed
		/// (e.g. <see cref="CodeSymbols.TriviaSLCommentAfter"/>).</summary>
		bool OmitComments { get; }

		/// <summary>Causes trivia that the printer does not recognize (other than 
		/// comments, spaces and raw text) to be dropped from the output rather than
		/// printed as attributes.</summary>
		/// <remarks>Note: Some printers may force all unknown trivia to be dropped.</remarks>
		bool OmitUnknownTrivia { get; }

		/// <summary>If supported by the printer, this option causes comments and 
		/// spaces to be printed as attributes in order to ensure faithful round-trip 
		/// parsing.</summary>
		/// <remarks>Note: Some printers may ignore <see cref="OmitUnknownTrivia"/>,
		/// <see cref="OmitSpaceTrivia"/> and <see cref="OmitComments"/> when this 
		/// flag is true.</remarks>
		bool PrintTriviaExplicitly { get; }

		/// <summary>If there are multiple ways to print a given node, this option 
		/// indicates that the printer should prefer an older, more compatible 
		/// syntactic style over new ones, where applicable.</summary>
		/// <remarks>For example, it tells the EC# printer to use C# printing mode.
		/// This option does not prevent printing of new constructs if such are 
		/// present in the code, however.
		/// </remarks>
		bool CompatibilityMode { get; }

		/// <summary>When this flag is set, the amount of whitespace in the output
		/// is reduced in a printer-defined way, in order to save bits.</summary>
		/// <remarks>This option should not suppress newlines, indents or trivia.
		/// To suppress newlines or indents, the user can use empty strings for 
		/// <see cref="IndentString"/> and <see cref="NewlineString"/>.</remarks>
		bool CompactMode { get; }

		/// <summary>Specifies the string to use for each level of indentation of 
		/// nested constructs in the language, e.g. a tab or four spaces.</summary>
		/// <remarks>
		/// If this option is null, the printer should use its default indent string.
		/// <para/>
		/// Indentation-sensitive languages should treat an empty string the same as null.
		/// </remarks>
		string IndentString { get; }
		
		/// <summary>Specifies the string to use for line breaks (typically "\n").</summary>
		/// <remarks>
		/// If this option is null, the printer should use its default newline string, which
		/// is almost always "\n".
		/// <para/>
		/// Newline-sensitive languages should treat an empty string the same as null.
		/// <para/>
		/// This string may or may not be used for line breaks inside multiline strings, 
		/// depending on how strings are defined in the language being printed.
		/// </remarks>
		string NewlineString { get; }
	}

	/// <summary>A concrete class that users can pass to an <see cref="LNodePrinter"/>.</summary>
	public class LNodePrinterOptions : ILNodePrinterOptions
	{
		public virtual bool AllowChangeParentheses { get; set; }
		public virtual bool OmitComments { get; set; }
		public virtual bool OmitUnknownTrivia { get; set; }
		public virtual bool PrintTriviaExplicitly { get; set; }
		public virtual bool CompatibilityMode { get; set; }
		public virtual bool CompactMode { get; set; }
		public virtual string IndentString { get; set; }
		public virtual string NewlineString { get; set; }

		public void CopyFrom(ILNodePrinterOptions original)
		{
			AllowChangeParentheses = original.AllowChangeParentheses;
			OmitComments = original.OmitComments;
			OmitUnknownTrivia = original.OmitUnknownTrivia;
			PrintTriviaExplicitly = original.PrintTriviaExplicitly;
			CompatibilityMode = original.CompatibilityMode;
			CompactMode = original.CompactMode;
			IndentString = original.IndentString;
			NewlineString = original.NewlineString;
		}
	}
}
