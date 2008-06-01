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
	/// Important: the LineToIndex() functions are not implemented.
	/// </remarks>
	public class BasicDividerSource<Token> : ISimpleSource2<TokenValueWrapper<Token>>
		where Token : ITokenValue
	{
		protected static readonly Symbol _PUNC = Symbol.Get("PUNC");

		/// <summary>
		/// Returns whether any multi-char PUNC tokens exist in the input.
		/// If this method returns false, it's not necessary to wrap the 
		/// source in a BasicDividerSource.
		/// </summary>
		/// <param name="source">Input token stream</param>
		/// <param name="startPosition">Index of first token to examine. 
		/// This method scans from there to the end</param>
		/// <param name="divider">Optional: an IOperatorDivider whose MayDivide method 
		/// is called to decide whether PUNC tokens need to be divided</param>
		public static bool MightNeedDivision(ISimpleSource<Token> source, int startPosition, int length, IOperatorDivider divider)
		{
			for (int i = startPosition; i < startPosition + length; i++)
				if (source[i].NodeType == _PUNC) {
					if ((divider != null && divider.MayDivide(source[i])) ||
						(divider == null && source[i].Text.Length > 1))
					return true;
				}
			return false;
		}

		/// <summary>Initializes a new instance of BasicDividerSource. It is 
		/// the caller's responsibility to set up the 'divider' by calling 
		/// IOperatorDivider.ProcessOperators().
		/// </summary>
		public BasicDividerSource(IOperatorDivider divider)
		{
			_divider = divider;
		}

		protected IOperatorDivider _divider;
		/// <summary>List of wrapper tokens that is used to present this stream
		/// to the user.</summary>
		protected List<TokenValueWrapper<Token>> _tokens = new List<TokenValueWrapper<Token>>();
		/// <summary>State information for all (potentially, but not necessarily) 
		/// divided tokens. This list is in order by position in the token list,
		/// such that _state[i].IndexInTokens > _state[i - 1].IndexInTokens.</summary>
		protected List<DividedOperatorState> _state = new List<DividedOperatorState>();
		/// <summary>Original token source that this object wraps around.</summary>
		protected ISimpleSource2<Token> _source;
		protected int _sourceStartPosition;
		protected int _sourceLength;        // Just for debugging

		protected struct DividedOperatorState
		{
			public Token Original;
			public IEnumerator<DividedOperatorPart> Generator;
			public int IndexInTokens;
			public int NumTokensInserted;
			public DividedOperatorState(Token original, IEnumerator<DividedOperatorPart> generator, int indexInTokens)
			{
				Original = original;
				Generator = generator;
				IndexInTokens = indexInTokens;
				NumTokensInserted = 0;
			}
		}

		/// <summary>Processes the specified source tokens to produce a new list
		/// of tokens in which punctuation is properly divided.
		/// </summary>
		/// <param name="source">Source stream</param>
		/// <param name="startPosition">Index of first token to process in source</param>
		/// <param name="length">Number of tokens to process in source</param>
		/// <remarks>It's important to note that if you do not request to process
		/// the entire stream, the new list provided by this object will only be a 
		/// subset of the old list--this object will not provide access to any tokens 
		/// that are outside the range specified here.</remarks>
		public void Process(ISimpleSource2<Token> source, int startPosition, int length)
		{
			Debug.Assert(source.Count <= startPosition + length);
			_tokens.Clear();
			_state.Clear();
			_source = source;
			_sourceStartPosition = startPosition;
			_sourceLength = length;

			for(int i = startPosition; i < startPosition + length; i++) 
			{
				Token t = source[i];
				string text = t.Text;
				if (t.NodeType == _PUNC && text != null && text.Length > 1) {
					DividedOperatorState dos = new DividedOperatorState(t, _divider.Divide(t), _tokens.Count);
					bool success = dos.Generator.MoveNext();
					Debug.Assert(success);
					_state.Add(dos);
					SwitchToNextInterpretation(dos);
				} else {
					_tokens.Add(new TokenValueWrapper<Token>(t));
				}
			}
			Debug.Assert(IndexInOriginalSource(Count-1) == startPosition+length-1);
		}

		/// <summary>Loads the next interpretation of the input, if any.</summary>
		/// <returns>True if another interpretation was found, false if not.</returns>
		/// <remarks>The implementation tries to switch to the next interpretation
		/// of the <b>last</b> divided operator. The reason this is done is an
		/// implementation detail: the token list may change length, and if any 
		/// divided token other than the last one were reinterpreted, the 
		/// IndexInTokens member of DividedOperatorState may become incorrect for 
		/// the divided tokens that are afterward.</remarks>
		public bool ProcessNextInterpretation()
		{
			while(_state.Count > 0) {
				DividedOperatorState state = _state[_state.Count - 1];
				if (SwitchToNextInterpretation(state))
					return true; // Success!
				_state.RemoveAt(_state.Count - 1);
			}
			return false;
		}

		/// <summary>Deletes the previous interpretation (if any) from _tokens and
		/// Inserts the next interpretation of the token into _tokens using the 
		/// generator in 'state'.</summary>
		/// <returns>Returns true on success or false if the generator was already
		/// exhausted.</returns>
		protected bool SwitchToNextInterpretation(DividedOperatorState state)
		{
			if (state.Generator == null)
				return false; // No more interpretations

			// Delete old interpretation
			while (state.NumTokensInserted > 0) {
				state.NumTokensInserted--;
				_tokens.RemoveAt(state.IndexInTokens);
			}

			Debug.Assert(state.Generator.Current.Offset == 0);

			for (int i = 0; ; i++) 
			{
				TokenValueWrapper<Token> token = new TokenValueWrapper<Token>(state.Original);
				token.Text = state.Generator.Current.Substring;
				_tokens.Insert(state.IndexInTokens + i, token);

				if (!state.Generator.MoveNext()) {
					state.Generator = null; // No more interpretations
					state.NumTokensInserted = i;
					return true;
				} else if (state.Generator.Current.Offset == 0) {
					// A new interpretation is starting. The previous one is over.
					state.NumTokensInserted = i;
					return true;
				}
			}
		}
		
		/// <summary>Returns the token source that was given to Process().</summary>
		public ISimpleSource<Token> OriginalSource { get { return _source; } }
		
		/// <summary>Returns the "true" index from the original source, given an
		/// artificial index in this source.
		/// </summary>
		public int IndexInOriginalSource(int index)
		{
			int offset = _sourceStartPosition;
			for (int i = 0; i < _state.Count; i++) {
				if (index <= _state[i].IndexInTokens)
					break;
				if (index < _state[i].IndexInTokens + _state[i].NumTokensInserted) {
					index = _state[i].IndexInTokens;
					break;
				}
				offset += _state[i].NumTokensInserted - 1;
			}
			return index + offset;
		}
		
		#region ISimpleSource interface
		public TokenValueWrapper<Token> this[int index] 
		{
			get { return index >= _tokens.Count ? null : _tokens[index]; }
		}
		public int Count 
		{
			get { return _tokens.Count; }
		}
		public SourcePos IndexToLine(int index)
		{
			int trueIndex = IndexInOriginalSource(index);
			Debug.Assert(index >= _tokens.Count ||
				object.ReferenceEquals(_source[trueIndex], _tokens[index].Original));
			return _source.IndexToLine(trueIndex);
		}
		#endregion

		#region IEnumerableT Members
		//System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		public IEnumerator<TokenValueWrapper<Token>> GetEnumerator()
		{
			for (int i = 0; i < Count; i++)
				yield return this[i];
		}
		#endregion
	}

	/// <summary>Used by BasicDividerSource to wrap tokens. Most of the time,
	/// the Type and Text of this wrapper is the same as the original object,
	/// but if the original token was divided then the Text is only a subset
	/// of Original.Text.</summary>
	public class TokenValueWrapper<Token> : TokenValue
		where Token : ITokenValue
	{
		public TokenValueWrapper(Token original)
			: base(original.NodeType, original.Text)
			{ _original = original; }
		protected Token _original;
		public Token Original
		{
			get { return _original; }
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
