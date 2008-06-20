using System;
using System.Collections.Generic;
using System.Text;
using Loyc.CompilerCore;
using Loyc.Runtime;

namespace Loyc.BooStyle
{
	class EssentialTreeParser
	{
		/// <summary>A stack of these is used to track the opening brackets that 
		/// have not yet been matched.</summary>
		public struct OpenBracket
		{
			public OpenBracket(Symbol open, int indent, AstNode parent) { Open = open; Indent = indent; Parent = parent; }
			public Symbol Open;
			public int Indent; // # of spaces/tabs on the line containing the bracket
			public AstNode Parent; // parent of opening bracket
		}

		public static bool Parse(AstNode rootNode, IEnumerable<AstNode> source)
		{
			Stack<OpenBracket> openers = new Stack<OpenBracket>(); // stack of brackets
			Symbol tt;
			AstNode parent = rootNode;

			bool countingSpaces = true;
			int indent = 0;

			foreach (AstNode t in source)
			{
				tt = t.NodeType;
				if (countingSpaces) {
					// Count the spaces/tabs at the beginning of the line.
					// We just use the string length, which is fine so long as the source 
					// file doesn't mix spaces and tabs. Since the indent is only needed 
					// for diagnostic purposes, it's no disaster to be wrong occasionally.
					if (tt == Tokens.WS)
						indent += t.Range.Length;
					else
						countingSpaces = false;
				}
				if (Tokens.IsOpener(tt)) {
					openers.Push(new OpenBracket(tt, indent, parent));
					parent = null;//TODO: create token
				}
				else if (openers.Count > 0)
				{
					if (Tokens.IsCloser(tt))
					{
						// TODO: set token type
						parent = openers.Pop().Parent;
					}
					//if (tt == Tokens.RPAREN)
					//	Match(opene
				}
				//if (t.VisibleToParser)
					parent.Block.Add(t);
				if (tt == Tokens.NEWLINE) {
					countingSpaces = true;
					indent = 0;
				}
			}
			return openers.Count == 0;
		}
	}

	//class BooTreeParser : EssentialTreeParser
	//{
	//	public BooTreeParser(ICharSource source, IDictionary<string, Symbol> keywords, bool wsaOnly)
	//		: base(new BooLexer(source, keywords, wsaOnly)) { }
	//}
}
