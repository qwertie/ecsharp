using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc
{
	public static class Maybe
	{
		/// <summary>Returns <c>new Maybe&lt;T>(value)</c>. (exists for type inference)</summary>
		public static Maybe<T> Just<T>(T value) { return new Maybe<T>(value); }
	}
	/// <summary>Same as <see cref="Nullable{T}"/> except that it behaves like a
	/// normal type, i.e. (1) T is allowed to be a reference type and (2) you can
	/// nest them, as in <c>Maybe{Maybe{int}}</c>.</summary>
	/// <remarks>The name of this type comes from some functional programming 
	/// languages which define a type such as <c>data Maybe t = Null | Just t</c>.
	/// <para/>
	/// There is an implicit conversion from T that returns <c>new Maybe{T}(value)</c>.
	/// </remarks>
	public struct Maybe<T>
	{
		public static readonly Maybe<T> Null = new Maybe<T>();
		public Maybe(T value) { _value = value; _hasValue = true; }
		
		readonly T _value;
		readonly bool _hasValue;

		public bool HasValue { get { return _hasValue; } }
		public T Value { 
			get {
				if (!_hasValue)
					throw new InvalidOperationException(Localize.From("This Maybe<{0}> does not have a value.", MemoizedTypeName.Get(typeof(T))));
				return _value;
			}
		}

		public static implicit operator Maybe<T>(T value) { return new Maybe<T>(value); }
	}
}
