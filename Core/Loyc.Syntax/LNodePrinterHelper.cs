using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Diagnostics;
using Loyc;
using Loyc.Collections.Impl;

namespace Loyc.Syntax.Impl
{
	[Obsolete("This class has been renamed to LNodePrinterHelper")]
	public class PrinterState : LNodePrinterHelper
	{
		public PrinterState(StringBuilder s, string indent = "\t", string newline = "\n")
			: base(s, null, true, indent, newline) { }

		[Obsolete("PrinterState.Checkpoint was renamed to " + nameof(LNodePrinterHelperLocation))]
		public struct Checkpoint { }
	}

	/// <summary>A helper type for printer objects; for details, please see
	/// <see cref="ILNodePrinterHelper{S}"/>, its derived interface
	/// <see cref="ILNodePrinterHelperWithRevokableNewlines{S,C}"/>, and the documentation of the
	/// constructor <see cref="LNodePrinterHelper(StringBuilder, string, string, bool, string, string)"/>.</summary>
	public class LNodePrinterHelper : ILNodePrinterHelperWithRevokableNewlines<LNodePrinterHelperLocation, LNodePrinterHelper>, ILNodePrinterHelper
	{
		protected int _indentLevel;
		protected int _lineNo;
		private int _lineStartIndex;
		private int _lineStartAfterIndent;
		private PendingAction _pendingAction;
		public string IndentString { get; set; }
		public string NewlineString { get; set; }
		public string LabelIndentString { get; set; }
		public string BracketIndentString { get; set; }
		public int MaxBracketIndents { get; set; }
		protected StringBuilder _s;
		protected InternalList<Pair<ILNode, int>> _nodeStack;
		protected InternalList<object> _indentStack;
		private InternalList<Revokable> _revokableNewlines;
		private InternalList<Triplet<ILNode, IndexRange, int>> _unsavedRanges;
		private Action<ILNode, IndexRange, int> _saveRange;

		public StringBuilder StringBuilder
		{
			get => _s;
			set => _s = value ?? throw new ArgumentNullException(nameof(StringBuilder));
		}
		public int LineStartIndex => _lineStartIndex;
		public int IndexInCurrentLine => _s.Length - _lineStartIndex;
		public int IndexInCurrentLineAfterIndent => _s.Length - _lineStartAfterIndent;
		[Obsolete("Renamed to IsAtStartOfLine")]
		public bool AtStartOfLine => IsAtStartOfLine;
		public bool IsAtStartOfLine => _s.Length <= _lineStartAfterIndent | (_pendingAction & PendingAction.Newline) != 0;
		public char LastCharWritten => _s.TryGet(_s.Length - 1, '\uFFFF');
		public int LineWidth => _s.Length - _lineStartIndex;
		public int LineNumber => _lineNo;

		/// <summary>Gets or sets a method that is called when the node's range in the 
		/// output is locked in (after you call <see cref="EndNode()"/>).</summary>
		/// <remarks>The third parameter of the method is the node's depth in the
		/// syntax tree (e.g. 3 means that the node has three known parents).</remarks>
		public Action<ILNode, IndexRange, int> SaveRange
		{
			get => _saveRange;
		}

		public LNodePrinterHelper(StringBuilder s, Action<ILNode, IndexRange, int> saveRange = null,
			bool allowNewlineRevocation = true,
			string indent = "\t", string newline = "\n",
			string labelIndent = "  ", string bracketIndent = "  ",
			int maxBracketIndents = 4)
		{
			_s = s ?? new StringBuilder();
			IndentString = indent;
			NewlineString = newline;
			LabelIndentString = labelIndent;
			BracketIndentString = bracketIndent;
			MaxBracketIndents = maxBracketIndents;
			_saveRange = saveRange;
			_indentStack = InternalList<object>.Empty;
			if (allowNewlineRevocation)
				_revokableNewlines = InternalList<Revokable>.Empty;
			//if (saveRange != null)
			_nodeStack = InternalList<Pair<ILNode, int>>.Empty;
			Reset();
		}

		/// <summary>Calls Dispose() and resets the state of this object, like calling the constructor.
		/// Does not clear the <see cref="StringBuilder"/>.</summary>
		public virtual void Reset()
		{
			Dispose();
			_lineStartIndex = 0;
			_lineStartAfterIndent = 0;
			_lineNo = 1;
			_pendingAction = 0;
			_indentStack = InternalList<object>.Empty;

			if (_revokableNewlines.InternalArray != null)
			{
				_revokableNewlines = InternalList<Revokable>.Empty;
				_unsavedRanges = InternalList<Triplet<ILNode, IndexRange, int>>.Empty;
			}
			if (_nodeStack.InternalArray != null)
			{
				_nodeStack = InternalList<Pair<ILNode, int>>.Empty;
			}
		}

