using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using LeMP;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc.Utilities;

namespace TextEditor
{
	public partial class LempDemoPanel : UserControl
	{
		TextEditorControl _inEditor  = new TextEditorControl();
		TextEditorControl _outEditor = new TextEditorControl();
		
		public TextEditorControl Editor { get { return _inEditor; } }
		public TextEditorControl OutEditor { get { return _outEditor; } }
		public TextEditorControl FocusedEditor
		{
			get { return _outEditor.ActiveTextAreaControl.TextArea.Focused ? _outEditor : _inEditor; }
		}
		
		/// <summary>This variable holds the editor settings (whether to show line 
		/// numbers, etc.) that all editor controls share.</summary>
		public ITextEditorProperties EditorSettings { get; set; }

		public LempDemoPanel(ITextEditorProperties editorSettings)
		{
			InitializeComponent();

			_inEditor  = new TextEditorControl();
			_inEditor.Dock = System.Windows.Forms.DockStyle.Fill;
			_inEditor.Document.DocumentChanged += OnDocumentChanged;
			
			_outEditor = new TextEditorControl();
			_outEditor.Dock = System.Windows.Forms.DockStyle.Fill;

			innerSplitContainer.Panel1.Controls.Add(_inEditor);
			innerSplitContainer.Panel2.Controls.Add(_outEditor);

			if ((EditorSettings = editorSettings) == null) {
				EditorSettings = _inEditor.TextEditorProperties;
			} else
				_inEditor.TextEditorProperties = EditorSettings;
			_outEditor.TextEditorProperties = EditorSettings;

			// ICSharpCode.TextEditor doesn't have any built-in code folding
			// strategies, so I've included a simple one. Apparently, the
			// foldings are not updated automatically, so in this demo the user
			// cannot add or remove folding regions after loading the file.
			Editor.Document.FoldingManager.FoldingStrategy = new RegionFoldingStrategy();
			timer.Start(); // initially update foldings & run LeMP

			_inEditor.SetHighlighting("C#");
			_outEditor.SetHighlighting("C#");
		}

		void OnDocumentChanged(object sender, DocumentEventArgs e)
		{
			SetModifiedFlag(true);
			timer.Stop();
			timer.Start();
		}
		private void timer_Tick(object sender, EventArgs e)
		{
			timer.Stop();
			AutoRunLemp();
			Editor.Document.FoldingManager.UpdateFoldings(null, null);
		}
		private void btnRegen_Click(object sender, EventArgs e)
		{
			AutoRunLemp();
		}

		private void AutoRunLemp()
		{
			if (LempStarted)
				return;
			messageList.Items.Clear();
			
			var args = G.SplitCommandLineArguments(_txtOptions.Text);
			RunLeMP(args, _inEditor.Text, _inEditor.FileName ?? "Untitled.ecs");
		}

		public void ShowOutput(string text)
		{
			InvokeIfRequired(txt => _outEditor.Text = txt, text);
		}
		public void InvokeIfRequired<T>(Action<T> action, T value)
		{
			if (InvokeRequired)
				BeginInvoke(action, value);
			else
				action(value);
		}

		#region RunLeMP and related

		bool _lempStarted;
		bool LempStarted { 
			get { return _lempStarted; } 
			set { InvokeIfRequired(v => lblRunning.Visible = _lempStarted = v, value); }
		}
		string _outFileName;

		private void RunLeMP(IList<string> args, string inputCode, string inputPath)
		{
			var options = new BMultiMap<string, string>();
			UG.ProcessCommandLineArguments(args, options, "", LeMP.Compiler.ShortOptions, LeMP.Compiler.TwoArgOptions);

			string _;
			var KnownOptions = LeMP.Compiler.KnownOptions;
			if (options.TryGetValue("help", out _) || options.TryGetValue("?", out _)) {
				var ms = new MemoryStream();
				LeMP.Compiler.ShowHelp(LeMP.Compiler.KnownOptions, new StreamWriter(ms));
				string output = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
				_outFileName = null;
				ShowOutput(output);
			} else {
				var sink = MessageSink.FromDelegate(WriteMessage);
				var sourceFile = new InputOutput((UString)inputCode, Path.GetFileName(inputPath));

				var c = new Compiler(sink, sourceFile);
				c.Files = new List<InputOutput> { sourceFile };
				c.Parallel = false; // only one file, parallel doesn't help

				if (LeMP.Compiler.ProcessArguments(c, options)) {
					LeMP.Compiler.WarnAboutUnknownOptions(options, sink, KnownOptions);

					c.AddMacros(typeof(global::LeMP.StandardMacros).Assembly);
					c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP"));
					c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude"));
					if (inputPath.EndsWith(".les", StringComparison.OrdinalIgnoreCase))
						c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude.Les"));

					LempStarted = true;
					new Thread(() => {
						try {
							c.Run();
							// Must get OutFileName after calling Run()
							_outFileName = sourceFile.OutFileName;
							ShowOutput(c.Output.ToString());
						} finally { LempStarted = false; }
					}).Start();
				}
			}
		}
		
