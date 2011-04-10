using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Essentials;

namespace Loyc.CompilerCore
{
	public static class MiscNodes
	{
		static public readonly Symbol FormalArg = GSymbol.Get("FormalArg"); // Params[0] is its type
		
		// Attributes
		static public readonly Symbol UserAttr = GSymbol.Get("UserAttr"); // e.g. "Serializable", "SerializableAttribute"
		static public readonly Symbol StdAttr = GSymbol.Get("StdAttr"); // e.g. "public", "internal", "static", "readonly", etc.

		static public readonly Symbol Type = GSymbol.Get("Type");
	}
}
