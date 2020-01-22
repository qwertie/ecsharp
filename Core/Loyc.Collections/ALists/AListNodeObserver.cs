using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Collections.Impl;

namespace Loyc.Collections
{
	/// <summary>Manages a collection of <see cref="IAListTreeObserver{K, T}"/>.</summary>
	/// <remarks>
	/// 
	/// TODO:
	/// - Check that all cloning is handled.
	/// - Check that all splitting is handled.
	/// - Check that all TakeFromLeft/Right is handled.
	/// - Check that all leaf changes are handled
	/// - Factor out certain calls to indexer to shorten/simplify code
	/// - Add a method to install indexer in AList
	/// - Write unit tests
	/// - Write benchmarks
	/// - Try optimizations: _isFrozen in AListNode, _children[0].Index always 0, 
	///                      redundant-if optimization on calls to AutoClone, 
	///                      larger inner nodes
	/// </remarks>
	public abstract partial class AListBase<K, T> : IListSource<T>, INotifyListChanging<T>
	{
		// Class diagram editor only sees this half of the partial class
		//protected internal ListChangingHandler<T> _listChanging; // Delegate for ListChanging
		//protected internal AListNode<K, T> _root;
		//protected internal IAListTreeObserver<K, T> _observer;
		//protected uint _count;
		//protected ushort _version;
		//protected ushort _maxLeafSize;
		//protected byte _maxInnerSize;
		//protected byte _treeHeight;
		//protected byte _freezeMode = NotFrozen;
	
		/// <summary>A multiplexer that is only created for ALists that have two or more attached intances of 
		/// <see cref="IAListTreeObserver{K,T}"/>.</summary>
		protected class ObserverMgr : IAListTreeObserver<K, T>
		{
			public ObserverMgr(AListBase<K, T> list, AListNode<K,T> root, IAListTreeObserver<K, T> existingObserver)
			{
				_list = list;
				_root = root;
				if (existingObserver != null)
					_observers.Add(existingObserver);
			}

			protected AListBase<K, T> _list;
			protected AListNode<K, T> _root;
			protected InternalList<IAListTreeObserver<K, T>> _observers = InternalList<IAListTreeObserver<K, T>>.Empty;

			#region IAListTreeObserver members

			public bool? Attach(AListBase<K, T> list)
			{
				throw new InvalidOperationException(); // should not be called
			}
			public void Detach(AListBase<K, T> list, AListNode<K, T> root)
			{
				throw new NotImplementedException(); // should not be called
			}

			public void RootChanged(AListBase<K, T> list, AListNode<K, T> newRoot, bool clear)
			{
				Debug.Assert(_list == list);
				_root = newRoot;
				try {
					for (int i = 0; i < _observers.Count; i++)
						_observers[i].RootChanged(list, newRoot, clear);
				} catch(Exception e) { IllegalException(e); }
			}

			public void ItemAdded(T item, AListLeafBase<K, T> parent)
			{
				try {
					for (int i = 0; i < _observers.Count; i++)
						_observers[i].ItemAdded(item, parent);
				} catch(Exception e) { IllegalException(e); }
			}

			public void ItemRemoved(T item, AListLeafBase<K, T> parent)
			{
				try {
					for (int i = 0; i < _observers.Count; i++)
						_observers[i].ItemRemoved(item, parent);
				} catch(Exception e) { IllegalException(e); }
			}

			public void NodeAdded(AListNode<K, T> child, AListInnerBase<K, T> parent)
			{
				try {
					for (int i = 0; i < _observers.Count; i++)
						_observers[i].NodeAdded(child, parent);
				} catch(Exception e) { IllegalException(e); }
			}

			public void NodeRemoved(AListNode<K, T> child, AListInnerBase<K, T> parent)
			{
				try {
					for (int i = 0; i < _observers.Count; i++)
						_observers[i].NodeRemoved(child, parent);
				} catch(Exception e) { IllegalException(e); }
			}

			public void RemoveAll(AListNode<K, T> node)
			{
				try {
					for (int i = 0; i < _observers.Count; i++)
						_observers[i].RemoveAll(node);
				} catch(Exception e) { IllegalException(e); }
			}

			public void AddAll(AListNode<K, T> node)
			{
				try {
					for (int i = 0; i < _observers.Count; i++)
						_observers[i].AddAll(node);
				} catch(Exception e) { IllegalException(e); }
			}
		
			/// <summary>Called when an observer throws something and the exception is
			/// being swallowed (because IAListTreeObserver is not allowed to throw).</summary>
			/// <param name="e">An exception that was caught.</param>
			protected virtual void IllegalException(Exception e)
			{
			}

			public void CheckPoint()
			{
				for (int i = 0; i < _observers.Count; i++)
					_observers[i].CheckPoint();
			}

			#endregion

			#region Observer management: AddObserver, RemoveObserver, ObserverCount

			internal bool AddObserver(IAListTreeObserver<K,T> observer)
			{
				for (int i = 0; i < _observers.Count; i++)
					if (observer == _observers[i])
						return false;
			
				observer.DoAttach(_list, _root);
				_observers.Add(observer);
				return true;
			}

			internal bool RemoveObserver(IAListTreeObserver<K,T> observer)
			{
 				int i = _observers.IndexOf(observer);
				if (i <= -1)
					return false;
				_observers[i].Detach(_list, _root);
				_observers.RemoveAt(i);
				return true;
			}

			public int ObserverCount { get { return _observers.Count; } }

			#endregion
		}
	}
}
