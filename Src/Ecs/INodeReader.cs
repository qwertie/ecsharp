using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Essentials;
using Loyc.Collections;

namespace Loyc.CompilerCore
{
	/// <summary>Read-only interface to one of the two kinds of Loyc nodes: red 
	/// and green.</summary>
	/// <remarks>
	/// INodeReader represents the common operations implemented by red and green 
	/// trees, but most users should access Loyc syntax trees through the 
	/// <see cref="Node"/> class. The meaning of the terms "red tree" and "green 
	/// tree" are explained below.
	/// <para/>
	/// You may find this interface useful in rare cases, when
	/// <para/>
	/// 1. You need to examine a syntax tree but not modify it, and
	/// 2. Your code takes both green and red nodes as input, for some reason, and
	/// 3. You don't need to know the absolute position or source file of the nodes.
	/// <para/>
	/// Now I will describe the implementation details of the Loyc red-green tree.
	/// The <see cref="GreenNode"/> class represents the green tree, while the 
	/// normal <see cref="Node"/> class represents the red tree. Please see the
	/// documentation of <see cref="Node"/> for a higher-level overview.
	/// <para/>
	/// The Loyc syntax tree design is a fusion of ideas from two sources:
	/// (1) the red-green tree used by Microsoft Roslyn (their compiler-as-a-
	///     service library), and
	/// (2) the fast-cloning A-List, a data structure of my own design.
	/// <para/>
	/// Loyc trees are mutable but freezable and support fast cloning under 
	/// certain conditions; this design attempts to enjoy the advantages of 
	/// mutable data structures (fast incremental updates) and immutable data 
	/// structures (safe sharing between non-cooperating modules and between 
	/// threads) in a single data type--actually a family of data types, but
	/// end users only interact with a single data type (<see cref="Node"/>)
	/// most of the time.
	/// <para/>
	/// It is a bold experiment; I cannot prove even to myself that this design 
	/// will be more efficient in the real world than a purely mutable or 
	/// immutable tree design, but I suspect it will be. That'll have to do, 
	/// since there is no way to test it in real-world conditions without (A) 
	/// optimizing it and (B) giving it a realistic workload by writing a 
	/// compiler based on it. The acid test will be IDE integration, which is a 
	/// long way off. Whether faster or not, the design is at least very 
	/// flexible.
	/// <para/>
	/// The design consists of a family of "green tree nodes" and a single type 
	/// of "red tree" node. The green tree is the internal syntax tree produced 
	/// by the parser, while the red tree is a wrapper around the green tree; it 
	/// provides the public interface with which programmers usually interact. 
	/// There are multiple reasons for using two parallel syntax trees like this,
	/// reasons that are somewhat difficult to explain, but I'll attempt it. You 
	/// may also find Eric Lippert's explanation of the Roslyn trees instructive,
	/// see http://blogs.msdn.com/b/ericlippert/archive/2012/06/08/persistence-facades-and-roslyn-s-red-green-trees.aspx
	/// <para/>
	/// The most important issue is incremental editing in an IDE, which should
	/// be able to analyze the code in real time. Ideally, when a user edits source 
	/// code, only the parts affected by the user's changes should be reparsed. 
	/// Incremental parsing is not likely to be supported initially, but I felt it 
	/// was important to design the tree for incremental changes right from the 
	/// start.
	/// <para/>
	/// The first problem is that the character offset of a node within a source 
	/// file changes whenever the code above it changes. A partial solution would 
	/// be to store the column and line number separately, which means that when 
	/// the user is editing a single line, we only have to reparse that one line 
	/// (until we start to deal with multi-line tokens such as @verbatim strings 
	/// and multi-line comments, anyway). Then when the user adds or removes lines,
	/// we could scan the syntax tree to adjust line numbers. I'm sure that would
	/// be fast enough, but then you hit the second problem.
	/// <para/>
	/// The second problem is that simply parsing the source code is not enough; it
	/// must be analyzed too. The user expects to be able to write "this." and 
	/// immediately get a popup list of members, including any member whose name
	/// he just changed one second ago. In EC# this problem is very serious, 
	/// because EC# code can invoke user-defined macros. Three facts about macros:
	/// (1) Macros can transform the syntax tree any way they want.
	/// (2) Macros can make decisions about which types and methods will exist in 
	///     the program, and what their names will be. Therefore, an IDE must run
	///     them in order to provide good code completion hints.
	/// (3) Macros can take forever to run. Literally. They can have infinite loops.
	///     They can also use unlimited memory, but I digress.
	/// <para/>
	/// Fact (1) is the most relevant here. How can we reparse incrementally if the
	/// macros have arbitrarily changed the syntax tree after it was parsed? This 
	/// implies we need either immutability or fast cloning:
	/// <para/>
	/// (A) If the syntax tree is immutable, the macros will have to construct a 
	///     new trees. Incremental parsing is still possible since the original
	///     tree never changes. However, if the tree is immutable, how can the 
	///     parser update the character offsets or line numbers stored in the 
	///     nodes?
	/// (B) If cloning is fast, we can afford to let the macros edit a copy of the 
	///     tree instead of the original; the incremental parser will edit the 
	///     original. Also, for the purpose of syntax highlighting we can reparse 
	///     the file without waiting for running macros to finish, since they are
	///     editing independent copies.
	/// <para/>
	/// An additional issue is that it is useful (although not necessarily crucial)
	/// to be able to learn the parent of a node. Since Project Roslyn chose to use 
	/// an immutable tree, they had to figure out how to update all the character 
	/// offsets and line numbers. They also wanted to be able to report the parent 
	/// node. Their solution was a pair of trees called "green" and "red", both 
	/// immutable. As Eric Lippert explains:
	/// <blockquote>
	///   The "green" tree is immutable, persistent, has no parent references, is 
	///   built "bottom-up", and every node tracks its width but not its absolute 
	///   position. When an edit happens we rebuild only the portions of the green 
	///   tree that were affected by the edit, which is typically about O(log n) of 
	///   the total parse nodes in the tree.
	///   <br/><br/>
	///   The "red" tree is an immutable facade that is built around the green tree;
	///   it is built "top-down" on demand and thrown away on every edit. It computes
	///   parent references by manufacturing them on demand as you descend through 
	///   the tree from the top. It manufactures absolute positions by computing them 
	///   from the widths, again, as you descend.
	/// </blockquote>
	/// The Loyc design is comparable, except that the red and green trees can both
	/// be mutable. At any time, red and/or green subtrees can be frozen, 
	/// preventing further edits, which permits safe sharing between macros, 
	/// analysis and incremental parsing.
	/// <para/>
	/// Don't worry, having two syntax trees does not use as much space
	/// as you might imagine; trust me, I habitually optimize my data structures 
	/// for memory usage because I have hacked DOS programs, Palm Pilots and 
	/// Windows CE in the past, and I dream of using a full-featured IDE on a 
	/// Raspberri Pi, which is less powerful than most smartphones. The main 
	/// memory-saving measure is the flyweight pattern (see
	/// http://en.wikipedia.org/wiki/Flyweight_pattern). All frozen green nodes 
	/// that represent terminals, such as "Console", "WriteLine" and "=", are 
	/// flyweights that can be cached and re-used in different parts of the tree.
	/// Thus, if your program refers to "double" 50 times in a source file and "0.0" 
	/// is used 10 times, there will only be a single green tree node for each of 
	/// those. The red tree will have 100 separate instances of "double" and 20 
	/// separate "0.0"s, but only after a complete analysis of the file; the entire 
	/// red tree is not rebuilt (at least not right away) every time you press a 
	/// key.
	/// <para/>
	/// Only the red tree knows the absolute position of a node, and the line and
	/// column numbers are computed only upon request. A green node tracks only 
	/// its width in the source file, relative offsets of its children, and a 
	/// reference to the source file from which the node was built*.
	/// <para/>
	/// * I resisted including the source file reference for a long time, but 
	///   since macros splice together nodes from different files, this was the 
	///   only efficient approach I could think of, given that the tree has both 
	///   mutable and frozen modes of operation. It is not sufficient to store the 
	///   source file reference in the red nodes, because red children are 
	///   discarded when the tree is frozen. It is inconvenient and not time-
	///   efficient to store the source file reference in an attribute, because 
	///   red nodes would have to look up the source file reference during each 
	///   detach or attach operation (in order not to lose track of the source
	///   file), which may be slow if the reference is attached as an attribute.
	/// <para/>
	/// The root node of a source file (e.g. an #ecs_root node) has a #pos node 
	/// attached to it as a pseudo-attribute, which indicates the filename and
	/// absolute position (zero) at the beginning of the file; positions are 
	/// computed on-demand in red nodes by scanning up the tree until a #pos node 
	/// is found. #pos nodes don't just appear at the roots; they also appear 
	/// whenever code substitution has occurred, to deal with the fact that you 
	/// can mix syntax trees from different places (even different source files).
	/// <para/>
	/// <b>General facts about Loyc trees:</b>
	/// <para/>
	/// - Green trees can be built (A) top-down by creating a mutable green node,
	///   adding children to it and then adding children to the children, or
	///   (B) bottom-up by constructing a node and specifying its children at that
	///   time, then constructing parent nodes in the same way. Green trees can be 
	///   constructed frozen or mutable.
	/// <para/>
	/// - Red trees are built incrementally from the top and are simply wrappers
	///   around green trees, so there is a green node for every red node. There
	///   are two types of red node, mutable and immutable. Red nodes can be rooted
	///   at an entire source file, but when you clone a red node, the clone has
	///   no parent; therefore, a cloned node is a lobotomized root node.
	/// <para/>
	/// - When you Clone() a red or green node, the clone is always mutable.
	/// <para/>
	/// <b>Storage space:</b>
	/// <para/>
	/// Red nodes have
	/// - A reference to their green node
	/// - A reference to their parent node
	/// - If they are mutable, a list of the red nodes wrapping their children
	/// <para/>
	/// Green nodes have
	/// - A width in the original source code, measured in UTF-16 characters. 
	///   For example, the (#+) node for foo + 10 has a width of 8.
	/// - A Head node
	/// - A Name symbol (always equals Head.Name)
	/// - A Value (usually a boxed value of a literal, e.g. (object)123)
	/// - Two sublists of children (arguments and attributes), There is a 
	///   relative offset integer associated with each child.
	/// <para/>
	/// Atoms (literals and symbols) and short calls (0 to 2 arguments) that 
	/// are constructed frozen consume less memory by omitting the head node, 
	/// the list of children, the value, or all three.
	/// <para/>
	/// <b>Position information:</b>
	/// <para/>
	/// Neither red nor green nodes keep track of their source file or their 
	/// absolute position. Absolute positions and source file information is 
	/// strategically placed with special #pos pseudo-attributes. The Value of a 
	/// #pos node points to an object that specifies the source file and the 
	/// absolute position of that particular node.
	/// <para/>
	/// Although the green tree contains all position information, green nodes
	/// themselves cannot make sense of this information because parent nodes 
	/// are required to reconstruct the actual location in the source code.
	/// Therefore, exceptions thrown by green nodes have no position information,
	/// but should offer summary text that summarizes the content of the node.
	/// <para/>
	/// To understand how positions are represented, it may help to see an 
	/// example. A source file with the following contents:
	/// <pre>
	/// // ArgCount.ecs
	/// void Main(string[] args)
	/// {
	///     // Print out the number of arguments we received.
	///		Console.WriteLine("There are {0} arguments.", args.Length);
	/// }
	/// </pre>
	/// Becomes the following syntax tree:
	/// <pre>
	/// [#pos] #ecs_root
	/// (
	///		#def(void, wMain, #(#var(#[](string), args)), #(
	///			#.(Console,WriteLine)("There are {0} arguments, #.(args, Length))
	///		))
	///	)
	/// </pre>
	/// Note that the comments are not part of the tree. The Value property of 
	/// the #pos attribute (which is not representable in prefix notation) points 
	/// to a **TODO** object that contains an absolute position of zero and a file 
	/// name. The object also has a table of trivia, including the comments.
	/// Trivia refers to things that should not affect a program's behavior, such
	/// as whitespace and comments.
	/// <para/>
	/// Given the following character positions of tokens in the file:
	/// <pre>
	/// 0              14
	/// // ArgCount.ecs
	/// 15   20   25    31 34   39
	/// void Main(string[] args)
	/// 40
	/// {
	/// 42  43                                               92
	///     // Print out the number of arguments we received.
	///	93  94      102       112                         140  145     153
	///     Console.WriteLine("There are {0} arguments.", args.Length);
	/// 154
	/// }
	/// </pre>
	/// Each node in the tree has a width, which reflects only the node and none 
	/// of the trivia (spaces or comments) surrounding it. For example, the width
	/// of "args" is 4, the width of Console.WriteLine is 17 (111-94), and the
	/// width of the entire WriteLine statement is 153-94=59.
	/// <para/>
	/// Associated with each child is a relative position, and this position is
	/// stored in the parent, not in the child (because the child may be shared
	/// between many parents). For example,
	/// - the head node #. of Console.WriteLine has a relative position of 7
	/// - the string passed to WriteLine has a relative position of 112-94=18
	/// - the statement itself has a relative position of 94-40=54
	/// - the braces (the # node) have a relative position of 40-15=25
	/// - the method has a relative position of 15-0=15.
	/// <para/>
	/// The absolute positions are erased after parsing, but we can find the 
	/// absolute position of the format string by adding its relative position 
	/// (18) to the relative positions of its parents (54, 25, 15) for a total 
	/// of 112. Obviously, this operation requires a red tree with a #pos
	/// attribute somewhere up the tree.
	/// <para/>
	/// Width values and #pos attributes always reflect the original source 
	/// code. Relative positions are not affected by adding or removing 
	/// siblings, and width values are not affected by adding or removing 
	/// children.
	/// <para/>
	/// A synthetic node (one not created from source code) has a width of 
	/// -1, and a transplanted node (one which has been moved from its original
	/// location in the tree) may be inserted with relative position that is 
	/// negative or beyond the width of its parent. For example, imagine a 
	/// macro that changes the precedence of ">>" to be higher than "+". After
	/// all, the precedence of ">>" was badly chosen; it's basically a divide 
	/// operation so it deserves higher precedence.
	/// <para/>
	/// Given an input of "x+y >> z", it is parsed to the syntax tree
	/// <pre>    #+(x, #>>(y, z))    </pre>
	/// The relative offsets in this tree are 0 for x, 2 for y and 7 for z. The
	/// macro could rearrange this to
	/// <pre>    #>>(+(x, y), z)    </pre>
	/// But the widths of the operators need not actually change (although the
	/// macro could reconstruct the operators so that it does change). Thus #+ 
	/// could still have width 8 and >> could still have width 6. In that case,
	/// the macro could legally place + inside >> using a relative offset of -2.
	/// If >> keeps the same relative offset it had originally, the math works
	/// out and the absolute positions of all five tokens can be computed 
	/// correctly.
	/// <para/>
	/// When transplanted and synthetic nodes are placed in the green tree with 
	/// a relative offset of int.MinValue, it tells the red tree that the 
	/// positions of all child nodes should be considered unknown. However, the 
	/// location of the parent or relative sibling can still be reported. For
	/// example, if the macro places the #>> node was placed in its parent with 
	/// a relative offset of int.MinValue, then the positions of all the 
	/// children are considered unknown, and any error messages for these nodes
	/// will report a slightly incorrect position somewhere before the 
	/// beginning of the expression.
	/// <para/>
	/// <b>Properties of Loyc trees:</b>
	///   <para/>
	/// - Null children are never allowed.
	///   <para/>
	/// - When you request a child of a red node, a new red child is created if 
	///   the node does not already hold a reference to that child. Red trees may 
	///   be rooted at the beginning of a source file, or they may be rooted at 
	///   any point where they are (A) cloned from a red node or (B) fabricated 
	///   from a green node.
	///   <para/>
	/// - A mutable red node holds references to all its child nodes, but a
	///   frozen red node does not. Therefore, a frozen red tree is more efficient 
	///   to read from (assuming fast allocation and gen-0 garbage collection), 
	///   while a mutable red tree is more efficient to modify incrementally.
	///   <para/>
	/// - Cloning a frozen red subtree is an O(1) operation; the node that you 
	///   asked to clone is duplicated and the clone is mutable, even though the
	///   corresponding green subtree is not. The clone is given no parent node.
	///   As you modify the clone, duplicate green nodes are created as necessary.
	///   <para/>
	/// - To clone a mutable red subtree, we
	///   (A) traverse all existing red children of the node to be cloned, 
	///       recursively. For each child, we freeze the corresponding green node.
	///       We can ignore children of red nodes when the green node is already
	///       frozen.
	///   (B) Duplicate and the requested red node, just as for the immutable case.
	///   <para/>
	/// - Because a cloned red node loses access to its parent, the cloned tree 
	///   also loses access to source code position information. The position must 
	///   be specified manually when splicing the node somewhere else in the tree.
	///   <para/>
	/// - When freezing a red node, all existing red children are marked immutable
	///   and green node for each red node is marked read-only at the same time.
	///   Then each node's list of red children is discarded.
	/// <para/>
	/// - An invariant is that if a red node is frozen, the green node is frozen 
	///   too. However, all other combinations are possible: mutable red + mutable
	///   green, frozen red + frozen green, or mutable red + frozen green.
	/// <para/>
	/// - When a green node is frozen, all its children are considered frozen, but 
	///   the children are not immediately marked as frozen! Rather, frozen nodes 
	///   freeze their children as they are returned by indexers and enumerators.
	///   This behavior ensures that freezing is an O(1) operation. Marking the 
	///   children later should be less costly than doing it up front, because 
	///   (1) it doesn't matter if there is a cache miss: since the child was 
	///       requested, the caller is intending to access the child, which 
	///       probably would have caused a cache miss anyway.
	///   (2) it's possible that no one will ever access the subtree after it is 
	///       frozen; in which case marking every node is a waste of time.
	///   To detect bugs, children are marked frozen immediately in debug builds.
	///   <para/>
	/// - Upon freezing a red node, all red children and their corresponding green
	///   nodes are immediately frozen, not delayed as for green nodes. Presumably,
	///   anyone could be holding a reference to any of these red nodes, so if we
	///   failed to freeze them immediately, they could be modified after the sub-
	///   tree was supposedly frozen. Green nodes are not given the same protection
	///   because they are meant for internal use by parsers and other low-level 
	///   code; this code is aware that when it freezes or clones a parent node, 
	///   it should be careful not to mutate any child references that it has 
	///   access to.
	/// </remarks>
	public interface INodeReader
	{
		INodeReader Head { get; }    // Returns the head node, which represents the node name
		Symbol Name { get; }         // Name of the node. Always equals Head.Name. Name cannot be empty ("")
		Symbol Kind { get; }         // #callkind for a call, otherwise same as Name
		object Value { get; }        // Value of the literal, or NonliteralValue.Value if the node does not represent a literal 
		bool IsSynthetic { get; }    // true if the node was not created from source code
		bool IsCall { get; }         // true if node has an argument list, even for things like method and variable declarations that are not method calls.
		bool IsLiteral { get; }      // true if the node represents a literal value
		bool HasSimpleHead { get; }  // true if the head is either null, or has no arguments and no head of its own
		bool IsSimpleSymbol { get; } // true if !IsCall && !IsLiteral && Head == null
		bool IsKeyword { get; }      // true if this node is a non-literal whose Name starts with '#'
		bool IsIdent { get; }        // true if this node is a non-literal whose Name does not start with '#'
		int SourceWidth { get; }     // Returns the width of the original source code, or -1 if the node is synthetic
		                             // If some children have been replaced with synthetics, the original width is still returned.
		int ArgCount { get; }        // Returns the number of arguments in the call. 0 means either no args, or the node is not a call
		int AttrCount { get; }       // Returns the number of attributes attached to the node, or 0 if none
		IListSource<INodeReader> Args { get; }  // Returns the argument list (never null)
		IListSource<INodeReader> Attrs { get; } // Returns the attribute list (never null)
		INodeReader TryGetArg(int index);
		INodeReader TryGetAttr(int index);
		string Print(NodeStyle style = NodeStyle.Statement, string indentString = "\t", string lineSeparator = "\n");
		bool IsFrozen { get; }       // true if the node is read-only or is a red node
		NodeStyle Style { get; }
		NodeStyle BaseStyle { get; }
	}

