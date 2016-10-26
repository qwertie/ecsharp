using System;
using System.Collections.Generic;
using Loyc.Math;
using System.Collections;
using Loyc.Threading;
using System.Text;
using Loyc.Syntax;

namespace Loyc.MiniTest
{
	#region Attributes

	/// <summary>Identifies a class that contains unit tests, or methods that 
	/// return other tests or test fixtures.</summary> 
	/// <remarks>
	/// The MiniTest runner will ignore any class that does not have the 
	/// [TestFixture] attribute and is not named according to a recognized pattern,
	/// such as My_TestFixture.
	/// </remarks>
	/// <example>
	/// [TestFixture]
	/// public class ExampleClass {...}
	/// </example>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class TestFixtureAttribute : Attribute
	{
		private string description;

		/// <summary>
		/// Descriptive text for this fixture
		/// </summary>
		public string Description
		{
			get { return description; }
			set { description = value; }
		}
	}

	/// <summary>Identifies a method that contains a unit test, or that
	/// returns other tests or test fixtures.</summary> 
	/// <remarks>
	/// In addition to standard tests (which return void), the MiniTest runner
	/// is planned to support [Test] methods with other return values:
	/// <ul>
	/// <li>A test can return a string, which describes the result of the test.</li>
	/// <li>A test can return an Action (or any other delegate that takes no 
	/// arguments), which is treated as a sub-test and executed. Sub-tests are 
	/// run without set-up or tear-down steps.</li>
	/// <li>A test can return an object that does not implement IEnumerable, which 
	/// the test runner will assume is a test fixture. The object will be scanned
	/// for test methods to execute.</li>
	/// <li>A test can return an object that implements IEnumerable, which the 
	/// test runner will scan to find tests and test fixtures to execute.</li>
	/// <li>A test can return a KeyValuePair(TKey, TValue) or DictionaryEntry.
	/// In that case pair's Value is processed as though it were the return value,
	/// and the key is a status message.</li>
	/// </ul>
	/// These features give the MiniTest runner powerful capabilities while keeping
	/// it simple. However, please note that NUnit doesn't offer this feature.
	/// <para/>
	/// MiniTest allows tests to be static methods if they are inside a test 
	/// fixture. However, when test objects are returned from a [Test] method, 
	/// they are not scanned for static methods.
	/// <para/>
	/// If multiple tests return the same test fixture instance, directly or 
	/// indirectly, MiniTest runner will avoid running the test fixture instance 
	/// multiple times, but it can show the results at multiple places in the 
	/// result tree, which can be used to construct multiple "views" of the test 
	/// results.  However, if a test fixture is nested within itself, the nested
	/// instance is excluded from the result tree.
	/// <para/>
	/// If a TestFixture class contains only a single "test suite" method (a
	/// method that returns tests), MiniTest merges that method with the class in 
	/// the tree view. For example, if the class "MyTests" has a single method 
	/// "GetTests", the tree view will use one line for "MyTests.GetTests" rather 
	/// than separating out GetTests as a child of MyTests.
	/// </remarks>
	/// <example>
	/// [Test]
	/// public void MyTest() {...}
	/// </example>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple=false)]
	public class TestAttribute : Attribute
	{
		private string _description;
		private int _maxParallelThreads;
		private object _failInfo;

		/// <summary>
		/// Descriptive text for this test
		/// </summary>
		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		/// <summary>
		/// Indicates whether this test can be run in parallel with other tests
		/// in different test fixtures.
		/// </summary>
		/// <remarks>This property does not exist in NUnit.</remarks>
		public bool AllowParallel
		{
			get { return _maxParallelThreads > 1; }
			set {
				if (AllowParallel != value) _maxParallelThreads = (value ? 256 : 1);
			}
		}
		public int MaxParallelThreads
		{
			get { return _maxParallelThreads; }
			set { _maxParallelThreads = value; }
		}

		/// <summary>Setting this property is used to indicate that the test is 
		/// known to fail. It is used to mark tests that should be fixed eventually, 
		/// but will not be fixed immediately.</summary>
		/// <remarks>If you are new to a codebase, it helps to be able to tell the
		/// difference between new problems that need to be investigated and 
		/// addressed immediately, and old problems that have been placed on the 
		/// backburner to be fixed "someday", or feature requests that have not
		/// yet been addressed. Setting this property suggests that a failure may 
		/// be low-priority and may be an old issue.</remarks>
		public object Fails
		{
			get { return _failInfo; }
			set { _failInfo = value; }
		}
	}

