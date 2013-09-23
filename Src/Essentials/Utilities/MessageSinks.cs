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

			if (msgType == MessageSink.Error)
			{
				color = ConsoleColor.Red;
				implicitText = true;
			}
			else if (msgType == MessageSink.Warning)
			{
				color = ConsoleColor.Yellow;
				implicitText = true;
			}
			else if (msgType == MessageSink.Note)
				color = ConsoleColor.White;
			else if (msgType == MessageSink.Verbose)
				color = ConsoleColor.Gray;
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
	public class MessageHolder : IMessageSink
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
}