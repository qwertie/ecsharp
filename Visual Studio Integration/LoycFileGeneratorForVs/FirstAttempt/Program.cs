using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Loyc
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var asm = Assembly.GetExecutingAssembly();
			var registrar = new RegistrationServices();
			char ch;
			if (args.Any(a => a == "--register")) 
				ch = 'Y';
			else if (args.Any(a => a == "--unregister")) 
				ch = 'U';
			else
			{
				Console.WriteLine("Would you like to register the \"LLLPG\" custom tool in Visual Studio? ");
				Console.Write("Y=Register, U=Unregister: ");
				ch = char.ToUpper(Console.ReadKey().KeyChar);
				Console.WriteLine();
			}

			if (ch == 'Y' || ch == 'R') {
				// Who the hell knows whether this will work on other people's machines
				//string mypath = @"""" + Assembly.GetExecutingAssembly().Location + @"""";
				//Console.WriteLine("Trying to invoke regasm.exe to register this file.");
				//if (!TryStart(@"regasm.exe", mypath))
				//	if (!TryStart(@"C:\Windows\Microsoft.NET\Framework\v4.0.30319\regasm.exe", mypath))
				//		if (!TryStart(@"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe", mypath)) {
				//			Console.WriteLine();
				//			Console.WriteLine("That didn't work, but you can still find and invoke it manually.");
				//			Console.WriteLine("Just pass this module as an argument to regasm.exe.");
				//		}
				
				// Found a better way!
				bool ok = registrar.RegisterAssembly(asm, AssemblyRegistrationFlags.None);
				Console.WriteLine(ok ? "Registered OK" : "Fail (No eligible types?!)");
			} else if (ch == 'U') {
				bool ok = registrar.UnregisterAssembly(asm);
				Console.WriteLine(ok ? "Unregistered OK" : "Fail (No eligible types?!)");
			} else
				Console.WriteLine("Exit.");
		}

		static bool TryStart(string program, string args)
		{
			try {
				Console.WriteLine("... Trying {0}", program);

				var p = new Process();
				p.StartInfo = new ProcessStartInfo(program, args) { UseShellExecute = false };
				p.Start();
				p.WaitForExit();
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine("{0}: {1}", ex.GetType().Name, ex.Message);
				return false;
			}
		}
	}
}
