using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Runtime;

namespace Loyc.CompilerCore
{
	// Tentative
	public static class Stmts
	{
		static public readonly Symbol Module = Symbol.Get("Module");
		static public readonly Symbol Import = Symbol.Get("Import");
		static public readonly Symbol Namespace = Symbol.Get("Namespace");

		// Type definitions
		static public readonly Symbol DefClass = Symbol.Get("DefClass");
		static public readonly Symbol DefStruct = Symbol.Get("DefStruct");
		static public readonly Symbol DefInterface = Symbol.Get("DefInterface");
		static public readonly Symbol DefDelegate = Symbol.Get("DefDelegate");
		static public readonly Symbol DefEnum = Symbol.Get("DefEnum");

		// Code & data definitions
		static public readonly Symbol DefVar = Symbol.Get("DefVar"); // Initial value provided in Params[0]. Some languages may allow DefVar nodes embedded within expressions.
		static public readonly Symbol DefFn = Symbol.Get("DefFn");
		static public readonly Symbol DefProperty = Symbol.Get("DefProperty");
		static public readonly Symbol DefConst = Symbol.Get("DefConst");

		// Imperative statements
		static public readonly Symbol ExprStmt = Symbol.Get("ExprStmt");
		static public readonly Symbol IfThen = Symbol.Get("IfThen");     // Condition in Params[0], true block in Params[1].Block, false block in Params[2].Block
		static public readonly Symbol WhileDo = Symbol.Get("WhileDo");   // Condition in Params[0], block in Block
		static public readonly Symbol DoWhile = Symbol.Get("DoWhile");   // Condition in Params[0], block in Block
		static public readonly Symbol ForC = Symbol.Get("ForC");         // 3 parts in Params[0..2], block in Block
		static public readonly Symbol ForEach = Symbol.Get("ForEach");   // Params[0] is :DefVar, Params[1] is an expr, block in Block
		static public readonly Symbol ForRange = Symbol.Get("ForRange"); // Params[0] is :DefVar or lvalue expr, Params[1] and Params[2] are the range, block in Block
		static public readonly Symbol Switch = Symbol.Get("Switch");     // Params[0] is expr, Block is a list of cases
		static public readonly Symbol Case = Symbol.Get("Case");         // TODO: develop node convention for cases
		static public readonly Symbol Using = Symbol.Get("Using");       // Params[0] is :DefVar, code block in Block
		static public readonly Symbol Block = Symbol.Get("Block");       // unnamed code block
	}
}
