using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Loyc.Runtime;

namespace Loyc.Runtime 
{
	/// <summary>
	/// An implementation of IAttributes that can hold one attribute before
	/// allocating any memory for a hashtable. It is intended to be used as
	/// a base class but can be used on its own.
	/// </summary>
	public class HashTags<ValueT> : ITags<ValueT>, IDictionary<Symbol, ValueT>
	{
		protected Symbol _cachedAttrKey;
		protected ValueT _cachedAttrValue;
		protected Dictionary<Symbol, ValueT> _attrs;

		public HashTags() { }
		public HashTags(HashTags<ValueT> original)
		{
			if (original._attrs != null)
				_attrs = new Dictionary<Symbol, ValueT>(original._attrs);
			_cachedAttrKey = original._cachedAttrKey;
			_cachedAttrValue = original._cachedAttrValue;
		}

		public IDictionary<Symbol, ValueT> Tags { get { return this; } }

		public ValueT GetTag(string key) { return GetTag(Symbol.GetIfExists(key)); }
		public ValueT GetTag(Symbol key)
		{
			ValueT val;
			if (key == _cachedAttrKey)
				return _cachedAttrValue;
			else if (_attrs == null || key == null)
				return default(ValueT);
			_attrs.TryGetValue(key, out val);
			return val;
		}
		
		public void SetTag(string key, ValueT val) { SetTag(Symbol.Get(key), val); }
		public void SetTag(Symbol key, ValueT val)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			if (key == _cachedAttrKey) {
				_cachedAttrValue = val;
				if (_attrs != null)
					_attrs[key] = val;
			} else if (_attrs == null) {
				if (_cachedAttrKey == null) {
					_cachedAttrKey = key;
					_cachedAttrValue = val;
				} else {
					// cached key is already in use, so create a dictionary.
					_attrs = new Dictionary<Symbol, ValueT>();
					_attrs.Add(_cachedAttrKey, _cachedAttrValue);
					_attrs.Add(key, val);
				}
			} else {
				// we have a dictionary, so use it.
				_attrs[key] = val;
			}
		}
		
		public bool RemoveTag(string key) { return RemoveTag(Symbol.GetIfExists(key)); }
		public bool RemoveTag(Symbol key)
		{
			if (key == null)
				return false;

			bool found = false;
			if (_attrs != null)
				found = _attrs.Remove(key);
			if (_cachedAttrKey == key)
			{
				_cachedAttrKey = null;
				_cachedAttrValue = default(ValueT);
				Debug.Assert(_attrs == null || found);
				found = true;
			}
			return found;
		}

		public bool HasTag(string key) { return HasTag(Symbol.GetIfExists(key)); }
		public bool HasTag(Symbol key)
		{
			if (key == null)
				return false;
			else
				return _cachedAttrKey == key ||
					(_attrs != null && _attrs.ContainsKey(key));
		}
		public IEnumerator<KeyValuePair<Symbol, ValueT>> TagEnumerator()
		{
			if (_attrs == null)
				return OneTagEnumerator();
			else
				return _attrs.GetEnumerator();
		}
		private IEnumerator<KeyValuePair<Symbol, ValueT>> OneTagEnumerator()
		{
			if (_cachedAttrKey != null)
				yield return new KeyValuePair<Symbol, ValueT>(_cachedAttrKey, _cachedAttrValue);
		}
		
		#region IDictionary<Symbol, ValueT>
		ValueT IDictionary<Symbol, ValueT>.this[Symbol key]
		{
			get { return GetTag(key); }
			set { SetTag(key, value); }
		}
		
		ICollection<Symbol> IDictionary<Symbol, ValueT>.Keys {
			get { return new KeyCollection<Symbol, ValueT>((IDictionary<Symbol, ValueT>)this); }
		}
		
		ICollection<ValueT> IDictionary<Symbol, ValueT>.Values {
			get { return new ValueCollection<Symbol, ValueT>((IDictionary<Symbol, ValueT>)this); }
		}

