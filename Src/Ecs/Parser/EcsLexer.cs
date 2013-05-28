using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using Loyc;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Threading;
using Loyc.Utilities;
using Loyc.CompilerCore;

namespace Ecs.Parser
{
	using TT = TokenType;
	using Loyc.Syntax;
	using Loyc.LLParserGenerator;

	/*public class TokenType : Symbol
	{
		private TokenType(Symbol prototype) : base(prototype) { }
		public static new readonly SymbolPool<TokenType> Pool
		                     = new SymbolPool<TokenType>(p => new TokenType(p));

		// Token types
		public static readonly TokenType Spaces = Pool.Get("Spaces");
		public static readonly TokenType Newline = Pool.Get("Newline");
		public static readonly TokenType SLComment = Pool.Get("SLComment");
		public static readonly TokenType MLComment = Pool.Get("MLComment");
		public static readonly TokenType SQString = Pool.Get("SQString");
		public static readonly TokenType DQString = Pool.Get("DQString");
		public static readonly TokenType BQString = Pool.Get("BQString");
		public static readonly TokenType Comma = Pool.Get("Comma");
		public static readonly TokenType Colon = Pool.Get("Colon");
		public static readonly TokenType Semicolon = Pool.Get("Semicolon");
		public static readonly TokenType Operator = Pool.Get("Operator");
		public new static readonly TokenType Id = Pool.Get("Id");
		public static readonly TokenType Symbol = Pool.Get("Symbol");
		public static readonly TokenType LParen = Pool.Get("LParen");
		public static readonly TokenType RParen = Pool.Get("RParen");
		public static readonly TokenType LBrack = Pool.Get("LBrack");
		public static readonly TokenType RBrack = Pool.Get("RBrack");
		public static readonly TokenType LBrace = Pool.Get("LBrace");
		public static readonly TokenType RBrace = Pool.Get("RBrace");
		public static readonly TokenType Number = Pool.Get("Number");
		public static readonly TokenType AttrKeyword = Pool.Get("AttrKeyword");
		public static readonly TokenType TypeKeyword = Pool.Get("TypeKeyword");
		public static readonly TokenType Shebang = Pool.Get("Shebang");
		
		public static readonly TokenType @break     = Pool.Get("break");
		public static readonly TokenType @case      = Pool.Get("case");
		public static readonly TokenType @checked   = Pool.Get("checked");
		public static readonly TokenType @class     = Pool.Get("class");
		public static readonly TokenType @continue  = Pool.Get("continue");
		public static readonly TokenType @default   = Pool.Get("default");
		public static readonly TokenType @delegate  = Pool.Get("delegate ");
		public static readonly TokenType @do        = Pool.Get("do");
		public static readonly TokenType @enum      = Pool.Get("enum");
		public static readonly TokenType @event     = Pool.Get("event");
		public static readonly TokenType @fixed     = Pool.Get("fixed");
		public static readonly TokenType @for       = Pool.Get("for");
		public static readonly TokenType @foreach   = Pool.Get("foreach");
		public static readonly TokenType @goto      = Pool.Get("goto");
		public static readonly TokenType @if        = Pool.Get("if");
		public static readonly TokenType @interface = Pool.Get("interface");
		public static readonly TokenType @lock      = Pool.Get("lock");
		public static readonly TokenType @namespace = Pool.Get("namespace");
		public static readonly TokenType @return    = Pool.Get("return");
		public static readonly TokenType @struct    = Pool.Get("struct");
		public static readonly TokenType @switch    = Pool.Get("switch");
		public static readonly TokenType @throw     = Pool.Get("throw");
		public static readonly TokenType @try       = Pool.Get("try");
		public static readonly TokenType @unchecked = Pool.Get("unchecked");
		public static readonly TokenType @using     = Pool.Get("using");
		public static readonly TokenType @while     = Pool.Get("while");

		public static readonly TokenType @operator   = Pool.Get("operator");
		public static readonly TokenType @sizeof     = Pool.Get("sizeof");
		public static readonly TokenType @typeof     = Pool.Get("typeof");

		public static readonly TokenType @else       = Pool.Get("else");
		public static readonly TokenType @catch      = Pool.Get("catch");
		public static readonly TokenType @finally    = Pool.Get("finally");

		public static readonly TokenType @in         = Pool.Get("in");
		public static readonly TokenType @as         = Pool.Get("as");
		public static readonly TokenType @is         = Pool.Get("is");

		public static readonly TokenType @base       = Pool.Get("base");
		public static readonly TokenType @false      = Pool.Get("false");
		public static readonly TokenType @null       = Pool.Get("null");
		public static readonly TokenType @true       = Pool.Get("true");
		public static readonly TokenType @this       = Pool.Get("this");

		public static readonly TokenType @new        = Pool.Get("new");
		public static readonly TokenType @out        = Pool.Get("out");
		public static readonly TokenType @stackalloc = Pool.Get("stackalloc");

		public static readonly TokenType PPif        = Pool.Get("#if");
		public static readonly TokenType PPelse      = Pool.Get("#else");
		public static readonly TokenType PPelif      = Pool.Get("#elif");
		public static readonly TokenType PPendif     = Pool.Get("#endif");
		public static readonly TokenType PPdefine    = Pool.Get("#define");
		public static readonly TokenType PPundef     = Pool.Get("#undef");
		public static readonly TokenType PPwarning   = Pool.Get("#warning");
		public static readonly TokenType PPerror     = Pool.Get("#error");
		public static readonly TokenType PPnote      = Pool.Get("#note");
		public static readonly TokenType PPline      = Pool.Get("#line");
		public static readonly TokenType PPregion    = Pool.Get("#region");
		public static readonly TokenType PPendregion = Pool.Get("#endregion");

		public static readonly TokenType Hash = Pool.Get("#");
		public static readonly TokenType Dollar = Pool.Get("$");
		public static readonly TokenType At = Pool.Get("@"); // NOT produced for identifiers e.g. @foo
	}*/
	public enum TokenType
	{
		Spaces = ' ',
		Newline = '\n',
		SLComment = '/',
		MLComment = '*',
		SQString = '\'',
		DQString = '"',
		BQString = '`',
		Comma = ',',
		Colon = ':',
		Semicolon = ';',
		Id = 'i',
		Symbol = 'S',
		LParen = '(',
		RParen = ')',
		LBrack = '[',
		RBrack = ']',
		LBrace = '{',
		RBrace = '}',
		At = '@',
		Number = 'n',
		AttrKeyword = 'a',
		TypeKeyword = 'p',
		Shebang = 'G',
		
