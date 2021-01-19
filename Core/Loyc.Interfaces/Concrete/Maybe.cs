using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Loyc
{
	public static class Maybe
	{
		/// <summary>Returns <c>new Maybe&lt;T>(value)</c>. (exists for type inference)</summary>
		public static Maybe<T> Value<T>(T value) { return new Maybe<T>(value); }

		/// <summary>Converts <see cref="Maybe{T}"/> to a <see cref="Nullable{T}"/> having the same HasValue property.</summary>
		public static T? AsNullable<T>(this Maybe<T> val) where T : struct 
			{ return val.HasValue ? val.Value : (T?)null; }
		/// <summary>Creates a <see cref="Maybe{T}"/>, using <see cref="Maybe{T}.NoValue"/> if and only if the input is null.</summary>
		public static Maybe<T> AsMaybe<T>(this Nullable<T> val) where T : struct 
			{ return val.HasValue ? val.Value : Maybe<T>.NoValue; }
		/// <summary>Creates a <see cref="Maybe{T}"/>, using <see cref="Maybe{T}.NoValue"/> if and only if the input is null.</summary>
		public static Maybe<T> AsMaybe<T>(this T val) where T : class
			{ return val != null ? val : Maybe<T>.NoValue; }

		/// <summary>Converts <see cref="IMaybe{T}"/> to T, returning a default value if <see cref="HasValue"/> is false.</summary>
		/// <remarks>This is like the <c>??</c> operator of <c>T?</c>.</remarks>
		public static T Or<M, T>(this M maybe, T defaultValue) where M : IMaybe<T> => maybe.HasValue ? maybe.Value : defaultValue;
		/// <summary>Converts <see cref="IMaybe{T}"/> to T, calling a factory function if <see cref="HasValue"/> is false.</summary>
		/// <remarks>This is like the <c>??</c> operator of <c>T?</c>.</remarks>
		public static T Or<M, T>(this M maybe, Func<T> getDefaultValue) where M : IMaybe<T> => maybe.HasValue ? maybe.Value : getDefaultValue();

		/// <summary>Runs a function if and only if <see cref="IMaybe{T}.HasValue"/>.</summary>
		public static void Then<T>(this IMaybe<T> maybe, Action<T> then) { if (maybe.HasValue) then(maybe.Value); }
		/// <summary>Runs one of two functions depending on whether <see cref="IMaybe{T}.HasValue"/>.</summary>
		public static R Then<T, R>(this IMaybe<T> maybe, Func<T, R> then, Func<R> @else) => maybe.HasValue ? then(maybe.Value) : @else();
		/// <summary>Runs a function if and only if <see cref="IMaybe{T}.HasValue"/>, returning a default value otherwise.</summary>
		public static R Then<T, R>(this IMaybe<T> maybe, Func<T, R> then, R defaultValue) => maybe.HasValue ? then(maybe.Value) : defaultValue;
	}

	/// <summary>Same as <see cref="Nullable{T}"/> except that it behaves like a
	/// normal type, i.e. (1) T is allowed to be a reference type and (2) you can
	/// nest them, as in <c>Maybe{Maybe{int}}</c>.</summary>
	/// <remarks>The name of this type comes from some functional programming 
	/// languages which define a type such as <c>data Maybe t = Nothing | Just t</c>.
	/// <para/>
	/// This type exists primarily for generic code, since C# does not allow you
	/// to write a generic function that returns "T?" if T could be a class.
	/// Places where this type is used include the DictionaryExt.TryGetValue(key) 
	/// extension method, and the ILexer&lt;T>.NextToken() method. Both of these
	/// may not be able to return a value, but they cannot return T? because T 
	/// could be a class (or even a Nullable-of-something).
	/// <para/>
	/// There is an implicit conversion from T that returns <c>new Maybe{T}(value)</c>,
	/// and from <see cref="NoValue.Value"/> that returns <see cref="Maybe{T}.NoValue"/>.
	/// Since C# doesn't allow us to define conversions to/from <c>T?</c>, these
	/// conversions can be accomplished with the extension methods <see cref="Maybe.AsNullable"/>
	/// and <see cref="Maybe.AsMaybe"/>.
	/// <para/>
	/// The <see cref="Or"/> method replicates the C# <c>??</c> operator.
	/// </remarks>
	[DebuggerDisplay("{HasValue ? (object)Value : Loyc.NoValue.Value}")]
	public struct Maybe<T> : IMaybe<T>, IEquatable<Maybe<T>>, IEquatable<IMaybe<T>>
	{
		public static Maybe<T> NoValue { get { return new Maybe<T>(); } }
		public Maybe(T value) { _value = value; _hasValue = true; }
		
		readonly T _value;
		readonly bool _hasValue;

		public bool HasValue { get { return _hasValue; } }
		public T Value { 
			get {
				if (!_hasValue)
					throw new InvalidOperationException("This Maybe<{0}> does not have a value.".Localized(MemoizedTypeName.Get(typeof(T))));
				return _value;
			}
		}

		public static implicit operator Maybe<T>(T value) { return new Maybe<T>(value); }
		public static implicit operator Maybe<T>(NoValue _) { return new Maybe<T>(); }

		public bool Equals(Maybe<T> obj)
			=> _hasValue == obj._hasValue && (!_hasValue || (_value == null ? obj.Value == null : _value.Equals(obj.Value)));
		public bool Equals(IMaybe<T> obj) => obj != null
			&& _hasValue == obj.HasValue && (!_hasValue || (_value == null ? obj.Value == null : _value.Equals(obj.Value)));
		public override bool Equals(object obj) => Equals(obj as IMaybe<T>);
		public override int GetHashCode() => !_hasValue ? -1 : _value == null ? 0 : _value.GetHashCode();

		public override string ToString()
		{
			return _hasValue ? "Value: {0}".Localized(_value) : Loyc.NoValue.Value.ToString();
		}

		// Although C# can now infer type parameters on an extension method like 
		//    public static T Or<M,T>(this M m, T defVal) where M: IMaybe<T> => m.HasValue ? m.Value : defVal;
		// This fails if M is Maybe<object> and T is string, so the following is also defined:

		/// <summary>Converts <see cref="Maybe{T}"/> to T, returning a default value if <see cref="HasValue"/> is false.</summary>
		/// <remarks>This is equivalent to the <c>??</c> operator of <c>T?</c>.</remarks>
		public T Or(T defaultValue) => _hasValue ? _value : defaultValue;
	}
}
