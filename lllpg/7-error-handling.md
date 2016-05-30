---
title: "7. Error handling mechanisms in LLLPG"
layout: article
date: 30 May 2016
---

Admittedly, I'm not 100% sure what the right way to do error handling is, but LLLPG does give you enough flexibility, I think.

First of all, when matching a single terminal, LLLPG puts your own code in charge of error handling. For example, if the rule says

~~~csharp
    rule PlusMinus @{ '+'|'-' };
~~~

the generated code is 

~~~csharp
    void PlusMinus()
    {
      Match('-', '+');
    }
~~~

So LLLPG is relying on the `Match()` method to decide how to handle errors. If the next input is neither `'-'` nor `'+'`, what should `Match()` do:

- Throw an exception?
- Print an error, consume one character and continue?
- Print an error, keep `InputPosition` unchanged and continue?

I'm not sure what the best approach is, but by default, `BaseLexer` throws a [`LogException`](http://ecsharp.net/doc/code/classLoyc_1_1LogException.html). You can modify the [`ErrorSink`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1Lexing_1_1BaseLexer_3_01CharSrc_01_4.html#a2e052d761c53ba883b58b03cb7f8e4ff) property to avoid throwing; for example, use [`MessageSink.Console`](http://ecsharp.net/doc/code/classLoyc_1_1MessageSink.html) to write errors to the terminal.

Currently, all the `Match` methods of `BaseLexer`/`BaseILexer` and `BaseParser`/`BaseParserForList` _do not_ consume the current character or token when an error occurs.

For cases that require if/else chains or switch statements, LLLPG's default behavior is optimistic: Quite simply, it assumes there are no erroroneous inputs. When you write

~~~csharp
    rule Either @{ 'A' | B };
~~~

the output is
  
~~~csharp
    void Either()
    {
      int la0;
      la0 = LA0;
      if (la0 == 'A')
        Skip();
      else
        B();
    }
~~~

Under the assumption that there are no errors, if the input is not `'A'` then it must be `B`. Therefore, when you are writing a list of alternatives and one of them makes sense as a catch-all or default, you should put it last in the list of alternatives, which will make it the default. You can also specify which branch is the default by writing the word `default` at the beginning of one alternative:

~~~csharp
    rule B @{ 'B' };
    rule Either @{ default 'A' | B };

    // Output
    void B()
    {
      Match('B');
    }
    void Either()
    {
      int la0;
      la0 = LA0;
      if (la0 == 'A')
        Match('A');
      else if (la0 == 'B')
        B();
      else
        Match('A');
    }
~~~

Remember that, if there is ambiguity between alternatives, the order of alternatives controls their priority. So you have at least two reasons to change the order of different alternatives:

1. To give one priority over another when the alteratives overlap
2. To select one as the default in case of invalid input

Occasionally these goals are in conflict: you may want a certain arm to have higher priority and _also_ be the default. That's where the `default` keyword comes in. In this example, the "Consonant" arm is the default:

~~~csharp
    LLLPG(lexer)
    {
        rule ConsonantOrNot @{ 
            ('A'|'E'|'I'|'O'|'U') {Vowel();} / 'A'..'Z' {Consonant();}
        };
    }
    
    void ConsonantOrNot()
    {
      switch (LA0) {
      // (Newlines between cases removed for brevity)
      case 'A': case 'E': case 'I': case 'O': case 'U':
        {
          Skip();
          Vowel();
        }
        break;
      default:
        {
          MatchRange('A', 'Z');
          Consonant();
        }
        break;
      }
    }
~~~

You can use the `default` keyword to mark the "vowel" arm as the default instead, in which case perhaps we should call it "non-consonant" rather than "vowel":

~~~csharp
    LLLPG(lexer) {
        rule ConsonantOrNot @{ 
            default ('A'|'E'|'I'|'O'|'U') {Other();} 
                   / 'A'..'Z' {Consonant();}
        };
    }
~~~
    
The generated code will be somewhat different:

~~~csharp
    static readonly HashSet<int> ConsonantOrNot_set0 = NewSet('A', 'E', 'I', 'O', 'U');
    void ConsonantOrNot()
    {
        do {
            switch (LA0) {
            case 'A': case 'E': case 'I': case 'O': case 'U':
                goto match1;
            case 'B': case 'C': case 'D': case 'F': case 'G':
            case 'H': case 'J': case 'K': case 'L': case 'M':
            case 'N': case 'P': case 'Q': case 'R': case 'S':
            case 'T': case 'V': case 'W': case 'X': case 'Y':
            case 'Z':
                {
                    Skip();
                    Consonant();
                }
                break;
            default:
                goto match1;
            }
            break;
        match1:
            {
                Match(ConsonantOrNot_set0);
                Other();
            }
        } while (false);
    }
~~~

This code ensures that the first branch matches any character that is not in one of the ranges 'B'..'D', 'F'..'H', 'J'..'N', 'P'..'T', or 'V'..'Z', i.e. the non-consonants (_Note_: this behavior was added in LLLPG 1.0.1; LLLPG 1.0.0 treated `default` as merely reordering the alternatives.)

Specifying a `default` branch should never change the behavior of the generated parser _when the input is valid_. The default branch is invoked when the input is unexpected, which means it is specifically an error-handling mechanism.

