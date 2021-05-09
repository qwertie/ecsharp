using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Ecs;
using Loyc.MiniTest;

namespace LeMP.Tests
{
	[TestFixture]
	public class TestMatchCodeMacro : MacroTesterBase
	{
		[Test]
		public void TestMatchCode()
		{
			TestLes(@"matchCode var { 777 => Yay(); 666 => Boo(); $_ => @Huh?(); }",
				@"#if(777.Equals(var.Value), { Yay(); }, 
					#if(666.Equals(var.Value), { Boo(); }, { @Huh?(); }));");
			TestEcs(@"matchCode(code) {
					case { '2' + 2; }: Weird();
				}", @"
					if (code.Calls(CodeSymbols.Add, 2) && '2'.Equals(code.Args[0].Value) && 2.Equals(code.Args[1].Value)) {
						Weird();
					}");
			TestEcs(@"matchCode(Get.Var) {
					case Do($stuff): Do(stuff);
				}", @"{
						var tmp_1 = Get.Var;
						LNode stuff;
						if (tmp_1.Calls((Symbol) ""Do"", 1) && (stuff = tmp_1.Args[0]) != null) {
							Do(stuff);
						}
					}"
				.Replace("tmp_1", "tmp_"+MacroProcessor.NextTempCounter));
			
			string expectOutput = @"{
					LNode id, lit;
					if ((lit = code) != null && lit.IsLiteral) {
						Literal();
					} else if ((id = code) != null && id.IsId) {
						Id();
					} else {
						Call();
					}
				}";
			// Use older condition syntaxes (id && cond, id[cond]) when input syntax is LES
			TestEcs(@"matchCode(code) { 
					$(lit && #.IsLiteral) => Literal(); 
					$(id[#.IsId]) => Id(); 
					$_ => Call();
				}",
				expectOutput);
			// Use new `when` operator in EC#
			Test(@"matchCode(code) { 
					case $(lit when #.IsLiteral): Literal(); 
					case $(id when #.IsId): Id(); 
					default: Call();
				}",
				EcsLanguageService.Value,
				expectOutput,
				EcsLanguageService.Value);

			TestEcs(@"matchCode(code) { 
					case foo, FOO:  Foo(); 
					case 1, 1.0:    One(); 
					case $whatever: Whatever();
				}", @"{
					LNode whatever;
					if (code.IsIdNamed((Symbol) ""foo"") || code.IsIdNamed((Symbol) ""FOO"")) {
						Foo();
					} else if (1.Equals(code.Value) || 1d.Equals(code.Value)) {
						One();
					} else if ((whatever = code) != null) {
						Whatever();
					}
				}");
			TestEcs(@"matchCode(code) { 
					case 1, one: One();
				}", @"
					if (1.Equals(code.Value) || code.IsIdNamed((Symbol) ""one"")) {
						One();
					}
				");
			TestEcs(@"matchCode(code) { 
					case $binOp($_, $_): BinOp(binOp);
				}", @"{
					LNode binOp;
					if (code.Args.Count == 2 && (binOp = code.Target) != null) {
						BinOp(binOp);
					}
				}");
			TestEcs(@"matchCode(code) { 
					case ($a, $b, $(...args), $c):                Three(a, b, c);
					case (null, $(...args)), ($first, $(..args)): Tuple(first, args);
					case ($(...args), $last):                     Unreachable();
					case ($(...args),):                           Tuple(args);
				}", @"{
					LNode a, b, c, first = null, last;
					LNodeList args;
					if (code.CallsMin(CodeSymbols.Tuple, 3) && (a = code.Args[0]) != null && (b = code.Args[1]) != null 
						&& (args = new LNodeList(code.Args.Slice(2, code.Args.Count - 3))).IsEmpty | true && (c = code.Args[code.Args.Count - 1]) != null) {
						Three(a, b, c);
					} else if (code.CallsMin(CodeSymbols.Tuple, 1) && code.Args[0].Value == null && (args = new LNodeList(code.Args.Slice(1))).IsEmpty | true 
						|| code.CallsMin(CodeSymbols.Tuple, 1) && (first = code.Args[0]) != null && (args = new LNodeList(code.Args.Slice(1))).IsEmpty | true) {
						Tuple(first, args);
					} else if (code.CallsMin(CodeSymbols.Tuple, 1) && (args = code.Args.WithoutLast(1)).IsEmpty | true && (last = code.Args[code.Args.Count - 1]) != null) {
						Unreachable();
					} else if (code.Calls(CodeSymbols.Tuple) && (args = code.Args).IsEmpty | true) {
						Tuple(args);
					}
				}");
			TestEcs(@"matchCode(code) { 
					case $x = $y:
						Assign(x, y);
					case [$(..attrs)] $x = $(x_ && #.Equals(x)) + 1, 
					     [$(..attrs)] $op($x, $y):
						Handle(attrs);
						Handle(x, y);
					default:
						Other();
				}", @"{
					LNode op          = null, tmp_1 = null, x, x_ = null, y = null;
					LNodeList attrs;
					if (code.Calls(CodeSymbols.Assign, 2) && (x = code.Args[0]) != null && (y = code.Args[1]) != null) {
						Assign(x, y);
					} else if ( 
						(attrs = code.Attrs).IsEmpty | true && code.Calls(CodeSymbols.Assign, 2) && 
						(x = code.Args[0]) != null && (tmp_1 = code.Args[1]) != null && tmp_1.Calls(CodeSymbols.Add, 2) && 
						(x_ = tmp_1.Args[0]) != null && x_.Equals(x) && 1.Equals(tmp_1.Args[1].Value) || 
						(attrs = code.Attrs).IsEmpty | true && code.Args.Count == 2 && (op = code.Target) != null && 
						(x = code.Args[0]) != null && (y = code.Args[1]) != null) {
						Handle(attrs);
						Handle(x, y);
					} else {
						Other();
					}
				}"
				.Replace("tmp_1", "tmp_"+MacroProcessor.NextTempCounter));
			// Ideally this generated code would use a tmp_n variable, but I'll accept the current output
			TestEcs(@"
				matchCode(classDecl) {
				case {
						[$(..attrs)] 
						class $typeName : $(..baseTypes) { $(..body); }
					}:
						Handler();
				}", @"{
					LNode typeName;
					LNodeList attrs, baseTypes, body;
					if ((attrs = classDecl.Attrs).IsEmpty | true && classDecl.Calls(CodeSymbols.Class, 3) && 
						(typeName = classDecl.Args[0]) != null && classDecl.Args[1].Calls(CodeSymbols.AltList) && 
						(baseTypes = classDecl.Args[1].Args).IsEmpty | true &&
						classDecl.Args[2].Calls(CodeSymbols.Braces) && (body = classDecl.Args[2].Args).IsEmpty | true)
					{
						Handler();
					}
				}");
			TestEcs(@"matchCode(code) {
					case { '2' + $(ref rhs); }: Weird();
				}", @"
					if (code.Calls(CodeSymbols.Add, 2) && '2'.Equals(code.Args[0].Value) && (rhs = code.Args[1]) != null) {
						Weird();
					}");
			TestEcs(@"matchCode (code) {
					case $_<$(.._)>:
					default: Nope();
				}", @"
					if (code.CallsMin(CodeSymbols.Of, 1)) {
					} else {
						Nope();
					}
				");
			TestEcs(@"matchCode (stmt) {
					case { alt $altName; }: 
						WriteLine(altName.ToString());
					}",
				@"{
					LNode altName;
					if (stmt.Calls(CodeSymbols.Var, 2) && stmt.Args[0].IsIdNamed((Symbol) ""alt"") && (altName = stmt.Args[1]) != null) {
						WriteLine(altName.ToString());
					}
				}");
		}

		[Test]
		public void TestMatchCodePreservesTrivia()
		{
			TestEcs(@"/*before*/ matchCode (var) { case 777: Yay(); case 666: Boo(); default: Huh(); } /*after*/",
				@"/*before*/ if (777.Equals(var.Value)) { Yay(); } else if (666.Equals(var.Value)) { Boo(); } else { Huh(); } /*after*/");
			TestEcs(@"/*before*/ 
					matchCode(Get.Var) {
						case Do($stuff):
							Do(stuff);
					} /*after*/",
					@"/*before*/ 
					{
						var tmp_1 = Get.Var;
						LNode stuff;
						if (tmp_1.Calls((Symbol) ""Do"", 1) && (stuff = tmp_1.Args[0]) != null) {
							Do(stuff);
						}
					} /*after*/"
				.Replace("tmp_1", "tmp_" + MacroProcessor.NextTempCounter));
		}

		[Test]
		public void TestMatchCodeWithDuplicateVariables()
		{
			TestEcs(@"matchCode (node) {
					case $x == $x, Foo($x): 
						Handler1();
					case $(ref r)($(ref r)): 
						Handler2();
					}",
				@"{
					LNode x;
					if (node.Calls(CodeSymbols.Eq, 2) && (x = node.Args[0]) != null && LNode.Equals(x, node.Args[1], LNode.CompareMode.IgnoreTrivia)
						|| node.Calls((Symbol) ""Foo"", 1) && (x = node.Args[0]) != null) {
						Handler1();
					} else if (node.Args.Count == 1 && (r = node.Target) != null && LNode.Equals(r, node.Args[0], LNode.CompareMode.IgnoreTrivia)) {
						Handler2();
					}
				}");
			TestEcs(@"matchCode (node) {
					case { Foo($x, $(...etc)) == Bar($(...etc), $y); }: 
						Handler1();
					case { [$(...etc)] Foo($(...etc)); }: 
						Handler2();
					}",
				@"{
					LNode tmp_A, tmp_B, x, y;
					LNodeList etc;
					if (node.Calls(CodeSymbols.Eq, 2) && (tmp_A = node.Args[0]) != null && tmp_A.CallsMin((Symbol) ""Foo"", 1) 
						&& (x = tmp_A.Args[0]) != null && (etc = new LNodeList(tmp_A.Args.Slice(1))).IsEmpty | true 
						&& (tmp_B = node.Args[1]) != null && tmp_B.CallsMin((Symbol) ""Bar"", 1) 
						&& LNode.Equals(etc, tmp_B.Args.WithoutLast(1), LNode.CompareMode.IgnoreTrivia)
						&& (y = tmp_B.Args[tmp_B.Args.Count - 1]) != null) {
						Handler1();
					} else if ((etc = node.Attrs).IsEmpty | true && node.Calls((Symbol) ""Foo"") 
						&& LNode.Equals(etc, node.Args, LNode.CompareMode.IgnoreTrivia)) {
						Handler2();
					}
				}".Replace("tmp_A", "tmp_" + MacroProcessor.NextTempCounter)
				  .Replace("tmp_B", "tmp_" + (MacroProcessor.NextTempCounter + 1)));
		}

