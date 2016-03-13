---
title: "Why don't you use a parser generator?"
layout: post
---

This is my response to ["Why I Don't Use a Parser Generator"](http://mortoray.com/2012/07/20/why-i-dont-use-a-parser-generator/).

The main concerns raised in this post are all valid.... but I think my own parser generator LLLPG can solve all of them, because it's designed to generate a particular style of output code that is meant to resemble hand-written code, and because it isn't a control freak - you can override it when you need to. You still should be able to "think like the generator", but if you write a parser by hand, you will sort of end up thinking like a parser generator anyway. Only less accurately.

"Lexing and Context" - yes, you could introduce context into the lexer when using LLLPG and the `Buffered()` extension method, but one must be cautious if the context can change "anywhere", since lookahead operations obviously trigger lexing (you have to be careful to change context in the parser before using lookahead; doing this requires some intuition of how the generator works).

"Shift/Reduce and Grammar Conflicts" - Since LLLPG is LL(k) there are no shift/reduce errors. I consider ambiguity to be a normal and expected part of real-life grammars, so ambiguity is handled explicitly using the greedy or nongreedy keywords, zero-width assertions, and the priority operator `/`. (Note that PEG-based parser generators also solve this issue.)

"Syntax Tree" - LLLPG doesn't have automatic tree generation, so you construct trees the same as you would in a hand-written parser. A language-agnostic syntax tree ([`LNode`](https://github.com/qwertie/LoycCore/wiki/Loyc-trees)) is provided, but currently you still have to construct it by hand.

"Mixed Code" - LLLPG's output is designed to be relatively easy to follow. Since it does use a "Mixed Code" system, the grammar can get cluttered, but IMO it's easier to follow than a hand-written parser because it is still much more concise. Let me show you what I mean:

~~~csharp
@[private] rule Expr(context::Precedence)::LNode @{
	{Debug.Assert(context.CanParse(EP.Prefix));}
	{prec::Precedence;}
	e:PrefixExpr
	greedy
	[	// Infix operator
		&{context.CanParse(prec=InfixPrecedenceOf($LA))}
		op:( "**"|"*"|"/,%"|"+"|"-"|"~"|".."|"<"|">"|"<=,>="|"==,!="
		   | "&"|"|"|"^"|"&&"|"||,^^"|"??"|"="|"??="|"=>"|TT.BQString|TT.@in )
		rhs:Expr(prec)
		{e = SetOperatorStyle(F.Call(op.Value -> Symbol, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex));}
	|	// 'is', 'as', or EC# 'using' cast (special case: #usingCast instead of #using)
		&{context.CanParse(prec=InfixPrecedenceOf($LA))}
		op:(TT.@is | TT.@as | TT.@using)
		rhs:DataType(true)
		{var opSym = op.Type() == TT.@using ? S.UsingCast : (op.Value -> Symbol);}
		{e = SetOperatorStyle(F.Call(opSym, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex));}
		FinishPrimaryExpr(ref e)
	|	// Shift operators (two LT or GT in a row)
		&{context.CanParse(EP.Shift)}
		&{LT($LI).EndIndex == LT($LI+1).StartIndex} // no spaces between them
		(	"<" "<" rhs:=Expr(EP.Shift)
			{e = SetOperatorStyle(F.Call(S.Shl, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex));}
		|	">" ">" rhs:=Expr(EP.Shift)
			{e = SetOperatorStyle(F.Call(S.Shr, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex));}
		)
	|	// Conditional operator
		&{context.CanParse(EP.IfElse)}
		"?" then:Expr(ContinueExpr) ":" else:Expr(EP.IfElse)
		{e = SetOperatorStyle(F.Call(S.QuestionMark, e, then, @else, e.Range.StartIndex, else.Range.EndIndex));}
	]*
	{return e;}
};
~~~

Is this grammar for C# expressions a little bit messy? Is it a little bit complicated? Well yes, yes it is. It might take a minute to figure out what's going on. You might need to [read about LLLPG](/lllpg) first. It's also not written in the usual language (this is LES code, not Enhanced C# code, because, of course, it was written before the EC# parser existed). But on the other hand, _it also fits on the screen_. If you wrote this by hand, it would be three or four times as long.

"inline assembly, which has an entirely different syntax": That kind of thing is easy with LLLPG. You can put both in the same lexer (easy), or switch to an entirely separate lexer (possible).

"Getting location information is a hassle": that's more of a problem in LALR(1) generators rather than LL(k) generators. LLLPG can say which tokens it expected and where it expected them, and custom error branches can easily get the line & column numbers. Any other hassles you encounter are ones you'd probably have in a hand-written parser too.

The OP did not mention performance, but may also a good reason to choose hand-written over a parser generator. I think you'll find LLLPG parsers tend to have good performance if you avoid syntactic predicates where possible. And if you need more speed, it is often possible to optimize the output. The main issue here is that any poor performance is hidden until you actually profile it or compare with an equivalent third-party parser, because you might not get an _intuition_ about performance just by writing the grammar.

Finally, if you do have trouble accomplishing what you want with LLLPG, just ask me. I'm here to help.

Today I've decided to actually "turn off" my parser generator in one especially complex non-LL(k) part of my C# parser, in order to optimize it (and, admittedly, to speed up the 10+ seconds LLLPG currently uses to analyze the grammar). This does require some skill, but it's also a testament to the design of LLLPG - a design that embraces, rather than rejects, developer control.

