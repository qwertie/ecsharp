using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Essentials
{
	public class InvalidStateException : InvalidOperationException
	{
		public InvalidStateException() : base(Localize.From("This object is in an invalid state.")) { }
		public InvalidStateException(string msg) : base(msg) { }
		public InvalidStateException(string msg, Exception innerException) : base(msg, innerException) { }
	}

	public class ConcurrentModificationException : InvalidOperationException
	{
		public ConcurrentModificationException() : base(Localize.From("A concurrect access was detected during modification.")) { }
		public ConcurrentModificationException(string msg) : base(msg) { }
		public ConcurrentModificationException(string msg, Exception innerException) : base(msg, innerException) { }
	}
}
namespace Loyc.Collections
{
	using Loyc.Essentials;

	public class EnumerationException : InvalidOperationException
	{
		public EnumerationException() : base(Localize.From("The collection was modified after enumeration started.")) { }
		public EnumerationException(string msg) : base(msg) { }
		public EnumerationException(string msg, Exception innerException) : base(msg, innerException) { }
	}
}
