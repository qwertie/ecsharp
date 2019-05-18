using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace Loyc.Collections
{
	/// <summary>An immutable set that can be inverted. For example, an 
	/// <c>InvertibleSet&lt;int></c> could contain "everything except 4 and 10",
	/// or it could contain a positive set such as "1, 2, and 3".</summary>
	/// <remarks>
	/// <c>InvertibleSet</c> is implemented as a normal <see cref="Set{T}"/> plus
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
	/// <para/>
	/// Performance warning: GetHashCode() XORs the hashcodes of all items in the
	/// set, while Equals() is a synonym for SetEquals(). Be aware that these 
	/// methods are very slow for large sets.
	/// </remarks>
	public class InvertibleSet<T> : ISetImm<T, InvertibleSet<T>>, IEquatable<InvertibleSet<T>>
	{
		public static readonly InvertibleSet<T> Empty = new InvertibleSet<T>(Set<T>.Empty, false);
		public static readonly InvertibleSet<T> All = new InvertibleSet<T>(Set<T>.Empty, true);
		public static InvertibleSet<T> With(params T[] list) { return new InvertibleSet<T>(list, false); }
		public static InvertibleSet<T> Without(params T[] list) { return new InvertibleSet<T>(list, true); }

		readonly Set<T> _set;
		readonly bool _inverted;

		public InvertibleSet(Set<T> set, bool inverted)
			{ _inverted = inverted; _set = set; }
		public InvertibleSet(IEnumerable<T> list, bool inverted = false)
			{ _set = new Set<T>(list); _inverted = inverted; }
		public InvertibleSet(IEnumerable<T> list, IEqualityComparer<T> comparer, bool inverted = false)
			{ _set = new Set<T>(list, comparer); _inverted = inverted; }

		public Set<T> BaseSet { get { return _set; } }
		public bool IsInverted { get { return _inverted; } }
		public bool IsEmpty { get { return !_inverted && _set.IsEmpty; } }
		public bool ContainsEverything { get { return _inverted && _set.IsEmpty; } }
		public InvertibleSet<T> Inverted() { return new InvertibleSet<T>(_set, !_inverted); }

		public bool Contains(T item)
		{
			return _set.Contains(item) ^ _inverted;
		}

		public InvertibleSet<T> Without(T item) { return With(item, true); }
		public InvertibleSet<T> With(T item) { return With(item, false); }
		protected InvertibleSet<T> With(T item, bool removed)
		{
			if (_inverted ^ removed)
				return new InvertibleSet<T>(_set.Without(item), _inverted);
			else
				return new InvertibleSet<T>(_set.With(item), _inverted);
		}
		public InvertibleSet<T> Union(InvertibleSet<T> other)
		{
			// if either set is inverted, the result must be inverted.
			// if both sets are inverted, the base sets should be intersected.
			// if only one set is inverted, the other set must be subtracted from it.
			if (_inverted || other._inverted) {
				if (_inverted) {
					if (other._inverted)
						return new InvertibleSet<T>(_set.Intersect(other._set), true);
					else
						return new InvertibleSet<T>(_set.Except(other._set), true);
				} else
					return new InvertibleSet<T>(other._set.Except(_set), true);
			}
			return new InvertibleSet<T>(_set.Union(other._set), false);
		}
		public InvertibleSet<T> Intersect(InvertibleSet<T> other) { return Intersect(other, false); }
		public InvertibleSet<T> Intersect(InvertibleSet<T> other, bool subtractOther)
		{
			bool otherInverted = other._inverted ^ subtractOther;
			// The result is inverted iff both inputs are inverted.
			// if both sets are inverted, the base sets should be unioned.
			// if only one set is inverted, the base set of the inverted one
			// should be subtracted from the set that is not inverted.
			if (_inverted || otherInverted) {
				if (_inverted) {
					if (otherInverted)
						return new InvertibleSet<T>(_set.Union(other._set), true);
					else
						return new InvertibleSet<T>(other._set.Except(_set), false);
				} else
					return new InvertibleSet<T>(_set.Except(other._set), false);
			}
			return new InvertibleSet<T>(_set.Intersect(other._set), false);
		}
		public InvertibleSet<T> Except(InvertibleSet<T> other)
		{
			// Subtraction is equivalent to intersection with the inverted set.
			return Intersect(other, true);
		}
		public InvertibleSet<T> Xor(InvertibleSet<T> other)
		{
			return new InvertibleSet<T>(_set.Xor(other._set), _inverted ^ other._inverted);
		}

		public override int GetHashCode()
		{
			int hc = BaseSet.GetHashCode();
			return IsInverted ? ~hc : hc;
		}
		public override bool Equals(object obj)
		{
			return obj is InvertibleSet<T> && Equals((InvertibleSet<T>)obj);
		}
		bool IEquatable<InvertibleSet<T>>.Equals(InvertibleSet<T> other) { return SetEquals(other); }

		#region ISetImm<T, InvertibleSet<T>>: IsSubsetOf, IsSupersetOf, Overlaps, IsProperSubsetOf, IsProperSupersetOf, SetEquals
		// Remember to keep this code in sync with MSet<T> (the copies can be identical)

		/// <summary>TODO NOT IMPLEMENTED Returns true if all items in this set are present in the other set.</summary>
		public bool IsSubsetOf(InvertibleSet<T> other) 
		{
			throw new NotImplementedException();
		}
		/// <summary>TODO NOT IMPLEMENTED Returns true if all items in the other set are present in this set.</summary>
		public bool IsSupersetOf(InvertibleSet<T> other)
		{
			throw new NotImplementedException();
		}
		/// <summary>TODO NOT IMPLEMENTED Returns true if this set contains at least one item from 'other'.</summary>
		public bool Overlaps(InvertibleSet<T> other)
		{
			throw new NotImplementedException();
		}
		/// <inheritdoc cref="Impl.InternalSet{T}.IsProperSubsetOf(ISet{T}, int)"/>
		public bool IsProperSubsetOf(InvertibleSet<T> other)
		{
			throw new NotImplementedException();
		}
		/// <inheritdoc cref="Impl.InternalSet{T}.IsProperSupersetOf(ISet{T}, IEqualityComparer{T}, int)"/>
		public bool IsProperSupersetOf(InvertibleSet<T> other)
		{
			throw new NotImplementedException();
		}
		public bool SetEquals(InvertibleSet<T> other)
		{
			return _inverted == other._inverted && _set.SetEquals(other._set);
		}

		#endregion

		IEnumerator<T> IEnumerable<T>.GetEnumerator() { return _set.GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return _set.GetEnumerator(); }
		//int ICount.Count { get { throw new NotSupportedException(); } }
		int IReadOnlyCollection<T>.Count { get { return _set.Count; } }

		void ICollectionSource<T>.CopyTo(T[] array, int arrayIndex) { _set.CopyTo(array, arrayIndex); }
	}

	[TestFixture]
	public class InvertibleSetTests : Assert
	{
		[Test]
		public void RegressionTests()
		{
			var a = new InvertibleSet<int>(new[] { 1, 2 }, false);
			var b = new InvertibleSet<int>(new[] { 1, 2, 3 }, true);
			var c = new InvertibleSet<int>(new[] { 3 }, true);
			That(!b.SetEquals(c));
			That(b.Union(a).SetEquals(c));
			That(a.Union(b).SetEquals(c)); // bug fix: this one failed
		}
	}
}
