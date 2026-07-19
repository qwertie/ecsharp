using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Compatibility;

#if NETSTANDARD2_0 || NET45 || NET46 || NET47
public static class ArraySegmentExt
{
	public static ArraySegment<T> Slice<T>(this ArraySegment<T> segment, int start)
	{
		return new ArraySegment<T>(segment.Array, segment.Offset + start, segment.Count - start);
	}

	public static ArraySegment<T> Slice<T>(this ArraySegment<T> segment, int start, int count)
	{
		return new ArraySegment<T>(segment.Array, segment.Offset + start, count);
	}
}
#endif
