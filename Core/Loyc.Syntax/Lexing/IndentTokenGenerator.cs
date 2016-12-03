using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.MiniTest;
using Loyc.Syntax.Les;

namespace Loyc.Syntax.Lexing
{
	/// <summary>
	/// A preprocessor usually inserted between the lexer and parser that inserts
	/// "indent", "dedent", and "end-of-line" tokens at appropriate places in a
	/// token stream.
	/// </summary>
	/// <remarks>This class will not work correctly if the lexer does not implement 
	/// <see cref="ILexer{T}.IndentLevel"/> properly.
	/// <para/>
	/// This class is abstract because it doesn't know how to classify or create 
	/// tokens. The derived class must implement <see cref="GetTokenCategory"/>,
	/// <see cref="MakeEndOfLineToken"/>, <see cref="MakeIndentToken"/> and 
	/// <see cref="MakeDedentToken"/>. <see cref="IndentTokenGenerator"/> is a 
	/// non-abstract version of this class based on <see cref="Loyc.Syntax.Lexing.Token"/> 
	/// structures, with several properties that can be customized.
	/// <para/>
	/// Creation of indent, dedent, and end-of-line tokens can be suppressed inside 
	/// brackets, i.e. () [] {}. This is accomplished by recognizing brackets inside
	/// your implementation of <see cref="GetTokenCategory"/>.
	/// <para/>
	/// <see cref="TokensToTree"/> can be placed in the pipeline before or after 
	/// this class; if it is placed afterward, anything between Indent and Dedent
	/// tokens will be made a child of the Indent token.
	/// <para/>
	/// Note: whitespace tokens (<see cref="TokenCategory.Whitespace"/>) are passed 
	/// through and otherwise unprocessed.
	/// <para/>
	/// Note: EOL tokens are not generated for empty or comment lines, and are not 
	/// generated after a generated indent token, although they could be generated 
	/// after a pre-existing indent token that was already in the token stream, 
	/// unless that token is categorized as <see cref="TokenCategory.OpenBracket"/>.
	/// <para/>
	/// Partial dedents and unexpected indents, as in
	/// <code>
	///   if Condition:
	///       print("Hello")
	///     print("Hello again")
	///   else:
	///       print("Goodbye")
	///         print("Goodbye again")
	/// </code>
	/// will cause an error message to be written to the <see cref="ILexer{Tok}.ErrorSink"/> 
	/// of the original lexer.
	/// <para/>
	/// Please see <see cref="IndentTokenGenerator"/> for additional remarks and examples.
	/// </remarks>
	public abstract class IndentTokenGenerator<Token> : LexerWrapper<Token>
	{
		/// <summary>Initializes the indent detector.</summary>
		/// <param name="lexer">Original lexer (either a raw lexer or an instance of another preprocessor such as <see cref="TokensToTree"/>.)</param>
		public IndentTokenGenerator(ILexer<Token> lexer) : base(lexer)
			{ InitState(); }

		public enum TokenCategory { 
			/// <summary>An open bracket, inside of which indent triggers should be ignored.</summary>
			OpenBracket = 1, 
			/// <summary>A close bracket, whch reverses the effect of an open bracket.</summary>
			CloseBracket = -1,
			/// <summary>This token may trigger an indentation token, with an unindent
			/// token to be generated later, when a line is encountered that is not 
			/// indented in comparison to this line.</summary>
			IndentTrigger = 0x10000,
			/// <summary>A whitespace token, which should be passed though and 
			/// otherwise ignored.</summary>
			Whitespace = 0x20000,
			/// <summary>None of the other categories apply to this token.</summary>
			Other = 0,
		}

		/// <summary>Gets the category of a token for the purposes of indent processing.</summary>
		public abstract TokenCategory GetTokenCategory(Token token);
		
