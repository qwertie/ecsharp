using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Syntax;
using Loyc.Utilities;
using Loyc.Collections;
using Loyc.Syntax.Lexing;
using Loyc.Math;
using Loyc.Collections.Impl;
using Loyc.Syntax.Les;
using LeMP;
using S = Loyc.Syntax.CodeSymbols;

/// <summary>
/// Contains macros for using LLLPG in LeMP.
/// </summary>
namespace Loyc.LLPG
{
	using Loyc.LLParserGenerator;

	/// <summary>
	/// Macros for using LLLPG in LeMP.
	/// </summary>
	/// <remarks>
	/// Example:
	/// <code>
	///   class Foo { 
	///     [DefaultK(2)] LLLPG lexer
	///     {
	///       private rule Int  @{ '0'..'9'+ };
	///       private rule Id   @{ 'a'..'z'|'A'..'Z' ('a'..'z'|'A'..'Z'|'0'..'9'|'_')* };
	///       token Token  @{ Int | Id };
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
	/// <li>The stage-one rule() macro uses <see cref="StageOneParser"/> to
	/// translate token trees into expressions, e.g. <c>@[ ("Foo" | bar)* ~';' ]</c> 
	/// is currently translated to <c>@'tuple(@`'suf*`("Foo" | bar), ~';')</c>.</li>
	/// <li>The stage-two macro is named run_LLLPG(). It accepts the code-gen 
	/// helper created by the LLLPG(lexer) or LLLPG(parser) macro, and it
	/// has the ProcessChildrenBefore flag so that the stage-1 rule() macros 
	/// run first. run_LLLPG calls <see cref="StageTwoParser"/> to translate 
	/// expressions into <see cref="Pred"/> objects, and then invokes 
	/// <see cref="LLParserGenerator"/> to analyze the grammar and generate 
	/// code.</li>
	/// </ol>
	/// </remarks>
	[ContainsMacros]
	public static class Macros
	{
		static readonly Symbol _rule = GSymbol.Get("rule");
		static readonly Symbol _hash_rule = GSymbol.Get("#rule");
		static readonly Symbol _token = GSymbol.Get("token");
		static readonly Symbol _hash_token = GSymbol.Get("#token");
		static readonly Symbol _term = GSymbol.Get("term");
		static readonly Symbol _def = GSymbol.Get("def");
		static readonly Symbol _lexer = GSymbol.Get("lexer");
		static readonly Symbol _parser = GSymbol.Get("parser");
		static readonly Symbol _seq = GSymbol.Get("#seq");
		static readonly Symbol _recognizer = GSymbol.Get("recognizer");
		static readonly Symbol _run_LLLPG = GSymbol.Get("run_LLLPG");
		static readonly LNodeFactory F = new LNodeFactory(new EmptySourceFile("LLLPG"));
		public static readonly Symbol MacroNamespace = GSymbol.Get("Loyc.LLPG");

		/// <summary>Helper macro that translates <c>lexer</c> in <c>LLLPG(lexer, {...})</c> 
		/// into a <see cref="IntStreamCodeGenHelper"/> object.</summary>
		[LexicalMacro("LLLPG lexer {Body...}", "Runs LLLPG in lexer mode (via IntStreamCodeGenHelper)", "LLLPG", Mode = MacroMode.Normal)]
		public static LNode LLLPG_lexer(LNode node, IMacroContext context)
		{
			return LllpgMacro(node, context, _lexer, lexerCfg =>
			{
				var helper = new IntStreamCodeGenHelper();
				foreach (var option in MacroContext.GetOptions(lexerCfg.Args))
				{
					LNode value = option.Value ?? LNode.Missing;
					string key = option.Key.Name.Name;
					switch (key.ToLowerInvariant()) {
						case "inputsource":      helper.InputSource = value; break;
						case "inputclass":       helper.InputClass = value; break;
						case "terminaltype":     helper.TerminalType = value; break;
						case "settype":          helper.SetType = value; break;
						case "listinitializer":  helper.SetListInitializer(value); break;
						case "nocheckbydefault": SetOption<bool>(context, option.Key, value.Value, b => helper.NoCheckByDefault = b); break;
						default:
							context.Sink.Error(option.Key, "Unrecognized option '{0}'. Available options: " +
								"InputSource: var, InputClass: type, TerminalType: type, SetType: type, "+
								"ListInitializer: var _ = new List<T>(), NoCheckByDefault: true", key);
							break;
					}
				}
				return helper;
			});
		}