		@base = 'b',
		@false = '0',
		@null = 'n',
		@true = '1',
		@this = 't',
	
		@break = 192,
		@case     ,
		@checked  ,
		@class    ,
		@continue ,
		@default  ,
		@delegate ,
		@do       ,
		@enum     ,
		@event    ,
		@fixed    ,
		@for      ,
		@foreach  ,
		@goto     ,
		@if       ,
		@interface,
		@lock     ,
		@namespace,
		@return   ,
		@struct   ,
		@switch   ,
		@throw    ,
		@try      ,
		@unchecked,
		@using    ,
		@while    ,

		@operator  ,
		@sizeof    ,
		@typeof    ,

		@else      ,
		@catch     ,
		@finally   ,

		@in        ,
		@as        ,
		@is        ,

		@new       ,
		@out       ,
		@stackalloc,

		PPif   = 11,
		PPelse     ,
		PPelif     ,
		PPendif    ,
		PPdefine   ,
		PPundef    ,
		PPwarning  ,
		PPerror    ,
		PPnote     ,
		PPline     ,
		PPregion   ,
		PPendregion,

		Hash = '#',
		Backslash = '\\',

		// Operators
		Mul = '*', Div = '/', 
		Add = '+', Sub = '-',
		Mod = '%', // there is no Exp token due to ambiguity
		Inc = 'U', Dec = 'D',
		And = 'A', Or = 'O', Xor = 'X', Not = '!',
		AndBits = '&', OrBits = '|', XorBits = '^', NotBits = '~',
		Set = '=', Eq = '≈', Neq = '≠', 
		GT = '>', GE = '≥', LT = '<', LE = '≤',
		Shr = '»', Shl = '«',
		QuestionMark = '?',
		DotDot = '…', Dot = '.', NullDot = '_', NullCoalesce = '¿',
		ColonColon = '¨', QuickBind = 'q',
		PtrArrow = 'R', Forward = '→',
		Substitute = '$',
		LambdaArrow = 'L',

