using System;
using System.Collections.Generic;
using System.Text;
using Loyc.CompilerCore;
using Loyc.Runtime;
using System.Diagnostics;
using NUnit.Framework;
using Loyc.BooStyle;
using Loyc.Utilities;

namespace Loyc.BooStyle
{
	public delegate void ETPRecoveryStrategy(ref AstNode root, List<EssentialTreeParser.ErrorNode> errors);

	/// <summary>
	/// Converts a token list into a token tree. Everything inside brackets or
	/// INDENTs is made a child of the open bracket's Block.
	/// </summary>
	/// <remarks>
	/// EssentialTreeParser emits errors in one of the following three error
	/// conditions:
	/// <ol>
	/// <li>A close bracket is found at the root level, meaning there's no matching
	///     open bracket.</li>
	/// <li>End-of-file is reached without enough close brackets</li>
	/// <li>The opening bracket and close bracket do not match. There are three
	///     classes of opening and closing brackets: INDENT/DEDENT, parenthesis
	///     (round and square  brackets plus EXTRA_LPAREN/EXTRA_RPAREN), and
	///     braces (curly braces plus EXTRA_LBRACE/EXTRA_PBRACE). Every open
	///     bracket must be matched by a closing bracket in the same class.
	///     EssentialTreeParser's response depends on the bracket type, as
	///     described below.</li>
	/// </ol>
	/// In all cases, every token of the input is emitted to the output, and
	/// EssentialTreeParser itself does not attempt to insert synthetic tokens for
	/// recovery; that is left to the "recovery strategy" that can be installed by
	/// setting the RecoveryStrategy property.
	/// <para/>
	/// The only case that EssentialTreeParser treats specially is bracket mismatch.
	/// When a close bracket is incompatible with the corresponding open bracket,
	/// EssentialTreeParser either ignores it (adding it to the output at the
	/// current tree level) or it returns to a higher tree level before adding it.
	/// The latter action is taken if the parent node is not INDENT, and either
	/// <ul>
	/// <li>The close bracket is DEDENT</li>
	/// <li>The close bracket's LineIndentation is not more than the parent's</li>
	/// </ul>
	/// EssentialTreeParser returns to the next higher level repeatedly (giving an
	/// error at each mismatch) until either the bracket finds its match, or the
	/// second condition is met. This strategy recovers in some cases, but most
	/// errors are discovered too late for an effective recovery. For example,
	/// consider this common error:
	/// <code>
	/// class Foo {
	///    void f(bool gah) {
	///       if (gah)
	///          g();
	///       else
	///          h();
	///   
	///    void g() {
	///       ...
	///    }
	///    void h() {
	///       ...
	///    }
	/// }
	/// </code>
	/// Here, the missing close brace is not detected until the end, by which time
	/// g() and h() have already been added to the token tree as children of f's
	/// body. Hence it would be nice to have a recovery strategy that would detect
	/// the need for a closing brace (or opening brace) at the correct location
	/// according to indentation, but a strategy has not been written yet.
	/// <para/>
	/// A recovery strategy will be important for IDE "intellisense" of Loyc C#
	/// because it is normal for code to have missing braces, especially close
	/// braces, while the user is writing code. In the example above, the class
	/// declaration is unparsable because it lacks a closing brace, so in fact none
	/// of the source file can be parsed at all. Consequently, intellisense is not
	/// possible because there is no context available in which to interpret the
	/// user's typing.
	/// <para/>
	/// 
	/// 
	/// Everything below is a scratchpad.
	/// 
	/// ETP error detection &amp; recovery scenarios:
	/// <code>
	/// It is suspected that within brackets or braces, all tokens should be at the
	/// same level and deeper than the brackets/braces at previous levels
	/// 
	/// Suspicions are caused by:
	/// - Indent without '{' => suspect '{'
	/// - Unindent (or failure to indent after '{') without '}' => suspect '}'
	/// 
	/// Topmost suspicion is resolved by
	/// - '{' or '(' found at level where it was expected
	/// 
	/// 
	/// 
	/// class Foo (
	///(     def f()
	///(    ?    blah
	///(     garbage foo )
	///      }                   // ignore extra
	/// )                        // ignore extra
	/// 
	/// if (foo
	///(    == bar)
	///?   print
	///?}                        // match suspect
	/// 
	/// void f()
	///?    if (x >= 2)
	///?   ?    x = 2
	/// }
	/// 
	/// void f() {
	///       bla {
	///    bla {
	///           }
	///     }
	///
	/// 
	/// 
	/// Rule: when opener used, delete alternate opener
	/// 
	/// Source file example #1
	/// ----------------------
	/// class Foo {										 {
	///		def f():									 { X( X)
	///     // suspect #1 {(							 { 1{(
	///         return goob								 
	///     } // split suspect #1						 ~{ 1{ ~}
	///
	///		void g()									 1{ 1( X( X)
	///		// suspect #2 {								 1{ 1( 2{(
	///			if (x) { blah blah }					 1{ 1( 2{( ~( ~) ~{ ~}
	///         if x == 3 &&							 1{ 1( 2{(
	///         // suspect #3 {							 1{ 1( 2{( 3{(
	///				y == 4 {							 1{ 1( 2{( 3{( {
	///			// suspect } due to dedent--cancels #3	 1{ 1( 2{( ~3{( { ~0)}
	///			}										 1{ 1( 2{( ~{ ~}
	///		} // no matching {							 1{ 1( 2{( }
	///		// Recovery: insert { at suspicion #2		 1{ 1( ~2{ ~}
	/// } // no matching {								 1{ 1( }
	/// // Recovery: insert { at suspicion #1			 ~1{ ~}
	/// 
	/// class Foo {										 {
	///		def f() {									 { ~( ~) {
	///         return goob
	/// 
	///     // suspect #4: } due to unindent			 { { 4}
	///		void g() {									 { { 4} ~( ~) {
	///			if (x) { ... }							 { { 4} { ~( ~) ~{ ~}
	///		}											 { { 4} ~{ ~}
	/// }												 { ~{ 4} ~}
	/// 
	/// class Foo {										 { 4} {
	///		void f() {									 { 4} { ~( ~) {
	///			if (x) {								 { 4} { { ~( ~) {
	///         // Suspicion #5: need } due to dedent    { 4} { { { 5}
	///         Console.WriteLine(x);					 { 4} { { { 5} ~( ~)
	///         if (y {}								 { 4} { { { 5} ( ~{ ~}
	///         // Suspicion #6: need ) due to dedent	 { 4} { { { 5} ( 6)
	///			) // Close suspicion #6 cuz ) found		 { 4} { { { 5} ~( ~6) ~)
	///		}
	/// 
	///		void g(int x = (12+2))/3)					 { 4} { { { 5} ~( ~( ~) ~) !)
	///     // Error above: last ) unmatchable (btw, brackets and braces can't 
	///		// match). No matching suspicion, so ignore token or leave unmatched.
	///		{											 { 4} { { { 5} {
	///			if (x { ... }							 { 4} { { { 5} { ( ~{ ~}
	///		// Suspect ) due to dedent                   { 4} { { { 5} { ( 0)
	///		}                                            { 4} { { { 5} { ( 0) }
	///		// Error: } can't match (. Recovery: 0)      { 4} { { { 5} ~{ ~( ~) ~}
	/// 
	///		void h() {                                   { 4} { { { 5} ~( ~) {
	///         if (x == 3 &&							 { 4} { { { 5} { (
	///				y == 4)								 { 4} { { { 5} { ~( ~)
	///	            // suspect #6 due to indent			 { 4} { { { 5} { 6{(
	///	            foo	                                     
	///			}
	///		}
	/// }
	/// // Error at end. Recovery: insert } at suspicion #5
	/// // Still an error. Recovery: insert } at suspicion #4
	/// </code>
	/// </remarks>
	public class EssentialTreeParser
	{
		public EssentialTreeParser() { }

