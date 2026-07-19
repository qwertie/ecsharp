using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Collections
{
	/// <summary>A list wrapper that provides ListChanging and ListChanged events. 
	/// Shorthand for Loyc.Collections.Impl.ListWithChangeEvents{T,IList{T}}.</summary>
	public class ListWithChangeEvents<T> : Loyc.Collections.Impl.ListWithChangeEvents<T, IList<T>>
	{
		public ListWithChangeEvents(IList<T> wrappedObject) : base(wrappedObject) { }
		public ListWithChangeEvents() : base(new List<T>()) { }
	}
}

namespace Loyc.Collections.Impl
{
	/// <summary>A list wrapper that provides ListChanging and ListChanged events. 
	/// You can also implement custom behavior by overriding its methods.</summary>
	/// <seealso cref="ListWrapper{TList,T}"/>
	public class ListWithChangeEvents<T, TList> : ListWrapper<T, TList>,
		IListExWithChangeEvents<T>
		where TList : IList<T>
	{
		public ListWithChangeEvents(TList list) : base(list) { }

		public virtual event ListChangingHandler<T, IListSource<T>>? ListChanging;
		public virtual event ListChangingHandler<T, IListSource<T>>? ListChanged;

		public override T this[int index]
		{
			get => _obj[index];
			set {
				if (!TrySet(index, value))
					CheckParam.ThrowOutOfRange(nameof(index), index, 0, Count-1);
			}
		}

		public override void Add(T item)
		{
			if ((ListChanged ?? ListChanging) == null)
				_obj.Add(item);
			else {
				var info = new ListChangeInfo<T>(NotifyCollectionChangedAction.Add, Count, 1, ListExt.Single(item), EmptyList<T>.Value);
				ListChanging?.Invoke(this, info);
				_obj.Add(item);
				ListChanged?.Invoke(this, info);
			}
		}

		public override void Clear()
		{
			if ((ListChanged ?? ListChanging) == null)
				_obj.Clear();
			else if (!IsEmpty) {
				var oldItems = ListChanged != null ? new DList<T>(_obj) : (IListSource<T>)this;
				var info = new ListChangeInfo<T>(NotifyCollectionChangedAction.Reset, 0, -_obj.Count, EmptyList<T>.Value, oldItems);
				ListChanging?.Invoke(this, info);
				_obj.Clear();
				ListChanged?.Invoke(this, info);
			}
		}
		
		public override bool Remove(T item)
		{
			int index = IndexOf(item);
			if (index >= 0) {
				RemoveAt(index);
				return true;
			}
			return false;
		}
		
		public override void Insert(int index, T item)
		{
			if ((ListChanged ?? ListChanging) == null)
				_obj.Insert(index, item);
			else {
				if ((uint)index > (uint)Count)
					CheckParam.ThrowOutOfRange(nameof(index), index, 0, Count);
				var info = new ListChangeInfo<T>(NotifyCollectionChangedAction.Add, index, 1, ListExt.Single(item), EmptyList<T>.Value);
				ListChanging?.Invoke(this, info);
				_obj.Insert(index, item);
				ListChanged?.Invoke(this, info);
			}
		}

		public override void RemoveAt(int index)
		{
			if ((ListChanged ?? ListChanging) == null)
				_obj.RemoveAt(index);
			else {
				var item = ListExt.Single(_obj[index]); // may throw
				var info = new ListChangeInfo<T>(NotifyCollectionChangedAction.Remove, index, -1, EmptyList<T>.Value, item);
				ListChanging?.Invoke(this, info);
				_obj.RemoveAt(index);
				ListChanged?.Invoke(this, info);
			}
		}

		public virtual void AddRange(IEnumerable<T> list) => InsertRange(Count, list, list.Count());

		public virtual void AddRange(IReadOnlyCollection<T> list) => InsertRange(Count, list, list.Count);

		public virtual void InsertRange(int index, IEnumerable<T> list) => InsertRange(index, list, list.Count());

		public virtual void InsertRange(int index, IReadOnlyCollection<T> list) => InsertRange(index, list, list.Count);

		private void InsertRange(int index, IEnumerable<T> list, int listCount)
		{
			if ((ListChanged ?? ListChanging) == null)
				ListExt.InsertRange(_obj, index, listCount, list);
			else {
				if ((uint)index > (uint)Count)
					CheckParam.ThrowOutOfRange(nameof(index), index, 0, Count);
				if (listCount != 0) {
					var list2 = (list as IListSource<T>) ?? new DList<T>(list);
					var info = new ListChangeInfo<T>(NotifyCollectionChangedAction.Add, index, list2.Count, list2, EmptyList<T>.Value);
					ListChanging?.Invoke(this, info);
					ListExt.InsertRange(_obj, index, list2);
					ListChanged?.Invoke(this, info);
				}
			}
		}

		public void RemoveRange(int index, int amount)
		{
			if ((ListChanged ?? ListChanging) == null)
				ListExt.RemoveRange(_obj, index, amount);	
			else {
				IListSource<T> oldItems = new ListSlice<T>(_obj, index, amount); // ensures index >= 0 and amount >= 0
				if (oldItems.Count != 0) {
					if (ListChanged != null)
						oldItems = new DList<T>(oldItems);
					var info = new ListChangeInfo<T>(NotifyCollectionChangedAction.Remove, index, -oldItems.Count, EmptyList<T>.Value, oldItems);
					ListChanging?.Invoke(this, info);
					ListExt.RemoveRange(_obj, index, oldItems.Count);
					ListChanged?.Invoke(this, info);
				}
			}
		}

		public virtual bool TrySet(int index, T value)
		{
			if ((uint)index < (uint)_obj.Count) {
				if ((ListChanged ?? ListChanging) == null)
					_obj[index] = value;
				else {
					var info = new ListChangeInfo<T>(NotifyCollectionChangedAction.Replace, index, 0,
						ListExt.Single(value), ListExt.Single(_obj[index]));
					ListChanging?.Invoke(this, info);
					_obj[index] = value;
					ListChanged?.Invoke(this, info);
				}
				return true;
			}
			return false;
		}
	}
}
