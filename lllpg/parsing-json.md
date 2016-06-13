---
title: "JSON as plain-old objects: 111-line parser and 217-line printer"
layout: article
date: 31 May 2016
tagline: "LLLPG demo: let's parse JSON as List&lt;object> or Dictionary&lt;string,object>, and print it compactly with smart line breaks"
toc: true
---

### WORK IN PROGRESS 

Introduction
------------

The parser generator [LLLPG](http://ecsharp.net/lllpg) recently got an ANTLR-style input mode, which requires a bit less typing than the original LLLPG mode. I thought parsing JSON would be a nice way to show off this new mode, as well as illustrate how to use LLLPG.

I'll also show you a relatively simple technique I developed for printing out JSON (or other languages) while deciding dynamically where to put newlines based on the length of the content. For example, 

How to parse JSON with LLLPG
----------------------------

JSON, it turns out, is one of the rare languages that is so simple it doesn't need a two-stage parser: it's LL(1) in characters. In contrast, most computer languages are LL(*) (unbounded lookahead is required) from the perspective of how many characters you have to look ahead before deciding which grammar rule to invoke) even when a two-stage parser would only be LL(2).

You can find the complete parser in the [LLLPG-Samples repo](http://github.com/qwertie/LLLPG-Samples); here I'll just show you around:


    using System(, .Collections.Generic, .Linq, .Text, .Diagnostics);
    using Loyc(, .Collections, .Syntax(, .Lexing));	

namespace Json
{	
	public class JsonParser : BaseLexer<ICharSource>
	{
		/// <summary>Parses a json string into an object.</summary>
		/// <returns>a Dictionary{string, object}, List{object}, string or number.</returns>
		/// <remarks>Remember, string converts implicitly to UString which boxes into ICharSource</remarks>
		public static object Parse(UString chars, bool allowComments = false) { return Parse(chars, "", 0, true, allowComments); }
		public static object Parse(ICharSource chars, string fileName, int inputPosition = 0, 
			bool checkForEofAfter = true, bool allowComments = false, IMessageSink errSink = null)
		{
			var parser = new JsonParser(chars, fileName, inputPosition, false) { AllowComments = allowComments };
			if (errSink != null) parser.ErrorSink = errSink;
			var result = parser.Value();
			if (checkForEofAfter && parser.LA0 != -1)
				parser.Error(0, "Expected EOF after JSON value");
			return result;
		}

		public bool AllowComments { get; set; }
		public JsonParser(ICharSource chars, string fileName = "", int inputPosition = 0, bool newSourceFile = true) 
			: base(chars, fileName, inputPosition, newSourceFile) {}

		[LL(1), FullLLk, AddCsLineDirectives(false)]
		LLLPG (lexer)
		@{	// '@' enables ANTLR syntax mode (not quite ANTLR-compatible though)
			// Whitespace -----------------------------------------------------

			[LL(2)] // for comments
			SkipWS : 
				greedy
				[	(' ' | '\t') 
				|	Newline
				|	&{AllowComments} // not a real part of JSON
					( "//" ~('\r'|'\n')* (Newline|EOF)
					| "/*" nongreedy(Newline / _)* "*/" )
				]*;
			extern rule Newline : '\r' + '\n'? | '\n'; // inherit from base class
		
			// Numbers & Strings ----------------------------------------------
		
			private Number returns [double result] : {int start = InputPosition;}
				'-'?
				( '0' | '1'..'9' '0'..'9'* )
				( '.' '0'..'9'* )?
				( ('e'|'E') ('+'|'-')? '0'..'9'+ )?
				{
					UString str = CharSource.Slice(start, InputPosition - start);
					$result = ParseHelpers.TryParseDouble(ref str, 10);
				};
			
			private String returns [string result] : {int start = InputPosition;}
				{bool escaped = false;}
				'"'
				( '\\' _ {escaped = true;} | default ~('"'|'\\'|0..31) )* 
				('"' | error { Error(0, "Expected closing quote"); })
				{
					UString text = CharSource.Slice(start + 1, InputPosition - start - 2);
					if (escaped) {
						$result = ParseHelpers.UnescapeCStyle(text);
					} else {
						$result = (string) text;
					}
				};

			// Complex values -------------------------------------------------
		
			protected Value returns [object result] :
				SkipWS
				(	result=Dictionary 
				|	result=List 
				|	result=Number
				|	result=String
				|	result=WordLiteral
				|	error { Error(0, "Expected a value"); $result = null; } 
					      greedy(~('}'|']'|','))*
				) SkipWS;
		
			[LL(10)] // LL(1) would work but not show the desired error message in case of e.g. "troo"
			private WordLiteral returns [object result] : {int start = InputPosition;}
				(	"true"  {$result = G.BoxedTrue;}
				/	"false" {return G.BoxedFalse;}
				/	"null"  {return null;}
				/	('a'..'z'|'A'..'Z')+ { 
						Error(0, "JSON does not support identifiers");
						return CharSource.Slice(start, InputPosition - start).ToString();
					}
				);

			private List returns [List<object> result] :
				{$result = new List<object>();}
				'[' SkipWS ( result+=Value (',' result+=Value)* )? ']';
			
			private Dictionary returns [Dictionary<string, object> result] :
				{$result = new Dictionary<string, object>();}
				'{' SkipWS ( Pair[result] ( ',' Pair[result] )* )? '}';

			private Pair[Dictionary<string, object> dict] :
				(	SkipWS String SkipWS
					(	':' Value
						{dict.Add($String, $Value);}
					|	error { Error(0, "Expected value for '{0}'", $String); }
					)
				|	error { $String = ""; Error(0, "Expected a string key"); } ~(':'|'}'|',')
				);
		};
	}
}

