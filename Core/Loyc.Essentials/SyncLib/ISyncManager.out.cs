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
	public delegate T SyncFieldFunc<T>(FieldId name, [AllowNull] T value);

	/// <summary>This is the central interface of Loyc.SyncLib. To learn more, please 
	/// visit the web site: http://loyc.net/serialization </summary>
	/// <remarks>
	/// Note: it is recommended (but not required) that Sync() methods in reader 
	/// implementations support the following implicit type conversions:
	/// <ul>
	/// <li>Boolean to byte (false => 0, true => 1)</li>
	/// <li>Byte to integer</li>
	/// <li>Integer to Float</li>
	/// <li>Float to String</li>
	/// <li>Char to String</li>
	/// </ul>
	/// </remarks>
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
		/// If the method is supported, it determines whether a field with a specific
		/// name exists, and if so, what type it has.</summary>
		/// <param name="name">Name to search for in the current stream.
		///   If <see cref="NeedsIntegerIds"/> is true then the FieldId must be in a
		///   private <see cref="SymbolPool"/> and it needs to have had an ID number
		///   manually assigned to it. If the name does not meet this requirement,
		///   the method will return SyncType.Unknown.</param>
		/// <param name="expectedType">The type that the caller expects to encounter,
		///   or SyncType.Unknown if the caller has no preference.</param>
		/// <returns>
		///   Returns <see cref="SyncType.Missing"/> if the field does not exist, or
		///   if the field exists but the field's type is not implicitly convertible
		///   to the expected type.
		///   <para/>
		///   Returns <see cref="SyncType.Unknown"/> if it cannot be determined whether
		///   the field exists, or if this ISyncManager is not a reader (Mode != 
		///   SyncMode.Loading), or if <see cref="IsInsideList"/> is true.
		///   <para/>
		///   Otherwise, an appropriate value of <see cref="SyncType"/> is returned 
		///   according to the data in the data stream.
		/// </returns>
		/// <remarks>
		///   Generally, this method can provide useful information only if 
		///   <see cref="SupportsReordering"/> is true, `Mode == SyncMode.Loading`, and
		///   <see cref="IsInsideList"/> is false. If any of these conditions is not 
		///   met, the method normally returns SyncType.Unknown. Conceptually,
		///   a list or tuple is treated as an object in which field names are unknown,
		///   rather than as an object in which the fields have no names.
		///   <para/>
		///   The SyncType enumeration has a collection of common types that are supported
		///   by most data formats. However, some formats may not support all types, or
		///   may store data in a different form than you might reasonably expect. For
		///   example, when a byte array is stored in JSON, it is stored as a string by
		///   default. When reading a byte array from JSON, HasField will report that the 
		///   type is SyncType.String because even though the reader is capable of 
		///   decoding the string as a byte array, it cannot know that the string 
		///   represents a byte array. For this reason, the expectedType parameter exists
		///   as a filtering technique. You can set this parameter to SyncType.ByteList 
		///   to indicate that you expect to read a byte array. If the JSON data type is 
		///   boolean, which cannot be interpreted as a byte array, HasField() returns 
		///   SyncType.Missing. But if the JSON data type is string, HasField() returns 
		///   SyncType.String. This indicates that the data tream contains a String that 
		///   is potentially convertible to SyncType.ByteList, although the conversion is 
		///   not guaranteed to work. If you then read this field by calling
		///   Sync(name, (byte[]) null) and it turns out that the string cannot be 
		///   interpreted as a byte array, an exception will be thrown.
		///   <para/>
		///   If a value is not implicitly convertible to the expectedType, HasField
		///   should return SyncType.Missing even if the conversion is supported.
		///   For example, if the actual type is Char but a Byte was expected, the
		///   ISyncManager implementation may support this conversion by masking off 
		///   the lowest 8 bits, but it should return SyncType.Missing because this 
		///   kind of conversion loses information and may not be what the user 
		///   intended. (It may seem like this is somewhat in contradiction with the
		///   previous paragraph, because JSON pretends String matches ByteList even 
		///   though the conversion may fail. However, the fact that a ByteList is 
		///   stored as a String is an implementation detail that your code should
		///   not explicitly deal with, so HasField must report that the conversion
		///   is supported in order not to confuse code that is unaware of the 
		///   implementation detail.)
		///   <para/>
		///   It is recommended that implementations of ISyncManager support an 
		///   implicit conversion from boolean to number, as well as conversions from 
		///   "smaller" to "bigger" number types.
		/// </remarks>
		SyncType HasField(FieldId name, SyncType expectedType = SyncType.Unknown);

		/// <summary>Returns the number of parent objects of the current object being
		/// loaded or saved. This property is zero if the root object is being loaded
		/// or saved.</summary>
		int Depth { get; }
		/// <summary>Reads or writes a value of a field on the current object.</summary>
		bool Sync(FieldId name, bool savable);
		/// <summary>Reads or writes a value of a field on the current object.</summary>
		sbyte Sync(FieldId name, sbyte savable);
		/// <summary>Reads or writes a value of a field on the current object.</summary>
		byte Sync(FieldId name, byte savable);
		/// <summary>Reads or writes a value of a field on the current object.</summary>
		short Sync(FieldId name, short savable);
		/// <summary>Reads or writes a value of a field on the current object.</summary>
		ushort Sync(FieldId name, ushort savable);
		/// <summary>Reads or writes a value of a field on the current object.</summary>
		int Sync(FieldId name, int savable);
		/// <summary>Reads or writes a value of a field on the current object.</summary>
		uint Sync(FieldId name, uint savable);
		/// <summary>Reads or writes a value of a field on the current object.</summary>
		long Sync(FieldId name, long savable);
		/// <summary>Reads or writes a value of a field on the current object.</summary>
		ulong Sync(FieldId name, ulong savable);
		/// <summary>Reads or writes a value of a field on the current object.</summary>
		float Sync(FieldId name, float savable);
		/// <summary>Reads or writes a value of a field on the current object.</summary>
		double Sync(FieldId name, double savable);
		/// <summary>Reads or writes a value of a field on the current object.</summary>
		decimal Sync(FieldId name, decimal savable);
		/// <summary>Reads or writes a value of a field on the current object.</summary>
		BigInteger Sync(FieldId name, BigInteger savable);
		/// <summary>Reads or writes a value of a field on the current object.</summary>
		char Sync(FieldId name, char savable);
		/// <summary>Reads or writes a value of a field on the current object.</summary>
		string? Sync(FieldId name, string? savable);
		/// <summary>Reads or writes a value of an integer bitfield on the current object.</summary>
		int Sync(FieldId name, int savable, int bits, bool signed = true);
		/// <summary>Reads or writes a value of an integer bitfield on the current object.</summary>
		long Sync(FieldId name, long savable, int bits, bool signed = true);
		/// <summary>Reads or writes a value of an integer bitfield on the current object.</summary>
		BigInteger Sync(FieldId name, BigInteger savable, int bits, bool signed = true);
		/// <summary>Reads or writes a value of a nullable field on the current object.</summary>
		bool? Sync(FieldId name, bool? savable);
		/// <summary>Reads or writes a value of a nullable field on the current object.</summary>
		sbyte? Sync(FieldId name, sbyte? savable);
		/// <summary>Reads or writes a value of a nullable field on the current object.</summary>
		byte? Sync(FieldId name, byte? savable);
		/// <summary>Reads or writes a value of a nullable field on the current object.</summary>
		short? Sync(FieldId name, short? savable);
		/// <summary>Reads or writes a value of a nullable field on the current object.</summary>
		ushort? Sync(FieldId name, ushort? savable);
		/// <summary>Reads or writes a value of a nullable field on the current object.</summary>
		int? Sync(FieldId name, int? savable);
		/// <summary>Reads or writes a value of a nullable field on the current object.</summary>
		uint? Sync(FieldId name, uint? savable);
		/// <summary>Reads or writes a value of a nullable field on the current object.</summary>
		long? Sync(FieldId name, long? savable);
		/// <summary>Reads or writes a value of a nullable field on the current object.</summary>
		ulong? Sync(FieldId name, ulong? savable);
		/// <summary>Reads or writes a value of a nullable field on the current object.</summary>
		float? Sync(FieldId name, float? savable);
		/// <summary>Reads or writes a value of a nullable field on the current object.</summary>
		double? Sync(FieldId name, double? savable);
		/// <summary>Reads or writes a value of a nullable field on the current object.</summary>
		decimal? Sync(FieldId name, decimal? savable);
		/// <summary>Reads or writes a value of a nullable field on the current object.</summary>
		BigInteger? Sync(FieldId name, BigInteger? savable);
		/// <summary>Reads or writes a value of a nullable field on the current object.</summary>
		char? Sync(FieldId name, char? savable);
		/// <summary>This method is used by Sync() extension methods to read 
		///   or write an array. Users don't need to call it.</summary>
		/// <remarks>Full documentation is located in source code (ISyncManager.ecs)</remarks>
		_numfn(@_apos_quest <List>, @_aposof(NameWithType(SyncList, bool, Impl), Scanner, List, ListBuilder), 
		_num(FieldId name, Scanner scanner, List? saving, ListBuilder builder, SubObjectMode mode, int tupleLength = -1)
		
		);
		/// <summary>This method is used by Sync() extension methods to read 
		///   or write an array. Users don't need to call it.</summary>
		/// <remarks>Full documentation is located in source code (ISyncManager.ecs)</remarks>
		_numfn(@_apos_quest <List>, @_aposof(NameWithType(SyncList, char, Impl), Scanner, List, ListBuilder), 
		_num(FieldId name, Scanner scanner, List? saving, ListBuilder builder, SubObjectMode mode, int tupleLength = -1)
		
		);
		/// <summary>This method is used by Sync() extension methods to read 
		///   or write an array. Users don't need to call it.</summary>
		/// <remarks>Full documentation is located in source code (ISyncManager.ecs)</remarks>
		_numfn(@_apos_quest <List>, @_aposof(NameWithType(SyncList, byte, Impl), Scanner, List, ListBuilder), 
		_num(FieldId name, Scanner scanner, List? saving, ListBuilder builder, SubObjectMode mode, int tupleLength = -1)
		
		);

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
		(bool Begun, object? Object) BeginSubObject(FieldId name, object? childKey, SubObjectMode mode, int listLength = -1);

		/// <summary>
		/// If you called <see cref="BeginSubObject"/> and it returned true, you 
		/// must call this method when you're done loading/saving the sub-object. 
		/// Do not call this method otherwise.
		/// </summary>
		void EndSubObject();
		
		/// <summary>If the current mode is Query or Merge, this method may add
		/// constraints to a query according to the scope of the query being 
		/// processed.</summary>
		//IQueryable<T>? QueryFilter(FieldId name, IQueryable<T> list);
	}

	public static partial class SyncManagerExt
	{
		//
		// TODO: support other list types with byte/char/bool
		//
		//NameWithType
		public static List<bool>? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, List<bool>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager
		
		{
			ScannableEnumerable<bool>.Scanner<List<bool>.Enumerator> scanner = default;
			if (savable != null)
				scanner = new ScannableEnumerable<bool>.Scanner<List<bool>.Enumerator>(savable.GetEnumerator());
			return sync.(@_aposof(NameWithType(SyncList, bool, Impl), ScannableEnumerable<bool>.Scanner<List<bool>.Enumerator>, List<bool>, ListBuilder<bool>))(
			name, scanner, savable, new ListBuilder<bool>(), listMode, tupleLength);
		}
		public static bool[]? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, bool[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = new InternalList.Scanner<bool>(savable.AsMemory());
			return sync.(@_aposof(NameWithType(SyncList, bool, Impl), InternalList.Scanner<bool>, @_apos_lsqb_rsqb <bool>, ArrayBuilder<bool>))(
			name, scanner, savable, new ArrayBuilder<bool>(), listMode, tupleLength);
		}
		public static Memory<bool> SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, Memory<bool> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager
		
		{
			if ((listMode & SubObjectMode.Deduplicate) != 0)
				throw new ArgumentException("SubObjectMode.Deduplicate is incompatible with Memory<T>");
			var scanner = new InternalList.Scanner<bool>(savable);
			return sync.(@_aposof(NameWithType(SyncList, bool, Impl), InternalList.Scanner<bool>, Memory<bool>, MemoryBuilder<bool>))(
			name, scanner, null, new MemoryBuilder<bool>(), listMode | SubObjectMode.NotNull, tupleLength);
		}
		// Produces an error: CS0111: Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	FieldId name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//{
		//	object? listRef = (listMode & (SubObjectMode.Deduplicate | SubObjectMode.NotNull)) == SubObjectMode.NotNull ? null : savable;
		//	var scanner = (savable == null ? Empty<$T>.Array : Loyc.Collections.MutableListExtensionMethods.LinqToLists.ToArray(savable)).Slice().Scan();
		//	return sync.$(out NameWithType(Sync, $T, ListImpl))<List, InternalList.Scanner<$T>, CollectionBuilder<List, $T>>
		//		(name, scanner, new CollectionBuilder<List, $T>(alloc), listMode, listRef, tupleLength);
		//}
		public static sbyte[]? SyncList<SM>(this SM sync, 
		  FieldId name, sbyte[]? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, sbyte, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static List<sbyte>? SyncList<SM>(this SM sync, 
		  FieldId name, List<sbyte>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, sbyte, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IList<sbyte>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<sbyte>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, sbyte, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static IReadOnlyList<sbyte>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<sbyte>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, sbyte, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IListSource<sbyte>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<sbyte>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, sbyte, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ICollection<sbyte>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<sbyte>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, sbyte, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IReadOnlyCollection<sbyte>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<sbyte>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, sbyte, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static HashSet<sbyte>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<sbyte>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, sbyte, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ReadOnlyMemory<sbyte> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<sbyte> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, sbyte, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static Memory<sbyte> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<sbyte> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, sbyte, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		
		//
		// TODO: support other list types with byte/char/bool
		//
		//NameWithType
		public static List<byte>? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, List<byte>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager
		
		{
			ScannableEnumerable<byte>.Scanner<List<byte>.Enumerator> scanner = default;
			if (savable != null)
				scanner = new ScannableEnumerable<byte>.Scanner<List<byte>.Enumerator>(savable.GetEnumerator());
			return sync.(@_aposof(NameWithType(SyncList, byte, Impl), ScannableEnumerable<byte>.Scanner<List<byte>.Enumerator>, List<byte>, ListBuilder<byte>))(
			name, scanner, savable, new ListBuilder<byte>(), listMode, tupleLength);
		}
		public static byte[]? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, byte[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = new InternalList.Scanner<byte>(savable.AsMemory());
			return sync.(@_aposof(NameWithType(SyncList, byte, Impl), InternalList.Scanner<byte>, @_apos_lsqb_rsqb <byte>, ArrayBuilder<byte>))(
			name, scanner, savable, new ArrayBuilder<byte>(), listMode, tupleLength);
		}
		public static Memory<byte> SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, Memory<byte> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager
		
		{
			if ((listMode & SubObjectMode.Deduplicate) != 0)
				throw new ArgumentException("SubObjectMode.Deduplicate is incompatible with Memory<T>");
			var scanner = new InternalList.Scanner<byte>(savable);
			return sync.(@_aposof(NameWithType(SyncList, byte, Impl), InternalList.Scanner<byte>, Memory<byte>, MemoryBuilder<byte>))(
			name, scanner, null, new MemoryBuilder<byte>(), listMode | SubObjectMode.NotNull, tupleLength);
		}
		// Produces an error: CS0111: Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	FieldId name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//{
		//	object? listRef = (listMode & (SubObjectMode.Deduplicate | SubObjectMode.NotNull)) == SubObjectMode.NotNull ? null : savable;
		//	var scanner = (savable == null ? Empty<$T>.Array : Loyc.Collections.MutableListExtensionMethods.LinqToLists.ToArray(savable)).Slice().Scan();
		//	return sync.$(out NameWithType(Sync, $T, ListImpl))<List, InternalList.Scanner<$T>, CollectionBuilder<List, $T>>
		//		(name, scanner, new CollectionBuilder<List, $T>(alloc), listMode, listRef, tupleLength);
		//}
		public static short[]? SyncList<SM>(this SM sync, 
		  FieldId name, short[]? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, short, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static List<short>? SyncList<SM>(this SM sync, 
		  FieldId name, List<short>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, short, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IList<short>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<short>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, short, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static IReadOnlyList<short>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<short>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, short, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IListSource<short>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<short>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, short, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ICollection<short>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<short>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, short, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IReadOnlyCollection<short>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<short>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, short, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static HashSet<short>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<short>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, short, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ReadOnlyMemory<short> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<short> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, short, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static Memory<short> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<short> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, short, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		
		public static ushort[]? SyncList<SM>(this SM sync, 
		  FieldId name, ushort[]? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ushort, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static List<ushort>? SyncList<SM>(this SM sync, 
		  FieldId name, List<ushort>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ushort, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IList<ushort>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<ushort>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ushort, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static IReadOnlyList<ushort>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<ushort>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ushort, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IListSource<ushort>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<ushort>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ushort, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ICollection<ushort>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<ushort>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ushort, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IReadOnlyCollection<ushort>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<ushort>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ushort, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static HashSet<ushort>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<ushort>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ushort, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ReadOnlyMemory<ushort> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<ushort> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ushort, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static Memory<ushort> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<ushort> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ushort, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		
		public static int[]? SyncList<SM>(this SM sync, 
		  FieldId name, int[]? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, int, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static List<int>? SyncList<SM>(this SM sync, 
		  FieldId name, List<int>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, int, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IList<int>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<int>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, int, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static IReadOnlyList<int>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<int>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, int, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IListSource<int>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<int>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, int, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ICollection<int>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<int>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, int, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IReadOnlyCollection<int>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<int>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, int, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static HashSet<int>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<int>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, int, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ReadOnlyMemory<int> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<int> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, int, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static Memory<int> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<int> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, int, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		
		public static uint[]? SyncList<SM>(this SM sync, 
		  FieldId name, uint[]? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, uint, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static List<uint>? SyncList<SM>(this SM sync, 
		  FieldId name, List<uint>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, uint, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IList<uint>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<uint>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, uint, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static IReadOnlyList<uint>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<uint>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, uint, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IListSource<uint>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<uint>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, uint, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ICollection<uint>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<uint>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, uint, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IReadOnlyCollection<uint>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<uint>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, uint, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static HashSet<uint>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<uint>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, uint, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ReadOnlyMemory<uint> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<uint> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, uint, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static Memory<uint> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<uint> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, uint, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		
		public static long[]? SyncList<SM>(this SM sync, 
		  FieldId name, long[]? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, long, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static List<long>? SyncList<SM>(this SM sync, 
		  FieldId name, List<long>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, long, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IList<long>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<long>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, long, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static IReadOnlyList<long>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<long>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, long, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IListSource<long>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<long>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, long, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ICollection<long>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<long>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, long, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IReadOnlyCollection<long>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<long>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, long, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static HashSet<long>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<long>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, long, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ReadOnlyMemory<long> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<long> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, long, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static Memory<long> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<long> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, long, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		
		public static ulong[]? SyncList<SM>(this SM sync, 
		  FieldId name, ulong[]? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ulong, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static List<ulong>? SyncList<SM>(this SM sync, 
		  FieldId name, List<ulong>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ulong, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IList<ulong>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<ulong>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ulong, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static IReadOnlyList<ulong>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<ulong>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ulong, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IListSource<ulong>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<ulong>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ulong, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ICollection<ulong>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<ulong>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ulong, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IReadOnlyCollection<ulong>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<ulong>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ulong, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static HashSet<ulong>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<ulong>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ulong, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ReadOnlyMemory<ulong> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<ulong> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ulong, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static Memory<ulong> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<ulong> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ulong, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		
		public static float[]? SyncList<SM>(this SM sync, 
		  FieldId name, float[]? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, float, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static List<float>? SyncList<SM>(this SM sync, 
		  FieldId name, List<float>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, float, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IList<float>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<float>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, float, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static IReadOnlyList<float>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<float>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, float, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IListSource<float>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<float>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, float, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ICollection<float>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<float>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, float, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IReadOnlyCollection<float>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<float>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, float, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static HashSet<float>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<float>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, float, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ReadOnlyMemory<float> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<float> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, float, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static Memory<float> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<float> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, float, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		
		public static double[]? SyncList<SM>(this SM sync, 
		  FieldId name, double[]? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, double, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static List<double>? SyncList<SM>(this SM sync, 
		  FieldId name, List<double>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, double, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IList<double>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<double>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, double, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static IReadOnlyList<double>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<double>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, double, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IListSource<double>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<double>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, double, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ICollection<double>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<double>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, double, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IReadOnlyCollection<double>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<double>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, double, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static HashSet<double>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<double>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, double, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ReadOnlyMemory<double> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<double> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, double, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static Memory<double> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<double> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, double, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		
		public static decimal[]? SyncList<SM>(this SM sync, 
		  FieldId name, decimal[]? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, decimal, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static List<decimal>? SyncList<SM>(this SM sync, 
		  FieldId name, List<decimal>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, decimal, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IList<decimal>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<decimal>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, decimal, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static IReadOnlyList<decimal>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<decimal>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, decimal, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IListSource<decimal>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<decimal>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, decimal, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ICollection<decimal>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<decimal>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, decimal, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IReadOnlyCollection<decimal>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<decimal>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, decimal, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static HashSet<decimal>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<decimal>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, decimal, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ReadOnlyMemory<decimal> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<decimal> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, decimal, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static Memory<decimal> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<decimal> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, decimal, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		
		public static BigInteger[]? SyncList<SM>(this SM sync, 
		  FieldId name, BigInteger[]? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, BigInteger, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static List<BigInteger>? SyncList<SM>(this SM sync, 
		  FieldId name, List<BigInteger>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, BigInteger, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IList<BigInteger>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<BigInteger>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, BigInteger, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static IReadOnlyList<BigInteger>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<BigInteger>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, BigInteger, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IListSource<BigInteger>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<BigInteger>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, BigInteger, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ICollection<BigInteger>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<BigInteger>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, BigInteger, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IReadOnlyCollection<BigInteger>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<BigInteger>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, BigInteger, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static HashSet<BigInteger>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<BigInteger>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, BigInteger, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ReadOnlyMemory<BigInteger> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<BigInteger> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, BigInteger, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static Memory<BigInteger> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<BigInteger> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, BigInteger, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		
		//
		// TODO: support other list types with byte/char/bool
		//
		//NameWithType
		public static List<char>? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, List<char>? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager
		
		{
			ScannableEnumerable<char>.Scanner<List<char>.Enumerator> scanner = default;
			if (savable != null)
				scanner = new ScannableEnumerable<char>.Scanner<List<char>.Enumerator>(savable.GetEnumerator());
			return sync.(@_aposof(NameWithType(SyncList, char, Impl), ScannableEnumerable<char>.Scanner<List<char>.Enumerator>, List<char>, ListBuilder<char>))(
			name, scanner, savable, new ListBuilder<char>(), listMode, tupleLength);
		}
		public static char[]? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, char[]? savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = new InternalList.Scanner<char>(savable.AsMemory());
			return sync.(@_aposof(NameWithType(SyncList, char, Impl), InternalList.Scanner<char>, @_apos_lsqb_rsqb <char>, ArrayBuilder<char>))(
			name, scanner, savable, new ArrayBuilder<char>(), listMode, tupleLength);
		}
		public static Memory<char> SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, Memory<char> savable, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SyncManager: ISyncManager
		
		{
			if ((listMode & SubObjectMode.Deduplicate) != 0)
				throw new ArgumentException("SubObjectMode.Deduplicate is incompatible with Memory<T>");
			var scanner = new InternalList.Scanner<char>(savable);
			return sync.(@_aposof(NameWithType(SyncList, char, Impl), InternalList.Scanner<char>, Memory<char>, MemoryBuilder<char>))(
			name, scanner, null, new MemoryBuilder<char>(), listMode | SubObjectMode.NotNull, tupleLength);
		}
		// Produces an error: CS0111: Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types
		//public static List? SyncList<SyncManager, List>(this SyncManager sync,
		//	FieldId name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		//	where SyncManager : ISyncManager
		//	where List : ICollection<$T>, IReadOnlyCollection<$T>
		//{
		//	object? listRef = (listMode & (SubObjectMode.Deduplicate | SubObjectMode.NotNull)) == SubObjectMode.NotNull ? null : savable;
		//	var scanner = (savable == null ? Empty<$T>.Array : Loyc.Collections.MutableListExtensionMethods.LinqToLists.ToArray(savable)).Slice().Scan();
		//	return sync.$(out NameWithType(Sync, $T, ListImpl))<List, InternalList.Scanner<$T>, CollectionBuilder<List, $T>>
		//		(name, scanner, new CollectionBuilder<List, $T>(alloc), listMode, listRef, tupleLength);
		//}
		public static string?[]? SyncList<SM>(this SM sync, 
		  FieldId name, string?[]? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, string?, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static List<string?>? SyncList<SM>(this SM sync, 
		  FieldId name, List<string?>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, string?, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IList<string?>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<string?>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, string?, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static IReadOnlyList<string?>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<string?>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, string?, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IListSource<string?>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<string?>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, string?, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ICollection<string?>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<string?>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, string?, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static IReadOnlyCollection<string?>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<string?>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, string?, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static HashSet<string?>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<string?>? savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, string?, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ReadOnlyMemory<string?> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<string?> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, string?, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		public static Memory<string?> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<string?> savable, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, string?, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleLength).Sync(ref sync, name, savable);
		
	}
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	[_numpublic, _numstatic, _numpartial] _numclass(NameWithType(SyncManagerExt, bool), _num(), {
		public static List? SyncList<SyncManager, List>(this SyncManager sync, 
		  FieldId name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		
		 where SyncManager: ISyncManager where List: ICollection<bool>, IReadOnlyCollection<bool>
		{
			var scanner = savable == null ? Empty<bool>.Scanner : Loyc.Collections.MutableListExtensionMethods.LinqToLists.ToArray(savable).Slice(0).Scan();
			return sync.(NameWithType(SyncList, bool, Impl))(name, scanner, savable, new CollectionBuilder<List, bool>(alloc), listMode, tupleLength);
		}

		public static List? SyncList<SyncManager, List>(this SyncManager sync, 
		  string name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		
		 where SyncManager: ISyncManager where List: ICollection<bool>, IReadOnlyCollection<bool> => 
		  SyncList(sync, (FieldId) name, savable, alloc, listMode, tupleLength);
	});
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	[_numpublic, _numstatic, _numpartial] _numclass(NameWithType(SyncManagerExt, sbyte), _num(), {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<sbyte> where SyncField: ISyncField<SM, sbyte> => 
		  
		  new SyncList<SM, sbyte, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	});
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	[_numpublic, _numstatic, _numpartial] _numclass(NameWithType(SyncManagerExt, byte), _num(), {
		public static List? SyncList<SyncManager, List>(this SyncManager sync, 
		  FieldId name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		
		 where SyncManager: ISyncManager where List: ICollection<byte>, IReadOnlyCollection<byte>
		{
			var scanner = savable == null ? Empty<byte>.Scanner : Loyc.Collections.MutableListExtensionMethods.LinqToLists.ToArray(savable).Slice(0).Scan();
			return sync.(NameWithType(SyncList, byte, Impl))(name, scanner, savable, new CollectionBuilder<List, byte>(alloc), listMode, tupleLength);
		}

		public static List? SyncList<SyncManager, List>(this SyncManager sync, 
		  string name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		
		 where SyncManager: ISyncManager where List: ICollection<byte>, IReadOnlyCollection<byte> => 
		  SyncList(sync, (FieldId) name, savable, alloc, listMode, tupleLength);
	});
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	[_numpublic, _numstatic, _numpartial] _numclass(NameWithType(SyncManagerExt, short), _num(), {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<short> where SyncField: ISyncField<SM, short> => 
		  
		  new SyncList<SM, short, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	});
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	[_numpublic, _numstatic, _numpartial] _numclass(NameWithType(SyncManagerExt, ushort), _num(), {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<ushort> where SyncField: ISyncField<SM, ushort> => 
		  
		  new SyncList<SM, ushort, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	});
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	[_numpublic, _numstatic, _numpartial] _numclass(NameWithType(SyncManagerExt, int), _num(), {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<int> where SyncField: ISyncField<SM, int> => 
		  
		  new SyncList<SM, int, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	});
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	[_numpublic, _numstatic, _numpartial] _numclass(NameWithType(SyncManagerExt, uint), _num(), {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<uint> where SyncField: ISyncField<SM, uint> => 
		  
		  new SyncList<SM, uint, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	});
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	[_numpublic, _numstatic, _numpartial] _numclass(NameWithType(SyncManagerExt, long), _num(), {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<long> where SyncField: ISyncField<SM, long> => 
		  
		  new SyncList<SM, long, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	});
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	[_numpublic, _numstatic, _numpartial] _numclass(NameWithType(SyncManagerExt, ulong), _num(), {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<ulong> where SyncField: ISyncField<SM, ulong> => 
		  
		  new SyncList<SM, ulong, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	});
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	[_numpublic, _numstatic, _numpartial] _numclass(NameWithType(SyncManagerExt, float), _num(), {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<float> where SyncField: ISyncField<SM, float> => 
		  
		  new SyncList<SM, float, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	});
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	[_numpublic, _numstatic, _numpartial] _numclass(NameWithType(SyncManagerExt, double), _num(), {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<double> where SyncField: ISyncField<SM, double> => 
		  
		  new SyncList<SM, double, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	});
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	[_numpublic, _numstatic, _numpartial] _numclass(NameWithType(SyncManagerExt, decimal), _num(), {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<decimal> where SyncField: ISyncField<SM, decimal> => 
		  
		  new SyncList<SM, decimal, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	});
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	[_numpublic, _numstatic, _numpartial] _numclass(NameWithType(SyncManagerExt, BigInteger), _num(), {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<BigInteger> where SyncField: ISyncField<SM, BigInteger> => 
		  
		  new SyncList<SM, BigInteger, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	});
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	[_numpublic, _numstatic, _numpartial] _numclass(NameWithType(SyncManagerExt, char), _num(), {
		public static List? SyncList<SyncManager, List>(this SyncManager sync, 
		  FieldId name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		
		 where SyncManager: ISyncManager where List: ICollection<char>, IReadOnlyCollection<char>
		{
			var scanner = savable == null ? Empty<char>.Scanner : Loyc.Collections.MutableListExtensionMethods.LinqToLists.ToArray(savable).Slice(0).Scan();
			return sync.(NameWithType(SyncList, char, Impl))(name, scanner, savable, new CollectionBuilder<List, char>(alloc), listMode, tupleLength);
		}

		public static List? SyncList<SyncManager, List>(this SyncManager sync, 
		  string name, List? savable, Func<int, List> alloc, SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		
		 where SyncManager: ISyncManager where List: ICollection<char>, IReadOnlyCollection<char> => 
		  SyncList(sync, (FieldId) name, savable, alloc, listMode, tupleLength);
	});
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	[_numpublic, _numstatic, _numpartial] _numclass(NameWithType(SyncManagerExt, @_apos_quest <string>), _num(), {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<string?> where SyncField: ISyncField<SM, string?> => 
		  
		  new SyncList<SM, string?, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	});
}