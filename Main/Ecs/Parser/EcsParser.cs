using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc;
using Loyc.Utilities;
using Loyc.Syntax;
using Loyc.Collections;
using Loyc.Syntax.Lexing;

/// <summary>Enhanced C# parser</summary>
namespace Ecs.Parser
{
	using TT = TokenType;
	using EP = EcsPrecedence;
	using S = CodeSymbols;
	using Loyc.Collections.Impl;

	/// <summary>Parses Enhanced C# code into a sequence of Loyc trees 
	/// (<see cref="LNode"/>), one per top-level statement.</summary>
	/// <remarks>
	/// You can use <see cref="EcsLanguageService.Value"/> with <see cref="ParsingService.Parse"/>
	/// to easily parse a text string (holding zero or more EC# statements) into a 
	/// Loyc tree. One does not normally use this class directly.
	/// </remarks>
	public partial class EcsParser : BaseParser<Token>
	{
		protected IMessageSink _messages;
		protected LNodeFactory F;
		protected IListSource<Token> _tokensRoot;
		protected IListSource<Token> _tokens;
		// index into source text of the first token at the current depth (inside 
		// parenthesis, etc.). Used if we need to print an error inside empty {} [] ()
		protected int _startTextIndex = 0;

		public IMessageSink ErrorSink
		{
			get { return _messages; } 
			set { _messages = value ?? Loyc.MessageSink.Current; }
		}
		public IListSource<Token> TokenTree { get { return _tokensRoot; } }

		static readonly Severity _Error = Severity.Error;
		static readonly Severity _Warning = Severity.Warning;

		protected LNode _triviaWordAttribute;
		protected LNode _triviaUseOperatorKeyword;
		protected LNode _triviaForwardedProperty;
		protected LNode _filePrivate;

		public EcsParser(IListSource<Token> tokens, ISourceFile file, IMessageSink messageSink)
		{
			ErrorSink = messageSink;
			Reset(tokens, file);
			
			_triviaWordAttribute = F.Id(S.TriviaWordAttribute);
			_triviaUseOperatorKeyword = F.Id(S.TriviaUseOperatorKeyword);
			_triviaForwardedProperty = F.Id(S.TriviaForwardedProperty);
			_filePrivate = F.Id(S.FilePrivate);
		}

		public virtual void Reset(IListSource<Token> tokens, ISourceFile file)
		{
			CheckParam.IsNotNull("tokens", tokens);
			CheckParam.IsNotNull("file", file);
			_tokensRoot = _tokens = tokens;
			_sourceFile = file;
			F = new LNodeFactory(file);
			InputPosition = 0; // reads LT(0)
		}

		public IListSource<LNode> ParseExprs()
		{
			var list = new RWList<LNode>();
			try {
				ExprList(list);
			} catch (Exception ex) { UnhandledException(ex); }
			return list;
		}

		public IListSource<LNode> ParseStmtsGreedy()
		{
			var list = new RWList<LNode>();
			try {
				StmtList(list);
			} catch (Exception ex) { UnhandledException(ex); }
			return list;
		}

		public IEnumerator<LNode> ParseStmtsLazy()
		{
			while (LA0 != EOF) {
				LNode stmt;
				try { stmt = Stmt(); }
				catch (Exception ex) { UnhandledException(ex); break; }
				yield return stmt;
			}
		}

		void UnhandledException(Exception ex)
		{
			int iPos = GetTextPosition(InputPosition);
			SourcePos pos = _sourceFile.IndexToLine(iPos);
			_messages.Write(Severity.Critical, pos, "Bug: unhandled exception in parser - " + ex.ExceptionMessageAndType());
		}

		#region Methods required by base class and by LLLPG

