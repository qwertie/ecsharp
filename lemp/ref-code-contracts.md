---
title: "LeMP Macro Reference: Code Contracts"
tagline: Standard macros in the LeMP namespace
layout: article
date: 20 Mar 2016
toc: true
redirectDomain: ecsharp.net
---

Introduction
------------

Code contracts allow you to specify "preconditions" (conditions that are required when a method starts) and "postconditions" (conditions that a method promises will be true when a method exits.) For example, you might require that a certain parameter is not null, and promise that a return value is greater than zero. Here's an example:

~~~csharp
// Enhanced C#
[ensures(_ >= 0), requires(num >= 0)]
public static double Sqrt(double num) => Math.Sqrt(num);
~~~

The condition on `ensures` includes an underscore `_` that refers to the return value of the method. The above can also be written equivalently as 

~~~csharp
[ensures(_ >= 0)]
public static double Sqrt([requires(_ >= 0)] double num) => Math.Sqrt(num);
~~~

The condition on `requires` includes an underscore `_` that refers to the argument that the attribute is attached to. Both versions of this code produce the following code by default:

~~~csharp
public static double Sqrt(double num)
{
  Contract.Assert(num >= 0, "Precondition failed: num >= 0");
  {
    var return_value = Math.Sqrt(num);
    Contract.Assert(return_value >= 0, "Postcondition failed: return_value >= 0");
    return return_value;
  }
}
~~~

All contract attributes (except notnull) can specify multiple expressions separated by commas, to produce multiple checks, each with its own error message. Example:

<div class='sbs' markdown='1'>
~~~csharp
[requires(code >= 32, code < 128)]
static char GetAscii(int code)
{
  return (char) code;
}
~~~

~~~csharp
// Output of LeMP
static char GetAscii(int code)
{
  Contract.Assert(code >= 32, "Precondition failed: code >= 32");
  Contract.Assert(code < 128, "Precondition failed: code < 128");
  return (char) code;
}
~~~
</div>

**Note:** It is thought that eventually we would like to use `_` for "don't care"; for example `Foo(out _)` would mean "use a dummy variable to hold this output parameter" and `(_, x, y) = tuple` would ignore the first value from a tuple. Therefore, using `_` for "current parameter" or "current return value" would be inconsistent. I propose to use `#` instead. In fact, this is already supported; for example you can write `[requires(# != null)]` rather than `[requires(_ != null)]`, and similarly for method forwarding you can write `void Foo() ==> target.#` rather than `void Foo() ==> target._`. In fact, the only thing stopping me from using `#` throughout the documentation is that C# developers are not used to seeing `#` as an identifier, and acquiring new users is such a massive struggle.

**Implementation details**: Code contracts are provided by just three macros in a single module, because it is difficult to modularize them due to technical limitations of LeMP (specifically, the fact that LeMP is _not_ designed to treat attributes as macros, and because it has limited mechanisms for ordering and conflict resolution between different macros). The first macro, internally named `ContractsOnMethod`, deals with contract attributes on methods and constructors. The second, `ContractsOnLambda`, deals with anonymous functions, and the third, `ContractsOnProperty`, deals with properties. All three macros provide access to the same set of contract attributes. This section (unlike other sections in this document) describes the _attributes_ rather than the macros, since the latter is merely an implementation detail.

Modes
-----

The contract macros (but not the "assert" attributes, which are also documented below) operate in two modes, which we could call "standalone" and "MS Code Contracts Rewriter". "standalone" is the default; it is designed _not to require_ the MS Code Contracts extension or the assembly rewriter. It calls only one methods:

	Contract.Assert(bool, string);

You must manually import the MS Code Contracts namespace:

	using System.Diagnostics.Contracts;

If you enable "MS Code Contracts Rewriter" mode, the contract attributes will 

1. Omit the second string argument (in order to allow the MS rewriter to choose the error string)
2. Call other contract methods as appropriate, e.g. `[ensures]` calls `Contract.Ensures`

