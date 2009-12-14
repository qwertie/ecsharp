using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Loyc.Utilities;
using Loyc.Runtime;

namespace Loyc.CompilerCore
{
	/// <summary>An implementation of ITags designed for AstNode.</summary>
	/// <remarks>
	/// It is supposed to be possible to clone AstNode quickly; to support extra 
	/// tags, we need to be able to clone the tags quickly too, so it makes sense
	/// to store them in a WList. To make cloning as quick as possible, we derive
	/// from WListProtected. To optimize access to tags, we use the UserByte of
	/// WListProtected to hold the index of the last tag that was used. This 
	/// ensures tags can be accessed quickly by code that only uses a single tag.
	/// </remarks>
	public class TagsInWList<ValueT> : WListProtected<KeyValuePair<Symbol,ValueT>>, IDictionary<Symbol, ValueT>, ITags<ValueT>
	{
		public TagsInWList() { UserByte = 0xFF; }
		public TagsInWList(WListProtected<KeyValuePair<Symbol, ValueT>> original) : base(original, true) { }

		public IDictionary<Symbol, ValueT> Tags { get { return this; } }

		public FVList<KeyValuePair<Symbol, ValueT>>.Enumerator TagEnumerator()
			{ return new FVList<KeyValuePair<Symbol, ValueT>>.Enumerator(InternalVList); }

		public ValueT GetTag(string key) { return GetTag(Symbol.GetIfExists(key)); }
		public ValueT GetTag(Symbol key)
		{
			if (key != null) {
				int count = Count;
				KeyValuePair<Symbol, ValueT> kvp;

				// Try the last-accessed entry
				int hint = UserByte;
				if (hint < count)
					if ((kvp = GetAtDff(hint)).Key == key)
						return kvp.Value;

				// Try the other entries
				for (int i = 0; i < count; i++)
					if (i != hint && (kvp = GetAtDff(i)).Key == key) {
						UserByte = (byte)i;
						return kvp.Value;
					}
			}
			return default(ValueT);
		}
		
		public void SetTag(string key, ValueT val) { SetTag(Symbol.Get(key), val); }
		public void SetTag(Symbol key, ValueT val)
		{
			if (key == null)
				throw new ArgumentException(Localize.From("SetTag: key is null"));
			
			int count = Count;
			KeyValuePair<Symbol, ValueT> kvp;
				
			// Try the last-accessed entry
			int hint = UserByte;
			if (hint < count)
				if ((kvp = GetAtDff(hint)).Key == key) {
					SetAt(hint, new KeyValuePair<Symbol, ValueT>(key, val));
					return;
				}

			// Try the other entries
			for (int i = 0; i < count; i++)
				if (i != hint && (kvp = GetAtDff(hint)).Key == key) {
					UserByte = (byte)i;
					SetAt(i, new KeyValuePair<Symbol, ValueT>(key, val));
					return;
				}
			
			Add(new KeyValuePair<Symbol, ValueT>(key, val));
		}
		
		public bool RemoveTag(string key) { return RemoveTag(Symbol.GetIfExists(key)); }
		public bool RemoveTag(Symbol key)
		{
			if (key != null) {
				int count = Count;
				for (int i = 0; i < count; i++)
					if (GetAtDff(i).Key == key) {
						RemoveAtDff(i);
						return true;
					}
			}
			return false;
		}

		public bool HasTag(string key) { return HasTag(Symbol.GetIfExists(key)); }
		public bool HasTag(Symbol key)
		{
			if (key != null) {
				int count = Count;

				// Try the last-accessed entry
				int hint = UserByte;
				if (hint < count)
					if (GetAtDff(hint).Key == key)
						return true;

				// Try the other entries
				for (int i = 0; i < count; i++)
					if (i != hint && GetAtDff(i).Key == key) {
						UserByte = (byte)i;
						return true;
					}
			}
			return false;
		}

		public int TagCount
		{
			get { return Count; }
		}

