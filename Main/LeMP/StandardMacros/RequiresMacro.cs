using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;

namespace LeMP
{
	partial class StandardMacros
	{
		// Hmm, our macro processor makes this hard o implement.
		// TODO: more features in macro processor?
		[SimpleMacro(@"[requires(expr)] T method(...) {...}; T method([requires(expr)] ArgType arg) {...}",
			"Generates a Contract.Requires(expr) statement at the beginning of the method.", Mode = MacroMode.Passive)]
		public static LNode requires(LNode node, IMessageSink sink)
		{
			return null;
		}
	}
}