In standalone mode, code contract attributes rely on helper macros such as `on_return` to work. For more information, please see [on_return, on_throw, on_throw_catch, on_finally](ref-on_star.html).

Configuration
-------------

### \#set #haveContractRewriter ###

	#set #haveContractRewriter;         // enable MS Code Contract Rewriter mode
	#set #haveContractRewriter = true;  // enable MS Code Contract Rewriter mode
	#set #haveContractRewriter = false; // disable MS Code Contract Rewriter mode (default)

Uses the `#set` macro to set a flag to indicate that your build process includes the Microsoft Code Contracts binary rewriter. In that case,

- `[requires(condition)]` will be rewritten as 

		Contract.Requires(condition) // instead of 
		Contract.Assert(condition, s) // where s is a string that includes the condition.
		
- `[ensures(condition)]` will be rewritten as 

		Contract.Ensures(condition) // instead of 
		on_return(return_value) { Contract.Assert(condition, s); }
		
- `[ensuresOnThrow<E>(condition)]` will be rewritten as 

		Contract.EnsuresOnThrow<E>(condition) // instead of 
		on_throw(E __exception__) { 
			if (!condition) throw new InvalidOperationException(
				"Postcondition failed after throwing an exception: condition", __exception__); 
		}`

- Other attributes are not affected, except `notnull` which is really an alias for `requires` or `ensures`.

### Assert method configuration ###

When `#haveContractRewriter = false`, you can choose the method that is called by `[requires]`, `[ensures]` and `[ensuresFinally]`. Here's how you configure these methods, along with their default values:

	#set #assertMethodForRequires = Contract.Assert;       // default
	#set #assertMethodForEnsures = Contract.Assert;        // default
	#set #assertMethodForEnsuresFinally = Contract.Assert; // default

**Note**: that MS Code Contracts do not have an equivalent of [`ensuresFinally`], so the `#assertMethodForEnsuresFinally` option still takes effect even when `#haveContractRewriter = true`.
	
`[ensuresOnThrow]` throws an exception manually instead of calling `Contract.Assert` because it needs to set the `Exception.InnerException` property, and the standard contract methods do not allow this. There is an option to change the exception, though:
	
	#set #exceptionTypeForEnsuresOnThrow = InvalidOperationException; // default

All contract attributes
-----------------------

### notnull & [notnull] ###

The word attribute `notnull` indicates that the argument or return value to which it is attached must not be null. `notnull` is equivalent to [requires(_ != null)] if applied to an argument, and [ensures(_ != null)] if applied to the method as a whole. For example,

	static notnull string Double(notnull string s) => s + s;

produces the following code by default:

~~~csharp
static string Double(string s)
{
	Contract.Assert(s != null, "Precondition failed: s != null");
	on_return (return_value) {
		Contract.Assert(return_value != null, "Postcondition failed: return_value != null");
	}
	return s + s;
}
~~~
	
This is ultimately expanded to

~~~csharp
static string Double(string s)
{
	Contract.Assert(s != null, "Precondition failed: s != null");
	{
		var return_value = s + s;
		Contract.Assert(return_value != null, "Postcondition failed: return_value != null");
		return return_value;
	}
}
~~~

The normal attribute `[notnull]` is equivalent, e.g. 

	[notnull] static string Double([notnull] string s) => s + s;

### [requires] & [assert] ###

`[requires(expr)]` and [assert(expr)] specify an expression that must be true at the beginning of the method; `requires` produces a call to `Contract.Assert` or `Contract.Requires` depending on its configuration (described above). Example:

<div class='sbs' markdown='1'>
~~~csharp
[assert(!_reentrant)]
[requires(!string.IsNullOrEmpty(handlerKey))]
int ProcessEvent(string handlerKey)
{
	_reentrant = true;
	on_finally { _reentrant = false; }
	
	Action a = handlerDict[handlerKey];
	if (a != null)
		Log(a());
}
~~~

