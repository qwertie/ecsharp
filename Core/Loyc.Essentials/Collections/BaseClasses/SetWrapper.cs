using System.Collections;
using System.Collections.Generic;

namespace Loyc.Collections.Impl
{
	///	<summary>A simple base class that helps you use the decorator pattern on a set.
	///	By default, all it does is forward every method to the underlying collection
	///	(including GetHashCode, Equals and ToString). You can change its behavior by
	///	overriding methods.</summary>
	///	<remarks>This could be used, for example, to help you implement a collection
	///	that needs to take some kind of action whenever the collection is modified.
	/// </remarks>
	/// <seealso cref="CollectionWithChangeEvents{T, TColl}"/>
	public abstract class SetWrapper<T, TSet> : WrapperBase<TSet>, ISet<T>, ICollection<T> where TSet : ISet<T>
	{
		public SetWrapper(TSet wrappedObject) : base(wrappedObject) { }

		void ICollection<T>.Add(T item) => Add(item);
		public virtual bool Add(T item) => _obj.Add(item);
		public virtual void Clear() => _obj.Clear();
		public virtual bool Remove(T item) => _obj.Remove(item);
		public virtual void ExceptWith(IEnumerable<T> other) => _obj.ExceptWith(other);
		public virtual void IntersectWith(IEnumerable<T> other) => _obj.IntersectWith(other);
		public virtual void SymmetricExceptWith(IEnumerable<T> other) => _obj.SymmetricExceptWith(other);
		public virtual void UnionWith(IEnumerable<T> other) => _obj.UnionWith(other);

		public virtual bool IsProperSubsetOf(IEnumerable<T> other) => _obj.IsProperSubsetOf(other);
		public virtual bool IsProperSupersetOf(IEnumerable<T> other) => _obj.IsProperSupersetOf(other);
		public virtual bool IsSubsetOf(IEnumerable<T> other) => _obj.IsSubsetOf(other);
		public virtual bool IsSupersetOf(IEnumerable<T> other) => _obj.IsSupersetOf(other);
		public virtual bool Overlaps(IEnumerable<T> other) => _obj.Overlaps(other);
		public virtual bool SetEquals(IEnumerable<T> other) => _obj.SetEquals(other);

		public virtual int Count => _obj.Count;
		public virtual bool IsReadOnly => _obj.IsReadOnly;
		public virtual bool Contains(T item) => _obj.Contains(item);
		public virtual void CopyTo(T[] array, int arrayIndex) => _obj.CopyTo(array, arrayIndex);
		public virtual IEnumerator<T> GetEnumerator() => _obj.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}