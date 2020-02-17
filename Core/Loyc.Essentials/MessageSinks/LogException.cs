//
// Miscellaneous small items related to message sinks
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc
{
	/// <summary>An exception that includes a "context" object as part of a
	/// <see cref="LogMessage"/> structure, typically used to indicate where an 
	/// error occurred.</summary>
	public class LogException : Exception
	{
		public LogException(object context, string format, params object[] args) : this(Severity.Error, context, format, args) {}
		public LogException(Severity severity, object context, string format, params object[] args) : this(new LogMessage(severity, context, format, args)) {}
		public LogException(LogMessage msg) { 
			Msg = msg;
			try {
				Data["Severity"] = msg.Severity;
				// Disabled because members of the Data dictionary must be serializable,
				// but msg.Context might not be. We could convert to string, but is it
				// worth the performance cost? Loyc code isn't really using Data anyway.
				//Data["Context"] = msg.Context;
			} catch { }
		}
		
		/// <summary>Contains additional information about the error that occurred.</summary>
		public LogMessage Msg { get; private set; }

		public override string Message
		{
			get { return Msg.Formatted; }
		}
	}
}
