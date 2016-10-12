using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Syntax.Lexing;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Syntax
{
	/// <summary>Encapsulates an algorithm that consumes trivia (comments and 
	/// newlines) from a list and adds it as trivia attributes into LNodes.</summary>
	/// <remarks>
	/// Usage: Call the constructor, then call <see cref="Run"/>. See 
	/// <see cref="AbstractTriviaInjector{T}"/> for more information.
	/// <para/>
	/// In brief, given input code with C-style comments like
	/// <pre>
	/// {
	///		// Leading Comment 1
	///		/* Leading Comment 2 */
	///		/* Leading Comment 3 */ x = y; // Trailing Comment 1
	///		/* Trailing Comment 2 */
	///		
	///		y = z; TheEnd();
	///	}
	/// </pre>
	/// The output, expressed in LESv2, is
	/// <pre>
	/// {
	///		@[#trivia_SLComment(" Leading Comment 1"),
	///		  #trivia_MLComment(" Leading Comment 2 "),
	///		  #trivia_newline,
	///		  #trivia_MLComment(" Leading Comment 3 "),
	///		  #trivia_beginTrailingTrivia,
	///		  #trivia_SLComment(" Trailing Comment 1"),
	///		  #trivia_MLComment(" Trailing Comment 2 "),
	///		  #trivia_newline]
	///		x = y;
	///		y = z;
	///		@[#trivia_appendStatement] TheEnd();
	/// }
	/// </pre>
	/// By default, printers add newlines between statements within a braced 
	/// block. Therefore, this class does not add trivia to mark a single newline 
	/// between statements; instead, it adds a #trivia_appendStatement attribute 
	/// when the expected newline prior to a statement in a braced block was NOT 
	/// present. Also, a newline is expected after a single-line comment and no 
	/// <c>#trivia_newline</c> is created for the expected newline.
	/// <para/>
	/// Finally, since printers typically add a newline before the closing brace by
	/// default, this class avoids adding an attribute for that newline, if present.
	/// </remarks>
	public class StandardTriviaInjector : AbstractTriviaInjector<Token>
	{
		LNode _trivia_newline, _trivia_beginTrailingTrivia, _trivia_appendStatement;
		ISourceFile _sourceFile;
		public ISourceFile SourceFile
		{
			get { return _sourceFile; }
			set {
				_sourceFile = value;
				_trivia_newline = LNode.Id(S.TriviaNewline, SourceFile);
				_trivia_beginTrailingTrivia = LNode.Id(S.TriviaBeginTrailingTrivia, SourceFile);
				_trivia_appendStatement = LNode.Id(S.TriviaAppendStatement, SourceFile);
			}
		}
		public int NewlineTypeInt { get; set; }
		public string SLCommentPrefix { get; set; }
		public string SLCommentSuffix { get; set; }
		public string MLCommentPrefix { get; set; }
		public string MLCommentSuffix { get; set; }

		/// <summary>Initializes <see cref="StandardTriviaInjector"/>.</summary>
		/// <param name="sortedTrivia">A list of trivia that will be injected into the 
		/// nodes passed to <see cref="AbstractTriviaInjector{T}.Apply"/>. Normally,
		/// text of comments is extracted from the provided <see cref="ISourceFile"/>,
		/// but comment tokens could instead store their text as a string in their
		/// <see cref="Token.Value"/>.</param>
		/// <param name="sourceFile">This is used as the source file of the <see cref="LNode.Range"/> of all trivia attributes that the algorithm injects.</param>
		/// <param name="newlineTypeInt">A token is interpreted as a newline when this value equals <see cref="Token.TypeInt"/>.</param>
		/// <param name="mlCommentPrefix">If a token's text begins with this prefix it is assumed to be a multi-line comment and the prefix is removed.</param>
		/// <param name="mlCommentSuffix">If a multi-line comment's text ends with this suffix, the suffix is removed.</param>
		/// <param name="slCommentPrefix">If a token's text begins with this prefix it is assumed to be a single-line comment and the prefix is removed.</param>
		public StandardTriviaInjector(IListSource<Token> sortedTrivia, ISourceFile sourceFile, int newlineTypeInt, string mlCommentPrefix, string mlCommentSuffix, string slCommentPrefix) : base(sortedTrivia)
		{
			SourceFile = sourceFile;
			NewlineTypeInt = newlineTypeInt;
			MLCommentPrefix = mlCommentPrefix;
			MLCommentSuffix = mlCommentSuffix;
			SLCommentPrefix = slCommentPrefix;
		}

		// This pair of variables is used to suppress a newline attribute after a single-
		// line comment (trivia can be added with multiple calls to AttachTriviaTo)
		bool _justAddedSLComment;
		LNode _lastAttachedToNode;

		protected override LNode AttachTriviaTo(LNode node, IListSource<Token> trivia, TriviaLocation loc, LNode parent, int indexInParent)
		{
			if (_lastAttachedToNode != node) {
				_lastAttachedToNode = null;
				_justAddedSLComment = false;
			}
			VList<LNode> attrs = node.Attrs;
			int i = 0;
			if (loc == TriviaLocation.Leading) {
				// leading trivia
				if (parent == null ? indexInParent > 0 : parent.Calls(S.Braces) && indexInParent >= 0) {
					// ignore expected leading newline
					if (trivia.Count > 0 && trivia[0].TypeInt == NewlineTypeInt)
						i++;
					else
						attrs.Add(_trivia_appendStatement);
				}
			} else {
				if (trivia.Count == 0)
					goto stop;
				else if (node.AttrNamed(S.TriviaBeginTrailingTrivia) == null) {
					attrs.Add(_trivia_beginTrailingTrivia);
					_justAddedSLComment = false;
				}
			}
			LNode attr = null;
			for (; i < trivia.Count; i++) {
				var t = trivia[i];
				// ignore first newline after single-line comment
				if (t.TypeInt == NewlineTypeInt && _justAddedSLComment) {
					_justAddedSLComment = false;
					continue;
				}
				if ((attr = MakeTriviaAttribute(t)) != null) {
					_justAddedSLComment = attr.Calls(S.TriviaSLComment);
					attrs.Add(attr);
				}
			}
			// Suppress newline before closing brace or at EOF
			if (loc == TriviaLocation.TrailingExtra && attrs.Count > 0 && attrs.Last == _trivia_newline)
				if (parent == null || parent.Calls(S.Braces)) {
					attrs.Pop(); // Printers add a newline here anyway
					if (attrs.Count > 0 && attrs.Last == _trivia_beginTrailingTrivia)
						attrs.Pop();
				}
		stop:
			return _lastAttachedToNode = node.WithAttrs(attrs);
		}

		/// <summary>Called to transform a trivia token into a trivia attribute.</summary>
		/// <remarks>If a trivia token is not recognized, null is returned to ignore the trivia.</remarks>
		protected virtual LNode MakeTriviaAttribute(Token t)
		{
			if (t.TypeInt == NewlineTypeInt)
				return _trivia_newline;
			else {
				Symbol commentType = null;
				UString text;
				if (t.Value == null || t.Value == WhitespaceTag.Value)
					text = SourceFile.Text.Slice(t.StartIndex, t.Length);
				else
					text = t.Value is UString ? ((UString)t.Value) : t.Value.ToString();

				if (MLCommentPrefix != null && text.StartsWith(MLCommentPrefix)) {
					commentType = S.TriviaMLComment;
					text = text.Substring(MLCommentPrefix.Length);
					if (text.EndsWith(MLCommentSuffix))
						text = text.Left(text.Length - MLCommentSuffix.Length);
				} else if (SLCommentPrefix != null && text.StartsWith(SLCommentPrefix)) {
					commentType = S.TriviaSLComment;
					text = text.Substring(SLCommentPrefix.Length);
					if (SLCommentSuffix != null && text.EndsWith(SLCommentSuffix))
						text = text.Left(text.Length - SLCommentSuffix.Length);
				}
				if (commentType == null)
					return null;
				return LNode.Trivia(commentType, text.ToString(), SourceFile, t.StartIndex, t.Length);
			}
		}

		protected override bool IsNewline(Token trivia)
		{
			return trivia.TypeInt == NewlineTypeInt;
		}
		protected override SourceRange GetRange(Token trivia)
		{
			return new SourceRange(SourceFile, trivia.StartIndex, trivia.Length);
		}
	}
}
