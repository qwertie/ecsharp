using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.SyncLib;

partial class SyncBinary
{
	internal class ReaderState
	{

		public bool IsInsideList;

		public bool? ReachedEndOfList { get; internal set; }
		public int Depth { get; internal set; }
		public FieldId NextField { get; internal set; }

		internal void SetCurrentObject(object value)
		{
			throw new NotImplementedException();
		}
	}
}
