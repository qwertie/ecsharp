using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;

namespace Loyc.VisualStudio
{
	/// <summary>A place to put the things that we want to import from Visual Studio
	/// that will be shared among the taggers for our syntax highlighters. Currently
	/// I'm only importing one service, but by using a special class I can easily 
	/// import other things without changing the code of all the taggers (I thought
	/// I would need several different tagger classes but it turned out I could
	/// combine them into one per language so... this class seems redundant now).</summary>
	/// <remarks>Note that [Import] does not work on 'static' fields, so to gain 
	/// access to the imports in this class, each part must manually import an 
	/// instance of this class.</remarks>
	[Export(typeof(VSImports))]
	public class VSImports
	{
		/// <summary>This "registry" lets us get IClassificationType objects which 
		/// represent token types (string, comment, etc.) in Visual Studio.</summary>
		[Import]
		internal IClassificationTypeRegistryService ClassificationRegistry = null; // Set via MEF

		// Handy technique for learning about predefined classifications
		[ImportMany] IEnumerable<EditorFormatDefinition> EditorFormats { get; set; }
		
		// Predefined squiggle type names:
		// "syntax error"
		// "compiler error"
		// "other error"
		// "compiler warning"
	}

	/// <summary>Groups a Visual Studio text buffer with VSImports.</summary>
	public class VSBuffer
	{
		public VSBuffer(VSImports vs, ITextBuffer buffer) { VS = vs; Buffer = buffer; }
		public VSImports VS;
		public ITextBuffer Buffer;
	}
}
