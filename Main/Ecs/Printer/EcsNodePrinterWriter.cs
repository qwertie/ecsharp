using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Loyc.Syntax;
using Loyc.Collections;

namespace Loyc.Ecs
{
	/// <summary>Helper class of <see cref="EcsNodePrinter"/></summary>
	internal class EcsNodePrinterWriter : Loyc.Syntax.Les.DefaultNodePrinterWriter
	{
		public EcsNodePrinterWriter(StringBuilder sb, string indentString = "\t", string lineSeparator = "\n", string labelIndent = "", Action<ILNode, IndexRange> saveRange = null)
			: base(sb, indentString, lineSeparator, labelIndent, saveRange) { }
		public EcsNodePrinterWriter(TextWriter @out, string indentString = "\t", string lineSeparator = "\n", string labelIndent = "")
			: base(@out, indentString, lineSeparator, labelIndent) { }

		char _lastStartCh; // character at beginning of previous token

		protected override void StartToken(char nextCh)
		{
			if (_newlinePending)
				Newline();
			if ((EcsValidators.IsIdentContChar(_lastCh) || _lastCh == '#')
				&& (EcsValidators.IsIdentContChar(nextCh) || nextCh == '@'))
				_out.Write(' ');
			else if ((_lastCh == '#' && nextCh == '#') || (_lastCh == '+' && nextCh == '+')
				  || (_lastCh == '-' && nextCh == '-')
				  || (_lastCh == '.' && (nextCh == '.' || char.IsDigit(nextCh)))
				  || (_lastCh == '/' && nextCh == '*'))
				_out.Write(' ');
			else if (_lastStartCh == '@' && _lastCh != '`' && nextCh > ' ' && nextCh != '(' && nextCh != '[' && nextCh != ')' && nextCh != ']' && nextCh != ';' && nextCh != ',')
				_out.Write(' ');

			_lastStartCh = nextCh;
		}

		public override void BeginStatement()
		{
		}

		public override void Reset()
		{
			base.Reset();
			_lastStartCh = '\0';
		}
	}
}
