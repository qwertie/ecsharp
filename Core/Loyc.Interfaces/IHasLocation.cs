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
}
