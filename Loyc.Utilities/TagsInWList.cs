using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Collections;

namespace Loyc.Utilities
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
	public class TagsInWList<ValueT> : WListProtected<KeyValuePair<Symbol,ValueT>>, IDictionary<Symbol, ValueT>, ITags<ValueT>, IReadOnlyDictionary<Symbol, ValueT>
	{
		public TagsInWList() { UserByte = 0xFF; }
		public TagsInWList(WListProtected<KeyValuePair<Symbol, ValueT>> original) : base(original, true) { }

		public IDictionary<Symbol, ValueT> Tags { get { return this; } }

		public FVList<KeyValuePair<Symbol, ValueT>>.Enumerator TagEnumerator()
			{ return GetVListEnumerator(); }

		public ValueT GetTag(string key) { return GetTag(GSymbol.GetIfExists(key)); }
		public ValueT GetTag(Symbol key) { return GetTag(key, default(ValueT)); }
		public ValueT GetTag(Symbol key, ValueT defaultValue)
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
			return defaultValue;
		}
		
		public void SetTag(string key, ValueT val) { SetTag(GSymbol.Get(key), val); }
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
		
		public bool RemoveTag(string key) { return RemoveTag(GSymbol.GetIfExists(key)); }
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

		public bool HasTag(string key) { return HasTag(GSymbol.GetIfExists(key)); }
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
		ValueT IReadOnlyDictionary<Symbol, ValueT>.this[Symbol key]
		{
			get { return GetTag(key); }
		}
		
		ICollection<Symbol> IDictionary<Symbol, ValueT>.Keys
		{
			get { return new KeyCollection<Symbol, ValueT>(this); }
		}
		ICollection<ValueT> IDictionary<Symbol, ValueT>.Values
		{
			get { return new ValueCollection<Symbol, ValueT>(this); }
		}
		IEnumerable<Symbol> IReadOnlyDictionary<Symbol, ValueT>.Keys
		{
			get { return new KeyCollection<Symbol, ValueT>(this); }
		}
		IEnumerable<ValueT> IReadOnlyDictionary<Symbol, ValueT>.Values
		{
			get { return new ValueCollection<Symbol, ValueT>(this); }
		}

		int ICollection<KeyValuePair<Symbol, ValueT>>.Count
		{
			get { return Count; }
		}
		int IReadOnlyCollection<KeyValuePair<Symbol, ValueT>>.Count
		{
			get { return Count; }
		}
		
		bool ICollection<KeyValuePair<Symbol, ValueT>>.IsReadOnly
		{
			get { return false; }
		}
		
		bool IDictionary<Symbol, ValueT>.ContainsKey(Symbol key)
			{ return HasTag(key); }
		bool IReadOnlyDictionary<Symbol, ValueT>.ContainsKey(Symbol key)
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
		bool IReadOnlyDictionary<Symbol, ValueT>.TryGetValue(Symbol key, out ValueT value)
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
}
