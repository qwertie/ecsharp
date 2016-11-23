---
title: "LeMP Macro Reference: on_return, on_throw, on_throw_catch, on_finally"
tagline: Standard macros in the LeMP namespace
layout: article
date: 20 Mar 2016
toc: true
---

Introduction
------------

The D programming language has a really nice feature that makes error handling easier and less, well, error-prone. It's called the `scope` statement, and it's the inspiration for the "`on_`" statements described here. On StackOverflow, a user explains how his code is shorter and more readable thanks to the `scope` statement:

<div class='sbs' markdown='1'>
~~~csharp
sqlite3* db;
sqlite3_open("some.db", &db);
scope (exit) sqlite3_close(db);

sqlite3_stmt* stmt;
sqlite3_prepare_v2(db, 
  "SELECT * FROM foo;", &stmt);
scope (exit) sqlite3_finalize(stmt);

// Lots of stuff...

scope (failure) 
  rollback_to(current_state);
make_changes_with(stmt);

// More stuff...

return;
~~~

~~~csharp
sqlite3* db;
sqlite3_open("some.db", &db);
try
{
  sqlite3_stmt* stmt;
  sqlite3_prepare_v2(db, 
    "SELECT * FROM foo;", &stmt);
  try
  {
    // Lots of stuff...
    try
    {
        make_changes_with(stmt);

        // More stuff...
    }
    catch( Exception e )
    {
        rollback_to(current_state);
        throw;
    }
  }
  finally
  {
    sqlite3_finalize(stmt);
  }
}
finally
{
  sqlite3_close(db);
}
~~~
</div>

> The code has turned into spaghetti, spreading the error recovery all over the shop and forcing a level of indentation for every try block. The version using scope(X) is, in my opinion, significantly more readable and easier to understand.

The Enhanced C# version of this feature uses names that (in my opinion) are easier to remember, since they are based on existing C# keywords:

- `on_finally { action(); }`: Take an action in a `finally` block, at the end of the current block.
- `on_throw { action(); }`: Take an action when an exception is thrown, then rethrow.
- `on_throw_catch { action(); }`: Take an action when an exception is thrown. Catch the exception without rethrowing.
- `on_return { action(); }`: Take an action when the method returns normally.

The `on_finally` statement is perhaps the most useful out of the four. The evidence of this is in the Google Go and Apple Swift languages, _both_ of which have comparable features, called `defer` in both languages. It appears that `defer` in swift works exactly like `on_finally`. However, `defer` in Go works differently: `defer` puts a clean-up action on a _list_ and executes that list of actions at the end of the current function. In contrast, `on_finally` is just a shortcut for a `try-finally` block.

Read on for more details and examples.

on_finally
----------

    // Usage:
    on_finally { Action(); }

Wraps the rest of the code of the current block in a `try-finally` block, and puts an action in the `finally` block.

<div class='sbs' markdown='1'>
~~~csharp
var old = Environment.CurrentDirectory;
Environment.CurrentDirectory = newDir;
on_finally { Environment.CurrentDirectory = old; }

foreach (var file in Directory.GetFiles("."))
  DoSomethingWith(file);
~~~

~~~csharp
// Output of LeMP
var old = Environment.CurrentDirectory;
Environment.CurrentDirectory = newDir;
try {
  foreach (var file in Directory.GetFiles("."))
    DoSomethingWith(file);
} finally {
  Environment.CurrentDirectory = old;
}
~~~
</div>

on_throw
--------

    // Usage:
    on_throw { Action(); }
    on_throw (IOException exc) { Action(exc); }
    
Wraps the rest of the code of the current block in a `try-catch` block, puts an action in the `catch` block, after which the exception is rethrown.

<div class='sbs' markdown='1'>
~~~csharp
on_throw (IOException e) {
    Log("IOException: " + e.Message);
}
var str = File.ReadAllText(filename);

on_throw { Log("Parse failed"); }
return Parse(str);
~~~

~~~csharp
// Output of LeMP
try {
  var str = File.ReadAllText(filename);
  try {
    return Parse(str);
  } catch {
    Log("Parse failed");
    throw;
  }
} catch (IOException e) {
  Log("IOException: " + e.Message);
  throw;
}
~~~
</div>

