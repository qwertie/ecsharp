using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections;
using System.Diagnostics;
using Loyc.Syntax.Lexing;
using Loyc.Collections.Impl;
using Loyc.Ecs;
using S = Loyc.Syntax.CodeSymbols;

namespace LeMP
{
	public partial class StandardMacros
	{
		[LexicalMacro(@"replace (input($capture) => output($capture), ...) {...}",
			"Finds one or more patterns in a block of code and replaces each matching expression with another expression. "+
			"The braces are omitted from the output (and are not matchable). "+
			"This macro can be used without braces, in which case it affects all the statements/arguments that follow it in the current statement or argument list. "+
			"The alternate name `replacePP` additionally preprocesses the input and output arguments, and is useful to get around problems with macro execution order. "+
			"This behavior is not the default, since the final output will be macro-processed a second time.",
			"replace", "#replace", "replacePP")]
		public static LNode replace(LNode node, IMacroContext context)
		{
			var args_body = context.GetArgsAndBody(true);
			var args = args_body.A;
			var body = args_body.B;
			if (args.Count == 1 && args[0].Calls(S.Tuple)) args = args[0].Args; // LESv2
			if (args.Count >= 1)
			{
				bool preprocess = node.Calls("replacePP");

				var patterns = new Pair<LNode, LNode>[args.Count];
				for (int i = 0; i < patterns.Length; i++)
				{
					var pair = args[i];
					if (pair.Calls(S.Lambda, 2)) {
						LNode pattern = pair[0], repl = pair[1];
						if (preprocess)
						{
							pattern = context.PreProcess(pattern);
							repl = context.PreProcess(repl);
						}
						if (pattern.Calls(S.Braces)) {
							if (pattern.ArgCount == 1)
								pattern = pattern.Args[0];
							else
								context.Write(Severity.Error, pattern, "The braces must contain only a single statement. To search for braces literally, use `{{ ... }}`");
						}
						if (repl.Calls(S.Braces))
							repl = repl.Args.AsLNode(S.Splice);
						
						// Avoid StackOverflowException when pattern is $Id (sadly, it
						// is uncatchable so it can crash LeMP.exe and even Visual Studio)
						if (LNodeExt.GetCaptureIdentifier(pattern) != null)
 							return Reject(context, pattern, "The left side of `=>` cannot be a capture. Remove the `$`.");

						patterns[i] = Pair.Create(pattern, repl);
					} else {
						string msg = "Expected 'pattern => replacement'.";
						if (pair.Descendants().Any(n => n.Calls(S.Lambda, 2)))
							msg += " " + "(Using '=>' already? Put the pattern on the left-hand side in parentheses.)";
						return Reject(context, pair, msg);
					}
				}

				int replacementCount;
				var output = Replace(body, patterns, out replacementCount);
				if (replacementCount == 0)
					context.Sink.Warning(node, "No patterns recognized; no replacements were made.");
				return output.AsLNode(S.Splice);
			}
			return null;
		}

		[ThreadStatic]
		static InternalList<Triplet<Symbol, LNode, int>> _tokenTreeRepls;

		/// <summary>Searches a list of expressions/statements for one or more 
		/// patterns, and performs replacements.</summary>
		/// <param name="stmts">A list of expressions/statements in which to search.</param>
		/// <param name="patterns">Each pair consists of (A) something to search 
		/// for and (B) a replacement expression. Part A can use the substitution
		/// operator with an identifier inside (e.g. $Foo) to "capture" any 
		/// subexpression, and part B can use the same substitution (e.g. $Foo)
		/// to insert the captured subexpression(s) into the output.</param>
		/// <param name="replacementCount">Number of replacements that occurred.</param>
		/// <returns>The result of applying the replacements.</returns>
		/// <remarks><see cref="LNodeExt.MatchesPattern(LNode, LNode, ref MMap{Symbol, LNode}, out LNodeList, bool)"/> is used for matching.</remarks>
		public static LNodeList Replace(LNodeList stmts, Pair<LNode, LNode>[] patterns, out int replacementCount)
		{
			// This list is used to support simple token replacement in TokenTrees
			_tokenTreeRepls = InternalList<Triplet<Symbol, LNode, int>>.Empty;
			foreach (var pair in patterns) // Look for Id => Id or Id => Literal
				if (pair.A.IsId && (pair.B.IsId || pair.B.IsLiteral))
					_tokenTreeRepls.Add(new Triplet<Symbol,LNode,int>(pair.A.Name, pair.B, 0));

			// Scan the syntax tree for things to replace...
			int count = 0;
			var temp = new MMap<Symbol, LNode>();
			var output = stmts.SmartSelect(stmt => stmt.ReplaceRecursive(n => {
				LNode r = TryReplaceHere(n, patterns, temp);
				if (r != null) count++;
				return r;
			}));
			replacementCount = count;
			return output;
		}
		public static LNode Replace(LNode stmt, Pair<LNode, LNode>[] patterns, out int replacementCount)
		{
			CheckParam.IsNotNull("stmt", stmt);
			return Replace(new LNodeList(stmt), patterns, out replacementCount)[0];
		}

		static LNode TryReplaceHere(LNode node, Pair<LNode, LNode>[] patterns, MMap<Symbol, LNode> temp)
		{
			for (int i = 0; i < patterns.Length; i++)
			{
				temp.Clear();
				LNode r = TryReplaceHere(node, patterns[i].A, patterns[i].B, temp, patterns);
				if (r != null) return r;
			}

			// Support simple token replacement in TokenTrees
			TokenTree tt;
			if (node.IsLiteral && (tt = node.Value as TokenTree) != null) {
				bool modified = ReplaceInTokenTree(ref tt, _tokenTreeRepls);
				if (modified)
					return node.WithValue(tt);
			}
	
			return null;
		}
		static LNode TryReplaceHere(LNode node, LNode pattern, LNode replacement, MMap<Symbol, LNode> captures, Pair<LNode, LNode>[] allPatterns)
		{
			if (LNodeExt.MatchesPattern(node, pattern, ref captures, out LNodeList attrs)) {
				foreach (var pair in captures) {
					var input = pair.Value.AsList(S.Splice);
					int c;
					var output = Replace(input, allPatterns, out c);
					if (output != input)
						captures[pair.Key] = output.AsLNode(S.Splice);
				}
				return ReplaceCaptures(replacement, captures).PlusAttrs(attrs);
			}

			return null;
		}

		/// <summary>Finds capture variables like <c>$x</c> and replaces them with values
		/// from <c>captures</c> (e.g. <c>captures[(Symbol)"x"]</c> for <c>$x</c>)</summary>
		public static LNode ReplaceCaptures(LNode outputSpec, MMap<Symbol, LNode> captures)
		{
			if (captures.Count != 0)
			{
				// TODO: EXPAND SPLICES! Generally it works anyway though because 
				// the macro processor has built-in support for #splice.
				return outputSpec.ReplaceRecursive(n => {
					LNode id, cap;
					if ((id = LNodeExt.GetCaptureIdentifier(n)) != null) {
						if (captures.TryGetValue(id.Name, out cap))
							return cap.PlusAttrsBefore(n.Attrs);
					}
					return null;
				});
			}
			return outputSpec;
		}
	}
}
