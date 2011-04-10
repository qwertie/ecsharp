using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Collections;

namespace Loyc.CompilerCore
{
	/// <summary>
	/// A simple source that can also provide the line number that corresponds to
	/// any index.
	/// </summary>
	/// <remarks>
	/// You should avoid calling Count in case the Count is not known in advance 
	/// (for some sources, the source must be scanned to the end to determine the 
	/// length.) Instead, use the two-argument indexer.
	/// </remarks>
	public interface IParserSource<T> : IListSource<T>, IIndexToLine { }
}
