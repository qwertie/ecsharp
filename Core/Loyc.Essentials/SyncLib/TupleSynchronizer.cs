using System;

namespace Loyc.SyncLib;

/// <summary>System.ValueTuple synchronizers - work in progress!</summary>
public static class TupleSynchronizer<SyncManager> where SyncManager : ISyncManager
{
	public static ValueTuple<Item1> Sync<Item1>(ref SyncManager sync, FieldId name, ValueTuple<Item1> value)
	{
		var (begun, _, obj) = sync.BeginSubObject(name, null, ObjectMode.NotNull | ObjectMode.Tuple, 1);
		if (!begun)
			return obj is ValueTuple<Item1> tuple ? tuple : default;
		var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
		sync.EndSubObject();
		return new ValueTuple<Item1>(item1);
	}

	public static ValueTuple<Item1, Item2> Sync<Item1, Item2>(ref SyncManager sync, FieldId name, ValueTuple<Item1, Item2> value)
	{
		var (begun, _, obj) = sync.BeginSubObject(name, null, ObjectMode.NotNull | ObjectMode.Tuple, 2);
		if (!begun)
			return obj is ValueTuple<Item1, Item2> tuple ? tuple : default;
		var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
		var item2 = DefaultSynchronizer.Sync(ref sync, null, value.Item2);
		sync.EndSubObject();
		return new ValueTuple<Item1, Item2>(item1, item2);
	}

	public static ValueTuple<Item1, Item2, Item3> Sync<Item1, Item2, Item3>(ref SyncManager sync, FieldId name, ValueTuple<Item1, Item2, Item3> value)
	{
		var (begun, _, obj) = sync.BeginSubObject(name, null, ObjectMode.NotNull | ObjectMode.Tuple, 3);
		if (!begun)
			return obj is ValueTuple<Item1, Item2, Item3> tuple ? tuple : default;
		var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
		var item2 = DefaultSynchronizer.Sync(ref sync, null, value.Item2);
		var item3 = DefaultSynchronizer.Sync(ref sync, null, value.Item3);
		sync.EndSubObject();
		return new ValueTuple<Item1, Item2, Item3>(item1, item2, item3);
	}

	public static ValueTuple<Item1, Item2, Item3, Item4> Sync<Item1, Item2, Item3, Item4>(ref SyncManager sync, FieldId name, ValueTuple<Item1, Item2, Item3, Item4> value)
	{
		var (begun, _, obj) = sync.BeginSubObject(name, null, ObjectMode.NotNull | ObjectMode.Tuple, 4);
		if (!begun)
			return obj is ValueTuple<Item1, Item2, Item3, Item4> tuple ? tuple : default;
		var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
		var item2 = DefaultSynchronizer.Sync(ref sync, null, value.Item2);
		var item3 = DefaultSynchronizer.Sync(ref sync, null, value.Item3);
		var item4 = DefaultSynchronizer.Sync(ref sync, null, value.Item4);
		sync.EndSubObject();
		return new ValueTuple<Item1, Item2, Item3, Item4>(item1, item2, item3, item4);
	}

	public static ValueTuple<Item1, Item2, Item3, Item4, Item5> Sync<Item1, Item2, Item3, Item4, Item5>(ref SyncManager sync, FieldId name, ValueTuple<Item1, Item2, Item3, Item4, Item5> value)
	{
		var (begun, _, obj) = sync.BeginSubObject(name, null, ObjectMode.NotNull | ObjectMode.Tuple, 5);
		if (!begun)
			return obj is ValueTuple<Item1, Item2, Item3, Item4, Item5> tuple ? tuple : default;
		var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
		var item2 = DefaultSynchronizer.Sync(ref sync, null, value.Item2);
		var item3 = DefaultSynchronizer.Sync(ref sync, null, value.Item3);
		var item4 = DefaultSynchronizer.Sync(ref sync, null, value.Item4);
		var item5 = DefaultSynchronizer.Sync(ref sync, null, value.Item5);
		sync.EndSubObject();
		return new ValueTuple<Item1, Item2, Item3, Item4, Item5>(item1, item2, item3, item4, item5);
	}

