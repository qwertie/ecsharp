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
	/// Styles may be used in different ways by different parsers, different
	/// printers and different people. Be careful how you rely on them; they are 
	/// intended to affect only the appearance of a node when it is printed, not 
	/// its semantics.
	/// <para/>
	/// Please note that language-specific printing styles can be denoted by 
	/// attaching special attributes recognized by the printer for that language.
	/// These attributes should have Names starting with the string "#trivia_";
	/// printers are programmed to ignore trivia attributes that they do not
	/// understand.
	[Flags]
	public enum NodeStyle : byte
	{
		/// <summary>No style flags are specified; the printer should choose a 
		/// style automatically.</summary>
		Default = 0,
		/// <summary>The node(s) should be printed as a normal expression, rather
		/// than using a special or statement notation.</summary>
		Expression = 1,
		/// <summary>The node(s) should be printed as a statement, if possible 
		/// given the context in which it is located (for example, EC# can only 
		/// switch to statement mode at certain node types such as # and #quote.)</summary>
		Statement = 2,
		/// <summary>The node should be printed with infix or suffix notation
		/// instead of prefix notation if applicable (requests `backquote notation` 
		/// in LES and EC#).</summary>
		Operator = 3,
		/// <summary>The node should be printed in prefix notation, unless it is
		/// a #::, #. or #of node, which uses a special notation (e.g. in EC# 
		/// style, Generic.List&ltint>).</summary>
		PrefixNotation = 4,
		/// <summary>The node should be printed in prefix notation regardless
		/// of the call target.</summary>
		PurePrefixNotation = 5,
		/// <summary>A language-specific special notation should be used for this
		/// node. In LES, this marker requests that the arguments to a call be
		/// broken out into separate expressions, forming a superexpression, e.g.
		/// in "x = if c a else b", which actually means "x = if(c, a, else, b)",
		/// the "if(...)" node will have this style.</summary>
		Special = 6,
		/// <summary>If s is a NodeStyle, (s & NodeStyle.BaseStyleMask) is the 
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

		/// <summary>Prefer to use the current base style recursively in child 
		/// nodes (not currently supported).</summary>
		Recursive = 64,
		
		/// <summary>User-defined meaning.</summary>
		UserFlag = 128,
	}
}
