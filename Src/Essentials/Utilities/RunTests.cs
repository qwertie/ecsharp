using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using Loyc;

namespace NUnit.Framework
{
	public static class RunTests
	{	// #Test
		public static void Run(object o)
		{
			// run all the tests methods in the given object
			MethodInfo[] methods = o.GetType().GetMethods();

			MethodInfo setup = GetSetup(methods);
			MethodInfo teardown = GetTeardown(methods);

			foreach (MethodInfo method in methods)
			{
				if (IsTest(method))
				{
					try {
						Console.WriteLine("{0}.{1}", o.GetType().NameWithGenericArgs(), method.Name);
						if (setup != null)
							setup.Invoke(o, null);
						method.Invoke(o, null);
					}
					catch (TargetInvocationException tie)
					{
						Exception exc = tie.InnerException;
						
						// Find out if it matches an expected exception
						object[] attrs = method.GetCustomAttributes(
							typeof(ExpectedExceptionAttribute), true);
						bool match = false;
						foreach (ExpectedExceptionAttribute ee in attrs) {
							if (exc.GetType().IsSubclassOf(ee.ExceptionType))
								match = true;
						}

						if (!match) {
							Console.WriteLine("{0} while running {1}.{2}:",
								exc.GetType().Name, o.GetType().Name, method.Name);
							Console.WriteLine(exc.Message);
						}
					}
					finally
					{
						if (teardown != null)
							teardown.Invoke(o, null);
					}
				}
			}
		}
		private static bool IsTest(MethodInfo info)
		{
			if (!info.IsStatic && info.IsPublic) {
				// this lets us know if a method is a valid [Test] method
				object[] attrs = info.GetCustomAttributes(true);
				if (attrs.Any(attr => attr.GetType().Name == "TestAttribute"))
					return true;
			}
			return false;
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