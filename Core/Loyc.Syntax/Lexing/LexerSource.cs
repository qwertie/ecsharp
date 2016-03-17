using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Loyc.Collections;

namespace Loyc.Syntax.Lexing
{
	/// <summary>An implementation of the LLLPG Lexer API, used with the LLLPG
	/// options <c>inputSource</c> and <c>inputClass</c>.</summary>
	/// <remarks>
	/// This derived class simply makes public all of the LLLPG APIs which are 
	/// marked protected in <see cref="BaseLexer{CharSrc}"/>.
	/// </remarks>
	/// <example>
	/// LLLPG(lexer(inputSource(src), inputClass(LexerSource))) {
	/// 	static rule int ParseInt(string input) {
	/// 		var src = (LexerSource&lt;UString>)input;
	/// 		@[ (d:='0'..'9' {$result = $result * 10 + (d - '0');})+ ];
	/// 	}
	/// }
	/// </example>
	/// <typeparam name="CharSrc">A class that implements ICharSource. In order
	/// to write lexers that can accept any source of characters, set 
	/// CharSrc=ICharSource. For maximum performance when parsing strings (or
	/// to avoid memory allocation), set CharSrc=UString (<see cref="UString"/> 
	/// is a wrapper around <c>System.String</c> that, among other things, 
	/// implements <c>ICharSource</c>; please note that C# will implicitly convert 
	/// normal strings to <see cref="UString"/> for you).</typeparam>
	public class LexerSource<CharSrc> : LexerSourceWorkaround<CharSrc>, ILllpgLexerApi<int>
		where CharSrc : ICharSource
	{
		public new static HashSet<int> NewSet(params int[] items) { return BaseLexer<CharSrc>.NewSet(items); }
		public new static HashSet<int> NewSetOfRanges(params int[] ranges) { return BaseLexer<CharSrc>.NewSetOfRanges(ranges); } 

		/// <summary>Initializes LexerSource.</summary>
		/// <param name="source">A source of characters, e.g. <see cref="UString"/>.</param>
		/// <param name="fileName">A file name associated with the characters, 
		/// which will be used for error reporting.</param>
		/// <param name="inputPosition">A location to start lexing (normally 0).
		/// Careful: If you're starting to lex in the middle of the file, the 
		/// <see cref="BaseLexer{C}.LineNumber"/> still starts at 1, and (if <c>newSourceFile</c>
		/// is true) the <see cref="SourceFile"/> object may or may not discover 
		/// line breaks prior to the starting point, depending on how it is used.</param>
		/// <param name="newSourceFile">Whether to create a <see cref="LexerSourceFile{C}"/>
		/// object (an implementation of <see cref="ISourceFile"/>) to keep track 
		/// of line boundaries. The <see cref="SourceFile"/> property will point
		/// to this object, and it will be null if this parameter is false. Using 
		/// 'false' will avoid memory allocation, but prevent you from mapping 
		/// character positions to line numbers and vice versa. However, this
		/// object will still keep track of the current <see cref="BaseLexer{C}.LineNumber"/> 
		/// and <see cref="LineStartAt"/> (the index where the current line started) 
		/// when this parameter is false.</param>
		public LexerSource(CharSrc source, string fileName = "", int inputPosition = 0, bool newSourceFile = true)
			: base(source, fileName, inputPosition, newSourceFile) {}

		public static explicit operator LexerSource<CharSrc>(CharSrc str) { return new LexerSource<CharSrc>(str); }
	
		/// <inheritdoc/>
		public new virtual void Reset(CharSrc source, string fileName = "", int inputPosition = 0, bool newSourceFile = true)
		{
			base.Reset(source, fileName, inputPosition, newSourceFile);
		}
		public new void Reset() { base.Reset(); }

		public new int LA0 { get { return base.LA0; } }
		public new CharSrc CharSource
		{
			get { return base.CharSource; }
		}

		public new string FileName 
		{ 
			get { return base.FileName; }
		}

		public new int InputPosition
		{
			get { return base.InputPosition; }
			#if DotNet45
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			#endif
			protected set { base.InputPosition = value; }
		}

		public new LexerSourceFile<CharSrc> SourceFile
		{
			get { return base.SourceFile; }
		}

		public new int LA(int i) { return base.LA(i); }

		/// <inheritdoc/>
		#if DotNet45
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		#endif
		public new void Skip() { base.Skip(); }
		/// <inheritdoc/>
		public new int LineStartAt { get { return base.LineStartAt; } }
		/// <inheritdoc/>
		public new virtual void AfterNewline() { base.AfterNewline(); }
		/// <inheritdoc/>
		public new void Newline() { base.Newline(); }

		#region Normal matching

		public new int MatchAny() { return base.MatchAny(); }
		public new int Match(HashSet<int> set) { return base.Match(set); }
		#if DotNet45
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		#endif
		public new int Match(int a) { return base.Match(a); }
		public new int Match(int a, int b) { return base.Match(a, b); }
		public new int Match(int a, int b, int c) { return base.Match(a, b, c); }
		public new int Match(int a, int b, int c, int d) { return base.Match(a, b, c, d); }
		public new int MatchRange(int aLo, int aHi) { return base.MatchRange(aLo, aHi); }
		public new int MatchRange(int aLo, int aHi, int bLo, int bHi) { return base.MatchRange(aLo, aHi, bLo, bHi); }
		public new int MatchExcept() { return base.MatchExcept(); }
		public new int MatchExcept(HashSet<int> set) { return base.MatchExcept(set); }
		public new int MatchExcept(int a) { return base.MatchExcept(a); }
		public new int MatchExcept(int a, int b) { return base.MatchExcept(a, b); }
		public new int MatchExcept(int a, int b, int c) { return base.MatchExcept(a, b, c); }
		public new int MatchExcept(int a, int b, int c, int d) { return base.MatchExcept(a, b, c, d); }
		public new int MatchExceptRange(int aLo, int aHi) { return base.MatchExceptRange(aLo, aHi); }
		public new int MatchExceptRange(int aLo, int aHi, int bLo, int bHi) { return base.MatchExceptRange(aLo, aHi, bLo, bHi); }

		#endregion

		#region Try-matching

		public new bool TryMatch(HashSet<int> set)      { return base.TryMatch(set); }
		public new bool TryMatch(int a)                 { return base.TryMatch(a); }
		public new bool TryMatch(int a, int b)          { return base.TryMatch(a, b); }
		public new bool TryMatch(int a, int b, int c)   { return base.TryMatch(a, b, c); }
		public new bool TryMatch(int a, int b, int c, int d) { return base.TryMatch(a, b, c, d); }
		public new bool TryMatchRange(int aLo, int aHi) { return base.TryMatchRange(aLo, aHi); }
		public new bool TryMatchRange(int aLo, int aHi, int bLo, int bHi) { return base.TryMatchRange(aLo, aHi, bLo, bHi); }
		public new bool TryMatchExcept()                    { return base.TryMatchExcept(); }
		public new bool TryMatchExcept(HashSet<int> set)    { return base.TryMatchExcept(set); }
		public new bool TryMatchExcept(int a)               { return base.TryMatchExcept(a); }
		public new bool TryMatchExcept(int a, int b)        { return base.TryMatchExcept(a, b); }
		public new bool TryMatchExcept(int a, int b, int c) { return base.TryMatchExcept(a, b, c); }
		public new bool TryMatchExcept(int a, int b, int c, int d) { return base.TryMatchExcept(a, b, c, d); }
		public new bool TryMatchExceptRange(int aLo, int aHi) { return base.TryMatchExceptRange(aLo, aHi); }
		public new bool TryMatchExceptRange(int aLo, int aHi, int bLo, int bHi) { return base.TryMatchExceptRange(aLo, aHi, bLo, bHi); }

		#endregion

		public new virtual void Check(bool expectation, string expectedDescr = "") { base.Check(expectation, expectedDescr); }

		protected override void Error_Renamed(int lookaheadIndex, string format, params object[] args)
			{ Error(lookaheadIndex, format, args); }
		protected override void Error_Renamed(int lookaheadIndex, string format)
			{ Error(lookaheadIndex, format); }
		public new virtual void Error(int lookaheadIndex, string format)
			{ Error(lookaheadIndex, format, (object[])null); }
		public new virtual void Error(int lookaheadIndex, string format, params object[] args) 
		{
			int index = InputPosition + lookaheadIndex;
			SourcePos pos;
			if (SourceFile == null)
				pos = new SourcePos(FileName, LineNumber, index - LineStartAt + 1);
			else
				pos = SourceFile.IndexToLine(index);

			if (ErrorSink != null) {
				if (args != null)
					ErrorSink.Write(Severity.Error, pos, format, args);
				else
					ErrorSink.Write(Severity.Error, pos, format);
			} else {
				string msg;
				if (args != null)
					msg = Localize.Localized(format, args);
				else
					msg = Localize.Localized(format);
				throw new FormatException(pos + ": " + msg);
			}
		}

		/// <inheritdoc/>
		public new void PrintChar(int c, StringBuilder sb) { base.PrintChar(c, sb); }
	}

