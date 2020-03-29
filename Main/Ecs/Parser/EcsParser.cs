using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc;
using Loyc.Utilities;
using Loyc.Syntax;
using Loyc.Collections;
using Loyc.Syntax.Lexing;

/// <summary>Enhanced C# parser</summary>
namespace Loyc.Ecs.Parser
{
	using TT = TokenType;
	using EP = EcsPrecedence;
	using S = CodeSymbols;
	using Loyc.Collections.Impl;

	/// <summary>Parses Enhanced C# code into a sequence of Loyc trees 
	/// (<see cref="LNode"/>), one per top-level statement.</summary>
	/// <remarks>
	/// You can use <see cref="EcsLanguageService.Value"/> with extension method
	/// <see cref="ParsingService.Parse(IParsingService, UString, IMessageSink, ParsingMode, bool)"/>
	/// to easily parse a text string (holding zero or more EC# statements) into a 
	/// Loyc tree. One does not normally use this class directly.
	/// </remarks>
	public partial class EcsParser : BaseParser<Token>
	{
		protected LNodeFactory F;
		protected IListSource<Token> _tokensRoot;
		protected IListSource<Token> _tokens;
		// index into source text of the first token at the current depth (inside 
		// parenthesis, etc.). Used if we need to print an error inside empty {} [] ()
		protected int _startTextIndex = 0;

		public IListSource<Token> TokensRoot { get { return _tokensRoot; } }

		protected LNode _triviaWordAttribute;
		protected LNode _triviaUseOperatorKeyword;
		protected LNode _triviaForwardedProperty;
		protected LNode _filePrivate;

		public EcsParser(IListSource<Token> tokens, ISourceFile file, IMessageSink messageSink) : base(file)
		{
			ErrorSink = messageSink;
			Reset(tokens, file);
			
			_triviaWordAttribute = F.Id(S.TriviaWordAttribute);
			_triviaUseOperatorKeyword = F.Id(S.TriviaUseOperatorKeyword);
			_triviaForwardedProperty = F.Id(S.TriviaForwardedProperty);
			_filePrivate = F.Id(S.FilePrivate);
		}

		public virtual void Reset(IListSource<Token> tokens, ISourceFile file)
		{
			CheckParam.IsNotNull("tokens", tokens);
			CheckParam.IsNotNull("file", file);
			_tokensRoot = _tokens = tokens;
			_sourceFile = file;
			F = new LNodeFactory(file);
			InputPosition = 0; // reads LT(0)
			_tentative = new TentativeState(false);
		}

		// Normally we use prediction analysis to distinguish expressions from
		// variable declarations, but as it turns out, that task is too complex 
		// when parsing expressions. Instead we'll try parsing as an expression
		// first and, if errors occur, parse as a variable decl instead. For 
		// this purpose we need a mode in which errors are not printed out.
		// And since parsing contexts can be nested, we need a way to save and
		// restore state. TentativeState is used to save and restore error state,
		// and TentativeResult encapsulates the result of a tentative parse.
		protected TentativeState _tentative;
		public struct TentativeState
		{
			public readonly bool DeferErrors;
			public MessageHolder DeferredErrors;
			public int LocalErrorCount;
			public TentativeState(bool deferErrors)
			{
				DeferErrors = deferErrors;
				DeferredErrors = null;
				LocalErrorCount = 0;
			}
		}
		public struct TentativeResult
		{
			public LNode Result;
			public int OldPosition;   // position before parsing
			public int InputPosition; // position after parsing
			public MessageHolder Errors;
			public TentativeResult(int oldPosition)
			{
				this = default(TentativeResult);
				OldPosition = oldPosition;
			}
		}

		public IListSource<LNode> ParseExprs(bool allowTrailingComma = false, bool allowUnassignedVarDecl = false)
		{
			var list = new VList<LNode>();
			try {
				ExprList(ref list, allowTrailingComma, allowUnassignedVarDecl);
			} catch (Exception ex) { UnhandledException(ex); }
			return list;
		}

