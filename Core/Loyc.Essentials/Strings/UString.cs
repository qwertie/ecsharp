using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Loyc.Collections;
using uchar = System.Int32;

namespace Loyc
{
	/// <summary>UString is a slice of a string. It is a wrapper around string that 
	/// provides a <see cref="IBRange{T}"/> of 21-bit UCS-4 characters. "U" stands for 
	/// "Unicode", as in UCS-4, as opposed to a normal string that is UTF-16.</summary>
	/// <remarks>
	/// UString is a slice type: it represents either an entire string, or a region
	/// of code units in a string. .NET strings are converted implicitly to UString.
	/// (it's like Memory{char}, but predates it by a few years.)
	/// <para/>
	/// It has been suggested that Java and .NET's reliance on 16-bit "unicode" 
	/// characters was a mistake, because it turned out that 16 bits was not enough 
	/// to represent all the world's characters.
	/// <para/>
	/// Instead it has been suggested that we should use <a href="http://www.utf8everywhere.org/">
	/// UTF-8 everywhere</a>. To scan UTF-8 data instead of UTF-16 while still 
	/// supporting non-English characters (or "ĉĥáràĉtérŝ", as I like to say),
	/// it is useful to have a bidirectional iterator that scans characters one
	/// codepoint at a time. UString provides that functionality for .NET, and
	/// the nice thing about UString is that it's portable to UTF-8 environments.
	/// That is, by using UString, your code will be portable to a UTF-8 
	/// environment that uses an equivalent implementation of UString for UTF-8. 
	/// Eventually I want Loyc to target native environments, where UTF-8 is 
	/// common, and UString can provide a common data type for both UTF-8 and 
	/// UTF-16 environments.
	/// <para/>
	/// UString is a bidirectional range of "uchar", which is an alias for int
	/// (uchar means "Unicode" or "UCS-4", rather than "unsigned").
	/// <para/>
	/// UString has a <see cref="DecodeAt(int)"/> method that tries to decode
	/// a UTF character to UCS at a particular index.
	/// <para/>
	/// Unfortunately, it's not possible for UString to compare equal to its 
	/// equivalent string, for two reasons: (1) System.String.Equals cannot be
	/// changed, and (2) UString.GetHashCode cannot return the same value as
	/// String.GetHashCode without actually generating a String object, which
	/// would be inefficient (String.GetHashCode cannot be emulated because it
	/// changes between versions of the .NET framework and even between 32- and 
	/// 64-bit builds.)
	/// <para/>
	/// TODO: add Normalize, FindLast, ReplaceAll, etc.
	/// </remarks>
	[DebuggerDisplay("{ToString()} (Length = {Count})")]
	public struct UString : IListSource<char>, ICharSource, IRange<char>, IBRange<uchar>, ICloneable<UString>, IEquatable<UString>
	{
		public static readonly UString Null = default(UString);
		public static readonly UString Empty = new UString("");

		private readonly string _str;
		private int _start, _count;
		
		/// <summary>Initializes a UString slice.</summary>
		/// <exception cref="ArgumentException">The start index was below zero.</exception>
		/// <remarks>The (start, count) range is allowed to be invalid, as long
		/// as 'start' is zero or above. 
		/// <ul>
		/// <li>If 'count' is below zero, or if 'start' is above the original Length, 
		/// the Count of the new slice is set to zero.</li>
		/// <li>if (start + count) is above the original Length, the Count of the new
		/// slice is reduced to <c>list.Length - start</c>.</li>
		/// </ul>
		/// </remarks>
		public UString(string str, int start, int count = int.MaxValue)
		{
			if (start < 0)
				throw new ArgumentException("UString: the start index was below zero.");
			if (count < 0)
				throw new ArgumentException("UString: the count was below zero.");
			_str = str ?? "";
			_start = start;
			if (count > _str.Length - start) {
				count = _str.Length - start;
				if (count < 0) {
					_start -= count;
					count = 0;
				}
			}
			_count = count;
		}
		public UString(string str)
		{
			_str = str;
			_start = 0;
			_count = str == null ? 0 : str.Length;
		}
		private UString(int start, int count, string str)
		{
			// Constructs without bounds checking
			_str = str;
			_start = start;
			_count = count;
			Debug.Assert(start >= 0 && count >= 0 && start + count <= (_str == null ? 0 : _str.Length));
		}
		/// <summary>Returns the original string.</summary>
		/// <remarks>Ideally, keep the string private, there would be no way to 
		/// access its contents beyond the boundaries of the slice. However, the
		/// reality in .NET today is that many methods accept "slices" in the 
		/// form of a triple (string, start index, count). In order to call such an
		/// old-style API using a slice, one must be able to extract the internal
		/// string and start index values.</remarks>
		public string InternalString { get { return _str; } }
		public int InternalStart { get { return _start; } }
		public int InternalStop { get { return _start + _count; } }