		AddSet = '2', SubSet = '3',
		MulSet = '4', DivSet = '5', 
		ModSet = '6', ExpSet = '7',
		ShrSet = '8', ShlSet = '9', 
		ConcatSet = 'B', XorBitsSet = 'D', 
		AndBitsSet = 'E', OrBitsSet = 'F',
		NullCoalesceSet = 'H', 
		QuickBindSet = 'Q',
		
		Indent = '\t', Dedent = '\b'
	}

	public interface ILexer
	{
		/// <summary>The file being lexed.</summary>
		ISourceFile Source { get; }
		/// <summary>Scans and returns information about the next token.</summary>
		Token? NextToken();
		/// <summary>Event handler for errors.</summary>
		Action<int, string> OnError { get; set; }
		/// <summary>Indentation level of the current line. This is updated after 
		/// scanning the first whitespaces on a new line, and may be reset to zero 
		/// when <see cref="NextToken()"/> returns a newline.</summary>
		int IndentLevel { get; }
		/// <summary>Current line number (1 for the first line).</summary>
		int LineNumber { get; }
		/// <summary>Restart lexing from beginning of <see cref="Source"/>.</summary>
		void Restart();
	}
	
	/// <summary>Lexer for EC# source code (see <see cref="ILexer"/>).</summary>
	/// <seealso cref="WhitespaceFilter"/>
	/// <seealso cref="TokensToTree"/>
	public partial class EcsLexer : BaseLexer<StringCharSourceFile>, ILexer
	{
		public EcsLexer(string text, Action<int, string> onError) : base(new StringCharSourceFile(text, "")) { OnError = onError; }
		public EcsLexer(StringCharSourceFile file, Action<int, string> onError) : base(file) { OnError = onError; }

		public bool AllowNestedComments = false;
		private bool _isFloat, _parseNeeded, _isNegative;
		// Alternate: hex numbers, verbatim strings
		// UserFlag: bin numbers, double-verbatim
		private NodeStyle _style;
		private int _numberBase, _verbatims;
		private Symbol _typeSuffix;
		private TokenType _type; // predicted type of the current token
		private object _value;
		private int _startPosition;
		// _allowPPAt is used to detect whether a preprocessor directive is allowed
		// at the current input position. When _allowPPAt==_startPosition, it's allowed.
		private int _allowPPAt, _lineStartAt;

		ISourceFile ILexer.Source { get { return CharSource; } }
		public StringCharSourceFile Source { get { return CharSource; } }
		public Action<int, string> OnError { get; set; }

		int _indentLevel, _lineNumber;
		public int IndentLevel { get { return _indentLevel; } }
		public int LineNumber { get { return _lineNumber; } }
		public int SpacesPerTab = 4;

		protected override void Error(string message)
		{
			Error(InputPosition, message);
		}
		protected void Error(int index, string message)
		{
			if (OnError != null)
				OnError(index, message);
			else
				throw new FormatException(message);
		}

		
		public void Restart()
		{
			_indentLevel = 0;
			_lineNumber = 0;
			_allowPPAt = _lineStartAt = 0;
		}

		internal static readonly HashSet<Symbol> CsKeywords = EcsNodePrinter.CsKeywords;
		internal static readonly HashSet<Symbol> PunctuationIdentifiers = EcsNodePrinter.PunctuationIdentifiers;
		internal static readonly HashSet<Symbol> PreprocessorIdentifiers = EcsNodePrinter.SymbolSet(
			"if", "else", "elif", "endif", "define", "undef", "line", 
			"region", "endregion", "warning", "error", "note");

		// This is the set of keywords that act only as attributes on statements.
		// This list does not include "new" and "out", which are only allowed as 
		// attributes on variable declarations and other specific statements.
		static readonly HashSet<Symbol> AttrKeywords = EcsNodePrinter.SymbolSet(
			"abstract", "const", "explicit", "extern", "implicit", "internal", //"new",
			"override", "params", "private", "protected", "public", "readonly", "ref",
			"sealed", "static", "unsafe", "virtual", "volatile");

