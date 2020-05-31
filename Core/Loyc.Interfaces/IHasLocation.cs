using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc
{
	/// <summary>This interface allows an object to declare its "location".</summary>
	/// <remarks>
	/// Objects designed to be used as a context parameter in <see cref="IMessageSink{T}"/>
	/// can implement this interface so that the string form of the message shows
	/// the location of a piece of data instead of the data itself. For example, 
	/// <see cref="Loyc.Syntax.LNode"/> implements this interface so that when a 
	/// compiler error refers to a source code construct, the context of the
	/// <see cref="LogMessage"/> can refer to the code itself while the printed form 
	/// of the the error message shows the location of the code instead.
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
