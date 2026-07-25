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
	///   <para/>
	///   Note: reader behaviors that are options in other formats are fixed here by
	///   Protobuf semantics: an absent field read as a non-nullable primitive returns the
	///   type's default value, integers too large for the requested type are truncated to
	///   its low bits (as Protobuf parsers do), and the root message always occupies the
	///   entire input.
	/// </remarks>
	public sealed class Options : ISyncOptions
	{
		/// <summary>Maximum size, in bytes, of a length-delimited payload (string, byte
		///   array, sub-message, list, etc.) that the reader will accept. This guards
		///   against corrupt or malicious inputs that claim an enormous length. The
		///   default is 64 MB.</summary>
		public int MaxPayloadSize { get; set; } = 64 * 1024 * 1024;

		/// <summary>The <see cref="ObjectMode"/> used to read/write the root object.
		///   This option has no effect if you use <see cref="NewWriter"/> or
		///   <see cref="NewReader(ReadOnlyMemory{byte}, Options?)"/> directly.</summary>
		/// <remarks>The same mode must be used when writing and when reading; in
		///   particular, toggling <see cref="ObjectMode.Deduplicate"/> changes the wire
		///   format of the root object (see <see cref="SyncProtobuf"/>).</remarks>
		public ObjectMode RootMode { get; set; } = ObjectMode.Normal;

		#region Writer-specific options

		public ForWriter Write { get; set; } = new ForWriter();

		public class ForWriter
		{
			/// <summary>Initial size, in bytes, of the writer's internal contiguous
			///   buffer, and of the output buffer that <see cref="SyncProtobuf.Write{T}(T, SyncObjectFunc{Writer, T}, Options?)"/>
			///   allocates (default: 512).</summary>
			public int InitialBufferSize { get; set; } = 512;
		}

		#endregion
	}
}
