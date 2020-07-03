/*
 Copyright 2009-2012 David Piepgrass

 Permission is hereby granted, free of charge, to any person
 obtaining a copy of this software and associated documentation
 files (the "Software"), to deal in the Software without
 restriction, including without limitation the rights to use,
 copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the
 Software is furnished to do so, subject to the following
 conditions:

 The above copyright notice and this permission notice shall be
 included in all copies or substantial portions of the Software's
 source code.

 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 OTHER DEALINGS IN THE SOFTWARE.

 The above license applies to Symbol.cs only. Most of Loyc uses the LGPL license.
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Loyc.Collections;

namespace Loyc
{
	/// <summary>Represents a symbol, which is a singleton string that supports fast 
	/// comparisons and extensible enums.</summary>
	/// <remarks>
	/// Call GSymbol.Get() to create a Symbol from a string, or GSymbol.GetIfExists()
	/// to find a Symbol that has already been created.
	/// <para/>
	/// Symbols can be used like a global, extensible enumeration. Comparing symbols
	/// is as fast as comparing two integers; this is because '==' is not
	/// overloaded--equality is defined as reference equality, as there is only one
	/// instance of a given Symbol.
	/// <para/>
	/// Symbols can also be produced in namespaces called "pools". Two Symbols with
	/// the same name, but in different pools, are considered to be different
	/// symbols. Using a derived class D of Symbol and a SymbolPool&lt;D&gt;,
	/// you can make Symbols that are as type-safe as enums.
	/// <para/>
	/// A Symbol's ToString() function formerly returned the symbol name prefixed 
	/// with a colon (:), following the convention of the Ruby language, from which 
	/// I got the idea of Symbols in the first place. However, since Symbols are 
	/// commonly used for extensible enums, I decided it was better that ToString()
	/// return just the Name alone, which makes Symbol more suitable as a drop-in 
	/// replacement for enums.
	/// <para/>
	/// Symbols are also useful in compilers and Loyc trees, where there may be a 
	/// performance advantage in comparing identifiers by reference rather than
	/// character-by-character.
	/// </remarks>
	[DebuggerDisplay("{Name}")]
	public class Symbol : IReferenceEquatable, IComparable<Symbol>, IEquatable<Symbol>
	{
		#region Public instance members

		/// <summary>Gets the name of the Symbol.</summary>
		public string Name     { [DebuggerStepThrough] get { return _name; } }
		
		/// <summary>Gets the <see cref="SymbolPool"/> in which this Symbol was created.</summary>
		public SymbolPool Pool { [DebuggerStepThrough] get { return _pool; } }

		/// <summary>Returns true if this symbol is in the global pool (<see cref="GSymbol.Pool"/>).</summary>
		public bool IsGlobal   { [DebuggerStepThrough] get { return _pool == GSymbol.Pool; } }
		
		/// <summary>Returns a numeric ID for the Symbol.</summary>
		/// <remarks>Normally the Id is auto-assigned and the Id corresponding to 
		/// a particular Name may vary between different runs of the same program. 
		/// However, <see cref="SymbolPool.Get(string, int)"/> can be used to 
		/// assign a specific Id to a Name when setting up a new pool.</remarks>
		public int Id          { [DebuggerStepThrough] get { return _id; } }
		
		[DebuggerStepThrough]
		public override string ToString() { return Name; }

		public override int GetHashCode() { return 5432 + _id ^ (_pool.PoolId << 16); }
		public override bool Equals(object b) { return ReferenceEquals(this, b); }
		public          bool Equals(Symbol b) { return this == b; }
		
		public int CompareTo(Symbol other)
		{
			if (other == this) return 0;
			if (other == null) return 1;
			return Name.CompareTo(other.Name);
		}

		#endregion

		#region Protected & private members

		private readonly int _id;
		private readonly string _name;
		private readonly SymbolPool _pool;
		
		/// <summary>For internal use only. Call GSymbol.Get() instead!</summary>
		internal Symbol(int id, string name, SymbolPool pool) 
			{ _id = id; _name = name; _pool = pool; }
		
		/// <summary>For use by a derived class to produce a statically-typed 
		/// enumeration in a private pool. See the example under SymbolPool 
		/// (of SymbolEnum)</summary>
		/// <param name="prototype">A strictly temporary Symbol that is used
		/// to initialize this object. The derived class should discard the
		/// prototype after calling this constructor.</param>
		protected Symbol(Symbol prototype)
		{
			_id = prototype._id;
			_name = prototype._name;
			_pool = prototype._pool;
		}

		#endregion

		public static explicit operator Symbol(string s) => s == null ? null : GSymbol.Get(s);
		public static explicit operator string(Symbol s) => s.Name;
		// TODO: switch to UString dictionary to avoid the need to call ToString().
		public static explicit operator Symbol(UString s) => GSymbol.Get(s);
		public static explicit operator UString(Symbol s) => new UString(s.Name);
		
		/// <summary>Alias for <see cref="GSymbol.Get(string)"/>. This function was 
		/// introduced to match the equivalent ES6 API <c>Symbol.for("string")</c></summary>
		public static Symbol For(string s) { return GSymbol.Get(s); }
	}

	/// <summary>This class produces global symbols.</summary>
	/// <remarks>
	/// Call GSymbol.Get() to create a Symbol from a string, or GSymbol.GetIfExists()
	/// to find a Symbol that has already been created.
	/// <para/>
	/// Symbols in the global pool are weak-referenced to allow garbage collection.
	/// </remarks>
	public class GSymbol
	{
		static public Symbol Get(UString name) => Pool.Get(name);
		static public Symbol GetIfExists(UString name) => Pool.GetIfExists(name);
		static public Symbol GetById(int id) { return Pool.GetById(id); }

		static public readonly Symbol Empty;
		static public readonly SymbolPool Pool;

		static GSymbol()
		{
			Pool = new SymbolPool(0, false, 0);
			Empty = Pool.Get("");
			Debug.Assert(Empty.Id == 0 && Empty.Name == "");
			Debug.Assert(((Symbol)Empty).Pool == Pool);
		}
	}

	/// <summary>A collection of <see cref="Symbol"/>s.</summary>
	/// <remarks>
	/// There is one global symbol pool (<c>GSymbol.Pool</c>) and you can create an 
	/// unlimited number of private pools, each with an independent namespace.
	/// <para/>
	/// Methods of this class are synchronized, so a SymbolPool can be used from
	/// multiple threads.
	/// <para/>
	/// By default, SymbolPool uses weak references to refer to Symbols, so they 
	/// can be garbage-collected when no longer in use. When creating a private 
	/// pool you can tell the SymbolPool constructor to use strong references 
	/// instead, which ensures that none of the symbols disappear, but risks a 
	/// memory leak if the pool itself is never garbage-collected. Strong 
	/// references also require less memory and may be slightly faster.
	/// <para/>
	/// By default, all Symbol are given non-negative IDs. GSymbol.Empty (whose 
	/// Name is "") has an Id of 0, but in a private pool, "" is not treated 
	/// differently than any other symbol so a new ID will be allocated for it.
	/// </remarks>
	public class SymbolPool : IAutoCreatePool<UString, Symbol>, IReadOnlyCollection<Symbol>
	{
		protected internal IDictionary<int, Symbol> _idMap; // created on-demand
		protected internal IDictionary<UString, Symbol> _map;
		protected internal WeakValueDictionary<UString, Symbol> _weakMap; // same as _map, or null
		protected internal int _nextId;
		protected readonly int _poolId;
		protected static int _nextPoolId = 1;

		public static SymbolPool @new() { return @new(1, false, _nextPoolId++); }
		public static SymbolPool @new(int firstID, bool useStrongRefs) { return @new(firstID, false, _nextPoolId++); }
		/// <summary>Initializes a new Symbol pool.</summary>
		/// <param name="firstID">The first Symbol created in the pool will have 
		/// the specified ID number, and IDs will proceed downward from there.</param>
		/// <param name="useStrongRefs">True to use strong references to the 
		/// Symbols in the pool, false to use WeakReferences that allow garbage-
		/// collection of individual Symbols.</param>
		/// <param name="poolId">Numeric ID of the pool (affects the HashCode of 
		/// Symbols from the pool)</param>
		public static SymbolPool @new(int firstID, bool useStrongRefs, int poolId)
		{
			return new SymbolPool(firstID, useStrongRefs, poolId);
		}

		public SymbolPool() : this(1, false, _nextPoolId++) { }
		public SymbolPool(int firstID) : this(firstID, false, _nextPoolId++) { }
		protected internal SymbolPool(int firstID, bool useStrongRefs, int poolId)
		{
			if (useStrongRefs)
				_map = new Dictionary<UString, Symbol>();
			else
				_map = _weakMap = new WeakValueDictionary<UString, Symbol>();
			_nextId = firstID;
			_poolId = poolId;
		}
		public bool UsesStrongReferences
		{
			get { return _weakMap == null; }
		}

		/// <summary>Gets a symbol from this pool, or creates it if it does not 
		/// exist in this pool already.</summary>
		/// <param name="name">Name to find or create.</param>
		/// <returns>A symbol with the requested name, or null if the name was null.</returns>
		/// <remarks>
		/// If Get("foo") is called in two different pools, two Symbols will be 
		/// created, each with the Name "foo" but not necessarily with the same 
		/// IDs. Note that two private pools re-use the same IDs, but this 
		/// generally doesn't matter, as Symbols are compared by reference, not by 
		/// ID.
		/// </remarks>
		public Symbol Get(UString name)
		{
			Get(name, out Symbol result);
			return result;
		}

		/// <inheritdoc cref="Get(string)"/>
		public Symbol this[UString name] { get { return Get(name); } }
		
		/// <summary>Creates a Symbol in this pool with a specific ID, or verifies 
		/// that the requested Name-Id pair is present in the pool.</summary>
		/// <param name="name">Name to find or create.</param>
		/// <param name="id">Id that must be associated with that name.</param>
		/// <exception cref="ArgumentNullException">name was null.</exception>
		/// <exception cref="ArgumentException">The specified Name or Id is already 
		/// in use, but the Name and Id do not match. For example, Get("a", 1) throws 
		/// this exception if a Symbol with Id==1 is not named "a", or a Symbol with
		/// Name=="a" does not have Id==1.</exception>
		/// <returns>A symbol with the requested Name and Id.</returns>
		public Symbol Get(UString name, int id)
		{
			Symbol result;
			Get(name, id, out result);
			return result;
		}
		
		private Symbol AddSymbol(Symbol sym)
		{
			_map.Add(sym.Name, sym);
			if (_idMap != null)
				_idMap.Add(sym.Id, sym);
			return sym;
		}

		/// <summary>Workaround for lack of covariant return types in C#</summary>
		protected virtual void Get(UString name, out Symbol sym)
		{
			if ((sym = GetIfExists(name)) == null)
				lock (_map) {
					if ((sym = GetIfExists(name)) != null)
						// It is possible for another thread to have added 'name' to
						// '_map' while we were waiting to acquire the lock.
						// If that has happened, then we want to return now.
						return;

					if (_idMap != null)
						while (_idMap.ContainsKey(_nextId))
							_nextId++;

					sym = AddSymbol(NewSymbol(_nextId, name.ToString()));
					_nextId++;
				}
		}

		/// <summary>Workaround for lack of covariant return types in C#</summary>
		protected virtual void Get(UString name, int id, out Symbol sym)
		{
			if ((sym = GetIfExists(name)) != null) {
				if (sym.Id != id)
					throw new ArgumentException("Symbol already exists with a different ID than requested.");
			} else lock (_map) {
				AutoCreateIdMap();
				if (_idMap.ContainsKey(id))
					throw new ArgumentException("ID is already assigned to a different name than requested.");
				sym = AddSymbol(NewSymbol(id, name.ToString()));
			}
		}
		private void AutoCreateIdMap()
		{
			if (_idMap == null)
			{
				if (UsesStrongReferences)
					_idMap = new Dictionary<int, Symbol>();
				else
					_idMap = new WeakValueDictionary<int, Symbol>();
				foreach (Symbol sym in _map.Values)
					_idMap[sym.Id] = sym;
			}
		}
		
		/// <summary>Factory method to create a new Symbol.</summary>
		protected virtual Symbol NewSymbol(int id, string name)
		{
			return new Symbol(id, name, this);
		}

		/// <summary>Gets a symbol from this pool, if the name exists already.</summary>
		/// <param name="name">Symbol Name to find</param>
		/// <returns>Returns the existing Symbol if found; returns null if the name 
		/// was not found, or if the name itself was null.</returns>
		public Symbol GetIfExists(UString name)
		{
			Symbol sym;
			lock (_map) {
				_map.TryGetValue(name, out sym);
				
				if (_weakMap != null)
					if (_weakMap.AutoCleanup())
						if (_idMap != null)
							((WeakValueDictionary<int, Symbol>)_idMap).Cleanup();

				return sym;
			}
		}
		
		/// <summary>Gets a symbol from the global pool, if it exists there already;
		/// otherwise, creates a Symbol in this pool.</summary>
		/// <param name="name">Name of a symbol to get or create</param>
		/// <returns>A symbol with the requested name</returns>
		[Obsolete("I don't think this is being used. Let me know if I'm mistaken. qwertie256@gmail.com")]
		public Symbol GetGlobalOrCreateHere(string name)
		{
			Symbol sym = GSymbol.Pool.GetIfExists(name);
			return sym ?? Get(name);
		}

		/// <summary>Gets a symbol by its ID, or null if there is no such symbol.</summary>
		/// <param name="id">ID of a symbol. If this is a private pool and the 
		/// ID does not exist in the pool, the global pool is searched instead.
		/// </param>
		/// <remarks>
		/// GetById() uses a dictionary of ID numbers to Symbols for fast lookup.
		/// To save time and memory, this dictionary is not created until either 
		/// GetById() or Get(string name, int id) is called.
		/// </remarks>
		/// <returns>The requested Symbol, or null if not found.</returns>
		public Symbol GetById(int id)
		{
			lock(_map) {
				AutoCreateIdMap();
				Symbol sym;
				if (_idMap.TryGetValue(id, out sym)) {
					Debug.Assert(sym != null);
					return sym;
				}
			}
			if (this != GSymbol.Pool)
				return GSymbol.Pool.GetById(id);
			return null;
		}

		/// <summary>Returns the number of Symbols created in this pool.</summary>
		public int TotalCount
		{ 
			get { return _map.Count; }
		}

		protected internal int PoolId
		{
			get { return _poolId; }
		}

		#region ISource<Symbol> Members

		public IEnumerator<Symbol> GetEnumerator()
		{
			lock (_map) {
				foreach (Symbol symbol in _map.Values)
					yield return symbol;
			}
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		int IReadOnlyCollection<Symbol>.Count { get { return TotalCount; } }

		#endregion
	}

	/// <summary>This type of SymbolPool helps create more strongly typed Symbols
	/// that simulate enums, but provide extensibility. Specifically, it 
	/// creates SymbolE objects, where SymbolE is some derived class of Symbol.
	/// </summary>
	/// <typeparam name="SymbolE">
	/// A derived class of Symbol that owns the pool. See the example below.
	/// </typeparam>
	/// <example>
	/// public class ShapeType : Symbol
	/// {
	///     private ShapeType(Symbol prototype) : base(prototype) { }
	///     public static new readonly SymbolPool&lt;ShapeType> Pool 
	///                          = new SymbolPool&lt;ShapeType>(p => new ShapeType(p));
	///
	///     public static readonly ShapeType Circle  = Pool.Get("Circle");
	///     public static readonly ShapeType Rect    = Pool.Get("Rect");
	///     public static readonly ShapeType Line    = Pool.Get("Line");
	///     public static readonly ShapeType Polygon = Pool.Get("Polygon");
	/// }
	/// </example>
	public class SymbolPool<SymbolE> : SymbolPool, IEnumerable<SymbolE>
		where SymbolE : Symbol
	{
		public delegate SymbolE SymbolFactory(Symbol prototype);
		protected SymbolFactory _factory;
		
		public SymbolPool(SymbolFactory factory)
		{
			_factory = factory;
		}
		public SymbolPool(SymbolFactory factory, int firstID) : base(firstID)
		{
			_factory = factory;
		}
		public new SymbolE Get(UString name)
		{
			return (SymbolE)base.Get(name);
		}
		public new SymbolE Get(UString name, int id)
		{
			return (SymbolE)base.Get(name, id);
		}
		protected override Symbol NewSymbol(int id, string name)
		{
 			return _factory(new Symbol(id, name, this));
		}
		public new SymbolE GetIfExists(UString name)
		{
			return (SymbolE)base.GetIfExists(name);
		}
		public new SymbolE GetById(int id)
		{
			return (SymbolE)base.GetById(id);
		}
		
		#region IEnumerable<Symbol> Members

		public new IEnumerator<SymbolE> GetEnumerator()
		{
			lock (_map)
			{
				foreach (SymbolE symbol in _map.Values)
					yield return symbol;
			}
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}

