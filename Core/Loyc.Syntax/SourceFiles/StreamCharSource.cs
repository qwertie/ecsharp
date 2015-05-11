namespace Loyc.Syntax
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.IO;
	using System.Linq;
	using System.Diagnostics;
	using Loyc.Utilities;
	using Loyc.Math;
	using Loyc.MiniTest;
	using Loyc.Syntax;
	using Loyc.Collections;
	using Loyc.Collections.Impl;
	using Loyc.Syntax.Les;

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
	/// <para/>
	/// This class assumes the underlying stream never changes.
	/// <para/>
	/// The stream does not (and probably cannot, if I understand the 
	/// System.Text.Decoder API correctly) save the decoder state at each block 
	/// boundary. Consequently, only encodings that meet special constraints
	/// will work with StreamCharSource. These include Encoding.Unicode,
	/// Encoding.UTF8, and Encoding.UTF32, but not Encoding.UTF7. Using 
	/// unsupported encodings will cause exceptions and/or or corrupted data 
	/// output while reading from the StreamCharSource.
	/// <para/>
	/// The decoder must meet the following constraints:
	/// 1. Characters must be divided on a byte boundary. UTF-7 doesn't work 
	///    because some characters are encoded using Base64.
	/// 2. Between characters output by the decoder, the decoder must be 
	///    stateless. Therefore, encodings that support compression generally 
	///    won't work.
	/// 3. The decoder must produce at least one character from a group of 
	///    8 bytes (StreamCharSource.MaxSeqSize).
	/// </remarks>
	class StreamCharSource : ListSourceBase<char>, ICharSource
	{
		protected Stream _stream;            // stream from which data is read
		protected byte[] _buf;               // input buffer
		protected char[] _blk, _blk2;        // character buffers
		protected int _blkStart, _blk2Start; // character index
		protected int _blkLen, _blk2Len;     // number of characters
		/// <summary>A sorted list of mappings between byte positions and character 
		/// indexes. In each Pair(of A,B), A is the character index and B is the byte 
		/// index. This list is built on-demand.
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

		public StreamCharSource(Stream stream) : this(stream, UTF8Encoding.Default.GetDecoder(), DefaultBufSize) { }
		public StreamCharSource(Stream stream, Decoder decoder) : this(stream, decoder, DefaultBufSize) { }
		public StreamCharSource(Stream stream, Encoding encoding) : this(stream, encoding.GetDecoder(), DefaultBufSize) { }
		public StreamCharSource(Stream stream, Decoder decoder, int bufSize)
		{
			if (bufSize <= MaxSeqSize)
				throw new ArgumentException("bufSize <= " + MaxSeqSize.ToString());
			if (!stream.CanSeek)
				throw new ArgumentException("stream does not support seeking.");
			_buf = new byte[bufSize];
			_blk = _blk2 = new char[0];
			_stream = stream;
			_decoder = decoder;
			_blkStart = _blk2Start = int.MinValue;
			_blkOffsets.Add(new Pair<int, uint>(0, 0)); // start of the first block
		}

		public new StringSlice Slice(int startIndex, int length)
		{
			CheckParam.IsNotNegative("startIndex", startIndex);
			if (length <= 0)
				return new StringSlice("", 0, length);

			StringBuilder sb = new StringBuilder(Math.Min(length, 1024));
			for (int i = startIndex; i < startIndex + length; i++) {
				if ((uint)(i - _blkStart) >= (uint)_blkLen) {
					if (!Access(i))
						break;
				}
				sb.Append(_blk[i - _blkStart]);
			}
			return sb.ToString();
		}

		public sealed override char TryGet(int index, out bool fail)
		{
			if (!Access(index)) {
				fail = true;
				return (char)0xFFFF;
			}
			fail = false;
			return _blk[index - _blkStart];
		}

		protected void SwapBlks()
		{
			MathEx.Swap(ref _blk, ref _blk2);
			MathEx.Swap(ref _blkLen, ref _blk2Len);
			MathEx.Swap(ref _blkStart, ref _blk2Start);
		}

		// Goal: get a block of characters, so that _blkStart <= charIndex < _blkStart + _blkLen
		protected bool Access(int charIndex)
		{
			if (charIndex >= _blkStart && charIndex < _blkStart + _blkLen) {
				return true;
			} else if (charIndex >= _blk2Start && charIndex < _blk2Start + _blk2Len) {
				SwapBlks();
				return true;
			} else if (_reachedEnd && charIndex >= _eofIndex) {
				return false;
			} else {
				SwapBlks(); // current _blk is backed up as _blk2
				if (charIndex >= _eofIndex)
					ScanPast(charIndex);
				else if (charIndex >= 0)
					ReloadBlockOf(charIndex);
				return (uint)(charIndex - _blkStart) < (uint)_blkLen;
			}
		}

		protected void ReloadBlockOf(int charIndex)
		{
			int i = GetBlockIndex(charIndex);
			Debug.Assert(i+1 < _blkOffsets.Count);
			
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
			int i = ListExt.BinarySearch((IList<Pair<int,uint>>) _blkOffsets,
				new Pair<int, uint>(charIndex, 0),
				delegate(Pair<int, uint> a, Pair<int, uint> b) { return a.A.CompareTo(b.A); });
			if (i < 0)
				i = ~i - 1;
			return i;
		}

		protected const int DefaultBufSize = 2048 + MaxSeqSize - 1;
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
			int amtRequested = _buf.Length;
			int amtRead = _stream.Read(_buf, 0, amtRequested);
			if (amtRead < amtRequested) {
				_reachedEnd = true;
				if (amtRead > 0) {
					// decode the block
					_blkStart = _eofIndex;
					int cc = _decoder.GetCharCount(_buf, 0, amtRead, true);
					if (_blk.Length < cc)
						Array.Resize(ref _blk, cc);
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
				int minBlkSize = _decoder.GetCharCount(_buf, 0, amtProcessed) + 1;
				if (_blk.Length < minBlkSize)
					Array.Resize(ref _blk, minBlkSize);
				_blkStart = _eofIndex;
				_blkLen = _decoder.GetChars(_buf, 0, amtProcessed, _blk, 0, false);
				
				// then add one more char, hopefully reaching a "checkpoint" where
				// the decoder's internal state is empty (a seekable block boundary)
				int n = 1, cc;
				while((cc = _decoder.GetCharCount(_buf, amtProcessed, n)) == 0) {
					n++;
					if (amtProcessed + n == _buf.Length)
						throw new ArgumentException(Localize.From("StreamCharSource cannot use the supplied decoder because it can produce single characters from byte sequences longer than {0} characters", MaxSeqSize));
				}
				minBlkSize = _blkLen + cc;
				if (_blk.Length < minBlkSize)
					Array.Resize(ref _blk, minBlkSize);
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

		public override int Count
		{
			get {
				while (!_reachedEnd)
					ScanPast(_eofIndex);
				return _eofIndex;
			}
		}
	}

	/// <summary>Unit tests of StreamCharSource</summary>
	[TestFixture]
	public class StreamCharSourceTests : TestHelpers
	{
		Encoding _enc;
		int _bufSize = 200;
		
		public StreamCharSourceTests() { _enc = UTF8Encoding.Default; }
		public StreamCharSourceTests(Encoding enc, int bufSize) { _enc = enc; _bufSize = bufSize; }
		
		[Test]
		public void TestWithLes()
		{
			// The idea: write some simple, correct LES code, then randomize it by
			// inserting comments in a bunch of places. Lex it both ways and verify 
			// that the non-whitespace tokens are the same either way.
			string lesCode = @"import System;
				import Loyc;
				namespace TestCode {
					class TestClass {
						public answer::float = 42.0e0f;
						public ComputeAnswer()::object {
							你好();
							// slack off while pretending to work
							Thread.Sleep(1042.0 * 1000 -> int);
							return answer;
						};
					};
				};";
			MessageHolder msgs = new MessageHolder();
			var lexer = LesLanguageService.Value.Tokenize(lesCode, msgs);
			var tokens = lexer.Buffered().Where(tok => !tok.IsWhitespace).ToList();
			Debug.Assert(msgs.List.Count == 0);

			LexWithSCSAndCompare(tokens, lesCode);

			var r = new Random();
			var lesCode2 = new StringBuilder(lesCode);
			for (int i = lesCode2.Length-1; i >= 0; i--)
				if (lesCode2[i] == ' ' || (i > 0 && lesCode[i-1] == ' '))
					if (r.Next(4) == 0) {
						if (r.Next(2) == 0)
							lesCode2.Insert(i, "/*你好 你好*/");
						else
							lesCode2.Insert(i, "/*2345678901234567 lárgér cómmént 45678901234567*/"); // 50
					}

			LexWithSCSAndCompare(tokens, lesCode2.ToString());
		}

		private void LexWithSCSAndCompare(List<Lexing.Token> originalTokens, string lesCode)
		{
			var msgs = new MessageHolder();
			var stream = new MemoryStream(Encoding.UTF8.GetBytes(lesCode));
			var source = new StreamCharSource(stream, Encoding.UTF8.GetDecoder(), _bufSize);
			var lexer = LesLanguageService.Value.Tokenize(source, "StreamCharSource.les", msgs);
			var tokens = lexer.Buffered().Where(tok => !tok.IsWhitespace).ToList();
			Assert.AreEqual(0, msgs.List.Count);
			ExpectList(tokens, originalTokens);

			// Now reset the lexer and read the same StreamCharSource again 
			// (different code paths are used the second time)
			lexer.Reset();
			tokens = lexer.Buffered().Where(tok => !tok.IsWhitespace).ToList();
			Assert.AreEqual(0, msgs.List.Count);
			ExpectList(tokens, originalTokens);
		}
	}
}
