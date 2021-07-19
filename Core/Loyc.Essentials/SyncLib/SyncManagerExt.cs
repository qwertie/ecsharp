using Loyc.Collections;
using Loyc.SyncLib.Impl;
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
		public static T? Sync<SyncManager, T>(this SyncManager sync,
			Symbol? name, T? savable, SyncObjectFunc<SyncManager, T> syncFunc,
			SubObjectMode mode = SubObjectMode.Deduplicate)
				where SyncManager : ISyncManager
		{
			return ObjectSyncher.For(syncFunc, mode).Sync(ref sync, name, savable);
		}

		public static T? Sync<SyncManager, SyncObj, T>(this SyncManager sync,
			Symbol? name, T? savable, SyncObj syncObj,
			SubObjectMode mode = SubObjectMode.Deduplicate)
				where SyncManager : ISyncManager
				where SyncObj : ISyncObject<SyncManager, T>
		{
			return new ObjectSyncher<SyncManager, SyncObj, T>(syncObj, mode).Sync(ref sync, name, savable);
		}

		#region SyncList methods that accept SyncObjectFunc<SM, T>

		public static List<T>? SyncList<SyncManager, T>(this SyncManager sync,
			Symbol? name, List<T>? savable,
			SyncObjectFunc<SyncManager, T> syncFunc,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SyncManager : ISyncManager
		{
			var syncItem = ObjectSyncher.For(syncFunc, itemMode);
			if ((sync.Mode & SyncMode.Loading) != 0) {
				var loader = new ListLoader<SyncManager, List<T>, T, ListBuilder<T>, ObjectSyncher<SyncManager, AsISyncObject<SyncManager, T>, T>>
					(syncItem, new ListBuilder<T>(), listMode, tupleLength);
				return loader.Sync(ref sync, name, savable);
			} else {
				var saver = new ListSaver<SyncManager, List<T>, T, ObjectSyncher<SyncManager, AsISyncObject<SyncManager, T>, T>>
					(syncItem, listMode);
				return saver.Sync(ref sync, name, savable);
			}
		}

		public static T[]? SyncList<SyncManager, T>(this SyncManager sync,
			Symbol? name, T[]? savable,
			SyncObjectFunc<SyncManager, T> syncFunc,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SyncManager : ISyncManager
		{
			var syncItem = ObjectSyncher.For(syncFunc, itemMode);
			if ((sync.Mode & SyncMode.Loading) != 0) {
				var loader = new ListLoader<SyncManager, T[], T, ArrayBuilder<T>, ObjectSyncher<SyncManager, AsISyncObject<SyncManager, T>, T>>
					(syncItem, new ArrayBuilder<T>(), listMode, tupleLength);
				return loader.Sync(ref sync, name, savable);
			} else {
				var saver = new ListSaver<SyncManager, T[], T, ObjectSyncher<SyncManager, AsISyncObject<SyncManager, T>, T>>
					(syncItem, listMode);
				return saver.Sync(ref sync, name, savable);
			}
		}

		public static Memory<T> SyncList<SyncManager, T>(this SyncManager sync,
			Symbol? name, Memory<T> savable,
			SyncObjectFunc<SyncManager, T> syncFunc,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SyncManager : ISyncManager
		{
			var syncItem = ObjectSyncher.For(syncFunc, itemMode);
			if ((sync.Mode & SyncMode.Loading) != 0) {
				var loader = new ListLoader<SyncManager, Memory<T>, T, MemoryBuilder<T>, ObjectSyncher<SyncManager, AsISyncObject<SyncManager, T>, T>>
					(syncItem, new MemoryBuilder<T>(), listMode, tupleLength);
				return loader.Sync(ref sync, name, savable);
			} else {
				var saver = new ListSaver<SyncManager, ArraySlice<T>, T, ObjectSyncher<SyncManager, AsISyncObject<SyncManager, T>, T>>
					(syncItem, listMode);
				return saver.Sync(ref sync, name, savable);
			}
		}

		public static List? SyncList<SyncManager, List, T>(this SyncManager sync,
			Symbol? name, List? savable,
			SyncObjectFunc<SyncManager, T> syncFunc, Func<int, List> alloc,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SyncManager : ISyncManager
				where List : ICollection<T>, IReadOnlyCollection<T>
		{
			var syncItem = ObjectSyncher.For(syncFunc, itemMode);
			if ((sync.Mode & SyncMode.Loading) != 0) {
				var loader = new ListLoader<SyncManager, List, T, CollectionBuilder<List, T>, ObjectSyncher<SyncManager, AsISyncObject<SyncManager, T>, T>>
					(syncItem, new CollectionBuilder<List, T>(alloc), listMode, tupleLength);
				return loader.Sync(ref sync, name, savable);
			} else {
				var saver = new ListSaver<SyncManager, List, T, ObjectSyncher<SyncManager, AsISyncObject<SyncManager, T>, T>>
					(syncItem, listMode);
				return saver.Sync(ref sync, name, savable);
			}
		}

		public static List<T>? SyncList<SyncManager, T>(this SyncManager sync,
			string name, List<T>? savable,
			SyncObjectFunc<SyncManager, T> syncFunc,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SyncManager : ISyncManager
		{
			return SyncList(sync, (Symbol)name, savable, syncFunc, itemMode, listMode, tupleLength);
		}

		public static T[]? SyncList<SyncManager, T>(this SyncManager sync,
			string name, T[]? savable,
			SyncObjectFunc<SyncManager, T> syncFunc,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SyncManager : ISyncManager
		{
			return SyncList(sync, (Symbol)name, savable, syncFunc, itemMode, listMode, tupleLength);
		}

		public static Memory<T> SyncList<SyncManager, T>(this SyncManager sync,
			string name, Memory<T> savable,
			SyncObjectFunc<SyncManager, T> syncFunc,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SyncManager : ISyncManager
		{
			return SyncList(sync, (Symbol)name, savable, syncFunc, itemMode, listMode, tupleLength);
		}

		public static List? SyncList<SyncManager, List, T>(this SyncManager sync,
			string name, List? savable,
			SyncObjectFunc<SyncManager, T> syncFunc, Func<int, List> alloc,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SyncManager : ISyncManager
				where List : ICollection<T>, IReadOnlyCollection<T>
		{
			return SyncList(sync, (Symbol)name, savable, syncFunc, alloc, itemMode, listMode, tupleLength);
		}

		#endregion

		#region SyncList methods that accept ISyncField<SM, T>

		public static List<T>? SyncList<SyncManager, T, SyncField>(this SyncManager sync,
			Symbol? name, List<T>? savable,
			SyncField syncItem,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SyncManager : ISyncManager
				where SyncField : ISyncField<SyncManager, T>
		{
			if ((sync.Mode & SyncMode.Loading) != 0) {
				var loader = new ListLoader<SyncManager, List<T>, T, ListBuilder<T>, SyncField>
					(syncItem, new ListBuilder<T>(), listMode, tupleLength);
				return loader.Sync(ref sync, name, savable);
			} else {
				var saver = new ListSaver<SyncManager, List<T>, T, SyncField>
					(syncItem, listMode);
				return saver.Sync(ref sync, name, savable);
			}
		}

		public static T[]? SyncList<SyncManager, T, SyncField>(this SyncManager sync,
			Symbol? name, T[]? savable,
			SyncField syncItem,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SyncManager : ISyncManager
				where SyncField : ISyncField<SyncManager, T>
		{
			if ((sync.Mode & SyncMode.Loading) != 0) {
				var loader = new ListLoader<SyncManager, T[], T, ArrayBuilder<T>, SyncField>
					(syncItem, new ArrayBuilder<T>(), listMode, tupleLength);
				return loader.Sync(ref sync, name, savable);
			} else {
				var saver = new ListSaver<SyncManager, T[], T, SyncField>
					(syncItem, listMode);
				return saver.Sync(ref sync, name, savable);
			}
		}

		public static Memory<T> SyncList<SyncManager, T, SyncField>(this SyncManager sync,
			Symbol? name, Memory<T> savable,
			SyncField syncItem,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SyncManager : ISyncManager
				where SyncField : ISyncField<SyncManager, T>
		{
			if ((sync.Mode & SyncMode.Loading) != 0) {
				var loader = new ListLoader<SyncManager, Memory<T>, T, MemoryBuilder<T>, SyncField>
					(syncItem, new MemoryBuilder<T>(), listMode, tupleLength);
				return loader.Sync(ref sync, name, savable);
			} else {
				var saver = new ListSaver<SyncManager, ArraySlice<T>, T, SyncField>
					(syncItem, listMode);
				return saver.Sync(ref sync, name, savable);
			}
		}

		public static List? SyncList<SyncManager, List, T, SyncField>(this SyncManager sync,
			Symbol? name, List? savable,
			SyncField syncItem, Func<int, List> alloc,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SyncManager : ISyncManager
				where List : ICollection<T>, IReadOnlyCollection<T>
				where SyncField : ISyncField<SyncManager, T>
		{
			if ((sync.Mode & SyncMode.Loading) != 0) {
				var loader = new ListLoader<SyncManager, List, T, CollectionBuilder<List, T>, SyncField>
					(syncItem, new CollectionBuilder<List, T>(alloc), listMode, tupleLength);
				return loader.Sync(ref sync, name, savable);
			} else {
				var saver = new ListSaver<SyncManager, List, T, SyncField>
					(syncItem, listMode);
				return saver.Sync(ref sync, name, savable);
			}
		}

		// Ummmm supporting IList<T> doesn't quite work
		//public static IList<T>? SyncList<SyncManager, T, SyncField>(this SyncManager sync,
		//	Symbol? name, IList<T>? savable,
		//	SyncField syncItem, 
		//	SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//		where SyncManager : ISyncManager
		//		where SyncField : ISyncField<SyncManager, T>
		//{
		//	return SyncList<SyncManager, IListAndReadOnly<T>, T, SyncField>(sync, name, savable, syncItem, cap => new DList<T>(cap), listMode, tupleLength);
		//}

		public static List<T>? SyncList<SyncManager, T, SyncField>(this SyncManager sync,
			string name, List<T>? savable,
			SyncField syncItem,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SyncManager : ISyncManager
				where SyncField : ISyncField<SyncManager, T>
		{
			return SyncList(sync, (Symbol)name, savable, syncItem, listMode, tupleLength);
		}

		public static T[]? SyncList<SyncManager, T, SyncField>(this SyncManager sync,
			string name, T[]? savable,
			SyncField syncItem,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SyncManager : ISyncManager
				where SyncField : ISyncField<SyncManager, T>
		{
			return SyncList(sync, (Symbol)name, savable, syncItem, listMode, tupleLength);
		}

		public static Memory<T> SyncList<SyncManager, T, SyncField>(this SyncManager sync,
			string name, Memory<T> savable,
			SyncField syncItem,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SyncManager : ISyncManager
				where SyncField : ISyncField<SyncManager, T>
		{
			return SyncList(sync, (Symbol)name, savable, syncItem, listMode, tupleLength);
		}

		public static List? SyncList<SyncManager, List, T, SyncField>(this SyncManager sync,
			string name, List? savable,
			SyncField syncItem, Func<int, List> alloc,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SyncManager : ISyncManager
				where List : ICollection<T>, IReadOnlyCollection<T>
				where SyncField : ISyncField<SyncManager, T>
		{
			return SyncList<SyncManager, List, T, SyncField>(sync, (Symbol)name, savable, syncItem, alloc, listMode, tupleLength);
		}

		#endregion
	}
}
