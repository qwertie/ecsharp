using Loyc.Collections;
using Loyc.SyncLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Loyc.SyncLib.Impl;
using System.IO;
using System.Linq;

namespace Loyc.SyncLib.Tests
{
	public class Calendar
	{
		public int Id { get; set; }
		public int UserId { get; set; }
		public Color DefaultColor { get; set; } = Color.SeaGreen;

		// Sorted table of calendar appointments (B+ tree from Loyc.Collections on NuGet)
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
		public DateTime StartTime { get; set; }
		public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(60);
		public string Location { get; set; } = "";
		public TimeSpan? AdvanceReminder { get; set; }
		public Color Color { get; set; }
	}

	#region Serialization via SyncLib

	public class CalendarSync
	{
		// Note: The serialized form does not include the Calendar.Id but it's included
		//       in the Web API's URL. The web controller will save that Calendar Id here.
		public int CalendarId { get; set; }
		public int ApiVersion { get; set; } = 2;
		
		public SyncJson.Options Options = new SyncJson.Options { NameConverter = SyncJson.ToCamelCase };

		public string Serialize(Calendar calendar) => SyncJson.WriteStringI(calendar, Sync, Options);
		public Calendar? Deserialize(string json) => SyncJson.ReadI<Calendar>(json, Sync, Options);

		public Calendar Sync(ISyncManager sm, Calendar? calendar)
		{
			_calendar = calendar ??= new Calendar { Id = CalendarId };

			calendar.UserId = sm.Sync("UserId", calendar.UserId);

			IReadOnlyCollection<CalendarEntry> entries = calendar.Entries.Select(p => p.Value);
			var entriesOut = sm.SyncColl("Entries", entries, SyncEntry, SubObjectMode.Normal)!;
			if (!sm.IsSaving) {
				calendar.Entries.Clear();
				foreach (var entry in entriesOut)
					calendar.Entries.Add(entry.StartTime, entry);
			}

			if (ApiVersion >= 2)
				calendar.DefaultColor = sm.Sync("DefColor", calendar.DefaultColor, new SyncColor<ISyncManager>());

			return calendar;
		}

		private Calendar _calendar;

		private CalendarEntry SyncEntry(ISyncManager sm, CalendarEntry? entry)
		{
			entry ??= new CalendarEntry { Id = CalendarId };

			entry.Calendar  ??= _calendar;
			entry.CalendarId  = entry.Calendar.Id;
			entry.Id          = sm.Sync("Id", entry.Id);
			entry.Description = sm.Sync("Description", entry.Description) ?? "";
			entry.StartTime   = sm.SyncDateAsString("StartTime", entry.StartTime);
			entry.Duration    = sm.SyncTimeAsString("Duration", entry.Duration);
			entry.Location    = sm.Sync("Location", entry.Location) ?? "";
			entry.AdvanceReminder = sm.SyncTimeAsString("AdvanceReminder", entry.AdvanceReminder);

			if (ApiVersion >= 2)
				entry.Color  = sm.Sync("Color", entry.Color, new SyncColor<ISyncManager>());

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
		public static Color ToColor(string s)
		{
			if (!s.StartsWith("#"))
				throw new FormatException("Expected a color (starting with '#')");
			return Color.FromArgb(Convert.ToInt32(s.Substring(1)));
		}
	}

	#endregion

	#region Traditional serialization code via Newtonsoft

	public class JsonCalendar
	{
		public int UserId { get; set; }
		public IEnumerable<JsonCalendarEntry?>? Entries { get; set; }
	}

	public class JsonCalendarV2 : JsonCalendar
	{
		public Color DefColor { get; set; }
	}

	public class JsonCalendarEntry
	{
		public int Id { get; set; }
		public string? Description { get; set; }
		public DateTime StartTime { get; set; }
		public TimeSpan Duration { get; set; }
		public string? Location { get; set; }
		public TimeSpan? AdvanceReminder { get; set; }
	}

	public class JsonCalendarEntryV2 : JsonCalendarEntry
	{
		public Color Color { get; set; }
	}

