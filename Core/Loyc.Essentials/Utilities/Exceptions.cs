using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc
{
	/// <summary>An exception thrown when an object detects that its own state is
	/// invalid, or in other words, that an invariant has been violated. 
	/// </summary><remarks>
	/// This exception often indicates that something went wrong earlier in the 
	/// execution of the program, before the method was called that threw this
	/// exception.
	/// </remarks>
	[Serializable]
	public class InvalidStateException : InvalidOperationException
	{
		public InvalidStateException() : base(Localize.From("This object is in an invalid state.")) { }
		public InvalidStateException(string msg) : base(msg) { }
		public InvalidStateException(string msg, Exception innerException) : base(msg, innerException) { }
	}

	/// <summary>An exception thrown when a data structure is accessed (read or 
	/// written) by one thread at the same time as it is modified on another 
	/// thread.</summary><remarks>
	/// Note: most data structures do not detect this situation, or do not detect it
	/// reliably. For example, the <see cref="Loyc.Collections.AList{T}"/> family of data structures 
	/// may or may not detect this situation. If it is detected then this exception
	/// is thrown, otherwise the data structure may take on an invalid state, leading
	/// to <see cref="InvalidStateException"/> or other unexpected exceptions.
	/// </remarks>
	[Serializable]
	public class ConcurrentModificationException : InvalidOperationException
	{
		public ConcurrentModificationException() : base(Localize.From("A concurrect access was detected during modification.")) { }
		public ConcurrentModificationException(string msg) : base(msg) { }
		public ConcurrentModificationException(string msg, Exception innerException) : base(msg, innerException) { }
	}

	/// <summary>An exception thrown when an attempt is made to modify a read-only object.</summary>
	[Serializable]
	public class ReadOnlyException : InvalidOperationException
	{
		public ReadOnlyException() : base(Localize.From("An attempt was made to modify a read-only object.")) { }
		public ReadOnlyException(string msg) : base(msg) { }
		public ReadOnlyException(string msg, Exception innerException) : base(msg, innerException) { }
	}

	/// <summary>Helper methods for checking argument values that throw exceptions 
	/// when an argument value is not acceptable.</summary>
	public static class CheckParam
	{
		public static T IsNotNull<T>(string paramName, T arg) where T : class
		{
			if (arg == null)
				ThrowArgumentNull(paramName);
			return arg;
		}
		public static int IsNotNegative(string argName, int value)
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(argName, Localize.From(@"Argument ""{0}"" value '{1}' should not be negative.", argName, value));
			return value;
		}
		public static int IsInRange(string paramName, int value, int min, int max)
		{
			if (value < min || value > max)
				ThrowOutOfRange(paramName, value, min, max);
			return value;
		}
		public static void ThrowOutOfRange(string argName)
		{
			throw new ArgumentOutOfRangeException(argName);
		}
		public static void ThrowOutOfRange(string argName, int value, int min, int max)
		{
			throw new ArgumentOutOfRangeException(argName, Localize.From(@"Argument ""{0}"" value '{1}' is not within the expected range ({2}...{3})", argName, value, min, max)); 
		}
		public static void ThrowArgumentNull(string argName)
		{
			throw new ArgumentNullException(argName);
		}
		public static void Arg(string argName, bool condition, object argValue)
		{
			if (!condition)
				throw new ArgumentException(Localize.From("Invalid argument ({0} = '{1}')", argName, argValue));
		}
		public static void Arg(string argName, bool condition)
		{
			if (!condition)
				throw new ArgumentException(Localize.From("Invalid value for '{0}'", argName));
		}
	}
}

namespace Loyc.Collections
{
	/// <summary>An exception thrown by an enumerator when it detects that the collection
	/// was modified after enumeration started but before it finished.</summary>
	[Serializable]
	public class EnumerationException : InvalidOperationException
	{
		public EnumerationException() : base(Localize.From("The collection was modified after enumeration started.")) { }
		public EnumerationException(string msg) : base(msg) { }
		public EnumerationException(string msg, Exception innerException) : base(msg, innerException) { }
	}

	/// <summary>An exception thrown by dictionary objects when they are asked to
	/// "add" a key-value pair that already exists.</summary>
	[Serializable]
	public class KeyAlreadyExistsException : InvalidOperationException
	{
		public KeyAlreadyExistsException() : base(Localize.From("The item or key being added already exists in the collection.")) { }
		public KeyAlreadyExistsException(string msg) : base(msg) { }
		public KeyAlreadyExistsException(string msg, Exception innerException) : base(msg, innerException) { }
	}

	/// <summary>An exception thrown by methods or properties that require a non-empty
	/// sequence but were provided with an empty sequence.</summary>
	[Serializable]
	public class EmptySequenceException : InvalidOperationException
	{
		public EmptySequenceException() : base(Localize.From("Failed to access the sequence because it is empty.")) { }
		public EmptySequenceException(string msg) : base(msg) { }
		public EmptySequenceException(string msg, Exception innerException) : base(msg, innerException) { }
	}
}
