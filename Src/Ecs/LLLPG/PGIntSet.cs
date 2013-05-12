using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.CompilerCore;
using System.Diagnostics;
using Loyc.Collections.Impl;
using Loyc.Threading;
using Loyc.Utilities;
using Loyc.Collections;
using Loyc.Syntax;
using S = ecs.CodeSymbols;

namespace Loyc.LLParserGenerator
{
	/// <summary>This interface represents a set of terminals (and <i>only</i> a 
	/// set of terminals, unlike <see cref="TerminalPred"/> which includes actions 
	/// and a Basis Node). Typical parsers and lexers only need one implementation:
	/// <see cref="PGIntSet"/>.</summary>
	/// </summary>
	public interface IPGTerminalSet : IEquatable<IPGTerminalSet>
	{
		/// <summary>Merges two sets.</summary>
		/// <returns>The combination of the two sets, or null if other's type is not supported.</returns>
		IPGTerminalSet UnionCore(IPGTerminalSet other);
		/// <summary>Computes the intersection of two sets.</summary>
		/// <returns>A set that has only items that are in both sets, or null if other's type is not supported.</returns>
		IPGTerminalSet IntersectionCore(IPGTerminalSet other, bool subtract = false, bool subtractThis = false);

		bool IsInverted { get; }
		bool ContainsEOF { get; }
		bool IsEmptySet { get; }
		bool ContainsEverything { get; }

		/// <summary>Adds or removes EOF from the set. If the set doesn't change,
		/// this method may return this.</summary>
		IPGTerminalSet WithEOF(bool wantEOF = true);
		/// <summary>Creates a version of the set with IsInverted toggled.</summary>
		IPGTerminalSet Inverted();

		/// <summary>Simplifies the set, if possible, so that GenerateTest() can
		/// generate simpler code for an if-else chain in a prediction tree.</summary>
		/// <param name="dontcare">A set of terminals that have been ruled out,
		/// i.e. it is already known that the lookahead value is not in this set.</param>
		/// <returns>An optimized set, or this.</returns>
		IPGTerminalSet Optimize(IPGTerminalSet dontcare);

		/// <summary>Returns an example of a character in the set, or null if this 
		/// is not a set of characters or if EOF is the only member of the set.</summary>
		char? ExampleChar { get; }
		/// <summary>Returns an example of an item in the set. If the example is
		/// a character, it should be surrounded by single quotes.</summary>
		string Example { get; }
	}
	public static class PGTerminalSet
	{
		public static IPGTerminalSet Subtract(this IPGTerminalSet @this, IPGTerminalSet other) { return @this.IntersectionCore(other, true) ?? other.IntersectionCore(@this, false, true); }
		public static IPGTerminalSet Union(this IPGTerminalSet @this, IPGTerminalSet other) { return @this.UnionCore(other) ?? other.UnionCore(@this); }
		public static IPGTerminalSet Intersection(this IPGTerminalSet @this, IPGTerminalSet other) { return @this.IntersectionCore(other) ?? other.IntersectionCore(@this); }
		public static IPGTerminalSet WithoutEOF(this IPGTerminalSet @this) { return @this.WithEOF(false); }

		public static bool Overlaps(this IPGTerminalSet @this, IPGTerminalSet other)
		{
			var tmp = @this.Intersection(other);
			return !tmp.IsEmptySet;
		}
		public static bool SlowEquals(this IPGTerminalSet @this, IPGTerminalSet other)
		{
			bool e = @this.ContainsEverything;
			if (e == other.ContainsEverything && @this.ContainsEOF == other.ContainsEOF) {
				if (e)
					return true;
				var sub1 = @this.Subtract(other);
				if (sub1 == null || !sub1.IsEmptySet) return false;
				var sub2 = other.Subtract(@this);
				return sub2 != null && sub2.IsEmptySet;
			}
			return false;
		}
	}

	/// <summary>Represents a set of characters (e.g. 'A'..'Z' | 'a'..'z' | '_'), 
	/// or a set of integers, used in the grammar of a parser.</summary>
	/// <remarks>This class extends <see cref="IntSet"/> to implement 
	/// <see cref="IPGTerminalSet"/>, used by <see cref="LLParserGenerator"/>.
	/// It also contains a a couple of code generation helper methods.
	/// <para/>
	/// -1 is assumed to represent EOF.
	/// </remarks>
	public class PGIntSet : IntSet, IPGTerminalSet
	{
		public const int EOF_int = -1;
		public     static readonly PGIntSet EOF = PGIntSet.With(-1);
		public new static readonly PGIntSet All = new PGIntSet(false, true);
		public     static readonly PGIntSet AllExceptEOF = PGIntSet.Without(-1);
		public new static readonly PGIntSet Empty = new PGIntSet();

