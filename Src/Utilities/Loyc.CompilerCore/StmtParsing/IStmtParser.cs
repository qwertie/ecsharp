using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Runtime;

namespace Loyc.CompilerCore
{
	/// <summary>Interface for a predicate that can be tested against tokens.</summary>
	public interface ITokenMatcher
	{
		/// <summary>Token type that is required to match, e.g. Tokens.ML_COMMENT; 
		/// Tokens.Null may match tokens of more than one type.</summary>
		Symbol Type { get; }
		
		/// <summary>Token text to match, e.g. "==", or null if more than one 
		/// string (or any string) can match.</summary>
		string Text { get; }
		
		/// <summary>Examines a token and decides whether it matches</summary>
		/// <returns>True if token 't' was a match.</returns>
        /// <remarks>If nether Type nor Text are null, the match criteria are fully
        /// specified so Match() must return
        /// <c>t.Type == Type &amp;&amp; t.Text == Text</c>.
        /// </remarks>
        bool Match(ITokenValue t);
	}

	/// <summary>Standard token matcher: matches a specific token type, specific token text,
	/// or both. Additionally, if Type.IsNull and Text == null then any token matches.</summary>
	public struct TokenMatcher : ITokenMatcher
	{
		public TokenMatcher(Symbol type, string text)
			{ _type = type; _text = text; }
		public TokenMatcher(Symbol type)
			{ _type = type; _text = null; }
		public TokenMatcher(string text)
			{ _type = null; _text = text; }

		private Symbol _type;
		private string _text;

		/// <summary>Token type to match, e.g. Tokens.ML_COMMENT; Tokens.Null matches 
		/// any type of token with the correct text.</summary>
		public Symbol Type { get { return _type; } set { _type = value; } }
		
		/// <summary>Content to match, e.g. "==", or null if any text is acceptable.</summary>
		public string Text { get { return _text; } set { _text = value; } }

		public bool Match(ITokenValue t)
		{
			if (t == null || (Type != t.NodeType && Type != null))
				return false;
			return Text == null || Text == t.Text;
		}
		public override string ToString()
		{
			if (Text != null)
				return string.Format("{0}\"{1}\"", Type, Text);
			else
				return Type.ToString();
		}
		public override bool Equals(object b_) 
		{
			ITokenMatcher b = b_ as ITokenMatcher;
			return b != null && b.Text == Text && b.Type == Type;
		}
		public static bool operator ==(TokenMatcher a, TokenMatcher b)
		{
			return a.Text == b.Text && a.Type == b.Type;
		}
		public static bool operator !=(TokenMatcher a, TokenMatcher b)
		{
			return !(a == b); 
		}
		public override int GetHashCode() 
		{
			int hc = Type.Id; 
			if (Text != null) 
				hc += Text.GetHashCode();
			return hc;
		}
	}
}

namespace Loyc.CompilerCore.StmtParsing
{
	/// <summary>This interface is implemented by parsed statement attributes
	/// whose meaning is not necessarily resolved yet. Objects that implement
	/// this interface are given to <see cref="ISingleStmtParser{Stmt,Token}.IsAcceptable"/>
	/// and <see cref="ISingleStmtParser<Stmt,Token>.Generate"/> to let those 
	/// methods know what attributes have been attached to the statement.
	/// </summary><remarks>
	/// The "Type" member should return :CustomAttribute if the attribute 
	/// had the custom attribute syntax. For keyword attributes, the value for
	/// Type is not yet standardized. TODO: standardize.
	/// </remarks>
	public interface IStmtAttribute : ITokenValue
	{
		/// <summary>This is the name part of the attribute, as written in code
		/// (before namespace resolution has been performed). For example, the 
		/// .NET attribute <c>[Serializable]</c> has a LiteralName of 
		/// "Serializable", rather than the resolved name which is
		/// "System.SerializableAttribute".</summary>
		string LiteralName { get; }
	}

	/// <summary>
	/// Interface for a class that supplies an operator specification to an IOneParser.
	/// </summary>
	public interface ISingleStmtParser<Stmt,Token> 
	{
		/// <summary>The type identifier of statements produced by this parser.</summary>
		/// <remarks>This member is informational. It should match the Type of 
		/// statements produced by Generate(), unless Generate() may produce more
		/// than one type of result.</remarks>
		Symbol Type { get; }

		/// <summary>Returns whether this operator can be prefixed by "keyword attributes"</summary>
		bool AllowKeywordAttributes { get; }
		
		/// <summary>Returns whether this operator can be prefixed by "custom attributes"</summary>
		bool AllowCustomAttributes { get; }
		
		/// <summary>Returns the "Prefix", a matcher that matches any acceptable first 
		/// token of the statement (after the attribute list, if any).</summary>
		ITokenMatcher Prefix { get; }