		/// <summary>Returns a token to represent indentation, or null to suppress 
		/// generating an indent-dedent pair at this point.</summary>
		/// <param name="indentTrigger">The token that triggered this function call.</param>
		/// <param name="tokenAfterward">The token after the indent trigger, or NoValue at EOF.</param>
		/// <param name="newlineAfter">true if the next non-whitespace token after 
		/// <c>indentTrigger</c> is on a different line, or if EOF comes afterward.</param>
		protected abstract Maybe<Token> MakeIndentToken(Token indentTrigger, ref Maybe<Token> tokenAfterward, bool newlineAfter);
		/// <summary>Returns token(s) to represent un-indentation.</summary>
		/// <param name="tokenBeforeNewline">The last non-whitespace token before dedent</param>
		/// <param name="tokenAfterNewline">The first non-whitespace un-indented 
		/// token after the unindent, or NoValue at the end of the file. The 
		/// derived class is allowed to change this token, or delete it by 
		/// changing it to NoValue.</param>
		/// <remarks>This class considers the indented block to be "over" even if 
		/// this method returns no tokens.</remarks>
		protected abstract IEnumerator<Token> MakeDedentToken(Token tokenBeforeNewline, ref Maybe<Token> tokenAfterNewline);
		/// <summary>Returns a token to represent the end of a line, or null to
		/// avoid generating such a token.</summary>
		/// <param name="tokenBeforeNewline">Final non-whitespace token before the newline was encountered.</param>
		/// <param name="tokenAfterNewline">First non-whitespace token after newline.</param>
		/// <param name="deltaIndent">Change of indentation after the newline, or 
		/// null if a dedent token is about to be inserted after the newline.</param>
		/// <remarks>This function is also called at end-of-file, unless there are 
		/// no tokens in the file.</remarks>
		protected abstract Maybe<Token> MakeEndOfLineToken(Token tokenBeforeNewline, ref Maybe<Token> tokenAfterNewline, int? deltaIndent);

		/// <summary>A method that is called when the indent level changed without
		/// a corresponding indent trigger.</summary>
		/// <param name="tokenBeforeNewline">Final non-whitespace token before the newline.</param>
		/// <param name="tokenAfterNewline">First non-whitespace token after the newline.
		/// Though it's a <see cref="Maybe{T}"/>, it always has a value, but this 
		/// function can suppress its emission by setting it to NoValue.Value.</param>
		/// <param name="deltaIndent">Amount of unexpected indentation (positive or 
		/// negative). On return, this parameter holds the amount by which to change
		/// the <see cref="CurrentIndent"/>; the default implementation leaves this
		/// value unchanged, which means that subsequent lines will be expected to 
		/// be indented by the same (unexpected) amount.</param>
		/// <returns>true if <see cref="MakeEndOfLineToken"/> should be called as 
		/// usual, or false to suppress EOL genertion. EOL can only be suppressed
		/// in case of an unexpected indent (<c>deltaIndent>0</c>), not an unindent.</returns>
		/// <remarks>The default implementation always returns true. It normally 
		/// writes an error message, but switches to a warning in case 
		/// <c>OuterIndents[OuterIndents.Count-1] == OuterIndents[OuterIndents.Count-2]</c>, 
		/// which this class interprets as a single unindent.
		/// </remarks>
		protected virtual bool IndentChangedUnexpectedly(Token tokenBeforeNewline, ref Maybe<Token> tokenAfterNewline, ref int deltaIndent)
		{
			var pos = IndexToMsgContext(tokenAfterNewline.Or(default(Token)));
			if (deltaIndent > 0) {
				if (_errorBias >= 0)
					ErrorSink.Write(Severity.Error, pos, "Unexpected indent");
				_errorBias++;
			} else {
				if (_errorBias <= 0) {
					var sev = Severity.Error;
					if (_outerIndents.Count >= 2 && _outerIndents.Last == _outerIndents[_outerIndents.Count - 2])
						sev = Severity.Warning;
					ErrorSink.Write(sev, pos, "Unindent does not match any outer indentation level");
				}
				_errorBias--;
			}
			return true;
		}

		/// <summary>Gets the context for use in error messages, which by convention is a <see cref="SourceRange"/>.</summary>
		/// <remarks>The base class uses Lexer.InputPosition as a fallback if the token doesn't implement ISimpleToken{int}.</remarks>
		protected virtual object IndexToMsgContext(Token token)
		{
			int index = Lexer.InputPosition;
			if (token is ISimpleToken<int>)
				index = (token as ISimpleToken<int>).StartIndex;
			return new SourceRange(Lexer.SourceFile, index);
		}

		protected virtual void CheckForIndentStyleMismatch(UString indent1, UString indent2, Token next)
		{
			int common = System.Math.Min(indent1.Length, indent2.Length);
			indent1 = indent1.Substring(0, common);
			indent2 = indent2.Substring(0, common);
			if (!indent1.Equals(indent2))
				ErrorSink.Write(Severity.Warning, IndexToMsgContext(next), "Indentation style changed on this line from {0} to {1}",
					Les2Printer.PrintLiteral(indent1.ToString()), 
					Les2Printer.PrintLiteral(indent2.ToString()));
		}

