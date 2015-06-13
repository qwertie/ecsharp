using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc
{
	public static class Maybe
	{
		/// <summary>Returns <c>new Maybe&lt;T>(value)</c>. (exists for type inference)</summary>
		public static Maybe<T> Value<T>(T value) { return new Maybe<T>(value); }

		public static T? AsNullable<T>(this Maybe<T> val) where T : struct 
			{ return val.HasValue ? val.Value : (T?)null; }
		public static Maybe<T> AsMaybe<T>(this Nullable<T> val) where T : struct 
			{ return val.HasValue ? val.Value : Maybe<T>.NoValue; }
		public static Maybe<T> AsMaybe<T>(this T val) where T : class
			{ return val != null ? val : Maybe<T>.NoValue; }
	}
	/// <summary>Same as <see cref="Nullable{T}"/> except that it behaves like a
	/// normal type, i.e. (1) T is allowed to be a reference type and (2) you can
	/// nest them, as in <c>Maybe{Maybe{int}}</c>.</summary>
	/// <remarks>The name of this type comes from some functional programming 
	/// languages which define a type such as <c>data Maybe t = Nothing | Just t</c>.
	/// <para/>
	/// There is an implicit conversion from T that returns <c>new Maybe{T}(value)</c>,
	/// and from <see cref="NoValue.Value"/> that returns <see cref="Maybe{T}.NoValue"/>.
	/// Since C# doesn't allow us to define conversions to/from <c>T?</c>, these
	/// conversions can be accomplished with the extension methods <see cref="Maybe.AsNullable"/>
	/// and <see cref="Maybe.AsMaybe"/>.
	/// </remarks>
	public struct Maybe<T>
	{
		public static Maybe<T> NoValue { get { return new Maybe<T>(); } }
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
		public static implicit operator Maybe<T>(NoValue _) { return new Maybe<T>(); }

		/// <summary>Converts <see cref="Maybe{T}"/> to T, returning a default value if <see cref="HasValue"/> is false.</summary>
		/// <remarks>This is equivalent to the <c>??</c> operator of <c>T?</c>.</remarks>
		public T Or(T defaultValue) 
			{ return _hasValue ? _value : defaultValue; }
	}
}
