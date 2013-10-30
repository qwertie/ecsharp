using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Language.StandardClassification;
using System.Diagnostics;
using Loyc.Syntax.Les;
using Loyc.Collections;
using System.Collections;
using Loyc.Syntax;
using Loyc;
using Loyc.Math;
using Loyc.Syntax.Lexing;

namespace VS.LesSyntax
{
	#region Classification types & color definitions
	// - Special: if foo bar();
	// - Method call: if(foo, bar());
	// - idea to define keywords:
	//   [#trivia_keywords("else", "catch", "finally")] { ... };

	/// <summary>Defines the colorization for called methods like "Foo" in <c>Foo()</c>.</summary>
	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "CallTarget")]
	[Name("CallTarget")] // I don't know what this does
	[UserVisible(true)] // When true, shows this type in the Fonts & Colors page of the VS Options
	[Order(Before = Priority.Default)] // Supposedly "sets the priority to be after the default classifiers", even though the word "Before" is used
	internal sealed class CallTargetDef : ClassificationFormatDefinition
	{
		public CallTargetDef()
		{
			this.DisplayName = "LES - method call target"; //human readable version of the name
			this.ForegroundColor = Color.FromRgb(100, 50, 0);
			this.IsBold = true;
			this.TextDecorations = System.Windows.TextDecorations.Underline;
		}

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("CallTarget")]
		internal static ClassificationTypeDefinition _ = null;
	}

	/// <summary>Defines the colorization for called methods like "Foo" in <c>Foo()</c>.</summary>
	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "PreSufOp")]
	[Name("PreSufOp")] // I don't know what this does
	[UserVisible(true)] // When true, shows this type in the Fonts & Colors page of the VS Options
	[Order(Before = Priority.Default)]
	internal sealed class PreSufOpDef : ClassificationFormatDefinition
	{
		public PreSufOpDef()
		{
			this.DisplayName = "LES - prefix/suffix operator";
			this.ForegroundColor = Color.FromRgb(50, 100, 0);
			this.IsBold = true;
		}

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("PreSufOp")]
		internal static ClassificationTypeDefinition _ = null;
	}

	/// <summary>Defines the colorization for parens & brackets & braces <c>([{}])</c>.</summary>
	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "Bracket")]
	[Name("Bracket")]
	[UserVisible(true)] // When true, shows this type in the Fonts & Colors page of the VS Options
	[Order(Before = Priority.Default)]
	internal sealed class BracketDef : ClassificationFormatDefinition
	{
		public BracketDef()
		{
			this.DisplayName = "LES - paren/bracket/brace ({[]})";
			this.ForegroundColor = Color.FromRgb(40, 80, 120);
			//this.BackgroundColor = Color.FromRgb(224, 240, 255);
			this.IsBold = true;
		}

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("Bracket")]
		internal static ClassificationTypeDefinition _ = null;
	}

	/// <summary>Defines the colorization for literals other than numbers and strings.
	/// There is already a predefined classification called "literal", but it shows up
	/// as plain text and it is not listed on the Visual Studio colors list.</summary>
	/// <remarks>Originaly I named it "Literal", but doing so caused this strange error
	/// message from the "Color Theme Service" in VS's ActivityLog.xml:
	/// <pre>
	/// The color &apos;Popup&apos; in category &apos;de7b1121-99a4-4708-aedf-15f40c9b332f&apos; does not exist.
	/// </pre>
	/// And the Literal classification didn't work (was not colored).
	/// </remarks>
	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "OtherLiteral")]
	[Name("OtherLiteral")]
	[UserVisible(true)] // When true, shows this type in the Fonts & Colors page of the VS Options
	[Order(Before = Priority.Default)]
	internal sealed class OtherLiteralDef : ClassificationFormatDefinition
	{
		public OtherLiteralDef()
		{
			this.DisplayName = "LES - other literals (@@foo, @true)";
			this.ForegroundColor = Color.FromRgb(64, 0, 128);
			this.IsBold = true;
		}

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("OtherLiteral")]
		internal static ClassificationTypeDefinition _ = null;
	}

	/// <summary>
	/// Defines the colorization for #hash_words, and words that are often used 
	/// as keywords in other languages.
	/// </remarks>
	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "SpecialName")]
	[Name("SpecialName")]
	[UserVisible(true)] // When true, shows this type in the Fonts & Colors page of the VS Options
	[Order(Before = Priority.Default)]
	internal sealed class SpecialNameDef : ClassificationFormatDefinition
	{
		public SpecialNameDef()
		{
			this.DisplayName = "LES - special names & common keywords";
			this.ForegroundColor = Color.FromRgb(0, 0, 96);
			this.IsBold = true;
		}

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("SpecialName")]
		internal static ClassificationTypeDefinition _ = null;
	}

	#endregion

	/// <summary>
	/// Boilerplate class that seems to act as a factory for <see cref="LesSyntaxForVS"/> objects.
	/// </summary>
	[Export(typeof(IClassifierProvider))]
	[ContentType("LES")]
	internal class LesSyntaxForVSProvider : IClassifierProvider
	{
		[Export]
		[Name("LES")]
		[BaseDefinition("code")]
		internal static ContentTypeDefinition _ = null;

		[Export]
		[FileExtension(".les")]
		[ContentType("LES")]
		internal static FileExtensionToContentTypeDefinition _1 = null;
		[Export]
		[FileExtension(".lemp")]
		[ContentType("LES")]
		internal static FileExtensionToContentTypeDefinition _2 = null;
		[Export]
		[FileExtension(".lel")]
		[ContentType("LES")]
		internal static FileExtensionToContentTypeDefinition _3 = null;

		/// <summary>
		/// Import the classification registry to be used for getting a reference
		/// to the custom classification type later.
		/// </summary>
		[Import]
		internal IClassificationTypeRegistryService ClassificationRegistry = null; // Set via MEF

		public IClassifier GetClassifier(ITextBuffer buffer)
		{
			return buffer.Properties.GetOrCreateSingletonProperty<LesSyntaxForVS>(delegate { return new LesSyntaxForVS(buffer, ClassificationRegistry); });
		}
	}

	/// <summary>
	/// This class is in charge of syntax highlighting. It classifies spans of LES 
	/// text and reports these classifications to the caller (Visual Studio), which
	/// selects the colors and draws the code based on "classification formats" like
	/// number, comment, and the CallTarget classification defined above.
	/// </summary>
	/// <remarks>
	/// Visual Studio creates one classifier instance per text file (actually, 
	/// <see cref="LesSyntaxForVSProvider"/> creates it), so this class contains 
	/// state related to the "current" source file.
	/// </remarks>
	class LesSyntaxForVS : IClassifier
	{
		static IClassificationType _superExprTargetType;
		static IClassificationType _callTargetType;
		//static IClassificationType _whiteSpaceType;
		static IClassificationType _numberType;
		static IClassificationType _commentType;
		static IClassificationType _identifierType;
		static IClassificationType _stringType;
		static IClassificationType _operatorType;
		static IClassificationType _literalType, _preSufOpType, _separatorType, _parenType;
		static IClassificationType _specialNameType;
		ITextBuffer _buffer;
		LesLexer _lexer;

		HashSet<Symbol> CommonKeywords = new HashSet<Symbol>(new[] {
			// C# keywords
			"abstract",  "event",     "new",        "struct", 
			"as",        "explicit",  "null",       "switch", 
			"base",      "extern",    "object",     "this", 
			"bool",      "false",     "operator",   "throw", 
			"break",     "finally",   "out",        "true", 
			"byte",      "fixed",     "override",   "try", 
			"case",      "float",     "params",     "typeof", 
			"catch",     "for",       "private",    "uint", 
			"char",      "foreach",   "protected",  "ulong", 
			"checked",   "goto",      "public",     "unchecked", 
			"class",     "if",        "readonly",   "unsafe", 
			"const",     "implicit",  "ref",        "ushort", 
			"continue",  "in",        "return",     "using", 
			"decimal",   "int",       "sbyte",      "virtual", 
			"default",   "interface", "sealed",     "volatile", 
			"delegate",  "internal",  "short",      "void", 
			"do",        "is",        "sizeof",     "while", 
			"double",    "lock",      "stackalloc",
			"else",      "long",      "static",
			"enum",      "namespace", "string",
			// C#/LES contextual keywords (add/remove excluded because they are 
			// rarely used and, after all, are not real keywords. LINQ keywords
			// are not likely to be used in LES so they're excluded too.)
			"partial", "var", "global", "get", "set", "alias",
			"select", "from", "where", "orderby", "let",
			// C keywords not listed above
			"auto", "unsigned", "register", "typedef", "union",
			// C++ keywords not listed above
			"asm", "typename", "template", "typeid", "friend", 
			"const_cast", "dynamic_cast", "static_cast", "reinterpret_cast",
			"inline", "nullptr", "constexpr", "delete",
			"mutable", "wchar_t",
			// alternate operator names in C++11
			"and", "bitand", "compl", "not_eq", "or_eq", "xor_eq", "and_eq",
			"bitor", "not", "or", "xor",
			// Java keywords not in C#
			"boolean", "extends", "final", "implements", "import", "instanceof", 
			"native", "package", "strictfp", "super", "synchronized", "throws", 
			"transient",
			// python keywords not in C#
			"and", "assert", "def", "del", "elif", "except", "exec", 
			"global", "from", "import", "lambda", "not", "or", "pass",
			"raise", "with",
			// other
			"let"
		}.Select(GSymbol.Get));

		internal LesSyntaxForVS(ITextBuffer buffer, IClassificationTypeRegistryService registry)
		{
			_buffer = buffer;
			_buffer.Changed += TextBufferChanged;

			_superExprTargetType = registry.GetClassificationType(PredefinedClassificationTypeNames.Keyword);
			_callTargetType = registry.GetClassificationType("CallTarget");
			//_whiteSpaceType = registry.GetClassificationType(PredefinedClassificationTypeNames.WhiteSpace);
			_numberType = registry.GetClassificationType(PredefinedClassificationTypeNames.Number);
			_commentType = registry.GetClassificationType(PredefinedClassificationTypeNames.Comment);
			_identifierType = registry.GetClassificationType(PredefinedClassificationTypeNames.Identifier);
			_stringType = registry.GetClassificationType(PredefinedClassificationTypeNames.String);
			_operatorType = registry.GetClassificationType(PredefinedClassificationTypeNames.Operator);
			_literalType = registry.GetClassificationType("OtherLiteral");
			_preSufOpType = registry.GetClassificationType("PreSufOp");
			_separatorType = registry.GetClassificationType("Separator") ?? _operatorType;
			_parenType = registry.GetClassificationType("Bracket");
			_specialNameType = registry.GetClassificationType("SpecialName");
		}
		IClassificationType TokenTypeToClassification(TokenType tt)
		{
			switch (tt)
			{
				//case TokenType.Spaces: return _whitespaceType;
				case TokenType.SLComment: return _commentType;
				case TokenType.MLComment: return _commentType;
				case TokenType.Shebang: return _commentType;
				case TokenType.Id: return _identifierType;
				case TokenType.Number : return _numberType;
				case TokenType.String : return _stringType;
				case TokenType.SQString: return _stringType;
				case TokenType.Symbol: return _literalType;
				case TokenType.OtherLit: return _literalType;
				case TokenType.Dot: return _operatorType;
				case TokenType.Assignment: return _operatorType;
				case TokenType.NormalOp: return _operatorType;
				case TokenType.PreSufOp: return _preSufOpType;
				case TokenType.SuffixOp : return _preSufOpType;
				case TokenType.PrefixOp: return _preSufOpType;
				case TokenType.Colon: return _separatorType;
				case TokenType.At: return _operatorType;
				case TokenType.Not: return _operatorType;
				case TokenType.BQString: return _operatorType;
				case TokenType.Comma: return _separatorType;
				case TokenType.Semicolon: return _separatorType;
				case TokenType.LParen: return _parenType;
				case TokenType.RParen: return _parenType;
				case TokenType.LBrack: return _parenType;
				case TokenType.RBrack: return _parenType;
				case TokenType.LBrace: return _parenType;
				case TokenType.RBrace: return _parenType;
			}
			return null;
		}

		// Might be helpful: "Inside the Editor"
		// http://msdn.microsoft.com/en-us/library/vstudio/dd885240.aspx
		// and: http://www.alashiban.com/multi-editing-tutorial/

		// One entry per line. nonzero means the line starts in the middle of a 
		// token; different numbers indicate different tokens.
		DList<byte> _midTokenAtLineStart = new DList<byte>();

		private void TextBufferChanged(object sender, TextContentChangedEventArgs e)
		{
			// This code assumes the changes are non-overlapping and sorted by 
			// position. Without these assumptions, I can't figure out how to
			// keep _midTokenAtLineStart properly synchronized with the text.
			foreach (var change in e.Changes) {
				// Keep _midTokenAtLineStart in-sync with the document
				int line = e.After.GetLineNumberFromPosition(change.NewPosition);
				int lcd = change.LineCountDelta;

				if (lcd < 0) _midTokenAtLineStart.RemoveRange(line, -lcd);
				if (lcd > 0)
				{
					_midTokenAtLineStart.InsertRange(line, (ICollection<byte>)Range.Repeat((byte)1, lcd));
					_midTokenAtLineStart[0] = 0; // in case we just inserted the first line
				}
				
				if (ClassificationChanged != null)
				{
					// Refresh classification of all lines that might be affected
					byte prev = 0;
					int fromLine = e.After.GetLineNumberFromPosition(change.NewPosition);
					if ((prev = _midTokenAtLineStart[fromLine, (byte)0]) != 0)
						do fromLine--;
						while (prev == _midTokenAtLineStart[fromLine, (byte)0]);
					
					int toLine = e.After.GetLineNumberFromPosition(change.NewEnd);
					if ((prev = _midTokenAtLineStart[toLine + 1, (byte)0]) != 0)
						do toLine++;
						while (prev == _midTokenAtLineStart[toLine + 1, (byte)0]);

					var spanToUpdate = new SnapshotSpan(
						e.After.GetLineFromLineNumber(fromLine).Start,
						e.After.GetLineFromLineNumber(toLine).End);
					ClassificationChanged(this, new ClassificationChangedEventArgs(spanToUpdate));
				}
			}
		}

		byte _nextTokenId = 1;

		/// <summary>Lexes the line(s) that overlap the specified span, and any
		/// lines before or after that share a token with those lines (e.g. 
		/// multi-line comments)</summary>
		/// <param name="trackingSpan">The span currently being classified</param>
		/// <returns>A list of ClassificationSpans that represent spans identified to be of this classification</returns>
		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
		{
			var fromSSLine = span.Start.GetContainingLine();
			int fromLine = fromSSLine.LineNumber, prev;
			if ((prev = _midTokenAtLineStart[fromLine, (byte)0]) != 0)
				do fromLine--;
				while (prev == _midTokenAtLineStart[fromLine, (byte)0]);

			var toSSLine = span.End.GetContainingLine();
			int toLine = toSSLine.LineNumber;

			ITextSnapshot ss = span.Snapshot;
			int start = ss.GetLineFromLineNumber(fromLine).Start.Position;
			int end = ss.GetLineFromLineNumber(toLine).End.Position;

			// create a list to hold the results
			List<ClassificationSpan> tags = new List<ClassificationSpan>();

			// And now let's lex the chosen set of lines: fromLine..toLine
			int prevLine = fromLine;
			_lexer = _lexer ?? new LesLexer("", (pos, errMsg) => { });
			_lexer.Reset(new TextSnapshotAsSourceFile(ss, ""), start);
			while (_lexer.InputPosition < end)
			{
				Token? token_ = _lexer.NextToken();
				if (token_ == null) break;
				Token token = token_.Value;

				int lineIndex = fromLine + _lexer.LineNumber - 1;
				if (lineIndex > prevLine) {
					if (token.Type() == TokenType.Newline) {
						_midTokenAtLineStart.MaybeEnlarge(lineIndex + 1);
						_midTokenAtLineStart[lineIndex] = 0;
					} else {
						_midTokenAtLineStart.MaybeEnlarge(lineIndex + 1);
						for (int i = prevLine + 1; i <= lineIndex; i++)
							_midTokenAtLineStart[i] = _nextTokenId;
						if (++_nextTokenId == 0)
							++_nextTokenId;
					}
					prevLine = lineIndex;
				}

				var @class = TokenTypeToClassification(token.Type());
				if (@class != null)
				{
					var tspan = new SnapshotSpan(ss, token.StartIndex, token.Length);
					if (tspan.OverlapsWith(span))
					{
						if (token.Type() == TokenType.Id && (token.Value.ToString().StartsWith("#") || CommonKeywords.Contains(token.Value)))
							@class = _specialNameType;
						tags.Add(new ClassificationSpan(tspan, @class));
					}
				}
			}

			return tags;
		}

		#pragma warning disable 67
		// The "Editor Classifier" project template contains several comments that are 
		// extremely confusing. For example, the following comment seems to be saying that 
		// "typing /*" is a "non-text change". WTF is this comment supposed to mean? The
		// documentation of IClassifier itself is only slightly better.
		//
		// "This event gets raised if a non-text change would affect the classification in some way,
		// for example typing /* would cause the classification to change in C# without directly
		// affecting the span."
		public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
		#pragma warning restore 67
	}

	class TextSnapshotAsSourceFile : ISourceFile
	{
		ITextSnapshot _ss;
		string _fileName;
		public TextSnapshotAsSourceFile(ITextSnapshot ss, string filename) { _ss = ss; _fileName = filename; }

		public string FileName
		{
			get { return _fileName; }
		}
		public Loyc.UString Substring(int startIndex, int length)
		{
			return SubstringCore(startIndex, length);
		}
		public string SubstringCore(int startIndex, int length)
		{
			int c = Count;
			if (startIndex >= c) return "";
			if (length > c - startIndex)
				length = c - startIndex;
			return _ss.GetText(startIndex, length);
		}
		public IRange<char> Slice(int start, int count = 2147483647)
		{
			string s = SubstringCore(start, count);
			return new StringSlice(s, 0, s.Length);
		}
		public char TryGet(int index, ref bool fail)
		{
			if ((uint)index < (uint)Count)
				return _ss[index];
			fail = true;
			return '\0';
		}
		public char this[int index]
		{
			get { return _ss[index]; }
		}
		public int Count
		{
			get { return _ss.Length; }
		}
		public IEnumerator<char> GetEnumerator()
		{
			for (int i = 0; i < Count; i++)
				yield return this[i];
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		public SourcePos IndexToLine(int index)
		{
			var line = _ss.GetLineFromPosition(index);
			return new SourcePos(FileName, line.LineNumber + 1, index - line.Start.Position + 1);
		}
		public int LineToIndex(SourcePos pos)
		{
			if (pos.Line <= 0)
				return -1;
			if (pos.Line >= _ss.LineCount)
				return _ss.Length;
			var line = _ss.GetLineFromLineNumber(pos.Line);
			return line.Start.Position + MathEx.InRange(pos.PosInLine - 1, 0, line.Length + 1);
		}
		public int LineToIndex(int lineNo)
		{
			if (lineNo <= 0)
				return -1;
			if (lineNo >= _ss.LineCount)
				return _ss.Length;
			var line = _ss.GetLineFromLineNumber(lineNo);
			return line.Start.Position;
		}
	}
}
