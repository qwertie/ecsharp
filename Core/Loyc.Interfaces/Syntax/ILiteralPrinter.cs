using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Syntax
{
	/// <summary>This interface for converting literals to text is implemented by <see cref="LiteralHandlerTable"/>.</summary>
	public interface ILiteralPrinter
	{
		/// <summary>Finds out whether there is a printer for the given type marker. Never throws.</summary>
		/// <returns>True if typeMarker is not null and there is a printer for that marker.</returns>
		bool CanPrint(Symbol typeMarker);

		/// <summary>Returns true if there is a printer registered for the given type. Never throws.</summary>
		/// <returns>True if type is not null and if there is a printer for that type.</returns>
		bool CanPrint(Type type);

		/// <summary>Attempts to convert the specified literal to a string.</summary>
		/// <param name="literal">A literal that you want to convert to a string.</param>
		/// <returns>Either a recommended type marker for the literal, or an error 
		/// message. The string form of the literal is appended to the StringBuilder
		/// provided by the caller. If an error occurs, it is possible that some kind 
		/// of output was added to the StringBuilder anyway.</returns>
		/// <remarks>The string is printed without escaping. For example a newline 
		/// would be printed as character (char)10, not "\n".</remarks>
		Either<Symbol, ILogMessage> TryPrint(ILNode literal, StringBuilder sb);
	}
}
