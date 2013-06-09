using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.CompilerCore;
using Loyc.Syntax;
using Loyc.Collections;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.LLParserGenerator
{
	/// <summary>Bootstrapper for the EC# lexer.</summary>
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
			// // Helper rules for Number
			// rule DecDigits() ==> #[ '0'..'9'+ ('_' '0'..'9'+)* ]
			// rule BinDigits() ==> #[ '0'..'1'+ ('_' '0'..'1'+)* ]
			// rule HexDigits() ==> #[ greedy('0'..'9' | 'a'..'f' | 'A'..'F')+ greedy('_' ('0'..'9' | 'a'..'f' | 'A'..'F')+)* ]
			Rule DecDigits = Rule("DecDigits", Plus(Set("[0-9]")) + Star('_' + Plus(Set("[0-9]"))), Private);
			Rule BinDigits = Rule("BinDigits", Plus(Set("[0-1]")) + Star('_' + Plus(Set("[0-1]"))), Private);
			Rule HexDigits = Rule("HexDigits", Plus(Set("[0-9a-fA-F]"), true) + Star('_' + Plus(Set("[0-9a-fA-F]"), true)), Private);

			// rule DecNumber() ==> #[
			//     {_numberBase=10;} 
			//     ( DecDigits ( {_isFloat=true;} '.' DecDigits )?
			//     | {_isFloat=true;} '.'
			//     ( {_isFloat=true;} ('e'|'E') ('+'|'-')? DecDigits )?
			// ];
			// rule HexNumber() ==> #[
			//     {_numberBase=16;}
			//     '0' ('x'|'X') HexDigits
			//     ( {_isFloat=true;} '.' HexDigits )?
			//     ( {_isFloat=true;} ('p'|'P') ('+'|'-')? DecDigits )?
			// ];
			// rule BinNumber() ==> #[
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
				Stmt("_numberBase = 2; _style = NodeStyle.UserFlag")
				+ C('0') + Set("[bB]")
				+ Opt(BinDigits)
				+ Opt(Set("_isFloat", true) + C('.') + BinDigits)
				+ Opt(Set("_isFloat", true) + Set("[pP]") + Opt(Set("[+\\-]")) + DecDigits),
				Private);

			// token Number() ==> #[
			//     { _isFloat = _isNegative = false; _typeSuffix = \``; }
			//     '-'?
			//     (HexNumber / BinNumber / DecNumber)
			//     ( ( ('f'|'F') {_typeSuffix=\F; _isFloat=true;}
			//       | ('d'|'D') {_typeSuffix=\D; _isFloat=true;}
			//       | ('m'|'M') {_typeSuffix=\M; _isFloat=true;}
			//       )
			//     | ('l'|'L') {_typeSuffix=\L;} (('u'|'U') {_typeSuffix=\UL;})?
			//     | ('u'|'U') {_typeSuffix=\U;} (('l'|'L') {_typeSuffix=\UL;})?
			//     )?
			// ];
			// rule Tokens() ==> #[ Token* ];
			// rule Token() ==> #[ Number | . ];
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
			_pg = new LLParserGenerator();
			_pg.OutputMessage += (node, pred, type, msg) =>
			{
				object subj = node == LNode.Missing ? (object)pred : node;
				Console.WriteLine("--- EC# Lexer at {0}:\n--- {1}: {2}", subj.ToString(), type, msg);
			};

			// Whitespace & comments
			var Newline   = Rule("Newline",   ((C('\r') + Opt(C('\n'))) | '\n') 
			              + Stmt("_lineNumber++"), Token);
			var Spaces    = Rule("Spaces",    Plus(C(' ')|'\t') 
			              + Stmt("if (_lineStartAt == _startPosition) _indentLevel = MeasureIndent(_startPosition, InputPosition - _startPosition)"), Token);
			var SLComment = Rule("SLComment", Seq("//") + Star(Set("[^\r\n]")), Token);
			var MLCommentRef = new RuleRef(null, null);
			var MLComment = Rule("MLComment", 
				Seq("/*") +
				Star(MLCommentRef / Any, false) + 
				Seq("*/"), Token, 3);
			MLCommentRef.Rule = MLComment;
			_pg.AddRules(Newline, Spaces, SLComment, MLComment);
			
			// Strings
			var SQString = Rule("SQString", Stmt("_parseNeeded = false") + 
				C('\'') + Star(C('\\') + Any + Stmt("_parseNeeded = true") | Set("[^'\\\\\r\n]")) + '\''
				+ Call("ParseCharValue"), Token);
			var TQString = Rule("TQString", Stmt("_parseNeeded = false; _style = NodeStyle.UserFlag") +
				Seq(@"""""""") + Star((Seq(@"""""""""") + Stmt("_parseNeeded = true")) / Any) + Seq(@"""""""") 
				+ Call("ParseStringValue"), Token, 4);
			var DQString = Rule("DQString", Stmt("_parseNeeded = false") + 
				( C('"') + Star(C('\\') + Any + Stmt("_parseNeeded = true") | Set("[^\"\\\\\r\n]")) + '"'
				| (Stmt("_style = NodeStyle.Alternate;") +
				  (Seq(@"#""") + Star( (Seq(@"""""") + Stmt("_parseNeeded = true")) / Set("[^\"]") ) + '"'))
				) + Call("ParseStringValue"), Token);
			var BQStringP = Rule("BQStringP", Stmt("_parseNeeded = false") + 
				C('`') + Star(C('\\') + Any + Stmt("_parseNeeded = true") | Set("[^`\\\\\r\n]")) + '`', Private);
			var BQString = Rule("BQString", BQStringP + Call("ParseStringValue"), Token);
			_pg.AddRules(SQString, DQString, TQString, BQString, BQStringP);

			// Punctuation
			var Comma     = Rule("Comma",       Op(",", "Comma"), Private);
			var Semicolon = Rule("Semicolon",   Op(";", "Semicolon"), Private);
			var At        = Rule("At",          Op("@", "At"), Private);
			var AtAt      = Rule("AtAt",        Op("@@", "AtAt"), Private);
			var Backslash = Rule("Backslash",   Op("\\", "Operator"), Private);
			var ops1 = Set("[~!%^&*-+=|<>/?:.]");
			var ops2 = Set("[~!%^&*-+=|<>/?:.$]");
			var CommentStart = Rule("CommentStart", '/' + (C('/') | '*'), Private);
			var OpChars   = Rule("OpChars",   AndNot(CommentStart) + ops1 + Star(AndNot(CommentStart) + ops2), Private);
			var Operator  = Rule("Operator",  OpChars + Stmt("ParseOp()"), Token);
			_pg.AddRules(Comma, Semicolon, OpChars, Operator);

			// Identifiers (keywords handled externally) and symbols
			var letterTest = F.Call(F.Dot("#char", "IsLetter"), F.Call(S.Cast, F.Call(_("LA"), F.Literal(0)), F.Id(S.Char)));
			var IdExtLetter = Rule("IdExtLetter", 
				And(letterTest) + Set("[\u0080-\uFFFC]"), Private);
			var IdStart    = Rule("IdStart", Set("[a-zA-Z_]") / IdExtLetter, Private);
			var IdCont     = Rule("IdCont", Set("[0-9a-zA-Z_']") / IdExtLetter, Private);
			var NormalId   = Rule("NormalId", (Set("[a-zA-Z_#]") | IdExtLetter) +
			                              Star(Set("[0-9a-zA-Z_'#]") | IdExtLetter));
			var Symbol     = Rule("Symbol", C('\\') + (BQString | NormalId) + Call("ParseSymbolValue"), Token);
			var Id         = Rule("Id",   NormalId | 
			    Seq(@"\\") + (BQString | Star(Set("[0-9a-zA-Z_'#~!%^&*-+=|<>/?:.@$]") | IdExtLetter)) + Call("ParseIdValue"), Private);
			_pg.AddRules(IdExtLetter, IdStart, IdCont, NormalId, Symbol, Id);

			// Openers & closers
			var LParen = Rule("LParen", C('('), Token);
			var RParen = Rule("RParen", C(')'), Token);
			var LBrack = Rule("LBrack", C('['), Token);
			var RBrack = Rule("RBrack", C(']'), Token);
			var LBrace = Rule("LBrace", C('{'), Token);
			var RBrace = Rule("RBrace", C('}'), Token);
			var OpenOf = Rule("OpenOf", Seq(".["), Token);
			_pg.AddRules(new[] { LParen, RParen, LBrack, RBrack, LBrace, RBrace, OpenOf });

			Rule Number;
			_pg.AddRules(NumberParts(out Number));

			var Shebang = Rule("Shebang", Seq("#!") + Star(Set("[^\r\n]")) + Opt(Newline));
			Alts tokenAlts = (Alts)(
				(And(Expr("InputPosition == 0")) + T(Shebang)) /
				T(Symbol) /
				T(Id) /
				T(Spaces) / T(Newline) /
				T(SLComment) / T(MLComment) /
				T(Number) /
				(Stmt("_type = TT.DQString") + TQString) /
				T(SQString) / T(DQString) / T(BQString) /
				T(Comma) / T(Semicolon) /
				T(LParen) / T(LBrack) / T(LBrace) /
				T(RParen) / T(RBrack) / T(RBrace) /
				T(OpenOf) / Operator);
			tokenAlts.DefaultArm = 2; // Id
			var token = Rule("Token", tokenAlts, Token, 3);
			_pg.AddRules(new[] { token, Shebang });
			_pg.FullLLk = true;

			var members = _pg.GenerateCode(F.File);

			members = members.PlusArgs(SymbolsToDeclare.Select(p => 
				F.Var(F.Id("Symbol"), p.Key, F.Call(F.Dot("GSymbol", "Get"), F.Literal(p.Value.Name)))));

			return F.Attr(F.Public, F.Id(S.Partial), 
			        F.Call(S.Class, F.Id(_("LesLexer")), F.List(), members));
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
