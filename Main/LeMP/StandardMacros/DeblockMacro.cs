using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;

namespace LeMP
{
	using S = CodeSymbols;

	partial class StandardMacros
	{
		[LexicalMacro("to_csharp;", "Scans code after this line, replacing certain EC# constructs with plain C# equivalents: "+
			"{code blocks as expressions}, quick::bindings, out var decls. "+
			"Note: must be used in a declaration context. Note: certain other conversions are provided automatically by other macros.")]
		public static LNode to_csharp(LNode node, IMacroContext context)
		{
			var args_body = context.GetArgsAndBody(true);

			return F.Call(S.Splice, args_body.B.SmartSelect(stmt => StmtToCSharp(stmt, context, false, null)));
		}

		/// <summary>Helper type to classify constructs of EC#.</summary>
		struct ExecInfo
		{
			public ExecInfo(int braceIndex, bool containsExec, int ignoreMax, Symbol parent = null)
				{ IgnoreMax = ignoreMax; BraceIndex = braceIndex; ContainsExec = containsExec; Parent = parent; }
			public ExecInfo(int braceIndex, bool containsExec, Symbol parent = null)
				: this(braceIndex, containsExec, containsExec ? 0 : 999, parent) { }
			/// <summary>The argument at this index can be a braced block that is 
			/// assumed to belong there (doesn't need to be transformed). 
			/// Special-cased: #if, #try</summary>
			public int BraceIndex;
			/// <summary>True if the braced block contains executable code.</summary>
			public bool ContainsExec;
			/// <summary>Arguments below this index are left unprocessed by StmtToCSharp</summary>
			public int IgnoreMax;
			/// <summary>For contextual keywords, this is what the parent construct must call.</summary>
			public Symbol Parent;
		}

		private static Dictionary<Symbol, ExecInfo> StatementTypes = new Dictionary<Symbol, ExecInfo> { 
			{ S.Var,       new ExecInfo(-1, true, 1) },
			{ S.Fn,        new ExecInfo(3, true, 999) },
			{ S.Property,  new ExecInfo(2, false) },
			{ S.get,       new ExecInfo(0, true, S.Property) },
			{ S.set,       new ExecInfo(0, true, S.Property) },
			{ S.Event,     new ExecInfo(2, false) },
			{ S.add,       new ExecInfo(0, true, S.Event) },
			{ S.remove,    new ExecInfo(0, true, S.Event) },
			{ S.Class,     new ExecInfo(2, false) },
			{ S.Struct,    new ExecInfo(2, false) },
			{ S.Alias,     new ExecInfo(2, false) },
			{ S.Trait,     new ExecInfo(2, false) },
			{ S.Enum,      new ExecInfo(2, false) },
			{ S.Namespace, new ExecInfo(2, false) },
			{ S.Interface, new ExecInfo(2, false) },
			{ S.If,        new ExecInfo(1, true) },
			{ S.DoWhile,   new ExecInfo(0, true) },
			{ S.While,     new ExecInfo(1, true) },
			{ S.UsingStmt, new ExecInfo(1, true) },
			{ S.For      , new ExecInfo(3, true) },
			{ S.ForEach  , new ExecInfo(2, true) },
			//{ S.Label    , new ExecInfo(-1, true) },
			{ S.Case     , new ExecInfo(-1, true, 999) },
			//{ S.Return   , new ExecInfo(-1, true) },
			//{ S.Continue , new ExecInfo(-1, true) },
			//{ S.Break    , new ExecInfo(-1, true) },
			//{ S.Goto     , new ExecInfo(-1, true) },
			//{ S.GotoCase , new ExecInfo(-1, true) },
			//{ S.Throw    , new ExecInfo(-1, true) },
			{ S.Checked  , new ExecInfo(0, true) },
			{ S.Unchecked, new ExecInfo(0, true) },
			{ S.Fixed    , new ExecInfo(2, true) },
			{ S.Lock     , new ExecInfo(1, true) },
			{ S.Switch   , new ExecInfo(1, true) },
			{ S.Try      , new ExecInfo(0, true) },
			{ S.Catch    , new ExecInfo(1, true, 1, S.Try) },
			{ S.Finally  , new ExecInfo(1, true, 0, S.Try) },
		};