	public class JsonCalendarSerialization
	{
		// Note: The serialized form does not include the Calendar.Id but it's included
		//       in the Web API's URL. The web controller will save that Calendar Id here.
		public int CalendarId { get; set; }
		public int ApiVersion { get; set; } = 2;

		private JsonSerializer _serializer = new JsonSerializer {
			Formatting = Formatting.Indented,
			PreserveReferencesHandling = PreserveReferencesHandling.None,
			ContractResolver = new DefaultContractResolver()
			{
				NamingStrategy = new CamelCaseNamingStrategy()
			}
		};

		public string Serialize(Calendar calendar)
		{
			var sb = new StringBuilder();
			using (var writer = new StringWriter(sb))
				_serializer.Serialize(writer, ToJsonCalendar(calendar));

			return sb.ToString();
		}

		public Calendar? Deserialize(string json)
		{
			var expectedType = ApiVersion >= 2 ? typeof(JsonCalendarV2) : typeof(JsonCalendar);
			var calendar = _serializer.Deserialize(new StringReader(json), expectedType);

			return calendar == null ? null : FromJsonCalendar((JsonCalendar) calendar);
		}

		#region Code to convert Calendar to JsonCalendar for serialization

		public JsonCalendar ToJsonCalendar(Calendar calendar)
		{
			var jsonEntries = calendar.Entries.Select(pair => ToJsonCalendarEntry(pair.Value));
			
			if (ApiVersion <= 1) {
				return new JsonCalendar {
					UserId = calendar.UserId,
					Entries = jsonEntries,
				};
			} else {
				return new JsonCalendarV2 {
					UserId = calendar.UserId,
					Entries = jsonEntries,
					DefColor = calendar.DefaultColor,
				};
			}
		}

		private JsonCalendarEntry ToJsonCalendarEntry(CalendarEntry entry)
		{
			JsonCalendarEntry jsonEntry;
			if (ApiVersion <= 1) {
				jsonEntry = new JsonCalendarEntry();
			} else {
				jsonEntry = new JsonCalendarEntryV2 {
					Color = entry.Color
				};
			}

			jsonEntry.Id = entry.Id;
			jsonEntry.Description = entry.Description;
			jsonEntry.StartTime = entry.StartTime;
			jsonEntry.Duration = entry.Duration;
			jsonEntry.Location = entry.Location;
			jsonEntry.AdvanceReminder = entry.AdvanceReminder;

			return jsonEntry;
		}

		#endregion

		#region Code to convert JsonCalendar to Calendar after deserialization

		private Calendar FromJsonCalendar(JsonCalendar jsonCalendar)
		{
			_calendar = new Calendar() {
				Id = this.CalendarId,
				UserId = jsonCalendar.UserId,
				Entries = new BMultiMap<DateTime, CalendarEntry>()
			};
			
			if (jsonCalendar.Entries == null)
				throw new FormatException("Missing calendar entries");
			
			foreach (var entry in jsonCalendar.Entries)
				_calendar.Entries[entry!.StartTime].Add(FromJsonCalendarEntry(entry!));
			
			if (jsonCalendar is JsonCalendarV2 v2) {
				_calendar.DefaultColor = v2.DefColor;
			}

			return _calendar;
		}

		private Calendar? _calendar;

		private CalendarEntry FromJsonCalendarEntry(JsonCalendarEntry jsonEntry)
		{
			var entry = new CalendarEntry();

			entry.Calendar = _calendar;
			entry.CalendarId = _calendar!.Id;
			entry.Id = jsonEntry.Id;
			entry.Description = jsonEntry.Description ?? "";
			entry.StartTime = jsonEntry.StartTime;
			entry.Duration = jsonEntry.Duration;
			entry.Location = jsonEntry.Location ?? "";
			entry.AdvanceReminder = jsonEntry.AdvanceReminder;

			if (jsonEntry is JsonCalendarEntryV2 v2)
				entry.Color = v2.Color;

			return entry;
		}

		#endregion
	}

	#endregion
}
