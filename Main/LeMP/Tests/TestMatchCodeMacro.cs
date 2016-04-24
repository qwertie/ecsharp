using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
				@"#if(777.Equals(var.Value), Yay(), 
					#if(666.Equals(var.Value), Boo(), @Huh?()));");
			TestEcs(@"matchCode(code) {
					case { '2' + 2; }: Weird();
				}", @"
					if (code.Calls(CodeSymbols.Add, 2) && '2'.Equals(code.Args[0].Value) && 2.Equals(code.Args[1].Value))
						Weird();");
			TestEcs(@"matchCode(Get.Var) {
					case Do($stuff): Do(stuff);
				}", @"{
						var tmp_1 = Get.Var;
						LNode stuff;
						if (tmp_1.Calls((Symbol) ""Do"", 1) && (stuff = tmp_1.Args[0]) != null)
							Do(stuff);
					}"
				.Replace("tmp_1", "tmp_"+StandardMacros.NextTempCounter));
			TestEcs(@"matchCode(code) { 
					$(lit && #.IsLiteral) => Literal(); 
					$(id[#.IsId]) => Id(); 
					$_ => Call();
				}",
				@"{
					LNode id, lit;
					if ((lit = code) != null && lit.IsLiteral)
						Literal();
					else if ((id = code) != null && id.IsId)
						Id();
					else
						Call();
				}");
			TestEcs(@"matchCode(code) { 
					case foo, FOO:  Foo(); 
					case 1, 1.0:    One(); 
					case $whatever: Whatever();
				}", @"{
					LNode whatever;
					if (code.IsIdNamed((Symbol) ""foo"") || code.IsIdNamed((Symbol) ""FOO""))
						Foo();
					else if (1.Equals(code.Value) || 1d.Equals(code.Value))
						One();
					else if ((whatever = code) != null)
						Whatever();
				}");
			TestEcs(@"matchCode(code) { 
					case 1, one: One();
				}", @"
					if (1.Equals(code.Value) || code.IsIdNamed((Symbol) ""one""))
						One();
				");
			TestEcs(@"matchCode(code) { 
					case $binOp($_, $_): BinOp(binOp);
				}", @"{
					LNode binOp;
					if (code.Args.Count == 2 && (binOp = code.Target) != null)
						BinOp(binOp);
				}");
			TestEcs(@"matchCode(code) { 
					case ($a, $b, $(...args), $c):                Three(a, b, c);
					case (null, $(...args)), ($first, $(..args)): Tuple(first, args);
					case ($(...args), $last):                     Unreachable();
					case ($(...args),):                           Tuple(args);
				}", @"{
					LNode a, b, c, first = null, last;
					VList<LNode> args;
					if (code.CallsMin(CodeSymbols.Tuple, 3) && (a = code.Args[0]) != null && (b = code.Args[1]) != null && (c = code.Args[code.Args.Count - 1]) != null) {
						args = new VList<LNode>(code.Args.Slice(2, code.Args.Count - 3));
						Three(a, b, c);
					} else if (code.CallsMin(CodeSymbols.Tuple, 1) && code.Args[0].Value == null && (args = new VList<LNode>(code.Args.Slice(1))).IsEmpty | true 
						|| code.CallsMin(CodeSymbols.Tuple, 1) && (first = code.Args[0]) != null && (args = new VList<LNode>(code.Args.Slice(1))).IsEmpty | true)
						Tuple(first, args);
					else if (code.CallsMin(CodeSymbols.Tuple, 1) && (last = code.Args[code.Args.Count - 1]) != null) {
						args = code.Args.WithoutLast(1);
						Unreachable();
					} else if (code.Calls(CodeSymbols.Tuple)) {
						args = code.Args;
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
					VList<LNode> attrs;
					if (code.Calls(CodeSymbols.Assign, 2) && (x = code.Args[0]) != null && (y = code.Args[1]) != null)
						Assign(x, y);
					else if ( 
						(attrs = code.Attrs).IsEmpty | true && code.Calls(CodeSymbols.Assign, 2) && 
						(x = code.Args[0]) != null && (tmp_1 = code.Args[1]) != null && tmp_1.Calls(CodeSymbols.Add, 2) && 
						(x_ = tmp_1.Args[0]) != null && x_.Equals(x) && 1.Equals(tmp_1.Args[1].Value) || 
						(attrs = code.Attrs).IsEmpty | true && code.Args.Count == 2 && (op = code.Target) != null && 
						(x = code.Args[0]) != null && (y = code.Args[1]) != null) {
						Handle(attrs);
						Handle(x, y);
					} else
						Other();
				}"
				.Replace("tmp_1", "tmp_"+StandardMacros.NextTempCounter));
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
					VList<LNode> attrs, baseTypes, body;
					if ((attrs = classDecl.Attrs).IsEmpty | true && classDecl.Calls(CodeSymbols.Class, 3) && 
						(typeName = classDecl.Args[0]) != null && classDecl.Args[1].Calls(CodeSymbols.AltList) && 
						classDecl.Args[2].Calls(CodeSymbols.Braces))
					{
						baseTypes = classDecl.Args[1].Args;
						body = classDecl.Args[2].Args;
						Handler();
					}
				}");
			TestEcs(@"matchCode(code) {
					case { '2' + $(ref rhs); }: Weird();
				}", @"
					if (code.Calls(CodeSymbols.Add, 2) && '2'.Equals(code.Args[0].Value) && (rhs = code.Args[1]) != null)
						Weird();");
			TestEcs(@"matchCode (code) {
					case $_<$(.._)>:
					default: Nope();
				}", @"
					if (code.CallsMin(CodeSymbols.Of, 1)) { } 
					else Nope();
				");
		}
	}
}