		public int Length
		{
			get { return _count; }
		}
		[EditorBrowsable(EditorBrowsableState.Never)]
		public int Count
		{
			get { return _count; }
		}
		public bool IsEmpty
		{
			get { return _count == 0; }
		}
		public uchar First
		{
			get { return DecodeAt(0); }
		}
		public uchar Last
		{
			get {
				int c = DecodeAt(_count - 1);
				if (c >= 0)
					return c;
				if (_count > 1) {
					int c1 = DecodeAt(_count - 2);
					if (c1 >= 0)
						return c1;
				}
				return c;
			}
		}

		public uchar PopFirst(out bool fail)
		{
			if (_count != 0) {
				fail = false;
				var c = First;
				int inc = c >= 0x10000 ? 2 : 1;
				_count -= inc;
				_start += inc;
				return c;
			}
			fail = true;
			return default(uchar);
		}
		public uchar PopLast(out bool fail)
		{
			if (_count != 0) {
				fail = false;
				var c = Last;
				_count -= (c >= 0x10000 ? 2 : 1);
				return c;
			}
			fail = true;
			return default(uchar);
		}

		char IFRange<char>.PopFirst(out bool fail)
		{
			if (_count != 0)
			{
				fail = false;
				_count--;
				return _str[_start++];
			}
			fail = true;
			return default(char);
		}
		char IBRange<char>.PopLast(out bool fail)
		{
			if (_count != 0)
			{
				fail = false;
				_count--;
				return _str[_start + _count];
			}
			fail = true;
			return default(char);
		}
		char IFRange<char>.First => this[0];
		char IBRange<char>.Last => this[_count - 1];
		IFRange<uchar>  ICloneable<IFRange<uchar>>.Clone() { return Clone(); }
		IBRange<uchar>  ICloneable<IBRange<uchar>>.Clone() { return Clone(); }
		IFRange<char>   ICloneable<IFRange<char>>.Clone() { return Clone(); }
		IBRange<char>   ICloneable<IBRange<char>>.Clone() { return Clone(); }
		IRange<char>    ICloneable<IRange<char>>.Clone()  { return Clone(); }
		public UString Clone() { return this; }

