using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;
using Loyc.Essentials;
using System.Diagnostics;
using Loyc.Utilities;

namespace Loyc.CompilerCore
{
	/// <summary>A mutable green node that can represent any code whatsoever.</summary>
	public class EditableGreenNode : GreenNode
	{
		GreenAndOffset _head;
		internal InternalList<GreenAndOffset> _children = InternalList<GreenAndOffset>.Empty;
		internal int _argCount;
		object _value = NonliteralValue.Value;

		public EditableGreenNode(Symbol name, int sourceWidth) : base(name, sourceWidth)
		{
		}
		public EditableGreenNode(GreenAndOffset head, int sourceWidth) : base(head.Node, sourceWidth)
		{
			_head = head;
		}
		public EditableGreenNode(GreenNode cloneFrom) : base(cloneFrom) // cloning constructor
		{
			_head = cloneFrom.HeadEx;
			Args.AddRange(cloneFrom.Args);
			if (cloneFrom.AttrCount != 0)
				Attrs.AddRange(cloneFrom.Attrs);
		}

		public override GreenNode Head { get { return _head.Node; } } // this, if name is simple
		public override GreenAndOffset HeadEx
		{
			get { return _head; } // no need to auto-freeze Head because Freeze() does so already
			set {
				G.RequireArg(value.Node != null);
				_head = value;
				_name = value.Node.Name;
				if (!value.Node.IsFrozen)
					_name = null; // do not cache Name; it could change in Head
			}
		}
		
		public override object Value
		{
			get { return _value; }
			set { ThrowIfFrozen(); _value = value; }
		}
		public new bool IsCall
		{
			get { return base.IsCall; }
			set {
				base.IsCall = value;
				if (!value)
					Args.Clear();
			}
		}
		public sealed override int ArgCount { get { return _argCount; } }
		public sealed override int AttrCount { get { return _children.Count - _argCount; } }
		public sealed override GreenAndOffset TryGetArg(int index)
		{
			if (index >= _argCount)
				return new GreenAndOffset();
			var g = _children[index];
			//AutoFreezeChild(g.Node);
			return _children[index];
		}

		public sealed override GreenAndOffset TryGetAttr(int index)
		{
			if (index >= AttrCount)
				return new GreenAndOffset();
			var g = _children[_argCount + index];
			//AutoFreezeChild(g.Node);
			return g;
		}
		public override void Name_set(Symbol name)
		{
			ThrowIfFrozen();
			if (_head.Node == this)
				_name = name;
			else
				throw new NotSupportedException("The GreenNode.Name cannot be changed directly because this node has a nontrivial Head node. Change the Head instead.");
		}
		public override void Freeze()
		{
			if (!IsFrozen) {
				for (int i = 0; i < _children.Count; i++)
				{
					var c = _children[i];
					if (!c.Node.IsFrozen)
						c.Node.Freeze();
				}
				base.Freeze();
			}
		}
		public new GreenArgList Args { get { return new GreenArgList(this); } }
		public new GreenAttrList Attrs { get { return new GreenAttrList(this); } }

		#region Methods that consider all children as a single list
		// The list claims that the number of children is ArgCount + AttrCount;
		// this[-1] is HeadEx, this[0..ArgCount] is the args, rest are attrs.

		public sealed override int ChildCount { get { return 1 + ArgCount + AttrCount; } }
		public sealed override GreenAndOffset TryGetChild(int index)
		{
			return index == 0 ? _head : _children[index - 1];
		}
		public override void SetChild(int index, GreenAndOffset value)
		{
			if (index == 0) _head = value;
			else _children[index - 1] = value;
		}
		public sealed override int IndexOf(GreenNode node) // int.MinValue for failure!
		{
			if (_head.Node == node)
				return 0;
			for (int i = 0; i < _children.Count;)
				if (_children[i++].Node == node)
					return i;
			return -1;
		}
		
		#endregion
	}
}
