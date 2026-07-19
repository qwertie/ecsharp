using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.ComponentModel;
using Loyc;
using Loyc.Syntax;
using Loyc.Utilities;
using Loyc.Math;
using Loyc.Collections;
using S = Loyc.Ecs.EcsCodeSymbols;
using EP = Loyc.Ecs.EcsPrecedence;
using Loyc.Syntax.Lexing;
using Loyc.Syntax.Impl;

namespace Loyc.Ecs
{
	// This file: code for printing statements
	public partial class EcsNodePrinter
	{
		#region Sets and dictionaries of statements

		// Syntactic categories of statements:
		//
		// | Category            | Syntax example(s)      | Detection method          |
		// |---------------------|------------------------|---------------------------|
		// | Space definition    | struct X : Y {...}     | IsSpaceStatement()        |
		// | Variable decl       | int x = 2;             | IsVariableDecl()          |
		// | Other definitions   | delegate void f();     | Check DefinitionStmts     |
		// | Simple keyword stmt | goto label;            | Check SimpleStmts list    |
		// | Block stmt with or  | for (...) {...}        | Check BlockStmts list     |
		// |   without args      | try {...} catch {...}  |                           |
		// | Label stmt          | case 2: ... label:     | IsLabelStmt()             |
		// | Block or list       | { ... }                | Name is S.Braces          |
		// | Expression stmt     | x += y;                | When none of the above    |
		// | Assembly attribute  | [assembly: Foo]        | Name is S.Assembly        |

		//static readonly HashSet<Symbol> StmtsWithWordAttrs = AllNonExprStmts;

		/// <summary>Result from statement printer</summary>
		public enum SPResult {
			Fail,              // input tree did not have the expected format
			NeedSemicolon,     // caller should print semicolon & suffix trivia
			NeedSuffixTrivia   // caller should print suffix trivia
		};
		delegate SPResult StatementPrinter(EcsNodePrinter @this);
		static Dictionary<Symbol, StatementPrinter> StatementPrinters = StatementPrinters_();
		static Dictionary<Symbol, StatementPrinter> StatementPrinters_()
		{
			// Build a dictionary of printers for each operator name.
			var d = new Dictionary<Symbol, StatementPrinter>();
			AddAll(d, EcsFacts.SpaceDefinitionStmts, nameof(AutoPrintSpaceDefinition));
			AddAll(d, EcsFacts.OtherDefinitionStmts, nameof(AutoPrintMethodDefinition));
			d[S.Var]      = OpenDelegate<StatementPrinter>(nameof(AutoPrintVarDecl));
			d[S.Event]    = OpenDelegate<StatementPrinter>(nameof(AutoPrintEvent));
			d[S.Property] = OpenDelegate<StatementPrinter>(nameof(AutoPrintProperty));
			AddAll(d, EcsFacts.SimpleStmts, nameof(AutoPrintSimpleStmt));
			AddAll(d, EcsFacts.TwoArgBlockStmts, nameof(AutoPrintTwoArgBlockStmt));
			AddAll(d, EcsFacts.OtherBlockStmts, nameof(AutoPrintOtherBlockStmt));
			AddAll(d, EcsFacts.LabelStmts, nameof(AutoPrintLabelStmt));
			d[S.Braces] = OpenDelegate<StatementPrinter>(nameof(AutoPrintBlockOfStmts));
			d[S.Result] = OpenDelegate<StatementPrinter>(nameof(AutoPrintResult));
			d[S.Missing] = OpenDelegate<StatementPrinter>(nameof(AutoPrintMissingStmt));
			d[S.RawText] = OpenDelegate<StatementPrinter>(nameof(AutoPrintRawText));
			d[S.CsRawText] = OpenDelegate<StatementPrinter>(nameof(AutoPrintRawText));
			d[S.CsPPRawText] = OpenDelegate<StatementPrinter>(nameof(AutoPrintRawText));
			d[S.Assembly] = OpenDelegate<StatementPrinter>(nameof(AutoPrintAssemblyAttribute));
			d[S.PPNullable] = OpenDelegate<StatementPrinter>(nameof(AutoPrintPPStringDirective));
			d[S.CsiReference] = OpenDelegate<StatementPrinter>(nameof(AutoPrintPPStringDirective));
			d[S.CsiLoad] = OpenDelegate<StatementPrinter>(nameof(AutoPrintPPStringDirective));
			d[S.Splice] = OpenDelegate<StatementPrinter>(nameof(AutoPrintEmptySplice));
			return d;
		}
		static void AddAll(Dictionary<Symbol,StatementPrinter> d, HashSet<Symbol> names, string handlerName)
		{
			var method = OpenDelegate<StatementPrinter>(handlerName);
 			foreach(var name in names)
				d.Add(name, method);
		}
		
		#endregion

		void PrintStmt(LNode n, Ambiguity flags = 0)
		{
			using (With(n, StartStmt, flags))
				PrintCurrentStmt();
		}

