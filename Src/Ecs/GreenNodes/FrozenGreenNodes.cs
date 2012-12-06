using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Essentials;
using Loyc.Collections.Impl;

namespace Loyc.CompilerCore
{
	using S = CodeSymbols;
	using F = GreenFactory;
	using System.Diagnostics;

	/// <summary>Base class of <see cref="GreenSymbol"/> and <see cref="GreenLiteral"/>.</summary>
	class GreenAtom : GreenNode
	{
		public GreenAtom(Symbol name, ISourceFile sourceFile, int sourceWidth) : base(name, sourceFile, sourceWidth, false, true) {}
		public sealed override GreenNode Head { get { return null; } }
		public sealed override GreenAtOffs HeadEx { get { return new GreenAtOffs(null, 0); } set { ThrowIfFrozen(); } }
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
	
	/// <summary>Represents a frozen literal such as 123 or "Hello".</summary>
	class GreenLiteral : GreenAtom
	{
		readonly object _value;
		public GreenLiteral(object value, ISourceFile sourceFile, int sourceWidth) : base(S.Literal, sourceFile, sourceWidth)
		{
			_value = value;
		}
		public override object Value
		{
			get { return _value; }
			set { throw new InvalidOperationException(string.Format("Cannot change Value of frozen node '{0}'", ToString())); }
		}
	}

	/// <summary>A frozen nullary call node.</summary>
	class GreenSimpleCall0 : GreenNode
	{
		public    GreenSimpleCall0(Symbol name, ISourceFile sourceFile, int sourceWidth)      : base(name, sourceFile, sourceWidth, true, true) { }
		protected GreenSimpleCall0(GreenAtOffs head, ISourceFile sourceFile, int sourceWidth) : base(head.Node, sourceFile, sourceWidth, true, true) { }

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
		public    GreenSimpleCall1(Symbol name, ISourceFile sourceFile, int sourceWidth, GreenAtOffs arg0)      : base(name, sourceFile, sourceWidth) { Arg0 = arg0; arg0.Node.Freeze(); }
		protected GreenSimpleCall1(GreenAtOffs head, ISourceFile sourceFile, int sourceWidth, GreenAtOffs arg0) : base(head, sourceFile, sourceWidth) { Arg0 = arg0; arg0.Node.Freeze(); }

		public override int ArgCount { get { return 1; } }
		public override GreenAtOffs TryGetArg(int index) { return index == 0 ? Arg0 : new GreenAtOffs(); }
	}

	/// <summary>A frozen binary call node.</summary>
	/// <remarks>Caution: this node's children will also be frozen automatically.</remarks>
	class GreenSimpleCall2 : GreenSimpleCall1
	{
		public readonly GreenAtOffs Arg1;
		public    GreenSimpleCall2(Symbol name, ISourceFile sourceFile, int sourceWidth, GreenAtOffs arg0, GreenAtOffs arg1)      : base(name, sourceFile, sourceWidth, arg0) { Arg1 = arg1; arg1.Node.Freeze(); }
		protected GreenSimpleCall2(GreenAtOffs head, ISourceFile sourceFile, int sourceWidth, GreenAtOffs arg0, GreenAtOffs arg1) : base(head, sourceFile, sourceWidth, arg0) { Arg1 = arg1; arg1.Node.Freeze(); }
		
		public override int ArgCount { get { return 2; } }
		public override GreenAtOffs TryGetArg(int index) { return index == 0 ? Arg0 : (index == 1 ? Arg1 : new GreenAtOffs()); }
	}

	/// <summary>A frozen nullary call node with a complex head.</summary>
	/// <remarks>Caution: this node's head will also be frozen automatically.</remarks>
	class GreenCall0 : GreenSimpleCall0
	{
		protected readonly GreenAtOffs _head;
		public GreenCall0(GreenAtOffs head, ISourceFile sourceFile, int sourceWidth) : base(head, sourceFile, sourceWidth) 
			{ _head = head; head.Node.Freeze(); }
		
		public override GreenNode Head { get { return _head.Node; } }
		public override GreenAtOffs HeadEx { get { return _head; } set { ThrowIfFrozen(); } }
	}

	/// <summary>A frozen unary call node with a complex head.</summary>
	/// <remarks>Caution: this node's head and child will also be frozen automatically.</remarks>
	class GreenCall1 : GreenSimpleCall1
	{
		protected readonly GreenAtOffs _head;
		public GreenCall1(GreenAtOffs head, ISourceFile sourceFile, int sourceWidth, GreenAtOffs arg0) : base(head, sourceFile, sourceWidth, arg0)
			{ _head = head; head.Node.Freeze(); }
		
		public override GreenNode Head { get { return _head.Node; } }
		public override GreenAtOffs HeadEx { get { return _head; } set { ThrowIfFrozen(); } }
	}

	/// <summary>A frozen binary call node with a complex head.</summary>
	/// <remarks>Caution: this node's head and children will also be frozen automatically.</remarks>
	class GreenCall2 : GreenSimpleCall2
	{
		protected readonly GreenAtOffs _head;
		public GreenCall2(GreenAtOffs head, ISourceFile sourceFile, int sourceWidth, GreenAtOffs arg0, GreenAtOffs arg1) : base(head, sourceFile, sourceWidth, arg0, arg1)
			{ _head = head; head.Node.Freeze(); }
		
		public override GreenNode Head { get { return _head.Node; } }
		public override GreenAtOffs HeadEx { get { return _head; } set { ThrowIfFrozen(); } }
	}
}
