using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.LLParserGenerator;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc;

namespace Loyc.Syntax.Les
{
	using TT = TokenType;
	using S = CodeSymbols;
	using P = LesPrecedence;
	using System.Diagnostics;
	using Loyc.Utilities;

	public partial class LesParser : BaseParser<Token, TokenType>
	{
		protected TokenTree _tokens;
		protected IMessageSink _messages;
		protected LNodeFactory F;
		static readonly Symbol SoftError = MessageSink.SoftError;
		static readonly Symbol Warning = MessageSink.Warning;

		/// <summary>Parses LES text into a sequence of LNodes, one per 
		/// top-level statement in the input.</summary>
		public static IEnumerator<LNode> Parse(string text, IMessageSink messages)
		{
			return Parse(new StringCharSourceFile(text, "[Immediate]"), messages);
		}
		/// <summary>Parses LES text into a sequence of LNodes, one per 
		/// top-level statement in the input.</summary>
		public static IEnumerator<LNode> Parse(StringCharSourceFile file, IMessageSink messages)
		{
			var lexer = new LesLexer(file, (index, message) => messages.Write(SoftError, file.IndexToLine(index), message));
			var treeLexer = new TokensToTree(lexer, true);
			return Parse(treeLexer.Buffered(), file, messages);
		}
		/// <summary>Parses a token tree into a sequence of LNodes, one per top-
		/// level statement in the input.</summary>
		public static IEnumerator<LNode> Parse(TokenTree tokenTree, IMessageSink messages)
		{
			return Parse(tokenTree, tokenTree.File, messages);
		}
		/// <summary>Parses a token tree into a sequence of LNodes, one per top-
		/// level statement in the input.</summary>
		public static IEnumerator<LNode> Parse(IListSource<Token> tokenTree, ISourceFile file, IMessageSink messages)
		{
			throw new NotImplementedException();
		}

		public LesParser(TokenTree tokens, IMessageSink messages)
		{
			_tokens = tokens; F = new LNodeFactory(_tokens.File);
			MissingExpr = MissingExpr ?? F.Id(S.Missing);
		}

		protected sealed override TT EOF
		{
			get { return TT.EOF; }
		}
		protected sealed override TT LA(int i)
		{
			bool fail = false;
			return _tokens.TryGet(InputPosition + i, ref fail).Type;
		}
		protected sealed override Token LT(int i)
		{
			bool fail = false;
			return _tokens.TryGet(InputPosition + i, ref fail);
		}
		protected override TT ToLA(Token lt)
		{
			return lt.Type;
		}
		protected override bool LT0Equals(TT b)
		{
			return LT0.Type == b;
		}
		protected override void Error(int inputPosition, string message)
		{
			SourcePos pos = _tokens.File.IndexToLine(inputPosition);
			_messages.Write(MessageSink.Error, pos, message);
		}

		static readonly int MinPrec = Precedence.MinValue.Lo;
		public static readonly Precedence StartStmt = new Precedence(MinPrec, MinPrec, MinPrec);
		static LNode MissingExpr;

		Stack<Pair<TokenTree, int>> _parents = new Stack<Pair<TokenTree,int>>();

		bool Down(int li)
		{
			return Down(LT(li).Children);
		}
		bool Down(TokenTree children)
		{
			if (children != null) {
				_parents.Push(Pair.Create(_tokens, InputPosition));
				_tokens = children;
				InputPosition = 0;
				return true;
			}
			return false;
		}
		T Up<T>(T value)
		{
			Up();
			return value;
		}
		void Up()
		{
			Debug.Assert(_parents.Count > 0);
			var pair = _parents.Pop();
			_tokens = pair.A;
			InputPosition = pair.B;
		}

		RWList<LNode> AppendExprsInside(Token group, RWList<LNode> list)
		{
			if (Down(group.Children)) {
				ExprList(ref list);
				return Up(list);
			}
			return list;
		}
		private RWList<LNode> ExprListInside(Token t)
		{
			return AppendExprsInside(t, new RWList<LNode>());
		}
		private LNode InterpretBraces(Token t)
		{
			RWList<LNode> list = new RWList<LNode>();
			if (Down(t.Children)) {
				StmtList(ref list);
				Up();
			}
			return F.Braces(list.ToRVList());
		}
		private LNode InterpretParens(Token t)
		{
			return F.Call(S.Missing, ExprListInside(t).ToRVList());
		}
		
