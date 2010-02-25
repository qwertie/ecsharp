using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.CompilerCore
{
	/// <summary>
	/// Transforms an enumerator into a List(of T) as its indexer is called. Only
	/// enough items are enumerated to fulfill a request, but if Count is 
	/// called then the entire list must be read all at once, so this property
	/// should usually not be called (this is a general rule of using 
	/// ISimpleSource, since other implementations may behave the same way.)
	/// </summary><remarks>
	/// This source exposes all tokens from the enumerator including ITokens
	/// where VisibleToParser is false. To filter out these hidden tokens, pass
	/// the token stream through <see cref="VisibleTokenFilter{Tok}"/>.
	/// </remarks>
	public class EnumerableSource<T> : ISimpleSource2<T>, IList<T>
		where T : class, ITokenValueAndPos
	{
		public EnumerableSource(IEnumerable<T> eSrc) : this(eSrc.GetEnumerator()) { }
		public EnumerableSource(IEnumerator<T> eSrc) { _eSrc = eSrc; }

		IEnumerator<T> _eSrc;
		List<T> _list = new List<T>();

		#region ISimpleSource<T> Members

		public T this[int index]
		{
			get {
				if (AutoQueueUp(index))
					return _list[index];
				else
					return default(T);
			}
			set {
				AutoQueueUp(index);
				_list[index] = value;
			}
		}

		protected bool AutoQueueUp(int index)
		{
			while (index >= _list.Count) {
				if (_eSrc == null)
					return false;
				if (_eSrc.MoveNext())
					_list.Add(_eSrc.Current);
				else {
					_eSrc.Dispose();
					_eSrc = null; // Discard exhausted enumerator
				}
			}
			return true;
		}
		public int Count
		{
			get {
				AutoLoadAll();
				return _list.Count;
			}
		}

		protected void AutoLoadAll()
		{
			if (_eSrc != null) {
				while (_eSrc.MoveNext())
					_list.Add(_eSrc.Current);
				_eSrc.Dispose();
				_eSrc = null;
			}
		}

		public SourcePos IndexToLine(int index)
		{
			ITokenValueAndPos t = this[index];
			if (t == null)
				return null;
			else
				return t.Position;
		}
		#endregion

		#region IEnumerable<T> Members
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		public IEnumerator<T> GetEnumerator()
		{
			T t;
			for (int i = 0; (t = this[i]) != null; i++)
				yield return t;
		}
		#endregion

		#region IList<T> Members

		public int IndexOf(T item)
		{
			AutoLoadAll();
			return _list.IndexOf(item);
		}

		public void Insert(int index, T item)
		{
			AutoQueueUp(index - 1);
			_list.Insert(index, item);
		}

		public void RemoveAt(int index)
		{
			AutoQueueUp(index);
			_list.RemoveAt(index);
		}

		#endregion

		#region ICollection<T> Members

		public void Add(T item)
		{
			AutoLoadAll();
			_list.Add(item);
		}

		public void Clear()
		{
			_eSrc.Dispose();
			_eSrc = null;
			_list.Clear();
		}

		public bool Contains(T item)
		{
			AutoLoadAll();
			return _list.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			AutoLoadAll();
			_list.CopyTo(array, arrayIndex);
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T item)
		{
			AutoLoadAll();
			return _list.Remove(item);
		}

		#endregion
	}
}
