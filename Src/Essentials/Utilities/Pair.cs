using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Loyc.Essentials;

namespace Loyc
{
	public static class Pair
	{
		public static Pair<T1, T2> Create<T1, T2>(T1 item1, T2 item2)
			{ return new Pair<T1, T2>(item1, item2); }
		public static Triplet<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3) 
			{ return new Triplet<T1, T2, T3>(item1, item2, item3); }
	}

	/// <summary>A tuple of two values, in a struct.</summary>
	/// <remarks>For compatibility with <see cref="KeyValuePair{A,B}"/>, this 
	/// structure has <c>Key</c> and <c>Value</c> properties. For compatibility
	/// with <see cref="Tuple{A,B}"/>, it has <c>Item1</c> and <c>Item2</c> 
	/// properties. Respectively, these properties refer to the A and B fields.</remarks>
	public struct Pair<T1,T2> : IComparable, IComparable<Pair<T1, T2>>
	{
		public Pair(T1 a, T2 b) { A = a; B = b; }
		public T1 A;
		public T2 B;
		public T1 Item1 { [DebuggerStepThrough] get { return A; } set { A = value; } }
		public T2 Item2 { [DebuggerStepThrough] get { return B; } set { B = value; } }
		
		[DebuggerBrowsable(DebuggerBrowsableState.Never)] // reduce clutter in debugger
		public T1 Key   { [DebuggerStepThrough] get { return A; } set { A = value; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)] // reduce clutter in debugger
		public T2 Value { [DebuggerStepThrough] get { return B; } set { B = value; } }

		static readonly EqualityComparer<T1> T1Comparer = EqualityComparer<T1>.Default;
		static readonly EqualityComparer<T2> T2Comparer = EqualityComparer<T2>.Default;

		public override bool Equals(object obj)
		{
			if (obj is Pair<T1, T2>)
			{
				Pair<T1, T2> rhs = (Pair<T1, T2>)obj;
				return T1Comparer.Equals(A, rhs.A) && 
					T2Comparer.Equals(B, rhs.B);
			}
			return false;
		}
		public override int GetHashCode()
		{
			return T1Comparer.GetHashCode(A) ^ T2Comparer.GetHashCode(B);
		}
		public override string ToString()
		{
			return string.Format("({0}, {1})", A, B);
		}
		public int CompareTo(Pair<T1, T2> other)
		{
			int c = Comparer<T1>.Default.Compare(A, other.A);
			if (c == 0)
				c = Comparer<T2>.Default.Compare(B, other.B);
			return c;
		}
		public int CompareTo(object obj)
		{
			return CompareTo((Pair<T1, T2>)obj);
		}
	}

	/// <summary>A tuple of three values, in a struct.</summary>
	public struct Triplet<T1, T2, T3>
	{
		public Triplet(T1 a, T2 b, T3 c) { A = a; B = b; C = c; }
		public T1 A;
		public T2 B;
		public T3 C;
		public T1 Item1 { [DebuggerStepThrough] get { return A; } set { A = value; } }
		public T2 Item2 { [DebuggerStepThrough] get { return B; } set { B = value; } }
		public T3 Item3 { [DebuggerStepThrough] get { return C; } set { C = value; } }

		static readonly EqualityComparer<T1> T1Comp = EqualityComparer<T1>.Default;
		static readonly EqualityComparer<T2> T2Comp = EqualityComparer<T2>.Default;
		static readonly EqualityComparer<T3> T3Comp = EqualityComparer<T3>.Default;

		public override bool Equals(object obj)
		{
			if (obj is Triplet<T1, T2, T3>)
			{
				Triplet<T1, T2, T3> rhs = (Triplet<T1, T2, T3>)obj;
				return T1Comp.Equals(A, rhs.A) && T2Comp.Equals(B, rhs.B) && T3Comp.Equals(C, rhs.C);
			}
			return false;
		}
		public override int GetHashCode()
		{
			return T1Comp.GetHashCode(A) ^ T2Comp.GetHashCode(B) ^ T3Comp.GetHashCode(C);
		}
		public override string ToString()
		{
			return string.Format("({0},{1},{2})", A, B, C);
		}
		public int CompareTo(Triplet<T1, T2, T3> other)
		{
			int c = Comparer<T1>.Default.Compare(A, other.A);
			if (c == 0) {
				c = Comparer<T2>.Default.Compare(B, other.B);
				if (c == 0) {
					c = Comparer<T3>.Default.Compare(C, other.C);
				}
			}
			return c;
		}
		public int CompareTo(object obj)
		{
			return CompareTo((Triplet<T1, T2, T3>)obj);
		}
	}

	/// <summary>A trivial class that holds a single value of type T in the 
	/// <see cref="Value"/> property.
	/// </summary><remarks>
	/// This class is useful mainly as an alternative to standard boxing. When you 
	/// box a structure in C#, you lose access to the members of that structure.
	/// This class, in contrast, provides access to the "boxed" value.
	/// </remarks>
	public class Holder<T> : WrapperBase<T>
	{
		public Holder(T value) : base(value) { }
		public Holder() : base(default(T)) { }

		/// <summary>Any value of type T.</summary>
		public T Value { get { return _obj; } set { _obj = value; } }
	}
}
