using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

using Loyc.Compatibility;

namespace Loyc
{
	/// <summary>Holds a single value of one of two types (L or R).</summary>
	/// <remarks>For efficiency, this is a struct, but this makes it possible
	/// to default-construct it. In that case its value will be <c>default(R)</c>.</remarks>
	[System.Diagnostics.DebuggerDisplay("{ToString()}")]
	public struct Either<L, R> : IEither<L, R>, IValue<object?>, IEquatable<Either<L, R>>, IEquatable<ITuple>, ITuple
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

		[AllowNull] private readonly L _left;
		[AllowNull] private readonly R _right;
		private readonly bool _hasLeft;

		public Maybe<L> Left => _hasLeft ? _left : new Maybe<L>();
		public Maybe<R> Right => _hasLeft ? new Maybe<R>() : _right;
		public object? Value => _hasLeft ? (object?)_left : _right;

		int ITuple.Length => 2;
		public object this[int index] => 
			index == 0 ? Left : 
			index == 1 ? (object)Right : 
			throw new IndexOutOfRangeException();

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
		
		/// <summary>Transforms <c>Left</c> with the given selector, if <c>Left.HasValue</c>. Otherwise, returns Right unchanged.</summary>
		public Either<L2, R> MapLeft<L2>(Func<L, L2> selectL)
			=> _hasLeft ? new Either<L2, R>(selectL(_left)) : new Either<L2, R>(_right);
		
		/// <summary>Transforms <c>Right</c> with the given selector, if <c>Right.HasValue</c>. Otherwise, returns Left unchanged.</summary>
		public Either<L, R2> MapRight<R2>(Func<R, R2> selectR)
			=> !_hasLeft ? new Either<L, R2>(selectR(_right)) : new Either<L, R2>(_left);

		/// <summary>Runs actionL if <c>Left.HasValue</c>. Equivalent to <c>Left.Then(actionL)</c>, but also returns <c>this</c>.</summary>
		public Either<L, R> IfLeft(Action<L> actionL) {
			if (_hasLeft)
				actionL(_left);
			return this;
		}

		/// <summary>Runs actionR if <c>Right.HasValue</c>. Equivalent to <c>Right.Then(actionL)</c>, but also returns <c>this</c>.</summary>
		public Either<L, R> IfRight(Action<R> actionR)
		{
			if (!_hasLeft)
				actionR(_right);
			return this;
		}

		public override string ToString()
		{
			return _hasLeft ? "Left: {0}".Localized(_left) : "Right: {0}".Localized(_right);
		}
		public override int GetHashCode()
		{
			return _hasLeft ? _left?.GetHashCode() ?? 0 : ~(_right?.GetHashCode() ?? 0);
		}

		public override bool Equals(object? obj) => Equals(obj as IEither<L, R>);

