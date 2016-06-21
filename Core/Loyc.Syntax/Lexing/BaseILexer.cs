using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;

namespace Loyc.Syntax.Lexing
{
	/// <summary>A version of <see cref="BaseLexer{CharSrc}"/> that implements 
	/// <see cref="ILexer{Token}"/>. You should use this base class if you want to 
	/// wrap your lexer in a postprocessor such as <see cref="IndentTokenGenerator"/> 
	/// or <see cref="TokensToTree"/>. It can also be used with the <see 
	/// cref="LCExt.Buffered"/> extension method to help feed data to your parser.
	/// </summary>
	/// <remarks>
	/// Important: the derived class must call <see cref="AfterNewline()"/> after
	/// encountering a newline (CR/LF/CRLF), in order to keep the properties 
	/// <see cref="BaseLexer{C}.LineNumber"/>, <see cref="BaseLexer{C}.LineStartAt"/>,
	/// <see cref="IndentString"/> and <see cref="IndentLevel"/> up-to-date.
	/// See <see cref="NextToken()"/>.
	/// <para/>
	/// Alternately, your lexer can borrow the newline parser built into the base
	/// class, which is called <see cref="BaseLexer{C}.Newline()"/> and will call 
	/// <see cref="AfterNewline()"/> for you. It is possible to have LLLPG treat 
	/// this method as a rule, and tell LLLPG the meaning of the rule like this:
	/// <code>
	///	  extern token Newline @{ '\r' '\n'? | '\n' };
	///	  // BaseLexer also defines a Spaces() method, which behaves like this:
	///	  extern token Spaces  @{ (' '|'\t')* }; 
	///	</code>
	/// The <c>extern</c> modifier tells LLLPG not to generate code for the
	///	rule, but the rule must still have a body so that LLLPG can perform 
	///	prediction.
	/// </remarks>
	/// <typeparam name="CharSrc">A class that implements <see cref="ICharSource"/>.
	/// In order to write lexers that can accept any source of characters, set 
	/// CharSrc=ICharSource. For maximum performance when parsing strings (or
	/// to avoid memory allocation), set CharSrc=UString (<see cref="UString"/> 
	/// is a wrapper around <c>System.String</c> that, among other things, 
	/// implements <c>ICharSource</c>; please note that C# will implicitly convert 
	/// normal strings to <see cref="UString"/> for you).</typeparam>
	/// <typeparam name="Token">The type of token that your lexer will produce,
	/// e.g. <see cref="Loyc.Syntax.Lexing.Token"/>.</typeparam>
	public abstract class BaseILexer<CharSrc, Token> : BaseLexer<CharSrc>, ILexer<Token>
		where CharSrc : ICharSource
	{
		protected BaseILexer(CharSrc charSrc, string fileName = "", int inputPosition = 0, bool newSourceFile = true) 
			: base(charSrc, fileName, inputPosition, newSourceFile) { }

		UString _indentString;
		protected int _indentLevel;
		int _spacesPerTab = 4;

		/// <summary>Number of spaces per tab, for the purpose of computing 
		/// <see cref="IndentLevel"/>. Initial value: 4</summary>
		public int SpacesPerTab { get { return _spacesPerTab; } set { _spacesPerTab = value; } }

		/// <summary>The token that will be returned from the Current property.</summary>
		protected Maybe<Token> _current;

		/// <summary>Gets a string slice that holds the spaces or tabs that were 
		/// used to indent the current line.</summary>
		public UString IndentString { get { return _indentString; } }
		
		/// <summary>Gets the number of spaces that were used to indent the current
		/// line, where a tab counts as rounding up to the next multiple of 
		/// <see cref="SpacesPerTab"/> spaces.</summary>
		public int IndentLevel { get { return _indentLevel; } }

		/// <summary>Reinitializes the object. This method is called by the constructor.</summary>
		/// <remarks>Compared to the base class version of this function, this 
		/// method also skips over the UTF BOM '\uFEFF', if present, and it measures
		/// the indentation of the first line (without skipping over it).
		/// </remarks>
		public override void Reset(CharSrc source, string fileName = "", int inputPosition = 0, bool newSourceFile = true)
		{
			base.Reset(source, fileName, inputPosition, newSourceFile);
			// Skip the UTF BOM, if present
			if (LA0 == '\uFEFF') {
				Skip();
				_lineStartAt = InputPosition;
			}
			ScanIndent(false);
		}

		/// <summary>Scans the next token in the character stream and returns the
		/// token, or null when the end of the text is reached.</summary>
		/// <remarks>
		/// The derived class must call <see cref="AfterNewline()"/> after it
		/// advances past each newline (CR/LF/CRLF), in order to keep the 
		/// properties <see cref="BaseLexer{C}.LineNumber"/>, <see cref="BaseLexer{C}.LineStartAt"/>,
		/// <see cref="IndentString"/> and <see cref="IndentLevel"/> up-to-date.
		/// This must be done even when the newline is encountered inside a comment
		/// or multi-line string. Note that the <see cref="BaseLexer{C}.Newline"/> rule 
		/// in the base class will call <see cref="AfterNewline"/> for you.
		/// <para/>
		/// Also, while returning, the derived class should set the <c>_current</c> 
		/// field to its own return value so that the <see cref="Current"/> property
		/// works reliably.
		/// </remarks>
		public abstract Maybe<Token> NextToken();

		/// <summary>The LES and EC# languages support "dot indents", which are 
		/// lines that start with a dot (.) followed by a tab or spaces. If you
		/// overload this method to return true, then <see cref="AfterNewline()"/>
		/// and <see cref="Reset"/> will count dot indents as part of the 
		/// indentation at the beginning of each line; otherwise, only spaces and
		/// tabs will be counted.</summary>
		/// <remarks>
		/// A dot indent has the syntax <c>('.' ('\t' | ' '+))*</c>. This 
		/// indentation style is recognized only if a dot is the first character 
		/// on a line. Each pair of dot+(tab/spaces) prior to the first non-space 
		/// token is counted the same way as a tab character (\t). Dot indents are 
		/// useful for posting source code on "bad" blog software or forums that 
		/// do not preseve indentation.</remarks>
		protected virtual bool SupportDotIndents() { return false; }
		
		/// <summary>The lexer must call this method exactly once after it advances 
		/// past each newline, even inside comments and strings. This method keeps
		/// the <see cref="BaseLexer{C}.LineNumber"/>, <see cref="BaseLexer{C}.LineStartAt"/>,
		/// <see cref="IndentString"/> and <see cref="IndentLevel"/> properties
		/// updated.</summary>
		protected override void AfterNewline()
		{
			AfterNewline(false, false);
		}
		/// <inheritdoc cref="AfterNewline()"/>
		/// <param name="ignoreIndent">Causes this method not to measure the indent
		/// at the beginning of this line, and leave the <see cref="IndentLevel"/>
		/// and <see cref="IndentString"/> unchanged. You may wish to set this flag 
		/// when a newline is encountered inside a multiline comment.</param>
		/// <param name="skipIndent">This method normally scans indentation after 
		/// the newline character, in order to update the <see cref="IndentString"/> 
		/// and <see cref="IndentLevel"/> properties. If this parameter is true,
		/// the <see cref="BaseLexer{C}.InputPosition"/> will also be increased, skipping past
		/// those initial spaces. If <c>supportDotIndent</c> is also true, the
		/// <see cref="BaseLexer{C}.InputPosition"/> will also skip past the dot indent, if any.</param>
		protected void AfterNewline(bool ignoreIndent, bool skipIndent)
		{
			base.AfterNewline();
			if (!ignoreIndent)
				ScanIndent(skipIndent);
		}

		/// <summary>Scans indentation at the beginning of a line and updates the
		/// <see cref="IndentLevel"/> and <see cref="IndentString"/> properties.
		/// This function is called automatically by <see cref="AfterNewline()"/>,
		/// but should be called manually on the very first line of the file.</summary>
		/// <remarks>Parameters are documented at <see cref="AfterNewline(bool,bool)"/></remarks>
		protected void ScanIndent(bool skipSpaces = true)
		{
			int li = 0, indentLevel = 0;
			if (SupportDotIndents() && LA0 == '.') {
				for (;;) {
					if (LA(li) == '.') {
						var la1 = LA(li + 1);
						if (la1 == '\t' || la1 == ' ') {
							li += 2;
							indentLevel += SpacesPerTab;
							for (; LA(li) == ' '; li++) {}
						} else
							break;
					} else
						break;
				}
			}
			for (;;) {
				var la0 = LA(li);
				if (la0 == ' ') {
					li++;
					indentLevel++;
				} else if (la0 == '\t') {
					li++;
					indentLevel += SpacesPerTab;
					indentLevel -= indentLevel % SpacesPerTab; 
				} else
					break;
			}
			_indentLevel = indentLevel;
			_indentString = CharSource.Slice(InputPosition, li);
			if (skipSpaces)
				InputPosition += li;
		}

		ISourceFile ILexer<Token>.SourceFile { get { return base.SourceFile; } }
		public new LexerSourceFile<CharSrc> SourceFile { get { return base.SourceFile; } }

		void IDisposable.Dispose() {}
		public Token Current { get { return _current.Or(default(Token)); } }
		object System.Collections.IEnumerator.Current { get { return _current; } }
		void System.Collections.IEnumerator.Reset() { Reset(CharSource, FileName, 0); }
		bool System.Collections.IEnumerator.MoveNext()
		{
			_current = NextToken();
			return _current.HasValue;
		}
	}
}
