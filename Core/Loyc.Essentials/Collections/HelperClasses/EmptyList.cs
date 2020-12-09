// This file is part of the Loyc project. Licence: LGPL
using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>Helper class: <see cref="EmptyList{T}.Value"/> is a read-only empty list.</summary>
	/// <remarks>It is a boxed copy of <c>ListExt.Repeat(default(T), 0)</c>.</remarks>
	[Serializable]
	public static class EmptyList<T>
	{
		public static readonly IListAndListSource<T> Value = new Repeated<T>(default(T), 0);
	}
}
