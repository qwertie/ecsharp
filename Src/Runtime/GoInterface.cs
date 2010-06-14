using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Loyc.Runtime
{
	internal static class GoInterface
	{
		internal static readonly AssemblyBuilder AssemblyBuilder;
		internal static readonly ModuleBuilder ModuleBuilder;
		
		static GoInterface()
		{
			// Create a single assembly and module to hold all generated classes.
			var name = new AssemblyName { Name = "GoInterfaceGeneratedClasses" };
			AssemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
			ModuleBuilder = AssemblyBuilder.DefineDynamicModule("Module", false);
		}
	}
	
	/// <summary>GoInterface&lt;Interface> creates wrappers around objects of your 
	/// choosing that implement the specified Interface, forwarding calls to 
	/// methods in the wrapped object. It is inspired by the duck-typed interfaces 
	/// in the Go programming language.</summary>
	/// <remarks>
	/// In the Go programming language, you do not say explicitly that your type 
	/// implements a given interface. Instead, a type is convertable to <i>any</i>
	/// interface, just so long as it implements all the methods in the interface.
	/// Function calls through an interface in Go are much faster than "duck 
	/// typing" in dynamic languages such as Python or Ruby; in fact, they are the 
	/// same speed as virtual method calls in C++ and C#!
	/// <para/>
	/// To put it in C# terms, if you have a class T...
	/// <pre>public class T {
	///     public void Foo(int x);
	/// }</pre>
	/// 
	/// ...and an interface called "Interface"...
	/// 
	/// <pre>public interface Interface {
	///     void Foo(int x);
	/// }</pre>
	///
	/// ...then you can cast T to Interface even though T does not explicitly implement it.
	///
	/// <pre>Interface t = new T();</pre>
	/// 
	/// This cast can be implicit since the compiler can tell at compile time that
	/// T implements the Interface. However, you can cast any object to Interface 
	/// and, at run-time, Go will determine whether it implements the Interface.
	/// <para/>
	/// I asked how Go dispatch works on the "Go Nuts" google group and was pointed 
	/// to two articles:
	/// <para/>
	/// http://research.swtch.com/2009/12/go-data-structures-interfaces.html
	/// http://www.airs.com/blog/archives/281
	/// <para/>
	/// To summarize those, the first time you convert a type "T" to an interface 
	/// "Interface", a vtable (virtual function table) is generated just like the 
	/// kind used for virtual calls in .NET and C++. However, instead of storing 
	/// the vtable in the object itself like C++ and .NET do, Go stores the vtable 
	/// pointer alongside the interface pointer (i.e. an interface pointer is 
	/// really two pointers). This simple but unique design allows a single object
	/// to implement an unlimited number of interfaces with overall performance 
	/// that is competitive with C# and Java.
	/// <para/>
	/// Unfortunately, as far as I can tell, there is no way to efficiently 
	/// implement this same technique in .NET without changing the CLR itself. A 
	/// virtual method table is just a list of pointers to functions; importantly, 
	/// function pointers in a virtual method table are not associated with a 
	/// specific object, which makes them different from .NET delegates. By not 
	/// associating the vtable with a specific object, it is possible to re-use the
	/// same vtable with any number of objects (as long as they are of the same 
	/// class). However, .NET delegates are associated with specific objects, so
	/// we can't use them to form a reusable vtable.
	/// <para/>
	/// Even if .NET allowed delegates that are not associated with a specific 
	/// object, delegate invocation on .NET is slower than virtual method 
	/// invocation; why this is so is not entirely clear to me, but part of the 
	/// reason may be that Microsoft decided to make delegates reference types 
	/// when they should have been a simpler 8-byte value type (just bundling a 
	/// function pointer with a 'this' pointer).
	/// <para/>
	/// However, just a few days ago I learned that Visual Basic 9 has a very 
	/// similar feature to Go called "dynamic interfaces", which pretty much lets
	/// you do as described above (albeit only in Visual Basic). So far I've 
	/// heard nothing about how VB's dynamic interfaces work, but I got to 
	/// thinking: how hard would it be to bring go-style interfaces to all .NET
	/// languages, and would it be possible to get decent performance?
	/// <para/>
	/// The technique I chose doesn't have performance as good as you would get
	/// from Go, but in exchange for a small performance hit (which I believe to
	/// be unavoidable anyway), the GoInterface classes provide automatic interface 
	/// adaptation that you can't get in Go itself. My GoInterface classes can
	/// automatically do small type conversion tasks like enlarging "int" to 
	/// "long", boxing value types, and allowing return type covariance (for 
	/// instance, if the wrapped method returns a "string", the Interface can 
	/// return an "object".)
	/// <para/>
	/// The GoInterface classes use .NET Reflection.Emit to generate wrapper 
	/// classes in a "dynamic assembly"--basically a DLL that exists only in 
	/// memory. Each wrapper class implements a single interface of your 
	/// choosing, and forwards calls on that interface to an object of your
	/// choosing.
	/// <para/>
	/// Given the types from above...
	/// 
	/// <pre>public class T {
	///     public void Foo(int x);
	/// }
	/// public interface Interface {
	///     void Foo(int x);
	/// }</pre>
	/// 
	/// ...you can use GoInterface to cast T to Interface like this:
	/// 
	/// <pre>Interface t = GoInterface&lt;Interface>.From(new T());</pre>
	/// 
	/// The first time you cast a T to Interface, GoInterface generates a wrapper 
	/// class such as the following on-the-fly:
	/// 
	/// <pre>public class T_6F3E12_3102AF : Interface
	/// {
	///     T _obj;
	///     public T_6F3E12_3102AF(T obj) { _obj = obj; }
	///     void Foo(int x) { _obj.Foo(x); }
	/// }</pre>
	/// 
	/// The hex numbers in the name of the type are simply handles to interface and 
	/// type being wrapped, in order to guarantee no name collisions occur when you 
	/// are wrapping a lot of different classes with GoInterface.
	/// <para/>
	/// After the first cast, all future casts are fairly fast, especially if
	/// you call GoInterface&lt;Interface,T>.From() instead of just 
	/// GoInterface&lt;Interface>.From(). That's because after
	/// GoInterface&lt;Interface,T> is fully initialized, all its From() method 
	/// does is invoke a delegate that contains the following code:
	/// 
	/// <pre>delegate(T obj) { return new T_6F3E12_3102AF(obj); }</pre>
	/// 
	/// You can create wrappers either with GoInterface&lt;Interface> or 
	/// GoInterface&lt;Interface, T> (note the extra type argument "T").
	/// <ul>
	/// <li>GoInterface&lt;Interface> is intended for creating wrappers when you 
	/// do not know the type of the object at compile time. For example, if you
	/// have a list of objects of unknown type and you want to cast them to an
	/// interface, use this one.</li>
	/// <li>GoInterface&lt;Interface, T> creates wrappers when you already know 
	/// the type of the object at compile time. This version assumes that T itself 
	/// (and not some derived class!) contains the methods you want to call.
	/// GoInterface&lt;Interface, T> has the disadvantage that it is unable to 
	/// call methods in a derived class of T. For example, you should not use
	/// GoInterface&lt;Interface, object> because the object class does not 
	/// contain a Foo method.</li>
	/// </ul>
	/// If you're not sure which one to use, use GoInterface&lt;Interface>. If you
	/// need to adapt a large number of objects to a single interface, you should
	/// use GoInterface&lt;Interface, T> where possible, because it is slightly 
	/// faster. GoInterface&lt;Interface>, in contrast, has to examine each object
	/// it is given to find out its most derived type. However, this process is 
	/// optimized so that an expensive analysis is only done once per derived type,
	/// after which only a hashtable lookup is required.
	/// <para/>
	/// Compared to interfaces in the Go programming language, which have a 1-word
	/// overhead for every interface pointer (the vtable pointer, which is 4 bytes 
	/// in 32-bit code), GoInterface wrappers normally have 3 words of overhead 
	/// (2 words for the wrapper's object header and 1 word for a reference to the
	/// wrapped object). Also, GoInterface wrapper classes are no doubt much more
	/// costly to produce (since they involve run-time code generation), which may 
	/// increase your program's startup time and have a fixed memory overhead that
	/// dwarfs Go's implementation. However, once you are up-and-running with
	/// GoInterface wrappers, their performance should be pretty good. TODO: benchmarks
	/// <para/>
	/// Note: GoInterface can create wrappers for value types (structures), not 
	/// just classes. Such wrappers have the same memory overhead as boxed 
	/// structures, which is one word less than wrappers for reference types.
	/// </remarks>
	public static class GoInterface<Interface> where Interface : class
	{
		public static Interface From<T>(T anything)
		{
			if (anything.GetType().TypeHandle.Value == typeof(T).TypeHandle.Value)
				return GoInterface<Interface, T>.From(anything);
			else
				return GetFactory(typeof(T).TypeHandle).From(anything);
		}
		public static Interface ForceFrom<T>(T anything)
		{
			if (anything.GetType().TypeHandle.Value == typeof(T).TypeHandle.Value)
				return GoInterface<Interface, T>.ForceFrom(anything);
			else
				return GetFactory(typeof(T).TypeHandle).ForceFrom(anything);
		}
		public static Interface From<T>(T anything, CastOptions options)
		{
			if (anything.GetType().TypeHandle.Value == typeof(T).TypeHandle.Value)
				return GoInterface<Interface, T>.From(anything, options);
			else
				return GetFactory(typeof(T).TypeHandle).From(anything, options);
		}

		private static GoInterfaceFactory<Interface> GetFactory(RuntimeTypeHandle hType)
		{
			GoInterfaceFactory<Interface> factory;
			if (!_factories.TryGetValue(hType.Value, out factory))
			{
				Type T = Type.GetTypeFromHandle(hType);

				if (typeof(Interface).IsAssignableFrom(T))
					factory = new GoDirectCaster<Interface>();
				else {
					Type factoryType = typeof(GoInterface<,>.Factory).MakeGenericType(new Type[] { typeof(Interface), T });
					factory = (GoInterfaceFactory<Interface>)Activator.CreateInstance(factoryType);
				}

				_factories[hType.Value] = factory;
			}
			return factory;
		}

		static Dictionary<IntPtr, GoInterfaceFactory<Interface>> _factories = new Dictionary<IntPtr,GoInterfaceFactory<Interface>>();
	}
	
	/// <summary>Options you can pass to GoInterface.FromEx()</summary>
	[Flags]
	public enum CastOptions
	{
		/// <summary>If there is a mismatch, return null instead of throwing InvalidCastException</summary>
		As = 1,
		/// <summary>Allow the cast even if NumberOfUnmatchedMethods > 0</summary>
		AllowUnmatchedMethods = 2,
		/// <summary>Allow the cast even if NumberOfMethodsWithRefMismatch > 0</summary>
		AllowRefMismatch = 4,
		/// <summary>Allow the cast even if NumberOfMethodsMissingParameters > 0</summary>
		AllowMissingParams = 8,
	}

	/// <summary>Base class of GoInterface&lt;Interface,T>.Factory and GoDirectCaster&lt;Interface></summary>
	internal abstract class GoInterfaceFactory<Interface>
	{
		public abstract Interface From(object obj);
		public abstract Interface From(object obj, CastOptions options);
		public abstract Interface ForceFrom(object obj);
	}
	
	/// <summary>Used by GoInterface&lt;Interface> to cast objects directly to an 
	/// interface when it turns out that they implement that interface.</summary>
	internal class GoDirectCaster<Interface> : GoInterfaceFactory<Interface>
	{
		public override Interface From(object obj)                      { return (Interface)obj; }
		public override Interface From(object obj, CastOptions options) { return (Interface)obj; }
		public override Interface ForceFrom(object obj) { return (Interface)obj; }
	}

	/// <summary>GoInterface&lt;Interface,T> creates a wrapper that implements 
	/// the specified Interface, forwarding calls to methods in T. It is inspired 
	/// by the duck-typed interfaces in the Go programming language.</summary>
	/// <remarks>
	/// Due to 
	/// </remarks>
	public static class GoInterface<Interface, T> where Interface:class
	{
		#region Public methods & properties
		
		/// <summary>Creates a wrapper regardless of whether or not T could be 
		/// wrapped completely.</summary>
		/// <exception cref="InvalidOperationException">The Interface is not 
		/// valid (e.g. it is not public or is not abstract)</exception>
		/// <remarks>
		/// GoInterface maps methods in certain cases where you might not 
		/// want it to--for example, if the Interface takes two ints and T only 
		/// takes one, GoInterface maps one to the other by omitting the second 
		/// parameter. To accept these mappings, call ForceFrom(T) or From(T, 
		/// CastOptions.AllowMissingParams | CastOptions.AllowRefMismatch); to 
		/// reject them, call From(T).
		/// <para/>
		/// ForceFrom always creates a wrapper, even if some methods of Interface
		/// couldn't be matched with T at all. If you then call a method that 
		/// couldn't be wrapped, you'll get a MissingMethodException.
		/// </remarks>
		public static Interface ForceFrom(T obj) { return _forceFrom(obj); }

		/// <summary>Creates a wrapper if the interface matches T well.</summary>
		/// <exception cref="InvalidCastException">T does not match the Interface 
		/// very well. Specifically, NumberOfUnmatchedMethods>0, 
		/// NumberOfMethodsMissingParameters>0 or NumberOfMethodsWithRefMismatch>0.</exception>
		/// <returns>A pointer to a wrapper that implements the Interface.</returns>
		public static Interface From(T obj) { return _from(obj); }

		/// <summary>Creates a wrapper if T matches Interface according to the 
		/// specified CastOptions.</summary>
		/// <returns>A pointer to a wrapper that implements the Interface.</returns>
		/// <remarks>See CastOptions for more information.</remarks>
		public static Interface From(T obj, CastOptions opt)
		{
			if (_unmatchedMsg != null && (opt & CastOptions.AllowUnmatchedMethods) == 0)
			{
				if ((opt & CastOptions.As) != 0)
					return null;
				throw new InvalidCastException(_unmatchedMsg);
			}
			if (_refMismatchMsg != null && (opt & CastOptions.AllowRefMismatch) == 0)
			{
				if ((opt & CastOptions.As) != 0)
					return null;
				throw new InvalidCastException(_refMismatchMsg);
			}
			if (_omittedParamMsg != null && (opt & CastOptions.AllowMissingParams) == 0)
			{
				if ((opt & CastOptions.As) != 0)
					return null;
				throw new InvalidCastException(_omittedParamMsg);
			}
			try {
				return ForceFrom(obj);
			} catch {
				if ((opt & CastOptions.As) != 0)
					return null;
				throw;
			}
		}

		/// <summary>If this value is false, Interface is not valid and ForceFrom 
		/// will throw InvalidOperationException if called. In that case, the other 
		/// values such as NumberOfUnmatchedMethods are zero, but have no meaning.</summary>
		/// <remarks>Calling this property or any of the "int" properties forces 
		/// the wrapper class to be generated, during which the relationship 
		/// between Interface and T is analyzed.</remarks>
		public static bool IsValidInterface
		{
			get { AutoInit(); return _isValidInterface; }
		}

		/// <summary>The number of methods in the interface for which a matching 
		/// method in T could not be found.</summary>
		public static int NumberOfUnmatchedMethods
		{
			get { AutoInit(); return _numberOfUnmatchedMethods; }
		}

		/// <summary>The number of methods in the interface for which a matching 
		/// method in T could not be found because T contained more than one 
		/// equally suitable match.</summary>
		public static int NumberOfAmbiguousMethods
		{
			get { AutoInit(); return _numberOfAmbiguousMethods; }
		}

		/// <summary>The number of methods in the interface that were matched to 
		/// a method in T with one or more parameters dropped. For instance, if a
		/// method in the interface takes two ints but T's method only takes one
		/// int, the second int of </summary>
		public static int NumberOfMethodsMissingParameters
		{
			get { AutoInit(); return _numberOfMethodsMissingParameters; }
		}

		/// <summary>The number of methods in the interface that were matched to a 
		/// method in T, in which there was a mismatch that one parameter was 
		/// passed by reference ("ref") and the other was passed by value.</summary>
		public static int NumberOfMethodsWithRefMismatch
		{
			get { AutoInit(); return _numberOfMethodsWithRefMismatch; }
		}

		/// <summary>Defines a custom wrapper class for the type pair (Interface, 
		/// T). If you want to install a custom wrapper, you must do so before 
		/// From() or ForceFrom() is ever called on this pair of types, otherwise 
		/// an exception is thrown. Also, you cannot call this method twice on 
		/// the same pair of types.</summary>
		/// <param name="from">A method to be invoked by From().</param>
		/// <param name="forceFrom">A method to be invoked by ForceFrom().</param>
		/// <remarks>
		/// Since generating a wrapper is expensive and the wrapper cannot be 
		/// garbage-collected, I decided to make sure you don't waste time and
		/// memory generating a wrapper you don't intend to use, by restricting 
		/// this method only to be used on type pairs that have never been used 
		/// before. Make sure you call this method as soon as possible, before 
		/// anybody calls From() or ForceFrom().
		/// </remarks>
		public static void DefineCustomWrapperCreator(GoWrapperCreator from, GoWrapperCreator forceFrom)
		{
			if (forceFrom == null)
				throw new ArgumentNullException("forceFrom");
			if (from == null)
				throw new ArgumentNullException("from");
			if (_forceFrom != null)
				throw new InvalidOperationException(string.Format("A wrapper for GoInterface<{0},{1}> is already defined.", typeof(Interface).Name, typeof(T).Name));
			_from = from;
			_forceFrom = forceFrom;
			_isValidInterface = true;
		}
		
		#endregion

		#region Factory class

		/// <summary>This helper class allows GoInterface&lt;Interface> to cache 
		/// access to the static methods in GoInterface&lt;Interface, T>, so that 
		/// it only needs to use reflection once.</summary>
		internal class Factory : GoInterfaceFactory<Interface>
		{
			public override Interface From(object obj)
			{
				return GoInterface<Interface, T>.From((T)obj);
			}
			public override Interface From(object obj, CastOptions options)
			{
				if ((options & CastOptions.As) != 0 && !(obj is T))
					return null;
				return From((T)obj, options);
			}
			public override Interface ForceFrom(object obj)
			{
				return GoInterface<Interface, T>.ForceFrom((T)obj);
			}
		}

		#endregion

		#region static variables, static constructor & related

		public delegate Interface GoWrapperCreator(T obj);

		private static GoWrapperCreator _forceFrom;
		private static GoWrapperCreator _from;
		private static string _unmatchedMsg;    // "Cannot cast <T> to <Interface>: <mismatchCount> methods are [missing or ambiguous]"
		private static string _refMismatchMsg;  // "Cannot cast <T> to <Interface>": <mismatchCount> methods have mismatched 'ref' parameters"
		private static string _omittedParamMsg; // "Cannot cast <T> to <Interface>": <mismatchCount> methods have omitted parameters"
		static int _numberOfUnmatchedMethods = 0; // For documentation, see the corresponding properties
		static int _numberOfAmbiguousMethods = 0;
		static int _numberOfMethodsMissingParameters = 0;
		static int _numberOfMethodsWithRefMismatch = 0;
		static bool _isInitialized = false;
		static bool _isValidInterface;
		static Type _wrapperType;
		
		static void AutoInit()
		{
			if (!_isInitialized)
				GenerateWrapperClass();
		}
		static GoInterface()
		{
			_from = GenerateWrapperClassWhenUserCallsFrom;
			_forceFrom = GenerateWrapperClassWhenUserCallsForceFrom;
		}
		static Interface GenerateWrapperClassWhenUserCallsFrom(T obj)
		{
			GenerateWrapperClass();
			return _from(obj);
		}
		static Interface GenerateWrapperClassWhenUserCallsForceFrom(T obj)
		{
			GenerateWrapperClass();
			return _forceFrom(obj);
		}

		#endregion

		#region GenerateWrapperClass & related (higher-level code generation)

		static void GenerateWrapperClass()
		{
			_isInitialized = true;

			// We need to generate two things:
			// 1. A class that implements the Interface or, if Interface is an
			//    abstract class, overrides its abstract methods.
			TypeAttributes typeFlags = TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed;
			string typeName = string.Format("{0}_{1:X}_{2:X}", typeof(T).Name, typeof(Interface).TypeHandle.Value.ToInt64(), typeof(T).TypeHandle.Value.ToInt64());
			TypeBuilder typeBuilder = null;

			// WTF? A public nested class is not considered public. Ideally I would 
			// ask "are we allowed to access type Interface from within the dynamic 
			// assembly?", but I don't know how.
			if (typeof(Interface).IsPublic || typeof(Interface).IsNestedPublic)
			{
				if (typeof(Interface).IsInterface)
				{
					typeBuilder = GoInterface.ModuleBuilder.DefineType(typeName, typeFlags);
					typeBuilder.AddInterfaceImplementation(typeof(Interface));
				}
				else if (IsAcceptableBaseClass(typeof(Interface)))
				{
					typeBuilder = GoInterface.ModuleBuilder.DefineType(typeName, typeFlags, typeof(Interface));
				}
			}

			if (typeBuilder == null)
			{	// There's no way to make a wrapper out of the Interface class.
				// Generate a dummy wrapper with an Invalid() method that simply 
				// throws an InvalidOperationException.
				typeName = string.Format("{0}_{1:X}", typeof(Interface).Name, typeof(Interface).TypeHandle.Value);
				if ((_wrapperType = (TypeBuilder)GoInterface.ModuleBuilder.GetType(typeName)) == null)
					_wrapperType = GenerateDummyWrapperForInvalidInterface(typeFlags, typeName);

				_forceFrom = _from = (GoWrapperCreator)Delegate.CreateDelegate(typeof(GoWrapperCreator), _wrapperType.GetMethod("Invalid"));
				return;
			}

			ConstructorInfo constructor = GenerateMembersOfWrapperType(typeBuilder);

			// 2. The ForceFrom method, which simply does this...
			//
			//    static Interface From(T obj) { return new GeneratedClass(obj); }
			//
			//    ...and the _from method, which either does the same thing OR 
			//    throws an exception, depending on whether T was close enough 
			//    to Interface.
			MethodBuilder forceFromMB = GenerateTheForceFromMethod(typeBuilder, constructor);
			MethodBuilder fromMB;
			if (NumberOfUnmatchedMethods == 0 && NumberOfMethodsMissingParameters == 0 && NumberOfMethodsWithRefMismatch == 0)
				fromMB = forceFromMB;
			else
			{	// Generate a From() method that throws an exception.

				// But first, generate message(s) to describe the error(s); these 
				// messages may also be used by From(T, CastOptions), so store the 
				// messages in static variables.
				string baseMsg = string.Format("Cannot cast {0} to {1}: ", typeof(T).Name, typeof(Interface).Name);

				if (NumberOfUnmatchedMethods > 0)
				{
					string term = "unmatched";
					if (NumberOfAmbiguousMethods > 0)
						term = (NumberOfAmbiguousMethods == NumberOfUnmatchedMethods ? "ambiguous" : "unmatched or ambiguous");
					_unmatchedMsg = baseMsg + string.Format("{0} {1} {2}", NumberOfUnmatchedMethods, NumberOfUnmatchedMethods > 1 ? "methods are" : "method is", term);
				}
				if (NumberOfMethodsWithRefMismatch > 0)
					_refMismatchMsg = baseMsg + string.Format("{0} method{1} have mismatched 'ref' parameters", NumberOfMethodsWithRefMismatch, NumberOfMethodsWithRefMismatch > 1 ? "s" : "");
				if (NumberOfMethodsMissingParameters > 0)
					_omittedParamMsg = baseMsg + string.Format("{0} method{1} have omitted parameters", NumberOfMethodsMissingParameters, NumberOfMethodsMissingParameters > 1 ? "s" : "");

				fromMB = typeBuilder.DefineMethod("FromFails",
									 MethodAttributes.Static | MethodAttributes.Public,
									 typeof(Interface), Array(typeof(T)));
				ILGenerator il = fromMB.GetILGenerator();
				il.Emit(OpCodes.Ldstr, _unmatchedMsg ?? _omittedParamMsg ?? _refMismatchMsg);
				ConstructorInfo exception = typeof(InvalidCastException).GetConstructor(Array(typeof(string)));
				il.Emit(OpCodes.Newobj, exception);
				il.Emit(OpCodes.Throw);
			}

			// Crystallize the type
			_wrapperType = typeBuilder.CreateType();

			// Reflection hidden rule #87: CreateDelegate requires you to specify a 
			// new MethodInfo from the created type, not to re-use a MethodBuilder
			_forceFrom = (GoWrapperCreator)Delegate.CreateDelegate(typeof(GoWrapperCreator), ReacquireMethod(_wrapperType, forceFromMB));
			_from = (GoWrapperCreator)Delegate.CreateDelegate(typeof(GoWrapperCreator), ReacquireMethod(_wrapperType, fromMB));

			_isValidInterface = true;
		}

		private static MethodInfo ReacquireMethod(Type type, MethodInfo method)
		{
			BindingFlags flags = BindingFlags.DeclaredOnly;
			flags |= (method.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic);
			flags |= (method.IsStatic ? BindingFlags.Static : BindingFlags.Instance);
			MethodInfo m = type.GetMethod(method.Name, flags, null, ParameterTypes(method), null);
			Debug.Assert(m != null);
			return m;
		}

		private static Type GenerateDummyWrapperForInvalidInterface(TypeAttributes typeFlags, string typeName)
		{
			TypeBuilder typeBuilder;
			typeBuilder = GoInterface.ModuleBuilder.DefineType(typeName, typeFlags);

			MethodBuilder method = typeBuilder.DefineMethod("Invalid",
								   MethodAttributes.Static | MethodAttributes.Public,
								   typeof(Interface), Array(typeof(T)));
			ILGenerator il = method.GetILGenerator();
			il.Emit(OpCodes.Ldstr,
				string.Format(typeof(Interface).IsInterface
					? "GoInterface: Cannot wrap \"{0}\". Make sure it is public and contains no events or generic methods."
					: "GoInterface: Cannot wrap \"{0}\". Make sure it is public, abstract, has a default constructor, and that there are no abstract events or abstract generic methods.",
					typeof(Interface).Name));
			ConstructorInfo exception = typeof(InvalidOperationException).GetConstructor(Array(typeof(string)));
			il.Emit(OpCodes.Newobj, exception);
			il.Emit(OpCodes.Throw);

			return typeBuilder.CreateType();
		}

		private static bool IsAcceptableBaseClass(Type type)
		{
			if (!type.IsAbstract || type == typeof(Array) || type == typeof(Delegate))
				return false;
			ConstructorInfo c = GetDefaultConstructor(type);
			return c != null && (c.IsPublic || c.IsFamily);
		}

		private static ConstructorInfo GetDefaultConstructor(Type type)
		{
			return type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, System.Type.EmptyTypes, null);
		}

		static FieldBuilder _obj;

		struct MethodAndParams
		{
			public MethodInfo Method;
			public ParameterInfo[] Params;
		}

		private static ConstructorInfo GenerateMembersOfWrapperType(TypeBuilder typeBuilder)
		{
			// In the wrapper type, create a constructor and a pointer to the wrapped object:
			//     private readonly T _obj;
			//     constructor(T obj) : base() { this._obj = obj; }
			_obj = typeBuilder.DefineField("_obj", typeof(T), FieldAttributes.Private | FieldAttributes.InitOnly);
			ConstructorBuilder constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Array(typeof(T)));
			constructor.DefineParameter(1, ParameterAttributes.In, "obj");
			ILGenerator il = constructor.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Call, GetDefaultConstructor(typeBuilder.BaseType));
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Stfld, _obj);
			il.Emit(OpCodes.Ret);

			// Reflect over Interface to find out what methods need to be 
			// implemented in the wrapper class, and generate them. Call 
			// GetParameters() on all T methods in advance so we don't waste 
			// time calling it many times throughout this process.
			MethodInfo[] methodsOfT = typeof(T).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
			MethodAndParams[] methodsOfT2 = new MethodAndParams[methodsOfT.Length];
			for (int i = 0; i < methodsOfT2.Length; i++)
			{
				methodsOfT2[i].Method = methodsOfT[i];
				methodsOfT2[i].Params = methodsOfT[i].GetParameters();
			}
			foreach (MethodInfo methodOfI in typeof(Interface).GetMethods(BindingFlags.Public | BindingFlags.Instance))
			{
				if (methodOfI.IsAbstract)
					GenerateWrapperMethod(typeBuilder, methodOfI, methodsOfT2);
				else
					Debug.Assert(!typeof(Interface).IsInterface);
			}
			return constructor;
		}

		private static MethodBuilder GenerateTheForceFromMethod(TypeBuilder typeBuilder, ConstructorInfo constructor)
		{
			MethodBuilder method = typeBuilder.DefineMethod("ForceFrom",
								   MethodAttributes.Static | MethodAttributes.Public,
								   typeof(Interface), Array(typeof(T)));
			ILGenerator il = method.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Newobj, constructor);
			il.Emit(OpCodes.Ret);

			return method;
		}

		#endregion

		#region GenerateWrapperMethod & related (lower-level code generation)

		/// <summary>Generates a method that implements baseMethod (a method of
		/// Interface) and forwards the call to the same method on T.</summary>
		/// <remarks>"Safe" parameter variance is allowed between Interface and T,
		/// such as return type covariance. Support for default parameters is not
		/// implemented.</remarks>
		private static bool GenerateWrapperMethod(TypeBuilder typeBuilder, MethodInfo baseMethod, MethodAndParams[] methodsOfT)
		{
			// Note: if you do not properly associate a method with the interface 
			// method it is overriding, CreateType() will throw a misleading 
			// exception claiming that the method of the class "does not have an 
			// implementation"--without mentioning the interface at all. To 
			// override a base class method, you just have to make sure the derived 
			// class method is virtual and has the same name. To implement an 
			// interface method you must do the same thing (I hate that Microsoft 
			// forces you to make the method Virtual), except that to make an 
			// explicit interface implementation, you use a different name 
			// (typically with dots, like "IEnumerable.GetEnumerator"), make the
			// method private, and call MethodBuilder.DefineMethodOverride to link
			// the method to the interface.
			//
			// (If you want the method to be non-virtual when called via the class,
			// you actually have to create two methods: an explicit interface 
			// method, which you mark Virtual and link to the interface, and a non-
			// virtual public method to expose in the class. The interface method 
			// then has to forward the call to the non-virtual method. PIA!)

			MethodAttributes flags = MethodAttributes.Public | MethodAttributes.HideBySig 
			                       | MethodAttributes.Virtual | MethodAttributes.Final;
			if (!typeof(Interface).IsInterface)
				flags |= MethodAttributes.ReuseSlot;

			// Create the method, copying parameter and return type info from the base method
			var bmps = baseMethod.GetParameters();
			var paramTypes = ParameterTypes(bmps);
			MethodBuilder method = typeBuilder.DefineMethod(baseMethod.Name, flags, baseMethod.ReturnType, paramTypes);
			foreach (ParameterInfo param in bmps)
				method.DefineParameter(param.Position + 1, param.Attributes, param.Name);

			//typeBuilder.DefineMethodOverride(method, baseMethod);

			method.SetImplementationFlags(MethodImplAttributes.Managed | MethodImplAttributes.IL);

			List<MethodInfo> matchesInT = GetMatchingMethods(baseMethod, methodsOfT);

			MethodInfo bestMatchInT = null;
			if (matchesInT != null)
			{
				bestMatchInT = ChooseBestMatch(baseMethod, matchesInT);
				if (bestMatchInT == null)
					_numberOfAmbiguousMethods++;
			}
			
			// Generate the code inside the method, which calls the matching method
			// on _obj. If no matching method was selected, we throw an exception
			// instead.
			if (bestMatchInT != null)
			{
				GenerateForwardingCode(baseMethod, method, bestMatchInT);
			}
			else
			{
				_numberOfUnmatchedMethods++;

				ILGenerator il = method.GetILGenerator();
				il.Emit(OpCodes.Ldstr, string.Format("Missing method: {0}.{1}", typeof(Interface).Name, baseMethod.Name));
				ConstructorInfo exception = typeof(MissingMethodException).GetConstructor(Array(typeof(string)));
				il.Emit(OpCodes.Newobj, exception);
				il.Emit(OpCodes.Throw);
			}

			return true;
		}

		private static Type[] ParameterTypes(MethodInfo method) 
		{
			return ParameterTypes(method.GetParameters());
		}
		private static Type[] ParameterTypes(ParameterInfo[] @params)
		{
			var paramTypes = new Type[@params.Length];
			for (int i = 0; i < @params.Length; i++)
				paramTypes[i] = @params[i].ParameterType;
			return paramTypes;
		}

		private static void GenerateForwardingCode(MethodInfo baseMethod, MethodBuilder method, MethodInfo bestMatchInT)
		{
			ILGenerator il = method.GetILGenerator();
			
			int refMismatches, missingParams;
			// Note: .NET won't let you call MethodBuilder.GetParameters(), so use the baseMethod
			ParameterInfo[] myParams = baseMethod.GetParameters();
			int commonCount = GetMatchingParameterCount(myParams, bestMatchInT.GetParameters(), out refMismatches, out missingParams);
			
			if (refMismatches != 0)
				_numberOfMethodsWithRefMismatch++;
			if (missingParams != 0)
				_numberOfMethodsMissingParameters++;

			if (!bestMatchInT.IsStatic)
			{
				// Get a pointer to the target, to use as the first argument.
				il.Emit(OpCodes.Ldarg_0);
				if (typeof(T).IsValueType)
					il.Emit(OpCodes.Ldflda, _obj);
				else
					il.Emit(OpCodes.Ldfld, _obj);
			}

			ParameterInfo[] toParams = bestMatchInT.GetParameters();
			Debug.Assert(myParams.Length >= commonCount && toParams.Length >= commonCount);

			LocalBuilder[] locals = new LocalBuilder[toParams.Length];

			// Add each parameter to the stack
			for (int i = 0; i < commonCount; i++)
				EmitInputParameterConversion(il, i, myParams[i], toParams[i], locals);
			for (int i = commonCount; i < toParams.Length; i++)
				EmitMissingParameter(il, i, toParams[i], locals);

			// Call the function
			il.Emit(typeof(T).IsInterface ? OpCodes.Callvirt : OpCodes.Call, bestMatchInT);

			// Transfer any covariant "out" parameters to the arguments they came in
			for (int i = 0; i < commonCount; i++)
				if (locals[i] != null)
				{
					Debug.Assert(IsRefOrOut(myParams[i]));
					EmitLdArg(il, i); // load *pointer* to out parameter of wrapper method
					il.Emit(OpCodes.Ldloc, locals[i].LocalIndex);
					EmitImplicitConv(il, NotByRef(locals[i].LocalType), NotByRef(myParams[i].ParameterType));
					EmitStInd(il, myParams[i].ParameterType);
				}

			// If the return value is covariant, convert it
			if (method.ReturnType == typeof(void))
				il.Emit(OpCodes.Pop);
			else
				EmitImplicitConv(il, bestMatchInT.ReturnType, method.ReturnType);

			il.Emit(OpCodes.Ret);
		}

		private static void EmitInputParameterConversion(ILGenerator il, int i, ParameterInfo from, ParameterInfo to, LocalBuilder[] locals)
		{
			if (!IsRefOrOut(from) && !IsRefOrOut(to)) {
				// This is a normal input argument.
				ConvType convType = ImplicitConvType(from.ParameterType, to.ParameterType);
				Debug.Assert((int)convType > 0);
				EmitLdArg(il, i);
				if (convType != ConvType.IsA)
					EmitImplicitConv(il, from.ParameterType, to.ParameterType);
			} else if (IsOut(to)) {
				// The target is an output parameter, so we must provide the 
				// address of a variable where the result will go. If the argument
				// types matched exactly, output directly into the argument; 
				// otherwise, create a local variable and output into that.
				if (NotByRef(from.ParameterType) == NotByRef(to.ParameterType)) {
					if (IsRefOrOut(from))
						EmitLdArg(il, i); // already an address
					else
						il.Emit(OpCodes.Ldarga, i + 1);
				} else {
					locals[i] = il.DeclareLocal(to.ParameterType);
					il.Emit(OpCodes.Ldloca, locals[i]);
				}
			} else {
				Debug.Assert(IsRef(from) || IsRef(to));
				throw new NotImplementedException();
			}
		}

		private static void EmitMissingParameter(ILGenerator il, int i, ParameterInfo to, LocalBuilder[] locals)
		{
			if (IsRefOrOut(to))
			{	// Create a local to receive value of 'out' parameter that is missing from Interface
				Debug.Assert(IsOut(to));
				locals[i] = il.DeclareLocal(to.ParameterType);
				il.Emit(OpCodes.Ldloca, locals[i]);
			}
			else
			{	// Get the default value and embed it in the code
				Debug.Assert(HasValidDefaultValue(to));
				object defaultValue = ((DefaultParameterValueAttribute)to.GetCustomAttributes(typeof(DefaultParameterValueAttribute), false)[0]).Value;
				EmitLdc(il, defaultValue);
			}
		}

		private static void EmitLdc(ILGenerator il, object value)
		{
			if (value == null)
				il.Emit(OpCodes.Ldnull);
			else if (value is string)
				il.Emit(OpCodes.Ldstr, (string)value);
			// The primitive types are Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single.
			else if (value is bool)
				il.Emit((bool)value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
			else if (value is byte)
				EmitLdc(il, (byte)value);
			else if (value is sbyte)
				EmitLdc(il, (sbyte)value);
			else if (value is ushort)
				EmitLdc(il, (ushort)value);
			else if (value is short)
				EmitLdc(il, (short)value);
			else if (value is uint)
				EmitLdc(il, (uint)value);
			else if (value is int)
				EmitLdc(il, (int)value);
			else if (value is ulong)
				EmitLdc(il, (ulong)value);
			else if (value is long)
				EmitLdc(il, (long)value);
			else if (value is IntPtr) {
				il.Emit(OpCodes.Ldc_I8, ((IntPtr)value).ToInt64());
				il.Emit(OpCodes.Conv_I);
			} else if (value is UIntPtr) {
				il.Emit(OpCodes.Ldc_I8, ((UIntPtr)value).ToUInt64());
				il.Emit(OpCodes.Conv_U);
			} else if (value is char) {
				EmitLdc(il, (int)(char)value);
			} else if (value is double) {
				il.Emit(OpCodes.Ldc_R8, (double)value);
			} else if (value is float) {
				il.Emit(OpCodes.Ldc_R4, (float)value);
			} else
				throw new NotSupportedException(value.GetType().Name);
		}

		private static void EmitStInd(ILGenerator il, Type type)
		{
			type = NotByRef(type);

			if (!type.IsValueType)
				il.Emit(OpCodes.Stind_Ref);
			else
			{
				bool unsigned;
				int size = PrimSize(type, out unsigned);
				if (size == 1)
					il.Emit(OpCodes.Stind_I1);
				else if (size == 2)
					il.Emit(OpCodes.Stind_I2);
				else if (size == 4)
					il.Emit(OpCodes.Stind_I4);
				else if (size == 8)
					il.Emit(OpCodes.Stind_I8);
				else if (size == -4)
					il.Emit(OpCodes.Stind_R4);
				else if (size == -8)
					il.Emit(OpCodes.Stind_R8);
				else
					il.Emit(OpCodes.Stobj, type);
			}
		}

		private static void EmitImplicitConv(ILGenerator il, Type fromType, Type toType)
		{
			Debug.Assert(!fromType.IsByRef && !toType.IsByRef);

			if (fromType == toType)
				return;

			if (fromType.IsValueType && toType == typeof(object)) {
				il.Emit(OpCodes.Box, fromType);
			} else if (toType.IsPrimitive) {
				Debug.Assert(fromType.IsPrimitive);
				if (toType == typeof(long) || toType == typeof(ulong)) {
					if (IsUnsigned(fromType))
						il.Emit(OpCodes.Conv_U8);
					else
						il.Emit(OpCodes.Conv_I8);
				} else if (toType == typeof(float)) {
					if (IsUnsigned(fromType))
						il.Emit(OpCodes.Conv_R_Un);
					il.Emit(OpCodes.Conv_R4);
				} else if (toType == typeof(double)) {
					if (IsUnsigned(fromType))
						il.Emit(OpCodes.Conv_R_Un);
					il.Emit(OpCodes.Conv_R8);
				} else {
					Debug.Assert(toType == typeof(byte) || toType == typeof(sbyte) ||
					             toType == typeof(ushort) || toType == typeof(short) || 
					             toType == typeof(uint) || toType == typeof(int));
				}
			} else
				Debug.Assert(toType.IsAssignableFrom(fromType));
		}

		#endregion

		#region GetMatchingMethods & related (searches for matching methods in T)
		
		/// <summary>Scans methods of T looking for a method matching a method 
		/// of Interface (baseMethod)</summary>
		/// <remarks>
		/// Before generating code for the method, we must find a matching 
		/// method of T. Note that we want to support various kinds of
		/// "variance", including boxing if necessary:
		/// <para/>
		/// - Return type covariance. If T's method returns a derived class,
		///   Interface's method can return a base class or interface. Also, 
		///   allow any return type if baseMethod returns void.
		/// - "out" parameter covariance.
		/// - T's "out" argument(s) can be absent from Interface, as long as
		///   they are at the end of the argument list.
		/// - Interface's input arguments can be absent from T, as long as the
		///   missing parameters come at the end of the argument list.
		/// - Input parameter contravariance. If T's method accepts a base class
		///   (or interface) such as "object", Interface's method can accept a 
		///   derived class (or a class that implements said interface).
		/// - If T takes a "ref" argument, allow the method in Interface not to
		///   be "ref" and vice versa.
		/// - T's method can be static
		/// <para/>
		/// Implicit conversion operators are not currently detected, except 
		/// primitive type conversions like Int16 to Float that C# allows you 
		/// to do implicitly. Information-losing conversions like Int64 to 
		/// Int32 are not supported.
		/// <para/>
		/// Variable argument lists are not supported specifically (they are 
		/// treated as arrays).
		/// <para/>
		/// Generic methods in T and Interface are not supported.
		/// <para/>
		/// Default (optional) arguments are generally supported.
		/// <para/>
		/// Argument names are ignored and need not match.
		/// <para/>
		/// Methods that differ only in case are not considered, but Aliases 
		/// specified with GoAliasAttribute are considered if a matching method 
		/// with the original name could not be found. All matching aliases are 
		/// considered together, as if they were overloads of each other.
		/// <para/>
		/// We scan all methods of "T" with a matching name and find the best
		/// match, since there may be multiple methods matching these
		/// requirements. For the most part I try to follow the rules of 
		/// the C# standard (ECMA-334 §14.4.2.1 and §14.4.2.2), but be a little 
		/// more liberal. The way I figure it, "void" should be treated like a
		/// base class of "object". Note that this matching algorithm may reject
		/// all overloads due to "ambiguity", in case one overload is not better
		/// than all others.
		/// </remarks>
		private static List<MethodInfo> GetMatchingMethods(MethodInfo baseMethod, MethodAndParams[] methodsOfT)
		{
			List<string> aliases = new List<string>();
			aliases.Add(baseMethod.Name);
			List<MethodInfo> matchesInT = GetMatchingMethods(baseMethod, methodsOfT, aliases);
			if (matchesInT == null)
			{
				// Try aliases from any GoAliasAttributes
				aliases.Clear();
				object[] attr = baseMethod.GetCustomAttributes(typeof(GoAliasAttribute), true);
				for (int i = 0; i < attr.Length; i++)
					aliases.AddRange(((GoAliasAttribute)attr[i]).Aliases);

				if (aliases.Count > 0)
					matchesInT = GetMatchingMethods(baseMethod, methodsOfT, aliases);
			}
			return matchesInT;
		}

		private static List<MethodInfo> GetMatchingMethods(MethodInfo @interface, MethodAndParams[] methodsOfT, List<string> aliases)
		{
			ParameterInfo[] @params = @interface.GetParameters();

			// TODO: support generic methods in T according to C# standard disambiguation rules

			List<MethodInfo> matches = null;
			for (int i = 0; i < methodsOfT.Length; i++)
			{
				MethodInfo methodOfT = methodsOfT[i].Method;

				for (int a = 0; a < aliases.Count; a++)
				{	// Check name
					if (aliases[a] == methodOfT.Name)
					{	// Check return type
						if (IsConvertable(methodOfT.ReturnType, @interface.ReturnType))
						{	// Check arguments
							int dummy1, dummy2;
							if (GetMatchingParameterCount(@params, methodsOfT[i].Params, out dummy1, out dummy2) >= 0)
							{
								if (matches == null)
									matches = new List<MethodInfo>();
								matches.Add(methodOfT);
							}
						}
					}
				}
			}
			return matches;
		}

		#endregion

		#region ChooseBestMatch & related (given a set of methods in T, chooses the best match)

		private static MethodInfo ChooseBestMatch(MethodInfo @interface, List<MethodInfo> matchesInT)
		{
			// The best match is a match that is "better" than all others
			for (int i = 0; i < matchesInT.Count; i++)
			{
				for (int j = 0; ; j++)
				{
					if (j == matchesInT.Count)
						return matchesInT[i]; // best match!
					if (i != j && !IsBetter(matchesInT[i], matchesInT[j], @interface))
						break;
				}
			}
			return null; // no best match
		}

		private static bool IsBetter(MethodInfo method, MethodInfo other, MethodInfo caller)
		{
			var callerArgs = caller.GetParameters();
			var methodArgs = method.GetParameters();
			var otherArgs  = other.GetParameters();
			int dummy1, dummy2;
			int methodCommon = GetMatchingParameterCount(callerArgs, methodArgs, out dummy1, out dummy2);
			int otherCommon = GetMatchingParameterCount(callerArgs, otherArgs, out dummy1, out dummy2);

			if (methodCommon != otherCommon)
				// I don't think ECMA-334 (C# standard) has anything to say about
				// this case. It seems to me that the extra parameter(s) trump all
				// other concerns.
				return methodCommon > otherCommon;

			bool better = false;
			for (int i = 0; i < methodCommon; i++)
			{
				int c = CompareArgs(methodArgs[i], otherArgs[i], callerArgs[i]);
				if (c < 0)
					return false; // worse
				if (c > 0)
					better = true;
			}
			return better;
		}

		private static int CompareArgs(ParameterInfo left, ParameterInfo right, ParameterInfo caller)
		{
			// ECMA-225 §14.4.2.3: Given an implicit conversion C1 that converts
			// from a type S to a type T1, and an implicit conversion C2 that
			// converts from a type S to a type T2, the better conversion of the two
			// conversions is determined as follows:
			// 
			// • If T1 and T2 are the same type, neither conversion is better.
			// • If S is T1, C1 is the better conversion.
			// • If S is T2, C2 is the better conversion.
			// • If an implicit conversion from T1 to T2 exists, and no implicit
			//   conversion from T2 to T1 exists, C1 is the better conversion.
			// • If an implicit conversion from T2 to T1 exists, and no implicit
			//   conversion from T1 to T2 exists, C2 is the better conversion.
			// • If T1 is sbyte and T2 is byte, ushort, uint, or ulong, C1 is the 
			//   better conversion.
			// • If T2 is sbyte and T1 is byte, ushort, uint, or ulong, C2 is the 
			//   better conversion.
			// • If T1 is short and T2 is ushort, uint, or ulong, C1 is the better 
			//   conversion.
			// • If T2 is short and T1 is ushort, uint, or ulong, C2 is the better 
			//   conversion.
			// • If T1 is int and T2 is uint, or ulong, C1 is the better conversion.
			// • If T2 is int and T1 is uint, or ulong, C2 is the better conversion.
			// • If T1 is long and T2 is ulong, C1 is the better conversion.
			// • If T2 is long and T1 is ulong, C2 is the better conversion.
			// • Otherwise, neither conversion is better.
			// 
			// We must expand the rules a little because we support some mapping
			// between input, "ref" and "out" parameters. 
			// • At the very top of that list, I would say that if the in-ref-out 
			//   status of one parameter matches the caller but the other does not, 
			//   the matching parameter is better. C#, in contrast, rejects non-
			//   matching parameters entirely.
			// • Otherwise, if the in-ref-out status differs between the two 
			//   parameters, neither is better.
			// • If both parameters are "out", the decision about which conversion
			//   is better is reversed.
			// 
			// User-defined implicit conversions are not supported; only numeric 
			// conversions are supported.

			bool leftMatchesInOut = GetInOutType(left) == GetInOutType(caller);
			bool rightMatchesInOut = GetInOutType(right) == GetInOutType(caller);
			if (leftMatchesInOut != rightMatchesInOut)
				return leftMatchesInOut ? 1 : -1;

			Debug.Assert(GetInOutType(left) == GetInOutType(right));

			int c = CompareArgs2(left, right, caller);
			return IsOut(left) ? -c : c;
		}
		private static int CompareArgs2(ParameterInfo left, ParameterInfo right, ParameterInfo caller)
		{
			Type leftT = NotByRef(left.ParameterType);
			Type rightT = NotByRef(right.ParameterType);
			Type callerT = NotByRef(caller.ParameterType);
			if (leftT == rightT)
				return 0;

			// We already know there is an implicit conversion to both left and right
			Debug.Assert(IsConvertable(callerT, leftT) && IsConvertable(callerT, rightT));

			ConvType leftToRight = ImplicitConvType(leftT, rightT);
			ConvType rightToLeft = ImplicitConvType(rightT, leftT);
			bool leftIsRight = (int)leftToRight > 0;
			bool rightIsLeft = (int)rightToLeft > 0;
			if (leftIsRight || rightIsLeft)
			{
				if (leftIsRight != rightIsLeft)
					return leftIsRight ? 1 : -1;
				
				Debug.Assert(false);// wait a minute...how can this happen?
				if (leftToRight == ConvType.IsA)
				{
					Debug.Assert(rightT == typeof(object) || rightT == typeof(void));
					return 1; // left is better
				}
				else
				{
					Debug.Assert(rightToLeft == ConvType.IsA);
					Debug.Assert(leftT == typeof(object) || leftT == typeof(void));
					return -1; // right is better
				}
			}

			if (rightToLeft == ConvType.IncompatibleSign)
			{
				Debug.Assert(leftToRight == ConvType.IncompatibleSign);
				Debug.Assert(IsUnsigned(callerT));

				bool leftUnsigned, rightUnsigned;
				int leftSize = PrimSize(leftT, out leftUnsigned);
				int rightSize = PrimSize(leftT, out rightUnsigned);
				Debug.Assert(leftUnsigned != rightUnsigned);
				Debug.Assert(leftUnsigned ? rightSize <= leftSize : leftSize <= rightSize);

				// The C# specification basically says the signed choice is better.
				return leftUnsigned ? -1 : 1;
			}

			return 0;
		}

		#endregion

		#region GetMatchingParameterCount & related (used in both matching & code generation)

		public static int GetMatchingParameterCount(ParameterInfo[] argsI, ParameterInfo[] argsT, out int refMismatches, out int missingParams)
		{
			refMismatches = missingParams = 0;

			int min = Math.Min(argsI.Length, argsT.Length);
			int common;
			for (common = 0; common < min; common++)
			{
				bool refMismatch;
				if (!ArgumentsAreCompatible(argsT[common], argsI[common], out refMismatch))
					break;
				if (refMismatch)
					refMismatches++;
			}

			// Now after the common parameters, there may be parameters that are 
			// missing in one method or the other.

			// If Interface has arguments that T lacks, they must be input-only.
			for (int i = common; i < argsI.Length; i++)
			{
				missingParams++;
				if (IsRefOrOut(argsI[i]))
					return ~common; // fail
			}

			// If T has arguments that Interface lacks, they must be output-only,
			// or they must have a default value.
			for (int i = common; i < argsT.Length; i++)
			{
				missingParams++;
				if (!IsRefOrOut(argsT[i]))
				{
					if (HasValidDefaultValue(argsT[i]))
						// Parameters with a default value don't count as "missing"
						missingParams--;
					else
						return ~common; // fail
				}
			}

			return common; // success
		}

		enum InOutType
		{
			In, Ref, Out
		}
		static InOutType GetInOutType(ParameterInfo arg)
		{
			if (!arg.ParameterType.IsByRef)
				return InOutType.In;
			else if ((arg.Attributes & ParameterAttributes.Out) == 0)
				return InOutType.Ref;
			else
				return InOutType.Out;
		}
		static bool IsRefOrOut(ParameterInfo arg)
		{
			return arg.ParameterType.IsByRef;
		}
		static bool IsRef(ParameterInfo arg)
		{
			return arg.ParameterType.IsByRef && (arg.Attributes & ParameterAttributes.Out) == 0;
		}
		static bool IsOut(ParameterInfo arg)
		{
			return arg.ParameterType.IsByRef && (arg.Attributes & ParameterAttributes.Out) != 0;
		}
		// Removes the by-ref designation on a type, if present, e.g. "ref int" (int&) to "int"
		static Type NotByRef(Type type)
		{
			return type.IsByRef ? type.GetElementType() : type;
		}

		private static bool ArgumentsAreCompatible(ParameterInfo argT, ParameterInfo argI, out bool refMismatch)
		{
			refMismatch = false;
			Type typeT = argT.ParameterType, typeI = argI.ParameterType;
			bool refT = IsRef(argT);
			bool refI = IsRef(argI);
			bool outT = !refT && IsOut(argT);
			bool outI = !refI && IsOut(argI);
			if (outT)
				return outI && IsConvertable(typeT, typeI); // both out? Covariant.
			if (outI)
				return false; // No way it can work since argT is not an out parameter
			if (refT && refI)
				return NotByRef(typeI) == NotByRef(typeT); // Both ref? Invariant: need exact type match.
			if (refT || refI)
				refMismatch = true;

			// Both parameters are input, and at most one ref param? Contravariant.
			return IsConvertable(typeI, typeT);
		}

		private static bool HasValidDefaultValue(ParameterInfo param)
		{
			object[] dpvs = param.GetCustomAttributes(typeof(DefaultParameterValueAttribute), false);
			if (dpvs.Length != 1)
				return false;
			object value = ((DefaultParameterValueAttribute)dpvs[0]).Value;
			if (value == null)
				return !param.ParameterType.IsValueType;
			else
				return param.ParameterType.IsAssignableFrom(value.GetType()) &&
					(value is string || value.GetType().IsPrimitive);
		}
		
		#endregion

		#region Miscellaneous helper methods & shortcuts

		enum ConvType
		{
			NoImplicitConv = -1,  // Not implicitly convertable
			IncompatibleSign = 0, // Signed-unsigned mismatch
			LargerInt = 1,        // Convert to a larger integer type
			IntToFloat = 2,       // Convert integer to float/double
			Box = 3,              // From value type to reference type
			IsA = 4,              // No conversion required
		}

		private static bool IsConvertable(Type from, Type to)
		{
			return (int)ImplicitConvType(from, to) > 0;
		}

		/// <summary>Figures out what kind of conversion you need to get from 
		/// "from" to "to", returning ConvType.IsA if no conversion is needed.</summary>
		private static ConvType ImplicitConvType(Type from, Type to)
		{
			from = NotByRef(from);
			to = NotByRef(to);

			if (from == to)
				return ConvType.IsA;
			if (to.IsAssignableFrom(from))
			{
				if (from.IsValueType) {
					Debug.Assert(!to.IsValueType);
					return ConvType.Box;
				} else
					return ConvType.IsA;
			}
			bool fromUnsigned, toUnsigned;
			int fromSize = PrimSize(from, out fromUnsigned);
			int toSize = PrimSize(to, out toUnsigned);
			if (fromSize > 0)
			{
				if (toSize > 0)
				{
					if (fromSize == toSize) {
						Debug.Assert(toUnsigned != fromUnsigned);
						return ConvType.IncompatibleSign;
					} else if (fromSize < toSize)
						return toUnsigned && !fromUnsigned ? ConvType.IncompatibleSign : ConvType.LargerInt;
					else
						return ConvType.NoImplicitConv;
				}
				else if (toSize < 0)
					return fromSize < -toSize ? ConvType.IntToFloat : ConvType.NoImplicitConv;
			}
			return 0;
		}

		/// <summary>Returns the size of a primitive integer or float type and 
		/// also tells you if the type is unsigned; note that IntPtr and 
		/// UIntPtr are not handled.</summary>
		/// <returns>The size of type t, or 0 for unhandled types.</returns>
		private static int PrimSize(Type t, out bool unsigned)
		{
			unsigned = false;
			if (t.IsPrimitive)
			{
				if (t == typeof(byte) || t == typeof(ushort))
					unsigned = true;
				if (t == typeof(byte) || t == typeof(sbyte))
					return 1;
				if (t == typeof(short) || t == typeof(ushort))
					return 2;
				if (t == typeof(uint) || t == typeof(ulong))
					unsigned = true;
				if (t == typeof(int) || t == typeof(uint))
					return 4;
				if (t == typeof(long) || t == typeof(ulong))
					return 8;
				if (t == typeof(float))
					return -4;
				if (t == typeof(double))
					return -8;
			}
			return 0;
		}

		private static bool IsUnsigned(Type t)
		{
			return t == typeof(byte) || t == typeof(ushort) || t == typeof(uint) || t == typeof(ulong);
		}

		private static void EmitLdArg(ILGenerator il, int i)
		{
			if (i == 0)
				il.Emit(OpCodes.Ldarg_1);
			else if (i == 1)
				il.Emit(OpCodes.Ldarg_2);
			else if (i == 3)
				il.Emit(OpCodes.Ldarg_3);
			else
				il.Emit(OpCodes.Ldarg, i + 1);
		}

		private static void EmitLdc(ILGenerator il, int value)
		{
			if (value == 0)
				il.Emit(OpCodes.Ldc_I4_0);
			else if (value == 1)
				il.Emit(OpCodes.Ldc_I4_1);
			else if (value == 2)
				il.Emit(OpCodes.Ldc_I4_2);
			else if (value == -1)
				il.Emit(OpCodes.Ldc_I4_M1);
			else
				il.Emit(OpCodes.Ldc_I4, value);
		}

		static E[] Array<E>(params E[] args) { return args; }

		#endregion
	}


	/// <summary>This attribute is applied to a method of an interface to specify 
	/// alternate names that a method can have in T when you use GoInterface
	/// &lt;Interface, T> to produce a wrapper.</summary>
	/// <example>
	/// class MyCollection
	/// {
	///      void Insert(object obj);
	///      int Size { get; }
	///      object GetAt(int i);
	/// }
	/// interface ISimpleList
	/// {
	///     [GoAlias("Insert")] void Add(object item);
	///     
	///     int Count 
	///     {
	///         [GoAlias("get_Size")] get;
	///     }
	///     object this[int index]
	///     {
	///         [GoAlias("GetAt")] get;
	///     }
	/// }
	/// void Example()
	/// {
	///     ISimpleList list = GoInterface&lt;ISimpleList>.From(new MyCollection());
	///     list.Add(10); // calls MyCollection.Insert(10)
	/// }
	/// </example>
	[AttributeUsage(AttributeTargets.Method)]
	public class GoAliasAttribute : Attribute
	{
		public readonly string[] Aliases;
		public GoAliasAttribute(params string[] aliases) { Aliases = aliases; }
	}
}
