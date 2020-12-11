using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Syntax
{
	/// <summary>This interface for parsing text into objects is implemented by <see cref="LiteralHandlerTable"/>.</summary>
	public interface ILiteralParser
	{
		/// <summary>Returns true if this object has a parser for the specified type marker.</summary>
		bool CanParse(Symbol typeMarker);

		/// <summary>Attempts to parse a string with a given type marker.</summary>
		/// <param name="textValue">A text value that has already been preprocessed to remove escape sequences</param>
		/// <param name="typeMarker">Indicates the type of the value. There is a standard
		/// set of type markers; please see the documention of <see cref="StandardLiteralHandlers"/>.</param>
		/// <returns>Returns either the parsed value or an error message. Does not throw.</returns>
		Either<object, LogMessage> TryParse(UString textValue, Symbol typeMarker);
	}

}