	/// <summary>
	/// Marks a method that is to be called prior to each test in a test fixture.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple=false)]
	public class SetUpAttribute : Attribute
	{
	}

	/// <summary>
	/// Marks a method that is to be called after each test in a test fixture.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class TearDownAttribute : Attribute
	{
	}

	/// <summary>
	/// Marks a benchmark test, which exists to test performance. Benchmark tests
	/// are often run multiple times to obtain an average running time.
	/// </summary>
	/// <remarks>This attribute does not exist in NUnit.</remarks>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class BenchmarkAttribute : TestAttribute
	{
		/// <summary>Gets or sets the recommended minimum length of time to
		/// run the test. The test runner will run the test repeatedly until
		/// the total time elapsed exceeds this number.</summary>
		public int? RepeatForMs { get; set; }
		
		/// <summary>Gets or sets the recommended minimum number of times to run 
		/// the benchmark in order to get an average. If this property is left
		/// at the default value (null), the test runner can decide.</summary>
		/// <remarks>If RepeatForMs is also specified, the number of trials can
		/// be increased to reach the requested running time.</remarks>
		public int? MinTrials { get; set; }
	}

	/// <summary>
	/// Marks a test that is expected to throw an exception of a particular type.
	/// The test fails if the expected exception is not thrown.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public class ExpectedExceptionAttribute : Attribute
	{
		public Type ExceptionType { get; private set; }
		public ExpectedExceptionAttribute(Type type) { ExceptionType = type; }
	}

	#endregion

	/// <summary>An exception thrown when a method of <see cref="Assert"/> fails.</summary>
	[Serializable]
	public class TestException : Exception
	{
		public TestException(string message) : base(message) { }
		public TestException(string message, Exception inner) : base(message, inner) { }
	}

	/// <summary>
	/// Thrown when an assertion fails during a call to a method of <see cref="Assert"/>.
	/// </summary>
	public class AssertionException : TestException
	{
		public AssertionException(string message) : base(message) { }
		public AssertionException(string message, Exception inner) : base(message, inner) { }
	}
	
	/// <summary>Thrown by <see cref="Assert.Ignore()"/>.</summary>
	public class IgnoreException : TestException
	{
		public IgnoreException(string message) : base(message) { }
		public IgnoreException(string message, Exception inner) : base(message, inner) { }
	}

	/// <summary>Thrown by <see cref="Assert.Inconclusive()"/>.</summary>
	public class InconclusiveException : TestException
	{
		public InconclusiveException(string message) : base(message) { }
		public InconclusiveException(string message, Exception inner) : base(message, inner) { }
	}

	/// <summary>Thrown by <see cref="Assert.Success()"/>.</summary>
	public class SuccessException : TestException
	{
		public SuccessException(string message) : base(message) { }
		public SuccessException(string message, Exception inner) : base(message, inner) { }
	}
	

	/// <summary>
	/// The Assert class contains a collection of static methods that mirror
	/// the most common assertions used in NUnit.
	/// </summary>
	/// <remarks>
	/// This class is mostly a drop-in replacement for "old-style" NUnit tests, 
	/// i.e. those that do not use constraint classes or the "Is" class.
	/// <para/>
	/// Some methods were dropped to keep this class small. Use the full NUnit 
	/// framework if the remaining methods are not sufficient for you.
	/// <ul>
	/// <li>When the same assertion was known by multiple similar names (e.g. 
	/// True and IsTrue), I kept only one of the names. However, I did keep 
	/// That(), Expect() and IsTrue() even though they all do the same thing.</li>
	/// <li>Some less-common overloads that take a format string and arguments 
	/// were dropped.</li>
	/// <li>Some overloads were dropped when the compiler can automatically
	/// select a different overload instead. In particular, most overloads that 
	/// take a message string (without arguments) were dropped. Code that relied
	/// on those overloads will still compile, because the compiler will 
	/// construct an empty argument list and call the overload that takes a
	/// variable argument list.</li>
	/// </ul>
	/// </remarks>
	public class Assert
	{
		/// <summary>
		/// You may find it useful to derive a test fixture from Assert so that 
		/// you do not need to prefix every test with "Assert."
		/// </summary>
		protected Assert() { }

