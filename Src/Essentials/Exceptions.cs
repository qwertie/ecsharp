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

	public static class CheckParam
	{
		public static void IsNotNull(string paramName, object arg)
		{
			if (arg == null)
				ThrowArgumentNull(paramName);
		}
		public static void Range(string paramName, int value, int min, int max)
		{
			if (value < min || value > max)
				ThrowOutOfRange(paramName, value, min, max);
		}
		public static void ThrowOutOfRange(string argName)
		{
			throw new ArgumentOutOfRangeException(argName);
		}
		public static void ThrowOutOfRange(string argName, int value, int min, int max)
		{
			throw new ArgumentOutOfRangeException(argName, Localize.From(@"Argument ""{0}"" value '{1}' is not within the expected range ({2}..{3})", argName, value, min, max)); 
		}
		public static void ThrowArgumentNull(string argName)
		{
			throw new ArgumentNullException(argName);
		}
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
