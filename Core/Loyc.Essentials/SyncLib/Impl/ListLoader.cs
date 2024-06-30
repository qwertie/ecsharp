using Loyc.Collections;
using Loyc.SyncLib.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Loyc.SyncLib.Impl
{
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
						_builder.Alloc(sync.MinimumListLength!.Value);
						for (int index = listLength; sync.ReachedEndOfList != true && index > 0; index--)
							_builder.Add(_syncItem.Sync(ref sync, null, default(T))!);
					}
					return _builder.List;
				} finally {
					sync.EndSubObject();
				}
			} else {
				Debug.Assert(obj != null);
				return _builder.CastList(obj);
			}
		}
	}
}
