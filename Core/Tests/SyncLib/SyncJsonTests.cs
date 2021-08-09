using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.SyncLib.Tests
{
	public class SyncJsonTests : SyncLibTests<SyncJson.Reader, SyncJson.Writer>
	{
		SyncJson.Options _options = new SyncJson.Options();

		public SyncJsonTests(bool newtonCompat, bool nonDefaultSettings = false, bool minify = false)
		{
			if (nonDefaultSettings)
				_options = new SyncJson.Options {
					NameConverter = SyncJson.ToCamelCase,
					RootMode = SubObjectMode.Deduplicate | SubObjectMode.FixedSize,
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
			_options.NewtonsoftCompatibility = newtonCompat;
			_options.Write.Minify = minify;
		}

		protected override T Read<T>(byte[] data, SyncObjectFunc<SyncJson.Reader, T> sync)
			=> SyncJson.Read<T>(data, sync, _options);

		protected override byte[] Write<T>(T value, SyncObjectFunc<SyncJson.Writer, T> sync)
			=> SyncJson.Write(value, sync, _options).ToArray();
	}
}
