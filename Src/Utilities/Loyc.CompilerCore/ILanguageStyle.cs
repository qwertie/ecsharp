using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Runtime;
using Loyc.CompilerCore.ExprParsing;
using Loyc.CompilerCore.StmtParsing;

namespace Loyc.CompilerCore
{
	/// <summary>
	/// This interface groups together the various facilities that may be 
	/// provided by a language front-end, including lexical analysis and 
	/// parsing. Not all styles provide all facilities; for example, there
	/// can be quasi-languages such as a reader of dynamic-link libraries, 
	/// which will not expose any parsing abilities, only a file-loading 
	/// capability.
	/// </summary>
	public interface ILanguageStyle
	{
		/// <summary>Returns the name of the language that this style supports 
		/// (regardless of whether it is supported exactly or approximately),
		/// such as "boo", "C#" or "Ruby". The name is null if unknown.</summary>
		string LanguageName { get; }

		/// <summary>Returns a string that describes the version and/or dialect
		/// of the language that this language style is configured to understand,
		/// e.g. "2.0" or "gcc 2001". Null if not applicable.
		/// </summary>
		/// <remarks>
		/// Some language styles can be configured for multiple dialects. The 
		/// value of LanguageVersion can change if dialect-related settings are 
		/// changed on object that implements this interface.
		/// </remarks>
		string LanguageVersion { get; }

		/// <summary>
		/// Returns a read-only table of standard keywords for this language and 
		/// the corresponding token types.
		/// </summary>
		/// <returns>A map from keywords to token type symbols, or null if this 
		/// language style doesn't offer the concept of keywords. The dictionary
		/// cannot be modified: copy it before adding your own keywords.
		/// </returns>
		/// <remarks>
		/// The symbol for any keyword is just the keyword prefixed with an 
		/// underscore.
		/// </remarks>
		/// <seealso cref="LoycG.AddKeyword"/>
		IDictionary<string, Symbol> StandardKeywords { get; }

		/// <summary>Gets the lexical processor factory, or null if this language 
		/// style doesn't offer the concept of lexical analysis. null may be
		/// returned if you cannot use the lexer directly or there is no lexer 
		/// for the language.</summary>
		ILexingProvider NewLexingProvider();

		ILexingProvider DefaultLexingProvider { get; }

		/// <summary>Returns the IParsingProvider which provides facilities for 
		/// parsing statements and whole source files, or null if there is no
		/// parser for the language.</summary>
		IParsingProvider NewParsingProvider();

		/// <summary>AstNode and its derived classes must call this method on its
		/// own language style (AstNode.Language) when one of its lists changes.</summary>
		/// <param name="list">The list of children that changed.</param>
		/// <param name="firstIndex">The index where an item was changed, added or 
		/// removed.</param>
		/// <param name="changeType">the type of change, where :Set indicates a
		/// single index was modified, :Insert indicates Add() or Insert() was
		/// called, and :Remove indicates Remove(), RemoveAt() or Clear() was 
		/// called. changeType can have some other value to represent a change 
		/// that does not fit into the categories defined here.</param>
		void AstListChanged(AstList list, int firstIndex, Symbol changeType);

		/// <summary>Returns whether the specified node type is out-of-band,
		/// meaning it should be ignored by a parser. Depending on how it is
		/// configured, the lexer should either ignore OOB tokens, or the ETPtree
		/// parser should place OOB tokens in an Oob list.
		/// </summary>
		/// <param name="nodeType">Node type to be considered.</param>
		bool IsOob(Symbol nodeType);

		//ICollection<ISingleStmtParser<Stmt, Token>> NewStmtList { get; }
		//ICollection<ISingleStmtParser<Stmt, Token>> NewOperatorList { get; }
		//ICollection<ISingleStmtParser<Stmt, Token>> NewCustomAttrList { get; }
	}

