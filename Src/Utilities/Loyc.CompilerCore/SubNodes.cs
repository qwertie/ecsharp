using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Runtime;

namespace Loyc.CompilerCore
{
	public static class SubNodes
	{
		static public readonly Symbol Attribute = Symbol.Get("Attribute");
		static public readonly Symbol FormalParam = Symbol.Get("FormalParam");
		static public readonly Symbol OpAssign = Symbol.Get("e=e");
		static public readonly Symbol OpAdd = Symbol.Get("e+e");
		static public readonly Symbol OpSub = Symbol.Get("e-e");
		static public readonly Symbol OpMul = Symbol.Get("e*e");
		static public readonly Symbol OpDiv = Symbol.Get("e/e");

		// Common attributes
		
	}
}
