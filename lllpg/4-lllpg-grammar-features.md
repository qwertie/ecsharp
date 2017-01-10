---
title: "4. LLLPG grammar features"
layout: article
date: 30 May 2016
toc: true
---

This is the biggest article, since there are lots of features & facts to talk about!

## Learning LLLPG features, with numbers

So you still want to use LLLPG after learning about the competition? Phew, what a relief! Okay, let's parse some numbers. Remember that code from the last article?

~~~csharp
public rule Tokens @{ Token* };
public rule Token  @{ Float | Id | ' ' };
token Float        @{ '0'..'9'* '.' '0'..'9'+ };
token Id           @{ IdStart IdCont* };
rule  IdStart      @{ 'a'..'z' | 'A'..'Z' | '_' };
rule  IdCont       @{ IdStart | '0'..'9' };
~~~

I will explain the difference between `rule` and `token` later. It doesn't matter yet; just use `token` for defining token rules (like `Float`, `Int` and `Id`) and you'll be fine.

Now, since `Float` and `Int` are ambiguous in LL(k), here's how I suggested combining them into a single rule:

~~~csharp
token Number       @{ '.' '0'..'9'+
                    | '0'..'9'+ ('.' '0'..'9'+)? };
~~~

That's arguably the cleanest solution, but there are others, and the alternatives are really useful for learning about LLLPG! First of all, one solution that looks simpler is this one:

~~~csharp
token Number      @{ '0'..'9'* ('.' '0'..'9'+)? };
~~~

But now you'll have a problem, because this matches an empty input, or it matches "hello" without consuming any input. Therefore, LLLPG will complain that Token is "nullable" and therefore must not be used in a loop (see `Tokens`). After all, if you call `Number` in a loop and it doesn't match anything, you'll have an infinite loop which is very bad. Below, you'll learn how to avoid that.

### Zero-width assertions: syntactic predicates

You can actually prevent it from matching an empty input as follows:

~~~csharp
token Number @{ &('0'..'9'|'.')
                '0'..'9'* ('.' '0'..'9'+)? };
~~~

Here I have introduced the _zero-width assertion_ or ZWA, also called an _and-predicate_ because it uses the "and" sign (&). There are two kinds of and-predicates, which are called the "syntactic predicate" and the "semantic predicate". This one is a _syntactic predicate_, which means that it tests for syntax, which means it tests a grammar fragment starting at the current position. And since it's a zero-width assertion, it does not consume any input (more precisely, it consumes input and then backtracks to the starting position, regardless of success or failure). and-predicates have two forms, the positive form `&` which checks that a condition is true, and the negative form `&!` which checks that a condition is false.

So, `&('0'..'9'|'.')` means that the number must start with `'0'..'9'` or `'.'`. Now `Number()` cannot possibly match an empty input. Unfortunately, LLLPG is not smart enough to _know_ that it cannot match an empty input; it does not currently analyze and-predicates **at all** (it merely runs them), so it doesn't understand the effect caused by `&('0'..'9'|'.')`. Consequently it will still complain that `Token` is nullable even though it isn't. Hopefully this will be fixed in a future version, when I or some other smart cookie has time to figure out how to perform the analysis.

A syntactic predicate causes two methods to be generated to handle it:

~~~csharp
private bool Try_Number_Test0(int lookaheadAmt)
{
  using (new SavePosition(this, lookaheadAmt))
    return Number_Test0();
}
private bool Number_Test0()
{
  if (!TryMatchRange('.', '.', '0', '9'))
    return false;
  return true;
}
~~~

The first method assumes the existance of a data type called `SavePosition` (as usual, you can define it yourself or inherit it from a base class that I wrote.) `SavePosition`'s job is to:

1. Save the current `InputPosition`, and then restore that position when `Dispose()` is called
2. Increment `InputPosition` by `lookaheadAmt` (which is often zero, but not always), where `0 <= lookaheadAmt < k`.

The `Try_` method is used to start a syntactic predicate and then backtrack, regardless of whether the result was true or false. The second method decides whether the expected input is present at the current input position. Syntactic predicates also assume the existence of `TryMatch` and (for lexers) `TryMatchRange` methods, which must return true (and advance to the next the input position) if one of the expected characters (or tokens) is present, or false if not.

Here's the code for Number() itself:

~~~csharp
void Number()
{
    int la0, la1;
    Check(Try_Number_Test0(0), "[.0-9]");
    for (;;) {
        la0 = LA0;
        if (la0 >= '0' && la0 <= '9')
            Skip();
        else
            break;
    }
    la0 = LA0;
    if (la0 == '.') {
        la1 = LA(1);
        if (la1 >= '0' && la1 <= '9') {
            Skip();
            Skip();
            for (;;) {
                la0 = LA0;
                if (la0 >= '0' && la0 <= '9')
                    Skip();
                else
                    break;
            }
        }
    }
}
~~~

