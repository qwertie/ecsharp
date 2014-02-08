using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Text.RegularExpressions;
using System.IO;
using Loyc.Geometry;
using Loyc.Collections;
using Loyc.Collections.Impl;

namespace Loyc.Utilities
{
	public static class UG
	{
		/// <summary>
		/// Expands environment variables (e.g. %TEMP%) and @files in a list of
		/// command-line arguments, and adds any options of the form "--opt" or
		/// "--opt=value" to a dictionary.
		/// </summary>
		/// <param name="args">The original arguments to process.</param>
		/// <param name="options">Any long options (arguments that start with "--") 
		/// will be added to this dictionary, and removed from <c>args</c>. This 
		/// parameter cannot be null. 
		///   By default, long options are not case sensitive. In that case, the 
		/// user's option name is converted to lower case.
		/// <para/>
		/// Long options are expected to have the form --ID or --ID=value, where ID 
		/// matches the regex "[a-zA-Z_0-9-]+". If there is no "=" or ":", that's 
		/// okay too. For example, --Id{foo} is equivalent to --Id={foo}; both yield
		/// in the name-value pair ("id", "{foo}"). If there is no value (no equals
		/// or colon), the value associated with the option is null.</param>
		/// <param name="atFolder">If a parameter has the form @filename, the folder
		/// specified by atFolder will be searched for an options text file with the
		/// user-specified filename, and the contents of the file will be expanded 
		/// into the list of arguments (split using SplitCommandLineArguments). The
		/// expanded list can contain new @filenames, which are also processed. To
		/// search in the current directory, use "". The @filename may use an absolute
		/// path, which overrides this folder. To disable @filename expansion, set 
		/// this parameter to null. Whether the feature is enabled or disabled, 
		/// @filenames are <i>not</i> removed from <c>args</c>, in case you want to 
		/// be aware of the filenames afterward.</param>
		/// <param name="shortOptions">A map from one-letter options that start with 
		/// "-" rather than "--", to the name of the corresponding long option (this
		/// option can be null to ignore all short options.) For example, if this 
		/// contains (<c>'a', "all"</c>), and the input <c>args</c> contains "-a:foo",
		/// the pair ("all", "foo") will be added to <c>options</c>. If a value in 
		/// this map is null, the key itself is used. Short options can be combined; 
		/// for example <c>-abc:foo</c> is equivalent to <c>-a -b -c:foo</c>. Short 
		/// options are always case-sensitive; to define an option that is not case 
		/// sensitive, place two entries in the dictionary e.g. ('a', "all") and 
		/// ('A', "all"). If the user specifies a short option letter that is not 
		/// recognized, the entire command will be ignored and left in args. For 
		/// example, if <c>shortOptions</c> contains only ('a', "all") but <c>args</c>
		/// contains "-ab=foo", the command is ignored and left in <c>args</c>. 
		/// Rationale: -ab=foo might be a filename.
		/// <para/>
		/// On the other hand, if -a is a valid option then <c>-a123</c> is also 
		/// valid even when there is no option called '1'; the number "123" is 
		/// treated as an argument to -a. Now, if '1' is a registered short option 
		/// but '2' is not, then <c>-a123</c> is equivalent to <c>-a -1=23</c>.
		/// </param>
		/// <param name="twoArgOptions">A set of options in which the argument can
		/// be separated by a space from the option. For example, if the input is 
		/// "--out foo.txt" and you want to recognize "foo.txt" as the argument to
		/// "--out", add the string "out" to this set. If you want to treat <i>all</i>
		/// options this way, use <c>InvertibleSet{string}.All</c>. Note: 
		/// If the input is "--out:foo bar", "foo" is recognized as the argument to
		/// "--out" and "bar" is left alone, i.e. it is treated as unrelated.
		///   Short options participate automatically. For example if "-f" means 
		/// "--foo", and twoArgOptions contains "foo", then "-f arg" is interpreted 
		/// like "--foo=arg".
		/// <para/>
		/// The argument will not be treated as an argument if it starts with a 
		/// dash, e.g. in <c>--foo -*</c>, <c>-*</c> will not be treated as an 
		/// argument to <c>--foo</c>, even if <c>-*</c> is not a registered option.
		/// </param>
		/// <param name="argLimit">A limit placed on the number of arguments when
		/// expanding @files. Such a file may refer to itself, and this is the only
		/// protection provided against infinite recursive expansion.</param>
		/// <param name="expandEnvVars">If true, environment variable references
		/// such as <c>%TEMP%</c> are expanded by calling the standard method
		/// <see cref="Environment.ExpandEnvironmentVariables"/>.</param>
		/// <param name="caseSensitiveLongOpts">If true, long options are case-
		/// sensitive. By default, long options are not case sensitive.</param>
		/// <remarks>
		/// Two types of options are recognized, short (-s) and long (--long), and
		/// only one argument is supported per option. The documentation is above.
		/// <para/>
		/// You can choose whether to permit duplicate options or not. If you use
		/// a standard <see cref="Dictionary{K,V}"/> to hold the options, an 
		/// exception will occur when this method calls Add() to add the duplicate. 
		/// The exception is caught, the first ocurrance is kept, and a warning 
		/// message is printed to <see cref="MessageSink.Current"/>.
		/// <para/>
		/// To allow duplicates, store options in a different data structure such as 
		/// <c>List(KeyValuePair(string, string))</c> or <c>BMultiMap(string,string)</c>.
		/// <para/>
		/// DOS-style slash-options like /foo are not supported. Since Windows
		/// recognizes the forward slash as a path separator, forward-slash options
		/// can be recognized as paths. If you want to recognize them as options 
		/// instead, you can preprocess the argument list, replacing every command 
		/// that starts with "/" with a "--" command:
		/// <code>
		///   for (int i = 0; args.Count > i; i++)
		///     if (args[i].StartsWith("/")) 
		///       args[i] = "--" + args[i].Substring(1);
		/// </code>
		/// <para/>
		/// Globs (e.g. *.txt) are not recognized or expanded, but environment 
		/// variables are expanded when <c>expandEnvVars</c> is true.
		/// <para/>
		/// Quote marks are not processed. An argument of <c>"--a"</c>, with quote 
		/// marks, is not recognized as an option (these quote marks should be 
		/// removed before calling this method, e.g. 
		/// <see cref="G.SplitCommandLineArguments"/> handles this.)
		/// </remarks>
		public static void ProcessCommandLineArguments(IList<string> args, ICollection<KeyValuePair<string, string>> options, string atFolder, IDictionary<char, string> shortOptions = null, InvertibleSet<string> twoArgOptions = null, int argLimit = 0xFFFF, bool expandEnvVars = true, bool caseSensitiveLongOpts = false)
		{
			CheckParam.IsNotNull("args", args);
			CheckParam.IsNotNull("options", options);
			for (int i = 0; i < args.Count; i++)
				ProcessArgument(args, i, options, atFolder, shortOptions, twoArgOptions, argLimit, expandEnvVars, caseSensitiveLongOpts);
			args.RemoveAll(s => s == null);
		}

