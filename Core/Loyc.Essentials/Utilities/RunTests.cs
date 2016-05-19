using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Loyc;

namespace Loyc.MiniTest
{
	/// <summary>
	/// Searches for test methods and runs them, printing the name of each test to
	/// the console followed by errors (if any) produced by the test.
	/// </summary>
	/// <remarks>
	/// This class finds tests by looking for custom attributes by their string
	/// name (e.g. "TestAttribute"), so it is compatible with both NUnit.Framework
	/// and Loyc.MiniTest.
	/// <para/>
	/// RunTests is a stripped-down subset of the functionality supported by
	/// MiniTestRunner.
	/// </remarks>
	public static class RunTests
	{
		/// <summary>
		/// Runs all tests defined by the given object.
		/// </summary>
		/// <returns><c>true</c>, if all tests were successful, <c>false</c> otherwise.</returns>
		public static bool Run(object o)
		{
			// run all the tests methods in the given object
			MethodInfo[] methods = o.GetType().GetMethods();
			bool any = false;
			// A boolean that remembers whether any errors have
			// occurred while running the tests.
			bool anyErrors = false;

			MethodInfo setup = GetSetup(methods);
			MethodInfo teardown = GetTeardown(methods);

			foreach (MethodInfo method in methods)
			{
				object testAttr = IsTest(method);
				if (testAttr != null)
				{
					object fails = testAttr is TestAttribute ? ((TestAttribute)testAttr).Fails : null;
					any = true;
					try {
						Console.Write("{0}.{1}", o.GetType().NameWithGenericArgs(), method.Name);
						Console.WriteLine(fails != null ? " (Fails: "+fails+")" :  "");
						if (setup != null)
							setup.Invoke(o, null);
						method.Invoke(o, null);
					}
					catch (TargetInvocationException tie)
					{
						Exception exc = tie.InnerException;

						// Find out if it matches an expected exception
						// TODO: look for attribute by string instead
						object[] attrs = method.GetCustomAttributes(
							typeof(ExpectedExceptionAttribute), true);
						bool match = false;
						foreach (ExpectedExceptionAttribute ee in attrs) {
							if (exc.GetType().IsSubclassOf(ee.ExceptionType))
								match = true;
						}

						if (!match) {
							// Remember that an error has occurred.
							anyErrors = true;
							// Let the user know that something went wrong by
							// printing some text to the console.
							var old = Console.ForegroundColor;
							Console.ForegroundColor = fails != null ? ConsoleColor.DarkGray : ConsoleColor.Red;
							Console.WriteLine("{0} while running {1}.{2}:",
								exc.GetType().Name, o.GetType().Name, method.Name);
							Console.WriteLine(exc.Message);
							Console.Write(exc.DataList());
							Console.ForegroundColor = old;
						}
					}
					finally
					{
						if (teardown != null)
							teardown.Invoke(o, null);
					}
				}
			}
			if (!any)
				Console.WriteLine("{0} contains no tests.", o.GetType().NameWithGenericArgs());

			return !anyErrors;
		}

		/// <summary>
		/// Runs all tests belonging to the given array
		/// of objects.
		/// </summary>
		/// <returns><c>true</c>, if all tests were successful, <c>false</c> otherwise.</returns>
		public static bool RunMany(params object[] os)
		{
			bool anyErrors = false;
			foreach (var o in os)
			{
				if (!Run(o))
					anyErrors = true;
			}
			return !anyErrors;
		}

		private static object IsTest(MethodInfo info)
		{
			if (!info.IsStatic && info.IsPublic) {
				// this lets us know if a method is a valid [Test] method
				object[] attrs = info.GetCustomAttributes(true);
				return attrs.FirstOrDefault(attr => attr.GetType().Name == "TestAttribute");
			}
			return null;
		}

		private static MethodInfo GetMethodWithAttribute(MethodInfo[] methods, string attrName)
		{
			// find a method with a given attribute type
			foreach (MethodInfo method in methods) {
				if (!method.IsPublic || method.IsStatic)
					continue;
				object[] attrs = method.GetCustomAttributes(true);
				if (attrs != null && attrs.Any(attr => attr.GetType().Name == attrName))
					return method;
			}
			return null;
		}

		private static MethodInfo GetSetup(MethodInfo[] methods)
		{
			// Gets the setup method - returns null if there is none
			return GetMethodWithAttribute(methods, "SetUpAttribute");
		}

		private static MethodInfo GetTeardown(MethodInfo[] methods)
		{
			// Gets the teardown method - returns null if there is none
			return GetMethodWithAttribute(methods, "TearDownAttribute");
		}
	}
}
