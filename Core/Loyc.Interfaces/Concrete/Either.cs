using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc
{
	/// <summary>Holds a single value of one of two types (L or R).</summary>
	/// <remarks>For efficiency, this is a struct, but this makes it possible
	/// to default-construct it. In that case its value will be <c>default(R)</c>.</remarks>
	public struct Either<L, R> : IEither<L, R>, IHasValue<object>
	{
		/// <summary>Simply calls the constructor. This method exists to make
		/// it possible to construct an Either when both types are the same.</summary>
		public static Either<L, R> NewLeft(L value) => new Either<L, R>(value);
		/// <summary>Simply calls the constructor. This method exists to make
		/// it possible to construct an Either when both types are the same.</summary>
		public static Either<L, R> NewRight(R value) => new Either<L, R>(value);

		public Either(L value)
		{
			_hasLeft = true;
			_left = value;
			_right = default(R);
		}
		public Either(R value)
		{
			_hasLeft = false;
			_left = default(L);
			_right = value;
		}
		private Either(bool hasLeft, L left, R right)
		{
			_hasLeft = hasLeft;
			_left = left;
			_right = right;
		}

		private readonly L _left;
		private readonly R _right;
		private readonly bool _hasLeft;

		public Maybe<L> Left => _hasLeft ? _left : new Maybe<L>();
		public Maybe<R> Right => _hasLeft ? new Maybe<R>() : _right;
		public object Value => _hasLeft ? (object)_left : _right;

		IMaybe<L> IEither<L, R>.Left => Left;
		IMaybe<R> IEither<L, R>.Right => Right;

		public static implicit operator Either<L, R>(L value) => NewLeft(value);
		public static implicit operator Either<L, R>(R value) => NewRight(value);

		/// <summary>Does an upcast, e.g. Either{string,ArgumentException} to Either{object,Exception}.
		/// C# does not allow defining conversion operators to take generic 
		/// parameters, so you'll have to put up with this hassle instead.</summary>
		/// <remarks>
		/// Sadly, automatically upcasting value types to reference types doesn't seem possible.
		/// </remarks>
		public static Either<L, R> From<L2, R2>(Either<L2,R2> x)
			where L2 : L 
			where R2 : R
			=> new Either<L, R>(x._hasLeft, x._left, x._right);

		/// <summary>Converts an Either to another with different types.</summary>
		public Either<L2, R2> Select<L2, R2>(Func<L, L2> selectL, Func<R, R2> selectR)
			=> _hasLeft ? new Either<L2, R2>(selectL(_left)) : selectR(_right);
	}
}