		static readonly HashSet<Symbol> TypeKeywords = EcsNodePrinter.SymbolSet(
			"bool", "byte", "char", "decimal", "double", "float", "int", "long",
			"object", "sbyte", "short", "string", "uint", "ulong", "ushort", "void");

		#region Lookup tables: Keyword trie and operator lists

		private class Trie
		{
			public char CharOffs;
			public Trie[] Child;
			public Symbol Value;
			public TokenType TokenType; // "AttrKeyword", "TypeKeyword" or same as Keyword
		}
		private static Trie BuildTrie(IEnumerable<Symbol> words, char minChar, char maxChar, Func<Symbol, TokenType> getTokenType, Func<Symbol, Symbol> getValue)
		{
			var trie = new Trie { CharOffs = minChar };
			foreach (Symbol word in words) {
				var t = trie;
				foreach (char c in word.Name) {
					t.Child = t.Child ?? new Trie[maxChar - minChar + 1];
					t = t.Child[c - t.CharOffs] = t.Child[c - t.CharOffs] ?? new Trie { CharOffs = minChar };
				}
				t.Value = getValue(word);
				t.TokenType = getTokenType(word);
			}
			return trie;
		}
		// Variable-length find method
		private static bool FindInTrie(Trie t, string source, int start, out int stop, ref Symbol value, ref TokenType type)
		{
			bool success = false;
			stop = start;
			for (int i = start; ; i++) {
				if (t.Value != null) {
					value = t.Value;
					type = t.TokenType;
					success = true;
					stop = i;
				}
				char input = source.TryGet(i, (char)0xFFFF);
				int input_i = input - t.CharOffs;
				if (t.Child == null || (uint)input_i >= t.Child.Length)
					return success;
				if ((t = t.Child[input - t.CharOffs]) == null)
					return success;
			}
		}

		private static readonly Trie PunctuationTrie = BuildTrie(PunctuationIdentifiers, (char)32, (char)127, 
			word => TT.Id, word => word);
		private static readonly Trie PreprocessorTrie = BuildTrie(PreprocessorIdentifiers, (char)32, (char)127, 
			word => (TT)Enum.Parse(typeof(TT), "PP" + word),
			word => GSymbol.Get("##" + word));
		private static readonly Trie KeywordTrie = BuildTrie(CsKeywords, 'a', 'z', word => {
			if (AttrKeywords.Contains(word))
				return TT.AttrKeyword;
			if (TypeKeywords.Contains(word))
				return TT.TypeKeyword;
			return (TT)Enum.Parse(typeof(TT), word.Name);
		},	word => GSymbol.Get("#" + word));

		static readonly Symbol[] OperatorSymbols, OperatorEqualsSymbols;
		static EcsLexer()
		{
			OperatorSymbols = new Symbol[128];
			OperatorEqualsSymbols = new Symbol[128];
			foreach (Symbol op in PunctuationIdentifiers) {
				if (op.Name.Length == 2)
					OperatorSymbols[(int)op.Name[1]] = op;
				else if (op.Name.Length == 3 && op.Name[2] == '=')
					OperatorEqualsSymbols[(int)op.Name[1]] = op;
			}
		}
		void OnOneCharOperator(int ch)
		{
			_value = OperatorSymbols[ch];
			Debug.Assert(_value != null);
		}
		void OnOperatorEquals(int ch)
		{
			_value = OperatorEqualsSymbols[ch];
			Debug.Assert(_value != null);
		}

		#endregion

		public Token? NextToken()
		{
			_startPosition = InputPosition;
			_value = null;
			_style = 0;
			if (InputPosition >= CharSource.Count)
				return null;
			else {
				Token();
				Debug.Assert(InputPosition > _startPosition);
				return new Token(_type, _startPosition, InputPosition - _startPosition, _style, _value);
			}
		}

		#region Value parsers
		// After the generated lexer code determines the boundaries of the token, 
		// the value parser extracts the value of the token (e.g. "17L" => (long)17)
		// There are value parsers for identifiers, numbers, and strings; certain
		// parser cores are also accessible as public static methods.