		internal static readonly Regex CmdLineLongOptRegex = new Regex(@"^--([a-zA-Z_0-9-]+)([=:])?(.*)$", RegexOptions.Compiled);

		private static void ProcessArgument(IList<string> args, int i, ICollection<KeyValuePair<string, string>> options, string atFolder, IDictionary<char, string> shortOptions, InvertibleSet<string> twoArgOptions, int argLimit, bool expandEnvVars, bool caseSensitiveLongOpts)
		{
			string s = args[i];
			if (s == null)
				return;
			if (expandEnvVars)
				args[i] = s = Environment.ExpandEnvironmentVariables(s);

			if (s.StartsWith("-")) {
				Match m = CmdLineLongOptRegex.Match(s);
				if (m.Success)
				{
					// it's an --option
					string name = m.Groups[1].ToString();
					string value = m.Groups[3].ToString();
					if (value == "" && m.Groups[2].ToString() == "")
						value = null; // no value present (value=="" means value is present but empty)

					if (!caseSensitiveLongOpts)
						name = name.ToLowerInvariant();

					args[i] = null;
					if (twoArgOptions != null && twoArgOptions.Contains(name))
						MaybeRemoveArg(args, i + 1, expandEnvVars, ref value);
					AddPair(options, s, name, value);
				}
				else if (shortOptions != null)
				{
					// Check if short option(s) are valid
					bool reject = false;
					int div;
					char ch = '\0';
					for (div = 1; div < s.Length; div++) {
						ch = s[div];
						if (!shortOptions.ContainsKey(ch)) {
							if (char.IsLetter(ch))
								reject = true;
							break;
						}
					}

					int afterDiv = (ch == ':' || ch == '=' ? div + 1 : div);
					string value = div < s.Length ? s.Substring(afterDiv) : null;

					if (div > 1 && !reject) // is s entirely valid?
					{
						// detect space-separated argument
						ch = s[div - 1];
						if (twoArgOptions != null && twoArgOptions.Contains(shortOptions[ch] ?? ch.ToString()))
							MaybeRemoveArg(args, i + 1, expandEnvVars, ref value);

						args[i] = null;
						for (int c = 1; c < div; c++) {
							ch = s[c];
							string longName = shortOptions[ch] ?? ch.ToString();
							string curValue = c + 1 == div ? value : null;
							AddPair(options, s, longName, curValue);
						}
					}
				}
			}
			else if (atFolder != null && s.StartsWith("@"))
			{
				// e.g. "@list of options.txt"
				string atFile = s.Substring(1);
				try {
					string fullpath = Path.Combine(atFolder, atFile);
					if (File.Exists(fullpath)) {
						string fileContents = File.OpenText(fullpath).ReadToEnd();
						List<string> list = G.SplitCommandLineArguments(fileContents);

						int maxMore = System.Math.Max(0, argLimit - args.Count);
						if (list.Count > maxMore) {
							// oops, command limit exceeded
							MessageSink.Current.Write(MessageSink.Warning, s, "Limit of {0} commands exceeded", argLimit);
							list.RemoveRange(maxMore, list.Count - maxMore);
						}

						args.InsertRange(i + 1, list);
					}
				} catch (Exception e) {
					MessageSink.Current.Write(MessageSink.Error, s, "Unable to use option file \"{0}\": {1}", atFile, e.Message);
				}
			}
		}

