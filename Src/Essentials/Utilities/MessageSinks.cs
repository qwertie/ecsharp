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

namespace Loyc.Utilities
{
	/// <summary>Sends all messages to the <see cref="System.Console.WriteLine"/>, 
	/// with hard-coded colors for Error, Warning, Note, Verbose, and Detail.</summary>
	public class ConsoleMessageSink : IMessageSink
	{
		protected static ConsoleColor _lastColor;

		protected virtual ConsoleColor PickColor(Symbol msgType, out string msgTypeText)
		{
			bool implicitText = false;
			ConsoleColor color;

			if (msgType == MessageSink.Critical || msgType == MessageSink._Alert || msgType == MessageSink.Fatal || msgType == MessageSink._Emergency)
				color = ConsoleColor.Magenta;
			if (msgType == MessageSink.Error || msgType == MessageSink._Severe)
				color = ConsoleColor.Red;
			else if (msgType == MessageSink.Warning)
				color = ConsoleColor.Yellow;
			else if (msgType == MessageSink.Note)
				color = ConsoleColor.White;
			else if (msgType == MessageSink.Debug)
				color = ConsoleColor.Cyan;
			else if (msgType == MessageSink.Verbose || msgType == MessageSink._Finer)
				color = ConsoleColor.DarkCyan;
			else if (msgType == MessageSink.Detail) {
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

			msgTypeText = implicitText ? null : Localize.From(msgType.Name);
			_lastColor = color;
			return color;
		}

		public void Write(Symbol type, object context, string format)
		{
			WriteCore(type, context, Localize.From(format));
		}
		public void Write(Symbol type, object context, string format, object arg0, object arg1 = null)
		{
			WriteCore(type, context, Localize.From(format, arg0, arg1));
		}
		public void Write(Symbol type, object context, string format, params object[] args)
		{
			WriteCore(type, context, Localize.From(format, args));
		}
		void WriteCore(Symbol type, object context, string text)
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
		public bool IsEnabled(Symbol type)
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

		public void Write(Symbol type, object context, string format)
		{
			_count++;
		}
		public void Write(Symbol type, object context, string format, object arg0, object arg1 = null)
		{
			_count++;
		}
		public void Write(Symbol type, object context, string format, params object[] args)
		{
			_count++;
		}
		/// <summary>Always returns false.</summary>
		public bool IsEnabled(Symbol type)
		{
			return false;
		}
	}

	/// <summary>Sends all messages to <see cref="System.Diagnostics.Trace.WriteLine"/>.</summary>
	public class TraceMessageSink : IMessageSink
	{
		public void Write(Symbol type, object context, string format)
		{
			WriteCore(type, context, Localize.From(format));
		}
		public void Write(Symbol type, object context, string format, object arg0, object arg1 = null)
		{
			WriteCore(type, context, Localize.From(format, arg0, arg1));
		}
		public void Write(Symbol type, object context, string format, params object[] args)
		{
			WriteCore(type, context, Localize.From(format, args));
		}
		public void WriteCore(Symbol type, object context, string text)
		{
			string loc = MessageSink.LocationString(context);
			if (!string.IsNullOrEmpty(loc))
				text = loc + ": " + text;
			Trace.WriteLine(text, type.Name);
		}
		/// <summary>Always returns true.</summary>
		public bool IsEnabled(Symbol type)
		{
			return true;
		}
	}

	/// <summary>A message sink that stores all messages it receives.</summary>
	public class MessageHolder : IMessageSink, ICloneable<MessageHolder>
	{
		public struct Message : ILocationString
		{
			public Message(Symbol type, object context, string format, object arg0, object arg1 = null)
				: this (type, context, format, new object[2] { arg0, arg1 }) {}
			public Message(Symbol type, object context, string format)
				: this (type, context, format, InternalList<object>.EmptyArray) {}
			public Message(Symbol type, object context, string format, params object[] args)
			{
				Type = type ?? GSymbol.Empty;
				Context = context;
				Format = format;
				_args = args;
			}
			public readonly Symbol Type;
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
				string text = Type.Name == "" ? Formatted : Type.Name + ": " + Formatted;
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

		public void Write(Symbol type, object context, string format)
		{
			List.Add(new Message(type, context, format));
		}
		public void Write(Symbol type, object context, string format, object arg0, object arg1 = null)
		{
			List.Add(new Message(type, context, format, arg0, arg1));
		}
		public void Write(Symbol type, object context, string format, params object[] args)
		{
			List.Add(new Message(type, context, format, args));
		}
		/// <summary>Always returns true.</summary>
		public bool IsEnabled(Symbol type)
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
	/// <see cref="IsEnabled(Symbol)"/>, the filter is invoked with only the type;
	/// the message is set to null. Accepted messages are sent to the 
	/// <see cref="Target"/> message sink.</remarks>
	public class MessageFilter : IMessageSink
	{
		public Func<Symbol, object, string, bool> Filter { get; set; }
		public Func<Symbol, bool> TypeFilter { get; set; }
		public IMessageSink Target { get; set; }
		
