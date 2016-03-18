---
title: LeMP Macro Reference: Code Contracts
tagline: Standard macros in the LeMP namespace
layout: article
date: 17 Mar 2016
toc: true
---

Introduction
------------

Code contracts allow you to specify "preconditions" (conditions that are required when a method starts) and "postconditions" (conditions that a method promises will be true when a method exits.) For example, you might require that a certain parameter is not null, and promise that a return value is greater than zero. Here's an example:

	// Enhanced C#
	[ensures(_ >= 0), requires(num >= 0)]
	public static double Sqrt(double num) => Math.Sqrt(num);

The condition on `ensures` includes an underscore `_` that refers to the return value of the method. The above can also be written equivalently as 

	[ensures(_ >= 0)]
	public static double Sqrt([requires(_ >= 0)] double num) => Math.Sqrt(num);

The condition on `requires` includes an underscore `_` that refers to the argument that the attribute is attached to. Both versions of this code produce the following code by default:

	public static double Sqrt(double num)
	{
		Contract.Assert(num >= 0, "Precondition failed: num >= 0");
		{
			var return_value = Math.Sqrt(num);
			Contract.Assert(return_value >= 0, "Postcondition failed: return_value >= 0");
			return return_value;
		}
	}

**Implementation detail**: Code contracts are provided by just three macros in a single module, because it is difficult to modularize them due to technical limitations. The first macro, internally named `ContractsOnMethod`, deals with contract attributes on methods and constructors. The second, `ContractsOnLambda`, deals with anonymous functions, and the third, `ContractsOnProperty`, deals with properties. All three macros provide access to the same set of contract attributes. This section (unlike other sections in this document) describes the _attributes_ rather than the macros, since the latter is merely an implementation detail.

Modes
-----

The contract macros (but not the "assert" attributes, which are also documented below) operate in two modes, which we could call "standalone" and "MS Code Contracts Rewriter". "standalone" is the default; it is designed _not to require_ the MS Code Contracts extension or the assembly rewriter. It calls only one methods:

	Contract.Assert(bool, string);

You must manually import the MS Code Contracts namespace:

	using System.Diagnostics.Contracts;

You can write these methods yourself or use the methods in Loyc.Essentials.dll, in the `Loyc.MiniContract` namespace. On the other hand, if you enable "MS Code Contracts Rewriter" mode, the contract attributes will 

1. Omit the second string argument (in order to allow the MS rewriter to choose the error string)
2. Call other contract methods as appropriate, e.g. `[ensures]` calls `Contract.Ensures`

### #set \#haveContractRewriter ###

	#set #haveContractRewriter;         // enable MS Code Contract Rewriter mode
	#set #haveContractRewriter = true;  // enable MS Code Contract Rewriter mode
	#set #haveContractRewriter = false; // disable MS Code Contract Rewriter mode (default)

	Uses the #set macro to set a flag to indicate that your build process includes the Microsoft Code Contracts binary rewriter. In that case,

- [requires(condition)] will be rewritten as `Contract.Requires(condition)` instead of Contract.Requires(condition, s) where s is a string that includes the method name and condition.
- [ensures(condition)] will be rewritten as `Contract.Ensures(condition)` instead of `on_return(return_value) { Contract.Assert(condition, s); }`.
- [ensuresOnThrow(condition)] will be rewritten as Contract.EnsuresOnThrow(condition) instead of `on_throw(__exception__) { Contract.Assert(condition, s); }`.
- Other attributes are not affected, except `notnull` which is really an alias for `requires` or `ensures`.

### notnull, [notnull] ###

	The word attribute `notnull` can indicates that either a 

### TODO ###

	notnull T method(notnull T arg) {...}; T method([requires(expr)] T arg) {...}; [requires(expr)] T method(...) {...}; [ensures(expr)] T method(...) {...}; [ensuresOnThrow(expr)] T method(...) {...}; [ensuresOnThrow<Exception>(expr)] T method(...) {...}

Generates Contract checks in a method.

- [requires(expr)] and [assert(expr)] specify an expression that must be true at the beginning of the method; assert conditions are checked in debug builds only, while "requires" conditions are checked in all builds. The condition can include an underscore `_` that refers to the argument that the attribute is attached to, if any.
- [ensures(expr)] and [assertEnsures(expr)] specify an expression that must be true if-and-when the method returns normally. assert conditions are checked in debug builds only. The condition can include an underscore `_` that refers to the return value of the method.
- [ensuresOnThrow(expr)] and [ensuresOnThrow<ExceptionType>(expr)] specify a condition that must be true if the method throws an exception. When #haveContractRewriter is false, underscore `_` refers to the thrown exception object; this is not available in the MS Code Contracts Rewriter.
- notnull is equivalent to [requires(_ != null)] if applied to an argument, and [ensures(_ != null)] if applied to the method as a whole.

All contract attributes (except notnull) can specify multiple expressions separated by commas, to produce multiple checks, each with its own error message.









