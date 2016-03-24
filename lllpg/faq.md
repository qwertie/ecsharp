---
title: LLLPG FAQ
layout: article
redirectDomain: ecsharp.net
---

### Q. What are the advantages of LLLPG over other tools?

Many! See the beginning of [article #5](lllpg-part-5.html).

### Q. I've used other parser generators before. Could you explain quckly how to use LLLPG?

That's covered in [article #1](lllpg-part-1.html). Skim it... I haven't written a cheat sheet yet. You might even want to read [article #5](lllpg-part-5.html), which was written 1.6 years later, because it talks about the newest features and summarizes the main features again, as well as the most advanced topics.

### Q. What example grammars are available?

Other than the various examples in the articles, you'll find three larger examples in the [release repo](https://github.com/qwertie/LLLPG-Release) - click "Download ZIP" on the right side. Note that the included Enhanced C# parser may not be up-to-date, you'll want to pull the [Loyc repo](https://github.com/qwertie/Loyc) for that.

### Q. How do I handle keywords properly in my input language?

See the "Keyword parsing" section in [article #5](lllpg-part-5.html).

### Q. How do I avoid memory allocations during parsing?

See "How to avoid memory allocation in a lexer" in [article #5](lllpg-part-5.html).

### Q. How do I use LLLPG without a runtime library?

This is demonstrated by the "CalcExample-Standalone" example in the [release repo](https://github.com/qwertie/LLLPG-Release).

### Q. Tell me about the runtime library (LoycCore in NuGet). Why three assemblies? ANTLR only has one!

I found the .NET Framework never had all the facilities I needed, so I made a series of libraries to "fill in the gaps" in the .NET Framework. You can read about them [on their home page](http://core.loyc.net/). Basically,

- Loyc.Essentials.dll contains miscellaneous things I personally can't bear to live without, stuff that comes in handy regardless of what kind of software I'm writing for .NET.
- Loyc.Syntax.dll contains the actual runtime classes for LLLPG, such as [`BaseLexer`](http://loyc.net/doc/code/classLoyc_1_1Syntax_1_1Lexing_1_1BaseLexer.html) and [`BaseParserForList<Token,MatchType>`](http://loyc.net/doc/code/classLoyc_1_1Syntax_1_1BaseParserForList_3_01Token_00_01MatchType_01_4.html). It also defines [Loyc trees](https://github.com/qwertie/LoycCore/wiki/Loyc-trees) ([LNode](http://loyc.net/doc/code/classLoyc_1_1Syntax_1_1LNode.html)) and [LES](https://github.com/qwertie/LoycCore/wiki/Loyc-Expression-Syntax).
- Loyc.Collections.dll isn't that important for LLLPG users, unless you use Loyc trees or LES, which rely on the `VList<T>` type defined in Loyc.Collections.dll. In any case, Loyc.Syntax takes a dependency on Loyc.Collections.dll so you'll have to have it, too.

[Article #3](lllpg-part-3.html) talks about these libraries in more detail; see "A brief overview of the Loyc libraries".

### Q. How do I customize error handling in my grammar?

Please see "Error handling mechanisms in LLLPG" in [article #3](lllpg-part-3.html).

### Q. How do I shut off an ambiguity warning?

If you have two ambiguous alternatives like `(A | B)`, change it to `(A / B)`. The only effect is to shut off the warning. You can mix `|` and `/` in a single series of alternatives, and warnings are suppressed only if there is a continuous chain of slashes between two ambiguous alternatives. For example, in `(A | B / C / D | E / F)`, an ambiguity warning between `B` and `D` would be suppressed, but a warning of ambiguity between `D` and `F` would not be suppressed.

If there is ambiguity with an exit branch (`[...]*`, `(...)+` or `[...]?`), use `greedy` to tell LLLPG to prefer to stay in the loop (or case of an optional item, to match the item.) `nongreedy` also exists, but requires caution; for more information about these, see "Managing ambiguity, part 4: greedy and nongreedy" in [article #4](lllpg-part-4.html).

### Q. What does this error message mean?

Could you be more specific? If you're a beginner and wondering why something is ambiguous, make sure you understand LL(k) (see the next question).

### Q. What kind of grammars does LLLPG accept? What's legal and what isn't?

LLLPG accepts "augmented" LL(k) grammars, which are the "top-down" grammars that decide which branch to take in the grammar _before_ matching (consuming) anything from the input stream. By "augmented" I mean that LLLPG also supports extra features: zero-width assertions, "gates", and mechanisms for ambiguity resolution (prioritization).

Please see "Parsing terminology" in [article #2](lllpg-part-2.html) for a more in-depth explanation.

### Q. LLLPG generates a *.cs file from my *.ecs file. Should I check it into source control (Git/SVN)?

Yes. In fact, if you're using the LLLPG Custom Tool in Visual Studio, LLLPG is _not_ invoked when you build your project, so failing to check it in is a recipe for failing builds.

### Q. My question isn't here!

I'm gonna level with you. This is not a real FAQ. In fact I've never been asked any of these questions. I am a fraud, I made them up. But you can still ask - reach me by email at `gmail.com`, with account name `qwertie256`.
