using Loyc.SyncLib;
using static Benchmark.Serialization.SyncFunctions;

namespace Benchmark.Serialization
{
	/// <summary>Registers all serialization benchmarks: SyncJson and SyncBinary
	/// versus System.Text.Json, Newtonsoft.Json, BinaryFormatter, protobuf-net,
	/// and MessagePack, across a variety of data shapes.</summary>
	public static class SerializationSuite
	{
		public static void Register(BenchmarkRegistry registry)
		{
			RegisterCalendar(registry);
			RegisterObjectsAndDictionaries(registry);
			RegisterDeepNesting(registry);
			RegisterPrimitiveArrays(registry);
		}

		#region Calendar (the home-page example) — with at least 100 entries

		static void RegisterCalendar(BenchmarkRegistry registry)
		{
			// Unlike the example, no NameConverter (camelCase renaming) and no
			// pretty-printing: optional CPU-eating formatting features are disabled
			// in every JSON serializer here so the comparison stays apples-to-apples.
			var jsonOptions = new SyncJson.Options(compactMode: true);

			var scenario = new Scenario<Calendar>("Calendar",
				("100 entries", 100, () => CalendarGenerator.Generate(100)),
				("300 entries", 300, () => CalendarGenerator.Generate(300)),
				("1000 entries", 1000, () => CalendarGenerator.Generate(1000)),
				("3000 entries", 3000, () => CalendarGenerator.Generate(3000))) {
				XAxisTitle = "Calendar size",
				Validate = CalendarGenerator.Validate,
			};

			// SyncLib, fast path: the example's sync code specialized for each manager type
			scenario.Adapters.Add(new SyncJsonAdapter<Calendar>("SyncJson",
				new CalendarSync<SyncJson.Writer>().Sync, new CalendarSync<SyncJson.Reader>().Sync, jsonOptions));
			// SyncLib through ISyncManager: the exact code shown in the home-page example
			scenario.Adapters.Add(new SyncJsonInterfaceAdapter<Calendar>("SyncJson (ISyncManager)",
				new CalendarSync<ISyncManager>().Sync, jsonOptions));
			// Same sync code, binary format
			scenario.Adapters.Add(new SyncBinaryAdapter<Calendar>("SyncBinary",
				new CalendarSync<SyncBinary.Writer>().Sync, new CalendarSync<SyncBinary.Reader>().Sync));
			// SyncBinary with framing markers off (Markers.None): isolates the cost of
			// the object start/end markers and type tags that Markers.Default writes per
			// sub-object — most visible in object-heavy data like this calendar.
			scenario.Adapters.Add(new SyncBinaryAdapter<Calendar>("SyncBinary (no markers)",
				new CalendarSync<SyncBinary.Writer>().Sync, new CalendarSync<SyncBinary.Reader>().Sync,
				new SyncBinary.Options { Markers = SyncBinary.Markers.None }));

			// The traditional approach: DTO types + conversion code
			var newtonsoft = new JsonCalendarSerialization();
			scenario.Adapters.Add(new DelegateAdapter<Calendar>("Newtonsoft.Json",
				c => newtonsoft.Serialize(c), p => newtonsoft.Deserialize((string)p)));
			var stj = new StjCalendarSerialization();
			scenario.Adapters.Add(new DelegateAdapter<Calendar>("System.Text.Json",
				c => stj.Serialize(c), p => stj.Deserialize((byte[])p)));

			var mapper = new BinCalendarMapper();
			scenario.Adapters.Add(new MappedAdapter<Calendar, BinCalendarDto>("protobuf-net",
				new ProtobufNetAdapter<BinCalendarDto>(), mapper.ToDto, mapper.FromDto));
			scenario.Adapters.Add(new MappedAdapter<Calendar, BinCalendarDto>("MessagePack",
				new MessagePackAdapter<BinCalendarDto>(), mapper.ToDto, mapper.FromDto));
			scenario.Adapters.Add(new MappedAdapter<Calendar, BinCalendarDto>("BinaryFormatter",
				new BinaryFormatterAdapter<BinCalendarDto>(), mapper.ToDto, mapper.FromDto));

			registry.AddScenarios("Loyc.SyncLib/Calendar (home-page example)",
				"The Calendar example from SyncLib's documentation (HomePageCalendarExample.cs): " +
				"entries live in a BMultiMap but are serialized as a flat list, colors become hex " +
				"strings, and dates/durations become strings. SyncLib does the conversions inline in " +
				"its sync function; the other serializers use dedicated DTO types plus conversion " +
				"code, which runs inside the timed operation because that is the real cost of the " +
				"traditional approach. Unlike the example, optional formatting features are off in " +
				"every JSON serializer (compact output, no camelCase renaming) so none of them pays " +
				"for CPU-eating extras.",
				scenario);
		}

