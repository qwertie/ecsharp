using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections;
using S = Loyc.Syntax.CodeSymbols;
using System.Diagnostics;
using Loyc.Utilities;

namespace Loyc.LLParserGenerator
{
	/// <summary>Bootstrapper. Generates source code for the EC# lexer.</summary>
	public class LesLexerGenerator : LlpgHelpers
	{
		public static Pred SendValueTo(string funcName, Pred pred)
		{
			pred.ResultSaver = res => {
				// $funcName($res)
				return F.Call(GSymbol.Get(funcName), res);
			};
			return pred;
		}
		public LNode Call(string funcName)
		{
			return F.Call(GSymbol.Get(funcName));
		}

		public Rule[] NumberParts(out Rule number)
		{
			// Helper rules for Number
			Rule DecDigits = Rule("DecDigits", Plus(Set("[0-9]")) + Star('_' + Plus(Set("[0-9]"))), Private);
			Rule BinDigits = Rule("BinDigits", Plus(Set("[0-1]")) + Star('_' + Plus(Set("[0-1]"))), Private);
			Rule HexDigits = Rule("HexDigits", Plus(Set("[0-9a-fA-F]"), true) + Star('_' + Plus(Set("[0-9a-fA-F]"), true)), Private);

			// rule DecNumber() ==> @[
			//     {_numberBase=10;} 
			//     ( DecDigits ( {_isFloat=true;} '.' DecDigits )?
			//     | {_isFloat=true;} '.'
			//     ( {_isFloat=true;} ('e'|'E') ('+'|'-')? DecDigits )?
			// ];
			// rule HexNumber() ==> @[
			//     {_numberBase=16;}
			//     '0' ('x'|'X') HexDigits
			//     ( {_isFloat=true;} '.' HexDigits )?
			//     ( {_isFloat=true;} ('p'|'P') ('+'|'-')? DecDigits )?
			// ];
			// rule BinNumber() ==> @[
			//     {_numberBase=2;}
			//     '0' ('b'|'B') BinDigits
			//     ( {_isFloat=true;} '.' BinDigits )?
			//     ( {_isFloat=true;} ('p'|'P') ('+'|'-')? DecDigits )?
			// ];
			
			Rule DecNumber = Rule("DecNumber",
				Set("_numberBase", 10) +
				(Set("_isFloat", true) + C('.') + DecDigits | (RuleRef)DecDigits + Opt(
				 Set("_isFloat", true) + C('.') + DecDigits))
				+ Opt(Set("_isFloat", true) + Set("[eE]") + Opt(Set("[+\\-]")) + DecDigits),
				Private);
			
			// Note that "0x!" is parsed as HexNumber and "0b!" as BinNumber,
			// but ParseNumberValue will report an error.
			Rule HexNumber = Rule("HexNumber",
				Stmt("_numberBase = 16; _style = NodeStyle.Alternate")
				+ C('0') + Set("[xX]")
				+ Opt(HexDigits)
				+ Opt(Set("_isFloat", true) + C('.') + HexDigits)
				+ Opt(Set("_isFloat", true) + Set("[pP]") + Opt(Set("[+\\-]")) + DecDigits),
				Private);
			Rule BinNumber = Rule("BinNumber",
				Stmt("_numberBase = 2; _style = NodeStyle.Alternate2")
				+ C('0') + Set("[bB]")
				+ Opt(BinDigits)
				+ Opt(Set("_isFloat", true) + C('.') + BinDigits)
				+ Opt(Set("_isFloat", true) + Set("[pP]") + Opt(Set("[+\\-]")) + DecDigits),
				Private);

			// token Number() ==> @[
			//     { _isFloat = _isNegative = false; _typeSuffix = GSymbol.Empty; }
			//     ('-' {_isNegative = true;})?
			//     (HexNumber / BinNumber / DecNumber)
			//     ( ( ('f'|'F') {_typeSuffix=_F; _isFloat=true;}
			//       | ('d'|'D') {_typeSuffix=_D; _isFloat=true;}
			//       | ('m'|'M') {_typeSuffix=_M; _isFloat=true;}
			//       )
			//     | ('l'|'L') {_typeSuffix=_L;} (('u'|'U') {_typeSuffix=_UL;})?
			//     | ('u'|'U') {_typeSuffix=_U;} (('l'|'L') {_typeSuffix=_UL;})?
			//     )?
			//     {ParseNumberValue();}
			// ];
			number = Rule("Number", 
				Set("_isFloat", false) + (Set("_isNegative", false)
				// Note that "0x01 and 0b01" are ambiguous, because "0x01" could be
				// interpreted as two separate integers and an identifier "x". The
				// slashes suppress the warning about this ambiguity.
				+ Opt(C('-') + Set("_isNegative", true)) + Set("_typeSuffix", GSymbol.Empty))
				+ (HexNumber / BinNumber / DecNumber)
				+ Opt(( Set("[fF]") + Stmt("_typeSuffix=_F; _isFloat=true")
				      | Set("[dD]") + Stmt("_typeSuffix=_D; _isFloat=true")
				      | Set("[mM]") + Stmt("_typeSuffix=_M; _isFloat=true"))
				  | Set("[lL]") + Stmt("_typeSuffix = _L") + Opt(Set("[uU]") + Stmt("_typeSuffix = _UL"))
				  | Set("[uU]") + Stmt("_typeSuffix = _U") + Opt(Set("[lL]") + Stmt("_typeSuffix = _UL"))
				  )
				+ Call("ParseNumberValue"), Token);
			return new[] { DecDigits, HexDigits, BinDigits, DecNumber, HexNumber, BinNumber, number };
		}