		private bool FindCurrentIdInKeywordTrie(Trie t, string source, int start, ref Symbol value, ref TokenType type)
		{
			Debug.Assert(InputPosition >= start);
			for (int i = start, stop = InputPosition; i < stop; i++) {
				char input = source[i];
				int input_i = input - t.CharOffs;
				if (t.Child == null || (uint)input_i >= t.Child.Length) {
					if (input == '\'' && t.Value != null) {
						// Detected keyword followed by single quote. This requires 
						// the lexer to backtrack so that, for example, case'x' is 
						// treated as two tokens instead of the one token it 
						// initially appears to be.
						InputPosition = i;
						break;
					}
					return false;
				}
				if ((t = t.Child[input - t.CharOffs]) == null)
					return false;
			}
			if (t.Value != null) {
				value = t.Value;
				type = t.TokenType;
				return true;
			}
			return false;
		}

		bool ParseIdValue()
		{
			bool isPPLine = false;
			Symbol keyword = null;
			if (_parseNeeded) {
				int len;
				_value = ParseIdentifier(CharSource.Text, _startPosition, out len, Error);
				Debug.Assert(len == InputPosition - _startPosition);
				// Detect whether this is a preprocessor token
				if (_allowPPAt == _startPosition && _value.ToString().TryGet(0) == '#') {
					if (FindCurrentIdInKeywordTrie(PreprocessorTrie, CharSource.Text, _startPosition + 1, ref keyword, ref _type)) {
						if (_type == TT.PPregion || _type == TT.PPwarning || _type == TT.PPerror || _type == TT.PPnote)
							isPPLine = true;
					}
				}
			} else if (FindCurrentIdInKeywordTrie(KeywordTrie, CharSource.Text, _startPosition, ref keyword, ref _type))
				_value = keyword;
			else
				_value = GSymbol.Get(CharSource.Substring(_startPosition, InputPosition - _startPosition));
			return isPPLine;
		}

		static ScratchBuffer<StringBuilder> _idBuffer = new ScratchBuffer<StringBuilder>(() => new StringBuilder());
		static readonly Symbol _Hash = GSymbol.Get("#");
		static readonly Symbol _Dollar = GSymbol.Get("$");

		public static Symbol ParseIdentifier(string source, int start, out int length, Action<int, string> onError)
		{
			var parsed = _idBuffer.Value;
			parsed.Clear();

			Symbol result;
			int i = start;
			char c = source.TryGet(i, (char)0xFFFF);
			if (c == '@' || c == '#') {
				char c1 = source.TryGet(i+1, (char)0xFFFF);
				if ((c == '@' && c1 != '#') || (c == '#' && c1 == '@')) {
					i++;
					if (c == '#') {
						i++;
						parsed.Append('#');
					}
					// expecting: BQStringV | Plus(IdCont)
					c = source.TryGet(i, (char)0xFFFF);
					if (c == '`') {
						result = ScanBQIdentifier(source, ref i, onError, parsed, true);
					} else if (IsIdContChar(c)) {
						result = ScanNormalIdentifier(source, ref i, parsed, c);
					} else {
						length = 0;
						return null;
					}
				} else {
					Debug.Assert(c == '#' || (c == '@' && c1 == '#'));
					i++;
					parsed.Append('#');
					if (c == '@')
						i++;

					// expecting: (Comma | Colon | Semicolon | Operator | SpecialId | "<<" | ">>" | "**" | '$')?
					// where SpecialId ==> BQStringN | Plus(IdCont)
					c = source.TryGet(i, (char)0xFFFF);
					if (c == '`') {
						result = ScanBQIdentifier(source, ref i, onError, parsed, true);
					} else if (IsIdContChar(c)) {
						result = ScanNormalIdentifier(source, ref i, parsed, c);
					} else {
						// Detect a punctuation identifier (#+, #??, #>>, etc)
						Symbol value = null;
						TT _ = 0;
						if (FindInTrie(PunctuationTrie, source, i - 1, out i, ref value, ref _))
							result = value;
						else {
							result = _Hash;
							i++;
						}
					}
				}
			} else if (IsIdStartChar(c) || IsEscapeStart(c, source.TryGet(i+1, (char)0xFFFF)))
				result = ScanNormalIdentifier(source, ref i, parsed, c);
			else
				result = null;

			length = i - start;
			return result;
		}

