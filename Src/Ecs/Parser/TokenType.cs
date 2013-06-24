using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
}
