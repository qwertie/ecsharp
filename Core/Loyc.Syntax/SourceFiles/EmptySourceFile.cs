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
		public static readonly EmptySourceFile Default = new EmptySourceFile("");
		public static readonly EmptySourceFile Unknown = new EmptySourceFile("Unknown");

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
			get { return StringSlice.Empty; }
		}

		public SourcePos IndexToLine(int index)
		{
			return SourcePos.Nowhere;
		}
		public int LineToIndex(int lineNo)
		{
			return -1;
		}
		public int LineToIndex(LineAndCol pos)
		{
			return -1;
		}
	}
}
