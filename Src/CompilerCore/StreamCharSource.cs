using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Loyc.Utilities;
using Loyc.Runtime;
using NUnit.Framework;
using System.Diagnostics;

namespace Loyc.CompilerCore
{
	/// <summary>
	/// Exposes a stream as an ICharSource, as though it were an array of 
	/// characters. The stream must support seeking, and if a text decoder 
	/// is specified, it must meet certain constraints.
	/// </summary><remarks>
	/// This class reads small blocks of bytes from a stream, reloading 
	/// blocks from the stream when necessary. Data is cached with a pair 
	/// of character buffers, and a third buffer is used to read from the
	/// stream. A Stream is required rather than a TextReader because
	/// TextReader doesn't support seeking.
	/// 
	/// This class assumes the underlying stream never changes.
	/// 
	/// The stream does not (and probably cannot, if I understand the 
	/// System.Text.Decoder API correctly) save the decoder state at each block 
	/// boundary. Consequently, only encodings that meet special constraints
	/// will work with StreamCharSource. These include Encoding.Unicode,
	/// Encoding.UTF8, and Encoding.UTF32, but not Encoding.UTF7. Using 
	/// unsupported encodings will cause exceptions and/or or corrupted data 
	/// output while reading from the StreamCharSource.
	/// 
	/// The decoder must meet the following constraints:
	/// 1. Characters must be divided on a byte boundary. UTF-7 doesn't work 
	///    because some characters are encoded using Base64.
	/// 2. Between characters output by the decoder, the decoder must be 
	///    stateless. Therefore, encodings that support compression generally 
	///    won't work.
	/// 3. The decoder must produce at least one character from a group of 
	///    8 bytes (StreamCharSource.MaxSeqSize).
	/// </remarks>
	class StreamCharSource : CharIndexPositionMapper, ICharSource
	{
		protected Stream _stream;            // stream from which data is read
		protected byte[] _buf;               // input buffer
		protected char[] _blk, _blk2;        // character buffers
		protected int _blkStart, _blk2Start; // character index
		protected int _blkLen, _blk2Len;     // number of characters
		/// <summary>A sorted list of mappings between byte positions and character 
		/// indexes. In each Pair(of A,B), A is the character index and B is the byte 
		/// index. This list is built on-demand as the 
		/// </summary>
		protected List<Pair<int,uint>> _blkOffsets = new List<Pair<int,uint>>();
		/// <summary>Set true when the last block has been scanned. If true, then
		/// _eofIndex and _eofPosition indicate the Count and the size of the 
		/// stream, respectively.</summary>
		protected bool _reachedEnd = false;
		/// <summary>_eofIndex is the character index of EOF if it has been reached 
		/// or, if not, the index of the first unscanned character. _eofIndex 
		/// equals _blkOffsets[_blkOffsets.Count-1].A.</summary>
		protected int _eofIndex = 0;
		/// <summary>_eofPosition is the byte position of EOF if it has been reached 
		/// or, if not, the position of the first unscanned character. _eofPosition 
		/// equals _blkOffsets[_blkOffsets.Count-1].B.</summary>
		protected uint _eofPosition = 0;
		protected Decoder _decoder;

		public static readonly Encoding LoycEncoding;
		static StreamCharSource() {
			// TODO use Latin1DecoderFallback
			LoycEncoding = Encoding.UTF8;
		}
		public StreamCharSource(Stream stream) : this(stream, UTF8Encoding.Default.GetDecoder(), 256) { }
		public StreamCharSource(Stream stream, Decoder decoder) : this(stream, decoder, 256) { }
		public StreamCharSource(Stream stream, Encoding encoding) : this(stream, encoding.GetDecoder(), 256) { }
		public StreamCharSource(Stream stream, Decoder decoder, int bufSize)
		{
			if (bufSize <= MaxSeqSize)
				throw new ArgumentException("bufSize <= " + MaxSeqSize.ToString());
			if (!stream.CanSeek)
				throw new ArgumentException("stream cannot seek.");
			_buf = new byte[bufSize];
			_blk = _blk2 = new char[0];
			_stream = stream;
			_decoder = decoder;
			_blkStart = _blk2Start = int.MinValue;
			_blkOffsets.Add(new Pair<int, uint>(0, 0)); // start of the first block
		}

