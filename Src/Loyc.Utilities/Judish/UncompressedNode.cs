using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Utilities.Judish.Internal
{
	internal sealed class NormalUncompressedNode : NodeBase
	{
		public static readonly NormalUncompressedNode Singleton = new NormalUncompressedNode();
	}
	internal sealed class CompactUncompressedNode : NodeBase
	{
		public static readonly CompactUncompressedNode Singleton = new CompactUncompressedNode();
	}
	internal sealed class BitArrayNode : NodeBase
	{
		public static readonly CompactBitmapNode Singleton = new CompactBitmapNode();
	}
}
