using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Loyc.Syntax.Lexing;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Loyc.VisualStudio
{
	/// <summary>Boilerplate factory class for <see cref="LesSyntaxForVS"/> that 
	/// associates it with content type "LES", and associates file extensions such 
	/// as .les with content type "LES".</summary>
	[Export(typeof(IClassifierProvider))]
	[ContentType("LES")]
	internal class LesSyntaxForVSProvider : IClassifierProvider
	{
		[Export]
		[Name("LES")] // Must match the [ContentType] attributes
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

		/// <summary>This "registry" lets us get IClassificationType objects which 
		/// represent token types (string, comment, etc.) in Visual Studio.</summary>
		[Import]
		internal IClassificationTypeRegistryService ClassificationRegistry = null; // Set via MEF

		public IClassifier GetClassifier(ITextBuffer buffer)
		{
			return Get(buffer, ClassificationRegistry);
		}
		public static LesSyntaxForVS Get(ITextBuffer buffer, IClassificationTypeRegistryService registry)
		{
			return buffer.Properties.GetOrCreateSingletonProperty<LesSyntaxForVS>(delegate { return new LesSyntaxForVS(buffer, registry); });
		}
	}

	/// <summary>Syntax colorizer for LES, based on LesLexer.</summary>
	internal class LesSyntaxForVS : SyntaxClassifierForVS
	{
		static IClassificationType _superExprTargetType;
		static IClassificationType _callTargetType;
		static IClassificationType _preSufOpType;

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
			: base(buffer, registry)
		{
			_superExprTargetType = registry.GetClassificationType(PredefinedClassificationTypeNames.Keyword);
			_callTargetType = registry.GetClassificationType("LoycCallTarget");
			_preSufOpType = registry.GetClassificationType("LesPreSufOp");
		}

		protected override ILexer PrepareLexer(ILexer lexer, ICharSource file, int position)
		{
			if (lexer == null)
				return new LesLexer(file, "?", MessageSink.Trace, position);
			((LesLexer)lexer).Reset(file, "?", position);
			return lexer;
		}

		protected override bool IsSpecialIdentifier(object value)
		{
			return CommonKeywords.Contains(value) ||
				(value is Symbol) && ((Symbol)value).Name.StartsWith("#");
		}
	}

	#region Old version
	
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
	class OldLesSyntaxForVS : IClassifier
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

		internal OldLesSyntaxForVS(ITextBuffer buffer, IClassificationTypeRegistryService registry)
		{
			_buffer = buffer;
			_buffer.Changed += TextBufferChanged;

			_superExprTargetType = registry.GetClassificationType(PredefinedClassificationTypeNames.Keyword);
			_callTargetType = registry.GetClassificationType("LoycCallTarget");
			//_whiteSpaceType = registry.GetClassificationType(PredefinedClassificationTypeNames.WhiteSpace);
			_numberType = registry.GetClassificationType(PredefinedClassificationTypeNames.Number);
			_commentType = registry.GetClassificationType(PredefinedClassificationTypeNames.Comment);
			_identifierType = registry.GetClassificationType(PredefinedClassificationTypeNames.Identifier);
			_stringType = registry.GetClassificationType(PredefinedClassificationTypeNames.String);
			_operatorType = registry.GetClassificationType(PredefinedClassificationTypeNames.Operator);
			#if VS2010
			// In VS2010 (not sure about 2012) "Operator" is fixed at teal and 
			// cannot be changed by the user. "script operator" can be changed though.
			_operatorType = registry.GetClassificationType("script operator");
			#endif
			_literalType = registry.GetClassificationType("LoycOtherLiteral");
			_preSufOpType = registry.GetClassificationType("LesPreSufOp");
			_separatorType = _operatorType;
			_parenType = registry.GetClassificationType("LoycBracket");
			_specialNameType = registry.GetClassificationType("LoycSpecialName");
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

		// One entry per line. nonzero means the line starts in the middle of a token.
		DList<MidToken> _midTokenAtLineStart = new DList<MidToken>();
		enum MidToken : byte {
			None = 0,               // means "line does not start mid-token"
			Unknown = 1,            // means "previous line(s) have not been lexed"
			TDQString = (byte)'"',  // """..."""
			TSQString = (byte)'\'', // '''...'''
			Comment = (byte)'/'     // /*...*/. Higher values mean "nested comment"
		}

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
					_midTokenAtLineStart.InsertRange(line, (ICollection<MidToken>)Range.Repeat(MidToken.Unknown, lcd));
					_midTokenAtLineStart[0] = 0; // in case we just inserted the first line
				}

				if (ClassificationChanged != null)
				{
					// Refresh classification of all lines that might be affected
					int fromLine = e.After.GetLineNumberFromPosition(change.NewPosition);
					int toLine = e.After.GetLineNumberFromPosition(change.NewEnd);
					while (_midTokenAtLineStart[toLine + 1, MidToken.None] != MidToken.None)
						toLine++;

					var spanToUpdate = new SnapshotSpan(
						e.After.GetLineFromLineNumber(fromLine).Start,
						e.After.GetLineFromLineNumber(toLine).End);
					ClassificationChanged(this, new ClassificationChangedEventArgs(spanToUpdate));
				}
			}
		}

		/// <summary>Lexes the line(s) that overlap the specified span and returns 
		/// the results that overlap the specified span.</summary>
		/// <param name="trackingSpan">The span currently being classified</param>
		/// <returns>A list of ClassificationSpans that represent spans identified to be of this classification</returns>
		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
		{
			int fromLine, toLine;
			{
				// Choose a range of lines fromLine..toLine to tokenize.
				var fromSSLine = span.Start.GetContainingLine();
				fromLine = fromSSLine.LineNumber; // zero-based
				while (_midTokenAtLineStart[fromLine, MidToken.None] == MidToken.Unknown)
					fromLine--;

				var toSSLine = span.End.GetContainingLine();
				toLine = toSSLine.LineNumber; // zero-based
				// The caller often includes a newline in 'span'. In that case,
				// decrement toLine to avoid lexing the entire line that follows.
				if (span.End.Position <= toSSLine.Start.Position)
					toLine--;
			}

			// Get the range of indexes start..end to tokenize.
			ITextSnapshot ss = span.Snapshot;
			int start = ss.GetLineFromLineNumber(fromLine).Start.Position;
			int end = ss.GetLineFromLineNumber(toLine).EndIncludingLineBreak.Position;

			// Prepare lexer object and a list to hold the results
			_lexer = _lexer ?? new LesLexer("", MessageSink.Null);
			var sourceFile = new TextSnapshotAsSourceFile(ss);
			_lexer.Reset(sourceFile, "", start);
			List<ClassificationSpan> tags = new List<ClassificationSpan>();

			// First, deal with the token in which we're located, if any...
			MidToken mid = _midTokenAtLineStart[fromLine, MidToken.None];
			bool classificationChanged;
			int curLine = ScanMidToken(_lexer, fromLine, mid, out classificationChanged);
			if (mid != MidToken.None)
			{
				Token token = new Token(ToTokenType(mid), start, _lexer.InputPosition - start);
				tags.Add(MakeClassificationSpan(ss, token, classificationChanged));
			}

			// Classify other tokens until we reach 'end'
			bool forceLexAnotherLine = false;
			while (_lexer.InputPosition < end || forceLexAnotherLine)
			{
				int prevLine = curLine;
				int tokenStart = _lexer.InputPosition;
				int lexerLineNumber = _lexer.LineNumber;
				classificationChanged = false;

				Token? token_ = _lexer.NextToken();
				if (token_ == null) break;
				Token token = token_.Value;

				if (_lexer.LineNumber > lexerLineNumber)
				{
					curLine += _lexer.LineNumber - lexerLineNumber;
					forceLexAnotherLine = false;
					_midTokenAtLineStart.MaybeEnlarge(curLine + 1);

					if (token.Type() == TokenType.Newline)
					{
						if (_midTokenAtLineStart[curLine] != MidToken.None)
						{
							_midTokenAtLineStart[curLine] = MidToken.None;
							// Consider if there is a many-line comment and the user deletes
							// the "/*". We need to force the lines afterward to be reparsed.
							forceLexAnotherLine = true;
						}
					}
					else
					{
						char firstChar = ss[tokenStart];
						Debug.Assert(firstChar == '"' || firstChar == '\'' || firstChar == '/');
						mid = (MidToken)firstChar;
						// Now go back to the beginning of the token and update 
						// _midTokenAtLineStart for all lines that start within the token
						int midIndex = tokenStart + (firstChar == '/' ? 2 : 3);
						var lexer = new LesLexer(sourceFile, "", MessageSink.Null, midIndex);
						ScanMidToken(lexer, prevLine, mid, out classificationChanged);
					}
				}

				var cspan = MakeClassificationSpan(ss, token, classificationChanged);
				if (cspan != null)
					tags.Add(cspan);
			}

			return tags;
		}

		ClassificationSpan MakeClassificationSpan(ITextSnapshot ss, Token token, bool fireClassificationChanged)
		{
			var @class = TokenTypeToClassification(token.Type());
			if (@class != null)
			{
				if (token.Type() == TokenType.Id && (token.Value.ToString().StartsWith("#") || CommonKeywords.Contains(token.Value)))
					@class = _specialNameType;
				var tspan = new SnapshotSpan(ss, token.StartIndex, token.Length);
				
				if (fireClassificationChanged && ClassificationChanged != null)
					ClassificationChanged(this, new ClassificationChangedEventArgs(tspan));
				
				return new ClassificationSpan(tspan, @class);
			}
			return null;
		}

		static int ToTokenType(MidToken mid)
		{
			if (mid == MidToken.TDQString || mid == MidToken.TSQString)
				return (int)TokenType.String;
			else
				return (int)TokenType.MLComment;
		}

		private int ScanMidToken(LesLexer lexer, int fromLine, MidToken mid, out bool classificationChangedInLaterLines)
		{
			classificationChangedInLaterLines = false;
			if (mid == MidToken.None)
				return fromLine;
			int curLine;
			for (curLine = fromLine; ; curLine++)
			{
				if (curLine > fromLine && _midTokenAtLineStart[curLine] != mid) {
					_midTokenAtLineStart[curLine] = mid;
					classificationChangedInLaterLines = true;
				}
				if (mid == MidToken.TDQString)
				{
					if (lexer.TDQStringLine())
						break;
				}
				else if (mid == MidToken.TSQString)
				{
					if (lexer.TSQStringLine())
						break;
				}
				else if (mid >= MidToken.Comment)
				{
					int nested = mid - MidToken.Comment;
					if (lexer.MLCommentLine(ref nested))
						break;
					else
						mid = (MidToken)((int)MidToken.Comment + nested);
				}
			}
			return curLine;
		}

		// The "Editor Classifier" project template contains several comments that are 
		// extremely confusing. For example, the following comment seems to be saying that 
		// "typing /*" is a "non-text change". WTF is that supposed to mean? The
		// documentation of IClassifier itself is only slightly better.
		//
		// "This event gets raised if a non-text change would affect the classification in some way,
		// for example typing /* would cause the classification to change in C# without directly
		// affecting the span."
		public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
	}

	#endregion

	#region Classification types & color definitions

	/// <summary>Defines the colorization for prefix and suffic operators.</summary>
	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "LesPreSufOp")]
	[Name("LesPreSufOp")] // I don't know what this does
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

		// I don't know what this is for but the Ook! sample had them
		[Export(typeof(ClassificationTypeDefinition))]
		[Name("LesPreSufOp")]
		[BaseDefinition(PredefinedClassificationTypeNames.Operator)]
		internal static ClassificationTypeDefinition _ = null;
	}

	/// <summary>Defines the colorization for called methods like "Foo" in <c>Foo()</c>.</summary>
	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "LoycCallTarget")]
	[Name("LoycCallTarget")] // I don't know what this does
	[UserVisible(false)] // When true, shows this type in the Fonts & Colors page of the VS Options
	[Order(Before = Priority.Default)] // Supposedly "sets the priority to be after the default classifiers", even though the word "Before" is used
	internal sealed class CallTargetDef : ClassificationFormatDefinition
	{
		public CallTargetDef()
		{
			this.DisplayName = "EC#/LES - method call target"; //human readable version of the name
			this.ForegroundColor = Color.FromRgb(80, 40, 0);
		}

		// I don't know what this is for but the Ook! sample had them
		[Export(typeof(ClassificationTypeDefinition))]
		[Name("LoycCallTarget")]
		[BaseDefinition(PredefinedClassificationTypeNames.Identifier)]
		internal static ClassificationTypeDefinition _ = null;
	}

	#endregion
}
