using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using System.ComponentModel;
using uchar = System.Int32;

namespace Loyc.Collections
{
	/// <summary>UString is a wrapper around string that provides a range of 21-bit 
	/// UCS-32 characters.</summary>
	/// <remarks>
	/// It has been suggested that Java and .NET's reliance on 16-bit "unicode" 
	/// characters was a mistake, because it turned out that 16 bits was not enough 
	/// to represent all the world's characters. And I agree.
	/// <para/>
	/// Instead it has been suggested that we should use <a href="http://www.utf8everywhere.org/">
	/// UTF-8 everywhere</a>. To scan UTF-8 data instead of UTF-16 while still 
	/// supporting non-English characters (or "ĉĥáràĉtérŝ", as I like to say),
	/// it is useful to have a bidirectional iterator that scans characters one
	/// codepoint at a time. UString provides that functionality for .NET, and
	/// the nice thing about UString is that it's portable to UTF-8 environments.
	/// Eventually I want Loyc to target native environments, where UTF-8 is
	/// common, and UString can provide a common data type for both UTF-8 and
	/// UTF-16 environments.
	/// <para/>
	/// UString is a bidirectional range of "uchar", which is an alias for int
	/// (uchar means "Unicode" or "UCS-4", rather than "unsigned").
	/// <para/>
	/// The difference between StringSlice and UString is that StringSlice is a
	/// random-access range of char, while UString is a bidirectional range of
	/// uchar (int) that also happens to implement <see cref="IListSource{char}"/>.
	/// Also, UString has a <see cref="DecodeAt(int)"/> method that tries to 
	/// decodes a UTF character to UCS at a particular index. Since UString and
	/// StringSlice are just slightly different views of the same data, you can
	/// implicitly cast between them.
	/// <para/>
	/// TODO: add StartsWith, IndexOf, etc.
	/// </remarks>
	public struct UString : IBRange<uchar>, IListSource<char>, ICloneable<UString>, IEquatable<UString>
	{
		string _str;
		int _start, _count;
		
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
			_count = count;
			if (_count > _str.Length - start)
				_count = System.Math.Max(_str.Length - _start, 0);
		}
		public UString(string str)
		{
			_str = str;
			_start = 0;
			_count = str.Length;
		}
		public string InternalString
		{
			get { return _str; }
		}
		public int InternalStart
		{
			get { return _start; }
		}

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
		public uchar Front
		{
			get { return DecodeAt(0); }
		}
		public uchar Back
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

		public uchar PopFront(out bool fail)
		{
			if (_count != 0) {
				fail = false;
				var c = Front;
				int inc = c >= 0x10000 ? 2 : 1;
				_count -= inc;
				_start += inc;
				return c;
			}
			fail = true;
			return default(uchar);
		}
		public uchar PopBack(out bool fail)
		{
			if (_count != 0) {
				fail = false;
				var c = Back;
				_count -= (c >= 0x10000 ? 2 : 1);
				return c;
			}
			fail = true;
			return default(uchar);
		}

		IFRange<uchar>  ICloneable<IFRange<uchar>>.Clone() { return Clone(); }
		IBRange<uchar>  ICloneable<IBRange<uchar>>.Clone() { return Clone(); }
		public UString Clone() { return this; }

		IEnumerator<uchar> IEnumerable<uchar>.GetEnumerator() { return GetEnumerator(); }
		IEnumerator<char> IEnumerable<char>.GetEnumerator() { return ((StringSlice)this).GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		public RangeEnumerator<UString,uchar> GetEnumerator()
		{
			return new RangeEnumerator<UString,uchar>(this);
		}

		/// <summary>Returns the UCS code point that starts at the specified index.</summary>
		/// <param name="index">Code unit index at which to decode.</param>
		/// <returns>The code point starting at this index, or a negative number.</returns>
		/// <exception cref="IndexOutOfRangeException">Oops.</exception>
		/// <remarks>
		/// If decoding fails, either because the index points to the "middle" of a
		/// multi-code-unit sequence or because the string contains an invalid
		/// UTF sequence, this method returns a negative value (the bitwise 'not' of 
		/// the invalid char).
		/// </remarks>
		public uchar DecodeAt(int index)
		{
			int c = this[0];
			if (c < 0xD800 || c >= 0xE000)
				return c;
			if (c < 0xDC00) {
				int c1 = this[1, '\0'];
				if (c1 >= 0xDC00 && c1 < 0xE000)
					return 0x10000 + (c << 10) + c1;
			}
			return ~(int)c;
		}

		/// <summary>Returns the code unit (16-bit value) at the specified index.</summary>
		/// <exception cref="IndexOutOfRangeException">Oops.</exception>
		public char this[int index]
		{
			get { 
				if ((uint)index < (uint)_count)
					return _str[_start + index];
				throw new IndexOutOfRangeException();
			}
		}
		/// <summary>Returns the code unit (16-bit value) at the specified index,
		/// or a default value if the specified index was out of range.</summary>
		public char this[int index, char defaultValue]
		{
			get { 
				if ((uint)index < (uint)_count)
					return _str[_start + index];
				return defaultValue;
			}
		}
		public char TryGet(int index, ref bool fail)
		{
			if ((uint)index < (uint)_count) {
				fail = false;
				return _str[_start + index];
			}
			fail = true;
			return default(char);
		}
		IRange<char> IListSource<char>.Slice(int start, int count) { return ((StringSlice)this).Slice(start, count); }
		public UString Slice(int start, int count = int.MaxValue)
		{
			if (start < 0)
				throw new ArgumentException("The start index was below zero.");
			if (count < 0)
				count = 0;
			var slice = new UString();
			slice._str = this._str;
			slice._start = this._start + start;
			slice._count = count;
			if (slice._count > this._count - start)
				slice._count = System.Math.Max(this._count - _start, 0);
			return slice;
		}

		#region GetHashCode, Equals

		public override uchar GetHashCode()
		{
			int hc1 = 352654597, hc2 = hc1;
			for (int i = _start, e = _start + _count; i < e; i++) {
				hc1 = ((hc1 << 5) + hc1 + (hc1 >> 27)) ^ _str[i];
				if (i++ == e) break;
				hc2 = ((hc2 << 5) + hc2 + (hc2 >> 27)) ^ _str[i];
			}
			return hc1 + hc2 * 1566083941;
		}
		public override bool Equals(object obj)
		{
			return (obj is UString) && Equals((UString)obj);
		}
		public bool Equals(UString other)
		{
			if (other._count != _count) return false;
			for (int i = _start, j = other._start, e = i + _count; i < e; i++, j++) {
				if (_str[i] != other._str[j])
					return false;
			}
			return true;
		}

		#endregion

		public static bool operator ==(UString x, UString y) { return x.Equals(y); }
		public static bool operator !=(UString x, UString y) { return !x.Equals(y); }
		public static implicit operator string(UString s) { return s._str.Substring(s._start, s._count); }
		public static implicit operator UString(string s) { return new UString(s); }
		public static implicit operator StringSlice(UString s) { return new StringSlice(s._str, s._start, s._count); }

		/// <summary>Synonym for Slice()</summary>
		public UString Substring(int start, int count = int.MaxValue) { return Slice(start, count); }

		// TODO: write lots of string-like methods
	}
}
