using Loyc.MiniTest;
using Loyc.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace LeMP.Tests
{
	[TestFixture]
	public class TestCompileTimeMacros : MacroTesterBase
	{
		// TODO: Test error reporting

		[Test]
		public void BasicTest()
		{
			TestEcs(@"compileTime {
					int X = 24;
				}
				const string Y = precompute((++X).ToString());",
				@"const string Y = ""25"";");
			TestEcs(@"compileTime {
					using System.Text;
					int Y = int.Parse(new StringBuilder(""25"").ToString());
				}
				const string Z = precompute((++Y).ToString());",
				@"const string Z = ""26"";");
		}

		[Test]
		public void DualPurposeCodeBlock()
		{
			TestEcs(@"
				compileTimeAndRuntime {
					namespace Company {
						public partial class Order {
							public string ProductCode { get; set; }
							public string ProductName { get; set; }
						}
					}
				}
				compileTime {
					using System.Linq;
					
					public static string WithoutPrefix(string s, string prefix) =>
						s.StartsWith(prefix) ? s.Substring(prefix.Length) : s;

					// In real life you might read a file with includeFileText(""FileName.csv"")
					// and parse it at compile time, to produce a list or dictionary of objects.
					Order[] CannedOrders = new[] { 
						new Order { ProductName = ""Tire"", ProductCode = ""#1234"" },
						new Order { ProductName = ""XL Tire"", ProductCode = ""#1236"" },
						new Order { ProductName = ""Black Rim"", ProductCode = ""#1238"" },
						new Order { ProductName = ""Red Rim"", ProductCode = ""#1240"" },
					};
				}
				namespace Company {
					public partial class Order {
						precompute(CannedOrders
							.Select(o => quote {
							public static Order $(LNode.Id(""New"" + o.ProductName.Replace("" "", """")))() => 
								new Order {
									ProductName = $(LNode.Literal(o.ProductName)),
									ProductCode = $(LNode.Literal(WithoutPrefix(o.ProductCode, ""#""))),
								};
						}));
					}
				}",
				@"
					namespace Company {
						public partial class Order {
							public string ProductCode { get; set; }
							public string ProductName { get; set; }
						}
					}
					namespace Company {
						public partial class Order {
							public static Order NewTire() =>
								new Order {
									ProductName = ""Tire"",
									ProductCode = ""1234""
								};
							public static Order NewXLTire() =>
								new Order {
									ProductName = ""XL Tire"",
									ProductCode = ""1236""
								};
							public static Order NewBlackRim() =>
								new Order {
									ProductName = ""Black Rim"",
									ProductCode = ""1238""
								};
							public static Order NewRedRim() =>
								new Order {
									ProductName = ""Red Rim"",
									ProductCode = ""1240""
								};
						}
					}
				");
		}

		[Test]
		public void PrecomputePreservesTrivia()
		{
			TestEcs(@"const string Y = /* a devilish computation */ precompute(666.ToString());",
				@"const string Y = /* a devilish computation */ ""666"";");
		}

		[Test]
		public void CompileTimeMacrosAreRegistered()
		{
			TestEcs(@"compileTime
				{
					[LexicalMacro(""syntax"", ""description"", ""#var"", Mode = MacroMode.Passive)]
					public static LNode DumbMacro(LNode node, IMacroContext context)
					{
						matchCode(node) {
							case float $x:
								if (x.IsId) // make sure it's not already an assignment
									return quote(float $x = 0.0);
						}
						return null;
					}
				}
				int i;
				float f;",
				@"int i;
				float f = 0d;");

			// Use different syntax to refer to LexicalMacro (this is detected, awkwardly)
			TestEcs(@"compileTime
				{
					[LeMP.LexicalMacroAttribute("""", """")]
					public static LNode poop(LNode node, IMacroContext context)
					{
						return node.WithTarget((Symbol)""POOP"");
					}
					[global::LeMP.LexicalMacro("""", """", ""#var"", Mode = MacroMode.Passive)]
					public static LNode DumbMacro2(LNode node, IMacroContext context)
					{
						matchCode(node) {
							case Int32 $(..v):
								return quote(int $(..v));
						}
						return null;
					}
				}
				Int32 i = poop(123);
				Int32 x, y = 0;",
				@"int i = POOP(123);
				int x, y = 0;");
		}

#if !NoReflectionEmit // Oops, can't generate assemblies in .NET Standard

		[Test]
		public void LoadReferenceTest()
		{
			string folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			AssemblyBuilder ab = GenerateSimpleAssembly(folder, "CompileTimeTest");
			string dllFile = ab.GetName().Name + ".dll";
			try {
				ab.Save(dllFile);
			} catch(Exception e) {
				// Save fails if the test is run a second time without restarting
			}
			string path = Path.Combine(folder, dllFile);

			// Load reference with absolute path
			// Note: weird string formatting is intentional to avoid errors #if NoReflectionEmit
			TestEcs("compileTime {\n" +
				$@"	#r {path}
					string X = new Class(123.ToString()).Field;
				}}
				const string X = precompute(X);",
				@"const string X = ""123"";");

			// Load reference with relative path (#inputFolder is normally set automatically)
			// Note: weird string formatting is intentional to avoid errors #if NoReflectionEmit
			TestEcs("compileTime {\n" +
				$@"	#set #inputFolder = @""{folder}"";" + "\n" +
				$@"	#r {dllFile}
					string X = new Class(4321.ToString()).Field;
				}}
				const string X = precompute(X);",
				@"const string X = ""4321"";");
		}

		static AssemblyBuilder GenerateSimpleAssembly(string folder, string nameStem)
		{
			AssemblyName name = new AssemblyName(nameStem);
			AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save, folder);
			ModuleBuilder mb = ab.DefineDynamicModule(name.Name, name.Name + ".dll");
			TypeBuilder tb = mb.DefineType("Class", TypeAttributes.Public);
			FieldBuilder field = tb.DefineField("Field", typeof(string), FieldAttributes.Public);

			// Constructor needs to call the constructor of the parent class, or another constructor in the same class
			ConstructorBuilder constructor = tb.DefineConstructor(
				MethodAttributes.Public, CallingConventions.Standard | CallingConventions.HasThis, new[] { typeof(string) });
			ILGenerator il = constructor.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Call, typeof(object).GetConstructor(Array.Empty<Type>())); // call parent's constructor
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Ldarg_1); // our argument
			il.Emit(OpCodes.Stfld, field); // store argument in this.Field
			il.Emit(OpCodes.Ret);

			tb.CreateType();

			return ab;
		}

		#endif
	}
}
