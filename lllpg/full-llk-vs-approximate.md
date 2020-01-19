---
title: "Appendix: FullLLk versus \"approximate\" LL(k)"
layout: article
date: 30 May 2016
---

First, the short version: try adding `[FullLLk(true)]` to your grammar if you suspect prediction isn't working perfectly.

Now, it's a bit difficult to explain how LLLPG generates a prediction tree without invoking all sorts of math-speak that, if you are like me, would make your head hurt. It is easier to explain with examples. Let's start simple:

	rule Comparison @{ '>' '=' | '<' '=' | '=' '=' | '>' | '<' };

	void Comparison()
	{
	  int la0, la1;
	  la0 = LA0;
	  if (la0 == '>') {
		 la1 = LA(1);
		 if (la1 == '=') {
			Skip();
			Skip();
		 } else
			Skip();
	  } else if (la0 == '<') {
		 la1 = LA(1);
		 if (la1 == '=') {
			Skip();
			Skip();
		 } else
			Skip();
	  } else {
		 Match('=');
		 Match('=');
	  }
	}

Roughly what happens here is that LLLPG

1. Finds the first set for each arm: {'>'} for 1 and 4, {'<'} for 2 and 5, {'='} for 3.
2. Finds a common subset between arm 1 and the others. In this case it finds {'>'}, common between 1 and 4.
3. Generates the `if (la0 == '>') {...}` statement and then generates an inner prediction tree _based on the knowledge_ that la0 is in the set {'>'}, which excludes arms 2, 3 and 5.
4. Knowing that `la0 != '>'`, it excludes arms 1 and 4, then looks for another common subset and finds {'<'}, common between 2 and 5.
5. Generates the `if (la0 == '<') {...}` statement and then generates an inner prediction tree _based on the knowledge_ that la0 is in the set {'<'}, which additionally excludes arm 3.
6. Only one arm is left, arm 3, and this becomes the else branch.

Note: the generated code is correct, although this example is unusual because arm 3 ends up acting as if it were the `default` branch. The code will change if you explicitly mark the last arm as the `default`, or if you add an `error` branch.

Here's another example:

    rule ABCD @{ (A B | C D) {} | A D };

    void ABCD()
    {
      int la0, la1;
      do {
        la0 = LA0;
        if (la0 == A) {
          la1 = LA(1);
          if (la1 == B)
            goto match1;
          else {
            Skip();
            Match(D);
          }
        } else
          goto match1;
        break;
      match1: { ... } // omitted for brevity
      } while (false);
    }

**Note**: `{}` forces LLLPG to create two prediction trees instead of one, see "A random fact" from the previous article.

I'm using this example because it was mentioned by Terrance Parr as something that ANTLR 2 couldn't handle. LLLPG has no problem; to generate the outer prediction tree, LLLPG

1. Finds the first set for each arm: {`A`,`B`} for 1, {`A`} for 2.
2. Finds a common subset between arm 1 and the others. In this case it finds {`A`}.
3. Generates the `if (la0 == A) {...}` statement and then generates an inner prediction tree (for `LA(1)`) _based on the knowledge_ that la0 is in the set {`A`}, which excludes the _inner_ arm `C D` of `(A B | C D)`.
4. Knowing that `la0 != A`, it excludes arms 2, leaving only arm 1, and this becomes the `else` branch.

**Note**: I've been speaking as though LLLPG generates code during prediction, but it doesn't. Instead there is an abstract intermediate representation for prediction trees, and the C# code is only generated after analysis and prediction is complete.

I didn't realize it at first, but LLLPG's technique doesn't support all LL(k) grammars. It is more powerful than the [Linear Approximate Lookahead](http://www.antlr2.org/doc/glossary.html#Linear_approximate_lookahead) of ANTLR 2, but some cases still don't work, like this one:

	LLLPG (lexer)
	{
		[LL(3)]
		token Token    @{ Number | Operator | ' ' };
		token Operator @{ '+'|'-'|'*'|'/'|'.' };
		token Number   @{ '-'? '.'? '0'..'9'+ };
	}

