using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Collections;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Syntax
{
	public static class LNodeExt
	{
		public static RVList<LNode> WithSpliced(this RVList<LNode> list, int index, LNode node, Symbol listName)
		{
			if (node.Calls(listName))
				return list.InsertRange(index, node.Args);
			else
				return list.Insert(index, node);
		}
		public static RVList<LNode> WithSpliced(this RVList<LNode> list, LNode node, Symbol listName)
		{
			if (node.Calls(listName))
				return list.AddRange(node.Args);
			else
				return list.Add(node);
		}
		public static void SpliceInsert(this RWList<LNode> list, int index, LNode node, Symbol listName)
		{
			if (node.Calls(listName))
				list.InsertRange(index, node.Args);
			else
				list.Insert(index, node);
		}
		public static void SpliceAdd(this RWList<LNode> list, LNode node, Symbol listName)
		{
			if (node.Calls(listName))
				list.AddRange(node.Args);
			else
				list.Add(node);
		}
		public static LNode FindAttrNamed(this LNode self, Symbol name)
		{
			return self.Attrs.FindNodeNamed(name);
		}
		public static LNode FindArgNamed(this LNode self, Symbol name)
		{
			return self.Args.FindNodeNamed(name);
		}
		public static int IndexWithName(this RVList<LNode> self, Symbol name)
		{
			int i = 0;
			foreach (LNode node in self)
				if (node.Name == name)
					return i;
				else
					i++;
			return -1;
		}
		public static LNode FindNodeNamed(this RVList<LNode> self, Symbol name)
		{
			foreach (LNode node in self)
				if (node.Name == name)
					return node;
			return null;
		}
	}
}
