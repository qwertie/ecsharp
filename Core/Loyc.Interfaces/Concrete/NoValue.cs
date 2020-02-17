using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc
{
	/// <summary><c>NoValue.Value</c> is meant to be used as the value of a 
	/// property that has "no value", meaning no value is assigned or that the 
	/// property is meaningless at the current time or in the current context.
	/// </summary><remarks>
	/// Most often <c>null</c> is used for this purpose; <c>NoValue.Value</c> 
	/// is used when <c>null</c> is (or might be) a valid, meaningful 
	/// value and you want to distinguish between "no value" and "null".
	/// For example, this can be returned by the Value property of <see cref="Loyc.Syntax.ILNode"/>,
	/// in which <c>NoValue</c> means "this is not a literal, so it can't have
	/// a value, not even null".
	/// <para/>
	/// Also, this value converts implicitly to <see cref="Maybe{T}.NoValue"/>.
	/// </remarks>
	public class NoValue
	{
		private NoValue() { }
		public static readonly NoValue Value = new NoValue();
		public override string ToString()
		{
			return "(No value)".Localized();
		}
	}
}