		public override void Reset() { 
			base.Reset();
			InitState();
		}
		void InitState()
		{
			_pending.Resize(0);
			_curLine = -1; // detect false "newline" on first token
			_curIndent = -1; // "beginning of file" marker
			_curIndentString = "";
			_curCat = TokenCategory.Other;
			_bracketDepth = 0;
			_eofHandledAlready = false;
			_errorBias = 0;
			_outerIndents.Resize(1);
			_outerIndents[0] = -1;
			_lastNonWS = default(Token);
		}

		protected int BracketDepth { get { return _bracketDepth; } }
		protected int CurrentIndent { get { return _curIndent; } }
		protected IListSource<int> OuterIndents { get { return _outerIndents; } }

		InternalDList<Token> _pending = InternalDList<Token>.Empty; // generated and deferred tokens
		int _curLine, _curIndent; // _cur* variables are updated after each newline
		UString _curIndentString;
		TokenCategory _curCat;
		short _bracketDepth;    // nonzero if we're inside brackets and ignoring indents
		bool _eofHandledAlready;
		short _errorBias;       // to suppress 'unexpected dedent' after 'unexpected dedent'
		InternalList<int> _outerIndents = InternalList<int>.Empty; // indentation stack
		Token _lastNonWS;       // most recent "real", non-whitespace value of _current

		public override Maybe<Token> NextToken()
		{
			Maybe<Token> next;
			var pending = _pending.TryPopFirst();
			if (pending.HasValue) {
				_lastNonWS = pending.Value;
				return _current = pending;
			} else
				next = Lexer.NextToken();

			if (next.HasValue) {
				var nextVal = next.Value;
				TokenCategory nextCat = GetTokenCategory(nextVal);

				if (_bracketDepth > 0) {
					// Processing disabled... wait for close brackets
					_curLine = Lexer.LineNumber;
					_lastNonWS = nextVal;
					_bracketDepth += (short)nextCat;
					_curCat = nextCat;
					return _current = next;
				} else if (nextCat == TokenCategory.Whitespace)
					return _current = next; // ignore whitespace, pass it through

				int nextLine = Lexer.LineNumber;
				bool newline = nextLine != _curLine;
				if (newline || _curCat == TokenCategory.IndentTrigger)
				{
					HandleNextToken(ref next, ref nextCat, newline);
					nextVal = next.Value;
				}

				_lastNonWS = nextVal;
				_bracketDepth += (short)nextCat;
				_curLine = nextLine;
				_curCat = nextCat;
				return _current = next;
			} else {
				// EOF
				if (!_eofHandledAlready) {
					_eofHandledAlready = true;
					TokenCategory nextCat = TokenCategory.Other;
					HandleNextToken(ref next, ref nextCat, true);
				}
				return _current = next;
			}
		}

