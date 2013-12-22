using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Loyc.CompilerCore;
using Loyc.Syntax;

namespace Ecs
{
	public class EcsNodePrinterWriter : Loyc.Syntax.Les.DefaultNodePrinterWriter
	{
		public EcsNodePrinterWriter(StringBuilder sb, string indentString = "\t", string lineSeparator = "\n", string labelIndent = "") : base(sb, indentString, lineSeparator, labelIndent) { }
		public EcsNodePrinterWriter(TextWriter @out, string indentString = "\t", string lineSeparator = "\n", string labelIndent = "") : base(@out, indentString, lineSeparator, labelIndent) { }

		protected override void StartToken(char nextCh)
		{
			if (_newlinePending)
				Newline();
			if ((EcsNodePrinter.IsIdentContChar(_lastCh) || _lastCh == '#')
				&& (EcsNodePrinter.IsIdentContChar(nextCh) || nextCh == '@'))
				_out.Write(' ');
			else if ((_lastCh == '#' && nextCh == '#') || (_lastCh == '+' && nextCh == '+') 
			      || (_lastCh == '-' && (nextCh == '-' || char.IsDigit(nextCh)))
			      || (_lastCh == '.' && (nextCh == '.' || char.IsDigit(nextCh)))
			      || (_lastCh == '/' && nextCh == '*'))
				_out.Write(' ');
		}
	}
}
