using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Essentials;
using Loyc.Collections.Impl;

namespace Loyc.CompilerCore
{
	using S = ecs.CodeSymbols;
	using F = GreenFactory;
	using System.Diagnostics;

	/// <summary>Base class of <see cref="GreenSymbol"/> and <see cref="GreenLiteral"/>.</summary>
	class GreenAtom : GreenNode
	{
		public GreenAtom(Symbol name, ISourceFile sourceFile, int sourceWidth) : base(name, sourceFile, sourceWidth, false, true) {}
		protected GreenAtom(GreenNode head, ISourceFile sourceFile, int sourceWidth) : base(head, sourceFile, sourceWidth, false, true) { }
		public override GreenNode Head { get { return null; } }
		public override GreenAtOffs HeadEx { get { return new GreenAtOffs(null, 0); } set { ThrowIfFrozen(); } }
		public sealed override Symbol Kind { get { return Name; } }
		public sealed override int ArgCount { get { return 0; } }
		public sealed override int AttrCount { get { return 0; } }
		public sealed override GreenAtOffs TryGetArg(int index) { return new GreenAtOffs(); }
		public sealed override GreenAtOffs TryGetAttr(int index) { return new GreenAtOffs(); }
		public sealed override void Freeze() { Debug.Assert(IsFrozen); }
	}

	/// <summary>Represents a frozen simple symbol such as WriteLine or #while.</summary>
	class GreenSymbol : GreenAtom
	{
		public GreenSymbol(Symbol name, ISourceFile sourceFile, int sourceWidth) : base(name, sourceFile, sourceWidth) {}
	}

	/// <summary>Represents a node that holds a read-only value.</summary>
	/// <remarks>Normally, the only kinds of nodes that have values are #literal
	/// nodes, and these values are always constant types such as integers and
	/// strings, because EC# source code can't represent anything else.
	/// <para/>
	/// However, sometimes you want to attach a value to a node for analysis 
	/// purposes even though it cannot be printed out. The safest way to do this
	/// is to attach an attribute whose name starts with the prefix "#trivia_".
	/// <see cref="ecs.EcsNodePrinter"/> does not try to print "#trivia_" 
	/// attributes that it does not recognize, so you can attach information this 
	/// way without affecting the source-code printout of a syntax tree.
	/// <para/>
	/// Therefore, it is recommended that if you create GreenValueHolder instances,
	/// you should create them using a name that starts with "#trivia_", and then
	/// attach them as an attribute to some other node, for a structure like
	/// <code>[#trivia_MyVal] SomeOtherNode(); // #trivia_MyVal has a Value</code>
	/// The overloads of <see cref="GreenFactory.TriviaValue"/> create objects of
	/// this class. For example, if <c>F</c> is a <c>GreenFactory</c>, you can
	/// synthesize the above code with
	/// <code>
	/// F.Attr(F.TriviaValue("MyVal", "attached value"), F.Call($SomeOtherValue));
	/// // In plain C#, use GSymbol.Get("SomeOtherValue") instead of $SomeOtherValue
	/// </code>
	/// <para/>
	/// Another approach is to set the <see cref="Value"/> of another node 
	/// directly (e.g. <c>SomeOtherNode</c> in this example); but this approach
	/// has the disadvantage that only one algorithm can use the <see cref="Value"/>
	/// and if multiple algorithms want to use it, they would overwrite each other's
	/// values. Besides, you'd have to be careful not to change the value of a 
	/// #literal node.
	/// <para/>
	/// Please note that you cannot modify the Value after creating 
	/// GreenValueHolder, because nodes do not support "partial" freezing--they 
	/// are either frozen or not--and GreenValueHolder cannot allow you to modify
	/// anything else in the node such as the argument list, because it doesn't
	/// have one. Consequently, you can't edit the value either. If you want to be 
	/// able to edit the <see cref="Value"/> after creating the <see cref="GreenNode"/>,
	/// create an <see cref="EditableGreenNode"/> instead of this class (recall 
	/// that you can also call <see cref="Clone"/> to create an editable copy
	/// of any node.)
	/// </remarks>
	class GreenValueHolder : GreenAtom
	{
		protected readonly object _value;
		public GreenValueHolder(Symbol name, object value, ISourceFile sourceFile, int sourceWidth = -1) : base(name, sourceFile, sourceWidth)
		{
			_value = value;
		}
		public override object Value
		{
			get { return _value; }
			set { throw new InvalidOperationException(string.Format("Cannot change Value of frozen node '{0}'", ToString())); }
		}
	}

	/// <summary>Represents a frozen literal such as 123 or "Hello".</summary>
	class GreenLiteral : GreenValueHolder
	{
		public GreenLiteral(object value, ISourceFile sourceFile, int sourceWidth) : base(S.Literal, value, sourceFile, sourceWidth) {}
	}

	/// <summary>A node that has only a head (represents parenthesis).</summary>
	class GreenInParens : GreenAtom
	{
		protected readonly GreenAtOffs _head;
		public GreenInParens(GreenAtOffs head, ISourceFile sourceFile, int sourceWidth) : base(head.Node, sourceFile, sourceWidth)
			{ _head = head; Debug.Assert(head.Node.IsFrozen); }

