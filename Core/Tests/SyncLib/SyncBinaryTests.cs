namespace Loyc.SyncLib.Tests
{
	public class SyncBinaryTests : SyncLibTests<SyncBinary.Reader, SyncBinary.Writer>
	{
		SyncBinary.Options _options = new SyncBinary.Options();
		ObjectMode _saveMode;

		public SyncBinaryTests(bool nonDefaultSettings)
		{
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
			return SyncBinary.Read<T>(data, sync, _options)!;
		}

		protected override byte[] Write<T>(T value, SyncObjectFunc<SyncBinary.Writer, T> sync, ObjectMode mode)
		{
			_options.RootMode = mode;
			return SyncBinary.Write(value, sync, _options).ToArray();
		}
	}
}
