using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>A forward range. Allows you to read the first element from the 
	/// range or skip it. The forward range lays the foundation for
	/// <see cref="IBRange{T}"/> and <see cref="IRange{T}"/>.</summary>
	/// <remarks>
	/// Ranges are a concept I first saw formalized in the D programming language.
	/// They are generally more useful than .NET enumerators because there are 
	/// more kinds of them:
	/// <ul>
	/// <li>A forward range (<see cref="IFRange{T}"/>) is a sequence of values 
	/// accessed starting with the first, like <see cref="IEnumerator{T}"/>. A 
	/// forward range is like IEnumerator, except that it can be cloned, so you 
	/// can restart from the same place later. This is more powerful than 
	/// <see cref="IEnumerable{T}"/>, which can only restart from the beginning.</li>
	/// <li>A bidirectional range inherits from the forward range, and provides
	/// access to the back of the sequence as well as the front. A bidirectional
	/// iterators may, for example, represent a linked list, or a UTF-8 string 
	/// that provides access to the first and last 32-bit code point (note that
	/// random access by character index is not really possible in UTF-8 data,
	/// since the size of N characters is unknown).</li>
	/// <li>A random-access range is a sequence that provides array-like access
	/// to a section of a list. A section of a list is called a "slice".</li>
	/// </ul>
	/// A range is read-only by default, but has a writable variant marked
	/// with the letter M, meaning "Mutable": <see cref="IMFRange{T}"/>, 
	/// <see cref="IMBRange{T}"/>, <see cref="IMRange{T}"/>.
	/// <para/>
	/// It is fair to ask why IFRange exists--since it behaves like an enumerator,
	/// why not simply use IEnumerator directly? Well, this interface serves as 
	/// the base interface for the other ranges, so its interface needs to make
	/// sense in that context. IEnumerator has "Current" and "MoveNext" methods,
	/// but in a bidirectional or random-access range, there is no single 
	/// "Current" or "Next" item.
	/// <para/>
	/// Also, when used through an interface, IFRange is potentially more 
	/// efficient than IEnumerator: you only need to call a single method, 
	/// PopFirst(), to get the next item, unlike IEnumerator which requires two 
	/// interface calls per iteration. That can improve performance, since 
	/// interface calls cannot be inlined. It is a bit inconvenient to use
	/// <see cref="PopFirst(out bool)"/> because of its "out" argument, and 
	/// more convenient extension methods would have been provided if C# 
	/// supported "ref Type this", which would be needed since ranges are
	/// often value types.
	/// <para/>
	/// Ranges are useful for implementing algorithms; they are comparable to
	/// the concept of "iterators" in C++, except that a range represents a 
	/// pair of iterators instead of a single iterator. And like C++ iterators,
	/// they are a useful starting point for writing generic algorithms.
	/// <para/>
	/// When using a range that is not typed <see cref="IFRange{T}"/>, you need to
	/// be careful how you use it because the range could be a struct type. Much 
	/// like an enumerator, a range is often a small wrapper around a larger data
	/// structure; therefore, it often makes sense to implement a range as a 
	/// struct. When a range is a struct, normally you are making a copy of it 
	/// whenever you assign it to a different variable, or pass it to another 
	/// method:
	/// <code>
	///     Range b = a;
	///     a.PopFirst(); // may not affect b if Range is a struct
	/// </code>
	/// In fact, a range should not be copied this way because assignment may or
	/// may not create a copy. You should avoid simple assignment, and use 
	/// Clone() to copy a range instead:
	/// <code>
	///     Range b = a.Clone();
	///     a.PopFirst(); // will not affect b, and you can be sure of it
	/// </code>
	/// However, if a range is a reference type or a reference to IFRange, you 
	/// are not making a copy of it when you assign it or pass it to another 
	/// method:
	/// <code>
	///     IFRange&lt;T> a = ...;
	///     IFRange&lt;T> b = a;
	///     a.PopFirst(); // The item was popped from b also
	/// </code>
	/// When writing generic code, you may want to use range types directly, as in:
	/// <code>
	///     void DoSomethingWithRange&lt;R,T>(R range) where R : IRange&lt;T> {...}
	/// </code>
	/// Using a range type directly can improve performance if R happens to be 
	/// a struct type, since there is no need to box the range when passing it 
	/// to this method. However, it is very important to keep in mind that "R" 
	/// may be a struct or a class. If this method is not intended to modify the 
	/// range from the perspective of the caller, the method must start by cloning 
	/// the range, in case R is a class or interface type:
	/// <code>
	///     void DoSomethingWithRange&lt;R,T>(R range) where R : IBRange&lt;T> {
	///         range = range.Clone();
	///         ...
	///     }
	/// </code>
	/// On the other hand, if this method <i>wants</i> the caller to see the 
	/// changes to the range, R must be passed by reference, in case it is a
	/// struct type:
	/// <code>
	///     void DoSomethingWithRange&lt;R,T>(ref R range) where R : IBRange&lt;T> {
	///         ...
	///     }
	/// </code>
	/// To avoid these subtle difficulties, you can access the range through
	/// the <see cref="IFRange{T}"/> interface; just remember to Clone() the 
	/// range when necessary.
	/// <para/>
	/// Remember that a range is an alias for some underlying data structure.
	/// If the original data structure is modified, a range will "see" those
	/// changes. For instance, suppose that a range provides a slice from 
	/// indexes 5 to 12 inclusive within an <see cref="IList{T}"/> object 
	/// that contains 15 items:
	/// <pre>
	///     IList  0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 
	///            a  b  c  d  e  f  g  h  i  j  k  l  m  n  o 
	///     IRange                0  1  2  3  4  5  6  7
	/// </pre>
	/// Thus, [0] in the range corresponds to item [5] in the list, "f". Now, 
	/// if the first three items in the list are removed, then [0] in the range 
	/// will still correspond to item [5] in the list, but the item at this 
	/// location, marked "i", used to be at index [8]:
	/// <pre>
	///     IList  0  1  2  3  4  5  6  7  8  9 10 11
	///            d  e  f  g  h  i  j  k  l  m  n  o
	///     IRange                0  1  2  3  4  5  6  7
	/// </pre>
	/// The exact behavior of the range at this point is implementation-dependent.
	/// The <c>Count</c> property may remain at 8, or it may drop to 7; perhaps the
	/// range will return default(T) when you read index [7], or perhaps it will
	/// throw <see cref="IndexOutOfRangeException"/>. Because of this lack of
	/// predictability, you should avoid keeping a range that points to a list whose
	/// size is decreasing. If individual elements are modified but not the list 
	/// size, the range is safe to use and will see the changes. If new elements 
	/// are added to the end of the list, the range may or may not continue working 
	/// as expected, depending on how the collection works and how the range works.
	/// In most cases the range will be unaffected, in contrast to common C++ 
	/// containers such as <c>std::vector</c>, in which iterators are "invalidated" 
	/// by any size change, and when an invalid iterator is accessed, it may return 
	/// bad data or crash the program.
	/// <para/>
	/// Currently there are no interfaces in Loyc.Essentials that return forward or 
	/// bidirectional ranges; the only notable method that returns a range is
	/// <see cref="IListSource{T}.Slice"/>, which returns a random-access range.
	/// Since a random-access range is also a bidirectional range, you can begin
	/// writing algorithms that accept forward and bidirectional ranges (for read 
	/// access). Any collection that implements <see cref="IListSource{T}"/> can 
	/// be treated as a range using the <see cref="ListExt.AsRange{T}(IListSource{T})"/> 
	/// extension method, which is like calling <c>Slice(0)</c>.
	/// <para/>
	/// The design philosophy of Loyc.Essentials is that potentially useful 
	/// interfaces should be included even if there are no implementations of the 
	/// interface. That's why there are interfaces like <see cref="IMFRange{T}"/>
	/// that are not implemented by any classes. Why offer unused interfaces?
	/// Because this library is designed to be extended by third parties, who 
	/// might want to implement the interface, e.g. if someone else creates 
	/// mutable data structures such as the B+ tree, the doubly-linked list or 
	/// the trie, they should offer implementations of <see cref="IMBRange{T}"/> 
	/// and/or <see cref="IMBinumerator{T}"/>.
	/// <para/>
	/// Implementors note: <see cref="IFRange{T}"/> includes <see cref="IEnumerable{T}"/>, 
	/// and you can use the following implementation of IEnumerable provided 
	/// that your range type <c>R</c> implements <see cref="ICloneable{R}"/>:
	/// <code>
	///     IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	///     IEnumerator&lt;T> IEnumerable&lt;T>.GetEnumerator() { return GetEnumerator(); }
	///     public RangeEnumerator&lt;R,T> GetEnumerator()
	///     {
	///         return new RangeEnumerator&lt;R,T>(this);
	///     }
	/// </code>
	/// TODO: write RangeBinumerator for IBRange{T},
	///             RangeMBinumerator for IMBRange{T},
	///         and RangeMEnumerator for IMEnumerator{T}.
	/// </remarks>
	public interface IFRange<out T> : IEnumerable<T>, ICloneable<IFRange<T>>, IHasFirst<T>, IIsEmpty
	{
		// <summary>Returns the first value in the range, without popping it.</summary>
		// <exception cref="EmptySequenceException">The sequence is empty.</exception>
		// <remarks>
		// A possible default implementation:
		// <pre>
		// T First { get { return Range.PopFirst(Clone()); } }
		// </pre>
		// </remarks>
		//T First { get; }

		/// <summary>Removes the first item from the range and returns it.</summary>
		/// <param name="fail">Receives the current value of <see cref="IIsEmpty.IsEmpty"/>.</param>
		/// <returns>The first item of the range, or default(T) if IsEmpty.</returns>
		/// <remarks>This method is a little unweildy in plain C#, but in EC# it 
		/// will be a bit more convenient to use via extension methods like 
		/// <c>T PopFirst(ref this Range range, T defaultValue)</c> and
		/// <c>T? PopFirst(ref this Range range)</c>, which are illegal in plain C#.
		/// <para/>
		/// I wanted to give this method the signature "bool PopFirst(out T first)"
		/// but the generic parameter "T" is covariant, i.e. it is marked "out T" 
		/// which, ironically, is not compatible with "out T" parameters, only with 
		/// return values.
		/// </remarks>
		[return: MaybeNull] // There's no attribute like [return: MaybeNullIf("fail")]
		T PopFirst(out bool fail);
	}

	/// <summary>A mutable forward range.</summary>
	/// <remarks>
	/// This range lets you change the value of <see cref="First"/>.
	/// <para/>
	/// Please see <see cref="IFRange{T}"/> for general documentation about ranges.
	/// <para/>
	/// The mutable ranges do not include a clone method due to a limitation of C#.
	/// C# does not support covariance, which means that every time a derived 
	/// interface supports cloning, the implementing class is required to write a 
	/// separate clone method. Read-only ranges already have to implement up to 
	/// four clone methods: ICloneable{IFRange{T}}, ICloneable{IBRange{T}}, 
	/// ICloneable{IRange{T}}, and ICloneable{IRangeEx{T}}, and that's in addition 
	/// to the Clone method for the concrete type! If mutable ranges also supported
	/// cloning, they would add up to three more clone methods, which is really 
	/// getting out of hand.
	/// <para/>
	/// To limit the maximum number of clone methods to something reasonable, only 
	/// the immutable ranges have a clone method, but if the original range was 
	/// mutable then the clone will also be mutable; you just have to cast the 
	/// result:
	/// <code>
	///     var r2 = (IMFRange&lt;T>)r.Clone();
	/// </code>
	/// </remarks>
	public interface IMFRange<T> : IFRange<T>, IHasMFirst<T>
	{
		// <summary>Gets or sets the value of the first item in the range.</summary>
		// <exception cref="EmptySequenceException">The sequence is empty.</exception>
		//new T First { get; set; }
	}

	/// <summary>A bidirectional range. Allows you to read or remove the first
	/// or last element in a range.</summary>
	/// <remarks>
	/// The bidirectional range interface is useful for supporting
	/// data structures such as doubly-linked lists that have a front and a
	/// back but no efficient access to the middle.
	/// <para/>
	/// Please see <see cref="IFRange{T}"/> for general documentation about ranges.
	/// </remarks>
	public interface IBRange<out T> : IFRange<T>, ICloneable<IBRange<T>>, IHasLast<T>
	{
		// <summary>Returns the value of the last item in the range.</summary>
		// <exception cref="EmptySequenceException">The sequence is empty.</exception>
		// <remarks>
		// A reasonable default implementation:
		// <pre>
		// T Last { get { return Range.PopLast(Clone()); } }
		// </pre>
		// </remarks>
		//T Last { get; }

		/// <summary>Removes the last item from the range and returns it.</summary>
		/// <param name="fail">Receives the current value of IsEmpty.</param>
		/// <returns>The first item of the range, or default(T) if IsEmpty.</returns>
		/// <remarks>The remarks of <see cref="IFRange{T}.PopFirst"/> apply to this method.</remarks>
		[return: MaybeNull] // There's no attribute like [return: MaybeNullIf("fail")]
		T PopLast(out bool fail);
	}

	/// <summary>A mutable bidirectional range.</summary>
	/// <remarks>This range lets you change the value of <see cref="IMFRange{T}.First"/> and <see cref="Last"/>.
	/// <para/>
	/// Please see <see cref="IFRange{T}"/> for general documentation about ranges.
	/// </remarks>
	public interface IMBRange<T> : IBRange<T>, IMFRange<T>
	{
		/// <summary>Gets or sets the value of the last item in the range.</summary>
		/// <exception cref="EmptySequenceException">The sequence is empty.</exception>
		new T Last { get; set; }
	}

	/// <summary>A random-access range, also known as a "slice". Allows you to 
	/// narrow down the range like <see cref="IBRange{T}"/> does, and also 
	/// provides random access via <see cref="IListSource{T}"/>.</summary>
	/// <remarks>
	/// Please see <see cref="IFRange{T}"/> for general documentation about ranges.
	/// </remarks>
	public interface IRange<out T> : IBRange<T>, IListSource<T>, ICloneable<IRange<T>>
	{
		// Since C# does not support covariant return types, implementing all the 
		// ICloneables can be quite a chore. Just copy and paste these, inserting
		// an appropriate constructor for your range type:
		/*
		IFRange<T>  ICloneable<IFRange<T>>.Clone() { return Clone(); }
		IBRange<T>  ICloneable<IBRange<T>>.Clone() { return Clone(); }
		IRange<T>   ICloneable<IRange<T>> .Clone() { return Clone(); }
		public MyType Clone() { return new MyType(...); }
		
		// If applicable
		IBRangeEx<T> ICloneable<IBRangeEx<T>>.Clone() { return Clone(); }
		IRangeEx<T>  ICloneable<IRangeEx<T>> .Clone() { return Clone(); }
		*/
	}

	/// <summary>A bidirectional range that can perform operations such as 
	/// intersection and overlap tests on pairs of ranges of the same type.</summary>
	/// <typeparam name="R">The type that implements this interface</typeparam>
	/// <typeparam name="T">The type of elements in the range</typeparam>
	public interface IBRangeEx<R, T> : IBRange<T> where R : IBRangeEx<R, T>, ICloneable<R>
	{
		/// <summary>Gets the list upon which this range is based.</summary>
		/// <remarks>The return type is <see cref="IEnumerable{T}"/> since the 
		/// available list interfaces may vary, e.g. it might be 
		/// <see cref="ICollection{T}"/> or <see cref="IListSource{T}"/>.</remarks>
		IEnumerable<T> InnerList { get; }
		/// <summary>Index where this range starts within the <see cref="InnerList"/>.</summary>
		int SliceStart { get; }
		/// <summary>Gets the region of overlap between two ranges.</summary>
		/// <remarks>If the ranges do not overlap, an empty range is returned with
		/// <see cref="InnerList"/> set to the same value that this range has.</remarks>
		R Intersect(R other);
		/// <summary>Gets a range just large enough to contain both ranges.</summary>
		/// <exception cref="InvalidOperationException">The two ranges cannot be 
		/// combined because they have different <see cref="InnerList"/> values.</exception>
		/// <remarks>As long as both ranges are based on the same list, this method 
		/// succeeds. For example, if one range covers 5..6 and the other range 
		/// covers 10..20, the union covers 5..20.</remarks>
		R Union(R other);
	}

	/// <summary>A bidirectional range that can perform operations such as 
	/// intersection and overlap tests on pairs of ranges.</summary>
	public interface IBRangeEx<T> : IBRangeEx<IBRangeEx<T>, T>, ICloneable<IBRangeEx<T>>
	{
	}

	/// <summary>A random-access range that can perform operations such as 
	/// intersection and overlap tests on pairs of ranges of the same type.</summary>
	public interface IRangeEx<R, T> : IRange<T>, IBRangeEx<R, T> where R : IRangeEx<R, T>, ICloneable<R>
	{
	}
	
	/// <summary>A random-access range that can perform operations such as 
	/// intersection and overlap tests on pairs of ranges.</summary>
	public interface IRangeEx<T> : IRangeEx<IRangeEx<T>, T>, ICloneable<IRangeEx<T>>
	{
	}

	/// <summary>A mutable random-access range.</summary>
	/// <remarks>IMRange models a shrinkable array. You can modify elements or 
	/// shrink the array, but not add anything new; this is a useful interface 
	/// for some divide-and-conquer problems, such as the quick sort.
	/// <para/>
	/// Please see <see cref="IFRange{T}"/> for general documentation about ranges.
	/// </remarks>
	public interface IMRange<T> : IMBRange<T>, IRange<T>
	{
		new T this[int index] { get; set; }
	}
}
