using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Syntax;
using System.Diagnostics;
using S = ecs.CodeSymbols;
using Loyc.Utilities;
using ecs;

namespace Loyc.LLParserGenerator
{
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
	public abstract class PGSet<T> : InvertibleSet<T>, IPGTerminalSet
	{
		public PGSet(Set<T> set, bool inverted = false) : base(set, inverted) { }
		public PGSet(InvertibleSet<T> set) : base(set.BaseSet, set.IsInverted) { }
		public PGSet(IEnumerable<T> list, bool inverted = false) : base(list, inverted) { }

		protected abstract PGSet<T> New(InvertibleSet<T> set);
		protected abstract PGSet<T> New(Set<T> set, bool inverted);
		protected abstract T EOF_T { get; }
		protected abstract LNode SetType { get; }
		protected abstract LNode ToLNode(T value);

		#region IPGTerminalSet

		public IPGTerminalSet UnionCore(IPGTerminalSet other)
		{
			var otherSS = other as PGSet<T>;
			if (otherSS == null) return null;
			return New(Union(otherSS));
		}
		IPGTerminalSet IPGTerminalSet.IntersectionCore(IPGTerminalSet other, bool subtract, bool subtractThis)
		{
			var otherSS = other as PGSet<T>;
			if (otherSS == null) return null;
			return Intersect(otherSS, subtract, subtractThis);
		}
		public PGSet<T> Intersect(PGSet<T> other, bool subtract = false, bool subtractThis = false)
		{
			if (subtractThis) {
				Debug.Assert(!subtract);
				return other.Intersect(this, true);
			} else
				return New(base.Intersect(other, subtract));
		}

		public bool ContainsEOF
		{
			get { return Contains(EOF_T); }
		}
		bool IPGTerminalSet.IsEmptySet
		{
			get { return IsEmpty; }
		}
		public IPGTerminalSet WithEOF(bool wantEOF = true)
		{
			return New(base.With(EOF_T, !wantEOF));
		}
		IPGTerminalSet IPGTerminalSet.Inverted()
		{
			return New(BaseSet, !IsInverted);
		}

		public IPGTerminalSet Optimize(IPGTerminalSet dontcare)
		{
			var dontcareSS = dontcare as PGSet<T>;
			if (dontcareSS == null) return this;
			return New(Except(dontcareSS));
		}

		public char? ExampleChar
		{
			get { return null; }
		}

		public bool Equals(IPGTerminalSet other)
		{
			if (other is PGSet<T>)
				return SetEquals((PGSet<T>)other);
			else
				return this.SlowEquals(other);
		}

		IPGTerminalSet IPGTerminalSet.Empty { get { return New(InvertibleSet<T>.Empty); } }

		#endregion

		#region Code gen helpers

		protected static LNodeFactory F = new LNodeFactory(new EmptySourceFile("PGSets.cs"));

		static readonly Symbol _Contains = GSymbol.Get("Contains");
		static readonly Symbol _With = GSymbol.Get("With");
		static readonly Symbol _Without = GSymbol.Get("Without");
		static readonly Symbol _setName = GSymbol.Get("setName");

		public virtual LNode GenerateSetDecl(Symbol setName)
		{
			// InvertibleSet<T> \setName = InvertibleSet<T>.With(...);
			// InvertibleSet<T> \setName = InvertibleSet<T>.Without(...);
			LNode setDecl;
			// Sort the list so that the test suite can compare results deterministically
			var setMemberList = BaseSet.OrderBy(s => s.ToString()).Select(s => ToLNode(s));
			if (IsInverted)
				setDecl = F.Attr(F.Id(S.Static), F.Id(S.Readonly),
					F.Var(SetType, F.Call(setName, F.Call(F.Dot(SetType, F.Id(_Without))).PlusArgs(setMemberList))));
			else
				setDecl = F.Attr(F.Id(S.Static), F.Id(S.Readonly),
					F.Var(SetType, F.Call(setName, F.Call(F.Dot(SetType, F.Id(_With))).PlusArgs(setMemberList))));
			return setDecl;
		}

		public virtual LNode GenerateTest(LNode subject, Symbol setName)
		{
			if (setName != null) {
				// setName.Contains(\subject)
				return F.Call(F.Dot(setName, _Contains), subject);
			} else {
				if (BaseSet.Count > 5)
					return null; // complex

				LNode test, result = null;
				// Note: sort the set so that the unit tests are deterministic
				foreach (T sym in BaseSet.OrderBy(s => s.ToString())) {
					test = F.Call(S.Eq, subject, ToLNode(sym));
					if (result == null)
						result = test;
					else
						result = F.Call(S.Or, result, test);
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

		#endregion
	}

	public class PGSymbolSet : PGSet<Symbol>
	{
		public     static readonly Symbol EOF_sym = null;
		public     static readonly PGSymbolSet EOF = With(EOF_sym);
		public new static readonly PGSymbolSet All = new PGSymbolSet(InvertibleSet<Symbol>.All);
		public     static readonly PGSymbolSet AllExceptEOF = Without(EOF_sym);
		public new static readonly PGSymbolSet Empty = new PGSymbolSet(InvertibleSet<Symbol>.Empty);
		public new static PGSymbolSet With(params Symbol[] list) { return new PGSymbolSet(list, false); }
		public new static PGSymbolSet Without(params Symbol[] list) { return new PGSymbolSet(list, true); }

		public PGSymbolSet(InvertibleSet<Symbol> set) : base(set) { }
		public PGSymbolSet(Set<Symbol> set, bool inverted) : base(set, inverted) { }
		public PGSymbolSet(IEnumerable<Symbol> list, bool inverted) : base(list, inverted) { }

		protected override PGSet<Symbol> New(InvertibleSet<Symbol> set) { return new PGSymbolSet(set); }
		protected override PGSet<Symbol> New(Set<Symbol> set, bool inverted) { return new PGSymbolSet(set, inverted); }

		static readonly Symbol _Symbol = GSymbol.Get("Symbol");
		static readonly Symbol _InvertibleSet = GSymbol.Get("InvertibleSet");
		static readonly LNode _SymbolSet = F.Of(_InvertibleSet, _Symbol);

		protected override Symbol EOF_T { get { return null; } }
		protected override LNode SetType { get { return _SymbolSet; } }
		protected override LNode ToLNode(Symbol value) { return F.Literal(value); }
	}
}