		[Flags]
		enum PendingAction
		{
			/// <summary>A "collapsable" newline requested by NewlineIsRequiredHere</summary>
			Newline = 1,
			/// <summary>A delayed indent requested by Newline(deferIndent: true)</summary>
			Indent = 2,
			/// <summary>Flag set by <see cref="BeginNode"/></summary>
			NodeStarted = 4,
			/// <summary>Flag set by <see cref="EndNode"/></summary>
			NodeEnded = 8,
			NodeChanged = NodeStarted | NodeEnded,
			/// <summary>A special flag that is needed to stop FlushIndent from interfering with newlines</summary>
			CollapsableNewlineWasJustWritten = 16
		}

		/// <inheritdoc/>
		public virtual LNodePrinterHelper Write(char c)
		{
			if (_pendingAction != 0)
				FlushPending(c);
			_s.Append(c);
			return this;
		}
		/// <inheritdoc/>
		public virtual LNodePrinterHelper Write(string s)
		{
			if (s.Length != 0)
			{
				if (_pendingAction != 0)
					FlushPending(s[0]);
				_s.Append(s);
			}
			return this;
		}
		/// <inheritdoc/>
		public virtual LNodePrinterHelper Write(UString s)
		{
			if (s.Length != 0)
			{
				if (_pendingAction != 0)
					FlushPending(s[0]);
				_s.Append(s);
			}
			return this;
		}

		/// <inheritdoc/>
		public LNodePrinterHelper Space()
		{
			if (LastCharWritten != ' ')
				Write(' ');
			return this;
		}

		/// <summary>This method, which does nothing by default, is called by all three 
		/// versions of Write() when the first character is written after a call to
		/// <see cref="BeginNode(ILNode)"/> or <see cref="EndNode()"/>. You can override 
		/// this method can detect conflicts between this character and the previous 
		/// characters in the stream (<see cref="LastCharWritten"/>).</summary>
		/// <param name="firstChar">First character in the new node, which has not
		/// been written yet</param>
		/// <remarks>
		/// For example, suppose a language has a prefix operator `.` and two binary
		/// operators called `?.` and `?`. If the last character printed was `?` then
		/// it should not be followed by `.` if the `.` is part of a different node,
		/// since the parser would treat `?.` as a single token rather than the 
		/// intended two tokens. A derived class can use this method to detect the
		/// conflict and prevent it by adding a space to <see cref="StringBuilder"/>.
		/// (If you call <see cref="Write(char)"/> to do this, it will cause this 
		/// method to be called again.)
		/// </remarks>
		protected virtual void OnNodeChanged(char firstChar) { }

		[Obsolete("This method was renamed to Write(s)")]
		public StringBuilder Append(string s) { Write(s); return _s; }
		[Obsolete("This method was renamed to Write(s)")]
		public StringBuilder Append(UString s) { Write(s); return _s; }

		/// <summary>Current length of the output string. This length can decrease if 
		/// newlines are revoked.</summary>
		[Obsolete("Please call " + nameof(StringBuilder) + ".Length instead")]
		public int Length => StringBuilder.Length;

		/// <summary>Older version of Newline method, which returns a checkpoint instead of <c>this</c>.</summary>
		/// <param name="changeIndentLevel">Amount by which to change the indent level (positive, negative or zero).</param>
		public LNodePrinterHelperLocation Newline(int changeIndentLevel)
		{
			while (changeIndentLevel > 0)
			{
				changeIndentLevel--;
				Indent();
			}
			while (changeIndentLevel < 0)
			{
				changeIndentLevel++;
				Dedent();
			}
			return NewlineAfterCheckpoint();
		}

		/// <inheritdoc/>
		public virtual LNodePrinterHelper Newline(bool deferIndent = false)
		{
			if ((_pendingAction & PendingAction.CollapsableNewlineWasJustWritten) == 0)
			{
				int oldLineStartIndex = _lineStartIndex;
				int newlineIndex = _s.Length;
				_s.Append(NewlineString);

				_lineNo++;
				_lineStartIndex = _s.Length;

				if ((_pendingAction & PendingAction.Newline) == 0 && _revokableNewlines.InternalArray != null)
					_revokableNewlines.Add(new Revokable(oldLineStartIndex, newlineIndex, _s.Length - newlineIndex));

				_pendingAction &= ~(PendingAction.Newline | PendingAction.Indent);

				if (deferIndent) {
					_pendingAction |= PendingAction.Indent;
					_lineStartAfterIndent = _s.Length;
				} else
					FlushIndentCore();
			}
			_pendingAction &= ~(PendingAction.CollapsableNewlineWasJustWritten);
			return this;
		}

