using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Loyc.MiniTest;
using Loyc.Utilities;
using System.ComponentModel;

namespace Loyc.Syntax
{
	/// <summary>This just helps end-users to discover the name change 
	/// SourcePos.PosInLine => ILineAndColumn.Column during upgrades</summary>
	public static class SourcePosIsObsolete
	{
		
		[Obsolete("The name has changed to \"Column\"")]
		public static int PosInLine(this ILineAndColumn c) => c.Column;
	}

	/// <summary>Please use the new name of this class: LineAndColumn.
	/// Holds a line number (Line) and a position in the line (Column).
	/// This class isn't really needed in Loyc but is separated from SourcePos 
	/// in case anyone might want position without a filename.</summary>
	/// <remarks>Numbering starts at one for both Line and Column. 
	/// Line=0 signifies nowhere in particular, or an unknown location.</remarks>
	[Obsolete("Please use the new name of this class: LineAndColumn")]
	public class LineAndCol : ILineAndColumn
	{
		protected LineAndCol() { }
		public LineAndCol(int Line, int Column)
			{ _line = Line; _column = Column; }// this.FileName = FileName; }

		protected int _line;
		protected int _column;
		
		public int Line { get { return _line; } }
		public int Column { get { return _column; } }
		
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This has been renamed to Column")]
		public int PosInLine { get { return _column; } }
		
		public override string ToString()
		{
			if (Line <= 0)
				return "Nowhere";
			else
				return string.Format("{0}:{1}", Line, Column);
		}

		public override bool Equals(object obj)
		{
			LineAndCol other = obj as LineAndCol;
			if (other == null)
				return false;
			return other._line == _line && other._column == _column;
		}
		public override int GetHashCode()
		{
			return (_line << 4) ^ _column;
		}
		public static LineAndCol Nowhere = new LineAndCol();
	}

	/// <summary>Please use the new name of this class: LineColumnFile.
	/// Holds a filename (FileName), a line number (Line) and a position in 
	/// the line (Column), representing a position in a source code file.</summary>
	/// <remarks>
	/// Line and column numbering both start at one (1). Line=0 signifies nowhere 
	/// in particular. Instances are immutable.
	/// </remarks>
	[Obsolete("Please use the new name of this class: LineColumnFile")]
	public class SourcePos : LineAndColumn, ILineColumnFile
	{
		protected SourcePos() { }
		public SourcePos(string FileName, int Line, int PosInLine)
			: base(Line, PosInLine) { _fileName = FileName ?? ""; }
		
		protected string _fileName;
		public string FileName { get { return _fileName; } }

		public override string ToString()
		{
			if (Line <= 0)
				return "Nowhere";
			else
				return string.Format("{0}({1},{2})", FileName, Line, Column);
		}
		public override bool Equals(object obj)
		{
			SourcePos other = obj as SourcePos;
			if (other == null)
				return false;
			return other._fileName == _fileName && base.Equals(obj);
		}
		public override int GetHashCode()
		{
			return base.GetHashCode() ^ _fileName.GetHashCode();
		}
		new public static LineColumnFile Nowhere = new LineColumnFile();
	}

	#pragma warning disable 618 // Obsoleteness warning

	/// <summary>This is the new (and recommended) name for LineAndCol. It
	/// holds a line number (Line) and a position in the line (Column).
	/// Numbering starts at one for both Line and Column.</summary>
	public class LineAndColumn : LineAndCol
	{
		protected LineAndColumn() { }
		public LineAndColumn(int Line, int Column)
			{ _line = Line; _column = Column; }// this.FileName = FileName; }
	}

	/// <summary>This is the new (and recommended) name for SourcePos. It's named 
	/// after what it contains: a line number, column number and file name.
	/// Numbering starts at one for both Line and Column.</summary>
	public class LineColumnFile : SourcePos
	{
		internal LineColumnFile() { }
		public LineColumnFile(int Line, int PosInLine, string FileName)
			: base(FileName, Line, PosInLine) { }
		public LineColumnFile(string FileName, int Line, int PosInLine)
			: base(FileName, Line, PosInLine) { }
	}
}
