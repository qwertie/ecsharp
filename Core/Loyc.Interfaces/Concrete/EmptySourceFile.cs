using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Collections;

namespace Loyc.Syntax
{
	/// <summary>
	/// A dummy implementation of <see cref="ISourceFile"/> that has only a 
	/// filename, no source text. Used as the source file of synthetic syntax 
	/// nodes.
	/// </summary>
	public class EmptySourceFile : ISourceFile
	{
		[Obsolete("Please use another instance, such as Synthetic or Unknown")]
		public static readonly EmptySourceFile Default = new EmptySourceFile("");
		public static readonly EmptySourceFile Synthetic = new EmptySourceFile("<Synthetic Code>".Localized());
		public static readonly EmptySourceFile Unknown = new EmptySourceFile("<Unknown Location>".Localized());

		public EmptySourceFile(string fileName)
		{
			_fileName = fileName;
		}
		
		private string _fileName;
		public string FileName
		{
			get { return _fileName; }
		}

		public ICharSource Text
		{
			get { return UString.Null; }
		}

		ILineColumnFile IIndexToLine.IndexToLine(int index) => IndexToLine(index);
		public LineColumnFile IndexToLine(int index)
		{
			return LineColumnFile.Nowhere;
		}
		public int LineToIndex(int lineNo)
		{
			return -1;
		}
		public int LineToIndex(ILineAndColumn pos)
		{
			return -1;
		}
	}
}
