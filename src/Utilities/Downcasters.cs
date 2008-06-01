using System;
using System.Collections.Generic;

namespace Loyc.Utilities
{
	public class EnumerableTDowncaster<FromT, ToT> : IEnumerable<ToT>
		where ToT : FromT
	{
		protected IEnumerable<FromT> _source;
		public EnumerableTDowncaster(IEnumerable<FromT> source) { _source = source; }

		public IEnumerator<ToT> GetEnumerator()
		{
			foreach (FromT el in _source)
				yield return (ToT)el;
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	public class CollectionTDowncaster<FromT, ToT> : ICollection<ToT>
		where ToT : FromT
	{
		protected ICollection<FromT> _source;
		public CollectionTDowncaster(ICollection<FromT> source) { _source = source; }

		#region ICollection<ToT> Members

		public void Add(ToT item)
		{
			_source.Add((FromT)item);
		}
		public void Clear()
		{
			_source.Clear();
		}
		public bool Contains(ToT item)
		{
			return _source.Contains((FromT)item);
		}
		public void CopyTo(ToT[] array, int arrayIndex)
		{
			// Why doesn't this work?
			//_source.CopyTo((FromT[])array, arrayIndex);
			int i = arrayIndex;
			foreach (FromT el in _source)
				array[i++] = (ToT)el;
		}
		public int Count
		{
			get { return _source.Count; }
		}
		public bool IsReadOnly
		{
			get { return _source.IsReadOnly; }
		}
		public bool Remove(ToT item)
		{
			return _source.Remove((FromT)item);
		}

		#endregion

		#region IEnumerable Members

		public IEnumerator<ToT> GetEnumerator()
		{
			foreach (FromT el in _source)
				yield return (ToT)el;
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}

	public class ListTDowncaster<FromT, ToT> : IList<ToT>
		where ToT : FromT
	{
		protected IList<FromT> _source;
		public ListTDowncaster(IList<FromT> source) { _source = source; }

		#region IList<ToT> Members

		public int IndexOf(ToT item)
		{
			return _source.IndexOf((FromT)item);
		}
		public void Insert(int index, ToT item)
		{
			_source.Insert(index, (FromT)item);
		}
		public void RemoveAt(int index)
		{
			_source.RemoveAt(index);
		}
		public ToT this[int index]
		{
			get { return (ToT)_source[index]; }
			set { _source[index] = (FromT)value; }
		}

		#endregion

		#region ICollection<ToT> Members

		public void Add(ToT item)
		{
			_source.Add((FromT)item);
		}
		public void Clear()
		{
			_source.Clear();
		}
		public bool Contains(ToT item)
		{
			return _source.Contains((FromT)item);
		}
		public void CopyTo(ToT[] array, int arrayIndex)
		{
			// Why doesn't this work?
			//_source.CopyTo((FromT[])array, arrayIndex);
			int i = arrayIndex;
			foreach (FromT el in _source)
				array[i++] = (ToT)el;
		}
		public int Count
		{
			get { return _source.Count; }
		}
		public bool IsReadOnly
		{
			get { return _source.IsReadOnly; }
		}
		public bool Remove(ToT item)
		{
			return _source.Remove((FromT)item);
		}

		#endregion

		#region IEnumerable Members

		public IEnumerator<ToT> GetEnumerator()
		{
			foreach (FromT el in _source)
				yield return (ToT)el;
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
