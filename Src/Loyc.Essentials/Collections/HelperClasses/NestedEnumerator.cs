using System;
using System.Collections.Generic;
using Loyc.Collections;
using Loyc.Collections.Impl;
using System.Collections;

namespace Loyc.Collections
{
	/// <summary>Helper class. An enumerator that helps enumerate tree data structures. 
	/// It maintains a virtual call stack that avoids the performance hit of using
	/// nested "yield return" statements in C#.</summary>
	/// <typeparam name="Frame">Frame data structure; represents the current 'depth' 
	/// in a tree data structure, or the current 'stack frame' on a virtual stack.
	/// Typically, this parameter will either be <see cref="EnumeratorFrame{T}"/> or 
	/// a struct that implements <see cref="IEnumeratorFrame{Frame, T}"/>.</typeparam>
	/// <typeparam name="T">Item data type returned by the enumerator.</typeparam>
	/// <remarks>
	/// This data type helps you solve the performance problem with using "yield
	/// return" recursively ().
	/// <para/>
	/// To illustrate how to use <see cref="NestedEnumerator{Frame,T}"/> let's 
	/// consider the case of binary tree traversal. Suppose you define this data
	/// structure to hold a subtree of a sorted binary tree of strings:
	/// <code>
	/// internal class StringNode : IEnumerable&lt;string>
	/// {
	///     internal StringNode LeftChild;
	///     internal string Value;
	///     internal StringNode RightChild;
	///     public IEnumerator&lt;string> GetEnumerator() { ... }
	/// }
	/// </code>
	/// You want to write an enumerator that returns all the strings in order.
	/// So you write this method in the StringNode class:
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
	/// NestedEnumerator helps to solve this problem using a "virtual stack". It 
	/// keeps track of all the nested enumerators and always returns values from 
	/// the current, deepest enumerator. The enumerator objects are called "frames",
	/// because they are "stack frames" on the virtual stack.
	/// <para/>
	/// Each enumerator implements <see cref="IEnumeratorFrame{Frame,T}"/> instead
	/// of just <see cref="IEnumerator{T}"/>. A normal enumerator can only do one
	/// of two actions on each call to MoveNext(): it can return a T value, or stop.
	/// But an <c>IEnumeratorFrame</c> can do one of three things: it can return a T
	/// value, it can stop, or it can return a new (child) stack frame. These actions
	/// are represented by <c>MoveNext</c> return values of 1, 0, and -1 respectively.
	/// <para/>
	/// You cannot use <c>yield return</c> with <c>NestedEnumerator</c> because it 
	/// is not supported by the C# compiler, so using <c>NestedEnumerator</c> 
	/// requires more developer effort. Here's how the GetEnumerator() method can 
	/// be implemented using <c>NestedEnumerator</c>:
	/// <code>
	/// public static IEnumerator&lt;string> GetEnumerator()
	/// {
	/// 	return new NestedEnumerator&lt;Frame, string>(new Frame(this));
	/// }
	/// struct Frame : IEnumeratorFrame&lt;Frame, string>
	/// {
	///     StringNode self;
	///     int step;
	///     public Frame(StringNode self) { _self = self; step = 0; }
	///     
	///     int MoveNext(ref Frame frame, ref T current)
	///     {
	///         switch(++step) {
	///             case 1: frame = new Frame(self.LeftChild);  return -1;
	///             case 2: current = Value;                    return 1;
	///             case 3: frame = new Frame(self.RightChild); return -1;
	///         }
	///         return 0;
	///     }
	/// }
	/// </code>
	/// The <c>NestedEnumerator</c> takes care of managing the stack and invoking 
	/// <c>MoveNext</c> on the deepest instance of <c>IEnumeratorFrame</c>.
	/// <para/>
	/// <see cref="IEnumeratorFrame{Frame,T}"/> is an unusual interface that 
	/// requires <c>Frame</c> to be derived from the interface itself. The purpose 
	/// of this design is to allow the <c>Frame</c> data type to be a struct,
	/// which allows the virtual stack to consist of value types (structs), which
	/// improves performance because a new object does not have to be allocated on
	/// the heap for every stack frame. Also, if <c>Frame</c> is a struct, 
	/// NestedEnumerator can call <see cref="IEnumeratorFrame{Frame,T}.MoveNext"/>
	/// directly, rather than via interface dispatch, which also improves 
	/// performance.
	/// </remarks>
	public struct NestedEnumerator<Frame, T> : IEnumerator<T>
		where Frame : IEnumeratorFrame<Frame, T>
	{
		T _current;
		Frame _frame;
		InternalList<Frame> _stack;
		
		public NestedEnumerator(Frame root) {
			_current = default(T);
			_frame = root;
			_stack = InternalList<Frame>.Empty;
		}

		public T Current
		{
			get { return _current; }
		}
		public bool MoveNext()
		{
			for (;;) {
				var child = _frame;
				int result = _frame.MoveNext(ref child, ref _current);
				if (result == 1)
					return true;
				if (result == 0) {
					// if possible, pop a frame off the stack and continue
					if (_stack.IsEmpty)
						return false;
					_frame = _stack.Last;
					_stack.Pop();
				} else {
					// push child frame and continue
					_stack.Add(_frame);
					_frame = child;
				}
			}
		}
		object System.Collections.IEnumerator.Current { get { return _current; } }
		void System.Collections.IEnumerator.Reset() { throw new NotSupportedException(); }
		void IDisposable.Dispose() {}
	}

