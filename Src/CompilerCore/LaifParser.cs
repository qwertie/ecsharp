using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Essentials;
using Loyc.Utilities;
using NUnit.Framework;

namespace Loyc.CompilerCore
{
	public class LaifParser
	{
		static Dictionary<string, Symbol> _keywords;
		static Symbol _null = GSymbol.Get("null");
		static LaifParser()
		{
			_keywords = new Dictionary<string, Symbol>();
			_keywords.Add("null", _null);
		}

		protected AstNode _parent;
		protected RVList<AstNode> _input;
		protected int _inputPosition = 0;
		AstNode LA0;
		Symbol LT0;
		int _lastErrorPos = int.MinValue;
		string _currentFileName;
		SourceRange _currentRange;

		public Func<string, ISourceFile> LookupFile { get; set; }

		protected AstNode LA(int i)
		{
			return i == 0 ? LA0 : _input[_inputPosition + i, null];
		}
		protected void Consume()
		{
			_inputPosition++;
			LA0 = _input[_inputPosition, null];
			LT0 = LA0 != null ? LA0.NodeType : null;
			Debug.Assert(_inputPosition <= _input.Count);
		}

		public RVList<AstNode> Parse(ISourceFile input, ISourceFile defaultFile)
		{
			return Parse(input, new SourceRange(defaultFile, -1, 0));
		}
		public RVList<AstNode> Parse(ISourceFile input, SourceRange startPos)
		{
			_currentRange = startPos;

			// Lex and tree-parse the input
			IEnumerable<AstNode> lexer = new CStyleLexer(input, _keywords);
			VisibleTokenFilter<AstNode> filter = new VisibleTokenFilter<AstNode>(lexer);
			EssentialTreeParser etp = new EssentialTreeParser();
			LA0 = AstNode.New(startPos, GSymbol.Empty);
			bool success = etp.Parse(ref LA0, filter); // May print errors

			// Now interpret the tree (whose root is LA0)
			return ParseInside<RVList<AstNode>>(ParseNodeList);
		}

		T ParseInside<T>(Func<T> parser)
		{
			AstNode parent = LA0;

			AstNode oldParent = _parent;
			RVList<AstNode> oldInput = _input;
			int oldPos = _inputPosition;
			AstNode oldLA0 = LA0;
			Symbol oldLT0 = LT0;

			_parent = parent;
			_input = parent.Children;
			_inputPosition = -1;
			Consume(); // retrieve first token in _input

			try {
				return parser();
			} finally {
				_parent = oldParent;
				_input = oldInput;
				_inputPosition = oldPos;
				LA0 = oldLA0;
				LT0 = oldLT0;
				_lastErrorPos = int.MinValue;
			}
		}

		private RVList<AstNode> ParseNodeList()
		{
			RVList<AstNode> output = RVList<AstNode>.Empty;
			SourceRange range = SourceRange.Nowhere;

			// The syntax is...
			// OOB* ID ([...] | {...} | (...) | OOB)*
			for (;;) {
				while (LT0 != Tokens.ID) {
					if (LA0 == null)
						return output;
					AutoWriteSyntaxError("identifier");
					Consume();
				}
				output.Add(ParseNode(ref _currentRange));
			}
		}

		private AstNode ParseNode()
		{
			SourceRange empty = SourceRange.Nowhere;
			return ParseNode(ref empty);
		}
		private AstNode ParseNode(ref SourceRange range)
		{
			Debug.Assert(LT0 == Tokens.ID);

			// The syntax is...
			// ID ([...] | {...} | (...) | @ Range | OOB)*
			Symbol nodeType = GSymbol.Get(ParseToken.ParseID(LA0.SourceText));
			AstNode first = LA0;
			object value = null;
			TagsInWList<object> tags = null;
			RVList<AstNode> children = new RVList<AstNode>();
			Consume();

			for(;;) {
				if (LT0 == Tokens.LBRACK) {
					value = ParseInside<object>(ParseValue);
					ConsumeBrackets();
				} else if (LT0 == Tokens.LBRACE) {
					tags = ParseInside<TagsInWList<object>>(ParseTags);
					ConsumeBrackets();
				} else if (LT0 == Tokens.LPAREN) {
					children = ParseInside<RVList<AstNode>>(ParseNodeList);
					ConsumeBrackets();
				} else if (LT0 == Tokens.PUNC && LA0.Range.Length >= 1 && LA0.Range[0] == '@') {
					ParseRange(ref range);
				} else if (LT0 == Tokens.ID || LT0 == null) {
					break; // beginning of next node, or EOF
				} else {
					AutoWriteSyntaxError("AstNode");
					Consume();
				}
			}

			if (tags != null)
				return AstNode.New(range, nodeType, children, value, tags);
			else
				return AstNode.New(range, nodeType, children, value);
		}

