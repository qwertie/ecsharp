using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Runtime;

namespace Loyc.Utilities
{
	public static class Output
	{
		public static void RawLine(string text)
		{
			Console.WriteLine(text);
		}
		public static void RawLine(string text, object arg1)
		{
			Console.WriteLine(text, arg1);
		}
		public static void RawLine(string text, params object[] args)
		{
			Console.WriteLine(text, args);
		}
		public static void Message(Symbol type, string text)
		{
			RawLine(Localize.From(type.Name) + ": " + Localize.From(text));
		}
		public static void Message(Symbol type, string text, object arg1)
		{
			RawLine(Localize.From(type.Name) + ": " + Localize.From(text, arg1));
		}
		public static void Message(Symbol type, string text, params object[] args)
		{
			RawLine(Localize.From(type.Name) + ": " + Localize.From(text, args));
		}

		static Symbol _Error = Symbol.Get("Error");
		static Symbol _Warning = Symbol.Get("Warning");
		static Symbol _Note = Symbol.Get("Note");
		static Symbol _Detail = Symbol.Get("Detail");
		static Symbol _Verbose = Symbol.Get("Verbose");

		public static void Error(string text, params object[] args) { Message(_Error, text, args); }
		public static void Warning(string text, params object[] args) { Message(_Warning, text, args); }
		public static void Note(string text, params object[] args) { Message(_Note, text, args); }
		public static void Detail(string text, params object[] args) { Message(_Detail, text, args); }
		public static void Verbose(string text, params object[] args) { Message(_Verbose, text, args); }
	}
}
