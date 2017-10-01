---
title: "LLLPG Home Page"
layout: page
tagline: "The LL(k) Parser Generator for Coders"
commentIssueId: 35
---

![Logo](lllpg-logo.png)

LLLPG is a recursive-decent LL(k) parser generator for C# that generates efficient code and integrates with Visual Studio. Parsers written with LLLPG include [Enhanced C#](https://github.com/loycnet/ecsharp/tree/master/Main/Ecs/Parser) and [LES](https://github.com/loycnet/ecsharp/tree/master/Core/Loyc.Syntax/LES). Help wanted to add additional output languages.

Learn it!
---------

All the documentation is absolutely free!

#### Tutorial series

   - [Part 1: Introduction](1-introduction.html)
   - [Part 2: Simple Examples](2-simple-examples.html)
   - [Part 3: Parsing Terminology](3-parsing-terminology.html)
   - [Part 4: Grammar Features](4-lllpg-grammar-features.html)
   - [Part 5: The Loyc Libraries](5-loyc-libraries.html)
   - [Part 6: How to Write a Parser](6-how-to-write-a-parser.html)
   - [Part 7: Error Handling](7-error-handling.html)
   - [Part 8: Managing Ambiguity](8-managing-ambiguity.html)
   - [Part 9: Advanced Techniques](9-advanced-techniques.html)

#### Bonus

   - [Parse JSON in 111 lines of code, print it back in 233](parsing-json.html)

Reference material
------------------

 - [Frequently Asked Questions](faq.html)
 - [Version history](version-history.html)
 - Reference: [Configuring & invoking LLLPG](lllpg-configuration.html)
 - Reference: [APIs called by LLLPG](lllpg-api-reference.html)
 - Reference: [The ANTLR-like syntax mode](lllpg-in-antlr-style.html)
 - Appendix: [FullLLk versus \"approximate\" LL(k)](full-llk-vs-approximate.html)
 - Appendix: [How LLLPG fits into LeMP & Enhanced C#](lemp-processing-model.html)
 - Appendix: [Parameters to recognizers](parameters-to-recognizers.html)
  
Download & install
------------------

LLLPG is distributed with LeMP and Enhanced C#; please see their [download & installation instructons](http://ecsharp.net/lemp/install.html). Also, please download the [Samples repository](http://github.com/qwertie/LLLPG-Samples) so you'll have some grammars to play with; these demos should compile successfully even before LLLPG is installed.
