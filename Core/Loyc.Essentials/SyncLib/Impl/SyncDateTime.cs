using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Loyc.SyncLib.Impl
{
	public struct SyncDateAsString<SyncManager> : ISyncField<SyncManager, DateTime>, ISyncField<SyncManager, DateTime?>
		where SyncManager : ISyncManager
	{
		string? _preferredFormat;
		DateTimeStyles _parseMode;
		public SyncDateAsString(string? preferredFormat, DateTimeStyles parseMode)
		{
			_preferredFormat = preferredFormat;
			_parseMode = parseMode;
		}

		public string ToString(DateTime date)
			=> date.ToString(_preferredFormat ?? (date.Ticks % 10_000_000 == 0 ? "yyyy'-'MM'-'dd'T'HH':'mm':'ssK" : "O"));
		public DateTime? ToDateTime(string? dateStr, string? propName)
		{
			if (dateStr == null)
				return null;
			if (_preferredFormat != null && DateTime.TryParseExact(dateStr, _preferredFormat, null, _parseMode, out var date))
				return date;
			return DateTime.Parse(dateStr, null, _parseMode);
		}

		public DateTime Sync(ref SyncManager sync, FieldId name, DateTime value)
		{
			var mode = sync.Mode;
			string? loadedValue;
			if ((mode & SyncMode.Writing) != 0)
			{
				var dateStr = ToString(value);
				if ((mode & SyncMode.Reading) != 0) {
					loadedValue = sync.Sync(name, dateStr);
				} else {
					sync.Sync(name, dateStr);
					return value;
				}
			}
			else
			{
				loadedValue = sync.Sync(name, (string?) null);
			}

			return ToDateTime(loadedValue, name) ?? throw new FormatException("'{0}' was unexpectedly null".Localized(name.Name));
		}

		public DateTime? Sync(ref SyncManager sync, FieldId name, DateTime? value)
		{
			var mode = sync.Mode;
			string? loadedValue;
			if ((mode & SyncMode.Writing) != 0)
			{
				string? dateStr = value == null ? null : ToString(value.Value);
				if ((mode & SyncMode.Reading) != 0)
				{
					loadedValue = sync.Sync(name, dateStr);
				}
				else
				{
					sync.Sync(name, dateStr);
					return value;
				}
			}
			else
			{
				loadedValue = sync.Sync(name, (string?)null);
			}

			return ToDateTime(loadedValue, name);
		}
	}

	public struct SyncDateAsDayNumber<SyncManager> : ISyncField<SyncManager, DateTime>, ISyncField<SyncManager, DateTime?>
		where SyncManager : ISyncManager
	{
		bool _asInt32;

		public SyncDateAsDayNumber(bool asInt32)
		{
			_asInt32 = asInt32;
		}

		public DateTime Sync(ref SyncManager sync, FieldId name, DateTime value)
		{
			if (_asInt32)
				return DateTime.FromOADate(sync.Sync(name, (int)value.ToOADate()));
			else
				return DateTime.FromOADate(sync.Sync(name, value.ToOADate()));
		}
		
		public DateTime? Sync(ref SyncManager sync, FieldId name, DateTime? value)
		{
			if (_asInt32)
			{
				int? result = sync.Sync(name, value == null ? null : (int)value.Value.ToOADate());
				return result == null ? null : DateTime.FromOADate(result.Value);
			}
			else
			{
				double? result = sync.Sync(name, value == null ? null : value.Value.ToOADate());
				return result == null ? null : DateTime.FromOADate(result.Value);
			}
		}
	}
}
