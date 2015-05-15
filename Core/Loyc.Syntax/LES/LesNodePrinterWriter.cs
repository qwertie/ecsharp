using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Loyc.Syntax.Les
{
	/// <summary>Base class for the helper classes of <see cref="LesNodePrinter"/>
	/// and <see cref="EcsNodePrinter"/>, called LesNodePrinterWriter and 
	/// EcsNodePrinterWriter. See <see cref="INodePrinterWriter"/>.</summary>
	public abstract class DefaultNodePrinterWriter : NodePrinterWriterBase
	{
		protected string _indentString;
		protected string _lineSeparator;
		protected string _labelIndent;
		protected char _lastCh = '\n';
		protected bool _startingToken = true;
		protected bool _newlinePending = false;
		protected bool _labelPending = false;
		protected TextWriter _out;

		public DefaultNodePrinterWriter(StringBuilder sb, string indentString = "\t", string lineSeparator = "\n", string labelIndent = "") : this(new StringWriter(sb), indentString, lineSeparator, labelIndent) { }
		public DefaultNodePrinterWriter(TextWriter @out, string indentString = "\t", string lineSeparator = "\n", string labelIndent = "")
		{
			_indentString = indentString;
			_lineSeparator = lineSeparator;
			_labelIndent = labelIndent;
			_out = @out;
			_indentLevel = 0;
		}

		public override object Target { get { return _out; } }

		public override void Write(char c, bool finishToken)
		{
			if (_startingToken)
				StartToken(c);
			_out.Write(c);
			if (finishToken) FinishToken(_lastCh = c);
			_startingToken = finishToken;
		}

		public override void Write(string s, bool finishToken)
		{
			if (s != "") {
				if (_startingToken)
					StartToken(s[0]);
				_out.Write(s);
				if (finishToken) FinishToken(_lastCh = s[s.Length-1]);
			} else if (finishToken)
				FinishToken(_lastCh);
			_startingToken = finishToken;
		}

		protected virtual void FinishToken(char lastCh)
		{
		}

		protected abstract void StartToken(char nextCh);

		public override void Space()
		{
			if (_lastCh != ' ')
				Write(' ', true);
		}
		public override void BeginLabel()
		{
			if (_newlinePending)
				_labelPending = true;
		}
		public override void BeginStatement()
		{
			if (_lastCh == '\n')
				return;
			Newline(true);
		}
		public override void Newline(bool pending = false)
		{
			_newlinePending = pending;
			if (!pending) {
				_lastCh = '\n';

				_out.Write(_lineSeparator);
				int level = _indentLevel;
				if (_labelPending) level--;
				for (int i = 0; i < level; i++)
					_out.Write(_indentString);
				if (_labelPending) {
					_labelPending = false;
					_out.Write(_labelIndent);
				}
			}
		}

		public virtual void Reset() { _lastCh = '\0'; }
	}

	/// <summary>Helper class of <see cref="LesNodePrinter"/> that ensures there is 
	/// a tokens are spaced apart properly.</summary>
	internal class LesNodePrinterWriter : DefaultNodePrinterWriter
	{
		public LesNodePrinterWriter(StringBuilder sb, string indentString = "\t", string lineSeparator = "\n", string labelIndent = "") : base(sb, indentString, lineSeparator, labelIndent) { }
		public LesNodePrinterWriter(TextWriter @out, string indentString = "\t", string lineSeparator = "\n", string labelIndent = "") : base(@out, indentString, lineSeparator, labelIndent) { }

		protected override void StartToken(char nextCh)
		{
			if (_newlinePending)
				Newline();
			if (LesLexer.IsIdContChar(_lastCh) && LesLexer.IsIdContChar(nextCh))
				_out.Write(' ');
			else if (LesLexer.IsOpContChar(_lastCh) && LesLexer.IsOpContChar(nextCh))
				_out.Write(' ');
			else if (_lastCh == '-' && (nextCh >= '0' && nextCh <= '9')) // - 2 is different from -2 (-(2) vs integer literal)
				_out.Write(' ');
		}
	}
}
