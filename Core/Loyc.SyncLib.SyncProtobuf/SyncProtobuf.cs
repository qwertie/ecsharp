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
///   (de)serialize an object, or <see cref="NewWriter"/> / <see cref="NewReader(ReadOnlyMemory{byte}, Options?)"/>
///   to obtain a low-level (de)serializer.
/// </summary>
/// <remarks>
///   Unlike <see cref="SyncBinary"/>, this format identifies every field by an integer
///   ID, exactly like Protocol Buffers. Therefore <see cref="Reader"/> and
///   <see cref="Writer"/> report <see cref="ISyncManager.NeedsIntegerIds"/> = true and
///   <see cref="ISyncManager.SupportsReordering"/> = true: fields may be read in any
///   order, and unknown fields are skipped.
///
/// <h3>Field IDs</h3>
///
///   Each call to a <c>Sync</c> method carries a <see cref="FieldId"/>. If the FieldId
///   specifies an integer ID (i.e. <c>FieldId.Id != int.MinValue</c>, as produced by the
///   <c>(name, id)</c> tuple conversion or by a private <see cref="Symbol"/> pool), that
///   ID becomes the Protobuf field number. Otherwise the field number is auto-assigned as
///   <c>N + 1</c>, where <c>N</c> is the last field number used in the current object
///   (starting from 0). This matches the convention documented for
///   <see cref="SyncBinary.FieldIdMode.Integers"/>. The auto-numbering advances for every
///   field synchronized (whether or not a value is physically written), so the reader and
///   writer stay in agreement as long as they synchronize the same fields in the same
///   order.
///
/// <h3>Wire format</h3>
///
///   Every field is preceded by a <i>tag</i>: an unsigned LEB128 varint equal to
///   <c>(fieldNumber &lt;&lt; 3) | wireType</c>. The wire types used are the standard
///   Protobuf ones:
///   <ul>
///   <li><b>VARINT (0)</b> — <c>bool</c>, <c>char</c> and all integer types. Signed
///       integers are stored as their 64-bit two's-complement bit pattern (so negative
///       numbers occupy 10 bytes, exactly like Protobuf <c>int32</c>/<c>int64</c>).</li>
///   <li><b>I64 (1)</b> — <c>double</c> (8 bytes, little-endian IEEE 754).</li>
///   <li><b>LEN (2)</b> — length-delimited payloads: <c>string</c> (UTF-8), byte arrays,
///       <c>decimal</c> (16 bytes), <c>BigInteger</c> (two's-complement, big-endian),
///       sub-messages, lists and tuples.</li>
///   <li><b>I32 (5)</b> — <c>float</c> (4 bytes, little-endian IEEE 754).</li>
///   </ul>
///
///   <b>Null and absent fields.</b> A field whose value is null (a null nullable scalar,
///   string, byte array or sub-object) is simply omitted, and the reader returns null
///   when a requested field is absent — matching Protobuf's "absent means default"
///   convention. Because presence is encoded structurally, no special null bit-patterns
///   are needed (contrast <see cref="SyncBinary"/>, which reserves NaN and 0xFF values).
///   To preserve round-trip fidelity, non-null scalars are always written, even zero.
///
/// <h3>Sub-messages, lists and tuples</h3>
///
///   A sub-object, list or tuple is written as a single LEN field: the tag is followed by
///   a varint byte-length and then the payload. For a normal object the payload is the
///   concatenation of its (tag, value) fields; the reader indexes them by field number so
///   they can be read in any order. For a list or tuple the payload is the concatenation
///   of its elements, written positionally with no tags:
///   <ul>
///   <li>A scalar element is written raw (VARINT / I32 / I64), which is self-delimiting.</li>
///   <li>A length-delimited element (string, BigInteger, sub-object, nested list) is
///       prefixed by a varint <c>length + 1</c>, where a stored 0 denotes a null element.</li>
///   </ul>
///   The reader knows the payload's byte range from the outer length prefix, so it detects
///   the end of a list when the read cursor reaches that boundary
///   (<see cref="ISyncManager.ReachedEndOfList"/>). This is a slight divergence from
///   idiomatic Protobuf, where a repeated message field re-emits its tag per element; here
///   a list is a self-contained length-delimited container, which lets SyncLib represent
///   nested lists, null elements and heterogeneous tuples uniformly. Lists of packed
///   scalars are byte-compatible with Protobuf's packed-repeated encoding.
///
/// <h3>Deduplication and cyclic graphs</h3>
///
///   When a field or element is synchronized with <see cref="ObjectMode.Deduplicate"/>,
///   the LEN payload begins with a one-byte marker (0 = first occurrence, 1 =
///   back-reference) followed by a varint object ID. A first occurrence is followed by the
///   object body; a back-reference is not. This lets <see cref="SyncProtobuf"/> serialize
///   shared references and cyclic object graphs (see the Jack-and-Jill example in
///   <see cref="ISyncManager"/>). These markers are a SyncLib extension and are only
///   present when deduplication is requested.
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

	/// <summary>The field number used to store an object's type tag (see
	///   <see cref="ISyncManager.SyncTypeTag(string?)"/>). Protobuf reserves field numbers
	///   19000-19999, so this value never collides with a user field number.</summary>
	internal const int TypeTagFieldNumber = 19000;

	/// <summary>Object framing marker (first byte of every sub-object/list/tuple LEN
	///   payload): the object is not deduplicated; its body follows immediately. Making
	///   this marker always present lets the reader detect deduplication from the data
	///   itself, so the <see cref="ObjectMode.Deduplicate"/> flag may be toggled between
	///   writing and reading (as with <see cref="SyncBinary"/>).</summary>
	internal const byte DedupNone = 0;
	/// <summary>Object framing marker: first occurrence of a deduplicated object; a varint
	///   object ID and then the body follow.</summary>
	internal const byte DedupFirst = 1;
	/// <summary>Object framing marker: a back-reference to a previously written object;
	///   only a varint object ID follows and no body is present.</summary>
	internal const byte DedupBackRef = 2;
}
