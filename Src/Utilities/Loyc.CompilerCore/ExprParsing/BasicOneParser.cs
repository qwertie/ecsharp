using System;
using System.Collections.Generic;
using Loyc.Runtime;
using Loyc.Utilities;
using System.Collections;
using System.Text;
//using Loyc.CompilerCore.Expressions;
using Loyc.Compatibility.Linq;
using System.Diagnostics;

namespace Loyc.CompilerCore.ExprParsing
{
	/// <summary>
	/// An One Nonterminal Expression Parser (ONEP) implementation by David Piepgrass.
	/// </summary><remarks>
	/// Please see Doc/onep.html for information about this parser, and see 
	/// <see cref="IOneParser">IOneParser</see> for information about the interface.
	/// </remarks>
	public class BasicOneParser<Token> : IOneParser<Token>
		where Token : ITokenValue
	{
		public BasicOneParser() : this(null) { }
		public BasicOneParser(IEnumerable<IOneOperator<Token>> ops)
		{
			if (ops != null) 
				AddRange(ops);
		}
		public void Add(IOneOperator<Token> op) 
			{ _ops.Add(op); _lutBuilt = false; }
		public void AddRange(IEnumerable<IOneOperator<Token>> ops)
			{ _ops.AddRange(ops); _lutBuilt = false; }
		public void Clear() 
			{ _ops.Clear(); ClearLUTs(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() 
			{ return GetEnumerator(); }
		public IEnumerator<IOneOperator<Token>> GetEnumerator() 
			{ return _ops.GetEnumerator(); }
		public IEnumerable<IOneOperator<Token>> Operators 
			{ get { return _ops; } }
		public int OperatorCount 
			{ get { return _ops.Count; } }

		protected int _startPosition;
		protected int _inputPosition;
		protected int _inputLength = 0;
		protected List<IOneOperator<Token>> _ops = new List<IOneOperator<Token>>();
		protected bool _lutBuilt = false;
		protected bool _verbose = true;
		public bool Verbose { get { return _verbose; } set { _verbose = value; } }
		
		/// <summary>Source of tokens derived from _originalSource, or null if
		/// tokens didn't have to be subdivided. If this is nonnull then it is 
		/// the active source of tokens used by LA(), otherwise _originalSource is 
		/// the active source.</summary>
		protected ISimpleSource<Token> _source;
		/// <summary>Source of tokens that was passed to Parse()</summary>
		protected ISimpleSource<Token> _originalSource;

		// Tables to quickly look up ops starting with a certain letter 
		// (int(op.Tok[0].Text[0]) & 0x3F) or int(LA.Text[0]) & 0x3F
		// or of a certain token type. I use a linked list because the lists
		// are expected to be very short (or empty) most of the time, so
		// this is more memory-efficient & faster than using List<> objects.
		protected OpLL[] _textLut1 = new OpLL[32];
		protected OpLL[] _typeLut1   = new OpLL[32];
		protected OpLL[] _textLut2 = new OpLL[32];
		protected OpLL[] _typeLut2   = new OpLL[32];
		protected class OpLL {
			public OpLL(IOneOperator<Token> op, OpLL next, int matchIndex) 
			{
				Op = op; 
				Next = next; 
				Parts = op.Parts; 
				MatchPart = Parts[matchIndex]; 
			}
			public IOneOperator<Token> Op;
			public OpLL Next;
			public IOperatorPartMatcher[] Parts;
			public IOperatorPartMatcher MatchPart;
			public bool Match(ITokenValue token)
			{
				// TODO: If MatchPart.MatchesExpr, then this part should match the
				// "first set", i.e. the first part of any operator. Either that or it should
				// always match, unconditionally; I'm not sure yet.
				if (MatchPart.Prec > -1)
					System.Diagnostics.Debugger.Break();
				return MatchPart.Match(token);
			}
		}

		protected void ClearLUTs()
		{
			// Clear lookup tables
			Array.Clear(_textLut1, 0, _textLut1.Length);
			Array.Clear(_textLut2, 0, _textLut2.Length);
			Array.Clear(_typeLut1, 0, _typeLut1.Length);
			Array.Clear(_typeLut2, 0, _typeLut2.Length);
			_lutBuilt = false;
		}
		protected void AutoBuildLUTs()
		{
			if (_lutBuilt)
				return;
			ClearLUTs();
			
			uint textFirstSet = 0, typeFirstSet = 0;
			
			foreach(IOneOperator<Token> op in _ops) {
				if (op.Parts.Length == 0 || (op.Parts.Length == 1 && op.Parts[0].Prec > -1))
					continue; // This operator is invalid (too short); skip it
				IOperatorPartMatcher p1 = op.Parts[0], p2;
				if (p1.Prec <= -1) {
					if (!string.IsNullOrEmpty(p1.Text)) {
						int i = p1.Text[0] & 0x1F;
						_textLut1[i] = new OpLL(op, _textLut1[i], 0);
						textFirstSet |= (uint)1 << i;
					} else {
						int i = p1.Type.Id & 0x1F;
						_typeLut1[i] = new OpLL(op, _typeLut1[i], 0);
						typeFirstSet |= (uint)1 << i;
					}
				} else if ((p2 = op.Parts[1]).Prec <= -1) {
					// Note: the case where p2.MatchesExpr is true is handled after
					// the first sets are determined.
					if (!string.IsNullOrEmpty(p2.Text)) {
						int i = p2.Text[0] & 0x1F;
						_textLut2[i] = new OpLL(op, _textLut2[i], 1);
					} else {
						int i = p2.Type.Id & 0x1F;
						_typeLut2[i] = new OpLL(op, _typeLut2[i], 1);
					}
				}
			}
			foreach(IOneOperator<Token> op in _ops) {
				if (op.Parts.Length >= 2 && op.Parts[0].Prec > -1 && op.Parts[1].Prec > -1) {
					// This operator begins with two expressions; to detect the second
					// expression with lookahead, add it to all array entries in the 
					// binary lookup table that correspond to the first set of operators
					// in the unary lookup table.
					for (int i = 0; i < 32; i++) {
						if ((textFirstSet & (1 << i)) != 0)
							_textLut2[i] = new OpLL(op, _textLut2[i], 1);
						if ((typeFirstSet & (1 << i)) != 0)
							_typeLut2[i] = new OpLL(op, _typeLut2[i], 1);
					}
				}
			}
			_lutBuilt = true;
		}

		protected bool IsTooLong(OpLL op)
		{
			return op.Parts.Length > _inputLength - _inputPosition;
		}

		protected void MatchText1(ITokenValue la, List<OpLL> list)
		{
			string text = la.Text;
			if (string.IsNullOrEmpty(text))
				return;
			int text_i = text[0] & 0x1F;
			MatchLA(la, _textLut1[text_i], list);
		}
		protected void MatchType1(ITokenValue la, List<OpLL> list)
		{
			Symbol type = la.NodeType;
			if (type == null)
				return;
			int type_i = type.Id & 0x1F;
			MatchLA(la, _typeLut1[type_i], list);
		}
		protected void MatchText2(ITokenValue la, List<OpLL> list, int maxPrec)
		{
			string text = la.Text;
			if (string.IsNullOrEmpty(text))
				return;
			int text_i = text[0] & 0x1F;
			MatchLA(la, _textLut2[text_i], list, maxPrec);
		}
		protected void MatchType2(ITokenValue la, List<OpLL> list, int maxPrec)
		{
			Symbol type = la.NodeType;
			if (type == null)
				return;
			int type_i = type.Id & 0x1F;
			MatchLA(la, _typeLut2[type_i], list, maxPrec);
		}
		void MatchLA(ITokenValue la, OpLL op, List<OpLL> list)
		{
			Debug.Assert(list != null);
			for (; op != null; op = op.Next)
				if (op.Match(la))
					list.Add(op);
		}
		void MatchLA(ITokenValue la, OpLL op, List<OpLL> list, int maxPrec)
		{
			Debug.Assert(list != null);
			for (; op != null; op = op.Next)
				if (op.Parts[0].Prec <= maxPrec && op.Match(la))
					list.Add(op);
		}

		protected static IOperatorPartMatcher EofToken = new OneOperatorPart(null, null);

		public OneOperatorMatch<Token> Parse(ISourceFile<Token> source, ref int position, bool untilEnd, IOperatorDivider<Token> divider)
		{
			AutoBuildLUTs();
			_source = _originalSource = source;
			_inputPosition = position;
			_inputLength = source.Count;
			IncompleteDividerSource<Token> dividerSource = null;
			if (divider != null && IncompleteDividerSource<Token>.MightNeedDivision
			    (source, position, _inputLength - position, divider)) {
				// Create _dividerSource to use as our active token source.
				dividerSource = new IncompleteDividerSource<Token>(divider);
				dividerSource.Process(source, position, _inputLength - position);
				_source = dividerSource;
				// In the _dividerSource, position zero corresponds to the 
				// original input position.
				_inputPosition = 0;
				_inputLength = _source.Count;
			}
			_startPosition = _inputPosition;

			if (_verbose) SpitTokenList();

			// Look at the first token from the input TODO...
			IOperatorPartMatcher eof = null;
			if (untilEnd) eof = EofToken;
			OneOperatorMatch<Token> outMatch = SubParse(999, eof);
			
			// Update position and return result.
			if (dividerSource != null)
				position = dividerSource.IndexInOriginalSource(_inputPosition);
			else
				position = _inputPosition;

			return outMatch;
		}

		protected OneOperatorMatch<Token> SubParse(int maxPrec, IOperatorPartMatcher followToken)
		{
			if (_verbose) PushSpit(maxPrec, followToken);
			List<OpLL> inMatches = new List<OpLL>(); // Declared here just to avoid repeated reallocation
			MyOneOperatorMatch lastKnownGood = null;
			int lastKnownGoodPos = _inputPosition;

			try {
				// Initial match.
				MyOneOperatorMatch prematch;
				prematch = SubParseOneOp(maxPrec, inMatches, null);
				if (prematch == null)
					return null;

				// Initial match succeeded. Advance input position past it, check the 
				// follow token, then stard matching against ops that start with an 
				// expression.
				for(;;) {
					_inputPosition = prematch.EndInputPosition;
					if (MatchesFollowToken(LA(0), followToken)) {
						lastKnownGood = prematch;
						lastKnownGoodPos = _inputPosition;
						if (_verbose) {
							// Output looks like  |999|35| e{foo} [OK]
							SpitCustom(Localize.From("{0} [OK]", StringizeMatch(prematch)));
						}
					}
					inMatches.Clear();
					prematch = SubParseOneOp(maxPrec, inMatches, prematch);
					if (prematch == null)
						break;
				}
			} finally {
				if (_verbose) PopSpit();
			}
			_inputPosition = lastKnownGoodPos;
			return lastKnownGood;
		}

		private bool MatchesFollowToken(ITokenValue token, IOperatorPartMatcher followToken)
		{
			if (followToken == null) // Meaning anything is allowed to follow
				return true;
			else if (followToken == EofToken)
				return token == null;
			else
				return followToken.Match(token);
		}

		/// <summary>
		/// Attempts to match a single operator.
		/// </summary>
		/// <param name="maxPrec">Max precedence at this level (e.g. 999)</param>
		/// <param name="inMatches">List of partial matches (1 or 2 parts matched)</param>
		/// <param name="prematch">If SubParseOneOp() is to match operators that start
		/// with another expression (e.g. e+e, e?e:e, e++), prematch represents that 
		/// expression. prematch is null if SubParseOneOp() is to match operators that 
		/// start with a token. (e.g. -e, *e)</param>
		/// <returns>
		/// If matching failed or was ambiguous, null is returned. Otherwise, the only 
		/// acceptable match will be returned (and any other matches in the linked list,
		/// if any, are irrelevant).
		/// </returns>
		/// <remarks>
		/// This is only called by SubParse. There is no followToken argument because 
		/// any token may follow.
		/// </remarks>
		protected MyOneOperatorMatch SubParseOneOp(int maxPrec, List<OpLL> inMatches, MyOneOperatorMatch prematch)
		{
			MyOneOperatorMatch outMatches = null;
			Debug.Assert(inMatches.Count == 0);

			Token LA0 = LA(0); // examine the next token
			if (_verbose) {
				// Output looks like  |999|35| (?'-':PUNC)
				//                or  |999|35| e{foo} (?'-':PUNC)
				if (prematch != null)
					SpitCustom(StringizeMatch(prematch) + " " + StringizeLA(LA0));
				else
					SpitCustom(StringizeLA(LA0));
			}
			if (LA0 == null)
				return null; // EOF; can't match anything
			
			// Advance past it because TryMatches() starts matching after the "prematched" token
			Consume(1);

			int tmp = _inputPosition; // for debug check
			try {
				if (prematch != null)
					MatchText2(LA0, inMatches, maxPrec);
				else
					MatchText1(LA0, inMatches);

				bool success = false;
				if (inMatches.Count > 0) {
					success = TryMatches(maxPrec, inMatches, out outMatches, prematch, LA0);
					Debug.Assert(tmp == _inputPosition);
					inMatches.Clear();
				}

				if (!success)
				{
					if (prematch != null)
						MatchType2(LA0, inMatches, maxPrec);
					else
						MatchType1(LA0, inMatches);

					if (inMatches.Count > 0) {
						MyOneOperatorMatch oldOutMatches = outMatches;
						success = TryMatches(maxPrec, inMatches, out outMatches, prematch, LA0);
						Debug.Assert(tmp == _inputPosition);
						inMatches.Clear();

						if (!success) {
							if (oldOutMatches != null) {
								if (outMatches == null)
									outMatches = oldOutMatches;
								else {
									// Append oldOutMatches to outMatches
									MyOneOperatorMatch m = outMatches;
									while (m.Next != null)
										m = m.Next;
									m.Next = oldOutMatches;
								}
							}
						}
					}
				}
				if (success) {
					Debug.Assert(outMatches != null);
					return outMatches;
				} else {
					if (outMatches == null)
						// Print LA(-1) because we had advanced past the first token
						if (_verbose) SpitFail(LA(-1), false); // |999|35|  No match at '@':PUNC
					else {
						// There are matches but not success: the input must be ambiguous
						if (_verbose) SpitFail(LA(-1), true); // |999|35|  Ambiguity at '-':PUNC
						Debug.Assert(inMatches.Count == 0);
						// Display error(s)
						WriteDisambiguationErrors(outMatches);
					}
					return null;
				}
			} finally {
				Consume(-1); // Go back to the starting point
			}
		}

		protected class MyOneOperatorMatch : OneOperatorMatch<Token>
		{
			public MyOneOperatorMatch() { }
			public MyOneOperatorMatch(MyOneOperatorMatch other)
				: base(other)
				{ this.EndInputPosition = other.EndInputPosition; }
			public int EndInputPosition;
			public MyOneOperatorMatch Next; // Another match, if any
			public Message AmbigMsg;        // Linked list
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="maxPrec">Maximum precedence value of subexpressions on the 
		/// right-hand side of a matched operator.</param>
		/// <param name="inMatches">List of potential matches where one token has 
		/// been prematched.</param>
		/// <param name="outMatches">
		/// A linked list of acceptable and unacceptable matches. If this function 
		/// returns true, then the only acceptable match will be at the front of the 
		/// list, and matches that were not accepted are not required to be included 
		/// in the list.</param>
		/// <param name="prematch">If TryMatches() is given operators that start
		/// with another expression (e.g. e+e, e?e:e, e++), prematch represents that 
		/// expression. prematch is null if TryMatches() is trying operators that 
		/// start with a token. (e.g. -e, *e)</param>
		/// <param name="LA0">the prematched token (never null)</param>
		/// <returns>True if there was exactly one match, or false otherwise.</returns>
		protected bool TryMatches(int maxPrec, List<OpLL> inMatches, out MyOneOperatorMatch outMatches, MyOneOperatorMatch prematch, Token LA0)
		{
			// The current, incomplete, match:
			MyOneOperatorMatch outMatch = new MyOneOperatorMatch();
			outMatches = null; // a linked list of completed matches
			
			if (_verbose && inMatches.Count > 1) 
				SpitMatchList(inMatches);

			int count = inMatches.Count;
			for (int i = 0; i < count; i++) {
				OpLL inMatch = inMatches[i];

				// Initialize OneOperatorMatch result structure
				Debug.Assert(!outMatch.IsAcceptable);    // if assert fails, we should be setting outMatch.IsAcceptable = false here
				Debug.Assert(outMatch.AmbigMsg == null); // if assert fails, we should be setting outMatch.AmbigMsg = null here
				outMatch.Operator = inMatch.Op;
				if (outMatch.Parts == null || outMatch.Parts.Length != inMatch.Parts.Length)
					outMatch.Parts = new OneOperatorMatchPart<Token>[inMatch.Parts.Length];

				// Match against the rest of the operator
				int prematched = prematch != null ? 2 : 1;
				if (_verbose) {
					if (i > 0)
						SpitCustom("|> ", Localize.From("Next match:"));
					// Set outMatch.Parts[0] and [1] to the 1 or 2 prematched parts:
					SetPrematched(prematch, LA0, outMatch);
					// As an optimization, we normally don't set it until we know that 
					// the rest of the operator matches. But in Verbose mode these 
					// parts will be printed out during MatchTheRest() to show the 
					// progress of the match.
				}
				if (!MatchTheRest(maxPrec, inMatch, outMatch, prematched))
					continue;

				// Success.
				SetPrematched(prematch, LA0, outMatch);

				// Return the result now if it will be the only one, else queue it up 
				// for possible disambiguation later.
				if (i + 1 == inMatches.Count && outMatches == null) {
					outMatches = outMatch;
					return true;
				} else {
					if (outMatches != null)
						outMatch.Next = outMatches;
					outMatches = outMatch;
					outMatch = new MyOneOperatorMatch();
				}
			}
			if (outMatches == null)
				return false;
			else if (outMatches.Next == null)
				return true;
			else // Two or more
				return Disambiguate(ref outMatches);
		}

		protected static void SetPrematched(MyOneOperatorMatch prematch, Token LA0, MyOneOperatorMatch outMatch)
		{
			if (prematch != null) {
				outMatch.Parts[0].Expr = prematch;
				outMatch.Parts[1].Token = LA0;
			} else
				outMatch.Parts[0].Token = LA0;
		}

		protected virtual void WriteDisambiguationErrors(MyOneOperatorMatch matches)
		{
			// Build lists: all operators and rejected operators
			StringBuilder all_ops = new StringBuilder();
			StringBuilder rejected = new StringBuilder();
			int count = 0, countRejected = 0;
			MyOneOperatorMatch m;
			for (m = matches; m != null; m = m.Next) {
				if (all_ops.Length > 0)
					all_ops.Append(", ");
				all_ops.Append('\'');
				all_ops.Append(m.Operator.Name);
				all_ops.Append('\'');
				count++;
				if (!m.IsAcceptable) {
					countRejected++;
					if (rejected.Length > 0)
						rejected.Append(", ");
					rejected.Append('\'');
					rejected.Append(m.Operator.Name);
					rejected.Append('\'');
				}
			}
			WriteErrorEN("Failed to resolve ambiguity between {0} operators: {1}.",
				count, all_ops.ToString());
			if (countRejected == count)
				WriteErrorEN("All possibilities were rejected.");
			else if (countRejected > 0)
				WriteErrorEN("{0} possibilities were rejected: {1}",
					countRejected, rejected.ToString());

			// Print out messages from operators, if any
			WriteAmbigMsgs(matches);
		}
		protected void WriteAmbigMsgs(MyOneOperatorMatch match)
		{
			// Write messages in reverse order because the last match is at the 
			// beginning of the list
			if (match != null) {
				WriteAmbigMsgs(match.Next);
				WriteAmbigMsgs(match, match.AmbigMsg);
			}
		}
		private void WriteAmbigMsgs(MyOneOperatorMatch match, Message m)
		{
			// Write messages in reverse order for the same reason as above
			if (m != null) {
				WriteAmbigMsgs(match, m.Prev);
				string msg = Localize.From(m.Resource, m.Msg, m.Args);
				WriteErrorEN("While interpreting '{0}': {1}", match.Operator.Name, msg);
			}
		}

		protected virtual bool DisambiguateTwo(ref MyOneOperatorMatch matches)
		{
			// The most common, simple case
			int prio = ComparePriority(matches.Operator, matches.Next.Operator);
			
			if (prio < 0)
				// The second match takes priority.
				matches = matches.Next;
			if (prio != 0)
				return true; // One of the two took priority

			bool a0 = CheckAcceptable(matches);
			bool a1 = CheckAcceptable(matches.Next);
			int count = (a0 ? 1 : 0) + (a1 ? 1 : 0);
			if (count != 1)
				return false;
			else if (a1) // Only the second match was accepted
				matches = matches.Next;
			return true;
		}
		/// <summary>Attempts to disambiguate two or more matches.</summary>
		/// <param name="matches">Linked list of matches. On exit, if true is
		/// returned then matches points to the only successful match.</param>
		/// <returns>true if successful (there was exactly one match), false
		/// otherwise.</returns>
		protected virtual bool Disambiguate(ref MyOneOperatorMatch matches)
		{
			// expecting at least two matches to be disambiguated:
			Debug.Assert(matches != null && matches.Next != null); 

			if (matches.Next.Next == null) {
				return DisambiguateTwo(ref matches);
			} else {
				// First check acceptability.
				int count = 0; // a tally of accepted matches
				MyOneOperatorMatch lastAcceptable = null;
				MyOneOperatorMatch m, m2;
				for (m = matches; m != null; m = m.Next) {
					if (CheckAcceptable(m)) {
						lastAcceptable = m;
						count++;
					}
				}

				if (count > 1) {
					// Next, narrow down the list by eliminating operators where something 
					// else has a higher priority.
					for (m = matches; m != null; m = m.Next) {
						if (!m.IsAcceptable)
							continue;
						for (m2 = matches; m2 != null; m2 = m2.Next) {
							if (m == m2 || !m2.IsAcceptable)
								continue;
							int cmp = ComparePriority(m.Operator, m2.Operator);
							if (cmp < 0) {
								// Operator m eliminated: something else has higher priority
								m.IsAcceptable = false;
								count--;
								break;
							}
						}
						if (m.IsAcceptable)
							lastAcceptable = m;
					}
				}
				if (count == 1) {
					matches = lastAcceptable;
					return true;
				} else
					return false;
			}
		}
		protected static readonly string OpCallInternalErr = "OpCallInternalErr\0Internal error: calling '{0}' in operator '{1}' caused an exception: {2}";

		protected int ComparePriority(IOneOperator<Token> a, IOneOperator<Token> b)
		{
			int pa = 0, pb = 0;
			try {
				pa = a.ComparePriority(b);
			} catch (Exception e) {
				WriteErrorEN(OpCallInternalErr, "ComparePriority", a.Name, e.ToString());
			}
			try {
				pb = b.ComparePriority(a);
			} catch (Exception e) {
				WriteErrorEN(OpCallInternalErr, "ComparePriority", a.Name, e.ToString());
			}
			return pa - pb; // Return >0 if pa>pb
		}
		protected bool CheckAcceptable(MyOneOperatorMatch match)
		{
			try {
				_messageSink.CurMatch = match;
				return (match.IsAcceptable = match.Operator.IsAcceptable(match, _messageSink));
			} catch(Exception e) {
				WriteErrorEN(OpCallInternalErr, "IsAcceptable", match.Operator.Name, e.ToString());
				return false;
			}
		}

		protected bool MatchTheRest(int maxPrec, OpLL inMatch, MyOneOperatorMatch outMatch, int prematched)
		{
			BeginGuess();
			try {
				for(int i = prematched;; i++) {
					if (_verbose) 
						SpitPartialMatchStatus(outMatch.Parts, inMatch.Parts, i, "");
					if (i >= inMatch.Parts.Length)
						break;
					
					// Check the next part
					if (!MatchOnePart(maxPrec, inMatch.Parts, outMatch.Parts, i)) {
						if (_verbose)
							SpitPartialMatchStatus(outMatch.Parts, inMatch.Parts, i, Localize.From("[REJECTED]"));
						return false;
					}
				}
				return true;
			} finally {
				outMatch.EndInputPosition = _inputPosition;
				EndGuess();
			}
		}

		private bool MatchOnePart(int maxPrec, IOperatorPartMatcher[] inParts, OneOperatorMatchPart<Token>[] outParts, int i)
		{
			if (inParts[i].Prec > -1) 
			{
				// To match an expression, first determine the follow token and precedence.
				IOperatorPartMatcher followThis;
				bool isFinal = i + 1 == inParts.Length; // is this the last part?
				if (isFinal)
					followThis = null;
				else
					followThis = inParts[i + 1];

				int prec = inParts[i].Prec;
				if (isFinal) {
					if (prec > maxPrec)
						prec = maxPrec;
					if ((prec & 1) == 0)
						prec--; // Left associativity
				}

				return (outParts[i].Expr = SubParse(prec, followThis)) != null;
			} else {
				// Simple. Match a single token.
				Token LA0 = LA(0);
				outParts[i].Token = LA0;
				Consume(1);
				return inParts[i].Match(LA0);
			}
		}
		protected void Consume(int amount)
		{
			_inputPosition += amount;
			Debug.Assert(_inputPosition >= 0 && _inputPosition <= _inputLength);
		}
		protected Stack<int> _savedPositions = new Stack<int>();
		public void BeginGuess()
		{
			_savedPositions.Push(_inputPosition);
		}
		public void EndGuess()
		{
			_inputPosition = _savedPositions.Pop();
		}
		protected void WriteErrorEN(string msg, params object[] args)
		{
			Console.WriteLine(msg, args);
		}

		public class Message
		{
			public Message(Message Prev, Symbol Category, Symbol Resource, string Msg, object[] Args)
			{
				this.Prev = Prev;
				this.Category = Category;
				this.Resource = Resource;
				this.Msg = Msg;
				this.Args = Args;
			}
			public Symbol Category;
			public Symbol Resource;
			public string Msg;
			public object[] Args;
			public Message Prev; // previous message (linked list)
		}
		protected class MessageSink : ISimpleMessageSink 
		{
			public MyOneOperatorMatch CurMatch;
			
			public void Write(Symbol category, string msg, params object[] args)
			{
				Message msgInfo = new Message(CurMatch.AmbigMsg, category, null, msg, args);
				CurMatch.AmbigMsg = msgInfo;
			}
			public void Write(Symbol category, Symbol resource, params object[] args)
			{
				Message msg = new Message(CurMatch.AmbigMsg, category, resource, null, args);
				CurMatch.AmbigMsg = msg;
			}
		}
		protected MessageSink _messageSink = new MessageSink();

		protected Token LA(int p)
		{
			return _source[_inputPosition + p];
		}

		#region Verbose mode progress spitting
		protected List<Pair<int, IOperatorPartMatcher>> _spitStack;

		protected virtual void SpitTokenList()
		{
			SpitStack();
			for (int i = 0; i < _inputLength; i++)
				Console.Write(LA(i - _startPosition).Text + ' ');
			Console.Write("// Types: ");
			for (int i = 0; i < _inputLength; i++)
				Console.Write(LA(i - _startPosition).NodeType.Name + ' ');
			Console.WriteLine();
		}
		protected void SpitStack() { SpitStack("|  "); }
		protected virtual void SpitStack(string prompt)
		{
			StringBuilder s = new StringBuilder(10);
			if (_spitStack != null) {
				foreach (Pair<int, IOperatorPartMatcher> p in _spitStack) {
					s.Append('|');
					s.Append(p.A.ToString());
					if (p.B != null) {
						if (p.B == EofToken)
							s.Append('$');
						else if (p.B.Text != null) {
							s.Append(' ');
							s.Append(p.B.Text);
						} else
							s.Append(p.B.Type.ToString());
					}
				}
			}
			s.Append(prompt);
			Console.Write(s.ToString());
		}
		protected void SpitCustom(string s) { SpitCustom("|  ", s); }
		protected virtual void SpitCustom(string prompt, string s)
		{
			SpitStack(prompt);
			Console.WriteLine(s);
		}
		protected virtual void PushSpit(int maxPrec, IOperatorPartMatcher followToken)
		{
			if (_spitStack == null)
				_spitStack = new List<Pair<int, IOperatorPartMatcher>>();
			_spitStack.Add(new Pair<int, IOperatorPartMatcher>(maxPrec, followToken));
		}
		protected virtual void PopSpit()
		{
			if (_spitStack.Count != 0) _spitStack.RemoveAt(_spitStack.Count-1);
		}
		protected virtual string StringizeLA(ITokenValue la)
		{
			if (la == null)
				return "(? $)";
			else
				return string.Format("(?'{0}'{1})", la.Text, la.NodeType.ToString());
		}

		protected virtual string StringizeMatch(OneOperatorMatch<Token> e)
		{
			StringBuilder sb = new StringBuilder("e{", 16);
			OneOperatorMatchPart<Token>[] ps = e.Parts;
			bool spacingEl = false;
			for (int i = 0; i < ps.Length; i++) {
				string text;
				if (ps[i].MatchedExpr) {
					text = "e";
					if (e.Parts.Length == 1 && !e.Parts[0].MatchedExpr)
						text = e.Parts[0].Token.Text;
				} else {
					text = ps[i].Token.Text;
					if (string.IsNullOrEmpty(text)) // unusual
						text = ps[i].Token.NodeType.ToString();
				}
				if (spacingEl && char.IsLetterOrDigit(text[0]))
					sb.Append(' ');
				sb.Append(text);
				spacingEl = char.IsLetterOrDigit(text[text.Length - 1]);
			}
			sb.Append('}');
			return sb.ToString();
		}

		protected virtual void SpitMatchList(List<BasicOneParser<Token>.OpLL> matches)
		{
			SpitStack("|> Matches: ");
			bool first = true;
			foreach (OpLL op in matches) {
				if (first)
					first = false;
				else
					Console.Write("; ");
				SpitParts(op.Op.Parts, 0);
			}
			Console.WriteLine();
		}

		protected virtual void SpitParts(IOperatorPartMatcher[] parts, int i)
		{
			for (; i < parts.Length; i++) {
				if (parts[i].Prec > -1)
					Console.Write("e{0}", parts[i].Prec);
				else if (parts[i].Text != null) {
					Console.Write("'{0}'", parts[i].Text);
					if (parts[i].Type != null)
						Console.Write("({0})", parts[i].Type.Name);
				} else {
					Console.Write(parts[i].Type.Name);
				}
				if (i != parts.Length - 1)
					Console.Write(' ');
			}
		}
		protected virtual void SpitPartialMatchStatus(OneOperatorMatchPart<Token>[] matched, IOperatorPartMatcher[] unmatched, int pos, string suffix)
		{
			SpitStack("|> ");
			for (int i = 0; i < pos; i++) {
				if (matched[i].MatchedExpr)
					Console.Write(StringizeMatch(matched[i].Expr));
				else
					Console.Write("'" + matched[i].Token.Text + "'");
				if (i + 1 == pos)
					Console.Write('.');
				Console.Write(' ');
			}
			SpitParts(unmatched, pos);
			Console.WriteLine(" {0}", suffix);
		}
		private void SpitFail(ITokenValue la, bool ambiguous)
		{
			SpitStack();
			if (ambiguous)
				Console.Write("Ambiguity at '{0}'{1}", la.Text, la.NodeType.ToString());
			else
				Console.Write("No match at '{0}'{1}", la.Text, la.NodeType.ToString());
			Console.WriteLine();
		}
		#endregion
	}

}
