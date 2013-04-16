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
using Loyc.LLParserGenerator;

namespace Ecs.Parser
{
	using LS = EcsTokenTypes;

	public class EcsTokenTypes
	{
		// Token types
		public static readonly Symbol Spaces = GSymbol.Get("Spaces");
		public static readonly Symbol Newline = GSymbol.Get("Newline");
		public static readonly Symbol SLComment = GSymbol.Get("SLComment");
		public static readonly Symbol MLComment = GSymbol.Get("MLComment");
		public static readonly Symbol SQString = GSymbol.Get("SQString");
		public static readonly Symbol DQString = GSymbol.Get("DQString");
		public static readonly Symbol BQString = GSymbol.Get("BQString");
		public static readonly Symbol Comma = GSymbol.Get("Comma");
		public static readonly Symbol Colon = GSymbol.Get("Colon");
		public static readonly Symbol Semicolon = GSymbol.Get("Semicolon");
		public static readonly Symbol Operator = GSymbol.Get("Operator");
		public static readonly Symbol Id = GSymbol.Get("Id");
		public static readonly Symbol Symbol = GSymbol.Get("Symbol");
		public static readonly Symbol LParen = GSymbol.Get("LParen");
		public static readonly Symbol RParen = GSymbol.Get("RParen");
		public static readonly Symbol LBrack = GSymbol.Get("LBrack");
		public static readonly Symbol RBrack = GSymbol.Get("RBrack");
		public static readonly Symbol LBrace = GSymbol.Get("LBrace");
		public static readonly Symbol RBrace = GSymbol.Get("RBrace");
		public static readonly Symbol LCodeQuote = GSymbol.Get("LCodeQuote");
		public static readonly Symbol LCodeQuoteS = GSymbol.Get("LCodeQuoteS");
		public static readonly Symbol Number = GSymbol.Get("Number");
		public static readonly Symbol AttrKeyword = GSymbol.Get("AttrKeyword");
		public static readonly Symbol TypeKeyword = GSymbol.Get("TypeKeyword");
		public static readonly Symbol Shebang = GSymbol.Get("Shebang");
		
		public static readonly Symbol @break     = GSymbol.Get("break");
		public static readonly Symbol @case      = GSymbol.Get("case");
		public static readonly Symbol @checked   = GSymbol.Get("checked");
		public static readonly Symbol @class     = GSymbol.Get("class");
		public static readonly Symbol @continue  = GSymbol.Get("continue");
		public static readonly Symbol @default   = GSymbol.Get("default");
		public static readonly Symbol @delegate  = GSymbol.Get("delegate ");
		public static readonly Symbol @do        = GSymbol.Get("do");
		public static readonly Symbol @enum      = GSymbol.Get("enum");
		public static readonly Symbol @event     = GSymbol.Get("event");
		public static readonly Symbol @fixed     = GSymbol.Get("fixed");
		public static readonly Symbol @for       = GSymbol.Get("for");
		public static readonly Symbol @foreach   = GSymbol.Get("foreach");
		public static readonly Symbol @goto      = GSymbol.Get("goto");
		public static readonly Symbol @if        = GSymbol.Get("if");
		public static readonly Symbol @interface = GSymbol.Get("interface");
		public static readonly Symbol @lock      = GSymbol.Get("lock");
		public static readonly Symbol @namespace = GSymbol.Get("namespace");
		public static readonly Symbol @return    = GSymbol.Get("return");
		public static readonly Symbol @struct    = GSymbol.Get("struct");
		public static readonly Symbol @switch    = GSymbol.Get("switch");
		public static readonly Symbol @throw     = GSymbol.Get("throw");
		public static readonly Symbol @try       = GSymbol.Get("try");
		public static readonly Symbol @unchecked = GSymbol.Get("unchecked");
		public static readonly Symbol @using     = GSymbol.Get("using");
		public static readonly Symbol @while     = GSymbol.Get("while");

		public static readonly Symbol @operator   = GSymbol.Get("operator");
		public static readonly Symbol @sizeof     = GSymbol.Get("sizeof");
		public static readonly Symbol @typeof     = GSymbol.Get("typeof");

