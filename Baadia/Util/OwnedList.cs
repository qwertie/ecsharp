using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using System.Collections.Specialized;
using Loyc.Collections.Impl;
using Loyc;

namespace Util.Collections
{
	/// <summary>Interface for an "owner" object that can be notified when a list 
	/// is changing; used with <see cref="OwnedList{T}"/> and <see cref="OwnedChildList{Parent,T}"/>.</summary>
	/// <seealso cref="ListChangingHandler{T}"/>
	public interface IListChanging<T>
	{
		void OnListChanging(IListSource<T> sender, ListChangeInfo<T> e);
	}

	/// <summary>Interface that allows a "child" object to be notified when it is 
	/// added or removed from a "parent" object.</summary>
	/// <remarks>Methods of this interface should only be called by the parent 
	/// object (or <see cref="ChildList{P,T}"/> or <see cref="OwnedChildList{P, T}"/>)
	/// so use an explicit interface implementation if possible.</remarks>
	public interface IChildOf<in TParent>
	{
		void OnBeingAdded(TParent parent);
		void OnBeingRemoved(TParent parent);
	}

	/// <summary>Interface with <see cref="Parent"/> property that returns the "parent" of an object.</summary>
	public interface IHasParent<out TParent>
	{
		TParent Parent { get; }
	}

	/// <summary>A variation of <see cref="IChildOf{P}"/> that has a <see cref="Parent"/> property.</summary>
	/// <seealso cref="ChildOfOneParent{Parent}"/>
	public interface IChildOfOneParent<TParent> : IChildOf<TParent>, IHasParent<TParent> {}

	/// <summary>An implementation of IChildOf that allows only one parent and 
	/// exposes it in the <see cref="Parent"/> property.</summary>
	public class ChildOfOneParent<TParent> : IChildOfOneParent<TParent> where TParent : class
	{
		protected TParent _parent;
		public TParent Parent { get { return _parent; } }
		public virtual void OnBeingAdded(TParent parent)
		{
			if (_parent != null)
				throw new InvalidStateException("OnBeingAdded: this object already has a parent");
			_parent = parent;
		}
		public virtual void OnBeingRemoved(TParent parent)
		{
			if (parent == _parent)
				_parent = null;
			else // don't throw because it prevents the non-parent from dissociating itself from this object
				MessageSink.Current.Write(Severity.Error, this, "OnBeingRemoved: specified object is not the parent of this object");
		}
	}

	/// <summary>A list that notifies its "owner" when items are added or removed.</summary>
	/// <remarks>This class is more lightweight than a ObservableCollection 
	/// because no delegates are allocated and at most one heap allocation is 
	/// needed for each notification sent to the owner (ObservableCollection 
	/// makes at least three allocations per notification).</remarks>
	public class OwnedList<T> : ListExBase<T> 
	{
		public OwnedList(IListChanging<T> owner) { _owner = owner; }

		protected IListChanging<T> _owner;
		protected InternalDList<T> _list = InternalDList<T>.Empty;

		public override T TryGet(int index, out bool fail)
		{
			return _list.TryGet(index, out fail);
		}
		public override bool TrySet(int index, T value)
		{
			if ((uint)index < (uint)_list.Count) {
				if (_owner != null) {
					var chgList = Range.Single(value);
					_owner.OnListChanging(this, new ListChangeInfo<T>(NotifyCollectionChangedAction.Replace, index, 0, chgList));
				}
				return _list.TrySet(index, value);
			}
			return false;
		}
		public override void Insert(int index, T item)
		{
			if ((uint)index > (uint)_list.Count)
				throw new ArgumentOutOfRangeException("index");
			if (_owner != null) {
				var chgList = Range.Single(item);
				_owner.OnListChanging(this, new ListChangeInfo<T>(NotifyCollectionChangedAction.Add, index, 1, chgList));
			}
			_list.Insert(index, item);
		}
		public override void Clear()
		{
			if (Count != 0) {
				if (_owner != null) {
					var empty = EmptyList<T>.Value;
					_owner.OnListChanging(this, new ListChangeInfo<T>(NotifyCollectionChangedAction.Remove, 0, Count, empty));
				}
				_list.Clear();
			}
		}
		public override void RemoveAt(int index)
		{
			if ((uint)index >= (uint)_list.Count)
				throw new ArgumentOutOfRangeException("index");
			if (_owner != null) {
				var empty = EmptyList<T>.Value;
				_owner.OnListChanging(this, new ListChangeInfo<T>(NotifyCollectionChangedAction.Remove, index, -1, empty));
			}
			_list.RemoveAt(index);
		}
		public override int Count
		{
			get { return _list.Count; }
		}
	}

	/// <summary>A list class that notifies "child" objects when they are being 
	/// associated with or removed from a "parent" object.</summary>
	/// <remarks>This class is derived from <see cref="OwnedList{T}"/> but that
	/// should be considered an implementation detail.</remarks>
	public class ChildList<Parent, T> : OwnedList<T>, IListChanging<T> 
		where T : IChildOf<Parent>
	{
		public ChildList(Parent parent) 
			: base(null) // C# won't let us call base(this)
		{
			base._owner = this;
			_parent = parent;
		}
		protected Parent _parent;

		public void OnListChanging(IListSource<T> sender, ListChangeInfo<T> e)
		{
			if (_parent is IListChanging<T>)
				(_parent as IListChanging<T>).OnListChanging(sender, e);
			int addCount = e.NewItems.Count;
			int removeCount = addCount - e.SizeChange;
			for (int i = e.Index; i < e.Index + removeCount; i++) {
				try {
					_list[i].OnBeingRemoved(_parent); // could throw
				} catch {
					// Notify earlier items that changes are being "undone"
					for (int j = e.Index; j < i; j++)
						try { _list[i].OnBeingAdded(_parent); } catch { }
					throw;
				}
			}
			for (int i = 0; i < addCount; i++) {
				try {
					e.NewItems[i].OnBeingAdded(_parent); // could throw
				} catch {
					// Notify children that changes are being "undone"
					for (int j = 0; j < i; j++)
						try { e.NewItems[j].OnBeingRemoved(_parent); } catch { }
					for (int j = e.Index; j < e.Index + removeCount; j++)
						try { _list[i].OnBeingAdded(_parent); } catch { }
					throw;
				}
			}
		}
	}

	/// <summary>A list class that notifies "child" objects when they are being 
	/// associated with or unassociated with a "parent" object, and then 
	/// notifies the parent object afterward (if none of the children stopped 
	/// the change by throwing an exception).</summary>
	public class OwnedChildList<Parent, T> : ChildList<Parent, T>, IListChanging<T>
		where Parent : IListChanging<T>
		where T : IChildOf<Parent>
	{
		public OwnedChildList(Parent parent) : base(parent) { }
		public new void OnListChanging(IListSource<T> sender, ListChangeInfo<T> e)
		{
			base.OnListChanging(sender, e);
			_parent.OnListChanging(sender, e);
		}
	}
}
