using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Threading;
using System.ComponentModel.Composition;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc.Syntax.Lexing;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Loyc.VisualStudio
{
	using System;
	using System.Windows.Media;

	/// <summary>A compact token representation used by <see cref="SyntaxClassifierForVS"/>.</summary>
	[DebuggerDisplay("Type = {Type}, Length = {Length}, Value = {Value}")]
	public struct EditorToken
	{
		public EditorToken(int type, int length, object value)
			{ TypeAndLength = (type & 0x3FFF) | (Math.Min(length, 0x3FFFF) << 14); Value = value; }
		public int Type { get { return TypeAndLength & 0x3FFF; } }
		public int Length { get { return (int)((uint)TypeAndLength >> 14); } }
		public Token ToToken(int start) { return new Token(Type, start, Length, NodeStyle.Default, Value); }

		public object Value;
		// 14 bits for token type (enough to handle TokenKind), 18 for length
		int TypeAndLength;
	}

	/// <summary>
	/// Base class for syntax highlighters based on lexers that produce tokens 
	/// represented by <see cref="Loyc.Syntax.Lexing.Token"/> structures.
	/// </summary>
	/// <remarks>
	/// A derived class is in charge of which lexer to use. By default, the 
	/// <see cref="TokenKind"/> (bits 8-12) of the <see cref="Token.TypeInt"/>
	/// are used to choose Visual Studio classifications.
	/// <para/>
	/// Visual Studio creates one classifier instance per text file (actually, 
	/// a "provider" class such as <see cref="LesSyntaxForVSProvider"/> creates it),
	/// so this class contains state related to the "current" source file.
	/// </remarks>
	public abstract class SyntaxClassifierForVS : IClassifier
	{
		protected ITextBuffer _buffer;
		public ITextBuffer Buffer { get { return _buffer; } }
		protected TextSnapshotAsSourceFile _wrappedBuffer;

		protected SparseAList<EditorToken> _tokens = new SparseAList<EditorToken>();
		protected SparseAList<EditorToken> _nestedTokens = new SparseAList<EditorToken>();
		ILexer _lexer;
		protected int _lookahead = 3;
		protected IClassificationTypeRegistryService _registry;
		public IClassificationTypeRegistryService ClassificationTypeRegistry { get { return _registry; } }

		protected SyntaxClassifierForVS(ITextBuffer buffer, IClassificationTypeRegistryService registry)
		{
			_buffer = buffer;
			_buffer.Changed += OnTextBufferChanged;
			_wrappedBuffer = new TextSnapshotAsSourceFile(buffer.CurrentSnapshot);
			_registry = registry;
			InitClassificationTypes();
		}

		public SparseAList<EditorToken> GetSnapshotOfTokens()
		{
			EnsureLexed(_buffer.CurrentSnapshot, _buffer.CurrentSnapshot.Length);
			return _tokens.Clone();
		}

		protected abstract ILexer PrepareLexer(ILexer oldLexer, ICharSource file, int position);

		protected internal void EnsureLexed(ITextSnapshot snapshot, int stopAt)
		{
			_wrappedBuffer.TextSnapshot = snapshot;

			int lastKnownTokenEnd = _tokens.Count;
			if (lastKnownTokenEnd < stopAt)
			{
				_lexer = PrepareLexer(_lexer, _wrappedBuffer, lastKnownTokenEnd);
				RunLexerUntil(stopAt);
			}
		}

		protected int RunLexerUntil(int stopAt)
		{
			if (_lexer.InputPosition < stopAt)
			{
				int startAt = _lexer.InputPosition;
				_tokens.ClearSpace(startAt, stopAt - startAt);
				_nestedTokens.ClearSpace(startAt, stopAt - startAt);
				for (Token? t_; _lexer.InputPosition < stopAt && (t_ = _lexer.NextToken()) != null; )
				{
					Token t = t_.Value;
					if (t.EndIndex > stopAt)
					{
						_tokens.ClearSpace(t.StartIndex, t.Length);
						_nestedTokens.ClearSpace(t.StartIndex, t.Length);
					}
					if (t.Children != null)
						foreach (var ct in t.Children)
							_nestedTokens[ct.StartIndex] = new EditorToken(ct.TypeInt, ct.Length, ct.Value);
					else if (!IsWhitespace(t.TypeInt))
						_tokens[t.StartIndex] = new EditorToken(t.TypeInt, t.Length, t.Value);
				}
			}
			return _lexer.InputPosition;
		}

		#region Implementation of IClassifier

		public virtual IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
		{
			EnsureLexed(span.Snapshot, span.End.Position);

			List<ClassificationSpan> tags = new List<ClassificationSpan>();
			int? position = span.Start.Position + 1;
			var eToken = _tokens.NextLowerItem(ref position);
			do
			{
				var tag = MakeClassificationSpan(span.Snapshot, eToken.ToToken(position ?? 0));
				if (tag != null)
					tags.Add(tag);
				eToken = _tokens.NextHigherItem(ref position);
			} while (position != null && position < span.End.Position);
			return tags;
		}

		ClassificationSpan MakeClassificationSpan(ITextSnapshot ss, Token token)
		{
			var @class = TokenToVSClassification(token);
			if (@class != null)
			{
				var tspan = new SnapshotSpan(ss, token.StartIndex, token.Length);
				return new ClassificationSpan(tspan, @class);
			}
			return null;
		}

		public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

		#endregion

		protected virtual void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
		{
			INormalizedTextChangeCollection changes = e.Changes; // ordered by position
			int totalDelta = 0;
			foreach (var change in changes)
			{
				int position = change.OldPosition, delta = change.Delta;
				Debug.Assert(change.Delta == change.NewLength - change.OldLength);
				Debug.Assert(position + totalDelta == change.NewPosition);
				TextBufferChanged(position, change.NewLength, delta);
				totalDelta += delta;
			}
		}
		void TextBufferChanged(int position, int newLength, int dif)
		{
			int startLex = Math.Max(position - _lookahead, 0);
			EnsureLexed(_buffer.CurrentSnapshot, startLex);

			if (dif > 0)
				_tokens.InsertSpace(position, dif);
			else
				_tokens.RemoveRange(position, -dif);

			int endLex = position + newLength + _lookahead;
			endLex = _tokens.NextHigherIndex(endLex - 1) ?? _tokens.Count;
			startLex = _tokens.NextLowerIndex(startLex + 1) ?? 0;

			_lexer = PrepareLexer(_lexer, _wrappedBuffer, startLex);
			int stoppedAt = RunLexerUntil(endLex);

			if (ClassificationChanged != null)
			{
				var span = new SnapshotSpan(_buffer.CurrentSnapshot, new Span(startLex, stoppedAt - startLex));
				ClassificationChanged(this, new ClassificationChangedEventArgs(span));
			}
		}

		#region Default classification based on TokenKind
		// Override these methods for different classifications
		
		protected static IClassificationType _typeKeywordType;
		protected static IClassificationType _attributeKeywordType;
		protected static IClassificationType _numberType;
		protected static IClassificationType _commentType;
		protected static IClassificationType _identifierType;
		protected static IClassificationType _stringType;
		protected static IClassificationType _operatorType;
		protected static IClassificationType _keywordType;
		protected static IClassificationType _literalType;
		protected static IClassificationType _bracketType;
		protected static IClassificationType _specialNameType;

		private void InitClassificationTypes()
		{
			_typeKeywordType = _registry.GetClassificationType("LoycTypeKeyword");
			_attributeKeywordType = _registry.GetClassificationType("LoycAttributeKeyword");
			_keywordType = _registry.GetClassificationType(PredefinedClassificationTypeNames.Keyword);
			_numberType = _registry.GetClassificationType(PredefinedClassificationTypeNames.Number);
			_commentType = _registry.GetClassificationType(PredefinedClassificationTypeNames.Comment);
			_identifierType = _registry.GetClassificationType(PredefinedClassificationTypeNames.Identifier);
			_stringType = _registry.GetClassificationType(PredefinedClassificationTypeNames.String);
			_operatorType = _registry.GetClassificationType(PredefinedClassificationTypeNames.Operator);
			#if VS2010
			// In VS2010 (not sure about 2012) "Operator" is fixed at teal and 
			// cannot be changed by the user. "script operator" can be changed though.
			_operatorType = _registry.GetClassificationType("script operator");
			#endif
			_bracketType = _registry.GetClassificationType("LoycBracket");
			_literalType = _registry.GetClassificationType("LoycOtherLiteral");
			_specialNameType = _registry.GetClassificationType("LoycSpecialName");
		}

		protected virtual IClassificationType TokenToVSClassification(Token t)
		{
			switch (t.Kind)
			{
				case TokenKind.Comment: return _commentType;
				case TokenKind.Id: return IsSpecialIdentifier(t.Value) ? _specialNameType : _identifierType;
				case TokenKind.Number: return _numberType;
				case TokenKind.String: return _stringType;
				case TokenKind.OtherLit: return _literalType;
				case TokenKind.Dot:
				case TokenKind.Assignment:
				case TokenKind.Operator: return _operatorType;
				case TokenKind.Separator: return _operatorType;
				case TokenKind.AttrKeyword: return _attributeKeywordType;
				case TokenKind.TypeKeyword: return _typeKeywordType;
				case TokenKind.OtherKeyword: return _keywordType;
				case TokenKind.LParen:
				case TokenKind.RParen:
				case TokenKind.LBrack:
				case TokenKind.RBrack:
				case TokenKind.LBrace:
				case TokenKind.RBrace: return _bracketType;
			}
			return null;
		}

		protected virtual bool IsSpecialIdentifier(object value)
		{
			return false;
		}

		protected virtual bool IsWhitespace(int tt)
		{
			return ((TokenKind)tt & TokenKind.KindMask) == TokenKind.Spaces;
		}

		#endregion
	}

	#region Classification types & color definitions

	/// <summary>
	/// Defines the colorization for #hash_words, and words that are often used 
	/// as keywords in other languages.
	/// </remarks>
	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "LoycAttributeKeyword")]
	[Name("LoycAttributeKeyword")]
	[UserVisible(true)] // When true, shows this type in the Fonts & Colors page of the VS Options
	[Order(Before = Priority.Default)]
	internal sealed class AttributeKeywordsDef : ClassificationFormatDefinition
	{
		public AttributeKeywordsDef()
		{
			this.DisplayName = "EC# - attribute keywords (public, virtual, etc.)";
			this.ForegroundColor = Color.FromRgb(0, 0, 255);
			this.IsBold = false;
		}

		// I don't know what this is for but the Ook! sample had them
		[Export(typeof(ClassificationTypeDefinition))]
		[Name("LoycAttributeKeyword")]
		[BaseDefinition(PredefinedClassificationTypeNames.Keyword)]
		internal static ClassificationTypeDefinition _ = null;
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "LoycTypeKeyword")]
	[Name("LoycTypeKeyword")]
	[UserVisible(true)] // When true, shows this type in the Fonts & Colors page of the VS Options
	[Order(Before = Priority.Default)]
	internal sealed class BuiltinTypeNameDef : ClassificationFormatDefinition
	{
		public BuiltinTypeNameDef()
		{
			this.DisplayName = "EC# - type keywords (int, string, etc.)";
			this.ForegroundColor = Color.FromRgb(0, 128, 255);
			this.IsBold = true;
		}

		// I don't know what this is for but the Ook! sample had them
		[Export(typeof(ClassificationTypeDefinition))]
		[Name("LoycTypeKeyword")]
		[BaseDefinition(PredefinedClassificationTypeNames.Keyword)]
		internal static ClassificationTypeDefinition _ = null;
	}

	/// <summary>
	/// Defines the colorization for #hash_words, and (in LES) words that are often 
	/// used as keywords in other languages.
	/// </remarks>
	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "LoycSpecialName")]
	[Name("LoycSpecialName")]
	[UserVisible(true)] // When true, shows this type in the Fonts & Colors page of the VS Options
	[Order(Before = Priority.Default)]
	internal sealed class SpecialNameDef : ClassificationFormatDefinition
	{
		public SpecialNameDef()
		{
			this.DisplayName = "EC# special names / LES common keywords";
			this.ForegroundColor = Color.FromRgb(0, 0, 96);
			this.IsBold = true;
		}

		// I don't know what this is for but the Ook! sample had them
		[Export(typeof(ClassificationTypeDefinition))]
		[Name("LoycSpecialName")]
		[BaseDefinition(PredefinedClassificationTypeNames.Keyword)]
		internal static ClassificationTypeDefinition _ = null;
	}

	/// <summary>Defines the colorization for parens & brackets & braces <c>([{}])</c>.</summary>
	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "LoycBracket")]
	[Name("LoycBracket")]
	[UserVisible(true)] // When true, shows this type in the Fonts & Colors page of the VS Options
	[Order(Before = Priority.Default)]
	internal sealed class BracketDef : ClassificationFormatDefinition
	{
		public BracketDef()
		{
			this.DisplayName = "EC#/LES - paren/bracket/brace ({[]})";
			this.ForegroundColor = Color.FromRgb(40, 80, 120);
			//this.BackgroundColor = Color.FromRgb(224, 240, 255);
			this.IsBold = true;
		}

		// I don't know what this is for but the Ook! sample had them
		[Export(typeof(ClassificationTypeDefinition))]
		[Name("LoycBracket")]
		[BaseDefinition(PredefinedClassificationTypeNames.Operator)]
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
	[ClassificationType(ClassificationTypeNames = "LoycOtherLiteral")]
	[Name("LoycOtherLiteral")]
	[UserVisible(true)] // When true, shows this type in the Fonts & Colors page of the VS Options
	[Order(Before = Priority.Default)]
	internal sealed class OtherLiteralDef : ClassificationFormatDefinition
	{
		public OtherLiteralDef()
		{
			this.DisplayName = "EC#/LES - other literals (symbols, booleans)";
			this.ForegroundColor = Color.FromRgb(96, 0, 192);
			this.IsBold = true;
		}

		// I don't know what this is for but the Ook! sample had them
		[Export(typeof(ClassificationTypeDefinition))]
		[Name("LoycOtherLiteral")]
		[BaseDefinition(PredefinedClassificationTypeNames.Literal)]
		internal static ClassificationTypeDefinition _ = null;
	}

	#endregion
}
