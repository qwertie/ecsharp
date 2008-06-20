using System;
using System.Collections.Generic;
using System.Text;
using Loyc.CompilerCore;
using Loyc.Runtime;
using Loyc.Utilities;
using System.Diagnostics;

namespace Loyc.BooStyle 
{
	/// <summary>
	/// Preprocesses lexer tokens to insert INDENT, DEDENT, and EOS tokens into the
	/// token stream. Also converts "|" at the beginning of a line to 
	/// LINE_CONTINUATION.
	/// </summary><remarks>
	/// The boo language can operate in two modes, 'normal' and 'whitespace-
	/// agnostic' (WSA). In normal mode, indentation controls statement nesting; in 
	/// whitespace-agnostic mode, indentation is semantically irrelevant and 
	/// colon-end pairs control statement nesting. BooLexer supports both modes, but
	/// INDENT/DEDENT pairs are not produced in WSA mode. Note that despite its name,
	/// WSA mode still treats newline like a divider between statements. In both 
	/// modes, newlines are ignored while inside brackets or braces.
	///
	/// Originally I wanted to produce INDENT/DEDENT tokens using extra logic in
	/// the lexer grammar, but that turned out to be difficult, so I'm using this 
	/// wrapper instead.
	/// 
	/// In normal mode, boo supports a compact syntax without indentation like this:
	/// <code>
	/// if false: throw FalseIsTrueException();
	/// else: print 'QC OK!'
	/// </code>
	/// I believe the parser should be given the same token stream as if the input
	/// had been
	/// <code>
	/// if false: 
	///   throw FalseIsTrueException();
	/// else: 
	///   print 'QC OK!'
	/// </code>
	/// Therefore, this class produces an INDENT token when the colon is encountered 
	/// rather than waiting for a newline and an indent. It then produces a DEDENT 
	/// token when it turns out that the next line is not indented. On the other hand, 
	/// if the next line is indented...
	/// <code>
	/// if false: print 'Oh no!!!'
	///     throw FalseIsTrueException();
	/// else: print 'QC OK!'
	/// </code>
	/// ...then neither INDENT nor DEDENT is produced where the indent occurs, but 
	/// the indentation stack is updated to reflect the new indentation level. 
	/// In this example, the output token stream will be as follows (for clarity,
	/// WS tokens are hidden and only the type symbols are shown)
	/// <code>
	/// _if _false : INDENT ID SQ_STRING NEWLINE EOS
	///   _throw ID LPAREN RPAREN EOS NEWLINE
	/// DEDENT _else : INDENT ID SQ_STRING NEWLINE EOS
	/// DEDENT
	/// </code>
	/// All this logic is found in the GetEnumerator() method. It's a big function
	/// and it can't easily be broken down because it uses yield statements which 
	/// can't be factored out to separate methods.
	/// 
	/// The equivalent code in WSA mode is this:
	/// <code>
	/// if false: throw FalseIsTrueException()
	/// else: print 'QC OK!'
	/// end
	/// </code>
	/// At first you might think that colon/end pairs work like INDENT/DEDENT pairs, 
	/// but then you notice that there are clauses like "else:" that ought to work 
	/// like <c>DEDENT else: INDENT</c>. Unfortunately, in general, the this class
	/// cannot recognize clauses like "else:" because, given the flexible nature of 
	/// Loyc, many situations may be too complex to recognize properly during 
	/// lexical analysis. One extension may define a clause that has complex syntax 
	/// and another extension may define a statement with the same prefix; in such 
	/// a case there's no practical way for a lexer to tell the difference between 
	/// a clause and the beginning of a new statement, even assuming it could be 
	/// informed about the two entities. Therefore, this class produces neither 
	/// INDENT nor DEDENT in WSA mode. A statement parser can detect whether WSA 
	/// mode is engaged by the presence or absence of INDENT following COLON.
	/// 
	/// When inside brackets or parenthesis, this class switches to WSA mode. Thus,
	/// unlike standard boo, Loyc allows you to use WSA mode and normal mode in the 
	/// same source file.
	/// 
	/// This wrapper also produces EOS tokens from newlines, but only if it is not
	/// waiting for a closing bracket. EOS is produced on every line that follows
	/// a parser-visible token, except after INDENT, EOS or COLON. For example, in 
	/// the code
	/// <code>
	/// (1)   Console.Write('Hello, ')
	/// (2)
	/// (3)   if NameIsKnown:
	/// (4)      print Name
	/// (5)   else: // !NameIsKnown
	/// (6)      // Generic title for unknown person
	/// (7)      print 'dude'.
	/// </code>
	/// EOS is produced after lines 1, 4, and 7. EOS is not produced for lines 2 
	/// and 6 because no tokens are VisibleToParser on those lines; EOS is not 
	/// produced for lines 3 and 5 because the last visible token on those lines is 
	/// INDENT or COLON. There's one more wrinkle: when "|" is used for line 
	/// continuation, EOS must not be produced, so GetEnumerator() does not produce
	/// EOS until it reaches the beginning of the next line, to confirm it is not 
	/// continued.
	/// </remarks>
	public class BooLexer : IEnumerable<AstNode>
	{
		public BooLexer(ISourceFile source, IDictionary<string, Symbol> keywords, bool wsaOnly) 
			: this(source, keywords, wsaOnly, 4) {}
		public BooLexer(ISourceFile source, IDictionary<string, Symbol> keywords, bool wsaOnly, int spacesPerTab) 
			: this(new BooLexerCore(source, keywords), wsaOnly) { }
		public BooLexer(BaseLexer lexer, bool wsaOnly)
			: this(lexer, wsaOnly, 4) { }
		public BooLexer(BaseLexer lexer, bool wsaOnly, int spacesPerTab)
		{
			_wsaOnly = wsaOnly;
			_spacesPerTab = spacesPerTab;
			_lexer = lexer;
			_indentStack = new Stack<int>(10);
			_indentStack.Push(0);
		}
		protected BaseLexer _lexer;
		int _spacesPerTab;

