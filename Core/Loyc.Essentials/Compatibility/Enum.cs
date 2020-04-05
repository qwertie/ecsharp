using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Compatibility
{
	/// <summary>Implements Enum.TryParse in .NET 3.5</summary>
	public class EnumStatic
	{
		[Obsolete("Please call Enum.TryParse instead.")]
		public static bool TryParse<TEnum>(string value, out TEnum result) where TEnum : struct
		{
			return Enum.TryParse(value, out result);
		}
	}
}
