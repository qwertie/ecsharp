using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Loyc.MiniTest;
using Loyc.Collections;

namespace Loyc.Syntax
{
	/// <summary>Represents a text file with a file name and its textual content,
	/// plus the data necessary to convert between line-column positions and 
	/// 0-based integer indexes.</summary>
	public interface ISourceFile : IIndexPositionMapper
	{
		ICharSource Text { get; }
		string FileName { get; }
	}

	/// <summary>A default implementation of ISourceFile based on <see cref="IndexPositionMapper"/>.</summary>
	public class SourceFile<CharSource> : IndexPositionMapper<CharSource>, ISourceFile
		where CharSource : ICharSource
	{
		new protected CharSource _source;

		public SourceFile(CharSource source, SourcePos startingPos = null) : base(source, startingPos) { _source = source; }
		public SourceFile(CharSource source, string fileName) : base(source, fileName) { _source = source; }

		public CharSource Text
		{
			get { return _source; }
		}
		ICharSource ISourceFile.Text
		{
			get { return Text; }
		}

        protected override SourcePos NewSourcePos(int Line, int PosInLine)
        {
            return new SourcePosAndFile(this, base.NewSourcePos(Line, PosInLine));
        }
	}

	[Obsolete("Please use SourceFile<ICharSource> instead.")]
	public class SourceFile : SourceFile<ICharSource>
	{
		public SourceFile(ICharSource source, SourcePos startingPos = null) : base(source, startingPos) { }
		public SourceFile(ICharSource source, string fileName) : base(source, fileName) { }
	} 
}
