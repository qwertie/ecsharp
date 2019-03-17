using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using System.Collections;

namespace Loyc.Syntax.Les
{
	using S = CodeSymbols;
	using P = LesPrecedence;

	/// <summary>This class's main job is to maintain a table of 
	/// <see cref="Precedence"/> values for LES operators. When you ask about a
	/// new operator, its precedence is chosen by this class and cached for 
	/// future reference.</summary>
	public class Les2PrecedenceMap
	{
		[ThreadStatic]
		protected static Les2PrecedenceMap _default;
		public static Les2PrecedenceMap Default { get { 
			return _default = _default ?? new Les2PrecedenceMap();
		} }

		public Les2PrecedenceMap() { Reset(); }

		/// <summary>Forgets previously encountered operators to save memory.</summary>
		public virtual void Reset() {
			this[OperatorShape.Suffix] = Pair.Create(PredefinedSuffixPrecedence.AsMutable(), P.Primary);
			this[OperatorShape.Prefix] = Pair.Create(PredefinedPrefixPrecedence.AsMutable(), P.Other);
			this[OperatorShape.Infix]  = Pair.Create(PredefinedInfixPrecedence .AsMutable(), P.Other);
			_suffixOpNames = null;
		}

		/// <summary>Gets the precedence in LES of a prefix, suffix, or infix operator.</summary>
		/// <param name="shape">Specifies which precedence table and rules to use 
		/// (Prefix, Suffix or Infix). Note: when this is Suffix, "suf" must not be 
		/// part of the name in <c>op</c> (see <see cref="IsSuffixOperatorName"/>)</param>
		/// <param name="op">Parsed form of the operator. op must be a Symbol, but 
		/// the parameter has type object to avoid casting Token.Value in the parser.</param>
		public Precedence Find(OperatorShape shape, object op, bool cacheWordOp = true, bool les3InfixOp = false)
		{
			var pair = this[shape];
			return FindPrecedence(pair.A, op, pair.B, cacheWordOp, les3InfixOp);
		}

		// Maps from Symbol to Precedence, paired with a default precedece for \word operators
		protected Pair<MMap<object, Precedence>, Precedence>[] _precedenceMap = new Pair<MMap<object, Precedence>, Precedence>[4];
		protected Pair<MMap<object, Precedence>, Precedence> this[OperatorShape s]
		{
			get { return _precedenceMap[(int)s + 1]; }
			set { _precedenceMap[(int)s + 1] = value; }
		}

		// All the keys are Symbols, but we use object as the key type to avoid casting Token.Value
		Dictionary<object, Symbol> _suffixOpNames;

		// All the keys are Symbols, but we use object as the key type to avoid casting Token.Value
		protected static readonly Map<object, Precedence> PredefinedPrefixPrecedence = 
			new MMap<object, Precedence>() {
				{ S.Substitute,  P.Substitute  }, // $
				{ S.Dot,         P.Substitute  }, // hmm, I might repurpose '.' with lower precedence to remove the spacing rule
				{ S.Colon,       P.Substitute  }, // :
				{ S.NotBits,     P.Prefix      }, // ~
				{ S.Not,         P.Prefix      }, // !
				{ S.Mod,         P.Prefix      }, // %
				{ S.XorBits,     P.Prefix      }, // ^
				{ S._AddressOf,  P.Prefix      }, // &
				{ S._Dereference,P.Prefix      }, // *
				{ S._UnaryPlus,  P.Prefix      }, // +
				{ S._Negate,     P.Prefix      }, // -
				{ S.DotDot,      P.PrefixDots  }, // ..
				{ S.OrBits,      P.PrefixOr    }, // |
				{ S.Div,         P.Prefix      }, // /
				//{ S.LT,          P.Reserved    }, // <
				//{ S.GT,          P.Reserved    }, // >
				//{ S.QuestionMark,P.Reserved    }, // ?
				//{ S.Assign,      P.Reserved    }, // =
			}.AsImmutable();

		protected static readonly Map<object, Precedence> PredefinedSuffixPrecedence =
			new MMap<object, Precedence>() {
				{ S.PreInc,     P.Primary }, // ++, never mind that it's called "pre"inc
				{ S.PreDec,     P.Primary }, // --
				{ S.PreBangBang,P.Primary }, // !!
			}.AsImmutable();

