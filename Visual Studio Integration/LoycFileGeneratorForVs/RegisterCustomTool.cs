using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace LoycFileGeneratorForVs
{
	/// <summary>
	/// This was my second attempt to run code on VS startup in order to add registry
	/// settings so that my Single-File Generator would work. However, while this 
	/// approach worked in VS2010, it only worked intermittently in VS2012 and didn't
	/// work in the two VS Express editions that I tried (2010, 2013).
	/// </summary>
	/// <remarks>
	/// Theoretically, single-file generators do not require a "Visual Studio package" 
	/// and they do not need a derived class of Package. However, they do require 
	/// certain registry settings to exist, and it isn't clear how Microsoft intended
	/// those registry settings to get created. The CodeGeneratorRegistrationAttribute
	/// doesn't seem to work by itself, so I wrote this class to do the job.
	/// <para/>
	/// <para/>
	/// After more than two weeks of waiting for answers on StackOverflow and MSDN:
	/// http://stackoverflow.com/questions/19718043/vs-single-file-generators-how-to-add-registry-information-run-code-on-vs-start/19992861#19992861
	/// http://social.msdn.microsoft.com/Forums/vstudio/en-US/d4e53cad-2fa3-45fc-8f6b-4d0bee3fb67c/vs-singlefile-generators-how-to-add-registry-information?forum=vsx#3308819b-ac2b-41f1-b61c-58f9f6000c64
	/// <para/>
	/// John Gardner suggested using [ProvideAutoLoad] on a Package class to make code
	/// run on VS startup. This allows me to create the necessary registry entries for
	/// the single-file generator when VS starts. However, if you just define a derived
	/// class of Package, Visual Studio will ignore it! You also need a *.vsct file, a
	/// *.regx file, and a bunch of special voodoo magic inside your *.csproj file, 
	/// or it just won't work. When you have the Visual Studio SDK installed, one of 
	/// the project templates is a Visual Studio package, which can create a working 
	/// example; also, the following blog post explains some of the magic incantations 
	/// required:
	/// http://codegoeshere.blogspot.ca/2009/11/vsix-with-vs2010-beta-2-sdk.html
	/// <para/>
	/// Boo Microsoft for making everything so ridiculously complicated, and relying 
	/// on multiple esoteric tools, obscure Attributes, XML files, resource files, etc.
	/// instead of just letting developers write a class with one or two simple 
	/// attributes and be done with it. If Microsoft hadn't confusingly overengineered
	/// everything, this entire project would only need to have a single file in it 
	/// (LllpgCustomTool.cs). Then you'd just rename the Extension.dll to, let's say, 
	/// Extension.vsix, and Visual Studio would have a file association for vsix
	/// that causes it to install the extension when a user double-clicks it. No 
	/// reasonable system needs all kinds of xml files and resx files and special 
	/// undocumented magic in the project file or hacks (like this class) to install 
	/// registry settings. And don't tell me "we need all this crap to avoid loading 
	/// all extensions on startup". No you don't. Load the extension once, find out
	/// what's inside it, and cache that information so subsequent runs don't have to
	/// load it on startup. Don't make a burden for extension authors.
	/// </remarks>
	//[DefaultRegistryRoot(@"Software\Microsoft\VisualStudio\10.0")]
	// This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
	// a package.
	[PackageRegistration(UseManagedResourcesOnly = true)]
	// This attribute is used to register the informations needed to show the this package
	// in the Help/About dialog of Visual Studio.
	[InstalledProductRegistration("#ProductName", "#ProductDetails", "1.0", IconResourceID = 400)]
	[ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids80.NoSoluti‌on​)]
	[Guid("DAAD24DB-6B31-49C6-86AE-6E20F156B82E")]
	class RegisterCustomTool : Package
	{
		internal RegistryKey _root;

		public RegisterCustomTool()
		{
			try {
				// returns null outside Visual Studio
				_root = VSRegistry.RegistryRoot(__VsLocalRegistryType.RegType_Configuration, true);
				if (_root != null)
					_root = _root.OpenSubKey("Generators", true);
			} catch { }
		}

		protected override void Initialize()
		{
			foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
			{
				var a = type.GetCustomAttributes(typeof(CodeGeneratorRegistrationAttribute), true);
				foreach (CodeGeneratorRegistrationAttribute attr in a) {
					string subKey = string.Format(@"{0}\{1}", attr.ContextGuid, attr.GeneratorType.Name);

					using (RegistryKey key = _root.CreateSubKey(subKey)) {
						key.SetValue("", attr.GeneratorName);
						key.SetValue("CLSID", "{" + attr.GeneratorGuid.ToString() + "}");
						key.SetValue("GeneratesDesignTimeSource", 1);
					}
				}
			}
			base.Initialize();
		}
		
		// Normally unused. It doesn't hurt to offer a manual registration option though.
		// (this is not fully tested)
		public static void Main(string[] args)
		{
			var rct = new RegisterCustomTool();

			// http://blogs.msdn.com/b/aaronmar/archive/2009/11/06/all-your-regkeys-are-belong-to-us.aspx
			// http://blogs.msdn.com/b/dsvst/archive/2010/03/01/dissecting-vs-2010-package-registration.aspx
			List<string> hives = new List<string> {
				"SOFTWARE\\Wow6432Node\\Microsoft\\VisualStudio\\12.0\\Generators",
				             "SOFTWARE\\Microsoft\\VisualStudio\\12.0\\Generators",
				"SOFTWARE\\Wow6432Node\\Microsoft\\VCSExpress\\12.0\\Generators",
				             "SOFTWARE\\Microsoft\\VCSExpress\\12.0\\Generators",
				"SOFTWARE\\Wow6432Node\\Microsoft\\VisualStudio\\11.0\\Generators",
				             "SOFTWARE\\Microsoft\\VisualStudio\\11.0\\Generators",
				"SOFTWARE\\Wow6432Node\\Microsoft\\VCSExpress\\11.0\\Generators", 
				             "SOFTWARE\\Microsoft\\VCSExpress\\11.0\\Generators", 
				"SOFTWARE\\Wow6432Node\\Microsoft\\VisualStudio\\10.0\\Generators",
				             "SOFTWARE\\Microsoft\\VisualStudio\\10.0\\Generators",
				"SOFTWARE\\Wow6432Node\\Microsoft\\VCSExpress\\10.0\\Generators",
				             "SOFTWARE\\Microsoft\\VCSExpress\\10.0\\Generators",
			};
			if (args.Length > 0 && args[0].StartsWith("SOFTWARE\\")) {
				hives.Clear();
				hives.Add(args[0]);
			}
			bool success = false;
			foreach (string hive in hives) {
				var key = Registry.LocalMachine.OpenSubKey(hive, true);
				if (key != null) {
					success = true;
					Console.WriteLine("Installing VS custom tool in registry: {0}", key);
					rct._root = key;
					rct.Initialize();
				}
			}
			if (!success)
				Console.WriteLine("Visual Studio registry hive not found!");

			Console.WriteLine("Note: this tool must also be registered with regasm.exe /codebase");
		}
	}
}
