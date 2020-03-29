using System;

namespace Loyc.Syntax
{
	public interface IHasFileName
	{
		string FileName { get; }
	}
	/// <summary>A line/column pair representing a location in a text file. 
	/// Numbering starts at one for both Line and Column.</summary>
	public interface ILineAndColumn
	{
		int Line { get; }
		int Column { get; }
	}
	public interface ILineColumnFile : ILineAndColumn, IHasFileName { }
}
