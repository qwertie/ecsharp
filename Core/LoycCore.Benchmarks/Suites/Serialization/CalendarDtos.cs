// The "traditional" serialization code that the SyncLib home-page example compares
// itself against: dedicated DTO types plus code to convert business objects to and
// from them. The Newtonsoft section is copied from
// Core/Tests/SyncLib/HomePageCalendarExample.cs; the System.Text.Json and binary
// (protobuf-net / MessagePack / BinaryFormatter) sections are the equivalent
// idiomatic DTO code for those libraries.
//
// One deviation from the example, for a fair benchmark: optional CPU-eating
// formatting features are disabled — no Formatting.Indented and no camelCase
// naming strategy (SyncJson's NameConverter is likewise off; see
// SerializationSuite.RegisterCalendar).
using System.Drawing;
using System.Text;
using MessagePack;
using Newtonsoft.Json;
using ProtoBuf;

namespace Benchmark.Serialization
{
	#region Newtonsoft.Json DTOs + serialization (copied from the home-page example)

	public class JsonCalendar
	{
		public IEnumerable<JsonCalendarEntry?>? Entries { get; set; }
		public int UserId { get; set; }
	}

	public class JsonCalendarV2 : JsonCalendar
	{
		public string? DefColor { get; set; }
		public new IEnumerable<JsonCalendarEntryV2?>? Entries { get; set; }
	}

	public class JsonCalendarEntry
	{
		public int Id { get; set; }
		public string? Description { get; set; }
		public DateTime StartTime { get; set; }
		public string? Location { get; set; }
		public TimeSpan? AdvanceReminder { get; set; }
		public virtual DateTime EndTime { get; set; }
	}

	public class JsonCalendarEntryV2 : JsonCalendarEntry
	{
		[JsonIgnore]
		public override DateTime EndTime { get; set; }
		public TimeSpan Duration { get; set; }
		public string? Color { get; set; }
	}

	public class JsonCalendarSerialization
	{
		// Note: The serialized form does not include the Calendar.Id but it's included
		//       in the Web API's URL. The web controller will save that Calendar Id here.
		public int CalendarId { get; set; }
		public int ApiVersion { get; set; } = 2;

		private JsonSerializer _serializer = new JsonSerializer {
			PreserveReferencesHandling = PreserveReferencesHandling.None,
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

			return calendar == null ? null : FromJsonCalendar((JsonCalendar)calendar);
		}

		#region Code to convert Calendar to JsonCalendar for serialization

		public JsonCalendar ToJsonCalendar(Calendar calendar)
		{
			var jsonEntries = calendar.Entries.Pairs().Select(pair => ToJsonCalendarEntry(pair.Value));

			if (ApiVersion <= 1) {
				return new JsonCalendar {
					UserId = calendar.UserId,
					Entries = jsonEntries,
				};
			} else {
				return new JsonCalendarV2 {
					UserId = calendar.UserId,
					Entries = jsonEntries.Cast<JsonCalendarEntryV2>(),
					DefColor = ToString(calendar.DefaultColor),
				};
			}
		}

		private JsonCalendarEntry ToJsonCalendarEntry(CalendarEntry entry)
		{
			JsonCalendarEntry jsonEntry;
			if (ApiVersion <= 1) {
				jsonEntry = new JsonCalendarEntry() {
					EndTime = entry.StartTime.Add(entry.Duration)
				};
			} else {
				jsonEntry = new JsonCalendarEntryV2 {
					Duration = entry.Duration,
					Color = ToString(entry.Color)
				};
			}

			jsonEntry.Id = entry.Id;
			jsonEntry.Description = entry.Description;
			jsonEntry.StartTime = entry.StartTime;
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
				Entries = new Loyc.Collections.BMultiMap<DateTime, CalendarEntry>()
			};

			var entries = jsonCalendar.Entries ?? (jsonCalendar as JsonCalendarV2)?.Entries;
			if (entries == null)
				throw new FormatException("Missing calendar entries");

			foreach (var entry in entries)
				_calendar.Entries[entry!.StartTime].Add(FromJsonCalendarEntry(entry!));

			if (jsonCalendar is JsonCalendarV2 v2) {
				_calendar.DefaultColor = ToColor(v2.DefColor);
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
			entry.Location = jsonEntry.Location ?? "";
			entry.AdvanceReminder = jsonEntry.AdvanceReminder;

			if (jsonEntry is JsonCalendarEntryV2 v2) {
				entry.Color = ToColor(v2.Color);
				entry.Duration = v2.Duration;
			} else {
				entry.Duration = jsonEntry.EndTime.Subtract(jsonEntry.StartTime);
			}

			return entry;
		}

		#endregion

		public static string ToString(Color c) => "#" + (c.ToArgb() & 0xFFFFF).ToString("X6");
		public static Color ToColor(string? s)
		{
			if (s == null || !s.StartsWith("#"))
				throw new FormatException("Expected a color (starting with '#')");
			return Color.FromArgb(Convert.ToInt32(s.Substring(1), 16));
		}
	}

	#endregion

	#region System.Text.Json DTOs + serialization (equivalent code for STJ)

	// Flat DTOs (v2 API only): System.Text.Json does not allow the property-hiding
	// trick the Newtonsoft DTOs use, so a real STJ user would write these instead.
	public class StjCalendarV2
	{
		public string? DefColor { get; set; }
		public List<StjCalendarEntryV2>? Entries { get; set; }
		public int UserId { get; set; }
	}

