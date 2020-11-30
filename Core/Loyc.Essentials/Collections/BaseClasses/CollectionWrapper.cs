using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Collections.Impl
{
	///	<summary>A simple base class that to helps you implement a "smart" collection.
	///	By default, all it does is forward every method to the underlying collection
	///	(including GetHashCode, Equals and ToString). You can change its behavior by
	///	overriding methods.</summary>
	///	<remarks>This could be used, for example, to help you implement a collection
	///	that needs to take some kind of action whenever the collection is modified.
	/// </remarks>
	///	<seealso cref="ListWrapper{TList,T}"/>
	public class CollectionWrapper<T, TCollection> : WrapperBase<TCollection>, ICollection<T> where TCollection : ICollection<T>
	{
		public CollectionWrapper(TCollection collection) : base(collection) { }

		protected TCollection Collection => _obj;

		public virtual void Add(T item) => _obj.Add(item);
		public virtual void Clear() => _obj.Clear();
		public virtual bool Remove(T item) => _obj.Remove(item);

		public virtual int Count => _obj.Count;
		public virtual bool IsReadOnly => _obj.IsReadOnly;
		public virtual bool Contains(T item) => _obj.Contains(item);
		public virtual void CopyTo(T[] array, int arrayIndex) => _obj.CopyTo(array, arrayIndex);
		public virtual IEnumerator<T> GetEnumerator() => _obj.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
