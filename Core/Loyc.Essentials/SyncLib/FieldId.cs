using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Loyc.SyncLib
{
	/// <summary>
	/// The data type of a field name in SyncLib, which can be implicitly converted from 
	/// a string. It includes both a Name string and an Id integer, in order to support 
	/// both string-keyed fields (e.g. JSON) and int-keyed fields (e.g. Protocol Buffers).
	/// By convention, an Id of -1 indicates that int-keyed implementations of 
	/// <see cref="ISyncManager"/> should choose an ID automatically based on the field 
	/// order.
	/// </summary>
	[DebuggerDisplay(@"{Name} (Id={Id == int.MinValue ? ""none"" : (object) Id})")]
	public struct FieldId
	{
		/// <summary>Represents the case where a data stream lacks a Field ID.
		/// Array elements lack field IDs, and a simple binary format could
		/// omit field IDs from all objects.</summary>
		public static readonly FieldId Missing = new FieldId(null, int.MinValue);

		public FieldId(string? name, int id) { Name = name; Id = id; }

		/// <summary>Name chosen by the user, or null if unspecified.</summary>
		public readonly string? Name;
		/// <summary>Id code chosen by the user, or int.MinValue if unspecified.</summary>
		public readonly int Id;

		public override string? ToString() => Name;

		public static implicit operator string?(FieldId field) => field.Name;
		public static implicit operator FieldId(string? name) => new FieldId(name, int.MinValue);
		public static implicit operator FieldId((string?, int) p) => new FieldId(p.Item1, p.Item2);
		/// <summary>
		/// This conversion from <see cref="Symbol"/> to <see cref="FieldId"/> must only 
		/// be used for symbols in a private <see cref="SymbolPool"/>, not global symbols.
		/// A global symbol has an ID that can change each time a program runs, which 
		/// makes the ID useless for serialization or deserialization.
		/// </summary>
		public static explicit operator FieldId(Symbol? name) => 
			name != null ? new FieldId(name.Name, name.Id) : new FieldId(null, int.MinValue);
	}
}
