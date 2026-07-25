// Benchmark-only data generation and validation for the Calendar model in
// Core/Tests/SyncLib/HomePageCalendarExample.cs.
using System.Drawing;
using Loyc.Collections;
using Calendar = Loyc.SyncLib.Tests.Calendar;
using CalendarEntry = Loyc.SyncLib.Tests.CalendarEntry;

namespace Benchmark.Serialization
{
	/// <summary>BMultiMap implements more than one IEnumerable&lt;T&gt;, which the
	/// C# 14 compiler won't disambiguate for LINQ; this picks the key/value view.</summary>
	public static class BMultiMapExt
	{
		public static IReadOnlyCollection<KeyValuePair<K, V>> Pairs<K, V>(this BMultiMap<K, V> map) => map;
	}

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
