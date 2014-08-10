using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.ComponentModel;
using Loyc;
using Loyc.Utilities;
using Loyc.Math;
using Loyc.Syntax;
using Loyc.Collections.Impl;
using S = Loyc.Syntax.CodeSymbols;
using EP = Ecs.EcsPrecedence;

namespace Ecs
{
	// This file: code for printing expressions
	public partial class EcsNodePrinter
	{
		#region Sets and dictionaries of operators

		static readonly Dictionary<Symbol,Precedence> PrefixOperators = Dictionary( 
			// This is a list of unary prefix operators only. Does not include the
			// binary prefix operator "#cast" or the unary suffix operators ++ and --.
			// Although #. can be a prefix operator, it is not included in this list
			// because it needs special treatment because its precedence is higher
			// than EP.Primary (i.e. above prefix notation). Therefore, it's printed
			// as an identifier if possible (e.g. #.(a)(x) is printed ".a(x)") and
			// uses prefix notation if not (e.g. #.(a(x)) must be in prefix form.)
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
			P(S.Substitute, EP.Substitute)
		);

		static readonly Dictionary<Symbol,Precedence> InfixOperators = Dictionary(
			// This is a list of infix binary opertors only. Does not include the
			// conditional operator #? or non-infix binary operators such as a[i].
			// #, is not an operator at all and generally should not occur. 
			// Note: I cancelled my plan to add a binary ~ operator because it would
			//       change the meaning of (x)~y from a type cast to concatenation.
			P(S.XorBits, EP.XorBits),   P(S.Xor, EP.Or),        P(S.Mod, EP.Multiply),
			P(S.AndBits, EP.AndBits),   P(S.And, EP.And),       P(S.Mul, EP.Multiply), 
			P(S.Exp, EP.Power),         P(S.Add, EP.Add),       P(S.Sub, EP.Add),
			P(S.Set, EP.Assign),        P(S.Eq, EP.Equals),     P(S.Neq, EP.Equals),
			P(S.OrBits, EP.OrBits),     P(S.Or, EP.Or),         P(S.Lambda, EP.Lambda),
			P(S.DotDot, EP.Range),      P(S.LT, EP.Compare),    P(S.Shl, EP.Shift),
			P(S.GT, EP.Compare),        P(S.Shr, EP.Shift),     P(S.Div, EP.Multiply),
			P(S.MulSet, EP.Assign),     P(S.DivSet, EP.Assign), P(S.ModSet, EP.Assign),
			P(S.SubSet, EP.Assign),     P(S.AddSet, EP.Assign), P(S.ConcatSet, EP.Assign),
			P(S.ExpSet, EP.Assign),     P(S.ShlSet, EP.Assign), P(S.ShrSet, EP.Assign),
			P(S.XorBitsSet, EP.Assign), P(S.AndBitsSet, EP.Assign), P(S.OrBitsSet, EP.Assign),
			P(S.NullDot, EP.NullDot),   P(S.NullCoalesce, EP.OrIfNull), P(S.NullCoalesceSet, EP.Assign),
			P(S.LE, EP.Compare),        P(S.GE, EP.Compare),    P(S.PtrArrow, EP.Primary),
			P(S.Is, EP.Compare),        P(S.As, EP.Compare),    P(S.UsingCast, EP.Compare),
			P(S.QuickBind, EP.Primary), P(S.In, EP.Equals),     P(S.ColonColon, EP.Primary),
			P(S.NotBits, EP.Add)
		);

		static readonly Dictionary<Symbol,Precedence> CastOperators = Dictionary(
			P(S.Cast, EP.Prefix),      // (Foo)x      (preferred form)
			P(S.As, EP.Compare),       // x as Foo    (preferred form)
			P(S.UsingCast, EP.Compare) // x using Foo (preferred form)
		);

		static readonly HashSet<Symbol> ListOperators = new HashSet<Symbol>(new[] {
			S.Tuple, S.CodeQuote, S.CodeQuoteSubstituting, S.Braces, S.ArrayInit });

		static readonly Dictionary<Symbol,Precedence> SpecialCaseOperators = Dictionary(
			// Operators that need special treatment (neither prefix nor infix nor casts)
			// ?  []  suf++  suf--  #of  .  #isLegal  #new
			P(S.QuestionMark,EP.IfElse),  // a?b:c
			P(S.Bracks,      EP.Primary), // a[]
			P(S.PostInc,     EP.Primary), // x++
			P(S.PostDec,     EP.Primary), // x--
			P(S.Of,          EP.Primary), // List<int>, int[], int?, int*
			P(S.Dot,         EP.Primary), // a.b.c
			P(S.IsLegal,     EP.Compare), // x is legal
			P(S.New,         EP.Primary)  // new A()
		);

