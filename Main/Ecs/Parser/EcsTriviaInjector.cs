using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc.Syntax.Lexing;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Ecs.Parser
{
	/// <summary>Trivia injector customized for Enhanced C#.</summary>
	/// <remarks>
	/// How newline trivia works in EC# (mostly this is the same as 
	/// <see cref="StandardTriviaInjector"/>):
	/// <ul>
	/// <li>Implicitly, there is a newline before every node within a braced block,
	/// and <see cref="CodeSymbols.TriviaAppendStatement"/> is required to suppress 
	/// it. The Loyc tree does not specify whether or not there is a newline before
	/// the closing brace; it is assumed that there should be a newline unless ALL
	/// children of the braced block use %appendStatement.</li>
	/// <li>Implicitly, there is a newline before every top-level statement except 
	/// the first, and %appendStatement can be used to suppress it.</li>
	/// <li>By default, the printer doesn't print a newline before an opening brace.
	/// #trivia_newline can be added to the braced block node to represent one.</li>
	/// <li>This injector explicitly encodes newlines in all other locations. 
	/// However, the EC# printer is designed to print all syntax trees reasonably 
	/// whether they have passed through the trivia injector or not. Therefore, on
	/// non-braced-block child statements of certain nodes (if statements, while
	/// loops, for loops, and other statements used without braces), the printer
	/// will print a newline before a child statement if it (1) does not have 
	/// #trivia_newline or %appendStatement attributes and (2) does not have 
	/// an ancestor with the <see cref="NodeStyle.OneLiner"/> style.</li>
	/// <li>In an if-else statement, `else` has no representation in the syntax 
	/// tree and may appear to the injector the same as a blank line. In this 
	/// situation the first newline is attached to the child of #if at index 1,
	/// and is deleted so that there is only one newline before the second child.</li>
	/// <li>Constructors that call another constructor (`: base(...)`) get a 
	/// newline before the colon by default, which can be suppressed with 
	/// %appendStatement. Note: These constructors have an unusual syntax 
	/// tree which the standard trivia injector can't handle properly; see comment 
	/// in DoneAttaching() for details.</li>
	/// </ul>
	/// </remarks>
	public class EcsTriviaInjector : StandardTriviaInjector
	{
		public EcsTriviaInjector(IListSource<Token> sortedTrivia, ISourceFile sourceFile, int newlineTypeInt, string mlCommentPrefix, string mlCommentSuffix, string slCommentPrefix) 
			: base(sortedTrivia, sourceFile, newlineTypeInt, mlCommentPrefix, mlCommentSuffix, slCommentPrefix)
		{
		}

		protected override LNodeList GetAttachedTrivia(LNode node, IListSource<Token> trivia, TriviaLocation loc, LNode parent, int indexInParent)
		{
			int? nli;
			if (loc == TriviaLocation.Trailing && indexInParent == 1 && parent != null && parent.Calls(CodeSymbols.If, 3) && 
				(nli = trivia.FinalIndexWhere(t => t.Type() == TokenType.Newline)) != null) {
				// The 'else' keyword is invisible here, but it often appears on a line by 
				// itself; remove a newline to avoid creating a blank line when printing.
				var triviaSans = new DList<Token>(trivia);
				triviaSans.RemoveAt(nli.Value);
				trivia = triviaSans;
			}
			return base.GetAttachedTrivia(node, trivia, loc, parent, indexInParent);
		}

		protected override LNode MakeTriviaAttribute(Token t)
		{
			if (t.Type() == TokenType.PPregion)
				return LNode.Trivia(S.TriviaRegion, t.Value.ToString(), t.Range(SourceFile));
			else if (t.Type() == TokenType.PPendregion)
				return LNode.Trivia(S.TriviaEndRegion, t.Value.ToString(), t.Range(SourceFile));
			return base.MakeTriviaAttribute(t);
		}

		protected override LNode DoneAttaching(LNode node, LNode parent, int indexInParent)
		{
			// Constructors are funky in EC# because EC# generalizes constructors so
			// you can write, for example, `this() { F(); base(); G(); }`. 
			// Plain C# constructors like `this() : base() { G(); }`, `base(x)`
			// are actually stored like   `this() { base(); G(); }`, where the colon
			// is the beginning of the Range of the constructor body and the opening 
			// brace is the range of the Target of the method body. This makes it 
			// difficult to get the trivia attached the way we want. For example, this 
			// constructor:
			//
			//     Foo(int x) 
			//        : base(x)
			//     {
			//        Bar();
			//     }
			//
			// Gets trivia attached like this:
			//
			//     #cons(``, Foo, #(int x), @`%newline` (@`%newline` `'{}`)(
			//         [`%appendStatement`] base(x), Bar()));
			//
			// This code changes the trivia to something more reasonable:
			//
			//     #cons(``, Foo, #(int x), @`%newline` { base(x), Bar() });
			//
			LNode baseCall;
			if (node.CallsMin(S.Braces, 1) && parent != null && parent.Calls(S.Constructor) 
				&& (baseCall = node.Args[0]).Range.StartIndex < node.Target.Range.StartIndex)
			{
				if (RemoveLeadingNewline(ref node) != null)
					node = node.WithArgChanged(0, baseCall.WithoutAttrNamed(S.TriviaAppendStatement));
				LNode target = node.Target, newline_trivia;
				if ((newline_trivia = RemoveLeadingNewline(ref target)) != null) {
					node = node.WithTarget(target).PlusAttrBefore(newline_trivia);
				}
			}
			return node;
		}

		LNode RemoveLeadingNewline(ref LNode node)
		{
			LNode newline;
			node = node.WithoutAttrNamed(S.TriviaNewline, out newline);
			return newline;
		}
	}
}
