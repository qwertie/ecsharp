using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.CompilerCore;
using Loyc.Collections;

namespace Loyc.Syntax
{
	public enum NodeType { Symbol, Literal, Call };

	/// <summary>TODO delete this file.</summary>
	public interface ILNode : ICloneable<LNode>
	{
		NodeType Type { get; }
		SourceRange Range { get; }
		ISourceFile Source { get; }
		NodeStyle Style { get; }
		RVList<LNode> Attrs { get; }
		
		bool IsFrozen { get; }
		string Print(NodeStyle style = NodeStyle.Statement, string indentString = "\t", string lineSeparator = "\n");
		
		LNode With(SourceRange range, NodeStyle style);
		LNode WithAttrs(RVList<LNode> attrs);
		CallNode WithArgs(RVList<LNode> args);
		LNode WithoutAttrs();
	}
	public interface ISymbolNode : ILNode
	{
		Symbol Name { get; }
		LNode WithName(Symbol name);
	}
	public interface ILiteralNode : ILNode
	{
		object Value { get; }
		LiteralNode WithValue(object value);
	}
	public interface ICallNode : ILNode
	{
		Symbol Name { get; }
		LNode Target { get; }
		RVList<LNode> Args { get; }

		CallNode WithTarget(LNode target);
		CallNode WithTarget(Symbol name);
		CallNode With(LNode target, RVList<LNode> args);
		CallNode With(Symbol target, RVList<LNode> args);

		LNode AddAttr(LNode attr);
		LNode AddAttrs(RVList<LNode> attrs) ;
		LNode AddAttrs(params LNode[] attrs) ;
		LNode AddArg(LNode arg) ;
		LNode AddArgs(RVList<LNode> args) ;
		LNode AddArgs(params LNode[] args) ;
	}
}
