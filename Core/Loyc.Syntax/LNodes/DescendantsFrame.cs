using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;
using Loyc.Collections;
using System.Diagnostics;

namespace Loyc.Syntax
{
	/// <summary>Specifies which children to enumerate when calling <see cref="LNode.Descendants"/>().</summary>
	/// <remarks>TODO code review</remarks>
	[Flags]
	public enum NodeScanMode
	{
		YieldSelf = 1,

		YieldLocalAttrs = 2,
		YieldLocalHead = 4,
		YieldLocalArgsOnly = 8,
		YieldLocalNonAttrs = YieldLocalArgsOnly | YieldLocalHead,
		YieldLocal = YieldLocalAttrs | YieldLocalNonAttrs,
			
		YieldDeepAttrs = 16,
		YieldDeepHeads = 32,
		YieldDeepArgsOnly = 64,
		YieldDeepNonAttrs = YieldDeepArgsOnly | YieldDeepHeads,
		YieldDeep = YieldDeepAttrs | YieldDeepNonAttrs,

		YieldAttrs = YieldLocalAttrs | YieldDeepAttrs,
		YieldHeads = YieldLocalHead | YieldDeepHeads,
		YieldArgsOnly = YieldLocalArgsOnly | YieldDeepArgsOnly,
		YieldNonAttrs = YieldLocalNonAttrs | YieldDeepNonAttrs,
		YieldAllChildren = YieldAttrs | YieldNonAttrs,
		YieldAll = YieldAllChildren | YieldSelf,
			
		ScanAttrs = 128,
		ScanHeads = 256,
		ScanArgsOnly = 512,
		ScanNonAttrs = ScanHeads | ScanArgsOnly,
		ScanAll = ScanAttrs | ScanNonAttrs,
	}

	/// <summary>Helper class used to enumerate <see cref="LNode.Descendants"/>().</summary>
	/// <remarks>TODO code review</remarks>
	public struct DescendantsFrame : IEnumeratorFrame<DescendantsFrame, LNode>, ICloneable<DescendantsFrame>
	{
		LNode _node;
		VList<LNode> _children;
		NodeScanMode _mode;
		int _step, _index;
		
		public DescendantsFrame(LNode node, NodeScanMode mode)
		{
			_node = node;
			_mode = mode;
			_children = VList<LNode>.Empty;
			_step = _index = 0;
		}

		public int MoveNext(ref DescendantsFrame frame, ref LNode current)
		{
			NodeScanMode subMode;
			switch(_step) {
			case 0: // the node itself
				_step = 1;
				if ((_mode & NodeScanMode.YieldSelf) != 0) {
					current = _node;
					return 1;
				}
				goto case 1;
			case 1: // consider whether to return attributes
				if ((_mode & (NodeScanMode.ScanAttrs | NodeScanMode.YieldLocalAttrs)) == 0 || _node.AttrCount == 0)
					goto case 3; // skip attrs
				_children = _node.Attrs;
				Debug.Assert(_index == 0);
				_step = 2;
				goto case 2;
			case 2: // return attributes
				Debug.Assert(_step == 2);
				if (_index < _children.Count) {
					subMode = SubMode(NodeScanMode.YieldLocalAttrs);
					frame = new DescendantsFrame(_children[_index], subMode);
					_index++;
					return -1;
				}
				goto case 3;
			case 3: // consider whether to return head
				if (_node.Target == null || (_mode & (NodeScanMode.ScanHeads | NodeScanMode.YieldLocalHead)) == 0)
					goto case 5; // skip attrs
				goto case 4;
			case 4: // return head
				subMode = SubMode(NodeScanMode.YieldLocalHead);
				frame = new DescendantsFrame(_node.Target, subMode);
				_step = 5;
				return -1;
			case 5: // consider whether to return args
				if ((_mode & (NodeScanMode.ScanArgsOnly | NodeScanMode.YieldLocalArgsOnly)) == 0 || _node.ArgCount == 0) {
					_step = -1;
					goto default; // skip args
				}
				_children = _node.Args;
				_index = 0;
				_step = 6;
				goto case 6;
			case 6: // return args
				Debug.Assert(_step == 6);
				if (_index < _children.Count) {
					subMode = SubMode(NodeScanMode.YieldLocalArgsOnly);
					frame = new DescendantsFrame(_children[_index], subMode);
					_index++;
					return -1;
				}
				_step = -1;
				goto default;
			default:
				Debug.Assert(_step == -1);
				return 0;
			}
		}
		private NodeScanMode SubMode(NodeScanMode newSelfFlag)
		{
			var subMode = (_mode & ~(NodeScanMode.YieldLocal | NodeScanMode.YieldSelf))
						| (NodeScanMode)((int)(_mode & NodeScanMode.YieldDeep) >> 3);
			if ((_mode & newSelfFlag) != 0)
				subMode |= NodeScanMode.YieldSelf;
			return subMode;
		}
		public DescendantsFrame Clone()
		{
			return this;
		}
	};
}
