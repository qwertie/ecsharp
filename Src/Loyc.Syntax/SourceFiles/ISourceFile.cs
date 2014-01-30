using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;
using Loyc.Collections;

namespace Loyc.Syntax
{
	/// <summary>Represents a text file with a file name, plus the data necessary 
	/// to convert between line-column positions and 0-based integer indexes.</summary>
	public interface ISourceFile : IIndexPositionMapper
	{
		ICharSource Text { get; }
		string FileName { get; }
	}
}
