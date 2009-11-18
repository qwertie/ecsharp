using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Loyc.Utilities.Judish.Internal;

namespace Loyc.Utilities.Judish
{
	/// <summary>
	/// Maps uint to object, or a key of arbitrary length to object. Currently, this
	/// is the only type of Judish collection implementation, but it has a variety 
	/// of possible uses.
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
	/// * It is sorted, and can be enumerated in order.
	/// * It uses less memory, especially if you don't usually need to associate
	///   values with your keys (in other words, Judish is better if you want to
	///   store a set rather than a map, or if your machine has limited memory, or
	///   if you want to hold very large data sets)
	/// * It can find keys that begin with any given prefix
	/// <para/>
	/// The main disadvantage is that only certain types of keys and values are
	/// supported. It is not a hash table, and hash codes are unsuitable as keys
	/// because there is no built-in way to deal with hash collisions. Acceptable
	/// keys in a Judish map are integers, strings, and byte arrays; other keys can 
	/// be supported by writing a class derived from JKeySerializer. Acceptable
	/// values are boolean (true or false) or object references; integer values and
	/// other value types cannot be used as values.
	/// <para/>
	/// Expanses of the key space can be "organized" in one of three ways (linear, 
	/// bitmap, or uncompressed); each organization is optimized for a different 
	/// key density. Each organization can be stored in three different ways: 
	/// "compact", "normal" or "bit array" (except that there is no "bitmap bit 
	/// array"), so in total there are actually 8 different kinds of nodes, and 
	/// some node types have multiple modes of operation.
	/// <para/>
	/// Key density refers to the number of keys in use divided by the key space; 
	/// for example, if a key is one byte then the key space is 256, and if there 
	/// are 31 unique keys then the key density is about 12%. Low-density key 
	/// spaces use linear nodes; medium-density spaces use bitmap nodes; and high-
	/// density spaces use uncompressed nodes.
	/// <para/>
	/// At each level of the Judish tree, a different node type may be chosen to 
	/// suit the local conditions. "Normal" nodes support keys up to 4 bytes, 
	/// while "compact" nodes only support 0 or 1-byte keys but they require much
	/// less memory. Both normal and compact nodes can contain both "leaves" 
	/// (keys that map directly to a value) and "children" (other nodes).
	/// <para/>
	/// "Bit array" nodes are special leaf nodes that are unable to have values or 
	/// children; bit arrays can be used only when all values associated with all 
	/// keys in a key space are null, and even then, only when the node has no 
	/// children. In some cases where children or values are present but there 
	/// are many nulls, the "bitmap" node type can be a hybrid data structure, as 
	/// it can include an "uncompressed bit array" just for the nulls.
	/// <para/>
	/// Judish is inspired by the C library called Judy, but it is substantially
	/// different for a number of reasons. Most fundamentally, it is not feasible to
	/// do a direct port because .NET does not support many of the tricks Judy
	/// relies upon. Far from being not a port of Judy, Judish is a much smaller
	/// clean-room rewrite that is merely based on similar principles as Judy. 
	/// Hence its name "Judish"--in the same way "Largish" means "sort-of large".
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
	/// integer keys). As it is, Judish does not allow you to associate an integer 
	/// value with a key unless you box it.
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
	/// object: an array whose first one or more elements have special meaning.
	/// This approach, unfortunately, is very anti-OO, because now we can't have
	/// a single "Node" base class with abstract "Add" and "Remove" methods that
	/// automatically do the right thing depending on the node type. Instead, we
	/// have to check: what kind of array is this? And then invoke a static action 
	/// method based on the node type.
	/// <para/>
	/// These fundamental .NET limitations force Judish to have a substantially
	/// different design than Judy. Besides that, I decided to support keys of
	/// nearly unlimited length (so that you can use strings and other large 
	/// objects as keys), and to combine Judy1 and JudyL in this single data 
	/// structure.
	/// <para/>
	/// Performace may also be harmed by the fact that...
	/// *   .NET data structures cannot guarantee a particular alignment, and one
	///     cannot write a custom memory manager. I assume Judy's memory manager can
	///     align many of its data structures on 16-byte, 32-byte or 64-byte
	///     boundaries in order to avoid straddling cache lines. As far as I know,
	///     .NET objects only get 4- or 8-byte alignment.
	/// *   We need a typecast whenever a reference can point to either a value or a
	///     subtree. In my implementation, casts are needed frequently.
	/// *   All array accesses are subject to an index-out-of-bounds check
	/// *   .NET arrays are covariant, so for any reference type Foo, Foo[] can be 
	///     cast to object[]. This is bad for performance because, at least 
	///     theoretically (as I don't know how to see the JIT's output), every time 
	///     we change an entry in an object array, the CLR has to check if the array
	///     is REALLY an object array and not some other kind of array.
	/// <para/>
	/// I don't actually know how much slower Judish is compared to Judy. Anyone
	/// care to write a benchmark?
	/// <para/>
	/// The current implementation is designed for 32-bit architectures. To improve
	/// efficiency in a 64-bit process, substantial changes might be needed, and I
	/// just haven't written such an implementation. This class can be used in
	/// 64-bit, but it probably uses almost twice as much memory so that the 64-bit
	/// pointers can be 8-byte aligned, and is therefore slower (ideally it would 
	/// use 1.5x as much memory, since only the object pointers should double in 
	/// size, not the integer keys).
	/// <para/>
	/// JudishInternal can often compress out null values, if the map holds a lot of
	/// items. Therefore, if you want to store a "set" rather than a "dictionary",
	/// just set the value associated with each key to null, and you'll save space
	/// automatically. This makes JudishInternal behave more like a Judy1 array
	/// than a JudyL array.
	/// <para/>
	/// COOL IDEA: Integer values aren't supported eh???? But it would be possible 
	/// to make a hashtable-like structure that maps objects to integers or other
	/// value types that support 'blitting'. Use the object's hashcode as its key,
	/// but use a long key--extend the key with the 'value' you want to associate 
	/// with the object. Then use a lookup that only cares about the first four 
	/// bytes of the key. Then again... this may be useless as the overhead of a 
	/// long key may decrease efficiency so that an ordinary Dictionary is more 
	/// efficient. Oops. But this could work for mapping object->byte if we 
	/// compress the hashcode to 3 bytes and store the 'value' in the final byte.
	/// This also limits efficiency though, as 'compact' nodes are impossible.
	/// In case of hash collision, we also need a collision chain object. But 
	/// collisions will be very rare if the hashcode is good.
	/// </remarks>
	public abstract class JudishInternal
	{
		#region Data

