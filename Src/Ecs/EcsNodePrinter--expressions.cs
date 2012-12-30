using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.ComponentModel;
using Loyc.Utilities;
using Loyc.Essentials;
using Loyc.Math;
using Loyc.CompilerCore;
using S = ecs.CodeSymbols;
using EP = ecs.EcsPrecedence;
using Loyc;

namespace ecs
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
			// The substitute operator \ also has higher precedence than Primary, 
			// but its special treatment is in the parser: the parser produces the
			// same tree for \(x) and \x, unlike e.g. ++(x) and ++x which are 
			// different trees. Therefore we can treat \ as a normal operator in
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
			P(S.Mod, EP.Multiply),      P(S.XorBits, EP.XorBits), 
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
			P(S.QuickBind, EP.Primary), P(S.In, EP.Equals)
		);

		static readonly Dictionary<Symbol,Precedence> CastOperators = Dictionary(
			P(S.Cast, EP.Prefix),      // (Foo)x      (preferred form)
			P(S.As, EP.Compare),       // x as Foo    (preferred form)
			P(S.UsingCast, EP.Compare) // x using Foo (preferred form)
		);

		static readonly HashSet<Symbol> ListOperators = new HashSet<Symbol>(new[] {
			S.List, S.Tuple, S.CodeQuote, S.CodeQuoteSubstituting, S.Braces});

		static readonly Dictionary<Symbol,Precedence> SpecialCaseOperators = Dictionary(
			// Operators that need special treatment (neither prefix nor infix nor casts)
			// #. #of #[] #postInc, #postDec, #, #'@', #'@@'. #tuple #?
			P(S.QuestionMark,EP.IfElse),  // a?b:c
			P(S.Bracks,      EP.Primary), // a[]
			P(S.PostInc,     EP.Primary), // x++
			P(S.PostDec,     EP.Primary), // x--
			P(S.Of,          EP.Primary), // List<int>, int[], int?, int*
			P(S.Dot,         EP.Primary), // a.b.c
			P(S.IsLegal,     EP.Compare), // x is legal
			P(S.New,         EP.Prefix)   // new A()
		);

		static readonly HashSet<Symbol> CallOperators = new HashSet<Symbol>(new[] {
			S.Typeof, S.Checked, S.Unchecked, S.Default,
		});


		delegate bool OperatorPrinter(EcsNodePrinter @this, Precedence mainPrec, Precedence context, Ambiguity flags);
		static Dictionary<Symbol, Pair<Precedence, OperatorPrinter>> OperatorPrinters = OperatorPrinters_();
		static Dictionary<Symbol, Pair<Precedence, OperatorPrinter>> OperatorPrinters_()
		{
			// Build a dictionary of printers for each operator name.
			var d = new Dictionary<Symbol, Pair<Precedence, OperatorPrinter>>();
			
			// Create open delegates to the printers for various kinds of operators
			var prefix = OpenDelegate<OperatorPrinter>("AutoPrintPrefixOperator");
			var infix = OpenDelegate<OperatorPrinter>("AutoPrintInfixOperator");
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

			return d;
		}

		#endregion

		static readonly int MinPrec = Precedence.MinValue.Lo;
		/// <summary>Context: beginning of statement (#namedArg not supported, allow multiple #var decl)</summary>
		public static readonly Precedence StartStmt      = new Precedence(MinPrec, MinPrec, MinPrec);
		/// <summary>Context: beginning of expression (#var must have initial value)</summary>
		public static readonly Precedence StartExpr      = new Precedence(MinPrec+1, MinPrec+1, MinPrec+1);
		/// <summary>Context: middle of expression, top level (#var and #namedArg not supported)</summary>
		public static readonly Precedence ContinueExpr   = new Precedence(MinPrec+2, MinPrec+2, MinPrec+2);

		/// <summary>Flags that represent special situations in EC# syntax.</summary>
		[Flags] public enum Ambiguity
		{
			/// <summary>The expression can contain uninitialized variable 
			/// declarations, e.g. because it is the subject of an assignment.
			/// In the tree "(x + y, int z) = (a, b)", this flag is passed down to 
			/// "(x + y, int z)" and then down to "int y" and "x + y", but it 
			/// doesn't propagate down to "x", "y" and "int".</summary>
			AllowUnassignedVarDecl = 1,
			/// <summary>The expression is the right side of a traditional cast, so 
			/// the printer must avoid ambiguity in case of the following prefix 
			/// operators: (Foo)-x, (Foo)+x, (Foo)&x, (Foo)*x, (Foo)~x, (Foo)++(x), 
			/// (Foo)--(x) (the (Foo)++(x) case is parsed as a post-increment and a 
			/// call).</summary>
			CastRhs = 2,
			/// <summary>The expression is in a location where, if it has the syntax 
			/// of a data type, it will be treated as a cast. This occurs when a 
			/// call that is printed with prefix notation has a parenthesized head
			/// node, e.g. (head)(arg). The head node can avoid the syntax of a data 
			/// type by adding "[ ]" (an empty set of attributes) at the beginning
			/// of the expression.</summary>
			AvoidCastAppearance = 4,
			/// <summary>No braced block permitted directly here (inside "if" clause)</summary>
			NoBracedBlock = 8,
			/// <summary>The current statement is the last one in the enclosing 
			/// block, so #result can be represented by omitting a semicolon.</summary>
			FinalStmt = 16,
			/// <summary>An expression is being printed in a context where a type
			/// is expected (its syntax has been verified in advance.)</summary>
			TypeContext = 32,
			/// <summary>The expression being printed is a complex identifier that
			/// may contain special attributes, e.g. <c>Foo&lt;out T></c>.</summary>
			InDefinitionName = 64,
			/// <summary>Inside angle brackets.</summary>
			InOf = 128,
			/// <summary>Allow pointer notation (when combined with TypeContext). 
			/// Also, a pointer is always allowed at the beginning of a statement,
			/// which is detected by the precedence context (StartStmt).</summary>
			AllowPointer = 256,
			/// <summary>Used to communicate to the operator printers that a binary 
			/// call should be expressed with the backtick operator.</summary>
			UseBacktick = 1024,
			/// <summary>Drop attributes only on the immediate expression being 
			/// printed. Used when printing the return type on a method, whose 
			/// attributes were already described by <c>[return: ...]</c>.</summary>
			DropAttributes = 2048,
			/// <summary>Forces a variable declaration to be allowed as the 
			/// initializer of a foreach loop.</summary>
			ForEachInitializer = 4096,
		}

		public void PrintExpr()
		{
			PrintExpr(StartExpr, Ambiguity.AllowUnassignedVarDecl);
		}
		protected internal void PrintExpr(Precedence context, Ambiguity flags = 0)
		{
			if (context > EP.Primary)
			{
				Debug.Assert((flags & Ambiguity.AllowUnassignedVarDecl) == 0);
				// Above EP.Primary (inside '\' or unary '.'), we can't use prefix 
				// notation or most other operators so we're very limited in what
				// we can print. If we have no attributes we can try to print as
				// an operator (this will work for prefix operators such as '++') 
				// and if that doesn't work, write the expr in parenthesis.
				if (!HasPAttrs(_n))
				{
					if (_n.IsSimpleSymbol) {
						PrintSimpleSymbolOrLiteral(flags);
						return;
					} else if (AutoPrintOperator(context, flags))
						return;
				}
				PrintWithinParens(ParenFor.Grouping, _n);
				return;
			}

			NodeStyle style = _n.BaseStyle;
			if (style == NodeStyle.PrefixNotation || style == NodeStyle.PurePrefixNotation)
				PrintPrefixNotation(context, false, style == NodeStyle.PurePrefixNotation, flags, false);
			else {
				bool startStmt = context.RangeEquals(StartStmt), needCloseParen = false;
				bool startExpr = context.RangeEquals(StartExpr);
				bool isVarDecl = IsVariableDecl(startStmt, startStmt || (flags & (Ambiguity.AllowUnassignedVarDecl|Ambiguity.ForEachInitializer)) != 0)
				              && (startExpr || startStmt || (flags & Ambiguity.ForEachInitializer) != 0);
				if (_n.AttrCount != 0)
					needCloseParen = PrintAttrs(context, isVarDecl ? AttrStyle.IsDefinition : AttrStyle.AllowKeywordAttrs, flags);

				if (!AutoPrintOperator(context, flags))
				{
					if (startExpr && IsNamedArgument())
						PrintNamedArg(context);
					else if (isVarDecl)
						PrintVariableDecl(false, context, flags);
					else
						PrintPrefixNotation(context, false, true, flags, true);
				}

				if (needCloseParen)
					_out.Write(')', true);
			}
		}

		private void PrintNamedArg(Precedence context)
		{
			using (With(_n.TryGetArg(0)))
				PrintExpr(EP.Primary.LeftContext(context));
			WriteThenSpace(':', SpaceOpt.AfterColon);
			using (With(_n.TryGetArg(1)))
				PrintExpr(StartExpr);
		}

		// Checks if an operator with precedence 'prec' can appear in this context.
		bool CanAppearIn(Precedence prec, Precedence context, out bool extraParens, bool prefix = false)
		{
			extraParens = false;
			if (prefix ? prec.PrefixCanAppearIn(context) 
				       : prec.CanAppearIn(context) && (MixImmiscibleOperators || prec.ShouldAppearIn(context)))
				return true;
			if (AllowExtraParenthesis || !EP.Primary.CanAppearIn(context))
				return extraParens = true;
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
			if (!_n.IsCall || !_n.HasSimpleHead)
				return false;
			Pair<Precedence, OperatorPrinter> info;
			if (OperatorPrinters.TryGetValue(_n.Name, out info))
				return info.Item2(this, info.Item1, context, flags);
			else if (_n.BaseStyle == NodeStyle.Operator)
			{
				if (_n.ArgCount == 2)
					return AutoPrintInfixOperator(EP.Backtick, context, flags | Ambiguity.UseBacktick);
				//if (_n.ArgCount == 1)
				//	return AutoPrintPrefixOperator(EP.Backtick, context, flags | Ambiguity.UseBacktick);
			}
			return false;
		}

		// These methods are public but hidden because they are found by reflection 
		// and they should be compatible with a partial-trust environment.
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintPrefixOperator(Precedence precedence, Precedence context, Ambiguity flags)
		{
			if (_n.ArgCount != 1)
				return false;
			// Attributes on the child disqualify operator notation (except \)
			var name = _n.Name;
			var arg = _n.TryGetArg(0);
			if (HasPAttrs(arg) && name != S.Substitute)
				return false;

			bool needParens;
			if (CanAppearIn(precedence, context, out needParens, true))
			{
				// Check for the ambiguous case of (Foo)-x, (Foo)*x, etc.
				if ((flags & Ambiguity.CastRhs) != 0 && !needParens && (
					name == S._Dereference || name == S.PreInc || name == S.PreDec || 
					name == S._UnaryPlus || name == S._Negate || name == S.NotBits ||
					name == S._AddressOf))// || name == S.Forward))
				{
					if (AllowExtraParenthesis)
						needParens = true; // Resolve ambiguity with extra parens
					else
						return false; // Fallback to prefix notation
				}
				// Check for the ambiguous case of "~Foo(...);"
				if (name == S.NotBits && context.Lo == StartStmt.Lo && arg.IsCall)
					return false;

				if (WriteOpenParen(ParenFor.Grouping, needParens))
					context = StartExpr;
				_out.Write(_n.Name.Name.Substring(1), true);
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
		public bool AutoPrintInfixOperator(Precedence prec, Precedence context, Ambiguity flags)
		{
			var name = _n.Name;
			Debug.Assert(!CastOperators.ContainsKey(name)); // not called for cast operators
			if (_n.ArgCount != 2)
				return false;
			// Attributes on the children disqualify operator notation
			INodeReader left = _n.TryGetArg(0), right = _n.TryGetArg(1);
			if (HasPAttrs(left) || HasPAttrs(right))
				return false;

			bool needParens, backtick = (_n.Style & NodeStyle.Alternate) != 0;
			if (CanAppearIn(ref prec, context, out needParens, ref backtick))
			{
				// Check for the ambiguous case of "Foo * bar;"
				if (name == S.Mul && context.Lo == StartStmt.Lo && IsComplexIdentifier(left))
					return false;

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
				return AutoPrintInfixOperator(infixPrec, context, flags);
			else
				return AutoPrintPrefixOperator(PrefixOperators[_n.Name], context, flags);
		}
		private void WriteOperatorName(Symbol name, Ambiguity flags = 0)
		{
			if ((flags & Ambiguity.UseBacktick) != 0)
				PrintString('`', null, name.Name);
			else {
				Debug.Assert(name.Name[0] == '#');
				string opName = name.Name.Substring(1);
				_out.Write(opName, true);
			}
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
			INodeReader subject = _n.TryGetArg(0), target = _n.TryGetArg(1);
			if (HasPAttrs(subject))
				return false;
			if (HasPAttrs(target) || (name == S.Cast && !IsComplexIdentifier(target)))
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
			// Handles one of: #tuple #'@' #'@@' #.
			int argCount = _n.ArgCount;
			Symbol name = _n.Name;
			Debug.Assert(_n.IsCall);
			
			bool braceMode;
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
			} else {
				Debug.Assert(name == S.CodeQuote || name == S.CodeQuoteSubstituting || name == S.List);
				_out.Write(name == S.CodeQuote ? "@" : name == S.CodeQuoteSubstituting ? "@@" : "#", false);
				braceMode = _n.BaseStyle == NodeStyle.Statement && (flags & Ambiguity.NoBracedBlock) == 0;
				flags = 0;
			}

			int c = _n.ArgCount;
			if (braceMode)
			{
				if (!Newline(NewlineOpt.BeforeOpenBraceInExpr))
					Space(SpaceOpt.OutsideParens);
				_out.Write('{', true);
				using (Indented)
				{
					for (int i = 0; i < c; i++)
						PrintStmt(_n.TryGetArg(i), i + 1 == c ? Ambiguity.FinalStmt : 0);
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
					PrintExpr(_n.TryGetArg(i), StartExpr, flags);
				}
				if (name == S.Tuple && c == 1)
					_out.Write(',', true);
				WriteCloseParen(ParenFor.Grouping);
			}
			return true;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintComplexIdentOperator(Precedence precedence, Precedence context, Ambiguity flags)
		{
			// Handles #of and #.
			int argCount = _n.ArgCount;
			Symbol name = _n.Name;
			Debug.Assert((name == S.Of || name == S.Dot) && _n.IsCall);
			var first = _n.TryGetArg(0);

			if (first == null)
				return false; // no args
			bool needParens;
			if (!CanAppearIn(precedence, context, out needParens) || needParens)
				return false; // this only happens inside \ operator, e.g. \(a.b)

			if (name == S.Dot) {
				if (argCount < 1)
					return false;
				// The trouble with the dot is its high precedence; because of 
				// this, arguments after a dot cannot use prefix notation as a 
				// fallback. For example "#.(a, b(c))" cannot be printed "a.b(c)"
				// since that means #.(a, b)(c)". The first argument to non-
				// unary "#." can use prefix notation safely though, e.g. 
				// "#.(b(c), a)" can (and must) be printed "b(c).a". Also,
				// #. must not directly contain other dotted expressions.
				// So: each argument after a dot must not be any kind of call 
				// and must not have attributes.
				if (argCount == 1) {
					if (first.IsCall || HasPAttrs(first))
						return false;
				} else {
					if (first.CallsMin(S.Dot, 1) || HasPAttrs(first))
						return false;
					for (int i = 1; i < argCount; i++) {
						var arg = _n.TryGetArg(i);
						if (arg.IsCall || HasPAttrs(arg))
							return false;
					}
				}
			} else if (name == S.Of) {
				var ici = ICI.Default | ICI.AllowAttrs;
				if ((flags & Ambiguity.InDefinitionName) != 0)
					ici |= ICI.NameDefinition;
				if (!IsComplexIdentifier(_n, ici))
					return false;
			}

			if (name == S.Dot)
			{
				if (argCount == 1) {
					_out.Write('.', true);
					PrintExpr(first, EP.Substitute);
				} else {
					PrintExpr(first, precedence.LeftContext(context), flags & Ambiguity.TypeContext);
					for (int i = 1; i < argCount; i++) {
						_out.Write('.', true);
						PrintExpr(_n.TryGetArg(i), precedence);
					}
				}
			}
			else if (_n.Name == S.Of)
			{
				if (_n.ArgCount == 2 && _n.Args[0].IsSimpleSymbol && (flags & Ambiguity.TypeContext)!=0)
				{
					var kind = first.Name;
					bool array = S.IsArrayKeyword(kind);
					if (array || kind == S.QuestionMark || 
						(kind == S._Pointer && ((flags & Ambiguity.AllowPointer) != 0 || context.Left == StartStmt.Left)))
					{
						PrintType(_n.TryGetArg(1), EP.Primary.LeftContext(context), (flags & Ambiguity.AllowPointer));
						if (array)
							_out.Write(kind.Name.Substring(1), true); // e.g. [] or [,]
						else
							_out.Write(kind == S.Mul ? '*' : '?', true);
						return true;
					}
				}

				PrintExpr(first, precedence.LeftContext(context));
				_out.Write('<', true);
				for (int i = 1; i < argCount; i++) {
					if (i > 1)
						WriteThenSpace(',', SpaceOpt.AfterCommaInOf);
					PrintType(_n.TryGetArg(i), ContinueExpr, Ambiguity.InOf | Ambiguity.AllowPointer | (flags & Ambiguity.InDefinitionName));
				}
				_out.Write('>', true);
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
			if (argCount < 1)
			{
				Debug.Assert(_n.IsCall);
				_out.Write("new()", true); // this is used in 'where' clauses
				return true;
			}
			bool needParens;
			Debug.Assert(CanAppearIn(precedence, context, out needParens) && !needParens);

			bool newArrayOf = false;
			// Verify that the special operator can appear at this precedence 
			// level and that its arguments fit the operator's constraints.
			var first = _n.TryGetArg(0);
			
			if (HasPAttrs(first))
				return false;
			// There are two basic uses of new:
			// 1. Init an object: new Foo<Bar>() { ... }
			// 2. Init an array:  new int[] { ... }, new[] { ... }.
			if (first.Calls(S.Of, 2) && first.TryGetArg(0).Name == S.Bracks) { // e.g. int[]
				newArrayOf = true;
				if (!IsComplexIdentifier(first))
					return false;
			} else {
				if (first.IsCall) {
					if (!IsComplexIdentifierOrNull(first.Head))
						return false;
				} else {
					// If there is only one argument and it's not a call, it must be "new[] {}"
					if (argCount == 1 && !(IsSimpleSymbolWPA(first, S.Bracks)))
						return false;
				}
			}

			_out.Write("new ", true);
				
			if (newArrayOf)
				PrintType(first, EP.Primary.LeftContext(context));
			else if (first.Name == S.Bracks && first.IsSimpleSymbol)
				_out.Write("[]", true);
			else {
				PrintExpr(first, EP.Primary.LeftContext(context));
				if (argCount == 1)
					return true;
			}

			if (!Newline(NewlineOpt.BeforeOpenBraceInNewExpr))
				Space(SpaceOpt.BeforeNewInitBrace);
			WriteThenSpace('{', SpaceOpt.InsideNewInitializer);
			using (Indented)
			{
				Newline(NewlineOpt.AfterOpenBraceInNewExpr);
				for (int i = 1; i < argCount; i++)
				{
					if (i != 1) {
						WriteThenSpace(',', SpaceOpt.AfterComma);
						Newline(NewlineOpt.AfterEachInitializerInNew);
					}
					PrintExpr(_n.TryGetArg(i), StartExpr);
				}
			}
			if (!Newline(NewlineOpt.BeforeCloseBraceInNewExpr))
				Space(SpaceOpt.InsideNewInitializer);
			_out.Write('}', true);
			Newline(NewlineOpt.AfterCloseBraceInNewExpr);
			
			return true;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool AutoPrintOtherSpecialOperator(Precedence precedence, Precedence context, Ambiguity flags)
		{
			// Handles one of: #? #[] #postInc #postDec
			int argCount = _n.ArgCount;
			Symbol name = _n.Name;
			if (argCount < 1)
				return false; // no args
			bool needParens;
			if (!CanAppearIn(precedence, context, out needParens))
				return false; // precedence fail

			// Verify that the special operator can appear at this precedence 
			// level and that its arguments fit the operator's constraints.
			var first = _n.TryGetArg(0);
			if (name == S.Bracks) {
				// Careful: a[] means #of(#[], a) in a type context, #[](a) otherwise
				int minArgs = (flags&Ambiguity.TypeContext)!=0 ? 2 : 1;
				if (argCount < minArgs || HasPAttrs(first))
					return false;
			} else if (name == S.QuestionMark) {
				if (argCount != 3 || HasPAttrs(first) || HasPAttrs(_n.TryGetArg(1)) || HasPAttrs(_n.TryGetArg(2)))
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
					PrintExpr(_n.TryGetArg(i), StartExpr);
				}
				Space(SpaceOpt.InsideCallParens);
				_out.Write(']', true);
			}
			else if (name == S.QuestionMark)
			{
				PrintExpr(_n.TryGetArg(0), precedence.LeftContext(context));
				PrintInfixWithSpace(S.QuestionMark, EP.IfElse, 0);
				PrintExpr(_n.TryGetArg(1), ContinueExpr);
				PrintInfixWithSpace(S.Colon, EP.IfElse, 0);
				PrintExpr(_n.TryGetArg(2), precedence.RightContext(context));
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
			bool needParens;
			Debug.Assert(CanAppearIn(precedence, context, out needParens));
			Debug.Assert(_n.IsKeyword);
			if (_n.ArgCount != 1)
				return false;
			var name = _n.Name;
			var arg = _n.TryGetArg(0);
			bool type = (name == S.Default || name == S.Typeof);
			if (type && !IsComplexIdentifier(arg, ICI.Default | ICI.AllowAttrs))
				return false;

			WriteOperatorName(name);
			PrintWithinParens(ParenFor.MethodCall, arg, type ? Ambiguity.TypeContext | Ambiguity.AllowPointer : 0);
			return true;
		}

		void PrintExpr(INodeReader n, Precedence context, Ambiguity flags = 0)
		{
			using (With(n))
				PrintExpr(context, flags);
		}
		void PrintType(INodeReader n, Precedence context, Ambiguity flags = 0)
		{
			using (With(n))
				PrintExpr(context, flags | Ambiguity.TypeContext);
		}

		public void PrintPrefixNotation(bool recursive = true, bool purePrefixNotation = false)
		{
			PrintPrefixNotation(StartExpr, recursive, purePrefixNotation);
		}
		internal void PrintPrefixNotation(Precedence context, bool recursive, bool purePrefixNotation, Ambiguity flags = 0, bool skipAttrs = false)
		{
			Debug.Assert(!(context > EP.Primary));
			bool needCloseParen = false;
			if (!skipAttrs)
				needCloseParen = PrintAttrs(context, purePrefixNotation ? AttrStyle.NoKeywordAttrs : AttrStyle.AllowKeywordAttrs, flags);

			if (!purePrefixNotation && IsComplexIdentifier(_n, ICI.Default | ICI.AllowAttrs))
			{
				PrintExpr(context);
				return;
			}

			// Print the head
			if (HasSimpleHeadWPA(_n))
				PrintSimpleSymbolOrLiteral(flags);
			else if (_n.IsParenthesizedExpr())
			{
				WriteOpenParen(ParenFor.Grouping);
				bool extraClose = false;
				if ((flags & Ambiguity.AvoidCastAppearance) != 0) {
					if (AllowExtraParenthesis) {
						extraClose = true;
						_out.Write('(', true);
					} else
						_out.Write("[ ] ", true);
				}
				PrintExprOrPrefixNotation(_n.Head, StartExpr, recursive, purePrefixNotation, flags & Ambiguity.AllowUnassignedVarDecl);
				if (extraClose)
					_out.Write(')', true);
				WriteCloseParen(ParenFor.Grouping);
			}
			else if (!purePrefixNotation && IsComplexIdentifier(_n.Head)) {
				PrintExpr(_n.Head, EP.Primary.LeftContext(context));
			} else {
				Debug.Assert(_n.IsCall);
				PrintExprOrPrefixNotation(_n.Head, EP.Primary.LeftContext(context), recursive, purePrefixNotation, Ambiguity.AvoidCastAppearance);
			}

			// Print args, if any
			if (_n.IsCall) {
				WriteOpenParen(ParenFor.MethodCall);
				var args = _n.Args;
				for (int i = 0, c = _n.ArgCount; i < c; i++) {
					if (i != 0)
						WriteThenSpace(',', SpaceOpt.AfterComma);
					PrintExprOrPrefixNotation(args[i], StartExpr, recursive, recursive ? purePrefixNotation : false);
				}
				WriteCloseParen(ParenFor.MethodCall);
			}
			if (needCloseParen)
				_out.Write(')', true);
		}

		private void PrintSimpleSymbolOrLiteral(Ambiguity flags)
		{
			Debug.Assert(_n.HasSimpleHead);
			if (_n.IsLiteral)
				PrintLiteral();
			else
				PrintSimpleIdent(_n.Name, flags, false, _n.TryGetAttr(S.StyleUseOperatorKeyword) != null);
		}
		internal void PrintExprOrPrefixNotation(INodeReader expr, Precedence context, bool prefix, bool purePrefixNotation, Ambiguity flags = 0)
		{
			using (With(expr))
				PrintExprOrPrefixNotation(context, prefix, purePrefixNotation, flags);
		}
		internal void PrintExprOrPrefixNotation(Precedence context, bool prefix, bool purePrefixNotation, Ambiguity flags)
		{
			if (prefix)
				PrintPrefixNotation(context, true, purePrefixNotation, flags);
			else
				PrintExpr(context, flags);
		}

		private void PrintVariableDecl(bool andAttrs, Precedence context, Ambiguity allowPointer) // skips attributes
		{
			if (andAttrs)
				PrintAttrs(StartExpr, AttrStyle.IsDefinition, 0);

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
				PrintSimpleIdent(var.Name, 0, false);
				if (var.IsCall) {
					PrintInfixWithSpace(S.Set, EP.Assign, 0);
					PrintExpr(var.Args[0], EP.Assign.RightContext(ContinueExpr));
				}
			}
		}
	}
}
