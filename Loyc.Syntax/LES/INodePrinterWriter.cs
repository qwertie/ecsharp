using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Syntax.Les
{
	/// <summary>This interface is implemented by helper objects that handle the 
	/// low-level details of node printing. It is used by <see cref="LesNodePrinter"/>.</summary>
	/// <remarks>Specifically, INodePrinterWriter objects are in charge of how much
	/// to indent lines of code, and ensuring that there are spaces between tokens
	/// whenever omitting a space would screw up parsing.
	/// <para/>
	/// Although this interface is also used by EC#, I've kept it in the 
	/// Les namespace because I'm not yet confident that it's a <i>good</i> design 
	/// for arbitrary languages.</remarks>
	public interface INodePrinterWriter
	{
		/// <summary>Gets the object being written to (TextWriter or StringBuilder)</summary>
		object Target { get; }
		void Write(char c, bool finishToken);
		void Write(string s, bool finishToken);
		int Indent();
		int Dedent();
		void Space(); // should merge adjacent spaces
		void Newline(bool pending = false); // pending = don't actually write a newline until next token starts
		void BeginStatement();
		void BeginLabel();
		void Push(LNode newNode);
		void Pop(LNode oldNode);
	}

	/// <summary>Abstract base class for <see cref="INodePrinterWriter"/>. Has an
	/// protected <c>_indentLevel</c> field that is increased by Indent() and 
	/// decreased by Dedent().</summary>
	public abstract class NodePrinterWriterBase : INodePrinterWriter
	{
		protected int _indentLevel = 0;
		public abstract object Target { get; }
		public virtual void Write(char c, bool finishToken) { Write(c.ToString(), finishToken); }
		public abstract void Write(string s, bool finishToken);
		public abstract void Newline(bool pending);
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
}
