using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.CompilerCore;
using System.Diagnostics;
using Loyc.Collections.Impl;
using Loyc.Threading;
using Loyc.Utilities;
using S = ecs.CodeSymbols;

namespace Loyc.LLParserGenerator
{
	/// <summary>This interface represents a set of terminals (and <i>only</i> a 
	/// set of terminals, unlike <see cref="TerminalPred"/> which includes actions
	/// and a Basis Node). Typical parsers and lexers only need <see cref="PGIntSet"/>.
	/// </summary>
	public interface IPGTerminalSet : ICloneable<IPGTerminalSet>
	{
		/// <summary>Merges two sets.</summary>
		/// <returns>The combination of the two sets, or null if other's type is not supported.</returns>
		IPGTerminalSet Union(IPGTerminalSet other);
		/// <summary>Computes the intersection of two sets.</summary>
		/// <returns>A set that has only items that are in both sets, or null if other's type is not supported.</returns>
		IPGTerminalSet Intersection(IPGTerminalSet other, bool subtract = false);

		bool Inverted { get; set; }
		bool ContainsEOF { get; set; }
		bool IsEmptySet { get; }
		bool ContainsEverything { get; }
		
		/// <summary>Generates a declaration for a variable that holds the set.</summary>
		/// <remarks>
		/// For example, if setName is foo, a set such as [aeiouy] 
		/// might use an external declaration such as 
		/// <code>IntSet foo = IntSet.Parse("[aeiouy]");</code>
		/// This method will not be called if <see cref="GenerateTest(Node)"/>
		/// never returns null.
		/// </remarks>
		Node GenerateSetDecl(Symbol setName);

		/// <summary>Generates code to test whether a terminal is in the set.</summary>
		/// <param name="subject">Represents the variable to be tested.</param>
		/// <param name="setName">Names an external set variable to use for the test.</param>
		/// <returns>Test expression, or null if an external declaration is needed.</returns>
		/// <remarks>
		/// At first, <see cref="LLParserGenerator"/> calls this method with 
		/// <c>setName == null</c>. If it returns null, it calls the method a
		/// second time, giving the name of an external variable in which the
		/// set is held (see <see cref="GenerateSetDecl"/>).
		/// <para/>
		/// For example, if the subject is @(la0), the test for a simple set
		/// like [a-z?] might be something like <c>@((la0 >= 'a' && 'z' >= la0)
		/// || la0 == '?')</c>. When the setName is @(foo), the test might be 
		/// <c>@(foo.Contains(la0))</c> instead.
		/// </remarks>
		Node GenerateTest(Node subject, Symbol setName);
	}
	public static class PGTerminalSet
	{
		public static IPGTerminalSet Subtract(this IPGTerminalSet @this, IPGTerminalSet other) { return @this.Intersection(other, true); }
	}

	class PGIntSet : IntSet, IPGTerminalSet
	{
		public bool IsSymbolSet { get; set; }
		public bool ContainsEOF { 
			get { return Contains(-1); }
			set { 
				if (value != ContainsEOF) {
					var eof = PGIntSet.With(-1);
					if (value)
						_ranges = Union(eof)._ranges;
					else
						_ranges = Subtract(eof)._ranges;
				}
			}
		}

		new public static PGIntSet With(params int[] members)             { return new PGIntSet(false, false, false, members); }
		new public static PGIntSet WithRanges(params int[] ranges)        { return new PGIntSet(false, false, true, ranges); }
		new public static PGIntSet Without(params int[] members)          { return new PGIntSet(false, true, false, members); }
		new public static PGIntSet WithoutRanges(params int[] ranges)     { return new PGIntSet(false, true, true, ranges); }
		new public static PGIntSet WithChars(params int[] members)        { return new PGIntSet(true, false, false, members); }
		new public static PGIntSet WithCharRanges(params int[] ranges)    { return new PGIntSet(true, false, true, ranges); }
		new public static PGIntSet WithoutChars(params int[] members)     { return new PGIntSet(true, true, false, members); }
		new public static PGIntSet WithoutCharRanges(params int[] ranges) { return new PGIntSet(true, true, true, ranges); }
		new public static PGIntSet Empty() { return new PGIntSet(false, false); }
		new public static PGIntSet All() { return new PGIntSet(false, true); }
		new public static PGIntSet Parse(string members)
		{
			int errorIndex;
			var pgr = new PGIntSet();
			if (TryParse(members, pgr, out errorIndex) == null)
				throw new FormatException(string.Format(
					"Input string could not be parsed to a PGIntSet (error at index {0})", errorIndex));
			return pgr;
		}
		new public static PGIntSet TryParse(string members)
		{
			int _;
			var pgr = new PGIntSet();
			if (TryParse(members, pgr, out _) == null)
				return null;
			return pgr;
		}
		
		public PGIntSet(bool isCharSet = false, bool inverted = false) : base(isCharSet, inverted) {}
		public PGIntSet(IntRange r, bool isCharSet = false, bool inverted = false) : base(r, isCharSet, inverted) {}
		public PGIntSet(bool isCharSet, bool inverted, params IntRange[] list) : base(isCharSet, inverted, list) {}
		protected PGIntSet(bool isCharSet, bool inverted, bool ranges, params int[] list) : base(isCharSet, inverted, ranges, list) {}

