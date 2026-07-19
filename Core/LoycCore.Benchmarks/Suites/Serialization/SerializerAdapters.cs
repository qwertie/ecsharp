using System.Text;
using Loyc.SyncLib;

namespace Benchmark.Serialization
{
	/// <summary>Wraps one serialization library for one data type so that all
	/// libraries can be benchmarked through a common interface. Each adapter uses
	/// the library's natural payload type (UTF-8 bytes, string, or stream) to avoid
	/// charging any library for conversions it wouldn't normally perform.</summary>
	public abstract class SerializerAdapter<T>
	{
		protected SerializerAdapter(string name)
		{
			Name = name;
			WriteLabel = name + " (write)";
			ReadLabel = name + " (read)";
		}

		public string Name { get; }
		/// <summary>Chart-series names for this serializer's write and read
		/// measurements. SyncLib adapters override the defaults with the actual API
		/// method being benchmarked (e.g. "SyncJson.Write").</summary>
		public string WriteLabel { get; protected set; }
		public string ReadLabel { get; protected set; }

		public abstract object Serialize(T value);
		public abstract T? Deserialize(object payload);

		/// <summary>Payload size in bytes (UTF-8 length for string payloads).</summary>
		public static int GetPayloadSize(object payload) => payload switch {
			byte[] b => b.Length,
			string s => Encoding.UTF8.GetByteCount(s),
			ReadOnlyMemory<byte> m => m.Length,
			MemoryStream ms => (int)ms.Length,
			_ => throw new NotSupportedException("Unexpected payload type " + payload.GetType()),
		};

		/// <summary>Turns a SyncLib adapter name like "SyncJson (Newton-compat)" into a
		/// series label naming the benchmarked API method, e.g.
		/// "SyncJson.Write (Newton-compat)".</summary>
		protected static string LabelWithMethod(string name, string method)
		{
			int space = name.IndexOf(' ');
			return space < 0 ? name + "." + method
				: name.Substring(0, space) + "." + method + name.Substring(space);
		}
	}

	public class SyncJsonAdapter<T> : SerializerAdapter<T>
	{
		readonly SyncObjectFunc<SyncJson.Writer, T> _write;
		readonly SyncObjectFunc<SyncJson.Reader, T> _read;
		readonly SyncJson.Options _options;

		public SyncJsonAdapter(string name,
			SyncObjectFunc<SyncJson.Writer, T> write, SyncObjectFunc<SyncJson.Reader, T> read,
			SyncJson.Options? options = null) : base(name)
		{
			WriteLabel = LabelWithMethod(name, "Write");
			ReadLabel = LabelWithMethod(name, "Read");
			_write = write;
			_read = read;
			_options = options ?? new SyncJson.Options();
		}

		public override object Serialize(T value) => SyncJson.Write(value, _write, _options);
		public override T? Deserialize(object payload)
			=> SyncJson.Read((ReadOnlyMemory<byte>)payload, _read, _options);
	}

	/// <summary>SyncJson through the ISyncManager interface (WriteI/ReadI), i.e. the
	/// way the home-page example is written. Slower than the generic fast path
	/// because every call is an interface dispatch.</summary>
	public class SyncJsonInterfaceAdapter<T> : SerializerAdapter<T>
	{
		readonly SyncObjectFunc<ISyncManager, T> _sync;
		readonly SyncJson.Options _options;

		public SyncJsonInterfaceAdapter(string name, SyncObjectFunc<ISyncManager, T> sync,
			SyncJson.Options? options = null)
			: base(name)
		{
			WriteLabel = LabelWithMethod(name, "WriteI");
			ReadLabel = LabelWithMethod(name, "ReadI");
			_sync = sync;
			_options = options ?? new SyncJson.Options();
		}

		public override object Serialize(T value) => SyncJson.WriteI(value, _sync, _options);
		public override T? Deserialize(object payload)
			=> SyncJson.ReadI((ReadOnlyMemory<byte>)payload, _sync, _options);
	}

	public class SyncBinaryAdapter<T> : SerializerAdapter<T>
	{
		readonly SyncObjectFunc<SyncBinary.Writer, T> _write;
		readonly SyncObjectFunc<SyncBinary.Reader, T> _read;
		readonly SyncBinary.Options _options;

		public SyncBinaryAdapter(string name,
			SyncObjectFunc<SyncBinary.Writer, T> write, SyncObjectFunc<SyncBinary.Reader, T> read,
			SyncBinary.Options? options = null) : base(name)
		{
			WriteLabel = LabelWithMethod(name, "Write");
			ReadLabel = LabelWithMethod(name, "Read");
			_write = write;
			_read = read;
			_options = options ?? new SyncBinary.Options();
		}

		public override object Serialize(T value) => SyncBinary.Write(value, _write, _options);
		public override T? Deserialize(object payload)
			=> SyncBinary.Read((ReadOnlyMemory<byte>)payload, _read, _options);
	}