		#region StopTestDelegate and methods to stop a test (Fail, Inconclusive, etc.)

		public enum StopReason
		{
			Success, Fail, Ignore, Inconclusive
		}

		public delegate void StopTestDelegate(StopReason reason, string format, params object[] args);

		public static ThreadLocalVariable<StopTestDelegate> StopTestHandler = new ThreadLocalVariable<StopTestDelegate>(ThrowException);

		protected static void ThrowException(StopReason reason, string format, params object[] args)
		{
			string msg = format;
			try {
				msg = format.Localized(args);
			} catch(Exception ex) {
				// Exception occurred while converting arguments to string
				msg += string.Format(" [FORMATTING:{0}]", ex.GetType().Name);
			}

			switch (reason)
			{
				case StopReason.Fail:   throw new AssertionException(msg);
				case StopReason.Ignore: throw new IgnoreException(msg);
				case StopReason.Inconclusive: throw new InconclusiveException(msg);
				case StopReason.Success: throw new SuccessException(msg);
			}
			Console.Error.WriteLine(msg);
			return;
		}

		/// <summary>Fails a test by invoking <see cref="StopTestHandler"/>.Value(), 
		/// which, by default, throws an AssertionException.</summary>
		public static void Fail(string format, params object[] args)
		{
			StopTestHandler.Value(StopReason.Fail, format, args);
		}

		/// <summary>Stops a test via StopTestHandler, which, by default, throws
		/// an IgnoreException. This causes the test to be reported as ignored.</summary>
		public static void Ignore(string format, params object[] args)
		{
			StopTestHandler.Value(StopReason.Ignore, format, args);
		}

		/// <summary>Stops a test via StopTestHandler, which, by default, throws 
		/// an InconclusiveException. This causes the test to be reported as 
		/// inconclusive.</summary>
		public static void Inconclusive(string format, params object[] args)
		{
			StopTestHandler.Value(StopReason.Inconclusive, format, args);
		}

		/// <summary>Stops a test via StopTestHandler, which, by default, 
		/// throws a SuccessException.</summary>
		public static void Success(string format, params object[] args)
		{
			StopTestHandler.Value(StopReason.Success, format, args);
		}

		/// <summary>Short for Fail("").</summary>
		public static void Fail()
		{
			Fail("");
		}
		/// <summary>Short for Ignore("").</summary>
		public static void Ignore()
		{
			Ignore("");
		}
		/// <summary>Short for Inconclusive("").</summary>
		public static void Inconclusive()
		{
			Inconclusive("");
		}
		/// <summary>Short for Success("").</summary>
		public static void Success()
		{
			Success("");
		}

		#endregion

		#region Helper methods

		protected static bool DoublesAreEqual(double expected, double actual, double delta)
		{
			if (expected == actual)
				return true;
			if (expected - delta <= actual && actual <= expected + delta)
				return true;
			if (double.IsNaN(expected))
				return double.IsNaN(actual);
			if (double.IsInfinity(expected))
				return double.IsInfinity(actual) && double.IsPositiveInfinity(expected) == double.IsPositiveInfinity(actual);
			return false;
		}

		private static void Fail2(string userMsg, object[] userArgs, string stdMsg, params object[] stdArgs)
		{
			if (userMsg != null) {
				try {
					Fail(userMsg, userArgs);
				} catch (Exception ex) {
					try {
						ex.Data["Failed Assertion"] = stdMsg.Localized(stdArgs);
					} catch {
						ex.Data["Failed Assertion"] = stdMsg;
					}
					throw;
				}
			} else
				Fail(stdMsg, stdArgs);
		}