		/// <summary>Helper macro that translates <c>parser</c> in <c>LLLPG(parser, {...})</c> 
		/// into a <see cref="GeneralCodeGenHelper"/> object.</summary>
		[LexicalMacro("LLLPG {Body...}; LLLPG parser {Body...}; LLLPG parser(option(value), ...) {Body...}", "Runs LLLPG in general-purpose mode (via GeneralCodeGenHelper)", "LLLPG",
			Mode = MacroMode.Normal)]
		public static LNode LLLPG_parser(LNode node, IMacroContext context)
		{
			return LllpgMacro(node, context, _parser, parserCfg => {
				// Scan options in parser(...) node
				var helper = new GeneralCodeGenHelper();
				if (parserCfg == null)
					return helper;
				foreach (var option in MacroContext.GetOptions(parserCfg.Args))
				{
					LNode value = option.Value ?? LNode.Missing;
					string key = option.Key.Name.Name;
					switch (key.ToLowerInvariant()) {
						case "inputsource":     helper.InputSource = value; break;
						case "inputclass":      helper.InputClass = value; break;
						case "terminaltype":    helper.TerminalType = value; break;
						case "settype":         helper.SetType = value;   break;
						case "listinitializer": helper.SetListInitializer(value); break;
						case "nocheckbydefault":SetOption<bool>(context, option.Key, value.Value, b => helper.NoCheckByDefault = b); break;
						case "allowswitch":     SetOption<bool>(context, option.Key, value.Value, b => helper.AllowSwitch = b); break;
						case "castla":          SetOption<bool>(context, option.Key, value.Value, b => helper.CastLA = b); break;
						case "latype":          helper.LaType = value;    break;
						case "matchtype":       // alternate name
						case "matchcast":       helper.MatchCast = value; break;
						default:
							context.Sink.Error(option.Key, "Unrecognized option '{0}'. Available options: "+
								"InputSource: variable, InputClass: type, TerminalType: type, SetType: type, "+
								"ListInitializer: var _ = new List<T>(), NoCheckByDefault: true, AllowSwitch: bool, "+
								"CastLA: bool, LAType: type, MatchCast: type", key);
							break;
					}
				}
				return helper;
			}, isDefault: true);
		}

		/// <summary>This method helps do the stage-one transform from <c>LLLPG (config) {...}</c>
		/// to <c>run_LLLPG (helper literal) {...}</c> and also invokes the ANTLR-style 
		/// parser if the second argument is a token literal. If <c>node[0]</c> 
		/// calls <c>expectedConfigNode</c> then the delegate is called to 
		/// construct a code generation helper object; otherwise, this method has 
		/// no effect and returns null.</summary>
		public static LNode LllpgMacro(LNode node, IMacroContext context, 
			Symbol expectedCodeGenMode, Func<LNode, IPGCodeGenHelper> makeCodeGenHelper, bool isDefault = false)
		{
			LNodeList args, body;
			LNode tokenTree = null, codeGenOptions = null;

			if (node.ArgCount >= 1 && (tokenTree = node.Args.Last).Value is TokenTree) {
				args = node.Args.WithoutLast(1);
				body = LNodeList.Empty;
			} else {
				tokenTree = null;
				var p = context.GetArgsAndBody(orRemainingNodes: true);
				args = p.A;
				body = p.B;
			}

			if ((args.Count == 1 && (codeGenOptions = args[0]).Name == expectedCodeGenMode)
				|| (args.Count == 0 && isDefault))
			{
				if (tokenTree != null)
					body = AntlrStyleParser.ParseTokenTree(tokenTree.Value as TokenTree, context.Sink);
				IPGCodeGenHelper helper = makeCodeGenHelper(codeGenOptions);
				return node.WithTarget(_run_LLLPG).WithArgs(F.Literal(helper), F.Braces(body));
			}
			return null;
		}

