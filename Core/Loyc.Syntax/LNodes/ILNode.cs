using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;

namespace Loyc.Syntax
{
	/// <summary>A read-only interface for objects that act as Loyc trees.</summary>
	/// <remarks>
	/// To simplify implementations of this interface, there is no separate 
	/// list of attributes and arguments; ILNode itself acts as a list of child
	/// nodes. The argument list is numbered from 0 to Max, so the number of 
	/// arguments is <c>Max + 1</c> (which is what the ArgCount() extension method 
	/// returns).
	/// <para/>
	/// A node with no children must report <c>Min = -1</c> and <c>Max = -2</c>,
	/// or some extension methods will malfunction.
	/// <para/>
	/// You can think of any node <c>N</c> as having a single contiguous list 
	/// of child nodes indexed from <c>N.Min</c> to <c>N.Max</c>. Items with
	/// non-negative indexes (e.g. <c>N[0], N[1]</c>) are arguments; <c>N[-1]</c>
	/// is an alias for <c>Target</c>; and indexes below -1 refer to attributes
	/// (e.g. <c>N[-2]</c> is the last attribute in the attribute list)
	/// <para/>
	/// ArgCount() and AttrCount() are implemented as extension methods; they
	/// return <c>Max + 1</c> and <c>-Min - 1</c> respectively (note: ArgCount()
	/// is different from <see cref="LNode.ArgCount"/> in case 
	/// <c>Kind != LNodeKind.Call</c>: ArgCount() returns -1 in that case while
	/// <see cref="LNode.ArgCount"/> reurns zero.
	/// <para/>
	/// The IsId(), IsLiteral() and IsCall() extension methods are useful 
	/// shorthand for testing the <see cref="Kind"/> property.
	/// <para/>
	/// The Attrs() and Args() extension methods return slices of this node
	/// corresponding to the attributes and arguments.
	/// <para/>
	/// Tip: the LES node printer can print any ILNode as a string. See
	/// <see cref="Loyc.Syntax.Les.LesNodePrinter.Print(ILNode, StringBuilder, IMessageSink, object, string, string)"/>
	/// </remarks>
	public interface ILNode : IToLNode, IEquatable<ILNode>, IHasValue<object>, INegListSource<ILNode>, IHasLocation
	{
		/// <inheritdoc cref="LNode.Kind"/>
		LNodeKind Kind { get; }
		//object Value { get; } // inherited
		/// <inheritdoc cref="LNode.Name"/>
		Symbol Name { get; }
		/// <inheritdoc cref="LNode.Target"/>
		LNode Target { get; }
		/// <inheritdoc cref="LNode.Range"/>
		SourceRange Range { get; }
		/// <inheritdoc cref="LNode.Style"/>
		NodeStyle Style { get; set; }
		/// <summary>Returns true if <c>Kind == LNodeKind.Call</c>, <c>Name == name</c>, 
		/// and <c>Max + 1 >= argCount</c>.</summary>
		/// <seealso cref="LNodeExt.Calls"/>
		bool CallsMin(Symbol name, int argCount);
		/// <summary>Returns true if <c>Name == name</c> and <c>Max + 1 == argCount</c>
		/// (which implies <c>Kind == LNodeKind.Call</c> if argCount != -1).</summary>
		/// <remarks>This could have been an extension method, but when verifying that
		/// you have a certain kind of node, it's common to check <i>both</i> the Name 
		/// and ArgCount(); checking both in one call avoids extra interface invocations.</remarks>
		bool Calls(Symbol name, int argCount);
	}

	/// <summary>An interface for objects that can be converted to <see cref="LNode"/>.</summary>
	public interface IToLNode
	{
		/// <summary>Converts this object to an <see cref="LNode"/>, or returns 
		/// <c>this</c> if the object is already an <see cref="LNode"/>.</summary>
		LNode ToLNode();
	}
}
