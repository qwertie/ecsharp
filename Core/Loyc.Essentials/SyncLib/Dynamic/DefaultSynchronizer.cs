using Loyc;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Graphs;
using Loyc.SyncLib.Impl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

// As in SyncManagerExt.cs: no "where K: notnull" constraint on dictionaries, since
// some Loyc dictionaries have never had that requirement.
#pragma warning disable 8714

namespace Loyc.SyncLib;

// If SyncManager is a struct, this is the type of most of its Sync methods
public delegate T SyncFieldFunc_Ref<SyncManager, T>(ref SyncManager sync, FieldId name, [AllowNull] T value);

// The type of Sync methods that accept an ObjectMode (e.g. the string overload)
internal delegate T SyncFieldFunc_RefMode<SyncManager, T>(ref SyncManager sync, FieldId name, [AllowNull] T value, ObjectMode mode);

/// <summary>The "synchronize any T with zero configuration" dispatcher. Given a type T, 
///   <see cref="Supports"/> and <see cref="Sync"/> try to find some mechanism to serialize it.
/// </summary><remarks>
///   This can be contrasted with <see cref="SyncDynamicExt"/>, which is meant for composite
///   types, especially cases where the a field's runtime type differs from its static type
///   (subclasses, interfaces).
/// <para/>
///   Why do both exist, Opus?
///  <code>
///  ┌───────────────────────────────┬──────────────────────────────────┬─────────────────────────────────────────┐
///  │                               │      DynamicSync / SyncDyn       │        DefaultSynchronizer.Sync         │
///  ├───────────────────────────────┼──────────────────────────────────┼─────────────────────────────────────────┤
///  │ Purpose                       │ Polymorphic dispatch by runtime  │ "Just serialize this T" convenience     │
///  │                               │ type                             │                                         │
///  ├───────────────────────────────┼──────────────────────────────────┼─────────────────────────────────────────┤
///  │ Type tags                     │ Yes — writes/reads them, checks  │ No (except via the DynamicSync it may   │
///  │                               │ assignability                    │ delegate to)                            │
///  ├───────────────────────────────┼──────────────────────────────────┼─────────────────────────────────────────┤
///  │ Handles                       │ No (registry-only)               │ Yes (the whole PredefinedSynchronizer   │
///  │ primitives/collections/tuples │                                  │ layer)                                  │
///  ├───────────────────────────────┼──────────────────────────────────┼─────────────────────────────────────────┤
///  │ Config knobs                  │ Registry instances, ObjectMode,  │ None — fully automatic                  │
///  │                               │ tag policy                       │                                         │
///  └───────────────────────────────┴──────────────────────────────────┴─────────────────────────────────────────┘ 
/// </code>
/// </remarks>
public static class DefaultSynchronizer
{
	public static bool Supports<T>()
	{
		if (DefaultSynchronizer<SyncBinary.Writer, T>.Default != DefaultSynchronizer<SyncBinary.Writer, T>._FallbackSync)
			return true;
		return DefaultSynchronizer<SyncBinary.Writer, T>.FindSynchronizer() != null;
	}

	public static T Sync<SyncManager, T>(ref SyncManager sync, FieldId name, [AllowNull] T value) where SyncManager: ISyncManager
	{
		return DefaultSynchronizer<SyncManager, T>.Default(ref sync, name, value);
	}
}

/// <summary>An <see cref="ISyncField{SM,T}"/> that synchronizes via
///   <see cref="DefaultSynchronizer{SM,T}"/>. It is used to build default
///   synchronizers for collections (the items are synchronized recursively via
///   their own default synchronizers) and can be used the same way in user code.</summary>
public struct DefaultSyncField<SyncManager, T> : ISyncField<SyncManager, T>
	where SyncManager : ISyncManager
{
	public T? Sync(ref SyncManager sync, FieldId name, T? value)
		=> DefaultSynchronizer<SyncManager, T>.Default(ref sync, name, value);
}

public static class DefaultSynchronizer<SyncManager, T> where SyncManager: ISyncManager
{
	internal static SyncFieldFunc_Ref<SyncManager, T> _FallbackSync = FallbackSync;
	internal static SyncFieldFunc_Ref<SyncManager, T> Default = FindSynchronizer() ?? _FallbackSync;

	internal static SyncFieldFunc_Ref<SyncManager, T>? FindSynchronizer()
	{
		var sync = PredefinedSynchronizer<SyncManager>.Get<T>();
		if (sync != null)
			return sync;
		// If the (ambient) SyncTypeRegistry can handle T — either T itself is
		// registered or some type derived from T is — return a dynamic-typing
		// synchronizer. Note: the cached synchronizer is registry-AGNOSTIC (it
		// consults SyncTypeRegistry.Default on every call), which keeps this
		// process-global cache correct when the ambient registry is swapped.
		if (TypeSyncRegistry.Default.Handles(typeof(T)))
			return SyncViaRegistry;
		return null;
	}

	static T SyncViaRegistry(ref SyncManager sync, FieldId name, T? value)
		=> new DynamicSync<SyncManager, T>(ObjectMode.Deduplicate).Sync(ref sync, name, value)!;

	static T FallbackSync(ref SyncManager sync, FieldId name, T? value)
	{
		var syncMethod = FindSynchronizer();
		if (syncMethod != null) {
			Default = syncMethod;
			return syncMethod(ref sync, name, value);
		}
		throw new NotSupportedException("There is no default synchronizer for " + typeof(T).NameWithGenericArgs());
	}
}


