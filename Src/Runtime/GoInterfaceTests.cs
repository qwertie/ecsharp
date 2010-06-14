using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;
using System.Runtime.InteropServices;

namespace Loyc.Runtime
{
	[TestFixture]
	public class GoInterfaceTests
	{
		public class SimpleClass : IDifferentName
		{
			public int Mutate(int x) { return x * x; }
		}
		public struct SimpleStruct
		{
			public SimpleStruct(int multiplier) { _x = multiplier; }
			int _x;
			public int Mutate(int x) { return x * _x; }
		}
		public interface ISimple
		{
			int Mutate(int x);
		}
		public interface IDifferentName
		{
			int Mutate(int x);
		}
		public abstract class SimpleBase
		{
			public abstract int Mutate(int x);
		}

		[Test]
		public void TestTheBasics()
		{
			ISimple class1 = GoInterface<ISimple, SimpleClass>.From(new SimpleClass());
			ISimple iface1 = GoInterface<ISimple, IDifferentName>.From(new SimpleClass());
			ISimple struct1 = GoInterface<ISimple, SimpleStruct>.From(new SimpleStruct(10));

			Assert.AreEqual(25, class1.Mutate(5));
			Assert.AreEqual(25, iface1.Mutate(5));
			Assert.AreEqual(70, struct1.Mutate(7));

			SimpleBase class2 = GoInterface<SimpleBase, SimpleClass>.From(new SimpleClass());
			SimpleBase iface2 = GoInterface<SimpleBase, IDifferentName>.From(new SimpleClass());
			SimpleBase struct2 = GoInterface<SimpleBase, SimpleStruct>.From(new SimpleStruct(10));

			for (int i = 0; i < 10; i++)
			{
				Assert.AreEqual(class1.Mutate(i), class2.Mutate(i));
				Assert.AreEqual(iface1.Mutate(i), iface2.Mutate(i));
				Assert.AreEqual(struct1.Mutate(i), struct2.Mutate(i));
			}
		}


		public class AdaptationTest
		{
			public void Passthrough(int i, double d, object o, string s) { }
			public bool Passthrough(out int i, out double d, out object o, out string s) { i = 1; d = 2; o = 3; s = "4"; return true; }

			public bool Enlarge2int(int i, bool signed) { return signed ? i == -1 : i == 0xFFFF; }
			public bool Enlarge2uint(uint i) { return i == 0xFF || i == 0xFFFF; }
			public bool Enlarge2long(long i, bool signed) { return signed ? i == -1 : (i == 0xFFFF || i == 0xFFFFFFFF); }
			public bool Enlarge2float(float one) { return one == 1.0f; }
			public bool Enlarge2double(double one) { return one == 1.0f; }
			public bool Box(object one) { return Convert.ToDouble(one) == 1.0; }
			public bool Contravariant(object one) { return one.ToString() == "1"; }
			public bool Contravariant(IDisposable obj) { return obj != null; }
			public bool Invariant(string x) { return x == "1"; }
			public bool Invariant2(ref string x) { return x == "1"; }
			public bool True() { return true; }
			public void Covariant(out ushort x) { x = 1; }
			public void Covariant(out MemoryStream obj) { obj = new MemoryStream(); }
			public int CovariantReturn() { return 1; }
			public bool CovariantReturn2() { return true; }
			public string CovariantReturn3() { return "true"; }
			public sbyte CovariantReturn4() { return 1; }
			public bool DroppedParam() { return true; }
			public bool DroppedParam2(out int x) { x = 1; return true; }
			public bool DroppedParam3(out object x) { x = null; return true; }
			public bool DefaultParam([DefaultParameterValue(1.0)] double x) { return x == 1.0; }
		}
		public abstract class AdaptationTestBase
		{
			public abstract void Passthrough(int i, double d, object o, string s);
			public abstract bool Passthrough(out int i, out double d, out object o, out string s);

