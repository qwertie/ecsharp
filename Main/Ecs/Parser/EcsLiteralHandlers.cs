using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Ecs.Parser
{
	public class EcsLiteralHandlers : StandardLiteralHandlers
	{
		private static EcsLiteralHandlers _value = null;
		public new static EcsLiteralHandlers Value => _value = _value ?? new EcsLiteralHandlers();

		public EcsLiteralHandlers()
		{
			// TODO: This isn't the right way to parse decimal, but I'm in a hurry
			AddParser(true, (Symbol)"_m", (s, tm) =>
				ParseDouble(s, out double n) ? OK((decimal)n) : SyntaxError(s, tm));
		}
	}
}