	/// <summary>Extension methods to help you use Loyc nodes.</summary>
	/// <seealso cref="INodeReader"/>
	public static class NodeReader
	{
		/// <summary>Returns all Args, all Attrs and the head (if different from 'this')</summary>
		public static IEnumerable<INodeReader> AllChildren(this INodeReader self)
		{
			foreach (var arg in self.Args)
				yield return arg;
			if (self.Head != self)
				yield return self.Head;
			foreach (var attr in self.Attrs)
				yield return attr;
		}
		/// <summary>Print out node as a string in mostly-prefix format</summary>
		public static string FullText(this INodeReader self)
		{
			return self.Name.Name+" TODO" ;
			//return new NodePrinter(this, 0).PrintStmts(false, false).Result();
		}
		public static bool Calls(this INodeReader self, Symbol name, int argCount)
		{
			return self.Name == name && self.ArgCount == argCount && self.HasSimpleHead;
		}
		public static bool Calls(this INodeReader self, Symbol name)
		{
			return self.Name == name && self.IsCall && self.HasSimpleHead;
		}
		public static bool CallsMin(this INodeReader self, Symbol name, int argCount)
		{
			return self.Name == name && self.ArgCount >= argCount && self.HasSimpleHead;
		}

		//public static bool CallsWPAIH(this INodeReader self, Symbol name)
		//{
		//    return self.Calls(name) && self.HasSimpleHeadWithoutPAttrs();
		//}
		//public static bool CallsMinWPAIH(this INodeReader self, Symbol name, int argCount)
		//{
		//    return self.CallsMin(name, argCount) && self.HasSimpleHeadWithoutPAttrs();
		//}
		//public static bool CallsWPAIH(this INodeReader self, Symbol name, int argCount)
		//{
		//    return self.Calls(name, argCount) && self.HasSimpleHeadWithoutPAttrs();
		//}

