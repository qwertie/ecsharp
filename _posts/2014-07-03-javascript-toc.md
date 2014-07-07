---
layout: post
title: Building a table of contents in Javascript
toc: true
---
{% raw %}
So you're publishing a long document online and don't have an easy mechanism to automatically add a table of contents on the server side? Well with Javascript, you enslave the web browser to do it instead! This TOC generator...

- needs no jQuery or other third-party library
- creates links to each heading, adding an `id` attribute to each heading that doesn't already have one
- can support pages that contain multiple content areas with multiple unrelated tables of contents

## Usage

After installing the code below, call `addTOC(contentElement, before, tocClass)`, where

- `contentElement` is an element that contains the document or blog post for which you want a table of contents, and
- `before` is an immediate child of `contentElement`; the table of contents will be inserted as a child of `contentElement`, just before `before`. `before` is optional and if unspecified, `contentElement.firstChild` is used.
- `tocClass` is the CSS class of the table-of-contents blog (default value: "sidebox")

See example below.

## The _code_

Insert this code at the bottom of any page in which you want to add a TOC (before `</body>`). 

~~~html
<script>// Add table of contents! <![CDATA[
function $get(selector) { return document.querySelector(selector); };
function $all(selector) {
	  return Array.prototype.slice.call(document.querySelectorAll(selector));
}

function buildTOC_ul(selector) {
	  var levels=[document.createElement("ul"),null,null];
	  levels[0].style
	  var lvl=0, c=0;
	  if (!selector) selector = "h2, h3, h4";
	  $all(selector).forEach(function(el) {
			if (!el.id) el.id='section_'+ ++c;
			var newLvl=(el.tagName=="H2"?0:el.tagName=="H3"?1:2);
			for (;lvl<newLvl;lvl++)
				 levels[lvl].appendChild(levels[lvl+1]=document.createElement("ul"));
			lvl=newLvl;
			
			var li=document.createElement('li');
			li.innerHTML="<a href='#"+el.id+"'></a>";
			li.firstChild.innerHTML=el.innerHTML;
			levels[lvl].appendChild(li);
	  });
	  return levels[0];
}
function addTOC(contentElement, before, tocClass) {
	  if (before===undefined) before=contentElement.firstChild;
	  var prefix = "";
	  if (contentElement.className) prefix="."+contentElement.className+" ";
	  var selector = prefix+"h2, "+prefix+"h3, "+prefix+"h4";
	  var toc=document.createElement("div");
	  toc.className=tocClass||"sidebox";
	  toc.appendChild(document.createTextNode("Contents"));
	  toc.appendChild(buildTOC_ul(selector));
	  contentElement.insertBefore(toc, before);
}
// =========================
// TODO: CALL addTOC() HERE!
// =========================
//]]>
</script>
~~~

## Example

The simplest thing you can do is to to call `addTOC(document.body)`, but that only works on very simple pages. Generally the page has a main content area; if you assign an `id="content"` to this content area, you can add the table of contents at the top like this:

~~~js
addTOC($get("#content"));
~~~

(See definition of `$get` in the code above.) Here's how I'm calling `addToc()` on my blog:

~~~js
var _post_ = $get("#post") || $get("#content");
addTOC(_post_, _post_.firstChild.nextSibling.nextSibling);
~~~

This allows the content area to have `id="post"` or `id="content"`. `_post_.firstChild.nextSibling.nextSibling` skips past the initial heading so that the table of contents is inserted underneath the heading.

## Sidebar style

The default class is `sidebox`, and if you insert the following CSS on your page, you will get a table of contents that floats beside the content, just as you see on [this page](http://loyc.net/2014/07/javascript-toc.html).

~~~html
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
.sidebox ul { padding:0em 0em 0em 1.3em; margin:0em; }//TRBL
</style>
~~~

## Usage with GitHub Pages / Jekyll

If you're publishing with Jekyll, I suggest adding the above code to the bottom of `/_layouts/default.html` (and any other layouts you might use that might need a TOC, as long as they do _not_ import `default.html` using a `layout: default` option), just before `</body>`, surrounded by a test like this:

    {% if page.toc %}
    <script>
    ...
    ...
    var _post_ = $get("#post")||$get("#content");
    addTOC(_post_, _post_.firstChild.nextSibling.nextSibling);
    </script>
    {% endif %}

This way, Jekyll only adds the TOC code to a page if the front-matter contains a `toc: true` option.
{% endraw %}
<a href="http://www.codeproject.com/script/Articles/BlogArticleList.aspx?amid=3453924" rel="tag">Published on CodeProject</a>. Comments? Leave them there.