After (correctly) warning that `Alternatives (1, 2) are ambiguous for input such as «'-' '.' 0» ([\-.], [.0-9], ~())`, LLLPG generates this slightly incorrect code for `Token`:

    void Token()
    {
      int la1;
      switch (LA0) {
      case '-': case '.':
        {
          la1 = LA(1);
          if (la1 == '.' || la1 >= '0' && la1 <= '9')
            Number();
          else
            Operator();
        }
        break;
      case '0': case '1': case '2': case '3': case '4':
      case '5': case '6': case '7': case '8': case '9':
        Number();
        break;
      case '*': case '+': case '/':
        Operator();
        break;
      default:
        Match(' ');
        break;
      }
    }

To choose this code, LLLPG

1. Finds the first set for each arm: {`'+','-','*','/','.'`} for 1, {`'-','.','0'..'9'`} for 2 and {`' '`} for 3.
2. Finds a common subset between arm 1 and the others. In this case it finds {`'-','.'`}.
3. Generates the `case '-': case '.': {...}` block and then generates an inner prediction tree (for `LA(1)`) _based on the knowledge_ that `LA0` is in the set {`'-','.'`}, which excludes only the possibility of arm 3 (`' '`).
  - LLLPG computes the _second sets_ which (keeping in mind that `LA0` is `'-'` or `'.'`) are {`'.','0'..'9'`} for arm 1 and `_` (any character) for arm 2.
  - It finds the common subset, {`'.','0'..'9'`}
  - It generates the `if (la1 == '.' || la1 >= '0' && la1 <= '9')` and generates an inner prediction tree for `LA(2)`.
    - `LA(2)` can be anything `_` for both rules, so LLLPG reports an ambiguity between 1 and 2 and chooses 1 (`Number()`) as it has higher priority because it is listed first.
  - Knowing that LA(1) is not in the set {`'.','0'..'9'`}, it excludes arms 1, leaving only arm 2, and this becomes the `else` branch.
4. It generates the other cases, which are easy to understand so I'll skip them.

Now, why is the generated code wrong? It's wrong in the case of the input string "`-. `", which should match `Operator` but instead matches `Number`. To fix this, I added a finer-grained analysis that is enabled by the `[FullLLk]` option.

	[LL(3)] [FullLLk]
	token Token    @{ Number | Operator | ' ' };

This analysis realizes that, due to the relatively complex substructure of `Number`, it should split `'-'` and `'.'` into two separate cases. 

When analyzing the case `la0 == '-'`, the set for `LA(1)` for arm 1 is still {`'.','0'..'9'`}, but LLLPG further figures out that it should split the analysis of `LA(1)` into separate subtrees for `'.'` and `'0'..'9'`. In the subtree where `la0 == '-'` and `la1 == '.'`, LLLPG is able to figure out that `Number` should only be invoked if `LA(2)` is `'0'..'9'`. It is able to figure this out now because the information `la0 == '-' && la1 == '.'` is more _specific_ than the information it had without `[FullLLk]` (i.e. `(la0 == '-' || la0 == '.') && (la1 == '.' || la1 >= '0' && la1 <= '9')`).

So after the more detailed analysis of `[FullLLk]`, the output code becomes

    void Token()
    {
      int la1, la2;
      switch (LA0) {
      case '-':
        {
          la1 = LA(1);
          if (la1 == '.') {
            la2 = LA(2);
            if (la2 >= '0' && la2 <= '9')
              Number();
            else
              Operator();
          } else if (la1 >= '0' && la1 <= '9')
            Number();
          else
            Operator();
        }
        break;
      case '.':
        {
          la1 = LA(1);
          if (la1 >= '0' && la1 <= '9')
            Number();
          else
            Operator();
        }
        break;
      case '0': case '1': case '2': case '3': case '4':
      case '5': case '6': case '7': case '8': case '9':
        Number();
        break;
      case '*': case '+': case '/':
        Operator();
        break;
      default:
        Match(' ');
        break;
      }
    }

You still get the ambiguity warning, though. Use a slash to suppress the warning: `Number / Operator | ' '`.

Full LL(k) mode doesn't always work perfectly, and may make LLLPG run slower, which is why it is not enabled by default. But usually, it works fine and you can safely apply it to your entire grammar.

In certain cases, LLLPG reports an ambiguity that doesn't actually exist in a grammar without the `[FullLLk]` option. One example is given by [this blog post](http://loyc-etc.blogspot.ca/2013/12/bogus-ambiguity-warnings-in-lllpg.html) that I wrote while writing the EC# grammar. So if you can't figure out where an ambiguity comes from, try `[FullLLk]`. If you still get the same ambiguity warning after enabling Full LL(k), check over your grammar carefully, because it is probably genuinely ambiguous.
