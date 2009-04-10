using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

namespace TextEditor
{
	/// <summary>Main form for a multi-file text editor based on 
	/// ICSharpCode.TextEditor.TextEditorControl.</summary>
	public partial class TextEditorForm : Form
	{
		public TextEditorForm()
		{
			InitializeComponent();
			AddNewTextEditor("New file");
		}

		#region Code related to File menu

		private void menuFileNew_Click(object sender, EventArgs e)
		{
			AddNewTextEditor("New file");
		}

		/// <summary>This variable holds the settings (whether to show line numbers, 
		/// etc.) that all editor controls share.</summary>
		ITextEditorProperties _editorSettings;

		private TextEditorControl AddNewTextEditor(string title)
		{
			var tab = new TabPage(title);
			var editor = new TextEditorControl();
			editor.Dock = System.Windows.Forms.DockStyle.Fill;
			editor.IsReadOnly = false;
			editor.Document.DocumentChanged += 
				new DocumentEventHandler((sender, e) => { SetModifiedFlag(editor, true); });
			// When a tab page gets the focus, move the focus to the editor control
			// instead when it gets the Enter (focus) event. I use BeginInvoke 
			// because changing the focus directly in the Enter handler doesn't 
			// work.
			tab.Enter +=
				new EventHandler((sender, e) => { 
					var page = ((TabPage)sender);
					page.BeginInvoke(new Action<TabPage>(p => p.Controls[0].Focus()), page);
				});
			tab.Controls.Add(editor);
			fileTabs.Controls.Add(tab);

			if (_editorSettings == null) {
				_editorSettings = editor.TextEditorProperties;
				OnSettingsChanged();
			} else
				editor.TextEditorProperties = _editorSettings;
			return editor;
		}

		private void menuFileOpen_Click(object sender, EventArgs e)
		{
			if (openFileDialog.ShowDialog() == DialogResult.OK)
				// Try to open chosen file
				OpenFiles(openFileDialog.FileNames);
		}

		private void OpenFiles(string[] fns)
		{
			// Close default untitled document if it is still empty
			if (fileTabs.TabPages.Count == 1 
				&& ActiveEditor.Document.TextLength == 0
				&& string.IsNullOrEmpty(ActiveEditor.FileName))
				RemoveTextEditor(ActiveEditor);

			// Open file(s)
			foreach (string fn in fns)
			{
				var editor = AddNewTextEditor(Path.GetFileName(fn));
				try {
					editor.LoadFile(fn);
					editor.Document.HighlightingStrategy = new TempHighlightWrapper(
						editor.Document.HighlightingStrategy);
					// Modified flag is set during loading because the document 
					// "changes" (from nothing to something). So, clear it again.
					SetModifiedFlag(editor, false);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, ex.GetType().Name);
					RemoveTextEditor(editor);
					return;
				}
				
				// ICSharpCode.TextEditor doesn't have any built-in code folding
				// strategies, so I've included a simple one. Apparently, the
				// foldings are not updated automatically, so in this demo the user
				// cannot add or remove folding regions after loading the file.
				editor.Document.FoldingManager.FoldingStrategy = new RegionFoldingStrategy();
				editor.Document.FoldingManager.UpdateFoldings(null, null);
			}
		}

		private void menuFileClose_Click(object sender, EventArgs e)
		{
			if (ActiveEditor != null)
				RemoveTextEditor(ActiveEditor);
		}

		private void RemoveTextEditor(TextEditorControl editor)
		{
			((TabControl)editor.Parent.Parent).Controls.Remove(editor.Parent);
		}

		private void menuFileSave_Click(object sender, EventArgs e)
		{
			TextEditorControl editor = ActiveEditor;
			if (editor != null)
				DoSave(editor);
		}

		private bool DoSave(TextEditorControl editor)
		{
			if (string.IsNullOrEmpty(editor.FileName))
				return DoSaveAs(editor);
			else {
				try {
					editor.SaveFile(editor.FileName);
					SetModifiedFlag(editor, false);
					return true;
				} catch (Exception ex) {
					MessageBox.Show(ex.Message, ex.GetType().Name);
					return false;
				}
			}
		}

		private void menuFileSaveAs_Click(object sender, EventArgs e)
		{
			var editor = ActiveEditor;
			if (editor != null)
				DoSaveAs(editor);
		}

		private bool DoSaveAs(TextEditorControl editor)
		{
			saveFileDialog.FileName = editor.FileName;
			if (saveFileDialog.ShowDialog() == DialogResult.OK) {
				try {
					editor.SaveFile(saveFileDialog.FileName);
					editor.Parent.Text = Path.GetFileName(editor.FileName);
					SetModifiedFlag(editor, false);
					
					// The syntax highlighting strategy doesn't change
					// automatically, so do it manually.
					editor.Document.HighlightingStrategy =
						HighlightingStrategyFactory.CreateHighlightingStrategyForFile(editor.FileName);
					return true;
				} catch (Exception ex) {
					MessageBox.Show(ex.Message, ex.GetType().Name);
				}
			}
			return false;
		}