		public int SpacesPerTab { 
			get { return _spacesPerTab; } 
			set { _spacesPerTab = value; } 
		}
		protected bool _usesTabs = false, _usesSpaces = false;
		protected bool _usedTabs = false, _usedSpaces = false;
		/// <summary>Contains the number of spaces at each indentation level.</summary>
		/// <remarks>Look at the code of GetEnumerator() to see exactly how 
		/// the stack is used. Note that it's initialized with a zero on the 
		/// top-of-stack.</remarks>
		protected Stack<int> _indentStack;
		protected readonly bool _wsaOnly;
		protected bool IsWSA { get { return _wsaOnly || _skipWhitespaceRegion > 0; } }
		
		/// <summary>Whether an EOS should be produced at the next newline</summary>
		private bool _produceEOS = false;
		/// <summary>Current token being processed by GetEnumerator(). If there are no tokens left, this points to the last token in the file.</summary>
		private AstNode _t;
		/// <summary>Current token type (_t.Type), or null if there are no more tokens left.</summary>
		private Symbol _tt;
		/// <summary>Used to enforce "only call GetEnumerator once" rule</summary>
		protected bool _enumerationStarted = false;
		/// <summary>Loads next _t and sets or clears _produceEOS as appropriate.</summary>
		private bool Advance()
		{
			if (_tt == Tokens.INDENT || _tt == BooLexerCore._COLON || _tt == Tokens.EOS)
				_produceEOS = false;
			else if (_t != null)// && _t.VisibleToParser)
				_produceEOS = true;

			AstNode t = _lexer.ParseNext();
            if (t == null) {
                _tt = null;
                return false;
            } else {
			    _t = t;
			    _tt = _t.NodeType;
			    return true;
            }
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		/// <summary>
		/// Returns the token enumerator which preprocesses tokens from the 
		/// lexer (normally BooLexerCore) that initialized this object.
		/// </summary><remarks>
		/// Note that only one enumerator from this object can be in operation 
		/// at a time, because (1) the underlying lexer only has one state, and 
		/// (2) this class contains state variables shared with the enumerator 
		/// that this method returns. To enforce this rule, GetEnumerator() can
		/// only be called once per object and will throw an exception if 
		/// enumeration is started twice.
		/// </remarks>
		public IEnumerator<AstNode> GetEnumerator()
		{
			bool isFirstToken = true;
			bool indentPending = false; // Indent caused by a colon
			// colonIndentFlag: set when INDENT is emitted due to colon; causes 
			// spacing stack adjustment, if necessary, on next normal-mode line
			bool colonIndentFlag = false; 
			
			int spaceCount = 0;
			AstNode lastNewline = null;

			if (_enumerationStarted)
				throw new InvalidOperationException(Localize.From("BooLexer.GetEnumerator() can only be called once per lexer."));
			_enumerationStarted = true;

			while (Advance()) {
			redo:
				if (_tt != BooLexerCore._WS) // <= optimization to skip all other checks
				{
					if (_tt == BooLexerCore._COLON && !IsWSA)
					{
						//////////////////////////////////////////////////////////////
						// Emit INDENT after a colon (in normal mode) ////////////////
						yield return _t;
						if (!Advance()) break;
						
						colonIndentFlag = true;
						_indentStack.Push(_indentStack.Peek() + 1);
						//////////////////////////////////////////////////////////////
					}
					else if (_skipWhitespaceRegion == 0 && (_tt == Tokens.NEWLINE || isFirstToken)) 
					{
						//////////////////////////////////////////////////////////////
						// Beginning of a line detected. /////////////////////////////
						//////////////////////////////////////////////////////////////
						if (isFirstToken) 
							isFirstToken = false;
						else {
							lastNewline = _t;
							// Proceed past the NEWLINE
							yield return _t; 
							if (!Advance()) break;
						}
						// Measure the type (spaces or tabs) and amount of whitespace.
						// Match DOT_INDENT | NORMAL_INDENT and keep track of spaces/tabs, where
						//   NORMAL_INDENT: WS*;
						//   DOT_INDENT: ('.' WS)+;
						_usesSpaces = _usesTabs = false;
						spaceCount = 0;
						if (_t.NodeType == Tokens.PUNC && _t.Text == ".") {
							yield return _t;
							if (!Advance()) break;

							while (_t.NodeType == Tokens.WS) {
								_usesTabs = true;
								spaceCount += SpacesPerTab;

								yield return _t; 
								if (!Advance()) goto end; // (break from outer loop)

								if (_t.NodeType != Tokens.PUNC || _t.Text != ".")
									break;

								yield return _t; 
								if (!Advance()) goto end; // (break from outer loop)
							}
						} else if (_t.NodeType == Tokens.WS) {
							spaceCount += CountSpaces(_t.Text);

							yield return _t; 
							if (!Advance()) break;
						}
						// Set a flag now. We will continue processing if/when 
						// a parser-visible token is encountered.
						indentPending = true;
						goto redo;
						//////////////////////////////////////////////////////////////
					} 
					else if (indentPending)// && _t.VisibleToParser)
					{
						//////////////////////////////////////////////////////////////
						// First parser-visible token on a line has been reached. ////
						//////////////////////////////////////////////////////////////
						indentPending = false;
						
						// Is it a line continuation token?
						if (_tt == Tokens.PUNC && _t.Text == "|") {
							// Yes. Replace PUNC with a LINE_CONTINUATION.
							yield return new AstNode(Tokens.LINE_CONTINUATION, _t.Range);
							if (!Advance()) break;
						} else {
							// No. Do normal processing for the beginning of a new line.
							// Warn if spaces are mixed with tabs improperly
							AutoWriteMixedSpacingWarning(_t.Position);

							if (_produceEOS) {
								_produceEOS = false;
								yield return new AstNode(Tokens.EOS, _t.Range);
							}

							// Emit INDENT/DEDENT(s) if necessary.
							// Look for change of indentation...
							int oldCount = _indentStack.Peek();
							if (spaceCount > oldCount) {
								// Indent increased compared to previous line
								if (colonIndentFlag) {
									// Correct the value at the top of stack
									_indentStack.Pop();
									Debug.Assert(oldCount == _indentStack.Peek() + 1);
									_indentStack.Push(spaceCount);
								} else {
									// Push new spacing on stack and emit INDENT
									_indentStack.Push(spaceCount);
									SourceRange range = _t.Range;
									range.EndIndex = range.StartIndex;
									yield return new AstNode(Tokens.INDENT, range);
								}
							} else if (spaceCount < oldCount) {
								// Remove indents from stack and emit DEDENT(s).
								// Emit PARTIAL_DEDENT if user doesn't decrease indent all the
								// way to its previous level. (PARTIAL_DEDENT is parser-visible, 
								// so the parser wil emit a syntax error unless it is designed 
								// specifically to handle/ignore PARTIAL_DEDENT.)
								do {
									_indentStack.Pop();
									if (spaceCount > _indentStack.Peek()) {
										_indentStack.Push(spaceCount);
										yield return new AstNode(Tokens.PARTIAL_DEDENT, lastNewline.Range);
										break;
									}
									yield return new AstNode(Tokens.DEDENT, lastNewline.Range);
								} while (spaceCount != _indentStack.Peek());
							}
							colonIndentFlag = false;
						}
						//////////////////////////////////////////////////////////////
					} 
					else if (Tokens.IsOpener(_tt)) 
						_skipWhitespaceRegion++;
					else if (_skipWhitespaceRegion > 0 && Tokens.IsCloser(_tt)) 
						_skipWhitespaceRegion--;
				}
				yield return _t;
			}
		end:
			// At the end, end statement and close indents if necessary.
			SourceRange range2 = new SourceRange(_t.Range.Source, _t.Range.EndIndex, _t.Range.EndIndex);
			if (_produceEOS) {
				_produceEOS = false;
				yield return new AstNode(Tokens.EOS, range2);
			}
			while (_indentStack.Peek() > 0) {
				_indentStack.Pop();
                yield return new AstNode(Tokens.DEDENT, range2);
			}
		}

		private void AutoWriteMixedSpacingWarning(SourcePos p)
		{
			// Give warning about mixing spaces/tabs if appropriate
			if (_usesSpaces && _usesTabs)
				Warning.Write(p, "Mixing spaces and tabs on the same line");
			else if ((_usesSpaces && _usedTabs) || (_usesTabs && _usedSpaces))
				Warning.Write(p, "Switching indentation style from spaces to tabs or vice versa");
			_usedTabs = _usesTabs;
			_usedSpaces = _usesSpaces;
		}

		private int CountSpaces(string p)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		protected int _skipWhitespaceRegion = 0;
		bool SkippingWS 
			{ get { return _skipWhitespaceRegion > 0; } }
		internal void EnterSkipWSRegion()
			{ ++_skipWhitespaceRegion; }
		internal void LeaveSkipWSRegion()
			{ --_skipWhitespaceRegion; }
	}
}