			public abstract bool Enlarge2int(short i, bool @true);
			public abstract bool Enlarge2int(ushort i, bool @false);
			public abstract bool Enlarge2uint(byte i);
			public abstract bool Enlarge2uint(ushort i);
			public abstract bool Enlarge2long(int i, bool @true);
			public abstract bool Enlarge2long(ushort i, bool @false);
			public abstract bool Enlarge2long(uint i, bool @false);
			public abstract bool Enlarge2float(ushort one);
			public abstract bool Enlarge2double(int one);
			public abstract bool Box(int one);
			public abstract bool Box(double one);
			public abstract bool Contravariant(string one);
			public abstract bool Contravariant(MemoryStream obj);
			public abstract bool Invariant(ref string x);
			public abstract bool Invariant2(string x);
			[GoAlias("True")]
			public abstract bool Renamed();
			public abstract void Covariant(out int x);
			public abstract void Covariant(out IDisposable obj);
			public abstract void CovariantReturn();
			public abstract object CovariantReturn2();
			public abstract object CovariantReturn3();
			public abstract float CovariantReturn4();
			public abstract bool DroppedParam(int x);
			public abstract bool DroppedParam2();
			public abstract bool DroppedParam3(string x);
			public abstract bool DefaultParam();
		}

		[Test]
		public void TestAdaptation()
		{
			AdaptationTestBase t = GoInterface<AdaptationTestBase, AdaptationTest>.ForceFrom(new AdaptationTest());

			t.Passthrough(1, 2, 3, "4");
			int i; double d; object o; string s;
			Assert.That(t.Passthrough(out i, out d, out o, out s));
			Assert.AreEqual(i, 1);
			Assert.AreEqual(d, 2);
			Assert.AreEqual(o, 3);
			Assert.AreEqual(s, "4");

			Assert.That(t.Enlarge2int((short)-1, true));
			Assert.That(t.Enlarge2int((ushort)0xFFFF, false));
			Assert.That(t.Enlarge2uint((byte)0xFF));
			Assert.That(t.Enlarge2uint((ushort)0xFFFF));
			Assert.That(t.Enlarge2long(-1, true));
			Assert.That(t.Enlarge2long((ushort)0xFFFF, false));
			Assert.That(t.Enlarge2long((uint)0xFFFFFFFF, false));
			Assert.That(t.Enlarge2float(1));
			Assert.That(t.Enlarge2double(1));
			Assert.That(t.Box((int)1));
			Assert.That(t.Box(1.0));
			Assert.That(t.Contravariant("1"));
			Assert.That(t.Contravariant(new MemoryStream()));
			s = "1";
			Assert.That(t.Invariant(ref s));
			Assert.That(t.Invariant2(s));
			Assert.That(t.Renamed());
			t.Covariant(out i);
			Assert.That(i == 1);
			IDisposable idisp;
			t.Covariant(out idisp);
			Assert.That(idisp is MemoryStream);
			t.CovariantReturn();
			Assert.That(t.CovariantReturn2() is bool);
			Assert.That(t.CovariantReturn3() == null);
			Assert.AreEqual(t.CovariantReturn4(), 1.0f);
			Assert.That(t.DroppedParam(1));
			Assert.That(t.DroppedParam2());
			Assert.That(t.DroppedParam3("1"));
			Assert.That(t.DefaultParam());
		}

		public class OverloadingTest
		{
			// Note: most methods in IOverloadingTest uses "int" parameters.
			// Caller is expected to pass arguments of 1, 2, etc.
			
			// The second overload matches in these cases because interface has 2+ args
			public bool MoreMatchingArgsAreBetter(int x) { return false; }
			public bool MoreMatchingArgsAreBetter(int x, int y) { return x == 1 && y == 2; }
			public bool MoreMatchingArgsAreBetter2(int x) { return false; }
			public bool MoreMatchingArgsAreBetter2(long x, long y) { return x == 1 && y == 2; }

			// Obviously the 'short' overloads should not be called
			public bool MinimumSizeRequired(short x) { return false; }
			public bool MinimumSizeRequired(long x) { return x == 1; }
			public bool MinimumSizeRequired2(ushort x) { return false; }
			public bool MinimumSizeRequired2(long x) { return x == 1; }

			// Value types are boxed to object
			public bool Boxing(object x) { return x is int && (int)x == 1; }
			public bool Boxing(string x) { return false; }
			public bool Boxing(byte x) { return false; }

			// Interface just takes an int; ensure nothing else distracts our code
			public bool ManyOverloads(int x) { return x == 1; }
			public bool ManyOverloads(int x, out int y) { y = 2; return false; }
			public bool ManyOverloads(uint x, out string y) { y = "2"; return false; }
			public bool ManyOverloads(long x) { return false; }
			public bool ManyOverloads(object x) { return false; }
			public bool ManyOverloads(ref int x) { return false; }
			public bool ManyOverloads(double x) { return false; }
			public bool ManyOverloads(char x) { return false; }

