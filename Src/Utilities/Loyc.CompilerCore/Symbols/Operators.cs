using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Essentials;

namespace Loyc.CompilerCore
{
	public static class Operators
	{
		static public readonly Symbol Assign = GSymbol.Get("e=e");
		static public readonly Symbol Add = GSymbol.Get("e+e");
		static public readonly Symbol Subtract = GSymbol.Get("e-e");
		static public readonly Symbol Times = GSymbol.Get("e*e");
		static public readonly Symbol Divide = GSymbol.Get("e/e");
		static public readonly Symbol Modulus = GSymbol.Get("e%e");
		static public readonly Symbol Exponentiate = GSymbol.Get("e**e");
		static public readonly Symbol ShiftLeft = GSymbol.Get("e<<e");
		static public readonly Symbol ShiftRight = GSymbol.Get("e>>e");
		static public readonly Symbol BinaryAnd = GSymbol.Get("e&e");
		static public readonly Symbol BinaryOr = GSymbol.Get("e|e");
		static public readonly Symbol BinaryXor = GSymbol.Get("e^e");
		static public readonly Symbol And = GSymbol.Get("e&&e");
		static public readonly Symbol Or = GSymbol.Get("e||e");
		static public readonly Symbol Comma = GSymbol.Get("e,e");
		
		static public readonly Symbol Negate = GSymbol.Get("-e");
		static public readonly Symbol Not = GSymbol.Get("!e");
		static public readonly Symbol Dereference = GSymbol.Get("*e");
		static public readonly Symbol Abs = GSymbol.Get("|e|");
		
		static public readonly Symbol Equal = GSymbol.Get("e==e");
		static public readonly Symbol NotEqual = GSymbol.Get("e!=e");
		static public readonly Symbol Greater = GSymbol.Get("e>e");
		static public readonly Symbol GreaterEqual = GSymbol.Get("e>=e");
		static public readonly Symbol Less = GSymbol.Get("e<e");
		static public readonly Symbol LessEqual = GSymbol.Get("e<=e");
		static public readonly Symbol Matches = GSymbol.Get("e=~e");

		static public readonly Symbol AssignAdd = GSymbol.Get("e+=e");
		static public readonly Symbol AssignSubtract = GSymbol.Get("e-=e");
		static public readonly Symbol AssignTimes = GSymbol.Get("e*=e");
		static public readonly Symbol AssignDivide = GSymbol.Get("e/=e");
		static public readonly Symbol AssignModulus = GSymbol.Get("e%=e");
		static public readonly Symbol AssignExponentiate = GSymbol.Get("e**=e");
		static public readonly Symbol AssignShiftLeft = GSymbol.Get("e<<=e");
		static public readonly Symbol AssignShiftRight = GSymbol.Get("e>>=e");
		static public readonly Symbol AssignBinaryAnd = GSymbol.Get("e&=e");
		static public readonly Symbol AssignBinaryOr = GSymbol.Get("e|=e");
		static public readonly Symbol AssignBinaryXor = GSymbol.Get("e^=e");
		static public readonly Symbol AssignAnd = GSymbol.Get("e&&=e");
		static public readonly Symbol AssignOr = GSymbol.Get("e||=e");

		static public readonly Symbol Dot = GSymbol.Get("e.e");
		static public readonly Symbol New = GSymbol.Get("New");
		static public readonly Symbol Index = GSymbol.Get("e[]");
		static public readonly Symbol Call = GSymbol.Get("e()"); // In boo, may be :New in disguise
		static public readonly Symbol Cast = GSymbol.Get("()e");
		static public readonly Symbol As = GSymbol.Get("e_as_e");
		static public readonly Symbol Is = GSymbol.Get("e_is_e");
		static public readonly Symbol Tuple = GSymbol.Get("Tuple"); // A list of expressions

		static public readonly Symbol Ternary = GSymbol.Get("e?e:e");

		static public readonly Symbol DefFn = GSymbol.Get("DefFn"); // Inner function or lambda; same as Stmts.DefFn
	}
}