	public class StjCalendarEntryV2
	{
		public int Id { get; set; }
		public string? Description { get; set; }
		public DateTime StartTime { get; set; }
		public string? Location { get; set; }
		public TimeSpan? AdvanceReminder { get; set; }
		public TimeSpan Duration { get; set; }
		public string? Color { get; set; }
	}

	public class StjCalendarSerialization
	{
		public int CalendarId { get; set; }

		// Defaults only: compact output, no naming policy — parity with the other
		// JSON serializers in the benchmark, which also have formatting extras off
		static readonly System.Text.Json.JsonSerializerOptions _options = new();

		public byte[] Serialize(Calendar calendar)
			=> System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(ToDto(calendar), _options);

		public Calendar? Deserialize(byte[] json)
		{
			var dto = System.Text.Json.JsonSerializer.Deserialize<StjCalendarV2>(json, _options);
			return dto == null ? null : FromDto(dto);
		}

		public StjCalendarV2 ToDto(Calendar calendar) => new StjCalendarV2 {
			UserId = calendar.UserId,
			DefColor = JsonCalendarSerialization.ToString(calendar.DefaultColor),
			Entries = calendar.Entries.Pairs().Select(pair => new StjCalendarEntryV2 {
				Id = pair.Value.Id,
				Description = pair.Value.Description,
				StartTime = pair.Value.StartTime,
				Location = pair.Value.Location,
				AdvanceReminder = pair.Value.AdvanceReminder,
				Duration = pair.Value.Duration,
				Color = JsonCalendarSerialization.ToString(pair.Value.Color),
			}).ToList(),
		};

		public Calendar FromDto(StjCalendarV2 dto)
		{
			var calendar = new Calendar {
				Id = CalendarId,
				UserId = dto.UserId,
				DefaultColor = JsonCalendarSerialization.ToColor(dto.DefColor),
			};
			foreach (var e in dto.Entries ?? throw new FormatException("Missing calendar entries"))
				calendar.Entries.Add(e.StartTime, new CalendarEntry(calendar) {
					Id = e.Id,
					Description = e.Description ?? "",
					StartTime = e.StartTime,
					Location = e.Location ?? "",
					AdvanceReminder = e.AdvanceReminder,
					Duration = e.Duration,
					Color = JsonCalendarSerialization.ToColor(e.Color),
				});
			return calendar;
		}
	}

	#endregion

	#region Binary DTOs (protobuf-net, MessagePack, BinaryFormatter) + conversion

	[Serializable, ProtoContract, MessagePackObject]
	public class BinCalendarDto
	{
		[ProtoMember(1), Key(0)] public int UserId;
		[ProtoMember(2), Key(1)] public int DefColorArgb;
		[ProtoMember(3), Key(2)] public List<BinCalendarEntryDto> Entries = new();
	}

	[Serializable, ProtoContract, MessagePackObject]
	public class BinCalendarEntryDto
	{
		[ProtoMember(1), Key(0)] public int Id;
		[ProtoMember(2), Key(1)] public string Description = "";
		[ProtoMember(3), Key(2)] public DateTime StartTime;
		[ProtoMember(4), Key(3)] public TimeSpan Duration;
		[ProtoMember(5), Key(4)] public string Location = "";
		[ProtoMember(6), Key(5)] public TimeSpan? AdvanceReminder;
		[ProtoMember(7), Key(6)] public int ColorArgb;
	}

	public class BinCalendarMapper
	{
		public int CalendarId { get; set; }

		public BinCalendarDto ToDto(Calendar calendar) => new BinCalendarDto {
			UserId = calendar.UserId,
			DefColorArgb = calendar.DefaultColor.ToArgb(),
			Entries = calendar.Entries.Pairs().Select(pair => new BinCalendarEntryDto {
				Id = pair.Value.Id,
				Description = pair.Value.Description,
				StartTime = pair.Value.StartTime,
				Duration = pair.Value.Duration,
				Location = pair.Value.Location,
				AdvanceReminder = pair.Value.AdvanceReminder,
				ColorArgb = pair.Value.Color.ToArgb(),
			}).ToList(),
		};

		public Calendar FromDto(BinCalendarDto dto)
		{
			var calendar = new Calendar {
				Id = CalendarId,
				UserId = dto.UserId,
				DefaultColor = Color.FromArgb(dto.DefColorArgb),
			};
			foreach (var e in dto.Entries)
				calendar.Entries.Add(e.StartTime, new CalendarEntry(calendar) {
					Id = e.Id,
					Description = e.Description,
					StartTime = e.StartTime,
					Duration = e.Duration,
					Location = e.Location,
					AdvanceReminder = e.AdvanceReminder,
					Color = Color.FromArgb(e.ColorArgb),
				});
			return calendar;
		}
	}

	#endregion

	/// <summary>Adapts a DTO-based serializer (inner) to the business-object type T.
	/// The DTO conversion runs inside the timed operation because it is a real cost
	/// of the traditional serialization approach.</summary>
	public class MappedAdapter<T, TDto> : SerializerAdapter<T>
	{
		readonly SerializerAdapter<TDto> _inner;
		readonly Func<T, TDto> _toDto;
		readonly Func<TDto, T> _fromDto;

		public MappedAdapter(string name, SerializerAdapter<TDto> inner, Func<T, TDto> toDto, Func<TDto, T> fromDto)
			: base(name)
		{
			_inner = inner;
			_toDto = toDto;
			_fromDto = fromDto;
		}

		public override object Serialize(T value) => _inner.Serialize(_toDto(value));
		public override T? Deserialize(object payload)
		{
			var dto = _inner.Deserialize(payload);
			return dto == null ? default : _fromDto(dto);
		}
	}
}
