/*
 * Created by SharpDevelop.
 * User: Pook
 * Date: 4/10/2011
 * Time: 8:47 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace Loyc.Collections
{
	/// <summary>A helper class that makes it easier to return objects that
	/// implement IIterable.</summary>
	/// <remarks>
	/// The C# compiler makes it extraordinarily easy to create objects that
	/// implement <see cref="IEnumerable{T}"/>. For example:
	/// <code>
	/// public IEnumerable&lt;int> Digits()
	/// {
	///     for (int i = 0; 10 > i; i++)
	///         yield return i;
	/// }
	/// </code>
	/// In C# 1.0 you would have had to write two classes to accomplish the same
	/// thing (one implementing IEnumerable, one implementing IEnumerator.)
	/// <see cref="IIterable{T}"/> cannot provide the same convenience without
	/// a customized C# compiler. However, this class makes the task slightly
	/// easier. The simplest IIterable version of the above code is as follows:
	/// <code>
	/// public IIterable&lt;int> Digits()
	/// {
	///     return new IteratorFactory&lt;int>(() =>
	///     {
	///         int i = -1;
	///         return (ref bool ended) =>
	///         {
	///             if (++i >= 10)
	///                 ended = true;
	///             return i;
	///         };
	///     });
	/// }
	/// </code>
	/// Here, the outer lambda function effectively implements IIterable{T}, and the
	/// inner lambda function implements Iterator{T}.
	/// </remarks>
	public class IteratorFactory<T> : IterableBase<T>
	{
		Func<Iterator<T>> _iterable;
		public IteratorFactory(Func<Iterator<T>> iterable)
		{
			_iterable = iterable;
		}
		public override Iterator<T> GetIterator()
		{
 			return _iterable();
		}
	}
}