		private void FlushIndentCore()
		{
			Debug.Assert(_s.Last() == '\n');
			bool isRevokable = _revokableNewlines.Count != 0 && _revokableNewlines.Last._index == _s.Length - 1;

			AppendIndentAfterNewline(_indentStack.Count);
			_lineStartAfterIndent = _s.Length;

			if (isRevokable)
				_revokableNewlines.InternalArray[_revokableNewlines.Count - 1]._length = _s.Length - _revokableNewlines.Last._index;
		}

		private void FlushPending(char firstChar)
		{
			var action = _pendingAction;
			_pendingAction &= PendingAction.Newline;

			if ((action & PendingAction.Newline) != 0)
				Newline(deferIndent: false);
			else if ((action & PendingAction.Indent) != 0)
				FlushIndentCore();

			if ((action & PendingAction.NodeChanged) != 0)
				OnNodeChanged(firstChar);
		}

		public LNodePrinterHelper FlushIndent()
		{
			if ((_pendingAction & PendingAction.Newline) != 0)
			{
				if ((_pendingAction & PendingAction.CollapsableNewlineWasJustWritten) == 0)
					Newline(deferIndent: false);
				_pendingAction |= PendingAction.CollapsableNewlineWasJustWritten;
			}
			else if ((_pendingAction & PendingAction.Indent) != 0)
				FlushIndentCore();

			_pendingAction &= ~(PendingAction.Indent | PendingAction.Newline);
			return this;
		}

		/// <summary>Called just after a newline is emitted to append indentation.</summary>
		/// <param name="hint">The hint that was passed to <see cref="Newline"/></param>
		protected virtual void AppendIndentAfterNewline(int indentStackSize)
		{
			int indents = 0, brackets = 0;
			int subexpr = 0, label = 0; // boolean
			for (int i = _indentStack.Count - 1; i >= 0; i--)
			{
				var hint = _indentStack[i];
				if (hint == PrinterIndentHint.Subexpression)
					subexpr = 1;
				else if (hint == PrinterIndentHint.Brackets)
					brackets++;
				else if (hint == PrinterIndentHint.Label)
					label = 1;
				//else if (hint == PrinterIndentHint.CancelSubexpression)
				//	for (object prev; 
				//		(prev = _indentStack[i - 1]) == PrinterIndentHint.Brackets || prev == PrinterIndentHint.Subexpression || prev == PrinterIndentHint.NoIndent; 
				//		i--) { }
				else if (hint != PrinterIndentHint.NoIndent)
					indents++;
			}
		
			for (int i = indents - label; i > 0; i--)
				_s.Append(IndentString);
			if (label != 0)
				_s.Append(LabelIndentString);
			for (int i = System.Math.Min(System.Math.Max(brackets, subexpr), MaxBracketIndents); i > 0; i--)
				_s.Append(BracketIndentString);
		}

		/// <inheritdoc/>
		public virtual LNodePrinterHelper NewlineIsRequiredHere()
		{
			if ((_pendingAction & PendingAction.CollapsableNewlineWasJustWritten) == 0)
				_pendingAction |= PendingAction.Newline;
			return this;
		}

		/// <inheritdoc/>
		public virtual LNodePrinterHelper Indent(PrinterIndentHint modeHint = null)
		{
			_indentStack.Add(modeHint);
			return this;
		}
		/// <inheritdoc/>
		public virtual LNodePrinterHelper Dedent(PrinterIndentHint modeHint = null)
		{
			if (modeHint != null && modeHint != _indentStack.Last)
				CheckParam.ThrowBadArgument(nameof(modeHint), "Dedent mismatch: arg {0} != {1} on stack", modeHint, _indentStack.Last);
			_indentStack.RemoveAt(_indentStack.Count - 1);
			return this;
		}

		/// <inheritdoc/>
		public virtual LNodePrinterHelper BeginNode(ILNode node)
		{
			_pendingAction |= PendingAction.NodeStarted;
			if (_nodeStack.InternalArray != null)
				_nodeStack.Add(Pair.Create(node, _s.Length));
			return this;
		}

		/// <inheritdoc/>
		public virtual LNodePrinterHelper EndNode()
		{
			_pendingAction |= PendingAction.NodeEnded;
			if (_nodeStack.InternalArray != null)
			{
				var triplet = _nodeStack.Last;
				_nodeStack.RemoveAt(_nodeStack.Count - 1);
				if (_saveRange != null)
				{
					var range = new IndexRange(triplet.B) { EndIndex = _s.Length };
					if (_revokableNewlines.InternalArray == null)
						_saveRange(triplet.A, range, _nodeStack.Count);
					else
						_unsavedRanges.Add(Triplet.Create(triplet.A, range, _nodeStack.Count));
				}
			}
			return this;
		}

