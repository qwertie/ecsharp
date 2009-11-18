using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Utilities.Judish.Internal
{
	internal sealed class NormalBitmapNode : NodeBase
	{
		public static readonly NormalBitmapNode Singleton = new NormalBitmapNode();
	}
	internal sealed class CompactBitmapNode : NodeBase
	{
		public static readonly CompactBitmapNode Singleton = new CompactBitmapNode();
	}



	/*
	internal struct BitmapEntry
	{
		public uint Flags;
		public object[] Refs;
	}
	internal static class BitmapNode
	{
	}
	 */
}
