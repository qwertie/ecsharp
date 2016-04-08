using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LeMP
{
	public partial class StandardMacros
	{
		// TODO: a macro with the following effect:
		// [fluent]
		// public partial class MyAPI
		// {
		// 	void Foo() { BlahBlah(); }
		// 	void Bar() ==> BlahBlahBlah();
		// 	int Baz() { return _baz; }
		// 	static void StaticMethod() { }
		// }
		//
		// Output:
		// public partial class MyAPI
		// {
		// 	MyAPI Foo() { BlahBlah(); return this; }
		// 	MyAPI Bar() { BlahBlahBlah(); return this; }
		// 	int Baz() { return _baz; }
		// 	static void StaticMethod() { }
		// }
	}
}