			// Signed argument => signed overload
			public bool SignedIsBetter(int x) { return x == 1; }
			public bool SignedIsBetter(uint x) { return false; }
			public bool SignedIsBetter2(uint x) { return false; }
			public bool SignedIsBetter2(long x) { return x == 1; }

			// Unsigned argument => unsigned overload
			public bool UnsignedIsBetter(int x) { return false; }
			public bool UnsignedIsBetter(uint x) { return x == 1; }
			public bool UnsignedIsBetter2(ulong x) { return x == 1; }
			public bool UnsignedIsBetter2(int x) { return false; }
			
			// Implicit int or float conversion is better than boxing
			public bool IntIsBetter(long x) { return x == 1; }
			public bool IntIsBetter(object x) { return false; }
			public bool FloatIsBetter(double x) { return x == 1.0; }
			public bool FloatIsBetter(object x) { return false; }
			public bool FloatIsBetter2(object x) { return false; }
			public bool FloatIsBetter2(double x) { return x == 1.0; }

			// "out" args in target are not required in the interface, and input 
			// arguments in the interface can be omitted when calling the target,
			// provided that missing arguments are at the end of the argument 
			// list. A missing input and output argument can even occur at the 
			// same time.
			public bool MissingOutput(int x, int y) { return false; }
			public bool MissingOutput(int x, out int y, out int z) { y = 2; z = 3; return x == 1; }
			public bool MissingOutput2(out int x) { x = 1; return true; }
			public bool MissingOutput2(int x) { return false; }
			public bool MissingInput(int x) { return false; }
			public bool MissingInput(int x, int y) { return x == 1 && y == 2; }
			public bool MissingInput2() { return true; }
			public bool MissingInputAndOutput(int x, out int y) { y = 2; return x == 1; }
			public bool MissingInputAndOutput(object x, out int y) { y = 2; return false; }

			// Optional parameters are supplied automatically, and they can occur 
			// at the same time as an input parameter in the interface is missing 
			// from the target.
			public bool OptionalParam(int x) { return false; }
			public bool OptionalParam(int x, int y, [DefaultParameterValue(3)] int z) { return x == 1 && y == 2 && z == 3; }
			public bool OptionalParam2(int x) { return false; }
			public bool OptionalParam2(int x, int y, [DefaultParameterValue("3")] string z) { return x == 1 && y == 2 && z == "3"; }

			// The "ref" status of an argument can mismatch, but GoInterface should
			// select an overload without a mismatch whenever possible. However, if
			// the ref-matched overload is incompatible, the ref-unmatched overload 
			// should be chosen instead (as in cases 4 and 5).
			public bool RefMismatch(ref int x) { return false; }
			public bool RefMismatch(int x) { return x == 1; }
			public bool RefMismatch2(ref int x) { return false; }
			public bool RefMismatch2(int x, [DefaultParameterValue(2)] int y) { return x == 1 && y == 2; }
			public bool RefMismatch3(ref int x) { return x == 1; }
			public bool RefMismatch3(int x) { return false; }
			public bool RefMismatch4(ref long x) { return false; }
			public bool RefMismatch4(ref short x) { return false; }
			public bool RefMismatch4(int x) { return true; }
			public bool RefMismatch5(long x) { return true; }
			public bool RefMismatch5(ref short x) { return false; }
			public bool RefMismatch5(ref long x) { return false; }

			// When there are multiple matching overloads, GoInterface should pick 
			// the one that is "nearest" in the type hierarchy.
			public bool Contravariance(object x) { return x.ToString() == "1"; }
			public bool Contravariance2(Stream s) { return true; }
			public bool Contravariance2(IDisposable s) { return false; }
			public bool Contravariance2(object s) { return false; }
			public bool Contravariance3(IDisposable s) { return true; }
			public bool Contravariance3(object s) { return false; }

			// For out parameters, the choice of which parameter type is "better" 
			// works in reverse.
			public bool Covariance(out short x) { x = 1; return true; }
			public bool Covariance(out long x) { x = 1; return false; }
			public bool Covariance2(out Stream x) { x = null; return true; }
			public bool Covariance2(out MemoryStream x) { x = null; return false; }

