// Generated from MatchMacro.ecs by LeMP custom tool. LeMP version: 2.7.1.1
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using LeMP.CSharp7.To.OlderVersions;
using S = Loyc.Syntax.CodeSymbols;

namespace LeMP
{
	partial class StandardMacros
	{
		[LexicalMacro("match (var) { case ...: ... }; // In LES, use a => b instead of case a: b", 
		"Attempts to match and deconstruct an object against \"patterns\", such as tuples, ranges or an algebraic data type. Example:\n" + 
		"match (obj) {  \n" + 
		"   case is Shape(ShapeType.Circle, $size, Location is Point<int> $p($x, $y)): \n" + 
		"      Circle(size, x, y); \n" + 
		"}\n\n" + 
		"This is translated to the following C# code: \n" + 
		"do { \n" + 
		"   Point<int> p; \n" + 
		"   Shape tmp1; \n" + 
		"   if (obj is Shape) { \n" + 
		"      var tmp1 = (Shape)obj; \n" + 
		"      if (tmp1.Item1 == ShapeType.Circle) { \n" + 
		"         var size = tmp1.Item2; \n" + 
		"         var tmp2 = tmp1.Location; \n" + 
		"         if (tmp2 is Point<int>) { \n" + 
		"            var p = (Point<int>)tmp2; \n" + 
		"            var x = p.Item1; \n" + 
		"            var y = p.Item2; \n" + 
		"            Circle(size, x, y); \n" + 
		"            break; \n" + 
		"         } \n" + 
		"      }\n" + 
		"   }\n" + 
		"} while(false); \n" + 
		"`break` is not required at the end of each handler (`case` code block), but it can " + 
		"be used to exit early from a `case`. You can associate multiple patterns with the same " + 
		"handler using `case pattern1, pattern2:` in EC#, but please note that (due to a " + 
		"limitation of plain C#) this causes code duplication since the handler will be repeated " + 
		"for each pattern.")] 
		public static LNode match(LNode node, IMacroContext context)
		{
			{
				LNode input;
				VList<LNode> contents;
				if (node.Args.Count == 2 && (input = node.Args[0]) != null && node.Args[1].Calls(CodeSymbols.Braces)) {
					contents = node.Args[1].Args;
					var outputs = new WList<LNode>();
					input = MaybeAddTempVarDecl(context, input, outputs);
				
					// Process the braced block, one case at a time
					int next_i = 0;
					for (int case_i = 0; case_i < contents.Count; case_i = next_i) {
						var @case = contents[case_i];
						if (!IsCaseLabel(@case)	// `case ...:` or `default:`
						)
							return Reject(context, contents[0], "In 'match': expected 'case' statement");
						// Find the end of the current case/default block
						for (next_i = case_i + 1; next_i < contents.Count; next_i++) {
							var stmt = contents[next_i];
							if (IsCaseLabel(stmt))
								break;
							if (stmt.Calls(S.Break, 0)) {
								next_i++;
								break;
							}
						}
						//  handler: the list of statements underneath `case`
						var handler = new VList<LNode>(contents.Slice(case_i + 1, next_i - (case_i + 1)));
					
						if (@case .Calls(S.Case) && @case .Args.Count > 0) {
							var codeGen = new CodeGeneratorForMatchCase(context, input, handler);
							foreach (var pattern in @case .Args)
								outputs.Add(codeGen.GenCodeForPattern(pattern));
						} else {	// default:
							// Note: the extra {braces} around the handler are rarely 
							// needed. They are added just in case the handler declares a 
							// variable and a different handler declares another variable 
							// by the same name, which is illegal unless we add braces.
							outputs.Add(LNode.Call(CodeSymbols.Braces, LNode.List(handler)).SetStyle(NodeStyle.StatementBlock));
							if (next_i < contents.Count)
								context.Sink.Error(@contents[next_i], "The default branch must be the final branch in a 'match' statement.");
						}
					}
					return LNode.Call(CodeSymbols.DoWhile, LNode.List(outputs.ToVList().AsLNode(S.Braces), LNode.Literal(false)));
				}
			}
			return null;
		}
	
		static bool IsCaseLabel(LNode @case) {
			if (@case .Calls(CodeSymbols.Case) || @case .Calls(CodeSymbols.Label, 1) && @case .Args[0].IsIdNamed((Symbol) "'default")) return true;
			return false;
		}
	
