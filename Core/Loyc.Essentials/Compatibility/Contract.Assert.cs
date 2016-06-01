using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;

namespace System.Diagnostics.Contracts
{
	#if DotNet3
	public class Contract
	{
		[Conditional("DEBUG")]
		public static void Assert(bool condition)
		{
			if (!condition) throw new InvalidStateException("Assertion failed");
		}

		[Conditional("DEBUG")]
		public static void Assert(bool condition, string userMessage)
		{
			if (!condition) throw new InvalidStateException(userMessage);
		}
	}
	#endif
}