		// I like the way NUnit presents string differences so I'm preserving this in MiniTest.
		// Actual NUnit samples:
		//  Expected string length 80 but was 1. Strings differ at index 0.
		//  Expected: "01234567890123456789_1234567890123456789_1234567890123456789_..."
		//  But was:  "a"
		//  -----------^
		//  String lengths are both 80. Strings differ at index 10.
		//  Expected: "01234567890123456789_1234567890123456789_1234567890123456789_..."
		//  But was:  "0123456789_123456789_1234567890123456789_1234567890123456789_..."
		//  ---------------------^
		// Objects that are different:
		//  Expected: "01234567890123456789_1234567890123456789_1234567890123456789_1234567890123456789"
		//  But was:  5.5d
		//  Expected: not "01234567890123456789_1234567890123456789_1234567890123456789_1234567890123456789"
		//  But was:  "01234567890123456789_1234567890123456789_1234567890123456789_1234567890123456789"
		private static int TruncateStringsLongerThan = 64;
		private static string GetStringsNotEqualMessage(string a, string b)
		{
			// TODO: test this code
			if (a == null || b == null)
				return GetObjectMismatchMessage(a, b);
			else {
				int i, c = System.Math.Min((a ?? "").Length, (b ?? "").Length);
				for (i = 0; i < c; i++)
					if (a[i] != b[i])
						break;
				StringBuilder msg = new StringBuilder();
				if (a.Length == b.Length)
					msg.AppendFormat("  String lengths are both {0}. Strings differ at index {1}.\n", c, i);
				else
					msg.AppendFormat("  Expected string length {0} but was {1}. Strings differ at index {2}.\n", a.Length, b.Length, i);
				int a_i = i, b_i = i, maxlen = System.Math.Max(a.Length, b.Length);
				msg.AppendFormat("  Expected: {0}\n", GetQuotedString(ref a, ref a_i, maxlen));
				msg.AppendFormat("  But was:  {0}\n", GetQuotedString(ref b, ref b_i, maxlen));
				
				int TailLength = "-----------".Length;
				var prefix = b.Left(b_i);
				int i_adjusted = ParseHelpers.EscapeCStyle(prefix, EscapeC.Default, '"').Length;
				msg.Append(' ', 2);
				msg.Append('-', TailLength + i_adjusted);
				msg.Append("^\n");
				return msg.ToString();
			}
		}
		static string GetQuotedString(ref string s, ref int dif_i, int len)
		{
			int maxw = TruncateStringsLongerThan;
			if (len > maxw) {
				if (dif_i < maxw/2) {
					s = s.Left(maxw - 3) + "..."; // "beginning..."
				} else if (len - dif_i < maxw/2) {
					s = "..." + s.SafeSubstring(len - (maxw - 3)); // "...ending"
					dif_i -= len - maxw;
				} else {
					s = "..." + s.SafeSubstring(dif_i - maxw / 2 + 3, maxw - 6) + "...";
					dif_i = maxw / 2; // "...middle..."
				}
			}
			return "\"" + ParseHelpers.EscapeCStyle(s, EscapeC.Default, '"') + "\"";
		}
		static string StringifyObject(object obj)
		{
			if (obj == null)
				return "null";
			if (obj is IConvertible)
			{
				if (obj is string)
					return "\"" + (string)obj + "\"";
				if (obj is double) return obj.ToString() + "d";
				if (obj is float) return obj.ToString() + "f";
				if (obj is long) return obj.ToString() + "L";
				if (obj is ulong) return obj.ToString() + "uL";
				if (obj is uint) return obj.ToString() + "u";
				if (obj is decimal) return obj.ToString() + "m";
			}
			return obj.ToString();
		}

		static string GetObjectMismatchMessage(object a, object b) { return GetObjectMismatchMessage(a, b, false); }
		static string GetObjectMismatchMessage(object a, object b, bool expectedNotEqual)
		{
			if (a is string && b is string)
				return GetStringsNotEqualMessage((string)a, (string)b);
			else {
				StringBuilder msg = new StringBuilder();
				if (expectedNotEqual)
					msg.AppendFormat("  Expected: not {0}\n", StringifyObject(a));
				else
					msg.AppendFormat("  Expected: {0}\n", StringifyObject(a));
				msg.AppendFormat("  But was:  {0}\n", StringifyObject(b));
				return msg.ToString();
			}
		}

		#endregion

		#region Equals and ReferenceEquals

		/// <summary>
		/// Equals() is inherited from object; you probably want to call AreEqual instead.
		/// </summary>
		[Obsolete("Use AreEqual instead")]
		public static new void Equals(object a, object b) { AreEqual(a, b); }

		/// <summary>
		/// Verifies that two references are equal.
		/// </summary>
		public static new void ReferenceEquals(object a, object b)
		{
			That(a == b, "References are not equal: {0} != {1}.", a, b);
		}

