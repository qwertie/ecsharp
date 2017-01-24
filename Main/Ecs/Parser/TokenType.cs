using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Syntax.Lexing;
using System.Diagnostics;

namespace Loyc.Ecs.Parser
{
	using TT = TokenType;
	using Loyc;

	public enum TokenType
	{
		EOF       = 0,
		Spaces    = TokenKind.Spaces + 1, // No longer used except to represent Byte Order Mark
		Newline   = TokenKind.Spaces + 2,
		SLComment = TokenKind.Comment,
		MLComment = TokenKind.Comment + 1,
		Shebang   = TokenKind.Comment + 2,
		Id        = TokenKind.Id,
		// var, dynamic, trait, alias, where, assembly, module.
		// Does not include partial, because any Id can be a word attribute.
		// Does not include where, which is LinqKeyword although used outside LINQ, too
		ContextualKeyword = TokenKind.Id + 1,
		// The 13 LINQ keywords:
		// where, from, select, let, join, on, equals, into, orderby, ascending, descending, group, by
		LinqKeyword = TokenKind.Id + 2,
		Base      = TokenKind.Id + 3,
		This      = TokenKind.Id + 4,
		Literal   = TokenKind.Literal,
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
		
		Break    = TokenKind.OtherKeyword + 1,
		Case     = TokenKind.OtherKeyword + 2,
		Checked  = TokenKind.OtherKeyword + 3,
		Class    = TokenKind.OtherKeyword + 4,
		Continue = TokenKind.OtherKeyword + 5,
		Default  = TokenKind.OtherKeyword + 6,
		Delegate = TokenKind.OtherKeyword + 7,
		Do       = TokenKind.OtherKeyword + 8,
		Enum     = TokenKind.OtherKeyword + 9,
		Event    = TokenKind.OtherKeyword + 10,
		Fixed    = TokenKind.OtherKeyword + 11,
		For      = TokenKind.OtherKeyword + 12,
		Foreach  = TokenKind.OtherKeyword + 13,
		Goto     = TokenKind.OtherKeyword + 14,
		If       = TokenKind.OtherKeyword + 15,
		Interface= TokenKind.OtherKeyword + 16,
		Lock     = TokenKind.OtherKeyword + 17,
		Namespace= TokenKind.OtherKeyword + 18,
		Return   = TokenKind.OtherKeyword + 19,
		Struct   = TokenKind.OtherKeyword + 20,
		Switch   = TokenKind.OtherKeyword + 21,
		Throw    = TokenKind.OtherKeyword + 22,
		Try      = TokenKind.OtherKeyword + 23,
		Unchecked= TokenKind.OtherKeyword + 24,
		Using    = TokenKind.OtherKeyword + 25,
		While    = TokenKind.OtherKeyword + 26,

		Operator = TokenKind.OtherKeyword + 32,
		Sizeof   = TokenKind.OtherKeyword + 33,
		Typeof   = TokenKind.OtherKeyword + 34,

		Else     = TokenKind.OtherKeyword + 40,
		Catch    = TokenKind.OtherKeyword + 41,
		Finally  = TokenKind.OtherKeyword + 42,

		In       = TokenKind.OtherKeyword + 48,
		As       = TokenKind.OtherKeyword + 49,
		Is       = TokenKind.OtherKeyword + 50,

		New       = TokenKind.OtherKeyword + 56,
		Out       = TokenKind.OtherKeyword + 57,
		Stackalloc= TokenKind.OtherKeyword + 58,

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
		PPignored  = TokenKind.Other + 77, // covers one or more lines ignored by #if/#elif/#else.

		Dot          = TokenKind.Dot,     // .
		PtrArrow     = TokenKind.Dot + 1, // ->
		ColonColon   = TokenKind.Dot + 2, // ::
		NullDot      = TokenKind.Dot + 3, // ?.

		Set         = TokenKind.Assignment, // =
		CompoundSet = TokenKind.Assignment + 1, // +=, *=, >>=, etc.
		QuickBindSet = TokenKind.Assignment + 2, // :=

		// Operators: Different operators that are used in the same way and have
		// the same precence may be grouped into a single TokenType. There is
		// no token type for >> or << because these are formed from two > or <
		// tokens.
		Colon     = TokenKind.Operator,     // :
		At        = TokenKind.Operator + 1, // @
		BQString  = TokenKind.Operator + 2, // `...`
		Backslash = TokenKind.Operator + 4, // \
		Mul       = TokenKind.Operator + 5, // *
		DivMod       = TokenKind.Operator + 6, // / %
		Power     = TokenKind.Operator + 7, // **   (can also represent double-deref: (**x))
		Add       = TokenKind.Operator + 8, // +
		Sub       = TokenKind.Operator + 9, // -
		IncDec    = TokenKind.Operator + 10, // ++ --
		And       = TokenKind.Operator + 11, // &&
		OrXor     = TokenKind.Operator + 12, // || ^^
		Not       = TokenKind.Operator + 14, // !
		AndBits   = TokenKind.Operator + 15, // &
		OrBits    = TokenKind.Operator + 16, // |
		XorBits   = TokenKind.Operator + 17, // ^
		NotBits   = TokenKind.Operator + 18, // ~
		EqNeq     = TokenKind.Operator + 19, // == !=
		LT        = TokenKind.Operator + 20, // <
		GT        = TokenKind.Operator + 21, // >
		LEGE      = TokenKind.Operator + 22, // <= >=
		DotDot       = TokenKind.Operator + 23, // ..
		QuestionMark = TokenKind.Operator + 24, // ?
		NullCoalesce = TokenKind.Operator + 25, // ??
		QuickBind    = TokenKind.Operator + 26, // =:
		Forward      = TokenKind.Operator + 27, // ==>
		Substitute   = TokenKind.Operator + 28, // $
		LambdaArrow  = TokenKind.Operator + 29, // =>
		
