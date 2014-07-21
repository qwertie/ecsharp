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
	/// <summary>Boilerplate factory class for <see cref="EcsSyntaxForVS"/> that 
	/// associates it with content type "Enhanced C#", and associates file 
	/// extensions such as .ecs with content type "Enhanced C#".</summary>
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

		/// <summary>This "registry" lets us get IClassificationType objects which 
		/// represent token types (string, comment, etc.) in Visual Studio.</summary>
		[Import]
		internal IClassificationTypeRegistryService ClassificationRegistry = null; // Set via MEF

		public IClassifier GetClassifier(ITextBuffer buffer)
		{
			return buffer.Properties.GetOrCreateSingletonProperty<EcsSyntaxForVS>(delegate { return new EcsSyntaxForVS(buffer, ClassificationRegistry); });
		}
	}

	internal class EcsSyntaxForVS : SyntaxClassifierForVS
	{
		internal EcsSyntaxForVS(ITextBuffer buffer, IClassificationTypeRegistryService registry) : base(buffer, registry) { }

		protected override ILexer PrepareLexer(ILexer lexer, ICharSource file, int position)
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
	}
}
