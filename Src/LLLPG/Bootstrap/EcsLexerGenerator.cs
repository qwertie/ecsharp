using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.LLParserGenerator;
using Loyc.CompilerCore;
using Loyc.Syntax;
using Loyc.Collections;
using S = Loyc.Syntax.CodeSymbols;

namespace Ecs.Parser
{
	/// <summary>Bootstrapper for the EC# lexer.</summary>
	public class EcsLexerGenerator : LlpgHelpers
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
			              + Stmt("_allowPPAt = _lineStartAt = InputPosition")
			              + Stmt("_lineNumber++"), Token);
			var Spaces    = Rule("Spaces",    Plus(C(' ')|'\t') 
			              + Stmt("if (_allowPPAt == _startPosition) _allowPPAt = InputPosition")
			              + Stmt("if (_lineStartAt == _startPosition) _indentLevel = MeasureIndent(_startPosition, InputPosition - _startPosition)"), Token);
			var SLComment = Rule("SLComment", Seq("//") + Star(Set("[^\r\n]")), Token);
			var MLCommentRef = new RuleRef(null, null);
			var MLComment = Rule("MLComment", 
				Seq("/*") +
				Star((And(F.Id("AllowNestedComments")) + MLCommentRef) / AnyCh, false) + 
				Seq("*/"), Token, 3);
			MLCommentRef.Rule = MLComment;
			_pg.AddRules(new[] { Newline, Spaces, SLComment, MLComment });

			// Strings
			var SQString = Rule("SQString", Stmt("_parseNeeded = false") + (
				Stmt("_verbatims = 0")  + C('\'') + Star(C('\\') + AnyCh + Stmt("_parseNeeded = true") | Set("[^'\\\\\r\n]")) + '\'')
				+ Call("ParseCharValue"), Token);
			var DQString = Rule("DQString", Stmt("_parseNeeded = false") + 
				( Stmt("_verbatims = 0") + C('"') + Star(C('\\') + AnyCh + Stmt("_parseNeeded = true") | Set("[^\"\\\\\r\n]")) + '"'
				| Stmt("_verbatims = 1; _style = NodeStyle.Alternate;")
				                        + C('@') + Opt(C('@') + Stmt("_verbatims = 2; _style = NodeStyle.UserFlag;"))
				                        + '"' + Star( (Seq(@"""""") + Stmt("_parseNeeded = true"))
				                                    | (C('\\') + Set(@"[({]") + Stmt("_parseNeeded = true"))
				                                    / Set("[^\"]"))
				                        + '"') + Call("ParseStringValue"), Token);
			var BQStringV = Rule("BQStringV", Stmt("_verbatims = 1") + 
				C('`') + Star(Seq("``") + Stmt("_parseNeeded = true") | Set("[^`\r\n]"), true) + '`', Private);
			var BQStringN = Rule("BQStringN", Stmt("_verbatims = 0") + 
				C('`') + Star(C('\\') + Stmt("_parseNeeded = true") + AnyCh | Set("[^`\\\\\r\n]")) + '`', Private);
			var BQString = Rule("BQString", Stmt("_parseNeeded = false") + 
				(RuleRef)BQStringN + Call("ParseBQStringValue"), Token);
			_pg.AddRules(new[] { SQString, DQString, BQString, BQStringN, BQStringV });

			// Punctuation
			var Comma     = Rule("Comma",     Op(",", "Comma"), Private);
			var Colon     = Rule("Colon",     Op(":", "Colon"), Private);
			var Semicolon = Rule("Semicolon", Op(";", "Semicolon"), Private);
			var At        = Rule("At",        Op("@", "At"), Private);
			// Note: << >> and ** are deliberately omitted. They are handled as a pair of tokens.
			var Operator  = Rule("Operator", 
				Op("->", "PtrArrow") / Op("..", "DotDot") / Op(".", "Dot") | 
				Op(">>=", "ShrSet") / Op(">=", "GE") / Op(">", "GT") | 
				Op("<<=", "ShlSet") / Op("<=", "LE") / Op("<", "LT") |          
				Op("&&", "And") / Op("&=", "AndBitsSet") / Op("&", "AndBits") | 
				Op("||", "Or") / Op("|=", "OrBitsSet") / Op("|", "OrBits") |    
				Op("^^", "Xor") / Op("^=", "XorBitsSet") / Op("^", "XorBits") | 
				Op(":=", "QuickBindSet") / Op("=:", "QuickBind") / Op("::", "ColonColon") | 
				Op("==>", "Forward") / Op("==", "Eq") |                         
				Op("=>", "LambdaArrow") / Op("=", "Set") |                      
				Op("!=", "Neq") / Op("!", "Not") / Op("~", "NotBits") |         
				Op("**=", "ExpSet") / // there is no Exp token due to ambiguity
				Op("*=", "MulSet") / Op("*", "Mul") | 
				Op("/=", "DivSet") / Op("/", "Div") | 
				Op("%=", "ModSet") / Op("%", "Mod") | 
				Op("+=", "AddSet") / Op("++", "Inc") / Op("+", "Add") |            
				Op("-=", "SubSet") / Op("--", "Dec") / Op("-", "Sub") |
				Op("??=", "NullCoalesceSet") / Op("??.", "NullDot") | 
				Op("??", "NullCoalesce") / Op("?.", "NullDot") / Op("?", "QuestionMark") |
				Op("$", "Substitute") / 
				Op(@"\", "Backslash"), Token, 3);
			_pg.AddRules(new[] { Comma, Colon, Semicolon, At, Operator });

			// Identifiers (keywords handled externally) and symbols
			var letterTest = F.Call(F.Dot("#char", "IsLetter"), F.Call(S.Cast, F.Call(_("LA"), F.Literal(0)), F.Id(S.Char)));

			var IdEscSeq = Rule("IdEscSeq",
				Seq(@"\u") + Set("[0-9a-fA-F]") + Set("[0-9a-fA-F]")
						   + Set("[0-9a-fA-F]") + Set("[0-9a-fA-F]"), Private);
			var IdSpecialChar = Rule("IdSpecialChar", 
				( Gate(And((RuleRef)IdEscSeq) + '\\', (RuleRef)IdEscSeq + Stmt("_parseNeeded = true"))
				| And(letterTest) + Set("[\u0080-\uFFFC]")
				), Private);
			var IdStart    = Rule("IdStart", Set("[a-zA-Z_]") / IdSpecialChar, Private);
			var IdCont     = Rule("IdCont", Set("[0-9a-zA-Z_']") / IdSpecialChar, Private);
			var SpecialId  = Rule("SpecialId", BQStringN | Plus(IdCont, true), Private);
			var SpecialIdV = Rule("SpecialIdV", BQStringV | Plus(IdCont, true), Private);
			var Id         = Rule("Id", 
				//NF.Set(NF.Id("_keyword"), NF.Literal(null)) + 
				//( Opt(C('#')) + '@' + SpecialIdV
				// most branches DO use special syntax so that's the default
				Stmt("_parseNeeded = true") +
				( (C('@') + SpecialIdV)
				/ (Seq("#@") + SpecialIdV)
				/ (Opt(C('@')) + '#' +
					Opt( SpecialId / Seq("<<=") / Seq("<<")
					   / Seq(">>=") / Seq(">>") / Seq("**") / ((RuleRef)Operator + Stmt("_type = TT.Id"))
					   | Comma | Colon | Semicolon, true))
				| (IdStart + Star(IdCont, true) + Stmt("_parseNeeded = false"))
				)
				+ Stmt("bool isPPLine = ParseIdValue()")
				// Because the loop below matches almost anything, several warnings
				// appear above it, even in different rules such as SpecialId; 
				// workaround is to add "greedy" flags on affected loops.
				+ Gate(Seq(""), Opt(And(F.Id("isPPLine")) 
					+ Stmt("int ppTextStart = InputPosition")
					+ Star(Set("[^\r\n]"))
					+ Stmt("_value = (string)CharSource.Substring(ppTextStart, InputPosition - ppTextStart)"))
				  ), Token, 3);
			var Symbol = Rule("Symbol", C('\\') + Stmt("_verbatims = -1") + SpecialId + Call("ParseSymbolValue"), Token);
			_pg.AddRules(Id, IdEscSeq, IdSpecialChar, IdStart, IdCont, SpecialId, SpecialIdV, Symbol);

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
				T(Spaces) / T(Newline) /
				T(SLComment) / T(MLComment) /
				T(Number) /
				T(SQString) / T(DQString) / T(BQString) /
				T(Comma) / T(Colon) / T(Semicolon) /
				T(LParen) / T(LBrack) / T(LBrace) /
				T(RParen) / T(RBrack) / T(RBrace) /
				Gate(C('@') + '@' + Set("[^\"]"), At) / At /
				Operator);
			tokenAlts.DefaultArm = 2; // Id
			var token = Rule("Token", tokenAlts, Token, 3);
			//var start   = Rule("Start", Opt(Shebang, true) + Star(token), Start);
			_pg.AddRules(new[] { token, Shebang });
			_pg.FullLLk = true;

			var members = _pg.GenerateCode(F.File);

			members = members.PlusArgs(SymbolsToDeclare.Select(p => 
				F.Var(F.Id("Symbol"), p.Key, F.Call(F.Dot("GSymbol", "Get"), F.Literal(p.Value.Name)))));

			return F.Attr(F.Public, F.Id(S.Partial), 
			        F.Call(S.Class, F.Id(_("EcsLexer")), F.List(), members));
		}
		protected Pred PP(string word)
		{
			return Seq(word) + Stmt(string.Format("_type = TT.PP{0}", word));
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
			return Stmt(string.Format(@"_type = TT.{0}", token.Name)) + (RuleRef)token;
		}
	}
}