**Note**: `(A | B | default C)` is usually, but not always, the same as `(A | B | C)`. Roughly speaking, in the latter case, LLLPG will sometimes let A or B handle invalid input if the code will be simpler that way.

Another error-handling feature is that LLLPG can insert error handlers automatically, in all cases more complicated than a call to `Match`. This is accomplished with the `[NoDefaultArm(true)]` grammar option, which causes an `Error(int, string)` method to be called whenever the input is not in the expected set. Here is an example:

~~~csharp
    //[NoDefaultArm]
    LLLPG(parser)
    {
        rule B @{ 'B' };
        rule Either @{ ('A' | B)* };
    }

    // Output
    void B()
    {
      Match('B');
    }
    void Either()
    {
      int la0;
      for (;;) {
         la0 = LA0;
         if (la0 == 'A')
            Skip();
         else if (la0 == 'B')
            B();
         else
            break;
      }
    }
~~~

When `[NoDefaultArm]` is added, the output changes to

~~~csharp
    void Either()
    {
      int la0;
      for (;;) {
         la0 = LA0;
         if (la0 == 'A')
            Skip();
         else if (la0 == 'B')
            B();
         else if (la0 == EOF)
            break;
         else
            Error(0, "In rule 'Either', expected one of: (EOF|'A'|'B')");
      }
    }
~~~

The error message is predefined, and `[NoDefaultArm]` is not currently supported on individual rules.

This mode probably isn't good enough for professional grammars so I'm taking suggestions for improvements. The other way to use this feature is to selectively enable it in individual loops using `default_error`. For example, this grammar produces the same output as the last one:

~~~csharp
    LLLPG(parser)
    {
        rule B @{ 'B' };
        rule Either @{ ['A' | B | default_error]* };
    }
~~~

`default_error` must be used by itself; it does not support, for example, attaching custom actions.

Finally, you can customize the error handling for a particular loop using an `error` branch:

~~~csharp
    LLLPG
    {
        rule B @[ 'B' ];
        rule Either @{
            [  'A' 
            |   B
            |   error {Error(0, ""Anticipita 'A' aŭ B ĉi tie"");} _
            ]*
        };
    }
~~~

In this example I've written a custom error message in [Esperanto](http://en.wikipedia.org/wiki/Esperanto); here's the output:

~~~csharp
    void B()
    {
      Match('B');
    }
    void Either()
    {
      int la0;
      for (;;) {
        la0 = LA0;
        if (la0 == 'A')
          Skip();
        else if (la0 == 'B')
          B();
        else if (la0 == EOF)
          break;
        else {
          MatchExcept();
          Error("Anticipita 'A' aŭ B ĉi tie");
        }
      }
    }
~~~

Notice that I used `_` inside the `error` branch to skip the invalid terminal. The `error` branch behaves very similarly to a `default` branch except that it does not participate in prediction decisions. A formal way to explain this would be to say that `(A | B | ... | error E)` is equivalent to `(A | B | ... | default ((~_) => E))`, although I didn't actually implement it that way, so maybe it's not perfectly equivalent.

One more thing that I think I should mention about error handling is the `Check()` function, which is used to check that an `&and` predicate matches. Previously you've seen an and-predicate that makes a prediction decision, as in:

~~~csharp
    token Number @[
        {dot::bool=false;}
        ('.' {dot=true;})?
        '0'..'9'+ (&{!dot} '.' '0'..'9'+)?
    ];
~~~

In this case `'.' '0'..'9'+` will only be matched if `!dot`:

~~~csharp
    ...
    la0 = LA0;
    if (la0 == '.') {
        if (!dot) {
            la1 = LA(1);
            if (la1 >= '0' && la1 <= '9') {
                Skip();
                Skip();
                for (;;) {
                    ...
~~~

The code only turns out this way because the follow set of Number is `_*`, as explained in the next article where I talk about the difference between `token` and `rule`. Due to the follow set, LLLPG assumes `Number` might be followed by `'.'` so `!dot` must be included in the prediction decision. But if `Number` is a normal `rule` (and the follow set of `Number` does not include `'.'`):

~~~csharp
    rule Number @[
        {dot::bool=false;}
        ('.' {dot=true;})?
        '0'..'9'+ (&{!dot} '.' '0'..'9'+)?
    ];
~~~

Then the generated code is different:

~~~csharp
    ...
    la0 = LA0;
    if (la0 == '.') {
      Check(!dot, "!dot");
      Skip();
      MatchRange('0', '9');
      for (;;) {
        ...
~~~

In this case, when LLLPG sees `'.'` it decides to enter the optional item `(&{!dot} '.' '0'..'9'+)?`  without checking `&{!dot}` first, because `'.'` is not considered a valid input for _skipping_ the optional item. Basically LLLPG thinks "if there's a dot here, matching the optional item is the only reasonable thing to do". So, it assumes there is a `Check(bool, string)` method, which it calls to check `&{!dot}` _after_ prediction.

Currently you can't (in general) force an and-predicate to be checked as part of prediction; prediction analysis checks and-predicates _only_ when needed to resolve ambiguity. Nor can you suppress `Check` statements or override the second parameter to `Check`. Let me know if this limitation is causing problems for you.

That's it for error handling in LLLPG!

Next up
-------

Next article in the series: [Managing Ambiguity](8-managing-ambiguity.html).
