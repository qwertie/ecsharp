using System;
using System.Diagnostics;
using System.Threading;

namespace Loyc.Threading
{
	/// <summary>
	/// Designed to be used in a "using" statement to temporarily alter a 
	/// <see cref="ThreadLocalVariable{T}"/> or <see cref="Holder{T}"/>.
	/// </summary>
	public struct SavedValue<T> : IDisposable
	{
		T _oldValue;
		IHasMutableValue<T> _valueHolder;

		public SavedValue(IHasMutableValue<T> oldValue, T newValue)
		{
			_valueHolder = oldValue;
			_oldValue = oldValue.Value;
			oldValue.Value = newValue;
		}
		public void Dispose()
		{
			_valueHolder.Value = _oldValue;
		}
		
		public T OldValue { get { return _oldValue; } }
	}

	/// <summary>
	/// Designed to be used in a "using" statement to temporarily alter a 
	/// <see cref="ThreadLocal{T}"/>.
	/// </summary>
	public struct SavedThreadLocal<T> : IDisposable
	{
		T _oldValue;
		ThreadLocal<T> _variable;

		public SavedThreadLocal(ThreadLocal<T> variable, T newValue)
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