		private static Symbol ScanNormalIdentifier(string source, ref int i, StringBuilder parsed, char c)
		{
			if (c != '\\')
				parsed.Append(c);
			else if (!ScanUnicodeEscape(source, ref i, parsed, c))
				goto stop;

			for (;;) {
				c = source.TryGet(++i, (char)0xFFFF);
				if (IsIdContChar(c))
					parsed.Append(c);
				else if (!ScanUnicodeEscape(source, ref i, parsed, c))
					break;
			}
		stop:
			return GSymbol.Get(parsed.ToString());
		}
		
		private static bool ScanUnicodeEscape(string source, ref int i, StringBuilder parsed, char c)
		{
			// I can't imagine why this is in C# in the first place. Unicode 
			// escapes inside identifiers are required to be letters or digits,
			// although the lexer doesn't enforce this (EC# has no such rule.)
			if (c != '\\')
				return false;
			char u = source.TryGet(i + 1, '\0');
			int len = 4;
			if (u == 'u' || u == 'U') {
				if (u == 'U') len = 8;
				string hex;
				if ((hex = source.SafeSubstring(i + 2, len)).Length != len)
					return false;
				int code;
				if (G.TryParseHex(hex, out code) == len && code <= 0x0010FFFF) {
					if (code >= 0x10000) {
						parsed.Append((char)(0xD800 + ((code - 0x10000) >> 10)));
						parsed.Append((char)(0xDC00 + ((code - 0x10000) & 0x3FF)));
					} else
						parsed.Append((char)code);
					i += len + 1;
					return true;
				}
			}
			return false;
		}

		private static Symbol ScanBQIdentifier(string source, ref int i, Action<int, string> onError, StringBuilder parsed, bool isVerbatim)
		{
			Symbol result;
			int stop;
			string str = UnescapeString(source, i, out stop, onError, false, isVerbatim).ToString();
			i = stop + 1;
			if (parsed.Length == 0)
				result = GSymbol.Get(str);
			else {
				parsed.Append(str);
				result = GSymbol.Get(parsed.ToString());
			}
			return result;
		}

