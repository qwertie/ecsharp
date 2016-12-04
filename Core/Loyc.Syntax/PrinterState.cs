using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Diagnostics;
using Loyc.Collections.Impl;

namespace Loyc.Syntax.Impl
{
	/// <summary>A helper type for printer objects. Its primary purposes are to
	/// manage indentation and to "revoke" newlines; it also tracks the current 
	/// line/column number.</summary>
	/// <remarks>
	/// Be careful not to duplicate this structure.
	/// <para/>
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
	/// Next I thought of the solution used here, a simpler solution based on an 
	/// ordinary StringBuilder. My idea was to insert newlines "pessimistically" 
	/// - insert them everywhere in which they might be needed - and then 
	/// selectively "revoke" them later if they turn out to be unnecessary. Only 
	/// the most recently-written newline(s) can be revoked, which keeps the 
	/// implementation simple and also limits the performance cost of deleting 
	/// the newlines.
	/// <para/>
	/// To use, call Newline() to write a newline (with indentation). To make 
	/// a decision about whether to keep or revoke the most recent newline(s), 
	/// call RevokeOrCommitNewlines(cp, maxLineLength) where cp is a "checkpoint"
	/// representing some point before the first newline want to potentially
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
	public struct PrinterState
	{
		public StringBuilder S;
		public int IndentLevel;
		public int LineNo;
		public string IndentString;
		public string NewlineString;
		private int _lineStartIndex;
		private int _lineStartAfterIndent;
		public int LineStartIndex { get { return _lineStartIndex; } }
		public int IndexInCurrentLine { get { return S.Length - _lineStartIndex; } }
		public int IndexInCurrentLineAfterIndent { get { return S.Length - _lineStartAfterIndent; } }
		public bool AtStartOfLine { get { return S.Length == _lineStartAfterIndent; } }
		private InternalList<Revokable> _newlines;

		public PrinterState(StringBuilder s, string indent = "\t", string newline = "\n")
		{
			S = s ?? new StringBuilder();
			IndentLevel = 0;
			IndentString = indent;
			NewlineString = newline;
			_lineStartIndex = 0;
			_lineStartAfterIndent = 0;
			LineNo = 1;
			_newlines = InternalList<Revokable>.Empty;
		}

		public StringBuilder Append(string s)
		{
			return S.Append(s);
		}
		
		public void Indent()
		{
			IndentLevel++;
		}
		public void Dedent()
		{
			IndentLevel--;
		}

		/// <summary>Current length of the output string</summary>
		public int Length { get { return S.Length; } }

		public Checkpoint GetCheckpoint() { 
			return new Checkpoint { _oldLineStart = _lineStartIndex, _oldLineNo = LineNo, _oldLineStartAfterIndent = _lineStartAfterIndent };
		}

		/// <summary>Writes a newline and the appropriate amount of indentation afterward.</summary>
		/// <param name="changeIndentLevel">Amount by which to change <see cref="IndentLevel"/> before writing the newline</param>
		/// <returns>A <see cref="Checkpoint"/> that can be used to revoke the newline</returns>
		/// <remarks>Note that "revoking" a newline does NOT restore the original indent level.</remarks>
		public Checkpoint Newline(int changeIndentLevel = 0)
		{
			var cp = GetCheckpoint();
			IndentLevel += changeIndentLevel;
			Contract.Assert(IndentLevel >= 0);

			var r = new Revokable(_lineStartIndex, S.Length, NewlineString.Length + IndentString.Length * IndentLevel);
			S.Append(NewlineString);
			LineNo++;
			_lineStartIndex = S.Length;
			for (int i = 0; i < IndentLevel; i++)
				S.Append(IndentString);
			_lineStartAfterIndent = S.Length;
			_newlines.Add(r);
			return cp;
		}

		/// <summary>Revokes or commits newlines added since the specified 
		/// checkpoint. Recent newlines are revoked if the combined line length 
		/// after revokation does not exceed <c>maxLineWidth</c>, otherwise ALL
		/// newlines are committed permanently.</summary>
		/// <returns>0 if the method had no effect, -N if N newlines were 
		/// revoked, and +N if N newlines were committed.</returns>
		/// <remarks>This method does not affect the indent level.</remarks>
		public int RevokeOrCommitNewlines(Checkpoint cp, int maxLineWidth)
		{
			// Length before revokation
			int lengthAfterCP = S.Length - cp._oldLineStart;
			// Figure out which newlines we can revoke and what the total line 
			// length would be if they were revoked
			int i0;
			bool any = false;
			for (i0 = _newlines.Count; i0 > 0 && _newlines[i0 - 1]._index >= cp._oldLineStart; i0--) {
				lengthAfterCP -= _newlines[i0].Length;
				any = true;
			}

			if (any) {
				if (lengthAfterCP <= maxLineWidth) {
					int count = RevokeNewlinesStartingAtIndex(i0);
					Debug.Assert(cp._oldLineNo == LineNo);
					Debug.Assert(cp._oldLineStart == _lineStartIndex);
					_lineStartAfterIndent = cp._oldLineStartAfterIndent;
					return -count;
				} else {
					// We have decided not to revoke the newest newlines; this means
					// we can't revoke older ones later, since Revoke() does not 
					// support revoking some recent newlines and not others.
					return CommitNewlines();
				}
			}
			return 0;
		}

		public int CommitNewlines()
		{
			int count = _newlines.Count;
			_newlines.Clear();
			return count;
		}

		public int RevokeNewlinesSince(Checkpoint cp)
		{
			int i0;
			for (i0 = _newlines.Count; i0 > 0 && _newlines[i0 - 1]._index >= cp._oldLineStart; i0--) { }
			return RevokeNewlinesStartingAtIndex(i0);
		}

		private int RevokeNewlinesStartingAtIndex(int i0)
		{
			int count = _newlines.Count - i0;
			for (int i = _newlines.Count - 1; i >= i0; i--) {
				Revoke(_newlines[i]);
				_newlines.RemoveAt(i);
			}
			return count;
		}

		/// <summary>Revokes (deletes) the last newline created, and its indent.</summary>
		/// <param name="r">Object returned from Newline()</param>
		/// <remarks>Only the most recent newline can be revoked, and of course, 
		/// it can only be revoked once. Multiple newlines can be revoked if 
		/// they are revoked in the reverse order in which they were created.</remarks>
		private void Revoke(Revokable r)
		{
			S.Remove(r._index, r._length);
			_lineStartIndex = r._oldLineStartIndex;
			LineNo--;
		}

		private struct Revokable
		{
			internal int _index;
			internal int _length;
			internal int _oldLineStartIndex;
			public Revokable(int oldNewlineIndex, int index, int length)
			{
				_oldLineStartIndex = oldNewlineIndex;
				_index = index;
				_length = length;
			}
			public int Length { get { return _length; } }
		}

		public struct Checkpoint
		{
			internal int _oldLineStart; // at start of line
			internal int _oldLineStartAfterIndent;
			internal int _oldLineNo;
			public int LineNo { get { return _oldLineNo; } }
		}
	}
}
