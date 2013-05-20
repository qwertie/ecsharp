using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

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
		/// <summary>Returns the empty set.</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IPGTerminalSet Empty { get; }
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
		public static bool IsSubsetOf(this IPGTerminalSet @this, IPGTerminalSet other)
		{
			var tmp = @this.Subtract(other);
			return tmp.IsEmptySet;
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

}
