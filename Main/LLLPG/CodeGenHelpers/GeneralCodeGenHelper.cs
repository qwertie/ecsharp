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
	/// <summary>General-purpose code generator that supports any language with a 
	/// finite number of input symbols represented by <see cref="LNode"/> 
	/// expressions. This is the code generator helper for <c>LLLPG parser {...}</c>.</summary>
	/// <remarks>
	/// To use, assign a new instance of this class to 
	/// <see cref="LLParserGenerator.CodeGenHelper"/>
	/// <para/>
	/// This code generator operates on sets of <see cref="LNode"/>s. It assumes that 
	/// every expression in a set is a unique terminal; for example, it assumes that 
	/// the expressions 123 and Foo represent two different terminals. The expected 
	/// data type of each terminal is given to the constructor (the default is int).
	/// </remarks>
	public class GeneralCodeGenHelper : CodeGenHelperBase
	{
		static readonly LNodeFactory F_ = new LNodeFactory(new EmptySourceFile("GeneralCodeGenHelper.cs"));
		public static readonly LNode EOF = F_.Id("EOF");

		protected static readonly Symbol _Symbol = GSymbol.Get("Symbol");
		protected static readonly Symbol _HashSet = GSymbol.Get("HashSet");
		protected static readonly Symbol _NewSet = GSymbol.Get("NewSet");

		/// <summary>Specifies the data type of LA0 and lookahead variables.</summary>
		public LNode LaType;

		/// <summary>Whether to cast the result of LA0 and LA(i) to LaType (default: true)</summary>
		public bool CastLA { get; set; }
		
		/// <summary>Specifies the data type for large terminal sets (default: <see cref="HashSet{T}"/>).</summary>
		public LNode SetType;
		
		/// <summary>Specified whether this class is allowed to generate C# switch() 
		/// statements.</summary>
		/// <remarks>C# switch() only allows constant values as cases. If the token
		/// values are not constants (e.g. if they are symbols), you'll have to 
		/// disable switch generation.</remarks>
		public bool AllowSwitch;

		/// <summary>If MatchCast is set, a cast to this type is added when calling 
		/// Match() or NewSet() or set.Contains().</summary>
		/// <remarks>
		/// This requires some explanation because it's a bit subtle. I made the
		/// decision to implement <see cref="BaseParser{Token}"/> with <c>Match(...)</c>
		/// methods that accept integers, e.g. <c>Match(int a, int b, int c)</c>. I 
		/// could have parameterized <c>BaseParser</c> and its <c>Match</c> methods
		/// on the token type (e.g. BaseParser(Token,TokenType)) but unfortunately 
		/// this lowers performance because if BaseParser doesn't know that 
		/// TokenType is an integer or an enum, it requires three virtual method 
		/// calls to compare the current token with a, b and c (also, note that C# 
		/// prohibits "enum" as a generic constraint for reasons unknown).
		/// <para/>
		/// To avoid this performance snag, BaseParser just assumes that the token
		/// type is an integer. Of course, the derived class will still use named
		/// enum values. If the enum type is called TT, <c>Match(TT.A, TT.B, TT.C)</c>
		/// produces a C# compiler error, so LLLPG needs to generate a cast to int:
		/// <c>Match((int) TT.A, (int) TT.B, (int) TT.C)</c>. That's what this 
		/// option is for. When you set this option, it inserts a cast to the 
		/// specified type. Normally you'll set it to #int32.
		/// <para/>
		/// When using this option, LaType should still be the enum type rather 
		/// than #int32.
		/// </remarks>
		public LNode MatchCast;

		public GeneralCodeGenHelper(LNode laType = null, bool allowSwitch = true) 
			: this(laType ?? F_.Int32, null, allowSwitch) { }
		public GeneralCodeGenHelper(LNode laType, LNode setType = null, bool allowSwitch = true)
		{
			LaType = laType;
			CastLA = true;
			SetType = setType ?? F_.Of(F_.Id(_HashSet), laType);
			AllowSwitch = allowSwitch;
		}

		public override IPGTerminalSet EmptySet
		{
			get { return PGNodeSet.Empty; }
		}

		public override Pred CodeToTerminalPred(LNode expr, ref string errorMsg)
		{
			Debug.Assert(!expr.IsIdNamed(EOF.Name) || expr.Equals(EOF));
			if (expr.IsCall && expr.Name != S.Dot && expr.Name != S.Of)
				errorMsg = "Unrecognized expression. Treating this as a terminal: " + expr.ToString(); // warning

			var expr2 = ResolveAlias(expr);

			PGNodeSet set;
			if (expr2.IsIdNamed(_underscore))
				set = PGNodeSet.AllExceptEOF;
			else
				set = new PGNodeSet(expr2);
			// bug fix 2015-07: must use expr, not expr2, as TerminalPred's Basis 
			// (wrong Basis breaks error locations, and $A in code blocks if A is an alias)
			return new TerminalPred(expr, set, true);
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
			return ex.Print(ParsingMode.Expressions);
		}

		static readonly Symbol _Contains = GSymbol.Get("Contains");

		protected override LNode GenerateTest(IPGTerminalSet set_, LNode subject, Symbol setName)
		{
			var set = (PGNodeSet)set_;

			if (setName != null) {
				// setName.Contains($subject)
				if (MatchCast != null)
					subject = F.Call(S.Cast, subject, MatchCast);
				var test = F.Call(F.Dot(setName, _Contains), subject);
				return set.IsInverted ? F.Call(S.Not, test) : test;
			} else {
				if (set.BaseSet.Count > 5)
					return null; // complex

				LNode test, result = null;
				// Note: sort the set so that the unit tests are deterministic
				foreach (LNode item in set.BaseSet.OrderBy(s => s.ToString())) {
					var item2 = item;
					if (item == PGNodeSet.EOF_node) {
						if (InputClass != null)
							item2 = F.Dot(InputClass, item);
						if (LaType != null)
							item2 = F.Call(S.Cast, item2, LaType);
					}
					test = F.Call(S.Eq, subject, item2);
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
			// static readonly $SetType $setName = NewSet(new $SetType[] { ... });
			// Sort the list so that the test suite can compare results deterministically
			IEnumerable<LNode> setMemberList = set.BaseSet.OrderBy(s => s.ToString());
			if (MatchCast != null)
				setMemberList = setMemberList.Select(item => F.Call(S.Cast, item, MatchCast));
			return F.Attr(F.Id(S.Static), F.Id(S.Readonly),
				F.Var(SetType, setName, F.Call(_NewSet, setMemberList)));
		}

		public override LNode GenerateMatchExpr(IPGTerminalSet set_, bool savingResult, bool recognizerMode)
		{
			var set = (PGNodeSet)set_;

			LNode call;

			int baseCount = set.BaseSet.Count;
			IEnumerable<LNode> symbols = set.BaseSet;
			if (set.IsInverted) {
				if (set.ContainsEOF) // Unusual set: ((~something)|EOF)
					baseCount = int.MaxValue;
				else { // Normal inverted set ~X has output "MatchExcept(X)"
					// which is a synonym for "MatchExcept(X, EOF)"
					symbols = symbols.Where(s => !s.IsIdNamed(EOF.Name));
					baseCount--;
				}
			}
			if (baseCount <= 4) {
				call = ApiCall(recognizerMode 
					? (set.IsInverted ? _TryMatchExcept : _TryMatch)
					: (set.IsInverted ? _MatchExcept : _Match),
					MatchArgs(symbols));
			} else {
				var setName = GenerateSetDecl(set);
				if (set.IsInverted)
					call = ApiCall(recognizerMode ? _TryMatchExcept : _MatchExcept, F.Id(setName));
				else
					call = ApiCall(recognizerMode ? _TryMatch : _Match, F.Id(setName));
			}
			return call;
		}
		private IEnumerable<LNode> MatchArgs(IEnumerable<LNode> symbols)
		{
			symbols = symbols.OrderBy(s => s.ToString());
			if (MatchCast != null)
				symbols = symbols.Select(s => F.Call(S.Cast, s, MatchCast));
			return symbols;
		}

		public override LNode LAType()
		{
			return LaType;
		}

		/// <summary>Generates code to read LA(k).</summary>
		public override LNode LA(int k)
		{
			var laK = base.LA(k);
			if (CastLA)
				return F.Call(S.Cast, laK, LaType);
			else
				return laK;
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
			// Sort the cases so they don't change order each time they are generated
			return set.BaseSet.OrderBy(n => n.ToString());
		}
	}


	/// <summary>An immutable set that implements <see cref="IPGTerminalSet"/> so
	/// that it can be used by <see cref="LLParserGenerator"/>.</summary>
	/// <remarks>
	/// This class effectively represents any type of set.
	/// It is used by <see cref="GeneralCodeGenHelper"/>.
	/// </remarks>
	public class PGNodeSet : InvertibleSet<LNode>, IPGTerminalSet
	{
		public PGNodeSet(Set<LNode> set, bool inverted = false) : base(set, inverted) { }
		public PGNodeSet(InvertibleSet<LNode> set) : base(set.BaseSet, set.IsInverted) { }
		public PGNodeSet(IEnumerable<LNode> list, bool inverted = false) : base(list, inverted) { }
		public PGNodeSet(params LNode[] list) : this((IEnumerable<LNode>)list) { }

		public static readonly LNode EOF_node = GeneralCodeGenHelper.EOF;
		public new static readonly PGNodeSet Empty = new PGNodeSet(Set<LNode>.Empty);
		public new static readonly PGNodeSet All = new PGNodeSet(InvertibleSet<LNode>.All);
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

		public override string ToString()
		{
			if (!IsInverted && BaseSet.Count == 1)
				return BaseSet.First().Print(ParsingMode.Expressions);

			var sb = new StringBuilder(40);
			if (IsInverted)
				sb.Append('~');
			sb.Append('(');
			bool first = true;
			var items = BaseSet.Select(node => node.Print(ParsingMode.Expressions)).ToList();
			items.Sort();
			foreach (var item in items) {
				if (!first)
					sb.Append('|');
				sb.Append(item);
				first = false;
			}
			sb.Append(')');
			return sb.ToString();
		}
	}
}
