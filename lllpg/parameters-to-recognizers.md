---
title: "Appendix: Parameters to recognizers"
layout: article
date: 30 May 2016
---

As explained in [LLLPG grammar features](4-lllpg-grammar-features.html), each rule can have a _recognizer form_ which is called by syntactic predicates `&(...)`. The recognizer always has a return type of `bool`, regardless of the return type of the main rule, and any action blocks `{...}` are removed from the recognizer (currently there is no way to keep an action block; sorry.)

You can cause parameters to be kept or discarded from a recognizer using a `recognizer` attribute on a rule. Observe how code is generated for the following rule:

~~~csharp
    LLLPG(parser) { 
        [recognizer { void FooRecognizer(int x); }]
        token double Foo(int x, int y) @{ match something };
        
        token double FooCaller(int x, int y) @[
            Foo(1) Foo(1, 2) &Foo(1) &Foo(1, 2)
        ];
    }
~~~

The recognizer version of Foo will accept only one argument because the `recognizer` attribute specifies only one argument. Although the `recognizer` attribute uses `void` as the return type of `FooRecognizer`, LLLPG ignores this and changes the return type to `bool`:

~~~csharp
    double Foo(int x, int y)
    {
      Match(match);
      Match(something);
    }
    bool Try_FooRecognizer(int lookaheadAmt, int x)
    {
      using (new SavePosition(this, lookaheadAmt))
         return FooRecognizer(x);
    }
    bool FooRecognizer(int x)
    {
      if (!TryMatch(match))
         return false;
      if (!TryMatch(something))
         return false;
      return true;
    }
    double FooCaller(int x, int y)
    {
      Foo(1);
      Foo(1, 2);
      Check(Try_FooRecognizer(0, 1), "Foo");
      Check(Try_FooRecognizer(0, 1, 2), "Foo");
    }
~~~

Notice that LLLPG does not verify that `FooCaller` passes the correct number of arguments to `Foo` or `FooRecognizer`, not in this case anyway. So LLLPG does not complain or alter the incorrect call `Foo(1)` or `Try_FooRecognizer(0, 1, 2)`. Usually LLLPG will simply repeat the argument argument list you provide, whether it makes sense or not. However, as a normal rule is "converted" into a recognizer, LLLPG can automatically reduce the number of arguments to other rules called by that rule, as demonstrated here:

~~~csharp
    [recognizer {void BarRecognizer(int x);}]
    token double Bar(int x, int y) @[ match something ];
    
    rule void BarCaller @[
        Bar(1, 2)
    ];
    
    rule double Start(int x, int y) @[ &BarCaller BarCaller ];
~~~

Generated code:

~~~csharp
    double Bar(int x, int y)
    {
      Match(match);
      Match(something);
    }
    bool Try_BarRecognizer(int lookaheadAmt, int x)
    {
      using (new SavePosition(this, lookaheadAmt))
         return BarRecognizer(x);
    }
    bool BarRecognizer(int x)
    {
      if (!TryMatch(match))
         return false;
      if (!TryMatch(something))
         return false;
      return true;
    }
    void BarCaller()
    {
      Bar(1, 2);
    }
    bool Try_Scan_BarCaller(int lookaheadAmt)
    {
      using (new SavePosition(this, lookaheadAmt))
         return Scan_BarCaller();
    }
    bool Scan_BarCaller()
    {
      if (!BarRecognizer(1))
         return false;
      return true;
    }
    double Start(int x, int y)
    {
      Check(Try_Scan_BarCaller(0), "BarCaller");
      BarCaller();
    }
~~~

Notice that `BarCaller()` calls `Bar(1, 2)`, with two arguments. However, `Scan_BarCaller`, which is the auto-generated name of the recognizer for `BarCaller`, calls `BarRecognizer(1)` with only a single parameter. Sometimes a parameter that is needed by the main rule (`Bar`) is not needed by the recognizer form of the rule (`BarRecognizer`) so LLLPG lets you remove parameters in the `recognizer` attribute; LLLPG will automatically delete call-site parameters when generating the recognizer version of a rule. You must only remove parameters from the end of the argument list; for example, if you write

~~~csharp
    [recognizer { void XRecognizer(string second); }]
    rule double X(int first, string second) @[ match something ];
~~~

LLLPG will **not notice** that you removed the _first_ parameter rather than the _second_, it will only notice that the recognizer has a _shorter_ parameter list, so it will only remove the _second_ parameter. Also, LLLPG will only remove parameters from calls to the recognizer, not calls to the main rule, so the recognizer cannot accept more arguments than the main rule.