		int _maxDepth = 100;
		public int MaxDepth
		{
			get { return _maxDepth; } 
			set {
				if (value < 2) throw new ArgumentException();
				_maxDepth = value;
			}
		}

		protected ETPRecoveryStrategy _recoveryStrategy;
		public ETPRecoveryStrategy RecoveryStrategy
		{
			get { return _recoveryStrategy; }
			set { _recoveryStrategy = value; }
		}

		//protected IEnumerator<object> _spState;
		
		public virtual bool Parse(ref AstNode rootNode, IEnumerable<AstNode> source)
		{
			_errorNodes.Clear();
			//_spState = SuspicionProcessor();
			IEnumerator<AstNode> e = source.GetEnumerator();
			AstNode closer;
			bool success = Parse(ref rootNode, out closer, e, new SymbolSet(), 0);
			PrintErrors();
			if (_errorNodes.Count > 0 && RecoveryStrategy != null)
				RecoveryStrategy(ref rootNode, _errorNodes);
			return _errorNodes.Count == 0;
		}

		protected virtual bool Parse(ref AstNode parent, out AstNode closer, IEnumerator<AstNode> e, SymbolSet possibleClosers, int depth)
		{
			RWList<AstNode> children = new RWList<AstNode>();
			AstNode cur;

			try {
				while (MoveNext(e, out cur))
				{
					if (cur == null)
					{
						children.Add(null);
						continue;
					}

					if (Tokens.IsOpener(cur.NodeType))
						if (!ParseInner(children, e, possibleClosers, depth, ref cur)) {
							closer = null;
							return false;
						}
					if (cur != null && Tokens.IsCloser(cur.NodeType)) {
						if (possibleClosers.Contains(cur.NodeType)) {
							closer = cur;
							return true;
						} else {
							_errorNodes.Add(new ErrorNode(cur,
								"'{0}': No matching opening bracket", cur.ShortName));
						}
					}
					if (cur != null)
						children.Add(cur);
				}
				closer = null;
				return possibleClosers.IsEmpty;
			} finally {
				if (!children.IsEmpty)
					parent = parent.WithChildren(children.ToRVList());
			}
		}