	/// <summary>Helper type. You pass a cloneable <c>Frame</c> object to the 
	/// constructor, and then a copy of this <c>Frame</c> is used to construct a
	/// new <see cref="NestedEnumerator{Frame,T}"/> each time the user calls 
	/// <see cref="GetEnumerator"/>.</summary>
	public struct NestedEnumerable<Frame, T> : IEnumerable<T>
		where Frame : IEnumeratorFrame<Frame, T>, ICloneable<Frame>
	{
		Frame _root;
		public NestedEnumerable(Frame root) { _root = root; }
		
		IEnumerator    IEnumerable.GetEnumerator()    { return GetEnumerator(); }
		IEnumerator<T> IEnumerable<T>.GetEnumerator() { return GetEnumerator(); }
		public NestedEnumerator<Frame, T> GetEnumerator()
		{
			return new NestedEnumerator<Frame, T>(_root.Clone());
		}
	}
}

namespace Loyc.Collections.Impl
{
	/// <summary>Helper interface for <see cref="NestedEnumerator{Frame, T}"/>.</summary>
	/// <typeparam name="Frame">A data type that implements this interface.</typeparam>
	/// <typeparam name="T">Type of items enumerated by this interface.</typeparam>
	public interface IEnumeratorFrame<Frame, T> where Frame : IEnumeratorFrame<Frame, T>
	{
		/// <summary>Enumerates the next item, or the next child collection.</summary>
		/// <param name="frame">Current frame (on entry, 'frame' is the same as 'this').
		/// To begin enumerating a child frame, this method must change 'frame' to the
		/// desired child frame and return -1.</param>
		/// <param name="current">Current item (on entry, 'current' is value most 
		/// recently enumerated by <see cref="NestedEnumerator{Frame,T}"/>.) To 
		/// enumerate an item, MoveNext must change the value of 'current' and 
		/// return 1.</param>
		/// <returns>1 to enumerate 'current', 0 when there are no more items in 
		/// this frame's sequence, and any other value to enumerate a child frame.</returns>
		int MoveNext(ref Frame frame, ref T current);
	}
	
	/// <summary>A standard base class for enumerator frames used by 
	/// <c>NestedEnumerator{EnumeratorFrame{T},T}</c>.</summary>
	/// <typeparam name="T">Type of items enumerated by this class.</typeparam>
	/// <remarks>
	/// This base class should be used whenever more than one type of frame
	/// will be present on <see cref="NestedEnumerator{Frame,T}"/>'s internal stack 
	/// of frames.
	/// <para/>
	/// If the enumerator will only use a single type of frame, then
	/// (in some cases) the frame data type can be a 'struct' to achieve higher 
	/// performance. But in that case, this class cannot be used.</remarks>
	public abstract class EnumeratorFrame<T> : IEnumeratorFrame<EnumeratorFrame<T>, T>
	{
		public abstract int MoveNext(ref EnumeratorFrame<T> frame, ref T current);
	}
}
