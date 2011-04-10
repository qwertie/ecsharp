using System;
using System.Collections.Generic;
using System.Text;
using Loyc.CompilerCore;
using Loyc.Essentials;
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
	/// if false: throw FalseIsTrueException()
	/// else: print 'QC OK!'
	/// </code>
	/// I believe the parser should be given the same token stream as if the input
	/// had been
	/// <code>
	/// if false: 
	///   throw FalseIsTrueException()
	/// else: 
	///   print 'QC OK!'
	/// </code>
	/// Therefore, this class produces an INDENT token when the colon is encountered 
	/// rather than waiting for a newline and an indent. It then produces a DEDENT 
	/// token when it turns out that the next line is not indented. On the other hand, 
	/// if the next line is indented...
	/// <code>
	/// if false: print 'Oh no!!!'
	///     throw FalseIsTrueException()
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
	/// <para/>
	/// When inside brackets or parenthesis, this class switches to WSA mode. Thus,
	/// unlike standard boo, Loyc allows you to use WSA mode and normal mode in the 
	/// same source file.
	/// <para/>
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
	/// and 6 because no tokens are parser-visible on those lines; EOS is not 
	/// produced for lines 3 and 5 because the last visible token on those lines is 
	/// INDENT or COLON. There's one more wrinkle: when "|" is used for line 
	/// continuation, EOS must not be produced, so GetEnumerator() does not produce
	/// EOS until it reaches the beginning of the next line, to confirm it is not 
	/// continued.
	/// <para/>
	/// Normally, as you've seen, code indentation normally determines structure,
	/// and the end of a line typically indicates the end of a statement. These two 
	/// features are suspended, however, inside bracketed regions such as 
	/// (parenthesis) and {curly braces}. For example, in the following code, the 
	/// end-of-line and indentation before '==' is ignored:
	/// <code>
	/// def foo():
	///     // Note: a line break in an expression is illegal outside parenthesis
	///     if (2+2
	///         ==4):
	///         print "2+2 is 4!"
	/// </code>
	/// A region that ignores newlines like this is called "whitespace agnostic" or 
	/// WSA. Because BooLexer is intended to be used in a smart IDE, however, it 
	/// contains a deliberate limitation that is different than standard boo: it 
	/// requires that inside one of these WSA regions, the indentation level never 
	/// drop below the indentation level on the line that started the WSA region.
	/// For example, the following code is legal in standard boo, but illegal 
	/// according to BooLexer:
	/// <code>
	/// def foo():
	///     // Note: a line break in an expression is illegal outside parenthesis
	///     if (2+2
	/// ==4):
	///         print "2+2 is 4!"
	/// </code>
	/// The reason has to do with the fact that code in an editor is often missing 
	/// close parenthesis. For example, suppose I am typing the statement on line 2 
	/// but I haven't finished it yet:
	/// <code>
	/// (1) def foo():
	/// (2)     if (2+2
	/// (3)         print "2+2 is 4!"
	/// (4) def bar():
	/// (5)     print "this is bar."
	/// </code>
	/// The code is missing a close parenthesis because I haven't typed it yet. In 
	/// order for a smart IDE to understand the code, it must be able to see that 
	/// the method 'bar()' is not part of the 'if' statement. So, if the 
	/// indentation decreases before reaching a closing bracket, BooLexer assumes
	/// that the programmer forgot the closing bracket and inserts one 
	/// automatically. The effect is to localize the syntax error to the block in 
	/// which it occurs.
	/// <para/>
	/// By the way, the Essential Tree Parser (ETP) has its own error-recovery 
	/// mechanism, but that mechanism cannot cope with this problem because it 
	/// operates at a higher level. ETP must know where INDENTs, DEDENTs and EOSs 
	/// are located, but a missing close bracket prevents those tokens from being 
	/// generated in the first place. Thus, all code after the missing close paren 
	/// is meaningless without the recovery mechanism in BooLexer.
	/// <para/>
	/// </remarks>
	public class BooLexer : IEnumerable<AstNode>
	{
		public BooLexer(ISourceFile source, IDictionary<string, Symbol> keywords, bool wsaOnly) 
			: this(source, keywords, wsaOnly, 4) {}
		public BooLexer(ISourceFile source, IDictionary<string, Symbol> keywords, bool wsaOnly, int spacesPerTab)
			: this(new BooLexerCore(source, keywords), wsaOnly, spacesPerTab) { }
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
		protected int _indentation = 0;
		/// <summary>Contains the number of spaces at each indentation level.</summary>
		/// <remarks>Look at the code of GetEnumerator() to see exactly how 
		/// the stack is used. Note that it's initialized with a zero on the 
		/// top-of-stack.</remarks>
		protected Stack<int> _indentStack;
		protected readonly bool _wsaOnly;
		protected int _skipWhitespaceRegion = 0;
		protected bool IsWSA { get { return _wsaOnly || _skipWhitespaceRegion > 0; } }
		
		/// <summary>Current token being processed by GetEnumerator(). If there are no tokens left, this points to the last token in the file.</summary>
		
		struct TokenEtc
		{
			public TokenEtc(AstNode node)
			{
				Node = node;
				SpacesAfter = LineIndentation = 0;
				ProduceEOS = false;
			}
			public bool IsNull { get { return Node == null; } }
			public bool IsOob() { return Node.IsOob(); }
			public AstNode Node;
			/// <summary>Space characters (' ') after _t</summary>
			public int SpacesAfter;
			/// <summary>Indentation of the line that the token is in</summary>
			public int LineIndentation;
			/// <summary>Whether an EOS should be produced at the next newline</summary>
			public bool ProduceEOS;
			public string NodeText { get { return ((ITokenValue)Node).Text; } }
		}
		private TokenEtc _t;
		private TokenEtc _prev_t, _next_t;
		/// <summary>Current token type (_t.NodeType), or null if there are no more tokens left.</summary>
		private Symbol _tt { get { return _t.Node != null ? _t.Node.NodeType : null; } }
		/// <summary>Used to enforce "only call GetEnumerator once" rule</summary>
		protected bool _enumerationStarted = false;
		/// <summary>Loads next _t and sets or clears _produceEOS as appropriate.</summary>

		static Symbol _LineIndentation = GSymbol.Get("LineIndentation");
		
		private bool Advance()
		{
			bool produceEOS = _t.ProduceEOS;
			if (_tt == BooLexerCore._COLON || _tt == Tokens.INDENT || _tt == Tokens.EOS)
				produceEOS = false;
			else if (_t.Node != null && !_t.IsOob())
				produceEOS = true;

			// Advance
			_prev_t = _t;
			if (_next_t.Node != null)
				_t = _next_t;
			else {
				int sa;
				_t = new TokenEtc(_lexer.ParseNext(out sa));
				_t.SpacesAfter = sa;
				_t.ProduceEOS = produceEOS;
				_t.LineIndentation = _indentation;
			}
			_next_t.Node = null;

			return _t.Node != null;
		}
		private void UndoAdvance()
		{
			Debug.Assert(_next_t.IsNull && !_prev_t.IsNull);
			_next_t = _t;
			_t = _prev_t;
			_prev_t.Node = null;
		}

		private AstNode NewEmptyToken(Symbol type)
		{
			SourceRange r;
			if (!_t.IsNull) {
				r = new SourceRange(_t.Node.Range.Source, _t.Node.Range.BeginIndex, 0);
			} else {
				r = new SourceRange(_prev_t.Node.Range.Source, _prev_t.Node.Range.BeginIndex, 0);
			}
			AstNode t = AstNode.New(r, type);
			return t;
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
			
			AstNode lastNewline = null;

			if (_enumerationStarted)
				throw new InvalidOperationException(Localize.From("BooLexer.GetEnumerator() can only be called once per lexer."));
			_enumerationStarted = true;

			while (Advance()) {
			redo:
				if (_tt == BooLexerCore._COLON && !IsWSA)
				{
					//////////////////////////////////////////////////////////////
					// Emit INDENT after a colon (in normal mode) ////////////////
					yield return _t.Node;
					if (!Advance()) break;

					yield return NewEmptyToken(Tokens.INDENT);
					if (colonIndentFlag)
						_indentStack.Push(_indentStack.Peek());
					else
						_indentStack.Push(_indentStack.Peek() + 1);
					colonIndentFlag = true;
					goto redo;
					//////////////////////////////////////////////////////////////
				}
				else if (_tt == Tokens.NEWLINE || isFirstToken)
				{
					//////////////////////////////////////////////////////////////
					// We are at the beginning of a line /////////////////////////

					// Bug fix: Advance() sets ProduceEOS when it encounters "."
					// because PUNC is not OOB. We'll restore it when it turns out 
					bool savedFlag = _t.ProduceEOS; // to be a spacer like ".   "

					if (isFirstToken) {
						isFirstToken = false;
						_indentation = 0;
					} else {
						lastNewline = _t.Node;
						// Proceed past the NEWLINE
						yield return _t.Node;
						_indentation = _t.SpacesAfter;
						if (!Advance()) break;
					}
					// Measure the type (spaces or tabs) and amount of whitespace.
					// Match DOT_INDENT | NORMAL_INDENT and keep track of spaces/tabs, 
					// where
					//   NORMAL_INDENT: WS?;
					//   DOT_INDENT: ('.' WS)+;
					// The type of the dot in DOT_INDENT is changed from PUNC to WS.
					_usesTabs = false;
					_usesSpaces = _indentation > 0;
					if (!_usesSpaces && _tt == Tokens.PUNC && _t.NodeText == ".") {
						// Match ('.' WS)+ and count the number of groups. Note
						// that if '.' is not followed by WS, we must backtrack.
						do {
							if (_t.SpacesAfter > 0)
								_indentation += SpacesPerTab;
							bool haveNext = Advance(); // _t becomes _prev_t 

							if (_prev_t.SpacesAfter == 0) {
								if (_tt == Tokens.WS)
									_indentation += SpacesPerTab;
								else {
									// Oops, the dot was not followed by spaces, 
									// so it does not represent spaces.
									UndoAdvance();
									break;
								}
							}
							_usesTabs = true;

							// Replace the PUNC token with WS to hide it from parser
							yield return _prev_t.Node.WithType(Tokens.WS);
							
							_t.ProduceEOS = savedFlag; // Bug fix explained above

							if (_tt == Tokens.WS) {
								yield return _t.Node;
								if (!Advance())
									goto end; // (break from outer loop)
							} else if (!haveNext)
								goto end; // (break from outer loop)
						} while (_tt == Tokens.PUNC && _t.NodeText == ".");
					}
					else if (_tt == Tokens.WS) {
						// Match WS
						_indentation += CountSpaces(_t.NodeText, ref _usesSpaces, ref _usesTabs);

						yield return _t.Node;
						if (!Advance()) break; // (break from outer loop)
						Debug.Assert(_tt != Tokens.WS);
					}

					// Reached the first non-WS token on the line. Save the 
					// _indentation in it, in case external code is interested in 
					// finding out the indentation of the line. We don't save it
					// in *every* token because Extra Tags suck up extra memory.
					// Use G.Cache() to limit the number of boxed integers.
					_t.Node.SetTag(_LineIndentation, G.Cache(_indentation));

					if (!_wsaOnly)
						// Set a flag now. We will continue processing if/when 
						// a parser-visible token is encountered.
						indentPending = true;
					goto redo;
					//////////////////////////////////////////////////////////////
				}
				else if (!_t.IsOob() && indentPending)
				{
					indentPending = false;
					if (_skipWhitespaceRegion > 0 && _indentation < _indentStack.Peek())
					{
						// User dedented without providing closing brackets; so
						// assume the user simply forgot them.
						_skipWhitespaceRegion = 0; 
						// This is important for error recovery in an IDE, because
						// if the user did forget close bracket(s), and we fail to
						// recover, nothing after this point can be parsed
						// correctly. On the other hand, if the user dedented but
						// otherwise made no mistake (i.e. the close brackets are
						// coming later) then this recovery action will lead to
						// syntax errors. Unfortunately, handling both situations
						// would require major design changes.
					}
					if (_skipWhitespaceRegion == 0) {
						//////////////////////////////////////////////////////////////
						// Reached first parser-visible token on non-WSA line
						//////////////////////////////////////////////////////////////

						// Is it a line continuation token?
						if (_tt == Tokens.PUNC && _t.NodeText == "|") {
							// Yes. Replace PUNC with a LINE_CONTINUATION.
							yield return _t.Node.WithType(Tokens.LINE_CONTINUATION);
							if (!Advance()) break;
							goto redo;
						} else {
							// No. Do normal processing for the beginning of a new line.
							
							// Warn if spaces are mixed with tabs improperly
							AutoWriteMixedSpacingWarning(_t.Node.Range.Begin);

							if (_t.ProduceEOS) {
								_t.ProduceEOS = false;
								yield return NewEmptyToken(Tokens.EOS);
							}

							// Emit INDENT/DEDENT(s) if necessary.
							// Look for change of indentation...
							int oldCount = _indentStack.Peek();
							if (_indentation > oldCount) {
								// Indent increased compared to previous line
								if (colonIndentFlag) {
									// Correct the value at the top of stack
									_indentStack.Pop();
									Debug.Assert(oldCount == _indentStack.Peek() + 1);
									_indentStack.Push(_indentation);
								} else {
									// Push new spacing on stack and emit INDENT
									_indentStack.Push(_indentation);
									yield return NewEmptyToken(Tokens.INDENT);
								}
							} else if (_indentation < oldCount) {
								// Remove indents from stack and emit DEDENT(s).
								// Emit PARTIAL_DEDENT if user doesn't decrease indent all the
								// way to its previous level. (PARTIAL_DEDENT is parser-visible, 
								// so the parser wil emit a syntax error unless it is designed 
								// specifically to handle/ignore PARTIAL_DEDENT.)
								do {
									_indentStack.Pop();
									if (_indentation > _indentStack.Peek()) {
										_indentStack.Push(_indentation);

										yield return NewEmptyToken(Tokens.PARTIAL_DEDENT);
										break;
									}
									yield return NewEmptyToken(Tokens.DEDENT);
								} while (_indentation != _indentStack.Peek());
							}
							colonIndentFlag = false;
						}
						//////////////////////////////////////////////////////////////
					}
				} 
				else if (Tokens.IsOpener(_tt)) 
					_skipWhitespaceRegion++;
				else if (_skipWhitespaceRegion > 0 && Tokens.IsCloser(_tt)) 
					_skipWhitespaceRegion--;

				yield return _t.Node;
			}
		end:
			// At the end, end statement and close indents if necessary.
			Debug.Assert(_t.Node == null);
			if (_t.ProduceEOS) {
				_t.ProduceEOS = false;
				yield return NewEmptyToken(Tokens.EOS);
			}
			while (_indentStack.Count > 1) {
				_indentStack.Pop();
				yield return NewEmptyToken(Tokens.DEDENT);
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

		protected internal int CountSpaces(string s, ref bool usesSpaces, ref bool usesTabs)
		{
			int count = 0;
			for (int i = 0; i < s.Length; i++) {
				if (s[i] == '\t') {
					usesTabs = true;
					count += _spacesPerTab;
				} else {
					Debug.Assert(s[i] == ' ');
					usesSpaces = true;
					count++;
				}
			}
			return count;
		}
	}

	// TODO: For test cases, see BooLexerTest.cs
}