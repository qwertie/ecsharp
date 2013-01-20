using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UpdateControls.Collections;

namespace MiniTestRunner.Model
{
	public class ResultSet
	{
		IndependentList<RowModel> _roots = new IndependentList<RowModel>();
		public IList<RowModel> Assemblies
		{
			get { return _roots; }
		}
	}
}
