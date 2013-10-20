using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Utilities.Judish.Internal
{
	internal struct StatHeader
	{
		/// <summary>Gets or sets the number of keys.</summary>
		/// <remarks>
		/// Nodes other than MiniLinearNode can hold between 2 and 257 keys--just 
		/// the range of a byte. An uncompressed or bitmap node may have to hold 
		/// 257 keys: 256 one-byte keys plus the zero-length key (ZLK). 
		/// LongLinearNode also shares this limit, so that it can use this 
		/// StatHeader structure. Only MiniLinearNode is capable of holding one 
		/// key, so when other node types get down to one key they always switch 
		/// to MiniLinearNode.
		/// </remarks>
		public int NumKeys
		{
			get { return (int)NumKeysMinus2 + 2; }
			set { 
				Debug.Assert(value >= 2 && value <= 257);
				NumKeysMinus2 = (byte)(value - 2);
			}
		}
		public byte DecisionCounter;
		public byte NumKeysMinus2;
		byte NumSubsumable1sEtc;
		byte NumSubsumable23Etc;
		
		public int NumSubsumable1s
		{
			get { return NumSubsumable1sEtc & 0x7F; }
			set {
				Debug.Assert(value < 0x80);
				NumSubsumable1sEtc = (byte)((NumSubsumable1sEtc & 0x80) | value);
			}
		}
		public int NumSubsumable23
		{
			get { return NumSubsumable23Etc & 0x7F; }
			set { 
				Debug.Assert(value < 0x80);
				NumSubsumable23Etc = (byte)((NumSubsumable23Etc & 0x80) | value);
			}
		}
		public void MultiByteKeyEncountered()
		{
			NumSubsumable23Etc |= 0x80;
		}
		public Type AutoChooseNodeType(NodeBase node)
		{
			if (DecisionCounter++ == (NumKeys >> 1) + 2)
			{
				return ChooseNodeType(node);
			}
		}

		public Type ChooseNodeType(NodeBase node)
		{
			throw new NotImplementedException();
		}
	}

	/// <summary>Statistics used to choose the kind of node that should be used at 
	/// a given level of the tree.</summary>
	internal struct DecisionStats
	{
		public short NumKeys;
		/// <summary>Specifies the number of keys of length 2 to 4 for which the 
		/// first byte is unique. Such keys are noteworthy because they can be
		/// stored directly in "normal" nodes, but they require a separate object 
		/// allocation to be stored in "compact" nodes.</summary>
		/// <remarks>If a node can't hold these keys directly in itself, it 
		/// must still keep track of them in this statistic, i.e. it must keep a 
		/// count of the number of child nodes that have only one entry with a 
		/// key of length 1 to 3.</remarks>
		public byte NumSubsumable1s;
		/// <summary>Specifies the number of key first bytes that match exactly
		/// two or three keys that are between 1 and 4 bytes long. Such groups 
		/// of keys are noteworthy because they can be stored directly in normal 
		/// linear nodes but require a separate object to be allocated if they 
		/// are to be held in bitmap or uncompressed nodes.</summary>
		/// <remarks>
		/// For example, suppose a Judish collection is to hold the following 
		/// 3-byte keys: 0x340001, 0x340002, 0x810001, 0x810002, and 0x810003, 
		/// and 0x100000. A normal linear node can hold all these keys at once, 
		/// but a bitmap (or uncompressed) node can only hold one entry for 0x34
		/// (or 0x3400), another for 0x81 (or 0x8100), and a third for 0x10 (or 
		/// 0x100000), so it must allocate two additional linear nodes to hold 
		/// the remainder of each key. NumSubsumableMs is 2 in this case becuse 
		/// there are two 1-byte prefixes with child nodes that contain 2 or 3 
		/// keys.
		/// <para/>
		/// A node must track this statistic whether it holds the keys in question 
		/// directly (normal linear nodes) or uses child nodes (all other node 
		/// types).
		/// </remarks>
		public byte NumSubsumableMs;
		/// <summary>Indicates whether any of the keys in the node are longer 
		/// than one byte. If so, the node cannot be converted to an uncompressed 
		/// bit array.</summary>
		public bool HasMultiByteKeys;
		/// <summary>Indicates whether any keys in the node are associated with 
		/// values or child nodes. If so, the node can't be converted to a bit 
		/// array (whether linear or uncompressed).</summary>
		public bool HasValuesOrChildren;
	}

	internal struct QueryState
	{
		public QueryState(JudishInternal map, KeyShifter key)
		{
			Map = map;
			Key = key;
			Parent = null;
		}

		public readonly JudishInternal Map;
		public readonly KeyShifter Key;
		public NodeBase Parent;
	}
}
