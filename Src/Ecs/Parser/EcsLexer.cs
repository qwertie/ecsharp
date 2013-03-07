using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.CompilerCore;
using Loyc.LLParserGenerator;
using System.Diagnostics;
using Loyc;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Threading;
using Loyc.Utilities;

namespace Ecs.Parser
{
	public partial class EcsLexer : BaseLexer<StringCharSource>
	{
		public EcsLexer(string text) : base(new StringCharSource(text)) {}

		public bool AllowNestedComments = false;
		private bool _isFloat, _parseNeeded, _isNegative;
		private int _numberBase, _verbatims;
		private Symbol _typeSuffix;
		private Symbol _type; // predicted type of the current token
		private object _value;
		private int _startPosition;

		internal static readonly HashSet<Symbol> CsKeywords = ecs.EcsNodePrinter.CsKeywords;
		internal static readonly HashSet<Symbol> PunctuationIdentifiers = ecs.EcsNodePrinter.PunctuationIdentifiers;

		static readonly Symbol _Id = GSymbol.Get("Id");
		static readonly Symbol _Hash = GSymbol.Get("#");
		static readonly Symbol _Dollar = GSymbol.Get("$");
		static readonly Symbol _AttrKeyword = GSymbol.Get("AttrKeyword");
		static readonly Symbol _TypeKeyword = GSymbol.Get("TypeKeyword");

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
					t = t.Child[c - t.CharOffs] = t.Child[c - t.CharOffs] ?? new Trie();
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
				t = t.Child[input - t.CharOffs];
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
				t = t.Child[input - t.CharOffs];
			}
		}

		private static readonly Trie PunctuationTrie = BuildTrie(PunctuationIdentifiers, (char)32, (char)127, word => _Id);
		private static readonly Trie KeywordTrie = BuildTrie(CsKeywords, 'a', 'z', word => {
			if (AttrKeywords.Contains(word))
				return _AttrKeyword;
			if (TypeKeywords.Contains(word))
				return _TypeKeyword;
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

		public Token ParseNextToken()
		{
			_startPosition = _inputPosition;
			_value = null;
			Token();
			return new Token(_type, _startPosition, _inputPosition - _startPosition, _value);
		}

		#region Value parsers
		// After the generated lexer code determines the boundaries of the token, 
		// the value parser extracts the value of the token (e.g. "17L" => (long)17)
		// There are value parsers for identifiers, numbers, and strings; certain
		// parser cores are also accessible as public static methods.

		void ParseIdValue()
		{
			Symbol keyword = null;
			if (_parseNeeded) {
				int len;
				_value = ParseIdentifier(_source.Text, _startPosition, out len, (i, msg) => Error(msg));
				Debug.Assert(len == _inputPosition - _startPosition);
			} else if (FindInTrie(KeywordTrie, _source.Text, _startPosition, _inputPosition, ref keyword, ref _type))
				_value = keyword;
			else
				_value = GSymbol.Get(_source.Substring(_startPosition, _inputPosition - _startPosition));
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
			if (c == '@' || (c == '#' && c1 == '@')) {
				i++;
				if (c == '#') {
					i++;
					parsed.Append('#');
				}
				// expecting: BQStringV | Plus(IdCont)
				c = source.TryGet(i, (char)0xFFFF);
				if (c == '`') {
					result = ScanBQIdentifier(source, ref i, onError, parsed);
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
					result = ScanBQIdentifier(source, ref i, onError, parsed);
				} else if (IsIdContChar(c)) {
					result = ScanNormalIdentifier(source, ref i, parsed, c);
				} else {
					// Detect a punctuation identifier (#+, #??, #>>, etc)
					Symbol _ = null, value = null;
					if (FindInTrie(PunctuationTrie, source, i - 1, out i, ref value, ref _))
						result = value;
					else
						result = _Hash;
				}
			} else if (c == '$') {
				i++;
				result = _Dollar;
			} else if (IsIdStartChar(c))
				result = ScanNormalIdentifier(source, ref i, parsed, c);
			else
				result = null;

			length = i - start;
			return result;
		}

		private static Symbol ScanNormalIdentifier(string source, ref int i, StringBuilder parsed, char c)
		{
			parsed.Append(c);
			while (IsIdContChar(c = source.TryGet(++i, (char)0xFFFF)))
				parsed.Append(c);
			return GSymbol.Get(parsed.ToString());
		}
		private static Symbol ScanBQIdentifier(string source, ref int i, Action<int, string> onError, StringBuilder parsed)
		{
			Symbol result;
			int stop;
			string str = UnescapeString(source, i, out stop, onError, false, true).ToString();
			i = stop + 1;
			if (parsed.Length == 0)
				result = GSymbol.Get(str);
			else {
				parsed.Append(str);
				result = GSymbol.Get(parsed.ToString());
			}
			return result;
		}

		static bool IsIdStartChar(char c) { return c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_' || c >= 0x80 && char.IsLetter(c); }
		static bool IsIdContChar(char c) { return IsIdStartChar(c) || c >= '0' && c <= '9' || c == '\''; }
		
		void ParseCharValue()
		{
			ParseStringCore();
			var s = _value as string;
			if (s != null) {
				if (s.Length == 0) {
					_value = '\0';
					Error(Localize.From("Empty character literal", s.Length));
				} else {
					_value = G.Cache(s[0]);
					if (s.Length != 1)
						Error(Localize.From("Character constant has {0} characters (there should be exactly one)", s.Length));
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
			if (_source.TryGet(_inputPosition - 1, (char)0xFFFF) != stringType)
				Error(Localize.From("Expected end-of-string marker here ({0})", stringType));
			int start = _startPosition + _verbatims + 1;
			int stop = _inputPosition - 1;

			if (_parseNeeded) {
	 			string sourceText = _source.Text;
				char verbatimType = _verbatims > 0 ? stringType : '\0';
				_value = UnescapeString(sourceText, start, stop, (i, msg) => Error(msg), _verbatims != 1, verbatimType);
			} else {
				_value = _source.Substring(start, stop - start);
				Debug.Assert(!_value.ToString().Contains(stringType) && (!_value.ToString().Contains('\\') || _verbatims != 0));
				return;
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

		static Symbol _Operator = GSymbol.Get("Operator");
		static Symbol _sub = GSymbol.Get("-");
		static Symbol _F = GSymbol.Get("_F");
		static Symbol _D = GSymbol.Get("_D");
		static Symbol _M = GSymbol.Get("_M");
		static Symbol _U = GSymbol.Get("_U");
		static Symbol _L = GSymbol.Get("_L");
		static Symbol _UL = GSymbol.Get("_UL");

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
				G.Verify(float.TryParse(token, out f));
				_value = f;
			} else if (_typeSuffix == _M) {
				decimal m;
				G.Verify(decimal.TryParse(token, out m));
				_value = m;
			} else {
				double d;
				G.Verify(double.TryParse(token, out d));
				_value = d;
			}
		}

		private void ParseIntegerValue()
		{
			// Some kind of integer
			int start = _startPosition;
			if (_isNegative)
				start++;
			if (_numberBase != 10) {
				Debug.Assert(char.IsLetter(_source[start + 1]));
				start += 2;
			}
			int len = _inputPosition - _startPosition;

			// Parse the integer
			ulong unsigned;
			bool overflow = !G.TryParseAt(_source.Text, ref start, out unsigned, _numberBase, G.ParseFlag.SkipUnderscores);
			Debug.Assert(start == _inputPosition - _typeSuffix.Name.Length);

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

			if (_isNegative && suffix == _U || suffix == _UL) {
				// Oops, an unsigned number can't be negative, so treat 
				// '-' as a separate token and let the number be reparsed.
				_inputPosition = _startPosition + 1;
				_type = _Operator;
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
				Error(Localize.From("Overflow in integer literal (the number is {0} after binary truncation).", _value));
			return;
		}

		private void ParseSpecialFloatValue()
		{
			Error("Support for hex and binary float constants is not yet implemented.");
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

	public class TokenTree : DList<Token>
	{
		public TokenTree(StringCharSourceFile file, int capacity) : base(capacity) { File = file; }
		public TokenTree(StringCharSourceFile file, IIterable<Token> items) : base(items) { File = file; }
		public TokenTree(StringCharSourceFile file, ISource<Token> items) : base(items) { File = file; }
		public TokenTree(StringCharSourceFile file, ICollection<Token> items) : base(items) { File = file; }
		public TokenTree(StringCharSourceFile file, IEnumerable<Token> items) : base(items) { File = file; }
		public TokenTree(StringCharSourceFile file) { File = file; }
		public readonly StringCharSourceFile File;
	}

	public struct Token : IListSource<Token>
	{
		public Symbol Type;
		public int StartIndex, Length;
		/// <summary>The parsed value of the token.</summary>
		/// <remarks>The value is
		/// <ul>
		/// <li>For strings: the parsed value of the string (no quotes, escape 
		/// sequences removed), i.e. a boxed char or string, or 
		/// <see cref="ApparentInterpolatedString"/> if the string contains 
		/// so-called interpolation expressions. A backquoted string (which is
		/// a kind of operator) is converted to a Symbol.</li>
		/// <li>For numbers: the parsed value of the number (e.g. 4 => int, 4L => long, 4.0f => float)</li>
		/// <li>For identifiers: the parsed name of the identifier, as a Symbol (e.g. x => $x, @for => $for, @`1+1` => $`1+1`)</li>
		/// <li>For any keyword including AttrKeyword and TypeKeyword tokens: a 
		/// Symbol containing the name of the keyword (no "#" prefix)</li>
		/// <li>For all other tokens: null</li>
		/// <li>For openers and closers (open paren, open brace, etc.) after tree-ification: a TokenTree object.</li>
		/// <li>For punctuation and operators: the text of the punctuation with "#" in front, as a symbol</li>
		/// </ul></remarks>
		public object Value;
		public TokenTree Children { get { return Value as TokenTree; } }

		public Token(Symbol type, int startIndex, int length, object value = null)
		{
			Type = type;
			StartIndex = startIndex;
			Length = length;
			Value = value;
		}

		#region IListSource<Token> Members

		public Token this[int index]
		{
			get { return Children[index]; }
		}
		public Token TryGet(int index, ref bool fail)
		{
			var c = Children;
			if (c != null)
				return c.TryGet(index, ref fail);
			fail = true;
			return default(Token);
		}
		public Iterator<Token> GetIterator()
		{
			var c = Children;
			return c == null ? EmptyIterator<Token>.Value : c.GetIterator();
		}
		public IEnumerator<Token> GetEnumerator()
		{
			var c = Children;
			return c == null ? EmptyEnumerator<Token>.Value : c.GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		public int Count
		{
			get { var c = Children; return c == null ? 0 : c.Count; }
		}

		#endregion
	}
}
