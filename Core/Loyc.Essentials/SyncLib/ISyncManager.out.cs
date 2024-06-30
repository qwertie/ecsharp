// Generated from ISyncManager.ecs by LeMP custom tool. LeMP version: 30.1.91.0
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
		/// <see cref="SyncMode.Reading"/>, <see cref="SyncMode.Schema"/> or 
		/// <see cref="SyncMode.Merge"/>. If your synchronizer method behaves 
		/// differently when it is loading than when it is saving, it's often
		/// more appropriate to get this property rather than testing whether 
		/// <c>Mode == SyncMode.Loading</c>, because if the current mode is 
		/// Schema or Merge, your synchronizer method should do most of its 
		/// "loading" behavior.</summary>
		bool IsReading { get; }

		/// <summary>Returns true if the current <see cref="Mode"/> is 
		/// <see cref="SyncMode.Writing"/>, <see cref="SyncMode.Query"/> or 
		/// <see cref="SyncMode.Merge"/>. If your synchronizer method behaves 
		/// differently when it is loading than when it is saving, you should
		/// almost always get this property rather than testing whether 
		/// <c>Mode == SyncMode.Saving</c>, because if the current mode is 
		/// Query or Merge, your synchronizer method should usually do the same
		/// thing it does when saving.</summary>
		bool IsWriting { get; }

		/// <summary>Indicates that the serialized format has some kind of schema that 
		///   enables fields to be read in a different order than they were written
		///   (e.g. JSON, Protobuf). If this field is false, fields must be read in
		///   the same order they were written, and omitting fields is not allowed
		///   (e.g. you cannot skip over a null field without saving it, nor skip
		///   over a field and then read it later).</summary>
		/// <remarks>If this property is false, the data may not have any recorded 
		///   structure, and failure to read the correct fields in the correct order 
		///   tends to give you "garbage" results.
		///   <para/>
		///   This property should be false for incomplete reader implementations in
		///   which the data format can support reordering physically, but the reader 
		///   does not.
		/// </remarks>
		bool SupportsReordering { get; }

		/// <summary>Indicates that this implementation of <see cref="ISyncManager"/>
		///   supports the <see cref="NextField"/> property. (This property must be
		///   false when <see cref="IsReading"/> is false.)</summary>
		/// <remarks>All implementations where <see cref="SupportsReordering"/> is 
		///   true should also return true for this property.</remarks>
		bool SupportsNextField { get; }

		/// <summary>Returns true if the <see cref="ISyncManager"/> supports 
		/// deduplication of objects and cyclic object graphs. Note: all standard 
		/// implementations of this interface do support deduplication and cyclic
		/// object graphs.</summary>
		bool SupportsDeduplication { get; }

		/// <summary>Indicates that the properties of the current sub-object do not
		/// have names because the basic <see cref="ObjectMode"/> is either
		/// <see cref="ObjectMode.Tuple"/> or <see cref="ObjectMode.List"/>.
		/// In this case, <see cref="SupportsReordering"/> is irrelevant, since 
		/// fields do not have names or ID numbers.</summary>
		bool IsInsideList { get; }

		/// <summary>Indicates that the serialized format uses field ID numbers rather 
		/// than field names (e.g. Protocol Buffers). When using strings or global 
		/// symbols, the ID is indicated implcitly via field order: the first field 
		/// read or written has ID=1, the second has ID=2, etc. If you need to 
		/// customize the field numbers, you can do so by creating a private 
		/// <see cref="Loyc.SymbolPool"/> and creating symbols with custom ID numbers.
		/// </summary>
		bool NeedsIntegerIds { get; }

		/// <summary>If a list is being scanned and the current object can detect
		/// the end of the list (e.g. the mode is <see cref="SyncMode.Loading"/>),
		/// this property returns a boolean value: true at the end of the list and
		/// false otherwise. In all other cases, null is returned.</summary>
		/// <remarks>
		/// <ul>
		/// <li>In Loading mode, the manager knows when the end of the list is reached,
		///     so true or false is returned if a list is being scanned.</li>
		/// <li>In Saving mode, the caller knows the list length but the manager does 
		///     not, so this property always returns null.</li>
		/// <li>In Schema mode, no list actually exists, but the manager typically 
		///     pretends that the list's length is 1.</li>
		/// <li>In Query mode, the manager doesn't know the list length but it may 
		///     have a maximum list length. In this case, this property returns
		///     null at the beginning, then true when the limit is reached.</li>
		/// <li>In Merge mode... this needs some thought; TODO</li>
		/// </ul>
		/// </remarks>
		bool? ReachedEndOfList { get; }

		/// <summary>If a variable-length list is being scanned in Loading mode, this 
		/// property returns either the list length (if known) or the minimum list 
		/// length (if the total length is not known before reading the list).
		/// This property is null in Saving mode, or when a variable-length list is not 
		/// being scanned.</summary>
		/// <remarks>
		/// Some data formats use length-prefixed lists, in which case the list length 
		/// is known from the begining. Other formats (such as JSON) use a delimiter to 
		/// mark the end of the list, so the list length is not known until the end
		/// is reached. In that case, this property should return 0.
		/// </remarks>
		int? MinimumListLength { get; }

		/// <summary>Returns the number of objects for which <see cref="BeginSubObject"/>
		///   opened an object, list or tuple that was not closed <see cref="EndSubObject"/>.
		///   This property is zero if <see cref="BeginSubObject"/> was never called to 
		///   start a subobject.</summary>
		/// <remarks>
		///   If you use a helper method designed to write a single object, such as 
		///   <see cref="SyncJson.Write{T, SyncObject}(T, SyncObject, SyncJson.Options?)"/>
		///   or <see cref="SyncBinary.Write{T, SyncObject}(T, SyncObject, SyncBinary.Options?)"/>,
		///   these helper methods call <see cref="BeginSubObject"/> so the Depth will be 
		///   1 inside the synchronizer for your root object.
		/// <para/>
		///   Typically it is allowed, but not recommended, to write fields at depth 0. 
		///   For example, this code writes two JSON primitives at depth 0:
		/// <pre><![CDATA[
		///     var writer = SyncJson.NewWriter();
		///     writer.Sync("a", 1234.5);
		///     writer.Sync("b", "Hello\t!");
		///     
		///     var output = (ArrayBufferWriter<byte>) writer.Flush();
		///     Console.WriteLine(Encoding.UTF8.GetString(output.WrittenSpan));
		///     Console.WriteLine();
		///     
		///     // Read the data that was just written
		///     var reader = SyncJson.NewReader(output.WrittenMemory);
		///     var a = reader.Sync(null, 0.0);
		///     var b = reader.Sync(null, "");
		///     Console.WriteLine($"a = {a}, b = {b}");
		/// ]]></pre>
		///   The output is pseudo-JSON, formatted like a list without square brackets:
		///   <pre>
		///   1234.5,
		///   "Hello\t!"
		///   </pre>
		///   Officially this is not valid JSON, but as the example shows, it is understood 
		///   by <see cref="SyncJson.Reader"/> which successfully reads it back:
		///   <pre>
		///   a = 1234.5, b = Hello	!
		///   </pre>
		/// </remarks>
		int Depth { get; }

		/// <summary>If <see cref="SupportsNextField"/> is true, the end of the current
		///   object has not been reached, and <see cref="IsInsideList"/> is false, 
		///   this property returns the name or integer ID of the next field in the 
		///   input stream. Otherwise, it returns <see cref="FieldId.Missing"/>.</summary>
		/// <remarks>
		///   Even if a data stream supports reordering (<see cref="SupportsReordering"/>),
		///   it may be inefficient to read fields out-of-order. Therefore, if your
		///   code wants to read as efficiently as possible, and it expects to receive
		///   data out of order, it can use this property to read the fields in the order 
		///   they appear. Another reason you might read data this way is if you expect
		///   to read an extremely large object (e.g. 1 GB or more) and you want to avoid
		///   the extra memory allocations that occur when reading data out of order.
		///   <para/>
		///   Here is an example:
		///   <example>
		///       public MyObject Sync(ISyncManager sm, MyObject? obj)
		///       {
		///           obj ??= new MyObject();
		///           if (!sm.SupportsNextField || sm.NeedsIntegerIds) {
		///               // Synchronize in the normal way
		///               obj.Field1 = sm.Sync("Field1", obj.Field1);
		///               obj.Field2 = sm.Sync("Field2", obj.Field2);
		///               obj.Field3 = sm.Sync("Field3", obj.Field3);
		///           } else {
		///               // Synchronize fields in the order they appear in the input.
		///               FieldId name;
		///               while ((name = sm.NextField) != FieldId.Missing) {
		///                   if (name.Name == "Field1") {
		///                       obj.Field1 = sm.Sync(null, obj.Field1);
		///                   } else if (name.Name == "Field2") {
		///                       obj.Field2 = sm.Sync(null, obj.Field2);
		///                   } else if (name.Name == "Field3") {
		///                       obj.Field3 = sm.Sync(null, obj.Field3);
		///                   } else {
		///                       throw new Exception("Unexpected field: " + name.Name);
		///                   }
		///               }
		///           }
		///           return obj;
		///       }
		///   </example>
		///   <b>Warning</b>: some readers support name conversion, so that the name 
		///   passed to the Sync methods is not the same as the name used in the data 
		///   stream. For example, when <see cref="SyncJson.Options.NameConverter"/> 
		///   is <see cref="SyncJson.ToCamelCase"/>, a name like "Field1" is stored 
		///   as "field1". But NextField may report the actual string from the 
		///   datastream ("field1") which will cause code written this way to fail.
		///   If the camelcase conversion is the only one you intend to support, you
		///   can work around this problem in your synchronizer with uppercase 
		///   comparisons:
		///   <para/>
		///       string? name;
		///       while ((name = sm.NextField.Name?.ToUpperInvariant()) != null) {
		///           if (name == "FIELD1") {
		///               obj.Field1 = sm.Sync(null, obj.Field1);
		///           } else if (name == "FIELD2") {
		///               obj.Field2 = sm.Sync(null, obj.Field2);
		///           } else if (name == "FIELD3") {
		///               obj.Field3 = sm.Sync(null, obj.Field3);
		///           } else {
		///               throw new Exception("Unexpected field: " + name);
		///           }
		///       }
		///   <para/>
		///   However, this workaround reduces the performance advantage of reading 
		///   fields in order.
		///   <para/>
		///   There are three other things worth noticing about this example.
		///   <para/>
		///   First, this example is only designed to support string field names, so it
		///   checks the <see cref="NeedsIntegerIds"/> property and falls back on the
		///   "normal" synchronization style if it is true. You also need a block of
		///   "normal" synchronization code when writing an object to an output stream.
		///   <para/>
		///   Second, notice that this style of reading also allows you to detect
		///   unexpected field names and respond to them (in this example, an exception
		///   is thrown when an unexpected field is encountered).
		///   <para/>
		///   Third, notice the use of null field names (sm.Sync(null, ...)). This is
		///   how you ask <see cref="ISyncManager"/> to synchronize the next field
		///   without regard for the name of that field.
		///   <para/>
		///   Another potential use of this property is to save or load a string 
		///   dictionary:
		///   <example><![CDATA[
		///   public IDictionary<string, string?> Sync(
		///          ISyncManager sm, IDictionary<string, string?>? dict)
		///   {
		///       dict ??= new Dictionary<string, string?>();
		///       if (sm.IsReading) {
		///           if (!sm.SupportsNextField || sm.NeedsIntegerIds || sm.IsWriting)
		///               throw new NotSupportedException(
		///                   "StringDictionarySync is incompatible with this " + sm.GetType().Name);
		///           
		///           string? name;
		///           while ((name = sm.NextField.Name) != null) {
		///               dict[name] = sm.Sync(null, "");
		///           }
		///       } else { // Writing
		///           foreach (var pair in dict)
		///               sm.Sync(pair.Key, pair.Value);
		///       }
		///       return dict;
		///   }
		///   ]]></example>
		///   However, loading/storing a dictionary this way is not compatible with data 
		///   formats that don't use string field names, such as protocol buffers.
		/// </remarks>
		FieldId NextField { get; }

		/// <summary>Some serializers do not support this method (see remarks).
		///   If the method is supported, it determines whether a field with a specific
		///   name exists, and if so, what type it has.</summary>
		/// <param name="name">Name to search for in the current stream. If this 
		///   parameter is <see cref="FieldId.Missing"/> it is interpreted as a request 
		///   to get the type of the next field or the next list item, which is only 
		///   supported if (1) <see cref="SupportsNextField"/> or (2) 
		///   <see cref="SupportsReordering"/> and <see	cref="IsInsideList"/>. If 
		///   <see cref="NeedsIntegerIds"/> is true then the FieldId needs an id number 
		///   to search for. </param>
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
		///   <see cref="IsReading"/>, when (1) <see cref="SupportsReordering"/> is true or
		///   (2) name is <see cref="FieldId.Missing"/> and <see cref="SupportsNextField"/>.
		///   If these conditions are not met, the method normally returns SyncType.Unknown.
		///   <para/>
		///   The <see cref="SyncType"/> enumeration has a collection of common types that 
		///   are supported by most data formats. However, some formats may not support all 
		///   types, or may store data in a different form than you might reasonably expect. 
		///   For example, when a byte array is stored in JSON, it is stored as a string by
		///   default. When reading a byte array from JSON, GetFieldType will report that  
		///   the type is SyncType.String because even though the reader is capable of 
		///   decoding the string as a byte array, it cannot know that the string 
		///   represents a byte array. For this reason, the expectedType parameter exists
		///   as a filtering technique. You can set this parameter to SyncType.ByteList 
		///   to indicate that you expect to read a byte array. If the JSON data type is 
		///   boolean, which cannot be interpreted as a byte array, GetFieldType() returns 
		///   SyncType.Missing. But if the JSON data type is string, GetFieldType() returns 
		///   SyncType.String. This indicates that the data tream contains a String that 
		///   is potentially convertible to SyncType.ByteList, although the conversion is 
		///   not guaranteed to work. If you then read this field by calling
		///   <c>Sync(name, (byte[]) null)</c> and it turns out that the string cannot be 
		///   interpreted as a byte array, an exception will be thrown. After catching
		///   this exception, it may or may not be possible to continue reading from the 
		///   stream, depending on whether the <see cref="ISyncManager"/> was designed to
		///   keep working after that kind of failure.
		///   <para/>
		///   If a value is not implicitly convertible to the expectedType, GetFieldType
		///   should return SyncType.Missing even if the conversion is supported.
		///   For example, if the actual type is Char but a Byte was expected, the
		///   ISyncManager implementation may support this conversion by masking off 
		///   the lowest 8 bits, but it should return SyncType.Missing because this 
		///   kind of conversion loses information and may not be what the user 
		///   intended. (It may seem like this is somewhat in contradiction with the
		///   previous paragraph, because JSON pretends String matches ByteList even 
		///   though the conversion may fail. However, the fact that a ByteList is 
		///   stored as a String is an implementation detail that your code should
		///   not explicitly deal with, so GetFieldType must report that the conversion
		///   is supported in order not to confuse code that is unaware of the 
		///   implementation detail.)
		///   <para/>
		///   It is recommended that implementations of ISyncManager support an 
		///   implicit conversion from boolean to number, as well as conversions from 
		///   "smaller" to "bigger" number types.
		/// </remarks>
		SyncType GetFieldType(FieldId name, SyncType expectedType = SyncType.Unknown);

		/// <summary>Reads or writes a "type tag" for the current object. This method
		///   can only be called once after BeginSubObject returns true, and can only
		///   be called before synchronizing the first subfield (i.e. before calling
		///   any of the Sync() methods).</summary>
		/// <param name="tag">The type tag to write. If <see cref="IsWriting"/> is 
		///   false, this parameter is ignored.</param>
		/// <returns>When <see cref="IsReading"/> is true, the return value is the 
		///   tag stored in the data stream, or null if there is no tag. When 
		///   <see cref="IsReading"/> is false, the return value is <c>tag</c>.
		/// <remarks>
		///   If <see cref="SupportsNextField"/> is false, in order to read a data stream 
		///   correctly, you must call this method if and only if this method was called 
		///   when writing the stream.
		///   <para/>
		///   No behavior has been defined for this method in <see cref="SyncMode.Merge"/>
		///   mode, when <see cref="IsReading"/> and <see cref="IsWriting"/> are both true.
		/// </remarks>
		string? SyncTypeTag(string? tag);
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
		/// <remarks>Set mode to ObjectMode.Deduplicate to deduplicate the string, if supported 
		///   by the current instance of ISyncManager.</remarks>
		string? Sync(FieldId name, string? savablem, ObjectMode mode = ObjectMode.Normal);
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
		/// <summary>This method is used by SyncList() extension methods to read 
		///   or write an array. Users don't need to call it.</summary>
		/// <param name="list">The list that the caller wants to save.
		///   This parameter is ignored when not saving.</param>
		/// <param name="saving">The object being saved. This parameter is used to 
		///   produce an object ID for deduplication, and it can be null if the user 
		///   actually provided a null reference, or if the mode includes
		///   ObjectMode.NotNull and not ObjectMode.Deduplicate.</param>
		/// <param name="builder">A builder used to construct a new list when loading.
		///   This parameter is ignored when not loading.</param>
		/// <param name="tupleOrListLength">When `mode` includes `ObjectMode.Tuple`, this
		///   is a constant specifying the tuple length (which, if <see cref="IsWriting"/>,
		///   must match the list length). Otherwise, if `IsWriting`, this parameter must 
		///   specify the number of items that the `scanner` will return (i.e.
		///   `saving.Count`). Otherwise, this parameter is ignored.</param>
		/// <returns>Returns default(TList) in Saving, Query and Schema modes. 
		///   Otherwise, the data that was loaded via the builder is returned.</returns>
		/// <remarks>If you're trying to implement `ISyncManager`, please see the extra 
		///   documentation above this method in ISyncManager.ecs in SyncLib's GitHub 
		///   repo.</remarks>
		List? SyncListBoolImpl<Scanner, List, ListBuilder>
		(FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleOrListLength = -1)
		
		 where Scanner: IScanner<bool> where ListBuilder: IListBuilder<List, bool>;
		/// <summary>This method is used by SyncList() extension methods to read 
		///   or write an array. Users don't need to call it.</summary>
		/// <param name="list">The list that the caller wants to save.
		///   This parameter is ignored when not saving.</param>
		/// <param name="saving">The object being saved. This parameter is used to 
		///   produce an object ID for deduplication, and it can be null if the user 
		///   actually provided a null reference, or if the mode includes
		///   ObjectMode.NotNull and not ObjectMode.Deduplicate.</param>
		/// <param name="builder">A builder used to construct a new list when loading.
		///   This parameter is ignored when not loading.</param>
		/// <param name="tupleOrListLength">When `mode` includes `ObjectMode.Tuple`, this
		///   is a constant specifying the tuple length (which, if <see cref="IsWriting"/>,
		///   must match the list length). Otherwise, if `IsWriting`, this parameter must 
		///   specify the number of items that the `scanner` will return (i.e.
		///   `saving.Count`). Otherwise, this parameter is ignored.</param>
		/// <returns>Returns default(TList) in Saving, Query and Schema modes. 
		///   Otherwise, the data that was loaded via the builder is returned.</returns>
		/// <remarks>If you're trying to implement `ISyncManager`, please see the extra 
		///   documentation above this method in ISyncManager.ecs in SyncLib's GitHub 
		///   repo.</remarks>
		List? SyncListCharImpl<Scanner, List, ListBuilder>
		(FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleOrListLength = -1)
		
		 where Scanner: IScanner<char> where ListBuilder: IListBuilder<List, char>;
		/// <summary>This method is used by SyncList() extension methods to read 
		///   or write an array. Users don't need to call it.</summary>
		/// <param name="list">The list that the caller wants to save.
		///   This parameter is ignored when not saving.</param>
		/// <param name="saving">The object being saved. This parameter is used to 
		///   produce an object ID for deduplication, and it can be null if the user 
		///   actually provided a null reference, or if the mode includes
		///   ObjectMode.NotNull and not ObjectMode.Deduplicate.</param>
		/// <param name="builder">A builder used to construct a new list when loading.
		///   This parameter is ignored when not loading.</param>
		/// <param name="tupleOrListLength">When `mode` includes `ObjectMode.Tuple`, this
		///   is a constant specifying the tuple length (which, if <see cref="IsWriting"/>,
		///   must match the list length). Otherwise, if `IsWriting`, this parameter must 
		///   specify the number of items that the `scanner` will return (i.e.
		///   `saving.Count`). Otherwise, this parameter is ignored.</param>
		/// <returns>Returns default(TList) in Saving, Query and Schema modes. 
		///   Otherwise, the data that was loaded via the builder is returned.</returns>
		/// <remarks>If you're trying to implement `ISyncManager`, please see the extra 
		///   documentation above this method in ISyncManager.ecs in SyncLib's GitHub 
		///   repo.</remarks>
		List? SyncListByteImpl<Scanner, List, ListBuilder>
		(FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleOrListLength = -1)
		
		 where Scanner: IScanner<byte> where ListBuilder: IListBuilder<List, byte>;

		/// <summary>Sets the "current object" reference. This method must be called 
		///   when deserializing object graphs with cycles (see remarks).</summary>
		/// <remarks>
		///   To understand why this property is needed, consider a Person class that 
		///   has a reference to all the Siblings of the person:
		///   <code>
		///     class Person
		///     {
		///         public string Name;
		///         public int Age;
		///         public Person[] Siblings;
		///     }
		///   </code>
		///   If Jack and Jill are siblings then Jack has a reference to Jill, and Jill 
		///   has a reference back to Jack. A naive implementation of a synchronization 
		///   function for Person might look like this:
		///   <code>
		///     public Person SyncPerson(ISyncManager sync, Person obj)
		///     {
		///         obj ??= new Person();
		///         obj.Name     = sync.SyncNullable("Name", obj.Name);
		///         obj.Age      = sync.Sync("Age", obj.Age);
		///         obj.Siblings = sync.SyncList("Siblings", obj.Siblings, SyncPerson);
		///     }
		///   </code>
		///   But it's impossible for this function to load a Person correctly! To 
		///   understand why, let's think about what <c>SyncList</c> does: it reads a 
		///   list of Persons (synchronously), and returns a <c>Person[]</c>. But each 
		///   Person in that array contains a reference back to the current person. If 
		///   Jack is being loaded, then the <c>Person[]</c> contains Jill, which has a 
		///   reference back to Jack.
		///   <para/>
		///   But <c>SyncList</c> can't return an array that has a reference to Jack, 
		///   because the reference to Jack only exists in the local variable <c>obj</c>.
		///   So as the <c>SyncList</c> method deserializes Jill, Jill's synchronizer 
		///   must fail while reading Jill's list of siblings.
		///   <para/>
		///   To fix this, set <c>CurrentObject</c> before calling <c>SyncList</c>:
		///   <code>
		///     public Person SyncPerson(ISyncManager sync, Person obj)
		///     {
		///         sync.CurrentObject = obj ??= new Person();
		///         obj.Name     = sync.SyncNullable("Name", obj.Name);
		///         obj.Age      = sync.Sync("Age", obj.Age);
		///         obj.Siblings = sync.SyncList("Siblings", obj.Siblings, SyncPerson);
		///     }
		///   </code>
		///   If the current type cannot contain any objects that may refer back to 
		///   itself, then setting <see cref="CurrentObject"/> is optional.
		/// </remarks>
		object CurrentObject { set; }

		/// <summary>Attempts to begin reading or writing a sub-object. This is a
		///   low-level method; end-users normally should not call it. Be sure to 
		///   fully read the documentation before use.</summary>
		/// <param name="name">The name of the property being loaded or saved in
		///   the current object.</param>
		/// <param name="childKey">If the current Mode is Saving or Query, this 
		///   must be a reference that represents the object being saved, or null 
		///   if the object is null. In Loading mode (and sometimes in Schema mode), 
		///   <see cref="ISyncManager"/> ignores this parameter. If a value type is
		///   being read/written, you can set this parameter to null to avoid a
		///   memory allocation for boxing, but only if you use a mode that includes 
		///   ObjectMode.NotNull and not ObjectMode.Deduplicate.</param>
		/// <param name="mode">See <see cref="ObjectMode"/> for information 
		///   about the possible modes. When ObjectMode.NotNull is present and
		///   ObjectMode.Deduplicate is absent, the value of childKey is ignored.</param>
		/// <param name="listLength">If a variable-length list is being written
		///   (i.e. <c>(mode & ObjectMode.List) != 0 && Mode is SyncMode.Saving 
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
		/// <exception cref="ArgumentException">
		///   <c>mode</c> includes <c>ObjectMode.List</c>, <c>listLength</c> was 
		///   negative, and <c>Mode is SyncMode.Saving or SyncMode.Query</c>.
		///   Implementations that don't require the list length in advance do not
		///   necessarily throw this exception.
		/// </exception>
		/// <exception cref="FormatException">
		///   The <see cref="Mode"/> is Reading or Merge and the input stream is 
		///   invalid or does not contain an object or list by the specified name,
		///   so it is impossible to fulfill the request.
		/// </exception>
		/// <remarks>
		///   Typical implementations of <see cref="ISyncManager"/> are used via helper
		///   methods that call this method and write a single object. For example,
		///   <see cref="SyncJson.Write"/> and <see cref="SyncManagerExt.Sync{SM, SyncField, T}(SM, FieldId, T?, SyncField)"/>
		///   call both BeginSubObject and EndSubObject for you automatically.
		///   Please see the remarks of <see cref="Depth"/> about what happens when 
		///   this method is <b>not</b> called.
		///   <para/>
		///   This method has six possible outcomes:
		///   (1) The request to read/write is approved. In this case, this method
		///       returns (true, childKey) and <see cref="Depth"/> increases by one.
		///       childKey is the same reference you passed to this method.
		///   (2) You set childKey = null and <see cref="Mode"/> is not Loading.
		///       (also, <c>(mode & (ObjectMode.NotNull | ObjectMode.Deduplicate)) 
		///       != ObjectMode.NotNull</c>). This indicates that no child object 
		///       exists, so this method returns (false, null).
		///   (3) The <see cref="Mode"/> is Loading and the input stream contains a 
		///       representation of null, so this method returns (false, null).
		///   (4) The list/tuple being read/write has already been read/written 
		///       earlier, and you enabled deduplication, so the request to 
		///       read/write is declined. In this case, this method returns false 
		///       with a reference to the object that was loaded or saved earlier.
		///   (5) The <see cref="Mode"/> is Query, Schema or Merge and the current 
		///       <see cref="ISyncManager"/> has decided not to traverse into the 
		///       current field. In this case, this method returns (false, childKey).
		///   (6) An exception is thrown if the input stream doesn't contain a 
		///       list/object as expected, or if you're writing a list without 
		///       providing <c>listLength</c>.
		///   <para/>
		///   In Saving mode, and in every case except 4, the returned Object is 
		///   the same as childKey.
		/// </remarks>
		(bool Begun, object? Object) BeginSubObject(FieldId name, object? childKey, ObjectMode mode, int listLength = -1);

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
		public static bool SyncRef<SyncManager>(this SyncManager sync, FieldId name, ref bool field) where SyncManager: ISyncManager => 
		  
		  field = sync.Sync(name, field);
		public static sbyte SyncRef<SyncManager>(this SyncManager sync, FieldId name, ref sbyte field) where SyncManager: ISyncManager => 
		  
		  field = sync.Sync(name, field);
		public static byte SyncRef<SyncManager>(this SyncManager sync, FieldId name, ref byte field) where SyncManager: ISyncManager => 
		  
		  field = sync.Sync(name, field);
		public static short SyncRef<SyncManager>(this SyncManager sync, FieldId name, ref short field) where SyncManager: ISyncManager => 
		  
		  field = sync.Sync(name, field);
		public static ushort SyncRef<SyncManager>(this SyncManager sync, FieldId name, ref ushort field) where SyncManager: ISyncManager => 
		  
		  field = sync.Sync(name, field);
		public static int SyncRef<SyncManager>(this SyncManager sync, FieldId name, ref int field) where SyncManager: ISyncManager => 
		  
		  field = sync.Sync(name, field);
		public static uint SyncRef<SyncManager>(this SyncManager sync, FieldId name, ref uint field) where SyncManager: ISyncManager => 
		  
		  field = sync.Sync(name, field);
		public static long SyncRef<SyncManager>(this SyncManager sync, FieldId name, ref long field) where SyncManager: ISyncManager => 
		  
		  field = sync.Sync(name, field);
		public static ulong SyncRef<SyncManager>(this SyncManager sync, FieldId name, ref ulong field) where SyncManager: ISyncManager => 
		  
		  field = sync.Sync(name, field);
		public static float SyncRef<SyncManager>(this SyncManager sync, FieldId name, ref float field) where SyncManager: ISyncManager => 
		  
		  field = sync.Sync(name, field);
		public static double SyncRef<SyncManager>(this SyncManager sync, FieldId name, ref double field) where SyncManager: ISyncManager => 
		  
		  field = sync.Sync(name, field);
		public static decimal SyncRef<SyncManager>(this SyncManager sync, FieldId name, ref decimal field) where SyncManager: ISyncManager => 
		  
		  field = sync.Sync(name, field);
		public static BigInteger SyncRef<SyncManager>(this SyncManager sync, FieldId name, ref BigInteger field) where SyncManager: ISyncManager => 
		  
		  field = sync.Sync(name, field);
		public static char SyncRef<SyncManager>(this SyncManager sync, FieldId name, ref char field) where SyncManager: ISyncManager => 
		  
		  field = sync.Sync(name, field);
		public static string? SyncRef<SyncManager>(this SyncManager sync, FieldId name, ref string? field) where SyncManager: ISyncManager => 
		  
		  field = sync.Sync(name, field);

		
		// SyncList methods for Bool
		public static bool[]? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, bool[]? savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			InternalList.Scanner<bool> scanner = default;
			if (savable != null) {
				scanner = new InternalList.Scanner<bool>(savable.AsMemory());
				tupleOrListLength = savable.Length;
			}
			return sync.SyncListBoolImpl<InternalList.Scanner<bool>, bool[], ArrayBuilder<bool>>(
			name, scanner, savable, new ArrayBuilder<bool>(), listMode, tupleOrListLength);
		}
		public static List<bool>? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, List<bool>? savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = default(ScannableEnumerable<bool>.Scanner<IEnumerator<bool>>);
			if (savable != null && sync.IsWriting) {
				scanner = new ScannableEnumerable<bool>.Scanner<IEnumerator<bool>>(savable.GetEnumerator());
				tupleOrListLength = savable.Count;
			}
			return sync.SyncListBoolImpl<ScannableEnumerable<bool>.Scanner<IEnumerator<bool>>, List<bool>?, ListBuilder<bool>>(
			name, scanner, savable, new ListBuilder<bool>(), listMode, tupleOrListLength);
		}

		public static IList<bool>? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, IList<bool>? savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = default(ScannableEnumerable<bool>.Scanner<IEnumerator<bool>>);
			if (savable != null && sync.IsWriting) {
				scanner = new ScannableEnumerable<bool>.Scanner<IEnumerator<bool>>(savable.GetEnumerator());
				tupleOrListLength = savable.Count;
			}
			return sync.SyncListBoolImpl<ScannableEnumerable<bool>.Scanner<IEnumerator<bool>>, IList<bool>?, ListBuilder<bool>>(
			name, scanner, savable, new ListBuilder<bool>(), listMode, tupleOrListLength);
		}

		public static IReadOnlyList<bool>? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, IReadOnlyList<bool>? savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = default(ScannableEnumerable<bool>.Scanner<IEnumerator<bool>>);
			if (savable != null && sync.IsWriting) {
				scanner = new ScannableEnumerable<bool>.Scanner<IEnumerator<bool>>(savable.GetEnumerator());
				tupleOrListLength = savable.Count;
			}
			return sync.SyncListBoolImpl<ScannableEnumerable<bool>.Scanner<IEnumerator<bool>>, IReadOnlyList<bool>?, ListBuilder<bool>>(
			name, scanner, savable, new ListBuilder<bool>(), listMode, tupleOrListLength);
		}

		public static ICollection<bool>? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, ICollection<bool>? savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = default(ScannableEnumerable<bool>.Scanner<IEnumerator<bool>>);
			if (savable != null && sync.IsWriting) {
				scanner = new ScannableEnumerable<bool>.Scanner<IEnumerator<bool>>(savable.GetEnumerator());
				tupleOrListLength = savable.Count;
			}
			return sync.SyncListBoolImpl<ScannableEnumerable<bool>.Scanner<IEnumerator<bool>>, ICollection<bool>?, ListBuilder<bool>>(
			name, scanner, savable, new ListBuilder<bool>(), listMode, tupleOrListLength);
		}

		public static IReadOnlyCollection<bool>? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, IReadOnlyCollection<bool>? savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = default(ScannableEnumerable<bool>.Scanner<IEnumerator<bool>>);
			if (savable != null && sync.IsWriting) {
				scanner = new ScannableEnumerable<bool>.Scanner<IEnumerator<bool>>(savable.GetEnumerator());
				tupleOrListLength = savable.Count;
			}
			return sync.SyncListBoolImpl<ScannableEnumerable<bool>.Scanner<IEnumerator<bool>>, IReadOnlyCollection<bool>?, ListBuilder<bool>>(
			name, scanner, savable, new ListBuilder<bool>(), listMode, tupleOrListLength);
		}
		
		public static Memory<bool> SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, Memory<bool> savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			if ((listMode & ObjectMode.Deduplicate) != 0)
				throw new ArgumentException("ObjectMode.Deduplicate is incompatible with Memory<T>");
			if (tupleOrListLength <= -1)
				tupleOrListLength = savable.Length;
			var scanner = new InternalList.Scanner<bool>(savable);
			return sync.SyncListBoolImpl<InternalList.Scanner<bool>, Memory<bool>, MemoryBuilder<bool>>(
			name, scanner, null, new MemoryBuilder<bool>(), listMode | ObjectMode.NotNull, tupleOrListLength);
		}
		public static ReadOnlyMemory<bool> SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, ReadOnlyMemory<bool> savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			if ((listMode & ObjectMode.Deduplicate) != 0)
				throw new ArgumentException("ObjectMode.Deduplicate is incompatible with ReadOnlyMemory<T>");
			if (tupleOrListLength <= -1)
				tupleOrListLength = savable.Length;
			var scanner = new InternalList.Scanner<bool>(savable);
			return sync.SyncListBoolImpl<InternalList.Scanner<bool>, ReadOnlyMemory<bool>, MemoryBuilder<bool>>(
			name, scanner, null, new MemoryBuilder<bool>(), listMode | ObjectMode.NotNull, tupleOrListLength);
		}
		public static IListSource<bool> SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, IListSource<bool> savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = default(ScannableEnumerable<bool>.Scanner<IEnumerator<bool>>);
			if (savable != null && sync.IsWriting) {
				scanner = new ScannableEnumerable<bool>.Scanner<IEnumerator<bool>>(savable.GetEnumerator());
				tupleOrListLength = savable.Count;
			}
			var builder = new CollectionBuilder<DList<bool>, bool>(minLen => minLen > 0 ? new DList<bool>(minLen) : new DList<bool>());
			return sync.SyncListBoolImpl<ScannableEnumerable<bool>.Scanner<IEnumerator<bool>>, IListSource<bool>, CollectionBuilder<DList<bool>, bool>>(
			name, scanner, savable, builder, listMode, tupleOrListLength);
		}

		// SyncList methods for SByte
		public static sbyte[]? SyncList<SM>(this SM sync, 
		  FieldId name, sbyte[]? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, sbyte, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static List<sbyte>? SyncList<SM>(this SM sync, 
		  FieldId name, List<sbyte>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, sbyte, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IList<sbyte>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<sbyte>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, sbyte, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static IReadOnlyList<sbyte>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<sbyte>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, sbyte, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IListSource<sbyte>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<sbyte>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, sbyte, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ICollection<sbyte>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<sbyte>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, sbyte, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IReadOnlyCollection<sbyte>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<sbyte>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, sbyte, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static HashSet<sbyte>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<sbyte>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, sbyte, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ReadOnlyMemory<sbyte> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<sbyte> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, sbyte, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static Memory<sbyte> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<sbyte> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, sbyte, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		

		
		// SyncList methods for Byte
		public static byte[]? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, byte[]? savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			InternalList.Scanner<byte> scanner = default;
			if (savable != null) {
				scanner = new InternalList.Scanner<byte>(savable.AsMemory());
				tupleOrListLength = savable.Length;
			}
			return sync.SyncListByteImpl<InternalList.Scanner<byte>, byte[], ArrayBuilder<byte>>(
			name, scanner, savable, new ArrayBuilder<byte>(), listMode, tupleOrListLength);
		}
		public static List<byte>? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, List<byte>? savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = default(ScannableEnumerable<byte>.Scanner<IEnumerator<byte>>);
			if (savable != null && sync.IsWriting) {
				scanner = new ScannableEnumerable<byte>.Scanner<IEnumerator<byte>>(savable.GetEnumerator());
				tupleOrListLength = savable.Count;
			}
			return sync.SyncListByteImpl<ScannableEnumerable<byte>.Scanner<IEnumerator<byte>>, List<byte>?, ListBuilder<byte>>(
			name, scanner, savable, new ListBuilder<byte>(), listMode, tupleOrListLength);
		}

		public static IList<byte>? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, IList<byte>? savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = default(ScannableEnumerable<byte>.Scanner<IEnumerator<byte>>);
			if (savable != null && sync.IsWriting) {
				scanner = new ScannableEnumerable<byte>.Scanner<IEnumerator<byte>>(savable.GetEnumerator());
				tupleOrListLength = savable.Count;
			}
			return sync.SyncListByteImpl<ScannableEnumerable<byte>.Scanner<IEnumerator<byte>>, IList<byte>?, ListBuilder<byte>>(
			name, scanner, savable, new ListBuilder<byte>(), listMode, tupleOrListLength);
		}

		public static IReadOnlyList<byte>? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, IReadOnlyList<byte>? savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = default(ScannableEnumerable<byte>.Scanner<IEnumerator<byte>>);
			if (savable != null && sync.IsWriting) {
				scanner = new ScannableEnumerable<byte>.Scanner<IEnumerator<byte>>(savable.GetEnumerator());
				tupleOrListLength = savable.Count;
			}
			return sync.SyncListByteImpl<ScannableEnumerable<byte>.Scanner<IEnumerator<byte>>, IReadOnlyList<byte>?, ListBuilder<byte>>(
			name, scanner, savable, new ListBuilder<byte>(), listMode, tupleOrListLength);
		}

		public static ICollection<byte>? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, ICollection<byte>? savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = default(ScannableEnumerable<byte>.Scanner<IEnumerator<byte>>);
			if (savable != null && sync.IsWriting) {
				scanner = new ScannableEnumerable<byte>.Scanner<IEnumerator<byte>>(savable.GetEnumerator());
				tupleOrListLength = savable.Count;
			}
			return sync.SyncListByteImpl<ScannableEnumerable<byte>.Scanner<IEnumerator<byte>>, ICollection<byte>?, ListBuilder<byte>>(
			name, scanner, savable, new ListBuilder<byte>(), listMode, tupleOrListLength);
		}

		public static IReadOnlyCollection<byte>? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, IReadOnlyCollection<byte>? savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = default(ScannableEnumerable<byte>.Scanner<IEnumerator<byte>>);
			if (savable != null && sync.IsWriting) {
				scanner = new ScannableEnumerable<byte>.Scanner<IEnumerator<byte>>(savable.GetEnumerator());
				tupleOrListLength = savable.Count;
			}
			return sync.SyncListByteImpl<ScannableEnumerable<byte>.Scanner<IEnumerator<byte>>, IReadOnlyCollection<byte>?, ListBuilder<byte>>(
			name, scanner, savable, new ListBuilder<byte>(), listMode, tupleOrListLength);
		}
		
		public static Memory<byte> SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, Memory<byte> savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			if ((listMode & ObjectMode.Deduplicate) != 0)
				throw new ArgumentException("ObjectMode.Deduplicate is incompatible with Memory<T>");
			if (tupleOrListLength <= -1)
				tupleOrListLength = savable.Length;
			var scanner = new InternalList.Scanner<byte>(savable);
			return sync.SyncListByteImpl<InternalList.Scanner<byte>, Memory<byte>, MemoryBuilder<byte>>(
			name, scanner, null, new MemoryBuilder<byte>(), listMode | ObjectMode.NotNull, tupleOrListLength);
		}
		public static ReadOnlyMemory<byte> SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, ReadOnlyMemory<byte> savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			if ((listMode & ObjectMode.Deduplicate) != 0)
				throw new ArgumentException("ObjectMode.Deduplicate is incompatible with ReadOnlyMemory<T>");
			if (tupleOrListLength <= -1)
				tupleOrListLength = savable.Length;
			var scanner = new InternalList.Scanner<byte>(savable);
			return sync.SyncListByteImpl<InternalList.Scanner<byte>, ReadOnlyMemory<byte>, MemoryBuilder<byte>>(
			name, scanner, null, new MemoryBuilder<byte>(), listMode | ObjectMode.NotNull, tupleOrListLength);
		}
		public static IListSource<byte> SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, IListSource<byte> savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = default(ScannableEnumerable<byte>.Scanner<IEnumerator<byte>>);
			if (savable != null && sync.IsWriting) {
				scanner = new ScannableEnumerable<byte>.Scanner<IEnumerator<byte>>(savable.GetEnumerator());
				tupleOrListLength = savable.Count;
			}
			var builder = new CollectionBuilder<DList<byte>, byte>(minLen => minLen > 0 ? new DList<byte>(minLen) : new DList<byte>());
			return sync.SyncListByteImpl<ScannableEnumerable<byte>.Scanner<IEnumerator<byte>>, IListSource<byte>, CollectionBuilder<DList<byte>, byte>>(
			name, scanner, savable, builder, listMode, tupleOrListLength);
		}

		// SyncList methods for Short
		public static short[]? SyncList<SM>(this SM sync, 
		  FieldId name, short[]? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, short, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static List<short>? SyncList<SM>(this SM sync, 
		  FieldId name, List<short>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, short, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IList<short>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<short>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, short, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static IReadOnlyList<short>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<short>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, short, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IListSource<short>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<short>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, short, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ICollection<short>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<short>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, short, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IReadOnlyCollection<short>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<short>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, short, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static HashSet<short>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<short>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, short, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ReadOnlyMemory<short> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<short> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, short, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static Memory<short> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<short> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, short, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		

		// SyncList methods for UShort
		public static ushort[]? SyncList<SM>(this SM sync, 
		  FieldId name, ushort[]? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ushort, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static List<ushort>? SyncList<SM>(this SM sync, 
		  FieldId name, List<ushort>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ushort, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IList<ushort>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<ushort>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ushort, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static IReadOnlyList<ushort>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<ushort>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ushort, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IListSource<ushort>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<ushort>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ushort, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ICollection<ushort>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<ushort>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ushort, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IReadOnlyCollection<ushort>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<ushort>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ushort, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static HashSet<ushort>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<ushort>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ushort, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ReadOnlyMemory<ushort> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<ushort> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ushort, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static Memory<ushort> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<ushort> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ushort, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		

		// SyncList methods for Int
		public static int[]? SyncList<SM>(this SM sync, 
		  FieldId name, int[]? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, int, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static List<int>? SyncList<SM>(this SM sync, 
		  FieldId name, List<int>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, int, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IList<int>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<int>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, int, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static IReadOnlyList<int>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<int>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, int, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IListSource<int>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<int>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, int, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ICollection<int>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<int>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, int, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IReadOnlyCollection<int>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<int>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, int, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static HashSet<int>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<int>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, int, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ReadOnlyMemory<int> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<int> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, int, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static Memory<int> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<int> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, int, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		

		// SyncList methods for UInt
		public static uint[]? SyncList<SM>(this SM sync, 
		  FieldId name, uint[]? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, uint, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static List<uint>? SyncList<SM>(this SM sync, 
		  FieldId name, List<uint>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, uint, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IList<uint>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<uint>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, uint, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static IReadOnlyList<uint>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<uint>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, uint, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IListSource<uint>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<uint>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, uint, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ICollection<uint>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<uint>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, uint, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IReadOnlyCollection<uint>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<uint>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, uint, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static HashSet<uint>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<uint>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, uint, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ReadOnlyMemory<uint> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<uint> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, uint, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static Memory<uint> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<uint> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, uint, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		

		// SyncList methods for Long
		public static long[]? SyncList<SM>(this SM sync, 
		  FieldId name, long[]? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, long, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static List<long>? SyncList<SM>(this SM sync, 
		  FieldId name, List<long>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, long, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IList<long>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<long>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, long, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static IReadOnlyList<long>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<long>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, long, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IListSource<long>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<long>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, long, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ICollection<long>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<long>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, long, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IReadOnlyCollection<long>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<long>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, long, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static HashSet<long>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<long>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, long, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ReadOnlyMemory<long> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<long> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, long, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static Memory<long> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<long> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, long, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		

		// SyncList methods for ULong
		public static ulong[]? SyncList<SM>(this SM sync, 
		  FieldId name, ulong[]? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ulong, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static List<ulong>? SyncList<SM>(this SM sync, 
		  FieldId name, List<ulong>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ulong, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IList<ulong>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<ulong>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ulong, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static IReadOnlyList<ulong>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<ulong>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ulong, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IListSource<ulong>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<ulong>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ulong, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ICollection<ulong>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<ulong>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ulong, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IReadOnlyCollection<ulong>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<ulong>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ulong, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static HashSet<ulong>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<ulong>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ulong, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ReadOnlyMemory<ulong> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<ulong> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ulong, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static Memory<ulong> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<ulong> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, ulong, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		

		// SyncList methods for Float
		public static float[]? SyncList<SM>(this SM sync, 
		  FieldId name, float[]? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, float, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static List<float>? SyncList<SM>(this SM sync, 
		  FieldId name, List<float>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, float, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IList<float>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<float>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, float, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static IReadOnlyList<float>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<float>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, float, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IListSource<float>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<float>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, float, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ICollection<float>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<float>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, float, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IReadOnlyCollection<float>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<float>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, float, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static HashSet<float>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<float>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, float, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ReadOnlyMemory<float> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<float> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, float, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static Memory<float> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<float> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, float, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		

		// SyncList methods for Double
		public static double[]? SyncList<SM>(this SM sync, 
		  FieldId name, double[]? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, double, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static List<double>? SyncList<SM>(this SM sync, 
		  FieldId name, List<double>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, double, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IList<double>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<double>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, double, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static IReadOnlyList<double>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<double>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, double, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IListSource<double>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<double>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, double, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ICollection<double>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<double>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, double, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IReadOnlyCollection<double>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<double>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, double, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static HashSet<double>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<double>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, double, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ReadOnlyMemory<double> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<double> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, double, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static Memory<double> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<double> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, double, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		

		// SyncList methods for Decimal
		public static decimal[]? SyncList<SM>(this SM sync, 
		  FieldId name, decimal[]? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, decimal, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static List<decimal>? SyncList<SM>(this SM sync, 
		  FieldId name, List<decimal>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, decimal, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IList<decimal>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<decimal>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, decimal, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static IReadOnlyList<decimal>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<decimal>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, decimal, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IListSource<decimal>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<decimal>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, decimal, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ICollection<decimal>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<decimal>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, decimal, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IReadOnlyCollection<decimal>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<decimal>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, decimal, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static HashSet<decimal>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<decimal>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, decimal, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ReadOnlyMemory<decimal> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<decimal> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, decimal, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static Memory<decimal> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<decimal> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, decimal, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		

		// SyncList methods for BigInteger
		public static BigInteger[]? SyncList<SM>(this SM sync, 
		  FieldId name, BigInteger[]? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, BigInteger, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static List<BigInteger>? SyncList<SM>(this SM sync, 
		  FieldId name, List<BigInteger>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, BigInteger, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IList<BigInteger>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<BigInteger>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, BigInteger, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static IReadOnlyList<BigInteger>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<BigInteger>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, BigInteger, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IListSource<BigInteger>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<BigInteger>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, BigInteger, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ICollection<BigInteger>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<BigInteger>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, BigInteger, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IReadOnlyCollection<BigInteger>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<BigInteger>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, BigInteger, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static HashSet<BigInteger>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<BigInteger>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, BigInteger, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ReadOnlyMemory<BigInteger> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<BigInteger> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, BigInteger, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static Memory<BigInteger> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<BigInteger> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, BigInteger, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		

		
		// SyncList methods for Char
		public static char[]? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, char[]? savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			InternalList.Scanner<char> scanner = default;
			if (savable != null) {
				scanner = new InternalList.Scanner<char>(savable.AsMemory());
				tupleOrListLength = savable.Length;
			}
			return sync.SyncListCharImpl<InternalList.Scanner<char>, char[], ArrayBuilder<char>>(
			name, scanner, savable, new ArrayBuilder<char>(), listMode, tupleOrListLength);
		}
		public static List<char>? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, List<char>? savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = default(ScannableEnumerable<char>.Scanner<IEnumerator<char>>);
			if (savable != null && sync.IsWriting) {
				scanner = new ScannableEnumerable<char>.Scanner<IEnumerator<char>>(savable.GetEnumerator());
				tupleOrListLength = savable.Count;
			}
			return sync.SyncListCharImpl<ScannableEnumerable<char>.Scanner<IEnumerator<char>>, List<char>?, ListBuilder<char>>(
			name, scanner, savable, new ListBuilder<char>(), listMode, tupleOrListLength);
		}

		public static IList<char>? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, IList<char>? savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = default(ScannableEnumerable<char>.Scanner<IEnumerator<char>>);
			if (savable != null && sync.IsWriting) {
				scanner = new ScannableEnumerable<char>.Scanner<IEnumerator<char>>(savable.GetEnumerator());
				tupleOrListLength = savable.Count;
			}
			return sync.SyncListCharImpl<ScannableEnumerable<char>.Scanner<IEnumerator<char>>, IList<char>?, ListBuilder<char>>(
			name, scanner, savable, new ListBuilder<char>(), listMode, tupleOrListLength);
		}

		public static IReadOnlyList<char>? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, IReadOnlyList<char>? savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = default(ScannableEnumerable<char>.Scanner<IEnumerator<char>>);
			if (savable != null && sync.IsWriting) {
				scanner = new ScannableEnumerable<char>.Scanner<IEnumerator<char>>(savable.GetEnumerator());
				tupleOrListLength = savable.Count;
			}
			return sync.SyncListCharImpl<ScannableEnumerable<char>.Scanner<IEnumerator<char>>, IReadOnlyList<char>?, ListBuilder<char>>(
			name, scanner, savable, new ListBuilder<char>(), listMode, tupleOrListLength);
		}

		public static ICollection<char>? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, ICollection<char>? savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = default(ScannableEnumerable<char>.Scanner<IEnumerator<char>>);
			if (savable != null && sync.IsWriting) {
				scanner = new ScannableEnumerable<char>.Scanner<IEnumerator<char>>(savable.GetEnumerator());
				tupleOrListLength = savable.Count;
			}
			return sync.SyncListCharImpl<ScannableEnumerable<char>.Scanner<IEnumerator<char>>, ICollection<char>?, ListBuilder<char>>(
			name, scanner, savable, new ListBuilder<char>(), listMode, tupleOrListLength);
		}

		public static IReadOnlyCollection<char>? SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, IReadOnlyCollection<char>? savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = default(ScannableEnumerable<char>.Scanner<IEnumerator<char>>);
			if (savable != null && sync.IsWriting) {
				scanner = new ScannableEnumerable<char>.Scanner<IEnumerator<char>>(savable.GetEnumerator());
				tupleOrListLength = savable.Count;
			}
			return sync.SyncListCharImpl<ScannableEnumerable<char>.Scanner<IEnumerator<char>>, IReadOnlyCollection<char>?, ListBuilder<char>>(
			name, scanner, savable, new ListBuilder<char>(), listMode, tupleOrListLength);
		}
		
		public static Memory<char> SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, Memory<char> savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			if ((listMode & ObjectMode.Deduplicate) != 0)
				throw new ArgumentException("ObjectMode.Deduplicate is incompatible with Memory<T>");
			if (tupleOrListLength <= -1)
				tupleOrListLength = savable.Length;
			var scanner = new InternalList.Scanner<char>(savable);
			return sync.SyncListCharImpl<InternalList.Scanner<char>, Memory<char>, MemoryBuilder<char>>(
			name, scanner, null, new MemoryBuilder<char>(), listMode | ObjectMode.NotNull, tupleOrListLength);
		}
		public static ReadOnlyMemory<char> SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, ReadOnlyMemory<char> savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			if ((listMode & ObjectMode.Deduplicate) != 0)
				throw new ArgumentException("ObjectMode.Deduplicate is incompatible with ReadOnlyMemory<T>");
			if (tupleOrListLength <= -1)
				tupleOrListLength = savable.Length;
			var scanner = new InternalList.Scanner<char>(savable);
			return sync.SyncListCharImpl<InternalList.Scanner<char>, ReadOnlyMemory<char>, MemoryBuilder<char>>(
			name, scanner, null, new MemoryBuilder<char>(), listMode | ObjectMode.NotNull, tupleOrListLength);
		}
		public static IListSource<char> SyncList<SyncManager>(this SyncManager sync, 
		  FieldId name, IListSource<char> savable, ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SyncManager: ISyncManager
		
		{
			var scanner = default(ScannableEnumerable<char>.Scanner<IEnumerator<char>>);
			if (savable != null && sync.IsWriting) {
				scanner = new ScannableEnumerable<char>.Scanner<IEnumerator<char>>(savable.GetEnumerator());
				tupleOrListLength = savable.Count;
			}
			var builder = new CollectionBuilder<DList<char>, char>(minLen => minLen > 0 ? new DList<char>(minLen) : new DList<char>());
			return sync.SyncListCharImpl<ScannableEnumerable<char>.Scanner<IEnumerator<char>>, IListSource<char>, CollectionBuilder<DList<char>, char>>(
			name, scanner, savable, builder, listMode, tupleOrListLength);
		}

		// SyncList methods for String
		public static string?[]? SyncList<SM>(this SM sync, 
		  FieldId name, string?[]? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, string?, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static List<string?>? SyncList<SM>(this SM sync, 
		  FieldId name, List<string?>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, string?, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IList<string?>? SyncList<SM>(this SM sync, 
		  FieldId name, IList<string?>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, string?, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static IReadOnlyList<string?>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyList<string?>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, string?, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IListSource<string?>? SyncList<SM>(this SM sync, 
		  FieldId name, IListSource<string?>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, string?, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ICollection<string?>? SyncList<SM>(this SM sync, 
		  FieldId name, ICollection<string?>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, string?, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static IReadOnlyCollection<string?>? SyncList<SM>(this SM sync, 
		  FieldId name, IReadOnlyCollection<string?>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, string?, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static HashSet<string?>? SyncList<SM>(this SM sync, 
		  FieldId name, HashSet<string?>? savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, string?, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);

		public static ReadOnlyMemory<string?> SyncList<SM>(this SM sync, 
		  FieldId name, ReadOnlyMemory<string?> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, string?, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		public static Memory<string?> SyncList<SM>(this SM sync, 
		  FieldId name, Memory<string?> savable, 
		  ObjectMode listMode = ObjectMode.List, int tupleOrListLength = -1) where SM: ISyncManager => 
		  
		  new SyncList<SM, string?, SyncPrimitive<SM>>(new SyncPrimitive<SM>(), listMode, tupleOrListLength)
		  .Sync(ref sync, name, savable);
		
	}
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	public static partial class SyncManagerExtBool {
		public static List? SyncColl<SyncManager, List>(this SyncManager sync, 
		  FieldId name, List? savable, Func<int, List> alloc, ObjectMode listMode = ObjectMode.List, int tupleLength = -1)
		
		 where SyncManager: ISyncManager where List: ICollection<bool>, IReadOnlyCollection<bool>
		{
			InternalList.Scanner<bool> scanner = Empty<bool>.Scanner;
			if (savable != null)
				scanner = Loyc.Collections.MutableListExtensionMethods.LinqToLists.ToArray(savable).Slice(0).Scan();
			return sync.SyncListBoolImpl(name, scanner, savable, new CollectionBuilder<List, bool>(alloc), listMode, tupleLength);
		}
	}
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	public static partial class SyncManagerExtSByte {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  ObjectMode listMode = ObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<sbyte> where SyncField: ISyncField<SM, sbyte> => 
		  
		  new SyncList<SM, sbyte, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	}
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	public static partial class SyncManagerExtByte {
		public static List? SyncColl<SyncManager, List>(this SyncManager sync, 
		  FieldId name, List? savable, Func<int, List> alloc, ObjectMode listMode = ObjectMode.List, int tupleLength = -1)
		
		 where SyncManager: ISyncManager where List: ICollection<byte>, IReadOnlyCollection<byte>
		{
			InternalList.Scanner<byte> scanner = Empty<byte>.Scanner;
			if (savable != null)
				scanner = Loyc.Collections.MutableListExtensionMethods.LinqToLists.ToArray(savable).Slice(0).Scan();
			return sync.SyncListByteImpl(name, scanner, savable, new CollectionBuilder<List, byte>(alloc), listMode, tupleLength);
		}
	}
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	public static partial class SyncManagerExtShort {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  ObjectMode listMode = ObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<short> where SyncField: ISyncField<SM, short> => 
		  
		  new SyncList<SM, short, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	}
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	public static partial class SyncManagerExtUShort {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  ObjectMode listMode = ObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<ushort> where SyncField: ISyncField<SM, ushort> => 
		  
		  new SyncList<SM, ushort, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	}
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	public static partial class SyncManagerExtInt {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  ObjectMode listMode = ObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<int> where SyncField: ISyncField<SM, int> => 
		  
		  new SyncList<SM, int, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	}
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	public static partial class SyncManagerExtUInt {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  ObjectMode listMode = ObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<uint> where SyncField: ISyncField<SM, uint> => 
		  
		  new SyncList<SM, uint, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	}
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	public static partial class SyncManagerExtLong {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  ObjectMode listMode = ObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<long> where SyncField: ISyncField<SM, long> => 
		  
		  new SyncList<SM, long, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	}
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	public static partial class SyncManagerExtULong {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  ObjectMode listMode = ObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<ulong> where SyncField: ISyncField<SM, ulong> => 
		  
		  new SyncList<SM, ulong, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	}
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	public static partial class SyncManagerExtFloat {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  ObjectMode listMode = ObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<float> where SyncField: ISyncField<SM, float> => 
		  
		  new SyncList<SM, float, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	}
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	public static partial class SyncManagerExtDouble {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  ObjectMode listMode = ObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<double> where SyncField: ISyncField<SM, double> => 
		  
		  new SyncList<SM, double, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	}
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	public static partial class SyncManagerExtDecimal {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  ObjectMode listMode = ObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<decimal> where SyncField: ISyncField<SM, decimal> => 
		  
		  new SyncList<SM, decimal, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	}
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	public static partial class SyncManagerExtBigInteger {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  ObjectMode listMode = ObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<BigInteger> where SyncField: ISyncField<SM, BigInteger> => 
		  
		  new SyncList<SM, BigInteger, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	}
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	public static partial class SyncManagerExtChar {
		public static List? SyncColl<SyncManager, List>(this SyncManager sync, 
		  FieldId name, List? savable, Func<int, List> alloc, ObjectMode listMode = ObjectMode.List, int tupleLength = -1)
		
		 where SyncManager: ISyncManager where List: ICollection<char>, IReadOnlyCollection<char>
		{
			InternalList.Scanner<char> scanner = Empty<char>.Scanner;
			if (savable != null)
				scanner = Loyc.Collections.MutableListExtensionMethods.LinqToLists.ToArray(savable).Slice(0).Scan();
			return sync.SyncListCharImpl(name, scanner, savable, new CollectionBuilder<List, char>(alloc), listMode, tupleLength);
		}
	}
	/// <summary>The methods in this class belong in <see cref="SyncManagerExt"/> but they 
	/// must be put in a different class to avoid C# compiler error CS0111, 
	/// "Type 'SyncManagerExt' already defines a member called 'SyncList' with the same parameter types".</summary>
	public static partial class SyncManagerExtString {
		public static Coll? SyncColl<SM, Coll, SyncField>(this SM sync, 
		  FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc, 
		  ObjectMode listMode = ObjectMode.List, int tupleLength = -1)
		
		 where SM: ISyncManager where Coll: ICollection<string?> where SyncField: ISyncField<SM, string?> => 
		  
		  new SyncList<SM, string?, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);
	}
}