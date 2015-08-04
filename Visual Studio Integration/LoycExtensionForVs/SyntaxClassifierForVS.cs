using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Media;
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

	/// <summary>A compact token representation used by <see cref="SyntaxClassifierForVS"/>.</summary>
	[DebuggerDisplay("Type = {Type}, Length = {Length}, Value = {Value}")]
	public struct EditorToken
	{
		public EditorToken(int type, int length, object value)
			{ TypeAndLength = (type & 0x3FFF) | (Math.Min(length, 0x3FFFF) << 14); Value = value; }
		public int Type { get { return TypeAndLength & 0x3FFF; } }
		public int Length { get { return (int)((uint)TypeAndLength >> 14); } }
		public Token ToToken(int start, bool useOriginalValue = true) {
			object value = Value;
			if (useOriginalValue)
				while (value is SyntaxClassifierForVS.LexerMessage)
					value = ((SyntaxClassifierForVS.LexerMessage)value).OriginalValue;
			return new Token(Type, start, Length, NodeStyle.Default, value);
		}

		public object Value;
		// 14 bits for token type (enough to handle TokenKind), 18 for length
		int TypeAndLength;
	}

	/// <summary>
	/// Base class for syntax highlighters based on lexers that implement the
	/// <see cref="Loyc.Syntax.Lexing.ILexer"/> interface that produce tokens 
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
	/// <para/>
	/// Optionally, this class supports lexer errors by storing error information
	/// in the <see cref="EditorToken"/> that was produced along with the error, or, 
	/// if the error occurs at EOF, in the final token. The purpose of storing the 
	/// errors inside the tokens (not separately) is that old errors are deleted
	/// automatically as portions of the text are re-lexed.
	/// <para/>
	/// To use this functionality, the derived class's <c>PrepareLexer()</c> method 
	/// must use <c>_lexerErrorSink</c> as the message sink and must manually 
	/// provide an implementation of <see cref="ITagger<ErrorTag>"/> to Visual 
	/// Studio (<see cref="SyntaxAnalyzerForVS{R}"/> has an implementation).
	/// If multiple errors are produced for one token, they are all stored using 
	/// nested Message objects.
	/// </remarks>
	public abstract class SyntaxClassifierForVS : IClassifier
	{
		protected VSBuffer _ctx;
		protected TextSnapshotAsSourceFile _wrappedBuffer;
		protected ITextBuffer Buffer { get { return _ctx.Buffer; } }

		protected SparseAList<EditorToken> _tokens = new SparseAList<EditorToken>();
		protected SparseAList<EditorToken> _nestedTokens = new SparseAList<EditorToken>();
		ILexer<Token> _lexer;
		protected int _lookahead = 3;
		protected IMessageSink _lexerMessageSink;
		LexerMessage _lexerError; // assigned when _lexerMessageSink receives an error
		protected bool _haveLexerErrors; // Set true when a lexer error occurs
		protected IClassificationTypeRegistryService _registry;
		public IClassificationTypeRegistryService ClassificationTypeRegistry { get { return _registry; } }

		public class LexerMessage // Error/warning message stored in a token
		{
			public object OriginalValue;
			public MessageHolder.Message Msg;
		}

		protected SyntaxClassifierForVS(VSBuffer ctx)
		{
			_ctx = ctx;
			_wrappedBuffer = new TextSnapshotAsSourceFile(Buffer.CurrentSnapshot);
			Buffer.Changed += OnTextBufferChanged;
			
			_lexerMessageSink = new MessageSinkFromDelegate((severity, context, fmt, args) => {
				if (severity >= Severity.Note)
					_lexerError = new LexerMessage {
						OriginalValue = _lexerError, 
						Msg = new MessageHolder.Message(severity, context, fmt, args)
					};
			});

			InitClassificationTypes();
		}

		public SparseAList<EditorToken> GetSnapshotOfTokens()
		{
			EnsureLexed(Buffer.CurrentSnapshot, Buffer.CurrentSnapshot.Length);
			return _tokens.Clone();
		}

		protected abstract ILexer<Token> PrepareLexer(ILexer<Token> oldLexer, ICharSource file, int position);

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
				for (Maybe<Token> t_; _lexer.InputPosition < stopAt && (t_ = _lexer.NextToken()).HasValue; )
				{
					Token t = t_.Value;
					if (t.EndIndex > stopAt) {
						_tokens.ClearSpace(t.StartIndex, t.Length);
						_nestedTokens.ClearSpace(t.StartIndex, t.Length);
					}
					if (t.Children != null)
						foreach (var ct in t.Children)
							_nestedTokens[ct.StartIndex] = new EditorToken(ct.TypeInt, ct.Length, ct.Value);
					if (!IsWhitespace(t.TypeInt)) {
						var et = new EditorToken(t.TypeInt, t.Length, t.Value);
						_tokens[t.StartIndex] = StoreLexerError(et);
					}
				}
				if (_lexerError != null && _tokens.Count != 0)
					_tokens.Last = StoreLexerError(_tokens.Last);
			}
			return _lexer.InputPosition;
		}

		EditorToken StoreLexerError(EditorToken token)
		{
			if (_lexerError != null) {
				_haveLexerErrors = true;
				_lexerError.OriginalValue = token.Value;
				token.Value = _lexerError;
				_lexerError = null;
			}
			return token;
		}

		#region Implementation of IClassifier

		public virtual IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
		{
			EnsureLexed(span.Snapshot, span.End.Position);

			List<ClassificationSpan> tags = new List<ClassificationSpan>();
			int? position = span.Start.Position + 1;
			var eToken = _tokens.NextLowerItem(ref position);
			do {
				var tag = MakeClassificationSpan(span.Snapshot, eToken.ToToken(position ?? 0, false));
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
			if (changes.Count == 0) return;
			int start     = changes[0].OldPosition;
			var last      = changes[changes.Count-1];
			int newLength = last.NewEnd - start;
			int delta     = last.NewEnd - last.OldEnd;
			TextBufferChanged(start, newLength, delta);
		}

		void TextBufferChanged(int position, int newLength, int dif)
		{
			int startLex = Math.Max(position - _lookahead, 0);
			EnsureLexed(Buffer.CurrentSnapshot, startLex);

			if (dif > 0)
				_tokens.InsertSpace(position, dif);
			else if (_tokens.Count > position)
				_tokens.RemoveRange(position, Math.Min(-dif, _tokens.Count - position));

			int endLex = position + newLength + _lookahead;
			endLex = _tokens.NextHigherIndex(endLex - 1) ?? _tokens.Count;
			startLex = _tokens.NextLowerIndex(startLex + 1) ?? 0;

			_lexer = PrepareLexer(_lexer, _wrappedBuffer, startLex);
			int stoppedAt = RunLexerUntil(endLex);

			if (ClassificationChanged != null)
			{
				var span = new SnapshotSpan(Buffer.CurrentSnapshot, new Span(startLex, stoppedAt - startLex));
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
			var registry = _ctx.VS.ClassificationRegistry;
			_typeKeywordType = registry.GetClassificationType("LoycTypeKeyword");
			_attributeKeywordType = registry.GetClassificationType("LoycAttributeKeyword");
			_keywordType = registry.GetClassificationType(PredefinedClassificationTypeNames.Keyword);
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
			_bracketType = registry.GetClassificationType("LoycBracket");
			_literalType = registry.GetClassificationType("LoycOtherLiteral");
			_specialNameType = registry.GetClassificationType("LoycSpecialName");
		}

		protected virtual IClassificationType TokenToVSClassification(Token t)
		{
			switch (t.Kind)
			{
				case TokenKind.Comment: return _commentType;
				case TokenKind.Id: return IsSpecialIdentifier(t.Value) ? _specialNameType : _identifierType;
				case TokenKind.Literal: 
					if (t.Value is string)
						return _stringType;
					if (t.Value is int || t.Value is uint || t.Value is sbyte || t.Value is byte
						|| t.Value is short || t.Value is ushort || t.Value is long || t.Value is ulong
						|| t.Value is float || t.Value is double || t.Value is decimal)
						return _numberType;
					return _literalType;
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
		
		public static DList<Token> ToNormalTokens(SparseAList<EditorToken> eTokens)
		{
			var output = new DList<Token>();
			int? index = null;
			for (;;) {
				EditorToken eTok = eTokens.NextHigherItem(ref index);
				if (index == null) break;
				if (eTok.Value != WhitespaceTag.Value)
					output.Add(eTok.ToToken(index.Value));
			}
			return output;
		}
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
