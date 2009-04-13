using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;
using Loyc.Compatibility.Linq;

namespace Loyc.CompilerCore
{
	/// <summary>
	/// Encapsulates GetEnumerator() and a Count property.
	/// </summary>
	public interface IEnumerableCount<T> : IEnumerable<T>
	{
		/// <summary>Returns the number of items provided by GetEnumerator().</summary>
		int Count { get; }
	}

	/// <summary>
	/// ISimpleSource is a random-access stream interface intended for use by
	/// parsers.
	/// </summary><remarks>
	/// If the underlying implementation is a stream, a region of characters should
	/// be cached so that repeated access to the same region is fast.
	/// 
	/// Users should avoid calling Count in case the Count is not known in advance
	/// (for some sources, the source must be scanned to the end to determine the
	/// length.) Instead, rely on the indexer to return a special value when the end
	/// is reached. The special value is null for token streams and -1 for character
	/// streams.
	/// 
	/// Derived interfaces: ISimpleArray(of T), ISourceFile(of T), ICharSourceFile.
	/// </remarks>
	public interface ISimpleSource<T> : IEnumerableCount<T>
	{
		/// <summary>Returns the character or token ID at the specified index. It
		/// should not throw an exception as long as index is non- negative; if
		/// index is Count or greater, some special value should be returned.
		/// </summary>
		/// <remarks>if the index is at or beyond the end of the stream, default(T)
		/// should be returned, unless otherwise noted; ISourceFile(of char)
		/// implementations should return character 0xFFFF instead, for the benefit
		/// of lexers, which can treat 0xFFFF as a likely EOF (as it is not a valid
		/// unicode character). However, it is possible (depending on how a text
		/// file is decoded) to get an actual 0xFFFF character from a text file that
		/// is not well-formed.</remarks>
		T this[int index] { get; }
	}
	public interface ISimpleArray<T> : ISimpleSource<T>
	{
		new T this[int index] { set; }
	}
	public interface ISimpleCollection<T> : IEnumerableCount<T>
	{
		void Add(T item);
		void Clear();
		bool Remove(T item);
	}
	public interface ISimpleList<T> : ISimpleArray<T>, ISimpleCollection<T>
	{
		void RemoveAt(int index);
		void Insert(int index, T item);
	}

