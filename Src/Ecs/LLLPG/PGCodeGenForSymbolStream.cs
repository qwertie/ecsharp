using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc;
using Loyc.Math;
using Loyc.CompilerCore;
using GreenNode = Loyc.Syntax.LNode;
using Node = Loyc.Syntax.LNode;
using INodeReader = Loyc.Syntax.LNode;

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
	/// <remarks>To use, assign a new instance of this class to 
	/// <see cref="LLParserGenerator.SnippetGenerator"/></remarks>
	class PGCodeGenForSymbolStream : PGCodeSnippetGeneratorBase
	{
		static readonly Symbol EOF_sym = PGSymbolSet.EOF_sym;

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
			var set = (PGSymbolSet)set_;
			if (set.BaseSet.Count <= 6 && !set_.ContainsEOF) {
				Node call = F.Call(set.IsInverted ? _MatchExcept : _Match);
				foreach (Symbol c in set.BaseSet) {
					if (!set.IsInverted || c != EOF_sym)
						call.Args.Add(F.Literal(c));
				}
				return call;
			}

			var setName = GenerateSetDecl(set_);
			return F.Call(_Match, F.Id(setName));
		}
		public override GreenNode LAType()
		{
			return F.Id(_Symbol);
		}
		public override bool ShouldGenerateSwitch(IPGTerminalSet[] sets, bool needErrorBranch, HashSet<int> casesToInclude)
		{
			return false;
		}
		public override Node GenerateSwitch(IPGTerminalSet[] branchSets, Node[] branchCode, HashSet<int> casesToInclude, Node defaultBranch, GreenNode laVar)
		{
			throw new NotImplementedException();
		}
	}
}
