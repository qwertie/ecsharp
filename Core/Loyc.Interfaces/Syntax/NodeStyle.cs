using Loyc.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
	/// These attributes should have Names starting with the % character;
	/// printers are programmed to ignore trivia attributes that they do not
	/// understand.</remarks>
	[Flags]
	public enum NodeStyle : byte
	{
		/// <summary>No style category is specified; the printer should choose a 
		/// style automatically.</summary>
		Default = 0,
		/// <summary>Indicates that a node was parsed as an operator (infix, prefix, 
		/// suffix, or other operator), or that it should be printed with operator 
		/// notation if possible.</summary>
		Operator = 1,
		[Obsolete("This was renamed to StatementBlock")] Statement = StatementBlock,
		/// <summary>The node's immediate children (and/or the node itself) should be 
		/// printed in statement notation, if possible in the context in which it is 
		/// located.</summary>
		/// <remarks>Used to mark braced blocks. In LES, marks a call in which ';'
		/// is used as the argument separator.</remarks>
		StatementBlock = 2,
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
		/// <remarks>In EC#, braced initializer blocks have this style. The EC# 
		/// node printer will refuse to print a node with this style as a statement.</remarks>
		Expression = 5,
		/// <summary>Unassigned.</summary>
		Reserved = 6,
		/// <summary>Use an older or backward-compatible notation.</summary>
		/// <remarks>In EC#: prints lambda as delegate; forces old cast notation in EC#.</remarks>
		OldStyle = 7,
		/// <summary>If s is a NodeStyle, (s &amp; NodeStyle.BaseStyleMask) is the 
		/// base style (Default, Operator, Statement, Special, PrefixNotation, Expression or OldStyle).</summary>
		BaseStyleMask = 7,

		/// <summary>Indicates that an identifier was marked in the standard way 
		/// used to indicate that it contained special characters or matched a 
		/// keyword (e.g. @int in C#)</summary>
		/// <remarks>Indicates the presence of the marking (e.g. @ sigil in C#) 
		/// regardless of whether the marking is necessary. Node printers must
		/// ensure their output is valid even when this style is not present.</remarks>
		VerbatimId = 4,
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
		/// selects the second (the less common style, or less-well-supported style).
		/// In EC#, it denotes x(->int) as opposed to (int)x, and x (as Y) as opposed 
		/// to (x as Y). In C#, delegate(X) {Y;} is considered to be the alternate 
		/// style for X => Y; it forces parens and braces as a side-effect.</summary>
		Alternate = 16,
		
		/// <summary>Reserved for use by specific compilers/languages.</summary>
		InternalFlag = 32,

		/// <summary>Indicates that the there is no comment or newline trivia associated
		/// with the children of this node, and therefore when printing this node,
		/// automatic newlines can be suppressed.</summary>
		OneLiner = 64,

		/// <summary>Indicates that some part of a compiler, or a macro, has seen 
		/// the node and done something with it.</summary>
		/// <remarks>The motivation for this flag relates to compilers that allow 
		/// user-defined attributes for plug-ins or macros that add functionality. 
		/// For example, internationalization plug-in might notice a language marker:
		/// <code>
		///    MessageBox.Show([en] "Hello, World!");
		/// </code>
		/// If an attribute is not used by any plug-in, the compiler should print 
		/// a warning that the attribute is unused. This leads to the question, how
		/// can a compiler tell if an attribute was ever used? The Handled flag
		/// is one possible mechanism; when any part of the compiler or its plug-
		/// ins use an attribute, the Handled flag could be set to disable the
		/// compiler warning.
		/// <para/>
		/// Remember that the same node can theoretically appear in multiple
		/// places in a syntax tree, which typically happens when a statement or
		/// expression is duplicated by a macro, without being changed. When a 
		/// style is changed on such a node, the change is visible at all locations 
		/// where the same node is used. However, style flags are not synchronized 
		/// between copies of a node.
		/// </remarks>
		Handled = 128,
	}
}