		public IListSource<LNode> ParseStmtsGreedy()
		{
			var list = new VList<LNode>();
			try {
				StmtList(ref list);
			} catch (Exception ex) { UnhandledException(ex); }
			ExpectEOF();
			return list;
		}
		public IEnumerator<LNode> ParseStmtsLazy()
		{
			while (LA0 != EOF) {
				LNode stmt;
				try { stmt = Stmt(); }
				catch (Exception ex) { UnhandledException(ex); break; }
				yield return stmt;
			}
			ExpectEOF();
		}
		private void ExpectEOF()
		{
			if (_parents.Count != 0)
				throw new InvalidStateException("Bug: EC# parser parent stack not empty"); // Failed to call Up?
			if (LA0 != TT.EOF)
				Error(0, "Expected end of file");
		}

		void UnhandledException(Exception ex)
		{
			int iPos = GetTextPosition(InputPosition);
			SourceRange pos = new SourceRange(_sourceFile, iPos);
			ErrorSink.Write(Severity.Critical, pos, "Bug: unhandled exception in parser - " + ex.ExceptionMessageAndType());
		}

		#region DetectStatementCategory

		// Originally I let LLLPG distinguish the statement types, but when the 
		// parser was mature, LLLPG's analysis took from 10 to 40 seconds and it 
		// generated 5000 lines of code to distinguish the various cases.
		// And that was without supporting tuple types.
		//
		// This custom code, which was very tricky to get right,
		// (1) detects method/property/variable (and lookalikes: trait, alias) and
		//     distinguishes certain other cases (StmtCat values) out of convenience.
		// (2) detects and skips word attributes (not including `this` in `this int x`).
		// (3) is fast if possible.
		//
		// "word attributes" are a tricky feature of EC#. C# has a few non-keyword 
		// attributes that appear at the beginning of a statement, such as "partial", 
		// "yield" and "async". EC# generalizes this concept to "word attributes", which 
		// can be ANY normal identifier.
		//
		// BTW: "word attributes" are not really "contextual keywords" in the C# 
		// sense, because the identifiers are not recognized by the parser. The
		// only true contextual keywords in EC# are "var", "module", "assembly",
		// "from" (LINQ), "trait", "alias", and "when"; others like "partial" and 
		// "yield" are merely "word attributes".
		//
		// Since they're not keywords, word attributes are difficult because:
		// 1. Lots of statements start with words that are not word attributes, 
		//    such as "foo=0", "foo* x = null", "foo x { ... }", and "foo x();"
		//    Somehow we have to figure out which words are attributes.
		// 2. Not all statements accept word attributes. But the attributes 
		//    appear BEFORE the statement, so how can we tell if we should
		//    be expecting word attributes or not?
		//
		// There are lots of "keyword-based" statements that accept word
		// attributes (think "partial class" and "yield return"), which are the
		// easiest to handle. Our main difficulty is the non-keyword statements:
		// 1. statements that can begin with an identifier, but also accept
		//    word attributes:
		//    - word type* name ... // field, property or method
		//    - word type? name ... // field, property or method
		//    - word type[,][] name ... // field, property or method
		//    - word type<x> name ... // field, property or method
		//    - word type name(); // method decl
		//    - word type name<x>(); // method decl
		//    - word type<x> name(); // method decl
		//    - word type<x> name<x>(); // method decl
		//    - word this();         // except inside methods
		//    - word this<x>();      // except inside methods
		//    - word label:
		// 2. statements that can start with an identifier, and do not accept 
		//    word attributes:
		//    - foo();     // method call OR constructor
		//    - foo<x>();
		//    - foo = 0;
		//    - foo - bar;
		//
		// Notice that if a word is followed by an operator of any kind, it 
		// can't be an attribute; but if it is followed by another word, it's
		// unclear. In particular, notice that for "A B<x>...", "A" is sometimes
		// an attribute and sometimes not. In "A B<x> C", A is an attribute,
		// but in "A B<x>()" it is a return type. The same difficulty exists
		// in case of alternate generics specifiers such as "A B!(x)" and types
		// with suffixes, like "A int?*".
		//
		// Note: DetectStatementCategory() is allowed to (mis)categorize "this int x" as an
		// IdStmt, so extension method parameters can't have word attributes.
		StmtCat DetectStatementCategory(out int wordAttrCount, DetectionMode mode)
		{
			// If the statement starts with identifier(s), we have to figure out 
			// whether, and how many of them, are word attributes. Also, skip over
			// `new` because can be a keyword attribute in some cases.
			wordAttrCount = 0;
			int wordsStartAt = InputPosition;
			bool haveNew = LA0 == TT.New;   // "new" keyword is the most annoying wrinkle
			if (haveNew || LA0 == TT.Id || LA0 == TT.ContextualKeyword || LA0 == TT.LinqKeyword)
			{
				if (!haveNew) {
					// Optimized path for common expressions that start with an Id (IdStmts)
					var la1k = LT(1).Kind;
					if (la1k == TokenKind.Assignment || la1k == TokenKind.Separator || la1k == (int)TT.EOF) {
						return StmtCat.IdStmt;
					} else if (la1k == TokenKind.Operator) {
						var la1 = LA(1);
						if (la1 != TT.QuestionMark && la1 != TT.LT && la1 != TT.Mul && la1 != TT.Power && la1 != TT.Substitute) { 
							return StmtCat.IdStmt;
						}
					} else if (la1k == TokenKind.LParen && LA(3) == TT.Semicolon) {
						return StmtCat.IdStmt; // Method();
					}
				}

				// Scan past identifiers and extra AttrKeywords at beginning of statement
				bool isAttrKw = haveNew;
				do {
					Skip(); // same as InputPosition++;
					if (isAttrKw)
						wordsStartAt = InputPosition;
					else
						wordAttrCount++;
					isAttrKw = LA0 == TT.New || LA0 == TT.AttrKeyword;
					haveNew |= LA0 == TT.New;
				} while (isAttrKw || LA0 == TT.Id || LA0 == TT.ContextualKeyword || 
				         LA0 == TT.LinqKeyword && !_insideLinqExpr);
			}

			// At this point we've skipped over all simple identifiers.
			// Now decide: what kind of statement do we appear to have?
			int consecutiveWords = InputPosition - wordsStartAt;
			if (LA0 == TT.TypeKeyword)
			{
				// The rest of this method expects this to be treated as if it were an extra word, 
				// although it cannot be treated as a word attribute.
				InputPosition++;
				consecutiveWords++;
			}
			else if (LA0 == TT.Substitute)
			{
				// The rest of this method expects this to be treated as if it were an extra word, 
				// although it cannot be treated as a word attribute.
				var la1 = LA(1);
				if (LA(1) == TT.LParen && LA(2) == TT.RParen)
				{
					InputPosition += 3;
				}
				else
				{
					InputPosition++;
				}
				consecutiveWords++;
			}
			else if (LA0 == TT.This)
			{
				if (LA(1) == TT.LParen && LA(2) == TT.RParen)
				{
					TT la3 = LA(3);
					if (la3 == TT.Colon || la3 == TT.LBrace || la3 == TT.Semicolon && _spaceName != S.Fn)
					{
						return StmtCat.ThisConstructor;
					}
					else
					{
						return StmtCat.OtherStmt;
					}
				}
				else if (consecutiveWords != 0)
				{
					// Appears to be a this[] property (there could be a type 
					// param, like this<T>[], so we can't check if LA(1) is "[")
					InputPosition--;
					return StmtCat.MethodOrPropOrVar;
				}
			}
			else if (LT0.Kind == TokenKind.OtherKeyword)
			{
				if (EasilyDetectedKeywordStatements.Contains(LA0) || LA0 == TT.Delegate && LA(1) != TT.LParen || (LA0 == TT.Checked || LA0 == TT.Unchecked) && LA(1) == TT.LBrace)
				{
					// `if` and `using` do not support word attributes:
					// - `if`, because in the original plan EC# was to support a 
					//   D-style 'if' clause at the end of property definitions, 
					//   making "T P if ..." ambiguous if word attributes were allowed.
					// - `using` because `x using T` was planned as a new cast operator.
					if (!(consecutiveWords > 0 && (LA0 == TT.If || LA0 == TT.Using)))
					{
						return StmtCat.KeywordStmt;
					}
				}
			}
			else if (consecutiveWords == 0 && !haveNew && LA0 != TT.LParen)
			{
				return StmtCat.OtherStmt;
			}

			// At this point we know it's not a "keyword statement" or "this constructor",
			// so it's either MethodOrPropOrVar, which allows word attributes, or 
			// something else that prohibits them (IdStmt or OtherStmt).
			if (consecutiveWords >= 2)
			{
				// We know it's MethodOrPropOrVar, but where do the word attributes end?
				int likelyStart = wordsStartAt + consecutiveWords - 2;   // most likely location
				if (ExpectedAfterTypeAndName[(int)mode].Contains(LA0))
				{
					InputPosition = likelyStart;
				}
				else
				{
					// We must distinguish among these three cases:
					// 1. IEnumerator IEnumerable.GetEnumerator()
					//    ^likelyStart(correct)  ^InputPosition
					// 2. alias              Map<K,V> = Dictionary <K,V>;
					//    ^likelyStart(correct) ^InputPosition
					// 3. partial    Namespace.Class Method()
					//    ^likelyStart(too low) ^InputPosition
					InputPosition = likelyStart + 1;

					if (Scan_ComplexNameDecl() && ExpectedAfterTypeAndName[(int)mode].Contains(LA0))
						InputPosition = likelyStart;
					else
						InputPosition = likelyStart + 1;
				}
				return StmtCat.MethodOrPropOrVar;
			}

			// At this point we're sure there are no word attributes, but it's the
			// hardest case: we need arbitrary lookahead to detect var/property/method
			InputPosition = wordsStartAt;
			using (new SavePosition(this, 0))
			{
				TryMatch((int)TT.This); // skip 'this' attribute for extension methods
										// Use MethodOrPropertyName to detect all possible method, property, 
										// and variable declarations (this incidentally matches some invalid 
										// things like `bool operator true { get {} }`.)
				if (Scan_DataType(false) && Scan_MethodOrPropertyName(true) && ExpectedAfterTypeAndName[(int)mode].Contains(LA0))
					return StmtCat.MethodOrPropOrVar;
			}
			if (haveNew)
			{
				if (LA(-1) == TT.New)
					InputPosition--;
				else {
					// count 'new' as a word attribute, to trigger an error if it shouldn't be there
					wordAttrCount++;
				}
			}
			return consecutiveWords != 0 ? StmtCat.IdStmt : StmtCat.OtherStmt;
		}

