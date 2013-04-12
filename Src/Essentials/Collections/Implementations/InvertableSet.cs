using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>An immutable set that can be inverted. For example, an 
	/// <c>InvertableSet&lt;int></c> could contain "everything except 4 and 10",
	/// or it could contain a positive set such as "1, 2, and 3".</summary>
	/// <remarks>
	/// <c>InvertableSet</c> is implemented as a normal <see cref="Set{T}"/> plus
	/// an <see cref="IsInverted"/> flag. The original (non-inverted) set can
	/// be retrieved from the <see cref="BaseSet"/> property
	/// <para/>
	/// <b>Note:</b> this class is designed with the assumption that there are an
	/// infinite number of possible T objects, and under certain conditions the
	/// set-testing operations such as Equals() and IsSubsetOf() can return false
	/// when they should return true. For example, consider two sets of bytes:
	/// one set holds the numbers 0..100, and the other contains 101..255 but is
	/// marked as inverted. Arguably, Equals() and IsSubsetOf() should return true
	/// when comparing these sets, but they return false because they are unaware 
	/// of the finite nature of a byte.
	/// </remarks>
	public class InvertableSet<T> : ISetImm<T, InvertableSet<T>>, IEquatable<InvertableSet<T>>
	{
		Set<T> _set;
		bool _inverted;

		public InvertableSet(Set<T> set, bool inverted)
			{ _inverted = inverted; _set = set; }
		public InvertableSet(IEnumerable<T> list, bool inverted = false)
			{ _set = new Set<T>(list); _inverted = inverted; }
		public InvertableSet(IEnumerable<T> list, IEqualityComparer<T> comparer, bool inverted = false)
			{ _set = new Set<T>(list, comparer); _inverted = inverted; }

		public Set<T> BaseSet { get { return _set; } }
		public bool IsInverted { get { return _inverted; } }
		public bool IsEmpty { get { return !_inverted && _set.IsEmpty; } }
		public bool ContainsEverything { get { return _inverted && _set.IsEmpty; } }
		public InvertableSet<T> Inverted() { return new InvertableSet<T>(_set, !_inverted); }

		public bool Contains(T item)
		{
			return _set.Contains(item) ^ _inverted;
		}

		public InvertableSet<T> Without(T item) { return With(item, true); }
		public InvertableSet<T> With(T item) { return With(item, false); }
		protected InvertableSet<T> With(T item, bool removed)
		{
			if (_inverted ^ removed)
				return new InvertableSet<T>(_set.Without(item), _inverted);
			else
				return new InvertableSet<T>(_set.With(item), _inverted);
		}
		public InvertableSet<T> Union(InvertableSet<T> other)
		{
			// if either set is inverted, the result must be inverted.
			// if both sets are inverted, the base sets should be intersected.
			// if only one set is inverted, the other set must be subtracted from it.
			if (_inverted || other._inverted) {
				if (_inverted && other._inverted)
					return new InvertableSet<T>(_set.Intersect(other._set), true);
				else
					return new InvertableSet<T>(_set.Except(other._set), true);
			}
			return new InvertableSet<T>(_set.Union(other._set), false);
		}
		public InvertableSet<T> Intersect(InvertableSet<T> other) { return Intersect(other, false); }
		public InvertableSet<T> Intersect(InvertableSet<T> other, bool subtractOther)
		{
			bool otherInverted = other._inverted ^ subtractOther;
			// The result is inverted iff both inputs are inverted.
			// if both sets are inverted, the base sets should be unioned.
			// if only one set is inverted, the base set of the inverted one
			// should be subtracted from the set that is not inverted.
			if (_inverted || otherInverted) {
				if (_inverted) {
					if (otherInverted)
						return new InvertableSet<T>(_set.Union(other._set), true);
					else
						return new InvertableSet<T>(other._set.Except(_set), false);
				} else
					return new InvertableSet<T>(_set.Except(other._set), false);
			}
			return new InvertableSet<T>(_set.Intersect(other._set), false);
		}
		public InvertableSet<T> Except(InvertableSet<T> other)
		{
			// Subtraction is equivalent to intersection with the inverted set.
			return Intersect(other, true);
		}
		public InvertableSet<T> Xor(InvertableSet<T> other)
		{
			return new InvertableSet<T>(_set.Xor(other._set), _inverted ^ other._inverted);
		}

		public bool Equals(InvertableSet<T> other) { return SetEquals(other); }

		#region ISetImm<T, InvertableSet<T>>: IsSubsetOf, IsSupersetOf, Overlaps, IsProperSubsetOf, IsProperSupersetOf, SetEquals
		// Remember to keep this code in sync with MSet<T> (the copies can be identical)

		/// <summary>Returns true if all items in this set are present in the other set.</summary>
		public bool IsSubsetOf(InvertableSet<T> other) 
		{
			throw new NotImplementedException();
		}
		/// <summary>Returns true if all items in the other set are present in this set.</summary>
		public bool IsSupersetOf(InvertableSet<T> other)
		{
			throw new NotImplementedException();
		}
		/// <summary>Returns true if this set contains at least one item from 'other'.</summary>
		public bool Overlaps(InvertableSet<T> other)
		{
			throw new NotImplementedException();
		}
		/// <inheritdoc cref="InternalSet{T}.IsProperSubsetOf(ISet{T}, int)"/>
		public bool IsProperSubsetOf(InvertableSet<T> other)
		{
			throw new NotImplementedException();
		}
		/// <inheritdoc cref="InternalSet{T}.IsProperSupersetOf(ISet{T}, IEqualityComparer{T}, int)"/>
		public bool IsProperSupersetOf(InvertableSet<T> other)
		{
			throw new NotImplementedException();
		}
		public bool SetEquals(InvertableSet<T> other)
		{
			return _inverted == other._inverted && _set.SetEquals(other._set);
		}

		#endregion

		IEnumerator<T> IEnumerable<T>.GetEnumerator() { return _set.GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return _set.GetEnumerator(); }
		int ICount.Count { get { throw new NotImplementedException(); } }
	}

}