		private void ConsumeBrackets()
		{
			Debug.Assert(Tokens.IsOpener(LT0));
			Consume();
			if (Tokens.IsCloser(LT0))
				Consume();
		}

		object ParseValue()
		{
			ICharSource source = LA0.Range.Source;
			int pos = LA0.Range.BeginIndex;
			object value = null;
			CompilerMsg error = null;

			if (LT0 == Tokens.SQ_STRING)
			{	// char
				value = ParseToken.ParseChar(source, ref pos, out error);
			}
			else if (LT0 == Tokens.DQ_STRING || LT0 == Tokens.TQ_STRING)
			{	// string
				value = ParseToken.ParseString(source, ref pos, out error);
			}
			else if (LT0 == Tokens.SYMBOL)
			{	// Symbol
				Debug.Assert(source[pos] == ':');
				pos++;
				value = GSymbol.Get(ParseToken.ParseString(source, ref pos, out error));
			}
			else if (LT0 == Tokens.INT)
			{	// int
				value = ParseToken.ParseInt(source, ref pos, out error);
			}
			else if (LT0 == Tokens.REAL)
			{	// double
				// TODO
				double result;
				if (double.TryParse(LA0.SourceText, out result))
					value = result;
			}
			else if (LT0 == _null)
			{
				Consume();
				return null;
			}
			else if (LT0 == Tokens.LBRACE)
			{	// Symbol dictionary
				value = ParseInside<TagsInWList<object>>(ParseTags);
			}
			else if (LT0 == Tokens.ID)
			{
				// Either it's an AstNode or an arbitrary object depending on LA(1)
				// (an arbitrary object has string or ID for LA(1))
				AstNode LA1 = LA(1);
				Symbol LT1 = LA1 != null ? LA1.NodeType : null;
				bool hasTwo = LT1 != null && !Tokens.SetOfClosers.Contains(LT1);

				if (!hasTwo)
				{
					string id = ParseToken.ParseID(source, ref pos, out error);
					value = AstNode.New(DefaultRange(), GSymbol.Get(id));
				}
				else if (LT1 == Tokens.ID || Tokens.IsString(LT1))
				{
					// TODO
				}
				else
					value = ParseNode();
			}

			if (error != null)
				CompilerOutput.Write(error);
			else if (value == null)
				Error.Write(LA0.Range.Begin, "Syntax error");
			return value;
		}

		private SourceRange DefaultRange()
		{
			if (_currentRange.Length > 0)
				return new SourceRange(_currentRange.Source, _currentRange.EndIndex, 0);
			return _currentRange;
		}

		TagsInWList<object> ParseTags()
		{
			// Parse symbol dictionary
			// Format: (ID ":" Value ("," ID ":" Value)*)? ","?

			TagsInWList<object> tags = new TagsInWList<object>();
			CompilerMsg error = null;

			AstNode LA1 = LA(1);
			while (LT0 == Tokens.ID && LA1 != null && LA1.Range.Length == 1 && LA1.Range[0] == ':')
			{
				int pos = LA0.Range.BeginIndex;
				string key = ParseToken.ParseID(LA0.Range.Source, ref pos, out error);
				Symbol key2 = GSymbol.Get(key);
				if (tags.HasTag(key2))
					error = CompilerMsg.Error(LA0.Range, "Duplicate key {0}", LA0.SourceText);
				if (error != null)
					CompilerOutput.Write(error);

				Consume();
				Consume();
				
				object value = ParseValue();
				tags.SetTag(key2, value);

				if (LA0.Range.Length == 1 && LA0.Range[0] == ',')
					Consume();
				else
					break;
			}
			return tags;
		}

