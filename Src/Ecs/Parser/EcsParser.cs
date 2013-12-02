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

namespace Ecs.Parser
{
	using TT = TokenType;

	/// <summary>Parses Enhanced C# code into a sequence of Loyc trees 
	/// (<see cref="LNode"/>), one per top-level statement.</summary>
	/// <remarks>
	/// You can use <see cref="EcsLanguageService.Value"/> with <see cref="ParsingService.Parse"/>
	/// to easily parse a text string (holding zero or more EC# statements) into a Loyc tree.
	/// </remarks>
	public partial class EcsParser : BaseParser<Token>
	{
		protected IMessageSink _messages;
		protected LNodeFactory F;
		protected ISourceFile _sourceFile;
		protected IListSource<Token> _tokensRoot;
		protected IListSource<Token> _tokens;
		// index into source text of the first token at the current depth (inside 
		// parenthesis, etc.). Used if we need to print an error inside empty {} [] ()
		protected int _startTextIndex = 0;
		protected LNode _missingExpr = null; // used by MissingExpr

		public IMessageSink MessageSink
		{
			get { return _messages; } 
			set { _messages = value ?? Loyc.Utilities.MessageSink.Current; }
		}
		public IListSource<Token> TokenTree { get { return _tokensRoot; } }
		public ISourceFile SourceFile { get { return _sourceFile; } }

		static readonly Symbol _Error = Loyc.Utilities.MessageSink.Error;

		public EcsParser(IListSource<Token> tokens, ISourceFile file, IMessageSink messageSink)
		{
			MessageSink = messageSink;
			Reset(tokens, file);
		}

		public virtual void Reset(IListSource<Token> tokens, ISourceFile file)
		{
			_tokensRoot = _tokens = tokens;
			_sourceFile = file;
			F = new LNodeFactory(file);
			InputPosition = 0; // reads LT(0)
			_missingExpr = null;
		}

		public IListSource<LNode> ParseExprs()
		{
			throw new NotImplementedException();
		}

		public IListSource<LNode> ParseStmtsGreedy()
		{
			throw new NotImplementedException();
		}

		public IEnumerator<LNode> ParseStmtsLazy()
		{
			throw new NotImplementedException();
		}

		#region Methods required by base class and by LLLPG

		protected sealed override int EofInt() { return 0; }
		protected sealed override int LA0Int { get { return _lt0.TypeInt; } }
		protected sealed override Token LT(int i)
		{
			bool fail = false;
			return _tokens.TryGet(InputPosition + i, ref fail);
		}
		protected override void Error(int inputPosition, string message)
		{
			int iPos = GetTextPosition(inputPosition);
			SourcePos pos = _sourceFile.IndexToLine(iPos);
			_messages.Write(_Error, pos, message);
		}
		protected int GetTextPosition(int tokenPosition)
		{
			bool fail = false;
			var token = _tokens.TryGet(tokenPosition, ref fail);
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
			bool fail = false;
			return _tokens.TryGet(InputPosition + i, ref fail).Type();
		}
		const TokenType EOF = TT.EOF;

		#endregion

		#region Token tree parsing helpers: Down, Up, ExprListInside, etc.

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

		protected virtual RWList<LNode> ParseAttributes(Token group, RWList<LNode> list)
		{
			return AppendExprsInside(group, list);
		}
		protected RWList<LNode> AppendExprsInside(Token group, RWList<LNode> list)
		{
			if (Down(group.Children)) {
				ExprList(ref list);
				return Up(list);
			}
			return list;
		}

		protected RWList<LNode> ExprListInside(Token t)
		{
			return AppendExprsInside(t, new RWList<LNode>());
		}

		#endregion

		private void ExprList(ref RWList<LNode> list)
		{
			throw new NotImplementedException();
		}
		private IEnumerable<LNode> ExprInside(Token t)
		{
			throw new NotImplementedException();
		}
		private int CountDims(Token token)
		{
			throw new NotImplementedException();
		}
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