		#endregion

		#region Methods/properties of LLLPG and base class overrides

		protected sealed override int EofInt() { return 0; }
		protected sealed override int LA0Int { get { return _lt0.TypeInt; } }
		protected sealed override Token LT(int i)
		{
			bool fail;
			return _tokens.TryGet(InputPosition + i, out fail);
		}

		protected override string ToString(int type_)
		{
			var type = (TokenType)type_;
			return type.ToString();
		}

		protected TT LA0 { [DebuggerStepThrough] get { return _lt0.Type(); } }
		protected TT LA(int i)
		{
			bool fail;
			return _tokens.TryGet(InputPosition + i, out fail).Type();
		}
		new const TokenType EOF = TT.EOF;

		#endregion

		#region Error handling

		UString CurrentTokenText()
		{
			return LT(0).SourceText(SourceFile.Text);
		}

		protected override void Error(int lookaheadIndex, string message)
		{
			Error(lookaheadIndex, message, EmptyArray<object>.Value);
		}
		protected override void Error(int lookaheadIndex, string message, params object[] args)
		{
			int iPos = GetTextPosition(InputPosition + lookaheadIndex);
			SourceRange pos = new SourceRange(_sourceFile, iPos);
			CurrentSink(true).Error(pos, message, args);
		}
		protected LNode Error(string message, params object[] args)
		{
			Error(0, message, args);
			if (_tentative.DeferErrors)
				return F.Missing;
			else {
				if (args.Length > 0)
					message = string.Format(message, args);
				return F.Call(S.Error, F.Literal(message));
			}
		}
		protected void Error(LNode node, string message, params object[] args)
		{
			CurrentSink(true).Error(node, message, args);
		}
		protected void Error(Token token, string message, params object[] args)
		{
			CurrentSink(true).Error(token.Range(_sourceFile), message, args);
		}
		protected int GetTextPosition(int tokenPosition)
		{
			bool fail;
			var token = _tokens.TryGet(tokenPosition, out fail);
			if (!fail)
				return token.StartIndex;
			else if (_tokens.Count == 0 || tokenPosition < 0)
				return _startTextIndex;
			else
				return _tokens[_tokens.Count - 1].EndIndex;
		}
		public IMessageSink CurrentSink(bool incErrorCount)
		{
			if (incErrorCount)
				_tentative.LocalErrorCount++;
			if (_tentative.DeferErrors)
				return _tentative.DeferredErrors = _tentative.DeferredErrors ?? new MessageHolder();
			return base.ErrorSink;
		}
		
