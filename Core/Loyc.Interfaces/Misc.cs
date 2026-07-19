using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc
{
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
	public delegate void WriteMessageFn(Severity type, object? context, string format, params object?[] args);
	public delegate void WriteMessageFn<Context>(Severity type, Context context, string format, params object?[] args);
}
