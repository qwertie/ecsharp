using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Collections.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Essentials.Tests
{
	public class ListWrapperForTesting<T> : ListWrapper<T, List<T>>,
		IAddRange<T>, ICloneable<ListWrapperForTesting<T>>
	{
		public ListWrapperForTesting() : base(new List<T>()) { }
		public ListWrapperForTesting(List<T> obj) : base(obj) { }

		public bool IsEmpty => throw new NotImplementedException();

		public void AddRange(IEnumerable<T> e) => ListExt.AddRange(_obj, e);

		public void AddRange(IReadOnlyCollection<T> s) => ListExt.AddRange(_obj, s);

		public ListWrapperForTesting<T> Clone() => new ListWrapperForTesting<T>(new List<T>(_obj));
	}
	
	public class ListWrapperTests : ListTests<ListWrapperForTesting<int>>
	{
		public ListWrapperTests(int randomSeed) 
			: base(true, size => new ListWrapperForTesting<int>().With(l => l.Resize(size)), randomSeed) { }
	}
}
