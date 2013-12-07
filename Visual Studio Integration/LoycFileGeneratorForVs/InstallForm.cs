using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.Shell;
using Loyc.Utilities;
using Loyc;
using System.Runtime.InteropServices;

namespace SingleFileGenerator
{
	/// <summary>
	/// Installer window designed to for Single-File Generators. This code works 
	/// for any SFG defined in the assembly given to the constructor; it does not 
	/// depend on LLLPG specifically.
	/// </summary>
	/// <remarks>
	/// Note: correct registration requires that this installer be run from an
	/// x86 executable. Although a program written in .NET for "AnyCPU" works in 
	/// both 32-bit and 64-bit, we use the <see cref="System.Runtime.InteropServices.RegistrationServices"/>
	/// class to register the assembly with COM. Unfortunately, if the installer 
	/// is built as AnyCPU, RegistrationServices will only register the module as 
	/// 64-bit, not 32-bit. Since Visual Studio is 32-bit, it won't be able to
	/// find our SFG. To fix this problem, build the installer as "x86" (32-bit).
	/// <para/>
	/// An alternate solution would be to invoke the 32-bit version of the 
	/// command-line tool <c>regasm.exe /codebase</c>. This may be installed at
	/// C:\Windows\Microsoft.NET\Framework\v4.0.30319 but this could vary by 
	/// Windows version (and certainly varies by .NET version).
	/// </remarks>
	public partial class InstallForm : Form
	{
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new InstallForm(Assembly.GetExecutingAssembly()));
		}

		Assembly _sfgAssembly;
		public InstallForm(Assembly sfgAssembly)
		{
			_sfgAssembly = sfgAssembly;
			InitializeComponent();
			this.Text = string.Format("Install Single-file generator: {0}", 
				Path.GetFileNameWithoutExtension(sfgAssembly.Location));
		}

		private void InstallForm_Load(object sender, EventArgs e)
		{
			foreach (VSType vs in VSType.VSEditions.Where(t => t.VersionNumber >= 8.0)) {
				string path = null;
				RegistryKey key = null;
				// In a 32-bit process, Windows adds "Wow6432Node" automatically
				if (IntPtr.Size == 8)
					key = Registry.LocalMachine.OpenSubKey(path = Path.Combine(vs.RegistryPath64, "Generators"), false);
				key = key ?? Registry.LocalMachine.OpenSubKey(path = Path.Combine(vs.RegistryPath, "Generators"), false);
				if (key != null)
					using (key) {
						var lvi = new ListViewItem(vs.Name);
						lvi.SubItems.Add("{HKLM}\\" + path);
						lvi.Checked = true;
						listVisualStudios.Items.Add(lvi);
					}
			}
		}

		void MessageBoxWriter(Symbol type, object context, string msg, object[] args)
		{
			MessageBox.Show(string.Format(msg, args), MessageSink.LocationString(context),
				MessageBoxButtons.OK, type == MessageSink.Error ? MessageBoxIcon.Error : MessageBoxIcon.None);
		}

		private void btnRegister_Click(object sender, EventArgs e)
		{
			int count = RegisterOrUnregister(new MessageSinkFromDelegate(MessageBoxWriter), false);
			MessageBox.Show(string.Format("Registered with {0} Visual Studio edition(s).", count));
			Close();
		}
		
		private void btnUnregister_Click(object sender, EventArgs e)
		{
			int count = RegisterOrUnregister(new MessageSinkFromDelegate(MessageBoxWriter), true);
			MessageBox.Show(string.Format("Unregistered from {0} Visual Studio edition(s).", count));
			Close();
		}

		private int RegisterOrUnregister(IMessageSink sink, bool unregister)
		{
			int count = 0;
			foreach (ListViewItem lvi in listVisualStudios.Items) {
				if (lvi.Checked) {
					string hive = lvi.SubItems[1].Text.Replace("{HKLM}\\", "");
					if (RegisterSingleFileGenerators(sink, _sfgAssembly, hive, unregister))
						count++;
				}
			}

			var registrar = new RegistrationServices();
			try {
				if (!unregister) { 
					if (!registrar.RegisterAssembly(_sfgAssembly, AssemblyRegistrationFlags.SetCodeBase))
						sink.Write(MessageSink.Error, "COM registration", "Failed (No eligible types?!)");
				} else {
					if (!registrar.UnregisterAssembly(_sfgAssembly))
						sink.Write(MessageSink.Error, "COM unregistration", "Failed (No eligible types?!)");
				}
			} catch (Exception e) {
				sink.Write(MessageSink.Error, unregister ? "COM unregistration" : "COM registration", e.Message);
			}

			return count;
		}

		public static bool RegisterSingleFileGenerators(IMessageSink sink, Assembly assembly, string generatorsKey, bool unregister = false)
		{
			bool ok = false;
			foreach (Type type in assembly.GetTypes())
			{
				var a = type.GetCustomAttributes(typeof(CodeGeneratorRegistrationAttribute), true);
				foreach (CodeGeneratorRegistrationAttribute attr in a) {
					string subKey = string.Format(@"{0}\{1}", attr.ContextGuid, attr.GeneratorType.Name);
					string path = Path.Combine(generatorsKey, subKey);
					if (unregister) {
						try {
							Registry.LocalMachine.DeleteSubKey(path, true);
							ok = true;
						} catch {}
					} else {
						try { 
							using (RegistryKey key = Registry.LocalMachine.CreateSubKey(path)) {
								if (key == null)
									sink.Write(MessageSink.Error, path, "Failed to create registry key");
								else {
									key.SetValue("", attr.GeneratorName);
									key.SetValue("CLSID", "{" + attr.GeneratorGuid.ToString() + "}");
									key.SetValue("GeneratesDesignTimeSource", 1);
									ok = true;
								}
							}
						} catch (Exception ex) {
							sink.Write(MessageSink.Error, path, "{0}: {1}", ex.GetType().Name, ex.Message);
							return false;
						}
					}
				}
			}
			return ok;
		}
	}

	/// <summary>
	/// List of Visual Studio versions and their registry paths in HKEY_LOCAL_MACHINE.
	/// http://stackoverflow.com/questions/10922913/visual-studio-express-2012-editions-exe-names-and-registry-path
	/// </summary>
	public class VSType
	{
		public readonly string Name;
		public readonly double VersionNumber;
		public readonly string RegistryPath;
		public readonly string SpecificLanguage;
		public readonly bool IsPro;
		public VSType(string name, double versionNumber, string registryPath, bool pro = false, string language = null)
		{
			Name = name;
			VersionNumber = versionNumber;
			RegistryPath = registryPath;
			SpecificLanguage = language;
			IsPro = pro;
		}
		public string RegistryPath64 
		{
			get { 
				Debug.Assert(RegistryPath.StartsWith("SOFTWARE\\"));
				return RegistryPath.Insert("SOFTWARE\\".Length, "Wow6432Node\\");
			}
		}

		public static readonly VSType VS2002 = new VSType("VS 2002", 7.0,  "SOFTWARE\\Microsoft\\VisualStudio\\7.0", true);
		public static readonly VSType VS2003 = new VSType("VS 2003", 7.1, "SOFTWARE\\Microsoft\\VisualStudio\\7.1", true);

		// 2005  ***********************************************************************
		public static readonly VSType VS2005 = new VSType("VS 2005", 8.0,  "SOFTWARE\\Microsoft\\VisualStudio\\8.0", true);
		public static readonly VSType VSExpress2005CSharp = new VSType("Visual C# 2005 Express", 8.0, "SOFTWARE\\Microsoft\\VCSExpress\\8.0", false, "C#");
		public static readonly VSType VSExpress2005VB  = new VSType("Visual Basic 2005 Express", 8.0, "SOFTWARE\\Microsoft\\VBExpress\\8.0", false, "VB");
		public static readonly VSType VSExpress2005Web = new VSType("Visual Web Developer 2005 Express", 8.0, "SOFTWARE\\Microsoft\\VWDExpress\\8.0", false, "Web");

		// 2008  ***********************************************************************
		public static readonly VSType VS2008 = new VSType("VS 2008", 9.0,  "SOFTWARE\\Microsoft\\VisualStudio\\9.0", true);
		public static readonly VSType VSExpress2008CSharp = new VSType("Visual C# 2008 Express", 9.0, "SOFTWARE\\Microsoft\\VCSExpress\\9.0", false, "C#");
		public static readonly VSType VSExpress2008VB  = new VSType("Visual Basic 2008 Express", 9.0, "SOFTWARE\\Microsoft\\VBExpress\\9.0", false, "VB");
		public static readonly VSType VSExpress2008Web = new VSType("Visual Web Developer 2008 Express", 9.0, "SOFTWARE\\Microsoft\\VWDExpress\\9.0", false, "Web");

		// 2010  ***********************************************************************
		public static readonly VSType VS2010 = new VSType("VS 2010", 10.0, "SOFTWARE\\Microsoft\\VisualStudio\\10.0", true);
		public static readonly VSType VSExpress2010CSharp = new VSType("Visual C# 2010 Express", 10.0, "SOFTWARE\\Microsoft\\VCSExpress\\10.0", false, "C#");
		public static readonly VSType VSExpress2010VB  = new VSType("Visual Basic 2010 Express", 10.0, "SOFTWARE\\Microsoft\\VBExpress\\10.0", false, "VB");
		public static readonly VSType VSExpress2010Web = new VSType("Visual Web Developer 2010 Express", 10.0, "SOFTWARE\\Microsoft\\VWDExpress\\10.0", false, "Web");

		// 2012 ***********************************************************************
		public static readonly VSType VS2012 = new VSType("VS 2012", 11.0, "SOFTWARE\\Microsoft\\VisualStudio\\11.0", true);
		// This is unverified:
		public static readonly VSType VSExpress2012Desktop = new VSType("VS Express 2012 for Desktop", 11.0, "SOFTWARE\\Microsoft\\WDExpress\\11.0");
		public static VSType VSExpress2012WIn8 = new VSType("VS Express 2012 for Windows 8", 11.0, "SOFTWARE\\Microsoft\\VSWinExpress\\11.0");
		public static VSType VSExpress2012Web = new VSType("VS Express 2012 for Web", 11.0, "SOFTWARE\\Microsoft\\VWDExpress\\11.0");
		public static VSType VSExpressTFS2012 = new VSType("VS TFS Express 2012", 11.0, "SOFTWARE\\Microsoft\\TeamFoundationServer\\11.0", false, "TFS");

		// 2013 ***********************************************************************
		public static readonly VSType VS2013 = new VSType("VS 2013", 12.0, "SOFTWARE\\Microsoft\\VisualStudio\\12.0", true);
		public static readonly VSType VSExpress2013Desktop = new VSType("VS Express 2013 for Desktop", 12.0, "SOFTWARE\\Microsoft\\WDExpress\\12.0");

		public static readonly List<VSType> VSEditions = new List<VSType> {
			VS2002, VS2003,
			VS2005, VSExpress2005CSharp, VSExpress2005VB, VSExpress2005Web,
			VS2008, VSExpress2008CSharp, VSExpress2008VB, VSExpress2008Web,
			VS2010, VSExpress2010CSharp, VSExpress2010VB, VSExpress2010Web,
			VS2012, VSExpress2012Desktop,
			VS2013, VSExpress2013Desktop,
		};
	}

}
