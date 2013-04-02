using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;

namespace Loyc.Collections
{
	/// <summary>An immutable set.</summary>
	/// <remarks>
	/// This is the immutable version of <see cref="ObjectSet{T}"/>. It does not
	/// allow changes to the set, but it provides operators (&, |, ^, -) for 
	/// intersecting, merging, and subtracting sets, and it can be converted to 
	/// a mutable <see cref="ObjectSet{T}"/> in O(1) time. You can also add
	/// single items to the set using operators + and -.
	/// <para/>
	/// For more information, please read the documentation of <see cref="ObjectSet{T}"/> 
	/// and <see cref="InternalSet{T}"/>.
	/// </remarks>
	public struct ObjectSetI<T> : ICollection<T>, ICount
	{
		InternalSet<T> _set;
		IEqualityComparer<T> _comparer;
		int _count;

		public ObjectSetI(IEnumerable<T> list) : this(list, EqualityComparer<T>.Default) { }
		public ObjectSetI(IEqualityComparer<T> comparer) : this(null, comparer) { }
		public ObjectSetI(IEnumerable<T> list, IEqualityComparer<T> comparer)
		{
			_set = new InternalSet<T>();
			_comparer = comparer;
			_count = 0;
			if (list != null) {
				_count = _set.UnionWith(list, Comparer, false);
				_set.CloneFreeze();
			}
		}
		public ObjectSetI(InternalSet<T> set, IEqualityComparer<T> comparer) : this(set, comparer, set.Count()) { }
		internal ObjectSetI(InternalSet<T> set, IEqualityComparer<T> comparer, int count)
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
					return _comparer = EqualityComparer<T>.Default;
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

		/// <summary>Enumerator for <see cref="ObjectSet{T}"/>.</summary>
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

		/// <inheritdoc cref="ObjectSet{T}.Find"/>
		public bool Find(ref T item)
		{
			return _set.Find(ref item, _comparer);
		}

		#region Operators: & | - ^ +
		// Note that if the two operands use different comparers or have different
		// types, the comparer and type of the left operand propagates to the 
		// result. When mixing ObjectSetI<T> and ObjectSet<T>, it is advisable
		// to use ObjectSetI<T> as the left-hand argument because the left-argument
		// is always freeze-cloned, which is a no-op for ObjectSetI<T>.

		public static ObjectSetI<T> operator &(ObjectSetI<T> a, ObjectSetI<T> b)
			{ a._count -= a._set.IntersectWith(b.InternalSet, b.Comparer); return a; }
		public static ObjectSetI<T> operator &(ObjectSetI<T> a, ObjectSet<T> b)
			{ a._count -= a._set.IntersectWith(b.InternalSet, b.Comparer); return a; }
		public static ObjectSetI<T> operator |(ObjectSetI<T> a, ObjectSetI<T> b)
			{ a._count += a._set.UnionWith(b.InternalSet, a.Comparer, false); return a; }
		public static ObjectSetI<T> operator |(ObjectSetI<T> a, ObjectSet<T> b)
			{ a._count += a._set.UnionWith(b.InternalSet, a.Comparer, false); return a; }
		public static ObjectSetI<T> operator -(ObjectSetI<T> a, ObjectSetI<T> b)
			{ a._count -= a._set.ExceptWith(b.InternalSet, a.Comparer); return a; }
		public static ObjectSetI<T> operator -(ObjectSetI<T> a, ObjectSet<T> b)
			{ a._count -= a._set.ExceptWith(b.InternalSet, a.Comparer); return a; }
		public static ObjectSetI<T> operator ^(ObjectSetI<T> a, ObjectSetI<T> b)
			{ a._count += a._set.SymmetricExceptWith(b.InternalSet, a.Comparer); return a; }
		public static ObjectSetI<T> operator ^(ObjectSetI<T> a, ObjectSet<T> b)
			{ a._count += a._set.SymmetricExceptWith(b.InternalSet, a.Comparer); return a; }
		public static explicit operator ObjectSetI<T>(ObjectSet<T> a)
			{ return new ObjectSetI<T>(a.InternalSet, a.Comparer, a.Count); }

		public static ObjectSetI<T> operator +(T item, ObjectSetI<T> a) { return a + item; }
		public static ObjectSetI<T> operator +(ObjectSetI<T> a, T item)
		{
			if (a._set.Add(ref item, a.Comparer, false))
				a._count++;
			return a;
		}
		public static ObjectSetI<T> operator -(ObjectSetI<T> a, T item)
		{
			if (a._set.Remove(item, a.Comparer))
				a._count--;
			return a;
		}

		#endregion
		
		/// <summary>Returns a new set that contains only items that match the 
		/// specified predicate (i.e. for which the predicate returns true).</summary>
		public ObjectSetI<T> Where(Predicate<T> match)
		{
			var result = new ObjectSetI<T>(_comparer);
			foreach (var item in this) {
				var item2 = item;
				if (match(item))
					if (result._set.Add(ref item2, _comparer, false))
						result._count++;
			}
			return result;
		}
	}

	/// <summary>An immutable set of <see cref="Symbol"/>s.</summary>
	/// <remarks>
	/// This is the immutable version of <see cref="SymbolSet"/>. It does not
	/// allow changes to the set, but it provides operators (&, |, ^, -) for 
	/// intersecting, merging, and subtracting sets, and it can be converted to 
	/// a mutable <see cref="SymbolSet"/> in O(1) time. You can also add
	/// single items to the set using operators + and -.
	/// <para/>
	/// For more information, please read the documentation of 
	/// <see cref="ObjectSet{T}"/> and <see cref="InternalSet{T}"/>.
	/// </remarks>
	public struct SymbolSetI : ICollection<Symbol>, ICount
	{
		InternalSet<Symbol> _set;
		int _count;

