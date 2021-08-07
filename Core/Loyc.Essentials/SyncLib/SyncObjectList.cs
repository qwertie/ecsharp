using Loyc.Collections;
using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib.Impl
{
	/// <summary>Contains implementations of ISyncField for several types of lists.
	/// The constructor requires a synchronizer that implements <see cref="ISyncObject{SM, T}"/>.</summary>
	/// <typeparam name="SM">An implementation of ISyncManager (or ISyncManager itself)</typeparam>
	/// <typeparam name="T">Type of each list item</typeparam>
	/// <typeparam name="SyncObj">Synchronizer for an individual list item</typeparam>
	public struct SyncObjectList<SM, T, SyncObj> :
		ISyncField<SM, List<T>>,
		ISyncField<SM, Memory<T>>,
		ISyncField<SM, ReadOnlyMemory<T>>,
		ISyncField<SM, T[]>,
		ISyncField<SM, IReadOnlyList<T>>,
		ISyncField<SM, IReadOnlyCollection<T>>,
		ISyncField<SM, IListSource<T>>
		where SM : ISyncManager
		where SyncObj : ISyncObject<SM, T>
	{
		SyncList<SM, T, ObjectSyncher<SM, SyncObj, T>> _sync;

		public SyncObjectList(SyncObj syncObj, SubObjectMode itemMode, SubObjectMode listMode, int tupleLength)
		{
			this._sync = new SyncList<SM, T, ObjectSyncher<SM, SyncObj, T>>(
				new ObjectSyncher<SM, SyncObj, T>(syncObj, itemMode), listMode, tupleLength);
		}

		public List<T>? Sync(ref SM sync, FieldId name, List<T>? savable) => _sync.Sync(ref sync, name, savable);
		public Memory<T> Sync(ref SM sync, FieldId name, Memory<T> savable) => _sync.Sync(ref sync, name, savable);
		public ReadOnlyMemory<T> Sync(ref SM sync, FieldId name, ReadOnlyMemory<T> savable) => _sync.Sync(ref sync, name, savable);
		public T[]? Sync(ref SM sync, FieldId name, T[]? savable) => _sync.Sync(ref sync, name, savable);
		public IReadOnlyList<T>? Sync(ref SM sync, FieldId name, IReadOnlyList<T>? savable) => _sync.Sync(ref sync, name, savable);
		public IReadOnlyCollection<T>? Sync(ref SM sync, FieldId name, IReadOnlyCollection<T>? savable) => _sync.Sync(ref sync, name, savable);
		public IListSource<T>? Sync(ref SM sync, FieldId name, IListSource<T>? savable) => _sync.Sync(ref sync, name, savable);
	}

	/// <summary>Contains implementations of ISyncField for several types of lists.
	/// The constructor needs a <see cref="SyncObjectFunc{SM, T}"/> delegate to synchronize 
	/// each list item.</summary>
	/// <typeparam name="SM">An implementation of ISyncManager (or ISyncManager itself)</typeparam>
	/// <typeparam name="T">Type of each list item</typeparam>
	/// <typeparam name="SyncObj">Synchronizer for an individual list item</typeparam>
	public struct SyncObjectList<SM, T> :
		ISyncField<SM, List<T>>,
		ISyncField<SM, Memory<T>>,
		ISyncField<SM, ReadOnlyMemory<T>>,
		ISyncField<SM, T[]>,
		ISyncField<SM, IReadOnlyList<T>>,
		ISyncField<SM, IReadOnlyCollection<T>>,
		ISyncField<SM, IListSource<T>>
		where SM : ISyncManager
	{
		SyncList<SM, T, ObjectSyncher<SM, AsISyncObject<SM, T>, T>> _sync;

		public SyncObjectList(SyncObjectFunc<SM, T> syncObj, SubObjectMode listMode, SubObjectMode itemMode, int tupleLength)
		{
			this._sync = new SyncList<SM, T, ObjectSyncher<SM, AsISyncObject<SM, T>, T>>(
				new ObjectSyncher<SM, AsISyncObject<SM, T>, T>(syncObj, itemMode), listMode, tupleLength);
		}

		public List<T>? Sync(ref SM sync, FieldId name, List<T>? savable) => _sync.Sync(ref sync, name, savable);
		public Memory<T> Sync(ref SM sync, FieldId name, Memory<T> savable) => _sync.Sync(ref sync, name, savable);
		public ReadOnlyMemory<T> Sync(ref SM sync, FieldId name, ReadOnlyMemory<T> savable) => _sync.Sync(ref sync, name, savable);
		public T[]? Sync(ref SM sync, FieldId name, T[]? savable) => _sync.Sync(ref sync, name, savable);
		public IReadOnlyList<T>? Sync(ref SM sync, FieldId name, IReadOnlyList<T>? savable) => _sync.Sync(ref sync, name, savable);
		public IReadOnlyCollection<T>? Sync(ref SM sync, FieldId name, IReadOnlyCollection<T>? savable) => _sync.Sync(ref sync, name, savable);
		public IListSource<T>? Sync(ref SM sync, FieldId name, IListSource<T>? savable) => _sync.Sync(ref sync, name, savable);
	}
}