		public MessageFilter(Func<Symbol, object, string, bool> filter, IMessageSink target) 
		{
			Filter = filter;
			Target = target;
		}
		public MessageFilter(Func<Symbol, bool> filter, IMessageSink target) 
		{
			TypeFilter = filter;
			Target = target;
		}
		bool Passes(Symbol type, object context, string format)
		{
			return Filter != null && Filter(type, context, format)
				|| TypeFilter != null && TypeFilter(type);
		}
		public void Write(Symbol type, object context, string format)
		{
			if (Passes(type, context, format))
				Target.Write(type, context, format);
		}
		public void Write(Symbol type, object context, string format, object arg0, object arg1 = null)
		{
			if (Passes(type, context, format))
				Target.Write(type, context, format, arg0, arg1);
		}
		public void Write(Symbol type, object context, string format, params object[] args)
		{
			if (Passes(type, context, format))
				Target.Write(type, context, format, args);
		}
		/// <summary>Returns true if <c>Filter(type, null)</c> and <c>target.IsEnabled(type)</c> are both true.</summary>
		public bool IsEnabled(Symbol type)
		{
			return Passes(type, null, null) && Target.IsEnabled(type);
		}
	}

	public class SeverityMessageFilter : IMessageSink
	{
		public SeverityMessageFilter(IMessageSink target, Symbol minSeverity) 
			{ Target = target; MinSeveritySymbol = minSeverity; }
		public SeverityMessageFilter(IMessageSink target, int minSeverity) 
			{ Target = target; MinSeverity = minSeverity; }
		int _minSeverity;
		bool _printedPrev; // whether the last-written message passed

		public IMessageSink Target { get; set; }
		public int MinSeverity { 
			get { return _minSeverity; }
			set { _minSeverity = value; }
		}
		public Symbol MinSeveritySymbol
		{
			get { return MessageSink.GetSymbol(_minSeverity); }
			set { _minSeverity = MessageSink.GetSeverity(value); }
		}

		public void Write(Symbol type, object context, string format)
		{
 			if (_printedPrev = Passes(type)) Target.Write(type, context, format);
		}
		public void Write(Symbol type, object context, string format, object arg0, object arg1 = null)
		{
 			if (_printedPrev = Passes(type)) Target.Write(type, context, format, arg0, arg1);
		}
		public void Write(Symbol type, object context, string format, params object[] args)
		{
 			if (_printedPrev = Passes(type)) Target.Write(type, context, format, args);
		}
		public bool IsEnabled(Symbol type)
		{
			return Passes(type) && Target.IsEnabled(type);
		}
		bool Passes(Symbol type)
		{
			return MessageSink.GetSeverity(type) >= _minSeverity || (type == MessageSink.Detail && _printedPrev);
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
	
		public void  Write(Symbol type, object context, string format)
		{
 			foreach(var sink in _list)
				sink.Write(type, context, format);
		}
		public void  Write(Symbol type, object context, string format, object arg0, object arg1 = null)
		{
 			foreach(var sink in _list)
				sink.Write(type, context, format, arg0, arg1);
		}
		public void  Write(Symbol type, object context, string format, params object[] args)
		{
			foreach (var sink in _list)
				sink.Write(type, context, format, args);
		}
		/// <summary>Returns true if <tt>s.IsEnabled(type)</tt> is true for at least one target message sink 's'.</summary>
		public bool IsEnabled(Symbol type)
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
		Action<Symbol, object, string, object[]> _writer;
		Func<Symbol, bool> _isEnabled;

		/// <summary>Initializes this object.</summary>
		/// <param name="writer">Required. A method that accepts output.</param>
		/// <param name="isEnabled">Optional. A method that decides whether to 
		/// output based on the message type. If this parameter is provided,
		/// the <see cref="Write"/>() will not invoke the writer when isEnabled
		/// returns false. This delegate is also called by <see cref="IsEnabled"/>().</param>
		public MessageSinkFromDelegate(Action<Symbol, object, string, object[]> writer, Func<Symbol, bool> isEnabled = null)
		{
			CheckParam.IsNotNull("writer", writer);
			_writer = writer;
			_isEnabled = isEnabled;
		}

		public void Write(Symbol type, object context, string format)
		{
			if (IsEnabled(type))
				_writer(type, context, format, InternalList<object>.EmptyArray);
		}
		public void Write(Symbol type, object context, string format, object arg0, object arg1 = null)
		{
			if (IsEnabled(type))
				_writer(type, context, format, new[] { arg0, arg1 });
		}
		public void Write(Symbol type, object context, string format, params object[] args)
		{
			if (IsEnabled(type))
				_writer(type, context, format, args);
		}

		public bool IsEnabled(Symbol type)
		{
			return _isEnabled != null ? _isEnabled(type) : true;
		}
	}
}