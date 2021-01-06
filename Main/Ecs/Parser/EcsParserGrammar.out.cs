// Generated from EcsParserGrammar.les by LeMP custom tool. LeMP version: 2.9.0.0
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
	using S = EcsCodeSymbols;
	using EP = EcsPrecedence;

	// 0162=Unreachable code detected; 0642=Possibly mistaken empty statement
	
	#pragma warning disable 162, 642
	

	partial class EcsParser
	{
		static readonly Symbol sy_await = (Symbol) "await", sy_from = (Symbol) "from", sy_with = (Symbol) "with", sy_when = (Symbol) "when", sy_where = (Symbol) "where", sy_or = (Symbol) "or", sy_and = (Symbol) "and", sy_not = (Symbol) "not", sy_var = (Symbol) "var", sy_let = (Symbol) "let", sy_join = (Symbol) "join", sy_orderby = (Symbol) "orderby", sy_group = (Symbol) "group", sy_into = (Symbol) "into", sy_on = (Symbol) "on", sy_equals = (Symbol) "equals", sy__numequals = (Symbol) "#equals", sy_ascending = (Symbol) "ascending", sy_descending = (Symbol) "descending", sy_select = (Symbol) "select", sy_by = (Symbol) "by", sy_trait = (Symbol) "trait", sy_alias = (Symbol) "alias", sy_assembly = (Symbol) "assembly", sy_module = (Symbol) "module";

		// Used to resolve the constructor ambiguity, in which "Foo()" could be a
		// constructor declaration or a method call. _spaceName is the name of the
		// current space, or #fn (S.Fn) when inside a method or constructor.
		Symbol _spaceName;

		// Inside a LINQ expression, certain ContextualKeywords are to be treated
		// as actual keywords. This flag enables that behavior.
		bool _insideLinqExpr;

		// Inside a constant pattern, or between `when` and `=>` in a `switch` expr.
		// Indicates that the `=>` operator cannot be used.
		bool _insidePattern = false;

		// Used to detect a specific ContextualKeyword; `@` renders it not-a-keyword
		bool Is(int li, Symbol value)
		{
			var lt = LT(li);
			return lt.Value == value && SourceFile.Text.TryGet(lt.StartIndex, '\0') != '@';
		}
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
		public LNode DataType(bool afterAs = false) {
			Token? majorDimension;
			var type = DataType(afterAs, out majorDimension);
			if ((majorDimension != null)) {
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

		// This is the same as ComplexId except that it is used in declaration 
		// locations such as the name of a class. The difference is that the 
		// type parameters can have [normal attributes] and in/out variance 
		// attrbutes. This matcher also helps match properties and therefore 
		// optionally allows names ending in 'this', such as 'IList<T>.this'
		LNode ComplexNameDecl() { return ComplexNameDecl(false, out bool _); }

		int count;	// hack allows Scan_TypeSuffixOpt() to compile
		public enum TypeParseMode { Normal = 0, AfterAs = 1, Pattern = 2 }
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
		//    (Foo[]) x; // data type: @'of(@`'[]`, Foo)
		//    (Foo[]).x; // indexer:   @`'[]`(Foo)
		// 5. Does "?" make a nullable type or a conditional operator?
		//    Foo<B> ? x = null;     // nullable type
		//    Foo<B> ? x = null : y; // conditional operator
		// 6. Contextual word operators (`with` in C# 9)
		//    Surprisingly, `(obj) with { A = 5 }` is a syntax error; `(obj) with` 
		//    is detected as a cast. This makes it relatively easy, actually; it 
		//    can be treated as a normal infix operator.
		/// Below lowest precedence
		public static readonly Precedence StartExpr = new Precedence(-100);
		
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

		// Attempts to parse, returning the parsed node on success or null if 
		// outer-level parse error(s) occurred; the out param result is never null, 
		// and in case of success it is the same as the return value. Error 
		// handling is tricky... we fail if there are errors at the current level, 
		// not if there are errors in parenthesized subexpressions.
		LNode TryParseNonVarDeclExpr(LNodeList attrs, out TentativeResult result)
		{
			result = new TentativeResult(InputPosition);
			{
				var old_10 = _tentativeErrors;
				_tentativeErrors = new TentativeState(true);
				try {

					bool failed = false;

					{
						result.Result = SubExpr(StartExpr).PlusAttrs(attrs);
						// A var decl like "A B" looks like an expression followed by
						// an identifier. To cede victory to VarDeclExpr, detect that
						// the expression didn't end properly and emit an error.
						if (LA0 != EOF && LA0 != TT.Semicolon && LA0 != TT.Comma && !(LA0 == TT.LinqKeyword && _insideLinqExpr)) {
							failed = true;
						}
					}
					result.Errors = _tentativeErrors.DeferredErrors;
					result.InputPosition = InputPosition;
					if (failed || _tentativeErrors.LocalErrorCount != 0) {
						// error(s) occurred.
						InputPosition = result.OldPosition;
						return null;
					}
				} finally { _tentativeErrors = old_10; }
			}
			// can't Commit until original _tentativeErrors is restored
			return Commit(result);
		}

		// Attempts to parse, returning the parsed node on success or null if 
		// outer-level parse error(s) occurred; the out param result is never null, 
		// and in case of success it is the same as the return value. Error 
		// handling is tricky... we fail if there are errors at the current level, 
		// not if there are errors in parenthesized subexpressions.
		LNode TryParseVarDecl(LNodeList attrs, out TentativeResult result, bool allowUnassigned = false)
		{
			result = new TentativeResult(InputPosition);
			{
				var old_11 = _tentativeErrors;
				_tentativeErrors = new TentativeState(true);
				try {

					bool failed = false;

					{
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
					}
					result.Errors = _tentativeErrors.DeferredErrors;
					result.InputPosition = InputPosition;
					if (failed || _tentativeErrors.LocalErrorCount != 0) {
						// error(s) occurred.
						InputPosition = result.OldPosition;
						return null;
					}
				} finally { _tentativeErrors = old_11; }
			}
			// can't Commit until original _tentativeErrors is restored
			return Commit(result);
		}

		LNode Commit(TentativeResult result)
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
			Symbol name = type.Name;
			if ((name == _var) && type.IsId 
			&& (type.BaseStyle & NodeStyle.VerbatimId) == 0)
			{	// the old test was: (rng=type.Range).Source.Text.TryGet(rng.StartIndex, '\0') != '@'
				type = type.WithName(S.Missing);
			}
		}

		LNodeList SubpatternsIn(Token opener, bool inParens = false) {
			G.Verify(Down(opener));
			return Up(LA0 != EOF ? SubPatterns(inParens) : LNode.List());
		}

		bool IsNamedArg(LNode node) { return node.Calls(S.NamedArg, 2) && node.BaseStyle == NodeStyle.Operator; }
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
			TT.Set, TT.LBrace, TT.LParen, TT.LBrack, TT.LambdaArrow, TT.Forward, TT.Semicolon, TT.Comma, TT.At	// @{...} token literal (property)
		};
		static readonly HashSet<TT> ExpectedAfterTypeAndName_InExpr = new HashSet<TT> { 
			TT.Set, TT.LBrace, TT.LambdaArrow, TT.Forward, TT.Semicolon, TT.Comma, TT.EOF, TT.At	// @{...} token literal (property)
			, TT.Colon	// case Type Name:
		};
		static readonly HashSet<TT>[] ExpectedAfterTypeAndName = new HashSet<TT>[] { 
			ExpectedAfterTypeAndName_InExpr, ExpectedAfterTypeAndName_InStmt
		};

		StmtCat DetectStatementCategoryAndAddWordAttributes(out int wordAttrCount, ref LNodeList attrs, DetectionMode mode)
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
				if (word.Kind == TokenKind.AttrKeyword) {
					wordAttr = F.Id(word);
				} else if ((word.Type() == TT.New)) {
					wordAttr = F.Id(S.NewAttribute);
				} else {
					wordAttr = F.Attr(_triviaWordAttribute, F.Id("#" + word.Value.ToString(), word.StartIndex, word.EndIndex));
				}
				attrs.Add(wordAttr);
			}
			return cat;
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
			// Detect an array type, which has the form @'of(@`'[,]`, Type)
			return type.Calls(S.Of, 2) && S.IsArrayKeyword(type.Args[0].Name);
		}

		LNode ArgList(Token lp, Token rp)
		{
			var list = new LNodeList();
			if ((Down(lp.Children))) {
				ArgList(ref list);
				Up();
			}
			return F.CallBrackets(S.AltList, lp, list, rp);
		}
		int ColumnOf(int index)
		{
			return _sourceFile.IndexToLine(index).Column;
		}
		
		// break, continue, return, throw --------------------------------------
			// (these can be expressions as well as statements)
		LNode MissingHere()
		{
			var i = GetTextPosition(InputPosition);
			return F.Id(S.Missing, i, i);
		}

		// A potential LINQ keyword that, it turns out, can be treated as an identifier
		// because we are not in the context of a LINQ expression.
		private Token LinqKeywordAsId()
		{
			Token result = default(Token);
			Check(!_insideLinqExpr, "Did not expect _insideLinqExpr");
			result = Match((int) TT.LinqKeyword);
			return result;
		}
		
		// ---------------------------------------------------------------------
			// -- Type names and complex identifiers -------------------------------
			// ---------------------------------------------------------------------

		// A potential LINQ keyword that, it turns out, can be treated as an identifier
		// because we are not in the context of a LINQ expression.
		private bool Scan_LinqKeywordAsId()
		{
			if (_insideLinqExpr)
				return false;
			if (!TryMatch((int) TT.LinqKeyword))
				return false;
			return true;
		}
		
		// ---------------------------------------------------------------------
			// -- Type names and complex identifiers -------------------------------
			// ---------------------------------------------------------------------

		
		LNode DataType(bool afterAs, out Token? majorDimensionBrack)
		{
			LNode result = default(LNode);
			// Line 154: (ComplexId | TupleType)
			switch (LA0) {
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
			case TT.Substitute: case TT.TypeKeyword:
				result = ComplexId();
				break;
			case TT.LParen:
				result = TupleType();
				break;
			default:
				{
					// line 156
					result = Error("Type expected");
				}
				break;
			}
			TypeSuffixOpt(afterAs ? TypeParseMode.AfterAs : TypeParseMode.Normal, out majorDimensionBrack, ref result);
			return result;
		}

		bool Try_Scan_DataType(int lookaheadAmt, bool afterAs = false) {
			using (new SavePosition(this, lookaheadAmt))
				return Scan_DataType(afterAs);
		}
		bool Scan_DataType(bool afterAs = false)
		{
			// Line 154: (ComplexId | TupleType)
			switch (LA0) {
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
			case TT.Substitute: case TT.TypeKeyword:
				if (!Scan_ComplexId())
					return false;
				break;
			case TT.LParen:
				if (!Scan_TupleType())
					return false;
				break;
			default:
				return false;
			}
			if (!Scan_TypeSuffixOpt(afterAs ? TypeParseMode.AfterAs : TypeParseMode.Normal))
				return false;
			return true;
		}

		private 
		LNode TupleType()
		{
			Token lit_lpar = default(Token);
			Token lit_rpar = default(Token);
			Check(Down(0) && Up(Scan_TupleTypeList() && LA0 == EOF), "Expected Down($LI) && Up(Scan_TupleTypeList() && LA0 == EOF)");
			lit_lpar = MatchAny();
			lit_rpar = Match((int) TT.RParen);
			// line 169
			Down(lit_lpar);
			var typeList = Up(TupleTypeList());
			return F.Of(S.Tuple, typeList, lit_lpar.StartIndex, lit_rpar.EndIndex);
		}
		private 
		bool Scan_TupleType()
		{
			if (!(Down(0) && Up(Scan_TupleTypeList() && LA0 == EOF)))
				return false;
			Skip();
			if (!TryMatch((int) TT.RParen))
				return false;
			return true;
		}

		LNodeList TupleTypeList()
		{
			TokenType la0;
			// line 176
			var items = LNode.List();
			items.Add(TupleTypeItem());
			Match((int) TT.Comma);
			// Line 178: (TupleTypeItem (TT.Comma TupleTypeItem)*)?
			switch (LA0) {
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.LParen:
			case TT.Operator: case TT.Substitute: case TT.TypeKeyword:
				{
					items.Add(TupleTypeItem());
					// Line 178: (TT.Comma TupleTypeItem)*
					for (;;) {
						la0 = LA0;
						if (la0 == TT.Comma) {
							Skip();
							items.Add(TupleTypeItem());
						} else
							break;
					}
				}
				break;
			}
			// line 179
			return items;
		}

		bool Try_Scan_TupleTypeList(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return Scan_TupleTypeList();
		}
		bool Scan_TupleTypeList()
		{
			TokenType la0;
			if (!Scan_TupleTypeItem())
				return false;
			if (!TryMatch((int) TT.Comma))
				return false;
			// Line 178: (TupleTypeItem (TT.Comma TupleTypeItem)*)?
			switch (LA0) {
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.LParen:
			case TT.Operator: case TT.Substitute: case TT.TypeKeyword:
				{
					if (!Scan_TupleTypeItem())
						return false;
					// Line 178: (TT.Comma TupleTypeItem)*
					for (;;) {
						la0 = LA0;
						if (la0 == TT.Comma) {
							Skip();
							if (!Scan_TupleTypeItem())
								return false;
						} else
							break;
					}
				}
				break;
			}
			return true;
		}

		LNode TupleTypeItem()
		{
			LNode result = default(LNode);
			result = DataType();
			// Line 183: (IdAtom)?
			switch (LA0) {
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
			case TT.Substitute: case TT.TypeKeyword:
				{
					var id = IdAtom();
					// line 183
					result = F.Var(result, id, null, result.Range.StartIndex, id.Range.EndIndex);
				}
				break;
			}
			return result;
		}
		bool Scan_TupleTypeItem()
		{
			if (!Scan_DataType())
				return false;
			// Line 183: (IdAtom)?
			switch (LA0) {
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
			case TT.Substitute: case TT.TypeKeyword:
				if (!Scan_IdAtom())
					return false;
				break;
			}
			return true;
		}

		// Complex identifier, e.g. Foo.Bar or Foo<x, y>
		// http://loyc-etc.blogspot.ca/2013/12/bogus-ambiguity-warnings-in-lllpg.html
		LNode ComplexId(bool declContext = false)
		{
			TokenType la0;
			LNode result = default(LNode);
			result = IdWithOptionalTypeParams(declContext);
			// Line 191: (TT.ColonColon IdWithOptionalTypeParams)?
			la0 = LA0;
			if (la0 == TT.ColonColon) {
				switch (LA(1)) {
				case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
				case TT.Substitute: case TT.TypeKeyword:
					{
						// line 191
						if ((result.Calls(S.Of))) {
							Error("Type parameters cannot appear before '::' in a declaration or type name");
						}
						var op = MatchAny();
						var rhs = IdWithOptionalTypeParams(declContext);
						// line 193
						result = F.CallInfixOp(result, op, rhs);
					}
					break;
				}
			}
			// Line 195: (TT.Dot IdWithOptionalTypeParams)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Dot) {
					switch (LA(1)) {
					case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
					case TT.Substitute: case TT.TypeKeyword:
						{
							var op = MatchAny();
							var rhs = IdWithOptionalTypeParams(declContext);
							// line 196
							result = F.Dot(result, op, rhs);
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

		// Complex identifier, e.g. Foo.Bar or Foo<x, y>
		// http://loyc-etc.blogspot.ca/2013/12/bogus-ambiguity-warnings-in-lllpg.html
		bool Scan_ComplexId(bool declContext = false)
		{
			TokenType la0;
			if (!Scan_IdWithOptionalTypeParams(declContext))
				return false;
			// Line 191: (TT.ColonColon IdWithOptionalTypeParams)?
			la0 = LA0;
			if (la0 == TT.ColonColon) {
				switch (LA(1)) {
				case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
				case TT.Substitute: case TT.TypeKeyword:
					{
						Skip();
						if (!Scan_IdWithOptionalTypeParams(declContext))
							return false;
					}
					break;
				}
			}
			// Line 195: (TT.Dot IdWithOptionalTypeParams)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Dot) {
					switch (LA(1)) {
					case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
					case TT.Substitute: case TT.TypeKeyword:
						{
							Skip();
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
			// Line 202: (TParams)?
			la0 = LA0;
			if (la0 == TT.LT) {
				switch (LA(1)) {
				case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
				case TT.Substitute: case TT.TypeKeyword:
					TParams(declarationContext, ref result);
					break;
				case TT.LParen:
					{
						if (Down(1) && Up(Scan_TupleTypeList() && LA0 == EOF))
							TParams(declarationContext, ref result);
					}
					break;
				case TT.AttrKeyword: case TT.Comma: case TT.GT: case TT.In:
				case TT.LBrack:
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
			// Line 202: (TParams)?
			do {
				la0 = LA0;
				if (la0 == TT.LT) {
					switch (LA(1)) {
					case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
					case TT.Substitute: case TT.TypeKeyword:
						goto matchTParams;
					case TT.LParen:
						{
							if (Down(1) && Up(Scan_TupleTypeList() && LA0 == EOF))
								goto matchTParams;
						}
						break;
					case TT.AttrKeyword: case TT.Comma: case TT.GT: case TT.In:
					case TT.LBrack:
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

		// identifier, $identifier, $(expr), operator+ (or another operator name), or a primitive type (int, string)
		LNode IdAtom()
		{
			// line 207
			LNode r;
			// Line 208: ( TT.Substitute Atom | TT.Operator AnyOperator | (TT.ContextualKeyword|TT.Id|TT.TypeKeyword) | LinqKeywordAsId )
			switch (LA0) {
			case TT.Substitute:
				{
					var t = MatchAny();
					var e = Atom();
					// line 208
					e = AutoRemoveParens(e);
					// line 209
					r = F.CallPrefixOp(t, e, S.Substitute);
				}
				break;
			case TT.Operator:
				{
					var op = MatchAny();
					var t = AnyOperator();
					// line 211
					r = F.Attr(_triviaUseOperatorKeyword, F.Id((Symbol) t.Value, op.StartIndex, t.EndIndex));
				}
				break;
			case TT.ContextualKeyword: case TT.Id: case TT.TypeKeyword:
				{
					var t = MatchAny();
					// line 213
					r = F.Id(t);
				}
				break;
			default:
				{
					var t = LinqKeywordAsId();
					// line 215
					r = F.Id(t);
				}
				break;
			}
			// line 217
			return r;
		}

		// identifier, $identifier, $(expr), operator+ (or another operator name), or a primitive type (int, string)
		bool Scan_IdAtom()
		{
			// Line 208: ( TT.Substitute Atom | TT.Operator AnyOperator | (TT.ContextualKeyword|TT.Id|TT.TypeKeyword) | LinqKeywordAsId )
			switch (LA0) {
			case TT.Substitute:
				{
					Skip();
					if (!Scan_Atom())
						return false;
				}
				break;
			case TT.Operator:
				{
					Skip();
					if (!Scan_AnyOperator())
						return false;
				}
				break;
			case TT.ContextualKeyword: case TT.Id: case TT.TypeKeyword:
				Skip();
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
			// line 234
			LNodeList list = new LNodeList(r);
			// line 235
			int endIndex;
			// Line 236: ( TT.LT TParamDeclOrDataType (TT.Comma TParamDeclOrDataType)* TT.GT | TT.Dot TT.LBrack TT.RBrack | TT.Not TT.LParen TT.RParen | TT.Not IdWithOptionalTypeParams )
			la0 = LA0;
			if (la0 == TT.LT) {
				op = MatchAny();
				list.Add(TParamDeclOrDataType(declContext));
				// Line 237: (TT.Comma TParamDeclOrDataType)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						Skip();
						list.Add(TParamDeclOrDataType(declContext));
					} else
						break;
				}
				var end = Match((int) TT.GT);
				// line 237
				endIndex = end.EndIndex;
			} else if (la0 == TT.Dot) {
				op = MatchAny();
				var t = Match((int) TT.LBrack);
				var end = Match((int) TT.RBrack);
				// line 238
				list = AppendExprsInside(t, list);
				endIndex = end.EndIndex;
			} else {
				la1 = LA(1);
				if (la1 == TT.LParen) {
					op = Match((int) TT.Not);
					var t = MatchAny();
					var end = Match((int) TT.RParen);
					// line 239
					list = AppendExprsInside(t, list);
					endIndex = end.EndIndex;
				} else {
					op = Match((int) TT.Not);
					list.Add(IdWithOptionalTypeParams(declContext));
					// line 240
					endIndex = list.Last.Range.EndIndex;
				}
			}
			// line 243
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
			// Line 236: ( TT.LT TParamDeclOrDataType (TT.Comma TParamDeclOrDataType)* TT.GT | TT.Dot TT.LBrack TT.RBrack | TT.Not TT.LParen TT.RParen | TT.Not IdWithOptionalTypeParams )
			la0 = LA0;
			if (la0 == TT.LT) {
				Skip();
				if (!Scan_TParamDeclOrDataType(declContext))
					return false;
				// Line 237: (TT.Comma TParamDeclOrDataType)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						Skip();
						if (!Scan_TParamDeclOrDataType(declContext))
							return false;
					} else
						break;
				}
				if (!TryMatch((int) TT.GT))
					return false;
			} else if (la0 == TT.Dot) {
				Skip();
				if (!TryMatch((int) TT.LBrack))
					return false;
				if (!TryMatch((int) TT.RBrack))
					return false;
			} else {
				la1 = LA(1);
				if (la1 == TT.LParen) {
					if (!TryMatch((int) TT.Not))
						return false;
					Skip();
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
			// Line 250: ( DataType / &{declarationContext} NormalAttributes TParamAttributeKeywords IdAtom / {..} )
			switch (LA0) {
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.LParen:
			case TT.Operator: case TT.Substitute: case TT.TypeKeyword:
				result = DataType();
				break;
			case TT.AttrKeyword: case TT.In: case TT.LBrack:
				{
					Check(declarationContext, "Expected declarationContext");
					// line 252
					LNodeList attrs = default(LNodeList);
					int startIndex = GetTextPosition(InputPosition);
					NormalAttributes(ref attrs);
					TParamAttributeKeywords(ref attrs);
					result = IdAtom();
					// line 256
					result = result.WithAttrs(attrs);
				}
				break;
			default:
				// line 257
				result = MissingHere();
				break;
			}
			return result;
		}

		bool Try_Scan_TParamDeclOrDataType(int lookaheadAmt, bool declarationContext) {
			using (new SavePosition(this, lookaheadAmt))
				return Scan_TParamDeclOrDataType(declarationContext);
		}
		bool Scan_TParamDeclOrDataType(bool declarationContext)
		{
			// Line 250: ( DataType / &{declarationContext} NormalAttributes TParamAttributeKeywords IdAtom / {..} )
			switch (LA0) {
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.LParen:
			case TT.Operator: case TT.Substitute: case TT.TypeKeyword:
				if (!Scan_DataType())
					return false;
				break;
			case TT.AttrKeyword: case TT.In: case TT.LBrack:
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
			default:
				{ }
				break;
			}
			return true;
		}

		
		LNode ComplexNameDecl(bool thisAllowed, out bool hasThis)
		{
			TokenType la0, la1;
			LNode got_ComplexThisDecl = default(LNode);
			LNode result = default(LNode);
			result = ComplexId(declContext: true);
			// line 269
			hasThis = false;
			// Line 270: (TT.Dot ComplexThisDecl)?
			la0 = LA0;
			if (la0 == TT.Dot) {
				la1 = LA(1);
				if (la1 == TT.This) {
					var op = MatchAny();
					got_ComplexThisDecl = ComplexThisDecl(thisAllowed);
					// line 270
					hasThis = true;
					// line 271
					result = F.Dot(result, got_ComplexThisDecl, result.Range.StartIndex, got_ComplexThisDecl.Range.EndIndex, op.StartIndex, op.EndIndex, NodeStyle.Operator);
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
			if (!Scan_ComplexId(declContext: true))
				return false;
			// Line 270: (TT.Dot ComplexThisDecl)?
			la0 = LA0;
			if (la0 == TT.Dot) {
				la1 = LA(1);
				if (la1 == TT.This) {
					Skip();
					if (!Scan_ComplexThisDecl(thisAllowed))
						return false;
				}
			}
			return true;
		}
		static readonly HashSet<int> MethodOrPropertyName_set0 = NewSet((int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.At, (int) TT.Backslash, (int) TT.BQString, (int) TT.Colon, (int) TT.ColonColon, (int) TT.Compare, (int) TT.CompoundSet, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.Forward, (int) TT.GT, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LEGE, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.PipeArrow, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.QuickBindSet, (int) TT.Set, (int) TT.Sub, (int) TT.Substitute, (int) TT.XorBits);

		// The matcher for method and property names is similar to ComplexNameDecl but
		// can allow 'this' if it's a property, and 'operator true' if it's a method.
		LNode MethodOrPropertyName(bool thisAllowed, out bool hasThis)
		{
			TokenType la1;
			LNode result = default(LNode);
			Token tf = default(Token);
			// Line 279: ( ComplexNameDecl | ComplexThisDecl | TT.Operator &{LT($LI).Value `is` @bool} TT.Literal )
			switch (LA0) {
			case TT.Operator:
				{
					la1 = LA(1);
					if (MethodOrPropertyName_set0.Contains((int) la1))
						result = ComplexNameDecl(thisAllowed, out hasThis);
					else {
						Skip();
						Check(LT(0).Value is bool, "Expected 'true' or 'false'");
						tf = Match((int) TT.Literal);
						// line 282
						result = F.Attr(_triviaUseOperatorKeyword, F.Literal(tf));
						hasThis = false;
					}
				}
				break;
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Substitute:
			case TT.TypeKeyword:
				result = ComplexNameDecl(thisAllowed, out hasThis);
				break;
			default:
				{
					result = ComplexThisDecl(thisAllowed);
					// line 280
					hasThis = true;
				}
				break;
			}
			return result;
		}

		bool Try_Scan_MethodOrPropertyName(int lookaheadAmt, bool thisAllowed) {
			using (new SavePosition(this, lookaheadAmt))
				return Scan_MethodOrPropertyName(thisAllowed);
		}
		bool Scan_MethodOrPropertyName(bool thisAllowed)
		{
			TokenType la1;
			// Line 279: ( ComplexNameDecl | ComplexThisDecl | TT.Operator &{LT($LI).Value `is` @bool} TT.Literal )
			do {
				switch (LA0) {
				case TT.Operator:
					{
						la1 = LA(1);
						if (MethodOrPropertyName_set0.Contains((int) la1))
							goto matchComplexNameDecl;
						else {
							Skip();
							if (!(LT(0).Value is bool))
								return false;
							if (!TryMatch((int) TT.Literal))
								return false;
						}
					}
					break;
				case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Substitute:
				case TT.TypeKeyword:
					goto matchComplexNameDecl;
				default:
					if (!Scan_ComplexThisDecl(thisAllowed))
						return false;
					break;
				}
				break;
			matchComplexNameDecl:
				{
					if (!Scan_ComplexNameDecl(thisAllowed))
						return false;
				}
			} while (false);
			return true;
		}

		// `this` with optional <type arguments>
		LNode ComplexThisDecl(bool allowed)
		{
			TokenType la0;
			LNode result = default(LNode);
			// line 287
			if ((!allowed)) {
				Error("'this' is not allowed at this location.");
			}
			var t = Match((int) TT.This);
			// line 288
			result = F.Id(t);
			// Line 289: (TParams)?
			la0 = LA0;
			if (la0 == TT.Dot || la0 == TT.LT || la0 == TT.Not) {
				switch (LA(1)) {
				case TT.AttrKeyword: case TT.Comma: case TT.ContextualKeyword: case TT.GT:
				case TT.Id: case TT.In: case TT.LBrack: case TT.LinqKeyword:
				case TT.LParen: case TT.Operator: case TT.Substitute: case TT.TypeKeyword:
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
			// Line 289: (TParams)?
			la0 = LA0;
			if (la0 == TT.Dot || la0 == TT.LT || la0 == TT.Not) {
				switch (LA(1)) {
				case TT.AttrKeyword: case TT.Comma: case TT.ContextualKeyword: case TT.GT:
				case TT.Id: case TT.In: case TT.LBrack: case TT.LinqKeyword:
				case TT.LParen: case TT.Operator: case TT.Substitute: case TT.TypeKeyword:
					if (!Scan_TParams(true))
						return false;
					break;
				}
			}
			return true;
		}

		
		bool TypeSuffixOpt(TypeParseMode mode, out Token? dimensionBrack, ref LNode e)
		{
			TokenType la0, la1;
			// line 299
			int count;
			bool result = false;
			// line 300
			dimensionBrack = null;
			// Line 336: greedy( TT.QuestionMark (&{mode == TypeParseMode.Normal} | &!(((TT.Add|TT.AndBits|TT.At|TT.ContextualKeyword|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.New|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | LinqKeywordAsId))) | &{mode != TypeParseMode.Pattern} TT.Mul | &{mode != TypeParseMode.Pattern} TT.Power | &{(count = CountDims(LT($LI), @true)) > 0} TT.LBrack TT.RBrack greedy(&{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack)* )*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.QuestionMark) {
					if (mode == TypeParseMode.Normal)
						goto match1;
					else if (mode != TypeParseMode.Pattern || (count = CountDims(LT(1), true)) > 0) {
						if (!Try_TypeSuffixOpt_Test0(1))
							goto match1;
						else
							break;
					} else if (!Try_TypeSuffixOpt_Test0(1))
						goto match1;
					else
						break;
				} else if (la0 == TT.Mul) {
					if (mode != TypeParseMode.Pattern) {
						var t = MatchAny();
						// line 345
						e = F.Of(F.Id(t), e, e.Range.StartIndex, t.EndIndex);
						result = true;
					} else
						break;
				} else if (la0 == TT.Power) {
					if (mode != TypeParseMode.Pattern) {
						var t = MatchAny();
						// line 348
						var ptr = F.Id(S._Pointer, t.StartIndex, t.EndIndex);
						e = F.Of(ptr, F.Of(ptr, e, e.Range.StartIndex, t.EndIndex - 1), e.Range.StartIndex, t.EndIndex);
						result = true;
					} else
						break;
				} else if (la0 == TT.LBrack) {
					if ((count = CountDims(LT(0), true)) > 0) {
						la1 = LA(1);
						if (la1 == TT.RBrack) {
							// line 354
							var dims = InternalList<Pair<int, int>>.Empty;
							// line 355
							Token rb;
							var lb = MatchAny();
							rb = MatchAny();
							// line 356
							dims.Add(Pair.Create(count, rb.EndIndex));
							// Line 357: greedy(&{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack)*
							for (;;) {
								la0 = LA0;
								if (la0 == TT.LBrack) {
									if ((count = CountDims(LT(0), false)) > 0) {
										la1 = LA(1);
										if (la1 == TT.RBrack) {
											Skip();
											rb = MatchAny();
											// line 357
											dims.Add(Pair.Create(count, rb.EndIndex));
										} else
											break;
									} else
										break;
								} else
									break;
							}
							// line 359
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
					// Line 336: (&{mode == TypeParseMode.Normal} | &!(((TT.Add|TT.AndBits|TT.At|TT.ContextualKeyword|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.New|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | LinqKeywordAsId)))
					if (mode == TypeParseMode.Normal) { } else
						Check(!Try_TypeSuffixOpt_Test0(0), "Did not expect ((TT.Add|TT.AndBits|TT.At|TT.ContextualKeyword|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.New|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | LinqKeywordAsId)");
					// line 341
					e = F.Of(F.Id(t), e, e.Range.StartIndex, t.EndIndex);
					result = true;
				}
			}
			// line 368
			return result;
		}
		
		// =====================================================================
			// == Expressions ======================================================
			// =====================================================================

		bool Try_Scan_TypeSuffixOpt(int lookaheadAmt, TypeParseMode mode) {
			using (new SavePosition(this, lookaheadAmt))
				return Scan_TypeSuffixOpt(mode);
		}
		bool Scan_TypeSuffixOpt(TypeParseMode mode)
		{
			TokenType la0, la1;
			// Line 336: greedy( TT.QuestionMark (&{mode == TypeParseMode.Normal} | &!(((TT.Add|TT.AndBits|TT.At|TT.ContextualKeyword|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.New|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | LinqKeywordAsId))) | &{mode != TypeParseMode.Pattern} TT.Mul | &{mode != TypeParseMode.Pattern} TT.Power | &{(count = CountDims(LT($LI), @true)) > 0} TT.LBrack TT.RBrack greedy(&{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack)* )*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.QuestionMark) {
					if (mode == TypeParseMode.Normal)
						goto match1;
					else if (mode != TypeParseMode.Pattern || (count = CountDims(LT(1), true)) > 0) {
						if (!Try_TypeSuffixOpt_Test0(1))
							goto match1;
						else
							break;
					} else if (!Try_TypeSuffixOpt_Test0(1))
						goto match1;
					else
						break;
				} else if (la0 == TT.Mul) {
					if (mode != TypeParseMode.Pattern)
						Skip();
					else
						break;
				} else if (la0 == TT.Power) {
					if (mode != TypeParseMode.Pattern)
						Skip();
					else
						break;
				} else if (la0 == TT.LBrack) {
					if ((count = CountDims(LT(0), true)) > 0) {
						la1 = LA(1);
						if (la1 == TT.RBrack) {
							Skip();
							Skip();
							// Line 357: greedy(&{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack)*
							for (;;) {
								la0 = LA0;
								if (la0 == TT.LBrack) {
									if ((count = CountDims(LT(0), false)) > 0) {
										la1 = LA(1);
										if (la1 == TT.RBrack) {
											Skip();
											Skip();
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
					Skip();
					// Line 336: (&{mode == TypeParseMode.Normal} | &!(((TT.Add|TT.AndBits|TT.At|TT.ContextualKeyword|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.New|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | LinqKeywordAsId)))
					if (mode == TypeParseMode.Normal) { } else if (Try_TypeSuffixOpt_Test0(0))
						return false;
				}
			}
			return true;
		}

		// Atom is: Id, TypeKeyword, $Atom, .Atom, new ..., (ExprStart), {Stmts},
		LNode Atom()
		{
			TokenType la0, la1;
			// line 445
			LNode r;
			// Line 446: ( (TT.Dot|TT.Substitute) Atom | TT.Operator AnyOperator | (TT.Base|TT.ContextualKeyword|TT.Id|TT.This|TT.TypeKeyword) | LinqKeywordAsId | TT.Literal | ExprInParensAuto | NewExpr | BracedBlock | TokenLiteral | TT.CheckedOrUnchecked TT.LParen TT.RParen | (TT.Sizeof|TT.Typeof) TT.LParen TT.RParen | TT.Default (TT.LParen TT.RParen / {..}) | TT.Delegate TT.LParen TT.RParen TT.LBrace TT.RBrace | TT.Is IsPattern )
			switch (LA0) {
			case TT.Dot: case TT.Substitute:
				{
					var t = MatchAny();
					var e = Atom();
					// line 446
					e = AutoRemoveParens(e);
					// line 447
					r = F.CallPrefixOp(t, e);
				}
				break;
			case TT.Operator:
				{
					var op = MatchAny();
					var t = AnyOperator();
					// line 449
					r = F.Attr(_triviaUseOperatorKeyword, F.Id((Symbol) t.Value, op.StartIndex, t.EndIndex));
				}
				break;
			case TT.Base: case TT.ContextualKeyword: case TT.Id: case TT.This:
			case TT.TypeKeyword:
				{
					var t = MatchAny();
					// line 451
					r = F.Id(t);
				}
				break;
			case TT.LinqKeyword:
				{
					var t = LinqKeywordAsId();
					// line 453
					r = F.Id(t);
				}
				break;
			case TT.Literal:
				{
					var t = MatchAny();
					// line 455
					r = F.Literal(t);
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
			case TT.CheckedOrUnchecked:
				{
					var t = MatchAny();
					var args = Match((int) TT.LParen);
					var rp = Match((int) TT.RParen);
					// line 462
					r = F.CallPrefix(t, ExprListInside(args), rp);
				}
				break;
			case TT.Sizeof: case TT.Typeof:
				{
					var t = MatchAny();
					var args = Match((int) TT.LParen);
					var rp = Match((int) TT.RParen);
					// line 465
					r = F.CallPrefix(t, TypeInside(args), rp);
				}
				break;
			case TT.Default:
				{
					var t = MatchAny();
					// Line 467: (TT.LParen TT.RParen / {..})
					la0 = LA0;
					if (la0 == TT.LParen) {
						la1 = LA(1);
						if (la1 == TT.RParen) {
							var args = MatchAny();
							var rp = MatchAny();
							// line 468
							r = F.CallPrefix(t, TypeInside(args), rp);
						} else
							// line 469
							r = F.Id(t);
					} else
						// line 469
						r = F.Id(t);
				}
				break;
			case TT.Delegate:
				{
					var t = MatchAny();
					var args = Match((int) TT.LParen);
					Match((int) TT.RParen);
					var block = Match((int) TT.LBrace);
					var rb = Match((int) TT.RBrace);
					// line 472
					var argList = LNode.List(F.List(ExprListInside(args, false, true)), F.Braces(block, StmtListInside(block), rb));
					r = F.CallBrackets(S.Lambda, t, argList, rb, NodeStyle.OldStyle);
				}
				break;
			case TT.Is:
				{
					var t = MatchAny();
					r = IsPattern(F.Missing, t);
				}
				break;
			default:
				{
					// line 479
					r = Error("'{0}': Expected an expression: (parentheses), {{braces}}, identifier, literal, or $substitution.", CurrentTokenText());
					// Line 480: greedy(~(EOF|TT.Comma|TT.Semicolon))*
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
				}
				break;
			}
			// line 482
			return r;
		}
		// Atom is: Id, TypeKeyword, $Atom, .Atom, new ..., (ExprStart), {Stmts},
		bool Scan_Atom()
		{
			TokenType la0, la1;
			// Line 446: ( (TT.Dot|TT.Substitute) Atom | TT.Operator AnyOperator | (TT.Base|TT.ContextualKeyword|TT.Id|TT.This|TT.TypeKeyword) | LinqKeywordAsId | TT.Literal | ExprInParensAuto | NewExpr | BracedBlock | TokenLiteral | TT.CheckedOrUnchecked TT.LParen TT.RParen | (TT.Sizeof|TT.Typeof) TT.LParen TT.RParen | TT.Default (TT.LParen TT.RParen / {..}) | TT.Delegate TT.LParen TT.RParen TT.LBrace TT.RBrace | TT.Is IsPattern )
			switch (LA0) {
			case TT.Dot: case TT.Substitute:
				{
					Skip();
					if (!Scan_Atom())
						return false;
				}
				break;
			case TT.Operator:
				{
					Skip();
					if (!Scan_AnyOperator())
						return false;
				}
				break;
			case TT.Base: case TT.ContextualKeyword: case TT.Id: case TT.This:
			case TT.TypeKeyword:
				Skip();
				break;
			case TT.LinqKeyword:
				if (!Scan_LinqKeywordAsId())
					return false;
				break;
			case TT.Literal:
				Skip();
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
			case TT.CheckedOrUnchecked:
				{
					Skip();
					if (!TryMatch((int) TT.LParen))
						return false;
					if (!TryMatch((int) TT.RParen))
						return false;
				}
				break;
			case TT.Sizeof: case TT.Typeof:
				{
					Skip();
					if (!TryMatch((int) TT.LParen))
						return false;
					if (!TryMatch((int) TT.RParen))
						return false;
				}
				break;
			case TT.Default:
				{
					Skip();
					// Line 467: (TT.LParen TT.RParen / {..})
					do {
						la0 = LA0;
						if (la0 == TT.LParen) {
							la1 = LA(1);
							if (la1 == TT.RParen) {
								Skip();
								Skip();
							} else
								goto match2;
						} else
							goto match2;
						break;
					match2:
						{ }
					} while (false);
				}
				break;
			case TT.Delegate:
				{
					Skip();
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
					Skip();
					if (!Scan_IsPattern())
						return false;
				}
				break;
			default:
				return false;
			}
			return true;
		}

		Token AnyOperator()
		{
			TokenType la0, la1;
			Token result = default(Token);
			// Line 488: (&{LT($LI).EndIndex == LT($LI + 1).StartIndex} (TT.LT TT.LT | TT.GT TT.GT) / (TT.Add|TT.And|TT.AndBits|TT.At|TT.Backslash|TT.BQString|TT.Colon|TT.ColonColon|TT.Compare|TT.CompoundSet|TT.DivMod|TT.Dot|TT.DotDot|TT.EqNeq|TT.Forward|TT.GT|TT.IncDec|TT.LambdaArrow|TT.LEGE|TT.LT|TT.Mul|TT.Not|TT.NotBits|TT.NullCoalesce|TT.NullDot|TT.OrBits|TT.OrXor|TT.PipeArrow|TT.Power|TT.PtrArrow|TT.QuestionMark|TT.QuickBind|TT.QuickBindSet|TT.Set|TT.Sub|TT.Substitute|TT.XorBits))
			la0 = LA0;
			if (la0 == TT.GT || la0 == TT.LT) {
				if (LT(0).EndIndex == LT(0 + 1).StartIndex) {
					la1 = LA(1);
					if (la1 == TT.GT || la1 == TT.LT) {
						// Line 489: (TT.LT TT.LT | TT.GT TT.GT)
						la0 = LA0;
						if (la0 == TT.LT) {
							var op = MatchAny();
							Match((int) TT.LT);
							// line 489
							result = new Token((int) TT.Operator, op.StartIndex, op.Length + 1, S.Shl);
						} else {
							var op = Match((int) TT.GT);
							Match((int) TT.GT);
							// line 490
							result = new Token((int) TT.Operator, op.StartIndex, op.Length + 1, S.Shr);
						}
					} else
						result = Match(MethodOrPropertyName_set0);
				} else
					result = Match(MethodOrPropertyName_set0);
			} else
				result = Match(MethodOrPropertyName_set0);
			return result;
		}

		bool Scan_AnyOperator()
		{
			TokenType la0, la1;
			// Line 488: (&{LT($LI).EndIndex == LT($LI + 1).StartIndex} (TT.LT TT.LT | TT.GT TT.GT) / (TT.Add|TT.And|TT.AndBits|TT.At|TT.Backslash|TT.BQString|TT.Colon|TT.ColonColon|TT.Compare|TT.CompoundSet|TT.DivMod|TT.Dot|TT.DotDot|TT.EqNeq|TT.Forward|TT.GT|TT.IncDec|TT.LambdaArrow|TT.LEGE|TT.LT|TT.Mul|TT.Not|TT.NotBits|TT.NullCoalesce|TT.NullDot|TT.OrBits|TT.OrXor|TT.PipeArrow|TT.Power|TT.PtrArrow|TT.QuestionMark|TT.QuickBind|TT.QuickBindSet|TT.Set|TT.Sub|TT.Substitute|TT.XorBits))
			do {
				la0 = LA0;
				if (la0 == TT.GT || la0 == TT.LT) {
					if (LT(0).EndIndex == LT(0 + 1).StartIndex) {
						la1 = LA(1);
						if (la1 == TT.GT || la1 == TT.LT) {
							// Line 489: (TT.LT TT.LT | TT.GT TT.GT)
							la0 = LA0;
							if (la0 == TT.LT) {
								Skip();
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
					if (!TryMatch(MethodOrPropertyName_set0))
						return false;
				}
			} while (false);
			return true;
		}

		LNode NewExpr()
		{
			TokenType la0, la1;
			Token lb = default(Token);
			Token rb = default(Token);
			// line 501
			Token? majorDimension = null;
			var list = LNodeList.Empty;
			var op = Match((int) TT.New);
			// Line 506: ( &{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack TT.LBrace TT.RBrace | TT.LBrace TT.RBrace | DataType (TT.LParen TT.RParen (TT.LBrace TT.RBrace)? / (TT.LBrace TT.RBrace)?) )
			la0 = LA0;
			if (la0 == TT.LBrack) {
				Check((count = CountDims(LT(0), false)) > 0, "Expected (count = CountDims(LT($LI), @false)) > 0");
				lb = MatchAny();
				rb = Match((int) TT.RBrack);
				// line 508
				var type = F.Id(S.GetArrayKeyword(count), lb.StartIndex, rb.EndIndex);
				lb = Match((int) TT.LBrace);
				rb = Match((int) TT.RBrace);
				// line 511
				list.Add(LNode.Call(type, type.Range));
				AppendInitializersInside(lb, ref list);
			} else if (la0 == TT.LBrace) {
				lb = MatchAny();
				rb = Match((int) TT.RBrace);
				// line 517
				list.Add(F.Missing);
				AppendInitializersInside(lb, ref list);
			} else {
				var type = DataType(false, out majorDimension);
				// Line 528: (TT.LParen TT.RParen (TT.LBrace TT.RBrace)? / (TT.LBrace TT.RBrace)?)
				do {
					la0 = LA0;
					if (la0 == TT.LParen) {
						la1 = LA(1);
						if (la1 == TT.RParen) {
							lb = MatchAny();
							rb = MatchAny();
							// line 530
							if ((majorDimension != null)) {
								Error("Syntax error: unexpected constructor argument list (...)");
							}
							list.Add(F.CallPrefix(type, ExprListInside(lb), rb));
							// Line 535: (TT.LBrace TT.RBrace)?
							la0 = LA0;
							if (la0 == TT.LBrace) {
								la1 = LA(1);
								if (la1 == TT.RBrace) {
									lb = MatchAny();
									rb = MatchAny();
									// line 536
									AppendInitializersInside(lb, ref list);
								}
							}
						} else
							goto match2;
					} else
						goto match2;
					break;
				match2:
					{
						var haveBraces = false;
						// Line 542: (TT.LBrace TT.RBrace)?
						la0 = LA0;
						if (la0 == TT.LBrace) {
							la1 = LA(1);
							if (la1 == TT.RBrace) {
								lb = MatchAny();
								rb = MatchAny();
								// line 542
								haveBraces = true;
							}
						}
						// line 544
						if ((majorDimension != null)) {
							list.Add(LNode.Call(type, ExprListInside(majorDimension.Value), type.Range));
						} else {
							list.Add(LNode.Call(type, type.Range));
						}
						if ((haveBraces)) {
							AppendInitializersInside(lb, ref list);
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
			// line 562
			return F.CallPrefix(op, list, rb, S.New);
		}

		bool Scan_NewExpr()
		{
			TokenType la0, la1;
			if (!TryMatch((int) TT.New))
				return false;
			// Line 506: ( &{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack TT.LBrace TT.RBrace | TT.LBrace TT.RBrace | DataType (TT.LParen TT.RParen (TT.LBrace TT.RBrace)? / (TT.LBrace TT.RBrace)?) )
			la0 = LA0;
			if (la0 == TT.LBrack) {
				if (!((count = CountDims(LT(0), false)) > 0))
					return false;
				Skip();
				if (!TryMatch((int) TT.RBrack))
					return false;
				if (!TryMatch((int) TT.LBrace))
					return false;
				if (!TryMatch((int) TT.RBrace))
					return false;
			} else if (la0 == TT.LBrace) {
				Skip();
				if (!TryMatch((int) TT.RBrace))
					return false;
			} else {
				if (!Scan_DataType(false))
					return false;
				// Line 528: (TT.LParen TT.RParen (TT.LBrace TT.RBrace)? / (TT.LBrace TT.RBrace)?)
				do {
					la0 = LA0;
					if (la0 == TT.LParen) {
						la1 = LA(1);
						if (la1 == TT.RParen) {
							Skip();
							Skip();
							// Line 535: (TT.LBrace TT.RBrace)?
							la0 = LA0;
							if (la0 == TT.LBrace) {
								la1 = LA(1);
								if (la1 == TT.RBrace) {
									Skip();
									Skip();
								}
							}
						} else
							goto match2;
					} else
						goto match2;
					break;
				match2:
					{
						// Line 542: (TT.LBrace TT.RBrace)?
						la0 = LA0;
						if (la0 == TT.LBrace) {
							la1 = LA(1);
							if (la1 == TT.RBrace) {
								Skip();
								Skip();
							}
						}
					}
				} while (false);
			}
			return true;
		}

		private LNode TokenLiteral()
		{
			TokenType la0;
			Token at = default(Token);
			Token L = default(Token);
			Token R = default(Token);
			at = Match((int) TT.At);
			// Line 574: (TT.LBrack TT.RBrack | TT.LBrace TT.RBrace)
			la0 = LA0;
			if (la0 == TT.LBrack) {
				L = MatchAny();
				R = Match((int) TT.RBrack);
			} else {
				L = Match((int) TT.LBrace);
				R = Match((int) TT.RBrace);
			}
			// line 575
			return F.Literal(L.Children, at.StartIndex, R.EndIndex);
		}

		private bool Scan_TokenLiteral()
		{
			TokenType la0;
			if (!TryMatch((int) TT.At))
				return false;
			// Line 574: (TT.LBrack TT.RBrack | TT.LBrace TT.RBrace)
			la0 = LA0;
			if (la0 == TT.LBrack) {
				Skip();
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
			// Line 580: (&(IdWithOptionalTypeParams ~(TT.ContextualKeyword|TT.Id|TT.LinqKeyword)) IdWithOptionalTypeParams / Atom)
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
			// Line 588: (TT.NullDot PrimaryExpr)?
			la0 = LA0;
			if (la0 == TT.NullDot) {
				switch (LA(1)) {
				case TT.At: case TT.Base: case TT.CheckedOrUnchecked: case TT.ContextualKeyword:
				case TT.Default: case TT.Delegate: case TT.Dot: case TT.Id:
				case TT.Is: case TT.LBrace: case TT.LinqKeyword: case TT.Literal:
				case TT.LParen: case TT.New: case TT.Operator: case TT.Sizeof:
				case TT.Substitute: case TT.This: case TT.TypeKeyword: case TT.Typeof:
					{
						var op = MatchAny();
						var rhs = PrimaryExpr();
						// line 588
						e = F.CallInfixOp(e, op, rhs);
					}
					break;
				}
			}
			// line 590
			return e;
		}

		private void FinishPrimaryExpr(ref LNode e)
		{
			TokenType la1;
			// Line 595: greedy( (TT.ColonColon|TT.Dot|TT.PtrArrow|TT.QuickBind) AtomOrTypeParamExpr / PrimaryExpr_NewStyleCast / TT.LParen TT.RParen | TT.LBrack TT.RBrack | TT.QuestionMark TT.LBrack TT.RBrack | TT.IncDec | BracedBlockOrTokenLiteral )*
			for (;;) {
				switch (LA0) {
				case TT.ColonColon: case TT.Dot: case TT.PtrArrow: case TT.QuickBind:
					{
						switch (LA(1)) {
						case TT.At: case TT.Base: case TT.CheckedOrUnchecked: case TT.ContextualKeyword:
						case TT.Default: case TT.Delegate: case TT.Dot: case TT.Id:
						case TT.Is: case TT.LBrace: case TT.LinqKeyword: case TT.Literal:
						case TT.LParen: case TT.New: case TT.Operator: case TT.Sizeof:
						case TT.Substitute: case TT.This: case TT.TypeKeyword: case TT.Typeof:
							{
								var op = MatchAny();
								var rhs = AtomOrTypeParamExpr();
								// line 596
								e = F.CallInfixOp(e, op, rhs);
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
								// line 599
								e = F.CallPrefix(e, ExprListInside(lp), rp);
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
							// line 600
							var list = new LNodeList { 
								e
							};
							// line 601
							e = F.CallPrefix(S.IndexBracks, e.Range, AppendExprsInside(lb, list), rb);
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
							// line 617
							e = F.CallPrefix(S.NullIndexBracks, e.Range, LNode.List(e, F.List(ExprListInside(lb))), rb);
						} else
							goto stop;
					}
					break;
				case TT.IncDec:
					{
						var t = MatchAny();
						// line 619
						e = F.CallSuffixOp(e, t, t.Value == S.PreInc ? S.PostInc : S.PostDec);
					}
					break;
				case TT.At: case TT.LBrace:
					{
						la1 = LA(1);
						if (la1 == TT.LBrace || la1 == TT.LBrack || la1 == TT.RBrace) {
							var bb = BracedBlockOrTokenLiteral();
							// line 621
							if ((!e.IsCall || e.BaseStyle == NodeStyle.Operator)) {
								e = F.CallPrefixOp(e, bb, NodeStyle.Default);
							} else {
								e = F.CallPrefixOp(e.Target, e.Args.Add(bb), NodeStyle.Default);
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
			// line 634
			Down(lp);
			// line 635
			Symbol kind;
			// line 636
			var attrs = LNodeList.Empty;
			// Line 637: ( TT.PtrArrow | TT.As | TT.Using )
			la0 = LA0;
			if (la0 == TT.PtrArrow) {
				op = MatchAny();
				// line 637
				kind = S.Cast;
			} else if (la0 == TT.As) {
				op = MatchAny();
				// line 638
				kind = S.As;
			} else {
				op = Match((int) TT.Using);
				// line 639
				kind = S.UsingCast;
			}
			NormalAttributes(ref attrs);
			AttributeKeywords(ref attrs);
			var type = DataType();
			Match((int) EOF);
			// line 644
			type = type.PlusAttrs(attrs);
			return Up(F.Call(kind, e, type, e.Range.StartIndex, rp.EndIndex, op.StartIndex, op.EndIndex, NodeStyle.Operator | NodeStyle.Alternate));
		}
		static readonly HashSet<int> PrefixExpr_set0 = NewSet((int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.Base, (int) TT.Break, (int) TT.CheckedOrUnchecked, (int) TT.ContextualKeyword, (int) TT.Continue, (int) TT.Default, (int) TT.Delegate, (int) TT.Dot, (int) TT.DotDot, (int) TT.Forward, (int) TT.Goto, (int) TT.Id, (int) TT.IncDec, (int) TT.Is, (int) TT.LBrace, (int) TT.LinqKeyword, (int) TT.Literal, (int) TT.LParen, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.Operator, (int) TT.Power, (int) TT.Return, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.Switch, (int) TT.This, (int) TT.Throw, (int) TT.TypeKeyword, (int) TT.Typeof);

		// Prefix expressions, atoms, and high-precedence expressions like f(x) and List<T>
		// to distinguish (cast) expr from (parens)
		private LNode PrefixExpr()
		{
			TokenType la2;
			// Line 655: ( ((TT.Add|TT.AndBits|TT.DotDot|TT.Forward|TT.IncDec|TT.Mul|TT.Not|TT.NotBits|TT.Sub) PrefixExpr | TT.Power PrefixExpr) | (&{Down($LI) && Up(Scan_DataType() && LA0 == EOF)} TT.LParen TT.RParen &!(( (TT.Add|TT.AndBits|TT.BQString|TT.Dot|TT.Mul|TT.Sub|TT.Switch) | TT.IncDec TT.LParen | &{_insideLinqExpr} TT.LinqKeyword )) PrefixExpr / KeywordOrPrimaryExpr) )
			do {
				switch (LA0) {
				case TT.Add: case TT.AndBits: case TT.DotDot: case TT.Forward:
				case TT.IncDec: case TT.Mul: case TT.Not: case TT.NotBits:
				case TT.Sub:
					{
						var op = MatchAny();
						var e = PrefixExpr();
						// line 656
						return F.CallPrefixOp(op, e);
					}
					break;
				case TT.Power:
					{
						var op = MatchAny();
						var e = PrefixExpr();
						// line 659
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
									// line 667
									Down(lp);
									return F.Call(S.Cast, e, Up(DataType()), lp.StartIndex, e.Range.EndIndex, lp.StartIndex, lp.EndIndex, NodeStyle.Operator);
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
					// line 668
					return e;
				}
			} while (false);
		}

		private 
		LNode KeywordOrPrimaryExpr()
		{
			TokenType la1;
			// Line 674: ( &{Is($LI, @@await)} TT.ContextualKeyword PrefixExpr / KeywordStmtAsExpr / LinqQueryExpression / PrimaryExpr )
			do {
				switch (LA0) {
				case TT.ContextualKeyword:
					{
						if (Is(0, sy_await)) {
							la1 = LA(1);
							if (PrefixExpr_set0.Contains((int) la1)) {
								var op = MatchAny();
								var e = PrefixExpr();
								// line 675
								return F.CallPrefixOp(op, e, sy_await);
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
						// line 677
						return e;
					}
					break;
				case TT.LinqKeyword:
					{
						if (Is(0, sy_from)) {
							la1 = LA(1);
							if (la1 == TT.ContextualKeyword || la1 == TT.Id || la1 == TT.Substitute) {
								var e = LinqQueryExpression();
								// line 679
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
					// line 681
					return e;
				}
			} while (false);
		}

		LNode KeywordStmtAsExpr()
		{
			TokenType la1;
			LNode result = default(LNode);
			// line 685
			var startIndex = LT0.StartIndex;
			// Line 686: ( ReturnBreakContinueThrow | (GotoCaseStmt / GotoStmt) | SwitchStmt )
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
		static readonly HashSet<int> SubExpr_set0 = NewSet((int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.Base, (int) TT.Break, (int) TT.CheckedOrUnchecked, (int) TT.ContextualKeyword, (int) TT.Continue, (int) TT.Default, (int) TT.Delegate, (int) TT.Dot, (int) TT.DotDot, (int) TT.Forward, (int) TT.Goto, (int) TT.Id, (int) TT.IncDec, (int) TT.Is, (int) TT.LinqKeyword, (int) TT.Literal, (int) TT.LParen, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.Operator, (int) TT.Power, (int) TT.Return, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.Switch, (int) TT.This, (int) TT.Throw, (int) TT.TypeKeyword, (int) TT.Typeof);
		static readonly HashSet<int> SubExpr_set1 = NewSet((int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.Base, (int) TT.Break, (int) TT.CheckedOrUnchecked, (int) TT.Compare, (int) TT.ContextualKeyword, (int) TT.Continue, (int) TT.Default, (int) TT.Delegate, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.Forward, (int) TT.Goto, (int) TT.GT, (int) TT.Id, (int) TT.IncDec, (int) TT.Is, (int) TT.LBrace, (int) TT.LEGE, (int) TT.LinqKeyword, (int) TT.Literal, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.Operator, (int) TT.Power, (int) TT.Return, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.Switch, (int) TT.This, (int) TT.Throw, (int) TT.TypeKeyword, (int) TT.Typeof);

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
		//     it may be slower when there are few levels. Note: EC# has 29 
		//     precedence levels, and this method handles the lower 24 of them.
		//
		// The higher levels of the EC# expression parser do not use this trick
		// because the higher precedence levels have some complications, such as 
		// C-style casts and <type arguments>, that I prefer to deal with 
		// separately.
		private LNode SubExpr(Precedence context)
		{
			TokenType la0, la1;
			Token lit_lcub = default(Token);
			Token lit_rcub = default(Token);
			// line 726
			Debug.Assert(context.CanParse(EP.Prefix));
			Precedence prec;
			Token op;
			LNode rhs;
			Token lt0;
			var e = PrefixExpr();
			// Line 735: greedy( TT.Switch SwitchBody / &{context.CanParse(prec = InfixPrecedenceOf($LA))} (TT.Add|TT.And|TT.AndBits|TT.BQString|TT.Compare|TT.CompoundSet|TT.DivMod|TT.DotDot|TT.EqNeq|TT.GT|TT.In|TT.LEGE|TT.LT|TT.Mul|TT.NotBits|TT.NullCoalesce|TT.OrBits|TT.OrXor|TT.PipeArrow|TT.Power|TT.Set|TT.Sub|TT.Switch|TT.XorBits) SubExpr | &{context.CanParse(prec = EP.Lambda) && !_insidePattern} TT.LambdaArrow SubExpr | &{context.CanParse(prec = EP.Is)} TT.Is Pattern | &{context.CanParse(prec = EP.AsUsing)} (TT.As|TT.Using) DataType | &{context.CanParse(EP.Shift)} &{LT($LI).EndIndex == LT($LI + 1).StartIndex} (TT.LT TT.LT SubExpr | TT.GT TT.GT SubExpr) | &{context.CanParse(EP.IfElse)} TT.QuestionMark SubExpr TT.Colon SubExpr | &{Is($LI, @@with)} &{context.CanParse(prec = EP.Switch)} TT.ContextualKeyword TT.LBrace TT.RBrace | &{((lt0 = LT($LI)).Value == @@when || lt0.Value == @@where && !_insideLinqExpr) && SourceFile.Text.TryGet(lt0.StartIndex, '\0') != '@'} &{context.CanParse(prec = EP.WhenWhere)} TT.ContextualKeyword SubExpr )*
			for (;;) {
				switch (LA0) {
				case TT.Switch:
					{
						la0 = LA0;
						if (context.CanParse(prec = InfixPrecedenceOf(la0))) {
							la1 = LA(1);
							if (la1 == TT.LBrace)
								goto match1;
							else if (SubExpr_set0.Contains((int) la1))
								goto match2;
							else
								goto stop;
						} else {
							la1 = LA(1);
							if (la1 == TT.LBrace)
								goto match1;
							else
								goto stop;
						}
					}
				case TT.GT: case TT.LT:
					{
						la0 = LA0;
						if (context.CanParse(prec = InfixPrecedenceOf(la0))) {
							if (LT(0).EndIndex == LT(0 + 1).StartIndex) {
								if (context.CanParse(EP.Shift)) {
									la1 = LA(1);
									if (PrefixExpr_set0.Contains((int) la1))
										goto match2;
									else if (la1 == TT.GT || la1 == TT.LT)
										goto match6;
									else
										goto stop;
								} else {
									la1 = LA(1);
									if (PrefixExpr_set0.Contains((int) la1))
										goto match2;
									else
										goto stop;
								}
							} else {
								la1 = LA(1);
								if (PrefixExpr_set0.Contains((int) la1))
									goto match2;
								else
									goto stop;
							}
						} else if (LT(0).EndIndex == LT(0 + 1).StartIndex) {
							if (context.CanParse(EP.Shift)) {
								la1 = LA(1);
								if (la1 == TT.GT || la1 == TT.LT)
									goto match6;
								else
									goto stop;
							} else
								goto stop;
						} else
							goto stop;
					}
				case TT.Add: case TT.And: case TT.AndBits: case TT.BQString:
				case TT.Compare: case TT.CompoundSet: case TT.DivMod: case TT.DotDot:
				case TT.EqNeq: case TT.In: case TT.LEGE: case TT.Mul:
				case TT.NotBits: case TT.NullCoalesce: case TT.OrBits: case TT.OrXor:
				case TT.PipeArrow: case TT.Power: case TT.Set: case TT.Sub:
				case TT.XorBits:
					{
						la0 = LA0;
						if (context.CanParse(prec = InfixPrecedenceOf(la0))) {
							la1 = LA(1);
							if (PrefixExpr_set0.Contains((int) la1))
								goto match2;
							else
								goto stop;
						} else
							goto stop;
					}
				case TT.LambdaArrow:
					{
						if (context.CanParse(prec = EP.Lambda) && !_insidePattern) {
							la1 = LA(1);
							if (PrefixExpr_set0.Contains((int) la1)) {
								op = MatchAny();
								rhs = SubExpr(prec);
								// line 748
								e = F.CallInfixOp(e, op, rhs);
							} else
								goto stop;
						} else
							goto stop;
					}
					break;
				case TT.Is:
					{
						if (context.CanParse(prec = EP.Is)) {
							la1 = LA(1);
							if (SubExpr_set1.Contains((int) la1)) {
								op = MatchAny();
								rhs = Pattern(prec);
								// line 753
								e = F.CallInfixOp(e, op, rhs, S.Is);
							} else
								goto stop;
						} else
							goto stop;
					}
					break;
				case TT.As: case TT.Using:
					{
						if (context.CanParse(prec = EP.AsUsing)) {
							switch (LA(1)) {
							case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.LParen:
							case TT.Operator: case TT.Substitute: case TT.TypeKeyword:
								{
									op = MatchAny();
									rhs = DataType(afterAs: true);
									// line 758
									e = F.CallInfixOp(e, op, rhs, op.Type() == TT.Using ? S.UsingCast : S.As);
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
								op = MatchAny();
								var then = SubExpr(StartExpr);
								Match((int) TT.Colon);
								var @else = SubExpr(EP.IfElse);
								// line 771
								e = F.CallInfixOp(e, op, then, @else);
							} else
								goto stop;
						} else
							goto stop;
					}
					break;
				case TT.ContextualKeyword:
					{
						if (context.CanParse(prec = EP.Switch)) {
							if (Is(0, sy_with)) {
								if (context.CanParse(prec = EP.WhenWhere)) {
									if (((lt0 = LT(0)).Value == sy_when || lt0.Value == sy_where && !_insideLinqExpr) && SourceFile.Text.TryGet(lt0.StartIndex, '\0') != '@') {
										la1 = LA(1);
										if (la1 == TT.LBrace)
											goto match8;
										else if (SubExpr_set0.Contains((int) la1))
											goto match9;
										else
											goto stop;
									} else {
										la1 = LA(1);
										if (la1 == TT.LBrace)
											goto match8;
										else
											goto stop;
									}
								} else {
									la1 = LA(1);
									if (la1 == TT.LBrace)
										goto match8;
									else
										goto stop;
								}
							} else if (context.CanParse(prec = EP.WhenWhere)) {
								if (((lt0 = LT(0)).Value == sy_when || lt0.Value == sy_where && !_insideLinqExpr) && SourceFile.Text.TryGet(lt0.StartIndex, '\0') != '@') {
									la1 = LA(1);
									if (PrefixExpr_set0.Contains((int) la1))
										goto match9;
									else
										goto stop;
								} else
									goto stop;
							} else
								goto stop;
						} else if (Is(0, sy_with)) {
							if (context.CanParse(prec = EP.WhenWhere)) {
								if (((lt0 = LT(0)).Value == sy_when || lt0.Value == sy_where && !_insideLinqExpr) && SourceFile.Text.TryGet(lt0.StartIndex, '\0') != '@') {
									la1 = LA(1);
									if (PrefixExpr_set0.Contains((int) la1))
										goto match9;
									else
										goto stop;
								} else
									goto stop;
							} else
								goto stop;
						} else if (context.CanParse(prec = EP.WhenWhere)) {
							if (((lt0 = LT(0)).Value == sy_when || lt0.Value == sy_where && !_insideLinqExpr) && SourceFile.Text.TryGet(lt0.StartIndex, '\0') != '@') {
								la1 = LA(1);
								if (PrefixExpr_set0.Contains((int) la1))
									goto match9;
								else
									goto stop;
							} else
								goto stop;
						} else
							goto stop;
					}
				default:
					goto stop;
				}
				continue;
			match1:
				{
					op = MatchAny();
					rhs = SwitchBody();
					// line 736
					e = F.CallInfixOp(e, op, rhs, S.SwitchOp);
				}
				continue;
			match2:
				{
					op = MatchAny();
					rhs = SubExpr(prec);
					// line 743
					e = F.CallInfixOp(e, op, rhs);
				}
				continue;
			match6:
				{
					// Line 762: (TT.LT TT.LT SubExpr | TT.GT TT.GT SubExpr)
					la0 = LA0;
					if (la0 == TT.LT) {
						op = MatchAny();
						Match((int) TT.LT);
						rhs = SubExpr(EP.Shift);
						// line 763
						e = F.CallInfixOp(e, S.Shl, new IndexRange(op.StartIndex) { 
							EndIndex = op.EndIndex + 1
						}, rhs);
					} else if (la0 == TT.GT) {
						op = MatchAny();
						Match((int) TT.GT);
						rhs = SubExpr(EP.Shift);
						// line 765
						e = F.CallInfixOp(e, S.Shr, new IndexRange(op.StartIndex) { 
							EndIndex = op.EndIndex + 1
						}, rhs);
					} else {
						// line 766
						e = Error("Syntax error");
					}
				}
				continue;
			match8:
				{
					op = MatchAny();
					lit_lcub = MatchAny();
					lit_rcub = Match((int) TT.RBrace);
					// line 777
					var list = ExprListInside(lit_lcub, true);
					var body = F.CallBrackets(S.Braces, lit_lcub, list, lit_rcub);
					e = F.CallInfixOp(e, op, body, S.With);
				}
				continue;
			match9:
				{
					op = MatchAny();
					rhs = SubExpr(EP.WhenWhere);
					// line 787
					e = F.CallInfixOp(e, op, rhs, op.Value == sy_when ? S.When : S.WhereOp);
				}
			}
		stop:;
			// line 789
			return e;
		}

		// DEPRECATED - original EC# "is" pattern matching. Short & simple...
		// TODO: delete this
		private LNode IsPattern(LNode lhs, Token isTok)
		{
			TokenType la0, la1;
			Token lit_lpar = default(Token);
			Token lit_rpar = default(Token);
			// line 797
			LNodeList argList = new LNodeList(lhs);
			var target = DataType(afterAs: true);
			// Line 800: (IdAtom)?
			switch (LA0) {
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
			case TT.Substitute: case TT.TypeKeyword:
				{
					var targetName = IdAtom();
					// line 800
					target = F.Call(S.Var, target, targetName, target.Range.StartIndex, targetName.Range.EndIndex);
				}
				break;
			}
			// line 801
			argList.Add(target);
			// Line 803: (TT.LParen TT.RParen)?
			la0 = LA0;
			if (la0 == TT.LParen) {
				la1 = LA(1);
				if (la1 == TT.RParen) {
					lit_lpar = MatchAny();
					lit_rpar = MatchAny();
					// line 803
					argList.Add(F.List(ExprListInside(lit_lpar, allowUnassignedVarDecl: true), lit_lpar.StartIndex, lit_rpar.EndIndex));
				}
			}
			// line 804
			return F.Call(isTok, argList, lhs.Range.StartIndex, argList.Last.Range.EndIndex, NodeStyle.Operator);
		}

		bool Try_Scan_IsPattern(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return Scan_IsPattern();
		}
		bool Scan_IsPattern()
		{
			TokenType la0, la1;
			if (!Scan_DataType(afterAs: true))
				return false;
			// Line 800: (IdAtom)?
			switch (LA0) {
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
			case TT.Substitute: case TT.TypeKeyword:
				if (!Scan_IdAtom())
					return false;
				break;
			}
			// Line 803: (TT.LParen TT.RParen)?
			la0 = LA0;
			if (la0 == TT.LParen) {
				la1 = LA(1);
				if (la1 == TT.RParen) {
					Skip();
					Skip();
				}
			}
			return true;
		}

		// An expression that can start with attributes [...], attribute keywords 
		// (out, ref, public, etc.), a named argument (a: expr) and/or a variable 
		// declaration (Foo? x = null).
		public LNode ExprStart(bool allowUnassignedVarDecl)
		{
			TokenType la0, la1;
			LNode result = default(LNode);
			// Line 812: (((TT.ContextualKeyword|TT.Id) | LinqKeywordAsId) TT.Colon ExprStartNNP / ExprStartNNP)
			la0 = LA0;
			if (la0 == TT.ContextualKeyword || la0 == TT.Id || la0 == TT.LinqKeyword) {
				la1 = LA(1);
				if (la1 == TT.Colon) {
					// line 812
					Token argName = default(Token);
					// Line 813: ((TT.ContextualKeyword|TT.Id) | LinqKeywordAsId)
					la0 = LA0;
					if (la0 == TT.ContextualKeyword || la0 == TT.Id)
						argName = MatchAny();
					else
						argName = LinqKeywordAsId();
					var colon = MatchAny();
					result = ExprStartNNP(allowUnassignedVarDecl);
					// line 815
					result = F.CallInfixOp(F.Id(argName), S.NamedArg, colon, result);
				} else
					result = ExprStartNNP(allowUnassignedVarDecl);
			} else
				result = ExprStartNNP(allowUnassignedVarDecl);
			return result;
		}

		// ExprStart with No Named Parameter allowed: an expression that can start 
		// with attributes [...], attribute keywords (out, ref, public, etc.), 
		// and/or a variable declaration (Foo? x = null).
		public LNode ExprStartNNP(bool allowUnassignedVarDecl)
		{
			LNode result = default(LNode);
			// line 823
			var attrs = LNodeList.Empty;
			var hasList = NormalAttributes(ref attrs);
			AttributeKeywords(ref attrs);
			// line 828
			if ((!attrs.IsEmpty || hasList)) {
				allowUnassignedVarDecl = true;
			}
			LNode expr;
			TentativeResult tresult, _;
			if ((allowUnassignedVarDecl)) {
				expr = TryParseVarDecl(attrs, out tresult, allowUnassignedVarDecl) ?? TryParseNonVarDeclExpr(attrs, out tresult);
			} else {
				expr = TryParseNonVarDeclExpr(attrs, out tresult);
				if (expr == null || (expr.Calls(S.Assign, 2) && expr.Args[0].Calls(S.GT, 2))) {
					InputPosition = tresult.OldPosition;
					expr = TryParseVarDecl(attrs, out _, allowUnassignedVarDecl);
				}
			}
			result = expr ?? Commit(tresult);
			return result;
		}

		private LNode VarDeclExpr(out bool hasInitializer, LNodeList attrs)
		{
			TokenType la0;
			LNode result = default(LNode);
			// Line 916: (TT.This)?
			la0 = LA0;
			if (la0 == TT.This) {
				var t = MatchAny();
				// line 916
				attrs.Add(F.Id(t));
			}
			var pair = VarDeclStart();
			// line 918
			LNode type = pair.Item1, name = pair.Item2;
			// Line 921: (RestOfPropertyDefinition / VarInitializerOpt)
			switch (LA0) {
			case TT.At: case TT.ContextualKeyword: case TT.Forward: case TT.LambdaArrow:
			case TT.LBrace: case TT.LBrack:
				{
					result = RestOfPropertyDefinition(type.Range.StartIndex, type, name, true);
					// line 922
					hasInitializer = true;
				}
				break;
			default:
				{
					var nameAndInit = VarInitializerOpt(name, IsArrayType(type));
					// line 925
					hasInitializer = (nameAndInit != name);
					var typeStart = type.Range.StartIndex;
					var start = attrs.IsEmpty ? typeStart : attrs[0].Range.StartIndex;
					result = F.Call(S.Var, type, nameAndInit, start, nameAndInit.Range.EndIndex, typeStart, typeStart);
					hasInitializer = true;
				}
				break;
			}
			// line 933
			result = result.PlusAttrs(attrs);
			return result;
		}

		private Pair<LNode, LNode> VarDeclStart()
		{
			var e = DataType();
			var id = IdAtom();
			// line 939
			MaybeRecognizeVarAsKeyword(ref e);
			// line 940
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

		private LNode ExprInParensAuto()
		{
			// Line 960: (&(ExprInParens (TT.LambdaArrow|TT.Set)) ExprInParens / ExprInParens)
			if (Try_ExprInParensAuto_Test0(0)) {
				var r = ExprInParens(true);
				// line 961
				return r;
			} else {
				var r = ExprInParens(false);
				// line 962
				return r;
			}
		}

		private bool Scan_ExprInParensAuto()
		{
			// Line 960: (&(ExprInParens (TT.LambdaArrow|TT.Set)) ExprInParens / ExprInParens)
			if (Try_ExprInParensAuto_Test0(0)){
				if (!Scan_ExprInParens(true))
					return false;}
			else if (!Scan_ExprInParens(false))
				return false;
			return true;
		}

		private LNode ExprInParens(bool allowUnassignedVarDecl)
		{
			Token lit_lpar = default(Token);
			Token lit_rpar = default(Token);
			// line 968
			var old_12 = _insideLinqExpr;
			_insideLinqExpr = false;
			try {
				lit_lpar = Match((int) TT.LParen);
				lit_rpar = Match((int) TT.RParen);
				// line 973
				if ((!Down(lit_lpar))) {
					return F.CallBrackets(S.Tuple, lit_lpar, LNode.List(), lit_rpar);
				}
				return Up(InParens_ExprOrTuple(allowUnassignedVarDecl, lit_lpar.StartIndex, lit_rpar.EndIndex));
			} finally { _insideLinqExpr = old_12; }
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
			// Line 980: (EOF => {..} / ExprStart nongreedy(TT.Comma ExprStart)* (TT.Comma)? EOF)
			la0 = LA0;
			if (la0 == EOF)
				// line 982
				return F.Tuple(LNodeList.Empty, startIndex, endIndex);
			else {
				// line 983
				var hasAttrList = LA0 == TT.LBrack;
				var e = ExprStart(allowUnassignedVarDecl);
				// line 985
				var list = new LNodeList { 
					e
				};
				// Line 987: nongreedy(TT.Comma ExprStart)*
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
				// line 989
				bool isTuple = list.Count > 1;
				// Line 990: (TT.Comma)?
				la0 = LA0;
				if (la0 == TT.Comma) {
					Skip();
					// line 990
					isTuple = true;
				}
				Match((int) EOF);
				// line 992
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
			// Line 1000: (BracedBlock | TokenLiteral)
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
			// line 1005
			var old_13 = _spaceName;
			_spaceName = spaceName ?? _spaceName;
			try {
				lit_lcub = Match((int) TT.LBrace);
				lit_rcub = Match((int) TT.RBrace);
				// line 1007
				if ((startIndex == -1)) {
					startIndex = lit_lcub.StartIndex;
				}
				var stmts = StmtListInside(lit_lcub);
				return F.Call(target ?? S.Braces, stmts, startIndex, lit_rcub.EndIndex, lit_lcub.StartIndex, lit_lcub.EndIndex, NodeStyle.StatementBlock);
			} finally { _spaceName = old_13; }
		}
		
		// ---------------------------------------------------------------------
			// -- "Patterns" (used in `is` and `switch` expressions) ---------------
			// ---------------------------------------------------------------------
			//
			// A pattern can appear after `is` or `case`, or inside the braced block
			// of a switch expression. NOTE: Patterns after `case` are not supported
			// in Enhanced C# v29 until we figure out how to resolve the major syntax 
		// conflict between Enhanced C# and plain C# that was introduced in C# 9.
			//
			// A ridiculously ambiguous grammar for patterns is given on this page:
			// https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/patterns
			// Sadly, not only does it not show any C# 9 features, but the C# 9 proposal at
			// https://github.com/dotnet/csharplang/blob/master/proposals/csharp-9.0/patterns3.md
			// refers to "existing" productions like primary_pattern that DON'T EXIST
			// in the C# 8 version. Anyway, here is my attempt to piece together a 
		// simplified, combined version of the C# 8 grammar and C# 9 partial grammar
			// (which still doesn't include `when` clauses from the `switch` expression):
			//
			// // aka disjunctive_pattern
			// pattern : conjunctive_pattern ('or' conjunctive_pattern)*;
			// conjunctive_pattern : negated_pattern ('and' negated_pattern)*;
			// negated_pattern : 'not' negated_pattern | primary_pattern;
			//
			// primary_pattern
			//     : type identifier     // aka declaration_pattern
			//     | constant_expression // aka constant_pattern
			//     | type                // aka type_pattern
			//     | var_pattern
			//     | positional_pattern
			//     | property_pattern
			//     | relational_pattern
			//     | '(' pattern ')'     // aka parenthesized_pattern
			//     | '_'                 // aka discard_pattern
			//     ;
			// 
		// subpatterns : subpattern (',' subpattern)*;
			// subpattern : (identifier ':')? pattern;
			// 
		// var_pattern : 'var' designation;
			//   designation : identifier | tuple_designation;    
		//   tuple_designation : '(' designation (',' designation)* ')';
			// 
		// positional_pattern : type? '(' subpatterns? ')' property_subpattern? identifier?;
			//
			// property_subpattern : '{' '}' | '{' subpatterns ','? '}';
			// 
		// property_pattern : type? property_subpattern identifier?;
			//
			// relational_pattern : ('<'|'>'|'<='|'>=') relational_expression;
			//   relational_expression : <<no definition is provided!>>
			//
			// NOTES ON AMBIGUITIES:
			// ---------------------
			// - The var_pattern is simply a special case of positional_pattern in which 
		//   the type happens to be `var` and the subpatterns are identifiers/tuples.
			//   But though syntactically it's a subset, its meaning is of course quite 
		//   different. The C# compiler evidently decided to parse it differently,
			//   as (contrary to the grammar above) `var(1, p)` is a syntax error even if 
		//   there is a `class var` in scope. EC# parses it differently, too.
			// - Supposedly, parenthesized_pattern was added in C# 9, but it's actually 
		//   just a special case of positional_pattern; `((var x))` should already have
			//   been valid in C# 8 given the grammar above, though maybe it has a 
		//   different meaning in C# 9? i.e. perhaps in C# 8 it's a double 
		//   deconstruction while in C# 9 there is no deconstruction at all.
			// - Less obvious is that `type` conflicts with `positional_pattern` (and also
			//   `constant_expression`) because `(A, B)` looks like a positional pattern and
			//   also looks like a tuple type (and a tuple value). C#, of course, assumes 
		//   that (A, B) is a pattern and not a type name nor a value.
			// - An expression of the form `(Int32) x` is tricky, since it matches 
		//   positional_pattern and constant_expression equally well. C# treats this 
		//   as constant_pattern, but superficially similar patterns like 
		//   `(Int32 + 1) x` are positional_patterns.
			// - One of the most surprising cases is e.g. `(1, 2) is (Int32, Int32)`. 
		//   Normally this is true, implying `(Int32, Int32)` is a type, but if you
			//   define a `const int Int32`, this becomes false. So, is it a type_pattern
			//   or is it a positional_pattern? A couple more: `(1, 2) is (Int32, int)` 
		//   and `(1, 2) is (Int32, 2)` are both true iff Int32==1. I think the 
		//   answer is positional_pattern; I think after parsing, the compiler tries 
		//   to interpret each subpattern first as a value, and if that fails, next 
		//   as a type. But this contradicts an earlier decision: `~Int32 is Int32` 
		//   returns true! In _this_ case the order is reversed - the pattern is
			//   interpreted first as a type, and next as a value. It's bizarre.
			//   It even looks like a breaking change, as `(1, 2) is (Int32, Int32)` 
		//   would have been true in C# 7, I think (the existence of `const int Int32` 
		//   should not have mattered in C# 7, as a type lookup was being performed.)
			// - Having defined `const int _ = 5;`, it is a syntax error to say
			//   `(object)4 switch { _ - 1 => 1, _ => 0 }` but the expression
			//   `(object)4 switch { +_ - 1 => 1, _ => 0 }` equals 1.
			// - Postfix `++` and `--` cause syntax errors, so I could not find out
			//   whether the expression in `x switch { (P) ++ when - 1 > 0 => 0, _ => 1 }` 
		//   is parsed as `((P)++) when (-1 > 0)` or `(((P) ++when) - 1) > 0`.
			//
			// NOTES ON PRECEDENCE AND LIMITATIONS
			// -----------------------------------
			// - There are undocumented restrictions on the usage of types and their
			//   suffixes. Although `(int, int)? tupleQ` is a valid variable declaration,
			//   `(object)(1,2) switch { (int,int)? => 7, _ => 0 }` is a syntax error, as is
			//   `(object)(1,2) switch { (int,int)? x => 7, _ => 0 }`. Even a simple pattern
			//   like `int?` is a syntax error in this context, but all arrays somehow 
		//   work, e.g. `(T,U)[,]`. `Foo*` is right out.
			// - My experimentation suggests that constant_expression, as you might guess,
			//   has a precedence matching the `is` operator when it appears on the right-
			//   hand side of an is-expression. In a switch expression it can be almost
			//   any expression, except that all operators with precedence below || don't
			//   seem to work (i.e. ?: and ?? and assignment operators).
			//   - `??` fails in a strange way by always saying "A constant value is 
		//     expected" (I would think that `(string)null ?? null` is constant, but,
			//     turns out, it's not, i.e. you can't make a const var with this value.)
			//     The others fail as syntax errors.
			//   - A lambda function can't be used in either context, even if you add
			//     parentheses around it.
			// - But what is the precedence of the relational_expression? Surprisingly,
			//   low precedence operators are not allowed, even in a switch expression:
			//   `(object)x switch { 2 & 3 => 7 }` is allowed, yet the ampersand in
			//   `(object)x switch { <= 2 & 3 => 7 }` produces a syntax error. Even
			//   `(object)x switch { (<= 2 & 3, 4) => 7 }` is a syntax error, although
			//   `(object)x switch { <= (2 & 3) => 7 }` is legal. Shifts are allowed, e.g.
			//   `(object)x switch { <= 2 << 3 => 7 }`.
			// - However, the parser sees `when` as an identifier in some cases and as a 
		//   keyword at other times. Here are a couple of interesting cases:
			//     const int when = 77;
			//     const int Int32 = 32;
			//     (object)true switch { ((Int32)) when -4 > 30 => 7, _ => 0 }
			//     (object)true switch { (Int32) when -4 > 30 => 7, _ => 0 }
			//   The first `switch` expression is `0`; the second is `7`.
			// - `when` is not allowed in an is-expression; you can write
			//   `(object)2 switch { int x when x < 5 => true, _ => false }` but
			//   `(object)2   is     int x when x < 5` is a syntax error.
			//
			// ADDITIONAL NOTES
			// ----------------
			// - Examples of patterns that don't look like normal expressions
			//   nor variable declarations:
			//     `Foo(Bar<Baz, Baz>variable)` // Foo takes only one argument
			//     `(> 0, name: {} x) y`
			//     `(> 0, name: {} x) { name2: Foo(x, y) { > 0 } } y`
			// - Not related to patterns but I just noticed that 
		//   `Foo(out Dictionary<Int32, Int32> d);` is parsed differently than
			//   `Foo(    Dictionary<Int32, Int32>d);`. Better test this in our parser.
			// - Not related to parsing, but this `is` expression weirdly returns false:
			//     struct Foo {
			//        public void Deconstruct(out Dictionary<int, int> d) { d = null; }
			//     }
			//     (object)(new Foo()) is Foo(Dictionary<Int32,Int32>dict) { } foo
			//   And yes, it will return true if the dictionary isn't null.
			//
			// P.S. non-tuple positional patterns behave weirdly, not that a parser cares:
			// https://endjin.com/blog/2019/10/dotnet-csharp-8-positional-patterns-deconstructor-pitfall
			//
			// SIMPLIFIED REFORMULATION
			// ------------------------
			// Here's a simplified version of the above grammar with reduced ambiguity:
			// 
		//   pattern : conjunctive_pattern ('or' conjunctive_pattern)*;      // as before
			//   conjunctive_pattern : negated_pattern ('and' negated_pattern)*; // as before
			//   negated_pattern : 'not' negated_pattern | primary_pattern;      // as before
			//
			//   primary_pattern
			//     : relational_pattern
			//     / typed_pattern // includes complex identifiers and even the discard _
			//     / paren_or_brace_pattern
			//     / expression // using appropriate precedence & excluding `=>`, `?`, `++`, `--`
			//
			//   paren_or_brace_pattern:
			//     ('(' subpatterns? ')' property_subpattern? | property_subpattern) identifier?
			//   typed_pattern:
			//     type (identifier | paren_or_brace_pattern)?
			//   property_subpattern: // as before
			//   subpatterns:         // as before
			//   relational_pattern:  // as before
			// 
		// LOYC TREE MAPPINGS, by example
			// ------------------------------
			//
			// | Example Pattern          | Loyc tree (EC#/LES2 notation) 
		// |--------------------------|-----------------------------
			// | `_`                      | ```_```
			// | `Enum.Value`             | ```Enum.Value```
			// | `2 + 2`                  | ```2 + 2```
			// | `>= 2 + 2`               | ```@`'>=`(2 + 2)```
			// | `var x`                  | ```#var(@``, x)```
			// | `var (x, y)`             | ```#var(@``, (x, y))``` (shorthand for ```#var(@``, @'tuple(x, y))```)
			// | `var (x, (y, z))`        | ```#var(@``, (x, (y, z)))```
			// | `{ } obj`                | ```#var(@'deconstruct(@'tuple()), obj)```
			// | `List<T>`                | ```List!T``` (shorthand for `@'of(List, T)` a.k.a. ``List `'of` T``)
			// | `List<T> list`           | ```#var(List!T, list)```
			// | `List<T>() list`         | ```#var(@'deconstruct(List!T()), list)```
			// | `List<T> { Count: >0 } x`| ```#var(@'deconstruct(List!T(), Count ::= @`'>`(0)), x)```
			// | `List<T>() { Count:7 } x`| ```#var(@'deconstruct(List!T(), Count ::= 7), x)```
			// | `int?`                   | ```@'of(@`'?`, #int32)``` (note: only simple type patterns can use `?`)
			// | `Foo<a, b>?`             | ```@'of(@`'?`, @'of(Foo, a, b))```
			// | `(Foo) { } x`            | ```#var(@'deconstruct(@'tuple(Foo)), x)```
			// | `(Foo) x`                | ```@'cast(x, Foo)```
			// | `(Foo + 0) x`            | ```#var(@'deconstruct(@'tuple(Foo + 0)), x)```
			// | `(a, b)`                 | ```@'deconstruct(@'tuple(a, b))``` (NOT a tuple type!)
			// | `(a, b) { Foo: x }`      | ```@'deconstruct(@'tuple(a, b), Foo ::= x)```
			// | `(> 5, (_, _))`          | ```@'deconstruct(@'tuple(@`'>`(5), @'deconstruct(@'tuple(_, _))))```
			// | `Point(X: > 5, Y: 0)`    | ```@'deconstruct(Point(X ::= @`'>`(5), Y ::= 0))```
			// | `Foo({ Length: 2 })`     | ```@'deconstruct(Foo(@'deconstruct(@'tuple(), Length ::= 2))```
			// | `{ X: 1, Y: >0 }`        | ```@'deconstruct(@'tuple(), X ::= 1, Y ::= @`'>`(0))```
			// | `Foo(X: int x) {Y: >4} f`| ```#var(@'deconstruct(Foo(X ::= #var(#int32, x)), Y ::= @`'>`(4)), f)```
			// | `not null`               | ```@'not(null)```
			// | `not not null`           | ```@'not(@'not(null))```
			// | `>= 'a' and <= 'z'`      | ```@'and(@`'>=`('a'), @`'<=`('z'))```
			// | `0 or 1`                 | ```@'or(0, 1)```
			// | `string or List<char>()` | ```@'or(#string, @'deconstruct(List!#char()))```
		private bool Scan_BracedBlock(Symbol spaceName = null, Symbol target = null, int startIndex = -1)
		{
			if (!TryMatch((int) TT.LBrace))
				return false;
			if (!TryMatch((int) TT.RBrace))
				return false;
			return true;
		}
		
		// ---------------------------------------------------------------------
			// -- "Patterns" (used in `is` and `switch` expressions) ---------------
			// ---------------------------------------------------------------------
			//
			// A pattern can appear after `is` or `case`, or inside the braced block
			// of a switch expression. NOTE: Patterns after `case` are not supported
			// in Enhanced C# v29 until we figure out how to resolve the major syntax 
		// conflict between Enhanced C# and plain C# that was introduced in C# 9.
			//
			// A ridiculously ambiguous grammar for patterns is given on this page:
			// https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/patterns
			// Sadly, not only does it not show any C# 9 features, but the C# 9 proposal at
			// https://github.com/dotnet/csharplang/blob/master/proposals/csharp-9.0/patterns3.md
			// refers to "existing" productions like primary_pattern that DON'T EXIST
			// in the C# 8 version. Anyway, here is my attempt to piece together a 
		// simplified, combined version of the C# 8 grammar and C# 9 partial grammar
			// (which still doesn't include `when` clauses from the `switch` expression):
			//
			// // aka disjunctive_pattern
			// pattern : conjunctive_pattern ('or' conjunctive_pattern)*;
			// conjunctive_pattern : negated_pattern ('and' negated_pattern)*;
			// negated_pattern : 'not' negated_pattern | primary_pattern;
			//
			// primary_pattern
			//     : type identifier     // aka declaration_pattern
			//     | constant_expression // aka constant_pattern
			//     | type                // aka type_pattern
			//     | var_pattern
			//     | positional_pattern
			//     | property_pattern
			//     | relational_pattern
			//     | '(' pattern ')'     // aka parenthesized_pattern
			//     | '_'                 // aka discard_pattern
			//     ;
			// 
		// subpatterns : subpattern (',' subpattern)*;
			// subpattern : (identifier ':')? pattern;
			// 
		// var_pattern : 'var' designation;
			//   designation : identifier | tuple_designation;    
		//   tuple_designation : '(' designation (',' designation)* ')';
			// 
		// positional_pattern : type? '(' subpatterns? ')' property_subpattern? identifier?;
			//
			// property_subpattern : '{' '}' | '{' subpatterns ','? '}';
			// 
		// property_pattern : type? property_subpattern identifier?;
			//
			// relational_pattern : ('<'|'>'|'<='|'>=') relational_expression;
			//   relational_expression : <<no definition is provided!>>
			//
			// NOTES ON AMBIGUITIES:
			// ---------------------
			// - The var_pattern is simply a special case of positional_pattern in which 
		//   the type happens to be `var` and the subpatterns are identifiers/tuples.
			//   But though syntactically it's a subset, its meaning is of course quite 
		//   different. The C# compiler evidently decided to parse it differently,
			//   as (contrary to the grammar above) `var(1, p)` is a syntax error even if 
		//   there is a `class var` in scope. EC# parses it differently, too.
			// - Supposedly, parenthesized_pattern was added in C# 9, but it's actually 
		//   just a special case of positional_pattern; `((var x))` should already have
			//   been valid in C# 8 given the grammar above, though maybe it has a 
		//   different meaning in C# 9? i.e. perhaps in C# 8 it's a double 
		//   deconstruction while in C# 9 there is no deconstruction at all.
			// - Less obvious is that `type` conflicts with `positional_pattern` (and also
			//   `constant_expression`) because `(A, B)` looks like a positional pattern and
			//   also looks like a tuple type (and a tuple value). C#, of course, assumes 
		//   that (A, B) is a pattern and not a type name nor a value.
			// - An expression of the form `(Int32) x` is tricky, since it matches 
		//   positional_pattern and constant_expression equally well. C# treats this 
		//   as constant_pattern, but superficially similar patterns like 
		//   `(Int32 + 1) x` are positional_patterns.
			// - One of the most surprising cases is e.g. `(1, 2) is (Int32, Int32)`. 
		//   Normally this is true, implying `(Int32, Int32)` is a type, but if you
			//   define a `const int Int32`, this becomes false. So, is it a type_pattern
			//   or is it a positional_pattern? A couple more: `(1, 2) is (Int32, int)` 
		//   and `(1, 2) is (Int32, 2)` are both true iff Int32==1. I think the 
		//   answer is positional_pattern; I think after parsing, the compiler tries 
		//   to interpret each subpattern first as a value, and if that fails, next 
		//   as a type. But this contradicts an earlier decision: `~Int32 is Int32` 
		//   returns true! In _this_ case the order is reversed - the pattern is
			//   interpreted first as a type, and next as a value. It's bizarre.
			//   It even looks like a breaking change, as `(1, 2) is (Int32, Int32)` 
		//   would have been true in C# 7, I think (the existence of `const int Int32` 
		//   should not have mattered in C# 7, as a type lookup was being performed.)
			// - Having defined `const int _ = 5;`, it is a syntax error to say
			//   `(object)4 switch { _ - 1 => 1, _ => 0 }` but the expression
			//   `(object)4 switch { +_ - 1 => 1, _ => 0 }` equals 1.
			// - Postfix `++` and `--` cause syntax errors, so I could not find out
			//   whether the expression in `x switch { (P) ++ when - 1 > 0 => 0, _ => 1 }` 
		//   is parsed as `((P)++) when (-1 > 0)` or `(((P) ++when) - 1) > 0`.
			//
			// NOTES ON PRECEDENCE AND LIMITATIONS
			// -----------------------------------
			// - There are undocumented restrictions on the usage of types and their
			//   suffixes. Although `(int, int)? tupleQ` is a valid variable declaration,
			//   `(object)(1,2) switch { (int,int)? => 7, _ => 0 }` is a syntax error, as is
			//   `(object)(1,2) switch { (int,int)? x => 7, _ => 0 }`. Even a simple pattern
			//   like `int?` is a syntax error in this context, but all arrays somehow 
		//   work, e.g. `(T,U)[,]`. `Foo*` is right out.
			// - My experimentation suggests that constant_expression, as you might guess,
			//   has a precedence matching the `is` operator when it appears on the right-
			//   hand side of an is-expression. In a switch expression it can be almost
			//   any expression, except that all operators with precedence below || don't
			//   seem to work (i.e. ?: and ?? and assignment operators).
			//   - `??` fails in a strange way by always saying "A constant value is 
		//     expected" (I would think that `(string)null ?? null` is constant, but,
			//     turns out, it's not, i.e. you can't make a const var with this value.)
			//     The others fail as syntax errors.
			//   - A lambda function can't be used in either context, even if you add
			//     parentheses around it.
			// - But what is the precedence of the relational_expression? Surprisingly,
			//   low precedence operators are not allowed, even in a switch expression:
			//   `(object)x switch { 2 & 3 => 7 }` is allowed, yet the ampersand in
			//   `(object)x switch { <= 2 & 3 => 7 }` produces a syntax error. Even
			//   `(object)x switch { (<= 2 & 3, 4) => 7 }` is a syntax error, although
			//   `(object)x switch { <= (2 & 3) => 7 }` is legal. Shifts are allowed, e.g.
			//   `(object)x switch { <= 2 << 3 => 7 }`.
			// - However, the parser sees `when` as an identifier in some cases and as a 
		//   keyword at other times. Here are a couple of interesting cases:
			//     const int when = 77;
			//     const int Int32 = 32;
			//     (object)true switch { ((Int32)) when -4 > 30 => 7, _ => 0 }
			//     (object)true switch { (Int32) when -4 > 30 => 7, _ => 0 }
			//   The first `switch` expression is `0`; the second is `7`.
			// - `when` is not allowed in an is-expression; you can write
			//   `(object)2 switch { int x when x < 5 => true, _ => false }` but
			//   `(object)2   is     int x when x < 5` is a syntax error.
			//
			// ADDITIONAL NOTES
			// ----------------
			// - Examples of patterns that don't look like normal expressions
			//   nor variable declarations:
			//     `Foo(Bar<Baz, Baz>variable)` // Foo takes only one argument
			//     `(> 0, name: {} x) y`
			//     `(> 0, name: {} x) { name2: Foo(x, y) { > 0 } } y`
			// - Not related to patterns but I just noticed that 
		//   `Foo(out Dictionary<Int32, Int32> d);` is parsed differently than
			//   `Foo(    Dictionary<Int32, Int32>d);`. Better test this in our parser.
			// - Not related to parsing, but this `is` expression weirdly returns false:
			//     struct Foo {
			//        public void Deconstruct(out Dictionary<int, int> d) { d = null; }
			//     }
			//     (object)(new Foo()) is Foo(Dictionary<Int32,Int32>dict) { } foo
			//   And yes, it will return true if the dictionary isn't null.
			//
			// P.S. non-tuple positional patterns behave weirdly, not that a parser cares:
			// https://endjin.com/blog/2019/10/dotnet-csharp-8-positional-patterns-deconstructor-pitfall
			//
			// SIMPLIFIED REFORMULATION
			// ------------------------
			// Here's a simplified version of the above grammar with reduced ambiguity:
			// 
		//   pattern : conjunctive_pattern ('or' conjunctive_pattern)*;      // as before
			//   conjunctive_pattern : negated_pattern ('and' negated_pattern)*; // as before
			//   negated_pattern : 'not' negated_pattern | primary_pattern;      // as before
			//
			//   primary_pattern
			//     : relational_pattern
			//     / typed_pattern // includes complex identifiers and even the discard _
			//     / paren_or_brace_pattern
			//     / expression // using appropriate precedence & excluding `=>`, `?`, `++`, `--`
			//
			//   paren_or_brace_pattern:
			//     ('(' subpatterns? ')' property_subpattern? | property_subpattern) identifier?
			//   typed_pattern:
			//     type (identifier | paren_or_brace_pattern)?
			//   property_subpattern: // as before
			//   subpatterns:         // as before
			//   relational_pattern:  // as before
			// 
		// LOYC TREE MAPPINGS, by example
			// ------------------------------
			//
			// | Example Pattern          | Loyc tree (EC#/LES2 notation) 
		// |--------------------------|-----------------------------
			// | `_`                      | ```_```
			// | `Enum.Value`             | ```Enum.Value```
			// | `2 + 2`                  | ```2 + 2```
			// | `>= 2 + 2`               | ```@`'>=`(2 + 2)```
			// | `var x`                  | ```#var(@``, x)```
			// | `var (x, y)`             | ```#var(@``, (x, y))``` (shorthand for ```#var(@``, @'tuple(x, y))```)
			// | `var (x, (y, z))`        | ```#var(@``, (x, (y, z)))```
			// | `{ } obj`                | ```#var(@'deconstruct(@'tuple()), obj)```
			// | `List<T>`                | ```List!T``` (shorthand for `@'of(List, T)` a.k.a. ``List `'of` T``)
			// | `List<T> list`           | ```#var(List!T, list)```
			// | `List<T>() list`         | ```#var(@'deconstruct(List!T()), list)```
			// | `List<T> { Count: >0 } x`| ```#var(@'deconstruct(List!T(), Count ::= @`'>`(0)), x)```
			// | `List<T>() { Count:7 } x`| ```#var(@'deconstruct(List!T(), Count ::= 7), x)```
			// | `int?`                   | ```@'of(@`'?`, #int32)``` (note: only simple type patterns can use `?`)
			// | `Foo<a, b>?`             | ```@'of(@`'?`, @'of(Foo, a, b))```
			// | `(Foo) { } x`            | ```#var(@'deconstruct(@'tuple(Foo)), x)```
			// | `(Foo) x`                | ```@'cast(x, Foo)```
			// | `(Foo + 0) x`            | ```#var(@'deconstruct(@'tuple(Foo + 0)), x)```
			// | `(a, b)`                 | ```@'deconstruct(@'tuple(a, b))``` (NOT a tuple type!)
			// | `(a, b) { Foo: x }`      | ```@'deconstruct(@'tuple(a, b), Foo ::= x)```
			// | `(> 5, (_, _))`          | ```@'deconstruct(@'tuple(@`'>`(5), @'deconstruct(@'tuple(_, _))))```
			// | `Point(X: > 5, Y: 0)`    | ```@'deconstruct(Point(X ::= @`'>`(5), Y ::= 0))```
			// | `Foo({ Length: 2 })`     | ```@'deconstruct(Foo(@'deconstruct(@'tuple(), Length ::= 2))```
			// | `{ X: 1, Y: >0 }`        | ```@'deconstruct(@'tuple(), X ::= 1, Y ::= @`'>`(0))```
			// | `Foo(X: int x) {Y: >4} f`| ```#var(@'deconstruct(Foo(X ::= #var(#int32, x)), Y ::= @`'>`(4)), f)```
			// | `not null`               | ```@'not(null)```
			// | `not not null`           | ```@'not(@'not(null))```
			// | `>= 'a' and <= 'z'`      | ```@'and(@`'>=`('a'), @`'<=`('z'))```
			// | `0 or 1`                 | ```@'or(0, 1)```
			// | `string or List<char>()` | ```@'or(#string, @'deconstruct(List!#char()))```

		LNode Pattern(Precedence ctx)
		{
			TokenType la0, la1;
			Token op = default(Token);
			LNode result = default(LNode);
			LNode rhs = default(LNode);
			result = ConjunctivePattern(ctx);
			// Line 1223: (&{Is($LI, @@or)} TT.Id ConjunctivePattern)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Id) {
					if (Is(0, sy_or)) {
						la1 = LA(1);
						if (SubExpr_set1.Contains((int) la1)) {
							op = MatchAny();
							rhs = ConjunctivePattern(ctx);
							// line 1224
							result = F.CallInfixOp(result, op, rhs, S.PatternOr);
						} else
							break;
					} else
						break;
				} else
					break;
			}
			return result;
		}

		LNode ConjunctivePattern(Precedence ctx)
		{
			TokenType la0, la1;
			Token op = default(Token);
			LNode result = default(LNode);
			LNode rhs = default(LNode);
			result = NegatedPattern(ctx);
			// Line 1231: greedy(&{Is($LI, @@and)} TT.Id NegatedPattern)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Id) {
					if (Is(0, sy_and)) {
						la1 = LA(1);
						if (SubExpr_set1.Contains((int) la1)) {
							op = MatchAny();
							rhs = NegatedPattern(ctx);
							// line 1232
							result = F.CallInfixOp(result, op, rhs, S.PatternAnd);
						} else
							break;
					} else
						break;
				} else
					break;
			}
			return result;
		}

		LNode NegatedPattern(Precedence ctx)
		{
			TokenType la0, la1;
			LNode got_NegatedPattern = default(LNode);
			Token not = default(Token);
			LNode result = default(LNode);
			// Line 1237: (&{Is($LI, @@not)} TT.Id NegatedPattern / PrimaryPattern)
			la0 = LA0;
			if (la0 == TT.Id) {
				if (Is(0, sy_not)) {
					la1 = LA(1);
					if (SubExpr_set1.Contains((int) la1)) {
						not = MatchAny();
						got_NegatedPattern = NegatedPattern(ctx);
						// line 1238
						result = F.CallPrefixOp(not, got_NegatedPattern, S.PatternNot);
					} else
						result = PrimaryPattern(ctx);
				} else
					result = PrimaryPattern(ctx);
			} else
				result = PrimaryPattern(ctx);
			return result;
		}

		
		LNode PrimaryPattern(Precedence context)
		{
			LNode result = default(LNode);
			// Line 1245: ( RelationalPattern / &(RecursivePattern) RecursivePattern / default SubExprInPattern )
			switch (LA0) {
			case TT.Compare: case TT.EqNeq: case TT.GT: case TT.LEGE:
			case TT.LT:
				result = RelationalPattern(context);
				break;
			case TT.ContextualKeyword: case TT.Id: case TT.LBrace: case TT.LinqKeyword:
			case TT.LParen: case TT.Operator: case TT.Substitute: case TT.TypeKeyword:
				{
					if (Try_Scan_RecursivePattern(0))
						result = RecursivePattern();
					else
						result = SubExprInPattern(context);
				}
				break;
			default:
				result = SubExprInPattern(context);
				break;
			}
			return result;
		}

		// to correctly decide whether to call DataTypeInPattern
		LNode RecursivePattern()
		{
			TokenType la0, la1;
			LNode got_VarDesignation = default(LNode);
			Token lit_lcub = default(Token);
			Token lit_lpar = default(Token);
			Token lit_rcub = default(Token);
			Token lit_rpar = default(Token);
			LNode result = default(LNode);
			LNode type = default(LNode);
			// Line 1260: (&{Is($LI, @@var)} TT.ContextualKeyword VarDesignation / DataTypeInPatternOpt greedy(&!{LA(2) == TT.LBrack} TT.LParen TT.RParen (&{type == @null && subpats.Count == 1} &!(IdAtomButNotPatternKeyword) ~(TT.LBrace) => {..} / (~(EOF))? => {..}) (TT.LBrace TT.RBrace)? | TT.LBrace TT.RBrace)? (IdAtomButNotPatternKeyword)?)
			do {
				la0 = LA0;
				if (la0 == TT.ContextualKeyword) {
					if (Is(0, sy_var)) {
						switch (LA(1)) {
						case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.LParen:
						case TT.Operator: case TT.Substitute: case TT.TypeKeyword:
							{
								var var = MatchAny();
								got_VarDesignation = VarDesignation();
								// line 1262
								result = F.Call(S.Var, MissingHere(), got_VarDesignation, var.StartIndex, got_VarDesignation.Range.EndIndex);
							}
							break;
						default:
							goto matchDataTypeInPatternOpt;
						}
					} else
						goto matchDataTypeInPatternOpt;
				} else
					goto matchDataTypeInPatternOpt;
				break;
			matchDataTypeInPatternOpt:
				{
					// line 1265
					int start = LT0.StartIndex;
					type = DataTypeInPatternOpt();
					// line 1266
					result = type ?? F.Id(S.Tuple, LT0.StartIndex, LT0.EndIndex);
					// Line 1269: greedy(&!{LA(2) == TT.LBrack} TT.LParen TT.RParen (&{type == @null && subpats.Count == 1} &!(IdAtomButNotPatternKeyword) ~(TT.LBrace) => {..} / (~(EOF))? => {..}) (TT.LBrace TT.RBrace)? | TT.LBrace TT.RBrace)?
					la0 = LA0;
					if (la0 == TT.LParen) {
						if (!(LA(2) == TT.LBrack)) {
							la1 = LA(1);
							if (la1 == TT.RParen) {
								lit_lpar = MatchAny();
								lit_rpar = MatchAny();
								// line 1269
								var subpats = SubpatternsIn(lit_lpar, true);
								// Line 1272: (&{type == @null && subpats.Count == 1} &!(IdAtomButNotPatternKeyword) ~(TT.LBrace) => {..} / (~(EOF))? => {..})
								la0 = LA0;
								if (la0 != TT.LBrace) {
									if (type == null && subpats.Count == 1) {
										if (!Try_Scan_IdAtomButNotPatternKeyword(0))
											// line 1276
											result = F.InParens(lit_lpar, subpats[0], lit_rpar);
										else
											// line 1278
											result = F.Call(S.Deconstruct, F.CallPrefix(result, subpats, lit_rpar), start, lit_rpar.EndIndex);
									} else
										// line 1278
										result = F.Call(S.Deconstruct, F.CallPrefix(result, subpats, lit_rpar), start, lit_rpar.EndIndex);
								} else
									// line 1278
									result = F.Call(S.Deconstruct, F.CallPrefix(result, subpats, lit_rpar), start, lit_rpar.EndIndex);
								// Line 1280: (TT.LBrace TT.RBrace)?
								la0 = LA0;
								if (la0 == TT.LBrace) {
									la1 = LA(1);
									if (la1 == TT.RBrace) {
										lit_lcub = MatchAny();
										lit_rcub = MatchAny();
										// line 1280
										result = result.PlusArgs(SubpatternsIn(lit_lcub)).WithRange(start, lit_rcub.EndIndex);
									}
								}
							}
						}
					} else if (la0 == TT.LBrace) {
						la1 = LA(1);
						if (la1 == TT.RBrace) {
							lit_lcub = MatchAny();
							lit_rcub = MatchAny();
							// line 1281
							var args = LNode.List(LNode.Call(result, result)).AddRange(SubpatternsIn(lit_lcub));
							// line 1282
							result = F.Call(S.Deconstruct, args, start, lit_rcub.EndIndex, start, start);
						}
					}
					// Line 1285: (IdAtomButNotPatternKeyword)?
					switch (LA0) {
					case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
					case TT.Substitute: case TT.TypeKeyword:
						{
							if (!(Is(0, sy_and) || Is(0, sy_or) || Is(0, sy_when))) {
								var name = IdAtomButNotPatternKeyword();
								// line 1286
								result = F.Var(result, name, null, start, name.Range.EndIndex);
							}
						}
						break;
					}
					// line 1290
					if (lit_lcub.TypeInt == 0) {
						{
							LNode name_apos, patternInParens;
							if ((result).Calls(CodeSymbols.Var, 2) && (result).Args[0].Calls((Symbol) "'deconstruct", 1) && (result).Args[0].Args[0].Calls(CodeSymbols.Tuple, 1) && (patternInParens = (result).Args[0].Args[0].Args[0]) != null && (name_apos = (result).Args[1]) != null) {
								if (EcsValidators.IsComplexIdentifier(patternInParens)) {
									result = F.Call(S.Cast, name_apos, patternInParens, patternInParens.Range.StartIndex, name_apos.Range.EndIndex);
								}
							}
						}
					}
				}
			} while (false);
			// line 1326
			return result ?? MissingHere();
			return result;
		}

		// to correctly decide whether to call DataTypeInPattern
		bool Try_Scan_RecursivePattern(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return Scan_RecursivePattern();
		}

		// to correctly decide whether to call DataTypeInPattern
		bool Scan_RecursivePattern()
		{
			TokenType la0, la1;
			var startPosition = InputPosition;
			// Line 1260: (&{Is($LI, @@var)} TT.ContextualKeyword VarDesignation / DataTypeInPatternOpt greedy(&!{LA(2) == TT.LBrack} TT.LParen TT.RParen  (TT.LBrace TT.RBrace)? | TT.LBrace TT.RBrace)? (IdAtomButNotPatternKeyword)?)
			do {
				la0 = LA0;
				if (la0 == TT.ContextualKeyword) {
					if (Is(0, sy_var)) {
						switch (LA(1)) {
						case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.LParen:
						case TT.Operator: case TT.Substitute: case TT.TypeKeyword:
							{
								Skip();
								if (!Scan_VarDesignation())
									return false;
							}
							break;
						default:
							goto matchDataTypeInPatternOpt;
						}
					} else
						goto matchDataTypeInPatternOpt;
				} else
					goto matchDataTypeInPatternOpt;
				break;
			matchDataTypeInPatternOpt:
				{
					if (!Scan_DataTypeInPattern())
						return false;
					// Line 1269: greedy(&!{LA(2) == TT.LBrack} TT.LParen TT.RParen  (TT.LBrace TT.RBrace)? | TT.LBrace TT.RBrace)?
					la0 = LA0;
					if (la0 == TT.LParen) {
						if (!(LA(2) == TT.LBrack)) {
							la1 = LA(1);
							if (la1 == TT.RParen) {
								Skip();
								Skip();
								// Line 1280: (TT.LBrace TT.RBrace)?
								la0 = LA0;
								if (la0 == TT.LBrace) {
									la1 = LA(1);
									if (la1 == TT.RBrace) {
										Skip();
										Skip();
									}
								}
							}
						}
					} else if (la0 == TT.LBrace) {
						la1 = LA(1);
						if (la1 == TT.RBrace) {
							Skip();
							Skip();
						}
					}
					// Line 1285: (IdAtomButNotPatternKeyword)?
					switch (LA0) {
					case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
					case TT.Substitute: case TT.TypeKeyword:
						{
							if (!(Is(0, sy_and) || Is(0, sy_or) || Is(0, sy_when)))
								if (!Scan_IdAtomButNotPatternKeyword())
									return false;
						}
						break;
					}
				}
			} while (false);
			// line 1314
			if (startPosition == InputPosition) {
				return false;
			}
			// Line 1319: ( (TT.Add|TT.BQString|TT.CheckedOrUnchecked|TT.ColonColon|TT.Compare|TT.DivMod|TT.Dot|TT.DotDot|TT.IncDec|TT.LBrack|TT.Literal|TT.LParen|TT.Mul|TT.New|TT.Not|TT.NotBits|TT.Power|TT.PtrArrow|TT.QuickBind|TT.Sub|TT.Switch|TT.Typeof) / (TT.LT TT.LT | TT.GT TT.GT) / default {..} )
			switch (LA0) {
			case TT.Add: case TT.BQString: case TT.CheckedOrUnchecked: case TT.ColonColon:
			case TT.Compare: case TT.DivMod: case TT.Dot: case TT.DotDot:
			case TT.IncDec: case TT.LBrack: case TT.Literal: case TT.LParen:
			case TT.Mul: case TT.New: case TT.Not: case TT.NotBits:
			case TT.Power: case TT.PtrArrow: case TT.QuickBind: case TT.Sub:
			case TT.Switch: case TT.Typeof:
				{
					Skip();
					// line 1321
					return false;
				}
				break;
			case TT.GT: case TT.LT:
				{
					la1 = LA(1);
					if (la1 == TT.GT || la1 == TT.LT) {
						// Line 1322: (TT.LT TT.LT | TT.GT TT.GT)
						la0 = LA0;
						if (la0 == TT.LT) {
							Skip();
							if (!TryMatch((int) TT.LT))
								return false;
						} else {
							if (!TryMatch((int) TT.GT))
								return false;
							if (!TryMatch((int) TT.GT))
								return false;
						}
						// line 1322
						return false;
					} else
						// line 1323
						return true;
				}
				break;
			default:
				// line 1323
				return true;
				break;
			}
			return true;
		}

		LNode IdAtomButNotPatternKeyword()
		{
			LNode result = default(LNode);
			Check(!(Is(0, sy_and) || Is(0, sy_or) || Is(0, sy_when)), "Did not expect Is($LI, @@and) || Is($LI, @@or) || Is($LI, @@when)");
			result = IdAtom();
			return result;
		}

		bool Try_Scan_IdAtomButNotPatternKeyword(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return Scan_IdAtomButNotPatternKeyword();
		}

		bool Scan_IdAtomButNotPatternKeyword()
		{
			if (Is(0, sy_and) || Is(0, sy_or) || Is(0, sy_when))
				return false;
			if (!Scan_IdAtom())
				return false;
			return true;
		}

		private 
		LNode VarDesignation()
		{
			Token lit_lpar = default(Token);
			Token lit_rpar = default(Token);
			LNode result = default(LNode);
			// Line 1335: (IdAtom | TT.LParen TT.RParen)
			switch (LA0) {
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
			case TT.Substitute: case TT.TypeKeyword:
				result = IdAtom();
				break;
			default:
				{
					lit_lpar = Match((int) TT.LParen);
					lit_rpar = Match((int) TT.RParen);
					// line 1338
					var contents = Down(lit_lpar) ? Up(VarTupleContents()) : LNode.List();
					result = F.CallBrackets(S.Tuple, lit_lpar, contents, lit_rpar);
				}
				break;
			}
			return result;
		}

		private 
		bool Scan_VarDesignation()
		{
			// Line 1335: (IdAtom | TT.LParen TT.RParen)
			switch (LA0) {
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
			case TT.Substitute: case TT.TypeKeyword:
				if (!Scan_IdAtom())
					return false;
				break;
			default:
				{
					if (!TryMatch((int) TT.LParen))
						return false;
					if (!TryMatch((int) TT.RParen))
						return false;
				}
				break;
			}
			return true;
		}

		LNodeList VarTupleContents()
		{
			TokenType la0;
			LNodeList result = default(LNodeList);
			result.Add(VarDesignation());
			// Line 1344: (TT.Comma VarDesignation)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Comma) {
					Skip();
					result.Add(VarDesignation());
				} else
					break;
			}
			return result;
		}

		LNodeList SubPatterns(bool inParens)
		{
			TokenType la0, la1;
			LNodeList result = default(LNodeList);
			// Line 1353: (SubPattern (TT.Comma SubPattern)* (TT.Comma)?)?
			la0 = LA0;
			if (SubExpr_set1.Contains((int) la0)) {
				result.Add(SubPattern());
				// Line 1354: (TT.Comma SubPattern)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						la1 = LA(1);
						if (SubExpr_set1.Contains((int) la1)) {
							Skip();
							result.Add(SubPattern());
						} else
							break;
					} else
						break;
				}
				// Line 1355: (TT.Comma)?
				la0 = LA0;
				if (la0 == TT.Comma) {
					Skip();
					// line 1356
					if ((inParens)) {
						Error(-1, "Expected subpattern after ','");
					}
				}
			}
			return result;
		}

		LNode SubPattern()
		{
			TokenType la0, la1;
			LNode got_Pattern = default(LNode);
			Token lit_colon = default(Token);
			Token param = default(Token);
			LNode result = default(LNode);
			// Line 1364: (TT.Id TT.Colon Pattern / Pattern)
			do {
				la0 = LA0;
				if (la0 == TT.Id) {
					if (Try_Scan_RecursivePattern(0)) {
						la1 = LA(1);
						if (la1 == TT.Colon)
							goto match1;
						else
							result = Pattern(EP.IfElse);
					} else {
						la1 = LA(1);
						if (la1 == TT.Colon)
							goto match1;
						else
							result = Pattern(EP.IfElse);
					}
				} else
					result = Pattern(EP.IfElse);
				break;
			match1:
				{
					param = MatchAny();
					lit_colon = MatchAny();
					got_Pattern = Pattern(EP.IfElse);
					// line 1364
					result = F.CallInfixOp(F.Id(param), lit_colon, got_Pattern, S.NamedArg);
				}
			} while (false);
			return result;
		}

		private 
		LNode RelationalPattern(Precedence context)
		{
			LNode e = default(LNode);
			Token op = default(Token);
			op = MatchAny();
			e = SubExprInPattern(context);
			// line 1373
			return F.CallPrefixOp(op, e);
		}

		LNode SubExprInPattern(Precedence context)
		{
			LNode result = default(LNode);
			// line 1377
			var old_14 = _insidePattern;
			_insidePattern = true;
			try {
				result = SubExpr(context);
				return result;
			} finally { _insidePattern = old_14; }
		}
		
		

		
		LNode DataTypeInPatternOpt()
		{
			TokenType la1, la2;
			LNode result = default(LNode);
			// Line 1394: (TT.LParen TT.RParen (TT.LBrack|TT.QuestionMark) => DataType / ComplexId TypeSuffixOpt)?
			switch (LA0) {
			case TT.LParen:
				{
					la1 = LA(1);
					if (la1 == TT.RParen) {
						la2 = LA(2);
						if (la2 == TT.LBrack || la2 == TT.QuestionMark)
							result = DataType(afterAs: true);
					}
				}
				break;
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
			case TT.Substitute: case TT.TypeKeyword:
				{
					result = ComplexId();
					TypeSuffixOpt(TypeParseMode.Pattern, out _, ref result);
				}
				break;
			}
			return result;
		}
		
		

		bool Try_Scan_DataTypeInPattern(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return Scan_DataTypeInPattern();
		}
		bool Scan_DataTypeInPattern()
		{
			TokenType la1, la2;
			// Line 1394: (TT.LParen TT.RParen (TT.LBrack|TT.QuestionMark) => DataType / ComplexId TypeSuffixOpt)?
			switch (LA0) {
			case TT.LParen:
				{
					la1 = LA(1);
					if (la1 == TT.RParen) {
						la2 = LA(2);
						if (la2 == TT.LBrack || la2 == TT.QuestionMark)
							if (!Scan_DataType(afterAs: true))
								return false;
					}
				}
				break;
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
			case TT.Substitute: case TT.TypeKeyword:
				{
					if (!Scan_ComplexId())
						return false;
					if (!Scan_TypeSuffixOpt(TypeParseMode.Pattern))
						return false;
				}
				break;
			}
			return true;
		}

		// ---------------------------------------------------------------------
		// -- Switch expression body -------------------------------------------
		// ---------------------------------------------------------------------
		// I dislike C# 8/9's incredibly ambiguous pattern matching design, and 
		// the weird repurposing of the lambda arrow => as a low-precedence "case" 
		// operator. The simpler EC# design allowed `break` to have an argument 
		// (the value to return from the `switch`) and then, if you wrote an 
		// expression without a semicolon at the end, it meant "use this value as 
		// the result". This way the switch expression and switch statement have 
		// identical syntax, each case could have multiple patterns (so users 
		// need not duplicate identical handlers), and of course the missing-
		// semicolon rule can be generalized to `if/else` and other statements:
		//
		// return switch(obj) { 
		//   case int i: i.ToString() + " (number)"
		//   case null: "(null)"
		//   case string s:
		//     if (s == "") 
		//       break "(empty)";
		//     s = s.Trim();
		//     if (s == "") "(whitespace)" else s
		//   default: y.ToString()
		// }
		//
		// It's nice that Microsoft chose a compact syntax, but why not pick 
		// something like this that (1) fits better into C#'s historical syntax 
		// choices and (2) generalizes to the rest of the language? It doesn't
		// hurt that my design is simpler and easier to implement either (like, 
		// not to brag, but is anyone reading this? I think not.)
		//
		// And if they were really hung up on achieving a compact syntax, it 
		// would have made more sense to use `:` instead of `=>`. Not only does
		// `expr:` make sense as an abbreviation for `case expr:`, it also 
		// makes parsing easier.
		LNode SwitchBody()
		{
			Token lit_lcub = default(Token);
			Token lit_rcub = default(Token);
			lit_lcub = Match((int) TT.LBrace);
			lit_rcub = Match((int) TT.RBrace);
			// line 1440
			var arms = Down(lit_lcub) ? SwitchExpressionArms() : LNode.List();
			return Up(F.CallBrackets(S.Braces, lit_lcub, arms, lit_rcub));
		}

		LNodeList SwitchExpressionArms()
		{
			TokenType la0, la1;
			LNodeList result = default(LNodeList);
			// Line 1447: (SwitchExpressionArm (TT.Comma EOF / TT.Comma SwitchExpressionArm)*)?
			la0 = LA0;
			if (SubExpr_set1.Contains((int) la0)) {
				result.Add(SwitchExpressionArm());
				// Line 1448: (TT.Comma EOF / TT.Comma SwitchExpressionArm)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						la1 = LA(1);
						if (la1 == EOF) {
							Skip();
							Skip();
						} else if (SubExpr_set1.Contains((int) la1)) {
							Skip();
							result.Add(SwitchExpressionArm());
						} else
							goto error;
					} else if (la0 == EOF)
						break;
					else
						goto error;
					continue;
				error:
					{
						// line 1450
						Error("Expected ',' or '}'");
						// Line 1450: (~(EOF|TT.Comma))*
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
			Match((int) EOF);
			return result;
		}

		private LNode SwitchExpressionArm()
		{
			TokenType la0;
			LNode lhs = default(LNode);
			Token lit_equals_gt = default(Token);
			LNode result = default(LNode);
			LNode rhs = default(LNode);
			lhs = Pattern(EcsPrecedence.IfElse);
			// Line 1455: (CaseGuard)?
			la0 = LA0;
			if (la0 == TT.ContextualKeyword)
				CaseGuard(ref lhs);
			lit_equals_gt = Match((int) TT.LambdaArrow);
			rhs = ExprStart(false);
			// line 1456
			result = F.CallInfixOp(lhs, lit_equals_gt, rhs);
			return result;
		}

		private void CaseGuard(ref LNode lhs)
		{
			LNode got_SubExpr = default(LNode);
			Token op = default(Token);
			Check(Is(0, sy_when), "Expected Is($LI, @@when)");
			op = MatchAny();
			// line 1461
			var old_15 = _insidePattern;
			_insidePattern = true;
			try {
				got_SubExpr = SubExpr(EP.Assign);
				// line 1463
				lhs = F.CallInfixOp(lhs, op, got_SubExpr, S.When);
			} finally { _insidePattern = old_15; }
		}
		
		// =====================================================================
			// == Attributes =======================================================
			// =====================================================================

		bool NormalAttributes(ref LNodeList attrs)
		{
			TokenType la0, la1;
			bool result = default(bool);
			// Line 1472: (&!{Down($LI) && Up(Try_Scan_AsmOrModLabel(0))} TT.LBrack TT.RBrack)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.LBrack) {
					if (!(Down(0) && Up(Try_Scan_AsmOrModLabel(0)))) {
						la1 = LA(1);
						if (la1 == TT.RBrack) {
							var t = MatchAny();
							Skip();
							// line 1475
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
			// Line 1472: (&!{Down($LI) && Up(Try_Scan_AsmOrModLabel(0))} TT.LBrack TT.RBrack)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.LBrack) {
					if (!(Down(0) && Up(Try_Scan_AsmOrModLabel(0)))) {
						la1 = LA(1);
						if (la1 == TT.RBrack) {
							Skip();
							Skip();
						} else
							break;
					} else
						break;
				} else
					break;
			}
			return true;
		}

		void AttributeContents(ref LNodeList attrs)
		{
			TokenType la1;
			Token lit_colon = default(Token);
			// line 1484
			Token attrTarget = default(Token);
			// Line 1485: ((TT.ContextualKeyword|TT.Id|TT.LinqKeyword|TT.Return) TT.Colon ExprList / ExprList)
			switch (LA0) {
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Return:
				{
					la1 = LA(1);
					if (la1 == TT.Colon) {
						attrTarget = MatchAny();
						lit_colon = MatchAny();
						// line 1486
						LNodeList newAttrs = new LNodeList();
						ExprList(ref newAttrs, allowTrailingComma: true, allowUnassignedVarDecl: true);
						// line 1489
						var attrTargetId = F.Id(attrTarget);
						for (int i = 0; i < newAttrs.Count; i++) {
							var attr = newAttrs[i];
							if ((!IsNamedArg(attr))) {
								attr = F.CallInfixOp(attrTargetId, S.NamedArg, lit_colon, attr);
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

		void AttributeKeywords(ref LNodeList attrs)
		{
			TokenType la0;
			// Line 1507: (TT.AttrKeyword)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.AttrKeyword) {
					var t = MatchAny();
					// line 1508
					attrs.Add(F.Id(t));
				} else
					break;
			}
		}

		void TParamAttributeKeywords(ref LNodeList attrs)
		{
			TokenType la0;
			// Line 1513: ((TT.AttrKeyword|TT.In))*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.AttrKeyword || la0 == TT.In) {
					var t = MatchAny();
					// line 1514
					attrs.Add(F.Id(t));
				} else
					break;
			}
		}
		
		// =====================================================================
			// == LINQ =============================================================
			// =====================================================================

		bool Try_Scan_TParamAttributeKeywords(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return Scan_TParamAttributeKeywords();
		}
		bool Scan_TParamAttributeKeywords()
		{
			TokenType la0;
			// Line 1513: ((TT.AttrKeyword|TT.In))*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.AttrKeyword || la0 == TT.In)
					Skip();
				else
					break;
			}
			return true;
		}

		public LNode LinqQueryExpression()
		{
			// line 1524
			int startIndex = LT0.StartIndex;
			var old_16 = _insideLinqExpr;
			_insideLinqExpr = true;
			try {
				var parts = LNode.List();
				parts.Add(LinqFromClause());
				QueryBody(ref parts);
				// line 1530
				return F.Call(S.Linq, parts, startIndex, parts.Last.Range.EndIndex, startIndex, startIndex);
			} finally { _insideLinqExpr = old_16; }
		}

		private LNode LinqFromClause()
		{
			Check(Is(0, sy_from), "Expected Is($LI, @@from)");
			var kw = Match((int) TT.LinqKeyword);
			var e = Var_In_Expr();
			// line 1537
			return F.CallPrefixOp(S.From, kw, e);
		}
		static readonly HashSet<int> Var_In_Expr_set0 = NewSet((int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.At, (int) TT.Backslash, (int) TT.Base, (int) TT.BQString, (int) TT.CheckedOrUnchecked, (int) TT.Colon, (int) TT.ColonColon, (int) TT.Compare, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.Default, (int) TT.Delegate, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.Forward, (int) TT.GT, (int) TT.Id, (int) TT.IncDec, (int) TT.Is, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LEGE, (int) TT.LinqKeyword, (int) TT.Literal, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.Operator, (int) TT.OrBits, (int) TT.OrXor, (int) TT.PipeArrow, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.QuickBindSet, (int) TT.RParen, (int) TT.Set, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.This, (int) TT.TypeKeyword, (int) TT.Typeof, (int) TT.XorBits);
		static readonly HashSet<int> Var_In_Expr_set1 = NewSet((int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.At, (int) TT.Backslash, (int) TT.Base, (int) TT.BQString, (int) TT.CheckedOrUnchecked, (int) TT.Colon, (int) TT.ColonColon, (int) TT.Compare, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.Default, (int) TT.Delegate, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.Forward, (int) TT.GT, (int) TT.Id, (int) TT.IncDec, (int) TT.Is, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LEGE, (int) TT.LinqKeyword, (int) TT.Literal, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.Operator, (int) TT.OrBits, (int) TT.OrXor, (int) TT.PipeArrow, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.QuickBindSet, (int) TT.Set, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.This, (int) TT.TypeKeyword, (int) TT.Typeof, (int) TT.XorBits);

		LNode Var_In_Expr()
		{
			TokenType la1;
			LNode got_VarIn = default(LNode);
			LNode result = default(LNode);
			// Line 1541: (&(VarIn) VarIn ExprStart / ExprStart)
			do {
				switch (LA0) {
				case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.LParen:
				case TT.Operator: case TT.Substitute: case TT.TypeKeyword:
					{
						if (Try_Scan_VarIn(0)) {
							if (Down(0) && Up(Scan_TupleTypeList() && LA0 == EOF)) {
								la1 = LA(1);
								if (Var_In_Expr_set0.Contains((int) la1))
									goto match1;
								else
									result = ExprStart(false);
							} else {
								la1 = LA(1);
								if (Var_In_Expr_set1.Contains((int) la1))
									goto match1;
								else
									result = ExprStart(false);
							}
						} else
							result = ExprStart(false);
					}
					break;
				default:
					result = ExprStart(false);
					break;
				}
				break;
			match1:
				{
					got_VarIn = VarIn(out Token inTok);
					var e = ExprStart(false);
					// line 1544
					return F.CallInfixOp(got_VarIn, S.In, inTok, e);
				}
			} while (false);
			return result;
		}

		
		private void QueryBody(ref LNodeList parts)
		{
			TokenType la0;
			// Line 1550: greedy(QueryBodyClause)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.LinqKeyword) {
					if (Is(0, sy_from) || Is(0, sy_let) || Is(0, sy_join) || Is(0, sy_orderby))
						parts.Add(QueryBodyClause());
					else
						break;
				} else if (la0 == TT.ContextualKeyword)
					parts.Add(QueryBodyClause());
				else
					break;
			}
			// Line 1551: (LinqGroupClause | LinqSelectClause)
			la0 = LA0;
			if (la0 == TT.LinqKeyword) {
				if (Is(0, sy_group))
					parts.Add(LinqGroupClause());
				else
					parts.Add(LinqSelectClause());
			} else {
				// line 1553
				Error("Expected 'select' or 'group' clause to end LINQ query");
			}
			// Line 1555: greedy(QueryContinuation)?
			la0 = LA0;
			if (la0 == TT.LinqKeyword) {
				if (Is(0, sy_into))
					parts.Add(QueryContinuation());
			}
		}

		
		private LNode QueryBodyClause()
		{
			TokenType la0;
			LNode result = default(LNode);
			// Line 1560: ( LinqFromClause | LinqLet | LinqWhere | LinqJoin | LinqOrderBy )
			la0 = LA0;
			if (la0 == TT.LinqKeyword) {
				if (Is(0, sy_from))
					result = LinqFromClause();
				else if (Is(0, sy_let))
					result = LinqLet();
				else if (Is(0, sy_join))
					result = LinqJoin();
				else
					result = LinqOrderBy();
			} else
				result = LinqWhere();
			return result;
		}

		private LNode LinqLet()
		{
			var kw = MatchAny();
			var e = ExprStart(false);
			// line 1569
			if ((!e.Calls(S.Assign, 2))) {
				Error("Expected an assignment after 'let'");
			}
			// line 1570
			return F.CallPrefixOp(S.Let, kw, e);
		}

		private LNode LinqWhere()
		{
			Check(Is(0, sy_where), "Expected Is($LI, @@where)");
			var kw = Match((int) TT.ContextualKeyword);
			var e = ExprStart(false);
			// line 1575
			return F.CallPrefixOp(S.WhereClause, kw, e);
		}

		
		private LNode LinqJoin()
		{
			TokenType la0;
			LNode from = default(LNode);
			LNode got_IdAtom = default(LNode);
			var kw = MatchAny();
			from = Var_In_Expr();
			Check(Is(0, sy_on), "Expected Is($LI, @@on)");
			Match((int) TT.LinqKeyword);
			var lhs = ExprStart(false);
			Check(Is(0, sy_equals), "Expected Is($LI, @@equals)");
			var eq = Match((int) TT.LinqKeyword);
			var rhs = ExprStart(false);
			// line 1585
			var equality = F.CallInfixOp(lhs, sy__numequals, eq, rhs);
			// Line 1591: (&{Is($LI, @@into)} TT.LinqKeyword IdAtom / {..})
			la0 = LA0;
			if (la0 == TT.LinqKeyword) {
				if (Is(0, sy_into)) {
					var intoKw = MatchAny();
					got_IdAtom = IdAtom();
					// line 1592
					var into = F.CallPrefixOp(S.Into, intoKw, got_IdAtom);
					// line 1593
					var args = LNode.List(from, equality, into);
					// line 1594
					return F.CallPrefixOp(S.Join, kw, args);
				} else
					// line 1596
					return F.CallPrefixOp(S.Join, kw, LNode.List(from, equality));
			} else
				// line 1596
				return F.CallPrefixOp(S.Join, kw, LNode.List(from, equality));
		}

		
		private LNode LinqOrderBy()
		{
			TokenType la0;
			Check(Is(0, sy_orderby), "Expected Is($LI, @@orderby)");
			var kw = MatchAny();
			// line 1603
			var parts = LNode.List();
			parts.Add(LinqOrdering());
			// Line 1604: (TT.Comma LinqOrdering)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Comma) {
					Skip();
					parts.Add(LinqOrdering());
				} else
					break;
			}
			// line 1605
			return F.CallPrefixOp(S.OrderBy, kw, parts);
		}

		private LNode LinqOrdering()
		{
			TokenType la0;
			Token dir = default(Token);
			LNode result = default(LNode);
			result = ExprStart(false);
			// Line 1611: greedy(&{Is($LI, @@ascending)} TT.LinqKeyword | &{Is($LI, @@descending)} TT.LinqKeyword)?
			la0 = LA0;
			if (la0 == TT.LinqKeyword) {
				if (Is(0, sy_ascending)) {
					dir = MatchAny();
					// line 1612
					result = F.CallSuffixOp(result, S.Ascending, dir);
				} else if (Is(0, sy_descending)) {
					dir = MatchAny();
					// line 1614
					result = F.CallSuffixOp(result, S.Descending, dir);
				}
			}
			return result;
		}

		private LNode LinqSelectClause()
		{
			Check(Is(0, sy_select), "Expected Is($LI, @@select)");
			var kw = MatchAny();
			var e = ExprStart(false);
			// line 1620
			return F.CallPrefixOp(S.Select, kw, e);
		}

		private LNode LinqGroupClause()
		{
			TokenType la0;
			Token by = default(Token);
			Token kw = default(Token);
			LNode lhs = default(LNode);
			LNode rhs = default(LNode);
			kw = MatchAny();
			lhs = ExprStart(false);
			// Line 1625: (&{Is($LI, @@by)} TT.LinqKeyword ExprStart)
			la0 = LA0;
			if (la0 == TT.LinqKeyword) {
				Check(Is(0, sy_by), "Expected Is($LI, @@by)");
				by = MatchAny();
				rhs = ExprStart(false);
			} else {
				// line 1626
				Error("Expected 'by'");
				rhs = MissingHere();
			}
			// line 1627
			return F.CallPrefixOp(S.GroupBy, kw, LNode.List(lhs, rhs));
		}

		
		private LNode QueryContinuation()
		{
			LNode got_IdAtom = default(LNode);
			Token kw = default(Token);
			kw = MatchAny();
			got_IdAtom = IdAtom();
			// line 1633
			var parts = LNode.List(got_IdAtom);
			QueryBody(ref parts);
			// line 1635
			return F.CallPrefixOp(S.Into, kw, parts);
		}
		
		// =====================================================================
			// == Statements =======================================================
			// =====================================================================

		
		public LNode Stmt()
		{
			LNode result = default(LNode);
			// line 1646
			var attrs = LNodeList.Empty;
			// line 1647
			int startIndex = LT0.StartIndex;
			NormalAttributes(ref attrs);
			AttributeKeywords(ref attrs);
			// line 1658
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
		LNode MethodOrPropertyOrVarStmt(int startIndex, LNodeList attrs)
		{
			TokenType la0;
			LNode result = default(LNode);
			// Line 1742: ( TraitDecl / AliasDecl / MethodOrPropertyOrVar )
			la0 = LA0;
			if (la0 == TT.ContextualKeyword) {
				if (Is(0, sy_trait)) {
					switch (LA(1)) {
					case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.Operator:
					case TT.Substitute: case TT.TypeKeyword:
						{
							result = TraitDecl(startIndex);
							// line 1742
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
					case TT.Substitute: case TT.TypeKeyword:
						{
							result = AliasDecl(startIndex);
							// line 1743
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
		static readonly HashSet<int> KeywordStmt_set0 = NewSet((int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.AttrKeyword, (int) TT.Base, (int) TT.Break, (int) TT.CheckedOrUnchecked, (int) TT.ContextualKeyword, (int) TT.Continue, (int) TT.Default, (int) TT.Delegate, (int) TT.Dot, (int) TT.DotDot, (int) TT.Forward, (int) TT.Goto, (int) TT.Id, (int) TT.IncDec, (int) TT.Is, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LinqKeyword, (int) TT.Literal, (int) TT.LParen, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.Operator, (int) TT.Power, (int) TT.Return, (int) TT.Semicolon, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.Switch, (int) TT.This, (int) TT.Throw, (int) TT.TypeKeyword, (int) TT.Typeof);
		static readonly HashSet<int> KeywordStmt_set1 = NewSet((int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.AttrKeyword, (int) TT.Base, (int) TT.Break, (int) TT.CheckedOrUnchecked, (int) TT.ContextualKeyword, (int) TT.Continue, (int) TT.Default, (int) TT.Delegate, (int) TT.Dot, (int) TT.DotDot, (int) TT.Forward, (int) TT.Goto, (int) TT.Id, (int) TT.IncDec, (int) TT.Is, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LinqKeyword, (int) TT.Literal, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.Operator, (int) TT.Power, (int) TT.Return, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.Switch, (int) TT.This, (int) TT.Throw, (int) TT.TypeKeyword, (int) TT.Typeof);

		// Statements that begin with a keyword
		LNode KeywordStmt(int startIndex, LNodeList attrs, bool hasWordAttrs)
		{
			TokenType la1;
			// line 1751
			LNode r;
			bool addAttrs = true;
			string showWordAttrErrorFor = null;
			// Line 1755: ( ((IfStmt | EventDecl | DelegateDecl | SpaceDecl | EnumDecl | CheckedOrUncheckedStmt | DoStmt | CaseStmt | ReturnBreakContinueThrow TT.Semicolon) | (GotoCaseStmt TT.Semicolon / GotoStmt TT.Semicolon) | SwitchStmt | WhileStmt | ForStmt | ForEachStmt) | (UsingStmt / UsingDirective) | LockStmt | FixedStmt | TryStmt | PPNullaryDirective | PPStringDirective )
			do {
				switch (LA0) {
				case TT.If:
					{
						r = IfStmt(startIndex);
						// line 1756
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
						// line 1758
						addAttrs = false;
					}
					break;
				case TT.Class: case TT.Interface: case TT.Namespace: case TT.Struct:
					r = SpaceDecl(startIndex);
					break;
				case TT.Enum:
					r = EnumDecl(startIndex);
					break;
				case TT.CheckedOrUnchecked:
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
							// line 1779
							showWordAttrErrorFor = "using statement";
						} else if (KeywordStmt_set1.Contains((int) la1)) {
							r = UsingDirective(startIndex, attrs);
							// line 1780
							addAttrs = false;
							// line 1781
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
				case TT.CSIclear: case TT.CSIhelp: case TT.CSIreset:
					r = PPNullaryDirective(startIndex);
					break;
				case TT.CSIload: case TT.CSIreference: case TT.PPnullable:
					r = PPStringDirective(startIndex);
					break;
				default:
					goto error;
				}
				break;
			error:
				{
					// line 1787
					r = Error("Bug: Keyword statement expected, but got '{0}'", CurrentTokenText());
					ScanToEndOfStmt();
				}
			} while (false);
			// line 1791
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
		LNode IdStmt(int startIndex, LNodeList attrs, bool hasWordAttrs)
		{
			TokenType la1;
			LNode result = default(LNode);
			// line 1805
			bool addAttrs = false;
			string showWordAttrErrorFor = null;
			// Line 1808: ( Constructor / BlockCallStmt / LabelStmt / &(DataType TT.This) DataType => MethodOrPropertyOrVar / ExprStatement )
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
				case TT.LParen: case TT.Operator: case TT.Substitute: case TT.TypeKeyword:
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
					// line 1809
					showWordAttrErrorFor = "old-style constructor";
				}
				break;
			matchBlockCallStmt:
				{
					result = BlockCallStmt(startIndex);
					// line 1811
					showWordAttrErrorFor = "block-call statement";
					addAttrs = true;
				}
				break;
			matchLabelStmt:
				{
					result = LabelStmt(startIndex);
					// line 1813
					addAttrs = true;
				}
				break;
			matchExprStatement:
				{
					result = ExprStatement();
					// line 1817
					showWordAttrErrorFor = "expression";
					addAttrs = true;
				}
			} while (false);
			// line 1820
			if (addAttrs) {
				result = result.PlusAttrs(attrs);
			}
			if (hasWordAttrs && showWordAttrErrorFor != null) {
				NonKeywordAttrError(attrs, showWordAttrErrorFor);
			}
			return result;
		}
		static readonly HashSet<int> OtherStmt_set0 = NewSet((int) EOF, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.As, (int) TT.At, (int) TT.Base, (int) TT.BQString, (int) TT.Break, (int) TT.Catch, (int) TT.CheckedOrUnchecked, (int) TT.ColonColon, (int) TT.Compare, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.Continue, (int) TT.Default, (int) TT.Delegate, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.Else, (int) TT.EqNeq, (int) TT.Finally, (int) TT.Forward, (int) TT.Goto, (int) TT.GT, (int) TT.Id, (int) TT.In, (int) TT.IncDec, (int) TT.Is, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LEGE, (int) TT.LinqKeyword, (int) TT.Literal, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.Operator, (int) TT.OrBits, (int) TT.OrXor, (int) TT.PipeArrow, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Return, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.Switch, (int) TT.This, (int) TT.Throw, (int) TT.TypeKeyword, (int) TT.Typeof, (int) TT.Using, (int) TT.While, (int) TT.XorBits);
		static readonly HashSet<int> OtherStmt_set1 = NewSet((int) EOF, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.As, (int) TT.At, (int) TT.BQString, (int) TT.Catch, (int) TT.ColonColon, (int) TT.Compare, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.Else, (int) TT.EqNeq, (int) TT.Finally, (int) TT.GT, (int) TT.In, (int) TT.IncDec, (int) TT.Is, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LEGE, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.PipeArrow, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sub, (int) TT.Switch, (int) TT.Using, (int) TT.While, (int) TT.XorBits);
		static readonly HashSet<int> OtherStmt_set2 = NewSet((int) EOF, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.As, (int) TT.At, (int) TT.BQString, (int) TT.Catch, (int) TT.ColonColon, (int) TT.Compare, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.Else, (int) TT.EqNeq, (int) TT.Finally, (int) TT.GT, (int) TT.In, (int) TT.IncDec, (int) TT.Is, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LEGE, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.PipeArrow, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sub, (int) TT.Switch, (int) TT.Using, (int) TT.While, (int) TT.XorBits);
		static readonly HashSet<int> OtherStmt_set3 = NewSet((int) EOF, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.As, (int) TT.At, (int) TT.BQString, (int) TT.Catch, (int) TT.ColonColon, (int) TT.Compare, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.Else, (int) TT.EqNeq, (int) TT.Finally, (int) TT.GT, (int) TT.Id, (int) TT.In, (int) TT.IncDec, (int) TT.Is, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LEGE, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.PipeArrow, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sub, (int) TT.Substitute, (int) TT.Switch, (int) TT.Using, (int) TT.While, (int) TT.XorBits);
		static readonly HashSet<int> OtherStmt_set4 = NewSet((int) EOF, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.As, (int) TT.At, (int) TT.BQString, (int) TT.Catch, (int) TT.ColonColon, (int) TT.Compare, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.Else, (int) TT.EqNeq, (int) TT.Finally, (int) TT.GT, (int) TT.Id, (int) TT.In, (int) TT.IncDec, (int) TT.Is, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LEGE, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.PipeArrow, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sub, (int) TT.Substitute, (int) TT.Switch, (int) TT.Using, (int) TT.While, (int) TT.XorBits);

		// Statements that don't start with an Id and don't allow keyword attributes.
		LNode OtherStmt(int startIndex, LNodeList attrs, bool hasWordAttrs)
		{
			TokenType la1;
			Token lit_semi = default(Token);
			LNode result = default(LNode);
			// line 1841
			bool addAttrs = false;
			string showWordAttrErrorFor = null;
			// Line 1844: ( BracedBlock / &(TT.NotBits (TT.ContextualKeyword|TT.Id|TT.LinqKeyword|TT.This) TT.LParen TT.RParen TT.LBrace TT.RBrace) Destructor / TT.Semicolon / LabelStmt / default ExprStatement / AssemblyOrModuleAttribute / OperatorCastMethod )
			do {
				switch (LA0) {
				case TT.LBrace:
					{
						result = BracedBlock(null, null, startIndex);
						// line 1845
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
									// line 1848
									showWordAttrErrorFor = "destructor";
								}
								break;
							case TT.Add: case TT.AndBits: case TT.At: case TT.Base:
							case TT.Break: case TT.CheckedOrUnchecked: case TT.Continue: case TT.Default:
							case TT.Delegate: case TT.Dot: case TT.DotDot: case TT.Forward:
							case TT.Goto: case TT.IncDec: case TT.Is: case TT.LBrace:
							case TT.Literal: case TT.LParen: case TT.Mul: case TT.New:
							case TT.Not: case TT.NotBits: case TT.Operator: case TT.Power:
							case TT.Return: case TT.Sizeof: case TT.Sub: case TT.Substitute:
							case TT.Switch: case TT.Throw: case TT.TypeKeyword: case TT.Typeof:
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
						// line 1849
						result = F.Id(S.Missing, startIndex, lit_semi.EndIndex);
						// line 1850
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
						else if (OtherStmt_set2.Contains((int) la1))
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
						case TT.Compare: case TT.CompoundSet: case TT.DivMod: case TT.Dot:
						case TT.DotDot: case TT.EqNeq: case TT.Forward: case TT.GT:
						case TT.IncDec: case TT.LambdaArrow: case TT.LEGE: case TT.LT:
						case TT.Mul: case TT.Not: case TT.NotBits: case TT.NullCoalesce:
						case TT.NullDot: case TT.OrBits: case TT.OrXor: case TT.PipeArrow:
						case TT.Power: case TT.PtrArrow: case TT.QuestionMark: case TT.QuickBind:
						case TT.QuickBindSet: case TT.Set: case TT.Sub: case TT.Substitute:
						case TT.XorBits:
							goto matchExprStatement;
						case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.LParen:
						case TT.Operator: case TT.TypeKeyword:
							{
								result = OperatorCastMethod(startIndex, attrs);
								// line 1857
								attrs.Clear();
							}
							break;
						default:
							goto error;
						}
					}
					break;
				case TT.At: case TT.Base: case TT.CheckedOrUnchecked: case TT.Delegate:
				case TT.Dot: case TT.Is: case TT.Literal: case TT.New:
				case TT.Sizeof: case TT.This: case TT.TypeKeyword: case TT.Typeof:
					goto matchExprStatement;
				case TT.LBrack:
					{
						result = AssemblyOrModuleAttribute(startIndex, attrs);
						// line 1856
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
					// line 1852
					addAttrs = true;
				}
				break;
			matchExprStatement:
				{
					result = ExprStatement();
					// line 1854
					showWordAttrErrorFor = "expression";
					addAttrs = true;
				}
				break;
			error:
				{
					// line 1863
					result = Error("Statement expected, but got '{0}'", CurrentTokenText());
					ScanToEndOfStmt();
				}
			} while (false);
			// line 1867
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
			// Line 1878: ((EOF|TT.Catch|TT.Else|TT.Finally|TT.While) =>  | TT.Semicolon)
			switch (LA0) {
			case EOF: case TT.Catch: case TT.Else: case TT.Finally:
			case TT.While:
				{
					// line 1879
					var rr = result.Range;
					// line 1880
					result = F.Call(S.Result, result, rr.StartIndex, rr.EndIndex, rr.StartIndex, rr.StartIndex);
				}
				break;
			case TT.Semicolon:
				{
					lit_semi = MatchAny();
					// line 1881
					result = result.WithRange(result.Range.StartIndex, lit_semi.EndIndex);
				}
				break;
			default:
				{
					// line 1882
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
			// Line 1889: greedy(~(EOF|TT.LBrace|TT.Semicolon))*
			for (;;) {
				la0 = LA0;
				if (!(la0 == (TokenType) EOF || la0 == TT.LBrace || la0 == TT.Semicolon))
					Skip();
				else
					break;
			}
			// Line 1890: greedy(TT.Semicolon | TT.LBrace (TT.RBrace)?)?
			la0 = LA0;
			if (la0 == TT.Semicolon)
				Skip();
			else if (la0 == TT.LBrace) {
				Skip();
				// Line 1890: (TT.RBrace)?
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
			// line 1900
			return r;
		}

		LNode TraitDecl(int startIndex)
		{
			Check(Is(0, sy_trait), "Expected Is($LI, @@trait)");
			var t = Match((int) TT.ContextualKeyword);
			var r = RestOfSpaceDecl(startIndex, t);
			// line 1906
			return r;
		}

		private LNode RestOfSpaceDecl(int startIndex, Token kindTok)
		{
			TokenType la0;
			// line 1910
			var kind = (Symbol) kindTok.Value;
			var name = ComplexNameDecl();
			var bases = BaseListOpt();
			WhereClausesOpt(ref name);
			// Line 1914: (TT.Semicolon | BracedBlock)
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				var end = MatchAny();
				// line 1915
				return F.Call(kind, name, bases, startIndex, end.EndIndex, kindTok.StartIndex, kindTok.EndIndex);
			} else {
				var body = BracedBlock(EcsValidators.KeyNameComponentOf(name));
				// line 1917
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

		LNode UsingDirective(int startIndex, LNodeList attrs)
		{
			TokenType la0;
			Token end = default(Token);
			LNode nsName = default(LNode);
			Token static_ = default(Token);
			Token t = default(Token);
			t = Match((int) TT.Using);
			// Line 1933: (&{Is($LI, S.Static)} TT.AttrKeyword ExprStart TT.Semicolon / ExprStart (&{nsName.Calls(S.Assign, 2)} RestOfAlias / TT.Semicolon))
			do {
				la0 = LA0;
				if (la0 == TT.AttrKeyword) {
					if (Is(0, S.Static)) {
						static_ = MatchAny();
						nsName = ExprStart(true);
						end = Match((int) TT.Semicolon);
						// line 1935
						attrs.Add(F.Id(static_));
					} else
						goto matchExprStart;
				} else
					goto matchExprStart;
				break;
			matchExprStart:
				{
					nsName = ExprStart(true);
					// Line 1938: (&{nsName.Calls(S.Assign, 2)} RestOfAlias / TT.Semicolon)
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
						case TT.Colon: case TT.ContextualKeyword: case TT.LBrace:
							goto matchRestOfAlias;
						default:
							{
								// line 1944
								Error("Expected ';'");
							}
							break;
						}
						break;
					matchRestOfAlias:
						{
							Check(nsName.Calls(S.Assign, 2), "Expected nsName.Calls(S.Assign, 2)");
							// line 1939
							LNode aliasedType = nsName.Args[1, F.Missing];
							// line 1940
							nsName = nsName.Args[0, F.Missing];
							var r = RestOfAlias(startIndex, t, aliasedType, nsName);
							// line 1942
							return r.WithAttrs(attrs).PlusAttr(_filePrivate);
						}
					} while (false);
				}
			} while (false);
			// line 1947
			return F.Call(S.Import, nsName, t.StartIndex, end.EndIndex, t.StartIndex, t.EndIndex).WithAttrs(attrs);
		}

		LNode RestOfAlias(int startIndex, Token aliasTok, LNode oldName, LNode newName)
		{
			TokenType la0;
			var bases = BaseListOpt();
			WhereClausesOpt(ref newName);
			// line 1953
			var name = F.Call(S.Assign, newName, oldName, newName.Range.StartIndex, oldName.Range.EndIndex);
			// Line 1954: (TT.Semicolon | BracedBlock)
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				var end = MatchAny();
				// line 1955
				return F.Call(S.Alias, name, bases, startIndex, end.EndIndex, aliasTok.StartIndex, aliasTok.EndIndex);
			} else {
				var body = BracedBlock(EcsValidators.KeyNameComponentOf(newName));
				// line 1957
				return F.Call(S.Alias, LNode.List(name, bases, body), startIndex, body.Range.EndIndex, aliasTok.StartIndex, aliasTok.EndIndex);
			}
		}

		private LNode EnumDecl(int startIndex)
		{
			TokenType la0;
			Token lit_lcub = default(Token);
			Token lit_rcub = default(Token);
			var kw = MatchAny();
			var name = ComplexNameDecl();
			var bases = BaseListOpt();
			// Line 1966: (TT.Semicolon | TT.LBrace TT.RBrace)
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				var end = MatchAny();
				// line 1967
				return F.Call(kw, name, bases, startIndex, end.EndIndex);
			} else {
				lit_lcub = Match((int) TT.LBrace);
				lit_rcub = Match((int) TT.RBrace);
				// line 1970
				var list = ExprListInside(lit_lcub, true);
				var body = F.CallBrackets(S.Braces, lit_lcub, list, lit_rcub);
				return F.CallPrefix(kw, LNode.List(name, bases, body), lit_rcub);
			}
		}

		private LNode BaseListOpt()
		{
			TokenType la0;
			// Line 1978: (TT.Colon DataType (TT.Comma DataType)* | {..})
			la0 = LA0;
			if (la0 == TT.Colon) {
				// line 1978
				var bases = new LNodeList();
				Skip();
				bases.Add(DataType());
				// Line 1980: (TT.Comma DataType)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						Skip();
						bases.Add(DataType());
					} else
						break;
				}
				// line 1981
				return F.List(bases);
			} else
				// line 1982
				return F.List();
		}

		private void WhereClausesOpt(ref LNode name)
		{
			TokenType la0;
			// line 1988
			var list = new BMultiMap<Symbol, LNode>();
			// Line 1989: (WhereClause)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.ContextualKeyword)
					list.Add(WhereClause());
				else
					break;
			}
			// line 1990
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
					name = name.WithArgs(tparams.ToLNodeList());
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
			// line 2020
			var constraints = LNodeList.Empty;
			constraints.Add(WhereConstraint());
			// Line 2022: (TT.Comma WhereConstraint)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Comma) {
					Skip();
					constraints.Add(WhereConstraint());
				} else
					break;
			}
			// line 2023
			return new KeyValuePair<Symbol, LNode>((Symbol) T.Value, F.CallPrefixOp(S.WhereClause, where, constraints));
		}

		private LNode WhereConstraint()
		{
			TokenType la0;
			// Line 2027: ( (TT.Class|TT.Struct) | TT.New &{LT($LI).Count == 0} TT.LParen TT.RParen | DataType )
			la0 = LA0;
			if (la0 == TT.Class || la0 == TT.Struct) {
				var t = MatchAny();
				// line 2027
				return F.Id(t);
			} else if (la0 == TT.New) {
				var newkw = MatchAny();
				Check(LT(0).Count == 0, "Expected LT($LI).Count == 0");
				var lp = Match((int) TT.LParen);
				var rp = Match((int) TT.RParen);
				// line 2029
				return F.CallPrefix(newkw, ExprListInside(lp), rp);
			} else {
				var t = DataType();
				// line 2030
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
			// line 2040
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

		private LNode AssemblyOrModuleAttribute(int startIndex, LNodeList attrs)
		{
			Check(Down(0) && Up(Try_Scan_AsmOrModLabel(0)), "Expected Down($LI) && Up(Try_Scan_AsmOrModLabel(0))");
			var lb = MatchAny();
			var rb = Match((int) TT.RBrack);
			// line 2046
			Down(lb);
			var kind = AsmOrModLabel();
			// line 2048
			var list = new LNodeList();
			ExprList(ref list);
			// line 2051
			Up();
			var r = F.Call(kind.Value == sy_module ? S.Module : S.Assembly, list, startIndex, rb.EndIndex, kind.StartIndex, kind.EndIndex);
			return r.WithAttrs(attrs);
		}
		
		// ---------------------------------------------------------------------
			// methods, properties, variable/field declarations, operators ---------
			// ---------------------------------------------------------------------

		private LNode MethodOrPropertyOrVar(int startIndex, LNodeList attrs)
		{
			TokenType la0;
			LNode name = default(LNode);
			LNode result = default(LNode);
			// line 2064
			bool isExtensionMethod = false;
			bool isNamedThis;
			// Line 2065: (TT.This)?
			la0 = LA0;
			if (la0 == TT.This) {
				var t = MatchAny();
				// line 2065
				attrs.Add(F.Id(t));
				isExtensionMethod = true;
			}
			var type = DataType();
			name = MethodOrPropertyName(!isExtensionMethod, out isNamedThis);
			// Line 2069: ( &{!isNamedThis} MethodArgListAndBody | &!{name.IsLiteral} RestOfPropertyDefinition | &{!isNamedThis} &!{name.IsLiteral} VarInitializerOpt (TT.Comma ComplexNameDecl VarInitializerOpt)* TT.Semicolon )
			switch (LA0) {
			case TT.LParen:
				{
					Check(!isNamedThis, "Expected !isNamedThis");
					result = MethodArgListAndBody(startIndex, type.Range.StartIndex, attrs, S.Fn, type, name);
					// line 2071
					return result;
				}
				break;
			case TT.At: case TT.ContextualKeyword: case TT.Forward: case TT.LambdaArrow:
			case TT.LBrace: case TT.LBrack:
				{
					Check(!name.IsLiteral, "Invalid property name");
					result = RestOfPropertyDefinition(startIndex, type, name, false);
				}
				break;
			case TT.Comma: case TT.QuickBindSet: case TT.Semicolon: case TT.Set:
				{
					Check(!isNamedThis, "Expected !isNamedThis");
					Check(!name.IsLiteral, "Invalid variable name");
					// line 2078
					MaybeRecognizeVarAsKeyword(ref type);
					// line 2079
					var parts = LNode.List(type);
					// line 2080
					var isArray = IsArrayType(type);
					parts.Add(VarInitializerOpt(name, isArray));
					// Line 2082: (TT.Comma ComplexNameDecl VarInitializerOpt)*
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
					// line 2085
					var typeStart = type.Range.StartIndex;
					// line 2086
					result = F.Call(S.Var, parts, startIndex, end.EndIndex, typeStart, typeStart);
				}
				break;
			default:
				{
					// line 2087
					Error("Syntax error in what appears to be a method, property, or variable declaration");
					ScanToEndOfStmt();
					// line 2089
					result = F.Call(S.Var, type, name, type.Range.StartIndex, name.Range.EndIndex);
				}
				break;
			}
			// line 2091
			result = result.PlusAttrs(attrs);
			return result;
		}

		private LNode VarInitializerOpt(LNode name, bool isArray)
		{
			TokenType la0;
			LNode expr = default(LNode);
			// Line 2095: (VarInitializer)?
			la0 = LA0;
			if (la0 == TT.QuickBindSet || la0 == TT.Set) {
				var eq = LT0;
				expr = VarInitializer(isArray);
				// line 2097
				return F.CallInfixOp(name, S.Assign, eq, expr);
			}
			// line 2098
			return name;
		}

		private LNode VarInitializer(bool isArray)
		{
			TokenType la0;
			LNode result = default(LNode);
			Skip();
			// Line 2105: (&{isArray} &{Down($LI) && Up(HasNoSemicolons())} TT.LBrace TT.RBrace / ExprStart)
			la0 = LA0;
			if (la0 == TT.LBrace) {
				if (Down(0) && Up(HasNoSemicolons())) {
					if (isArray) {
						var lb = MatchAny();
						var rb = Match((int) TT.RBrace);
						// line 2109
						var initializers = InitializerListInside(lb);
						result = F.CallBrackets(S.ArrayInit, lb, initializers, rb, NodeStyle.Expression);
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
			// line 2117
			LNode args = F.Missing;
			// Line 2118: (TT.LBrack TT.RBrack)?
			la0 = LA0;
			if (la0 == TT.LBrack) {
				lb = MatchAny();
				rb = Match((int) TT.RBrack);
				// line 2118
				args = ArgList(lb, rb);
			}
			WhereClausesOpt(ref name);
			// line 2120
			LNode initializer;
			var body = MethodBodyOrForward(true, out initializer, isExpression);
			// line 2123
			var parts = new LNodeList { 
				type, name, args, body
			};
			if (initializer != null) {
				parts.Add(initializer);
			}
			int targetIndex = type.Range.StartIndex;
			result = F.Call(S.Property, parts, startIndex, body.Range.EndIndex, targetIndex, targetIndex);
			return result;
		}

		private LNode OperatorCastMethod(int startIndex, LNodeList attrs)
		{
			// line 2131
			LNode r;
			var op = MatchAny();
			var type = DataType();
			// line 2133
			var name = F.Attr(_triviaUseOperatorKeyword, F.Id(S.Cast, op.StartIndex, op.EndIndex));
			r = MethodArgListAndBody(startIndex, op.StartIndex, attrs, S.Fn, type, name);
			// line 2135
			return r;
		}

		private LNode MethodArgListAndBody(int startIndex, int targetIndex, LNodeList attrs, Symbol kind, LNode type, LNode name)
		{
			TokenType la0;
			Token lit_colon = default(Token);
			var lp = Match((int) TT.LParen);
			var rp = Match((int) TT.RParen);
			WhereClausesOpt(ref name);
			// line 2141
			LNode r, _, baseCall = null;
			// line 2141
			int consCallIndex = -1;
			// Line 2142: (TT.Colon (TT.Base|TT.This) TT.LParen TT.RParen)?
			la0 = LA0;
			if (la0 == TT.Colon) {
				lit_colon = MatchAny();
				var target = Match((int) TT.Base, (int) TT.This);
				var baselp = Match((int) TT.LParen);
				var baserp = Match((int) TT.RParen);
				// line 2144
				baseCall = F.CallPrefix(target, ExprListInside(baselp), baserp);
				if ((kind != S.Constructor)) {
					Error(baseCall, "This is not a constructor declaration, so there should be no ':' clause.");
				}
				consCallIndex = lit_colon.StartIndex;
			}
			// line 2152
			for (int i = 0; i < attrs.Count; i++) {
				var attr = attrs[i];
				if (IsNamedArg(attr) && attr.Args[0].IsIdNamed(S.Return)) {
					type = type.PlusAttr(attr.Args[1]);
					attrs.RemoveAt(i);
					i--;
				}
			}
			// Line 2161: (default TT.Semicolon | MethodBodyOrForward)
			do {
				switch (LA0) {
				case TT.Semicolon:
					goto match1;
				case TT.At: case TT.Forward: case TT.LambdaArrow: case TT.LBrace:
					{
						var body = MethodBodyOrForward(false, out _, false, consCallIndex);
						// line 2175
						if (kind == S.Delegate) {
							Error("A 'delegate' is not expected to have a method body.");
						}
						if (baseCall != null) {
							if ((!body.Calls(S.Braces))) {
								body = F.Braces(LNode.List(body), startIndex, body.Range.EndIndex);
							}
							body = body.WithArgs(body.Args.Insert(0, baseCall));
						}
						var parts = new LNodeList { 
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
					// line 2163
					if (kind == S.Constructor && baseCall != null) {
						Error(baseCall, "A method body is required.");
						var parts = LNode.List(type, name, ArgList(lp, rp), LNode.Call(S.Braces, new LNodeList(baseCall), baseCall.Range));
						r = F.Call(kind, parts, startIndex, baseCall.Range.EndIndex, targetIndex, targetIndex);
					} else {
						var parts = LNode.List(type, name, ArgList(lp, rp));
						r = F.Call(kind, parts, startIndex, end.EndIndex, targetIndex, targetIndex);
					}
				}
			} while (false);
			// line 2186
			return r.PlusAttrs(attrs);
		}

		private LNode MethodBodyOrForward(bool isProperty, out LNode propInitializer, bool isExpression = false, int bodyStartIndex = -1)
		{
			TokenType la0;
			// line 2191
			propInitializer = null;
			// Line 2192: ( TT.Forward ExprStart SemicolonIf | TT.LambdaArrow ExprStart SemicolonIf | TokenLiteral (&{!isExpression} TT.Semicolon)? | BracedBlock greedy(&{isProperty} TT.Set ExprStart SemicolonIf)? )
			la0 = LA0;
			if (la0 == TT.Forward) {
				var op = MatchAny();
				var e = ExprStart(true);
				SemicolonIf(!isExpression);
				// line 2192
				return F.CallPrefixOp(op, e);
			} else if (la0 == TT.LambdaArrow) {
				var op = MatchAny();
				var e = ExprStart(false);
				SemicolonIf(!isExpression);
				// line 2193
				return e;
			} else if (la0 == TT.At) {
				var e = TokenLiteral();
				// Line 2194: (&{!isExpression} TT.Semicolon)?
				la0 = LA0;
				if (la0 == TT.Semicolon) {
					Check(!isExpression, "Expected !isExpression");
					Skip();
				}
				// line 2194
				return e;
			} else {
				var body = BracedBlock(S.Fn, null, bodyStartIndex);
				// Line 2198: greedy(&{isProperty} TT.Set ExprStart SemicolonIf)?
				la0 = LA0;
				if (la0 == TT.Set) {
					Check(isProperty, "Expected isProperty");
					Skip();
					propInitializer = ExprStart(false);
					SemicolonIf(!isExpression);
				}
				// line 2201
				return body;
			}
		}

		private void SemicolonIf(bool isStatement)
		{
			TokenType la0;
			// Line 2206: (&{isStatement} TT.Semicolon / {..})
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				if (isStatement)
					Skip();
				else// line 2207
				if (isStatement) {
					Error(0, "Expected ';' to end statement");
				}
			} else// line 2207
			if (isStatement) {
				Error(0, "Expected ';' to end statement");
			}
		}

		private 
		void NoSemicolons()
		{
			TokenType la0;
			// Line 2228: (~(EOF|TT.Semicolon))*
			for (;;) {
				la0 = LA0;
				if (!(la0 == (TokenType) EOF || la0 == TT.Semicolon))
					Skip();
				else
					break;
			}
			Match((int) EOF);
		}
		
		// ---------------------------------------------------------------------
			// Constructor/destructor ----------------------------------------------
			// ---------------------------------------------------------------------

		bool Try_HasNoSemicolons(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return HasNoSemicolons();
		}
		bool HasNoSemicolons()
		{
			TokenType la0;
			// Line 2228: (~(EOF|TT.Semicolon))*
			for (;;) {
				la0 = LA0;
				if (!(la0 == (TokenType) EOF || la0 == TT.Semicolon))
					Skip();
				else
					break;
			}
			if (!TryMatch((int) EOF))
				return false;
			return true;
		}

		private LNode Constructor(int startIndex, LNodeList attrs)
		{
			TokenType la0;
			// line 2235
			LNode r;
			Token n;
			// Line 2236: ( &{_spaceName == LT($LI).Value} (TT.ContextualKeyword|TT.Id|TT.LinqKeyword) &(TT.LParen TT.RParen (TT.LBrace|TT.Semicolon)) / &{_spaceName != S.Fn || LA($LI + 3) == TT.LBrace} TT.This &(TT.LParen TT.RParen (TT.LBrace|TT.Semicolon)) / (TT.ContextualKeyword|TT.Id|TT.LinqKeyword|TT.This) &(TT.LParen TT.RParen TT.Colon) )
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
			// line 2245
			LNode name = F.Id((Symbol) n.Value, n.StartIndex, n.EndIndex);
			r = MethodArgListAndBody(startIndex, n.StartIndex, attrs, S.Constructor, F.Missing, name);
			// line 2247
			return r;
		}

		private LNode Destructor(int startIndex, LNodeList attrs)
		{
			LNode result = default(LNode);
			var tilde = MatchAny();
			var n = MatchAny();
			// line 2253
			var name = (Symbol) n.Value;
			if (name != _spaceName) {
				Error("Unexpected destructor '{0}'", name);
			}
			LNode name2 = F.CallPrefixOp(tilde, F.Id(n));
			result = MethodArgListAndBody(startIndex, tilde.StartIndex, attrs, S.Fn, F.Missing, name2);
			return result;
		}
		
		// ---------------------------------------------------------------------
			// Delegate & event declarations ---------------------------------------
			// ---------------------------------------------------------------------

		private LNode DelegateDecl(int startIndex, LNodeList attrs)
		{
			var d = MatchAny();
			var type = DataType();
			var name = ComplexNameDecl();
			var r = MethodArgListAndBody(startIndex, d.StartIndex, attrs, S.Delegate, type, name);
			// line 2269
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
			// Line 2275: (TT.Comma ComplexNameDecl (TT.Comma ComplexNameDecl)*)?
			la0 = LA0;
			if (la0 == TT.Comma) {
				// line 2275
				var parts = new LNodeList(name);
				Skip();
				parts.Add(ComplexNameDecl());
				// Line 2276: (TT.Comma ComplexNameDecl)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						Skip();
						parts.Add(ComplexNameDecl());
					} else
						break;
				}
				// line 2277
				name = F.List(parts, name.Range.StartIndex, parts.Last.Range.EndIndex);
			}
			// Line 2279: (TT.Semicolon | BracedBlock)
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				lit_semi = MatchAny();
				// line 2280
				result = F.Call(eventkw, type, name, startIndex, lit_semi.EndIndex);
			} else {
				var body = BracedBlock(S.Fn);
				// line 2282
				if (name.Calls(S.AltList)) {
					Error("A body is not allowed when defining multiple events.");
				}
				// line 2283
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
			// line 2295
			return F.Call(S.Label, F.Id(id), startIndex, end.EndIndex, id.StartIndex, id.StartIndex);
		}

		LNode CaseStmt(int startIndex)
		{
			TokenType la0;
			// line 2299
			var cases = LNodeList.Empty;
			var kw = Match((int) TT.Case);
			cases.Add(ExprStartNNP(true));
			// Line 2301: (TT.Comma ExprStartNNP)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Comma) {
					Skip();
					cases.Add(ExprStartNNP(true));
				} else
					break;
			}
			var end = Match((int) TT.Colon);
			// line 2302
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
			// line 2318
			var args = new LNodeList();
			// line 2319
			LNode block;
			// Line 2320: ( TT.LParen TT.RParen (BracedBlock | TT.Id => Stmt) | TT.Forward ExprStart TT.Semicolon | BracedBlock )
			la0 = LA0;
			if (la0 == TT.LParen) {
				var lp = MatchAny();
				var rp = Match((int) TT.RParen);
				// line 2320
				args = AppendExprsInside(lp, args, false, true);
				// Line 2321: (BracedBlock | TT.Id => Stmt)
				la0 = LA0;
				if (la0 == TT.LBrace)
					block = BracedBlock();
				else {
					block = Stmt();
					// line 2324
					ErrorSink.Write(Severity.Error, block, ColumnOf(block.Range.StartIndex) <= ColumnOf(id.StartIndex) ? "Probable missing semicolon before this statement." : "Probable missing braces around body of '{0}' statement.", id.Value);
				}
			} else if (la0 == TT.Forward) {
				var fwd = MatchAny();
				var e = ExprStart(true);
				Match((int) TT.Semicolon);
				// line 2331
				block = F.CallPrefixOp(fwd, e);
			} else
				block = BracedBlock();
			// line 2335
			args.Add(block);
			var result = F.Call((Symbol) id.Value, args, startIndex, block.Range.EndIndex, id.StartIndex, id.EndIndex, NodeStyle.Special);
			if (block.Calls(S.Forward, 1)) {
				result = F.Attr(_triviaForwardedProperty, result);
			}
			return result;
		}
		static readonly HashSet<int> ReturnBreakContinueThrow_set0 = NewSet((int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.AttrKeyword, (int) TT.Base, (int) TT.Break, (int) TT.CheckedOrUnchecked, (int) TT.ContextualKeyword, (int) TT.Continue, (int) TT.Default, (int) TT.Delegate, (int) TT.Dot, (int) TT.DotDot, (int) TT.Forward, (int) TT.Goto, (int) TT.Id, (int) TT.IncDec, (int) TT.Is, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LinqKeyword, (int) TT.Literal, (int) TT.LParen, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.Operator, (int) TT.Power, (int) TT.Return, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.Switch, (int) TT.This, (int) TT.Throw, (int) TT.TypeKeyword, (int) TT.Typeof);

		private LNode ReturnBreakContinueThrow(int startIndex)
		{
			TokenType la0;
			LNode e = default(LNode);
			var kw = MatchAny();
			// Line 2355: greedy(ExprStartNNP)?
			la0 = LA0;
			if (ReturnBreakContinueThrow_set0.Contains((int) la0))
				e = ExprStartNNP(false);
			// line 2357
			if (e != null)
				return F.Call((Symbol) kw.Value, e, startIndex, e.Range.EndIndex, kw.StartIndex, kw.EndIndex);
			else
				return F.Call((Symbol) kw.Value, startIndex, kw.EndIndex, kw.StartIndex, kw.EndIndex);
		}
		
		// goto, goto case -----------------------------------------------------
			// (these can be expressions as well as statements)

		private LNode GotoStmt(int startIndex)
		{
			TokenType la0;
			var kw = MatchAny();
			// Line 2367: (TT.Default / ExprOrNull)
			la0 = LA0;
			if (la0 == TT.Default) {
				var def = MatchAny();
				// line 2368
				return F.Call(kw, F.Id(def), startIndex, kw.EndIndex);
			} else {
				var e = ExprOrNull(false);
				// line 2372
				if (e != null)
					return F.Call(kw, e, startIndex, e.Range.EndIndex);
				else
					return F.Call(kw, startIndex, kw.EndIndex);
			}
		}

		private LNode GotoCaseStmt(int startIndex)
		{
			TokenType la0;
			// line 2379
			LNode e = null;
			var kw = MatchAny();
			var kw2 = MatchAny();
			// Line 2381: (TT.Default / ExprStartNNP)
			la0 = LA0;
			if (la0 == TT.Default) {
				var def = MatchAny();
				// line 2382
				e = F.Id(S.Default, def.StartIndex, def.EndIndex);
			} else
				e = ExprStartNNP(false);
			// line 2384
			var endIndex = e != null ? e.Range.EndIndex : kw2.EndIndex;
			// line 2385
			return F.Call(S.GotoCase, e, startIndex, endIndex, kw.StartIndex, kw2.EndIndex);
		}
		
		// checked & unchecked -------------------------------------------------

		private LNode CheckedOrUncheckedStmt(int startIndex)
		{
			var kw = MatchAny();
			var bb = BracedBlock();
			// line 2393
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
			// line 2401
			var parts = new LNodeList(block);
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
			// line 2410
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
			// line 2419
			Down(lit_lpar);
			// line 2420
			var init = LNodeList.Empty;
			var inc = init;
			ExprList(ref init, false, true);
			Match((int) TT.Semicolon);
			var cond = ExprOpt(false);
			Match((int) TT.Semicolon);
			ExprList(ref inc, false, false);
			// line 2422
			Up();
			// line 2424
			var initL = F.Call(S.AltList, init);
			var incL = F.Call(S.AltList, inc);
			var parts = new LNodeList { 
				initL, cond, incL, block
			};
			return F.Call(kw, parts, startIndex, block.Range.EndIndex);
		}

		private LNode ForEachStmt(int startIndex)
		{
			TokenType la1;
			LNode var = default(LNode);
			var kw = MatchAny();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			// line 2434
			Down(p);
			// Line 2435: (&(VarIn) VarIn)?
			switch (LA0) {
			case TT.ContextualKeyword: case TT.Id: case TT.LinqKeyword: case TT.LParen:
			case TT.Operator: case TT.Substitute: case TT.TypeKeyword:
				{
					if (Try_Scan_VarIn(0)) {
						if (Down(0) && Up(Scan_TupleTypeList() && LA0 == EOF)) {
							la1 = LA(1);
							if (Var_In_Expr_set0.Contains((int) la1))
								var = VarIn(out Token _);
						} else {
							la1 = LA(1);
							if (Var_In_Expr_set1.Contains((int) la1))
								var = VarIn(out Token _);
						}
					}
				}
				break;
			}
			var expr = ExprStart(false);
			Match((int) EOF, (int) TT.RParen);
			// line 2439
			var parts = LNode.List(var ?? F.Missing, expr, block);
			return Up(F.Call(kw, parts, startIndex, block.Range.EndIndex));
		}

		// The "T id in" part of "foreach (T id in e)" or "from int x in ..." (type is optional)
		private LNode VarIn(out Token inTok)
		{
			LNode result = default(LNode);
			var pair = VarDeclStart();
			// line 2449
			var start = pair.A.Range.StartIndex;
			// line 2450
			result = F.Call(S.Var, pair.A, pair.B, start, pair.B.Range.EndIndex, start, start);
			inTok = Match((int) TT.In);
			return result;
		}
		
		// if-else -------------------------------------------------------------

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

		private LNode IfStmt(int startIndex)
		{
			TokenType la0;
			// line 2457
			LNode @else = null;
			var kw = MatchAny();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var then = Stmt();
			// Line 2459: greedy(TT.Else Stmt)?
			la0 = LA0;
			if (la0 == TT.Else) {
				Skip();
				@else = Stmt();
			}
			// line 2461
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
			// line 2470
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
			// line 2480
			var expr = SingleExprInside(p, "using (...)");
			return F.Call(S.UsingStmt, expr, block, startIndex, block.Range.EndIndex, kw.StartIndex, kw.EndIndex);
		}

		private LNode LockStmt(int startIndex)
		{
			var kw = MatchAny();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			// line 2488
			var expr = SingleExprInside(p, "lock (...)");
			return F.Call(kw, expr, block, startIndex, block.Range.EndIndex);
		}

		private LNode FixedStmt(int startIndex)
		{
			var kw = MatchAny();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			// line 2496
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
			// line 2505
			var parts = new LNodeList { 
				header
			};
			// line 2506
			LNode varExpr;
			LNode whenExpr;
			// Line 2508: greedy(TT.Catch (TT.LParen TT.RParen / {..}) (&{Is($LI, @@when)} TT.ContextualKeyword TT.LParen TT.RParen / {..}) Stmt)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Catch) {
					var kw = MatchAny();
					// Line 2509: (TT.LParen TT.RParen / {..})
					la0 = LA0;
					if (la0 == TT.LParen) {
						la1 = LA(1);
						if (la1 == TT.RParen) {
							var p = MatchAny();
							Skip();
							// line 2509
							varExpr = SingleExprInside(p, "catch (...)", true);
						} else
							// line 2510
							varExpr = MissingHere();
					} else
						// line 2510
						varExpr = MissingHere();
					// Line 2511: (&{Is($LI, @@when)} TT.ContextualKeyword TT.LParen TT.RParen / {..})
					la0 = LA0;
					if (la0 == TT.ContextualKeyword) {
						if (Is(0, sy_when)) {
							la1 = LA(1);
							if (la1 == TT.LParen) {
								Skip();
								var c = MatchAny();
								Match((int) TT.RParen);
								// line 2512
								whenExpr = SingleExprInside(c, "when (...)");
							} else
								// line 2513
								whenExpr = MissingHere();
						} else
							// line 2513
							whenExpr = MissingHere();
					} else
						// line 2513
						whenExpr = MissingHere();
					handler = Stmt();
					// line 2515
					parts.Add(F.Call(kw, LNode.List(varExpr, whenExpr, handler), kw.StartIndex, handler.Range.EndIndex));
				} else
					break;
			}
			// Line 2518: greedy(TT.Finally Stmt)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Finally) {
					var kw = MatchAny();
					handler = Stmt();
					// line 2519
					parts.Add(F.Call(kw, handler, kw.StartIndex, handler.Range.EndIndex));
				} else
					break;
			}
			// line 2522
			var result = F.Call(trykw, parts, startIndex, parts.Last.Range.EndIndex);
			if (parts.Count == 1) {
				Error(result, "'try': At least one 'catch' or 'finally' clause is required");
			}
			return result;
		}
		
		// C# interactive directives -------------------------------------------

		private LNode PPNullaryDirective(int startIndex)
		{
			var t = MatchAny();
			// line 2534
			return F.Call(t, startIndex, t.EndIndex);
		}

		private LNode PPStringDirective(int startIndex)
		{
			var t = MatchAny();
			// line 2540
			var target = S.PPNullable;
			if ((t.Type() == TT.CSIload)) {
				target = S.CsiLoad;
			} else if ((t.Type() == TT.CSIreference)) {
				target = S.CsiReference;
			}
			var value = t.Value.ToString().WithoutPrefix(" ");
			return F.Trivia(target, value, startIndex, t.EndIndex);
		}
		
		// ---------------------------------------------------------------------
			// ExprList and StmtList -----------------------------------------------
			// ---------------------------------------------------------------------

		LNode ExprOrNull(bool allowUnassignedVarDecl = false)
		{
			TokenType la0;
			LNode result = default(LNode);
			// Line 2557: greedy(ExprStart)?
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
		static readonly HashSet<int> ExprList_set0 = NewSet((int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.AttrKeyword, (int) TT.Base, (int) TT.Break, (int) TT.CheckedOrUnchecked, (int) TT.Comma, (int) TT.ContextualKeyword, (int) TT.Continue, (int) TT.Default, (int) TT.Delegate, (int) TT.Dot, (int) TT.DotDot, (int) TT.Forward, (int) TT.Goto, (int) TT.Id, (int) TT.IncDec, (int) TT.Is, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LinqKeyword, (int) TT.Literal, (int) TT.LParen, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.Operator, (int) TT.Power, (int) TT.Return, (int) TT.Semicolon, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.Switch, (int) TT.This, (int) TT.Throw, (int) TT.TypeKeyword, (int) TT.Typeof);

		void ExprList(ref LNodeList list, bool allowTrailingComma = false, bool allowUnassignedVarDecl = false)
		{
			TokenType la0, la1;
			// Line 2570: nongreedy(ExprOpt (TT.Comma &{allowTrailingComma} EOF / TT.Comma ExprOpt)*)?
			la0 = LA0;
			if (la0 == EOF || la0 == TT.Semicolon)
				;
			else {
				list.Add(ExprOpt(allowUnassignedVarDecl));
				// Line 2571: (TT.Comma &{allowTrailingComma} EOF / TT.Comma ExprOpt)*
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
						// line 2573
						Error("'{0}': Syntax error in expression list", CurrentTokenText());
						MatchExcept((int) TT.Comma);
						// Line 2574: (~(EOF|TT.Comma))*
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

		void ArgList(ref LNodeList list)
		{
			TokenType la0;
			// Line 2582: nongreedy(ExprOpt (TT.Comma ExprOpt)*)?
			la0 = LA0;
			if (la0 == EOF)
				;
			else {
				list.Add(ExprOpt(true));
				// Line 2583: (TT.Comma ExprOpt)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						Skip();
						list.Add(ExprOpt(true));
					} else if (la0 == EOF)
						break;
					else {
						// line 2584
						Error("Syntax error in argument list");
						// Line 2584: (~(EOF|TT.Comma))*
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
			// Line 2591: ( TT.LBrace TT.RBrace / TT.LBrack TT.RBrack TT.Set ExprStart / ExprOpt )
			la0 = LA0;
			if (la0 == TT.LBrace) {
				la2 = LA(2);
				if (la2 == (TokenType) EOF || la2 == TT.Comma) {
					var lb = MatchAny();
					var rb = Match((int) TT.RBrace);
					// line 2593
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
					// line 2597
					result = F.Call(S.DictionaryInitAssign, ExprListInside(lb).Add(e), lb.StartIndex, e.Range.EndIndex, eq.StartIndex, eq.EndIndex);
				} else
					result = ExprOpt(false);
			} else
				result = ExprOpt(false);
			return result;
		}
		static readonly HashSet<int> InitializerList_set0 = NewSet((int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.AttrKeyword, (int) TT.Base, (int) TT.Break, (int) TT.CheckedOrUnchecked, (int) TT.Comma, (int) TT.ContextualKeyword, (int) TT.Continue, (int) TT.Default, (int) TT.Delegate, (int) TT.Dot, (int) TT.DotDot, (int) TT.Forward, (int) TT.Goto, (int) TT.Id, (int) TT.IncDec, (int) TT.Is, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LinqKeyword, (int) TT.Literal, (int) TT.LParen, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.Operator, (int) TT.Power, (int) TT.Return, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.Switch, (int) TT.This, (int) TT.Throw, (int) TT.TypeKeyword, (int) TT.Typeof);

		// Used for new int[][] { ... } or int[][] x = { ... }
		void InitializerList(ref LNodeList list)
		{
			TokenType la0, la1;
			// Line 2605: nongreedy(InitializerExpr (TT.Comma EOF / TT.Comma InitializerExpr)*)?
			la0 = LA0;
			if (la0 == EOF)
				;
			else {
				list.Add(InitializerExpr());
				// Line 2606: (TT.Comma EOF / TT.Comma InitializerExpr)*
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
						// line 2608
						Error("Syntax error in initializer list");
						// Line 2608: (~(EOF|TT.Comma))*
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

		void StmtList(ref LNodeList list)
		{
			TokenType la0;
			// Line 2613: (~(EOF) => Stmt)*
			for (;;) {
				la0 = LA0;
				if (la0 != (TokenType) EOF)
					list.Add(Stmt());
				else
					break;
			}
			Skip();
		}

		private bool Try_TypeSuffixOpt_Test0(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return TypeSuffixOpt_Test0();
		}
		private bool TypeSuffixOpt_Test0()
		{
			// Line 337: ((TT.Add|TT.AndBits|TT.At|TT.ContextualKeyword|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.New|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | LinqKeywordAsId)
			switch (LA0) {
			case TT.Add: case TT.AndBits: case TT.At: case TT.ContextualKeyword:
			case TT.Forward: case TT.Id: case TT.IncDec: case TT.LBrace:
			case TT.Literal: case TT.LParen: case TT.Mul: case TT.New:
			case TT.Not: case TT.NotBits: case TT.Sub: case TT.Substitute:
			case TT.TypeKeyword:
				Skip();
				break;
			default:
				if (!Scan_LinqKeywordAsId())
					return false;
				break;
			}
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

		private bool Try_PrefixExpr_Test0(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return PrefixExpr_Test0();
		}
		private bool PrefixExpr_Test0()
		{
			// Line 665: ( (TT.Add|TT.AndBits|TT.BQString|TT.Dot|TT.Mul|TT.Sub|TT.Switch) | TT.IncDec TT.LParen | &{_insideLinqExpr} TT.LinqKeyword )
			switch (LA0) {
			case TT.Add: case TT.AndBits: case TT.BQString: case TT.Dot:
			case TT.Mul: case TT.Sub: case TT.Switch:
				Skip();
				break;
			case TT.IncDec:
				{
					Skip();
					if (!TryMatch((int) TT.LParen))
						return false;
				}
				break;
			default:
				{
					if (!_insideLinqExpr)
						return false;
					if (!TryMatch((int) TT.LinqKeyword))
						return false;
				}
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
			// Line 2317: ( TT.LParen TT.RParen (TT.LBrace TT.RBrace | TT.Id) | TT.LBrace TT.RBrace | TT.Forward )
			la0 = LA0;
			if (la0 == TT.LParen) {
				Skip();
				if (!TryMatch((int) TT.RParen))
					return false;
				// Line 2317: (TT.LBrace TT.RBrace | TT.Id)
				la0 = LA0;
				if (la0 == TT.LBrace) {
					Skip();
					if (!TryMatch((int) TT.RBrace))
						return false;
				} else if (!TryMatch((int) TT.Id))
					return false;
			} else if (la0 == TT.LBrace) {
				Skip();
				if (!TryMatch((int) TT.RBrace))
					return false;
			} else if (!TryMatch((int) TT.Forward))
				return false;
			return true;
		}
	}
}