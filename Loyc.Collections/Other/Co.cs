using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;

namespace Loyc.Utilities
{
	/// <summary>A structure that helps you to write coroutines, or to avoid the 
	/// performance penalty of nested iterators.</summary>
	/// <remarks>
	/// This data type helps you solve the performance problem with using "yield
	/// return" recursively.
	/// <para/>
	/// To illustrate the problem, let's consider the case of binary tree traversal. 
	/// Suppose you define this data structure to hold a subtree of a sorted binary 
	/// tree of strings:
	/// <code>
	/// internal class StringNode : IEnumerable&lt;string>
	/// {
	///     internal StringNode LeftChild;
	///     internal string Value;
	///     internal StringNode RightChild;
	/// }
	/// </code>
	/// You want to write an enumerator that returns all the strings in order.
	/// So you add this method to the StringNode class:
	/// <code>
	/// public IEnumerator&lt;string> GetEnumerator()
	/// {
	/// 	foreach(string item in LeftChild)
	/// 		yield return item;
	/// 	yield return Value;
	/// 	foreach(string item in RightChild)
	/// 		yield return item;
	/// }
	/// </code>
	/// As explained in <a href="http://stackoverflow.com/questions/3969963/when-not-to-use-yield-return">
	/// this web page</a>, this implementation will be slow, and it will get slower 
	/// and slower as the tree gets deeper.
	/// <para/>
	/// <see cref="Co{T}"/> (Co is short for "coroutine") helps to solve this 
	/// problem with a postprocessing stage. Instead of writing GetEnumerator()
	/// directly as above, you write it like this instead:
	/// <code>
	/// public IEnumerator&lt;string> GetEnumerator()
	/// {
	/// 	return CoGetEnumerator().Unwrap();
	/// }
	/// public IEnumerator&lt;Co&lt;string>> CoGetEnumerator()
	/// {
	///		yield return LeftChild.All();
	/// 	yield return Value;
	/// 	yield return RightChild.All();
	/// }
	/// </code>
	/// Simple, right? String is automatically converted to Co{string} so that you 
	/// can "yield return" one, but C# does not allow automatic coersion from 
	/// IEnumerator{Co{T}} to Co{T} so you must add the suffix <c>.All()</c> which
	/// really means "wrap the IEnumerator{Co{T}} into a Co{T} object".
	/// <para/>
	/// In fact, this class simulates the core functionality of C# 5's async/await 
	/// feature just using C# 2's iterators. However, the C# 5 async feature is 
	/// richer; for example, it supports exceptions properly. If an exception is
	/// thrown from within a nested iterator, the outer iterator is unable to catch
	/// the exception; it will be thrown directly into whatever foreach loop (or
	/// other code) that is enumerating the sequence.
	/// <para/>
	/// This class solves the same problem as <see cref="NestedEnumerator{F,T}"/> in
	/// a way that is very easy to use. You should use <see cref="NestedEnumerator{F,T}"/>
	/// instead if you need the highest possible performance.
	/// </remarks>
	/// <typeparam name="T">Type of item being enumerated.</typeparam>
	public struct Co<T> : IEnumerable<T>
	{
		internal IEnumerator<Co<T>> _e;
		internal T _value;
		public Co(IEnumerator<Co<T>> e) { _e = e; _value = default(T); }
		public Co(T value)              { _e = null; _value = value; }
		public static implicit operator Co<T>(T e) { return new Co<T>(e); }

		/// <summary>Enumerates the underlying sequence.</summary>
		public IEnumerator<T> GetEnumerator()
		{
			if (_e != null) {
				var top = _e;
				for (InternalList<IEnumerator<Co<T>>> stack = InternalList<IEnumerator<Co<T>>>.Empty; ; ) {
					while (top.MoveNext()) {
						var next = top.Current;
						if (next._e != null) {
							stack.Add(top);
							top = next._e;
						} else {
							yield return next._value;
						}
					}
					if (stack.IsEmpty)
						break;
					top = stack.Last;
					stack.Pop();
				}
			} else
				yield return _value;
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
	/// <summary>Extension methods for <see cref="Co{T}"/>.</summary>
	public static class Co
	{
		public static Co<T> All<T>(this IEnumerator<Co<T>> sequence) 
			{ return new Co<T>(sequence); }
		public static IEnumerator<T> Unwrap<T>(this IEnumerator<Co<T>> sequence) 
			{ return new Co<T>(sequence).GetEnumerator(); }
	}
}