		public static readonly Symbol @else       = GSymbol.Get("else");
		public static readonly Symbol @catch      = GSymbol.Get("catch");
		public static readonly Symbol @finally    = GSymbol.Get("finally");

		public static readonly Symbol @in         = GSymbol.Get("in");
		public static readonly Symbol @as         = GSymbol.Get("as");
		public static readonly Symbol @is         = GSymbol.Get("is");

		public static readonly Symbol @base       = GSymbol.Get("base");
		public static readonly Symbol @false      = GSymbol.Get("false");
		public static readonly Symbol @null       = GSymbol.Get("null");
		public static readonly Symbol @true       = GSymbol.Get("true");
		public static readonly Symbol @this       = GSymbol.Get("this");

		public static readonly Symbol @new        = GSymbol.Get("new");
		public static readonly Symbol @out        = GSymbol.Get("out");
		public static readonly Symbol @stackalloc = GSymbol.Get("stackalloc");

		public static readonly Symbol PPif        = GSymbol.Get("#if");
		public static readonly Symbol PPelse      = GSymbol.Get("#else");
		public static readonly Symbol PPelif      = GSymbol.Get("#elif");
		public static readonly Symbol PPendif     = GSymbol.Get("#endif");
		public static readonly Symbol PPdefine    = GSymbol.Get("#define");
		public static readonly Symbol PPundef     = GSymbol.Get("#undef");
		public static readonly Symbol PPwarning   = GSymbol.Get("#warning");
		public static readonly Symbol PPerror     = GSymbol.Get("#error");
		public static readonly Symbol PPnote      = GSymbol.Get("#note");
		public static readonly Symbol PPline      = GSymbol.Get("#line");
		public static readonly Symbol PPregion    = GSymbol.Get("#region");
		public static readonly Symbol PPendregion = GSymbol.Get("#endregion");

		public static readonly Symbol Hash = GSymbol.Get("#");
		public static readonly Symbol Dollar = GSymbol.Get("$");
	}
	/*public enum EcsTokenTypes
	{
		Spaces,
		Newline,
		SLComment,
		MLComment,
		SQString,
		DQString,
		BQString,
		Comma,
		Colon,
		Semicolon,
		Id,
		Symbol,
		LParen,
		RParen,
		LBrack,
		RBrack,
		LBrace,
		RBrace,
		LCodeQuote,
		LCodeQuoteS,
		Number,
		AttrKeyword,
		TypeKeyword,
		Shebang,
		
		@break    ,
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

		@base      ,
		@false     ,
		@null      ,
		@true      ,
		@this      ,

		@new       ,
		@out       ,
		@stackalloc,

		PPif       ,
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

		Hash,
		Dollar,

		// Operators
		Mul, Div, Add, Sub, Mod, Exp,
		Inc, Dec,
		And, Or, Xor, Not,
		AndBits, OrBits, XorBits, NotBits,
		Set, Eq, Neq, 
		GT, GE, LT, LE,
		Shr, Shl,
		QuestionMark,
		DotDot, Dot, NullDot, NullCoalesce,
		ColonColon, QuickBind,
		Forward,
		Substitute,

		MulSet, DivSet, ModSet, SubSet, AddSet, ExpSet,
		NullCoalesceSet, ConcatSet,
		ShrSet, ShlSet,
		XorBitsSet, AndBitsSet, OrBitsSet,
	}*/

	public interface ILexer
	{
		ICharSource Source { get; }
		Token? ParseNextToken();
		Action<int, string> OnError { get; set; }
	}
	
	public partial class EcsLexer : BaseLexer<StringCharSource>, ILexer
	{
		public EcsLexer(string text, Action<int, string> onError) : base(new StringCharSource(text)) { OnError = onError; }

		public bool AllowNestedComments = false;
		private bool _isFloat, _parseNeeded, _isNegative;
		private int _numberBase, _verbatims;
		private Symbol _typeSuffix;
		private Symbol _type; // predicted type of the current token
		private object _value;
		private int _startPosition;
		// _allowPPAt is used to detect whether a preprocessor directive is allowed
		// at the current input position. When _allowPPAt==_startPosition, it's allowed.
		private int _allowPPAt;

		public ICharSource Source { get { return _source; } }
		public Action<int, string> OnError { get; set; }