		void PrintCurrentStmt()
		{
			Debug.Assert(_context == StartStmt);

			if (_o.AllowChangeParentheses || !_n.IsParenthesizedExpr())
			{
				var style = _n.BaseStyle;
				StatementPrinter printer;
				if (StatementPrinters.TryGetValueSafe(_name, out printer) && HasSimpleHeadWPA(_n))
				{
					if (_o.PreferPlainCSharp || _name == S.RawText || _name == S.CsRawText ||
						(style != NodeStyle.Expression && style != NodeStyle.PrefixNotation))
					{
						using (WithFlags(_flags | Ambiguity.NoParentheses)) {
							var result = printer(this);
							if (result != SPResult.Fail) {
								PrintTrivia(trailingTrivia: true, needSemicolon: result == SPResult.NeedSemicolon);
								return;
							}
						}
					}
				}

				if (style == NodeStyle.Special && AutoPrintMacroBlockCall(false))
					return;

				var attrs = _n.Attrs;
				for (int i = 0, c = attrs.Count; i < c; i++)
				{
					var a = attrs[i];
					if (a.Name == S.TriviaForwardedProperty && AutoPrintForwardedProperty())
						return;
				}
			}
			
			PrintCurrentExpr();
			PrintTrivia(trailingTrivia: true, needSemicolon: true);
		}