		int ICollection<KeyValuePair<Symbol, ValueT>>.Count {
			get { 
				if (_attrs == null)
					return _cachedAttrKey != null ? 1 : 0;
				else
					return _attrs.Count;
			}
		}
		
		bool ICollection<KeyValuePair<Symbol, ValueT>>.IsReadOnly {
			get { return false; }
		}
		
		bool IDictionary<Symbol, ValueT>.ContainsKey(Symbol key)
			{ return this.HasTag(key); }
		
		void IDictionary<Symbol, ValueT>.Add(Symbol key, ValueT value)
		{
			if (this.HasTag(key))
				throw new ArgumentException(string.Format("The key '{0}' already exists in the IDictionary", key.Name));
			this.SetTag(key, value);
		}
		
		bool IDictionary<Symbol, ValueT>.Remove(Symbol key)
			{ return this.RemoveTag(key); }
		
		bool IDictionary<Symbol, ValueT>.TryGetValue(Symbol key, out ValueT value)
		{
			value = GetTag(key);
			return HasTag(key);
		}
		
		void ICollection<KeyValuePair<Symbol, ValueT>>.Add(KeyValuePair<Symbol, ValueT> item)
		{
			((IDictionary<Symbol, ValueT>)this).Add(item.Key, item.Value);
		}
		
		void ICollection<KeyValuePair<Symbol, ValueT>>.Clear()
		{
			_cachedAttrKey = null;
			_cachedAttrValue = default(ValueT);
			_attrs.Clear();
		}
		
		bool ICollection<KeyValuePair<Symbol, ValueT>>.Contains(KeyValuePair<Symbol, ValueT> item)
		{
			return HasTag(item.Key) && GetTag(item.Key).Equals(item.Value);
		}
		
		void ICollection<KeyValuePair<Symbol, ValueT>>.CopyTo(KeyValuePair<Symbol, ValueT>[] array, int arrayIndex)
		{
			if (((IDictionary<Symbol, ValueT>)this).Count > array.Length - arrayIndex)
				throw new ArgumentException("Insufficient space in supplied array");
			if (_attrs == null)
				((ICollection<KeyValuePair<Symbol, ValueT>>)_attrs).CopyTo(array, arrayIndex);
			if (_cachedAttrKey != null)
				array[arrayIndex] = new KeyValuePair<Symbol, ValueT>(_cachedAttrKey, _cachedAttrValue);
		}
		
		bool ICollection<KeyValuePair<Symbol, ValueT>>.Remove(KeyValuePair<Symbol, ValueT> item)
		{
			if (GetTag(item.Key).Equals(item.Value))
				return RemoveTag(item.Key);
			else
				return false;
		}
		
		IEnumerator<KeyValuePair<Symbol, ValueT>> IEnumerable<KeyValuePair<Symbol, ValueT>>.GetEnumerator()
		{
			return TagEnumerator();
		}
		
		//[System.Runtime.InteropServices.DispIdAttribute()]
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return TagEnumerator();
		}
		#endregion
	}
