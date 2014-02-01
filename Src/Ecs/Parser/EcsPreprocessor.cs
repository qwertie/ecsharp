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
	/// #line
	/// #region {arbitrary text}
	/// #endregion
	/// #pragma warning ...
	/// #pragma ... // ignored
	/// </code>
	/// </remarks>
	public class EcsPreprocessor : LexerWrapper
	{
		public EcsPreprocessor(ILexer source) : base(source) {}

		public ISet<Symbol> DefinedSymbols = new HashSet<Symbol>();

		List<Token> _commentList = new List<Token>();
		public IList<Token> CommentList { get { return _commentList; } }

		EcsParser _parser = null;

		// Holds the remainder of a preprocessor line. This will be empty for
		// #region, #error, #warning and #note, as the lexer reads the rest of
		// of these lines as text and stores that text as the PP token's Value.
		List<Token> _rest;
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

		Stack<Pair<Token,bool>> _ifRegions;

		IEnumerator<Token> Preprocess()
		{
			Token? t_;
			while ((t_ = _source.NextToken()) != null) {
				var t = t_.Value;
				if (!t.IsWhitespace) {
					if (t.Kind == TokenKind.Other) {
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
									ErrorSink.Write(MessageSink.Error, SourceFile.IndexToLine(t.StartIndex), "'{0}' should be followed by a single, simple identifier", undef ? "#undef" : "#define");
								break;
							case TokenType.PPif:
								var tree = ReadRestAsTokenTree();
								if (_parser == null) _parser = new EcsParser(tree, SourceFile, ErrorSink);
								else                 _parser.Reset(tree, SourceFile);
								LNode expr = _parser.ExprStart(false);
					
								//_ifRegions.Push()
								var cond = Evaluate(expr);
								if (cond == true) {
									SkipIgnoredRegion();
								} else {
									
								}
								break;
						}
					}

					yield return t;

				} else if (t.Kind == TokenKind.Comment)
					_commentList.Add(t);
			}
		}

		public override Token? NextToken()
		{
			Token? t_ = _source.NextToken();
			//if (t_ != null) {
			//    var t = t_.Value;
			//    if (!t.IsWhitespace) {
			//        if (t.Kind == TokenKind.Other) {
			//            Preprocess(t);
			//        }
			//    } else if (t.Kind == TokenKind.Comment)
			//        _commentList.Add(t);
			//}
			return t_;
		}

		private void SkipIgnoredRegion()
		{
			for (;;) {
				Token? t_ = _source.NextToken();
				if (t_ == null)
					break;
				var t = t_.Value;
			}
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
				ErrorSink.Write(MessageSink.Error, expr.Range, "Only simple boolean expressions with &&, ||, !, ==, !=, are supported in #if and #elif");
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
					if (t.Value.Kind == TokenKind.Comment)
						_commentList.Add(t.Value);
				} else
					break;
			}
			return t;
		}
	}

	public class ListAsLexer : ILexer
	{
		public ListAsLexer(IEnumerable<Token> tokenList, ISourceFile sourceFile) : this(tokenList.GetEnumerator(), sourceFile) { }
		public ListAsLexer(IEnumerator<Token> tokenList, ISourceFile sourceFile) { _e = tokenList; _sourceFile = sourceFile; }

		IEnumerator<Token> _e;
		ISourceFile _sourceFile;
		public Loyc.Syntax.ISourceFile SourceFile
		{
			get { return _sourceFile; }
		}

		public Token? NextToken()
		{
			if (_e.MoveNext())
				return _e.Current;
			else
				return null;
		}

		public Loyc.Utilities.IMessageSink ErrorSink { get; set; }
		public int IndentLevel { get { return 0; } } // TODO
		public int LineNumber { get { return 0; } } // TODO

		public bool MoveNext()
		{
			return _e.MoveNext();
		}
		public Token Current
		{
			get { return _e.Current; }
		}
		object System.Collections.IEnumerator.Current
		{
			get { return _e.Current; }
		}
		void IDisposable.Dispose() { _e.Dispose(); }
		void IEnumerator.Reset() { _e.Reset(); }
	}
}
