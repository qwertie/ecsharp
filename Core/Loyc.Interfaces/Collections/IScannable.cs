using Loyc.Threading;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>A data-reading object returned by <see cref="IScannable{T}"/>
	///   implementations. This interface is used for reading data, similar to
	///   <see cref="IEnumerator{T}"/>, but it reads blocks of data instead of
	///   single items. This can improve performance by reducing the number of 
	///   interface calls necessary to scan a collection.</summary>
	public interface IScanner<T>
	{
		/// <summary>Jumps forward in the input by the specified amount, and reads a 
		///   chunk of data at the new location (without skipping past it). The precise 
		///   chunk size is chosen by the scanner.</summary>
		/// <param name="skip">Amount of data to advance past in the input. If the 
		///   caller wants to have access to each data item in the sequence exactly 
		///   once, this parameter should 0 the first time it calls this method, and 
		///   mem.Length (where mem is the previous return value of this method) each 
		///   time afterward.</param>
		/// <param name="minLength">Minimum block size. The scanner must return a memory
		///   block with a Length at least this high, unless there are not enough
		///   remaining items in the collection or data stream to achieve this. 
		///   If minLength is negative (e.g. -1), the scanner must return at least one 
		///   item, and should choose whatever amount is optimal for the scanner.</param>
		/// <param name="buffer">If the scanner cannot naturally offer a block as large
		///   as requested, it may need a temporary storage area in which to combine
		///   multiple blocks. If so, the method can use a buffer provided by the caller 
		///   if it is large enough. If the caller provides a span that is smaller than
		///   <c>minLength</c>, and if the scanner also needs to allocate a buffer, the
		///   scanner allocates a new buffer and returns it in this parameter. Note:
		///   implementations are allowed to ignore this parameter.</param>
		/// <exception cref="ArgumentException">skip was negative, and backward 
		///   scanning is not supported or the caller tried to skip backward beyond the
		///   beginning of the sequence.</exception>
		/// <returns>A chunk of data, or an empty span. The span is empty if and only 
		///   if the end of collection is reached.</returns>
		/// <remarks>The caller can force the entire sequence to be read into a single 
		///   contiguous buffer by using minLength = int.MaxValue. On the other hand,
		///   if the caller wants the scanner to use the optimal size</remarks>
		ReadOnlyMemory<T> Read(int skip, int minLength, ref Memory<T> buffer);

		/// <summary>Returns true if the skip parameter is (ever) allowed to be negative 
		/// when calling <see cref="Read(int, int, ref Memory{T})"/>.</summary>
		bool CanScanBackward { get; }
	}

	/// <summary>Same as <see cref="IScannable{T}"/> except that it does not implement 
	/// <see cref="IEnumerable{T}"/>. In most cases <see cref="IScannable{T}"/> should
	/// be implemented instead.</summary>
	public interface IScan<T>
	{
		IScanner<T> Scan();
	}

	/// <summary>A sequence of T objects that is meant to be read in chunks. This 
	/// interface is essentially a faster version of <see cref="IEnumerable{T}"/>: 
	/// it allows the caller to scan a sequence faster by avoiding unnecessary 
	/// interface calls.</summary>
	/// <remarks>
	/// Example usage:
	/// <pre><code><![CDATA[
	/// public static List<T> AddToList<T>(IScannable<T> sequence, List<T> list)
	/// {
	///     var scanner = ((IScannable<T>)null!).Scan();
	///     var current = ReadOnlySpan<T>.Empty;
	///     var temp = Memory<T>.Empty;
	///     while ((current = scanner.Read(current.Length, 0, ref temp).Span).Length != 0) {
	///         for (int i = 0; i < current.Length; i++)
	///             list.Add(current[i]);
	///     }
	///     return list;
	/// }
	/// ]]></code></pre>
	/// <para/>
	/// In practice, an object that implements Scan() will also implement 
	/// <see cref="IEnumerable{T}"/>, and this interface includes <see cref="IEnumerable{T}"/>
	/// for disambiguation purposes, so that 
	/// </remarks>
	public interface IScannable<T> : IEnumerable<T>, IScan<T>
	{
	}
	
	public static partial class LCInterfaces
	{
		/// <inheritdoc cref="IScanner{T}.Read(int, int, ref Memory{T})"/>
		public static ReadOnlyMemory<T> Read<Scanner, T>(this Scanner self, int skip, int minLength) where Scanner : IScanner<T>
		{
			var empty = Memory<T>.Empty;
			return self.Read(skip, minLength, ref empty);
		}
		//public static ReadOnlyMemory<T> CopyTo<Scannable, T>(this Scannable scannable, int skip, int minLength) where Scannable : IScannable<T>
		//{
		//	
		//}
	}
}
