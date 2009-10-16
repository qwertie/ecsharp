using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Utilities.Judish
{
	internal struct LinearEntry
	{
		// KeyEtc normally contains 1 to 3 bytes of "key" and 1 byte of flags (in
		// the low byte). However, if this is a root-level leaf then KeyEtc MAY
		// hold a 4-byte key, depending on a flag in the header entry.
		// 
		// If this is a root-level leaf then KeyEtc's low two bytes are 0, and bits
		// 16-30 store a flag for each of up to 15 items. The flag indicates
		// whether the item's KeyEtc field holds a 4-byte key.
		public uint KeyEtc;
		public object Value;
	}
	internal static class LinearNode
	{
		public static object Add(this LinearEntry[] self, ref KeyShifter key, object value)
		{
			// First we need to find out if the key is an existing prefix or a new one.
			int index = self.Search(ref key);
		}

		private static int Search(this LinearEntry[] self, ref KeyShifter key)
		{
			// The entries are sorted by key. The keys are variable-length and 1 to
			// 4 bytes of the key are stored in each LinearEntry. Sometimes it will
			// happen that a long key is prefixed by a short key in the same node,
			// e.g. self[2]'s key may be 0x4567 while self[3]'s key is 0x456700.
			// In that case, the shorter key is always stored first.
			uint longKeyFlags = self[0].KeyEtc >> 15;

			for (int i = 1; i < self.Length; i++)
			{
				uint curKey = self[i].KeyEtc;
				if (KeyEtc

				
				uint searchKey = key.KeyPart;
				if ((longKeyFlags & (1 << middle)) == 0)
				{
					curKey &= ~0xFF;
					searchKey &= ~0xFF;
				}
			}
		}
	}
}