~~~csharp
// Output of LeMP
int ProcessEvent(string handlerKey)
{
  System.Diagnostics.Debug.Assert(!_reentrant, "Assertion failed in `ProcessEvent`: !_reentrant");
  Contract.Assert(!string.IsNullOrEmpty(handlerKey), "Precondition failed: !#string.IsNullOrEmpty(handlerKey)");
  _reentrant = true;
  try {
    Action a = handlerDict[handlerKey];
    if (a != null)
      Log(a());
  } finally {
    _reentrant = false;
  }
}
~~~
</div>

The condition can include an underscore `_` that refers to the argument that the attribute is attached to, if any.

The `[assert]` attribute is actually translated to a call to the `assert()` macro.

### [ensures] & [ensuresAssert] ###

`[ensures(expr)]` and `[ensuresAssert(expr)]` specify an expression that must be true if-and-when the method returns normally. Example:

<div class='sbs' markdown='1'>
~~~csharp
[ensuresAssert(_ >= 0)]
static double Root(double x)
{
	return Math.Sqrt(x);
}
[ensures(File.Exists(fn))]
public void Save(string fn, string text)
{
	File.WriteAllText(fn, text);
}
~~~

~~~csharp
// Output of LeMP
static double Root(double x)
{
  {
    var return_value = Math.Sqrt(x);
    System.Diagnostics.Debug.Assert(return_value >= 0, "Postcondition failed: return_value >= 0");
    return return_value;
  }
}
public void Save(string fn, string text)
{
  File.WriteAllText(fn, text);
  Contract.Assert(File.Exists(fn), "Postcondition failed: File.Exists(fn)");
}
~~~
</div>

### [ensuresOnThrow] ###

Specifies a condition that must be true if the method throws an exception. When `#haveContractRewriter` is false, underscore `_` refers to the thrown exception object; this is not available in the MS Code Contracts Rewriter.

<div class='sbs' markdown='1'>
~~~csharp
[ensuresOnThrow(Condition1)] 
[ensuresOnThrow<IOException>(Condition2)] 
void DoSomething()
{
  DoSomethingCore();
}
~~~

~~~csharp
// Output of LeMP
void DoSomething()
{
  try {
    try {
      DoSomethingCore();
    } catch (IOException __exception__) {
      if (!Condition2)
        throw new InvalidOperationException("Postcondition failed after throwing an exception: Condition2", __exception__);
      throw;
    }
  } catch (Exception __exception__) {
    if (!Condition1)
      throw new InvalidOperationException("Postcondition failed after throwing an exception: Condition1", __exception__);
    throw;
  }
}
~~~
</div>

**Note**: in `#haveContractRewriter` mode, `ensuresOnThrow` must have a type parameter, because Microsoft did not define an overload of `Contract.EnsuresOnThrow` that _doesn't_ have a type parameter.

### [ensuresFinally] ###

Introduces a test in a `finally` clause to ensure that a condition is met regardless of whether the method returns normally or throws an exception. There is no equivalent of this attribute in the standard Microsoft `Contract` class; this might be because, in case the contract fails, a finally clause does not have access to the exception that was thrown and therefore there is a problem: throwing a contract-violation exception causes the original exception to be lost. If this is a problem for you, you can solve it using `#set #assertMethodForEnsuresFinally` to choose a custom assert method, e.g. one that logs or prints an error instead of throwing an exception.

Example:

<div class='sbs' markdown='1'>
~~~csharp
[ensuresFinally(ObjectIsValid)] 
void Method(int option)
{
  ChangeStateBasedOn(option);
}
~~~

~~~csharp
// Output of LeMP
void Method(int option)
{
  try {
    ChangeStateBasedOn(option);
  } finally {
    Contract.Assert(ObjectIsValid, "Postcondition failed: ObjectIsValid");
  }
}
~~~
</div>