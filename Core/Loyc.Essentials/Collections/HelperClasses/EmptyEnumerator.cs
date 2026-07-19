// This file is part of the Loyc project. Licence: LGPL
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Loyc.Collections
{
	/// <summary>Helper class: an empty enumerator.</summary>
	public static class EmptyEnumerator<T>
	{
		[Obsolete("Use Empty<T>.Enumerator instead")]
		public static readonly List<T>.Enumerator Value = new List<T>().GetEnumerator();
	}
}
