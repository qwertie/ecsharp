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
		/// <summary>Runs all test methods on the given object (public methods 
		/// that have a TestAttribute).</summary>
		/// <returns>The number of tests that failed unexpectedly (where the 
		/// <see cref="TestAttribute.Fails"/> property is unset).</returns>
		public static int Run(object o)
		{
			int testCount = 0;
			int errorCount = 0;

			MethodInfo[] methods = o.GetType().GetMethods();
			MethodInfo setup = GetSetup(methods);
			MethodInfo teardown = GetTeardown(methods);

			foreach (MethodInfo method in methods)
			{
				object testAttr = IsTest(method);
				if (testAttr != null)
				{
					object fails = testAttr is TestAttribute ? ((TestAttribute)testAttr).Fails : null;
					testCount++;
					try {
						Console.Write("{0}.{1}", o.GetType().NameWithGenericArgs(), method.Name);
						Console.WriteLine(fails != null ? " (Fails: "+fails+")" :  "");
						if (setup != null)
							setup.Invoke(method.IsStatic ? null : o, null);
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
							if (fails == null)
								errorCount++;
							// Inform user that something went wrong.
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
			if (testCount == 0)
				Console.WriteLine("{0} contains no tests.", o.GetType().NameWithGenericArgs());

			return errorCount;
		}

		/// <summary>Runs all tests in an array of test objects.</summary>
		/// <returns>The total number of tests that unexpectedly failed.</returns>
		public static int RunMany(params object[] os)
		{
			return os.Aggregate(0, (errCount, o) => errCount + Run(o));
		}

		private static object IsTest(MethodInfo info)
		{
			if (info.IsPublic) {
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