		private void ParseRange(ref SourceRange range)
		{
			Debug.Assert(LT0 == Tokens.PUNC && LA0.Range[0] == '@');

			if (LA0.Range.Length > 1 && LA0.Range[1] == '?')
			{
				Consume();
				range = new SourceRange(range.Source, -1, 0);
				return;
			}
			if (LA0.Range.Length == 1 && LA(1) != null &&
				LA(1).NodeType == Tokens.PUNC && LA(1).Range[0] == '?')
			{
				Consume();
				Consume();
				range = new SourceRange(range.Source, -1, 0);
				return;
			}
			
			Consume();

			ISourceFile source = range.Source;
			int beginIndex = range.EndIndex, length = 0;

			try
			{
				if (Tokens.IsString(LT0))
				{
					string filename = ParseString(LA0);

					if (_currentFileName != filename)
					{
						_currentFileName = filename;
						if (LookupFile != null)
							source = LookupFile(filename);
						else
							source = new EmptySourceFile(filename, _currentRange.Source.Language);
					}
					Consume();
				}

				if (LT0 == Tokens.INT)
				{
					beginIndex = ParseInt(LA0);
					Consume();
				}
				else if (LT0 == Tokens.LPAREN)
				{
					int beginLine, beginCol;

					if (LA0.ChildCount >= 3 &&
						LA0.Children[0].NodeType == Tokens.INT &&
						LA0.Children[1].NodeType == Tokens.PUNC &&
						LA0.Children[2].NodeType == Tokens.INT)
					{
						beginLine = ParseInt(LA0.Children[0]);
						beginCol = ParseInt(LA0.Children[2]);
						beginIndex = source.LineToIndex(new SourcePos(_currentFileName, beginLine, beginCol));
						ConsumeBrackets();
					}
				}
				else
				{
					Error.Write(LA0.Range.Begin, "Source location expected");
					return;
				}

				if (LT0 == Tokens.PUNC && LA0.SourceText == ":")
				{
					Consume();

					if (LT0 == Tokens.INT)
						length = ParseInt(LA0);
					else
						Error.Write(LA0.Range.Begin, "Token length expected");
				}

				range = new SourceRange(source, beginIndex, length);
			}
			catch (Exception ex)
			{
				Error.Write(LA0 != null ? LA0.Range.Begin : SourcePos.Nowhere, 
					"Internal error decoding node range: {0}", ex.Message);
			}
		}


		private string ParseString(AstNode node)
		{
			CompilerMsg error;
			int pos = node.Range.BeginIndex;
			string value = ParseToken.ParseString(node.Range.Source, ref pos, out error);
			if (error != null)
				CompilerOutput.Write(error);
			if (node.Value == null)
				node.Value = value;
			return value;
		}
		private int ParseInt(AstNode node)
		{
			CompilerMsg error;
			int pos = node.Range.BeginIndex;
			int value = ParseToken.ParseInt(node.Range.Source, ref pos, out error);
			if (error != null)
				CompilerOutput.Write(error);
			if (node.Value == null)
				node.Value = value;
			return value;
		}
		void AutoWriteSyntaxError(string expected)
		{
			Debug.Assert(LA0 != null);
			if (!Tokens.IsOob(LT0) && _inputPosition != _lastErrorPos + 1)
				Error.Write(LA0.Range.Begin, "Syntax error; expected {0}", expected);
		}
	}

	[TestFixture]
	public class LaifParserTests
	{
		LaifParser _p = new LaifParser();

		[Test]
		public void SimpleTests()
		{
			Test("");
			Test("ID", Node(ID));
			Test("INT[1]", Node(INT, 1));
			Test("REAL[1.0]", Node(REAL, 1.0));
			Test("SQ_STRING['A']", Node(SQ_STRING, 'A'));
			Test(@"INT {EOS:'\n'}", Node(INT, null, Tag(EOS, '\n')));
			Test(@"INT [55] {EOS:'\n'}", Node(INT, (object)55, Tag(EOS, '\n')));
			Test("ID (ID)", AstNode.NewUnary(SourceRange.Nowhere, ID, Node(ID)));
		}

