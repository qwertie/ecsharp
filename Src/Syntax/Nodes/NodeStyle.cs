using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Syntax
{
	/// <summary>Suggests a printing style when serializing a Loyc tree to text.</summary>
	/// <remarks>See <see cref="LNode.Style"/>.
	/// <para/>
	/// A printer should not throw exceptions unless specifically requested. It 
	/// should ignore printing styles that it does not allow, rather than throwing.
	/// <para/>
	/// Please note that language-specific printing styles can be denoted by 
	/// attaching special attributes recognized by the printer for that language,
	/// e.g. the #macroAsAttribute attribute causes a statement like 
	/// <c>foo(int x = 2);</c> to be printed as <c>\[foo] int x = 2;</c>.</remarks>
	[Flags]
	public enum NodeStyle : byte
	{
		/// <summary>No style flags are specified; the printer should choose a 
		/// style automatically.</summary>
		Default = 0,
		/// <summary>The node(s) should be printed as an expression, if possible 
		/// given the context in which it is located (in EC# it is almost always 
		/// possible to print something as an expression).</summary>
		Expression = 1,
		/// <summary>The node(s) should be printed as a statement, if possible 
		/// given the context in which it is located (for example, EC# can only 
		/// switch to statement mode at certain node types such as # and #quote.)</summary>
		Statement = 2,
		/// <summary>The node(s) should be printed with infix or suffix notation
		/// instead of prefix notation if applicable (uses `backquote notation` 
		/// in EC#).</summary>
		Operator = 3,
		/// <summary>The node(s) should be printed in prefix notation, except 
		/// complex identifiers that use #. and #of nodes, which are printed in 
		/// EC# style e.g. Generic.List&ltint>.</summary>
		PrefixNotation = 4,
		/// <summary>The node(s) should be printed in prefix notation only.</summary>
		PurePrefixNotation = 5,
		/// <summary>If s is a NodeStyle, (s & NodeStyle.BaseStyleMask) is the 
		/// base style (Default, Expression, Statement, PrefixNotation, or PurePrefixNotation).</summary>
		BaseStyleMask = 7,

		/// <summary>If this node has two common styles in which it is printed, this
		/// selects the second (either the less common style, or the EC# style for
		/// features of C# with new syntax in EC#). In EC#, alternate style denotes 
		/// verbatim strings, hex numbers, x(->int) as opposed to (int)x, x (as Y)
		/// as opposed to (x as Y). delegate(X) {Y;} is considered to be the 
		/// alternate style for X => Y, and it forces parens and braces as a side-
		/// effect.</summary>
		Alternate = 8,

		// *******************************************************************
		// **** The following are not yet supported or may be redesigned. ****
		// *******************************************************************

		/// <summary>The node and its immediate children should be on a single line.</summary>
		SingleLine = 16,
		/// <summary>Each of the node's immediate children should be on separate lines.</summary>
		MultiLine = 32,
		/// <summary>Applies the NodeStyle to children recursively, except on 
		/// children that also have this flag.</summary>
		Recursive = 64,
		/// <summary>User-defined meaning.</summary>
		UserFlag = 128,
	}
}