		public bool Equals(ITuple? other)
		{
			if (other != null)
				return other.Length == 2 && Left.Equals(other[0]) && Right.Equals(other[1]);
			return false;
		}
		public bool Equals(Either<L, R> other)
		{
			if (other._hasLeft == _hasLeft) {
				if (_hasLeft)
					return _left == null ? other._left == null : _left.Equals(other._left);
				else
					return _right == null ? other._right == null : _right.Equals(other._right);
			}
			return false;
		}
	}

	/*
	/// <summary>Holds a single value of one of three types.</summary>
	/// <remarks>For efficiency, this is a struct, but this makes it possible
	/// to default-construct it. In that case its value will be <c>default(A)</c>.</remarks>
	[System.Diagnostics.DebuggerDisplay("{ToString()}")]
	public struct Either<A, B, C> : IEither<A, B, C>, IValue<object>, IEquatable<Either<A, B, C>>, IEquatable<ITuple>
	{
		/// <summary>Simply calls the constructor. This method exists to make
		/// it possible to construct an Either when two or more types are the same.</summary>
		public static Either<A, B, C> New1(A value) => new Either<A, B, C>(value);
		/// <summary>Simply calls the constructor. This method exists to make
		/// it possible to construct an Either when two or more types are the same.</summary>
		public static Either<A, B, C> New2(B value) => new Either<A, B, C>(value);
		/// <summary>Simply calls the constructor. This method exists to make
		/// it possible to construct an Either when two or more types are the same.</summary>
		public static Either<A, B, C> New3(C value) => new Either<A, B, C>(value);

		public Either(A value)
		{
			_index = 0;
			_1 = value;
			_2 = default(B);
			_3 = default(C);
		}
		public Either(B value)
		{
			_index = 1;
			_1 = default(A);
			_2 = value;
			_3 = default(C);
		}
		public Either(C value)
		{
			_index = 2;
			_1 = default(A);
			_2 = default(B);
			_3 = value;
		}
		private Either(sbyte index, A a, B b, C c)
		{
			_index = index;
			_1 = a;
			_2 = b;
			_3 = c;
		}

		private readonly A _1;
		private readonly B _2;
		private readonly C _3;
		private readonly sbyte _index;

		public Maybe<A> Item1 => _index == 0 ? _1 : new Maybe<A>();
		public Maybe<B> Item2 => _index == 1 ? _2 : new Maybe<B>();
		public Maybe<C> Item3 => _index == 2 ? _3 : new Maybe<C>();
		public object Value => _index == 0 ? _1 : _index == 1 ? _2 : (object)_3;

		int ITuple.Length => 3;
		public object this[int index] =>
			index == 0 ? Item1 :
			index == 1 ? Item2 :
			index == 1 ? (object)Item3 :
			throw new IndexOutOfRangeException();

		IMaybe<A> IEither<A,B,C>.Item1 => Item1;
		IMaybe<B> IEither<A,B,C>.Item2 => Item2;
		IMaybe<C> IEither<A,B,C>.Item3 => Item3;

		public static implicit operator Either<A,B,C>(A value) => new Either<A, B, C>(value);
		public static implicit operator Either<A,B,C>(B value) => new Either<A, B, C>(value);
		public static implicit operator Either<A,B,C>(C value) => new Either<A, B, C>(value);

		/// <summary>Does an upcast, e.g. Either{int,ArgumentException,int} to Either{int,Exception,int}.
		/// C# does not allow defining conversion operators to take generic 
		/// parameters, so you'll have to put up with this hassle instead.</summary>
		/// <remarks>
		/// Sadly, automatically upcasting value types to reference types doesn't seem possible.
		/// </remarks>
		public static Either<A,B,C> From<A2, B2, C2>(Either<A2,B2,C2> x)
			where A2 : A
			where B2 : B
			where C2 : C
			=> new Either<A,B,C>(x._index, x._1, x._2, x._3);

		/// <summary>Converts an Either to another with different types.</summary>
		public Either<A2, B2, C2> Select<A2, B2, C2>(Func<A, A2> select1, Func<B, B2> select2, Func<C, C2> select3)
			=> _index == 0 ? new Either<A2, B2, C2>(select1(_1))
			 : _index == 1 ? new Either<A2, B2, C2>(select2(_2))
			 :               new Either<A2, B2, C2>(select3(_3));

		/// <summary>Transforms <c>Item1</c> with the given selector, if <c>Item1.HasValue</c>. Otherwise, returns the value unchanged.</summary>
		public Either<A2, B, C> Map1<A2>(Func<A, A2> select1)
			=> _index == 0 ? new Either<A2, B, C>(select1(_1)) : new Either<A2, B, C>(_index, default, _2, _3);

		/// <summary>Transforms <c>Item2</c> with the given selector, if <c>Item2.HasValue</c>. Otherwise, returns the value unchanged.</summary>
		public Either<A, B2, C> Map2<B2>(Func<B, B2> select2)
			=> _index == 1 ? new Either<A, B2, C>(select2(_2)) : new Either<A, B2, C>(_index, _1, default, _3);

		/// <summary>Transforms <c>Item3</c> with the given selector, if <c>Item3.HasValue</c>. Otherwise, returns the value unchanged.</summary>
		public Either<A, B, C2> Map3<C2>(Func<C, C2> select3)
			=> _index == 2 ? new Either<A, B, C2>(select3(_3)) : new Either<A, B, C2>(_index, _1, _2, default);

		public Maybe<Either<B, C>> Without1() =>
			_index == 1 ? new Either<B, C>(_2) :
			_index == 2 ? new Either<B, C>(_3) : new Maybe<Either<B, C>>();
		public Maybe<Either<A, C>> Without2() =>
			_index == 0 ? new Either<A, C>(_1) :
			_index == 2 ? new Either<A, C>(_3) : new Maybe<Either<A, C>>();
		public Maybe<Either<A, B>> Without3() =>
			_index == 0 ? new Either<A, B>(_1) :
			_index == 1 ? new Either<A, B>(_2) : new Maybe<Either<A, B>>();

		public override string ToString()
		{
			return _index == 0 ? "Item1: {0}".Localized(_1) 
			     : _index == 1 ? "Item2: {0}".Localized(_2)
			     :               "Item3: {0}".Localized(_3);
		}
		public override int GetHashCode()
		{
			return _index == 0 ? _1?.GetHashCode() ?? 0
				 : _index == 1 ? (_2?.GetHashCode() ?? 0) + 1 
				 :               (_3?.GetHashCode() ?? 0) + 2;
		}

		public override bool Equals(object obj) => Equals(obj as IEither<A, B, C>);

		public bool Equals(ITuple other)
		{
			if (other != null)
				return other.Length == 3 && Item1.Equals(other[0]) && Item2.Equals(other[1]) && Item3.Equals(other[2]);
			return false;
		}
		public bool Equals(Either<A, B, C> other)
		{
			if (other._index == _index) {
				if (_index == 0)
					return _1 == null ? other._1 == null : _1.Equals(other._1);
				else if (_index == 1)
					return _2 == null ? other._2 == null : _2.Equals(other._2);
				else if (_index == 2)
					return _3 == null ? other._3 == null : _3.Equals(other._3);
			}
			return false;
		}
	}
	*/
}
