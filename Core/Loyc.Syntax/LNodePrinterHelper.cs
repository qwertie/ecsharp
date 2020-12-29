using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Diagnostics;
using Loyc.Collections.Impl;

namespace Loyc.Syntax.Impl
{
	[Obsolete("This class has been renamed to LNodePrinterHelper")]
	public class PrinterState : LNodePrinterHelper
	{
		public PrinterState(StringBuilder s, string indent = "\t", string newline = "\n") : base(s, indent, newline) { }

		[Obsolete("PrinterState.Checkpoint was renamed to " + nameof(LNodePrinterHelperLocation))]
		public struct Checkpoint { }
	}

	/// <summary>A helper type for printer objects; for details, please see
	/// <see cref="ILNodePrinterHelper{S}"/>, its derived interface
	/// <see cref="ILNodePrinterHelperWithRevokableNewlines{S,C}"/>, and the documentation of the
	/// constructor <see cref="LNodePrinterHelper(StringBuilder, string, string, bool, string, string, int)"/>.</summary>
	public class LNodePrinterHelper : ILNodePrinterHelperWithRevokableNewlines<LNodePrinterHelperLocation, LNodePrinterHelper>, ILNodePrinterHelper
	{
		protected int _indentLevel;
		protected int _lineNo;
		private int _lineStartIndex;
		private int _lineStartAfterIndent;
		private bool _newlineIsRequiredHere;
		public string IndentString { get; set; }
		public string NewlineString { get; set; }
		public string LabelIndentString { get; set; }
		public string SubexpressionIndentString { get; set; }
		private int _indexWhereNodeBegan = -1;
		protected StringBuilder _s;
		private InternalList<Revokable> _revokableNewlines;
		private InternalList<Triplet<ILNode, IndexRange, int>> _unsavedRanges;
		protected InternalList<Triplet<ILNode, int, Symbol>> _nodeStack;
		protected Action<ILNode, IndexRange, int> _saveRange;
		
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
		public bool IsAtStartOfLine => _s.Length == _lineStartAfterIndent || _newlineIsRequiredHere;
		public char LastCharWritten => _s.TryGet(_s.Length - 1, '\uFFFF');
		public int LineWidth => _s.Length - _lineStartIndex;
		public int LineNumber => _lineNo;
		public Symbol HintOnTopOfStack => _nodeStack.IsEmpty ? null : _nodeStack.Last.C;
		public int IndentLevel
		{
			get => _indentLevel;
			set {
				CheckParam.IsNotNegative(nameof(IndentLevel), value);
				_indentLevel = value;
			}
		}
		/// <summary>Gets or sets a method that is called when the node's range in the 
		/// output is locked in (after you call <see cref="EndNode()"/>).</summary>
		/// <remarks>The third parameter of the method is the node's depth in the
		/// syntax tree (e.g. 3 means that the node has three known parents).</remarks>
		public Action<ILNode, IndexRange, int> SaveRange
		{
			get => _saveRange;
			set => _saveRange = value;
		}

		public LNodePrinterHelper(StringBuilder s, string indent = "\t", string newline = "\n", bool allowNewlineRevocation = true, string labelIndent = "  ", string subexprIndent = "\t\t")
		{
			_s = s ?? new StringBuilder();
			IndentString = indent;
			NewlineString = newline;
			LabelIndentString = labelIndent;
			SubexpressionIndentString = subexprIndent;
			if (allowNewlineRevocation)
				_revokableNewlines = InternalList<Revokable>.Empty;
			Reset();
		}

		/// <summary>Calls Dispose() and resets the state of this object, like calling the constructor.
		/// Does not clear the <see cref="StringBuilder"/>.</summary>
		public virtual void Reset()
		{
			Dispose();
			IndentLevel = 0;
			_lineStartIndex = 0;
			_lineStartAfterIndent = 0;
			_lineNo = 1;
			_indexWhereNodeBegan = -1;
			_nodeStack = InternalList<Triplet<ILNode, int, Symbol>>.Empty;
			if (_revokableNewlines.InternalArray != null)
			{
				_revokableNewlines = InternalList<Revokable>.Empty;
				_unsavedRanges = InternalList<Triplet<ILNode, IndexRange, int>>.Empty;
			}
		}

		/// <inheritdoc/>
		public virtual LNodePrinterHelper Write(char c)
		{
			if (_newlineIsRequiredHere)
				Newline();
			if (_indexWhereNodeBegan == _s.Length)
				OnNodeStarting(c);
			_s.Append(c);
			return this;
		}
		/// <inheritdoc/>
		public virtual LNodePrinterHelper Write(string s)
		{
			if (s.Length != 0)
			{
				if (_newlineIsRequiredHere)
					Newline();
				if (_indexWhereNodeBegan == _s.Length)
					OnNodeStarting(s[0]);
				_s.Append(s);
			}
			return this;
		}
		/// <inheritdoc/>
		public virtual LNodePrinterHelper Write(UString s)
		{
			if (s.Length != 0)
			{
				if (_newlineIsRequiredHere)
					Newline();
				if (_indexWhereNodeBegan == _s.Length)
					OnNodeStarting(s[0]);
				_s.Append(s);
			}
			return this;
		}

