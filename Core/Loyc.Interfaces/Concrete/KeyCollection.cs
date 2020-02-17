/*
 * Created by David on 7/19/2007 at 10:16 PM
 * 
 * Original copyright notice follows.
 */
//
// System.Collections.Generic.Dictionary
//
// Authors:
//	Sureshkumar T (tsureshkumar@novell.com)
//	Marek Safar (marek.safar@seznam.cz) (stubs)
//	Ankit Jain (radical@corewars.org)
//	David Waite (mass@akuma.org)
//
//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
// Copyright (C) 2005 David Waite
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;

namespace Loyc.Collections
{
	/// <summary>Adapter: this is a read-only collection of Keys read from a 
	/// generic IDictionary. It is a modified version of 
	/// Dictionary{TKey, TValue}.KeyCollection from the Mono project, changed 
	/// to use IDictionary instead of Dictionary.</summary>
	[Serializable]
	public class KeyCollection<TKey, TValue> : ICollection<TKey>, ICollectionAndReadOnly<TKey>, IEnumerable<TKey>, ICollection, IEnumerable 
	{
		IReadOnlyDictionary<TKey, TValue> dictionary;

		public KeyCollection (IReadOnlyDictionary<TKey, TValue> dictionary)
		{
			if (dictionary == null)
				throw new ArgumentNullException ("dictionary");
			this.dictionary = dictionary;
		}

		public void CopyTo (TKey [] array, int index)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
			if (index < 0)
				throw new ArgumentOutOfRangeException ("index");
			// we want no exception for index==array.Length && dictionary.Count == 0
			if (index > array.Length)
				throw new ArgumentException ("index larger than largest valid index of array");
			if (array.Length - index < dictionary.Count)
				throw new ArgumentException ("Destination array cannot hold the requested elements!");

			foreach (TKey k in this)
				array [index++] = k;
		}

		public Enumerator GetEnumerator ()
		{
			return new Enumerator (dictionary);
		}

		void ICollection<TKey>.Add (TKey item)
		{
			throw new NotSupportedException ("this is a read-only collection");
		}

		void ICollection<TKey>.Clear ()
		{
			throw new NotSupportedException ("this is a read-only collection");
		}

		bool ICollection<TKey>.Contains (TKey item)
		{
			return dictionary.ContainsKey (item);
		}

		bool ICollection<TKey>.Remove (TKey item)
		{
			throw new NotSupportedException ("this is a read-only collection");
		}

		IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator ()
		{
			return this.GetEnumerator ();
		}

		void ICollection.CopyTo (Array array, int index)
		{
			CopyTo ((TKey []) array, index);
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return this.GetEnumerator ();
		}

		public int Count {
			get { return dictionary.Count; }
		}

		bool ICollection<TKey>.IsReadOnly {
			get { return true; }
		}

		bool ICollection.IsSynchronized {
			get { return false; }
		}

		object ICollection.SyncRoot {
			get { return ((ICollection) dictionary).SyncRoot; }
		}

		[Serializable]
		public struct Enumerator : IEnumerator<TKey>, IDisposable, IEnumerator {
			IEnumerator<KeyValuePair<TKey, TValue>> host_enumerator;

			internal Enumerator (IReadOnlyDictionary<TKey, TValue> host)
			{
				host_enumerator = host.GetEnumerator ();
			}

			public void Dispose ()
			{
				host_enumerator.Dispose ();
			}

			public bool MoveNext ()
			{
				return host_enumerator.MoveNext ();
			}

			public TKey Current {
				get { return host_enumerator.Current.Key; }
			}

			object IEnumerator.Current {
				get { return host_enumerator.Current.Key; }
			}

			void IEnumerator.Reset ()
			{
				((IEnumerator)host_enumerator).Reset ();
			}
		}
	}
}
