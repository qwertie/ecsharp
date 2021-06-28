using Loyc.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;

namespace Loyc.SyncLib.Impl
{
	/// <summary>
	/// A class to help implementations of <see cref="ISyncManager"/> to implement its many 
	/// overloads of SyncList in a way that enables high performance.
	/// </summary>
	public static class SyncManagerHelper
	{
		public static void SaveList<SyncManager, T, List, SyncWrite>(
			ref SyncManager sync, 
			Symbol? name, List list, 
			SyncWrite itemWriter, SubObjectMode listMode = SubObjectMode.List)
				where SyncManager : ISyncManager
				where List : IReadOnlyList<T>
				where SyncWrite : ISyncWrite<SyncManager, T>
		{
			Debug.Assert((sync.Mode & SyncMode.Saving) != 0);
			object? childKey = null;
			int count = 0;
			if ((listMode & (SubObjectMode.Deduplicate | SubObjectMode.NotNull)) != SubObjectMode.NotNull) {
				childKey = list;
				count = list.Count;
			}

			if (sync.BeginSubObject(name, childKey, listMode, count).Begun)
			{
				foreach (T item in list!) {
					itemWriter.Write(ref sync, null, item);
				}
				sync.EndSubObject();
			}
		}

		public static void SaveList<SyncManager, T, SyncWrite>(
			ref SyncManager sync, 
			Symbol? name, ReadOnlySpan<T> list, 
			SyncWrite itemWriter, SubObjectMode listMode = SubObjectMode.List)
				where SyncManager : ISyncManager
				where SyncWrite : ISyncWrite<SyncManager, T>
		{
			Debug.Assert((sync.Mode & SyncMode.Saving) != 0);
			if (list != default) {
				// Ensure that the null childKey parameter is ignored
				listMode = (listMode & ~SubObjectMode.Deduplicate) | SubObjectMode.NotNull;
			}

			if (sync.BeginSubObject(name, childKey: null, listMode, list.Length).Begun)
			{
				for (int i = 0; i < list.Length; i++) {
					itemWriter.Write(ref sync, name, list[i]);
				}
				sync.EndSubObject();
			}
		}

		/// <summary>Reads a list of items from a SyncManager.</summary>
		/// <returns>If the list was already loaded earlier, the existing object is 
		/// returned. Otherwise, this method returns null and the <c>list</c> parameter 
		/// is a reference to the new list (to avoid boxing, <c>list</c> is not 
		/// returned, in case List is a value type).</returns>
		public static object? LoadList<SyncManager, T, List, SyncField>(
			this SyncManager sync, 
			Symbol? name, ref List list, 
			SyncField itemReader,
			Func<int, List> allocate,
			SubObjectMode listMode = SubObjectMode.List)
				where SyncManager : ISyncManager
				where List : IAdd<T?>
				where SyncField : ISyncField<SyncManager, T>
		{
			Debug.Assert((sync.Mode & SyncMode.Loading) != 0);

			var (begunList, obj) = sync.BeginSubObject(name, null, listMode);
			if (begunList)
			{
				Debug.Assert(sync.IsInsideList);
				try {
					if (list == null) {
						if (allocate != null)
							list = allocate(sync.MinimumListLength ?? 4);
						else
							throw new ArgumentNullException(nameof(allocate));
					}

					for (int index = 0; sync.ReachedEndOfList != true; index++) {
						var item = itemReader.Sync(ref sync, null, default(T));
						list.Add(item);
					}
				}
				finally
				{
					sync.EndSubObject();
				}
				return null;
			}
			else
			{
				return obj;
				//} catch (InvalidCastException) {
				//	string? got = obj?.GetType().NameWithGenericArgs() ?? "null";
				//	throw new InvalidCastException(
				//		$"{sync.GetType().Name}: expected {typeof(T).NameWithGenericArgs()}, got {got}");
				//}
			}
		}
	}
}
