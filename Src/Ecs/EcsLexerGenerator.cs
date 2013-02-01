using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.LLParserGenerator;

namespace ecs
{
	using S = ecs.CodeSymbols;
	using Loyc;
	using Loyc.CompilerCore;

	/// <summary>Bootstrapper for the EC# lexer.</summary>
	public class EcsLexerGenerator : LlpgTests
	{
		public Node GenerateLexerCode()
		{
			_pg = new LLParserGenerator();
			_pg.OutputMessage += (node, pred, type, msg) =>
			{
				object subj = node == Node.Missing ? (object)pred : node;
				Console.WriteLine("--- at {0}:\n--- {1}: {2}", subj.ToString(), type, msg);
			};

			// Whitespace & comments
			var Newline   = Rule("Newline",   (C('\r') + Opt(C('\n'))) | '\n', Token);
			var Spaces    = Rule("Spaces",    Plus(C(' ')|'\t'), Token);
			var SLComment = Rule("SLComment", C('/') + '/' + Star(Any, false) + Newline, Token);
			var MLCommentRef = new RuleRef(null, null);
			var MLComment = Rule("MLComment", 
				Seq("/*") +
				Star((NF.Symbol("AllowNestedComments") + MLCommentRef) / Any, false) + 
				Seq("*/"), Token, 3);
			MLCommentRef.Rule = MLComment;
			_pg.AddRules(new[] { Newline, Spaces, SLComment, MLComment });

			// Strings
			var SQString = Rule("SQString", 
				C('\'') + (C('\\') + Any | Set(@"[^'\\]")) + '\'', Token);
			var DQString = Rule("DQString", 
				  C('@') + Opt(C('@')) + '"' + Star(C('"') + '"' | Set(@"[^""]")) + '"'
				| C('"') + Star(C('\\') + Any | Set(@"[^""\\]")) + '"', Token);
			var BQStringV = Rule("BQStringV", '`' + Star(Seq("``") | Set(@"[^`]")) + '`', Fragment);
			var BQStringN = Rule("BQStringN", '`' + Star(C('\\') + Any | Set(@"[^`\\]")) + '`', Fragment);
			var BQString = Rule("BQString", C('@') + BQStringV | BQStringN, Token);
			_pg.AddRules(new[] { SQString, DQString, BQString, BQStringN, BQStringV });

			// Punctuation
			var Comma     = Rule("Comma", C(','), Token);
			var Colon     = Rule("Colon", C(':'), Token);
			var Semicolon = Rule("Semicolon", C(';'), Token);
			// << >> and ** are deliberately omitted. They are handled as a pair of tokens.
			var Operator  = Rule("Operator", 
				Set(@"[~!%^&*+\-=|\\.<>/?]") |
				Set(@"[!=%^&*-+|<>]") + '=' |
				Seq("&&") | Seq("++") | Seq("--") | Seq("||") |
				Seq("..") | Seq("??") + Opt(Set("[.=]")) |
				Seq("=>") | Seq("==>") | Seq("->"), Token, 3);
			_pg.AddRules(new[] { Comma, Colon, Semicolon, Operator });

			// Identifiers (keywords handled externally) and symbols
			var letterTest = NF.Call(NF.Dot("#char", "IsLetter"), NF.Call(_("LA"), NF.Literal(0)));
			var IdSpecial = Rule("IdSpecial", 
				Seq(@"\u") + Set("[0-9a-fA-F]") + Set("[0-9a-fA-F]")
				           + Set("[0-9a-fA-F]") + Set("[0-9a-fA-F]") 
				,Fragment);//| And(letterTest) + Any);
			var IdStart   = Rule("IdStart", Set("[a-zA-Z_]") / IdSpecial, Fragment);
			var IdCont    = Rule("IdCont", Set("[0-9a-zA-Z_']") / IdSpecial, Fragment);
			var SpecialId  = Rule("SpecialId", BQStringN | Plus(IdCont), Fragment);
			var SpecialIdV = Rule("SpecialIdV", BQStringV | Plus(IdCont), Fragment);
			var Ident     = Rule("Ident", 
				NF.Call(S.Set, NF.Symbol("_keyword"), NF.Symbol("#null")) + 
				( Opt(C('@')) + '#' + 
					Opt(Comma | Colon | Semicolon | Operator | SpecialId
					  | Seq("<<") | Seq(">>") | Seq("**") | C('$'))
				//| Opt(C('#')) + '@' + SpecialIdV
				| Seq("#@") + SpecialIdV
				| C('@') + SpecialIdV
				| IdStart + Plus(IdCont) + NF.Call(_("AutoDetectKeyword"))
				| C('$')
				), Token);
			var SymbolLiteral = Rule("SymbolLiteral", C('$') + SpecialId, Token);
			_pg.AddRules(new[] { Ident, IdSpecial, IdStart, IdCont, SpecialId, SpecialIdV, SymbolLiteral });

			// Openers & closers
			var LParen = Rule("LParen", C('('), Token);
			var RParen = Rule("RParen", C(')'), Token);
			var LBrack = Rule("LBrack", C('['), Token);
			var RBrack = Rule("RBrack", C(']'), Token);
			var LBrace = Rule("LBrace", C('{'), Token);
			var RBrace = Rule("RBrace", C('}'), Token);
			var LCodeQuote = Rule("LCodeQuote", C('@') + Set(@"[{(\[]"), Token);
			var LCodeQuoteS = Rule("LCodeQuoteS", C('@') + '@' + Set(@"[{(\[]"), Token);
			_pg.AddRules(new[] { LParen, RParen, LBrack, RBrack, LBrace, RBrace, LCodeQuote, LCodeQuoteS });

			Rule Number;
			_pg.AddRules(NumberParts(out Number));

			var UnknownChar = Rule("UnknownChar", Any, Token);
			var token = Rule("Token", 
				T(Spaces) / T(Newline) /
				T(SLComment) / T(MLComment) /
				T(Ident) /
				T(Number) /
				T(SQString) / T(DQString) / T(BQString) /
				T(Comma) / T(Colon) / T(Semicolon) /
				T(Operator) /
				T(LParen) / T(LBrack) / T(LBrace) /
				T(RParen) / T(RBrack) / T(RBrace) /
				T(LCodeQuote) / T(LCodeQuoteS)
				, Token, 3);
			var Shebang = Rule("Shebang", Seq("#!") + Star(Any, false) + Newline);
			var start   = Rule("Start", Opt(Shebang, true) + Star(token), Start);
			_pg.AddRules(new[] { UnknownChar, token, Shebang, start });

			return _pg.GenerateCode(_("EcsLexer"), NF.File);
		}

		Pred T(Rule token)
		{
			return NF.Call(S.Set, NF.Symbol("_type"), NF.Literal(token.Name)) + (RuleRef)token;
		}
	}
}
