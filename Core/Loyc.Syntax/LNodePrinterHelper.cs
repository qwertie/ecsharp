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
	public class LNodePrinterHelper : ILNodePrinterHelperWithRevokableNewlines<LNodePrinterHelperLocation, LNodePrinterHelper>
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
		protected StringBuilder _s;
		private InternalList<Revokable> _revokableNewlines;
		private InternalList<Pair<ILNode, IndexRange>> _unsavedRanges;
		private InternalList<Triplet<ILNode, int, Symbol>> _nodeStack;
		private Action<ILNode, IndexRange> _saveRange;

		public StringBuilder S
		{
			get => _s;
			set => _s = value ?? throw new ArgumentNullException(nameof(S));
		}
		public int LineStartIndex => _lineStartIndex;
		public int IndexInCurrentLine => S.Length - _lineStartIndex;
		public int IndexInCurrentLineAfterIndent => S.Length - _lineStartAfterIndent;
		[Obsolete("Renamed to IsAtStartOfLine")]
		public bool AtStartOfLine => IsAtStartOfLine;
		public bool IsAtStartOfLine => S.Length == _lineStartAfterIndent || _newlineIsRequiredHere;
		public char LastCharWritten => S.TryGet(S.Length - 1, '\uFFFF');
		public int LineWidth => S.Length - _lineStartIndex;
		public int LineNumber => _lineNo;
		public int IndentLevel
		{
			get => _indentLevel;
			set {
				CheckParam.IsNotNegative(nameof(IndentLevel), value);
				_indentLevel = value;
			}
		}
		public Action<ILNode, IndexRange> SaveRange
		{
			get => _saveRange;
			set => _saveRange = value;
		}

		public LNodePrinterHelper(StringBuilder s, string indent = "\t", string newline = "\n", bool allowNewlineRevocation = true, string labelIndent = "  ", string subexprIndent = "\t\t")
		{
			S = s ?? new StringBuilder();
			IndentLevel = 0;
			IndentString = indent;
			NewlineString = newline;
			LabelIndentString = labelIndent;
			SubexpressionIndentString = subexprIndent;
			_lineStartIndex = 0;
			_lineStartAfterIndent = 0;
			_lineNo = 1;
			_nodeStack = InternalList<Triplet<ILNode, int, Symbol>>.Empty;
			if (allowNewlineRevocation)
			{
				_revokableNewlines = InternalList<Revokable>.Empty;
				_unsavedRanges = InternalList<Pair<ILNode, IndexRange>>.Empty;
			}
		}

		/// <inheritdoc/>
		public LNodePrinterHelper Write(char c)
		{
			if (_newlineIsRequiredHere)
				Newline();
			S.Append(c);
			return this;
		}
		/// <inheritdoc/>
		public LNodePrinterHelper Write(string s)
		{
			if (_newlineIsRequiredHere && s.Length != 0)
				Newline();
			S.Append(s);
			return this;
		}
		/// <inheritdoc/>
		public LNodePrinterHelper Write(UString s)
		{
			if (_newlineIsRequiredHere && s.Length != 0)
				Newline();
			S.Append(s);
			return this;
		}

		[Obsolete("This method was renamed to Write(s)")]
		public StringBuilder Append(string s) { Write(s); return S; }
		[Obsolete("This method was renamed to Write(s)")]
		public StringBuilder Append(UString s) { Write(s); return S; }

		/// <summary>Current length of the output string. This length can decrease if 
		/// newlines are revoked.</summary>
		[Obsolete("Please call " + nameof(S) + ".Length instead")]
		public int Length => S.Length;

		/// <summary>Older version of Newline method, which returns a checkpoint instead of <c>this</c>.</summary>
		/// <param name="changeIndentLevel">Amount by which to change the indent level (positive, negative or zero).</param>
		public LNodePrinterHelperLocation Newline(int changeIndentLevel)
		{
			IndentLevel += changeIndentLevel;
			Contract.Assert(IndentLevel >= 0);
			return NewlineWithCheckpoint();
		}

		/// <inheritdoc/>
		public LNodePrinterHelper Newline()
		{
			int oldLineStartIndex = _lineStartIndex;
			int newlineIndex = S.Length;
			S.Append(NewlineString);
			_lineNo++;
			_lineStartIndex = S.Length;

			var curNodeKind = _nodeStack[_nodeStack.Count - 1, default].C;
			for (int i = IndentLevel - (curNodeKind == PrinterIndentHint.Label ? 1 : 0); i > 0; i--)
				S.Append(IndentString);
			if (curNodeKind == PrinterIndentHint.Label)
				S.Append(LabelIndentString);
			else if (curNodeKind == PrinterIndentHint.Subexpression)
				S.Append(SubexpressionIndentString);

			_lineStartAfterIndent = S.Length;
			if (!_newlineIsRequiredHere && _revokableNewlines.InternalArray != null)
				_revokableNewlines.Add(new Revokable(oldLineStartIndex, newlineIndex, S.Length - newlineIndex));
			_newlineIsRequiredHere = false;
			return this;
		}

		/// <inheritdoc/>
		public LNodePrinterHelper NewlineIsRequiredHere()
		{
			_newlineIsRequiredHere = true;
			return this;
		}

		/// <inheritdoc/>
		public LNodePrinterHelper Indent() { IndentLevel++; return this; }
		/// <inheritdoc/>
		public LNodePrinterHelper Dedent() { IndentLevel--; return this; }

		public LNodePrinterHelper BeginNode(ILNode node, Symbol kind)
		{
			_nodeStack.Add(Triplet.Create(node, S.Length, kind));
			return this;
		}

		public LNodePrinterHelper EndNode()
		{
			var triplet = _nodeStack.Last;
			_nodeStack.RemoveAt(_nodeStack.Count - 1);
			if (_saveRange != null)
			{
				var range = new IndexRange(triplet.B) { EndIndex = S.Length };
				if (_revokableNewlines.InternalArray == null)
					_saveRange(triplet.A, range);
				else
					_unsavedRanges.Add(Pair.Create(triplet.A, range));
			}
			return this;
		}

		/// <inheritdoc/>
		public LNodePrinterHelperLocation GetCheckpoint() { 
			return new LNodePrinterHelperLocation { 
				_oldLineStart = _lineStartIndex, 
				_oldLineNo = _lineNo, 
				_oldLineStartAfterIndent = _lineStartAfterIndent
			};
		}

		/// <inheritdoc/>
		public LNodePrinterHelperLocation NewlineWithCheckpoint()
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
			int lengthWithoutNewlines = S.Length - cp._oldLineStart;
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
					for (int iChar = lineStart; iChar <= S.Length; iChar++)
					{
						while (i1 < _revokableNewlines.Count && iChar == _revokableNewlines[i1]._index) {
							charsRevocableOnThisLine += _revokableNewlines[i1].Length;
							iChar                    += _revokableNewlines[i1].Length;
							i1++;
						}
						if (iChar == S.Length || S[iChar] == '\n') {
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
					return CommitNewlines();
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
		public int CommitNewlines()
		{
			int count = _revokableNewlines.Count;
			if (_revokableNewlines.InternalArray != null) {
				_revokableNewlines.Clear();
				SaveRanges();
			}
			return count;
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
				foreach (var pair in _unsavedRanges)
					_saveRange(pair.A, pair.B);

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

			// Finally remove the character(s)
			S.Remove(r._index, r._length);
			_lineStartIndex = r._oldLineStartIndex;
			_lineNo--;
		}

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
