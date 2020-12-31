using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Syntax.Impl
{
	/// <summary>A fluent interface for classes that help you print syntax trees (<see cref="ILNode"/>s).</summary>
	/// <typeparam name="Self">The return type of methods in this interface.</typeparam>
	public interface ILNodePrinterHelper<out Self> : IDisposable
		where Self : ILNodePrinterHelper<Self>
	{
		/// <summary>Appends a character to the output stream or StringBuilder.</summary>
		/// <remarks>Do not call <c>Write('\n')</c>; call <see cref="Newline()"/> instead.</remarks>
		Self Write(char c);
		/// <summary>Appends a string to the output stream or StringBuilder.</summary>
		/// <remarks>The string should not include newlines (if it might, call 
		/// <see cref="LNodePrinterHelperExt.WriteSmartly"/> instead).</remarks>
		Self Write(string s);
		/// <summary>Appends a string to the output stream or StringBuilder.</summary>
		/// <remarks>The string should not include newlines (if it might, call 
		/// <see cref="LNodePrinterHelperExt.WriteSmartly"/> instead).</remarks>
		Self Write(UString s);
		/// <summary>Writes a space character unless the last character written was a space.</summary>
		Self Space();
		/// <summary>Appends a newline, with indentation afterward according to the 
		/// current indentation level.</summary>
		/// <param name="deferIndent">Requests that the indentation after the newline not
		/// be printed until <see cref="Write"/> or <see cref="FlushIndent"/> is called.
		/// This option is provided because you may know that you want to print a newline
		/// before deciding what indent is needed.</param>
		Self Newline(bool deferIndent = false);
		/// <summary>Writes the pending indent, if applicable. This allows you to call 
		/// <see cref="Indent"/> or <see cref="Dedent"/> afterward while making sure the 
		/// indentation on the current line is unaffected.</summary>
		/// <remarks>This method has no effect if indentation has already been written, 
		/// e.g. right after calling Newline(false) or Write(char). If a newline is pending
		/// as a result of a call to <see cref="NewlineIsRequiredHere"/>, it is implementation-
		/// defined whether that additional newline (and its indent) is written. 
		/// <see cref="LNodePrinterHelper"/> write a newline when this method is called,
		/// but it sets a special internal flag so that a call sequence such as
		///   <code>NewlineIsRequiredHere().FlushIndent().NewlineIsRequiredHere().Newline()</code>
		/// writes only a single newline.
		/// </remarks>
		Self FlushIndent();
		/// <summary>Requests that a newline be written at this location. If this method is 
		/// called multiple times at the same location, or if <see cref="Newline(int)"/> is 
		/// called immediately afterward, only a single newline is written.</summary>
		/// <remarks>
		/// This method does not officially write a newline; if you call this method followed 
		/// by <see cref="Dedent"/>, for example, the newline that is eventually written will 
		/// be followed by a lower amount of indentation.
		/// </remarks>
		Self NewlineIsRequiredHere();
		/// <summary>Informs the helper that the printer is starting to write the specified 
		/// node. The printer must call <see cref="EndNode"/> when it is done. This 
		/// information may be used to record range information (to understand why this is
		/// useful, please read about <see cref="LNodeRangeMapper"/>)</summary>
		/// <param name="node">The node that begins here.</param>
		Self BeginNode(ILNode node);
		/// <summary>Informs the helper that the printer is done writing the most recently
		/// started node. The helper may save the range of the node, e.g. by calling 
		/// <see cref="ILNodePrinterOptions.SaveRange"/>.</summary>
		/// <param name="abort">Indicates that the range of this node should not be saved.</param>
		Self EndNode();
		/// <summary>Combines the <see cref="BeginNode"/> and <see cref="Indent"/> operations.</summary>
		/// <param name="indentHint">Typically a member of <see cref="PrinterIndentHint"/> 
		/// that influences how indentation is performed</param>
		Self BeginNode(ILNode node, Symbol indentHint);
		/// <summary>Combines the <see cref="EndNode"/> and <see cref="Dedent"/> operations.</summary>
		Self EndNode(Symbol indentHint);
		/// <summary>Increases the current indent level.</summary>
		/// <param name="hint">Information associated with the indent, typically a member 
		/// of <see cref="PrinterIndentHint"/> that influences how indentation is performed.
		/// You can use null for standard indentation.</param>
		/// <remarks>
		/// This interface does not define what string is used for an indent; 
		/// typically it's a tab or 2 to 4 spaces.
		/// <para/>
		/// The standard kind of indentation expresses block boundaries, e.g. statements are 
		/// indented between curly braces (in languages in the C family).
		/// <para/>
		/// When printing labels (targets of goto statements), a typical style is that labels 
		/// are printed with less indentation than a normal statement. The 
		/// <see cref="PrinterIndentHint.Label"/> hint indicates that a label is about to
		/// be printed.
		/// <para/>
		/// Another interesting case is a mid-expression break. Consider a statement 
		/// <c>x = 2 * (1 + Method(@`%newline` parameter1, parameter2))</c>
		/// with a newline before parameter1. It is the printer's responsibility to notice the 
		/// trivia and call <see cref="Newline"/>, but in this case the newline should 
		/// include some extra indentation to indicate that a new statement does not begin
		/// here. The <see cref="PrinterIndentHint.Subexpression"/> and <see 
		/// cref="PrinterIndentHint.Brackets"/> hints request this extra indentation; both of 
		/// these flags indicate a subexpression but the latter is meant to imply a kind of 
		/// subexpression that begins with round or square brackets `() []`.
		/// <para/>
		/// <see cref="LNodePrinterHelper"/> has an indent stack which it uses to make a
		/// smart decision about indentation. When printing the example above, if your code
		/// calls <see cref="Indent(Symbol)"/> with an appropriate hint as it begins each node, 
		/// the helper will know that four subexpressions have begun but not ended at the time 
		/// <see cref="Newline"/> is called:
		/// <ul>
		/// <li><c>2 * (1 + Method(@`%newline` parameter1, parameter2))</c></li>
		/// <li><c>(1 + Method(@`%newline` parameter1, parameter2))</c></li>
		/// <li><c>Method(@`%newline` parameter1, parameter2))</c></li>
		/// <li><c>@`%newline` parameter1</c></li>
		/// </ul>
		/// <see cref="LNodePrinterHelper"/> counts the number of "indents" of type
		/// <see cref="PrinterIndentHint.Brackets"/> and then prints indentation based on
		/// properties that were given to its constructor 
		/// (<see cref="LNodePrinterHelper.BracketIndentString"/> and
		/// <see cref="LNodePrinterHelper.MaxBracketIndents"/>). It treats the 
		/// <see cref="PrinterIndentHint.Subexpression"/> hint differently; it ignores the
		/// number of Subexpressions on the indent stack and only pays attention to whether 
		/// there are zero of them, or more than zero. Any number of Subexpression indents 
		/// larger than zero causes a single bracket indent to be printed. However, if 
		/// there are also bracket indents on the stack, then the Subexpression indents 
		/// have no effect.
		/// </remarks>
		Self Indent(Symbol hint = null);
		/// <summary>Decreases the current indent level.</summary>
		/// <param name="mode">If the hint is not null, the writer may be able to check 
		/// that it was the same hint that was passed to <see cref="Indent"/> and
		/// throw an exception if not.</param>
		/// <exception cref="ArgumentException">Hint provided doesn't match Indent hint</exception>
		Self Dedent(Symbol hint = null);
		/// <summary>Returns true iff nothing has been written since the last call to 
		/// <see cref="Newline"/> or <see cref="NewlineIsRequiredHere"/>.</summary>
		bool IsAtStartOfLine { get; }
		/// <summary>Gets the character most recently written to the stream, or 
		/// '\uFFFF' if no characters have been written.</summary>
		char LastCharWritten { get; }
	}

	/// <summary>A version of <see cref="ILNodePrinterHelper{Self}"/> without a type parameter.</summary>
	public interface ILNodePrinterHelper : ILNodePrinterHelper<ILNodePrinterHelper> { }

	/// <summary>Values used with <see cref="ILNodePrinterHelper{Self}.Newline(Symbol)"/>.</summary>
	public static class PrinterIndentHint
	{
		/// <summary>Requests normal (statement) indentation (this is the default)</summary>
		public static Symbol Normal = (Symbol)GSymbol.Empty;
		/// <summary>Specifies that a subexpression has started.</summary>
		public static Symbol Subexpression = (Symbol)nameof(Subexpression);
		/// <summary>Requests no indentation. This is sometimes useful to help 
		/// achieve a simple printer structure that uses either Indent(Subexpression) 
		/// or Indent(NoIndent) at the beginning with an unconditional Dedent() at
		/// the end, so that you don't need to keep track of whether or not you 
		/// called Indent.</summary>
		public static Symbol NoIndent = (Symbol)nameof(NoIndent);
		/// <summary>Specifies that brackets (parentheses or square brackets) have started.</summary>
		public static Symbol Brackets = (Symbol)nameof(Brackets);
		/// <summary>Requests label-tyle indentation</summary>
		public static Symbol Label = (Symbol)nameof(Label);

		// I thought I'd use this while printing triple-quoted strings, but it does 
		// the wrong thing. What we actually need is to duplicate whatever indentation
		// was used on the previous line, and anyway it can be handled internally in 
		// the printer for a specific language by overriding AppendIndentAfterNewline,
		// so there is no need to implement this in LNodePrinterHelper.
		/// <summary>Requests that all indentation of type Subexpression or Brackets 
		/// that is below this entry on the indent stack, but above the most recent 
		/// Normal/null indent, should be ignored.</summary>
		//public static Symbol CancelSubexpression = (Symbol)nameof(CancelSubexpression);
	}

	/// <summary>Standard extension methods for <see cref="ILNodePrinterHelper"/></summary>
	public static class LNodePrinterHelperExt
	{
		/// <summary>Appends a string, except newline ('\n') characters which are translated
		/// into calls to <see cref="Newline()"/>.</summary>
		public static ILNodePrinterHelper WriteSmartly(this ILNodePrinterHelper self, UString s)
		{
			do {
				int? iNewline = s.IndexOf('\n');
				if (iNewline == null)
				{
					return self.Write(s);
				}
				else
				{
					self.Write(s.Slice(0, iNewline.Value));
					self.Newline();
					s = s.Slice(iNewline.Value + 1);
				}
			} while (s.Length != 0);
			return self;
		}

		/// <summary>Appends a space to the output stream or StringBuilder if the 
		/// parameter is true.</summary>
		/// <remarks>This helper method exists because printers often want to add spaces 
		/// conditionally, e.g. it might want to add spaces around the current binary 
		/// operator if it is not `.` or `?.`.</remarks>
		public static ILNodePrinterHelper SpaceIf(this ILNodePrinterHelper self, bool flag)
		{
			return flag ? self.Write(' ') : self;
		}

		/// <summary>NewlineOrSpace(true) appends a newline while NewlineOrSpace(false) 
		/// appends a space.</summary>
		public static ILNodePrinterHelper NewlineOrSpace<Helper>(this ILNodePrinterHelper self, bool newline)
		{
			return newline ? self.Newline() : self.Write(' ');
		}

		/// <summary>Creates an newline that cannot be revoked later by calling 
		/// NewlineIsRequiredHere() followed by Newline().</summary>
		/// <remarks>This should be an extension method for <see cref="ILNodePrinterHelperWithRevokableNewlines{C,Helper}"/>
		/// but C# 9 fails to infer type argument C in that case.</remarks>
		public static ILNodePrinterHelper IrrevokableNewline(this ILNodePrinterHelper self)
		{
			self.NewlineIsRequiredHere();
			return self.Newline();
		}

		/// <summary>Calls <c>self.Write(c).Indent(PrinterIndentHint.Brackets)</c>.</summary>
		public static ILNodePrinterHelper WriteOpening(this ILNodePrinterHelper self, char c) => self.Write(c).Indent(PrinterIndentHint.Brackets);
		/// <summary>Calls <c>self.Write(s).Indent(PrinterIndentHint.Brackets)</c>.</summary>
		public static ILNodePrinterHelper WriteOpening(this ILNodePrinterHelper self, string s) => self.Write(s).Indent(PrinterIndentHint.Brackets);
		/// <summary>Calls <c>self.Write(c).Dedent(PrinterIndentHint.Brackets)</c>.</summary>
		public static ILNodePrinterHelper WriteClosing(this ILNodePrinterHelper self, char c) => self.Write(c).Dedent(PrinterIndentHint.Brackets);
		/// <summary>Calls <c>self.Write(s).Dedent(PrinterIndentHint.Brackets)</c>.</summary>
		public static ILNodePrinterHelper WriteClosing(this ILNodePrinterHelper self, string s) => self.Write(s).Dedent(PrinterIndentHint.Brackets);
	}

	/// <summary>Enhances <see cref="ILNodePrinterHelper{Self}"/> with an 
	/// ability to revoke newlines.</summary>
	/// <typeparam name="Self">The return type of methods in the base interface.</typeparam>
	/// <typeparam name="Checkpoint">A type returned by <see cref="GetCheckpoint"/>
	/// representing a location in the output stream.</typeparam>
	/// <remarks>
	/// When pretty-printing any language as text, it's a challenge to decide
	/// where to place newlines. You may want to break up long lines into
	/// shorter ones, as in
	/// <pre>
	/// if (ReallyLongIdentifier[Fully.Qualified.Name(multiple, parameters)] 
	///    > SomeConstant)
	/// {
	///    return ReallyLongIdentifier[firstThing + secondThing] 
	///       + thirdThing + fourthThing;
	/// }
	/// </pre>
	/// Conversely, you may want to print something on one line that you would
	/// ordinarily print on two:
	/// <pre>
	///     if (c) break;
	/// </pre>
	/// Of course, the problem is, you don't know how long the syntax tree 
	/// will be in text form until after you try to print it.
	/// <para/>
	/// My first idea to solve this problem was to use a 
	/// <a href="https://en.wikipedia.org/wiki/Rope_(data_structure)">rope</a> 
	/// tree data structure - inner syntax trees would produce small strings 
	/// that could be "roped" together to produce a bigger tree. But ropes tend
	/// not to use memory efficiently, and there was the challenge, which I 
	/// didn't see how to solve, of how to keep the tree balanced efficiently 
	/// (for this particular application perhaps a balanced tree wasn't needed,
	/// but as a perfectionist I didn't want to implement a "half-baked" data 
	/// structure.)
	/// <para/>
	/// Next I thought of a simpler solution based on an ordinary StringBuilder. 
	/// My idea was to insert newlines "pessimistically" - insert them 
	/// everywhere in which they might be needed - and then selectively "revoke" 
	/// them later if they turn out to be unnecessary. Only the most 
	/// recently-written newline(s) can be revoked, which keeps the implementation 
	/// simple and also limits the performance cost of deleting the newlines.
	/// <para/>
	/// To use, call Newline() to write a newline (with indentation). To make 
	/// a decision about whether to keep or revoke the most recent newline(s), 
	/// call RevokeOrCommitNewlines(cp, maxLineLength) where cp is a "checkpoint"
	/// representing some point before the first newline you want to potentially
	/// revoke, and maxLineLength is the line length threshold: if the line length 
	/// after combining lines, starting at the line on which the checkpoint is 
	/// located, does not exceed maxLineLength, then the newlines are revoked, 
	/// otherwise ALL newlines are committed (so earlier newlines can no longer 
	/// be revoked.)
	/// <para/>
	/// This design allows a potentially long series of newlines to be deleted
	/// in the reverse order that they were created, but if any newline is kept
	/// then previous ones can no longer be deleted.
	/// <para/>
	/// For an example of how this is used, see the JSON printer in LLLPG samples
	/// or look at the implementation of the LESv3 printer.
	/// </remarks>
	public interface ILNodePrinterHelperWithRevokableNewlines<Checkpoint, out Self> : ILNodePrinterHelper<Self>
		where Self : ILNodePrinterHelperWithRevokableNewlines<Checkpoint, Self>
	{
		/// <summary>Appends a newline, returning a checkpoint from with indentation afterward according to the 
		/// current indentation level.</summary>
		Checkpoint NewlineAfterCheckpoint();
		/// <summary>Gets a value that can be passed later to <see cref="RevokeNewlinesSince(Checkpoint)"/>.</summary>
		Checkpoint GetCheckpoint();
		/// <summary>Gets the current width of the current line (typically measured in characters).</summary>
		int LineWidth { get; }
		/// <summary>Deletes uncommitted newlines that were written after the specified checkpoint.</summary>
		/// <returns>The number of newlines that were just revoked.</returns>
		/// <remarks>Newlines created after a call to 
		/// <see cref="ILNodePrinterHelper{Self}.NewlineIsRequiredHere"/> or 
		/// <see cref="LNodePrinterHelperExt.IrrevokableNewline"/> are not revoked.</remarks>
		int RevokeNewlinesSince(Checkpoint cp);
		/// <summary>Commits all uncommitted newlines permanently.</summary>
		/// <remarks>Also causes ranges after the uncommitted newlines to be saved.</remarks>
		Self CommitNewlines();
		/// <summary>Revokes or commits newlines added since the specified 
		/// checkpoint. Recent newlines are revoked if the combined line length 
		/// after revokation does not exceed <c>maxLineWidth</c>, otherwise ALL
		/// newlines are committed permanently.</summary>
		/// <returns>0 if the method had no effect, -N if N newlines were 
		/// revoked, and +N if N newlines were committed.</returns>
		/// <remarks>This method does not affect the indent level.</remarks>
		int RevokeOrCommitNewlines(Checkpoint cp, int maxLineWidth);
	}

	/// <summary>Alias for <see cref="ILNodePrinterHelperWithRevokableNewlines{Checkpoint,Self}"/> 
	/// without the second type parameter.</summary>
	public interface ILNodePrinterHelperWithRevokableNewlines<Checkpoint> : 
	                 ILNodePrinterHelperWithRevokableNewlines<Checkpoint, ILNodePrinterHelperWithRevokableNewlines<Checkpoint>> { }
}