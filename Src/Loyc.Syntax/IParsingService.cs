using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Syntax.Lexing;
using Loyc.Collections;
using Loyc.Utilities;
using Loyc.Syntax.Les;
using Loyc.Threading;

namespace Loyc.Syntax
{
	/// <summary>An interface that encapsulates the lexer, parser, and printer
	/// of a programming language, or a non-programming language that can be 
	/// represented by Loyc trees.</summary>
	/// <remarks>
	/// The simplest way to parse code is with the extension method 
	/// <c>Parse(string, IMessageSink msgs = null, Symbol inputType = null)</c>.
	/// The simplest way to print is with <c>Print(LNode, IMessageSink)</c>
	/// <para/>
	/// The ToString() method should return a string that indicates the 
	/// programming language represented by this object, e.g. "LES 1.0 parser".
	/// </remarks>
	public interface IParsingService
	{
		/// <summary>Returns true if the Tokenize() method is available.</summary>
		bool HasTokenizer { get; }

		/// <summary>Returns a lexer that is configured to begin reading the specified file.</summary>
		/// <remarks>If the language uses tree lexing (in which tokens are grouped 
		/// by parentheses and braces), the returned lexer will be a tree lexer 
		/// that returns bracketed areas of code as a single unit.</remarks>
		ILexer Tokenize(ISourceFile file, IMessageSink msgs);

		/// <summary>Parses a source file into one or more Loyc trees.</summary>
		/// <param name="file">input file or string.</param>
		/// <param name="msgs">output sink for error and warning messages.</param>
		/// <param name="inputType">Indicates the kind of input: <c>Exprs</c> (one 
		/// or more expressions, typically seprated by commas but this is language-
		/// defined), <c>Stmts</c> (a series of statements), or <c>File</c> (an 
		/// entire source file). <c>null</c> is a synonym for <c>File</c>.</param>
		IListSource<LNode> Parse(ISourceFile file, IMessageSink msgs, Symbol inputType = null);

		/// <summary>If <see cref="HasTokenizer"/> is true, this method accepts a 
		/// lexer returned by Tokenize() and begins parsing.</summary>
		/// <exception cref="NotSupportedException">HasTokenizer is false.</exception>
		IListSource<LNode> Parse(ILexer input, IMessageSink msgs, Symbol inputType = null);

		/// <summary>If <see cref="HasTokenizer"/> is true, this method parses a
		/// list of tokens from the specified source file into one or more Loyc 
		/// trees.</summary>
		/// <exception cref="NotSupportedException">HasTokenizer is false.</exception>
		IListSource<LNode> Parse(IListSource<Token> input, ISourceFile file, IMessageSink msgs, Symbol inputType = null);

		/// <summary>Gets a printer delegate that you can use with 
		/// <see cref="LNode.Printer"/> and <see cref="LNode.PushPrinter"/>,
		/// or null if there is no corresponding printer available for the parser
		/// reresented by this object.</summary>
		LNodePrinter Printer { get; }

		/// <summary>Converts the specified syntax tree to a string.</summary>
		string Print(LNode node, IMessageSink msgs, object mode = null, string indentString = "\t", string lineSeparator = "\n");
	}
	
	/// <summary>Extension methods for <see cref="IParsingService"/>.</summary>
	public static class ParsingService
	{
		public static readonly Symbol Exprs = GSymbol.Get("Exprs");
		public static readonly Symbol Stmts = GSymbol.Get("Stmts");
		public static readonly Symbol File = GSymbol.Get("File");

		static ThreadLocalVariable<IParsingService> _current = new ThreadLocalVariable<IParsingService>();
		/// <summary>Gets or sets the active language service on this thread. If 
		/// no service has been assigned on this thread, returns <see cref="LesLanguageService.Value"/>.</summary>
		public static IParsingService Current
		{
			get { return _current.Value ?? LesLanguageService.Value; }
			set { _current.Value = value; }
		}
		/// <summary>Sets the current language service, returning a value suitable 
		/// for use in a C# using statement, which will restore the old service.</summary>
		/// <param name="newValue">new value of Current</param>
		public static PushedCurrent PushCurrent(IParsingService newValue) { return new PushedCurrent(newValue); }
		public struct PushedCurrent : IDisposable
		{
			IParsingService old;
			public PushedCurrent(IParsingService @new) { old = Current; Current = @new; }
			public void Dispose() { Current = old; }
		}

		public static string Print(this IParsingService self, LNode node)
		{
			return self.Print(node, MessageSink.Current);
		}
		public static ILexer Tokenize(this IParsingService parser, string input, IMessageSink msgs = null)
		{
			return parser.Tokenize(new StringCharSourceFile(input, ""), msgs ?? MessageSink.Current);
		}
		public static IListSource<LNode> Parse(this IParsingService parser, string expr, IMessageSink msgs = null, Symbol inputType = null)
		{
			return parser.Parse(new StringCharSourceFile(expr, ""), msgs ?? MessageSink.Current, inputType).Buffered();
		}
		public static LNode ParseSingle(this IParsingService parser, string expr, IMessageSink msgs = null, Symbol inputType = null)
		{
			var e = parser.Parse(expr, msgs, inputType);
			return Single(e);
		}
		public static LNode ParseSingle(this IParsingService parser, ISourceFile file, IMessageSink msgs = null, Symbol inputType = null)
		{
			var e = parser.Parse(file, msgs, inputType);
			return Single(e);
		}
		static LNode Single(IListSource<LNode> e)
		{
			LNode node = e.TryGet(0, null);
			if (node == null)
				throw new InvalidOperationException(Localize.From("ParseSingle: result was empty."));
			if (e.TryGet(1, null) != null) // don't call Count because e is typically Buffered()
				throw new InvalidOperationException(Localize.From("ParseSingle: multiple parse results."));
			return node;
		}
	}
}