		Unknown = TokenKind.Other, // invalid input
		//Indent = TokenKind.LBrace + 1,
		//Dedent = TokenKind.RBrace + 1,
	}

	/// <summary>Provides the <c>Type()</c> extension method required by 
	/// <see cref="Token"/> and the ToString(Token) method to express an EC# token
	/// as a string, for tokens that contain sufficient information to do so.</summary>
	public static class TokenExt
	{
		/// <summary>Converts <c>t.TypeInt</c> to <see cref="TokenType"/>.</summary>
		[DebuggerStepThrough]
		public static TokenType Type(this Token t) { return (TokenType)t.TypeInt; }

		/// <summary>Expresses an EC# token as a string.</summary>
		/// <remarks>Note that some Tokens do not contain enough information to
		/// reconstruct a useful token string, e.g. comment tokens do not store the 
		/// comment but merely contain the location of the comment in the source code.
		/// For performance reasons, a <see cref="Token"/> does not have a reference 
		/// to its source file, so this method cannot return the original string.
		/// <para/>
		/// The results are undefined if the token was not produced by <see cref="EcsLexer"/>.
		/// </remarks>
		public static string ToString(Token t)
		{
			StringBuilder sb = new StringBuilder();
			if (t.Kind == TokenKind.Operator || t.Kind == TokenKind.Assignment || t.Kind == TokenKind.Dot)
			{
				if (t.Type() == TT.BQString)
					return EcsNodePrinter.PrintString((t.Value ?? "").ToString(), '`', false);
				string value = (t.Value ?? "(null)").ToString();
				return value;
			}
			switch (t.Type()) {
				case TT.EOF: return "/*EOF*/";
				case TT.Spaces: return " ";
				case TT.Newline: return "\n";
				case TT.SLComment: return "//\n";
				case TT.MLComment: return "/**/";
				case TT.Shebang: return "#!" + (t.Value ?? "").ToString() + "\n";
				case TT.Id:
				case TT.ContextualKeyword:
					return EcsNodePrinter.PrintId(t.Value as Symbol ?? GSymbol.Empty);
				case TT.Base: return "base";
				case TT.This: return "this";
				case TT.Literal:
					return EcsNodePrinter.PrintLiteral(t.Value, t.Style);
				case TT.Comma: return ",";
				case TT.Semicolon: return ";";
				case TT.LParen: return "(";
				case TT.RParen: return ")";
				case TT.LBrack: return "[";
				case TT.RBrack: return "]";
				case TT.LBrace: return "{";
				case TT.RBrace: return "}";
				case TT.AttrKeyword:
					string value = (t.Value ?? "(null)").ToString();
					return value;
				case TT.TypeKeyword:
					Symbol valueSym = (t.Value as Symbol) ?? GSymbol.Empty;
					string result;
					if (EcsNodePrinter.TypeKeywords.TryGetValue(valueSym, out result))
						return result;
					else {
						Debug.Fail("Unexpected value for " + t.Type());
						return (t.Value ?? "(null)").ToString();
					}
				case TT.Break:     return "break";    
				case TT.Case:    	return "case";     
				case TT.Checked:	return "checked";  
				case TT.Class:		return "class";    
				case TT.Continue:	return "continue"; 
				case TT.Default:	return "default";  
				case TT.Delegate:	return "delegate"; 
				case TT.Do:		return "do";       
				case TT.Enum:		return "enum";     
				case TT.Event:		return "event";    
				case TT.Fixed:		return "fixed";    
				case TT.For:		return "for";      
				case TT.Foreach:	return "foreach";  
				case TT.Goto:		return "goto";     
				case TT.If:		return "if";       
				case TT.Interface:	return "interface";
				case TT.Lock:		return "lock";     
				case TT.Namespace:	return "namespace";
				case TT.Return:	return "return";   
				case TT.Struct:	return "struct";   
				case TT.Switch:	return "switch";   
				case TT.Throw:		return "throw";    
				case TT.Try:		return "try";      
				case TT.Unchecked:	return "unchecked";
				case TT.Using:		return "using";    
				case TT.While:		return "while";    
										   
				case TT.Operator:  return "operator"; 
				case TT.Sizeof:    return "sizeof";   
				case TT.Typeof:    return "typeof";   
				case TT.Else:	    return "else";     
				case TT.Catch:     return "catch";       
				case TT.Finally:  	return "finally";  
				case TT.In:       	return "in";       
				case TT.As:       	return "as";       
				case TT.Is:       	return "is";       
				case TT.New:      	return "new";      
				case TT.Out:      	return "out";
				case TT.Stackalloc:return "stackalloc";

				case TT.PPif       : return "#if";
				case TT.PPelse     : return "#else";
				case TT.PPelif     : return "#elif";
				case TT.PPendif    : return "#endif";
				case TT.PPdefine   : return "#define";
				case TT.PPundef    : return "#undef";
				case TT.PPwarning  : return "#warning" + t.Value;
				case TT.PPerror    : return "#error" + t.Value;
				case TT.PPnote     : return "#note" + t.Value;
				case TT.PPline     : return "#line";
				case TT.PPregion   : return "#region" + t.Value;
				case TT.PPendregion: return "#endregion";
				case TT.PPpragma   : return "#pragma";
				case TT.PPignored  : return (t.Value ?? "").ToString();
				default:
					return string.Format("@`unknown token 0x{0:X4}`", t.TypeInt);
			}
		}
	}
}
