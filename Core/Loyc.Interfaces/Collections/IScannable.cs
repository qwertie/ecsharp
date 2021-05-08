using Loyc.Threading;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>A data-reading object returned by <see cref="IScannable{T}"/>
	///   implementations.</summary>
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
		///   block with a Length at least this high.</param>
		/// <param name="buffer">If the scanner cannot naturally offer a block as long 
		///   as requested, it may need a temporary storage area in which to combine
		///   multiple blocks. If so, the method can use a buffer provided by the caller 
		///   if it is large enough. If the caller provides a span that is smaller than
		///   <c>minLength</c>, and if the scanner also needs to allocate a buffer, it
		///   allocates a new buffer and returns it in this parameter.</param>
		/// <exception cref="ArgumentException">skip was negative, and backward 
		///   scanning is not supported or the caller skipped backward too far.</exception>
		/// <returns>A chunk of data, or an empty span. The span is empty if and only 
		///   if the end of collection is reached.</returns>
		ReadOnlyMemory<T> Read(int skip, int minLength, ref Memory<T> buffer);

		/// <summary>Returns true if the skip parameter is (ever) allowed to be negative 
		/// when calling <see cref="Read(int, int, ref Memory{T})"/>.</summary>
		bool CanScanBackward { get; }
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
	/// </remarks>
	public interface IScannable<T>
	{
		IScanner<T> Scan();
	}
	
	public static partial class LCExt
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
