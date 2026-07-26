using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Graphs;
using Loyc.MiniTest;

namespace Loyc.Essentials.Tests
{
	/// <summary>Regression tests for a batch of long-standing bugs that were fixed
	///   together, and whose subjects have no other natural home among the existing
	///   fixtures (MemoizedTypeName, DictionaryBase, Repeated, ListSourceAsSparse,
	///   GraphMethods and the netstandard2.0 ArrayBufferWriter shim).</summary>
	[TestFixture]
	public class Tier0RegressionTests : TestHelpers
	{
		#region MemoizedTypeName

		class OuterGeneric<T> { public class Inner { } }

		[Test]
		public void MemoizedTypeNameHandlesNonGenericNestedInGeneric()
		{
			// Regression: a non-generic type nested inside a generic type reports
			// IsGenericType == true, but its Name has no `arity suffix, so
			// LastIndexOf('`') returned -1 and Substring(0, -1) threw.
			var name = MemoizedTypeName.Get(typeof(OuterGeneric<int>.Inner));
			Assert.IsNotNull(name);
			Assert.IsTrue(name!.StartsWith("Inner"), "unexpected name: " + name);

			// Ordinary cases are unaffected
			Assert.AreEqual("Int32", MemoizedTypeName.Get(typeof(int)));
			Assert.AreEqual("Dictionary<Int32, String>", MemoizedTypeName.Get(typeof(Dictionary<int, string>)));
			Assert.IsNull(MemoizedTypeName.Get(null));

			// Memoized: the same string instance comes back the second time (non-generic
			// results are now cached too, which they previously were not)
			Assert.IsTrue(object.ReferenceEquals(MemoizedTypeName.Get(typeof(int)), MemoizedTypeName.Get(typeof(int))));
			Assert.IsTrue(object.ReferenceEquals(MemoizedTypeName.Get(typeof(OuterGeneric<int>.Inner)),
			                              MemoizedTypeName.Get(typeof(OuterGeneric<int>.Inner))));
		}

		#endregion

		#region DictionaryBase.IsEmpty

		[Test]
		public void DictionaryBaseIsEmptyIsNotInverted()
		{
			// Regression: DictionaryBase.IsEmpty was `Count != 0`.
			var dict = new WeakValueDictionary<string, string>();
			Assert.AreEqual(0, dict.Count);
			Assert.IsTrue(dict.IsEmpty);

			dict["k"] = "v";
			Assert.AreEqual(1, dict.Count);
			Assert.IsFalse(dict.IsEmpty);

			dict.Remove("k");
			Assert.IsTrue(dict.IsEmpty);
		}

		#endregion

		#region Repeated<T>

		[Test]
		public void RepeatedContainsHonorsCount()
		{
			// Regression: Contains ignored _count, so a zero-length Repeated<T> claimed
			// to contain the repeated value.
			var empty = ListExt.Repeat(5, 0);
			Assert.AreEqual(0, empty.Count);
			Assert.IsFalse(empty.Contains(5));
			Assert.IsFalse(empty.Contains(0));

			var three = ListExt.Repeat(5, 3);
			Assert.IsTrue(three.Contains(5));
			Assert.IsFalse(three.Contains(6));

			// Empty<T>.List is a zero-length Repeated under the hood
			Assert.IsFalse(((IContains<int>)Empty<int>.List).Contains(0));
		}

		[Test]
		public void RepeatedPopLastDoesNotRecurseForever()
		{
			// Regression: PopLast(out fail) called itself unconditionally, causing an
			// uncatchable StackOverflowException.
			var r = ListExt.Repeat(7, 2);
			bool fail;
			Assert.AreEqual(7, r.PopLast(out fail));
			Assert.IsFalse(fail);
			Assert.AreEqual(1, r.Count);
			Assert.AreEqual(7, r.PopLast(out fail));
			Assert.IsFalse(fail);
			Assert.AreEqual(0, r.Count);
			r.PopLast(out fail);
			Assert.IsTrue(fail);
		}

		#endregion

		#region ListSourceAsSparse

