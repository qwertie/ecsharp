using Loyc.Collections;
using Loyc.SyncLib.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Loyc.SyncLib.Impl
{
	public static class ListLoader
	{
		/// <summary>The maximum amount of memory that a list loader preallocates for a
		///   list (when the data stream says how long it is). A longer list still loads,
		///   but its memory grows as items are actually read, so that a corrupt or
		///   malicious length prefix cannot cause a huge allocation. Readers that load a
		///   list's data in bulk (e.g. byte lists in SyncBinary) use the same limit for
		///   the amount of data they request before seeing that the data exists.</summary>
		public const int MaxPreallocationBytes = 16 * 1024 * 1024;
	}

	public struct ListLoader<SyncManager, TList, T, ListBuilder, SyncItem> : ISyncField<SyncManager, TList>
		where SyncManager : ISyncManager
		where ListBuilder : IListBuilder<TList, T>
		where SyncItem : ISyncField<SyncManager, T>
	{
		SyncItem _syncItem;
		ListBuilder _builder;
		readonly ObjectMode _listMode;
		readonly int _tupleLength;

		public ListLoader(SyncItem syncItem, ListBuilder builder, ObjectMode listMode, int tupleLength = -1)
		{
			_syncItem = syncItem;
			_builder = builder;
			_listMode = listMode;
			_tupleLength = tupleLength;
		}

		public TList? Sync(ref SyncManager sync, FieldId propName, TList? ignored)
		{
			Debug.Assert((sync.Mode & SyncMode.Reading) != 0);

			var (begunList, listLength, obj) = sync.BeginSubObject(propName, null, _listMode);
			if (begunList) {
				Debug.Assert(sync.IsInsideList);
				try {
					if ((_listMode & ObjectMode.Tuple) == ObjectMode.Tuple) {
						Debug.Assert(_tupleLength > -1);
						_builder.Alloc(_tupleLength);
						for (int index = _tupleLength; sync.ReachedEndOfList != true && index > 0; index--)
							_builder.Add(_syncItem.Sync(ref sync, null, default(T))!);
					} else {
						int maxPrealloc = ListLoader.MaxPreallocationBytes / Unsafe.SizeOf<T>();
						_builder.Alloc(System.Math.Min(sync.MinimumListLength!.Value, maxPrealloc));
						for (int index = listLength; sync.ReachedEndOfList != true && index > 0; index--)
							_builder.Add(_syncItem.Sync(ref sync, null, default(T))!);
					}
					return _builder.List;
				} finally {
					sync.EndSubObject();
				}
			} else {
				// obj is null if the stream contained a null list; otherwise it is a
				// deduplicated list that was read earlier (a back-reference)
				return obj == null ? default : _builder.CastList(obj);
			}
		}
	}
}
