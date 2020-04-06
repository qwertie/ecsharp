using Loyc.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Syntax
{
	/// <summary>A list of non-null references to LNode.</summary>
	/// <remarks>
	/// Users of Loyc trees previously needed to refer directly to <see cref="VList{LNode}"/> so they 
	/// were dependent on which list implementation is used, which is unfortunate. It is nice to have 
	/// the freedom of a <i>struct</i> for the node list so that implementations like VList are possible,
	/// but to refer to VList by name prevents easily changing list representations.
	/// <para/>
	/// In the future we'll probably move away from VList as a representation, mainly so that 
	/// Loyc.Syntax.dll does not need to take a dependency on Loyc.Collections.dll. This struct
	/// will aid the transition.
	/// <para/>
	/// For now, there are implicit conversions between <see cref="VList{LNode}"/> and LNodeList. These
	/// will be removed eventually.
	/// <para/>
	/// A disadvantage of this approach is that C# does not support user-defined conversions to 
	/// interfaces. Therefore, conversion to IReadOnlyList will box the wrapper instead of the 
	/// underlying implementation. Ugh.
	/// <para/>
	/// This change is issue #100: https://github.com/qwertie/ecsharp/issues/100
	/// </remarks>
	public struct LNodeList : IListAndListSource<LNode>, ICloneable<LNodeList>, IEquatable<LNodeList>
	{
		public static readonly LNodeList Empty = new LNodeList();

		private VList<LNode> _list;

		public static implicit operator LNodeList(VList<LNode> list) => new LNodeList(list);
		public static implicit operator VList<LNode>(LNodeList list) => list._list;

		public LNodeList(LNode item_0) => _list = new VList<LNode>(item_0 ?? throw new ArgumentNullException(nameof(item_0)));
		public LNodeList(LNode item_0, LNode item_1) => _list = new VList<LNode>(item_0 ?? throw new ArgumentNullException(nameof(item_0)), item_1 ?? throw new ArgumentNullException(nameof(item_1)));
		public LNodeList(params LNode[] items) : this(new VList<LNode>(items)) { }
		public LNodeList(IEnumerable<LNode> items) : this(new VList<LNode>(items)) { }
		public LNodeList(VList<LNode> list)
		{
			int i = 0;
			foreach (var n in list.ToFVList()) { // FVList enumerates faster
				i++;
				if (n == null) throw new ArgumentNullException("list[" + (list.Count - i) + "]");
			}
			_list = list;
		}
		
		public LNode this[int index]
		{
			get => _list[index];
			set => _list[index] = value;
		}
		public LNode this[int index, LNode defaultValue] => _list[index, defaultValue];

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int Count => _list.Count;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsReadOnly => false;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsEmpty => _list.IsEmpty;

		void ICollection<LNode>.Add(LNode item) => Add(item);
		public LNodeList Add(LNode item) => new LNodeList(_list.Add(item ?? throw new ArgumentNullException(nameof(item))));
		public void Clear() => _list.Clear();
		public bool Contains(LNode item) => _list.Contains(item);
		public void CopyTo(LNode[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
		public IEnumerator<LNode> GetEnumerator() => _list.GetEnumerator();
		public int IndexOf(LNode item) => _list.IndexOf(item);
		void IList<LNode>.Insert(int index, LNode item) => Insert(index, item);
		public LNodeList Insert(int index, LNode item) => new LNodeList(_list.Insert(index, item ?? throw new ArgumentNullException(nameof(item))));
		public bool Remove(LNode item) => _list.Remove(item);
		void IList<LNode>.RemoveAt(int index) => RemoveAt(index);
		public LNodeList RemoveAt(int index) => new LNodeList(_list.RemoveAt(index));
		IRange<LNode> IListSource<LNode>.Slice(int start, int count) => Slice(start, count);
		public Slice_<LNode> Slice(int start, int count = int.MaxValue) => _list.Slice(start, count);
		public LNode TryGet(int index, out bool fail) => _list.TryGet(index, out fail);
		IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
		LNodeList ICloneable<LNodeList>.Clone() => this;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public LNode Last => _list.Last;

		[Obsolete("Please call Initial() instead (this was renamed for consistency; `First` is usually a property that returns the first item)")]
		public LNodeList First(int count) => new LNodeList(_list.First(count));
		public LNodeList Initial(int count) => new LNodeList(_list.First(count));
		public LNodeList Final(int count) => count > _list.Count ? this : new LNodeList(_list.Slice(_list.Count - count));

		public LNodeList WithoutLast(int count) => new LNodeList(_list.WithoutLast(count));

		/// <summary>Maps a list to another list of the same length.</summary>
		/// <param name="map">A function that transforms each item in the list.</param>
		/// <returns>The list after the map function is applied to each item. The 
		/// original list is not modified.</returns>
		/// <remarks>
		/// This method is called "Smart" because of what happens if the map
		/// doesn't do anything. If the map function returns the first N items
		/// unmodified, those N items are typically not copied, but shared between
		/// the existing list and the new one. This is useful for functional code
		/// that often processes a list without modifying it at all.
		/// </remarks>
		public LNodeList SmartSelect(Func<LNode, LNode> map) => new LNodeList(_list.SmartSelect(map));

		/// <summary>Maps a list to another list by concatenating the outputs of a mapping function.</summary>
		/// <param name="map">A function that transforms each item in the list to a list of items.</param>
		/// <returns>A list that contains all the items returned from `map`.</returns>
		/// <remarks>
		/// This method is called "Smart" because of what happens if the map returns
		/// the same list again. If, for the first N items, the `map` returns a 
		/// list of length 1, and that one item is the same item that was passed 
		/// in, then those N items are typically not copied, but shared between
		/// the existing list and the new one. This is useful for functional code
		/// that often processes a list without modifying it at all.
		/// </remarks>
		public LNodeList SmartSelectMany(Func<LNode, IList<LNode>> map) => new LNodeList(_list.SmartSelectMany(map));

		/// <summary>Filters the list, returning the same list if the filter function returns true for every item.</summary>
		public LNodeList SmartWhere(Func<LNode, bool> filter) => new LNodeList(_list.SmartWhere(filter));

		/// <summary>Filters and maps a list with a user-defined function.</summary>
		/// <param name="filter">A function that chooses which items to include
		/// in a new list, and what to change them to.</param>
		/// <returns>The list after filtering has been applied. The original list
		/// structure is not modified.</returns>
		/// <remarks>
		/// This is a smart function. If the filter does not modify the first N 
		/// items it is passed (which are the last items in a FVList), those N items 
		/// are typically not copied, but shared between the existing list and the 
		/// new one.
		/// </remarks>
		public LNodeList WhereSelect(Func<LNode, Maybe<LNode>> filter) => new LNodeList(_list.WhereSelect(filter));

		public LNodeList AddRange(VList<LNode> list) => new LNodeList(_list.AddRange(list));
		public LNodeList AddRange(LNodeList list) => new LNodeList(_list.AddRange(list._list));
		public LNodeList AddRange(IList<LNode> list) => new LNodeList(_list.AddRange(list));
		public LNodeList AddRange(IEnumerable<LNode> list) => new LNodeList(_list.AddRange(list));
		public LNodeList InsertRange(int index, IList<LNode> list) => new LNodeList(_list.InsertRange(index, list));
		public LNodeList RemoveRange(int index, int count) => new LNodeList(_list.RemoveRange(index, count));
		public LNode Pop() => _list.Pop();

		/// <summary>Returns whether the two list references are the same.
		/// Does not compare the contents of the lists.</summary>
		public static bool operator ==(LNodeList lhs, LNodeList rhs) => lhs._list == rhs._list;
		/// <summary>Returns whether the two list references are different.
		/// Does not compare the contents of the lists.</summary>
		public static bool operator !=(LNodeList lhs, LNodeList rhs) => lhs._list != rhs._list;

		public bool Equals(LNodeList other) => _list == other._list;
		public override bool Equals(object rhs) => rhs is LNodeList && Equals((LNodeList)rhs);
		public override int GetHashCode() => _list.GetHashCode();
		public override string ToString() => string.Join(", ", _list);
	}
}
