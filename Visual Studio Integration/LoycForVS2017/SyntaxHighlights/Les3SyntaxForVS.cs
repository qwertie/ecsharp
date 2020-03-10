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
	/// <summary>Boilerplate factory class for <see cref="Les3SyntaxForVS"/> that 
	/// associates it with content type "LES3", and associates file extensions such 
	/// as .les with content type "LES3".</summary>
	[Export(typeof(IClassifierProvider))]
	[Export(typeof(ITaggerProvider))]
	[TagType(typeof(ClassificationTag))]
	[TagType(typeof(ErrorTag))]
	[ContentType("LES3")]
	internal class Les3SyntaxForVSProvider : ITaggerProvider, IClassifierProvider
	{
		[Export]
		[Name("LES3")] // Must match the [ContentType] attributes
		[BaseDefinition("code")]
		internal static ContentTypeDefinition _ = null;
		[Export]
		[FileExtension(".les3")]
		[ContentType("LES3")]
		internal static FileExtensionToContentTypeDefinition _1 = null;
		[Export]
		[FileExtension(".lemp")]
		[ContentType("LES3")]
		internal static FileExtensionToContentTypeDefinition _3 = null;

		[Import] VSImports _vs = null;

		public static Les3SyntaxForVS Get(VSImports vs, ITextBuffer buffer)
		{
			return buffer.Properties.GetOrCreateSingletonProperty<Les3SyntaxForVS>(() => new Les3SyntaxForVS(new VSBuffer(vs, buffer)));
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

	internal class Les3SyntaxForVS : SyntaxAnalyzerForVS<IListSource<ITagSpan<ITag>>>
	{
		public Les3SyntaxForVS(VSBuffer ctx) : base(ctx)
		{
			var registry = ctx.VS.ClassificationRegistry;
			_preSufOpType = registry.GetClassificationType("LesPreSufOp");
			_keywordTag = MakeTag(PredefinedClassificationTypeNames.Keyword);
			_callTargetTag = MakeTag("LoycCallTarget");
		}

		#region SyntaxClassifierForVS overrides (lexical analysis)

		static IClassificationType _preSufOpType;

		// A list of words to be highlighted in dark blue
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

		protected override ILexer<Token> PrepareLexer(ILexer<Token> lexer, ICharSource file, int position)
		{
			if (lexer == null)
				return new Les3Lexer(file, "?", _lexerMessageSink, position);
			((Les3Lexer)lexer).Reset(file, "?", position);
			return lexer;
		}

		protected override bool IsSpecialIdentifier(object value)
		{
			return CommonKeywords.Contains(value) ||
				(value is Symbol) && LNode.IsSpecialName((Symbol)value);
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

		class MyLesParser : Les3Parser
		{
			private TextSnapshotAsSourceFile _file;
			private IAdd<ITagSpan<ClassificationTag>> _results;
			public MyLesParser(IList<Token> tokens, TextSnapshotAsSourceFile file, 
			                   IMessageSink messageSink, IAdd<ITagSpan<ClassificationTag>> results)
				: base(tokens, file, messageSink)
			{
				_file = file;
				_results = results;
			}

			protected override LNode MarkSpecial(LNode expr)
			{
				var range = expr.Target.Range;
				AddTag(range.StartIndex, range.Length, _keywordTag);
				return base.MarkSpecial(expr);
			}
			protected override LNode MarkCall(LNode call)
			{
				var target = call.Target;
				if (target.IsCall && target.BaseStyle == NodeStyle.Operator) {
					if (target.CallsMin(S.Of, 1))
						target = target.Args[0];
					if (target.CallsMin(S.Dot, 1))
						target = target.Args.Last;
				}
				if (!target.IsCall)
					AddTag(target.Range.StartIndex, target.Range.Length, _callTargetTag);
				return base.MarkCall(call);
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
			var results = new DList<ITagSpan<ClassificationTag>>();
			var parser = new MyLesParser(tokens, sourceFile, TraceMessageSink.Value, results);
			var endMarker = default(TokenType);
			var _ = parser.ExprList(ref endMarker, isBracedBlock: true);

			results.Sort((t1, t2) => t1.Span.Start.Position.CompareTo(t2.Span.Start.Position));
			return results;
		}

		#endregion
	}


	// Classification types & color definitions defined in the source file for LES2
}
