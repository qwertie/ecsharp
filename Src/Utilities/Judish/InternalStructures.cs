using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Utilities.Judish.Internal
{
	/// <summary>Holds an integer and a value. JRow arrays are used by several
	/// node types.</summary>
	internal struct JRow
	{
		public JRow(uint k, object value) { K = k; V = value; }
		
		/// <summary>Holds all or part of the remaining key.</summary>
		public uint K;
		/// <summary>Holds the value associated with a key, or a child node of 
		/// type NodeBase.</summary>
		public object V;
		
		/// <summary>Acts as an array of 32 bits</summary>
		public bool this[int index]
		{
			get { return ((K >> index) & 1) != 0; }
			set {
				Debug.Assert((uint)index < 32);
				uint bit = 1u << index;
				if (value) K |= bit;
				else K &= ~bit;
			}
		}

		public static object Free = "Free";
		public bool IsFree {
			get {
				return V == Free;
			}
		}
	}

	/// <summary>Bit array node types are represented as an array of Bits[].
	/// </summary>
	/// <remarks>It was necessary to use a special structure instead of int[] 
	/// because we need to be able to tell the difference between child nodes 
	/// and values. int[] is a legitimate type for values, so we need a 
	/// different, special type for child nodes.
	/// <para/>
	/// Bits, Compact and Normal are internal so that external code cannot 
	/// attempt to put a Bits[] array in a Judish collection (doing so would 
	/// confuse Judish and cause it to crash.)
	/// </remarks>
	internal struct Bits
	{
		public Bits(int n) { N = n; }
		public int N;

		/// <summary>Acts as an array of 32 bits</summary>
		public bool this[int index]
		{
			get { return ((N >> index) & 1) != 0; }
			set {
				Debug.Assert((uint)index < 32);
				int bit = 1 << index;
				if (value) N |= bit;
				else N &= ~bit;
			}
		}
		public static implicit operator int(Bits j) { return j.N; }
		public static implicit operator uint(Bits j) { return (uint)j.N; }
	}

	/// <summary>Some node types are represented as an array of Compact[].</summary>
	/// <remarks>It was necessary to use a special structure instead of object[] 
	/// because we need to be able to tell the difference between child nodes 
	/// and values. object[] is a legitimate type for values, so we need a 
	/// different, special type for child nodes</remarks>
	internal struct Compact
	{
		public Compact(object o) { O = o; }
		public object O;
		//public static implicit operator object(Compact j) { return j.O; }
	}
}
