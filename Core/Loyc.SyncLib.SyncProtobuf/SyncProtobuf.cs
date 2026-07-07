using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib;

/// <summary>
///   Convenience methods and shared definitions for a pair of <see cref="ISyncManager"/>
///   implementations (<see cref="Reader"/> and <see cref="Writer"/>) that read and write
///   the <a href="https://protobuf.dev/programming-guides/encoding/">Protocol Buffers</a>
///   wire format. Call <see cref="Write{T}(T, SyncObjectFunc{Writer, T}, Options?)"/> or
///   <see cref="Read{T}(ReadOnlyMemory{byte}, SyncObjectFunc{Reader, T}, Options?)"/> to
///   (de)serialize an object, <see cref="NewWriter"/> / <see cref="NewReader(ReadOnlyMemory{byte}, Options?)"/>
///   to obtain a low-level (de)serializer, or <see cref="WriteSchema{T}(SyncObjectFunc{Schema, T}, Options?)"/>
///   to generate a <c>.proto</c> schema describing the output.
/// </summary>
/// <remarks>
///   The output is standard, valid Protocol Buffers: any Protobuf implementation (such
///   as protoc-generated code or protobuf-net) can parse it using the proto3 schema
///   produced by <see cref="SyncProtobuf.Schema"/>, and <see cref="Reader"/> can parse
///   messages produced by other Protobuf implementations from the same schema.
///   <para/>
///   Unlike <see cref="SyncBinary"/>, this format identifies every field by an integer
///   ID. Therefore <see cref="Reader"/> and <see cref="Writer"/> report
///   <see cref="ISyncManager.NeedsIntegerIds"/> = true and
///   <see cref="ISyncManager.SupportsReordering"/> = true: fields may be read in any
///   order, and unknown fields are skipped.
///
/// <h3>Field numbers</h3>
///
///   Each call to a <c>Sync</c> method carries a <see cref="FieldId"/>. If the FieldId
///   specifies an integer ID (i.e. <c>FieldId.Id != int.MinValue</c>, as produced by the
///   <c>(name, id)</c> tuple conversion or by a private <see cref="Symbol"/> pool), that
///   ID becomes the Protobuf field number. Otherwise the field number is auto-assigned as
///   <c>N + 1</c>, where <c>N</c> is the last field number used in the current message
///   (starting from 0). The auto-numbering advances for every field synchronized (whether
///   or not a value is physically written), so the reader and writer stay in agreement as
///   long as they synchronize the same fields in the same order. Valid field numbers are
///   1 to 536,870,909; the range 19000-19999 (reserved by Protobuf) and the two highest
///   numbers (reserved by SyncProtobuf, see below) are rejected.
///
/// <h3>Scalar wire format</h3>
///
///   Every field is preceded by a <i>tag</i>: a varint equal to
///   <c>(fieldNumber &lt;&lt; 3) | wireType</c>, using the standard Protobuf wire types:
///   <ul>
///   <li><b>VARINT (0)</b> — <c>bool</c>, <c>char</c> and all integer types. Signed
///       integers are stored as their 64-bit two's-complement bit pattern (so negative
///       numbers occupy 10 bytes, exactly like Protobuf <c>int32</c>/<c>int64</c>).</li>
///   <li><b>I64 (1)</b> — <c>double</c> (8 bytes, little-endian IEEE 754).</li>
///   <li><b>I32 (5)</b> — <c>float</c> (4 bytes, little-endian IEEE 754).</li>
///   <li><b>LEN (2)</b> — length-delimited payloads: <c>string</c> (UTF-8), <c>byte[]</c>
///       (Protobuf <c>bytes</c>), <c>decimal</c> (16 bytes: the little-endian layout of
///       <see cref="decimal.GetBits(decimal)"/>), <see cref="System.Numerics.BigInteger"/>
///       (little-endian two's complement, the format of <c>BigInteger.ToByteArray()</c>),
///       sub-messages, and the list/tuple/deduplication containers described below.</li>
///   </ul>
///
///   <b>Null and absent fields.</b> A field whose value is null (a null nullable scalar,
///   string, byte array, list or sub-object) is simply omitted, and the reader returns
///   null when a requested nullable field is absent — matching Protobuf's "absent means
///   default" convention. Reading an absent field as a non-nullable primitive returns the
///   type's default value, exactly like Protobuf. To preserve round-trip fidelity,
///   non-null values are always written, even zero (in schema terms, every scalar field
///   is <c>optional</c>, i.e. it has explicit presence).
///
/// <h3>The root object</h3>
///
///   The root object is written as a bare message body — no envelope — just like a
///   message serialized by any other Protobuf library. Because of this, the root must be
///   an object (not a list or tuple), a null root is encoded as zero bytes, and a
///   non-null root that happens to contain no fields is marked with a reserved boolean
///   field (number 536,870,910, called <c>_present</c> in generated schemas) so that it
///   remains distinguishable from null.
///
/// <h3>Sub-objects, lists and tuples</h3>
///
///   A sub-object is a nested message: a LEN field containing the concatenation of its
///   (tag, value) fields, which can be read in any order.
///   <para/>
///   A <b>tuple</b> is also a nested message; its elements are stored as fields
///   auto-numbered 1, 2, 3, ... (a null element is an omitted field, which stays
///   position-safe because element numbering advances regardless).
///   <para/>
///   A <b>list</b> field is a nested <i>list container</i> message in which all elements
///   are stored in field 1 (in generated schemas this container is a message type like
///   <c>Int32List { repeated int32 items = 1; }</c>). This one level of nesting is what
///   allows SyncLib to distinguish a null list (field omitted) from an empty one (empty
///   container), and to nest lists inside lists. Within the container:
///   <ul>
///   <li>Non-nullable scalar elements are stored <i>packed</i>: field 1 is one
///       length-delimited block of concatenated scalar values, exactly like Protobuf's
///       packed repeated encoding.</li>
///   <li>Non-nullable length-delimited elements (<c>decimal</c>, BigInteger, sub-objects
///       synchronized with <see cref="ObjectMode.NotNull"/>) are stored as one field-1
///       entry per element, like an ordinary repeated field.</li>
///   <li>Nullable elements (strings, nullable scalars, nullable sub-objects, nested
///       lists) are each wrapped in a single-field message <c>{ optional T value = 1; }</c>
///       — the same idea as Protobuf's well-known wrapper types — where an empty wrapper
///       represents a null element. This keeps null distinguishable from default values
///       such as the empty string.</li>
///   </ul>
///   <c>byte[]</c> and other byte lists are not stored as list containers at all; they
///   are written as a single Protobuf <c>bytes</c> value.
///
/// <h3>Deduplication and cyclic graphs</h3>
///
///   A field or element synchronized with <see cref="ObjectMode.Deduplicate"/> is stored
///   as a <i>reference wrapper</i> message <c>{ uint64 id = 1; T value = 2; }</c>: the
///   first occurrence of an object stores both its ID and its value, and each later
///   occurrence stores only the ID. This lets SyncProtobuf serialize shared references
///   and cyclic object graphs (see the Jack-and-Jill example in <see cref="ISyncManager"/>)
///   while remaining valid Protobuf. Deduplication also works for strings and byte
///   arrays.
///   <para/>
///   <b>Caution:</b> because the wrapper is only present when Deduplicate is requested,
///   the writer and reader must agree on which fields use Deduplicate. Unlike
///   <see cref="SyncBinary"/> (whose optional markers make the flag self-describing),
///   toggling <see cref="ObjectMode.Deduplicate"/> is a breaking change to the data
///   stream.
///
/// <h3>Type tags</h3>
///
///   <see cref="ISyncManager.SyncTypeTag(string?)"/> stores the tag as a string field
///   with the reserved number 536,870,911 (<c>_type</c> in generated schemas). Protobuf
///   parsers that don't know about it simply skip it.
/// </remarks>
public partial class SyncProtobuf
{
	/// <summary>Protobuf wire types.</summary>
	internal enum WireType : byte
	{
		Varint = 0,   // int32, int64, uint32, uint64, bool, char, enum
		I64 = 1,      // fixed64, sfixed64, double
		Len = 2,      // string, bytes, embedded messages, packed repeated, lists
		StartGroup = 3, // (deprecated in Protobuf; unused here)
		EndGroup = 4,   // (deprecated in Protobuf; unused here)
		I32 = 5,      // fixed32, sfixed32, float
	}

	/// <summary>The largest field number permitted by Protocol Buffers (2^29 - 1).</summary>
	internal const int MaxFieldNumber = 536_870_911;

	/// <summary>The largest field number available to user fields. The two numbers above
	///   it are reserved for <see cref="TypeTagFieldNumber"/> and
	///   <see cref="PresentFieldNumber"/>.</summary>
	internal const int MaxUserFieldNumber = MaxFieldNumber - 2;

	/// <summary>The reserved field number used to store an object's type tag (see
	///   <see cref="ISyncManager.SyncTypeTag(string?)"/>) as a string field. This is the
	///   maximum legal Protobuf field number; generated schemas declare it as
	///   <c>optional string _type</c>.</summary>
	internal const int TypeTagFieldNumber = MaxFieldNumber;

	/// <summary>The reserved field number of the boolean marker written when a non-null
	///   root object would otherwise serialize to zero bytes (which is the encoding of a
	///   null root). Generated schemas declare it as <c>optional bool _present</c>.</summary>
	internal const int PresentFieldNumber = MaxFieldNumber - 1;
}
