// Generated from Les3Printer_CanPrintAsNumber.ecs by LeMP custom tool. LeMP version: 2.8.4.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Loyc.Syntax.Lexing;

namespace Loyc.Syntax.Les
{
	partial class Les3Printer
	{
		[ThreadStatic] static LexerSource<UString> src;	// provides the APIs expected by LLLPG
		[ThreadStatic] static NullMessageSink msgCounter;
	
	
		public static bool CanPrintAsNumber(UString textValue, Symbol typeMarker)
		{
			int la0;
			// Check if the typeMarker is potentially acceptable for printing as a number
			// line 23
			if (typeMarker.Name.TryGet(0, '\0') != '_' || !IsNormalIdentifier(typeMarker.Name))
				return false;
			// line 25
			char firstTMChar = typeMarker.Name.TryGet(1, '\0');
			// line 26
			if (firstTMChar == 'e' || firstTMChar == 'E' || firstTMChar == 'p' || firstTMChar == 'P')
				return false;
		
			// Prepare LexerSource needed by LLLPG, and our error counter
			// line 30
			if (src == null) {
				src = new LexerSource<UString>(textValue, "", 0, false);
				src.ErrorSink = msgCounter = new NullMessageSink();
			} else {
				src.Reset(textValue, "", 0, false);	// re-use old object
				msgCounter.ResetCountersToZero();
			}
		
			// Find out whether textValue is in numeric format
			// line 39
			bool isHex = false;
			// Line 41: ([−])?
			do {
				la0 = src.LA0;
				if (la0 == '−')
					src.Skip();
				else if (la0 == -1 || la0 == '.' || la0 >= '0' && la0 <= '9')
					;
				else
					src.Error(0, "In rule 'CanPrintAsNumber', expected one of: [\\$.0-9−]");
			} while (false);
			// Line 42: ( BinNumber / HexNumber / DecNumber )
			la0 = src.LA0;
			if (la0 == '0') {
				switch (src.LA(1)) {
				case 'B': case 'b':
					BinNumber();
					break;
				case 'X': case 'x':
					{
						HexNumber();
						// line 43
						isHex = true;
					}
					break;
				case -1: case '\'': case '.': case '0':
				case '1': case '2': case '3': case '4':
				case '5': case '6': case '7': case '8':
				case '9': case 'E': case '_': case 'e':
					DecNumber();
					break;
				default:
					src.Error(1, "In rule 'CanPrintAsNumber', expected one of: [\\$'.0-9BEX_bex]");
					break;
				}
			} else if (la0 == '.' || la0 >= '1' && la0 <= '9')
				DecNumber();
			else
				src.Error(0, "In rule 'CanPrintAsNumber', expected one of: [.0-9]");
			src.Match(-1);
			// line 47
			return msgCounter.ErrorCount == 0 && 
			!(isHex && (firstTMChar >= 'a' && firstTMChar <= 'f' || firstTMChar >= 'A' && firstTMChar <= 'F'));
		}
	
	
		static void DecDigits()
		{
			int la0, la1;
			src.MatchRange('0', '9');
			// Line 51: ([0-9])*
			for (;;) {
				la0 = src.LA0;
				if (la0 >= '0' && la0 <= '9')
					src.Skip();
				else
					break;
			}
			// Line 51: greedy(['_] [0-9] ([0-9])*)*
			for (;;) {
				la0 = src.LA0;
				if (la0 == '\'' || la0 == '_') {
					la1 = src.LA(1);
					if (la1 >= '0' && la1 <= '9') {
						src.Skip();
						src.Skip();
						// Line 51: ([0-9])*
						for (;;) {
							la0 = src.LA0;
							if (la0 >= '0' && la0 <= '9')
								src.Skip();
							else
								break;
						}
					} else
						break;
				} else
					break;
			}
		}
		static readonly HashSet<int> HexDigit_set0 = LexerSource.NewSetOfRanges('0', '9', 'A', 'F', 'a', 'f');
	
		static void HexDigit()
		{
			src.Match(HexDigit_set0);
		}
	
