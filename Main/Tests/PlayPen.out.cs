// Generated from PlayPen.ecs by LeMP custom tool. LeMP version: 2.7.1.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Loyc.Collections;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Lexing;
using Loyc.Ecs;
using Loyc;
namespace Samples
{
	partial class PlayPen
	{
		internal static LNode GetName(LNode type)
		{
			{
				LNode name;
				if (type.Calls(CodeSymbols.Class, 3) && (name = type.Args[0]) != null && type.Args[1].Calls(CodeSymbols.AltList) && type.Args[2].Calls(CodeSymbols.Braces) || type.Calls(CodeSymbols.Struct, 3) && (name = type.Args[0]) != null && type.Args[1].Calls(CodeSymbols.AltList) && type.Args[2].Calls(CodeSymbols.Braces) || type.Calls(CodeSymbols.Enum, 3) && (name = type.Args[0]) != null && type.Args[1].Calls(CodeSymbols.AltList) && type.Args[2].Calls(CodeSymbols.Braces))
					return name;
				else
					return null;
			}
		}
	}
}