		protected sealed override int EofInt() { return 0; }
		protected sealed override int LA0Int { get { return _lt0.TypeInt; } }
		protected sealed override Token LT(int i)
		{
			bool fail;
			return _tokens.TryGet(InputPosition + i, out fail);
		}
		protected LNode Error(string message, params object[] args)
		{
			Error(InputPosition, message, args);
			if (args.Length > 0)
				message = string.Format(message, args);
			return F.Call(S.Error, F.Literal(message));
		}
		protected void Error(LNode node, string message, params object[] args)
		{
			_messages.Write(_Error, node, message, args);
		}
		protected void Error(Token token, string message, params object[] args)
		{
			_messages.Write(_Error, _sourceFile.IndexToLine(token.StartIndex), message, args);
		}
		protected override void Error(int lookaheadIndex, string message)
		{
			Error(lookaheadIndex, message, EmptyArray<object>.Value);
		}
		protected override void Error(int lookaheadIndex, string message, params object[] args)
		{
			int iPos = GetTextPosition(InputPosition + lookaheadIndex);
			SourcePos pos = _sourceFile.IndexToLine(iPos);
			_messages.Write(_Error, pos, message, args);
		}
		protected int GetTextPosition(int tokenPosition)
		{
			bool fail;
			var token = _tokens.TryGet(tokenPosition, out fail);
			if (!fail)
				return token.StartIndex;
			else if (_tokens.Count == 0 || tokenPosition < 0)
				return _startTextIndex;
			else
				return _tokens[_tokens.Count - 1].EndIndex;
		}
		protected override string ToString(int type_)
		{
			var type = (TokenType)type_;
			return type.ToString();
		}
		
		protected TT LA0 { [DebuggerStepThrough] get { return _lt0.Type(); } }
		protected TT LA(int i)
		{
			bool fail;
			return _tokens.TryGet(InputPosition + i, out fail).Type();
		}
		new const TokenType EOF = TT.EOF;

		#endregion

		#region Down & Up
		// These are used to traverse into token subtrees, e.g. given w=(x+y)*z, 
		// the outer token list is w=()*z, and the 3 tokens x+y are children of '('
		// So the parser calls something like Down(lparen) to begin parsing inside,
		// then it calls Up() to return to the parent tree.

		Stack<Pair<IListSource<Token>, int>> _parents = new Stack<Pair<IListSource<Token>, int>>();

		protected bool Down(int li)
		{
			return Down(LT(li).Children);
		}
		protected bool Down(IListSource<Token> children)
		{
			if (children != null) {
				_parents.Push(Pair.Create(_tokens, InputPosition));
				_tokens = children;
				InputPosition = 0;
				return true;
			}
			return false;
		}
		protected T Up<T>(T value)
		{
			Up();
			return value;
		}
		protected void Up()
		{
			Debug.Assert(_parents.Count > 0);
			var pair = _parents.Pop();
			_tokens = pair.A;
			InputPosition = pair.B;
		}
		
		#endregion

		#region Other parsing helpers: ExprListInside, etc.

		protected LNode SingleExprInside(Token group, string stmtType, RWList<LNode> list = null, bool allowUnassignedVarDecl = false)
		{
			list = list ?? new RWList<LNode>();
			int oldCount = list.Count;
			AppendExprsInside(group, list, false, allowUnassignedVarDecl);
			if (list.Count != oldCount + 1)
			{
				if (list.Count <= oldCount) {
					LNode result = F.Id(S.Missing, group.StartIndex + 1, group.StartIndex + 1);
					list.Add(result);
					Error(result, "Missing expression inside '{0}'", stmtType);
					return result;
				} else {
					Error(list[1], "There should be only one expression inside '{0}'", stmtType);
					list.Resize(oldCount + 1);
				}
			}
			return list[0];
		}
		protected RWList<LNode> AppendExprsInside(Token group, RWList<LNode> list, bool allowTrailingComma = false, bool allowUnassignedVarDecl = false)
		{
			if (Down(group.Children)) {
				ExprList(list, allowTrailingComma, allowUnassignedVarDecl);
				return Up(list);
			}
			return list;
		}
		protected RWList<LNode> AppendInitializersInside(Token group, RWList<LNode> list)
		{
			if (Down(group.Children)) {
				InitializerList(list);
				return Up(list);
			}
			return list;
		}
		protected RWList<LNode> AppendStmtsInside(Token group, RWList<LNode> list)
		{
			if (Down(group.Children)) {
				StmtList(list);
				return Up(list);
			}
			return list;
		}
		protected RWList<LNode> ExprListInside(Token t, bool allowTrailingComma = false)
		{
			return AppendExprsInside(t, new RWList<LNode>(), allowTrailingComma);
		}
		private RWList<LNode> StmtListInside(Token t)
		{
			return AppendStmtsInside(t, new RWList<LNode>());
 		}
		private RWList<LNode> InitializerListInside(Token t)
		{
			return AppendInitializersInside(t, new RWList<LNode>());
 		}

