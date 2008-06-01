using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Runtime;

namespace Loyc.CompilerCore
{
	interface IMainParser
	{
		IEnumerable<IToken> TreeTokenSource { get; set; }
		void SetSource(ICharSource source, IDictionary<string, Symbol> keywords);
	}
}
