using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace LoycFileGeneratorForVs
{
	// This was the first technique I found for running code when VS starts.
	// It's not ideal, as it requires the user to open a source file, but it's
	// simpler than creating a "Visual Studio Package".

	[Export(typeof(ITaggerProvider))] // from System.ComponentModel.Composition.dll
	[ContentType("text")]             // from Microsoft.VisualStudio.CoreUtility.dll
	[TagType(typeof(TextMarkerTag))]  // TextMarkerTag is in Microsoft.VisualStudio.Text.UI.dll
	internal sealed class FakeTagProvider : ITaggerProvider
	{
		// ITextBuffer is from Microsoft.VisualStudio.Text.Data.dll
		public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
		{
			Trace.WriteLine(string.Format("**** CreateTagger<{0}>", typeof(T).Name));
			return null;
		}
	}
}
