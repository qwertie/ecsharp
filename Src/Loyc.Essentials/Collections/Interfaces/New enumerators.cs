//
// Defines enhanced enumerators:
// - IBinumerator<T>: bidirectional enumerator
// - IMEnumerator<T>: unidirectional, mutable enumerator
// - IMBinumerator<T>: bidirectional, mutable enumerator
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>Extends the "enumerator" concept to allow backward enumeration.</summary>
	/// <remarks>
	/// When MoveNext() returns false, indicating that there are no more elements,
	/// you can still call MovePrev() to go back to the previous element.
	/// </remarks>
	public interface IBinumerator<T> : IEnumerator<T>
	{
		bool MovePrev();

		// Implementing IEnumerator is such a pain in the butt. 
		// Copy and paste to get started:
		/*
		public bool MovePrev()
		{
			TODO;
		}
		public bool MoveNext()
		{
			TODO;
		}
		public T Current
		{
			get { return TODO; }
		}

		void IDisposable.Dispose() { }
		object System.Collections.IEnumerator.Current { get { return Current; } }
		public void Reset() { throw new NotSupportedException(); }
		*/
	}

	/// <summary>A mutable enumerator interface. Provides a "Remove" method like
	/// Java iterators have, and allows you to modify the current item.</summary>
	/// <remarks>Please note, not all collections will support "Remove".</remarks>
	interface IMEnumerator<T> : IEnumerator<T>
	{
		/// <summary>Gets or sets the value of the current item.</summary>
		new T Current { get; set; }
		/// <summary>Removes the current item and moves to the next one. Remember
		/// NOT to call MoveNext() immediately after Remove().</summary>
		/// <returns>True if there is a next item after this one, 
		/// false if the removed item was the last one.</returns>
		/// <exception cref="NotSupportedException">The collection does not permit
		/// this operation.</exception>
		bool Remove();
	}

	/// <summary>A mutable bidirectional enumerator interface. Please note that
	/// the "Remove" method always moves to the next item, even though the 
	/// Binumerator is capable of moving backward.</summary>
	interface IMBinumerator<T> : IBinumerator<T>, IMEnumerator<T>
	{
	}

	interface IBinumerable<T>
	{
		/// <summary>Returns a binumerator that points before the beginning of the current collection.</summary>
		IBinumerator<T> Begin();
		/// <summary>Returns a binumerator that points after the end of the current collection.</summary>
		IBinumerator<T> End();
	}
}
