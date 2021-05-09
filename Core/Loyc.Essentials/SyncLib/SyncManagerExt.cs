using Loyc.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Loyc.SyncLib
{
	/// <summary>Standard extension methods for <see cref="ISyncManager"/>.</summary>
	public static partial class SyncManagerExt
	{
		[return: MaybeNull]
		public static T Sync<SyncManager, T>(this SyncManager sync,
			string name, T? savable, SyncObjectFunc<SyncManager, T?> syncFunc,
			SubObjectMode mode = SubObjectMode.Deduplicate)
				where SyncManager : ISyncManager
			=> Sync(sync, (Symbol)name, savable, syncFunc, mode);

		[return: MaybeNull]
		public static T Sync<SyncManager, T>(this SyncManager sync, 
			Symbol? name, T? savable, SyncObjectFunc<SyncManager, T?> syncFunc, 
			SubObjectMode mode = SubObjectMode.Deduplicate)
				where SyncManager : ISyncManager
		{
			object? childKey = null;
			if ((mode & (SubObjectMode.Deduplicate | SubObjectMode.NotNull)) != SubObjectMode.NotNull)
				childKey = savable;
			var (started, obj) = sync.BeginSubObject(name, childKey, mode);
			if (started) {
				try {
					return syncFunc(sync, savable);
				} finally {
					sync.EndSubObject();
				}
			} else {
				var syncMode = sync.Mode;
				if (syncMode == SyncMode.Saving || syncMode == SyncMode.Query)
					return savable;
				try {
					return (T) obj;
				} catch (InvalidCastException) {
					throw new InvalidCastException(
						$"{sync.GetType().Name}: expected {typeof(T).Name}, got {obj?.GetType().Name}");
				}
			}
		}
		
		[return: MaybeNull]
		public static List SyncList<SyncManager, T, List>(this SyncManager sync, 
			Symbol? name, [AllowNull] List list, 
			SyncObjectFunc<SyncManager, T> syncItemObject, Func<int, List> allocate,
			SubObjectMode itemMode = SubObjectMode.Normal | SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List)
				where SyncManager : ISyncManager
				where List : ICollection<T>
		{
			object? childKey = null;
			int count = 0;
			if (list != null) {
				if ((listMode & (SubObjectMode.Deduplicate | SubObjectMode.NotNull)) != SubObjectMode.NotNull)
					childKey = list;
				count = list.Count;
			}

			var (begunList, obj) = sync.BeginSubObject(name, childKey, listMode, count);

			var syncMode = sync.Mode;
			bool itemsAreLists = (listMode & SubObjectMode.List) == SubObjectMode.List;

			if (begunList)
			{
				Debug.Assert(sync.IsInsideList);
				try {
					if ((syncMode & SyncMode.Saving) != 0)
					{
						if (list == null) {
							Debug.Assert((listMode & SubObjectMode.NotNull) == SubObjectMode.NotNull);
							throw new ArgumentNullException(nameof(list));
						}
						if (syncMode == SyncMode.Saving) {
							// Common cases (optimized)
							if ((itemMode & (SubObjectMode.Deduplicate | SubObjectMode.NotNull)) == SubObjectMode.NotNull) {
								foreach (T item in list!)
									SaveListItem(sync, item, null, itemMode, syncItemObject);
							} else {
								foreach (T item in list!)
									SaveListItem(sync, item, item, itemMode, syncItemObject);
							}
						} else {
							// Query or Merge mode
							foreach (T item in list!) {
								if (sync.ReachedEndOfList == true)
									break;
								object? itemKey = null;
								if ((itemMode & (SubObjectMode.Deduplicate | SubObjectMode.NotNull)) != SubObjectMode.NotNull)
									itemKey = list;
								var (begun, itemKey2) = sync.BeginSubObject(null, itemKey, itemMode);
								if (begun) {
									try {
										syncItemObject(sync, item);
									} finally {
										sync.EndSubObject();
									}
								} else {
									Debug.Assert(itemKey == itemKey2 || itemKey == null);
								}
							}
						}
					}
					else
					{   // Loading or Schema mode
						if (allocate != null)
							list = allocate(sync.MinimumListLength ?? 4);
						else if (list == null)
							throw new ArgumentNullException(nameof(list));
						for (int index = 0; sync.ReachedEndOfList != true; index++) {
							var (begun, duplicateItem) = sync.BeginSubObject(null, null, itemMode);
							if (begun) {
								try {
									var item = syncItemObject(sync, default(T));
									list.Add(item);
								} finally { sync.EndSubObject(); }
							} else {
								try {
									list.Add((T) duplicateItem);
								} catch (InvalidCastException) {
									throw new InvalidCastException(
									  $"{sync.GetType().Name}: expected {typeof(T).Name}, got {obj?.GetType().Name}");
								}
							}
						}
					}
				}
				finally
				{
					sync.EndSubObject();
				}
				return list;
			}
			else
			{
				if ((syncMode & SyncMode.Saving) != 0)
					return list;
				try {
					return (List) obj;
				} catch (InvalidCastException) {
					throw new InvalidCastException(
					  $"{sync.GetType().Name}: expected {typeof(T).Name}, got {obj?.GetType().Name}");
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void SaveListItem<SyncManager, T>(SyncManager sync, T? item, object? itemKey, SubObjectMode itemMode, SyncObjectFunc<SyncManager, T> syncItemObject) where SyncManager : ISyncManager
		{
			G.Verify(sync.BeginSubObject(null, itemKey, itemMode).Begun);
			try {
				syncItemObject(sync, item);
			} finally {
				sync.EndSubObject();
			}
		}

		private static int GetCount<T>([MaybeNull] T value)
		{
			if (value == null)
				return 0;
			if (value is IReadOnlyCollection<T> roc)
				return roc.Count;
			if (value is ICollection<T> c)
				return c.Count;
			if (value is ICount icount)
				return icount.Count;
			if (value is IEnumerable e) {
				int count = 0;
				foreach (var item in e)
					count++;
				return count;
			}
			return -1; // Some implementations of ISyncManager may throw in this case
			//throw new ArgumentException("SubObjectMode.List was used to serialize a non-collection", "itemMode");
		}
	}
}
