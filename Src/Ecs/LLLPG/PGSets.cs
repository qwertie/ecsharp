using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.CompilerCore;
using System.Diagnostics;

namespace Loyc.LLParserGenerator
{
	/// <summary>This interface represents a set of terminals (and <i>only</i> a 
	/// set of terminals, unlike <see cref="TerminalSet"/> which includes actions
	/// and a Basis Node). Typical parsers and lexers only need <see cref="PGIntSet"/>.
	/// </summary>
	interface IPGTerminalSet
	{
		IPGTerminalSet Union(IPGTerminalSet other);
		IPGTerminalSet Intersection(IPGTerminalSet other);
		IPGTerminalSet Subtract(IPGTerminalSet other);
		IPGTerminalSet Inverted { get; }
		
		/// <summary>Generates code to test whether a terminal is in the set.</summary>
		/// <param name="subject">Represents the variable to be tested.</param>
		/// <param name="nextVarName">If an external variable declaration is needed, this is the name to use</param>
		/// <param name="externalDecl">Code to insert at class level, or null if not needed.</param>
		/// <returns>Test expression.</returns>
		/// <remarks>For example, if the subject is @(la0), the code for a simple 
		/// set like [@a-z] would be something like <c>@(la0 == '@' || (la0 >= 'a' 
		/// && 'z' >= la0))</c>. If nextVarName is $foo_1, a more complex set such 
		/// as [aeiouy] might use an external declaration such as 
		/// <code>IntSet foo_1 = IntSet.WithChars('a', 'e', 'i', 'o'</code>
		/// </remarks>
		Node GenerateTest(Node subject, Symbol nextVarName, out Node externalDecl);
	}
	//class PGIntSet : IntSet, IPGTerminalSet
	//{
		
	//}
	//class PGEmptySet : IPGTerminalSet
	//{
	//}
	//class PGAnyTerminal : IPGTerminalSet
	//{
	//}

	/// <summary><see cref="TerminalSet.Empty"/> represents the empty set, which cannot match anything.</summary>
	public class EmptyTerminalSet : TerminalSet
	{
		public EmptyTerminalSet(Node basis) : base(basis) { }

		public override bool CanMerge(TerminalSet r)
		{
			Debug.Assert(PreAction == null && PostAction == null);
			return true;
		}
		public override TerminalSet Union(TerminalSet r, bool ignoreActions = false)
		{
			return r;
		}
		public override bool ContainsEOF { get { return false; } }
		public override bool IsEmptySet { get { return true; } }
		public override TerminalSet Intersection(TerminalSet other)
		{
			return this;
		}
	}

	/// <summary>Represents any single terminal, optionally including EOF.</summary>
	public class AnyTerminal : TerminalSet
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
		public override bool CanMerge(TerminalSet r)
		{
			return r.PreAction == PreAction && r.PostAction == PostAction;
		}
		public override TerminalSet Union(TerminalSet r, bool ignoreActions = false)
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
		public override TerminalSet Intersection(TerminalSet r)
		{
			if (ContainsEOF)
				return r;
			else if (r.ContainsEOF)
				return r.Intersection(this); // the other set needs to be able to handle this case
			else
				return r;
		}
	}
}
