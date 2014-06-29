using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections;
using S = Loyc.Syntax.CodeSymbols;

namespace LeMP
{
	[ContainsMacros]
	public partial class StandardMacros
	{
		[ThreadStatic]
		internal static int NextTempCounter = 0; // next tmp variable
		internal static Symbol NextTempName() { return GSymbol.Get("tmp_" + NextTempCounter++); }

		static LNodeFactory F = new LNodeFactory(new EmptySourceFile("StandardMacros.cs"));

		static LNode Reject(IMessageSink sink, LNode at, string msg)
		{
			sink.Write(Severity.Error, at, msg);
			return null;
		}

		// quote @{ Hope.For(777, $cash) } => F.Call(F.Dot(F.Id("Hope"), F.Id("For")), F.Literal(777), cash)
		// declare_symbols @@Foo @@Bar {...} (braces optional, invoke replace_code)
		// replace_code (IntSet.Parse($s) \with ParseSet($s), IntSet \with HashSet!int) {...}
		// replace_code_after ($X($Y, -1) \with $X($Y, EOF)) {...}
		// with Foo.Bar {...} => replace_code (.($x) \with Foo.Bar.$x) {...}
		// include_here "../Util/Util.les"
		// specialize $T \in (int, float, double) { def Square!$T(T x) { x*x; }; class Foo!($T,U) {}; }
		// run_macros (specialize $T \in (int, float)) (replace Xyz \with Zyx) { ... }
		// Bob.Hair?.Brush()
		// cons Point(public field X::int, public field Y::int) { }
		// def SetX(public X::int) { }
		// prop X::int (public field _x) { get; set; };
		// def DoSomething(required X::string) { };
		// public override def ToString()::string ==> _obj;
		// forward_to _obj { def ToString()::string; def GetHashCode()::int; };
		// foo ??= Foo();
		// x - 0.5;
		// xin(1, 2, 3);
		// $"The value is {Value,-10:C}." => string.Format("The value is {0,-10:C}", Value)
		// save_and_restore _foo { Foo(_foo = true); } => var tmp17 = _foo; try { Foo(_foo = true); } finally { _foo = tmp17; }

		#region concat_id, ##

		[SimpleMacro(@"a `##` b", "Concatenates identifiers and/or literals to produce an identifier", "##", "concat_id")]
		public static LNode concat(LNode node, IMessageSink sink)
		{
			var args = node.Args;
			if (args.Count == 0)
				return null;
			if (args.Slice(0, args.Count - 1).Any(n => n.IsCall))
				return Reject(sink, node, "All arguments to ##() or concat() must be identifiers or literals (except the last one)");

			StringBuilder sb = new StringBuilder();
			foreach (LNode arg in args) 
			{
				object name = arg.IsLiteral ? arg.Value ?? "null" : arg.Name;
				sb.Append(name);
			}
			Symbol combined = GSymbol.Get(sb.ToString());
			if (args.Last.IsCall)
				return node.WithTarget(combined);
			else
				return LNode.Id(combined, node);
		}

		#endregion

		[SimpleMacro("A ??= B", "Assign A = B only when A is null. Caution: currently, A is evaluated twice.", "??=")]
		public static LNode NullCoalesceSet(LNode node, IMessageSink sink)
		{
			var a = node.Args;
			if (a.Count != 2)
				return null;
			LNode x = a[0], y = a[1];
			// This is INCOMPLETE! But it'll suffice temporarily.
			// #??=(x, y) => x = x ?? y => #=(x, #??(x, y))
			return F.Set(x, F.Call(S.NullCoalesce, x, y));
		}

		[SimpleMacro("A=:B; A:::B", "Declare a variable B and set it to the value A. Typically used within a larger expression, "+
			"e.g. if (int.Parse(text):::num > 0) positives += num;", "=:", ":::")]
		public static LNode QuickBind(LNode node, IMessageSink sink)
		{
			var a = node.Args;
			if (a.Count == 2) {
				LNode A = a[0], B = a[1];
				return F.Vars(F._Missing, F.Set(B, A));
			}
			return null;
		}
		
		[SimpleMacro("A := B", "Declare a variable A and set it to the value of B. Equivalent to \"var A = B\".")]
		public static LNode ColonEquals(LNode node, IMessageSink sink)
		{
			var a = node.Args;
			if (a.Count == 2) {
				LNode A = a[0], B = a[1];
				return F.Vars(F._Missing, F.Set(A, B));
			}
			return null;
		}
	}
}
