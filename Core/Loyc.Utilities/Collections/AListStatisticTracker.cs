using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Collections
{
	/// <summary>Base class that helps efficiently keep track of statistics about the 
	/// contents of one or more <see cref="AListBase{K, T}"/> objects (including derived 
	/// classes such as AList, BList, BDictionary and BMultiMap). Usually you'll use one of
	/// the derived classes instead, e.g. <see cref="AListSumTracker{K,T}"/>.</summary>
	/// <typeparam name="K">Key type of the AListBase</typeparam>
	/// <typeparam name="T">Value type of the AListBase</typeparam>
	/// <typeparam name="TSummary">Type of value that will be derived from the contents of the list(s)</typeparam>
	/// <remarks>
	/// This class keeps track of a "summary" of each node in each attached AList.
	/// It is assumed that there is some way to combine ("aggregate") summaries.
	/// For example, one common "summary" is the sum of a list of items, and 
	/// multiple summaries can be combined simply by adding them.
	/// <para/>
	/// This class can observe multiple lists at once and combine their results.
	/// </remarks>
	public abstract class AListStatisticTrackerBase<K, T, TSummary> : IAListTreeObserver<K, T>
	{
		Dictionary<AListNode<K, T>, TSummary> _summaries = new Dictionary<AListNode<K, T>, TSummary>();
		Dictionary<AListBase<K, T>, AListNode<K, T>> _roots = new Dictionary<AListBase<K, T>, AListNode<K, T>>();
		Dictionary<AListNode<K, T>, AListInnerBase<K, T>> _parents = new Dictionary<AListNode<K, T>, AListInnerBase<K, T>>();
		bool _hasCachedSummary;
		TSummary _cachedSummary;

		public IEnumerable<AListBase<K, T>> AttachedLists => _roots.Keys;
		public int TotalCountInAttachedLists => _roots.Sum(pair => pair.Key.Count);

		protected abstract TSummary Summarize(AListLeafBase<K,T> data);
		protected abstract TSummary Aggregate(TSummary[] data);
		protected virtual TSummary EmptyResult => Aggregate(EmptyArray<TSummary>.Value);

		public TSummary Summary => GetSummary();
		public TSummary GetSummary()
		{
			if (_hasCachedSummary)
				return _cachedSummary;
			
			_hasCachedSummary = true;
			int count = _roots.Count;
			if (count == 0)
				return _cachedSummary = EmptyResult;
			if (count == 1)
				return _cachedSummary = GetSummary(_roots.First().Value);
			
			TSummary[] summaries = new TSummary[_roots.Count];
			int i = 0;
			foreach (var pair in _roots)
				summaries[i++] = GetSummary(pair.Value);
			return _cachedSummary = Aggregate(summaries);
		}

		protected TSummary GetSummary(AListNode<K, T> node)
		{
			if (node == null)
				return EmptyResult;
			if (_summaries.TryGetValue(node, out var summary))
				return summary;
			
			if (node is AListInnerBase<K, T> inner)
			{
				TSummary[] list = new TSummary[inner.LocalCount];
				for (int i = 0; i < list.Length; i++) {
					var child = inner.Child(i);
					_parents[child] = inner;
					list[i] = GetSummary(child);
				}
				summary = Aggregate(list);
			}
			else
			{
				summary = Summarize((AListLeafBase<K,T>) node);
			}
			return _summaries[node] = summary;
		}

		#region IAListTreeObserver implementation

		void IAListTreeObserver<K, T>.AddAll(AListNode<K, T> node) { }

		bool? IAListTreeObserver<K, T>.Attach(AListBase<K, T> list) => null;
		void IAListTreeObserver<K, T>.Detach(AListBase<K, T> list, AListNode<K, T> root)
		{
			if (_roots.TryGetValue(list, null) is AListInnerBase<K,T>) {
				// Avoid a memory leak
				_parents.Clear();
				_summaries.Clear();
			}
			_roots.Remove(list);
			
		}
		void IAListTreeObserver<K, T>.RootChanged(AListBase<K, T> list, AListNode<K, T> newRoot, bool clear)
		{
			_roots[list] = newRoot;
			if (clear) {
				_hasCachedSummary = false;
				_cachedSummary = default(TSummary);
				_summaries.Clear();
			}
		}

		void IAListTreeObserver<K, T>.CheckPoint() { }

		void Forget(AListNode<K, T> node)
		{
			while (true) {
				_summaries.Remove(node);
				if (_parents.TryGetValue(node, out var parent)) {
					_parents.Remove(node);
					node = parent;
				} else
					break;
			}
			_hasCachedSummary = false;
		}
		void IAListTreeObserver<K, T>.ItemAdded(T item, AListLeafBase<K, T> parent) => Forget(parent); 
		void IAListTreeObserver<K, T>.ItemRemoved(T item, AListLeafBase<K, T> parent) => Forget(parent); 

		void IAListTreeObserver<K, T>.NodeAdded(AListNode<K, T> child, AListInnerBase<K, T> parent) => Forget(parent); 
		void IAListTreeObserver<K, T>.NodeRemoved(AListNode<K, T> child, AListInnerBase<K, T> parent) => Forget(parent); 

		void IAListTreeObserver<K, T>.RemoveAll(AListNode<K, T> node) => Forget(node); 

		#endregion
	}

	/// <summary>This class efficiently lets you keep track of simple commutative statistics 
	/// (such as total, average, sum of squares, and min/max) derived from the items of 
	/// <see cref="AList{T}"/> and its variants (BList, BDictionary, and BMultiMap).</summary>
	/// <remarks>
	/// Note: this class relies on the "tree observer" feature of ALists, but SparseAList 
	/// doesn't support tree observers as of 2020/01.
	/// <para/>
	/// Typically a statistic tracker is created by calling an extension method on AList, BList, etc.
	/// </remarks>
	public class AListStatisticTracker<K,T,TSummary> : AListStatisticTrackerBase<K,T,TSummary>
	{
		Func<T, TSummary> _selector;
		Func<TSummary[], TSummary> _aggregator;
		TSummary _emptyResult;

		public AListStatisticTracker(Func<T, TSummary> selector, Func<TSummary[], TSummary> aggregator, TSummary emptyResult, AListBase<K,T> list = null)
		{
			_selector = selector;
			_aggregator = aggregator;
			_emptyResult = emptyResult;
			list?.AddObserver(this);
		}

		public AListStatisticTracker(Func<T, TSummary> selector, Func<TSummary, TSummary, TSummary> aggregator, TSummary emptyResult, AListBase<K,T> list = null)
		{
			_selector = selector;
			_emptyResult = emptyResult;
			_aggregator = array => {
				if (array.Length == 0)
					return _emptyResult;
				var total = array[0];
				for (int i = 1; i < array.Length; i++)
					total = aggregator(total, array[i]);
				return total;
			};
			list?.AddObserver(this);
		}

		protected override TSummary EmptyResult => _emptyResult;
		protected override TSummary Aggregate(TSummary[] data) => _aggregator(data);
		protected override TSummary Summarize(AListLeafBase<K, T> data)
		{
			IReadOnlyList<T> data2 = data;
			var array = new TSummary[data2.Count];
			if (array.Length == 0)
				return _emptyResult; // unusual
			for (int i = 0; i < array.Length; i++)
				array[i] = _selector(data2[i]);
			return _aggregator(array);
		}
	}

	/// <summary>This class incrementally updates a <see cref="Statistic"/> object based on
	/// changes to <see cref="AList{T}"/> and its variants (BList, BDictionary, and BMultiMap).</summary>
	public class AListStatisticTracker<K,T> : AListStatisticTrackerBase<K,T,Statistic>
	{
		Func<T, double> _selector;

		public AListStatisticTracker(Func<T, double> selector) => _selector = selector;
		public AListStatisticTracker(Func<T, double> selector, AListBase<K, T> list) : this(selector) => list?.AddObserver(this);

		public Statistic Statistic => Summary;

		protected override Statistic Aggregate(Statistic[] data) => Statistic.Merge(data);

		protected override Statistic Summarize(AListLeafBase<K, T> data)
		{
			var stat = new Statistic();
			foreach (T value in data)
				stat.Add(_selector(value));
			return stat;
		}
	}
	
	public class AListStatisticTracker<T> : AListStatisticTracker<int, T>
	{
		public AListStatisticTracker(Func<T, double> selector) : base(selector) { }
		public AListStatisticTracker(Func<T, double> selector, AListBase<int, T> list) : base(selector, list) { }
	}

	/// <summary>This class incrementally recomputes the sum of an <see cref="AList{T}"/> 
	/// (or its variants - BList, BDictionary, and BMultiMap).</summary>
	public class AListSumTracker<K,T> : AListStatisticTracker<K,T,double>
	{
		static Func<double[], double> SumFunction = array => Enumerable.Sum(array);
		
		public AListSumTracker(Func<T, double> selector, AListBase<K,T> list = null) : base(selector, SumFunction, 0.0, list) { }
		
		public double Sum => Summary;
		public double Average => Summary / TotalCountInAttachedLists;
	}
	
	public class AListSumTracker<T> : AListSumTracker<int,T>
	{
		static Func<double[], double> SumFunction = array => Enumerable.Sum(array);
		
		public AListSumTracker(Func<T, double> selector, AListBase<int, T> list = null) : base(selector, list) { }
	}
}
