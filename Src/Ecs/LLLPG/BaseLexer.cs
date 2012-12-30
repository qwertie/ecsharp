using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.CompilerCore;
using Loyc.Utilities;

namespace Loyc.LLParserGenerator
{
	class BaseLexer : BaseRecognizer<int>
	{
		protected BaseLexer(IParserSource<int> source = null) : base(source) { }

		protected void MatchRange(int lo, int hi)
		{
			int la0 = LA(0);
			if (la0 < lo || la0 > hi)
				Error(new IntRange(lo, hi).ToString(), la0);
			else
				Match();
		}
		protected void MatchRange(int lo1, int hi1, int lo2, int hi2)
		{
			int la0 = LA(0);
			if (!(la0 >= lo1 && la0 <= hi1) && !(la0 >= lo2 && la0 <= hi2)) {
				IntSet set = new IntSet(true, false, new IntRange(lo1, hi1), new IntRange(lo2, hi2));
				Error(set.ToString(), la0);
			} else
				Match();
		}
		protected void Match(IntSet set)
		{
			int la0 = LA(0);
			if (!set.Contains(la0))
				Error(set.ToString(), la0);
			else
				Match();
		}
	}
}