		// Counts the number of array dimensions, e.g. [] => 1, [,,] => 3
		private int CountDims(Token token, bool allowNonCommas)
		{
			if (token.Type() != TT.LBrack)
				return -1;
			var children = token.Children;
			if (children == null)
				return 1;
			else {
				int commas = 0;
				for (int i = 0; i < children.Count; i++)
					if (children[i].Type() == TT.Comma)
						commas++;
					else if (!allowNonCommas)
						return -1;
				return commas + 1;
			}
		}

		#endregion

		#region Infix precedence table

		// Use "int" rather than Precedence because enum keys are slow in Dictionary
		// Note: << and >> are not listed because they are represented by two tokens
		static readonly Dictionary<int, Precedence> _infixPrecedenceTable = new Dictionary<int, Precedence> {
			{ (int)TT.Dot, EP.Primary },
			{ (int)TT.ColonColon, EP.Primary },
			{ (int)TT.QuickBind, EP.Primary },
			{ (int)TT.PtrArrow, EP.Primary },
			{ (int)TT.NullDot, EP.NullDot },
			{ (int)TT.Power, EP.Power },
			{ (int)TT.Mul, EP.Multiply },
			{ (int)TT.DivMod, EP.Multiply },
			{ (int)TT.Add, EP.Add },
			{ (int)TT.Sub, EP.Add },
			{ (int)TT.NotBits, EP.Add },
			{ (int)TT.DotDot, EP.Range },
			{ (int)TT.BQString, EP.Backtick },
			{ (int)TT.Backslash, EP.Backtick },
			{ (int)TT.LT, EP.Compare },
			{ (int)TT.GT, EP.Compare },
			{ (int)TT.LEGE, EP.Compare },
			{ (int)TT.@is, EP.Compare },
			{ (int)TT.@as, EP.Compare },
			{ (int)TT.@using, EP.Compare },
			{ (int)TT.EqNeq, EP.Equals },
			{ (int)TT.@in, EP.Equals },
			{ (int)TT.AndBits, EP.AndBits },
			{ (int)TT.XorBits, EP.XorBits },
			{ (int)TT.OrBits, EP.OrBits },
			{ (int)TT.And, EP.And },
			{ (int)TT.OrXor, EP.Or },
			{ (int)TT.NullCoalesce, EP.OrIfNull },
			{ (int)TT.QuestionMark, EP.IfElse }, // yeah, not really infix
			{ (int)TT.Set, EP.Assign },
			{ (int)TT.CompoundSet, EP.Assign },
			{ (int)TT.LambdaArrow, EP.Lambda },
		};
		static Precedence InfixPrecedenceOf(TokenType la)
		{
			return _infixPrecedenceTable[(int)la];
		}

		#endregion
	}

	// +-------------------------------------------------------------+
	// | (allowed as attrs.) | (not allowed as keyword attributes)   |
	// | Modifier keywords   | Statement names | Types** | Other***  |
	// |:-------------------:|:---------------:|:-------:|:---------:|
	// | abstract            | break           | bool    | operator  |
	// | const               | case            | byte    | sizeof    |
	// | explicit            | checked         | char    | typeof    |
	// | extern              | class           | decimal |:---------:|
	// | implicit            | continue        | double  | else      |
	// | internal            | default         | float   | catch     |
	// | new*                | delegate        | int     | finally   |
	// | override            | do              | long    |:---------:|
	// | params              | enum            | object  | in        |
	// | private             | event           | sbyte   | as        |
	// | protected           | fixed           | short   | is        |
	// | public              | for             | string  |:---------:|
	// | readonly            | foreach         | uint    | base      |
	// | ref                 | goto            | ulong   | false     |
	// | sealed              | if              | ushort  | null      |
	// | static              | interface       | void    | true      |
	// | unsafe              | lock            |         | this      |
	// | virtual             | namespace       |         |:---------:|
	// | volatile            | return          |         | stackalloc|
	// | out*                | struct          |         |           |
	// |                     | switch          |         |           |
	// |                     | throw           |         |           |
	// |                     | try             |         |           |
	// |                     | unchecked       |         |           |
	// |                     | using           |         |           |
	// |                     | while           |         |           |
	// +-------------------------------------------------------------+
}
