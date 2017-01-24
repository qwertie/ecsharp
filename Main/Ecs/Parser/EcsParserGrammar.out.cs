// Generated from EcsParserGrammar.les by LeMP custom tool. LeMP version: 2.5.0.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Loyc.Syntax.Lexing;
using Loyc.Collections;
using Loyc.Collections.Impl;

namespace Loyc.Ecs.Parser
{
	using TT = TokenType;
	using S = CodeSymbols;
	using EP = EcsPrecedence;

	// 0162=Unreachable code detected; 0642=Possibly mistaken empty statement
	
	#pragma warning disable 162, 642
	

	partial class EcsParser {
		static readonly Symbol sy_await = (Symbol) "await", sy_from = (Symbol) "from", sy_into = (Symbol) "into", sy_let = (Symbol) "let", sy_where = (Symbol) "where", sy_join = (Symbol) "join", sy_orderby = (Symbol) "orderby", sy_select = (Symbol) "select", sy_on = (Symbol) "on", sy_equals = (Symbol) "equals", sy__numequals = (Symbol) "#equals", sy_ascending = (Symbol) "ascending", sy_descending = (Symbol) "descending", sy_group = (Symbol) "group", sy_by = (Symbol) "by", sy_trait = (Symbol) "trait", sy_alias = (Symbol) "alias", sy_assembly = (Symbol) "assembly", sy_module = (Symbol) "module", sy_when = (Symbol) "when";
		// Used to resolve the constructor ambiguity, in which "Foo()" could be a
		// constructor declaration or a method call. _spaceName is the name of the
		// current space, or #fn (S.Fn) when inside a method or constructor.
		Symbol _spaceName;
	
		// Inside a LINQ expression, certain ContextualKeywords are to be treated
		// as actual keywords. This flag enables that behavior.
		bool _insideLinqExpr;
	
		// Used to detect a specific ContextualKeyword; `@` renders it not-a-keyword
		bool Is(int li, Symbol value)
		{
			var lt = LT(li);
			return lt.Value == value && SourceFile.Text.TryGet(lt.StartIndex, '\0') != '@';
		}
	
		// ---------------------------------------------------------------------
		// -- Type names and complex identifiers -------------------------------
		// ---------------------------------------------------------------------
		// The complex identifier in EC# is a subset of the language of expressions,
		// and data types are a superset of the language of complex identifiers.
		// Complex identifiers can appear in the following contexts:
		// - Space names: namespace Foo.Bar<$T> {...}
		//   (and yes, I want to support template parameters on namespaces someday)
		// - Method/property names: bool global::System.IEquatable<T>.Equals(T other)
		// Data types can appear in the following contexts:
		// - Fields and properties: int* x { get; set; }
		// - Methods declarations: int[] f(Foo<T>[,][] x);
		// - Certain pseudo-functions: typeof(Foo<T[]>)
		// Note that complex identifiers can contain substitution expressions,
		// which, in turn, can contain expressions of any complexity, e.g. 
		// Foo<$(x*y(++z))>. Of course, complex identifiers also appear within 
		// normal expressions, but the expression grammar doesn't use the
		// main "ComplexId" rule, instead it's handled somewhat separately.
		// LLLPG is unaware of this overload, but when I write DataType(),
		// it unwittingly invokes it.
		public LNode DataType(bool afterAsOrIs = false) {
			Token? brack;
			var type = DataType(afterAsOrIs, out brack);
			if ((brack != null)) {
				Error("A type name cannot include [array dimensions]. The square brackets should be empty.");
			}
			return type;
		}
	
		static LNode AutoRemoveParens(LNode node)
		{
			int i = node.Attrs.IndexWithName(S.TriviaInParens);
			if ((i > -1)) {
				return node.WithAttrs(node.Attrs.RemoveAt(i));
			}
			return node;
		}
	
		LNode ComplexNameDecl() { bool _; return ComplexNameDecl(false, out _); }
	
		int count;	// hack allows Scan_TypeSuffixOpt() to compile
		// ---------------------------------------------------------------------
		// -- Expressions ------------------------------------------------------
		// ---------------------------------------------------------------------
		// Parsing EC# expressions is very tricky. Here are some of the things 
		// that make it difficult, especially in an LL(k) parser:
		//    
		// 1. Parenthesis: is it a cast or a normal expression?
		//    (A<B,C>)y      is a cast
		//    (A<B,C>)-y     is NOT a cast
		//    (A<B,C>)(-y)   is a cast
		//    (A<B,C>)(as B) is NOT a cast (well, the second part is)
		//    x(A<B,C>)(-y)  is NOT a cast
		//    -(A<B,C>)(-y)  is a cast
		//    $(A<B,C>)(-y)  is NOT a cast
		//    (A<B,C>){y}    is a cast
		//    (A<B,C>)[y]    is NOT a cast
		//    (A<B,C>)++(y)  is NOT a cast (it's post-increment and call)
		//    (A<B> C)(-y)   is NOT a cast
		//    ([] A<B,C>)(y) is NOT a cast (empty attr list defeats cast)
		//    (int)*y        is a cast
		//    (A<B,C>)*y     is NOT a cast
		//    (A<B,C>)&y     is NOT a cast
		//    (A+B==C)y      is nonsensical
		//    x(A<B,C>)y     is nonsensical
		// 2. In-expr var declarations: is "(A<B,C>D)" a variable declaration?
		//    (A<B,C>D) => D.Foo() // variable declaration
		//    (A<B,C>D) = Foo()    // variable declaration
		//    (f1, f2) = (A<B,C>D) // tuple
		//    Foo(A<B,C>D)         // method with two arguments
		//    void Foo(A<B,C>D)    // variable declaration at statement level
		//    Foo(A<B,C>D)         // variable declaration at statement level if 'Foo' is the space name
		//                         // (i.e. when 'Foo' is a constructor)
		// 3. Less-than: is it a generics list or an operator? Need unlimited lookahead.
		//    (A<B,C)    // less-than operator
		//    (A<B,C>)   // generics list
		//    (A<B,C>D)  // context-dependent
		// 4. Brackets. Is "Foo[]" a data type, or an indexer with no args?
		//    (Foo[]) x; // data type: #of(#[], Foo)
		//    (Foo[]).x; // indexer:   #[](Foo)
		// 5. Does "?" make a nullable type or a conditional operator?
		//    Foo<B> ? x = null;     // nullable type
		//    Foo<B> ? x = null : y; // conditional operator
		/// Below lowest precedence
		public static readonly Precedence StartExpr = new Precedence(-100);
	
		LNode TypeInside(Token args)
		{
			if ((!Down(args)))
				return F.Id(S.Missing, args.EndIndex, args.EndIndex);
			var type = DataType();
			Match((int) EOF);
			return Up(type);
		}
	
		LNode SetOperatorStyle(LNode node)
		{
			return node.SetBaseStyle(NodeStyle.Operator);
		}
		LNode SetAlternateStyle(LNode node)
		{
			node.Style |= NodeStyle.Alternate;
			return node;
		}
	
		// Parses an expression (TentativeExpr) or variable declaration (TentativeVarDecl).
		// Returns the parsed node on success or null if outer-level parse error(s) 
		// occurred; the out param result is never null, and in case of success it 
		// is the same as the return value. Error handling is tricky... we fail if
		// there are errors at the current level, not if there are errors in 
		// parenthesized subexpressions.
		LNode TentativeExpr(VList<LNode> attrs, out TentativeResult result, bool allowUnassigned = false)
		{
			result = new TentativeResult(InputPosition);
			var oldState = _tentative;
			_tentative = new TentativeState(true);
			{
				try {
					bool failed = false;
					result.Result = SubExpr(StartExpr).PlusAttrs(attrs);
					if ((LA0 != EOF && LA0 != TT.Semicolon && LA0 != TT.Comma)) {
						failed = true;
					}
					result.Errors = _tentative.DeferredErrors;
					result.InputPosition = InputPosition;
					if (failed || _tentative.LocalErrorCount != 0) {
						// error(s) occurred.
						InputPosition = result.OldPosition;
						return null;
					}
				} finally {
					_tentative = oldState;
				}
			
			}
			return Apply(result);	// must Apply after finally block
		}
		// Parses an expression (TentativeExpr) or variable declaration (TentativeVarDecl).
		// Returns the parsed node on success or null if outer-level parse error(s) 
		// occurred; the out param result is never null, and in case of success it 
		// is the same as the return value. Error handling is tricky... we fail if
		// there are errors at the current level, not if there are errors in 
		// parenthesized subexpressions.
		LNode TentativeVarDecl(VList<LNode> attrs, out TentativeResult result, bool allowUnassigned = false)
		{
			result = new TentativeResult(InputPosition);
			var oldState = _tentative;
			_tentative = new TentativeState(true);
			{
				try {
					bool failed = false;
					int _;
					var cat = DetectStatementCategoryAndAddWordAttributes(out _, ref attrs, DetectionMode.Expr);
					if ((cat != StmtCat.MethodOrPropOrVar)) {
						failed = true;
						result.Result = F.Missing;
					} else {
						bool hasInitializer;
						result.Result = VarDeclExpr(out hasInitializer, attrs);
						if ((!hasInitializer && !allowUnassigned)) {
							Error(-1, "An unassigned variable declaration is not allowed in this context");
						}
					}
					result.Errors = _tentative.DeferredErrors;
					result.InputPosition = InputPosition;
					if (failed || _tentative.LocalErrorCount != 0) {
						// error(s) occurred.
						InputPosition = result.OldPosition;
						return null;
					}
				} finally {
					_tentative = oldState;
				}
			
			}
			return Apply(result);	// must Apply after finally block
		}
		LNode Apply(TentativeResult result)
		{
			InputPosition = result.InputPosition;
			if (result.Errors != null) {
				result.Errors.WriteListTo(CurrentSink(false));
			}
			return result.Result;
		}
	
		static readonly Symbol _var = GSymbol.Get("var");
	
		private void MaybeRecognizeVarAsKeyword(ref LNode type)
		{
			// Recognize "var" (but not @var) as a contextual keyword.
			SourceRange rng;
			Symbol name = type.Name;
			if ((name == _var) && type.IsId 
			&& (rng = type.Range).Source.Text.TryGet(rng.StartIndex, '\0') != '@') {
				type = type.WithName(S.Missing);
			}
		}
	
		bool IsNamedArg(LNode node) { return node.Calls(S.NamedArg, 2) && node.BaseStyle == NodeStyle.Operator; }
	
		// =====================================================================
		// == Statements =======================================================
		// =====================================================================
		WList<LNode> _stmtAttrs = new WList<LNode>();
	
		// Statement categories distinguished by DetectStatementCategory
		enum StmtCat {
			MethodOrPropOrVar = 0	// method, property, or variable declaration 
			,
			KeywordStmt = 1	// declarative like struct/enum/event, or executable like while/for/try 
			,
			IdStmt = 2	// a statement that starts with an Id and doesn't allow keyword attributes;
			,
			// in certain cases it's also used for stmts that start with "$" or a type keyword
			ThisConstructor = 3	// a constructor named 'this'
			,
			OtherStmt = 4	// everything else (word attributes not allowed)
		}
		// Tweak detection behavior depending on context
		enum DetectionMode {
			Expr = 0,
			Stmt = 1
		}
	
		static readonly HashSet<TT> ExpectedAfterTypeAndName_InStmt = new HashSet<TT> { 
			TT.Set, TT.LBrace, TT.LParen, TT.LBrack, TT.LambdaArrow, TT.Forward, TT.Semicolon, TT.Comma, TT.At
		};
		static readonly HashSet<TT> ExpectedAfterTypeAndName_InExpr = new HashSet<TT> { 
			TT.Set, TT.LBrace, TT.LambdaArrow, TT.Forward, TT.Semicolon, TT.Comma, TT.EOF, TT.At
		};
		static readonly HashSet<TT>[] ExpectedAfterTypeAndName = new HashSet<TT>[] { 
			ExpectedAfterTypeAndName_InExpr, ExpectedAfterTypeAndName_InStmt
		};
		static readonly HashSet<TT> EasilyDetectedKeywordStatements = new HashSet<TT> { 
			TT.Namespace, TT.Class, TT.Struct, TT.Interface, TT.Enum, TT.Event, TT.Switch, TT.Case, TT.Using, TT.While, TT.Fixed, TT.For, TT.Foreach, TT.Goto, TT.Lock, TT.Return, TT.Try, TT.Do, TT.Continue, TT.Break, TT.Throw, TT.If
		};
	
		StmtCat DetectStatementCategoryAndAddWordAttributes(out int wordAttrCount, ref VList<LNode> attrs, DetectionMode mode)
		{
			// (This is also called if a variable declaration is suspected in an 
			// expression. In this case, detecting the statement category isnt
			// the main goal; rather, the main goal is figuring out where the
			// word attributes end.)
			var oldPosition = InputPosition;
			var cat = DetectStatementCategory(out wordAttrCount, mode);
		
			// Add word attributes, if any
			for (; oldPosition < InputPosition; oldPosition++) {
				Token word = _tokens[oldPosition];
				LNode wordAttr;
				if ((word.Kind == TokenKind.AttrKeyword || word.Type() == TT.New)) {
					wordAttr = F.Id(word);
				} else {
					wordAttr = F.Attr(_triviaWordAttribute, F.Id("#" + word.Value.ToString(), word.StartIndex, word.EndIndex));
				}
				attrs.Add(wordAttr);
			}
			return cat;
		}
	
		// Originally I let LLLPG distinguish the statement types, but when the 
		// parser was mature, LLLPG's analysis took from 10 to 40 seconds and it 
		// generated 5000 lines of code to distinguish the various cases.
		// This custom code, which was very tricky to get right,
		// (1) detects method/property/variable (and lookalikes: trait, alias) and
		//     distinguishes certain other cases (StmtCat values) out of convenience.
		// (2) detects and skips word attributes (not including `this` in `this int x`).
		// (3) is fast if possible.
		//
		// "word attributes" are a messy feature of EC#. C# has a few non-keyword 
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
		StmtCat DetectStatementCategory(out int wordAttrCount, DetectionMode mode)
		{
			// If the statement starts with identifier(s), we have to figure out 
			// whether, and how many of them, are word attributes. Also, skip over
			// `new` because can be a keyword attribute in some cases.
			wordAttrCount = 0;
			int wordsStartAt = InputPosition;
			bool haveNew = LA0 == TT.New;	// "new" keyword is the most annoying wrinkle
			if ((haveNew || LA0 == TT.Id || LA0 == TT.ContextualKeyword || LA0 == TT.LinqKeyword))
			{
				if ((!haveNew)) {
					// Optimized path for common expressions that start with an Id (IdStmts)
					// first lets get it working without optimization
				/*var la1k = LT(1).Kind;
					if la1k == TokenKind.LParen || la1k == TokenKind.Assignment ||
					   la1k == TokenKind.Separator || la1k->int == TT.EOF->int {
						return StmtCat.IdStmt;
					} else if (la1k == TokenKind.Operator) {
						var la1 = LA(1);
						if la1 != TT.QuestionMark && la1 != TT.LT && la1 != TT.Mul { 
							return StmtCat.IdStmt;
						};
					};*/ }
			
				// Scan past identifiers and extra AttrKeywords at beginning of statement
				bool isAttrKw = haveNew;
				do {
					Skip();
					if ((isAttrKw)) {
						wordsStartAt = InputPosition;
					} else {
						wordAttrCount++;
					}
					haveNew |= (isAttrKw = (LA0 == TT.New));
				} while (((isAttrKw |= LA0 == TT.AttrKeyword || LA0 == TT.New) || LA0 == TT.Id || LA0 == TT.ContextualKeyword || LA0 == TT.LinqKeyword)
				);
			}
		
			// At this point we've skipped over all simple identifiers.
			// Now decide: what kind of statement do we appear to have?
			int consecutive = InputPosition - wordsStartAt;
			if ((LA0 == TT.TypeKeyword)) {
				// We can treat this as if it were one additional identifier, 
				// although it cannot be treated as a word attribute.
				InputPosition++;
				consecutive++;
			} else if ((LA0 == TT.Substitute)) {
				// We can treat this as if it were one additional identifier, 
				// although it cannot be treated as a word attribute.
				var la1 = LA(1);
				if ((LA(1) == TT.LParen && LA(2) == TT.RParen)) {
					InputPosition += 3;
				} else {
					InputPosition++;
				}
				consecutive++;
			} else if ((LA0 == TT.This)) {
				if (LA(1) == TT.LParen && LA(2) == TT.RParen) {
					TT la3 = LA(3);
					if (la3 == TT.Colon || la3 == TT.LBrace || la3 == TT.Semicolon && _spaceName != S.Fn) {
						return StmtCat.ThisConstructor;
					} else {
						return StmtCat.OtherStmt;
					}
				} else if ((consecutive != 0)) {
					// Appears to be a this[] property (there could be a type 
					// param, like this<T>[], so we can't check if LA(1) is "[")
					InputPosition--;
					return StmtCat.MethodOrPropOrVar;
				}
			} else if ((LT0.Kind == TokenKind.OtherKeyword)) {
				if ((EasilyDetectedKeywordStatements.Contains(LA0) || LA0 == TT.Delegate && LA(1) != TT.LParen || (LA0 == TT.Checked || LA0 == TT.Unchecked) && LA(1) == TT.LBrace)
				)
			
				{
					// `if` and `using` do not support word attributes:
					// - `if`, because in the original plan EC# was to support a 
					//   D-style 'if' clause at the end of property definitions, 
					//   making "T P if ..." ambiguous if word attributes were allowed.
					// - `using` because `x using T` was planned as a new cast operator.
					if (!(consecutive > 0 && (LA0 == TT.If || LA0 == TT.Using))) {
						return StmtCat.KeywordStmt;
					}
				}
			} else if ((consecutive == 0)) {
				if ((!haveNew)) {
					return StmtCat.OtherStmt;
				}
			}
		
			// At this point we know it's not a "keyword statement" or "this constructor",
			// so it's either MethodOrPropOrVar, which allows word attributes, or 
			// something else that prohibits them (IdStmt or OtherStmt).
			if (consecutive >= 2) {
				// We know it's MethodOrPropOrVar, but where do the word attributes end?
				int likelyStart = wordsStartAt + consecutive - 2;	// most likely location
				if ((ExpectedAfterTypeAndName[(int) mode].Contains(LA0))) {
					InputPosition = likelyStart;
				} else {
					// We must distinguish among these three cases:
					// 1. IEnumerator IEnumerable.GetEnumerator()
					//    ^likelyStart(correct)  ^InputPosition
					// 2. alias              Map<K,V> = Dictionary <K,V>;
					//    ^likelyStart(correct) ^InputPosition
					// 3. partial    Namespace.Class Method()
					//    ^likelyStart(too low) ^InputPosition
					InputPosition = likelyStart + 1;
				
					if ((Scan_ComplexNameDecl() && ExpectedAfterTypeAndName[(int) mode].Contains(LA0))) {
						InputPosition = likelyStart;
					} else {
						InputPosition = likelyStart + 1;
					}
				}
				return StmtCat.MethodOrPropOrVar;
			}
		
			// Worst case: need arbitrary lookahead to detect var/property/method
			InputPosition = wordsStartAt;
			using (new SavePosition(this, 0)) {
				TryMatch((int) TT.This);	// skip 'this' attribute for extension methods
				if ((Scan_DataType(false) && Scan_ComplexNameDecl() && ExpectedAfterTypeAndName[(int) mode].Contains(LA0))) {
					return StmtCat.MethodOrPropOrVar;
				}
			}
			if ((haveNew)) {
				if ((LA(-1) == TT.New)) {
					InputPosition--;
				} else {
					// count 'new' as a word attribute, to trigger an error if it shouldn't be there
					wordAttrCount++;
				}
			}
			return consecutive != 0 ? StmtCat.IdStmt : StmtCat.OtherStmt;
		}
	
		void NonKeywordAttrError(IList<LNode> attrs, string stmtType)
		{
			var attr = attrs.FirstOrDefault(a => a.AttrNamed(S.TriviaWordAttribute) != null);
			if ((attr != null)) {
				Error(attr, "'{0}' appears to be a word attribute, which is not permitted before '{1}'", attr.Range.SourceText, stmtType);
			}
		}
	
		Symbol TParamSymbol(LNode T)
		{
			if (T.IsId)
				return T.Name;
			else if (T.Calls(S.Substitute, 1) && T.Args[0].IsId)
				return T.Args[0].Name;
			else
				return S.Missing;
		
		
		
		}
	
		private LNode MethodBodyOrForward() { LNode _; return MethodBodyOrForward(false, out _); }
		bool IsArrayType(LNode type)
		{
			// Detect an array type, which has the form #of(#[,], Type)
			return type.Calls(S.Of, 2) && S.IsArrayKeyword(type.Args[0].Name);
		}
	
		LNode ArgList(Token lp, Token rp)
		{
			var list = new VList<LNode>();
			if ((Down(lp.Children))) {
				ArgList(ref list);
				Up();
			}
			return F.Call(S.AltList, list, lp.StartIndex, rp.EndIndex, lp.StartIndex, lp.StartIndex + 1);
		}
	
		int ColumnOf(int index)
		{
			return _sourceFile.IndexToLine(index).PosInLine;
		}
	
		LNode MissingHere()
		{
			var i = GetTextPosition(InputPosition);
			return F.Id(S.Missing, i, i);
		}
	
		// A potential LINQ keyword that, it turns out, can be treated as an identifier
		private Token LinqKeywordAsId()
		{
			Check(!_insideLinqExpr, "Did not expect _insideLinqExpr");
			var t = Match((int) TT.LinqKeyword);
			// line 105
			return t;
		}
	
		// A potential LINQ keyword that, it turns out, can be treated as an identifier
		private bool Scan_LinqKeywordAsId()
		{
			if (_insideLinqExpr)
				return false;
			if (!TryMatch((int) TT.LinqKeyword))
				return false;
			return true;
		}
	
	
		LNode DataType(bool afterAsOrIs, out Token? majorDimension)
		{
			LNode result = default(LNode);
			result = ComplexId();
			TypeSuffixOpt(afterAsOrIs, out majorDimension, ref result);
			return result;
		}
	
	
		bool Try_Scan_DataType(int lookaheadAmt, bool afterAsOrIs = false) {
			using (new SavePosition(this, lookaheadAmt))
				return Scan_DataType(afterAsOrIs);
		}
		bool Scan_DataType(bool afterAsOrIs = false)
		{
			if (!Scan_ComplexId())
				return false;
			if (!Scan_TypeSuffixOpt(afterAsOrIs))
				return false;
			return true;
		}
	
