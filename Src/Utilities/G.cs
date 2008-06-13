using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Loyc.Runtime;
using System.IO;
using System.Text.RegularExpressions;
using Loyc.Compatibility.Linq;
using System.Collections;

namespace Loyc.Utilities
{
	public delegate string WriterDelegate(string format, params object[] args);

	/// <summary>Contains global functions that don't really belong in any class.</summary>
	public static class G
	{
		public static PoorMansLinq<T> Linq<T>(IEnumerable<T> source)
		{
			return new PoorMansLinq<T>(source);
		}
		public static PoorMansLinq<T> Cast<T>(IEnumerable source)
		{
			return new PoorMansLinq<T>(Enumerable.Cast<T>(source));
		}
		public static PoorMansLinq<T> OfType<T>(IEnumerable source)
		{
			return new PoorMansLinq<T>(Enumerable.OfType<T>(source));
		}

		public static void Swap<T>(ref T a, ref T b)
		{
			T tmp = a;
			a = b;
			b = tmp;
		}
		public static int InRange(int n, int min, int max)
		{
			if (n >= min)
				if (n <= max)
					return n;
				else
					return max;
			else
				return min;
		}
		public static double InRange(double n, double min, double max)
		{
			if (n >= min)
				if (n <= max)
					return n;
				else
					return max;
			else
				return min;
		}
		public static float InRange(float n, float min, float max)
		{
			if (n >= min)
				if (n <= max)
					return n;
				else
					return max;
			else
				return min;
		}
		public static bool IsInRange(int n, int min, int max)
		{
			return n >= min && n <= max;
		}
		public static bool IsInRange(double n, double min, double max)
		{
			return n >= min && n <= max;
		}
		public static bool IsInRange(float n, float min, float max)
		{
			return n >= min && n <= max;
		}
		public static int BinarySearch<T>(IList<T> list, T value, Comparison<T> pred)
		{
			int lo = 0, hi = list.Count, i;
			while(lo < hi) 
			{
				i = (lo+hi)/2;
				int cmp = pred(list[i], value);
				if (cmp < 0)
					lo = i+1;
				else if (cmp > 0)
					hi = i;
				else
					return i;
			}
			return ~lo;
		}
		public static int BinarySearch<T>(IList<T> list, T value) where T : IComparable<T>
		{
			return BinarySearch<T>(list, value, ToComparison<T>());
		}
		public static int BinarySearch<T>(IList<T> list, T value, IComparer<T> pred)
		{
			return BinarySearch<T>(list, value, ToComparison(pred));
		}
		public static Comparison<T> ToComparison<T>(IComparer<T> pred)
		{
			return delegate(T a, T b) { return pred.Compare(a, b); };
		}
		public static Comparison<T> ToComparison<T>() where T : IComparable<T>
		{
			return delegate(T a, T b) { return a.CompareTo(b); };
		}
		public static List<string> SplitCommandLineArguments(string listString)
		{
			List<string> list = new List<string>();
			string regex = "(?=[^\\s\n])" // Match at least one non-whitespace character
			             + "[^\\s\n\"]*"  // Optional unquoted text
						   // (Optional quoted text, then optional unquoted text)*
			             + "(\"[^\"\n]*(\"[^\\s\n\"]*)?)*";

			Match m = Regex.Match(listString, regex, RegexOptions.IgnorePatternWhitespace);
			for (; m.Success; m = m.NextMatch()) {
				string s = m.ToString();
				if (s.StartsWith("\"") && s.EndsWith("\""))
					s = s.Substring(1, s.Length - 2);
				list.Add(s);
			}
			return list;
		}

        /// <summary>
        /// Expands environment variables (e.g. %TEMP%) and @files in a list of
        /// command-line arguments, and adds any options of the form "--opt" or
        /// "--opt=value" to a dictionary.
        /// </summary>
        /// <param name="args">The original arguments to process</param>
        /// <param name="options">Any --options found will go here, and their keys
        /// will be converted to lowercase, unless this parameter is null. Note that
        /// options are not removed or converted to lowercase in the original args
        /// list.</param>
        /// <param name="atPath">If a parameter has the form @filename, the folder
        /// specified by atPath will be searched for an options text file with that
        /// filename, and the contents of the file will be expanded into the list of
        /// arguments (split using SplitCommandLineArguments).</param>
        /// <param name="argLimit">A limit placed on the number of arguments when
        /// expanding @files. Such a file may refer to itself, and this is the only
        /// protection provided against infinite recursive expansion.</param>
        /// <remarks>
        /// Options are expected to have the form -ID=value, where ID matches the
        /// regex "[a-zA-Z_0-9]+". If there is no "=", that's okay too. For example,
        /// --ID{foo} is equivalent to --Id={foo}; both result in the name-value
        /// pair ("id", "{foo}").
        /// </remarks>
		public static void ProcessCommandLineArguments(List<string> args, Dictionary<string, string> options, string atPath, int argLimit)
		{
			for (int i = 0; i < args.Count; i++)
				if (ProcessArgument(args, i, options, atPath, argLimit))
					i--; // redo
		}
		public static readonly Regex CmdLineArgRegex = new Regex(@"^--([a-zA-Z_0-9]+)([=]?(.*))?$");

