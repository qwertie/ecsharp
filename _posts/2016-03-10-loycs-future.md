---
title: "Loyc's future"
layout: post
---

I don't know whether I want to continue using .NET at all.

For one thing, the developers are remarkably ... I don't know, un-comp-sci. My ["optionally persistent" data structure library](http://core.loyc.net/collections) has been completely ignored, it's becoming clear that .NET devs ignore stuff like [LeMP](/lemp) too. I guess if something comes out of Redmond itself, people will be clamoring to write books about it, but the free open source landscape seems a little... forlorned. Either that or I don't know where to find the good stuff. Did everyone move on from CodeProject when I wasn't looking?

Now, I've noticed Microsoft's various strategic and technical mistakes lately, many of which cannot be corrected. I'm thinking of WPF (with its learning cliff and at-times careless design); WCF (as lacking in generality as it is enormous); the splitting of MS technology into separate .NET and Metro branches (which I heard was caused by internal politics); the [design flaws of the CLR itself](http://loyc.net/2014/dotnet-annoyances.html), which MS does not intend to fix; obvious mistakes that doomed Windows 8 to have slow uptake, other mistakes that doomed Windows Phone .... now, combine all this with whatever kept C# out of academia (the historical distrust/hatred of MS? it wasn't the lack of Linux support, since there was always Mono), and I feel that the platform is stagnating.

Plus there's this Wasm thing, and I'd like to be part of a movement to define CLR-like infrastructure for it: garbage collection, sure, but above all, interoperability features. Not just any interoperability features, but something much more ambitious than the CLR in terms of the variety of potentially supported languages.

I'm not sure what angle to take on it. Should I focus on the MLSL? On the binary interoperability aspect? On the programming language angle? I wish I could do it all. Maybe we could do a little bit of each, as a seed upon which a community might grow. But what programming language should that seed be written in? Well, wasm is focused on the web, and node.js is hugely popular. Do I dare switch to Javascript? Hmm... is there such a thing as IntelliSense for JS? I miss it when I code in EC#. Maybe Typescript?

Besides all that, I would so much like to work on [Bret Victor's ideas](http://worrydream.com/#!/LearnableProgramming)...

<iframe width="560" height="315" src="https://www.youtube.com/embed/PUv66718DII" frameborder="0" allowfullscreen></iframe>

Maybe I'll have to push Wasm to have the features that would be needed to create (1) advanced debugger tech to allow Bret Victor's prototype to scale up, and (2) advanced programming languages with things like runtime codegen, optional GC and sub-sandboxing (one module restricting what another module is allowed to do, such as restricting memory allocation and "unsafe" code). The Wasm folks are so focused on supporting C++ right now that they could easily miss some of the basic design elements they'll need in the future. For example, so far I've seen no sign of run-time codegen in MVP. Why not? It seems like an easy thing to include.

In any case, the Loyc home page will have to be revamped to more clearly show visitors a new Wasm-centric vision. But if I do that... where should the old Loyc .NET stuff go? Well, I still own ecsharp.net and lllpg.com. I could shift it all over there.
