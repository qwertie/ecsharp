using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Runtime
{
	/// <summary>
	/// Designed to be used in a "using" statement to alter a thread-local variable 
	/// temporarily. See G.Altered() (in Loyc.Utilities) for a usage example.
	/// </summary>
	public class AlteredVariable<T> : IDisposable
	{
		T _oldValue;
		ThreadLocalVariable<T> _variable;

		public AlteredVariable(ThreadLocalVariable<T> variable, T newValue)
		{
			_variable = variable;
			_oldValue = variable.Value;
			variable.Value = newValue;
		}
		public void Dispose()
		{
			_variable.Value = _oldValue;
		}
		
		public T OldValue { get { return _oldValue; } }
		public ThreadLocalVariable<T> Variable { get { return _variable; } }
	}
}
