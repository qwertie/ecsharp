using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Collections;

namespace Loyc.SyncLib;

/// <summary>
///   Convenience methods for a fast pair of <see cref="ISyncManager"/> implementations
///   intended for binary files/streams. The binary format stores no metadata, so it is
///   fast and compact but must be used carefully to avoid data corruption (see remarks).
/// </summary>
/// <remarks>
///   The binary <see cref="ISyncManager"/> implementations (which are used by the 
///   static methods in this class) do not support reordering or the <c>NextField</c>
///   property. They don't support automatic dynamic typing, and they don't store 
///   metadata such as field names in the output. However, they do support deduplication 
///   (including cyclic object graphs).
/// <para/>
///   Fields must be read/written in the same order they were read, and with the same 
///   data type (or a "compatible" data type as described below), or at best it will 
///   not be possible to read the data stream. At worst, the data stream could seem
///   to be read successfully but produce a garbage result.
/// <para/>
///   To understand this, suppose that in version 1 of your software, you write a
///   class with an integer field called "SerialNumber":
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
/// <code>
///     var result = SyncBinary.Write(
///         new MyItem { Id = 3, SerialNumber = 65537 },
///         new MySync<SyncBinary.Writer>());
/// </code>
///   The result is simply be these 4 bytes: 0b00000011, 0b11000001, 0b00000000, 0b00000001
/// <para/>
///   The first byte represents 7 (Id); the other three represent 65537 (SerialNumber).
///   It's impossible to tell just by looking at these bytes that they represent two numbers;
///   in fact, by coincidence, if you serialize the array <c>new byte[] { 193, 0, 1 }</c>
///   the output is exactly the same 4 bytes! This is why you must read a binary data stream 
///   with the same schema you used to read it (same fields, same types, same order). If the
///   schema is not exactly the same (and isn't compatible according to the rules in the 
///   descriptions of the data type formats below) then reading will either fail or (worse)
///   produce a garbage answer.
///   
/// <h3>Versioning with SyncBinary</h3>
///
///   Suppose that in version 2 of our software, we change the type of SerialNumber to string.
/// <code>
///     class MyItem
///     {
///     	public int Id { get; set; }
///     	public string SerialNumber { get; set; }
///     }
/// </code>
///   What happens if we naively read an old-format data stream after this type change? 
///   The outcome depends on the details of the old and new binary formats. In this case, 
///   the reader expects a length-prefixed string. It would see the number 65537 and 
///   interpret it as a string length, but the stream ends at that point (without the 
///   expected 65537 additional bytes) making it seem that the byte stream is too short, 
///   as far as SyncBinary is concerned (of course, the real problem was that the data 
///   type changed).
/// <para/>
///   Does this mean you can't use <see cref="SyncBinary"/> for data types that may change
///   over time? Not at all!
/// <para/>
///   Rather, you should usually implement versioning manually if you want your 
///   synchronizer to be compatible with <see cref="SyncBinary"/>. The nice thing about 
///   manual versioning is that it is still compatible (and useful) in other data formats 
///   such as JSON. You can be compatible with <see cref="SyncBinary"/>, 
///   <see cref="SyncJson"/> and protocol buffers all at the same time, but your JSON output 
///   will have one or more version fields (which is probably a good thing anyway!).
/// <para/>
///   One useful pattern is to write a format version number in a "top-level" object and
///   then use that version number in child objects. For example, suppose that <c>MyItem</c>
///   objects are organized in groups:
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
///     			throw new InvalidOperationException("Invalid version number for item group");
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
///     			entry.SerialNumber = sm.Sync(entry.SerialNumber, (long?)null).ToString();
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
/// <h3>Data encodings</h3>
/// 
///   The following encoding schemes for key data types are "set in stone": they won't
///   change between versions of SyncLib.
/// 
/// <h4>Integers</h4>
///   
///   Integers are normally stored in big-endian format according to the variable-
///   length scheme depicted below. It can be thought of as 9 separate formats: 7 of 
///   the formats are for integers of specific sizes, one is for BigIntegers, and one 
///   is for null. Each 'x' represents a number bit:
///   <code>
///    (1) 0xxxxxxx                                 | 1 byte;   7 number bits
///    (2) 10xxxxxx xxxxxxxx                        | 2 bytes; 14 number bits
///    (3) 110xxxxx xxxxxxxx xxxxxxxx               | 3 bytes; 21 number bits
///    (4) 1110xxxx xxxxxxxx xxxxxxxx xxxxxxxx      | 4 bytes; 28 number bits
///    (5) 11110xxx xxxxxxxx xxxxxxxx xxxxxxxx x... | 5 bytes; 35 number bits
///    (6) 111110xx xxxxxxxx xxxxxxxx xxxxxxxx x... | 6 bytes; 42 number bits
///    (7) 1111110x xxxxxxxx xxxxxxxx xxxxxxxx x... | 7 bytes; 49 number bits
///    (8) 11111110 [length prefix "n"] [bytes "x"] | n+2 bytes; (n-1) * 8 number bits
///    (9) 11111111                                 | 1 byte for "null"
///   </code>
///   If the number is signed, the sign bit is always the first 'x' in the 
///   depiction. For example, the number -2 is represented as 0b01111110.
/// <para/>
///   This format offers several advantages:
///   <ul>
///     <li>Since the format is the same regardless of the .NET integer type,
///       you can safely enlarge an integer field, e.g. from `Int32` to `Int64`,
///       without breaking backward compatibility. You can even upgrade fields to 
///       `BigInteger`!</li>
///     <li>Since the format is the same regardless of whether the integer is 
///       nullable or not, you can safely "upgrade" an integer field to be nullable
///       without breaking backward compatibility.</li>
///     <li>Because signed and unsigned numbers are stored in a similar way, you
///       can switch a signed number to be unsigned and retain backward compaibility
///       if (and only if) the field is never negative in any of the old data streams.
///       However, you can't switch unsigned to signed (see explanation below).
///     <li>A CPU can read numbers in this format faster than it can read numbers
///       in the traditional VLQ or LEB128 formats. For integers under 50 bits, it 
///       requires only one conditional branch to read one number, rather one branch 
///       per byte. Also, generally, less bit-fiddling is required to reconstruct 
///       integers from their binary form.</li>
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
///   <see cref="SyncBinary.Writer"/> writes integers in the shortest ("canonical")
///   possible form.
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
///   single byte. For example, if you write a 12-bit bitfield followed by a 4-bit
///   bitfield using 
///   <code>
///       sm.Sync("...", 257, 12, true); // 257 = 0b0001_00000001
///       sm.Sync("...",  -1,  4, true);
///   </code>
///   Then two bytes are written: 0b00000001, 0b11110001.
/// <para/>
///   If the total number of bits in a series of bitfields is not a multiple of 8, 
///   then the high bits of the final byte are zero, and these high bits are ignored 
///   when the stream is read. For example, if your code says:
///   <code>
///       sm.Sync("...", 257, 10, true); // 257 = 0b0001_00000001
///       sm.Sync("...",  -1,  4, true);
///       sm.Sync("...",   7);
///   </code>
///   It produces the following output: 0b00000001, 0b00111101, 0b00000111.
///   
/// <h4>Strings</h4>
///   
///   Strings are stored in length-prefixed WTF-8 format, which is the same as UTF-8
///   except that any .NET string can be stored, even if it contains unpaired 
///   surrogate code units. The length is stored in the same format as integers 
///   (above). A length prefix of null represents the string being null. The length 
///   is expressed in bytes, not codepoints. This is essentially the same format as a 
///   byte array, so you can change the type of a string to a byte array while 
///   retaining backward compatibility.
/// <para/>
///   Surrogate pairs in the .NET representation are converted to single 4-byte 
///   characters in the UTF-8 format.
///   
/// <h4>Characters</h4>
///   
///   An individual <c>char</c> is saved the same way as a <c>ushort</c> (unsigned 
///   16-bit integer). This is a variable-size format between 1 and 3 bytes.
///   
/// <h4>Booleans</h4>
///   
///   A boolean is encoded as an integer where 0 = false and 1 = true (and thus 
///   255 = null). When reading a boolean from the data stream, it is read as a 32-bit 
///   integer, which is interpreted as `false` if it is 0 and `true` otherwise.
///   Of course, this means you can safely change an integer field to boolean or vice 
///   versa.
///   
/// <h4>Floating point</h4>
///   
///   Floating point numbers are stored in 32-bit or 64-bit little-endian fixed-size 
///   IEEE 754 format.
///   
/// <h4>Decimal</h4>
/// 
///   Decimals are stored in the 80-bit little-endian fixed-size format defined by 
///   Microsoft for decimal numbers.
/// 
/// <h4>Arrays/lists</h4>
///   
///   Arrays/lists are length-prefixed, and the length itself uses the standard integer 
///   format laid out above. For example, <c>new[] { 1, 10, 100, 1000 }</c> is encoded 
///   in six bytes as <c>4, 1, 10, 100, 0b10000011, 0b11101000</c>. Null is encoded in a 
///   single byte (255). An empty array is also a single byte (0).
///   
/// </remarks>
partial class SyncBinary
{
	public static ReadOnlyMemory<byte> Write<T>(T value, SyncObjectFunc<Writer, T> sync, Options? options = null)
	{
		throw new NotImplementedException();
	}
	public static ReadOnlyMemory<byte> WriteI<T>(T value, SyncObjectFunc<ISyncManager, T> sync, Options? options = null)
	{
		throw new NotImplementedException();
	}
	public static ReadOnlyMemory<byte> Write<T, SyncObject>(T value, SyncObject sync, Options? options = null)
		where SyncObject : ISyncObject<SyncBinary.Writer, T>
	{
		throw new NotImplementedException();
	}
}
