using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Syntax
{
	/// <summary>Standard interface for parsing tokens, statements, or anthing else. 
	/// Has one main method, ParseNext().
	/// </summary>
	/// <remarks>
	/// For the user's convenience, the IEnumerable interface is included so that 
	/// the parser can be used with foreach in C#. However, GetEnumerator() should 
	/// only be called once, and the enumerator must not be reset.
	/// </remarks>
	public interface IParseNext<Node> : IEnumerable<Node>
	{
		Node ParseNext();
	}
}
