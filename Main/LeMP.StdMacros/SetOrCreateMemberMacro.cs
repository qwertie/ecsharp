using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LeMP
{
	using S = CodeSymbols;
	using Loyc.Ecs;

	public partial class StandardMacros
	{
		static readonly Symbol _set = GSymbol.Get("#set");

		[LexicalMacro("Type Name(set Type name) {...}; Type Name(public Type name) {...}", 
			"Automatically assigns a value to an existing field, or creates a new "+
			"field with an initial value set by calling the method. This macro can "+
			"be used with constructors and methods. This macro is activated by "+
			"attaching one of the following modifiers to a method parameter: "+
			"`set, public, internal, protected, private, protectedIn, static, partial`.", 
			"#fn", "#cons", Mode = MacroMode.Passive | MacroMode.Normal)]
		public static LNode SetOrCreateMember(LNode fn, IMessageSink sink)
		{
			// Expecting #fn(Type, Name, #(args), {body})
			if (fn.ArgCount < 3 || !fn.Args[2].Calls(S.AltList))
				return null;
			var args = fn.Args[2].Args;
			LNode body = null;

			VList<LNode> propOrFieldDecls = VList<LNode>.Empty;
			VList<LNode> setStmts = VList<LNode>.Empty;
			for (int i = 0; i < args.Count; i++) {
				var arg = args[i];
				Symbol a = S.Property;
				Symbol fieldName = null;
				Symbol paramName = null;
				LNode plainArg = null;
				LNode propOrFieldDecl = null;
				if (arg.CallsMin(S.Property, 4)) {
					// #property(Type, Name<T>, {...})
					var name = arg.Args[1];
					fieldName = EcsNodePrinter.KeyNameComponentOf(name);
					paramName = ChooseArgName(fieldName);
					if (arg.ArgCount == 5) { // initializer is Args[4]
						plainArg = F.Var(arg.Args[0], F.Call(S.Assign, F.Id(paramName), arg.Args[4]));
						propOrFieldDecl = arg.WithArgs(arg.Args.First(4));
					} else {
						plainArg = F.Var(arg.Args[0], paramName);
						propOrFieldDecl = arg;
					}
				} else {
					LNode type, defaultValue;
					if (IsVar(arg, out type, out paramName, out defaultValue)) {
						int a_i = 0;
						foreach (var attr in arg.Attrs) {
							if (attr.IsId) {
								a = attr.Name;
								if (a == _set 
									|| a == S.Public || a == S.Internal || a == S.Protected || a == S.Private
									|| a == S.ProtectedIn || a == S.Static || a == S.Partial)
								{
									fieldName = paramName;
									paramName = ChooseArgName(fieldName);
									if (a == _set) {
										plainArg = F.Var(type, paramName, defaultValue).WithAttrs(arg.Attrs.RemoveAt(a_i));
									} else {
										// in case of something like "[A] public params T arg = value", 
										// assume that "= value" represents a default value, not a field 
										// initializer, that [A] belongs on the field, except `params` 
										// which stays on the argument.
										plainArg = F.Var(type, paramName, defaultValue);
										propOrFieldDecl = arg;
										if (arg.Args[1].Calls(S.Assign, 2))
											propOrFieldDecl = arg.WithArgChanged(1,
												arg.Args[1].Args[0]);
										int i_params = arg.Attrs.IndexWithName(S.Params);
										if (i_params > -1)
										{
											plainArg = plainArg.PlusAttr(arg.Attrs[i_params]);
											propOrFieldDecl = propOrFieldDecl.WithAttrs(propOrFieldDecl.Attrs.RemoveAt(i_params));
										}
									}
									break;
								}
							}
							a_i++;
						}
					}
				}
				if (plainArg != null)
				{
					if (body == null)
					{
						if (fn.ArgCount < 4 || !fn.Args[3].Calls(S.Braces))
							return Reject(sink, arg, Localize.Localized("'{0}': to set or create a field or property, the method must have a body in braces {{}}.", a));
						body = fn.Args[3];
					}

					args[i] = plainArg;
					LNode assignment;
					if (fieldName == paramName)
						assignment = F.Call(S.Assign, F.Dot(F.@this, F.Id(fieldName)), F.Id(paramName));
					else
						assignment = F.Call(S.Assign, F.Id(fieldName), F.Id(paramName));
					setStmts.Add(assignment);
					if (propOrFieldDecl != null)
						propOrFieldDecls.Add(propOrFieldDecl);
				}
			}
			if (body != null) // if this macro has been used...
			{
				var parts = fn.Args;
				parts[2] = parts[2].WithArgs(args);
				parts[3] = body.WithArgs(body.Args.InsertRange(0, setStmts));
				fn = fn.WithArgs(parts);
				if (propOrFieldDecls.IsEmpty)
					return fn;
				else {
					propOrFieldDecls.Add(fn);
					return F.Call(S.Splice, propOrFieldDecls);
				}
			}
			return null;
		}

		private static bool IsVar(LNode arg,out LNode type, out Symbol name, out LNode defaultValue)
		{
			type = defaultValue = null;
			name = null;
 			if (arg.Calls(S.Var, 2)) {
				type = arg.Args[0];
				var n = arg.Args[1];
				if (n.IsId) {
					name = n.Name;
					return true;
				} else if (n.Calls(S.Assign, 2) && n.Args[0].IsId) {
					name = n.Args[0].Name;
					defaultValue = n.Args[1];
					return true;
				}
			}
			return false;
		} 
		static Symbol ChooseArgName(Symbol fieldName)
		{
			char first = fieldName.Name.FirstOrDefault();
			if (first == '_' && fieldName.Name.Length > 1)
				return GSymbol.Get(fieldName.Name.Substring(1));
			char lower;
			if ((lower = char.ToLowerInvariant(first)) != first)
				return GSymbol.Get(lower + fieldName.Name.Substring(1));
			
			// NOTE: if the method is static, "this" does not exist so it's not 
			// possible to write "this.name = name;" and "name = name;" won't work, 
			// so the arg name should be different from the field name. 
			// Ignoring that corner case for now.
			return fieldName;
		}
	}
}
