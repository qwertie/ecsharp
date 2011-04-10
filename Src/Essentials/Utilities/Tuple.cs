using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Runtime
{
	public static class Tuple
	{
		public static Tuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2)
			{ return new Tuple<T1, T2>(item1, item2); }
		public static Tuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3)
			{ return new Tuple<T1, T2, T3>(item1, item2, item3); }
		public static Tuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4)
			{ return new Tuple<T1, T2, T3, T4>(item1, item2, item3, item4); }
		public static Tuple<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
			{ return new Tuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5); }
		public static Tuple<T1, T2, T3, T4, T5, T6> Create<T1, T2, T3, T4, T5, T6>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
			{ return new Tuple<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6); }
	}

	public class Tuple<T1, T2>
	{
		public Tuple(T1 a, T2 b) { A = a; B = b; }
		public readonly T1 A;
		public readonly T2 B;
		public T1 Item1 { [DebuggerStepThrough] get { return A; } }
		public T2 Item2 { [DebuggerStepThrough] get { return B; } }

		public override bool Equals(object obj)
		{
			Tuple<T1, T2> rhs = obj as Tuple<T1, T2>;
			if (rhs == null)
				return false;
			return rhs.A.Equals(A) && rhs.B.Equals(B);
		}
		public override int GetHashCode()
		{
			return A.GetHashCode() ^ B.GetHashCode();
		}
		public override string ToString()
		{
			return string.Format("({0},{1})", A, B);
		}
		public int CompareTo(Tuple<T1, T2> other)
		{
			int c = Comparer<T1>.Default.Compare(A, other.A);
			if (c == 0) {
				c = Comparer<T2>.Default.Compare(B, other.B);
			}
			return c;
		}
		public int CompareTo(object obj)
		{
			return CompareTo((Tuple<T1, T2>)obj);
		}
	}

	public class Tuple<T1, T2, T3>
	{
		public Tuple(T1 a, T2 b, T3 c) { A = a; B = b; C = c; }
		public readonly T1 A;
		public readonly T2 B;
		public readonly T3 C;
		public T1 Item1 { [DebuggerStepThrough] get { return A; } }
		public T2 Item2 { [DebuggerStepThrough] get { return B; } }
		public T3 Item3 { [DebuggerStepThrough] get { return C; } }

		public override bool Equals(object obj)
		{
			Tuple<T1, T2, T3> rhs = obj as Tuple<T1, T2, T3>;
			if (rhs == null)
				return false;
			return rhs.A.Equals(A) && rhs.B.Equals(B) && rhs.C.Equals(C);
		}
		public override int GetHashCode()
		{
			return A.GetHashCode() ^ B.GetHashCode() ^ C.GetHashCode();
		}
		public override string ToString()
		{
			return string.Format("({0},{1},{2})", A, B, C);
		}
		public int CompareTo(Tuple<T1, T2, T3> other)
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
			return CompareTo((Tuple<T1, T2, T3>)obj);
		}
	}

	public class Tuple<T1, T2, T3, T4>
	{
		public Tuple(T1 a, T2 b, T3 c, T4 d) { A = a; B = b; C = c; D = d; }
		public readonly T1 A;
		public readonly T2 B;
		public readonly T3 C;
		public readonly T4 D;
		public T1 Item1 { [DebuggerStepThrough] get { return A; } }
		public T2 Item2 { [DebuggerStepThrough] get { return B; } }
		public T3 Item3 { [DebuggerStepThrough] get { return C; } }
		public T4 Item4 { [DebuggerStepThrough] get { return D; } }

		public override bool Equals(object obj)
		{
			Tuple<T1, T2, T3, T4> rhs = obj as Tuple<T1, T2, T3, T4>;
			if (rhs == null)
				return false;
			return rhs.A.Equals(A) && rhs.B.Equals(B) && rhs.C.Equals(C) && rhs.D.Equals(D);
		}
		public override int GetHashCode()
		{
			return A.GetHashCode() ^ B.GetHashCode() ^ C.GetHashCode() ^ D.GetHashCode();
		}
		public override string ToString()
		{
			return string.Format("({0},{1},{2},{3})", A, B, C, D);
		}
		public int CompareTo(Tuple<T1, T2, T3, T4> other)
		{
			int c = Comparer<T1>.Default.Compare(A, other.A);
			if (c == 0) {
				c = Comparer<T2>.Default.Compare(B, other.B);
				if (c == 0) {
					c = Comparer<T3>.Default.Compare(C, other.C);
					if (c == 0) {
						c = Comparer<T4>.Default.Compare(D, other.D);
					}
				}
			}
			return c;
		}
		public int CompareTo(object obj)
		{
			return CompareTo((Tuple<T1, T2, T3, T4>)obj);
		}
	}

	public class Tuple<T1, T2, T3, T4, T5>
	{
		public Tuple(T1 a, T2 b, T3 c, T4 d, T5 e) { A = a; B = b; C = c; D = d; E = e; }
		public readonly T1 A;
		public readonly T2 B;
		public readonly T3 C;
		public readonly T4 D;
		public readonly T5 E;
		public T1 Item1 { [DebuggerStepThrough] get { return A; } }
		public T2 Item2 { [DebuggerStepThrough] get { return B; } }
		public T3 Item3 { [DebuggerStepThrough] get { return C; } }
		public T4 Item4 { [DebuggerStepThrough] get { return D; } }
		public T5 Item5 { [DebuggerStepThrough] get { return E; } }

		public override bool Equals(object obj)
		{
			Tuple<T1, T2, T3, T4, T5> rhs = obj as Tuple<T1, T2, T3, T4, T5>;
			if (rhs == null)
				return false;
			return rhs.A.Equals(A) && rhs.B.Equals(B) && rhs.C.Equals(C) && rhs.D.Equals(D) && rhs.E.Equals(E);
		}
		public override int GetHashCode()
		{
			return A.GetHashCode() ^ B.GetHashCode() ^ C.GetHashCode() ^ D.GetHashCode() ^ E.GetHashCode();
		}
		public override string ToString()
		{
			return string.Format("({0},{1},{2},{3},{4})", A, B, C, D, E);
		}
		public int CompareTo(Tuple<T1, T2, T3, T4, T5> other)
		{
			int c = Comparer<T1>.Default.Compare(A, other.A);
			if (c == 0) {
				c = Comparer<T2>.Default.Compare(B, other.B);
				if (c == 0) {
					c = Comparer<T3>.Default.Compare(C, other.C);
					if (c == 0) {
						c = Comparer<T4>.Default.Compare(D, other.D);
						if (c == 0) {
							c = Comparer<T5>.Default.Compare(E, other.E);
						}
					}
				}
			}
			return c;
		}
		public int CompareTo(object obj)
		{
			return CompareTo((Tuple<T1, T2, T3, T4, T5>)obj);
		}
	}

	public class Tuple<T1, T2, T3, T4, T5, T6> : IComparable<Tuple<T1, T2, T3, T4, T5, T6>>, IComparable
	{
		public Tuple(T1 a, T2 b, T3 c, T4 d, T5 e, T6 f) { A = a; B = b; C = c; D = d; E = e; F = f; }
		public readonly T1 A;
		public readonly T2 B;
		public readonly T3 C;
		public readonly T4 D;
		public readonly T5 E;
		public readonly T6 F;
		public T1 Item1 { [DebuggerStepThrough] get { return A; } }
		public T2 Item2 { [DebuggerStepThrough] get { return B; } }
		public T3 Item3 { [DebuggerStepThrough] get { return C; } }
		public T4 Item4 { [DebuggerStepThrough] get { return D; } }
		public T5 Item5 { [DebuggerStepThrough] get { return E; } }
		public T6 Item6 { [DebuggerStepThrough] get { return F; } }

		public override bool Equals(object obj)
		{
			Tuple<T1, T2, T3, T4, T5, T6> rhs = obj as Tuple<T1, T2, T3, T4, T5, T6>;
			if (rhs == null)
				return false;
			return rhs.A.Equals(A) && rhs.B.Equals(B) && rhs.C.Equals(C) && rhs.D.Equals(D) && rhs.E.Equals(E) && rhs.F.Equals(F);
		}
		public override int GetHashCode()
		{
			return A.GetHashCode() ^ B.GetHashCode() ^ C.GetHashCode() ^ D.GetHashCode() ^ E.GetHashCode() ^ F.GetHashCode();
		}
		public override string ToString()
		{
			return string.Format("({0},{1},{2},{3},{4},{5})", A, B, C, D, E, F);
		}
		public int CompareTo(Tuple<T1, T2, T3, T4, T5, T6> other)
		{
			int c = Comparer<T1>.Default.Compare(A, other.A);
			if (c == 0) {
				c = Comparer<T2>.Default.Compare(B, other.B);
				if (c == 0) {
					c = Comparer<T3>.Default.Compare(C, other.C);
					if (c == 0) {
						c = Comparer<T4>.Default.Compare(D, other.D);
						if (c == 0) {
							c = Comparer<T5>.Default.Compare(E, other.E);
							if (c == 0) {
								c = Comparer<T6>.Default.Compare(F, other.F);
							}
						}
					}
				}
			}
			return c;
		}
		public int CompareTo(object obj)
		{
			return CompareTo((Tuple<T1, T2, T3, T4, T5, T6>)obj);
		}
	}
}
