using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.CompilerCore
{
	/// <summary>
	/// A simple non-optimizing final-stage code generator that converts a method
	/// AstNode to CIL using Reflection.Emit. This class is not responsible for
	/// creating assemblies or types.
	/// </summary>
	/// <remarks>
	/// TODO: Generics support.
	/// <para/>
	/// References must be resolved already.
	/// <para/>
	/// The following node types are supported:
	/// </remarks>
	class CilSimpleMethodGenerator
	{
		
	}
}
