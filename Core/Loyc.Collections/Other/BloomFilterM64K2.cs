using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Utilities
{
	/// <summary>A bloom filter for very small sets.</summary>
	/// <remarks>
	/// Please see the following article for an introduction to the bloom filter:
	/// 
	/// http://www.devsource.com/c/a/Languages/Bloom-Filters-in-C/
	/// <para/>
	/// This bloom filter's parameters are m=64 and k=2, so it contains just a
	/// single long value. If item hashes are random, the false positive rate (p)
	/// is under 5% if the set contains no more than 8 items, and under 10% if the
	/// set holds no more than 12 items. This is according to the calculator at 
	/// 
	/// http://www-static.cc.gatech.edu/~manolios/bloom-filters/calculator.html
	/// <para/>
	/// The two 6-bit hashes this filter uses are simply the lowest 12 bits of the
	/// hashcode.
	/// <para/>
	/// If this filter is used to hold Symbols, it should be noted that the IDs 
	/// are not random but sequentially allocated, so it is likely to have
	/// a different false positive rate. Tentatively, I believe the number of bits
	/// set will be higher, leading to a worse false positive rate on random
	/// membership tests; but when testing related inputs, the false positive rate
	/// should be lower than the worst case.
	/// <para/>
	/// In any case, this filter performs increasing poorly as the number of
	/// elements increases: at 40 items, p exceeds 50%.
	/// </remarks>
	public struct BloomFilterM64K2
	{
		ulong _bits;

		public bool IsEmpty { get { return _bits == 0; } }
		public void Clear() { _bits = 0; }

		private ulong Mask(int id)
		{
			// We rely on C#'s promise to discard the high bits of the shift amount.
			return ((ulong)1 << id) | ((ulong)1 << (id >> 6));
		}
		public void Add(Symbol symbol) { _bits |= Mask(symbol.Id); }
		public void Add(object obj) { _bits |= Mask(obj.GetHashCode()); }
		public void Add(int hashCode) { _bits |= Mask(hashCode); }

		public bool MayContain(Symbol symbol) { return symbol != null && MayContain(symbol.Id); }
		public bool MayContain(object obj) { return MayContain(obj.GetHashCode()); }
		public bool MayContain(int hashCode)
		{
			ulong h = Mask(hashCode);
			return (_bits & h) == h;
		}
	}
}
