---
layout: post
category : misc
title:  "Blogging on GitHub"
tagline: "At least it's better than Blogspot"
tags: [jekyll, tutorial]
---
GitHub has a "built-in" simple content management system called Jekyll. It's unobtrusive; you can put ordinary HTML files in your webspace and they will be served unchanged, or you can create Jekyll files, which are text files that start with a header block that the Jekyll documentation calls "front matter" (a phrase that the documentation uses as if they expect you to already know what it means). Among other things, Jekyll allows you to write web pages and blog posts in Markdown. And since it's GitHub, you won't be surprised to learn that your web space is version-controlled with Git, which means that you can update your web site with an ordinary Git push.

The way GitHub decided to organize the web space is bizarre; it's based on an "orphan branch" within the _same_ repository as your project, which is basically a "parallel universe" within the same, well, universe as the repo you already have. This means that you typically have to clone your repository _twice_ on the same PC, once for your code and again for your web site, but you are storing two complete copies of the history of your repo. Weird. (In theory you only _need_ one clone of your repo, but then git would have to delete your entire source tree whenever you want to edit your web site. Unsettling. Why can't I put the web site in a subfolder, let's say, `/www` in `master`?)

They recommend installing [Jekyll](http://jekyllrb.com/) on your local computer to be able to preview your web site, but installing Jekyll on Windows was a slight pain in the ass. The Jekyll gem normally fails to install; you have to install something called Ruby Installer DevKit first. Here's a hint, because it took me awhile to find the download link to this thing. It turns out that the DevKit download link is on [this page](http://rubyinstaller.org/downloads/) underneath the download links for the Ruby Installer for Windows, under the heading "Development Kit".

The documentation of Jekyll is backwards. The introductory pages give you all the minor details first; the key information comes later. For example, it isn't until the [eighth section](http://jekyllrb.com/docs/posts/) that they finally tell you how to add a blog post. But that's not enough of course, you also need to know how to create a "main page" for your blog and a "history page", and all they provide at first is an incomplete template for part of a history page. Plus, any good web site should have category links on every page (e.g. Main Page, Blog, Documentation, Code) but they don't give you any clues about setting that up until way down deep in the docs. Styling & theming? In the docs, I haven't found anything about that yet.

Instead, it appears that a much better way to get started with Jekyll is to use [Jekyll Bootstrap](http://jekyllbootstrap.com/). The [Quick Start page](http://jekyllbootstrap.com/usage/jekyll-quick-start.html) tells you exactly how to set it up, and it does a better job explaining what Jekyll is and how it works than the Jekyll documentation. Then '[Using Themes](http://jekyllbootstrap.com/usage/jekyll-theming.html)' tells you how to set a theme (because the default theme is ... well, it has _big red headings_! Not my cup of tea), and once you've worked out those two things you can pretty much start blogging in your site's `_posts` folder.

Another source of themes is [this web site](http://jekyllthemes.org/). Although these other themes aren't designed to work within the Jekyll Bootstrap framework, if you're familiar with the building blocks of the web (HTML/CSS) and Jekyll (basically just a bunch of code snippets in a folder with "front matter"), you should be able to figure out how to install one of those themes.

So once you figure out how to install Jekyll and a theme and you've written a dummy blog post and/or web page, you'll want to preview it, and that means running Jekyll. My Jekyll site is in a Loyc-web folder on my PC and I want to create the preview web site output in a Loyc-preview folder. So I make a batch file (shell script) alongside the Loyc-web folder that contains this command:

    jekyll serve --drafts --watch --source Loyc-web --destination Loyc-preview

`serve` (rather than `build`) causes it to serve the preview at `http://localhost:4000`, `--drafts` asks Jekyll to render unpublished drafts and `--watch` causes the preview to be automatically updated in response to changes. The default `--destination` is `_site` within the source folder. And don't name your batch file `jekyll.bat` like I did. You won't like the result...

Once I'm happy with my web site, I push to GitHub and I'm live! But what about comments? Although Jekyll supports blogging, it is incomplete as a blogging engine since it is strictly designed to serve static content, which comments are not. Well, I'll get back to you on that.

By the way, no matter whether you're using GitHub or not or Jekyll or not, there's no need to write web sites in HTML anymore. No matter how crappy your web hosting provider might be, no matter whether you're allowed to run scripts or not, you can still author pages in Markdown, thanks to a nifty library called [mdwiki](http://dynalon.github.io/mdwiki/#!index.md). This thing uses Javascript to convert markdown to HTML, 100% client-side, so you don't have to worry about what your web host may or may not support. On GitHub, you may as well use Jekyll, but I must admit, it looks like mdwiki has a fantastic feature set, probably better than I'll get with Jekyll. But the important thing is, I don't have to write HTML anymore. Good riddance!

You can even use MDWiki without a web server, if you view mdwiki.html in Firefox (it doesn't work in Google Chrome), which means you can use it for offline Markdown previews (which is great because, at least on the Windows, tools for editing Markdown are generally quite limited.)