		/// <inheritdoc/>
		public LNodePrinterHelper BeginNode(ILNode node, PrinterIndentHint indentHint)
		{
			BeginNode(node);
			Indent(indentHint);
			return this;
		}

		/// <inheritdoc/>
		public LNodePrinterHelper EndNode(PrinterIndentHint indentHint)
		{
			EndNode();
			Dedent(indentHint);
			return this;
		}

		/// <inheritdoc/>
		public virtual LNodePrinterHelperLocation GetCheckpoint() { 
			return new LNodePrinterHelperLocation { 
				_oldLineStart = _lineStartIndex, 
				_oldLineNo = _lineNo, 
				_oldLineStartAfterIndent = _lineStartAfterIndent
			};
		}

		/// <inheritdoc/>
		public virtual LNodePrinterHelperLocation NewlineAfterCheckpoint()
		{
			var cp = GetCheckpoint();
			Newline();
			return cp;
		}

		/// <inheritdoc/>
		public int RevokeOrCommitNewlines(LNodePrinterHelperLocation cp, int maxLineWidth)
		{
			// Figure out which newlines we can revoke and what the total 
			// length would be if they were revoked
			int lengthWithoutNewlines = _s.Length - cp._oldLineStart;
			int i0;
			bool anyRevokables = false;
			for (i0 = _revokableNewlines.Count; i0 > 0 && _revokableNewlines[i0 - 1]._index >= cp._oldLineStart; i0--) {
				lengthWithoutNewlines -= _revokableNewlines[i0 - 1].Length;
				anyRevokables = true;
			}

			if (anyRevokables)
			{
				var tooLong = false;
				if (lengthWithoutNewlines > maxLineWidth)
				{
					// Line length appears to be exceeded, but it may be that there are 
					// non-revokable newlines so that the actual max line length is lower.
					// Find out the maximum line length considering non-revokables.
					int i1 = i0, lineStart = cp._oldLineStart, charsRevocableOnThisLine = 0;
					for (int iChar = lineStart; iChar <= _s.Length; iChar++)
					{
						while (i1 < _revokableNewlines.Count && iChar == _revokableNewlines[i1]._index) {
							charsRevocableOnThisLine += _revokableNewlines[i1].Length;
							iChar                    += _revokableNewlines[i1].Length;
							i1++;
						}
						if (iChar == _s.Length || _s[iChar] == '\n') {
							int lineLength = iChar - lineStart - charsRevocableOnThisLine;
							if (lineLength > maxLineWidth) {
								tooLong = true;
								break;
							}
							lineStart = iChar + 1;
							charsRevocableOnThisLine = 0;
						}
					}
				}

				if (tooLong)
				{
					// We have decided not to revoke the newest newlines; this means
					// we can't revoke older ones later, since Revoke() does not 
					// support revoking some recent newlines and not others.
					int count = _revokableNewlines.Count;
					CommitNewlines();
					return count;
				}
				else
				{
					int count = RevokeNewlinesStartingAtIndex(i0);
					Debug.Assert(cp._oldLineNo == _lineNo);
					Debug.Assert(cp._oldLineStart == _lineStartIndex);
					_lineStartAfterIndent = cp._oldLineStartAfterIndent;
					return -count;
				}
			}
			return 0;
		}

		/// <inheritdoc/>
		public LNodePrinterHelper CommitNewlines()
		{
			int count = _revokableNewlines.Count;
			if (_revokableNewlines.InternalArray != null) {
				_revokableNewlines.Clear();
				SaveRanges();
			}
			return this;
		}

		/// <inheritdoc/>
		public int RevokeNewlinesSince(LNodePrinterHelperLocation cp)
		{
			int i0;
			for (i0 = _revokableNewlines.Count; i0 > 0 && _revokableNewlines[i0 - 1]._index >= cp._oldLineStart; i0--) { }
			return RevokeNewlinesStartingAtIndex(i0);
		}

		/// <summary>Commits uncommitted newlines and node ranges. Throws if 
		/// a SaveRange method was provided but EndNode has not been called 
		/// the correct number of times.</summary>
		public void Dispose()
		{
			CommitNewlines();
			Contract.Assert(_nodeStack.Count == 0, "{0} calls to BeginNode without matching calls to EndNode".Localized(_nodeStack.Count));
		}