	public static ValueTuple<Item1, Item2, Item3, Item4, Item5, Item6> Sync<Item1, Item2, Item3, Item4, Item5, Item6>(ref SyncManager sync, FieldId name, ValueTuple<Item1, Item2, Item3, Item4, Item5, Item6> value)
	{
		var (begun, _, obj) = sync.BeginSubObject(name, null, ObjectMode.NotNull | ObjectMode.Tuple, 6);
		if (!begun)
			return obj is ValueTuple<Item1, Item2, Item3, Item4, Item5, Item6> tuple ? tuple : default;
		var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
		var item2 = DefaultSynchronizer.Sync(ref sync, null, value.Item2);
		var item3 = DefaultSynchronizer.Sync(ref sync, null, value.Item3);
		var item4 = DefaultSynchronizer.Sync(ref sync, null, value.Item4);
		var item5 = DefaultSynchronizer.Sync(ref sync, null, value.Item5);
		var item6 = DefaultSynchronizer.Sync(ref sync, null, value.Item6);
		sync.EndSubObject();
		return new ValueTuple<Item1, Item2, Item3, Item4, Item5, Item6>(item1, item2, item3, item4, item5, item6);
	}

	public static ValueTuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7> Sync<Item1, Item2, Item3, Item4, Item5, Item6, Item7>(ref SyncManager sync, FieldId name, ValueTuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7> value)
	{
		var (begun, _, obj) = sync.BeginSubObject(name, null, ObjectMode.NotNull | ObjectMode.Tuple, 7);
		if (!begun)
			return obj is ValueTuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7> tuple ? tuple : default;
		var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
		var item2 = DefaultSynchronizer.Sync(ref sync, null, value.Item2);
		var item3 = DefaultSynchronizer.Sync(ref sync, null, value.Item3);
		var item4 = DefaultSynchronizer.Sync(ref sync, null, value.Item4);
		var item5 = DefaultSynchronizer.Sync(ref sync, null, value.Item5);
		var item6 = DefaultSynchronizer.Sync(ref sync, null, value.Item6);
		var item7 = DefaultSynchronizer.Sync(ref sync, null, value.Item7);
		sync.EndSubObject();
		return new ValueTuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7>(item1, item2, item3, item4, item5, item6, item7);
	}

	public static ValueTuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7, Rest> Sync<Item1, Item2, Item3, Item4, Item5, Item6, Item7, Rest>(ref SyncManager sync, FieldId name, ValueTuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7, Rest> value)
		where Rest : struct
	{
		var (begun, _, obj) = sync.BeginSubObject(name, null, ObjectMode.NotNull | ObjectMode.Tuple, 8);
		if (!begun)
			return obj is ValueTuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7, Rest> tuple ? tuple : default;
		var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
		var item2 = DefaultSynchronizer.Sync(ref sync, null, value.Item2);
		var item3 = DefaultSynchronizer.Sync(ref sync, null, value.Item3);
		var item4 = DefaultSynchronizer.Sync(ref sync, null, value.Item4);
		var item5 = DefaultSynchronizer.Sync(ref sync, null, value.Item5);
		var item6 = DefaultSynchronizer.Sync(ref sync, null, value.Item6);
		var item7 = DefaultSynchronizer.Sync(ref sync, null, value.Item7);
		var rest = DefaultSynchronizer.Sync(ref sync, null, value.Rest);
		sync.EndSubObject();
		return new ValueTuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7, Rest>(item1, item2, item3, item4, item5, item6, item7, rest);
	}

	// System.Tuple synchronizers

