// Generated from EcsParserGrammar.les by LeMP custom tool. LeMP version: 1.7.6.0
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
	
	#pragma warning disable 162, 642
	
	partial class EcsParser
	{
		static readonly Symbol _trait = GSymbol.Get("trait");
		static readonly Symbol _alias = GSymbol.Get("alias");
		static readonly Symbol _where = GSymbol.Get("where");
		static readonly Symbol _when = GSymbol.Get("when");
		static readonly Symbol _assembly = GSymbol.Get("assembly");
		static readonly Symbol _module = GSymbol.Get("module");
		static readonly Symbol _from = GSymbol.Get("from");
		static readonly Symbol _await = GSymbol.Get("await");
		Symbol _spaceName;
		bool _insideLinqExpr;
		bool Is(int li, Symbol value)
		{
			var lt = LT(li);
			return lt.Value == value && SourceFile.Text.TryGet(lt.StartIndex, '\0') != '@';
		}
		internal static readonly HashSet<object> LinqKeywords = EcsLexer.LinqKeywords;
		public LNode DataType(bool afterAsOrIs = false)
		{
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
		int count;
		LNode ComplexNameDecl()
		{
			bool _;
			return ComplexNameDecl(false, out _);
		}
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
		LNode TentativeExpr(VList<LNode> attrs, out TentativeResult result, bool allowUnassigned = false)
		{
			result = new TentativeResult(InputPosition);
			var oldState = _tentative;
			_tentative = new TentativeState(true);
			{
				try {
					bool failed = false;
					result.Result = Expr(StartExpr).PlusAttrs(attrs);
					if ((LA0 != EOF && LA0 != TT.Semicolon && LA0 != TT.Comma)) {
						failed = true;
					}
					result.Errors = _tentative.DeferredErrors;
					result.InputPosition = InputPosition;
					if (failed || _tentative.LocalErrorCount != 0) {
						InputPosition = result.OldPosition;
						return null;
					}
				} finally {
					_tentative = oldState;
				}
			}
			return Apply(result);
		}
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
						InputPosition = result.OldPosition;
						return null;
					}
				} finally {
					_tentative = oldState;
				}
			}
			return Apply(result);
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
			SourceRange rng;
			Symbol name = type.Name;
			if ((name == _var) && type.IsId && (rng = type.Range).Source.Text.TryGet(rng.StartIndex, '\0') != '@') {
				type = type.WithName(S.Missing);
			}
		}
		bool IsNamedArg(LNode node)
		{
			return node.Calls(S.NamedArg, 2) && node.BaseStyle == NodeStyle.Operator;
		}
		WList<LNode> _stmtAttrs = new WList<LNode>();
		enum StmtCat
		{
			MethodOrPropOrVar = 0, KeywordStmt = 1, IdStmt = 2, ThisConstructor = 3, OtherStmt = 4
		}
		enum DetectionMode
		{
			Expr = 0, Stmt = 1
		}
		static readonly HashSet<TT> ExpectedAfterTypeAndName_InStmt = new HashSet<TT> { 
			TT.Set, TT.LBrace, TT.LParen, TT.LBrack, TT.LambdaArrow, TT.Forward, TT.Semicolon, TT.Comma, TT.At
		};
		static readonly HashSet<TT> ExpectedAfterTypeAndName_InExpr = new HashSet<TT> { 
			TT.Set, TT.LBrace, TT.LambdaArrow, TT.Forward, TT.Semicolon, TT.Comma, TT.EOF, TT.At
		};
		static readonly HashSet<TT>[] ExpectedAfterTypeAndName = new[] { 
			ExpectedAfterTypeAndName_InExpr, ExpectedAfterTypeAndName_InStmt
		};
		static readonly HashSet<TT> EasilyDetectedKeywordStatements = new HashSet<TT> { 
			TT.Namespace, TT.Class, TT.Struct, TT.Interface, TT.Enum, TT.Event, TT.Switch, TT.Case, TT.Using, TT.While, TT.Fixed, TT.For, TT.Foreach, TT.Goto, TT.Lock, TT.Return, TT.Try, TT.Do, TT.Continue, TT.Break, TT.Throw, TT.If
		};
		StmtCat DetectStatementCategoryAndAddWordAttributes(out int wordAttrCount, ref VList<LNode> attrs, DetectionMode mode)
		{
			var oldPosition = InputPosition;
			var cat = DetectStatementCategory(out wordAttrCount, mode);
			if ((wordAttrCount != 0)) {
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
			}
			return cat;
		}
		StmtCat DetectStatementCategory(out int wordAttrCount, DetectionMode mode)
		{
			wordAttrCount = 0;
			int wordsStartAt = InputPosition;
			bool haveNew = LA0 == TT.New;
			if ((haveNew || LA0 == TT.Id || LA0 == TT.ContextualKeyword)) {
				if ((!haveNew)) {
				}
				bool isAttrKw = haveNew;
				do {
					Skip();
					if ((isAttrKw)) {
						wordsStartAt = InputPosition;
					} else {
						wordAttrCount++;
					}
					haveNew |= (isAttrKw = (LA0 == TT.New));
				} while (((isAttrKw |= LA0 == TT.AttrKeyword || LA0 == TT.New) || LA0 == TT.Id || LA0 == TT.ContextualKeyword));
			}
			int consecutive = InputPosition - wordsStartAt;
			if ((LA0 == TT.TypeKeyword)) {
				InputPosition++;
				consecutive++;
			} else if ((LA0 == TT.Substitute)) {
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
					InputPosition--;
					return StmtCat.MethodOrPropOrVar;
				}
			} else if ((LT0.Kind == TokenKind.OtherKeyword)) {
				if ((EasilyDetectedKeywordStatements.Contains(LA0) || LA0 == TT.Delegate && LA(1) != TT.LParen || (LA0 == TT.Checked || LA0 == TT.Unchecked) && LA(1) == TT.LBrace)) {
					if (!(consecutive > 0 && (LA0 == TT.If || LA0 == TT.Using))) {
						return StmtCat.KeywordStmt;
					}
				}
			} else if ((consecutive == 0)) {
				if ((!haveNew)) {
					return StmtCat.OtherStmt;
				}
			}
			if (consecutive >= 2) {
				int likelyStart = wordsStartAt + consecutive - 2;
				if ((ExpectedAfterTypeAndName[(int) mode].Contains(LA0))) {
					InputPosition = likelyStart;
				} else {
					InputPosition = likelyStart + 1;
					if ((Scan_ComplexNameDecl() && ExpectedAfterTypeAndName[(int) mode].Contains(LA0))) {
						InputPosition = likelyStart;
					} else {
						InputPosition = likelyStart + 1;
					}
				}
				return StmtCat.MethodOrPropOrVar;
			}
			InputPosition = wordsStartAt;
			using (new SavePosition(this, 0)) {
				TryMatch((int) TT.This);
				if ((Scan_DataType(false) && Scan_ComplexNameDecl() && ExpectedAfterTypeAndName[(int) mode].Contains(LA0))) {
					return StmtCat.MethodOrPropOrVar;
				}
			}
			if ((haveNew)) {
				if ((LA(-1) == TT.New)) {
					InputPosition--;
				} else {
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
		private LNode MethodBodyOrForward()
		{
			LNode _;
			return MethodBodyOrForward(false, out _);
		}
		bool IsArrayType(LNode type)
		{
			return type.Calls(S.Of, 2) && S.IsArrayKeyword(type.Args[0].Name);
		}
		LNode ArgList(Token lp, Token rp)
		{
			var list = new VList<LNode>();
			if ((Down(lp.Children))) {
				ArgList(ref list);
				Up();
			}
			return F.List(list, lp.StartIndex, rp.EndIndex);
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
		Token IdNotLinqKeyword()
		{
			Check(!(_insideLinqExpr && LinqKeywords.Contains(LT(0).Value)), "!(_insideLinqExpr && LinqKeywords.Contains(LT($LI).Value))");
			var t = Match((int) TT.ContextualKeyword);
			// line 115
			return t;
		}
		bool Scan_IdNotLinqKeyword()
		{
			if (_insideLinqExpr && LinqKeywords.Contains(LT(0).Value))
				return false;
			if (!TryMatch((int) TT.ContextualKeyword))
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
		bool Try_Scan_DataType(int lookaheadAmt, bool afterAsOrIs = false)
		{
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
		LNode ComplexId()
		{
			TokenType la0, la1;
			var e = IdAtom();
			// Line 160: (TT.ColonColon IdAtom)?
			do {
				la0 = LA0;
				if (la0 == TT.ColonColon) {
					switch (LA(1)) {
					case TT.Id:
					case TT.Operator:
					case TT.Substitute:
					case TT.TypeKeyword:
						goto match1;
					case TT.ContextualKeyword:
						{
							if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
								goto match1;
						}
						break;
					}
				}
				break;
			match1:
				{
					Skip();
					var e2 = IdAtom();
					// line 161
					e = F.Call(S.ColonColon, e, e2, e.Range.StartIndex, e2.Range.EndIndex);
				}
			} while (false);
			// Line 163: (TParams)?
			la0 = LA0;
			if (la0 == TT.LT) {
				switch (LA(1)) {
				case TT.Id:
				case TT.Operator:
				case TT.Substitute:
				case TT.TypeKeyword:
					TParams(ref e);
					break;
				case TT.ContextualKeyword:
					{
						if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
							TParams(ref e);
					}
					break;
				case TT.GT:
					TParams(ref e);
					break;
				}
			} else if (la0 == TT.Dot) {
				la1 = LA(1);
				if (la1 == TT.LBrack)
					TParams(ref e);
			} else if (la0 == TT.Not) {
				la1 = LA(1);
				if (la1 == TT.LParen)
					TParams(ref e);
			}
			// Line 164: (TT.Dot IdAtom (TParams)?)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Dot) {
					switch (LA(1)) {
					case TT.Id:
					case TT.Operator:
					case TT.Substitute:
					case TT.TypeKeyword:
						goto match1_a;
					case TT.ContextualKeyword:
						{
							if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
								goto match1_a;
							else
								goto stop;
						}
					default:
						goto stop;
					}
				} else
					goto stop;
			match1_a:
				{
					Skip();
					var rhs = IdAtom();
					// line 164
					e = F.Dot(e, rhs);
					// Line 165: (TParams)?
					la0 = LA0;
					if (la0 == TT.LT) {
						switch (LA(1)) {
						case TT.Id:
						case TT.Operator:
						case TT.Substitute:
						case TT.TypeKeyword:
							TParams(ref e);
							break;
						case TT.ContextualKeyword:
							{
								if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
									TParams(ref e);
							}
							break;
						case TT.GT:
							TParams(ref e);
							break;
						}
					} else if (la0 == TT.Dot) {
						la1 = LA(1);
						if (la1 == TT.LBrack)
							TParams(ref e);
					} else if (la0 == TT.Not) {
						la1 = LA(1);
						if (la1 == TT.LParen)
							TParams(ref e);
					}
				}
			}
		stop:;
			// line 167
			return e;
		}
		bool Scan_ComplexId()
		{
			TokenType la0, la1;
			if (!Scan_IdAtom())
				return false;
			// Line 160: (TT.ColonColon IdAtom)?
			do {
				la0 = LA0;
				if (la0 == TT.ColonColon) {
					switch (LA(1)) {
					case TT.Id:
					case TT.Operator:
					case TT.Substitute:
					case TT.TypeKeyword:
						goto match1;
					case TT.ContextualKeyword:
						{
							if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
								goto match1;
						}
						break;
					}
				}
				break;
			match1:
				{
					if (!TryMatch((int) TT.ColonColon))
						return false;
					if (!Scan_IdAtom())
						return false;
				}
			} while (false);
			// Line 163: (TParams)?
			do {
				la0 = LA0;
				if (la0 == TT.LT) {
					switch (LA(1)) {
					case TT.Id:
					case TT.Operator:
					case TT.Substitute:
					case TT.TypeKeyword:
						goto matchTParams;
					case TT.ContextualKeyword:
						{
							if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
								goto matchTParams;
						}
						break;
					case TT.GT:
						goto matchTParams;
					}
				} else if (la0 == TT.Dot) {
					la1 = LA(1);
					if (la1 == TT.LBrack)
						goto matchTParams;
				} else if (la0 == TT.Not) {
					la1 = LA(1);
					if (la1 == TT.LParen)
						goto matchTParams;
				}
				break;
			matchTParams:
				{
					if (!Scan_TParams())
						return false;
				}
			} while (false);
			// Line 164: (TT.Dot IdAtom (TParams)?)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Dot) {
					switch (LA(1)) {
					case TT.Id:
					case TT.Operator:
					case TT.Substitute:
					case TT.TypeKeyword:
						goto match1_a;
					case TT.ContextualKeyword:
						{
							if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
								goto match1_a;
							else
								goto stop;
						}
					default:
						goto stop;
					}
				} else
					goto stop;
			match1_a:
				{
					if (!TryMatch((int) TT.Dot))
						return false;
					if (!Scan_IdAtom())
						return false;
					// Line 165: (TParams)?
					do {
						la0 = LA0;
						if (la0 == TT.LT) {
							switch (LA(1)) {
							case TT.Id:
							case TT.Operator:
							case TT.Substitute:
							case TT.TypeKeyword:
								goto matchTParams_a;
							case TT.ContextualKeyword:
								{
									if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
										goto matchTParams_a;
								}
								break;
							case TT.GT:
								goto matchTParams_a;
							}
						} else if (la0 == TT.Dot) {
							la1 = LA(1);
							if (la1 == TT.LBrack)
								goto matchTParams_a;
						} else if (la0 == TT.Not) {
							la1 = LA(1);
							if (la1 == TT.LParen)
								goto matchTParams_a;
						}
						break;
					matchTParams_a:
						{
							if (!Scan_TParams())
								return false;
						}
					} while (false);
				}
			}
		stop:;
			return true;
		}
		LNode IdAtom()
		{
			// line 172
			LNode r;
			// Line 173: ( TT.Substitute Atom | TT.Operator AnyOperator | (TT.Id|TT.TypeKeyword) | IdNotLinqKeyword )
			switch (LA0) {
			case TT.Substitute:
				{
					var t = MatchAny();
					var e = Atom();
					e = AutoRemoveParens(e);
					r = F.Call(S.Substitute, e, t.StartIndex, e.Range.EndIndex);
				}
				break;
			case TT.Operator:
				{
					var op = MatchAny();
					var t = AnyOperator();
					// line 176
					r = F.Attr(_triviaUseOperatorKeyword, F.Id((Symbol) t.Value, op.StartIndex, t.EndIndex));
				}
				break;
			case TT.Id:
			case TT.TypeKeyword:
				{
					var t = MatchAny();
					// line 178
					r = F.Id(t);
				}
				break;
			default:
				{
					var t = IdNotLinqKeyword();
					// line 180
					r = F.Id(t);
				}
				break;
			}
			// line 181
			return r;
		}
		bool Scan_IdAtom()
		{
			// Line 173: ( TT.Substitute Atom | TT.Operator AnyOperator | (TT.Id|TT.TypeKeyword) | IdNotLinqKeyword )
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
			case TT.Id:
			case TT.TypeKeyword:
				if (!TryMatch((int) TT.Id, (int) TT.TypeKeyword))
					return false;
				break;
			default:
				if (!Scan_IdNotLinqKeyword())
					return false;
				break;
			}
			return true;
		}
		void TParams(ref LNode r)
		{
			TokenType la0;
			VList<LNode> list = new VList<LNode>(r);
			Token end;
			// Line 198: ( TT.LT (DataType (TT.Comma DataType)*)? TT.GT | TT.Dot TT.LBrack TT.RBrack | TT.Not TT.LParen TT.RParen )
			la0 = LA0;
			if (la0 == TT.LT) {
				Skip();
				// Line 198: (DataType (TT.Comma DataType)*)?
				switch (LA0) {
				case TT.ContextualKeyword:
				case TT.Id:
				case TT.Operator:
				case TT.Substitute:
				case TT.TypeKeyword:
					{
						list.Add(DataType());
						// Line 198: (TT.Comma DataType)*
						for (;;) {
							la0 = LA0;
							if (la0 == TT.Comma) {
								Skip();
								list.Add(DataType());
							} else
								break;
						}
					}
					break;
				}
				end = Match((int) TT.GT);
			} else if (la0 == TT.Dot) {
				Skip();
				var t = Match((int) TT.LBrack);
				end = Match((int) TT.RBrack);
				// line 199
				list = AppendExprsInside(t, list);
			} else {
				Match((int) TT.Not);
				var t = Match((int) TT.LParen);
				end = Match((int) TT.RParen);
				// line 200
				list = AppendExprsInside(t, list);
			}
			// line 203
			int start = r.Range.StartIndex;
			r = F.Call(S.Of, list, start, end.EndIndex);
		}
		bool Try_Scan_TParams(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_TParams();
		}
		bool Scan_TParams()
		{
			TokenType la0;
			// Line 198: ( TT.LT (DataType (TT.Comma DataType)*)? TT.GT | TT.Dot TT.LBrack TT.RBrack | TT.Not TT.LParen TT.RParen )
			la0 = LA0;
			if (la0 == TT.LT) {
				if (!TryMatch((int) TT.LT))
					return false;
				// Line 198: (DataType (TT.Comma DataType)*)?
				switch (LA0) {
				case TT.ContextualKeyword:
				case TT.Id:
				case TT.Operator:
				case TT.Substitute:
				case TT.TypeKeyword:
					{
						if (!Scan_DataType())
							return false;
						// Line 198: (TT.Comma DataType)*
						for (;;) {
							la0 = LA0;
							if (la0 == TT.Comma) {
								if (!TryMatch((int) TT.Comma))
									return false;
								if (!Scan_DataType())
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
				if (!TryMatch((int) TT.Not))
					return false;
				if (!TryMatch((int) TT.LParen))
					return false;
				if (!TryMatch((int) TT.RParen))
					return false;
			}
			return true;
		}
		bool TypeSuffixOpt(bool afterAsOrIs, out Token? dimensionBrack, ref LNode e)
		{
			TokenType la0, la1;
			// line 213
			int count;
			bool result = false;
			dimensionBrack = null;
			// Line 246: greedy( TT.QuestionMark (&!{afterAsOrIs} | &!(((TT.Add|TT.AndBits|TT.At|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.New|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | IdNotLinqKeyword))) | TT.Mul | &{(count = CountDims(LT($LI), @true)) > 0} TT.LBrack TT.RBrack greedy(&{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack)* )*
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
					// line 252
					e = F.Of(F.Id(t), e, e.Range.StartIndex, t.EndIndex);
					result = true;
				} else if (la0 == TT.LBrack) {
					if ((count = CountDims(LT(0), true)) > 0) {
						la1 = LA(1);
						if (la1 == TT.RBrack) {
							var dims = InternalList<Pair<int,int>>.Empty;
							Token rb;
							var lb = MatchAny();
							rb = MatchAny();
							// line 257
							dims.Add(Pair.Create(count, rb.EndIndex));
							// Line 258: greedy(&{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack)*
							for (;;) {
								la0 = LA0;
								if (la0 == TT.LBrack) {
									if ((count = CountDims(LT(0), false)) > 0) {
										la1 = LA(1);
										if (la1 == TT.RBrack) {
											Skip();
											rb = MatchAny();
											// line 258
											dims.Add(Pair.Create(count, rb.EndIndex));
										} else
											break;
									} else
										break;
								} else
									break;
							}
							// line 260
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
					// Line 246: (&!{afterAsOrIs} | &!(((TT.Add|TT.AndBits|TT.At|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.New|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | IdNotLinqKeyword)))
					if (!afterAsOrIs) {
					} else
						Check(!Try_TypeSuffixOpt_Test0(0), "!(((TT.Add|TT.AndBits|TT.At|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.New|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | IdNotLinqKeyword))");
					// line 249
					e = F.Of(F.Id(t), e, e.Range.StartIndex, t.EndIndex);
					result = true;
				}
			}
			// line 269
			return result;
		}
		bool Try_Scan_TypeSuffixOpt(int lookaheadAmt, bool afterAsOrIs)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_TypeSuffixOpt(afterAsOrIs);
		}
		bool Scan_TypeSuffixOpt(bool afterAsOrIs)
		{
			TokenType la0, la1;
			// Line 246: greedy( TT.QuestionMark (&!{afterAsOrIs} | &!(((TT.Add|TT.AndBits|TT.At|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.New|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | IdNotLinqKeyword))) | TT.Mul | &{(count = CountDims(LT($LI), @true)) > 0} TT.LBrack TT.RBrack greedy(&{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack)* )*
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
				} else if (la0 == TT.Mul)
					{if (!TryMatch((int) TT.Mul))
						return false;}
				else if (la0 == TT.LBrack) {
					if ((count = CountDims(LT(0), true)) > 0) {
						la1 = LA(1);
						if (la1 == TT.RBrack) {
							if (!TryMatch((int) TT.LBrack))
								return false;
							if (!TryMatch((int) TT.RBrack))
								return false;
							// Line 258: greedy(&{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack)*
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
					// Line 246: (&!{afterAsOrIs} | &!(((TT.Add|TT.AndBits|TT.At|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.New|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | IdNotLinqKeyword)))
					if (!afterAsOrIs) {
					} else if (Try_TypeSuffixOpt_Test0(0))
						return false;
				}
			}
			return true;
		}
		LNode ComplexNameDecl(bool thisAllowed, out bool hasThis)
		{
			TokenType la0, la1;
			LNode e = default(LNode);
			LNode got_ComplexThisDecl = default(LNode);
			// Line 279: (ComplexThisDecl | IdAtom (TT.ColonColon IdAtom)? (TParamsDecl)? (TT.Dot IdAtom (TParamsDecl)?)* (TT.Dot ComplexThisDecl)?)
			la0 = LA0;
			if (la0 == TT.This) {
				e = ComplexThisDecl(thisAllowed);
				// line 279
				hasThis = true;
			} else {
				e = IdAtom();
				// line 280
				hasThis = false;
				// Line 282: (TT.ColonColon IdAtom)?
				do {
					la0 = LA0;
					if (la0 == TT.ColonColon) {
						switch (LA(1)) {
						case TT.Id:
						case TT.Operator:
						case TT.Substitute:
						case TT.TypeKeyword:
							goto match1;
						case TT.ContextualKeyword:
							{
								if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
									goto match1;
							}
							break;
						}
					}
					break;
				match1:
					{
						Skip();
						var e2 = IdAtom();
						// line 283
						e = F.Call(S.ColonColon, e, e2, e.Range.StartIndex, e2.Range.EndIndex);
					}
				} while (false);
				// Line 285: (TParamsDecl)?
				la0 = LA0;
				if (la0 == TT.LT) {
					switch (LA(1)) {
					case TT.Id:
					case TT.Operator:
					case TT.Substitute:
					case TT.TypeKeyword:
						TParamsDecl(ref e);
						break;
					case TT.ContextualKeyword:
						{
							if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
								TParamsDecl(ref e);
						}
						break;
					case TT.LBrack:
						{
							if (!(Down(1) && Up(Try_Scan_AsmOrModLabel(0))))
								TParamsDecl(ref e);
						}
						break;
					case TT.AttrKeyword:
					case TT.GT:
					case TT.In:
						TParamsDecl(ref e);
						break;
					}
				} else if (la0 == TT.Dot) {
					la1 = LA(1);
					if (la1 == TT.LBrack)
						TParamsDecl(ref e);
				} else if (la0 == TT.Not) {
					la1 = LA(1);
					if (la1 == TT.LParen)
						TParamsDecl(ref e);
				}
				// Line 286: (TT.Dot IdAtom (TParamsDecl)?)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Dot) {
						switch (LA(1)) {
						case TT.Id:
						case TT.Operator:
						case TT.Substitute:
						case TT.TypeKeyword:
							goto match1_a;
						case TT.ContextualKeyword:
							{
								if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
									goto match1_a;
								else
									goto stop;
							}
						default:
							goto stop;
						}
					} else
						goto stop;
				match1_a:
					{
						Skip();
						var rhs = IdAtom();
						// line 287
						e = F.Dot(e, rhs);
						// Line 288: (TParamsDecl)?
						la0 = LA0;
						if (la0 == TT.LT) {
							switch (LA(1)) {
							case TT.Id:
							case TT.Operator:
							case TT.Substitute:
							case TT.TypeKeyword:
								TParamsDecl(ref e);
								break;
							case TT.ContextualKeyword:
								{
									if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
										TParamsDecl(ref e);
								}
								break;
							case TT.LBrack:
								{
									if (!(Down(1) && Up(Try_Scan_AsmOrModLabel(0))))
										TParamsDecl(ref e);
								}
								break;
							case TT.AttrKeyword:
							case TT.GT:
							case TT.In:
								TParamsDecl(ref e);
								break;
							}
						} else if (la0 == TT.Dot) {
							la1 = LA(1);
							if (la1 == TT.LBrack)
								TParamsDecl(ref e);
						} else if (la0 == TT.Not) {
							la1 = LA(1);
							if (la1 == TT.LParen)
								TParamsDecl(ref e);
						}
					}
				}
			stop:;
				// Line 290: (TT.Dot ComplexThisDecl)?
				la0 = LA0;
				if (la0 == TT.Dot) {
					la1 = LA(1);
					if (la1 == TT.This) {
						Skip();
						got_ComplexThisDecl = ComplexThisDecl(thisAllowed);
						hasThis = true;
						e = F.Dot(e, got_ComplexThisDecl);
					}
				}
			}
			// line 293
			return e;
		}
		bool Try_Scan_ComplexNameDecl(int lookaheadAmt, bool thisAllowed = false)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_ComplexNameDecl(thisAllowed);
		}
		bool Scan_ComplexNameDecl(bool thisAllowed = false)
		{
			TokenType la0, la1;
			// Line 279: (ComplexThisDecl | IdAtom (TT.ColonColon IdAtom)? (TParamsDecl)? (TT.Dot IdAtom (TParamsDecl)?)* (TT.Dot ComplexThisDecl)?)
			la0 = LA0;
			if (la0 == TT.This)
				{if (!Scan_ComplexThisDecl(thisAllowed))
					return false;}
			else {
				if (!Scan_IdAtom())
					return false;
				// Line 282: (TT.ColonColon IdAtom)?
				do {
					la0 = LA0;
					if (la0 == TT.ColonColon) {
						switch (LA(1)) {
						case TT.Id:
						case TT.Operator:
						case TT.Substitute:
						case TT.TypeKeyword:
							goto match1;
						case TT.ContextualKeyword:
							{
								if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
									goto match1;
							}
							break;
						}
					}
					break;
				match1:
					{
						if (!TryMatch((int) TT.ColonColon))
							return false;
						if (!Scan_IdAtom())
							return false;
					}
				} while (false);
				// Line 285: (TParamsDecl)?
				do {
					la0 = LA0;
					if (la0 == TT.LT) {
						switch (LA(1)) {
						case TT.Id:
						case TT.Operator:
						case TT.Substitute:
						case TT.TypeKeyword:
							goto matchTParamsDecl;
						case TT.ContextualKeyword:
							{
								if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
									goto matchTParamsDecl;
							}
							break;
						case TT.LBrack:
							{
								if (!(Down(1) && Up(Try_Scan_AsmOrModLabel(0))))
									goto matchTParamsDecl;
							}
							break;
						case TT.AttrKeyword:
						case TT.GT:
						case TT.In:
							goto matchTParamsDecl;
						}
					} else if (la0 == TT.Dot) {
						la1 = LA(1);
						if (la1 == TT.LBrack)
							goto matchTParamsDecl;
					} else if (la0 == TT.Not) {
						la1 = LA(1);
						if (la1 == TT.LParen)
							goto matchTParamsDecl;
					}
					break;
				matchTParamsDecl:
					{
						if (!Scan_TParamsDecl())
							return false;
					}
				} while (false);
				// Line 286: (TT.Dot IdAtom (TParamsDecl)?)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Dot) {
						switch (LA(1)) {
						case TT.Id:
						case TT.Operator:
						case TT.Substitute:
						case TT.TypeKeyword:
							goto match1_a;
						case TT.ContextualKeyword:
							{
								if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
									goto match1_a;
								else
									goto stop;
							}
						default:
							goto stop;
						}
					} else
						goto stop;
				match1_a:
					{
						if (!TryMatch((int) TT.Dot))
							return false;
						if (!Scan_IdAtom())
							return false;
						// Line 288: (TParamsDecl)?
						do {
							la0 = LA0;
							if (la0 == TT.LT) {
								switch (LA(1)) {
								case TT.Id:
								case TT.Operator:
								case TT.Substitute:
								case TT.TypeKeyword:
									goto matchTParamsDecl_a;
								case TT.ContextualKeyword:
									{
										if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
											goto matchTParamsDecl_a;
									}
									break;
								case TT.LBrack:
									{
										if (!(Down(1) && Up(Try_Scan_AsmOrModLabel(0))))
											goto matchTParamsDecl_a;
									}
									break;
								case TT.AttrKeyword:
								case TT.GT:
								case TT.In:
									goto matchTParamsDecl_a;
								}
							} else if (la0 == TT.Dot) {
								la1 = LA(1);
								if (la1 == TT.LBrack)
									goto matchTParamsDecl_a;
							} else if (la0 == TT.Not) {
								la1 = LA(1);
								if (la1 == TT.LParen)
									goto matchTParamsDecl_a;
							}
							break;
						matchTParamsDecl_a:
							{
								if (!Scan_TParamsDecl())
									return false;
							}
						} while (false);
					}
				}
			stop:;
				// Line 290: (TT.Dot ComplexThisDecl)?
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
		LNode ComplexThisDecl(bool allowed)
		{
			TokenType la0;
			LNode result = default(LNode);
			// line 297
			if ((!allowed)) {
				Error("'this' is not allowed in this location.");
			}
			var t = Match((int) TT.This);
			// line 298
			result = F.Id(t);
			// Line 299: (TParamsDecl)?
			la0 = LA0;
			if (la0 == TT.Dot || la0 == TT.LT || la0 == TT.Not) {
				switch (LA(1)) {
				case TT.AttrKeyword:
				case TT.ContextualKeyword:
				case TT.GT:
				case TT.Id:
				case TT.In:
				case TT.LBrack:
				case TT.LParen:
				case TT.Operator:
				case TT.Substitute:
				case TT.TypeKeyword:
					TParamsDecl(ref result);
					break;
				}
			}
			return result;
		}
		bool Scan_ComplexThisDecl(bool allowed)
		{
			TokenType la0;
			if (!TryMatch((int) TT.This))
				return false;
			// Line 299: (TParamsDecl)?
			la0 = LA0;
			if (la0 == TT.Dot || la0 == TT.LT || la0 == TT.Not) {
				switch (LA(1)) {
				case TT.AttrKeyword:
				case TT.ContextualKeyword:
				case TT.GT:
				case TT.Id:
				case TT.In:
				case TT.LBrack:
				case TT.LParen:
				case TT.Operator:
				case TT.Substitute:
				case TT.TypeKeyword:
					if (!Scan_TParamsDecl())
						return false;
					break;
				}
			}
			return true;
		}
		void TParamsDecl(ref LNode r)
		{
			TokenType la0;
			VList<LNode> list = new VList<LNode>(r);
			Token end;
			// Line 307: ( TT.LT (TParamDecl (TT.Comma TParamDecl)*)? TT.GT | TT.Dot TT.LBrack TT.RBrack | TT.Not TT.LParen TT.RParen )
			la0 = LA0;
			if (la0 == TT.LT) {
				Skip();
				// Line 307: (TParamDecl (TT.Comma TParamDecl)*)?
				switch (LA0) {
				case TT.AttrKeyword:
				case TT.ContextualKeyword:
				case TT.Id:
				case TT.In:
				case TT.LBrack:
				case TT.Operator:
				case TT.Substitute:
				case TT.TypeKeyword:
					{
						list.Add(TParamDecl());
						// Line 307: (TT.Comma TParamDecl)*
						for (;;) {
							la0 = LA0;
							if (la0 == TT.Comma) {
								Skip();
								list.Add(TParamDecl());
							} else
								break;
						}
					}
					break;
				}
				end = Match((int) TT.GT);
			} else if (la0 == TT.Dot) {
				Skip();
				var t = Match((int) TT.LBrack);
				end = Match((int) TT.RBrack);
				// line 308
				list = AppendExprsInside(t, list);
			} else {
				Match((int) TT.Not);
				var t = Match((int) TT.LParen);
				end = Match((int) TT.RParen);
				// line 309
				list = AppendExprsInside(t, list);
			}
			// line 312
			int start = r.Range.StartIndex;
			r = F.Call(S.Of, list, start, end.EndIndex);
		}
		bool Try_Scan_TParamsDecl(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_TParamsDecl();
		}
		bool Scan_TParamsDecl()
		{
			TokenType la0;
			// Line 307: ( TT.LT (TParamDecl (TT.Comma TParamDecl)*)? TT.GT | TT.Dot TT.LBrack TT.RBrack | TT.Not TT.LParen TT.RParen )
			la0 = LA0;
			if (la0 == TT.LT) {
				if (!TryMatch((int) TT.LT))
					return false;
				// Line 307: (TParamDecl (TT.Comma TParamDecl)*)?
				switch (LA0) {
				case TT.AttrKeyword:
				case TT.ContextualKeyword:
				case TT.Id:
				case TT.In:
				case TT.LBrack:
				case TT.Operator:
				case TT.Substitute:
				case TT.TypeKeyword:
					{
						if (!Scan_TParamDecl())
							return false;
						// Line 307: (TT.Comma TParamDecl)*
						for (;;) {
							la0 = LA0;
							if (la0 == TT.Comma) {
								if (!TryMatch((int) TT.Comma))
									return false;
								if (!Scan_TParamDecl())
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
				if (!TryMatch((int) TT.Not))
					return false;
				if (!TryMatch((int) TT.LParen))
					return false;
				if (!TryMatch((int) TT.RParen))
					return false;
			}
			return true;
		}
		LNode TParamDecl()
		{
			LNode result = default(LNode);
			VList<LNode> attrs = default(VList<LNode>);
			int startIndex = GetTextPosition(InputPosition);
			// Line 319: (ComplexId / NormalAttributes TParamAttributeKeywords IdAtom)
			switch (LA0) {
			case TT.ContextualKeyword:
			case TT.Id:
			case TT.Operator:
			case TT.Substitute:
			case TT.TypeKeyword:
				result = ComplexId();
				break;
			default:
				{
					NormalAttributes(ref attrs);
					TParamAttributeKeywords(ref attrs);
					result = IdAtom();
				}
				break;
			}
			result = result.WithAttrs(attrs);
			return result;
		}
		bool Scan_TParamDecl()
		{
			// Line 319: (ComplexId / NormalAttributes TParamAttributeKeywords IdAtom)
			switch (LA0) {
			case TT.ContextualKeyword:
			case TT.Id:
			case TT.Operator:
			case TT.Substitute:
			case TT.TypeKeyword:
				if (!Scan_ComplexId())
					return false;
				break;
			default:
				{
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
		LNode Atom()
		{
			TokenType la0, la1;
			// line 397
			LNode r;
			// Line 398: ( (TT.Dot|TT.Substitute) Atom | TT.Operator AnyOperator | (TT.Base|TT.Id|TT.This|TT.TypeKeyword) | IdNotLinqKeyword | TT.Literal | ExprInParensAuto | NewExpr | BracedBlock | TokenLiteral | (TT.Checked|TT.Unchecked) TT.LParen TT.RParen | (TT.Default|TT.Sizeof|TT.Typeof) TT.LParen TT.RParen | TT.Delegate TT.LParen TT.RParen TT.LBrace TT.RBrace | TT.Is DataType )
			switch (LA0) {
			case TT.Dot:
			case TT.Substitute:
				{
					var t = MatchAny();
					var e = Atom();
					e = AutoRemoveParens(e);
					r = F.Call((Symbol) t.Value, e, t.StartIndex, e.Range.EndIndex);
				}
				break;
			case TT.Operator:
				{
					var op = MatchAny();
					var t = AnyOperator();
					// line 401
					r = F.Attr(_triviaUseOperatorKeyword, F.Id((Symbol) t.Value, op.StartIndex, t.EndIndex));
				}
				break;
			case TT.Base:
			case TT.Id:
			case TT.This:
			case TT.TypeKeyword:
				{
					var t = MatchAny();
					// line 403
					r = F.Id(t);
				}
				break;
			case TT.ContextualKeyword:
				{
					var t = IdNotLinqKeyword();
					// line 405
					r = F.Id(t);
				}
				break;
			case TT.Literal:
				{
					var t = MatchAny();
					// line 407
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
			case TT.Checked:
			case TT.Unchecked:
				{
					var t = MatchAny();
					var args = Match((int) TT.LParen);
					var rp = Match((int) TT.RParen);
					// line 414
					r = F.Call((Symbol) t.Value, ExprListInside(args), t.StartIndex, rp.EndIndex);
				}
				break;
			case TT.Default:
			case TT.Sizeof:
			case TT.Typeof:
				{
					var t = MatchAny();
					var args = Match((int) TT.LParen);
					var rp = Match((int) TT.RParen);
					// line 417
					r = F.Call((Symbol) t.Value, TypeInside(args), t.StartIndex, rp.EndIndex);
				}
				break;
			case TT.Delegate:
				{
					var t = MatchAny();
					var args = Match((int) TT.LParen);
					Match((int) TT.RParen);
					var block = Match((int) TT.LBrace);
					var rb = Match((int) TT.RBrace);
					// line 419
					r = F.Call(S.Lambda, F.List(ExprListInside(args, false, true)), F.Braces(StmtListInside(block), block.StartIndex, rb.EndIndex), t.StartIndex, rb.EndIndex);
				}
				break;
			case TT.Is:
				{
					var t = MatchAny();
					var dt = DataType();
					// line 421
					r = F.Call(S.Is, dt, t.StartIndex, dt.Range.EndIndex);
				}
				break;
			default:
				{
					// Line 423: greedy(~(EOF|TT.Comma|TT.Semicolon))*
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
					// line 424
					r = Error("'{0}': Invalid expression. Expected (parentheses), {{braces}}, identifier, literal, or $substitution.", CurrentTokenText());
				}
				break;
			}
			// line 426
			return r;
		}
		bool Scan_Atom()
		{
			TokenType la0, la1;
			// Line 398: ( (TT.Dot|TT.Substitute) Atom | TT.Operator AnyOperator | (TT.Base|TT.Id|TT.This|TT.TypeKeyword) | IdNotLinqKeyword | TT.Literal | ExprInParensAuto | NewExpr | BracedBlock | TokenLiteral | (TT.Checked|TT.Unchecked) TT.LParen TT.RParen | (TT.Default|TT.Sizeof|TT.Typeof) TT.LParen TT.RParen | TT.Delegate TT.LParen TT.RParen TT.LBrace TT.RBrace | TT.Is DataType )
			switch (LA0) {
			case TT.Dot:
			case TT.Substitute:
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
			case TT.Base:
			case TT.Id:
			case TT.This:
			case TT.TypeKeyword:
				if (!TryMatch((int) TT.Base, (int) TT.Id, (int) TT.This, (int) TT.TypeKeyword))
					return false;
				break;
			case TT.ContextualKeyword:
				if (!Scan_IdNotLinqKeyword())
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
			case TT.Checked:
			case TT.Unchecked:
				{
					if (!TryMatch((int) TT.Checked, (int) TT.Unchecked))
						return false;
					if (!TryMatch((int) TT.LParen))
						return false;
					if (!TryMatch((int) TT.RParen))
						return false;
				}
				break;
			case TT.Default:
			case TT.Sizeof:
			case TT.Typeof:
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
					// Line 423: greedy(~(EOF|TT.Comma|TT.Semicolon))*
					for (;;) {
						la0 = LA0;
						if (!(la0 == (TokenType) EOF || la0 == TT.Comma || la0 == TT.Semicolon)) {
							la1 = LA(1);
							if (la1 != (TokenType) EOF)
								{if (!TryMatchExcept((int) TT.Comma, (int) TT.Semicolon))
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
			var op = Match(AnyOperator_set0);
			// line 436
			return op;
		}
		bool Scan_AnyOperator()
		{
			if (!TryMatch(AnyOperator_set0))
				return false;
			return true;
		}
		LNode NewExpr()
		{
			TokenType la0, la1;
			// line 441
			Token? majorDimension = null;
			int endIndex;
			var list = VList<LNode>.Empty;
			var op = Match((int) TT.New);
			// Line 447: ( &{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack TT.LBrace TT.RBrace | TT.LBrace TT.RBrace | DataType (TT.LParen TT.RParen (TT.LBrace TT.RBrace)? / (TT.LBrace TT.RBrace)?) )
			la0 = LA0;
			if (la0 == TT.LBrack) {
				Check((count = CountDims(LT(0), false)) > 0, "(count = CountDims(LT($LI), @false)) > 0");
				var lb = MatchAny();
				var rb = Match((int) TT.RBrack);
				// line 449
				var type = F.Id(S.GetArrayKeyword(count), lb.StartIndex, rb.EndIndex);
				lb = Match((int) TT.LBrace);
				rb = Match((int) TT.RBrace);
				// line 452
				list.Add(LNode.Call(type, type.Range));
				AppendInitializersInside(lb, ref list);
				endIndex = rb.EndIndex;
			} else if (la0 == TT.LBrace) {
				var lb = MatchAny();
				var rb = Match((int) TT.RBrace);
				// line 459
				list.Add(F.Missing);
				AppendInitializersInside(lb, ref list);
				endIndex = rb.EndIndex;
			} else {
				var type = DataType(false, out majorDimension);
				// Line 471: (TT.LParen TT.RParen (TT.LBrace TT.RBrace)? / (TT.LBrace TT.RBrace)?)
				do {
					la0 = LA0;
					if (la0 == TT.LParen) {
						la1 = LA(1);
						if (la1 == TT.RParen) {
							var lp = MatchAny();
							var rp = MatchAny();
							// line 473
							if ((majorDimension != null)) {
								Error("Syntax error: unexpected constructor argument list (...)");
							}
							list.Add(F.Call(type, ExprListInside(lp), type.Range.StartIndex, rp.EndIndex));
							endIndex = rp.EndIndex;
							// Line 479: (TT.LBrace TT.RBrace)?
							la0 = LA0;
							if (la0 == TT.LBrace) {
								la1 = LA(1);
								if (la1 == TT.RBrace) {
									var lb = MatchAny();
									var rb = MatchAny();
									// line 481
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
						// line 488
						Token lb = op, rb = op;
						bool haveBraces = false;
						// Line 489: (TT.LBrace TT.RBrace)?
						la0 = LA0;
						if (la0 == TT.LBrace) {
							la1 = LA(1);
							if (la1 == TT.RBrace) {
								lb = MatchAny();
								rb = MatchAny();
								// line 489
								haveBraces = true;
							}
						}
						// line 491
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
			// line 512
			return F.Call(S.New, list, op.StartIndex, endIndex);
		}
		bool Scan_NewExpr()
		{
			TokenType la0, la1;
			if (!TryMatch((int) TT.New))
				return false;
			// Line 447: ( &{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack TT.LBrace TT.RBrace | TT.LBrace TT.RBrace | DataType (TT.LParen TT.RParen (TT.LBrace TT.RBrace)? / (TT.LBrace TT.RBrace)?) )
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
				// Line 471: (TT.LParen TT.RParen (TT.LBrace TT.RBrace)? / (TT.LBrace TT.RBrace)?)
				do {
					la0 = LA0;
					if (la0 == TT.LParen) {
						la1 = LA(1);
						if (la1 == TT.RParen) {
							if (!TryMatch((int) TT.LParen))
								return false;
							if (!TryMatch((int) TT.RParen))
								return false;
							// Line 479: (TT.LBrace TT.RBrace)?
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
						// Line 489: (TT.LBrace TT.RBrace)?
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
		LNode ExprInParensAuto()
		{
			// Line 526: (&(ExprInParens (TT.LambdaArrow|TT.Set)) ExprInParens / ExprInParens)
			if (Try_ExprInParensAuto_Test0(0)) {
				var r = ExprInParens(true);
				// line 527
				return r;
			} else {
				var r = ExprInParens(false);
				// line 528
				return r;
			}
		}
		bool Scan_ExprInParensAuto()
		{
			// Line 526: (&(ExprInParens (TT.LambdaArrow|TT.Set)) ExprInParens / ExprInParens)
			if (Try_ExprInParensAuto_Test0(0))
				{if (!Scan_ExprInParens(true))
					return false;}
			else if (!Scan_ExprInParens(false))
				return false;
			return true;
		}
		LNode TokenLiteral()
		{
			TokenType la0;
			Token at = default(Token);
			Token L = default(Token);
			Token R = default(Token);
			at = Match((int) TT.At);
			// Line 533: (TT.LBrack TT.RBrack | TT.LBrace TT.RBrace)
			la0 = LA0;
			if (la0 == TT.LBrack) {
				L = MatchAny();
				R = Match((int) TT.RBrack);
			} else {
				L = Match((int) TT.LBrace);
				R = Match((int) TT.RBrace);
			}
			// line 534
			return F.Literal(L.Children, at.StartIndex, R.EndIndex);
		}
		bool Scan_TokenLiteral()
		{
			TokenType la0;
			if (!TryMatch((int) TT.At))
				return false;
			// Line 533: (TT.LBrack TT.RBrack | TT.LBrace TT.RBrace)
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
		LNode PrimaryExpr()
		{
			TokenType la0;
			var e = Atom();
			FinishPrimaryExpr(ref e);
			// Line 541: (TT.NullDot PrimaryExpr)?
			la0 = LA0;
			if (la0 == TT.NullDot) {
				Skip();
				var rhs = PrimaryExpr();
				// line 541
				e = F.Call(S.NullDot, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex);
			}
			// line 543
			return e;
		}
		void FinishPrimaryExpr(ref LNode e)
		{
			TokenType la1;
			// Line 548: greedy( (TT.ColonColon|TT.Dot|TT.PtrArrow|TT.QuickBind) Atom / PrimaryExpr_NewStyleCast / TT.LParen TT.RParen | TT.LBrack TT.RBrack | TT.QuestionMark TT.LBrack TT.RBrack | TT.IncDec | &(TParams ~(TT.ContextualKeyword|TT.Id)) ((TT.LT|TT.Not) | TT.Dot TT.LBrack) => TParams | BracedBlockOrTokenLiteral )*
			for (;;) {
				switch (LA0) {
				case TT.Dot:
					{
						if (Try_FinishPrimaryExpr_Test0(0)) {
							switch (LA(1)) {
							case TT.At:
							case TT.Base:
							case TT.Checked:
							case TT.ContextualKeyword:
							case TT.Default:
							case TT.Delegate:
							case TT.Dot:
							case TT.Id:
							case TT.Is:
							case TT.LBrace:
							case TT.Literal:
							case TT.LParen:
							case TT.New:
							case TT.Operator:
							case TT.Sizeof:
							case TT.Substitute:
							case TT.This:
							case TT.TypeKeyword:
							case TT.Typeof:
							case TT.Unchecked:
								goto match1;
							default:
								TParams(ref e);
								break;
							}
						} else
							goto match1;
					}
					break;
				case TT.ColonColon:
				case TT.PtrArrow:
				case TT.QuickBind:
					goto match1;
				case TT.LParen:
					{
						if (Down(0) && Up(LA0 == TT.As || LA0 == TT.Using || LA0 == TT.PtrArrow))
							e = PrimaryExpr_NewStyleCast(e);
						else {
							var lp = MatchAny();
							var rp = Match((int) TT.RParen);
							// line 552
							e = F.Call(e, ExprListInside(lp), e.Range.StartIndex, rp.EndIndex);
						}
					}
					break;
				case TT.LBrack:
					{
						var lb = MatchAny();
						var rb = Match((int) TT.RBrack);
						var list = new VList<LNode> { 
							e
						};
						e = F.Call(S.IndexBracks, AppendExprsInside(lb, list), e.Range.StartIndex, rb.EndIndex);
					}
					break;
				case TT.QuestionMark:
					{
						la1 = LA(1);
						if (la1 == TT.LBrack) {
							var t = MatchAny();
							var lb = MatchAny();
							var rb = Match((int) TT.RBrack);
							// line 570
							e = F.Call(S.NullIndexBracks, e, F.List(ExprListInside(lb)), e.Range.StartIndex, rb.EndIndex);
						} else
							goto stop;
					}
					break;
				case TT.IncDec:
					{
						var t = MatchAny();
						// line 572
						e = F.Call(t.Value == S.PreInc ? S.PostInc : S.PostDec, e, e.Range.StartIndex, t.EndIndex);
					}
					break;
				case TT.LT:
					{
						if (Try_FinishPrimaryExpr_Test0(0))
							TParams(ref e);
						else
							goto stop;
					}
					break;
				case TT.Not:
					TParams(ref e);
					break;
				case TT.At:
				case TT.LBrace:
					{
						var bb = BracedBlockOrTokenLiteral();
						// line 576
						if ((!e.IsCall || e.BaseStyle == NodeStyle.Operator)) {
							e = F.Call(e, bb, e.Range.StartIndex, bb.Range.EndIndex);
						} else {
							e = e.WithArgs(e.Args.Add(bb)).WithRange(e.Range.StartIndex, bb.Range.EndIndex);
						}
					}
					break;
				default:
					goto stop;
				}
				continue;
			match1:
				{
					var op = MatchAny();
					var rhs = Atom();
					// line 549
					e = F.Call((Symbol) op.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex);
				}
			}
		stop:;
		}
		LNode PrimaryExpr_NewStyleCast(LNode e)
		{
			TokenType la0;
			var lp = MatchAny();
			var rp = Match((int) TT.RParen);
			Down(lp);
			Symbol kind;
			var attrs = VList<LNode>.Empty;
			// Line 592: ( TT.PtrArrow | TT.As | TT.Using )
			la0 = LA0;
			if (la0 == TT.PtrArrow) {
				Skip();
				// line 592
				kind = S.Cast;
			} else if (la0 == TT.As) {
				Skip();
				// line 593
				kind = S.As;
			} else {
				Match((int) TT.Using);
				// line 594
				kind = S.UsingCast;
			}
			NormalAttributes(ref attrs);
			AttributeKeywords(ref attrs);
			var type = DataType();
			Match((int) EOF);
			// line 599
			type = type.PlusAttrs(attrs);
			return Up(SetAlternateStyle(SetOperatorStyle(F.Call(kind, e, type, e.Range.StartIndex, rp.EndIndex))));
		}
		static readonly HashSet<int> PrefixExpr_set0 = NewSet((int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.Base, (int) TT.Checked, (int) TT.ContextualKeyword, (int) TT.Default, (int) TT.Delegate, (int) TT.Dot, (int) TT.DotDot, (int) TT.Forward, (int) TT.Id, (int) TT.IncDec, (int) TT.Is, (int) TT.LBrace, (int) TT.Literal, (int) TT.LParen, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.Operator, (int) TT.Power, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.This, (int) TT.TypeKeyword, (int) TT.Typeof, (int) TT.Unchecked);
		static readonly HashSet<int> PrefixExpr_set1 = NewSet((int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.Base, (int) TT.Checked, (int) TT.ContextualKeyword, (int) TT.Default, (int) TT.Delegate, (int) TT.Dot, (int) TT.DotDot, (int) TT.Forward, (int) TT.Id, (int) TT.IncDec, (int) TT.Is, (int) TT.LBrace, (int) TT.LBrack, (int) TT.Literal, (int) TT.LParen, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.Operator, (int) TT.Power, (int) TT.RBrace, (int) TT.RParen, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.This, (int) TT.TypeKeyword, (int) TT.Typeof, (int) TT.Unchecked);
		LNode PrefixExpr()
		{
			TokenType la2;
			// Line 608: ( (TT.Add|TT.AndBits|TT.DotDot|TT.Forward|TT.IncDec|TT.Mul|TT.Not|TT.NotBits|TT.Sub) PrefixExpr | (&{Down($LI) && Up(Scan_DataType() && LA0 == EOF)} TT.LParen TT.RParen &!(((TT.Add|TT.AndBits|TT.BQString|TT.Dot|TT.Mul|TT.Sub) | TT.IncDec TT.LParen)) PrefixExpr / TT.Power PrefixExpr / &{Is($LI, _await)} TT.ContextualKeyword PrefixExpr / PrimaryExpr) )
			do {
				switch (LA0) {
				case TT.Add:
				case TT.AndBits:
				case TT.DotDot:
				case TT.Forward:
				case TT.IncDec:
				case TT.Mul:
				case TT.Not:
				case TT.NotBits:
				case TT.Sub:
					{
						var op = MatchAny();
						var e = PrefixExpr();
						// line 609
						return SetOperatorStyle(F.Call((Symbol) op.Value, e, op.StartIndex, e.Range.EndIndex));
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
									// line 615
									Down(lp);
									return SetOperatorStyle(F.Call(S.Cast, e, Up(DataType()), lp.StartIndex, e.Range.EndIndex));
								} else
									goto matchPrimaryExpr;
							} else
								goto matchPrimaryExpr;
						} else
							goto matchPrimaryExpr;
					}
					break;
				case TT.Power:
					{
						var op = MatchAny();
						var e = PrefixExpr();
						// line 618
						return SetOperatorStyle(F.Call(S._Dereference, SetOperatorStyle(F.Call(S._Dereference, e, op.StartIndex + 1, e.Range.EndIndex)), op.StartIndex, e.Range.EndIndex));
					}
					break;
				case TT.ContextualKeyword:
					{
						if (Is(0, _await)) {
							if (!(_insideLinqExpr && LinqKeywords.Contains(LT(0).Value))) {
								switch (LA(1)) {
								case TT.Add:
								case TT.AndBits:
								case TT.At:
								case TT.Dot:
								case TT.DotDot:
								case TT.IncDec:
								case TT.Is:
								case TT.LBrace:
								case TT.LParen:
								case TT.Mul:
								case TT.Not:
								case TT.NotBits:
								case TT.Power:
								case TT.Sub:
									{
										if (Down(1) && Up(Scan_DataType() && LA0 == EOF)) {
											if (Down(1) && Up(LA0 == TT.As || LA0 == TT.Using || LA0 == TT.PtrArrow)) {
												if (Try_FinishPrimaryExpr_Test0(1)) {
													la2 = LA(2);
													if (PrefixExpr_set1.Contains((int) la2))
														goto match4;
													else
														goto matchPrimaryExpr;
												} else {
													la2 = LA(2);
													if (PrefixExpr_set1.Contains((int) la2))
														goto match4;
													else
														goto matchPrimaryExpr;
												}
											} else if (Try_FinishPrimaryExpr_Test0(1)) {
												la2 = LA(2);
												if (PrefixExpr_set1.Contains((int) la2))
													goto match4;
												else
													goto matchPrimaryExpr;
											} else {
												la2 = LA(2);
												if (PrefixExpr_set1.Contains((int) la2))
													goto match4;
												else
													goto matchPrimaryExpr;
											}
										} else if (Down(1) && Up(LA0 == TT.As || LA0 == TT.Using || LA0 == TT.PtrArrow)) {
											if (Try_FinishPrimaryExpr_Test0(1)) {
												la2 = LA(2);
												if (PrefixExpr_set1.Contains((int) la2))
													goto match4;
												else
													goto matchPrimaryExpr;
											} else {
												la2 = LA(2);
												if (PrefixExpr_set1.Contains((int) la2))
													goto match4;
												else
													goto matchPrimaryExpr;
											}
										} else if (Try_FinishPrimaryExpr_Test0(1)) {
											la2 = LA(2);
											if (PrefixExpr_set1.Contains((int) la2))
												goto match4;
											else
												goto matchPrimaryExpr;
										} else {
											la2 = LA(2);
											if (PrefixExpr_set1.Contains((int) la2))
												goto match4;
											else
												goto matchPrimaryExpr;
										}
									}
								case TT.Base:
								case TT.Checked:
								case TT.ContextualKeyword:
								case TT.Default:
								case TT.Delegate:
								case TT.Forward:
								case TT.Id:
								case TT.Literal:
								case TT.New:
								case TT.Operator:
								case TT.Sizeof:
								case TT.Substitute:
								case TT.This:
								case TT.TypeKeyword:
								case TT.Typeof:
								case TT.Unchecked:
									goto match4;
								default:
									goto matchPrimaryExpr;
								}
							} else
								goto match4;
						} else
							goto matchPrimaryExpr;
					}
				default:
					goto matchPrimaryExpr;
				}
				break;
			match4:
				{
					var op = MatchAny();
					var e = PrefixExpr();
					// line 624
					return SetOperatorStyle(F.Call(_await, e, op.StartIndex, e.Range.EndIndex));
				}
				break;
			matchPrimaryExpr:
				{
					var e = PrimaryExpr();
					// line 626
					return e;
				}
			} while (false);
		}
		LNode Expr(Precedence context)
		{
			TokenType la0, la1;
			Debug.Assert(context.CanParse(EP.Prefix));
			Precedence prec;
			var e = PrefixExpr();
			// Line 667: greedy( &{context.CanParse(prec = InfixPrecedenceOf($LA))} (TT.Add|TT.And|TT.AndBits|TT.BQString|TT.CompoundSet|TT.DivMod|TT.DotDot|TT.EqNeq|TT.GT|TT.In|TT.LambdaArrow|TT.LEGE|TT.LT|TT.Mul|TT.NotBits|TT.NullCoalesce|TT.OrBits|TT.OrXor|TT.Power|TT.Set|TT.Sub|TT.XorBits) Expr | &{context.CanParse(prec = InfixPrecedenceOf($LA))} (TT.As|TT.Is|TT.Using) DataType FinishPrimaryExpr | &{context.CanParse(EP.Shift)} &{LT($LI).EndIndex == LT($LI + 1).StartIndex} (TT.LT TT.LT Expr | TT.GT TT.GT Expr) | &{context.CanParse(EP.IfElse)} TT.QuestionMark Expr TT.Colon Expr )*
			for (;;) {
				switch (LA0) {
				case TT.GT:
				case TT.LT:
					{
						la0 = LA0;
						if (context.CanParse(prec = InfixPrecedenceOf(la0))) {
							if (context.CanParse(EP.Shift)) {
								if (LT(0).EndIndex == LT(0 + 1).StartIndex) {
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
							} else if (LT(0).EndIndex == LT(0 + 1).StartIndex) {
								la1 = LA(1);
								if (PrefixExpr_set0.Contains((int) la1))
									goto match1;
								else
									goto stop;
							} else {
								la1 = LA(1);
								if (PrefixExpr_set0.Contains((int) la1))
									goto match1;
								else
									goto stop;
							}
						} else if (context.CanParse(EP.Shift)) {
							if (LT(0).EndIndex == LT(0 + 1).StartIndex) {
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
				case TT.Add:
				case TT.And:
				case TT.AndBits:
				case TT.BQString:
				case TT.CompoundSet:
				case TT.DivMod:
				case TT.DotDot:
				case TT.EqNeq:
				case TT.In:
				case TT.LambdaArrow:
				case TT.LEGE:
				case TT.Mul:
				case TT.NotBits:
				case TT.NullCoalesce:
				case TT.OrBits:
				case TT.OrXor:
				case TT.Power:
				case TT.Set:
				case TT.Sub:
				case TT.XorBits:
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
				case TT.As:
				case TT.Is:
				case TT.Using:
					{
						la0 = LA0;
						if (context.CanParse(prec = InfixPrecedenceOf(la0))) {
							switch (LA(1)) {
							case TT.ContextualKeyword:
							case TT.Id:
							case TT.Operator:
							case TT.Substitute:
							case TT.TypeKeyword:
								{
									var op = MatchAny();
									var rhs = DataType(true);
									var opSym = op.Type() == TT.Using ? S.UsingCast : ((Symbol) op.Value);
									e = SetOperatorStyle(F.Call(opSym, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex));
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
								Skip();
								var then = Expr(StartExpr);
								Match((int) TT.Colon);
								var @else = Expr(EP.IfElse);
								// line 690
								e = SetOperatorStyle(F.Call(S.QuestionMark, e, then, @else, e.Range.StartIndex, @else.Range.EndIndex));
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
					var rhs = Expr(prec);
					// line 671
					e = SetOperatorStyle(F.Call((Symbol) op.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex));
				}
				continue;
			match3:
				{
					// Line 682: (TT.LT TT.LT Expr | TT.GT TT.GT Expr)
					la0 = LA0;
					if (la0 == TT.LT) {
						Skip();
						Match((int) TT.LT);
						var rhs = Expr(EP.Shift);
						// line 683
						e = SetOperatorStyle(F.Call(S.Shl, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex));
					} else {
						Match((int) TT.GT);
						Match((int) TT.GT);
						var rhs = Expr(EP.Shift);
						// line 685
						e = SetOperatorStyle(F.Call(S.Shr, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex));
					}
				}
			}
		stop:;
			// line 692
			return e;
		}
		public LNode ExprStart(bool allowUnassignedVarDecl)
		{
			TokenType la0, la1;
			LNode result = default(LNode);
			// Line 700: ((TT.Id | IdNotLinqKeyword) TT.Colon ExprStart2 / ExprStart2)
			do {
				la0 = LA0;
				if (la0 == TT.Id) {
					la1 = LA(1);
					if (la1 == TT.Colon)
						goto match1;
					else
						result = ExprStart2(allowUnassignedVarDecl);
				} else if (la0 == TT.ContextualKeyword) {
					if (!(_insideLinqExpr && LinqKeywords.Contains(LT(0).Value))) {
						la1 = LA(1);
						if (la1 == TT.Colon)
							goto match1;
						else
							result = ExprStart2(allowUnassignedVarDecl);
					} else
						result = ExprStart2(allowUnassignedVarDecl);
				} else
					result = ExprStart2(allowUnassignedVarDecl);
				break;
			match1:
				{
					// line 700
					Token argName = default(Token);
					// Line 701: (TT.Id | IdNotLinqKeyword)
					la0 = LA0;
					if (la0 == TT.Id)
						argName = MatchAny();
					else
						argName = IdNotLinqKeyword();
					Skip();
					result = ExprStart2(allowUnassignedVarDecl);
					// line 703
					result = SetOperatorStyle(F.Call(S.NamedArg, F.Id(argName), result, argName.StartIndex, result.Range.EndIndex));
				}
			} while (false);
			return result;
		}
		public LNode ExprStart2(bool allowUnassignedVarDecl)
		{
			// line 707
			var attrs = VList<LNode>.Empty;
			NormalAttributes(ref attrs);
			AttributeKeywords(ref attrs);
			// line 712
			if ((!attrs.IsEmpty)) {
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
		LNode VarDeclExpr(out bool hasInitializer, VList < LNode > attrs)
		{
			TokenType la0;
			LNode result = default(LNode);
			// Line 832: (TT.This)?
			la0 = LA0;
			if (la0 == TT.This) {
				var t = MatchAny();
				// line 832
				attrs.Add(F.Id(t));
			}
			var pair = VarDeclStart();
			// line 834
			LNode type = pair.Item1, name = pair.Item2;
			// Line 837: (RestOfPropertyDefinition / VarInitializerOpt)
			switch (LA0) {
			case TT.At:
			case TT.ContextualKeyword:
			case TT.Forward:
			case TT.LambdaArrow:
			case TT.LBrace:
			case TT.LBrack:
				{
					result = RestOfPropertyDefinition(type, name, true);
					// line 838
					hasInitializer = true;
				}
				break;
			default:
				{
					var nameAndInit = VarInitializerOpt(name, IsArrayType(type));
					// line 841
					hasInitializer = (nameAndInit != name);
					result = F.Call(S.Var, type, nameAndInit, type.Range.StartIndex, nameAndInit.Range.EndIndex);
					hasInitializer = true;
				}
				break;
			}
			result = result.PlusAttrs(attrs);
			return result;
		}
		Pair<LNode,LNode> VarDeclStart()
		{
			var e = DataType();
			var id = IdAtom();
			MaybeRecognizeVarAsKeyword(ref e);
			return Pair.Create(e, id);
		}
		bool Scan_VarDeclStart()
		{
			if (!Scan_DataType())
				return false;
			if (!Scan_IdAtom())
				return false;
			return true;
		}
		LNode ExprInParens(bool allowUnassignedVarDecl)
		{
			var lp = Match((int) TT.LParen);
			var rp = Match((int) TT.RParen);
			// line 876
			if ((!Down(lp))) {
				return F.Call(S.Tuple, lp.StartIndex, rp.EndIndex);
			}
			return Up(InParens_ExprOrTuple(allowUnassignedVarDecl, lp.StartIndex, rp.EndIndex));
		}
		bool Scan_ExprInParens(bool allowUnassignedVarDecl)
		{
			if (!TryMatch((int) TT.LParen))
				return false;
			if (!TryMatch((int) TT.RParen))
				return false;
			return true;
		}
		LNode InParens_ExprOrTuple(bool allowUnassignedVarDecl, int startIndex, int endIndex)
		{
			TokenType la0, la1;
			// Line 883: (EOF / ExprStart nongreedy(TT.Comma ExprStart)* (TT.Comma)? EOF)
			do {
				la0 = LA0;
				if (la0 == EOF) {
					la1 = LA(1);
					if (la1 == (TokenType) EOF) {
						Skip();
						// line 884
						return F.Tuple(VList<LNode>.Empty, startIndex, endIndex);
					} else
						goto match2;
				} else
					goto match2;
				break;
			match2:
				{
					var e = ExprStart(allowUnassignedVarDecl);
					// line 886
					var list = new VList<LNode> { 
						e
					};
					// Line 888: nongreedy(TT.Comma ExprStart)*
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
					// line 890
					bool isTuple = list.Count > 1;
					// Line 891: (TT.Comma)?
					la0 = LA0;
					if (la0 == TT.Comma) {
						Skip();
						// line 891
						isTuple = true;
					}
					Match((int) EOF);
					// line 892
					return isTuple ? F.Tuple(list, startIndex, endIndex) : F.InParens(e, startIndex, endIndex);
				}
			} while (false);
		}
		LNode BracedBlockOrTokenLiteral(Symbol spaceName = null, Symbol target = null, int startIndex = -1)
		{
			TokenType la0;
			LNode result = default(LNode);
			// Line 895: (BracedBlock | TokenLiteral)
			la0 = LA0;
			if (la0 == TT.LBrace)
				result = BracedBlock(spaceName, target, startIndex);
			else
				result = TokenLiteral();
			return result;
		}
		LNode BracedBlock(Symbol spaceName = null, Symbol target = null, int startIndex = -1)
		{
			Token lit_lcub = default(Token);
			Token lit_rcub = default(Token);
			// line 900
			var oldSpace = _spaceName;
			_spaceName = spaceName ?? oldSpace;
			lit_lcub = Match((int) TT.LBrace);
			lit_rcub = Match((int) TT.RBrace);
			// line 904
			if ((startIndex == -1)) {
				startIndex = lit_lcub.StartIndex;
			}
			var stmts = StmtListInside(lit_lcub);
			_spaceName = oldSpace;
			return F.Call(target ?? S.Braces, stmts, lit_lcub.StartIndex, lit_rcub.EndIndex).SetBaseStyle(NodeStyle.Statement);
		}
		bool Scan_BracedBlock(Symbol spaceName = null, Symbol target = null, int startIndex = -1)
		{
			if (!TryMatch((int) TT.LBrace))
				return false;
			if (!TryMatch((int) TT.RBrace))
				return false;
			return true;
		}
		void NormalAttributes(ref VList<LNode> attrs)
		{
			TokenType la0, la1;
			// Line 917: (&!{Down($LI) && Up(Try_Scan_AsmOrModLabel(0))} TT.LBrack TT.RBrack)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.LBrack) {
					if (!(Down(0) && Up(Try_Scan_AsmOrModLabel(0)))) {
						la1 = LA(1);
						if (la1 == TT.RBrack) {
							var t = MatchAny();
							Skip();
							// line 920
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
		}
		bool Try_Scan_NormalAttributes(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_NormalAttributes();
		}
		bool Scan_NormalAttributes()
		{
			TokenType la0, la1;
			// Line 917: (&!{Down($LI) && Up(Try_Scan_AsmOrModLabel(0))} TT.LBrack TT.RBrack)*
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
			TokenType la0, la1;
			// line 928
			Token attrTarget = default(Token);
			// Line 930: greedy((TT.ContextualKeyword|TT.Id|TT.Return) TT.Colon)?
			la0 = LA0;
			if (la0 == TT.ContextualKeyword || la0 == TT.Id || la0 == TT.Return) {
				la1 = LA(1);
				if (la1 == TT.Colon) {
					attrTarget = MatchAny();
					Skip();
				}
			}
			ExprList(ref attrs, allowTrailingComma: true, allowUnassignedVarDecl: true);
			// line 935
			if (attrTarget.Value != null) {
				var attrTargetNode = F.Id(attrTarget);
				for (int i = 0; i < attrs.Count; i++) {
					var attr = attrs[i];
					if ((!IsNamedArg(attr))) {
						attrs[i] = SetOperatorStyle(F.Call(S.NamedArg, attrTargetNode, attr, attrTarget.StartIndex, attr.Range.EndIndex));
					} else {
						attrTargetNode = attrs[i].Args[1];
						Error(attrTargetNode, "Syntax error: only one attribute target is allowed");
					}
				}
			}
		}
		void AttributeKeywords(ref VList<LNode> attrs)
		{
			TokenType la0;
			// Line 954: (TT.AttrKeyword)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.AttrKeyword) {
					var t = MatchAny();
					// line 955
					attrs.Add(F.Id(t));
				} else
					break;
			}
		}
		void TParamAttributeKeywords(ref VList<LNode> attrs)
		{
			TokenType la0;
			// Line 960: ((TT.AttrKeyword|TT.In))*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.AttrKeyword || la0 == TT.In) {
					var t = MatchAny();
					// line 961
					attrs.Add(F.Id(t));
				} else
					break;
			}
		}
		bool Try_Scan_TParamAttributeKeywords(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_TParamAttributeKeywords();
		}
		bool Scan_TParamAttributeKeywords()
		{
			TokenType la0;
			// Line 960: ((TT.AttrKeyword|TT.In))*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.AttrKeyword || la0 == TT.In)
					{if (!TryMatch((int) TT.AttrKeyword, (int) TT.In))
						return false;}
				else
					break;
			}
			return true;
		}
		public LNode Stmt()
		{
			LNode result = default(LNode);
			var attrs = VList<LNode>.Empty;
			int startIndex = LT0.StartIndex;
			NormalAttributes(ref attrs);
			AttributeKeywords(ref attrs);
			// line 1122
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
		LNode MethodOrPropertyOrVarStmt(int startIndex, VList<LNode> attrs)
		{
			TokenType la0;
			LNode result = default(LNode);
			// Line 1370: ( TraitDecl / AliasDecl / MethodOrPropertyOrVar )
			do {
				la0 = LA0;
				if (la0 == TT.ContextualKeyword) {
					if (Is(0, _trait)) {
						if (Is(0, _alias)) {
							if (!(_insideLinqExpr && LinqKeywords.Contains(LT(0).Value))) {
								switch (LA(1)) {
								case TT.ContextualKeyword:
								case TT.Id:
								case TT.Operator:
								case TT.Substitute:
								case TT.This:
								case TT.TypeKeyword:
									goto matchTraitDecl;
								default:
									result = MethodOrPropertyOrVar(startIndex, attrs);
									break;
								}
							} else
								goto matchTraitDecl;
						} else if (!(_insideLinqExpr && LinqKeywords.Contains(LT(0).Value))) {
							switch (LA(1)) {
							case TT.ContextualKeyword:
							case TT.Id:
							case TT.Operator:
							case TT.Substitute:
							case TT.This:
							case TT.TypeKeyword:
								goto matchTraitDecl;
							default:
								result = MethodOrPropertyOrVar(startIndex, attrs);
								break;
							}
						} else
							goto matchTraitDecl;
					} else if (Is(0, _alias)) {
						if (!(_insideLinqExpr && LinqKeywords.Contains(LT(0).Value))) {
							switch (LA(1)) {
							case TT.ContextualKeyword:
							case TT.Id:
							case TT.Operator:
							case TT.Substitute:
							case TT.This:
							case TT.TypeKeyword:
								goto matchAliasDecl;
							default:
								result = MethodOrPropertyOrVar(startIndex, attrs);
								break;
							}
						} else
							goto matchAliasDecl;
					} else
						result = MethodOrPropertyOrVar(startIndex, attrs);
				} else
					result = MethodOrPropertyOrVar(startIndex, attrs);
				break;
			matchTraitDecl:
				{
					result = TraitDecl(startIndex);
					// line 1370
					result = result.PlusAttrs(attrs);
				}
				break;
			matchAliasDecl:
				{
					result = AliasDecl(startIndex);
					// line 1371
					result = result.PlusAttrs(attrs);
				}
			} while (false);
			return result;
		}
		LNode KeywordStmt(int startIndex, VList<LNode> attrs, bool hasWordAttrs)
		{
			TokenType la1;
			// line 1379
			LNode r;
			bool addAttrs = true;
			string showWordAttrErrorFor = null;
			// Line 1383: ( ((IfStmt | EventDecl | DelegateDecl | SpaceDecl | EnumDecl | CheckedOrUncheckedStmt | DoStmt | CaseStmt) | (GotoCaseStmt / GotoStmt) | ReturnBreakContinueThrow | WhileStmt | ForStmt | ForEachStmt | SwitchStmt) | (UsingStmt / UsingDirective) | LockStmt | FixedStmt | TryStmt )
			switch (LA0) {
			case TT.If:
				{
					r = IfStmt(startIndex);
					// line 1384
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
					// line 1386
					addAttrs = false;
				}
				break;
			case TT.Class:
			case TT.Interface:
			case TT.Namespace:
			case TT.Struct:
				r = SpaceDecl(startIndex);
				break;
			case TT.Enum:
				r = EnumDecl(startIndex);
				break;
			case TT.Checked:
			case TT.Unchecked:
				r = CheckedOrUncheckedStmt(startIndex);
				break;
			case TT.Do:
				r = DoStmt(startIndex);
				break;
			case TT.Case:
				r = CaseStmt(startIndex);
				break;
			case TT.Goto:
				{
					la1 = LA(1);
					if (la1 == TT.Case)
						r = GotoCaseStmt(startIndex);
					else
						r = GotoStmt(startIndex);
				}
				break;
			case TT.Break:
			case TT.Continue:
			case TT.Return:
			case TT.Throw:
				r = ReturnBreakContinueThrow(startIndex);
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
			case TT.Switch:
				r = SwitchStmt(startIndex);
				break;
			case TT.Using:
				{
					la1 = LA(1);
					if (la1 == TT.LParen) {
						r = UsingStmt(startIndex);
						// line 1400
						showWordAttrErrorFor = "using statement";
					} else {
						r = UsingDirective(startIndex, attrs);
						addAttrs = false;
						showWordAttrErrorFor = "using directive";
					}
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
				{
					// line 1406
					r = Error("Bug: Keyword statement expected, but got '{0}'", CurrentTokenText());
					ScanToEndOfStmt();
				}
				break;
			}
			// line 1410
			if (addAttrs) {
				r = r.PlusAttrs(attrs);
			}
			if (hasWordAttrs && showWordAttrErrorFor != null) {
				NonKeywordAttrError(attrs, showWordAttrErrorFor);
			}
			return r;
		}
		LNode IdStmt(int startIndex, VList<LNode> attrs, bool hasWordAttrs)
		{
			TokenType la1;
			LNode result = default(LNode);
			bool addAttrs = false;
			string showWordAttrErrorFor = null;
			// Line 1427: ( Constructor / BlockCallStmt / LabelStmt / &(DataType TT.This) DataType => MethodOrPropertyOrVar / ExprStatement )
			do {
				switch (LA0) {
				case TT.ContextualKeyword:
				case TT.Id:
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
								case TT.Forward:
								case TT.LBrace:
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
								case TT.Forward:
								case TT.LBrace:
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
							if (Is(0, _await)) {
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
								case TT.Forward:
								case TT.LBrace:
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
						} else if (Is(0, _await)) {
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
							case TT.Forward:
							case TT.LBrace:
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
				case TT.Operator:
				case TT.Substitute:
				case TT.TypeKeyword:
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
					// line 1428
					showWordAttrErrorFor = "old-style constructor";
				}
				break;
			matchBlockCallStmt:
				{
					result = BlockCallStmt(startIndex);
					// line 1430
					showWordAttrErrorFor = "block-call statement";
					addAttrs = true;
				}
				break;
			matchLabelStmt:
				{
					result = LabelStmt(startIndex);
					// line 1432
					addAttrs = true;
				}
				break;
			matchExprStatement:
				{
					result = ExprStatement();
					// line 1436
					showWordAttrErrorFor = "expression";
					addAttrs = true;
				}
			} while (false);
			// line 1439
			if (addAttrs) {
				result = result.PlusAttrs(attrs);
			}
			if (hasWordAttrs && showWordAttrErrorFor != null) {
				NonKeywordAttrError(attrs, showWordAttrErrorFor);
			}
			return result;
		}
		static readonly HashSet<int> OtherStmt_set0 = NewSet((int) EOF, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.As, (int) TT.At, (int) TT.Base, (int) TT.BQString, (int) TT.Catch, (int) TT.Checked, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.Default, (int) TT.Delegate, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.Else, (int) TT.EqNeq, (int) TT.Finally, (int) TT.Forward, (int) TT.GT, (int) TT.Id, (int) TT.In, (int) TT.IncDec, (int) TT.Is, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LEGE, (int) TT.Literal, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.Operator, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.This, (int) TT.TypeKeyword, (int) TT.Typeof, (int) TT.Unchecked, (int) TT.Using, (int) TT.While, (int) TT.XorBits);
		static readonly HashSet<int> OtherStmt_set1 = NewSet((int) EOF, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.As, (int) TT.At, (int) TT.BQString, (int) TT.Catch, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.Else, (int) TT.EqNeq, (int) TT.Finally, (int) TT.GT, (int) TT.In, (int) TT.IncDec, (int) TT.Is, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LEGE, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sub, (int) TT.Using, (int) TT.While, (int) TT.XorBits);
		LNode OtherStmt(int startIndex, VList<LNode> attrs, bool hasWordAttrs)
		{
			TokenType la1;
			Token lit_semi = default(Token);
			LNode result = default(LNode);
			bool addAttrs = false;
			string showWordAttrErrorFor = null;
			// Line 1463: ( BracedBlock / &(TT.NotBits (TT.ContextualKeyword|TT.Id|TT.This) TT.LParen TT.RParen TT.LBrace TT.RBrace) Destructor / TT.Semicolon / LabelStmt / default ExprStatement / AssemblyOrModuleAttribute / OperatorCastMethod )
			do {
				switch (LA0) {
				case TT.LBrace:
					{
						result = BracedBlock(null, null, startIndex);
						// line 1464
						showWordAttrErrorFor = "braced-block statement";
						addAttrs = true;
					}
					break;
				case TT.NotBits:
					{
						if (Try_OtherStmt_Test0(0)) {
							switch (LA(1)) {
							case TT.ContextualKeyword:
							case TT.Id:
							case TT.This:
								{
									result = Destructor(startIndex, attrs);
									// line 1467
									showWordAttrErrorFor = "destructor";
								}
								break;
							case TT.Add:
							case TT.AndBits:
							case TT.At:
							case TT.Base:
							case TT.Checked:
							case TT.Default:
							case TT.Delegate:
							case TT.Dot:
							case TT.DotDot:
							case TT.Forward:
							case TT.IncDec:
							case TT.Is:
							case TT.LBrace:
							case TT.Literal:
							case TT.LParen:
							case TT.Mul:
							case TT.New:
							case TT.Not:
							case TT.NotBits:
							case TT.Operator:
							case TT.Power:
							case TT.Sizeof:
							case TT.Sub:
							case TT.Substitute:
							case TT.TypeKeyword:
							case TT.Typeof:
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
						// line 1468
						result = F.Id(S.Missing, startIndex, lit_semi.EndIndex);
						showWordAttrErrorFor = "empty statement";
						addAttrs = true;
					}
					break;
				case TT.ContextualKeyword:
					{
						if (Is(0, _await)) {
							if (!(_insideLinqExpr && LinqKeywords.Contains(LT(0).Value))) {
								la1 = LA(1);
								if (la1 == TT.Colon)
									goto matchLabelStmt;
								else if (OtherStmt_set0.Contains((int) la1))
									goto matchExprStatement;
								else
									goto error;
							} else {
								la1 = LA(1);
								if (la1 == TT.Colon)
									goto matchLabelStmt;
								else if (PrefixExpr_set0.Contains((int) la1))
									goto matchExprStatement;
								else
									goto error;
							}
						} else if (!(_insideLinqExpr && LinqKeywords.Contains(LT(0).Value))) {
							la1 = LA(1);
							if (la1 == TT.Colon)
								goto matchLabelStmt;
							else if (OtherStmt_set1.Contains((int) la1))
								goto matchExprStatement;
							else
								goto error;
						} else
							goto matchLabelStmt;
					}
				case TT.Id:
					{
						la1 = LA(1);
						if (la1 == TT.Colon)
							goto matchLabelStmt;
						else if (OtherStmt_set1.Contains((int) la1))
							goto matchExprStatement;
						else
							goto error;
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
				case TT.Add:
				case TT.AndBits:
				case TT.Dot:
				case TT.DotDot:
				case TT.Forward:
				case TT.IncDec:
				case TT.LParen:
				case TT.Mul:
				case TT.Not:
				case TT.Power:
				case TT.Sub:
				case TT.Substitute:
					goto matchExprStatement;
				case TT.Operator:
					{
						la1 = LA(1);
						switch (la1) {
						case TT.ContextualKeyword:
						case TT.Id:
						case TT.Operator:
						case TT.TypeKeyword:
							{
								result = OperatorCastMethod(startIndex, attrs);
								// line 1476
								attrs.Clear();
							}
							break;
						default:
							if (AnyOperator_set0.Contains((int) la1))
								goto matchExprStatement;
							else
								goto error;
						}
					}
					break;
				case TT.At:
				case TT.Base:
				case TT.Checked:
				case TT.Delegate:
				case TT.Is:
				case TT.Literal:
				case TT.New:
				case TT.Sizeof:
				case TT.This:
				case TT.TypeKeyword:
				case TT.Typeof:
				case TT.Unchecked:
					goto matchExprStatement;
				case TT.LBrack:
					{
						result = AssemblyOrModuleAttribute(startIndex, attrs);
						// line 1475
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
					// line 1471
					addAttrs = true;
				}
				break;
			matchExprStatement:
				{
					result = ExprStatement();
					// line 1473
					showWordAttrErrorFor = "expression";
					addAttrs = true;
				}
				break;
			error:
				{
					// line 1482
					result = Error("Statement expected, but got '{0}'", CurrentTokenText());
					ScanToEndOfStmt();
				}
			} while (false);
			// line 1486
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
			LNode result = default(LNode);
			result = Expr(StartExpr);
			// Line 1497: ((EOF|TT.Catch|TT.Else|TT.Finally|TT.While) =>  | TT.Semicolon)
			switch (LA0) {
			case EOF:
			case TT.Catch:
			case TT.Else:
			case TT.Finally:
			case TT.While:
				// line 1498
				result = F.Call(S.Result, result, result.Range.StartIndex, result.Range.EndIndex);
				break;
			case TT.Semicolon:
				Skip();
				break;
			default:
				{
					// line 1500
					result = Error("Syntax error in expression at '{0}'; possibly missing semicolon", CurrentTokenText());
					ScanToEndOfStmt();
				}
				break;
			}
			return result;
		}
		void ScanToEndOfStmt()
		{
			TokenType la0;
			// Line 1507: greedy(~(EOF|TT.LBrace|TT.Semicolon))*
			for (;;) {
				la0 = LA0;
				if (!(la0 == (TokenType) EOF || la0 == TT.LBrace || la0 == TT.Semicolon))
					Skip();
				else
					break;
			}
			// Line 1508: greedy(TT.Semicolon | TT.LBrace (TT.RBrace)?)?
			la0 = LA0;
			if (la0 == TT.Semicolon)
				Skip();
			else if (la0 == TT.LBrace) {
				Skip();
				// Line 1508: (TT.RBrace)?
				la0 = LA0;
				if (la0 == TT.RBrace)
					Skip();
			}
		}
		LNode SpaceDecl(int startIndex)
		{
			var t = MatchAny();
			// line 1517
			var kind = (Symbol) t.Value;
			var r = RestOfSpaceDecl(startIndex, kind);
			// line 1519
			return r;
		}
		LNode TraitDecl(int startIndex)
		{
			Check(Is(0, _trait), "Is($LI, _trait)");
			var t = Match((int) TT.ContextualKeyword);
			var r = RestOfSpaceDecl(startIndex, S.Trait);
			// line 1525
			return r;
		}
		LNode RestOfSpaceDecl(int startIndex, Symbol kind)
		{
			TokenType la0;
			var name = ComplexNameDecl();
			var bases = BaseListOpt();
			WhereClausesOpt(ref name);
			// Line 1532: (TT.Semicolon | BracedBlock)
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				var end = MatchAny();
				// line 1533
				return F.Call(kind, name, bases, startIndex, end.EndIndex);
			} else {
				var body = BracedBlock(EcsValidators.KeyNameComponentOf(name));
				// line 1535
				return F.Call(kind, name, bases, body, startIndex, body.Range.EndIndex);
			}
		}
		LNode AliasDecl(int startIndex)
		{
			LNode result = default(LNode);
			Check(Is(0, _alias), "Is($LI, _alias)");
			var t = Match((int) TT.ContextualKeyword);
			var newName = ComplexNameDecl();
			Match((int) TT.QuickBindSet, (int) TT.Set);
			var oldName = ComplexNameDecl();
			result = RestOfAlias(oldName, startIndex, newName);
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
			// Line 1550: (&{Is($LI, S.Static)} TT.AttrKeyword ExprStart TT.Semicolon / ExprStart (&{nsName.Calls(S.Assign, 2)} RestOfAlias / TT.Semicolon))
			do {
				la0 = LA0;
				if (la0 == TT.AttrKeyword) {
					if (Is(0, S.Static)) {
						static_ = MatchAny();
						nsName = ExprStart(true);
						end = Match((int) TT.Semicolon);
						// line 1552
						attrs.Add(F.Id(static_));
					} else
						goto matchExprStart;
				} else
					goto matchExprStart;
				break;
			matchExprStart:
				{
					nsName = ExprStart(true);
					// Line 1555: (&{nsName.Calls(S.Assign, 2)} RestOfAlias / TT.Semicolon)
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
						case TT.Colon:
						case TT.ContextualKeyword:
						case TT.LBrace:
							goto matchRestOfAlias;
						default:
							{
								// line 1561
								Error("Expected ';'");
							}
							break;
						}
						break;
					matchRestOfAlias:
						{
							Check(nsName.Calls(S.Assign, 2), "nsName.Calls(S.Assign, 2)");
							LNode aliasedType = nsName.Args[1, F.Missing];
							nsName = nsName.Args[0, F.Missing];
							var r = RestOfAlias(aliasedType, startIndex, nsName);
							// line 1559
							return r.WithAttrs(attrs).PlusAttr(_filePrivate);
						}
					} while (false);
				}
			} while (false);
			// line 1564
			return F.Call(S.Import, nsName, t.StartIndex, end.EndIndex).WithAttrs(attrs);
		}
		LNode RestOfAlias(LNode oldName, int startIndex, LNode newName)
		{
			TokenType la0;
			var bases = BaseListOpt();
			WhereClausesOpt(ref newName);
			// line 1570
			var name = F.Call(S.Assign, newName, oldName, newName.Range.StartIndex, oldName.Range.EndIndex);
			// Line 1571: (TT.Semicolon | BracedBlock)
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				var end = MatchAny();
				// line 1572
				return F.Call(S.Alias, name, bases, startIndex, end.EndIndex);
			} else {
				var body = BracedBlock(EcsValidators.KeyNameComponentOf(newName));
				// line 1574
				return F.Call(S.Alias, name, bases, body, startIndex, body.Range.EndIndex);
			}
		}
		LNode EnumDecl(int startIndex)
		{
			TokenType la0;
			var t = MatchAny();
			var name = ComplexNameDecl();
			var bases = BaseListOpt();
			// Line 1582: (TT.Semicolon | TT.LBrace TT.RBrace)
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				var end = MatchAny();
				// line 1583
				return F.Call(S.Enum, name, bases, startIndex, end.EndIndex);
			} else {
				var lb = Match((int) TT.LBrace);
				var rb = Match((int) TT.RBrace);
				// line 1586
				var list = ExprListInside(lb, true);
				var body = F.Braces(list, lb.StartIndex, rb.EndIndex);
				return F.Call(S.Enum, name, bases, body, startIndex, body.Range.EndIndex);
			}
		}
		LNode BaseListOpt()
		{
			TokenType la0;
			// Line 1594: (TT.Colon DataType (TT.Comma DataType)* | )
			la0 = LA0;
			if (la0 == TT.Colon) {
				// line 1594
				var bases = new VList<LNode>();
				Skip();
				bases.Add(DataType());
				// Line 1596: (TT.Comma DataType)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						Skip();
						bases.Add(DataType());
					} else
						break;
				}
				// line 1597
				return F.List(bases);
			} else
				// line 1598
				return F.List();
		}
		void WhereClausesOpt(ref LNode name)
		{
			TokenType la0;
			// line 1604
			var list = new BMultiMap<Symbol,LNode>();
			// Line 1605: (WhereClause)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.ContextualKeyword)
					list.Add(WhereClause());
				else
					break;
			}
			// line 1606
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
		KeyValuePair<Symbol,LNode> WhereClause()
		{
			TokenType la0;
			Check(Is(0, _where), "Is($LI, _where)");
			var where = MatchAny();
			var T = Match((int) TT.ContextualKeyword, (int) TT.Id);
			Match((int) TT.Colon);
			// line 1636
			var constraints = VList<LNode>.Empty;
			constraints.Add(WhereConstraint());
			// Line 1638: (TT.Comma WhereConstraint)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Comma) {
					Skip();
					constraints.Add(WhereConstraint());
				} else
					break;
			}
			// line 1639
			return new KeyValuePair<Symbol,LNode>((Symbol) T.Value, F.Call(S.Where, constraints, where.StartIndex, constraints.Last.Range.EndIndex));
		}
		LNode WhereConstraint()
		{
			TokenType la0;
			// Line 1643: ( (TT.Class|TT.Struct) | TT.New &{LT($LI).Count == 0} TT.LParen TT.RParen | DataType )
			la0 = LA0;
			if (la0 == TT.Class || la0 == TT.Struct) {
				var t = MatchAny();
				// line 1643
				return F.Id(t);
			} else if (la0 == TT.New) {
				var n = MatchAny();
				Check(LT(0).Count == 0, "LT($LI).Count == 0");
				var lp = Match((int) TT.LParen);
				var rp = Match((int) TT.RParen);
				// line 1645
				return F.Call(S.New, n.StartIndex, rp.EndIndex);
			} else {
				var t = DataType();
				// line 1646
				return t;
			}
		}
		Token AsmOrModLabel()
		{
			Check(LT(0).Value == _assembly || LT(0).Value == _module, "LT($LI).Value == _assembly || LT($LI).Value == _module");
			var t = Match((int) TT.ContextualKeyword);
			Match((int) TT.Colon);
			// line 1656
			return t;
		}
		bool Try_Scan_AsmOrModLabel(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_AsmOrModLabel();
		}
		bool Scan_AsmOrModLabel()
		{
			if (!(LT(0).Value == _assembly || LT(0).Value == _module))
				return false;
			if (!TryMatch((int) TT.ContextualKeyword))
				return false;
			if (!TryMatch((int) TT.Colon))
				return false;
			return true;
		}
		LNode AssemblyOrModuleAttribute(int startIndex, VList<LNode> attrs)
		{
			Check(Down(0) && Up(Try_Scan_AsmOrModLabel(0)), "Down($LI) && Up(Try_Scan_AsmOrModLabel(0))");
			var lb = MatchAny();
			var rb = Match((int) TT.RBrack);
			// line 1662
			Down(lb);
			var kind = AsmOrModLabel();
			// line 1664
			var list = new VList<LNode>();
			ExprList(ref list);
			// line 1667
			Up();
			var r = F.Call(kind.Value == _module ? S.Module : S.Assembly, list, startIndex, rb.EndIndex);
			return r.WithAttrs(attrs);
		}
		LNode MethodOrPropertyOrVar(int startIndex, VList<LNode> attrs)
		{
			TokenType la0;
			LNode name = default(LNode);
			LNode result = default(LNode);
			// line 1679
			bool isExtensionMethod = false;
			bool isNamedThis;
			// Line 1680: (TT.This)?
			la0 = LA0;
			if (la0 == TT.This) {
				var t = MatchAny();
				// line 1680
				attrs.Add(F.Id(t));
				isExtensionMethod = true;
			}
			var type = DataType();
			name = ComplexNameDecl(!isExtensionMethod, out isNamedThis);
			// Line 1684: ( &{!isNamedThis} VarInitializerOpt (TT.Comma ComplexNameDecl VarInitializerOpt)* TT.Semicolon / &{!isNamedThis} MethodArgListAndBody | RestOfPropertyDefinition )
			switch (LA0) {
			case TT.Comma:
			case TT.QuickBindSet:
			case TT.Semicolon:
			case TT.Set:
				{
					Check(!isNamedThis, "!isNamedThis");
					MaybeRecognizeVarAsKeyword(ref type);
					var parts = LNode.List(type);
					var isArray = IsArrayType(type);
					parts.Add(VarInitializerOpt(name, isArray));
					// Line 1689: (TT.Comma ComplexNameDecl VarInitializerOpt)*
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
					// line 1692
					result = F.Call(S.Var, parts, type.Range.StartIndex, end.EndIndex);
				}
				break;
			case TT.LParen:
				{
					Check(!isNamedThis, "!isNamedThis");
					result = MethodArgListAndBody(startIndex, attrs, S.Fn, type, name);
					// line 1696
					return result;
				}
				break;
			case TT.At:
			case TT.ContextualKeyword:
			case TT.Forward:
			case TT.LambdaArrow:
			case TT.LBrace:
			case TT.LBrack:
				result = RestOfPropertyDefinition(type, name, false);
				break;
			default:
				{
					// line 1699
					Error("Syntax error in what appears to be a method, property, or variable declaration");
					ScanToEndOfStmt();
					// line 1701
					result = F.Call(S.Var, type, name, type.Range.StartIndex, name.Range.EndIndex);
				}
				break;
			}
			result = result.PlusAttrs(attrs);
			return result;
		}
		LNode VarInitializerOpt(LNode name, bool isArray)
		{
			TokenType la0;
			LNode expr = default(LNode);
			// Line 1707: (VarInitializer)?
			la0 = LA0;
			if (la0 == TT.QuickBindSet || la0 == TT.Set) {
				expr = VarInitializer(isArray);
				// line 1708
				return F.Call(S.Assign, name, expr, name.Range.StartIndex, expr.Range.EndIndex);
			}
			// line 1709
			return name;
		}
		LNode VarInitializer(bool isArray)
		{
			TokenType la0, la1;
			LNode result = default(LNode);
			Skip();
			// Line 1716: (&{isArray} &{Down($LI) && Up(HasNoSemicolons())} TT.LBrace TT.RBrace / ExprStart)
			la0 = LA0;
			if (la0 == TT.LBrace) {
				if (isArray) {
					if (Down(0) && Up(HasNoSemicolons())) {
						la1 = LA(1);
						if (la1 == TT.RBrace) {
							var lb = MatchAny();
							var rb = MatchAny();
							// line 1720
							var initializers = InitializerListInside(lb);
							result = F.Call(S.ArrayInit, initializers, lb.StartIndex, rb.EndIndex).SetBaseStyle(NodeStyle.OldStyle);
						} else
							result = ExprStart(false);
					} else
						result = ExprStart(false);
				} else
					result = ExprStart(false);
			} else
				result = ExprStart(false);
			return result;
		}
		LNode RestOfPropertyDefinition(LNode type, LNode name, bool isExpression)
		{
			TokenType la0;
			Token lb = default(Token);
			Token rb = default(Token);
			LNode result = default(LNode);
			WhereClausesOpt(ref name);
			// line 1730
			LNode args = F.Missing;
			// Line 1731: (TT.LBrack TT.RBrack)?
			la0 = LA0;
			if (la0 == TT.LBrack) {
				lb = MatchAny();
				rb = Match((int) TT.RBrack);
				// line 1731
				args = ArgList(lb, rb);
			}
			// line 1732
			LNode initializer;
			var body = MethodBodyOrForward(true, out initializer, isExpression);
			result = (initializer != null ? F.Property(type, name, args, body, initializer, type.Range.StartIndex, initializer.Range.EndIndex) : F.Property(type, name, args, body, null, type.Range.StartIndex, body.Range.EndIndex));
			return result;
		}
		LNode OperatorCastMethod(int startIndex, VList<LNode> attrs)
		{
			// line 1741
			LNode r;
			var op = MatchAny();
			var type = DataType();
			// line 1743
			var name = F.Attr(_triviaUseOperatorKeyword, F.Id(S.Cast, op.StartIndex, op.EndIndex));
			r = MethodArgListAndBody(startIndex, attrs, S.Fn, type, name);
			// line 1745
			return r;
		}
		LNode MethodArgListAndBody(int startIndex, VList<LNode> attrs, Symbol kind, LNode type, LNode name)
		{
			TokenType la0;
			var lp = Match((int) TT.LParen);
			var rp = Match((int) TT.RParen);
			WhereClausesOpt(ref name);
			// line 1751
			LNode r, baseCall = null;
			// Line 1752: (TT.Colon (TT.Base|TT.This) TT.LParen TT.RParen)?
			la0 = LA0;
			if (la0 == TT.Colon) {
				Skip();
				var target = Match((int) TT.Base, (int) TT.This);
				var baselp = Match((int) TT.LParen);
				var baserp = Match((int) TT.RParen);
				// line 1754
				baseCall = F.Call((Symbol) target.Value, ExprListInside(baselp), target.StartIndex, baserp.EndIndex);
				if ((kind != S.Constructor)) {
					Error(baseCall, "This is not a constructor declaration, so there should be no ':' clause.");
				}
			}
			// line 1761
			for (int i = 0; i < attrs.Count; i++) {
				var attr = attrs[i];
				if (IsNamedArg(attr) && attr.Args[0].IsIdNamed(S.Return)) {
					type = type.PlusAttr(attr.Args[1]);
					attrs.RemoveAt(i);
					i--;
				}
			}
			// Line 1770: (default TT.Semicolon | MethodBodyOrForward)
			do {
				switch (LA0) {
				case TT.Semicolon:
					goto match1;
				case TT.At:
				case TT.Forward:
				case TT.LambdaArrow:
				case TT.LBrace:
					{
						var body = MethodBodyOrForward();
						// line 1782
						if (kind == S.Delegate) {
							Error("A 'delegate' is not expected to have a method body.");
						}
						if (baseCall != null) {
							body = body.WithArgs(body.Args.Insert(0, baseCall)).WithRange(baseCall.Range.StartIndex, body.Range.EndIndex);
						}
						var parts = new VList<LNode> { 
							type, name, ArgList(lp, rp), body
						};
						r = F.Call(kind, parts, startIndex, body.Range.EndIndex);
					}
					break;
				default:
					goto match1;
				}
				break;
			match1:
				{
					var end = Match((int) TT.Semicolon);
					// line 1772
					if (kind == S.Constructor && baseCall != null) {
						Error(baseCall, "A method body is required.");
						var parts = new VList<LNode> { 
							type, name, ArgList(lp, rp), LNode.Call(S.Braces, new VList<LNode>(baseCall), baseCall.Range)
						};
						return F.Call(kind, parts, startIndex, baseCall.Range.EndIndex);
					}
					r = F.Call(kind, type, name, ArgList(lp, rp), startIndex, end.EndIndex);
				}
			} while (false);
			// line 1790
			return r.PlusAttrs(attrs);
		}
		LNode MethodBodyOrForward(bool isProperty, out LNode propInitializer, bool isExpression = false)
		{
			TokenType la0;
			// line 1795
			propInitializer = null;
			// Line 1796: ( TT.Forward ExprStart SemicolonIf | TT.LambdaArrow ExprStart SemicolonIf | TokenLiteral (&{!isExpression} TT.Semicolon)? | BracedBlock greedy(&{isProperty} TT.Set ExprStart SemicolonIf)? )
			la0 = LA0;
			if (la0 == TT.Forward) {
				var op = MatchAny();
				var e = ExprStart(true);
				SemicolonIf(!isExpression);
				// line 1796
				return F.Call(S.Forward, e, op.StartIndex, e.Range.EndIndex);
			} else if (la0 == TT.LambdaArrow) {
				var op = MatchAny();
				var e = ExprStart(false);
				SemicolonIf(!isExpression);
				// line 1797
				return e;
			} else if (la0 == TT.At) {
				var e = TokenLiteral();
				// Line 1798: (&{!isExpression} TT.Semicolon)?
				la0 = LA0;
				if (la0 == TT.Semicolon) {
					Check(!isExpression, "!isExpression");
					Skip();
				}
				// line 1798
				return e;
			} else {
				var body = BracedBlock(S.Fn);
				// Line 1802: greedy(&{isProperty} TT.Set ExprStart SemicolonIf)?
				la0 = LA0;
				if (la0 == TT.Set) {
					Check(isProperty, "isProperty");
					Skip();
					propInitializer = ExprStart(false);
					SemicolonIf(!isExpression);
				}
				// line 1805
				return body;
			}
		}
		void SemicolonIf(bool isStatement)
		{
			TokenType la0;
			// Line 1810: (&{isStatement} TT.Semicolon / )
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				if (isStatement)
					Skip();
				else// line 1811
				if (isStatement) {
					Error(0, "Expected ';' to end statement");
				}
			} else// line 1811
			if (isStatement) {
				Error(0, "Expected ';' to end statement");
			}
		}
		void NoSemicolons()
		{
			TokenType la0;
			// Line 1832: (~(EOF|TT.Semicolon))*
			for (;;) {
				la0 = LA0;
				if (!(la0 == (TokenType) EOF || la0 == TT.Semicolon))
					Skip();
				else
					break;
			}
			Match((int) EOF);
		}
		bool Try_HasNoSemicolons(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return HasNoSemicolons();
		}
		bool HasNoSemicolons()
		{
			TokenType la0;
			// Line 1832: (~(EOF|TT.Semicolon))*
			for (;;) {
				la0 = LA0;
				if (!(la0 == (TokenType) EOF || la0 == TT.Semicolon))
					{if (!TryMatchExcept((int) TT.Semicolon))
						return false;}
				else
					break;
			}
			if (!TryMatch((int) EOF))
				return false;
			return true;
		}
		LNode Constructor(int startIndex, VList<LNode> attrs)
		{
			TokenType la0;
			// line 1839
			LNode r;
			Token n;
			// Line 1840: ( &{_spaceName == LT($LI).Value} (TT.ContextualKeyword|TT.Id) &(TT.LParen TT.RParen (TT.LBrace|TT.Semicolon)) / &{_spaceName != S.Fn || LA($LI + 3) == TT.LBrace} TT.This &(TT.LParen TT.RParen (TT.LBrace|TT.Semicolon)) / (TT.ContextualKeyword|TT.Id|TT.This) &(TT.LParen TT.RParen TT.Colon) )
			do {
				la0 = LA0;
				if (la0 == TT.ContextualKeyword || la0 == TT.Id) {
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
					n = Match((int) TT.ContextualKeyword, (int) TT.Id, (int) TT.This);
					Check(Try_Constructor_Test2(0), "TT.LParen TT.RParen TT.Colon");
				}
			} while (false);
			// line 1849
			LNode name = F.Id((Symbol) n.Value, n.StartIndex, n.EndIndex);
			r = MethodArgListAndBody(startIndex, attrs, S.Constructor, F.Missing, name);
			// line 1851
			return r;
		}
		LNode Destructor(int startIndex, VList<LNode> attrs)
		{
			LNode result = default(LNode);
			var tilde = MatchAny();
			var n = MatchAny();
			// line 1857
			var name = (Symbol) n.Value;
			if (name != _spaceName) {
				Error("Unexpected destructor '{0}'", name);
			}
			LNode name2 = F.Call(S.NotBits, F.Id(name, n.StartIndex, n.EndIndex), tilde.StartIndex, n.EndIndex);
			result = MethodArgListAndBody(startIndex, attrs, S.Fn, F.Missing, name2);
			return result;
		}
		LNode DelegateDecl(int startIndex, VList<LNode> attrs)
		{
			Skip();
			var type = DataType();
			var name = ComplexNameDecl();
			var r = MethodArgListAndBody(startIndex, attrs, S.Delegate, type, name);
			// line 1873
			return r.WithAttrs(attrs);
		}
		LNode EventDecl(int startIndex)
		{
			TokenType la0;
			Token lit_semi = default(Token);
			LNode result = default(LNode);
			Skip();
			var type = DataType();
			var name = ComplexNameDecl();
			// Line 1879: (TT.Comma ComplexNameDecl (TT.Comma ComplexNameDecl)*)?
			la0 = LA0;
			if (la0 == TT.Comma) {
				// line 1879
				var parts = new VList<LNode>(name);
				Skip();
				parts.Add(ComplexNameDecl());
				// Line 1880: (TT.Comma ComplexNameDecl)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						Skip();
						parts.Add(ComplexNameDecl());
					} else
						break;
				}
				// line 1881
				name = F.List(parts, name.Range.StartIndex, parts.Last.Range.EndIndex);
			}
			// Line 1883: (TT.Semicolon | BracedBlock)
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				lit_semi = MatchAny();
				// line 1884
				result = F.Call(S.Event, type, name, startIndex, lit_semi.EndIndex);
			} else {
				var body = BracedBlock(S.Fn);
				if (name.Calls(S.AltList)) {
					Error("A body is not allowed when defining multiple events.");
				}
				result = F.Call(S.Event, type, name, body, startIndex, body.Range.EndIndex);
			}
			return result;
		}
		LNode LabelStmt(int startIndex)
		{
			var id = Match((int) TT.ContextualKeyword, (int) TT.Default, (int) TT.Id);
			var end = Match((int) TT.Colon);
			// line 1899
			return F.Call(S.Label, F.Id(id), startIndex, end.EndIndex);
		}
		LNode CaseStmt(int startIndex)
		{
			TokenType la0;
			// line 1903
			var cases = VList<LNode>.Empty;
			var kw = Match((int) TT.Case);
			cases.Add(ExprStart2(true));
			// Line 1905: (TT.Comma ExprStart2)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Comma) {
					Skip();
					cases.Add(ExprStart2(true));
				} else
					break;
			}
			var end = Match((int) TT.Colon);
			// line 1906
			return F.Call(S.Case, cases, startIndex, end.EndIndex);
		}
		LNode BlockCallStmt(int startIndex)
		{
			TokenType la0;
			var id = MatchAny();
			Check(Try_BlockCallStmt_Test0(0), "( TT.LParen TT.RParen (TT.LBrace TT.RBrace | TT.Id) | TT.LBrace TT.RBrace | TT.Forward )");
			var args = new VList<LNode>();
			LNode block;
			// Line 1924: ( TT.LParen TT.RParen (BracedBlock | TT.Id => Stmt) | TT.Forward ExprStart TT.Semicolon | BracedBlock )
			la0 = LA0;
			if (la0 == TT.LParen) {
				var lp = MatchAny();
				var rp = Match((int) TT.RParen);
				// line 1924
				args = AppendExprsInside(lp, args, false, true);
				// Line 1925: (BracedBlock | TT.Id => Stmt)
				la0 = LA0;
				if (la0 == TT.LBrace)
					block = BracedBlock();
				else {
					block = Stmt();
					// line 1928
					ErrorSink.Write(Severity.Error, block, ColumnOf(block.Range.StartIndex) <= ColumnOf(id.StartIndex) ? "Probable missing semicolon before this statement." : "Probable missing braces around body of '{0}' statement.", id.Value);
				}
			} else if (la0 == TT.Forward) {
				var fwd = MatchAny();
				var e = ExprStart(true);
				Match((int) TT.Semicolon);
				// line 1935
				block = SetOperatorStyle(F.Call(S.Forward, e, fwd.StartIndex, e.Range.EndIndex));
			} else
				block = BracedBlock();
			// line 1939
			args.Add(block);
			var result = F.Call((Symbol) id.Value, args, id.StartIndex, block.Range.EndIndex);
			if (block.Calls(S.Forward, 1)) {
				result = F.Attr(_triviaForwardedProperty, result);
			}
			return result.SetBaseStyle(NodeStyle.Special);
		}
		LNode ReturnBreakContinueThrow(int startIndex)
		{
			var kw = MatchAny();
			var e = ExprOrNull(false);
			var end = Match((int) TT.Semicolon);
			// line 1958
			if (e != null)
				 return F.Call((Symbol) kw.Value, e, startIndex, end.EndIndex);
			else
				 return F.Call((Symbol) kw.Value, startIndex, end.EndIndex);
		}
		LNode GotoStmt(int startIndex)
		{
			Skip();
			var e = ExprOrNull(false);
			var end = Match((int) TT.Semicolon);
			// line 1968
			if (e != null)
				 return F.Call(S.Goto, e, startIndex, end.EndIndex);
			else
				 return F.Call(S.Goto, startIndex, end.EndIndex);
		}
		LNode GotoCaseStmt(int startIndex)
		{
			TokenType la0, la1;
			// line 1974
			LNode e = null;
			Skip();
			Skip();
			// Line 1976: (TT.Default / ExprOpt)
			la0 = LA0;
			if (la0 == TT.Default) {
				la1 = LA(1);
				if (la1 == TT.Semicolon) {
					var @def = MatchAny();
					// line 1977
					e = F.Id(S.Default, @def.StartIndex, @def.EndIndex);
				} else
					e = ExprOpt(false);
			} else
				e = ExprOpt(false);
			var end = Match((int) TT.Semicolon);
			// line 1980
			return F.Call(S.GotoCase, e, startIndex, end.EndIndex);
		}
		LNode CheckedOrUncheckedStmt(int startIndex)
		{
			var kw = MatchAny();
			var bb = BracedBlock();
			// line 1988
			return F.Call((Symbol) kw.Value, bb, startIndex, bb.Range.EndIndex);
		}
		LNode DoStmt(int startIndex)
		{
			Token lit_lpar = default(Token);
			Skip();
			var block = Stmt();
			Match((int) TT.While);
			lit_lpar = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var end = Match((int) TT.Semicolon);
			// line 1996
			var parts = new VList<LNode>(block);
			SingleExprInside(lit_lpar, "while (...)", false, ref parts);
			return F.Call(S.DoWhile, parts, startIndex, end.EndIndex);
		}
		LNode WhileStmt(int startIndex)
		{
			Token lit_lpar = default(Token);
			Skip();
			lit_lpar = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			// line 2005
			var cond = SingleExprInside(lit_lpar, "while (...)");
			return F.Call(S.While, cond, block, startIndex, block.Range.EndIndex);
		}
		LNode ForStmt(int startIndex)
		{
			Token lit_lpar = default(Token);
			Skip();
			lit_lpar = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			// line 2014
			Down(lit_lpar);
			var init = ExprOpt(true);
			Match((int) TT.Semicolon);
			var cond = ExprOpt(false);
			Match((int) TT.Semicolon);
			var inc = ExprOpt(false);
			// line 2016
			Up();
			// line 2018
			var parts = new VList<LNode> { 
				init, cond, inc, block
			};
			return F.Call(S.For, parts, startIndex, block.Range.EndIndex);
		}
		static readonly HashSet<int> ForEachStmt_set0 = NewSet((int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.At, (int) TT.Backslash, (int) TT.Base, (int) TT.BQString, (int) TT.Checked, (int) TT.Colon, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.Default, (int) TT.Delegate, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.Forward, (int) TT.GT, (int) TT.Id, (int) TT.IncDec, (int) TT.Is, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LEGE, (int) TT.Literal, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.Operator, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.QuickBindSet, (int) TT.Set, (int) TT.Sizeof, (int) TT.Sub, (int) TT.Substitute, (int) TT.This, (int) TT.TypeKeyword, (int) TT.Typeof, (int) TT.Unchecked, (int) TT.XorBits);
		LNode ForEachStmt(int startIndex)
		{
			TokenType la1;
			LNode @var = default(LNode);
			Skip();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			// line 2026
			Down(p);
			// Line 2027: (&(VarIn) VarIn)?
			switch (LA0) {
			case TT.ContextualKeyword:
			case TT.Id:
			case TT.Operator:
			case TT.Substitute:
			case TT.TypeKeyword:
				{
					if (Try_Scan_VarIn(0)) {
						la1 = LA(1);
						if (ForEachStmt_set0.Contains((int) la1))
							@var = VarIn();
					}
				}
				break;
			}
			var expr = ExprStart(false);
			// line 2030
			return Up(F.Call(S.ForEach, @var ?? F.Missing, expr, block, startIndex, block.Range.EndIndex));
		}
		LNode VarIn()
		{
			LNode result = default(LNode);
			var pair = VarDeclStart();
			// line 2037
			result = F.Call(S.Var, pair.A, pair.B, pair.A.Range.StartIndex, pair.B.Range.EndIndex);
			Match((int) TT.In);
			return result;
		}
		bool Try_Scan_VarIn(int lookaheadAmt)
		{
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
		LNode IfStmt(int startIndex)
		{
			TokenType la0;
			// line 2044
			LNode @else = null;
			Skip();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var then = Stmt();
			// Line 2046: greedy(TT.Else Stmt)?
			la0 = LA0;
			if (la0 == TT.Else) {
				Skip();
				@else = Stmt();
			}
			// line 2048
			var cond = SingleExprInside(p, "if (...)");
			if (@else == null)
				 return F.Call(S.If, cond, then, startIndex, then.Range.EndIndex);
			else
				 return F.Call(S.If, cond, then, @else, startIndex, then.Range.EndIndex);
		}
		LNode SwitchStmt(int startIndex)
		{
			Skip();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			// line 2057
			var expr = SingleExprInside(p, "switch (...)");
			return F.Call(S.Switch, expr, block, startIndex, block.Range.EndIndex);
		}
		LNode UsingStmt(int startIndex)
		{
			Skip();
			var p = MatchAny();
			Match((int) TT.RParen);
			var block = Stmt();
			// line 2067
			var expr = SingleExprInside(p, "using (...)");
			return F.Call(S.UsingStmt, expr, block, startIndex, block.Range.EndIndex);
		}
		LNode LockStmt(int startIndex)
		{
			Skip();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			// line 2075
			var expr = SingleExprInside(p, "lock (...)");
			return F.Call(S.Lock, expr, block, startIndex, block.Range.EndIndex);
		}
		LNode FixedStmt(int startIndex)
		{
			Skip();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			// line 2083
			var expr = SingleExprInside(p, "fixed (...)", true);
			return F.Call(S.Fixed, expr, block, startIndex, block.Range.EndIndex);
		}
		LNode TryStmt(int startIndex)
		{
			TokenType la0, la1;
			LNode handler = default(LNode);
			Skip();
			var header = Stmt();
			// line 2092
			var parts = new VList<LNode> { 
				header
			};
			LNode varExpr;
			LNode whenExpr;
			// Line 2095: greedy(TT.Catch (TT.LParen TT.RParen / ) (&{Is($LI, _when)} TT.ContextualKeyword TT.LParen TT.RParen / ) Stmt)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Catch) {
					var kw = MatchAny();
					// Line 2096: (TT.LParen TT.RParen / )
					la0 = LA0;
					if (la0 == TT.LParen) {
						la1 = LA(1);
						if (la1 == TT.RParen) {
							var p = MatchAny();
							Skip();
							// line 2096
							varExpr = SingleExprInside(p, "catch (...)", true);
						} else
							// line 2097
							varExpr = MissingHere();
					} else
						// line 2097
						varExpr = MissingHere();
					// Line 2098: (&{Is($LI, _when)} TT.ContextualKeyword TT.LParen TT.RParen / )
					la0 = LA0;
					if (la0 == TT.ContextualKeyword) {
						if (Is(0, _when)) {
							la1 = LA(1);
							if (la1 == TT.LParen) {
								Skip();
								var c = MatchAny();
								Match((int) TT.RParen);
								// line 2099
								whenExpr = SingleExprInside(c, "when (...)");
							} else
								// line 2100
								whenExpr = MissingHere();
						} else
							// line 2100
							whenExpr = MissingHere();
					} else
						// line 2100
						whenExpr = MissingHere();
					handler = Stmt();
					// line 2102
					parts.Add(F.Call(S.Catch, varExpr, whenExpr, handler, kw.StartIndex, handler.Range.EndIndex));
				} else
					break;
			}
			// Line 2105: greedy(TT.Finally Stmt)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Finally) {
					var kw = MatchAny();
					handler = Stmt();
					// line 2106
					parts.Add(F.Call(S.Finally, handler, kw.StartIndex, handler.Range.EndIndex));
				} else
					break;
			}
			// line 2109
			var result = F.Call(S.Try, parts, startIndex, parts.Last.Range.EndIndex);
			if (parts.Count == 1) {
				Error(result, "'try': At least one 'catch' or 'finally' clause is required");
			}
			return result;
		}
		LNode ExprOrNull(bool allowUnassignedVarDecl = false)
		{
			TokenType la0;
			LNode result = default(LNode);
			// Line 2122: ((EOF|TT.Comma|TT.Semicolon) =>  / ExprStart)
			la0 = LA0;
			if (la0 == EOF || la0 == TT.Comma || la0 == TT.Semicolon)
				// line 2122
				result = null;
			else
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
		void ExprList(ref VList<LNode> list, bool allowTrailingComma = false, bool allowUnassignedVarDecl = false)
		{
			TokenType la0, la1;
			// Line 2136: nongreedy(ExprOpt (TT.Comma &{allowTrailingComma} EOF / TT.Comma ExprOpt)*)?
			la0 = LA0;
			if (la0 == EOF)
				;
			else {
				list.Add(ExprOpt(allowUnassignedVarDecl));
				// Line 2137: (TT.Comma &{allowTrailingComma} EOF / TT.Comma ExprOpt)*
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
						} else
							goto match2;
					} else if (la0 == EOF)
						break;
					else {
						// line 2139
						Error("'{0}': Syntax error in expression list", CurrentTokenText());
						// Line 2139: (~(EOF|TT.Comma))*
						for (;;) {
							la0 = LA0;
							if (!(la0 == (TokenType) EOF || la0 == TT.Comma))
								Skip();
							else
								break;
						}
					}
					continue;
				match2:
					{
						Skip();
						list.Add(ExprOpt(allowUnassignedVarDecl));
					}
				}
			}
			Skip();
		}
		void ArgList(ref VList<LNode> list)
		{
			TokenType la0, la1;
			// Line 2145: nongreedy(ExprOpt (TT.Comma ExprOpt)*)?
			do {
				la0 = LA0;
				if (la0 == EOF) {
					la1 = LA(1);
					if (la1 == (TokenType) EOF)
						;
					else
						goto matchExprOpt;
				} else
					goto matchExprOpt;
				break;
			matchExprOpt:
				{
					list.Add(ExprOpt(true));
					// Line 2146: (TT.Comma ExprOpt)*
					for (;;) {
						la0 = LA0;
						if (la0 == TT.Comma) {
							Skip();
							list.Add(ExprOpt(true));
						} else if (la0 == EOF)
							break;
						else {
							// line 2147
							Error("Syntax error in argument list");
							// Line 2147: (~(EOF|TT.Comma))*
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
			} while (false);
			Skip();
		}
		LNode InitializerExpr()
		{
			TokenType la0, la1, la2;
			LNode result = default(LNode);
			// Line 2154: ( TT.LBrace TT.RBrace / TT.LBrack TT.RBrack TT.Set ExprStart / ExprOpt )
			la0 = LA0;
			if (la0 == TT.LBrace) {
				la1 = LA(1);
				if (la1 == TT.RBrace) {
					la2 = LA(2);
					if (la2 == (TokenType) EOF || la2 == TT.Comma) {
						var lb = MatchAny();
						var rb = MatchAny();
						// line 2156
						var exprs = InitializerListInside(lb);
						result = F.Call(S.Braces, exprs, lb.StartIndex, rb.EndIndex).SetBaseStyle(NodeStyle.OldStyle);
					} else
						result = ExprOpt(false);
				} else
					result = ExprOpt(false);
			} else if (la0 == TT.LBrack) {
				la1 = LA(1);
				if (la1 == TT.RBrack) {
					la2 = LA(2);
					if (la2 == TT.Set) {
						var lb = MatchAny();
						var rb = MatchAny();
						Skip();
						var e = ExprStart(false);
						// line 2160
						result = F.Call(S.InitializerAssignment, ExprListInside(lb).Add(e), lb.StartIndex, e.Range.EndIndex);
					} else
						result = ExprOpt(false);
				} else
					result = ExprOpt(false);
			} else
				result = ExprOpt(false);
			return result;
		}
		void InitializerList(ref VList<LNode> list)
		{
			TokenType la0, la1;
			// Line 2167: nongreedy(InitializerExpr (TT.Comma EOF / TT.Comma InitializerExpr)*)?
			do {
				la0 = LA0;
				if (la0 == EOF) {
					la1 = LA(1);
					if (la1 == (TokenType) EOF)
						;
					else
						goto matchInitializerExpr;
				} else
					goto matchInitializerExpr;
				break;
			matchInitializerExpr:
				{
					list.Add(InitializerExpr());
					// Line 2168: (TT.Comma EOF / TT.Comma InitializerExpr)*
					for (;;) {
						la0 = LA0;
						if (la0 == TT.Comma) {
							la1 = LA(1);
							if (la1 == EOF) {
								Skip();
								Skip();
							} else {
								Skip();
								list.Add(InitializerExpr());
							}
						} else if (la0 == EOF)
							break;
						else {
							// line 2170
							Error("Syntax error in initializer list");
							// Line 2170: (~(EOF|TT.Comma))*
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
			} while (false);
			Skip();
		}
		void StmtList(ref VList<LNode> list)
		{
			TokenType la0;
			// Line 2175: (~(EOF) => Stmt)*
			for (;;) {
				la0 = LA0;
				if (la0 != (TokenType) EOF)
					list.Add(Stmt());
				else
					break;
			}
			Skip();
		}
		static readonly HashSet<int> TypeSuffixOpt_Test0_set0 = NewSet((int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.Forward, (int) TT.Id, (int) TT.IncDec, (int) TT.LBrace, (int) TT.Literal, (int) TT.LParen, (int) TT.Mul, (int) TT.New, (int) TT.Not, (int) TT.NotBits, (int) TT.Sub, (int) TT.Substitute, (int) TT.TypeKeyword);
		private bool Try_TypeSuffixOpt_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return TypeSuffixOpt_Test0();
		}
		private bool TypeSuffixOpt_Test0()
		{
			// Line 246: ((TT.Add|TT.AndBits|TT.At|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.New|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | IdNotLinqKeyword)
			switch (LA0) {
			case TT.Add:
			case TT.AndBits:
			case TT.At:
			case TT.Forward:
			case TT.Id:
			case TT.IncDec:
			case TT.LBrace:
			case TT.Literal:
			case TT.LParen:
			case TT.Mul:
			case TT.New:
			case TT.Not:
			case TT.NotBits:
			case TT.Sub:
			case TT.Substitute:
			case TT.TypeKeyword:
				if (!TryMatch(TypeSuffixOpt_Test0_set0))
					return false;
				break;
			default:
				if (!Scan_IdNotLinqKeyword())
					return false;
				break;
			}
			return true;
		}
		private bool Try_ExprInParensAuto_Test0(int lookaheadAmt)
		{
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
		static readonly HashSet<int> FinishPrimaryExpr_Test0_set0 = NewSet((int) TT.ContextualKeyword, (int) TT.Id);
		private bool Try_FinishPrimaryExpr_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return FinishPrimaryExpr_Test0();
		}
		private bool FinishPrimaryExpr_Test0()
		{
			if (!Scan_TParams())
				return false;
			if (!TryMatchExcept(FinishPrimaryExpr_Test0_set0))
				return false;
			return true;
		}
		static readonly HashSet<int> PrefixExpr_Test0_set0 = NewSet((int) TT.Add, (int) TT.AndBits, (int) TT.BQString, (int) TT.Dot, (int) TT.Mul, (int) TT.Sub);
		private bool Try_PrefixExpr_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return PrefixExpr_Test0();
		}
		private bool PrefixExpr_Test0()
		{
			// Line 613: ((TT.Add|TT.AndBits|TT.BQString|TT.Dot|TT.Mul|TT.Sub) | TT.IncDec TT.LParen)
			switch (LA0) {
			case TT.Add:
			case TT.AndBits:
			case TT.BQString:
			case TT.Dot:
			case TT.Mul:
			case TT.Sub:
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
		private bool Try_IdStmt_Test0(int lookaheadAmt)
		{
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
		private bool Try_OtherStmt_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return OtherStmt_Test0();
		}
		private bool OtherStmt_Test0()
		{
			if (!TryMatch((int) TT.NotBits))
				return false;
			if (!TryMatch((int) TT.ContextualKeyword, (int) TT.Id, (int) TT.This))
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
		private bool Try_Constructor_Test0(int lookaheadAmt)
		{
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
		private bool Try_Constructor_Test1(int lookaheadAmt)
		{
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
		private bool Try_Constructor_Test2(int lookaheadAmt)
		{
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
		private bool Try_BlockCallStmt_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return BlockCallStmt_Test0();
		}
		private bool BlockCallStmt_Test0()
		{
			TokenType la0;
			// Line 1921: ( TT.LParen TT.RParen (TT.LBrace TT.RBrace | TT.Id) | TT.LBrace TT.RBrace | TT.Forward )
			la0 = LA0;
			if (la0 == TT.LParen) {
				if (!TryMatch((int) TT.LParen))
					return false;
				if (!TryMatch((int) TT.RParen))
					return false;
				// Line 1921: (TT.LBrace TT.RBrace | TT.Id)
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