/// <summary>Default synchronizers, beyond those provided by the SyncManager
///   itself and by <see cref="TupleSynchronizer{SM}"/>, that
///   <see cref="DefaultSynchronizer{SM,T}"/> discovers via reflection (once per
///   closed generic pair): arrays, common collections, dictionaries,
///   KeyValuePair, enums, DateTime and TimeSpan. Collection items are
///   synchronized recursively via <see cref="DefaultSyncField{SM,T}"/>, so e.g.
///   <c>List&lt;(int, Person[])></c> works as long as Person is registered in the
///   ambient <see cref="TypeSyncRegistry"/>.</summary>
static class ExtraSynchronizers<SyncManager> where SyncManager : ISyncManager
{
	// Lists of bytes/bools/chars get special treatment from the SyncManager
	// itself (e.g. SyncJson stores byte[] as a Base64 or BAIS string).
	public static byte[]? Sync(ref SyncManager sync, FieldId name, byte[]? value)
		=> sync.SyncArray(name, value);
	public static bool[]? Sync(ref SyncManager sync, FieldId name, bool[]? value)
		=> sync.SyncArray(name, value);
	public static char[]? Sync(ref SyncManager sync, FieldId name, char[]? value)
		=> sync.SyncArray(name, value);

	public static DateTime Sync(ref SyncManager sync, FieldId name, DateTime value)
		=> new SyncDateAsString<SyncManager>(null, System.Globalization.DateTimeStyles.AllowWhiteSpaces)
			.Sync(ref sync, name, value);
	public static DateTime? Sync(ref SyncManager sync, FieldId name, DateTime? value)
		=> new SyncDateAsString<SyncManager>(null, System.Globalization.DateTimeStyles.AllowWhiteSpaces)
			.Sync(ref sync, name, value);
	public static TimeSpan Sync(ref SyncManager sync, FieldId name, TimeSpan value)
		=> new SyncTimeSpanAsString<SyncManager>().Sync(ref sync, name, value);
	public static TimeSpan? Sync(ref SyncManager sync, FieldId name, TimeSpan? value)
		=> new SyncTimeSpanAsString<SyncManager>().Sync(ref sync, name, value);

	/// <summary>Synchronizes an enum numerically (as its underlying value), which
	///   suits binary formats and Protocol Buffers; <see cref="SyncEnumAsString{SM,E}"/>
	///   remains available as an opt-in alternative.</summary>
	public static E SyncEnum<E>(ref SyncManager sync, FieldId name, E value) where E : struct, Enum
	{
		long num = sync.Sync(name, ToInt64(value));
		return sync.IsReading ? (E) Enum.ToObject(typeof(E), num) : value;

		static long ToInt64(E value)
			=> Type.GetTypeCode(typeof(E)) == TypeCode.UInt64
				? unchecked((long) Convert.ToUInt64(value))
				: Convert.ToInt64(value);
	}

	public static E[]? SyncArray<E>(ref SyncManager sync, FieldId name, E[]? value)
		=> new SyncList<SyncManager, E, DefaultSyncField<SyncManager, E>>(default, ObjectMode.List, -1)
			.Sync(ref sync, name, value);

	public static List<E>? Sync<E>(ref SyncManager sync, FieldId name, List<E>? value)
		=> new SyncList<SyncManager, E, DefaultSyncField<SyncManager, E>>(default, ObjectMode.List, -1)
			.Sync(ref sync, name, value);
	public static IList<E>? Sync<E>(ref SyncManager sync, FieldId name, IList<E>? value)
		=> new SyncList<SyncManager, E, DefaultSyncField<SyncManager, E>>(default, ObjectMode.List, -1)
			.Sync(ref sync, name, value);
	public static IReadOnlyList<E>? Sync<E>(ref SyncManager sync, FieldId name, IReadOnlyList<E>? value)
		=> new SyncList<SyncManager, E, DefaultSyncField<SyncManager, E>>(default, ObjectMode.List, -1)
			.Sync(ref sync, name, value);
	public static HashSet<E>? Sync<E>(ref SyncManager sync, FieldId name, HashSet<E>? value)
		=> new SyncList<SyncManager, E, DefaultSyncField<SyncManager, E>>(default, ObjectMode.List, -1)
			.Sync(ref sync, name, value);

	public static Dictionary<K, V>? Sync<K, V>(ref SyncManager sync, FieldId name, Dictionary<K, V>? value)
		=> new SyncList<SyncManager, KeyValuePair<K, V>, Dictionary<K, V>, DefaultSyncField<SyncManager, KeyValuePair<K, V>>>(
				default, ObjectMode.List, -1, min => new Dictionary<K, V>(min))
			.Sync(ref sync, name, value);
	public static IDictionary<K, V>? Sync<K, V>(ref SyncManager sync, FieldId name, IDictionary<K, V>? value)
		=> new SyncList<SyncManager, KeyValuePair<K, V>, IDictionary<K, V>, DefaultSyncField<SyncManager, KeyValuePair<K, V>>>(
				default, ObjectMode.List, -1, min => new Dictionary<K, V>(min))
			.Sync(ref sync, name, value);

	public static KeyValuePair<K, V> Sync<K, V>(ref SyncManager sync, FieldId name, KeyValuePair<K, V> value)
	{
		var (begun, _, obj) = sync.BeginSubObject(name, null, ObjectMode.NotNull | ObjectMode.Tuple, 2);
		if (!begun)
			return obj is KeyValuePair<K, V> pair ? pair : default;
		var key = DefaultSynchronizer.Sync(ref sync, null, value.Key);
		var val = DefaultSynchronizer.Sync(ref sync, null, value.Value);
		sync.EndSubObject();
		return new KeyValuePair<K, V>(key!, val!);
	}
}