	public static Tuple<Item1>? Sync<Item1>(ref SyncManager sync, FieldId name, Tuple<Item1>? value)
	{
		var (begun, _, obj) = sync.BeginSubObject(name, value, ObjectMode.Tuple, 1);
		if (!begun)
			return (Tuple<Item1>?)obj;
		var item1 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item1);
		sync.EndSubObject();
		return new Tuple<Item1>(item1);
	}

	public static Tuple<Item1, Item2>? Sync<Item1, Item2>(ref SyncManager sync, FieldId name, Tuple<Item1, Item2>? value)
	{
		var (begun, _, obj) = sync.BeginSubObject(name, value, ObjectMode.Tuple, 2);
		if (!begun)
			return (Tuple<Item1, Item2>?)obj;
		var item1 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item1);
		var item2 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item2);
		sync.EndSubObject();
		return new Tuple<Item1, Item2>(item1, item2);
	}

	public static Tuple<Item1, Item2, Item3>? Sync<Item1, Item2, Item3>(ref SyncManager sync, FieldId name, Tuple<Item1, Item2, Item3>? value)
	{
		var (begun, _, obj) = sync.BeginSubObject(name, value, ObjectMode.Tuple, 3);
		if (!begun)
			return (Tuple<Item1, Item2, Item3>?)obj;
		var item1 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item1);
		var item2 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item2);
		var item3 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item3);
		sync.EndSubObject();
		return new Tuple<Item1, Item2, Item3>(item1, item2, item3);
	}

	public static Tuple<Item1, Item2, Item3, Item4>? Sync<Item1, Item2, Item3, Item4>(ref SyncManager sync, FieldId name, Tuple<Item1, Item2, Item3, Item4>? value)
	{
		var (begun, _, obj) = sync.BeginSubObject(name, value, ObjectMode.Tuple, 4);
		if (!begun)
			return (Tuple<Item1, Item2, Item3, Item4>?)obj;
		var item1 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item1);
		var item2 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item2);
		var item3 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item3);
		var item4 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item4);
		sync.EndSubObject();
		return new Tuple<Item1, Item2, Item3, Item4>(item1, item2, item3, item4);
	}

	public static Tuple<Item1, Item2, Item3, Item4, Item5>? Sync<Item1, Item2, Item3, Item4, Item5>(ref SyncManager sync, FieldId name, Tuple<Item1, Item2, Item3, Item4, Item5>? value)
	{
		var (begun, _, obj) = sync.BeginSubObject(name, value, ObjectMode.Tuple, 5);
		if (!begun)
			return (Tuple<Item1, Item2, Item3, Item4, Item5>?)obj;
		var item1 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item1);
		var item2 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item2);
		var item3 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item3);
		var item4 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item4);
		var item5 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item5);
		sync.EndSubObject();
		return new Tuple<Item1, Item2, Item3, Item4, Item5>(item1, item2, item3, item4, item5);
	}

	public static Tuple<Item1, Item2, Item3, Item4, Item5, Item6>? Sync<Item1, Item2, Item3, Item4, Item5, Item6>(ref SyncManager sync, FieldId name, Tuple<Item1, Item2, Item3, Item4, Item5, Item6>? value)
	{
		var (begun, _, obj) = sync.BeginSubObject(name, value, ObjectMode.Tuple, 6);
		if (!begun)
			return (Tuple<Item1, Item2, Item3, Item4, Item5, Item6>?)obj;
		var item1 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item1);
		var item2 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item2);
		var item3 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item3);
		var item4 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item4);
		var item5 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item5);
		var item6 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item6);
		sync.EndSubObject();
		return new Tuple<Item1, Item2, Item3, Item4, Item5, Item6>(item1, item2, item3, item4, item5, item6);
	}

	public static Tuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7>? Sync<Item1, Item2, Item3, Item4, Item5, Item6, Item7>(ref SyncManager sync, FieldId name, Tuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7>? value)
	{
		var (begun, _, obj) = sync.BeginSubObject(name, value, ObjectMode.Tuple, 7);
		if (!begun)
			return (Tuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7>?)obj;
		var item1 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item1);
		var item2 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item2);
		var item3 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item3);
		var item4 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item4);
		var item5 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item5);
		var item6 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item6);
		var item7 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item7);
		sync.EndSubObject();
		return new Tuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7>(item1, item2, item3, item4, item5, item6, item7);
	}

	public static Tuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7, Rest>? Sync<Item1, Item2, Item3, Item4, Item5, Item6, Item7, Rest>(ref SyncManager sync, FieldId name, Tuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7, Rest>? value)
		where Rest : struct
	{
		var (begun, _, obj) = sync.BeginSubObject(name, value, ObjectMode.Tuple, 8);
		if (!begun)
			return (Tuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7, Rest>?)obj;
		var item1 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item1);
		var item2 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item2);
		var item3 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item3);
		var item4 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item4);
		var item5 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item5);
		var item6 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item6);
		var item7 = DefaultSynchronizer.Sync(ref sync, null, value is null ? default! : value.Item7);
		var rest = DefaultSynchronizer.Sync(ref sync, null, value is null ? default : value.Rest);
		sync.EndSubObject();
		return new Tuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7, Rest>(item1, item2, item3, item4, item5, item6, item7, rest);
	}
}
