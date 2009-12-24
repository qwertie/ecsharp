using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.CompilerCore
{
	public class SimpleSliceSource<T> : ISimpleSource<T>
	{
		public SimpleSliceSource(ISimpleSource<T> inner, int start, int length)
		{
			_inner = inner;
			_start = start;
			_length = length;
		}

		protected ISimpleSource<T> _inner;
		protected int _start, _length;
		
		static T Default = default(T);
		static SimpleSliceSource()
		{
			Default = default(T);
			if (typeof(T) == typeof(char))
				Default = (T)(object)(char)0xFFFF;
		}

		public T this[int index]
		{
			get {
				if ((uint)index < (uint)_length)
					return _inner[_start + index];
				else
					return Default;
			}
		}
		public int Count
		{
			get { return _length; }
		}
		public IEnumerator<T> GetEnumerator()
		{
 			for (int i = 0; i < _length; i++)
				yield return _inner[_start + i];
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
 			return GetEnumerator();
		}
		public int SliceStart { get { return _start; } }
		public ISimpleSource<T> OriginalSource { get { return _inner; } }
	}
	public class SliceSource<T> : SimpleSliceSource<T>, ISimpleSource2<T>
	{
		public SliceSource(ISimpleSource2<T> inner, int start, int length)
			: base(inner, start, length) { }
		
		public SourcePos IndexToLine(int index)
		{
 			return ((ISimpleSource2<T>)_inner).IndexToLine(_start + index);
		}
		public new ISimpleSource2<T> OriginalSource 
		{
			get { return (ISimpleSource2<T>)_inner; }
		}
	}
	public class SliceCharSource : SliceSource<char>, ICharSource
	{
		public SliceCharSource(ICharSource inner, int start, int length)
			: base(inner, start, length) { _inner = inner; }

		new ICharSource _inner;

		public new SourcePos IndexToLine(int index)
		{
 			return _inner.IndexToLine(_start + index);
		}
		public int LineToIndex(int lineNo)
		{
			return _inner.LineToIndex(lineNo);
		}
		public int LineToIndex(SourcePos pos)
		{
			return _inner.LineToIndex(pos);
		}
		public string Substring(int startIndex, int length)
		{
			if (startIndex < _start) {
				length -= _start - startIndex;
				startIndex = _start;
			}
			return _inner.Substring(_start + startIndex,
						    Math.Min(_length - startIndex, length));
		}
		public new ICharSource OriginalSource
		{
			get { return _inner; }
		}
	}
	public class SliceCharSourceFile : SliceCharSource, ISourceFile
	{
		public SliceCharSourceFile(ISourceFile inner, int start, int length)
			: base(inner, start, length) { _inner = inner; }

		public string FileName 
		{
			get { return ((ISourceFile)_inner).FileName; }
		}
		public string Language
		{
			get { return ((ISourceFile)_inner).Language; }
		}
		public new ISourceFile OriginalSource
		{
			get { return (ISourceFile)_inner; }
		}
	}

}