		#endregion

		#region Down & Up
		// These are used to traverse into token subtrees, e.g. given w=(x+y)*z, 
		// the outer token list is w=()*z, and the 3 tokens x+y are children of '('
		// So the parser calls something like Down(lparen) to begin parsing inside,
		// then it calls Up() to return to the parent tree.

		Stack<Pair<IListSource<Token>, int>> _parents = new Stack<Pair<IListSource<Token>, int>>();

		protected bool Down(int li)
		{
			return Down(LT(li).Children);
		}
		protected bool Down(IListSource<Token> children)
		{
			if (children != null) {
				_parents.Push(Pair.Create(_tokens, InputPosition));
				_tokens = children;
				InputPosition = 0;
				return true;
			}
			return false;
		}
		protected T Up<T>(T value)
		{
			Up();
			return value;
		}
		protected void Up()
		{
			Debug.Assert(_parents.Count > 0);
			var pair = _parents.Pop();
			_tokens = pair.A;
			InputPosition = pair.B;
		}
		
		#endregion

		#region Other parsing helpers: ExprListInside, etc.

		protected LNode SingleExprInside(Token group, string stmtType, bool allowUnassignedVarDecl = false)
		{
			VList<LNode> list = default(VList<LNode>);
			return SingleExprInside(group, stmtType, allowUnassignedVarDecl, ref list);
		}
		protected LNode SingleExprInside(Token group, string stmtType, bool allowUnassignedVarDecl, ref VList<LNode> list)
		{
			int oldCount = list.Count;
			list = AppendExprsInside(group, list, false, allowUnassignedVarDecl);
			if (list.Count != oldCount + 1)
			{
				if (list.Count <= oldCount) {
					LNode result = F.Id(S.Missing, group.StartIndex + 1, group.StartIndex + 1);
					list.Add(result);
					Error(result, "Missing expression inside '{0}'", stmtType);
					return result;
				} else {
					Error(list[1], "There should be only one expression inside '{0}'", stmtType);
					list.Resize(oldCount + 1);
				}
			}
			return list[0];
		}
		protected VList<LNode> AppendExprsInside(Token group, VList<LNode> list, bool allowTrailingComma = false, bool allowUnassignedVarDecl = false)
		{
			if (Down(group.Children)) {
				ExprList(ref list, allowTrailingComma, allowUnassignedVarDecl);
				return Up(list);
			}
			return list;
		}
		protected void AppendInitializersInside(Token group, ref VList<LNode> list)
		{
			if (Down(group.Children)) {
				InitializerList(ref list);
				Up();
			}
		}
		protected VList<LNode> ExprListInside(Token t, bool allowTrailingComma = false, bool allowUnassignedVarDecl = false)
		{
			return AppendExprsInside(t, new VList<LNode>(), allowTrailingComma, allowUnassignedVarDecl);
		}
		private VList<LNode> StmtListInside(Token t)
		{
			var list = new VList<LNode>();
			if (Down(t.Children)) {
				StmtList(ref list);
				return Up(list);
			}
			return list;
 		}
		private VList<LNode> InitializerListInside(Token t)
		{
			var list = VList<LNode>.Empty;
			AppendInitializersInside(t, ref list);
			return list;
 		}