		static readonly HashSet<Symbol> CallOperators = new HashSet<Symbol>(new[] {
			S.Typeof, S.Checked, S.Unchecked, S.Default, S.Sizeof
		});


		delegate bool OperatorPrinter(EcsNodePrinter @this, Precedence mainPrec, Precedence context, Ambiguity flags);
		static Dictionary<Symbol, Pair<Precedence, OperatorPrinter>> OperatorPrinters = OperatorPrinters_();
		static Dictionary<Symbol, Pair<Precedence, OperatorPrinter>> OperatorPrinters_()
		{
			// Build a dictionary of printers for each operator name.
			var d = new Dictionary<Symbol, Pair<Precedence, OperatorPrinter>>();
			
			// Create open delegates to the printers for various kinds of operators
			var prefix = OpenDelegate<OperatorPrinter>("AutoPrintPrefixUnaryOperator");
			var infix = OpenDelegate<OperatorPrinter>("AutoPrintInfixBinaryOperator");
			var both = OpenDelegate<OperatorPrinter>("AutoPrintPrefixOrInfixOperator");
			var cast = OpenDelegate<OperatorPrinter>("AutoPrintCastOperator");
			var list = OpenDelegate<OperatorPrinter>("AutoPrintListOperator");
			var ident = OpenDelegate<OperatorPrinter>("AutoPrintComplexIdentOperator");
			var @new = OpenDelegate<OperatorPrinter>("AutoPrintNewOperator");
			var other = OpenDelegate<OperatorPrinter>("AutoPrintOtherSpecialOperator");
			var call = OpenDelegate<OperatorPrinter>("AutoPrintCallOperator");
			
			foreach (var p in PrefixOperators)
				d.Add(p.Key, G.Pair(p.Value, prefix));
			foreach (var p in InfixOperators)
				if (d.ContainsKey(p.Key))
					d[p.Key] = G.Pair(p.Value, both); // both prefix and infix
				else
					d.Add(p.Key, G.Pair(p.Value, infix));
			foreach (var p in CastOperators)
				d[p.Key] = G.Pair(p.Value, cast);
			foreach (var op in ListOperators)
				d[op] = G.Pair(Precedence.MaxValue, list);
			foreach (var p in SpecialCaseOperators) {
				var handler = p.Key == S.Of || p.Key == S.Dot ? ident : p.Key == S.New ? @new : other;
				d.Add(p.Key, G.Pair(p.Value, handler));
			}
			foreach (var op in CallOperators)
				d.Add(op, G.Pair(Precedence.MaxValue, call));

			var rawText = OpenDelegate<OperatorPrinter>("PrintRawText");
			d[S.RawText] = G.Pair(EP.Substitute, rawText);

			return d;
		}

		#endregion

		static readonly int MinPrec = Precedence.MinValue.Lo;
		/// <summary>Context: beginning of statement (#namedArg not supported, allow multiple #var decl)</summary>
		internal static readonly Precedence StartStmt      = new Precedence(MinPrec, MinPrec, MinPrec);
		/// <summary>Context: beginning of expression (#var must have initial value)</summary>
		internal static readonly Precedence StartExpr      = new Precedence(MinPrec+1, MinPrec+1, MinPrec+1);
		/// <summary>Context: middle of expression, top level (#var and #namedArg not supported)</summary>
		internal static readonly Precedence ContinueExpr   = new Precedence(MinPrec+2, MinPrec+2, MinPrec+2);

		public void PrintExpr()
		{
			PrintExpr(StartExpr, Ambiguity.AllowUnassignedVarDecl);
		}
		protected internal void PrintExpr(Precedence context, Ambiguity flags = 0)
		{
			if (!EP.Primary.CanAppearIn(context) && !_n.IsParenthesizedExpr())
			{
				Debug.Assert((flags & Ambiguity.AllowUnassignedVarDecl) == 0);
				// Above EP.Primary (inside '$' or unary '.'), we can't use prefix 
				// notation or most other operators so we're very limited in what
				// we can print.
				if (!HasPAttrs(_n))
				{
					if (!_n.IsCall) {
						PrintSimpleSymbolOrLiteral(flags);
						return;
					}
				}
				PrintWithinParens(ParenFor.Grouping, _n);
				return;
			}

			NodeStyle style = _n.BaseStyle;
			if (style == NodeStyle.PrefixNotation)
				PrintPrefixNotation(context, true, flags, false);
			else {
				bool isVarDecl = IsVariableDecl(false, (flags & (Ambiguity.AllowUnassignedVarDecl|Ambiguity.ForEachInitializer)) != 0);
				
				int inParens = 0;
				if (_n.AttrCount != 0)
					inParens = PrintAttrs(ref context, isVarDecl ? AttrStyle.IsDefinition : AttrStyle.AllowKeywordAttrs, flags);
				
				bool startStmt = context.RangeEquals(StartStmt);
				bool startExpr = context.RangeEquals(StartExpr);
				
				if (isVarDecl && (startExpr || startStmt || (flags & Ambiguity.ForEachInitializer) != 0))
					PrintVariableDecl(false, context, flags);
				else if (startExpr && IsNamedArgument())
					PrintNamedArg(context);
				else if (!AutoPrintOperator(context, flags))
					PrintPrefixNotation(context, true, flags, true);

				WriteCloseParens(inParens);
			}
			if (context.Lo != StartStmt.Lo)
				PrintSuffixTrivia(false);
		}

