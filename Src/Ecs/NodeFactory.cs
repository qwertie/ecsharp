using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using S = ecs.CodeSymbols;
using Loyc.Utilities;

namespace Loyc.CompilerCore
{
	public class NodeFactory
	{
		GreenFactory F;
		public ISourceFile File { get { return F.File; } set { F.File = value; } }

		public NodeFactory(ISourceFile file) { F = new GreenFactory(file); }

		public Node Symbol(string name, int position = -1, int sourceWidth = -1)
		{
			return Node.CursorFromGreen(F.Symbol(name, sourceWidth), position);
		}
		public Node Symbol(Symbol name, int position = -1, int sourceWidth = -1)
		{
			return Node.CursorFromGreen(F.Symbol(name, sourceWidth), position);
		}
		public Node Symbol(Node location, Symbol name)
		{
			return Node.CursorFromGreen(new GreenSymbol(name, location.SourceFile, location.SourceWidth), location.SourceIndex);
		}
		public Node Literal(object value, int position = -1, int sourceWidth = -1)
		{
			return Node.CursorFromGreen(F.Literal(value, sourceWidth), position);
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
			return Node.CursorFromGreen(new GreenSimpleCall0(name, File, sourceWidth), position);
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
		public Node Call(Symbol name, params Node[] list)
		{
			return Call(name, list, -1);
		}
		public Node Call(Symbol name, Node[] list, int position, int sourceWidth = -1)
		{
			return AddArgs(Node.NewSyntheticCursor(name, new SourceRange(File, position, sourceWidth)), list);
		}
		
		// These should be static, but it causes C# compiler to whine, whine, 
		// whine when you try to call the methods through an instance reference.
		
		public Node Call(Node location, Symbol name)
		{
			return Node.CursorFromGreen(new GreenSimpleCall0(name, location.SourceFile, location.SourceWidth), location.SourceIndex);
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
		public Node Call(Node location, Symbol name, params Node[] list)
		{
			var node = Node.NewSyntheticCursor(name, location.SourceRange);
			return AddArgs(node, list);
		}
		private static Node AddArgs(Node n, Node[] list)
		{
			n.IsCall = true;
			var a = n.Args;
			for (int i = 0; i < list.Length; i++)
				a.Add(list[i]);
			return n;
		}

		public Node Braces(params Node[] contents)
		{
			return Call(S.Braces, contents);
		}
		public Node Braces(Node[] contents, int position = -1, int sourceWidth = -1)
		{
			return Call(S.Braces, contents, position, sourceWidth);
		}
		public Node List(params Node[] contents)
		{
			return Call(S.List, contents);
		}
		public Node List(Node[] contents, int position = -1, int sourceWidth = -1)
		{
			return Call(S.List, contents, position, sourceWidth);
		}
		public Node Def(Node retType, Symbol name, Node argList, Node body = null, int position = -1, int sourceWidth = -1)
		{
			return Def(retType, Symbol(name), argList, body, sourceWidth);
		}
		public Node Def(Node retType, Node name, Node argList, Node body = null, int position = -1, int sourceWidth = -1)
		{
			G.Require(argList.Name == S.List || argList.Name == S.Missing);
			Node def;
			if (body == null) def = Call(S.Def, retType, name, argList, position, sourceWidth);
			else def = Call(S.Def, new Node[] { retType, name, argList, body }, position, sourceWidth);
			return def;
		}
	}
}
