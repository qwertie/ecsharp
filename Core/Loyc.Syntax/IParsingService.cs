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
	/// <summary>An interface that encapsulates the lexer and parser of a 
	/// programming language, or a non-programming language that can be 
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

		/// <summary>Returns true if the <see cref="Tokenize"/> method is available.</summary>
		bool HasTokenizer { get; }

		/// <summary>Returns true if the parser supports preserving comments.</summary>
		bool CanPreserveComments { get; }

		/// <summary>Returns a lexer that is configured to begin reading the specified file.</summary>
		/// <param name="text">Text to be tokenized (e.g. <see cref="UString"/>)</param>
		/// <param name="fileName">File name to be associated with any errors that occur.</param>
		/// <param name="msgs">Error messages are sent to this object.</param>
		/// <remarks>
		/// The returned lexer should be a "simple" tokenizer. If the language uses 
		/// tree lexing (in which tokens are grouped by parentheses and braces),
		/// the returned lexer should NOT include the grouping process.
		/// <para/>
		/// It is recommended that the implementation of this method filter out 
		/// spaces (for best performance) but not comments or newlines. 
		/// If there is a preprocessor, it should not run.
		/// </remarks>
		ILexer<Token> Tokenize(ICharSource text, string fileName, IMessageSink msgs);

		/// <summary>Parses a source file into one or more Loyc trees.</summary>
		/// <param name="text">input file or string.</param>
		/// <param name="fileName">A file name to associate with errors, warnings, and output nodes.</param>
		/// <param name="msgs">Error and warning messages are sent to this object.</param>
		/// <param name="mode">Indicates the kind of input, e.g. <c>File</c> 
		/// (an entire source file), <c>FormalArguments</c> (function parameter list), etc. 
		/// <c>null</c> is a synonym for <c>File</c>.</param>
		/// <param name="preserveComments">Whether to preserve comments and newlines 
		/// by attaching trivia attributes to the output. If the property
		/// <see cref="CanPreserveComments"/> is false, this parameter will not work.</param>
		IListSource<LNode> Parse(ICharSource text, string fileName, IMessageSink msgs = null, ParsingMode mode = null, bool preserveComments = true);

		/// <summary>If <see cref="HasTokenizer"/> is true, this method accepts a 
		/// lexer returned by Tokenize() and begins parsing.</summary>
		/// <param name="input">A source of tokens.</param>
		/// <param name="msgs">Error and warning messages are sent to this object. 
		/// If this parameter is null, messages should be sent to <see cref="MessageSink.Current"/>.</param>
		/// <param name="mode">Indicates how the input should be parsed.
		/// <c>null</c> is a synonym for <see cref="ParsingMode.File"/></param>
		/// <param name="preserveComments">Whether to preserve comments and newlines 
		/// by attaching trivia attributes to the output. If the property
		/// <see cref="CanPreserveComments"/> is false, this parameter will not work.</param>
		/// <exception cref="NotSupportedException"><see cref="HasTokenizer"/> is false.</exception>
		/// <remarks>
		/// This method adds any preprocessing steps to the lexer (tree-ification 
		/// or token preprocessing) that are required by this language before it 
		/// sends the results to the parser. If possible, the output is computed 
		/// lazily.
		/// </remarks>
		IListSource<LNode> Parse(ILexer<Token> input, IMessageSink msgs = null, ParsingMode mode = null, bool preserveComments = true);

		/// <summary>Parses a token tree, such as one that came from a token literal.</summary>
		/// <param name="tokens">List of tokens</param>
		/// <param name="file">A source file to associate with errors, warnings, and output nodes.</param>
		/// <param name="msgs">Error and warning messages are sent to this object.
		/// If this parameter is null, messages should be sent to <see cref="MessageSink.Current"/>.</param>
		/// <param name="inputType">Indicates how the input should be parsed.</param>
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
	}
	
	/// <summary>Standard extension methods for <see cref="IParsingService"/>.</summary>
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

		/// <summary>Parses a string by invoking <see cref="IParsingService.Tokenize(ICharSource, string, IMessageSink)"/> using an empty string as the file name.</summary>
		public static ILexer<Token> Tokenize(this IParsingService parser, UString input, IMessageSink msgs = null)
		{
			return parser.Tokenize(input, "", msgs ?? MessageSink.Current);
		}
		/// <summary>Parses a string by invoking <see cref="IParsingService.Parse(ICharSource, string, IMessageSink, ParsingMode, bool)"/> using an empty string as the file name.</summary>
		public static IListSource<LNode> Parse(this IParsingService parser, UString input, IMessageSink msgs = null, ParsingMode inputType = null, bool preserveComments = true)
		{
			return parser.Parse(input, "", msgs ?? MessageSink.Current, inputType, preserveComments);
		}
		/// <summary>Parses a string and expects exactly one output.</summary>
		/// <exception cref="InvalidOperationException">The output list was empty or contained multiple nodes.</exception>
		public static LNode ParseSingle(this IParsingService parser, UString expr, IMessageSink msgs = null, ParsingMode inputType = null, bool preserveComments = true)
		{
			var e = parser.Parse(expr, msgs, inputType, preserveComments);
			return Single(e);
		}
		/// <summary>Parses a string and expects exactly one output.</summary>
		/// <exception cref="InvalidOperationException">The output list was empty or contained multiple nodes.</exception>
		public static LNode ParseSingle(this IParsingService parser, ICharSource text, string fileName, IMessageSink msgs = null, ParsingMode inputType = null, bool preserveComments = true)
		{
			var e = parser.Parse(text, fileName, msgs, inputType, preserveComments);
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
		/// <summary>Parses a Stream.</summary>
		public static IListSource<LNode> Parse(this IParsingService parser, Stream stream, string fileName, ParsingMode inputType = null, IMessageSink msgs = null, bool preserveComments = true)
		{
			return parser.Parse(new StreamCharSource(stream), fileName, msgs, inputType, preserveComments);
		}
		/// <summary>Parses a Stream.</summary>
		public static ILexer<Token> Tokenize(this IParsingService parser, Stream stream, string fileName, IMessageSink msgs = null)
		{
			return parser.Tokenize(new StreamCharSource(stream), fileName, msgs);
		}
		/// <summary>Opens the specified file and parses it.</summary>
		public static IListSource<LNode> ParseFile(this IParsingService parser, string fileName, IMessageSink msgs = null, ParsingMode inputType = null, bool preserveComments = true)
		{
			using (var stream = new FileStream(fileName, FileMode.Open))
				return Parse(parser, stream, fileName, inputType ?? ParsingMode.File, msgs, preserveComments);
		}
		/// <summary>Opens the specified file and tokenizes it.</summary>
		public static ILexer<Token> TokenizeFile(this IParsingService parser, string fileName, IMessageSink msgs = null)
		{
			using (var stream = new FileStream(fileName, FileMode.Open))
				return Tokenize(parser, stream, fileName, msgs);
		}
	}
}