		private void menuFileExit_Click(object sender, EventArgs e)
		{
			this.Close();
		}
		
		#endregion

		#region Code related to Edit menu

		/// <summary>Performs an action encapsulated in IEditAction.</summary>
		/// <remarks>
		/// There is an implementation of IEditAction for every action that 
		/// the user can invoke using a shortcut key (arrow keys, Ctrl+X, etc.)
		/// The editor control doesn't provide a public funciton to perform one
		/// of these actions directly, so I wrote DoEditAction() based on the
		/// code in TextArea.ExecuteDialogKey(). You can call ExecuteDialogKey
		/// directly, but it is more fragile because it takes a Keys value (e.g.
		/// Keys.Left) instead of the action to perform.
		/// <para/>
		/// Clipboard commands could also be done by calling methods in
		/// editor.ActiveTextAreaControl.TextArea.ClipboardHandler.
		/// </remarks>
		private void DoEditAction(TextEditorControl editor, ICSharpCode.TextEditor.Actions.IEditAction action)
		{
			if (editor != null && action != null) {
				var area = editor.ActiveTextAreaControl.TextArea;
				editor.BeginUpdate();
				try {
					lock (editor.Document) {
						action.Execute(area);
						if (area.SelectionManager.HasSomethingSelected && area.AutoClearSelection /*&& caretchanged*/) {
							if (area.Document.TextEditorProperties.DocumentSelectionMode == DocumentSelectionMode.Normal) {
								area.SelectionManager.ClearSelection();
							}
						}
					}
				} finally {
					editor.EndUpdate();
					area.Caret.UpdateCaretPosition();
				}
			}
		}

		private void menuEditCut_Click(object sender, EventArgs e)
		{
			if (HaveSelection())
				DoEditAction(ActiveEditor, new ICSharpCode.TextEditor.Actions.Cut());
		}
		private void menuEditCopy_Click(object sender, EventArgs e)
		{
			if (HaveSelection())
				DoEditAction(ActiveEditor, new ICSharpCode.TextEditor.Actions.Copy());
		}
		private void menuEditPaste_Click(object sender, EventArgs e)
		{
			DoEditAction(ActiveEditor, new ICSharpCode.TextEditor.Actions.Paste());
		}
		private void menuEditDelete_Click(object sender, EventArgs e)
		{
			if (HaveSelection())
				DoEditAction(ActiveEditor, new ICSharpCode.TextEditor.Actions.Delete());
		}

		private bool HaveSelection()
		{
			var editor = ActiveEditor;
			return editor != null &&
				editor.ActiveTextAreaControl.TextArea.SelectionManager.HasSomethingSelected;
		}

		FindAndReplaceForm _findForm = new FindAndReplaceForm();

		private void menuEditFind_Click(object sender, EventArgs e)
		{
			TextEditorControl editor = ActiveEditor;
			if (editor == null) return;
			_findForm.ShowFor(editor, false);
		}

		private void menuEditReplace_Click(object sender, EventArgs e)
		{
			TextEditorControl editor = ActiveEditor;
			if (editor == null) return;
			_findForm.ShowFor(editor, true);
		}

		private void menuFindAgain_Click(object sender, EventArgs e)
		{
			_findForm.FindNext(true, false, 
				string.Format("Search text «{0}» not found.", _findForm.LookFor));
		}
		private void menuFindAgainReverse_Click(object sender, EventArgs e)
		{
			_findForm.FindNext(true, true, 
				string.Format("Search text «{0}» not found.", _findForm.LookFor));
		}

		private void menuToggleBookmark_Click(object sender, EventArgs e)
		{
			var editor = ActiveEditor;
			if (editor != null) {
				DoEditAction(ActiveEditor, new ICSharpCode.TextEditor.Actions.ToggleBookmark());
				editor.IsIconBarVisible = editor.Document.BookmarkManager.Marks.Count > 0;
			}
		}

		private void menuGoToNextBookmark_Click(object sender, EventArgs e)
		{
			DoEditAction(ActiveEditor, new ICSharpCode.TextEditor.Actions.GotoNextBookmark
				(bookmark => true));
		}

		private void menuGoToPrevBookmark_Click(object sender, EventArgs e)
		{
			DoEditAction(ActiveEditor, new ICSharpCode.TextEditor.Actions.GotoPrevBookmark
				(bookmark => true));
		}

		#endregion

		#region Code related to Options menu