`Check()`'s job is to print an error if the ZWA is not matched. If the `Number` rule is marked `private`, and the rule(s) that call `Number()` have already verified that `Try_Number_Test0` is true, then for efficiency, the call to `Check()` will be eliminated by prematch analysis (mentioned in [part 2](2-simple-examples.html)).

### Zero-width assertions: semantic predicates

Moving on now, another approach is:

~~~csharp
token Number @{ {bool dot=false;}
                ('.' {dot=true;})?
                '0'..'9'+ (&{!dot} '.' '0'..'9'+)?
              };
~~~

Here I use the other kind of ZWA, the _semantic predicate_, to test whether dot is false (`&{!dot}`, which can be written equivalently as `&!{dot}`). `&{expr}` simply specifies a condition that must be true in order to proceed; it is normally used to resolve ambiguity between two possible paths through the grammar. Semantic predicates are a distinctly simpler feature than syntactic predicates and were implemented first in LLLPG. They simply test the user-defined expression during prediction.

So, here I have created a "dot" flag which is set to "true" if the first  character is a dot. The sequence `'.' '0'..'9'+` is only allowed if the "dot" flag has not been set. This approach works correctly; however, you must exercise caution  when using `&{...}` because `&{...}` blocks may execute earlier than you might expect them to; this is explained below.

Here's the code generated for this version of Number:

~~~csharp
void Number()
{
    int la0, la1;
    bool dot = false;
    la0 = LA0;
    if (la0 == '.') {
        Skip();
        dot = true;
    }
    MatchRange('0', '9');
    for (;;) {
        la0 = LA0;
        if (la0 >= '0' && la0 <= '9')
            Skip();
        else
            break;
    }
    la0 = LA0;
    if (la0 == '.') {
        if (!dot) {
            la1 = LA(1);
            if (la1 >= '0' && la1 <= '9') {
                Skip();
                Skip();
                for (;;) {
                    la0 = LA0;
                    if (la0 >= '0' && la0 <= '9')
                        Skip();
                    else
                        break;
                }
            }
        }
    }
}
~~~

The expression inside `&{...}` can include the "substitution variables" `$LI` and `$LA`, which refer to the current lookahead index and the current lookahead value; these are useful if you want to run a test on the input character. For example, when if you want to detect _letters_, you might write:

~~~csharp
rule Letter @{ 'a'..'z' | 'A'..'Z' };
token Word @{ Letter+ };
~~~

but this doesn't detect _all_ possible letters; there are áĉçèntéd letters to worry about, grΣεk letters, Russiaи letters and so on. I've been supporting these other letters with a semantic and-predicate:

~~~csharp
rule Letter @{ 'a'..'z' | 'A'..'Z'| &{[Hoist] char.IsLetter((char) $LA)} 0x80..0xFFFC };
[FullLLk] token Word @{ Letter+ };
~~~

`0x80..0xFFFC` denotes all the non-ASCII characters supported by a .NET char. `$LA` will be replaced with the appropriate lookahead token, which is most often `LA0`, but not always. 

The `[Hoist]` marker allows this check to be copied (in other words, "hoisted") into other rules, specifically the `Word` rule:

~~~csharp
void Word()
{
  int la0;
  Letter();
  for (;;) {
    la0 = LA0;
    if (la0 >= 'A' && la0 <= 'Z' || la0 >= 'a' && la0 <= 'z')
      Letter();
    else if (la0 >= 128 && la0 <= 65532) {
      la0 = LA0;
      if (char.IsLetter((char) la0))
        Letter();
      else
        break;
    } else
      break;
  }
}
~~~

This can occur when one rule needs to use the and-predicate to decide whether to call another rule. ANTLR calls this "hoisting", so that's what I call it too: the predicate was _hoisted_ from `Letter` to `Word`. (In this particular case I had to add `FullLLk` to make it work this way; more about that in a future article.)

Originally, copying and-predicates across rules was the default behavior for both kinds of predicates (syntactic and semantic). However, semantic predicates may refer to local variables, so hoisting by default can produce invalid output. Therefore, the default has been changed to hoist syntactic predicates but not semantic predicates. You can add the attribute `[Local]` or `[Hoist]` (right after `&{`) to choose whether a semantic predicate should be hoisted.

**Note**: Currently the `[Local]` and `[Hoist]` flags are implemented only for `&{semantic}` predicates, not `&(syntactic)` predicates, because there is no syntax defined for attaching options to the latter.

Here's another example, in which hoisting cannot happen:

~~~csharp
token Number  @{ {bool dot=false;}
                 ('.' {dot=true;})?
                 '0'..'9'+ (&{!dot} '.' '0'..'9'+)?
               };
~~~

While LLLPG is analyzing other rules, it acts as if `&{!dot}` doesn't appear in `Number`.

### Gates

Here's one final technique:

~~~csharp
token Number  @{ '.'? '0'..'9' =>
                 '0'..'9'* ('.' '0'..'9'+)? };
~~~

