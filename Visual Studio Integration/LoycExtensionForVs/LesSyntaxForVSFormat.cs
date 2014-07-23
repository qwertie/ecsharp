using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace LesSyntaxForVS
{
	/// <summary>
	/// Defines an editor format for the LesSyntaxForVS type that has a purple background
	/// and is underlined.
	/// </summary>
	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "LesSyntaxForVS")]
	[Name("LesSyntaxForVS")]
	[UserVisible(true)] //this should be visible to the end user
	[Order(Before = Priority.Default)] //set the priority to be after the default classifiers
	internal sealed class LesSyntaxForVSFormat : ClassificationFormatDefinition
	{
		/// <summary>
		/// Defines the visual format for the "LesSyntaxForVS" classification type
		/// </summary>
		public LesSyntaxForVSFormat()
		{
			this.DisplayName = "LesSyntaxForVS"; //human readable version of the name
			this.BackgroundColor = Colors.BlueViolet;
			this.IsItalic = true;
			this.TextDecorations = System.Windows.TextDecorations.Underline;
		}
	}
}
