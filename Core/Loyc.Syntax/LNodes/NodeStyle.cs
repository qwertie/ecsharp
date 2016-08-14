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
		/// <summary>Indicates that a node was parsed as an operator (infix, prefix, 
		/// suffix) or that it should be printed with operator notation if possible.</summary>
		Operator = 1,
		/// <summary>The node's immediate children (and/or the node itself) should be 
		/// printed in statement notation, if possible in the context in which it is 
		/// located.</summary>
		Statement = 2,
		/// <summary>A language-specific special notation should be used for this
		/// node. In LESv3, the parser puts this style on block call nodes (e.g. 
		/// <c>if (...) {...}</c>) and on keyword expressions (e.g. <c>#if x {...}</c>).</summary>
		Special = 3,
		/// <summary>The node should be printed in prefix notation (even if it is 
		/// not the natural notation to use). An example in EC# notation is 
		/// <c>@`'+`(X, Y)</c> instead of <c>X + Y</c>.</summary>
		PrefixNotation = 4,
		/// <summary>The node(s) should be printed as a normal expression, rather
		/// than using a special or statement notation.</summary>
		Expression = 5,
		/// <summary>The node should be printed like a data type, if the type 
		/// notation is somehow different from expression notation. (Note: in 
		/// general, one cannot expect data types to have this style).</summary>
		DataType = 6,
		/// <summary>Use an older or backward-compatible notation.</summary>
		OldStyle = 7,
		/// <summary>If s is a NodeStyle, (s &amp; NodeStyle.BaseStyleMask) is the 
		/// base style (Default, Expression, Statement, PrefixNotation, or PurePrefixNotation).</summary>
		BaseStyleMask = 7,

		/// <summary>Used for a binary (base-2) literal like 0b11111.</summary>
		BinaryLiteral = 5,
		/// <summary>Used for a hexadecimal (base-16) literal like 0x1F.</summary>
		HexLiteral = 6,
		/// <summary>Used for an octal (base-7) literal like 0o37.</summary>
		OctalLiteral = 7,
		/// <summary>Used for an EC# verbatim string literal like <c>@"foo"</c>.</summary>
		VerbatimStringLiteral = 5,
		/// <summary>Used for a triple-quoted string literal like <c>'''foo'''</c>.</summary>
		TQStringLiteral = 6,
		/// <summary>Used for a triple-double-quoted string literal like <c>"""foo"""</c>.</summary>
		TDQStringLiteral = 7,

		/// <summary>If this node has two styles in which it can be printed, this
		/// selects the second (either the less common style, or in EC#, the EC# 
		/// style for features of C# with new syntax in EC#). In EC#, it denotes 
		/// x(->int) as opposed to (int)x, and x (as Y) as opposed to (x as Y). 
		/// In C#, delegate(X) {Y;} is considered to be the alternate style for 
		/// X => Y, and it forces parens and braces as a side-effect.</summary>
		Alternate = 8,

		/// <summary>Another alternate style flag. In LES and EC#, this is used for
		/// binary-format numbers. In LES, it is used for triple-quoted strings that 
		/// use single quotes.</summary>
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
