using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc
{
	/// <summary><c>@void.Value</c> represents the sole value of <c>System.Void</c>
	/// (called "void" in C#).</summary>
	/// <remarks>.NET unfortunately treats void as something that is not a real 
	/// type; for example you cannot use <c>new void()</c> or <c>default(void)</c>
	/// in C#. This was a dumb decision because it means that some generic code
	/// must be duplicated for void and non-void types. A good example is the fact
	/// that when you have a <see cref="Dictionary{TKey,TVal}"/>, TVal cannot be 
	/// void, so you cannot use <c>Dictionary(string,void)</c> to express the idea 
	/// of "a set of strings with no associated values". The <see cref="HashSet{T}"/> 
	/// class uses a completely separate implementation and cannot just be an
	/// alias for <c>Dictionary{T,void}</c> (actually they could share 
	/// implementations using a dummy type like this one, but unfortunately .NET
	/// made another dumb decision that all types must consume at least one byte,
	/// so <c>HashSet</c> sharing code with <c>Dictionary</c> would make 
	/// <c>HashSet</c> less efficient.)
	/// <para/>
	/// Defining a @void type allows you to use it when it makes conceptual sense,
	/// although we cannot avoid .NET's requirement to waste at least one byte.
	/// </remarks>
	public struct @void
	{
		public static readonly @void Value = new @void();
		public override string ToString()
		{
			return "void";
		}
	}

	/// <summary><c>NoValue.Value</c> is meant to be used as the value of a 
	/// property that has "no value", meaning no value is assigned or that the 
	/// property is meaningless at the current time or in the current context.
	/// </summary><remarks>
	/// Most often <c>null</c> is used for this purpose; <c>NoValue.Value</c> 
	/// is used when <c>null</c> is a considered to be a valid, meaningful 
	/// value and you want to distinguish between "no value" and "null".
	/// For example, this is used by the the <see cref="Loyc.Syntax.LNode.Value"/>
	/// class, in which <c>NoValue</c> has a different meaning than both 
	/// <c>null</c> and <c>void</c>.
	/// </remarks>
	public class NoValue
	{
		private NoValue() { }
		public static readonly NoValue Value = new NoValue();
		public override string ToString()
		{
			return Localize.From("(No value)");
		}
	}
}
