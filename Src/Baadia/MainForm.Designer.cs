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
			Util.WinForms.DrawStyle drawStyle1 = new Util.WinForms.DrawStyle();
			Util.WinForms.DrawStyle drawStyle2 = new Util.WinForms.DrawStyle();
			Util.WinForms.DrawStyle drawStyle3 = new Util.WinForms.DrawStyle();
			Util.WinForms.MarkerPolygon markerPolygon1 = new Util.WinForms.MarkerPolygon();
			Util.WinForms.DrawStyle drawStyle4 = new Util.WinForms.DrawStyle();
			Util.WinForms.DrawStyle drawStyle5 = new Util.WinForms.DrawStyle();
			Util.WinForms.DrawStyle drawStyle6 = new Util.WinForms.DrawStyle();
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
			this._arrowheadCtrl.BackColor = System.Drawing.Color.White;
			drawStyle1.FillColor = System.Drawing.Color.Gray;
			drawStyle1.LineColor = System.Drawing.Color.Black;
			drawStyle1.LineStyle = System.Drawing.Drawing2D.DashStyle.Solid;
			drawStyle1.LineWidth = 2F;
			drawStyle1.TextColor = System.Drawing.Color.Blue;
			this._arrowheadCtrl.BoxStyle = drawStyle1;
			drawStyle2.FillColor = System.Drawing.Color.Gray;
			drawStyle2.LineColor = System.Drawing.Color.Black;
			drawStyle2.LineStyle = System.Drawing.Drawing2D.DashStyle.Solid;
			drawStyle2.LineWidth = 2F;
			drawStyle2.TextColor = System.Drawing.Color.Blue;
			this._arrowheadCtrl.LineStyle = drawStyle2;
			this._arrowheadCtrl.Location = new System.Drawing.Point(320, 3);
			this._arrowheadCtrl.MarkerRadius = 5F;
			drawStyle3.FillColor = System.Drawing.Color.Red;
			drawStyle3.LineColor = System.Drawing.Color.Black;
			drawStyle3.LineStyle = System.Drawing.Drawing2D.DashStyle.Solid;
			drawStyle3.LineWidth = 1F;
			drawStyle3.TextColor = System.Drawing.Color.DarkRed;
			this._arrowheadCtrl.MarkerStyle = drawStyle3;
			this._arrowheadCtrl.MarkerType = markerPolygon1;
			this._arrowheadCtrl.Name = "_arrowheadCtrl";
			this._arrowheadCtrl.Size = new System.Drawing.Size(54, 24);
			this._arrowheadCtrl.TabIndex = 4;
			this._arrowheadCtrl.Text = "arrowheadControl1";
			// 
			// _diagramCtrl
			// 
			this._diagramCtrl.BackColor = System.Drawing.Color.White;
			drawStyle4.FillColor = System.Drawing.Color.Gray;
			drawStyle4.LineColor = System.Drawing.Color.Black;
			drawStyle4.LineStyle = System.Drawing.Drawing2D.DashStyle.Solid;
			drawStyle4.LineWidth = 2F;
			drawStyle4.TextColor = System.Drawing.Color.Blue;
			this._diagramCtrl.BoxStyle = drawStyle4;
			this._diagramCtrl.Dock = System.Windows.Forms.DockStyle.Bottom;
			drawStyle5.FillColor = System.Drawing.Color.Gray;
			drawStyle5.LineColor = System.Drawing.Color.Black;
			drawStyle5.LineStyle = System.Drawing.Drawing2D.DashStyle.Solid;
			drawStyle5.LineWidth = 2F;
			drawStyle5.TextColor = System.Drawing.Color.Blue;
			this._diagramCtrl.LineStyle = drawStyle5;
			this._diagramCtrl.Location = new System.Drawing.Point(0, 32);
			this._diagramCtrl.MarkerRadius = 5F;
			drawStyle6.FillColor = System.Drawing.Color.Red;
			drawStyle6.LineColor = System.Drawing.Color.Black;
			drawStyle6.LineStyle = System.Drawing.Drawing2D.DashStyle.Solid;
			drawStyle6.LineWidth = 1F;
			drawStyle6.TextColor = System.Drawing.Color.DarkRed;
			this._diagramCtrl.MarkerStyle = drawStyle6;
			this._diagramCtrl.MarkerType = markerPolygon1;
			this._diagramCtrl.Name = "_diagramCtrl";
			this._diagramCtrl.Size = new System.Drawing.Size(466, 283);
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
			this.Text = "Baadia";
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