		private static LNode StmtToCSharp(LNode stmt, IMacroContext context, bool execContext, Symbol parentConstruct)
		{
			if (!stmt.IsCall)
				return stmt;
			if (stmt.Calls(S.Braces))
				return null;

			var helpers = RVList<LNode>.Empty;
			ExecInfo info;
			if (!StatementTypes.TryGetValueSafe(stmt.Name, out info))
				info = new ExecInfo(-1, execContext);

			var args = stmt.Args;
			for (int i = info.IgnoreMax; i < stmt.Args.Count; i++)
			{
				if (i == info.BraceIndex) continue;
			}

			return stmt;
		}

		private static LNode MaybeTransformExpr(LNode lNode, ref RVList<LNode> helpers, bool execContext)
		{
			throw new NotImplementedException();
		}

		static LNode TransformExpr(LNode expr)
		{
			throw new NotImplementedException();
		}

		static LNode TransformCall(LNode call, IMessageSink sink)
		{
			codematch (call) {
				case [$(...attrs)] $target($(...args)):
					matches($target, @`{}`, @`#`)
					target = TransformExpr(target);
				
			}
			
			// Transform target if necessary
			var target = TransformExpr(call.Target);
			if (target != null) {
				Debug.Assert(target.Name == S.Braces || target.Name == S.AltList);
				if (target.ArgCount == 0) {
					sink.Write(Severity.Error, target, "An empty block cannot be used as an expression.");
				} else {
					// Goal: {A; B; final}(D, E, F) =>  {A; B; final(D, E, F)}
					//   or #(A; B; final)(D, E, F) => #(A; B; final(D, E, F))
					//       target   (call.Args)
					var final = target.Args.Last;
					bool isBraces = target.Name == S.Braces;
					if (final.Calls(S.Result, 1)) {
						final = final.Args[0];
					} else if (isBraces) {
						sink.Write(Severity.Error, final, "A {braced expression} must end with a simple expression that produces a value (remove trailing ';', if any)");
					}
					return target.WithArgChanged(target.ArgCount - 1, TransformCall(call.WithTarget(final), sink));
				}
			}

			// Transform arguments.
			// 1. F(A, {B}, C) => 
			//    #( var tmp1 = A; [[ var tmp2 = {B} ]]; F(tmp1, tmp2, C) )
			var expanded = RVList<LNode>.Empty;
			var newArgs = RVList<LNode>.Empty;
			var args = call.Args;
			for (int i = 0; i < args.Count; i++)
			{
				var result = TransformExpr(args[i]);
				if (result != null)
				{
					LNode tmpVarName;
					while (newArgs.Count < i) {
						expanded.Add(TempVarDecl(args[newArgs.Count], out tmpVarName));
						newArgs.Add(tmpVarName);
					}
					expanded.Add(FinishTransformVarDecl(TempVarDecl(result, out tmpVarName)));
					newArgs.Add(tmpVarName);
				}
			}
			if (newArgs.Count == 0)
				return null; // This is the common case: no transformations happened
			else {
				while (newArgs.Count < args.Count)
					newArgs.Add(args[newArgs.Count]);
				expanded.Add(call.WithArgs(newArgs));
				return F.Call(S.AltList, expanded);
			}
		}

		private static LNode FinishTransformVarDecl(LNode var)
		{
			Debug.Assert (var.CallsMin(S.Var, 2));
			if (var.ArgCount > 2)
				return F.Call(S.AltList, var.Args.Slice(1).Select(a => FinishTransformVarDecl(var.WithArgs(var.Args[0], a))));
			
			var expanded = RVList<LNode>.Empty;
			var ass = var.Args[1];
			if (!ass.Calls(S.Assign, 2))
				return var;
			LNode block;
			if ((block = ass.Args[1]).CallsMin(S.Braces, 1) && !var.Args[0].IsIdNamed(GSymbol.Empty)) {
				// Use advance-declaration mode, which preserves braces and therefore scope:
				// Type a = {A0;A1} => 
				//     Type a;
				//     { A0; a = A1; }
				expanded.Add(var.WithArgs(var.Args[0], ass.Args[0]));
				expanded.Add(block.WithArgChanged(block.ArgCount-1, F.Call(S.Assign, ass.Args[0], block.Last)));
			} else if (block.CallsMin(S.AltList, 1)) {
				// var a = #(B0,B1); =>
				//     B0;
				//     var b = B1;
				expanded = block.WithoutLast(1);
				expanded.Add(F.Var(F._Missing, ass.Args[0], block.Last));
			} else
				return var;
			return F.List(expanded);
		}
	}
}
