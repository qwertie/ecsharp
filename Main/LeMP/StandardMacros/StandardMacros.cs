using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections;
using S = Loyc.Syntax.CodeSymbols;
using Loyc.Math;

namespace LeMP
{
	/// <summary>
	/// Standard macros, such as unroll() and replace() that can work in all Loyc languages.
	/// Also includes macros designed to convert EC# feature to C# (e.g. tuples).
	/// </summary>
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

		// Used to avoid evaluating `value` more than once by creating a 
		// declaration in `output` of a temporary variable to hold the value. 
		// If `value` looks simple (according to LooksLikeSimpleValue), this 
		// fn returns value and leaves output unchanged.
		protected static LNode MaybeAddTempVarDecl(LNode value, RWList<LNode> output)
		{
			if (!LooksLikeSimpleValue(value)) {
				LNode tmpId;
				output.Add(TempVarDecl(value, out tmpId));
				return tmpId;
			}
			return value;
		}
		// Decides whether a value should be placed in a temporary variable
		// if it is evaluated more than once by a macro. This returns true for 
		// non-uppercase identifiers like "x" or "_Foo", for literals like 42,
		// and for dotted expressions in which all components are non-uppercase 
		// identifiers, e.g. foo.bar.baz. Returns false for everything else.
		protected static bool LooksLikeSimpleValue(LNode value)
		{
			if (value.IsCall) {
				if (value.Calls(S.Dot)) {
					if (value.ArgCount == 1)
						value = value.Args[0];
					else if (value.ArgCount == 2) {
						if (!LooksLikeSimpleValue(value.Args[0]))
							return false;
						value = value.Args[1];
					} else
						return false;
				} else
					return false;
			}
			return !char.IsUpper(value.Name.Name.TryGet(0, '\0'));
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

		#region concat_id (##), nameof

		[LexicalMacro(@"a `##` b", "Concatenates identifiers and/or literals to produce an identifier", "##", "concat_id")]
		public static LNode concat_id(LNode node, IMessageSink sink)
		{
			var args = node.Args;
			if (args.Count == 0)
				return null;
			if (args.Slice(0, args.Count - 1).Any(n => n.IsCall))
				return Reject(sink, node, "All arguments to ##() or concat() must be identifiers or literals (except the last one)");

			RVList<LNode> attrs = node.Attrs;
			LNode arg = null;
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < args.Count; i++)
			{
				arg = args[i];
				attrs.AddRange(arg.Attrs);

				if (arg.IsLiteral)
					sb.Append(arg.Value ?? "null");
				else if (arg.IsId)
					sb.Append(arg.Name);
				else { // call
					if (i + 1 != args.Count || !arg.HasSimpleHead())
						return Reject(sink, arg, "Expected simple identifier or literal");
					sb.Append(arg.Name);
				}
			}
			Symbol combined = GSymbol.Get(sb.ToString());
			LNode result;
			if (arg.IsCall)
				result = arg.WithTarget(combined);
			else
				result = LNode.Id(combined, node);
			return result.WithAttrs(attrs);
		}

		[LexicalMacro(@"nameof(id_or_expr)", @"Converts the 'key' name component of an expression to a string (e.g. nameof(A.B<C>(D)) == ""B"")")]
		public static LNode @nameof(LNode nameof, IMacroContext context)
		{
			if (nameof.ArgCount != 1)
				return null;
			var expr = KeyNameComponentOf(nameof.Args[0]);
			return F.Literal(ParsingService.Current.Print(expr, context.Sink, ParsingService.Exprs));
		}

		/// <summary>Retrieves the "key" name component for the nameof(...) macro.</summary>
		/// <remarks>
		/// The key name component of <c>global::Foo!int.Bar!T(x)</c> (in C# notation
		/// global::Foo&lt;int>.Bar&lt;T>(x)) is <c>Bar</c>. This example tree has the 
		/// structure <c>((((global::Foo)!int).Bar)!T)(x)</c>).
		/// </remarks>
		public static LNode KeyNameComponentOf(LNode name)
		{
			// So if #of, get first arg (which cannot itself be #of), then if @`.`, get second arg.
			// If it's a call, note that we have to check for #of and @`.` BEFORE stripping off the args.
			if (name.CallsMin(S.Of, 1))
				name = name.Args[0];
			if (name.CallsMin(S.Dot, 1))
				name = name.Args.Last;
			if (name.IsCall)
				return KeyNameComponentOf(name.Target);
			return name;
		}

