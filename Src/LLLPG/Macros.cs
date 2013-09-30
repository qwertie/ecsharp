using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Syntax;
using Loyc.Utilities;
using LEL.Prelude;
using Loyc.Collections;
using Loyc.Syntax.Lexing;
using S = Loyc.Syntax.CodeSymbols;
using Loyc.Math;
using Loyc.Collections.Impl;
using Loyc.Syntax.Les;

namespace Loyc.LLParserGenerator
{
	/// <summary>
	/// Macros for using LLLPG in micro-LEL.
	/// </summary>
	/// <remarks>
	/// Example:
	/// <code>
	///   class Foo { 
	///     [DefaultK(2)] LLLPG lexer
	///     {
	///       [priv]  rule int  @[ '0'..'9'+ ];
	///       [priv]  rule id   @[ 'a'..'z'|'A'..'Z' ('a'..'z'|'A'..'Z'|'0'..'9'|'_')* ];
	///       [token] rule token  @[ int | id ];
	///     };
	///   };
	/// </code>
	/// Up to three macros are used to invoke LLLPG. 
	/// <ol>
	/// <li>there is a macro to recognize the pattern <c>LLLPG(lexer, {...})</c> 
	/// and translate "lexer" to an unprintable literal of type 
	/// <see cref="IntStreamCodeGenHelper"/>, and another macro for 
	/// <c>LLLPG(parser(Symbol, false), {...})"</c> that creates a 
	/// <see cref="GeneralCodeGenHelper"/> (this is the default helper).</li>
	/// <li>The stage-one LLLPG() macro uses <see cref="StageOneParser"/> to
	/// translate token trees into expressions, e.g. <c>@[ ("Foo" | bar)* ~';' ]</c> 
	/// is currently translated to <c>#tuple(@`#suf*`("Foo" | bar), ~';')</c>.
	/// LLLPG() replaces itself with LLLPG_stage2() so users need not be aware
	/// that two stages exist. <c>LLLPG()</c> expects an entire grammar, but
	/// <c>LLLPG_stage1(@[...])</c> might be used in advanced scenarios to invoke 
	/// the stage-one parser directly.</li>
	/// <li>The stage-two LLLPG_stage2() macro calls <see cref="StageTwoParser"/>
	/// to translate expressions into <see cref="Pred"/> objects, and then
	/// invokes <see cref="LLParserGenerator"/> to generate code.</li>
	/// </ol>
	/// </remarks>
	[ContainsMacros]
	public static class Macros
	{
		static readonly Symbol _rule = GSymbol.Get("rule");
		static readonly Symbol _token = GSymbol.Get("token");
		static readonly Symbol _term = GSymbol.Get("term");
		static readonly Symbol _def = GSymbol.Get("def");
		static readonly Symbol _lexer = GSymbol.Get("lexer");
		static readonly Symbol _parser = GSymbol.Get("parser");
		static readonly Symbol _seq = GSymbol.Get("#seq");
		static readonly Symbol _LLLPG_stage2 = GSymbol.Get("LLLPG_stage2");
		static readonly LNodeFactory F = new LNodeFactory(new EmptySourceFile("LLLPG"));

		/// <summary>Helper macro that translates <c>lexer</c> in <c>LLLPG(lexer, {...})</c> 
		/// into a <see cref="IntStreamCodeGenHelper"/> object.</summary>
		[SimpleMacro("LLLPG lexer {Body...}", "Runs LLLPG in lexer mode (via IntStreamCodeGenHelper)", "LLLPG")]
		public static LNode LLLPG_lexer(LNode node, IMessageSink sink)
		{
			LNode helper;
			if (node.ArgCount != 2 || (helper = node.Args[0]).Name != _lexer)
				return null;
			if (helper.ArgCount != 0) {
				sink.Write(MessageSink.Error, helper, "lexer: no arguments expected");
				return null;
			}
			return node.WithArgChanged(0, F.Literal(new IntStreamCodeGenHelper()));
		}

