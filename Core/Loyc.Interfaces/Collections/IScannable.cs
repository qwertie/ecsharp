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
		///   once, this parameter should be 0 the first time it calls this method, and 
		///   mem.Length (where mem is the previous return value of this method) each 
		///   time afterward.</param>
		/// <param name="minLength">Minimum block size. The scanner must return a memory
		///   block with a Length at least this high, unless there are not enough
		///   remaining items in the collection or data stream to achieve this. 
		///   If minLength is zero or negative (e.g. -1), the scanner must return at 
		///   least one item, and should choose whatever amount is optimal for the 
		///   scanner.
		///   <para/>
		///   Caution: do not use an excessive amount such as int.MaxValue for this 
		///   parameter; doing so is harmless in some implementations (which just 
		///   return the remainder of the list) but causes OutOfMemoryException in 
		///   others, such as streams that don't know their own length and always 
		///   allocate a memory block of at least the requested size.</param>
		/// <param name="buffer">If the scanner cannot naturally offer a block as large
		///   as requested, it may need a temporary storage area in which to combine
		///   multiple blocks. If so, the method can use a buffer provided by the caller 
		///   in this parameter, if that buffer is large enough. If the caller provides 
		///   a memory block that is smaller than <c>minLength</c>, and if the scanner 
		///   also needs to allocate a buffer, the scanner allocates a new buffer and 
		///   returns it in this parameter.
		///   <para/>
		///   If the caller doesn't need to re-use or re-read old buffers, it should 
		///   simply initialize a variable to the `default` value and pass the same 
		///   variable into this parameter every time it calls this method. If, on the 
		///   other hand, the caller wants to preserve the contents of an old buffer,
		///   it should send a new empty buffer in this parameter so that this method 
		///   won't overwrite the old one.
		///   <para/>
		///   Note: <see cref="IScanner{T}"/> implementations are allowed to ignore 
		///   this parameter. If you send a very large buffer, the scanner may or may 
		///   not decide to use all of it.
		/// </param>
		/// <exception cref="ArgumentException"><c>skip</c> was negative, and backward 
		///   scanning is not supported or the caller tried to skip backward beyond the
		///   beginning of the sequence.</exception>
		/// <returns>A chunk of data, or an empty span. The span is empty if and only 
		///   if the end of collection is reached.</returns>
		/// <remarks>The caller can force the entire sequence to be read into a single 
		///   contiguous buffer by using minLength = int.MaxValue. On the other hand,
		///   if you want the scanner to use the size it deems optimal, set 
		///   minLength = -1.</remarks>
		ReadOnlyMemory<T> Read(int skip, int minLength, ref Memory<T> buffer);

		/// <summary>Returns true if the <c>skip</c> parameter is allowed to be 
		///   negative when calling <see cref="Read(int, int, ref Memory{T})"/>.</summary>
		bool CanScanBackward { get; }
	}

	/// <summary>A sequence of T objects that is meant to be read in chunks. This 
	///   interface is essentially a faster version of <see cref="IEnumerable{T}"/>: 
	///   it allows the caller to scan a sequence faster by avoiding unnecessary 
	///   interface calls.</summary>
	/// <remarks>
	///   Example usage:
	///   <pre><code><![CDATA[
	///   public static List<T> AddToList<T>(IScannable<T> sequence, List<T> list)
	///   {
	///       IScanner<T> scanner = sequence.Scan();
	///       var current = ReadOnlySpan<T>.Empty;
	///       var temp = Memory<T>.Empty;
	///       while ((current = scanner.Read(current.Length, 0, ref temp).Span).Length != 0) {
	///           for (int i = 0; i < current.Length; i++)
	///               list.Add(current[i]);
	///       }
	///       return list;
	///   }
	///   ]]></code></pre>
	///   <para/>
	///   In practice, an object that implements Scan() will also implement 
	///   <see cref="IEnumerable{T}"/>, and this interface includes <see cref="IEnumerable{T}"/>
	///   for disambiguation purposes, so that 
	///   </remarks>
	public interface IScannable<T> : IEnumerable<T>, IScan<T>
	{
	}

	/// <summary>Same as <see cref="IScannable{T}"/> except that it does not implement 
	///   <see cref="IEnumerable{T}"/>. In most cases <see cref="IScannable{T}"/> should
	///   be implemented instead.</summary>
	public interface IScan<T>
	{
		IScanner<T> Scan();
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
