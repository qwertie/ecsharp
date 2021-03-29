using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc
{
	///	<summary>A formatted message with an associated Context.</summary>
	///	<seealso cref="Loyc.LogMessage"/>
	public interface ILogMessage
	{
		/// <summary>Indicates how problematic the message is (Debug, Note, Warning, Error, etc.)</summary>
		Severity Severity { get; }
		/// <summary>An object associated with the event being logged (possibly huge).</summary>
		object? Context { get; }
		/// <summary>Format string (without substitutions or localization)</summary>
		string Format { get; }
		/// <summary>Values to be substituted into the format string.</summary>
		object?[] Args { get; }
		/// <summary>Formatted string (with substitution and localization applied)</summary>
		string Formatted { get; }
		/// <summary>Typically this returns <c>MessageSink.LocationOf(Context)</c>.</summary>
		object? Location { get; }
	}
}