		/// <summary>Helper macro that translates <c>parser</c> in <c>LLLPG(parser, {...})</c> 
		/// into a <see cref="GeneralCodeGenHelper"/> object.</summary>
		[SimpleMacro("LLLPG parser {Body...}; LLLPG parser(laType, allowSwitch) {Body...}", "Runs LLLPG in general-purpose mode (via GeneralCodeGenHelper)", "LLLPG")]
		public static LNode LLLPG_parser(LNode node, IMessageSink sink)
		{
			LNode helper;
			if (node.ArgCount != 2 || (helper = node.Args[0]).Name != _parser)
				return null;
			var arg0 = helper.Args.TryGet(0, null) ?? F.Literal("#int32");
			var arg1 = helper.Args.TryGet(1, null) ?? F.Literal(true);
			if (!helper.ArgCount.IsInRange(0, 2) || !(arg0.Value is string) || !(arg1.Value is bool)) {
				sink.Write(MessageSink.Error, helper, "parser: expected arguments (laType::string, allowSwitch::bool). The arguments must be literals.");
				return null;
			}
			return node.WithArgChanged(0, F.Literal(new GeneralCodeGenHelper((string)arg0.Value, (bool)arg1.Value)));
		}

		// Stage 1 macro
		[SimpleMacro("LLLPG {Body...}", "Runs the Loyc LL(k) Parser Generator on the specified Body, which describes a grammar using 'rule Name @[Body]' statements.")]
		public static LNode LLLPG(LNode node, IMessageSink sink)
		{
			IPGCodeGenHelper helper = null;
			LNode body;
			if (!node.ArgCount.IsInRange(1, 2) || !(body = node.Args.Last).Calls(S.Braces) 
				|| (node.ArgCount == 2 && null == (helper = node.Args[0].Value as IPGCodeGenHelper))) {
				sink.Write(MessageSink.Note, node, "Expected LLLPG({...}), which means LLLPG(parser(), {...}), or LLLPG(lexer, {...})");
				return null;
			}
			
			// So there's a bunch of rules like this:
			//   rule Start()::AST @[ {statement1;} (a | b | c) {statement2;} ];
			// or like this:
			//   rule Start()::AST {
			//     statement1;
			//     @[ a | b | c ];
			//     statement2;
			//   }
			// And we'd like to use the stage-1 parser to produce output like this:
			//   rule Start()::AST #({statement1;}, a | b | c, {statement2;});

			body = body.WithArgs(stmt => {
				LNode ruleBody = null;
				if (IsRule(stmt, out ruleBody, true)) {
					TokenTree ruleTokens = ruleBody.Value as TokenTree;
					if (ruleTokens != null)
						ruleBody = ParseTokens(ruleTokens, sink);
					else { // ruleBraces
						if (ruleBody.Args.Any(stmt2 => stmt2.Value is TokenTree)) {
							ruleBody = ruleBody.With(S.Tuple, ruleBody.Args.SmartSelect(stmt2 => 
							{
								if (stmt2.Value is TokenTree)
									return ParseTokens((TokenTree)stmt2.Value, sink);
								else if (stmt2.Calls(S.Braces))
									return stmt2;
								else
									return F.Braces(stmt2);
							}));
						}
					}
					return stmt.WithArgChanged(1, ruleBody);
				} else if (stmt.Calls(_rule) || stmt.Calls(_token))
					sink.Write(MessageSink.Error, stmt, "A rule should have the form rule(Name(Args)::ReturnType, @[...])");
				return stmt;
			});
			return node.With(_LLLPG_stage2, F.Literal(helper), body);
		}
		private static bool IsRule(LNode stmt, out LNode ruleBody, bool stage1)
		{
			ruleBody = null;
			if (stmt.Calls(_rule, 2) || stmt.Calls(_token, 2)) {
				ruleBody = stmt.Args[1];
				if (ruleBody.Value is TokenTree)
					return stage1;
				return stage1 ? ruleBody.Calls(S.Braces) : true;
			}
				
			return false;
		}
		private static LNode ParseRuleBody(LNode ruleBody, IMessageSink sink)
		{
			TokenTree ruleTokens;
			if ((ruleTokens = ruleBody.Value as TokenTree) == null && !ruleBody.Calls(S.Braces))
				return null;

			if (ruleTokens != null)
				return ParseTokens(ruleTokens, sink);
			else {
				if (ruleBody.Args.Any(stmt2 => stmt2.Value is TokenTree)) {
					ruleBody = ruleBody.With(S.Tuple, ruleBody.Args.SmartSelect(stmt2 => 
					{
						if (stmt2.Value is TokenTree)
							return ParseTokens((TokenTree)stmt2.Value, sink);
						else if (stmt2.Calls(S.Braces))
							return stmt2;
						else
							return F.Braces(stmt2);
					}));
				}
			}
			return ruleBody;
		}
		private static LNode ParseTokens(TokenTree tokens, IMessageSink sink)
		{
			var list = StageOneParser.Parse(tokens, tokens.File, sink);
			if (list.Count == 1 && list[0].Calls(S.Tuple))
				return list[0];
			else
				return F.Tuple(list);
		}