		/// <summary>Processes all children of the specified 'opener'; adds the 
		/// opener (with new children) and closer to the 'children' list.</summary>
		/// <remarks>ParseInner normally sets opener to null. If the closer doesn't 
		/// match the opener, it sets opener = closer to allow the caller to handle
		/// it.</remarks>
		/// <returns>Returns false to abort all processing, typically when end of 
		/// stream is reached.</returns>
		private bool ParseInner(RWList<AstNode> children, IEnumerator<AstNode> e, SymbolSet possibleClosers, int depth, ref AstNode opener)
		{
			Symbol type = opener.NodeType;
			if (depth > MaxDepth)
			{
				_errorNodes.Add(new ErrorNode(opener, "Bracket depth limit of {#} reached", "#", MaxDepth));
				return false;
			}

			// Get the set of tokens that SHOULD close this level
			SymbolSet curClosers = null;
			if (type == Tokens.INDENT)
				curClosers = Tokens.SetOfDedent;
			else if (Tokens.IsOpenParen(type))
				curClosers = Tokens.SetOfCloseParens;
			else if (Tokens.IsOpenBrace(type))
				curClosers = Tokens.SetOfCloseBraces;

			// Get the set of tokens that CAN close this level
			SymbolSet allClosers = curClosers;
			if (!possibleClosers.IsEmpty)
			{
				allClosers = new SymbolSet(curClosers);
				allClosers.AddRange(possibleClosers);
			}

			AstNode closer;
			if (!Parse(ref opener, out closer, e, allClosers, depth + 1))
			{
				_errorNodes.Add(new ErrorNode(opener,
					"'{0}': No matching closing bracket", opener.ShortName));
				return false; // premature end-of-stream or depth limit reached
			}

			if (opener != null) {
				children.Add(opener);
			
				if (curClosers.Contains(closer.NodeType)) {
					// Normal case: add this closer beside the opener
					children.Add(closer);
					opener = null;
				} else {
					_errorNodes.Add(new ErrorNode(closer,
						"'{closer}' doesn't match '{opener}'.",
						"closer", closer.ShortName, "opener", opener.ShortName));
					opener = closer; // return mismatched bracket to the caller
				}
			}
			return true;
		}

		protected bool MoveNext(IEnumerator<AstNode> e, out AstNode cur)
		{
			bool success = e.MoveNext();
			if (success) {
				cur = e.Current; // may be null
			} else
				cur = null;
			return success;
		}

