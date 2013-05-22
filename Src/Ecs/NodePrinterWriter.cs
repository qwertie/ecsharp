using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Loyc.CompilerCore;
using Loyc.Syntax;

namespace ecs
{
	/// <summary>This interface is implemented by helper objects that handle the 
	/// low-level details of node printing. It is used by <see cref="EcsNodePrinter"/>.</summary>
	public interface INodePrinterWriter
	{
		/// <summary>Gets the object being written to (TextWriter or StringBuilder)</summary>
		object Target { get; }
		void Write(char c, bool finishToken);
		void Write(string s, bool finishToken);
		int Indent();
		int Dedent();
		void Space(); // should merge adjacent spaces
		void Newline(bool endLine = false);
		void BeginStatement();
		void BeginLabel();
		void Push(LNode newNode);
		void Pop(LNode oldNode);
	}

	public abstract class NodePrinterWriterBase : INodePrinterWriter
	{
		protected int _indentLevel = 0;
		public abstract object Target { get; }
		public virtual void Write(char c, bool finishToken) { Write(c.ToString(), finishToken); }
		public abstract void Write(string s, bool finishToken);
		public abstract void Newline(bool endLine);
		public abstract void Space();
		public virtual void BeginStatement() { Newline(false); }
		public abstract void BeginLabel();

		public virtual int Indent()
		{
			return ++_indentLevel;
		}
		public virtual int Dedent()
		{
			return --_indentLevel;
		}
		public virtual void Push(LNode n) { }
		public virtual void Pop(LNode n) { }
	}

	public class SimpleNodePrinterWriter : NodePrinterWriterBase
	{
		string _indentString;
		string _lineSeparator;
		string _labelIndent;
		internal char _lastCh = '\n';
		bool _startingToken = true;
		bool _newlinePending = false;
		bool _labelPending = false;
		TextWriter _out;

		public SimpleNodePrinterWriter(StringBuilder sb, string indentString = "\t", string lineSeparator = "\n", string labelIndent = "") : this(new StringWriter(sb), indentString, lineSeparator, labelIndent) { }
		public SimpleNodePrinterWriter(TextWriter @out, string indentString = "\t", string lineSeparator = "\n", string labelIndent = "")
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

		void FinishToken(char lastCh)
		{
		}
		void StartToken(char nextCh)
		{
			if (_newlinePending)
				Newline();
			if ((EcsNodePrinter.IsIdentContChar(_lastCh) || _lastCh == '#')
				&& (EcsNodePrinter.IsIdentContChar(nextCh) || nextCh == '@'))
				_out.Write(' ');
			else if ((_lastCh == '#' && nextCh == '#') || (_lastCh == '+' && nextCh == '+') 
				  || (_lastCh == '-' && (nextCh == '-' || char.IsDigit(nextCh)))
			      || (_lastCh == '.' && nextCh == '.') || (_lastCh == '/' && nextCh == '*'))
				_out.Write(' ');
		}

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
	}
}
