using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Loyc.Collections;
using Loyc.Syntax.Lexing;

namespace Loyc.Syntax
{
	/// <summary>An implementation of the LLLPG Parser API, used with the LLLPG
	/// options <c>inputSource</c> and <c>inputClass</c>.</summary>
	/// <remarks>
	/// This derived class simply makes public all of the LLLPG APIs which are 
	/// marked protected in <see cref="BaseParserForList{Token,MatchType,List}"/>.
	/// </remarks>
	/// <typeparam name="Token">Data type of complete tokens in the token list. A 
	/// token contains the type of a "word" in the program (string, identifier, plus 
	/// sign, etc.), a value (e.g. the name of an identifier), and a range of 
	/// characters in the source file. See <see cref="ISimpleToken{MatchType}"/>.
	/// Note: Token is usually a small struct; this class does not expect it to
	/// ever be null.</typeparam>
	/// <typeparam name="MatchType">A data type, usually int, that represents a 
	/// token type (identifier, operator, etc.) and implements <see cref="IEquatable{T}"/>
	/// so it can be compared for equality with other token types; this is also the 
	/// type of the <see cref="ISimpleToken{Matchtype}.Type"/> property.</typeparam>
	/// <typeparam name="List">Data type of the list that contains the tokens (one 
	/// often uses IList{Token}, but one could use <see cref="Loyc.Collections.Impl.InternalList{T}"/> 
	/// for potentially higher performance.)</typeparam>
	public class ParserSource<Token,MatchType,List> : BaseParserForList<Token,MatchType,List>, ILllpgApi<Token, MatchType, MatchType>
		where Token : ISimpleToken<MatchType>
		where MatchType : IEquatable<MatchType>
		where List : IList<Token>
	{
		public new static HashSet<MatchType> NewSet(params MatchType[] items) { return new HashSet<MatchType>(items); }

		/// <inheridoc/>
		public ParserSource(List list, Token eofToken, ISourceFile file, int startIndex = 0) 
			: base(list, eofToken, file, startIndex) { }
		
		/// <inheritdoc/>
		public new virtual void Reset(List source, Token eofToken, ISourceFile file, int startIndex = 0)
		{
			base.Reset(source, eofToken, file, startIndex);
		}
		public new void Reset() { base.Reset(); }

		/// <summary>Converts from MatchType (usually integer) to string (used in 
		/// error messages).</summary>
		public Func<MatchType, string> TokenTypeToString { get; set; }

		protected override string ToString(MatchType tokenType)
		{
			if (TokenTypeToString == null)
				return tokenType.ToString();
			else
				return TokenTypeToString(tokenType);
		}

		public new List TokenList { get { return base._tokenList; } }
		public new Token LT0 { [DebuggerStepThrough] get { return _lt0; } }
		public new MatchType LA0 { get { return base.LA0; } }
		public new Token LT(int i) { return base.LT(i); }
		public new MatchType LA(int i) { return base.LA(i); }
		
		public new int InputPosition
		{
			get { return base.InputPosition; }
			set { base.InputPosition = value; }
		}

		public new ISourceFile SourceFile
		{
			get { return base.SourceFile; }
		}

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new void Skip() { base.Skip(); }

		#region Normal matching

		public new Token MatchAny() { return base.MatchAny(); }
		public new Token Match(MatchType a) { return base.Match(a); }
		public new Token Match(MatchType a, MatchType b) { return base.Match(a, b); }
		public new Token Match(MatchType a, MatchType b, MatchType c) { return base.Match(a, b, c); }
		public new Token Match(MatchType a, MatchType b, MatchType c, MatchType d) { return base.Match(a, b, c, d); }
		public     Token Match(HashSet<MatchType> set) { return base.Match(set); }
		public new Token MatchExcept() { return base.MatchExcept(); }
		public new Token MatchExcept(MatchType a) { return base.MatchExcept(a); }
		public new Token MatchExcept(MatchType a, MatchType b) { return base.MatchExcept(a, b); }
		public new Token MatchExcept(MatchType a, MatchType b, MatchType c) { return base.MatchExcept(a, b, c); }
		public new Token MatchExcept(MatchType a, MatchType b, MatchType c, MatchType d) { return base.MatchExcept(a, b, c, d); }
		public new Token MatchExcept(HashSet<MatchType> set) { return base.MatchExcept(set); }

		#endregion

		#region Try-matching

		public     bool TryMatch(HashSet<MatchType> set)            { return base.TryMatch(set); }
		public new bool TryMatch(MatchType a)                       { return base.TryMatch(a); }
		public new bool TryMatch(MatchType a, MatchType b)             { return base.TryMatch(a, b); }
		public new bool TryMatch(MatchType a, MatchType b, MatchType c)   { return base.TryMatch(a, b, c); }
		public new bool TryMatch(MatchType a, MatchType b, MatchType c, MatchType d) { return base.TryMatch(a, b, c, d); }
		public new bool TryMatchExcept()                         { return base.TryMatchExcept(); }
		public new bool TryMatchExcept(HashSet<MatchType> set)      { return base.TryMatchExcept(set); }
		public new bool TryMatchExcept(MatchType a)                 { return base.TryMatchExcept(a); }
		public new bool TryMatchExcept(MatchType a, MatchType b)       { return base.TryMatchExcept(a, b); }
		public new bool TryMatchExcept(MatchType a, MatchType b, MatchType c) { return base.TryMatchExcept(a, b, c); }
		public new bool TryMatchExcept(MatchType a, MatchType b, MatchType c, MatchType d) { return base.TryMatchExcept(a, b, c, d); }

		#endregion

		public new virtual void Check(bool expectation, string expectedDescr = "") { base.Check(expectation, expectedDescr); }

		void ILllpgApi<Token, MatchType, MatchType>.Error(int li, string fmt) { base.Error(li, fmt); }
		public new virtual void Error(int lookaheadIndex, string format, params object[] args) 
		{
			base.Error(lookaheadIndex, format, args);
		}
	}
	