		/// <summary>Determines whether this statement should take priority over the 
		/// 'other' statement.</summary>
		/// <returns>1 if this statement takes priority over the other, -1 if the other 
		/// statement has higher priority, and 0 if this statement is neutral about the 
		/// matter.</returns>
		/// <remarks>ComparePriority() has the opportunity to disambiguate this 
		/// statement with others, but the other statement is given the same 
		/// opportunity. If both ComparePriority methods return the same value, the 
		/// conflict remains unresolved, but if the two methods are in agreement, or 
		/// if one is neutral (returning 0) while the other is not, then ambiguity 
		/// between the two statements is resolved.
		/// 
		/// The parser selector may attempt to determine priorities in advance or it may 
		/// call this method only when an ambiguity arises at an actual statement. 
		/// 
		/// ComparePriority() is invoked to resolve ambiguities only in case two 
		/// interpretations start at the same point, so there is no disambiguation 
		/// mechanism between a statement and a clause; for example, if the user 
		/// writes <c>if (foo) f(); else g();</c> in C#, and some weirdo comes along
		/// and defines a new "else" statement, then the "if" statement automatically
		/// takes priority because the parser for the "if" statement has total 
		/// control over parsing until the statement is over. It consumes the "else"
		/// clause itself without knowing or caring that there is another statement
		/// that starts with "else". Similarly, the "dangling else" ambiguity, shown
		/// here...
		/// <code>
		/// if (a)
		///	if (b)
		///		f();
		///	else
		///		g();
		/// </code>
		/// ...is solved automatically by the fact that the <c>if (b)</c> statement 
		/// has control over parsing when "else" is encountered, so it will consume
		/// the "else" clause without making use of any ComparePriority() methods.
		/// </remarks>
		int ComparePriority(ISingleStmtParser<Stmt,Token>  other);
		
		/// <summary>An parser selector uses this method in case of ambiguity to query
		/// whether an interpretation is valid.</summary>
		/// <param name="attributes">List of attributes that were in front of the 
		/// statement, if any. May be null.</param>
		/// <param name="source">A source of tokens to be parsed. The tokens will 
		/// be in tree form if the language style (of the language being parsed)
		/// performs tree parsing.</param>
		/// <param name="mainStartPosition">Index into 'source', after any attributes, 
		/// where the statement starts.</param>
		/// <param name="output">Errors and warnings about a failure should be sent 
		/// to this object. The messages will not be output unless disambiguation 
		/// fails.</param>
		/// <returns></returns>
		/// <remarks>
		/// The parser selector need not call this method if there is no ambiguity, 
		/// but it is allowed to.
		/// </remarks>
		bool IsAcceptable(IList<IStmtAttribute> attributes, ISimpleSource2<Token> source, int mainStartPosition, ISimpleMessageSink output);
		
		/// <summary>Generates a statement node by parsing the statement. This 
		/// method should return null (not throw an exception) to reject the 
		/// syntax of the statement. Syntax errors should be output using 
		/// <see cref="Message"/>.
		/// </summary>
		/// <param name="attributes">List of attributes that were in front of the 
		/// statement, if any. May be null.</param>
		/// <param name="source">A source of tokens to be parsed. The tokens will 
		/// be in tree form if the language style (of the language being parsed)
		/// performs tree parsing.</param>
		/// <param name="position">On entry, 'position' is the index into 'source'
		/// of the token at which to begin parsing. On exit, it is the first index
		/// after the parsed statement. If parsing fails, position should be the
		/// index of the token that caused parsing to fail, but if the parser 
		/// doesn't know the failure location, it can leave position unchanged.</param>
		/// <remarks>The IOneParser only calls Generate() on the outermost expression;
		/// it is the respoisibility of each operator to call Generate() on its own 
		/// arguments to generate the subexpressions, if applicable.</remarks>
		Stmt Generate(IList<IStmtAttribute> attributes, ISimpleSource2<Token> source, ref int position);
	}

	/// <summary>
	/// This interface provides a way to add custom statements, attributes 
	/// and operators to a parser.
	/// </summary>
	/// <typeparam name="Stmt"></typeparam>
	/// <typeparam name="Token"></typeparam>
	public interface IStmtParserBuilder<Stmt,Token> : ICollection<ISingleStmtParser<Stmt,Token>>
	{
		/// <summary>Returns a new parser selector.</summary>
		/// <param name="source">A source of tokens to be parsed. The tokens must 
		/// be in tree form if the language style (to which this parser belongs) 
		/// performs tree parsing.</param>
		/// <param name="startPosition">The index into 'source' of the token at 
		/// which to begin parsing.</param>
		/// <returns>An IParseNext object that can perform parsing.</returns>
		/// <remarks>The returned parser may or may not be stateful, meaning its 
		/// response to a given input may change after some statements are
		/// parsed. For example, in C, unlike in C++ or C#, all variable 
		/// declarations must come before all executable statements. So if this 
		/// IStmtParserSelector parses C function bodies, and it is given the 
		/// following code:
		/// <code>
		/// int x;
		/// x = 2;
		/// int y;
		/// </code>
		/// Then it will produce a syntax error on the third line, because it 
		/// keeps track of whether declarations are allowed. A C++ parser, on the 
		/// other hand, may be stateless in this respect (all three lines would 
		/// be legal).
		/// 
		/// The returned parser may or may not be linked to the list stored in
		/// this IStmtParserBuilder: it may use the collection represented by 
		/// this object or it may make a copy of the information in this object.
		/// Given this uncertainty, you shouldn't change the collection after 
		/// calling GetParser.
		/// </remarks>
		IParseNext<Stmt> GetParser(ISimpleSource2<Token> source, int startPosition);
	}
}