		protected override void Error(string message)
		{
			Error(_inputPosition, message);
		}
		protected void Error(int index, string message)
		{
			if (OnError != null)
				OnError(index, message);
			else
				throw new FormatException(message);
		}

		internal static readonly HashSet<Symbol> CsKeywords = ecs.EcsNodePrinter.CsKeywords;
		internal static readonly HashSet<Symbol> PunctuationIdentifiers = ecs.EcsNodePrinter.PunctuationIdentifiers;
		internal static readonly HashSet<Symbol> PreprocessorIdentifiers = ecs.EcsNodePrinter.SymbolSet(
			"if", "else", "elif", "endif", "define", "undef", "line", 
			"region", "endregion", "warning", "error", "note");

		// This is the set of keywords that act only as attributes on statements.
		// This list does not include "new" and "out", which are only allowed as 
		// attributes on variable declarations and other specific statements.
		static readonly HashSet<Symbol> AttrKeywords = ecs.EcsNodePrinter.SymbolSet(
			"abstract", "const", "explicit", "extern", "implicit", "internal", //"new",
			"override", "params", "private", "protected", "public", "readonly", "ref",
			"sealed", "static", "unsafe", "virtual", "volatile");

		static readonly HashSet<Symbol> TypeKeywords = ecs.EcsNodePrinter.SymbolSet(
			"bool", "byte", "char", "decimal", "double", "float", "int", "long",
			"object", "sbyte", "short", "string", "uint", "ulong", "ushort", "void");

		#region Lookup tables: Keyword trie and operator lists

