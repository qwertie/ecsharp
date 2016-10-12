using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Syntax.Lexing;
using Loyc.Collections;
using Loyc.Utilities;
using Loyc.Syntax.Les;
using Loyc.Threading;
using System.IO;

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
	/// programming language represented by this object, e.g. "Enhanced C#".
	/// </remarks>
	/// <seealso cref="ParsingService"/>
	public interface IParsingService
	{
		/// <summary>Standard file extensions for this language, without leading 
		/// dots, with the first one being the most common.</summary>
		IEnumerable<string> FileExtensions { get; }

		/// <summary>Returns true if the Tokenize() method is available.</summary>
		bool HasTokenizer { get; }

		/// <summary>Returns a lexer that is configured to begin reading the specified file.</summary>
		/// <param name="text">Text to be tokenized (e.g. <see cref="UString"/>)</param>
		/// <param name="fileName">File name to be associated with any errors that occur.</param>
		/// <param name="msgs">Error messages are sent to this object.</param>
		/// <remarks>
		/// The returned lexer should be a "simple" tokenizer. If the language uses 
		/// tree lexing (in which tokens are grouped by parentheses and braces),
		/// the returned lexer should NOT include the grouping process, and it 
		/// should not remove comments, although it may skip spaces and perhaps
		/// newlines. If there is a preprocessor, it should not run.
		/// <para/>
		/// Whether comments and other whitespaces are filtered out is implementation-defined.
		/// </remarks>
		ILexer<Token> Tokenize(ICharSource text, string fileName, IMessageSink msgs);

		/// <summary>Parses a source file into one or more Loyc trees.</summary>
		/// <param name="text">input file or string.</param>
		/// <param name="fileName">A file name to associate with output nodes and errors.</param>
		/// <param name="msgs">output sink for error and warning messages.</param>
		/// <param name="inputType">Indicates the kind of input, e.g. <c>File</c> 
		/// (an entire source file), <c>FormalArguments</c> (function parameter list), etc. 
		/// <c>null</c> is a synonym for <c>File</c>.</param>
		IListSource<LNode> Parse(ICharSource text, string fileName, IMessageSink msgs, ParsingMode inputType = null);

		/// <summary>If <see cref="HasTokenizer"/> is true, this method accepts a 
		/// lexer returned by Tokenize() and begins parsing.</summary>
		/// <param name="msgs">output sink for error and warning messages.</param>
		/// <param name="inputType">Indicates how the input should be interpreted,
		/// e.g. <see cref="ParsingMode.File"/>, <see cref="ParsingMode.Expressions"/> or
		/// <see cref="ParsingMode.Statements"/>. The default input type should be
		/// File.</param>
		/// <exception cref="NotSupportedException">HasTokenizer is false.</exception>
		/// <remarks>
		/// This method adds any preprocessing steps to the lexer (tree-ification 
		/// or token preprocessing) that are required by this language before it 
		/// sends the results to the parser. If possible, the output is computed 
		/// lazily.
		/// </remarks>
		IListSource<LNode> Parse(ILexer<Token> input, IMessageSink msgs, ParsingMode inputType = null);

		/// <summary>Parses a token tree, such as one that came from a token literal.</summary>
		/// <remarks>
		/// Some languages may offer token literals, which are stored as token trees
		/// that can be processed by "macros" or compiler plugins. A macro may wish 
		/// to parse some of the token literal using the host language's parser 
		/// (e.g. LLLPG needs to do this), so this method is provided for that 
		/// purpose.
		/// </remarks>
		/// <exception cref="NotSupportedException">This feature is not supported 
		/// by this parsing service.</exception>
		IListSource<LNode> Parse(IListSource<Token> tokens, ISourceFile file, IMessageSink msgs, ParsingMode inputType);

		/// <summary>Gets a printer delegate that you can use with 
		/// <see cref="LNode.Printer"/> and <see cref="LNode.PushPrinter"/>,
		/// or null if there is no corresponding printer available for the parser
		/// reresented by this object.</summary>
		LNodePrinter Printer { get; }

		/// <summary>Converts the specified syntax tree to a string.</summary>
		/// <param name="node">A syntax tree to print.</param>
		/// <param name="msgs">If errors or warnings occur during printing, 
		/// they are sent here. If this is null, messages shall be sent to
		/// <see cref="MessageSink.Current"/>.</param>
		/// <param name="mode">Language-defined configuration. It is suggested 
		/// that the printing service should accept the members of 
		/// <see cref="ParsingMode"/> (e.g. ParsingMode.Statements) as possible 
		/// printing modes.</param>
		/// <param name="indentString">Indent character for multi-line nodes</param>
		/// <param name="lineSeparator">Newline string for multi-line nodes</param>
		string Print(LNode node, IMessageSink msgs = null, object mode = null, string indentString = "\t", string lineSeparator = "\n");
	}
	
	/// <summary>Standard parsing modes used with <see cref="IParsingService"/>.
	/// These modes should also be understood by printers (text serializers).</summary>
	public class ParsingMode : Symbol
	{
		private ParsingMode(Symbol prototype) : base(prototype) { }
		public static new readonly SymbolPool<ParsingMode> Pool 
		                     = new SymbolPool<ParsingMode>(p => new ParsingMode(p));

		/// <summary>Tells <see cref="IParsingService.Parse"/> to treat the input 
		/// as a single expression or expression list (which, in most languages, 
		/// is comma-separated).</summary>
		public static readonly ParsingMode Expressions = Pool.Get("Exprs");
		/// <summary>Tells <see cref="IParsingService.Parse"/> to treat the input
		/// as a list of statements. If the language makes a distinction between 
		/// executable and declaration contexts, this refers to the executable 
		/// context.</summary>
		public static readonly ParsingMode Statements = Pool.Get("Stmts");
		/// <summary>Tells <see cref="IParsingService.Parse"/> to treat the input
		/// as a list of statements. If the language makes a distinction between 
		/// executable and declaration contexts, this refers to the declaration
		/// context, in which types, methods, and properties are recognized.</summary>
		public static readonly ParsingMode Declarations = Pool.Get("Decls");
		/// <summary>Tells <see cref="IParsingService.Parse"/> to treat the input
		/// as a list of types (or a single type, if a list is not supported).</summary>
		public static readonly ParsingMode Types = Pool.Get("Types");
		/// <summary>Tells <see cref="IParsingService.Parse"/> to treat the input
		/// as a formal argument list (parameter names with types).</summary>
		public static readonly ParsingMode FormalArguments = Pool.Get("FormalArguments");
		/// <summary>Tells <see cref="IParsingService.Parse"/> to treat the input
		/// as a complete source file (this should be the default, i.e. null will
		/// do the same thing).</summary>
		public static readonly ParsingMode File = Pool.Get("File");
	}

	/// <summary>Extension methods for <see cref="IParsingService"/>.</summary>
	public static class ParsingService
	{
		static ThreadLocalVariable<IParsingService> _current = new ThreadLocalVariable<IParsingService>();
		/// <summary>Gets or sets the active language service on this thread. If 
		/// no service has been assigned on this thread, returns <see cref="LesLanguageService.Value"/>.</summary>
		public static IParsingService Current
		{
			get { return _current.Value ?? LesLanguageService.Value; }
			set { _current.Value = value; }
		}

		#region Management of registered languages

		// Thread safe: since this is an immutable reference type, we can replace it atomically
		static Map<string, IParsingService> _registeredLanguages = InitRegisteredLanguages();
		static Map<string, IParsingService> InitRegisteredLanguages()
		{
			_registeredLanguages = Map<string, IParsingService>.Empty;
			Register(Les2LanguageService.Value);
			Register(Les3LanguageService.Value);
			return _registeredLanguages;
		}

		/// <summary>Dictionary of registered parsing services, keyed by file extension 
		/// (without leading dots). The default dictionary contains one pair: 
		/// <c>("les", LesLanguageService.Value)</c></summary>
		public static IReadOnlyDictionary<string, IParsingService> RegisteredLanguages
		{
			get { return _registeredLanguages; }
		}
		
		/// <summary>Registers a parsing service.</summary>
		/// <param name="service">Service to register.</param>
		/// <param name="fileExtensions">File extensions affected (null to use the service's own list)</param>
		/// <returns>The number of new file extensions registered, or 0 if none.</returns>
		/// <remarks>This method does not replace existing registrations.</remarks>
		public static int Register(IParsingService service, IEnumerable<string> fileExtensions = null)
		{
			CheckParam.IsNotNull("service", service);
			fileExtensions = fileExtensions ?? service.FileExtensions;
			int oldCount = _registeredLanguages.Count;
			foreach (var fileExt_ in service.FileExtensions) {
				var fileExt = fileExt_; // make writable
				if (fileExt.StartsWith("."))
					fileExt = fileExt.Substring(1);
				_registeredLanguages = _registeredLanguages.With(fileExt, service, false);
			}
			return _registeredLanguages.Count - oldCount;
		}

		/// <summary>Unregisters a language service.</summary>
		/// <param name="service">Service to unregister</param>
		/// <param name="fileExtensions">File extensions affected (null to use the service's own list)</param>
		/// <returns>The number of file extensions unregistered, or 0 if none.</returns>
		/// <remarks>The service for a file extension is not removed unless the given service reference is equal to the registered service.</remarks>
		public static int Unregister(IParsingService service, IEnumerable<string> fileExtensions = null)
		{
			CheckParam.IsNotNull("service", service);
			fileExtensions = fileExtensions ?? service.FileExtensions;
			int oldCount = _registeredLanguages.Count;
			foreach (var fileExt in fileExtensions)
				if (_registeredLanguages.TryGetValue(fileExt, null) == service)
					_registeredLanguages = _registeredLanguages.Without(fileExt);
			return oldCount - _registeredLanguages.Count;
		}

		/// <summary>Finds the language service associated with the longest matching registered file extension.</summary>
		/// <remarks>Returns null if there is no registered language service for the filename's extension.</remarks>
		public static IParsingService GetServiceForFileName(string filename)
		{
			return RegisteredLanguages
				.Where(pair => ExtensionMatches(pair.Key, filename))
				.MaxOrDefault(pair => pair.Key.Length).Value;
		}
		static bool ExtensionMatches(string ext, string fn)
		{
			return fn.Length > ext.Length && fn[fn.Length - ext.Length - 1] == '.' && fn.EndsWith(ext, StringComparison.OrdinalIgnoreCase);
		}

		#endregion

		#region Management of "current" language

		/// <summary>Sets the current language service, returning a value suitable 
		/// for use in a C# using statement, which will restore the old service.</summary>
		/// <param name="newValue">new value of Current</param>
		/// <example><code>
		/// LNode code;
		/// using (var old = ParsingService.PushCurrent(LesLanguageService.Value))
		///     code = ParsingService.Current.ParseSingle("This `is` LES_code;");
		/// </code></example>
		public static PushedCurrent PushCurrent(IParsingService newValue) { return new PushedCurrent(newValue); }

		/// <summary>Returned by <see cref="PushCurrent(IParsingService)"/>.</summary>
		public struct PushedCurrent : IDisposable
		{
			public readonly IParsingService OldValue;
			public PushedCurrent(IParsingService @new) { OldValue = Current; Current = @new; }
			public void Dispose() { Current = OldValue; }
		}

		#endregion

		public static string Print(this IParsingService self, LNode node)
		{
			return self.Print(node, MessageSink.Current);
		}
		public static ILexer<Token> Tokenize(this IParsingService parser, UString input, IMessageSink msgs = null)
		{
			return parser.Tokenize(input, "", msgs ?? MessageSink.Current);
		}
		public static IListSource<LNode> Parse(this IParsingService parser, UString input, IMessageSink msgs = null, ParsingMode inputType = null)
		{
			return parser.Parse(input, "", msgs ?? MessageSink.Current, inputType);
		}
		public static LNode ParseSingle(this IParsingService parser, UString expr, IMessageSink msgs = null, ParsingMode inputType = null)
		{
			var e = parser.Parse(expr, msgs, inputType);
			return Single(e);
		}
		public static LNode ParseSingle(this IParsingService parser, ICharSource text, string fileName, IMessageSink msgs = null, ParsingMode inputType = null)
		{
			var e = parser.Parse(text, fileName, msgs, inputType);
			return Single(e);
		}
		static LNode Single(IListSource<LNode> e)
		{
			LNode node = e.TryGet(0, null);
			if (node == null)
				throw new InvalidOperationException(Localize.Localized("ParseSingle: result was empty."));
			if (e.TryGet(1, null) != null) // don't call Count because e is typically Buffered()
				throw new InvalidOperationException(Localize.Localized("ParseSingle: multiple parse results."));
			return node;
		}
		public static IListSource<LNode> Parse(this IParsingService parser, Stream stream, string fileName, IMessageSink msgs = null, ParsingMode inputType = null)
		{
			return parser.Parse(new StreamCharSource(stream), fileName, msgs, inputType);
		}
		public static ILexer<Token> Tokenize(this IParsingService parser, Stream stream, string fileName, IMessageSink msgs = null)
		{
			return parser.Tokenize(new StreamCharSource(stream), fileName, msgs);
		}
		public static IListSource<LNode> ParseFile(this IParsingService parser, string fileName, IMessageSink msgs = null, ParsingMode inputType = null)
		{
			using (var stream = new FileStream(fileName, FileMode.Open))
				return Parse(parser, stream, fileName, msgs, inputType ?? ParsingMode.File);
		}
		public static ILexer<Token> TokenizeFile(this IParsingService parser, string fileName, IMessageSink msgs = null)
		{
			using (var stream = new FileStream(fileName, FileMode.Open))
				return Tokenize(parser, stream, fileName, msgs);
		}
		/// <summary>Converts a sequences of LNodes to strings, adding a newline after each.</summary>
		/// <param name="printer">Printer for a single LNode.</param>
		/// <param name="mode">A language-specific way of modifying printer behavior.
		/// The printer ignores the mode object if it does not not understand it.</param>
		/// <param name="indentString">A string to print for each level of indentation, such as a tab or four spaces.</param>
		/// <param name="lineSeparator">Line separator, typically "\n" or "\r\n".</param>
		/// <returns>A string form of the nodes.</returns>
		public static string PrintMultiple(this LNodePrinter printer, IEnumerable<LNode> nodes, IMessageSink msgs = null, object mode = null, string indentString = "\t", string lineSeparator = "\n")
		{
			var sb = new StringBuilder();
			foreach (LNode node in nodes) {
				printer(node, sb, msgs ?? MessageSink.Current, mode, indentString, lineSeparator);
				sb.Append(lineSeparator);
			}
			return sb.ToString();
		}
		public static string Print(this IParsingService service, IEnumerable<LNode> nodes, IMessageSink msgs = null, object mode = null, string indentString = "\t", string lineSeparator = "\n")
		{
			return PrintMultiple(service.Printer, nodes, msgs, mode, indentString, lineSeparator);
		}
	}
}
