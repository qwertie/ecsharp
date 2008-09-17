// Syntax

namespace Foo {
	class Bar {
		rule Start() = AddExpr? EOF;
		rule AddExpr() = MultExpr (PLUSMINUS MultExpr)*;
		rule MultExpr() = Atom (MULTDIV Atom)*;
		rule Atom() = NUMBER | BROPEN AddExpr BRCLOSE;			
	}
}