	/// <summary>This class only exists to work around a limitation of the C# language:
	/// "cannot change access modifiers when overriding 'protected' inherited member Error(...)".</summary>
	public abstract class LexerSourceWorkaround<CharSrc> : BaseLexer<CharSrc>
		where CharSrc : ICharSource
	{
		public LexerSourceWorkaround(CharSrc source, string fileName = "", int inputPosition = 0, bool newSourceFile = true)
			: base(source, fileName, inputPosition, newSourceFile) {}

		protected abstract void Error_Renamed(int lookaheadIndex, string format);
		protected override void Error(int lookaheadIndex, string format)
			{ Error_Renamed(lookaheadIndex, format); }
		protected abstract void Error_Renamed(int lookaheadIndex, string format, params object[] args);
		protected override void Error(int lookaheadIndex, string format, params object[] args)
			{ Error_Renamed(lookaheadIndex, format, args); }
	}

	/// <summary>A synonym for <see cref="LexerSource{C}"/> where C is <see cref="ICharSource"/>.</summary>
	public class LexerSource : LexerSource<ICharSource>
	{
		void f<T>() where T:IServiceProvider{}
		public LexerSource(ICharSource source, string fileName = "", int inputPosition = 0, bool newSourceFile = true)
			: base(source, fileName, inputPosition, newSourceFile) { }
		
