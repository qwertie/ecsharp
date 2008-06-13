using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Runtime;

namespace Loyc.Utilities
{
	public delegate void OutputWriter(Symbol msgType, Symbol msgId, [Localizable] string message, params object[] args);

	public class Output
	{
		static ThreadLocalVariable<OutputWriter> Writer = new ThreadLocalVariable<OutputWriter>(ConsoleOutputWriter.Write);

		public static void Write(string message) { Write(null, message, (object[])null); }
		public static void Write(string message, params object[] args) { Write(null, message, args); }

		public static void Write(Symbol msgId) { Write(null, msgId, (object[])null); }
		public static void Write(Symbol msgId, params object[] args) { Write(null, msgId, args); }

		public static void Write(Symbol msgType, string message) { Write(msgType, message, (object[])null); }
		public static void Write(Symbol msgType, string message, params object[] args)
		{
			Writer.Value(msgType, null, message, args);
		}
		public static void Write(Symbol msgType, Symbol msgId) { Write(msgType, msgId, (object[])null); }
		public static void Write(Symbol msgType, Symbol msgId, params object[] args)
		{
			Writer.Value(msgType, msgId, null, args);
		}
	}

	public class ConsoleOutputWriter
	{
		protected static ConsoleColor _lastColor;
		protected static readonly Symbol Error = Symbol.Get("Error");
		protected static readonly Symbol Warning = Symbol.Get("Warning");
		protected static readonly Symbol Note = Symbol.Get("Note");
		protected static readonly Symbol Verbose = Symbol.Get("Verbose");
		protected static readonly Symbol Detail = Symbol.Get("Detail");

		protected static ConsoleColor PickColor(Symbol msgType, out string msgTypeText)
		{
			bool implicitText = false;
			ConsoleColor color;

			if (msgType == Error)
			{
				color = ConsoleColor.Red;
				implicitText = true;
			}
			else if (msgType == Warning)
			{
				color = ConsoleColor.Yellow;
				implicitText = true;
			}
			else if (msgType == Note)
				color = ConsoleColor.White;
			else if (msgType == Verbose)
				color = ConsoleColor.Gray;
			else if (msgType == Detail) {
				switch (_lastColor)
				{
					case ConsoleColor.Red: color = ConsoleColor.DarkRed; break;
					case ConsoleColor.Yellow: color = ConsoleColor.DarkYellow; break;
					case ConsoleColor.White: color = ConsoleColor.Gray; break;
					case ConsoleColor.Green: color = ConsoleColor.DarkGreen; break;
					case ConsoleColor.Blue: color = ConsoleColor.DarkBlue; break;
					case ConsoleColor.Magenta: color = ConsoleColor.DarkMagenta; break;
					case ConsoleColor.Cyan: color = ConsoleColor.DarkCyan; break;
					default: color = ConsoleColor.DarkGray; break;
				}
			} else
				color = Console.ForegroundColor;

			msgTypeText = implicitText ? null : Localize.From(msgType.Name);

			return color;
		}

		public static void Write(Symbol msgType, Symbol msgId, [Localizable] string message, params object[] args)
		{
			string result = Localize.From(msgId, message, args);
			string typeText;
			ConsoleColor oldColor = Console.ForegroundColor;
			Console.ForegroundColor = PickColor(msgType, out typeText);
			if (typeText == null)
				Console.WriteLine(result);
			else
				Console.WriteLine(typeText + ": " + result);
			Console.ForegroundColor = oldColor;
		}
	}
}
