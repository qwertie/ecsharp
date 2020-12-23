using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Collections.Tests;

namespace Loyc.Essentials.Tests
{
	public class DictionaryWrapperForTesting<K, V> : DictionaryWrapper<K, V, Dictionary<K, V>>,
		IAddRange<KeyValuePair<K, V>>, ICloneable<DictionaryWrapperForTesting<K, V>>
	{
		public DictionaryWrapperForTesting() : base(new Dictionary<K, V>()) { }
		public DictionaryWrapperForTesting(Dictionary<K, V> obj) : base(obj) { }

		public void AddRange(IEnumerable<KeyValuePair<K, V>> e) => DictionaryExt.AddRange(_obj, e);

		public void AddRange(IReadOnlyCollection<KeyValuePair<K, V>> s) => DictionaryExt.AddRange(_obj, s);

		public DictionaryWrapperForTesting<K, V> Clone() => new DictionaryWrapperForTesting<K, V>(new Dictionary<K, V>(_obj));
	}

	class DictionaryWrapperTests : DictionaryTests<DictionaryWrapperForTesting<object, object>>
	{
	}
}