		private int RevokeNewlinesStartingAtIndex(int i0)
		{
			// Actually perform the revokations
			int count = _revokableNewlines.Count - i0;
			for (int i = _revokableNewlines.Count - 1; i >= i0; i--) {
				Revoke(_revokableNewlines[i]);
				_revokableNewlines.RemoveAt(i);
			}
			if (_revokableNewlines.IsEmpty)
				SaveRanges();
			return count;
		}

		private void SaveRanges()
		{
			if (_saveRange != null)
				foreach (var item in _unsavedRanges)
					_saveRange(item.A, item.B, item.C);

			_unsavedRanges.Clear();
		}

		/// <summary>Revokes (deletes) the last newline created, and its indent.</summary>
		/// <param name="r">Object returned from Newline()</param>
		/// <remarks>Only the most recent newline can be revoked, and of course, 
		/// it can only be revoked once. Multiple newlines can be revoked if 
		/// they are revoked in the reverse order in which they were created.</remarks>
		private void Revoke(Revokable r)
		{
			// Adjust indexes in _nodeStack if necessary
			for (int i = _nodeStack.Count - 1; i >= 0; i--)
				if (_nodeStack[i].B > r._index)
					_nodeStack.InternalArray[i].B -= r._length;
				else
					break;

			// Adjust indexes in uncommitted ranges
			for (int i = _unsavedRanges.Count - 1; i >= 0; i--)
				if (_unsavedRanges[i].B.EndIndex > r._index)
					if (_unsavedRanges[i].B.StartIndex > r._index)
						_unsavedRanges.InternalArray[i].B.StartIndex -= r._length;
					else
						_unsavedRanges.InternalArray[i].B.Length -= r._length;

			// TODO: I think we're not updating this properly, may have so save old value as we do for _lineStartIndex
			if (_lineStartAfterIndent > r._index)
				_lineStartAfterIndent -= r._length;

			// Finally remove the character(s)
			_s.Remove(r._index, r._length);
			_lineStartIndex = r._oldLineStartIndex;
			_lineNo--;
		}

		ILNodePrinterHelper IPrinterHelper<ILNodePrinterHelper>.Write(char c) => Write(c);
		ILNodePrinterHelper IPrinterHelper<ILNodePrinterHelper>.Write(string s) => Write(s);
		ILNodePrinterHelper IPrinterHelper<ILNodePrinterHelper>.Write(UString s) => Write(s);
		ILNodePrinterHelper IPrinterHelper<ILNodePrinterHelper>.Space() => Space();
		ILNodePrinterHelper IPrinterHelper<ILNodePrinterHelper>.Newline(bool deferIndent) => Newline(deferIndent);
		ILNodePrinterHelper IPrinterHelper<ILNodePrinterHelper>.NewlineIsRequiredHere() => NewlineIsRequiredHere();
		ILNodePrinterHelper IPrinterHelper<ILNodePrinterHelper>.FlushIndent() => FlushIndent();
		ILNodePrinterHelper IPrinterHelper<ILNodePrinterHelper>.Indent(PrinterIndentHint modeHint) => Indent(modeHint);
		ILNodePrinterHelper IPrinterHelper<ILNodePrinterHelper>.Dedent(PrinterIndentHint modeHint) => Dedent(modeHint);
		ILNodePrinterHelper ILNodePrinterHelper<ILNodePrinterHelper>.BeginNode(ILNode node) => BeginNode(node);
		ILNodePrinterHelper ILNodePrinterHelper<ILNodePrinterHelper>.EndNode() => EndNode();
		ILNodePrinterHelper ILNodePrinterHelper<ILNodePrinterHelper>.BeginNode(ILNode node, PrinterIndentHint indentHint) => BeginNode(node, indentHint);
		ILNodePrinterHelper ILNodePrinterHelper<ILNodePrinterHelper>.EndNode(PrinterIndentHint indentHint) => EndNode(indentHint);

		// Represents something (a newline) that can be revoked
		private struct Revokable
		{
			internal int _index;
			internal int _length; // length of newline + indent
			internal int _oldLineStartIndex;
			public Revokable(int oldLineStartIndex, int index, int length)
			{
				_oldLineStartIndex = oldLineStartIndex;
				_index = index;
				_length = length;
			}
			public int Length { get { return _length; } }
		}
	}
	
	/// <summary>A location in the output stream of <see cref="LNodePrinterHelper"/>.</summary>
	public struct LNodePrinterHelperLocation
	{
		internal int _oldLineStart; // at start of line
		internal int _oldLineStartAfterIndent;
		internal int _oldLineNo;
		public int LineNumber { get { return _oldLineNo; } }
	}
}
