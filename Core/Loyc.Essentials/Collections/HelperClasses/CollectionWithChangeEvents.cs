using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Collections
{
	/// <summary>A collection wrapper that provides ListChanging and ListChanged events.
	/// Shorthand for Loyc.Collections.Impl.CollectionWithChangeEvents{T,ICollection{T}}.</summary>
	public class CollectionWithChangeEvents<T> : Loyc.Collections.Impl.CollectionWithChangeEvents<T, ICollection<T>>
	{
		public CollectionWithChangeEvents(ICollection<T> obj) : base(obj) { }
	}
}

namespace Loyc.Collections.Impl
{
	/// <summary>A collection wrapper that provides ListChanging and ListChanged events. 
	/// You can also implement custom behavior by overriding its methods.</summary>
	/// <remarks>
	/// This class is designed to support both sets and other collection types.
	/// If the underlying collection implements <see cref="ISet{T}"/>, the Add method
	/// avoids firing change notification events if the requested item is already in the
	/// set. Regardless of whether the collection is a set, the Remove method also avoids 
	/// firing any notifications when the requested item is not in the collection.
	/// </remarks>
	/// <typeparam name="T">Type of each list item</typeparam>
	/// <typeparam name="TColl">Type of the underlying collection</typeparam>
	public class CollectionWithChangeEvents<T, TColl> : CollectionWrapper<T, TColl>, ICollectionWithChangeEvents<T> where TColl : ICollection<T>
	{
		public CollectionWithChangeEvents(TColl wrappedObject) : base(wrappedObject) 
			=> _asSet = wrappedObject as ISet<T>;

		public virtual event ListChangingHandler<T, ICollection<T>> ListChanging;
		public virtual event ListChangingHandler<T, ICollection<T>> ListChanged;
		public ISet<T> _asSet;

		public bool IsEmpty => Count == 0;

		void ICollection<T>.Add(T item) => Add(item);
		public override void Add(T item) => TryAdd(item);

		/// <summary>Synonym for Add(). If the collection implements ISet{T}, this method
		/// returns false if the item is already present in the set; otherwise it always 
		/// returns true.</summary>
		/// <returns>True if the item was added, false if the collection is a set and the 
		/// item was already in the set.</returns>
		public virtual bool TryAdd(T item)
		{
			if ((ListChanged ?? ListChanging) == null) {
				if (_asSet != null)
					return _asSet.Add(item);
				_obj.Add(item);
				return true;
			} else {
				if (_asSet != null && _asSet.Contains(item))
					return false;
				var info = new ListChangeInfo<T>(NotifyCollectionChangedAction.Add, int.MinValue, 1, ListExt.Single(item), EmptyList<T>.Value);
				ListChanging?.Invoke(this, info);
				_obj.Add(item);
				ListChanged?.Invoke(this, info);
				return true;
			}
		}

		public override void Clear()
		{
			if ((ListChanged ?? ListChanging) == null)
				_obj.Clear();
			else if (!IsEmpty) {
				var oldItems = ListChanged != null ? new DList<T>(_obj) : (_obj as IReadOnlyList<T>).AsListSource();
				var info = new ListChangeInfo<T>(NotifyCollectionChangedAction.Reset, 0, -_obj.Count, EmptyList<T>.Value, oldItems);
				ListChanging?.Invoke(this, info);
				_obj.Clear();
				ListChanged?.Invoke(this, info);
			}
		}

		public override bool Remove(T item)
		{
			if ((ListChanged ?? ListChanging) == null)
				return _obj.Remove(item);
			else {
				if (ListChanging != null)
					if (!_obj.Contains(item))
						return false;
				var info = new ListChangeInfo<T>(NotifyCollectionChangedAction.Remove, int.MinValue, -1, EmptyList<T>.Value, ListExt.Single(item));
				ListChanging?.Invoke(this, info);
				bool result = _obj.Remove(item);
				if (result)
					ListChanged?.Invoke(this, info);
				return result;
			}
		}
	}
}
