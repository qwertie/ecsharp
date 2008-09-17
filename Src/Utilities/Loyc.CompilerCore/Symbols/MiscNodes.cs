using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Runtime;

namespace Loyc.CompilerCore
{
	public static class MiscNodes
	{
		static public readonly Symbol FormalArg = Symbol.Get("FormalArg"); // Params[0] is its type
		
		// Attributes
		static public readonly Symbol UserAttr = Symbol.Get("UserAttr"); // e.g. "Serializable", "SerializableAttribute"
		static public readonly Symbol StdAttr = Symbol.Get("StdAttr"); // e.g. "public", "internal", "static", "readonly", etc.

		static public readonly Symbol Type = Symbol.Get("Type");
	}
}
