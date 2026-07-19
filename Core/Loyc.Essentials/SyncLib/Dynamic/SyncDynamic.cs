using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib;

/// <summary>A synchronizer for dynamically-typed (polymorphic) fields: when
///   writing, the runtime type of the value selects a synchronizer from a
///   <see cref="TypeSyncRegistry"/> and its type tag (from a
///   <see cref="TypeTagRegistry"/>) is recorded; when reading, the type tag read
///   from the data stream selects the synchronizer. Being an ordinary
///   <see cref="ISyncField{SM, T}"/>, it composes with the list/collection
///   helpers (e.g. as the item synchronizer of a list).</summary>
/// <remarks>
///   By default the ambient services (<see cref="TypeSyncRegistry.Default"/> and
///   <see cref="TypeTagRegistry.Default"/>) are consulted on every call; the
///   two-argument constructor binds specific instances instead.
///   See also: <see cref="SyncDynamicExt.SyncDyn{SM, T}(SM, FieldId, T, ObjectMode)"/>.</remarks>
public struct DynamicSync<SM, T> : ISyncField<SM, T> where SM : ISyncManager
{
	ObjectMode _mode;
	TypeSyncRegistry? _synchronizers; // null => use the ambient SyncTypeRegistry.Default
	TypeTagRegistry? _tags;           // null => use the ambient TypeTagRegistry.Default

	public DynamicSync(ObjectMode mode)
	{
		_mode = mode;
		_synchronizers = null;
		_tags = null;
	}

	/// <summary>Binds this synchronizer to specific registry instances instead of
	///   the ambient (async-local) defaults. This is useful when different data
	///   streams need different registrations at the same time, and avoids any
	///   dependence on thread identity (e.g. across await points).</summary>
	public DynamicSync(TypeSyncRegistry synchronizers, TypeTagRegistry? tags = null,
		ObjectMode mode = ObjectMode.Deduplicate)
	{
		_mode = mode;
		_synchronizers = synchronizers ?? throw new ArgumentNullException(nameof(synchronizers));
		_tags = tags;
	}

	public T? Sync(ref SM sync, FieldId name, T? value)
	{
		object? childKey = sync.Mode == SyncMode.Schema ? (object?)typeof(T) : value;
		var (begun, _, existing) = sync.BeginSubObject(name, childKey, _mode);
		if (!begun)
		{
			if ((_mode & ObjectMode.ReadNullAsDefault) != 0 && existing == null)
				return default;
			try
			{
				return (T?)existing;
			}
			catch (Exception)
			{
				string got = existing?.GetType().NameWithGenericArgs() ?? "null";
				throw new InvalidCastException(
					$"{sync.GetType().Name}: expected {typeof(T).NameWithGenericArgs()}, got {got}");
			}
		}
		object? result;
		try
		{
			var tables = (_synchronizers ?? TypeSyncRegistry.Default).TablesFor<SM>();
			var tags = _tags ?? TypeTagRegistry.Default;
			if (sync.Mode == SyncMode.Reading)
			{
				string? tag = (_mode & ObjectMode.NoTypeTag) == 0 ? sync.SyncTypeTag(null) : null;
				Type? type = null;
				if (tag != null)
				{
					// An unknown tag is reported to the policy handler, which throws by
					// default; it may also substitute a type, or return null to fall
					// back to the statically-expected type.
					type = tags.TypeOf(tag) ?? tags.UnknownTagError(tag, typeof(T), name);
					if (type != null && !typeof(T).IsAssignableFrom(type))
						throw new FormatException(
							$"'{name.Name}' is tagged '{tag}', which is {type.NameWithGenericArgs()} — " +
							$"not a {typeof(T).NameWithGenericArgs()} as expected.");
				}
				type = type ?? typeof(T);
				var body = tables.TryByType(type) ?? throw new FormatException(
					tag == null
					? $"'{name.Name}' has no type tag, and {typeof(T).NameWithGenericArgs()} itself " +
					  "is not registered in the SyncTypeRegistry."
					: $"'{name.Name}' (tagged '{tag}'): {type.NameWithGenericArgs()} " +
					  "is not registered in the SyncTypeRegistry.");
				result = body(sync, value);
				sync.CurrentObject = result!;
			}
			else
			{
				Type runtimeType = value != null ? value.GetType() : typeof(T);
				var body = tables.TryByType(runtimeType) ?? throw new NotSupportedException(
					$"Cannot synchronize '{name.Name}' dynamically: {runtimeType.NameWithGenericArgs()} " +
					"is not registered in the SyncTypeRegistry.");
				if ((_mode & ObjectMode.NoTypeTag) == 0)
				{
					string? tag = tags.TagOf(runtimeType);
					if (tag == null && runtimeType != typeof(T))
						throw new NotSupportedException(
							$"Cannot write {runtimeType.NameWithGenericArgs()} dynamically as " +
							$"{typeof(T).NameWithGenericArgs()}: it has no type tag in the " +
							"TypeTagRegistry (see TypeTagAttribute).");
					sync.SyncTypeTag(tag);
				}
				result = body(sync, value);
			}
		}
		catch (Exception)
		{
			try
			{
				sync.EndSubObject();
			}
			catch
			{
				// This exception is probably caused by the previous failure, so ignore it.
			}
			throw;
		}
		sync.EndSubObject();
		try
		{
			return (T?)result;
		}
		catch (InvalidCastException)
		{
			throw new InvalidCastException(
				$"'{name.Name}' was read as {result?.GetType().NameWithGenericArgs() ?? "null"}, " +
				$"which is not a {typeof(T).NameWithGenericArgs()} as expected.");
		}
	}
}
