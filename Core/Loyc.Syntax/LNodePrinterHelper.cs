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

		[Obsolete("PrinterState.Checkpoint was renamed to " + nameof(LNodePrinterOutputLocation))]
		public struct Checkpoint { }
	}

	/// <summary>A helper type for printer objects; for details, please see
	/// <see cref="ILNodePrinterHelper{S}"/>, its derived interface
	/// <see cref="ILNodePrinterHelperWithRevokableNewlines{S,C}"/>, and the documentation of the
	/// constructor <see cref="LNodePrinterHelper(StringBuilder, string, string, bool, string, string, int)"/>.</summary>
	public class LNodePrinterHelper : ILNodePrinterHelperWithRevokableNewlines<LNodePrinterOutputLocation, LNodePrinterHelper>
	{
		protected int _indentLevel;
		protected int _lineNo;
		private int _lineStartIndex;
		private int _lineStartAfterIndent;
		private bool _newlineIsRequiredHere;
		public string IndentString { get; set; }
		public string NewlineString { get; set; }
		protected StringBuilder _s;
		private InternalList<Revokable> _revokableNewlines;
		private InternalList<IndexRange> _unsavedRanges;
		private Action<ILNode, IndexRange> _saveRange;
		protected Stack<Pair<ILNode, int>> _startingIndexes;

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
		public bool IsAtStartOfLine => S.Length == _lineStartAfterIndent;
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

		public LNodePrinterHelper(StringBuilder s, string indent = "\t", string newline = "\n", bool allowNewlineRevocation = true, string labelIndent = "  ", string subexprIndent = "  ", int maxSubexprIndent = 1)
		{
			S = s ?? new StringBuilder();
			IndentLevel = 0;
			IndentString = indent;
			NewlineString = newline;
			_lineStartIndex = 0;
			_lineStartAfterIndent = 0;
			_lineNo = 1;
			if (allowNewlineRevocation)
			{
				_revokableNewlines = InternalList<Revokable>.Empty;
				_unsavedRanges = InternalList<IndexRange>.Empty;
			}
		}

		/// <inheritdoc/>
		public LNodePrinterHelper Write(char c) { S.Append(c); return this; }
		/// <inheritdoc/>
		public LNodePrinterHelper Write(string s) { S.Append(s); return this; }
		/// <inheritdoc/>
		public LNodePrinterHelper Write(UString s) { s.AppendTo(S); return this; }

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
		public LNodePrinterOutputLocation Newline(int changeIndentLevel)
		{
			IndentLevel += changeIndentLevel;
			Contract.Assert(IndentLevel >= 0);
			return NewlineWithCheckpoint();
		}

		/// <inheritdoc/>
		public LNodePrinterHelper Newline()
		{
			var r = new Revokable(_lineStartIndex, S.Length, NewlineString.Length + IndentString.Length * IndentLevel);
			S.Append(NewlineString);
			_lineNo++;
			_lineStartIndex = S.Length;
			for (int i = 0; i < IndentLevel; i++)
				S.Append(IndentString);
			_lineStartAfterIndent = S.Length;
			if (!_newlineIsRequiredHere && _revokableNewlines.InternalArray != null)
				_revokableNewlines.Add(r);
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
			return this;
		}

		public LNodePrinterHelper EndNode()
		{
			return this;
		}

		/// <inheritdoc/>
		public LNodePrinterOutputLocation GetCheckpoint() { 
			return new LNodePrinterOutputLocation { 
				_oldLineStart = _lineStartIndex, 
				_oldLineNo = _lineNo, 
				_oldLineStartAfterIndent = _lineStartAfterIndent
			};
		}

		/// <inheritdoc/>
		public LNodePrinterOutputLocation NewlineWithCheckpoint()
		{
			var cp = GetCheckpoint();
			Newline();
			return cp;
		}

		/// <inheritdoc/>
		public int RevokeOrCommitNewlines(LNodePrinterOutputLocation cp, int maxLineWidth)
		{
			// Length before revokation
			int nonNewlineLength = S.Length - cp._oldLineStart;
			// Figure out which newlines we can revoke and what the total 
			// length would be if they were revoked
			int i0;
			bool anyRevokables = false;
			for (i0 = _revokableNewlines.Count; i0 > 0 && _revokableNewlines[i0 - 1]._index >= cp._oldLineStart; i0--) {
				nonNewlineLength -= _revokableNewlines[i0].Length;
				anyRevokables = true;
			}

			if (anyRevokables)
			{
				var tooLong = false;
				if (nonNewlineLength > maxLineWidth)
				{
					// Line length appears to be exceeded, but it may be that there are 
					// non-revokable newlines so that the actual line length is lower.
					// Find out the maximum line length considering non-revokables.
					int i1 = i0, lineStart = cp._oldLineStart, charsRevocableOnThisLine = 0;
					for (int iChar = lineStart; iChar <= S.Length; iChar++)
					{
						while (iChar == _revokableNewlines[i1]._index) {
							charsRevocableOnThisLine += _revokableNewlines[i1].Length;
							iChar                    += _revokableNewlines[i1].Length;
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
			if (_revokableNewlines.InternalArray != null)
				_revokableNewlines.Clear();
			return count;
		}

		/// <inheritdoc/>
		public int RevokeNewlinesSince(LNodePrinterOutputLocation cp)
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
			Contract.Assert(_startingIndexes.Count == 0, "{0} calls to BeginNode without matching calls to EndNode".Localized(_startingIndexes.Count));
		}

		private int RevokeNewlinesStartingAtIndex(int i0)
		{
			int count = _revokableNewlines.Count - i0;
			for (int i = _revokableNewlines.Count - 1; i >= i0; i--) {
				Revoke(_revokableNewlines[i]);
				_revokableNewlines.RemoveAt(i);
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
			_lineNo--;
		}

		// Represents something (a newline) that can be revoked
		private struct Revokable
		{
			internal int _index;
			internal int _length; // length of newline + indent
			internal int _oldLineStartIndex;
			public Revokable(int oldNewlineIndex, int index, int length)
			{
				_oldLineStartIndex = oldNewlineIndex;
				_index = index;
				_length = length;
			}
			public int Length { get { return _length; } }
		}
	}
	
	/// <summary>A location in the output stream of <see cref="LNodePrinterHelper"/>.</summary>
	public struct LNodePrinterOutputLocation
	{
		internal int _oldLineStart; // at start of line
		internal int _oldLineStartAfterIndent;
		internal int _oldLineNo;
		public int LineNo { get { return _oldLineNo; } }
	}
}
