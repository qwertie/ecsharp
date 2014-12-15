---
title: Parsing & type inference (Algorithm W in Haskell), and thoughts about Union types
layout: post
---
For a university class I had to implement type inference for a toy lambda calculus (first an untyped one, then a typed one). A parser was provided, but it was very bare-bones so I decided to make my own using Parsec. (Sadly, returning to university has slowed my work on [Loyc](http://loyc.net), but I digress.)

There are few Parsec examples available in which there is a separate lexer and parser, so if you're looking for a two-stage Parsec parser, you've come to the right place. My lexer+parser is probably significantly larger than necessary though, because I don't know all the tricks of the Parsec trade. Two-stage parsers are apparently harder to write, but I heard they have better performance.

It's relatively hard to find good examples of type inference implementations, and even harder to find good explanations of how type inference is supposed to work. I am therefore providing the implementation of my toy language to the public in the hope that it might help you see what it takes to make a working type inference engine. This is not a complete Algorithm W (see source code for details) and it has some useless extra crap required by the assignment (e.g. it includes the unit type so you can write an expression like `case (\x.x) () of () -> "Output"`), but the type inference algoritm is cleanly divided into three parts (equation collection, unification, solving), and part 1 includes a discussion of how that [weird CS notation](http://loyc.net/2013/formalism-tutorial.html) relates to actual source code, as least in terms of the notation used by my professor (evidently each professor has a different notation).

The implementation consists of three files, the parser ([ParserA4.hs](/misc/typeInference/ParserA4.hs)), the type inference and runtime engines ([Ass4.hs](/misc/typeInference/Ass4.hs)), and an example file which you can run directly ([examples.txt](/misc/typeInference/examples.txt)). Each is self-documenting. If you run `ghci Ass4.hs` you can see all the type equations collected by part one using `testCollect`, e.g. `testCollect "(Y \\fact x. if x<=1 then 1 else x*(fact (x-1))) 5"`. Run main or repl to test the whole language (typing and execution).

The toy language does not allow you to define your own type constructors, which is sad because defining pairs, lists, and unit as built-in types is a bit of a waste of time. I think the assignment may as well have just asked us to create a proper programming language with user-defined types. It wouldn't have been much harder, would it? Not for me anyway.

Haskell isn't the ideal language for a type-inference engine. I believe my type inference algorithm runs in slightly more than O(N log N) time (O(N (log N)^2) maybe?) for typical programs of size N, which is better than naive type inference implementations that run in O(N^2) time. But I believe if I had implemented this in an imperative language, I'd be able to reduce that to O(N) straightforwardly by taking advantage of hashtables and mutable state.

## How Haskell annoyed me

Writing these two assignments also drew my attention to a design flaw of Haskell, or at least an annoying limitation, which clearly makes it hard to build large software systems. Indeed, since Haskell doesn't allow you to construct lists of a type class like such as `Show a`, and has no dynamic casting, it's a good sign that large software systems are difficult in Haskell. Then you notice that Haskell makes it really hard to define two record types that coincidentally (or deliberately) have the same field name. Thus Haskell doesn't scale that well. Today I want to describe another limitation of the type system that I hit while making my toy compiler: the lack of inheritance, or of _union types_.

The code is divided into two parts, a parser (ParserA4.hs) and the main assignment (Ass4.hs). The parser defines this type, which is the data type of literals that it can parse:

    data Value = IntVal Int
               | FloatVal Float
               | CharVal Char
               | BoolVal Bool
               | ListVal [Value]
               | UnitVal 
               | PairVal (Value, Value)
               deriving (Eq)

Actually PairVal is kind of superfluous for the parser, because while the parser can _parse_ the comma operator for pair construction, it does not actually support pair _literals_ and does not use the `PairVal` constructor.

The parser also defines the expression type for lambda terms, which includes a Literal with a Value:

    data LTerm = Var String       -- bound variable OR operator name
               | Abs String LTerm -- anonymous function (λstring. term)
               | App LTerm LTerm  -- application (fn arg) or built-in oper.
               | Literal Value    -- any literal (2, 2.5, True, '2', "two")
               | If  LTerm LTerm LTerm  -- if c then t else f
               | Case LTerm (CasePattern LTerm) -- case expr of * -> *
               deriving (Eq)

The assignment itself (Ass4.hs), however, needs to support not just the 7 types of `Value` listed, but also closures of compiled run-time code (lists of `CesInstr`, which are instructions for the simple "CES virtual machine"). So I defined this second type:

    -- We must support closures as well as normal Values
    data Value' = V Value
                | Closure [CesInstr] [Value']
                | YClosure [CesInstr] [Value']
                deriving (Eq, Show)

Since Haskell doesn't have a concept of inheritance, I build `Value'` with composition instead and a new constructor 'V'. (There are two types of closures, due to the fact that the professor decided that recursive functions built with the Y combinator would work differently than those built without it.)

This causes a limitation at run time, though, one that I didn't mention when I handed in the assignment. Remember that there is a `ListVal [Value]` constructor `Value`. This means that lists can only contain values of type `Value`, not `Value'`. This, in turn, means that my runtime engine cannot support lists of closures. You'll get a runtime error if you try to store a lambda function in a list. In principle it is perfectly possible, but because of the types involved, you aren't allowed.

How can we solve this problem? In Haskell there are multiple ways, none of which are very satisfying.

The first solution is that we could modify the definition of Value in the ParserA4 module to support closures. But this would be inappropriate. The parser module is supposed to define the parser--it knows nothing about the runtime engine, and it would be inappropriate to define large types like `CesInstr` in the Parser module since `CesInstr` is unrelated to parsing. I suppose that's the main point I want to make: as long as you're writing a single module or you have global knowledge of what the program as a whole wants to accomplish, you can introduce unnecessary dependencies between different components to solve your typing problems. This, however, is hostile to large-scale software engineering.

The second solution is to define a completely separate type for run-time values versus compile-time values:

    -- Runtime version of Value
    data Value' = IntVal' Int
                | FloatVal' Float
                | CharVal' Char
                | BoolVal' Bool
                | ListVal' [Value]
                | UnitVal' 
                | PairVal' (Value, Value)
                | Closure' [CesInstr] [Value']
                | YClosure' [CesInstr] [Value']
                deriving (Eq)

But this makes a lot of extra work for you.

- Either you have to put apostrophes on most of your new type contructors, or you have to `import qualified` the parser and manually change all references to the parser module.
- You also have to write conversion routines that manually map each and every kind of `Value` to `Value'`. This is annoying to do in a toy compiler--how much more annoying would it be in a "real-life" compiler?

Neither of these solutions is fun. I think that this problem the solution to this kind of problem has already been solved nicely by [Ceylon](http://ceylon-lang.org/), which has a feature called **union types**. If Haskell supported union types, then a type like

    data Color = Red | Green | Blue | Custom Int

would be treated as four separate types, with `Color` as an alias for the union of those four types. This immediately solves the ugly-composition problem; I could write something like

    data Value' = existing Value
                | Closure [CesInstr] [Value']
                | YClosure [CesInstr] [Value']
                deriving (Eq, Show)

Where "`existing`" would be a way to re-use existing type constructor(s) in a new union type.

Then I could just write `(IntVal 5)` instead of `(V (IntVal 5))`, and it would simultaneously be a valid value of type `Value` and `Value'` . But what about closures? How can I support lists of closures without modifying the parser module?

Well, remember that what makes this problem so annoying is that I have to write a big function to map `IntVal` to `IntVal'`, `FloatVal` to `FloatVal'`, etc. I have to write code for _all_ the type constructors even though the actual problem I'm trying to solve involves only lists and closures.

If we had union types, we could create a new runtime list type that supports closures, but keep all the other values the same. It would work something like this: I would define a new `Value'` type as before, but it would re-use most of the type constructors that already exist.

    data Value' = existing (IntVal 
                          | FloatVal 
                          | CharVal 
                          | BoolVal 
                          | UnitVal 
                          | PairVal)
                | ListVal' [Value']
                | Closure [CesInstr] [Value']
                | YClosure [CesInstr] [Value']
                deriving (Eq)

Notice that `Value'` is the same as `Value` except that (1) `ListVal` has been removed and (2) `ListVal'`, `Closure`, and `YClosure` have been added.

I still need a function to convert from `Value` to `Value'`, but it's very simple:

    toRuntimeValue v = case v of
      ListVal list -> ListVal' (map toRuntimeValue list)
      v' -> v'

This is just a type-changing trick. In this code, `list` has type `[Value]` which really means `[IntVal|FloatVal|ListVal...]`, where the union of possible types does _not_ include `Closure`. `ListVal'` is defined such that it _can_ contain `Closure` and `YClosure`, as desired for the runtime engine. Therefore we have to switch from `ListVal` to `ListVal'`, and the expression `map toRuntimeValue list` is needed in case the list itself contains other `ListVal`s that _also_ need to be converted to `ListVal'`.

The inferred return value of `toRuntimeValue` will be `IntVal|FloatVal|ListVal'|...`. This type is _not equal_ to `Value'`, because `Closure` and `YClosure` are not among the possible output types, but it _would be a subtype_ of `Value'`, so the compiler would allow me to add this type signature:

    toRuntimeValue :: Value -> Value'
    toRuntimeValue v = case v of
      ListVal list -> ListVal' (map toRuntimeValue list)
      v' -> v'

Note that inside the last case, `v'` would have type `IntVal|FloatVal|...` where `ListVal` is _not_ one of the possibilities; thus `v'` is compatible with `Value'`, or in other words `v'` is a subtype of `Value'`.

Hope that makes sense. Anyway, it's just a thought. It's possible that Haskell's type system has some limitations that would prevent such a feature from being added, I don't know.
