using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Essentials;
using Loyc.Collections;

namespace Loyc.Syntax
{
	/// <summary>
	/// A dummy implementation of ISourceFile that has only a filename, no source text.
	/// Used as the source file of synthetic syntax nodes.
	/// </summary>
	public class EmptySourceFile : ListSourceBase<char>, ISourceFile
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
		public UString Substring(int startIndex, int length)
		{
			return "";
		}
		public char this[int index, char defaultValue]
		{
			get { return defaultValue; }
		}
		public override char TryGet(int index, ref bool fail)
		{
			fail = true;
			return default(char);
		}
		public override int Count
		{
			get { return 0; }
		}
		public override IEnumerator<char> GetEnumerator()
		{
			return EmptyEnumerator<char>.Value;
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
