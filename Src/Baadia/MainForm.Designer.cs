namespace BoxDiagrams
{
	partial class MainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			BoxDiagrams.DiagramDrawStyle diagramDrawStyle1 = new BoxDiagrams.DiagramDrawStyle();
			BoxDiagrams.DiagramDocumentCore diagramDocumentCore1 = new BoxDiagrams.DiagramDocumentCore();
			BoxDiagrams.DiagramDrawStyle diagramDrawStyle2 = new BoxDiagrams.DiagramDrawStyle();
			BoxDiagrams.DiagramDrawStyle diagramDrawStyle3 = new BoxDiagrams.DiagramDrawStyle();
			BoxDiagrams.DiagramDocumentCore diagramDocumentCore2 = new BoxDiagrams.DiagramDocumentCore();
			BoxDiagrams.DiagramDrawStyle diagramDrawStyle4 = new BoxDiagrams.DiagramDrawStyle();
			this.label1 = new System.Windows.Forms.Label();
			this._cbLineStyle = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this._btnZoomIn = new System.Windows.Forms.Button();
			this._btnZoomOut = new System.Windows.Forms.Button();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.menuNew = new System.Windows.Forms.ToolStripMenuItem();
			this.menuOpen = new System.Windows.Forms.ToolStripMenuItem();
			this.menuSave = new System.Windows.Forms.ToolStripMenuItem();
			this.menuSaveAs = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.menuExit = new System.Windows.Forms.ToolStripMenuItem();
			this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.menuCut = new System.Windows.Forms.ToolStripMenuItem();
			this.menuCopy = new System.Windows.Forms.ToolStripMenuItem();
			this.menuPaste = new System.Windows.Forms.ToolStripMenuItem();
			this.menuDelete = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.menuSelectAll = new System.Windows.Forms.ToolStripMenuItem();
			this.customComboBox1 = new BoxDiagrams.CustomComboBox();
			this._diagramCtrl = new BoxDiagrams.DiagramControl();
			this._arrowheadCtrl = new BoxDiagrams.ArrowheadControl();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(278, 6);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(35, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "Lines:";
			// 
			// _cbLineStyle
			// 
			this._cbLineStyle.FormattingEnabled = true;
			this._cbLineStyle.Location = new System.Drawing.Point(319, 2);
			this._cbLineStyle.Name = "_cbLineStyle";
			this._cbLineStyle.Size = new System.Drawing.Size(95, 21);
			this._cbLineStyle.TabIndex = 4;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(89, 6);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(82, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "Boxes/Markers:";
			// 
			// _btnZoomIn
			// 
			this._btnZoomIn.Location = new System.Drawing.Point(488, 1);
			this._btnZoomIn.Name = "_btnZoomIn";
			this._btnZoomIn.Size = new System.Drawing.Size(38, 28);
			this._btnZoomIn.TabIndex = 6;
			this._btnZoomIn.Text = "+";
			this._btnZoomIn.UseVisualStyleBackColor = true;
			// 
			// _btnZoomOut
			// 
			this._btnZoomOut.Location = new System.Drawing.Point(527, 1);
			this._btnZoomOut.Name = "_btnZoomOut";
			this._btnZoomOut.Size = new System.Drawing.Size(38, 28);
			this._btnZoomOut.TabIndex = 7;
			this._btnZoomOut.Text = "-";
			this._btnZoomOut.UseVisualStyleBackColor = true;
			// 
			// menuStrip1
			// 
			this.menuStrip1.Dock = System.Windows.Forms.DockStyle.None;
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 1);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(84, 24);
			this.menuStrip1.TabIndex = 7;
			this.menuStrip1.Text = "menuStrip";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuNew,
            this.menuOpen,
            this.menuSave,
            this.menuSaveAs,
            this.toolStripSeparator1,
            this.menuExit});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// menuNew
			// 
			this.menuNew.Name = "menuNew";
			this.menuNew.Size = new System.Drawing.Size(155, 22);
			this.menuNew.Text = "&New";
			this.menuNew.Click += new System.EventHandler(this.menuNew_Click);
			// 
			// menuOpen
			// 
			this.menuOpen.Name = "menuOpen";
			this.menuOpen.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.menuOpen.Size = new System.Drawing.Size(155, 22);
			this.menuOpen.Text = "&Open...";
			this.menuOpen.Click += new System.EventHandler(this.menuOpen_Click);
			// 
			// menuSave
			// 
			this.menuSave.Name = "menuSave";
			this.menuSave.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.menuSave.Size = new System.Drawing.Size(155, 22);
			this.menuSave.Text = "&Save";
			this.menuSave.Click += new System.EventHandler(this.menuSave_Click);
			// 
			// menuSaveAs
			// 
			this.menuSaveAs.Name = "menuSaveAs";
			this.menuSaveAs.Size = new System.Drawing.Size(155, 22);
			this.menuSaveAs.Text = "Save &as...";
			this.menuSaveAs.Click += new System.EventHandler(this.menuSaveAs_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(152, 6);
			// 
			// menuExit
			// 
			this.menuExit.Name = "menuExit";
			this.menuExit.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.menuExit.Size = new System.Drawing.Size(155, 22);
			this.menuExit.Text = "E&xit";
			this.menuExit.Click += new System.EventHandler(this.menuExit_Click);
			// 
			// editToolStripMenuItem
			// 
			this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuCut,
            this.menuCopy,
            this.menuPaste,
            this.menuDelete,
            this.toolStripSeparator2,
            this.menuSelectAll});
			this.editToolStripMenuItem.Name = "editToolStripMenuItem";
			this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
			this.editToolStripMenuItem.Text = "&Edit";
			// 
			// menuCut
			// 
			this.menuCut.Name = "menuCut";
			this.menuCut.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
			this.menuCut.Size = new System.Drawing.Size(164, 22);
			this.menuCut.Text = "Cu&t";
			this.menuCut.Click += new System.EventHandler(this.menuCut_Click);
			// 
			// menuCopy
			// 
			this.menuCopy.Name = "menuCopy";
			this.menuCopy.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.menuCopy.Size = new System.Drawing.Size(164, 22);
			this.menuCopy.Text = "&Copy";
			this.menuCopy.Click += new System.EventHandler(this.menuCopy_Click);
			// 
			// menuPaste
			// 
			this.menuPaste.Name = "menuPaste";
			this.menuPaste.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
			this.menuPaste.Size = new System.Drawing.Size(164, 22);
			this.menuPaste.Text = "&Paste";
			this.menuPaste.Click += new System.EventHandler(this.menuPaste_Click);
			// 
			// menuDelete
			// 
			this.menuDelete.Name = "menuDelete";
			this.menuDelete.ShortcutKeys = System.Windows.Forms.Keys.Delete;
			this.menuDelete.Size = new System.Drawing.Size(164, 22);
			this.menuDelete.Text = "&Delete";
			this.menuDelete.Click += new System.EventHandler(this.menuDelete_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(161, 6);
			// 
			// menuSelectAll
			// 
			this.menuSelectAll.Name = "menuSelectAll";
			this.menuSelectAll.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.menuSelectAll.Size = new System.Drawing.Size(164, 22);
			this.menuSelectAll.Text = "Select &All";
			this.menuSelectAll.Click += new System.EventHandler(this.menuSelectAll_Click);
			// 
			// customComboBox1
			// 
			this.customComboBox1.FormattingEnabled = true;
			this.customComboBox1.Location = new System.Drawing.Point(177, 3);
			this.customComboBox1.Name = "customComboBox1";
			this.customComboBox1.Size = new System.Drawing.Size(95, 21);
			this.customComboBox1.TabIndex = 2;
			// 
			// _diagramCtrl
			// 
			this._diagramCtrl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._diagramCtrl.BackColor = System.Drawing.Color.White;
			diagramDrawStyle1.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
			diagramDrawStyle1.LineColor = System.Drawing.Color.Black;
			diagramDrawStyle1.LineStyle = System.Drawing.Drawing2D.DashStyle.Solid;
			diagramDrawStyle1.LineWidth = 2F;
			diagramDrawStyle1.TextColor = System.Drawing.Color.Blue;
			this._diagramCtrl.BoxStyle = diagramDrawStyle1;
			this._diagramCtrl.Document = diagramDocumentCore1;
			diagramDrawStyle2.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
			diagramDrawStyle2.LineColor = System.Drawing.Color.Black;
			diagramDrawStyle2.LineStyle = System.Drawing.Drawing2D.DashStyle.Solid;
			diagramDrawStyle2.LineWidth = 2F;
			diagramDrawStyle2.TextColor = System.Drawing.Color.Blue;
			this._diagramCtrl.LineStyle = diagramDrawStyle2;
			this._diagramCtrl.Location = new System.Drawing.Point(0, 29);
			this._diagramCtrl.MarkerRadius = 5F;
			this._diagramCtrl.Name = "_diagramCtrl";
			this._diagramCtrl.Size = new System.Drawing.Size(581, 286);
			this._diagramCtrl.TabIndex = 0;
			this._diagramCtrl.Text = "e";
			// 
			// _arrowheadCtrl
			// 
			this._arrowheadCtrl.BackColor = System.Drawing.Color.White;
			diagramDrawStyle3.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
			diagramDrawStyle3.LineColor = System.Drawing.Color.Black;
			diagramDrawStyle3.LineStyle = System.Drawing.Drawing2D.DashStyle.Solid;
			diagramDrawStyle3.LineWidth = 2F;
			diagramDrawStyle3.TextColor = System.Drawing.Color.Blue;
			this._arrowheadCtrl.BoxStyle = diagramDrawStyle3;
			this._arrowheadCtrl.Document = diagramDocumentCore2;
			diagramDrawStyle4.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
			diagramDrawStyle4.LineColor = System.Drawing.Color.Black;
			diagramDrawStyle4.LineStyle = System.Drawing.Drawing2D.DashStyle.Solid;
			diagramDrawStyle4.LineWidth = 2F;
			diagramDrawStyle4.TextColor = System.Drawing.Color.Blue;
			this._arrowheadCtrl.LineStyle = diagramDrawStyle4;
			this._arrowheadCtrl.Location = new System.Drawing.Point(420, 3);
			this._arrowheadCtrl.MarkerRadius = 5F;
			this._arrowheadCtrl.Name = "_arrowheadCtrl";
			this._arrowheadCtrl.Size = new System.Drawing.Size(62, 24);
			this._arrowheadCtrl.TabIndex = 5;
			this._arrowheadCtrl.Text = "arrowheadControl1";
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(581, 315);
			this.Controls.Add(this.customComboBox1);
			this.Controls.Add(this._diagramCtrl);
			this.Controls.Add(this._arrowheadCtrl);
			this.Controls.Add(this._btnZoomOut);
			this.Controls.Add(this._btnZoomIn);
			this.Controls.Add(this._cbLineStyle);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "MainForm";
			this.Text = "Baadia";
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox _cbLineStyle;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button _btnZoomIn;
		private System.Windows.Forms.Button _btnZoomOut;
		private ArrowheadControl _arrowheadCtrl;
		private DiagramControl _diagramCtrl;
		private CustomComboBox customComboBox1;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem menuNew;
		private System.Windows.Forms.ToolStripMenuItem menuOpen;
		private System.Windows.Forms.ToolStripMenuItem menuSave;
		private System.Windows.Forms.ToolStripMenuItem menuSaveAs;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem menuExit;
		private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem menuCut;
		private System.Windows.Forms.ToolStripMenuItem menuCopy;
		private System.Windows.Forms.ToolStripMenuItem menuPaste;
		private System.Windows.Forms.ToolStripMenuItem menuDelete;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem menuSelectAll;

	}
}