This example introduces a feature that I call "the gate". The grammar fragment `('.'? '0'..'9'+)` before the gate operator `=>` is not actually used by `Number` itself, but it can be used by the caller to decide whether to invoke the rule.

A gate is an advanced yet simple mechanism to alter the way prediction works. Recall that parsing is a series of prediction and matching steps. First the parser decides what path to take next, which is called "prediction", then it matches based on that decision. Normally, prediction and matching are based on the **same information**. However, a gate `=>` causes **different information** to be given to prediction and matching. The left-hand side of the gate is used for the purpose of prediction analysis; the right-hand side is used for matching.

The decision of whether or not `Token` will call the `Number` rule is a prediction decision, therefore it uses the left-hand side of the gate. This ensures that the _caller_ will not believe that `Number` can match an empty input. When code is generated for `Number` itself, the left-hand side of the gate is ignored because it is not part of an "alts" (i.e. the gate expression is not located in a loop or within a list of alternatives separated by `|`, so no _prediction decision_ is needed). Instead, `Number` just runs the matching code, which is the right-hand side, `'0'..'9'* ('.' '0'..'9'+)?`.

Gates are a way of lying to the prediction system. You are telling it to expect a certain pattern, then saying "no, no, match this other pattern instead." Gates are rarely needed, but they can provide simple solutions to certain tricky problems. In this case, the problem is that 

    '0'..'9'* ('.' '0'..'9'+)?
    
is nullable, so it can match a non-numeric input simply by doing nothing. The gate ensures that `Token` doesn't call the rule on a non-numeric input, basically by _lying_ and saying "this rule starts with `'.'? '0'..'9'`, so don't call it if the input is anything else".

Here's the code generated for `Number`, but note that `'0'..'9'* ('.' '0'..'9'+)?` (without the gate) would produce exactly the same code.

~~~csharp
void Number()
{
    int la0, la1;
    for (;;) {
        la0 = LA0;
        if (la0 >= '0' && la0 <= '9')
            Skip();
        else
            break;
    }
    la0 = LA0;
    if (la0 == '.') {
        la1 = LA(1);
        if (la1 >= '0' && la1 <= '9') {
            Skip();
            Skip();
            for (;;) {
                la0 = LA0;
                if (la0 >= '0' && la0 <= '9')
                    Skip();
                else
                    break;
            }
        }
    }
}
~~~

Gates can also be used to produce nonsensical code, e.g.

~~~csharp
   // 'A' => 'Q'
   la0 = LA0;
   if (la0 == 'A')
     Match('Q');
~~~

But don't do that.

Please note that gates, unlike syntactic predicates, **do not** provide unlimited lookahead. For example, if k=2, the characters `'c' 'd'` in `('a' 'b' 'c' 'd' => ...)` will not have any effect.

The gate operator `=>` has higher precedence than `|`, so `a | b => c | d` is parsed as `a | (b => c) | d`.

One more thing, hardly worth mentioning. There are actually two gate operators: the normal one `=>`, and the "_equivalence gate_" `<=>`. The difference between them is the follow set assigned to the left-hand side of the gate. A normal gate `=>` has a "false" follow set of `_*` (anything), and ambiguity warnings that involve this follow set are suppressed (the follow set of the right hand side is computed normally, e.g. in `(('a' => A) 'b')` the follow set of `A` is `'b'`). The "equivalence gate" `<=>` tells LLLPG not to replace the follow set on the left-hand side, so that both sides have the same follow set. It only makes sense to use the equivalence gate if

1. the left-hand side and right-hand side always have the same length,
2. both sides are short, so that the follow set of the gate expression `P => M` can affect prediction decisions.

You almost always what the normal gate. As of January 2017, I have used `=>` dozens of times and `<=>` just once.

### More about and-predicates

I was saying something about and-predicates running earlier than you might expect. Consider this example:

~~~csharp
bool flag = false;
public rule Paradox @{ {flag = true;} &{flag} 'x' / 'x' };
~~~

Here I've introduced the "`/`" operator. It behaves identically to the "`|`" operator, but has the effect of suppressing warnings about ambiguity between the two branches (both branches match `'x'` if `flag == true`, so they are certainly ambiguous).

What will the value of `flag` be after you call `Paradox()`? Since both branches are the same (`'x'`), the only way LLLPG can make a decision is by testing the and-predicate `&{flag}`. But the actions `{flag=false;}` and `{flag=true;}` execute _after_ prediction, so `&{flag}` actually runs first even though it appears to come after `{flag=true;}`. You can clearly see this when you look at the actual generated code:

~~~csharp
bool flag = false;
public void Paradox()
{
  if (flag) {
    flag = true;
    Match('x');
  } else
    Match('x');
}
~~~

What happened here? Well, LLLPG doesn't bother to read `LA0` at all, because it won't  help make a decision. So the usual prediction step is replaced with a test of the and-predicate `&{flag}`, and then the matching code runs (`{flag = true;} 'x'` for the left branch and `'x'` for the right branch).

