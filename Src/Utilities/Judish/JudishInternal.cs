using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Utilities.Judish
{
    /// <summary>
    /// Maps uint to object, or a key of arbitrary length to object. Currently, this
    /// is the only type of Judish collection.
    /// </summary><remarks>
    /// JudishInternal is intended to be used as a base class, so its methods are
    /// protected.
    /// <para/>
    /// JudishInternal implements a Judy-like digital trie. It is designed to
    /// efficiently store dictionaries of any size, while minimizing memory usage by
    /// "compressing" keys whenever the dictionary is holding a large number of
    /// items.
    /// <para/>
    /// A Judish dictionary has the following advantages over a standard Dictionary:
    /// * It is sorted and can be enumerated in order.
    /// * It uses less memory, especially if you don't usually need to associate
    ///   values with your keys (in other words, Judish is better if you want to
    ///   store a set rather than a map, or if your machine has limited memory, or
    ///   if you want to hold very large data sets)
    /// * It might be faster (TODO: benchmark)
    /// <para/>
    /// The main disadvantage is that only certain types of keys and values are
    /// supported. It is not a hash table, and hash codes are unsuitable as keys
    /// because there is no built-in way to deal with hash collisions. Acceptable
    /// keys in a Judish map are integers, strings, and byte arrays. Acceptable
    /// values are boolean (true or false) or object references; integer values and
    /// other value types are not supported.
    /// <para/>
    /// Expanses of the key space can be stored in one of three different "node
    /// formats" (linear, bitmap, or uncompressed), each format optimized for 
    /// a different key density. Key density refers to the number of keys in use
    /// divided by the key space; for example, if a key is one byte then the key
    /// space is 256, and if there are 31 unique keys then the key density is about
    /// 12%.
    /// <para/>
    /// * Low-density key spaces use linear nodes, which may be packed or sparse
    /// * Medium-density spaces use bitmap nodes
    /// * High-density spaces use uncompressed nodes.
    /// <para/>
    /// There's one wrinkle: if all values are null, a bit array can sometimes be 
    /// used instead of an uncompressed or bitmap node.
	/// <para/>
    /// Judish is inspired by the C library called Judy, but it is substantially
    /// different for a number of reasons. Most fundamentally, it is not feasible to
    /// do a direct port because .NET does not support many of the tricks Judy
    /// relies upon. Far from being not a port of Judy, Judish is a much smaller
    /// clean-room rewrite that is merely based on similar principles as Judy.
    /// <para/>
    /// There can be no doubt Judish is much slower than its C counterpart, because
    /// it has to operate within the constraints of .NET. There are two main reasons
    /// this design is slower.
    /// <para/>
    /// First, the author of Judy fine-tuned Judy's performance over a period of
    /// many years. Judish, in contrast, was written in a matter of days. Moreover,
    /// I've never even used Judy, let alone read the source code. I just read the
    /// "Shop manual"! And Judy incorporates tons more optimizations.
    /// <para/>
    /// Second, in verifiable code (which this is), .NET code is prohibited from
    /// using many of the tricks Judy uses. Even in unverifiable code, .NET code
    /// cannot use a very important trick, which is to store integer data in some
    /// pointers.
    /// <para/>
    /// The pointer part of the 2-word "Judy pointer" can sometimes store integer
    /// bitfields instead of a pointer, which I'm pretty sure can't be done in .NET
    /// even in "unsafe" code because the garbage collector isn't designed to handle
    /// it, and would crash. That's too bad, since it is quite workable on all
    /// modern architectures. All .NET pointers are aligned on at least a 4-byte
    /// boundary, so the bottom two bits are always zero. Therefore, if Microsoft
    /// wanted, it could designate the low bit of some pointers as an "integer
    /// flag". If the low bit is set, then the high 31 bits (or 63 bits on a 64-bit
    /// system) are treated like an integer instead of a pointer. This trick may
    /// sound obscure, but it is a crucial feature of some well-known software such 
    /// as the Ruby interpreter, which lets object pointers hold integers directly, 
    /// thereby allowing integers to act like all other objects, but without
    /// allocating memory for each one (memory is only allocated for large
    /// integers).
    /// <para/>
    /// It's really a shame .NET doesn't support putting integers in pointers, as it
    /// would have made it much easier to support integer values (in addition to
    /// integer keys). As it is, Judish does not support integer or other value-type
    /// values at present.
    /// <para/>
    /// Another problem is that .NET doesn't support fixed-size arrays except in
    /// unsafe code. So I can't use a structure like this:
    /// <code>
    ///     class Node { 
    ///         byte ...; 
    ///         ushort ...; 
    ///         fixed Entry Array[8];
    ///     }
    ///     struct Entry { 
    ///         public uint Flags; 
    ///         public object Value;
    ///     }
    /// </code>
    /// It would be undesirable to use a standard variable-size array in the Node
    /// class since it would incur additional overhead and sometimes would require
    /// an extra cache line to be filled (especially if the Entry[] is not adjacent 
    /// to Node in memory). To avoid creating two separate .NET objects (one for
    /// the node and one for the fixed-size array), I typically use just one .NET 
    /// object: an array whose first (and maybe second) element has special
    /// meaning.
    /// <para/>
    /// These fundamental .NET limitations force Judish to have a substantially
    /// different design than Judy. Besides that, I decided to support keys of
    /// nearly unlimited length (so that strings can more naturally be used as
    /// keys), and to combine Judy1 and JudyL in this single data structure.
    /// <para/>
    /// Performace may also be harmed by the fact that...
    /// *   .NET data structures cannot guarantee a particular alignment, and one
    ///     cannot write a custom memory manager. I assume Judy's memory manager can
    ///     align many of its data structures on 16-byte, 32-byte or 64-byte
    ///     boundaries in order to avoid straddling cache lines. As far as I know,
    ///     .NET objects only get 8-byte alignment.
    /// *   All array accesses are subject to an index-out-of-bounds check
    /// *   We need a typecast whenever a reference can point to either a value or a
    ///     subtree. In my implementation, casts are needed frequently.
    /// <para/>
    /// I don't actually know how much slower Judish is compared to Judy. Anyone
    /// care to write a benchmark?
    /// <para/>
    /// The current implementation is designed for 32-bit architectures. To improve
    /// efficiency in a 64-bit process, substantial changes might be needed, and I
    /// just haven't written such an implementation. This class can be used in
    /// 64-bit, but it uses almost twice as much memory and is therefore slower 
    /// (ideally it would use 1.5x as much memory, since only the object pointers 
    /// should double in size, not the integer keys).
    /// <para/>
    /// JudishInternal can often compress out null values, if the map holds a lot of
    /// items. Therefore, if you want to store a "set" rather than a "dictionary",
    /// just set the value associated with each key to null, and you'll save space
    /// automatically. This makes JudishInternal behave more like a Judy1 array
    /// than a JudyL array.
    /// </remarks>
	public abstract class JudishInternal
	{
		#region Data

		/// <summary>Total population, or, if there is only one key, this may hold
		/// a value between -1 and -4 to indicate the key length (1 to 4 bytes).</summary>
		internal int _count;
		/// <summary>If _count is negative, this holds the map's only key.
		/// Otherwise, its use yet to be chosen.
		/// </summary>
		internal uint _stuff;
		/// <summary>This is a pointer to the data of the map. If _count is
		/// negative, this points to map's only value; otherwise, it points to
		/// RootLeafEntry[], LinearBranchEntry[], BitmapEntry[], or UncompressedEntry[].
		/// </summary>
		internal object _tree;

		#endregion

		protected void Add(uint key, object value)
		{
			if (_count <= 0) {
				if (_count == 0) {
					_count = -4;
					_stuff = key;
					_tree = value;
					return;
				} else
					MakeRootNode();
			}
			LinearEntry[] rootL = _tree as LinearEntry[];
			if (rootL != null)
				rootL.
			if (_count > 0) {
			} else 
		}

		private void MakeRootNode()
		{
 			Debug.Assert(_count <= 0);
			object val = _tree;
			LinearNode[] root = new LinearNode[4];
			_tree = root;
		}

		protected bool ContainsKey(uint key)
		{
			object dummy;
			return TryGetValue(key, out dummy);
		}

		public bool Remove(uint key)
		{
			throw new NotImplementedException();
		}

		public bool TryGetValue(uint key, out object value)
		{
			throw new NotImplementedException();
		}

		public ICollection<object> Values
		{
			get { throw new NotImplementedException(); }
		}

		public object this[uint key]
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		#endregion

		#region ICollection<KeyValuePair<uint,object>> Members

		public void Add(KeyValuePair<uint, object> item)
		{
			throw new NotImplementedException();
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}

		public bool Contains(KeyValuePair<uint, object> item)
		{
			throw new NotImplementedException();
		}

		public void CopyTo(KeyValuePair<uint, object>[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public int Count
		{
			get { return _count; }
		}

		public bool IsReadOnly
		{
			get { throw new NotImplementedException(); }
		}

		public bool Remove(KeyValuePair<uint, object> item)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IEnumerable<KeyValuePair<uint,object>> Members

		public IEnumerator<KeyValuePair<uint, object>> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
