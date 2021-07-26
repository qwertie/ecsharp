// Generated from ISyncManager.ecs by LeMP custom tool. LeMP version: 30.1.0.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using Loyc;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.SyncLib.Impl;
using Loyc.Collections.MutableListExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;

#nullable enable

namespace Loyc.SyncLib
{
	public delegate T SyncObjectFunc<in SyncManager, T>(SyncManager sync, [AllowNull] T value);
	public delegate T SyncFieldFunc<T>(Symbol? name, [AllowNull] T value);

	/// <summary>This is the central interface of Loyc.SyncLib. To learn more, please 
	/// visit the web site: http://loyc.net/serialization </summary>
	public interface ISyncManager
	{
		/// <summary>Indicates what kind of synchronizer this is: one that saves
		/// data, one that loads data, or one that saves a schema.</summary>
		SyncMode Mode { get; }

		/// <summary>Returns true if the current <see cref="Mode"/> is 
		/// <see cref="SyncMode.Saving"/>, <see cref="SyncMode.Query"/> or 
		/// <see cref="SyncMode.Merge"/>. If your synchronizer method behaves 
		/// differently when it is loading than when it is saving, you should
		/// almost always get this property rather than testing whether 
		/// <c>Mode == SyncMode.Saving</c>, because if the current mode is 
		/// Query or Merge, your synchronizer method should usually do the same
		/// thing it does when saving.</summary>
		bool IsSaving { get; }

		/// <summary>Indicates that the serialized format has some kind of schema that 
		/// enables fields to be read in a different order than they were written
		/// (e.g. JSON, Protobuf). If this field is false, fields must be read in
		/// the same order they were written, and omitting fields is not allowed
		/// (e.g. you cannot skip over a null field without saving it).</summary>
		/// <remarks>If this property is false, the data may not have any recorded 
		/// structure, and failure to read the correct fields in the correct order 
		/// tends to give you "garbage" results.</remarks>
		bool SupportsReordering { get; }

		/// <summary>Returns true if the <see cref="ISyncManager"/> supports 
		/// deduplication of objects and cyclic object graphs. Note: all standard 
		/// implementations of this interface do support deduplication and cyclic
		/// object graphs.</summary>
		bool SupportsDeduplication { get; }

		/// <summary>Indicates that the properties of the current sub-object do not
		/// have names because the basic <see cref="SubObjectMode"/> is either
		/// <see cref="SubObjectMode.Tuple"/> or <see cref="SubObjectMode.List"/>.
		/// In this case, <see cref="SupportsReordering"/> is irrelevant, since 
		/// fields do not have names or ID numbers.</summary>
		bool IsInsideList { get; }

		/// <summary>Indicates that the serialized format uses field ID numbers rather 
		/// than field names (e.g. Protocol Buffers). When using strings or global 
		/// symbols, the ID is indicated implcitly via field order: the first field 
		/// read or written has ID=1, the second has ID=2, etc. If you need to customize 
		/// the field numbers, you can do so by creating a private 
		/// <see cref="Loyc.SymbolPool"/> and creating symbols with custom ID numbers.
		/// </summary>
		bool NeedsIntegerIds { get; }

		/// <summary>If a list is being scanned and the current object can detect
		/// the end of the list (e.g. the mode is <see cref="SyncMode.Loading"/>),
		/// this property returns a boolean value: true at the end of the list and
		/// false otherwise. In all other cases, null is returned.</summary>
		/// <remarks>
		/// <ul>
		/// <li>In Loading mode, the manager knows the list length if the list has 
		///     variable length, in which case true or false is returned.</li>
		/// <li>In Saving mode, the caller knows the list length but the manager does 
		///     not, so this property always returns null.</li>
		/// <li>In Schema mode, the no list actually exists, but the manager typically 
		///     pretends that the list's length is 1.</li>
		/// <li>In Query and Merge modes, the manager doesn't know the list length but 
		///     it may have a maximum list length. In this case, this property returns
		///     null at the beginning, then true when the limit is reached.</li>
		/// </ul>
		/// </remarks>
		bool? ReachedEndOfList { get; }

		/// <summary>If a variable-length list is being scanned in Loading mode, this 
		/// property returns either the list length (if known) or the minimum list 
		/// length (if the total length is not known before reading the list).
		/// This property is null if the list length is unknown (e.g. Saving mode), 
		/// or if a variable-length list is not being scanned.</summary>
		/// <remarks>
		/// Some data formats use length-prefixed lists, in which case the list length 
		/// is known from the begining. Other formats (such as JSON) use a delimiter to 
		/// mark the end of the list, so the list length may not be known until the end
		/// is reached. In that case, this property typically returns 0 or 1.
		/// </remarks>
		int? MinimumListLength { get; }