This example will give the following warning: "It's poor style to put a code block {} before an and-predicate &{} because the and-predicate normally runs first."

In a different sense, though, and-predicates might run after you might expect. Let's look again at the code for this `Number` rule from earlier:

~~~csharp
token Number  @{ {dot::bool=false;}
                 ('.' {dot=true;})?
                 '0'..'9'+ (&{!dot} '.' '0'..'9'+)?
               };
~~~

The generated code for this rule is:

~~~csharp
void Number()
{

  la0 = LA0;
  if (la0 == '.') {
    if (!dot) {
      la1 = LA(1);
      if (la1 >= '0' && la1 <= '9') {
        Skip();
        Skip();
        for (;;) {
          la0 = LA0;
          if (la0 >= '0' && la0 <= '9')
            Skip();
          else
            break;
        }
      }
    }
  }
}
~~~

Here I would draw your attention to the way that `(&{!dot} '.' '0'..'9'+)?` is handled: first LLLPG checks `if (la0 == '.')`, then `if (!dot)` afterward, even though `&{!dot}` is written first in the grammar. Another example shows more specifically how LLLPG behaves:

~~~csharp
token Foo @{ (&{a()} 'A' &{b()} 'B')? };

void Foo()
{
  int la0, la1;
  la0 = LA0;
  if (la0 == 'A') {
    if (a()) {
      la1 = LA(1);
      if (la1 == 'B') {
        if (b()) {
          Skip();
          Skip();
        }
      }
    }
  }
}
~~~

First LLLPG tests for `'A'`, then it checks `&{a()}`, then it tests for `'B'`, and finally it checks `&{b()}`; it is as if the and-predicates are being "bumped" one position to the right. Actually, I decided that all zero-width  assertions should work this way for the sake of performance. To understand this, consider the `Letter` and `Word` rules from earlier:

~~~csharp
rule Letter @{ 'a'..'z' | 'A'..'Z'| &{char.IsLetter($LA -> char)} 0x80..0xFFFC };
[FullLLk] token Word @{ Letter+ };
~~~

In the code for `Word` you can see that `char.IsLetter` is called after the tests on LA0:

~~~csharp
    if (la0 >= 'A' && la0 <= 'Z' || la0 >= 'a' && la0 <= 'z')
      Letter();
    else if (la0 >= 128 && la0 <= 65532) {
      la0 = LA0;
      if (char.IsLetter((char) la0))
        Letter();
      else
        break;
    } else
      break;
~~~

And this makes sense: `char.IsLetter` is expected to be relatively expensive because (if nothing else) it can't be inlined. And if it were called first, there would be little point in testing for `'a'..'z' | 'A'..'Z'` at all. It makes even more sense in the larger context of a "Token" rule like this one:

~~~csharp
[FullLLk] rule Token @{ Spaces / Word / Number / Punctuation / Comma / _ };
~~~

The Token method will look something like this (some newlines removed for brevity):

~~~csharp
void Token()
{
  int la0, la1;
  la0 = LA0;
  switch (la0) {
  case 't':  case ' ':
    Spaces();
    break;
  case '.':
    {
      la1 = LA(1);
      if (la1 >= '0' && la1 <= '9')
        Number();
      else
        Punctuation();
    }
    break;
  case '0':  case '1':  case '2':  case '3':  case '4':
  case '5':  case '6':  case '7':  case '8':  case '9':
    Number();
    break;
  case '!':  case '#':  case '$':  case '%':  case '&':
  case '*':  case '+':  case ',':  case '-':  case '/':
  case '<':  case '=':  case '>':  case '?':  case '@':
  case '\': case '^':  case '|':
    Punctuation();
    break;
  default:
    if (la0 >= 'A' && la0 <= 'Z' || la0 >= 'a' && la0 <= 'z')
      Word();
    else if (la0 >= 128 && la0 <= 65532) {
      la0 = LA0;
      if (char.IsLetter((char) la0))
        Word();
      else
        MatchExcept();
    } else
      MatchExcept();
    break;
  }
}
~~~

In this example, clearly it makes more sense to examine LA0 before checking `&{char.IsLetter(...)}`. If LLLPG invoked the and-predicate first, the code would have the form:

~~~csharp
void Token()
{
  int la0, la1;
  la0 = LA0;
  if (char.IsLetter((char) la0)) {
    switch (la0) {
      ...
    }
  } else {
    switch (la0) {
      ...
    }
  }
}
~~~

The code of `Token` would be much longer, and slower too, since we'd call  `char.IsLetter` on every single input character, not just the ones in the  Unicode range `0x80..0xFFFC`. Clearly, then, as a general rule it's good that LLLPG tests the character values before the ZWAs.

In fact, I am now questioning whether the tests should be interleaved. As you've seen, it currently will test the character/token at position 0, then ZWAs at position 0, then the character/token at position 1, then ZWAs at position 1. This seemed like the best approach when I started, but in the EC# parser this ordering produced a `Stmt` (statement) method that was 3122 lines long (the original `rule` is just 58 lines), which is nearly half the LOCs of the entire parser; it looks like testing LA(0) and LA(1) before any ZWAs might work better, for that particular rule anyway.

