using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc;
using Loyc.Math;
using Loyc.CompilerCore;

namespace Loyc.LLParserGenerator
{
	using S = ecs.CodeSymbols;

	// Refactoring plan:
	//  DONE 1. Support switch() for chars and ints, not symbols
	//  DONE 2. Change unit tests to use switch() where needed
	//  DONE 3. Change IPGTerminalSet to be fully immutable
	// 1test 4. Write unit tests for Symbol stream parsing
	//  DONE 5. Write PGSymbolSet
	//  DONE 6. Eliminate Symbol support from PGIntSet
	//       7. Write PGCodeGenForSymbolStream
	//       8. Replace unnecessary Match() calls with Consume(); eliminate unnecessary Check()s

	/// <summary>Standard code generator for streams of <see cref="Symbol"/>s.</summary>
	class PGCodeGenForSymbolStream : PGCodeSnippetGeneratorBase
	{
		protected static readonly Symbol _Symbol = GSymbol.Get("Symbol");

		public override IPGTerminalSet EmptySet
		{
			get { return PGSymbolSet.Empty; }
		}
		protected override Node GenerateTest(IPGTerminalSet set, Node subject, Symbol setName)
		{
			return ((PGSymbolSet)set).GenerateTest(subject, setName);
		}
		protected override Node GenerateSetDecl(IPGTerminalSet set, Symbol setName)
		{
			return ((PGSymbolSet)set).GenerateSetDecl(setName);
		}

		public override Node GenerateMatch(IPGTerminalSet set_)
		{
			throw new NotImplementedException();
		}
		public override GreenNode LAType()
		{
			return F.Symbol(_Symbol);
		}
		public override bool ShouldGenerateSwitch(IPGTerminalSet[] sets, bool needErrorBranch, HashSet<int> casesToInclude)
		{
			throw new NotImplementedException();
		}
		public override Node GenerateSwitch(IPGTerminalSet[] branchSets, Node[] branchCode, HashSet<int> casesToInclude, Node defaultBranch, GreenNode laVar)
		{
			throw new NotImplementedException();
		}
	}
}
