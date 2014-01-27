using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Syntax;
using Loyc.Utilities;

namespace LeMP
{
	/// <summary>Method signature of an LEL simple macro.</summary>
	/// <param name="node">The node that caused the macro to be invoked (includes 
	/// the name of the macro itself, and any attributes applied to the macro)</param>
	/// <param name="rejectReason">If the input does not have a valid form, the
	/// macro rejects it by returning null. When returning null, the macro should
	/// explain the reason for the rejection (including a pattern that the macro 
	/// accepts) via this object.</param>
	/// <returns>A node to replace the original <c>node</c>, or null if this 
	/// macro rejects the input node. Returning null can allow a different macro 
	/// to accept the node instead.</returns>
	/// <remarks>If there are multiple macros in scope with the same name, they 
	/// are <i>all</i> called. Macro expansion succeeds if exactly one macro accepts 
	/// the input. If no macros accept the input, the error message given by each
	/// macro is printed; if multiple macros accept the input, an ambiguity error
	/// is printed.
	/// <para/>
	/// When the macro processor scans an assembly looking for macros, it requires
	/// <see cref="ContainsMacroAttribute"/> on the containing class, and 
	/// <see cref="SimpleMacroAttribute"/> on each macro in the class. The macros 
	/// must be public static methods.
	/// </remarks>
	public delegate LNode SimpleMacro(LNode node, IMessageSink rejectReason);

	/// <summary>Marks a class to be searched for macros.</summary>
	/// <remarks>The method signature of a macro must be <see cref="SimpleMacro"/> and
	/// it must be marked with <see cref="SimpleMacroAttribute"/>.</remarks>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class ContainsMacrosAttribute : Attribute
	{
	}

	/// <summary>Marks a method as an LEL simple macro.</summary>
	/// <remarks>
	/// To be recognized as a macro, the method must be public and static and its 
	/// signature must be <see cref="SimpleMacro"/>. A class will not be automatically
	/// searched for macros unless the class is marked with <see cref="ContainsMacrosAttribute"/>.</remarks>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple=false)]
	public class SimpleMacroAttribute : Attribute
	{
		public SimpleMacroAttribute(string syntax, string description, params string[] names)
			{ Syntax = syntax; Description = description; Names = names; Mode = MacroMode.Normal; }
		public readonly string Syntax;
		public readonly string Description;
		public readonly string[] Names;
		public MacroMode Mode { get; set; }
	}

	/// <summary>Flags that affect the way that <see cref="LeMP.MacroProcessor"/>
	/// uses a SimpleMacro. Unless otherwise specified, these flags only apply when 
	/// the macro accepts the input by returning a non-null result.</summary>
	[Flags]
	public enum MacroMode
	{
		/// <summary>The macro's result (including children) is not processed further.</summary>
		NoReprocessing = 0,
		/// <summary>The macro's result is reprocessed directly (this is the default behavior).</summary>
		Normal = 1,
		/// <summary>The macro's result is not reprocessed, but the result's children are processed.</summary>
		ProcessChildrenAfter = 2,
		/// <summary>The result is pre-processed before calling the macro.</summary>
		ProcessChildrenBefore = 4,
		/// <summary>The macro doesn't change the code. A warning should not be printed when the macro "rejects" the input.</summary>
		Passive = 8,
	}

	// TODO
	public class StockOverloadAttribute : Attribute { }
	// TODO
	public class LowPriorityOverloadAttribute : Attribute { }
}
