using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Loyc.SyncLib.Impl
{
	public struct SyncTimeSpanAsString<SyncManager> : ISyncField<SyncManager, TimeSpan>, ISyncField<SyncManager, TimeSpan?>
		where SyncManager : ISyncManager
	{
		public string ToString(TimeSpan time) => time.ToString();
		public TimeSpan? ToTimeSpan(string? time) => time == null ? null : TimeSpan.Parse(time);

		public TimeSpan Sync(ref SyncManager sync, FieldId name, TimeSpan value)
		{
			var mode = sync.Mode;
			string? loadedValue;
			if ((mode & SyncMode.Writing) != 0) {
				var timeStr = ToString(value);
				if ((mode & SyncMode.Reading) != 0) {
					loadedValue = sync.Sync(name, timeStr);
				} else {
					sync.Sync(name, timeStr);
					return value;
				}
			} else {
				loadedValue = sync.Sync(name, (string?)null);
			}

			return ToTimeSpan(loadedValue) ?? throw new FormatException("'{0}' was unexpectedly null".Localized(name.Name));
		}

		public TimeSpan? Sync(ref SyncManager sync, FieldId name, TimeSpan? value)
		{
			var mode = sync.Mode;
			string? loadedValue;
			if ((mode & SyncMode.Writing) != 0) {
				var timeStr = value == null ? null : ToString(value.Value);
				if ((mode & SyncMode.Reading) != 0) {
					loadedValue = sync.Sync(name, timeStr);
				} else {
					sync.Sync(name, timeStr);
					return value;
				}
			} else {
				loadedValue = sync.Sync(name, (string?)null);
			}

			return ToTimeSpan(loadedValue);
		}
	}

	public struct SyncTimeSpanAsDays<SyncManager> : ISyncField<SyncManager, TimeSpan>, ISyncField<SyncManager, TimeSpan?>
		where SyncManager : ISyncManager
	{
		public TimeSpan Sync(ref SyncManager sync, FieldId name, TimeSpan value)
		{
			return TimeSpan.FromDays(sync.Sync(name, value.TotalDays));
		}

		public TimeSpan? Sync(ref SyncManager sync, FieldId name, TimeSpan? value)
		{
			var result = sync.Sync(name, value == null ? null : value.Value.TotalDays);
			return result == null ? null : TimeSpan.FromDays(result.Value);
		}
	}

	public struct SyncTimeSpanAsSeconds<SyncManager> : ISyncField<SyncManager, TimeSpan>
		where SyncManager : ISyncManager
	{
		bool _asInt32;

		public SyncTimeSpanAsSeconds(bool asInt32) => _asInt32 = asInt32;

		public TimeSpan Sync(ref SyncManager sync, FieldId name, TimeSpan value)
		{
			if (_asInt32)
				return TimeSpan.FromSeconds(sync.Sync(name, checked((int)value.TotalSeconds)));
			else
				return TimeSpan.FromSeconds(sync.Sync(name, value.TotalSeconds));
		}

		public TimeSpan? Sync(ref SyncManager sync, FieldId name, TimeSpan? value)
		{
			if (_asInt32)
			{
				int? result = sync.Sync(name, value.HasValue ? checked((int)value.Value.TotalSeconds) : null);
				return result == null ? null : TimeSpan.FromSeconds(result.Value);
			}
			else
			{
				double? result = sync.Sync(name, value.HasValue ? value.Value.TotalSeconds : null);
				return result == null ? null : TimeSpan.FromSeconds(result.Value);
			}
		}
	}

	public struct SyncTimeSpanAsMinutes<SyncManager> : ISyncField<SyncManager, TimeSpan>, ISyncField<SyncManager, TimeSpan?>
		where SyncManager : ISyncManager
	{
		bool _asInt32;

		public SyncTimeSpanAsMinutes(bool asInt32) => _asInt32 = asInt32;

		public TimeSpan Sync(ref SyncManager sync, FieldId name, TimeSpan value)
		{
			if (_asInt32)
				return TimeSpan.FromMinutes(sync.Sync(name, checked((int)value.TotalMinutes)));
			else
				return TimeSpan.FromMinutes(sync.Sync(name, value.TotalMinutes));
		}

		public TimeSpan? Sync(ref SyncManager sync, FieldId name, TimeSpan? value)
		{
			if (_asInt32)
			{
				int? result = sync.Sync(name, value.HasValue ? checked((int)value.Value.TotalMinutes) : null);
				return result == null ? null : TimeSpan.FromMinutes(result.Value);
			}
			else
			{
				double? result = sync.Sync(name, value.HasValue ? value.Value.TotalMinutes : null);
				return result == null ? null : TimeSpan.FromMinutes(result.Value);
			}
		}
	}
}