		// Complex identifier in non-declaration context, e.g. Foo.Bar or Foo<x, y>
		// http://loyc-etc.blogspot.ca/2013/12/bogus-ambiguity-warnings-in-lllpg.html
		LNode ComplexId(bool declContext = false)
		{
			TokenType la0;
			LNode result = default(LNode);
			result = IdWithOptionalTypeParams(declContext);
			// Line 150: (TT.ColonColon IdWithOptionalTypeParams)?
			la0 = LA0;
			if (la0 == TT.ColonColon) {
				switch (LA(1)) {
				case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
				case TT.Substitute: case TT.TypeKeyword:
					{
						// line 150
						if ((result.Calls(S.Of))) {
							Error("Type parameters cannot appear before '::' in a declaration or type name");
						}
						var op = MatchAny();
						var rhs = IdWithOptionalTypeParams(declContext);
						// line 152
						result = F.Call(S.ColonColon, result, rhs, result.Range.StartIndex, rhs.Range.EndIndex, op.StartIndex, op.EndIndex, NodeStyle.Operator);
					}
					break;
				}
			}
			// Line 154: (TT.Dot IdWithOptionalTypeParams)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Dot) {
					switch (LA(1)) {
					case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
					case TT.Substitute: case TT.TypeKeyword:
						{
							var op = MatchAny();
							var rhs = IdWithOptionalTypeParams(declContext);
							// line 155
							result = F.Dot(result, rhs, result.Range.StartIndex, rhs.Range.EndIndex, op.StartIndex, op.EndIndex, NodeStyle.Operator);
						}
						break;
					default:
						goto stop;
					}
				} else
					goto stop;
			}
		stop:;
			return result;
		}
	
		// Complex identifier in non-declaration context, e.g. Foo.Bar or Foo<x, y>
		// http://loyc-etc.blogspot.ca/2013/12/bogus-ambiguity-warnings-in-lllpg.html
		bool Scan_ComplexId(bool declContext = false)
		{
			TokenType la0;
			if (!Scan_IdWithOptionalTypeParams(declContext))
				return false;
			// Line 150: (TT.ColonColon IdWithOptionalTypeParams)?
			la0 = LA0;
			if (la0 == TT.ColonColon) {
				switch (LA(1)) {
				case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
				case TT.Substitute: case TT.TypeKeyword:
					{
						if (!TryMatch((int) TT.ColonColon))
							return false;
						if (!Scan_IdWithOptionalTypeParams(declContext))
							return false;
					}
					break;
				}
			}
			// Line 154: (TT.Dot IdWithOptionalTypeParams)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Dot) {
					switch (LA(1)) {
					case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
					case TT.Substitute: case TT.TypeKeyword:
						{
							if (!TryMatch((int) TT.Dot))
								return false;
							if (!Scan_IdWithOptionalTypeParams(declContext))
								return false;
						}
						break;
					default:
						goto stop;
					}
				} else
					goto stop;
			}
		stop:;
			return true;
		}
	
	
		LNode IdWithOptionalTypeParams(bool declarationContext)
		{
			TokenType la0, la1;
			LNode result = default(LNode);
			result = IdAtom();
			// Line 161: (TParams)?
			la0 = LA0;
			if (la0 == TT.LT) {
				switch (LA(1)) {
				case TT.AttrKeyword: case TT.ContextualKeyword: case TT.GT: case TT.Id:
				case TT.In: case TT.LBrack: case TT.LinqKeyword: case TT.Operator:
				case TT.Substitute: case TT.TypeKeyword:
					TParams(declarationContext, ref result);
					break;
				}
			} else if (la0 == TT.Dot) {
				la1 = LA(1);
				if (la1 == TT.LBrack)
					TParams(declarationContext, ref result);
			} else if (la0 == TT.Not) {
				switch (LA(1)) {
				case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.LParen:
				case TT.Operator: case TT.Substitute: case TT.TypeKeyword:
					TParams(declarationContext, ref result);
					break;
				}
			}
			return result;
		}
	
	
		bool Scan_IdWithOptionalTypeParams(bool declarationContext)
		{
			TokenType la0, la1;
			if (!Scan_IdAtom())
				return false;
			// Line 161: (TParams)?
			do {
				la0 = LA0;
				if (la0 == TT.LT) {
					switch (LA(1)) {
					case TT.AttrKeyword: case TT.ContextualKeyword: case TT.GT: case TT.Id:
					case TT.In: case TT.LBrack: case TT.LinqKeyword: case TT.Operator:
					case TT.Substitute: case TT.TypeKeyword:
						goto matchTParams;
					}
				} else if (la0 == TT.Dot) {
					la1 = LA(1);
					if (la1 == TT.LBrack)
						goto matchTParams;
				} else if (la0 == TT.Not) {
					switch (LA(1)) {
					case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.LParen:
					case TT.Operator: case TT.Substitute: case TT.TypeKeyword:
						goto matchTParams;
					}
				}
				break;
			matchTParams:
				{
					if (!Scan_TParams(declarationContext))
						return false;
				}
			} while (false);
			return true;
		}
	
	
		// identifier, $identifier, $(expr), or primitive type (int, string)
		LNode IdAtom()
		{
			// line 166
			LNode r;
			// Line 167: ( TT.Substitute Atom | TT.Operator AnyOperator | (TT.ContextualKeyword|TT.Id|TT.TypeKeyword) | LinqKeywordAsId )
			switch (LA0) {
			case TT.Substitute:
				{
					var t = MatchAny();
					var e = Atom();
					e = AutoRemoveParens(e);
					r = F.Call(S.Substitute, e, t.StartIndex, e.Range.EndIndex, t.StartIndex, t.EndIndex, NodeStyle.Operator);
				}
				break;
			case TT.Operator:
				{
					var op = MatchAny();
					var t = AnyOperator();
					// line 170
					r = F.Attr(_triviaUseOperatorKeyword, F.Id((Symbol) t.Value, op.StartIndex, t.EndIndex));
				}
				break;
			case TT.ContextualKeyword: case TT.Id: case TT.TypeKeyword:
				{
					var t = MatchAny();
					// line 172
					r = F.Id(t);
				}
				break;
			default:
				{
					var t = LinqKeywordAsId();
					// line 174
					r = F.Id(t);
				}
				break;
			}
			// line 176
			return r;
		}
	
		// identifier, $identifier, $(expr), or primitive type (int, string)
		bool Scan_IdAtom()
		{
			// Line 167: ( TT.Substitute Atom | TT.Operator AnyOperator | (TT.ContextualKeyword|TT.Id|TT.TypeKeyword) | LinqKeywordAsId )
			switch (LA0) {
			case TT.Substitute:
				{
					if (!TryMatch((int) TT.Substitute))
						return false;
					if (!Scan_Atom())
						return false;
				}
				break;
			case TT.Operator:
				{
					if (!TryMatch((int) TT.Operator))
						return false;
					if (!Scan_AnyOperator())
						return false;
				}
				break;
			case TT.ContextualKeyword: case TT.Id: case TT.TypeKeyword:
				if (!TryMatch((int) TT.ContextualKeyword, (int) TT.Id, (int) TT.TypeKeyword))
					return false;
				break;
			default:
				if (!Scan_LinqKeywordAsId())
					return false;
				break;
			}
			return true;
		}
	
	
		// List of type parameters. `declContext` specifies that type parameters can 
		// have [normal attributes] and in/out variance attrbutes.
		void TParams(bool declContext, ref LNode r)
		{
			TokenType la0, la1;
			Token op = default(Token);
			VList<LNode> list = new VList<LNode>(r);
			int endIndex;
			// Line 195: ( TT.LT (TParamDeclOrDataType (TT.Comma TParamDeclOrDataType)*)? TT.GT | TT.Dot TT.LBrack TT.RBrack | TT.Not TT.LParen TT.RParen | TT.Not IdWithOptionalTypeParams )
			la0 = LA0;
			if (la0 == TT.LT) {
				op = MatchAny();
				// Line 195: (TParamDeclOrDataType (TT.Comma TParamDeclOrDataType)*)?
				switch (LA0) {
				case TT.AttrKeyword: case TT.ContextualKeyword: case TT.Id: case TT.In:
				case TT.LBrack: case TT.LinqKeyword: case TT.Operator: case TT.Substitute:
				case TT.TypeKeyword:
					{
						list.Add(TParamDeclOrDataType(declContext));
						// Line 196: (TT.Comma TParamDeclOrDataType)*
						for (;;) {
							la0 = LA0;
							if (la0 == TT.Comma) {
								Skip();
								list.Add(TParamDeclOrDataType(declContext));
							} else
								break;
						}
					}
					break;
				}
				var end = Match((int) TT.GT);
				// line 196
				endIndex = end.EndIndex;
			} else if (la0 == TT.Dot) {
				op = MatchAny();
				var t = Match((int) TT.LBrack);
				var end = Match((int) TT.RBrack);
				// line 197
				list = AppendExprsInside(t, list);
				endIndex = end.EndIndex;
			} else {
				la1 = LA(1);
				if (la1 == TT.LParen) {
					op = Match((int) TT.Not);
					var t = MatchAny();
					var end = Match((int) TT.RParen);
					// line 198
					list = AppendExprsInside(t, list);
					endIndex = end.EndIndex;
				} else {
					op = Match((int) TT.Not);
					list.Add(IdWithOptionalTypeParams(declContext));
					// line 199
					endIndex = list.Last.Range.EndIndex;
				}
			}
			// line 202
			int start = r.Range.StartIndex;
			r = F.Call(S.Of, list, start, endIndex, op.StartIndex, op.EndIndex, NodeStyle.Operator);
		}
	
	
		bool Try_Scan_TParams(int lookaheadAmt, bool declContext) {
			using (new SavePosition(this, lookaheadAmt))
				return Scan_TParams(declContext);
		}
		bool Scan_TParams(bool declContext)
		{
			TokenType la0, la1;
			// Line 195: ( TT.LT (TParamDeclOrDataType (TT.Comma TParamDeclOrDataType)*)? TT.GT | TT.Dot TT.LBrack TT.RBrack | TT.Not TT.LParen TT.RParen | TT.Not IdWithOptionalTypeParams )
			la0 = LA0;
			if (la0 == TT.LT) {
				if (!TryMatch((int) TT.LT))
					return false;
				// Line 195: (TParamDeclOrDataType (TT.Comma TParamDeclOrDataType)*)?
				switch (LA0) {
				case TT.AttrKeyword: case TT.ContextualKeyword: case TT.Id: case TT.In:
				case TT.LBrack: case TT.LinqKeyword: case TT.Operator: case TT.Substitute:
				case TT.TypeKeyword:
					{
						if (!Scan_TParamDeclOrDataType(declContext))
							return false;
						// Line 196: (TT.Comma TParamDeclOrDataType)*
						for (;;) {
							la0 = LA0;
							if (la0 == TT.Comma) {
								if (!TryMatch((int) TT.Comma))
									return false;
								if (!Scan_TParamDeclOrDataType(declContext))
									return false;
							} else
								break;
						}
					}
					break;
				}
				if (!TryMatch((int) TT.GT))
					return false;
			} else if (la0 == TT.Dot) {
				if (!TryMatch((int) TT.Dot))
					return false;
				if (!TryMatch((int) TT.LBrack))
					return false;
				if (!TryMatch((int) TT.RBrack))
					return false;
			} else {
				la1 = LA(1);
				if (la1 == TT.LParen) {
					if (!TryMatch((int) TT.Not))
						return false;
					if (!TryMatch((int) TT.LParen))
						return false;
					if (!TryMatch((int) TT.RParen))
						return false;
				} else {
					if (!TryMatch((int) TT.Not))
						return false;
					if (!Scan_IdWithOptionalTypeParams(declContext))
						return false;
				}
			}
			return true;
		}
	
		LNode TParamDeclOrDataType(bool declarationContext)
		{
			LNode result = default(LNode);
			VList<LNode> attrs = default(VList<LNode>);
			int startIndex = GetTextPosition(InputPosition);
			// Line 210: (DataType / &{declarationContext} NormalAttributes TParamAttributeKeywords IdAtom)
			switch (LA0) {
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
			case TT.Substitute: case TT.TypeKeyword:
				result = DataType(false);
				break;
			default:
				{
					Check(declarationContext, "Expected declarationContext");
					NormalAttributes(ref attrs);
					TParamAttributeKeywords(ref attrs);
					result = IdAtom();
				}
				break;
			}
			result = result.WithAttrs(attrs);
			return result;
		}
	
	
		bool Try_Scan_TParamDeclOrDataType(int lookaheadAmt, bool declarationContext) {
			using (new SavePosition(this, lookaheadAmt))
				return Scan_TParamDeclOrDataType(declarationContext);
		}
		bool Scan_TParamDeclOrDataType(bool declarationContext)
		{
			// Line 210: (DataType / &{declarationContext} NormalAttributes TParamAttributeKeywords IdAtom)
			switch (LA0) {
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
			case TT.Substitute: case TT.TypeKeyword:
				if (!Scan_DataType(false))
					return false;
				break;
			default:
				{
					if (!declarationContext)
						return false;
					if (!Scan_NormalAttributes())
						return false;
					if (!Scan_TParamAttributeKeywords())
						return false;
					if (!Scan_IdAtom())
						return false;
				}
				break;
			}
			return true;
		}
	
		// This is the same as ComplexId except that it is used in declaration locations
		// such as the name of a method. The difference is that the type parameters can 
		// have [normal attributes] and in/out variance attrbutes. In addition, 'this' is 
		// also allowed as a name if the current name is a property name.
		LNode ComplexNameDecl(bool thisAllowed, out bool hasThis)
		{
			TokenType la0, la1;
			LNode got_ComplexThisDecl = default(LNode);
			LNode result = default(LNode);
			// Line 225: (ComplexThisDecl | ComplexId (TT.Dot ComplexThisDecl)?)
			la0 = LA0;
			if (la0 == TT.This) {
				result = ComplexThisDecl(thisAllowed);
				// line 225
				hasThis = true;
			} else {
				result = ComplexId(declContext: true);
				// line 226
				hasThis = false;
				// Line 227: (TT.Dot ComplexThisDecl)?
				la0 = LA0;
				if (la0 == TT.Dot) {
					la1 = LA(1);
					if (la1 == TT.This) {
						var op = MatchAny();
						got_ComplexThisDecl = ComplexThisDecl(thisAllowed);
						hasThis = true;
						result = F.Dot(result, got_ComplexThisDecl, result.Range.StartIndex, got_ComplexThisDecl.Range.EndIndex, op.StartIndex, op.EndIndex, NodeStyle.Operator);
					}
				}
			}
			return result;
		}
	
	
		bool Try_Scan_ComplexNameDecl(int lookaheadAmt, bool thisAllowed = false) {
			using (new SavePosition(this, lookaheadAmt))
				return Scan_ComplexNameDecl(thisAllowed);
		}
		bool Scan_ComplexNameDecl(bool thisAllowed = false)
		{
			TokenType la0, la1;
			// Line 225: (ComplexThisDecl | ComplexId (TT.Dot ComplexThisDecl)?)
			la0 = LA0;
			if (la0 == TT.This){
				if (!Scan_ComplexThisDecl(thisAllowed))
					return false;}
			else {
				if (!Scan_ComplexId(declContext: true))
					return false;
				// Line 227: (TT.Dot ComplexThisDecl)?
				la0 = LA0;
				if (la0 == TT.Dot) {
					la1 = LA(1);
					if (la1 == TT.This) {
						if (!TryMatch((int) TT.Dot))
							return false;
						if (!Scan_ComplexThisDecl(thisAllowed))
							return false;
					}
				}
			}
			return true;
		}
	
		// `this` with optional <type arguments>
		LNode ComplexThisDecl(bool allowed)
		{
			TokenType la0;
			LNode result = default(LNode);
			// line 236
			if ((!allowed)) {
				Error("'this' is not allowed in this location.");
			}
			var t = Match((int) TT.This);
			// line 237
			result = F.Id(t);
			// Line 238: (TParams)?
			la0 = LA0;
			if (la0 == TT.Dot || la0 == TT.LT || la0 == TT.Not) {
				switch (LA(1)) {
				case TT.AttrKeyword: case TT.ContextualKeyword: case TT.GT: case TT.Id:
				case TT.In: case TT.LBrack: case TT.LinqKeyword: case TT.LParen:
				case TT.Operator: case TT.Substitute: case TT.TypeKeyword:
					TParams(true, ref result);
					break;
				}
			}
			return result;
		}
	
		// `this` with optional <type arguments>
		bool Scan_ComplexThisDecl(bool allowed)
		{
			TokenType la0;
			if (!TryMatch((int) TT.This))
				return false;
			// Line 238: (TParams)?
			la0 = LA0;
			if (la0 == TT.Dot || la0 == TT.LT || la0 == TT.Not) {
				switch (LA(1)) {
				case TT.AttrKeyword: case TT.ContextualKeyword: case TT.GT: case TT.Id:
				case TT.In: case TT.LBrack: case TT.LinqKeyword: case TT.LParen:
				case TT.Operator: case TT.Substitute: case TT.TypeKeyword:
					if (!Scan_TParams(true))
						return false;
					break;
				}
			}
			return true;
		}
	
	
		bool TypeSuffixOpt(bool afterAsOrIs, out Token? dimensionBrack, ref LNode e)
		{
			TokenType la0, la1;
			// line 246
			int count;
			bool result = false;
			dimensionBrack = null;
			// Line 279: greedy( TT.QuestionMark (&!{afterAsOrIs} | &!(((TT.Add|TT.AndBits|TT.At|TT.ContextualKeyword|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.New|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | LinqKeywordAsId))) | TT.Mul | &{(count = CountDims(LT($LI), @true)) > 0} TT.LBrack TT.RBrack greedy(&{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack)* )*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.QuestionMark) {
					if (!afterAsOrIs)
						goto match1;
					else if ((count = CountDims(LT(1), true)) > 0) {
						if (!Try_TypeSuffixOpt_Test0(1))
							goto match1;
						else
							break;
					} else if (!Try_TypeSuffixOpt_Test0(1))
						goto match1;
					else
						break;
				} else if (la0 == TT.Mul) {
					var t = MatchAny();
					// line 285
					e = F.Of(F.Id(t), e, e.Range.StartIndex, t.EndIndex);
					result = true;
				} else if (la0 == TT.LBrack) {
					if ((count = CountDims(LT(0), true)) > 0) {
						la1 = LA(1);
						if (la1 == TT.RBrack) {
							var dims = InternalList<Pair<int, int>>.Empty;
							Token rb;
							var lb = MatchAny();
							rb = MatchAny();
							// line 290
							dims.Add(Pair.Create(count, rb.EndIndex));
							// Line 291: greedy(&{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack)*
							for (;;) {
								la0 = LA0;
								if (la0 == TT.LBrack) {
									if ((count = CountDims(LT(0), false)) > 0) {
										la1 = LA(1);
										if (la1 == TT.RBrack) {
											Skip();
											rb = MatchAny();
											// line 291
											dims.Add(Pair.Create(count, rb.EndIndex));
										} else
											break;
									} else
										break;
								} else
									break;
							}
							// line 293
							if (CountDims(lb, false) <= 0) {
								dimensionBrack = lb;
							}
							for (int i = dims.Count - 1; i >= 0; i--) {
								e = F.Of(F.Id(S.GetArrayKeyword(dims[i].A)), e, e.Range.StartIndex, dims[i].B);
							}
							result = true;
						} else
							break;
					} else
						break;
				} else
					break;
				continue;
			match1:
				{
					var t = MatchAny();
					// Line 279: (&!{afterAsOrIs} | &!(((TT.Add|TT.AndBits|TT.At|TT.ContextualKeyword|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.New|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | LinqKeywordAsId)))
					if (!afterAsOrIs) { } else
						Check(!Try_TypeSuffixOpt_Test0(0), "Did not expect ((TT.Add|TT.AndBits|TT.At|TT.ContextualKeyword|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.New|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | LinqKeywordAsId)");
					// line 282
					e = F.Of(F.Id(t), e, e.Range.StartIndex, t.EndIndex);
					result = true;
				}
			}
			// line 302
			return result;
		}
	
	
		bool Try_Scan_TypeSuffixOpt(int lookaheadAmt, bool afterAsOrIs) {
			using (new SavePosition(this, lookaheadAmt))
				return Scan_TypeSuffixOpt(afterAsOrIs);
		}
		bool Scan_TypeSuffixOpt(bool afterAsOrIs)
		{
			TokenType la0, la1;
			// Line 279: greedy( TT.QuestionMark (&!{afterAsOrIs} | &!(((TT.Add|TT.AndBits|TT.At|TT.ContextualKeyword|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.New|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | LinqKeywordAsId))) | TT.Mul | &{(count = CountDims(LT($LI), @true)) > 0} TT.LBrack TT.RBrack greedy(&{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack)* )*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.QuestionMark) {
					if (!afterAsOrIs)
						goto match1;
					else if ((count = CountDims(LT(1), true)) > 0) {
						if (!Try_TypeSuffixOpt_Test0(1))
							goto match1;
						else
							break;
					} else if (!Try_TypeSuffixOpt_Test0(1))
						goto match1;
					else
						break;
				} else if (la0 == TT.Mul){
					if (!TryMatch((int) TT.Mul))
						return false;}
				else if (la0 == TT.LBrack) {
					if ((count = CountDims(LT(0), true)) > 0) {
						la1 = LA(1);
						if (la1 == TT.RBrack) {
							if (!TryMatch((int) TT.LBrack))
								return false;
							if (!TryMatch((int) TT.RBrack))
								return false;
							// Line 291: greedy(&{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack)*
							for (;;) {
								la0 = LA0;
								if (la0 == TT.LBrack) {
									if ((count = CountDims(LT(0), false)) > 0) {
										la1 = LA(1);
										if (la1 == TT.RBrack) {
											if (!TryMatch((int) TT.LBrack))
												return false;
											if (!TryMatch((int) TT.RBrack))
												return false;
										} else
											break;
									} else
										break;
								} else
									break;
							}
						} else
							break;
					} else
						break;
				} else
					break;
				continue;
			match1:
				{
					if (!TryMatch((int) TT.QuestionMark))
						return false;
					// Line 279: (&!{afterAsOrIs} | &!(((TT.Add|TT.AndBits|TT.At|TT.ContextualKeyword|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.New|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | LinqKeywordAsId)))
					if (!afterAsOrIs) { } else if (Try_TypeSuffixOpt_Test0(0))
						return false;
				}
			}
			return true;
		}
	
		// Types of expressions:
		// - identifier
		// - (parenthesized expr)
		// - (tuple,)
		// - ++prefix_operators
		// - suffix_operators++
		// - infix + operators, including x => y
		// - the ? conditional : operator
		// - generic<arguments>, generic!arguments, generic.[arguments]
		// - (old_style) casts
		// - call_style(->casts)
		// - method_calls(with, arguments)
		// - typeof(and other pseudofunctions)
		// - indexers[with, indexes]
		// - new Object()
		// - { code in braces; new scope }
		// - #{ code in braces; old scope }
		// - delegate(...) {...}
		// - from x in expr... (LINQ)
		// Atom is: Id, TypeKeyword, $Atom, .Atom, new ..., (ExprStart), {Stmts},
		LNode Atom()
		{
			TokenType la0, la1;
			// line 375
			LNode r;
			// Line 376: ( (TT.Dot|TT.Substitute) Atom | TT.Operator AnyOperator | (TT.Base|TT.ContextualKeyword|TT.Id|TT.This|TT.TypeKeyword) | LinqKeywordAsId | TT.Literal | ExprInParensAuto | NewExpr | BracedBlock | TokenLiteral | (TT.Checked|TT.Unchecked) TT.LParen TT.RParen | (TT.Default|TT.Sizeof|TT.Typeof) TT.LParen TT.RParen | TT.Delegate TT.LParen TT.RParen TT.LBrace TT.RBrace | TT.Is DataType )
			switch (LA0) {
			case TT.Dot: case TT.Substitute:
				{
					var t = MatchAny();
					var e = Atom();
					e = AutoRemoveParens(e);
					r = F.Call((Symbol) t.Value, e, t.StartIndex, e.Range.EndIndex, t.StartIndex, t.EndIndex);
				}
				break;
			case TT.Operator:
				{
					var op = MatchAny();
					var t = AnyOperator();
					// line 379
					r = F.Attr(_triviaUseOperatorKeyword, F.Id((Symbol) t.Value, op.StartIndex, t.EndIndex));
				}
				break;
			case TT.Base: case TT.ContextualKeyword: case TT.Id: case TT.This:
			case TT.TypeKeyword:
				{
					var t = MatchAny();
					// line 381
					r = F.Id(t);
				}
				break;
			case TT.LinqKeyword:
				{
					var t = LinqKeywordAsId();
					// line 383
					r = F.Id(t);
				}
				break;
			case TT.Literal:
				{
					var t = MatchAny();
					// line 385
					r = F.Literal(t.Value, t.StartIndex, t.EndIndex);
				}
				break;
			case TT.LParen:
				r = ExprInParensAuto();
				break;
			case TT.New:
				r = NewExpr();
				break;
			case TT.LBrace:
				r = BracedBlock();
				break;
			case TT.At:
				r = TokenLiteral();
				break;
			case TT.Checked: case TT.Unchecked:
				{
					var t = MatchAny();
					var args = Match((int) TT.LParen);
					var rp = Match((int) TT.RParen);
					// line 392
					r = F.Call((Symbol) t.Value, ExprListInside(args), t.StartIndex, rp.EndIndex, t.StartIndex, t.EndIndex);
				}
				break;
			case TT.Default: case TT.Sizeof: case TT.Typeof:
				{
					var t = MatchAny();
					var args = Match((int) TT.LParen);
					var rp = Match((int) TT.RParen);
					// line 395
					r = F.Call((Symbol) t.Value, TypeInside(args), t.StartIndex, rp.EndIndex, t.StartIndex, t.EndIndex);
				}
				break;
			case TT.Delegate:
				{
					var t = MatchAny();
					var args = Match((int) TT.LParen);
					Match((int) TT.RParen);
					var block = Match((int) TT.LBrace);
					var rb = Match((int) TT.RBrace);
					// line 397
					r = F.Call(S.Lambda, F.List(ExprListInside(args, false, true)), F.Braces(StmtListInside(block), block.StartIndex, rb.EndIndex), t.StartIndex, rb.EndIndex, t.StartIndex, t.EndIndex, NodeStyle.OldStyle);
				}
				break;
			case TT.Is:
				{
					var t = MatchAny();
					var dt = DataType();
					// line 400
					r = F.Call(S.Is, dt, t.StartIndex, dt.Range.EndIndex, t.StartIndex, t.EndIndex);
				}
				break;
			default:
				{
					// Line 402: greedy(~(EOF|TT.Comma|TT.Semicolon))*
					for (;;) {
						la0 = LA0;
						if (!(la0 == (TokenType) EOF || la0 == TT.Comma || la0 == TT.Semicolon)) {
							la1 = LA(1);
							if (la1 != (TokenType) EOF)
								Skip();
							else
								break;
						} else
							break;
					}
					// line 403
					r = Error("'{0}': Expected an expression: (parentheses), {{braces}}, identifier, literal, or $substitution.", CurrentTokenText());
				}
				break;
			}
			// line 405
			return r;
		}
	
		static readonly HashSet<int> Scan_Atom_set0 = NewSet((int) TT.Base, (int) TT.ContextualKeyword, (int) TT.Id, (int) TT.This, (int) TT.TypeKeyword);
		// Types of expressions:
		// - identifier
		// - (parenthesized expr)
		// - (tuple,)
		// - ++prefix_operators
		// - suffix_operators++
		// - infix + operators, including x => y
		// - the ? conditional : operator
		// - generic<arguments>, generic!arguments, generic.[arguments]
		// - (old_style) casts
		// - call_style(->casts)
		// - method_calls(with, arguments)
		// - typeof(and other pseudofunctions)
		// - indexers[with, indexes]
		// - new Object()
		// - { code in braces; new scope }
		// - #{ code in braces; old scope }
		// - delegate(...) {...}
		// - from x in expr... (LINQ)
		// Atom is: Id, TypeKeyword, $Atom, .Atom, new ..., (ExprStart), {Stmts},
		bool Scan_Atom()
		{
			TokenType la0, la1;
			// Line 376: ( (TT.Dot|TT.Substitute) Atom | TT.Operator AnyOperator | (TT.Base|TT.ContextualKeyword|TT.Id|TT.This|TT.TypeKeyword) | LinqKeywordAsId | TT.Literal | ExprInParensAuto | NewExpr | BracedBlock | TokenLiteral | (TT.Checked|TT.Unchecked) TT.LParen TT.RParen | (TT.Default|TT.Sizeof|TT.Typeof) TT.LParen TT.RParen | TT.Delegate TT.LParen TT.RParen TT.LBrace TT.RBrace | TT.Is DataType )
			switch (LA0) {
			case TT.Dot: case TT.Substitute:
				{
					if (!TryMatch((int) TT.Dot, (int) TT.Substitute))
						return false;
					if (!Scan_Atom())
						return false;
				}
				break;
			case TT.Operator:
				{
					if (!TryMatch((int) TT.Operator))
						return false;
					if (!Scan_AnyOperator())
						return false;
				}
				break;
			case TT.Base: case TT.ContextualKeyword: case TT.Id: case TT.This:
			case TT.TypeKeyword:
				if (!TryMatch(Scan_Atom_set0))
					return false;
				break;
			case TT.LinqKeyword:
				if (!Scan_LinqKeywordAsId())
					return false;
				break;
			case TT.Literal:
				if (!TryMatch((int) TT.Literal))
					return false;
				break;
			case TT.LParen:
				if (!Scan_ExprInParensAuto())
					return false;
				break;
			case TT.New:
				if (!Scan_NewExpr())
					return false;
				break;
			case TT.LBrace:
				if (!Scan_BracedBlock())
					return false;
				break;
			case TT.At:
				if (!Scan_TokenLiteral())
					return false;
				break;
			case TT.Checked: case TT.Unchecked:
				{
					if (!TryMatch((int) TT.Checked, (int) TT.Unchecked))
						return false;
					if (!TryMatch((int) TT.LParen))
						return false;
					if (!TryMatch((int) TT.RParen))
						return false;
				}
				break;
			case TT.Default: case TT.Sizeof: case TT.Typeof:
				{
					if (!TryMatch((int) TT.Default, (int) TT.Sizeof, (int) TT.Typeof))
						return false;
					if (!TryMatch((int) TT.LParen))
						return false;
					if (!TryMatch((int) TT.RParen))
						return false;
				}
				break;
			case TT.Delegate:
				{
					if (!TryMatch((int) TT.Delegate))
						return false;
					if (!TryMatch((int) TT.LParen))
						return false;
					if (!TryMatch((int) TT.RParen))
						return false;
					if (!TryMatch((int) TT.LBrace))
						return false;
					if (!TryMatch((int) TT.RBrace))
						return false;
				}
				break;
			case TT.Is:
				{
					if (!TryMatch((int) TT.Is))
						return false;
					if (!Scan_DataType())
						return false;
				}
				break;
			default:
				{
					// Line 402: greedy(~(EOF|TT.Comma|TT.Semicolon))*
					for (;;) {
						la0 = LA0;
						if (!(la0 == (TokenType) EOF || la0 == TT.Comma || la0 == TT.Semicolon)) {
							la1 = LA(1);
							if (la1 != (TokenType) EOF){
								if (!TryMatchExcept((int) TT.Comma, (int) TT.Semicolon))
									return false;}
							else
								break;
						} else
							break;
					}
				}
				break;
			}
			return true;
		}
	
		static readonly HashSet<int> AnyOperator_set0 = NewSet((int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.At, (int) TT.Backslash, (int) TT.BQString, (int) TT.Colon, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.Forward, (int) TT.GT, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LEGE, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.QuickBindSet, (int) TT.Set, (int) TT.Sub, (int) TT.Substitute, (int) TT.XorBits);
	
		Token AnyOperator()
		{
			TokenType la0, la1;
			Token result = default(Token);
			// Line 411: (&{LT($LI).EndIndex == LT($LI + 1).StartIndex} (TT.LT TT.LT | TT.GT TT.GT) / (TT.Add|TT.And|TT.AndBits|TT.At|TT.Backslash|TT.BQString|TT.Colon|TT.ColonColon|TT.CompoundSet|TT.DivMod|TT.Dot|TT.DotDot|TT.EqNeq|TT.Forward|TT.GT|TT.IncDec|TT.LambdaArrow|TT.LEGE|TT.LT|TT.Mul|TT.Not|TT.NotBits|TT.NullCoalesce|TT.NullDot|TT.OrBits|TT.OrXor|TT.Power|TT.PtrArrow|TT.QuestionMark|TT.QuickBind|TT.QuickBindSet|TT.Set|TT.Sub|TT.Substitute|TT.XorBits))
			la0 = LA0;
			if (la0 == TT.GT || la0 == TT.LT) {
				if (LT(0).EndIndex == LT(0 + 1).StartIndex) {
					la1 = LA(1);
					if (la1 == TT.GT || la1 == TT.LT) {
						// Line 412: (TT.LT TT.LT | TT.GT TT.GT)
						la0 = LA0;
						if (la0 == TT.LT) {
							var op = MatchAny();
							Match((int) TT.LT);
							// line 412
							result = new Token((int) TT.Operator, op.StartIndex, op.Length + 1, S.Shl);
						} else {
							var op = Match((int) TT.GT);
							Match((int) TT.GT);
							// line 413
							result = new Token((int) TT.Operator, op.StartIndex, op.Length + 1, S.Shr);
						}
					} else
						result = Match(AnyOperator_set0);
				} else
					result = Match(AnyOperator_set0);
			} else
				result = Match(AnyOperator_set0);
			return result;
		}
	
		bool Scan_AnyOperator()
		{
			TokenType la0, la1;
			// Line 411: (&{LT($LI).EndIndex == LT($LI + 1).StartIndex} (TT.LT TT.LT | TT.GT TT.GT) / (TT.Add|TT.And|TT.AndBits|TT.At|TT.Backslash|TT.BQString|TT.Colon|TT.ColonColon|TT.CompoundSet|TT.DivMod|TT.Dot|TT.DotDot|TT.EqNeq|TT.Forward|TT.GT|TT.IncDec|TT.LambdaArrow|TT.LEGE|TT.LT|TT.Mul|TT.Not|TT.NotBits|TT.NullCoalesce|TT.NullDot|TT.OrBits|TT.OrXor|TT.Power|TT.PtrArrow|TT.QuestionMark|TT.QuickBind|TT.QuickBindSet|TT.Set|TT.Sub|TT.Substitute|TT.XorBits))
			do {
				la0 = LA0;
				if (la0 == TT.GT || la0 == TT.LT) {
					if (LT(0).EndIndex == LT(0 + 1).StartIndex) {
						la1 = LA(1);
						if (la1 == TT.GT || la1 == TT.LT) {
							// Line 412: (TT.LT TT.LT | TT.GT TT.GT)
							la0 = LA0;
							if (la0 == TT.LT) {
								if (!TryMatch((int) TT.LT))
									return false;
								if (!TryMatch((int) TT.LT))
									return false;
							} else {
								if (!TryMatch((int) TT.GT))
									return false;
								if (!TryMatch((int) TT.GT))
									return false;
							}
						} else
							goto match2;
					} else
						goto match2;
				} else
					goto match2;
				break;
			match2:
				{
					if (!TryMatch(AnyOperator_set0))
						return false;
				}
			} while (false);
			return true;
		}
	
	
		LNode NewExpr()
		{
			TokenType la0, la1;
			// line 424
			Token? majorDimension = null;
			int endIndex;
			var list = VList<LNode>.Empty;
			var op = Match((int) TT.New);
			// Line 430: ( &{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack TT.LBrace TT.RBrace | TT.LBrace TT.RBrace | DataType (TT.LParen TT.RParen (TT.LBrace TT.RBrace)? / (TT.LBrace TT.RBrace)?) )
			la0 = LA0;
			if (la0 == TT.LBrack) {
				Check((count = CountDims(LT(0), false)) > 0, "Expected (count = CountDims(LT($LI), @false)) > 0");
				var lb = MatchAny();
				var rb = Match((int) TT.RBrack);
				// line 432
				var type = F.Id(S.GetArrayKeyword(count), lb.StartIndex, rb.EndIndex);
				lb = Match((int) TT.LBrace);
				rb = Match((int) TT.RBrace);
				// line 435
				list.Add(LNode.Call(type, type.Range));
				AppendInitializersInside(lb, ref list);
				endIndex = rb.EndIndex;
			} else if (la0 == TT.LBrace) {
				var lb = MatchAny();
				var rb = Match((int) TT.RBrace);
				// line 442
				list.Add(F.Missing);
				AppendInitializersInside(lb, ref list);
				endIndex = rb.EndIndex;
			} else {
				var type = DataType(false, out majorDimension);
				// Line 454: (TT.LParen TT.RParen (TT.LBrace TT.RBrace)? / (TT.LBrace TT.RBrace)?)
				do {
					la0 = LA0;
					if (la0 == TT.LParen) {
						la1 = LA(1);
						if (la1 == TT.RParen) {
							var lp = MatchAny();
							var rp = MatchAny();
							// line 456
							if ((majorDimension != null)) {
								Error("Syntax error: unexpected constructor argument list (...)");
							}
							list.Add(F.Call(type, ExprListInside(lp), type.Range.StartIndex, rp.EndIndex));
							endIndex = rp.EndIndex;
							// Line 462: (TT.LBrace TT.RBrace)?
							la0 = LA0;
							if (la0 == TT.LBrace) {
								la1 = LA(1);
								if (la1 == TT.RBrace) {
									var lb = MatchAny();
									var rb = MatchAny();
									// line 464
									AppendInitializersInside(lb, ref list);
									endIndex = rb.EndIndex;
								}
							}
						} else
							goto match2;
					} else
						goto match2;
					break;
				match2:
					{
						// line 471
						Token lb = op, rb = op;
						bool haveBraces = false;
						// Line 472: (TT.LBrace TT.RBrace)?
						la0 = LA0;
						if (la0 == TT.LBrace) {
							la1 = LA(1);
							if (la1 == TT.RBrace) {
								lb = MatchAny();
								rb = MatchAny();
								// line 472
								haveBraces = true;
							}
						}
						// line 474
						if ((majorDimension != null)) {
							list.Add(LNode.Call(type, ExprListInside(majorDimension.Value), type.Range));
						} else {
							list.Add(LNode.Call(type, type.Range));
						}
						if ((haveBraces)) {
							AppendInitializersInside(lb, ref list);
							endIndex = rb.EndIndex;
						} else {
							endIndex = type.Range.EndIndex;
						}
						if ((!haveBraces && majorDimension == null)) {
							if (IsArrayType(type)) {
								Error("Syntax error: missing array size expression");
							} else {
								Error("Syntax error: expected constructor argument list (...) or initializers {...}");
							}
						}
					}
				} while (false);
			}
			// line 495
			return F.Call(S.New, list, op.StartIndex, endIndex, op.StartIndex, op.EndIndex);
		}
	
		bool Scan_NewExpr()
		{
			TokenType la0, la1;
			if (!TryMatch((int) TT.New))
				return false;
			// Line 430: ( &{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack TT.LBrace TT.RBrace | TT.LBrace TT.RBrace | DataType (TT.LParen TT.RParen (TT.LBrace TT.RBrace)? / (TT.LBrace TT.RBrace)?) )
			la0 = LA0;
			if (la0 == TT.LBrack) {
				if (!((count = CountDims(LT(0), false)) > 0))
					return false;
				if (!TryMatch((int) TT.LBrack))
					return false;
				if (!TryMatch((int) TT.RBrack))
					return false;
				if (!TryMatch((int) TT.LBrace))
					return false;
				if (!TryMatch((int) TT.RBrace))
					return false;
			} else if (la0 == TT.LBrace) {
				if (!TryMatch((int) TT.LBrace))
					return false;
				if (!TryMatch((int) TT.RBrace))
					return false;
			} else {
				if (!Scan_DataType(false))
					return false;
				// Line 454: (TT.LParen TT.RParen (TT.LBrace TT.RBrace)? / (TT.LBrace TT.RBrace)?)
				do {
					la0 = LA0;
					if (la0 == TT.LParen) {
						la1 = LA(1);
						if (la1 == TT.RParen) {
							if (!TryMatch((int) TT.LParen))
								return false;
							if (!TryMatch((int) TT.RParen))
								return false;
							// Line 462: (TT.LBrace TT.RBrace)?
							la0 = LA0;
							if (la0 == TT.LBrace) {
								la1 = LA(1);
								if (la1 == TT.RBrace) {
									if (!TryMatch((int) TT.LBrace))
										return false;
									if (!TryMatch((int) TT.RBrace))
										return false;
								}
							}
						} else
							goto match2;
					} else
						goto match2;
					break;
				match2:
					{
						// Line 472: (TT.LBrace TT.RBrace)?
						la0 = LA0;
						if (la0 == TT.LBrace) {
							la1 = LA(1);
							if (la1 == TT.RBrace) {
								if (!TryMatch((int) TT.LBrace))
									return false;
								if (!TryMatch((int) TT.RBrace))
									return false;
							}
						}
					}
				} while (false);
			}
			return true;
		}
	
	
		private LNode ExprInParensAuto()
		{
			// Line 509: (&(ExprInParens (TT.LambdaArrow|TT.Set)) ExprInParens / ExprInParens)
			if (Try_ExprInParensAuto_Test0(0)) {
				var r = ExprInParens(true);
				// line 510
				return r;
			} else {
				var r = ExprInParens(false);
				// line 511
				return r;
			}
		}
	
		private bool Scan_ExprInParensAuto()
		{
			// Line 509: (&(ExprInParens (TT.LambdaArrow|TT.Set)) ExprInParens / ExprInParens)
			if (Try_ExprInParensAuto_Test0(0)){
				if (!Scan_ExprInParens(true))
					return false;}
			else if (!Scan_ExprInParens(false))
				return false;
			return true;
		}
	
	
		private LNode TokenLiteral()
		{
			TokenType la0;
			Token at = default(Token);
			Token L = default(Token);
			Token R = default(Token);
			at = Match((int) TT.At);
			// Line 516: (TT.LBrack TT.RBrack | TT.LBrace TT.RBrace)
			la0 = LA0;
			if (la0 == TT.LBrack) {
				L = MatchAny();
				R = Match((int) TT.RBrack);
			} else {
				L = Match((int) TT.LBrace);
				R = Match((int) TT.RBrace);
			}
			// line 517
			return F.Literal(L.Children, at.StartIndex, R.EndIndex);
		}
	
		private bool Scan_TokenLiteral()
		{
			TokenType la0;
			if (!TryMatch((int) TT.At))
				return false;
			// Line 516: (TT.LBrack TT.RBrack | TT.LBrace TT.RBrace)
			la0 = LA0;
			if (la0 == TT.LBrack) {
				if (!TryMatch((int) TT.LBrack))
					return false;
				if (!TryMatch((int) TT.RBrack))
					return false;
			} else {
				if (!TryMatch((int) TT.LBrace))
					return false;
				if (!TryMatch((int) TT.RBrace))
					return false;
			}
			return true;
		}
	
	
		private 
		LNode AtomOrTypeParamExpr()
		{
			LNode result = default(LNode);
			// Line 522: (&(IdWithOptionalTypeParams ~(TT.ContextualKeyword|TT.Id|TT.LinqKeyword)) IdWithOptionalTypeParams / Atom)
			switch (LA0) {
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
			case TT.Substitute: case TT.TypeKeyword:
				{
					if (Try_AtomOrTypeParamExpr_Test0(0))
						result = IdWithOptionalTypeParams(false);
					else
						result = Atom();
				}
				break;
			default:
				result = Atom();
				break;
			}
			return result;
		}
	
	
		private LNode PrimaryExpr()
		{
			TokenType la0;
			var e = AtomOrTypeParamExpr();
			FinishPrimaryExpr(ref e);
			// Line 530: (TT.NullDot PrimaryExpr)?
			la0 = LA0;
			if (la0 == TT.NullDot) {
				switch (LA(1)) {
				case TT.At: case TT.Base: case TT.Checked: case TT.ContextualKeyword:
				case TT.Default: case TT.Delegate: case TT.Dot: case TT.Id:
				case TT.Is: case TT.LBrace: case TT.LinqKeyword: case TT.Literal:
				case TT.LParen: case TT.New: case TT.Operator: case TT.Sizeof:
				case TT.Substitute: case TT.This: case TT.TypeKeyword: case TT.Typeof:
				case TT.Unchecked:
					{
						var op = MatchAny();
						var rhs = PrimaryExpr();
						// line 530
						e = F.Call(op, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex, NodeStyle.Operator);
					}
					break;
				}
			}
			// line 532
			return e;
		}
	
	
		private void FinishPrimaryExpr(ref LNode e)
		{
			TokenType la1;
			// Line 537: greedy( (TT.ColonColon|TT.Dot|TT.PtrArrow|TT.QuickBind) AtomOrTypeParamExpr / PrimaryExpr_NewStyleCast / TT.LParen TT.RParen | TT.LBrack TT.RBrack | TT.QuestionMark TT.LBrack TT.RBrack | TT.IncDec | BracedBlockOrTokenLiteral )*
			for (;;) {
				switch (LA0) {
				case TT.ColonColon: case TT.Dot: case TT.PtrArrow: case TT.QuickBind:
					{
						switch (LA(1)) {
						case TT.At: case TT.Base: case TT.Checked: case TT.ContextualKeyword:
						case TT.Default: case TT.Delegate: case TT.Dot: case TT.Id:
						case TT.Is: case TT.LBrace: case TT.LinqKeyword: case TT.Literal:
						case TT.LParen: case TT.New: case TT.Operator: case TT.Sizeof:
						case TT.Substitute: case TT.This: case TT.TypeKeyword: case TT.Typeof:
						case TT.Unchecked:
							{
								var op = MatchAny();
								var rhs = AtomOrTypeParamExpr();
								// line 538
								e = F.Call((Symbol) op.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex, op.StartIndex, op.EndIndex, NodeStyle.Operator);
							}
							break;
						default:
							goto stop;
						}
					}
					break;
				case TT.LParen:
					{
						if (Down(0) && Up(LA0 == TT.As || LA0 == TT.Using || LA0 == TT.PtrArrow)) {
							la1 = LA(1);
							if (la1 == TT.RParen)
								e = PrimaryExpr_NewStyleCast(e);
							else
								goto stop;
						} else {
							la1 = LA(1);
							if (la1 == TT.RParen) {
								var lp = MatchAny();
								var rp = MatchAny();
								// line 541
								e = F.Call(e, ExprListInside(lp), e.Range.StartIndex, rp.EndIndex);
							} else
								goto stop;
						}
					}
					break;
				case TT.LBrack:
					{
						la1 = LA(1);
						if (la1 == TT.RBrack) {
							var lb = MatchAny();
							var rb = MatchAny();
							var list = new VList<LNode> { 
								e
							};
							e = F.Call(S.IndexBracks, AppendExprsInside(lb, list), e.Range.StartIndex, rb.EndIndex, lb.StartIndex, lb.EndIndex);
						} else
							goto stop;
					}
					break;
				case TT.QuestionMark:
					{
						la1 = LA(1);
						if (la1 == TT.LBrack) {
							var t = MatchAny();
							var lb = MatchAny();
							var rb = Match((int) TT.RBrack);
							// line 559
							e = F.Call(S.NullIndexBracks, e, F.List(ExprListInside(lb)), e.Range.StartIndex, rb.EndIndex, t.StartIndex, lb.EndIndex);
						} else
							goto stop;
					}
					break;
				case TT.IncDec:
					{
						var t = MatchAny();
						// line 561
						e = F.Call(t.Value == S.PreInc ? S.PostInc : S.PostDec, e, e.Range.StartIndex, t.EndIndex, t.StartIndex, t.EndIndex);
					}
					break;
				case TT.At: case TT.LBrace:
					{
						la1 = LA(1);
						if (la1 == TT.LBrace || la1 == TT.LBrack || la1 == TT.RBrace) {
							var bb = BracedBlockOrTokenLiteral();
							// line 563
							if ((!e.IsCall || e.BaseStyle == NodeStyle.Operator)) {
								e = F.Call(e, bb, e.Range.StartIndex, bb.Range.EndIndex);
							} else {
								e = e.WithArgs(e.Args.Add(bb)).WithRange(e.Range.StartIndex, bb.Range.EndIndex);
							}
						} else
							goto stop;
					}
					break;
				default:
					goto stop;
				}
			}
		stop:;
		}
	
	
		private LNode PrimaryExpr_NewStyleCast(LNode e)
		{
			TokenType la0;
			Token op = default(Token);
			var lp = MatchAny();
			var rp = MatchAny();
			Down(lp);
			Symbol kind;
			var attrs = VList<LNode>.Empty;
			// Line 579: ( TT.PtrArrow | TT.As | TT.Using )
			la0 = LA0;
			if (la0 == TT.PtrArrow) {
				op = MatchAny();
				// line 579
				kind = S.Cast;
			} else if (la0 == TT.As) {
				op = MatchAny();
				// line 580
				kind = S.As;
			} else {
				op = Match((int) TT.Using);
				// line 581
				kind = S.UsingCast;
			}
			NormalAttributes(ref attrs);
			AttributeKeywords(ref attrs);
			var type = DataType();
			Match((int) EOF);
			// line 586
			type = type.PlusAttrs(attrs);
			return Up(SetAlternateStyle(SetOperatorStyle(F.Call(kind, e, type, e.Range.StartIndex, rp.EndIndex, op.StartIndex, op.EndIndex))));
		}
	
		static readonly HashSet<int> PrefixExpr_set0 = NewSet((int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.Base, (int) TT.Break, (int) TT.Checked, (int) TT.ContextualKeyword, (int) TT.Continue, (int) TT.Default, (int) TT.Delegate, (int) TT.Dot, (int) TT.DotDot, (int) TT.Forward, (int) TT.Goto, (int) TT.Id, (int) TT.IncDec, (int) TT.Is, (int) TT.LBrace, (int) TT.LinqKeyword, (int) TT.Literal, (int) TT.LParen, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.Operator, (int) TT.Power, (int) TT.Return, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.Switch, (int) TT.This, (int) TT.Throw, (int) TT.TypeKeyword, (int) TT.Typeof, (int) TT.Unchecked);
	
		// Prefix expressions, atoms, and high-precedence expressions like f(x) and List<T>
		// to distinguish (cast) expr from (parens)
		private LNode PrefixExpr()
		{
			TokenType la2;
			// Line 596: ( ((TT.Add|TT.AndBits|TT.DotDot|TT.Forward|TT.IncDec|TT.Mul|TT.Not|TT.NotBits|TT.Sub) PrefixExpr | TT.Power PrefixExpr) | (&{Down($LI) && Up(Scan_DataType() && LA0 == EOF)} TT.LParen TT.RParen &!(((TT.Add|TT.AndBits|TT.BQString|TT.Dot|TT.Mul|TT.Sub) | TT.IncDec TT.LParen)) PrefixExpr / KeywordOrPrimaryExpr) )
			do {
				switch (LA0) {
				case TT.Add: case TT.AndBits: case TT.DotDot: case TT.Forward:
				case TT.IncDec: case TT.Mul: case TT.Not: case TT.NotBits:
				case TT.Sub:
					{
						var op = MatchAny();
						var e = PrefixExpr();
						// line 597
						return SetOperatorStyle(F.Call(op, e, op.StartIndex, e.Range.EndIndex));
					}
					break;
				case TT.Power:
					{
						var op = MatchAny();
						var e = PrefixExpr();
						// line 600
						return F.Call(S._Dereference, F.Call(S._Dereference, e, op.StartIndex + 1, e.Range.EndIndex, op.StartIndex + 1, op.EndIndex, NodeStyle.Operator), op.StartIndex, e.Range.EndIndex, op.StartIndex, op.StartIndex + 1, NodeStyle.Operator);
					}
					break;
				case TT.LParen:
					{
						if (Down(0) && Up(Scan_DataType() && LA0 == EOF)) {
							la2 = LA(2);
							if (PrefixExpr_set0.Contains((int) la2)) {
								if (!Try_PrefixExpr_Test0(2)) {
									var lp = MatchAny();
									Match((int) TT.RParen);
									var e = PrefixExpr();
									// line 608
									Down(lp);
									return SetOperatorStyle(F.Call(S.Cast, e, Up(DataType()), lp.StartIndex, e.Range.EndIndex, lp.StartIndex, lp.EndIndex));
								} else
									goto matchKeywordOrPrimaryExpr;
							} else
								goto matchKeywordOrPrimaryExpr;
						} else
							goto matchKeywordOrPrimaryExpr;
					}
					break;
				default:
					goto matchKeywordOrPrimaryExpr;
				}
				break;
			matchKeywordOrPrimaryExpr:
				{
					var e = KeywordOrPrimaryExpr();
					// line 609
					return e;
				}
			} while (false);
		}
	
	
		private 
		LNode KeywordOrPrimaryExpr()
		{
			TokenType la1;
			// Line 615: ( &{Is($LI, @@await)} TT.ContextualKeyword PrefixExpr / KeywordStmtAsExpr / LinqQueryExpression / PrimaryExpr )
			do {
				switch (LA0) {
				case TT.ContextualKeyword:
					{
						if (Is(0, sy_await)) {
							la1 = LA(1);
							if (PrefixExpr_set0.Contains((int) la1)) {
								var op = MatchAny();
								var e = PrefixExpr();
								// line 616
								return SetOperatorStyle(F.Call(sy_await, e, op.StartIndex, e.Range.EndIndex, op.StartIndex, op.EndIndex));
							} else
								goto matchPrimaryExpr;
						} else
							goto matchPrimaryExpr;
					}
					break;
				case TT.Break: case TT.Continue: case TT.Goto: case TT.Return:
				case TT.Switch: case TT.Throw:
					{
						var e = KeywordStmtAsExpr();
						// line 618
						return e;
					}
					break;
				case TT.LinqKeyword:
					{
						if (Is(0, sy_from)) {
							la1 = LA(1);
							if (la1 == TT.ContextualKeyword || la1 == TT.Id || la1 == TT.Substitute) {
								var e = LinqQueryExpression();
								// line 620
								return e;
							} else
								goto matchPrimaryExpr;
						} else
							goto matchPrimaryExpr;
					}
					break;
				default:
					goto matchPrimaryExpr;
				}
				break;
			matchPrimaryExpr:
				{
					var e = PrimaryExpr();
					// line 622
					return e;
				}
			} while (false);
		}
	
	
		LNode KeywordStmtAsExpr()
		{
			TokenType la1;
			LNode result = default(LNode);
			var startIndex = LT0.StartIndex;
			// Line 627: ( ReturnBreakContinueThrow | (GotoCaseStmt / GotoStmt) | SwitchStmt )
			switch (LA0) {
			case TT.Break: case TT.Continue: case TT.Return: case TT.Throw:
				result = ReturnBreakContinueThrow(startIndex);
				break;
			case TT.Goto:
				{
					la1 = LA(1);
					if (la1 == TT.Case)
						result = GotoCaseStmt(startIndex);
					else
						result = GotoStmt(startIndex);
				}
				break;
			default:
				result = SwitchStmt(startIndex);
				break;
			}
			return result;
		}
	
	
		// This rule handles all lower precedence levels, from ** down to assignment
		// (=). This rule uses the "precedence floor" concept, documented in 
		// Loyc.Syntax.Precedence, to handle different precedence levels. The 
		// traditional approach is to define a separate rule for every precedence
		// level. By collapsing many precedence levels into a single rule, there are
		// two benefits: 
		// (1) shorter code.
		// (2) potentially better performance: using a separate rule per precedence
		//     level causes a new stack frame and prediction step to be created for 
		//     each level, regardless of the operators present in the actual 
		//     expression being parsed. For example, to parse the simple expression 
		//     "Hello" the traditional way requires 20 rules and therefore 20 stack 
		//     frames if there are 20 precedence levels. On the other hand, the 
		//     "precedence floor" approach requires an extra precedence check, so 
		//     it may be slower when there are few levels. Note: EC# has 22 
		//     precedence levels, and this method handles the lower 18 of them.
		//
		// The higher levels of the EC# expression parser do not use this trick
		// because the higher precedence levels have some complications, such as 
		// C-style casts and <type arguments>, that I prefer to deal with 
		// separately.
		private LNode SubExpr(Precedence context)
		{
			TokenType la0, la1;
			Debug.Assert(context.CanParse(EP.Prefix));
			Precedence prec;
			var e = PrefixExpr();
			// Line 671: greedy( &{context.CanParse(prec = InfixPrecedenceOf($LA))} (TT.Add|TT.And|TT.AndBits|TT.BQString|TT.CompoundSet|TT.DivMod|TT.DotDot|TT.EqNeq|TT.GT|TT.In|TT.LambdaArrow|TT.LEGE|TT.LT|TT.Mul|TT.NotBits|TT.NullCoalesce|TT.OrBits|TT.OrXor|TT.Power|TT.Set|TT.Sub|TT.XorBits) SubExpr | &{context.CanParse(prec = InfixPrecedenceOf($LA))} (TT.As|TT.Is|TT.Using) DataType FinishPrimaryExpr | &{context.CanParse(EP.Shift)} &{LT($LI).EndIndex == LT($LI + 1).StartIndex} (TT.LT TT.LT SubExpr | TT.GT TT.GT SubExpr) | &{context.CanParse(EP.IfElse)} TT.QuestionMark SubExpr TT.Colon SubExpr )*
			for (;;) {
				switch (LA0) {
				case TT.GT: case TT.LT:
					{
						la0 = LA0;
						if (context.CanParse(prec = InfixPrecedenceOf(la0))) {
							if (LT(0).EndIndex == LT(0 + 1).StartIndex) {
								if (context.CanParse(EP.Shift)) {
									la1 = LA(1);
									if (PrefixExpr_set0.Contains((int) la1))
										goto match1;
									else if (la1 == TT.GT || la1 == TT.LT)
										goto match3;
									else
										goto stop;
								} else {
									la1 = LA(1);
									if (PrefixExpr_set0.Contains((int) la1))
										goto match1;
									else
										goto stop;
								}
							} else {
								la1 = LA(1);
								if (PrefixExpr_set0.Contains((int) la1))
									goto match1;
								else
									goto stop;
							}
						} else if (LT(0).EndIndex == LT(0 + 1).StartIndex) {
							if (context.CanParse(EP.Shift)) {
								la1 = LA(1);
								if (la1 == TT.GT || la1 == TT.LT)
									goto match3;
								else
									goto stop;
							} else
								goto stop;
						} else
							goto stop;
					}
				case TT.Add: case TT.And: case TT.AndBits: case TT.BQString:
				case TT.CompoundSet: case TT.DivMod: case TT.DotDot: case TT.EqNeq:
				case TT.In: case TT.LambdaArrow: case TT.LEGE: case TT.Mul:
				case TT.NotBits: case TT.NullCoalesce: case TT.OrBits: case TT.OrXor:
				case TT.Power: case TT.Set: case TT.Sub: case TT.XorBits:
					{
						la0 = LA0;
						if (context.CanParse(prec = InfixPrecedenceOf(la0))) {
							la1 = LA(1);
							if (PrefixExpr_set0.Contains((int) la1))
								goto match1;
							else
								goto stop;
						} else
							goto stop;
					}
				case TT.As: case TT.Is: case TT.Using:
					{
						la0 = LA0;
						if (context.CanParse(prec = InfixPrecedenceOf(la0))) {
							switch (LA(1)) {
							case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
							case TT.Substitute: case TT.TypeKeyword:
								{
									var op = MatchAny();
									var rhs = DataType(true);
									var opSym = op.Type() == TT.Using ? S.UsingCast : ((Symbol) op.Value);
									e = F.Call(opSym, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex, op.StartIndex, op.EndIndex, NodeStyle.Operator);
									FinishPrimaryExpr(ref e);
								}
								break;
							default:
								goto stop;
							}
						} else
							goto stop;
					}
					break;
				case TT.QuestionMark:
					{
						if (context.CanParse(EP.IfElse)) {
							la1 = LA(1);
							if (PrefixExpr_set0.Contains((int) la1)) {
								var op = MatchAny();
								var then = SubExpr(StartExpr);
								Match((int) TT.Colon);
								var @else = SubExpr(EP.IfElse);
								// line 694
								e = F.Call(S.QuestionMark, LNode.List(e, then, @else), e.Range.StartIndex, @else.Range.EndIndex, op.StartIndex, op.EndIndex, NodeStyle.Operator);
							} else
								goto stop;
						} else
							goto stop;
					}
					break;
				default:
					goto stop;
				}
				continue;
			match1:
				{
					var op = MatchAny();
					var rhs = SubExpr(prec);
					// line 675
					e = F.Call((Symbol) op.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex, op.StartIndex, op.EndIndex, NodeStyle.Operator);
				}
				continue;
			match3:
				{
					// Line 686: (TT.LT TT.LT SubExpr | TT.GT TT.GT SubExpr)
					la0 = LA0;
					if (la0 == TT.LT) {
						var op = MatchAny();
						Match((int) TT.LT);
						var rhs = SubExpr(EP.Shift);
						// line 687
						e = F.Call(S.Shl, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex, op.StartIndex, op.EndIndex + 1, NodeStyle.Operator);
					} else {
						var op = Match((int) TT.GT);
						Match((int) TT.GT);
						var rhs = SubExpr(EP.Shift);
						// line 689
						e = F.Call(S.Shr, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex, op.StartIndex, op.EndIndex + 1, NodeStyle.Operator);
					}
				}
			}
		stop:;
			// line 697
			return e;
		}
	
	
		// An expression that can start with attributes [...], attribute keywords 
		// (out, ref, public, etc.), a named argument (a: expr) and/or a variable 
		// declaration (Foo? x = null).
		public LNode ExprStart(bool allowUnassignedVarDecl)
		{
			TokenType la0, la1;
			LNode result = default(LNode);
			// Line 705: (((TT.ContextualKeyword|TT.Id) | LinqKeywordAsId) TT.Colon ExprStartNNP / ExprStartNNP)
			la0 = LA0;
			if (la0 == TT.ContextualKeyword || la0 == TT.Id || la0 == TT.LinqKeyword) {
				la1 = LA(1);
				if (la1 == TT.Colon) {
					// line 705
					Token argName = default(Token);
					// Line 706: ((TT.ContextualKeyword|TT.Id) | LinqKeywordAsId)
					la0 = LA0;
					if (la0 == TT.ContextualKeyword || la0 == TT.Id)
						argName = MatchAny();
					else
						argName = LinqKeywordAsId();
					var colon = MatchAny();
					result = ExprStartNNP(allowUnassignedVarDecl);
					// line 708
					result = F.Call(S.NamedArg, F.Id(argName), result, argName.StartIndex, result.Range.EndIndex, colon.StartIndex, colon.EndIndex, NodeStyle.Operator);
				} else
					result = ExprStartNNP(allowUnassignedVarDecl);
			} else
				result = ExprStartNNP(allowUnassignedVarDecl);
			return result;
		}
	
	
		// ExprStart with No Named Parameter allowed
		public LNode ExprStartNNP(bool allowUnassignedVarDecl)
		{
			// line 714
			var attrs = VList<LNode>.Empty;
			var hasList = NormalAttributes(ref attrs);
			AttributeKeywords(ref attrs);
			// line 719
			if ((!attrs.IsEmpty || hasList)) {
				allowUnassignedVarDecl = true;
			}
			LNode expr;
			TentativeResult result, _;
			if ((allowUnassignedVarDecl)) {
				expr = TentativeVarDecl(attrs, out result, allowUnassignedVarDecl) ?? TentativeExpr(attrs, out result);
			} else {
				expr = TentativeExpr(attrs, out result);
				if (expr == null || expr.Calls(S.Assign, 2)) {
					InputPosition = result.OldPosition;
					expr = TentativeVarDecl(attrs, out _, allowUnassignedVarDecl);
				}
			}
			expr = expr ?? Apply(result);
			return expr;
		}
	
	
		private LNode VarDeclExpr(out bool hasInitializer, VList < LNode > attrs)
		{
			TokenType la0;
			LNode result = default(LNode);
			// Line 807: (TT.This)?
			la0 = LA0;
			if (la0 == TT.This) {
				var t = MatchAny();
				// line 807
				attrs.Add(F.Id(t));
			}
			var pair = VarDeclStart();
			// line 809
			LNode type = pair.Item1, name = pair.Item2;
			// Line 812: (RestOfPropertyDefinition / VarInitializerOpt)
			switch (LA0) {
			case TT.At: case TT.Forward: case TT.LambdaArrow: case TT.LBrace:
			case TT.LBrack: case TT.LinqKeyword:
				{
					result = RestOfPropertyDefinition(type.Range.StartIndex, type, name, true);
					// line 813
					hasInitializer = true;
				}
				break;
			default:
				{
					var nameAndInit = VarInitializerOpt(name, IsArrayType(type));
					// line 816
					hasInitializer = (nameAndInit != name);
					int start = type.Range.StartIndex;
					result = F.Call(S.Var, type, nameAndInit, start, nameAndInit.Range.EndIndex, start, start);
					hasInitializer = true;
				}
				break;
			}
			result = result.PlusAttrs(attrs);
			return result;
		}
	
	
		private Pair<LNode, LNode> VarDeclStart()
		{
			var e = DataType();
			var id = IdAtom();
			MaybeRecognizeVarAsKeyword(ref e);
			return Pair.Create(e, id);
		}
	
		private bool Scan_VarDeclStart()
		{
			if (!Scan_DataType())
				return false;
			if (!Scan_IdAtom())
				return false;
			return true;
		}
	
	
		private LNode ExprInParens(bool allowUnassignedVarDecl)
		{
			var lp = Match((int) TT.LParen);
			var rp = Match((int) TT.RParen);
			// line 852
			if ((!Down(lp))) {
				return F.Call(S.Tuple, lp.StartIndex, rp.EndIndex, lp.StartIndex, lp.EndIndex);
			}
			return Up(InParens_ExprOrTuple(allowUnassignedVarDecl, lp.StartIndex, rp.EndIndex));
		}
		private bool Scan_ExprInParens(bool allowUnassignedVarDecl)
		{
			if (!TryMatch((int) TT.LParen))
				return false;
			if (!TryMatch((int) TT.RParen))
				return false;
			return true;
		}
	
		// Called inside parens by ExprInParens
		LNode InParens_ExprOrTuple(bool allowUnassignedVarDecl, int startIndex, int endIndex)
		{
			TokenType la0, la1;
			// Line 859: (EOF =>  / ExprStart nongreedy(TT.Comma ExprStart)* (TT.Comma)? EOF)
			la0 = LA0;
			if (la0 == EOF)
				// line 860
				return F.Tuple(VList<LNode>.Empty, startIndex, endIndex);
			else {
				// line 861
				var hasAttrList = LA0 == TT.LBrack;
				var e = ExprStart(allowUnassignedVarDecl);
				// line 863
				var list = new VList<LNode> { 
					e
				};
				// Line 865: nongreedy(TT.Comma ExprStart)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						la1 = LA(1);
						if (la1 == EOF)
							break;
						else {
							Skip();
							list.Add(ExprStart(allowUnassignedVarDecl));
						}
					} else
						break;
				}
				// line 867
				bool isTuple = list.Count > 1;
				// Line 868: (TT.Comma)?
				la0 = LA0;
				if (la0 == TT.Comma) {
					Skip();
					// line 868
					isTuple = true;
				}
				Match((int) EOF);
				// line 870
				if (isTuple) {
					return F.Tuple(list, startIndex, endIndex);
				} else {
					return hasAttrList ? e : F.InParens(e, startIndex, endIndex);
				}
			}
		}
	
		private LNode BracedBlockOrTokenLiteral(Symbol spaceName = null, Symbol target = null, int startIndex = -1)
		{
			TokenType la0;
			LNode result = default(LNode);
			// Line 878: (BracedBlock | TokenLiteral)
			la0 = LA0;
			if (la0 == TT.LBrace)
				result = BracedBlock(spaceName, target, startIndex);
			else
				result = TokenLiteral();
			return result;
		}
	
		private LNode BracedBlock(Symbol spaceName = null, Symbol target = null, int startIndex = -1)
		{
			Token lit_lcub = default(Token);
			Token lit_rcub = default(Token);
			// line 883
			var oldSpace = _spaceName;
			_spaceName = spaceName ?? oldSpace;
			lit_lcub = Match((int) TT.LBrace);
			lit_rcub = Match((int) TT.RBrace);
			// line 887
			if ((startIndex == -1)) {
				startIndex = lit_lcub.StartIndex;
			}
			var stmts = StmtListInside(lit_lcub);
			_spaceName = oldSpace;
			return F.Call(target ?? S.Braces, stmts, startIndex, lit_rcub.EndIndex, lit_lcub.StartIndex, lit_lcub.EndIndex, NodeStyle.Statement);
		}
	
		private bool Scan_BracedBlock(Symbol spaceName = null, Symbol target = null, int startIndex = -1)
		{
			if (!TryMatch((int) TT.LBrace))
				return false;
			if (!TryMatch((int) TT.RBrace))
				return false;
			return true;
		}
	
	
		// ---------------------------------------------------------------------
		// -- Attributes -------------------------------------------------------
		// ---------------------------------------------------------------------
	
		bool NormalAttributes(ref VList<LNode> attrs)
		{
			TokenType la0, la1;
			bool result = default(bool);
			// Line 901: (&!{Down($LI) && Up(Try_Scan_AsmOrModLabel(0))} TT.LBrack TT.RBrack)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.LBrack) {
					if (!(Down(0) && Up(Try_Scan_AsmOrModLabel(0)))) {
						la1 = LA(1);
						if (la1 == TT.RBrack) {
							var t = MatchAny();
							Skip();
							// line 904
							result = true;
							if ((Down(t))) {
								AttributeContents(ref attrs);
								Up();
							}
						} else
							break;
					} else
						break;
				} else
					break;
			}
			return result;
		}
	
		bool Try_Scan_NormalAttributes(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return Scan_NormalAttributes();
		}
		bool Scan_NormalAttributes()
		{
			TokenType la0, la1;
			// Line 901: (&!{Down($LI) && Up(Try_Scan_AsmOrModLabel(0))} TT.LBrack TT.RBrack)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.LBrack) {
					if (!(Down(0) && Up(Try_Scan_AsmOrModLabel(0)))) {
						la1 = LA(1);
						if (la1 == TT.RBrack) {
							if (!TryMatch((int) TT.LBrack))
								return false;
							if (!TryMatch((int) TT.RBrack))
								return false;
						} else
							break;
					} else
						break;
				} else
					break;
			}
			return true;
		}
	
		void AttributeContents(ref VList<LNode> attrs)
		{
			TokenType la1;
			// line 913
			Token attrTarget = default(Token);
			// Line 914: ((TT.ContextualKeyword|TT.Id|TT.LinqKeyword|TT.Return) TT.Colon ExprList / ExprList)
			switch (LA0) {
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Return:
				{
					la1 = LA(1);
					if (la1 == TT.Colon) {
						attrTarget = MatchAny();
						Skip();
						// line 915
						VList<LNode> newAttrs = new VList<LNode>();
						ExprList(ref newAttrs, allowTrailingComma: true, allowUnassignedVarDecl: true);
						// line 918
						var attrTargetId = F.Id(attrTarget);
						for (int i = 0; i < newAttrs.Count; i++) {
							var attr = newAttrs[i];
							if ((!IsNamedArg(attr))) {
								attr = SetOperatorStyle(F.Call(S.NamedArg, attrTargetId, attr, i == 0 ? attrTarget.StartIndex : attr.Range.StartIndex, attr.Range.EndIndex));
							} else {
								Error(attrTargetId = attrs[i].Args[0], "Syntax error: only one attribute target is allowed");
							}
							attrs.Add(attr);
						}
					} else
						ExprList(ref attrs, allowTrailingComma: true, allowUnassignedVarDecl: true);
				}
				break;
			default:
				ExprList(ref attrs, allowTrailingComma: true, allowUnassignedVarDecl: true);
				break;
			}
		}
	
	
		void AttributeKeywords(ref VList<LNode> attrs)
		{
			TokenType la0;
			// Line 937: (TT.AttrKeyword)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.AttrKeyword) {
					var t = MatchAny();
					// line 938
					attrs.Add(F.Id(t));
				} else
					break;
			}
		}
	
		void TParamAttributeKeywords(ref VList<LNode> attrs)
		{
			TokenType la0;
			// Line 943: ((TT.AttrKeyword|TT.In))*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.AttrKeyword || la0 == TT.In) {
					var t = MatchAny();
					// line 944
					attrs.Add(F.Id(t));
				} else
					break;
			}
		}
	
	
		bool Try_Scan_TParamAttributeKeywords(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return Scan_TParamAttributeKeywords();
		}
		bool Scan_TParamAttributeKeywords()
		{
			TokenType la0;
			// Line 943: ((TT.AttrKeyword|TT.In))*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.AttrKeyword || la0 == TT.In){
					if (!TryMatch((int) TT.AttrKeyword, (int) TT.In))
						return false;}
				else
					break;
			}
			return true;
		}
	
		// =====================================================================
		// == LINQ =============================================================
		// =====================================================================
		public LNode LinqQueryExpression()
		{
			TokenType la0;
			// line 954
			int startIndex = LT0.StartIndex;
			_insideLinqExpr = true;
			try {
				var parts = LNode.List();
				parts.Add(LinqFromClause());
				QueryBody(ref parts);
				// Line 959: (QueryContinuation)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.LinqKeyword) {
						if (Is(0, sy_into)) {
							switch (LA(1)) {
							case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
							case TT.Substitute: case TT.TypeKeyword:
								parts.Add(QueryContinuation());
								break;
							default:
								goto stop;
							}
						} else
							goto stop;
					} else
						goto stop;
				}
			stop:;
				// line 960
				return F.Call(S.Linq, parts, startIndex, parts.Last.Range.EndIndex, startIndex, startIndex);
			} finally {
				_insideLinqExpr = false;
			}
		}
	
	
		private LNode LinqFromClause()
		{
			Check(Is(0, sy_from), "Expected Is($LI, @@from)");
			var kw = Match((int) TT.LinqKeyword);
			var e = Var_In_Expr();
			// line 967
			return F.Call(S.From, e, kw.StartIndex, e.Range.EndIndex, kw.StartIndex, kw.EndIndex);
		}
	
		static readonly HashSet<int> Var_In_Expr_set0 = NewSet((int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.At, (int) TT.Backslash, (int) TT.Base, (int) TT.BQString, (int) TT.Checked, (int) TT.Colon, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.Default, (int) TT.Delegate, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.Forward, (int) TT.GT, (int) TT.Id, (int) TT.IncDec, (int) TT.Is, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LEGE, (int) TT.LinqKeyword, (int) TT.Literal, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.Operator, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.QuickBindSet, (int) TT.Set, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.This, (int) TT.TypeKeyword, (int) TT.Typeof, (int) TT.Unchecked, (int) TT.XorBits);
	
		LNode Var_In_Expr()
		{
			TokenType la1;
			LNode got_VarIn = default(LNode);
			LNode result = default(LNode);
			// Line 971: (&(VarIn) VarIn ExprStart / ExprStart)
			switch (LA0) {
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
			case TT.Substitute: case TT.TypeKeyword:
				{
					if (Try_Scan_VarIn(0)) {
						la1 = LA(1);
						if (Var_In_Expr_set0.Contains((int) la1)) {
							Token inTok;
							got_VarIn = VarIn(out inTok);
							var e = ExprStart(false);
							// line 974
							return F.Call(S.In, got_VarIn, e, got_VarIn.Range.StartIndex, e.Range.EndIndex, inTok.StartIndex, inTok.EndIndex);
						} else
							result = ExprStart(false);
					} else
						result = ExprStart(false);
				}
				break;
			default:
				result = ExprStart(false);
				break;
			}
			return result;
		}
	
	
		private void QueryBody(ref VList<LNode> parts)
		{
			TokenType la0;
			// Line 980: greedy(QueryBodyClause)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.LinqKeyword) {
					if (Is(0, sy_from) || Is(0, sy_let) || Is(0, sy_where) || Is(0, sy_join) || Is(0, sy_orderby))
						parts.Add(QueryBodyClause());
					else
						break;
				} else
					break;
			}
			// Line 981: (LinqSelectClause | LinqGroupClause)
			la0 = LA0;
			if (la0 == TT.LinqKeyword) {
				if (Is(0, sy_select))
					parts.Add(LinqSelectClause());
				else
					parts.Add(LinqGroupClause());
			} else {
				// line 983
				Error("Expected 'select' or 'group' clause to end LINQ query");
			}
		}
	
	
		private LNode QueryBodyClause()
		{
			LNode result = default(LNode);
			// Line 989: ( LinqFromClause | LinqLet | LinqWhere | LinqJoin | LinqOrderBy )
			if (Is(0, sy_from))
				result = LinqFromClause();
			else if (Is(0, sy_let))
				result = LinqLet();
			else if (Is(0, sy_where))
				result = LinqWhere();
			else if (Is(0, sy_join))
				result = LinqJoin();
			else
				result = LinqOrderBy();
			return result;
		}
	
	
		private LNode LinqLet()
		{
			var kw = Match((int) TT.LinqKeyword);
			var e = ExprStart(false);
			if ((!e.Calls(S.Assign, 2))) {
				Error("Expected an assignment after 'let'");
			}
			return F.Call(S.Let, e, kw.StartIndex, e.Range.EndIndex, kw.StartIndex, kw.EndIndex);
		}
	
	
		private LNode LinqWhere()
		{
			var kw = Match((int) TT.LinqKeyword);
			var e = ExprStart(false);
			// line 1004
			return F.Call(S.Where, e, kw.StartIndex, e.Range.EndIndex, kw.StartIndex, kw.EndIndex);
		}
	
	
		private LNode LinqJoin()
		{
			TokenType la0;
			LNode from = default(LNode);
			LNode got_IdAtom = default(LNode);
			var kw = Match((int) TT.LinqKeyword);
			from = Var_In_Expr();
			Check(Is(0, sy_on), "Expected Is($LI, @@on)");
			Match((int) TT.LinqKeyword);
			var lhs = ExprStart(false);
			Check(Is(0, sy_equals), "Expected Is($LI, @@equals)");
			var eq = Match((int) TT.LinqKeyword);
			var rhs = ExprStart(false);
			// line 1014
			var equality = F.Call(sy__numequals, lhs, rhs, lhs.Range.StartIndex, rhs.Range.EndIndex, eq.StartIndex, eq.EndIndex);
			// Line 1020: (&{Is($LI, @@into)} TT.LinqKeyword IdAtom / )
			la0 = LA0;
			if (la0 == TT.LinqKeyword) {
				if (Is(0, sy_into)) {
					var intoKw = MatchAny();
					got_IdAtom = IdAtom();
					var into = F.Call(S.Into, got_IdAtom, intoKw.StartIndex, got_IdAtom.Range.EndIndex, intoKw.StartIndex, intoKw.EndIndex);
					var args = LNode.List(from, equality, into);
					return F.Call(S.Join, args, kw.StartIndex, into.Range.EndIndex, kw.StartIndex, kw.EndIndex);
				} else
					// line 1025
					return F.Call(S.Join, from, equality, kw.StartIndex, equality.Range.EndIndex, kw.StartIndex, kw.EndIndex);
			} else
				// line 1025
				return F.Call(S.Join, from, equality, kw.StartIndex, equality.Range.EndIndex, kw.StartIndex, kw.EndIndex);
		}
	
	
		private LNode LinqOrderBy()
		{
			TokenType la0;
			Check(Is(0, sy_orderby), "Expected Is($LI, @@orderby)");
			var kw = Match((int) TT.LinqKeyword);
			// line 1032
			var parts = LNode.List();
			parts.Add(LinqOrdering());
			// Line 1033: (TT.Comma LinqOrdering)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Comma) {
					Skip();
					parts.Add(LinqOrdering());
				} else
					break;
			}
			// line 1034
			return F.Call(S.OrderBy, parts, kw.StartIndex, parts.Last.Range.EndIndex, kw.StartIndex, kw.EndIndex);
		}
	
		private LNode LinqOrdering()
		{
			TokenType la0;
			Token dir = default(Token);
			LNode result = default(LNode);
			result = ExprStart(false);
			// Line 1040: greedy(&{Is($LI, @@ascending)} TT.LinqKeyword | &{Is($LI, @@descending)} TT.LinqKeyword)?
			la0 = LA0;
			if (la0 == TT.LinqKeyword) {
				if (Is(0, sy_ascending)) {
					dir = MatchAny();
					// line 1041
					result = F.Call(S.Ascending, result, dir.StartIndex, result.Range.EndIndex, dir.StartIndex, dir.EndIndex);
				} else if (Is(0, sy_descending)) {
					dir = MatchAny();
					// line 1043
					result = F.Call(S.Descending, result, dir.StartIndex, result.Range.EndIndex, dir.StartIndex, dir.EndIndex);
				}
			}
			return result;
		}
	
	
		private LNode LinqSelectClause()
		{
			var kw = MatchAny();
			var e = ExprStart(false);
			// line 1049
			return F.Call(S.Select, e, kw.StartIndex, e.Range.EndIndex, kw.StartIndex, kw.EndIndex);
		}
	
	
		private LNode LinqGroupClause()
		{
			TokenType la0;
			Token by = default(Token);
			Token kw = default(Token);
			LNode lhs = default(LNode);
			LNode rhs = default(LNode);
			Check(Is(0, sy_group), "Expected Is($LI, @@group)");
			kw = MatchAny();
			lhs = ExprStart(false);
			// Line 1054: (&{Is($LI, @@by)} TT.LinqKeyword ExprStart)
			la0 = LA0;
			if (la0 == TT.LinqKeyword) {
				Check(Is(0, sy_by), "Expected Is($LI, @@by)");
				by = MatchAny();
				rhs = ExprStart(false);
			} else {
				// line 1055
				Error("Expected 'by'");
				rhs = MissingHere();
			}
			// line 1056
			return F.Call(S.GroupBy, lhs, rhs, kw.StartIndex, rhs.Range.EndIndex, kw.StartIndex, kw.EndIndex);
		}
	
	
		private LNode QueryContinuation()
		{
			LNode got_IdAtom = default(LNode);
			Token kw = default(Token);
			kw = MatchAny();
			got_IdAtom = IdAtom();
			// line 1062
			var parts = LNode.List(got_IdAtom);
			QueryBody(ref parts);
			// line 1064
			return F.Call(S.Into, parts, kw.StartIndex, parts.Last.Range.EndIndex, kw.StartIndex, kw.EndIndex);
		}
	
	
		public LNode Stmt()
		{
			LNode result = default(LNode);
			var attrs = VList<LNode>.Empty;
			int startIndex = LT0.StartIndex;
			NormalAttributes(ref attrs);
			AttributeKeywords(ref attrs);
			// line 1087
			int wordAttrCount;
			var cat = DetectStatementCategoryAndAddWordAttributes(out wordAttrCount, ref attrs, DetectionMode.Stmt);
			switch ((cat)) {
			case StmtCat.MethodOrPropOrVar:
				result = MethodOrPropertyOrVarStmt(startIndex, attrs);
				break;
			case StmtCat.KeywordStmt:
				result = KeywordStmt(startIndex, attrs, wordAttrCount != 0);
				break;
			case StmtCat.IdStmt:
				result = IdStmt(startIndex, attrs, wordAttrCount != 0);
				break;
			case StmtCat.OtherStmt:
				result = OtherStmt(startIndex, attrs, wordAttrCount != 0);
				break;
			case StmtCat.ThisConstructor:
				result = Constructor(startIndex, attrs);
				break;
			default:
				throw new Exception("Parser bug");
			}
			return result;
		}
	
	
		// Methods, properties, variables, and things that look like them (trait & alias)
		LNode MethodOrPropertyOrVarStmt(int startIndex, VList<LNode> attrs)
		{
			TokenType la0;
			LNode result = default(LNode);
			// Line 1375: ( TraitDecl / AliasDecl / MethodOrPropertyOrVar )
			la0 = LA0;
			if (la0 == TT.ContextualKeyword) {
				if (Is(0, sy_trait)) {
					switch (LA(1)) {
					case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
					case TT.Substitute: case TT.This: case TT.TypeKeyword:
						{
							result = TraitDecl(startIndex);
							// line 1375
							result = result.PlusAttrs(attrs);
						}
						break;
					default:
						result = MethodOrPropertyOrVar(startIndex, attrs);
						break;
					}
				} else if (Is(0, sy_alias)) {
					switch (LA(1)) {
					case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
					case TT.Substitute: case TT.This: case TT.TypeKeyword:
						{
							result = AliasDecl(startIndex);
							// line 1376
							result = result.PlusAttrs(attrs);
						}
						break;
					default:
						result = MethodOrPropertyOrVar(startIndex, attrs);
						break;
					}
				} else
					result = MethodOrPropertyOrVar(startIndex, attrs);
			} else
				result = MethodOrPropertyOrVar(startIndex, attrs);
			return result;
		}
	
		static readonly HashSet<int> KeywordStmt_set0 = NewSet((int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.AttrKeyword, (int) TT.Base, (int) TT.Break, (int) TT.Checked, (int) TT.ContextualKeyword, (int) TT.Continue, (int) TT.Default, (int) TT.Delegate, (int) TT.Dot, (int) TT.DotDot, (int) TT.Forward, (int) TT.Goto, (int) TT.Id, (int) TT.IncDec, (int) TT.Is, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LinqKeyword, (int) TT.Literal, (int) TT.LParen, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.Operator, (int) TT.Power, (int) TT.Return, (int) TT.Semicolon, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.Switch, (int) TT.This, (int) TT.Throw, (int) TT.TypeKeyword, (int) TT.Typeof, (int) TT.Unchecked);
		static readonly HashSet<int> KeywordStmt_set1 = NewSet((int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.AttrKeyword, (int) TT.Base, (int) TT.Break, (int) TT.Checked, (int) TT.ContextualKeyword, (int) TT.Continue, (int) TT.Default, (int) TT.Delegate, (int) TT.Dot, (int) TT.DotDot, (int) TT.Forward, (int) TT.Goto, (int) TT.Id, (int) TT.IncDec, (int) TT.Is, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LinqKeyword, (int) TT.Literal, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.Operator, (int) TT.Power, (int) TT.Return, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.Switch, (int) TT.This, (int) TT.Throw, (int) TT.TypeKeyword, (int) TT.Typeof, (int) TT.Unchecked);
	
		// Statements that begin with a keyword
		LNode KeywordStmt(int startIndex, VList<LNode> attrs, bool hasWordAttrs)
		{
			TokenType la1;
			// line 1384
			LNode r;
			bool addAttrs = true;
			string showWordAttrErrorFor = null;
			// Line 1388: ( ((IfStmt | EventDecl | DelegateDecl | SpaceDecl | EnumDecl | CheckedOrUncheckedStmt | DoStmt | CaseStmt | ReturnBreakContinueThrow TT.Semicolon) | (GotoCaseStmt TT.Semicolon / GotoStmt TT.Semicolon) | SwitchStmt | WhileStmt | ForStmt | ForEachStmt) | (UsingStmt / UsingDirective) | LockStmt | FixedStmt | TryStmt )
			do {
				switch (LA0) {
				case TT.If:
					{
						r = IfStmt(startIndex);
						// line 1389
						showWordAttrErrorFor = "if statement";
						addAttrs = true;
					}
					break;
				case TT.Event:
					r = EventDecl(startIndex);
					break;
				case TT.Delegate:
					{
						r = DelegateDecl(startIndex, attrs);
						// line 1391
						addAttrs = false;
					}
					break;
				case TT.Class: case TT.Interface: case TT.Namespace: case TT.Struct:
					r = SpaceDecl(startIndex);
					break;
				case TT.Enum:
					r = EnumDecl(startIndex);
					break;
				case TT.Checked: case TT.Unchecked:
					r = CheckedOrUncheckedStmt(startIndex);
					break;
				case TT.Do:
					r = DoStmt(startIndex);
					break;
				case TT.Case:
					r = CaseStmt(startIndex);
					break;
				case TT.Break: case TT.Continue: case TT.Return: case TT.Throw:
					{
						r = ReturnBreakContinueThrow(startIndex);
						Match((int) TT.Semicolon);
					}
					break;
				case TT.Goto:
					{
						la1 = LA(1);
						if (la1 == TT.Case) {
							r = GotoCaseStmt(startIndex);
							Match((int) TT.Semicolon);
						} else if (KeywordStmt_set0.Contains((int) la1)) {
							r = GotoStmt(startIndex);
							Match((int) TT.Semicolon);
						} else
							goto error;
					}
					break;
				case TT.Switch:
					r = SwitchStmt(startIndex);
					break;
				case TT.While:
					r = WhileStmt(startIndex);
					break;
				case TT.For:
					r = ForStmt(startIndex);
					break;
				case TT.Foreach:
					r = ForEachStmt(startIndex);
					break;
				case TT.Using:
					{
						la1 = LA(1);
						if (la1 == TT.LParen) {
							r = UsingStmt(startIndex);
							// line 1412
							showWordAttrErrorFor = "using statement";
						} else if (KeywordStmt_set1.Contains((int) la1)) {
							r = UsingDirective(startIndex, attrs);
							addAttrs = false;
							showWordAttrErrorFor = "using directive";
						} else
							goto error;
					}
					break;
				case TT.Lock:
					r = LockStmt(startIndex);
					break;
				case TT.Fixed:
					r = FixedStmt(startIndex);
					break;
				case TT.Try:
					r = TryStmt(startIndex);
					break;
				default:
					goto error;
				}
				break;
			error:
				{
					// line 1418
					r = Error("Bug: Keyword statement expected, but got '{0}'", CurrentTokenText());
					ScanToEndOfStmt();
				}
			} while (false);
			// line 1422
			if (addAttrs) {
				r = r.PlusAttrs(attrs);
			}
			if (hasWordAttrs && showWordAttrErrorFor != null) {
				NonKeywordAttrError(attrs, showWordAttrErrorFor);
			}
			return r;
		}
	
	
		// Statements that start with an Id and don't allow keyword attributes
		// This may also be called for expression-statements that start with "$" or a type keyword
		LNode IdStmt(int startIndex, VList<LNode> attrs, bool hasWordAttrs)
		{
			TokenType la1;
			LNode result = default(LNode);
			bool addAttrs = false;
			string showWordAttrErrorFor = null;
			// Line 1439: ( Constructor / BlockCallStmt / LabelStmt / &(DataType TT.This) DataType => MethodOrPropertyOrVar / ExprStatement )
			do {
				switch (LA0) {
				case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword:
					{
						if (Try_IdStmt_Test0(0)) {
							if (_spaceName == LT(0).Value) {
								switch (LA(1)) {
								case TT.LParen:
									{
										if (Try_BlockCallStmt_Test0(1)) {
											if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
												goto matchConstructor;
											else
												goto matchBlockCallStmt;
										} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
											goto matchConstructor;
										else
											result = MethodOrPropertyOrVar(startIndex, attrs);
									}
									break;
								case TT.Forward: case TT.LBrace:
									{
										if (Try_BlockCallStmt_Test0(1))
											goto matchBlockCallStmt;
										else
											result = MethodOrPropertyOrVar(startIndex, attrs);
									}
									break;
								case TT.Colon:
									goto matchLabelStmt;
								default:
									result = MethodOrPropertyOrVar(startIndex, attrs);
									break;
								}
							} else {
								switch (LA(1)) {
								case TT.LParen:
									{
										if (Try_Constructor_Test2(1))
											goto matchConstructor;
										else if (Try_BlockCallStmt_Test0(1))
											goto matchBlockCallStmt;
										else
											result = MethodOrPropertyOrVar(startIndex, attrs);
									}
									break;
								case TT.Forward: case TT.LBrace:
									{
										if (Try_BlockCallStmt_Test0(1))
											goto matchBlockCallStmt;
										else
											result = MethodOrPropertyOrVar(startIndex, attrs);
									}
									break;
								case TT.Colon:
									goto matchLabelStmt;
								default:
									result = MethodOrPropertyOrVar(startIndex, attrs);
									break;
								}
							}
						} else if (_spaceName == LT(0).Value) {
							if (Is(0, sy_await)) {
								switch (LA(1)) {
								case TT.LParen:
									{
										if (Try_BlockCallStmt_Test0(1)) {
											if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
												goto matchConstructor;
											else
												goto matchBlockCallStmt;
										} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
											goto matchConstructor;
										else
											goto matchExprStatement;
									}
								case TT.Forward: case TT.LBrace:
									{
										if (Try_BlockCallStmt_Test0(1))
											goto matchBlockCallStmt;
										else
											goto matchExprStatement;
									}
								case TT.Colon:
									goto matchLabelStmt;
								default:
									goto matchExprStatement;
								}
							} else if (Is(0, sy_from)) {
								if (Try_AtomOrTypeParamExpr_Test0(0)) {
									la1 = LA(1);
									if (la1 == TT.LParen) {
										if (Try_BlockCallStmt_Test0(1)) {
											if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
												goto matchConstructor;
											else
												goto matchBlockCallStmt;
										} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
											goto matchConstructor;
										else
											goto matchExprStatement;
									} else if (la1 == TT.LBrace) {
										if (Try_BlockCallStmt_Test0(1))
											goto matchBlockCallStmt;
										else
											goto matchExprStatement;
									} else if (la1 == TT.Forward)
										goto matchBlockCallStmt;
									else if (la1 == TT.Colon)
										goto matchLabelStmt;
									else
										goto matchExprStatement;
								} else {
									la1 = LA(1);
									if (la1 == TT.LParen) {
										if (Try_BlockCallStmt_Test0(1)) {
											if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
												goto matchConstructor;
											else
												goto matchBlockCallStmt;
										} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
											goto matchConstructor;
										else
											goto matchExprStatement;
									} else if (la1 == TT.LBrace) {
										if (Try_BlockCallStmt_Test0(1))
											goto matchBlockCallStmt;
										else
											goto matchExprStatement;
									} else if (la1 == TT.Forward)
										goto matchBlockCallStmt;
									else if (la1 == TT.Colon)
										goto matchLabelStmt;
									else
										goto matchExprStatement;
								}
							} else if (Try_AtomOrTypeParamExpr_Test0(0)) {
								la1 = LA(1);
								if (la1 == TT.LParen) {
									if (Try_BlockCallStmt_Test0(1)) {
										if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
											goto matchConstructor;
										else
											goto matchBlockCallStmt;
									} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
										goto matchConstructor;
									else
										goto matchExprStatement;
								} else if (la1 == TT.LBrace) {
									if (Try_BlockCallStmt_Test0(1))
										goto matchBlockCallStmt;
									else
										goto matchExprStatement;
								} else if (la1 == TT.Forward)
									goto matchBlockCallStmt;
								else if (la1 == TT.Colon)
									goto matchLabelStmt;
								else
									goto matchExprStatement;
							} else {
								la1 = LA(1);
								if (la1 == TT.LParen) {
									if (Try_BlockCallStmt_Test0(1)) {
										if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
											goto matchConstructor;
										else
											goto matchBlockCallStmt;
									} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
										goto matchConstructor;
									else
										goto matchExprStatement;
								} else if (la1 == TT.LBrace) {
									if (Try_BlockCallStmt_Test0(1))
										goto matchBlockCallStmt;
									else
										goto matchExprStatement;
								} else if (la1 == TT.Forward)
									goto matchBlockCallStmt;
								else if (la1 == TT.Colon)
									goto matchLabelStmt;
								else
									goto matchExprStatement;
							}
						} else if (Is(0, sy_await)) {
							switch (LA(1)) {
							case TT.LParen:
								{
									if (Try_Constructor_Test2(1))
										goto matchConstructor;
									else if (Try_BlockCallStmt_Test0(1))
										goto matchBlockCallStmt;
									else
										goto matchExprStatement;
								}
							case TT.Forward: case TT.LBrace:
								{
									if (Try_BlockCallStmt_Test0(1))
										goto matchBlockCallStmt;
									else
										goto matchExprStatement;
								}
							case TT.Colon:
								goto matchLabelStmt;
							default:
								goto matchExprStatement;
							}
						} else if (Is(0, sy_from)) {
							if (Try_AtomOrTypeParamExpr_Test0(0)) {
								la1 = LA(1);
								if (la1 == TT.LParen) {
									if (Try_Constructor_Test2(1))
										goto matchConstructor;
									else if (Try_BlockCallStmt_Test0(1))
										goto matchBlockCallStmt;
									else
										goto matchExprStatement;
								} else if (la1 == TT.LBrace) {
									if (Try_BlockCallStmt_Test0(1))
										goto matchBlockCallStmt;
									else
										goto matchExprStatement;
								} else if (la1 == TT.Forward)
									goto matchBlockCallStmt;
								else if (la1 == TT.Colon)
									goto matchLabelStmt;
								else
									goto matchExprStatement;
							} else {
								la1 = LA(1);
								if (la1 == TT.LParen) {
									if (Try_Constructor_Test2(1))
										goto matchConstructor;
									else if (Try_BlockCallStmt_Test0(1))
										goto matchBlockCallStmt;
									else
										goto matchExprStatement;
								} else if (la1 == TT.LBrace) {
									if (Try_BlockCallStmt_Test0(1))
										goto matchBlockCallStmt;
									else
										goto matchExprStatement;
								} else if (la1 == TT.Forward)
									goto matchBlockCallStmt;
								else if (la1 == TT.Colon)
									goto matchLabelStmt;
								else
									goto matchExprStatement;
							}
						} else if (Try_AtomOrTypeParamExpr_Test0(0)) {
							la1 = LA(1);
							if (la1 == TT.LParen) {
								if (Try_Constructor_Test2(1))
									goto matchConstructor;
								else if (Try_BlockCallStmt_Test0(1))
									goto matchBlockCallStmt;
								else
									goto matchExprStatement;
							} else if (la1 == TT.LBrace) {
								if (Try_BlockCallStmt_Test0(1))
									goto matchBlockCallStmt;
								else
									goto matchExprStatement;
							} else if (la1 == TT.Forward)
								goto matchBlockCallStmt;
							else if (la1 == TT.Colon)
								goto matchLabelStmt;
							else
								goto matchExprStatement;
						} else {
							la1 = LA(1);
							if (la1 == TT.LParen) {
								if (Try_Constructor_Test2(1))
									goto matchConstructor;
								else if (Try_BlockCallStmt_Test0(1))
									goto matchBlockCallStmt;
								else
									goto matchExprStatement;
							} else if (la1 == TT.LBrace) {
								if (Try_BlockCallStmt_Test0(1))
									goto matchBlockCallStmt;
								else
									goto matchExprStatement;
							} else if (la1 == TT.Forward)
								goto matchBlockCallStmt;
							else if (la1 == TT.Colon)
								goto matchLabelStmt;
							else
								goto matchExprStatement;
						}
					}
					break;
				case TT.This:
					{
						if (_spaceName != S.Fn || LA(0 + 3) == TT.LBrace) {
							la1 = LA(1);
							if (la1 == TT.LParen) {
								if (Try_Constructor_Test1(1) || Try_Constructor_Test2(1))
									goto matchConstructor;
								else
									goto matchExprStatement;
							} else
								goto matchExprStatement;
						} else {
							la1 = LA(1);
							if (la1 == TT.LParen) {
								if (Try_Constructor_Test2(1))
									goto matchConstructor;
								else
									goto matchExprStatement;
							} else
								goto matchExprStatement;
						}
					}
				case TT.Default:
					{
						la1 = LA(1);
						if (la1 == TT.Colon)
							goto matchLabelStmt;
						else
							goto matchExprStatement;
					}
				case TT.Operator: case TT.Substitute: case TT.TypeKeyword:
					{
						if (Try_IdStmt_Test0(0))
							result = MethodOrPropertyOrVar(startIndex, attrs);
						else
							goto matchExprStatement;
					}
					break;
				default:
					goto matchExprStatement;
				}
				break;
			matchConstructor:
				{
					result = Constructor(startIndex, attrs);
					// line 1440
					showWordAttrErrorFor = "old-style constructor";
				}
				break;
			matchBlockCallStmt:
				{
					result = BlockCallStmt(startIndex);
					// line 1442
					showWordAttrErrorFor = "block-call statement";
					addAttrs = true;
				}
				break;
			matchLabelStmt:
				{
					result = LabelStmt(startIndex);
					// line 1444
					addAttrs = true;
				}
				break;
			matchExprStatement:
				{
					result = ExprStatement();
					// line 1448
					showWordAttrErrorFor = "expression";
					addAttrs = true;
				}
			} while (false);
			// line 1451
			if (addAttrs) {
				result = result.PlusAttrs(attrs);
			}
			if (hasWordAttrs && showWordAttrErrorFor != null) {
				NonKeywordAttrError(attrs, showWordAttrErrorFor);
			}
			return result;
		}
	
		static readonly HashSet<int> OtherStmt_set0 = NewSet((int) EOF, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.As, (int) TT.At, (int) TT.Base, (int) TT.BQString, (int) TT.Break, (int) TT.Catch, (int) TT.Checked, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.Continue, (int) TT.Default, (int) TT.Delegate, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.Else, (int) TT.EqNeq, (int) TT.Finally, (int) TT.Forward, (int) TT.Goto, (int) TT.GT, (int) TT.Id, (int) TT.In, (int) TT.IncDec, (int) TT.Is, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LEGE, (int) TT.LinqKeyword, (int) TT.Literal, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.Operator, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Return, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.Switch, (int) TT.This, (int) TT.Throw, (int) TT.TypeKeyword, (int) TT.Typeof, (int) TT.Unchecked, (int) TT.Using, (int) TT.While, (int) TT.XorBits);
		static readonly HashSet<int> OtherStmt_set1 = NewSet((int) EOF, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.As, (int) TT.At, (int) TT.BQString, (int) TT.Catch, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.Else, (int) TT.EqNeq, (int) TT.Finally, (int) TT.GT, (int) TT.In, (int) TT.IncDec, (int) TT.Is, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LEGE, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sub, (int) TT.Using, (int) TT.While, (int) TT.XorBits);
		static readonly HashSet<int> OtherStmt_set2 = NewSet((int) EOF, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.As, (int) TT.At, (int) TT.BQString, (int) TT.Catch, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.Else, (int) TT.EqNeq, (int) TT.Finally, (int) TT.GT, (int) TT.In, (int) TT.IncDec, (int) TT.Is, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LEGE, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sub, (int) TT.Using, (int) TT.While, (int) TT.XorBits);
		static readonly HashSet<int> OtherStmt_set3 = NewSet((int) EOF, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.As, (int) TT.At, (int) TT.BQString, (int) TT.Catch, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.Else, (int) TT.EqNeq, (int) TT.Finally, (int) TT.GT, (int) TT.Id, (int) TT.In, (int) TT.IncDec, (int) TT.Is, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LEGE, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sub, (int) TT.Substitute, (int) TT.Using, (int) TT.While, (int) TT.XorBits);
		static readonly HashSet<int> OtherStmt_set4 = NewSet((int) EOF, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.As, (int) TT.At, (int) TT.BQString, (int) TT.Catch, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.Else, (int) TT.EqNeq, (int) TT.Finally, (int) TT.GT, (int) TT.Id, (int) TT.In, (int) TT.IncDec, (int) TT.Is, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LEGE, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sub, (int) TT.Substitute, (int) TT.Using, (int) TT.While, (int) TT.XorBits);
	
		// Statements that don't start with an Id and don't allow keyword attributes.
		LNode OtherStmt(int startIndex, VList<LNode> attrs, bool hasWordAttrs)
		{
			TokenType la1;
			Token lit_semi = default(Token);
			LNode result = default(LNode);
			bool addAttrs = false;
			string showWordAttrErrorFor = null;
			// Line 1475: ( BracedBlock / &(TT.NotBits (TT.ContextualKeyword|TT.Id|TT.LinqKeyword|TT.This) TT.LParen TT.RParen TT.LBrace TT.RBrace) Destructor / TT.Semicolon / LabelStmt / default ExprStatement / AssemblyOrModuleAttribute / OperatorCastMethod )
			do {
				switch (LA0) {
				case TT.LBrace:
					{
						result = BracedBlock(null, null, startIndex);
						// line 1476
						showWordAttrErrorFor = "braced-block statement";
						addAttrs = true;
					}
					break;
				case TT.NotBits:
					{
						if (Try_OtherStmt_Test0(0)) {
							switch (LA(1)) {
							case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.This:
								{
									result = Destructor(startIndex, attrs);
									// line 1479
									showWordAttrErrorFor = "destructor";
								}
								break;
							case TT.Add: case TT.AndBits: case TT.At: case TT.Base:
							case TT.Break: case TT.Checked: case TT.Continue: case TT.Default:
							case TT.Delegate: case TT.Dot: case TT.DotDot: case TT.Forward:
							case TT.Goto: case TT.IncDec: case TT.Is: case TT.LBrace:
							case TT.Literal: case TT.LParen: case TT.Mul: case TT.New:
							case TT.Not: case TT.NotBits: case TT.Operator: case TT.Power:
							case TT.Return: case TT.Sizeof: case TT.Sub: case TT.Substitute:
							case TT.Switch: case TT.Throw: case TT.TypeKeyword: case TT.Typeof:
							case TT.Unchecked:
								goto matchExprStatement;
							default:
								goto error;
							}
						} else
							goto matchExprStatement;
					}
					break;
				case TT.Semicolon:
					{
						lit_semi = MatchAny();
						// line 1480
						result = F.Id(S.Missing, startIndex, lit_semi.EndIndex);
						showWordAttrErrorFor = "empty statement";
						addAttrs = true;
					}
					break;
				case TT.ContextualKeyword:
					{
						if (Is(0, sy_await)) {
							la1 = LA(1);
							if (la1 == TT.Colon)
								goto matchLabelStmt;
							else if (OtherStmt_set0.Contains((int) la1))
								goto matchExprStatement;
							else
								goto error;
						} else if (Try_AtomOrTypeParamExpr_Test0(0)) {
							la1 = LA(1);
							if (la1 == TT.Colon)
								goto matchLabelStmt;
							else if (OtherStmt_set1.Contains((int) la1))
								goto matchExprStatement;
							else
								goto error;
						} else {
							la1 = LA(1);
							if (la1 == TT.Colon)
								goto matchLabelStmt;
							else if (OtherStmt_set2.Contains((int) la1))
								goto matchExprStatement;
							else
								goto error;
						}
					}
				case TT.LinqKeyword:
					{
						if (Is(0, sy_from)) {
							if (Try_AtomOrTypeParamExpr_Test0(0)) {
								la1 = LA(1);
								if (la1 == TT.Colon)
									goto matchLabelStmt;
								else if (OtherStmt_set3.Contains((int) la1))
									goto matchExprStatement;
								else
									goto error;
							} else {
								la1 = LA(1);
								if (la1 == TT.Colon)
									goto matchLabelStmt;
								else if (OtherStmt_set4.Contains((int) la1))
									goto matchExprStatement;
								else
									goto error;
							}
						} else if (Try_AtomOrTypeParamExpr_Test0(0)) {
							la1 = LA(1);
							if (la1 == TT.Colon)
								goto matchLabelStmt;
							else if (OtherStmt_set1.Contains((int) la1))
								goto matchExprStatement;
							else
								goto error;
						} else {
							la1 = LA(1);
							if (la1 == TT.Colon)
								goto matchLabelStmt;
							else if (OtherStmt_set2.Contains((int) la1))
								goto matchExprStatement;
							else
								goto error;
						}
					}
				case TT.Id:
					{
						if (Try_AtomOrTypeParamExpr_Test0(0)) {
							la1 = LA(1);
							if (la1 == TT.Colon)
								goto matchLabelStmt;
							else if (OtherStmt_set1.Contains((int) la1))
								goto matchExprStatement;
							else
								goto error;
						} else {
							la1 = LA(1);
							if (la1 == TT.Colon)
								goto matchLabelStmt;
							else if (OtherStmt_set2.Contains((int) la1))
								goto matchExprStatement;
							else
								goto error;
						}
					}
				case TT.Default:
					{
						la1 = LA(1);
						if (la1 == TT.Colon)
							goto matchLabelStmt;
						else if (la1 == TT.LParen)
							goto matchExprStatement;
						else
							goto error;
					}
				case TT.Add: case TT.AndBits: case TT.Break: case TT.Continue:
				case TT.DotDot: case TT.Forward: case TT.Goto: case TT.IncDec:
				case TT.LParen: case TT.Mul: case TT.Not: case TT.Power:
				case TT.Return: case TT.Sub: case TT.Substitute: case TT.Switch:
				case TT.Throw:
					goto matchExprStatement;
				case TT.Operator:
					{
						switch (LA(1)) {
						case TT.Add: case TT.And: case TT.AndBits: case TT.At:
						case TT.Backslash: case TT.BQString: case TT.Colon: case TT.ColonColon:
						case TT.CompoundSet: case TT.DivMod: case TT.Dot: case TT.DotDot:
						case TT.EqNeq: case TT.Forward: case TT.GT: case TT.IncDec:
						case TT.LambdaArrow: case TT.LEGE: case TT.LT: case TT.Mul:
						case TT.Not: case TT.NotBits: case TT.NullCoalesce: case TT.NullDot:
						case TT.OrBits: case TT.OrXor: case TT.Power: case TT.PtrArrow:
						case TT.QuestionMark: case TT.QuickBind: case TT.QuickBindSet: case TT.Set:
						case TT.Sub: case TT.Substitute: case TT.XorBits:
							goto matchExprStatement;
						case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
						case TT.TypeKeyword:
							{
								result = OperatorCastMethod(startIndex, attrs);
								// line 1488
								attrs.Clear();
							}
							break;
						default:
							goto error;
						}
					}
					break;
				case TT.At: case TT.Base: case TT.Checked: case TT.Delegate:
				case TT.Dot: case TT.Is: case TT.Literal: case TT.New:
				case TT.Sizeof: case TT.This: case TT.TypeKeyword: case TT.Typeof:
				case TT.Unchecked:
					goto matchExprStatement;
				case TT.LBrack:
					{
						result = AssemblyOrModuleAttribute(startIndex, attrs);
						// line 1487
						showWordAttrErrorFor = "assembly or module attribute";
					}
					break;
				default:
					goto error;
				}
				break;
			matchLabelStmt:
				{
					result = LabelStmt(startIndex);
					// line 1483
					addAttrs = true;
				}
				break;
			matchExprStatement:
				{
					result = ExprStatement();
					// line 1485
					showWordAttrErrorFor = "expression";
					addAttrs = true;
				}
				break;
			error:
				{
					// line 1494
					result = Error("Statement expected, but got '{0}'", CurrentTokenText());
					ScanToEndOfStmt();
				}
			} while (false);
			// line 1498
			if (addAttrs) {
				result = result.PlusAttrs(attrs);
			}
			if (hasWordAttrs && showWordAttrErrorFor != null) {
				NonKeywordAttrError(attrs, showWordAttrErrorFor);
			}
			return result;
		}
	
	
		LNode ExprStatement()
		{
			Token lit_semi = default(Token);
			LNode result = default(LNode);
			result = SubExpr(StartExpr);
			// Line 1509: ((EOF|TT.Catch|TT.Else|TT.Finally|TT.While) =>  | TT.Semicolon)
			switch (LA0) {
			case EOF: case TT.Catch: case TT.Else: case TT.Finally:
			case TT.While:
				{
					var rr = result.Range;
					result = F.Call(S.Result, result, rr.StartIndex, rr.EndIndex, rr.StartIndex, rr.StartIndex);
				}
				break;
			case TT.Semicolon:
				{
					lit_semi = MatchAny();
					// line 1512
					result = result.WithRange(result.Range.StartIndex, lit_semi.EndIndex);
				}
				break;
			default:
				{
					// line 1513
					result = Error("Syntax error in expression at '{0}'; possibly missing semicolon", CurrentTokenText());
					ScanToEndOfStmt();
				}
				break;
			}
			return result;
		}
	
	
		private void ScanToEndOfStmt()
		{
			TokenType la0;
			// Line 1520: greedy(~(EOF|TT.LBrace|TT.Semicolon))*
			for (;;) {
				la0 = LA0;
				if (!(la0 == (TokenType) EOF || la0 == TT.LBrace || la0 == TT.Semicolon))
					Skip();
				else
					break;
			}
			// Line 1521: greedy(TT.Semicolon | TT.LBrace (TT.RBrace)?)?
			la0 = LA0;
			if (la0 == TT.Semicolon)
				Skip();
			else if (la0 == TT.LBrace) {
				Skip();
				// Line 1521: (TT.RBrace)?
				la0 = LA0;
				if (la0 == TT.RBrace)
					Skip();
			}
		}
	
	
		// ---------------------------------------------------------------------
		// namespace, class, struct, interface, trait, alias, using, enum ------
		// ---------------------------------------------------------------------
		private LNode SpaceDecl(int startIndex)
		{
			var t = MatchAny();
			var r = RestOfSpaceDecl(startIndex, t);
			// line 1531
			return r;
		}
	
	
		LNode TraitDecl(int startIndex)
		{
			Check(Is(0, sy_trait), "Expected Is($LI, @@trait)");
			var t = Match((int) TT.ContextualKeyword);
			var r = RestOfSpaceDecl(startIndex, t);
			// line 1537
			return r;
		}
	
	
		private LNode RestOfSpaceDecl(int startIndex, Token kindTok)
		{
			TokenType la0;
			// line 1541
			var kind = (Symbol) kindTok.Value;
			var name = ComplexNameDecl();
			var bases = BaseListOpt();
			WhereClausesOpt(ref name);
			// Line 1545: (TT.Semicolon | BracedBlock)
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				var end = MatchAny();
				// line 1546
				return F.Call(kind, name, bases, startIndex, end.EndIndex, kindTok.StartIndex, kindTok.EndIndex);
			} else {
				var body = BracedBlock(EcsValidators.KeyNameComponentOf(name));
				// line 1548
				return F.Call(kind, LNode.List(name, bases, body), startIndex, body.Range.EndIndex, kindTok.StartIndex, kindTok.EndIndex);
			}
		}
	
	
		LNode AliasDecl(int startIndex)
		{
			LNode result = default(LNode);
			Check(Is(0, sy_alias), "Expected Is($LI, @@alias)");
			var t = Match((int) TT.ContextualKeyword);
			var newName = ComplexNameDecl();
			Match((int) TT.QuickBindSet, (int) TT.Set);
			var oldName = ComplexNameDecl();
			result = RestOfAlias(startIndex, t, oldName, newName);
			return result;
		}
	
	
		LNode UsingDirective(int startIndex, VList<LNode> attrs)
		{
			TokenType la0;
			Token end = default(Token);
			LNode nsName = default(LNode);
			Token static_ = default(Token);
			Token t = default(Token);
			t = Match((int) TT.Using);
			// Line 1564: (&{Is($LI, S.Static)} TT.AttrKeyword ExprStart TT.Semicolon / ExprStart (&{nsName.Calls(S.Assign, 2)} RestOfAlias / TT.Semicolon))
			do {
				la0 = LA0;
				if (la0 == TT.AttrKeyword) {
					if (Is(0, S.Static)) {
						static_ = MatchAny();
						nsName = ExprStart(true);
						end = Match((int) TT.Semicolon);
						// line 1566
						attrs.Add(F.Id(static_));
					} else
						goto matchExprStart;
				} else
					goto matchExprStart;
				break;
			matchExprStart:
				{
					nsName = ExprStart(true);
					// Line 1569: (&{nsName.Calls(S.Assign, 2)} RestOfAlias / TT.Semicolon)
					do {
						switch (LA0) {
						case TT.Semicolon:
							{
								if (nsName.Calls(S.Assign, 2))
									goto matchRestOfAlias;
								else
									end = MatchAny();
							}
							break;
						case TT.Colon: case TT.LBrace: case TT.LinqKeyword:
							goto matchRestOfAlias;
						default:
							{
								// line 1575
								Error("Expected ';'");
							}
							break;
						}
						break;
					matchRestOfAlias:
						{
							Check(nsName.Calls(S.Assign, 2), "Expected nsName.Calls(S.Assign, 2)");
							LNode aliasedType = nsName.Args[1, F.Missing];
							nsName = nsName.Args[0, F.Missing];
							var r = RestOfAlias(startIndex, t, aliasedType, nsName);
							// line 1573
							return r.WithAttrs(attrs).PlusAttr(_filePrivate);
						}
					} while (false);
				}
			} while (false);
			// line 1578
			return F.Call(S.Import, nsName, t.StartIndex, end.EndIndex, t.StartIndex, t.EndIndex).WithAttrs(attrs);
		}
	
	
		LNode RestOfAlias(int startIndex, Token aliasTok, LNode oldName, LNode newName)
		{
			TokenType la0;
			var bases = BaseListOpt();
			WhereClausesOpt(ref newName);
			// line 1584
			var name = F.Call(S.Assign, newName, oldName, newName.Range.StartIndex, oldName.Range.EndIndex);
			// Line 1585: (TT.Semicolon | BracedBlock)
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				var end = MatchAny();
				// line 1586
				return F.Call(S.Alias, name, bases, startIndex, end.EndIndex, aliasTok.StartIndex, aliasTok.EndIndex);
			} else {
				var body = BracedBlock(EcsValidators.KeyNameComponentOf(newName));
				// line 1588
				return F.Call(S.Alias, LNode.List(name, bases, body), startIndex, body.Range.EndIndex, aliasTok.StartIndex, aliasTok.EndIndex);
			}
		}
	
	
		private LNode EnumDecl(int startIndex)
		{
			TokenType la0;
			var kw = MatchAny();
			var name = ComplexNameDecl();
			var bases = BaseListOpt();
			// Line 1597: (TT.Semicolon | TT.LBrace TT.RBrace)
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				var end = MatchAny();
				// line 1598
				return F.Call(kw, name, bases, startIndex, end.EndIndex);
			} else {
				var lb = Match((int) TT.LBrace);
				var rb = Match((int) TT.RBrace);
				// line 1601
				var list = ExprListInside(lb, true);
				var body = F.Braces(list, lb.StartIndex, rb.EndIndex);
				return F.Call(kw, LNode.List(name, bases, body), startIndex, body.Range.EndIndex);
			}
		}
	
	
		private LNode BaseListOpt()
		{
			TokenType la0;
			// Line 1609: (TT.Colon DataType (TT.Comma DataType)* | )
			la0 = LA0;
			if (la0 == TT.Colon) {
				// line 1609
				var bases = new VList<LNode>();
				Skip();
				bases.Add(DataType());
				// Line 1611: (TT.Comma DataType)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						Skip();
						bases.Add(DataType());
					} else
						break;
				}
				// line 1612
				return F.List(bases);
			} else
				// line 1613
				return F.List();
		}
	
	
		private void WhereClausesOpt(ref LNode name)
		{
			TokenType la0;
			// line 1619
			var list = new BMultiMap<Symbol, LNode>();
			// Line 1620: (WhereClause)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.LinqKeyword)
					list.Add(WhereClause());
				else
					break;
			}
			// line 1621
			if ((list.Count != 0)) {
				if ((!name.CallsMin(S.Of, 2))) {
					Error("'{0}' is not generic and cannot use 'where' clauses.", name.ToString());
				} else {
					var tparams = name.Args.ToWList();
					for (int i = 1; i < tparams.Count; i++) {
						var wheres = list[TParamSymbol(tparams[i])];
						tparams[i] = tparams[i].PlusAttrs(wheres);
						wheres.Clear();
					}
					name = name.WithArgs(tparams.ToVList());
					if ((list.Count > 0)) {
						Error(list[0].Value, "There is no type parameter named '{0}'", list[0].Key);
					}
				}
			}
		}
	
		private KeyValuePair<Symbol, LNode> WhereClause()
		{
			TokenType la0;
			Check(Is(0, sy_where), "Expected Is($LI, @@where)");
			var where = MatchAny();
			var T = Match((int) TT.ContextualKeyword, (int) TT.Id, (int) TT.LinqKeyword);
			Match((int) TT.Colon);
			// line 1651
			var constraints = VList<LNode>.Empty;
			constraints.Add(WhereConstraint());
			// Line 1653: (TT.Comma WhereConstraint)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Comma) {
					Skip();
					constraints.Add(WhereConstraint());
				} else
					break;
			}
			// line 1654
			return new KeyValuePair<Symbol, LNode>((Symbol) T.Value, F.Call(S.Where, constraints, where.StartIndex, constraints.Last.Range.EndIndex, where.StartIndex, where.EndIndex));
		}
	
		private LNode WhereConstraint()
		{
			TokenType la0;
			// Line 1658: ( (TT.Class|TT.Struct) | TT.New &{LT($LI).Count == 0} TT.LParen TT.RParen | DataType )
			la0 = LA0;
			if (la0 == TT.Class || la0 == TT.Struct) {
				var t = MatchAny();
				// line 1658
				return F.Id(t);
			} else if (la0 == TT.New) {
				var newkw = MatchAny();
				Check(LT(0).Count == 0, "Expected LT($LI).Count == 0");
				var lp = Match((int) TT.LParen);
				var rp = Match((int) TT.RParen);
				// line 1660
				return F.Call(newkw, newkw.StartIndex, rp.EndIndex);
			} else {
				var t = DataType();
				// line 1661
				return t;
			}
		}
	
	
		// ---------------------------------------------------------------------
		// -- assembly or module attribute -------------------------------------
		// ---------------------------------------------------------------------
		private // recognizer used by AssemblyOrModuleAttribute
		Token AsmOrModLabel()
		{
			Check(LT(0).Value == sy_assembly || LT(0).Value == sy_module, "Expected LT($LI).Value == @@assembly || LT($LI).Value == @@module");
			var t = Match((int) TT.ContextualKeyword);
			Match((int) TT.Colon);
			// line 1671
			return t;
		}
	
	
		bool Try_Scan_AsmOrModLabel(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return Scan_AsmOrModLabel();
		}
		bool Scan_AsmOrModLabel()
		{
			if (!(LT(0).Value == sy_assembly || LT(0).Value == sy_module))
				return false;
			if (!TryMatch((int) TT.ContextualKeyword))
				return false;
			if (!TryMatch((int) TT.Colon))
				return false;
			return true;
		}
	
		private LNode AssemblyOrModuleAttribute(int startIndex, VList<LNode> attrs)
		{
			Check(Down(0) && Up(Try_Scan_AsmOrModLabel(0)), "Expected Down($LI) && Up(Try_Scan_AsmOrModLabel(0))");
			var lb = MatchAny();
			var rb = Match((int) TT.RBrack);
			// line 1677
			Down(lb);
			var kind = AsmOrModLabel();
			// line 1679
			var list = new VList<LNode>();
			ExprList(ref list);
			// line 1682
			Up();
			var r = F.Call(kind.Value == sy_module ? S.Module : S.Assembly, list, startIndex, rb.EndIndex, kind.StartIndex, kind.EndIndex);
			return r.WithAttrs(attrs);
		}
	
	
		// ---------------------------------------------------------------------
		// methods, properties, variable/field declarations, operators ---------
		// ---------------------------------------------------------------------
		private LNode MethodOrPropertyOrVar(int startIndex, VList<LNode> attrs)
		{
			TokenType la0;
			LNode name = default(LNode);
			LNode result = default(LNode);
			// line 1694
			bool isExtensionMethod = false;
			bool isNamedThis;
			// Line 1695: (TT.This)?
			la0 = LA0;
			if (la0 == TT.This) {
				var t = MatchAny();
				// line 1695
				attrs.Add(F.Id(t));
				isExtensionMethod = true;
			}
			var type = DataType();
			name = ComplexNameDecl(!isExtensionMethod, out isNamedThis);
			// Line 1699: ( &{!isNamedThis} VarInitializerOpt (TT.Comma ComplexNameDecl VarInitializerOpt)* TT.Semicolon / &{!isNamedThis} MethodArgListAndBody | RestOfPropertyDefinition )
			switch (LA0) {
			case TT.Comma: case TT.QuickBindSet: case TT.Semicolon: case TT.Set:
				{
					Check(!isNamedThis, "Expected !isNamedThis");
					MaybeRecognizeVarAsKeyword(ref type);
					var parts = LNode.List(type);
					var isArray = IsArrayType(type);
					parts.Add(VarInitializerOpt(name, isArray));
					// Line 1704: (TT.Comma ComplexNameDecl VarInitializerOpt)*
					for (;;) {
						la0 = LA0;
						if (la0 == TT.Comma) {
							Skip();
							name = ComplexNameDecl();
							parts.Add(VarInitializerOpt(name, isArray));
						} else
							break;
					}
					var end = Match((int) TT.Semicolon);
					var typeStart = type.Range.StartIndex;
					result = F.Call(S.Var, parts, typeStart, end.EndIndex, typeStart, typeStart);
				}
				break;
			case TT.LParen:
				{
					Check(!isNamedThis, "Expected !isNamedThis");
					result = MethodArgListAndBody(startIndex, type.Range.StartIndex, attrs, S.Fn, type, name);
					// line 1712
					return result;
				}
				break;
			case TT.At: case TT.Forward: case TT.LambdaArrow: case TT.LBrace:
			case TT.LBrack: case TT.LinqKeyword:
				result = RestOfPropertyDefinition(startIndex, type, name, false);
				break;
			default:
				{
					// line 1715
					Error("Syntax error in what appears to be a method, property, or variable declaration");
					ScanToEndOfStmt();
					// line 1717
					result = F.Call(S.Var, type, name, type.Range.StartIndex, name.Range.EndIndex);
				}
				break;
			}
			result = result.PlusAttrs(attrs);
			return result;
		}
	
	
		private LNode VarInitializerOpt(LNode name, bool isArray)
		{
			TokenType la0;
			LNode expr = default(LNode);
			// Line 1723: (VarInitializer)?
			la0 = LA0;
			if (la0 == TT.QuickBindSet || la0 == TT.Set) {
				// line 1723
				int eqIndex = LT0.StartIndex;
				expr = VarInitializer(isArray);
				// line 1725
				return F.Call(S.Assign, name, expr, name.Range.StartIndex, expr.Range.EndIndex, eqIndex, eqIndex + 1);
			}
			// line 1726
			return name;
		}
	
		private LNode VarInitializer(bool isArray)
		{
			TokenType la0;
			LNode result = default(LNode);
			Skip();
			// Line 1733: (&{isArray} &{Down($LI) && Up(HasNoSemicolons())} TT.LBrace TT.RBrace / ExprStart)
			la0 = LA0;
			if (la0 == TT.LBrace) {
				if (Down(0) && Up(HasNoSemicolons())) {
					if (isArray) {
						var lb = MatchAny();
						var rb = Match((int) TT.RBrace);
						// line 1737
						var initializers = InitializerListInside(lb);
						result = F.Call(S.ArrayInit, initializers, lb.StartIndex, rb.EndIndex, lb.StartIndex, lb.EndIndex, NodeStyle.Expression);
					} else
						result = ExprStart(false);
				} else
					result = ExprStart(false);
			} else
				result = ExprStart(false);
			return result;
		}
	
	
		private LNode RestOfPropertyDefinition(int startIndex, LNode type, LNode name, bool isExpression)
		{
			TokenType la0;
			Token lb = default(Token);
			Token rb = default(Token);
			LNode result = default(LNode);
			// line 1746
			LNode args = F.Missing;
			// Line 1747: (TT.LBrack TT.RBrack)?
			la0 = LA0;
			if (la0 == TT.LBrack) {
				lb = MatchAny();
				rb = Match((int) TT.RBrack);
				// line 1747
				args = ArgList(lb, rb);
			}
			WhereClausesOpt(ref name);
			// line 1749
			LNode initializer;
			var body = MethodBodyOrForward(true, out initializer, isExpression);
			// line 1752
			var parts = new VList<LNode> { 
				type, name, args, body
			};
			if (initializer != null) {
				parts.Add(initializer);
			}
			int targetIndex = type.Range.StartIndex;
			result = F.Call(S.Property, parts, startIndex, body.Range.EndIndex, targetIndex, targetIndex);
			return result;
		}
	
	
		private LNode OperatorCastMethod(int startIndex, VList<LNode> attrs)
		{
			// line 1760
			LNode r;
			var op = MatchAny();
			var type = DataType();
			// line 1762
			var name = F.Attr(_triviaUseOperatorKeyword, F.Id(S.Cast, op.StartIndex, op.EndIndex));
			r = MethodArgListAndBody(startIndex, op.StartIndex, attrs, S.Fn, type, name);
			// line 1764
			return r;
		}
	
	
		private LNode MethodArgListAndBody(int startIndex, int targetIndex, VList<LNode> attrs, Symbol kind, LNode type, LNode name)
		{
			TokenType la0;
			Token lit_colon = default(Token);
			var lp = Match((int) TT.LParen);
			var rp = Match((int) TT.RParen);
			WhereClausesOpt(ref name);
			// line 1770
			LNode r, _, baseCall = null;
			// line 1770
			int consCallIndex = -1;
			// Line 1771: (TT.Colon (TT.Base|TT.This) TT.LParen TT.RParen)?
			la0 = LA0;
			if (la0 == TT.Colon) {
				lit_colon = MatchAny();
				var target = Match((int) TT.Base, (int) TT.This);
				var baselp = Match((int) TT.LParen);
				var baserp = Match((int) TT.RParen);
				// line 1773
				baseCall = F.Call((Symbol) target.Value, ExprListInside(baselp), target.StartIndex, baserp.EndIndex, target.StartIndex, target.EndIndex);
				if ((kind != S.Constructor)) {
					Error(baseCall, "This is not a constructor declaration, so there should be no ':' clause.");
				}
				consCallIndex = lit_colon.StartIndex;
			}
			// line 1781
			for (int i = 0; i < attrs.Count; i++) {
				var attr = attrs[i];
				if (IsNamedArg(attr) && attr.Args[0].IsIdNamed(S.Return)) {
					type = type.PlusAttr(attr.Args[1]);
					attrs.RemoveAt(i);
					i--;
				}
			}
			// Line 1790: (default TT.Semicolon | MethodBodyOrForward)
			do {
				switch (LA0) {
				case TT.Semicolon:
					goto match1;
				case TT.At: case TT.Forward: case TT.LambdaArrow: case TT.LBrace:
					{
						var body = MethodBodyOrForward(false, out _, false, consCallIndex);
						// line 1804
						if (kind == S.Delegate) {
							Error("A 'delegate' is not expected to have a method body.");
						}
						if (baseCall != null) {
							if ((!body.Calls(S.Braces))) {
								body = F.Braces(LNode.List(body), startIndex, body.Range.EndIndex);
							}
							body = body.WithArgs(body.Args.Insert(0, baseCall));
						}
						var parts = new VList<LNode> { 
							type, name, ArgList(lp, rp), body
						};
						r = F.Call(kind, parts, startIndex, body.Range.EndIndex, targetIndex, targetIndex);
					}
					break;
				default:
					goto match1;
				}
				break;
			match1:
				{
					var end = Match((int) TT.Semicolon);
					// line 1792
					if (kind == S.Constructor && baseCall != null) {
						Error(baseCall, "A method body is required.");
						var parts = LNode.List(type, name, ArgList(lp, rp), LNode.Call(S.Braces, new VList<LNode>(baseCall), baseCall.Range));
						r = F.Call(kind, parts, startIndex, baseCall.Range.EndIndex, targetIndex, targetIndex);
					} else {
						var parts = LNode.List(type, name, ArgList(lp, rp));
						r = F.Call(kind, parts, startIndex, end.EndIndex, targetIndex, targetIndex);
					}
				}
			} while (false);
			// line 1815
			return r.PlusAttrs(attrs);
		}
	
	
		private LNode MethodBodyOrForward(bool isProperty, out LNode propInitializer, bool isExpression = false, int bodyStartIndex = -1)
		{
			TokenType la0;
			// line 1820
			propInitializer = null;
			// Line 1821: ( TT.Forward ExprStart SemicolonIf | TT.LambdaArrow ExprStart SemicolonIf | TokenLiteral (&{!isExpression} TT.Semicolon)? | BracedBlock greedy(&{isProperty} TT.Set ExprStart SemicolonIf)? )
			la0 = LA0;
			if (la0 == TT.Forward) {
				var op = MatchAny();
				var e = ExprStart(true);
				SemicolonIf(!isExpression);
				// line 1821
				return F.Call(op, e, op.StartIndex, e.Range.EndIndex);
			} else if (la0 == TT.LambdaArrow) {
				var op = MatchAny();
				var e = ExprStart(false);
				SemicolonIf(!isExpression);
				// line 1822
				return e;
			} else if (la0 == TT.At) {
				var e = TokenLiteral();
				// Line 1823: (&{!isExpression} TT.Semicolon)?
				la0 = LA0;
				if (la0 == TT.Semicolon) {
					Check(!isExpression, "Expected !isExpression");
					Skip();
				}
				// line 1823
				return e;
			} else {
				var body = BracedBlock(S.Fn, null, bodyStartIndex);
				// Line 1827: greedy(&{isProperty} TT.Set ExprStart SemicolonIf)?
				la0 = LA0;
				if (la0 == TT.Set) {
					Check(isProperty, "Expected isProperty");
					Skip();
					propInitializer = ExprStart(false);
					SemicolonIf(!isExpression);
				}
				// line 1830
				return body;
			}
		}
	
		private void SemicolonIf(bool isStatement)
		{
			TokenType la0;
			// Line 1835: (&{isStatement} TT.Semicolon / )
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				if (isStatement)
					Skip();
				else// line 1836
				if (isStatement) {
					Error(0, "Expected ';' to end statement");
				}
			} else// line 1836
			if (isStatement) {
				Error(0, "Expected ';' to end statement");
			}
		}
	
	
		private 
		void NoSemicolons()
		{
			TokenType la0;
			// Line 1857: (~(EOF|TT.Semicolon))*
			for (;;) {
				la0 = LA0;
				if (!(la0 == (TokenType) EOF || la0 == TT.Semicolon))
					Skip();
				else
					break;
			}
			Match((int) EOF);
		}
	
	
		bool Try_HasNoSemicolons(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return HasNoSemicolons();
		}
		bool HasNoSemicolons()
		{
			TokenType la0;
			// Line 1857: (~(EOF|TT.Semicolon))*
			for (;;) {
				la0 = LA0;
				if (!(la0 == (TokenType) EOF || la0 == TT.Semicolon)){
					if (!TryMatchExcept((int) TT.Semicolon))
						return false;}
				else
					break;
			}
			if (!TryMatch((int) EOF))
				return false;
			return true;
		}
	
		// ---------------------------------------------------------------------
		// Constructor/destructor ----------------------------------------------
		// ---------------------------------------------------------------------
		private LNode Constructor(int startIndex, VList<LNode> attrs)
		{
			TokenType la0;
			// line 1864
			LNode r;
			Token n;
			// Line 1865: ( &{_spaceName == LT($LI).Value} (TT.ContextualKeyword|TT.Id|TT.LinqKeyword) &(TT.LParen TT.RParen (TT.LBrace|TT.Semicolon)) / &{_spaceName != S.Fn || LA($LI + 3) == TT.LBrace} TT.This &(TT.LParen TT.RParen (TT.LBrace|TT.Semicolon)) / (TT.ContextualKeyword|TT.Id|TT.LinqKeyword|TT.This) &(TT.LParen TT.RParen TT.Colon) )
			do {
				la0 = LA0;
				if (la0 == TT.ContextualKeyword || la0 == TT.Id || la0 == TT.LinqKeyword) {
					if (_spaceName == LT(0).Value) {
						if (Try_Constructor_Test0(1))
							n = MatchAny();
						else
							goto match3;
					} else
						goto match3;
				} else {
					if (_spaceName != S.Fn || LA(0 + 3) == TT.LBrace) {
						if (Try_Constructor_Test1(1))
							n = Match((int) TT.This);
						else
							goto match3;
					} else
						goto match3;
				}
				break;
			match3:
				{
					n = Match((int) TT.ContextualKeyword, (int) TT.Id, (int) TT.LinqKeyword, (int) TT.This);
					Check(Try_Constructor_Test2(0), "Expected TT.LParen TT.RParen TT.Colon");
				}
			} while (false);
			// line 1874
			LNode name = F.Id((Symbol) n.Value, n.StartIndex, n.EndIndex);
			r = MethodArgListAndBody(startIndex, n.StartIndex, attrs, S.Constructor, F.Missing, name);
			// line 1876
			return r;
		}
	
	
		private LNode Destructor(int startIndex, VList<LNode> attrs)
		{
			LNode result = default(LNode);
			var tilde = MatchAny();
			var n = MatchAny();
			// line 1882
			var name = (Symbol) n.Value;
			if (name != _spaceName) {
				Error("Unexpected destructor '{0}'", name);
			}
			LNode name2 = F.Call(tilde, F.Id(n), tilde.StartIndex, n.EndIndex);
			result = MethodArgListAndBody(startIndex, tilde.StartIndex, attrs, S.Fn, F.Missing, name2);
			return result;
		}
	
	
		// ---------------------------------------------------------------------
		// Delegate & event declarations ---------------------------------------
		// ---------------------------------------------------------------------
		private LNode DelegateDecl(int startIndex, VList<LNode> attrs)
		{
			var d = MatchAny();
			var type = DataType();
			var name = ComplexNameDecl();
			var r = MethodArgListAndBody(startIndex, d.StartIndex, attrs, S.Delegate, type, name);
			// line 1898
			return r.WithAttrs(attrs);
		}
	
	
		private LNode EventDecl(int startIndex)
		{
			TokenType la0;
			Token eventkw = default(Token);
			Token lit_semi = default(Token);
			LNode result = default(LNode);
			eventkw = MatchAny();
			var type = DataType();
			var name = ComplexNameDecl();
			// Line 1904: (TT.Comma ComplexNameDecl (TT.Comma ComplexNameDecl)*)?
			la0 = LA0;
			if (la0 == TT.Comma) {
				// line 1904
				var parts = new VList<LNode>(name);
				Skip();
				parts.Add(ComplexNameDecl());
				// Line 1905: (TT.Comma ComplexNameDecl)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						Skip();
						parts.Add(ComplexNameDecl());
					} else
						break;
				}
				// line 1906
				name = F.List(parts, name.Range.StartIndex, parts.Last.Range.EndIndex);
			}
			// Line 1908: (TT.Semicolon | BracedBlock)
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				lit_semi = MatchAny();
				// line 1909
				result = F.Call(eventkw, type, name, startIndex, lit_semi.EndIndex);
			} else {
				var body = BracedBlock(S.Fn);
				if (name.Calls(S.AltList)) {
					Error("A body is not allowed when defining multiple events.");
				}
				result = F.Call(eventkw, LNode.List(type, name, body), startIndex, body.Range.EndIndex);
			}
			return result;
		}
	
	
		// ---------------------------------------------------------------------
		// Statements for executable contexts ----------------------------------
		// ---------------------------------------------------------------------
		// Labels, default:, case expr: ----------------------------------------
		LNode LabelStmt(int startIndex)
		{
			var id = Match((int) TT.ContextualKeyword, (int) TT.Default, (int) TT.Id, (int) TT.LinqKeyword);
			var end = Match((int) TT.Colon);
			// line 1924
			return F.Call(S.Label, F.Id(id), startIndex, end.EndIndex, id.StartIndex, id.StartIndex);
		}
	
	
		LNode CaseStmt(int startIndex)
		{
			TokenType la0;
			// line 1928
			var cases = VList<LNode>.Empty;
			var kw = Match((int) TT.Case);
			cases.Add(ExprStartNNP(true));
			// Line 1930: (TT.Comma ExprStartNNP)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Comma) {
					Skip();
					cases.Add(ExprStartNNP(true));
				} else
					break;
			}
			var end = Match((int) TT.Colon);
			// line 1931
			return F.Call(kw, cases, startIndex, end.EndIndex);
		}
	
	
		// Block-call statement (e.g. get {...}, unroll(...) {...}) ----------------
		// Block-call statements help support properties (get/set), events (add/
		// remove) and macros. No semicolon is required at the end, and the 
		// statement cannot continue afterward (at statement level, foo {y} = z; 
		// is a syntax error since '}' marks the end of the statement.)
		// The expressions in "foo(exprs) {...}" are parsed subtly differently than
		// for "foo(exprs);" : unassigned variable declarations are allowed only in
		// the former. This enables macro syntax like "on_throw(Exception e) {...}";
		// in contrast we MUST NOT parse "Foo(Bar<T> x);" as a variable declaration.
		private LNode BlockCallStmt(int startIndex)
		{
			TokenType la0;
			var id = MatchAny();
			Check(Try_BlockCallStmt_Test0(0), "Expected ( TT.LParen TT.RParen (TT.LBrace TT.RBrace | TT.Id) | TT.LBrace TT.RBrace | TT.Forward )");
			var args = new VList<LNode>();
			LNode block;
			// Line 1949: ( TT.LParen TT.RParen (BracedBlock | TT.Id => Stmt) | TT.Forward ExprStart TT.Semicolon | BracedBlock )
			la0 = LA0;
			if (la0 == TT.LParen) {
				var lp = MatchAny();
				var rp = Match((int) TT.RParen);
				// line 1949
				args = AppendExprsInside(lp, args, false, true);
				// Line 1950: (BracedBlock | TT.Id => Stmt)
				la0 = LA0;
				if (la0 == TT.LBrace)
					block = BracedBlock();
				else {
					block = Stmt();
					// line 1953
					ErrorSink.Write(Severity.Error, block, ColumnOf(block.Range.StartIndex) <= ColumnOf(id.StartIndex) ? "Probable missing semicolon before this statement." : "Probable missing braces around body of '{0}' statement.", id.Value);
				}
			} else if (la0 == TT.Forward) {
				var fwd = MatchAny();
				var e = ExprStart(true);
				Match((int) TT.Semicolon);
				// line 1960
				block = SetOperatorStyle(F.Call(fwd, e, fwd.StartIndex, e.Range.EndIndex));
			} else
				block = BracedBlock();
			// line 1964
			args.Add(block);
			var result = F.Call((Symbol) id.Value, args, id.StartIndex, block.Range.EndIndex, id.StartIndex, id.EndIndex, NodeStyle.Special);
			if (block.Calls(S.Forward, 1)) {
				result = F.Attr(_triviaForwardedProperty, result);
			}
			return result;
		}
		static readonly HashSet<int> ReturnBreakContinueThrow_set0 = NewSet((int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.AttrKeyword, (int) TT.Base, (int) TT.Break, (int) TT.Checked, (int) TT.ContextualKeyword, (int) TT.Continue, (int) TT.Default, (int) TT.Delegate, (int) TT.Dot, (int) TT.DotDot, (int) TT.Forward, (int) TT.Goto, (int) TT.Id, (int) TT.IncDec, (int) TT.Is, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LinqKeyword, (int) TT.Literal, (int) TT.LParen, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.Operator, (int) TT.Power, (int) TT.Return, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.Switch, (int) TT.This, (int) TT.Throw, (int) TT.TypeKeyword, (int) TT.Typeof, (int) TT.Unchecked);
	
		// break, continue, return, throw --------------------------------------
	
		private LNode ReturnBreakContinueThrow(int startIndex)
		{
			TokenType la0;
			LNode e = default(LNode);
			var kw = MatchAny();
			// Line 1982: greedy(ExprStartNNP)?
			la0 = LA0;
			if (ReturnBreakContinueThrow_set0.Contains((int) la0))
				e = ExprStartNNP(false);
			// line 1984
			if (e != null)
				return F.Call((Symbol) kw.Value, e, startIndex, e.Range.EndIndex, kw.StartIndex, kw.EndIndex);
			else
				return F.Call((Symbol) kw.Value, startIndex, kw.EndIndex, kw.StartIndex, kw.EndIndex);
		}
	
	
		// goto, goto case -----------------------------------------------------
		private LNode GotoStmt(int startIndex)
		{
			TokenType la0;
			var kw = MatchAny();
			// Line 1993: (TT.Default / ExprOrNull)
			la0 = LA0;
			if (la0 == TT.Default) {
				var @def = MatchAny();
				// line 1994
				return F.Call(kw, F.Id(@def), startIndex, kw.EndIndex);
			} else {
				var e = ExprOrNull(false);
				// line 1998
				if (e != null)
					return F.Call(kw, e, startIndex, e.Range.EndIndex);
				else
					return F.Call(kw, startIndex, kw.EndIndex);
			}
		}
	
	
		private LNode GotoCaseStmt(int startIndex)
		{
			TokenType la0;
			// line 2005
			LNode e = null;
			var kw = MatchAny();
			var kw2 = MatchAny();
			// Line 2007: (TT.Default / ExprStartNNP)
			la0 = LA0;
			if (la0 == TT.Default) {
				var @def = MatchAny();
				// line 2008
				e = F.Id(S.Default, @def.StartIndex, @def.EndIndex);
			} else
				e = ExprStartNNP(false);
			var endIndex = e != null ? e.Range.EndIndex : kw2.EndIndex;
			return F.Call(S.GotoCase, e, startIndex, endIndex, kw.StartIndex, kw2.EndIndex);
		}
	
	
		// checked & unchecked -------------------------------------------------
		private LNode CheckedOrUncheckedStmt(int startIndex)
		{
			var kw = MatchAny();
			var bb = BracedBlock();
			// line 2019
			return F.Call((Symbol) kw.Value, bb, startIndex, bb.Range.EndIndex, kw.StartIndex, kw.EndIndex);
		}
	
	
		// do-while & while ----------------------------------------------------
		private LNode DoStmt(int startIndex)
		{
			Token lit_lpar = default(Token);
			var kw = MatchAny();
			var block = Stmt();
			Match((int) TT.While);
			lit_lpar = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var end = Match((int) TT.Semicolon);
			// line 2027
			var parts = new VList<LNode>(block);
			SingleExprInside(lit_lpar, "while (...)", false, ref parts);
			return F.Call(S.DoWhile, parts, startIndex, end.EndIndex, kw.StartIndex, kw.EndIndex);
		}
	
	
		private LNode WhileStmt(int startIndex)
		{
			Token lit_lpar = default(Token);
			var kw = MatchAny();
			lit_lpar = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			// line 2036
			var cond = SingleExprInside(lit_lpar, "while (...)");
			return F.Call(kw, cond, block, startIndex, block.Range.EndIndex);
		}
	
	
		// for & foreach -------------------------------------------------------
		private LNode ForStmt(int startIndex)
		{
			Token lit_lpar = default(Token);
			var kw = MatchAny();
			lit_lpar = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			// line 2045
			Down(lit_lpar);
			// line 2046
			var init = VList<LNode>.Empty;
			var inc = init;
			ExprList(ref init, false, true);
			Match((int) TT.Semicolon);
			var cond = ExprOpt(false);
			Match((int) TT.Semicolon);
			ExprList(ref inc, false, false);
			// line 2048
			Up();
			// line 2050
			var initL = F.Call(S.AltList, init);
			var incL = F.Call(S.AltList, inc);
			var parts = new VList<LNode> { 
				initL, cond, incL, block
			};
			return F.Call(kw, parts, startIndex, block.Range.EndIndex);
		}
	
	
		private LNode ForEachStmt(int startIndex)
		{
			TokenType la1;
			LNode @var = default(LNode);
			var kw = MatchAny();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			// line 2060
			Down(p);
			// Line 2061: (&(VarIn) VarIn)?
			switch (LA0) {
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
			case TT.Substitute: case TT.TypeKeyword:
				{
					if (Try_Scan_VarIn(0)) {
						la1 = LA(1);
						if (Var_In_Expr_set0.Contains((int) la1)) {
							Token _;
							@var = VarIn(out _);
						}
					}
				}
				break;
			}
			var expr = ExprStart(false);
			Match((int) EOF, (int) TT.RParen);
			// line 2065
			var parts = LNode.List(@var ?? F.Missing, expr, block);
			return Up(F.Call(kw, parts, startIndex, block.Range.EndIndex));
		}
	
	
		// The "T id in" part of "foreach (T id in e)" or "from int x in ..." (type is optional)
		private LNode VarIn(out Token inTok)
		{
			LNode result = default(LNode);
			var pair = VarDeclStart();
			var start = pair.A.Range.StartIndex;
			result = F.Call(S.Var, pair.A, pair.B, start, pair.B.Range.EndIndex, start, start);
			inTok = Match((int) TT.In);
			return result;
		}
	
	
		bool Try_Scan_VarIn(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return Scan_VarIn();
		}
		bool Scan_VarIn()
		{
			if (!Scan_VarDeclStart())
				return false;
			if (!TryMatch((int) TT.In))
				return false;
			return true;
		}
	
		// if-else -------------------------------------------------------------
		private LNode IfStmt(int startIndex)
		{
			TokenType la0;
			// line 2083
			LNode @else = null;
			var kw = MatchAny();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var then = Stmt();
			// Line 2085: greedy(TT.Else Stmt)?
			la0 = LA0;
			if (la0 == TT.Else) {
				Skip();
				@else = Stmt();
			}
			// line 2087
			var cond = SingleExprInside(p, "if (...)");
			var parts = (@else == null ? LNode.List(cond, then) : LNode.List(cond, then, @else));
			return F.Call(kw, parts, startIndex, then.Range.EndIndex);
		}
	
	
		private LNode SwitchStmt(int startIndex)
		{
			var kw = Match((int) TT.Switch);
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = BracedBlock();
			// line 2096
			var expr = SingleExprInside(p, "switch (...)");
			return F.Call(kw, expr, block, startIndex, block.Range.EndIndex);
		}
	
	
		// using, lock, fixed --------------------------------------------------
		private LNode UsingStmt(int startIndex)
		{
			var kw = MatchAny();
			var p = MatchAny();
			Match((int) TT.RParen);
			var block = Stmt();
			// line 2106
			var expr = SingleExprInside(p, "using (...)");
			return F.Call(S.UsingStmt, expr, block, startIndex, block.Range.EndIndex, kw.StartIndex, kw.EndIndex);
		}
	
	
		private LNode LockStmt(int startIndex)
		{
			var kw = MatchAny();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			// line 2114
			var expr = SingleExprInside(p, "lock (...)");
			return F.Call(kw, expr, block, startIndex, block.Range.EndIndex);
		}
	
	
		private LNode FixedStmt(int startIndex)
		{
			var kw = MatchAny();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			// line 2122
			var expr = SingleExprInside(p, "fixed (...)", true);
			return F.Call(kw, expr, block, startIndex, block.Range.EndIndex);
		}
	
	
		// try -----------------------------------------------------------------
		private LNode TryStmt(int startIndex)
		{
			TokenType la0, la1;
			LNode handler = default(LNode);
			var trykw = MatchAny();
			var header = Stmt();
			// line 2131
			var parts = new VList<LNode> { 
				header
			};
			LNode varExpr;
			LNode whenExpr;
			// Line 2134: greedy(TT.Catch (TT.LParen TT.RParen / ) (&{Is($LI, @@when)} TT.ContextualKeyword TT.LParen TT.RParen / ) Stmt)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Catch) {
					var kw = MatchAny();
					// Line 2135: (TT.LParen TT.RParen / )
					la0 = LA0;
					if (la0 == TT.LParen) {
						la1 = LA(1);
						if (la1 == TT.RParen) {
							var p = MatchAny();
							Skip();
							// line 2135
							varExpr = SingleExprInside(p, "catch (...)", true);
						} else
							// line 2136
							varExpr = MissingHere();
					} else
						// line 2136
						varExpr = MissingHere();
					// Line 2137: (&{Is($LI, @@when)} TT.ContextualKeyword TT.LParen TT.RParen / )
					la0 = LA0;
					if (la0 == TT.ContextualKeyword) {
						if (Is(0, sy_when)) {
							la1 = LA(1);
							if (la1 == TT.LParen) {
								Skip();
								var c = MatchAny();
								Match((int) TT.RParen);
								// line 2138
								whenExpr = SingleExprInside(c, "when (...)");
							} else
								// line 2139
								whenExpr = MissingHere();
						} else
							// line 2139
							whenExpr = MissingHere();
					} else
						// line 2139
						whenExpr = MissingHere();
					handler = Stmt();
					// line 2141
					parts.Add(F.Call(kw, LNode.List(varExpr, whenExpr, handler), kw.StartIndex, handler.Range.EndIndex));
				} else
					break;
			}
			// Line 2144: greedy(TT.Finally Stmt)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Finally) {
					var kw = MatchAny();
					handler = Stmt();
					// line 2145
					parts.Add(F.Call(kw, handler, kw.StartIndex, handler.Range.EndIndex));
				} else
					break;
			}
			// line 2148
			var result = F.Call(trykw, parts, startIndex, parts.Last.Range.EndIndex);
			if (parts.Count == 1) {
				Error(result, "'try': At least one 'catch' or 'finally' clause is required");
			}
			return result;
		}
	
	
		// ---------------------------------------------------------------------
		// ExprList and StmtList -----------------------------------------------
		// ---------------------------------------------------------------------
	
		LNode ExprOrNull(bool allowUnassignedVarDecl = false)
		{
			TokenType la0;
			LNode result = default(LNode);
			// Line 2162: greedy(ExprStart)?
			la0 = LA0;
			if (ReturnBreakContinueThrow_set0.Contains((int) la0))
				result = ExprStart(allowUnassignedVarDecl);
			return result;
		}
	
		LNode ExprOpt(bool allowUnassignedVarDecl = false)
		{
			LNode result = default(LNode);
			result = ExprOrNull(allowUnassignedVarDecl);
			result = result ?? MissingHere();
			return result;
		}
		static readonly HashSet<int> ExprList_set0 = NewSet((int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.AttrKeyword, (int) TT.Base, (int) TT.Break, (int) TT.Checked, (int) TT.Comma, (int) TT.ContextualKeyword, (int) TT.Continue, (int) TT.Default, (int) TT.Delegate, (int) TT.Dot, (int) TT.DotDot, (int) TT.Forward, (int) TT.Goto, (int) TT.Id, (int) TT.IncDec, (int) TT.Is, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LinqKeyword, (int) TT.Literal, (int) TT.LParen, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.Operator, (int) TT.Power, (int) TT.Return, (int) TT.Semicolon, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.Switch, (int) TT.This, (int) TT.Throw, (int) TT.TypeKeyword, (int) TT.Typeof, (int) TT.Unchecked);
	
		void ExprList(ref VList<LNode> list, bool allowTrailingComma = false, bool allowUnassignedVarDecl = false)
		{
			TokenType la0, la1;
			// Line 2175: nongreedy(ExprOpt (TT.Comma &{allowTrailingComma} EOF / TT.Comma ExprOpt)*)?
			la0 = LA0;
			if (la0 == EOF || la0 == TT.Semicolon)
				;
			else {
				list.Add(ExprOpt(allowUnassignedVarDecl));
				// Line 2176: (TT.Comma &{allowTrailingComma} EOF / TT.Comma ExprOpt)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						la1 = LA(1);
						if (la1 == EOF) {
							if (allowTrailingComma) {
								Skip();
								Skip();
							} else
								goto match2;
						} else if (ExprList_set0.Contains((int) la1))
							goto match2;
						else
							goto error;
					} else if (la0 == EOF || la0 == TT.Semicolon)
						break;
					else
						goto error;
					continue;
				match2:
					{
						Skip();
						list.Add(ExprOpt(allowUnassignedVarDecl));
					}
					continue;
				error:
					{
						// line 2178
						Error("'{0}': Syntax error in expression list", CurrentTokenText());
						MatchExcept((int) TT.Comma);
						// Line 2179: (~(EOF|TT.Comma))*
						for (;;) {
							la0 = LA0;
							if (!(la0 == (TokenType) EOF || la0 == TT.Comma))
								Skip();
							else
								break;
						}
					}
				}
			}
		}
	
		void ArgList(ref VList<LNode> list)
		{
			TokenType la0;
			// Line 2187: nongreedy(ExprOpt (TT.Comma ExprOpt)*)?
			la0 = LA0;
			if (la0 == EOF)
				;
			else {
				list.Add(ExprOpt(true));
				// Line 2188: (TT.Comma ExprOpt)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						Skip();
						list.Add(ExprOpt(true));
					} else if (la0 == EOF)
						break;
					else {
						// line 2189
						Error("Syntax error in argument list");
						// Line 2189: (~(EOF|TT.Comma))*
						for (;;) {
							la0 = LA0;
							if (!(la0 == (TokenType) EOF || la0 == TT.Comma))
								Skip();
							else
								break;
						}
					}
				}
			}
			Skip();
		}
	
		LNode InitializerExpr()
		{
			TokenType la0, la2;
			Token eq = default(Token);
			LNode result = default(LNode);
			// Line 2196: ( TT.LBrace TT.RBrace / TT.LBrack TT.RBrack TT.Set ExprStart / ExprOpt )
			la0 = LA0;
			if (la0 == TT.LBrace) {
				la2 = LA(2);
				if (la2 == (TokenType) EOF || la2 == TT.Comma) {
					var lb = MatchAny();
					var rb = Match((int) TT.RBrace);
					// line 2198
					var exprs = InitializerListInside(lb);
					result = F.Call(S.Braces, exprs, lb.StartIndex, rb.EndIndex, lb.StartIndex, lb.EndIndex, NodeStyle.Expression);
				} else
					result = ExprOpt(false);
			} else if (la0 == TT.LBrack) {
				la2 = LA(2);
				if (la2 == TT.Set) {
					var lb = MatchAny();
					Match((int) TT.RBrack);
					eq = MatchAny();
					var e = ExprStart(false);
					// line 2202
					result = F.Call(S.InitializerAssignment, ExprListInside(lb).Add(e), lb.StartIndex, e.Range.EndIndex, eq.StartIndex, eq.EndIndex);
				} else
					result = ExprOpt(false);
			} else
				result = ExprOpt(false);
			return result;
		}
		static readonly HashSet<int> InitializerList_set0 = NewSet((int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.AttrKeyword, (int) TT.Base, (int) TT.Break, (int) TT.Checked, (int) TT.Comma, (int) TT.ContextualKeyword, (int) TT.Continue, (int) TT.Default, (int) TT.Delegate, (int) TT.Dot, (int) TT.DotDot, (int) TT.Forward, (int) TT.Goto, (int) TT.Id, (int) TT.IncDec, (int) TT.Is, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LinqKeyword, (int) TT.Literal, (int) TT.LParen, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.Operator, (int) TT.Power, (int) TT.Return, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.Switch, (int) TT.This, (int) TT.Throw, (int) TT.TypeKeyword, (int) TT.Typeof, (int) TT.Unchecked);
	
		// Used for new int[][] { ... } or int[][] x = { ... }
		void InitializerList(ref VList<LNode> list)
		{
			TokenType la0, la1;
			// Line 2209: nongreedy(InitializerExpr (TT.Comma EOF / TT.Comma InitializerExpr)*)?
			la0 = LA0;
			if (la0 == EOF)
				;
			else {
				list.Add(InitializerExpr());
				// Line 2210: (TT.Comma EOF / TT.Comma InitializerExpr)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						la1 = LA(1);
						if (la1 == EOF) {
							Skip();
							Skip();
						} else if (InitializerList_set0.Contains((int) la1)) {
							Skip();
							list.Add(InitializerExpr());
						} else
							goto error;
					} else if (la0 == EOF)
						break;
					else
						goto error;
					continue;
				error:
					{
						// line 2212
						Error("Syntax error in initializer list");
						// Line 2212: (~(EOF|TT.Comma))*
						for (;;) {
							la0 = LA0;
							if (!(la0 == (TokenType) EOF || la0 == TT.Comma))
								Skip();
							else
								break;
						}
					}
				}
			}
			Skip();
		}
	
		void StmtList(ref VList<LNode> list)
		{
			TokenType la0;
			// Line 2217: (~(EOF) => Stmt)*
			for (;;) {
				la0 = LA0;
				if (la0 != (TokenType) EOF)
					list.Add(Stmt());
				else
					break;
			}
			Skip();
		}
		static readonly HashSet<int> TypeSuffixOpt_Test0_set0 = NewSet((int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.ContextualKeyword, (int) TT.Forward, (int) TT.Id, (int) TT.IncDec, (int) TT.LBrace, (int) TT.Literal, (int) TT.LParen, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.Sub, (int) TT.Substitute, (int) TT.TypeKeyword);
	
		private bool Try_TypeSuffixOpt_Test0(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return TypeSuffixOpt_Test0();
		}
		private bool TypeSuffixOpt_Test0()
		{
			// Line 279: ((TT.Add|TT.AndBits|TT.At|TT.ContextualKeyword|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.New|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | LinqKeywordAsId)
			switch (LA0) {
			case TT.Add: case TT.AndBits: case TT.At: case TT.ContextualKeyword:
			case TT.Forward: case TT.Id: case TT.IncDec: case TT.LBrace:
			case TT.Literal: case TT.LParen: case TT.Mul: case TT.New:
			case TT.Not: case TT.NotBits: case TT.Sub: case TT.Substitute:
			case TT.TypeKeyword:
				if (!TryMatch(TypeSuffixOpt_Test0_set0))
					return false;
				break;
			default:
				if (!Scan_LinqKeywordAsId())
					return false;
				break;
			}
			return true;
		}
	
		private bool Try_ExprInParensAuto_Test0(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return ExprInParensAuto_Test0();
		}
		private bool ExprInParensAuto_Test0()
		{
			if (!Scan_ExprInParens(true))
				return false;
			if (!TryMatch((int) TT.LambdaArrow, (int) TT.Set))
				return false;
			return true;
		}
		static readonly HashSet<int> AtomOrTypeParamExpr_Test0_set0 = NewSet((int) TT.ContextualKeyword, (int) TT.Id, (int) TT.LinqKeyword);
	
		private bool Try_AtomOrTypeParamExpr_Test0(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return AtomOrTypeParamExpr_Test0();
		}
		private bool AtomOrTypeParamExpr_Test0()
		{
			if (!Scan_IdWithOptionalTypeParams(false))
				return false;
			if (!TryMatchExcept(AtomOrTypeParamExpr_Test0_set0))
				return false;
			return true;
		}
		static readonly HashSet<int> PrefixExpr_Test0_set0 = NewSet((int) TT.Add, (int) TT.AndBits, (int) TT.BQString, (int) TT.Dot, (int) TT.Mul, (int) TT.Sub);
	
		private bool Try_PrefixExpr_Test0(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return PrefixExpr_Test0();
		}
		private bool PrefixExpr_Test0()
		{
			// Line 606: ((TT.Add|TT.AndBits|TT.BQString|TT.Dot|TT.Mul|TT.Sub) | TT.IncDec TT.LParen)
			switch (LA0) {
			case TT.Add: case TT.AndBits: case TT.BQString: case TT.Dot:
			case TT.Mul: case TT.Sub:
				if (!TryMatch(PrefixExpr_Test0_set0))
					return false;
				break;
			default:
				{
					if (!TryMatch((int) TT.IncDec))
						return false;
					if (!TryMatch((int) TT.LParen))
						return false;
				}
				break;
			}
			return true;
		}
	
		private bool Try_IdStmt_Test0(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return IdStmt_Test0();
		}
		private bool IdStmt_Test0()
		{
			if (!Scan_DataType())
				return false;
			if (!TryMatch((int) TT.This))
				return false;
			return true;
		}
	
		private bool Try_OtherStmt_Test0(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return OtherStmt_Test0();
		}
		private bool OtherStmt_Test0()
		{
			if (!TryMatch((int) TT.NotBits))
				return false;
			if (!TryMatch((int) TT.ContextualKeyword, (int) TT.Id, (int) TT.LinqKeyword, (int) TT.This))
				return false;
			if (!TryMatch((int) TT.LParen))
				return false;
			if (!TryMatch((int) TT.RParen))
				return false;
			if (!TryMatch((int) TT.LBrace))
				return false;
			if (!TryMatch((int) TT.RBrace))
				return false;
			return true;
		}
	
		private bool Try_Constructor_Test0(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return Constructor_Test0();
		}
		private bool Constructor_Test0()
		{
			if (!TryMatch((int) TT.LParen))
				return false;
			if (!TryMatch((int) TT.RParen))
				return false;
			if (!TryMatch((int) TT.LBrace, (int) TT.Semicolon))
				return false;
			return true;
		}
	
		private bool Try_Constructor_Test1(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return Constructor_Test1();
		}
		private bool Constructor_Test1()
		{
			if (!TryMatch((int) TT.LParen))
				return false;
			if (!TryMatch((int) TT.RParen))
				return false;
			if (!TryMatch((int) TT.LBrace, (int) TT.Semicolon))
				return false;
			return true;
		}
	
		private bool Try_Constructor_Test2(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return Constructor_Test2();
		}
		private bool Constructor_Test2()
		{
			if (!TryMatch((int) TT.LParen))
				return false;
			if (!TryMatch((int) TT.RParen))
				return false;
			if (!TryMatch((int) TT.Colon))
				return false;
			return true;
		}
	
		private bool Try_BlockCallStmt_Test0(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return BlockCallStmt_Test0();
		}
		private bool BlockCallStmt_Test0()
		{
			TokenType la0;
			// Line 1946: ( TT.LParen TT.RParen (TT.LBrace TT.RBrace | TT.Id) | TT.LBrace TT.RBrace | TT.Forward )
			la0 = LA0;
			if (la0 == TT.LParen) {
				if (!TryMatch((int) TT.LParen))
					return false;
				if (!TryMatch((int) TT.RParen))
					return false;
				// Line 1946: (TT.LBrace TT.RBrace | TT.Id)
				la0 = LA0;
				if (la0 == TT.LBrace) {
					if (!TryMatch((int) TT.LBrace))
						return false;
					if (!TryMatch((int) TT.RBrace))
						return false;
				} else if (!TryMatch((int) TT.Id))
					return false;
			} else if (la0 == TT.LBrace) {
				if (!TryMatch((int) TT.LBrace))
					return false;
				if (!TryMatch((int) TT.RBrace))
					return false;
			} else if (!TryMatch((int) TT.Forward))
				return false;
			return true;
		}
	}
}