using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections;
using Loyc.Collections.Impl;
using S = Loyc.Ecs.EcsCodeSymbols;
using EP = Loyc.Ecs.EcsPrecedence;

namespace Loyc.Ecs
{
	// This file: code for printing expressions and types
	public partial class EcsNodePrinter
	{
		#region Sets and dictionaries of operators

		// Simple statements with the syntax "keyword;" or "keyword expr;" can also be
		// used as expressions, except for `using` (S.Import).
		internal static readonly HashSet<Symbol> SimpleStmts = new HashSet<Symbol>(new[] {
			S.Break, S.Continue, S.Goto, S.GotoCase, S.Return, S.Throw, S.Import
		});

		static readonly Dictionary<Symbol,Precedence> PrefixOperators = Dictionary(
			// This is a list of unary prefix operators only. Does not include the
			// binary prefix operator 'cast or the unary suffix operators ++ and --.
			// Although @`.` can be a prefix operator, it is not included in this list
			// because it needs special treatment because its precedence is higher
			// than EP.Primary (i.e. above prefix notation). Therefore, it's printed
			// as an identifier if possible (e.g. @`.`(a)(x) is printed ".a(x)") and
			// uses prefix notation if not (e.g. @`.`(a(x)) must be in prefix form.)
			//
			// The substitute operator $ also has higher precedence than Primary, 
			// but its special treatment is in the parser: the parser produces the
			// same tree for $(x) and $x, unlike e.g. ++(x) and ++x which are 
			// different trees. Therefore we can treat $ as a normal operator in
			// the printer except that we must emit parenthesis around the argument
			// if it is anything but a simple identifier (CanAppearIn detects when
			// this is necessary.)
			P(S._Negate,    EP.Prefix), P(S._UnaryPlus,   EP.Prefix), P(S.NotBits, EP.Prefix), 
			P(S.Not,        EP.Prefix), P(S.PreInc,       EP.Prefix), P(S.PreDec,  EP.Prefix),
			P(S._AddressOf, EP.Prefix), P(S._Dereference, EP.Prefix), P(S.Forward, EP.Forward), 
			P(S.DotDot,     EP.Prefix), P(S.DotDotDot,    EP.Prefix), 
			P(S.Dot,    EP.Substitute), P(S.Substitute, EP.Substitute),
			P(S.LT, EP.Compare), P(S.GT, EP.Compare),
			P(S.LE, EP.Compare), P(S.GE, EP.Compare), P(S.PatternNot, EP.PatternNot)
		);

		static readonly Dictionary<Symbol,Precedence> InfixOperators = Dictionary(
			// This is a list of infix binary opertors only. Does not include the
			// conditional operator `?` or non-infix binary operators such as a[i].
			// Comma is not an operator at all and generally should not occur. 
			// '=>' is not included because it has a special 'delegate() {}' form
			// and is not handled by the normal infix operator printer. Likewise, C# 9
			// `and`/`or` pattern operators, `with`, and `switch` operators have their 
			// own special handlers.
			// Note: I cancelled my plan to add a binary ~ operator because it would
			//       change the meaning of (x)~y from a type cast to concatenation.
			P(S.Dot, EP.Primary),      P(S.ColonColon, EP.Primary), P(S.QuickBind, EP.Primary), 
			P(S.RightArrow, EP.Primary), P(S.NullDot, EP.NullDot),
			P(S.Exp, EP.Power),        P(S.Mul, EP.Multiply),
			P(S.Div, EP.Multiply),     P(S.Mod, EP.Multiply),
			P(S.Add, EP.Add),          P(S.Sub, EP.Add),        P(S.NotBits, EP.Add),
			P(S.Shl, EP.Shift),        P(S.Shr, EP.Shift),
			P(S.DotDot, EP.Range),     P(S.DotDotDot, EP.Range),
			P(S.LE, EP.Compare),       P(S.GE, EP.Compare),
			P(S.LT, EP.Compare),       P(S.GT, EP.Compare),
			P(S.Is, EP.Is),            P(S.As, EP.AsUsing),       P(S.UsingCast, EP.AsUsing),
			P(S.Eq, EP.Equals),        P(S.NotEq, EP.Equals),     P(S.In, EP.Equals),
			P(S.AndBits, EP.AndBits),  P(S.XorBits, EP.XorBits),  P(S.OrBits, EP.OrBits), 
			P(S.And, EP.And),          P(S.Or, EP.Or),            P(S.Xor, EP.Or),
			P(S.Assign, EP.Assign),    P(S.MulAssign, EP.Assign),      P(S.DivAssign, EP.Assign),
			P(S.ModAssign, EP.Assign),      P(S.SubAssign, EP.Assign), P(S.AddAssign, EP.Assign), 
			P(S.ConcatAssign, EP.Assign),   P(S.ShlAssign, EP.Assign), P(S.ShrAssign, EP.Assign), 
			P(S.ExpAssign, EP.Assign),      P(S.XorBitsAssign, EP.Assign),
			P(S.AndBitsAssign, EP.Assign),  P(S.OrBitsAssign, EP.Assign), 
			P(S.NullCoalesce, EP.OrIfNull), P(S.NullCoalesceAssign, EP.Assign),
			P(S.Compare, EP.Compare3Way),   P(S.ForwardPipeArrow, EP.PipeArrow),
			P(S.ForwardAssign, EP.PipeArrow),
			P(S.NullForwardPipeArrow, EP.PipeArrow),
			P(S.ForwardNullCoalesceAssign, EP.PipeArrow),
			P(S.When, EP.WhenWhere), P(S.WhereOp, EP.WhenWhere)
		);

		static readonly Dictionary<Symbol,Precedence> CastOperators = Dictionary(
			P(S.Cast, EP.Prefix),         // (Foo)x      (preferred form)
			P(S.As, EP.AsUsing),        // x as Foo    (preferred form)
			P(S.UsingCast, EP.AsUsing)  // x using Foo (preferred form)
		);

		static readonly HashSet<Symbol> ListOperators = new HashSet<Symbol>(new[] {
			S.Tuple, S.Braces, S.ArrayInit });

		static readonly Dictionary<Symbol,Precedence> SpecialCaseOperators = Dictionary(
			// Operators that need special treatment (neither prefix nor infix nor casts)
			// ?  []  suf++  suf--  'of  .  'isLegal  'new
			P(S.QuestionMark,EP.IfElse),  // a?b:c
			P(S.IndexBracks, EP.Primary), // a[]
			P(S.NullIndexBracks, EP.Primary), // a?[] (C# 6 feature)
			P(S.PostInc,     EP.Primary), // x++
			P(S.PostDec,     EP.Primary), // x--
			P(S.IsLegal,     EP.Compare)  // x is legal
			//P(S.New,         EP.Primary),
			//P(S.Lambda,      EP.Substitute) // delegate(int x) { return x+1; }
		);

		static readonly HashSet<Symbol> CallOperators = new HashSet<Symbol>(new[] {
			S.Typeof, S.Checked, S.Unchecked, S.Default, S.Sizeof
		});


