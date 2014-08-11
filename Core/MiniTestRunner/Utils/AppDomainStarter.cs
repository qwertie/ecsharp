using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;
using System.IO;

namespace MiniTestRunner
{
	/// <summary><see cref="AppDomainStarter.Start"/> starts an AppDomain.</summary>
	public static class AppDomainStarter
	{
		/// <summary>Creates a type in a new sandbox-friendly AppDomain.</summary>
		/// <typeparam name="T">A trusted type derived MarshalByRefObject to create 
		/// in the new AppDomain. The constructor of this type must catch any 
		/// untrusted exceptions so that no untrusted exception can escape the new 
		/// AppDomain.</typeparam>
		/// <param name="baseFolder">Value to use for AppDomainSetup.ApplicationBase.
		/// The AppDomain will be able to use any assemblies in this folder.</param>
		/// <param name="appDomainName">A friendly name for the AppDomain. MSDN
		/// does not state whether or not the name must be unique.</param>
		/// <param name="constructorArgs">Arguments to send to the constructor of T,
		/// or null to call the default constructor. Do not send arguments of 
		/// untrusted types this way.</param>
		/// <param name="partialTrust">Whether the new AppDomain should run in 
		/// partial-trust mode.</param>
		/// <returns>A remote proxy to an instance of type T. You can call methods 
		/// of T and the calls will be marshalled across the appdomain boundary.</returns>
		public static T Start<T>(string baseFolder, string appDomainName, object[] constructorArgs, bool partialTrust)
			where T : MarshalByRefObject
		{
			// With help from http://msdn.microsoft.com/en-us/magazine/cc163701.aspx
			// "Discover Techniques for Safely Hosting Untrusted Add-Ins with the .NET Framework 2.0"
			AppDomainSetup setup = new AppDomainSetup();
			setup.ApplicationBase = baseFolder;
			
			AppDomain newDomain;
			if (partialTrust) {
				var permSet = new PermissionSet(PermissionState.None);
				permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
				permSet.AddPermission(new UIPermission(PermissionState.Unrestricted));
				permSet.AddPermission(new FileIOPermission(FileIOPermissionAccess.AllAccess, baseFolder));
				string folderOfT = Path.GetFullPath(Path.Combine(typeof(T).Assembly.Location, ".."));
				permSet.AddPermission(new FileIOPermission(FileIOPermissionAccess.Read, folderOfT));
				permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.AllFlags)); // Required to call Assembly.GetExportedTypes
				newDomain = AppDomain.CreateDomain(appDomainName, null, setup, permSet);
			} else {
				newDomain = AppDomain.CreateDomain(appDomainName, null, setup);
			}
			#pragma warning disable 618 // method is "obsolete" in .NET4 but its replacement does not exist in .NET 3.5
			return (T)Activator.CreateInstanceFrom(newDomain, 
				typeof(T).Assembly.ManifestModule.FullyQualifiedName, 
				typeof(T).FullName, false,
				0, null, constructorArgs, null, null, null).Unwrap();
		}
	}
}