		/*struct Bracket
		{
			public static Bracket Opener(AstNode node, int spaces)
			{
				Bracket b = new Bracket();
				b.Node = node;
				b.Type = node.NodeType;
				Debug.Assert(Tokens.IsOpener(b.Type));
				b.IsOpener = true;
				b.IsSuspicion = false;
				b.IsBrace = Tokens.IsOpenBrace(b.Type);
				b.IsParen = Tokens.IsOpenParen(b.Type);
				b.Spaces = spaces;
				return b;
			}
			public static Bracket Closer(AstNode node, int spaces)
			{
				Bracket b = new Bracket();
				b.Node = node;
				b.Type = node.NodeType;
				Debug.Assert(Tokens.IsCloser(b.Type));
				b.IsOpener = false;
				b.IsSuspicion = false;
				b.IsBrace = Tokens.IsCloseBrace(b.Type);
				b.IsParen = Tokens.IsCloseParen(b.Type);
				b.Spaces = spaces;
				return b;
			}
			public static Bracket SuspectOpener(AstNode node, int spaces)
			{
				Bracket b = new Bracket();
				b.Node = node;
				b.Type = Tokens.LBRACE;
				b.IsOpener = b.IsSuspicion = true;
				b.IsBrace = b.IsParen = true;
				b.Spaces = spaces;
				return b;
			}
			public static Bracket SuspectCloser(AstNode node, int spaces)
			{
				Bracket b = new Bracket();
				b.Node = node;
				b.Type = Tokens.RBRACE;
				b.IsOpener = false;
				b.IsSuspicion = true;
				b.IsBrace = true;
				b.IsParen = true;
				b.Spaces = spaces;
				return b;
			}
			public bool IsOpener;
			public bool IsBrace;
			public bool IsParen;
			public bool IsSuspicion;
			public Symbol Type;
			public AstNode Node;
			public int Spaces;
		}*/

		public class ErrorNode
		{
			public ErrorNode(AstNode node, [Localizable] string msg, params object[] args)
				{ Node = node; Message = Localize.From(msg, args); }
			public AstNode Node;
			public string Message;
		}

		protected List<ErrorNode> _errorNodes = new List<ErrorNode>();

		/*protected class Bracket
		{
			public Bracket(AstNode node, int spaces, bool isOpener) 
				{ Node = node; Spaces = spaces; IsOpener = isOpener; }
			public AstNode Node;
			public int Spaces;
			public bool IsOpener;
		}
		protected List<Bracket> _suspicions = new List<Bracket>(16);

        /// <summary>A coroutine that guesses where open or close brackets should
		/// be placed in case of problems matching brackets.</summary>
        /// <returns>SuspicionProcessor() does 'yield return null' to move to the
        /// next _cur token, so ignore the enumerator values.</returns>
        /// <remarks>
		/// SuspicionProcessor() tracks the number of spaces at the beginning of
		/// the current and previous lines, and the net number of openers and
		/// closers on each line. It watches for situations that could warrant
		/// the insertion of open or close brackets. Out-of-band (OOB) tokens are
		/// ignored except for newlines and spaces that are relevant to the
		/// analysis.
		/// <para/>
		/// Suspicions occur when
		/// <ol>
		/// <li>A line is indented but there was no opener on the previous line.</li>
		/// <li>A line is dedented but it does not begin with a closer.</li>
		/// <li>A line is not indented even though the previous line had openers.</li>
		/// </ol>
		/// A suspected closer cancels out a suspected opener if that opener was on
		/// a line that began with the exact same number of spaces.
		/// <para/>
		/// Currently, SuspicionProcessor() ignores all brackets/braces except
		/// closers that begin a line, and its analysis is strictly local
		/// (suspicions are derived from comparing the current line with the
		/// immediately previous line.)
        /// </remarks>
		protected virtual IEnumerator<object> SuspicionProcessor()
		{
			_suspicions.Clear();

			int prevSpaces = 0, curSpaces = 0, newSpaces;
			int nestingThisLine = 0, nestingLastLine = 0;
			AstNode spaceToken;
			while (_cur != null) {
				// Measure the number of spaces at the start of this line
				newSpaces = _cur.SpacesAfter;
				yield return null;
				if (_cur.NodeType == Tokens.WS) {
					spaceToken = _cur;
					newSpaces += CountSpaces(_cur.Text);
					newSpaces += _cur.SpacesAfter;
					yield return null;
				} else
					spaceToken = null;

				// Now examine the tokens on the line, looking for any non-oob
				// tokens.
				AstNode firstOnLine = null;
				while (_cur != null && _cur.NodeType != Tokens.NEWLINE) {
					if (!_cur.IsOob) {
						if (firstOnLine == null) {
							// This is the first relevant token on the line.
							prevSpaces = curSpaces;
							curSpaces = newSpaces;
							nestingLastLine = nestingThisLine;
							nestingThisLine = 0;
						}
						
						Symbol type = _cur.NodeType;
						if (Tokens.IsOpener(type))
							nestingThisLine++;
						else if (Tokens.IsCloser(type))
							nestingThisLine--;
						
						if (firstOnLine == null) {
							firstOnLine = _cur;
							if (nestingLastLine <= 0 && newSpaces > prevSpaces)
								_suspicions.Add(new Bracket(_cur, curSpaces, true));
							while (nestingLastLine >= 0 && newSpaces <= prevSpaces) {
								if (nestingLastLine == 0 && newSpaces == prevSpaces)
									break;
								_suspicions.Add(new Bracket(_cur, curSpaces, false));
								nestingLastLine--;
							}
						}
					}
					yield return null;
				}
				if (firstOnLine == null)
					continue; // Ignore the line, it had only OOB tokens
			}
		}

		private int CountSpaces(string s)
		{
			int count = 0;
			for (int i = 0; i < s.Length; i++)
				count += s[i] == '\t' ? _spacesPerTab : 1;
			return count;
		}*/

