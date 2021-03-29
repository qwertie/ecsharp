using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Loyc.Collections;

namespace Loyc.Collections.Impl
{
	///	<summary>A simple base class that to helps you implement a "smart" collection.
	///	By default, all it does is forward every method to the underlying collection
	///	(including GetHashCode, Equals and ToString). You can change its behavior by
	///	overriding methods.</summary>
	///	<remarks>This could be used, for example, to help you implement a collection
	///	that needs to take some kind of action whenever the collection is modified.
	/// </remarks>
	public abstract class ListWrapper<T, TList> : CollectionWrapper<T, TList>, IListAndListSource<T> where TList : IList<T>
	{
		public ListWrapper(TList wrappedObject) : base(wrappedObject) { }

		public virtual T this[int index]
		{
			get => _obj[index];
			set => _obj[index] = value;
		}

		public virtual void Insert(int index, T item) => _obj.Insert(index, item);
		public virtual void RemoveAt(int index) => _obj.RemoveAt(index);

		public virtual int IndexOf(T item) => _obj.IndexOf(item);
		public virtual IListSource<T> Slice(int start, int count = int.MaxValue) => new ListSlice<T>(_obj, start, count);
		[return: MaybeNull] // There's no attribute like [return: MaybeNullIf("fail")]
		public virtual T TryGet(int index, out bool fail) => (fail = (uint)index >= (uint)_obj.Count) ? default(T) : _obj[index];
	}
}
