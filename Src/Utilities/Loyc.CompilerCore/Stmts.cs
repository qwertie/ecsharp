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
		static public readonly Symbol DefVar = Symbol.Get("DefVar");
		static public readonly Symbol DefFn = Symbol.Get("DefFn");
		static public readonly Symbol DefProperty = Symbol.Get("DefProperty");
		static public readonly Symbol DefConst = Symbol.Get("DefConst");

		// Imperative statements
		static public readonly Symbol ExprStmt = Symbol.Get("ExprStmt");
		static public readonly Symbol IfThen = Symbol.Get("IfThen");
		static public readonly Symbol WhileDo = Symbol.Get("WhileDo");
		static public readonly Symbol DoWhile = Symbol.Get("DoWhile");
		static public readonly Symbol ForC = Symbol.Get("ForC");
		static public readonly Symbol ForEach = Symbol.Get("ForEach");
		static public readonly Symbol ForRange = Symbol.Get("ForRange");
		static public readonly Symbol Switch = Symbol.Get("Switch");
		static public readonly Symbol Using = Symbol.Get("Using");
	}
}