		IEnumerator<uchar> IEnumerable<uchar>.GetEnumerator() { return GetEnumerator(); }
		IEnumerator<char> IEnumerable<char>.GetEnumerator() { return new RangeEnumerator<UString,char>(this); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		public RangeEnumerator<UString,uchar> GetEnumerator()
		{
			return new RangeEnumerator<UString,uchar>(this);
		}

		/// <summary>Returns the UCS code point that starts at the specified index.</summary>
		/// <remarks>
		/// Works the same way as <see cref="DecodeAt(int)"/> except that if the index
		/// is invalid, this method returns -1 rather than throwing.
		/// </remarks>
		public uchar TryDecodeAt(int index)
		{
			if ((uint)index < (uint)_count) {
				int c = _str[_start + index];
				if (c < 0xD800 || c > 0xDBFF || (uint)(index + 1) >= (uint)_count)
					return c;
				int c1 = _str[_start + index + 1];
				if (c1 >= 0xDC00 && c1 <= 0xDFFF)
					return 0x10000 + ((c & 0x3FF) << 10) + (c1 & 0x3FF);
			}
			return -1;
		}

		/// <summary>Returns the UCS code point that starts at the specified index.</summary>
		/// <param name="index">Code unit index at which to decode.</param>
		/// <returns>The code point starting at this index.</returns>
		/// <exception cref="IndexOutOfRangeException">invalid <c>index</c>.</exception>
		public uchar DecodeAt(int index)
		{
			uchar r = TryDecodeAt(index);
			if (r == -1)
				ThrowIndexOutOfRange(index);
			return r;
		}

		void ThrowIndexOutOfRange(int i)
		{
			throw new IndexOutOfRangeException(string.Format("UString index out of range ([{0}]/{1})", i, Length));
		}

		/// <summary>Returns the code unit (16-bit value) at the specified index.</summary>
		/// <exception cref="IndexOutOfRangeException">Oops.</exception>
		public char this[int index]
		{
			#if DotNet45
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			#endif
			get { 
				if ((uint)index >= (uint)_count)
					ThrowIndexOutOfRange(index);
				return _str[_start + index];
			}
		}
		/// <summary>Returns the code unit (16-bit value) at the specified index,
		/// or a default value if the specified index was out of range.</summary>
		public char this[int index, char defaultValue]
		{
			#if DotNet45
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			#endif
			get {
				if ((uint)index < (uint)_count)
					return _str[_start + index];
				return defaultValue;
			}
		}
		/// <summary>Returns the code point (21-bit value) at the specified index,
		/// or a default value if the specified index was out of range.</summary>
		public int this[int index, int defaultValue]
		{
			get {
				int c = TryDecodeAt(index);
				if (c == -1)
					return defaultValue;
				return c;
			}
		}
		public char TryGet(int index, out bool fail)
		{
			if ((uint)index < (uint)_count) {
				fail = false;
				return _str[_start + index];
			}
			fail = true;
			return default(char);
		}

		IRange<char> IListSource<char>.Slice(int start, int count) { return Slice(start, count); }
		public UString Slice(int start, int count = int.MaxValue)
		{
			// if either parameter is below zero...
			if ((start | count) < 0) {
				if (start < 0)
					throw new ArgumentOutOfRangeException("start", start, "The start index was below zero.");
				count = 0;
			}
			Debug.Assert(_start <= _str.Length);
			if (start > _count)
				start = _count;
			if (count > _count - start)
				count = _count - start;
			return new UString(_start + start, count, _str); // private constructor
		}

		#region GetHashCode, Equals, ToString

		public override int GetHashCode()
		{
			int hc1 = 352654597, hc2 = hc1;
			for (int i = _start, e = _start + _count; i < e; i++) {
				hc1 = ((hc1 << 5) + hc1 + (hc1 >> 27)) ^ _str[i];
				if (++i == e) break;
				hc2 = ((hc2 << 5) + hc2 + (hc2 >> 27)) ^ _str[i];
			}
			return hc1 + hc2 * 1566083941;
		}
		public override bool Equals(object obj)
		{
			return (obj is UString) && Equals((UString)obj);
		}
		public bool Equals(UString other) { return Equals(other, false); }
		public bool Equals(UString other, bool ignoreCase)
		{
			if (other._count != _count) return false;
			return SubstringEqualHelper(_str, _start, other, ignoreCase);
		}
		public override string ToString()
		{
			return _str.Substring(_start, _count);
		}

		#endregion

		public static bool operator ==(UString x, UString y) { return x.Equals(y); }
		public static bool operator !=(UString x, UString y) { return !x.Equals(y); }
		public static explicit operator string(UString s) { return s._str.Substring(s._start, s._count); }
		public static implicit operator UString(string s) { return new UString(s); }

		/// <summary>Synonym for Slice()</summary>
		public UString Substring(int start, int count)
		{
			if (start < 0)
				throw new ArgumentException("The start index was below zero.");
			if (count < 0)
				count = 0;
			Debug.Assert(_start <= (_str != null ? _str.Length : 0));
			if (start > _count)
				start = _count;
			if (count > _count - start)
				count = _count - start;
			return new UString(_start + start, count, _str);
		}
		/// <summary>Returns the sequence of code units from this UString starting
		/// at the index <c>start</c>, e.g. Substring(1) returns all code units 
		/// except the first.</summary>
		public UString Substring(int start)
		{
			if (start < 0)
				throw new ArgumentException("The start index was below zero.");
			if (start > _count)
				start = _count;
			return new UString(_start + start, _count - start, _str);
		}
		/// <summary>Returns the leftmost <c>length</c> code units of the string, 
		/// or fewer if the string length is less than <c>length</c>.</summary>
		public UString Left(int length)
		{
			CheckParam.IsNotNegative("length", length);
			length = System.Math.Min(_count, length);
			return new UString(_start, length, _str);
		}
		/// <summary>Returns the rightmost <c>length</c> code units of the string,
		/// or fewer if the string length is less than <c>length</c>.</summary>
		public UString Right(int length)
		{
			CheckParam.IsNotNegative("length", length);
			length = System.Math.Min(_count, length);
			return new UString(_start + _count - length, length, _str);
		}

		//
		// TODO: write lots of string-like methods.
		// This can be implemented more efficiently using macros when EC# is ready.
		//

		/// <summary>Finds the specified UCS-4 character.</summary>
		/// <returns>returns a range from the first occurrence of 'what' to the 
		/// original end of this UString. If the character is not found, an empty 
		/// string (slicing the end of this range) is returned.</returns>
		public UString Find(uchar what, bool ignoreCase = false)
		{
			var sub = this;
			if (what <= 0xFFFF) {
				if (ignoreCase) {
					what = char.ToUpperInvariant((char)what);
					for (;;) {
						bool fail;
						var was = sub;
						uchar f = sub.PopFirst(out fail);
						if (fail || what == f || f <= 0xFFFF && what == char.ToUpperInvariant((char)f))
							return was;
					}
				} else {
					int i = _str.IndexOf((char)what, _start, _count);
					if (i == -1) 
						return new UString(InternalStop, 0, _str);
					return new UString(i, InternalStop - i, _str);
				}
			} else {
				for (;;) {
					bool fail;
					var was = sub;
					uchar f = sub.PopFirst(out fail);
					if (fail || what == f)
						return was;
				}
			}
		}
		/// <summary>Finds the specified string within this string.</summary>
		/// <returns>Returns a range from the first occurrence of 'what' to the 
		/// original end of this UString. If 'what' is not found, an empty string
		/// (slicing the end of this range) is returned.</returns>
		public UString Find(UString what, bool ignoreCase = false)
		{
			if (what.Length <= 1) {
				if (what.Length == 0)
					return this;
				return Find(what[0], ignoreCase);
			}
			if (ignoreCase)
				what = what.ToUpper();
			char first = what[0];
			int i = _start, last = _start + _count - what.Length;
			if (ignoreCase)
				for (; i <= last; i++) {
					if (char.ToUpperInvariant(_str[i]) == first && EqualsAtCaseInsensitive(i, what))
						return new UString(i, _start + _count - i, _str);
				}
			else
				for (; i <= last; i++) {
					if (_str[i] == first && EqualsAt(i, what))
						return new UString(i, _start + _count - i, _str);
				}
			return new UString(_start + _count, 0, _str);
		}
		private bool EqualsAtCaseInsensitive(int i, UString what)
		{
			for (int w = 0; w < what.Length; w++) {
				if (char.ToUpperInvariant(_str[i++]) != what[w])
					return false;
			}
			return true;
		}
		private bool EqualsAt(int i, UString what)
		{
			for (int w = 0; w < what.Length; w++) {
				if (_str[i++] != what[w])
					return false;
			}
			return true;
		}

		private bool IsSmallSlice { get { return (_count << 1) < (_str.Length - 4); } }
		
		/// <summary>This method makes a copy of the string if this is a 
		/// sufficiently small slice of a larger string.</summary>
		/// <returns>returns ToString() if <c>InternalString.Length - Length > maxExtra</c>, otherwise this.</returns>
		public UString ShedExcessMemory(int maxExtra)
		{
			if (_str.Length - _count > maxExtra)
				return ToString();
			else
				return this;
		}

		/// <summary>Converts the string to uppercase using the 'invariant' culture.</summary>
		public UString ToUpper()
		{
			var sb = new StringBuilder(Length);
			bool change = false;
			for (int i = _start; i < _start + _count; i++) {
				char c = _str[i], uc = char.ToUpperInvariant(c);
				if (c != uc) change = true;
				sb.Append(uc);
			}
			return change || IsSmallSlice ? sb.ToString() : this;
		}

		/// <summary>Determines whether this string starts with the specified other 
		/// string.</summary>
		/// <returns>true if this string starts with the contents of 'what'</returns>
		public bool StartsWith(UString what, bool ignoreCase = false)
		{
			if (what.Length > Length)
				return false;
			return SubstringEqualHelper(_str, _start, what, ignoreCase);
		}

		public bool EndsWith(UString what, bool ignoreCase = false)
		{
			if (what.Length > Length)
				return false;
			return SubstringEqualHelper(_str, _start + Length - what.Length, what, ignoreCase);
		}
		
		#if DotNet45
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		#endif
		static bool SubstringEqualHelper(string _str, int _start, UString what, bool ignoreCase = false)
		{
			if (ignoreCase)
				for (int i = 0; i < what.Length; i++) {
					if (char.ToUpperInvariant(_str[_start + i]) != char.ToUpperInvariant(what[i]))
						return false;
				}
			else
				for (int i = 0; i < what.Length; i++) {
					if (_str[_start + i] != what[i])
						return false;
				}
			return true;
		}

		/// <summary>Returns a new string in which all occurrences (or a specified 
		/// number of occurrences) of a specified string in the current instance 
		/// are replaced with another specified string.</summary>
		/// <param name="what"></param>
		/// <param name="replacement"></param>
		/// <param name="ignoreCase"></param>
		/// <param name="maxReplacements"></param>
		/// <returns>Returns a new string with replacements made, or the same 
		/// string if no replacements occurred.</returns>
		public UString Replace(UString what, UString replacement, bool ignoreCase = false, int maxReplacements = int.MaxValue)
		{
			if (maxReplacements <= 0)
				return this;
			UString sub = Find(what, ignoreCase);
			if (sub.IsEmpty)
				return this;
			StringBuilder sb = new StringBuilder(_str, _start, sub._start - _start, 
								capacity: _count + replacement._count - what._count);
			sb.Append(replacement._str, replacement._start, replacement._count);

			UString self;
			for (int rep = 1; ; rep++) {
				self = sub.Substring(what.Length);
				if (rep >= maxReplacements)
					break;
				sub = self.Find(what, ignoreCase);
				sb.Append(self._str, self._start, sub._start - self._start);
				if (sub.IsEmpty)
					return sb.ToString();
				sb.Append(replacement._str, replacement._start, replacement._count);
			}
			sb.Append(self._str, self._start, self._count);
			return sb.ToString();
		}
		public UString ReplaceOne(UString what, UString replacement, bool ignoreCase = false)
		{
			return Replace(what, replacement, ignoreCase, 1);
		}

		public int? IndexOf(char find, bool ignoreCase = false)
		{
			if (ignoreCase) {
				int stop = _start + _count;
				find = char.ToUpperInvariant(find);
				for (int i = _start; i < stop; i++)
					if (char.ToUpperInvariant(_str[i]) == find)
						return i - _start;
			} else {
				int i = _str.IndexOf(find, _start, _count);
				if (i > -1)
					return i - _start;
			}
			return null;
		}
		public int? IndexOf(UString find, bool ignoreCase = false)
		{
			int end = _start + _count - find.Length;
			for (int i = _start; i <= end; i++) {
				if (SubstringEqualHelper(_str, i, find, ignoreCase))
					return i - _start;
			}
			return null;
		}

		public Pair<UString, UString> SplitAt(char delimiter, bool ignoreCase = false)
		{
			int? i = IndexOf(delimiter, ignoreCase);
			if (i != null)
				return new Pair<UString, UString>(Substring(0, i.Value), Substring(i.Value + 1));
			else
				return new Pair<UString, UString>(this, UString.Null);
		}
		public Pair<UString, UString> SplitAt(UString delimiter)
		{
			int? i = IndexOf(delimiter);
			if (i != null)
				return new Pair<UString, UString>(Substring(0, i.Value), Substring(i.Value + delimiter.Length));
			else
				return new Pair<UString, UString>(this, UString.Null);
		}

		public static StringBuilder Append(StringBuilder sb, UString s)
		{
			if (s._count == s._str.Length)
				return sb.Append(s);
			else {
				sb.EnsureCapacity(sb.Length + s.Length);
				for (int i = s._start; i < s._start + s._count; i++)
					sb.Append(s._str[i]);
				return sb;
			}
		}

		public static UString operator+(string a, UString b)
		{
			if (b.Length == 0)
				return a;
			if (a.Length == 0)
				return b;
			var sb = new StringBuilder(a, a.Length + b.Length);
			return Append(sb, b).ToString();
		}
		public static UString operator+(UString a, string b)
		{
			if (b.Length == 0)
				return a;
			if (a.Length == 0)
				return b;
			var sb = new StringBuilder(a._str, a._start, a._count, a.Length + b.Length);
			return sb.Append(b).ToString();
		}
		public static UString operator+(UString a, UString b)
		{
			if (b.Length == 0)
				return a;
			if (a.Length == 0)
				return b;
			var sb = new StringBuilder(a._str, a._start, a._count, a.Length + b.Length);
			return Append(sb, b).ToString();
		}
	}
}
