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

namespace Loyc.LLParserGenerator
{
	using S = ecs.CodeSymbols;
	using Loyc.Collections;
	using Loyc.Syntax;

	/// <summary>Standard code generator for character/integer input streams
	/// and is the default code generator for <see cref="LLParserGenerator"/>.</summary>
	class PGCodeGenForIntStream : PGCodeSnippetGeneratorBase
	{
		public const int EOF_int = PGIntSet.EOF_int;

		public override IPGTerminalSet EmptySet
		{
			get { return PGIntSet.Empty; }
		}

		public override IPGTerminalSet Optimize(IPGTerminalSet set, IPGTerminalSet dontcare)
		{
			return ((PGIntSet)set).Optimize((IntSet)dontcare);
		}

		public int? ExampleInt(PGIntSet set)
		{
			if (set.IsCharSet && set.IsInverted && set.Contains('_'))
				return '_';
			if (set.IsEmptySet)
				return null;
			int example = int.MinValue;
			int min = set.IsCharSet ? 32 : 0;
			foreach (var range in set.Runs()) {
				example = range.Lo < min ? range.Hi : range.Lo;
				if (example > min)
					break;
			}
			return example;
		}
		public override char? ExampleChar(IPGTerminalSet set_)
		{
			var set = ((PGIntSet)set_);
			
			if (!set.IsCharSet)
				return null;
			int? ex = ExampleInt(set);
			char c;
			if (ex == null || (c = (char)ex.Value) != ex.Value)
				return null;
			return c;
		}
		public override string Example(IPGTerminalSet set_)
		{
			var set = ((PGIntSet)set_);
			
			char? ch = ExampleChar(set);
			if (ch != null)
				return ch == '\'' ? @"'\''" : string.Format("'{0}'", ch);
			int? ex = ExampleInt(set);
			if (ex == null)
				return "<nothing>";
			if (ex == EOF_int)
				return "<EOF>";
			return ex.Value.ToString();
		}

		protected override Node GenerateTest(IPGTerminalSet set, Node subject, Symbol setName)
		{
			return ((PGIntSet)set).GenerateTest(subject, setName);
		}
		protected override Node GenerateSetDecl(IPGTerminalSet set, Symbol setName)
		{
			return ((PGIntSet)set).GenerateSetDecl(setName);
		}

		public override Node GenerateMatch(IPGTerminalSet set_)
		{
			var set = (PGIntSet)set_;
			if (set.Complexity(2, 3, !set.IsInverted) <= 6) {
				Node call;
				var args = new RWList<LNode>();
				if (set.Complexity(1, 2, true) > set.Count) {
					// Use MatchRange or MatchExceptRange
					foreach (var r in set) {
						if (!set.IsInverted || r.Lo != EOF_int || r.Hi != EOF_int) {
							args.Add((Node)set.MakeLiteral(r.Lo));
							args.Add((Node)set.MakeLiteral(r.Hi));
						}
					}
					call = F.Call(set.IsInverted ? _MatchExceptRange : _MatchRange, args.ToRVList());
				} else {
					// Use Match or MatchExcept
					foreach (var r in set) {
						for (int c = r.Lo; c <= r.Hi; c++) {
							if (!set.IsInverted || c != EOF_int)
								args.Add((Node)set.MakeLiteral(c));
						}
					}
					call = F.Call(set.IsInverted ? _MatchExcept : _Match, args.ToRVList());
				}
				return call;
			}

			var setName = GenerateSetDecl(set_);
			return F.Call(_Match, F.Id(setName));
		}

		public override GreenNode LAType()
		{
			return F.Int32;
		}

		protected override int GetRelativeCostForSwitch(IPGTerminalSet set)
		{
			var intset = (PGIntSet)set;
			int switchCost = (int)System.Math.Min(1 + intset.Size, 1000000);
			int ifCost = System.Math.Min(intset.Complexity(4, 8, true), 32);
			return ifCost - switchCost;
		}

		protected override IEnumerable<LNode> GetCases(IPGTerminalSet set)
		{
			var intset = (PGIntSet)set;
			foreach (IntRange range in intset) {
				for (int ch = range.Lo; ch <= range.Hi; ch++) {
					bool isChar = intset.IsCharSet && (char)ch == ch;
					yield return F.Literal(isChar ? (object)(char)ch : (object)ch);
				}
			}
		}
	}
}
