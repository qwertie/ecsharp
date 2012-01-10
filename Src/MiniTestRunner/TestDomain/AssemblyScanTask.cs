using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using Loyc.Collections;
using System.Threading;

namespace MiniTestRunner.TestDomain
{
	class AssemblyScanTask : TaskBase, ITaskEx
	{
		public readonly string AssemblyFilename;

		public AssemblyScanTask(string assemblyFilename)
		{
			Summary = AssemblyFilename = assemblyFilename;
		}

		protected override void Changed(string prop)
		{
			if (prop == "Status")
			{
				if (Status == TestStatus.NotRun)
					Summary = "Pending: " + AssemblyFilename;
				else if (Status == TestStatus.Running)
					Summary = "Opening: " + AssemblyFilename;
				else
					Summary = AssemblyFilename;
			}
			base.Changed(prop);
		}

		#region RunCore

		protected override void RunCore()
		{
			Assembly target = Assembly.LoadFrom(AssemblyFilename);

			List<IRowModel> typeRows = new List<IRowModel>();
			foreach (Type type in target.GetExportedTypes().Where(IsTestFixtureOrSuite))
				typeRows.Add(CreateTypeTree(type));

			Children = typeRows;
		}

		private ContainerRowModel CreateTypeTree(System.Type type)
		{
			var children = new List<IRowModel>();
			object[] attrs;
			Attribute attr;
			BindingFlags flags = BindingFlags.Static | BindingFlags.Public;
			Lazy<object> instance = null;
			if (IsConstructable(type)) {
				flags |= BindingFlags.Instance;
				instance = new Lazy<object>(() => Activator.CreateInstance(type), LazyThreadSafetyMode.ExecutionAndPublication);
			}

			foreach (MethodInfo method in type.GetMethods(flags).Where(m => IsTestMethod(m, true, true, true)))
			{
				// Create a row for this method
				attrs = method.GetCustomAttributes(true);
				attr = FindAttribute(attrs, "TestAttribute") ?? FindAttribute(attrs, "BenchmarkAttribute");
				var eea = FindAttribute(attrs, "ExpectedExceptionAttribute");
				bool isTestSet = method.IsStatic && MayBeTestSuite(method.ReturnType) && IsTestMethod(method, true, true, false);
				var utt = new UnitTestTask(method, instance, attr, eea, isTestSet);
				var row = new TaskRowModel(method.Name, TestNodeType.Test, utt);
				if (IsTestMethod(method, false, false, true)) // benchmark?
					row.Priority--; // Give benchmarks low priority by default

				children.Add(row);
			}

			foreach (Type nested in type.GetNestedTypes().Where(IsTestFixtureOrSuite))
				children.Add(CreateTypeTree(nested));
			
			// Create a row for this type
			var result = new ContainerRowModel(type.Name, TestNodeType.TestFixture, children);
			result.SetSummary(type.FullName);
			attrs = type.GetCustomAttributes(true);
			attr = FindAttribute(attrs, "TestFixtureAttribute");
			string description = GetPropertyValue<string>(attr, "Description", null);
			if (description != null)
				result.SetSummary(string.Format("{0} ({1})", description, type.FullName));
			return result;
		}

		private bool MayBeTestSuite(System.Type returns)
		{
			Type _;
			if (returns == typeof(object))
				return true;
			else if (IsDelegateTakingNoArgs(returns))
				return true;
			else if (IsPairWithStringKey(returns, out _))
				return true;
			else if (typeof(IEnumerable).IsAssignableFrom(returns))
				return true;
			else
				return false;
		}

		public bool IsPairWithStringKey(System.Type type, out System.Type typeOfValue)
		{
			typeOfValue = typeof(object);
			if (type.IsGenericType) {
				Type open = type.GetGenericTypeDefinition();
				Type[] args = type.GetGenericArguments();
				if (open == typeof(KeyValuePair<,>) && (args[0] == typeof(string) || args[0] == typeof(StringBuilder))) {
					typeOfValue = args[1];
					return true;
				}
			} else if (type == typeof(DictionaryEntry))
				return true;
			return false;
		}
		private bool IsDelegateTakingNoArgs(System.Type type)
		{
			return typeof(Delegate).IsAssignableFrom(type) && type.GetMethod("Invoke").GetParameters().Length == 0;
		}

		public bool IsConstructable(Type t)
		{
			return t.GetConstructor(System.Type.EmptyTypes) != null;
		}

		public bool IsTestFixtureOrSuite(Type t)
		{
			if (t == null)
				return false;
			if (t.ContainsGenericParameters)
				return false;
			object[] attrs = t.GetCustomAttributes(true);
			return IsTestFixture(t.Name, attrs) || IsTestSuite(t.Name, attrs);
		}

		public bool IsTestMethod(MethodInfo m, bool allowTest, bool allowSuite, bool allowBenchmark)
		{
			if (m.GetParameters().Length > 0)
				return false;
			object[] attrs = m.GetCustomAttributes(true);
			return (allowTest && IsTest(m.Name, attrs))
				|| (allowSuite && m.ReturnType != typeof(void) && IsTestSuite(m.Name, attrs))
				|| (allowBenchmark && (m.ReturnType == typeof(void) || m.ReturnType == typeof(string) || m.ReturnType == typeof(StringBuilder)) && IsBenchmark(m.Name, attrs));
		}

		public static Attribute FindAttribute(MethodInfo method, string name)
		{
			object[] attrs = method.GetCustomAttributes(true);
			return FindAttribute(attrs, name);
		}
		public static Attribute FindAttribute(object[] attrs, string name)
		{
			for (int i = 0; i < attrs.Length; i++)
				if (attrs[i].GetType().Name == name)
					return attrs[i] as Attribute;
			return null;
		}

		public static bool IsTest(string name, object[] attrs)
		{
			return name.EndsWith("_Test")
				|| name.StartsWith("Test_")
				|| FindAttribute(attrs, "TestAttribute") != null;
		}
		public static bool IsBenchmark(string name, object[] attrs)
		{
			return name.EndsWith("_Benchmark")
				|| name.StartsWith("Benchmark_")
				|| FindAttribute(attrs, "BenchmarkAttribute") != null;
		}
		public static bool IsTestSuite(string name, object[] attrs)
		{
			return name.EndsWith("_TestSuite")
				|| name.StartsWith("TestSuite_")
				|| FindAttribute(attrs, "TestSuiteAttribute") != null;
		}
		public static bool IsTestFixture(string name, object[] attrs)
		{
			return name.EndsWith("_TestFixture")
				|| name.StartsWith("TestFixture_")
				|| FindAttribute(attrs, "TestFixtureAttribute") != null;
		}

		#endregion
	}
}
