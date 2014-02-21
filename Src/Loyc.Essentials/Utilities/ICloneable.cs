using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc
{
	/// <summary>Interface for types that can duplicate themselves.</summary>
	/// <typeparam name="T">Normally T is the type that implements this interface.</typeparam>
	public interface ICloneable<out T>
	{
		T Clone();
	}
}
