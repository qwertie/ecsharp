using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Loyc.Syntax;
using Loyc.Collections;
using Loyc.Syntax.Impl;

namespace Loyc.Ecs
{
	/// <summary>Helper class of <see cref="EcsNodePrinter"/></summary>
	internal class EcsNodePrinterWriter : Loyc.Syntax.Impl.LNodePrinterHelper
	{
		public EcsNodePrinterWriter(StringBuilder sb, string indentString = "\t", string lineSeparator = "\n", string labelIndent = "", Action<ILNode, IndexRange, int> saveRange = null)
			: base(sb, saveRange, false, indentString, lineSeparator, labelIndent, "  ") { }

		private int _punctuationIdentifierEndIndex = -1; // index where an identifier starting with @' ended
		internal void OnPunctuationIdentifierEnding() => _punctuationIdentifierEndIndex = StringBuilder.Length;

		protected override void OnNodeChanged(char nextCh)
		{
			var _lastCh = IsAtStartOfLine ? '\n' : LastCharWritten;
			if ((EcsValidators.IsIdentContChar(_lastCh) || _lastCh == '#')
				&& (EcsValidators.IsIdentContChar(nextCh) || nextCh == '@'))
				Write(' ');
			else if ((_lastCh == '#' && nextCh == '#') || (_lastCh == '+' && nextCh == '+')
				  || (_lastCh == '-' && nextCh == '-')
				  || (_lastCh == '.' && (nextCh == '.' || char.IsDigit(nextCh)))
				  || (_lastCh == '/' && nextCh == '*'))
				Write(' ');
			// EC# allows operator punctuation in identifiers after @' (or @0 to @9), 
			// e.g. @'Foo= is an identifier. So we must not print an expression like 
			// @'Foo . Bar as @'Foo.Bar which would be a single identifier.
			else if (_punctuationIdentifierEndIndex == StringBuilder.Length && _lastCh != '`' && nextCh > ' ' && nextCh != '(' && nextCh != '[' && nextCh != ')' && nextCh != ']' && nextCh != ';' && nextCh != ',')
				Write(' ');
		}

		public override void Reset()
		{
			base.Reset();
			_punctuationIdentifierEndIndex = -1;
		}
	}
}