		#endregion

		// the goal!
		//class AList_TestFixture : Assert
		//{
		//    public static object[] Test_Suite1()
		//    {
		//        return new object[] {
		//            new AList_TestFixture(10),
		//            new AList_TestFixture(100),
		//            new AList_TestFixture(1000),
		//        };
		//    }
		//    public static IEnumerable<object> Test_Suite2()
		//    {
		//        yield return KeyValuePair<string, AList_TestFixture>("10", new AList_TestFixture(10));
		//        yield return KeyValuePair<string, AList_TestFixture>("100", new AList_TestFixture(100));
		//    }
		//
		//    public AList_TestFixture(int iterations) {
		//    }
		//
		//    IEnumerable<Func<int,int>> Test_Functions() { yield return ... }
		//
		//    string Test_Success() { return "Test passed with flying colors"; }
		//}


		/// <summary>Calls Fail(message, args) if condition is false.</summary>
		public static void That(bool condition, string message, params object[] args)
		{
			if (!condition) Fail(message, args);
		}

		/// <summary>Calls Fail(message) if condition is false.</summary>
		public static void That(bool condition, string message)
		{
			if (!condition) Fail(message);
		}
		
		/// <summary>Calls Fail() if condition is false.</summary>
		public static void That(bool condition)
		{
			if (!condition) Fail("That: condition is false");
		}

		/// <summary>Calls Fail() if condition is false.</summary>
		public static void Expect(bool condition)
		{
			if (!condition) Fail("Expect: condition is false");
		}

		/// <summary>
		/// Verifies that a delegate throws a particular exception when called.
		/// </summary>
		/// <param name="expectedExceptionType">The exception Type expected</param>
		/// <param name="code">A method to run</param>
		/// <param name="message">The message that will be displayed on failure</param>
		/// <param name="args">Arguments to be used in formatting the message</param>
		public static Exception Throws(Type expectedExceptionType, Action code, string message, params object[] args)
		{
			try {
				code();
			} catch (Exception ex) {
				if (expectedExceptionType.IsAssignableFrom(ex.GetType()))
					return ex;
				Fail2(message, args, "Throws(): Expected {0}, got {1}", expectedExceptionType.Name, ex.GetType().Name);
			}
			Fail2(message, args, "Throws(): Expected {0}, but no exception was thrown", expectedExceptionType.Name);
			return null; // normally unreachable
		}
		
		public static Exception Throws(Type expectedExceptionType, Action code)
		{
			return Throws(expectedExceptionType, code, null, null);
		}

		public static T Throws<T>(Action code, string message, params object[] args) where T : Exception
		{
			return (T)Throws(typeof(T), code, message, args);
		}
		public static T Throws<T>(Action code) where T : Exception
		{
			return (T)Throws(typeof(T), code);
		}

		/// <summary>
		/// Verifies that a delegate throws an exception when called and returns it.
		/// </summary>
		/// <param name="code">A method to run</param>
		/// <param name="message">The message that will be displayed on failure</param>
		/// <param name="args">Arguments to be used in formatting the message</param>
		public static Exception Catch(Action code, string message, params object[] args)
		{
			return Throws(typeof(Exception), code, message, args);
		}

		/// <summary>
		/// Verifies that a delegate throws an exception when called
		/// and returns it.
		/// </summary>
		/// <param name="code">A TestDelegate</param>
		public static Exception Catch(Action code)
		{
			return Throws(typeof(Exception), code);
		}

		/// <summary>
		/// Verifies that a delegate does not throw an exception
		/// </summary>
		/// <param name="code">Code to run</param>
		/// <param name="message">The message that will be displayed on failure</param>
		public static void DoesNotThrow(Action code, string message, params object[] args)
		{
			try {
				code();
			} catch (Exception ex) {
				Assert.Fail2(message, args, "Unexpected exception: {0}", ex.GetType());
			}
		}
		/// <summary>
		/// Verifies that a delegate does not throw an exception.
		/// </summary>
		/// <param name="code">Code to run</param>
		public static void DoesNotThrow(Action code)
		{
			DoesNotThrow(code, null, null);
		}

