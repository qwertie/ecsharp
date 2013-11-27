namespace SingleFileGenerator
{
	partial class InstallForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InstallForm));
			this.listVisualStudios = new System.Windows.Forms.ListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.label1 = new System.Windows.Forms.Label();
			this.btnRegister = new System.Windows.Forms.Button();
			this.btnUnregister = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// listVisualStudios
			// 
			this.listVisualStudios.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.listVisualStudios.CheckBoxes = true;
			this.listVisualStudios.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
			this.listVisualStudios.Location = new System.Drawing.Point(12, 35);
			this.listVisualStudios.Name = "listVisualStudios";
			this.listVisualStudios.Size = new System.Drawing.Size(600, 155);
			this.listVisualStudios.TabIndex = 0;
			this.listVisualStudios.UseCompatibleStateImageBehavior = false;
			this.listVisualStudios.View = System.Windows.Forms.View.Details;
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Visual Studio Type";
			this.columnHeader1.Width = 181;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Registry location";
			this.columnHeader2.Width = 375;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(182, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Discovered versions of Visual Studio:";
			// 
			// btnRegister
			// 
			this.btnRegister.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnRegister.Location = new System.Drawing.Point(486, 196);
			this.btnRegister.Name = "btnRegister";
			this.btnRegister.Size = new System.Drawing.Size(126, 42);
			this.btnRegister.TabIndex = 4;
			this.btnRegister.Text = "Register (install)";
			this.btnRegister.UseVisualStyleBackColor = true;
			this.btnRegister.Click += new System.EventHandler(this.btnRegister_Click);
			// 
			// btnUnregister
			// 
			this.btnUnregister.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnUnregister.Location = new System.Drawing.Point(486, 244);
			this.btnUnregister.Name = "btnUnregister";
			this.btnUnregister.Size = new System.Drawing.Size(126, 32);
			this.btnUnregister.TabIndex = 4;
			this.btnUnregister.Text = "Unregister (uninstall)";
			this.btnUnregister.UseVisualStyleBackColor = true;
			this.btnUnregister.Click += new System.EventHandler(this.btnUnregister_Click);
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.label2.Location = new System.Drawing.Point(12, 196);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(457, 86);
			this.label2.TabIndex = 5;
			this.label2.Text = resources.GetString("label2.Text");
			// 
			// InstallForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(624, 288);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.btnUnregister);
			this.Controls.Add(this.btnRegister);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.listVisualStudios);
			this.Name = "InstallForm";
			this.Text = "Install Single-File Generator";
			this.Load += new System.EventHandler(this.InstallForm_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListView listVisualStudios;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button btnRegister;
		private System.Windows.Forms.Button btnUnregister;
		private System.Windows.Forms.Label label2;
	}
}