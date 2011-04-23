using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Essentials
{
	public interface ICloneable<T>
	{
		T Clone();
	}
}
