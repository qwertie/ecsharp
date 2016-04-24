using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace LeMP.Tests
{
	[TestFixture]
	public class TestCodeContractMacros : MacroTesterBase
	{
		[Test]
		public void AssertAttributeTest()
		{
			TestEcs(@"[assert(x >= min, x <= max)]"+
			        @"public void AssertRange(int x, int min, int max) { }",
			        @"public void AssertRange(int x, int min, int max) {
			            System.Diagnostics.Debug.Assert(x >= min, ""Assertion failed in `AssertRange`: x >= min""); 
			            System.Diagnostics.Debug.Assert(x <= max, ""Assertion failed in `AssertRange`: x <= max"");
			        }");
			TestEcs(@"public void Wait([assert(_ != null)] Task<T> task) { task.Wait(); }",
			        @"public void Wait(Task<T> task) { 
			            System.Diagnostics.Debug.Assert(task != null, ""Assertion failed in `Wait`: task != null""); 
			            task.Wait();
			        }");
		}

		[Test]
		public void RequireTest()
		{
			// [requires] is currently not supported for methods that do not have a body (e.g. interface methods)
			// [ensures] is currently not supported for methods that do not have a body (e.g. interface methods)
			TestEcs("[requires(t != null)]" +
				   @"public void Wait(Task<T> t) { t.Wait(); }",
				   @"public void Wait(Task<T> t) { " +
				   @"  Contract.Assert(t != null, ""Precondition failed: t != null""); t.Wait(); " +
				   @"}");
			TestEcs("public void Wait([requires(_ != null)] Task<T> t) { t.Wait(); }",
				   @"public void Wait(Task<T> t) { " +
				   @"  Contract.Assert(t != null, ""Precondition failed: t != null""); t.Wait(); " +
				   @"}");
			TestEcs("static uint Decrement([requires(_ > 0)] uint positive) => positive - 1;", 
			       @"static uint Decrement(uint positive) {
			           Contract.Assert(positive > 0, ""Precondition failed: positive > 0"");
			           return positive - 1;
			       }");
			TestEcs("public this([requires(_ > 0)] uint positive) { P = positive; }", 
			       @"public this(uint positive) {
			           Contract.Assert(positive > 0, ""Precondition failed: positive > 0"");
			           P = positive;
			       }");
		}

		[Test]
		public void RequirePropertyTest()
		{
			TestEcs(@"[requires(_ > 0)]
			       static uint Positive { 
			           get { return P; }
			           set { P = value; }
			       }",
				   @"static uint Positive { 
			           get { return P; }
			           set { 
			               Contract.Assert(value > 0, ""Precondition failed: value > 0"");
			               P = value;
			           }
			       }");
			TestEcs(@"
			       static uint Positive { 
			           get { return P; }
			           [requires(_ > 0)]
			           set { P = value; }
			       }",
				   @"static uint Positive { 
			           get { return P; }
			           set { 
			               Contract.Assert(value > 0, ""Precondition failed: value > 0"");
			               P = value;
			           }
			       }");
			TestEcs(@"public T this[[requires((uint)_ < (uint)Count)] int index]
			       { 
			           get { return _array[index]; }
			           internal set { _array[index] = value; }
			       }",
				   @"public T  this[int index] { 
			           get { 
			               Contract.Assert((uint)index < (uint)Count, ""Precondition failed: (uint)index < (uint)Count"");
			               return _array[index];
			           }
			           internal set {
			               Contract.Assert((uint)index < (uint)Count, ""Precondition failed: (uint)index < (uint)Count"");
			               _array[index] = value;
			           }
			       }");
		}

		[Test]
		public void EnsuresTest()
		{
			TestEcs(@"[ensures(_ >= 0)]
			          public static int Square(int x) { 
			            return x*x;
			          }",
			        @"public static int Square(int x) {
			            { var return_value = x*x; 
			              Contract.Assert(return_value >= 0, ""Postcondition failed: return_value >= 0""); 
			              return return_value; }
			          }");
			TestEcs(@"public static Node Root { 
			            [ensures(_ != null, !IsFrozen)] 
			            get { return _root; }
			          }",
			        @"public static Node Root { get {
			            { var return_value = _root; 
			              Contract.Assert(return_value != null, ""Postcondition failed: return_value != null"");
			              Contract.Assert(!IsFrozen, ""Postcondition failed: !IsFrozen"");
			              return return_value; }
			          } }");
			TestEcs(@"[ensures(File.Exists(filename))]
			          void Save(string filename) { 
			            File.WriteAllText(filename, ""Saved!"");
			          }",
			        @"void Save(string filename) { 
			            File.WriteAllText(filename, ""Saved!""); 
			            Contract.Assert(File.Exists(filename), ""Postcondition failed: File.Exists(filename)"");
			          }");
			TestEcs(@"[ensuresFinally(everybodyIsHappy)]
			          void Save(string filename) { 
			            File.WriteAllText(filename, ""Saved!"");
			          }",
			        @"void Save(string filename) { 
			            try {
			                File.WriteAllText(filename, ""Saved!""); 
			            } finally {
			                Contract.Assert(everybodyIsHappy, ""Postcondition failed: everybodyIsHappy"");
			            }
			          }");
		}

		[Test]
		public void EnsuresPropertyTest()
		{
			TestEcs(@"
			       static uint Positive { 
			           [ensures(_ > 0)]
			           get { return P; }
			           set { P = value; }
			       }",
			       @"static uint Positive { 
			           get { {
			               var return_value = P;
			               Contract.Assert(return_value > 0, ""Postcondition failed: return_value > 0"");
			               return return_value;
			           } }
			           set { P = value; }
			       }");
			TestEcs(@"[ensures(_ > 0)]
			       static uint Positive { 
			           get { return P; }
			           set { P = value; }
			       }",
				   @"static uint Positive { 
			           get { {
			               var return_value = P;
			               Contract.Assert(return_value > 0, ""Postcondition failed: return_value > 0"");
			               return return_value;
			           } }
			           set { P = value; }
			       }");
			TestEcs(@"[ensures(P > 0)]
			       static uint Positive { 
			           get { return P; }
			           set { P = value; }
			       }",
				   @"static uint Positive { 
			           get { {
			               var return_value = P;
			               Contract.Assert(P > 0, ""Postcondition failed: P > 0"");
			               return return_value;
			           } }
			           set { 
			               P = value;
			               Contract.Assert(P > 0, ""Postcondition failed: P > 0"");
			           }
			       }");
			TestEcs(@"[ensures(_ > 0)] static uint Positive => P;",
			        @"static uint Positive { 
			           get { {
			               var return_value = P;
			               Contract.Assert(return_value > 0, ""Postcondition failed: return_value > 0"");
			               return return_value;
			           } }
			       }");
		}

		[Test]
		public void NotNullTest()
		{
			TestEcs("public void Wait(notnull Task<T> t) { t.Wait(); }",
				   @"public void Wait(Task<T> t) { " +
				   @"  Contract.Assert(t != null, ""Precondition failed: t != null""); t.Wait(); " +
				   @"}");
			TestEcs("void IFoo<T>.Bar<U>(notnull Task<T> t) { t.Wait(); }",
				   @"void IFoo<T>.Bar<U>(Task<T> t) { " +
				   @"  Contract.Assert(t != null, ""Precondition failed: t != null""); t.Wait(); " +
				   @"}");
			TestEcs(@"public class Foo {
			           public Foo(notnull string name) { Name = name; }
			       }",
				   @"public class Foo {
			           public Foo(string name) {
			               Contract.Assert(name != null, ""Precondition failed: name != null""); 
			               Name = name;
                       }
			       }");
			TestEcs(@"public static notnull string Double(string x) { 
			            return x + x;
			          }",
			        @"public static string Double(string x) { {
			            var return_value = x + x; 
			            Contract.Assert(return_value != null, ""Postcondition failed: return_value != null""); 
			            return return_value;
			        } }");
		}

		[Test]
		public void NotNullPropertyTest()
		{
			TestEcs(@"notnull static uint Positive { get { return P; } set { P = value; } }",
			        @"static uint Positive { 
			           get { {
			               var return_value = P;
			               Contract.Assert(return_value != null, ""Postcondition failed: return_value != null"");
			               return return_value;
			           } }
			           set {
			               Contract.Assert(value != null, ""Precondition failed: value != null""); 
			               P = value;
			           }
			       }");
		}

		[Test]
		public void AssertEnsuresTest()
		{
			TestEcs(@"[ensuresAssert(comp(lo, hi) <= 0)]
				public static bool SortPair<T>(ref T lo, ref T hi, Comparison<T> comp) {
					if (comp(lo, hi) > 0) {
						Swap(ref lo, ref hi);
						return true;
					}
					return false;
				}", @"
				public static bool SortPair<T>(ref T lo, ref T hi, Comparison<T> comp) {
					if (comp(lo, hi) > 0) {
						Swap(ref lo, ref hi);
						{	var return_value = true;
							System.Diagnostics.Debug.Assert(comp(lo, hi) <= 0, ""Postcondition failed: comp(lo, hi) <= 0"");
							return return_value;
						}
					}
					{	var return_value = false;
						System.Diagnostics.Debug.Assert(comp(lo, hi) <= 0, ""Postcondition failed: comp(lo, hi) <= 0"");
						return return_value;
					}
				}");
		}

		[Test]
		public void EnsuresOnThrowTest()
		{
			TestEcs(@"[ensuresOnThrow(Member != null)] 
			        public void Test() { 
			            Member = Member ?? new Whatever();
			            throw new SomeException();
			        }",
			      @"public void Test() { 
			            try {
			                Member = Member ?? new Whatever();
			                throw new SomeException();
			            } catch (Exception __exception__) {
			                if (!(Member != null)) throw new InvalidOperationException(""Postcondition failed after throwing an exception: Member != null"", __exception__);
			                throw;
			            }
			        }");
			TestEcs(@"[ensuresOnThrow<SomeException>(Member != null)] 
			        public void Test() { 
			            Member = Member ?? new Whatever();
			            throw new SomeException();
			        }",
			      @"public void Test() { 
			            try {
			                Member = Member ?? new Whatever();
			                throw new SomeException();
			            } catch (SomeException __exception__) {
			                if (!(Member != null)) throw new InvalidOperationException(""Postcondition failed after throwing an exception: Member != null"", __exception__);
			                throw;
			            }
			        }");
		}

		[Test]
		public void HaveContractRewriterTest()
		{
			TestEcs(@"#set #haveContractRewriter;
			          public static string Double(notnull string x) { 
			            return x + x;
			          }",
			        @"public static string Double(string x) {
			            Contract.Requires(x != null); 
			            return x + x;
			          }");
			TestEcs(@"#set #haveContractRewriter;
			          [ensures(_ >= 0)]
			          public static int Square(int x) { 
			            return x * x;
			          }",
			        @"public static int Square(int x) {
			            Contract.Ensures(Contract.Result<int>() >= 0); 
			            return x * x;
			          }");
			TestEcs(@"#set #haveContractRewriter;
			        [ensuresOnThrow(Member != null)] 
			        public void Test() { 
			            Member = Member ?? new Whatever();
			            throw new SomeException();
			        }",
			      @"public void Test() { 
			            Contract.EnsuresOnThrow(Member != null);
			            Member = Member ?? new Whatever();
			            throw new SomeException();
			        }");
			TestEcs(@"#set #haveContractRewriter;
			        [ensuresOnThrow<SomeException>(Member != null)] 
			        public void Test() { 
			            Member = Member ?? new Whatever();
			            throw new SomeException();
			        }",
			      @"public void Test() { 
			            Contract.EnsuresOnThrow<SomeException>(Member != null);
			            Member = Member ?? new Whatever();
			            throw new SomeException();
			        }");
		}

		[Test]
		public void ContractOnLambdaTest()
		{
			TestEcs(@"
				public static Func<int,int> Multiplier() {
					return (int x, [requires(_ > 0)] int y) => x * y;
				}", @"
				public static Func<int,int> Multiplier() {
					return (int x, int y) => { 
						Contract.Assert(y > 0, ""Precondition failed: y > 0""); 
						return x * y;
					};
				}");
			TestEcs(@"
				public static Func<int,int> Decrementor() {
					return ([requires(_ > 0)] num) => num - 1;
				}", @"
				public static Func<int,int> Decrementor() {
					return (num) => { 
						Contract.Assert(num > 0, ""Precondition failed: num > 0""); 
						return num - 1;
					};
				}");
			TestEcs(@"
				public static Func<int,int> Squarer() {
					return ([ensures(_ >= 0)] delegate (int num) { return num * num; });
				}", @"
				public static Func<int,int> Squarer() {
					return (delegate (int num) { 
						{	var return_value = num * num;
							Contract.Assert(return_value >= 0, ""Postcondition failed: return_value >= 0""); 
							return return_value;
						}
					});
				}");
			TestEcs(@"#set #haveContractRewriter;
				public static Func<int,int> Squarer() {
					return ([requires(num >= 0)] delegate (int num) { return num * num; });
				}", @"
				public static Func<int,int> Squarer() {
					return (delegate (int num) { 
						Contract.Requires(num >= 0); 
						return num * num;
					});
				}");
		}
	}
}
