using System.Reflection;

namespace Loyc.Compatibility
{
#if NetStandard20 || DotNet45

	/// <summary>Defines ITuple in .NET Standard 2.0. System.Runtime.CompilerServices.ITuple
	/// was added to .NET Core 2.0 and .NET Framework 4.7.1, but it wasn't added to .NET 
	/// Standard 2.0. ITuple was added to .NET Standard 2.1, but this requires .NET Core 3.</summary>
	public interface ITuple
	{
		object this[int index] { get; }
		int Length { get; }
	}

	#else

	/// <summary>Ensure `using Loyc.Compatibility` is not an error</summary>
	public interface __ThisEnsuresTheNamespaceIsNotEmpty { }

	#endif
}
