using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections;
using Loyc.Collections.Impl;
using S = Loyc.Syntax.CodeSymbols;
using Loyc.Syntax.Lexing;

namespace LeMP
{
	public partial class StandardMacros
	{
		static readonly Symbol @in = GSymbol.Get("in");

		[LexicalMacro(@"unroll ((X, Y) \in ((X, Y), (Y, X))) {...}",
			"Produces variations of a block of code. The braces are omitted from the output. ")]
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
			var replacements = InternalList<Triplet<Symbol, LNode, int>>.Empty;
			if (var.IsId && !var.HasPAttrs()) {
				replacements.Add(Pair.Create(var.Name, (LNode)LNode.Missing, 0));
			} else {
				var vars = var.Args;
				if ((var.Calls(S.Tuple) || var.Calls(S.Braces)) && vars.All(a => a.IsId && !a.HasPAttrs())) {
					replacements = new Triplet<Symbol, LNode, int>[vars.Count].AsInternalList();
					for (int i = 0; i < vars.Count; i++) {
						replacements.InternalArray[i].A = vars[i].Name;
						
						// Check for duplicate names
						for (int j = 0; j < i; j++)
							if (replacements[i].A == replacements[j].A)
								sink.Write(Severity.Error, vars[i], "unroll: duplicate name in the left-hand tuple"); // non-fatal
					}
				} else
					return Reject(sink, cases, "unroll: the left-hand side of 'in' should be a simple identifier or a tuple of simple identifiers.");
			}

			UnrollCtx ctx = new UnrollCtx { Replacements = replacements };
			RWList<LNode> output = new RWList<LNode>();
			int iteration = 0;
			foreach (LNode replacement in cases.Args)
			{
				iteration++;
				bool tuple = replacement.Calls(S.Tuple) || replacement.Calls(S.Braces);
				int count = tuple ? replacement.ArgCount : 1;
				if (replacements.Count != count)
				{
					sink.Write(Severity.Error, replacement, "unroll, iteration {0}: Expected {1} replacement items, got {2}", iteration, replacements.Count, count);
					if (count < replacements.Count)
						continue; // too few
				}
				for (int i = 0; i < replacements.Count; i++)
					replacements.InternalArray[i].B = tuple ? replacement.Args[i] : replacement;

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
		class UnrollCtx // helper class for unroll
		{
			Func<LNode, LNode> _replace;
			public UnrollCtx() { _replace = Replace; } // optimization
			public InternalList<Triplet<Symbol, LNode, int>> Replacements;

			public LNode Replace(LNode node) {
				TokenTree tt;
				if (node.IsId) {
					Symbol name = node.Name;
					for (int i = 0; i < Replacements.Count; i++)
						if (Replacements[i].A == name) {
							Replacements.InternalArray[i].C++;
							var repl = Replacements[i].B;
							return repl.WithAttrs(node.Attrs.SmartSelect(_replace).AddRange(repl.Attrs));
						}
					return node;
				} else if ((tt = node.Value as TokenTree) != null) {
					if (ReplaceInTokenTree(ref tt, Replacements))
						return node.WithValue(tt);
				} 
				return node.Select(_replace);
			}
		}

		/// <summary>Replaces Ids with Ids or Literals in token trees.</summary>
		private static bool ReplaceInTokenTree(ref TokenTree tokens, InternalList<Triplet<Symbol, LNode, int>> Replacements)
		{
			TokenTree children;
			bool modified = false;
			for (int i = 0; i < tokens.Count; i++) {
				Token token = tokens[i];
				Symbol id = token.Value as Symbol;
				if (id != null) {
					for (int r = 0; r < Replacements.Count; r++) {
						var repl = Replacements[r];
						if (id == repl.A) {
							if (repl.B.IsId)
								ReplaceAt(ref modified, ref tokens, i, token.WithValue(repl.B.Name));
							else if (repl.B.IsLiteral) {
								ReplaceAt(ref modified, ref tokens, i, new Token(
									(int)Token.GetLiteralKind(repl.B.Value),
									token.StartIndex, token.Length, token.Style,
									repl.B.Value));
							}
						}
					}
				} else if ((children = token.Children) != null) {
					if (ReplaceInTokenTree(ref children, Replacements))
						ReplaceAt(ref modified, ref tokens, i, token.WithValue(children));
				}
			}
			return modified;
		}
		private static void ReplaceAt(ref bool modified, ref TokenTree tokens, int i, Token newToken)
		{
			// Do not modify the token tree directly since it's stored in a 
			// (supposed-to-be-immutable) LNode
			if (!modified) {
				modified = true;
				tokens = tokens.Clone(true);
			}
			tokens[i] = newToken;
		}
	}
}
