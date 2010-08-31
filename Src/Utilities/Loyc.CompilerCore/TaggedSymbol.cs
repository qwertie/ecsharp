using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Runtime;

namespace Loyc.Utilities.Loyc.CompilerCore
{
	public class TaggedSymbol<T> : Symbol, ITags<T>
	{
		public TaggedSymbol(Symbol prototype) : base(prototype) { }
		protected Dictionary<Symbol, T> _tags;

		public IDictionary<Symbol, T> Tags
		{
			get { 
				if (_tags != null)
					return _tags;
				return _tags = new Dictionary<Symbol, T>();
			}
		}
	}
	public class TaggedSymbol : TaggedSymbol<object>
	{
		public TaggedSymbol(Symbol prototype) : base(prototype) { }
	}
}
