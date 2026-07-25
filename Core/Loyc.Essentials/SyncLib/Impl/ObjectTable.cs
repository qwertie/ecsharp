using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Loyc.SyncLib.Impl
{
	/// <summary>An <see cref="IEqualityComparer{T}"/> that uses reference identity,
	///   ignoring any <c>Equals</c>/<c>GetHashCode</c> overrides on T.</summary>
	/// <remarks>This matters for strings in particular: two equal but distinct string
	///   instances must be treated as two different objects by <see cref="ObjectIdTable"/>,
	///   exactly as the old <c>ObjectIDGenerator</c> did.</remarks>
	internal sealed class ObjectReferenceComparer<T> : IEqualityComparer<T> where T : class
	{
		public static readonly ObjectReferenceComparer<T> Instance = new ObjectReferenceComparer<T>();

		public bool Equals(T? x, T? y) => ReferenceEquals(x, y);
		public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
	}

	/// <summary>Assigns a unique <c>long</c> ID (starting at 1) to each distinct object
	///   reference presented to it.</summary>
	/// <remarks>
	///   This is a drop-in replacement for <c>System.Runtime.Serialization.ObjectIDGenerator</c>,
	///   which Microsoft marked obsolete in .NET 8 (SYSLIB0050, "Formatter-based serialization
	///   is obsolete and should not be used"). The observable behaviour is the same: IDs are
	///   assigned sequentially starting at 1, objects are compared by reference identity, and
	///   <c>GetId</c> reports whether the object had been seen before.
	/// </remarks>
	public sealed class ObjectIdTable
	{
		readonly Dictionary<object, long> _ids = new Dictionary<object, long>(ObjectReferenceComparer<object>.Instance);
		long _nextId = 1; // IDs start at one, matching ObjectIDGenerator

		/// <summary>Gets the ID of <paramref name="obj"/>, assigning a new one if this is
		///   the first time the object has been seen.</summary>
		/// <param name="firstTime">Set to true if a new ID was assigned.</param>
		/// <exception cref="ArgumentNullException">obj was null.</exception>
		public long GetId(object obj, out bool firstTime)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));

			#if NET6_0_OR_GREATER
				ref long slot = ref System.Runtime.InteropServices.CollectionsMarshal
					.GetValueRefOrAddDefault(_ids, obj, out bool existed);
				if (existed) {
					firstTime = false;
					return slot;
				}
				firstTime = true;
				return slot = _nextId++;
			#else
				if (_ids.TryGetValue(obj, out long id)) {
					firstTime = false;
					return id;
				}
				firstTime = true;
				_ids.Add(obj, id = _nextId++);
				return id;
			#endif
		}

		/// <summary>Returns the ID previously assigned to <paramref name="obj"/>, or 0 if
		///   the object has not been seen. Does not assign a new ID.</summary>
		public long HasId(object obj, out bool firstTime)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));
			bool found = _ids.TryGetValue(obj, out long id);
			firstTime = !found;
			return found ? id : 0;
		}
	}
}