		/// <summary>Some serializers do not support this method (see remarks).
		/// If `Mode == SyncMode.Loading`, this method returns true if the current 
		/// object contains the specified field and false otherwise. If 
		/// `Mode == SyncMode.Saving`, this method always returns null.
		/// If `Mode == SyncMode.Schema`, this method always returns null and, in
		/// addition, causes the synchronizer to assume that the field you asked 
		/// about is optional.</summary>
		/// <remarks>
		/// Generally, this method can provide useful information only if 
		/// <see cref="SupportsReordering"/> is true, `Mode == SyncMode.Loading`, and
		/// <see cref="IsInsideList"/> is false. If <see cref="SupportsReordering"/> 
		/// is false, this method generally cannot know whether a field exists, and 
		/// it returns null unless it can determine that the next field is definitely 
		/// not the one you asked about (e.g. if the end of the current object or 
		/// list has been reached.)
		/// <para/>
		/// In addition, if <see cref="NeedsIntegerIds"/> is true then the Symbol must
		/// be in a private <see cref="SymbolPool"/> and it needs to have had an ID 
		/// number manually assigned to it.</remarks>
		bool? HasField(Symbol name);

		/// <summary>Returns the number of parent objects of the current object being
		/// loaded or saved. This property is zero if the root object is being loaded
		/// or saved.</summary>
		int Depth { get; }
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		bool Sync(Symbol? name, bool savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		sbyte Sync(Symbol? name, sbyte savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		byte Sync(Symbol? name, byte savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		short Sync(Symbol? name, short savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		ushort Sync(Symbol? name, ushort savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		int Sync(Symbol? name, int savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		uint Sync(Symbol? name, uint savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		long Sync(Symbol? name, long savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		ulong Sync(Symbol? name, ulong savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		float Sync(Symbol? name, float savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		double Sync(Symbol? name, double savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		decimal Sync(Symbol? name, decimal savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		BigInteger Sync(Symbol? name, BigInteger savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		char Sync(Symbol? name, char savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		string Sync(Symbol? name, string savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		int Sync(Symbol? name, int savable, int bits, bool signed = true);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		long Sync(Symbol? name, long savable, int bits, bool signed = true);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		BigInteger Sync(Symbol? name, BigInteger savable, int bits, bool signed = true);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		bool? SyncNullable(Symbol? name, bool? savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		sbyte? SyncNullable(Symbol? name, sbyte? savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		byte? SyncNullable(Symbol? name, byte? savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		short? SyncNullable(Symbol? name, short? savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		ushort? SyncNullable(Symbol? name, ushort? savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		int? SyncNullable(Symbol? name, int? savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		uint? SyncNullable(Symbol? name, uint? savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		long? SyncNullable(Symbol? name, long? savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		ulong? SyncNullable(Symbol? name, ulong? savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		float? SyncNullable(Symbol? name, float? savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		double? SyncNullable(Symbol? name, double? savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		decimal? SyncNullable(Symbol? name, decimal? savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		BigInteger? SyncNullable(Symbol? name, BigInteger? savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		char? SyncNullable(Symbol? name, char? savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		string? SyncNullable(Symbol? name, string? savable);
		/// <summary>This method is used by Sync() extension methods to read 
		///   or write an array. Users don't need to call it.</summary>
		/// <remarks>Full documentation is located in source code (ISyncManager.ecs)</remarks>
		List? SyncListBoolImpl<Scanner, List, ListBuilder>
		(Symbol? name, Scanner scanner, List? saving, ListBuilder builder, SubObjectMode mode, int tupleLength = -1)
		
		 where Scanner: IScanner<bool> where ListBuilder: IListBuilder<List, bool>;
		/// <summary>This method is used by Sync() extension methods to read 
		///   or write an array. Users don't need to call it.</summary>
		/// <remarks>Full documentation is located in source code (ISyncManager.ecs)</remarks>
		List? SyncListCharImpl<Scanner, List, ListBuilder>
		(Symbol? name, Scanner scanner, List? saving, ListBuilder builder, SubObjectMode mode, int tupleLength = -1)
		
		 where Scanner: IScanner<char> where ListBuilder: IListBuilder<List, char>;
		/// <summary>This method is used by Sync() extension methods to read 
		///   or write an array. Users don't need to call it.</summary>
		/// <remarks>Full documentation is located in source code (ISyncManager.ecs)</remarks>
		List? SyncListByteImpl<Scanner, List, ListBuilder>
		(Symbol? name, Scanner scanner, List? saving, ListBuilder builder, SubObjectMode mode, int tupleLength = -1)
		
		 where Scanner: IScanner<byte> where ListBuilder: IListBuilder<List, byte>;
		/// <summary>Sets the "current object" reference. This method must be called 
		///   when deserializing object graphs with cycles (see remarks).</summary>
		/// <remarks>
		/// To understand why this property is needed to help deserialize object 
		/// graphs that contain cycles, consider a Person class that has a reference 
		/// to all the Siblings of the person:
		/// <code>
		///   class Person
		///   {
		///       public string Name;
		///       public int Age;
		///       public Person[] Siblings;
		///   }
		/// </code>
		/// If Jack and Jill are siblings then Jack has a reference to Jill, and Jill 
		/// has a reference back to Jack. A naive implementation of a synchronization 
		/// function for Person might look like this:
		/// <code>
		///   public Person SyncPerson(ISyncManager sync, Person obj)
		///   {
		///       obj ??= new Person();
		///       obj.Name     = sync.SyncNullable("Name", obj.Name);
		///       obj.Age      = sync.Sync("Age", obj.Age);
		///       obj.Siblings = sync.SyncList("Siblings", obj.Siblings, SyncPerson);
		///   }
		/// </code>
		/// But this function cannot load a Person correctly! To understand this,
		/// let's think about what <c>SyncList</c> does: it reads a list of Persons
		/// (synchronously), and returns a <c>Person[]</c>. But each Person in that 
		/// array contains a reference back to the current person. If Jack is being 
		/// loaded, then the <c>Person[]</c> contains Jill, which has a reference back 
		/// to Jack.
		/// <para/>
		/// But it is impossible for <c>SyncList</c> to return an object that has a 
		/// reference to Jack, because the reference to Jack only exists in the local 
		/// variable <c>obj</c>. So as the <c>SyncList</c> method deserializes Jill, 
		/// Jill's synchronizer must fail while reading the list of siblings, because
		/// no reference to Jack is available.
		/// <para/>
		/// To fix this, set <c>CurrentObject</c> before calling <c>SyncList</c>:
		/// <code>
		///   public Person SyncPerson(ISyncManager sync, Person obj)
		///   {
		///       sync.CurrentObject = obj ??= new Person();
		///       obj.Name     = sync.SyncNullable("Name", obj.Name);
		///       obj.Age      = sync.Sync("Age", obj.Age);
		///       obj.Siblings = sync.SyncList("Siblings", obj.Siblings, SyncPerson);
		///   }
		/// </code>
		/// If the current type needs deduplication, but is not involved in cyclic 
		/// object graphs, then setting <see cref="CurrentObject"/> is optional.
		/// </remarks>
		object CurrentObject { set; }

		/// <summary>Attempts to begin reading or writing a sub-object.
		///   Be sure to read the remarks.</summary>
		/// <param name="name">The name of the property being loaded or saved in
		///   the current object.</param>
		/// <param name="childKey">If the current Mode is Saving or Query, this 
		///   must be a reference that represents the object being saved, or null 
		///   if the object is null. In Loading mode (and sometimes in Schema mode), 
		///   <see cref="ISyncManager"/> ignores this parameter. If a value type is
		///   being read/written, you can set this parameter to null to avoid 
		///   memory allocation, but be sure to use a mode that includes 
		///   SubObjectMode.NotNull and not SubObjectMode.Deduplicate.</param>
		/// <param name="mode">See <see cref="SubObjectMode"/> for information 
		///   about the possible modes. When SubObjectMode.NotNull is present and
		///   SubObjectMode.Deduplicate is absent, the value of childKey is ignored.</param>
		/// <param name="listLength">If a variable-length list is being written
		///   (i.e. <c>(mode & SubObjectMode.List) != 0 && Mode is SyncMode.Saving 
		///   or SyncMode.Query</c>), this must specify the list length. 
		///   Implementations of this interface that use delimiters (e.g. JSON) 
		///   will ignore this parameter, but others will write the length to the 
		///   output stream at the beginning of the list.</param>
		/// <returns>
		///   The boolean value is true if the request to read/write is approved.
		///   In this case you are expected to write the contents of the object and 
		///   then call <see cref="FinishSubObject()"/>. If the boolean is false, 
		///   the request was declined and you must not write the fields of the 
		///   sub-object, and you must not call <see cref="FinishSubObject()"/>.
		///   <para/>
		///   The second return value is a reference to a deduplicated object,
		///   a reference to childKey, or null, depending on the situation (see
		///   remarks).
		///   <para/>
		///   When calling <see cref="BeginList"/> with a <c>listLength</c> parameter,
		///   there is a third return value, the list length, which is the number 
		///   of elements you are expected to read or write. 
		/// </returns>
		/// <remarks>
		///   This method has six possible outcomes:
		///   (1) The request to read/write is approved. In this case, this method
		///       returns (true, childKey) and <see cref="Depth"/> increases by one.
		///       childKey is the same reference you passed to this method.
		///   (2) You set childKey = null and <see cref="Mode"/> is not Loading.
		///       This indicates that no child object exists, so this method returns 
		///       (false, null).
		///   (3) The <see cref="Mode"/> is Loading and the input stream contains a 
		///       representation of null, so this method returns (false, null).
		///   (4) The list/tuple being read/write has already been read/written 
		///       earlier (and you enabled deduplication), so the request to 
		///       read/write is declined. In this case, this method returns false 
		///       with a reference to the object that was loaded or saved earlier.
		///   (5) The <see cref="Mode"/> is Loading or Merge and the input stream
		///       is invalid or does not contain an object or list by the specified 
		///       name, so it is impossible to fulfill the request. In this case, 
		///       an exception is thrown, such as <see cref="FormatException"/>.
		///   (6) The <see cref="Mode"/> is Query, Schema or Merge and the current 
		///       <see cref="ISyncManager"/> has decided not to traverse into the 
		///       current field. In this case, this method returns (false, childKey).
		///   <para/>
		///   In Saving mode, and in every case except 4, the returned Object is 
		///   the same as childKey.
		/// </remarks>
		(bool Begun, object? Object) BeginSubObject(Symbol? name, object? childKey, SubObjectMode mode, int listLength = -1);

		/// <summary>
		/// If you called <see cref="BeginSubObject"/> and it returned true, you 
		/// must call this method when you're done loading/saving the sub-object. 
		/// Do not call this method otherwise.
		/// </summary>
		void EndSubObject();
		
		/// <summary>If the current mode is Query or Merge, this method may add
		/// constraints to a query according to the scope of the query being 
		/// processed.</summary>
		//IQueryable<T>? QueryFilter(Symbol name, IQueryable<T> list);
	}

	public static partial class SyncManagerExt
	{
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		public static bool Sync<SyncManager>(this SyncManager sync, string name, bool savable) where SyncManager: ISyncManager => 
		  sync.Sync((Symbol) name, savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		public static sbyte Sync<SyncManager>(this SyncManager sync, string name, sbyte savable) where SyncManager: ISyncManager => 
		  sync.Sync((Symbol) name, savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		public static byte Sync<SyncManager>(this SyncManager sync, string name, byte savable) where SyncManager: ISyncManager => 
		  sync.Sync((Symbol) name, savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		public static short Sync<SyncManager>(this SyncManager sync, string name, short savable) where SyncManager: ISyncManager => 
		  sync.Sync((Symbol) name, savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		public static ushort Sync<SyncManager>(this SyncManager sync, string name, ushort savable) where SyncManager: ISyncManager => 
		  sync.Sync((Symbol) name, savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		public static int Sync<SyncManager>(this SyncManager sync, string name, int savable) where SyncManager: ISyncManager => 
		  sync.Sync((Symbol) name, savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		public static uint Sync<SyncManager>(this SyncManager sync, string name, uint savable) where SyncManager: ISyncManager => 
		  sync.Sync((Symbol) name, savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		public static long Sync<SyncManager>(this SyncManager sync, string name, long savable) where SyncManager: ISyncManager => 
		  sync.Sync((Symbol) name, savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		public static ulong Sync<SyncManager>(this SyncManager sync, string name, ulong savable) where SyncManager: ISyncManager => 
		  sync.Sync((Symbol) name, savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		public static float Sync<SyncManager>(this SyncManager sync, string name, float savable) where SyncManager: ISyncManager => 
		  sync.Sync((Symbol) name, savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		public static double Sync<SyncManager>(this SyncManager sync, string name, double savable) where SyncManager: ISyncManager => 
		  sync.Sync((Symbol) name, savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		public static decimal Sync<SyncManager>(this SyncManager sync, string name, decimal savable) where SyncManager: ISyncManager => 
		  sync.Sync((Symbol) name, savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		public static BigInteger Sync<SyncManager>(this SyncManager sync, string name, BigInteger savable) where SyncManager: ISyncManager => 
		  sync.Sync((Symbol) name, savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		public static char Sync<SyncManager>(this SyncManager sync, string name, char savable) where SyncManager: ISyncManager => 
		  sync.Sync((Symbol) name, savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		public static string Sync<SyncManager>(this SyncManager sync, string name, string savable) where SyncManager: ISyncManager => 
		  sync.Sync((Symbol) name, savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		public static int Sync<SyncManager>(this SyncManager sync, string name, int savable, int bits, bool signed = true) where SyncManager: ISyncManager => 
		  sync.Sync((Symbol) name, savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		public static long Sync<SyncManager>(this SyncManager sync, string name, long savable, int bits, bool signed = true) where SyncManager: ISyncManager => 
		  sync.Sync((Symbol) name, savable);
		/// <summary>Reads or writes a value of a non-nullable field of the current object.</summary>
		public static BigInteger Sync<SyncManager>(this SyncManager sync, string name, BigInteger savable, int bits, bool signed = true) where SyncManager: ISyncManager => 
		  sync.Sync((Symbol) name, savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		public static bool? SyncNullable<SyncManager>(this SyncManager sync, string name, bool? savable) where SyncManager: ISyncManager => 
		  sync.SyncNullable((Symbol) name, savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		public static sbyte? SyncNullable<SyncManager>(this SyncManager sync, string name, sbyte? savable) where SyncManager: ISyncManager => 
		  sync.SyncNullable((Symbol) name, savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		public static byte? SyncNullable<SyncManager>(this SyncManager sync, string name, byte? savable) where SyncManager: ISyncManager => 
		  sync.SyncNullable((Symbol) name, savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		public static short? SyncNullable<SyncManager>(this SyncManager sync, string name, short? savable) where SyncManager: ISyncManager => 
		  sync.SyncNullable((Symbol) name, savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		public static ushort? SyncNullable<SyncManager>(this SyncManager sync, string name, ushort? savable) where SyncManager: ISyncManager => 
		  sync.SyncNullable((Symbol) name, savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		public static int? SyncNullable<SyncManager>(this SyncManager sync, string name, int? savable) where SyncManager: ISyncManager => 
		  sync.SyncNullable((Symbol) name, savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		public static uint? SyncNullable<SyncManager>(this SyncManager sync, string name, uint? savable) where SyncManager: ISyncManager => 
		  sync.SyncNullable((Symbol) name, savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		public static long? SyncNullable<SyncManager>(this SyncManager sync, string name, long? savable) where SyncManager: ISyncManager => 
		  sync.SyncNullable((Symbol) name, savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		public static ulong? SyncNullable<SyncManager>(this SyncManager sync, string name, ulong? savable) where SyncManager: ISyncManager => 
		  sync.SyncNullable((Symbol) name, savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		public static float? SyncNullable<SyncManager>(this SyncManager sync, string name, float? savable) where SyncManager: ISyncManager => 
		  sync.SyncNullable((Symbol) name, savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		public static double? SyncNullable<SyncManager>(this SyncManager sync, string name, double? savable) where SyncManager: ISyncManager => 
		  sync.SyncNullable((Symbol) name, savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		public static decimal? SyncNullable<SyncManager>(this SyncManager sync, string name, decimal? savable) where SyncManager: ISyncManager => 
		  sync.SyncNullable((Symbol) name, savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		public static BigInteger? SyncNullable<SyncManager>(this SyncManager sync, string name, BigInteger? savable) where SyncManager: ISyncManager => 
		  sync.SyncNullable((Symbol) name, savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		public static char? SyncNullable<SyncManager>(this SyncManager sync, string name, char? savable) where SyncManager: ISyncManager => 
		  sync.SyncNullable((Symbol) name, savable);
		/// <summary>Reads or writes a value of a nullable field of the current object.</summary>
		public static string? SyncNullable<SyncManager>(this SyncManager sync, string name, string? savable) where SyncManager: ISyncManager => 
		  sync.SyncNullable((Symbol) name, savable);

		// SyncList methods for Bool
		public static List<bool>? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, List<bool>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager
		
		{
			ScannableEnumerable<bool>.Scanner<List<bool>.Enumerator> scanner = default;
			if (savable != null)
				scanner = new ScannableEnumerable<bool>.Scanner<List<bool>.Enumerator>(savable.GetEnumerator());
			return sync.SyncListBoolImpl<ScannableEnumerable<bool>.Scanner<List<bool>.Enumerator>, List<bool>, ListBuilder<bool>>(
			name, scanner, savable, new ListBuilder<bool>(), listMode, tupleLength);
		}
		public static bool[]? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, bool[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = new InternalList.Scanner<bool>(savable.AsMemory());
			return sync.SyncListBoolImpl<InternalList.Scanner<bool>, bool[], ArrayBuilder<bool>>(
			name, scanner, savable, new ArrayBuilder<bool>(), listMode, tupleLength);
		}
		public static Memory<bool> SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, Memory<bool> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager
		
		{
			if ((listMode & SubObjectMode.Deduplicate) != 0)
				throw new ArgumentException("SubObjectMode.Deduplicate is incompatible with Memory<T>");
			var scanner = new InternalList.Scanner<bool>(savable);
			return sync.SyncListBoolImpl<InternalList.Scanner<bool>, Memory<bool>, MemoryBuilder<bool>>(
			name, scanner, null, new MemoryBuilder<bool>(), listMode | SubObjectMode.NotNull, tupleLength);
		}
		// Produces an error: CS0111: Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	Symbol? name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//{
		//	object? listRef = (listMode & (SubObjectMode.Deduplicate | SubObjectMode.NotNull)) == SubObjectMode.NotNull ? null : savable;
		//	var scanner = (savable == null ? Empty<$T>.Array : Loyc.Collections.MutableListExtensionMethods.LinqToLists.ToArray(savable)).Slice().Scan();
		//	return sync.$(out NameWithType(Sync, $T, ListImpl))<List, InternalList.Scanner<$T>, CollectionBuilder<List, $T>>
		//		(name, scanner, new CollectionBuilder<List, $T>(alloc), listMode, listRef, tupleLength);
		//}
		public static List<bool>? SyncList<SyncManager>(this SyncManager sync, 
		  string name, List<bool>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, listMode, tupleLength);
		public static bool[]? SyncList<SyncManager>(this SyncManager sync, 
		  string name, bool[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, listMode, tupleLength);
		public static Memory<bool> SyncList<SyncManager>(this SyncManager sync, 
		  string name, Memory<bool> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	string name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList(sync, (Symbol)name, savable, listMode, tupleLength);

		// SyncList methods for #int8
		public static List<sbyte>? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, List<sbyte>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static sbyte[]? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, sbyte[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<sbyte> SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, Memory<sbyte> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	Symbol? name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList<SyncManager, List, $T, SyncPrimitive<SyncManager>>(
		//		sync, name, savable, new SyncPrimitive<SyncManager>(), alloc, listMode, tupleLength);
		public static List<sbyte>? SyncList<SyncManager>(this SyncManager sync, 
		  string name, List<sbyte>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static sbyte[]? SyncList<SyncManager>(this SyncManager sync, 
		  string name, sbyte[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<sbyte> SyncList<SyncManager>(this SyncManager sync, 
		  string name, Memory<sbyte> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	string name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList(sync, (Symbol)name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);

		// SyncList methods for Byte
		public static List<byte>? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, List<byte>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager
		
		{
			ScannableEnumerable<byte>.Scanner<List<byte>.Enumerator> scanner = default;
			if (savable != null)
				scanner = new ScannableEnumerable<byte>.Scanner<List<byte>.Enumerator>(savable.GetEnumerator());
			return sync.SyncListByteImpl<ScannableEnumerable<byte>.Scanner<List<byte>.Enumerator>, List<byte>, ListBuilder<byte>>(
			name, scanner, savable, new ListBuilder<byte>(), listMode, tupleLength);
		}
		public static byte[]? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, byte[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = new InternalList.Scanner<byte>(savable.AsMemory());
			return sync.SyncListByteImpl<InternalList.Scanner<byte>, byte[], ArrayBuilder<byte>>(
			name, scanner, savable, new ArrayBuilder<byte>(), listMode, tupleLength);
		}
		public static Memory<byte> SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, Memory<byte> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager
		
		{
			if ((listMode & SubObjectMode.Deduplicate) != 0)
				throw new ArgumentException("SubObjectMode.Deduplicate is incompatible with Memory<T>");
			var scanner = new InternalList.Scanner<byte>(savable);
			return sync.SyncListByteImpl<InternalList.Scanner<byte>, Memory<byte>, MemoryBuilder<byte>>(
			name, scanner, null, new MemoryBuilder<byte>(), listMode | SubObjectMode.NotNull, tupleLength);
		}
		// Produces an error: CS0111: Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	Symbol? name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//{
		//	object? listRef = (listMode & (SubObjectMode.Deduplicate | SubObjectMode.NotNull)) == SubObjectMode.NotNull ? null : savable;
		//	var scanner = (savable == null ? Empty<$T>.Array : Loyc.Collections.MutableListExtensionMethods.LinqToLists.ToArray(savable)).Slice().Scan();
		//	return sync.$(out NameWithType(Sync, $T, ListImpl))<List, InternalList.Scanner<$T>, CollectionBuilder<List, $T>>
		//		(name, scanner, new CollectionBuilder<List, $T>(alloc), listMode, listRef, tupleLength);
		//}
		public static List<byte>? SyncList<SyncManager>(this SyncManager sync, 
		  string name, List<byte>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, listMode, tupleLength);
		public static byte[]? SyncList<SyncManager>(this SyncManager sync, 
		  string name, byte[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, listMode, tupleLength);
		public static Memory<byte> SyncList<SyncManager>(this SyncManager sync, 
		  string name, Memory<byte> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	string name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList(sync, (Symbol)name, savable, listMode, tupleLength);

		// SyncList methods for #int16
		public static List<short>? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, List<short>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static short[]? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, short[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<short> SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, Memory<short> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	Symbol? name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList<SyncManager, List, $T, SyncPrimitive<SyncManager>>(
		//		sync, name, savable, new SyncPrimitive<SyncManager>(), alloc, listMode, tupleLength);
		public static List<short>? SyncList<SyncManager>(this SyncManager sync, 
		  string name, List<short>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static short[]? SyncList<SyncManager>(this SyncManager sync, 
		  string name, short[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<short> SyncList<SyncManager>(this SyncManager sync, 
		  string name, Memory<short> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	string name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList(sync, (Symbol)name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);

		// SyncList methods for #uint16
		public static List<ushort>? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, List<ushort>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static ushort[]? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, ushort[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<ushort> SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, Memory<ushort> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	Symbol? name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList<SyncManager, List, $T, SyncPrimitive<SyncManager>>(
		//		sync, name, savable, new SyncPrimitive<SyncManager>(), alloc, listMode, tupleLength);
		public static List<ushort>? SyncList<SyncManager>(this SyncManager sync, 
		  string name, List<ushort>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static ushort[]? SyncList<SyncManager>(this SyncManager sync, 
		  string name, ushort[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<ushort> SyncList<SyncManager>(this SyncManager sync, 
		  string name, Memory<ushort> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	string name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList(sync, (Symbol)name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);

		// SyncList methods for #int32
		public static List<int>? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, List<int>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static int[]? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, int[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<int> SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, Memory<int> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	Symbol? name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList<SyncManager, List, $T, SyncPrimitive<SyncManager>>(
		//		sync, name, savable, new SyncPrimitive<SyncManager>(), alloc, listMode, tupleLength);
		public static List<int>? SyncList<SyncManager>(this SyncManager sync, 
		  string name, List<int>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static int[]? SyncList<SyncManager>(this SyncManager sync, 
		  string name, int[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<int> SyncList<SyncManager>(this SyncManager sync, 
		  string name, Memory<int> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	string name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList(sync, (Symbol)name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);

		// SyncList methods for #uint32
		public static List<uint>? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, List<uint>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static uint[]? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, uint[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<uint> SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, Memory<uint> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	Symbol? name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList<SyncManager, List, $T, SyncPrimitive<SyncManager>>(
		//		sync, name, savable, new SyncPrimitive<SyncManager>(), alloc, listMode, tupleLength);
		public static List<uint>? SyncList<SyncManager>(this SyncManager sync, 
		  string name, List<uint>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static uint[]? SyncList<SyncManager>(this SyncManager sync, 
		  string name, uint[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<uint> SyncList<SyncManager>(this SyncManager sync, 
		  string name, Memory<uint> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	string name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList(sync, (Symbol)name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);

		// SyncList methods for #int64
		public static List<long>? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, List<long>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static long[]? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, long[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<long> SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, Memory<long> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	Symbol? name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList<SyncManager, List, $T, SyncPrimitive<SyncManager>>(
		//		sync, name, savable, new SyncPrimitive<SyncManager>(), alloc, listMode, tupleLength);
		public static List<long>? SyncList<SyncManager>(this SyncManager sync, 
		  string name, List<long>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static long[]? SyncList<SyncManager>(this SyncManager sync, 
		  string name, long[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<long> SyncList<SyncManager>(this SyncManager sync, 
		  string name, Memory<long> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	string name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList(sync, (Symbol)name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);

		// SyncList methods for #uint64
		public static List<ulong>? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, List<ulong>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static ulong[]? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, ulong[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<ulong> SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, Memory<ulong> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	Symbol? name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList<SyncManager, List, $T, SyncPrimitive<SyncManager>>(
		//		sync, name, savable, new SyncPrimitive<SyncManager>(), alloc, listMode, tupleLength);
		public static List<ulong>? SyncList<SyncManager>(this SyncManager sync, 
		  string name, List<ulong>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static ulong[]? SyncList<SyncManager>(this SyncManager sync, 
		  string name, ulong[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<ulong> SyncList<SyncManager>(this SyncManager sync, 
		  string name, Memory<ulong> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	string name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList(sync, (Symbol)name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);

		// SyncList methods for #single
		public static List<float>? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, List<float>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static float[]? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, float[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<float> SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, Memory<float> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	Symbol? name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList<SyncManager, List, $T, SyncPrimitive<SyncManager>>(
		//		sync, name, savable, new SyncPrimitive<SyncManager>(), alloc, listMode, tupleLength);
		public static List<float>? SyncList<SyncManager>(this SyncManager sync, 
		  string name, List<float>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static float[]? SyncList<SyncManager>(this SyncManager sync, 
		  string name, float[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<float> SyncList<SyncManager>(this SyncManager sync, 
		  string name, Memory<float> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	string name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList(sync, (Symbol)name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);

		// SyncList methods for #double
		public static List<double>? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, List<double>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static double[]? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, double[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<double> SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, Memory<double> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	Symbol? name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList<SyncManager, List, $T, SyncPrimitive<SyncManager>>(
		//		sync, name, savable, new SyncPrimitive<SyncManager>(), alloc, listMode, tupleLength);
		public static List<double>? SyncList<SyncManager>(this SyncManager sync, 
		  string name, List<double>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static double[]? SyncList<SyncManager>(this SyncManager sync, 
		  string name, double[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<double> SyncList<SyncManager>(this SyncManager sync, 
		  string name, Memory<double> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	string name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList(sync, (Symbol)name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);

		// SyncList methods for #decimal
		public static List<decimal>? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, List<decimal>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static decimal[]? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, decimal[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<decimal> SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, Memory<decimal> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	Symbol? name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList<SyncManager, List, $T, SyncPrimitive<SyncManager>>(
		//		sync, name, savable, new SyncPrimitive<SyncManager>(), alloc, listMode, tupleLength);
		public static List<decimal>? SyncList<SyncManager>(this SyncManager sync, 
		  string name, List<decimal>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static decimal[]? SyncList<SyncManager>(this SyncManager sync, 
		  string name, decimal[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<decimal> SyncList<SyncManager>(this SyncManager sync, 
		  string name, Memory<decimal> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	string name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList(sync, (Symbol)name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);

		// SyncList methods for BigInteger
		public static List<BigInteger>? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, List<BigInteger>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static BigInteger[]? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, BigInteger[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<BigInteger> SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, Memory<BigInteger> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	Symbol? name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList<SyncManager, List, $T, SyncPrimitive<SyncManager>>(
		//		sync, name, savable, new SyncPrimitive<SyncManager>(), alloc, listMode, tupleLength);
		public static List<BigInteger>? SyncList<SyncManager>(this SyncManager sync, 
		  string name, List<BigInteger>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static BigInteger[]? SyncList<SyncManager>(this SyncManager sync, 
		  string name, BigInteger[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<BigInteger> SyncList<SyncManager>(this SyncManager sync, 
		  string name, Memory<BigInteger> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	string name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList(sync, (Symbol)name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);

		// SyncList methods for Char
		public static List<char>? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, List<char>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager
		
		{
			ScannableEnumerable<char>.Scanner<List<char>.Enumerator> scanner = default;
			if (savable != null)
				scanner = new ScannableEnumerable<char>.Scanner<List<char>.Enumerator>(savable.GetEnumerator());
			return sync.SyncListCharImpl<ScannableEnumerable<char>.Scanner<List<char>.Enumerator>, List<char>, ListBuilder<char>>(
			name, scanner, savable, new ListBuilder<char>(), listMode, tupleLength);
		}
		public static char[]? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, char[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = new InternalList.Scanner<char>(savable.AsMemory());
			return sync.SyncListCharImpl<InternalList.Scanner<char>, char[], ArrayBuilder<char>>(
			name, scanner, savable, new ArrayBuilder<char>(), listMode, tupleLength);
		}
		public static Memory<char> SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, Memory<char> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager
		
		{
			if ((listMode & SubObjectMode.Deduplicate) != 0)
				throw new ArgumentException("SubObjectMode.Deduplicate is incompatible with Memory<T>");
			var scanner = new InternalList.Scanner<char>(savable);
			return sync.SyncListCharImpl<InternalList.Scanner<char>, Memory<char>, MemoryBuilder<char>>(
			name, scanner, null, new MemoryBuilder<char>(), listMode | SubObjectMode.NotNull, tupleLength);
		}
		// Produces an error: CS0111: Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	Symbol? name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//{
		//	object? listRef = (listMode & (SubObjectMode.Deduplicate | SubObjectMode.NotNull)) == SubObjectMode.NotNull ? null : savable;
		//	var scanner = (savable == null ? Empty<$T>.Array : Loyc.Collections.MutableListExtensionMethods.LinqToLists.ToArray(savable)).Slice().Scan();
		//	return sync.$(out NameWithType(Sync, $T, ListImpl))<List, InternalList.Scanner<$T>, CollectionBuilder<List, $T>>
		//		(name, scanner, new CollectionBuilder<List, $T>(alloc), listMode, listRef, tupleLength);
		//}
		public static List<char>? SyncList<SyncManager>(this SyncManager sync, 
		  string name, List<char>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, listMode, tupleLength);
		public static char[]? SyncList<SyncManager>(this SyncManager sync, 
		  string name, char[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, listMode, tupleLength);
		public static Memory<char> SyncList<SyncManager>(this SyncManager sync, 
		  string name, Memory<char> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	string name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList(sync, (Symbol)name, savable, listMode, tupleLength);

		// SyncList methods for 'of
		public static List<string?>? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, List<string?>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static string?[]? SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, string?[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<string?> SyncList<SyncManager>(this SyncManager sync, 
		  Symbol? name, Memory<string?> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	Symbol? name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList<SyncManager, List, $T, SyncPrimitive<SyncManager>>(
		//		sync, name, savable, new SyncPrimitive<SyncManager>(), alloc, listMode, tupleLength);
		public static List<string?>? SyncList<SyncManager>(this SyncManager sync, 
		  string name, List<string?>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static string?[]? SyncList<SyncManager>(this SyncManager sync, 
		  string name, string?[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		public static Memory<string?> SyncList<SyncManager>(this SyncManager sync, 
		  string name, Memory<string?> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager => 
		  
		  SyncManagerExt.SyncList(sync, (Symbol) name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	string name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//	=> SyncManagerExt.SyncList(sync, (Symbol)name, savable, new SyncPrimitive<SyncManager>(), listMode, tupleLength);
	}
}