using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Loyc.Utilities.Tests
{
	[TestFixture]
	public class UGTests : TestHelpers
	{
		KeyValuePair<string, string> P(string k, string v) { return new KeyValuePair<string, string>(k, v); }

		[Test]
		public void TestProcessCommandLineArguments1()
		{
			string commandLine = "-abZ -ab123 and -a=Foo --Apple:No -b plantain --a %TEMP% @notExpanded --banana -Z --empty=";
			var args = G.SplitCommandLineArguments(commandLine);

			var shortOpts = new Dictionary<char, string> { { 'a', null }, { 'b', "banana" } };
			var twoArgOptions = new InvertibleSet<string>(new[] { "banana" });
			var options = new DList<KeyValuePair<string, string>>();
			UG.ProcessCommandLineArguments(args, options, null, shortOpts, twoArgOptions, expandEnvVars: false);

			ExpectList(args.AsListSource(), "-abZ", "and", "%TEMP%", "@notExpanded", "-Z");
			ExpectList(options, P("a", null), P("banana", "123"), P("a", "Foo"), P("apple", "No"), P("banana", "plantain"), P("a", null), P("banana", null), P("empty", ""));
		}

		[Test]
		public void TestProcessCommandLineArguments2()
		{
			// Generate two options files, where the first refers to the second
			string atFolder = Environment.ExpandEnvironmentVariables("%TEMP%");
			string file1 = "test ProcessCmdLine 1.txt";
			string file2 = "test ProcessCmdLine 2.txt";
			StreamWriter w = new StreamWriter(Path.Combine(atFolder, file1));
			w.WriteLine("\"@" + file2 + "\" fox--jumps\n--over");
			w.Close();
			w = new StreamWriter(Path.Combine(atFolder, file2));
			w.WriteLine("these arguments are ignored (arg limit exceeded)");
			w.Close();

			// Expand command line and ensure that the arg limit is enforced
			List<string> args = G.SplitCommandLineArguments("\"@" + file1 + "\" \"lazy dog\"");
			var options = new DList<KeyValuePair<string, string>>();
			var msgs = new MessageHolder();
			using (MessageSink.SetDefault(msgs))
				UG.ProcessCommandLineArguments(args, options, atFolder, null, null, 5);

			ExpectList(args.AsListSource(), "@" + file1, "@" + file2, "fox--jumps", "lazy dog");
			ExpectList(options, P("over", null));
			ExpectList(msgs.List.AsListSource().Select(msg => msg.ToString()),
				"@test ProcessCmdLine 2.txt: Warning: Limit of 5 commands exceeded");
		}
	}
}
