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

	public partial class StandardMacros
	{
		static readonly Symbol _set = GSymbol.Get("#set");

		[LexicalMacro("Type Name(set Type name) {...}; Type Name(public Type name) {...}", 
			"Automatically assign a value to an existing field, or creates a new "+
			"field with an initial value set by calling the method. This macro is "+
			"activated by attaching one of the following attributes to a method "+
			"parameter: set, public, internal, protected, private, #protectedIn, static, partial.", 
			"#fn", "#cons", Mode = MacroMode.Passive)]
		public static LNode SetOrCreateMember(LNode fn, IMessageSink sink)
		{
			// Expecting #fn(Type, Name, #(args), {body})
			if (fn.ArgCount < 3 || !fn.Args[2].Calls(S.List))
				return null;
			var args = fn.Args[2].Args;
			LNode body = null;
			RVList<LNode> createStmts = RVList<LNode>.Empty;
			RVList<LNode> setStmts = RVList<LNode>.Empty;
			for (int i = 0; i < args.Count; i++) {
				var arg = args[i];
				Symbol a = S.Property;
				Symbol fieldName = null;
				Symbol paramName = null;
				LNode plainArg = null;
				LNode createStmt = null;
				if (arg.Calls(S.Property)) {
					// #property(Type, Name<T>, {...})
					var name = arg.Args[1];
					fieldName = Ecs.EcsNodePrinter.KeyNameComponentOf(name);
					paramName = ChooseArgName(fieldName);
					plainArg = F.Var(arg.Args[0], paramName);
					createStmt = arg;
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
										plainArg = F.Var(type, paramName, defaultValue);
										createStmt = arg;
										// in case of something like "public T arg = value", assume that
										// "= value" represents a default value, not a field initializer.
										if (arg.Args[1].Calls(S.Assign, 2))
											createStmt = arg.WithArgChanged(1,
												arg.Args[1].Args[0]);
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
							return Reject(sink, arg, Localize.From("'{0}': to set or create a field or property, the method must have a body in braces {{}}.", a));
						body = fn.Args[3];
					}

					args[i] = plainArg;
					LNode assignment;
					if (fieldName == paramName)
						assignment = F.Call(S.Assign, F.Dot(F.@this, F.Id(fieldName)), F.Id(paramName));
					else
						assignment = F.Call(S.Assign, F.Id(fieldName), F.Id(paramName));
					setStmts.Add(assignment);
					if (createStmt != null)
						createStmts.Add(createStmt);
				}
			}
			if (body != null) // if this macro has been used...
			{
				var parts = fn.Args;
				parts[2] = parts[2].WithArgs(args);
				parts[3] = body.WithArgs(body.Args.InsertRange(0, setStmts));
				fn = fn.WithArgs(parts);
				if (createStmts.IsEmpty)
					return fn;
				else {
					createStmts.Add(fn);
					return F.Call(S.Splice, createStmts);
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