		[LexicalMacro("rule Name Body; rule Name::Type Body; rule Name(Args...)::Type Body",
			"Declares a rule for use inside an LLLPG block. The 'Body' can be a token literal @{...} or a code block that contains token literals {...@[...]...}.",
			"rule", "token", Mode = MacroMode.NoReprocessing | MacroMode.Passive)]
		public static LNode rule(LNode node, IMacroContext context)
		{
			bool isToken;
			if ((isToken = node.Calls(_token, 2)) || node.Calls(_rule, 2)) {
				node = context.PreProcessChildren();
				LNode sig = node.Args[0];
				// Ugh. Because the rule has been macro-processed, "rule X::Y ..." 
				// has become "rule #var(Y,X) ...". We must allow this, because in 
				// case of something like "rule X(arg::int)::Y" we actually do want
				// the argument to become `#var(int, arg)`; so just reverse the
				// transform that we didn't want.
				if (sig.Calls(S.Var, 2))
					sig = F.Call(S.ColonColon, sig.Args[1], sig.Args[0]);

				LNode name = sig, returnType = F.Void;
				if (sig.Calls(S.ColonColon, 2)) {
					returnType = sig.Args[1];
					name = sig.Args[0];
				}
				if (LeMP.Prelude.Les.Macros.IsComplexId(name))
					name = F.Call(name); // def requires an argument list
				
				LNodeList args = name.Args;
				name = name.Target;
				
				LNode newBody = ParseRuleBody(node.Args[1], context);
				if (newBody != null)
					return node.With(isToken ? _hash_token : _hash_rule, 
						returnType, name, F.List(args), newBody);
			}
			return null;
		}

		//private static bool IsRule(LNode stmt, out LNode ruleBody, bool stage1)
		//{
		//    ruleBody = null;
		//    if (stmt.Calls(_rule, 2) || stmt.Calls(_token, 2)) {
		//        ruleBody = stmt.Args[1];
		//        if (ruleBody.Value is TokenTree)
		//            return stage1;
		//        return stage1 ? ruleBody.Calls(S.Braces) : true;
		//    }
		//    return false;
		//}

		[LexicalMacro("rule Name() @{...}; rule Name @{...}; rule Type Name() @{...}; rule Type Name @{...}",
			"Declares a rule for use inside an LLLPG block. The 'Body' can be a token literal @{...} or a code block that contains top-level token literals {...@{...};...}.",
			"#fn", "#property", Mode = MacroMode.Passive | MacroMode.NoReprocessing)]
		public static LNode ECSharpRule(LNode node, IMacroContext context)
		{
			// This will be called for all methods and properties, so we have to 
			// examine it for the earmarks of a rule definition.
			bool isProp;
			if (!(isProp = node.Calls(S.Property, 4)) && !node.Calls(S.Fn, 4))
				return null;
			LNode returnType = node.Args[0];
			bool isToken;
			bool retValIsRule = (isToken = returnType.IsIdNamed(_token)) || returnType.IsIdNamed(_rule);
			
			var attrs = node.Attrs;
			if (!retValIsRule) {
				int? i_rule = attrs.FinalIndexWhere(n => n.IsIdNamed(_hash_token) || n.IsIdNamed(_hash_rule));
				if (i_rule == null)
					return null;
				isToken |= attrs[i_rule.Value].IsIdNamed(_hash_token);
				attrs.RemoveAt(i_rule.Value);
			} else
				returnType = F.Void;

			//node = context.PreProcessChildren();

			LNode name = node.Args[1];
			LNode args = node.Args[2];
			if (args.IsIdNamed(S.Missing)) // @``
				args = F.List(); // output will be a #fn, which does not allow @`` as its arg list
			LNode newBody = ParseRuleBody(node.Args.Last, context);
			if (newBody != null)
				// #rule($returnType, $name, $args, $newBody)
				return LNode.Call(isToken ? _hash_token : _hash_rule, 
					LNode.List(returnType, name, args, newBody),
					node.Range, node.Style).WithAttrs(attrs);
			else
				return null;
		}

		private static LNode ParseRuleBody(LNode ruleBody, IMessageSink sink)
		{
			TokenTree ruleTokens;
			// Expecting @{...} or {...}
			if ((ruleTokens = ruleBody.Value as TokenTree) == null && !ruleBody.Calls(S.Braces))
				return null;

			if (ruleTokens != null)
				return StageOneParser.ParseTokenTree(ruleTokens, sink);
			else {
				if (ruleBody.Args.Any(stmt => stmt.Value is TokenTree))
					ruleBody = ruleBody.With(S.Tuple, ruleBody.Args.SmartSelect(stmt => ParseStmtInRule(stmt, sink)));
			}
			return ruleBody;
		}

