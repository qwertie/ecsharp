using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.SyncLib;

partial class SyncBinary
{
	static Options _defaultOptions = new Options();

	// TODO: remove this. Less flexibility will yield higher performance!
	public enum IntFormat
	{
		/// <summary>Specifies that integers be stored little-endian (meaning least 
		/// significant byte first) with a fixed size (e.g. 4 bytes for Int32).
		/// This is the default.</summary>
		LittleEndian = 0,
		/// <summary>Specifies that integers be stored big-endian (meaning most 
		/// significant byte first) with a fixed size (e.g. 4 bytes for Int32).</summary>
		BigEndian = 1,
		/// <summary>A flag that specifies signed integers should use zigzag 
		/// encoding, in which the low bit serves as a sign bit rather than 
		/// the high bit: 0 means 0, 1 means -1, 2 means 1, 3 means -2, 4 means 2,
		/// etc. When zigzag encoding is disabled, signed integers are stored in
		/// two's complement notation, even when using a variable-size encoding.
		/// </summary>
		ZigZag = 2,
		/// <summary>Specifies that integers be stored in LEB128 (little-endian 
		/// base 128) format. This format is a sequence of bytes in which the final 
		/// byte is less than 128, and all other bytes are at least 128. In other words,
		/// the high bit acts as a "continuator" signal indicating that there are 
		/// more bytes. The 7 low bits of each byte are bits of the number itself,
		/// with the low bits stored first.</summary>
		LEB128 = 4,
		/// <summary>Combines LEB128 with ZigZag encoding.</summary>
		LEB128ZigZag = LEB128 | ZigZag,
		/// <summary>Specifies that integers be stored in a generic format designed 
		/// for SyncLib. The format is a hybrid between PrefixVarInt designed for both small numbers and 
		/// extremely large ones. All formats are little-endian.</summary>
		/// <remarks>
		/// <code>
		///              [64 bits]
		/// (1) 0xxxxxxx                                 | 1 byte;   7 number bits
		/// (2) 10xxxxxx xxxxxxxx                        | 2 bytes; 14 number bits
		/// (3) 110xxxxx xxxxxxxx xxxxxxxx               | 3 bytes; 21 number bits
		/// (4) 1110xxxx xxxxxxxx xxxxxxxx xxxxxxxx      | 4 bytes; 28 number bits
		/// (5) 11110xxx xxxxxxxx xxxxxxxx xxxxxxxx x... | 5 bytes; 35 number bits
		/// (6) 111110xx xxxxxxxx xxxxxxxx xxxxxxxx x... | 6 bytes; 42 number bits
		/// (7) 1111110x xxxxxxxx xxxxxxxx xxxxxxxx x... | 7 bytes; 49 number bits
		/// (8) 11111110 [length prefix "n"] [bytes "x"] | n+2 bytes; (n-1) * 8 number bits
		/// (9) 11111111                                 | 1 byte for "null"
		/// </code>
		/// <code>
		///              [64 bits]
		/// (1) xxxxxxx1                                 | 1 byte;   7 number bits
		/// (2) xxxxxx10 xxxxxxxx                        | 2 bytes; 14 number bits
		/// (3) xxxxx100 xxxxxxxx xxxxxxxx               | 3 bytes; 21 number bits
		/// (4) xxxx1000 xxxxxxxx xxxxxxxx xxxxxxxx      | 4 bytes; 28 number bits
		/// (5) xxx10000 xxxxxxxx xxxxxxxx xxxxxxxx x... | 5 bytes; 35 number bits
		/// (6) xx100000 xxxxxxxx xxxxxxxx xxxxxxxx x... | 6 bytes; 42 number bits
		/// (7) x1000000 xxxxxxxx xxxxxxxx xxxxxxxx x... | 7 bytes; 49 number bits
		/// (8) 10000000 [length prefix "n"] [bytes "x"] | n+2 bytes; (n-1) * 8 number bits
		/// (9) 00000000                                 | 1 byte for "null"
		/// </code>
		/// <code>
		/// 
		/// (1) 11111111 => null
		/// (2) 11111110 => length-prefixed
		/// (3) 1xxxxxxx xxxxxxxx => 14-bit number (little endian) = 224 + 
		/// (4) xxxxxxxx => number below 224
		/// byte0 < 192 => byte 0
		/// byte0 < 
		/// 
		/// 
		///              [64 bits]
		/// (1) 0xxxxxxx                                 | 1 byte;   7 number bits
		/// (2) 10xxxxxx                                 | 1 byte;   7 number bits
		/// (2) xxxxxx10 xxxxxxxx                        | 2 bytes; 14 number bits
		/// (3) xxxxx100 xxxxxxxx xxxxxxxx               | 3 bytes; 21 number bits
		/// (4) xxxx1000 xxxxxxxx xxxxxxxx xxxxxxxx      | 4 bytes; 28 number bits
		/// (5) xxx10000 xxxxxxxx xxxxxxxx xxxxxxxx x... | 5 bytes; 35 number bits
		/// (6) xx100000 xxxxxxxx xxxxxxxx xxxxxxxx x... | 6 bytes; 42 number bits
		/// (7) x1000000 xxxxxxxx xxxxxxxx xxxxxxxx x... | 7 bytes; 49 number bits
		/// (8) 10000000 [length prefix "n"] [bytes "x"] | n+2 bytes; (n-1) * 8 number bits
		/// (9) 00000000                                 | 1 byte for "null"
		/// </code>
		/// </remarks>
		SyncLib = 8,
		/// <summary>Combines SyncLib encoding with ZigZag encoding.</summary>
		SyncLibZigZag = SyncLib | ZigZag,
	}
	/*class thinking
	{
		static ulong? ReadLong(Span<byte> stream)
		{
			if (stream.Length >= 8) {
				byte s0 = (byte)stream[0];
				if (s0 <= 0b0111_1111)
					return s0;
				if (s0 < 0b1111_1110) {
					int lz = G.LeadingZeroCount(s0 ^ 0xFFu);
					if (stream.Length >= 8) {
						ulong @long = BinaryPrimitives.ReadUInt64LittleEndian(stream);
						int size = lz - 23;
						int initialBits = 31 - lz;
						ulong result0 = s0 & ((1u << initialBits) - 1u);

						int bitsRemaining = (size - 1) << 3;

						return result0 | (((@long >> 8) & ((1uL << bitsRemaining) - 1)) << initialBits);
					}
				} else if (s0 == 0b1111_1111) {
					return null;
				} else {
					Debug.Assert(stream[0] == 0xFE);

				}
			} else {
				throw new NotImplementedException("TODO");
			}
		}
	}*/

