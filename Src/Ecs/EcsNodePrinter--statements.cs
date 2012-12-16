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
using S = Loyc.CompilerCore.CodeSymbols;

namespace ecs
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
		// | Simple stmt         | goto label;            | Check SimpleStmts list    |
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
			S.Var, S.Def, S.Delegate, S.Event, S.Property
		});
		// Simple statements have the syntax "keyword;" or "keyword expr;"
		static readonly HashSet<Symbol> SimpleStmts = new HashSet<Symbol>(new[] {
			S.Break, S.Continue, S.Goto, S.GotoCase, S.Return, S.Throw,
		});
		// Block statements take block(s) as arguments
		static readonly HashSet<Symbol> BlockStmts = new HashSet<Symbol>(new[] {
			S.If, S.Checked, S.DoWhile, S.Fixed, S.For, S.ForEach, S.If, S.Lock, 
			S.Switch, S.Try, S.Unchecked, S.UsingStmt, S.While
		});
		static readonly HashSet<Symbol> LabelStmts = new HashSet<Symbol>(new[] {
			S.Label, S.Case
		});
		static readonly HashSet<Symbol> BlocksOfStmts = new HashSet<Symbol>(new[] {
			S.List, S.Braces
		});

		//static readonly HashSet<Symbol> StmtsWithWordAttrs = AllNonExprStmts;

		public enum SPResult { Fail, Complete, NeedSemicolon };
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
			AddAll(d, BlockStmts, "AutoPrintBlockStmt");
			AddAll(d, LabelStmts, "AutoPrintLabelStmt");
			AddAll(d, BlocksOfStmts, "AutoPrintBlockOfStmts");
			d[S.Result] = OpenDelegate<StatementPrinter>("AutoPrintResult");
			return d;
		}
		static void AddAll(Dictionary<Symbol,StatementPrinter> d, HashSet<Symbol> names, string handlerName)
		{
			var method = OpenDelegate<StatementPrinter>(handlerName);
 			foreach(var name in names)
				d.Add(name, method);
		}
		
		#endregion

		void PrintStmt(INodeReader n, Ambiguity flags = 0)
		{
			using (With(n))
				PrintStmt(flags);
		}

		public void PrintStmt(Ambiguity flags = 0)
		{
			_out.BeginStatement();

			var style = _n.BaseStyle;
			if (style != NodeStyle.Expression && style != NodeStyle.PrefixNotation && style != NodeStyle.PurePrefixNotation)
			{
				StatementPrinter printer;
				var name = _n.Name;
				if (_n.IsKeyword && HasSimpleHeadWPA(_n) && StatementPrinters.TryGetValue(name, out printer)) {
					var result = printer(this, flags);
					if (result != SPResult.Fail) {
						if (result == SPResult.NeedSemicolon)
							_out.Write(';', true);
						return;
					}
				}
			}
			PrintExpr(StartStmt);
			_out.Write(';', true);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintResult(Ambiguity flags)
		{
			if (!IsResultExpr(_n) || (flags & Ambiguity.FinalStmt) == 0)
				return SPResult.Fail;
			PrintExpr(_n.TryGetArg(0), StartExpr); // not StartStmt => allows multiplication e.g. a*b by avoiding ptr ambiguity
			return SPResult.Complete;
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
			PrintAttrs(StartStmt, AttrStyle.IsDefinition, ifClause);

			INodeReader name = _n.TryGetArg(0), bases = _n.TryGetArg(1), body = _n.TryGetArg(2);
			PrintOperatorName(_n.Name);
			_out.Space();
			PrintExpr(name, ContinueExpr, Ambiguity.InDefinitionName);
			if (bases.CallsMin(S.List, 1))
			{
				Space(SpaceOpt.BeforeBaseListColon);
				WriteThenSpace(':', SpaceOpt.AfterColon);
				for (int i = 0, c = bases.ArgCount; i < c; i++) {
					if (i != 0)
						WriteThenSpace(',', SpaceOpt.AfterComma);
					PrintType(bases.TryGetArg(i), ContinueExpr);
				}
			}
			bool alias = name.Calls(S.Set, 2);
			var name2 = name;
			if (name2.Calls(S.Of) || (alias && (name2 = name.Args[0]).Calls(S.Of)))
				PrintWhereClauses(name2);

			AutoPrintIfClause(ifClause);
			
			if (body == null)
				return SPResult.NeedSemicolon;

			if (_n.Name == S.Enum)
				PrintEnumBody(body);
			else
				PrintBracedBlock(body, NewlineOpt.BeforeSpaceDefBrace);
			return SPResult.Complete;
		}

		void AutoPrintIfClause(INodeReader ifClause)
		{
			if (ifClause != null) {
				if (!Newline(NewlineOpt.BeforeIfClause))
					Space(SpaceOpt.Default);
				_out.Write("if", true);
				Space(SpaceOpt.BeforeKeywordStmtArgs);
				PrintExpr(ifClause.TryGetArg(0), StartExpr, Ambiguity.NoBracedBlock);
			}
		}

		private INodeReader GetIfClause()
		{
			var ifClause = _n.TryGetAttr(S.If);
			if (ifClause != null && !HasPAttrs(ifClause) && HasSimpleHeadWPA(ifClause) && ifClause.ArgCount == 1)
				return ifClause;
			return null;
		}

		private void PrintWhereClauses(INodeReader name)
		{
			// Look for "where" clauses and print them
			bool first = true;
			for (int i = 1, c = name.ArgCount; i < c; i++)
			{
				var param = name.TryGetArg(i);
				for (int a = 0, ac = param.AttrCount; a < ac; a++)
				{
					var where = param.TryGetAttr(a);
					if (where.CallsMin(S.Where, 1))
					{
						using (Indented)
						{
							if (!Newline(first ? NewlineOpt.BeforeWhereClauses : NewlineOpt.BeforeEachWhereClause))
								_out.Space();
							first = false;
							_out.Write("where ", true);
							var paramName = param.Name.Name;
							_out.Write(param.IsKeyword ? paramName.Substring(1) : paramName, true);
							Space(SpaceOpt.BeforeWhereClauseColon);
							WriteThenSpace(':', SpaceOpt.AfterColon);
							foreach (var constraint in where.Args)
							{
								if (constraint.IsSimpleSymbol && (constraint.Name == S.Class || constraint.Name == S.Struct))
									PrintOperatorName(constraint.Name);
								else
									PrintExpr(constraint, StartExpr);
							}
						}
					}
				}
			}
		}

		private void PrintEnumBody(INodeReader body)
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
					PrintExpr(body.TryGetArg(i), StartExpr);
				}
			}
			_out.Newline();
			_out.Write('}', true);
		}

		private void PrintBracedBlock(INodeReader body, NewlineOpt before, NewlineOpt after = (NewlineOpt)(-1))
		{
			if (before != 0)
				if (!Newline(before))
					Space(SpaceOpt.Default);
			if (body.Name == S.List)
				_out.Write('#', false);
			_out.Write('{', true);
			using (Indented)
				for (int i = 0, c = body.ArgCount; i < c; i++)
					PrintStmt(body.TryGetArg(i), i + 1 == c ? Ambiguity.FinalStmt : 0);
			Newline(after);
			_out.Write('}', true);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintMethodDefinition(Ambiguity flags)
		{
			// S.Def, S.Delegate
			return SPResult.Fail;
		}
		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintVarDecl(Ambiguity flags)
		{
			if (!IsVariableDecl(true, true))
				return SPResult.Fail;
			// S.Var
			return SPResult.Fail;
		}
		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintProperty(Ambiguity flags)
		{
			// S.Property
			return SPResult.Fail;
		}
		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintEvent(Ambiguity flags)
		{
			// S.Event
			return SPResult.Fail;
		}
		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintSimpleStmt(Ambiguity flags)
		{
			// S.Break, S.Continue, S.Goto, S.GotoCase, S.Return, S.Throw
			if (!IsSimpleStmt(_n))
				return SPResult.Fail;
			return SPResult.Fail;
		}
		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintBlockStmt(Ambiguity flags)
		{
			// S.If, S.Checked, S.DoWhile, S.Fixed, S.For, S.ForEach, S.If, S.Lock, 
			// S.Switch, S.Try, S.Unchecked, S.Using, S.While
			if (!IsBlockStmt(_n))
				return SPResult.Fail;
			return SPResult.Fail;
		}
		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintLabelStmt(Ambiguity flags)
		{
			if (!IsLabelStmt())
				return SPResult.Fail;

			if (_n.Name == S.Label) {
				PrintExpr(_n.Args[0], StartStmt);
			} else if (_n.Name == S.Case) {
				_out.Write("case", true);
				_out.Space();
				for (int i = 0, c = _n.ArgCount; i < c; i++)
				{
					WriteThenSpace(',', SpaceOpt.AfterComma);
					PrintExpr(_n.TryGetArg(i), StartStmt);
				}
			}
			_out.Write(':', true);
			return SPResult.Complete;
		}
		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintBlockOfStmts(Ambiguity flags)
		{
			if (!IsBlockOfStmts(_n))
				return SPResult.Fail;

			PrintAttrs(StartStmt, AttrStyle.AllowKeywordAttrs);
			PrintBracedBlock(_n, 0);
			return SPResult.Complete;
		}
	}
}