		/// <summary>Toggles whether the editor control is split in two parts.</summary>
		/// <remarks>Exercise for the reader: modify TextEditorControl and
		/// TextAreaControl so it shows a little "splitter stub" like you see in
		/// other apps, that allows the user to split the text editor by dragging
		/// it.</remarks>
		private void menuSplitTextArea_Click(object sender, EventArgs e)
		{
			TextEditorControl editor = ActiveEditor;
			if (editor == null) return;
			editor.Split();
		}

		/// <summary>Show current settings on the Options menu</summary>
		/// <remarks>We don't have to sync settings between the editors because 
		/// they all share the same DefaultTextEditorProperties object.</remarks>
		private void OnSettingsChanged()
		{
			menuShowSpacesTabs.Checked = _editorSettings.ShowSpaces;
			menuShowNewlines.Checked = _editorSettings.ShowEOLMarker;
			menuHighlightCurrentRow.Checked = _editorSettings.LineViewerStyle == LineViewerStyle.FullRow;
			menuBracketMatchingStyle.Checked = _editorSettings.BracketMatchingStyle == BracketMatchingStyle.After;
			menuEnableVirtualSpace.Checked = _editorSettings.AllowCaretBeyondEOL;
			menuShowLineNumbers.Checked = _editorSettings.ShowLineNumbers;
		}

		private void menuShowSpaces_Click(object sender, EventArgs e)
		{
			TextEditorControl editor = ActiveEditor;
			if (editor == null) return;
			editor.ShowSpaces = editor.ShowTabs = !editor.ShowSpaces;
			OnSettingsChanged();
		}
		private void menuShowNewlines_Click(object sender, EventArgs e)
		{
			TextEditorControl editor = ActiveEditor;
			if (editor == null) return;
			editor.ShowEOLMarkers = !editor.ShowEOLMarkers;
			OnSettingsChanged();
		}

		private void menuHighlightCurrentRow_Click(object sender, EventArgs e)
		{
			TextEditorControl editor = ActiveEditor;
			if (editor == null) return;
			editor.LineViewerStyle = editor.LineViewerStyle == LineViewerStyle.None 
				? LineViewerStyle.FullRow : LineViewerStyle.None;
			OnSettingsChanged();
		}

		private void menuBracketMatchingStyle_Click(object sender, EventArgs e)
		{
			TextEditorControl editor = ActiveEditor;
			if (editor == null) return;
			editor.BracketMatchingStyle = editor.BracketMatchingStyle == BracketMatchingStyle.After 
				? BracketMatchingStyle.Before : BracketMatchingStyle.After;
			OnSettingsChanged();
		}

		private void menuEnableVirtualSpace_Click(object sender, EventArgs e)
		{
			TextEditorControl editor = ActiveEditor;
			if (editor == null) return;
			editor.AllowCaretBeyondEOL = !editor.AllowCaretBeyondEOL;
			OnSettingsChanged();
		}

		private void menuShowLineNumbers_Click(object sender, EventArgs e)
		{
			TextEditorControl editor = ActiveEditor;
			if (editor == null) return;
			editor.ShowLineNumbers = !editor.ShowLineNumbers;
			OnSettingsChanged();
		}

		private void menuSetTabSize_Click(object sender, EventArgs e)
		{
			if (ActiveEditor != null) {
				string result = InputBox.Show("Specify the desired tab width.", "Tab size", _editorSettings.TabIndent.ToString());
				int value;
				if (result != null && int.TryParse(result, out value) && value.IsInRange(1, 32)) {
					ActiveEditor.TabIndent = value;
				}
			}
		}
		
		private void menuSetFont_Click(object sender, EventArgs e)
		{
			var editor = ActiveEditor;
			if (editor != null) {
				fontDialog.Font = editor.Font;
				if (fontDialog.ShowDialog(this) == DialogResult.OK) {
					editor.Font = fontDialog.Font;
					OnSettingsChanged();
				}
			}
		}

		#endregion

		#region Other stuff

