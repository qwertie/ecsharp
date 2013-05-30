using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;

namespace Loyc.Syntax
{
	public enum TokenKind
	{
		Spacer       = 0x00,
		Id           = 0x10,
		Number       = 0x20,
		OtherLiteral = 0x30,
		Dot          = 0x40,
		Assignment   = 0x50,
		Operator     = 0x60,
		Separator    = 0x70,
		AttrKeyword  = 0x80,
		TypeKeyword  = 0x90,
		OtherKeyword = 0xA0,
		Parens       = 0xB0,
		Bracks       = 0xC0,
		Braces       = 0xD0,
		OtherGroup   = 0xE0,
		Other        = 0xF0,
		KindMask     = 0xF0,
	}

	public interface IToken : ICloneable<IToken>
	{
		SourceRange Range { get; }
		ISourceFile Source { get; }
		
		Symbol Type { get; }
		LNode WithType(Symbol name);

		TokenKind Kind { get; }

		object Value { get; }
		LiteralNode WithValue(object value);

		IListSource<IToken> Children { get; }
	}
}
