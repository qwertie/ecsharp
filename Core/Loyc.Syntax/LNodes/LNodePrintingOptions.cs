using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Syntax
{
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