		delegate bool OperatorPrinter(EcsNodePrinter @this, Precedence mainPrec);
		static Dictionary<Symbol, Pair<Precedence, OperatorPrinter>> OperatorPrinters = OperatorPrinters_();
		static Dictionary<Symbol, Pair<Precedence, OperatorPrinter>> OperatorPrinters_()
		{
			// Build a dictionary of printers for each operator name.
			var d = new Dictionary<Symbol, Pair<Precedence, OperatorPrinter>>();
			
			// Create open delegates to the printers for various kinds of operators
			var prefix = OpenDelegate<OperatorPrinter>(nameof(AutoPrintPrefixUnaryOperator));
			var infix = OpenDelegate<OperatorPrinter>(nameof(AutoPrintInfixBinaryOperator));
			var both = OpenDelegate<OperatorPrinter>(nameof(AutoPrintPrefixOrInfixOperator));
			var throwEtc = OpenDelegate<OperatorPrinter>(nameof(AutoPrintPrefixReturnThrowEtc));
			var cast = OpenDelegate<OperatorPrinter>(nameof(AutoPrintCastOperator));
			var isOp = OpenDelegate<OperatorPrinter>(nameof(AutoPrintIsOperator));
			var wordOp = OpenDelegate<OperatorPrinter>(nameof(AutoPrintBinaryWordOperator));
			var list = OpenDelegate<OperatorPrinter>(nameof(AutoPrintListOperator));
			var other = OpenDelegate<OperatorPrinter>(nameof(AutoPrintOtherSpecialOperator));
			var call = OpenDelegate<OperatorPrinter>(nameof(AutoPrintCallOperator));
			d.Add(S.Of, Pair.Create(EP.Of, OpenDelegate<OperatorPrinter>(nameof(AutoPrintOfOperator))));
			d.Add(S.Linq, Pair.Create(EP.Primary, OpenDelegate<OperatorPrinter>(nameof(AutoPrintLinqExpression))));

			foreach (var p in PrefixOperators)
				d.Add(p.Key, Pair.Create(p.Value, prefix));
			foreach (var p in InfixOperators)
				if (d.ContainsKey(p.Key))
					d[p.Key] = Pair.Create(p.Value, both); // both prefix and infix
				else
					d.Add(p.Key, Pair.Create(p.Value, infix));
			foreach (Symbol op in SimpleStmts)
				if (op != S.Import)
					d[op] = Pair.Create(EcsPrecedence.Lambda, throwEtc);
			foreach (var p in CastOperators)
				d[p.Key] = Pair.Create(p.Value, cast);
			d[S.Is] = Pair.Create(EP.Is, isOp);
			foreach (Symbol op in ListOperators)
				d[op] = Pair.Create(Precedence.MaxValue, list);
			foreach (var p in SpecialCaseOperators)
				d.Add(p.Key, Pair.Create(p.Value, other));

			// Other special cases
			foreach (var op in CallOperators)
				d.Add(op, Pair.Create(Precedence.MaxValue, call));
			d[S.SwitchOp]   = Pair.Create(EP.Switch, wordOp);
			d[S.With]       = Pair.Create(EP.Switch, wordOp);
			d[S.When]       = Pair.Create(EP.WhenWhere, wordOp);
			d[S.WhereOp]    = Pair.Create(EP.WhenWhere, wordOp);
			d[S.New]        = Pair.Create(EP.Primary, OpenDelegate<OperatorPrinter>(nameof(AutoPrintNewOperator)));
			d[S.Lambda]     = Pair.Create(EP.Lambda, OpenDelegate<OperatorPrinter>(nameof(AutoPrintLambdaFunction)));
			d[S.RawText]    = Pair.Create(EP.Substitute, OpenDelegate<OperatorPrinter>(nameof(PrintRawText)));
			d[S.CsRawText]  = Pair.Create(EP.Substitute, OpenDelegate<OperatorPrinter>(nameof(PrintRawText)));
			d[S.NamedArg]   = Pair.Create(StartExpr, OpenDelegate<OperatorPrinter>(nameof(AutoPrintNamedArg)));
			d[S.Property]   = Pair.Create(StartExpr, OpenDelegate<OperatorPrinter>(nameof(AutoPrintPropDeclExpr)));
			d[S.Deconstruct] = Pair.Create(StartExpr, OpenDelegate<OperatorPrinter>(nameof(AutoPrintDeconstructOperator)));
			d[S.PatternNot] = Pair.Create(EP.PatternNot, OpenDelegate<OperatorPrinter>(nameof(AutoPrintPatternUnaryOperator)));
			d[S.PatternAnd] = Pair.Create(EP.PatternAnd, OpenDelegate<OperatorPrinter>(nameof(AutoPrintPatternAndOrOperator)));
			d[S.PatternOr]  = Pair.Create(EP.PatternOr,  OpenDelegate<OperatorPrinter>(nameof(AutoPrintPatternAndOrOperator)));

			return d;
		}

		#endregion

		static readonly int MinPrec = Precedence.MinValue.Lo;
		/// <summary>Context: beginning of statement (Named argument operator not supported, allow multiple #var decl)</summary>
		internal static readonly Precedence StartStmt      = new Precedence(MinPrec);
		/// <summary>Context: beginning of expression (#var must have initial value)</summary>
		internal static readonly Precedence StartExpr      = new Precedence(MinPrec+1);
		/// <summary>Context: middle of expression, top level (#var and named arguments not supported)</summary>
		internal static readonly Precedence ContinueExpr   = new Precedence(MinPrec+2);

		void PrintExpr(LNode n) => PrintExpr(n, _context);
		void PrintExpr(LNode n, Precedence context, Ambiguity flags = 0)
		{
			_out.FlushIndent();
			using (With(n, context, flags))
				PrintCurrentExpr();
		}

		protected internal void PrintCurrentExpr()
		{
			if (!_n.IsCall)
			{
				PrintInPrefixNotation(skipAttrs: false);
			}
			else
			{
				if (!EP.Primary.CanAppearIn(_context) && !_n.IsParenthesizedExpr())
				{
					// Above EP.Primary (inside '$' or unary '.'), we can't use prefix 
					// notation or most other operators without parens.
					// @'of and '$ are notable exceptions, and we must not wrap them 
					// in parentheses in case they appear in a type context:
					if (_n.CallsMin(S.Of, 1))
						if (AutoPrintOfOperator(EcsPrecedence.Of))
							return;
					if (_n.Calls(S.Substitute, 1))
						if (AutoPrintPrefixUnaryOperator(EcsPrecedence.Substitute))
							return;

					if (_o.AllowChangeParentheses || _context.Left > EP.Primary.Left) {
						PrintWithinParens(ParenFor.Grouping, _n);
						return;
					} else if (!_o.DropNonDeclarationAttributes)
						_flags |= Ambiguity.ForceAttributeList;
				}

				NodeStyle style = _n.BaseStyle;
				if (style == NodeStyle.PrefixNotation && !_o.PreferPlainCSharp)
					PrintInPrefixNotation(skipAttrs: false);
				else {
					int inParens = 0;
					if (_name == S.Var && IsVariableDecl(false, true)) {
						if (!_o.DropNonDeclarationAttributes) {
							if (!Flagged(Ambiguity.AllowUnassignedVarDecl | Ambiguity.TypeContext) && !IsVariableDecl(false, false) && !_n.Attrs.Any(a => a.IsIdNamed(S.Ref) || a.IsIdNamed(S.Out)))
								_flags |= Ambiguity.ForceAttributeList;
							else if (!_context.RangeEquals(StartExpr) && !_context.RangeEquals(StartStmt) && !_n.IsParenthesizedExpr() && (_flags & Ambiguity.ForEachInitializer) == 0)
								_flags |= Ambiguity.ForceAttributeList;
						}
						if ((_flags & Ambiguity.IsPattern) == 0)
							inParens = PrintAttrs(AttrStyle.IsDefinition);
						PrintVariableDecl(false);
					} else {
						inParens = PrintAttrs(AttrStyle.AllowKeywordAttrs);
						do {
							if (AutoPrintOperator())
								break;
							if (style == NodeStyle.Special || _name == S.SwitchStmt)
								if (AutoPrintMacroBlockCall(true))
									break;
							PrintInPrefixNotation(skipAttrs: true);
						} while (false);
					}
					WriteCloseParens(inParens);
				}
			}
			if (_context.Lo != StartStmt.Lo)
				PrintTrivia(trailingTrivia: true);
		}