		public bool ContainsEOF { get { return Contains(EOF_int); } }
		IPGTerminalSet IPGTerminalSet.WithEOF(bool wantEOF) { return WithEOF(wantEOF); }
		public PGIntSet WithEOF(bool wantEOF = true)
		{
			if (wantEOF == ContainsEOF)
				return this;
			return wantEOF ? Union(EOF) : Subtract(EOF);
		}
		IPGTerminalSet IPGTerminalSet.Inverted() { return (PGIntSet)Inverted(); }

		public new static PGIntSet With(params int[] members) { return new PGIntSet(false, false, false, members); }
		public new static PGIntSet WithRanges(params int[] ranges) { return new PGIntSet(false, false, true, ranges); }
		public new static PGIntSet Without(params int[] members) { return new PGIntSet(false, true, false, members); }
		public new static PGIntSet WithoutRanges(params int[] ranges) { return new PGIntSet(false, true, true, ranges); }
		public new static PGIntSet WithChars(params int[] members) { return new PGIntSet(true, false, false, members); }
		public new static PGIntSet WithCharRanges(params int[] ranges) { return new PGIntSet(true, false, true, ranges); }
		public new static PGIntSet WithoutChars(params int[] members) { return new PGIntSet(true, true, false, members); }
		public new static PGIntSet WithoutCharRanges(params int[] ranges) { return new PGIntSet(true, true, true, ranges); }

		public new static PGIntSet Parse(string members)
		{
			int errorIndex;
			var set = TryParse(members, out errorIndex);
			if (set == null)
				throw new FormatException(string.Format(
					"Input string could not be parsed to a PGIntSet (error at index {0})", errorIndex));
			return set;
		}
		public new static PGIntSet TryParse(string members)
		{
			int _;
			return TryParse(members, out _);
		}
		public new static PGIntSet TryParse(string members, out int errorIndex)
		{
			bool isCharSet, inverted;
			InternalList<IntRange> ranges;
			if (!TryParse(members, out isCharSet, out ranges, out inverted, out errorIndex))
				return null;
			return new PGIntSet(isCharSet, ranges, inverted, true);
		}

		public PGIntSet(bool isCharSet = false, bool inverted = false) : base(isCharSet, inverted) { }
		public PGIntSet(IntRange r, bool isCharSet = false, bool inverted = false) : base(r, isCharSet, inverted) {}
		public PGIntSet(bool isCharSet, bool inverted, params IntRange[] list) : base(isCharSet, inverted, list) {}
		protected PGIntSet(bool isCharSet, InternalList<IntRange> ranges, bool inverted, bool autoSimplify) : base(isCharSet, ranges, inverted, autoSimplify) { }
		protected PGIntSet(bool isCharSet, bool inverted, bool ranges, params int[] list) : base(isCharSet, inverted, ranges, list) { }

		protected override IntSet New(IntSet basis, bool inverted, InternalList<IntRange> ranges)
		{
			return new PGIntSet(basis.IsCharSet, ranges, inverted, false);
		}

		#region IPGTerminalSet

		IPGTerminalSet IPGTerminalSet.UnionCore(IPGTerminalSet other)
		{
			var other_ = other as IntSet;
			if (other_ == null) return null;
			return Union(other_);
		}
		public PGIntSet Union(IntSet other)
		{
			return (PGIntSet)base.Union(other, true);
		}
		IPGTerminalSet IPGTerminalSet.IntersectionCore(IPGTerminalSet other, bool subtract, bool subtractThis)
		{
			var other_ = other as IntSet;
			if (other_ == null) return null;
			return Intersection(other_, subtract, subtractThis);
		}
		new public PGIntSet Intersection(IntSet other, bool subtract = false, bool subtractThis = false)
		{
			return (PGIntSet)base.Intersection(other, subtract, subtractThis);
		}
		new public PGIntSet Subtract(IntSet other)
		{
			return Intersection(other, true);
		}