		/// <summary>Synonym for Write(' ').</summary>
		public LNodePrinterHelper Space() => Write(' ');

		/// <summary>This method, which does nothing by default, is called by all three 
		/// versions of Write() when the first character is written after a call to
		/// <see cref="BeginNode"/>. You can override this method can detect conflicts
		/// between this character and the previous characters in the stream
		/// (<see cref="LastCharWritten"/>).</summary>
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
		protected virtual void OnNodeStarting(char firstChar) { }

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
			IndentLevel += changeIndentLevel;
			Contract.Assert(IndentLevel >= 0);
			return NewlineAfterCheckpoint();
		}

		/// <inheritdoc/>
		public virtual LNodePrinterHelper Newline(Symbol hint = null)
		{
			int oldLineStartIndex = _lineStartIndex;
			int newlineIndex = _s.Length;
			_s.Append(NewlineString);
			_lineNo++;
			_lineStartIndex = _s.Length;

			AppendIndentAfterNewline(hint);

			_lineStartAfterIndent = _s.Length;
			if (!_newlineIsRequiredHere && _revokableNewlines.InternalArray != null)
				_revokableNewlines.Add(new Revokable(oldLineStartIndex, newlineIndex, _s.Length - newlineIndex));
			_newlineIsRequiredHere = false;
			return this;
		}

		/// <summary>Called just after a newline is emitted to append indentation.</summary>
		/// <param name="hint">The hint that was passed to <see cref="Newline"/></param>
		protected virtual void AppendIndentAfterNewline(Symbol hint)
		{
			var curNodeKind = hint ?? _nodeStack[_nodeStack.Count - 1, default].C;
			for (int i = IndentLevel - (curNodeKind == PrinterIndentHint.Label ? 1 : 0); i > 0; i--)
				_s.Append(IndentString);
			if (curNodeKind == PrinterIndentHint.Label)
				_s.Append(LabelIndentString);
			else if (curNodeKind == PrinterIndentHint.Subexpression)
				_s.Append(SubexpressionIndentString);
		}

		/// <inheritdoc/>
		public virtual LNodePrinterHelper NewlineIsRequiredHere()
		{
			_newlineIsRequiredHere = true;
			return this;
		}

		/// <inheritdoc/>
		public virtual LNodePrinterHelper Indent() { IndentLevel++; return this; }
		/// <inheritdoc/>
		public virtual LNodePrinterHelper Dedent() { IndentLevel--; return this; }

		public virtual LNodePrinterHelper BeginNode(ILNode node, Symbol kind = null)
		{
			_indexWhereNodeBegan = _s.Length;
			_nodeStack.Add(Triplet.Create(node, _s.Length, kind));
			return this;
		}

		public virtual LNodePrinterHelper EndNode()
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
		/// EndNode has not been called the correct number of times.</summary>
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

			// Adjust
			if (_indexWhereNodeBegan > r._index)
				_indexWhereNodeBegan -= r._length;

			// Finally remove the character(s)
			_s.Remove(r._index, r._length);
			_lineStartIndex = r._oldLineStartIndex;
			_lineNo--;
		}

		ILNodePrinterHelper ILNodePrinterHelper<ILNodePrinterHelper>.Write(char c) => Write(c);
		ILNodePrinterHelper ILNodePrinterHelper<ILNodePrinterHelper>.Write(string s) => Write(s);
		ILNodePrinterHelper ILNodePrinterHelper<ILNodePrinterHelper>.Write(UString s) => Write(s);
		ILNodePrinterHelper ILNodePrinterHelper<ILNodePrinterHelper>.Newline(Symbol hint) => Newline(hint);
		ILNodePrinterHelper ILNodePrinterHelper<ILNodePrinterHelper>.NewlineIsRequiredHere() => NewlineIsRequiredHere();
		ILNodePrinterHelper ILNodePrinterHelper<ILNodePrinterHelper>.BeginNode(ILNode node, Symbol kind) => BeginNode(node, kind);
		ILNodePrinterHelper ILNodePrinterHelper<ILNodePrinterHelper>.EndNode() => EndNode();
		ILNodePrinterHelper ILNodePrinterHelper<ILNodePrinterHelper>.Indent() => Indent();
		ILNodePrinterHelper ILNodePrinterHelper<ILNodePrinterHelper>.Dedent() => Dedent();

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
