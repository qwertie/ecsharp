// This file is part of the Loyc project. Licence: LGPL
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace String { }

namespace Loyc.Collections
{
	/// <summary>
	/// This helper class gives a nice view of a custom collection within the 
	/// debugger.
	/// </summary>
	/// <remarks>
	/// For ISource or IListSource collections, use ListSourceDebugView instead.
	/// <para/>
	/// Use the following custom attributes on your class that implements 
	/// ICollection(of T) or IList(of T):
	/// <code>
	/// [DebuggerTypeProxy(typeof(CollectionDebugView&lt;>)), DebuggerDisplay("Count = {Count}")]
	/// </code>
	/// See the following link for more information:
	/// http://www.codeproject.com/Articles/28405/Make-the-debugger-show-the-contents-of-your-custom
	/// </remarks>
	public class CollectionDebugView<T>
	{
		private ICollection<T> _collection;

		public CollectionDebugView(ICollection<T> collection)
		{
			if (collection == null)
				throw new ArgumentNullException("collection");
			this._collection = collection;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items
		{
			get {
				T[] array = new T[_collection.Count];
				_collection.CopyTo(array, 0);
				return array;
			}
		}
	}
	
	/// <summary>Workaround for a limitation of the debugger: it doesn't support
	/// <see cref="CollectionDebugView{T}"/> when T is <see cref="KeyValuePair{K,V}"/>.
	/// This class is identical, except that T is replaced with KeyValuePair{K,V}.
	/// </summary>
	public class DictionaryDebugView<K, V>
	{
		private ICollection<KeyValuePair<K,V>> _collection;

		public DictionaryDebugView(ICollection<KeyValuePair<K, V>> collection)
		{
			if (collection == null)
				throw new ArgumentNullException("collection");
			this._collection = collection;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public KeyValuePair<K,V>[] Items
		{
			get {
				KeyValuePair<K,V>[] array = new KeyValuePair<K,V>[_collection.Count];
				_collection.CopyTo(array, 0);
				return array;
			}
		}
	}

	/// <summary>
	/// This helper class gives a nice view of a custom collection within the 
	/// debugger.
	/// </summary>
	/// <remarks>
	/// Use the following custom attributes on your class that implements 
	/// IListSource(of T) or ISource(of T):
	/// <code>
	/// [DebuggerTypeProxy(typeof(ListSourceDebugView&lt;>)), DebuggerDisplay("Count = {Count}")]
	/// </code>
	/// See the following link for more information:
	/// http://www.codeproject.com/KB/dotnet/DebugIList.aspx
	/// </remarks>
	public class ListSourceDebugView<T>
	{
		private IReadOnlyCollection<T> _collection;

		public ListSourceDebugView(IReadOnlyCollection<T> collection)
		{
			if (collection == null)
				throw new ArgumentNullException("collection");
			this._collection = collection;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items
		{
			get {
				T[] array = new T[_collection.Count];
				_collection.CopyTo(array, 0);
				return array;
			}
		}
	}
}
