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
		/// <summary>Returns true if this object has a parser for the specified type marker.
		/// Returns false if typeMarker == null.</summary>
		bool CanParse(Symbol typeMarker);

		/// <summary>Attempts to parse a string with a given type marker.</summary>
		/// <param name="textValue">A text value that has already been preprocessed to remove 
		///   escape sequences.</param>
		/// <param name="typeMarker">Indicates the type of the value. There is a standard
		///   set of type markers; please see the documention of <see cref="StandardLiteralHandlers"/>.
		///   If typeMarker is null, it is treated as an empty string.
		///   </param>
		/// <returns>Returns either the parsed value or an error message. Does not throw.</returns>
		/// <remarks>If the problem is that the type marker doesn't have an associated parser,
		/// the returned <see cref="LogMessage"/> should not have a Severity of Error; 
		/// <see cref="LiteralHandlerTable"/> uses <see cref="Severity.Note"/> for this.</remarks>
		Either<object, ILogMessage> TryParse(UString textValue, Symbol typeMarker);
	}

}
