using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Utilities.Judish
{
	public class JByteArraySerializer : JKeySerializer<byte[]>
	{
		byte[] _key;
		int _pos;

		public override int BeginSerialize(byte[] key)
		{
			_key = key;
			return key.Length;
		}
		public override uint GetBytes(int numBytes)
		{
			int pos = _pos;
			_pos += numBytes;
			if (numBytes <= 1) {
				return _key[pos];
			} else if (numBytes >= 4) {
				return (uint)((_key[pos] << 24) 
				            | (_key[pos+1] << 16) 
							| (_key[pos+2] << 8) 
							| _key[pos+3]);
			} else {
				int v = (_key[pos] << 8) | _key[pos + 1];
				if (numBytes == 2)
					return (uint)v;
				v = (v << 8) | _key[pos + 2];
				return (uint)v;
			}
		}

		public override byte[] Deserialize(JKeyEnumerator key, int keyLength)
		{
			byte[] array = new byte[keyLength];
			for (int i = 0; i < keyLength; i++)
				array[i] = (byte)key.GetBytes(1);
			return array;
		}
	}
}