		static void HexDigits()
		{
			int la0, la1;
			src.Match(HexDigit_set0);
			// Line 53: greedy([0-9A-Fa-f])*
			for (;;) {
				la0 = src.LA0;
				if (HexDigit_set0.Contains(la0))
					src.Skip();
				else
					break;
			}
			// Line 53: greedy(['_] [0-9A-Fa-f] greedy([0-9A-Fa-f])*)*
			for (;;) {
				la0 = src.LA0;
				if (la0 == '\'' || la0 == '_') {
					la1 = src.LA(1);
					if (HexDigit_set0.Contains(la1)) {
						src.Skip();
						src.Skip();
						// Line 53: greedy([0-9A-Fa-f])*
						for (;;) {
							la0 = src.LA0;
							if (HexDigit_set0.Contains(la0))
								src.Skip();
							else
								break;
						}
					} else
						break;
				} else
					break;
			}
		}
	
	
		static void DecNumber()
		{
			int la0;
			// Line 56: (DecDigits | [.] DecDigits => )
			la0 = src.LA0;
			if (la0 >= '0' && la0 <= '9')
				DecDigits();
			else if (la0 == '.') { } else
				src.Error(0, "In rule 'DecNumber', expected one of: [.0-9]");
			// Line 57: ([.] DecDigits)?
			do {
				switch (src.LA0) {
				case '.':
					{
						src.Skip();
						DecDigits();
					}
					break;
				case -1: case 'E': case 'e':
					;
					break;
				default:
					src.Error(0, "In rule 'DecNumber', expected one of: [\\$.Ee]");
					break;
				}
			} while (false);
			// Line 58: greedy([Ee] ([+\-])? DecDigits)?
			do {
				la0 = src.LA0;
				if (la0 == 'E' || la0 == 'e') {
					src.Skip();
					// Line 58: ([+\-])?
					do {
						la0 = src.LA0;
						if (la0 == '+' || la0 == '-')
							src.Skip();
						else if (la0 == -1 || la0 >= '0' && la0 <= '9')
							;
						else
							src.Error(0, "In rule 'DecNumber', expected one of: [\\$+\\-0-9]");
					} while (false);
					DecDigits();
				} else if (la0 == -1)
					;
				else
					src.Error(0, "In rule 'DecNumber', expected one of: [\\$Ee]");
			} while (false);
		}
	
		static void HexNumber()
		{
			int la0;
			src.Match('0');
			src.Match('X', 'x');
			// Line 62: (HexDigits | [.] HexDigits => )
			la0 = src.LA0;
			if (HexDigit_set0.Contains(la0))
				HexDigits();
			else if (la0 == '.') { } else
				src.Error(0, "In rule 'HexNumber', expected one of: [.0-9A-Fa-f]");
			// Line 63: ([.] ([Pp] | [0-9A-Fa-f]) => [.] greedy(HexDigits)? greedy([Pp] ([+\-])? DecDigits)?)?
			do {
				la0 = src.LA0;
				if (la0 == '.') {
					src.Skip();
					// Line 64: greedy(HexDigits)?
					do {
						switch (src.LA0) {
						case '0': case '1': case '2': case '3':
						case '4': case '5': case '6': case '7':
						case '8': case '9': case 'A': case 'B':
						case 'C': case 'D': case 'E': case 'F':
						case 'a': case 'b': case 'c': case 'd':
						case 'e': case 'f':
							HexDigits();
							break;
						case -1: case 'P': case 'p':
							;
							break;
						default:
							src.Error(0, "In rule 'HexNumber', expected one of: [\\$0-9A-FPa-fp]");
							break;
						}
					} while (false);
					// Line 65: greedy([Pp] ([+\-])? DecDigits)?
					do {
						la0 = src.LA0;
						if (la0 == 'P' || la0 == 'p') {
							src.Skip();
							// Line 65: ([+\-])?
							do {
								la0 = src.LA0;
								if (la0 == '+' || la0 == '-')
									src.Skip();
								else if (la0 == -1 || la0 >= '0' && la0 <= '9')
									;
								else
									src.Error(0, "In rule 'HexNumber', expected one of: [\\$+\\-0-9]");
							} while (false);
							DecDigits();
						} else if (la0 == -1)
							;
						else
							src.Error(0, "In rule 'HexNumber', expected one of: [\\$Pp]");
					} while (false);
				} else if (la0 == -1)
					;
				else
					src.Error(0, "In rule 'HexNumber', expected one of: [\\$.]");
			} while (false);
		}
	
		static void BinNumber()
		{
			int la0;
			src.Match('0');
			src.Match('B', 'b');
			// Line 70: (DecDigits | [.] DecDigits => )
			la0 = src.LA0;
			if (la0 >= '0' && la0 <= '9')
				DecDigits();
			else if (la0 == '.') { } else
				src.Error(0, "In rule 'BinNumber', expected one of: [.0-9]");
			// Line 71: ([.] DecDigits)?
			do {
				switch (src.LA0) {
				case '.':
					{
						src.Skip();
						DecDigits();
					}
					break;
				case -1: case 'P': case 'p':
					;
					break;
				default:
					src.Error(0, "In rule 'BinNumber', expected one of: [\\$.Pp]");
					break;
				}
			} while (false);
			// Line 72: greedy([Pp] ([+\-])? DecDigits)?
			do {
				la0 = src.LA0;
				if (la0 == 'P' || la0 == 'p') {
					src.Skip();
					// Line 72: ([+\-])?
					do {
						la0 = src.LA0;
						if (la0 == '+' || la0 == '-')
							src.Skip();
						else if (la0 == -1 || la0 >= '0' && la0 <= '9')
							;
						else
							src.Error(0, "In rule 'BinNumber', expected one of: [\\$+\\-0-9]");
					} while (false);
					DecDigits();
				} else if (la0 == -1)
					;
				else
					src.Error(0, "In rule 'BinNumber', expected one of: [\\$Pp]");
			} while (false);
		}
	}
}