		// All the keys are Symbols, but we use object as the key type to avoid casting Token.Value
		Dictionary<object, Precedence> _prefixPrecedence = new Dictionary<object, Precedence>() {
			{ S.Substitute,  P.Substitute }, // #$
			{ S.Dot,         P.Substitute }, // hmm, I might repurpose '.' with lower precedence to remove the spacing rule
			{ S.Colon,       P.Substitute }, // #:
			{ S.NotBits,     P.Prefix     }, // #~
			{ S.Not,         P.Prefix     }, // #!
			{ S.Mod,         P.Prefix     }, // #%
			{ S.XorBits,     P.Prefix     }, // #^
			{ S._AddressOf,  P.Prefix     }, // #&
			{ S._Dereference,P.Prefix     }, // #*
			{ S._UnaryPlus,  P.Prefix     }, // #+
			{ S._Negate,     P.Prefix     }, // #-
			{ S.DotDot,      P.PrefixDots }, // #..
			{ S.OrBits,      P.PrefixOr   }, // #|
			{ S.Div,         P.Reserved   }, // #/
			{ S.Backslash,   P.Reserved   }, // #\
			{ S.LT,          P.Reserved   }, // #<
			{ S.GT,          P.Reserved   }, // #>
		};
		Dictionary<object, Precedence> _suffixPrecedence = new Dictionary<object, Precedence>() {
			{ S.PreInc,     P.Primary   }, // #++, never mind that it's called "pre"inc
			{ S.PreDec,     P.Primary   }, // #--
		};
		Dictionary<object, Precedence> _infixPrecedence = new Dictionary<object, Precedence>() { 
			{ S.Dot,        P.Primary   }, // #.
			{ S.QuickBind,  P.Primary   }, // #=:
			{ S.Not,        P.Primary   }, // #!
			{ S.NullDot,    P.NullDot   }, // #?.
			{ S.ColonColon, P.NullDot   }, // #::
			{ S.DoubleBang, P.DoubleBang}, // #!!
			{ S.Exp,        P.Power     }, // #**
			{ S.Mul,        P.Multiply  }, // #*
			{ S.Div,        P.Multiply  }, // #/
			{ S.Mod,        P.Multiply  }, // #%
			{ S.Backslash,  P.Multiply  }, // #\
			{ S.Shr,        P.Multiply  }, // #>>
			{ S.Shl,        P.Multiply  }, // #<<
			{ S.Add,        P.Add       }, // #+
			{ S.Sub,        P.Add       }, // #-
			{ S._RightArrow,P.Arrow     }, // #->
			{ S.LeftArrow,  P.Arrow     }, // #<-
			{ S.AndBits,    P.AndBits   }, // #&
			{ S.OrBits,     P.OrBits    }, // #|
			{ S.XorBits,    P.OrBits    }, // #^
			{ S.NullCoalesce,P.OrIfNull }, // #??
			{ S.DotDot,     P.Range     }, // #..
			{ S.GT,         P.Compare   }, // #>
			{ S.LT,         P.Compare   }, // #<
			{ S.LE,         P.Compare   }, // #<=
			{ S.GE,         P.Compare   }, // #>=
			{ S.Eq,         P.Compare   }, // #==
			{ S.Neq,        P.Compare   }, // #!=
			{ S.And,        P.And       }, // #&&
			{ S.Or,         P.Or        }, // #||
			{ S.Xor,        P.Or        }, // #^^
			{ S.QuestionMark,P.IfElse   }, // #?
			{ S.Colon,      P.Reserved  }, // #:
			{ S.Set,        P.Assign    }, // #:
			{ S.Lambda,     P.Lambda    }, // #=>
			{ S.XorBits,    P.Reserved  }, // #~
		};

		Precedence FindPrecedence(Dictionary<object,Precedence> table, object symbol, Precedence @default)
		{
			// You can see the official rules in the LesPrecedence documentation.
			// Rule 1 is covered by the pre-populated contents of the table, and
			// the pre-populated table helps interpret rules 3-4 also.
			Precedence prec;
			if (table.TryGetValue(symbol, out prec))
				return prec;
			
			string sym = symbol.ToString();
			char first = sym[0], last = sym[sym.Length - 1];
			// All one-character operators should be found in the table
			Debug.Assert(sym.Length > (first == '#' ? 2 : 1));

			if (table == _infixPrecedence && last == '=')
				return table[symbol] = P.Assign;
			if (first == '#')
				first = sym[1];
			
			var twoCharOp = GSymbol.Get("#" + first + last);
			if (table.TryGetValue(twoCharOp, out prec))
				return table[symbol] = prec;

			var oneCharOp = GSymbol.Get("#" + last);
			if (table.TryGetValue(oneCharOp, out prec))
				return table[symbol] = prec;

			return table[symbol] = @default;
		}
		private Precedence PrefixPrecedenceOf(Token t)
		{
			return FindPrecedence(_prefixPrecedence, t.Value, P.Prefix);
		}
		private Precedence SuffixPrecedenceOf(Token t)
		{
			return FindPrecedence(_suffixPrecedence, t.Value, P.Suffix2);
		}
		private Precedence InfixPrecedenceOf(Token t)
		{
			return FindPrecedence(_infixPrecedence, t.Value, P.Reserved);
		}
		private Symbol ToSuffixOpName(Symbol symbol)
		{
			return GSymbol.Get(@"\" + symbol.ToString());
		}
		private LNode MakeSuperExpr(LNode e, LNode primary, RVList<LNode> otherExprs)
		{
			if (primary == null)
				return e; // an error should have been printed already

			if (otherExprs.IsEmpty)
				return e;
			if (e == primary) {
				Debug.Assert(e.BaseStyle == NodeStyle.Special);
				return e.WithArgs(e.Args.AddRange(otherExprs));
			} else {
				Debug.Assert(e.BaseStyle != NodeStyle.Special);
				Debug.Assert(e != null && e.IsCall && e.ArgCount > 0);
				int c = e.ArgCount-1;
				LNode ce = MakeSuperExpr(e.Args[c], primary, otherExprs);
				return e.WithArgChanged(c, ce);
			}
		}
		IEnumerator<LNode> StmtsUntilEnd()
		{
			LNode next = SuperExprOpt();
			for (;;) {
				if (LA0 == TT.Semicolon) {
					yield return next;
					Skip();
					next = SuperExprOpt();
				} else
					break;
			}
			if (next != (object)MissingExpr)
				yield return next;
		}
	}
}