		class Compiler : global::LeMP.Compiler
		{
			public Compiler(IMessageSink sink, InputOutput file)
				: base(sink, typeof(global::LeMP.Prelude.Macros), new [] { file }) { }

			public StringBuilder Output = new StringBuilder();

			protected override void WriteOutput(InputOutput io)
			{
				VList<LNode> results = io.Output;

				using (LNode.PushPrinter(io.OutPrinter)) {
					Output.AppendFormat("// Generated from {1} by LeMP {2}.{0}", NewlineString,
						io.FileName, typeof(Compiler).Assembly.GetName().Version.ToString());
					foreach (LNode node in results) {
						LNode.Printer(node, Output, Sink, null, IndentString, NewlineString);
						Output.Append(NewlineString);
					}
				}
			}
		}

		void WriteMessage(Severity sev, object ctx, string msg, params object[] args)
		{
			BeginInvoke(new WriteMessageFn(WriteMessageCore), sev, ctx, msg, args);
		}
		void WriteMessageCore(Severity severity, object ctx, string fmt, params object[] args)
		{
			string msg = fmt;
			try {
				msg = Localize.From(fmt, args);
			} catch { }
			
			var pos = GetSourcePos(ctx);
			if (pos == null)
				msg = MessageSink.LocationString(ctx) + ": " + msg;

			var lvi = new ListViewItem(new string[] {
				pos != null ? pos.Line.ToString() : "",
				pos != null ? pos.PosInLine.ToString() : "",
				severity.ToString(), msg
			});
			lvi.BackColor = severity >= Severity.Error ? Color.Pink : 
			                severity >= Severity.Warning ? Color.LightYellow : 
							messageList.BackColor;
			messageList.Items.Add(lvi);
		}

		private SourcePos GetSourcePos(object context)
		{
			if (context is SourcePos)
				return (SourcePos)context;
			if (context is SourceRange)
				return ((SourceRange)context).Start;
			if (context is LNode)
				return ((LNode)context).Range.Start;
			return null;
		}

		#endregion

		/// <summary>Gets whether the file in the specified editor is modified.</summary>
		/// <remarks>TextEditorControl doesn't maintain its own internal modified 
		/// flag, so we use the '*' shown after the file name to represent the 
		/// modified state.</remarks>
		public bool IsModified()
		{
			return Parent.Text.EndsWith("*");
		}
		public void SetModifiedFlag(bool flag)
		{
			if (IsModified() != flag)
			{
				var p = this.Parent;
				if (IsModified())
					p.Text = p.Text.Substring(0, p.Text.Length - 1);
				else
					p.Text += "*";
			}
		}
	
		public void LoadFile(string fn)
		{
 			Editor.LoadFile(fn); // auto-sets syntax highlighting mode
			// TextEditorControl clears highlighting if file extension was unknown
			if (fn.EndsWith(".ecs") || fn.EndsWith(".les"))
				Editor.SetHighlighting("C#"); // there, FTFY

			// Modified flag is set during loading because the document 
			// "changes" (from nothing to something). So, clear it again.
			SetModifiedFlag(false);
		}

		public bool SaveOutput()
		{
			if (string.IsNullOrEmpty(_outFileName)) {
				MessageBox.Show("Output does not yet have a file name.");
				return false;
			} else {
				if (_lempStarted)
					Thread.Sleep(250); // wait and hope it'll finish
				try {
					_outEditor.SaveFile(_outFileName);
				} catch (Exception ex) {
					MessageBox.Show(ex.Message, ex.GetType().Name);
					return false;
				}
				if (_lempStarted)
					MessageBox.Show("LeMP is still running. Old version of output was saved.");
				return true;
			}
		}

		private void messageList_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (messageList.SelectedItems.Count == 1) {
				var lvi = messageList.SelectedItems[0];
				string lineStr = lvi.SubItems[0].Text, colStr = lvi.SubItems[1].Text;
				if (lineStr == "") { 
					var m = Regex.Match(lvi.SubItems[3].Text, @"\(([0-9]+)[,)]"); // Try to parse LLLPG error
					if (m.Success)
						lineStr = m.Captures[0].Value;
				}
				int line, col;
				if (int.TryParse(lineStr, out line)) {
					int.TryParse(colStr, out col);
					try {
						var area = _inEditor.ActiveTextAreaControl;
						if (col > 0)
							area.Caret.Position = new TextLocation(col - 1, line - 1);
						else
							area.Caret.Line = line - 1;
						area.TextArea.Focus();
					} catch { }
				}
			}
		}
	}
}
