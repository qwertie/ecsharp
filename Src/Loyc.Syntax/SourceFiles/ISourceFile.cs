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

	/// <summary>A default implementation of ISourceFile based on <see cref="IndexPositionMapper"/>.</summary>
	public class SourceFile : IndexPositionMapper, ISourceFile
	{
		new protected ICharSource _source;

		public SourceFile(ICharSource source, SourcePos startingPos = null) : base(source, startingPos) { _source = source; }
		public SourceFile(ICharSource source, string fileName) : base(source, fileName) { _source = source; }

		public ICharSource Text
		{
			get { return _source; }
		}
	}
}
