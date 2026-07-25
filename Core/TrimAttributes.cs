// Polyfills for the trimming/AOT analysis attributes, following the same pattern as
// NullableAttributes.cs in this directory: on target frameworks where the real attribute
// exists in the BCL we compile nothing, and elsewhere we declare an internal copy so that
// source code can carry the annotations unconditionally.
//
// RequiresUnreferencedCodeAttribute was added in .NET 5; RequiresDynamicCodeAttribute in .NET 7.
// The attributes are declared `internal` deliberately: they are compile-time analysis metadata
// and must not become part of any Loyc assembly's public API surface.

#pragma warning disable

namespace System.Diagnostics.CodeAnalysis
{
#if !NET5_0_OR_GREATER
	/// <summary>
	/// Indicates that the specified method requires dynamic access to code that is not referenced
	/// statically, for example through <see cref="System.Reflection"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
	internal sealed class RequiresUnreferencedCodeAttribute : Attribute
	{
		public RequiresUnreferencedCodeAttribute(string message) { Message = message; }

		/// <summary>Gets a message that contains information about the usage of unreferenced code.</summary>
		public string Message { get; }

		/// <summary>Gets or sets an optional URL that contains more information about the method.</summary>
		public string? Url { get; set; }
	}
#endif

#if !NET7_0_OR_GREATER
	/// <summary>
	/// Indicates that the specified method requires the ability to generate new code at runtime,
	/// for example through <see cref="System.Reflection"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
	internal sealed class RequiresDynamicCodeAttribute : Attribute
	{
		public RequiresDynamicCodeAttribute(string message) { Message = message; }

		/// <summary>Gets a message that contains information about the usage of dynamic code.</summary>
		public string Message { get; }

		/// <summary>Gets or sets an optional URL that contains more information about the method.</summary>
		public string? Url { get; set; }
	}
#endif
}
