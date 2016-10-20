using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Math;

namespace Loyc.Collections
{
	/// <summary>Represents a range of integers of a specified data type.</summary>
	/// <typeparam name="Num">Any numeric type</typeparam>
	/// <typeparam name="Math">Trait type that implements math operations for the 
	/// type, e.g. for Int32, use Loyc.Math.MathI.</typeparam>
	/// <remarks>
	/// TODO: unit tests.
	/// <para/>
	/// Note: if the low value is the minimum value of an integer type (e.g. 
	/// int.MinValue of int) then the Enumerator will not work (MoveNext() will return
	/// false). Also, if the difference between the min and max value is int.MaxValue 
	/// or more, the Count property will overflow and return an incorrect value.
	/// </remarks>
	public struct NumRange<Num, Math> : ICollection<Num>, IReadOnlyList<Num>, IIsEmpty
		where Num : IConvertible
		where Math : IMath<Num>, new()
	{
		static Math M = new Math();
		Num _lo, _hi;
		int _count;

		public NumRange(Num low, Num highIncl)
		{
			_lo = low;
			_hi = highIncl;
			_count = M.IsLess(_hi, _lo) ? 0 : M.Sub(_hi, _lo).ToInt32(null) + 1;
		}

		public Num Lo { get { return _lo; } }
		public Num Hi { get { return _hi; } }

		#region IListSource<Num> Members

		public Num TryGet(int index, out bool fail)
		{
			if (fail = ((uint)index >= (uint)_count))
				return default(Num);
			return M.Add(_lo, M.From(index));
		}

		#endregion

		public bool IsEmpty
		{
			get { return Count <= 0; }
		}

		public int Count
		{
			get { return _count; }
		}

		public Num this[int index]
		{
			get {
				if ((uint)index >= (uint)_count)
					CheckParam.ThrowOutOfRange("index", index, 0, _count - 1);
				return M.Add(_lo, M.From(index));
			}
		}
		
		public int IndexOf(Num item)
		{
			if (M.IsLess(item, _lo))
				return -1;
			if (M.IsGreater(item, _hi))
				return -1;
			Num dif = M.Sub(item, _lo);
			if (M.IsInteger || M.Equals(M.Floor(dif), dif))
				return dif.ToInt32(null);
			return -1;
		}

		#region ICollection<int> Members

		void ICollection<Num>.Add(Num item)
		{
			throw new ReadOnlyException("Range<N,M> is read-only.");
		}
		void ICollection<Num>.Clear()
		{
			throw new ReadOnlyException("Range<N,M> is read-only.");
		}
		void ICollection<Num>.CopyTo(Num[] array, int arrayIndex)
		{
			LCInterfaces.CopyTo(this, array, arrayIndex);
		}
		bool ICollection<Num>.IsReadOnly
		{
			get { return true; }
		}
		bool ICollection<Num>.Remove(Num item)
		{
			throw new NotSupportedException("Range<N,M> is read-only.");
		}
		public bool Contains(Num item)
		{
			if (M.IsLess(item, _lo))
				return false;
			if (M.IsGreater(item, _hi))
				return false;
			if (M.IsInteger)
				return true;
			Num dif = M.Sub(item, _lo);
			return M.Equals(M.Floor(dif), dif);
		}

		#endregion

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		IEnumerator<Num> IEnumerable<Num>.GetEnumerator() { return GetEnumerator(); }
		public Enumerator GetEnumerator()
		{
			return new Enumerator(_lo, _hi);
		}

		public struct Enumerator : IEnumerator<Num>
		{
			Num _first, _cur, _last;

			public Enumerator(Num first, Num last)
			{
				_first = first;
				_last = last;
				_cur = M.SubOne(_first);
			}
			public Num Current
			{
				get { return _cur; }
			}
			void IDisposable.Dispose() { }
			object System.Collections.IEnumerator.Current
			{
				get { return Current; }
			}
			public bool MoveNext()
			{
				// Tricky: should work if _last==int.MaxValue, or if _last 
				// is floating-point and (_last-_first) is not an integer.
				// We should also minimize the number of operations here.
				if (M.IsLess(_cur, _last)) {
					var next = M.AddOne(_cur);
					if (M.IsInteger || M.IsLessOrEqual(next, _last)) {
						_cur = next;
						return true;
					}
				}
				return false;
			}
			public void Reset()
			{
				_cur = M.SubOne(_first);
			}
		}
	}
}
