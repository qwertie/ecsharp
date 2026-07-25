using Loyc.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Loyc.SyncLib;

/// <summary>Gets synchronizer functions for types with built-in support, such as int, 
///   enums, tuples, or arrays of primitives -- work in progress.</summary>
static class PredefinedSynchronizer<SyncManager> where SyncManager: ISyncManager
{
	public static SyncFieldFunc_Ref<SyncManager, T>? Get<T>()
	{
		if (_builtin.TryGetValue(typeof(T), out Delegate? syncMethod))
			return (SyncFieldFunc_Ref<SyncManager, T>) syncMethod;

		// Types with special handling (byte[]/bool[]/char[], DateTime, TimeSpan...)
		_staticSpecial = _staticSpecial ?? GetStaticSpecialSynchronizers();
		if (_staticSpecial.TryGetValue(typeof(T), out syncMethod))
			return (SyncFieldFunc_Ref<SyncManager, T>) syncMethod;

		if (typeof(T).IsEnum)
			return MakeClosedDelegate<T>(_syncEnumMethod!, typeof(T));

		if (typeof(T).IsArray && typeof(T).GetArrayRank() == 1
			&& typeof(T) == typeof(T).GetElementType()!.MakeArrayType()) // exclude e.g. int[*]
			return MakeClosedDelegate<T>(_syncArrayMethod!, typeof(T).GetElementType()!);

		if (typeof(T).IsGenericType) {
			_genericMethods = _genericMethods ?? GetGenericSynchronizerMethods();

			// Find a generic synchronizer such as TupleSynchronizer.Sync whose value
			// parameter has the same generic type definition as T (e.g. the method
			// Sync<I1,I2>(ref SM, FieldId, ValueTuple<I1,I2>) matches T = (int, string)).
			//
			// A nullable value type such as (int, string)? is Nullable<ValueTuple<..>>,
			// so its own type definition is Nullable<> and its single type argument is
			// the whole tuple. To bind it to the nullable synchronizer
			// Sync<I1,I2>(ref SM, FieldId, ValueTuple<I1,I2>?) we must match against the
			// UNDERLYING tuple's definition/arguments, and select the synchronizer whose
			// value parameter is itself Nullable<..> (so nullable T -> nullable method).
			Type? underlying = Nullable.GetUnderlyingType(typeof(T));
			bool wantNullable = underlying != null && underlying.IsGenericType;
			var matchType = wantNullable ? underlying! : typeof(T);
			var matchDef = matchType.GetGenericTypeDefinition();
			var matchArgs = matchType.GetGenericArguments();
			foreach (MethodInfo mi in _genericMethods) {
				var valueParam = mi.GetParameters()[2].ParameterType;
				var valueUnderlying = Nullable.GetUnderlyingType(valueParam);
				// A nullable T binds only to a nullable synchronizer, and vice versa
				if ((valueUnderlying != null) != wantNullable)
					continue;
				var candidate = valueUnderlying ?? valueParam;
				if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == matchDef
					&& mi.GetGenericArguments().Length == matchArgs.Length) {
					MethodInfo closed;
					try {
						closed = mi.MakeGenericMethod(matchArgs);
					} catch (ArgumentException) {
						continue; // a generic constraint was violated
					}
					return (SyncFieldFunc_Ref<SyncManager, T>) Delegate.CreateDelegate(
						typeof(SyncFieldFunc_Ref<SyncManager, T>), closed);
				}
			}
		}

