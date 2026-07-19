using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
				new AsISyncObject<SyncManager, T>(func), mode, TypeTagRegistry.Default.AttributeTagOf(func));
	}

	/// <summary>A helper for reading/writing objects and structs via <see cref="ISyncObject{SyncManager, T}"/>.</summary>
	public struct ObjectSyncher<SyncManager, SyncObj, T> : ISyncField<SyncManager, T>
		where SyncManager : ISyncManager
		where SyncObj : ISyncObject<SyncManager, T>
	{
		SyncObj _syncObj;
		ObjectMode _mode;
		string? _typeTag;

		// Resolved once per closed generic type (using the TypeTagRegistry that is
		// ambient at first use, normally the root one): a [TypeTag] on the SyncObj
		// struct (or on one of its methods returning T) costs nothing per call.
		static class DefaultTag
		{
			public static readonly string? Value = TypeTagRegistry.Default.AttributeTagOf(typeof(SyncObj), typeof(T));
		}

		public ObjectSyncher(SyncObj sync, ObjectMode mode) : this(sync, mode, DefaultTag.Value) { }

		public ObjectSyncher(SyncObj sync, ObjectMode mode, string? typeTag)
		{
			_syncObj = sync;
			_mode = mode;
			_typeTag = typeTag;
		}

		/// <summary>Reads or writes the type tag, which must happen immediately after
		///   BeginSubObject, before the object's first field. The tag is owned by this
		///   wrapper (not by the sync function) so that a reader can, in a dynamically-
		///   typed context, read the tag first and use it to choose a sync function.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void SyncTypeTag(ref SyncManager sync, FieldId propName)
		{
			if (_typeTag != null && (_mode & ObjectMode.NoTypeTag) == 0) {
				string? streamTag = sync.SyncTypeTag(_typeTag);
				// When reading, verify the tag (leniently: some formats, like JSON, can
				// detect that the tag is absent, and an absent tag is accepted). A
				// mismatch is reported to the ambient policy, which throws by default;
				// if it returns, the read proceeds with our synchronizer anyway.
				if (sync.Mode == SyncMode.Reading && streamTag != null && streamTag != _typeTag)
					TypeTagRegistry.Default.TagMismatchError(_typeTag, streamTag, typeof(T), propName);
			}
		}

		public T? Sync(ref SyncManager sync, FieldId propName, T? item)
		{
			bool avoidBoxing = (_mode & (ObjectMode.Deduplicate | ObjectMode.NotNull)) == ObjectMode.NotNull;
			// When avoidBoxing, readers and writers ignore childKey, so pass typeof(T),
			// which a schema saver (SyncMode.Schema) requires: it identifies the schema
			// of the sub-object, since in Schema mode there is no data.
			object? childKey = avoidBoxing || sync.Mode == SyncMode.Schema ? typeof(T) : item;
			var (begun, length, existingItem) = sync.BeginSubObject(propName, childKey, _mode);
			if (begun) {
				try {
					SyncTypeTag(ref sync, propName);
					var result = _syncObj.Sync(sync, item);
					if (!avoidBoxing)
						sync.CurrentObject = result!;
					sync.EndSubObject();
					return result;
				} catch(Exception) {
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
					SyncTypeTag(ref sync, propName);
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