		[Test]
		public void TestsWithRanges()
		{
			Test("ID @? INT @? [1]", Node(ID), Node(INT, 1));
			// TODO
		}

		private void Test(string @in, params AstNode[] @expected)
		{
			try {
				RVList<AstNode> Out = _p.Parse(
					new StringCharSourceFile(@in, "Laif"), SourceRange.Nowhere.Source);
				TestEqual(new RVList<AstNode>(@expected), Out);
			} catch {
				Trace.WriteLine("Error for input: " + @in);
				throw;
			}
		}

		private void TestEqual(RVList<AstNode> exp, RVList<AstNode> act)
		{
			Assert.AreEqual(exp.Count, act.Count);
			for (int i = 0; i < exp.Count; i++)
				TestEqual(exp[i], act[i]);
		}

		private void TestEqual(AstNode exp, AstNode act)
		{
			Assert.AreEqual(exp.NodeType, act.NodeType);
			TestEqual(exp.Value, act.Value);
			TestEqual(exp.Children, act.Children);
			TestEqual((TagsInWList<object>)exp.Tags, (TagsInWList<object>)act.Tags);
		}

		private void TestEqual(TagsInWList<object> exp, TagsInWList<object> act)
		{
			foreach(Symbol tag in exp.Tags.Keys) {
				Assert.That(act.HasTag(tag));
				TestEqual(exp.GetTag(tag), act.GetTag(tag));
			}
			Assert.AreEqual(exp.TagCount, act.TagCount);
		}

		private void TestEqual(object exp, object act)
		{
			if (exp is AstNode && act is AstNode)
				TestEqual((AstNode)exp, (AstNode)act);
			else if (exp is TagsInWList<object>)
				TestEqual((TagsInWList<object>)exp, (TagsInWList<object>)act);
			else
				Assert.AreEqual(exp, act);
		}

		static AstNode Node(Symbol type)
		{
			return AstNode.New(SourceRange.Nowhere, type);
		}
		static AstNode Node(Symbol type, object value)
		{
			return AstNode.New(SourceRange.Nowhere, type, value);
		}
		static AstNode Node(Symbol type, object value, TagsInWList<object> tags)
		{
			return AstNode.New(SourceRange.Nowhere, type, new RVList<AstNode>(), value, tags);
		}

		private TagsInWList<object> Tag(Symbol s, object v)
		{
			TagsInWList<object> tags = new TagsInWList<object>();
			tags.SetTag(s, v);
			return tags;
		}

		static public readonly Symbol ID = GSymbol.Get("ID");
		static public readonly Symbol PUNC = GSymbol.Get("PUNC");
		static public readonly Symbol EOS = GSymbol.Get("EOS");
		static public readonly Symbol ML_COMMENT = GSymbol.Get("ML_COMMENT");
		static public readonly Symbol SL_COMMENT = GSymbol.Get("SL_COMMENT");
		static public readonly Symbol LPAREN = GSymbol.Get("LPAREN");
		static public readonly Symbol RPAREN = GSymbol.Get("RPAREN");
		static public readonly Symbol LBRACK = GSymbol.Get("LBRACK");
		static public readonly Symbol RBRACK = GSymbol.Get("RBRACK");
		static public readonly Symbol LBRACE = GSymbol.Get("LBRACE");
		static public readonly Symbol RBRACE = GSymbol.Get("RBRACE");
		static public readonly Symbol INT = GSymbol.Get("INT");
		static public readonly Symbol REAL = GSymbol.Get("REAL");
		static public readonly Symbol SYMBOL = GSymbol.Get("SYMBOL");
		static public readonly Symbol SQ_STRING = GSymbol.Get("SQ_STRING");
		static public readonly Symbol DQ_STRING = GSymbol.Get("DQ_STRING");
		static public readonly Symbol BQ_STRING = GSymbol.Get("BQ_STRING");
		static public readonly Symbol TQ_STRING = GSymbol.Get("TQ_STRING");
		static public readonly Symbol RE_STRING = GSymbol.Get("RE_STRING");
	}
}
