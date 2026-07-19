using Loyc.SyncLib.Impl;
using System.Collections.Generic;

namespace Loyc.SyncLib;

/// <summary>Extension methods for dynamically-typed (polymorphic) synchronization.
///   By default these use the ambient services <see cref="TypeSyncRegistry.Default"/>
///   and <see cref="TypeTagRegistry.Default"/>; overloads that accept registry
///   instances bypass the ambient services. See <see cref="TypeSyncRegistry"/>,
///   <see cref="TypeTagRegistry"/> and <see cref="TypeTagAttribute"/> to learn how
///   dynamic typing works.</summary>
public static class SyncDynamicExt
{
	/// <summary>Reads/writes a field whose runtime type may differ from its static
	///   type <c>T</c>, using the synchronizers and type tags registered in the
	///   ambient services (<see cref="TypeSyncRegistry.Default"/> and
	///   <see cref="TypeTagRegistry.Default"/>).</summary>
	public static T? SyncDyn<SM, T>(this SM sync, FieldId name, T? savable, ObjectMode mode = ObjectMode.Deduplicate)
			where SM : ISyncManager
		=> new DynamicSync<SM, T>(mode).Sync(ref sync, name, savable);

	/// <summary>Reads/writes a dynamically-typed field using specific registry
	///   instances rather than the ambient ones. When <c>tags</c> is null,
	///   <see cref="TypeTagRegistry.Default"/> is used for tags.</summary>
	public static T? SyncDyn<SM, T>(this SM sync, FieldId name, T? savable,
		TypeSyncRegistry synchronizers, TypeTagRegistry? tags = null, ObjectMode mode = ObjectMode.Deduplicate)
			where SM : ISyncManager
		=> new DynamicSync<SM, T>(synchronizers, tags, mode).Sync(ref sync, name, savable);

	public static List<T>? SyncDynList<SM, T>(this SM sync, FieldId name, List<T>? savable,
		ObjectMode itemMode = ObjectMode.Deduplicate, ObjectMode listMode = ObjectMode.List, int tupleLength = -1)
			where SM : ISyncManager
		=> new SyncList<SM, T, DynamicSync<SM, T>>(new DynamicSync<SM, T>(itemMode), listMode, tupleLength)
			.Sync(ref sync, name, savable);

	public static T[]? SyncDynList<SM, T>(this SM sync, FieldId name, T[]? savable,
		ObjectMode itemMode = ObjectMode.Deduplicate, ObjectMode listMode = ObjectMode.List, int tupleLength = -1)
			where SM : ISyncManager
		=> new SyncList<SM, T, DynamicSync<SM, T>>(new DynamicSync<SM, T>(itemMode), listMode, tupleLength)
			.Sync(ref sync, name, savable);

	public static IList<T>? SyncDynList<SM, T>(this SM sync, FieldId name, IList<T>? savable,
		ObjectMode itemMode = ObjectMode.Deduplicate, ObjectMode listMode = ObjectMode.List, int tupleLength = -1)
			where SM : ISyncManager
		=> new SyncList<SM, T, DynamicSync<SM, T>>(new DynamicSync<SM, T>(itemMode), listMode, tupleLength)
			.Sync(ref sync, name, savable);

	public static List<T>? SyncDynList<SM, T>(this SM sync, FieldId name, List<T>? savable,
		TypeSyncRegistry synchronizers, TypeTagRegistry? tags = null,
		ObjectMode itemMode = ObjectMode.Deduplicate, ObjectMode listMode = ObjectMode.List, int tupleLength = -1)
			where SM : ISyncManager
		=> new SyncList<SM, T, DynamicSync<SM, T>>(
				new DynamicSync<SM, T>(synchronizers, tags, itemMode), listMode, tupleLength)
			.Sync(ref sync, name, savable);

	/// <summary>Reads/writes a field of any type for which a synchronizer is known:
	///   either a built-in one (primitives, strings, tuples, arrays, common
	///   collections, DateTime/TimeSpan, enums) or one registered in
	///   <see cref="TypeSyncRegistry.Default"/> (in which case this behaves like
	///   <see cref="SyncDyn{SM, T}(SM, FieldId, T, ObjectMode)"/>). Note:
	///   primitive types don't reach this extension method because
	///   <see cref="ISyncManager"/>'s instance methods take precedence in overload
	///   resolution.</summary>
	public static T SyncAny<SM, T>(this SM sync, FieldId name, T savable)
		where SM : ISyncManager
		=> DefaultSynchronizer.Sync(ref sync, name, savable)!;
}
