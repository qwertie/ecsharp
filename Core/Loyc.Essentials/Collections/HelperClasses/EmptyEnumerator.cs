// This file is part of the Loyc project. Licence: LGPL
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Loyc.Collections
{
	/// <summary>Helper class: an empty enumerator.</summary>
	[Serializable]
	public class EmptyEnumerator<T> : IEnumerator<T>, IEnumerator
	{
		public static readonly IEnumerator<T> Value = new EmptyEnumerator<T>();

		public T Current => default(T)!;
		object? IEnumerator.Current => this.Current;
		public void Dispose() { }
		public bool MoveNext() { return false; }
		public void Reset() { }
	}
}