		IPGTerminalSet IPGTerminalSet.Optimize(IPGTerminalSet dontcare) { return Optimize(dontcare as IntSet); }
		public PGIntSet Optimize(IntSet dontcare)
		{
			return (PGIntSet)base.Optimize(dontcare);
		}

		public int? ExampleInt
		{
			get {
				if (IsCharSet && IsInverted && Contains('_'))
					return '_';
				if (IsEmptySet)
					return null;
				int example = int.MinValue;
				int min = IsCharSet ? 32 : 0;
				foreach (var range in Runs()) {
					example = range.Lo < min ? range.Hi : range.Lo;
					if (example > min)
						break;
				}
				return example;
			}
		}
		public char? ExampleChar
		{
			get {
				if (!IsCharSet)
					return null;
				int? ex = ExampleInt;
				char c;
				if (ex == null || (c = (char)ex.Value) != ex.Value)
					return null;
				return c;
			}
		}
		public string Example
		{
			get {
				char? ch = ExampleChar;
				if (ch != null)
					return ch == '\'' ? @"'\''" : string.Format("'{0}'", ch);
				int? ex = ExampleInt;
				if (ex == null)
					return "<nothing>";
				if (ex == EOF_int)
					return "<EOF>";
				return ex.Value.ToString();
			}
		}

		public bool Equals(IPGTerminalSet other)
		{
			if (other is IntSet)
				return Equals((IntSet)other);
			else
				return this.SlowEquals(other);
		}

		#endregion

		#region Code gen helpers

		protected static LNodeFactory F = new LNodeFactory(new EmptySourceFile("PGIntSet.cs"));
		static readonly Symbol _setName = GSymbol.Get("setName");
		static readonly Symbol _IntSet = GSymbol.Get("IntSet");
		static readonly Symbol _Parse = GSymbol.Get("Parse");
		static readonly Symbol _Contains = GSymbol.Get("Contains");
		static readonly Symbol _With = GSymbol.Get("With");
		static readonly Symbol _Without = GSymbol.Get("Without");
		static readonly LNode _false = F.Literal(false);

		public LNode GenerateSetDecl(Symbol setName)
		{
			return
				F.Attr(F.Id(S.Static), F.Id(S.Readonly),
					F.Var(F.Id("IntSet"), F.Call(setName,
						F.Call(F.Dot(_IntSet, _Parse), F.Literal(this.ToString())))));
		}

		/// <summary>Returns the "complexity" of the set.</summary>
		/// <remarks>The parser generator tests simple sets such as "la0 == ' ' || 
		/// la0 == '\t'" inline using an expression, but large sets are stored in 
		/// variables and tested by calling a method. Complexity() is used to 
		/// decide which approach is more appropriate.</remarks>
		public int Complexity(int singleCountsAs, int rangeCountsAs, bool countEOF)
		{
			int result = 0;
			for (int i = 0; i < _ranges.Count; i++) {
				var r = _ranges[i];
				int dif = r.Hi - r.Lo;
				if (!countEOF && r.Contains(EOF_int) && --dif < 0)
					continue;
				result += (dif == 0 ? singleCountsAs : rangeCountsAs);
			}
			return result;
		}

		public LNode GenerateTest(LNode subject, Symbol setName)
		{
			if (setName != null) {
				// setName.Contains(\subject)
				LNode result = F.Call(F.Dot(setName, _Contains), subject);
				return result;
			} else {
				if (_ranges.Count >= 3 && Complexity(1, 2, true) > 5)
					return null; // complex

				LNode test, result = null;
				for (int i = 0; i < _ranges.Count; i++) {
					var r = _ranges[i];
					if (r.Lo == r.Hi)
						test = F.Call(S.Eq, subject, MakeLiteral(r.Lo));
					else
						test = F.Call(S.And, F.Call(S.GE, subject, MakeLiteral(r.Lo)),
						                     F.Call(S.LE, subject, MakeLiteral(r.Hi)));
					AddTest(ref result, test);
				}
				if (IsInverted) {
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
		internal LNode MakeLiteral(int c)
		{
			if (IsCharSet && c >= 0 && new IntRange(c).CanPrintAsCharRange)
				return F.Literal((char)c);
			else
				return F.Literal(c);
		}
		private void AddTest(ref LNode result, LNode test)
		{
			if (result == null)
				result = test;
			else
				result = F.Call(S.Or, result, test);
		}

		#endregion
	}


}