		// Counts the number of array dimensions, e.g. [] => 1, [,,] => 3
		private int CountDims(Token token, bool allowNonCommas)
		{
			if (token.Type() != TT.LBrack)
				return -1;
			var children = token.Children;
			if (children == null)
				return 1;
			else {
				int commas = 0;
				for (int i = 0; i < children.Count; i++)
					if (children[i].Type() == TT.Comma)
						commas++;
					else if (!allowNonCommas)
						return -1;
				return commas + 1;
			}
		}

		#endregion

		#region Infix precedence table

		// Use "int" rather than Precedence because enum keys are slow in Dictionary
		// Note: << and >> are not listed because they are represented by two tokens
		static readonly Dictionary<int, Precedence> _infixPrecedenceTable = new Dictionary<int, Precedence> {
			{ (int)TT.Dot, EP.Primary },
			{ (int)TT.ColonColon, EP.Primary },
			{ (int)TT.QuickBind, EP.Primary },
			{ (int)TT.PtrArrow, EP.Primary },
			{ (int)TT.NullDot, EP.NullDot },
			{ (int)TT.Power, EP.Power },
			{ (int)TT.Mul, EP.Multiply },
			{ (int)TT.DivMod, EP.Multiply },
			{ (int)TT.Add, EP.Add },
			{ (int)TT.Sub, EP.Add },
			{ (int)TT.NotBits, EP.Add },
			{ (int)TT.DotDot, EP.Range },
			{ (int)TT.BQString, EP.Backtick },
			{ (int)TT.Backslash, EP.Backtick },
			{ (int)TT.LT, EP.Compare },
			{ (int)TT.GT, EP.Compare },
			{ (int)TT.LEGE, EP.Compare },
			{ (int)TT.Is, EP.Compare },
			{ (int)TT.As, EP.Compare },
			{ (int)TT.Using, EP.Compare },
			{ (int)TT.EqNeq, EP.Equals },
			{ (int)TT.In, EP.Equals },
			{ (int)TT.AndBits, EP.AndBits },
			{ (int)TT.XorBits, EP.XorBits },
			{ (int)TT.OrBits, EP.OrBits },
			{ (int)TT.And, EP.And },
			{ (int)TT.OrXor, EP.Or },
			{ (int)TT.NullCoalesce, EP.OrIfNull },
			{ (int)TT.QuestionMark, EP.IfElse }, // yeah, not really infix
			{ (int)TT.Set, EP.Assign },
			{ (int)TT.CompoundSet, EP.Assign },
			{ (int)TT.LambdaArrow, EP.Lambda },
		};
		static Precedence InfixPrecedenceOf(TokenType la)
		{
			return _infixPrecedenceTable[(int)la];
		}

