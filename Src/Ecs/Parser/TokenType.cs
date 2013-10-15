using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Syntax.Lexing;
using System.Diagnostics;

namespace Ecs.Parser
{
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
		EOF       = 0,
		Spaces    = TokenKind.Spaces + 1,
		Newline   = TokenKind.Spaces + 2,
		SLComment = TokenKind.Comment,
		MLComment = TokenKind.Comment + 1,
		Shebang   = TokenKind.Comment + 2,
		Id        = TokenKind.Id,
		@base     = TokenKind.Id + 1,
		@this     = TokenKind.Id + 2,
		Number    = TokenKind.Number,
		String    = TokenKind.String,
		SQString  = TokenKind.String + 1,
		OtherLit  = TokenKind.OtherLit,
		Symbol    = TokenKind.OtherLit + 1,
		Comma     = TokenKind.Separator,
		Semicolon = TokenKind.Separator + 1,
		LParen    = TokenKind.LParen,
		RParen    = TokenKind.RParen,
		LBrack    = TokenKind.LBrack,
		RBrack    = TokenKind.RBrack,
		LBrace    = TokenKind.LBrace,
		RBrace    = TokenKind.RBrace,
		AttrKeyword = TokenKind.AttrKeyword,
		TypeKeyword = TokenKind.TypeKeyword,
		
		@break    = TokenKind.OtherKeyword + 1,
		@case     = TokenKind.OtherKeyword + 2,
		@checked  = TokenKind.OtherKeyword + 3,
		@class    = TokenKind.OtherKeyword + 4,
		@continue = TokenKind.OtherKeyword + 5,
		@default  = TokenKind.OtherKeyword + 6,
		@delegate = TokenKind.OtherKeyword + 7,
		@do       = TokenKind.OtherKeyword + 8,
		@enum     = TokenKind.OtherKeyword + 9,
		@event    = TokenKind.OtherKeyword + 10,
		@fixed    = TokenKind.OtherKeyword + 11,
		@for      = TokenKind.OtherKeyword + 12,
		@foreach  = TokenKind.OtherKeyword + 13,
		@goto     = TokenKind.OtherKeyword + 14,
		@if       = TokenKind.OtherKeyword + 15,
		@interface= TokenKind.OtherKeyword + 16,
		@lock     = TokenKind.OtherKeyword + 17,
		@namespace= TokenKind.OtherKeyword + 18,
		@return   = TokenKind.OtherKeyword + 19,
		@struct   = TokenKind.OtherKeyword + 20,
		@switch   = TokenKind.OtherKeyword + 21,
		@throw    = TokenKind.OtherKeyword + 22,
		@try      = TokenKind.OtherKeyword + 23,
		@unchecked= TokenKind.OtherKeyword + 24,
		@using    = TokenKind.OtherKeyword + 25,
		@while    = TokenKind.OtherKeyword + 26,

		@operator = TokenKind.OtherKeyword + 32,
		@sizeof   = TokenKind.OtherKeyword + 33,
		@typeof   = TokenKind.OtherKeyword + 34,

		@else     = TokenKind.OtherKeyword + 40,
		@catch    = TokenKind.OtherKeyword + 41,
		@finally  = TokenKind.OtherKeyword + 42,

		@in       = TokenKind.OtherKeyword + 48,
		@as       = TokenKind.OtherKeyword + 49,
		@is       = TokenKind.OtherKeyword + 50,

		@new       = TokenKind.OtherKeyword + 56,
		@out       = TokenKind.OtherKeyword + 57,
		@stackalloc= TokenKind.OtherKeyword + 58,

		PPif       = TokenKind.Other + 64,
		PPelse     = TokenKind.Other + 65,
		PPelif     = TokenKind.Other + 66,
		PPendif    = TokenKind.Other + 67,
		PPdefine   = TokenKind.Other + 68,
		PPundef    = TokenKind.Other + 69,
		PPwarning  = TokenKind.Other + 70,
		PPerror    = TokenKind.Other + 71,
		PPnote     = TokenKind.Other + 72,
		PPline     = TokenKind.Other + 73,
		PPregion   = TokenKind.Other + 74,
		PPendregion= TokenKind.Other + 75,
		PPpragma   = TokenKind.Other + 76,

		Dot          = TokenKind.Dot,     // .
		PtrArrow     = TokenKind.Dot + 1, // ->
		ColonColon   = TokenKind.Dot + 2, // ::
		NullDot      = TokenKind.Dot + 3, // ?.

		Set         = TokenKind.Assignment, // =
		CompoundSet = TokenKind.Assignment, // +=, *=, >>=, :=, etc.

		// Operators: Different operators that are used in the same way and have
		// the same precence may be grouped into a single TokenType. There is
		// no token type for >> or << because these are formed from two > or <
		// tokens.
		Colon     = TokenKind.Operator,     // :
		At        = TokenKind.Operator + 1, // @
		BQString  = TokenKind.Operator + 2, // `...`
		Backslash = TokenKind.Operator + 4, // \
		BackslashOp = TokenKind.Operator + 4, // \
		MulDiv    = TokenKind.Operator + 5, // * / %
		Power     = TokenKind.Operator + 6, // **
		Add       = TokenKind.Operator + 7, // +
		Sub       = TokenKind.Operator + 8, // -
		IncDec    = TokenKind.Operator + 9, // ++ --
		AndOr     = TokenKind.Operator + 10, // && || ^^
		Not       = TokenKind.Operator + 11, // !
		AndBits   = TokenKind.Operator + 12, // &
		OrXorBits = TokenKind.Operator + 13, // | ^
		NotBits   = TokenKind.Operator + 14, // ~
		EqNeq     = TokenKind.Operator + 15, // == !=
		LT        = TokenKind.Operator + 16, // <
		GT        = TokenKind.Operator + 17, // >
		LEGE      = TokenKind.Operator + 18, // <= >=
		DotDot       = TokenKind.Operator + 22, // ..
		QuestionMark = TokenKind.Operator + 24, // ?
		NullCoalesce = TokenKind.Operator + 25, // ??
		QuickBind    = TokenKind.Operator + 26, // =:
		Forward      = TokenKind.Operator + 27, // ==>
		Substitute   = TokenKind.Operator + 28, // $
		LambdaArrow  = TokenKind.Operator + 29, // =>
		
		Indent = TokenKind.LBrace + 1,
		Dedent = TokenKind.RBrace + 1,
	}

	public static class TokenExt
	{
		[DebuggerStepThrough]
		public static TokenType Type(this Token t) { return (TokenType)t.TypeInt; }
	}
}