		protected static readonly Map<object, Precedence> PredefinedInfixPrecedence =
			new MMap<object, Precedence>() {
				{ S.Dot,         P.Primary    }, // .
				{ S.QuickBind,   P.Primary    }, // =:
				//{ (Symbol)"'!.", P.Primary    }, // !. (redundant now that the final char determines the precedence)
				{ S.Not,         P.Of         }, // !
				{ S.NullDot,     P.NullDot    }, // ?.
				{ S.ColonColon,  P.NullDot    }, // ::
				{ S.Exp,         P.Power      }, // **
				{ S.Mul,         P.Multiply   }, // *
				{ S.Div,         P.Multiply   }, // /
				{ S.Mod,         P.Multiply   }, // %
				//{ S.Backslash,   P.Multiply   }, // \  (no longer supported)
				{ S.Shr,         P.Shift      }, // >>
				{ S.Shl,         P.Shift      }, // <<
				{ S.Add,         P.Add        }, // +
				{ S.Sub,         P.Add        }, // -
				{ S._RightArrow, P.Arrow      }, // ->
				{ S.LeftArrow,   P.Arrow      }, // <-
				{ S.AndBits,     P.AndBitsLESv2 }, // &
				{ S.OrBits,      P.OrBitsLESv2 }, // |
				{ S.XorBits,     P.OrBitsLESv2 }, // ^
				{ S.NullCoalesce,P.OrIfNull   }, // ??
				{ S.DotDot,      P.Range      }, // ..
				{ (Symbol)"'.<", P.Range      }, // .< (controls the precedence of ..<)
				{ S.GT,          P.Compare    }, // >
				{ S.LT,          P.Compare    }, // <
				{ S.LE,          P.Compare    }, // <=
				{ S.GE,          P.Compare    }, // >=
				{ S.Eq,          P.Compare    }, // ==
				{ S.Neq,         P.Compare    }, // !=
				{ S.And,         P.And        }, // &&
				{ S.Or,          P.Or         }, // ||
				{ S.Xor,         P.Or         }, // ^^
				{ S.QuestionMark,P.IfElse     }, // ?
				{ S.Colon,       P.IfElse     }, // :
				{ S.Assign,      P.Assign     }, // =
				{ S.Lambda,      P.Lambda     }, // =>
				{ (Symbol)"'|>", P.Triangle   }, // |>
				{ (Symbol)"'<|", P.Triangle   }, // <|
				{ S.NotBits,     P.Other      }, // ~
			}.AsImmutable();
		
		protected Precedence FindPrecedence(MMap<object,Precedence> table, object symbol, Precedence @default, bool cacheWordOp, bool les3InfixOp = false)
		{
			// You can see the official rules in the LesPrecedence documentation.
			
			// Rule 1 (for >= <= != ==) is covered by the pre-populated contents 
			// of the table, and the pre-populated table helps interpret other 
			// rules too.
			CheckParam.IsNotNull("symbol", symbol);
			Precedence prec;
			if (table.TryGetValue(symbol, out prec))
				return prec;

			string sym = symbol.ToString();
			if (sym.Length <= 1 || sym[0] != '\'')
				return @default; // empty or non-operator

			// Note: all one-character operators should have been found in the table
			char first = sym[1], last = sym[sym.Length - 1];
			bool isInfix = table == this[OperatorShape.Infix].A;

			if (les3InfixOp)
			{
				// Check for lowercase word prefix
				int i = 1;
				while (first >= 'a' && first <= 'z' || first == '_') {
					if (++i == sym.Length) {
						if (cacheWordOp)
							table[symbol] = P.LowerKeyword;
						return P.LowerKeyword;
					}
					first = sym[i];
				}
				
				if (i + 1 == sym.Length) {
					// After the word is a one-character op. See if it is in the table
					var oneCharOp_ = GSymbol.Get("'" + first);
					if (table.TryGetValue(oneCharOp_, out prec))
						return prec;
				}
			}

			if (isInfix && last == '=') {
				if (first == '=' || first == '!')
					return table[symbol] = P.Compare;
				else
					return table[symbol] = P.Assign;
			}

			var twoCharOp = GSymbol.Get("'" + first + last);
			if (table.TryGetValue(twoCharOp, out prec))
				return table[symbol] = prec;

			var oneCharOp = GSymbol.Get("'" + last);
			if (table.TryGetValue(oneCharOp, out prec))
				return table[symbol] = prec;

			if (isInfix && char.IsLower(first))
				return table[symbol] = P.LowerKeyword;

			// Default precedence is used for anything else
			if (cacheWordOp)
				return table[symbol] = @default;
			return @default;
		}

