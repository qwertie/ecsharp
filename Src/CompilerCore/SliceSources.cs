namespace Loyc.CompilerCore
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Loyc.Collections;
	using Loyc.Essentials;

	public class SlicePSource<T> : ListSourceSlice<T>, IParserSource<T>
	{
		public SlicePSource(IParserSource<T> inner, int start, int length)
			: base(inner, start, length) { }
		
		public SourcePos IndexToLine(int index)
		{
 			return ((IParserSource<T>)_obj).IndexToLine(_start + index);
		}
		public new IParserSource<T> OriginalList 
		{
			get { return (IParserSource<T>)_obj; }
		}
	}
	public class SliceCharSource : SlicePSource<char>, ICharSource
	{
		public SliceCharSource(ICharSource inner, int start, int length)
			: base(inner, start, length) { _inner = inner; }

		ICharSource _inner;

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
		public new ICharSource OriginalList
		{
			get { return _inner; }
		}
	}
	public class SliceCharSourceFile : SliceCharSource, ISourceFile
	{
		public SliceCharSourceFile(ISourceFile inner, int start, int length)
			: base(inner, start, length) { _obj = inner; }

		public string FileName 
		{
			get { return ((ISourceFile)_obj).FileName; }
		}
		public string Language
		{
			get { return ((ISourceFile)_obj).Language; }
		}
		public new ISourceFile OriginalList
		{
			get { return (ISourceFile)_obj; }
		}
	}

}