		private static void MaybeRemoveArg(IList<string> args, int i, bool expandEnvVars, ref string value)
		{
			if (value == null && i < args.Count && !args[i].StartsWith("-"))
			{
				value = args[i];
				if (expandEnvVars)
					value = Environment.ExpandEnvironmentVariables(value);
				args[i] = null;
			}
		}

		private static void AddPair(ICollection<KeyValuePair<string, string>> options, string option, string name, string value)
		{
			try {
				options.Add(new KeyValuePair<string, string>(name.ToLower(), value));
			} catch {
				MessageSink.Current.Write(MessageSink.Warning, option,
					"Option --{0} was specified more than once. Only the first instance is used.", name);
			}
		}
	}

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
			w.WriteLine("\"@"+file2+"\" fox--jumps\n--over");
			w.Close();
			w = new StreamWriter(Path.Combine(atFolder, file2));
			w.WriteLine("these arguments are ignored (arg limit exceeded)");
			w.Close();

			// Expand command line and ensure that the arg limit is enforced
			List<string> args = G.SplitCommandLineArguments("\"@"+file1+"\" \"lazy dog\"");
			var options = new DList<KeyValuePair<string, string>>();
			var msgs = new MessageHolder();
			using (MessageSink.PushCurrent(msgs))
				UG.ProcessCommandLineArguments(args, options, atFolder, null, null, 5);

			ExpectList(args.AsListSource(), "@"+file1, "@"+file2, "fox--jumps", "lazy dog");
			ExpectList(options, P("over", null));
			ExpectList(msgs.List.Select(msg => msg.ToString()).Buffered(),
				"@test ProcessCmdLine 2.txt: Warning: Limit of 5 commands exceeded");
		}
	}
}
