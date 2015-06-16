using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Ecs.Parser;
using Loyc.Collections;
using Loyc.Syntax.Lexing;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Loyc.VisualStudio
{
	/// <summary>Boilerplate factory class that associates <see cref="EcsSyntaxForVS"/>,
	/// and file extensions such as .ecs, with content type "Enhanced C#".</summary>
	[Export(typeof(IClassifierProvider))]
	[ContentType("Enhanced C#")]
	internal class EcsSyntaxForVSProvider : IClassifierProvider
	{
		[Export]
		[Name("Enhanced C#")] // Must match the [ContentType] attributes
		[BaseDefinition("code")]
		internal static ContentTypeDefinition _ = null;
		[Export]
		[FileExtension(".ecs")]
		[ContentType("Enhanced C#")]
		internal static FileExtensionToContentTypeDefinition _1 = null;
		[Export]
		[FileExtension(".lllpg")]
		[ContentType("Enhanced C#")]
		internal static FileExtensionToContentTypeDefinition _2 = null;

		[Import] VSImports _vs = null; // Set via MEF

		public static EcsSyntaxForVS Get(VSImports vs, ITextBuffer buffer)
		{
			return buffer.Properties.GetOrCreateSingletonProperty<EcsSyntaxForVS>(
				delegate { return new EcsSyntaxForVS(new VSBuffer(vs, buffer)); });
		}
		public IClassifier GetClassifier(ITextBuffer buffer)
		{
			return Get(_vs, buffer);
		}
	}

	internal class EcsSyntaxForVS : SyntaxClassifierForVS
	{
		internal EcsSyntaxForVS(VSBuffer ctx) : base(ctx)
		{
			_preprocessorType = ctx.VS.ClassificationRegistry.GetClassificationType("Preprocessor Keyword");
		}

		#region SyntaxClassifierForVS overrides (lexical analysis)

		protected override ILexer<Token> PrepareLexer(ILexer<Token> lexer, ICharSource file, int position)
		{
			if (lexer == null)
				return new EcsLexer(file, "?", MessageSink.Trace, position);
			((EcsLexer)lexer).Reset(file, "?", position);
			return lexer;
		}

		protected override bool IsSpecialIdentifier(object value)
		{
			return (value is Symbol) && ((Symbol)value).Name.StartsWith("#");
		}

		protected static IClassificationType _preprocessorType;

		protected override IClassificationType TokenToVSClassification(Token t)
		{
			var c = base.TokenToVSClassification(t);
			if (c == null && t.Kind == TokenKind.Other && t.Type() != TokenType.Unknown)
				return _preprocessorType;
			return c;
		}

		#endregion
	}
}
