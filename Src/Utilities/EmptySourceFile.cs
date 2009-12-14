using System;
using System.Collections.Generic;
using System.Text;
using Loyc.CompilerCore;
using Loyc.Runtime;

namespace Loyc.Utilities
{
	public class EmptySourceFile : ISourceFile
	{
		public static readonly EmptySourceFile Default = new EmptySourceFile("", null);

		private string _fileName;
		private string _lang;

		public EmptySourceFile(string fileName, string lang)
		{
			_fileName = fileName;
			_lang = lang;
		}
		public string FileName
		{
			get { return _fileName; }
		}
		public string Language
		{
			get { return _lang; }
		}
		public string Substring(int startIndex, int length)
		{
			return "";
		}
		public char this[int index]
		{
			get { return (char)0xFFFF; }
		}
		public int Count
		{
			get { return 0; }
		}
		public IEnumerator<char> GetEnumerator()
		{
			return EmptyEnumerator<char>.Default;
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return EmptyEnumerator<char>.Default;
		}
		public SourcePos IndexToLine(int index)
		{
			return SourcePos.Nowhere;
		}
		public int LineToIndex(int lineNo)
		{
			return -1;
		}
		public int LineToIndex(SourcePos pos)
		{
			return -1;
		}
	}
}
