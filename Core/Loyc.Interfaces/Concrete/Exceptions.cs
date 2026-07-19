using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
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
		public InvalidStateException() : this(null) { }
		public InvalidStateException(string? msg) : base(msg ?? "This object is in an invalid state.".Localized()) { }
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
		public ConcurrentModificationException() : this(null) { }
		public ConcurrentModificationException(string? msg) : base(msg ?? "A concurrect access was detected during modification.".Localized()) { }
		public ConcurrentModificationException(string msg, Exception innerException) : base(msg, innerException) { }
	}

	/// <summary>An exception thrown when an attempt is made to modify a read-only object.</summary>
	[Serializable]
	public class ReadOnlyException : InvalidOperationException
	{
		public ReadOnlyException() : this(null) { }
		public ReadOnlyException(string? msg) : base(msg ?? "An attempt was made to modify a read-only object.".Localized()) { }
		public ReadOnlyException(string msg, Exception innerException) : base(msg, innerException) { }
	}

	/// <summary>Helper methods for checking argument values that throw exceptions 
	/// when an argument value is not acceptable.</summary>
	public static class CheckParam
	{
		public static T IsNotNull<T>(string paramName, T? arg) where T : class
		{
			if (arg == null)
				ThrowArgumentNull(paramName);
			return arg;
		}
		public static int IsNotNegative(string argName, int value)
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(argName, @"Argument ""{0}"" value '{1}' should not be negative.".Localized(argName, value));
			return value;
		}
		public static int IsInRange(string paramName, int value, int min, int max)
		{
			if (value < min || value > max)
				ThrowOutOfRange(paramName, value, min, max);
			return value;
		}
		[DoesNotReturn]
		public static void ThrowOutOfRange(string argName)
		{
			throw new ArgumentOutOfRangeException(argName);
		}
		[DoesNotReturn]
		public static void ThrowOutOfRange(string argName, string message)
		{
			throw new ArgumentOutOfRangeException(argName, message.Localized());
		}
		[DoesNotReturn]
		public static void ThrowOutOfRange(string argName, string message, object? arg1, object? arg2 = null)
		{
			throw new ArgumentOutOfRangeException(argName, message.Localized(arg1, arg2));
		}
		[DoesNotReturn]
		public static void ThrowOutOfRange(string argName, int value, int min, int max)
		{
			throw new ArgumentOutOfRangeException(argName, @"Argument ""{0}"" value '{1}' is not within the expected range ({2}...{3})".Localized(argName, value, min, max)); 
		}
		[DoesNotReturn]
		public static void ThrowArgumentNull(string argName)
		{
			throw new ArgumentNullException(argName);
		}
		[DoesNotReturn]
		public static void ThrowBadArgument(string message)
		{
			throw new ArgumentException(message.Localized());
		}
		[DoesNotReturn]
		public static void ThrowBadArgument(string argName, string message)
		{
			throw new ArgumentException(message.Localized(), argName);
		}
		[DoesNotReturn]
		public static void ThrowBadArgument(string argName, string message, object? arg1, object? arg2 = null)
		{
			throw new ArgumentException(message.Localized(arg1, arg2), argName);
		}

		/// <summary>Captures a common code sequence from many slice classes.</summary>
		/// <param name="start">Start location in the slice</param>
		/// <param name="count">Desired length of the slice</param>
		/// <param name="listCount">Count of the list being sliced</param>
		/// <returns>Returns count or, if start + count > listCount, Max(listCount - start, 0).</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int ThrowIfStartOrCountAreBelowZeroAndLimitCountIfNecessary(int start, int count, int outerCount)
		{
			if (start < 0) CheckParam.ThrowBadArgument("The start index was below zero.");
			if (count < 0) CheckParam.ThrowBadArgument("The count was below zero.");
			return count <= outerCount - start ? count : System.Math.Max(outerCount - start, 0);
		}
		public static void Arg(string argName, [DoesNotReturnIf(false)] bool condition, object argValue)
		{
			if (!condition)
				throw new ArgumentException("Invalid argument ({0} = '{1}')".Localized(argName, argValue));
		}
		public static void Arg(string argName, [DoesNotReturnIf(false)] bool condition)
		{
			if (!condition)
				throw new ArgumentException("Invalid value for '{0}'".Localized(argName));
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
		public EnumerationException() : this(null) { }
		public EnumerationException(string? msg) : base(msg ?? "The collection was modified after enumeration started.".Localized()) { }
		public EnumerationException(string msg, Exception innerException) : base(msg, innerException) { }
	}

	/// <summary>An exception thrown by dictionary objects when they are asked to
	/// "add" a key-value pair that already exists.</summary>
	[Serializable]
	public class KeyAlreadyExistsException : InvalidOperationException
	{
		public KeyAlreadyExistsException() : this(null) { }
		public KeyAlreadyExistsException(string? msg) : base(msg ?? "The item or key being added already exists in the collection.".Localized()) { }
		public KeyAlreadyExistsException(string msg, Exception innerException) : base(msg, innerException) { }
	}

	/// <summary>An exception thrown by methods or properties that require a non-empty
	/// sequence but were provided with an empty sequence.</summary>
	[Serializable]
	public class EmptySequenceException : InvalidOperationException
	{
		public EmptySequenceException() : this(null) { }
		public EmptySequenceException(string? msg) : base(msg ?? "Failed to access the sequence because it is empty.".Localized()) { }
		public EmptySequenceException(string msg, Exception innerException) : base(msg, innerException) { }
	}
}