		private static LNode ParseStmtInRule(LNode stmt, IMessageSink sink)
		{
			if (stmt.Value is TokenTree)
				return StageOneParser.ParseTokenTree((TokenTree)stmt.Value, sink);
			else
				return F.Braces(stmt);
		}

		[LexicalMacro("run_LLLPG(helper literal, {Body...})", "Runs the Loyc LL(k) Parser Generator on the specified Body, with a Helper object supplied by an auxiliary macro named LLLPG(...).")]
		public static LNode run_LLLPG(LNode node, IMacroContext context)
		{
			node = context.PreProcessChildren();

			IPGCodeGenHelper helper;
			LNode body;
			bool hasBraces = true;
			if (node.ArgCount != 2 
				|| (helper = node.Args[0].Value as IPGCodeGenHelper) == null 
				|| !(hasBraces = (body = node.Args[1]).Calls(S.Braces)))
			{
				string msg = Localize.Localized("Expected run_LLLPG(helper_object, {...}).");
				if (hasBraces) msg = " " + Localize.Localized("An auxiliary macro is required to supply the helper object.");
				context.Write(Severity.Note, node, msg);
				return null;
			}
			helper = helper ?? new GeneralCodeGenHelper();
			
			var rules = new List<Pair<Rule, LNode>>();
			var stmts = new List<LNode>();

			// Let helper preprocess the code if it wants to
			foreach (var stmt in body.Args) {
				var stmt2 = helper.VisitInput(stmt, context) ?? stmt;
				if (stmt2.Calls(S.Splice))
					stmts.AddRange(stmt2.Args);
				else
					stmts.Add(stmt2);
			}

			// Find rule definitions, create Rule objects
			for (int i = 0; i < stmts.Count; i++)
			{
				LNode stmt = stmts[i];
				bool isToken;
				if ((isToken = stmt.Calls(_hash_token, 4)) || stmt.Calls(_hash_rule, 4)) {
					LNode basis = stmt.WithTarget(S.Fn);
					LNode methodBody = stmt.Args.Last;

					// basis has the form #fn(ReturnType, Name, #(Args))
					var rule = MakeRuleObject(isToken, ref basis, context);
					if (rule != null) {
						var prev = rules.FirstOrDefault(pair => pair.A.Name == rule.Name);
						if (prev.A != null)
							context.Sink.Error(rule.Basis, "The rule name «{0}» was used before at {1}", rule.Name, prev.A.Basis.Range.Start);
						else
							rules.Add(Pair.Create(rule, methodBody));

						stmts[i] = null; // remove processed rules from the list
					}
				} else {
					if (stmt.Calls(_rule) || stmt.Calls(_token))
						context.Sink.Error(stmt, "A rule should have the form rule(Name(Args)::ReturnType, @{...})");
				}
			}

			if (rules.Count == 0)
				context.Sink.Warning(node, "No grammar rules were found in LLLPG block");

			// Parse the rule definitions (now that we know the names of all the 
			// rules, we can decide if an Id refers to a rule; if not, it's assumed
			// to refer to a terminal).
			new StageTwoParser(helper, context).Parse(rules);
			
			// Process the grammar & generate code
			var lllpg = new LLParserGenerator(helper, context);
			ApplyOptions(node, lllpg, context, rules.Select(p => p.Key)); // Read attributes such as [DefaultK(3)]
			foreach (var pair in rules)
				lllpg.AddRule(pair.A);
			
			// TODO: change lllpg so we can interleave generated code with other 
			// user code, to preserve the order of declarations in the original code.
			var results = lllpg.Run(node.Source);
			return F.Call(S.Splice, stmts.Where(p => p != null).Concat(results.Args));
		}

		private static Rule MakeRuleObject(bool isToken, ref LNode basis, IMacroContext context)
		{
			var name = basis.Args[1];
			if (name.CallsMin(S.Of, 1))
				name = name.Args[0];
			if (!name.IsId || name.Name.Name.IsOneOf("EOF", "any", "error", "default", "default_error", "greedy", "nongreedy")) {
				context.Sink.Error(name, "'{0}' is not allowed as a rule name", name);
				return null;
			} else {
				var rule = new Rule(basis, name.Name, null, true);
				rule.IsToken = isToken;
				ApplyRuleOptions(ref rule.Basis, rule, context);

				return rule;
			}
		}

