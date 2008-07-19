//
// This small utility permanently assigns an integer to any number of strings 
// at run-time.
// 
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace Loyc.Runtime
{
	/// <summary>
	/// Symbols are used like a global, extensible enumeration. They typically
	/// offer better performance than interned strings, because a callee 
	/// receiving a string typically has to call String.Intern() or 
	/// String.IsInterned() to guarantee that the string is interned, whereas 
	/// Symbols are guaranteed to be interned already. Also, no special '=='
	/// operator is needed because equality is defined as reference equality;
	/// and the Equals() function is likewise very fast. A Symbol's ToString() 
	/// function returns the symbol name prefixed with a colon (:), following 
	/// the convention of the Ruby language, from which I got the idea of 
	/// Symbols in the first place. The Name property returns the original 
	/// string without the colon.
	/// </summary>
	public sealed class Symbol
	{
		#region Public static members

		static public Symbol Get(string name)
		{
			Symbol sym;
			if (name == null)
				return null;
			else lock (typeof(Symbol)) {
				if (_map.TryGetValue(name, out sym))
					return sym;
				else {
					name = string.Intern(name);
					int id = _list.Count;
					sym = new Symbol(id, name);
					_list.Add(sym);
					_map.Add(name, sym);
					return sym;
				}
			}
		}
		static public Symbol GetIfExists(string name)
		{
			Symbol sym;
			if (name == null)
				return null;
			else lock (typeof(Symbol)) {
				_map.TryGetValue(name, out sym);
				return sym;
			}
		}
		[MethodImpl(MethodImplOptions.Synchronized)]
		static public Symbol GetById(int id)
		{
			if (id < 0 || id >= _list.Count)
				throw new ArgumentException("Invalid Symbol ID", "id");
			else
				return _list[id];
		}

		static public readonly Symbol Empty;

		#endregion

		#region Public instance members

		public int Id { [DebuggerStepThrough] get { return _id; } }
		public string Name { [DebuggerStepThrough] get { return _name; } }
		public override int GetHashCode() { return _id; }
		[DebuggerStepThrough]
		public override string ToString()
		{
			if (_id == 0)
				return string.Empty;
			else
				return ":" + Name;
		}
		[DebuggerStepThrough]
		public override bool Equals(object b) { return ReferenceEquals(this, b); }
		
		#endregion

		#region Protected & private members

		private readonly int _id;
		private readonly string _name;
		private Symbol(int id, string name) { _id = id; _name = name; }

		static private List<Symbol> _list;
		static private Dictionary<string, Symbol> _map;
		static Symbol()
		{
			_map = new Dictionary<string, Symbol>();
			_list = new List<Symbol>();
			Empty = Get(string.Empty);
			Debug.Assert(Get(string.Empty).Id == 0);
		}

		static private int TotalSymbolCount
		{ 
			get {
				lock (typeof(Symbol)) {
					return _list.Count; 
				}
			}
		}

		#endregion
	}

	[TestFixture]
	public class SymbolTests
	{
		[Test]
		public void BasicChecks()
		{
			Assert.AreEqual(null, Symbol.Get(null));
			Assert.AreEqual(0, Symbol.Get("").Id);

			Symbol foo = Symbol.Get("Foo");
			Symbol bar = Symbol.Get("Bar");
			Assert.AreNotEqual(foo, bar);
			Assert.AreEqual(":Foo", foo.ToString());
			Assert.AreEqual(":Bar", bar.ToString());
			Assert.AreEqual("Foo", foo.Name);
			Assert.AreEqual("Bar", bar.Name);
			Assert.IsNotNull(string.IsInterned(foo.Name));
			Assert.IsNotNull(string.IsInterned(bar.Name));

			Symbol foo2 = Symbol.Get("Foo");
			Symbol bar2 = Symbol.Get("Bar");
			Assert.AreNotEqual(foo2, bar2);
			Assert.AreEqual(foo, foo2);
			Assert.AreEqual(bar, bar2);
			Assert.That(object.ReferenceEquals(foo.Name, foo2.Name));
			Assert.That(object.ReferenceEquals(bar.Name, bar2.Name));
		}
		[Test]
		public void Exception()
		{
			try {
				Symbol.GetById(6543210);
				Assert.Fail("Expected ArgumentException");
			} catch (ArgumentException) { }
		}
	}
}