	/// <summary>
	///   Options that control general behavior of <see cref="SyncBinary.Reader"/> and 
	///   <see cref="SyncBinary.Writer"/>. Note: some behaviors such as deduplication 
	///   (including support for cyclic references) are controlled at the level of
	///   individual fields, and such options are not duplicated here.
	/// </summary>
	/// <remarks>
	///   <see cref="Reader"/> and <see cref="Writer"/> do not make a copy of this
	///   object before using it, and it is possible to change these options after 
	///   constructing the reader/writer and have those changes take effect 
	///   somewhere in the middle of a file stream. In fact, changing the options
	///   mid-file is sometimes required to support (for example) file formats that 
	///   use multiple integer formats. However, if you change these options while 
	///   writing an object, remember to change them back to their previous state 
	///   before returning to the parent object. If you fail to restore the previous 
	///   state expected by the parent object, you may cause the parent object to 
	///   corrupt the output stream (when writing) or misinterpret the stream and 
	///   receive corrupted data (when reading).
	/// </remarks>
	public class Options
	{
		/// <summary>Maximum size of one number, in bytes. The default is 1 MB, i.e. a 
		///   maximum value of roughly <c>(BigInteger)int.MaxValue << (8 * 1024 * 1024)</c>.
		///   An exception occurs if you try to serialize/deserialize a number
		///   larger than this.</summary>
		public int MaxNumberSize { get; set; } = 1024 * 1024 + 4;

		/// <summary>Controls the set of markers that are written or expected in
		///   the binary data stream. The purpose of markers is simply to increase 
		///   the chance that when a data stream is being read incorrectly (because 
		///   you are not reading exactly the same fields/types that were written) 
		///   an exception will occur soon afterward. Markers increase the data 
		///   size, however.</summary>
		public Markers Markers { get; set; } = Markers.Default;

		/// <summary>The <see cref="ObjectMode"/> used to read/write the root object.
		///   This option has no effect if you are using <see cref="NewWriter"/> or 
		///   <see cref="NewReader"/>.</summary>
		public ObjectMode RootMode { get; set; } = ObjectMode.Normal;

		#region Writer-specific options

		public ForWriter Write { get; set; } = new ForWriter();

		public class ForWriter
		{
			/// <summary>Initial size of the output buffer when writing JSON (default: 1024).
			/// This property is ignored if you provide your own buffer to <see cref="SyncJson.NewWriter"/></summary>
			public int InitialBufferSize { get; set; } = 512;
		}

		#endregion

		#region Reader-specific options

		// TODO: reconsider how custom type conversions work before initial release
		public ForReader Read { get; set; } = new ForReader();

		public class ForReader
		{
			/// <summary>If this is true, numbers in the data stream that are too 
			///   large to fit in the requested type are silently truncated. If this
			///   is false, such large numbers cause <see cref="Reader"/> to throw
			///   <see cref="OverflowException"/>.</summary>
			/// <remarks>For example, 33000 is too large for Int16, and if this property
			///   is true it will be "truncated" to -32536.</remarks>
			public bool SilentlyTruncateLargeNumbers { get; set; } = false;

			/// <summary>This property requests that if a property is set to null but read as 
			///   a primitive type, the default value of that type should be returned instead
			///   of throwing an exception. For example, if <see cref="Reader.Sync(FieldId, int)"/>
			///   encounters a null, it will return 0 instead if throwing an exception if this
			///   property is true.</summary>
			/// <seealso cref="ObjectMode.ReadNullAsDefault"/>
			public bool ReadNullPrimitivesAsDefault { get; set; } = false;

			/// <summary>When this property is true and the root object has been read successfully,
			///   the reader checks whether there is additional non-whitespace text beyond the end 
			///   of what was read, and throws an exception if extra junk is encountered.</summary>
			public bool VerifyEof { get; set; } = true;

		}

		#endregion
	}

	public enum Markers
	{
		None = 0,
		ObjectStart = 1,
		ObjectEnd = 2,
		Objects = ObjectStart | ObjectEnd,
		ListStart = 4,
		ListEnd = 8,
		Lists = ListStart | ListEnd,
		TupleStart = 16,
		TupleEnd = 32,
		Tuples = TupleStart | TupleEnd,
		TypeTag = 64,
		Default = Objects | ListStart | TypeTag,
		All = 127,
	}
}