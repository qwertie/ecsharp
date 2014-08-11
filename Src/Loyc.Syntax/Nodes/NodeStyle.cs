using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Syntax
{
	/// <summary>Suggests a printing style when serializing a Loyc tree to text.</summary>
	/// <remarks>See <see cref="LNode.Style"/>.
	/// <para/>
	/// TODO: review, rethink.
	/// <para/>
	/// A printer should not throw exceptions unless specifically requested. It 
	/// should ignore printing styles that it does not allow, rather than throwing.
	/// <para/>
	/// Styles may be used in different ways by different parsers, different
	/// printers and different people. Be careful how you rely on them; they are 
	/// intended to affect only the appearance of a node when it is printed, not 
	/// its semantics.
	/// <para/>
	/// Please note that language-specific printing styles can be denoted by 
	/// attaching special attributes recognized by the printer for that language.
	/// These attributes should have Names starting with the string "#trivia_";
	/// printers are programmed to ignore trivia attributes that they do not
	/// understand.</remarks>
	[Flags]
	public enum NodeStyle : byte
	{
		/// <summary>No style category is specified; the printer should choose a 
		/// style automatically.</summary>
		Default = 0,
		/// <summary>The node(s) should be printed as a normal expression, rather
		/// than using a special or statement notation.</summary>
		Expression = 1,
		/// <summary>The node, or its immediate children, should be printed in
		/// statement notation, if possible in the context in which it is located.</summary>
		Statement = 2,
		/// <summary>The node should be printed with infix or suffix notation
		/// instead of prefix notation if applicable (requests `backquote notation` 
		/// in LES and EC#).</summary>
		Operator = 3,
		/// <summary>The node should be printed in prefix notation, e.g. <c>@.(X, Y)</c>
		/// instead of <c>X.Y</c>.</summary>
		PrefixNotation = 4,
		/// <summary>The node should be printed like a data type, if the type 
		/// notation is somehow different from expression notation.</summary>
		DataType = 5,
		/// <summary>A language-specific special notation should be used for this
		/// node. In LES, this marker requests that the arguments to a call be
		/// broken out into separate expressions, forming a superexpression, e.g.
		/// in "x = if c a else b", which actually means "x = if(c, a, else, b)",
		/// the "if(...)" node will have this style.</summary>
		Special = 6,
		/// <summary>Use an older or backward-compatible notation.</summary>
		OldStyle = 7,
		/// <summary>If s is a NodeStyle, (s &amp; NodeStyle.BaseStyleMask) is the 
		/// base style (Default, Expression, Statement, PrefixNotation, or PurePrefixNotation).</summary>
		BaseStyleMask = 7,

		/// <summary>If this node has two common styles in which it is printed, this
		/// selects the second (either the less common style, or the EC# style for
		/// features of C# with new syntax in EC#). In LES and EC#, alternate style
		/// denotes hex numbers. In EC#, it denotes verbatim strings, x(->int) as 
		/// opposed to (int)x, x (as Y) as opposed to (x as Y). delegate(X) {Y;} is 
		/// considered to be the alternate style for X => Y, and it forces parens 
		/// and braces as a side-effect.</summary>
		Alternate = 8,
		/// <summary>Another alternate style flag. In LES and EC#, this is used for
		/// binary-format numbers.</summary>
		Alternate2 = 16,
		
		/// <summary>Indicates that some part of a compiler has seen the node and 
		/// done something with it.</summary>
		/// <remarks>The idea behind this flag relates to compilers that allow 
		/// user-defined attributes for plug-ins that add functionality. For 
		/// example, internationalization plug-in might notice a language marker:
		/// <code>
		///    MessageBox.Show([en] "Hello, World!");
		/// </code>
		/// If an attribute is not used by any plug-in, the compiler should print 
		/// a warning that the attribute is unused. This leads to the question, how
		/// can a compiler tell if an attribute was used or not? The Handled flag
		/// is one possible mechanism; when any part of the compiler or its plug-
		/// ins use an attribute, the Handled flag should be set to disable the
		/// compiler warning.
		/// <para/>
		/// Remember that the same node can theoretically appear in multiple
		/// places in a syntax tree, which typically happens when a statement or
		/// expression is duplicated by a macro, without being changed. Remember 
		/// that when a style is changed on such a node, the change is visible at 
		/// all locations where the same node is used. However, style flags are
		/// not synchronized between copies of a node.
		/// </remarks>
		Handled = 128,
	}
}
