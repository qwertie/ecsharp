using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Utilities;
using Loyc.Syntax;
using Loyc.Syntax.Lexing;
using Loyc.Collections;
using Loyc.Collections.Impl;
using System.Collections;

namespace Ecs.Parser
{
	using S = CodeSymbols;

	/// <summary>Handles EC# processor directives.</summary>
	/// <remarks>This class not only preprocesses C# source code, it saves 
	/// preprocessor directives and comments so that any code excluded by the 
	/// preprocessor can be added back in later, if and when the parsed code is 
	/// printed out. For example, given input like this:
	/// <code>
	///    void foo // see below
	///    #if false
	///    invalid code!
	///    #endif
	///    () { Console.WriteLine("foo()!"); }
	/// </code>
	/// EcsPreprocessor removes the #if...#endif region of tokens, creates a 
	/// single Token of type TokenType.PPFalseBlock to represent that region, and
	/// saves it, after the "see below" comment token, in a list.
	/// <para/>
	/// C# has the following preprocessor directives:
	/// <code>
	/// #define Id
	/// #undef Id
	/// #if expr
	/// #elif expr
	/// #else
	/// #endif
	/// #warning {arbitrary text}
	/// #error {arbitrary text}
	/// #region {arbitrary text}
	/// #endregion
	/// #line 123 "filename"
	/// #pragma warning ...
	/// #pragma ... // ignored
	/// </code>
	/// </remarks>
	public class EcsPreprocessor : LexerWrapper
	{
		public EcsPreprocessor(ILexer source, Action<Token> onComment = null)
			: base(source) { _onComment = onComment; }

		// Can't use ISet<T>: it is new in .NET 4, but HashSet is new in .NET 3.5
		public HashSet<Symbol> DefinedSymbols = new HashSet<Symbol>();

		Action<Token> _onComment;
		List<Token> _commentList = new List<Token>();
		public IList<Token> CommentList { get { return _commentList; } }

		EcsParser _parser = null;

		// Holds the remainder of a preprocessor line. This will be empty for
		// #region, #error, #warning and #note, as the lexer reads the rest of
		// of these lines as text and stores that text as the PP token's Value.
		List<Token> _rest = new List<Token>();
		private void ReadRest()
		{
			_rest.Clear();
			for (;;) {
				Token? t = _source.NextToken();
				if (t == null || t.Value.Type() == TokenType.Newline)
					break;
				else if (!t.Value.IsWhitespace)
					_rest.Add(t.Value);
			}
		}

		Stack<Pair<Token,bool>> _ifRegions = new Stack<Pair<Token,bool>>();
		Stack<Token> _regions = new Stack<Token>();

