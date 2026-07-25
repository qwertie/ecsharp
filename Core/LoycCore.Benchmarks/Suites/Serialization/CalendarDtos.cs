// Benchmark-only DTOs and mappers equivalent to the traditional Newtonsoft code
// in Core/Tests/SyncLib/HomePageCalendarExample.cs.
//
// One deviation from the example, for a fair benchmark: optional CPU-eating
// formatting features are disabled — no Formatting.Indented and no camelCase
// naming strategy (SyncJson's NameConverter is likewise off; see
// SerializationSuite.RegisterCalendar).
using System.Drawing;
using MessagePack;
using ProtoBuf;
using Calendar = Loyc.SyncLib.Tests.Calendar;
using CalendarEntry = Loyc.SyncLib.Tests.CalendarEntry;
using JsonCalendarSerialization = Loyc.SyncLib.Tests.JsonCalendarSerialization;

namespace Benchmark.Serialization
{
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
			foreach (var e in dto.Entries) {
				// protobuf-net (at its default CompatibilityLevel) round-trips DateTime
				// ticks correctly but drops DateTimeKind, returning Unspecified. The
				// benchmark's generated StartTimes are UTC, and Validate normalizes with
				// ToUniversalTime() — which misreads an Unspecified value as Local and
				// shifts it by the machine's offset (silently failing round-trip
				// validation on any non-UTC machine). Re-stamp the known-UTC instant so
				// the DTO round-trip is correct. This is a no-op for MessagePack and
				// BinaryFormatter, which already preserve Kind=Utc.
				var startTime = DateTime.SpecifyKind(e.StartTime, DateTimeKind.Utc);
				calendar.Entries.Add(startTime, new CalendarEntry(calendar) {
					Id = e.Id,
					Description = e.Description,
					StartTime = startTime,
					Duration = e.Duration,
					Location = e.Location,
					AdvanceReminder = e.AdvanceReminder,
					Color = Color.FromArgb(e.ColorArgb),
				});
			}
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
