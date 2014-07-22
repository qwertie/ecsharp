using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Loyc.Syntax.Lexing;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.VisualStudio
{
	/// <summary>Boilerplate factory class for <see cref="LesSyntaxForVS"/> that 
	/// associates it with content type "LES", and associates file extensions such 
	/// as .les with content type "LES".</summary>
	[Export(typeof(IClassifierProvider))]
	[Export(typeof(ITaggerProvider))]
	[TagType(typeof(ClassificationTag))]
	[TagType(typeof(ErrorTag))]
	[ContentType("LES")]
	internal class LesSyntaxForVSProvider : ITaggerProvider, IClassifierProvider
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

		[Import] VSImports _vs;

		public static LesSyntaxForVS Get(VSImports vs, ITextBuffer buffer)
		{
			return buffer.Properties.GetOrCreateSingletonProperty<LesSyntaxForVS>(() => new LesSyntaxForVS(new VSBuffer(vs, buffer)));
		}
		public IClassifier GetClassifier(ITextBuffer buffer)
		{
			return Get(_vs, buffer);
		}
		public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
		{
			return Get(_vs, buffer) as ITagger<T>;
		}
	}

	internal class LesSyntaxForVS : SyntaxAnalyzerForVS<IListSource<ITagSpan<ITag>>>
	{
		public LesSyntaxForVS(VSBuffer ctx) : base(ctx)
		{
			var registry = ctx.VS.ClassificationRegistry;
			_preSufOpType = registry.GetClassificationType("LesPreSufOp");
			_keywordTag = MakeTag(PredefinedClassificationTypeNames.Keyword);
			_callTargetTag = MakeTag("LoycCallTarget");
		}

		#region SyntaxClassifierForVS overrides (lexical analysis)

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

		protected override ILexer PrepareLexer(ILexer lexer, ICharSource file, int position)
		{
			if (lexer == null)
				return new LesLexer(file, "?", _lexerMessageSink, position);
			((LesLexer)lexer).Reset(file, "?", position);
			return lexer;
		}

		protected override bool IsSpecialIdentifier(object value)
		{
			return CommonKeywords.Contains(value) ||
				(value is Symbol) && ((Symbol)value).Name.StartsWith("#");
		}

		#endregion

		#region SyntaxAnalyzerForVS overrides (parsing on a background thread)

		ClassificationTag MakeTag(string name)
		{
			var type = _ctx.VS.ClassificationRegistry.GetClassificationType(name);
			return new ClassificationTag(type);
		}

		static ClassificationTag _keywordTag;
		static ClassificationTag _callTargetTag;

		class MyLesParser : LesParser
		{
			private TextSnapshotAsSourceFile _file;
			private IAdd<ITagSpan<ClassificationTag>> _results;
			public MyLesParser(IListSource<Token> tokens, TextSnapshotAsSourceFile file, 
			                   IMessageSink messageSink, IAdd<ITagSpan<ClassificationTag>> results)
				: base(tokens, file, messageSink)
			{
				_file = file;
				_results = results;
			}

			protected override void MarkSpecial(LNode primary)
			{
				base.MarkSpecial(primary);
				var range = primary.Range;
				AddTag(range.StartIndex, range.Length, _keywordTag);
			}
			protected override LNode ParseCall(Token target, Token paren, int endIndex)
			{
				AddTag(target.StartIndex, target.Length, _callTargetTag);
				return base.ParseCall(target, paren, endIndex);
			}
			protected override LNode ParseCall(LNode target, Token paren, int endIndex)
			{
				AddCallTag(target);
				return base.ParseCall(target, paren, endIndex);
			}
			void AddCallTag(LNode target)
			{
				if (!target.IsCall)
				{
					var range = target.Range;
					AddTag(range.StartIndex, range.Length, _callTargetTag);
				}
				else
				{
					if (target.CallsMin(S.Dot, 1))
						AddCallTag(target.Args.Last);
					else if (target.CallsMin(S.Of, 1))
						AddCallTag(target.Args.Last);
				}
			}

			void AddTag(int startIndex, int length, ClassificationTag tag)
			{
				var sspan = new SnapshotSpan(_file.TextSnapshot, new Span(startIndex, length));
				_results.Add(new TagSpan<ClassificationTag>(sspan, tag));
			}
		}

		public override IListSource<ITagSpan<ITag>> RunAnalysis(ITextSnapshot snapshot, 
			SparseAList<EditorToken> eTokens, CancellationToken cancellationToken)
		{
			var sourceFile = new TextSnapshotAsSourceFile(snapshot);
			var tokens = ToNormalTokens(eTokens);
			var tokensAsLexer = new TokenListAsLexer(tokens, sourceFile);
			var tokensTree = new TokensToTree(new TokenListAsLexer(tokens, sourceFile), true) 
			                                 { ErrorSink = MessageSink.Trace };
			var results = new DList<ITagSpan<ClassificationTag>>();
			var parser = new MyLesParser(tokensTree.Buffered(), sourceFile, MessageSink.Trace, results);
			parser.ParseStmtsGreedy();
			results.Sort((t1, t2) => t1.Span.Start.Position.CompareTo(t2.Span.Start.Position));
			return results;
		}

		#endregion
	}


	#region Classification types & color definitions

	/// <summary>Defines the colorization for prefix and suffix operators.</summary>
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
			this.DisplayName = "LES - method call target"; //human readable version of the name
			this.ForegroundColor = Color.FromRgb(40, 20, 0);
			this.BackgroundColor = Color.FromRgb(255, 224, 192);
		}

		// I don't know what this is for but the Ook! sample had them
		[Export(typeof(ClassificationTypeDefinition))]
		[Name("LoycCallTarget")]
		[BaseDefinition(PredefinedClassificationTypeNames.Identifier)]
		internal static ClassificationTypeDefinition _ = null;
	}

	#endregion
}
