// The Calendar model and its SyncLib/Newtonsoft serialization code, taken from
// Core/Tests/SyncLib/HomePageCalendarExample.cs (the example planned for the home
// page). Two changes were made for benchmarking:
//   1. CalendarSync is generic over the sync manager type (CalendarSync<SM>), so the
//      same code can be benchmarked both through the ISyncManager interface (as on
//      the home page: CalendarSync<ISyncManager>) and through SyncLib's fast generic
//      path (e.g. CalendarSync<SyncJson.Writer>). The code is otherwise identical.
//   2. The System.Text.Json attributes on the DTOs (in CalendarDtos.cs) were added
//      so the traditional-DTO approach can also be measured with System.Text.Json.
using System.Drawing;
using Loyc.Collections;
using Loyc.SyncLib;

namespace Benchmark.Serialization
{
	public class Calendar
	{
		public int Id { get; set; }
		public int UserId { get; set; }
		public Color DefaultColor { get; set; } = Color.SeaGreen;

		// A sorted list of calendar appointments stored in a "multi-map" (a
		// dictionary that can have multiple values per key). The goal is to
		// serialize it as a simple list of CalendarEntry.
		public BMultiMap<DateTime, CalendarEntry> Entries { get; set; }
		 = new BMultiMap<DateTime, CalendarEntry>();
	}

	public class CalendarEntry
	{
		public CalendarEntry(Calendar? parent = null)
		{
			Calendar = parent;
			CalendarId = parent?.Id ?? 0;
		}

		public int Id { get; set; }
		public int CalendarId { get; set; }
		public Calendar? Calendar { get; set; }

		public string Description { get; set; } = "";

		// Date and time when the appointment starts
		public DateTime StartTime { get; set; }
		// Note: the first version of the API has EndTime instead of Duration
		public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(60);

		public string Location { get; set; } = "";
		public TimeSpan? AdvanceReminder { get; set; }
		public Color Color { get; set; }
	}

	/// <summary>BMultiMap implements more than one IEnumerable&lt;T&gt;, which the
	/// C# 14 compiler won't disambiguate for LINQ; this picks the key/value view.</summary>
	public static class BMultiMapExt
	{
		public static IReadOnlyCollection<KeyValuePair<K, V>> Pairs<K, V>(this BMultiMap<K, V> map) => map;
	}

	#region Serialization via SyncLib

	public class CalendarSync<SM> where SM : ISyncManager
	{
		// Note: The serialized form does not include the Calendar.Id but it's included
		//       in the Web API's URL. The web controller will save that Calendar Id here.
		public int CalendarId { get; set; }
		public int ApiVersion { get; set; } = 2;

		public Calendar Sync(SM sm, Calendar? calendar)
		{
			_calendar = calendar ??= new Calendar { Id = CalendarId };

			if (ApiVersion >= 2)
				calendar.DefaultColor = sm.Sync("DefColor", calendar.DefaultColor, new SyncColor<SM>());

			// Serialize (save) or deserialize (load). It's saved as a simple list of
			// entries, while in memory we have a more complex dictionary data structure.
			IReadOnlyCollection<CalendarEntry> entries = calendar.Entries.Pairs().Select(p => p.Value);
			var entriesOut = sm.SyncColl("Entries", entries, SyncEntry, ObjectMode.Normal)!;
			if (sm.IsReading) {
				calendar.Entries.Clear();
				foreach (var entry in entriesOut)
					calendar.Entries.Add(entry.StartTime, entry);
			}

			calendar.UserId = sm.Sync("UserId", calendar.UserId);

			return calendar;
		}

		private Calendar? _calendar;

		private CalendarEntry SyncEntry(SM sm, CalendarEntry? entry)
		{
			entry ??= new CalendarEntry { Id = CalendarId };

			if (ApiVersion >= 2) {
				entry.Duration = sm.SyncAsString("Duration", entry.Duration);
				entry.Color    = sm.Sync("Color", entry.Color, new SyncColor<SM>());
			}

			entry.Calendar  ??= _calendar;
			entry.CalendarId  = entry.Calendar!.Id;
			entry.Id          = sm.Sync("Id", entry.Id);
			entry.Description = sm.Sync("Description", entry.Description) ?? "";
			entry.StartTime   = sm.SyncAsString("StartTime", entry.StartTime);
			entry.Location    = sm.Sync("Location", entry.Location) ?? "";
			entry.AdvanceReminder = sm.SyncAsString("AdvanceReminder", entry.AdvanceReminder);

			if (ApiVersion <= 1) {
				// API version 1 has an EndTime field instead of a Duration field
				var end = sm.SyncAsString("EndTime", entry.StartTime.Add(entry.Duration));
				if (sm.IsReading)
					entry.Duration = end.Subtract(entry.StartTime);
			}

			return entry;
		}
	}

