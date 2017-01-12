//
// This file defines several standard message sinks:
//     ConsoleMessageSink, 
//     NullMessageSink,
//     TraceMessageSink,
//     MessageHolder,
//     MessageFilter,
//     MulticastMessageSink
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Collections.Impl;
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

		protected virtual ConsoleColor PickColor(Severity msgType, out string msgTypeText)
		{
			bool implicitText = msgType < PrintSeverityAt;
			ConsoleColor color;

			if (msgType >= Severity.Critical)
				color = ConsoleColor.Magenta;
			else if (msgType >= Severity.Error)
				color = ConsoleColor.Red;
			else if (msgType >= Severity.Warning)
				color = ConsoleColor.Yellow;
			else if (msgType >= Severity.Note)
				color = ConsoleColor.White;
			else if (msgType >= Severity.Debug)
				color = ConsoleColor.Cyan;
			else if (msgType >= Severity.Verbose || msgType == Severity._Finer)
				color = ConsoleColor.DarkCyan;
			else if (msgType == Severity.Detail)
			{
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
				msgTypeText = null;
				return color;
			} else
				color = Console.ForegroundColor;

			msgTypeText = implicitText ? null : msgType.ToString().Localized();
			_lastColor = color;
			return color;
		}

		public void Write(Severity type, object context, string format)
		{
			WriteCore(type, context, format.Localized());
		}
		public void Write(Severity type, object context, string format, object arg0, object arg1 = null)
		{
			WriteCore(type, context, format.Localized(arg0, arg1));
		}
		public void Write(Severity type, object context, string format, params object[] args)
		{
			WriteCore(type, context, format.Localized(args));
		}
		void WriteCore(Severity type, object context, string text)
		{
			string typeText;
			var color = PickColor(type, out typeText);
			if (typeText != null)
				text = typeText + ": " + text;
			WriteColoredMessage(color, context, text);
		}
		public static void WriteColoredMessage(ConsoleColor color, object context, string text)
		{
			string loc = MessageSink.LocationString(context);
			if (!string.IsNullOrEmpty(loc))
				Console.Write(loc + ": ");

			ConsoleColor oldColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.WriteLine(text);
			Console.ForegroundColor = oldColor;
		}

		/// <summary>Always returns true.</summary>
		public bool IsEnabled(Severity type)
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
		public int Count { get { return _errorCount; } }
		public int ErrorCount { get { return _count; } }
		public bool IsEmpty { get { return _count == 0; } }
		public void ResetCountersToZero() { _count = _errorCount = 0; }

		public void Write(Severity type, object context, string format)
		{
			_count++;
			if (type >= Severity.Error)
				_errorCount++;
		}
		public void Write(Severity type, object context, string format, object arg0, object arg1 = null)
		{
			Write(type, context, format);
		}
		public void Write(Severity type, object context, string format, params object[] args)
		{
			Write(type, context, format);
		}
		/// <summary>Always returns false.</summary>
		public bool IsEnabled(Severity type)
		{
			return false;
		}
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
			string loc = MessageSink.LocationString(context);
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

	/// <summary>Holds an argument list compatible with <see cref="IMessageSink.Write"/>.
	/// Typically used with <see cref="MessageHolder"/>.</summary>
	public struct LogMessage : IHasLocation
	{
		public LogMessage(Severity type, object context, string format, object arg0, object arg1 = null)
			: this (type, context, format, new object[2] { arg0, arg1 }) {}
		public LogMessage(Severity type, object context, string format)
			: this (type, context, format, EmptyArray<object>.Value) {}
		public LogMessage(Severity type, object context, string format, params object[] args)
		{
			Severity = type;
			Context = context;
			Format = format;
			_args = args;
		}
		public readonly Severity Severity;
		public readonly object Context;
		public readonly string Format;
		readonly object[] _args;
		public object[] Args { get { return _args; } }
		public string Formatted
		{
			get {
				try {
					return Localize.Localized(Format, _args);
				} catch {
					return Format;
				}
			}
		}
		public override string ToString()
		{
			return MessageSink.FormatMessage(Severity, Context, Format, _args);
		}
		public object Location
		{
			get { return MessageSink.LocationOf(Context); }
		}
		public void WriteTo(IMessageSink sink)
		{
			if (_args.Length == 0)
				sink.Write(Severity, Context, Format);
			else
				sink.Write(Severity, Context, Format, _args);
		}
	}

	/// <summary>A message sink that puts the messages it receives in a list.</summary>
	public class MessageHolder : IMessageSink, ICloneable<MessageHolder>
	{
		List<LogMessage> _messages;

		public IList<LogMessage> List
		{
			get { return _messages = _messages ?? new List<LogMessage>(); }
		}

		public void WriteListTo(IMessageSink sink)
		{
			foreach (LogMessage msg in List)
				msg.WriteTo(sink);
		}

		public void Write(Severity type, object context, string format)
		{
			List.Add(new LogMessage(type, context, format));
		}
		public void Write(Severity type, object context, string format, object arg0, object arg1 = null)
		{
			List.Add(new LogMessage(type, context, format, arg0, arg1));
		}
		public void Write(Severity type, object context, string format, params object[] args)
		{
			List.Add(new LogMessage(type, context, format, args));
		}
		/// <summary>Always returns true.</summary>
		public bool IsEnabled(Severity type)
		{
			return true;
		}

		public MessageHolder Clone()
		{
			var copy = new MessageHolder();
			if (_messages != null)
				copy._messages = new List<LogMessage>(_messages);
			return copy;
		}
	}

	/// <summary>A decorator that uses a delegate to accept or ignore messages.</summary>
	/// <remarks>The filter can accept or reject messages based on both the message 
	/// type and the actual message (format string). When someone calls 
	/// <see cref="IsEnabled(Severity)"/>, the filter is invoked with only the type;
	/// the message is set to null. Accepted messages are sent to the 
	/// <see cref="Target"/> message sink.</remarks>
	public class MessageFilter : IMessageSink
	{
		public Func<Severity, object, string, bool> Filter { get; set; }
		public Func<Severity, bool> TypeFilter { get; set; }
		public IMessageSink Target { get; set; }

		public MessageFilter(IMessageSink target, Func<Severity, object, string, bool> filter) 
		{
			Filter = filter;
			Target = target;
		}
		public MessageFilter(IMessageSink target, Func<Severity, bool> filter) 
		{
			TypeFilter = filter;
			Target = target;
		}
		bool Passes(Severity type, object context, string format)
		{
			return Filter != null && Filter(type, context, format)
				|| TypeFilter != null && TypeFilter(type);
		}
		public void Write(Severity type, object context, string format)
		{
			if (Passes(type, context, format))
				Target.Write(type, context, format);
		}
		public void Write(Severity type, object context, string format, object arg0, object arg1 = null)
		{
			if (Passes(type, context, format))
				Target.Write(type, context, format, arg0, arg1);
		}
		public void Write(Severity type, object context, string format, params object[] args)
		{
			if (Passes(type, context, format))
				Target.Write(type, context, format, args);
		}
		/// <summary>Returns true if <c>Filter(type, null)</c> and <c>target.IsEnabled(type)</c> are both true.</summary>
		public bool IsEnabled(Severity type)
		{
			return Passes(type, null, null) && Target.IsEnabled(type);
		}
	}

	/// <summary>A decorator (wrapper) for <see cref="IMessageSink"/> that filters
	/// out some messages if their <see cref="Severity"/> is too low, according
	/// to the value of the <see cref="MinSeverity"/> property.</summary>
	public class SeverityMessageFilter<TContext> : IMessageSink<TContext>
	{
		public SeverityMessageFilter(IMessageSink<TContext> target, Severity minSeverity) 
			{ Target = target; _minSeverity = minSeverity; }
		Severity _minSeverity;
		bool _printedPrev; // whether the last-written message passed

		public IMessageSink<TContext> Target { get; set; }
		public Severity MinSeverity { 
			get { return _minSeverity; }
			set { _minSeverity = value; }
		}

		public void Write(Severity type, TContext context, string format)
		{
 			if (_printedPrev = Passes(type)) Target.Write(type, context, format);
		}
		public void Write(Severity type, TContext context, string format, object arg0, object arg1 = null)
		{
 			if (_printedPrev = Passes(type)) Target.Write(type, context, format, arg0, arg1);
		}
		public void Write(Severity type, TContext context, string format, params object[] args)
		{
 			if (_printedPrev = Passes(type)) Target.Write(type, context, format, args);
		}
		public bool IsEnabled(Severity type)
		{
			return Passes(type) && Target.IsEnabled(type);
		}
		bool Passes(Severity type)
		{
			return type >= _minSeverity || (type == Severity.Detail && _printedPrev);
		}
	}

	/// <summary>Alias for SeverityMessageFilter&lt;object>.</summary>
	public class SeverityMessageFilter : SeverityMessageFilter<object>, IMessageSink
	{
		public SeverityMessageFilter(IMessageSink<object> target, Severity minSeverity) : base(target, minSeverity) { }
	}
		
	/// <summary>A message sink that sends its messages to a list of other sinks.</summary>
	/// <remarks>Null elements are allowed in the <see cref="List"/> and are ignored.</remarks>
	public class MessageSplitter<TContext> : IMessageSink<TContext>
	{
		List<IMessageSink<TContext>> _list = new List<IMessageSink<TContext>>();
		public IList<IMessageSink<TContext>> List { get { return _list; } }

		public MessageSplitter(IEnumerable<IMessageSink<TContext>> targets) { _list = new List<IMessageSink<TContext>>(targets); }
		public MessageSplitter(params IMessageSink<TContext>[] targets) { _list = new List<IMessageSink<TContext>>(targets); }
		public MessageSplitter() { _list = new List<IMessageSink<TContext>>(); }
	
		public void  Write(Severity type, TContext context, string format)
		{
 			foreach(var sink in _list)
				if (sink != null)
					sink.Write(type, context, format);
		}
		public void  Write(Severity type, TContext context, string format, object arg0, object arg1 = null)
		{
 			foreach(var sink in _list)
				if (sink != null)
					sink.Write(type, context, format, arg0, arg1);
		}
		public void  Write(Severity type, TContext context, string format, params object[] args)
		{
			foreach (var sink in _list)
				if (sink != null)
					sink.Write(type, context, format, args);
		}
		/// <summary>Returns true if <tt>s.IsEnabled(type)</tt> is true for at least one target message sink 's'.</summary>
		public bool IsEnabled(Severity type)
		{
			foreach (var sink in _list)
				if (sink != null && sink.IsEnabled(type))
					return true;
			return false;
		}
	}

	/// <summary>Alias for MessageSplitter&lt;object>.</summary>
	public class MessageSplitter : MessageSplitter<object>, IMessageSink
	{
		public MessageSplitter(IEnumerable<IMessageSink<object>> targets) : base(targets) { }
		public MessageSplitter(params IMessageSink<object>[] targets) : base(targets) { }
		public MessageSplitter() : base() { }
	}

	/// <summary>A message sink wrapper that has a default value for the context 
	/// parameter, which is used when the context provided is null, and an optional 
	/// message prefix which is inserted at the beginning of the format string.</summary>
	public class MessageSinkWithContext<TContext> : IMessageSink<TContext> where TContext: class
	{
		IMessageSink<TContext> _target { get; set; }
		public IMessageSink<TContext> Target
		{
			get { return _target ?? MessageSink.Default; }
			set { _target = value; }
		}
		public TContext DefaultContext { get; set; }
		public string MessagePrefix { get; set; }

		/// <summary>Initializes the wrapper.</summary>
		/// <param name="target">Message sink to which all messages are forwarded. If this parameter 
		/// is null, then messages are written to <see cref="MessageSink.Default"/>
		/// <i>at the time a message is written</i>.</param>
		/// <param name="defaultContext">Default context object, used if Write is called with a context of null.</param>
		/// <param name="messagePrefix">A prefix to prepend at the beginning of all messages written.</param>
		/// <param name="scrubPrefix">Whether to replace "{" with ""{{" and "}" with "}}" in 
		/// <c>messagePrefix</c> to avoid accidental misbehavior when the string is formatted.</param>
		public MessageSinkWithContext(IMessageSink<TContext> target, TContext defaultContext, string messagePrefix = null, bool scrubPrefix = true)
		{
			Target = target;
			DefaultContext = defaultContext;
			if (scrubPrefix && messagePrefix != null)
				messagePrefix = messagePrefix.Replace("{", "{{").Replace("}", "}}");
			MessagePrefix = messagePrefix;
		}

		string Prefixed(Severity level, string message)
		{
			if (!string.IsNullOrEmpty(MessagePrefix))
				if (IsEnabled(level))
				return MessagePrefix + message;
			return message;
		}

		public bool IsEnabled(Severity type)
		{
			return Target.IsEnabled(type);
		}

		public void Write(Severity level, TContext context, string format)
		{
			if (MessagePrefix != null)
			{
				if (!IsEnabled(level))
					return;
				format = MessagePrefix + format;
			}
			Target.Write(level, context ?? DefaultContext, format);
		}
		public void Write(Severity level, TContext context, string format, params object[] args)
		{
			if (MessagePrefix != null)
			{
				if (!IsEnabled(level))
					return;
				format = MessagePrefix + format;
			}
			Target.Write(level, context ?? DefaultContext, format, args);
		}
		public void Write(Severity level, TContext context, string format, object arg0, object arg1 = null)
		{
			if (MessagePrefix != null)
			{
				if (!IsEnabled(level))
					return;
				format = MessagePrefix + format;
			}
			Target.Write(level, context ?? DefaultContext, format, arg0, arg1);
		}
	}

	/// <summary>Alias for MessageSinkWithContext&lt;object>.</summary>
	public class MessageSinkWithContext : MessageSinkWithContext<object>, IMessageSink
	{
		public MessageSinkWithContext(IMessageSink target, object defaultContext, string messagePrefix = null, bool scrubPrefix = true)
			: base(target, defaultContext, messagePrefix, scrubPrefix) { }
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

		public void Write(Severity type, object context, string format)
		{
			if (IsEnabled(type))
				_writer(type, context, format, EmptyArray<object>.Value);
		}
		public void Write(Severity type, object context, string format, object arg0, object arg1 = null)
		{
			if (IsEnabled(type))
				_writer(type, context, format, new[] { arg0, arg1 });
		}
		public void Write(Severity type, object context, string format, params object[] args)
		{
			if (IsEnabled(type))
				_writer(type, context, format, args);
		}

		public bool IsEnabled(Severity type)
		{
			return _isEnabled != null ? _isEnabled(type) : true;
		}
	}
}