		[Test]
		public void ListSourceAsSparseGetEnumeratorDoesNotRecurse()
		{
			// Regression: the `new` GetEnumerator() returned GetEnumerator() -- itself,
			// because the return type is not part of the signature -- so any use blew
			// the stack.
			IListSource<int> source = new List<int> { 10, 20, 30 }.AsListSource();
			var sparse = source.AsSparse();

			var viaInterface = new List<int>();
			foreach (int x in (IEnumerable<int>)sparse)
				viaInterface.Add(x);
			ExpectList(viaInterface, 10, 20, 30);

			// The non-generic enumerator is the one that used to recurse
			var e = sparse.GetEnumerator();
			var items = new List<object?>();
			while (e.MoveNext())
				items.Add(e.Current);
			Assert.AreEqual(3, items.Count);
		}

		#endregion

		#region ArrayBufferWriter compatibility shim

		[Test]
		public void ArrayBufferWriterGetSpanZeroNeverReturnsEmpty()
		{
			// Regression (netstandard2.0 / .NET Framework shim only): GetSpan(0) returned
			// an EMPTY span once the buffer happened to be exactly full, so the standard
			// IBufferWriter loop (GetSpan/Advance until done) spun forever. The BCL bumps
			// a sizeHint of 0 up to 1.
			// (the net472 test project defines DotNet45 rather than NETFRAMEWORK)
			#if NETSTANDARD2_0 || NETFRAMEWORK || DotNet45
			var w = new Loyc.Compatibility.ArrayBufferWriter<byte>(4);
			w.GetSpan(4);
			w.Advance(4);           // buffer is now exactly full
			Assert.AreEqual(0, w.FreeCapacity);
			Assert.IsTrue(w.GetSpan(0).Length > 0, "GetSpan(0) returned an empty span when full");
			Assert.IsTrue(w.GetMemory(0).Length > 0, "GetMemory(0) returned empty memory when full");

			// A fresh (zero-capacity) writer must also hand out space for sizeHint 0
			var w2 = new Loyc.Compatibility.ArrayBufferWriter<byte>();
			Assert.IsTrue(w2.GetSpan(0).Length > 0);
			#else
			// On .NET 6 the BCL's own ArrayBufferWriter is used and the shim is not
			// compiled, so there is nothing to test here.
			Assert.IsTrue(true);
			#endif
		}

		#endregion

		#region GraphMethods inbound traversal

		class GNode : Loyc.Graphs.INode<IEnumerable<GEdge>>
		{
			public string Name;
			public List<GEdge> Out = new List<GEdge>();
			public List<GEdge> In = new List<GEdge>();
			public GNode(string name) { Name = name; }
			public IEnumerable<GEdge> Outbound => Out;
			public IEnumerable<GEdge> Inbound => In;
			public bool HasInbound => true;
			public override string ToString() => Name;
		}
		class GEdge : Loyc.Graphs.IEdge<GNode>
		{
			public GNode F, T;
			public GEdge(GNode f, GNode t) { F = f; T = t; }
			public GNode From => F;
			public GNode To => T;
			public float Cost => 1;
			public override string ToString() => F.Name + "->" + T.Name;
		}

		static void Link(GNode from, GNode to)
		{
			var e = new GEdge(from, to);
			from.Out.Add(e);
			to.In.Add(e);
		}

		[Test]
		public void ScanComponentFollowsInboundEdgesUpstream()
		{
			// Regression: the inbound loop recursed on edge.To, which for an INBOUND edge
			// is the current node itself, so upstream nodes were never visited and the
			// scan stopped one edge short.
			// Graph:  a -> b -> c        (start the scan at c and walk upstream)
			var a = new GNode("a");
			var b = new GNode("b");
			var c = new GNode("c");
			Link(a, b);
			Link(b, c);

			var edges = c.ScanComponent<GNode, GEdge>();

			// Starting at c, the inbound walk must reach BOTH b->c and a->b
			Assert.AreEqual(2, edges.Count,
				"upstream edges were not all discovered; found: " + string.Join(", ", edges.Select(e => e.ToString())));
		}

		#endregion
	}
}