	/// <summary>Alias for ParserSource{Token, int, IList{Token}}.</summary>
	/// <typeparam name="Token">Token type (one often uses <see cref="Loyc.Syntax.Lexing.Token"/>).</typeparam>
	/// <typeparam name="MatchType">Data type of the Type property of 
	/// <see cref="ISimpleToken{MatchType}"/> (often set to int).</typeparam>
	public class ParserSource<Token,MatchType> : ParserSource<Token,MatchType,IList<Token>> 
		where Token : ISimpleToken<MatchType>
		where MatchType : IEquatable<MatchType>
	{
		/// <inheridoc/>
		public ParserSource(IList<Token> list, Token eofToken, ISourceFile file, int startIndex = 0) 
			: base(list, eofToken, file, startIndex) { }
		/// <inheridoc/>
		public ParserSource(IEnumerable<Token> list, Token eofToken, ISourceFile file, int startIndex = 0) 
			: this(list.Buffered(), eofToken, file, startIndex) { }
		/// <inheridoc/>
		public ParserSource(IEnumerator<Token> list, Token eofToken, ISourceFile file, int startIndex = 0) 
			: this(list.Buffered(), eofToken, file, startIndex) { }
	}

	/// <summary>Alias for ParserSource{Token, int, IList{Token}}.</summary>
	/// <typeparam name="Token">Token type (one often uses <see cref="Loyc.Syntax.Lexing.Token"/>).</typeparam>
	public class ParserSource<Token> : ParserSource<Token, int>
		where Token : ISimpleToken<int>
	{
		/// <inheridoc/>
		public ParserSource(IList<Token> list, Token eofToken, ISourceFile file, int startIndex = 0) 
			: base(list, eofToken, file, startIndex) { }
		/// <inheridoc/>
		public ParserSource(IEnumerable<Token> list, Token eofToken, ISourceFile file, int startIndex = 0) 
			: this(list.Buffered(), eofToken, file, startIndex) { }
		/// <inheridoc/>
		public ParserSource(IEnumerator<Token> list, Token eofToken, ISourceFile file, int startIndex = 0) 
			: this(list.Buffered(), eofToken, file, startIndex) { }
	}
}
