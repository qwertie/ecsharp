using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Loyc.Runtime
{
	/// <summary>
	/// This helper class gives a nice view of a custom collection within the 
	/// debugger.
	/// </summary>
	/// <remarks>
	/// Use the following custom attributes on your class that implements 
	/// ICollection(of T) or IList(of T):
	/// <code>
	/// [DebuggerTypeProxy(typeof(CollectionDebugView<>)), DebuggerDisplay("Count = {Count}")]
	/// </code>
	/// See the following link for more information:
	/// http://www.codeproject.com/KB/dotnet/DebugIList.aspx
	/// </remarks>
	public class CollectionDebugView<T>
	{
		private ICollection<T> collection;

		public CollectionDebugView(ICollection<T> collection)
		{
			if (collection == null)
				throw new ArgumentNullException("collection");
			this.collection = collection;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items
		{
			get {
				T[] array = new T[this.collection.Count];
				this.collection.CopyTo(array, 0);
				return array;
			}
		}
	}
}
