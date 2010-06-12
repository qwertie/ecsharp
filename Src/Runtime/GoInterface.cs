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
	public static class GoInterface<Interface> where Interface : class
	{
		public static Interface From<T>(T anything)
		{
			return From(anything, false);
		}
		public static Interface From<T>(T anything, bool allowMismatch) where Interface : class
		{
			if (anything.GetType() == typeof(T))
				return GoInterface<Interface, T>.From(anything, allowMismatch);
			else {
				// TODO: call GoInterface<Interface, T2>.From() where T2 is 
				// anything's most derived type
				throw new NotImplementedException();
			}
		}
		struct TypePair : IEquatable<TypePair>
		{
			public TypePair(RuntimeTypeHandle a, RuntimeTypeHandle b) { A = a; B = b; }
			public readonly RuntimeTypeHandle A;
			public readonly RuntimeTypeHandle B;
			public override int GetHashCode()
			{
				return A.GetHashCode() ^ B.GetHashCode();
			}
			public override bool Equals(object obj)
			{
				return obj is TypePair && this.Equals((TypePair)obj);
			}
			public bool Equals(TypePair other)
			{
				return A.Value == other.A.Value && B.Value == other.B.Value;
			}
		}
	}
	public static class GoInterface<Interface, T> where Interface:class
	{
		delegate Interface WrapperCreator(T obj, bool allowMismatch);

		public static readonly WrapperCreator From;
		public static readonly TypeBuilder WrapperType;

		static GoInterface()
		{
			// We need to generate two things:
			// 1. A class that implements the Interface or, if Interface is an
			//    abstract class, overrides its abstract methods.
			TypeAttributes typeFlags = TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed;
			string typeName = string.Format("{0}_{1:X}_{2:X}", typeof(T).Name, typeof(Interface).TypeHandle.Value, typeof(T).TypeHandle.Value);
			ConstructorInfo c;

			if (typeof(Interface).IsInterface)
			{
				WrapperType = GoInterface.ModuleBuilder.DefineType(typeName, typeFlags);
				WrapperType.AddInterfaceImplementation(typeof(Interface));
			}
			else if (typeof(Interface).IsAbstract && 
				(c = typeof(Interface).GetConstructor(System.Type.EmptyTypes)) != null && 
				(c.IsPublic || c.IsFamily))
			{
				WrapperType = GoInterface.ModuleBuilder.DefineType(typeName, typeFlags, typeof(Interface));
			}
			else
			{	// Generate a dummy wrapper with a method that throws an exception:
				// throw new InvalidOperationException("GoInterface: 'Interface' is not an interface or abstract class");
				typeName = string.Format("{0}_{1:X}", typeof(Interface).Name, typeof(Interface).TypeHandle.Value);
				Type type;
				if ((type = GoInterface.ModuleBuilder.GetType(typeName)) == null) {
					TypeBuilder typeB = GoInterface.ModuleBuilder.DefineType(typeName, typeFlags);
					type = typeB;

					MethodBuilder method = typeB.DefineMethod("Invalid", 
										   MethodAttributes.Static | MethodAttributes.Public,
										   WrapperType, Array(typeof(T), typeof(bool)));
					ILGenerator il = method.GetILGenerator();
					il.Emit(OpCodes.Ldstr, string.Format("GoInterface: '{0}' is not an interface (or abstract class with default constructor)", typeof(Interface).Name);
					ConstructorInfo exception = typeof(InvalidOperationException).GetConstructor(Array(typeof(string)));
					il.Emit(OpCodes.Newobj, exception);
					il.Emit(OpCodes.Throw);
				}
				From = (WrapperCreator)Delegate.CreateDelegate(typeof(WrapperCreator), type.GetMethod("Invalid"));
				return;
			}

			int mismatchCount = GenerateWrapper();
			
			// 2. The From method, which does this:
			//    static Interface From(T obj, bool allowMismatch) { 
			//      if (mismatchCount>0 && !allowMismatch)
			//        throw new InvalidCastException("Cannot cast <T> to <Interface>: <mismatchCount> methods are missing.");
			//      return new GeneratedClass(obj);
			//    }
			MethodInfo from = GenerateFrom(mismatchCount);
			From = (WrapperCreator)Delegate.CreateDelegate(typeof(WrapperCreator), from);
		}

		static FieldBuilder _obj;

		private static int GenerateWrapper()
		{
			// In the WrapperType, create a constructor and a pointer to the wrapped object:
			//     private readonly T _obj;
			//     WrapperType(T obj) : base() { this._obj = obj; }
			_obj = WrapperType.DefineField("_obj", typeof(T), FieldAttributes.Private | FieldAttributes.InitOnly);
			ConstructorBuilder constructor = WrapperType.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Array(typeof(T)));
			constructor.DefineParameter(1, ParameterAttributes.In, "obj");
			ILGenerator il = constructor.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Call, WrapperType.BaseType.GetConstructor(System.Type.EmptyTypes));
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Stfld, _obj);
			il.Emit(OpCodes.Ret);

			// Reflect over Interface to find out what methods need to be
			// implemented in the wrapper class, and generate them.
			int mismatchCount = 0;
			MethodInfo[] methodsOfT = typeof(T).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
			foreach (MethodInfo methodOfI in typeof(Interface).GetMethods(BindingFlags.Public | BindingFlags.Instance))
			{
				if (methodOfI.IsAbstract) {
					if (!GenerateWrapper(methodOfI, methodsOfT))
						mismatchCount++;
				} else
					Debug.Assert(!typeof(Interface).IsInterface);
			}
			return mismatchCount;
		}

		/// <summary>Generates a method that implements baseMethod (a method of
		/// Interface) and forwards the call to the same method on T.</summary>
		/// <remarks>"Safe" parameter variance is allowed between Interface and T,
		/// such as return type covariance. Support for default parameters is not
		/// implemented.</remarks>
		private static bool GenerateWrapper(MethodInfo baseMethod, MethodInfo[] methodsOfT)
		{
			MethodAttributes flags = MethodAttributes.Public | MethodAttributes.HideBySig;
			if (!typeof(Interface).IsInterface)
				flags |= MethodAttributes.Virtual | MethodAttributes.ReuseSlot;

			MethodBuilder method = WrapperType.DefineMethod(baseMethod.Name, flags);

			// Copy the parameter information and return type from the base method
			var bmps = baseMethod.GetParameters();
			var paramTypes = new Type[bmps.Length];
			for (int i = 0; i < bmps.Length; i++)
				paramTypes[i] = bmps[i].ParameterType;
			method.SetParameters(paramTypes);
			method.SetReturnType(baseMethod.ReturnType);
			foreach (ParameterInfo param in baseMethod.GetParameters())
				method.DefineParameter(param.Position + 1, param.Attributes, param.Name);

			method.SetImplementationFlags(MethodImplAttributes.Managed | MethodImplAttributes.IL);

			// Before generating code for the method, we must find a matching 
			// method of T. Note that we want to support various kinds of
			// "variance", including boxing if necessary:
			// - Return type covariance. If T's method returns a derived class,
			//   Interface's method can return a base class or interface. Also, 
			//   allow any return type if baseMethod returns void.
			// - "out" parameter covariance.
			// - T's "out" argument(s) can be absent from Interface, as long as
			//   they are at the end of the argument list.
			// - Interface's input arguments can be absent from T, as long as the
			//   missing parameters come at the end of the argument list.
			// - Input parameter contravariance. If T's method accepts a base class
			//   (or interface), Interface's method can accept a derived class (or
			//   a class that implements said interface).
			// - If T takes a "ref" argument, allow the method in Interface not to
			//   be "ref" and vice versa.
			// - T's method can be static
			// 
			// Implicit conversion operators are not currently detected, not even
			// simple ones like Int16 to Int32.
			// 
			// Variable argument lists are not supported specifically (they are 
			// treated as arrays).
			// 
			// Generic methods in T and Interface are not supported.
			// 
			// Default arguments are generally supported.
			// 
			// Methods that differ only in case are considered only if there are no
			// matching methods that do not differ in case.
			//
			// Scan all methods of "T" with a matching name and find the best
			// match, since there may be multiple methods matching these
			// requirements. For the most part I try to follow the rules of 
			// the C# standard (ECMA-334 §14.4.2.1 and §14.4.2.2), but be a little 
			// more liberal. The way I figure it, "void" should be treated like a
			// base class of "object". Note that this matching algorithm may reject
			// all overloads due to "ambiguity", in case one overload is not better
			// than all others.
			List<MethodInfo> matchesInT = GetMatchingMethods(baseMethod, methodsOfT, false);
			if (matchesInT == null)
				matchesInT = GetMatchingMethods(baseMethod, methodsOfT, true);
			MethodInfo bestMatchInT = null;
			if (matchesInT != null)
				bestMatchInT = ChooseBestMatch(baseMethod, matchesInT);
			
			// Generate the code inside the method, which calls the matching method
			// on _obj. If no matching method was selected, we throw an exception
			// instead.
			if (bestMatchInT != null)
			{
				GenerateForwardingCode(method, bestMatchInT);
			}


			// To call the method, we must copy all the arguments...
			ILGenerator il = method.GetILGenerator();
		}

		private static void GenerateForwardingCode(MethodBuilder method, MethodInfo bestMatchInT)
		{
			ILGenerator il = method.GetILGenerator();
			TODO;
		}

		private static MethodInfo ChooseBestMatch(MethodInfo @interface, List<MethodInfo> matchesInT)
		{
			// The best match is a match that is "better" than all others
			for (int i = 0; i < matchesInT.Count; i++) {
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
			int methodCommon = GetMatchingParameterCount(callerArgs, methodArgs);
			int otherCommon = GetMatchingParameterCount(callerArgs, otherArgs);

			if (methodCommon != otherCommon)
				// I don't think ECMA-334 (C# standard) has anything to say about
				// this case, as it predates optional parameters. It seems to me
				// that maybe the extra parameter(s) trump all other concerns.
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
			// between input, "ref" and "out" parameters. Specifically, at the very
			// top of that list, I would say that if the in-out status of one
			// parameter matches but not the other, the matching parameter is
			// better. C#, in contrast, rejects non-matching parameters entirely.
			// 
			// Note: the implicit conversions in the above list are not supported.
			bool leftMatchesInOut = (left.IsIn == caller.IsIn && left.IsOut == caller.IsOut);
			bool rightMatchesInOut = (right.IsIn == caller.IsIn && right.IsOut == caller.IsOut);
			if (leftMatchesInOut != rightMatchesInOut)
				return leftMatchesInOut ? 1 : -1;

			TODO;
		}

		private static List<MethodInfo> GetMatchingMethods(MethodInfo @interface, MethodInfo[] methodsOfT, bool ignoreCase)
		{
			List<MethodInfo> matches = null;
			for (int i = 0; i < methodsOfT.Length; i++)
			{
				MethodInfo methodOfT = methodsOfT[i];
				if (string.Compare(@interface.Name, methodOfT.Name, ignoreCase) != 0)
					continue;
				
				// Check return type
				if (@interface.ReturnType != typeof(void) && !@interface.ReturnType.IsAssignableFrom(methodOfT.ReturnType))
					continue;
				
				// Check arguments
				if (GetMatchingParameterCount(@interface, methodOfT) < 0)
					continue;

				if (matches == null)
					matches = new List<MethodInfo>();
				matches.Add(methodOfT);
			}
			return matches;
		}

		public static int GetMatchingParameterCount(ParameterInfo[] argsI, ParameterInfo[] argsT)
		{
			int min = Math.Min(argsI.Length, argsT.Length);
			int common;
			for (common = 0; common < min; min++)
				if (!ArgumentsAreCompatible(argsT[common], argsI[common]))
					break;

			// Now after the common parameters, there may be parameters that are 
			// missing in one method or the other.

			// If Interface has arguments that T lacks, they must be input-only.
			for (int i = common; i < argsI.Length; i++)
				if (argsI[i].IsOut)
					return ~common; // fail
			// If T has arguments that Interface lacks, they must be output-only,
			// or they must have a default value.
			for (int i = common; i < argsT.Length; i++)
				if (argsT[i].IsIn && !HasDefaultValue(argsT[i]))
					return ~common; // fail

			return common; // success
		}

		private bool ArgumentsAreCompatible(ParameterInfo argT, ParameterInfo argI)
		{
			Type typeT = argT.ParameterType, typeI = argI.ParameterType;
			bool refT = argT.IsIn && argT.IsOut;
			bool refI = argI.IsIn && argI.IsOut;
			bool outT = !refT && argT.IsOut;
			bool outI = !refI && argI.IsOut;
			if (outT)
				return outI && typeI.IsAssignableFrom(typeT); // both out? Covariant.
			if (outI)
				return false; // No way it can work since argT is not an out parameter
			if (refT && refI)
				return typeI == typeT; // Both ref? Invariant: need exact type match.

			// Both parameters are input, and at most one ref param? Contravariant.
			return typeT.IsAssignableFrom(typeI);
		}
		private bool HasDefaultValue(ParameterInfo param)
		{
			return param.GetCustomAttributes(typeof(DefaultParameterValueAttribute), false).Length == 1;
		}

		private static MethodInfo GenerateFrom(int mismatchCount)
		{
			MethodBuilder method = WrapperType.DefineMethod("From",
								   MethodAttributes.Static | MethodAttributes.Public,
								   WrapperType, Array(typeof(T), typeof(bool)));
			ILGenerator il = method.GetILGenerator();
			ConstructorInfo exception = typeof(InvalidOperationException).GetConstructor(Array(typeof(string)));
			il.Emit(OpCodes.Newobj, exception);
			il.Emit(OpCodes.Throw);
			TODO;

			return WrapperType.GetMethod("From");
		}

		static T[] Array<T>(params T[] args) { return args; }
	}

	interface IEnumerableCount<T> : IEnumerable<T>
	{
		//IEnumerator<T> GetEnumerator();
		//IEnumerator IEnumerable.GetEnumerator();
		int Count { get; }
	}

	// Caller can provide this manually, or it will be generated from the interface
	abstract class GoEnumerableCount<T> : IEnumerableCount<T>
	{
		public abstract int Count { get; }
		public abstract IEnumerator<T> GetEnumerator();
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw new NotImplementedException(); }
	}
	class GoEnumerableCount_List<T> : GoEnumerableCount<T>
	{
		List<T> obj;
		public GoEnumerableCount_List(List<T> obj) { }
		public override int Count { get { return obj.Count; } }
		public override IEnumerator<T> GetEnumerator() { return obj.GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return ((System.Collections.IEnumerable)obj).GetEnumerator(); }
	}
}
