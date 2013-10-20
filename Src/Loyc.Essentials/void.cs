using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc
{
	public struct @void
	{
		public static readonly @void Value = new @void();
		public override string ToString()
		{
			return "void";
		}
	}
}
