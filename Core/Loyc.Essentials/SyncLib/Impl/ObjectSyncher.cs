using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Loyc.SyncLib.Impl
{
	/// <summary>A helper for reading/writing data using a <see cref="SyncObjectFunc{SyncManager, T}"/>.</summary>
	public static class ObjectSyncher
	{
		public static ObjectSyncher<SyncManager, AsISyncObject<SyncManager, T>, T> 
			For<SyncManager, T>(SyncObjectFunc<SyncManager, T> func, ObjectMode mode)
			where SyncManager : ISyncManager
			=> new ObjectSyncher<SyncManager, AsISyncObject<SyncManager, T>, T>(
				new AsISyncObject<SyncManager, T>(func), mode);
	}

	/// <summary>A helper for reading/writing objects and structs via <see cref="ISyncObject{SyncManager, T}"/>.</summary>
	public struct ObjectSyncher<SyncManager, SyncObj, T> : ISyncField<SyncManager, T>
		where SyncManager : ISyncManager
		where SyncObj : ISyncObject<SyncManager, T>
	{
		SyncObj _syncObj;
		ObjectMode _mode;

		public ObjectSyncher(SyncObj sync, ObjectMode mode)
		{
			_syncObj = sync;
			_mode = mode;
		}

		public T? Sync(ref SyncManager sync, FieldId propName, T? item)
		{
			bool avoidBoxing = (_mode & (ObjectMode.Deduplicate | ObjectMode.NotNull)) == ObjectMode.NotNull;
			var (begun, length, existingItem) = sync.BeginSubObject(propName, avoidBoxing ? null : item, _mode);
			if (begun) {
				try {
					var result = _syncObj.Sync(sync, item);
					if (!avoidBoxing)
						sync.CurrentObject = result!;
					sync.EndSubObject();
					return result;
				} catch(Exception e) {
					try {
						sync.EndSubObject();
					} catch {
						// This exception is probably caused by the previous failure, so ignore it.
					}
					throw;
				}
			} else {
				if (avoidBoxing) {
					Debug.Assert(existingItem == null);
					return item!;
				}
				if ((_mode & ObjectMode.ReadNullAsDefault) != 0 && existingItem == null)
					return default;
				try {
					return (T?) existingItem;
				} catch (Exception) {
					// Either InvalidCastException, or NullReferenceException if casting null to struct
					string? got = existingItem?.GetType().NameWithGenericArgs() ?? "null";
					throw new InvalidCastException(
						$"{sync.GetType().Name}: expected {typeof(T).NameWithGenericArgs()}, got {got}");
				}
			}
		}

		public void Write(ref SyncManager sync, FieldId propName, T? item)
		{
			bool avoidBoxing = (_mode & (ObjectMode.Deduplicate | ObjectMode.NotNull)) == ObjectMode.NotNull;
			var (begun, length, existingItem) = sync.BeginSubObject(propName, avoidBoxing ? null : item, _mode);
			if (begun) {
				try {
					_syncObj.Sync(sync, item);
				} finally {
					sync.EndSubObject();
				}
			} else {
				Debug.Assert((object?)item == existingItem || item == null);
			}
		}
	}
}
