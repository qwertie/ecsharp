using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniTestRunner.ViewModel
{
	class DisplaySettingsVM : NPCHelper
	{
		public string Filter { get; set; }
		public bool HideSuccess { get; set; }
		public bool HideIgnored { get; set; }

		internal bool IsFilteredOut(RowModel m)
		{
			throw new NotImplementedException();
		}
	}
}