		// Checks if an operator with precedence 'prec' can appear in this context.
		bool CanAppearHere(Precedence prec, out bool extraParens, bool prefix = false)
		{
			extraParens = false;
			if (prec.CanAppearIn(_context, prefix) && (prefix || _o.MixImmiscibleOperators || prec.CanMixWith(_context)))
				return true;
			if (_n.IsParenthesizedExpr())
				return true;
			if (_o.AllowChangeParentheses || !EP.Primary.CanAppearIn(_context)) {
				Trace.WriteLineIf(!_o.AllowChangeParentheses, "Forced to write node in parens");
				return extraParens = true;
			}
			return false;
		}
		// Checks if an operator that may or may not be configured to output in 
		// `backtick notation` can appear in this context; this method may toggle
		// backtick notation to make it acceptable (in terms of precedence).
		bool CanAppearHere(ref Precedence prec, out bool extraParens, ref bool backtick, bool prefix = false)
		{
			var altPrec = EP.Backtick;
			if (backtick) G.Swap(ref prec, ref altPrec);
			if (CanAppearHere(prec, out extraParens, prefix && !backtick))
				return true;

			backtick = !backtick;
			G.Swap(ref prec, ref altPrec);
			return CanAppearHere(prec, out extraParens, prefix && !backtick);
		}

		private bool AutoPrintOperator()
		{
			if (!_n.IsCall || !HasSimpleHeadWPA(_n))
				return false;
			Pair<Precedence, OperatorPrinter> info;
			if (OperatorPrinters.TryGetValueSafe(_name, out info))
				return info.Item2(this, info.Item1);
			else if (_n.BaseStyle == NodeStyle.Operator)
			{
				if (_n.ArgCount == 2)
					using (WithFlags(_flags | Ambiguity.UseBacktick))
						return AutoPrintInfixBinaryOperator(EP.Backtick);
				//if (_n.ArgCount == 1)
				//	return AutoPrintPrefixUnaryOperator(EP.Backtick, context, flags | Ambiguity.UseBacktick);
			}
			return false;
		}

		public bool IsPrefixOperator(LNode n, bool checkName)
		{
			if (n.ArgCount != 1)
				return false;
			// Attributes on the child disqualify operator notation (except \)
			var name = n.Name;
			if (HasPAttrs(n.Args[0]) && name != S.Substitute)
				return false;
			if (checkName && !PrefixOperators.ContainsKey(name))
				return false;
			return true;
		}

