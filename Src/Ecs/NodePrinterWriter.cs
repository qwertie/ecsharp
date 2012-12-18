using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.CompilerCore;
using System.IO;

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
		void Newline();
		void BeginStatement();
		void Push(INodeReader newNode);
		void Pop(INodeReader oldNode);
	}

	public abstract class NodePrinterWriterBase : INodePrinterWriter
	{
		protected int _indentLevel = 0;
		public abstract object Target { get; }
		public virtual void Write(char c, bool finishToken) { Write(c.ToString(), finishToken); }
		public abstract void Write(string s, bool finishToken);
		public abstract void Newline();
		public abstract void Space();
		public virtual void BeginStatement() { Newline(); }

		public virtual int Indent()
		{
			return ++_indentLevel;
		}
		public virtual int Dedent()
		{
			return --_indentLevel;
		}
		public virtual void Push(INodeReader n) { }
		public virtual void Pop(INodeReader n) { }
	}

	public class SimpleNodePrinterWriter : NodePrinterWriterBase
	{
		string _indentString;
		string _lineSeparator;
		char _lastCh = '\n';
		bool _startingToken = true;
		TextWriter _out;

		public SimpleNodePrinterWriter(StringBuilder sb, string indentString = "\t", string lineSeparator = "\n") : this(new StringWriter(sb), indentString, lineSeparator) { }
		public SimpleNodePrinterWriter(TextWriter @out, string indentString = "\t", string lineSeparator = "\n")
		{
			_indentString = indentString;
			_lineSeparator = lineSeparator;
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
		public override void BeginStatement()
		{
			if (_lastCh == '\n')
				return;
			Newline();
		}
		public override void Newline()
		{
			_lastCh = '\n';

			_out.Write(_lineSeparator);
			for (int i = 0; i < _indentLevel; i++)
				_out.Write(_indentString);
		}
	}
}
