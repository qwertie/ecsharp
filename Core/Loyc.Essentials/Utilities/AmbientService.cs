using System;
using System.Threading;

namespace Loyc.Threading
{
	/// <summary>Holds the shared state behind an ambient service (the Ambient
	///   Service Pattern): a global default instance, plus an async-local
	///   override installed by <see cref="Set"/> in a <c>using</c> statement.
	/// </summary><remarks>
	///   This is based on AsyncLocal, which is reportedly slow, so <see cref="Value"/>
	///   doesn't touch it while no override exists anywhere.
	///   <para/>
	///   Because the override is an AsyncLocal rather than a ThreadLocal, it flows
	///   across <c>await</c> points to continuations, even when they resume on a
	///   different thread.
	/// </remarks>
	public sealed class AmbientService<T>
	{
		// _override is null - the fast path - whenever no override exists anywhere:
		// Set() creates it, and disposing the last Saved discards it (_overrideCount
		// tracks that; _lock makes the create/discard pair atomic vs a racing Set).
		// Maybe<T> distinguishes "no override in this context" from any real value,
		// including default(T), so T need not be a reference type.
		AsyncLocal<Maybe<T>>? _override;
		T _globalDefault;
		int _overrideCount;
		readonly object _lock = new object();

		public AmbientService(T globalDefault)
		{
			if (globalDefault == null)
				throw new ArgumentNullException(nameof(globalDefault));
			_globalDefault = globalDefault;
		}

		/// <summary>The instance used by every execution context that has no
		///   ambient override from <see cref="Set"/>. It can be replaced app-wide.</summary>
		public T GlobalDefault {
			get => _globalDefault;
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				_globalDefault = value;
			}
		}

		/// <summary>Gets the current T instance (the current async execution context's
		///   override if one is active, otherwise <see cref="GlobalDefault"/>.</summary>
		public T Value {
			get {
				var ov = _override;
				if (ov != null) {
					var o = ov.Value;
					if (o.HasValue)
						return o.Value;
				}
				return _globalDefault;
			}
		}

		/// <summary>Installs an ambient (async-local) override. Designed to be used
		///   in a <c>using</c> statement, which restores the old value at the end.
		///   If requested, changes <see cref="GlobalDefault"/> too.</summary>
		/// <remarks>An override installed inside an async method does not propagate up to
		///   its caller's context, so call <see cref="Set"/> in the same method as the
		///   <c>using</c> block that scopes it.</remarks>
		public Saved Set(T newValue, bool alsoSetGlobalDefault = false)
		{
			if (newValue == null)
				throw new ArgumentNullException(nameof(newValue));
			return new Saved(this, newValue, alsoSetGlobalDefault);
		}

		/// <summary>Returned by <see cref="Set"/>; restores the previous ambient
		///   override (if any) when disposed.</summary>
		public struct Saved : IDisposable
		{
			AmbientService<T>? _owner;
			Maybe<T> _oldValue;
			Maybe<T> _oldGlobalDefault;
			internal Saved(AmbientService<T> owner, T newValue, bool alsoSetGlobalDefault)
			{
				AsyncLocal<Maybe<T>> ov;
				lock (owner._lock) {
					owner._overrideCount++;
					ov = owner._override ??= new AsyncLocal<Maybe<T>>();
				}
				_owner = owner;
				_oldValue = ov.Value;
				_oldGlobalDefault = alsoSetGlobalDefault ? (Maybe<T>)owner._globalDefault : default;
				if (alsoSetGlobalDefault)
					owner._globalDefault = newValue;
				ov.Value = newValue;
			}
			public void Dispose()
			{
				var owner = _owner;
				if (owner != null) {
					_owner = null;
					owner._override!.Value = _oldValue;
					_oldValue = default;
					if (_oldGlobalDefault.HasValue) {
						owner._globalDefault = _oldGlobalDefault.Value;
						_oldGlobalDefault = default;
					}
					lock (owner._lock) {
						if (--owner._overrideCount == 0)
							owner._override = null;
					}
				}
			}
		}
	}
}