### Underscore and tilde ###

The underscore `_` means "match any terminal", while `~(X|Y)` means "match any terminal except `X` or `Y`". The next section has an example that uses both.

In `~(X|Y)`, X and Y _must_ be terminals (if X and/or Y are non-terminals, consider using something like `&!(X|Y) _` instead.)

A subtle point about `~(...)` and `_` is that both of them exclude `EOF` (`-1` in a lexer). Thus `~X` really means "anything except `X` or `EOF`"; and `~(~EOF)` does not represent `EOF`, it represents the empty set (which, as far as I know, is completely useless). By the way, `~` causes LLLPG to use the `MatchExcept()` API, which LLLPG _assumes_ will not match `EOF`. So for `~(X|Y)`, LLLPG generates `MatchExcept(X, Y)` which must be equivalent to `MatchExcept(X, Y, EOF)`.

### Saving inputs ###

LLLPG recognizes five operators for "assigning" a token or return value to a variable: `:`, `=`, `:=`, `+=` and `+:`.

- `x:=Foo`: create a variable `x` in the current scope with `Foo` as its value (i.e. `var x = Foo();` if `Foo` is a rule). Note: alternatives (`a | b | c`), optional elements (`?`), and loops (`*` `+`) cause new lexical scopes to be created in the form of `if`, `for`, `switch` or `do-while` statements.
- `x=Foo`: set an existing variable `x` to the value of `Foo` 
- `x+=Foo`: add the value of `Foo` to the existing list variable `x`, e.g. by calling `x.Add(Foo())`, if `Foo` is a rule)
- `x:Foo`: create a variable called `x` of the appropriate type at the beginning of the method and set `x` to it here. Because the variable is created separately, LLLPG must "guess" the correct data type for the variable. If `Foo` is a token or character, use the `terminalType` code-generation option to control the declared type of `x` (e.g. `LLLPG(parser(terminalType: Token))`). If you use the label `x` in more than once place, LLLPG will create only a single (non-list) variable called `x`.
- `x+:Foo`: create a _list_ variable called `x` of the appropriate type at the beginning of the method, and add the value of `Foo` to the list (i.e. `x.Add(Foo())`, if `Foo` is a rule). By default the list will have type `List<T>` (where `T` is the appropriate type), and you can use the `listInitializer` option to change the list type globally (e.g. `LLLPG(parser(listInitializer: IList<T> _ = new DList<T>()))`, if you prefer [DList](http://core.loyc.net/collections/dlist.html))

This table how code is generated for these operators:

<table  border="1" width="640px">
<tr>
<th>Operator</th>
<th>Example</th>
<th>Generated code for terminal</th>
<th>Generated code for nonterminal</th>
</tr>
<tr>
<td><code>=</code></td>
<td><code>x=Foo</code></td>
<td><code>x = Match(Foo);</code></td>
<td><code>x = Foo();</code></td>
</tr>
<tr>
<td><code>:=</code></td>
<td><code>x:=Foo</code></td>
<td><code>var x = Match(Foo);</code></td>
<td><code>var x = Foo();</code></td>
</tr>
<tr>
<td><code>:</code></td>
<td><code>x:Foo</code></td>
<td><code>// Use with `terminalType: Token`
     <br/>// Output at top of method:
     <br/>Token x = default(Token);
     <br/>// output in-place:
     <br/>x = Match(Foo); </code></td>
<td><code>// RetType refers to Foo's return type
     <br/>// Output at top of method:
     <br/>RetType x = default(RetType);
     <br/>// Output in-place:
     <br/>x = Foo();</code></td>
</tr>
<tr>
<td><code>+=</code></td>
<td><code>lst+=Foo</code></td>
<td><code>lst.Add(Match(Foo));</code></td>
<td><code>lst.Add(Foo());</code></td>
</tr>
<tr>
<td><code>+:</code></td>
<td><code>lst+:Foo</code></td>
<td><code>// Use with `terminalType: Token`
     <br/>// Output at top of method:
     <br/>List&lt;Token> x = new List&lt;Token>;
     <br/>// output in-place:
     <br/>lst.Add(Match(Foo)); // later</code></td>
<td><code>// RetType refers to Foo's return type
     <br/>// Output at top of method:
     <br/>List&lt;RetType> x = new List&lt;RetType>;
     <br/>// Output in-place:
     <br/>lst.Add(Foo());</code></td>
</tr>
</table>

You can match one of a set of terminals, for example `x:=('+'|'-'|'.')` generates code like `var x = Match('+', '-', '.')` (or `var x = Match(set)` for some `set` object, for large sets). However, currently LLLPG does not support matching a list of nonterminals, e.g. `x:=(A()|B())` is not supported.

In LLLPG 1.3.2 I added a feature where you would write simply `Foo` instead of `foo:=Foo` and then write `$Foo` in code later, which _retrospectively_ saves the value returned from `Foo` in an "anonymous" variable. For example, instead of writing code like this:

~~~csharp
private rule LNode IfStmt() @{
    {LNode els = null;}
    t:=TT.If "(" cond:=Expr ")" then:=Stmt 
    greedy[TT.Else els=Stmt]?
    {return IfNode(t, cond, then, els);}
};
~~~

It would be written like this instead:

~~~csharp
private rule LNode IfStmt() @{
    TT.If "(" Expr ")" Stmt 
    greedy[TT.Else els:Stmt]?
    {return IfNode($(TT.If), $Expr, $Stmt, els);}
};
~~~

This makes the grammar look less cluttered. Note that if the optional branch is skipped, the variable `els` ends up with its default value (`null`); the same would happen with `$Expr`, if `Expr` were optional.

### Automatic Value Saver ###

Often you need to store stuff in variables, and in LLLPG 1.1 this was inconvenient because you had to manually create variables to hold stuff. Now LLLPG can create the variables for you. Consider this parser for integers:

~~~csharp
/// Usage: int num = new Parser("1234").ParseInt();
public class Parser : BaseLexer {
  /// Note: a string converts implicitly to UString
  public Parser(UString s) : base(s) {}
  
  LLLPG (lexer(terminalType: int));
  
  public token int ParseInt() @{
    ' '*
    (neg:'-')?
    ( digit:='0'..'9' {$result = 10*$result + (digit - '0');} )+ 
    EOF
    {if (neg != 0) $result = -$result;}
  };
}
~~~

The label `neg:'-'` causes LLLPG to create a variable at the beginning of the method (`int neg = 0`), and to assign the result of matching to it (`digit = MatchAny()`). The type of the variable is controlled by the `terminalType: X` option, but the default for a lexer is `int` so it wasn't needed in this example.

That's different from the existing syntax `digit:='0'..'9'`, in that `:` creates a variable at the _beginning_ of the method, whereas `:=` creates a variable in the current scope (`var digit = MatchRange('0', '9');`). In either case, inside action blocks, LLLPG will recognize the named label `$neg` or `$digit` and replace it with the actual variable name, which is simply `neg` or `digit`.

This example also uses `$result`, which causes LLLPG to create a variable called `result` with the same return type as the method (in this case `int`), returning it at the end. So the generated code looks like this:

~~~csharp
public int ParseInt()
{
    int la0;
    int neg = 0;
    int result = 0;
    
    ... parsing code ...
    
    if (neg)
        result = -result;
    return result;
}
~~~

You don't even have to explicitly apply labels. The above rule could be written like this instead: 

~~~csharp
public token int ParseInt() @{
    ' '*
    ('-')?
    ( '0'..'9' {$result = 10*$result + ($('0'..'9') - '0');} )+ 
    EOF
    {if ($'-' != 0) $result = -$result;}
};
~~~

In this version I removed the labels `neg` and `digit`, instead referring to `$'-'` and `$('0'..'9')` in my grammar actions. This makes LLLPG create two variables to represent the value of `'-'` and `'0'..'9'`:

~~~csharp
int ch_dash = 0;
int ch_0_ch_9 = 0;
...
if (la0 == '-')
    ch_dash = MatchAny();
ch_0_ch_9 = MatchRange('0', '9');
...
~~~

It's as if I had used `ch_dash:'-'` and `ch_0_ch_9:'0'..'9'` in the grammar.

Last but not least, you can use the (admittedly weird-looking) `+:` operator to add stuff to a list. For example:

~~~csharp
/// Usage: int num = new IntParser("1234").Parse();
public class Parser : BaseLexer {
  public Parser(UString s) : base(s) {}
  
  LLLPG (lexer(terminalType: int));
  
  public rule int ParseInt() @{
    ' '* (digits+:'0'..'9')+
    // Use LINQ to convert the list of digits to an integer
    {return digits.Aggregate(0, (n, d) => n * 10 + (d - '0'));}
  };
}

/// Generated output for ParseInt()
public int ParseInt()
{
    int la0;
    List<int> digits = new List<int>();
    for (;;) {
        la0 = LA0;
        if (la0 == ' ')
            Skip();
        else
            break;
    }
    digits.Add(MatchRange('0', '9'));
    for (;;) {
        la0 = LA0;
        if (la0 >= '0' && la0 <= '9')
            digits.Add(MatchAny());
        else
            break;
    }
    return digits.Aggregate(0, (n, d) => n * 10 + (d - '0'));
}
~~~

In ANTLR you use `+=` to accomplish the same thing. Obviously, `+:` is uglier; unfortunately I had already defined `+=` as "add something to a _user-defined_ list", whereas `+:` means "automatically _create_ a list at the beginning of the method, and add something to it here".

In summary, if `Foo` represents a rule, token type, or a character, the following five operators are available:

- `x=Foo`: set an existing variable `x` to the value of `Foo` 
- `x+=Foo`: add the value of `Foo` to the existing list variable `x` (i.e. `x.Add(Foo())`, if `Foo` is a rule)
- `x:=Foo`: create a variable `x` in the current scope with `Foo` as its value (i.e. `var x = Foo();` if `Foo` is a rule).
- `x:Foo`: create a variable called `x` of the appropriate type at the beginning of the method and set `x` to it here. If `Foo` is a token or character, use the `terminalType` code-generation option to control the declared type of `x` (e.g. `LLLPG(parser(terminalType: Token))`) If you use the label `x` in more than once place, LLLPG will create only one (non-list) variable called `x`.
- `x+:Foo`: create a _list_ variable called `x` of the appropriate type at the beginning of the method, and add the value of `Foo` to the list (i.e. `x.Add(Foo())`, if `Foo` is a rule). By default the list will have type `List<T>` (where `T` is the appropriate type), and you can use the `listInitializer` option to change the list type globally (e.g. `LLLPG(parser(listInitializer: IList<T> _ = new DList<T>()))`, if you prefer [DList](http://core.loyc.net/collections/dlist.html))

You can only use these operators on "primitive" grammar elements: terminal sets and rule references. For example, `digits:(('0'..'9')*)` and `digits+:(('0'..'9')*)` are illegal; but `(digits+:('0'..'9'))*` is legal.

### `any` directive ###

In a rare victory for feature creep, LLLPG 1.3 lets you mark a rule with an extra "word" attribute, which can be basically any word, and then refer to that word with the "any" directive. For example:

~~~csharp
rule Words @{ (any fruit ' ')* };
fruit rule Apple @{ "apple" };
fruit rule Grape @{ "grape" };
fruit rule Lemon @{ "lemon" };
~~~

Here, the `Words` rule uses `any fruit`, which is equivalent to

~~~csharp
rule Words @{ ((Apple / Grape / Lemon) ' ')* };
~~~

The word `fruit` is stripped from the output. You could also write `[fruit]` as a normal attribute with square brackets around it, but in that case the attribute _remains_ in the output.

The `any` directive also has an "`any..in`" version, in which you supply a grammar fragment that is repeated for each matching rule. This is best explained by example:

~~~csharp
rule int SumWords @{ (any word in (x+:word) ' ')* {return x.Sum();} };
word rule int One @{ ""one"" {return 1;}  };
word rule int Two @{ ""two"" {return 2;}  };
word rule int Ten @{ ""ten"" {return 10;} };
~~~

The `SumWords` rule could be written equivalently as

~~~csharp
rule int SumWords @{
    (x+:One ' ' / x+:Two ' ' / x+:Ten ' ')* {return x.Sum();}
};
~~~

### `inline` and `extern` rules ###

These features are described in "[Advanced techniques](9-advanced-techniques.html)".

### Error handling features ###

There is a separate article about [error handling](7-error-handling.html).

### Greedy, nongreedy, and the slash `/` operator ###

For more information about these features, see [Managing ambiguity](8-managing-ambiguity.html).

Rules with parameters and return values
---------------------------------------

You can add parameters and a return value to any rule, and use parameters when calling any rule:

~~~csharp
// Define a rule that takes an argument and returns a value.
// Matches a pattern like TT.Num TT.Comma TT.Num TT.Comma TT.Num...
// with a length that depends on the 'times' argument.
token double Mul(int times) @{
    x:=TT.Num 
    nongreedy(
       &{times>0} {times--;}
        TT.Comma y:=Num
        {x *= y;})*
    {return x;}
};
// To call a rule with a parameter, add a parameter list after 
// the rule name.
rule double Mul3 @{ x:=Mul(3) {return x;} };
~~~

Here's the code generated for this parser:

~~~csharp
double Mul(int times)
{
  int la0, la1;
  var x = Match(TT.Num);
  for (;;) {
     la0 = LA0;
     if (la0 == TT.Comma) {
        if (times > 0) {
          la1 = LA(1);
          if (la1 == Num) {
             times--;
             Skip();
             var y = MatchAny();
             x *= y;
          } else
             break;
        } else
          break;
     } else
        break;
  }
  return x;
}
double Mul3()
{
  var x = Mul(3);
  return x;
}
~~~

There is a difference between `Foo(123)` and `Foo (123)` with a space. `Foo(123)` calls the `Foo` rule with a parameter of 123; `Foo (123)` is equivalent to `Foo 123` so the rule (or terminal) Foo is matched followed by the number 123 (which is "`{`" in ASCII).

_Related_: [parameters to recognizers](parameters-to-recognizers.html)

LLLPG configuration
-------------------

In this article you learned about LLLPG's special _grammar_ features. To learn about grammar _options_, see the reference [Configuring LLLPG](lllpg-configuration.html).

A random fact
-------------

_Did you know?_ Unlike ANTLR, LLLPG does not care much about parenthesis when interpreting loops and alternatives separated by `|` or `/`. For example, all of the following rules are interpreted the same way and produce the same code:

~~~csharp
rule Foo1 @{ ["AB" | "A" | "CD" | "C"]*     };
rule Foo2 @{ [("AB" | "A" | "CD" | "C")]*   };
rule Foo3 @{ [("AB" | "A") | ("CD" | "C")]* };
rule Foo4 @{ ["AB" | ("A" | "CD") | "C"]*   };
rule Foo5 @{ ["AB" | ("A" | ("CD" | "C"))]* };
~~~

The loop (`*`) and all the arms are integrated into a single prediction tree, regardless of how you might fiddle with parenthesis. Knowing this may help you understand error messages and generated code better.

Another thing that is Good to Know™ is that `|` behaves differently when the left and right side are terminals. `('1'|'3'|'5'|'9' '9')` is treated not as _four_ alternatives, but only _two_: `('1'|'3'|'5') | '9' '9'`, as you can see from the generated code:

~~~csharp
  la0 = LA0;
  if (la0 == '1' || la0 == '3' || la0 == '5')
    Skip();
  else {
    Match('9');
    Match('9');
  }
~~~

This happens because a "terminal set" is always a single unit in LLLPG, i.e. multiple terminals like `('A'|'B')` are combined and treated the same as a single terminal `'X'`, whenever the left and right sides of `|` are both terminal sets. If you insert an empty code block into the first alternative, it is no longer treated as a simple terminal, LLLPG cannot join the terminals into a single set anymore. In that case LLLPG sees four alternatives instead, causing different output:

~~~csharp
rule Foo @{ '1' {/*empty*/} | '3' | '5' | '9' '9' };

void Foo()
{
  int la0;
  la0 = LA0;
  if (la0 == '1')
    Skip();
  else if (la0 == '3')
    Skip();
  else if (la0 == '5')
    Skip();
  else {
    Match('9');
    Match('9');
  }
}
~~~

### Another random fact

Also, LLLPG is not the Ferrari of parser generators.

I'm not sure where to put this in the article series, but if you're going to parse a really complicated language, it's something to bear in mind.

During my experiences parsing EC#, I've discovered that LLLPG can take virtually unlimited time and memory to process certain grammars (especially highly ambiguous grammars, such as the kind you will write accidentally during development), and processing time can increase exponentially with `k`. In fact, even at the default LL(2), certain complex grammars can make LLLPG run a very long time and use a large amount of RAM. At LL(3), the same "bad grammars" will make it run pretty much forever at LL(3) and suck up all your RAM. As I was writing my EC# parser under LLLPG 0.92, I made a seemingly innocuous change that caused it to take pretty much forever to process my grammar, even at LL(2), so I added a timeout feature where the Custom Tool will abort after 10-15 seconds.

The problem was hard to track down because it only seemed to happen in large, complex grammars, but eventually I figured out how to write a short grammar that made LLLPG sweat:

    [DefaultK(2)] [FullLLk(false)]
    LLLPG lexer {
        rule PositiveDigit @{ '1'..'9' {""Think positive!""} };
        rule WeirdDigit @{ '0' | &{a} '1' | &{b} '2' | &{c} '3'
                 | &{d} '4' | &{e} '5' | &{f} '6' | &{g} '7'
                 | &{h} '8' | &{i} '9' };
        rule Start @{ (WeirdDigit / PositiveDigit)* };
    }

Originally LLLPG took about 15 seconds to process this, but I was able to fix it; now LLLPG can process this grammar with no perceptible delay. However, I realized that there was a similar grammar that the fix wouldn't fix at all:

    [DefaultK(2)] [FullLLk(false)]
    LLLPG lexer {
        rule PositiveDigit @{ '1'..'9' {""Think positive!""} };
        rule WeirdDigit @{ '0' | &{a} '1' | &{b} '2' | &{c} '3'
                 | &{d} '4' | &{e} '5' | &{f} '6' | &{g} '7'
                 | &{h} '8' | &{i} '9' };
        rule Start @{ WeirdDigit+ / PositiveDigit+ };
    }

Again, this takes about 15 seconds at LL(2) and virtually unlimited time at LL(3), and it looks like major design changes will be needed to overcome the problem.

Luckily, most grammars _don't_ make LLLPG horribly slow, but it's no speed demon on large grammars either. Little by little, LLLPG took more time to process my EC# parser until (at about 30 seconds) I decided to manually rewrite the code that gave LLLPG so much trouble, namely the prediction code for statements. The rest of the grammar is still written in LLLPG.

So, until I find time to fix the speed issue, I'd suggest using LL(2), which is the default, except in specific rules where you need more. In any case, large values of `DefaultK` will never be a good idea; I heard that, in theory, for certain "hard" or highly ambiguous grammars, the output code size can grow exponentially as k increases (although I do not have any examples).

Next up
-------

The next article in this series is about the [Loyc libraries](5-loyc-libraries.html), which includes the LLLPG runtime library.