		#endregion

		/// <summary>Three data shapes sharing one set of charts (like Primitive arrays):
		/// small-object lists, string dictionaries, and one wide flat object.</summary>
		static void RegisterObjectsAndDictionaries(BenchmarkRegistry registry)
		{
			const string group = "Objects & dictionaries";

			// Lists of a tiny 3-field object (int, string, double)
			var smallObjects = new Scenario<List<SmallObject>>("Small objects",
				("Small objects ×5", 5, () => SmallObject.MakeList(5)),
				("Small objects ×100", 100, () => SmallObject.MakeList(100)),
				("Small objects ×10000", 10000, () => SmallObject.MakeList(10000))) {
				Validate = (a, b) => b != null && a.Count == b.Count && a.Zip(b).All(p => p.First.Equals(p.Second))
					? null : "list mismatch",
			};
			AddStandardAdapters(smallObjects,
				SyncSmallObjectList, SyncSmallObjectList,
				SyncSmallObjectList, SyncSmallObjectList);

			// Dictionary<string, string> with realistic keys and phrase values
			var stringDict = new Scenario<Dictionary<string, string>>("String dict",
				("String dict ×5", 5, () => ArrayData.MakeStringDict(5)),
				("String dict ×100", 100, () => ArrayData.MakeStringDict(100)),
				("String dict ×10000", 10000, () => ArrayData.MakeStringDict(10000))) {
				Validate = (a, b) => b != null && a.Count == b.Count
					&& a.All(p => b.TryGetValue(p.Key, out var v) && v == p.Value) ? null : "dictionary mismatch",
			};
			var compact = new SyncJson.Options(compactMode: true);
			// The natural JSON representation: one JSON object, keys as property names
			stringDict.Adapters.Add(new SyncJsonAdapter<Dictionary<string, string>>("SyncJson",
				SyncStringDictAsObject, SyncStringDictAsObject, compact));
			// Binary has no JSON-object analog, so it stores a list of key/value pairs
			stringDict.Adapters.Add(new SyncBinaryAdapter<Dictionary<string, string>>("SyncBinary",
				SyncStringDictAsList, SyncStringDictAsList));
			stringDict.Adapters.Add(new SystemTextJsonAdapter<Dictionary<string, string>>());
			stringDict.Adapters.Add(new NewtonsoftAdapter<Dictionary<string, string>>());
			stringDict.Adapters.Add(new BinaryFormatterAdapter<Dictionary<string, string>>());
			stringDict.Adapters.Add(new ProtobufNetAdapter<Dictionary<string, string>>());
			stringDict.Adapters.Add(new MessagePackAdapter<Dictionary<string, string>>());

			// A single wide, flat object (26 fields). Its figures are divided by 10
			// (and the label says so) because per-object values would dwarf the
			// per-item values of the list/dictionary categories sharing these charts.
			var wideObject = new Scenario<WideObject>("Wide object",
				("Wide object ÷10", 10, () => WideObject.Make())) {
				Validate = (a, b) => a.DiffFrom(b) is string field ? "field " + field + " mismatch" : null,
			};
			AddStandardAdapters(wideObject,
				SyncWideObject, SyncWideObject,
				SyncWideObject, SyncWideObject);

			var scenarios = new ScenarioBase[] { smallObjects, stringDict, wideObject };
			foreach (var s in scenarios) {
				s.GraphGroup = group;
				s.XAxisTitle = "Data shape";
			}

			registry.AddScenarios("Loyc.SyncLib/Objects & dictionaries",
				"Everyday object graphs on one set of charts: lists of a tiny 3-field object (int, " +
				"string, double); Dictionary<string, string> with realistic keys and phrase values " +
				"(JSON serializers store it as a JSON object, binary serializers as key/value pairs); " +
				"and one wide, flat object with 13 primitive/string field types plus a nullable variant " +
				"of each (half null), measuring per-field costs including field names/tags. (BigInteger " +
				"and char are omitted from the wide object because protobuf-net does not support them.)",
				scenarios);
		}

