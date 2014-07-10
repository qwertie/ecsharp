//
// Standard message sinks:
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
	/// <summary>Sends all messages to <see cref="System.Console.WriteLine"/>, 
	/// with hard-coded colors for Error, Warning, Note, Verbose, and Detail.</summary>
	public class ConsoleMessageSink : IMessageSink
	{
		protected static ConsoleColor _lastColor;

		protected virtual ConsoleColor PickColor(Severity msgType, out string msgTypeText)
		{
			bool implicitText = false;
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

			msgTypeText = implicitText ? null : Localize.From(msgType.ToString());
			_lastColor = color;
			return color;
		}

		public void Write(Severity type, object context, string format)
		{
			WriteCore(type, context, Localize.From(format));
		}
		public void Write(Severity type, object context, string format, object arg0, object arg1 = null)
		{
			WriteCore(type, context, Localize.From(format, arg0, arg1));
		}
		public void Write(Severity type, object context, string format, params object[] args)
		{
			WriteCore(type, context, Localize.From(format, args));
		}
		void WriteCore(Severity type, object context, string text)
		{
			string loc = MessageSink.LocationString(context);
			if (!string.IsNullOrEmpty(loc))
				Console.Write(loc + ": ");

			string typeText;
			ConsoleColor oldColor = Console.ForegroundColor;
			Console.ForegroundColor = PickColor(type, out typeText);
			if (typeText != null)
				text = typeText + ": " + text;
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
	/// increases by one with each message received.</summary>
	public sealed class NullMessageSink : IMessageSink, ICount
	{
		int _count;
		public int Count { get { return _count; } set { _count = value; } }
		public bool IsEmpty { get { return _count == 0; } }

		public void Write(Severity type, object context, string format)
		{
			_count++;
		}
		public void Write(Severity type, object context, string format, object arg0, object arg1 = null)
		{
			_count++;
		}
		public void Write(Severity type, object context, string format, params object[] args)
		{
			_count++;
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
		public void Write(Severity type, object context, string format)
		{
			WriteCore(type, context, Localize.From(format));
		}
		public void Write(Severity type, object context, string format, object arg0, object arg1 = null)
		{
			WriteCore(type, context, Localize.From(format, arg0, arg1));
		}
		public void Write(Severity type, object context, string format, params object[] args)
		{
			WriteCore(type, context, Localize.From(format, args));
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

	/// <summary>A message sink that stores all messages it receives.</summary>
	public class MessageHolder : IMessageSink, ICloneable<MessageHolder>
	{
		/// <summary>Holds an argument list of <see cref="IMessageSink.Write"/>.</summary>
		public struct Message : ILocationString
		{
			public Message(Severity type, object context, string format, object arg0, object arg1 = null)
				: this (type, context, format, new object[2] { arg0, arg1 }) {}
			public Message(Severity type, object context, string format)
				: this (type, context, format, InternalList<object>.EmptyArray) {}
			public Message(Severity type, object context, string format, params object[] args)
			{
				Type = type;
				Context = context;
				Format = format;
				_args = args;
			}
			public readonly Severity Type;
			public readonly object Context;
			public readonly string Format;
			readonly object[] _args;
			public object[] Args { get { return _args; } }
			public string Formatted
			{
				get { return Localize.From(Format, _args); }
			}
			public override string ToString()
			{
				string loc = LocationString;
				string text = Type.ToString() + ": " + Formatted;
				return string.IsNullOrEmpty(loc) ? text : loc + ": " + text;
			}
			public string LocationString
			{
				get { return MessageSink.LocationString(Context); }
			}
			public void WriteTo(IMessageSink sink)
			{
				if (_args.Length == 0)
					sink.Write(Type, Context, Format);
				else
					sink.Write(Type, Context, Format, _args);
			}
		}
		List<Message> _messages;

		public IList<Message> List
		{
			get { return _messages = _messages ?? new List<Message>(); }
		}
		public void WriteListTo(IMessageSink sink)
		{
			foreach (Message msg in List)
				msg.WriteTo(sink);
		}

		public void Write(Severity type, object context, string format)
		{
			List.Add(new Message(type, context, format));
		}
		public void Write(Severity type, object context, string format, object arg0, object arg1 = null)
		{
			List.Add(new Message(type, context, format, arg0, arg1));
		}
		public void Write(Severity type, object context, string format, params object[] args)
		{
			List.Add(new Message(type, context, format, args));
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
				copy._messages = new List<Message>(_messages);
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

		public MessageFilter(Func<Severity, object, string, bool> filter, IMessageSink target) 
		{
			Filter = filter;
			Target = target;
		}
		public MessageFilter(Func<Severity, bool> filter, IMessageSink target) 
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
	public class SeverityMessageFilter : IMessageSink
	{
		public SeverityMessageFilter(IMessageSink target, Severity minSeverity) 
			{ Target = target; _minSeverity = minSeverity; }
		Severity _minSeverity;
		bool _printedPrev; // whether the last-written message passed

		public IMessageSink Target { get; set; }
		public Severity MinSeverity { 
			get { return _minSeverity; }
			set { _minSeverity = value; }
		}

		public void Write(Severity type, object context, string format)
		{
 			if (_printedPrev = Passes(type)) Target.Write(type, context, format);
		}
		public void Write(Severity type, object context, string format, object arg0, object arg1 = null)
		{
 			if (_printedPrev = Passes(type)) Target.Write(type, context, format, arg0, arg1);
		}
		public void Write(Severity type, object context, string format, params object[] args)
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
	
	/// <summary>A message sink that sends its messages to a list of other sinks.</summary>
	public class MessageSplitter : IMessageSink
	{
		List<IMessageSink> _list = new List<IMessageSink>();
		public IList<IMessageSink> List { get { return _list; } }

		public MessageSplitter(IEnumerable<IMessageSink> targets) { _list = new List<IMessageSink>(targets); }
		public MessageSplitter(params IMessageSink[] targets) { _list = new List<IMessageSink>(targets); }
		public MessageSplitter() { _list = new List<IMessageSink>(); }
	
		public void  Write(Severity type, object context, string format)
		{
 			foreach(var sink in _list)
				sink.Write(type, context, format);
		}
		public void  Write(Severity type, object context, string format, object arg0, object arg1 = null)
		{
 			foreach(var sink in _list)
				sink.Write(type, context, format, arg0, arg1);
		}
		public void  Write(Severity type, object context, string format, params object[] args)
		{
			foreach (var sink in _list)
				sink.Write(type, context, format, args);
		}
		/// <summary>Returns true if <tt>s.IsEnabled(type)</tt> is true for at least one target message sink 's'.</summary>
		public bool IsEnabled(Severity type)
		{
			foreach (var sink in _list)
				if (sink.IsEnabled(type))
					return true;
			return false;
		}
	}

	/// <summary>This helper class lets you implement <see cref="IMessageSink"/> 
	/// with one or two delegates (a writer method, and an optional severity filter).</summary>
	public class MessageSinkFromDelegate : IMessageSink
	{
		Action<Severity, object, string, object[]> _writer;
		Func<Severity, bool> _isEnabled;

		/// <summary>Initializes this object.</summary>
		/// <param name="writer">Required. A method that accepts output.</param>
		/// <param name="isEnabled">Optional. A method that decides whether to 
		/// output based on the message type. If this parameter is provided,
		/// then <see cref="Write"/>() will not invoke the writer when isEnabled
		/// returns false. This delegate is also called by <see cref="IsEnabled"/>().</param>
		public MessageSinkFromDelegate(Action<Severity, object, string, object[]> writer, Func<Severity, bool> isEnabled = null)
		{
			CheckParam.IsNotNull("writer", writer);
			_writer = writer;
			_isEnabled = isEnabled;
		}

		public void Write(Severity type, object context, string format)
		{
			if (IsEnabled(type))
				_writer(type, context, format, InternalList<object>.EmptyArray);
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