        static bool IsEscapeStart(char c0, char c1) { return c0 == '\\' && c1 == 'u'; }
		static bool IsIdStartChar(char c) { return c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_' || c >= 0x80 && char.IsLetter(c); }
		static bool IsIdContChar(char c) { return IsIdStartChar(c) || c >= '0' && c <= '9' || c == '\''; }

		void ParseSymbolValue()
		{
			if (_verbatims == -1) {
				if (_parseNeeded) {
					var parsed = new StringBuilder();
					int i = _startPosition + 1;
					_value = ScanNormalIdentifier(CharSource.Text, ref i, parsed, CharSource.TryGet(i, (char)0xFFFF));
					Debug.Assert(i == InputPosition);
				} else
					_value = GSymbol.Get(CharSource.Substring(_startPosition + 1, InputPosition - _startPosition - 1));
			} else {
				var parsed = new StringBuilder();
				int i = _startPosition + 1;
				_value = ScanBQIdentifier(CharSource.Text, ref i, Error, parsed, false);
			}
		}

		void ParseCharValue()
		{
			ParseStringCore();
			var s = _value as string;
			if (s != null) {
				if (s.Length == 0) {
					_value = '\0';
					Error(_startPosition, Localize.From("Empty character literal", s.Length));
				} else {
					_value = G.Cache(s[0]);
					if (s.Length != 1)
						Error(_startPosition, Localize.From("Character constant has {0} characters (there should be exactly one)", s.Length));
				}
			}
		}
		void ParseBQStringValue()
		{
			ParseStringCore();
			_value = GSymbol.Get(_value.ToString());
		}
		void ParseStringValue()
		{
			ParseStringCore();
			if (_value.ToString().Length < 16)
				_value = G.Cache(_value);
		}

		void ParseStringCore()
		{
			char stringType = CharSource[_startPosition + _verbatims];
			Debug.Assert(_verbatims == 0 || CharSource[_startPosition] == '@');
			Debug.Assert(stringType == '"' || stringType == '\'' || stringType == '`');
			int start = _startPosition + _verbatims + 1;
			int stop = InputPosition - 1;
			if (CharSource.TryGet(InputPosition - 1, (char)0xFFFF) != stringType || stop < start)
				Error(Localize.From("Expected end-of-string marker here ({0})", stringType));

			if (stop < start)
				_value = "";
			else if (_parseNeeded || stop < start) {
	 			string sourceText = CharSource.Text;
				char verbatimType = _verbatims > 0 ? stringType : '\0';
				_value = UnescapeString(sourceText, start, stop, Error, _verbatims != 1, verbatimType);
			} else {
				_value = CharSource.Substring(start, stop - start);
				Debug.Assert(!_value.ToString().Contains(stringType) && (!_value.ToString().Contains('\\') || _verbatims != 0));
			}
		}

		public static object UnescapeString(string sourceText, int openQuoteIndex, out int stop, Action<int, string> onError, bool detectInterpolations, bool isVerbatim)
		{
			return UnescapeString(sourceText, openQuoteIndex + 1, out stop, onError, detectInterpolations, sourceText[openQuoteIndex], isVerbatim);
		}
		public static object UnescapeString(string sourceText, int start, out int stop, Action<int, string> onError, bool detectInterpolations, char stringType, bool isVerbatim)
		{
			for (stop = start; ; stop++) {
				if ((uint)stop >= (uint)sourceText.Length) {
					onError(stop, Localize.From("End-of-file in string literal"));
					break;
				}
				char c = sourceText[stop];
				if (c == stringType) {
					if (isVerbatim && sourceText.TryGet(stop+1, '\0') == stringType) {
						stop++; 
						continue;
					}
					break;
				}
				if (!isVerbatim) {
					if (c == '\\')
						stop++;
					else if (c == '\r' || c == '\n') {
						onError(stop, Localize.From("End-of-line in string literal"));
						break;
					}
				}
			}
			return UnescapeString(sourceText, start, stop, onError, detectInterpolations, isVerbatim ? stringType : '\0');
		}
		public static object UnescapeString(string sourceText, int start, int stop, Action<int, string> onError, bool detectInterpolations, char verbatimType = '\0')
		{
			ApparentInterpolatedString swse = null;

			var sb = new StringBuilder(stop - start);
			if (verbatimType == '\0') {
				// Unescape the string and detect interpolations, if any
				for (int i = start; i < stop; ) {
					int oldi = i;
					char c = G.UnescapeChar(sourceText, ref i);
					if (c == '\\' && i == oldi + 1) {
						// This backslash was ignored by UnescapeChar
						if (detectInterpolations && (c = sourceText.TryGet(i, '\0')) == '(' || c == '{') {
							// Apparent string interpolation
							if (swse == null)
								swse = new ApparentInterpolatedString();
							swse.StartLocations.Add(sb.Length);
						} else {
							onError(i, Localize.From(@"Unrecognized escape sequence '\{0}' in string", G.EscapeCStyle(c.ToString(), EscapeC.Control)));
						}
					}
					sb.Append(c);
				}
			} else {
				// Replace "" with " (or `` with `, depending on the string type)
				// and detect interpolations, if any.
				for (int i = start; i < stop; i++) {
					char c;
					if ((c = sourceText[i]) == verbatimType) {
						Debug.Assert(sourceText[i + 1] == verbatimType);
						i++;
					}
					if (c == '\\' && detectInterpolations && ((c = sourceText.TryGet(i + 1, '\0')) == '(' || c == '}')) {
						if (swse == null)
							swse = new ApparentInterpolatedString();
						swse.StartLocations.Add(sb.Length);
					}
					sb.Append(c);
				}
			}
			if (swse != null) {
				swse.String = sb.ToString();
				return swse;
			} else
				return sb.ToString();
		}

		static Symbol _sub = GSymbol.Get("#-");
		static Symbol _F = GSymbol.Get("F");
		static Symbol _D = GSymbol.Get("D");
		static Symbol _M = GSymbol.Get("M");
		static Symbol _U = GSymbol.Get("U");
		static Symbol _L = GSymbol.Get("L");
		static Symbol _UL = GSymbol.Get("UL");

		void ParseNumberValue()
		{
			// Optimize the most common case: a one-digit integer
			if (InputPosition == _startPosition + 1) {
				Debug.Assert(char.IsDigit(CharSource[_startPosition]));
				_value = G.Cache(CharSource[_startPosition] - '0');
				return;
			}

			if (_isFloat) {
				if (_numberBase == 10) {
					ParseFloatValue();
				} else {
					Debug.Assert(char.IsLetter(CharSource[_startPosition+1]));
					ParseSpecialFloatValue();
				}
			} else {
				ParseIntegerValue();
			}
		}

		private void ParseFloatValue()
		{
			string token = CharSource.Substring(_startPosition, InputPosition - _startPosition - _typeSuffix.Name.Length);
			token = token.Replace("_", "");
			if (_typeSuffix == _F) {
				float f;
                G.Verify(float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out f));
				_value = f;
			} else if (_typeSuffix == _M) {
                decimal m = 0.3e+2m;
				G.Verify(decimal.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out m));
				_value = m;
			} else {
				double d;
                G.Verify(double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out d));
				_value = d;
			}
		}

		private void ParseIntegerValue()
		{
			// Some kind of integer
			int index = _startPosition;
			if (_isNegative)
				index++;
			if (_numberBase != 10) {
				Debug.Assert(char.IsLetter(CharSource[index + 1]));
				index += 2;
			}
			int len = InputPosition - _startPosition;

			// Parse the integer
			ulong unsigned;
			bool overflow = !G.TryParseAt(CharSource.Text, ref index, out unsigned, _numberBase, G.ParseFlag.SkipUnderscores);
            Debug.Assert(index == InputPosition - _typeSuffix.Name.Length);

			// If no suffix, automatically choose int, uint, long or ulong
			var suffix = _typeSuffix;
			if (suffix == GSymbol.Empty) {
				if (unsigned > long.MaxValue)
					suffix = _UL;
				else if (unsigned > uint.MaxValue)
					suffix = _L;
				else if (unsigned > int.MaxValue)
					suffix = _U;
			}

			if (_isNegative && (suffix == _U || suffix == _UL)) {
				// Oops, an unsigned number can't be negative, so treat 
				// '-' as a separate token and let the number be reparsed.
				InputPosition = _startPosition + 1;
				_type = TT.Sub;
				_value = _sub;
				return;
			}

			// Set _value to an integer of the appropriate type 
			if (suffix == _UL)
				_value = unsigned;
			else if (suffix == _U) {
				overflow = overflow || (uint)unsigned != unsigned;
				_value = (uint)unsigned;
			} else if (suffix == _L) {
				if (_isNegative) {
					overflow = overflow || -(long)unsigned > 0;
					_value = -(long)unsigned;
				} else {
					overflow = overflow || (long)unsigned < 0;
					_value = (long)unsigned;
				}
			} else {
				_value = _isNegative ? -(int)unsigned : (int)unsigned;
			}

			if (overflow)
				Error(_startPosition, Localize.From("Overflow in integer literal (the number is 0x{0:X} after binary truncation).", _value));
			return;
		}

		private void ParseSpecialFloatValue()
		{
			Error(_startPosition, "Support for hex and binary float constants is not yet implemented.");
			_value = double.NaN;
		}

		#endregion

		// Due to the way generics are implemented, repeating the implementation 
		// of this base-class method might improve performance (TODO: verify this idea)
		new protected int LA(int i)
		{
			bool fail = false;
			char result = CharSource.TryGet(InputPosition + i, ref fail);
			return fail ? -1 : result;
		}

		private int MeasureIndent(int startIndex, int length)
		{
			int indent = 0, end = startIndex + length;
			for (int i = startIndex; i != end; i++) {
				if (Source[startIndex] == '\t')
					indent = ((indent / SpacesPerTab) + 1) * SpacesPerTab;
				else
					indent++;
			}
			return indent;
		}
	}

	/// <summary>A token that represents an interpolated string has a 
	/// <see cref="Token.Value"/> that is an instance of this class. The lexer does
	/// not lex or parse an interpolated expression, but merely produces one of
	/// these objects, which indicates the locations where interpolations seem to 
	/// begin.</summary>
	public class ApparentInterpolatedString
	{
		public string String;
		/// <summary>A list of indexes where "\(" and "\{" appear in String.</summary>
		public List<int> StartLocations = new List<int>();

		public override string ToString() { return String; }
	}

}
