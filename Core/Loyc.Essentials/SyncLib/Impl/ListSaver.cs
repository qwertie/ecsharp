using Loyc.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Loyc.SyncLib.Impl
{
	/// <summary>Contains a sync function for saving a <see cref="IReadOnlyCollection{T}"/>.
	/// Requires and assumes that IsSaving is true in the ISyncManager provided.</summary>
	public struct ListSaver<SyncManager, List, T, SyncItem> : ISyncField<SyncManager, List>
		where SyncManager : ISyncManager
		where List : IReadOnlyCollection<T>
		where SyncItem : ISyncField<SyncManager, T>
	{
		readonly SyncItem _syncItem;
		readonly SubObjectMode _listMode;

		public ListSaver(SyncItem syncItem, SubObjectMode listMode)
		{
			_syncItem = syncItem;
			_listMode = listMode | SubObjectMode.List;
		}

		public List? Sync(ref SyncManager sync, FieldId name, List? list)
		{
			Debug.Assert(sync.IsWriting);
			if (list == null) {
				var status = sync.BeginSubObject(name, null, _listMode, 0);
				Debug.Assert(!status.Begun && status.Object == null);
			} else {
				var saver = new ScannerSaver<SyncManager, ScannableEnumerable<T>.Scanner<IEnumerator<T>>, T, SyncItem>(_syncItem, _listMode);
				bool avoidBoxing = (_listMode & (SubObjectMode.Deduplicate | SubObjectMode.NotNull)) == SubObjectMode.NotNull;
				saver.Write(ref sync, name, new ScannableEnumerable<T>.Scanner<IEnumerator<T>>(list.GetEnumerator()), avoidBoxing ? null : list, list.Count);
			}
			return list;
		}
	}

	/// <summary>A variation of ListSaver<...> for lists that implement <see cref="ICollection{T}"/>.</summary>
	public struct ListSaverC<SyncManager, List, T, SyncItem> : ISyncField<SyncManager, List>
		where SyncManager : ISyncManager
		where List : ICollection<T>
		where SyncItem : ISyncField<SyncManager, T>
	{
		readonly SyncItem _syncItem;
		readonly SubObjectMode _listMode;

		public ListSaverC(SyncItem syncItem, SubObjectMode listMode)
		{
			_syncItem = syncItem;
			_listMode = listMode | SubObjectMode.List;
		}

		public List? Sync(ref SyncManager sync, FieldId name, List? list)
		{
			if (list == null) {
				var status = sync.BeginSubObject(name, null, _listMode, 0);
				Debug.Assert(!status.Begun && status.Object == null);
			} else {
				var saver = new ScannerSaver<SyncManager, ScannableEnumerable<T>.Scanner<IEnumerator<T>>, T, SyncItem>(_syncItem, _listMode);
				bool avoidBoxing = (_listMode & (SubObjectMode.Deduplicate | SubObjectMode.NotNull)) == SubObjectMode.NotNull;
				saver.Write(ref sync, name, new ScannableEnumerable<T>.Scanner<IEnumerator<T>>(list.GetEnumerator()), avoidBoxing ? null : list, list.Count);
			}
			return list;
		}
	}

	/// <summary>Contains a sync function for saving an <see cref="IScanner{T}"/>.
	/// Requires and assumes that IsSaving is true in the ISyncManager provided.</summary>
	public struct ScannerSaver<SyncManager, Scanner, T, SyncItem>
		where SyncManager : ISyncManager
		where Scanner : IScanner<T>
		where SyncItem : ISyncField<SyncManager, T>
	{
		readonly SyncItem _syncItem;
		readonly SubObjectMode _listMode;

		public ScannerSaver(SyncItem syncItem, SubObjectMode listMode)
		{
			_syncItem = syncItem;
			_listMode = listMode | SubObjectMode.List;
		}

		public void Write(ref SyncManager sync, FieldId name, Scanner scanner, object? list, int listCount)
		{
			Debug.Assert(!sync.IsReading);
			Debug.Assert(list != null || (_listMode & (SubObjectMode.Deduplicate | SubObjectMode.NotNull)) == SubObjectMode.NotNull);
			
			var (begunList, obj) = sync.BeginSubObject(name, list, _listMode, listCount);
			if (begunList) {
				Debug.Assert(sync.IsInsideList);
				Debug.Assert(scanner != null);
				try {
					Memory<T> mem = default;
					ReadOnlySpan<T> span = default;
					if (sync.Mode == SyncMode.Writing) {
						while ((span = scanner.Read(span.Length, -1, ref mem).Span).Length != 0) {
							for (int i = 0; i < span.Length; i++)
								_syncItem.Sync(ref sync, null, span[i]);
						}
					} else if (sync.Mode == SyncMode.Query) {
						while ((span = scanner.Read(span.Length, -1, ref mem).Span).Length != 0) {
							for (int i = 0; i < span.Length; i++) {
								if (sync.ReachedEndOfList == true)
									break;
								_syncItem.Sync(ref sync, null, span[i]);
							}
						}
					} else {
						Debug.Assert(sync.Mode == SyncMode.Merge);
						throw new NotImplementedException();
					}
				} finally {
					sync.EndSubObject();
				}
			} else {
				Debug.Assert(obj != null);
			}
		}
	}
}
