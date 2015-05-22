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
	public partial class StandardMacros
	{
		[ThreadStatic]
		static List<Pair<LNode, LNode>> TupleMakers;
		[ThreadStatic]
		static Pair<LNode, LNode> DefaultTupleMaker;

		static void MaybeInitTupleMakers() {
			if (TupleMakers == null) {
				DefaultTupleMaker = Pair.Create(F.Id("Tuple"), F.Dot("Tuple", "Create"));
				TupleMakers = new List<Pair<LNode, LNode>>() {
					Pair.Create<LNode,LNode>(null, null),               // ignore 0 args
					DefaultTupleMaker,                                  // 1 arg
					Pair.Create(F.Id("Pair"), F.Dot("Pair", "Create")), // 2 args
				};
			}
		}

		[LexicalMacro("use_default_tuple_types()", "")]
		public static LNode use_default_tuple_types(LNode node, IMessageSink sink)
		{
			if (node.ArgCount != 0)
				return null;
			TupleMakers = null;
			return F.Call(S.Splice);
		}

		// Flaw: this is nondeterministic... given multiple files, set_tuple_type() in one file may or may not affect other files.
		[LexicalMacro("set_tuple_type(BareName); set_tuple_type(TupleSize, BareName); set_tuple_type(TupleSize, BareName, Factory.Method)",
			"Set type and creation method for tuples, for a specific size of tuple or for all sizes at once")]
		public static LNode set_tuple_type(LNode node, IMessageSink sink) {
			MaybeInitTupleMakers();

			int? size = node.Args[0, F._Missing].Value as int?;
			var rest = node.Slice(size != null ? 1 : 0);
			if (!MathEx.IsInRange(rest.Count, 1, 2)) 
				return Reject(sink, node, "Incorrect number of arguments");
			var tupleCfg = Pair.Create(rest[0], rest.TryGet(1, null));
			if (tupleCfg.A.Value == null)
				tupleCfg.A = null; // Makes us ignore tuples of this size
			if (tupleCfg.B == null && tupleCfg.A != null)
				tupleCfg.B = F.Dot(tupleCfg.A, F.Id("Create"));

			if (size == null) {
				TupleMakers.Resize(1);
				DefaultTupleMaker = tupleCfg;
			} else {
				while (TupleMakers.Count <= size.Value)
					TupleMakers.Add(DefaultTupleMaker);
				TupleMakers[size.Value] = tupleCfg;
			}
			return F.Call(S.Splice);
		}

		[LexicalMacro("#<x, y, ...>", "Represents a tuple type", "#of")]
		public static LNode TupleType(LNode node, IMessageSink sink)
		{
			var stem = node.Args[0, F._Missing];
			if (stem.IsId && (stem.Name == S.List || stem.Name == S.Tuple)) {
				MaybeInitTupleMakers();
				var bareType = TupleMakers.TryGet(node.Args.Count - 1, new Pair<LNode, LNode>()).A;
				if (bareType == null)
					bareType = DefaultTupleMaker.A;
				if (bareType != null)
					return node.WithArgChanged(0, bareType);
			}
			return null;
		}

		[LexicalMacro("(x,); (x, y, ...)", "Create a tuple", "#tuple")]
		public static LNode Tuple(LNode node, IMessageSink sink)
		{
			// TODO: consider supporting .[a, b] (and !(a, b)) as syntax for @``<a, b> 
			// which will refer to a tuple type.
			if (node.IsCall) {
				MaybeInitTupleMakers();
				LNode method = (node.ArgCount < TupleMakers.Count ? TupleMakers[node.ArgCount] : DefaultTupleMaker).B;
				if (method != null)
					return node.WithTarget(method);
			}
			return null;
		}

		// In EC# we should support cases like "if (Foo[(a, b) = expr]) {...}"
		// This macro targets plain C# where that is not possible.
		[LexicalMacro("(a, b, etc) = expr;", "Assign a = expr.Item1, b = expr.Item2, etc.", "=")]
		public static LNode UnpackTuple(LNode node, IMessageSink sink)
		{
			var a = node.Args;
			if (a.Count == 2 && a[0].CallsMin(S.Tuple, 1)) {
				var stmts = new RWList<LNode>();
				var tuple = a[0].Args;
				var rhs = a[1];
				bool needTemp = rhs.IsCall || !char.IsLower(rhs.Name.Name.TryGet(0, '\0'));
				if (needTemp) {
					LNode tmp = F.Id(NextTempName());
					stmts.Add(F.Var(F._Missing, tmp.Name, rhs));
					rhs = tmp;
				}
				for (int i = 0; i < tuple.Count; i++)
					stmts.Add(F.Call(S.Assign, tuple[i], F.Dot(rhs, F.Id(GSymbol.Get("Item" + (i + 1))))));
				return F.Call(S.Splice, stmts.ToRVList());
			}
			return null;
		}
	}
}
