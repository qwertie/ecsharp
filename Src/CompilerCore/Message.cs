using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Runtime;

namespace Loyc.CompilerCore
{
	public delegate void MessageWriter(Symbol msgType, SourcePos pos, Symbol msgId, string lang, string message, params object[] args);

	/// <summary>
	/// Trace is a simple message-output facility that can provide message 
	/// translation at the same time. It is a singleton (static class) but doesn't
	/// output the messages itself; rather, it allows multiple MessageWriters to 
	/// listen for messages, and relies upon them to output the messages.
	/// </summary><remarks>
	/// TODO: Currently, no one initializes Message, so it relies on 
	/// ConsoleMessageWriter as a fallback to write messages. This should be
	/// changed soon so that if there are no event handlers, nothing happens at 
	/// all.
	/// 
	/// In Loyc, you can use the Error, Warning, Note, Detail and Verbose classes
	/// to output messages that have the same msgType as the class name. For example,
	/// <c>Warning.WriteEN(position, message)</c> is equivalent to 
	/// <c>Message.WriteEN(:Warning, position, message)</c>.
	/// 
	/// TODO: consider how to support "warning levels" and "verbose levels" so user
	/// can hide all messages below a specified priority.
	/// </remarks>
	public class Message
	{
		// TODO: perhaps thread local storage should be used, one MessageWriter event per thread?
		public static event MessageWriter WriteMessage;

		public static void Write(Symbol msgType, SourcePos pos, string message, params object[] args)
			{ Write(msgType, pos, "EN", message, args); }
		public static void Write(Symbol msgType, SourcePos pos, string lang, string message, params object[] args)
		{
			if (WriteMessage != null)
				WriteMessage(msgType, pos, null, lang, message, args); 
			else
				ConsoleMessageWriter.WriteLine(msgType, pos, msgType, "EN", message, args);
		}
		public static void Write(Symbol msgType, SourcePos pos, Symbol msgId, params object[] args)
		{
			if (WriteMessage != null)
				WriteMessage(msgType, pos, msgId, null, null, args); 
			else
				ConsoleMessageWriter.WriteLine(msgType, pos, msgId, null, null, args);
		}

		public bool HasAnyHandlers { get { return WriteMessage != null; } }
	}
	public class Warning : Message
	{
		public static readonly Symbol MsgType = Symbol.Get("Warning");

		public static void WriteEN(SourcePos pos, string message, params object[] args)
			{ Write(MsgType, pos, "EN", message, args); }
		public static void Write(SourcePos pos, string lang, string message, params object[] args)
			{ Write(MsgType, pos, lang, message, args); }
		public static void Write(SourcePos pos, Symbol msgId, params object[] args)
			{ Write(MsgType, pos, msgId, args); }
	}
	public class Error : Message
	{
		public static readonly Symbol MsgType = Symbol.Get("Error");

		public static void WriteEN(SourcePos pos, string message, params object[] args)
			{ Write(MsgType, pos, "EN", message, args); }
		public static void Write(SourcePos pos, string lang, string message, params object[] args)
			{ Write(MsgType, pos, lang, message, args); }
		public static void Write(SourcePos pos, Symbol msgId, params object[] args)
			{ Write(MsgType, pos, msgId, args); }
	}
	public class Note : Message
	{
		public static readonly Symbol MsgType = Symbol.Get("Note");

		public static void WriteEN(SourcePos pos, string message, params object[] args)
			{ Write(MsgType, pos, "EN", message, args); }
		public static void Write(SourcePos pos, string lang, string message, params object[] args)
			{ Write(MsgType, pos, lang, message, args); }
		public static void Write(SourcePos pos, Symbol msgId, params object[] args)
			{ Write(MsgType, pos, msgId, args); }
	}
	public class Detail : Message
	{
		public static readonly Symbol MsgType = Symbol.Get("Detail");

		public static void WriteEN(SourcePos pos, string message, params object[] args)
			{ Write(MsgType, pos, "EN", message, args); }
		public static void Write(SourcePos pos, string lang, string message, params object[] args)
			{ Write(MsgType, pos, lang, message, args); }
		public static void Write(SourcePos pos, Symbol msgId, params object[] args)
			{ Write(MsgType, pos, msgId, args); }
	}
	public class Verbose : Message
	{
		public static readonly Symbol MsgType = Symbol.Get("Verbose");

		public static void WriteEN(SourcePos pos, string message, params object[] args)
			{ Write(MsgType, pos, "EN", message, args); }
		public static void Write(SourcePos pos, string lang, string message, params object[] args)
			{ Write(MsgType, pos, lang, message, args); }
		public static void Write(SourcePos pos, Symbol msgId, params object[] args)
			{ Write(MsgType, pos, msgId, args); }
	}

	public class ConsoleMessageWriter
	{
		private static ConsoleColor lastColor;
		public static void WriteLine(Symbol msgType, SourcePos pos, Symbol msgId, string lang, string message, params object[] args)
		{
			string result = Localize.From(msgId, lang, message, args);
			
			ConsoleColor oldColor = Console.ForegroundColor;
			if (msgType == Error.MsgType)
				Console.ForegroundColor = ConsoleColor.Red;
			else if (msgType == Warning.MsgType) 
				Console.ForegroundColor = ConsoleColor.Yellow;
			else if (msgType == Note.MsgType)
				Console.ForegroundColor = ConsoleColor.White;
			else if (msgType == Verbose.MsgType)
				Console.ForegroundColor = ConsoleColor.Gray;
			else if (msgType == Detail.MsgType) {
				switch (lastColor) {
					case ConsoleColor.Red: Console.ForegroundColor = ConsoleColor.DarkRed; break;
					case ConsoleColor.Yellow: Console.ForegroundColor = ConsoleColor.DarkYellow; break;
					case ConsoleColor.White: Console.ForegroundColor = ConsoleColor.Gray; break;
					case ConsoleColor.Green: Console.ForegroundColor = ConsoleColor.DarkGreen; break;
					case ConsoleColor.Blue: Console.ForegroundColor = ConsoleColor.DarkBlue; break;
					case ConsoleColor.Magenta: Console.ForegroundColor = ConsoleColor.DarkMagenta; break;
					case ConsoleColor.Cyan: Console.ForegroundColor = ConsoleColor.DarkCyan; break;
				}
			}
			if (msgType == Error.MsgType)
				Console.WriteLine("{0} Error: {1}", pos.ToString(), result);
			else
				Console.WriteLine("{0} {1}", pos.ToString(), result);

			lastColor = Console.ForegroundColor;
			Console.ForegroundColor = oldColor;
		}
	}
}