		static void RegisterDeepNesting(BenchmarkRegistry registry)
		{
			var stjOptions = new System.Text.Json.JsonSerializerOptions { MaxDepth = 2048, IncludeFields = true };
			var newtonsoftSettings = new Newtonsoft.Json.JsonSerializerSettings { MaxDepth = null };

			var scenario = new Scenario<Node>("Deep nesting",
				("depth 10", 10, () => Node.MakeChain(10)),
				("depth 50", 50, () => Node.MakeChain(50)),
				("depth 250", 250, () => Node.MakeChain(250))) {
				XAxisTitle = "Nesting depth",
				Validate = Node.Diff,
			};
			AddStandardAdapters(scenario,
				SyncNode, SyncNode,
				SyncNode, SyncNode,
				stjOptions, newtonsoftSettings,
				tweakSyncJson: options => options.Read.MaxDepth = 2048);

			registry.AddScenarios("Loyc.SyncLib/Deep nesting",
				"A linked chain of small nodes, nested up to 250 levels deep (metrics are shown " +
				"per nesting level) — stresses recursion and depth tracking. (System.Text.Json and " +
				"Newtonsoft both need their default 64-level depth limits raised for this.) " +
				"⚠ SyncLib is not designed for deeply-nested data: because it recurses on the call " +
				"stack, very deep nesting can throw a StackOverflowException, which .NET cannot catch " +
				"and which would terminate the whole benchmark process — that is why depth is capped " +
				"at 250 here.",
				scenario);
		}

		static void RegisterPrimitiveArrays(BenchmarkRegistry registry)
		{
			const string group = "Primitive arrays";
			var scenarios = new ScenarioBase[] {
				MakeArrayScenario("int[10k], values 0–127", 10_000, () => ArrayData.MakeSmallInts(10_000),
					SyncIntArray, SyncIntArray, SyncIntArray, SyncIntArray),
				MakeArrayScenario("int[10k], full range", 10_000, () => ArrayData.MakeLargeInts(10_000),
					SyncIntArray, SyncIntArray, SyncIntArray, SyncIntArray),
				MakeArrayScenario("long[10k]", 10_000, () => ArrayData.MakeLongs(10_000),
					SyncLongArray, SyncLongArray, SyncLongArray, SyncLongArray),
				MakeArrayScenario("double[10k]", 10_000, () => ArrayData.MakeDoubles(10_000),
					SyncDoubleArray, SyncDoubleArray, SyncDoubleArray, SyncDoubleArray),
				MakeArrayScenario("byte[100k]", 100_000, () => ArrayData.MakeBytes(100_000),
					SyncByteArray, SyncByteArray, SyncByteArray, SyncByteArray),
				MakeArrayScenario("string[1k], plain ASCII", 1000, () => ArrayData.MakeAsciiStrings(1000),
					SyncStringArray, SyncStringArray, SyncStringArray, SyncStringArray),
				MakeArrayScenario("string[1k], escapes+Unicode", 1000, () => ArrayData.MakeMessyStrings(1000),
					SyncStringArray, SyncStringArray, SyncStringArray, SyncStringArray),
			};
			foreach (var s in scenarios) {
				s.GraphGroup = group;
				s.XAxisTitle = "Array type";
			}

			registry.AddScenarios("Loyc.SyncLib/Primitive arrays",
				"Bulk data throughput: arrays of ints (small and full-range values), longs, doubles, " +
				"100 KB of raw bytes, and 1000 strings (plain, and laced with characters JSON must " +
				"escape plus non-ASCII text). Note: with NewtonsoftCompatibility off, SyncJson " +
				"stores byte arrays as BAIS strings instead of Base64.",
				scenarios);
		}