		#endregion
	}

	// +-------------------------------------------------------------+
	// | (allowed as attrs.) | (not allowed as keyword attributes)   |
	// | Modifier keywords   | Statement names | Types** | Other***  |
	// |:-------------------:|:---------------:|:-------:|:---------:|
	// | abstract            | break           | bool    | operator  |
	// | const               | case            | byte    | sizeof    |
	// | explicit            | checked         | char    | typeof    |
	// | extern              | class           | decimal |:---------:|
	// | implicit            | continue        | double  | else      |
	// | internal            | default         | float   | catch     |
	// | new*                | delegate        | int     | finally   |
	// | override            | do              | long    |:---------:|
	// | params              | enum            | object  | in        |
	// | private             | event           | sbyte   | as        |
	// | protected           | fixed           | short   | is        |
	// | public              | for             | string  |:---------:|
	// | readonly            | foreach         | uint    | base      |
	// | ref                 | goto            | ulong   | false     |
	// | sealed              | if              | ushort  | null      |
	// | static              | interface       | void    | true      |
	// | unsafe              | lock            |         | this      |
	// | virtual             | namespace       |         |:---------:|
	// | volatile            | return          |         | stackalloc|
	// | out*                | struct          |         |           |
	// |                     | switch          |         |           |
	// |                     | throw           |         |           |
	// |                     | try             |         |           |
	// |                     | unchecked       |         |           |
	// |                     | using           |         |           |
	// |                     | while           |         |           |
	// +-------------------------------------------------------------+
}
