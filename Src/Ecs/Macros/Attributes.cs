using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;

namespace Ecs.Macros
{
	public class ContainsMacroAttribute : Attribute { }
	public class LexicalMacroAttribute : Attribute
	{
		public LexicalMacroAttribute(string name) { Name = GSymbol.Get(name); }
		public Symbol Name { get; set; }
	}
}
