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
	/// newlines) from a list and adds it as trivia attributes into LNodes. This
	/// makes it possible to preserve comments and newlines independently of the
	/// language parser, so that the parser need not be specifically designed to 
	/// preserve them.</summary>
	/// <remarks>
	/// Usage: Call the constructor, then call <see cref="AbstractTriviaInjector{T}.Run"/>.
	/// See <see cref="AbstractTriviaInjector{T}"/> for more information.
	/// <para/>
	/// In brief, given input code with C-style comments like
	/// <pre>
	/// {
	///		// Leading Comment 1
	///		/* Leading Comment 2 * /
	///		/* Leading Comment 3 * / x = y; // Trailing Comment 1
	///		/* Trailing Comment 2 * /
	///		
	///		y = z; TheEnd();
	///	}
	/// </pre>
	/// [NOTE: the space in "* /" is a workaround for a serious bug in Doxygen, the html doc generator]
	/// 
	/// The output, expressed in LESv2, is
	/// <pre>
	/// {
	///		@[@%SLComment(" Leading Comment 1"),
	///		  @%MLComment(" Leading Comment 2 "),
	///		  @%newline,
	///		  @%MLComment(" Leading Comment 3 "),
	///		  @%trailing(
	///		    @%SLComment(" Trailing Comment 1"),
	///		    @%MLComment(" Trailing Comment 2 "),
	///		    @%newline)]
	///		x = y;
	///		y = z;
	///		@[@%appendStatement] TheEnd();
	/// }
	/// </pre>
	/// By default, printers should add newlines between statements within a braced 
	/// block. Therefore, this class does not add trivia to mark a single newline 
	/// between statements; instead, it adds an %appendStatement attribute 
	/// when the expected newline prior to a statement in a braced block was NOT 
	/// present. Also, a newline is expected after a single-line comment and no 
	/// <c>%newline</c> is created for the expected newline.
	/// <para/>
	/// Finally, since printers typically add a newline before the closing brace by
	/// default, this class avoids adding an attribute for that newline, if present.
	/// </remarks>
	public class StandardTriviaInjector : AbstractTriviaInjector<Token>
	{
		LNode _trivia_newline, _trivia_appendStatement;
		ISourceFile _sourceFile;
		public ISourceFile SourceFile
		{
			get { return _sourceFile; }
			set {
				_sourceFile = value;
				_trivia_newline = LNode.Id(S.TriviaNewline, SourceFile);
				_trivia_appendStatement = LNode.Id(S.TriviaAppendStatement, SourceFile);
			}
		}
		public int NewlineTypeInt { get; set; }
		public string SLCommentPrefix { get; set; }
		public string SLCommentSuffix { get; set; }
		public string MLCommentPrefix { get; set; }
		public string MLCommentSuffix { get; set; }
		public bool TopLevelIsBlock { get; set; } // whether the root node list should expect newlines between items

		/// <summary>Initializes <see cref="StandardTriviaInjector"/>.</summary>
		/// <param name="sortedTrivia">A list of trivia that will be injected into the 
		/// nodes passed to <see cref="AbstractTriviaInjector{T}.Run"/>. Normally,
		/// text of comments is extracted from the provided <see cref="ISourceFile"/>,
		/// but comment tokens could instead store their text as a string in their
		/// <see cref="Token.Value"/>.</param>
		/// <param name="sourceFile">This is used as the source file of the <see cref="LNode.Range"/> 
		///   of all trivia attributes that the algorithm injects.</param>
		/// <param name="newlineTypeInt">A token is interpreted as a newline when this value equals 
		///   <see cref="Token.TypeInt"/>.</param>
		/// <param name="mlCommentPrefix">If a token's text begins with this prefix it is assumed to be 
		///   a multi-line comment and the prefix is removed.</param>
		/// <param name="mlCommentSuffix">If a multi-line comment's text ends with this suffix, 
		///   the suffix is removed.</param>
		/// <param name="slCommentPrefix">If a token's text begins with this prefix it is assumed to be 
		///   a single-line comment and the prefix is removed.</param>
		/// <param name="topLevelIsBlock">If true, newlines are expected between items at the top level, 
		///   like in a braced block, so that, for example, if two consecutive nodes are on the same line,
		///   an @appendStatement trivia attribute will be attached to the second one.</param>
		public StandardTriviaInjector(IListSource<Token> sortedTrivia, ISourceFile sourceFile, int newlineTypeInt, string mlCommentPrefix, string mlCommentSuffix, string slCommentPrefix, bool topLevelIsBlock = true) : base(sortedTrivia)
		{
			SourceFile = sourceFile;
			NewlineTypeInt = newlineTypeInt;
			MLCommentPrefix = mlCommentPrefix;
			MLCommentSuffix = mlCommentSuffix;
			SLCommentPrefix = slCommentPrefix;
			TopLevelIsBlock = topLevelIsBlock;
		}

		protected override LNodeList GetAttachedTrivia(LNode node, IListSource<Token> trivia, TriviaLocation loc, LNode parent, int indexInParent)
		{
			var newAttrs = LNode.List();
			int i = 0;
			if (loc == TriviaLocation.Leading) {
				// leading trivia
				if (HasImplicitLeadingNewline(node, parent, indexInParent)) {
					// ignore expected leading newline
					if (trivia.Count > 0 && trivia[0].TypeInt == NewlineTypeInt)
						i++;
					else
						newAttrs.Add(_trivia_appendStatement);
				}
			}
			bool justAddedSLComment = false;
			LNode attr = null;
			for (; i < trivia.Count; i++) {
				var t = trivia[i];
				// ignore first newline after single-line comment
				if (t.TypeInt == NewlineTypeInt && justAddedSLComment) {
					justAddedSLComment = false;
					continue;
				}
				if ((attr = MakeTriviaAttribute(t)) != null) {
					justAddedSLComment = attr.Calls(S.TriviaSLComment);
					newAttrs.Add(attr);
				}
			}
			// Suppress newline before closing brace or at EOF
			if (loc == TriviaLocation.TrailingExtra && newAttrs.Count > 0 && newAttrs.Last == _trivia_newline) {
				if (parent == null || parent.Calls(S.Braces))
					newAttrs.Pop(); // Printers add a newline here anyway
			}
			return newAttrs;
		}

		/// <summary>Called to find out if a newline is to be added implicitly 
		/// before the current child of the specified node.</summary>
		/// <returns>By default, returns true if the node is a braced block.</returns>
		protected virtual bool HasImplicitLeadingNewline(LNode child, LNode parent, int indexInParent)
		{
			if (parent != null)
				return parent.Calls(S.Braces) && indexInParent >= 0;
			else
				return indexInParent > 0 && TopLevelIsBlock;
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
				return LNode.Trivia(commentType, text.ToString(), t.Range(SourceFile));
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