		[Test]
		public void TestMatches()
		{
			TestEcs(@"bool matches = matches(Get.Var, Do($stuff));", 
					@"bool matches = LNode.Var(out var tmp_A, Get.Var) && (tmp_A.Calls((Symbol) ""Do"", 1) && LNode.Var(out var stuff, tmp_A.Args[0]));"
					.Replace("tmp_A", "tmp_"+MacroProcessor.NextTempCounter));
			TestEcs(@"
					if (matches(code, { '2' + 2; }))
						Weird();
				", @"
					if (code.Calls(CodeSymbols.Add, 2) && '2'.Equals(code.Args[0].Value) && 2.Equals(code.Args[1].Value))
						Weird();
					");
			TestEcs(@"if (matches(code, 1, one)) 
						One();
				", @"
					if (1.Equals(code.Value) || code.IsIdNamed((Symbol) ""one""))
						One();
				");
			TestEcs(@"bool b = matches(stmt, { alt $altName; });",
					@"bool b = stmt.Calls(CodeSymbols.Var, 2) && stmt.Args[0].IsIdNamed((Symbol) ""alt"") && LNode.Var(out var altName, stmt.Args[1]);");
			TestEcs(@"bool b = matches(e, (_, $a, $(...args), $c));",
					@"bool b = e.CallsMin(CodeSymbols.Tuple, 3) && e.Args[0].IsIdNamed((Symbol) ""_"") && LNode.Var(out var a, e.Args[1])
					        && LNode.Var(out var args, new LNodeList(e.Args.Slice(2, e.Args.Count - 3)))
					        && LNode.Var(out var c, e.Args[e.Args.Count - 1]);");
		}
	}
}
