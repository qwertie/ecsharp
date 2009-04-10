// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 3037 $</version>
// </file>

using System;
using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Document
{
	/// <summary>
	/// A highlighting strategy for a buffer.
	/// </summary>
	public interface IHighlightingStrategy
	{
		/// <value>
		/// The name of the highlighting strategy, must be unique (e.g. "C#")
		/// </value>
		string Name {
			get;
		}
		
		/// <value>
		/// The file extenstions on which this highlighting strategy gets
		/// used, e.g. { ".cs" }
		/// </value>
		string[] Extensions {
			get;
		}
		
		/// <remarks>DLP: C# only has one entry, "LineComment" => "//". This entry
		/// is accessed by the ToggleComment edit action. Also BlockCommentBegin and
		/// BlockCommentEnd may be looked up.</remarks>
		Dictionary<string, string> Properties {
			get;
		}
		
		// returns special color. (BackGround Color, Cursor Color and so on)
		
		/// <remarks>
		/// Gets the color of an Environment element.
		/// </remarks>
		HighlightColor GetColorFor(string name);
		
		/// <remarks>
		/// Used internally, do not call
		/// </remarks>
		void MarkTokens(IDocument document, List<LineSegment> lines);
		
		/// <remarks>
		/// Used internally, do not call
		/// </remarks>
		void MarkTokens(IDocument document);
	}
	
	public interface IHighlightingStrategyUsingRuleSets : IHighlightingStrategy
	{
		/// <remarks>
		/// Used internally, do not call
		/// </remarks>
		HighlightRuleSet GetRuleSet(Span span);
		
		/// <remarks>
		/// Used internally, do not call
		/// </remarks>
		HighlightColor GetColor(IDocument document, LineSegment keyWord, int index, int length);
	}
}
