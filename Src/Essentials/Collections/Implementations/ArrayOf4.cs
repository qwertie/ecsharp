using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Loyc.Essentials.Collections.Implementations
{
	class ArrayOf4<T>
	{
		T t0, t1, t2, t3;

		public T this[int index]
		{
			get {
				Debug.Assert((uint)index < (uint)4);
				if (index < 2)
					return index > 0 ? t1 : t0;
				else
					return index == 2 ? t2 : t3;
			}
			set {
				Debug.Assert((uint)index < (uint)4);
				if (index < 2) {
					if (index == 0)
						t0 = value;
					else
						t1 = value;
				} else {
					if (index == 2)
						t2 = value;
					else
						t3 = value;
				}
			}
		}
		public T Insert(int index, T item)
		{
			T popped = t3;
			t3 = t2;
			if (index < 2) {
				t2 = t1;
				if (index == 0)
					t0 = item;
				else
					t1 = item;
				return popped;
			} else {
				if (index == 2)
					t2 = item;
				else
					t3 = item;
				return popped;
			}
		}
		public void RemoveAt(int index, T newFourth)
		{
			if (index < 2)
			{
				if (index == 0)
					t0 = t1;
				t1 = t2;
			}
			if (index == 2)
				t2 = t3;
			t3 = newFourth;
		}
		public T First
		{
			get { return t0; }
			set { t0 = value; }
		}
	}
}
