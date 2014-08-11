using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Utilities;

namespace Loyc.Syntax.Lexing
{
	/// <summary>
	/// A standard interface for lexers.
	/// </summary>
	public interface ILexer : IEnumerator<Token>
	{
		/// <summary>The file being lexed.</summary>
		ISourceFile SourceFile { get; }
		/// <summary>Scans the next token and returns information about it.</summary>
		/// <returns>The next token, or null at the end of the source file.</returns>
		Token? NextToken();
		/// <summary>Event handler for errors.</summary>
		IMessageSink ErrorSink { get; set; }
		/// <summary>Indentation level of the current line. This is updated after 
		/// scanning the first whitespaces on a new line, and may be reset to zero 
		/// when <see cref="NextToken()"/> returns a newline.</summary>
		int IndentLevel { get; }
		/// <summary>Current line number (1 for the first line).</summary>
		int LineNumber { get; }
		/// <summary>Current input position (an index into SourceFile.Text).</summary>
		int InputPosition { get; }
	}

	/// <summary>A base class for wrappers that modify lexer behavior.
	/// Implements the ILexer interface, except for the NextToken() method.</summary>
	public abstract class LexerWrapper : ILexer
	{
		public LexerWrapper(ILexer source)
			{ _source = source; }
		
		protected ILexer _source;

		public abstract Token? NextToken();

		public ISourceFile SourceFile
		{
			get { return _source.SourceFile; }
		}
		public virtual IMessageSink ErrorSink
		{
			get { return _source.ErrorSink; }
			set { _source.ErrorSink = value; }
		}
		public int IndentLevel
		{
			get { return _source.IndentLevel; }
		}
		public int LineNumber
		{
			get { return _source.LineNumber; }
		}
		public int InputPosition
		{
			get { return _source.InputPosition; }
		}
		public virtual void Reset()
		{
			_source.Reset();
		}

		protected Token? _current;
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
			if (ErrorSink == null)
				throw new FormatException(MessageSink.FormatMessage(Severity.Error, SourceFile.IndexToLine(index), msg, args));
			else
				ErrorSink.Write(Severity.Error, SourceFile.IndexToLine(index), msg, args);
		}
	}
}