		public SymbolSetI(IEnumerable<Symbol> list)
		{
			_set = new InternalSet<Symbol>(list, null);
			_count = 0;
		}
		public SymbolSetI(InternalSet<Symbol> set) : this(set, set.Count()) { }
		internal SymbolSetI(InternalSet<Symbol> set, int count)
		{
			_set = set;
			_count = count;
			set.CloneFreeze();
		}

		public InternalSet<Symbol> InternalSet { get { return _set; } }

		#region ICollection<Symbol>

		public bool Contains(Symbol item)
		{
			return _set.Find(ref item, null);
		}
		public void CopyTo(Symbol[] array, int arrayIndex)
		{
			if (_count > array.Length - arrayIndex)
				throw new ArgumentException(Localize.From("CopyTo: Insufficient space in supplied array"));
			_set.CopyTo(array, arrayIndex);
		}
		public int Count { get { return _count; } }
		public Enumerator GetEnumerator() { return new Enumerator(_set); }

		/// <summary>Enumerator for <see cref="ObjectSet{T}"/>.</summary>
		/// <remarks>This is a wrapper of <see cref="InternalSet{T}.Enumerator"/> 
		/// that blocks editing functionality.</remarks>
		public struct Enumerator : IEnumerator<Symbol>
		{
			internal Enumerator(InternalSet<Symbol> set) { _e = new InternalSet<Symbol>.Enumerator(set); }
			InternalSet<Symbol>.Enumerator _e;

			public Symbol Current { get { return _e.Current; } }
			public bool MoveNext() { return _e.MoveNext(); }

			void IDisposable.Dispose() { }
			object System.Collections.IEnumerator.Current { get { return Current; } }
			void System.Collections.IEnumerator.Reset() { throw new NotSupportedException(); }
		}

		public bool IsReadOnly { get { return true; } }
		void ICollection<Symbol>.Add(Symbol item) { throw new ReadOnlyException(); }
		void ICollection<Symbol>.Clear() { throw new ReadOnlyException(); }
		bool ICollection<Symbol>.Remove(Symbol item) { throw new ReadOnlyException(); }
		IEnumerator<Symbol> IEnumerable<Symbol>.GetEnumerator() { return GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

		#endregion

		/// <inheritdoc cref="ObjectSet{T}.Find"/>
		public bool Find(ref Symbol item)
		{
			return _set.Find(ref item, null);
		}

		#region Operators: & | - ^ +
		// When mixing SymbolSetI and ObjectSet<Symbol>/SymbolSet, it's advisable
		// to use SymbolSetI as the left-hand argument because the left-argument
		// is always freeze-cloned, which is a no-op for SymbolSetI.

		public static SymbolSetI operator &(SymbolSetI a, SymbolSetI b)
			{ a._count -= a._set.IntersectWith(b.InternalSet, null); return a; }
		public static SymbolSetI operator &(SymbolSetI a, ObjectSet<Symbol> b)
			{ a._count -= a._set.IntersectWith(b.InternalSet, null); return a; }
		public static SymbolSetI operator |(SymbolSetI a, SymbolSetI b)
			{ a._count += a._set.UnionWith(b.InternalSet, null, false); return a; }
		public static SymbolSetI operator |(SymbolSetI a, ObjectSet<Symbol> b)
			{ a._count += a._set.UnionWith(b.InternalSet, null, false); return a; }
		public static SymbolSetI operator -(SymbolSetI a, SymbolSetI b)
			{ a._count -= a._set.ExceptWith(b.InternalSet, null); return a; }
		public static SymbolSetI operator -(SymbolSetI a, ObjectSet<Symbol> b)
			{ a._count -= a._set.ExceptWith(b.InternalSet, null); return a; }
		public static SymbolSetI operator ^(SymbolSetI a, SymbolSetI b)
			{ a._count += a._set.SymmetricExceptWith(b.InternalSet, null); return a; }
		public static SymbolSetI operator ^(SymbolSetI a, ObjectSet<Symbol> b)
			{ a._count += a._set.SymmetricExceptWith(b.InternalSet, null); return a; }
		public static explicit operator SymbolSetI(ObjectSet<Symbol> a)
			{ return new SymbolSetI(a.InternalSet, a.Count); }

		public static SymbolSetI operator +(Symbol item, SymbolSetI a) { return a + item; }
		public static SymbolSetI operator +(SymbolSetI a, Symbol item)
		{
			if (a._set.Add(ref item, null, false))
				a._count++;
			return a;
		}
		public static SymbolSetI operator -(SymbolSetI a, Symbol item)
		{
			if (a._set.Remove(item, null))
				a._count--;
			return a;
		}

		#endregion
		
		/// <summary>Returns a new set that contains only items that match the 
		/// specified predicate (i.e. for which the predicate returns true).</summary>
		public SymbolSet Where(Predicate<Symbol> match)
		{
			var result = new SymbolSet();
			foreach (var item in this) {
				var item2 = item;
				if (match(item))
					if (result._set.Add(ref item2, null, false))
						result._count++;
			}
			return result;
		}
	}
}