		private void HandleNextToken(ref Maybe<Token> next, ref TokenCategory nextCat, bool newline)
		{
			bool atEof = !next.HasValue;
			bool indentTriggered = false;
			if (_curCat == TokenCategory.IndentTrigger) {
				var insert = MakeIndentToken(_lastNonWS, ref next, newline);
				if (insert.HasValue) {
					indentTriggered = true;
					_outerIndents.Add(_curIndent);
					_pending.PushLast(insert.Value);
				}
				AutoNext(ref next, ref nextCat, atEof);
			}

			UString nextIndentString = Lexer.IndentString;
			if (!atEof)
				CheckForIndentStyleMismatch(_curIndentString, nextIndentString, next.Or(_lastNonWS));

			int nextIndent = atEof ? _outerIndents[1, _curIndent] : Lexer.IndentLevel;
			if (_curIndent > -1)
			{
				// Check for unexpected indent
				bool emitEol = true;
				if (nextIndent > _curIndent && _outerIndents.Last != _curIndent)
				{
					int delta = nextIndent - _curIndent;
					emitEol = IndentChangedUnexpectedly(_lastNonWS, ref next, ref delta);
					AutoNext(ref next, ref nextCat, atEof);
					nextIndent = _curIndent + delta;
				}

				if (newline && !indentTriggered && emitEol)
				{
					int? delta = nextIndent - _curIndent;
					if (nextIndent <= _outerIndents.Last)
						delta = null;
					var insert = MakeEndOfLineToken(_lastNonWS, ref next, delta);
					AutoNext(ref next, ref nextCat, atEof);
					if (insert.HasValue)
						_pending.PushLast(insert.Value);
				}

				if (newline) {
					// Add dedents
					int outerIndent = _outerIndents.Last;
					for (int oi = outerIndent; nextIndent <= oi; oi = _outerIndents.Last)
					{
						// Unindent detected
						outerIndent = oi;
						_outerIndents.Pop();
						AddPendingLast(MakeDedentToken(_lastNonWS, ref next));
						AutoNext(ref next, ref nextCat, atEof);
						if (!next.HasValue && !atEof) return; // advance to next token
					}

					// Detect partial dedent
					if (nextIndent < _curIndent && nextIndent > outerIndent)
					{
						int delta = nextIndent - _curIndent;
						bool _ = IndentChangedUnexpectedly(_lastNonWS, ref next, ref delta);
						AutoNext(ref next, ref nextCat, atEof);
						nextIndent = _curIndent + delta;

						if (_outerIndents.Count >= 2 && _outerIndents.Last == _outerIndents[_outerIndents.Count - 2])
						{
							// The situation is like this:
							// if c1: if c2:
							//         c1_and_c2();
							//     c1_only();
							// It's usually illegal, but it has a reasonable interpretation.
							// (Note: in Python you can't even write "if c1: if c2:")
							_outerIndents.Pop();
							AddPendingLast(MakeDedentToken(_lastNonWS, ref next));
							AutoNext(ref next, ref nextCat, atEof);
						}
					}
				}
			}

			if (!_pending.IsEmpty) {
				if (next.HasValue)
					_pending.PushLast(next.Value);
				next = _pending.TryPopFirst().Value;
			}

			if (newline) {
				_curIndent = nextIndent;
				_curIndentString = nextIndentString;
			}
		}

		private void AutoNext(ref Maybe<Token> next, ref TokenCategory nextCat, bool atEof)
		{
			if (!next.HasValue && !atEof) {
				next = Lexer.NextToken();
				nextCat = next.HasValue ? GetTokenCategory(next.Value) : TokenCategory.Other;
			}
		}

		void AddPendingLast(IEnumerator<Token> e)
		{
			if (e != null) {
				while (e.MoveNext())
					_pending.PushLast(e.Current);
			}
		}
	}

