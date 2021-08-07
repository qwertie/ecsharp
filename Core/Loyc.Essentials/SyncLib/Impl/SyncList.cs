using Loyc.Collections;
using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib.Impl
{
	/// <summary>Contains implementations of ISyncField for several types of lists.
	/// The constructor requires a synchronizer that implements <see cref="ISyncField{SM, T}"/>.</summary>
	/// <typeparam name="SM">An ISyncManager implementation (or ISyncManager itself)</typeparam>
	/// <typeparam name="T">Type of each list item</typeparam>
	/// <typeparam name="SyncItem">Synchronizer for an individual list item</typeparam>
	public struct SyncList<SM, T, SyncItem> :
		ISyncField<SM, T[]>,
		ISyncField<SM, List<T>>,
		ISyncField<SM, IList<T>>,
		ISyncField<SM, IReadOnlyList<T>>,
		ISyncField<SM, IReadOnlyCollection<T>>,
		ISyncField<SM, IListSource<T>>,
		ISyncField<SM, ICollection<T>>,
		ISyncField<SM, HashSet<T>>,
		ISyncField<SM, Memory<T>>,
		ISyncField<SM, ReadOnlyMemory<T>>
		where SM : ISyncManager
		where SyncItem : ISyncField<SM, T>
	{
		SyncItem _syncItem;
		SubObjectMode _listMode;
		int _tupleLength;
		
		public SyncList(SyncItem syncItem, SubObjectMode listMode, int tupleLength)
		{
			this._syncItem = syncItem;
			this._listMode = listMode;
			this._tupleLength = tupleLength;
		}

		public List<T>? Sync(ref SM sync, FieldId name, List<T>? savable)
		{
			if (!sync.IsSaving) {
				var loader = new ListLoader<SM, List<T>, T, ListBuilder<T>, SyncItem>
					(_syncItem, new ListBuilder<T>(), _listMode, _tupleLength);
				return loader.Sync(ref sync, name, savable);
			} else {
				var saver = new ListSaver<SM, List<T>, T, SyncItem>(_syncItem, _listMode);
				return saver.Sync(ref sync, name, savable);
			}
		}

		public IList<T>? Sync(ref SM sync, FieldId name, IList<T>? savable)
		{
			if (!sync.IsSaving) {
				var loader = new ListLoader<SM, List<T>, T, CollectionBuilder<List<T>, T>, SyncItem>
					(_syncItem, new CollectionBuilder<List<T>, T>(Alloc<T>.List), _listMode, _tupleLength);
				return loader.Sync(ref sync, name, null);
			} else {
				var saver = new ListSaverC<SM, IList<T>, T, SyncItem>(_syncItem, _listMode);
				return saver.Sync(ref sync, name, savable);
			}
		}

		public Memory<T> Sync(ref SM sync, FieldId name, Memory<T> savable)
		{
			if (!sync.IsSaving) {
				var loader = new ListLoader<SM, Memory<T>, T, MemoryBuilder<T>, SyncItem>
					(_syncItem, new MemoryBuilder<T>(), _listMode, _tupleLength);
				return loader.Sync(ref sync, name, savable);
			} else {
				var saver = new ListSaver<SM, ArraySlice<T>, T, SyncItem>(_syncItem, _listMode);
				return saver.Sync(ref sync, name, savable);
			}
		}

		public ReadOnlyMemory<T> Sync(ref SM sync, FieldId name, ReadOnlyMemory<T> savable)
		{
			if (!sync.IsSaving) {
				var loader = new ListLoader<SM, ReadOnlyMemory<T>, T, MemoryBuilder<T>, SyncItem>
					(_syncItem, new MemoryBuilder<T>(), _listMode, _tupleLength);
				return loader.Sync(ref sync, name, savable);
			} else {
				var saver = new ListSaver<SM, ReadOnlyArraySlice<T>, T, SyncItem>(_syncItem, _listMode);
				return saver.Sync(ref sync, name, savable);
			}
		}

		public T[]? Sync(ref SM sync, FieldId name, T[]? savable)
		{
			if (!sync.IsSaving) {
				var loader = new ListLoader<SM, T[], T, ArrayBuilder<T>, SyncItem>
					(_syncItem, new ArrayBuilder<T>(), _listMode, _tupleLength);
				return loader.Sync(ref sync, name, savable);
			} else {
				var saver = new ListSaver<SM, T[], T, SyncItem>(_syncItem, _listMode);
				return saver.Sync(ref sync, name, savable);
			}
		}

		public IReadOnlyList<T>? Sync(ref SM sync, FieldId name, IReadOnlyList<T>? savable)
		{
			if (!sync.IsSaving) {
				var loader = new ListLoader<SM, List<T>, T, CollectionBuilder<List<T>, T>, SyncItem>
					(_syncItem, new CollectionBuilder<List<T>, T>(Alloc<T>.List), _listMode, _tupleLength);
				return loader.Sync(ref sync, name, null);
			} else {
				var saver = new ListSaver<SM, IReadOnlyList<T>, T, SyncItem>(_syncItem, _listMode);
				return saver.Sync(ref sync, name, savable);
			}
		}

		public IReadOnlyCollection<T>? Sync(ref SM sync, FieldId name, IReadOnlyCollection<T>? savable)
		{
			if (!sync.IsSaving) {
				var loader = new ListLoader<SM, List<T>, T, CollectionBuilder<List<T>, T>, SyncItem>
					(_syncItem, new CollectionBuilder<List<T>, T>(Alloc<T>.List), _listMode, _tupleLength);
				return loader.Sync(ref sync, name, null);
			} else {
				var saver = new ListSaver<SM, IReadOnlyCollection<T>, T, SyncItem>(_syncItem, _listMode);
				return saver.Sync(ref sync, name, savable);
			}
		}

		public IListSource<T>? Sync(ref SM sync, FieldId name, IListSource<T>? savable)
		{
			if (!sync.IsSaving) {
				var loader = new ListLoader<SM, DList<T>, T, CollectionBuilder<DList<T>, T>, SyncItem>
					(_syncItem, new CollectionBuilder<DList<T>, T>(Alloc<T>.DList), _listMode, _tupleLength);
				return loader.Sync(ref sync, name, null);
			} else {
				var saver = new ListSaver<SM, IListSource<T>, T, SyncItem>(_syncItem, _listMode);
				return saver.Sync(ref sync, name, savable);
			}
		}

		public ICollection<T>? Sync(ref SM sync, FieldId name, ICollection<T>? savable)
		{
			if (!sync.IsSaving) {
				var loader = new ListLoader<SM, ICollection<T>, T, CollectionBuilder<ICollection<T>, T>, SyncItem>
					(_syncItem, new CollectionBuilder<ICollection<T>, T>(Alloc<T>.List), _listMode, _tupleLength);
				return loader.Sync(ref sync, name, null);
			} else {
				var saver = new ListSaverC<SM, ICollection<T>, T, SyncItem>(_syncItem, _listMode);
				return saver.Sync(ref sync, name, savable);
			}
		}

		public HashSet<T>? Sync(ref SM sync, FieldId name, HashSet<T>? savable)
		{
			if (!sync.IsSaving) {
				var loader = new ListLoader<SM, HashSet<T>, T, CollectionBuilder<HashSet<T>, T>, SyncItem>
					(_syncItem, new CollectionBuilder<HashSet<T>, T>(Alloc<T>.HashSet), _listMode, _tupleLength);
				return loader.Sync(ref sync, name, null);
			} else {
				var saver = new ListSaver<SM, HashSet<T>, T, SyncItem>(_syncItem, _listMode);
				return saver.Sync(ref sync, name, savable);
			}
		}
	}

	/// <summary>Contains an implementation of ISyncField for any <see cref="ICollection{T}"/> type.
	/// The constructor requires a synchronizer that implements <see cref="ISyncField{SM, T}"/>.</summary>
	public struct SyncList<SM, T, List, SyncItem> :
		ISyncField<SM, List>
		where SM : ISyncManager
		where SyncItem : ISyncField<SM, T>
		where List : ICollection<T>
	{
		SyncItem _syncItem;
		SubObjectMode _listMode;
		int _tupleLength;
		Func<int, List> _alloc;

		public SyncList(SyncItem syncItem, SubObjectMode listMode, int tupleLength, Func<int, List> alloc)
		{
			this._syncItem = syncItem;
			this._listMode = listMode;
			this._tupleLength = tupleLength;
			this._alloc = alloc;
		}

		public List? Sync(ref SM sync, FieldId name, List? savable)
		{
			if (!sync.IsSaving) {
				return new ListLoader<SM, List, T, CollectionBuilder<List, T>, SyncItem>
					(_syncItem, new CollectionBuilder<List, T>(_alloc), _listMode, _tupleLength).Sync(ref sync, name, savable);
			} else {
				return new ListSaverC<SM, List, T, SyncItem>(_syncItem, _listMode)
					.Sync(ref sync, name, savable);
			}
		}
	}

	/// <summary>Allocation delegates used with <see cref="CollectionBuilder{TList, T}"/></summary>
	internal class Alloc<T>
	{
		public static readonly Func<int, List<T>> List = min => new List<T>(min <= 1 ? 4 : min);
		public static readonly Func<int, DList<T>> DList = min => new DList<T>(min <= 1 ? 4 : min);
		
		#if NETSTANDARD2_0 || NET45 || NET451 || NET452 || NET46 || NET461 || NET462 || NET47 || NET471 || NET472
		public static readonly Func<int, HashSet<T>> HashSet = min => new HashSet<T>();
		#else
		public static readonly Func<int, HashSet<T>> HashSet = min => new HashSet<T>(min <= 1 ? 4 : min);
		#endif
	}
}