		// This macro is used to translate a single token tree or rule body
		[SimpleMacro("LLLPG_stage1(@[...])", "The LLLPG stage-1 parser converts a token tree into a Loyc tree suitable for input into stage 2.")]
		public static LNode LLLPG_stage1(LNode node, IMessageSink sink)
		{
			LNode result;
			if (node.ArgCount == 1 && (result = ParseRuleBody(node.Args[0], sink)) != null)
				return result;
			else {
				sink.Write(MessageSink.Error, node, "Expected one argument of the form @[...] or {... @[...]; ...}");
				return null;
			}
		}

		// Stage 2 macro
		[SimpleMacro("LLLPG_stage2 {Body...}", "The LLLPG stage-2 parser analyzes a set of rules (with all @[token trees] removed) and generates C# code for each one.")]
		public static LNode LLLPG_stage2(LNode node, IMessageSink sink)
		{
			if (node.ArgCount != 2)
				return null; // wtf?

			var helper = (node.Args[0].Value as IPGCodeGenHelper) ?? new GeneralCodeGenHelper();
			var rules = new List<Pair<Rule, LNode>>();
			var stmts = new List<LNode>();
			var body = node.Args[1];

			// Let helper preprocess the code if it wants to
			foreach (var stmt in body.Args) {
				var stmt2 = helper.VisitInput(stmt, sink) ?? stmt;
				if (stmt2.Calls(S.Splice))
					stmts.AddRange(stmt2.Args);
				else
					stmts.Add(stmt2);
			}

			// Find rule definitions, create Rule objects
			for (int i = 0; i < stmts.Count; i++)
			{
				LNode stmt = stmts[i], methodBody;
				if (IsRule(stmt, out methodBody, false)) {
					// Create a method prototype to use for the rule
					LNode sig = stmt.Args[0];
					if (LEL.Prelude.Macros.IsComplexId(sig))
						sig = F.Call(sig); // def requires an argument list
					var basis = LEL.Prelude.Macros.def(
						stmt.With(_def, new RVList<LNode>(sig)), sink);
					if (basis != null) {
						// basis has the form #def(ReturnType, Name, #(Args))
						var name = basis.Args[1];
						if (name.CallsMin(S.Of, 1))
							name = name.Args[0];
						if (!name.IsId) {
							sink.Write(MessageSink.Error, name, "Unacceptable rule name");
						} else {
							var prev = rules.FirstOrDefault(pair => pair.A.Name == name.Name);
							if (prev.A != null)
								sink.Write(MessageSink.Error, name, "The rule name «{0}» was used before at {1}", name, prev.A.Basis.Range.Begin);
							else {
								var rule = new Rule(basis, name.Name, null, true);
								if (stmt.Calls(_token))
									rule.IsToken = true;
								ApplyRuleOptions(ref rule.Basis, rule, sink);
								rules.Add(Pair.Create(rule, methodBody));
								stmts[i] = null; // remove processed rules from the list
							}
						}
					}
				}
			}

			if (rules.Count == 0)
				sink.Write(MessageSink.Warning, node, "No grammar rules were found in LLLPG block");

			// Parse the rule definitions (now that we know the names of all the 
			// rules, we can decide if an Id refers to a rule; if not, it's assumed
			// to refer to a terminal).
			new StageTwoParser(helper, sink).Parse(rules);
			
			// Process the grammar & generate code
			var lllpg = new LLParserGenerator(helper);
			lllpg.OutputMessage += (node_, pred, type, msg) => { sink.Write(type, (object)node_ ?? pred, msg); };
			ApplyOptions(node, lllpg, sink); // Read attributes such as [DefaultK(3)]
			foreach (var pair in rules)
				lllpg.AddRule(pair.A);
			
			// TODO: change lllpg so we can interleave generated code with other 
			// user code, to preserve the order of the original code.
			var results = lllpg.GenerateCode(node.Source);
			return F.Call(S.Splice, stmts.Where(p => p != null).Concat(results.Args));
		}

