using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections;
using Loyc.Collections.Impl;
using S = Loyc.Syntax.CodeSymbols;

namespace LeMP
{
	public partial class StandardMacros
	{
		static readonly Symbol @in = GSymbol.Get("in");

		[SimpleMacro(@"unroll ((X, Y) \in ((X, Y), (Y, X))) {...}",
			"Produces variations of a block of code. The braces are omitted from the output.")]
		public static LNode unroll(LNode node, IMessageSink sink)
		{
			LNode clause;
			// unroll (X, Y) \in ((X, Y), (Y, X)) {...}
			// unroll ((X, Y) in ((X, Y), (Y, X))) {...}
			if (node.ArgCount == 2 && ((clause = node.Args[0]).Calls(@in, 2) || clause.Calls(S.In, 2)))
			{
				var result = unroll(clause.Args[0], clause.Args[1], node.Args[1], sink);
				if (result != null && node.HasPAttrs())
					sink.Write(Severity.Warning, result.Attrs[0], "'unroll' does not support attributes.");
				return result;
			}
			return null;
		}
		
		public static LNode unroll(LNode var, LNode cases, LNode body, IMessageSink sink)
		{
			if (!cases.Calls(S.Tuple) && !cases.Calls(S.Braces))
				return Reject(sink, cases, "unroll: the right-hand side of 'in' should be a tuple");

			// Maps identifiers => replacements. The integer counts how many times replacement occurred.
			Triplet<Symbol, LNode, int>[] replacements = null;
			if (var.IsId && !var.HasPAttrs()) {
				replacements = new Triplet<Symbol, LNode, int>[1] { 
					Pair.Create(var.Name, (LNode)LNode.Missing, 0)
				};
			} else {
				var vars = var.Args;
				if ((var.Calls(S.Tuple) || var.Calls(S.Braces)) && vars.All(a => a.IsId && !a.HasPAttrs())) {
					replacements = new Triplet<Symbol, LNode, int>[vars.Count];
					for (int i = 0; i < vars.Count; i++) {
						replacements[i].A = vars[i].Name;
						
						// Check for duplicate names
						for (int j = 0; j < i; j++)
							if (replacements[i].A == replacements[j].A)
								sink.Write(Severity.Error, vars[i], "unroll: duplicate name in the left-hand tuple"); // non-fatal
					}
				} else
					return Reject(sink, cases, "unroll: the left-hand side of 'in' should be a simple identifier or a tuple of simple identifiers.");
			}

			ReplaceCtx ctx = new ReplaceCtx { Replacements = replacements };
			RWList<LNode> output = new RWList<LNode>();
			int iteration = 0;
			foreach (LNode replacement in cases.Args)
			{
				iteration++;
				bool tuple = replacement.Calls(S.Tuple) || replacement.Calls(S.Braces);
				int count = tuple ? replacement.ArgCount : 1;
				if (replacements.Length != count)
				{
					sink.Write(Severity.Error, replacement, "unroll, iteration {0}: Expected {1} replacement items, got {2}", iteration, replacements.Length, count);
					if (count < replacements.Length)
						continue; // too few
				}
				for (int i = 0; i < replacements.Length; i++)
					replacements[i].B = tuple ? replacement.Args[i] : replacement;

				if (body.Calls(S.Braces)) {
					foreach (LNode stmt in body.Args)
						output.Add(ctx.Replace(stmt));
				} else
					output.Add(ctx.Replace(body));
			}

			foreach (var r in replacements)
				if (r.C == 0)
					sink.Write(Severity.Warning, r.B, "Replacement variable '{0}' was never used", r.A);
			
			return body.With(S.Splice, output.ToRVList());
		}
		class ReplaceCtx // helper class for unroll
		{
			Func<LNode, LNode> _replace;
			public ReplaceCtx() { _replace = Replace; } // optimization
			public Triplet<Symbol, LNode, int>[] Replacements;

			public LNode Replace(LNode node) {
				if (node.IsId) {
					Symbol name = node.Name;
					for (int i = 0; i < Replacements.Length; i++)
						if (Replacements[i].A == name) {
							Replacements[i].C++;
							var repl = Replacements[i].B;
							return repl.WithAttrs(node.Attrs.SmartSelect(_replace).AddRange(repl.Attrs));
						}
					return node;
				} else
					return node.Select(_replace);
			}
		}
	}
}
