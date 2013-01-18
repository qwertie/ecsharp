using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;
using Loyc.CompilerCore;
using Loyc.LLParserGenerator;
using Loyc;
using Loyc.Utilities;

namespace ecs
{
	class Program
	{
		static void Fn(int x) { Console.WriteLine("int"); }
		static class X
		{
			static void Fn(long x) { Console.WriteLine("long"); }
			public static void Test() { Fn(0); }
		}

		static void Main(string[] args)
		{
			X.Test();
			Console.WriteLine("Running tests...");
			RunTests.Run(new IntSetTests());
			RunTests.Run(new GTests());
			RunTests.Run(new GreenTests());
			RunTests.Run(new NodeTests());
			RunTests.Run(new LlpgTests());
			RunTests.Run(new EcsNodePrinterTests());

			//if (args.Contains("--genparser"))
			//{
				PrintParser();
			//}
		}

		private static void PrintTesParsers()
		{
			
		}

		private static void PrintParser()
		{
			// Our goal: describe a parser with objects and use it to generate C# source code
			// Notes: LoycPG operators
			// ' ' | '\t'               -- Alt operator (ordered by priority)
			// '_' || LETTER_CHAR       -- Not sure if there is any difference with '|'
			// ESC_SEQ / ~NEWLINE_CHAR  -- Alt operator with warning of ambiguity between alts suppressed
			// '\u0000'..'\u001F'       -- Character range
			// ~WS_CHAR                 -- Inverted character set (applies to single terminals)
			// '/' '*'                  -- Two things in a row
			// "/*"                     -- Equivalent to '/' '*'
			// ('+'|'-')?               -- Zero or one
			// WS_CHAR+                 -- One or more
			// WS_CHAR*                 -- Zero or more
			// nongreedy(.)*            -- Zero or more, exiting loop takes priority over staying
			// nongreedy(.)+            -- One or more, exiting loop takes priority over staying
			// nongreedy(FOO)?          -- Zero or one, zero preferred
			// greedy(...)*             -- Suppress warning about ambiguity between alt and exit
			// &('\\' '\\')             -- Syntactic predicate: sequence matches here, consumes no input
			// &!('\\' '\\')            -- Syntactic predicate: sequence does not match here
			// &{ ...C# expression... } -- Check whether condition is true, consumes no input
			// { ...C# expression... }? -- ANTLR syntax for the same thing, a "validating semantic predicate"
			// '"' => DQ_STRING         -- Gated predicate, can be used to simplify prediction or to resolve ambiguity
			// {...}? => DQ_STRING      -- Called a "gated semantic predicate" in ANTLR; just combines a gate with a C# expr
			// { ...C# statements... }  -- Once input prior to this point is confirmed, run this code. Low precedence.
			// A(C# expression)         -- Call a rule with arguments
			// 
			// Precedence:
			// 1. Unary *, +, ?, &, &!, greedy, nongreedy
			// 2. Juxtaposition
			// 3. Binary =>
			// 4. Binary ||, |, /
			//
			// How would this work as a EC# DSL?
			// rule X() = 'x' ==> rule X() { 'x'; }  ==> #def(X, #(), rule, #{}('x'))
			// .              ==> _                  ==> _
			// 'a' B C        ==> ('A', B, C)        ==> #tuple('A', B, C)
			// 'x' | X Y | Z  ==> 'X' | (X, Y) | Z   ==> #|(#|('X', (X, Y)), Z)
			// 'x' / X Y / Z  ==> 'X' / (X, Y) / Z   ==> #/(#/('X', (X, Y)), Z)
			// ~WS_CHAR       ==> ~WS_CHAR           ==> #~(WS_CHAR)
			// 'a'..'z'       ==> 'a'..'z'           ==> #..('a', 'z')
			// ('+'|'-')?     ==> ('+'|'-')`?`       ==> #`?`(#|('+','-'))
			// WS_CHAR+       ==> WS_CHAR`+`         ==> #`+`(WS_CHAR)
			// WS_CHAR*       ==> WS_CHAR`*`         ==> #`*`(WS_CHAR)
			// nongreedy(.)+  ==> _ `+min`           ==> #`+min`(WS_CHAR)
			// greedy(' ')*   ==> ' '`*max`          ==> #`*max`(WS_CHAR)
			// &('\\' '\\')   ==> &('\\' '\\')       ==> #&('\\' '\\')
			// &!('\\' '\\')  ==> &!('\\' '\\')      ==> #&(#!('\\' '\\')) => postprocessed to #`&!`('\\' '\\')
			// A {code;} B    ==> (A, {code;}, B)    ==> #tuple(A, #{}(code), B)
			// A &{code;} B   ==> (A, &{code;}, B)   ==> #tuple(A, #&(#{}(code)), B) => postprocessed to #`&{}`(code)
			// A(arg)         ==> A(arg)             ==> A(arg)
			
			// 
		}

	}

	public struct @void
	{
		public static readonly @void Value = new @void();
		public override string ToString()
		{
			return "()";
		}
	}
}