		public override string Substring(int startIndex, int length)
		{
			if (startIndex + length > _eofIndex) {
				ScanPast(startIndex + length-1);
				if (startIndex + length > _eofIndex)
					length = _eofIndex - startIndex;
			}
			if (length <= 1) {
				if (length == 1)
					return ((char)this[startIndex]).ToString();
				else
					return string.Empty;
			} else {
				// TODO: optimize this
				StringBuilder sb = new StringBuilder(length);
				for (int i = 0; i < length; i++)
					sb.Append((char)this[startIndex + i]);
				return sb.ToString();
			}
		}

		public override char this[int index]
		{
			get {
				if (index >= _eofIndex) {
					ScanPast(index);
					if (index >= _eofIndex)
						return (char)0xFFFF;
					Debug.Assert(G.IsInRange(index, _blkStart, _blkStart + _blkLen-1));
					return _blk[index - _blkStart];
				} else if (index < 0) {
					throw new ArgumentException("index < 0");
				} else if (index >= _blkStart && index < _blkStart + _blkLen) {
					return _blk[index - _blkStart];
				} else if (index >= _blk2Start && index < _blk2Start + _blk2Len) {
					SwapBlks();
					return _blk[index - _blkStart];
				}

				ReloadBlockOf(index);
				Debug.Assert(G.IsInRange(index, _blkStart, _blkStart + _blkLen-1));
				return _blk[index - _blkStart];
			}
		}
		protected void SwapBlks()
		{
			G.Swap(ref _blk, ref _blk2);
			G.Swap(ref _blkLen, ref _blk2Len);
			G.Swap(ref _blkStart, ref _blk2Start);
		}

		protected void ReloadBlockOf(int charIndex)
		{
			int i = GetBlockIndex(charIndex);
			Debug.Assert(i+1 < _blkOffsets.Count);
			
			SwapBlks();
			_blkStart = _blkOffsets[i].A;
			_blkLen = _blkOffsets[i+1].A - _blkStart;
			_stream.Position = _blkOffsets[i].B;
			int bytesToRead = (int)(_blkOffsets[i+1].B - _blkOffsets[i].B);
			if (_stream.Read(_buf, 0, bytesToRead) != bytesToRead)
				throw new Exception(Localize.From("Result of {0} changed unexpectedly", "stream.Read"));
			_decoder.Reset();
			if (_decoder.GetChars(_buf, 0, bytesToRead, _blk, 0, true) != _blkLen)
				throw new Exception(Localize.From("Result of {0} changed unexpectedly", "decoder.GetChars"));
		}

		private int GetBlockIndex(int charIndex)
		{
			int i = G.BinarySearch<Pair<int, uint>>(_blkOffsets,
				new Pair<int, uint>(charIndex, 0),
				delegate(Pair<int, uint> a, Pair<int, uint> b) { return a.A.CompareTo(b.A); });
			if (i < 0)
				i = ~i - 1;
			return i;
		}

		protected const int MaxSeqSize = 8;
		protected void ScanPast(int index)
		{
			while (index >= _eofIndex && !_reachedEnd) {
				_stream.Position = _eofPosition;
				ReadNextBlock();
			}
		}