		private void PrintNamedArg(Precedence context)
		{
			using (With(_n.Args[0]))
				PrintExpr(EP.Primary.LeftContext(context));
			WriteThenSpace(':', SpaceOpt.AfterColon);
			using (With(_n.Args[1]))
				PrintExpr(StartExpr);
		}

		// Checks if an operator with precedence 'prec' can appear in this context.
		bool CanAppearIn(Precedence prec, Precedence context, out bool extraParens, bool prefix = false)
		{
			extraParens = false;
			if (prec.CanAppearIn(context, prefix) && (prefix || MixImmiscibleOperators || prec.CanMixWith(context)))
				return true;
			if (_n.IsParenthesizedExpr())
				return true;
			if (AllowChangeParenthesis || !EP.Primary.CanAppearIn(context)) {
				Trace.WriteLineIf(!AllowChangeParenthesis, "Forced to write node in parens");
				return extraParens = true;
			}
			return false;
		}
		// Checks if an operator that may or may not be configured to output in 
		// `backtick notation` can appear in this context; this method may toggle
		// backtick notation to make it acceptable (in terms of precedence).
		bool CanAppearIn(ref Precedence prec, Precedence context, out bool extraParens, ref bool backtick, bool prefix = false)
		{
			var altPrec = EP.Backtick;
			if (backtick) MathEx.Swap(ref prec, ref altPrec);
			if (CanAppearIn(prec, context, out extraParens, prefix && !backtick))
				return true;

			backtick = !backtick;
			MathEx.Swap(ref prec, ref altPrec);
			return CanAppearIn(prec, context, out extraParens, prefix && !backtick);
		}

