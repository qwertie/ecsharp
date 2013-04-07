using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;
using System.Diagnostics;

namespace Loyc.Collections
{
	/// <summary>An immutable set.</summary>
	/// <remarks>
	/// This is the immutable version of <see cref="MSet{T}"/>. It does not
	/// allow changes to the set, but it provides operators (&, |, ^, -) for 
	/// intersecting, merging, and subtracting sets, and it can be converted to 
	/// a mutable <see cref="MSet{T}"/> in O(1) time. You can also add
	/// single items to the set using operators + and -.
	/// <para/>
	/// For more information, please read the documentation of <see cref="Set{T}"/> 
	/// and <see cref="InternalSet{T}"/>.
	/// </remarks>
	public struct Set<T> : ICollection<T>, ICount
	{
		InternalSet<T> _set;
		IEqualityComparer<T> _comparer;
		int _count;

		public Set(IEnumerable<T> list) : this(list, InternalSet<T>.DefaultComparer) { }
		public Set(IEqualityComparer<T> comparer) : this(null, comparer) { }
		public Set(IEnumerable<T> list, IEqualityComparer<T> comparer)
		{
			_set = new InternalSet<T>();
			_comparer = comparer;
			_count = 0;
			if (list != null) {
				_count = _set.UnionWith(list, comparer, false);
				_set.CloneFreeze();
			}
			if (comparer == null && !_set.HasRoot) {
				// give it a root so that Comparer does not change _comparer
				_set = InternalSet<T>.Empty;
			}
		}
		public Set(InternalSet<T> set, IEqualityComparer<T> comparer) : this(set, comparer, set.Count()) { }
		internal Set(InternalSet<T> set, IEqualityComparer<T> comparer, int count)
		{
			_set = set;
			_comparer = comparer;
			_count = count;
			set.CloneFreeze();
		}

		public InternalSet<T> InternalSet { get { return _set; } }
		public IEqualityComparer<T> Comparer {
			get {
				if (_comparer == null && !_set.HasRoot)
					return _comparer = InternalSet<T>.DefaultComparer;
				return _comparer;
			}
		}

		#region ICollection<T>

		public bool Contains(T item)
		{
			return _set.Find(ref item, _comparer);
		}
		public void CopyTo(T[] array, int arrayIndex)
		{
			if (_count > array.Length - arrayIndex)
				throw new ArgumentException(Localize.From("CopyTo: Insufficient space in supplied array"));
			_set.CopyTo(array, arrayIndex);
		}
		public int Count { get { return _count; } }
		public Enumerator GetEnumerator() { return new Enumerator(_set); }

		/// <summary>Enumerator for <see cref="MSet{T}"/>.</summary>
		/// <remarks>This is a wrapper of <see cref="InternalSet{T}.Enumerator"/> 
		/// that blocks editing functionality.</remarks>
		public struct Enumerator : IEnumerator<T>
		{
			internal Enumerator(InternalSet<T> set) { _e = new InternalSet<T>.Enumerator(set); }
			InternalSet<T>.Enumerator _e;

			public T Current { get { return _e.Current; } }
			public bool MoveNext() { return _e.MoveNext(); }

			void IDisposable.Dispose() { }
			object System.Collections.IEnumerator.Current { get { return Current; } }
			void System.Collections.IEnumerator.Reset() { throw new NotSupportedException(); }
		}

		public bool IsReadOnly { get { return true; } }
		void ICollection<T>.Add(T item) { throw new ReadOnlyException(); }
		void ICollection<T>.Clear() { throw new ReadOnlyException(); }
		bool ICollection<T>.Remove(T item) { throw new ReadOnlyException(); }
		IEnumerator<T> IEnumerable<T>.GetEnumerator() { return GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

		#endregion

		/// <inheritdoc cref="MSet{T}.Find"/>
		public bool Find(ref T item)
		{
			return _set.Find(ref item, _comparer);
		}

		#region Operators: & | - ^ +
		// Note that if the two operands use different comparers or have different
		// types, the comparer and type of the left operand propagates to the 
		// result. When mixing Set<T> and MSet<T>, it is advisable to use Set<T> 
		// as the left-hand argument because the left-argument is always 
		// freeze-cloned, which is a no-op for Set<T>.

		public static Set<T> operator &(Set<T> a, Set<T> b)
		{
			Debug.Assert(a._set.IsRootFrozen);
			a._count -= a._set.IntersectWith(b.InternalSet, null);
			a._set.CloneFreeze();
			return a;
		}
		public static Set<T> operator &(Set<T> a, MSet<T> b)
		{
			Debug.Assert(a._set.IsRootFrozen);
			a._count -= a._set.IntersectWith(b.InternalSet, null);
			a._set.CloneFreeze();
			return a;
		}
		public static Set<T> operator |(Set<T> a, Set<T> b)
		{
			Debug.Assert(a._set.IsRootFrozen);
			a._count += a._set.UnionWith(b.InternalSet, a.Comparer, false);
			a._set.CloneFreeze();
			return a;
		}
		public static Set<T> operator |(Set<T> a, MSet<T> b)
		{
			Debug.Assert(a._set.IsRootFrozen);
			a._count += a._set.UnionWith(b.InternalSet, a.Comparer, false);
			a._set.CloneFreeze(); 
			return a;
		}
		public static Set<T> operator -(Set<T> a, Set<T> b)
		{
			Debug.Assert(a._set.IsRootFrozen);
			a._count -= a._set.ExceptWith(b.InternalSet, a.Comparer); 
			a._set.CloneFreeze(); 
			return a;
		}
		public static Set<T> operator -(Set<T> a, MSet<T> b)
		{
			Debug.Assert(a._set.IsRootFrozen);
			a._count -= a._set.ExceptWith(b.InternalSet, a.Comparer);
			a._set.CloneFreeze(); 
			return a;
		}
		public static Set<T> operator ^(Set<T> a, Set<T> b)
		{
			Debug.Assert(a._set.IsRootFrozen);
			a._count += a._set.SymmetricExceptWith(b.InternalSet, a.Comparer);
			a._set.CloneFreeze(); 
			return a;
		}
		public static Set<T> operator ^(Set<T> a, MSet<T> b)
		{
			Debug.Assert(a._set.IsRootFrozen);
			a._count += a._set.SymmetricExceptWith(b.InternalSet, a.Comparer);
			a._set.CloneFreeze(); 
			return a;
		}
		public static explicit operator Set<T>(MSet<T> a)
		{
			return new Set<T>(a.InternalSet, a.Comparer, a.Count);
		}

		public static Set<T> operator +(T item, Set<T> a) { return a + item; }
		public static Set<T> operator +(Set<T> a, T item)
		{
			Debug.Assert(a._set.IsRootFrozen);
			if (a._set.Add(ref item, a.Comparer, false))
				a._count++;
			a._set.CloneFreeze();
			return a;
		}
		public static Set<T> operator -(Set<T> a, T item)
		{
			Debug.Assert(a._set.IsRootFrozen);
			if (a._set.Remove(ref item, a.Comparer))
				a._count--;
			a._set.CloneFreeze();
			return a;
		}

		#endregion
		
		/// <summary>Returns a new set that contains only items that match the 
		/// specified predicate (i.e. for which the predicate returns true).</summary>
		public Set<T> Where(Predicate<T> match)
		{
			var result = new Set<T>(_comparer);
			foreach (var item in this) {
				var item2 = item;
				if (match(item))
					if (result._set.Add(ref item2, _comparer, false))
						result._count++;
			}
			return result;
		}
	}
}
