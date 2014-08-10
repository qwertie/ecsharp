using System;
using System.Diagnostics;

namespace Loyc.Threading
{
	/// <summary>
	/// Designed to be used in a "using" statement to alter a thread-local variable 
	/// temporarily.
	/// </summary>
	public struct PushedTLV<T> : IDisposable
	{
		T _oldValue;
		ThreadLocalVariable<T> _variable;

		public PushedTLV(ThreadLocalVariable<T> variable, T newValue)
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
		public T Value { get { return _variable.Value; } }
	}
}