	/// <summary>
	/// A preprocessor usually inserted between the lexer and parser that inserts
	/// "indent", "dedent", and "end-of-line" tokens at appropriate places in a
	/// token stream.
	/// </summary>
	/// <remarks>
	/// Suppose you use an <see cref="IndentToken"/> and <see cref="DedentToken"/> 
	/// that are equal to the token types you've chosen for <c>{ braces }</c> (e.g.  
	/// (<see cref="TokenKind.LBrace"/> and <see cref="TokenKind.RBrace"/>), the 
	/// only indent trigger is a colon (:), and you set <see cref="EolToken"/> to 
	/// the token type you're using for semicolons. Then the token stream from 
	/// input such as
	/// <code>
	///	def Sqrt(value):
	///		if value == 0: return 0
	///		g = 0; bshft = Log2Floor(value) >> 1;
	///		b = 1 &lt;&lt; bshft
	///		do:
	///			temp = (g + g + b) &lt;&lt; bshft
	///			if value >= temp: g += b
	///				value -= temp
	///			b >>= 1
	///		while (bshft-- > 0)
	///		return g
	/// </code>
	/// will be converted to a token stream equivalent to
	/// <code>
	///	def Sqrt(value): {
	///		if value == 0: { return 0;
	///		} g = 0; bshft = Log2Floor(value) >> 1;
	///		b = 1 &lt;&lt; bshft;
	///		do: {
	///			temp = (g + g + b) &lt;&lt; bshft
	///			if value >= temp: { g += b;
	///				value -= temp;
	///			} b >>= 1;
	///		} while (bshft-- > 0);
	///		return g;
	///	}</code>
	///	That is, a semicolon is added to lines that don't already have one, open 
	///	braces are inserted right after colons, and semicolons are <i>not</i> added 
	///	right after opening braces.
	///	<para/>
	/// If multiple indents occur on a single line, as in
	/// <code>
	/// if x: if y:
	///     Foo(x, y)
	/// </code>
	/// The output will be like this:
	/// <code>
	/// if x: { if y: {
	///     Foo(x, y);
	/// }}
	/// </code>
	/// 
	/// <h3>Configuration for Python</h3>
	/// 
	/// Newlines generally represent the end of a statement, while colons mark 
	/// places where a "child" block is expected. Inside parenthesis, square 
	/// brackets, or braces, newlines are ignored:
	/// <code>
	/// 	s = ("this is a pretty long string that I'd like "
	/// 	  + " to continue writing on the next line")
	/// </code>
	/// And, inside brackets, indentation is ignored, so this is allowed:
	/// <code>
	/// if foo:
	/// 	s = ("this is a pretty long string that I'd like "
	/// + " to continue writing on the next line")
	/// 	print(s)
	/// </code>
	/// Note that if you don't use brackets, Python 3 doesn't try to figure out if 
	/// you "really" meant to continue a statement on the next line:
	/// <code>
	/// 	# SyntaxError after '+': invalid syntax
	/// 	s = "this is a pretty long string that I'd like " + 
	/// 		" to continue writing on the next line"
	/// </code>
	/// Thus OpenBrackets and CloseBrackets should be <c>( [ {</c> and <c>) ] }</c>, 
	/// respectively. IndentType and DedentType should be synthetic Indent and 
	/// Dedent tokens, since curly braces have a different meaning (they define a 
	/// dictionary).
	/// <para/>
	/// In Python, it appears you can't write two "block" statements on one line, 
	/// as in this example:
	/// <code>
	///   if True: if True: print() # SyntaxError: invalid syntax
	/// </code>
	/// You're also not allowed to indent the next line if the block statement on
	/// the current line is followed by another statement:
	/// <code>
	///   if True: print('a')
	///       print('b') # IndentationError: unexpected indent
	/// </code>
	/// But you can switch style in different branches:
	/// <code>
	///   if True:
	///       print("t")
	///   else: print("f")
	///   try: print("t")
	///   except: 
	///       print("e")
	/// </code>
	/// Also, although you can normally separate statements with semicolons:
	/// <code>
	///   print("hell", end=""); print("o")
	/// </code>
	/// You are not allowed to write this:
	/// <code>
	///   print("?"); if True: # SyntaxError: invalid syntax
	///      print("t")
	/// </code>
	/// Considering these three facts, I would say that the colon should be 
	/// classified as an EOL indent trigger (EolIndentTriggers), and the parser 
	/// should 
	/// 1. recognize non-block statements separately from block statements,
	/// 2. expect a colon to be followed by either an indented block or a non-block 
	///    statement, but
	/// 3. recognize a non-block "statement" as a <i>list</i> of statements 
	///    separated by semicolons, with an optional semicolon at the end.
	/// <para/>
	/// Now, Python doesn't allow a block statement without a <c>pass</c>, e.g.:
	/// <code>
	///   if cond: # "do nothing"
	///   return   # IndentationError: expected an indented block
	/// </code>
	/// I'm inclined to treat this as a special case to be detected in the parser.
	/// And although you can write a semicolon on a line by itself, you can't 
	/// write any of these lines:
	/// <code>
	///   if cond: ;         # SyntaxError: invalid syntax
	///   print(); ; print() # SyntaxError: invalid syntax
	///   ; ;                # SyntaxError: invalid syntax
	/// </code>
	/// My interpretation is that a semicolon <i>by itself</i> is treated as a block 
	/// statement (i.e. illegal in a non-block statement context). Since a semicolon
	/// is not treated the same way as a newline, the <see cref="EolToken"/> should 
	/// be a special token, not a semicolon.
	/// </remarks>
	/// <seealso cref="IndentTokenGenerator{Token}"/>
	public class IndentTokenGenerator : IndentTokenGenerator<Token>
	{
		Token? _eolToken;
		Token _indentToken, _dedentToken;

