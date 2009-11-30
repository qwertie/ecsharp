using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Runtime
{
	public struct Pair<T1,T2>
	{
		public Pair(T1 a, T2 b) { _1 = a; _2 = b; }
		public T1 _1;
		public T2 _2;
		public T1 A { get { return _1; } set { _1 = value; } }
		public T2 B { get { return _2; } set { _2 = value; } }
		public T1 Key { get { return _1; } set { _1 = value; } }
		public T2 Value { get { return _2; } set { _2 = value; } }
		public T1 First { get { return _1; } set { _1 = value; } }
		public T2 Second { get { return _2; } set { _2 = value; } }
	}
	public struct Tuple<T1, T2, T3>
	{
		public Tuple(T1 a, T2 b, T3 c) { _1 = a; _2 = b; _3 = c; }
		public T1 _1;
		public T2 _2;
		public T3 _3;
		public T1 A { get { return _1; } set { _1 = value; } }
		public T2 B { get { return _2; } set { _2 = value; } }
		public T3 C { get { return _3; } set { _3 = value; } }
		public T1 First { get { return _1; } set { _1 = value; } }
		public T2 Second { get { return _2; } set { _2 = value; } }
		public T3 Third { get { return _3; } set { _3 = value; } }
	}
	public struct Tuple<T1, T2, T3, T4>
	{
		public Tuple(T1 a, T2 b, T3 c, T4 d) { _1 = a; _2 = b; _3 = c; _4 = d; }
		public T1 _1;
		public T2 _2;
		public T3 _3;
		public T4 _4;
		public T1 A { get { return _1; } set { _1 = value; } }
		public T2 B { get { return _2; } set { _2 = value; } }
		public T3 C { get { return _3; } set { _3 = value; } }
		public T4 D { get { return _4; } set { _4 = value; } }
		public T1 First { get { return _1; } set { _1 = value; } }
		public T2 Second { get { return _2; } set { _2 = value; } }
		public T3 Third { get { return _3; } set { _3 = value; } }
		public T4 Fourth { get { return _4; } set { _4 = value; } }
	}
}
