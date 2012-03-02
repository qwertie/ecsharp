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
		protected Func<Iterator<T>> _iterable;

		public IteratorFactory(Func<Iterator<T>> iterable)
		{
			_iterable = iterable;
		}
		public sealed override Iterator<T> GetIterator()
		{
 			return _iterable();
		}
	}

	/// <summary>Implements <see cref="IIterable{T}"/> trivially by always returning 
	/// the same iterator that was passed to the constructor. Warning: this adapter 
	/// should be used very carefully because you can iterate over the collection
	/// only once, which is technically incorrect behavior for IIterable.
	/// </summary>
	/// <remarks>
	/// This adapter is useful if a method returns an Iterator but you want to run a 
	/// LINQ query on the result. <see cref="Iterator{T}"/> doesn't support LINQ so
	/// an adapter like this one is required.
	/// </remarks>
	[Serializable]
	public class IteratorToIterableAdapter<T> : IterableBase<T>
	{
		Iterator<T> _it;
		public IteratorToIterableAdapter(Iterator<T> it)
		{
			_it = it;
		}
		public override Iterator<T> GetIterator()
		{
			return _it;
		}
	}


	/// <summary>A helper class that makes it easier to return objects that
	/// implement IIterable.</summary>
	/// <remarks>This class is the same as <see cref="IteratorFactory{T}"/>, except
	/// that the constructor takes an additional state argument S. This allows you
	/// to avoid using closures around local variables of the iterator factory 
	/// (which require the compiler to implicitly create an additional class and 
	/// object). The state object is passed to the iterator factory lambda which 
	/// you must also pass to the constructor. You may use it, for example, to hold 
	/// a reference to the collection being enumerated.</remarks>
	public class IteratorFactoryWithState<S,T> : IterableBase<T>
	{
		protected Func<S, Iterator<T>> _iterable;
		protected S _state;

		public IteratorFactoryWithState(S state, Func<S, Iterator<T>> iterable)
		{
			_iterable = iterable;
			_state = state;
		}
		public sealed override Iterator<T> GetIterator()
		{
 			return _iterable(_state);
		}
	}
}