	/// <summary>SyncBinary through the ISyncManager interface (used for the calendar
	/// scenario, whose sync function is written against ISyncManager).</summary>
	public class SyncBinaryInterfaceAdapter<T> : SerializerAdapter<T>
	{
		readonly SyncObjectFunc<ISyncManager, T> _sync;
		readonly SyncBinary.Options _options;

		public SyncBinaryInterfaceAdapter(string name, SyncObjectFunc<ISyncManager, T> sync,
			SyncBinary.Options? options = null)
			: base(name)
		{
			WriteLabel = LabelWithMethod(name, "WriteI");
			ReadLabel = LabelWithMethod(name, "ReadI");
			_sync = sync;
			_options = options ?? new SyncBinary.Options();
		}

		public override object Serialize(T value) => SyncBinary.WriteI(value, _sync, _options);
		public override T? Deserialize(object payload)
			=> SyncBinary.ReadI((ReadOnlyMemory<byte>)payload, _sync, _options);
	}

	public class SystemTextJsonAdapter<T> : SerializerAdapter<T>
	{
		// IncludeFields: unlike Newtonsoft, STJ ignores public fields by default,
		// and several benchmark models use fields
		public static readonly System.Text.Json.JsonSerializerOptions DefaultOptions
			= new() { IncludeFields = true };
		readonly System.Text.Json.JsonSerializerOptions _options;

		public SystemTextJsonAdapter(System.Text.Json.JsonSerializerOptions? options = null,
			string name = "System.Text.Json")
			: base(name) => _options = options ?? DefaultOptions;

		public override object Serialize(T value)
			=> System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(value, _options);
		public override T? Deserialize(object payload)
			=> System.Text.Json.JsonSerializer.Deserialize<T>((byte[])payload, _options);
	}

	public class NewtonsoftAdapter<T> : SerializerAdapter<T>
	{
		readonly Newtonsoft.Json.JsonSerializerSettings _settings;

		public NewtonsoftAdapter(Newtonsoft.Json.JsonSerializerSettings? settings = null,
			string name = "Newtonsoft.Json")
			: base(name) => _settings = settings ?? new Newtonsoft.Json.JsonSerializerSettings();

		public override object Serialize(T value) => Newtonsoft.Json.JsonConvert.SerializeObject(value, _settings);
		public override T? Deserialize(object payload)
			=> Newtonsoft.Json.JsonConvert.DeserializeObject<T>((string)payload, _settings);
	}

	public class MessagePackAdapter<T> : SerializerAdapter<T>
	{
		readonly MessagePack.MessagePackSerializerOptions _options;

		public MessagePackAdapter(MessagePack.MessagePackSerializerOptions? options = null,
			string name = "MessagePack")
			: base(name) => _options = options ?? MessagePack.MessagePackSerializerOptions.Standard;

		public override object Serialize(T value) => MessagePack.MessagePackSerializer.Serialize(value, _options);
		public override T? Deserialize(object payload)
			=> MessagePack.MessagePackSerializer.Deserialize<T>((byte[])payload, _options);
	}

	public class ProtobufNetAdapter<T> : SerializerAdapter<T>
	{
		public ProtobufNetAdapter(string name = "protobuf-net") : base(name)
		{
			// Warm up the runtime model. Inbuilt types (primitive arrays, dictionaries)
			// can't be prepared this way but serialize fine anyway.
			try { ProtoBuf.Serializer.PrepareSerializer<T>(); } catch (ArgumentException) { }
		}

		public override object Serialize(T value)
		{
			var stream = new MemoryStream();
			ProtoBuf.Serializer.Serialize(stream, value);
			return stream;
		}
		public override T? Deserialize(object payload)
		{
			var stream = (MemoryStream)payload;
			stream.Position = 0;
			return ProtoBuf.Serializer.Deserialize<T>(stream);
		}
	}

	/// <summary>The retired .NET Framework BinaryFormatter ("old binary
	/// serialization"), available on .NET 9+ only via the unsupported
	/// System.Runtime.Serialization.Formatters compatibility package.</summary>
	public class BinaryFormatterAdapter<T> : SerializerAdapter<T>
	{
#pragma warning disable SYSLIB0011 // BinaryFormatter is obsolete
		readonly System.Runtime.Serialization.Formatters.Binary.BinaryFormatter _formatter = new();

		public BinaryFormatterAdapter(string name = "BinaryFormatter") : base(name) { }

		public override object Serialize(T value)
		{
			var stream = new MemoryStream();
			_formatter.Serialize(stream, value!);
			return stream;
		}
		public override T? Deserialize(object payload)
		{
			var stream = (MemoryStream)payload;
			stream.Position = 0;
			return (T?)_formatter.Deserialize(stream);
		}
#pragma warning restore SYSLIB0011
	}
}
