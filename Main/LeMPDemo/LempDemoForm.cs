using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using Loyc;

namespace TextEditor
{
	/// <summary>Main form for a multi-file text editor based on 
	/// ICSharpCode.TextEditor.TextEditorControl.</summary>
	public partial class LempDemoForm : Form
	{
		string DemoText =
			"#ecs;\n" +
			"using System(, .Collections.Generic);\n" +
			"using Loyc(, .Collections, .Syntax, .Syntax.Lexing);\n" +
			"namespace Example;\n" +
			"\n" +
			"// Create a class with some properties\n" +
			"public class Class\n" +
			"{\n" +
			"	public this(\n" +
			"	       public string PropertyA { get; private set; },\n" +
			"	       public Symbol PropertyB { get; private set; } = @@Hello) {}\n" +
			"}\n\n" +
			"static string RemoveCommentFromEndOfLine(string line) =>\n" +
			"	line.IndexOf(\"//\")::i > -1 ? line.Substring(0, i) : line;\n";

		public LempDemoForm()
		{
			InitializeComponent();
			AddNewTextEditor("Untitled", DemoText);
		}

		#region Code related to File menu

		private void menuFileNew_Click(object sender, EventArgs e)
		{
			AddNewTextEditor("New file", DemoText);
		}

		/// <summary>This variable holds the editor settings (whether to show line 
		/// numbers, etc.) that all editor controls share.</summary>
		ITextEditorProperties _editorSettings;

		private LempDemoPanel AddNewTextEditor(string title, string initialText = "")
		{
			var tab = new TabPage(title);
			// When a tab page gets the focus, move the focus to the editor control
			// instead when it gets the Enter (focus) event. I use BeginInvoke 
			// because changing the focus directly in the Enter handler doesn't 
			// work.
			tab.Enter +=
				new EventHandler((sender, e) => { 
					var page = ((TabPage)sender);
					page.BeginInvoke(new Action<TabPage>(p => p.Controls[0].Focus()), page);
				});

			var panel = new LempDemoPanel(_editorSettings);
			_editorSettings = panel.EditorSettings;
			panel.Dock = System.Windows.Forms.DockStyle.Fill;
	
			tab.Controls.Add(panel);
			fileTabs.Controls.Add(tab);

			panel.Editor.Text = initialText;
			panel.SetModifiedFlag(false);
			return panel;
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
				&& ActivePage.Editor.Document.TextLength == 0
				&& string.IsNullOrEmpty(ActivePage.Editor.FileName))
				RemoveTextEditor(ActivePage);

