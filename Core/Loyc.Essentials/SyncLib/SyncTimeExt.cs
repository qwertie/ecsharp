using Loyc.SyncLib.Impl;
using System;
using System.Globalization;

#nullable enable

namespace Loyc.SyncLib
{
	/// <summary>Extension methods of <see cref="ISyncManager"/> for synchronizing the
	///   date/time types <see cref="DateTime"/>, <see cref="DateTimeOffset"/> and
	///   <see cref="TimeSpan"/> (plus DateOnly and TimeOnly on .NET 6+, but no target
	///   framework of Loyc.Essentials is currently new enough to include them).</summary>
	/// <remarks>
	///   The <c>Sync</c> methods choose a representation automatically (see
	///   <see cref="SyncTime{SM}"/>):
	///   <ul>
	///   <li>In plain-text formats (<see cref="ISyncManager.IsPlainText"/>), values are
	///     written as strings in the same formats Newtonsoft.Json uses: ISO-8601 for
	///     DateTime — in one of three forms according to its <see cref="DateTimeKind"/>
	///     — and DateTimeOffset, and constant ("c") format for TimeSpan.</li>
	///   <li>In binary formats, values that fit in 64 bits are stored as integers:
	///     DateTime uses <see cref="DateTime.ToBinary"/> (which preserves the
	///     <see cref="DateTimeKind"/>) and TimeSpan uses <see cref="TimeSpan.Ticks"/>.
	///     A DateTimeOffset does not fit in 64 bits, so it is stored as a tuple of two
	///     integers: the Ticks of the local date/time, and the UTC offset in minutes.</li>
	///   <li>When reading a plain-text format, the actual type of the field decides:
	///     a string is parsed as above, while an integer (or tuple) is decoded as in a
	///     binary format. This works because <see cref="ISyncManager.GetFieldType"/>
	///     returns the type of the field when the reader can determine it.</li>
	///   </ul>
	///   The other methods (SyncDateAsString, SyncTimeAsTicks, SyncTimeAsSeconds, etc.)
	///   each store values in one specific way, regardless of the data format.
	/// </remarks>
	public static partial class SyncTimeExt
	{
		public static DateTime Sync<SM>(this SM sync, FieldId name, DateTime value) where SM : ISyncManager
			=> new SyncTime<SM>().Sync(ref sync, name, value);
		public static DateTime? Sync<SM>(this SM sync, FieldId name, DateTime? value) where SM : ISyncManager
			=> new SyncTime<SM>().Sync(ref sync, name, value);

		public static DateTimeOffset Sync<SM>(this SM sync, FieldId name, DateTimeOffset value) where SM : ISyncManager
			=> new SyncTime<SM>().Sync(ref sync, name, value);
		public static DateTimeOffset? Sync<SM>(this SM sync, FieldId name, DateTimeOffset? value) where SM : ISyncManager
			=> new SyncTime<SM>().Sync(ref sync, name, value);

		public static TimeSpan Sync<SM>(this SM sync, FieldId name, TimeSpan value) where SM : ISyncManager
			=> new SyncTime<SM>().Sync(ref sync, name, value);
		public static TimeSpan? Sync<SM>(this SM sync, FieldId name, TimeSpan? value) where SM : ISyncManager
			=> new SyncTime<SM>().Sync(ref sync, name, value);


		public static DateTime SyncAsString<SM>(this SM sync, FieldId name, DateTime value,
			string? preferredFormat = null, DateTimeStyles parseMode = DateTimeStyles.AllowWhiteSpaces)
			where SM : ISyncManager
			=> new SyncTimeAsString<SM>(preferredFormat, parseMode).Sync(ref sync, name, value);
		public static DateTime? SyncAsString<SM>(this SM sync, FieldId name, DateTime? value,
			string? preferredFormat = null, DateTimeStyles parseMode = DateTimeStyles.AllowWhiteSpaces)
			where SM : ISyncManager
			=> new SyncTimeAsString<SM>(preferredFormat, parseMode).Sync(ref sync, name, value);

		public static DateTimeOffset SyncAsString<SM>(this SM sync, FieldId name, DateTimeOffset value,
			string? preferredFormat = null, DateTimeStyles parseMode = DateTimeStyles.AllowWhiteSpaces)
			where SM : ISyncManager
			=> new SyncTimeAsString<SM>(preferredFormat, parseMode).Sync(ref sync, name, value);
		public static DateTimeOffset? SyncAsString<SM>(this SM sync, FieldId name, DateTimeOffset? value,
			string? preferredFormat = null, DateTimeStyles parseMode = DateTimeStyles.AllowWhiteSpaces)
			where SM : ISyncManager
			=> new SyncTimeAsString<SM>(preferredFormat, parseMode).Sync(ref sync, name, value);

		public static TimeSpan SyncAsString<SM>(this SM sync, FieldId name, TimeSpan value, string? preferredFormat = null) where SM : ISyncManager
			=> new SyncTimeAsString<SM>(preferredFormat).Sync(ref sync, name, value);
		public static TimeSpan? SyncAsString<SM>(this SM sync, FieldId name, TimeSpan? value, string? preferredFormat = null) where SM : ISyncManager
			=> new SyncTimeAsString<SM>(preferredFormat).Sync(ref sync, name, value);


		public static DateTime SyncAsTicks<SM>(this SM sync, FieldId name, DateTime value) where SM : ISyncManager
			=> new SyncTimeAsTicks<SM>().Sync(ref sync, name, value);
		public static DateTime? SyncAsTicks<SM>(this SM sync, FieldId name, DateTime? value) where SM : ISyncManager
			=> new SyncTimeAsTicks<SM>().Sync(ref sync, name, value);
		public static DateTimeOffset SyncAsTicks<SM>(this SM sync, FieldId name, DateTimeOffset value) where SM : ISyncManager
			=> new SyncTimeAsTicks<SM>().Sync(ref sync, name, value);
		public static DateTimeOffset? SyncAsTicks<SM>(this SM sync, FieldId name, DateTimeOffset? value) where SM : ISyncManager
			=> new SyncTimeAsTicks<SM>().Sync(ref sync, name, value);
		public static TimeSpan SyncAsTicks<SM>(this SM sync, FieldId name, TimeSpan value) where SM : ISyncManager
			=> new SyncTimeAsTicks<SM>().Sync(ref sync, name, value);
		public static TimeSpan? SyncAsTicks<SM>(this SM sync, FieldId name, TimeSpan? value) where SM : ISyncManager
			=> new SyncTimeAsTicks<SM>().Sync(ref sync, name, value);


		public static DateTime SyncAsDayNumber<SM>(this SM sync, FieldId name, DateTime value, bool asInt32 = false) where SM : ISyncManager
			=> new SyncDateAsDayNumber<SM>(asInt32).Sync(ref sync, name, value);
		public static DateTime? SyncAsDayNumber<SM>(this SM sync, FieldId name, DateTime? value, bool asInt32 = false) where SM : ISyncManager
			=> new SyncDateAsDayNumber<SM>(asInt32).Sync(ref sync, name, value);
	}
}
