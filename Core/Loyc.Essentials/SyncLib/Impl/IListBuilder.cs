using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Collections.MutableListExtensionMethods;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.SyncLib.Impl
{
	/// <summary>An interface implemented by adapters that help read and write lists 
	/// of various types, e.g. <see cref="ListBuilder{T}"/>. The adapter can be
	/// used to read a collection once or write it once, but not both.</summary>
	public interface IListBuilder<TList, T> : IAdd<T> //, ICount, IReadSpan<T>
	{
		/// <summary>A method that is always called once when loading a list.</summary>
		/// <param name="minLength">Minimum list size (<see cref="ISyncManager.MinimumListLength"/></param>
		/// <returns>Returns the list object so that it can be assigned as the 
		/// <see cref="ISyncManager.CurrentObject"/> in case the list contains
		/// a cyclic reference to itself. If a cyclic reference is not possible,
		/// this method can return null.
		/// 
		/// TODO: WHOA we totally forgot about our plan above to handle circular references
		/// </returns>
		void Alloc(int minLength);
		/// <summary>When the list being loaded was already read from the data 
		/// stream earlier, this property is called to convert that existing object 
		/// to the target list type, skipping the usual calls to Alloc(), Add(T)
		/// and List.</summary>
		TList CastList(object value);
		/// <summary>Called once when done loading to retrieve the list.</summary>
		TList? List { get; }
	}

	public struct ListBuilder<T> : IListBuilder<List<T>, T>
	{
		public List<T>? List { get; set; }

		public void Alloc(int minLength) => List = minLength <= 1 ? new List<T>() : new List<T>(minLength);

		public List<T> CastList(object value) => List = value as List<T> ?? new List<T>((IEnumerable<T>)value);

		public void Add(T item) => List!.Add(item);

		//public ReadOnlySpan<T> ReadSpan()
		//{
		//	T[] result = new T[_index + 256 < List.Count ? 256 : List.Count - _index];
		//	for (int i = 0; i < result.Length; i++)
		//		result[i] = List[_index++];
		//	return result.AsSpan();
		//}
	}

	public struct ArrayBuilder<T> : IListBuilder<T[], T>
	{
		InternalList<T> _list;
		public T[]? List => _list.AsArray();

		public void Alloc(int minLength) => _list = new InternalList<T>(minLength <= 1 ? 4 : minLength);

		public T[] CastList(object value)
		{
			if (value is T[] array)
				_list = new InternalList<T>(array, array.Length);
			else {
				_list = InternalList<T>.Empty;
				_list.AddRange((IEnumerable<T>)value);
			}
			return List!;
		}

		public void Add(T item) => _list.Add(item);

		//public ReadOnlySpan<T> ReadSpan()
		//{
		//	T[] result = new T[_index + 256 < List.Count ? 256 : List.Count - _index];
		//	for (int i = 0; i < result.Length; i++)
		//		result[i] = List[_index++];
		//	return result.AsSpan();
		//}
	}

	public struct MemoryBuilder<T> : IListBuilder<Memory<T>, T>, IListBuilder<ReadOnlyMemory<T>, T>
	{
		InternalList<T> _list;
		public Memory<T> List => _list.AsMemory();
		ReadOnlyMemory<T> IListBuilder<ReadOnlyMemory<T>, T>.List => _list.AsMemory();

		public void Alloc(int minLength) => _list = new InternalList<T>(minLength <= 1 ? 4 : minLength);

		public Memory<T> CastList(object value)
		{
			if (value is Memory<T> mem)
				return mem;
			else {
				var list = InternalList<T>.Empty;
				list.AddRange((IEnumerable<T>)value);
				return list.AsMemory();
			}
		}
		ReadOnlyMemory<T> IListBuilder<ReadOnlyMemory<T>, T>.CastList(object value)
		{
			if (value is Memory<T> mem)
				return mem;
			else if (value is ReadOnlyMemory<T> romem)
				return romem;
			else {
				var list = InternalList<T>.Empty;
				list.AddRange((IEnumerable<T>)value);
				return list.AsMemory();
			}
		}

		public void Add(T item) => _list.Add(item);
	}

	/// <summary>Helper type for reading <see cref="ICollection{T}"/> types.</summary>
	public struct CollectionBuilder<TList, T> : IListBuilder<TList, T> where TList : ICollection<T>
	{
		public CollectionBuilder(Func<int, TList> alloc) { List = default; _alloc = alloc; }

		public TList? List { get; set; }
		Func<int, TList> _alloc;

		public void Alloc(int minLength) => List = _alloc(minLength);

		public TList CastList(object value)
		{
			if (value is TList list)
				return List = list;
			else {
				Alloc(0);
				List!.AddRange((IEnumerable<T>)value);
				return List!;
			}
		}

		public void Add(T item) => List!.Add(item);

		//public ReadOnlySpan<T> ReadSpan()
		//{
		//	T[] result = new T[_index + 256 < List.Count ? 256 : List.Count - _index];
		//	for (int i = 0; i < result.Length; i++)
		//		result[i] = List[_index++];
		//	return result.AsSpan();
		//}
	}
}
