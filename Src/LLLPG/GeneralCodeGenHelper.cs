using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc;
using Loyc.Math;
using Loyc.Syntax;
using Loyc.Collections;
using Loyc.Utilities;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.LLParserGenerator
{

	// Refactoring plan:
	//  DONE 1. Support switch() for chars and ints, not symbols
	//  DONE 2. Change unit tests to use switch() where needed
	//  DONE 3. Change IPGTerminalSet to be fully immutable
	//  DONE 4. Write unit tests for Symbol stream parsing
	//  DONE 5. Write PGSymbolSet
	//  DONE 6. Eliminate Symbol support from PGIntSet
	//  DONE 7. Write PGCodeGenForSymbolStream
	//  DONE 8. Implement support for terminals of unknown value (based on Ids)
	//       9. Implement syntactic predicates
	//  DONE 10. Replace unnecessary Match() calls with Skip(); eliminate unnecessary Check()s

	/// <summary>General-purpose code generator that supports any language with a finite number
	/// of input symbols represented by <see cref="LNode"/> expressions.</summary>
	/// <remarks>To use, assign a new instance of this class to 
	/// <see cref="LLParserGenerator.SnippetGenerator"/>
	/// <para/>
	/// This code generator operates on sets of <see cref="LNode"/>s. It assumes that every 
	/// expression in a set is a unique terminal; for example, it assumes that the expressions
	/// 123 and Foo represent two different terminals. The expected data type of each terminal
	/// is given to the constructor (the default is int).
	/// </remarks>
	public class GeneralCodeGenHelper : CodeGenHelperBase
	{
		static readonly LNodeFactory F_ = new LNodeFactory(new EmptySourceFile("GeneralCodeGenHelper.cs"));
		public static readonly LNode EOF = F_.Id("EOF");

		protected static readonly Symbol _Symbol = GSymbol.Get("Symbol");

		public LNode LaType, SetType;
		public bool AllowSwitch;

		public GeneralCodeGenHelper(string laType = "#int", bool allowSwitch = true) : this(F_.Id(laType), null, allowSwitch) { }
		public GeneralCodeGenHelper(LNode laType, LNode setType = null, bool allowSwitch = true)
		{
			LaType = laType;
			SetType = setType ?? F_.Of(F_.Id("HashSet"), laType);
			AllowSwitch = allowSwitch;
		}

		public override IPGTerminalSet EmptySet
		{
			get { return PGNodeSet.Empty; }
		}

		static readonly LNode __ = F_.Id("_");
		public override string Example(IPGTerminalSet set_)
		{
			var set = (PGNodeSet)set_;

			if (set.IsInverted) {
				if (set.Contains(__))
					return "_";
				else for (int i = 0; ; i++)
					if (set.Contains(F.Literal(i)))
						return i.ToString();
			}
			var ex = set.BaseSet.FirstOrDefault();
			if (ex == null)
				return set.IsEmpty ? "<nothing>" : "<EOF>";
			return ex.Print(NodeStyle.Expression);
		}

		static readonly Symbol _Contains = GSymbol.Get("Contains");

		protected override LNode GenerateTest(IPGTerminalSet set_, LNode subject, Symbol setName)
		{
			var set = (PGNodeSet)set_;

			if (setName != null) {
				// setName.Contains($subject)
				var test = F.Call(F.Dot(setName, _Contains), subject);
				return set.IsInverted ? F.Call(S.Not, test) : test;
			} else {
				if (set.BaseSet.Count > 5)
					return null; // complex

				LNode test, result = null;
				// Note: sort the set so that the unit tests are deterministic
				foreach (LNode item in set.BaseSet.OrderBy(s => s.ToString())) {
					test = F.Call(S.Eq, subject, item);
					if (result == null)
						result = test;
					else
						result = F.Call(S.Or, result, test);
				}
				if (set.IsInverted) {
					if (result == null)
						return F.@true;
					if (result.Calls(S.Eq))
						result = result.WithTarget(S.Neq);
					else
						result = F.Call(S.Not, F.InParens(result));
				}
				return result ?? F.@false;
			}

		}
		protected override LNode GenerateSetDecl(IPGTerminalSet set_, Symbol setName)
		{
			var set = (PGNodeSet)set_;
			// static readonly $SetType $setName = new $SetType(new $SetType[] { ... });
			// Sort the list so that the test suite can compare results deterministically
			var setMemberList = set.BaseSet.OrderBy(s => s.ToString());
			return F.Attr(F.Id(S.Static), F.Id(S.Readonly),
				F.Var(SetType, setName, 
					F.Call(S.New, F.Call(SetType)).PlusArgs(setMemberList)));
		}

		public override LNode GenerateMatch(IPGTerminalSet set_, bool savingResult, bool recognizerMode)
		{
			var set = (PGNodeSet)set_;

			LNode call;
			if (set.BaseSet.Count <= 4 && !set_.ContainsEOF) {
				IEnumerable<LNode> symbols = set.BaseSet;
				//if (!set.IsInverted)
				//	symbols = symbols.Where(s => !s.Equals(EOF));
				call = F.Call(recognizerMode 
					? (set.IsInverted ? _IsMatchExcept : _IsMatch)
					: (set.IsInverted ? _MatchExcept : _Match),
					symbols.OrderBy(s => s.ToString()));
			} else {
				var setName = GenerateSetDecl(set_);
				call = F.Call(recognizerMode ? _IsMatch : _Match, F.Id(setName));
			}
			if (recognizerMode)
				call = F.Call(S.If, F.Call(S.Not, call), F.Call(S.Return, F.@false));
			return call;
		}
		
		public override LNode LAType()
		{
			return LaType;
		}

		protected override int GetRelativeCostForSwitch(IPGTerminalSet set_)
		{
			var set = (PGNodeSet)set_;
			if (!AllowSwitch || set.IsInverted)
				return -1000000;

			int switchCost = 1 + set.BaseSet.Count;
			int ifCost = System.Math.Min(set.BaseSet.Count * 4, 32);
			return ifCost - switchCost;
		}

		protected override IEnumerable<LNode> GetCases(IPGTerminalSet set_)
		{
			var set = (PGNodeSet)set_;
			Debug.Assert(!set.IsInverted);
			return set.BaseSet;
		}
	}


	/// <summary>An immutable set that implements <see cref="IPGTerminalSet"/> so
	/// that it can be used by <see cref="LLParserGenerator"/>.</summary>
	/// <typeparam name="T"></typeparam>
	/// <remarks>
	/// This is an abstract base class; a derived class is required to define the
	/// conversion from T to LNode (<see cref="ToLNode(T)"/>), to define the value 
	/// of EOF (<see cref="EOF_T"/>), and to create derived class instances 
	/// (<see cref="New(InvertibleSet{T})"/>).
	/// <para/>
	/// This class could be used to represent any kind of set, including integer 
	/// and character sets. However, if the values of all the integers or 
	/// characters are known at compile-time (when the parser is generated) then it 
	/// may be more efficient to use <see cref="PGIntSet"/> instead of this class.
	/// <see cref="PGIntSet"/> can do range comparisons, for example the test for
	/// 'A'..'Z' can be written as <c>la0 >= 'A' && 'Z' >= la0</c>; this class does
	/// not understand ranges so it cannot do efficient tests like that.
	/// </remarks>
	public class PGNodeSet : InvertibleSet<LNode>, IPGTerminalSet
	{
		public PGNodeSet(Set<LNode> set, bool inverted = false) : base(set, inverted) { }
		public PGNodeSet(InvertibleSet<LNode> set) : base(set.BaseSet, set.IsInverted) { }
		public PGNodeSet(IEnumerable<LNode> list, bool inverted = false) : base(list, inverted) { }

		public static readonly LNode EOF_node = GeneralCodeGenHelper.EOF;
		public new static readonly PGNodeSet Empty = new PGNodeSet(Set<LNode>.Empty);
		public new static readonly PGNodeSet All = new PGNodeSet(InvertibleSet<LNode>.All);
		public static readonly PGNodeSet EOF = new PGNodeSet(InvertibleSet<LNode>.Empty.With(EOF_node));
		public static readonly PGNodeSet AllExceptEOF = new PGNodeSet(InvertibleSet<LNode>.All.Without(EOF_node));

		#region IPGTerminalSet

		public IPGTerminalSet UnionCore(IPGTerminalSet other)
		{
			var otherSS = other as PGNodeSet;
			if (otherSS == null) return null;
			return new PGNodeSet(Union(otherSS));
		}
		IPGTerminalSet IPGTerminalSet.IntersectionCore(IPGTerminalSet other, bool subtract, bool subtractThis)
		{
			var otherSS = other as PGNodeSet;
			if (otherSS == null) return null;
			return Intersect(otherSS, subtract, subtractThis);
		}
		public PGNodeSet Intersect(PGNodeSet other, bool subtract = false, bool subtractThis = false)
		{
			if (subtractThis) {
				Debug.Assert(!subtract);
				return other.Intersect(this, true);
			} else
				return new PGNodeSet(base.Intersect(other, subtract));
		}

		public bool ContainsEOF
		{
			get { return Contains(EOF_node); }
		}
		bool IPGTerminalSet.IsEmptySet
		{
			get { return IsEmpty; }
		}
		public IPGTerminalSet WithEOF(bool wantEOF = true)
		{
			return new PGNodeSet(base.With(EOF_node, !wantEOF));
		}
		IPGTerminalSet IPGTerminalSet.Inverted()
		{
			return new PGNodeSet(BaseSet, !IsInverted);
		}

		public IPGTerminalSet Optimize(IPGTerminalSet dontcare)
		{
			var dontcareSS = dontcare as PGNodeSet;
			if (dontcareSS == null) return this;
			return new PGNodeSet(Except(dontcareSS));
		}

		public char? ExampleChar
		{
			get { return null; }
		}

		public bool Equals(IPGTerminalSet other)
		{
			if (other is PGNodeSet)
				return SetEquals((PGNodeSet)other);
			else
				return this.SlowEquals(other);
		}

		IPGTerminalSet IPGTerminalSet.Empty { get { return new PGNodeSet(InvertibleSet<LNode>.Empty); } }

		#endregion
	}

}
