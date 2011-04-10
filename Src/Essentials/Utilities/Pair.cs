using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Essentials
{
	public static class Pair
	{
		public static Pair<T1, T2> Create<T1, T2>(T1 item1, T2 item2)
			{ return new Pair<T1, T2>(item1, item2); }
		public static Pair<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3) 
			{ return new Pair<T1, T2, T3>(item1, item2, item3); }
		public static Pair<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4)
			{ return new Pair<T1, T2, T3, T4>(item1, item2, item3, item4); }
	}
	public struct Pair<T1,T2>
	{
		public Pair(T1 a, T2 b) { A = a; B = b; }
		public T1 A;
		public T2 B;
		public T1 Item1 { [DebuggerStepThrough] get { return A; } set { A = value; } }
		public T2 Item2 { [DebuggerStepThrough] get { return B; } set { B = value; } }
		public T1 Key   { [DebuggerStepThrough] get { return A; } set { A = value; } }
		public T2 Value { [DebuggerStepThrough] get { return B; } set { B = value; } }
	}
	public struct Pair<T1, T2, T3>
	{
		public Pair(T1 a, T2 b, T3 c) { A = a; B = b; C = c; }
		public T1 A;
		public T2 B;
		public T3 C;
		public T1 Item1 { [DebuggerStepThrough] get { return A; } set { A = value; } }
		public T2 Item2 { [DebuggerStepThrough] get { return B; } set { B = value; } }
		public T3 Item3 { [DebuggerStepThrough] get { return C; } set { C = value; } }
	}
	public struct Pair<T1, T2, T3, T4>
	{
		public Pair(T1 a, T2 b, T3 c, T4 d) { A = a; B = b; C = c; D = d; }
		public T1 A;
		public T2 B;
		public T3 C;
		public T4 D;
		public T1 Item1 { [DebuggerStepThrough] get { return A; } set { A = value; } }
		public T2 Item2 { [DebuggerStepThrough] get { return B; } set { B = value; } }
		public T3 Item3 { [DebuggerStepThrough] get { return C; } set { C = value; } }
		public T4 Item4 { [DebuggerStepThrough] get { return D; } set { D = value; } }
	}
}