		protected void ReadNextBlock()
		{
			_decoder.Reset();

			// Read the next block
			int amtRequested = _buf.Length - MaxSeqSize;
			int amtRead = _stream.Read(_buf, 0, amtRequested);
			if (amtRead < amtRequested) {
				_reachedEnd = true;
				if (amtRead > 0) {
					// decode the block
					_blkStart = _eofIndex;
					int cc = _decoder.GetCharCount(_buf, 0, amtRead, true);
					AutoEnlargeBuffers(cc);
					_blkLen = _decoder.GetChars(_buf, 0, amtRead, _blk, 0, true);
					Debug.Assert(cc == _blkLen);
					// compute & record location of end of block
					_eofIndex += _blkLen;
					_eofPosition += (uint)amtRead;
					_blkOffsets.Add(new Pair<int, uint>(_eofIndex, _eofPosition));
				}
			} else {
				// decode the block...
				int amtProcessed = amtRead - MaxSeqSize;
				AutoEnlargeBuffers(_decoder.GetCharCount(_buf, 0, amtProcessed) + 1);
				_blkStart = _eofIndex;
				_blkLen = _decoder.GetChars(_buf, 0, amtProcessed, _blk, 0, false);
				// then 'top it up'
				int n = 1, cc;
				while((cc = _decoder.GetCharCount(_buf, amtProcessed, n)) == 0) {
					n++;
					if (amtProcessed + n == _buf.Length)
						throw new ArgumentException(Localize.From("StreamCharSource cannot use the supplied decoder because it can produce single characters from byte sequences longer than {0} characters", MaxSeqSize));
				}
				AutoEnlargeBuffers(_blkLen + cc);
				try {
					_blkLen += _decoder.GetChars(_buf, amtProcessed, n, _blk, _blkLen, true);
					amtProcessed += n;
				} catch(Exception exc) { 
					// assume index-out-of-range encountered. Note that this exception 
					// may never happen even if the decoder is incompatible.
					throw new ArgumentException(Localize.From("StreamCharSource cannot use the supplied decoder because it seems to divide characters on bit boundaries"), exc);
				}
				// compute & record location of end of block
				_eofIndex += _blkLen;
				_eofPosition += (uint)amtProcessed;
				_blkOffsets.Add(new Pair<int, uint>(_eofIndex, _eofPosition));
				// note: when necessary, the caller will rewind to the last byte 
				// actually processed by doing _stream.Position = _eofPosition
			}
		}

		protected void AutoEnlargeBuffers(int to)
		{
			if (_blk.Length < to)
				Array.Resize(ref _blk, to);
			if (_blk2.Length < to)
				Array.Resize(ref _blk2, to);
		}

		public override int Count
		{
			get {
				while (!_reachedEnd)
					ScanPast(_eofIndex);
				return _eofIndex;
			}
		}
	}

	class StreamCharSourceFile : StreamCharSource, ISourceFile
	{
		public StreamCharSourceFile(ILanguageStyle lang, Stream stream) : this(lang, stream, UTF8Encoding.Default.GetDecoder(), 256) { }
		public StreamCharSourceFile(ILanguageStyle lang, Stream stream, Decoder decoder) : this(lang, stream, decoder, 256) { }
		public StreamCharSourceFile(ILanguageStyle lang, Stream stream, Encoding encoding) : this(lang, stream, encoding.GetDecoder(), 256) { }
		public StreamCharSourceFile(ILanguageStyle lang, Stream stream, Decoder decoder, int bufSize) : base(stream, decoder, bufSize) { _lang = lang; }

		protected ILanguageStyle _lang;
		public ILanguageStyle Language
		{
			get { return _lang; }
		}
	}

	[TestFixture]
	public class StreamCharSourceTests : CharIndexPositionMapperTests
	{
		Encoding _enc;
		int _bufSize = 256;
		
		public StreamCharSourceTests() { _enc = UTF8Encoding.Default; }
		public StreamCharSourceTests(Encoding enc, int bufSize) { _enc = enc; _bufSize = bufSize; }
		
		protected override CharIndexPositionMapper CreateSource(string s)
		{
			MemoryStream ms = new MemoryStream(s.Length * 2);
			byte[] b = _enc.GetBytes(s);
			ms.Write(b, 0, b.Length);
			// StreamCharSource will set ms.Position = 0 by itself
			return new StreamCharSource(ms, _enc.GetDecoder(), _bufSize);
		}
	}
}
