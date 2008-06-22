using System;
using System.Collections.Generic;
using System.Text;
using Loyc.CompilerCore;
using Loyc.Runtime;

namespace Loyc.BooStyle
{
	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// ETP error detection & recovery scenarios:
	/// <code>
	/// It is suspected that within brackets or braces, all tokens should be at the
	/// same level and deeper than the brackets/braces at previous levels
	/// Suspicions are caused by:
	/// - Indent without '{' => suspect '{'
	/// - Unindent (or failure to indent after '{') without '}' => suspect '}'
	/// 
	/// Topmost suspicion is resolved by
	/// - '{' or '(' found at level where it was expected
	/// 
	/// |class Foo (
	///  |    def f()
	///  |        // suspect {(
	///  |        blah
	///  |    garbage
	///  |    foo ) // narrow suspect to (
	/// |     } // no matching suspects, so ignore }
	/// |) // realize suspect ( to match
	/// 
	/// |if (foo
	///  |   == bar)
	/// |
	/// |   // suspect {( due to indent    
	/// |   print
	/// |} // match suspect
	/// |
	/// |void f()
	/// |    if (x >= 2)
	/// |        x = 2
	/// |}
	/// 
	/// 
	/// Rule: when opener used, delete alternate opener
	/// 
	/// Source file example #1
	/// ----------------------
	/// class Foo {										 {
	///		def f():									 { X( X)
	///     // suspect #1 {(							 { 1{(
	///         return goob								 
	///     } // split suspect #1						 ~{ 1{ ~}
	///
	///		void g()									 1{ 1( X( X)
	///		// suspect #2 {								 1{ 1( 2{(
	///			if (x) { blah blah }					 1{ 1( 2{( ~( ~) ~{ ~}
	///         if x == 3 &&							 1{ 1( 2{(
	///         // suspect #3 {							 1{ 1( 2{( 3{(
	///				y == 4 {							 1{ 1( 2{( 3{( {
	///			// suspect } due to dedent--cancels #3	 1{ 1( 2{( ~3{( { ~0)}
	///			}										 1{ 1( 2{( ~{ ~}
	///		} // no matching {							 1{ 1( 2{( }
	///		// Recovery: insert { at suspicion #2		 1{ 1( ~2{ ~}
	/// } // no matching {								 1{ 1( }
	/// // Recovery: insert { at suspicion #1			 ~1{ ~}
	/// 
	/// class Foo {										 {
	///		def f() {									 { ~( ~) {
	///         return goob
	/// 
	///     // suspect #4: } due to unindent			 { { 4}
	///		void g() {									 { { 4} ~( ~) {
	///			if (x) { ... }							 { { 4} { ~( ~) ~{ ~}
	///		}											 { { 4} ~{ ~}
	/// }												 { ~{ 4} ~}
	/// 
	/// class Foo {										 { 4} {
	///		void f() {									 { 4} { ~( ~) {
	///			if (x) {								 { 4} { { ~( ~) {
	///         // Suspicion #5: need } due to dedent    { 4} { { { 5}
	///         Console.WriteLine(x);					 { 4} { { { 5} ~( ~)
	///         if (y {}								 { 4} { { { 5} ( ~{ ~}
	///         // Suspicion #6: need ) due to dedent	 { 4} { { { 5} ( 6)
	///			) // Close suspicion #6 cuz ) found		 { 4} { { { 5} ~( ~6) ~)
	///		}
	/// 
	///		void g(int x = (12+2))/3)					 { 4} { { { 5} ~( ~( ~) ~) !)
	///     // Error above: last ) unmatchable (btw, brackets and braces can't 
	///		// match). No matching suspicion, so ignore token or leave unmatched.
	///		{											 { 4} { { { 5} {
	///			if (x { ... }							 { 4} { { { 5} { ( ~{ ~}
	///		// Suspect ) due to dedent                   { 4} { { { 5} { ( 0)
	///		}                                            { 4} { { { 5} { ( 0) }
	///		// Error: } can't match (. Recovery: 0)      { 4} { { { 5} ~{ ~( ~) ~}
	/// 
	///		void h() {                                   { 4} { { { 5} ~( ~) {
	///         if (x == 3 &&							 { 4} { { { 5} { (
	///				y == 4)								 { 4} { { { 5} { ~( ~)
	///	            // suspect #6 due to indent			 { 4} { { { 5} { 6{(
	///	            foo	                                     
	///			}
	///		}
	/// }
	/// // Error at end. Recovery: insert } at suspicion #5
	/// // Still an error. Recovery: insert } at suspicion #4
	/// </code>
	/// </remarks>
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
