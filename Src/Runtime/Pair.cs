using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Utilities
{
	public struct Pair<TA,TB>
	{
		public Pair(TA a, TB b) { A = a; B = b; }
		public TA A;
		public TB B;
		public TA Key { get { return A; } set { A = value; } }
		public TB Value { get { return B; } set { B = value; } }
		public TA First { get { return A; } set { A = value; } }
		public TB Second { get { return B; } set { B = value; } }
	}
}
