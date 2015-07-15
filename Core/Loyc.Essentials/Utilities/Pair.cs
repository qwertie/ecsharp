using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc
{
	/// <summary><c>Pair.Create(a, b)</c> is a helper method for making pairs.</summary>
	/// <remarks>To avoid creating an extra class, this class also contains 
	/// <c>Pair.Create(a, b, c)</c> which makes triplet structs.</remarks>
	public static class Pair
	{
		public static Pair<T1, T2> Create<T1, T2>(T1 item1, T2 item2)
			{ return new Pair<T1, T2>(item1, item2); }
		public static Triplet<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3) 
			{ return new Triplet<T1, T2, T3>(item1, item2, item3); }
	}

	/// <summary>A tuple of two values, <c>A</c> and <c>B</c>, in a struct.</summary>
	/// <remarks>
	/// The BCL has a <see cref="KeyValuePair{A,B}"/> structure that has two problems:
	/// not all pairs are key-value pairs, and its name is overly long and clumsy.
	/// There is also a <see cref="Tuple{T1,T2}"/> type, whose problem is that it 
	/// requires a heap allocation.
	/// <para/>
	/// For compatibility with <see cref="KeyValuePair{A,B}"/>, this 
	/// structure has <c>Key</c> and <c>Value</c> properties. For compatibility
	/// with <see cref="Tuple{A,B}"/>, it has <c>Item1</c> and <c>Item2</c> 
	/// properties. Respectively, these properties refer to the A and B fields.
	/// <para/>
	/// This is a mutable structure. Some people fear mutable structures, but I have 
	/// heard all the arguments, and find them unpersuasive. The most common pitfall, 
	/// changing a copy and expecting a different copy to change, is nothing more than
	/// ignorance about how structs work. The second most common pitfall involves 
	/// mutator methods inside a struct, but this struct doesn't have any of those
	/// (and the problem would largely be fixed by a compiler-recognized attribute 
	/// like <c>[Mutates]</c> that would detect potential problems).
	/// </remarks>
	[Serializable]
	[DebuggerDisplay("A = {A}, B = {B}")]
	public struct Pair<T1, T2> : IComparable, IComparable<Pair<T1, T2>>, IEquatable<Pair<T1, T2>>, IHasValue<T2>
	{
		public Pair(T1 a, T2 b) { A = a; B = b; }
		public T1 A;
		public T2 B;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)] // reduce clutter in debugger
		public T1 Item1 { [DebuggerStepThrough] get { return A; } set { A = value; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)] // reduce clutter in debugger
		public T2 Item2 { [DebuggerStepThrough] get { return B; } set { B = value; } }
		
		[DebuggerBrowsable(DebuggerBrowsableState.Never)] // reduce clutter in debugger
		public T1 Key   { [DebuggerStepThrough] get { return A; } set { A = value; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)] // reduce clutter in debugger
		public T2 Value { [DebuggerStepThrough] get { return B; } set { B = value; } }

		static readonly EqualityComparer<T1> T1Comparer = EqualityComparer<T1>.Default;
		static readonly EqualityComparer<T2> T2Comparer = EqualityComparer<T2>.Default;

		public bool Equals(Pair<T1,T2> rhs)
		{
			return T1Comparer.Equals(A, rhs.A) &&
				T2Comparer.Equals(B, rhs.B);
		}
		public static bool operator ==(Pair<T1, T2> a, Pair<T1, T2> b) { return a.Equals(b); }
		public static bool operator !=(Pair<T1, T2> a, Pair<T1, T2> b) { return !a.Equals(b); }
		public override bool Equals(object obj)
		{
			if (obj is Pair<T1, T2>)
				return Equals((Pair<T1, T2>) obj);
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

	/// <summary>A tuple of three values (<c>A</c>, <c>B</c> and <c>C</c>) in a struct.</summary>
	/// <remarks>
	/// For compatibility with <see cref="Tuple{A,B,C}"/>, it has <c>Item1</c>, 
	/// <c>Item2</c> and <c>Item3</c> properties, which refer to the A, B and 
	/// C fields, respectively.
	/// </remarks>
	[Serializable]
	[DebuggerDisplay("A = {A}, B = {B}, C = {C}")]
	public struct Triplet<T1, T2, T3>
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

		static readonly EqualityComparer<T1> T1Comp = EqualityComparer<T1>.Default;
		static readonly EqualityComparer<T2> T2Comp = EqualityComparer<T2>.Default;
		static readonly EqualityComparer<T3> T3Comp = EqualityComparer<T3>.Default;

		public bool Equals(Triplet<T1, T2, T3> rhs)
		{
			return T1Comp.Equals(A, rhs.A) && T2Comp.Equals(B, rhs.B) && T3Comp.Equals(C, rhs.C);
		}
		public static bool operator ==(Triplet<T1, T2, T3> a, Triplet<T1, T2, T3> b) { return a.Equals(b); }
		public static bool operator !=(Triplet<T1, T2, T3> a, Triplet<T1, T2, T3> b) { return !a.Equals(b); }
		public override bool Equals(object obj)
		{
			if (obj is Triplet<T1, T2, T3>)
				return Equals((Triplet<T1, T2, T3>) obj);
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
	public class Holder<T> : IHasValue<T>
	{
		public Holder(T value) { Value = value; }
		public Holder() { }

		/// <summary>Any value of type T.</summary>
		public T Value;
		T IHasValue<T>.Value { get { return Value; } }

		public override bool Equals(object obj)
		{
			if (obj is Holder<T>)
				obj = ((Holder<T>)obj).Value;
			return Value == null ? obj == null : Value.Equals(obj);
		}
		public override int GetHashCode()
		{
			return Value == null ? 0 : Value.GetHashCode();
		}
		public override string ToString()
		{
			return Value == null ? null : Value.ToString();
		}
		public static implicit operator Holder<T>(T value) { return new Holder<T>(value); }
	}
}
