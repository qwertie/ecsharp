using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Loyc.SyncLib.Impl
{
	/// <summary>The default synchronizer for times and dates (DateTime, DateTimeOffset
	///   and TimeSpan), used by the Sync extension methods in <see cref="SyncTimeExt"/>.
	///   In plain-text formats, values are written as strings in the same way as
	///   Newtonsoft.Json (see <see cref="SyncTimeAsString{SM}"/>); otherwise they are
	///   written as integers (see <see cref="SyncTimeAsTicks{SM}"/>). When reading, the
	///   type of the field in the data stream (if available) determines which
	///   representation is parsed, so either representation can be read back.</summary>
	public struct SyncTime<SM> :
		ISyncField<SM, DateTime>, ISyncField<SM, DateTime?>,
		ISyncField<SM, DateTimeOffset>, ISyncField<SM, DateTimeOffset?>,
		ISyncField<SM, TimeSpan>, ISyncField<SM, TimeSpan?>
		where SM : ISyncManager
	{
		// RoundtripKind ensures that the Kind of a DateTime survives a round trip
		// (its three Kinds are written in three different ways; see NewtonsoftDateFormat)
		static SyncTimeAsString<SM> AsString => new SyncTimeAsString<SM>(null, DateTimeStyles.RoundtripKind);
		static SyncTimeAsTicks<SM> AsTicks => new SyncTimeAsTicks<SM>();

		public DateTime Sync(ref SM sync, FieldId name, DateTime value)
			=> ShouldUseStringFormat(ref sync, name)
				? AsString.Sync(ref sync, name, value)
				: AsTicks.Sync(ref sync, name, value);

		public DateTime? Sync(ref SM sync, FieldId name, DateTime? value)
			=> ShouldUseStringFormat(ref sync, name)
				? AsString.Sync(ref sync, name, value)
				: AsTicks.Sync(ref sync, name, value);

		public DateTimeOffset Sync(ref SM sync, FieldId name, DateTimeOffset value)
			=> ShouldUseStringFormat(ref sync, name)
				? AsString.Sync(ref sync, name, value)
				: AsTicks.Sync(ref sync, name, value);

		public DateTimeOffset? Sync(ref SM sync, FieldId name, DateTimeOffset? value)
			=> ShouldUseStringFormat(ref sync, name)
				? AsString.Sync(ref sync, name, value)
				: AsTicks.Sync(ref sync, name, value);

		public TimeSpan Sync(ref SM sync, FieldId name, TimeSpan value)
			=> ShouldUseStringFormat(ref sync, name)
				? AsString.Sync(ref sync, name, value)
				: AsTicks.Sync(ref sync, name, value);

		public TimeSpan? Sync(ref SM sync, FieldId name, TimeSpan? value)
			=> ShouldUseStringFormat(ref sync, name)
				? AsString.Sync(ref sync, name, value)
				: AsTicks.Sync(ref sync, name, value);

		/// <summary>Decides whether a date/time field should be synchronized as a string:
		///   strings are used in plain-text formats, integers (or a tuple, for
		///   DateTimeOffset) in binary formats.
		///   <para/>
		///   When reading a plain-text format the actual representation in the data
		///   stream takes precedence, so that the compact form is readable from a
		///   plain-text format too: a field stored as an Integer/Float (DateTime and
		///   TimeSpan) or as a two-element tuple, i.e. a List (DateTimeOffset), is read
		///   via <see cref="SyncTimeAsTicks{SM}"/>, while anything else — a string, a
		///   null, or a missing field — is read via <see cref="SyncTimeAsString{SM}"/>
		///   (which, unlike the tuple decoder, handles null correctly). We can't simply
		///   test for <see cref="SyncType.String"/> because that misses null.
		///   <para/>
		///   In Writing and Schema mode there is no data to inspect (GetFieldType returns
		///   Unknown), so the mode is checked explicitly to make the output — and the
		///   schema — match the string form a plain-text writer produces.</summary>
		internal static bool ShouldUseStringFormat<SM>(ref SM sync, FieldId name) where SM : ISyncManager
		{
			if (!sync.IsPlainText)
				return false;
			if (sync.Mode is SyncMode.Writing or SyncMode.Schema)
				return true;
			var type = sync.GetFieldType(name);
			return type != SyncType.Integer && type != SyncType.Float && type != SyncType.List;
		}
	}

	/// <summary>Synchronizes DateTime, DateTimeOffset and TimeSpan values as strings.
	///   By default the formats match Newtonsoft.Json: ISO-8601 for DateTime — written
	///   in one of three forms according to its <see cref="DateTimeKind"/> (see
	///   <see cref="SyncDateTimeHelper.NewtonsoftDateFormat"/>) — and for DateTimeOffset,
	///   and constant ("c") format for TimeSpan. A different format can be chosen via
	///   the constructor. All strings are written and parsed with the invariant
	///   culture.</summary>
	public struct SyncTimeAsString<SM> :
		ISyncField<SM, DateTime>, ISyncField<SM, DateTime?>,
		ISyncField<SM, DateTimeOffset>, ISyncField<SM, DateTimeOffset?>,
		ISyncField<SM, TimeSpan>, ISyncField<SM, TimeSpan?>
		where SM : ISyncManager
	{
		/// <summary>The format in which Newtonsoft.Json writes DateTime and DateTimeOffset.
		///   The trailing "K" causes a DateTime to be written in one of three ways
		///   according to its <see cref="DateTimeKind"/>: with a "Z" suffix (Utc), with a
		///   UTC-offset suffix such as "+05:00" (Local), or with no suffix (Unspecified).
		///   For DateTimeOffset, "K" always writes the offset. ".FFFFFFF" writes up to
		///   seven digits of fractional seconds but, like Newtonsoft, omits trailing
		///   zeros (including the "." itself when the fraction is zero).</summary>
		internal const string NewtonsoftDateFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";

		string? _preferredFormat;
		DateTimeStyles _parseMode;

		/// <param name="preferredFormat">The format used for writing, which is also tried
		///   first when parsing (parsing falls back on the standard Parse method of the
		///   value's type). If this is null, DateTime and DateTimeOffset use
		///   <see cref="NewtonsoftDateFormat"/> and TimeSpan uses
		///   constant ("c") format.</param>
		/// <param name="parseMode">Style flags used when parsing DateTime and
		///   DateTimeOffset (e.g. <see cref="DateTimeStyles.RoundtripKind"/> preserves
		///   the <see cref="DateTimeKind"/> of a DateTime).</param>
		public SyncTimeAsString(string? preferredFormat = null, DateTimeStyles parseMode = DateTimeStyles.AllowWhiteSpaces)
		{
			_preferredFormat = preferredFormat;
			_parseMode = parseMode;
		}

		public string ToString(DateTime value)
			=> value.ToString(_preferredFormat ?? NewtonsoftDateFormat, CultureInfo.InvariantCulture);
		public string ToString(DateTimeOffset value)
			=> value.ToString(_preferredFormat ?? NewtonsoftDateFormat, CultureInfo.InvariantCulture);
		public string ToString(TimeSpan value)
			=> value.ToString(_preferredFormat ?? "c", CultureInfo.InvariantCulture);

		public DateTime? ToDateTime(string? str)
		{
			if (str == null)
				return null;
			if (_preferredFormat != null && DateTime.TryParseExact(str, _preferredFormat, CultureInfo.InvariantCulture, _parseMode, out var date))
				return date;
			return DateTime.Parse(str, CultureInfo.InvariantCulture, _parseMode);
		}
		public DateTimeOffset? ToDateTimeOffset(string? str)
		{
			if (str == null)
				return null;
			if (_preferredFormat != null && DateTimeOffset.TryParseExact(str, _preferredFormat, CultureInfo.InvariantCulture, _parseMode, out var date))
				return date;
			return DateTimeOffset.Parse(str, CultureInfo.InvariantCulture, _parseMode);
		}
		public TimeSpan? ToTimeSpan(string? str)
		{
			if (str == null)
				return null;
			if (_preferredFormat != null && TimeSpan.TryParseExact(str, _preferredFormat, CultureInfo.InvariantCulture, out var time))
				return time;
			return TimeSpan.Parse(str, CultureInfo.InvariantCulture);
		}

		public DateTime Sync(ref SM sync, FieldId name, DateTime value)
		{
			string? str = sync.Sync(name, sync.IsWriting ? ToString(value) : null);
			if (!sync.IsReading)
				return value;
			if (G.Var(out var r, ToDateTime(str)) is null)
				ThrowUnexpectedNull(name);
			return r.Value;
		}
		public DateTime? Sync(ref SM sync, FieldId name, DateTime? value)
		{
			string? str = sync.Sync(name, sync.IsWriting && value != null ? ToString(value.Value) : null);
			return sync.IsReading ? ToDateTime(str) : value;
		}

		public DateTimeOffset Sync(ref SM sync, FieldId name, DateTimeOffset value)
		{
			string? str = sync.Sync(name, sync.IsWriting ? ToString(value) : null);
			if (!sync.IsReading)
				return value;
			if (G.Var(out var r, ToDateTimeOffset(str)) is null)
				ThrowUnexpectedNull(name);
			return r.Value;
		}
		public DateTimeOffset? Sync(ref SM sync, FieldId name, DateTimeOffset? value)
		{
			string? str = sync.Sync(name, sync.IsWriting && value != null ? ToString(value.Value) : null);
			return sync.IsReading ? ToDateTimeOffset(str) : value;
		}

		public TimeSpan Sync(ref SM sync, FieldId name, TimeSpan value)
		{
			string? str = sync.Sync(name, sync.IsWriting ? ToString(value) : null);
			if (!sync.IsReading)
				return value;
			if (G.Var(out var r, ToTimeSpan(str)) is null)
				ThrowUnexpectedNull(name);
			return r.Value;
		}
		public TimeSpan? Sync(ref SM sync, FieldId name, TimeSpan? value)
		{
			string? str = sync.Sync(name, sync.IsWriting && value != null ? ToString(value.Value) : null);
			return sync.IsReading ? ToTimeSpan(str) : value;
		}

		internal static void ThrowUnexpectedNull(FieldId name)
			=> throw new FormatException("'{0}' was unexpectedly null".Localized(name.Name));
	}

	/// <summary>Synchronizes date/time values as integers: DateTime as the 64-bit value
	///   of <see cref="DateTime.ToBinary"/> (which equals Ticks when the
	///   <see cref="DateTimeKind"/> is Unspecified, and preserves the Kind otherwise),
	///   TimeSpan as <see cref="TimeSpan.Ticks"/>, and DateTimeOffset — the one type
	///   here that does not fit in 64 bits — as a tuple of two integers: Ticks, and the
	///   UTC offset in minutes.</summary>
	public struct SyncTimeAsTicks<SM> :
		ISyncField<SM, DateTime>, ISyncField<SM, DateTime?>,
		ISyncField<SM, DateTimeOffset>, ISyncField<SM, DateTimeOffset?>,
		ISyncField<SM, TimeSpan>, ISyncField<SM, TimeSpan?>
		where SM : ISyncManager
	{
		public DateTime Sync(ref SM sync, FieldId name, DateTime value)
		{
			long num = sync.Sync(name, sync.IsWriting ? value.ToBinary() : 0L);
			return sync.IsReading ? DateTime.FromBinary(num) : value;
		}
		public DateTime? Sync(ref SM sync, FieldId name, DateTime? value)
		{
			long? num = sync.Sync(name, sync.IsWriting && value != null ? value.Value.ToBinary() : (long?)null);
			if (!sync.IsReading)
				return value;
			return num == null ? (DateTime?)null : DateTime.FromBinary(num.Value);
		}

		const int TicksPerMinute = 60_000_000_0;

		public DateTimeOffset Sync(ref SM sm, FieldId name, DateTimeOffset value)
		{
			var (begun, _, obj) = sm.BeginSubObject(name, null, ObjectMode.NotNull | ObjectMode.Tuple, 2);
			if (begun)
				return FinishDTOSync(sm, value);
			else
				return obj is DateTimeOffset dt ? dt : default;
		}
		public DateTimeOffset? Sync(ref SM sm, FieldId name, DateTimeOffset? value)
		{
			// Omit NotNull when the value is null so that BeginSubObject writes null
			// (and, when reading, accepts null) instead of forcing a tuple.
			var mode = value is null ? ObjectMode.Tuple : ObjectMode.Tuple | ObjectMode.NotNull;
			var (begun, _, obj) = sm.BeginSubObject(name, null, mode, 2);
			if (begun)
				return FinishDTOSync(sm, value ?? default);
			else
				return obj is DateTimeOffset dt ? dt : null;
		}
		private static DateTimeOffset FinishDTOSync(SM sm, DateTimeOffset value)
		{
			var item1 = sm.Sync(null, value.Ticks);
			var item2 = sm.Sync(null, (int)(value.Offset.Ticks / TicksPerMinute));
			sm.EndSubObject();
			return new DateTimeOffset(item1, TimeSpan.FromMinutes(item2));
		}

		public TimeSpan Sync(ref SM sync, FieldId name, TimeSpan value)
		{
			return TimeSpan.FromTicks(sync.Sync(name, value.Ticks));
		}
		public TimeSpan? Sync(ref SM sync, FieldId name, TimeSpan? value)
		{
			long? ticks = sync.Sync(name, value?.Ticks);
			return ticks is null ? (TimeSpan?)null : TimeSpan.FromTicks(ticks.Value);
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
