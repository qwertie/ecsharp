---
title: The List Trifecta, part 3
tagline: SparseAList and some AList benchmarks
layout: article
---

## `SparseAList<T>`

There is one more `AList<T>` data structure that I created after writing the first two articles. The motivation for this data structure was _syntax highlighting_. More on that later.

The `SparseAList<T>` is a list in which not all the indexes are "set". Unset (a.k.a. "clear") indexes are "virtual" and use no memory at all, and `sparseAList[i]` returns `default(T)` when `[i]` is clear. Meanwhile, indexes that _are_ set use 4 bytes extra. The `Count` property returns the total number of "virtual" items, set and unset alike. The internal nodes of `SparseAList<T>` are practically the same as a normal `AList<T>`, but the leaf nodes have a different structure:

  ~~~csharp
	public class SparseAListLeaf<T> : AListNode<int, T>
	{
		[DebuggerDisplay("Offset = {Offset}, Item = {Item}")]
		protected struct Entry
		{
			public Entry(uint offset, T item) { Offset = offset; Item = item; }
			public T Item;
			public uint Offset;
		}
		protected InternalDList<Entry> _list;
		protected uint _totalCount;
		...
  ~~~

For example, after running this code:

~~~csharp
var list = SparseAList<string>();
list.InsertSpace(0, 1000);
list[321] = "three two one";
list[32] = "three two";
list[3] = "three";
~~~

the list consists of a single leaf node that contains three `Entry` structures:

~~~
_list[0]: Offset = 3, Item = "three"
_list[1]: Offset = 32, Item = "three two"
_list[2]: Offset = 321, Item = "three two one"
_totalCount = 1000
~~~

From the outside it appears to be a list of 1000 items, but in reality there are only three.

This kind of list may resemble a `SortedDictionary<int,T>` but there is a big difference: you can _insert and remove_ ranges of indexes, which efficiently "shifts" the indexes of all items above the affected index. For example, if I add one million real items to a `SparseAList<T>`, I can do `list.InsertSpace(0, 1000)` and this will increase the index of all existing items by 1000 (in O(log N) time).

`SparseAList<T>` implements my `ISparseListSource<T>` and `ISparseList<T>` interfaces. Compared to a normal list, a sparse list offers the following additional methods:

~~~csharp
	/// <summary>Represents a read-only indexed list in which parts of the index 
	/// space may be unused or "clear".</summary>
	/// ... long remarks section removed ...
	public interface ISparseListSource<T> : IListSource<T>
	{
		/// <summary>Increases <c>index</c> by at least one to reach the next index
		/// that is not classified as empty space, and returns the item at that 
		/// index.</summary>
		/// <param name="index">This parameter is increased by at least one, and
		/// perhaps more than one so that it refers to an index where there is a
		/// value. If <c>index</c> is null upon entering this method, the first 
		/// non-empty space in the list is found. If there are no values at higher 
		/// indexes, if the list is empty or if <c>index + 1 >= Count</c>, 
		/// <c>index</c> is <c>null</c> when the method returns.</param>
		/// <remarks>This method must skip over all indexes i for which 
		/// <c>IsSet(i)</c> returns false, and return the next index for which
		/// <c>IsSet(i)</c> returns true. This method must accept any integer as 
		/// input, including invalid indexes.
		/// </remarks>
		T NextHigherItem(ref int? index);

		/// <summary>Decreases <c>index</c> by at least one to reach the next index
		/// that is not classified as empty space, and returns the item at that 
		/// index.</summary>
		/// <param name="index">This parameter is increased by at least one, and
		/// perhaps more than one so that it refers to an index where there is a
		/// value. If <c>index</c> is null upon entering this method, the last
		/// non-empty space in the list is found. If there are no values at lower
		/// indexes, if the list is empty or if <c>index</c> is 0 or less, 
		/// <c>index</c> is <c>null</c> when the method returns.</param>
		/// <remarks>This method must skip over all indexes i for which 
		/// <c>IsSet(i)</c> returns false, and return the next index for which
		/// <c>IsSet(i)</c> returns true. This method must accept any integer as 
		/// input, including invalid indexes.
		/// </remarks>
		T NextLowerItem(ref int? index);

		/// <summary>Determines whether a value exists at the specified index.</summary>
		/// <param name="index"></param>
		/// <returns>true if a value is assigned at the specified index, or false
		/// if index is part of an empty space, or is outside the range of indexes
		/// that exist.</returns>
		bool IsSet(int index);
	}

	public partial class LCInterfaces
	{
		/// <summary>Gets the next higher index that is not classified as an
		/// empty space, or null if there are no non-blank higher indexes.</summary>
		/// <remarks>This extension method works by calling <c>NextHigherItem()</c>.</remarks>
		public static int? NextHigherIndex<T>(this ISparseListSource<T> list, int? index)
		{
			list.NextHigherItem(ref index);
			return index;
		}
		
		/// <summary>Gets the next lower index that is not classified as an
		/// empty space, or null if there are no non-blank lower indexes.</summary>
		/// <remarks>This extension method works by calling <c>NextHigherItem()</c>.</remarks>
		public static int? NextLowerIndex<T>(this ISparseListSource<T> list, int? index)
		{
			list.NextLowerItem(ref index);
			return index;
		}

		/// <summary>Returns the non-cleared items in the sparse list, along with 
		/// their indexes, sorted by index.</summary>
		/// <remarks>
		/// The returned sequence should exactly match the set of indexes for which 
		/// <c>list.IsSet(Key)</c> returns true.</remarks>
		public static IEnumerable<KeyValuePair<int, T>> Items<T>(this ISparseListSource<T> list)
		{
			int? i = null;
			for (;;) {
				T value = list.NextHigherItem(ref i);
				if (i == null) break;
				yield return new KeyValuePair<int, T>(i.Value, value);
			}
		}
	}

	/// <summary>Represents a sparse list that allows insertion and removal of items
	/// and empty spaces. In a sparse list, some spaces can be "clear" meaning that 
	/// they have no value.</summary>
	/// ... long remarks section removed ...
	public interface ISparseList<T> : ISparseListSource<T>, IListAndListSource<T>
	{
		/// <summary>Unsets the range of indices <c>index</c> to <c>index+count-1</c> inclusive.
		/// If <c>index + count > Count</c>, the sparse list shall enlarge <c>Count</c>
		/// to be equal to <c>index + count</c>.</summary>
		/// <exception cref="ArgumentOutOfRangeException"><c>index</c> or <c>count</c> was negative.</exception>
		/// <exception cref="OverflowException"><c>index + count</c> overflowed.</exception>
		void ClearSpace(int index, int count = 1);
		
		/// <summary>Inserts empty space starting at the specified index.</summary>
		/// <exception cref="OverflowException"><c>index + count</c> overflowed.</exception>
		/// <exception cref="ArgumentOutOfRangeException"><c>index</c> or <c>count</c
		/// > was negative. If <c>index > Count</c>, this method may throw: if, for 
		/// this kind of list, setting this[i] for some invalid i>=0 throws 
		/// <c>ArgumentOutOfRangeException</c>, then so too does this method throw.
		/// If you want the list to be enlarged instead, call <c>Clear(index, 0)</c> 
		/// first, since the contract of Clear() requires it not to throw in the 
		/// same situation.</exception>
		void InsertSpace(int index, int count = 1);
	}
~~~

## `SparseAList<T>` and syntax highlighting

The first version of my syntax highlighter simply kept track of the lexer state at the beginning of each line. This basically worked, but there was a challenge or two [I forgot the details because it's been many months since I worked on it], and it didn't allow me to efficiently add "higher-level" syntax highlighting. I wanted to offer syntax highlighting not just of tokens, but also of higher-level elements (using C# as an example, imagine highlighting data types and method names).

The simplest way to do this is to parse the entire file, which is slow. I did not (and don't) know how to achieve incremental parsing, but I felt that I could at least achieve incremental _lexing_, which would speed up the parsing process because lexical analysis tends to account for around half of total parsing time. My idea is that I would build a list of all non-space tokens in the file and the location where each token starts. Then, when the user types stuff, I would re-lex a small region starting before the edit location, in real time, an this would be a very fast operation (most of the time, anyway). The parser would still have to reprocess the entire file, but it would run on a timer, waiting at least one second or so between each parse. This parser would run on a background thread and could operate on a frozen version of the token list in case it changes on the other thread (remember, `AList` supports fast cloning).

Let's say that each token is a quad of (token type, token value, starting location, length). The biggest problem with that is that a file could easily contain 100,000 tokens (e.g. 10,000+ lines of code). If the user edits the beginning of the file, we don't want to update the "start location" associated with 100,000 tokens for every key that the user presses. That's inefficient. A second problem is the relatively large storage space required for such a list.

A commonly-used alternative is to store information per-line rather than holding all information for the entire file in one collection. This allows most updates to affect only a single line, and we don't have to update line numbers or do large data-moving operations unless a newline is added or removed (and even then, the size of "large" operations is proportional to the number of lines rather than the number of characters). Very long lines cause some inefficiency, but those are rare.

But the parser and lexer I had already written were designed to work with indexes, not (line, column) pairs, and it wasn't clear that I could map from one representation to the other quickly enough. Besides, it could be a lot of _non-reusable_ work: work I spent on that would not be useful outside of the specific task of syntax highlighting. I prefer to create software that has _many applications_.

An second alternative is based on an `AList` (or another data structure designed for this scenario, like a [gap buffer](http://en.wikipedia.org/wiki/Gap_buffer)). In this design, a token is not a quad, but rather a triplet of (token type, token value, length), and the starting location of each token is implied by its location in the AList, and there is a special value that means "no token starts at this location". The storage required for this could be reduced using the [flyweight pattern](http://en.wikipedia.org/wiki/Flyweight_pattern), but would probably still require more than 4 times as much memory as the on-disk file size.

To save memory, a variation is to get rid of the token lengths and values, storing only the token type at every character. The token lengths can be obtained on-the-fly when they are needed, and the token values are probably not needed at all for syntax highlighting (the parser could be given dummy values, assuming that the parse results are ultimately discarded). Token types could probably fit in one byte, so that the token information takes the same space as the text or less.

But I didn't think of that solution. Instead I realized that I could create a sparse version of the `AList`, so that the size of the list was proportional to the number of tokens rather than the number of characters (potentially this saves memory, but not necessarily). So that's what I did. In my current syntax highlighter, I use a `SparseAList<EditorToken>` where `EditorToken` is a compact 8-byte version of the usual 16-byte Loyc [Token](http://loyc.net/doc/code/structLoyc_1_1Syntax_1_1Lexing_1_1Token.html), with no `StartIndex`:

	/// <summary>A compact token representation used by <see cref="SyntaxClassifierForVS"/>.</summary>
	[DebuggerDisplay("Type = {Type}, Length = {Length}, Value = {Value}")]
	public struct EditorToken
	{
		public EditorToken(int type, int length, object value)
			{ TypeAndLength = (type & 0x3FFF) | (Math.Min(length, 0x3FFFF) << 14); Value = value; }
		public int Type { get { return TypeAndLength & 0x3FFF; } }
		public int Length { get { return (int)((uint)TypeAndLength >> 14); } }
		public Token ToToken(int start) { return new Token(Type, start, Length, NodeStyle.Default, Value); }

		public object Value;
		// 14 bits for token type (enough to handle TokenKind), 18 for length
		int TypeAndLength;
	}

But, this is really a story for another article.

## Conclusion

`DList<T>` is a nice alternative to `List<T>`. `SparseAList<T>` is a unique data structure; I have never seen a similar data structure made by anyone else. Enjoy!

Note: not all bugs in `SparseAList<T>` have been worked out. Standby.
