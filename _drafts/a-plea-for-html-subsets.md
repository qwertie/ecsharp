---
title: A plea for html subsets
layout: post
---
Hi folks. I'm not involved in Servo development and may be repeating thoughts you might have had already; if so, sorry for the redundancy.

Today there is one document format that is used more and understood better than any other. It's not docx, it's not odf, it's not pdf and it's obviously not ps, rtf or latex. The single most recognized and used document format is, of course, html. In fact, today it is used not just for documents but for entire user interfaces for "web" applications.

However, HTML has become a standard of enormous size. Although many people still write HTML by hand, very few people fully understand the HTML/CSS standards or are familiar with all its capabilities. While many people have a vague idea how basic functionality works (the box model, how CSS cascades, how floats work, etc.), very few people who don't write web browsers for a living are intimately familiar with the details.

As a mostly-non-web-developer, I think that the HTML layout and formatting model should not be limited to the web browser (or even, necessarily, to HTML/CSS.) But because HTML/CSS are such enormous standards, in practice they are only available in the form of web browsers and web browser "controls" or "widgets". Most programming languages do not have libraries natively written in that language to correctly render even a small subset of HTML, and do not offer a comfortable "native" way to manipulate and render HTML/CSS.

I propose that the world needs defined subsets of HTML (or rather XHTML, since most languages support XML already), with publically available, technical (but still easy-to-read) standards, that thoroughly describe these subsets. I would suggest two subsets, covering perhaps 5% and 20% of HTML and CSS. These subsets would omit all the fancy stuff (drop shadows, most of the pseudo-selectors, etc.) and go "back to basics" without going "back in time".

Subsets of HTML5/CSS3 would have many potential uses:

- As a rich-text control with more capability than the typical ones (not just bold/italic/bullet points but also floats and images) without the "heft" of a web browser control
- As a lightweight rendering standard for low-speed, low-memory, and low-power devices (feature phones, smart watches, solar-powered laptops for developing nations)
- As the starting point for a graphical terminal / command line (terminals ought to be lightweight, but it's way past time to replace the text-only 80x25 grid).
- As the basis (not the whole story, but a starting point) for cross-platform UI libraries for various programming languages. Such a library needs HTML-like capabilities without actually being a web browser.
- Allowing more programming languages to support HTML/CSS manipulation (metaprogramming) and rendering in a comfortable way that feels "at home" in that language, rather than as a limited interface to a C++ browser API. For this to happen it must be possible for people without immense resources to implement the specification.

I think the people in the best position to define standard subsets today might just be the people who are writing a new web browser: Mozilla's Servo. You guys are becoming more familiar with web standards than almost anyone else, and since your browser is new you have the freedom to refactor it however you want to. Thus, you are in a position to structure the web browser itself into modules that could be designed to correspond to subsets of HTML. You guys have the power to create a series of 'crates' that represent subsets of HTML, such that other people could embed HTML subsets in their Rust applications.

This would allow people to ship programs statically linked to subsets of Servo. These programs could be much smaller than full web browsers, yet they would be able to render a majority of XHTML pages. These subsets would serve both as useful libraries in themselves, and also as reference implementations for the XHTML subsets you define.

Comments?