using System;
using System.Collections.Generic;
using System.Text;
using Loyc.CompilerCore;
using Loyc.Essentials;
using Loyc.Collections;

namespace Loyc.Utilities
{
	public class EmptySourceFile : IterableBase<char>, ISourceFile
	{
		public static readonly EmptySourceFile Default = new EmptySourceFile("");
		public static readonly EmptySourceFile Unknown = new EmptySourceFile("Unknown");

		private string _fileName;
		//private string _lang;

		public EmptySourceFile(string fileName)//, string lang)
		{
			_fileName = fileName;
			//_lang = lang;
		}
		public string FileName
		{
			get { return _fileName; }
		}
		//public string Language
		//{
		//    get { return _lang; }
		//}
		public string Substring(int startIndex, int length)
		{
			return "";
		}
		public char this[int index]
		{
			get { throw new IndexOutOfRangeException("EmptySourceFile"); }
		}
		public char this[int index, char defaultValue]
		{
			get { return defaultValue; }
		}
		public char TryGet(int index, ref bool fail)
		{
			fail = true;
			return default(char);
		}
		public int Count
		{
			get { return 0; }
		}
		public override Iterator<char> GetIterator()
		{
			return EmptyIterator<char>.Value;
		}
		public int IndexOf(char item)
		{
			return -1;
		}
		public bool Contains(char item)
		{
			return false;
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
