using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Loyc.Syntax.Les
{
	/// <summary>Base class for the helper classes of <see cref="LesNodePrinter"/>
	/// and <see cref="Ecs.EcsNodePrinter"/>, called LesNodePrinterWriter and 
	/// EcsNodePrinterWriter. See <see cref="INodePrinterWriter"/>.</summary>
	public abstract class DefaultNodePrinterWriter : NodePrinterWriterBase
	{
		protected string _indentString;
		protected string _lineSeparator;
		protected string _labelIndent;
		protected char _lastCh = '\n';
		protected bool _startingToken = true;
		protected bool _newlinePending = false;
		// the final indent of a line is not written at first, in case 
		// BeginLabel() is called to suppress it. Instead, it's stored here.
		protected string _indentPending;
		protected int _lineNumber = 1;
		protected int _lastNewlineAt = 0;
		protected TextWriter _out;

		public DefaultNodePrinterWriter(StringBuilder sb, string indentString = "\t", string lineSeparator = "\n", string labelIndent = null) : this(new StringWriter(sb), indentString, lineSeparator, labelIndent) { }
		public DefaultNodePrinterWriter(TextWriter @out, string indentString = "\t", string lineSeparator = "\n", string labelIndent = null)
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
			FinishIndent();
			_out.Write(c);
			_lastCh = c;
			if (finishToken)
				FinishToken(c);
			_startingToken = finishToken;
		}

		public override void Write(string s, bool finishToken)
		{
			if (s != "") {
				if (_startingToken)
					StartToken(s[0]);
				FinishIndent();
				_out.Write(s);
				if (finishToken) FinishToken(_lastCh = s[s.Length - 1]);
			} else if (finishToken)
				FinishToken(_lastCh);
			_startingToken = finishToken;
		}

		private void FinishIndent()
		{
			if (_indentPending != null) {
				_out.Write(_indentPending);
				_indentPending = null;
			}
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
				Newline();
			if (_indentPending != null)
				_indentPending = _labelIndent;
		}
		public override void BeginStatement()
		{
			if (_lastCh == '\n')
				return;
			Newline(true);
		}
		public override void Newline(bool pending = false)
		{
			_lastCh = '\n';
			if (_newlinePending = pending)
				_startingToken = true;
			else {
				_lineNumber++;
				_out.Write(_lineSeparator);
				int level = _indentLevel;
				if (level > 0) {
					for (int i = 0; i < level - 1; i++)
						_out.Write(_indentString);
					_indentPending = _indentString;
				} else
					_indentPending = null;
			}
		}

		public virtual void Reset() { _lastCh = '\0'; }

		public override char LastCharWritten { get { return _lastCh; } }

		public override int LineNumber { get { return _lineNumber; } }
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
