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
	public partial class ProblemMacros
	{
	}

	public partial class StandardMacros
	{
		[ThreadStatic]
		static List<LNode> TupleMakers;
		[ThreadStatic]
		static LNode DefaultTupleMaker;

		static void MaybeInitTupleMakers() {
			if (TupleMakers == null) {
				DefaultTupleMaker = F.Dot("Tuple", "Create");
				TupleMakers = new List<LNode>() {
					null,             // ignore 0 args
					DefaultTupleMaker,       // 1 arg
					F.Dot("Pair", "Create"), // 2 args
				};
			}
		}

		[SimpleMacro("use_default_tuple_makers()", "")]
		public static LNode use_default_tuple_makers(LNode node, IMessageSink sink)
		{
			if (node.ArgCount != 0)
				return null;
			TupleMakers = null;
			return F.Call(S.Splice);
		}

		// Flaw: this is nondeterministic... given multiple files, set_tuple_maker() in one file may or may not affect other files.
		[SimpleMacro("set_tuple_maker(Class.Method); set_tuple_maker(TupleSize, Class.Method)", "Set creation method for tuples")]
		public static LNode set_tuple_maker(LNode node, IMessageSink sink) {
			MaybeInitTupleMakers();
			if (node.ArgCount == 1) {
				TupleMakers.Resize(1);
				DefaultTupleMaker = node.Args[0];
				return F.Call(S.Splice);
			} else if (node.ArgCount == 2) {
				var error = "Argument 1 of 2 must be a positive integer literal representing the tuple size";
				if (!(node.Args[0].Value is int))
					return Reject(sink, node.Args[0], error);
				int size = (int)(node.Args[0].Value);
				if (size < 0)
					return Reject(sink, node.Args[0], error);
				
				while (TupleMakers.Count <= size)
					TupleMakers.Add(DefaultTupleMaker);
				var method = node.Args[1];
				TupleMakers[size] = (method.IsLiteral && method.Value == null ? null : method);
				return F.Call(S.Splice);
			} else
				return Reject(sink, node, "Expected one or two arguments");
		}

		// BLOCK this macro, as it interferes with constructs like #def(void, Foo, (arg, arg))
		// We need to think of a different approach to this conversion problem... 
		// simple macros are not good enough. Ideally we'd apply this macro only
		// in the context of "executable" code, but without a full compiler 
		// pipeline, we have no idea what code is executable and what code is not.
		// In fact, SimpleMacro provides no context information at all.
		//[SimpleMacro("(x,); (x, y, ...)", "Create a tuple", "#tuple")]
		public static LNode Tuple(LNode node, IMessageSink sink)
		{
			// TODO: consider supporting .[a, b] (and !(a, b)) as syntax for @``<a, b> 
			// which will refer to a tuple type.
			if (node.IsCall) {
				MaybeInitTupleMakers();
				LNode method = node.ArgCount < TupleMakers.Count ? TupleMakers[node.ArgCount] : DefaultTupleMaker;
				if (method != null)
					return node.WithTarget(method);
			}
			return null;
		}

		// In EC# we should support cases like "if (Foo[(a, b) = expr]) {...}"
		// This macro targets plain C# where that is not possible.
		[SimpleMacro("(a, b, etc) = expr;", "Assign a = expr.Item1, b = expr.Item2, etc.", "=", "#=")]
		public static LNode UnpackTuple(LNode node, IMessageSink sink)
		{
			var a = node.Args;
			if (a.Count == 2 && a[0].CallsMin(S.Tuple, 1)) {
				var stmts = new RWList<LNode>();
				var tuple = a[0].Args;
				var rhs = a[1];
				bool needTemp = rhs.IsCall || !char.IsLower(rhs.Name.Name.TryGet(0, '\0'));
				Symbol tempName = NextTempName();
				if (needTemp) {
					LNode tmp = F.Id(NextTempName());
					stmts.Add(F.Var(F._Missing, tmp.Name, rhs));
					rhs = tmp;
				}
				for (int i = 0; i < tuple.Count; i++)
					stmts.Add(F.Call(S.Set, tuple[i], F.Dot(rhs, F.Id(GSymbol.Get("Item" + (i + 1))))));
				return F.Call(S.Splice, stmts.ToRVList());
			}
			return null;
		}
	}
}