		private static void ApplyOptions(LNode node, LLParserGenerator lllpg, IMessageSink sink)
		{
			for (int i = 0; i < node.Attrs.Count; i++) {
				var attr = node.Attrs[i];
				switch (attr.Name.Name) {
					case "FullLLk":
						ReadOption<bool>(sink, attr, v => lllpg.FullLLk = v, true);
						break;
					case "Verbosity":
						ReadOption<int>(sink, attr, v => lllpg.Verbosity = v, null);
						break;
					case "NoDefaultArm":
						ReadOption<bool>(sink, attr, v => lllpg.NoDefaultArm = v, null);
						break;
					case "DefaultK":
						ReadOption<int>(sink, attr, v => lllpg.DefaultK = v, null);
						break;
					default:
						sink.Write(MessageSink.Error, attr,
							"Unrecognized attribute. LLLPG supports the following options: " +
							"FullLLk(bool), Verbosity(0..3), NoDefaultArm(bool), and DefaultK(1..9)");
						break;
				}
			}
		}

		private static void ApplyRuleOptions(ref LNode node, Rule rule, IMessageSink sink)
		{
			node = node.WithAttrs(node.Attrs.Select(attr => {
				switch (attr.Name.Name) {
					case "fullLLk": case "FullLLk":
						ReadOption<bool>(sink, attr, v => rule.FullLLk = v, true);
						break;
					case "private": case "#private": case "priv": case "Private":
						ReadOption<bool>(sink, attr, v => rule.IsPrivate = v, true);
						break;
					case "token": case "Token":
						ReadOption<bool>(sink, attr, v => rule.IsToken = v, true);
						break;
					case "start": case "Start":
						ReadOption<bool>(sink, attr, v => rule.IsStartingRule = v, true);
						break;
					case "extern": case "Extern":
						ReadOption<bool>(sink, attr, v => rule.IsExternal = v, true);
						break;
					case "k": case "K":
						ReadOption<int>(sink, attr, k => rule.K = k, null);
						break;
					default:
						return attr;
				}
				return null;
			}).WhereNotNull().ToArray());
		}

		private static void ReadOption<T>(IMessageSink sink, LNode attr, Action<T> setter, T? defaultValue) where T:struct
		{
			if (attr.ArgCount > 1 || (attr.ArgCount == 0 && defaultValue == null))
				sink.Write(MessageSink.Error, attr, Localize.From("{0}: one parameter expected", Signature(attr, typeof(T), defaultValue)));
			else if (attr.ArgCount == 1) {
				if (attr.Args[0].Value is T)
					setter((T)attr.Args[0].Value);
				else
					sink.Write(MessageSink.Error, attr, Localize.From("{0}: literal of type «{1}» expected", Signature(attr, typeof(T), defaultValue), typeof(T).Name));
			} else
				setter(defaultValue.Value);
		}
		private static string Signature(LNode attr, Type type, object defaultValue)
		{
			return string.Format(defaultValue == null ? "{0}({1})" : "{0}({1} = {2})",
				attr.Name, type.Name, defaultValue);
		}
	}
}
