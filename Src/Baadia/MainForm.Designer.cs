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
			this.label1 = new System.Windows.Forms.Label();
			this.comboBox1 = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.comboBox2 = new System.Windows.Forms.ComboBox();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this._arrowheadCtrl = new BoxDiagrams.ArrowheadControl();
			this._diagramCtrl = new BoxDiagrams.DiagramControl();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(168, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(35, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Lines:";
			// 
			// comboBox1
			// 
			this.comboBox1.FormattingEnabled = true;
			this.comboBox1.Location = new System.Drawing.Point(209, 6);
			this.comboBox1.Name = "comboBox1";
			this.comboBox1.Size = new System.Drawing.Size(105, 21);
			this.comboBox1.TabIndex = 1;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 9);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(39, 13);
			this.label2.TabIndex = 0;
			this.label2.Text = "Boxes:";
			// 
			// comboBox2
			// 
			this.comboBox2.FormattingEnabled = true;
			this.comboBox2.Location = new System.Drawing.Point(57, 6);
			this.comboBox2.Name = "comboBox2";
			this.comboBox2.Size = new System.Drawing.Size(105, 21);
			this.comboBox2.TabIndex = 1;
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(380, 1);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(38, 28);
			this.button1.TabIndex = 2;
			this.button1.Text = "+";
			this.button1.UseVisualStyleBackColor = true;
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(419, 1);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(38, 28);
			this.button2.TabIndex = 2;
			this.button2.Text = "-";
			this.button2.UseVisualStyleBackColor = true;
			// 
			// _arrowheadCtrl
			// 
			this._arrowheadCtrl.BackgroundColor = System.Drawing.Color.White;
			this._arrowheadCtrl.Location = new System.Drawing.Point(320, 3);
			this._arrowheadCtrl.Name = "_arrowheadCtrl";
			this._arrowheadCtrl.Size = new System.Drawing.Size(54, 24);
			this._arrowheadCtrl.TabIndex = 4;
			this._arrowheadCtrl.Text = "arrowheadControl1";
			// 
			// _diagramCtrl
			// 
			this._diagramCtrl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._diagramCtrl.BackgroundColor = System.Drawing.Color.White;
			this._diagramCtrl.Location = new System.Drawing.Point(0, 33);
			this._diagramCtrl.Name = "_diagramCtrl";
			this._diagramCtrl.Size = new System.Drawing.Size(465, 283);
			this._diagramCtrl.TabIndex = 5;
			this._diagramCtrl.Text = "diagramControl1";
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(466, 315);
			this.Controls.Add(this._diagramCtrl);
			this.Controls.Add(this._arrowheadCtrl);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.comboBox2);
			this.Controls.Add(this.comboBox1);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Name = "MainForm";
			this.Text = "Form1";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox comboBox1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ComboBox comboBox2;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
		private ArrowheadControl _arrowheadCtrl;
		private DiagramControl _diagramCtrl;

	}
}

