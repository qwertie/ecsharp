using Loyc.Essentials;
using System.IO;

namespace Loyc.SyncLib.Tests
{
	public class SyncBinaryTests : SyncLibTests<SyncBinary.Reader, SyncBinary.Writer>
	{
		SyncBinary.Options _options = new SyncBinary.Options();
		ObjectMode _saveMode;
		bool _readViaStream;

		/// <param name="readViaStream">Read the data through a <see cref="StreamScanner"/>
		///   that delivers it in small blocks, instead of reading it from memory.</param>
		public SyncBinaryTests(bool nonDefaultSettings, bool readViaStream = false)
		{
			_readViaStream = readViaStream;
			if (nonDefaultSettings)
			{
				_options = new SyncBinary.Options()
				{
				};
				_saveMode = ObjectMode.Deduplicate;
			}
		}


		protected override T Read<T>(byte[] data, SyncObjectFunc<SyncBinary.Reader, T> sync)
		{
			_options.RootMode = _saveMode;
			if (_readViaStream) {
				var scanner = new StreamScanner(new MemoryStream(data), minBlockSize: 1);
				var reader = SyncBinary.NewReader(scanner, _options);
				return SyncManagerExt.Sync(reader, null, default(T), sync, _options.RootMode)!;
			}
			return SyncBinary.Read<T>(data, sync, _options)!;
		}

		protected override byte[] Write<T>(T value, SyncObjectFunc<SyncBinary.Writer, T> sync, ObjectMode mode)
		{
			_options.RootMode = mode;
			return SyncBinary.Write(value, sync, _options).ToArray();
		}
	}
}