		static Scenario<T[]> MakeArrayScenario<T>(string label, int items, Func<T[]> data,
			SyncObjectFunc<SyncJson.Writer, T[]> syncJsonWrite, SyncObjectFunc<SyncJson.Reader, T[]> syncJsonRead,
			SyncObjectFunc<SyncBinary.Writer, T[]> syncBinWrite, SyncObjectFunc<SyncBinary.Reader, T[]> syncBinRead)
			where T : IEquatable<T>
		{
			var scenario = new Scenario<T[]>(label, (label, items, data)) {
				Validate = (a, b) => b != null && a.Length == b.Length && a.Zip(b).All(p => p.First.Equals(p.Second))
					? null : "array mismatch",
			};
			AddStandardAdapters(scenario, syncJsonWrite, syncJsonRead, syncBinWrite, syncBinRead);
			return scenario;
		}

		/// <summary>Adds the standard set of eight serializers to a scenario whose
		/// type all of them can handle directly (via attributes on the model).</summary>
		static void AddStandardAdapters<T>(Scenario<T> scenario,
			SyncObjectFunc<SyncJson.Writer, T> syncJsonWrite, SyncObjectFunc<SyncJson.Reader, T> syncJsonRead,
			SyncObjectFunc<SyncBinary.Writer, T> syncBinWrite, SyncObjectFunc<SyncBinary.Reader, T> syncBinRead,
			System.Text.Json.JsonSerializerOptions? stjOptions = null,
			Newtonsoft.Json.JsonSerializerSettings? newtonsoftSettings = null,
			Action<SyncJson.Options>? tweakSyncJson = null)
		{
			var compact = new SyncJson.Options(compactMode: true);
			var compatOff = new SyncJson.Options(compactMode: true) { NewtonsoftCompatibility = false };
			tweakSyncJson?.Invoke(compact);
			tweakSyncJson?.Invoke(compatOff);
			scenario.Adapters.Add(new SyncJsonAdapter<T>("SyncJson (Newton-compat)", syncJsonWrite, syncJsonRead, compact));
			scenario.Adapters.Add(new SyncJsonAdapter<T>("SyncJson", syncJsonWrite, syncJsonRead, compatOff));
			scenario.Adapters.Add(new SyncBinaryAdapter<T>("SyncBinary", syncBinWrite, syncBinRead));
			// Markers.None variant: framing markers/type tags off (see the calendar note).
			scenario.Adapters.Add(new SyncBinaryAdapter<T>("SyncBinary (no markers)", syncBinWrite, syncBinRead,
				new SyncBinary.Options { Markers = SyncBinary.Markers.None }));
			scenario.Adapters.Add(new SystemTextJsonAdapter<T>(stjOptions));
			scenario.Adapters.Add(new NewtonsoftAdapter<T>(newtonsoftSettings));
			scenario.Adapters.Add(new BinaryFormatterAdapter<T>());
			scenario.Adapters.Add(new ProtobufNetAdapter<T>());
			scenario.Adapters.Add(new MessagePackAdapter<T>());
		}

		/// <summary>An adapter defined by two lambdas (for serializers that need
		/// scenario-specific glue, like the calendar DTO serialization classes).</summary>
		class DelegateAdapter<T> : SerializerAdapter<T>
		{
			readonly Func<T, object> _serialize;
			readonly Func<object, T?> _deserialize;
			public DelegateAdapter(string name, Func<T, object> serialize, Func<object, T?> deserialize)
				: base(name) { _serialize = serialize; _deserialize = deserialize; }
			public override object Serialize(T value) => _serialize(value);
			public override T? Deserialize(object payload) => _deserialize(payload);
		}
	}
}
