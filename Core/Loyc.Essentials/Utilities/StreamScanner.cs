using static System.Math;
using Loyc.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using Loyc.Compatibility;

namespace Loyc.Essentials;

/// <summary>An implementation of <see cref="IScanner{T}"/> that reads from a Stream.</summary>
/// <remarks>
///   This class assumes that no other code will seek/read/write in the stream while this 
///   class is using it. It does not require CanSeek to be true on the stream, but if it
///   is, then <see cref="CanScanBackward"/> will be true, and <see cref="Position"/> will
///   be fully functional.
/// </remarks>
public class StreamScanner : IScanner<byte>, IDisposable
{
	/// <summary>Opens a file and returns a <see cref="StreamScanner"/> representing it.</summary>
	public static StreamScanner OpenFile(string path, int minBlockSize = 0x10000, FileShare share = FileShare.Read)
	{
		var stream = new FileStream(path, FileMode.Open, FileAccess.Read, share);
		return new StreamScanner(stream, minBlockSize, disposeStream: true);
	}

	// Stream being read
	protected Stream _stream;
	// Whether to dispose _stream if Dispose() is called
	protected bool _disposeStream;
	// Position of the end of the last block that was read (should match _stream.Position).
	// If the stream is not seekable, this holds the relative position (useful for debugging).
	long _position;
	// This variable tracks the block that was most recently returned, in case Read(skip, ...)
	// is called with skip < _currentBlock.Length, in which case the next block returned
	// will include some data copied from the previous block.
	Memory<byte> _previousBlock;

	public StreamScanner(Stream stream, int minBlockSize = 0x10000, bool disposeStream = true)
	{
		_stream = stream;
		_disposeStream = disposeStream;
		MinBlockSize = minBlockSize;
		if (stream.CanSeek) {
			_position = stream.Position;
		}
	}

	/// <summary>The minimum amount of data to read from the stream at once.</summary>
	public int MinBlockSize { get; set; }

	/// <summary>
	///   Gets or sets the stream's Position. If the stream is not seekable,
	///   the getter returns the position relative to the stream position when this 
	///   object was constructed, and the setter cannot be used.
	/// </summary>
	public long Position
	{
		get => _position;
		set {
			_position = _stream.Position = value; // Throws if not !CanSeek
			_previousBlock = default;
		}
	}

	public bool CanScanBackward => _stream.CanSeek;

	public ReadOnlyMemory<byte> Read(int skip, int minLength, ref Memory<byte> buffer)
	{
		Memory<byte> readBuffer; // region in which data will be read from the _stream

		if (skip < 0)
		{
			Debug.Assert(_stream.Position == _position);

			// Seeking backward is the most complex case to do performantly, if there
			// is an overlapping memory region to preserve.
			MaybeReallocate(minLength, ref buffer);

			_position = _stream.Position = _position - _previousBlock.Length + skip;

			int overlapAmount = buffer.Length + skip;
			if (overlapAmount > 0 && overlapAmount < _previousBlock.Length) {
				// Preserve the overlapping portion of the old buffer (into `buffer`)
				int readAmount = buffer.Length - overlapAmount;
				_previousBlock.Slice(0, overlapAmount).CopyTo(buffer.Slice(readAmount));

				// Read the non-overlapping block of data
				readBuffer = ReadNewBlock(buffer.Slice(0, readAmount));

				// Check whether we somehow reached EOF after seeking backward, implying
				// that the stream was shortened.
				Debug.Assert(readBuffer.Length == readAmount);
				if (readBuffer.Length < readAmount) {
					// That's weird. Try to compensate.
					int shortBy = readAmount - readBuffer.Length;
					_previousBlock = readBuffer;
					_position = _stream.Position;
				} else {
					// Normal case
					Debug.Assert(readBuffer.Span == buffer.Slice(0, readAmount).Span);
					_previousBlock = buffer;
					_position = _stream.Position + overlapAmount;
					// Technically, this could also throw if the stream was shortened,
					// ugh. But at least _position correctly points to the end of
					// _previousBlock at this point.
					_stream.Position = _position;
				}

				return _previousBlock;
			} else {
				// Re-read entire buffer (via fallthrough)
				return _previousBlock = ReadNewBlock(buffer);
			}
		}
		else if (skip < _previousBlock.Length)
		{
			// Drop data that was skipped, leaving only the overlapping region
			_previousBlock = _previousBlock.Slice(skip);

			if (minLength <= _previousBlock.Length) {
				// No need to read anything new
				return _previousBlock;
			}

			MaybeReallocate(minLength, ref buffer);

			// Move stuff from the end of the previous block to the beginning of the
			// (new or reused) buffer.
			_previousBlock.CopyTo(buffer);

			readBuffer = buffer.Slice(_previousBlock.Length);
			
			var readBuffer2 = ReadNewBlock(readBuffer);
			return _previousBlock = buffer.Slice(0, _previousBlock.Length + readBuffer2.Length);
		}
		else
		{
			skip -= _previousBlock.Length;
			_previousBlock = default;

			MaybeReallocate(minLength, ref buffer);

			if (skip > 0) {
				// The user asked to skip over part of the file without looking at it
				if (skip > buffer.Length && _stream.CanSeek) {
					_position = _stream.Seek(skip, SeekOrigin.Current);
					skip = 0;
				} else {
					// Skip by reading into `buffer`
					for (int gotBytes; skip > 0; skip -= gotBytes, _position += gotBytes) {
						#if NETSTANDARD2_0 || NET45 || NET46 || NET47
						G.Verify(MemoryMarshal.TryGetArray<byte>(buffer, out var span));
						span = span.Slice(0, Min(skip, buffer.Span.Length));
						gotBytes = _stream.Read(span.Array, span.Offset, span.Count);
						#else
						gotBytes = _stream.Read(buffer.Span.Slice(0, Min(skip, buffer.Span.Length)));
						#endif
						if (gotBytes == 0)
							return default;
					}
				}
			}

			return _previousBlock = ReadNewBlock(buffer);
		}
	}

	/// <summary>Picks a buffer size for Read() and reallocates if necessary.</summary>
	void MaybeReallocate(int minLength, ref Memory<byte> buffer)
	{
		int blockSize = Max(Max(MinBlockSize, minLength), buffer.Length);
		if (buffer.Length < blockSize) {
			buffer = new byte[blockSize].AsMemory();
		}
	}

	// Reads a new block of data from the _stream
	Memory<byte> ReadNewBlock(Memory<byte> targetBuffer)
	{
		#if NETSTANDARD2_0 || NET45 || NET46 || NET47
			G.Verify(MemoryMarshal.TryGetArray<byte>(targetBuffer, out var span));
			
			for (int gotBytes; span.Count > 0; _position += gotBytes, span = span.Slice(gotBytes)) {
				gotBytes = _stream.Read(span.Array, span.Offset, span.Count);
			
				if (gotBytes == 0) {
					// Reached EOF
					return targetBuffer.Slice(0, targetBuffer.Length - span.Count);
				}
			}
		#else
			var span = targetBuffer.Span;
			for (int gotBytes; span.Length > 0; _position += gotBytes, span = span.Slice(gotBytes)) {
				gotBytes = _stream.Read(span);
			
				if (gotBytes == 0) {
					// Reached EOF
					return targetBuffer.Slice(0, targetBuffer.Length - span.Length);
				}
			}
		#endif
		return targetBuffer;
	}

	public void Dispose()
	{
		if (_disposeStream)
			_stream.Dispose();
	}
}