		protected virtual void PrintErrors()
		{
			foreach (ErrorNode e in _errorNodes)
				Error.Write(e.Node.Range.Begin, e.Message);
		}
	}

	//class BooTreeParser : EssentialTreeParser
	//{
	//	public BooTreeParser(ICharSource source, IDictionary<string, Symbol> keywords, bool wsaOnly)
	//		: base(new BooLexer(source, keywords, wsaOnly)) { }
	//}
}

[TestFixture]
public class EssentialTreeParserTests
{
	[Test]
	public void Test1() {
		DoTest(
			"class Foo {\n" +
			"  int seven() { return 7; }\n" +
			"}",
			false, true,
			"_class ID LBRACE RBRACE",
				"NEWLINE _int ID LPAREN RPAREN LBRACE RBRACE NEWLINE",
					"_return INT EOS");
		DoTest(
			"class Foo:\n" +
			"  def Seven():\n" + 
			"    return _seven\n" +
			"  _seven = 7",
			true, true,
			"_class ID COLON INDENT DEDENT",
				"NEWLINE _def ID LPAREN RPAREN COLON INDENT DEDENT ID PUNC INT EOS",
					"NEWLINE _return ID NEWLINE EOS");
	}

	public void DoTest(string input, bool boo, bool success, params object[] outputs)
	{
		ILanguageStyle lang;
		ISourceFile src;
		IEnumerable<AstNode> lexer;
		if (boo) {
			lang = new BooLanguage();
			src = new StringCharSourceFile(lang, input);
			lexer = new BooLexer(src, src.Language.StandardKeywords, false);
		} else {
			lang = new BooLanguage();
			src = new StringCharSourceFile(lang, input);
			lexer = new BooLexerCore(src, src.Language.StandardKeywords);
		}
		EssentialTreeParser etp = new EssentialTreeParser();
		AstNode root = AstNode.New(SourceRange.Nowhere, Symbol.Empty);

		Assert.AreEqual(success, etp.Parse(ref root, lexer));
		CheckOutput(root, outputs, 0);
	}

	private void CheckOutput(AstNode node, object[] data, int dataIndex)
	{
		string expect_s = data[dataIndex].ToString();
		if (expect_s == null)
			return; // null means "ignore these child tokens"
		string[] expTokens = expect_s.Split(' ');
		Assert.AreEqual(expTokens.Length, node.ChildCount);
		for (int i = 0; i < expTokens.Length; i++) {
			Assert.AreEqual(expTokens[i], node.Children[i].NodeType.Name);
			if (node.Children[i].ChildCount > 0)
				CheckOutput(node.Children[i], data, ++dataIndex);
		}
	}
}
