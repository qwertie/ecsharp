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

		public void Write(Symbol type, string format)
		{
			WriteCore(type, Localize.From(format));
		}
		public void Write(Symbol type, string format, object arg0, object arg1 = null)
		{
			WriteCore(type, Localize.From(format, arg0, arg1));
		}
		public void Write(Symbol type, string format, params object[] args)
		{
			WriteCore(type, Localize.From(format, args));
		}
		void WriteCore(Symbol type, string text)
		{
			string typeText;
			ConsoleColor oldColor = Console.ForegroundColor;
			Console.ForegroundColor = PickColor(type, out typeText);
			if (typeText == null)
				Console.WriteLine(text);
			else
				Console.WriteLine(typeText + ": " + text);
			Console.ForegroundColor = oldColor;
		}
		/// <summary>Always returns true.</summary>
		public bool IsEnabled(Symbol type)
		{
			return true;
		}
	}

	/// <summary>Discards all messages.</summary>
	public sealed class NullMessageSink : IMessageSink
	{
		public void Write(Symbol type, string format)
		{
		}
		public void Write(Symbol type, string format, object arg0, object arg1 = null)
		{
		}
		public void Write(Symbol type, string format, params object[] args)
		{
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
		public void Write(Symbol type, string format)
		{
			Trace.WriteLine(Localize.From(format), type.Name);
		}
		public void Write(Symbol type, string format, object arg0, object arg1 = null)
		{
			Trace.WriteLine(Localize.From(format, arg0, arg1), type.Name);
		}
		public void Write(Symbol type, string format, params object[] args)
		{
			Trace.WriteLine(Localize.From(format, args), type.Name);
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
		public struct Message
		{
			public Message(Symbol type, string format, object arg0, object arg1 = null)
				: this (type, format, new object[2] { arg0, arg1 }) {}
			public Message(Symbol type, string format)
				: this (type, format, InternalList<object>.EmptyArray) {}
			public Message(Symbol type, string format, params object[] args)
			{
				Type = type ?? GSymbol.Empty;
				Format = format;
				_args = args;
			}
			public readonly Symbol Type;
			public readonly string Format;
			readonly object[] _args;
			public object[] Args { get { return _args; } }
			public string Formatted
			{
				get { return Localize.From(Format, _args); }
			}
			public override string ToString()
			{
				return Type.Name == "" ? Formatted : Type.Name + ": " + Formatted;
			}
		}
		List<Message> _messages;

		public IList<Message> List
		{
			get { return _messages = _messages ?? new List<Message>(); }
		}
		public void Write(Symbol type, string format)
		{
			List.Add(new Message(type, format));
		}
		public void Write(Symbol type, string format, object arg0, object arg1 = null)
		{
			List.Add(new Message(type, format, arg0, arg1));
		}
		public void Write(Symbol type, string format, params object[] args)
		{
			List.Add(new Message(type, format, args));
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
		public Func<Symbol, string, bool> Filter { get; set; }
		public IMessageSink Target { get; set; }
		
		public MessageFilter(Func<Symbol, string, bool> filter, IMessageSink target) 
		{
			Filter = filter;
			Target = target;
		}
		public void Write(Symbol type, string format)
		{
			if (Filter(type, format))
				Target.Write(type, format);
		}
		public void Write(Symbol type, string format, object arg0, object arg1 = null)
		{
			if (Filter(type, format))
				Target.Write(type, format, arg0, arg1);
		}
		public void Write(Symbol type, string format, params object[] args)
		{
			if (Filter(type, format))
				Target.Write(type, format, args);
		}
		/// <summary>Returns true if <c>Filter(type, null)</c> and <c>target.IsEnabled(type)</c> are both true.</summary>
		public bool IsEnabled(Symbol type)
		{
			return Filter(type, null) && Target.IsEnabled(type);
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
	
		public void  Write(Symbol type, string format)
		{
 			foreach(var sink in _list)
				sink.Write(type, format);
		}
		public void  Write(Symbol type, string format, object arg0, object arg1 = null)
		{
 			foreach(var sink in _list)
				sink.Write(type, format, arg0, arg1);
		}
		public void  Write(Symbol type, string format, params object[] args)
		{
			foreach (var sink in _list)
				sink.Write(type, format, args);
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