	/// <summary>
	/// This interface specifies the lexing facilities that a full-featured 
	/// language style should provide.
	/// </summary>
	/// <remarks>
	/// In general, although the methods NewCoreLexer, NewLexer, 
	/// NewPreprocessor, and NewTreeParser return IEnumerable, the enumerators 
	/// can only be used once and cannot be reset. The user should not call 
	/// GetEnumerator() directly; instead, the next phase in the pipeline will 
	/// do so itself. If you want to enumerate the same stage more than once,
	/// fill a List(of AstNode) from the enumerator and enumerate the list
	/// repeatedly.
	/// 
	/// Implementations of this interface need not interact directly with any 
	/// extensions; it is the responsibility of the user of this class to 
	/// install any desired extensions into the compiler pipeline. In other
	/// words, ILanguageStyle only provides the standard built-in features of a
	/// language, not extension features. That said, a language style may 
	/// directly support standard features of Loyc, e.g. Symbols and identifiers 
	/// that contain escape sequences.
	/// </remarks>
	public interface ILexingProvider
	{
		/// <summary>
		/// Returns a new parser that can perform "core lexing" on the specified 
		/// source. The new lexer will recognize the specified list of keywords. 
		/// The meaning of "core lexing" may vary from language to language and 
		/// the concept may not be supported in all languages.
		/// </summary>
		/// <exception cref="NotSupportedException">Thrown if this language style 
		/// doesn't offer the concept of a core lexer. In that case, you can try
		/// calling NewLexer() instead with the same arguments.</exception>
		/// <remarks>
		/// A keyword may not be recognized if it does not meet the syntax of an
		/// identifier in the language.
		/// </remarks>
		IParseNext<AstNode> NewCoreLexer(ICharSource source);

		/// <summary>Returns a new object that can perform lexical analysis. Note:
		/// you may not be allowed to call GetEnumerator() more than once on the result.
		/// </summary>
		IEnumerable<AstNode> NewLexer(ICharSource source);

		/// <summary>Returns a new object that can perform lexical analysis. Note:
		/// you may not be allowed to call GetEnumerator() more than once on the result.
		/// </summary>
		IEnumerable<AstNode> NewLexer(IParseNext<AstNode> coreLexer);

		/// <summary>Returns a new object that preprocesses the input, if applicable,
		/// using the standard techniques of the language.</summary>
		/// <param name="lexer">Enumerable obtained (directly or indirectly) from 
		/// return value of NewLexer.</param>
		/// <returns>An enumerator that produces a preprocessed token stream, or
		/// 'lexer' if the language has no preprocessing phase.</returns>
		/// <remarks>
		/// If the language has no preprocessing phase, NewPreprocessor() simply 
		/// returns its argument.
		/// </remarks>
		IEnumerable<AstNode> NewPreprocessor(IEnumerable<AstNode> lexer);

		/// <summary>Converts the specified token stream to a token tree.</summary>
		/// <param name="preprocessedInput">Enumerable obtained (directly or 
		/// indirectly) from return value of NewPreprocessor.</param>
		/// <returns>An enumerator that enumerates through the highest-level nodes 
		/// of the token tree. If the language has no tree-parsing phase then 
		/// 'preprocessedInput' is returned.</returns>
		/// <remarks>
		/// Note to implementor: if desired, instantiate StandardTreeParser to 
		/// handle tree parsing for you.
		/// </remarks>
		AstNode MakeTokenTree(IEnumerable<AstNode> preprocessedInput);

		/// <summary>Converts the specified source file to a token tree.</summary>
		/// <param name="charSource"></param>
		/// <returns></returns>
		AstNode MakeTokenTree(ICharSource charSource);
	}
	// ok, the design for ILexingProvider is insufficient in case more 
	// configuration info than a list of keywords is required.
	public interface IParsingProvider
	{
		//IExtensibleParser NewMainParser();
		IStmtParserBuilder NewFileParser();
		IStmtParserBuilder NewMethodBodyParser();
		IStmtParserBuilder NewClassBodyParser();
		/// <summary>
		/// Returns a parser pre-configured to parse standard expressions of the 
		/// language.
		/// </summary>
		/// <returns>An IExprParser or IOneParser that can parse expressions. To find
		/// out whether the parser is extensible, try casting the return value to 
		/// IOneParser(of ICodeNode, IToken).</returns>
		IExprParser<AstNode, AstNode> NewExprParser();
	}
}