//#printKnownMacros
namespace LeMP
{
	/*
		### on_error_catch ###

			on_error_catch(exc) { _foo = 0; }

		Wraps the code that follows this macro in a try-catch statement, with the given braced block as the 'catch' block. The first argument to on_error_catch is optional and represents the desired name of the exception variable. In contrast to on_throw(), the exception is not rethrown at the end of the generated catch block.
	*/
	on_error_catch; // StandardMacros.on_error_catch (Mode = PriorityNormal)
	/*
		### #unroll ###

			unroll ((X, Y) \in ((X, Y), (Y, X))) {...}

		Produces variations of a block of code, by replacing an identifier left of `in` with each of the corresponding expressions on the right of `in`. The braces are omitted from the output. 
	*/
	#unroll; // StandardMacros.unroll (Mode = PriorityNormal)
	/*
		### #class ###

			e.g. alt class Tree<T> { alt Node(Tree<T> Left, Tree<T> Right); alt Leaf(T Value); }

		Expands a short description of an 'algebraic data type' into a set of classes with a common base class.
	*/
	#class; // StandardMacros.AlgebraicDataType (Mode = Passive, PriorityNormal)
	/*
		### scope ###

			scope(exit) { ... }; scope(success) {..}; scope(failure) {...}

		Translates the three 'scope' statements from the D programming language to the LeMP equivalents on_finally, on_return and on_catch.
	*/
	scope; // StandardMacros.scope (Mode = PriorityNormal)
	/*
		### #useSymbols ###

			use_symbols; ... @@Foo ...

		Replaces each @@symbol in the code that follows with a static readonly variable named sy_X for each symbol @@X.
	*/
	#useSymbols; // StandardMacros.use_symbols (Mode = PriorityNormal)
	/*
		### #fn ###

			Type SomeMethod(Type param) ==> target._;

		Forward a call to another method. The target method must not include an argument list; the method parameters are forwarded automatically. If the target expression includes an underscore (`_`), it is replaced with the name of the current function. For example, `int Compute(int x) ==> base._` is implemented as `int Compute(int x) { return base.Compute(x); }`
	*/
	#fn; // StandardMacros.ForwardMethod (Mode = Passive, PriorityNormal)
	/*
		### #fn ###

			Type Name(set Type name) {...}; Type Name(public Type name) {...}

		Automatically assigns a value to an existing field, or creates a new field with an initial value set by calling the method. This macro can be used with constructors and methods. This macro is activated by attaching one of the following modifiers to a method parameter: `set, public, internal, protected, private, protectedIn, static, partial`.
	*/
	#fn; // StandardMacros.SetOrCreateMember (Mode = Passive, PriorityNormal)
	/*
		### on_return ###

			on_return (result) { Console.WriteLine(result); }

		In the code that follows this macro, all return statements are replaced by a block that runs a copy of this code and then returns. For example, the code `{ on_return(r) { r++; } Foo(); return Math.Abs(x); }` is replaced by `{ Foo(); { var r = Math.Abs(x); r++; return r; } }`. Because this is a lexical macro, it lets you do things that you shouldn't be allowed to do. For example, `{ on_return { x++; } int x=0; return; }` will compile although the `on_return` block shouldn't be allowed to access `x`. Please don't do that, because if this were a built-in language feature, it wouldn't be allowed.
	*/
	on_return; // StandardMacros.on_return (Mode = PriorityNormal)
	/*
		### use_symbols ###

			use_symbols; ... @@Foo ...

		Replaces each @@symbol in the code that follows with a static readonly variable named sy_X for each symbol @@X.
	*/
	use_symbols; // StandardMacros.use_symbols (Mode = PriorityNormal)
	/*
		### #cons ###

			notnull T method(notnull T arg) {...}; T method([requires(expr)] T arg) {...}; [requires(expr)] T method(...) {...}; [ensures(expr)] T method(...) {...}; [ensuresOnThrow(expr)] T method(...) {...}; [ensuresOnThrow<Exception>(expr)] T method(...) {...}

		Generates Contract checks in a method.
		
		- [requires(expr)] and [assert(expr)] specify an expression that must be true at the beginning of the method; assert conditions are checked in debug builds only, while "requires" conditions are checked in all builds. The condition can include an underscore `_` that refers to the argument that the attribute is attached to, if any.
		- [ensures(expr)] and [assertEnsures(expr)] specify an expression that must be true if-and-when the method returns normally. assert conditions are checked in debug builds only. The condition can include an underscore `_` that refers to the return value of the method.
		- [ensuresOnThrow(expr)] and [ensuresOnThrow<ExceptionType>(expr)] specify a condition that must be true if the method throws an exception. When #haveContractRewriter is false, underscore `_` refers to the thrown exception object; this is not available in the MS Code Contracts Rewriter.
		- notnull is equivalent to [requires(_ != null)] if applied to an argument, and [ensures(_ != null)] if applied to the method as a whole.
		
		All contract attributes (except notnull) can specify multiple expressions separated by commas, to produce multiple checks, each with its own error message.
	*/
	#cons; // StandardMacros.ContractsOnMethod (Mode = Passive, PriorityInternalFallback)
	/*
		### #cons ###

			Type Name(set Type name) {...}; Type Name(public Type name) {...}

		Automatically assigns a value to an existing field, or creates a new field with an initial value set by calling the method. This macro can be used with constructors and methods. This macro is activated by attaching one of the following modifiers to a method parameter: `set, public, internal, protected, private, protectedIn, static, partial`.
	*/
	#cons; // StandardMacros.SetOrCreateMember (Mode = Passive, PriorityNormal)
	/*
		### #cons ###

			class Foo { this() {} }

		Supports the EC# 'this()' syntax for constructors by replacing 'this' with the name of the enclosing class.
	*/
	#cons; // StandardMacros.Constructor (Mode = Passive, PriorityOverride)
	/*
		### replace ###

			replace (input($capture) => output($capture), ...) {...}

		Finds one or more patterns in a block of code and replaces each matching expression with another expression. The braces are omitted from the output (and are not matchable).This macro can be used without braces, in which case it affects all the statements/arguments that follow it in the current statement or argument list.
	*/
	replace; // StandardMacros.replace (Mode = PriorityNormal)
	/*
		### with ###

			with (Some.Thing) { .Member = 0; .Method(); }

		Use members of a particular object with a shorthand "prefix-dot" notation. **Warning**: if used with a value type, a copy of the value is made.
	*/
	with; // StandardMacros.with (Mode = ProcessChildrenBefore, PriorityNormal)
	/*
		### #setAssertMethod ###

			#setAssertMethod(Class.MethodName)

		Sets the method to be called by the assert() macro (default: System.Diagnostics.Debug.Assert). This method should accept two arguments: (bool condition, string failureMessage)
	*/
	#setAssertMethod; // StandardMacros.setAssertMethod (Mode = PriorityNormal)
	/*
		### #replace ###

			replace (input($capture) => output($capture), ...) {...}

		Finds one or more patterns in a block of code and replaces each matching expression with another expression. The braces are omitted from the output (and are not matchable).This macro can be used without braces, in which case it affects all the statements/arguments that follow it in the current statement or argument list.
	*/
	#replace; // StandardMacros.replace (Mode = PriorityNormal)
	/*
		### #import ###

			using (System, System.(Collections.Generic, Linq, Text));

		Generates multiple using-statements from a single one.
	*/
	#import; // StandardMacros.UsingMulti (Mode = Passive, PriorityNormal)
	/*
		### assert ###

			assert(condition);

		Translates assert(expr) to System.Diagnostics.Debug.Assert(expr, "Assertion failed in Class.MethodName: expr").
	*/
	assert; // StandardMacros.assert (Mode = PriorityNormal)
	/*
		### ## ###

			a `##` b; concatId(a, b)

		Concatenates identifiers and/or literals to produce an identifier. For example, the output of ``a `##` b`` is `ab`..
	*/
	##; // StandardMacros.concatId (Mode = PriorityNormal)
	/*
		### quote ###

			e.g. quote({ foo(); }) ==> F.Id(id);

		Macro-based code quote mechanism, to be used as long as a more complete compiler is availabe. If there is a single parameter that is braces, the braces are stripped out. If there are multiple parameters, or multiple statements in braces, the result is a call to #splice(). The output refers unqualified to 'CodeSymbols' and 'LNodeFactory' so you must have 'using Loyc.Syntax' at the top of your file. The substitution operator $(expr) causes the specified expression to be inserted unchanged into the output.
	*/
	quote; // StandardMacros.quote (Mode = PriorityNormal)
	/*
		### concatId ###

			a `##` b; concatId(a, b)

		Concatenates identifiers and/or literals to produce an identifier. For example, the output of ``a `##` b`` is `ab`..
	*/
	concatId; // StandardMacros.concatId (Mode = PriorityNormal)
	/*
		### #namespace ###

			namespace Foo;

		Surrounds the remaining code in a namespace block.
	*/
	#namespace; // StandardMacros.Namespace (Mode = Passive, PriorityNormal)
	/*
		### #quote ###

			e.g. quote({ foo(); }) ==> F.Id(id);

		Macro-based code quote mechanism, to be used as long as a more complete compiler is availabe. If there is a single parameter that is braces, the braces are stripped out. If there are multiple parameters, or multiple statements in braces, the result is a call to #splice(). The output refers unqualified to 'CodeSymbols' and 'LNodeFactory' so you must have 'using Loyc.Syntax' at the top of your file. The substitution operator $(expr) causes the specified expression to be inserted unchanged into the output.
	*/
	#quote; // StandardMacros.quote (Mode = PriorityNormal)
	/*
		### nameof ###

			nameof(id_or_expr)

		Converts the 'key' name component of an expression to a string (e.g. nameof(A.B<C>(D)) == "B")
	*/
	nameof; // StandardMacros.nameof (Mode = PriorityNormal)
	/*
		### rawQuote ###

			e.g. quoteRaw($foo) ==> F.Call(CodeSymbols.Substitute, F.Id("foo"));

		Behaves the same as quote(code) except that the substitution operator $ is not recognized as a request for substitution.
	*/
	rawQuote; // StandardMacros.rawQuote (Mode = PriorityNormal)
	/*
		### stringify ###

			stringify(expr)

		Converts an expression to a string (note: original formatting is not preserved)
	*/
	stringify; // StandardMacros.stringify (Mode = PriorityNormal)
	/*
		### @`?.` ###

			a.b?.c.d

		a.b?.c.d means (a.b != null ? a.b.c.d : null)
	*/
	@`?.`; // StandardMacros.NullDot (Mode = PriorityNormal)
	/*
		### #rawQuote ###

			e.g. quoteRaw($foo) ==> F.Call(CodeSymbols.Substitute, F.Id("foo"));

		Behaves the same as quote(code) except that the substitution operator $ is not recognized as a request for substitution.
	*/
	#rawQuote; // StandardMacros.rawQuote (Mode = PriorityNormal)
	/*
		### @`tree==` ###

			x `tree==` y

		Returns the literal true if two or more syntax trees are equal, or false if not.
	*/
	@`tree==`; // StandardMacros.TreeEqual (Mode = PriorityNormal)
	/*
		### @#if ###

			static if() {...} else {...}

		TODO. Only boolean true/false implemented now
	*/
	@#if; // StandardMacros.StaticIf (Mode = Passive, PriorityNormal)
	/*
		### static_if ###

			static_if(cond, then, otherwise)

		TODO. Only boolean true/false implemented now
	*/
	static_if; // StandardMacros.static_if (Mode = Passive, PriorityNormal)
	/*
		### #property ###

			[field x] int X { get; set; }

		Create a backing field for a property. In addition, if the body of the property is empty, a getter is added.
	*/
	#property; // StandardMacros.BackingField (Mode = Passive, PriorityNormal)
	/*
		### #property ###

			notnull T Prop {...}; T this[[requires(expr)] T arg] {...}; T Prop { [requires(expr)] set; }; [ensures(expr)] T Prop {...}; [ensuresOnThrow(expr)] T Prop {...}; [ensuresOnThrow<Exception>(expr)] T Prop {...}

		Generates contract checks in a property. You can apply contract attributes to the property itself, to the getter, to the setter, or all three. When the [requires] or [assert] attributes are applied to the property itself, they are treated as if they were applied to the getter; but when the [ensures], [assertEnsures], notnull, and [ensuresOnThrow] attributes are applied to the property itself, they are treated as if they were applied to both the getter and the setter separately.
	*/
	#property; // StandardMacros.ContractsOnProperty (Mode = Passive, PriorityInternalFallback)
	/*
		### #property ###

			Type Prop ==> target; Type Prop { get ==> target; set ==> target; }

		Forward property getter and/or setter. If the first syntax is used (with no braces), only the getter is forwarded.
	*/
	#property; // StandardMacros.ForwardProperty (Mode = Passive, PriorityNormal)
	/*
		### #in ###

			x in lo..hi; x in lo...hi; x in ..hi; x in lo..._; x in range

		Converts an 'in' expression to a normal C# expression using the following rules (keeping in mind that the EC# parser treats `..<` as an alias for `..`):
		1. `x in _..hi` and `x in ..hi` become `x.IsInRangeExcl(hi)`
		2. `x in _...hi` and `x in ...hi` become `x.IsInRangeIncl(hi)`
		3. `x in lo.._` and `x in lo..._` become simply `x >= lo`
		4. `x in lo..hi` becomes `x.IsInRangeExcludeHi(lo, hi)`
		5. `x in lo...hi` becomes `x.IsInRange(lo, hi)`
		6. `x in range` becomes `range.Contains(x)`
		The first applicable rule is used.
	*/
	#in; // StandardMacros.In (Mode = PriorityNormal)
	/*
		### #haveContractRewriter ###

			#haveContractRewriter(true)

		Sets a flag to indicate that your build process includes the Microsoft Code Contracts binary rewriter, so
		
		- [requires(condition)] will be rewritten as `Contract.Requires(condition)` instead of Contract.Requires(condition, s) where s is a string that includes the method name and condition.- [ensures(condition)] will be rewritten as `Contract.Ensures(condition)` instead of `on_return(return_value) { Contract.Assert(condition, s); }`.- [ensuresOnThrow(condition)] will be rewritten as Contract.EnsuresOnThrow(condition) instead of `on_throw(__exception__) { Contract.Assert(condition, s); }`.
	*/
	#haveContractRewriter; // StandardMacros.haveContractRewriter (Mode = PriorityNormal)
	/*
		### matchCode ###

			matchCode (var) { case ...: ... }; // In LES, use a => b instead of case a: b

		Attempts to match and deconstruct a Loyc tree against a series of cases with patterns, e.g. `case $a + $b:` expects a tree that calls `+` with two parameters, placed in new variables called a and b. `break` is not required or recognized at the end of each case's handler (code block). Use `$(...x)` to gather zero or more parameters into a list `x`. Use `case pattern1, pattern2:` in EC# to handle multiple cases with the same handler.
	*/
	matchCode; // StandardMacros.matchCode (Mode = PriorityNormal)
	/*
		### @`=>` ###

			([notnull] (x => ...)); ([notnull] x) => ...; ([requires(expr)] x) => ...; ([ensures(expr)] (x => ...)); ([ensuresOnThrow(expr)] (x => ...)); 

		Generates Contract checks in a lambda function. See the documentation of ContractsOnMethod for more information about the contract attributes.
	*/
	@`=>`; // StandardMacros.ContractsOnLambda (Mode = Passive, PriorityInternalFallback)
	/*
		### @`:::` ###

			A=:B; A:::B

		Declare a variable B and set it to the value A. Typically used within a larger expression, e.g. if (int.Parse(text):::num > 0) positives += num;
	*/
	@`:::`; // StandardMacros.QuickBind (Mode = PriorityNormal)
	/*
		### match ###

			match (var) { case ...: ... }; // In LES, use a => b instead of case a: b

		Attempts to match and deconstruct an object against a "pattern", such as a tuple or an algebraic data type. Example:
		match (obj) {  
		   case is Shape(ShapeType.Circle, $size, Location: $p is Point<int>($x, $y)): 
		      Circle(size, x, y); 
		}
		
		This is translated to the following C# code: 
		do { 
		   Point<int> p; 
		   Shape tmp1; 
		   if (obj is Shape) { 
		      var tmp1 = (Shape)obj; 
		      if (tmp1.Item1 == ShapeType.Circle) { 
		         var size = tmp1.Item2; 
		         var tmp2 = tmp1.Location; 
		         if (tmp2 is Point<int>) { 
		            var p = (Point<int>)tmp2; 
		            var x = p.Item1; 
		            var y = p.Item2; 
		            Circle(size, x, y); 
		            break; 
		         } 
		      }
		   }
		} while(false); 
		`break` is not expected at the end of each handler (`case` code block), but it can be used to exit early from a `case`. You can associate multiple patterns with the same handler using `case pattern1, pattern2:` in EC#, but please note that (due to a limitation of plain C#) this causes code duplication since the handler will be repeated for each pattern.
	*/
	match; // StandardMacros.match (Mode = PriorityNormal)
	/*
		### ColonEquals ###

			A := B

		Declare a variable A and set it to the value of B. Equivalent to "var A = B".
	*/
	ColonEquals; // StandardMacros.ColonEquals (Mode = PriorityNormal)
	/*
		### @`..` ###

			lo..hi; ..hi; lo.._

		Given `lo..hi, produces `Range.Excl(lo, hi)
	*/
	@`..`; // StandardMacros.RangeExcl (Mode = PriorityNormal)
	/*
		### @`??.` ###

			a.b?.c.d

		a.b?.c.d means (a.b != null ? a.b.c.d : null)
	*/
	@`??.`; // StandardMacros.NullDot (Mode = PriorityNormal)
	/*
		### @`??=` ###

			A ??= B

		Assign A = B only when A is null. Caution: currently, A is evaluated twice.
	*/
	@`??=`; // StandardMacros.NullCoalesceSet (Mode = PriorityNormal)
	/*
		### #useDefaultTupleTypes ###

			#useDefaultTupleTypes;

		Reverts to using Tuple and Tuple.Create for all arities of tuple.
	*/
	#useDefaultTupleTypes; // StandardMacros.useDefaultTupleTypes (Mode = PriorityNormal)
	/*
		### @`...` ###

			lo..hi; ..hi; lo.._

		Given `lo..hi, produces `Range.Excl(lo, hi)
	*/
	@`...`; // StandardMacros.RangeIncl (Mode = PriorityNormal)
	/*
		### #of ###

			#<x, y, ...>

		Represents a tuple type
	*/
	#of; // StandardMacros.TupleType (Mode = Passive, PriorityNormal)
	/*
		### @`=` ###

			(a, b, etc) = expr;

		Assign a = expr.Item1, b = expr.Item2, etc.
	*/
	@`=`; // StandardMacros.UnpackTuple (Mode = Passive, PriorityNormal)
	/*
		### on_finally ###

			on_finally { _foo = 0; }

		Wraps the code that follows this macro in a try-finally statement, with the specified block as the 'finally' block.
	*/
	on_finally; // StandardMacros.on_finally (Mode = PriorityNormal)
	/*
		### #setTupleType ###

			#setTupleType(BareName); #setTupleType(TupleSize, BareName); #setTupleType(TupleSize, BareName, Factory.Method)

		Set type and creation method for tuples, for a specific size of tuple or for all sizes at once
	*/
	#setTupleType; // StandardMacros.setTupleType (Mode = PriorityNormal)
	/*
		### #tuple ###

			(x,); (x, y, ...)

		Create a tuple
	*/
	#tuple; // StandardMacros.Tuple (Mode = Passive, PriorityNormal)
	/*
		### on_throw ###

			on_throw(exc) { OnThrowAction(exc); }

		Specifies an action to take in case the current block of code ends with an exception being thrown. It wraps the code that follows this macro in a try-catch statement, with the braced block you provide as the 'catch' block, followed by a 'throw;' statement to rethrow the exception. The first argument to on_throw is optional and represents the desired name of the exception variable.
	*/
	on_throw; // StandardMacros.on_throw (Mode = PriorityNormal)
	/*
		### unroll ###

			unroll ((X, Y) \in ((X, Y), (Y, X))) {...}

		Produces variations of a block of code, by replacing an identifier left of `in` with each of the corresponding expressions on the right of `in`. The braces are omitted from the output. 
	*/
	unroll; // StandardMacros.unroll (Mode = PriorityNormal)
	/*
		### @`=:` ###

			A=:B; A:::B

		Declare a variable B and set it to the value A. Typically used within a larger expression, e.g. if (int.Parse(text):::num > 0) positives += num;
	*/
	@`=:`; // StandardMacros.QuickBind (Mode = PriorityNormal)
}
//#printKnownMacros
namespace LeMP.Prelude
{
	/*
		### noMacro ###

			noMacro(Code)

		Pass code through to the output language, without macro processing.
	*/
	noMacro; // BuiltinMacros.noMacro (Mode = NoReprocessing, PriorityNormal)
	/*
		### import_macros ###

			import_macros Namespace

		Use macros from specified namespace. The 'macros' modifier imports macros only, deleting this statement from the output.
	*/
	import_macros; // BuiltinMacros.import_macros (Mode = PriorityNormal)
	/*
		### #printKnownMacros ###

			#printKnownMacros;

		Prints a table of all macros known to LeMP, as (invalid) C# code.
	*/
	#printKnownMacros; // BuiltinMacros.printKnownMacros (Mode = NoReprocessing, PriorityNormal)
}
//#printKnownMacros
namespace LeMP.Prelude.Les
{
	/*
		### @foreach ###

			foreach Item \in Collection {Body...}; foreach Item::Type \in Collection {Body...}

		Represents the C# 'foreach' statement.
	*/
	@foreach; // Macros.foreach (Mode = PriorityNormal)
	/*
		### @is ###

			Expr `is` Type

		Determines whether a value is an instance of a specified type (@false or @true).
	*/
	@is; // Macros.is (Mode = PriorityNormal)
	/*
		### prot ###

			[prot]

		Used as an attribute to indicate that a method, field or inner type has protected accessibility, meaning it only accessible in the current scope and in the scope of derived classes.
	*/
	prot; // Macros.prot (Mode = PriorityNormal)
	/*
		### @`:=` ###

			Name::Type = Value; Name::Type := Value

		Defines a variable or field in the current scope.
	*/
	@`:=`; // Macros.ColonColonInit (Mode = Passive, PriorityNormal)
	/*
		### @`:=` ###

			Name := Value

		Defines a variable or field in the current scope.
	*/
	@`:=`; // Macros.ColonEquals (Mode = PriorityNormal)
	/*
		### @new ###

			(new Type); (new Type(Args...))

		Initializes a new instance of the specified type.
	*/
	@new; // Macros.new (Mode = PriorityNormal)
	/*
		### @out ###

			[out]

		Used as an attribute on a method parameter to indicate that it is passed by reference. In addition, the called method must assign a value to the variable, and it cannot receive input through the variable.
	*/
	@out; // Macros.out (Mode = PriorityNormal)
	/*
		### @protected ###

			protected <declaration>

		Indicates that a method, field or inner type has protected accessibility, meaning it only accessible in the current scope and in the scope of derived classes.
	*/
	@protected; // Macros.protected (Mode = PriorityNormal)
	/*
		### @return ###

			return; return Expr

		Returns to the caller of the current method or lambda function.
	*/
	@return; // Macros.return (Mode = PriorityNormal)
	/*
		### virt ###

			[virt]

		Indicates that a method is 'virtual', which means that calls to it can potentially go to a derived class that 'overrides' the method.
	*/
	virt; // Macros.virt (Mode = PriorityNormal)
	/*
		### @struct ###

			struct Name { Members; }; struct Name(Bases...) { Members... }

		Defines a struct (a by-value data type with data and/or methods).
	*/
	@struct; // Macros.struct (Mode = PriorityNormal)
	/*
		### @true ###

			true
	*/
	@true; // Macros.true (Mode = PriorityNormal)
	/*
		### @ulong ###

			ulong

		An unsigned 64-bit data type
	*/
	@ulong; // Macros.ulong (Mode = PriorityNormal)
	/*
		### @using ###

			using NewName = OldName

		Defines an alias that applies inside the current module only.
	*/
	@using; // Macros.using1 (Mode = PriorityNormal)
	/*
		### @using ###

			using Disposable {Body...}; using VarName := Disposable {Body...}

		The Dispose() method of the 'Disposable' expression is called when the Body finishes.
	*/
	@using; // Macros.using2 (Mode = PriorityNormal)
	/*
		### @while ###

			while Condition {Body...}

		Runs the Body code repeatedly, as long as 'Condition' is true. The Condition is checked before Body is run the first time.
	*/
	@while; // Macros.while (Mode = PriorityNormal)
	/*
		### partial ###

			partial <declaration>

		Indicates that the declared thing may be formed by combining multiple separate parts. When you see this, look for other things with the same name.
	*/
	partial; // Macros.partial (Mode = PriorityNormal)
	/*
		### @as ###

			Expr `as` Type

		Attempts to cast a reference down to a derived class. The result is null if the cast fails.
	*/
	@as; // Macros.as (Mode = PriorityNormal)
	/*
		### @byte ###

			byte

		An unsigned 8-bit data type
	*/
	@byte; // Macros.byte (Mode = PriorityNormal)
	/*
		### @decimal ###

			decimal

		A 128-bit floating-point BCD data type
	*/
	@decimal; // Macros.decimal (Mode = PriorityNormal)
	/*
		### @double ###

			double

		A 64-bit floating-point data type
	*/
	@double; // Macros.double (Mode = PriorityNormal)
	/*
		### @`:` ###

			arg: value

		Represents a named argument.
	*/
	@`:`; // Macros.NamedArg (Mode = PriorityNormal)
	/*
		### @goto ###

			goto LabelName

		Run code starting at the specified label in the same method.
	*/
	@goto; // Macros.goto (Mode = PriorityNormal)
	/*
		### @goto ###

			goto case ConstExpr

		Jump to the specified case in the body of the same switch statement.
	*/
	@goto; // Macros.GotoCase (Mode = PriorityNormal)
	/*
		### @int ###

			int

		A signed 32-bit data type
	*/
	@int; // Macros.int (Mode = PriorityNormal)
	/*
		### @lock ###

			lock Object {Body...}

		Acquires a multithreading lock associated with the specified object. 'lock' waits for any other thread holding the lock to release it before running the statements in 'Body'.
	*/
	@lock; // Macros.lock (Mode = PriorityNormal)
	/*
		### @null ###

			null

		(Nothing in Visual Basic)
	*/
	@null; // Macros.null (Mode = PriorityNormal)
	/*
		### @override ###

			override <declaration>

		Indicates that a method overrides a virtual method in the base class.
	*/
	@override; // Macros.override (Mode = PriorityNormal)
	/*
		### @public ###

			public <declaration>

		Indicates that a type, method or field is publicly accessible.
	*/
	@public; // Macros.public (Mode = PriorityNormal)
	/*
		### @sbyte ###

			sbyte

		A signed 8-bit data type
	*/
	@sbyte; // Macros.sbyte (Mode = PriorityNormal)
	/*
		### import ###

			import Namespace;

		Use symbols from specified namespace ('using' in C#).
	*/
	import; // Macros.import (Mode = PriorityNormal)
	/*
		### @switch ###

			switch Value { case ConstExpr1; Handler1; break; case ConstExpr2; Handler2; break; default; DefaultHandler; }

		Chooses one of several code paths based on the specified 'Value'.
	*/
	@switch; // Macros.switch (Mode = PriorityNormal)
	/*
		### @try ###

			try {Code...} catch (E::Exception) {Handler...} finally {Cleanup...}

		Runs 'Code'. The try block must be followed by at least one catch or finally clause. A catch clause catches any exceptions that are thrown while the Code is running, and executes 'Handler'. A finally clause runs 'Cleanup' code before propagating the exception to higher-level code.
	*/
	@try; // Macros.try (Mode = PriorityNormal)
	/*
		### @virtual ###

			virtual <declaration>

		Indicates that a method is 'virtual', which means that calls to it can potentially go to a derived class that 'overrides' the method.
	*/
	@virtual; // Macros.virtual (Mode = PriorityNormal)
	/*
		### trait ###

			trait Name { Members; }; trait Name(Bases...) { Members... }

		Not implemented. A set of methods that can be inserted easily into a host class or struct; just add the trait to the host's list of Bases.
	*/
	trait; // Macros.trait (Mode = PriorityNormal)
	/*
		### @base ###

			base(Params...)

		Calls a constructor in the base class. Can only be used inside a constructor.
	*/
	@base; // Macros.base (Mode = PriorityNormal)
	/*
		### @case ###

			case ConstExpr; case ConstExpr { Code... }

		One label in a switch statement.
	*/
	@case; // Macros.case (Mode = PriorityNormal)
	/*
		### @class ###

			class Name { Members; }; class Name(Bases...) { Members... }

		Defines a class (a by-reference data type with data and/or methods).
	*/
	@class; // Macros.class (Mode = PriorityNormal)
	/*
		### @default ###

			default; default { Code... }

		The default label in a switch statement.
	*/
	@default; // Macros.default1 (Mode = PriorityNormal)
	/*
		### @default ###

			default(Type)

		The default value for the specified type (@null or an empty structure).
	*/
	@default; // Macros.default2 (Mode = PriorityNormal)
	/*
		### @long ###

			long

		A signed 64-bit data type
	*/
	@long; // Macros.long (Mode = PriorityNormal)
	/*
		### alias ###

			alias NewName = OldName; alias NewName(Bases...) = OldName; alias NewName(Bases) = OldName { FakeMembers... }

		Not implemented. Defines an alternate view on a data type. If 'Bases' specifies one or more interfaces, a variable of type NewName can be implicitly converted to those interfaces.
	*/
	alias; // Macros.alias (Mode = PriorityNormal)
	/*
		### @extern ###

			extern <declaration>

		Indicates that the definition is supplies elsewhere.
	*/
	@extern; // Macros.extern (Mode = PriorityNormal)
	/*
		### @float ###

			float

		A 32-bit floating-point data type
	*/
	@float; // Macros.float (Mode = PriorityNormal)
	/*
		### @if ###

			if Condition {Then...}; if Condition {Then...} else {Else...}

		If 'Condition' is true, runs the 'Then' code; otherwise, runs the 'Else' code, if any.
	*/
	@if; // Macros.if (Mode = PriorityNormal)
	/*
		### @static ###

			static <declaration>

		Applies the #static attribute to a declaration.
	*/
	@static; // Macros.static (Mode = PriorityNormal)
	/*
		### fn ###

			fn Name(Args...) { Body... }; fn Name(Args...)::ReturnType { Body }; fn Name ==> ForwardingTarget { Body }

		Defines a function (also known as a method). The '==> ForwardingTarget' version is not implemented.
	*/
	fn; // Macros.fn (Mode = PriorityNormal)
	/*
		### @object ###

			object

		Common base class of all .NET data types
	*/
	@object; // Macros.object (Mode = PriorityNormal)
	/*
		### @readonly ###

			readonly Name::Type; readonly Name::Type = Value; readonly Name = Value

		Indicates that a variable cannot be changed after it is initialized.
	*/
	@readonly; // Macros.readonly (Mode = PriorityNormal)
	/*
		### @enum ###

			enum Name { Tag1 = Num1; Tag2 = Num2; ... }; enum Name(BaseInteger) { Tag1 = Num1; Tag2 = Num2; ... }

		Defines an enumeration (a integer that represents one of several identifiers, or a combination of bit flags when marked with [Flags]).
	*/
	@enum; // Macros.enum (Mode = PriorityNormal)
	/*
		### cons ###

			cons ClassName(Args...) {Body...}

		Defines a constructor for the enclosing type. To call the base class constructor, call base(...) as the first statement of the Body.
	*/
	cons; // Macros.cons (Mode = PriorityNormal)
	/*
		### @`->` ###

			cast(Expr, Type); Expr \cast Type

		Converts an expression to a new data type.
	*/
	@`->`; // Macros.cast (Mode = PriorityNormal)
	/*
		### @this ###

			this(Params...)

		Calls a constructor in the same class. Can only be used inside a constructor.
	*/
	@this; // Macros.this (Mode = PriorityNormal)
	/*
		### @unsafe ###

			unsafe <declaration>

		Indicates that the definition may use 'unsafe' parts of C#, such as pointers
	*/
	@unsafe; // Macros.unsafe (Mode = PriorityNormal)
	/*
		### @namespace ###

			namespace Name { Members... }

		Adds the specified members to a namespace. Namespaces are used to organize code; it is recommended that every data type and method be placed in a namespace. The 'Name' can have multiple levels (A.B.C).
	*/
	@namespace; // Macros.namespace (Mode = PriorityNormal)
	/*
		### prop ###

			prop Name::Type { get {Body...} set {Body...} }

		Defines a property. The getter and setter are optional, but there must be at least one of them.
	*/
	prop; // Macros.prop (Mode = PriorityNormal)
	/*
		### @`::` ###

			Name::Type

		Defines a variable or field in the current scope.
	*/
	@`::`; // Macros.ColonColon (Mode = Passive, PriorityNormal)
	/*
		### @bool ###

			bool

		The boolean data type (holds one of two values, @true or @false)
	*/
	@bool; // Macros.bool (Mode = PriorityNormal)
	/*
		### @const ###

			const Name::Type; const Name::Type = Value; const Name = Value

		Indicates a compile-time constant.
	*/
	@const; // Macros.const (Mode = PriorityNormal)
	/*
		### @string ###

			string

		The string data type: a read-only sequence of characters.
	*/
	@string; // Macros.string (Mode = PriorityNormal)
	/*
		### unless ###

			unless Condition {Then...}; unless Condition {Then...} else {Else...}

		If 'Condition' is false, runs the 'Then' code; otherwise, runs the 'Else' code, if any.
	*/
	unless; // Macros.unless (Mode = PriorityNormal)
	/*
		### @false ###

			false
	*/
	@false; // Macros.false (Mode = PriorityNormal)
	/*
		### @for ###

			for Init Test Increment {Body...}; for (Init, Test, Increment) {Body...};

		Represents the standard C/C++/C#/Java 'for' statement, e.g. 'for i=0 i<10 i++ { Console.WriteLine(i); };'
	*/
	@for; // Macros.for (Mode = PriorityNormal)
	/*
		### @internal ###

			internal <declaration>

		Indicates that a type, method or field is accessible only inside the same assembly. When combined with prot, it is also accessible to derived classes in different assemblies.
	*/
	@internal; // Macros.internal (Mode = PriorityNormal)
	/*
		### label ###

			label LabelName

		Define a label here that 'goto' can jump to.
	*/
	label; // Macros.label (Mode = PriorityNormal)
	/*
		### @`?` ###

			condition ? (t : f)

		Attempts to cast a reference down to a derived class. The result is null if the cast fails.
	*/
	@`?`; // Macros.QuestionMark (Mode = Passive, PriorityNormal)
	/*
		### @var ###

			var Name::Type; var Name::Type = Value; var Name = Value

		Defines a variable or field in the current scope. You can define more than one at a time, e.g. 'var X::int Name::string;'
	*/
	@var; // Macros.var (Mode = PriorityNormal)
	/*
		### @private ###

			private <declaration>

		Indicates that a method, field or inner type is private, meaning it is inaccessible outside the scope in which it is defined.
	*/
	@private; // Macros.private (Mode = PriorityNormal)
	/*
		### @ref ###

			[ref]

		Used as an attribute on a method parameter to indicate that it is passed by reference. This means the caller must pass a variable (not a value), and that the caller can see changes to the variable.
	*/
	@ref; // Macros.ref (Mode = PriorityNormal)
	/*
		### @short ###

			short

		A signed 16-bit data type
	*/
	@short; // Macros.short (Mode = PriorityNormal)
	/*
		### cast ###

			cast(Expr, Type); Expr \cast Type

		Converts an expression to a new data type.
	*/
	cast; // Macros.cast (Mode = PriorityNormal)
	/*
		### #of ###

			array!Type; opt!Type; ptr!Type

		array!Type represents an array of Type; opt!Type represents the nullable version of Type; ptr!Type represents a pointer to Type.
	*/
	#of; // Macros.of (Mode = Passive, PriorityNormal)
	/*
		### @def ###

			def Name(Args...) { Body... }; def Name(Args...)::ReturnType { Body }; def Name ==> ForwardingTarget { Body }

		Defines a function (also known as a method). The '==> ForwardingTarget' version is not implemented.
	*/
	@def; // Macros.def (Mode = PriorityNormal)
	/*
		### @throw ###

			return; return Expr

		Returns to the caller of the current method or lambda function.
	*/
	@throw; // Macros.throw (Mode = PriorityNormal)
	/*
		### @uint ###

			uint

		An unsigned 32-bit data type
	*/
	@uint; // Macros.uint (Mode = PriorityNormal)
	/*
		### @ushort ###

			ushort

		An unsigned 16-bit data type
	*/
	@ushort; // Macros.ushort (Mode = PriorityNormal)
	/*
		### @`=` ###

			Name::Type = Value; Name::Type := Value

		Defines a variable or field in the current scope.
	*/
	@`=`; // Macros.ColonColonInit (Mode = Passive, PriorityNormal)
	/*
		### @void ###

			void

		An empty data type that always has the same value, known as '@void'
	*/
	@void; // Macros.void (Mode = PriorityNormal)
	/*
		### pub ###

			[pub]

		Used as an attribute to indicate that a type, method or field is publicly accessible.
	*/
	pub; // Macros.pub (Mode = PriorityNormal)
	/*
		### @break ###

			break

		Exit the loop or switch body (the innermost loop, if more than one enclosing loop)
	*/
	@break; // Macros.break (Mode = PriorityNormal)
	/*
		### @char ###

			char

		A 16-bit single-character data type
	*/
	@char; // Macros.char (Mode = PriorityNormal)
	/*
		### @continue ###

			continue

		Jump to the end of the loop body, running the loop again if the loop condition is true.
	*/
	@continue; // Macros.continue (Mode = PriorityNormal)
	/*
		### @do ###

			do {Body...} while Condition; do {Body...} while(Condition)

		Runs the Body code repeatedly, as long as 'Condition' is true. The Condition is checked after Body has already run.
	*/
	@do; // Macros.do (Mode = PriorityNormal)
	/*
		### priv ###

			[priv]

		Used as an attribute to indicate that a method, field or inner type is private, meaning it is inaccessible outside the scope in which it is defined.
	*/
	priv; // Macros.priv (Mode = PriorityNormal)
	/*
		### @`=:` ###

			Value=:Name

		Defines a variable or field in the current scope.
	*/
	@`=:`; // Macros.QuickBind (Mode = PriorityNormal)
}