		public override GreenNode Head { get { return _head.Node; } }
		public override GreenAtOffs HeadEx { get { return _head; } set { ThrowIfFrozen(); } }
	}

	/// <summary>A frozen nullary call node.</summary>
	class GreenSimpleCall0 : GreenNode
	{
		public    GreenSimpleCall0(Symbol name, ISourceFile sourceFile, int sourceWidth)    : base(name, sourceFile, sourceWidth, true, true) { }
		protected GreenSimpleCall0(GreenNode head, ISourceFile sourceFile, int sourceWidth) : base(head, sourceFile, sourceWidth, true, true) { }

		public sealed override Symbol Kind { get { return S.CallKind; } }
		public sealed override int AttrCount { get { return 0; } }
		public sealed override GreenAtOffs TryGetAttr(int index) { return new GreenAtOffs(); }
		public sealed override void Freeze() { Debug.Assert(IsFrozen); }
		
		// Things that derived classes override
		public override GreenNode Head { get { return null; } }
		public override GreenAtOffs HeadEx { get { return new GreenAtOffs(null, 0); } set { ThrowIfFrozen(); } }
		public override int ArgCount { get { return 0; } }
		public override GreenAtOffs TryGetArg(int index) { return new GreenAtOffs(); }
	}
	
	/// <summary>A frozen unary call node.</summary>
	/// <remarks>Caution: this node's child will also be frozen automatically.</remarks>
	class GreenSimpleCall1 : GreenSimpleCall0
	{
		public readonly GreenAtOffs Arg0;
		public    GreenSimpleCall1(Symbol name, ISourceFile sourceFile, int sourceWidth, GreenAtOffs arg0)    : base(name, sourceFile, sourceWidth) { Arg0 = arg0; arg0.Node.Freeze(); }
		protected GreenSimpleCall1(GreenNode head, ISourceFile sourceFile, int sourceWidth, GreenAtOffs arg0) : base(head, sourceFile, sourceWidth) { Arg0 = arg0; arg0.Node.Freeze(); }

		public override int ArgCount { get { return 1; } }
		public override GreenAtOffs TryGetArg(int index) { return index == 0 ? Arg0 : new GreenAtOffs(); }
	}

	/// <summary>A frozen binary call node.</summary>
	/// <remarks>Caution: this node's children will also be frozen automatically.</remarks>
	class GreenSimpleCall2 : GreenSimpleCall1
	{
		public readonly GreenAtOffs Arg1;
		public    GreenSimpleCall2(Symbol name, ISourceFile sourceFile, int sourceWidth, GreenAtOffs arg0, GreenAtOffs arg1)    : base(name, sourceFile, sourceWidth, arg0) { Arg1 = arg1; arg1.Node.Freeze(); }
		protected GreenSimpleCall2(GreenNode head, ISourceFile sourceFile, int sourceWidth, GreenAtOffs arg0, GreenAtOffs arg1) : base(head, sourceFile, sourceWidth, arg0) { Arg1 = arg1; arg1.Node.Freeze(); }
		
		public override int ArgCount { get { return 2; } }
		public override GreenAtOffs TryGetArg(int index) { return index == 0 ? Arg0 : (index == 1 ? Arg1 : new GreenAtOffs()); }
	}

	/// <summary>A frozen nullary call node with a complex head.</summary>
	/// <remarks>Caution: this node's head will also be frozen automatically.</remarks>
	class GreenCall0 : GreenSimpleCall0
	{
		protected readonly GreenAtOffs _head;
		public GreenCall0(GreenAtOffs head, ISourceFile sourceFile, int sourceWidth) : base(head.Node, sourceFile, sourceWidth) 
			{ _head = head; Debug.Assert(head.Node.IsFrozen); }
		
		public override GreenNode Head { get { return _head.Node; } }
		public override GreenAtOffs HeadEx { get { return _head; } set { ThrowIfFrozen(); } }
	}

	/// <summary>A frozen unary call node with a complex head.</summary>
	/// <remarks>Caution: this node's head and child will also be frozen automatically.</remarks>
	class GreenCall1 : GreenSimpleCall1
	{
		protected readonly GreenAtOffs _head;
		public GreenCall1(GreenAtOffs head, ISourceFile sourceFile, int sourceWidth, GreenAtOffs arg0) : base(head.Node, sourceFile, sourceWidth, arg0)
			{ _head = head; Debug.Assert(head.Node.IsFrozen); }
		
		public override GreenNode Head { get { return _head.Node; } }
		public override GreenAtOffs HeadEx { get { return _head; } set { ThrowIfFrozen(); } }
	}

	/// <summary>A frozen binary call node with a complex head.</summary>
	/// <remarks>Caution: this node's head and children will also be frozen automatically.</remarks>
	class GreenCall2 : GreenSimpleCall2
	{
		protected readonly GreenAtOffs _head;
		public GreenCall2(GreenAtOffs head, ISourceFile sourceFile, int sourceWidth, GreenAtOffs arg0, GreenAtOffs arg1) : base(head.Node, sourceFile, sourceWidth, arg0, arg1)
			{ _head = head; Debug.Assert(head.Node.IsFrozen); }
		
		public override GreenNode Head { get { return _head.Node; } }
		public override GreenAtOffs HeadEx { get { return _head; } set { ThrowIfFrozen(); } }
	}
}