		[LexicalMacro(@"stringify(expr)", "Converts an expression to a string (note: original formatting is not preserved)")]
		public static LNode stringify(LNode nameof, IMacroContext context)
		{
			if (nameof.ArgCount != 1)
				return null; // reject
			return F.Literal(ParsingService.Current.Print(nameof.Args[0], context.Sink, ParsingService.Exprs));
		}
		
		#endregion

		[LexicalMacro(@"x `tree==` y", "Returns the literal true if two or more syntax trees are equal, or false if not.", "tree==")]
		public static LNode TreeEqual(LNode node, IMessageSink sink)
		{
			var a = node.Args;
			if (a.Count < 2) return null;
			
			LNode left = a[0];
			for (int i = 1; i < a.Count; i++)
				if (!left.Equals(a[i]))
					return F.Literal(G.BoxedFalse);
			return F.Literal(G.BoxedTrue);
		}

		[LexicalMacro(@"static if() {...} else {...}", "TODO. Only boolean true/false implemented now", "#if", 
			Mode = MacroMode.Passive | MacroMode.Normal)]
		public static LNode StaticIf(LNode @if, IMessageSink sink)
		{
			LNode @static;
			if ((@static = @if.AttrNamed(S.Static)) == null || !@static.IsId)
				return null;
			return static_if(@if, sink);
		}
		
		[LexicalMacro(@"static_if(cond, then, otherwise)", "TODO. Only boolean true/false implemented now",
			Mode = MacroMode.Passive | MacroMode.Normal)]
		public static LNode static_if(LNode @if, IMessageSink sink)
		{
			if (!MathEx.IsInRange(@if.ArgCount, 2, 3))
				return null;
			RVList<LNode> conds = MacroProcessor.Current.ProcessSynchronously(@if.Args[0]);
			object @bool;
			if (conds.Count == 1 && (@bool = conds[0].Value) is bool)
			{
				LNode output = (bool)@bool ? @if.Args[1] : @if.Args.TryGet(2, null) ?? F.Call(S.Splice);
				if (output.Calls(S.Braces))
					return output.WithTarget(S.Splice);
				else
					return output;
			}
			else
				return Reject(sink, @if.Args[0], "'static if' is incredibly limited right now. Currently it only supports a literal boolean or (x `tree==` y)");
		}

		[LexicalMacro("A ??= B", "Assign A = B only when A is null. Caution: currently, A is evaluated twice.", "??=")]
		public static LNode NullCoalesceSet(LNode node, IMessageSink sink)
		{
			var a = node.Args;
			if (a.Count != 2)
				return null;
			LNode x = a[0], y = a[1];
			// This is INCOMPLETE! But it'll suffice temporarily.
			// #??=(x, y) => x = x ?? y => #=(x, #??(x, y))
			return F.Assign(x, F.Call(S.NullCoalesce, x, y));
		}

		[LexicalMacro("A=:B; A:::B", "Declare a variable B and set it to the value A. Typically used within a larger expression, "+
			"e.g. if (int.Parse(text):::num > 0) positives += num;", "=:", ":::")]
		public static LNode QuickBind(LNode node, IMessageSink sink)
		{
			var a = node.Args;
			if (a.Count == 2) {
				LNode A = a[0], B = a[1];
				return F.Vars(F.Missing, F.Assign(B, A));
			}
			return null;
		}
		
		[LexicalMacro("A := B", "Declare a variable A and set it to the value of B. Equivalent to \"var A = B\".")]
		public static LNode ColonEquals(LNode node, IMessageSink sink)
		{
			var a = node.Args;
			if (a.Count == 2) {
				LNode A = a[0], B = a[1];
				return F.Vars(F.Missing, F.Assign(A, B));
			}
			return null;
		}
	}
}
