using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Loyc.Collections;
using Loyc.Syntax.Impl;

namespace Loyc.Syntax.Les
{
	internal sealed class Les2PrinterWriter : LNodePrinterHelper
	{
		public Les2PrinterWriter(StringBuilder sb, string indentString = "\t", string lineSeparator = "\n", string labelIndent = "", Action<ILNode, IndexRange, int> saveRange = null)
			: base(sb, saveRange, false, indentString, lineSeparator, labelIndent, "  ", 4) { }

		internal bool JustWroteSymbolOrSpecialId;

		protected override void OnNodeChanged(char nextCh)
		{
			var lastCh = LastCharWritten;
			if (Les2Lexer.IsIdContChar(lastCh) && Les2Lexer.IsIdContChar(nextCh))
				StringBuilder.Append(' ');
			else if (Les2Lexer.IsOpContChar(lastCh) && Les2Lexer.IsOpContChar(nextCh))
				StringBuilder.Append(' ');
			else if (JustWroteSymbolOrSpecialId && Les2Lexer.IsSpecialIdChar(nextCh))
				StringBuilder.Append(' ');

			JustWroteSymbolOrSpecialId = false;
		}
	}
}
