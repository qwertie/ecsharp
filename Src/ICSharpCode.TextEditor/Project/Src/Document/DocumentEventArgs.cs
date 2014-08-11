// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 915 $</version>
// </file>

using System;

namespace ICSharpCode.TextEditor.Document
{
	/// <summary>
	/// This delegate is used for document events.
	/// </summary>
	public delegate void DocumentEventHandler(object sender, DocumentEventArgs e);
	
	/// <summary>
	/// This class contains more information on a document event
	/// </summary>
	public class DocumentEventArgs : EventArgs
	{
		IDocument document;
		int       offset;
		int       length;
		string    text;
		
		/// <returns>
		/// always a valid Document which is related to the Event.
		/// </returns>
		public IDocument Document {
			get {
				return document;
			}
		}
		
		/// <returns>
		/// -1 if no offset was specified for this event
		/// </returns>
		public int Offset {
			get {
				return offset;
			}
		}
		
		/// <summary>DLP: Specifies the text that is being inserted, or null if text is
		/// being removed. Typically either Text==null or Length==-1 depending on
		/// whether text is being removed or inserted, but both members are used in
		/// case of a Replace operation.</summary>
		/// <returns>
		/// null if no text was specified for this event
		/// </returns>
		public string Text {
			get {
				return text;
			}
		}
		
		/// <summary>DLP: It seems that Length specifies the amount of text that is
		/// being removed. If this object represents an insert operation then Length
		/// is -1. There is one exception: if IDocument.TextContent is being set,
		/// then Length is 0, Offset is 0, and Text is the new text.</summary>
		/// <returns>
		/// -1 if no length was specified for this event
		/// </returns>
		public int Length {
			get {
				return length;
			}
		}
		
		/// <summary>
		/// Creates a new instance off <see cref="DocumentEventArgs"/>
		/// </summary>
		public DocumentEventArgs(IDocument document) : this(document, -1, -1, null)
		{
		}
		
		/// <summary>
		/// Creates a new instance off <see cref="DocumentEventArgs"/>
		/// </summary>
		public DocumentEventArgs(IDocument document, int offset) : this(document, offset, -1, null)
		{
		}
		
		/// <summary>
		/// Creates a new instance off <see cref="DocumentEventArgs"/>
		/// </summary>
		public DocumentEventArgs(IDocument document, int offset, int length) : this(document, offset, length, null)
		{
		}
		
		/// <summary>
		/// Creates a new instance off <see cref="DocumentEventArgs"/>
		/// </summary>
		public DocumentEventArgs(IDocument document, int offset, int length, string text)
		{
			this.document = document;
			this.offset   = offset;
			this.length   = length;
			this.text     = text;
		}
		public override string ToString()
		{
			return String.Format("[DocumentEventArgs: Document = {0}, Offset = {1}, Text = {2}, Length = {3}]",
			                     Document,
			                     Offset,
			                     Text,
			                     Length);
		}
	}
}
