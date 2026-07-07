using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib;

partial class SyncProtobuf
{
	internal static Options _defaultOptions = new Options();

	/// <summary>
	///   Options that control the behavior of <see cref="SyncProtobuf.Reader"/> and
	///   <see cref="SyncProtobuf.Writer"/>.
	/// </summary>
	/// <remarks>
	///   <see cref="Reader"/> and <see cref="Writer"/> do not copy this object, so changing
	///   its properties affects any reader/writer that was constructed with it.
	/// </remarks>
	public class Options
	{
		/// <summary>Maximum size, in bytes, of a length-delimited payload (string, byte
		///   array, sub-message, list, etc.) that the reader will accept. This guards
		///   against corrupt or malicious inputs that claim an enormous length. The
		///   default is 64 MB.</summary>
		public int MaxPayloadSize { get; set; } = 64 * 1024 * 1024;

		/// <summary>The <see cref="ObjectMode"/> used to read/write the root object.
		///   This option has no effect if you use <see cref="NewWriter"/> or
		///   <see cref="NewReader(ReadOnlyMemory{byte}, Options?)"/> directly.</summary>
		public ObjectMode RootMode { get; set; } = ObjectMode.Normal;

		#region Writer-specific options

		public ForWriter Write { get; set; } = new ForWriter();

		public class ForWriter
		{
			/// <summary>Initial size of the output buffer, in bytes (default: 512). Ignored
			///   if you provide your own <see cref="System.Buffers.IBufferWriter{T}"/> to
			///   <see cref="NewWriter"/> (the writer still uses an internal contiguous
			///   buffer for length back-patching, but this controls its initial size).</summary>
			public int InitialBufferSize { get; set; } = 512;
		}

		#endregion

		#region Reader-specific options

		public ForReader Read { get; set; } = new ForReader();

		public class ForReader
		{
			/// <summary>If true, a null in the data stream that is read as a non-nullable
			///   primitive returns the type's default value instead of throwing
			///   <see cref="FormatException"/>. Because absent fields are already treated as
			///   null/default, this rarely matters.</summary>
			public bool ReadNullPrimitivesAsDefault { get; set; } = true;

			/// <summary>When true (the default) and the root object has been read
			///   successfully, the reader verifies that the data stream has ended, throwing
			///   <see cref="FormatException"/> if trailing bytes remain.</summary>
			public bool VerifyEof { get; set; } = true;
		}

		#endregion
	}
}