		// Handles block calls like `quote { }` and `match (_) { }`, and also `switch (_) { }`
		private bool AutoPrintMacroBlockCall(bool insideExpr)
		{
			var argCount = _n.ArgCount;
			if (argCount < 1)
				return false;
			var body = _n.Args.Last;
			if (!CallsWPAIH(body, S.Braces))
				return false;
			if (!_n.HasSimpleHead() && !IsComplexIdentifier(_n.Target))
				return false;
			if (insideExpr && _context.Left == StartStmt.Left)
				return false;

			if (argCount == 1)
			{
				if (_n.BaseStyle == NodeStyle.PrefixNotation && !_o.PreferPlainCSharp)
					return false;

				if (!insideExpr)
					G.Verify(0 == PrintAttrs(AttrStyle.AllowKeywordAttrs));

				if (_name != GSymbol.Empty) {
					PrintSimpleIdent(_name, 0);
					PrintBracedBlock(body, _name.Name.Length > 7 ? NewlineOpt.BeforeExecutableBrace : NewlineOpt.BeforeSimpleStmtBrace);
				} else {
					PrintExpr(_n.Target, EP.Primary.LeftContext(_context));
					PrintBracedBlock(body, NewlineOpt.BeforeExecutableBrace);
				}
				return true;
			}
			else // argCount > 1
			{
				bool isSwitch = _name == S.SwitchStmt;
				if (!isSwitch && (_n.BaseStyle == NodeStyle.PrefixNotation || _o.AvoidMacroSyntax))
					return false;

				if (!insideExpr)
					G.Verify(0 == PrintAttrs(AttrStyle.AllowKeywordAttrs));

				if (_name != GSymbol.Empty) {
					if (isSwitch)
						_out.Write("switch");
					else
						PrintSimpleIdent(_name, 0);
				} else
					PrintExpr(_n.Target, EP.Primary.LeftContext(_context));

				PrintArgList(_n.Args.WithoutLast(1), ParenFor.MacroCall, true, _o.OmitMissingArguments);

				PrintBracedBlockOrStmt(body, NewlineOpt.BeforeExecutableBrace);
				return true;
			}
		}
		private bool AutoPrintForwardedProperty()
		{
			if (!EcsValidators.IsForwardedProperty(_n, Pedantics))
				return false;

			G.Verify(0 == PrintAttrs(AttrStyle.AllowKeywordAttrs));
			PrintSimpleIdent(_name, 0);
			Space(SpaceOpt.BeforeForwardArrow);
			_out.Write("==>");
			PrefixSpace(EP.Forward);
			PrintExpr(_n.Args[0].Args[0], EP.Forward.RightContext(StartExpr));
			_out.Write(";");
			return true;
		}


		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintResult()
		{
			if (!IsResultExpr(_n) || !Flagged(Ambiguity.FinalStmt))
				return SPResult.Fail;
			G.Verify(0 == PrintAttrs(AttrStyle.NoKeywordAttrs));
			PrintExpr(_n.Args[0], StartExpr); // not StartStmt => allows multiplication e.g. a*b by avoiding ptr ambiguity
			return SPResult.NeedSuffixTrivia;
		}


		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintMissingStmt()
		{
			Debug.Assert(_name == S.Missing);
			if (!_n.IsId)
				return SPResult.Fail;
			G.Verify(0 == PrintAttrs(AttrStyle.AllowKeywordAttrs));
			return SPResult.NeedSemicolon;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintEmptySplice()
		{
			// LeMP can produce an empty #splice() with trivia if there is no other node 
			// on which to place the trivia, so we normally won't see non-empty splices.
			Debug.Assert(_name == S.Splice);
			if (_n.ArgCount != 0)
				return SPResult.Fail;
			G.Verify(0 == PrintAttrs(AttrStyle.AllowKeywordAttrs));
			return SPResult.NeedSuffixTrivia;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintRawText()
		{
			if (!_o.ObeyRawText)
				return SPResult.Fail;
			G.Verify(0 == PrintAttrs(AttrStyle.NoKeywordAttrs));

			WriteRawText(GetRawText(_n), _name == S.CsPPRawText);

			return SPResult.NeedSuffixTrivia;
		}

		// These methods are public but hidden because they are found by reflection 
		// and they should be compatible with a partial-trust environment.
		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintSpaceDefinition()
		{
			// Spaces: S.Struct, S.Class, S.Trait, S.Enum, S.Alias, S.Interface, S.Namespace
			var kind = EcsValidators.SpaceDefinitionKind(_n, Pedantics);
			if (kind == null)
				return SPResult.Fail;

			int? ai;
			var old_n = _n;
			if (kind == S.Alias && (ai = _n.Attrs.FirstIndexWhere(a => a.IsIdNamed(S.FilePrivate))) != null) {
				// Cause "[#filePrivate] #alias x = y;" to print as "using x = y;"
				_n = _n.WithAttrs(_n.Attrs.RemoveAt(ai.Value)).WithTarget(S.UsingStmt);
				kind = S.UsingStmt;
			}

			G.Verify(0 == PrintAttrs(AttrStyle.IsDefinition));

			LNode name = _n.Args[0], bases = _n.Args[1], body = _n.Args[2, null];
			WriteOperatorName(kind);
			
			_n = old_n;
			
			_out.Space();
			PrintExpr(name, ContinueExpr, Ambiguity.InDefinitionName);

			if (bases.CallsMin(S.AltList, 1))
			{
				Space(SpaceOpt.BeforeBaseListColon);
				WriteThenSpace(':', SpaceOpt.AfterColon);
				for (int i = 0, c = bases.ArgCount; i < c; i++) {
					if (i != 0)
						WriteThenSpace(',', SpaceOpt.AfterComma);
					PrintType(bases.Args[i], ContinueExpr);
				}
			}
			bool alias = name.Calls(S.Assign, 2);
			var name2 = name;
			if (name2.Calls(S.Of) || (alias && (name2 = name.Args[0]).Calls(S.Of)))
				PrintWhereClauses(name2);

			if (body == null)
				return SPResult.NeedSemicolon;

			PrintBracedBlock(body, NewlineOpt.BeforeSpaceDefBrace, false, KeyNameComponentOf(name), 
				mode: kind == S.Enum ? BraceMode.Enum : BraceMode.Normal);
			return SPResult.NeedSuffixTrivia;
		}

		/// <summary>Given a complex name such as <c>global::Foo&lt;int>.Bar&lt;T></c>,
		/// this method identifies the base name component, which in this example 
		/// is Bar. This is used, for example, to identify the expected name for
		/// a constructor based on the class name, e.g. <c>Foo&lt;T></c> => Foo.</summary>
		/// <remarks>This was moved to EcsValidators.</remarks>
		public static Symbol KeyNameComponentOf(LNode name)
		{
			return EcsValidators.KeyNameComponentOf(name);
		}

		private void PrintWhereClauses(LNode name)
		{
			// Example: @'of(Foo, [#where(#class, IEnumerable)] T)
			//          represents Foo<T> where T: class, IEnumerable
			if (!name.Calls(S.Of))
				return;

			// Look for "where" clauses and print them
			bool first = true;
			for (int i = 1, c = name.ArgCount; i < c; i++)
			{
				var param = name.Args[i];
				for (int a = 0, ac = param.AttrCount; a < ac; a++)
				{
					var where = param.Attrs[a];
					if (where.CallsMin(S.WhereClause, 1))
					{
						using (Indented)
						using (With(where, StartExpr))
						{
							if (!Newline(first ? NewlineOpt.BeforeWhereClauses : NewlineOpt.BeforeEachWhereClause))
								_out.Space();
							first = false;
							_out.Write("where").Space();
							PrintSimpleIdent(param.Name, 0);
							Space(SpaceOpt.BeforeWhereClauseColon);
							WriteThenSpace(':', SpaceOpt.AfterColon);
							bool firstC = true;
							foreach (var constraint in where.Args)
							{
								using (With(constraint, StartExpr))
								{
									if (firstC)
										firstC = false;
									else
										WriteThenSpace(',', SpaceOpt.AfterComma);
									if (constraint.Name == S.New && constraint.ArgCount == 0)
										_out.Write("new()");
									else if (constraint.IsId && (constraint.Name == S.Class || constraint.Name == S.Struct))
										WriteOperatorName(constraint.Name);
									else
										PrintCurrentExpr();
								}
							}
						}
					}
				}
			}
		}

		// Prints a child statement that could be a braced block, or not
		private bool PrintBracedBlockOrStmt(LNode child, NewlineOpt beforeBrace = NewlineOpt.BeforeExecutableBrace)
		{
			var name = child.Name;
			if (name == S.Braces && !HasPAttrs(child) && HasSimpleHeadWPA(child))
			{
				PrintBracedBlock(child, beforeBrace);
				return true;
			}
			// Detect "else if (...)", and suppress newline/indent between "else" and "if".
			if (name == S.If && Flagged(Ambiguity.ElseClause))
			{
				if (EcsValidators.OtherBlockStmtType(_n, Pedantics) == S.If) {
					PrintStmt(child, _flags & (Ambiguity.FinalStmt | Ambiguity.ElseClause));
					return false;
				}
			}
			using (Indented)
			{
				PrintStmt(child, _flags & (Ambiguity.FinalStmt | Ambiguity.NoIfWithoutElse) | Ambiguity.NewlineBeforeChildStmt);
				return false;
			}
		}

		static readonly Symbol _openBrace = GSymbol.Get("'{");

		enum BraceMode { Normal, BlockExpr, BlockStmt, Enum, SwitchExpression, AutoProp, Initializer };

		private void PrintBracedBlock(LNode body, NewlineOpt beforeBrace, bool skipFirstStmt = false, Symbol spaceName = null, BraceMode mode = BraceMode.Normal)
		{
			if (mode != BraceMode.BlockExpr)
			{
				if (mode != BraceMode.BlockStmt)
					PrintTrivia(body, trailingTrivia: false);
				else
					G.Verify(PrintAttrs(AttrStyle.AllowKeywordAttrs) == 0);
				if (!_out.LastCharWritten.IsOneOf('\n', ' ', '\t') && beforeBrace != 0)
					NewlineOrSpace(beforeBrace, IsDefaultNewlineSuppressed(body));
			}
			_out.Write('{');
			// body.Target represents the opening brace. Injector adds trailing trivia only, not leading
			PrintTrivia(body.Target, trailingTrivia: true);

			using (WithSpace(spaceName))
			{
				if (mode == BraceMode.Initializer || mode == BraceMode.Enum || mode == BraceMode.SwitchExpression)
				{
					Debug.Assert(!skipFirstStmt);
					PrintExpressionsInBraces(body, mode == BraceMode.Initializer,
						mode == BraceMode.SwitchExpression ? Ambiguity.InSwitchExpr : 0);
				}
				else
					PrintStatementsInBraces(body, skipFirstStmt, newlinesByDefault: mode != BraceMode.AutoProp);
			}

			_out.Write('}');
			if (mode != BraceMode.BlockStmt)
				PrintTrivia(body, trailingTrivia: true);
		}

		private void PrintStatementsInBraces(LNode braces, bool skipFirstStmt = false, bool newlinesByDefault = true)
		{
			bool anyNewlines = false;
			using (Indented)
				for (int i = (skipFirstStmt ? 1 : 0), c = braces.ArgCount; i < c; i++) {
					var stmt = braces.Args[i];
					// Bug fix: check if '\n' was just written to avoid a space before 'g' in
					// @`%trailing`(`%newline`) f();
					// @`%appendStatement` g();
					if (!newlinesByDefault || IsDefaultNewlineSuppressed(stmt) || !Newline(NewlineOpt.Default)) {
						if (!_out.IsAtStartOfLine)
							Space(SpaceOpt.Default);
						else
							anyNewlines = true;
					} else
						anyNewlines = true;
					PrintStmt(stmt, i + 1 == c ? Ambiguity.FinalStmt : 0);
				}
			NewlineOrSpace(NewlineOpt.Minimal, forceSpace: !anyNewlines);
		}

		private void PrintExpressionsInBraces(LNode body, bool isInitializer, Ambiguity flags)
		{
			bool anyNewlines = false;
			using (Indented)
			{
				NewlineOpt nlo = isInitializer ? NewlineOpt.AfterOpenBraceInNewExpr : NewlineOpt.Minimal | NewlineOpt.BeforeEachEnumItem;
				NewlineOpt next_nlo = isInitializer ? NewlineOpt.AfterEachInitializer : NewlineOpt.BeforeEachEnumItem;

				for (int i = 0, c = body.ArgCount; i < c; i++)
				{
					var stmt = body.Args[i];
					if (i != 0) {
						_out.Write(',');
						nlo = next_nlo;
					}
					if (NewlineOrSpace(nlo, IsDefaultNewlineSuppressed(stmt), SpaceOpt.AfterComma))
						anyNewlines = true;
					PrintExpr(stmt, StartExpr, flags);
				}
			}
			NewlineOrSpace(isInitializer ? NewlineOpt.BeforeCloseBraceInExpr : NewlineOpt.Default, !anyNewlines);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintMethodDefinition()
		{
			// S.Fn, S.Delegate: #fn(#int32, Square, #(int x), { return x * x; });
			if (EcsValidators.MethodDefinitionKind(_n, true, Pedantics) == null)
				return SPResult.Fail;

			LNode retType = _n.Args[0], name = _n.Args[1];
			LNode args = _n.Args[2];
			LNode body = _n.Args[3, null];
			bool isConstructor = _name == S.Constructor;
			bool isDestructor = !isConstructor && name.Calls(S._Destruct, 1);
			
			LNode firstStmt = null;
			if (isConstructor && body != null && body.CallsMin(S.Braces, 1)) {
				// Detect ": this(...)" or ": base(...)"
				firstStmt = body.Args[0];
				if (!CallsWPAIH(firstStmt, S.This) &&
					!CallsWPAIH(firstStmt, S.Base))
					firstStmt = null;
			}

			if (!_o.AllowConstructorAmbiguity) {
				if (isDestructor && _spaceName == S.Fn)
					// When destructor syntax is ambiguous, use prefix notation.
					return SPResult.Fail;
				else if (isConstructor && firstStmt == null) {
					// When constructor syntax is ambiguous, use prefix notation.
					if (name.IsIdNamed(S.This)) {
						if (_spaceName == S.Fn)
							return SPResult.Fail;
					} else if (!name.IsIdNamed(_spaceName))
						return SPResult.Fail;
				}
			}

			// A cast operator with the structure: #fn(Foo, [@`%useOperatorKeyword`] @'cast, #(...))
			// can be printed in a special format: operator Foo(...);
			// Note: operator bool is a cast operator but operator true/false are not
			bool isCastOperator = (name.Name == S.Cast && name.AttrNamed(S.TriviaUseOperatorKeyword) != null);

			PrintTypeAndName(isConstructor || isDestructor, isCastOperator, 
				isConstructor && !name.IsIdNamed(S.This) ? AttrStyle.IsConstructor : AttrStyle.IsDefinition);

			PrintTrivia(args, trailingTrivia: false);
			PrintArgTuple(args, ParenFor.MethodDecl, true, _o.OmitMissingArguments);
			PrintTrivia(args, trailingTrivia: true);

			PrintWhereClauses(name);
			
			// If this is a constructor where the first statement is this(...) or 
			// base(...), we must change the notation to ": this(...) {...}" as
			// required in plain C#
			if (firstStmt != null) {
				using (Indented) {
					if (!IsDefaultNewlineSuppressed(firstStmt))
						Newline(NewlineOpt.BeforeConstructorColon);
					Space(SpaceOpt.BeforeConstructorColon);
					WriteThenSpace(':', SpaceOpt.AfterColon);
					PrintExpr(firstStmt, StartExpr, Ambiguity.NoBracedBlock);
				}
			}

			return AutoPrintBodyOfMethodOrProperty(body, firstStmt != null);
		}

		private bool IsDefaultNewlineSuppressed(LNode node)
		{
			return node.AttrNamed(S.TriviaAppendStatement) != null;
		}

		// e.g. given the method void f() {...}, prints "void f"
		//      for a cast operator #fn(Foo, #cast, #(...)) it prints "operator Foo" if requested
		private void PrintTypeAndName(bool isConstructor, bool isCastOperator = false, AttrStyle attrStyle = AttrStyle.IsDefinition, string eventKeywordOpt = null)
		{
			LNode retType = _n.Args[0], name = _n.Args[1];

			if (retType.HasPAttrs())
				using (With(retType, StartStmt))
					G.Verify(0 == PrintAttrs(AttrStyle.NoKeywordAttrs, null, "return"));

			G.Verify(0 == PrintAttrs(attrStyle));

			var target = _n.Target; // #fn or #prop
			PrintTrivia(target, trailingTrivia: false);
			PrintTrivia(target, trailingTrivia: true);

			if (eventKeywordOpt != null)
				_out.Write(eventKeywordOpt);

			if (_name == S.Delegate)
			{
				_out.Write("delegate ");
			}
			if (isCastOperator)
			{
				_out.Write("operator ");
				PrintType(retType, ContinueExpr, Ambiguity.AllowPointer);
			}
			else
			{
				if (!isConstructor) {
					PrintType(retType, ContinueExpr, Ambiguity.AllowPointer | Ambiguity.NoParentheses);
					if (!_out.IsAtStartOfLine)
						_out.Space();
				}
				if (isConstructor && name.IsIdNamed(S.This))
					_out.Write("this");
				else if (name.IsLiteral) { // operator true/false
					_out.Write("operator ");
					using (With(name, Precedence.MaxValue)) PrintLiteral();
				} else { // Normal name
					PrintExpr(name, ContinueExpr, Ambiguity.InDefinitionName | Ambiguity.NoParentheses);
				}
			}
		}
		private void PrintArgTuple(LNode tuple, ParenFor kind, bool allowUnassignedVarDecl, bool omitMissingArguments, char separator = ',')
		{
			_out.BeginNode(tuple);
			PrintArgList(tuple.Args, kind, allowUnassignedVarDecl, omitMissingArguments, separator);
			_out.EndNode();
		}
		private void PrintArgList(LNodeList args, ParenFor kind, bool allowUnassignedVarDecl, bool omitMissingArguments, char separator = ',')
		{
			var keepFlags = _flags & (Ambiguity.InPattern | Ambiguity.IsPattern);
			using (WithFlags(keepFlags | (allowUnassignedVarDecl ? Ambiguity.AllowUnassignedVarDecl : 0))) {
				WriteOpenParen(kind);
				_out.Indent(PrinterIndentHint.Brackets);
				PrintArgs(args, _flags, omitMissingArguments, separator);
				_out.Dedent(PrinterIndentHint.Brackets);
				WriteCloseParen(kind);
			}
		}
		private void PrintArgs(LNode args, bool omitMissingArguments, char separator = ',')
		{
			PrintArgs(args.Args, _flags, omitMissingArguments, separator);
		}
		private void PrintArgs(LNodeList args, Ambiguity flags, bool omitMissingArguments, char separator = ',')
		{
			for (int i = 0; i < args.Count; i++)
			{
				var arg = args[i];
				bool missing = omitMissingArguments && IsSimpleSymbolWPA(arg, S.Missing) && args.Count > 1;
				if (i != 0)
					WriteThenSpace(separator, missing ? SpaceOpt.MissingAfterComma : SpaceOpt.AfterComma);
				if (!missing)
					PrintExpr(arg, StartExpr, flags);
			}
		}
		private SPResult AutoPrintBodyOfMethodOrProperty(LNode body, bool skipFirstStmt = false)
		{
			using (WithSpace(S.Fn)) {
				if (body == null)
					return SPResult.NeedSemicolon;
				if (body.Name == S.Forward)
				{
					Space(SpaceOpt.BeforeForwardArrow);
					_out.Write("==>");
					PrefixSpace(EP.Forward);
					PrintExpr(body.Args[0], EP.Forward.RightContext(StartExpr));
					return SPResult.NeedSemicolon;
				}
				else if (body.Name == S.Braces && (_o.PreferPlainCSharp || body.BaseStyle != NodeStyle.PrefixNotation))
				{
					PrintBracedBlock(body, NewlineOpt.BeforeMethodBrace, skipFirstStmt, S.Fn, mode: IsAutoPropBody(body) ? BraceMode.AutoProp : BraceMode.Normal);
					return SPResult.NeedSuffixTrivia;
				}
				else
				{
					PrefixSpace(EP.Lambda);
					_out.Write("=>");
					PrefixSpace(EP.Lambda);
					_out.Indent(PrinterIndentHint.Subexpression);
					PrintExpr(body, EP.Lambda.RightContext(StartExpr));
					_out.Dedent(PrinterIndentHint.Subexpression);
					return SPResult.NeedSemicolon;
				}
			}
		}

		private bool IsAutoPropBody(LNode body)
		{
			return body.ArgCount.IsInRange(1, 2) && body.Args.All(s =>
				s.IsId && s.Name.IsOneOf(S.get, S.set, S.add, S.remove));
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintProperty()
		{
			// For S.Property (#property), _n typically looks like this: 
			// #property(int, Foo, @``, { 
			//     get({ return _foo; });
			//     set({ _foo = value; });
			// });
			if (!EcsValidators.IsPropertyDefinition(_n, Pedantics))
				return SPResult.Fail;

			PrintTypeAndName(false);

			// Detect if property has argument list (T this[...] {...})
			if (_n.Args[2].Calls(S.AltList))
			{
				// Do what PrintArgList does, only with [] instead of ()
				Space(SpaceOpt.BeforeMethodDeclArgList);
				_out.Write('[');
				WriteInnerSpace(ParenFor.MethodDecl);
				PrintArgs(_n.Args[2].Args, _flags | Ambiguity.AllowUnassignedVarDecl, false);
				WriteInnerSpace(ParenFor.MethodDecl);
				_out.Write(']');
			}

			PrintWhereClauses(_n.Args[1]);

			var spr = AutoPrintBodyOfMethodOrProperty(_n.Args[3, null]);
			if (_n.Args.Count >= 5) {
				var initializer = _n.Args[4];
				if (!initializer.IsIdNamed(S.Missing)) {
					PrintInfixWithSpace(S.Assign, null, EcsPrecedence.Assign);
					PrintExpr(initializer, StartExpr, _flags);
					return SPResult.NeedSemicolon;
				}
			}
			return spr;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintVarDecl()
		{
			if (!IsVariableDecl(true, true))
				return SPResult.Fail;

			_flags |= Ambiguity.AllowUnassignedVarDecl;
			PrintVariableDecl(true);
			return SPResult.NeedSemicolon;
		}
		
		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintEvent()
		{
			LNode type, name, body;
			if (!EcsValidators.IsEventDefinition(_n, out type, out name, out body, Pedantics))
				return SPResult.Fail;

			G.Verify(0 == PrintAttrs(AttrStyle.IsDefinition));
			_out.Write("event ");
			PrintType(type, ContinueExpr, Ambiguity.AllowPointer);
			_out.Space();

			if (name.Calls(S.AltList)) {
				bool first = true;
				foreach (var name2 in name.Args) {
					if (first)
						first = false;
					else
						WriteThenSpace(',', SpaceOpt.AfterComma);
					PrintExpr(name2, ContinueExpr);
				}
			} else
				PrintExpr(name, ContinueExpr);

			return AutoPrintBodyOfMethodOrProperty(body);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintSimpleStmt()
		{
			// S.Break, S.Continue, S.Goto, S.GotoCase, S.Return, S.Throw, S.Import
			if (!EcsValidators.IsSimpleExecutableKeywordStmt(_n, Pedantics))
				return SPResult.Fail;

			LNode usingStatic = _name == S.Import && _n.AttrCount > 0 && _n.Attrs.Last.IsIdNamed(S.Static) ? _n.Attrs.Last : null;
			var allowAttrs = (_name == S.Import ? AttrStyle.AllowKeywordAttrs : AttrStyle.AllowWordAttrs);
			G.Verify(0 == PrintAttrs(allowAttrs, usingStatic));

			PrintReturnThrowEtc(usingStatic != null ? _using_static : _name, _n.Args[0, null]);

			return SPResult.NeedSemicolon;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintPPStringDirective()
		{
			// S.CsiReference, S.CsiLoad.
			if (_n.ArgCount != 1 || !(_n[0].Value is string value) || value.Contains('\n'))
				return SPResult.Fail;

			G.Verify(0 == PrintAttrs(AttrStyle.NoKeywordAttrs));
			if (_n.Name == S.CsiReference)
				_out.Write("#r");
			else
				_out.Write(_n.Name.Name.Substring(1));
			_out.Space();
			_out.Write(value);
			_out.NewlineIsRequiredHere();

			return SPResult.NeedSuffixTrivia;
		}

		readonly Symbol _using_static = (Symbol)"using static";

		public void PrintReturnThrowEtc(Symbol name, LNode arg)
		{
			if (name == S.GotoCase)
				_out.Write("goto case");
			else if (name == _using_static)
				_out.Write("using static");
			else if (name == S.Import)
				_out.Write("using");
			else if (name == S.Goto && _n.ArgCount == 1 && _n.Args[0].IsIdNamed(S.Default)) {
				_out.Write("goto default");
				return;
			} else
				WriteOperatorName(name);

			if (arg != null) {
				Space(SpaceOpt.Minimal);
				PrintExpr(arg, StartExpr);
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintTwoArgBlockStmt()
		{
			// S.Do, S.Fixed, S.Lock, S.SwitchStmt, S.UsingStmt, S.While
			var type = EcsValidators.TwoArgBlockStmtType(_n, Pedantics);
			if (type == null)
				return SPResult.Fail;

			var allowAttrs = (_name == S.UsingStmt ? AttrStyle.AllowKeywordAttrs : AttrStyle.AllowWordAttrs);
			G.Verify(0 == PrintAttrs(allowAttrs));

			if (type == S.DoWhile)
			{
				_out.Write("do");
				bool braces = PrintBracedBlockOrStmt(_n.Args[0], NewlineOpt.BeforeSimpleStmtBrace);

				// Print newline in front of "while" if appropriate and avoid printing 
				// "while (\ncondition)" when condition has an explicit newline; use
				// "\nwhile (condition)" instead.
				LNode cond = _n.Args[1];
				LNode condWithoutNewline = cond.WithoutAttrNamed(S.TriviaNewline);
				if (cond != condWithoutNewline)
					_out.Newline();
				else if (!Newline(braces ? NewlineOpt.BeforeExecutableBrace : NewlineOpt.Default))
					Space(SpaceOpt.Default);

				_out.Write("while");
				PrintWithinParens(ParenFor.KeywordCall, condWithoutNewline);
				return SPResult.NeedSemicolon;
			}
			else
			{
				WriteOperatorName(_name);
				Ambiguity argFlags = 0;
				if (_name == S.Fixed)
					argFlags |= Ambiguity.AllowPointer;
				PrintWithinParens(ParenFor.KeywordCall, _n.Args[0], argFlags);
				PrintBracedBlockOrStmt(_n.Args[1]);
				return SPResult.NeedSuffixTrivia;
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintOtherBlockStmt()
		{
			// S.If, S.For, S.ForEach, S.Checked, S.Unchecked, S.Try
			var type = EcsValidators.OtherBlockStmtType(_n, Pedantics);
			if (type == null)
				return SPResult.Fail;

			if (type == S.If)
			{
				var @else = _n.Args[2, null];
				bool needCloseBrace = false;
				if (@else == null && Flagged(Ambiguity.NoIfWithoutElse)) {
					if (_o.AllowExtraBraceForIfElseAmbig) {
						_out.Write('{');
						needCloseBrace = true;
					} else
						return SPResult.Fail;
				}

				// Note: the "if" statement in particular cannot have "word" attributes
				//       because they would create ambiguity with property declarations
				G.Verify(0 == PrintAttrs(AttrStyle.AllowKeywordAttrs));

				_out.Write("if");
				PrintWithinParens(ParenFor.KeywordCall, _n.Args[0]);

				var thenFlags = _flags & ~(Ambiguity.ElseClause);
				if (@else != null) thenFlags |= Ambiguity.NoIfWithoutElse;
				bool braces;
				using (WithFlags(thenFlags))
					braces = PrintBracedBlockOrStmt(_n.Args[1]);
				
				if (@else != null) {
					if (Newline(braces ? NewlineOpt.BeforeExecutableBrace : NewlineOpt.Default))
						@else = @else.WithoutAttrNamed(S.TriviaNewline);
					else
						Space(SpaceOpt.Default);
					_out.Write("else");
					using (WithFlags(_flags | Ambiguity.ElseClause))
						PrintBracedBlockOrStmt(@else);
				}

				if (needCloseBrace)
					_out.Write('}');
				return SPResult.NeedSuffixTrivia;
			}

			G.Verify(0 == PrintAttrs(AttrStyle.AllowWordAttrs));

			if (type == S.For)
			{
				_out.Write("for");
				WriteOpenParen(ParenFor.KeywordCall);

				PrintArgs(_n.Args[0].Args, Ambiguity.AllowUnassignedVarDecl, false, ',');
				if (!IsSimpleSymbolWPA(_n.Args[1], S.Missing)) {
					WriteThenSpace(';', SpaceOpt.AfterComma);
					PrintExpr(_n.Args[1], StartExpr, 0);
				} else
					_out.Write(';');
				if (_n.Args[2].ArgCount > 0) {
					WriteThenSpace(';', SpaceOpt.AfterComma);
					PrintArgs(_n.Args[2].Args, 0, false, ',');
				} else
					_out.Write(';');

				WriteCloseParen(ParenFor.KeywordCall);
				PrintBracedBlockOrStmt(_n.Args[3]);
			}
			else if (type == S.ForEach)
			{
				_out.Write("foreach");
				WriteOpenParen(ParenFor.KeywordCall);
				PrintExpr(_n.Args[0], EP.Equals.LeftContext(StartStmt), Ambiguity.AllowUnassignedVarDecl | Ambiguity.ForEachInitializer);
				_out.Space();
				_out.Write("in");
				_out.Space();
				PrintExpr(_n.Args[1], ContinueExpr);
				WriteCloseParen(ParenFor.KeywordCall);

				PrintBracedBlockOrStmt(_n.Args[2]);
			}
			else if (type == S.Try)
			{
				_out.Write("try");
				bool braces = PrintBracedBlockOrStmt(_n.Args[0], NewlineOpt.BeforeSimpleStmtBrace);
				for (int i = 1, c = _n.ArgCount; i < c; i++)
				{
					if (!Newline(braces ? NewlineOpt.BeforeExecutableBrace : NewlineOpt.Default))
						Space(SpaceOpt.Default);
					var clause = _n.Args[i];
					LNode first = clause.Args[0], second = clause.Args[1, null];
					
					WriteOperatorName(clause.Name);
					if (clause.Name == S.Finally)
						braces = PrintBracedBlockOrStmt(clause.Args[0]);
					else { // catch
						var eVar = clause.Args[0];
						if (!eVar.IsIdNamed(S.Missing))
							PrintWithinParens(ParenFor.KeywordCall, eVar, Ambiguity.AllowUnassignedVarDecl);
						var when = clause.Args[1];
						if (!when.IsIdNamed(S.Missing)) {
							Space(SpaceOpt.Default);
							_out.Write("when");
							PrintWithinParens(ParenFor.KeywordCall, when);
						}
						braces = PrintBracedBlockOrStmt(clause.Args[2]);
					}
				}
			}
			else if (type == S.Checked) // includes S.Unchecked
			{
				WriteOperatorName(_name);
				PrintBracedBlockOrStmt(_n.Args[0], NewlineOpt.BeforeSimpleStmtBrace);
			}

			return SPResult.NeedSuffixTrivia;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintLabelStmt()
		{
			if (!EcsValidators.IsLabelStmt(_n, Pedantics))
				return SPResult.Fail;

			_out.Indent(PrinterIndentHint.Label);

			G.Verify(0 == PrintAttrs(AttrStyle.AllowWordAttrs));

			if (_name == S.Label) {
				if (_n.Args[0].Name == S.Default)
					_out.Write("default");
				else
					PrintExpr(_n.Args[0], StartStmt);
			} else if (_name == S.Case) {
				_out.Write("case");
				_out.Space();
				bool first = true;
				foreach (var arg in _n.Args) 
				{
					if (first) first = false;
					else WriteThenSpace(',', SpaceOpt.AfterComma);
					PrintExpr(arg, StartExpr, Ambiguity.AllowUnassignedVarDecl);
				}
			}
			_out.Write(':');

			_out.Dedent(PrinterIndentHint.Label);
			return SPResult.NeedSuffixTrivia;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintBlockOfStmts()
		{
			if (!_n.Calls(S.Braces))
				return SPResult.Fail;

			PrintBracedBlock(_n, 0, mode: BraceMode.BlockStmt);
			return SPResult.NeedSuffixTrivia;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintAssemblyAttribute()
		{
			Debug.Assert(_n.Calls(S.Assembly));
			PrintAttrs(AttrStyle.NoKeywordAttrs);
			_out.Write("[assembly:");
			Space(SpaceOpt.Default);
			PrintArgs(_n, false);
			_out.Write(']');
			return SPResult.NeedSuffixTrivia;
		}
	}
}