		return null;
	}

	static SyncFieldFunc_Ref<SyncManager, T> MakeClosedDelegate<T>(MethodInfo genericMethod, Type typeArg)
	{
		var closed = genericMethod.MakeGenericMethod(typeArg);
		return (SyncFieldFunc_Ref<SyncManager, T>) Delegate.CreateDelegate(
			typeof(SyncFieldFunc_Ref<SyncManager, T>), closed);
	}

	static Dictionary<Type, Delegate>? _staticSpecial;
	static readonly MethodInfo? _syncEnumMethod = typeof(ExtraSynchronizers<SyncManager>)
		.GetMethod(nameof(ExtraSynchronizers<SyncManager>.SyncEnum), BindingFlags.Public | BindingFlags.Static);
	static readonly MethodInfo? _syncArrayMethod = typeof(ExtraSynchronizers<SyncManager>)
		.GetMethod(nameof(ExtraSynchronizers<SyncManager>.SyncArray), BindingFlags.Public | BindingFlags.Static);

	// Builds a table from the NON-generic static methods of ExtraSynchronizers,
	// which have the form `T Sync(ref SyncManager, FieldId, T)`.
	static Dictionary<Type, Delegate> GetStaticSpecialSynchronizers()
	{
		var dict = new Dictionary<Type, Delegate>();
		foreach (var mi in typeof(ExtraSynchronizers<SyncManager>).GetMethods(BindingFlags.Public | BindingFlags.Static)) {
			if (mi.IsGenericMethodDefinition)
				continue;
			var p = mi.GetParameters();
			if (p.Length == 3 && p[0].ParameterType.IsByRef
				&& p[0].ParameterType.GetElementType() == typeof(SyncManager)
				&& p[1].ParameterType == typeof(FieldId)
				&& p[2].ParameterType == mi.ReturnType) {
				dict[mi.ReturnType] = Delegate.CreateDelegate(
					typeof(SyncFieldFunc_Ref<,>).MakeGenericType(typeof(SyncManager), mi.ReturnType), mi);
			}
		}
		return dict;
	}

	static Dictionary<Type, Delegate> _builtin = GetBuiltInSynchronizers();
	private static Dictionary<Type, Delegate> GetBuiltInSynchronizers()
	{
		// Build a table of synchronizers implemented by SyncManager. Note that
		// SyncNullable takes priority over Sync, e.g. if we're asked to synchronize
		// a string, we will synchronize it using the nullable-string method. (In
		// contrast, int and int? are distinct types with different synchronizers.)
		var dict = new Dictionary<Type, Delegate>();
		dict.SetRange(GetSynchronizers(nameof(ISyncManager.Sync)));
		//dict.SetRange(GetSynchronizers(nameof(ISyncManager.SyncNullable)));
		// TODO: support SyncList extension methods
		//dict.SetRange(GetSynchronizers(nameof(ISyncManager.SyncList)));
		return dict;

		static IEnumerable<KeyValuePair<Type, Delegate>> GetSynchronizers(string name)
		{
			var mode = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

			foreach (MethodInfo mi in typeof(SyncManager).GetMethods(mode)) {
				if (mi.Name != name || mi.IsGenericMethodDefinition)
					continue;
				var p = mi.GetParameters();
				if (p.Length < 2 || p.Length > 3
					|| p[0].ParameterType != typeof(FieldId)
					|| p[1].ParameterType != mi.ReturnType)
					continue;
				// Accept both T Sync(FieldId, T) and T Sync(FieldId, T, ObjectMode)
				// (e.g. the string overload has an optional ObjectMode parameter)
				bool hasModeParam = p.Length == 3;
				if (hasModeParam && p[2].ParameterType != typeof(ObjectMode))
					continue;

				var helper = typeof(PredefinedSynchronizer<SyncManager>).GetMethod(
					hasModeParam ? nameof(MakeSyncDelegateWithMode) : nameof(MakeSyncDelegate),
					BindingFlags.NonPublic | BindingFlags.Static)!;
				var dlg = (Delegate) helper.MakeGenericMethod(mi.ReturnType).Invoke(null, new object[] { mi })!;
				yield return new KeyValuePair<Type, Delegate>(mi.ReturnType, dlg);
			}
		}
	}

	static SyncFieldFunc_Ref<SyncManager, T> MakeSyncDelegate<T>(MethodInfo mi)
	{
		if (typeof(SyncManager).IsValueType) {
			// excellent, this delegate type is optimized for SyncManagers that are structs
			return (SyncFieldFunc_Ref<SyncManager, T>) Delegate.CreateDelegate(
				typeof(SyncFieldFunc_Ref<SyncManager, T>), null, mi, true)!;
		} else {
			// SyncManager is a class; bind an open-instance delegate and wrap it
			var sync = (Func<SyncManager, FieldId, T?, T>) Delegate.CreateDelegate(
				typeof(Func<SyncManager, FieldId, T?, T>), null, mi, true)!;
			return (ref SyncManager syncMan, FieldId name, T? savable) => sync(syncMan, name, savable);
		}
	}

	static SyncFieldFunc_Ref<SyncManager, T> MakeSyncDelegateWithMode<T>(MethodInfo mi)
	{
		if (typeof(SyncManager).IsValueType) {
			var sync = (SyncFieldFunc_RefMode<SyncManager, T>) Delegate.CreateDelegate(
				typeof(SyncFieldFunc_RefMode<SyncManager, T>), null, mi, true)!;
			return (ref SyncManager syncMan, FieldId name, T? savable) => sync(ref syncMan, name, savable, ObjectMode.Normal);
		} else {
			var sync = (Func<SyncManager, FieldId, T?, ObjectMode, T>) Delegate.CreateDelegate(
				typeof(Func<SyncManager, FieldId, T?, ObjectMode, T>), null, mi, true)!;
			return (ref SyncManager syncMan, FieldId name, T? savable) => sync(syncMan, name, savable, ObjectMode.Normal);
		}
	}

	static List<MethodInfo>? _genericMethods;
	static List<MethodInfo> GetGenericSynchronizerMethods()
	{
		var genericMethods = new List<MethodInfo>();

		GetGenericSynchronizerMethods(typeof(TupleSynchronizer<SyncManager>), genericMethods);
		GetGenericSynchronizerMethods(typeof(ExtraSynchronizers<SyncManager>), genericMethods);
		return genericMethods;
	}
	static void GetGenericSynchronizerMethods(object classOrObj, List<MethodInfo> genericMethods)
	{
		Type type = (classOrObj as Type) ?? classOrObj.GetType();
		bool isStatic = classOrObj is Type;
		var mode = (isStatic ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.Public | BindingFlags.FlattenHierarchy;
		foreach (var mi in type.GetMethods(mode)) {
			if (mi.IsGenericMethodDefinition) {
				var p = mi.GetParameters();
				if (isStatic) {
					// static synchronizer form: T Sync<...>(ref SyncManager sync, FieldId name, T value)
					if (p.Length == 3 && p[0].ParameterType.IsByRef
						&& p[0].ParameterType.GetElementType() == typeof(SyncManager)
						&& p[1].ParameterType == typeof(FieldId)
						&& p[2].ParameterType == mi.ReturnType)
						genericMethods.Add(mi);
				} else {
					if (p.Length == 2 && p[0].ParameterType == typeof(FieldId) && p[1].ParameterType == mi.ReturnType)
						genericMethods.Add(mi);
				}
			}
		}
	}
}
