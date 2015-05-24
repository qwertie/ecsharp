using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Syntax.Lexing;

namespace Loyc.Syntax
{
	/// <summary>
	/// An base class designed for parsers that use LLLPG (Loyc LL(k) Parser 
	/// Generator) and receive tokens from any <see cref="IEnumerator{Token}"/>.
	/// </summary>
	/// <remarks>
	/// This base class for LLLPG parsers simply requires an enumerator to work,
	/// and it has a small buffer to hold lookahead tokens. Old tokens are 
	/// forgotten, so this base class does not support backtracking (i.e. syntactic 
	/// predicates), but it can save memory. Please use <see cref="BaseParserForList"/> 
	/// if your input sequence comes in the form of a list.
	/// <para/>
	/// This version of BaseParser has Enumerator as a generic parameter. Compared 
	/// to using IEnumerator{Token} directly, this can increase performance in case 
	/// Enumerator is a value type (e.g. <c>List&lt;Token>.Enumerator</c>).
	/// <para/>
	/// (I wrote this class by mistake... I actually forgot about backtracking!)
	/// </remarks>
	public abstract class BaseParserNoBacktracking<Token, Enumerator> : BaseParser<Token>
		where Token : ISimpleToken
		where Enumerator : IEnumerator<Token>
	{
		/// <summary>Initializes this object to begin parsing the specified tokens.</summary>
		/// <param name="sequence">A list of tokens to be parsed.</param>
		/// <param name="eofToken">A token value to return when the input position 
		/// reaches the end of the token list.</param>
		/// <param name="file">A source file object that will be returned by the <see cref="SourceFile"/>
		/// property. By default, this object is used to get the file name, line 
		/// number and column number shown in parser errors. If you are using 
		/// <see cref="BaseLexer"/>, you can get this object from the
		/// <see cref="BaseLexer{C}.SourceFile"/> property. The <see cref="SourceFile"/>.
		/// property (in this class) will return this value. It can be null, which
		/// means that default error messages will show the character index instead
		/// of the file, line number and column number.</param>
		/// <param name="startIndex">The initial value of the InputPosition property.
		/// This is informational only, and has no effect on the behavior of this 
		/// class.</param>
		protected BaseParserNoBacktracking(Enumerator sequence, Token eofToken, ISourceFile file, int startIndex = 0) : base(file, startIndex)
		{
			Reset(sequence, eofToken, file);
		}
		protected void Reset(Enumerator sequence, Token eofToken, ISourceFile file, int startIndex = 0)
		{
			_sequence = sequence;
			EofToken = eofToken;
			EOF = EofToken.TypeInt;
			_tokenBuffer.Resize(0);
			_sourceFile = file;
			_inputPosition = startIndex;
			base._inputPosition = startIndex;
		}

		protected Token EofToken;
		protected Int32 EOF; // EofToken.TypeInt

		// Holds lookahead tokens only, starting at LT(0)
		private InternalDList<Token> _tokenBuffer = new InternalDList<Token>(8);
		Enumerator _sequence; // Remaining tokens beyond the end of _tokenBuffer.
		bool _sequenceEnded; // Becomes true after MoveNext() returns false.
		protected new int _inputPosition; // hack related to BaseParser.InputPosition being nonvirtual

		protected sealed override Int32 EofInt() { return EOF; }
		protected sealed override Int32 LA0Int { get { return _lt0.TypeInt; } }
		/// <summary>Returns the Token at lookahead i, where 0 is the next token.
		/// This class does not support negative lookahead because old tokens from 
		/// the IEnumerator are discarded.</summary>
		protected sealed override Token LT(int i) {
			if (_inputPosition != base._inputPosition) // We are probably being called by BaseParser.InputPosition.set
				InputPosition = base._inputPosition;
			if ((uint)i >= (uint)_tokenBuffer.Count) {
				if (i < 0)
					throw new InvalidOperationException(string.Format(
						"BaseParser.LT({0}): negative lookahead is not supported (old tokens are discarded)", i));
				do {
					if (!ReadNextFromSequence())
						return EofToken;
				} while (i >= _tokenBuffer.Count);
			}
			return _tokenBuffer[i];
		}
		bool ReadNextFromSequence()
		{
			if (_sequenceEnded)
				return false;
			if (!_sequence.MoveNext()) {
				_sequenceEnded = true;
				return false;
			}
			_tokenBuffer.Add(_sequence.Current);
			return true;
		}

		/// <summary>Returns a string representation of the specified token type.
		/// These strings are used in error messages.</summary>
		protected override abstract string ToString(Int32 tokenType);

		/// <summary>Cumulative index of the next token to be parsed.</summary>
		/// <remarks>This class doesn't care what the absolute InputPosition is, 
		/// since it reads from an <see cref="IEnumerator{Token}"/>. This property
		/// is constrained to always increase, since old tokens are forgotten.</remarks>
		protected new int InputPosition
		{
			[DebuggerStepThrough]
			get { return _inputPosition; }
			set {
				int dif = value - _inputPosition;
				_inputPosition = base._inputPosition = value;
				if (dif >= _tokenBuffer.Count) {
					// optimize common case (which is dif=1)
					_tokenBuffer.PopFirst(dif);
				} else {
					if (dif < 0) {
						_inputPosition -= dif; // restore old value
						throw new InvalidStateException(Localize.From(
							"BaseParser.InputPosition can only increase. Unable to decrease from {0} to {1}.", _inputPosition, value));
					}
					if (dif > _tokenBuffer.Count)
						LT(dif); // try to read more tokens into the buffer
					_tokenBuffer.PopFirst(System.Math.Min(dif, _tokenBuffer.Count));
				}
				if (_tokenBuffer.IsEmpty && !ReadNextFromSequence())
					_lt0 = EofToken;
				else
					_lt0 = _tokenBuffer[0];
			}
		}
	}
	
	/// <summary>
	/// An base class designed for parsers that use LLLPG (Loyc LL(k) Parser 
	/// Generator) and receive tokens from an <see cref="IEnumerator{Token}"/>.
	/// </summary>
	/// <seealso cref="BaseParserNoBacktracking{Token, Enumerator}"/>
	public abstract class BaseParserNoBacktracking<Token> : BaseParserNoBacktracking<Token, IEnumerator<Token>>
		where Token : ISimpleToken
	{
		protected BaseParserNoBacktracking(IEnumerable<Token> sequence, Token eofToken, ISourceFile file)
			: this(sequence.GetEnumerator(), eofToken, file) { }
		protected BaseParserNoBacktracking(IEnumerator<Token> sequence, Token eofToken, ISourceFile file)
			: base(sequence, eofToken, file) { }
	}
}