		/// <summary>Initializes the indent detector.</summary>
		/// <param name="lexer">Original lexer</param>
		/// <param name="allIndentTriggers">A list of all token types that could trigger the insertion of an indentation token.</param>
		/// <param name="eolToken">Prototype token for end-statement markers inserted when
		/// newlines are encountered, or null to avoid generating such markers.</param>
		/// <param name="indentToken">Prototype token for indentation markers</param>
		/// <param name="dedentToken">Prototype token for un-indent markers</param>
		public IndentTokenGenerator(ILexer<Token> lexer, int[] allIndentTriggers, Token? eolToken, Token indentToken, Token dedentToken)
			: base(lexer)
		{
			AllIndentTriggers = allIndentTriggers;
			_indentToken = indentToken;
			_dedentToken = dedentToken;
			_eolToken = eolToken;
		}
		public IndentTokenGenerator(ILexer<Token> lexer, int[] allIndentTriggers, Token? eolToken)
			: this(lexer, allIndentTriggers, eolToken, 
			       new Token((int)TokenKind.Indent, 0, 0, null), 
			       new Token((int)TokenKind.Dedent, 0, 0, null)) { }

		private int[] _allIndentTriggers;
		public int[] AllIndentTriggers
		{
			get { return _allIndentTriggers; }
			set { _allIndentTriggers = value ?? EmptyArray<int>.Value; }
		}

		/// <summary>A subset of <see cref="AllIndentTriggers"/> that only take 
		/// effect at the end of a line.</summary>
		/// <remarks>If this list includes items that are not in 
		/// <see cref="AllIndentTriggers"/>, they have no effect.</remarks>
		private int[] _eolIndentTriggers;
		public int[] EolIndentTriggers
		{
			get { return _eolIndentTriggers; }
			set { _eolIndentTriggers = value ?? EmptyArray<int>.Value; }
		}

		/// <summary>Gets or sets the prototype token for end-statement (a.k.a. 
		/// end-of-line) markers, cast to an integer as required by <see cref="Token"/>. 
		/// Use <c>null</c> to avoid generating such markers.</summary>
		/// <remarks>Note: if the last token on a line has this same type, this 
		/// class will not generate an extra newline token.
		/// <para/>
		/// The StartIndex is updated for each actual token emitted.</remarks>
		public Token? EolToken
		{
			get { return _eolToken; }
			set { _eolToken = value; }
		}
		/// <summary>Gets or sets the prototype token for indentation markers.</summary>
		/// <remarks>The StartIndex is updated for each actual token emitted.</remarks>
		public Token IndentToken
		{
			get { return _indentToken; }
			set { _indentToken = value; }
		}
		/// <summary>Gets or sets the prototype token for unindentation markers.</summary>
		/// <remarks>The StartIndex is updated for each actual token emitted.</remarks>
		public Token DedentToken
		{
			get { return _dedentToken; }
			set { _dedentToken = value; }
		}

		public override TokenCategory GetTokenCategory(Token token)
		{
			if (token.Value == WhitespaceTag.Value)
				return TokenCategory.Whitespace;
			if (Token.IsOpenerOrCloser((TokenKind)token.TypeInt))
				return Token.IsOpener((TokenKind)token.TypeInt) ? TokenCategory.OpenBracket : TokenCategory.CloseBracket;
			if (Contains(AllIndentTriggers, token.TypeInt))
				return TokenCategory.IndentTrigger;
			return TokenCategory.Other;
		}

		protected bool Contains(int[] list, int item)
		{
			for (int i = 0; i < list.Length; i++)
				if (list[i] == item)
					return true;
			return false;
		}

		protected override Maybe<Token> MakeIndentToken(Token indentTrigger, ref Maybe<Token> tokenAfterward, bool newlineAfter)
		{
			if (!newlineAfter && Contains(EolIndentTriggers, indentTrigger.TypeInt))
				return Maybe<Token>.NoValue; // ignore EOL trigger not followed by newline
			return IndentToken.WithStartIndex(indentTrigger.EndIndex);
		}
		protected override IEnumerator<Token> MakeDedentToken(Token tokenBeforeDedent, ref Maybe<Token> tokenAfterDedent)
		{
			return ListExt.Single(DedentToken.WithStartIndex(tokenBeforeDedent.EndIndex)).GetEnumerator();
		}
		protected override Maybe<Token> MakeEndOfLineToken(Token tokenBeforeNewline, ref Maybe<Token> tokenAfterNewline, int? deltaIndent)
		{
			if (!EolToken.HasValue)
				return null;
			if (tokenBeforeNewline.TypeInt == EolToken.Value.TypeInt)
				return Maybe<Token>.NoValue;
			else
				return EolToken.Value.WithStartIndex(tokenBeforeNewline.EndIndex);
		}
	}
}
