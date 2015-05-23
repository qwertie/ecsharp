namespace Loyc.Collections
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Diagnostics;
	using Loyc.Collections.Impl;
	using System.Runtime.CompilerServices;

	/// <summary>
	/// A compact auto-enlarging list that efficiently supports supports insertions 
	/// at the beginning or end of the list.
	/// </summary><remarks>
	/// An <a href="http://core.loyc.net/collections/dlist.html">article</a>
	/// about this class is available.
	/// </remarks>
	/// <seealso cref="InternalDList{T}"/>
	/// <seealso cref="DList"/>
	[Serializable()]
	#if !CompactFramework
	[DebuggerTypeProxy(typeof(ListSourceDebugView<>)), DebuggerDisplay("Count = {Count}")]
	#endif
	public class DList<T> : IListEx<T>, IDeque<T>, IListRangeMethods<T>, ICloneable<DList<T>> //, IGetIteratorSlice<T>
	{
		protected InternalDList<T> _dlist = InternalDList<T>.Empty;

		internal DList(InternalDList<T> internalList) { _dlist = internalList; }
		public DList(int capacity)     { Capacity = capacity; }
		public DList(IReadOnlyCollection<T> items) { PushLast(items); }
		public DList(ICollection<T> items) { PushLast(items); }
		public DList(ICollectionAndReadOnly<T> items) { PushLast(items); }
		public DList(IEnumerable<T> items) { PushLast(items); }
		public DList(T[] items) : this((ICollection<T>)items) { }
		public DList() { }

		private void CheckPopCount(int amount)
		{
			if (amount < 0)
	 			throw new InvalidOperationException(string.Format("Can't pop a negative number of elements ({0})", amount));
			if (amount > _dlist.Count)
	 			throw new InvalidOperationException(string.Format("Can't pop more elements than Deque<{0}> contains ({1}>{2})", typeof(T).Name, amount, Count));
		}

		public int IndexOf(T item)
		{
			return _dlist.IndexOf(item);
		}

		public void PushLast(ICollection<T> items)
		{
			_dlist.PushLast(items);
		}
		public void PushLast(IEnumerable<T> items)
		{
			_dlist.PushLast(items);
		}
		public void PushLast(IReadOnlyCollection<T> items)
		{
			_dlist.PushLast(items);
		}
		public void PushLast(ICollectionAndReadOnly<T> items)
		{
			_dlist.PushLast(items);
		}

		public void PushLast(T item)
		{
			_dlist.PushLast(item);
		}
		
		public void PushFirst(T item)
		{
			_dlist.PushFirst(item);
		}

		public void PopLast(int amount)
		{
			CheckPopCount(amount);
			_dlist.PopLast(amount);
		}

		public void PopFirst(int amount)
		{
			CheckPopCount(amount);
			_dlist.PopFirst(amount);
		}

		public int Capacity
		{
			get { return _dlist.Capacity; }
			set {
				if (value < _dlist.Count)
					throw new ArgumentOutOfRangeException(string.Format("Capacity is too small ({0}<{1})", value, Count));
				_dlist.Capacity = value;
			}
		}

		public void Insert(int index, T item)
		{
			CheckInsertIndex(index);
			_dlist.Insert(index, item);
		}

		public void InsertRange(int index, ICollection<T> items)
		{
			CheckInsertIndex(index);
			_dlist.InsertRange(index, items);
		}
		public void InsertRange(int index, IReadOnlyCollection<T> items)
		{
			CheckInsertIndex(index);
			_dlist.InsertRange(index, items);
		}
		public void InsertRange(int index, ICollectionAndReadOnly<T> items)
		{
			InsertRange(index, (IReadOnlyCollection<T>)items);
		}
		public void InsertRange(int index, IEnumerable<T> e)
		{
			var s = e as IReadOnlyCollection<T>;
			if (s != null)
				InsertRange(index, s);
			var c = e as ICollection<T>;
			if (c != null)
				InsertRange(index, c);
			else
				InsertRange(index, (ICollection<T>)new List<T>(e));
		}

		public void AddRange(ICollectionAndReadOnly<T> c)
		{
			InsertRange(_dlist.Count, (IReadOnlyCollection<T>)c);
		}
		public void AddRange(ICollection<T> c)
		{
			InsertRange(_dlist.Count, c);
		}
		public void AddRange(IReadOnlyCollection<T> s)
		{
			InsertRange(_dlist.Count, s);
		}
		public void AddRange(IEnumerable<T> e)
		{
			foreach (T item in e)
				Add(item);
		}
		
		void CheckInsertIndex(int index)
		{
			if ((uint)index > (uint)_dlist.Count)
				throw new IndexOutOfRangeException(string.Format("Invalid index in Deque<{0}> ({1}∉[0,{2}])", typeof(T).Name, index, Count));
		}

		public void RemoveAt(int index)
		{
			CheckRemoveIndex(index, 1);
			_dlist.RemoveAt(index);
		}
		public void RemoveRange(int index, int amount)
		{
			if (amount < 0)
				throw new ArgumentOutOfRangeException("amount");
			CheckRemoveIndex(index, amount);
			_dlist.RemoveRange(index, amount);
		}
		void CheckRemoveIndex(int index, int amount)
		{
			if ((uint)index > (uint)_dlist.Count || (uint)(index + amount) > (uint)_dlist.Count)
				throw new IndexOutOfRangeException(string.Format("Invalid removal range in Deque<{0}> ([{1},{2})⊈[0,{3}))", typeof(T).Name, index, index + amount, Count));
		}
		public int RemoveAll(Predicate<T> condition)
		{
			return ListExt.RemoveAll(this, condition);
		}

		public T this[int index]
		{
			#if DotNet45
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			#endif
			[DebuggerStepThrough]
			get {
				CheckIndex(index);
				return _dlist[index];
			}
			#if DotNet45
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			#endif
			[DebuggerStepThrough]
			set {
				CheckIndex(index);
				_dlist[index] = value;
			}
		}
		public T this[int index, T defaultValue]
		{
			#if DotNet45
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			#endif
			[DebuggerStepThrough]
			get { return _dlist[index, defaultValue]; }
		}
		#if DotNet45
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		#endif
		private void CheckIndex(int index)
		{
			if ((uint)index >= (uint)_dlist.Count) 
				ThrowOutOfRange(index);
		}
		private void ThrowOutOfRange(int index)
		{
			throw new ArgumentOutOfRangeException(string.Format("Invalid index in Deque<{0}> ({1}∉[0,{2}))", typeof(T).Name, index, Count));
		}

		public bool TrySet(int index, T value)
		{
			return _dlist.TrySet(index, value);
		}
		public T TryGet(int index, out bool fail)
		{
			return _dlist.TryGet(index, out fail);
		}

		/// <summary>An alias for PushLast().</summary>
		public void Add(T item)
		{
			_dlist.PushLast(item);
		}

		public void Clear()
		{
			_dlist.Clear();
		}

		public bool Contains(T item)
		{
			return _dlist.IndexOf(item) > -1;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			if (array == null || array.Length < _dlist.Count)
				throw new ArgumentOutOfRangeException("array");
			if (arrayIndex < 0 || array.Length - arrayIndex < _dlist.Count)
				throw new ArgumentOutOfRangeException("arrayIndex");
			_dlist.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			[DebuggerStepThrough]
			get { return _dlist.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T item)
		{
			return _dlist.Remove(item);
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return _dlist.GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return _dlist.GetEnumerator();
		}
		public IEnumerator<T> GetEnumerator()
		{
			return _dlist.GetEnumerator();
		}

		#region IDeque<T>

		public T TryPopFirst(out bool isEmpty)
		{
			return _dlist.TryPopFirst(out isEmpty);
		}
		public T TryPeekFirst(out bool isEmpty)
		{
			return _dlist.TryPeekFirst(out isEmpty);
		}
		public T TryPopLast(out bool isEmpty)
		{
			return _dlist.TryPopLast(out isEmpty);
		}
		public T TryPeekLast(out bool isEmpty)
		{
			return _dlist.TryPeekLast(out isEmpty);
		}

		public T First
		{
			#if DotNet45
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			#endif
			get { return _dlist.First; }
			#if DotNet45
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			#endif
			set { _dlist.First = value; }
		}
		public T Last
		{
			#if DotNet45
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			#endif
			get { return _dlist.Last; }
			#if DotNet45
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			#endif
			set { _dlist.Last = value; }
		}
		public bool IsEmpty
		{
			get { return _dlist.IsEmpty; }
		}

		#endregion

		public int BinarySearch(T k, Comparer<T> comp)
		{
			return _dlist.BinarySearch(k, comp);
		}
		public int BinarySearch<K>(K k, Func<T, K, int> comp)
		{
			return _dlist.BinarySearch(k, comp);
		}
		
		public void Resize(int newSize)
		{
			if (newSize < Count)
				RemoveRange(newSize, Count - newSize);
			else if (newSize > Count)
				InsertRange(Count, (IReadOnlyCollection<T>)Range.Repeat(default(T), newSize - Count));
		}

		public DList<T> Clone()
		{
			return new DList<T>(_dlist.Clone());
		}

		public void CopyTo(int sourceIndex, T[] destination, int destinationIndex, int subcount)
		{
			if ((uint)sourceIndex > (uint)_dlist.Count)
				throw new ArgumentOutOfRangeException("sourceIndex");
			if ((uint)destinationIndex > (uint)destination.Length)
				throw new ArgumentOutOfRangeException("sourceIndex");
			if ((uint)subcount > (uint)Math.Max(_dlist.Count - sourceIndex, destination.Length - destinationIndex))
				throw new ArgumentOutOfRangeException("subcount");
			
			_dlist.CopyTo(sourceIndex, destination, destinationIndex, subcount);
		}

		public DList<T> CopySection(int start, int subcount)
		{
			if ((uint)start > (uint)_dlist.Count)
				throw new ArgumentOutOfRangeException("start");
			if (subcount < 0)
				throw new ArgumentOutOfRangeException("subcount");
			
			return new DList<T>(_dlist.CopySection(start, subcount));
		}

		public void Sort(Comparison<T> comp)
		{
			_dlist.Sort(comp);
		}
		public void Sort(int index, int count, Comparison<T> comp)
		{
			_dlist.Sort(index, count, comp);
		}

		IRange<T> IListSource<T>.Slice(int start, int count)
		{
			return new Slice_<T>(this, start, count);
		}
		public ListSlice<T> Slice(int start, int count)
		{
			return new ListSlice<T>(this, start, count);
		}
	}

	/// <summary>
	/// This class is the same as <c>DList{object}</c> except that it 
	/// also implements the IList interface.
	/// </summary>
	[Serializable()]
	public class DList : DList<object>, System.Collections.IList
	{
		public bool IsFixedSize
		{
			get { return false; }
		}
		public void CopyTo(Array array, int arrayIndex)
		{
			if (array == null || array.Length < Count)
				throw new ArgumentOutOfRangeException("array");
			if (arrayIndex < 0 || array.Length - arrayIndex < Count)
				throw new ArgumentOutOfRangeException("arrayIndex");
			
			foreach(object obj in this)
				array.SetValue(obj, arrayIndex++);
		}
		public bool IsSynchronized
		{
			get { return false; }
		}
		public object SyncRoot
		{
			get { return this; }
		}
		public new void Remove(object obj)
		{
			base.Remove(obj);
		}
		public new int Add(object obj)
		{
			base.Add(obj);
			return Count - 1;
		}
	}
}