		LLParserGenerator _pg;

		public Dictionary<string, Symbol> SymbolsToDeclare = new Dictionary<string, Symbol>();

		public LNode GenerateLexerCode()
		{
			_pg = new LLParserGenerator(new IntStreamCodeGenHelper(), MessageSink.Console);

			// Whitespace & comments
			var Newline   = Rule("Newline",   ((C('\r') + Opt(C('\n'))) | '\n') 
			              + Stmt("_lineStartAt = InputPosition")
			              + Stmt("_lineNumber++")
			              + Stmt("_value = WhitespaceTag.Value"), Token);
			var DotIndent = Rule("DotIndent", And("_startPosition == _lineStartAt")
			              + Stmt("_type = TT.Spaces")
			              + Plus(C('.') + Plus(C('\t') | ' '))
			              + Stmt("_indentLevel = MeasureIndent(_indent = Source.Substring(_startPosition, InputPosition - _startPosition))")
			              + Stmt("_value = WhitespaceTag.Value"), Private);
			var Spaces    = Rule("Spaces",    Plus(C(' ')|'\t') 
			              + Stmt("if (_lineStartAt == _startPosition) "
			                   + "_indentLevel = MeasureIndent(_indent = Source.Substring(_startPosition, InputPosition - _startPosition))")
			              + Stmt("_value = WhitespaceTag.Value"), Token);
			var SLComment = Rule("SLComment", Seq("//") + Star(Set("[^\r\n]")) + Stmt("_value = WhitespaceTag.Value"), Token);
			var MLCommentRef = new RuleRef(null, null);
			var MLComment = Rule("MLComment", 
				Seq("/*") +
				Star(MLCommentRef / AnyCh, false) + 
				Seq("*/") +
				Stmt("_value = WhitespaceTag.Value"), Token, 3);
			MLCommentRef.Rule = MLComment;
			_pg.AddRules(Newline, DotIndent, Spaces, SLComment, MLComment);
			
			// Strings
			var SQString = Rule("SQString", Stmt("_parseNeeded = false") + 
				C('\'') 
				+ ( (C('\\') + Set("[^\r\n]") + Stmt("_parseNeeded = true"))
				  / Set("[^'\r\n\\\\]") 
				  / (Seq("") + Expr("_parseNeeded = true")))
				+ Star(Set("[^' \t\n\r]") + Stmt("_parseNeeded = true"))
				+ (C('\'') / (Seq("") + Stmt("_parseNeeded = true")))
				+ Call("ParseSQStringValue"), Token);
			var TQString = Rule("TQString", Stmt("_parseNeeded = true")
				+ ( Stmt("_style = NodeStyle.Alternate") + 
				    Seq(@"""""""") + Star(Seq(@"\\""") / AnyCh, false) + Seq(@"""""""") 
				  | Stmt("_style = NodeStyle.Alternate | NodeStyle.Alternate2") + 
				    Seq(@"'''") + Star(Seq(@"\\'") / AnyCh, false) + Seq(@"'''"))
				+ Stmt("ParseStringValue(true)"), Token, 4);
			var DQString = Rule("DQString", Stmt("_parseNeeded = false") + 
				( C('"') + Star(C('\\') + AnyCh + Stmt("_parseNeeded = true") | Set("[^\"\\\\\r\n]")) + '"'
				| (Stmt("_style = NodeStyle.Alternate") +
				  (Seq(@"#""") + Star( (Seq(@"""""") + Stmt("_parseNeeded = true")) / Set("[^\"]") ) + '"'))
				) + Stmt("ParseStringValue(false)"), Token);
			var BQString2 = Rule("BQString2", Stmt("_parseNeeded = false") + 
				C('`') + Star(C('\\') + AnyCh + Stmt("_parseNeeded = true") | Set("[^`\\\\\r\n]")) + '`', Private);
			var BQString = Rule("BQString", BQString2 + Stmt("ParseBQStringValue()"), Token);
			_pg.AddRules(SQString, DQString, TQString, BQString, BQString2);

			// Identifiers and symbols
			var letterTest = F.Call(F.Dot("#char", "IsLetter"), F.Call(S.Cast, F.Id("LA0"), F.Id(S.Char)));
			var lettersOrPunc = Set(@"[0-9a-zA-Z_'#~!%^&*\-+=|<>/\\?:.$]");
			Debug.Assert(!((PGIntSet)lettersOrPunc.Set).Contains('`'));
			var IdExtLetter = Rule("IdExtLetter", 
				And(letterTest) + Set("[\u0080-\uFFFC]"), Private);
			var NormalId   = Rule("NormalId", (Set("[#_a-zA-Z]") | IdExtLetter) +
			                              Star(Set("[#_a-zA-Z0-9']") | IdExtLetter));
			var CommentStart = Rule("CommentStart", '/' + (C('/') | '*'), Private);
			var FancyId    = Rule("FancyId", BQString2 | Plus(AndNot(CommentStart) + lettersOrPunc | IdExtLetter));
			var Symbol     = Rule("Symbol", Stmt("_parseNeeded = false") + 
			                                Seq("@@") + FancyId + Call("ParseSymbolValue"), Token);
			var Id         = Rule("Id",   Stmt("_parseNeeded = false") + 
			                              (NormalId | '@' + FancyId + Stmt("_parseNeeded = true")) + 
			                              Call("ParseIdValue"), Private);
			_pg.AddRules(IdExtLetter, NormalId, CommentStart, FancyId, Symbol, Id);

			// Punctuation
			var Comma     = Rule("Comma",       Op(",", "Comma"), Private);
			var Semicolon = Rule("Semicolon",   Op(";", "Semicolon"), Private);
			var At        = Rule("At",          C('@') + Stmt("_type = TT.At; _value = GSymbol.Empty"), Private);
			var ops = Set(@"[~!%^&*\-+=|<>/?:.$]");
			var Operator  = Rule("Operator",  Plus(AndNot(CommentStart) + ops) + Stmt("ParseNormalOp()"), Private);
			var BackslashOp = Rule("BackslashOp", '\\' + Opt(FancyId) + Stmt("ParseBackslashOp()"), Private);
			_pg.AddRules(Comma, Semicolon, At, Operator, BackslashOp);

			// Openers & closers
			var LParen = Rule("LParen", C('('), Token);
			var RParen = Rule("RParen", C(')'), Token);
			var LBrack = Rule("LBrack", C('['), Token);
			var RBrack = Rule("RBrack", C(']'), Token);
			var LBrace = Rule("LBrace", C('{'), Token);
			var RBrace = Rule("RBrace", C('}'), Token);
			_pg.AddRules(new[] { LParen, RParen, LBrack, RBrack, LBrace, RBrace });

			Rule Number;
			_pg.AddRules(NumberParts(out Number));

			var Shebang = Rule("Shebang", Seq("#!") + Star(Set("[^\r\n]")) + Opt(Newline));
			Alts tokenAlts = (Alts)(
				(And(Expr("InputPosition == 0")) + T(Shebang)) /
				T(Symbol) /
				T(Id) /
				T(Spaces) / T(Newline) / DotIndent /
				T(SLComment) / T(MLComment) /
				T(Number) /
				(Stmt("_type = TT.String") + TQString) /
				(Stmt("_type = TT.String") + DQString) /
				T(SQString) / T(BQString) /
				T(Comma) / T(Semicolon) /
				T(LParen) / T(LBrack) / T(LBrace) /
				T(RParen) / T(RBrack) / T(RBrace) /
				T(At) / BackslashOp / Operator);
			tokenAlts.DefaultArm = 2; // Id
			var token = Rule("Token", tokenAlts, Token, 3);
			_pg.AddRules(new[] { token, Shebang });
			_pg.FullLLk = true;
			//_pg.Verbosity = 3;

			var members = _pg.Run(F.File);

			members = members.PlusArgs(SymbolsToDeclare.Select(p => 
				F.Var(F.Id("Symbol"), p.Key, F.Call(F.Dot("GSymbol", "Get"), F.Literal(p.Value.Name)))));

			return F.Attr(F.Public, F.Id(S.Partial), 
			        F.Call(S.Class, F.Id(_("LesLexer")), F.Tuple(), members));
		}
		protected Pred Op(string @operator, string name)
		{
			var symName = "_" + name;
			SymbolsToDeclare[symName] = GSymbol.Get("#" + @operator);
			return Seq(@operator) + Stmt(@"_type = TT." + name) + Stmt(@"_value = " + symName);
		}

		protected LNode Set(string var, object value)
		{
			if (value is Symbol)
				// As long as we're targeting plain C#, don't output \Symbol literals
				return F.Set(F.Id(var), F.Call(F.Dot("GSymbol", "Get"), F.Literal(value.ToString())));
			else
				return F.Set(F.Id(var), F.Literal(value));
		}

		Pred T(Rule token)
		{
			return Stmt(string.Format(@"_type = TT.{0}", token.Name)) + token;
		}
	}
}