		private bool AutoPrintOperator(Precedence context, Ambiguity flags)
		{
			if (!_n.IsCall || !_n.HasSimpleHead())
				return false;
			Pair<Precedence, OperatorPrinter> info;
			if (OperatorPrinters.TryGetValue(_n.Name, out info))
				return info.Item2(this, info.Item1, context, flags);
			else if (_n.BaseStyle == NodeStyle.Operator)
			{
				if (_n.ArgCount == 2)
					return AutoPrintInfixBinaryOperator(EP.Backtick, context, flags | Ambiguity.UseBacktick);
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
		public bool AutoPrintPrefixUnaryOperator(Precedence precedence, Precedence context, Ambiguity flags)
		{
			if (!IsPrefixOperator(_n, false))
				return false;
			var name = _n.Name;
			var arg = _n.Args[0];

			bool needParens;
			if (CanAppearIn(precedence, context, out needParens, true))
			{
				// Check for the ambiguous case of (Foo)-x, (Foo)*x, etc.
				if ((flags & Ambiguity.CastRhs) != 0 && !needParens && (
					name == S._Dereference || name == S.PreInc || name == S.PreDec || 
					name == S._UnaryPlus || name == S._Negate || name == S.NotBits ||
					name == S._AddressOf) && !_n.IsParenthesizedExpr())
				{
					if (AllowChangeParenthesis)
						needParens = true; // Resolve ambiguity with extra parens
					else
						return false; // Fallback to prefix notation
				}
				// Check for the ambiguous case of "~Foo(...);"
				if (name == S.NotBits && context.Lo == StartStmt.Lo && arg.IsCall)
					return false;

				if (WriteOpenParen(ParenFor.Grouping, needParens))
					context = StartExpr;
				_out.Write(_n.Name.Name, true);
				PrefixSpace(precedence);
				PrintExpr(arg, precedence.RightContext(context), name == S.Forward ? Ambiguity.TypeContext : 0);
				//if (backtick) {
				//    Debug.Assert(precedence == EP.Backtick);
				//    if ((SpacingOptions & SpaceOpt.AroundInfix) != 0 && precedence.Lo < SpaceAroundInfixStopPrecedence)
				//        _out.Space();
				//    PrintOperatorName(_n.Name, Ambiguity.UseBacktick);
				//}
				WriteCloseParen(ParenFor.Grouping, needParens);
				return true;
			}
			return false;
		}
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintInfixBinaryOperator(Precedence prec, Precedence context, Ambiguity flags)
		{
			var name = _n.Name;
			Debug.Assert(!CastOperators.ContainsKey(name)); // not called for cast operators
			if (_n.ArgCount != 2)
				return false;
			// Attributes on the children disqualify operator notation
			LNode left = _n.Args[0], right = _n.Args[1];
			if (HasPAttrs(left) || HasPAttrs(right))
				return false;

			bool needParens, backtick = (_n.Style & NodeStyle.Alternate) != 0;
			if (CanAppearIn(ref prec, context, out needParens, ref backtick))
			{
				// Check for the ambiguous case of "A * b;" and consider using `*` instead
				if (name == S.Mul && context.Left == StartStmt.Left && IsComplexIdentifier(left)) {
					backtick = true;
					prec = EP.Backtick;
					if (!CanAppearIn(prec, context, out needParens, false))
						return false;
				}

				if (WriteOpenParen(ParenFor.Grouping, needParens))
					context = StartExpr;
				PrintExpr(left, prec.LeftContext(context), (name == S.Set || name == S.Lambda ? Ambiguity.AllowUnassignedVarDecl : 0));
				if (backtick)
					flags |= Ambiguity.UseBacktick;
				PrintInfixWithSpace(_n.Name, prec, flags);
				PrintExpr(right, prec.RightContext(context));
				WriteCloseParen(ParenFor.Grouping, needParens);
				return true;
			}
			return false;
		}
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintPrefixOrInfixOperator(Precedence infixPrec, Precedence context, Ambiguity flags)
		{
			if (_n.ArgCount == 2)
				return AutoPrintInfixBinaryOperator(infixPrec, context, flags);
			else
				return AutoPrintPrefixUnaryOperator(PrefixOperators[_n.Name], context, flags);
		}
		private void WriteOperatorName(Symbol name, Ambiguity flags = 0)
		{
			string opName = name.Name;
			if ((flags & Ambiguity.UseBacktick) != 0)
				PrintString(opName, '`', null);
			else if (opName.StartsWith("#"))
				_out.Write(opName.Substring(1), true);
			else
				_out.Write(opName, true);
		}
		
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintCastOperator(Precedence precedence, Precedence context, Ambiguity flags)
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
			Symbol name = _n.Name;
			bool alternate = (_n.Style & NodeStyle.Alternate) != 0 && !PreferOldStyleCasts;
			LNode subject = _n.Args[0], target = _n.Args[1];
			if (HasPAttrs(subject))
				return false;
			if (HasPAttrs(target) || (name == S.Cast && !IsComplexIdentifier(target, ICI.Default | ICI.AllowAnyExprInOf)))
				alternate = true;
			
			bool needParens;
			if (alternate)
				precedence = EP.Primary;
			if (!CanAppearIn(precedence, context, out needParens)) {
				// There are two different precedences for cast operators; we prefer 
				// the traditional forms (T)x, x as T, x using T which have lower 
				// precedence, but they don't work in this context so consider using 
				// x(->T), x(as T) or x(using T) instead.
				alternate = true;
				precedence = EP.Primary;
				if (!CanAppearIn(precedence, context, out needParens))
					return false;
			}

			if (alternate && PreferOldStyleCasts)
				return false; // old-style cast is impossible here

			if (WriteOpenParen(ParenFor.Grouping, needParens))
				context = StartExpr;

			if (alternate) {
				PrintExpr(subject, precedence.LeftContext(context));
				WriteOpenParen(ParenFor.NewCast);
				_out.Write(GetCastText(_n.Name), true);
				Space(SpaceOpt.AfterCastArrow);
				PrintType(target, StartExpr, Ambiguity.AllowPointer);
				WriteCloseParen(ParenFor.NewCast);
			} else {
				if (_n.Name == S.Cast) {
					WriteOpenParen(ParenFor.Grouping);
					PrintType(target, ContinueExpr, Ambiguity.AllowPointer);
					WriteCloseParen(ParenFor.Grouping);
					Space(SpaceOpt.AfterCast);
					PrintExpr(subject, precedence.RightContext(context), Ambiguity.CastRhs);
				} else {
					// "x as y" or "x using y"
					PrintExpr(subject, precedence.LeftContext(context));
					_out.Write(GetCastText(_n.Name), true);
					PrintType(target, precedence.RightContext(context));
				}
			}

			WriteCloseParen(ParenFor.Grouping, needParens);
			return true;
		}
		private string GetCastText(Symbol name)
		{
			if (name == S.UsingCast) return "using";
			if (name == S.As) return "as";
			return "->";
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintListOperator(Precedence precedence, Precedence context, Ambiguity flags)
		{
			// Handles one of: #tuple #`@` #`@@`.
			int argCount = _n.ArgCount;
			Symbol name = _n.Name;
			Debug.Assert(_n.IsCall);
			
			bool? braceMode;
			if (name == S.Tuple) {
				braceMode = false;
				flags &= Ambiguity.AllowUnassignedVarDecl;
			} else if (name == S.Braces) {
				// A braced block is not allowed at start of an expression 
				// statement; the parser would mistake it for a standalone 
				// braced block (the difference is that a standalone braced 
				// block ends automatically after '}', with no semicolon.)
				if (context.Left == StartStmt.Left || (flags & Ambiguity.NoBracedBlock) != 0)
					return false;
				braceMode = true;
				if (context.Left <= ContinueExpr.Left && _n.BaseStyle == NodeStyle.OldStyle)
					braceMode = null; // initializer mode
			} else if (name == S.ArrayInit) {
				braceMode = null; // initializer mode
			} else {
				Debug.Assert(name == S.CodeQuote || name == S.CodeQuoteSubstituting || name == S.List);
				_out.Write(name == S.CodeQuote ? "@" : "@@", false);
				braceMode = _n.BaseStyle == NodeStyle.Statement && (flags & Ambiguity.NoBracedBlock) == 0;
				flags = 0;
			}

			int c = _n.ArgCount;
			if (braceMode ?? true)
			{
				if (!Newline(NewlineOpt.BeforeOpenBraceInExpr))
					Space(SpaceOpt.OutsideParens);
				_out.Write('{', true);
				if (braceMode == true) {
					using (Indented) {
						for (int i = 0; i < c; i++)
							PrintStmt(_n.Args[i], i + 1 == c ? Ambiguity.FinalStmt : 0);
					}
				} else {
					_out.Space();
					for (int i = 0; i < c; i++) {
						if (i != 0) WriteThenSpace(',', SpaceOpt.AfterComma);
						PrintExpr(_n.Args[i], StartExpr, flags);
					}
				}
				if (!Newline(NewlineOpt.BeforeCloseBraceInExpr))
					_out.Space();
				_out.Write('}', true);
				if (!Newline(NewlineOpt.AfterCloseBraceInExpr))
					Space(SpaceOpt.OutsideParens);
			}
			else
			{
				WriteOpenParen(ParenFor.Grouping);
				for (int i = 0; i < c; i++)
				{
					if (i != 0) WriteThenSpace(',', SpaceOpt.AfterComma);
					PrintExpr(_n.Args[i], StartExpr, flags);
				}
				if (name == S.Tuple && c == 1)
					_out.Write(',', true);
				WriteCloseParen(ParenFor.Grouping);
			}
			return true;
		}

		static Symbol SpecialTypeKind(LNode n, Ambiguity flags, Precedence context)
		{
			// detects when notation for special types applies: Foo[], Foo*, Foo?
			// assumes IsComplexIdentifier() is already known to be true
			LNode first;
			if (n.Calls(S.Of, 2) && (first = n.Args[0]).IsId && (flags & Ambiguity.TypeContext)!=0) {
				var kind = first.Name;
				if (S.IsArrayKeyword(kind) || kind == S.QuestionMark)
					return kind;
				if (kind == S._Pointer && ((flags & Ambiguity.AllowPointer) != 0 || context.Left == StartStmt.Left))
					return kind;
			}
			return null;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintComplexIdentOperator(Precedence precedence, Precedence context, Ambiguity flags)
		{
			// Handles #of and #., including array types
			int argCount = _n.ArgCount;
			Symbol name = _n.Name;
			Debug.Assert((name == S.Of || name == S.Dot) && _n.IsCall);
			
			var first = _n.Args[0, null];
			if (first == null)
				return false; // no args

			bool needParens, needSpecialOfNotation = false;
			if (!CanAppearIn(precedence, context, out needParens) || needParens)
				return false; // this only happens inside $ operator, e.g. $(a.b)

			if (name == S.Dot) {
				// The trouble with the dot is its high precedence; because of 
				// this, arguments after a dot cannot use prefix notation as a 
				// fallback. For example "@.(a, b(c))" cannot be printed "a.b(c)"
				// since that means @.(a, b)(c)". The first argument to non-
				// unary "." can use prefix notation safely though, e.g. 
				// "@.(b(c), a)" can (and must) be printed "b(c).a".
				if (argCount > 2)
					return false;
				if (HasPAttrs(first))
					return false;
				LNode afterDot = _n.Args.Last;
				// Unary dot is no problem: .(a) is parsed the same as .a, i.e. 
				// the parenthesis are ignored, so we can print an expr like 
				// @`.`(a+b) as .(a+b), but the parser counts parens on binary 
				// dot, so in that case the argument after the dot must not be any 
				// kind of call (except substitution) and must not have attributes,
				// unless it is in parens.
				if (argCount == 2 && !afterDot.IsParenthesizedExpr()) {
					if (HasPAttrs(afterDot))
						return false;
					if (afterDot.IsCall && afterDot.Name != S.Substitute)
						return false;
				}
			} else if (name == S.Of) {
				var ici = ICI.Default | ICI.AllowAttrs;
				if ((flags & Ambiguity.InDefinitionName) != 0)
					ici |= ICI.NameDefinition;
				if (!IsComplexIdentifier(_n, ici)) {
					if (IsComplexIdentifier(_n, ici | ICI.AllowAnyExprInOf))
						needSpecialOfNotation = true;
					else
						return false;
				}
			}

			if (name == S.Dot)
			{
				if (argCount == 1) {
					_out.Write('.', true);
					PrintExpr(first, EP.Substitute);
				} else {
					PrintExpr(first, precedence.LeftContext(context), flags & Ambiguity.TypeContext);
					_out.Write('.', true);
					PrintExpr(_n.Args[1], precedence.RightContext(context));
				}
			}
			else if (_n.Name == S.Of)
			{
				// Check for special type names such as Foo? or Foo[]
				Symbol stk = SpecialTypeKind(_n, flags, context);
				if (stk != null)
				{
					if (S.IsArrayKeyword(stk)) {
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
						} while (S.IsArrayKeyword(stk = SpecialTypeKind(innerType, flags, context) ?? GSymbol.Empty));

						PrintType(innerType, EP.Primary.LeftContext(context), (flags & Ambiguity.AllowPointer));

						for (int i = 0; i < stack.Count; i++)
							_out.Write(stack[i].Name, true); // e.g. [] or [,]
					} else {
						PrintType(_n.Args[1], EP.Primary.LeftContext(context), (flags & Ambiguity.AllowPointer));
						_out.Write(stk == S._Pointer ? '*' : '?', true);
					}
					return true;
				}

				PrintExpr(first, precedence.LeftContext(context));

				_out.Write(needSpecialOfNotation ? "!(" : "<", true);
				for (int i = 1; i < argCount; i++) {
					if (i > 1)
						WriteThenSpace(',', SpaceOpt.AfterCommaInOf);
					PrintType(_n.Args[i], StartExpr, Ambiguity.InOf | Ambiguity.AllowPointer | (flags & Ambiguity.InDefinitionName));
				}
				_out.Write(needSpecialOfNotation ? ')' : '>', true);
			}
			else 
			{
				Debug.Assert(_n.Name == S.Substitute);
				G.Verify(AutoPrintOperator(ContinueExpr, 0));
			}
			return true;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintNewOperator(Precedence precedence, Precedence context, Ambiguity flags)
		{
			// Prints the new Xyz(...) {...} operator
			Debug.Assert (_n.Name == S.New);
			int argCount = _n.ArgCount;
			if (argCount == 0)
				return false;
			bool needParens;
			Debug.Assert(CanAppearIn(precedence, context, out needParens) && !needParens);

			LNode cons = _n.Args[0];
			LNode type = cons.Target;
			var consArgs = cons.Args;

			// There are two basic uses of new: for objects, and for arrays.
			// In all cases, #new has 1 arg plus optional initializer arguments,
			// and there's always a list of "constructor args" even if it is empty 
			// (exception: new {...}).
			// 1. Init an object: 1a. new Foo<Bar>() { ... }  <=> #new(Foo<bar>(...), ...)
			//                    1b. new { ... }             <=> #new(@``, ...)
			// 2. Init an array:  2a. new int[] { ... },      <=> #new(int[](), ...) <=> #new(#of(@`[]`, int)(), ...)
			//                    2b. new[,] { ... }.         <=> #new(@`[,]`(), ...)
			//                    2c. new int[10,10] { ... }, <=> #new(#of(@`[,]`, int)(10,10), ...)
			//                    2d. new int[10][] { ... },  <=> #new(#of(@`[]`, #of(@`[]`, int))(10), ...)
			if (HasPAttrs(cons))
				return false;
			if (type == null ? !cons.IsIdNamed(S.Missing) : HasPAttrs(type) || !IsComplexIdentifier(type))
				return false;

			// Okay, we can now be sure that it's printable, but is it an array decl?
			if (type == null) {
				// 1b, new {...}
				_out.Write("new ", true);
				PrintBracedBlockInNewExpr();
			} else if (type != null && type.IsId && S.CountArrayDimensions(type.Name) > 0) { // 2b
				_out.Write("new", true);
				_out.Write(type.Name.Name, true);
				Space(SpaceOpt.Default);
				PrintBracedBlockInNewExpr();
			} else {
				_out.Write("new ", true);
				int dims = CountDimensionsIfArrayType(type);
				if (dims > 0 && cons.Args.Count == dims) {
					PrintTypeWithArraySizes(cons);
				} else {
					// Otherwise we can print the type name without caring if it's an array or not.
					PrintType(type, EP.Primary.LeftContext(context));
					if (cons.ArgCount != 0 || (argCount == 1 && dims == 0))
						PrintArgList(cons, ParenFor.MethodCall, cons.ArgCount, 0, OmitMissingArguments);
				}
				if (_n.Args.Count > 1)
					PrintBracedBlockInNewExpr();
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
		private void PrintBracedBlockInNewExpr()
		{
			if (!Newline(NewlineOpt.BeforeOpenBraceInNewExpr))
				Space(SpaceOpt.BeforeNewInitBrace);
			WriteThenSpace('{', SpaceOpt.InsideNewInitializer);
			using (Indented) {
				Newline(NewlineOpt.AfterOpenBraceInNewExpr);
				for (int i = 1, c = _n.ArgCount; i < c; i++) {
					if (i != 1) {
						WriteThenSpace(',', SpaceOpt.AfterComma);
						Newline(NewlineOpt.AfterEachInitializerInNew);
					}
					PrintExpr(_n.Args[i], StartExpr);
				}
			}
			if (!Newline(NewlineOpt.BeforeCloseBraceInNewExpr))
				Space(SpaceOpt.InsideNewInitializer);
			_out.Write('}', true);
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
			
			_out.Write('[', true);
			bool first = true;
			foreach (var arg in cons.Args) {
				if (first) first = false;
				else WriteThenSpace(',', SpaceOpt.AfterComma);
				PrintExpr(arg, StartExpr, 0);
			}
			_out.Write(']', true);

			// Write the brackets for the inner array types
			for (int i = dimStack.Count - 1; i >= 0; i--)
				_out.Write(S.GetArrayKeyword(dimStack[i]).Name, true);
		}


		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintOtherSpecialOperator(Precedence precedence, Precedence context, Ambiguity flags)
		{
			// Handles one of:  ?  []  suf++  suf--
			int argCount = _n.ArgCount;
			Symbol name = _n.Name;
			if (argCount < 1)
				return false; // no args
			bool needParens;
			if (!CanAppearIn(precedence, context, out needParens))
				return false; // precedence fail

			// Verify that the special operator can appear at this precedence 
			// level and that its arguments fit the operator's constraints.
			var first = _n.Args[0];
			if (name == S.Bracks) {
				// Careful: a[] means #of(@`[]`, a) in a type context, @`[]`(a) otherwise
				int minArgs = (flags&Ambiguity.TypeContext)!=0 ? 2 : 1;
				if (argCount < minArgs || HasPAttrs(first))
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

			if (name == S.Bracks)
			{
				PrintExpr(first, precedence.LeftContext(context));
				Space(SpaceOpt.BeforeMethodCall);
				_out.Write('[', true);
				Space(SpaceOpt.InsideCallParens);
				for (int i = 1, c = _n.ArgCount; i < c; i++)
				{
					if (i != 1) WriteThenSpace(',', SpaceOpt.AfterComma);
					PrintExpr(_n.Args[i], StartExpr);
				}
				Space(SpaceOpt.InsideCallParens);
				_out.Write(']', true);
			}
			else if (name == S.QuestionMark)
			{
				PrintExpr(_n.Args[0], precedence.LeftContext(context));
				PrintInfixWithSpace(S.QuestionMark, EP.IfElse, 0);
				PrintExpr(_n.Args[1], ContinueExpr);
				PrintInfixWithSpace(S.Colon, EP.IfElse, 0);
				PrintExpr(_n.Args[2], precedence.RightContext(context));
			}
			else
			{
				Debug.Assert(name == S.PostInc || name == S.PostDec || name == S.IsLegal);
				PrintExpr(first, precedence.LeftContext(context));
				_out.Write(name == S.PostInc ? "++" : name == S.PostDec ? "--" : "is legal", true);
			}

			WriteCloseParen(ParenFor.Grouping, needParens);
			return true;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintCallOperator(Precedence precedence, Precedence context, Ambiguity flags)
		{
			// Handles "call operators" such as default(...) and checked(...)
			bool needParens;
			Debug.Assert(CanAppearIn(precedence, context, out needParens));
			Debug.Assert(_n.HasSpecialName);
			if (_n.ArgCount != 1)
				return false;
			var name = _n.Name;
			var arg = _n.Args[0];
			bool type = (name == S.Default || name == S.Typeof || name == S.Sizeof);
			if (type && !IsComplexIdentifier(arg, ICI.Default | ICI.AllowAttrs))
				return false;

			WriteOperatorName(name);
			PrintWithinParens(ParenFor.MethodCall, arg, type ? Ambiguity.TypeContext | Ambiguity.AllowPointer : 0);
			return true;
		}

		// Handles #rawText("custom string")
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool PrintRawText(Precedence mainPrec, Precedence context, Ambiguity flags)
		{
			if (OmitRawText)
				return false;
			_out.Write(GetRawText(_n), true);
			return true;
		}

		void PrintExpr(LNode n, Precedence context, Ambiguity flags = 0)
		{
			using (With(n))
				PrintExpr(context, flags);
		}
		void PrintType(LNode n, Precedence context, Ambiguity flags = 0)
		{
			using (With(n))
				PrintExpr(context, flags | Ambiguity.TypeContext);
		}

		public void PrintPrefixNotation(Ambiguity flags = Ambiguity.RecursivePrefixNotation, bool purePrefixNotation = false)
		{
			PrintPrefixNotation(StartExpr, purePrefixNotation, flags);
		}
		internal void PrintPrefixNotation(Precedence context, bool purePrefixNotation, Ambiguity flags = 0, bool skipAttrs = false)
		{
			Debug.Assert(EP.Primary.CanAppearIn(context) || _n.IsParenthesizedExpr());
			int inParens = 0;
			if (!skipAttrs)
				inParens = PrintAttrs(ref context, purePrefixNotation ? AttrStyle.NoKeywordAttrs : AttrStyle.AllowKeywordAttrs, flags);

			if (!_n.IsCall)
				PrintSimpleSymbolOrLiteral(flags);
			else if (!purePrefixNotation && IsComplexIdentifier(_n, ICI.Default | ICI.AllowAttrs | ICI.AllowParensAround))
				PrintExpr(context);
			else {
				if (!AllowConstructorAmbiguity && _n.Calls(_spaceName) && context == StartStmt && inParens == 0)
				{
					inParens++;
					WriteOpenParen(ParenFor.Grouping);
				}

				// Print Target
				var target = _n.Target;
				var f = Ambiguity.IsCallTarget;
				if (_spaceName == S.Def || context != StartStmt)
					f |= Ambiguity.AllowThisAsCallTarget;
				if (!purePrefixNotation && IsComplexIdentifier(target, ICI.Default | ICI.AllowAttrs | ICI.AllowParensAround)) {
					PrintExpr(target, EP.Primary.LeftContext(context), f);
				} else {
					PrintExprOrPrefixNotation(target, EP.Primary.LeftContext(context), purePrefixNotation, f | (flags & Ambiguity.RecursivePrefixNotation));
				}

				// Print argument list
				WriteOpenParen(ParenFor.MethodCall);

				var innerFlags = flags & Ambiguity.RecursivePrefixNotation;
				bool first = true;
				foreach (var arg in _n.Args) {
					if (OmitMissingArguments && IsSimpleSymbolWPA(arg, S.Missing) && _n.ArgCount > 1) {
						if (!first) WriteThenSpace(',', SpaceOpt.MissingAfterComma);
					} else {
						if (!first) WriteThenSpace(',', SpaceOpt.AfterComma);
						PrintExprOrPrefixNotation(arg, StartExpr, purePrefixNotation, innerFlags);
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
			object value = rawTextNode.Value;
			if (value == null || value == NoValue.Value) {
				var node = rawTextNode.Args[0, null];
				if (node != null)
					value = node.Value;
			}
			return (value ?? rawTextNode.Name).ToString();
		}
		private void PrintSimpleSymbolOrLiteral(Ambiguity flags)
		{
			Debug.Assert(_n.HasSimpleHead());
			if (_n.IsLiteral)
				PrintLiteral();
			else if (_n.Name == S.RawText && !OmitRawText)
				_out.Write(GetRawText(_n), true);
			else
				PrintSimpleIdent(_n.Name, flags, false, _n.AttrNamed(S.TriviaUseOperatorKeyword) != null);
		}
		internal void PrintExprOrPrefixNotation(LNode expr, Precedence context, bool purePrefixNotation, Ambiguity flags = 0)
		{
			using (With(expr))
				PrintExprOrPrefixNotation(context, purePrefixNotation, flags);
		}
		internal void PrintExprOrPrefixNotation(Precedence context, bool purePrefixNotation, Ambiguity flags)
		{
			if ((flags & Ambiguity.RecursivePrefixNotation) != 0)
				PrintPrefixNotation(context, purePrefixNotation, flags);
			else
				PrintExpr(context, flags);
		}

		private void PrintVariableDecl(bool printAttrs, Precedence context, Ambiguity allowPointer)
		{
			if (printAttrs)
				G.Verify(0 == PrintAttrs(StartExpr, AttrStyle.IsDefinition, 0));

			Debug.Assert(_n.Name == S.Var);
			var a = _n.Args;
			if (IsSimpleSymbolWPA(a[0], S.Missing))
				_out.Write("var", true);
			else
				PrintType(a[0], context, allowPointer & Ambiguity.AllowPointer);
			_out.Space();
			for (int i = 1; i < a.Count; i++) {
				var var = a[i];
				if (i > 1)
					WriteThenSpace(',', SpaceOpt.AfterComma);

				PrintExpr(var, EP.Assign.RightContext(context), Ambiguity.NoParenthesis);
			}
		}
	}
}
