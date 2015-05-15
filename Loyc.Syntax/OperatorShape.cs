using System;

namespace Loyc.Syntax
{
	/// <summary>An enum of common operator formats.</summary>
	/// <remarks>It is intentional that the absolute value of each OperatorShape
	/// (except Other) is the arity of (number of arguments to) that shape.</remarks>
	public enum OperatorShape { Suffix = -1, Nullary = 0, Prefix = 1, Infix = 2, Ternary = 3, Other = 4 }
}