		public static void IsTrue(bool condition, string message, params object[] args)
		{
			if (!condition) 
				Fail2(message, args, "IsTrue: condition is unexpectedly false");
		}
		public static void IsTrue(bool condition)
		{
			IsTrue(condition, null, null);
		}
		public static void IsFalse(bool condition, string message, params object[] args)
		{
			if (condition) 
				Fail2(message, args, "IsFalse: condition is unexpectedly true");
		}
		public static void IsFalse(bool condition)
		{
			IsFalse(condition, null, null);
		}
		public static void IsNotNull(object anObject, string message, params object[] args)
		{
			if (anObject == null) 
				Fail2(message, args, "IsNotNull: object is null");
		}
		public static void IsNotNull(object anObject)
		{
			IsNotNull(anObject, null, null);
		}
		public static void IsNull(object anObject, string message, params object[] args)
		{
			if (anObject != null) 
				Fail2(message, args, "IsNull: object is not null");
		}
		public static void IsNull(object anObject)
		{
			IsNull(anObject, null, null);
		}
		public static void IsNaN(double aDouble)
		{
			if (!double.IsNaN(aDouble))
				Fail("IsNaN: {0} is a number", aDouble);
		}
		public static void IsEmpty(string aString)
		{
			if ("" != aString)
				Fail("IsEmpty: {0} != \"\"", aString);
		}
		public static void IsEmpty(System.Collections.IEnumerable collection)
		{
			if (collection == null) 
				Fail ("IsEmpty: collection is null");
			if (collection.GetEnumerator().MoveNext())
				Fail("IsEmpty: collection is not empty");
		}
		public static void IsNotEmpty(System.Collections.IEnumerable collection)
		{
			if (collection != null)
				Fail("IsNotEmpty: collection is null");
			if (!collection.GetEnumerator().MoveNext())
				Fail("IsNotEmpty: collection is empty");
		}
		public static void IsNullOrEmpty(string aString)
		{
			if (!string.IsNullOrEmpty(aString))
				Fail("IsNullOrEmpty: unexpected string: {0}", aString);
		}
		public static void IsNotNullOrEmpty(string aString)
		{
			if (string.IsNullOrEmpty(aString))
				Fail("IsNotNullOrEmpty: string is {0}", aString == null ? "null" : "\"\"");
		}
		public static void IsInstanceOf(Type expected, object actual)
		{
			if (actual == null)
				Fail("IsInstanceOf: value is null");
			if (!expected.IsAssignableFrom(actual.GetType()))
				Fail("IsInstanceOf: expected {0}, got {1} ({2})", expected.Name, actual.GetType().Name, actual);
		}
		public static void IsNotInstanceOf(Type expected, object actual)
		{
			if (actual != null && expected.IsAssignableFrom(actual.GetType()))
				Fail("IsNotInstanceOf: got an instance of {0} ({1})", actual.GetType().Name, actual);
		}
		public static void IsInstanceOf<T>(object actual)
		{
			IsInstanceOf(typeof(T), actual);
		}
		public static void IsNotInstanceOf<T>(object actual)
		{
			IsNotInstanceOf(typeof(T), actual);
		}


		public static void AreEqual(long expected, long actual, string message, params object[] args)
		{
			if (expected != actual)
				Fail2(message, args, "AreEqual: {0} != {1}", expected, actual);
		}
		public static void AreEqual(ulong expected, ulong actual, string message, params object[] args)
		{
			if (expected != actual)
				Fail2(message, args, "AreEqual: {0} != {1}", expected, actual);
		}
		public static void AreEqual(int expected, int actual)
		{
			AreEqual(expected, actual, null, null);
		}
		public static void AreEqual(long expected, long actual)
		{
			AreEqual(expected, actual, null, null);
		}
		[CLSCompliant(false)]
		public static void AreEqual(ulong expected, ulong actual)
		{
			AreEqual(expected, actual, null, null);
		}
		public static void AreEqual(decimal expected, decimal actual)
		{
			if (expected != actual)
				Fail("AreEqual: {0} != {1}", expected, actual);
		}
		public static void AreEqual(double expected, double actual, double delta, string message, params object[] args)
		{
			if (!DoublesAreEqual(expected, actual, delta))
				Fail2(message, args, "AreEqual: {0} != {1} (delta: {2})", expected, actual, delta);
		}
		public static void AreEqual(double expected, double actual, double delta)
		{
			AreEqual(expected, actual, delta, null, null);
		}
		public static void AreEqual(double expected, double actual)
		{
			AreEqual(expected, actual, 0, null, null);
		}
		public static void AreEqual(object expected, object actual, string message, params object[] args)
		{
			if (!object.Equals(expected, actual))
				Fail2(message, args, GetObjectMismatchMessage(expected, actual), (object[])null);
		}
		public static void AreEqual(object expected, object actual)
		{
			AreEqual(expected, actual, null, null);
		}


