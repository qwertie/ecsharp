using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Essentials;

namespace Loyc.CompilerCore
{
	// Tentative
	public static class Stmts
	{
		static public readonly Symbol Module = GSymbol.Get("Module");
		static public readonly Symbol Import = GSymbol.Get("Import");
		static public readonly Symbol Namespace = GSymbol.Get("Namespace");

		// Type definitions
		static public readonly Symbol DefClass = GSymbol.Get("DefClass");
		static public readonly Symbol DefStruct = GSymbol.Get("DefStruct");
		static public readonly Symbol DefInterface = GSymbol.Get("DefInterface");
		static public readonly Symbol DefDelegate = GSymbol.Get("DefDelegate");
		static public readonly Symbol DefEnum = GSymbol.Get("DefEnum");

		// Code & data definitions
		static public readonly Symbol DefVar = GSymbol.Get("DefVar"); // Initial value provided in Params[0]. Some languages may allow DefVar nodes embedded within expressions.
		static public readonly Symbol DefFn = GSymbol.Get("DefFn");
		static public readonly Symbol DefProperty = GSymbol.Get("DefProperty");
		static public readonly Symbol DefConst = GSymbol.Get("DefConst");

		// Imperative statements
		static public readonly Symbol ExprStmt = GSymbol.Get("ExprStmt");
		static public readonly Symbol IfThen = GSymbol.Get("IfThen");     // Condition in Params[0], true block in Params[1].Block, false block in Params[2].Block
		static public readonly Symbol WhileDo = GSymbol.Get("WhileDo");   // Condition in Params[0], block in Block
		static public readonly Symbol DoWhile = GSymbol.Get("DoWhile");   // Condition in Params[0], block in Block
		static public readonly Symbol ForC = GSymbol.Get("ForC");         // 3 parts in Params[0..2], block in Block
		static public readonly Symbol ForEach = GSymbol.Get("ForEach");   // Params[0] is :DefVar, Params[1] is an expr, block in Block
		static public readonly Symbol ForRange = GSymbol.Get("ForRange"); // Params[0] is :DefVar or lvalue expr, Params[1] and Params[2] are the range, block in Block
		static public readonly Symbol Switch = GSymbol.Get("Switch");     // Params[0] is expr, Block is a list of cases
		static public readonly Symbol Case = GSymbol.Get("Case");         // TODO: develop node convention for cases
		static public readonly Symbol Using = GSymbol.Get("Using");       // Params[0] is :DefVar, code block in Block
		static public readonly Symbol Block = GSymbol.Get("Block");       // unnamed code block
	}
}