		/** This class is for generating code for a single case pattern.

			Microsoft C# plan as of 2018-04
			-------------------------------

			The C# team's official plan for pattern syntax looks like [this]
			(https://github.com/dotnet/csharplang/blob/master/proposals/patterns.md)
			after I rearrange it for easier comprehension (that document 
			refers to "argument_name" which is left undefined, but it seems 
			to refer to "argument-name" in the C# 5 grammar which has a syntax
			of `identifier ':'`):

			pattern
				: Type id
				| Type id { comma-separated list of property_subpattern }
				| Type '(' comma-separated list of subpattern ')'
				| Type '{' comma-separated list of property_subpattern '}'
				| constant expression
				| '(' comma-separated list of subpattern ')'
				| '{' comma-separated list of property_subpattern '}' ;
			subpattern: pattern | identifier ':' pattern              ;
			property_subpattern: identifier 'is' pattern              ;

			This seems out of date; I think the newer syntax is issue [#1054]
			(https://github.com/dotnet/csharplang/issues/1054) which is like...

			pattern
				: expression
				| Type id
				| Type '(' subpatterns? ')' property_subpattern? id?
				| Type property_subpattern id?
				| '(' subpatterns? ')' property_subpattern? id?
				| property_subpattern id?                         ;
			subpatterns : subpattern | subpattern ',' subpatterns ;
			subpattern : pattern | identifier ':' pattern         ;
			property_subpattern : '{' subpatterns? '}'            ;

			The actual grammar says `property_subpattern? simple_designation?` and
			I'm inferring that a simple_designation is really an identifier.
			However it looks like `var (tuple, parts)` might be a new syntax
			they are adding for simple_designation...

			Property subpatterns (in braces) are required to start with 
			`identifier ':'`. So what's the difference between `(Foo: pattern)` 
			and `{Foo: pattern}`? It looks like parentheses cause an "operator is" 
			function to be invoked, whereas braces cause a sequence of properties 
			to be matched. Because a pattern can be an expression, it's ambiguous
			whether `(Foo) id` is a cast or, I guess, a request to create a 
			variable named `id` if the value matches `Foo` (whatever that means), 
			and the team decided that it would be a cast. It's also ambiguous 
			whether the pattern `Foo` matches a type called `Foo` or a value 
			called `Foo`, and the older document says it will match the type 
			preferentially. In addition (Foo) could be considered either an 
			expression in parentheses or a "positional deconstruct", and it is
			currently preferred to treat it as an expression.

			Finally, patterns in `switch` can have a `when` clause appended.

			Pattern syntax: old and new
			---------------------------

			The old syntax looked like this (creating variables s, n, ds): 

				case $s is Shape(Kind: $n, Style: $ds is DrawStyle) in area:

			Given the new C# 7 syntax (`x is T y`) this doesn't seem quite 
			right anymore. Unfortunately MS's planned syntax is completely 
			different, highly ambiguous with the existing expression syntax,
			and IMO very strange. Since it's not finalized I'm not going to
			try to parse that monstrosity yet, expecially since LeMP can't come
			anywhere close to simulating its semantics.
			
			Instead I'm adjusting the current syntax to fit better into C# 7
			and to be simpler to analyze in the `match` macro (the old syntax 
			supported variations like `a in b is c(d)` and `a is b in c(d)` but
			I'm retiring that first form). A pattern now will be either (1) a 
			literal or (2) a subset of the following components:

			1. Property name against which to pattern-match
			2. A type check (`is DerivedClass`)
			3. A variable name to hold the property after type conversion
			4. A range check (`in x..y`)
			
			So, here is a list of allowed subpattern forms in the new 
			implementation (where Range is a range expression such as x..y):

			    PropName: is DerivedClass name(...) in Range
			    PropName  is DerivedClass name(...) in Range
			    PropName: is DerivedClass name(...)
			    PropName  is DerivedClass name(...)
			    PropName: is DerivedClass(...) in Range
			    PropName  is DerivedClass(...) in Range
			    PropName: is DerivedClass(...)
			    PropName  is DerivedClass(...)
			    PropName: is var name(...) in Range
			    PropName  is var name(...) in Range
			    PropName: is var name(...)
			    PropName  is var name(...)
			    PropName: (...) in Range
			    PropName: (...)
			    PropName: Range
				PropName: _

			In most of these, the list of subpatterns `(...)` is optional, but if
			`is` and `(...)` are omitted then you cannot write `PropName: in Range` 
			but must use `PropName: Range` instead because there is no unary `in` 
			operator. It is ambiguous whether "PropName(...)" is intended as a 
			match expr or as an ordinary method call, so this form produces a 
			warning that can be fixed by adding parens or by adding a colon.
			
			The old form `PropName: $name is DerivedClass(...) in Range` will also
			be allowed.
		*/
		class CodeGeneratorForMatchCase
		{
			protected IMacroContext _context;
			protected LNode _input;
			protected VList<LNode> _handler;
			internal CodeGeneratorForMatchCase(IMacroContext context, LNode input, VList<LNode> handler)
			{
				_context = context;
				_input = input;
				_handler = handler;
				var @break = LNode.Call(CodeSymbols.Break);
				if (_handler.IsEmpty || !_handler.Last.Equals(@break))
					_handler.Add(@break);
			}
			internal LNode GenCodeForPattern(LNode pattern)
			{
				_output = new List<Pair<Mode, LNode>>();
				GenCodeForPattern(_input, pattern);
				return GetOutputAsLNode();
			}
		
