using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Loyc.Collections;

namespace Loyc
{
	/// <summary>
	/// An implementation of IAttributes that can hold one attribute before
	/// allocating any memory for a hashtable. It is intended to be used as
	/// a base class but can be used on its own.
	/// </summary>
	public class HashTags<ValueT> : ITags<ValueT>, IDictionary<Symbol, ValueT>, IReadOnlyDictionary<Symbol, ValueT>
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

		public ValueT GetTag(string key) { return GetTag(GSymbol.GetIfExists(key)); }
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

		public void SetTag(string key, ValueT val) { SetTag(GSymbol.Get(key), val); }
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

		public bool RemoveTag(string key) { return RemoveTag(GSymbol.GetIfExists(key)); }
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

		public bool HasTag(string key) { return HasTag(GSymbol.GetIfExists(key)); }
		public bool HasTag(Symbol key)
		{
			if (key == null)
				return false;
			else
				return _cachedAttrKey == key ||
					(_attrs != null && _attrs.ContainsKey(key));
		}
		protected internal IEnumerator<KeyValuePair<Symbol, ValueT>> TagEnumerator()
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
		
		#region IDictionary<Symbol, ValueT> and IReadOnlyDictionary<Symbol, ValueT>
		ValueT IDictionary<Symbol, ValueT>.this[Symbol key]
		{
			get { return GetTag(key); }
			set { SetTag(key, value); }
		}
		ValueT IReadOnlyDictionary<Symbol, ValueT>.this[Symbol key]
		{
			get { return GetTag(key); }
		}
		
		ICollection<Symbol> IDictionary<Symbol, ValueT>.Keys {
			get { return new KeyCollection<Symbol, ValueT>((IReadOnlyDictionary<Symbol, ValueT>)this); }
		}
		IEnumerable<Symbol> IReadOnlyDictionary<Symbol, ValueT>.Keys {
			get { return new KeyCollection<Symbol, ValueT>((IReadOnlyDictionary<Symbol, ValueT>)this); }
		}
		ICollection<ValueT> IDictionary<Symbol, ValueT>.Values {
			get { return new ValueCollection<Symbol, ValueT>((IReadOnlyDictionary<Symbol, ValueT>)this); }
		}
		IEnumerable<ValueT> IReadOnlyDictionary<Symbol, ValueT>.Values {
			get { return new ValueCollection<Symbol, ValueT>((IReadOnlyDictionary<Symbol, ValueT>)this); }
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int TagCount
		{
			get {
				if (_attrs == null)
					return _cachedAttrKey != null ? 1 : 0;
				else
					return _attrs.Count;
			}
		}
		int IReadOnlyCollection<KeyValuePair<Symbol, ValueT>>.Count { get { return TagCount; } }
		int ICollection<KeyValuePair<Symbol, ValueT>>.Count { get { return TagCount; } }
		
		bool ICollection<KeyValuePair<Symbol, ValueT>>.IsReadOnly {
			get { return false; }
		}
		
		bool IDictionary<Symbol, ValueT>.ContainsKey(Symbol key)
			{ return this.HasTag(key); }
		bool IReadOnlyDictionary<Symbol, ValueT>.ContainsKey(Symbol key)
			{ return this.HasTag(key); }
		
		void IDictionary<Symbol, ValueT>.Add(Symbol key, ValueT value)
		{
			if (this.HasTag(key))
				CheckParam.ThrowBadArgument(nameof(key), "The key '{0}' already exists in the IDictionary", key.Name);
			this.SetTag(key, value);
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
				CheckParam.ThrowBadArgument("Insufficient space in supplied array");
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
}
