namespace Benchmark
{
	partial class EzChartForm
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
			this._tabs = new System.Windows.Forms.TabControl();
			this.btnSaveAll = new System.Windows.Forms.Button();
			this.btnSaveCurrent = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// _tabs
			// 
			this._tabs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._tabs.Location = new System.Drawing.Point(4, 3);
			this._tabs.Name = "_tabs";
			this._tabs.SelectedIndex = 0;
			this._tabs.Size = new System.Drawing.Size(577, 429);
			this._tabs.TabIndex = 1;
			// 
			// btnSaveAll
			// 
			this.btnSaveAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnSaveAll.Location = new System.Drawing.Point(485, 434);
			this.btnSaveAll.Name = "btnSaveAll";
			this.btnSaveAll.Size = new System.Drawing.Size(96, 23);
			this.btnSaveAll.TabIndex = 2;
			this.btnSaveAll.Text = "Save all";
			this.btnSaveAll.UseVisualStyleBackColor = true;
			this.btnSaveAll.Click += new System.EventHandler(this.btnSaveAll_Click);
			// 
			// btnSaveCurrent
			// 
			this.btnSaveCurrent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnSaveCurrent.Location = new System.Drawing.Point(345, 434);
			this.btnSaveCurrent.Name = "btnSaveCurrent";
			this.btnSaveCurrent.Size = new System.Drawing.Size(134, 23);
			this.btnSaveCurrent.TabIndex = 2;
			this.btnSaveCurrent.Text = "Save current view...";
			this.btnSaveCurrent.UseVisualStyleBackColor = true;
			this.btnSaveCurrent.Click += new System.EventHandler(this.btnSaveCurrent_Click);
			// 
			// EzChartForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(584, 462);
			this.Controls.Add(this._tabs);
			this.Controls.Add(this.btnSaveCurrent);
			this.Controls.Add(this.btnSaveAll);
			this.Name = "EzChartForm";
			this.Text = "Benchmark Results";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl _tabs;
		private System.Windows.Forms.Button btnSaveAll;
		private System.Windows.Forms.Button btnSaveCurrent;
	}
}