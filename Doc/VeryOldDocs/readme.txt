These files are old and probably not worth looking at. Here's some historical context.

I originally thought of the "Loyc" concept in roughly 2007 or possibly before, at which point I registered the domain Loyc.net (which I still haven't populated with anything.)

I have a tendency to document ideas before I actually build them, which conveniently allows me to see what I was thinking in the past even if I never wrote a line of code. I did a flurry of design work, but then I became discouraged because I couldn't work out the semantic and syntactic difficulties of "how do I get various unrelated compiler extensions to work together"? I also had a bit of a crisis of confidence in myself; I was trying to write a parser generator and for the life of me, I just couldn't figure out how to do it.

I gave up, and the Loyc concept went into hibernation. The files in this folder were written before then.

I was revitalized in early 2012 when I decided that, rather than make the ultimate multi-language infrastructure, I should simply make a modestly improved version of C# and use it as a starting point for something greater. After designing EC# 1.0, I realized that it wasn't good enough. I looked on microsoft's suggestion site for C# and saw that EC# didn't solve one of the most popular feature requests: "Provide a way for INotifyPropertyChanged to be implemented for you automatically on classes".

I think it was in July or August 2012 that I realized that Lisp, with its macros, might offer the answer, so I studied Lisp just enough to understand how macros work. I then invented Loyc trees and EC# 2.0, whose most important feature would be macros rather than all that other stuff I'd chosen. Several of EC# 1's features could be implemented with macros, so those would be the features that I would focus on first.

In August I switched to a part-time employee at work (20 hours), to give me more time to work on my ideas. Prior to that I had been at 80% time (32 hours), but I found that I simply did not have the energy to work a full 40 hours per week, so 32 hours actually left no time for Loyc, at least not on weekdays, although I suppose it left me with more energy for weekend work.

In August or September I also restarted work on my parser generator, LLLPG. It was still a very difficult project; I knew what the input and output should look like and what semantics I wanted, but figuring out what intermediate data structures I needed and what steps needed to be taken in what order was a huge challenge. But, somehow I figured it out this time (having 50% free time might have something to do with it).

I originally envisioned Loyc as a unified architecture that all kinds of parsers and tools would fit into, and I was strongly focused on tools that could be integrated into an IDE. But my ideas have evolved; I now think of Loyc as a "looser" set of tools, with a vision that is less grandiose and parts that are better separated from each other. Following Eric Raymond's famous analogy, I have found that I can't design or build a Cathedral entirely in my brain; smaller pieces just work better, and even from the beginning I wanted something that lots of other people could easily start using and contribute to.

The fact that my focus has drifted away from IDE integration is also a product of my limited time and brain power; once I get a basic EC# compiler working, I can perhaps figure out how to install it in an IDE. Macros are a major challenge for IDE integration, since they can use unlimited time and, thanks to Microsoft's refusal to give .NET apps any control .NET's allocator, unlimited memory too.
