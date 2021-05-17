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
		[LexicalMacro(@"##unroll (pattern in inputList) { output }; // EC# syntax; use .#unroll in LES3",
			"Produces variations of a block of code, matching each item in the `inputList` against " +
			"a pattern to the left of `in`. The `inputList` is preprocessed before its items are " +
			"matched against the pattern. The item list must be a tuple, a braced block, a JSON-style " +
			"list (the '[] operator), or a call to #splice. Every item in inputList should match the " +
			"pattern, or an error is produced." +
			"\n\n" +
			"This macro is closely related to ##map, but a warning is produced if any items in " +
			"`inputList` don't match the pattern.",
			"##unroll")]
		public static LNode unroll(LNode node, IMacroContext context)
		{
			if (node.ArgCount != 2)
				Reject(context, node, "Expected two arguments, got {0}.".Localized(node.ArgCount));

			LNode spec = node[0], body = node[1];
			if (spec.Calls(CodeSymbols.In, 2) || spec.Calls(@in, 2)) {
				var pattern = spec.Args[0];
				var input = context.PreProcess(spec.Args[1]);
				var listType = input.Target?.Name;
				if (listType == null || !listType.IsOneOf(S.Tuple, S.Braces, S.Array, S.Splice)) {
					return Reject(context, input, "Expected a list (Target must be 'tuple, '[], '{} or #splice)");
				}

				var @case = new List<(LNodeList, LNodeList)> { (pattern.AsList(S.Braces), body.AsList(S.Braces)) };
				var (results, failIndex) = DoMapping(input, 0, input.ArgCount, @case);
				if (failIndex > -1)
					context.Error(input[failIndex], "Item #{0} does not match the pattern".Localized(failIndex + 1));

				return input.With(S.Splice, LNode.List(results));
			}
			return Reject(context, spec, "Expected binary `in` operator. If you already used one, try enclosing the left-hand side in parentheses.");
		}

		static readonly Symbol @in = GSymbol.Get("in");

		[LexicalMacro(@"/* LES syntax */ unroll ((X, Y) `in` ((X, Y), (Y, X))) {...}; /* EC#/LES3 syntax */ unroll ((X, Y) in ((X, Y), (Y, X))) {...}",
			 "This is obsolete. Please use ##foreach instead, which accepts arbitrary patterns" +
			 "on the left but expects the `$` operator to mark all uses of each variable." +
			 "For example, `unroll (f in (F,G)) { f(f); }` should be changed to " +
			 "`##unroll ($f in (F,G)) { $f($f); }`.\n\n"
			+"Produces variations of a block of code, by replacing an identifier left of `in` "
			+"with each of the corresponding expressions on the right of `in`. The list on the "
			+"right side can either be a tuple or a braced list of statements.\n\n"
			+"The braces around the final block of code are omitted from the output.\n\n"
			+"If the right-hand side of `in` is not a list (tuple, splice or braced block), "
			+"macros are executed on the right-hand side in the hope of creating a list.",
			"unroll", "#unroll")]
		public static LNode old_unroll(LNode node, IMacroContext context)
		{
			LNode clause;
			// unroll (X, Y) \in ((X, Y), (Y, X)) {...}
			// unroll ((X, Y) in ((X, Y), (Y, X))) {...}
			if (node.ArgCount == 2 && ((clause = node.Args[0]).Calls(@in, 2) || clause.Calls(S.In, 2)))
			{
				LNode identifiers = clause.Args[0], cases = clause.Args[1];
				if (!cases.Calls(S.Tuple) && !cases.Calls(S.Braces) && !cases.Calls(S.Splice)) {
					cases = context.PreProcess(cases);
					if (!cases.Calls(S.Tuple) && !cases.Calls(S.Braces) && !cases.Calls(S.Splice))
						return Reject(context, cases, "The right-hand side of 'in' should be a tuple or braced block.");
				}
				var result = old_unroll(identifiers, cases.Args, node.Args[1], context.Sink);
				if (result != null)
				{
					if (node.HasPAttrs())
						context.Sink.Warning(result.Attrs[0], "'unroll' does not support attributes.");
					return result.IncludingTriviaFrom(node);
				}
			}
			return null;
		}
		
		public static LNode old_unroll(LNode var, LNodeList cases, LNode body, IMessageSink sink)
		{
			// Maps identifiers => replacements. The integer counts how many times replacement occurred.
			var replacements = InternalList<Triplet<Symbol, LNode, int>>.Empty;
			if (var.IsId && !var.HasPAttrs()) {
				replacements.Add(Triplet.Create(var.Name, (LNode)LNode.Missing, 0));
			} else {
				var vars = var.Args;
				if ((var.Calls(S.Tuple) || var.Calls(S.Braces)) && vars.All(a => a.IsId && !a.HasPAttrs())) {
					replacements = new Triplet<Symbol, LNode, int>[vars.Count].AsInternalList();
					for (int i = 0; i < vars.Count; i++) {
						replacements.InternalArray[i].A = vars[i].Name;
						
						// Check for duplicate names
						for (int j = 0; j < i; j++)
							if (replacements[i].A == replacements[j].A && replacements[i].A.Name != "_")
								sink.Error(vars[i], "Duplicate name in the left-hand tuple"); // non-fatal
					}
				} else
					return Reject(sink, var, "The left-hand side of 'in' should be a simple identifier or a tuple of simple identifiers.");
			}

			UnrollCtx ctx = new UnrollCtx { Replacements = replacements };
			WList<LNode> output = new WList<LNode>();
			int iteration = 0;
			foreach (LNode replacement in cases)
			{
				iteration++;
				bool tuple = replacement.Calls(S.Tuple) || replacement.Calls(S.Braces);
				int count = tuple ? replacement.ArgCount : 1;
				if (replacements.Count != count)
				{
					sink.Error(replacement, "iteration {0}: Expected {1} replacement items, got {2}", iteration, replacements.Count, count);
					if (count < replacements.Count)
						continue; // too few
				}
				for (int i = 0; i < replacements.Count; i++)
					replacements.InternalArray[i].B = tuple ? replacement.Args[i] : replacement;

				if (body.Calls(S.Braces)) {
					foreach (LNode stmt in body.Args)
						output.Add(ctx.Replace(stmt).Value);
				} else
					output.Add(ctx.Replace(body).Value);
			}

			foreach (var r in replacements)
				if (r.C == 0 && !r.A.Name.StartsWith("_"))
					sink.Write(Severity.Warning, var, "Replacement variable '{0}' was never used", r.A);
			
			return body.With(S.Splice, output.ToLNodeList());
		}
		class UnrollCtx // helper class for old_unroll
		{
			Func<LNode, Maybe<LNode>> _replace;
			public UnrollCtx() { _replace = Replace; } // optimization
			public InternalList<Triplet<Symbol, LNode, int>> Replacements;

			public Maybe<LNode> Replace(LNode node) {
				TokenTree tt;
				if (node.IsId) {
					Symbol name = node.Name;
					for (int i = 0; i < Replacements.Count; i++)
						if (Replacements[i].A == name) {
							Replacements.InternalArray[i].C++;
							var repl = Replacements[i].B;
							return repl.WithAttrs(node.Attrs.WhereSelect(_replace).AddRange(repl.Attrs));
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
							if (repl.B.IsId) {
								ReplaceAt(ref modified, ref tokens, i, token.WithValue(repl.B.Name));
							} else if (repl.B.IsLiteral) {
								ReplaceAt(ref modified, ref tokens, i, new Token(
									(int)TokenKind.Literal,
									token.StartIndex, token.Length, token.Style,
									repl.B.Value));
							}
							if (!repl.B.IsCall)
								Replacements.InternalArray[r].C++; // prevent 'never used' warning
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
