using System;
using System.Collections.Generic;
using Loyc.Runtime;
using Loyc.Utilities;
using System.Collections;
using NUnit.Framework;
using System.Diagnostics;

namespace Loyc.CompilerCore.ExprParsing
{
	/// <summary>Given a token source and an IOperatorDivider, this class 
	/// produces a new view on the source data in which the tokens have been
	/// broken down into subtokens.</summary>
	/// <remarks>
	/// This is a stopgap class for use until I write a complete version...
	/// What's incomplete about it, you ask? It doesn't support multiple
	/// interpretations of the input tokens! It just uses the first interpretation
	/// of each token from the IOperatorDivider.
	/// </remarks>
	public class IncompleteDividerSource<Token> : ISourceFile<Token>
		where Token : ITokenValue
	{
		protected static readonly Symbol _PUNC = Symbol.Get("PUNC");

		/// <summary>
		/// Returns whether any dividable PUNC tokens exist in the input.
		/// If this method returns false, it's not necessary to wrap the 
		/// source in a BasicDividerSource.
		/// </summary>
		/// <param name="source">Input token stream</param>
		/// <param name="startPosition">Index of first token to examine. 
		/// This method scans from there to the end</param>
		/// <param name="divider">Optional: an IOperatorDivider whose Consider method 
		/// is called to decide whether PUNC tokens need to be divided</param>
		public static bool MightNeedDivision(ISimpleSource<Token> source, int startPosition, int length, IOperatorDivider<Token> divider)
		{
			for (int i = startPosition; i < startPosition + length; i++)
				if (source[i].NodeType == _PUNC) {
					if (divider != null) {
						if (divider.MayDivide(source[i])) return true;
					} else {
						if (source[i].Text.Length > 1) return true;
					}
				}
			return false;
		}

		/// <summary>Initializes a new instance of BasicDividerSource. It is 
		/// the caller's responsibility to set up the 'divider' by calling 
		/// IOperatorDivider.ProcessOperators().
		/// </summary>
		public IncompleteDividerSource(IOperatorDivider<Token> divider)
		{
			_divider = divider;
		}

		protected IOperatorDivider<Token> _divider;
		/// <summary>List of wrapper tokens that is used to present this stream
		/// to the user.</summary>
		protected List<Token> _tokens = new List<Token>();
		protected List<Pair<int, int>> _dividedSizes = new List<Pair<int,int>>();
		/// <summary>Original token source that this object wraps around.</summary>
		protected ISourceFile<Token> _source;
		protected int _sourceStartPosition;
		protected int _sourceLength;        // Just for debugging

		/// <summary>Processes the specified source tokens to produce a new list
		/// of tokens in which punctuation is properly divided.
		/// </summary>
		/// <param name="source">Source stream</param>
		/// <param name="startPosition">Index of first token to process in source</param>
		/// <param name="length">Number of tokens to process in source</param>
		/// <remarks>It's important to note that if you do not request to process
		/// the entire stream, the new list provided by this object will only be a 
		/// subset of the old list--this object will not provide access to any tokens 
		/// that are outside the range specified here. The token at this[0] will
		/// correspond to source[startPosition].</remarks>
		public void Process(ISourceFile<Token> source, int startPosition, int length)
		{
			Debug.Assert(source.Count <= startPosition + length);
			_tokens.Clear();
			_source = source;
			_sourceStartPosition = startPosition;
			_sourceLength = length;
			
			List<Token> tList = new List<Token>(3);
			for(int i = startPosition; i < startPosition + length; i++) 
			{
				Token t = source[i];
				if (t.NodeType == _PUNC && _divider.MayDivide(t)) {
					_divider.Divide(t).GetNext(tList);
					_dividedSizes.Add(new Pair<int, int>(_tokens.Count, tList.Count));
					foreach (Token t2 in tList)
						_tokens.Add(t2);
				} else {
					_tokens.Add(t);
				}
			}
			Debug.Assert(IndexInOriginalSource(Count-1) == startPosition+length-1);
		}

		/// <summary>Loads the next interpretation of the input, if any.</summary>
		/// <returns>True if another interpretation was found, false if not.</returns>
		public bool ProcessNextInterpretation()
		{
			return false;
		}
		
		/// <summary>Returns the token source that was given to Process().</summary>
		public ISimpleSource<Token> OriginalSource { get { return _source; } }
		
		/// <summary>Returns the "true" index from the original source, given an
		/// artificial index in this source.
		/// </summary>
		public int IndexInOriginalSource(int index)
		{
			int offset = _sourceStartPosition;
			for (int i = 0; i < _dividedSizes.Count; i++) {
				if (index <= _dividedSizes[i].A)
					break;
				if (index < _dividedSizes[i].A + _dividedSizes[i].B) {
					index = _dividedSizes[i].A;
					break;
				}
				offset += _dividedSizes[i].B - 1;
			}
			return index + offset;
		}
		
		#region ISimpleSource interface
		public Token this[int index] 
		{
			get { return (uint)index >= (uint)_tokens.Count ? default(Token) : _tokens[index]; }
		}
		public int Count 
		{
			get { return _tokens.Count; }
		}
		public SourcePosition IndexToLine(int index)
		{
			int trueIndex = IndexInOriginalSource(index);
			Debug.Assert((uint)index >= (uint)_tokens.Count ||
				object.ReferenceEquals(_source[trueIndex], _tokens[index]));
			return _source.IndexToLine(trueIndex);
		}
		#endregion

		//System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		public IEnumerator<Token> GetEnumerator()
		{
			for (int i = 0; i < Count; i++)
				yield return this[i];
		}

		public string FileName { get { throw NotImplementedException(); } }
		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}
	}

	[TestFixture]
	public class BasicDividerSourceTest
	{
		public BasicDividerSourceTest()
		{
			// TODO: Get a list of operators
			Debug.Fail("TODO");
		}

		[Test]
		public void DoTest()
		{
		}
	}
}
