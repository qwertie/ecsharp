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
		static readonly Symbol _field = GSymbol.Get("field");
		static readonly Symbol __field = GSymbol.Get("#field");

		// TODO: support "attribute" macros.
		[LexicalMacro("[field] int X { get; set; }; [field x] int X { get; set; }; [field int x] int X { get; set; }", "Create a backing field for a property. In addition, if the body of the property is empty, a getter is added.", "#property", 
			Mode = MacroMode.Passive | MacroMode.Normal)]
		public static LNode BackingField(LNode prop, IMessageSink sink)
		{
			LNode propType, propName, propArgs, body;
			if (prop.ArgCount != 4 || !(body = prop.Args[3]).Calls(S.Braces))
				return null;

			// Look for an attribute of the form [field], [field name] or [field Type name]
			LNode fieldAttr = null, fieldName;
			bool autoType = false;
			int i;
			for (i = 0; i < prop.Attrs.Count; i++) {
				LNode attr = prop.Attrs[i];
				if (attr.IsIdNamed(_field)) {
					fieldAttr = attr;
					break;
				} else if (attr.Calls(S.Var, 2)) {
					LNode fieldVarAttr = null;
					attr = attr.WithoutAttrNamed(__field, out fieldVarAttr);
					if (fieldVarAttr != null && fieldVarAttr.IsId || (autoType = attr.Args[0].IsIdNamed(_field))) {
						fieldAttr = attr;
						break;
					}
				}
			}
			if (fieldAttr == null)
				return null;

			// Extract the type and name of the backing field, if specified
			LNode field = fieldAttr;
			propType = prop.Args[0];
			propName = prop.Args[1];
			propArgs = prop.Args[2];
			if (field.IsId) {
				fieldName = F.Id(ChooseFieldName(Loyc.Ecs.EcsNodePrinter.KeyNameComponentOf(propName)));
				field = F.Call(S.Var, propType, fieldName).WithAttrs(fieldAttr.Attrs);
			} else {
				fieldName = field.Args[1];
				if (fieldName.Calls(S.Assign, 2))
					fieldName = fieldName.Args[0];
			}
			if (autoType)
				field = field.WithArgChanged(0, propType);

			// Construct the new backing field, fill in the property getter and/or setter
			if (body.ArgCount == 0)
				body = body.WithArgs(LNode.Id(S.get));
			LNode newBody = body.WithArgs(body.Args.SmartSelect(stmt =>
			{
				var fieldAccessExpr = fieldName;
				if (propArgs.ArgCount > 0) {
					// Special case: the property has arguments, 
					// e.g. [field List<T> L] T this[int x] { get; set; } 
					//  ==> List<T> L; T this[int x] { get { return L[x]; } set { L[x] = value; } }
					var argList = GetArgNamesFromFormalArgList(propArgs, formalArg =>
						sink.Error(formalArg, "'field' macro expected a variable declaration here"));
					fieldAccessExpr = F.Call(S.IndexBracks, argList.Insert(0, fieldName));
				}
				var attrs = stmt.Attrs;
				if (stmt.IsIdNamed(S.get)) {
					stmt = F.Call(stmt.WithoutAttrs(), F.Braces(F.Call(S.Return, fieldAccessExpr))).WithAttrs(attrs);
					stmt.BaseStyle = NodeStyle.Special;
				}
				if (stmt.IsIdNamed(S.set)) {
					stmt = F.Call(stmt.WithoutAttrs(), F.Braces(F.Call(S.Assign, fieldAccessExpr, F.Id(S.value)))).WithAttrs(attrs);
					stmt.BaseStyle = NodeStyle.Special;
				}
				return stmt;
			}));
			if (newBody == body)
				sink.Warning(fieldAttr, "The body of the property does not contain a 'get;' or 'set;' statement without a body, so no code was generated to get or set the backing field.");

			prop = prop.WithAttrs(prop.Attrs.RemoveAt(i)).WithArgChanged(3, newBody);
			prop.Style &= ~NodeStyle.OneLiner; // avoid collapsing output to one line
			return F.Call(S.Splice, new VList<LNode>(field, prop));
		}

		static Symbol ChooseFieldName(Symbol propName)
		{
			string name = propName.Name;
			char first = name.FirstOrDefault();
			char lower = char.ToLowerInvariant(first);
			if (lower != first)
				name = lower + name.Substring(1);
			return GSymbol.Get("_" + name);
		}
	}
}
