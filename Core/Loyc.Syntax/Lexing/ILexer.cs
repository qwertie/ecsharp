using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Utilities;

namespace Loyc.Syntax.Lexing
{
	/// <summary>A standard interface for lexers.</summary>
	/// <typeparam name="Token">Type of tokens produced by the lexer (usually
	/// <see cref="Loyc.Syntax.Lexing.Token"/>).</typeparam>
	public interface ILexer<Token> : IIndexToLine, IEnumerator<Token>
	{
		/// <summary>The file being lexed.</summary>
		ISourceFile SourceFile { get; }
		/// <summary>Scans the next token and returns information about it.</summary>
		/// <returns>The next token, or null at the end of the source file.</returns>
		Maybe<Token> NextToken();
		/// <summary>Event handler for errors.</summary>
		IMessageSink ErrorSink { get; set; }
		/// <summary>Indentation level of the current line. This is updated after 
		/// scanning the first whitespaces on a new line, and may be reset to zero 
		/// when <see cref="NextToken()"/> returns a newline.</summary>
		int IndentLevel { get; }
		/// <summary>Gets a string slice that holds the spaces or tabs that were 
		/// used to indent the current line.</summary>
		UString IndentString { get; }
		/// <summary>Current line number (1 for the first line).</summary>
		int LineNumber { get; }
		/// <summary>Current input position (an index into SourceFile.Text).</summary>
		int InputPosition { get; }
	}

	/// <summary>A base class for wrappers that modify lexer behavior.
	/// Implements the ILexer interface, except for the NextToken() method.</summary>
	public abstract class LexerWrapper<Token> : ILexer<Token>
	{
		public LexerWrapper(ILexer<Token> sourceLexer)
			{ Lexer = sourceLexer; }

		protected ILexer<Token> Lexer { get; set; }

		/// <summary>Returns the next (postprocessed) token. This method should set
		/// the <c>_current</c> field to the returned value.</summary>
		public abstract Maybe<Token> NextToken();

		public ISourceFile SourceFile
		{
			get { return Lexer.SourceFile; }
		}
		public virtual IMessageSink ErrorSink
		{
			get { return Lexer.ErrorSink; }
			set { Lexer.ErrorSink = value; }
		}
		public int IndentLevel
		{
			get { return Lexer.IndentLevel; }
		}
		public UString IndentString
		{
			get { return Lexer.IndentString; }
		}
		public int LineNumber
		{
			get { return Lexer.LineNumber; }
		}
		public int InputPosition
		{
			get { return Lexer.InputPosition; }
		}
		public string FileName
		{
			get { return Lexer.FileName; }
		}
		public SourcePos IndexToLine(int index)
		{
			return Lexer.IndexToLine(index);
		}
		public virtual void Reset()
		{
			Lexer.Reset();
		}

		protected Maybe<Token> _current;
		void IDisposable.Dispose() {}
		Token IEnumerator<Token>.Current { get { return _current.Value; } }
		object System.Collections.IEnumerator.Current { get { return _current; } }
		bool System.Collections.IEnumerator.MoveNext()
		{
			_current = NextToken();
			return _current.HasValue;
		}

		protected void WriteError(int index, string msg, params object[] args)
		{
			LogMessage lm = new LogMessage(Severity.Error, SourceFile.IndexToLine(index), msg, args);
			if (ErrorSink == null)
				throw new LogException(lm);
			else
				lm.WriteTo(ErrorSink);
		}
	}
}
