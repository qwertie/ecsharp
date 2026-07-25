using Loyc.MiniTest;
using Loyc.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.SyncLib.Tests
{
	public class SyncJsonTests : SyncLibTests<SyncJson.Reader, SyncJson.Writer>
	{
		SyncJson.Options _options = new SyncJson.Options();
		ObjectMode _saveMode;

		public SyncJsonTests(bool newtonCompat, bool nonDefaultSettings = false, bool minify = false)
		{
			if (nonDefaultSettings) {
				_options = new SyncJson.Options {
					NameConverter = SyncJson.ToCamelCase,
					Write = {
						EscapeUnicode = true,
						MaxIndentDepth = 2,
						CharListAsString = false,
						SpaceAfterColon = false,
						Indent = "  ",
						Newline = "\n",
						InitialBufferSize = 1,
					},
					Read = {
						Strict = true,
						AllowComments = false,
						VerifyEof = false,
					}
				};
				_saveMode = ObjectMode.Deduplicate;
			}
			_options.NewtonsoftCompatibility = newtonCompat;
			_options.Write.Minify = minify;
		}

		protected override T Read<T>(byte[] data, SyncObjectFunc<SyncJson.Reader, T> sync)
		{
			_options.RootMode = _saveMode;
			// mysteriously, changing the return value to T? creates a compiler error, so use `!`
			return SyncJson.Read<T>(data, sync, _options)!; 
		}

		protected override byte[] Write<T>(T value, SyncObjectFunc<SyncJson.Writer, T> sync, ObjectMode mode) {
			_options.RootMode = mode;
			return SyncJson.Write(value, sync, _options).ToArray();
		}

		[Test] public void HomePageCalendarTestV1() => HomePageCalendarTest(1);
		[Test] public void HomePageCalendarTestV2() => HomePageCalendarTest(2);
		public void HomePageCalendarTest(int apiVersion)
		{
			// Create an example calendar
			var calendar = new Calendar { DefaultColor = Color.Black, Id = 123, UserId = 321 };

			foreach (var entry in new[] {
				new CalendarEntry(calendar) { 
					StartTime = T(9,00), Duration = M(8*60), Description = "Workday!",
					AdvanceReminder = M(30), Color = Color.Gray, Id = 10
				},
				new CalendarEntry(calendar) {
					StartTime = T(11,30), Duration = M(30), Description = "Sales meeting",
					AdvanceReminder = M(5), Color = Color.Black, Id = 11
				},
				new CalendarEntry(calendar) {
					StartTime = T(13,00), Description = "Doctor appointment",
					AdvanceReminder = M(25), Color = Color.Red, Id = 12
				},
				new CalendarEntry(calendar) {
					StartTime = T(22,00), Duration = M(15), Description = "Brush teeth",
					Color = Color.Black, Id = 13
				}
			}) {
				calendar.Entries[entry.StartTime].Add(entry);
			}

			var newtonSync = new JsonCalendarSerialization { ApiVersion = apiVersion, CalendarId = calendar.Id };
			var synclibSync = new CalendarSync             { ApiVersion = apiVersion, CalendarId = calendar.Id };

			// Adjust SyncLib's output formatting slightly to match Newtonsoft
			synclibSync.Options.Write.Indent = "  ";
			synclibSync.Options.Write.SpaceAfterColon = true;

			string newtonJson = newtonSync.Serialize(calendar);
			string syncJson = synclibSync.Serialize(calendar);

			Assert.AreEqual(SyncJsonWriterTests.RestyleNewtonJson(newtonJson), syncJson);

			// By default, SyncLib can deserialize Newtonsoft output and vice versa
			Calendar calendarN = newtonSync.Deserialize(syncJson)!;
			Calendar calendarS = synclibSync.Deserialize(newtonJson)!;

			if (apiVersion >= 2) {
				CheckEqual(calendar, calendarN);
				CheckEqual(calendar, calendarS);
			}
			CheckEqual(calendarN, calendarS);
			
			static TimeSpan M(int minutes) 
				=> TimeSpan.FromMinutes(minutes);
			static DateTime T(int hour, int minute)
				=> DateTime.Today.AddHours(hour).AddMinutes(minute);
		}

		static void CheckEqual(Calendar a, Calendar b)
		{
			Assert.AreEqual(a.Id, b.Id);
			Assert.AreEqual(a.UserId, b.UserId);
			Assert.AreEqual(a.DefaultColor.ToArgb(), b.DefaultColor.ToArgb());
			Assert.AreEqual(a.Entries.Count, b.Entries.Count);
			
			BList<KeyValuePair<DateTime, CalendarEntry>> aEntries = a.Entries;
			BList<KeyValuePair<DateTime, CalendarEntry>> bEntries = b.Entries;
			for (int i = 0; i < a.Entries.Count; i++) {
				Assert.AreEqual(aEntries[i].Key, bEntries[i].Key);
				CheckEqual(aEntries[i].Value, bEntries[i].Value);
			}
		}
		static void CheckEqual(CalendarEntry a, CalendarEntry b)
		{
			Assert.AreEqual(a.Calendar?.Id, b.Calendar?.Id);
			Assert.AreEqual(a.CalendarId, b.CalendarId);
			Assert.AreEqual(a.Id, b.Id);
			Assert.AreEqual(a.Description, b.Description);
			Assert.AreEqual(a.StartTime, b.StartTime);
			Assert.AreEqual(a.Duration, b.Duration);
			Assert.AreEqual(a.Location, b.Location);
			Assert.AreEqual(a.AdvanceReminder, b.AdvanceReminder);
			Assert.AreEqual(a.Color.ToArgb(), b.Color.ToArgb());
		}

		/// <summary>Verifies that the Sync extension methods in <see cref="SyncTimeExt"/>
		///   write Newtonsoft-style strings in JSON (a plain-text format), and that either 
		///   representation, the string form or the compact integer/tuple form used by 
		///   binary formats, can be read back regardless of the reader's options, because 
		///   the reader detects the type of the field via GetFieldType.</summary>
		[Test]
		public void AdaptiveDateSyncUsesExpectedJsonFormat()
		{
			var utc = new DateTime(2026, 6, 12, 13, 45, 59, DateTimeKind.Utc);
			var unspecified = new DateTime(2026, 6, 12, 13, 45, 59, 500, DateTimeKind.Unspecified);
			var local = new DateTime(2026, 6, 12, 13, 45, 59, DateTimeKind.Local);
			var dto = new DateTimeOffset(2026, 6, 12, 13, 45, 59, TimeSpan.FromHours(-8));
			var span = TimeSpan.FromSeconds(90.5);

			// he adaptive Sync methods write Newtonsoft-style strings in plain-text formats.
			// A DateTime is written in one of three formats depending on its Kind: "Z" suffix
			// (Utc), local UTC-offset suffix (Local), or no suffix (Unspecified).
			// Fractional seconds appear only when nonzero.
			Assert.IsTrue(WriteToString(utc, SyncDT<SyncJson.Writer>).Contains("\"2026-06-12T13:45:59Z\""));
			Assert.IsTrue(WriteToString(unspecified, SyncDT<SyncJson.Writer>).Contains("\"2026-06-12T13:45:59.5\""));
			string localJson = WriteToString(local, SyncDT<SyncJson.Writer>);
			string localOffset = local.ToString("zzz"); // e.g. "-06:00" (depends on machine)
			Assert.IsTrue(localJson.Contains("\"2026-06-12T13:45:59" + localOffset + "\""), localJson);

			string dtoJson = WriteToString(dto, SyncDTO<SyncJson.Writer>);
			Assert.IsTrue(dtoJson.Contains("\"2026-06-12T13:45:59-08:00\""), dtoJson);
			string spanJson = WriteToString(span, SyncTS<SyncJson.Writer>);
			Assert.IsTrue(spanJson.Contains("\"00:01:30.5000000\""), spanJson);

			// The strings round-trip, and the DateTimeKind survives:
			foreach (var date in new[] { utc, unspecified, local }) {
				var readBack = Read<DateTime>(Write(date, SyncDT<SyncJson.Writer>, ObjectMode.Normal), SyncDT<SyncJson.Reader>);
				Assert.AreEqual(date, readBack);
				Assert.AreEqual(date.Kind, readBack.Kind);
			}
			Assert.AreEqual(dto, Read<DateTimeOffset>(Write(dto, SyncDTO<SyncJson.Writer>, ObjectMode.Normal), SyncDTO<SyncJson.Reader>));
			Assert.AreEqual(span, Read<TimeSpan>(Write(span, SyncTS<SyncJson.Writer>, ObjectMode.Normal), SyncTS<SyncJson.Reader>));

			// The compact representations used by binary formats (see SyncTimeAsTicks)
			// can also be read from JSON, regardless of the reader's options, because
			// the reader checks the type of each field in the data stream:
			foreach (bool newtonCompat in new[] { false, true }) {
				var options = new SyncJson.Options { NewtonsoftCompatibility = newtonCompat };
				var readBack = SyncJson.Read<DateTime>(
					Encoding.UTF8.GetBytes("{\"dt\": " + utc.ToBinary() + "}"), (sm, v) => sm.Sync("dt", v), options);
				Assert.AreEqual(utc, readBack);
				Assert.AreEqual(utc.Kind, readBack.Kind);
				Assert.AreEqual(dto, SyncJson.Read<DateTimeOffset>(
					Encoding.UTF8.GetBytes("{\"dt\": [" + dto.Ticks + ", " + (int)dto.Offset.TotalMinutes + "]}"),
					SyncDTO<SyncJson.Reader>, options));
				Assert.AreEqual(span, SyncJson.Read<TimeSpan>(
					Encoding.UTF8.GetBytes("{\"dt\": " + span.Ticks + "}"), SyncTS<SyncJson.Reader>, options));

				// ...and so can the strings, whether or not NewtonsoftCompatibility is on:
				Assert.AreEqual(dto, SyncJson.Read<DateTimeOffset>(
					Encoding.UTF8.GetBytes(dtoJson), SyncDTO<SyncJson.Reader>, options));
			}

			string WriteToString<T>(T value, SyncObjectFunc<SyncJson.Writer, T> sync)
				=> Encoding.UTF8.GetString(Write(value, sync, ObjectMode.Normal));

			static DateTime SyncDT<SM>(SM sm, DateTime v) where SM : ISyncManager => sm.Sync("dt", v);
			static DateTimeOffset SyncDTO<SM>(SM sm, DateTimeOffset v) where SM : ISyncManager => sm.Sync("dt", v);
			static TimeSpan SyncTS<SM>(SM sm, TimeSpan v) where SM : ISyncManager => sm.Sync("dt", v);
		}

		[Test]
		public void ToCamelCaseTest()
		{
			Assert.AreEqual("hiThere", SyncJson.ToCamelCase("HiThere"));
			Assert.AreEqual("_HiThere", SyncJson.ToCamelCase("_HiThere"));
			Assert.AreEqual("hello", SyncJson.ToCamelCase("hello"));
			Assert.AreEqual("hello", SyncJson.ToCamelCase("Hello"));
			Assert.AreEqual("hello", SyncJson.ToCamelCase("HELLO"));
			Assert.AreEqual("sqlQuery", SyncJson.ToCamelCase("SQLQuery"));
			Assert.AreEqual("_SQLQuery", SyncJson.ToCamelCase("_SQLQuery"));
		}
	}
}
