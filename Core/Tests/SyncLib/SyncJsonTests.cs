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
				_saveMode = ObjectMode.Deduplicate | ObjectMode.FixedSizeNumbers;
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
					AdvanceReminder = M(30), Color = Color.Gray
				},
				new CalendarEntry(calendar) {
					StartTime = T(11,30), Duration = M(30), Description = "Sales meeting",
					AdvanceReminder = M(5)
				},
				new CalendarEntry(calendar) {
					StartTime = T(13,00), Description = "Doctor appointment",
					AdvanceReminder = M(25), Color = Color.Red
				},
				new CalendarEntry(calendar) {
					StartTime = T(22,00), Duration = M(15), Description = "Brush teeth"
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
			Assert.AreEqual(a.DefaultColor, b.DefaultColor);
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
			Assert.AreEqual(a.Color, b.Color);
		}
	}
}