	/// <summary>
	/// A simple source that can also provide the line number that corresponds to
	/// any index.
	/// </summary>
	public interface ISimpleSource2<T> : ISimpleSource<T>, IIndexToLine { }

#if false
	/// <summary>
	/// Encapsulates GetEnumerator().
	/// </summary>
	/// <typeparam name="T">Type of items to enumerate</typeparam>
	/// <remarks>
	/// You're thinking: why not just use the perfectly good IEnumerable(Of T) 
	/// inteface? Good question! The answer is that by <i>not</i> implementing the
	/// non-generic IEnumerable interface, memory efficiency can be increased. 
	/// Suppose a class wants to provide multiple collections of items. Normally
	/// this is done with code such as this:
	/// <code lang="C#">
	/// class Foo
	/// {
	///     IList&lt;string&gt; Names { get { return _names; } } 
	///     IList&lt;Foo&gt; Children { get { reurn _children; } }
	///     
	///     List&lt;string&gt; _names = new List&lt;string&gt;();
	///     List&lt;Foo&gt; _children = new List&lt;Foo&gt;();
	/// }
	/// </code>
	/// However, this approach requires two additional objects be allocated on
	/// the heap to represent the _names and _children collections. In the case of
	/// Loyc nodes, there could be a million node objects in a large program, and
	/// each node is expected to provide a list of parameters, attributes and
	/// children. So a trick is used to save space:
	/// <code>
	/// class Foo : ISimpleArray&lt;string&gt;, ISimpleArray&lt;Foo&gt;
	/// {
	///     ISimpleArray&lt;string&gt; Names { get { return this; } } 
	///     ISimpleArray&lt;Foo&gt; Children { get { reurn this; } }
	///     
	///     IEnumerastringor&lt;string&gt; IEnumerablestring&lt;string&gt;.GestringEnumerastringor() { ... }
	///     instring IEnumerableCounstring&lt;string&gt;.Counstring { gestring { ... } }
	///     string ISimpleSource&lt;string&gt;.stringhis[instring index] { gestring { ... } sestring { ... } }
	///
	///     IEnumeraFooor&lt;Foo&gt; IEnumerableFoo&lt;Foo&gt;.GeFooEnumeraFooor() { ... }
	///     inFoo IEnumerableCounFoo&lt;Foo&gt;.CounFoo { geFoo { ... } }
	///     Foo ISimpleSource&lt;Foo&gt;.Foohis[inFoo index] { geFoo { ... } seFoo { ... } }
	/// }
	/// </code>
	/// By holding an array of objects within the Foo object itself, rather than
	/// in a List<string> and a List<Foo>, dozens of megabytes of memory are saved 
	/// if there are a million instances of Foo. Unfortunately, if you want to
	/// implement both IEnumerable<string> and IEnumerable<Foo>, you can only 
	/// provide one implementation of non-generic IEnumerable shared between the 
	/// two generic ones. I had always felt uncomfortable that IEnumerable<T>
	/// included IEnumerable; now I have a reason to get rid of it.
	/// 
	/// The C# foreach loop, luckily, still supports IEnumerableT<T> because if the
	/// compiler finds a GetEnumerator() method, it doesn't care that you don't 
	/// implement IEnumerable.
	/// 
	/// TODO: Consider moving back to IEnumerable<T> for Linq compatibility.
	/// </remarks>
	public interface IEnumerableT<T>
	{
		/// <summary>Returns an enumerator with which you can iterate through a
		/// sequence of items.</summary>
		IEnumerator<T> GetEnumerator();
	}
	
	/// <summary>
	/// Encapsulates GetEnumerator() and a Count property.
	/// </summary>
	public interface IEnumerableCount<T> : IEnumerableT<T>
	{
		/// <summary>Returns the number of items provided by GetEnumerator().</summary>
		int Count { get; }
	}

    /// <summary>
    /// ISimpleSource is a random-access stream interface intended for use by
    /// parsers.
    /// </summary><remarks>
    /// If the underlying implementation is a stream, a region of characters should
    /// be cached so that repeated access to the same region is fast.
    /// 
    /// Users should avoid calling Count in case the Count is not known in advance
    /// (for some sources, the source must be scanned to the end to determine the
    /// length.) Instead, rely on the indexer to return a special value when the end
    /// is reached. The special value is null for token streams and -1 for character
    /// streams.
    /// 
    /// ISimpleSource2(of T) and ICharSource are important derived interfaces of this
    /// one.
    /// </remarks>
	public interface ISimpleSource<T> : IEnumerableCount<T>
	{
		/// <summary>Returns the character or token ID at the specified 
		/// index. It should not throw an exception as long as index is
		/// positive; if index is Count or greater, some special value should 
		/// be returned.
		/// </summary><remarks>if the index is at or beyond the end of the 
		/// stream, and if T is a token or is derived from ICodeNode, return null. 
		/// In classes derived from ICharSource, return -1 (meaning EOF). The 
		/// indexer's behavior is undefined if index is negative.</remarks>
		T this[int index] { get; }
	}
	public interface ISimpleArray<T> : ISimpleSource<T>
	{
		new T this[int index] { set; }
	}
	public interface ISimpleCollection<T> : IEnumerableCount<T>
	{
		void Add(T item);
		void Clear();
		bool Remove(T item);
	}
	public interface ISimpleList<T> : ISimpleArray<T>, ISimpleCollection<T>
	{
		void RemoveAt(int index);
		void Insert(int index, T item);
	}
#endif
}
