using Loyc.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Syntax
{
	/// <summary>Represents a text file with a file name and its textual content,
	/// plus the data necessary to convert between line-column positions and 
	/// 0-based integer indexes.</summary>
	public interface ISourceFile : IIndexPositionMapper
	{
		ICharSource Text { get; }
	}
}