		public override Token? NextToken()
		{
			do {
				Token? t_ = _source.NextToken();
			redo:
				if (t_ == null)
					break;
			    var t = t_.Value;
			    if (t.IsWhitespace) {
					if (t.Kind == TokenKind.Comment)
						AddComment(t);
					continue;
				} else if (t.Kind == TokenKind.Other) {
					switch (t.Type()) {
					case TokenType.PPdefine:
					case TokenType.PPundef:
						ReadRest();
						bool undef = t.Type() == TokenType.PPundef;
						if (_rest.Count == 1 && _rest[0].Type() == TokenType.Id) {
							if (undef)
								DefinedSymbols.Remove((Symbol)_rest[0].Value);
							else
								DefinedSymbols.Add((Symbol)_rest[0].Value);
						} else
							ErrorSink.Write(Severity.Error, t.ToSourceRange(SourceFile), "'{0}' should be followed by a single, simple identifier", undef ? "#undef" : "#define");
						continue;
					case TokenType.PPif:
						var tree = ReadRestAsTokenTree();
						LNode expr = ParseExpr(tree);

						var cond = Evaluate(expr) ?? false;
						_ifRegions.Push(Pair.Create(t, cond));
						t_ = SaveDirectiveAndAutoSkip(t, cond);
						goto redo;
					case TokenType.PPelse:
					case TokenType.PPelif:
						var tree_ = ReadRestAsTokenTree();

						if (_ifRegions.Count == 0) {
							ErrorSink.Write(Severity.Error, t.ToSourceRange(SourceFile), 
								"Missing #if clause before '{0}'", t);
							_ifRegions.Push(Pair.Create(t, false));
						}
						bool isElif = t.Type() == TokenType.PPelif, hasExpr = tree_.HasIndex(0);
						if (hasExpr != isElif)
							Error(t, isElif ? "Missing condition on #elif" : "Unexpected tokens after #else");
						bool cond_ = true;
						if (hasExpr) {
							LNode expr_ = ParseExpr(tree_);
							cond_ = Evaluate(expr_) ?? false;
						}
						if (_ifRegions.Peek().B)
							cond_ = false;
						t_ = SaveDirectiveAndAutoSkip(t, cond_);
						if (cond_)
							_ifRegions.Push(Pair.Create(_ifRegions.Pop().A, cond_));
						goto redo;
					case TokenType.PPendif:
						var tree__ = ReadRestAsTokenTree();
						if (_ifRegions.Count == 0)
							Error(t, "Missing #if before #endif");
						else {
							_ifRegions.Pop();
							if (tree__.Count > 0)
								Error(t, "Unexpected tokens after #endif");
						}
						_commentList.Add(t);
						continue;
					case TokenType.PPerror:
						_commentList.Add(t);
						Error(t, t.Value.ToString());
						continue;
					case TokenType.PPwarning:
						_commentList.Add(t);
						ErrorSink.Write(Severity.Warning, t.ToSourceRange(SourceFile), t.Value.ToString());
						continue;
					case TokenType.PPregion:
						_commentList.Add(t);
						_regions.Push(t);
						continue;
					case TokenType.PPendregion:
						_commentList.Add(t);
						if (_regions.Count == 0)
							ErrorSink.Write(Severity.Warning, t.ToSourceRange(SourceFile), "#endregion without matching #region");
						else
							_regions.Pop();
						continue;
					case TokenType.PPline:
						_commentList.Add(new Token(t.TypeInt, t.StartIndex, _source.InputPosition));
						var rest = ReadRestAsTokenTree();
						// TODO
						ErrorSink.Write(Severity.Note, t.ToSourceRange(SourceFile), "Support for #line is not implemented");
						continue;
					case TokenType.PPpragma:
						_commentList.Add(new Token(t.TypeInt, t.StartIndex, _source.InputPosition));
						var rest_ = ReadRestAsTokenTree();
						// TODO
						ErrorSink.Write(Severity.Note, t.ToSourceRange(SourceFile), "Support for #pragma is not implemented");
						continue;
					}
			    }
				return t_;
			} while (true);
			// end of stream
			if (_ifRegions.Count > 0)
				ErrorSink.Write(Severity.Error, _ifRegions.Peek().A.ToSourceRange(SourceFile), "#if without matching #endif");
			if (_regions.Count > 0)
				ErrorSink.Write(Severity.Warning, _regions.Peek().ToSourceRange(SourceFile), "#region without matching #endregion");
			return null;
		}

		private void AddComment(Token t)
		{
			if (_commentList != null)
				_commentList.Add(t);
			if (_onComment != null)
				_onComment(t);
		}

		private void Error(Token pptoken, string message)
		{
			ErrorSink.Write(Severity.Error, pptoken.ToSourceRange(SourceFile), message);
		}

		private Token? SaveDirectiveAndAutoSkip(Token pptoken, bool cond)
		{
			_commentList.Add(new Token(pptoken.TypeInt, pptoken.StartIndex, _source.InputPosition));
			if (!cond)
				return SkipIgnoredRegion();
			else
				return _source.NextToken();
		}

		private LNode ParseExpr(IListSource<Token> tree)
		{
			if (_parser == null) _parser = new EcsParser(tree, SourceFile, ErrorSink);
			else _parser.Reset(tree, SourceFile);
			LNode expr = _parser.ExprStart(false);
			return expr;
		}

