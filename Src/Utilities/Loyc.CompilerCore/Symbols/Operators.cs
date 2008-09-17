using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Runtime;

namespace Loyc.CompilerCore
{
	public static class Operators
	{
		static public readonly Symbol Assign = Symbol.Get("e=e");
		static public readonly Symbol Add = Symbol.Get("e+e");
		static public readonly Symbol Subtract = Symbol.Get("e-e");
		static public readonly Symbol Times = Symbol.Get("e*e");
		static public readonly Symbol Divide = Symbol.Get("e/e");
		static public readonly Symbol Modulus = Symbol.Get("e%e");
		static public readonly Symbol Exponentiate = Symbol.Get("e**e");
		static public readonly Symbol ShiftLeft = Symbol.Get("e<<e");
		static public readonly Symbol ShiftRight = Symbol.Get("e>>e");
		static public readonly Symbol BinaryAnd = Symbol.Get("e&e");
		static public readonly Symbol BinaryOr = Symbol.Get("e|e");
		static public readonly Symbol BinaryXor = Symbol.Get("e^e");
		static public readonly Symbol And = Symbol.Get("e&&e");
		static public readonly Symbol Or = Symbol.Get("e||e");
		static public readonly Symbol Comma = Symbol.Get("e,e");
		
		static public readonly Symbol Negate = Symbol.Get("-e");
		static public readonly Symbol Not = Symbol.Get("!e");
		static public readonly Symbol Dereference = Symbol.Get("*e");
		static public readonly Symbol Abs = Symbol.Get("|e|");
		
		static public readonly Symbol Equal = Symbol.Get("e==e");
		static public readonly Symbol NotEqual = Symbol.Get("e!=e");
		static public readonly Symbol Greater = Symbol.Get("e>e");
		static public readonly Symbol GreaterEqual = Symbol.Get("e>=e");
		static public readonly Symbol Less = Symbol.Get("e<e");
		static public readonly Symbol LessEqual = Symbol.Get("e<=e");
		static public readonly Symbol Matches = Symbol.Get("e=~e");

		static public readonly Symbol AssignAdd = Symbol.Get("e+=e");
		static public readonly Symbol AssignSubtract = Symbol.Get("e-=e");
		static public readonly Symbol AssignTimes = Symbol.Get("e*=e");
		static public readonly Symbol AssignDivide = Symbol.Get("e/=e");
		static public readonly Symbol AssignModulus = Symbol.Get("e%=e");
		static public readonly Symbol AssignExponentiate = Symbol.Get("e**=e");
		static public readonly Symbol AssignShiftLeft = Symbol.Get("e<<=e");
		static public readonly Symbol AssignShiftRight = Symbol.Get("e>>=e");
		static public readonly Symbol AssignBinaryAnd = Symbol.Get("e&=e");
		static public readonly Symbol AssignBinaryOr = Symbol.Get("e|=e");
		static public readonly Symbol AssignBinaryXor = Symbol.Get("e^=e");
		static public readonly Symbol AssignAnd = Symbol.Get("e&&=e");
		static public readonly Symbol AssignOr = Symbol.Get("e||=e");

		static public readonly Symbol Dot = Symbol.Get("e.e");
		static public readonly Symbol New = Symbol.Get("New");
		static public readonly Symbol Index = Symbol.Get("e[]");
		static public readonly Symbol Call = Symbol.Get("e()"); // In boo, may be :New in disguise
		static public readonly Symbol Cast = Symbol.Get("()e");
		static public readonly Symbol As = Symbol.Get("e_as_e");
		static public readonly Symbol Is = Symbol.Get("e_is_e");
		static public readonly Symbol Tuple = Symbol.Get("Tuple"); // A list of expressions

		static public readonly Symbol Ternary = Symbol.Get("e?e:e");

		static public readonly Symbol DefFn = Symbol.Get("DefFn"); // Inner function or lambda; same as Stmts.DefFn
	}
}
