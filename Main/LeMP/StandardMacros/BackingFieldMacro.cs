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

		[SimpleMacro("[field x] int X { get; set; }", "Create a backing field for a property.", "#property", Mode = MacroMode.Passive)]
		public static LNode BackingField(LNode prop, IMessageSink sink)
		{
			LNode type, name, body;
			if (prop.ArgCount != 3 || !(body = prop.Args[2]).Calls(S.Braces))
				return null;

			LNode fieldAttr = null, fieldVarAttr = null;
			LNode fieldName;
			bool autoType = false;
			int i;
			for (i = 0; i < prop.Attrs.Count; i++)
			{
				LNode attr = prop.Attrs[i];
				if (attr.IsIdNamed(_field)
					|| attr.Calls(S.Var, 2) 
						&& ((autoType = attr.Args[0].IsIdNamed(_field)) ||
							(fieldVarAttr = attr.AttrNamed(_field)) != null && fieldVarAttr.IsId))
				{
					fieldAttr = attr;
					break;
				}
			}
			if (fieldAttr == null)
				return null;

			LNode field = fieldAttr;
			type = prop.Args[0];
			if (field.IsId) {
				name = prop.Args[1];
				field = F.Call(S.Var, type, fieldName = F.Id(ChooseFieldName(Ecs.EcsNodePrinter.KeyNameComponentOf(name))));
			} else {
				fieldName = field.Args[1];
				if (fieldName.Calls(S.Assign, 2))
					fieldName = fieldName.Args[0];
			}
			if (autoType)
				field = field.WithArgChanged(0, type);
			if (fieldVarAttr != null)
				field = field.WithoutAttrNamed(_field);

			LNode newBody = body.WithArgs(body.Args.SmartSelect(stmt =>
			{
				var attrs = stmt.Attrs;
				if (stmt.IsIdNamed(S.get)) {
					stmt = F.Call(stmt.WithoutAttrs(), F.Braces(F.Call(S.Return, fieldName))).WithAttrs(attrs);
					stmt.BaseStyle = NodeStyle.Special;
				}
				if (stmt.IsIdNamed(S.set)) {
					stmt = F.Call(stmt.WithoutAttrs(), F.Braces(F.Call(S.Assign, fieldName, F.Id(S.value)))).WithAttrs(attrs);
					stmt.BaseStyle = NodeStyle.Special;
				}
				return stmt;
			}));
			if (newBody == body)
				sink.Write(Severity.Warning, fieldAttr, "The body of the property does not contain a 'get;' or 'set;' statement without a body, so no code was generated to get or set the backing field.");

			prop = prop.WithAttrs(prop.Attrs.RemoveAt(i)).WithArgChanged(2, newBody);
			return F.Call(S.Splice, new RVList<LNode>(field, prop));
		}

		static Symbol ChooseFieldName(Symbol propName)
		{
			string name = propName.Name;
			char first = name.FirstOrDefault();
			char lower;
			if ((lower = char.ToLowerInvariant(first)) != first)
				name = lower + name.Substring(1);
			return GSymbol.Get("_" + name);
		}
	}
}
