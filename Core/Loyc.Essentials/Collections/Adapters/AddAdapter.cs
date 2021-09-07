using Loyc.Collections;
using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Collections
{
	
	public struct AddAdapter<T> : IAdd<T>
	{
		Action<T> _add;
		
		public AddAdapter(Action<T> add) => _add = add;

		public void Add(T item) => _add(item);
	}
	
	public class AddAdapter<T, Context> : IAdd<T>
	{
		Action<T, Context> _add;
		Context _context;

		public AddAdapter(Action<T, Context> add, Context context)
		{
			_add = add;
			_context = context;
		}

		public void Add(T item) => _add(item, _context);
	}
}