		public static int IndexOf(this IListSource<INodeReader> self, Symbol name)
		{
			for (int i = 0; i < self.Count; i++)
				if (self[i].Name == name)
					return i;
			return -1;
		}
		public static INodeReader Find(this IListSource<INodeReader> self, Symbol name)
		{
			INodeReader n;
			for (int i = 0; i < self.Count; i++)
				if ((n = self[i]).Name == name)
					return n;
			return null;
		}
		public static bool HasAttrs(this INodeReader self) { return self.AttrCount != 0; }
		public static bool HasSimpleHeadWithoutPAttrs(this INodeReader self)
		{
			var h = self.Head; 
			return h == null || (h.Head == null && !h.IsCall && !HasPAttrs(h));
		}
		public static bool IsSimpleSymbol(this INodeReader self, Symbol name)
		{
			return self.IsSimpleSymbol && self.Name == name;
		}
		public static bool IsSimpleSymbolWithoutPAttrs(this INodeReader self)
		{
			return self.IsSimpleSymbol && !HasPAttrs(self);
		}
		public static bool IsSimpleSymbolWithoutPAttrs(this INodeReader self, Symbol name)
		{
			return self.IsSimpleSymbolWithoutPAttrs() && self.Name == name;
		}

		public static bool IsPrintableAttr(this INodeReader self)
		{
			var name = self.Name.Name;
			return !name.StartsWith("#style_");
		}
		public static bool HasPAttrs(this INodeReader self)
		{
			for (int i = 0, c = self.AttrCount; i < c; i++)
				if (IsPrintableAttr(self.Attrs[i]))
					return true;
			return false;
		}

		public static bool IsParenthesizedExpr(this INodeReader self)
		{
			return !self.IsCall && self.Head != null;
		}

		public static INodeReader TryGetAttr(this INodeReader self, Symbol name)
		{
			for (int i = 0, c = self.AttrCount; i < c; i++) {
				INodeReader attr;
				if ((attr=self.TryGetAttr(i)).Name == name)
					return attr;
			}
			return null;
		}
	}
}