		private void TextEditor_FormClosing(object sender, FormClosingEventArgs e)
		{
			// Ask user to save changes
			foreach (var editor in AllEditors)
			{
				if (IsModified(editor))
				{
					var r = MessageBox.Show(string.Format("Save changes to {0}?", editor.FileName ?? "new file"),
						"Save?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
					if (r == DialogResult.Cancel)
						e.Cancel = true;
					else if (r == DialogResult.Yes)
						if (!DoSave(editor))
							e.Cancel = true;
				}
			}
		}

		/// <summary>Returns a list of all editor controls</summary>
		private IEnumerable<TextEditorControl> AllEditors
		{
			get {
				return from t in fileTabs.Controls.Cast<TabPage>()
					   from c in t.Controls.OfType<TextEditorControl>()
					   select c;
			}
		}
		
		/// <summary>Returns the currently displayed editor, or null if none are open</summary>
		private TextEditorControl ActiveEditor
		{
			get {
				if (fileTabs.TabPages.Count == 0) return null;
				return fileTabs.SelectedTab.Controls.OfType<TextEditorControl>().FirstOrDefault();
			}
		}
		
		/// <summary>Gets whether the file in the specified editor is modified.</summary>
		/// <remarks>TextEditorControl doesn't maintain its own internal modified 
		/// flag, so we use the '*' shown after the file name to represent the 
		/// modified state.</remarks>
		private bool IsModified(TextEditorControl editor)
		{
			// TextEditorControl doesn't seem to contain its own 'modified' flag, so 
			// instead we'll treat the "*" on the filename as the modified flag.
			return editor.Parent.Text.EndsWith("*");
		}
		private void SetModifiedFlag(TextEditorControl editor, bool flag)
		{
			if (IsModified(editor) != flag)
			{
				var p = editor.Parent;
				if (IsModified(editor))
					p.Text = p.Text.Substring(0, p.Text.Length - 1);
				else
					p.Text += "*";
			}
		}

		/// <summary>We handle DragEnter and DragDrop so users can drop files on the editor.</summary>
		private void TextEditorForm_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
				e.Effect = DragDropEffects.Copy;
		}
		private void TextEditorForm_DragDrop(object sender, DragEventArgs e)
		{
			string[] list = e.Data.GetData(DataFormats.FileDrop) as string[];
			if (list != null)
				OpenFiles(list);
		}

		#endregion
	}

	/// <summary>
	/// The class to generate the foldings, it implements ICSharpCode.TextEditor.Document.IFoldingStrategy
	/// </summary>
	public class RegionFoldingStrategy : IFoldingStrategy
	{
		/// <summary>
		/// Generates the foldings for our document.
		/// </summary>
		/// <param name="document">The current document.</param>
		/// <param name="fileName">The filename of the document.</param>
		/// <param name="parseInformation">Extra parse information, not used in this sample.</param>
		/// <returns>A list of FoldMarkers.</returns>
		public List<FoldMarker> GenerateFoldMarkers(IDocument document, string fileName, object parseInformation)
		{
			List<FoldMarker> list = new List<FoldMarker>();

			Stack<int> startLines = new Stack<int>();
			
			// Create foldmarkers for the whole document, enumerate through every line.
			for (int i = 0; i < document.TotalNumberOfLines; i++)
			{
				var seg = document.GetLineSegment(i);
				int offs, end = document.TextLength;
				char c;
				for (offs = seg.Offset; offs < end && ((c = document.GetCharAt(offs)) == ' ' || c == '\t'); offs++)
					{}
				if (offs == end) 
					break;
				int spaceCount = offs - seg.Offset;

				// now offs points to the first non-whitespace char on the line
				if (document.GetCharAt(offs) == '#') {
					string text = document.GetText(offs, seg.Length - spaceCount);
					if (text.StartsWith("#region"))
						startLines.Push(i);
					if (text.StartsWith("#endregion") && startLines.Count > 0) {
						// Add a new FoldMarker to the list.
						int start = startLines.Pop();
						list.Add(new FoldMarker(document, start, 
							document.GetLineSegment(start).Length, 
							i, spaceCount + "#endregion".Length));
					}
				}
			}
			
			return list;
		}
	}

	class TempHighlightWrapper : IHighlightingStrategy
	{
		IHighlightingStrategy _inner;
		IDocument _document;
		protected void SetDocument(IDocument document)
		{
			if (_document != document) {
				if (_document != null) {
					_document.DocumentChanged -= OnDocumentChanged;
					_document.DocumentAboutToBeChanged -= OnDocumentAboutToBeChanged;
				}
				_document = document;
				if (document != null) {
					_document.DocumentChanged += OnDocumentChanged;
					_document.DocumentAboutToBeChanged += OnDocumentAboutToBeChanged;
				}
			}
		}

		void OnDocumentAboutToBeChanged(object sender, DocumentEventArgs e)
		{

		}
		protected virtual void OnDocumentChanged(object sender, DocumentEventArgs e)
		{
		}

		public TempHighlightWrapper(IHighlightingStrategy inner)
		{
			_inner = inner;
		}

		public string Name
		{
			get { return _inner.Name; }
		}

		public string[] Extensions
		{
			get { return _inner.Extensions; }
		}

		public Dictionary<string, string> Properties
		{
			get { return _inner.Properties; }
		}

		public HighlightColor GetColorFor(string name)
		{
			Console.WriteLine("GetColorFor({0})", name);
			return _inner.GetColorFor(name);
		}

		public void MarkTokens(IDocument document, List<LineSegment> lines)
		{
			SetDocument(document);
			//_inner.MarkTokens(document, lines);
		}

		public void MarkTokens(IDocument document)
		{
			SetDocument(document);
			_inner.MarkTokens(document);
		}
	}
}
