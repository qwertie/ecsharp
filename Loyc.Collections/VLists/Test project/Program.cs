using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Loyc.Utilities;
using System.Collections;

namespace VList
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Running VList unit tests...");
			RunTests.Run(new RWListTests());
			RunTests.Run(new WListTests());
			RunTests.Run(new RVListTests());
			RunTests.Run(new VListTests());
		}

		// Restructures an RVList if it has degraded severely. Time: O(Count)
		void AutoOptimize<T>(ref RVList<T> v)
		{
			// Check if the chain length substantially exceeds Sqrt(v.Count)
			if ((v.BlockChainLength-10) * (v.BlockChainLength-10) > v.Count) {
				RWList<T> w = v.ToRWList();  // This is basically a no-op
				w[0] = w[0];                 // Restructure & make mutable
				v = w.ToRVList();             // Mark immutable again
			}
		}

		// Restructures a VList if it has degraded severely. Time: O(Count)
		void AutoOptimize<T>(ref VList<T> v)
		{
			// Check if the chain length substantially exceeds Sqrt(v.Count)
			if ((v.BlockChainLength-10) * (v.BlockChainLength-10) > v.Count) {
				WList<T> w = v.ToWList();
				int end = w.Count - 1;
				w[end] = w[end];
				v = w.ToVList();
			}
		}
	}
}

/// <summary>Persistent linked list from CodeProject article</summary>
public struct PList<T> : IEnumerable<T>
{
	private PList(PNode<T> n) { _node = n; }
	private PNode<T> _node;

	public PList<T> Add(T item)
	{
		_node = new PNode<T>(item, _node);
		return this;
	}
	public bool IsEmpty
	{
		get { return _node == null; }
	}
	public T Head
	{
		get { return _node.Value; }
	}
	public PList<T> Tail
	{
		get { return new PList<T>(_node.Next); }
	}
	public IEnumerator<T> GetEnumerator()
	{
		PNode<T> n = _node;
		while (n != null)
		{
			yield return n.Value;
			n = n.Next;
		}
	}
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
class PNode<T> // used internally
{
	public PNode(T value, PNode<T> next) { Value=value; Next=next; }
	public readonly T Value;
	public readonly PNode<T> Next;
}