		private static void ApplyOptions(LNode node, LLParserGenerator lllpg, IMacroContext sink, IEnumerable<Rule> rules)
		{
			foreach (var pair in MacroContext.GetOptions(node.Attrs))
			{
				LNode key = pair.Key;
				object value = pair.Value != null ? pair.Value.Value : null;
				switch (key.Name.Name) {
					case "FullLLk":
						SetOption<bool>(sink, key, value ?? G.BoxedTrue, v => lllpg.FullLLk = v);
						break;
					case "Verbosity":
						SetOption<int>(sink, key, value, v => lllpg.Verbosity = v);
						break;
					case "NoDefaultArm":
						SetOption<bool>(sink, key, value ?? G.BoxedTrue, v => lllpg.NoDefaultArm = v);
						break;
					case "LL": case "DefaultK": case "k": case "K": // [LL(k)] is preferred
						SetOption<int>(sink, key, value, v => lllpg.DefaultK = v);
						break;
					case "AddComments":
						SetOption<bool>(sink, key, value ?? G.BoxedTrue, v => lllpg.AddComments = v);
						break;
					case "AddCsLineDirectives":
						SetOption<bool>(sink, key, value ?? G.BoxedTrue, v => lllpg.AddCsLineDirectives = v);
						break;
					case "PrematchByDefault":
						SetOption<bool>(sink, key, value ?? G.BoxedTrue, v => lllpg.PrematchByDefault = v);
						break;
					default:
						if (!key.IsTrivia)
							sink.Error(key,
								"Unrecognized attribute. LLLPG supports the following options: " +
								"FullLLk(bool), Verbosity(0..3), NoDefaultArm(bool), DefaultK(1..9), AddComments(bool), AddCsLineDirectives(bool), and PrematchByDefault(bool)");
						break;
				}
			}
		}

		private static void ApplyRuleOptions(ref LNode node, Rule rule, IMacroContext context)
		{
			node = node.WithAttrs(node.Attrs.WhereSelect(attr => {
				if (attr.ArgCount > 1)
					return attr;
				LNode key = attr.Target ?? attr;
				object value = null;
				if (attr.ArgCount == 1)
					value = attr.Args[0].Value;
				switch (key.Name.Name) {
					case "fullLLk": case "FullLLk":
						SetOption<bool>(context, key, value ?? G.BoxedTrue, v => rule.FullLLk = v);
						break;
					case "#private": case "private":
						SetOption<bool>(context, key, value ?? G.BoxedTrue, v => rule.IsPrivate = v);
						return attr; // keep attribute
					case "#public": case "#internal": case "#protected":
					case "public": case "internal": case "protected": // this is before macros run, and non-special names are used in LES
						rule.IsPrivate = false;
						return attr; // keep attribute
					case "token": case "Token":
						SetOption<bool>(context, key, value ?? G.BoxedTrue, v => rule.IsToken = v);
						break;
					case "start": case "Start":
						SetOption<bool>(context, key, value ?? G.BoxedTrue, v => rule.IsStartingRule = v);
						break;
					case "#extern": case "extern": case "Extern":
						SetOption<bool>(context, key, value ?? G.BoxedTrue, v => rule.IsExternal = v);
						break;
					case "#inline": case "inline": case "Inline":
					case "#fragment": case "fragment":
						SetOption<bool>(context, key, value ?? G.BoxedTrue, v => rule.IsInline = v);
						break;
					case "k": case "K": case "LL":
						SetOption<int>(context, key, value, k => rule.K = k);
						break;
					case "recognizer": case "Recognizer":
						LNode sig = attr.Args[0, null];
						if (sig != null) {
							if (sig.Calls(S.Braces, 1))
								sig = sig.Args[0];

							// Invoke macros here so that LES code like "public fn Foo()::bool"
							// is transformed into a method signature.
							sig = context.PreProcess(sig);
						}
						if (sig != null && sig.CallsMin(S.Fn, 3))
							rule.MakeRecognizerVersion(sig).TryWrapperNeeded();
						else
							context.Sink.Error(sig, "'recognizer' expects one parameter, a method signature.");
						break;
					default:
						return attr;
				}
				return NoValue.Value;
			}).ToArray());
		}

		private static void SetOption<T>(IMessageSink errorSink, LNode optionName, object value, Action<T> setter)
		{
			if (value is T)
				setter((T)value);
			else
				errorSink.Error(optionName, "{0}: expected literal of type «{1}».", optionName.Name, typeof(T));
		}
	}
}
