/*
 * Created by David on 7/17/2007 at 7:11 AM
 */

using System;
using System.Collections.Generic;
using Loyc.Runtime;

namespace Loyc.CompilerCore.Onep
{
	/// <summary>List of suggested multi-language precedence levels.</summary>
	/// <remarks>Remember that IONEParser assumes odd-numbered precedence levels
	/// are right-associative and even-numbered levels are left-associative.
	/// Whether the number is odd or even is not important for unary operators.
	/// </remarks>
	public enum Precedence {
		ReservedHi = 0,       // Reserved for future use
		TightHi = 1,          // beginning of 'tightly bound' operators
		ScopeDot = 4,         // e.ID     (and a->b in a C-based language)
		UnaryHi = 8,          // e++, e--, e(...), e[...]
		UnaryMed = 10,        // !e, ~e, *e, &e, (typecast...)e, maybe -e and +e
		TightBinOp = 12,      // e:e (range operator)
		UnaryLo = 14,         // not e
		TightLo = 18,         // End of 'tightly bound' operators
		Exponentiation = 22,  // e**e     (e^e in some languages)
		Negation = 25,        // maybe -e
		MulDiv = 28,          // e*e, e/e, e%e, e mod e
		Shift = 32,           // e<<e, e<<<e, e>>e, e>>>e (useful precedence)
		AddSub = 36,          // e+e, e-e
		ShiftLo = 40,         // e<<e, e<<<e, e>>e, e>>>e (precedence in C)
		BitwiseAndHi = 44,    // alternate precedence for e&e
		BitwiseOrHi = 46,     // alternate precedence for e|e, e^e
		ComparativeHi = 50,   // no ops defined at this level
		Comparative = 54,     // e==e, e!=e, e>e, e<e, e>=e, e<=e, e=~e
		ComparativeLo = 58,   // e==e, e!=e in some languages
		BitwiseAnd = 62,      // e&e      (precedence in C)
		BitwiseOr = 64,       // e|e, e^e (precedence in C)
		LooseHi = 68,         // Beginning of 'loosely bound' operators
		LogicalAnd = 72,      // e&&e, e||e, e and e, e or e, e xor e
		LogicalOr = 74,       // e&&e, e||e, e and e, e or e, e xor e
		PhraseHi = 78,        // no ops defined at this level
		Phrase = 82,          // complex phrases: e?e:e, between e and e
		PhraseLo = 86,        // no ops defined at this level
		Assignment = 89,      // e=e, e+=e, e-=e, e*=e, e/=e, e%=e, e&=e, ...
		Comma = 94,           // e,e in a C-based language
		ArgSeparator = 96,    // commas used to separate arguments
		LooseLo = 100,        // End of 'loosely bound' operators
	}
}