			// Open file(s)
			foreach (string fn in fns)
			{
				var panel = AddNewTextEditor(Path.GetFileName(fn));
				try {
					panel.LoadFile(fn);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, ex.GetType().Name);
					RemoveTextEditor(panel);
					return;
				}
			}
		}

		private void menuFileClose_Click(object sender, EventArgs e)
		{
			if (ActivePage != null)
				RemoveTextEditor(ActivePage);
		}

		private void RemoveTextEditor(LempDemoPanel panel)
		{
			((TabControl)panel.Parent.Parent).Controls.Remove(panel.Parent);
		}

		private void menuFileSave_Click(object sender, EventArgs e)
		{
			LempDemoPanel panel = ActivePage;
			if (panel != null)
				DoSave(panel);
		}

		private bool DoSave(LempDemoPanel panel)
		{
			if (string.IsNullOrEmpty(panel.Editor.FileName))
				return DoSaveAs(panel);
			else {
				try {
					panel.Editor.SaveFile(panel.Editor.FileName);
					panel.SetModifiedFlag(false);
					return true;
				} catch (Exception ex) {
					MessageBox.Show(ex.Message, ex.GetType().Name);
					return false;
				}
			}
		}

		private void menuFileSaveAs_Click(object sender, EventArgs e)
		{
			var panel = ActivePage;
			if (panel != null)
				DoSaveAs(panel);
		}

		private bool DoSaveAs(LempDemoPanel panel)
		{
			TextEditorControl editor = panel.Editor;
			saveFileDialog.FileName = editor.FileName;
			if (saveFileDialog.ShowDialog() == DialogResult.OK) {
				try {
					editor.SaveFile(saveFileDialog.FileName);
					editor.Parent.Text = Path.GetFileName(editor.FileName);
					panel.SetModifiedFlag(false);
					
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
		private void DoEditAction(LempDemoPanel panel, ICSharpCode.TextEditor.Actions.IEditAction action)
		{
			TextEditorControl editor = panel.Editor;
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

		// These menu items prevent clipboard operations on the OTHER controls. This
		// is true even if we don't do anything when 
		//private void menuEditCut_Click(object sender, EventArgs e)
		//{
		//	if (HaveSelection())
		//		DoEditAction(ActivePage, new ICSharpCode.TextEditor.Actions.Cut());
		//}
		//private void menuEditCopy_Click(object sender, EventArgs e)
		//{
		//	if (HaveSelection())
		//		DoEditAction(ActivePage, new ICSharpCode.TextEditor.Actions.Copy());
		//}
		//private void menuEditPaste_Click(object sender, EventArgs e)
		//{
		//	DoEditAction(ActivePage, new ICSharpCode.TextEditor.Actions.Paste());
		//}
		//private void menuEditDelete_Click(object sender, EventArgs e)
		//{
		//	if (HaveSelection())
		//		DoEditAction(ActivePage, new ICSharpCode.TextEditor.Actions.Delete());
		//}

		private bool HaveSelection()
		{
			var panel = ActivePage;
			return panel != null &&
				panel.Editor.ActiveTextAreaControl.TextArea.SelectionManager.HasSomethingSelected;
		}

		FindAndReplaceForm _findForm = new FindAndReplaceForm();

		private void menuEditFind_Click(object sender, EventArgs e)
		{
			LempDemoPanel panel = ActivePage;
			if (panel == null) return;
			_findForm.ShowFor(panel.FocusedEditor, false);
		}

		private void menuEditReplace_Click(object sender, EventArgs e)
		{
			LempDemoPanel panel = ActivePage;
			if (panel == null) return;
			_findForm.ShowFor(panel.FocusedEditor, true);
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
			var panel = ActivePage;
			if (panel != null) {
				DoEditAction(ActivePage, new ICSharpCode.TextEditor.Actions.ToggleBookmark());
				panel.Editor.IsIconBarVisible = panel.Editor.Document.BookmarkManager.Marks.Count > 0;
			}
		}

		private void menuGoToNextBookmark_Click(object sender, EventArgs e)
		{
			DoEditAction(ActivePage, new ICSharpCode.TextEditor.Actions.GotoNextBookmark
				(bookmark => true));
		}

		private void menuGoToPrevBookmark_Click(object sender, EventArgs e)
		{
			DoEditAction(ActivePage, new ICSharpCode.TextEditor.Actions.GotoPrevBookmark
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
			LempDemoPanel editor = ActivePage;
			if (editor == null) return;
			editor.FocusedEditor.Split();
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
			TextEditorControl editor = ActivePage.Editor;
			if (editor == null) return;
			editor.ShowSpaces = editor.ShowTabs = !editor.ShowSpaces;
			OnSettingsChanged();
		}
		private void menuShowNewlines_Click(object sender, EventArgs e)
		{
			TextEditorControl editor = ActivePage.Editor;
			if (editor == null) return;
			editor.ShowEOLMarkers = !editor.ShowEOLMarkers;
			OnSettingsChanged();
		}

		private void menuHighlightCurrentRow_Click(object sender, EventArgs e)
		{
			TextEditorControl editor = ActivePage.Editor;
			if (editor == null) return;
			editor.LineViewerStyle = editor.LineViewerStyle == LineViewerStyle.None 
				? LineViewerStyle.FullRow : LineViewerStyle.None;
			OnSettingsChanged();
		}

		private void menuBracketMatchingStyle_Click(object sender, EventArgs e)
		{
			TextEditorControl editor = ActivePage.Editor;
			if (editor == null) return;
			editor.BracketMatchingStyle = editor.BracketMatchingStyle == BracketMatchingStyle.After 
				? BracketMatchingStyle.Before : BracketMatchingStyle.After;
			OnSettingsChanged();
		}

		private void menuEnableVirtualSpace_Click(object sender, EventArgs e)
		{
			TextEditorControl editor = ActivePage.Editor;
			if (editor == null) return;
			editor.AllowCaretBeyondEOL = !editor.AllowCaretBeyondEOL;
			OnSettingsChanged();
		}

		private void menuShowLineNumbers_Click(object sender, EventArgs e)
		{
			TextEditorControl editor = ActivePage.Editor;
			if (editor == null) return;
			editor.ShowLineNumbers = !editor.ShowLineNumbers;
			OnSettingsChanged();
		}

		private void menuSetTabSize_Click(object sender, EventArgs e)
		{
			if (ActivePage != null) {
				string result = InputBox.Show("Specify the desired tab width.", "Tab size", _editorSettings.TabIndent.ToString());
				int value;
				if (result != null && int.TryParse(result, out value) && value.IsInRange(1, 32)) {
					ActivePage.Editor.TabIndent = value;
					ActivePage.OutEditor.TabIndent = value;
				}
			}
		}
		
		private void menuSetFont_Click(object sender, EventArgs e)
		{
			var panel = ActivePage;
			if (panel != null) {
				fontDialog.Font = panel.Editor.Font;
				if (fontDialog.ShowDialog(this) == DialogResult.OK) {
					panel.Editor.Font = fontDialog.Font;
					panel.OutEditor.Font = fontDialog.Font;
					OnSettingsChanged();
				}
			}
		}

		#endregion

		#region Other stuff

		private void TextEditor_FormClosing(object sender, FormClosingEventArgs e)
		{
			// Ask user to save changes
			foreach (var panel in AllEditors)
			{
				if (panel.IsModified())
				{
					var r = MessageBox.Show(string.Format("Save changes to {0}?", panel.Editor.FileName ?? "new file"),
						"Save?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
					if (r == DialogResult.Cancel)
						e.Cancel = true;
					else if (r == DialogResult.Yes)
						if (!DoSave(panel))
							e.Cancel = true;
				}
			}
		}

		/// <summary>Returns a list of all editor controls</summary>
		private IEnumerable<LempDemoPanel> AllEditors
		{
			get {
				return from t in fileTabs.Controls.Cast<TabPage>()
					   from c in t.Controls.OfType<LempDemoPanel>()
					   select c;
			}
		}
		
		/// <summary>Returns the currently displayed editor page, or null if none are open</summary>
		private LempDemoPanel ActivePage
		{
			get {
				if (fileTabs.TabPages.Count == 0) return null;
				return fileTabs.SelectedTab.Controls.OfType<LempDemoPanel>().FirstOrDefault();
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

		private void saveOutputPaneToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (ActivePage != null) ActivePage.SaveOutput();
		}
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
}