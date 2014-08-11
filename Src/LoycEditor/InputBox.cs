using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

namespace System.Windows.Forms
{
	/// <summary>
	/// Used by InputBox.Show().
	/// </summary>
	internal class InputBoxDialog : Form 
	{
		private System.Windows.Forms.Label lblPrompt;
		public System.Windows.Forms.TextBox txtInput;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnCancel;
	
		public InputBoxDialog(string prompt, string title) : this(prompt, title, int.MinValue, int.MinValue) {} 

 		public InputBoxDialog(string prompt, string title, int xPos, int yPos)
		{
			if (xPos != int.MinValue && yPos != int.MinValue) {
				this.StartPosition = FormStartPosition.Manual;
				this.Location = new System.Drawing.Point(xPos, yPos);
			}

			InitializeComponent();

			lblPrompt.Text = prompt;
			this.Text = title;

			Graphics g = this.CreateGraphics();
			SizeF size = g.MeasureString(prompt, lblPrompt.Font, lblPrompt.Width);
			Debug.WriteLine("PROMPT SIZE: " + size);
			if (size.Height > lblPrompt.Height)
				this.Height += (int)size.Height - lblPrompt.Height;

			txtInput.SelectionStart = 0;
			txtInput.SelectionLength = txtInput.Text.Length;
			txtInput.Focus();
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.lblPrompt = new System.Windows.Forms.Label();
			this.txtInput = new System.Windows.Forms.TextBox();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// lblPrompt
			// 
			this.lblPrompt.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left)));
			this.lblPrompt.BackColor = System.Drawing.SystemColors.Control;
			this.lblPrompt.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.lblPrompt.Location = new System.Drawing.Point(12, 9);
			this.lblPrompt.Name = "lblPrompt";
			this.lblPrompt.Size = new System.Drawing.Size(302, 71);
			this.lblPrompt.TabIndex = 3;
			// 
			// txtInput
			// 
			this.txtInput.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.txtInput.Location = new System.Drawing.Point(8, 88);
			this.txtInput.Name = "txtInput";
			this.txtInput.Size = new System.Drawing.Size(381, 20);
			this.txtInput.TabIndex = 0;
			this.txtInput.Text = "";
			// 
			// btnOK
			// 
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Location = new System.Drawing.Point(326, 8);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(64, 24);
			this.btnOK.TabIndex = 1;
			this.btnOK.Text = "&OK";
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(326, 40);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(64, 24);
			this.btnCancel.TabIndex = 2;
			this.btnCancel.Text = "&Cancel";
			// 
			// InputBoxDialog
			// 
			this.AcceptButton = this.btnOK;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(398, 117);
			this.Controls.Add(this.txtInput);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.lblPrompt);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "InputBoxDialog";
			this.ResumeLayout(false);

		}
		#endregion
	}

	/// <summary>
	/// This static class contains methods named Show() to display a dialog box 
	/// with an input field, similar in appearance to the one in Visual Basic.
	/// The Show() method returns null if the user clicks Cancel, and non-null
	/// if the user clicks OK.
	/// </summary>
	public static class InputBox
	{
		static public string Show(string Prompt) 
			{ return Show(Prompt, null, null, int.MinValue, int.MinValue, false); }
		static public string Show(string Prompt, string Title, string Default)
			{ return Show(Prompt, Title, Default, int.MinValue, int.MinValue, false); }
		
		static public string Show(string Prompt, string Title, string Default, int xPos, int yPos, bool isPassword)
		{
			if (Title == null)
				Title = Application.ProductName;
			InputBoxDialog dlg = new InputBoxDialog(Prompt, Title, xPos, yPos);
			if (isPassword)
				dlg.txtInput.UseSystemPasswordChar = true;
			if (Default != null)
				dlg.txtInput.Text = Default;
			DialogResult result = dlg.ShowDialog();
			if (result == DialogResult.Cancel)
				return null;
			else
				return dlg.txtInput.Text;
		}
		static public string ShowPasswordBox(string Prompt, string Title)
			{ return Show(Prompt, Title, "", int.MinValue, int.MinValue, true); }
	}
}
