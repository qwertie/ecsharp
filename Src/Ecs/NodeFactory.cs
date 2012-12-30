using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.CompilerCore
{
	class NodeFactory
	{
		GreenFactory F;
		public ISourceFile File { get { return F.File; } set { F.File = value; } }

		public NodeFactory(ISourceFile file) { F = new GreenFactory(file); }

		public Node Symbol(string name, int position = -1, int sourceWidth = -1)
		{
			return Node.NewCursorFromGreen(F.Symbol(name, sourceWidth), position);
		}
		public Node Symbol(Symbol name, int position = -1, int sourceWidth = -1)
		{
			return Node.NewCursorFromGreen(F.Symbol(name, sourceWidth), position);
		}
		public Node Symbol(Node location, Symbol name)
		{
			return Node.NewCursorFromGreen(new GreenSymbol(name, location.SourceFile, location.SourceWidth), location.SourceIndex);
		}
		public Node Literal(object value, int position = -1, int sourceWidth = -1)
		{
			return Node.NewCursorFromGreen(F.Literal(value, sourceWidth), position);
		}

		// Calls
		public Node Call(Node head, int sourceWidth = -1)
		{
			var node = Node.NewCursorWithHead(head, head.SourceIndex, sourceWidth <= -1 ? head.SourceWidth : sourceWidth);
			node.IsCall = true;
			return node;
		}
		public Node Call(Node head, Node _1, int sourceWidth = -1)
		{
			var node = Node.NewCursorWithHead(head, head.SourceIndex, sourceWidth);
			node.Args.Add(_1);
			return node;
		}
		public Node Call(Node head, Node _1, Node _2, int sourceWidth = -1)
		{
			var node = Node.NewCursorWithHead(head, head.SourceIndex, sourceWidth);
			var a = node.Args;
			a.Add(_1);
			a.Add(_2);
			return node;
		}
		public Node Call(Node head, Node _1, Node _2, Node _3, int sourceWidth = -1)
		{
			var node = Node.NewCursorWithHead(head, head.SourceIndex, sourceWidth);
			var a = node.Args;
			a.Add(_1);
			a.Add(_2);
			a.Add(_3);
			return node;
		}
		public Node Call(Node head, params Node[] list)
		{
			return AddArgs(Node.NewCursorWithHead(head, head.SourceIndex, -1), list);
		}
		public Node Call(Symbol name, int position = -1, int sourceWidth = -1)
		{
			return Node.NewCursorFromGreen(new GreenSimpleCall0(name, File, sourceWidth), position);
		}
		public Node Call(Symbol name, Node _1, int position = -1, int sourceWidth = -1)
		{
			var node = Node.NewSyntheticCursor(name, new SourceRange(File, position, sourceWidth));
			node.Args.Add(_1);
			return node;
		}
		public Node Call(Symbol name, Node _1, Node _2, int position = -1, int sourceWidth = -1)
		{
			var node = Node.NewSyntheticCursor(name, new SourceRange(File, position, sourceWidth));
			var a = node.Args;
			a.Add(_1);
			a.Add(_2);
			return node;
		}
		public Node Call(Symbol name, Node _1, Node _2, Node _3, int position = -1, int sourceWidth = -1)
		{
			var node = Node.NewSyntheticCursor(name, new SourceRange(File, position, sourceWidth));
			var a = node.Args;
			a.Add(_1);
			a.Add(_2);
			a.Add(_3);
			return node;
		}
		
		// These should be static, but it causes C# compiler to whine, whine, 
		// whine when you try to call the methods through an instance reference.
		
		public Node Call(Node location, Symbol name, params Node[] list)
		{
			var node = Node.NewSyntheticCursor(name, location.SourceRange);
			return AddArgs(node, list);
		}
		public Node Call(Node location, Symbol name)
		{
			return Node.NewCursorFromGreen(new GreenSimpleCall0(name, location.SourceFile, location.SourceWidth), location.SourceIndex);
		}
		public Node Call(Node location, Symbol name, Node _1)
		{
			var node = Node.NewSyntheticCursor(name, location.SourceRange);
			node.Args.Add(_1);
			return node;
		}
		public Node Call(Node location, Symbol name, Node _1, Node _2)
		{
			var node = Node.NewSyntheticCursor(name, location.SourceRange);
			var a = node.Args;
			a.Add(_1);
			a.Add(_2);
			return node;
		}
		public Node Call(Node location, Symbol name, Node _1, Node _2, Node _3)
		{
			var node = Node.NewSyntheticCursor(name, location.SourceRange);
			var a = node.Args;
			a.Add(_1);
			a.Add(_2);
			a.Add(_3);
			return node;
		}
		private static Node AddArgs(Node n, Node[] list)
		{
			n.IsCall = true;
			var a = n.Args;
			for (int i = 0; i < list.Length; i++)
				a.Add(list[i]);
			return n;
		}
	}
}
