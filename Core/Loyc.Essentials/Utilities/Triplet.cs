using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Loyc.Compatibility;

namespace Loyc
{
	public static partial class Triplet
	{
		public static Triplet<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3)
			{ return new Triplet<T1, T2, T3>(item1, item2, item3); }
	}

	/// <summary>A tuple of three values (<c>A</c>, <c>B</c> and <c>C</c>) in a struct.</summary>
	/// <remarks>
	/// For compatibility with <see cref="Tuple{A,B,C}"/>, it has <c>Item1</c>, 
	/// <c>Item2</c> and <c>Item3</c> properties, which refer to the A, B and 
	/// C fields, respectively.
	/// </remarks>
	[Serializable]
	[DebuggerDisplay("A = {A}, B = {B}, C = {C}")]
	public struct Triplet<T1, T2, T3> : ITuple
	{
		public Triplet(T1 a, T2 b, T3 c) { A = a; B = b; C = c; }
		public T1 A;
		public T2 B;
		public T3 C;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)] // reduce clutter in debugger
		public T1 Item1 { [DebuggerStepThrough] get { return A; } set { A = value; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)] // reduce clutter in debugger
		public T2 Item2 { [DebuggerStepThrough] get { return B; } set { B = value; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)] // reduce clutter in debugger
		public T3 Item3 { [DebuggerStepThrough] get { return C; } set { C = value; } }

		int ITuple.Length => 3;
		public object? this[int index] => 
			index == 0 ? Item1 : 
			index == 1 ? Item2 : 
			index == 2 ? (object?)Item3 :
			throw new IndexOutOfRangeException();

		static readonly EqualityComparer<T1> T1Comp = EqualityComparer<T1>.Default;
		static readonly EqualityComparer<T2> T2Comp = EqualityComparer<T2>.Default;
		static readonly EqualityComparer<T3> T3Comp = EqualityComparer<T3>.Default;

		public bool Equals(Triplet<T1, T2, T3> rhs)
		{
			return T1Comp.Equals(A, rhs.A) && T2Comp.Equals(B, rhs.B) && T3Comp.Equals(C, rhs.C);
		}
		public static bool operator ==(Triplet<T1, T2, T3> a, Triplet<T1, T2, T3> b) { return a.Equals(b); }
		public static bool operator !=(Triplet<T1, T2, T3> a, Triplet<T1, T2, T3> b) { return !a.Equals(b); }
		public override bool Equals(object? obj)
		{
			if (obj is Triplet<T1, T2, T3>)
				return Equals((Triplet<T1, T2, T3>) obj);
			return false;
		}
		public override int GetHashCode()
		{
			// GetHashCode(null) works (returns 0) but gives us a warning anyway
			return T1Comp.GetHashCode(A!) ^ T2Comp.GetHashCode(B!) ^ T3Comp.GetHashCode(C!);
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
}
