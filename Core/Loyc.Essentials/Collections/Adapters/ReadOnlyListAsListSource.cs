using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Collections
{
	public static partial class LCExt
	{
		/// <summary>Adapter: treats any IReadOnlyList{T} object as IListSource{T}.</summary>
		/// <remarks>This method is named "AsListSource" and not "ToListSource" 
		/// because, in contrast to methods like ToArray() and ToList(), it does not 
		/// make a copy of the sequence.</remarks>
		public static IListSource<T> AsListSource<T>(this IReadOnlyList<T> c)
		{
			if (c == null)
				return null!; // Nullability contract broken, and there's no attribute like [return: MaybeNullIfNull("c")]
			var listS = c as IListSource<T>;
			if (listS != null)
				return listS;
			return new ReadOnlyListAsListSource<T>(c);
		}
		/// <summary>No-op.</summary>
		public static IListSource<T> AsListSource<T>(this IListSource<T> c) => c;
	}

	/// <summary>
	/// Helper type returned from <see cref="LCExt.AsListSource{T}(IReadOnlyList{T})"/>.
	/// </summary>
	/// <summary>A read-only wrapper that implements IListSource.</summary>
	[Serializable]
	public sealed class ReadOnlyListAsListSource<T> : WrapperBase<IReadOnlyList<T>>, IListSource<T>
	{
		public ReadOnlyListAsListSource(IReadOnlyList<T> obj) : base(obj) { }

		public int Count => _obj.Count;
		public bool IsEmpty => Count == 0;
		public bool Contains(T item) => _obj.Contains(item);

		public T this[int index] => _obj[index];
		public int IndexOf(T item) => _obj.IndexOf(item);
		public void CopyTo(T[] array, int arrayIndex) => _obj.CopyTo(array, arrayIndex);

		public IEnumerator<T> GetEnumerator() => _obj.GetEnumerator();
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => (_obj as System.Collections.IEnumerable).GetEnumerator();

		public T this[int index, T defaultValue] => (uint)index >= (uint)_obj.Count ? defaultValue : _obj[index];
		[return: MaybeNull] // There's no attribute like [return: MaybeNullIf("fail")]
		public T TryGet(int index, out bool fail)
		{
			return (fail = (uint)index >= (uint)_obj.Count) ? default(T) : _obj[index];
		}
		IListSource<T> IListSource<T>.Slice(int start, int count) => new Slice_<T>(this, start, count);
		public Slice_<T> Slice(int start, int count) => new Slice_<T>(this, start, count);
	}
}
