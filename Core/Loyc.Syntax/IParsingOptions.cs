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
	}

	/// <summary>A simple implementation of <see cref="IParsingOptions"/>.</summary>
	class ParsingOptions : IParsingOptions
	{
		public ParsingMode Mode { get; set; } = ParsingMode.File;

		public bool PreserveComments { get; set; } = true;

		public bool SurfaceScanOnly { get; set; } = false;
		
		public int SpacesPerTab { get; set; } = 4;
	}
}
