using System;

namespace System.Runtime.CompilerServices
{
	[AttributeUsageAttribute(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
	public sealed class ExtensionAttribute : Attribute { }
}
