//
// Defines MessageHolder and the LogMessage structure that it depends on
//
using System;
using System.Collections.Generic;
using System.Linq;
using Loyc.Collections;

namespace Loyc
{
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

		public void Write(Severity level, object context, string format)
		{
			List.Add(new LogMessage(level, context, format));
		}
		public void Write(Severity level, object context, string format, object arg0, object arg1 = null)
		{
			List.Add(new LogMessage(level, context, format, arg0, arg1));
		}
		public void Write(Severity level, object context, string format, params object[] args)
		{
			List.Add(new LogMessage(level, context, format, args));
		}
		/// <summary>Always returns true.</summary>
		public bool IsEnabled(Severity level)
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
}
