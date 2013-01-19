namespace MiniTestRunner.WinForms
{
	partial class TestingForm
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
			if (disposing && (components != null))
			{
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TestingForm));
			this.menuStrip = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.menuOpenAssembly = new System.Windows.Forms.ToolStripMenuItem();
			this.menuAddAssembly = new System.Windows.Forms.ToolStripMenuItem();
			this.menuClearTree = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.menuLoadProject = new System.Windows.Forms.ToolStripMenuItem();
			this.menuSaveProject = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.menuLoadResultSet = new System.Windows.Forms.ToolStripMenuItem();
			this.menuSaveResultSet = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.menuExit = new System.Windows.Forms.ToolStripMenuItem();
			this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.menuLoadLastProjectOnStartup = new System.Windows.Forms.ToolStripMenuItem();
			this.menuRunTestsOnLoad = new System.Windows.Forms.ToolStripMenuItem();
			this.menuRunTestsOnChange = new System.Windows.Forms.ToolStripMenuItem();
			this.menuAutoUnload = new System.Windows.Forms.ToolStripMenuItem();
			this.menuPartialTrust = new System.Windows.Forms.ToolStripMenuItem();
			this.menuAlwaysOnTop = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.menuResetAllPriorities = new System.Windows.Forms.ToolStripMenuItem();
			this.menuReEnableAllTests = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.menuMultithreading = new System.Windows.Forms.ToolStripMenuItem();
			this.menu1Thread = new System.Windows.Forms.ToolStripMenuItem();
			this.menu2Threads = new System.Windows.Forms.ToolStripMenuItem();
			this.menu3Threads = new System.Windows.Forms.ToolStripMenuItem();
			this.menu4Threads = new System.Windows.Forms.ToolStripMenuItem();
			this.menu6Threads = new System.Windows.Forms.ToolStripMenuItem();
			this.menu8Threads = new System.Windows.Forms.ToolStripMenuItem();
			this.menu12Threads = new System.Windows.Forms.ToolStripMenuItem();
			this.menu16Threads = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
			this.menuHideOutputPane = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStrip = new System.Windows.Forms.ToolStrip();
			this.btnOpenAssembly = new System.Windows.Forms.ToolStripButton();
			this.btnOpenProject = new System.Windows.Forms.ToolStripButton();
			this.btnSaveProject = new System.Windows.Forms.ToolStripButton();
			this.btnCopy = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.btnRunTests = new System.Windows.Forms.ToolStripButton();
			this.btnStopTests = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
			this.txtFilter = new System.Windows.Forms.ToolStripTextBox();
			this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.btnSplitHorizontally = new System.Windows.Forms.ToolStripButton();
			this.btnHideOutputPane = new System.Windows.Forms.ToolStripButton();
			this.btnOutputWordWrap = new System.Windows.Forms.ToolStripButton();
			this.statusStrip = new System.Windows.Forms.StatusStrip();
			this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
			this.lblSpacer = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
			this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
			this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.runThisTestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
			this.lowPriorityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.normalPriorityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.highPriorityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
			this.forgetResultsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._testTreeView = new Aga.Controls.Tree.TreeViewAdv();
			this._treeColumnName = new Aga.Controls.Tree.TreeColumn();
			this._treeColumnStatus = new Aga.Controls.Tree.TreeColumn();
			this._treeColumnRuntime = new Aga.Controls.Tree.TreeColumn();
			this._treeColumnInfo = new Aga.Controls.Tree.TreeColumn();
			this._treeColumnDate = new Aga.Controls.Tree.TreeColumn();
			this._nodeTypeIcon = new Aga.Controls.Tree.NodeControls.NodeIcon();
			this._nodeStatusIcon = new Aga.Controls.Tree.NodeControls.NodeIcon();
			this._nodePriorityIcon = new Aga.Controls.Tree.NodeControls.NodeIcon();
			this._nodeName = new Aga.Controls.Tree.NodeControls.NodeTextBox();
			this._nodeStatusText = new Aga.Controls.Tree.NodeControls.NodeTextBox();
			this._nodeRunTime = new Aga.Controls.Tree.NodeControls.NodeTextBox();
			this._nodeSummary = new Aga.Controls.Tree.NodeControls.NodeTextBox();
			this._lastRunDate = new Aga.Controls.Tree.NodeControls.NodeTextBox();
			this.splitContainer = new System.Windows.Forms.SplitContainer();
			this.txtOutput = new System.Windows.Forms.TextBox();
			this.menuCloseProject = new System.Windows.Forms.ToolStripMenuItem();
			this.menuStrip.SuspendLayout();
			this.toolStrip.SuspendLayout();
			this.statusStrip.SuspendLayout();
			this.contextMenu.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
			this.splitContainer.Panel1.SuspendLayout();
			this.splitContainer.Panel2.SuspendLayout();
			this.splitContainer.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip
			// 
			this.menuStrip.Dock = System.Windows.Forms.DockStyle.None;
			this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.optionsToolStripMenuItem});
			this.menuStrip.Location = new System.Drawing.Point(0, 0);
			this.menuStrip.Name = "menuStrip";
			this.menuStrip.Size = new System.Drawing.Size(198, 24);
			this.menuStrip.TabIndex = 1;
			this.menuStrip.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuOpenAssembly,
            this.menuAddAssembly,
            this.menuClearTree,
            this.toolStripSeparator2,
            this.menuLoadProject,
            this.menuSaveProject,
            this.menuCloseProject,
            this.toolStripSeparator1,
            this.menuLoadResultSet,
            this.menuSaveResultSet,
            this.toolStripSeparator3,
            this.menuExit});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// menuOpenAssembly
			// 
			this.menuOpenAssembly.Image = ((System.Drawing.Image)(resources.GetObject("menuOpenAssembly.Image")));
			this.menuOpenAssembly.Name = "menuOpenAssembly";
			this.menuOpenAssembly.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.menuOpenAssembly.Size = new System.Drawing.Size(233, 22);
			this.menuOpenAssembly.Text = "&Open test assembly...";
			this.menuOpenAssembly.Click += new System.EventHandler(this.menuOpenAssembly_Click);
			// 
			// menuAddAssembly
			// 
			this.menuAddAssembly.Name = "menuAddAssembly";
			this.menuAddAssembly.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Up)));
			this.menuAddAssembly.Size = new System.Drawing.Size(233, 22);
			this.menuAddAssembly.Text = "&Add test assembly...";
			this.menuAddAssembly.Click += new System.EventHandler(this.menuAddAssembly_Click);
			// 
			// menuClearTree
			// 
			this.menuClearTree.Name = "menuClearTree";
			this.menuClearTree.Size = new System.Drawing.Size(233, 22);
			this.menuClearTree.Text = "Clear test tree";
			this.menuClearTree.Click += new System.EventHandler(this.menuClearTree_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(230, 6);
			// 
			// menuLoadProject
			// 
			this.menuLoadProject.Image = global::MiniTestRunner.Properties.Resources.OpenProject;
			this.menuLoadProject.Name = "menuLoadProject";
			this.menuLoadProject.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
						| System.Windows.Forms.Keys.O)));
			this.menuLoadProject.Size = new System.Drawing.Size(233, 22);
			this.menuLoadProject.Text = "Open &project...";
			this.menuLoadProject.Click += new System.EventHandler(this.menuLoadProject_Click);
			// 
			// menuSaveProject
			// 
			this.menuSaveProject.Image = ((System.Drawing.Image)(resources.GetObject("menuSaveProject.Image")));
			this.menuSaveProject.Name = "menuSaveProject";
			this.menuSaveProject.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
						| System.Windows.Forms.Keys.S)));
			this.menuSaveProject.Size = new System.Drawing.Size(233, 22);
			this.menuSaveProject.Text = "&Save project as...";
			this.menuSaveProject.Click += new System.EventHandler(this.menuSaveProject_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(230, 6);
			// 
			// menuLoadResultSet
			// 
			this.menuLoadResultSet.Name = "menuLoadResultSet";
			this.menuLoadResultSet.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt)
						| System.Windows.Forms.Keys.O)));
			this.menuLoadResultSet.Size = new System.Drawing.Size(233, 22);
			this.menuLoadResultSet.Text = "Open result set...";
			this.menuLoadResultSet.Click += new System.EventHandler(this.menuLoadResultSet_Click);
			// 
			// menuSaveResultSet
			// 
			this.menuSaveResultSet.Name = "menuSaveResultSet";
			this.menuSaveResultSet.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt)
						| System.Windows.Forms.Keys.S)));
			this.menuSaveResultSet.Size = new System.Drawing.Size(233, 22);
			this.menuSaveResultSet.Text = "Save result set...";
			this.menuSaveResultSet.Click += new System.EventHandler(this.menuSaveResultSet_Click);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(230, 6);
			// 
			// menuExit
			// 
			this.menuExit.Name = "menuExit";
			this.menuExit.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.menuExit.Size = new System.Drawing.Size(233, 22);
			this.menuExit.Text = "E&xit";
			this.menuExit.Click += new System.EventHandler(this.menuExit_Click);
			// 
			// optionsToolStripMenuItem
			// 
			this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuLoadLastProjectOnStartup,
            this.menuRunTestsOnLoad,
            this.menuRunTestsOnChange,
            this.menuAutoUnload,
            this.menuPartialTrust,
            this.menuAlwaysOnTop,
            this.toolStripSeparator6,
            this.menuResetAllPriorities,
            this.menuReEnableAllTests,
            this.toolStripSeparator7,
            this.menuMultithreading,
            this.toolStripSeparator10,
            this.menuHideOutputPane});
			this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
			this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
			this.optionsToolStripMenuItem.Text = "&Options";
			// 
			// menuLoadLastProjectOnStartup
			// 
			this.menuLoadLastProjectOnStartup.Name = "menuLoadLastProjectOnStartup";
			this.menuLoadLastProjectOnStartup.Size = new System.Drawing.Size(338, 22);
			this.menuLoadLastProjectOnStartup.Text = "&Load last project on startup";
			this.menuLoadLastProjectOnStartup.Click += new System.EventHandler(this.menuLoadLastProjectOnStartup_Click);
			// 
			// menuRunTestsOnLoad
			// 
			this.menuRunTestsOnLoad.Name = "menuRunTestsOnLoad";
			this.menuRunTestsOnLoad.Size = new System.Drawing.Size(338, 22);
			this.menuRunTestsOnLoad.Text = "Auto-&run tests when loading an assembly/project";
			this.menuRunTestsOnLoad.Click += new System.EventHandler(this.menuRunTestsOnLoad_Click);
			// 
			// menuRunTestsOnChange
			// 
			this.menuRunTestsOnChange.Name = "menuRunTestsOnChange";
			this.menuRunTestsOnChange.Size = new System.Drawing.Size(338, 22);
			this.menuRunTestsOnChange.Text = "Auto-run tests when an assembly &changes on disk";
			this.menuRunTestsOnChange.Click += new System.EventHandler(this.menuRunTestsOnChange_Click);
			// 
			// menuAutoUnload
			// 
			this.menuAutoUnload.Name = "menuAutoUnload";
			this.menuAutoUnload.Size = new System.Drawing.Size(338, 22);
			this.menuAutoUnload.Text = "&Unload assemblies when not running tests";
			this.menuAutoUnload.Click += new System.EventHandler(this.menuAutoUnload_Click);
			// 
			// menuPartialTrust
			// 
			this.menuPartialTrust.Name = "menuPartialTrust";
			this.menuPartialTrust.Size = new System.Drawing.Size(338, 22);
			this.menuPartialTrust.Text = "Load tests with &partial trust by default";
			this.menuPartialTrust.Click += new System.EventHandler(this.menuPartialTrust_Click);
			// 
			// menuAlwaysOnTop
			// 
			this.menuAlwaysOnTop.Name = "menuAlwaysOnTop";
			this.menuAlwaysOnTop.Size = new System.Drawing.Size(338, 22);
			this.menuAlwaysOnTop.Text = "&Keep window always-on-top unless maximized";
			this.menuAlwaysOnTop.Click += new System.EventHandler(this.menuAlwaysOnTop_Click);
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(335, 6);
			// 
			// menuResetAllPriorities
			// 
			this.menuResetAllPriorities.Name = "menuResetAllPriorities";
			this.menuResetAllPriorities.Size = new System.Drawing.Size(338, 22);
			this.menuResetAllPriorities.Text = "Reset all priorities to &neutral";
			this.menuResetAllPriorities.Click += new System.EventHandler(this.menuResetAllPriorities_Click);
			// 
			// menuReEnableAllTests
			// 
			this.menuReEnableAllTests.Name = "menuReEnableAllTests";
			this.menuReEnableAllTests.Size = new System.Drawing.Size(338, 22);
			this.menuReEnableAllTests.Text = "Re-&enable all assemblies and all tests";
			this.menuReEnableAllTests.Click += new System.EventHandler(this.menuReEnableAllTests_Click);
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(335, 6);
			// 
			// menuMultithreading
			// 
			this.menuMultithreading.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menu1Thread,
            this.menu2Threads,
            this.menu3Threads,
            this.menu4Threads,
            this.menu6Threads,
            this.menu8Threads,
            this.menu12Threads,
            this.menu16Threads});
			this.menuMultithreading.Name = "menuMultithreading";
			this.menuMultithreading.Size = new System.Drawing.Size(338, 22);
			this.menuMultithreading.Text = "Run tests in parallel";
			this.menuMultithreading.Click += new System.EventHandler(this.menuMultithreading_Click);
			// 
			// menu1Thread
			// 
			this.menu1Thread.Name = "menu1Thread";
			this.menu1Thread.Size = new System.Drawing.Size(158, 22);
			this.menu1Thread.Text = "Single-threaded";
			this.menu1Thread.Click += new System.EventHandler(this.menuMultithreading_Click);
			// 
			// menu2Threads
			// 
			this.menu2Threads.Name = "menu2Threads";
			this.menu2Threads.Size = new System.Drawing.Size(158, 22);
			this.menu2Threads.Text = "2 threads";
			this.menu2Threads.Click += new System.EventHandler(this.menuMultithreading_Click);
			// 
			// menu3Threads
			// 
			this.menu3Threads.Name = "menu3Threads";
			this.menu3Threads.Size = new System.Drawing.Size(158, 22);
			this.menu3Threads.Text = "3 threads";
			this.menu3Threads.Click += new System.EventHandler(this.menuMultithreading_Click);
			// 
			// menu4Threads
			// 
			this.menu4Threads.Name = "menu4Threads";
			this.menu4Threads.Size = new System.Drawing.Size(158, 22);
			this.menu4Threads.Text = "4 threads";
			this.menu4Threads.Click += new System.EventHandler(this.menuMultithreading_Click);
			// 
			// menu6Threads
			// 
			this.menu6Threads.Name = "menu6Threads";
			this.menu6Threads.Size = new System.Drawing.Size(158, 22);
			this.menu6Threads.Text = "6 threads";
			this.menu6Threads.Click += new System.EventHandler(this.menuMultithreading_Click);
			// 
			// menu8Threads
			// 
			this.menu8Threads.Name = "menu8Threads";
			this.menu8Threads.Size = new System.Drawing.Size(158, 22);
			this.menu8Threads.Text = "8 threads";
			this.menu8Threads.Click += new System.EventHandler(this.menuMultithreading_Click);
			// 
			// menu12Threads
			// 
			this.menu12Threads.Name = "menu12Threads";
			this.menu12Threads.Size = new System.Drawing.Size(158, 22);
			this.menu12Threads.Text = "12 threads";
			this.menu12Threads.Click += new System.EventHandler(this.menuMultithreading_Click);
			// 
			// menu16Threads
			// 
			this.menu16Threads.Name = "menu16Threads";
			this.menu16Threads.Size = new System.Drawing.Size(158, 22);
			this.menu16Threads.Text = "16 threads";
			this.menu16Threads.Click += new System.EventHandler(this.menuMultithreading_Click);
			// 
			// toolStripSeparator10
			// 
			this.toolStripSeparator10.Name = "toolStripSeparator10";
			this.toolStripSeparator10.Size = new System.Drawing.Size(335, 6);
			// 
			// menuHideOutputPane
			// 
			this.menuHideOutputPane.Image = global::MiniTestRunner.Properties.Resources.TreeOnly;
			this.menuHideOutputPane.Name = "menuHideOutputPane";
			this.menuHideOutputPane.Size = new System.Drawing.Size(338, 22);
			this.menuHideOutputPane.Text = "&Hide output pane";
			this.menuHideOutputPane.Click += new System.EventHandler(this.menuHideOutputPane_Click);
			// 
			// toolStrip
			// 
			this.toolStrip.Dock = System.Windows.Forms.DockStyle.None;
			this.toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnOpenAssembly,
            this.btnOpenProject,
            this.btnSaveProject,
            this.btnCopy,
            this.toolStripSeparator5,
            this.btnRunTests,
            this.btnStopTests,
            this.toolStripSeparator4,
            this.toolStripLabel1,
            this.txtFilter,
            this.toolStripSeparator,
            this.btnSplitHorizontally,
            this.btnHideOutputPane,
            this.btnOutputWordWrap});
			this.toolStrip.Location = new System.Drawing.Point(118, 0);
			this.toolStrip.Name = "toolStrip";
			this.toolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
			this.toolStrip.Size = new System.Drawing.Size(356, 25);
			this.toolStrip.TabIndex = 0;
			this.toolStrip.Text = "toolStrip";
			// 
			// btnOpenAssembly
			// 
			this.btnOpenAssembly.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnOpenAssembly.Image = ((System.Drawing.Image)(resources.GetObject("btnOpenAssembly.Image")));
			this.btnOpenAssembly.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnOpenAssembly.Name = "btnOpenAssembly";
			this.btnOpenAssembly.Size = new System.Drawing.Size(23, 22);
			this.btnOpenAssembly.Text = "&Open";
			this.btnOpenAssembly.Click += new System.EventHandler(this.menuOpenAssembly_Click);
			// 
			// btnOpenProject
			// 
			this.btnOpenProject.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnOpenProject.Image = global::MiniTestRunner.Properties.Resources.OpenProject;
			this.btnOpenProject.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnOpenProject.Name = "btnOpenProject";
			this.btnOpenProject.Size = new System.Drawing.Size(23, 22);
			this.btnOpenProject.Text = "Open &project";
			this.btnOpenProject.Click += new System.EventHandler(this.menuLoadProject_Click);
			// 
			// btnSaveProject
			// 
			this.btnSaveProject.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnSaveProject.Image = ((System.Drawing.Image)(resources.GetObject("btnSaveProject.Image")));
			this.btnSaveProject.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnSaveProject.Name = "btnSaveProject";
			this.btnSaveProject.Size = new System.Drawing.Size(23, 22);
			this.btnSaveProject.Text = "&Save";
			this.btnSaveProject.Click += new System.EventHandler(this.menuSaveProject_Click);
			// 
			// btnCopy
			// 
			this.btnCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnCopy.Image = ((System.Drawing.Image)(resources.GetObject("btnCopy.Image")));
			this.btnCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnCopy.Name = "btnCopy";
			this.btnCopy.Size = new System.Drawing.Size(23, 22);
			this.btnCopy.Text = "&Copy";
			this.btnCopy.Click += new System.EventHandler(this.btnCopy_Click);
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(6, 25);
			// 
			// btnRunTests
			// 
			this.btnRunTests.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnRunTests.Image = ((System.Drawing.Image)(resources.GetObject("btnRunTests.Image")));
			this.btnRunTests.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnRunTests.Name = "btnRunTests";
			this.btnRunTests.Size = new System.Drawing.Size(23, 22);
			this.btnRunTests.Text = "Run tests";
			this.btnRunTests.Click += new System.EventHandler(this.btnRunTests_Click);
			// 
			// btnStopTests
			// 
			this.btnStopTests.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnStopTests.Image = global::MiniTestRunner.Properties.Resources.StopIcon;
			this.btnStopTests.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnStopTests.Name = "btnStopTests";
			this.btnStopTests.Size = new System.Drawing.Size(23, 22);
			this.btnStopTests.Text = "Stop";
			this.btnStopTests.Click += new System.EventHandler(this.btnStopTests_Click);
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(6, 25);
			// 
			// toolStripLabel1
			// 
			this.toolStripLabel1.Name = "toolStripLabel1";
			this.toolStripLabel1.Size = new System.Drawing.Size(36, 22);
			this.toolStripLabel1.Text = "Filter:";
			// 
			// txtFilter
			// 
			this.txtFilter.Name = "txtFilter";
			this.txtFilter.Size = new System.Drawing.Size(90, 25);
			this.txtFilter.TextChanged += new System.EventHandler(this.txtFilter_TextChanged);
			// 
			// toolStripSeparator
			// 
			this.toolStripSeparator.Name = "toolStripSeparator";
			this.toolStripSeparator.Size = new System.Drawing.Size(6, 25);
			// 
			// btnSplitHorizontally
			// 
			this.btnSplitHorizontally.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnSplitHorizontally.Image = global::MiniTestRunner.Properties.Resources.SplitHorizontal;
			this.btnSplitHorizontally.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnSplitHorizontally.Name = "btnSplitHorizontally";
			this.btnSplitHorizontally.Size = new System.Drawing.Size(23, 22);
			this.btnSplitHorizontally.Text = "Output pane on right";
			this.btnSplitHorizontally.Click += new System.EventHandler(this.btnSplitHorizontally_Click);
			// 
			// btnHideOutputPane
			// 
			this.btnHideOutputPane.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnHideOutputPane.Image = global::MiniTestRunner.Properties.Resources.TreeOnly;
			this.btnHideOutputPane.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnHideOutputPane.Name = "btnHideOutputPane";
			this.btnHideOutputPane.Size = new System.Drawing.Size(23, 22);
			this.btnHideOutputPane.Text = "Hide output pane";
			this.btnHideOutputPane.Click += new System.EventHandler(this.menuHideOutputPane_Click);
			// 
			// btnOutputWordWrap
			// 
			this.btnOutputWordWrap.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnOutputWordWrap.Image = global::MiniTestRunner.Properties.Resources.WordWrap;
			this.btnOutputWordWrap.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnOutputWordWrap.Name = "btnOutputWordWrap";
			this.btnOutputWordWrap.Size = new System.Drawing.Size(23, 22);
			this.btnOutputWordWrap.Text = "Word wrap output pane";
			this.btnOutputWordWrap.Click += new System.EventHandler(this.btnWordWrap_Click);
			// 
			// statusStrip
			// 
			this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus,
            this.lblSpacer,
            this.toolStripStatusLabel1,
            this.toolStripProgressBar1,
            this.toolStripStatusLabel2});
			this.statusStrip.Location = new System.Drawing.Point(0, 285);
			this.statusStrip.Name = "statusStrip";
			this.statusStrip.Size = new System.Drawing.Size(512, 22);
			this.statusStrip.TabIndex = 2;
			this.statusStrip.Text = "statusStrip1";
			// 
			// lblStatus
			// 
			this.lblStatus.Name = "lblStatus";
			this.lblStatus.Size = new System.Drawing.Size(26, 17);
			this.lblStatus.Text = "Idle";
			// 
			// lblSpacer
			// 
			this.lblSpacer.Name = "lblSpacer";
			this.lblSpacer.Size = new System.Drawing.Size(289, 17);
			this.lblSpacer.Spring = true;
			// 
			// toolStripStatusLabel1
			// 
			this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
			this.toolStripStatusLabel1.Size = new System.Drawing.Size(0, 17);
			// 
			// toolStripProgressBar1
			// 
			this.toolStripProgressBar1.Name = "toolStripProgressBar1";
			this.toolStripProgressBar1.Size = new System.Drawing.Size(100, 16);
			this.toolStripProgressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			this.toolStripProgressBar1.ToolTipText = "Test 0 of 0";
			// 
			// toolStripStatusLabel2
			// 
			this.toolStripStatusLabel2.Image = global::MiniTestRunner.Properties.Resources.StatusNotRun;
			this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
			this.toolStripStatusLabel2.Padding = new System.Windows.Forms.Padding(5, 0, 5, 0);
			this.toolStripStatusLabel2.Size = new System.Drawing.Size(80, 17);
			this.toolStripStatusLabel2.Text = "0 Error(s)";
			this.toolStripStatusLabel2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// contextMenu
			// 
			this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.runThisTestToolStripMenuItem,
            this.toolStripSeparator8,
            this.lowPriorityToolStripMenuItem,
            this.normalPriorityToolStripMenuItem,
            this.highPriorityToolStripMenuItem,
            this.toolStripSeparator9,
            this.forgetResultsToolStripMenuItem});
			this.contextMenu.Name = "contextMenu";
			this.contextMenu.Size = new System.Drawing.Size(256, 126);
			// 
			// runThisTestToolStripMenuItem
			// 
			this.runThisTestToolStripMenuItem.Name = "runThisTestToolStripMenuItem";
			this.runThisTestToolStripMenuItem.Size = new System.Drawing.Size(255, 22);
			this.runThisTestToolStripMenuItem.Text = "Run this test or group of tests now";
			// 
			// toolStripSeparator8
			// 
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			this.toolStripSeparator8.Size = new System.Drawing.Size(252, 6);
			// 
			// lowPriorityToolStripMenuItem
			// 
			this.lowPriorityToolStripMenuItem.Name = "lowPriorityToolStripMenuItem";
			this.lowPriorityToolStripMenuItem.Size = new System.Drawing.Size(255, 22);
			this.lowPriorityToolStripMenuItem.Text = "Low priority";
			// 
			// normalPriorityToolStripMenuItem
			// 
			this.normalPriorityToolStripMenuItem.Name = "normalPriorityToolStripMenuItem";
			this.normalPriorityToolStripMenuItem.Size = new System.Drawing.Size(255, 22);
			this.normalPriorityToolStripMenuItem.Text = "Normal priority";
			// 
			// highPriorityToolStripMenuItem
			// 
			this.highPriorityToolStripMenuItem.Name = "highPriorityToolStripMenuItem";
			this.highPriorityToolStripMenuItem.Size = new System.Drawing.Size(255, 22);
			this.highPriorityToolStripMenuItem.Text = "High priority";
			// 
			// toolStripSeparator9
			// 
			this.toolStripSeparator9.Name = "toolStripSeparator9";
			this.toolStripSeparator9.Size = new System.Drawing.Size(252, 6);
			// 
			// forgetResultsToolStripMenuItem
			// 
			this.forgetResultsToolStripMenuItem.Name = "forgetResultsToolStripMenuItem";
			this.forgetResultsToolStripMenuItem.Size = new System.Drawing.Size(255, 22);
			this.forgetResultsToolStripMenuItem.Text = "Remove assembly";
			// 
			// _testTreeView
			// 
			this._testTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._testTreeView.BackColor = System.Drawing.SystemColors.Window;
			this._testTreeView.Columns.Add(this._treeColumnName);
			this._testTreeView.Columns.Add(this._treeColumnStatus);
			this._testTreeView.Columns.Add(this._treeColumnRuntime);
			this._testTreeView.Columns.Add(this._treeColumnInfo);
			this._testTreeView.Columns.Add(this._treeColumnDate);
			this._testTreeView.DefaultToolTipProvider = null;
			this._testTreeView.DragDropMarkColor = System.Drawing.Color.Black;
			this._testTreeView.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._testTreeView.FullRowSelect = true;
			this._testTreeView.GridLineStyle = Aga.Controls.Tree.GridLineStyle.Vertical;
			this._testTreeView.LineColor = System.Drawing.SystemColors.ControlDark;
			this._testTreeView.Location = new System.Drawing.Point(0, 0);
			this._testTreeView.Model = null;
			this._testTreeView.Name = "_testTreeView";
			this._testTreeView.NodeControls.Add(this._nodeTypeIcon);
			this._testTreeView.NodeControls.Add(this._nodeStatusIcon);
			this._testTreeView.NodeControls.Add(this._nodePriorityIcon);
			this._testTreeView.NodeControls.Add(this._nodeName);
			this._testTreeView.NodeControls.Add(this._nodeStatusText);
			this._testTreeView.NodeControls.Add(this._nodeRunTime);
			this._testTreeView.NodeControls.Add(this._nodeSummary);
			this._testTreeView.NodeControls.Add(this._lastRunDate);
			this._testTreeView.SelectedNode = null;
			this._testTreeView.SelectionMode = Aga.Controls.Tree.TreeSelectionMode.Multi;
			this._testTreeView.Size = new System.Drawing.Size(512, 210);
			this._testTreeView.TabIndex = 0;
			this._testTreeView.UseColumns = true;
			// 
			// _treeColumnName
			// 
			this._treeColumnName.Header = "Name";
			this._treeColumnName.SortOrder = System.Windows.Forms.SortOrder.None;
			this._treeColumnName.TooltipText = "Assembly, class, test or test set";
			this._treeColumnName.Width = 220;
			// 
			// _treeColumnStatus
			// 
			this._treeColumnStatus.Header = "Errors";
			this._treeColumnStatus.SortOrder = System.Windows.Forms.SortOrder.None;
			this._treeColumnStatus.TooltipText = null;
			this._treeColumnStatus.Width = 44;
			// 
			// _treeColumnRuntime
			// 
			this._treeColumnRuntime.Header = "Run time";
			this._treeColumnRuntime.SortOrder = System.Windows.Forms.SortOrder.None;
			this._treeColumnRuntime.TooltipText = null;
			this._treeColumnRuntime.Width = 60;
			// 
			// _treeColumnInfo
			// 
			this._treeColumnInfo.Header = "Info";
			this._treeColumnInfo.SortOrder = System.Windows.Forms.SortOrder.None;
			this._treeColumnInfo.TooltipText = null;
			this._treeColumnInfo.Width = 260;
			// 
			// _treeColumnDate
			// 
			this._treeColumnDate.Header = "Last run at";
			this._treeColumnDate.SortOrder = System.Windows.Forms.SortOrder.None;
			this._treeColumnDate.TooltipText = null;
			this._treeColumnDate.Width = 130;
			// 
			// _nodeTypeIcon
			// 
			this._nodeTypeIcon.DataPropertyName = "TypeIcon";
			this._nodeTypeIcon.LeftMargin = 1;
			this._nodeTypeIcon.ParentColumn = this._treeColumnName;
			this._nodeTypeIcon.ScaleMode = Aga.Controls.Tree.ImageScaleMode.Clip;
			// 
			// _nodeStatusIcon
			// 
			this._nodeStatusIcon.DataPropertyName = "StatusIcon";
			this._nodeStatusIcon.LeftMargin = 1;
			this._nodeStatusIcon.ParentColumn = this._treeColumnName;
			this._nodeStatusIcon.ScaleMode = Aga.Controls.Tree.ImageScaleMode.Clip;
			// 
			// _nodePriorityIcon
			// 
			this._nodePriorityIcon.DataPropertyName = "PriorityIcon";
			this._nodePriorityIcon.LeftMargin = 1;
			this._nodePriorityIcon.ParentColumn = this._treeColumnName;
			this._nodePriorityIcon.ScaleMode = Aga.Controls.Tree.ImageScaleMode.Clip;
			// 
			// _nodeName
			// 
			this._nodeName.DataPropertyName = "Name";
			this._nodeName.IncrementalSearchEnabled = true;
			this._nodeName.LeftMargin = 3;
			this._nodeName.ParentColumn = this._treeColumnName;
			this._nodeName.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
			// 
			// _nodeStatusText
			// 
			this._nodeStatusText.DataPropertyName = "StatusText";
			this._nodeStatusText.IncrementalSearchEnabled = true;
			this._nodeStatusText.LeftMargin = 3;
			this._nodeStatusText.ParentColumn = this._treeColumnStatus;
			this._nodeStatusText.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
			// 
			// _nodeRunTime
			// 
			this._nodeRunTime.DataPropertyName = "RunTime";
			this._nodeRunTime.IncrementalSearchEnabled = true;
			this._nodeRunTime.LeftMargin = 3;
			this._nodeRunTime.ParentColumn = this._treeColumnRuntime;
			this._nodeRunTime.Trimming = System.Drawing.StringTrimming.Character;
			// 
			// _nodeSummary
			// 
			this._nodeSummary.DataPropertyName = "Summary";
			this._nodeSummary.IncrementalSearchEnabled = true;
			this._nodeSummary.LeftMargin = 3;
			this._nodeSummary.ParentColumn = this._treeColumnInfo;
			// 
			// _lastRunDate
			// 
			this._lastRunDate.DataPropertyName = "LastRunAt";
			this._lastRunDate.IncrementalSearchEnabled = true;
			this._lastRunDate.LeftMargin = 3;
			this._lastRunDate.ParentColumn = this._treeColumnDate;
			this._lastRunDate.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
			// 
			// splitContainer
			// 
			this.splitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.splitContainer.Location = new System.Drawing.Point(0, 24);
			this.splitContainer.Name = "splitContainer";
			this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer.Panel1
			// 
			this.splitContainer.Panel1.Controls.Add(this._testTreeView);
			// 
			// splitContainer.Panel2
			// 
			this.splitContainer.Panel2.Controls.Add(this.txtOutput);
			this.splitContainer.Panel2MinSize = 15;
			this.splitContainer.Size = new System.Drawing.Size(512, 261);
			this.splitContainer.SplitterDistance = 210;
			this.splitContainer.SplitterWidth = 5;
			this.splitContainer.TabIndex = 3;
			// 
			// txtOutput
			// 
			this.txtOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.txtOutput.Location = new System.Drawing.Point(0, 0);
			this.txtOutput.Multiline = true;
			this.txtOutput.Name = "txtOutput";
			this.txtOutput.ReadOnly = true;
			this.txtOutput.Size = new System.Drawing.Size(511, 43);
			this.txtOutput.TabIndex = 0;
			this.txtOutput.WordWrap = false;
			// 
			// menuCloseProject
			// 
			this.menuCloseProject.Name = "menuCloseProject";
			this.menuCloseProject.Size = new System.Drawing.Size(233, 22);
			this.menuCloseProject.Text = "&Close project";
			// 
			// TestingForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(512, 307);
			this.Controls.Add(this.splitContainer);
			this.Controls.Add(this.statusStrip);
			this.Controls.Add(this.toolStrip);
			this.Controls.Add(this.menuStrip);
			this.MainMenuStrip = this.menuStrip;
			this.Name = "TestingForm";
			this.Text = "MiniTest runner";
			this.Resize += new System.EventHandler(this.SetAlwaysOnTop);
			this.menuStrip.ResumeLayout(false);
			this.menuStrip.PerformLayout();
			this.toolStrip.ResumeLayout(false);
			this.toolStrip.PerformLayout();
			this.statusStrip.ResumeLayout(false);
			this.statusStrip.PerformLayout();
			this.contextMenu.ResumeLayout(false);
			this.splitContainer.Panel1.ResumeLayout(false);
			this.splitContainer.Panel2.ResumeLayout(false);
			this.splitContainer.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
			this.splitContainer.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private Aga.Controls.Tree.TreeViewAdv _testTreeView;
		private System.Windows.Forms.MenuStrip menuStrip;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem menuOpenAssembly;
		private System.Windows.Forms.ToolStripMenuItem menuAddAssembly;
		private System.Windows.Forms.ToolStripMenuItem menuClearTree;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem menuSaveProject;
		private System.Windows.Forms.ToolStripMenuItem menuLoadProject;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem menuSaveResultSet;
		private System.Windows.Forms.ToolStripMenuItem menuLoadResultSet;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem menuExit;
		private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem menuRunTestsOnLoad;
		private System.Windows.Forms.ToolStripMenuItem menuLoadLastProjectOnStartup;
		private System.Windows.Forms.ToolStripMenuItem menuResetAllPriorities;
		private System.Windows.Forms.ToolStrip toolStrip;
		private System.Windows.Forms.ToolStripButton btnOpenAssembly;
		private System.Windows.Forms.ToolStripButton btnSaveProject;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private System.Windows.Forms.ToolStripButton btnRunTests;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripLabel toolStripLabel1;
		private System.Windows.Forms.ToolStripTextBox txtFilter;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator;
		private System.Windows.Forms.ToolStripMenuItem menuRunTestsOnChange;
		private System.Windows.Forms.ToolStripMenuItem menuAutoUnload;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
		private System.Windows.Forms.ToolStripMenuItem menuReEnableAllTests;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
		private System.Windows.Forms.StatusStrip statusStrip;
		private System.Windows.Forms.ToolStripStatusLabel lblStatus;
		private System.Windows.Forms.ToolStripButton btnStopTests;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
		private System.Windows.Forms.ContextMenuStrip contextMenu;
		private System.Windows.Forms.ToolStripMenuItem runThisTestToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
		private System.Windows.Forms.ToolStripMenuItem lowPriorityToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem normalPriorityToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem highPriorityToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
		private System.Windows.Forms.ToolStripMenuItem forgetResultsToolStripMenuItem;
		private System.Windows.Forms.ToolStripStatusLabel lblSpacer;
		private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar1;
		private System.Windows.Forms.ToolStripButton btnOpenProject;
		private System.Windows.Forms.ToolStripButton btnCopy;
		private System.Windows.Forms.ToolStripMenuItem menuPartialTrust;
		private System.Windows.Forms.ToolStripMenuItem menuAlwaysOnTop;
		private Aga.Controls.Tree.TreeColumn _treeColumnName;
		private Aga.Controls.Tree.TreeColumn _treeColumnInfo;
		private Aga.Controls.Tree.TreeColumn _treeColumnRuntime;
		private Aga.Controls.Tree.TreeColumn _treeColumnDate;
		private Aga.Controls.Tree.NodeControls.NodeIcon _nodeTypeIcon;
		private Aga.Controls.Tree.NodeControls.NodeIcon _nodePriorityIcon;
		private Aga.Controls.Tree.NodeControls.NodeTextBox _nodeName;
		private Aga.Controls.Tree.NodeControls.NodeTextBox _nodeRunTime;
		private Aga.Controls.Tree.NodeControls.NodeIcon _nodeStatusIcon;
		private Aga.Controls.Tree.NodeControls.NodeTextBox _nodeStatusText;
		private Aga.Controls.Tree.NodeControls.NodeTextBox _lastRunDate;
		private System.Windows.Forms.ToolStripMenuItem menuMultithreading;
		private System.Windows.Forms.ToolStripMenuItem menu1Thread;
		private System.Windows.Forms.ToolStripMenuItem menu2Threads;
		private System.Windows.Forms.ToolStripMenuItem menu3Threads;
		private System.Windows.Forms.ToolStripMenuItem menu4Threads;
		private System.Windows.Forms.ToolStripMenuItem menu6Threads;
		private System.Windows.Forms.ToolStripMenuItem menu8Threads;
		private System.Windows.Forms.ToolStripMenuItem menu12Threads;
		private System.Windows.Forms.ToolStripMenuItem menu16Threads;
		private Aga.Controls.Tree.TreeColumn _treeColumnStatus;
		private System.Windows.Forms.SplitContainer splitContainer;
		private System.Windows.Forms.ToolStripButton btnSplitHorizontally;
		private System.Windows.Forms.ToolStripButton btnHideOutputPane;
		private System.Windows.Forms.ToolStripButton btnOutputWordWrap;
		private System.Windows.Forms.TextBox txtOutput;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
		private System.Windows.Forms.ToolStripMenuItem menuHideOutputPane;
		private Aga.Controls.Tree.NodeControls.NodeTextBox _nodeSummary;
		private System.Windows.Forms.ToolStripMenuItem menuCloseProject;
	}
}