on_throw_catch
--------------

    // Usage:
    on_throw_catch { Action(); }
    on_throw_catch (IOException exc) { Action(exc); }
    
Wraps the rest of the code of the current block in a `try-catch` block, and puts an action in the `catch` block. The exception is not rethrown.

<div class='sbs' markdown='1'>
~~~csharp
on_throw_catch (IOException exc) {
    MessageBox.Show(exc.Message);
}
var data1 = File.ReadAllText(fn1);
var data2 = File.ReadAllText(fn2);
~~~

~~~csharp
// Output of LeMP
try {
  var data1 = File.ReadAllText(fn1);
  var data2 = File.ReadAllText(fn2);
} catch (IOException exc) {
  MessageBox.Show(exc.Message);
}
~~~
</div>

on_return
---------

    // Usage:
    on_return { Action(); }
    on_return (result) { Action(result); }
    on_return (ResultType result) { Action(result); }

In the code that follows this macro, all return statements are replaced by a block that runs a copy of this code and then returns. Example:
    
<div class='sbs' markdown='1'>
~~~csharp
// Counts the number of "1" bits in an integer
public static int CountOnes(uint x)
{
    on_return(retval) { Trace.WriteLine(retval); }
    x -= ((x >> 1) & 0x55555555);
    x = (((x >> 2) & 0x33333333) + (x & 0x33333333));
    x = (((x >> 4) + x) & 0x0f0f0f0f);
    x += (x >> 8);
    x += (x >> 16);
    return (int)(x & 0x0000003f);
}
~~~

~~~csharp
// Output of LeMP
public static int CountOnes(uint x) {
  x -= ((x >> 1) & 1431655765);
  x = (((x >> 2) & 858993459) + (x & 858993459));
  x = (((x >> 4) + x) & 252645135);
  x += (x >> 8);
  x += (x >> 16);
  {
    var retval = (int) (x & 63);
    Trace.WriteLine(retval);
    return retval;
  }
}
~~~
</div>
    
**Caution:** In the current implementation, if there are multiple return statements, there will be multiple copies of the handler in the output. Keep that in mind if the handler is long.

`on_return` only affects the current block in which it is placed. For example, the following code has two return statements, but `on_return` only applies to the one in the "inner" block.

<div class='sbs' markdown='1'>
~~~csharp
foreach (var item in list) {
    on_return { Trace.WriteLine("FAIL"); }
    if (!IsValid(item))
        return false;
    ProcessValidItem(item);
}
return true;
~~~

~~~csharp
// Output of LeMP
foreach (var item in list) {
  if (!IsValid(item)) {
    var __result__ = false;
    Trace.WriteLine("FAIL");
    return __result__;
  }
  ProcessValidItem(item);
}
return true;
~~~
</div>

If you use `on_return` within a `void` function or property setter, it will work without any physical `return` statement being present:

<div class='sbs' markdown='1'>
~~~csharp
public static void Main()
{
    on_return { WriteLine("The End."); }
    WriteLine("The Beginning.");
}
~~~

~~~csharp
// Output of LeMP
public static void Main() {
  WriteLine("The Beginning.");
  WriteLine("The End.");
}
~~~
</div>

**Caution:** Because this is a lexical macro, it lets you do things that you shouldn't be allowed to do. For example, `{ on_return { x++; } int x=0; return; }` will compile although the `on_return` block shouldn't be allowed to access `x`. Please don't do that, because if this were a built-in language feature, it wouldn't be allowed.

scope(...)
----------

As an homage to D, you can use the original D syntax if you feel like it:

<div class='sbs' markdown='1'>
~~~csharp
int ScopeExample() {
    // Same as on_finally { Cleanup() }
    scope (exit) { Cleanup(); }
    // Same as on_throw { Fail() }
    scope (failure) { Fail(); }
    // Same as on_return { Ok() }
    scope (success) { Ok(); }
    
    return NormalCodePath();
}
~~~

~~~csharp
// Output of LeMP
int ScopeExample() {
  try {
    try {
      {
        var __result__ = NormalCodePath();
        Ok();
        return __result__;
      }
    } catch {
      Fail();
    }
  } finally {
    Cleanup();
  }
}
~~~
</div>

However, in Enhanced C# the braces are required, due to the fact that `scope` is not a keyword.