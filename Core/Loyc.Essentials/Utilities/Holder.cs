using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc
{
	/// <summary>A trivial class that holds a single value of type T in the 
	/// <see cref="Value"/> property.
	/// </summary><remarks>
	/// This class is useful mainly as an alternative to standard boxing. When you 
	/// box a structure in C#, you lose access to the members of that structure.
	/// This class, in contrast, provides access to the "boxed" value.
	/// This type is different from the standard <c>Tuple{T}</c> in that the 
	/// <see cref="Value"/> is a mutable field.
	/// </remarks>
	public class Holder<T> : IHasMutableValue<T>
	{
		public Holder(T value) { Value = value; }
		public Holder() { }

		/// <summary>Any value of type T.</summary>
		public T Value;
		T IHasValue<T>.Value { get { return Value; } }
		T IHasMutableValue<T>.Value { get { return Value; } set { Value = value; } }

		public override bool Equals(object obj)
		{
			if (obj is Holder<T>)
				obj = ((Holder<T>)obj).Value;
			return Value == null ? obj == null : Value.Equals(obj);
		}
		public override int GetHashCode() => Value == null ? 0 : Value.GetHashCode();
		public override string ToString() => Value?.ToString();
		public static implicit operator Holder<T>(T value) { return new Holder<T>(value); }
	}
}
