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
		/// <see cref="LNodePrinterHelperExt.WriteSmartly"/>).</remarks>
		Self Write(string s);
		/// <summary>Appends a string to the output stream or StringBuilder.</summary>
		/// <remarks>The string should not include newlines (if it might, call 
		/// <see cref="LNodePrinterHelperExt.WriteSmartly"/>).</remarks>
		Self Write(UString s);
		/// <summary>Appends a newline, with indentation afterward according to the 
		/// current indentation level.</summary>
		Self Newline();
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
		/// node. The printer must call <see cref="EndNode"/> when it is done.</summary>
		/// <param name="kind">Tells the helper what kind of node this is, either 
		/// <see cref="PrinterIndentHint.Subexpression"/> or <see cref="PrinterIndentHint.Label"/>,
		/// which may affect indentation. See Remarks.</param>
		/// <remarks>
		/// Consider a statement <c>x = 2 * (1 + Method(@`%newline` parameter1, parameter2))</c>
		/// with a newline before parameter1. It is the printer's responsibility to notice the 
		/// trivia and call <see cref="Newline"/>, but in this case the newline should 
		/// include some extra indentation to indicate that a new statement does not begin
		/// here. To this end, the printer should call BeginNode with kind = 
		/// <see cref="PrinterIndentHint.Subexpression"/> for each node that is not at the 
		/// statement level. Then, the helper can detect the need for extra indentation by 
		/// noticing that four subexpressions have begun but not ended at the time 
		/// <see cref="Newline"/> is called:
		/// <ul>
		/// <li><c>2 * (1 + Method(@`%newline` parameter1, parameter2))</c></li>
		/// <li><c>(1 + Method(@`%newline` parameter1, parameter2))</c></li>
		/// <li><c>Method(@`%newline` parameter1, parameter2))</c></li>
		/// <li><c>@`%newline` parameter1</c></li>
		/// </ul>
		/// The helper object should therefore include more indentation above and beyond the
		/// level of indentation required by calls to <see cref="Indent"/>. The fact that there
		/// are <i>four</i> subexpressions may or may not affect the <i>amount</i> of 
		/// indentation.
		/// <para/>
		/// Another kind of interesting node is labels (targets of goto statements). A typical
		/// style is that labels are printed with less indentation than a normal statement.
		/// kind = <see cref="PrinterIndentHint.Label"/> represents a label. To print a label 
		/// properly, the printer needs to call this method before calling <see cref="Newline"/> 
		/// so that the indentation after the newline is correct.
		/// </remarks>
		Self BeginNode(ILNode node, Symbol kind = null);
		/// <summary>Informs the helper that the printer is done writing the most recently
		/// started node. The helper may save the range of the node, e.g. by calling 
		/// <see cref="ILNodePrinterOptions.SaveRange"/>.</summary>
		Self EndNode();
		/// <summary>Increases the current indent level.</summary>
		/// <remarks>This interface does not define what string is used for an indent; 
		/// typically it's a tab or 2 to 4 spaces.</remarks>
		Self Indent();
		/// <summary>Decreases the current indent level.</summary>
		Self Dedent();
		/// <summary>Returns true iff nothing has been written since the last call to 
		/// <see cref="Newline"/> or <see cref="NewlineIsRequiredHere"/>.</summary>
		bool IsAtStartOfLine { get; }
		/// <summary>Gets the character most recently written to the stream, or 
		/// '\uFFFF' if no characters have been written.</summary>
		char LastCharWritten { get; }
	}

	/// <summary>Alias for <see cref="ILNodePrinterHelper{Self}"/> without a type parameter.</summary>
	public interface ILNodePrinterHelper : ILNodePrinterHelper<ILNodePrinterHelper> { }

	/// <summary>Values used with <see cref="ILNodePrinterHelper{Self}.BeginNode(ILNode, Symbol)"/>.</summary>
	public static class PrinterIndentHint
	{
		public static Symbol Subexpression = (Symbol)nameof(Subexpression);
		public static Symbol Label = (Symbol)nameof(Label);
	}

	/// <summary>Standard extension methods for <see cref="ILNodePrinterHelper{Self}"/></summary>
	public static class LNodePrinterHelperExt
	{
		/// <summary>Appends a string, except newline ('\n') characters which are translated
		/// into calls to <see cref="Newline()"/>.</summary>
		public static Helper WriteSmartly<Helper>(this Helper self, UString s) where Helper : ILNodePrinterHelper<Helper>
		{
			do
			{
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
		public static Helper SpaceIf<Helper>(this Helper self, bool flag) where Helper : ILNodePrinterHelper<Helper>
		{
			return flag ? self.Write(' ') : self;
		}

		/// <summary>NewlineOrSpace(true) appends a newline while NewlineOrSpace(false) 
		/// appends a space.</summary>
		public static Helper NewlineOrSpace<Helper>(this Helper self, bool newline) where Helper : ILNodePrinterHelper<Helper>
		{
			return newline ? self.Newline() : self.Write(' ');
		}

		/// <summary>Creates an newline that cannot be revoked later by calling 
		/// NewlineIsRequiredHere() followed by Newline().</summary>
		/// <remarks>This should be an extension method for <see cref="ILNodePrinterHelperWithRevokableNewlines{C,Helper}"/>
		/// but C# 9 fails to infer type argument C in that case.</remarks>
		public static Helper IrrevokableNewline<Helper>(this Helper self) where Helper : ILNodePrinterHelper<Helper>
		{
			self.NewlineIsRequiredHere();
			return self.Newline();
		}
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
		Checkpoint NewlineWithCheckpoint();
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
		/// <returns>The number of newlines that were just committed.</returns>
		int CommitNewlines();
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