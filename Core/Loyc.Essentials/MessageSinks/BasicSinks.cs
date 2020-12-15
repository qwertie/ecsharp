//
// This file defines simple standard message sinks:
//
//   - ConsoleMessageSink, 
//   - NullMessageSink,
//   - TraceMessageSink,
//   - MessageSinkFromDelegate
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Loyc.Collections;

namespace Loyc
{
	/// <summary>Sends all messages to <see cref="System.Console.WriteLine()"/>, 
	/// with hard-coded colors for Error, Warning, Note, Verbose, and Detail.</summary>
	public class ConsoleMessageSink : IMessageSink
	{
		public static readonly ConsoleMessageSink Value = new ConsoleMessageSink();

		protected static ConsoleColor _lastColor;

		public ConsoleMessageSink() { PrintSeverityAt = Severity.Warning; }

		/// <summary>Minimum severity level at which the severity level is printed.
		/// For example, if set to Severity.Error, then the prefix "Error:" is printed
		/// for errors but the prefix "Warning:" is not printed for warnings.</summary>
		/// <remarks>Initial value: Severity.Warning</remarks>
		public Severity PrintSeverityAt { get; set; }

		protected virtual ConsoleColor PickColor(Severity level, out string levelText)
		{
			bool isDetail = ((int)level & 1) != 0;
			bool implicitLevel = level < PrintSeverityAt || isDetail;
			ConsoleColor color;

			if (level >= Severity.CriticalDetail)
				color = isDetail ? ConsoleColor.DarkMagenta : ConsoleColor.Magenta;
			else if (level >= Severity.ErrorDetail)
				color = isDetail ? ConsoleColor.DarkRed : ConsoleColor.Red;
			else if (level >= Severity.WarningDetail)
				color = isDetail ? ConsoleColor.DarkYellow : ConsoleColor.Yellow;
			else if (level >= Severity.NoteDetail)
				color = isDetail ? ConsoleColor.Gray : ConsoleColor.White;
			else if (level >= Severity.InfoDetail)
				color = isDetail ? ConsoleColor.DarkGreen : ConsoleColor.Green;
			else if (level >= Severity.DebugDetail)
				color = isDetail ? ConsoleColor.DarkCyan : ConsoleColor.Cyan;
			else if (level >= Severity.VerboseDetail)
				color = isDetail ? ConsoleColor.DarkBlue : ConsoleColor.DarkCyan;
			else
				color = Console.ForegroundColor;

			levelText = implicitLevel ? null : level.ToString().Localized();
			_lastColor = color;
			return color;
		}

		public void Write(Severity level, object context, string format)
		{
			WriteCore(level, context, format.Localized());
		}
		public void Write(Severity level, object context, string format, object arg0, object arg1 = null)
		{
			WriteCore(level, context, format.Localized(arg0, arg1));
		}
		public void Write(Severity level, object context, string format, params object[] args)
		{
			WriteCore(level, context, format.Localized(args));
		}
		void WriteCore(Severity level, object context, string text)
		{
			string typeText;
			var color = PickColor(level, out typeText);
			if (typeText != null)
				text = typeText + ": " + text;
			WriteColoredMessage(color, context, text);
		}
		public static void WriteColoredMessage(ConsoleColor color, object context, string text)
		{
			string loc = MessageSink.ContextToString(context);
			if (!string.IsNullOrEmpty(loc))
				Console.Write(loc + ": ");

			ConsoleColor oldColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.WriteLine(text);
			Console.ForegroundColor = oldColor;
		}

		/// <summary>Always returns true.</summary>
		public bool IsEnabled(Severity level)
		{
			return true;
		}
	}

	/// <summary>Discards all messages. However, there is a Count property that 
	/// increases by one with each message received, as well as an ErrorCount.</summary>
	public sealed class NullMessageSink : IMessageSink, ICount
	{
		public static readonly NullMessageSink Value = new NullMessageSink();

		int _count, _errorCount;
		/// <summary>Total number of messages that have been discarded.</summary>
		public int Count { get { return _count; } }
		/// <summary>Number of errors sent to this object so far, not including detail messages.</summary>
		public int ErrorCount { get { return _errorCount; } }
		public bool IsEmpty { get { return _count == 0; } }
		public void ResetCountersToZero() { _count = _errorCount = 0; }

		public void Write(Severity level)
		{
			_count++;
			if (level >= Severity.Error && ((int) level & 1) == 0)
				_errorCount++;
		}
		public void Write(Severity level, object context, string format) => Write(level);
		public void Write(Severity level, object context, string format, object arg0, object arg1 = null) => Write(level);
		public void Write(Severity level, object context, string format, params object[] args) => Write(level);
		
		/// <summary>Always returns false.</summary>
		public bool IsEnabled(Severity level) => false;
	}

	/// <summary>Sends all messages to <see cref="System.Diagnostics.Trace.WriteLine(string)"/>.</summary>
	public class TraceMessageSink : IMessageSink
	{
		public static readonly TraceMessageSink Value = new TraceMessageSink();

		public void Write(Severity type, object context, string format)
		{
			WriteCore(type, context, Localize.Localized(format));
		}
		public void Write(Severity type, object context, string format, object arg0, object arg1 = null)
		{
			WriteCore(type, context, Localize.Localized(format, arg0, arg1));
		}
		public void Write(Severity type, object context, string format, params object[] args)
		{
			WriteCore(type, context, Localize.Localized(format, args));
		}
		public void WriteCore(Severity type, object context, string text)
		{
			string loc = MessageSink.ContextToString(context);
			if (!string.IsNullOrEmpty(loc))
				text = loc + ": " + text;
			Trace.WriteLine(text, type.ToString());
		}
		/// <summary>Always returns true.</summary>
		public bool IsEnabled(Severity type)
		{
			return true;
		}
	}

	/// <summary>This helper class lets you implement <see cref="IMessageSink"/> 
	/// with one or two delegates (a writer method, and an optional severity filter).</summary>
	public class MessageSinkFromDelegate : IMessageSink
	{
		WriteMessageFn _writer;
		Func<Severity, bool> _isEnabled;

		/// <summary>Initializes this object.</summary>
		/// <param name="writer">Required. A method that accepts output.</param>
		/// <param name="isEnabled">Optional. A method that decides whether to 
		/// output based on the message type. If this parameter is provided,
		/// then <see cref="Write"/>() will not invoke the writer when isEnabled
		/// returns false. This delegate is also called by <see cref="IsEnabled"/>().</param>
		public MessageSinkFromDelegate(WriteMessageFn writer, Func<Severity, bool> isEnabled = null)
		{
			CheckParam.IsNotNull("writer", writer);
			_writer = writer;
			_isEnabled = isEnabled;
		}

		public void Write(Severity level, object context, string format)
		{
			if (IsEnabled(level))
				_writer(level, context, format, EmptyArray<object>.Value);
		}
		public void Write(Severity level, object context, string format, object arg0, object arg1 = null)
		{
			if (IsEnabled(level))
				_writer(level, context, format, new[] { arg0, arg1 });
		}
		public void Write(Severity level, object context, string format, params object[] args)
		{
			if (IsEnabled(level))
				_writer(level, context, format, args);
		}

		public bool IsEnabled(Severity level)
		{
			return _isEnabled != null ? _isEnabled(level) : true;
		}
	}
}