	// A custom synchronizer for Color values (it saves them in hex, e.g. "#446688")
	public struct SyncColor<SM> : ISyncField<SM, Color> where SM : ISyncManager
	{
		public Color Sync(ref SM sm, FieldId name, Color color)
		{
			var str = sm.Sync(name, ToString(color));
			if (str == null)
				throw new FormatException("Got null when a color was expected");
			return ToColor(str);
		}

		public static string ToString(Color c) => "#" + (c.ToArgb() & 0xFFFFF).ToString("X6");
		public static Color ToColor(string? s)
		{
			if (s == null || !s.StartsWith("#"))
				throw new FormatException("Expected a color (starting with '#')");
			return Color.FromArgb(Convert.ToInt32(s.Substring(1), 16));
		}
	}

	#endregion

	/// <summary>Deterministic (seeded) generator of realistic calendar data.</summary>
	public static class CalendarGenerator
	{
		public static Calendar Generate(int entryCount, int seed = 12345)
		{
			var random = new Random(seed);
			var words = SampleData.Words;
			string MakePhrase(int minWords, int maxWords)
			{
				int n = random.Next(minWords, maxWords + 1);
				return string.Join(" ", Enumerable.Range(0, n).Select(_ => words[random.Next(words.Length)]));
			}

			var calendar = new Calendar { Id = 501, UserId = 3576, DefaultColor = Color.SeaGreen };
			var start = new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc);
			for (int i = 0; i < entryCount; i++) {
				// Unique start times so round-tripping through the sorted multimap
				// preserves entry order and results can be compared field-by-field.
				start = start.AddMinutes(15 * random.Next(1, 96));
				calendar.Entries.Add(start, new CalendarEntry(calendar) {
					Id = 1000 + i,
					Description = MakePhrase(2, 8),
					StartTime = start,
					Duration = TimeSpan.FromMinutes(15 * random.Next(1, 9)),
					Location = random.Next(3) == 0 ? "" : MakePhrase(1, 3),
					AdvanceReminder = random.Next(3) == 0 ? TimeSpan.FromMinutes(5 * random.Next(1, 13)) : null,
					Color = Color.FromArgb(unchecked((int)0xFF000000) | random.Next(0x1000000)),
				});
			}
			return calendar;
		}

		/// <summary>Returns an error description if the two calendars differ, else null.</summary>
		public static string? Validate(Calendar expected, Calendar? actual)
		{
			if (actual == null)
				return "deserialized to null";
			if (expected.UserId != actual.UserId)
				return $"UserId {actual.UserId} ≠ {expected.UserId}";
			if (expected.DefaultColor.ToArgb() != actual.DefaultColor.ToArgb())
				return "DefaultColor differs";
			if (expected.Entries.Count != actual.Entries.Count)
				return $"entry count {actual.Entries.Count} ≠ {expected.Entries.Count}";
			var expectedEntries = expected.Entries.Pairs().Select(p => p.Value).ToList();
			var actualEntries = actual.Entries.Pairs().Select(p => p.Value).ToList();
			for (int i = 0; i < expectedEntries.Count; i++) {
				var (e, d) = (expectedEntries[i], actualEntries[i]);
				if (e.Description != d.Description)
					return $"Description mismatch on entry {e.Id}";
				// Compare in UTC: serializers legitimately differ in whether they return
				// Kind=Utc or a Local-converted time for the same instant.
				if (e.StartTime.ToUniversalTime() != d.StartTime.ToUniversalTime())
					return $"StartTime mismatch on entry {e.Id}";
				if (e.Duration != d.Duration)
					return $"Duration mismatch on entry {e.Id}";
				if (e.Location != d.Location)
					return $"Location mismatch on entry {e.Id}";
				if (e.AdvanceReminder != d.AdvanceReminder)
					return $"AdvanceReminder mismatch on entry {e.Id}";
				if (e.Color.ToArgb() != d.Color.ToArgb())
					return $"Color mismatch on entry {e.Id}";
			}
			return null;
		}
	}

	/// <summary>Realistic sample words for generated test data (from the embedded
	/// 2of12 word list that the old benchmark app already carried).</summary>
	public static class SampleData
	{
		public static readonly string[] Words = Benchmark.Resources.Resources.WordList
			.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
	}
}
