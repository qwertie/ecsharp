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
using S = Loyc.Syntax.CodeSymbols;
using EP = Ecs.EcsPrecedence;
using Loyc.Syntax.Lexing;

namespace Ecs
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
		// | Block or list       | { ... } or #{ ... }    | Name in (S.List,S.Braces) |
		// | Expression stmt     | x += y;                | When none of the above    |

		// Space definitions are containers for other definitions
		static readonly HashSet<Symbol> SpaceDefinitionStmts = new HashSet<Symbol>(new[] {
			S.Struct, S.Class, S.Trait, S.Enum, S.Alias, S.Interface, S.Namespace
		});
		// Definition statements define types, spaces, methods, properties, events and variables
		static readonly HashSet<Symbol> OtherDefinitionStmts = new HashSet<Symbol>(new[] {
			S.Var, S.Fn, S.Cons, S.Delegate, S.Event, S.Property
		});
		// Simple statements have the syntax "keyword;" or "keyword expr;"
		static readonly HashSet<Symbol> SimpleStmts = new HashSet<Symbol>(new[] {
			S.Break, S.Continue, S.Goto, S.GotoCase, S.Return, S.Throw, S.Import
		});
		// Block statements take block(s) as arguments
		static readonly HashSet<Symbol> TwoArgBlockStmts = new HashSet<Symbol>(new[] {
			S.DoWhile, S.Fixed, S.Lock, S.Switch, S.UsingStmt, S.While
		});
		static readonly HashSet<Symbol> OtherBlockStmts = new HashSet<Symbol>(new[] {
			S.If, S.Checked, S.For, S.ForEach, S.If, S.Try, S.Unchecked
		});
		static readonly HashSet<Symbol> LabelStmts = new HashSet<Symbol>(new[] {
			S.Label, S.Case
		});

		//static readonly HashSet<Symbol> StmtsWithWordAttrs = AllNonExprStmts;

		public enum SPResult { Fail, Complete, NeedSemicolon, NeedSuffixTrivia };
		delegate SPResult StatementPrinter(EcsNodePrinter @this, Ambiguity flags);
		static Dictionary<Symbol, StatementPrinter> StatementPrinters = StatementPrinters_();
		static Dictionary<Symbol, StatementPrinter> StatementPrinters_()
		{
			// Build a dictionary of printers for each operator name.
			var d = new Dictionary<Symbol, StatementPrinter>();
			AddAll(d, SpaceDefinitionStmts, "AutoPrintSpaceDefinition");
			AddAll(d, OtherDefinitionStmts, "AutoPrintMethodDefinition");
			d[S.Var]      = OpenDelegate<StatementPrinter>("AutoPrintVarDecl");
			d[S.Event]    = OpenDelegate<StatementPrinter>("AutoPrintEvent");
			d[S.Property] = OpenDelegate<StatementPrinter>("AutoPrintProperty");
			AddAll(d, SimpleStmts, "AutoPrintSimpleStmt");
			AddAll(d, TwoArgBlockStmts, "AutoPrintTwoArgBlockStmt");
			AddAll(d, OtherBlockStmts, "AutoPrintOtherBlockStmt");
			AddAll(d, LabelStmts, "AutoPrintLabelStmt");
			d[S.Braces] = OpenDelegate<StatementPrinter>("AutoPrintBlockOfStmts");
			d[S.Result] = OpenDelegate<StatementPrinter>("AutoPrintResult");
			d[S.Missing] = OpenDelegate<StatementPrinter>("AutoPrintMissingStmt");
			d[S.RawText] = OpenDelegate<StatementPrinter>("AutoPrintRawText");
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
			using (With(n))
				PrintStmt(flags);
		}

		public void PrintStmt(Ambiguity flags = 0)
		{
			if ((flags & Ambiguity.ElseClause) == 0)
				_out.BeginStatement();

			var style = _n.BaseStyle;
			if (style != NodeStyle.Expression && style != NodeStyle.PrefixNotation && 
				style != NodeStyle.PrefixNotation && (AllowChangeParenthesis || !_n.IsParenthesizedExpr()))
			{
				StatementPrinter printer;
				var name = _n.Name;
				if (StatementPrinters.TryGetValue(name, out printer) && HasSimpleHeadWPA(_n))
				{
					var result = printer(this, flags | Ambiguity.NoParenthesis);
					if (result != SPResult.Fail) {
						if (result != SPResult.Complete)
							PrintSuffixTrivia(result == SPResult.NeedSemicolon);
						return;
					}
				}

				if (_n.BaseStyle == NodeStyle.Special && AutoPrintMacroCall(flags | Ambiguity.NoParenthesis))
					return;

				var attrs = _n.Attrs;
				for (int i = 0, c = attrs.Count; i < c; i++)
				{
					var a = attrs[i];
					if ((a.Name == S.TriviaMacroAttribute && AutoPrintMacroAttribute()) ||
						(a.Name == S.TriviaForwardedProperty && AutoPrintForwardedProperty()))
						return;
				}
			}

			PrintExpr(StartStmt);
			PrintSuffixTrivia(true);
		}

		private bool AutoPrintMacroAttribute()
		{
			var argCount = _n.ArgCount;
			if (argCount < 1)
				return false;

			// TODO
			// _out.Write("[[", true);
			// _out.Write("]]", true);
			return false;
		}
		private bool AutoPrintMacroCall(Ambiguity flags)
		{
			var argCount = _n.ArgCount;
			if (!_n.HasSimpleHead() && !IsComplexIdentifier(_n.Target))
				return false;

			if (argCount == 1)
			{
				var body = _n.Args[0];
				if (!CallsWPAIH(body, S.Braces))
					return false;

				G.Verify(0 == PrintAttrs(StartStmt, AttrStyle.AllowKeywordAttrs, flags));

				if (_n.Name == GSymbol.Empty) {
					PrintExpr(_n.Target, EP.Primary.LeftContext(StartExpr));
					PrintBracedBlock(body, NewlineOpt.BeforeExecutableBrace);
				} else {
					PrintSimpleIdent(_n.Name, 0);
					PrintBracedBlock(body, _n.Name.Name.Length > 7 ? NewlineOpt.BeforeExecutableBrace : NewlineOpt.BeforeSimpleStmtBrace);
				}
				return true;
			}
			else if (argCount > 1)
			{
				if (AvoidMacroSyntax)
					return false;

				var body = _n.Args[argCount - 1];
				// If the body calls anything other than S.Braces, don't use macro-call notation.
				if (!CallsWPAIH(body, S.Braces))
					return false;

				G.Verify(0 == PrintAttrs(StartStmt, AttrStyle.AllowKeywordAttrs, flags));

				if (_n.Name == GSymbol.Empty)
					PrintExpr(_n.Target, EP.Primary.LeftContext(StartExpr));
				else
					PrintSimpleIdent(_n.Name, 0);

				PrintArgList(_n, ParenFor.MacroCall, argCount - 1, 0, OmitMissingArguments);

				PrintBracedBlockOrStmt(body, flags, NewlineOpt.BeforeExecutableBrace);
				return true;
			}
			return false;
		}
		private bool AutoPrintForwardedProperty()
		{
			if (!IsForwardedProperty())
				return false;

			G.Verify(0 == PrintAttrs(StartStmt, AttrStyle.AllowKeywordAttrs, Ambiguity.NoParenthesis));
			PrintSimpleIdent(_n.Name, 0);
			Space(SpaceOpt.BeforeForwardArrow);
			_out.Write("==>", true);
			PrefixSpace(EP.Forward);
			PrintExpr(_n.Args[0].Args[0], EP.Forward.RightContext(StartExpr));
			_out.Write(";", true);
			return true;
		}


		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintResult(Ambiguity flags)
		{
			if (!IsResultExpr(_n) || (flags & Ambiguity.FinalStmt) == 0)
				return SPResult.Fail;
			PrintExpr(_n.Args[0], StartExpr); // not StartStmt => allows multiplication e.g. a*b by avoiding ptr ambiguity
			return SPResult.NeedSuffixTrivia;
		}


		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintMissingStmt(Ambiguity flags)
		{
			Debug.Assert(_n.Name == S.Missing);
			if (!_n.IsId)
				return SPResult.Fail;
			G.Verify(0 == PrintAttrs(StartStmt, AttrStyle.AllowKeywordAttrs, flags));
			return SPResult.NeedSemicolon;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintRawText(Ambiguity flags)
		{
			if (OmitRawText)
				return SPResult.Fail;
			G.Verify(0 == PrintAttrs(StartStmt, AttrStyle.NoKeywordAttrs, flags));
			_out.Write(GetRawText(_n), true);
			return SPResult.NeedSuffixTrivia;
		}

		// These methods are public but hidden because they are found by reflection 
		// and they should be compatible with a partial-trust environment.
		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintSpaceDefinition(Ambiguity flags)
		{
			// Spaces: S.Struct, S.Class, S.Trait, S.Enum, S.Alias, S.Interface, S.Namespace
			if (!IsSpaceStatement())
				return SPResult.Fail;

			var ifClause = GetIfClause();

			int ai;
			var old_n = _n;
			if (_n.Name == S.Alias && (ai = _n.Attrs.IndexWhere(a => a.IsIdNamed(S.FilePrivate))) > -1) {
				// Cause "[#filePrivate] #alias x = y;" to print as "using x = y;"
				_n = _n.WithAttrs(_n.Attrs.RemoveAt(ai)).WithTarget(S.UsingStmt);
			}

			G.Verify(0 == PrintAttrs(StartStmt, AttrStyle.IsDefinition, flags, ifClause));

			LNode name = _n.Args[0], bases = _n.Args[1], body = _n.Args[2, null];
			WriteOperatorName(_n.Name);
			
			_n = old_n;
			
			_out.Space();
			PrintExpr(name, ContinueExpr, Ambiguity.InDefinitionName);

			if (bases.CallsMin(S.List, 1))
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

			AutoPrintIfClause(ifClause);
			
			if (body == null)
				return SPResult.NeedSemicolon;

			if (_n.Name == S.Enum)
				PrintEnumBody(body);
			else
				PrintBracedBlock(body, NewlineOpt.BeforeSpaceDefBrace, false, KeyNameComponentOf(name));
			return SPResult.NeedSuffixTrivia;
		}

		/// <summary>Given a complex name such as <c>global::Foo&lt;int>.Bar&lt;T></c>,
		/// this method identifies the base name component, which in this example 
		/// is Bar. This is used, for example, to identify the expected name for
		/// a constructor based on the class name, e.g. <c>Foo&lt;T></c> => Foo.</summary>
		/// <remarks>It is not verified that name is a complex identifier. There
		/// is no error detection but in some cases an empty name may be returned, 
		/// e.g. for input like <c>Foo."Hello"</c>.</remarks>
		public static Symbol KeyNameComponentOf(LNode name)
		{
			// global::Foo<int>.Bar<T> is structured (((global::Foo)<int>).Bar)<T>
			// So if #of, get first arg (which cannot itself be #of), then if #dot, 
			// get second arg.
			if (name.CallsMin(S.Of, 1))
				name = name.Args[0];
			if (name.CallsMin(S.Dot, 1))
				name = name.Args.Last;
			return name.Name;
		}

		void AutoPrintIfClause(LNode ifClause)
		{
			if (ifClause != null) {
				if (!Newline(NewlineOpt.BeforeIfClause))
					Space(SpaceOpt.Default);
				_out.Write("if", true);
				Space(SpaceOpt.BeforeKeywordStmtArgs);
				PrintExpr(ifClause.Args[0], StartExpr, Ambiguity.NoBracedBlock);
			}
		}

		private LNode GetIfClause()
		{
			var ifClause = _n.AttrNamed(S.If);
			if (ifClause != null && !HasPAttrs(ifClause) && HasSimpleHeadWPA(ifClause) && ifClause.ArgCount == 1)
				return ifClause;
			return null;
		}

		private void PrintWhereClauses(LNode name)
		{
			// Example: #of(Foo, [#where(#class, IEnumerable)] T)
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
					if (where.CallsMin(S.Where, 1))
					{
						using (Indented)
						{
							if (!Newline(first ? NewlineOpt.BeforeWhereClauses : NewlineOpt.BeforeEachWhereClause))
								_out.Space();
							first = false;
							_out.Write("where", true);
							PrintSimpleIdent(param.Name, 0);
							Space(SpaceOpt.BeforeWhereClauseColon);
							WriteThenSpace(':', SpaceOpt.AfterColon);
							bool firstC = true;
							foreach (var constraint in where.Args)
							{
								if (firstC)
									firstC = false;
								else
									WriteThenSpace(',', SpaceOpt.AfterComma);
								if (constraint.Name == S.New && constraint.ArgCount == 0)
									_out.Write("new()", true);
								else if (constraint.IsId && (constraint.Name == S.Class || constraint.Name == S.Struct))
									WriteOperatorName(constraint.Name);
								else
									PrintExpr(constraint, StartExpr);
							}
						}
					}
				}
			}
		}

		private void PrintEnumBody(LNode body)
		{
			if (!Newline(NewlineOpt.BeforeSpaceDefBrace))
				Space(SpaceOpt.Default);
			_out.Write('{', true);
			using (Indented)
			{
				_out.Newline();
				for (int i = 0, c = body.ArgCount; i < c; i++)
				{
					if (i != 0) {
						_out.Write(',', true);
						if (!Newline(NewlineOpt.AfterEachEnumValue))
							Space(SpaceOpt.AfterComma);
					}
					PrintExpr(body.Args[i], StartExpr);
				}
			}
			_out.Newline();
			_out.Write('}', true);
		}

		private bool PrintBracedBlockOrStmt(LNode stmt, Ambiguity flags, NewlineOpt beforeBrace = NewlineOpt.BeforeExecutableBrace)
		{
			var name = stmt.Name;
			if (name == S.Braces && !HasPAttrs(stmt) && HasSimpleHeadWPA(stmt))
			{
				PrintBracedBlock(stmt, beforeBrace);
				return true;
			}
			// Detect "else if (...)", and suppress newline/indent between "else" and "if".
			if (name == S.If && (flags & Ambiguity.ElseClause) != 0)
			{
				using (With(stmt))
					if (OtherBlockStmtType() == S.If)
					{
						PrintStmt(flags & (Ambiguity.FinalStmt | Ambiguity.ElseClause));
						return false;
					}
			}
			using (Indented)
			{
				Newline(NewlineOpt.BeforeSingleSubstmt);
				PrintStmt(stmt, flags & (Ambiguity.FinalStmt | Ambiguity.NoIfWithoutElse));
				return false;
			}
		}

		private void PrintBracedBlock(LNode body, NewlineOpt beforeBrace, bool skipFirstStmt = false, Symbol spaceName = null)
		{
			if (beforeBrace != 0)
				if (!Newline(beforeBrace))
					Space(SpaceOpt.Default);
			_out.Write('{', true);
			using (WithSpace(spaceName))
				using (Indented)
					for (int i = (skipFirstStmt?1:0), c = body.ArgCount; i < c; i++)
						PrintStmt(body.Args[i], i + 1 == c ? Ambiguity.FinalStmt : 0);
			Newline(NewlineOpt.Default);
			_out.Write('}', true);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintMethodDefinition(Ambiguity flags)
		{
			// S.Fn, S.Delegate: #fn(#int32, Square, #(int x), { return x * x; });
			if (!IsMethodDefinition(true))
				return SPResult.Fail;

			LNode retType = _n.Args[0], name = _n.Args[1];
			LNode args = _n.Args[2];
			LNode body = _n.Args[3, null];
			bool isConstructor = _n.Name == S.Cons;
			bool isDestructor = !isConstructor && name.Calls(S._Destruct, 1);
			
			LNode firstStmt = null;
			if (isConstructor && body != null && body.CallsMin(S.Braces, 1)) {
				// Detect ": this(...)" or ": base(...)"
				firstStmt = body.Args[0];
				if (!CallsWPAIH(firstStmt, S.This) &&
					!CallsWPAIH(firstStmt, S.Base))
					firstStmt = null;
			}

			if (!AllowConstructorAmbiguity) {
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

			// A cast operator with the structure: #fn(Foo, operator`#cast`, #(...))
			// can be printed in a special format: operator Foo(...);
			bool isCastOperator = (name.Name == S.Cast && name.AttrNamed(S.TriviaUseOperatorKeyword) != null);

			var ifClause = PrintTypeAndName(isConstructor || isDestructor, isCastOperator, 
				isConstructor && !name.IsIdNamed(S.This) ? AttrStyle.IsConstructor : AttrStyle.IsDefinition);

			PrintArgList(args, ParenFor.MethodDecl, args.ArgCount, Ambiguity.AllowUnassignedVarDecl, OmitMissingArguments);
	
			PrintWhereClauses(name);
			
			// If this is a constructor where the first statement is this(...) or 
			// base(...), we must change the notation to ": this(...) {...}" as
			// required in plain C#
			if (firstStmt != null) {
				using (Indented) {
					if (!Newline(NewlineOpt.BeforeConstructorColon))
						Space(SpaceOpt.BeforeConstructorColon);
					WriteThenSpace(':', SpaceOpt.AfterColon);
					PrintExpr(firstStmt, StartExpr, Ambiguity.NoBracedBlock);
				}
			}

			return AutoPrintBodyOfMethodOrProperty(body, ifClause, firstStmt != null);
		}

		// e.g. given the method void f() {...}, prints "void f"
		//      for a cast operator #fn(Foo, #cast, #(...)) it prints "operator Foo" if requested
		private LNode PrintTypeAndName(bool isConstructor, bool isCastOperator = false, AttrStyle attrStyle = AttrStyle.IsDefinition, string eventKeywordOpt = null)
		{
			LNode retType = _n.Args[0], name = _n.Args[1];
			var ifClause = GetIfClause();

			if (retType.HasPAttrs())
				using (With(retType))
					G.Verify(0 == PrintAttrs(StartStmt, AttrStyle.NoKeywordAttrs, 0, null, "return"));

			G.Verify(0 == PrintAttrs(StartStmt, attrStyle, 0, ifClause));

			if (eventKeywordOpt != null)
				_out.Write(eventKeywordOpt, true);

			if (_n.Name == S.Delegate)
			{
				_out.Write("delegate", true);
				_out.Space();
			}
			if (isCastOperator)
			{
				_out.Write("operator", true);
				_out.Space();
				PrintType(retType, ContinueExpr, Ambiguity.AllowPointer | Ambiguity.DropAttributes);
			}
			else
			{
				if (!isConstructor) {
					PrintType(retType, ContinueExpr, Ambiguity.AllowPointer | Ambiguity.DropAttributes);
					_out.Space();
				}
				if (isConstructor && name.IsIdNamed(S.This))
					_out.Write("this", true);
				else
					PrintExpr(name, ContinueExpr, Ambiguity.InDefinitionName);
			}
			return ifClause;
		}
		private void PrintArgList(LNode args, ParenFor kind, int argCount, Ambiguity flags, bool omitMissingArguments, char separator = ',')
		{
			WriteOpenParen(kind);
			PrintArgs(args, argCount, flags, omitMissingArguments, separator);
			WriteCloseParen(kind);
		}
		private void PrintArgs(LNode args, int argCount, Ambiguity flags, bool omitMissingArguments, char separator = ',')
		{
			for (int i = 0; i < argCount; i++)
			{
				var arg = args.Args[i];
				bool missing = omitMissingArguments && IsSimpleSymbolWPA(arg, S.Missing) && argCount > 1;
				if (i != 0)
					WriteThenSpace(separator, missing ? SpaceOpt.MissingAfterComma : SpaceOpt.AfterComma);
				if (!missing)
					PrintExpr(arg, StartExpr, flags);
			}
		}
		private SPResult AutoPrintBodyOfMethodOrProperty(LNode body, LNode ifClause, bool skipFirstStmt = false)
		{
			using (WithSpace(S.Fn)) {
				AutoPrintIfClause(ifClause);

				if (body == null)
					return SPResult.NeedSemicolon;
				if (body.Name == S.Forward)
				{
					Space(SpaceOpt.BeforeForwardArrow);
					_out.Write("==>", true);
					PrefixSpace(EP.Forward);
					PrintExpr(body.Args[0], EP.Forward.RightContext(StartExpr));
					return SPResult.NeedSemicolon;
				}
				else if (body.Name == S.Braces && body.BaseStyle != NodeStyle.PrefixNotation)
				{
					PrintBracedBlock(body, NewlineOpt.BeforeMethodBrace, skipFirstStmt, S.Fn);
					return SPResult.NeedSuffixTrivia;
				}
				else
				{
					PrefixSpace(EP.Lambda);
					_out.Write("=>", true);
					PrefixSpace(EP.Lambda);
					PrintExpr(body, EP.Lambda.RightContext(StartExpr));
					return SPResult.NeedSemicolon;
				}
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintProperty(Ambiguity flags)
		{
			// S.Property: 
			// #property(int, Foo, { 
			//     get({ return _foo; });
			//     set({ _foo = value; });
			// });
			if (!IsPropertyDefinition())
				return SPResult.Fail;

			var ifClause = PrintTypeAndName(false);

			PrintWhereClauses(_n.Args[1]);

			return AutoPrintBodyOfMethodOrProperty(_n.Args[2, null], ifClause);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintVarDecl(Ambiguity flags)
		{
			if (!IsVariableDecl(true, true))
				return SPResult.Fail;

			var ifClause = GetIfClause();
			G.Verify(0 == PrintAttrs(StartStmt, AttrStyle.IsDefinition, flags, ifClause));
			PrintVariableDecl(false, StartStmt, flags);
			AutoPrintIfClause(ifClause);
			return SPResult.NeedSemicolon;
		}
		
		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintEvent(Ambiguity flags)
		{
			var eventType = EventDefinitionType();
			if (eventType == EventDef.Invalid)
				return SPResult.Fail;

			var ifClause = PrintTypeAndName(false, false, AttrStyle.IsDefinition, "event ");
			if (eventType == EventDef.WithBody)
				return AutoPrintBodyOfMethodOrProperty(_n.Args[2, null], ifClause);
			else {
				for (int i = 2, c = _n.ArgCount; i < c; i++)
				{
					WriteThenSpace(',', SpaceOpt.AfterComma);
					PrintExpr(_n.Args[i], ContinueExpr);
				}
				return SPResult.NeedSemicolon;
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintSimpleStmt(Ambiguity flags)
		{
			// S.Break, S.Continue, S.Goto, S.GotoCase, S.Return, S.Throw
			if (!IsSimpleKeywordStmt())
				return SPResult.Fail;

			G.Verify(0 == PrintAttrs(StartStmt, AttrStyle.AllowWordAttrs, flags));

			var name = _n.Name;
			if (name == S.GotoCase)
				_out.Write("goto case", true);
			else if (name == S.Import)
				_out.Write("using", true);
			else
				WriteOperatorName(name);

			int i = 0;
			foreach (var arg in _n.Args)
			{
				if (i++ == 0) Space(SpaceOpt.Default);
				else WriteThenSpace(',', SpaceOpt.AfterComma);
				PrintExpr(arg, StartExpr);
			}
			return SPResult.NeedSemicolon;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintTwoArgBlockStmt(Ambiguity flags)
		{
			// S.Do, S.Fixed, S.Lock, S.Switch, S.UsingStmt, S.While
			var type = TwoArgBlockStmtType();
			if (type == null)
				return SPResult.Fail;

			G.Verify(0 == PrintAttrs(StartStmt, AttrStyle.AllowWordAttrs, flags));

			if (type == S.DoWhile)
			{
				_out.Write("do", true);
				bool braces = PrintBracedBlockOrStmt(_n.Args[0], flags, NewlineOpt.BeforeSimpleStmtBrace);
				if (!Newline(braces ? NewlineOpt.BeforeExecutableBrace : NewlineOpt.Default))
					Space(SpaceOpt.Default);
				_out.Write("while", true);
				PrintWithinParens(ParenFor.KeywordCall, _n.Args[1]);
				return SPResult.NeedSemicolon;
			}
			else
			{
				WriteOperatorName(_n.Name);
				Ambiguity argFlags = 0;
				if (_n.Name == S.Fixed)
					argFlags |= Ambiguity.AllowPointer;
				PrintWithinParens(ParenFor.KeywordCall, _n.Args[0], argFlags);
				PrintBracedBlockOrStmt(_n.Args[1], flags);
				return SPResult.NeedSuffixTrivia;
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintOtherBlockStmt(Ambiguity flags)
		{
			// S.If, S.For, S.ForEach, S.Checked, S.Unchecked, S.Try
			var type = OtherBlockStmtType();
			if (type == null)
				return SPResult.Fail;

			if (type == S.If)
			{
				var @else = _n.Args[2, null];
				bool needCloseBrace = false;
				if (@else == null && (flags & Ambiguity.NoIfWithoutElse) != 0) {
					if (AllowExtraBraceForIfElseAmbig) {
						_out.Write('{', true);
						needCloseBrace = true;
					} else
						return SPResult.Fail;
				}

				// Note: the "if" statement in particular cannot have "word" attributes
				//       because they would create ambiguity with property declarations
				G.Verify(0 == PrintAttrs(StartStmt, AttrStyle.AllowKeywordAttrs, flags));

				_out.Write("if", true);
				PrintWithinParens(ParenFor.KeywordCall, _n.Args[0]);

				var thenFlags = flags & ~(Ambiguity.ElseClause);
				if (@else != null) thenFlags |= Ambiguity.NoIfWithoutElse;
				bool braces = PrintBracedBlockOrStmt(_n.Args[1], thenFlags);
				
				if (@else != null) {
					if (!Newline(braces ? NewlineOpt.BeforeExecutableBrace : NewlineOpt.Default))
						Space(SpaceOpt.Default);
					_out.Write("else", true);
					PrintBracedBlockOrStmt(@else, flags | Ambiguity.ElseClause);
				}

				if (needCloseBrace)
					_out.Write('}', true);
				return SPResult.NeedSuffixTrivia;
			}

			G.Verify(0 == PrintAttrs(StartStmt, AttrStyle.AllowWordAttrs, flags));

			if (type == S.For)
			{
				_out.Write("for", true);
				PrintArgList(_n, ParenFor.KeywordCall, 3, flags, true, ';');
				PrintBracedBlockOrStmt(_n.Args[3], flags);
			}
			else if (type == S.ForEach)
			{
				_out.Write("foreach", true);
				
				WriteOpenParen(ParenFor.KeywordCall);
				PrintExpr(_n.Args[0], EP.Equals.LeftContext(StartStmt), Ambiguity.AllowUnassignedVarDecl | Ambiguity.ForEachInitializer);
				_out.Space();
				_out.Write("in", true);
				_out.Space();
				PrintExpr(_n.Args[1], ContinueExpr, flags);
				WriteCloseParen(ParenFor.KeywordCall);

				PrintBracedBlockOrStmt(_n.Args[2], flags);
			}
			else if (type == S.Try)
			{
				_out.Write("try", true);
				bool braces = PrintBracedBlockOrStmt(_n.Args[0], flags, NewlineOpt.BeforeSimpleStmtBrace);
				for (int i = 1, c = _n.ArgCount; i < c; i++)
				{
					if (!Newline(braces ? NewlineOpt.BeforeExecutableBrace : NewlineOpt.Default))
						Space(SpaceOpt.Default);
					var clause = _n.Args[i];
					LNode first = clause.Args[0], second = clause.Args[1, null];
					
					WriteOperatorName(clause.Name);
					if (second != null && !IsSimpleSymbolWPA(first, S.Missing))
						PrintWithinParens(ParenFor.KeywordCall, first, Ambiguity.AllowUnassignedVarDecl);
					braces = PrintBracedBlockOrStmt(second ?? first, flags);
				}
			}
			else if (type == S.Checked) // includes S.Unchecked
			{
				WriteOperatorName(_n.Name);
				PrintBracedBlockOrStmt(_n.Args[0], flags, NewlineOpt.BeforeSimpleStmtBrace);
			}

			return SPResult.NeedSuffixTrivia;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintLabelStmt(Ambiguity flags)
		{
			if (!IsLabelStmt())
				return SPResult.Fail;

			_out.BeginLabel();

			G.Verify(0 == PrintAttrs(StartStmt, AttrStyle.AllowWordAttrs, flags));

			if (_n.Name == S.Label) {
				if (_n.Args[0].Name == S.Default)
					_out.Write("default", true);
				else
					PrintExpr(_n.Args[0], StartStmt);
			} else if (_n.Name == S.Case) {
				_out.Write("case", true);
				_out.Space();
				bool first = true;
				foreach (var arg in _n.Args) 
				{
					if (first) first = false;
					else WriteThenSpace(',', SpaceOpt.AfterComma);
					PrintExpr(arg, StartStmt);
				}
			}
			_out.Write(':', true);
			return SPResult.NeedSuffixTrivia;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintBlockOfStmts(Ambiguity flags)
		{
			if (!IsBlockOfStmts(_n))
				return SPResult.Fail;

			G.Verify(0 == PrintAttrs(StartStmt, AttrStyle.AllowKeywordAttrs, flags));
			PrintBracedBlock(_n, 0);
			return SPResult.NeedSuffixTrivia;
		}
	}
}
