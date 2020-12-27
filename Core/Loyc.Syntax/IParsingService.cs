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
	/// <seealso cref="ILNodePrinter"/>
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
		/// spaces (for best performance) but not comments or newlines. If there is a 
		/// preprocessor, it should not run. If <see cref="ParsingOptions.PreserveComments"/>
		/// is false, it is not this method's responsibility to filter them out.
		/// </remarks>
		ILexer<Token> Tokenize(ICharSource text, string fileName, IMessageSink msgs, IParsingOptions options);

		/// <summary>Parses a source file into one or more Loyc trees.</summary>
		/// <param name="text">input file or string.</param>
		/// <param name="fileName">A file name to associate with errors, warnings, and output nodes.</param>
		/// <param name="msgs">Error and warning messages are sent to this object.
		/// If this parameter is null, messages should be sent to <see cref="MessageSink.Default"/>.</param>
		/// <param name="options">Parsing options.</param>
		IListSource<LNode> Parse(ICharSource text, string fileName, IMessageSink msgs, IParsingOptions options);

		/// <summary>If <see cref="HasTokenizer"/> is true, this method accepts a 
		/// lexer returned by Tokenize() and begins parsing.</summary>
		/// <param name="input">A source of tokens.</param>
		/// <param name="msgs">Error and warning messages are sent to this object. 
		/// If this parameter is null, messages should be sent to <see cref="MessageSink.Default"/>.</param>
		/// <exception cref="NotSupportedException"><see cref="HasTokenizer"/> is false.</exception>
		/// <remarks>
		/// This method adds any preprocessing steps to the lexer (tree-ification 
		/// or token preprocessing) that are required by this language before it 
		/// sends the results to the parser. If possible, the output is computed 
		/// lazily.
		/// </remarks>
		IListSource<LNode> Parse(ILexer<Token> input, IMessageSink msgs, IParsingOptions options);

		/// <summary>Parses a token tree, such as one that came from a token literal.</summary>
		/// <param name="tokens">List of tokens</param>
		/// <param name="file">A source file to associate with errors, warnings, and output nodes.</param>
		/// <param name="msgs">Error and warning messages are sent to this object.
		/// If this parameter is null, messages should be sent to <see cref="MessageSink.Default"/>.</param>
		/// <param name="options">Indicates how the input should be parsed. </param>
		/// <remarks>
		/// Some languages may offer token literals, which are stored as token trees
		/// that can be processed by "macros" or compiler plugins. A macro may wish 
		/// to parse some of the token literal using the host language's parser 
		/// (e.g. LLLPG needs to do this), so this method is provided for that 
		/// purpose.
		/// </remarks>
		/// <exception cref="NotSupportedException">This feature is not supported 
		/// by this parsing service.</exception>
		IListSource<LNode> Parse(IListSource<Token> tokens, ISourceFile file, IMessageSink msgs, IParsingOptions options);
	}

	/// <summary>Standard extension methods for <see cref="IParsingService"/>.</summary>
	public static class ParsingService
	{
		static ThreadLocalVariable<IParsingService> _default = new ThreadLocalVariable<IParsingService>();
		/// <summary>Gets or sets the default language service on this thread. If 
		/// no service has been assigned on this thread, returns <see cref="Les2LanguageService.Value"/>.</summary>
		public static IParsingService Default
		{
			get { return _default.Value ?? LesLanguageService.Value; }
			set { _default.Value = value; }
		}
		[Obsolete("This property was renamed to 'Default'")]
		public static IParsingService Current
		{
			get { return Default; }
			set { Default = value; }
		}

		public static SavedValue<IParsingService> SetDefault(IParsingService newValue)
		{
			return new SavedValue<IParsingService>(_default, newValue);
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
			int oldCount = _registeredLanguages.Count;
			foreach (var fileExt_ in fileExtensions ?? service.FileExtensions) {
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
		[Obsolete("Use 'SetDefault' instead")]
		public static PushedCurrent PushCurrent(IParsingService newValue) { return new PushedCurrent(newValue); }

		/// <summary>Returned by <see cref="PushCurrent(IParsingService)"/>.</summary>
		public struct PushedCurrent : IDisposable
		{
			public readonly IParsingService OldValue;
			public PushedCurrent(IParsingService @new) { OldValue = Default; Default = @new; }
			public void Dispose() { Default = OldValue; }
		}

		#endregion

		private static ParsingOptions _fileWithComments = new ParsingOptions();
		private static ParsingOptions QuickOptions(ParsingMode mode = null, bool preserveComments = true)
		{
			if (preserveComments && (mode == null || mode == ParsingMode.File))
				return _fileWithComments;
			return new ParsingOptions { Mode = mode ?? ParsingMode.File, PreserveComments = preserveComments };
		}

		/// <summary>Parses a string by invoking <see cref="IParsingService.Tokenize(ICharSource, string, IMessageSink, IParsingOptions)"/> using an empty string as the file name.</summary>
		public static ILexer<Token> Tokenize(this IParsingService parser, UString input, IMessageSink msgs = null)
		{
			return parser.Tokenize(input, "", msgs ?? MessageSink.Default, _fileWithComments);
		}
		/// <summary>Parses a string by invoking <see cref="IParsingService.Tokenize(ICharSource, string, IMessageSink, IParsingOptions)"/> using default options.</summary>
		public static ILexer<Token> Tokenize(this IParsingService parser, ICharSource text, string fileName, IMessageSink msgs = null)
		{
			return parser.Tokenize(text, fileName, msgs ?? MessageSink.Default, _fileWithComments);
		}

		/// <summary>Parses a string by invoking <see cref="IParsingService.Parse(ICharSource, string, IMessageSink, IParsingOptions)"/> using an empty string as the file name.</summary>
		public static IListSource<LNode> Parse(this IParsingService parser, UString input, IMessageSink msgs = null, ParsingMode inputType = null, bool preserveComments = true)
		{
			return parser.Parse(input, "", msgs ?? MessageSink.Default, QuickOptions(inputType, preserveComments));
		}
		/// <summary>Parses a string by invoking <see cref="IParsingService.Parse(ICharSource, string, IMessageSink, IParsingOptions)"/> using an empty string as the file name.</summary>
		public static IListSource<LNode> Parse(this IParsingService parser, UString input, IMessageSink msgs, IParsingOptions options)
		{
			return parser.Parse(input, "", msgs ?? MessageSink.Default, options ?? _fileWithComments);
		}
		public static IListSource<LNode> Parse(this IParsingService parser, ICharSource text, string fileName, IMessageSink msgs = null, ParsingMode inputType = null, bool preserveComments = true)
		{
			return parser.Parse(text, fileName, msgs ?? MessageSink.Default, QuickOptions(inputType, preserveComments));
		}
		public static IListSource<LNode> Parse(this IParsingService parser, ILexer<Token> input, IMessageSink msgs = null, ParsingMode mode = null, bool preserveComments = true)
		{
			return parser.Parse(input, msgs, QuickOptions(mode, preserveComments));
		}
		public static IListSource<LNode> Parse(this IParsingService parser, IListSource<Token> tokens, ISourceFile file, IMessageSink msgs, ParsingMode inputType = null)
		{
			return parser.Parse(tokens, file, msgs, QuickOptions(inputType));
		}

		/// <inheritdoc cref="ParseSingle(IParsingService, UString, IMessageSink, IParsingOptions)"/>
		public static LNode ParseSingle(this IParsingService parser, UString expr, IMessageSink msgs = null, ParsingMode inputType = null, bool preserveComments = true)
		{
			return ParseSingle(parser, expr, msgs, QuickOptions(inputType, preserveComments));
		}
		/// <summary>Parses a string and expects exactly one output.</summary>
		/// <exception cref="InvalidOperationException">The output list was empty or contained multiple nodes.</exception>
		public static LNode ParseSingle(this IParsingService parser, UString expr, IMessageSink msgs, IParsingOptions options)
		{
			var e = Parse(parser, expr, msgs, options);
			return Single(e);
		}
		/// <inheritdoc cref="ParseSingle(IParsingService, ICharSource, string, IMessageSink, IParsingOptions)"/>
		public static LNode ParseSingle(this IParsingService parser, ICharSource text, string fileName, IMessageSink msgs = null, ParsingMode inputType = null, bool preserveComments = true)
		{
			return ParseSingle(parser, text, fileName, msgs, QuickOptions(inputType, preserveComments));
		}
		/// <summary>Parses a string and expects exactly one output.</summary>
		/// <exception cref="InvalidOperationException">The output list was empty or contained multiple nodes.</exception>
		public static LNode ParseSingle(this IParsingService parser, ICharSource text, string fileName, IMessageSink msgs = null, IParsingOptions options = null)
		{
			var e = parser.Parse(text, fileName, msgs ?? MessageSink.Default, options ?? _fileWithComments);
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
		/// <summary>Opens the specified file, parses the entire file, and closes the file.</summary>
		public static IListSource<LNode> ParseFile(this IParsingService parser, string fileName, IMessageSink msgs = null, ParsingMode inputType = null, bool preserveComments = true)
		{
			using (var stream = new FileStream(fileName, FileMode.Open)) {
				var results = Parse(parser, stream, fileName, inputType ?? ParsingMode.File, msgs, preserveComments);
				// TODO: think about whether we should explicitly document or spec this out...
				// If we're not careful, the caller gets a "Cannot access a closed file"
				// exception. The problem is that IParsingService.Parse() may parse the 
				// file lazily, so we can't close the file (as `using` does for us) until
				// we make sure it is fully parsed. Luckily this is easy: just invoke the
				// Count property, which can only be computed by parsing the whole file.
				var _ = results.Count;
				return results;
			}
		}
		/// <summary>Opens the specified file and tokenizes it.</summary>
		public static ILexer<Token> TokenizeFile(this IParsingService parser, string fileName, IMessageSink msgs = null)
		{
			using (var stream = new FileStream(fileName, FileMode.Open))
				return Tokenize(parser, stream, fileName, msgs);
		}
	}
}
