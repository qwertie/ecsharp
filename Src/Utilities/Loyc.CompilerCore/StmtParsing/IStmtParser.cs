using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Runtime;

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
	public interface ISingleStmtParser
	{
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
		/// <para/>
		/// The parser selector may attempt to determine priorities in advance or it may 
		/// call this method only when an ambiguity arises at an actual statement. 
		/// <para/>
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
		int ComparePriority(ISingleStmtParser other);
		
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
		bool IsAcceptable(IList<IStmtAttribute> attributes, AstList source, int mainStartPosition, ISimpleMessageSink output);
		
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
		AstNode Generate(IList<IStmtAttribute> attributes, AstList source, ref int position);
	}

	/// <summary>
	/// This interface provides a way to add custom statements, attributes 
	/// and operators to a parser.
	/// </summary>
	public interface IStmtParserBuilder : ICollection<ISingleStmtParser>
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
		IParseNext<AstNode> GetParser(ISourceFile<AstNode> source, int startPosition);
	}
}
