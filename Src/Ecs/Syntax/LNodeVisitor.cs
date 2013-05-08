using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Syntax
{
	/// <summary>Base class for people that want to implement the visitor pattern with <see cref="LNode"/>.</summary>
	public abstract class LNodeVisitor : ILNodeVisitor
	{
		public void Visit(LNode node) { node.Call(this); }
		public abstract void Visit(SymbolNode node);
		public abstract void Visit(LiteralNode node);
		public abstract void Visit(CallNode node);
	}
	/// <summary>Interface for people that want to implement the visitor pattern with <see cref="LNode"/>.
	/// If your visitor does not need a base class, use <see cref="LNodeVisitor"/> as the base class.</summary>
	public interface ILNodeVisitor
	{
		void Visit(SymbolNode node);
		void Visit(LiteralNode node);
		void Visit(CallNode node);
	}
}