		/// <summary>Total population, or, if there is only one key, this may hold
		/// a value between -1 and -5 to indicate the key length (0 to 4 bytes).</summary>
		internal int _count;
		/// <summary>If _count is negative, this holds the map's only key. If the 
		/// key is less than 4 bytes then the low bytes are zero. Otherwise, its 
		/// use yet to be chosen.
		/// </summary>
		internal uint _stuff;
		/// <summary>This is a pointer to the data of the map. If _count is
		/// negative, this points to map's only value; otherwise, it points to
		/// RootLeafEntry[], LinearBranchEntry[], BitmapEntry[], or UncompressedEntry[].
		/// </summary>
		internal object _tree;

		#endregion

		protected void Add(ref KeyShifter key, object value)
		{
			if (_count <= 0) {
				if (_count == 0 && key.BytesLeft < 4) {
					_count = -1 - key.BytesLeft;
					_stuff = key.KeyPart;
					_tree = value;
					return;
				} else
					MakeRootNode();
			}
			QueryState q = new QueryState(this, key, (NodeBase)_tree);
		}

		private void MakeRootNode()
		{
 			Debug.Assert(_count <= 0);
			
			
			
			object val = _tree;

			//LinearNode[] root = new LinearNode[4];
			//_tree = root;
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
	}
}