			// _output is a list of conditions and statements in the order they 
			// must be executed to check whether a single case pattern matches.
			List<Pair<Mode, LNode>> _output;
			enum Mode { Statement, Condition }
			void PutStmt(LNode stmt) { _output.Add(Pair.Create(Mode.Statement, stmt)); }
			void PutCond(LNode cond) { _output.Add(Pair.Create(Mode.Condition, cond)); }
		
			void GenCodeForPattern(LNode input, LNode pattern, string defaultPropName = null)
			{
				// Get the parts of the pattern, e.g. `$x is T(sp)` => varBinding=x, isType=T, sp is returned
				bool refExistingVar;
				LNode varBinding, cmpExpr, isType, inRange, propName;
				VList<LNode> subPatterns, conditions;
				GetPatternComponents(pattern, out propName, out varBinding, out refExistingVar, out cmpExpr, out isType, out inRange, out subPatterns, out conditions);
			
				if (defaultPropName == null) {	// Outermost pattern
					if (propName != null)
						_context.Sink.Error(propName, "match: property name not allowed on outermost pattern");
				} else {
					if ((propName = propName ?? LNode.Id(defaultPropName, pattern)) != null)
						input = LNode.Call(CodeSymbols.Dot, LNode.List(input, propName)).SetStyle(NodeStyle.Operator);
				}
			
				// For a pattern like `is Type varBinding(subPatterns) in A...B && conds`, 
				// our goal is to generate code like this:
				//
				//   var tmp_1 = $input; // temp var created unless $input looks simple
				//   if (tmp_1 is Type) {
				//     Type varBinding = (Type)tmp_1;
				//     if (varBinding >= A && varBinding <= B && /* code for matching subPatterns */)
				//         if (conds)
				//             $handler;
				//   }
				if (isType != null) {
					if ((cmpExpr ?? inRange ?? varBinding) != null) {
						// input will be used multiple times, so consider making a tmp var.
						if (!LooksLikeSimpleValue(input))
							PutStmt(TempVarDecl(_context, input, out input));
					}
				
					PutCond(LNode.Call(CodeSymbols.Is, LNode.List(input, isType)).SetStyle(NodeStyle.Operator));
				
					if (varBinding == null && ((cmpExpr ?? inRange) != null || subPatterns.Count > 0))
						// we'll need another temp variable to hold the same value, casted.
						varBinding = LNode.Id(NextTempName(_context), isType);
				}
			
				if (varBinding != null) {
					if (isType != null) {
						if (refExistingVar)
							PutStmt(LNode.Call(CodeSymbols.Assign, LNode.List(varBinding, LNode.Call(CodeSymbols.Cast, LNode.List(input, isType)).SetStyle(NodeStyle.Operator))).SetStyle(NodeStyle.Operator));
						else
							PutStmt(LNode.Call(CodeSymbols.Var, LNode.List(isType, LNode.Call(CodeSymbols.Assign, LNode.List(varBinding, LNode.Call(CodeSymbols.Cast, LNode.List(input, isType)).SetStyle(NodeStyle.Operator))))));
					} else {
						if (refExistingVar)
							PutStmt(LNode.Call(CodeSymbols.Assign, LNode.List(varBinding, input)).SetStyle(NodeStyle.Operator));
						else
							PutStmt(LNode.Call(CodeSymbols.Var, LNode.List(LNode.Missing, LNode.Call(CodeSymbols.Assign, LNode.List(varBinding, input)))));
					}
					input = varBinding;
				}
			
				if (cmpExpr != null) {	// do equality test
					if (cmpExpr.Value == null)
						PutCond(LNode.Call(CodeSymbols.Eq, LNode.List(input, LNode.Literal(null))).SetStyle(NodeStyle.Operator));
					else
						PutCond(LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(cmpExpr, LNode.Id((Symbol) "Equals"))).SetStyle(NodeStyle.Operator), LNode.List(input)));
				}
			
