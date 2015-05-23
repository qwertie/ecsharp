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

	/// <summary>This class's main job is to maintains a table of 
	/// <see cref="Precedence"/> values for LES operators. When you ask about a
	/// new operator, its precedence is cached for future reference.</summary>
	public class LesPrecedenceMap
	{
		[ThreadStatic]
		protected static LesPrecedenceMap _default;
		public static LesPrecedenceMap Default { get { 
			return _default = _default ?? new LesPrecedenceMap();
		} }

		public LesPrecedenceMap() { Reset(); }

		/// <summary>Forgets previously encountered operators to save memory.</summary>
		public void Reset() {
			this[OperatorShape.Suffix] = Pair.Create(PredefinedSuffixPrecedence.AsMutable(), P.Suffix2);
			this[OperatorShape.Prefix] = Pair.Create(PredefinedPrefixPrecedence.AsMutable(), P.BackslashWord);
			this[OperatorShape.Infix]  = Pair.Create(PredefinedInfixPrecedence.AsMutable(), P.BackslashWord);
			_suffixOpNames = null;
		}

		/// <summary>Gets the precedence of a prefix, suffix, or infix operator in 
		/// LES, under the assumption that the operator isn't surrounded in 
		/// backticks (in which case its precedence is always Backtick).</summary>
		/// <param name="op">Parsed form of the operator. op must be a Symbol, but 
		/// the parameter has type object to avoid casting Token.Value in the parser.</param>
		public Precedence Find(OperatorShape shape, object op, bool cacheWordOp = true)
		{
			var pair = this[shape];
			return FindPrecedence(pair.A, op, pair.B, cacheWordOp);
		}

		// Maps from Symbol to Precedence, paired with a default precedece for \word operators
		Pair<MMap<object, Precedence>, Precedence>[] _precedenceMap = new Pair<MMap<object, Precedence>, Precedence>[4];
		Pair<MMap<object, Precedence>, Precedence> this[OperatorShape s]
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
				{ S.Div,         P.Reserved    }, // /
				{ S.Backslash,   P.Reserved    }, // \
				{ S.LT,          P.Reserved    }, // <
				{ S.GT,          P.Reserved    }, // >
				{ S.QuestionMark,P.Reserved    }, // ?
				{ S.Assign,      P.Reserved    }, // =
			}.AsImmutable();

		protected static readonly Map<object, Precedence> PredefinedSuffixPrecedence =
			new MMap<object, Precedence>() {
				{ S.PreInc,     P.Primary }, // ++, never mind that it's called "pre"inc
				{ S.PreDec,     P.Primary }, // --
			}.AsImmutable();

		protected static readonly Map<object, Precedence> PredefinedInfixPrecedence =
			new MMap<object, Precedence>() {
				{ S.Dot,         P.Primary    }, // .
				{ S.QuickBind,   P.Primary    }, // =:
				{ S.Not,         P.Primary    }, // !
				{ S.NullDot,     P.NullDot    }, // ?.
				{ S.ColonColon,  P.NullDot    }, // ::
				{ S.DoubleBang,  P.DoubleBang }, // !!
				{ S.Exp,         P.Power      }, // **
				{ S.Mul,         P.Multiply   }, // *
				{ S.Div,         P.Multiply   }, // /
				{ S.Mod,         P.Multiply   }, // %
				{ S.Backslash,   P.Multiply   }, // \
				{ S.Shr,         P.Multiply   }, // >>
				{ S.Shl,         P.Multiply   }, // <<
				{ S.Add,         P.Add        }, // +
				{ S.Sub,         P.Add        }, // -
				{ S._RightArrow, P.Arrow      }, // ->
				{ S.LeftArrow,   P.Arrow      }, // <-
				{ S.AndBits,     P.AndBits    }, // &
				{ S.OrBits,      P.OrBits     }, // |
				{ S.XorBits,     P.OrBits     }, // ^
				{ S.NullCoalesce,P.OrIfNull   }, // ??
				{ S.DotDot,      P.Range      }, // ..
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
				{ S.Colon,       P.Reserved   }, // :
				{ S.Assign,      P.Assign     }, // =
				{ S.Lambda,      P.Lambda     }, // =>
				{ S.NotBits,     P.Reserved   }, // ~
			}.AsImmutable();
		
		protected Precedence FindPrecedence(MMap<object,Precedence> table, object symbol, Precedence @default, bool cacheWordOp)
		{
			// You can see the official rules in the LesPrecedence documentation.
			// Rule 1 (for >= <= != ==) is covered by the pre-populated contents 
			// of the table, and the pre-populated table helps interpret rules 
			// 3-4 also.
			Precedence prec;
			if (table.TryGetValue(symbol, out prec))
				return prec;

			string sym = symbol.ToString();
			if (sym == "") return prec; // yikes!
			char first = sym[0], last = sym[sym.Length - 1];
			// All one-character operators should be found in the table

			if (table == this[OperatorShape.Infix].A && last == '=')
				return table[symbol] = table[S.Assign];
			
			var twoCharOp = GSymbol.Get(first.ToString() + last);
			if (table.TryGetValue(twoCharOp, out prec))
				return table[symbol] = prec;

			var oneCharOp = GSymbol.Get(last.ToString());
			if (table.TryGetValue(oneCharOp, out prec))
				return table[symbol] = prec;

			// Default precedence is used for word operators
			if (cacheWordOp)
				table[symbol] = @default;
			return @default;
		}

		static readonly BitArray OpChars = GetOpChars();
		private static BitArray GetOpChars()
		{
			var map = new BitArray(128);
			map['~']  = map['!'] = map['%'] = map['^'] = map['&'] = map['*'] = true;
			map['\\'] = map['-'] = map['+'] = map['='] = map['|'] = map['<'] = true;
			map['>']  = map['/'] = map['?'] = map[':'] = map['.'] = map['$'] = true;
			return map;
		}
		/// <summary>Returns true if this character is one of those that operators are normally made out of in LES.</summary>
		public static bool IsOpChar(char c)
		{
			return OpChars[c];
		}
		/// <summary>Returns true if the given Symbol can be printed as an operator 
		/// without escaping it.</summary>
		/// <remarks>The parser should read something like <c>+/*</c> as an operator
		/// with three characters, rather than "+" and a comment, but the printer 
		/// should be conservative, so this function returns false in such a case:
		/// "Be liberal in what you accept, and conservative in what you produce."</remarks>
		public static bool IsNaturalOperator(Symbol s)
		{
			string name = s.Name;
			if (name.Length == 0)
				return false;
			for (int i = 0; ;) {
				char c = name[i];
				if (!IsOpChar(c))
					return false;
				if (++i == name.Length)
					break;
				if (c == '/' && (name[i] == '/' || name[i] == '*'))
					return false; // oops, looks like a comment
			}
			return true;
		}
		/// <summary>Returns true if a given Les operator can only be printed with 
		/// backticks, because a leading backslash is insufficient.</summary>
		public static bool RequiresBackticks(Symbol s)
		{
			string name = s.Name;
			for (int i = 0; i < name.Length; i++)
				if (!IsOpChar(name[i]) && !char.IsLetter(name[i]) && !char.IsDigit(name[i]) 
					&& name[i] != '_' && name[i] != '\'')
					return true;
			return name.Length == 0;
		}

		/// <summary>Given a normal operator symbol like <c>(Symbol)"++"</c>, gets
		/// the suffix form of the name, such as <c>(Symbol)"suf++"</c>. In LES,
		/// operators that end with a backslash (\) are always suffix operators, so
		/// their names are left unchanged.</summary>
		/// <remarks>op must be a Symbol, but the parameter has type object to avoid casting Token.Value in the parser.</remarks>
		public Symbol ToSuffixOpName(object symbol)
		{
			_suffixOpNames = _suffixOpNames ?? new Dictionary<object, Symbol>();
			Symbol name;
			if (_suffixOpNames.TryGetValue(symbol, out name))
				return name;

			var was = symbol.ToString();
			if (was.EndsWith("\\"))
				return _suffixOpNames[symbol] = (Symbol)symbol;
			else
				return _suffixOpNames[symbol] = GSymbol.Get(@"suf" + symbol.ToString());
		}

		/// <summary>Decides whether the name appears to represent a suffix operator 
		/// of the form <c>sufOP</c> or <c>OP\</c>.</summary>
		/// <param name="name">Potential operator name to evaluate.</param>
		/// <param name="bareName">If the name starts with "suf", this is the same 
		/// name without "suf", otherwise it is set to <c>name</c> itself. This
		/// output is calculated even if the function returns false.</param>
		/// <param name="checkNatural">If true, part of the requirement for 
		/// returning true will be that IsNaturalOperator(bareName) == true.</param>
		public static bool IsSuffixOperatorName(Symbol name, out Symbol bareName, bool checkNatural)
		{
			if (name.Name.StartsWith("suf"))
				bareName = (Symbol)name.Name.Substring(3);
			else {
				bareName = name;
				if (!name.Name.EndsWith(@"\"))
					return false;
			}
			return !checkNatural || IsNaturalOperator(bareName);
		}
	}
}