			// Return-type covariance works very much like 'out' covariance, with 
			// the added caveat that the in interface, the return value can be 
			// 'void'. Covariance is 'better' than a missing parameter.
			public int ReturnCovariance() { return 1; } // preferred
			public object ReturnCovariance(int x) { return null; }
			public byte ReturnCovariance2() { return 1; } // preferred
			public int ReturnCovariance2(int x) { return 0; }
			public int ReturnCovariance3(out bool correct) { correct = true; return 1; } // preferred
			public void ReturnCovariance3(out bool correct, out int x) { x = 1; correct = false; }

			// GoInterface can disambiguate based on return type alone.
			public bool ChooseTheMatchingReturn(int x, out int y) { y = 2; return x == 1; }
			public string ChooseTheMatchingReturn(int x) { return null; }

			protected bool IgnoreNonPublic(int x) { return false; }
			public bool IgnoreNonPublic(object x) { return x is int && (int)x == 1; }
		}
		public interface IOverloadingTest
		{
			bool MoreMatchingArgsAreBetter(int x, int y);
			bool MoreMatchingArgsAreBetter2(int x, int y, int z);
			bool MinimumSizeRequired(int x);
			bool MinimumSizeRequired2(uint x);
			bool SignedIsBetter(int x);
			bool SignedIsBetter2(int x);
			bool UnsignedIsBetter(uint x);
			bool UnsignedIsBetter2(uint x);
			bool IntIsBetter(int x);
			bool FloatIsBetter(int x);
			bool FloatIsBetter2(float x);
			bool MissingOutput(int x);
			bool MissingOutput2();
			bool MissingInput(int x, int y, int z);
			bool MissingInput2(int x, int y);
			bool MissingInputAndOutput(int x, int y, int z);
			bool OptionalParam(int x, int y);
			bool OptionalParam2(int x, int y, int z);
			bool RefMismatch(int x);
			bool RefMismatch2(int x);
			bool RefMismatch3(ref int x);
			bool RefMismatch4(ref int x);
			bool RefMismatch5(int x);
			bool Contravariance(string x);
			bool Contravariance2(MemoryStream x);
			bool Contravariance3(MemoryStream x);
			bool Covariance(out int x);
			bool Covariance2(out object x);
			object ReturnCovariance();
			float ReturnCovariance2();
			void ReturnCovariance3(out bool correct);
			bool ChooseTheMatchingReturn(int x);
			bool IgnoreNonPublic(int x);
		}

		[Test]
		public void TestOverloading()
		{
			bool correct;
			int x = 1;
			object o;
			IOverloadingTest i = GoInterface<IOverloadingTest>.ForceFrom(new OverloadingTest());
			Assert.That(i.MoreMatchingArgsAreBetter(1, 2));
			Assert.That(i.MoreMatchingArgsAreBetter2(1, 2, 3));
			Assert.That(i.MinimumSizeRequired(1));
			Assert.That(i.MinimumSizeRequired2(1));
			Assert.That(i.SignedIsBetter(1));
			Assert.That(i.SignedIsBetter2(1));
			Assert.That(i.UnsignedIsBetter(1));
			Assert.That(i.UnsignedIsBetter2(1));
			Assert.That(i.IntIsBetter(1));
			Assert.That(i.FloatIsBetter(1));
			Assert.That(i.FloatIsBetter2(1.0f));
			Assert.That(i.MissingOutput(1));
			Assert.That(i.MissingOutput2());
			Assert.That(i.MissingInput(1, 2, 3));
			Assert.That(i.MissingInput2(1, 2));
			Assert.That(i.MissingInputAndOutput(1, 2, 3));
			Assert.That(i.OptionalParam(1, 2));
			Assert.That(i.OptionalParam2(1, 2, 3));
			Assert.That(i.RefMismatch(1));
			Assert.That(i.RefMismatch2(1));
			Assert.That(i.RefMismatch3(ref x));
			Assert.That(i.RefMismatch4(ref x));
			Assert.That(i.RefMismatch5(1));
			Assert.That(i.Contravariance("1"));
			Assert.That(i.Contravariance2(new MemoryStream()));
			Assert.That(i.Contravariance3(new MemoryStream()));
			Assert.That(i.Covariance(out x));
			Assert.That(i.Covariance2(out o));
			Assert.That(i.ReturnCovariance() is int && (int)i.ReturnCovariance() == 1);
			Assert.That(i.ReturnCovariance2() == 1.0f);
			i.ReturnCovariance3(out correct);
			Assert.That(correct);
			Assert.That(i.ChooseTheMatchingReturn(1));
			Assert.That(i.IgnoreNonPublic(1));
		}
	}
}