		static readonly BitArray OpChars = GetOpChars();
		static readonly BitArray OpCharsEx = GetOpCharsEx();
		private static BitArray GetOpChars()
		{
			var map = new BitArray(128);
			map['~']  = map['!'] = map['%'] = map['^'] = map['&'] = map['*'] = true;
			map['-'] = map['+'] = map['='] = map['|'] = map['<'] = map['>'] = true;
			map['/'] = map['?'] = map[':'] = map['.'] = true;
			return map;
		}
		private static BitArray GetOpCharsEx()
		{
			var map = GetOpChars();
			map['$'] = true;
			for (char c = 'a'; c <= 'z'; c++)
				map[c] = true;
			for (char c = 'A'; c <= 'Z'; c++)
				map[c] = true;
			for (char c = '0'; c <= '9'; c++)
				map[c] = true;
			return map;
		}
		/// <summary>Returns true if this character is one of those that operators are normally made out of in LES.</summary>
		public static bool IsOpChar(char c)
		{
			return (uint)c < (uint)OpChars.Count ? OpChars[c] : false;
		}
		/// <summary>Returns true if this character is one of those that can appear 
		/// in "extended" LESv3 operators that start with an apostrophe.</summary>
		public static bool IsOpCharEx(char c)
		{
			return (uint)c < (uint)OpCharsEx.Count ? OpCharsEx[c] : false;
		}
		
		/// <summary>Returns true if the given Symbol can be printed as an operator 
		/// without escaping it in LESv2.</summary>
		/// <remarks>The parser should read something like <c>+/*</c> as an operator
		/// with three characters, rather than "+" and a comment, but the printer 
		/// is more conservative, so this function returns false in such a case.</remarks>
		public static bool IsNaturalOperator(string name)
		{
			if (name.Length <= 1 || name[0] != '\'')
				return false; // optimized path
			return IsOperator(name, OpChars, true, "'") || name == "'$" || IsOperator(name, OpChars, true, "'$");
		}

		/// <summary>Returns true if the given Symbol can ever be used as an "extended" 
		/// binary operator in LESv3.</summary>
		/// <remarks>A binary operator's length must be between 2 and 255, its name must
		/// start with an apostrophe, and each remaining character must be punctuation marks 
		/// from natural operators and/or characters from the set 
		/// {'#', '_', 'a'..'z', 'A'..'Z', '0'..'9', '$'}.</remarks>
		public static bool IsExtendedOperator(string name, string expectPrefix = "'")
		{
			return IsOperator(name, OpCharsEx, false, expectPrefix);
		}

		static bool IsOperator(string name, BitArray opChars, bool rejectComment, string expectPrefix)
		{
			int i = expectPrefix.Length;
			if (!name.StartsWith(expectPrefix))
				return false;
			if (i >= name.Length || name.Length > 255)
				return false;
			for (;;) {
				char c = name[i];
				if ((uint)c > (uint)opChars.Count || !opChars[c])
					return false;
				if (++i == name.Length)
					break;
				if (c == '/' && rejectComment && (name[i] == '/' || name[i] == '*'))
					return false; // oops, looks like a comment
			}
			return true;
		}

		/// <summary>Given a normal operator symbol like <c>(Symbol)"'++"</c>, gets
		/// the suffix form of the name, such as <c>(Symbol)"'++suf"</c>.</summary>
		/// <remarks>op must be a Symbol, but the parameter has type object to avoid casting Token.Value in the parser.</remarks>
		public Symbol ToSuffixOpName(object symbol)
		{
			_suffixOpNames = _suffixOpNames ?? new Dictionary<object, Symbol>();
			Symbol name;
			if (_suffixOpNames.TryGetValue(symbol, out name))
				return name;

			var was = symbol.ToString();
			return _suffixOpNames[symbol] = GSymbol.Get(symbol.ToString() + "suf");
		}

		/// <summary>Decides whether the name appears to represent a suffix operator 
		/// of the form <c>sufOP</c> or <c>OP\</c>.</summary>
		/// <param name="name">Potential operator name to evaluate.</param>
		/// <param name="bareName">If the name ends with "suf", this is the same 
		/// name without "suf", otherwise it is set to <c>name</c> itself. This
		/// output is calculated even if the function returns false.
		/// IsNaturalOperator(bareName) is true if the function returns true.</param>
		public static bool IsSuffixOperatorName(Symbol name, out Symbol bareName)
		{
			if (name.Name.EndsWith("suf")) {
				bareName = (Symbol)name.Name.Substring(0, name.Name.Length - 3);
				return IsNaturalOperator(bareName.Name);
			} else {
				bareName = name;
				return false;
			}
		}
	}

	public class Les3PrecedenceMap : Les2PrecedenceMap
	{
		[ThreadStatic]
		protected new static Les3PrecedenceMap _default;
		public new static Les3PrecedenceMap Default { get { 
			return _default = _default ?? new Les3PrecedenceMap();
		} }
		public override void Reset() {
			base.Reset();
			this[OperatorShape.Infix].Item1[S.ColonColon] = LesPrecedence.Primary;
			this[OperatorShape.Infix].Item1[S.AndBits] = LesPrecedence.AndBits;
			this[OperatorShape.Infix].Item1[S.OrBits] = LesPrecedence.OrBits;
			this[OperatorShape.Infix].Item1[S.XorBits] = LesPrecedence.OrBits;
		}
	}
}