		public static void AreNotEqual(long expected, long actual, string message, params object[] args)
		{
			if (expected == actual)
				Fail2(message, args, "AreNotEqual: {0} == {1}", expected, actual);
		}
		public static void AreNotEqual(ulong expected, ulong actual, string message, params object[] args)
		{
			if (expected == actual)
				Fail2(message, args, "AreNotEqual: {0} == {1}", expected, actual);
		}
		public static void AreNotEqual(int expected, int actual)
		{
			AreNotEqual(expected, actual, null, null);
		}
		public static void AreNotEqual(long expected, long actual)
		{
			AreNotEqual(expected, actual, null, null);
		}
		[CLSCompliant(false)]
		public static void AreNotEqual(ulong expected, ulong actual)
		{
			AreNotEqual(expected, actual, null, null);
		}
		public static void AreNotEqual(decimal expected, decimal actual)
		{
			if (expected == actual)
				Fail("AreNotEqual: {0} == {1}", expected, actual);
		}
		public static void AreNotEqual(double expected, double actual, double delta, string message, params object[] args)
		{
			if (DoublesAreEqual(expected, actual, delta))
				Fail2(message, args, "AreNotEqual: {0} == {1} (delta: {2})", expected, actual, delta);
		}
		public static void AreNotEqual(double expected, double actual, double delta)
		{
			AreNotEqual(expected, actual, delta, null, null);
		}
		public static void AreNotEqual(object expected, object actual, string message, params object[] args)
		{
			if (object.Equals(expected, actual))
				Fail2(message, args, GetObjectMismatchMessage(expected, actual, true));
		}
		public static void AreNotEqual(object expected, object actual)
		{
			AreNotEqual(expected, actual, null, null);
		}


		public static void AreSame(object expected, object actual, string message, params object[] args)
		{
			if (!object.ReferenceEquals(expected, actual))
				Fail2(message, args, "AreSame: references are not equal: {0} != {1}", expected, actual);
		}
		public static void AreSame(object expected, object actual)
		{
			AreSame(expected, actual, null, null);
		}
		public static void AreNotSame(object expected, object actual, string message, params object[] args)
		{
			if (object.ReferenceEquals(expected, actual))
				Fail2(message, args, "AreNotSame: references are equal: {0} == {1}", expected, actual);
		}
		public static void AreNotSame(object expected, object actual)
		{
			AreNotSame(expected, actual, null, null);
		}


		public static void Greater(long arg1, long arg2, string message, params object[] args)
		{
			if (!(arg1 > arg2))
				Fail2(message, args, "Greater: {0} <= {1}", arg1, arg2);
		}
		public static void Greater(double arg1, double arg2, string message, params object[] args)
		{
			if (!(arg1 > arg2))
				Fail2(message, args, "Greater: {0} <= {1}", arg1, arg2);
		}
		public static void Greater(IComparable arg1, IComparable arg2, string message, params object[] args)
		{
			if (arg1.CompareTo(arg2) <= 0)
				Fail2(message, args, "Greater: {0} <= {1}", arg1, arg2);
		}
		public static void Greater(int arg1, int arg2)
		{
			Greater(arg1, arg2, null, null);
		}
		public static void Greater(long arg1, long arg2)
		{
			Greater(arg1, arg2, null, null);
		}
		public static void Greater(double arg1, double arg2)
		{
			Greater(arg1, arg2, null, null);
		}
		public static void Greater(IComparable arg1, IComparable arg2)
		{
			Greater(arg1, arg2, null, null);
		}


