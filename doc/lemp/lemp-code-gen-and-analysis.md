---
title: Using LeMP for C# code generation and analysis
layout: post
---

Introduction
------------

Today I planned to write an article about the new pattern-matching and "algebraic data type" features I added to C# via [LeMP](https://github.com/qwertie/Loyc/wiki/LeMP), but then I saw the new [WuffProjects.CodeGeneration](http://www.codeproject.com/Articles/892114/WuffProjects-CodeGeneration) library and thought "wait a minute, LeMP has made that easy for a year now!" In fact, LeMP can do some pretty neat stuff, as you'll see!

LeMP is a macro processor for a superset of C# called "Enhanced C#". If you've ever used [sweet.js](http://sweetjs.org/), LeMP is basically the same thing for C#, just not as polished. Also, whereas sweet.js seems focused on letting you create your own macros, LeMP comes with many useful built-in macros, but creating new ones isn't as easy (yet).

So here's the scenario: you want to write a program that generates C# source code, and either runs it or analyzes it somehow. How should you do it?

In fact, this article also shows how to _parse_ and analyze C# source code, but I'll focus first on code generation. This article also contains links to some fascinating stuff, so try to read to the end before you click off to somewhere else... this article gets more interesting (IMO) as you geet farther into it! 

