using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

	/// <summary>Marks a method as a LeMP lexical macro.</summary>
	/// <remarks>
	/// To be recognized as a macro, the method must be public and static and its 
	/// signature must be <see cref="LexicalMacro"/>. A class will not be automatically
	/// searched for macros unless the class is marked with <see cref="ContainsMacrosAttribute"/>.</remarks>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple=false)]
	public class LexicalMacroAttribute : Attribute
	{
		/// <summary>LexicalMacroAttribute constuctor.</summary>
		/// <param name="syntax">A string that shows the expected syntax used to call the node.</param>
		/// <param name="description"></param>
		/// <param name="names"></param>
		public LexicalMacroAttribute(string syntax, string description, params string[] names)
			{ Syntax = syntax; Description = description; Names = names; Mode = MacroMode.PriorityNormal; }

		public string Syntax { get; protected set; }
		public string Description { get; protected set; }
		public string[] Names { get; private set; }
		MacroMode _mode;
		public MacroMode Mode {
			get { return _mode; }
			set {
				_mode = value;
				if ((_mode & MacroMode.PriorityMask) == 0)
					_mode |= MacroMode.PriorityNormal;
			}
		}
		public MacroMode Priority { get { return Mode & MacroMode.PriorityMask; } }
	}

	/// <summary>Flags that affect the way that <see cref="LeMP.MacroProcessor"/>
	/// uses a LexicalMacro.</summary>
	[Flags]
	public enum MacroMode
	{
		/// <summary>The macro's result is reprocessed directly (this is the default behavior).</summary>
		Normal = 0,
		/// <summary>The macro's result (including children) is not processed further. 
		/// This flag only takes effect when the macro accepts the input by returning a non-null result.</summary>
		NoReprocessing = 1,
		/// <summary>The macro's result is not reprocessed, but the result's children are processed. 
		/// This flag only takes effect when the macro accepts the input by returning a non-null result.</summary>
		ProcessChildrenAfter = 2,
		/// <summary>The result is pre-processed before calling the macro, and not processed afterward
		/// (if the macro accepts the input by returning a non-null result).</summary>
		ProcessChildrenBefore = 4,
		/// <summary>It is normal for this macro not to change the code, so a warning should not be printed when the macro "rejects" the input by returning null.</summary>
		Passive = 8,
		/// <summary>If this macro is ambiguous with one or more macro of the same priority, this flag blocks the ambiguity error message if all the macros produce equivalent results.</summary>
		AllowDuplicates = 16,
		/// <summary>If this macro succeeds, all nodes after this one in the current attribute or statement/argument list are dropped.</summary>
		/// <remarks>This option may be used by macros that splice together the list of <see cref="IMacroContext.RemainingNodes"/> into their own result.
		/// It is more common, however, to set the <see cref="IMacroContext.DropRemainingNodes"/> property inside the macro.</remarks>
		DropRemainingListItems = 32,
		/// <summary>If this flag is present, the macro can match a plain identifier. By default, only calls can be treated as macros.</summary>
		/// <remarks>This flag does <i>not</i> prevent the macro from matching calls.</remarks>
		[Obsolete("This was renamed to MatchIdentifierOrCall. Consider using MatchIdentifierOnly if applicable.")]
		MatchIdentifier = 64,
		MatchIdentifierOrCall = 64,
		/// <summary>If this flag is present, the macro can match a plain identifier but cannot match calls.</summary>
		MatchIdentifierOnly = 128,
		/// <summary>The macro will be called whenever any kind of literal is encountered
		/// (if its namespace is imported).</summary>
		/// <remarks>When a literal is encountered, it is processed as though the Passive 
		/// flag were present on all macros that use this flag, because no macro should alter 
		/// all literals.</remarks>
		MatchEveryLiteral = 0x1000,
		/// <summary>The macro will be called for every call node
		/// (if its namespace is imported).</summary>
		/// <remarks>When a macro with this flag is called, it is processed as though the 
		/// Passive flag were present.</remarks>
		MatchEveryCall = 0x2000,
		/// <summary>The macro will be called for every identifier node 
		/// (if its namespace is imported).</summary>
		/// <remarks>When a macro with this flag is called, it is processed as though the 
		/// Passive flag were present.</remarks>
		MatchEveryIdentifier = 0x4000,
		/// <summary>Tells LeMP not to mention the physical method that implements the macro.
		/// This improves error message clarity for user-defined macros, all of which share 
		/// a single implementing method.</summary>
		UseLogicalNameInErrorMessages = 0x8000,
		/// <summary>Lowest priority. If this macro is ambiguous with another macro that doesn't have this flag, the results produced by the other macro are used (note: only one priority flag can be used at a time).</summary>
		PriorityFallbackMin = 0x100,
		/// <summary>Low priority. If this macro is ambiguous with another macro that doesn't have this flag nor FallbackMin, the results produced by the other macro are used (note: only one priority flag can be used at a time).</summary>
		PriorityFallback = 0x300,
		/// <summary>Used to order behavior of standard macros.</summary>
		PriorityInternalFallback = 0x400,
		/// <summary>Normal priority (this is the default and does not need to be specified.)</summary>
		PriorityNormal = 0x500,
		/// <summary>Used to order behavior of standard macros.</summary>
		PriorityInternalOverride = 0x600,
		/// <summary>High priority. If this macro is ambiguous with another macro that doesn't have this flag nor OverrideAll, this macro takes precedence (note: only one priority flag can be used at a time).</summary>
		PriorityOverride = 0x700,
		/// <summary>Highest priority. If this macro is ambiguous with another macro that doesn't have this flag, the results produced by this macro are used (note: only one priority flag can be used at a time).</summary>
		PriorityOverrideMax = 0x900,
		/// <summary>For internal use to isolate the priority of a macro.</summary>
		PriorityMask = 0xF00,
	}

	/// <summary>Data returned from <see cref="IMacroContext.AllKnownMacros"/></summary>
	public class MacroInfo : LexicalMacroAttribute
	{
		public MacroInfo(Symbol @namespace, string name, LexicalMacro macro) : this(@namespace, new LexicalMacroAttribute("", "", name), macro) { }
		public MacroInfo(Symbol @namespace, LexicalMacroAttribute a, LexicalMacro macro)
			: base(a.Syntax, a.Description, a.Names != null && a.Names.Length > 0 
				? a.Names : (a.Mode & (MacroMode.MatchEveryCall | MacroMode.MatchEveryLiteral)) != 0
				? EmptyArray<string>.Value : new[] { macro.Method.Name })
		{
			CheckParam.IsNotNull("macro", macro);
			Namespace = @namespace;
			Macro = macro;
			Mode = a.Mode;
		}
		public Symbol Namespace { get; private set; }
		public LexicalMacro Macro { get; private set; }

		/// <summary>Uses reflection to find a list of macros within the specified 
		/// type by searching for (static) methods that
		/// (1) are marked with <see cref="LexicalMacroAttribute"/> and
		/// (2) take no parameters and return a list (IEnumerable) of 
		///     <see cref="MacroInfo"/>. Such methods are called to get macros.
		/// </summary>
		/// <param name="type">The type to search for macros.</param>
		/// <param name="namespace">Optionally overrides the namespace associated 
		/// with macros marked with <see cref="LexicalMacroAttribute"/>. Does not
		/// affect macros returned in a list of <see cref="MacroInfo"/>.</param>
		/// <param name="errorSink">An object in which to send error messages
		/// (<see cref="Severity.Warning"/> is used because unavailability of a macro
		/// should not appear as an error when compiling something with LeMP.)</param>
		/// <param name="instance">An optional object of the same type as <c>type</c>, 
		/// which will be used to bind instance methods. If this is null, only static
		/// methods are discovered.</param>
		/// <returns></returns>
		public static IEnumerable<MacroInfo> GetMacros(Type type, IMessageSink errorSink = null, Symbol @namespace = null, object instance = null)
		{
			List<MacroInfo> results = new List<MacroInfo>();
			errorSink = errorSink ?? MessageSink.Default;
			@namespace = @namespace ?? (Symbol)type.Namespace;

			var flags = BindingFlags.Public | BindingFlags.Static;
			if (instance != null)
				flags |= BindingFlags.Instance;
			foreach(var method in type.GetMethods(flags))
			{
				// First look for [LexicalMacro(...)]
				var lma = method.GetCustomAttributes(typeof(LexicalMacroAttribute), false);
				if (lma.Length != 0) {
					foreach (LexicalMacroAttribute attr in lma) {
						var @delegate = AsDelegate(method);
						Debug.Assert(@delegate != null);
						results.Add(new MacroInfo(@namespace, attr, @delegate));
					}
				} else {
					// If the method returns a list of MacroInfo, try to get macros by calling it.
					try {
						var d = Delegate.CreateDelegate(typeof(Func<IEnumerable<MacroInfo>>), 
							method.IsStatic ? null : instance, method, throwOnBindFailure: false);
						if (d != null)
							results.AddRange(((Func<IEnumerable<MacroInfo>>)d)());
					} catch (Exception e) {
						errorSink.Warning(method.DeclaringType, "Unable to get macros from '{0}': {1}", method.Name, e.Message);
					}
				}
			}

			return results;

			LexicalMacro AsDelegate(MethodInfo method) {
				try {
					return (LexicalMacro)Delegate.CreateDelegate(typeof(LexicalMacro), method.IsStatic ? null : instance, method,
						throwOnBindFailure: true);
				} catch (Exception e) {
					errorSink.Write(Severity.Warning, method.DeclaringType, "Macro '{0}' is uncallable: {1}", method.Name, e.Message);
					return null;
				}
			}
		}
	}
}