				// Generate code for subpatterns
				for (int itemIndex = 0; itemIndex < subPatterns.Count; itemIndex++) {
					var subPattern = subPatterns[itemIndex];
					GenCodeForPattern(input, subPattern, "Item" + (itemIndex + 1));
				}
			
				if (inRange != null) {
					PutCond(LNode.Call(CodeSymbols.In, LNode.List(input, inRange)).SetStyle(NodeStyle.Operator));
				}
			
				foreach (var cond in conditions)
					PutCond(cond);
			}
		
			void GetPatternComponents(LNode pattern, out LNode propName, 
				out LNode varBinding, out bool refExistingVar, 
				out LNode cmpExpr, out LNode isType, out LNode inRange, 
				out VList<LNode> subPatterns, out VList<LNode> conditions)
			{
				// Format: PropName: is DerivedClass name(...) in Range
				// Here's a typical pattern (case expr):
				//  is Shape(ShapeType.Circle, ref size, Location: p is Point<int>(x, y))
				// When there is an arg list, we decode its Target and return the args.
				//
				// The caller is in charge of stripping out "Property:" prefix, if any,
				// so the most complex pattern that this method considers is something 
				// like `expr is Type x(subPatterns) in Range && conds` where `expr` is 
				// a varName or $varName to deconstruct, or some expression to test for 
				// equality. Assuming it's an equality test, the output will be
				//
				//   varBinding = null
				//   refExistingVar = false
				//   cmpExpr = quote(expr);
				//   isType = quote(Type);
				//   inRange = quote(Range);
				//   conds will have "conds" pushed to the front.
				// 
				subPatterns = VList<LNode>.Empty;
				refExistingVar = pattern.AttrNamed(S.Ref) != null;
			
				propName = varBinding = cmpExpr = isType = inRange = null;
				// Deconstruct `PropName: pattern` (fun fact: we can't use `matchCode` 
				// to detect a named parameter here, because if we write 
				// `case { $propName: $subPattern; }:` it is parsed as a goto-label, 
				// not as a named parameter.)
				if (pattern.Calls(S.NamedArg, 2) || pattern.Calls(S.Colon, 2)) {
					propName = pattern[0]; pattern = pattern[1];
				}
				// Deconstruct `pattern && condition` (iteratively)
				conditions = VList<LNode>.Empty;
				while (pattern.Calls(S.And, 2)) {
					conditions.Add(pattern.Args.Last);
					pattern = pattern.Args[0];
				}
			
				{
					LNode lhs;
					if (pattern.Calls(CodeSymbols.In, 2) && (lhs = pattern.Args[0]) != null && (inRange = pattern.Args[1]) != null || pattern.Calls((Symbol) "in", 2) && (lhs = pattern.Args[0]) != null && (inRange = pattern.Args[1]) != null || pattern.Calls(CodeSymbols.In, 2) && (lhs = pattern.Args[0]) != null && (inRange = pattern.Args[1]) != null)
						pattern = lhs;
				}
				// Deconstruct `PropName is Type` with optional list of subpatterns.
				// In LES let's accept ``PropName `is` (Type `with` (subpatterns))`` instead.
				LNode subpatterns = null;
				{
					LNode lhs, type;
					if (pattern.Calls(CodeSymbols.Is, 2) && (lhs = pattern.Args[0]) != null && (type = pattern.Args[1]) != null || pattern.Calls(CodeSymbols.Is, 3) && (lhs = pattern.Args[0]) != null && (type = pattern.Args[1]) != null && (subpatterns = pattern.Args[2]) != null || pattern.Calls((Symbol) "is", 2) && (lhs = pattern.Args[0]) != null && (type = pattern.Args[1]) != null || pattern.Calls(CodeSymbols.Is, 2) && (lhs = pattern.Args[0]) != null && (type = pattern.Args[1]) != null) {
						if (subpatterns == null) {
							if (type.Calls((Symbol) "with", 2) && (isType = type.Args[0]) != null && (subpatterns = type.Args[1]) != null || type.Calls((Symbol) "'with", 2) && (isType = type.Args[0]) != null && (subpatterns = type.Args[1]) != null) { }
						}
						if (type.Calls(CodeSymbols.Var, 2) && (isType = type.Args[0]) != null && (varBinding = type.Args[1]) != null) { } else isType = type;
						if (lhs.Calls(S.Substitute, 1)) {
							if (varBinding != null)
								_context.Sink.Error(varBinding, "match: cannot bind two variable names to one value");
							varBinding = lhs[0];
						} else if (propName != null) {
							_context.Sink.Error(varBinding, "match: property name already set to {0}", propName.Name);
							if (varBinding == null)
								varBinding = lhs;	// assume it was intended as a variable binding
						}
						if (type.IsIdNamed("")	// is var x
						)
							isType = null;
					} else if (pattern.Calls(CodeSymbols.DotDotDot, 2) || pattern.Calls(CodeSymbols.DotDot, 2) || pattern.Calls(CodeSymbols.DotDotDot, 1) || pattern.Calls(CodeSymbols.DotDot, 1))
						inRange = pattern;
					else if (pattern.Calls(CodeSymbols.AltList) || pattern.Calls(CodeSymbols.Tuple))
						subpatterns = pattern;
					else if (pattern.Calls(S.Substitute, 1))
						varBinding = pattern[0];
					else
						cmpExpr = pattern;
				}
				if (subpatterns != null) {
					if (subpatterns.Calls(S.Tuple) || subpatterns.Calls(S.AltList))
						subPatterns = subpatterns.Args;
					else
						_context.Sink.Error(subpatterns, "match: expected list of subpatterns (at '{0}')", subpatterns);
				}
				if (cmpExpr != null) {
					if (cmpExpr.IsIdNamed(__))
						cmpExpr = null;
					else if (refExistingVar && varBinding == null) {	// Treat `ref expr` as var binding
						varBinding = cmpExpr;
						cmpExpr = null;
					}
				}
				if (varBinding != null) {
					if (varBinding.AttrNamed(S.Ref) != null) {
						varBinding = varBinding.WithoutAttrNamed(S.Ref);
						refExistingVar = true;
					} else if (varBinding.IsIdNamed(__)) {
						varBinding = null;
					} else if (!varBinding.IsId) {
						_context.Sink.Error(varBinding, "match: expected variable name (at '{0}')", varBinding);
						varBinding = null;
					}
				}
			}
		
			LNode GetOutputAsLNode()
			{
				WList<LNode> finalOutput = _handler.ToWList();
				for (int end = _output.Count - 1; end >= 0; end--) {
					var tmp_10 = _output[end];
					Mode mode = tmp_10.Item1;
					LNode code = tmp_10.Item2;
					if (mode == Mode.Condition) {
						// Merge adjacent conditions into the same if-statement
						int start = end;
						for (; start > 0 && _output[start - 1].A == mode; start--) { }
						LNode cond = _output[start].B;
						for (int i = start + 1; i <= end; i++)
							cond = LNode.Call(CodeSymbols.And, LNode.List(cond, _output[i].B)).SetStyle(NodeStyle.Operator);
						end = start;
					
						finalOutput = new WList<LNode> { 
							LNode.Call(CodeSymbols.If, LNode.List(cond, finalOutput.ToVList().AsLNode(S.Braces)))
						};
					} else
						finalOutput.Insert(0, code);
				}
				return finalOutput.ToVList().AsLNode(S.Braces);
			}
		}
	
	}
}