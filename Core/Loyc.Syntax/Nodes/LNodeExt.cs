using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Collections;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Syntax
{
	/// <summary>Standard extension methods for <see cref="LNode"/>.</summary>
	public static class LNodeExt
	{
		/// <summary>Interprets a node as a list by returning <c>block.Args</c> if 
		/// <c>block.Calls(braces)</c>, otherwise returning a one-item list of nodes 
		/// with <c>block</c> as the only item.</summary>
		public static RVList<LNode> AsList(this LNode block, Symbol braces)
		{
			return block.Calls(braces) ? block.Args : new RVList<LNode>(block);
		}

		/// <summary>Converts a list of LNodes to a single LNode by using the list 
		/// as the argument list in a call to the specified identifier, or, if the 
		/// list contains a single item, by returning that single item.</summary>
		/// <param name="listIdentifier">Target of the node that is created if <c>list</c>
		/// does not contain exactly one item. Typical values include "{}" and "#splice".</param>
		/// <remarks>This is the reverse of the operation performed by <see cref="AsList(LNode,Symbol)"/>.</remarks>
		public static LNode AsLNode(this RVList<LNode> list, Symbol listIdentifier)
		{
			return list.Count == 1 ? list[0] : LNode.Call(listIdentifier, list, SourceRange.Nowhere);
		}

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


		public static LNode AttrNamed(this LNode self, Symbol name)
		{
			return self.Attrs.NodeNamed(name);
		}
		public static LNode WithoutAttrNamed(this LNode self, Symbol name)
		{
			LNode _;
			return WithoutAttrNamed(self, name, out _);
		}
		public static LNode WithoutAttrNamed(this LNode self, Symbol name, out LNode removedAttr)
		{
			var a = self.Attrs;
			for (int i = 0, c = a.Count; i < c; i++)
				if (a[i].Name == name) {
					removedAttr = a[i];
					return self.WithAttrs(a.RemoveAt(i));
				}
			removedAttr = null;
			return self;
		}
		public static LNode ArgNamed(this LNode self, Symbol name)
		{
			return self.Args.NodeNamed(name);
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
		public static LNode NodeNamed(this RVList<LNode> self, Symbol name)
		{
			foreach (LNode node in self)
				if (node.Name == name)
					return node;
			return null;
		}
	}
}
