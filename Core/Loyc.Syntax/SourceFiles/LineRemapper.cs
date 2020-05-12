using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using System.Diagnostics;

namespace Loyc.Syntax
{
	/// <summary>An wrapper around ISourceFile that applies line remapping 
	/// information (if the source file uses it).</summary>
	/// <remarks>
	/// A preprocessor that supports #line may wrap the original <see 
	/// cref="ISourceFile"/> in one of these, even when the source file 
	/// doesn't use #line.
	/// <para/>
	/// Call Remaps.AddRemap() and, optionally, Remaps.EndRemap(), to add each
	/// mapping.
	/// <para/>
	/// <see cref="ISourceFile"/> includes <see cref="IIndexPositionMapper"/>
	/// which allows reverse-mapping from line/position back to index. However,
	/// a position derived from #line information may be ambiguous (does not 
	/// always have a unique reverse mapping), and I'd rather avoid the work of 
	/// reverse mapping anyway. So this class does not perform reverse mapping,
	/// but forward mappings return <see cref="SourcePosAndIndex"/> which are
	/// automatically recognized by <see cref="LineToIndex"/> which thereby 
	/// recovers the original index.
	/// </remarks>
	public class SourceFileWithLineRemaps : WrapperBase<ISourceFile>, ISourceFile
	{
		public SourceFileWithLineRemaps(ISourceFile original) 
			: base(original) { Remaps = new LineRemapper(); }
		
		public LineRemapper Remaps { get; private set; }
		
		public ICharSource Text
		{
			get { return _obj.Text; }
		}
		public string FileName
		{
			get { return _obj.FileName; }
		}
		public int LineToIndex(int lineNo)
		{
			return _obj.LineToIndex(lineNo);
		}
		public int LineToIndex(ILineAndColumn pos)
		{
			if (pos is LineColumnFileAndIndex)
				return (pos as LineColumnFileAndIndex).OriginalIndex;
			return _obj.LineToIndex(pos);
		}
		ILineColumnFile IIndexToLine.IndexToLine(int index) => IndexToLine(index);
		public ILineColumnFile IndexToLine(int index)
		{
			var pos = _obj.IndexToLine(index);
			int line = pos.Line;
			string fn = pos.FileName;
			if (Remaps.Remap(ref line, ref fn))
				return new LineColumnFileAndIndex(line, pos.Column, fn ?? FileName, index);
			return pos;
		}
	}

	/// <summary>Please use the new name of this class, LineColumnFile.
	/// This is a <see cref="LineColumnFile"/> that also includes the original index 
	/// from which the Line and PosInLine were derived.</summary>
	/// <remarks>Returned by <see cref="SourceFileWithLineRemaps.IndexToLine"/>.</remarks>
	[Obsolete("Please use the new name of this class: LineColumnFileAndIndex")]
	public class SourcePosAndIndex : LineColumnFile
	{
		public SourcePosAndIndex(int originalIndex, string fileName, int line, int column)
			: base(fileName, line, column) { OriginalIndex = originalIndex; }
		public int OriginalIndex { get; }
	}

	/// <summary>This is a tuple of a FileName, Line, Column, and OriginalIndex.</summary>
	public class LineColumnFileAndIndex : SourcePosAndIndex
	{
		public LineColumnFileAndIndex(int line, int column, string fileName, int originalIndex)
			: base(originalIndex, fileName, line, column) { }
	}

	/// <summary>A small helper class for languages such as C# and C++ that permit 
	/// the locations reported by error messages to be remapped. This class stores
	/// and applies such commands (#line in C#/C++)</summary>
	/// <remarks>
	/// This is part of <see cref="SourceFileWithLineRemaps"/>.
	/// One LineRemapper should be created per real source file.</remarks>
	public class LineRemapper
	{
		public LineRemapper() { }

		BDictionary<int, Pair<int, string>> _map = new BDictionary<int,Pair<int,string>>();

		/// <summary>Adds a mapping that starts on the specified real line.</summary>
		/// <remarks>In C++ and C#, a directive like "#line 200" affects the line 
		/// after the preprocessor directive. So if "#line 200" is on line 10, 
		/// you'd call AddRemap(11, 200) or possibly AddRemap(10, 199).</remarks>
		public void AddRemap(int realLine, int reportLine, string reportFileName = null)
		{
			_map[realLine] = Pair.Create(reportLine, reportFileName);
		}
		/// <summary>Corresponds to <c>#line default</c> in C#.</summary>
		public void EndRemap(int realLine)
		{
			_map[realLine] = Pair.Create(realLine, (string)null);
		}

		/// <summary>Remaps the specified line number, if a remapping has been created that applies to it.</summary>
		/// <param name="line">On entry, a real line number. On exit, a remapped line number</param>
		/// <param name="fileName">This is changed to the user-specified file name 
		/// string, if and only if a file-name remapping exists and applies here.</param>
		/// <returns>true if a remapping exists and was applied, false if not.</returns>
		public bool Remap(ref int line, ref string fileName)
		{
			int i = _map.FindUpperBound(line) - 1;
			if (i == -1)
				return false;
			var kvp = ((AListBase<int, KeyValuePair<int, Pair<int, string>>>)_map)[i];
			if (kvp.Value.B != null)
				fileName = kvp.Value.B;
			else if (kvp.Key == kvp.Value.A)
				return false; // remap ended
			
			int realLine = kvp.Key;
			int reportLine = kvp.Value.A;
			Debug.Assert(line >= realLine);
			line = reportLine + (line - realLine);
			return true;
		}
	}
}
