namespace TextEditor
{
	partial class LempDemoPanel
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.splitContainer = new System.Windows.Forms.SplitContainer();
			this.innerSplitContainer = new System.Windows.Forms.SplitContainer();
			this.messageList = new System.Windows.Forms.ListView();
			this.Line = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Col = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Type = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Message = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.btnRegen = new System.Windows.Forms.Button();
			this.timer = new System.Windows.Forms.Timer(this.components);
			this.topPanel = new System.Windows.Forms.Panel();
			this.lblOptHeader = new System.Windows.Forms.Label();
			this.lblRunning = new System.Windows.Forms.Label();
			this._txtOptions = new System.Windows.Forms.TextBox();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
			this.splitContainer.Panel1.SuspendLayout();
			this.splitContainer.Panel2.SuspendLayout();
			this.splitContainer.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.innerSplitContainer)).BeginInit();
			this.innerSplitContainer.SuspendLayout();
			this.topPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// splitContainer
			// 
			this.splitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.splitContainer.Location = new System.Drawing.Point(0, 23);
			this.splitContainer.Name = "splitContainer";
			this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer.Panel1
			// 
			this.splitContainer.Panel1.Controls.Add(this.innerSplitContainer);
			// 
			// splitContainer.Panel2
			// 
			this.splitContainer.Panel2.Controls.Add(this.messageList);
			this.splitContainer.Size = new System.Drawing.Size(439, 304);
			this.splitContainer.SplitterDistance = 201;
			this.splitContainer.TabIndex = 0;
			// 
			// innerSplitContainer
			// 
			this.innerSplitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.innerSplitContainer.Location = new System.Drawing.Point(0, 0);
			this.innerSplitContainer.Name = "innerSplitContainer";
			this.innerSplitContainer.Size = new System.Drawing.Size(439, 201);
			this.innerSplitContainer.SplitterDistance = 211;
			this.innerSplitContainer.TabIndex = 0;
			// 
			// messageList
			// 
			this.messageList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Line,
            this.Col,
            this.Type,
            this.Message});
			this.messageList.Dock = System.Windows.Forms.DockStyle.Fill;
			this.messageList.FullRowSelect = true;
			this.messageList.Location = new System.Drawing.Point(0, 0);
			this.messageList.Name = "messageList";
			this.messageList.Size = new System.Drawing.Size(439, 99);
			this.messageList.TabIndex = 1;
			this.messageList.UseCompatibleStateImageBehavior = false;
			this.messageList.View = System.Windows.Forms.View.Details;
			this.messageList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.messageList_MouseDoubleClick);
			// 
			// Line
			// 
			this.Line.Text = "Line";
			this.Line.Width = 40;
			// 
			// Col
			// 
			this.Col.Text = "Col";
			this.Col.Width = 40;
			// 
			// Type
			// 
			this.Type.Text = "Type";
			// 
			// Message
			// 
			this.Message.Text = "Message";
			this.Message.Width = 800;
			// 
			// btnRegen
			// 
			this.btnRegen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnRegen.Location = new System.Drawing.Point(385, 0);
			this.btnRegen.Name = "btnRegen";
			this.btnRegen.Size = new System.Drawing.Size(54, 24);
			this.btnRegen.TabIndex = 2;
			this.btnRegen.Text = "Regen";
			this.btnRegen.UseVisualStyleBackColor = true;
			this.btnRegen.Click += new System.EventHandler(this.btnRegen_Click);
			// 
			// timer
			// 
			this.timer.Interval = 2000;
			this.timer.Tick += new System.EventHandler(this.timer_Tick);
			// 
			// topPanel
			// 
			this.topPanel.Controls.Add(this.lblOptHeader);
			this.topPanel.Controls.Add(this.lblRunning);
			this.topPanel.Controls.Add(this._txtOptions);
			this.topPanel.Controls.Add(this.btnRegen);
			this.topPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.topPanel.Location = new System.Drawing.Point(0, 0);
			this.topPanel.Name = "topPanel";
			this.topPanel.Size = new System.Drawing.Size(439, 23);
			this.topPanel.TabIndex = 1;
			// 
			// lblOptHeader
			// 
			this.lblOptHeader.AutoSize = true;
			this.lblOptHeader.Location = new System.Drawing.Point(1, 3);
			this.lblOptHeader.Name = "lblOptHeader";
			this.lblOptHeader.Size = new System.Drawing.Size(46, 13);
			this.lblOptHeader.TabIndex = 5;
			this.lblOptHeader.Text = "Options:";
			// 
			// lblRunning
			// 
			this.lblRunning.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.lblRunning.AutoSize = true;
			this.lblRunning.Location = new System.Drawing.Point(336, 5);
			this.lblRunning.Name = "lblRunning";
			this.lblRunning.Size = new System.Drawing.Size(44, 13);
			this.lblRunning.TabIndex = 4;
			this.lblRunning.Text = "WAIT...";
			this.lblRunning.Visible = false;
			// 
			// _txtOptions
			// 
			this._txtOptions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._txtOptions.Location = new System.Drawing.Point(53, 0);
			this._txtOptions.Name = "_txtOptions";
			this._txtOptions.Size = new System.Drawing.Size(282, 20);
			this._txtOptions.TabIndex = 3;
			this._txtOptions.Text = "--timeout=10 --outext=out.cs --inlang=ecs --forcelang";
			// 
			// LempDemoPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.topPanel);
			this.Controls.Add(this.splitContainer);
			this.Name = "LempDemoPanel";
			this.Size = new System.Drawing.Size(439, 327);
			this.splitContainer.Panel1.ResumeLayout(false);
			this.splitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
			this.splitContainer.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.innerSplitContainer)).EndInit();
			this.innerSplitContainer.ResumeLayout(false);
			this.topPanel.ResumeLayout(false);
			this.topPanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.SplitContainer splitContainer;
		private System.Windows.Forms.SplitContainer innerSplitContainer;
		private System.Windows.Forms.ListView messageList;
		private System.Windows.Forms.ColumnHeader Line;
		private System.Windows.Forms.ColumnHeader Type;
		private System.Windows.Forms.ColumnHeader Message;
		private System.Windows.Forms.Button btnRegen;
		private System.Windows.Forms.Timer timer;
		private System.Windows.Forms.Panel topPanel;
		private System.Windows.Forms.Label lblOptHeader;
		private System.Windows.Forms.Label lblRunning;
		private System.Windows.Forms.TextBox _txtOptions;
		private System.Windows.Forms.ColumnHeader Col;
	}
}
