using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace Loyc
{
	/// <summary>Something inside .NET automatically invokes the method marked 
	/// [ComRegisterFunction] when registering a DLL with COM via the regasm.exe utility,
	/// and similarly the [ComUnregisterFunction] method is invoked during deregistration.</summary>
	class RegisterCustomTool
	{
		/// <summary>Undocumented fact: a method marked [ComRegisterFunction] is NOT 
		/// called except on a COM-exported class, which I guess means "something 
		/// that has a Guid attribute and inherits a COM interface". Since the 
		/// ComRegisterFunction takes a Type argument, I assumed I could just write 
		/// a single handler here, and it would be called for all Types being 
		/// registered. In reality, no; separate registration methods are needed
		/// in each class that wants to be registered.
		/// </summary>
		[ComRegisterFunction] // doesn't work
		public static void Register(Type t)
		{
			var info = (CustomToolAttribute)GetAttribute(t, typeof(CustomToolAttribute));
			if (info != null)
				RegisterWithVs(t, info.ToolName, info.ToolDescr, info.VsLanguageGuid, info.VsVersions);
		}

		[ComUnregisterFunction] // doesn't work
		public static void Unregister(Type t)
		{
			var info = (CustomToolAttribute)GetAttribute(t, typeof(CustomToolAttribute));
			if (info != null)
				UnregisterWithVs(t, info.ToolName, info.VsLanguageGuid, info.VsVersions);
		}

		internal static string GetKeyName(string vsVersion, Guid vsLanguageGuid, string toolName)
		{
			return String.Format(@"SOFTWARE\Microsoft\VisualStudio\{0}\Generators\{{{1}}}\{2}\",
				vsVersion, vsLanguageGuid, toolName);
		}

		internal static void RegisterWithVs(Type t, string toolName, string toolDescr, Guid vsLanguageGuid, params string[] vsVersions)
		{
			GuidAttribute toolGuid = GetGuidAttribute(t);
			foreach (var vsVersion in vsVersions)
				using (RegistryKey key = Registry.LocalMachine.CreateSubKey(
					GetKeyName(vsVersion, vsLanguageGuid, toolName)))
				{
					key.SetValue("", toolDescr);
					key.SetValue("CLSID", "{" + toolGuid.Value + "}");
					key.SetValue("GeneratesDesignTimeSource", 1);
				}
		}

		internal static void UnregisterWithVs(Type t, string toolName, Guid vsLanguageGuid, params string[] vsVersions)
		{
			foreach (var vsVersion in vsVersions)
				Registry.LocalMachine.DeleteSubKey(
					GetKeyName(vsVersion, vsLanguageGuid, toolName), false);
		}

		internal static GuidAttribute GetGuidAttribute(Type t)
		{
			return (GuidAttribute)GetAttribute(t, typeof(GuidAttribute));
		}
		internal static Attribute GetAttribute(Type t, Type attributeType)
		{
			object[] attributes = t.GetCustomAttributes(attributeType, /* inherit */ true);
			if (attributes.Length == 0)
				return null;
			return (Attribute)attributes[0];
		}
	}

	internal class CustomToolAttribute : Attribute 
	{
		public string ToolName, ToolDescr;
		public Guid VsLanguageGuid;
		public string[] VsVersions;
		public CustomToolAttribute(string toolName, string toolDescr, string vsLanguageGuid, params string[] vsVersions)
		{
			ToolName = toolName;
			ToolDescr = toolDescr;
			VsLanguageGuid = new Guid(vsLanguageGuid);
			VsVersions = vsVersions;
		}
	}
}
