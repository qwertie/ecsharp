using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
	/// <summary>Boilerplate factory class for <see cref="LesParserForVS"/> that 
	/// associates it with content type "LES".</summary>
	[Export(typeof(ITaggerProvider))]
	[TagType(typeof(ClassificationTag))]
	[ContentType("LES")]
	internal class LesParserForVSProvider : ITaggerProvider
	{
		[Import]
		internal IClassificationTypeRegistryService ClassificationRegistry = null; // Set via MEF

		public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
		{
			return Get(buffer, ClassificationRegistry) as ITagger<T>;
		}
		public static LesParserForVS Get(ITextBuffer buffer, IClassificationTypeRegistryService registry)
		{
			var classifier = LesSyntaxForVSProvider.Get(buffer, registry);
			return buffer.Properties.GetOrCreateSingletonProperty<LesParserForVS>(() => new LesParserForVS(buffer, classifier));
		}
	}

	internal class LesParserForVS : BackgroundTaggerForVS<ClassificationTag>
	{
		static ClassificationTag _keywordTag;
		static ClassificationTag _callTargetTag;

		public LesParserForVS(ITextBuffer buffer, LesSyntaxForVS classifier) : base(buffer, classifier)
		{
			_keywordTag = MakeTag(PredefinedClassificationTypeNames.Keyword);
			_callTargetTag = MakeTag("LoycCallTarget");
		}
		ClassificationTag MakeTag(string name)
		{
			var type = _classifier.ClassificationTypeRegistry.GetClassificationType(name);
			return new ClassificationTag(type);
		}

		class MyLesParser : LesParser
		{
			private TextSnapshotAsSourceFile _file;
			private IAdd<ITagSpan<ClassificationTag>> _results;
			public MyLesParser(IListSource<Token> tokens, TextSnapshotAsSourceFile file, IMessageSink messageSink, IAdd<ITagSpan<ClassificationTag>> results) : base(tokens, file, messageSink)
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
				if (!target.IsCall) {
					var range = target.Range;
					AddTag(range.StartIndex, range.Length, _callTargetTag);
				} else {
					if (target.CallsMin(S.Dot, 1) )
						AddCallTag(target.Args.Last);
					else if (target.CallsMin(S.Of, 1) )
						AddCallTag(target.Args.Last);
				}
			}

			void AddTag(int startIndex, int length,ClassificationTag tag)
			{
				var sspan = new SnapshotSpan(_file.TextSnapshot, new Span(startIndex, length));
				_results.Add(new TagSpan<ClassificationTag>(sspan, tag));
			}
		}

		protected override IEnumerable<ITagSpan<ClassificationTag>> Run(ITextSnapshot snapshot, SparseAList<EditorToken> eTokens, CancellationToken cancellationToken)
		{
			var sourceFile = new TextSnapshotAsSourceFile(snapshot);
			var tokens = ToNormalTokens(eTokens);
			var tokensAsLexer = new TokenListAsLexer(tokens, sourceFile);
			var tokensTree = new TokensToTree(new TokenListAsLexer(tokens, sourceFile), true) { ErrorSink = MessageSink.Trace };
			var results = new DList<ITagSpan<ClassificationTag>>();
			var parser = new MyLesParser(tokensTree.Buffered(), sourceFile, MessageSink.Trace, results);
			parser.ParseStmtsGreedy();
			return results;
		}
	}


	//internal class OokTokenTagger : ITagger<IErrorTag>
	//{
	//	public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
	//	{
	//		throw new NotImplementedException();
	//	}
	//	public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
	//}
}
