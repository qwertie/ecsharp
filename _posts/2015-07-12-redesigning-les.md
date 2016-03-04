---
title: Redesigning LES
layout: post
commentIssueId: 9
---
I proposed Loyc Expression Syntax [version 1](https://github.com/qwertie/LoycCore/wiki/LESv1) to an external group for the first time recently, and did not get a warm reception. 'Non-obvious whitespace rules', 'overbearing semicolon requirements', and 'operator precedence rules that differ from C/JS' were mentioned as pain points. For two weeks I thought and I thought, exploring different options, and while I could not find a perfect solution, I did find a solution that I am happy with.

First of all, I've realized now that I did too much alone: I tried to specify _eveything_ about the language, with too many minor features, from day one, before LES gained any popularity. I realized that I needed some simplifications so that others would be able to understand LES. No one would support a language they did not understand, even if the parser didn't have that many lines of code.

Secondly, I needed to address those concerns - minimize whitespace sensitivity, minimize problems with semicolons, and avoid rocking the boat too much by improving precedence rules.

Here are a couple of things that I decided against. My first plan was described in [issue 3](https://github.com/qwertie/LoycCore/issues/3). It involved having expressions end upon reaching the first `{braced block}`, unless the braced block is followed by comma or semicolon...

    if (x > 0) {...}: else {...}

but this approach turned out to complicate the parser substantially, especially the possibility of using a comma, which is also an expression separator inside argument lists. The colon option is unambiguous, but much uglier. Eventually I realized that an apostrophe was the best option:

    if (x > 0) {...}' else {...}

but I guessed people would think it quite strange to use a string delimiter in this way.

After speaking with a developer friend who was adamant that _any_ whitespace sensitivity was unacceptable (other than accepted whitespace sensitivity, like the difference between `helloworld` and `hello world`, or between `3.0f` and `3.0 f`), I slipped into depression for a couple of days, then came up with a design that eliminated "Python mode" and avoided whitespace sensitivity entirely. I actually committed an implementation at 6:47AM, July 6, but I did so knowing that I wasn't satisfied and would probably change it.

How this version worked was that, instead of "superexpressions", I define the "juxtaposition" operator as an operator with a precedence just below NullDot. It is an expression followed by a sequence of `ids` and `{braces}`, and these additional particles are appended to the argument list of the previous expression (or a new argument list is created, if the previous expression didn't have one). `(parens)` are simply not allowed, which allows this operator to be non-whitespace-sensitive.

For example, you could write `if(x > 0) {...} else {...}`, which is parsed into `if(x > 0, {...}, else, {...})`, or you could write `x = y.z {foo}`, which would be parsed into `x = y.z({foo})`. Under these rules, a statement like `var one = 1.0` is no longer possible, but that's okay, I figured: you can use `one := 1.0` instead (where `:=` is an operator for creating new variables).

This solution actually works quite well, if all we're talking about is executable code. It supports all the usual stuff pretty well:

~~~csharp
if(...) {...};
if(...) {...} else {...};
while(...) {...}
for(..., ..., ...) {...};
return(...);
switch(...) { case(...) {...} };
do {...} while {...};
~~~

The do-while statement is a little awkward, needing braces around the loop condition instead of parentheses, but on the whole, this design seemed quite usuable. And although it requires semicolons after each statement, it is pretty effective at detecting missing semicolons:

~~~csharp
while (x < 100) { x *= 2; }
Foo(x); // Syntax error

do {x++} while {Foo(x)}
x++; // Syntax error

if (c) { c.F(); }
x := 0; // Syntax error

if (c) { c.F(); }
if (x > 0) { return 0 }; // Syntax error
~~~

However, declarative statements that used to have a nice, clean syntax became illegal, and the `new` expression won't work either:

~~~csharp
var x = 0;                        // syntax error
fn square(x::int)::int { x*x };   // syntax error
class Foo(Base, IBase) { ... };   // syntax error
delegate Action();                // syntax error
alias Foo = Food;                 // syntax error
x := (new Class.Name(...) {...}); // syntax error
~~~

Of these, I guess function declarations and `new` expressions bothered me the most. The best solution seemed to be using a colon (`:` being a normal operator)

~~~csharp
fn: square(x::int)::int { x * x };
~~~

The tree structure of this is a bit odd though:

~~~csharp
fn : ((square(x::int))::int)({ x * x }));
~~~

A `new` expression written `` `new` Foo(x) { Prop = Value }`` could also be parsed, but it has a different tree structure than I wanted: `` `new` Foo(x, { Prop = Value })`` instead of `new(Foo(x), {...})`.

It's not that bad, but I've written a lot of LES code already, and the new parser broke all of it, _and_ I'd have to rewrite or change numerous macros, and in the end I'd have a language that just isn't as _pretty_ as the original LES. So I put whitespace sensitivity back on the table. I explored the idea of having two kinds of identifier tokens - a normal identifier, and one followed by a space - but eventually I decided it was better to have two kinds of _left parenthesis_ instead: a normal one, and one _preceded_ by a space.

So I came up with a design I called LES version 2, and I wrote the [new specification](https://github.com/qwertie/LoycCore/wiki/Loyc-Expression-Syntax). Finally I had a solution that I felt was acceptable: yes, it has a whitespace rule, but the rule is very easy to understand. Yes, it requires more semicolons than C, but the parser can detect most missing semicolons, without whitespace rules (although in special cases, a validation postprocessor, or whatever compiler receives the parsed code, would have to detect the error instead.)

LESv2 _looks_ nice; consider this function which is valid as Javascript as well as LES:

    function length(s) {
        if (s == null) {
          return 0;
        } else {
          var len;
          for (len = 0; s[len] != '\0'; len++) {};
          return len;
        };
    };

I overlooked his advice at the time, but my programmer friend noticed that if there were some kind of registry of "keywords", it wouldn't be necessary to have a whitespace rule. But I actually think the whitespace rule is preferable, because it is entirely local: LES does not require a symbol table of any kind, and its parsing rules don't change based on previously-encountered text. There is another possibility that I didn't consider: having a predefined list of reserved words based on popular languages. I think it's pretty cool that LES has no reserved words, but when it comes to syntax, a lot of developers are irrational and cranky. So depending on how much hate I receive about the whitespace rule, I could introduce a predefined list of keywords in exchange for eliminating the whitespace rule (plus, any identifier that starts with `#` (and/or `@`) could be treated as a keyword). But I'll defer this decision to another day. Predefined keywords have an additional advantage that some of them could be designated as "continuators": `else`, `catch`, `finally`, `while`.

Finally, I decided last night to make LES into a superset of JSON, both as a migration path from JSON to LES, and also to stress the fact that LES will work well as a plain-old data format.

This requires three changes: 

1. Use `@[...]` instead of `[...]` for attributes, and `@{...}` instead of `@[...]` for token literals (to avoid changing token literals, I could have used `@(...)` for attributes, but I figured it would be better to use `@[...]` because `[]` does not require the shift key, and token literals are a more obscure, optional feature. I thought about Java-like `@Foo(...)` syntax for attributes, but that approach does not allow an empty attribute list, which is currently permitted as a way of suppressing storage of parentheses in the Loyc tree.)
2. Allow `,` as a separator inside `{...}` (at least if the first token after `{` is a string)
3. Introduce `[list syntax]`.

Then JSON like `{"foo": [[22, 22.2], true], "bar":0}` is also LES. Yay!

New idea
--------
New idea for whitespace agnosticism. Three kinds of sugar:

1. Block-call expression (adds an argument): expr {...}, expr (...) {...}
2. Contextual binary operators: else catch finally where in @anything
3. Top-level expr: Id Expr (`return 0`) - doesn't work if Expr starts with binary op or '('

do for while if unless until switch return break throw goto using let var loop with | else catch finally where in | class struct fn type new case enum event alias foreach import | public private protected internal module