		protected override IntSet New(bool isCharSet, bool inverted, InternalList<IntRange> ranges)
		{
			return new PGIntSet(isCharSet, inverted) { _ranges = ranges };
		}
		IPGTerminalSet ICloneable<IPGTerminalSet>.Clone() { return Clone(); }
		new public PGIntSet Clone()
		{
			return new PGIntSet(IsCharSet, Inverted) { _ranges = _ranges.CloneAndTrim() };
		}

		IPGTerminalSet IPGTerminalSet.Union(IPGTerminalSet other)
		{
			var other_ = other as IntSet;
			if (other_ == null) return null;
			return Union(other_);
		}
		public PGIntSet Union(IntSet other)
		{
			return (PGIntSet)base.Union(other, true);
		}
		IPGTerminalSet IPGTerminalSet.Intersection(IPGTerminalSet other, bool subtract)
		{
			var other_ = other as IntSet;
			if (other_ == null) return null;
			return Intersection(other_, subtract);
		}
		new public PGIntSet Intersection(IntSet other, bool subtract = false)
		{
			return (PGIntSet)base.Intersection(other, subtract);
		}
		new public PGIntSet Subtract(IntSet other)
		{
			return Intersection(other, true);
		}

		static GreenFactory F = new GreenFactory(new EmptySourceFile("PGSets.cs"));
		static readonly Symbol _setName = GSymbol.Get("setName");
		static readonly Symbol _IntSet = GSymbol.Get("IntSet");
		static readonly Symbol _Parse = GSymbol.Get("Parse");
		static readonly Symbol _Id = GSymbol.Get("Id");
		static readonly Symbol _Contains = GSymbol.Get("Contains");
		static readonly GreenNode _false = F.Literal(false);
		// static IntSet setName = new IntSet(false, \inverted, false, ...);
		// static IntSet setName = #new(IntSet(false, \inverted, false, ...))
		static readonly GreenNode _symbolSetDecl = F.Attr(F.Symbol(S.Static), 
			F.Var(F.Symbol("IntSet"), F.Call(_setName, 
				F.Call(S.New, F.Call(_IntSet, _false, _false, _false)))));
		// static IntSet setName = IntSet.Parse(...)
		static readonly GreenNode _setDecl = F.Attr(F.Symbol(S.Static),
			F.Var(F.Symbol("IntSet"), F.Call(_setName, F.Call(F.Dot(_IntSet, _Parse)))));

		public Node GenerateSetDecl(Symbol setName)
		{
			Node setDecl = Node.FromGreen(IsSymbolSet ? _symbolSetDecl : _setDecl, -1);
			var var = setDecl.Args[1];
			var.Name = setName;
			var initializer = var.Args[0];

			if (IsSymbolSet) {
				var args = initializer.Args[0].Args;
				args[1].Value = F.Literal(Inverted);
				for (int i = 0; i < _ranges.Count; i++)
					for (int n = _ranges[i].Lo; n <= _ranges[i].Hi; n++)
						args.Add(Node.FromGreen(F.Dot(F.Literal(GSymbol.GetById(n)), F.Symbol(_Id))));
			} else {
				var args = initializer.Args;
				args.Add(Node.FromGreen(F.Literal(this.ToString())));
			}
			return setDecl;
		}

		public Node GenerateTest(Node subject, Symbol setName)
		{
			if (setName != null)
			{
				// setName.Contains(...)
				Node result = Node.FromGreen(F.Call(F.Dot(setName, _Contains)));
				result.Args.Add(subject);
				return result;
			}
			else
			{
				if (_ranges.Count >= 3)
				{
					for (int i = 0, checks = 0; i < _ranges.Count; i++)
						if ((checks += (_ranges[i].Lo != _ranges[i].Hi ? 2 : 1)) > 4)
							return null; // complex
				}
				GreenNode test, result = null;
				for (int i = 0; i < _ranges.Count; i++)
				{
					var r = _ranges[i];
					if (IsSymbolSet) {
						for (int id = r.Lo; id <= r.Hi; id++) {
							test = F.Call(S.Eq, subject.FrozenGreen, F.Literal(GSymbol.GetById(id)));
							AddTest(ref result, test);
						}
					} else {
						if (r.Lo == r.Hi)
							test = F.Call(S.Eq, subject.FrozenGreen, Literal(r.Lo));
						else
							test = F.Call(S.And, F.Call(S.GE, subject.FrozenGreen, Literal(r.Lo)),
							                     F.Call(S.LE, subject.FrozenGreen, Literal(r.Hi)));
						AddTest(ref result, test);
					}
				}
				return Node.FromGreen(result);
			}
		}
		private GreenNode Literal(int c)
		{
			return F.Literal(IsCharSet && (char)c == c ? (char)c : c);
		}
		private void AddTest(ref GreenNode result, GreenNode test)
		{
			if (result == null)
				result = test;
			else
				result = F.Call(S.Or, result, test);
		}
	}

