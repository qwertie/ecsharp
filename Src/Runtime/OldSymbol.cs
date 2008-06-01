using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace Loyc.Runtime 
{
	public struct Symbol
	{
		public int Id;
		public Symbol(int id) { Id = id; }
		public Symbol(string s) { Id = SymbolMapper.Lookup(s); }
		public string Name { get { return SymbolMapper.NameOf(Id); } }
		public string SafeName { get {
			if (Id == 0)
				return String.Empty;
			string s = SymbolMapper.NameOrNull(Id); 
			if (s != null)
				return s;
			else
				return string.Format("Invalid#{0}", Id);
		} }

		public bool IsNull { get { return Id == 0; } }
		public static readonly Symbol Null = new Symbol();
		
		public override string ToString() {
			if (Id == 0)
				return String.Empty;
			return ":" + SafeName; 
		}
		public override bool Equals(object b) { return base.Equals(b); }
		public override int GetHashCode() { return Id; }
		
		public static bool operator ==(Symbol a, Symbol b) { return a.Id == b.Id; }
		public static bool operator !=(Symbol a, Symbol b) { return a.Id != b.Id; }
	}

	public static class SymbolMapper
	{
		[MethodImpl(MethodImplOptions.Synchronized)]
		static public string NameOf(int symbolId)
			{ return _list[symbolId]; }

		[MethodImpl(MethodImplOptions.Synchronized)]
		static public string NameOrNull(int symbolId) 
		{
			if (symbolId < 0 || symbolId >= _list.Count)
				return null;
			return _list[symbolId];
		}
		static public int Lookup(string name) 
		{
			int id;
			if (name == null)
				return 0;
			else lock (typeof(SymbolMapper)) {
				if (_map.TryGetValue(name, out id))
					return id;
				else {
					id = _list.Count;
					_list.Add(name);
					_map.Add(name, id);
					return id;
				}
			}
		}
		static SymbolMapper() { 
			_map = new Dictionary<string, int>();
			_list = new List<string>();
			_list.Add(null); 
			_list.Add(""); 
			_map.Add("", 1); 
		}
		static private List<string> _list;
		static private Dictionary<string, int> _map;
	}
	[TestFixture] public class SymbolTests
	{
		[Test] public void SanityChecks()
		{
			Assert.IsTrue(Symbol.Null.IsNull);
			Assert.IsTrue((new Symbol()).IsNull);
			Assert.AreEqual(0, SymbolMapper.Lookup(null));
		}
		[Test] public void BasicChecks()
		{
			Symbol foo = new Symbol("Foo");
			Symbol bar = new Symbol("Bar");
			Assert.AreNotEqual(foo, bar);
			Assert.AreEqual(":Foo", foo.ToString());
			Assert.AreEqual(":Bar", bar.ToString());
			Assert.AreEqual("Foo", foo.Name);
			Assert.AreEqual("Bar", bar.Name);

			Symbol bad;
			bad.Id = 6543210;
			Assert.AreEqual(bad.SafeName, "Invalid#6543210");
			Assert.AreEqual(bad.ToString(), ":Invalid#6543210");

			Symbol foo2 = new Symbol("Foo");
			Symbol bar2 = new Symbol("Bar");
			Assert.AreNotEqual(foo2, bar2);
			Assert.AreEqual(foo, foo2);
			Assert.AreEqual(bar, bar2);
		}
		[ExpectedException(typeof(Exception))]
		public void Exc1()
		{
			SymbolMapper.NameOf(6543210);
		}
		[ExpectedException(typeof(Exception))]
		public void Exc2()
		{
			Symbol s;
			s.Id = -1;
			String name = s.Name;
		}
	}
}

