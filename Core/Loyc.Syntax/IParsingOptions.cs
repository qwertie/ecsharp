using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Syntax
{
	/// <summary>A set of relatively universal parsing options that 
	/// <see cref="IParsingService"/>s should understand.</summary>
	/// <seealso cref="ParsingOptions"/>
	public interface IParsingOptions
	{
		/// <summary>A <see cref="global::Loyc.Syntax.ParsingMode"/> value indicating which part of the 
		/// language is being parsed (e.g. expressions, or an entire file).</summary>
		ParsingMode Mode { get; }
		/// <summary>Whether to preserve comments and newlines by attaching trivia 
		/// attributes to the output. If <see cref="IParsingService.CanPreserveComments"/> 
		/// is false, this parameter will have no effect.</summary>
		bool PreserveComments { get; }
		/// <summary>Indicates that the parsing service is only being used for 
		/// syntax highlighting, so the content of literals is not important. The
		/// produced tokens or LNode can claim every literal is null.</summary>
		bool SurfaceScanOnly { get; }

		/// <summary>If tabs are significant, this option controls the number of 
		/// spaces a single tab should be equated with.</summary>
		int SpacesPerTab { get; }

		/// <summary>Requests that a specific parser be used to convert literals into strings.</summary>
		/// <remarks>Support for this property is optional, and if it is supported,
		/// the parser may choose to use this parser only for type markers for which
		/// it does not have built-in support, or it may use it for all literals.
		/// </remarks>
		ILiteralParser LiteralParser { get; }
	}

	/// <summary>A simple implementation of <see cref="IParsingOptions"/>.</summary>
	public class ParsingOptions : IParsingOptions
	{
		public ParsingMode Mode { get; set; } = ParsingMode.File;

		public bool PreserveComments { get; set; } = true;

		public bool SurfaceScanOnly { get; set; } = false;
		
		public int SpacesPerTab { get; set; } = 4;
		
		public ILiteralParser LiteralParser { get; set; }
	}
}