	//class PGEmptySet : IPGTerminalSet
	//{
	//}
	//class PGAnyTerminal : IPGTerminalSet
	//{
	//}

	/// <summary>Represents a terminal set that matches all terminals or none of 
	/// them, and may or may not match EOF. The <see cref="LLParserGenerator"/>
	/// uses this class so that it can define empty and "all" sets without caring 
	/// what concrete type of <see cref="IPGTerminalSet"/> is used to match 
	/// terminals in the parser being generated.</summary>
	/// <remarks>This object is considered to be empty when Inverted=false, and
	/// to match anything when Inverted=true. The value of <see cref="ContainsEOF"/>
	/// inverts when you change <see cref="Inverted"/>.</remarks>
	public class TrivialTerminalSet : IPGTerminalSet
	{
		public static TrivialTerminalSet Empty() { return new TrivialTerminalSet(false); }
		public static TrivialTerminalSet All()   { return new TrivialTerminalSet(false) { Inverted = true }; }
		public static TrivialTerminalSet EOF()   { return new TrivialTerminalSet(true); }
		public static TrivialTerminalSet AllExceptEOF() { return new TrivialTerminalSet(true) { Inverted = true }; }

		public TrivialTerminalSet(bool hasEOF = false) { _hasEOF = hasEOF; }

		public IPGTerminalSet Union(IPGTerminalSet other)
		{
			if (_inverted)
			{
				return new TrivialTerminalSet() { 
					Inverted = true, 
					ContainsEOF = ContainsEOF || other.ContainsEOF
				};
			}
			else
			{
				if (ContainsEOF && !other.ContainsEOF)
				{
					other = other.Clone();
					other.ContainsEOF = true;
				}
				return other;
			}
		}
		public IPGTerminalSet Intersection(IPGTerminalSet other, bool subtract)
		{
			if (_inverted)
			{
				if (other.ContainsEOF && !ContainsEOF)
				{
					other = other.Clone();
					other.ContainsEOF = false;
				}
				return other;
			}
			else
			{
				return new TrivialTerminalSet() { 
					Inverted = false,
					ContainsEOF = ContainsEOF && other.ContainsEOF
				};
			}
		}

		bool _hasEOF, _inverted;
		public bool ContainsEOF { get { return _hasEOF ^ _inverted; } set { _hasEOF = value ^ _inverted; } }
		public bool Inverted { get { return _inverted; } set { _inverted = value; } }
		public bool IsEmptySet { get { return !_inverted && !_hasEOF; } }
		public bool ContainsEverything { get { return _inverted && !_hasEOF; } }

		static GreenFactory F = new GreenFactory(new EmptySourceFile("PGSets.cs"));
		public Node GenerateSetDecl(Symbol setName)
		{
			return Node.NewSynthetic(S.Missing, F.File);
		}
		public Node GenerateTest(Node subject, Symbol setName)
		{
			// !ContainsEOF && !Inverted: @(false)
			// !ContainsEOF && Inverted: @(subject != -1)
			// ContainsEOF && !Inverted: @(subject == -1)
			// ContainsEOF && Inverted: @(true)
			if (_hasEOF)
				return Node.FromGreen(F.Call(Inverted ? S.Neq : S.Eq, subject.FrozenGreen, F.Literal(-1)));
			else
				return Node.FromGreen(F.Literal(Inverted));
		}

		IPGTerminalSet ICloneable<IPGTerminalSet>.Clone() { return Clone(); }
		public TrivialTerminalSet Clone()
		{
			return new TrivialTerminalSet(_hasEOF) { _inverted = _inverted };
		}
	}

/*	/// <summary>Represents any single terminal, optionally including EOF.</summary>
	public class AnyTerminal : TerminalPred
	{
		public bool AllowEOF = false;
		public static AnyTerminal AnyFollowSet()
		{
			var a = new AnyTerminal() { AllowEOF = true };
			a.Next = a;
			return a;
		}

		public AnyTerminal() : base(Node.Missing) { }
		public AnyTerminal(Node basis, bool allowEOF) : base(basis) { AllowEOF = allowEOF; }
		
		public override bool ContainsEOF
		{
			get { return AllowEOF; }
		}
		public override bool CanMerge(TerminalPred r)
		{
			return r.PreAction == PreAction && r.PostAction == PostAction;
		}
		public override TerminalPred Union(TerminalPred r, bool ignoreActions = false)
		{
			if (!ignoreActions && (PreAction != r.PreAction || PostAction != r.PostAction))
				throw new InvalidOperationException("Internal error: cannot merge TerminalSets that have actions");

			if (ContainsEOF)
				return this;
			else if (r.ContainsEOF) {
				if (r is AnyTerminal)
					return r;
				else
					return new AnyTerminal(Basis, true);
			} else
				return this;
		}
		public override bool IsNullable { get { return false; } }
		public override bool IsEmptySet { get { return false; } }
		public override TerminalPred Intersection(TerminalPred r)
		{
			if (ContainsEOF)
				return r;
			else if (r.ContainsEOF)
				return r.Intersection(this); // the other set needs to be able to handle this case
			else
				return r;
		}
	}*/
}
