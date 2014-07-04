---
# Jekyll front matter
layout: post
title:  "Blogging on GitHub"
tags: [jekyll, tutorial]
commentIssueId: 1
paragraphLinks: true
toc: true
---
{% raw %}
GitHub has a "built-in" simple content management system called Jekyll. It's unobtrusive; you can put ordinary HTML files in your webspace and they will be served unchanged, or you can create Jekyll files, which are text files that start with a header block that the Jekyll documentation calls "front matter" (a phrase that the documentation uses as if everyone knows what it means already). Among other things, Jekyll allows you to write web pages and blog posts in Markdown. And since it's GitHub, you won't be surprised to learn that your web space is version-controlled with Git, which means that you can update your web site with an ordinary Git push.

<style>
.sidebox {
  border: 1px dotted rgb(127, 127, 127);
  padding: 4px 3px 4px 6px; // top right bottom left
  min-width: 100px ! important;
  float: right ! important;
  font-size: 90%;
  margin-top: 1px;
  margin-bottom: 1px;
  margin-left: 6px;
  visibility: visible;
  max-width: 50%;
  width: 35%;
}
</style>

The way GitHub decided to organize its [web space](https://pages.github.com/) is unusual; it's based on an "orphan branch" within the _same_ repository as your project, which is basically a "parallel universe" within the same, well, universe as the repo you already have. This means that you typically have to clone your repository _twice_ on the same PC, once for your code and again for your web site, but you are storing two copies of the complete history of both "halves" of your repo. That is weird to me. (In theory you only _need_ one clone of your repo, but then git would have to delete your entire source tree whenever you want to edit your web site. Unsettling, no? Why can't I just have the web site in a subfolder, let's say, `/www` in `master`?)

They recommend installing [Jekyll](http://jekyllrb.com/) on your local computer to be able to preview your web site, but installing Jekyll on Windows was a pain in the ass. The Jekyll gem normally fails to install; you have to install something called Ruby Installer DevKit first. Here's a hint, because it took me awhile to find the download link to this thing. It turns out that the DevKit download link is on [this page](http://rubyinstaller.org/downloads/) underneath the download links for the Ruby Installer for Windows, under the heading "Development Kit". Later I found [instructions for installing Jekyll on Windows](https://github.com/juthilo/run-jekyll-on-windows). But then I had problems with "yajl" and "wdm" on two different PCs (see sidebar) and one of the PCs still can't run github's (old) version of Jekyll.

<div class="sidebox">If Jekyll won't start because it's babbling about something called "yajl", you might have to uninstall "yajl" it and reinstall it with
<pre>
rem If github still uses jekyll 1.5.1, the yajl version is v1.1.0
gem uninstall yajl-ruby
gem install yajl-ruby -v 1.1.0 --platform=ruby
</pre>
And in this situation it seems like bundler might cause a problem with Ruby-2.0.0 so if in doubt, don't use bundler to start jekyll. Finally, if jekyll won't start and says "cannot load such file -- wdm", run <tt>gem install wdm</tt>.
<br/><br/>
After I finally got Jekyll all working and rendering this site, I pushed it to github. GitHub sent an email that my site "contains markdown errors". What errors? Who knows, they don't tell you! But of course it worked fine for me, so what the hell was wrong? So I got to work installing Jekyll on a new machine, and this time I took care to install github's version of Jekyll (1.5.1) instead of the latest version, by installing the <tt>github-pages</tt> gem instead of the normal jekyll gem.
<br/><br/>
On the new machine I had to work out several errors starting github's version of Jekyll--errors with yajl, the error with wdm--and then finally I'm told "There was an error converting '_posts/2014-06-29-blogging-on-github.md'." (i.e. this post). Which brings us to the worst thing about Jekyll: <b>terrible error handling</b>. An error with one page usually brings down the whole site (although not in this case), and you're lucky if you even get a filename from the damn thing. So what was causing this new error message? Code blocks. Every code block causes a "conversion error" on my machine. What kind of error? Ha ha, as if Jekyll would tell <i>you</i>.
<br/><br/>
So in desperation I installed github-pages on another machine and, when invoking jekyll, forced version 1.5.1 by running <tt>jekyll _1.5.1_</tt> on the command line instead of <tt>jekyll</tt>. After getting errors about `yajl` and `wdm` yet again, and fixing those, the error stack trace changed to involve <tt>pygments/mentos.rb, line 303, in start</tt>.
<br/><br/>
By deleting text from my post, section-by-section, I finally found the problem: I had written <tt>~~~none</tt> to disable syntax highlighting, when I should have used <tt>~~~text</tt>.
<br/><br/>
Once everything starts working, Jekyll still proves to be flaky. Sometimes it spews several errors on startup, the last one being <tt>jekyll 2.1.0 | Error:  no implicit conversion of true into String</tt>.
<br/><br/>
Solution: just run Jekyll again and Hope It Works This Time.
</div>

The [documentation of Jekyll](http://jekyllrb.com/docs/home/) is backwards. The introductory pages give you all the minor details first; the key information comes later. For example, it isn't until the [eighth section](http://jekyllrb.com/docs/posts/) that they finally tell you how to add a blog post. But that's not enough of course, you also need to know how to create a "main page" for your blog and a "history page", and all they provide at first is an incomplete template for part of a history page. Plus, any good web site should have category links on every page (e.g. Main Page, Blog, Documentation, Code) but they don't give you any clues about setting that up until way down deep in the docs. What about styling & theming? In the docs, I haven't seen anything about that yet.

Instead, it appears that a much better way to get started with Jekyll is to use [Jekyll Bootstrap](http://jekyllbootstrap.com/). The [Quick Start page](http://jekyllbootstrap.com/usage/jekyll-quick-start.html) tells you exactly how to set it up, and it does a better job explaining what Jekyll is and how it works than the Jekyll documentation. Then '[Using Themes](http://jekyllbootstrap.com/usage/jekyll-theming.html)' tells you how to set a theme (because the default theme is ... well, it has _big red headings_! Not my cup of tea), and once you've worked out those two things you can pretty much start blogging in your site's `_posts` folder.

Another source of themes is [this web site](http://jekyllthemes.org/). Although these other themes aren't designed to work within the Jekyll Bootstrap framework, if you're familiar with the building blocks of the web (HTML/CSS) and Jekyll (basically just a bunch of code snippets in a folder with "front matter"), you should be able to figure out how to install one of those themes.

But there's another framework for theming Jekyll called [Poole](http://markdotto.com/2014/01/02/introducing-poole/) and the two available themes not only look good, they scale very well to various browser sizes. Mobile? Check. I found out about Poole through [a helpful blog post](http://joshualande.com/jekyll-github-pages-poole/) by Joshua Lande.

So once you figure out how to install Jekyll and a theme and you've written a dummy blog post and/or web page, you'll want to preview it, and that means running Jekyll. My Jekyll site is in a Loyc-web folder on my PC and I want to create the preview web site output in a Loyc-preview folder. So I make a batch file (shell script) alongside the Loyc-web folder that contains this command:

    jekyll serve --drafts --watch --source Loyc-web --destination Loyc-preview

`serve` (rather than `build`) causes it to serve the preview at `http://localhost:4000`, `--drafts` asks Jekyll to render unpublished drafts as if they were published, and `--watch` causes the preview to be automatically updated in response to changes (this works on one of my PCs but not another, I don't know why). The default `--destination` is `_site` within the source folder. And don't name your batch file `jekyll.bat` like I did. You won't like the result...

Once I'm happy with my web site, I push to GitHub and I'm live!

## Syntax highlighting fails

On Windows you need a bunch of extra install steps to install "pygments" in order to support syntax highlighting with the widest number of languages, but it's easier to use "Rouge" instead. I have found that if you have _neither_ Pygments nor Rouge installed, Jekyll will crash with a strange message when it encounters a page that requests syntax highlighting:

    Liquid Exception: cannot load such file -- yajl/2.0/yajl in _posts/2014-01-01-example.md
    C:/Dev/Ruby200/lib/ruby/2.0.0/rubygems/core_ext/kernel_require.rb:55:in `require': 
    cannot load such file -- yajl/2.0/yajl (LoadError)

No, Jekyll isn't good at error messages. To solve this quickly, 

1. Install Rouge: `gem install rouge`
2. Remove the `pigments` option from your `_config.yml`, if there is one
3. Add this option: `highlighter: rouge`
4. If applicable, restart `jekyll serve` whenever you change `_config.yml`

To create code blocks, Jekyll recommends the use of `{%...%}` blocks like this:

~~~text
{% highlight cpp %}
// Code
{% endhighlight %}
~~~

But this, of course, is not standard Markdown code. I want to write my posts in the language of Markdown, not Jekyll; I should be able to use a standard "fence" instead:

    ~~~cpp
    // Code
    ~~~

But by default, syntax highlighting is not supported in fences. To enable this feature, change your Markdown parser from "kramdown" to "redcarpet" by setting the `markdown` option in _config.yml:

    markdown: redcarpet

The names you have to use on the fence tend to be longer than just file extensions. For example, `~~~cs` is not recognized as C#, you have to use `~~~csharp`, and `~~~yml` isn't recognized as `.yml`, you have to use `~~~yaml`. Use `~~~text` to ensure that no language auto-detection occurs (GitHub's old version of Jekyll doesn't do that, though.)

Unfortunately, redcarpet and kramdown have different sets of advanced features. Kramdown seems more flexible to me, but redcarpet appears to be [The Standard GitHub Flavored Markdown](https://github.com/blog/832-rolling-out-the-redcarpet).

**ProTip**: Jekyll won't easily let you write the literal character combination `{%` or `{{`, not even inside code blocks. You could write `{{"{%"}}` or `{{ "{{" }}` instead, but if you are not intending to use Liquid (Jekyll's templating ending), a better option is to wrap the entire page in `{% raw %} ... {% endraw %}{{"{%"}} endraw %}`, after the front-matter, as I have done in this post. One problem with this though: you can't use [jekyll internal links](http://stackoverflow.com/questions/4629675/jekyll-markdown-internal-links).
{% raw %}

## How I set up this site

I decided to use the Poole-based "Hyde" theme, so I downloaded [`hyde-master.zip` from GitHub](https://github.com/poole/hyde/). The provided Usage instructions are a bit confusing and incomplete so I wrote this in case it helps. I added the prefix "(Hyde)" on issues that _only_ apply to Poole or Hyde.

1. Earlier I generated my index.html using GitHub's "Automatic Page Generator". This thing generates not just `index.html` but a bunch of css files and images and javascript files, plus a file in the root directory called `params.json` which contains a description of the content you provided for `index.html`, including the page text. Since "Hyde" uses a different folder structure than the "Automatic Page Generator" and doesn't share any of the same css, I thought I would confuse myself if I kept both... so I deleted all of GitHub's generated stuff, such as the `css` folder, the `images` folder, `params.json` (which had the same content as my README anyhow), `_includes/header.html`, `_includes/footer.html`, and `index.html` itself.

2. (Hyde) After unzipping `hyde-master.zip` I copied most of its contents over my `gh-pages` working copy, *excluding* .gitignore, _config.yml, README.md, CNAME, README.md, LICENSE.md (lest it be confused with the license of my site content), and the _posts folder.

3. (Hyde) Hyde expects the following options to exist in `_config.yml`:

        title: "Project Name"         # shown in large text
        description: "Blah blah blah" # shown under the title
        tagline: "Blah blah blah"     # shown in title bar beside the title
        version: "1.2.3"              # "Currently v1.2.3"
        url:     "http://loyc.net"    # Used by the Atom feed
        baseurl: ""                   # URL of site relative to domain
        github:
          repo:  https://github.com/user/Proj # "GitHub project" link on sidebar

    Other themes also use `title`, `description`, `baseurl` and possibly other options. Here are some settings recognized by Jekyll itself that you should probably also have:

    ~~~yaml
    # Jekyll settings:
    markdown: redcarpet
    encoding: UTF-8                
    highlighter: rouge             # if you did not install pygments
    paginate: 10                   # blog posts per page
    paginate_path: "blog/page:num" # optional, see below
    safe: true
    lsi: false
    ~~~

4. (Hyde) The `baseurl` option is a problem if you want it to refer to the root of the domain. The `Jekyll` documentation implies you should use "" in this case, and offers sample code that doesn't work correctly with "/"; for example, this code is in the Jekyll site template (`Ruby200\lib\ruby\gems\2.0.0\gems\jekyll-2.1.0\lib\site_template`):
    
    ~~~html
    <ul class="posts">
      {% for post in site.posts %}
        <li>
         <span class="post-date">{{ post.date | date: "%b %-d, %Y" }}</span>
          <a class="post-link" href="{{ post.url | prepend: site.baseurl }}">{{ post.title }}</a>
        </li>
      {% endfor %}
    </ul>
    ~~~
    
    For some reason this produces bogus links if baseurl is "/". But Hyde will break if you use "/" because it uses raw concatenation in `/_includes/head.html`:
    
    ~~~html
    <link rel="stylesheet" href="{{ site.baseurl }}public/css/poole.css">
    <link rel="stylesheet" href="{{ site.baseurl }}public/css/syntax.css">
    <link rel="stylesheet" href="{{ site.baseurl }}public/css/hyde.css">
    ~~~

    Note that a slash is needed before '`public`'. To fix this I changed all instances of `{{ site.baseurl }}` to `{{ site.baseurl }}/` in all of Hyde's HTML files (be sure to replace _all_ of them!)
    
    Perhaps the same problem would arise if you are _not_ at the root of the domain; i.e. if your web space is at `username.github.io/projectname`. In that case Jekyll wants `baseurl` to be <i>projectname</i> while Hyde expects <i>projectname/</i>.
    
5. By default, the home page (`index.html`) just shows your blog posts, in full, in chronological order. I didn't want the blog to be the home page so I renamed `index.html` to `blog.html` (giving it the front-matter `layout: page` and `title: Blog`), but this [caused all the blog posts to disappear](http://stackoverflow.com/questions/21248607/jekyll-pagination-on-every-page)! Jekyll has a bizarre limitation here: you are only allowed to show your blog on a single page, and that page **must** be named `index.html`. However, you can use `/blog/index.html` instead if you add the line `paginate_path: "blog/page:num"` to `_config.yml`.

6. (Hyde) By default, the sidebar lists all pages that use the `layout: page` option in their front-matter (in `_includes/sidebar.html` you can see the for-loop that does this), and the `title: ...` option is used as the link text. After that there is a "Download" link, a "GitHub project" link, and a version number. I did not want the "Download" link, or the version number, or "All Rights Reserved", so I removed those parts from `_includes/sidebar.html`. Of course, if you don't like how the links are auto-generated, you can change the code to suit you.

7. I created `index.md` and put some content in it:

        ---
        layout: default
        title: Home
        ---
        # Welcome!
        This is the home page!

8. I added my `.sidebox` class to `sidebar.html` (see [Using custom CSS](#customcss) below)

9. (Hyde) I switched to a different preset color scheme by changing `<body>` to `<body class="theme-base-0d">` in `/_layouts/default.html`.

10. (Hyde) I edited `/public/css/hyde.css` to adjust the theme to my liking. In particular I reduced the various `margin-left` and `margin-right` values, shrunk the font a little, and tweaked the sidebar color. Since this blog contains code (and what GitHub blog doesn't?) wide margins would cause scroll bars or wrapping on every code snippet, and we don't want that.

11. At the top of the blog I made a "List of all posts" link, which goes to `blog-list.html`:

    ~~~html
    ---
    layout: default
    title: Blog index
    ---
    <h1>Blog index</h1>
    <div class="home">
      <ul class="posts">
        {% for post in site.posts %}
          <li>
            <span>{{ post.date | date: "%b %-d, %Y" }}</span>:
            <a class="post-link" href="{{ post.url | prepend: site.baseurl }}">{{ post.title }}</a>
          </li>
        {% endfor %}
      </ul>

      <p class="rss-subscribe">subscribe <a href="{{ "atom.xml" | prepend: site.baseurl }}">via Atom</a></p>
    </div>
    ~~~

12. (Hyde) if you have enough blog posts to cause pagination, Hyde shows the "Blog" link multiple times, one for each page! This turned out to be slightly difficult to fix; after a little while I realized that Jekyll, or more specifically __Liquid__, __does not support Ruby expressions__. In fact, it hardly supports any expressions at all! Check it out: [Liquid contains only 9 operators __in total__](http://docs.shopify.com/themes/liquid-basics/operators). There is not even a `not` operator, and there is no regex matching (or if there is, it is implemented as something other than an operator.) Anyway, the unwanted pages can still be filtered out, although not with the precision I am used to. In `_includes/sidebar.html`, I changed this code:

    ~~~
    {% assign pages_list = site.pages %}
    {% for node in pages_list %}
      {% if node.title != null %}
        {% if node.layout == "page" %}
          <a class="sidebar-nav-item{% if page.url == node.url %} active
              {% endif %}" href="{{ node.url }}">{{ node.title }}</a>
        {% endif %}
      {% endif %}
    {% endfor %}
    ~~~
    by adding an `unless` statement:
    
    ~~~
    {% assign pages_list = site.pages %}
    {% for node in pages_list %}
      {% if node.title != null and node.layout == "page" %}
        {% unless node.url contains '/page' %}
          <a class="sidebar-nav-item{% if page.url == node.url %} active
              {% endif %}" href="{{ node.url }}">{{ node.title }}</a>
        {% endunless %}
      {% endif %}
    {% endfor %}
    ~~~

13. (Hyde) The Older and Newer buttons had broken links because they assumed the blog would be the home page. And you know what, I made so many little changes and fixes, I won't list them all. Let's just say you can clone my repo and make it your own, if you like. And [see here](http://jekyllrb.com/docs/permalinks/) about customizing blog permalinks (why call it "permalink" when you can change the URL schema at any time?)

## Writing blog posts

Writing a blog post in Jekyll is super easy. Just create a text file in the `/_posts` folder (that's in your `gh-pages` branch if you are using GitHub) and give it a name like `2014-12-31-file-name.md`, but with the correct date, of course. Inside the file, add front matter:

~~~
---
title: Title of your blog post
layout: post
---
The content of the post goes here.
~~~

Jekyll puts the output HTML in `2014/12/31/file-name.html`, or `2014/12/31/file-name/index.html` instead if your site is using the `permalink: pretty` option in `_config.yml`.

## Comments

Although Jekyll supports blogging, it is incomplete as a blogging engine since it is strictly designed to serve static content, which comments are not. And there's no way to have comments directly in GitHub pages, since GitHub provides no place to store the comments. But, it is still possible to use a comment service provided by a third party such as Disqus. Disqus adds ads to make money for them, though: annoying ads with pictures.

[Jekyll Bootstrap](http://jekyllbootstrap.com/) currently supports four comment engines including Disqus but I am not sure whether I trust these commercial entities with my comments... although, er, very few comments have ever been left on this blog.

Hopefully GitHub itself will provide a comment engine someday, since the GitHub site obviously supports comments very well. Good news though! It's possible to co-opt the GitHub issue tracker to manage blog comments! [The technique is described here](http://ivanzuzak.info/2011/02/18/github-hosted-comments-for-github-hosted-blogs.html) and I decided to use it! There's no need to limit comments to blog posts either, I can put them on any page I want, although it does take a little extra effort.

How to set up:

1. Add configuration to `/_config.yml`:
    
    ~~~yaml
    github:
      user: qwertie
      project: loyc
    ~~~

2. In `_layouts/default.html`, include `comments.html` below the content:
    
    ~~~
        ...
        {{ content }}
        {% include comments.html %}
      </div>
    </body>
    ~~~

3. Add my copy of [`comments.html`](https://github.com/qwertie/Loyc/tree/gh-pages/_includes/comments.html) unchanged to your `/_includes` folder, and add [`comments.css`](https://github.com/qwertie/Loyc/blob/gh-pages/res/css/comments.css) unchanged to `/res/css` (oh, you don't have a `/res/css` folder? Either create that folder, or put `comments.css` wherever you like to put css files and change `comments.html` to point to the new location.)

4. To add comments to a page, create an issue in your github project (with whatever name and description you want) and then set `commentIssueId` line in the front matter of your blog post to match the issue:
    
    ~~~yaml
    ---
    layout: post
    title:  "Blogging on GitHub"
    commentIssueId: 42
    ---
    ~~~

5. [Ivan says](http://ivanzuzak.info/2011/02/18/github-hosted-comments-for-github-hosted-blogs.html) you also have to register an "OAuth application", so I did that for `loyc.net`, but comments are still working when I look at my Jekyll preview at `localhost:4000`.

## <a name="customcss"></a>Using custom CSS in Markdown

Sometimes I like to place sidebars in my posts, which I call ".sidebox" in CSS because some websites already have a ".sidebar" class to describe the _main_ sidebar:

~~~css
<style>
.sidebox {
  border: 1px dotted rgb(127, 127, 127);
  padding: 4px 3px 4px 6px; // top right bottom left
  min-width: 100px ! important;
  float: right ! important;
  font-size: 90%;
  margin-top: 1px;
  margin-bottom: 1px;
  margin-left: 6px;
  visibility: visible;
  max-width: 50%;
  width: 35%;
}
</style>
~~~

<div class="sidebox"><p><b>Hey, look over here!</b></p> This is a <i>sidebar</i>.</div>
Markdown supports HTML, so you can add a style block like this one at the top of a `.md` file or in your shared `_includes\head.html` file, and then use it like this:

~~~
<div class="sidebox">This is a <i>sidebar</i>.</div>
~~~

Note that all the text inside the `<div>` tag is treated as HTML, not Markdown. In kramdown you can also use the "block attribute" `{: .sidebox}` after a paragraph to create a sidebar, or use a "refdef" which allows the sidebox to contain multiple paragraphs. Both of these approaches are better because you can use Markdown syntax inside the sidebar... but also worse because only kramdown will understand your code.

## Visitor data

I am using [Google Analytics](http://www.google.ca/analytics/) to track page views. After you sign up you'll be give a Javascript snippet to put in your web pages; I inserted mine right before `</body>` in `/layouts/default.html`.

## Table-of-contents generation

Kramdown has a feature for [automatic generation of a table of contents](http://kramdown.gettalong.org/converter/html.html#toc) for long posts like this one. Sadly I'm not using Kramdown so I don't have access to it. A javascript solution is more universal, but when I searched for a solution I didn't immediately find one I was happy with. So I made my own [Javascript table-of-contents builder](/2014/07/javascript-toc.html).

## Markdown everywhere

By the way, no matter whether you're using GitHub or not or Jekyll or not, there's no need to write web sites in HTML anymore. No matter how crappy your web hosting provider might be, no matter whether you're allowed to run scripts or not, you can still author pages in Markdown, thanks to a nifty library called [mdwiki](http://dynalon.github.io/mdwiki/#!index.md). This thing uses Javascript to convert markdown to HTML, 100% client-side, so you don't have to worry about what your web host may or may not support. On GitHub, you may as well use Jekyll, but I must admit, it looks like mdwiki has a fantastic feature set, probably better than I'll get with Jekyll. But the important thing is, I don't have to write HTML anymore. Good riddance!

You can even use MDWiki without a web server, if you view `mdwiki.html` in Firefox (it doesn't work in Google Chrome), which means you can use it for offline Markdown previews (which is great because, at least on the Windows, tools for editing Markdown are generally quite limited.)

{% endraw %}
<a href="http://www.codeproject.com/script/Articles/BlogArticleList.aspx?amid=3453924" rel="tag" style="display:none">Published on CodeProject</a>