		public static explicit operator LexerSource(string str)      { return new LexerSource((UString)str); }
	}

	/// <summary>Adds the <see cref="AfterNewline"/> method to <see cref="SourceFile"/>.</summary>
	/// <remarks>
	/// When implementing a lexer, the most efficient approach to building the list
	/// of line breaks is to save the location of each newline as it is encountered 
	/// while lexing, rather than doing a separate pass over the file just to find 
	/// line breaks. This class supports this optimization.
	/// </remarks>
	public class LexerSourceFile<CharSource> : SourceFile<CharSource>, ISourceFile
		where CharSource : ICharSource
	{
		public LexerSourceFile(CharSource source, SourcePos startingPos = null) : base(source, startingPos) { }
		public LexerSourceFile(CharSource source, string fileName) : base(source, fileName) { }

		/// <summary>Allows a lexer to record the index of the character after 
		/// each line break, in the order they exist in the file or string.</summary>
		/// <param name="index">Index of the first character after the newline.</param>
		/// <remarks>
		/// A lexer is not required to call this method; if the lexer doesn't call 
		/// it, the list of line breaks (which is used to map indexes to line 
		/// numbers and vice versa) will be built on-demand when one calls methods
		/// such as <c>IndexToLine</c>.
		/// </remarks>
		public void AfterNewline(int index)
		{
			if (_lineOffsets.Last >= index)
				return;
			_lineOffsets.Add(index);
		}
	}
}