		#region IDictionary<Symbol, ValueT> members
		
		ValueT IDictionary<Symbol, ValueT>.this[Symbol key]
		{
			get { return GetTag(key); }
			set { SetTag(key, value); }
		}
		
		ICollection<Symbol> IDictionary<Symbol, ValueT>.Keys
		{
			get { return new KeyCollection<Symbol, ValueT>((IDictionary<Symbol, ValueT>)this); }
		}
		
		ICollection<ValueT> IDictionary<Symbol, ValueT>.Values
		{
			get { return new ValueCollection<Symbol, ValueT>((IDictionary<Symbol, ValueT>)this); }
		}

		int ICollection<KeyValuePair<Symbol, ValueT>>.Count
		{
			get { return Count; }
		}
		
		bool ICollection<KeyValuePair<Symbol, ValueT>>.IsReadOnly
		{
			get { return false; }
		}
		
		bool IDictionary<Symbol, ValueT>.ContainsKey(Symbol key)
			{ return HasTag(key); }
		
		void IDictionary<Symbol, ValueT>.Add(Symbol key, ValueT value)
		{
			if (HasTag(key))
				throw new ArgumentException(string.Format("The key '{0}' already exists in the IDictionary", key.Name));
			SetTag(key, value);
		}
		
		bool IDictionary<Symbol, ValueT>.Remove(Symbol key)
			{ return this.RemoveTag(key); }
		
		bool IDictionary<Symbol, ValueT>.TryGetValue(Symbol key, out ValueT value)
		{
			value = GetTag(key);
			return HasTag(key);
		}
		
		void ICollection<KeyValuePair<Symbol, ValueT>>.Add(KeyValuePair<Symbol, ValueT> item)
			{ ((IDictionary<Symbol, ValueT>)this).Add(item.Key, item.Value); }
		
		void ICollection<KeyValuePair<Symbol, ValueT>>.Clear() { Clear(); }
		
		bool ICollection<KeyValuePair<Symbol, ValueT>>.Contains(KeyValuePair<Symbol, ValueT> item)
			{ return HasTag(item.Key) && GetTag(item.Key).Equals(item.Value); }
		
		void ICollection<KeyValuePair<Symbol, ValueT>>.CopyTo(KeyValuePair<Symbol, ValueT>[] array, int arrayIndex)
			{ CopyTo(array, arrayIndex); }
		
		bool ICollection<KeyValuePair<Symbol, ValueT>>.Remove(KeyValuePair<Symbol, ValueT> item)
		{
			if (GetTag(item.Key).Equals(item.Value))
				return RemoveTag(item.Key);
			else
				return false;
		}
		
		IEnumerator<KeyValuePair<Symbol, ValueT>> IEnumerable<KeyValuePair<Symbol, ValueT>>.GetEnumerator()
			{ return TagEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{ return TagEnumerator(); }

		#endregion
	}
	[TestFixture]
	public class ExtraTagsInWListTests
	{
		private void TestTheBasics(TagsInWList<string> a, bool startsEmpty)
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
		void AddFour(TagsInWList<string> a)
		{
			a.SetTag("Food", "Pizza");
			a.SetTag("Drink", "Mountain Dew");
			a.SetTag("Gender", "Male");
			a.SetTag("Disposition", "Insane");
		}
		
		[Test] 
		public void TestFromEmpty()
		{
			TagsInWList<string> a = new TagsInWList<string>();
			TestTheBasics(a, true);
		}

		[Test] 
		public void TestFromFour()
		{
			TagsInWList<string> a = new TagsInWList<string>();
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
			TagsInWList<string> a = new TagsInWList<string>();
			a.SetTag((Symbol)null, "hello");
		}
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestSetNull2()
		{
			TagsInWList<string> a = new TagsInWList<string>();
			a.SetTag("SomethingElseFirst", "hello");
			a.SetTag((string)null, "hi");
		}
	};
}