		// These methods should not really be public, but they are found via 
		// reflection and must be public for compatibility with partial-trust 
		// environments; therefore we hide them from IntelliSense instead.
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintPrefixUnaryOperator(Precedence precedence)
		{
			if (!IsPrefixOperator(_n, (_flags & Ambiguity.CastRhs) != 0))
				return false;
			var arg = _n.Args[0];

			bool needParens;
			if (CanAppearHere(precedence, out needParens, true))
			{
				// Check for the ambiguous case of (Foo)-x, (Foo)+x, (Foo) .x; (Foo)*x and (Foo)&x are OK
				if ((_flags & Ambiguity.CastRhs) != 0 && !needParens && (
					_name == S.Dot || _name == S.PreInc || _name == S.PreDec || 
					_name == S._UnaryPlus || _name == S._Negate) && !_n.IsParenthesizedExpr())
				{
					if (_o.AllowChangeParentheses)
						needParens = true; // Resolve ambiguity with extra parens
					else
						return false; // Fallback to prefix notation
				}
				// Check for the ambiguous case of "~Foo(...);"
				if (_name == S.NotBits && _context.Lo == StartStmt.Lo && arg.IsCall)
					return false;

				if (WriteOpenParen(ParenFor.Grouping, needParens))
					_context = StartExpr;
				WriteOperatorName(_name);
				PrefixSpace(precedence);
				PrintExpr(arg, precedence.RightContext(_context), 0);
				//if (backtick) {
				//    Debug.Assert(precedence == EP.Backtick);
				//    if ((SpacingOptions & SpaceOpt.AroundInfix) != 0 && precedence.Lo < SpaceAroundInfixStopPrecedence)
				//        _out.Space();
				//    PrintOperatorName(_name, Ambiguity.UseBacktick);
				//}
				WriteCloseParen(ParenFor.Grouping, needParens);
				return true;
			}
			return false;
		}
		private void WriteOperatorName(Symbol name, bool useBacktick = false)
		{
			string opName = name.Name;
			if (useBacktick)
				PrintString(opName, '`', null);
			else {
				Debug.Assert(opName.StartsWith("'") || opName.StartsWith("#"));
				_out.Write(opName.Substring(1));
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintInfixBinaryOperator(Precedence prec)
		{
			Debug.Assert(!CastOperators.ContainsKey(_name)); // not called for cast operators
			if (_n.ArgCount != 2)
				return false;
			LNode left = _n.Args[0], right = _n.Args[1];
			if (!_o.AllowChangeParentheses) {
				// Attributes on the children normally disqualify operator notation
				if (HasPAttrs(left) || HasPAttrs(right))
					return false;
			}

			bool needParens, backtick = (_n.Style & NodeStyle.Alternate) != 0 || (_flags & Ambiguity.UseBacktick) != 0;
			if (CanAppearHere(ref prec, out needParens, ref backtick))
			{
				// Check for the ambiguous case of "A * b;" and consider using `*` instead
				if (_name == S.Mul && _context.Left == StartStmt.Left && IsComplexIdentifier(left)) {
					backtick = true;
					prec = EP.Backtick;
					if (!CanAppearHere(prec, out needParens, false))
						return false;
				}

				if (WriteOpenParen(ParenFor.Grouping, needParens))
					_context = StartExpr;
				Ambiguity lFlags = _flags & Ambiguity.TypeContext;
				if (_name == S.Assign || _name == S.Lambda) lFlags |= Ambiguity.AllowUnassignedVarDecl;
				if (_name == S.NotBits) lFlags |= Ambiguity.IsCallTarget;
				PrintExpr(left, prec.LeftContext(_context), lFlags);
				PrintInfixWithSpace(_name, _n.Target, prec, backtick);
				PrintExpr(right, prec.RightContext(_context));
				WriteCloseParen(ParenFor.Grouping, needParens);
				return true;
			}
			return false;
		}

		public bool AutoPrintBinaryWordOperator(Precedence prec)
		{
			// Handles `with switch when where` but not `is as using`
			Debug.Assert(_name == S.With || _name == S.SwitchOp || _name == S.When || _name == S.WhereOp);
			var a = _n.Args;
			// NOTE: properly validating that the contents of the switch expr body are
			//       printable is difficult, and due to lack of popular demand I haven't
			//       done it. Given a malformed switch body it may print something that
			//       either has syntax errors or won't round-trip.
			if (a.Count != 2 || !HasSimpleHeadWPA(_n))
				return false;

			LNode lhs = a[0], rhs = a[1];
			bool needParens = false;
			if (_name == S.When && (_flags & Ambiguity.IsPattern) != 0) {
				PrintExpr(lhs, EP.IfElse, Ambiguity.IsPattern);
			} else {
				G.Verify(CanAppearHere(prec, out needParens));
				WriteOpenParen(ParenFor.Grouping, needParens);

				// Avoid printing something that looks like `(TypeName) with {...}`, 
				// which the parser would perceive as a cast.
				if (_name == S.With && lhs.IsParenthesizedExpr()
					&& EcsValidators.IsComplexIdentifier(a[0], ICI.AllowParensAround))
					PrintExpr(lhs, prec.LeftContext(_context), Ambiguity.ForceAttributeList);
				else
					PrintExpr(lhs, prec.LeftContext(_context));
			}

			PrintInfixWithSpace(_name, _n.Target, prec);

			if (_name == S.SwitchOp || _name == S.With)
				PrintBracedBlock(rhs, NewlineOpt.BeforeOpenBraceInNewExpr, false, _spaceName, _name == S.With ? BraceMode.Enum : BraceMode.SwitchExpression);
			else
				PrintExpr(rhs, prec.RightContext(_context), 
					_name == S.When && (_flags & Ambiguity.IsPattern) != 0 ? Ambiguity.InPattern : 0);

			WriteCloseParen(ParenFor.Grouping, needParens);
			return true;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintPrefixOrInfixOperator(Precedence infixPrec)
		{
			if (_n.ArgCount == 2)
				return AutoPrintInfixBinaryOperator(infixPrec);
			else if (infixPrec.Lo == EP.Compare.Lo) {
				// One of the pattern relational operators
				Debug.Assert(_name.IsOneOf(S.LT, S.GT, S.LE, S.GE));
				return AutoPrintPatternUnaryOperator(EP.Compare);
			} else
				return AutoPrintPrefixUnaryOperator(PrefixOperators[_name]);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintPrefixReturnThrowEtc(Precedence prec)
		{
			if (!EcsValidators.IsSimpleExecutableKeywordStmt(_n, Pedantics))
				return false;
			bool needParens;
			if (!CanAppearHere(prec, out needParens))
				return false;
			if (WriteOpenParen(ParenFor.Grouping, needParens))
				_context = StartExpr;
			else if (_context.Left == StartStmt.Left)
				return false; // cannot print throw/return subexpression at beginning of a statement

			PrintReturnThrowEtc(_name, _n.Args[0, null]);

			WriteCloseParen(ParenFor.Grouping, needParens);
			return true;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintCastOperator(Precedence precedence)
		{
			if (_n.ArgCount != 2)
				return false;

			// Cast operators can have attributes on the second argument using 
			// alternate notation, e.g. x(as [A] Foo) is legal but "x as [A] Foo"
			// is not, because attributes must only appear at the beginning of an 
			// expression and only the second case treats the text after 'as' as 
			// the beginning of a new expression. Also, because a standard cast 
			// like (Foo)(x) is ambiguous (is x being cast to type Foo, or is a
			// delegate named Foo being called with x as an argument?), an 
			// attribute list can be used to resolve the ambiguity. So (Foo)(x) 
			// is considered a cast, while ([ ] Foo)(x) is a call to Foo in which 
			// Foo happens to be placed in parenthesis. Thus, if target type of a 
			// cast has attributes, it must be expressed in alternate form, e.g.
			// (x)(->[A] Foo), or in prefix form.
			//
			// There is an extra rule for (X)Y casts: X must be a complex (or 
			// simple) identifier, since anything else won't be parsed as a cast.
			Symbol name = _name;
			bool alternate = (_n.Style & NodeStyle.Alternate) != 0 && !_o.PreferPlainCSharp;
			LNode subject = _n.Args[0], target = _n.Args[1];
			if (HasPAttrs(subject))
				return false;
			if (HasPAttrs(target) || (name == S.Cast && !IsComplexIdentifier(target, ICI.Default | ICI.AllowAnyExprInOf)))
				alternate = true;
			
			bool needParens;
			if (alternate)
				precedence = EP.Primary;
			if (!CanAppearHere(precedence, out needParens) && name != S.Is) {
				// There are two different precedences for cast operators; we prefer 
				// the traditional forms (T)x, x as T, x using T which have lower 
				// precedence, but they don't work in this context so consider using 
				// x(->T), x(as T) or x(using T) instead.
				alternate = true;
				precedence = EP.Primary;
				if (!CanAppearHere(precedence, out needParens))
					return false;
			}

			if (alternate && _o.PreferPlainCSharp)
				return false; // old-style cast is impossible here

			if (WriteOpenParen(ParenFor.Grouping, needParens))
				_context = StartExpr;

			if (alternate) {
				PrintExpr(subject, precedence.LeftContext(_context));
				WriteOpenParen(ParenFor.NewCast);
				_out.Write(GetCastText(_name));
				Space(SpaceOpt.AfterCastArrow);
				PrintType(target, StartExpr, Ambiguity.AllowPointer);
				WriteCloseParen(ParenFor.NewCast);
			} else {
				if (_name == S.Cast) {
					WriteOpenParen(ParenFor.Grouping);
					PrintType(target, ContinueExpr, Ambiguity.AllowPointer);
					WriteCloseParen(ParenFor.Grouping);
					Space(SpaceOpt.AfterCast);
					PrintExpr(subject, precedence.RightContext(_context), Ambiguity.CastRhs);
				} else {
					// "x as y" or "x using y"
					PrintExpr(subject, precedence.LeftContext(_context));
					_out.Space().Write(GetCastText(_name));
					PrintType(target, precedence.RightContext(_context));
				}
			}

			WriteCloseParen(ParenFor.Grouping, needParens);
			return true;
		}
		private string GetCastText(Symbol name)
		{
			if (name == S.UsingCast) return "using ";
			if (name == S.As) return "as ";
			return "->";
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintIsOperator(Precedence precedence)
		{	
			LNode subject, pattern;
			if (!EcsValidators.IsIsTest(_n, out subject, out pattern))
				return false;
			bool needParens;
			if (!CanAppearHere(precedence, out needParens))
				return false;
			if (WriteOpenParen(ParenFor.Grouping, needParens))
				_context = StartExpr;

			PrintExpr(subject, precedence.LeftContext(_context));
			_out.Space().Write("is ");

			if (IsComplexIdentifier(pattern))
				PrintType(pattern, EP.Is);
			else
				PrintExpr(pattern, EP.Is, Ambiguity.IsPattern);

			WriteCloseParen(ParenFor.Grouping, needParens);
			return true;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintDeconstructOperator(Precedence _)
		{
			// Prints `@'deconstruct(Xyz(positional), braced)` as `Xyz(positional) { braced }`
			Debug.Assert(_name == S.Deconstruct);
			int argCount = _n.ArgCount;
			if ((_flags & Ambiguity.IsPattern) == 0 || argCount == 0)
				return false;

			LNode deconstructCall = _n.Args[0];
			LNode type = deconstructCall.Target;
			var positionals = deconstructCall.Args;

			// By example:
			// var(X, Y)       <=> @'deconstruct operator is not used (it's #var(@``, (X, Y)))
			// Foo(X, Y)       <=> @'deconstruct(Foo(X, Y))
			// (X, Y)          <=> @'deconstruct(@'tuple(X, Y))
			// (X, Y) { }      <=> @'deconstruct(@'tuple(X, Y))
			// (X, Y) { X: x } <=> @'deconstruct(@'tuple(X, Y), X ::= x)
			// { X: x, Y: y }  <=> @'deconstruct(@'tuple(), X ::= x, Y ::= y)
			// { }             <=> @'deconstruct(@'tuple())
			// { } x           <=> #var(@'deconstruct(@'tuple()), x)
			// ((X, Y), { Z }) <=> @'deconstruct(@'tuple(@'deconstruct(@'tuple(X, Y)), @'deconstruct(@'tuple(), Z)))
			// (X, Y)[]        <=> @'deconstruct operator is not used (it's @'of(@`'[]`, (X, Y)))
			// (Foo<X, Y>)     <=> @'deconstruct operator is not used (it's (Foo!(X, Y)))
			// 7 + 7           <=> @'deconstruct operator is not used (it's 7 + 7)
			// <= 7 * 7        <=> @'deconstruct operator is not used (it's @`'<=`(7 * 7))

			bool hasType = !type.IsIdNamed(S.Tuple);
			if (hasType) {
				PrintType(type, _context);
			}
			if (positionals.Count != 0) {
				PrintArgTuple(deconstructCall, ParenFor.MethodCall, true, _o.OmitMissingArguments);
			}
			// Output like `(Foo)` is not a deconstruction pattern, so if there is no type,
			// and only a single argument in the parens, and otherwise no reason to print 
			// braces, we must print empty braces (`(Foo) { }`). Empty braces are required 
			// even if our parent node is #var, because `(Foo) x` looks like a cast, not a 
			// deconstruction pattern, so we need the output to look like `(Foo) { } x`.
			if (argCount > 1 || positionals.Count == 0 || (!hasType && positionals.Count == 1)) {
				if (hasType || positionals.Count != 0)
					Space(SpaceOpt.BeforeNewInitBrace);
				PrintBracedBlockInNewOrDeconstructExpr(1, Ambiguity.IsPattern | Ambiguity.InPattern | Ambiguity.AllowUnassignedVarDecl);
			}
			return true;
		}
		
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintPatternUnaryOperator(Precedence precedence)
		{
			if ((_flags & Ambiguity.IsPattern) == 0 || _n.ArgCount != 1)
				return false;
		
			_out.Write(_name.Name.Slice(1)).Space();
			PrintExpr(_n.Args[0], precedence.RightContext(_context), Ambiguity.InPattern | Ambiguity.IsPattern);
			
			return true;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintPatternAndOrOperator(Precedence precedence)
		{
			if ((_flags & Ambiguity.IsPattern) == 0 || _n.ArgCount != 2)
				return false;

			PrintExpr(_n.Args[0], precedence.LeftContext(_context), Ambiguity.InPattern | Ambiguity.IsPattern);
			_out.Space().Write(_name.Name.Slice(1)).Space();
			PrintExpr(_n.Args[1], precedence.RightContext(_context), Ambiguity.InPattern | Ambiguity.IsPattern);
			
			return true;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintListOperator(Precedence precedence)
		{
			// Handles 'tuple and '{} braces.
			int argCount = _n.ArgCount;
			Symbol name = _name;
			Debug.Assert(_n.IsCall);

			bool? braceMode;
			if (name == S.Tuple) {
				braceMode = false;
				_flags &= Ambiguity.AllowUnassignedVarDecl;
			} else if (name == S.Braces) {
				// A braced block is not allowed at start of an expression 
				// statement; the parser would mistake it for a standalone 
				// braced block (the difference is that a standalone braced 
				// block ends automatically after '}', with no semicolon.)
				if (_context.Left == StartStmt.Left || (_flags & Ambiguity.NoBracedBlock) != 0)
					return false;
				braceMode = true;
				if (_context.Left <= ContinueExpr.Left && _n.BaseStyle == NodeStyle.Expression)
					braceMode = null; // initializer mode
			} else if (name == S.ArrayInit) {
				braceMode = null; // initializer mode
			} else {
				Debug.Assert(false);
				braceMode = _n.BaseStyle == NodeStyle.StatementBlock && (_flags & Ambiguity.NoBracedBlock) == 0;
				_flags = 0;
			}

			if (braceMode ?? true)
			{
				PrintBracedBlock(_n, 0, 
					mode: braceMode == null ? BraceMode.Initializer : BraceMode.BlockExpr);
			}
			else
			{
				WriteOpenParen(ParenFor.Grouping);
				for (int i = 0; i < argCount; i++)
				{
					if (i != 0) WriteThenSpace(',', SpaceOpt.AfterComma);
					PrintExpr(_n.Args[i], StartExpr, _flags);
				}
				if (name == S.Tuple && argCount == 1)
					_out.Write(',');
				WriteCloseParen(ParenFor.Grouping);
			}
			return true;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintOfOperator(Precedence precedence)
		{
			bool needSpecialOfNotation = false;
			var ici = ICI.Default | ICI.AllowAttrs;
			if ((_flags & Ambiguity.InDefinitionName) != 0)
				ici |= ICI.NameDefinition;
			if (!IsComplexIdentifier(_n, ici)) {
				if (IsComplexIdentifier(_n, ici | ICI.AllowAnyExprInOf))
					needSpecialOfNotation = true;
				else
					return false;
			}
			bool parens;
			if (!CanAppearHere(precedence, out parens) || parens)
				return false;

			Debug.Assert(_n.ArgCount >= 1);
			PrintExpr(_n.Args[0], precedence.LeftContext(_context), _flags & (Ambiguity.InDefinitionName | Ambiguity.TypeContext));

			_out.Write(needSpecialOfNotation ? "!(" : "<");
			for (int i = 1, argC = _n.ArgCount; i < argC; i++) {
				if (i > 1)
					WriteThenSpace(',', SpaceOpt.AfterCommaInOf);
				var typeArg = _n.Args[i];
				if (typeArg.IsIdNamed(S.Missing) && !HasPAttrs(typeArg)) {
					// Omit argument (e.g. Dictionary<,> means Dictionary<@``, @``>)
				} else {
					PrintType(_n.Args[i], StartExpr, Ambiguity.InOf | Ambiguity.AllowPointer | (_flags & Ambiguity.InDefinitionName));
				}
			}
			_out.Write(needSpecialOfNotation ? ')' : '>');
			return true;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintNewOperator(Precedence precedence)
		{
			// Prints the new Xyz(...) {...} operator
			Debug.Assert (_name == S.New);
			int argCount = _n.ArgCount;
			if (argCount == 0)
				return false;
			bool needParens;
			Debug.Assert(CanAppearHere(precedence, out needParens) && !needParens);

			LNode constructorCall = _n.Args[0];
			LNode type = constructorCall.Target;
			var consArgs = constructorCall.Args;

			// There are two basic uses of new: for objects, and for arrays.
			// In all cases, 'new has 1 arg plus optional initializer arguments,
			// and there's always a list of "constructor args" even if it is empty 
			// (exception: new {...}).
			// 1. Init an object: 1a. new Foo<Bar>() { ... }  <=> @'new(@`'of`(Foo, Bar)(...), ...)
			//                    1b. new { ... }             <=> @'new(@``, ...)
			// 2. Init an array:  2a. new int[] { ... },      <=> @'new(int[](), ...) <=> @'new(@`'of`(@`'[]`, int)(), ...)
			//                    2b. new[,] { ... }.         <=> @'new(@`'[,]`(), ...)
			//                    2c. new int[10,10] { ... }, <=> @'new(@`'of`(@`'[,]`, int)(10,10), ...)
			//                    2d. new int[10][] { ... },  <=> @'new(@`'of`(@`'[]`, @`'of`(@`'[]`, int))(10), ...)
			if (HasPAttrs(constructorCall))
				return false;
			if (type == null ? !constructorCall.IsIdNamed(S.Missing) : HasPAttrs(type) || !IsComplexIdentifier(type))
				return false;

			// Okay, we can now be sure that it's printable, but is it an array decl?
			if (type == null) {
				// 1b, new {...}
				_out.Write("new");
				PrintBracedBlockInNewOrDeconstructExpr(1);
			} else if (type != null && type.IsId && S.CountArrayDimensions(type.Name) > 0) { // 2b
				_out.Write("new");
				Debug.Assert(type.Name.Name.StartsWith("'"));
				_out.Write(type.Name.Name.Substring(1));
				Space(SpaceOpt.Default);
				PrintBracedBlockInNewOrDeconstructExpr(1);
			} else {
				_out.Write("new");
				int dims = CountDimensionsIfArrayType(type);
				if (dims > 0 && constructorCall.Args.Count == dims) {
					PrintTypeWithArraySizes(constructorCall);
				} else {
					// Otherwise we can print the type name without caring if it's an array or not.
					PrintType(type, EP.Primary.LeftContext(_context));
					if (constructorCall.ArgCount != 0 || (argCount == 1 && dims == 0))
						PrintArgTuple(constructorCall, ParenFor.MethodCall, false, _o.OmitMissingArguments);
				}
				if (_n.Args.Count > 1)
					PrintBracedBlockInNewOrDeconstructExpr(1);
			}
			return true;
		}
		int CountDimensionsIfArrayType(LNode type)
		{
			LNode dimsNode;
			if (type.Calls(S.Of, 2) && (dimsNode = type.Args[0]).IsId)
				return S.CountArrayDimensions(dimsNode.Name);
			return 0;
		}
		private void PrintBracedBlockInNewOrDeconstructExpr(int start_i, Ambiguity flags = 0)
		{
			if ((flags & Ambiguity.IsPattern) == 0) {
				if (!Newline(NewlineOpt.BeforeOpenBraceInNewExpr))
					Space(SpaceOpt.BeforeNewInitBrace);
			}
			WriteThenSpace('{', SpaceOpt.InsideNewInitializer);
			using (Indented) {
				Newline(NewlineOpt.AfterOpenBraceInNewExpr);
				for (int i = start_i, c = _n.ArgCount; i < c; i++) {
					if (i != start_i) {
						WriteThenSpace(',', SpaceOpt.AfterComma);
						Newline(NewlineOpt.AfterEachInitializer);
					}
					var expr = _n.Args[i];
					if (expr.Calls(S.Braces))
						using (With(expr, StartExpr))
							PrintBracedBlockInNewOrDeconstructExpr(0);
					else if (expr.CallsMin(S.DictionaryInitAssign, 1)) {
						_out.Write('[');
						PrintArgs(expr.Args.WithoutLast(1), 0, false);
						_out.Write(']');
						PrintInfixWithSpace(S.Assign, expr.Target, EcsPrecedence.Assign);
						PrintExpr(expr.Args.Last, StartExpr);
					} else 
						PrintExpr(expr, StartExpr, flags);
				}
			}
			if (!Newline(NewlineOpt.BeforeCloseBraceInNewExpr))
				Space(SpaceOpt.InsideNewInitializer);
			_out.Write('}');
			Newline(NewlineOpt.AfterCloseBraceInNewExpr);
		}
		private void PrintTypeWithArraySizes(LNode cons)
		{
			LNode type = cons.Target;
			// Called by AutoPrintNewOperator; type is already validated.
			Debug.Assert(type.Calls(S.Of, 2) && S.IsArrayKeyword(type.Args[0].Name));
			// We have to deal with the "constructor arguments" specially.
			// First of all, the constructor arguments appear inside the 
			// square brackets, which is unusual: int[x + y]. But there's 
			// something much more strange in case of arrays of arrays: the 
			// order of the square brackets must be reversed. If the 
			// constructor argument is 10, an array of two-dimensional 
			// arrays of int is written int[10][,], rather than int[,][10] 
			// which would be easier to handle.
			int dims = cons.ArgCount, innerDims;
 			LNode elemType = type.Args[1];
			var dimStack = InternalList<int>.Empty;
			while ((innerDims = CountDimensionsIfArrayType(elemType)) != 0) {
				dimStack.Add(innerDims);
				elemType = elemType.Args[1];
			}
			
			PrintType(elemType, EP.Primary.LeftContext(ContinueExpr));
			
			_out.Write('[');
			PrintArgs(cons.Args, 0, false);
			_out.Write(']');

			// Write the brackets for the inner array types
			for (int i = dimStack.Count - 1; i >= 0; i--) {
				var arrayKW = S.GetArrayKeyword(dimStack[i]).Name;
				Debug.Assert(arrayKW.StartsWith("'"));
				_out.Write(arrayKW.Substring(1));
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintLambdaFunction(Precedence precedence)
		{
			Symbol name = _name;
			Debug.Assert(name == S.Lambda);
			if (_n.ArgCount != 2)
				return false;
			LNode args = _n.Args[0], body = _n.Args[1];

			bool needParens = false;
			bool canUseOldStyle = body.Calls(S.Braces) && args.Calls(S.AltList);
			bool oldStyle = _n.BaseStyle == NodeStyle.OldStyle && canUseOldStyle;
			if ((_flags & Ambiguity.InSwitchExpr) == 0 && !oldStyle && !CanAppearHere(EP.Lambda, out needParens)) {
				if (canUseOldStyle)
					oldStyle = true;
				else
					return false; // precedence fail
			}

			WriteOpenParen(ParenFor.Grouping, needParens);

			if (oldStyle) {
				_out.Write("delegate");
				PrintArgTuple(_n.Args[0], ParenFor.MethodDecl, true, _o.OmitMissingArguments);
				PrintBracedBlock(body, NewlineOpt.BeforeOpenBraceInExpr, spaceName: S.Fn);
			} else {
				var lhsFlags = Ambiguity.AllowUnassignedVarDecl;
				if ((_flags & Ambiguity.InSwitchExpr) != 0) {
					precedence = new Precedence(EP.IfElse.Left, StartExpr.Right, EP.Lambda.Lo, EP.Lambda.Hi);
					lhsFlags = Ambiguity.IsPattern;
				}
				PrintExpr(_n.Args[0], precedence.LeftContext(_context), lhsFlags);
				PrintInfixWithSpace(S.Lambda, _n.Target, EP.IfElse);
				PrintExpr(_n.Args[1], precedence.RightContext(_context));
			}

			WriteCloseParen(ParenFor.Grouping, needParens);
			return true;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintOtherSpecialOperator(Precedence precedence)
		{
			// Handles one of:  ?  suf[]  ?[]  suf++  suf--
			int argCount = _n.ArgCount;
			Symbol name = _name;
			if (argCount < 1)
				return false; // no args
			bool needParens;
			if (!CanAppearHere(precedence, out needParens))
				return false; // precedence fail

			// Verify that the special operator can appear at this precedence 
			// level and that its arguments fit the operator's constraints.
			var first = _n.Args[0];
			if (name == S.IndexBracks) {
				// Careful: a[] means @'of(@`[]`, a) in a type context, @`suf[]`(a) otherwise
				int minArgs = (_flags & Ambiguity.TypeContext) != 0 ? 2 : 1;
				if (argCount < minArgs || HasPAttrs(first))
					return false;
			} else if (name == S.NullIndexBracks) {
				if (argCount != 2 || HasPAttrs(first) || HasPAttrs(_n.Args[1]) || !_n.Args[1].Calls(S.AltList))
					return false;
			} else if (name == S.QuestionMark) {
				if (argCount != 3 || HasPAttrs(first) || HasPAttrs(_n.Args[1]) || HasPAttrs(_n.Args[2]))
					return false;
			} else {
				Debug.Assert(name == S.PostInc || name == S.PostDec || name == S.IsLegal);
				if (argCount != 1 || HasPAttrs(first))
					return false;
			}

			// Print the thing!
			WriteOpenParen(ParenFor.Grouping, needParens);

			if (name == S.IndexBracks)
			{
				PrintExpr(first, precedence.LeftContext(_context));
				Space(SpaceOpt.BeforeMethodCall);
				_out.Write('[');
				Space(SpaceOpt.InsideCallParens);
				for (int i = 1, c = _n.ArgCount; i < c; i++)
				{
					if (i != 1) WriteThenSpace(',', SpaceOpt.AfterComma);
					PrintExpr(_n.Args[i], StartExpr);
				}
				Space(SpaceOpt.InsideCallParens);
				_out.Write(']');
			}
			else if (name == S.NullIndexBracks)
			{
				PrintExpr(first, precedence.LeftContext(_context));
				Space(SpaceOpt.BeforeMethodCall);
				_out.Write("?[");
				Space(SpaceOpt.InsideCallParens);
				PrintArgs(_n.Args[1], false);
				Space(SpaceOpt.InsideCallParens);
				_out.Write(']');
			}
			else if (name == S.QuestionMark)
			{
				PrintExpr(_n.Args[0], precedence.LeftContext(_context));
				PrintInfixWithSpace(S.QuestionMark, _n.Target, EP.IfElse);
				PrintExpr(_n.Args[1], ContinueExpr);
				PrintInfixWithSpace(S.Colon, null, EP.IfElse);
				PrintExpr(_n.Args[2], precedence.RightContext(_context));
			}
			else
			{
				Debug.Assert(name == S.PostInc || name == S.PostDec || name == S.IsLegal);
				PrintExpr(first, precedence.LeftContext(_context));
				_out.Write(name == S.PostInc ? "++" : name == S.PostDec ? "--" : "is legal");
			}

			WriteCloseParen(ParenFor.Grouping, needParens);
			return true;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintCallOperator(Precedence precedence)
		{
			// Handles "call operators" such as default(...) and checked(...)
			bool needParens;
			Debug.Assert(CanAppearHere(precedence, out needParens));
			Debug.Assert(_n.HasSpecialName);
			if (_n.ArgCount != 1)
				return false;
			var arg = _n.Args[0];
			bool type = (_name == S.Default || _name == S.Typeof || _name == S.Sizeof);
			if (type && !IsComplexIdentifier(arg, ICI.Default | ICI.AllowAttrs))
				return false;

			WriteOperatorName(_name);
			if (type) {
				WriteOpenParen(ParenFor.MethodCall);
				PrintType(arg, StartExpr, Ambiguity.AllowPointer);
				WriteCloseParen(ParenFor.MethodCall);
			} else
				PrintWithinParens(ParenFor.MethodCall, arg, 0);
			return true;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintNamedArg(Precedence precedence)
		{
			if (!EcsValidators.IsNamedArgument(_n, Pedantics) || _context.RangeEquals(StartStmt))
				return false;
			bool needParens;
			if (!CanAppearHere(precedence, out needParens) || needParens)
				return false;

			PrintExpr(_n.Args[0], EP.Primary.LeftContext(_context));
			WriteThenSpace(':', SpaceOpt.AfterColon);
			PrintExpr(_n.Args[1], StartExpr, _flags & (Ambiguity.AllowUnassignedVarDecl | Ambiguity.InPattern | Ambiguity.IsPattern));
			return true;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintPropDeclExpr(Precedence precedence)
		{
			return AutoPrintProperty() != SPResult.Fail;
		}

		// Handles #rawText("custom string") and #C#RawText("custom string") in expression context
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool PrintRawText(Precedence mainPrec)
		{
			if (!_o.ObeyRawText)
				return false;
			_out.Write(GetRawText(_n));
			return true;
		}

		internal void PrintInPrefixNotation(bool skipAttrs = false)
		{
			int inParens = 0;
			if (!skipAttrs)
				inParens = PrintAttrs(AttrStyle.AllowKeywordAttrs);

			if (!_n.IsCall)
				PrintSimpleIdentOrLiteral();
			else {
	 			Debug.Assert(EP.Primary.CanAppearIn(_context));
				if (!_o.AllowConstructorAmbiguity && _n.Calls(_spaceName) && _context == StartStmt && inParens == 0)
				{
					inParens++;
					WriteOpenParen(ParenFor.Grouping);
				}

				// Print Target
				var target = _n.Target;
				var f = Ambiguity.IsCallTarget;
				if (_spaceName == S.Fn || _context != StartStmt)
					f |= Ambiguity.AllowThisAsCallTarget;
				PrintExpr(target, EP.Primary.LeftContext(_context), f);

				// Print argument list
				WriteOpenParen(ParenFor.MethodCall);

				bool first = true;
				foreach (var arg in _n.Args) {
					if (_o.OmitMissingArguments && IsSimpleSymbolWPA(arg, S.Missing) && _n.ArgCount > 1) {
						if (!first) WriteThenSpace(',', SpaceOpt.MissingAfterComma);
					} else {
						if (!first) WriteThenSpace(',', SpaceOpt.AfterComma);
						PrintExpr(arg, StartExpr);
					}
					first = false;
				}
				WriteCloseParen(ParenFor.MethodCall);
			}
			WriteCloseParens(inParens);
		}

		void WriteCloseParens(int parenCount)
		{
			while (parenCount-- > 0)
				WriteCloseParen(ParenFor.Grouping);
		}

		static string GetRawText(LNode rawTextNode)
		{
			object tVal = rawTextNode.TriviaValue;
			return tVal == NoValue.Value || tVal == null ? "" : tVal.ToString();
		}
		private void PrintSimpleIdentOrLiteral()
		{
			Debug.Assert(_n.HasSimpleHead());
			if (_n.IsLiteral)
				PrintLiteral();
			else {
				var mode = IdPrintMode.Normal;
				if (_n.AttrNamed(S.TriviaUseOperatorKeyword) != null 
					|| (_n.Name.Name.StartsWith("'") && (_flags & Ambiguity.InDefinitionName) != 0))
					mode = IdPrintMode.Operator;
				if (_n.BaseStyle == NodeStyle.VerbatimId)
					mode = IdPrintMode.Verbatim;
				PrintSimpleIdent(_name, _flags, mode);
			}
		}

		private void PrintVariableDecl(bool printAttrs, LNode skipClause = null)
		{
			var flags = _flags;
			Debug.Assert(_name == S.Var);
			var a = _n.Args;

			if (printAttrs) {
				if (a[1].IsId && (flags & Ambiguity.AllowUnassignedVarDecl) == 0)
					flags |= Ambiguity.ForceAttributeList;
				G.Verify(0 == PrintAttrs(AttrStyle.IsDefinition, skipClause));
			}

			var target = _n.Target;
			PrintTrivia(target, trailingTrivia: false);
			PrintTrivia(target, trailingTrivia: true);

			Debug.Assert(_context == StartStmt || _context == StartExpr || Flagged(Ambiguity.ForEachInitializer | Ambiguity.IsPattern));
			if (IsSimpleSymbolWPA(a[0], S.Missing))
				_out.Write("var");
			else if ((_flags & Ambiguity.IsPattern) != 0 && a[0].CallsMin(S.Deconstruct, 1))
				using (With(a[0], StartExpr, _flags)) {
					if (!G.Verify(AutoPrintDeconstructOperator(StartExpr)))
						PrintCurrentExpr(); // should never happen
				}
			else
				PrintType(a[0], EP.Primary.LeftContext(_context), flags & Ambiguity.AllowPointer);

			_out.Space();
			for (int i = 1; i < a.Count; i++) {
				if (i > 1)
					WriteThenSpace(',', SpaceOpt.AfterComma);
				PrintExpr(a[i], EP.Assign.RightContext(_context), Ambiguity.InDefinitionName);
			}
		}

		#region Linq expressions

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintLinqExpression(Precedence primary)
		{
			if (EcsValidators.IsLinqExpression(_n, Pedantics)) {
				// Print the clauses
				bool first = true;
				foreach (LNode clause in _n.Args)
				{
					if (!first)
						Space(SpaceOpt.Default);
					first = false;
					using (With(clause, StartExpr))
						PrintLinqClause();
				}
				return true;
			}
			return false;
		}

		private void PrintLinqClause()
		{
			LNode clause = _n;
			PrintTrivia(clause, false);
			var name = clause.Name;
			var arg0 = clause[0];
			if (name == S.From) {
				_out.Write("from ");
				Debug.Assert(clause.ArgCount == 1);
				if (arg0.Calls(S.In, 2)) {
					PrintExpr(arg0[0], StartExpr, Ambiguity.AllowUnassignedVarDecl);
					_out.Write(" in ");
					PrintExpr(arg0[1], ContinueExpr);
				} else
					PrintExpr(arg0, StartExpr);
			} else if (name == S.OrderBy) {
				_out.Write("orderby ");
				var first = true;
				foreach (var arg in clause.Args) {
					if (!first)
						WriteThenSpace(',', SpaceOpt.AfterComma);
					first = false;
					if (arg.Calls(S.Ascending, 1)) {
						PrintExpr(arg[0], StartExpr);
						_out.Write(" ascending");
					} else if (arg.Calls(S.Descending, 1)) {
						PrintExpr(arg[0], StartExpr);
						_out.Write(" descending");
					} else {
						PrintExpr(arg, StartExpr);
					}
				}
			} else if (name == S.Join) {
				_out.Write("join ");
				LNode equals = clause.Args[1], into = clause.Args[2, null];
				Debug.Assert(arg0.Calls(S.In, 2));
				Debug.Assert(equals.Calls("#equals", 2));
				Debug.Assert(into == null || into.Calls("#into", 1));
				PrintExpr(arg0[0], StartExpr, Ambiguity.AllowUnassignedVarDecl);
				_out.Space().Write("in ");
				PrintExpr(arg0[1], ContinueExpr);
				_out.Space().Write("on ");
				PrintExpr(equals[0], StartExpr);
				_out.Space().Write("equals ");
				PrintExpr(equals[1], StartExpr);
				if (into != null) {
					_out.Space().Write("into ");
					PrintExpr(into[0], StartExpr);
				}
			} else if (name == S.GroupBy) {
				_out.Write("group ");
				Debug.Assert(clause.ArgCount == 2);
				LNode arg1 = clause.Args[1];
				PrintExpr(arg0, StartExpr);
				_out.Space().Write("by ");
				PrintExpr(arg1, StartExpr);
			} else if (name == S.Into) {
				_out.Write("into ");
				PrintExpr(arg0, StartExpr);
				for (int i = 1; i < clause.ArgCount; i++)
					using (With(clause[i], StartExpr))
						PrintLinqClause();
			} else {
				Debug.Assert(name == S.Let || name == S.WhereClause || name == S.Select);
				_out.Write(name.Name.Substring(1));
				Space(SpaceOpt.Default);
				Debug.Assert(clause.ArgCount == 1);
				PrintExpr(arg0, StartExpr);
			}
			PrintTrivia(clause, true);
		}

		#endregion

		#region PrintType()

		protected void PrintType(LNode n, Precedence context, Ambiguity flags = 0)
		{
			using (With(n, context, flags | Ambiguity.TypeContext))
				PrintCurrentType();
		}
		void PrintCurrentType()
		{
			// TODO: add test case of array type with %inParens attr
			// TODO: add test case of array type with other trivia in default(), typeof(), ret val, field type
			// TODO: add test case of array type with nontrivia attributes
			// TODO: add test case of method name with %inParens attr
			// TODO: add test case of method name with nontrivia attributes

			bool allowPointer = (_flags & Ambiguity.AllowPointer) != 0;
			bool inDefinitionName = (_flags & Ambiguity.InDefinitionName) != 0;
			bool inOf = (_flags & Ambiguity.InOf) != 0;
			
			// Check for special type names such as Foo? or Foo[]
			Symbol stk = SpecialTypeKind(_n);
			if (stk != null)
			{
				G.Verify(0 == PrintAttrs(AttrStyle.AllowKeywordAttrs));

				if (S.IsArrayKeyword(stk))
				{
					// We do something very strange in case of arrays of arrays:
					// the order of the square brackets must be reversed when 
					// arrays are nested. For example, an array of two-dimensional 
					// arrays of int is written int[][,], rather than int[,][] 
					// which would be much easier to handle.
					var stack = InternalList<Symbol>.Empty;
					var innerType = _n;
					do {
						stack.Add(stk);
						innerType = innerType.Args[1];
					} while ((stk = SpecialTypeKind(innerType)) != null && S.IsArrayKeyword(stk));

					PrintType(innerType, EP.Primary.LeftContext(_context), (_flags & Ambiguity.AllowPointer));

					for (int i = 0; i < stack.Count; i++)
					{
						Debug.Assert(stack[i].Name.StartsWith("'"));
						_out.Write(stack[i].Name.Substring(1)); // e.g. [] or [,]
					}
				}
				else if (stk == S.Tuple)
				{
					PrintTupleType();
				}
				else
				{
					PrintType(_n.Args[1], EP.Primary.LeftContext(_context), (_flags & Ambiguity.AllowPointer));
					_out.Write(stk == S._Pointer ? '*' : '?');
				}

				PrintTrivia(trailingTrivia: true);
			}
			else
			{
				// All other types are structurally the same as normal expressions.
				PrintCurrentExpr();
			}
		}

		void PrintTupleType()
		{
			Debug.Assert(_n.CallsMin(S.Of, 1) && _n[0].IsIdNamed(S.Tuple));
			Debug.Assert((_flags & Ambiguity.TypeContext) != 0);

			WriteOpenParen(ParenFor.Grouping);
			int i = 1;
			for (int c = _n.ArgCount; i < c; i++)
			{
				if (i > 1) WriteThenSpace(',', SpaceOpt.AfterComma);
				LNode arg = _n.Args[i];
				if (arg.Calls(S.Var))
					PrintExpr(arg, StartExpr, _flags);
				else
					PrintType(arg, StartExpr, _flags);
			}
			if (i == 2)
				_out.Write(',');
			WriteCloseParen(ParenFor.Grouping);
		}

		Symbol SpecialTypeKind(LNode n)
		{
			// detects when notation for special types applies: Foo[], Foo*, Foo?
			// assumes IsComplexIdentifier() is already known to be true
			LNode first;
			if (n.CallsMin(S.Of, 1) && (first = n.Args[0]).IsId) {
				var kind = first.Name;
				if (n.ArgCount == 2) {
					if (S.IsArrayKeyword(kind) || kind == S.QuestionMark)
						return kind;
					if (kind == S._Pointer && ((_flags & Ambiguity.AllowPointer) != 0 || _context.Left == StartStmt.Left))
						return kind;
				}
				if (kind == S.Tuple)
					return kind;
			}
			return null;
		}

		#endregion
	}
}
