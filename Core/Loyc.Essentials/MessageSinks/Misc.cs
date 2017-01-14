//
// Miscellaneous small items related to message sinks
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc
{
	/// <summary>This interface allows an object to declare its "location".</summary>
	/// <remarks>For example, <see cref="Loyc.Syntax.LNode"/> implements this 
	/// interface so that when a compiler error refers to a source code construct,
	/// the error message contains the location of that source code rather than the
	/// code itself.
	/// <para/>
	/// Given a context object that may or may not implement this interface, it's
	/// handy to use <see cref="MessageSink.ContextToString"/> to convert the 
	/// "context" of a message into a string, or <see cref="MessageSink.LocationOf(object)"/> 
	/// to unwrap objects that implement IHasLocation.
	/// </remarks>
	public interface IHasLocation
	{
		object Location { get; }
	}

	/// <summary>This is the method signature of <c>IMessageSink.Write()</c>. You 
	/// can convert from one of these delegates to <see cref="IMessageSink"/> by 
	/// calling <see cref="MessageSink.FromDelegate"/>.</summary>
	/// <param name="type">Severity or importance of the message; widely-used
	/// types include Error, Warning, Note, Debug, and Verbose. The special 
	/// type Detail is intended to provide more information about a previous 
	/// message.</param>
	/// <param name="context">An object that represents the location that the
	/// message applies to, a string that indicates what the program was doing 
	/// when the message was generated, or any other relevant context information.
	/// See also <see cref="MessageSink.ContextToString"/>().</param>
	/// <param name="format">A message to display. If there are additional 
	/// arguments, placeholders such as {0} and {1} refer to these arguments.</param>
	/// <param name="args">Optional arguments to fill in placeholders in the format 
	/// string.</param>
	public delegate void WriteMessageFn(Severity type, object context, string format, params object[] args);

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
