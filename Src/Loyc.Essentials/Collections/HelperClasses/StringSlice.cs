using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Loyc.Collections;
using System.Diagnostics;

namespace Loyc.Collections
{
	/// <summary>A random-access slice of System.String.</summary>
	/// <remarks>
	/// Where possible, it is recommended that you use <see cref="UString"/> instead.
	/// </remarks>
	public struct StringSlice : IRange<char>, ICharSource, ICloneable<StringSlice>
	{
		public static StringSlice Empty = new StringSlice("");

		string _str;
		int _start, _count;

		/// <summary>Initializes a StringSlice.</summary>
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
		public StringSlice(string list, int start, int count)
		{
			if (start < 0)
				throw new ArgumentException("The start index was below zero.");
			if (count < 0)
				count = 0;
			_str = list;
			_start = start;
			_count = count;
			if (_count > _str.Length - start)
				_count = System.Math.Max(_str.Length - _start, 0);
		}
		public StringSlice(string str)
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

		[EditorBrowsable(EditorBrowsableState.Never)]
		public int Length
		{
			get { return _count; }
		}
		public int Count
		{
			get { return _count; }
		}
		public bool IsEmpty
		{
			get { return _count == 0; }
		}
		public char Front
		{
			get { return this[0]; }
		}
		public char Back
		{
			get { return this[_count - 1]; }
		}

		public char PopFront(out bool fail)
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
		public char PopBack(out bool fail)
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

		IFRange<char> ICloneable<IFRange<char>>.Clone() { return Clone(); }
		IBRange<char> ICloneable<IBRange<char>>.Clone() { return Clone(); }
		IRange<char>  ICloneable<IRange<char>> .Clone() { return Clone(); }
		public StringSlice Clone() { return this; }

		IEnumerator<char> IEnumerable<char>.GetEnumerator() { return GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		public RangeEnumerator<StringSlice, char> GetEnumerator()
		{
			return new RangeEnumerator<StringSlice, char>(this);
		}

		public char this[int index]
		{
			get
			{
				if ((uint)index < (uint)_count)
					return _str[_start + index];
				throw new IndexOutOfRangeException();
			}
		}
		public char this[int index, char defaultValue]
		{
			get {
				if ((uint)index < (uint)_count)
					return _str[_start + index];
				return defaultValue;
			}
		}
		public int this[int index, int defaultValue]
		{
			get { 
				if ((uint)index < (uint)_count)
					return _str[_start + index];
				return defaultValue;
			}
		}
		public char TryGet(int index, out bool fail)
		{
			if ((uint)index < (uint)_count)
			{
				fail = false;
				return _str[_start + index];
			}
			fail = true;
			return default(char);
		}
		IRange<char> IListSource<char>.Slice(int start, int count) { return Slice(start, count); }
		public StringSlice Slice(int start, int count)
		{
			if (start < 0)
				throw new ArgumentException("The start index was below zero.");
			if (count < 0)
				count = 0;
			Debug.Assert(_start <= _str.Length);
			var slice = new StringSlice();
			if (start > _count)
				start = _count;
			if (count > _count - start)
				count = _count - start;
			slice._str = _str;
			slice._start = _start + start;
			slice._count = count;
			return slice;
		}

		public static implicit operator StringSlice(UString s) { return new StringSlice(s.InternalString, s.InternalStart, s.Length); }
		public static implicit operator StringSlice(string s)  { return new StringSlice(s); }

		#region GetHashCode, Equals, ToString

		public override int GetHashCode()
		{
			return ((UString)this).GetHashCode();
		}
		public override bool Equals(object obj)
		{
			return (obj is StringSlice) && Equals((StringSlice)obj);
		}
		public bool Equals(StringSlice other)
		{
			return ((UString)this).Equals((UString)other);
		}
		public override string ToString()
		{
			return _str.Substring(_start, _count);
		}

		#endregion
	}
}
