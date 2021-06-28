using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Loyc.SyncLib;

namespace Loyc.SyncLib.Impl
{
	/// <summary>Represents the low-level synchronization behavior for a single list 
	/// item. <see cref="SyncManagerHelper"/> needs this.</summary>
	/// <remarks>This interface is used instead of <see cref="SyncFieldFunc_Ref"/> 
	/// so that the implementation can be a struct, in order to take advantage of the
	/// CLR's ability to specialize generic methods for structs, which provides better
	/// performance by avoiding indirect calls and enabling inlining.</remarks>
	public interface ISyncField<SyncManager, T>
	{
		T? Sync(ref SyncManager sync, Symbol? propName, T? x);
	}
	public interface ISyncWrite<SyncManager, T>
	{
		void Write(ref SyncManager sync, Symbol? propName, T? x);
	}

	/// <summary>A helper for reading data using a <see cref="SyncObjectFunc{SyncManager, T}"/>.</summary>
	public struct ObjectReader<SyncManager, T> : ISyncField<SyncManager, T> where SyncManager : ISyncManager
	{
		SyncObjectFunc<SyncManager, T> _sync;
		SubObjectMode _mode;

		public ObjectReader(SyncObjectFunc<SyncManager, T> sync, SubObjectMode mode)
		{
			_sync = sync;
			_mode = mode | SubObjectMode.NotNull;
		}

		public T? Sync(ref SyncManager sync, Symbol? propName, T? ignored)
		{
			var (begun, existingItem) = sync.BeginSubObject(propName, null, _mode);
			if (begun) {
				try {
					return _sync(sync, default(T));
				} finally {
					sync.EndSubObject();
				}
			} else {
				try {
					return (T) existingItem!;
				} catch (InvalidCastException) {
					string? got = existingItem?.GetType().NameWithGenericArgs() ?? "null";
					throw new InvalidCastException(
						$"{sync.GetType().Name}: expected {typeof(T).NameWithGenericArgs()}, got {got}");
				}
			}
		}
	}

	/// <summary>A helper for writing data using a <see cref="SyncObjectFunc{SyncManager, T}"/>.</summary>
	public struct ObjectWriter<SyncManager, T> : ISyncWrite<SyncManager, T> where SyncManager : ISyncManager
	{
		SyncObjectFunc<SyncManager, T> _sync;
		SubObjectMode _mode;

		public ObjectWriter(SyncObjectFunc<SyncManager, T> sync, SubObjectMode mode)
		{
			_sync = sync;
			_mode = mode | SubObjectMode.NotNull;
		}

		public void Write(ref SyncManager sync, Symbol? propName, T? item)
		{
			var (begun, existingItem) = sync.BeginSubObject(propName, item, _mode);
			if (begun) {
				try {
					_sync(sync, item);
				} finally {
					sync.EndSubObject();
				}
			} else {
				Debug.Assert((object?)item == existingItem || item == null);
			}
		}
	}

	/// <summary>A variation on ObjectWriter that is optimized for structs.
	/// It avoids boxing, requires <see cref="SubObjectMode.NotNull"/>, and does not 
	/// support <see cref="SubObjectMode.Deduplicate"/>.</summary>
	public struct StructWriter<SyncManager, T> : ISyncWrite<SyncManager, T> where SyncManager : ISyncManager
	{
		SyncObjectFunc<SyncManager, T> _sync;
		SubObjectMode _mode;

		public StructWriter(SyncObjectFunc<SyncManager, T> sync, SubObjectMode mode)
		{
			_sync = sync;
			_mode = (mode | SubObjectMode.NotNull) & ~SubObjectMode.Deduplicate;
		}

		public void Write(ref SyncManager sync, Symbol? propName, T? item)
		{
			var (begun, itemKey2) = sync.BeginSubObject(propName, null, _mode);
			Debug.Assert(begun);
			if (begun) {
				try {
					_sync(sync, item);
				} finally {
					sync.EndSubObject();
				}
			}
		}
	}
}