		private static bool ProcessArgument(List<string> args, int i, Dictionary<string, string> pairs, string atPath, int argLimit)
		{
			string s = args[i];
			args[i] = s = Environment.ExpandEnvironmentVariables(s);

			if (pairs != null) {
				Match m = CmdLineArgRegex.Match(s);
				if (m.Success) {
					// it's an --option
					string name = m.Groups[1].ToString();
					string value = m.Groups[3].ToString();
					try {
						pairs.Add(name.ToLower(), value);
					} catch {
						Output.Warning("Option {0} was specified more than once. The first value, {1}, will be used.",
							name, pairs[name]);
					}
				}
			}
			if (atPath != null && s.StartsWith("@")) {
				// e.g. "@list of options.txt"
				try {
					string fullpath = Path.Combine(atPath, s.Substring(1));
					if (File.Exists(fullpath))
					{
						string fileContents = File.OpenText(fullpath).ReadToEnd();
						List<string> list = G.SplitCommandLineArguments(fileContents);
						
						args.RemoveAt(i);
						
						int maxMore = Math.Max(0, argLimit - args.Count);
						if (list.Count > maxMore) {
							// oops, command limit exceeded
							Output.Warning("{0}: Limit of {1} commands exceeded", s, argLimit);
							list.RemoveRange(maxMore, list.Count - maxMore);
						}
						
						args.InsertRange(i, list);

						return true;
					}
				} catch (Exception e) {
					Output.Error(s + ": " + e.Message);
				}
			}
			return false;
		}
	}
	[TestFixture]
	public class GTests
	{
		[Test] public void TestSwap()
		{
			int a = 7, b = 13;
			G.Swap(ref a, ref b);
			Assert.AreEqual(7, b);
			Assert.AreEqual(13, a);
		}
		[Test] public void TestInRange()
		{
			Assert.IsFalse(G.IsInRange(1,2,5));
			Assert.IsTrue(G.IsInRange(2,2,5));
			Assert.IsTrue(G.IsInRange(3,2,5));
			Assert.IsTrue(G.IsInRange(4,2,5));
			Assert.IsTrue(G.IsInRange(5,2,5));
			Assert.IsFalse(G.IsInRange(6,2,5));
			Assert.IsFalse(G.IsInRange(2,5,2));
			Assert.IsFalse(G.IsInRange(3,5,2));
			Assert.IsFalse(G.IsInRange(5,5,2));
		}
		[Test] public void InRange()
		{
			Assert.AreEqual(2, G.InRange(-1, 2, 5));
			Assert.AreEqual(2, G.InRange(1, 2, 5));
			Assert.AreEqual(2, G.InRange(2, 2, 5));
			Assert.AreEqual(3, G.InRange(3, 2, 5));
			Assert.AreEqual(4, G.InRange(4, 2, 5));
			Assert.AreEqual(5, G.InRange(5, 2, 5));
			Assert.AreEqual(5, G.InRange(6, 2, 5));
		}
		[Test] public void TestBinarySearch()
		{
			int[] list = new int[] { };
			Assert.AreEqual(~0, G.BinarySearch(list, 15));
			Assert.AreEqual(~0, G.BinarySearch(list, -15));
			list = new int[] { 5 };
			Assert.AreEqual(0, G.BinarySearch(list, 5));
			Assert.AreEqual(~0, G.BinarySearch(list, 0));
			Assert.AreEqual(~1, G.BinarySearch(list, 10));
			list = new int[] { 5, 7 };
			Assert.AreEqual(~0, G.BinarySearch(list, 0));
			Assert.AreEqual( 0, G.BinarySearch(list, 5));
			Assert.AreEqual(~1, G.BinarySearch(list, 6));
			Assert.AreEqual( 1, G.BinarySearch(list, 7));
			Assert.AreEqual(~2, G.BinarySearch(list, 10));
			list = new int[] { 1, 5, 7, 13, 17, 29, 29, 31 };
			Assert.AreEqual(~0, G.BinarySearch(list, -1));
			Assert.AreEqual( 0, G.BinarySearch(list, 1));
			Assert.AreEqual(~1, G.BinarySearch(list, 2));
			Assert.AreEqual( 1, G.BinarySearch(list, 5));
			Assert.AreEqual(~2, G.BinarySearch(list, 6));
			Assert.AreEqual( 2, G.BinarySearch(list, 7));
			Assert.AreEqual(~3, G.BinarySearch(list, 10));
			Assert.AreEqual( 3, G.BinarySearch(list, 13));
			Assert.AreEqual(~4, G.BinarySearch(list, 16));
			Assert.AreEqual( 4, G.BinarySearch(list, 17));
			Assert.AreEqual(~5, G.BinarySearch(list, 28));
			int i = G.BinarySearch(list, 29);
			Assert.IsTrue(i == 5 || i == 6);
			Assert.AreEqual(~7, G.BinarySearch(list, 30));
			Assert.AreEqual( 7, G.BinarySearch(list, 31));
			Assert.AreEqual(~8, G.BinarySearch(list, 1000));
		}
		[Test] public void TestPredicatedBinarySearch()
		{
			Comparison<int> p = G.ToComparison<int>();
			int[] list = new int[] { };
			Assert.AreEqual(~0, G.BinarySearch<int>(list, 15, p));
			Assert.AreEqual(~0, G.BinarySearch<int>(list, -15, p));
			list = new int[] { 5 };
			Assert.AreEqual(0, G.BinarySearch<int>(list, 5, p));
			Assert.AreEqual(~0, G.BinarySearch<int>(list, 0, p));
			Assert.AreEqual(~1, G.BinarySearch<int>(list, 10, p));
			list = new int[] { 5, 7 };
			Assert.AreEqual(~0, G.BinarySearch<int>(list, 0, p));
			Assert.AreEqual( 0, G.BinarySearch<int>(list, 5, p));
			Assert.AreEqual(~1, G.BinarySearch<int>(list, 6, p));
			Assert.AreEqual( 1, G.BinarySearch<int>(list, 7, p));
			Assert.AreEqual(~2, G.BinarySearch<int>(list, 10, p));
			list = new int[] { 1, 5, 7, 13, 17, 29, 29, 31 };
			Assert.AreEqual(~0, G.BinarySearch<int>(list, -1, p));
			Assert.AreEqual( 0, G.BinarySearch<int>(list, 1, p));
			Assert.AreEqual(~1, G.BinarySearch<int>(list, 2, p));
			Assert.AreEqual( 1, G.BinarySearch<int>(list, 5, p));
			Assert.AreEqual(~2, G.BinarySearch<int>(list, 6, p));
			Assert.AreEqual( 2, G.BinarySearch<int>(list, 7, p));
			Assert.AreEqual(~3, G.BinarySearch<int>(list, 10, p));
			Assert.AreEqual( 3, G.BinarySearch<int>(list, 13, p));
			Assert.AreEqual(~4, G.BinarySearch<int>(list, 16, p));
			Assert.AreEqual( 4, G.BinarySearch<int>(list, 17, p));
			Assert.AreEqual(~5, G.BinarySearch<int>(list, 28, p));
			int i = G.BinarySearch<int>(list, 29, p);
			Assert.IsTrue(i == 5 || i == 6);
			Assert.AreEqual(~7, G.BinarySearch<int>(list, 30, p));
			Assert.AreEqual( 7, G.BinarySearch<int>(list, 31, p));
			Assert.AreEqual(~8, G.BinarySearch<int>(list, 1000, p));
		}
		[Test] public void TestSplitCommandLineArguments()
		{
			// Give it some easy and some difficult arguments
			string input = "123: apple \t banana=\"a b\" Carrot\n"
				+ "!@#$%^&*() \"duck\" \"error \"foo\"!\ngrape   ";
			List<string> expected = new List<string>();
			expected.Add("123:");
			expected.Add("apple");
			expected.Add("banana=\"a b\"");
			expected.Add("Carrot");
			expected.Add("!@#$%^&*()");
			expected.Add("duck");
			expected.Add("\"error \"foo\"!");
			expected.Add("grape");
			
			List<string> output = G.SplitCommandLineArguments(input);
			Assert.AreEqual(output.Count, expected.Count);
			for (int i = 0; i < expected.Count; i++)
				Assert.AreEqual(output[i], expected[i]);
		}
		[Test] public void TestProcessCommandLineArguments()
		{
			// TODO: trap warning message generated by ExpandCommandLineArguments

			// Generate two options files, where the first refers to the second
			string atPath = Environment.ExpandEnvironmentVariables("%TEMP%");
			string file1 = "test_g_expand_1.txt";
			string file2 = "test_g_expand_2.txt";
			StreamWriter w = new StreamWriter(Path.Combine(atPath, file1));
			w.WriteLine("@"+file2+" fox--jumps\n--over the hill");
			w.Close();
			w = new StreamWriter(Path.Combine(atPath, file2));
			w.WriteLine("\"%TEMP%\"");
			w.Close();

			// Expand command line and ensure that the arg limit of 4 is enforced
			List<string> args = G.SplitCommandLineArguments("\"@"+file1+"\" \"lazy dog\"");
			Dictionary<string, string> pairs = new Dictionary<string, string>();
			G.ProcessCommandLineArguments(args, pairs, atPath, 4);

			Assert.AreEqual(4, args.Count);
			Assert.AreEqual(args[0], atPath);
			Assert.AreEqual(args[1], "fox--jumps");
			Assert.AreEqual(args[2], "--over");
			Assert.AreEqual(args[3], "lazy dog");
			Assert.AreEqual(1, pairs.Count);
			Assert.AreEqual(pairs["over"], "");
		}
	}
}