		private class Trie
		{
			public char CharOffs;
			public Trie[] Child;
			public Symbol Value;
			public Symbol TokenType; // "AttrKeyword", "TypeKeyword" or same as Keyword
		}
		private static Trie BuildTrie(IEnumerable<Symbol> words, char minChar, char maxChar, Func<Symbol, Symbol> getTokenType)
		{
			var trie = new Trie { CharOffs = minChar };
			foreach (Symbol word in words) {
				var t = trie;
				foreach (char c in word.Name) {
					t.Child = t.Child ?? new Trie[maxChar - minChar + 1];
					t = t.Child[c - t.CharOffs] = t.Child[c - t.CharOffs] ?? new Trie { CharOffs = minChar };
				}
				t.Value = word;
				t.TokenType = getTokenType(word);
			}
			return trie;
		}
		private static bool FindInTrie(Trie t, string source, int start, int stop, ref Symbol value, ref Symbol type)
		{
			for (int i = start; i < stop; i++) {
				char input = source[i];
				int input_i = input - t.CharOffs;
				if (t.Child == null || (uint)input_i >= t.Child.Length)
					return false;
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
		// Variable-length find method
		private static bool FindInTrie(Trie t, string source, int start, out int stop, ref Symbol value, ref Symbol type)
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

		private static readonly Trie PunctuationTrie = BuildTrie(PunctuationIdentifiers, (char)32, (char)127, word => LS.Id);
		private static readonly Trie PreprocessorTrie = BuildTrie(PreprocessorIdentifiers, (char)32, (char)127, word => GSymbol.Get("#" + word.Name));
		private static readonly Trie KeywordTrie = BuildTrie(CsKeywords, 'a', 'z', word => {
			if (AttrKeywords.Contains(word))
				return LS.AttrKeyword;
			if (TypeKeywords.Contains(word))
				return LS.TypeKeyword;
			return word;
		});

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

		public Token? ParseNextToken()
		{
			_startPosition = _inputPosition;
			_value = null;
			if (_inputPosition >= _source.Count)
				return null;
			else {
				Token();
				Debug.Assert(_inputPosition > _startPosition);
				return new Token(_type, _startPosition, _inputPosition - _startPosition, _value);
			}
		}

		#region Value parsers
		// After the generated lexer code determines the boundaries of the token, 
		// the value parser extracts the value of the token (e.g. "17L" => (long)17)
		// There are value parsers for identifiers, numbers, and strings; certain
		// parser cores are also accessible as public static methods.

		bool ParseIdValue()
		{
			bool isPPLine = false;
			Symbol keyword = null;
			if (_parseNeeded) {
				int len;
				_value = ParseIdentifier(_source.Text, _startPosition, out len, Error);
				Debug.Assert(len == _inputPosition - _startPosition);
				// Detect whether this is a preprocessor token
				if (_allowPPAt == _startPosition && _value.ToString().TryGet(0) == '#') {
					if (FindInTrie(PreprocessorTrie, _source.Text, _startPosition + 1, _inputPosition, ref keyword, ref _type)) {
						if (_type == LS.PPregion || _type == LS.PPwarning || _type == LS.PPerror || _type == LS.PPnote)
							isPPLine = true;
					}
				}
			} else if (FindInTrie(KeywordTrie, _source.Text, _startPosition, _inputPosition, ref keyword, ref _type))
				_value = keyword;
			else
				_value = GSymbol.Get(_source.Substring(_startPosition, _inputPosition - _startPosition));
			return isPPLine;
		}

		static ScratchBuffer<StringBuilder> _idBuffer = new ScratchBuffer<StringBuilder>(() => new StringBuilder());

		public static Symbol ParseIdentifier(string source, int start, out int length, Action<int, string> onError)
		{
			var parsed = _idBuffer.Value;
			parsed.Clear();

			Symbol result;
			int i = start;
			char c = source.TryGet(i, (char)0xFFFF);
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
			} else if (c == '#' || (c == '@' && c1 == '#')) {
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
					Symbol _ = null, value = null;
					if (FindInTrie(PunctuationTrie, source, i - 1, out i, ref value, ref _))
						result = value;
					else {
						result = LS.Hash;
						i++;
					}
				}
			} else if (c == '$') {
				i++;
				result = LS.Dollar;
			} else if (IsIdStartChar(c) | IsEscapeStart(c, c1))
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
					_value = ScanNormalIdentifier(_source.Text, ref i, parsed, _source[i]);
					Debug.Assert(i == _inputPosition);
				} else
					_value = GSymbol.Get(_source.Substring(_startPosition + 1, _inputPosition - _startPosition - 1));
			} else {
				var parsed = new StringBuilder();
				int i = _startPosition + 1;
				_value = ScanBQIdentifier(_source.Text, ref i, Error, parsed, false);
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
			char stringType = _source[_startPosition + _verbatims];
			Debug.Assert(_verbatims == 0 || _source[_startPosition] == '@');
			Debug.Assert(stringType == '"' || stringType == '\'' || stringType == '`');
			int start = _startPosition + _verbatims + 1;
			int stop = _inputPosition - 1;
			if (_source.TryGet(_inputPosition - 1, (char)0xFFFF) != stringType || stop < start)
				Error(Localize.From("Expected end-of-string marker here ({0})", stringType));

			if (stop < start)
				_value = "";
			else if (_parseNeeded || stop < start) {
	 			string sourceText = _source.Text;
				char verbatimType = _verbatims > 0 ? stringType : '\0';
				_value = UnescapeString(sourceText, start, stop, Error, _verbatims != 1, verbatimType);
			} else {
				_value = _source.Substring(start, stop - start);
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
			if (_inputPosition == _startPosition + 1) {
				Debug.Assert(char.IsDigit(_source[_startPosition]));
				_value = G.Cache(_source[_startPosition] - '0');
				return;
			}

			if (_isFloat) {
				if (_numberBase == 10) {
					ParseFloatValue();
				} else {
					Debug.Assert(char.IsLetter(_source[_startPosition+1]));
					ParseSpecialFloatValue();
				}
			} else {
				ParseIntegerValue();
			}
		}

		private void ParseFloatValue()
		{
			string token = _source.Substring(_startPosition, _inputPosition - _startPosition - _typeSuffix.Name.Length);
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
				Debug.Assert(char.IsLetter(_source[index + 1]));
				index += 2;
			}
			int len = _inputPosition - _startPosition;

			// Parse the integer
			ulong unsigned;
			bool overflow = !G.TryParseAt(_source.Text, ref index, out unsigned, _numberBase, G.ParseFlag.SkipUnderscores);
            Debug.Assert(index == _inputPosition - _typeSuffix.Name.Length);

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
				_inputPosition = _startPosition + 1;
				_type = LS.Operator;
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
			char result = _source.TryGet(_inputPosition + i, ref fail);
			return fail ? -1 : result;
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