/*	public class OneItemReadOnlyCollection : ICollection
	{
		int _count;
		object _item;
		
		public int Count { get { return _count; } }
		
		public bool IsReadOnly { get { return false; } }
		
		public void Add(T item)
		{
			throw new InvalidOperationException();
		}
		
		public void Clear()
		{
			throw new NotImplementedException();
		}
		
		public bool Contains(T item)
		{
			throw new NotImplementedException();
		}
		
		public void CopyTo(T[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}
		
		public bool Remove(T item)
		{
			throw new NotImplementedException();
		}
		
		public IEnumerator<T> GetEnumerator()
		{
			throw new NotImplementedException();
		}
		
		[System.Runtime.InteropServices.DispIdAttribute()]
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}
	}*/
	[TestFixture] public class HashTagsTests
	{
		private void TestTheBasics(HashTags<string> a, bool startsEmpty)
		{
			// This test is run twice, once on a set that starts empty (to
			// test the one-element code paths) and again on a set that has
			// unrelated stuff in it already.
			IEnumerator<KeyValuePair<Symbol, string>> e;

			// Sanity checks
			Assert.IsNull(a.GetTag((string)null));
			Assert.IsFalse(a.RemoveTag("Nonexistant"));
			Assert.IsFalse(a.HasTag("Nonexistant"));
			
			a.SetTag("One", "Two");
			Assert.AreEqual(a.GetTag("One"), "Two");

			// Test the enumerator
			e = a.TagEnumerator();
			Assert.IsTrue(e.MoveNext());
			if (startsEmpty)
			{
				Assert.AreEqual(Symbol.Get("One"), e.Current.Key);
				Assert.AreEqual("Two", e.Current.Value);
				Assert.IsFalse(e.MoveNext());
			}

			// Remove what we added
			Assert.IsNull(a.GetTag((string)null));
			Assert.IsFalse(a.RemoveTag(""));
			Assert.IsTrue(a.RemoveTag("One"));
			Assert.IsNull(a.GetTag("One"));

			if (startsEmpty)
			{
				e = a.TagEnumerator();
				Assert.IsFalse(e.MoveNext());
			}

			// Do almost the same thing again: add an attr, then remove it
			a.SetTag("One", "Two");
			Assert.AreEqual("Two", a.GetTag("One"));
			Assert.IsTrue(a.HasTag("One"));
			Assert.IsTrue(a.RemoveTag("One"));
			Assert.IsNull(a.GetTag("One"));

			// A different attribute
			a.SetTag("Two", "Three");
			Assert.AreEqual("Three", a.GetTag("Two"));
			a.SetTag("Two", "Four");
			a.SetTag("Two", "Two");
			Assert.AreEqual("Two", a.GetTag("Two"));

			// Another attribute should not disturb the first
			a.SetTag("Three", "Four");
			Assert.AreEqual("Two", a.GetTag("Two"));
			Assert.IsFalse(a.HasTag("One"));
			Assert.IsTrue(a.HasTag("Two"));
			Assert.IsTrue(a.HasTag("Three"));

			// Test the enumerator
			e = a.TagEnumerator();
			Assert.IsTrue(e.MoveNext());
			Assert.IsTrue(e.MoveNext());
			if (startsEmpty)
				Assert.IsFalse(e.MoveNext());

			// Clean up by removing all that we added
			Assert.IsTrue(a.RemoveTag("Two"));
			Assert.IsTrue(a.RemoveTag("Three"));
		}
		void AddFour(HashTags<string> a)
		{
			a.SetTag("Food", "Pizza");
			a.SetTag("Drink", "Mountain Dew");
			a.SetTag("Genitals", "Male");
			a.SetTag("Disposition", "Insane");
		}
		
		[Test] 
		public void TestFromEmpty()
		{
			HashTags<string> a = new HashTags<string>();
			TestTheBasics(a, true);
		}

		[Test] 
		public void TestFromFour()
		{
			HashTags<string> a = new HashTags<string>();
			AddFour(a);
			TestTheBasics(a, false);
			
			// There should be four left
			IEnumerator<KeyValuePair<Symbol, string>> e = a.TagEnumerator();
			for (int i = 0; i < 4; i++)
				Assert.IsTrue(e.MoveNext());
			Assert.IsFalse(e.MoveNext());
		}

		[ExpectedException(typeof(ArgumentNullException))]
		public void TestSetNull1()
		{
			HashTags<string> a = new HashTags<string>();
			a.SetTag((Symbol)null, "hello");
		}
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestSetNull2()
		{
			HashTags<string> a = new HashTags<string>();
			a.SetTag("SomethingElseFirst", "hello");
			a.SetTag((string)null, "hi");
		}
	};
}
