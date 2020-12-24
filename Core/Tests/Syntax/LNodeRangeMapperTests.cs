using Loyc.Collections.Impl;
using Loyc.MiniTest;
using Loyc.Syntax.Les;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Syntax.Tests
{
	[TestFixture]
	public class LNodeRangeMapperTests : TestHelpers
	{
		[Test]
		public void BasicTests()
		{
			var parsed = Les2LanguageService.Value.Parse((UString)@"
				// Leading comment
				x = random.NextInt();
				// Comment nodes extend a node's range, which could certainly, in principle,
				// affect LNodeRangeMapper's ability to do its job properly. But given
				// a range covering the text `x = random.NextInt()`, it should still decide
				// that the node for `x = random.NextInt()` is a better match than, say, `x`
				// or `random.NextInt()` or the `if` block below.
				
				if x > ExtraordinarilyLongIdentifier {
					return x - ExtraordinarilyLongIdentifier;
				} else {
					Log.Write(Severity.Error, ""Unexpectedly low x"");
					// Trailing comment

					return -(1);
				}
				// Trailing comment",
				"Input.les").ToList();
			// Replace first "return" statement with a synthetic call to #return
			LNode originalFirstReturn = parsed[1][1][0];
			parsed[1] = parsed[1].WithArgChanged(1, parsed[1][1].WithArgChanged(0, LNode.Call(S.Return, LNode.List(originalFirstReturn[0]))));

			LNode assignment = parsed[0], @if = parsed[1], logWrite = @if[3][0], firstReturn = @if[1][0], random = assignment[1].Target[0];
			// Verify we've correctly extracted parts of the input tree
			IsTrue(@if[1].Calls(S.Braces) && @if[3].Calls(S.Braces));
			IsTrue(firstReturn.Calls(S.Return) && random.IsIdNamed("random"));
			AreEqual("Input.les", @if.Target.Range.Source.FileName);
			AreSame(EmptySourceFile.Synthetic, firstReturn.Range.Source);

			// Create a simulated output file mapping in which all indexes are multiplied by 2
			IndexRange TweakedRange(IIndexRange r) => new IndexRange(r.StartIndex * 2, r.Length * 2);
			var mapper = new LNodeRangeMapper();
			foreach (var node in parsed.SelectMany(stmt => stmt.DescendantsAndSelf()).Where(n => n.Range.StartIndex >= 0))
				mapper.SaveRange(node, TweakedRange(node.Range));
			// In real life, synthetic nodes do get their range saved, but here we must do it manually
			mapper.SaveRange(firstReturn, TweakedRange(originalFirstReturn.Range));

			IndexRange CallFindRelatedNode_AndExpectFirstFoundNodeToBe(LNode node, IndexRange searchQuery, int maxSearchResults)
			{
				var list = mapper.FindRelatedNodes(searchQuery, 10);
				AreEqual(node, list[0].Item1);
				return list[0].B;
			}

			// Let the tests begin.
			foreach (var node in new[] { assignment, @if, logWrite, random })
			{
				// Given perfect input, FindRelatedNodes should always list the correct node first
				var range = CallFindRelatedNode_AndExpectFirstFoundNodeToBe(node, TweakedRange(node.Range), 10);
				AreEqual(TweakedRange(node.Range), range);

				// FindMostUsefulRelatedNode will find the same result
				var pair2 = mapper.FindMostUsefulRelatedNode(TweakedRange(node.Range), node.Range.Source);
				AreEqual(node, pair2.Item1);
				AreEqual(TweakedRange(node.Range), pair2.Item2);
			}

			// However, the firstReturn is synthetic. It can still be found with FindRelatedNodes(), 
			// but `FindMostUsefulRelatedNode` won't return it because its source file is wrong.
			// Instead, the best match should be the first argument to #return.
			CallFindRelatedNode_AndExpectFirstFoundNodeToBe(firstReturn, TweakedRange(originalFirstReturn.Range), 10);
			var bestPair = mapper.FindMostUsefulRelatedNode(TweakedRange(originalFirstReturn.Range), originalFirstReturn.Range.Source);
			AreEqual(firstReturn[0], bestPair.Item1);
			AreEqual(TweakedRange(firstReturn[0].Range), bestPair.Item2);

			// Compute and test the target range for `x = random.NextInt()` with comments excluded
			var assignmentRange = new IndexRange(assignment[0].Range.StartIndex, assignment[1].Range.EndIndex);
			CallFindRelatedNode_AndExpectFirstFoundNodeToBe(assignment, TweakedRange(assignmentRange), 10);
			CallFindRelatedNode_AndExpectFirstFoundNodeToBe(assignment, TweakedRange(assignmentRange), 1);

			// Given a slightly skewed range, it should still find the nearest node.
			foreach (var node in new[] { assignment, @if, logWrite, random })
			{
				CallFindRelatedNode_AndExpectFirstFoundNodeToBe(node, TweakedRange(node.Range).With(r => { r.StartIndex += 2; r.EndIndex += 2; }), 10);
				CallFindRelatedNode_AndExpectFirstFoundNodeToBe(node, TweakedRange(node.Range).With(r => { r.StartIndex -= 2; r.EndIndex -= 2; }), 10);
				CallFindRelatedNode_AndExpectFirstFoundNodeToBe(node, TweakedRange(node.Range).With(r => { r.StartIndex += 2; r.EndIndex -= 2; }), 10);
				CallFindRelatedNode_AndExpectFirstFoundNodeToBe(node, TweakedRange(node.Range).With(r => { r.StartIndex -= 2; r.EndIndex += 2; }), 10);
				// We don't need to ask for 10 search results either, not in code this simple
				CallFindRelatedNode_AndExpectFirstFoundNodeToBe(node, TweakedRange(node.Range).With(r => { r.StartIndex += 2; r.EndIndex += 2; }), 1);
				CallFindRelatedNode_AndExpectFirstFoundNodeToBe(node, TweakedRange(node.Range).With(r => { r.StartIndex -= 2; r.EndIndex -= 2; }), 1);
				CallFindRelatedNode_AndExpectFirstFoundNodeToBe(node, TweakedRange(node.Range).With(r => { r.StartIndex += 2; r.EndIndex -= 2; }), 1);
				CallFindRelatedNode_AndExpectFirstFoundNodeToBe(node, TweakedRange(node.Range).With(r => { r.StartIndex -= 2; r.EndIndex += 2; }), 1);
			}
		}
	}
}
