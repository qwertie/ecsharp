using System;
using System.Threading;

namespace Loyc.Threading
{
	/// <summary>Holds the shared state behind an ambient service (the Ambient
	///   Service Pattern): a global default instance, plus an async-local 
	///   override installed by <see cref="Set"/> in a <c>using</c> statement.
	/// </summary><remarks>
	///   This is based on AsyncLocal, which is reportedly slow. <see cref="Value"/> 
	///   has a fast path for the case that no override is active anywhere.
	///   <para/>
	///   Because the override is an AsyncLocal rather than a ThreadLocal, it flows
	///   across <c>await</c> points to continuations, even when they resume on a
	///   different thread.
	/// </remarks>
	public sealed class AmbientService<T> where T : class
	{
		readonly AsyncLocal<T?> _override = new AsyncLocal<T?>();
		volatile T _globalDefault;
		int _overrideCount;

		public AmbientService(T globalDefault)
			=> _globalDefault = globalDefault ?? throw new ArgumentNullException(nameof(globalDefault));

		/// <summary>The instance used by every execution context that has no
		///   ambient override from <see cref="Set"/>. It can be replaced app-wide.</summary>
		public T GlobalDefault {
			get => _globalDefault;
			set => _globalDefault = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>Gets the current T instance (the current async execution context's
		///   override if one is active, otherwise <see cref="GlobalDefault"/>.</summary>
		public T Value => _overrideCount == 0 ? _globalDefault : _override.Value ?? _globalDefault;

		/// <summary>Installs an ambient (async-local) override. Designed to be used
		///   in a <c>using</c> statement, which restores the old value at the end.</summary>
		/// <remarks>An override installed inside an async method does not propagate up to 
		///   its caller's context, so call <see cref="Set"/> in the same method as the 
		///   <c>using</c> block that scopes it.</remarks>
		public Saved Set(T newValue)
		{
			if (newValue == null)
				throw new ArgumentNullException(nameof(newValue));
			return new Saved(this, newValue);
		}

		/// <summary>Returned by <see cref="Set"/>; restores the previous ambient
		///   override (if any) when disposed.</summary>
		public struct Saved : IDisposable
		{
			AmbientService<T>? _owner;
			T? _oldValue;
			internal Saved(AmbientService<T> owner, T newValue)
			{
				_owner = owner;
				_oldValue = owner._override.Value;
				owner._override.Value = newValue;
				Interlocked.Increment(ref owner._overrideCount);
			}
			public void Dispose()
			{
				var owner = _owner;
				if (owner != null) {
					_owner = null;
					owner._override.Value = _oldValue;
					_oldValue = null;
					Interlocked.Decrement(ref owner._overrideCount);
				}
			}
		}
	}
}
