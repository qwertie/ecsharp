using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;

namespace LeMP
{
	/// <summary>Method signature of an LeMP macro.</summary>
	/// <param name="node">The node that caused the macro to be invoked (includes 
	/// the name of the macro itself, and any attributes applied to the macro)</param>
	/// <param name="context">This is a dual-purpose object. Firstly, this object
	/// implements <see cref="IMessageSink"/>. if the input does not have a valid 
	/// form, the macro rejects it by returning null. Before returning null, the 
	/// macro should explain the reason for the rejection (including a pattern that 
	/// the macro accepts) by writinga message to this object. Secondly, this 
	/// object contains additional information including the ancestors of the 
	/// current node and a list of "scoped properties" (see <see cref="IMacroContext"/>.)
	/// </param>
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
	/// <see cref="ContainsMacrosAttribute"/> on the containing class, and 
	/// <see cref="LexicalMacroAttribute"/> on each macro in the class. The macros 
	/// must be public static methods.
	/// </remarks>
	public delegate LNode LexicalMacro(LNode node, IMacroContext context);

	/// <summary>Marks a class to be searched for macros.</summary>
	/// <remarks>The method signature of a macro must be <see cref="LexicalMacro"/> and
	/// it must be marked with <see cref="LexicalMacroAttribute"/>.</remarks>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class ContainsMacrosAttribute : Attribute
	{
	}

	/// <summary>Marks a method as an LEL simple macro.</summary>
	/// <remarks>
	/// To be recognized as a macro, the method must be public and static and its 
	/// signature must be <see cref="LexicalMacro"/>. A class will not be automatically
	/// searched for macros unless the class is marked with <see cref="ContainsMacrosAttribute"/>.</remarks>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple=false)]
	public class LexicalMacroAttribute : Attribute
	{
		public LexicalMacroAttribute(string syntax, string description, params string[] names)
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
		/// <summary>The result is pre-processed before calling the macro, and not processed afterward.</summary>
		ProcessChildrenBefore = 4,
		/// <summary>It is normal for this macro not to change the code, so a warning should not be printed when the macro "rejects" the input by returning null.</summary>
		Passive = 8,
		/// <summary>If this macro is ambiguous with one or more macro of the same priority, this flag blocks the ambiguity error message if all the macros produce the same results.</summary>
		AllowDuplicates = 16,
		/// <summary>If this macro succeeds, all nodes after this one in the current attribute or statement/argument list are dropped.</summary>
		/// <remarks>Tyically this option is used by macros that splice together the list of <see cref="IMacroContext.RemainingNodes"/> into their own result.</remarks>
		DropRemainingListItems = 32,
		/// <summary>Lowest priority. If this macro is ambiguous with another macro that doesn't have this flag, the results produced by the other macro are used (note: only one priority flag can be used at a time).</summary>
		FallbackMin = 0x100,
		/// <summary>Low priority. If this macro is ambiguous with another macro that doesn't have this flag nor FallbackMin, the results produced by the other macro are used (note: only one priority flag can be used at a time).</summary>
		Fallback = 0x300,
		/// <summary>Normal priority (this is the default and does not need to be specified.)</summary>
		NormalPriority = 0x500,
		/// <summary>High priority. If this macro is ambiguous with another macro that doesn't have this flag nor OverrideAll, this macro takes precedence (note: only one priority flag can be used at a time).</summary>
		Override = 0x700,
		/// <summary>Highest priority. If this macro is ambiguous with another macro that doesn't have this flag, the results produced by this macro are used (note: only one priority flag can be used at a time).</summary>
		OverrideMax = 0x900,
		/// <summary>For internal use to isolate the priority of a macro.</summary>
		PriorityMask = 0xF00,
	}
}
