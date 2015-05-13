LLLPG FAQ
=========

### Q. What are the advantages of LLLPG over other tools?
### Q. I've used other parser generators before. Could you explain quckly how to use LLLPG?
### Q. How do I get started quickly with LLLPG?
### Q. What example grammars are available?
### Q. How do I handle keywords properly in my input language?
### Q. How do I avoid memory allocations during parsing?
### Q. How do I use LLLPG without a runtime library?
### Q. Tell me about the runtime library (LoycCore in NuGet). Why four assemblies? ANTLR only has one!
### Q. How do I customize error handling in my grammar?
### Q. How do I shut off an ambiguity warning?
### Q. What does this error message mean?

Could you be more specific? If you're a beginner and wondering why something is ambiguous, make sure you understand LL(k) (see the next question).

### Q. What kind of grammars does LLLPG accept? What's legal and what isn't?
### Q. LLLPG generates a *.cs file from my *.ecs file. Should I check it into source control (Git/SVN)?

Yes. In fact, if you're using the LLLPG Custom Tool in Visual Studio, LLLPG is _not_ invoked when you build your project, so failing to check it in is a recipe for failing builds.

### Q. My question isn't here!

You can reach me by email at `gmail.com`, with account name `qwertie256`.
