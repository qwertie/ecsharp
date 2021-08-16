using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.SyncLib.Tests
{
	public class SyncJsonTests : SyncLibTests<SyncJson.Reader, SyncJson.Writer>
	{
		SyncJson.Options _options = new SyncJson.Options();
		SubObjectMode _saveMode;

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
				_saveMode = SubObjectMode.Deduplicate | SubObjectMode.FixedSize;
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

		protected override byte[] Write<T>(T value, SyncObjectFunc<SyncJson.Writer, T> sync, SubObjectMode mode) {
			_options.RootMode = mode;
			return SyncJson.Write(value, sync, _options).ToArray();
		}
	}
}