		public static void Less(long arg1, long arg2, string message, params object[] args)
		{
			if (!(arg1 < arg2))
				Fail2(message, args, "Less: {0} >= {1}", arg1, arg2);
		}
		public static void Less(double arg1, double arg2, string message, params object[] args)
		{
			if (!(arg1 < arg2))
				Fail2(message, args, "Less: {0} >= {1}", arg1, arg2);
		}
		public static void Less(IComparable arg1, IComparable arg2, string message, params object[] args)
		{
			if (arg1.CompareTo(arg2) >= 0)
				Fail2(message, args, "Less: {0} >= {1}", arg1, arg2);
		}
		public static void Less(int arg1, int arg2)
		{
			Less(arg1, arg2, null, null);
		}
		public static void Less(long arg1, long arg2)
		{
			Less(arg1, arg2, null, null);
		}
		public static void Less(double arg1, double arg2)
		{
			Less(arg1, arg2, null, null);
		}
		public static void Less(IComparable arg1, IComparable arg2)
		{
			Less(arg1, arg2, null, null);
		}


		public static void GreaterOrEqual(long arg1, long arg2, string message, params object[] args)
		{
			if (!(arg1 >= arg2))
				Fail2(message, args, "GreaterOrEqual: {0} < {1}", arg1, arg2);
		}
		public static void GreaterOrEqual(double arg1, double arg2, string message, params object[] args)
		{
			if (!(arg1 >= arg2))
				Fail2(message, args, "GreaterOrEqual: {0} < {1}", arg1, arg2);
		}
		public static void GreaterOrEqual(IComparable arg1, IComparable arg2, string message, params object[] args)
		{
			if (arg1.CompareTo(arg2) < 0)
				Fail2(message, args, "GreaterOrEqual: {0} < {1}", arg1, arg2);
		}
		public static void GreaterOrEqual(int arg1, int arg2)
		{
			GreaterOrEqual(arg1, arg2, null, null);
		}
		public static void GreaterOrEqual(long arg1, long arg2)
		{
			GreaterOrEqual(arg1, arg2, null, null);
		}
		public static void GreaterOrEqual(double arg1, double arg2)
		{
			GreaterOrEqual(arg1, arg2, null, null);
		}
		public static void GreaterOrEqual(IComparable arg1, IComparable arg2)
		{
			GreaterOrEqual(arg1, arg2, null, null);
		}
	
		
		public static void LessOrEqual(long arg1, long arg2, string message, params object[] args)
		{
			if (!(arg1 <= arg2))
				Fail2(message, args, "LessOrEqual: {0} > {1}", arg1, arg2);
		}
		public static void LessOrEqual(double arg1, double arg2, string message, params object[] args)
		{
			if (!(arg1 <= arg2))
				Fail2(message, args, "LessOrEqual: {0} > {1}", arg1, arg2);
		}
		public static void LessOrEqual(IComparable arg1, IComparable arg2, string message, params object[] args)
		{
			if (arg1.CompareTo(arg2) > 0)
				Fail2(message, args, "LessOrEqual: {0} > {1}", arg1, arg2);
		}
		public static void LessOrEqual(int arg1, int arg2)
		{
			LessOrEqual(arg1, arg2, null, null);
		}
		public static void LessOrEqual(long arg1, long arg2)
		{
			LessOrEqual(arg1, arg2, null, null);
		}
		public static void LessOrEqual(double arg1, double arg2)
		{
			LessOrEqual(arg1, arg2, null, null);
		}
		public static void LessOrEqual(IComparable arg1, IComparable arg2)
		{
			LessOrEqual(arg1, arg2, null, null);
		}
		

		/// <summary>
		/// Asserts that an object is contained in a list.
		/// </summary>
		/// <param name="expected">The expected object</param>
		/// <param name="actual">The list to be examined</param>
		/// <param name="message">The message to display in case of failure</param>
		/// <param name="args">Array of objects to be used in formatting the message</param>
		public static void Contains(object expected, IEnumerable actual, string message, params object[] args)
		{
			int count = 0;
			foreach (object item in actual) {
				count++;
				if (object.Equals(item, expected))
					return;
			}
			Fail2(message, args, "Collection of {0} items lacks the expected item: {1}", count, expected);
		}
		/// <summary>
		/// Asserts that an object is contained in a list.
		/// </summary>
		/// <param name="expected">The expected object</param>
		/// <param name="actual">The list to be examined</param>
		public static void Contains(object expected, IEnumerable actual)
		{
			Contains(expected, actual, null, null);
		}
	}
}