		// Skips over a region that has is within a "false" #if/#elif/#else region.
		// The region (not including the leading or trailing #if/#elif/#else/#endif)
		// is added to _commentList as a single "token" of type TokenType.PPignored.
		private Token? SkipIgnoredRegion()
		{
			int nestedIfs = 0;
			int startIndex = _source.InputPosition;
			Token? t_;
			while ((t_ = _source.NextToken()) != null) {
				var t = t_.Value;
				if (t.Type() == TokenType.PPif)
					nestedIfs++;
				else if (t.Type() == TokenType.PPendif && --nestedIfs < 0)
					break;
				else if ((t.Type() == TokenType.PPelif || t.Type() == TokenType.PPelse) && nestedIfs == 0)
					break;
			}
			int stopIndex = t_ == null ? _source.InputPosition : t_.Value.StartIndex;
			_commentList.Add(new Token((int)TokenType.PPignored, startIndex, stopIndex - startIndex));
			return t_;
		}

		private bool? Evaluate(LNode expr)
		{
			if (expr.IsId)
				return DefinedSymbols.Contains(expr.Name);
			else if (expr.IsLiteral && expr.Value is bool)
				return (bool)expr.Value;
			else if (expr.Calls(S.And, 2))
				return Evaluate(expr.Args[0]) & Evaluate(expr.Args[1]);
			else if (expr.Calls(S.Or, 2))
				return Evaluate(expr.Args[0]) | Evaluate(expr.Args[1]);
			else if (expr.Calls(S.Not, 1))
				return !Evaluate(expr.Args[0]);
			else if (expr.Calls(S.Eq, 2))
				return Evaluate(expr.Args[0]) == Evaluate(expr.Args[1]);
			else if (expr.Calls(S.Neq, 2))
				return Evaluate(expr.Args[0]) != Evaluate(expr.Args[1]);
			else {
				ErrorSink.Write(Severity.Error, expr.Range, "Only simple boolean expressions with &&, ||, !, ==, !=, are supported in #if and #elif");
				return null;
			}
		}

		private IListSource<Token> ReadRestAsTokenTree()
		{
			ReadRest();
			var restAsLexer = new ListAsLexer(_rest, _source.SourceFile);
			var treeLexer = new TokensToTree(restAsLexer, false);
			return treeLexer.Buffered();
		}
	}

	/// <summary>A helper class that removes comments from a token stream, saving 
	/// them into a list. This class deletes whitespace, but adds tokens to a list.</summary>
	public class CommentSaver : LexerWrapper
	{
		public CommentSaver(ILexer source, IList<Token> commentList = null)
			: base(source) { _commentList = commentList ?? new List<Token>(); }

		IList<Token> _commentList;
		public IList<Token> CommentList { get { return _commentList; } }
	
		public sealed override Token? NextToken()
		{
			Token? t = _source.NextToken();
			for (;;) {
				t = _source.NextToken();
				if (t == null)
					break;
				else if (t.Value.IsWhitespace) {
					if (t.Value.Kind == TokenKind.Comment) {
						_commentList.Add(t.Value);
					}
				} else
					break;
			}
			return t;
		}
	}

	/// <summary>Adapter: converts <c>IEnumerable(Token)</c> to the <see cref="ILexer"/> interface.</summary>
	/// <remarks>TODO: Incomplete, does not track line numbers.</remarks>
	public class ListAsLexer : ILexer
	{
		public ListAsLexer(IEnumerable<Token> tokenList, ISourceFile sourceFile) : this(tokenList.GetEnumerator(), sourceFile) { }
		public ListAsLexer(IEnumerator<Token> tokenList, ISourceFile sourceFile) { _e = tokenList; _sourceFile = sourceFile; }

		IEnumerator<Token> _e;
		ISourceFile _sourceFile;
		Token _current;
		public Loyc.Syntax.ISourceFile SourceFile
		{
			get { return _sourceFile; }
		}

		public Token? NextToken()
		{
			if (MoveNext())
				return _current;
			else
				return null;
		}

		public Loyc.IMessageSink ErrorSink { get; set; }
		public int IndentLevel { get { return 0; } } // TODO
		public int LineNumber { get { return 0; } } // TODO
		public int InputPosition { get { return _current.EndIndex; } }

		public bool MoveNext()
		{
			if (_e.MoveNext()) {
				_current = _e.Current;
				return true;
			}
			return false;
		}
		public Token Current
		{
			get { return _current; }
		}
		object System.Collections.IEnumerator.Current
		{
			get { return _e.Current; }
		}
		void IDisposable.Dispose() { _e.Dispose(); }
		void IEnumerator.Reset() { _e.Reset(); }
	}
}
