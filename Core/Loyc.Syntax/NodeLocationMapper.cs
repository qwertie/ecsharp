using Loyc.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Math;

namespace Loyc.Syntax
{
	/// <summary>Helps map locations (such as error locations) from a file (usually an 
	/// output file) to an <see cref="LNode"/> (see Remarks).</summary>
	/// <remarks>
	/// A common scenario is that you've written code in Enhanced C# and then sent it 
	/// through LeMP to produce an output file. The C# compiler produces error messages 
	/// with locations in the output file, but you would like to see what part of the
	/// original Enhanced C# file is associated with those errors. The LeMP Visual 
	/// Studio extension could use this class to help obtain this information.
	/// <para/>
	/// This requires (1) keeping track of the relationship between the output file
	/// and the original list of <see cref="LNode"/>s that contain source locations, and
	/// (2) an algorithm to map error locations back to source nodes and locations.
	/// Feature (1) is provided by individual node printers and can be requested via
	/// <see cref="ILNodePrinterOptions.SaveRange"/>. This class provides Feature (2).
	/// In addition, locations provided by a compiler tend to be line/column pairs
	/// instead of indexes, so (3) an <see cref="ILineToIndex"/> implementation is
	/// needed to convert line/column pairs to indexes.
	/// <para/>
	/// Mapping back to a source location is trickier than mapping back to a source 
	/// node, mainly because transformations (such as those done by LeMP) can 
	/// introduce synthetic nodes, duplicate nodes, and nodes from other files. 
	/// The node that best matches the output location might be something that has
	/// no location in the current source file, or no location in any source file.
	/// </remarks>
	public class LNodeRangeMapper
	{
		// These dictionaries map locations in a file to input nodes
		public BMultiMap<int, Pair<ILNode, int>> _startLocations = new BMultiMap<int, Pair<ILNode, int>>(null, (p, q) => p.B.CompareTo(q.B));
		public BMultiMap<int, Pair<ILNode, int>> _endLocations   = new BMultiMap<int, Pair<ILNode, int>>(null, (p, q) => p.B.CompareTo(q.B));

		/// <summary>Associates a node with a range.</summary>
		/// <remarks>Typically this method is set as the value of 
		/// <see cref="ILNodePrinterOptions.SaveRange"/> so that range info is captured while converting a node to text.</remarks>
		public void SaveRange(ILNode node, IndexRange range)
		{
			_startLocations[range.StartIndex].Add(Pair.Create(node, range.EndIndex));
			_endLocations  [range.EndIndex].Add(Pair.Create(node, range.StartIndex));
		}

		/// <summary>Gets the number of ranges that were stored with <see cref="SaveRange"/>.</summary>
		public int SavedRangeCount => _startLocations.Count;

		/// <summary>Gets a list of nodes close to the requested range, ordered by similarity 
		/// so that the first item in the returned list is the best result.</summary>
		/// <returns>A list of nodes and their ranges, sorted by similarity to the targetRange
		/// (most similar first), and limited in size as requested. The nodes and ranges must
		/// have been previously saved via <see cref="SaveRange(ILNode, IndexRange)"/>.</returns>
		public IReadOnlyList<Pair<ILNode, IndexRange>> FindRelatedNodes(IndexRange targetRange, int maxSearchResults) =>
			FindRelatedNodesCore(targetRange, maxSearchResults, true);

		/// <summary>Selects the "closest" node to the target range, under the constraint 
		/// that, if possible, you want to find a node belonging to a particular file and
		/// with a valid (non-negative) StartIndex. If there are no good results, this 
		/// method will return a poor result instead (e.g. a node from a different file).</summary>
		/// <returns>The best node and its range in the file being searched (not sourceFile, but
		/// the file for which ranges were saved via <see cref="SaveRange(ILNode, IndexRange)"/>.)</returns>
		public Pair<ILNode, IndexRange> FindMostUsefulRelatedNode(IndexRange targetRange, ISourceFile sourceFile, int searchIntensity = 10)
		{
			var list = FindRelatedNodesCore(targetRange, searchIntensity, false);
			var inThisFile = list.Where(p => p.A.Range.Source.FileName == sourceFile.FileName);
			return inThisFile.Where(p => p.A.Range.StartIndex >= 0).Concat(inThisFile).Concat(list).FirstOrDefault();
		}

		public IReadOnlyList<Pair<ILNode, IndexRange>> FindRelatedNodesCore(IndexRange targetRange, int maxSearchResults, bool trim)
		{
			var candidates = new Dictionary<Pair<ILNode, IndexRange>, float>();

			// This is a heuristic search, not an algorithm; it is not guaranteed to find the
			// best answer, rather we scan enough entries to find what we're looking for with 
			// high probability. In total, scan about 10x as many entries as maxSearchResults.
			Scan(_startLocations, targetRange.StartIndex, targetRange, 1, maxSearchResults * 5, candidates);
			Scan(_startLocations, targetRange.StartIndex, targetRange, -1, maxSearchResults * 2, candidates);
			Scan(_endLocations, targetRange.EndIndex, targetRange, 1, maxSearchResults * 1, candidates);
			Scan(_endLocations, targetRange.EndIndex, targetRange, -1, maxSearchResults * 2, candidates);

			var sorted = new BMultiMap<float, Pair<ILNode, IndexRange>>(null, (p, q) => 0);
			foreach (var candidate in candidates)
			{
				if (!trim || sorted[candidate.Value].StartIndex < maxSearchResults)
					sorted[candidate.Value].Add(candidate.Key);
			}
			if (trim && sorted.Count > maxSearchResults)
				sorted.RemoveRange(maxSearchResults, sorted.Count - maxSearchResults);
			return sorted.Select(pair => pair.Value).ToList();
		}

		private static void Scan(BMultiMap<int, Pair<ILNode, int>> locations, int charIndex, IndexRange targetRange, int scanDirection, int iterations, Dictionary<Pair<ILNode, IndexRange>, float> results)
		{
			for (int i = locations.FindLowerBound(charIndex); iterations > 0; iterations--, i += scanDirection)
			{
				var pair = locations.TryGet(i, out bool fail);
				if (!fail)
				{
					var nodeWithRange = Pair.Create(pair.Value.Item1, RangeOf(pair));
					float score = GetMismatchScore(pair, targetRange);
					if (score < results.TryGetValue(nodeWithRange, float.MaxValue))
						results[nodeWithRange] = score;
				}
			}
		}
		
		/// <summary>Scores the extent to which a location-table item does NOT match the source file.</summary>
		private static float GetMismatchScore(KeyValuePair<int, Pair<ILNode, int>> pair, IndexRange targetRange)
		{
			var range = RangeOf(pair);
			int offs1 = range.StartIndex - targetRange.StartIndex;
			int offs2 = range.EndIndex - targetRange.EndIndex;
			bool terrible = !range.Overlaps(targetRange) && !range.Contains(targetRange);
			// Prefer ranges closer to the start, rather than the end, of the target range
			return (terrible ? 1_000_000 : 0) + Abs(offs1) * 4f + Abs(offs2);
		}
		
		private static IndexRange RangeOf(KeyValuePair<int, Pair<ILNode, int>> pair)
			=> new IndexRange(pair.Key) { EndIndex = pair.Value.Item2 }.Normalize();
	}
}
