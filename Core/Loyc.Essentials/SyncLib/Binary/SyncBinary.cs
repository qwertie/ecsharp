using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Collections;
using System.Buffers;
using Loyc.Compatibility;

namespace Loyc.SyncLib;

/// <summary>
///   Convenience methods for a fast pair of <see cref="ISyncManager"/> implementations
///   intended for binary files/streams. The binary format is very fast and compact 
///   because it stores no metadata, but it must be used with care to avoid data 
///   corruption (see remarks).
/// </summary>
/// <remarks>
///   The binary <see cref="ISyncManager"/> implementations used by the static methods 
///   in this class do not support reordering or the <c>NextField</c> property, and 
///   they don't store metadata such as field names in the output. However, they do 
///   support deduplication (including cyclic object graphs).
/// <para/>
///   Fields must be read/written in the same order they were read, and with the same 
///   data type (or a "compatible" data type as described below), or (at best) it will 
///   be impossible to read the data stream. At worst, the data stream could seem
///   to be read successfully but produce a garbage result. 
/// <para/>
///   For this reason, you'll need to use versioning code (as explained below) if you
///   can't ensure that the data will be read by the same version of your software that 
///   originally wrote it.
/// <para/>
///   To understand the problem, suppose that in version 1 of your software, you write 
///   a class with an integer field called "SerialNumber":
/// <code>
///     class MyItem
///     {
///     	public int Id { get; set; }
///     	public int SerialNumber { get; set; }
///     }
/// </code>
///   So you write your synchronizer like this:
/// <code><![CDATA[
///     // Note: implementing a generic class with <SM>, and implementing ISyncObject,
///     //       are both optional, but doing so can speed up (de)serialization.
///     class MySync<SM> : ISyncObject<SM, MyItem> where SM : ISyncManager
///     {
///     	public MyItem Sync(SM sm, MyItem? entry)
///     	{
///     		entry ??= new MyItem();
///     		entry.Id = sm.Sync("Id", entry.Id);
///     		entry.SerialNumber = sm.Sync("SerialNumber", entry.SerialNumber);
///     		return entry;
///     	}
///     }
/// ]]></code>
///   And you can serialize an example item like this:
/// <code><![CDATA[
///     var result = SyncBinary.Write(
///         new MyItem { Id = 3, SerialNumber = 65539 },
///         new MySync<SyncBinary.Writer>());
/// ]]></code>
///   The result is these 6 bytes: '(', 3, 0b11000001, 0b00000000, 0b00000011, ')'
/// <para/>
///   The first byte is an object start marker (0x28). The second byte is the Id (0x03).
///   The next three bytes are the SerialNumber (65539). The last byte is an end marker 
///   (0x29).
/// <para/>
///   It's impossible to know just by looking at these bytes that they represent two numbers;
///   in fact, by coincidence, if you serialize an object that contains the single floating-
///   point number <c>3.7837386E-37f</c>, the output is exactly the same 6 bytes! This is why 
///   you must read a binary data stream with the same schema you used to read it (same 
///   fields, same types, same order). If the schema is not exactly the same (and isn't 
///   compatible according to the rules listed below) then reading will fail in most cases, 
///   and in rare cases could produce a garbage answer.
///   
/// <h3>Versioning with SyncBinary</h3>
///
///   Suppose that in version 2 of our software, we change the type of SerialNumber to 
///   string.
/// <code>
///     class MyItem
///     {
///     	public int Id { get; set; }
///     	public string SerialNumber { get; set; }
///     }
/// </code>
///   What happens if we naively read an old-format data stream after this type change? 
///   The outcome depends on the details of the old and new binary formats. If you use
///   default settings, the string reader expects a list-start marker '[' followed by a 
///   length prefix. Since there is no '[' in the data stream, the reader throws an 
///   exception.
/// <para/>
///   To avoid such an outcome, you should use a manual versioning process to make your 
///   synchronizer compatible with <see cref="SyncBinary"/>. The nice thing about manual 
///   versioning is that it not only maintains compatibility with other data formats such 
///   as JSON, but also adds clarification to your JSON output. So your code can be 
///   compatible with <see cref="SyncBinary"/>, <see cref="SyncJson"/> and protocol 
///   buffers all at the same time, while also allowing your data format to evolve over 
///   time.
/// <para/>
///   One useful pattern is to write a format version number in a "top-level" object and
///   then use that version number in child objects. For example, suppose that <c>MyItem</c>
///   objects are <b>always</b> organized in groups:
/// <code><![CDATA[
///     class MyItemGroup
///     {
///         public string GroupName { get; set; }
///         public List<MyItem> Items { get; set; }
///     }
/// ]]></code>
///   Here's a properly versioned synchronizer for MyItemGroup and its child MyItems:
/// <code><![CDATA[
///     class MySync<SM> : ISyncObject<SM, MyItemGroup>, ISyncObject<SM, MyItem> where SM : ISyncManager
///     {
///     	public int Version { get; set; } = 2;
///     
///     	public MyItemGroup Sync(SM sm, MyItemGroup? group)
///     	{
///     		Version = sm.Sync("Version", Version);
///     		if (Version < 1 || Version > 2)
///     			throw new InvalidOperationException("Item group has unrecognized version number");
///     
///     		group ??= new MyItemGroup();
///     		group.GroupName = sm.Sync("GroupName", group.GroupName);
///     		group.Items = sm.SyncList("Items", group.Items, Sync);
///     		return group;
///     	}
///     	
///     	public MyItem Sync(SM sm, MyItem? entry)
///     	{
///     		entry ??= new MyItem();
///     		
///     		entry.Id = sm.Sync("Id", entry.Id);
///     
///     		if (Version <= 1) {
///     			// Version 1 serial numbers are integers. Support reading but not writing.
///     			if (sm.IsWriting)
///     				throw new NotSupportedException();
///     			entry.SerialNumber = sm.Sync("SerialNumber", 0).ToString();
///     		} else {
///     			entry.SerialNumber = sm.Sync("SerialNumber", entry.SerialNumber);
///     		}
///     
///     		return entry;
///     	}
///     }
/// ]]></code>
/// 
///   Here there is no version number in each individual MyItem object. Instead they inherit 
///   the version number from the Version property, which, in Reading mode, is normally read 
///   by the <c>MyItemGroup Sync</c> method. As usual, the <c>MyItemGroup Sync</c> method 
///   passes the <c>MyItem Sync</c> method as the third parameter when it calls
///   <c>sm.SyncList("Items", group.Items, Sync)</c>, so the value of Version is shared
///   between them. Of course, if you deserialize MyItem directly using the <c>MyItem Sync</c>
///   method, you will need to also set the <c>Version</c> property manually (or accept the
///   default Version number).
/// <para/>
///   The above code handles all versions of MyItem in a single function. If you prefer, you
///   could have separate code paths for different versions:
///   
/// <code><![CDATA[
///     class MySync<SM> : ISyncObject<SM, MyItemGroup>, ISyncObject<SM, MyItem> where SM : ISyncManager
///     {
///     	public int Version { get; set; } = 2;
///     
///     	public MyItemGroup Sync(SM sm, MyItemGroup? group)
///     	{
///     		Version = sm.Sync("Version", Version);
///     
///     		group ??= new MyItemGroup();
///     		group.GroupName = sm.Sync("GroupName", group.GroupName);
///     		group.Items = sm.SyncList("Items", group.Items, Sync);
///     		return group;
///     	}
///     
///     	public MyItem Sync(SM sm, MyItem? item)
///     	{
///     		if (Version <= 1) {
///     			// Only support converting from the old format, not to it
///     			if (sm.IsWriting)
///     				throw new NotSupportedException();
///     
///     			var itemV1 = SyncV1(sm, null);
///     
///     			// Convert to new format
///     			return new MyItem {
///     				Id = itemV1.Id,
///     				SerialNumber = itemV1.SerialNumber.ToString(),
///     			};
///     		} else {
///     			return SyncV2(sm, item);
///     		}
///     	}
///     
///     	public MyItem SyncV2(SM sm, MyItem? item)
///     	{
///     		item ??= new MyItem();
///     		item.Id = sm.Sync("Id", item.Id);
///     		item.SerialNumber = sm.Sync("SerialNumber", item.SerialNumber);
///     		return item;
///     	}
///     
///     	public MyItemV1 SyncV1(SM sm, MyItemV1? item)
///     	{
///     		item ??= new MyItemV1();
///     		item.Id = sm.Sync("Id", item.Id);
///     		item.SerialNumber = sm.Sync("SerialNumber", item.SerialNumber);
///     		return item;
///     	}
///     }
///     
///     class MyItemV1
///     {
///     	public int Id { get; set; }
///     	public int SerialNumber { get; set; }
///     }
/// ]]></code>
///   This style of code is longer, but it does have an advantage: the old code for
///   synchronizing V1 items has not been changed (except that MyItem is renamed to 
///   MyItemV1). The idea here is that it is easier to avoid accidentally changing
///   the old schema if you avoid changing the old code.
///   
/// <h3>Data type interchangeability</h3>
/// 
///   The binary format is designed in such a way that certain data types/values are 
///   interchangeable. This means that in some cases it is possible to change certain 
///   data types into certain other data types while retaining backward compatibility 
///   (ability to read old versions of the format using new code that expects a 
///   different type than was originally stored).
///   
///   1. You can upgrade an integer type to a larger integer type with the same 
///      signedness, e.g. `short` to `int`, or `uint` to `ulong`, or even `int` to 
///      `BigInteger`. (This rule does not apply to bitfields, which in general 
///      cannot change size.)
///   2. You can change a signed integer type into an unsigned integer type if the
///      old streams do not contain any negative numbers. Note: DO NOT change unsigned 
///      numbers into signed numbers, because some unsigned numbers would be interpreted 
///      as being negative.
///   3. You can change `bool` to any integer type or nullable integer type.
///   4. You can change `bool?` to any nullable integer type.
///   5. You can change any of `byte sbyte ushort short int uint` to `bool` or `bool?`.
///   6. You can change any of `byte? sbyte? ushort? short? int? uint?` to `bool?`.
///   7. You can change `char` to `ushort` or vice versa.
///   8. You can change a string into a byte array.
///   9. You can change any integer type into its nullable equivalent, e.g. 
///      `int` to `int?`.
///   10. You can change a floating-point or decimal field to its nullable equivalent
///       or vice versa: `float` to `float?`, `double` to `double?`, or `decimal` to 
///       `decimal?`. Note: floating-point formats cannot be enlarged.
///   11. Depending on your <see cref="SyncBinary.Options"/>, it may be safe to 
///       interchange a tuple with the fields it is made of, if you are NOT using the
///       <see cref="ObjectMode.Deduplicate"/> flag (as explained in the section below
///       about the Tuple format).
/// 
///   All other type changes are unsafe and require you to write versioning code.
///   
///   It's important to note, also, that changing the <see cref="ObjectMode"/> of a 
///   field is unsafe unless otherwise indicated. For exmaple, adding or removing the 
///   <see cref="ObjectMode.Deduplicate"/> flag is unsafe unless the object, list or 
///   tuple being written begins with a marker (see <see cref="Options.Markers"/>).
///   
///   Finally, you must not change certain settings of <see cref="SyncBinary.Options"/>
///   between writing a data stream and reading it again. In particular, do not change
///   <see cref="Options.Markers"/>, and don't change <see cref="Options.RootMode"/> in
///   ways that affect the file format.
/// 
/// <h3>Data encodings</h3>
/// 
///   The following encoding schemes for key data types are "set in stone": they won't
///   change between versions of SyncLib.
/// 
/// <h4>Object/array start/end markers</h4>
/// 
///   SyncBinary can optionally output markers at the start/end of an object. Their
///   main purpose is simply to increase the chance that when a data stream is being read
///   incorrectly (because you are not reading exactly the same fields/types that were 
///   written) an exception will occur soon afterward. Markers increase the data size,
///   however.
///   
///   All the markers are single ASCII bytes, e.g. if a marker is '{', that means 123.
///   The start-object marker is '{' when entering an even-numbered Depth, or '(' if odd.
///   The end-object marker is '}' when exiting an even-numbered Depth, or ')' otherwise.
///   The start-list marker is always '['; the end-list marker is always ']'. Please note
///   that strings count as lists of bytes, and will have list markers if they are 
///   enabled.
///   
///   <see cref="SyncBinary.Options.Markers"/> controls which markers are read/written.
///   Markers cannot be auto-detected (for example, if the data stream contains '{' there 
///   is no way to tell whether it's a marker or whether 0x7B is part of a data value).
///   Therefore, when reading a binary data stream, you must use the same marker mode as 
///   when the blob was written, or reading will fail.
///   
///   The default behavior is to write start/end markers for objects and type tags, and
///   to write a start marker <i>but no end marker</i> for lists (including strings);
///   the omitted end marker saves space.
///   
///   A side effect of enabling start markers is that you can safely add or remove the 
///   <see cref="ObjectMode.Deduplicate"/> flag when writing an object that has a marker.
///   By default, objects and lists have start markers but tuples do not.
///   
///   If an object or list reference is null, it is written as a single 255 byte without
///   any markers. The same thing is written when markers are disabled.
/// 
/// <h4>Tuples</h4>
/// 
///   Both lists and normal objects can be written in tuple mode by including the
///   <see cref="ObjectMode.Tuple"/> mode flag. This mode is useful for writing short 
///   fixed-length sequences of values in a compact way.
/// 
///   When writing a list in tuple mode, the list length is omitted from the output in 
///   order to make the output more compact. You must instead specify a <c>tupleLength</c>
///   parameter when you call any of the <c>SyncList</c> extension methods.
///   
///   Start/end markers can be enabled/disabled separately for tuples, and by default, 
///   markers are disabled. If you enable markers, the start/end markers are the same
///   as for lists ('[' and ']'). If markers are disabled, the raw fields of the tuple
///   are written to the output stream without any indication that the current object
///   has changed. 
///   
///   <b>If markers and deduplication are both disabled</b>, it's possible to interchange 
///   a tuple of length N with N fields that have the same types. For example, if you 
///   write a tuple with two fields (an integer and a string), the data stream simply 
///   contains the integer followed by the string. Therefore, it is possible to change a 
///   normal integer and string field into a tuple without breaking backward compatibility 
///   (again, only if markers and deduplication are disabled). The same is true for 
///   ordinary objects, except that markers are enabled by default on ordinary objects.
/// 
/// <h4>Deduplication</h4>
/// 
///   When deduplication is enabled on a particular subobject or sublist, the object 
///   will have a deduplication marker right <i>before</i> the start marker (if any),
///   unless the value written is null. The deduplication marker begins either with 
///   
///   1. '#' (35) to indicate that the object is being seen for the first time and 
///      will follow
///   2. '@' (64) to indicate that the object has been seen before and no data will
///      follow (not even start/end markers).
///   
///   The `#` or `@` is followed by a variable-length integer which is an object ID.
///   Object IDs are normally numbered starting from 1.
///   
///   For example, suppose you write the same string reference twice using 
///   deduplication:
///   
///       syncManager.Sync("1", "Hello", ObjectMode.Deduplicate);
///       syncManager.Sync("2", "Hello", ObjectMode.Deduplicate);
///       
///   Given the default markers, the output bytes would be
///   
///       '#', 0x01, '[', 0x05, 'H', 'e', 'l', 'l', 'o', '@', 0x01
///   
///   Or in other words
///   
///       35, 1, 91, 5, 72, 101, 108, 108, 111, 64, 1
/// 
///   If the start marker is enabled (as it is by default), then it is safe to add or 
///   remove the <see cref="ObjectMode.Deduplicate"/> flag between writing the data 
///   stream and reading it again. This is because <see cref="SyncBinary.Reader"/> 
///   can tell whether the Deduplicate mode was originally present when writing the
///   stream: if it was, the first byte is '#' or '@', otherwise the first byte is '['
///   or 255 (meaning null). If the start marker is disabled, this doesn't work because
///   (for example) it would be ambiguous whether '#' represents a deduplication marker
///   or a list of length 35.
///   
///   For strings in particular, you can change <c>syncManager.Sync(fieldId, stringVal)</c> 
///   to <c>syncManager.Sync(fieldId, stringVal, ObjectMode.Deduplicate)</c> safely
///   if <see cref="Markers.ListStart"/> is enabled (as it is by default).
/// 
/// <h4>Object type tag</h4>
/// 
///   An object type tag is like a normal string field except that it is additionally 
///   prefixed with 'T' if the <see cref="Markers.TypeTag"/> option is enabled (as it 
///   is by default)
/// 
/// <h4>Integers</h4>
///   
///   Integers are normally stored in big-endian format according to the variable-
///   length scheme depicted below. It can be thought of as 9 separate formats: 7 of 
///   the formats are for integers of specific sizes, one is for very large integers,
///   and one is for null. Each 'x' represents a number bit:
///   <code>
///    (1) 0xxxxxxx                                 | 1 byte;   7 number bits
///    (2) 10xxxxxx xxxxxxxx                        | 2 bytes; 14 number bits
///    (3) 110xxxxx xxxxxxxx xxxxxxxx               | 3 bytes; 21 number bits
///    (4) 1110xxxx xxxxxxxx xxxxxxxx xxxxxxxx      | 4 bytes; 28 number bits
///    (5) 11110xxx xxxxxxxx xxxxxxxx xxxxxxxx x... | 5 bytes; 35 number bits
///    (6) 111110xx xxxxxxxx xxxxxxxx xxxxxxxx x... | 6 bytes; 42 number bits
///    (7) 1111110x xxxxxxxx xxxxxxxx xxxxxxxx x... | 7 bytes; 49 number bits
///    (8) 11111110 [length prefix "n"] [bytes "x"] | (1 + length prefix size + n) bytes
///    (9) 11111111                                 | 1 byte for "null"
///   </code>
///   If the number is signed, the sign bit is always the first 'x' in the 
///   depiction. For example, the number -2 is represented as 0b01111110.
/// <para/>
///   If there is a length prefix "n", it is encoded in the same way, so in principle 
///   there is no limit to the number of bits in the number. However, SyncBinary.Reader 
///   and SyncBinary.Writer limit the number size, by default, to 1 MB, large enough 
///   for <c>(BigInteger)1 << (8 * 1024 * 1024)</c>. Also, the length prefix is not 
///   allowed to start with 1111111x (0xFE or 0xFF).
/// <para/>
///   An example of the length-prefixed format is <c>254, 2, 1, 44</c>. In this 
///   example, the length prefix is 2, and the number bits are 0x01 0x2C, which 
///   represents the number 300. However, the normal way of encoding 300 uses only
///   two bytes (0x81, 0x2C).
/// <para/>
///   This set of integer formats offers several advantages:
///   <ul>
///     <li>Since the format is the same regardless of the integer's size in code,
///       you can safely enlarge an integer field, e.g. from `Int32` to `Int64`,
///       without breaking backward compatibility. You can even upgrade fields to 
///       `BigInteger`!</li>
///     <li>Since the format is the same regardless of whether the integer is 
///       nullable or not, you can safely "upgrade" an integer field to be nullable
///       without breaking backward compatibility.</li>
///     <li>Because signed and unsigned numbers are stored in a similar way, you
///       can switch a signed number to be unsigned and retain backward compaibility
///       if (and only if) the field is never negative in any of the old data streams.
///       However, you cannot switch unsigned to signed (see explanation below).
///     <li>A CPU can read numbers in this format faster than it can read numbers
///       in the traditional VLQ or LEB128 formats. For integers under 50 bits, it 
///       requires only one conditional branch to read one number, rather one branch 
///       per byte. Also, generally, fewer machine instructions are required to 
///       reconstruct integers from their serialized form.</li>
///   </ul>
///   Note: although signed and unsigned numbers are stored in a similar way, 
///   switching an unsigned field to signed is unsafe. That's because certain positive 
///   numbers will be stored differently when signed vs unsigned. For example, signed 
///   72 is a pair of bytes (0b10000000 0b01001000), but unsigned 72 is a single byte
///   (0b01001000). If signed 72 is interpreted as unsigned, it's still 72, but if
///   unsigned 72 is interpreted as signed, it becomes -56 because its most significant
///   bit is a 1, which means "negative".
/// <para/>
///   Integers in this format can be stored in a larger size than necessary (this can 
///   be called "non-canonical encoding"). For example, the number -3 can be stored
///   in one byte as 0b01111101 or in two bytes as 0b10111111 0b11111101. 
///   <see cref="SyncBinary.Writer"/> writes integers in the shortest possible form
///   (also known as the "canonical" form).
/// <para/>
///   By the way, while variable-size integers use big-endian format, fixed-size 
///   numbers (e.g. floads) use little-endian format. There is a reason for this. 
///   Little-endian architectures such as x64 are the most common, and on these 
///   architectures, little-endian format is more natural and could lead to 
///   slightly higher performance. However, when writing variable-size integers, 
///   there are fewer than 8 bits in the top byte, so writing it in little-endian 
///   format would "scramble" it. For example, the number 0x12345 is written as 
///   hex <c>C1 23 45</c>, but in little-endian format the same number would be 
///   <c>C5 1A 01</c> which is not only unrecognizable, but potentially could be 
///   slower to read/write depending on the implementation of the reader/writer.
///   
/// <h4>Bitfields</h4>
///   
///   The binary format also supports fixed-size little-endian bitfields via three 
///   special methods of <see cref="ISyncManager"/>: 
///   <see cref="ISyncManager.Sync(FieldId, int, int, bool)"/>,
///   <see cref="ISyncManager.Sync(FieldId, long, int, bool)"/> and 
///   <see cref="ISyncManager.Sync(FieldId, BigInteger, int, bool)"/>.
/// <para/>
///   If you always use a multiple of 8 as the number of bits, <see cref="SyncBinary"/>
///   will store a whole number of bytes to represent each bitfield. For example, you
///   can write 1234 as a 16-bit integer by calling Sync("ignored", 1234, 16, true).
///   The first parameter is ignored. The boolean parameter is ignored when writing 
///   but causes sign-extension when reading. For example, if the input stream has a
///   byte 0b1111_1111, <c>Sync("...", 0, 8, true)</c> would return it as -1 but 
///   <c>Sync("...", 0, 8, false)</c> would return it as 255.
/// <para/>
///   If you use a fractional number of bytes then adjacent bitfields can share a 
///   single byte. For example, if you write a 20-bit bitfield followed by a 4-bit
///   bitfield like so: 
///   <code>
///       sm.Sync("...", 259, 20, true); // 259 = 0b0001_00000011
///       sm.Sync("...",  -1,  4, true); // -1 (as 4 bits) = 0b1111
///   </code>
///   Then these three bytes are written: 0b00000011, 0b00000001, 0b11110000.
/// <para/>
///   If the total number of bits in a series of bitfields is not a multiple of 8, 
///   then the high bits of the final byte are zero, and these high bits are ignored 
///   when the stream is read. For example, if your code says
///   <code>
///       sm.Sync("...", 259, 10, true); // 259 = 0b0001_00000011
///       sm.Sync("...",  -1,  4, true); // -1 (as 4 bits) = 0b1111
///       sm.Sync("...",   7);           // 7 = 0b111
///   </code>
///   Then these three bytes are written: 0b00000011, 0b00111101, 0b00000111. When
///   reading these fields, the top two bits of the second byte are ignored.
///   
/// <h4>Strings</h4>
///   
///   Strings are stored in length-prefixed WTF-8 format (this means UTF-8 format,
///   but highlights that any .NET string can be stored, even if it contains unpaired 
///   surrogate code units). The length is stored in the same format as integers 
///   (above). A length prefix of null represents the string being null. The length 
///   is expressed in bytes, not codepoints. This is essentially the same format as a 
///   byte array, so you could change the type of a string to a byte array while 
///   retaining backward compatibility.
/// <para/>
///   If list markers are enabled, '[' is written before the string and ']' after.
///   However, if the string is null, the markers are omitted (only 0xFF is written).
/// <para/>
///   Surrogate pairs in the .NET representation are converted to single 4-byte 
///   characters in the UTF-8 format.
/// <para/>
///   For example, suppose the string "Hello" is written, followed by a null 
///   reference, followed by a smiley "😀", using the default marker settings 
///   (i.e. list-start markers only). The bytes written will be
///   <code>
///     '[', 5, 'H', 'e', 'l', 'l', 'o', 0xFF, '[', 4, 0xF0, 0x9F, 0x98, 0x80
///   </code>
///   or in hexadecimal:
///   <code>
///     5B 05 48 65 6C 6C 6F FF 5B 04 F0 9F 98 80
///   </code>
///   In .NET, the smiley "😀" is two UTF-16 code units (0xD83D 0xDE00), which is
///   treated as a single code point U+1F600 that is converted to UTF-8 format.
/// <para/>
///   If deduplication is enabled, the output will look approximately like this 
///   (object ID bytes may vary):
///   <code>
///     '#', 1, '[', 5, 'H', 'e', 'l', 'l', 'o', 0xFF, '#', 2, '[', 4, 0xF0, 0x9F, 0x98, 0x80
///   </code>
///   
/// <h4>Characters</h4>
///   
///   An individual <c>char</c> is saved the same way as a <c>ushort</c> (unsigned 
///   16-bit integer). This is a variable-size format between 1 and 3 bytes.
///   For example, the character 'A' is encoded as 0x41, and '•' (U+2022) is encoded 
///   as 0b1010_0000, 0b0010_0010.
///   
/// <h4>Booleans</h4>
///   
///   A boolean is encoded as a signed integer where 0 = false and 1 = true (and as 
///   usual, 255 = null). When reading a boolean from the data stream, it is read as a 
///   32-bit signed integer, which is interpreted as `false` if it is 0 and `true` 
///   otherwise.
///   
///   This means you can safely change an integer field to boolean or vice versa
///   between versions. However, something can go wrong when reading an integer larger
///   than 32 bits as a boolean. For example, if the integer is unsigned 0x8000_0001, 
///   it doesn't fit in a signed 32-bit integer. 
///   
/// <h4>Floating point</h4>
///   
///   Floating point numbers are stored in 32-bit or 64-bit little-endian fixed-size 
///   IEEE 754 format.
///   
///   Nullable floating point numbers are the same size, but use a specific NaN value
///   to represent null: 0xFFF368E0 for 32-bit floats and 0xFFFE6C6C_756E06FE for 64-bit
///   floats. 0xFFF368E0 typically shows up as "àhóÿ" in a hex editor ("ahoy", get it?),
///   while 0xFFFE6C6C_756E06FE looks something like "þ nullþÿ". Why these particular 
///   values?
///   
///   1. They are different from the standard .NET NaN values (0xFFC00000 and 
///      0xFFF8000000000000).
///   2. They are not hard to spot in a hex editor.
///   3. They are unlikely to be the same as other NaN values that users might use, as
///      they are close (but not *too* close) to the "top" of the NaN space.
///   4. If the pattern 0xFFF368E0 is reinterpreted as an integer, it begins with E0 
///      whose top nibble 0xE marks the integer as being four bytes long. So if you
///      interpret floating-point null as an integer, it's still a single number.
///      That number is 0x68F3FF, however, which has no particular meaning. Similarly
///      0xFFFE6C6C_756E06FE starts with FE 06, which marks the integer as 8 bytes long
///      (having a meaningless value of 0x6E756C6CFEFF).
///      
///   Since these NaNs have the high bit set, they are "quiet" NaNs on x64 and ARM.
///   
/// <h4>Decimal</h4>
/// 
///   Decimals are stored in the 128-bit fixed-size format defined by Microsoft for 
///   decimal numbers, but little-endian, so: the 96-bit "integer part" is stored 
///   first (it's analagous to the "mantissa" in floating-point, but is actually an 
///   integer), followed by 16 unused bits that are zero, followed by 8 bits for
///   the negative base-10 exponent (which is limited to a range of 0 to 28) 
///   followed by the most significant byte, which is 0 if the number is positive 
///   and 0x80 if negative.
///   
///   The most unique thing about this format is that zero bits at the end of a number
///   are significant. For example, 7.00m is different from 7m (but equal to it). For 
///   7m, the integer part is 7 and the negative exponent is 0; for 7.00m, the integer 
///   part is 700 and the negative exponent is 2.
///   
///   The null value of `decimal?` is represented by 16 bytes that are all 0xFF. When
///   reading a `decimal?`, the 13th and 14th bytes are checked to see if they are
///   0xFF, and if so, the value is treated as null. If they are neither 0xFF nor 0x00,
///   an exception is thrown.
/// 
/// <h4>Arrays/lists/Memory&lt;T></h4>
///   
///   If list markers are enabled, '[' is written before a list and ']' afterward.
/// <para/>
///   Arrays/lists are length-prefixed, and the length itself uses the standard integer 
///   format laid out above. For example, <c>new[] { 1, 10, 100, 1000 }</c> is encoded 
///   in seven bytes if markers are disabled (<c>4, 1, 10, 0x80, 100, 0b10000011, 0b11101000</c>)
///   and eight bytes with a start marker (<c>'[', 4, 1, 10, 0x80, 100, 0b10000011, 0b11101000</c>).
///   Null is encoded in a single byte (255). An empty array is also a single byte (0).
/// <para/>
///   When writing null, list markers are omitted (only 0xFF is written).
///   
/// </remarks>
public partial class SyncBinary
{
	internal const uint FloatNullBitPattern = 0xFFF368E0u;
	internal const ulong DoubleNullBitPattern = 0xFFFE6C6C_756E06FE;
}
