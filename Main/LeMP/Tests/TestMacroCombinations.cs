using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace LeMP.Tests
{
	[TestFixture]
	public class TestMacroCombinations : MacroTesterBase
	{
		[Test]
		public void TestThisConstructorAndCreateMember()
		{
			TestEcs(@"
				class Holder<T> {
					public this(public readonly T Value) {}
				}
			", @"
				class Holder<T> {
					public readonly T Value;
					public Holder(T value) { Value = value; }
				}
			");
		}


		[Test] 
		public void MixingFeatures()
		{
			// Check that we can mix code contracts with other features that affect methods & properties...
			// requires + method forwarding
			TestEcs("[requires(x >= 0)] static double Sqrt(double x) ==> Math.Sqrt;",
			       @"static double Sqrt(double x) { 
			             Contract.Assert(x >= 0, ""Precondition failed: x >= 0""); return Math.Sqrt(x);
			         }");
			// notnull + backing field
			TestEcs("[field _foo] public List<int> Foo { get; [notnull] set; }",
			  @"List<int> _foo; public List<int> Foo { 
			      get { return _foo; } 
			      set { Contract.Assert(value != null, ""Precondition failed: value != null""); _foo = value; } 
			    }");
			// ensures + requires + backing field
			TestEcs("[field _foo] public List<int> Foo { [ensures(_ != null)] get; [requires(_ != null)] set; }",
			  @"List<int> _foo; 
			    public List<int> Foo { 
			        get { { var return_value = _foo; Contract.Assert(return_value != null, ""Postcondition failed: return_value != null""); return return_value; } }
			        set { Contract.Assert(value != null, ""Precondition failed: value != null""); _foo = value; }
			    }");
			// ensures by itself
			string result;
			TestEcs(@"
				public static Node Root { 
					[ensures(_ != null)] get { return _root; }
				}",
			  result = @"
			    public static Node Root { 
			      get { {
			          var return_value = _root; 
			          Contract.Assert(return_value != null, ""Postcondition failed: return_value != null"");
			          return return_value;
			      } }
			    }");
			// // ensures + backing field
			TestEcs(@"[field _root]
				[ensures(_ != null)] 
				public static Node Root { get; }",
				"Node _root; " + result);
			// requires + set member
			TestEcs(@"void Foo(set notnull string Name, [requires(IsValidZip(_))] set string Zip) {}",
				@"
				void Foo(string name, string zip) {
					Contract.Assert(name != null, ""Precondition failed: name != null"");
					Contract.Assert(IsValidZip(zip), ""Precondition failed: IsValidZip(zip)"");
					Name = name;
					Zip = zip;
				}
				");
		}

		[Test(Fails = "Haven't figured out how to support this")] 
		public void MixingFeatures2()
		{
			// requires + set member + create member
			TestEcs(@"void Foo(public notnull string Name, [requires(IsValidZip(_))] set string Zip) {}",
				@"public string Name;
				void Foo(string name, string zip) {
					Contract.Assert(name != null, ""Precondition failed: name != null"");
					Contract.Assert(IsValidZip(zip), ""Precondition failed: IsValidZip(zip)"");
					Name = name;
					Zip = zip;
				}
				");
		}
	}
}
