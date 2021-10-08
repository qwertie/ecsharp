using Loyc;
using Loyc.Collections;
using Loyc.Collections.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace Loyc.SyncLib
{
	// If SyncManager is a struct, this is the type of most of its Sync methods
	public delegate T SyncFieldFunc_Ref<SyncManager, T>(ref SyncManager sync, FieldId name, [AllowNull] T value);

	public static class DefaultSynchronizer
	{
		public static bool Supports<T>()
		{
			if (DefaultSynchronizer<SyncJson.Writer, T>.Default != DefaultSynchronizer<SyncJson.Writer, T>._FallbackSync)
				return true;
			return DefaultSynchronizer<SyncJson.Writer, T>.FindSynchronizer() != null;
		}
		
		public static T Sync<SyncManager, T>(ref SyncManager sync, FieldId name, [AllowNull] T value) where SyncManager: ISyncManager
		{
			return DefaultSynchronizer<SyncManager, T>.Default(ref sync, name, value);
		}

		static Dictionary<Type, Func<Type, Delegate>> RegisteredFieldSyncs = new Dictionary<Type, Func<Type, Delegate>>();
		public static bool RegisterStaticSynchronizers(Type type, bool replaceExisting = false)
		{
			if (type.IsGenericTypeDefinition) {
				var gArgs = type.GetGenericArguments();
				if (gArgs.Length == 1 && gArgs[0].GetGenericParameterConstraints().Any(c => c == typeof(ISyncManager))) {
					
				}
			}
			return false;
		}
	}

	public static class DefaultSynchronizer<SyncManager, T> where SyncManager: ISyncManager
	{
		internal static SyncFieldFunc_Ref<SyncManager, T> _FallbackSync = FallbackSync;
		internal static SyncFieldFunc_Ref<SyncManager, T> Default = FindSynchronizer() ?? _FallbackSync;

		internal static SyncFieldFunc_Ref<SyncManager, T>? FindSynchronizer()
		{
			var sync = PredefinedSynchronizer<SyncManager>.Get<T>();
			if (sync == null) {
				// TODO: check if a synchronizer is registered
				return null;
			}
			return sync;
		}

		static T FallbackSync(ref SyncManager sync, FieldId name, T? value)
		{
			var syncMethod = FindSynchronizer();
			if (syncMethod != null) {
				Default = syncMethod;
				return syncMethod(ref sync, name, value);
			}
			throw new NotSupportedException("There is no default synchronizer for " + typeof(T).NameWithGenericArgs());
		}
	}

	static class PredefinedSynchronizer<SyncManager> where SyncManager: ISyncManager
	{
		public static SyncFieldFunc_Ref<SyncManager, T>? Get<T>()
		{
			if (_builtin.TryGetValue(typeof(T), out Delegate? syncMethod))
				return (SyncFieldFunc_Ref<SyncManager, T>) syncMethod;

			if (typeof(T).IsGenericType) {
				_genericMethods = _genericMethods ?? GetGenericSynchronizerMethods();

			}

			return null;
		}

		static Dictionary<Type, Delegate> _builtin = GetBuiltInSynchronizers();
		private static Dictionary<Type, Delegate>  GetBuiltInSynchronizers()
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

				return (from mi in typeof(SyncManager).GetMethods(mode)
						where mi.Name == name && !mi.IsGenericMethodDefinition
						let p = mi.GetParameters()
						where p.Length == 2 && p[0].ParameterType.IsAssignableFrom(typeof(Symbol))
						                    && p[1].ParameterType == mi.ReturnType
						select new KeyValuePair<Type, Delegate>
							(p[1].ParameterType, MakeOpenDelegateForSync(mi, p[1].ParameterType)));

				static Delegate MakeOpenDelegateForSync(MethodInfo mi, Type dataType)
				{
					if (typeof(SyncManager).IsValueType)
					{
						// excellent, this method is optimized for SyncManagers that are structs
						Type delegateType = typeof(SyncFieldFunc_Ref<,>).MakeGenericType(typeof(SyncManager), dataType);
						return Delegate.CreateDelegate(delegateType, null, mi, true)!;
					}
					else
					{
						// SyncManager is a class; we'll need to create a wrapper delegate.
						var helper = typeof(PredefinedSynchronizer<SyncManager>).GetMethod(nameof(HelpMakeOpenDelegate))!;
						return (Delegate) helper.MakeGenericMethod(dataType).Invoke(null, new object[] { mi })!;
					}
				}
				static SyncFieldFunc_Ref<SyncManager, T> HelpMakeOpenDelegate<T>(MethodInfo mi)
				{
					var sync = (SyncFieldFunc_Ref<SyncManager, T>)Delegate.CreateDelegate(typeof(SyncFieldFunc_Ref<SyncManager, T>), null, mi);
					return (ref SyncManager syncMan, FieldId name, T savable) => sync(ref syncMan, name, savable);
				}
			}
		}

		static List<MethodInfo>? _genericMethods;
		static List<MethodInfo> GetGenericSynchronizerMethods()
		{
			var genericMethods = new List<MethodInfo>();
			
			// TODO: allow user-defined generic synchronizers
			GetGenericSynchronizerMethods(typeof(TupleSynchronizer<SyncManager>), genericMethods);
			return genericMethods;
		}
		static void GetGenericSynchronizerMethods(object classOrObj, List<MethodInfo> genericMethods)
		{
			Type type = (classOrObj as Type) ?? classOrObj.GetType();
			var mode = (classOrObj is Type ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.Public | BindingFlags.FlattenHierarchy;
			foreach (var mi in type.GetMethods(mode)) {
				if (mi.IsGenericMethodDefinition) {
					var p = mi.GetParameters();
					if (p.Length == 2 && p[0].ParameterType.IsAssignableFrom(typeof(Symbol)) && p[1].ParameterType == mi.ReturnType)
						genericMethods.Add(mi);
				}
			}
		}
	}

	static class TupleSynchronizer<SyncManager> where SyncManager : ISyncManager
	{
		// System.ValueTuple synchronizers

		public static ValueTuple<Item1> Sync<Item1>(ref SyncManager sync, FieldId name, ValueTuple<Item1> value)
		{
			sync.BeginSubObject(name, null, ObjectMode.NotNull | ObjectMode.Tuple, 8);
			var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
			sync.EndSubObject();
			return new ValueTuple<Item1>(item1);
		}

		public static ValueTuple<Item1, Item2> Sync<Item1, Item2>(ref SyncManager sync, FieldId name, ValueTuple<Item1, Item2> value)
		{
			sync.BeginSubObject(name, null, ObjectMode.NotNull | ObjectMode.Tuple, 8);
			var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
			var item2 = DefaultSynchronizer.Sync(ref sync, null, value.Item2);
			sync.EndSubObject();
			return new ValueTuple<Item1, Item2>(item1, item2);
		}

		public static ValueTuple<Item1, Item2, Item3> Sync<Item1, Item2, Item3>(ref SyncManager sync, FieldId name, ValueTuple<Item1, Item2, Item3> value)
		{
			sync.BeginSubObject(name, null, ObjectMode.NotNull | ObjectMode.Tuple, 8);
			var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
			var item2 = DefaultSynchronizer.Sync(ref sync, null, value.Item2);
			var item3 = DefaultSynchronizer.Sync(ref sync, null, value.Item3);
			sync.EndSubObject();
			return new ValueTuple<Item1, Item2, Item3>(item1, item2, item3);
		}

		public static ValueTuple<Item1, Item2, Item3, Item4> Sync<Item1, Item2, Item3, Item4>(ref SyncManager sync, FieldId name, ValueTuple<Item1, Item2, Item3, Item4> value)
		{
			sync.BeginSubObject(name, null, ObjectMode.NotNull | ObjectMode.Tuple, 8);
			var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
			var item2 = DefaultSynchronizer.Sync(ref sync, null, value.Item2);
			var item3 = DefaultSynchronizer.Sync(ref sync, null, value.Item3);
			var item4 = DefaultSynchronizer.Sync(ref sync, null, value.Item4);
			sync.EndSubObject();
			return new ValueTuple<Item1, Item2, Item3, Item4>(item1, item2, item3, item4);
		}

		public static ValueTuple<Item1, Item2, Item3, Item4, Item5> Sync<Item1, Item2, Item3, Item4, Item5>(ref SyncManager sync, FieldId name, ValueTuple<Item1, Item2, Item3, Item4, Item5> value)
		{
			sync.BeginSubObject(name, null, ObjectMode.NotNull | ObjectMode.Tuple, 8);
			var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
			var item2 = DefaultSynchronizer.Sync(ref sync, null, value.Item2);
			var item3 = DefaultSynchronizer.Sync(ref sync, null, value.Item3);
			var item4 = DefaultSynchronizer.Sync(ref sync, null, value.Item4);
			var item5 = DefaultSynchronizer.Sync(ref sync, null, value.Item5);
			sync.EndSubObject();
			return new ValueTuple<Item1, Item2, Item3, Item4, Item5>(item1, item2, item3, item4, item5);
		}

		public static ValueTuple<Item1, Item2, Item3, Item4, Item5, Item6> Sync<Item1, Item2, Item3, Item4, Item5, Item6>(ref SyncManager sync, FieldId name, ValueTuple<Item1, Item2, Item3, Item4, Item5, Item6> value)
		{
			sync.BeginSubObject(name, null, ObjectMode.NotNull | ObjectMode.Tuple, 8);
			var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
			var item2 = DefaultSynchronizer.Sync(ref sync, null, value.Item2);
			var item3 = DefaultSynchronizer.Sync(ref sync, null, value.Item3);
			var item4 = DefaultSynchronizer.Sync(ref sync, null, value.Item4);
			var item5 = DefaultSynchronizer.Sync(ref sync, null, value.Item5);
			var item6 = DefaultSynchronizer.Sync(ref sync, null, value.Item6);
			sync.EndSubObject();
			return new ValueTuple<Item1, Item2, Item3, Item4, Item5, Item6>(item1, item2, item3, item4, item5, item6);
		}

		public static ValueTuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7> Sync<Item1, Item2, Item3, Item4, Item5, Item6, Item7>(ref SyncManager sync, FieldId name, ValueTuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7> value)
		{
			sync.BeginSubObject(name, null, ObjectMode.NotNull | ObjectMode.Tuple, 8);
			var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
			var item2 = DefaultSynchronizer.Sync(ref sync, null, value.Item2);
			var item3 = DefaultSynchronizer.Sync(ref sync, null, value.Item3);
			var item4 = DefaultSynchronizer.Sync(ref sync, null, value.Item4);
			var item5 = DefaultSynchronizer.Sync(ref sync, null, value.Item5);
			var item6 = DefaultSynchronizer.Sync(ref sync, null, value.Item6);
			var item7 = DefaultSynchronizer.Sync(ref sync, null, value.Item7);
			sync.EndSubObject();
			return new ValueTuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7>(item1, item2, item3, item4, item5, item6, item7);
		}

		public static ValueTuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7, Rest> Sync<Item1, Item2, Item3, Item4, Item5, Item6, Item7, Rest>(ref SyncManager sync, FieldId name, ValueTuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7, Rest> value)
			where Rest: struct
		{
			sync.BeginSubObject(name, null, ObjectMode.NotNull | ObjectMode.Tuple, 8);
			var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
			var item2 = DefaultSynchronizer.Sync(ref sync, null, value.Item2);
			var item3 = DefaultSynchronizer.Sync(ref sync, null, value.Item3);
			var item4 = DefaultSynchronizer.Sync(ref sync, null, value.Item4);
			var item5 = DefaultSynchronizer.Sync(ref sync, null, value.Item5);
			var item6 = DefaultSynchronizer.Sync(ref sync, null, value.Item6);
			var item7 = DefaultSynchronizer.Sync(ref sync, null, value.Item7);
			var rest  = DefaultSynchronizer.Sync(ref sync, null, value.Rest);
			sync.EndSubObject();
			return new ValueTuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7, Rest>(item1, item2, item3, item4, item5, item6, item7, rest);
		}

		// System.Tuple synchronizers

		public static Tuple<Item1> Sync<Item1>(ref SyncManager sync, FieldId name, Tuple<Item1> value)
		{
			sync.BeginSubObject(name, value, ObjectMode.Tuple, 8);
			var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
			sync.EndSubObject();
			return new Tuple<Item1>(item1);
		}

		public static Tuple<Item1, Item2> Sync<Item1, Item2>(ref SyncManager sync, FieldId name, Tuple<Item1, Item2> value)
		{
			sync.BeginSubObject(name, value, ObjectMode.Tuple, 8);
			var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
			var item2 = DefaultSynchronizer.Sync(ref sync, null, value.Item2);
			sync.EndSubObject();
			return new Tuple<Item1, Item2>(item1, item2);
		}

		public static Tuple<Item1, Item2, Item3> Sync<Item1, Item2, Item3>(ref SyncManager sync, FieldId name, Tuple<Item1, Item2, Item3> value)
		{
			sync.BeginSubObject(name, value, ObjectMode.Tuple, 8);
			var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
			var item2 = DefaultSynchronizer.Sync(ref sync, null, value.Item2);
			var item3 = DefaultSynchronizer.Sync(ref sync, null, value.Item3);
			sync.EndSubObject();
			return new Tuple<Item1, Item2, Item3>(item1, item2, item3);
		}

		public static Tuple<Item1, Item2, Item3, Item4> Sync<Item1, Item2, Item3, Item4>(ref SyncManager sync, FieldId name, Tuple<Item1, Item2, Item3, Item4> value)
		{
			sync.BeginSubObject(name, value, ObjectMode.Tuple, 8);
			var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
			var item2 = DefaultSynchronizer.Sync(ref sync, null, value.Item2);
			var item3 = DefaultSynchronizer.Sync(ref sync, null, value.Item3);
			var item4 = DefaultSynchronizer.Sync(ref sync, null, value.Item4);
			sync.EndSubObject();
			return new Tuple<Item1, Item2, Item3, Item4>(item1, item2, item3, item4);
		}

		public static Tuple<Item1, Item2, Item3, Item4, Item5> Sync<Item1, Item2, Item3, Item4, Item5>(ref SyncManager sync, FieldId name, Tuple<Item1, Item2, Item3, Item4, Item5> value)
		{
			sync.BeginSubObject(name, value, ObjectMode.Tuple, 8);
			var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
			var item2 = DefaultSynchronizer.Sync(ref sync, null, value.Item2);
			var item3 = DefaultSynchronizer.Sync(ref sync, null, value.Item3);
			var item4 = DefaultSynchronizer.Sync(ref sync, null, value.Item4);
			var item5 = DefaultSynchronizer.Sync(ref sync, null, value.Item5);
			sync.EndSubObject();
			return new Tuple<Item1, Item2, Item3, Item4, Item5>(item1, item2, item3, item4, item5);
		}

		public static Tuple<Item1, Item2, Item3, Item4, Item5, Item6> Sync<Item1, Item2, Item3, Item4, Item5, Item6>(ref SyncManager sync, FieldId name, Tuple<Item1, Item2, Item3, Item4, Item5, Item6> value)
		{
			sync.BeginSubObject(name, value, ObjectMode.Tuple, 8);
			var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
			var item2 = DefaultSynchronizer.Sync(ref sync, null, value.Item2);
			var item3 = DefaultSynchronizer.Sync(ref sync, null, value.Item3);
			var item4 = DefaultSynchronizer.Sync(ref sync, null, value.Item4);
			var item5 = DefaultSynchronizer.Sync(ref sync, null, value.Item5);
			var item6 = DefaultSynchronizer.Sync(ref sync, null, value.Item6);
			sync.EndSubObject();
			return new Tuple<Item1, Item2, Item3, Item4, Item5, Item6>(item1, item2, item3, item4, item5, item6);
		}

		public static Tuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7> Sync<Item1, Item2, Item3, Item4, Item5, Item6, Item7>(ref SyncManager sync, FieldId name, Tuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7> value)
		{
			sync.BeginSubObject(name, value, ObjectMode.Tuple, 8);
			var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
			var item2 = DefaultSynchronizer.Sync(ref sync, null, value.Item2);
			var item3 = DefaultSynchronizer.Sync(ref sync, null, value.Item3);
			var item4 = DefaultSynchronizer.Sync(ref sync, null, value.Item4);
			var item5 = DefaultSynchronizer.Sync(ref sync, null, value.Item5);
			var item6 = DefaultSynchronizer.Sync(ref sync, null, value.Item6);
			var item7 = DefaultSynchronizer.Sync(ref sync, null, value.Item7);
			sync.EndSubObject();
			return new Tuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7>(item1, item2, item3, item4, item5, item6, item7);
		}

		public static Tuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7, Rest> Sync<Item1, Item2, Item3, Item4, Item5, Item6, Item7, Rest>(ref SyncManager sync, FieldId name, Tuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7, Rest> value)
			where Rest: struct
		{
			sync.BeginSubObject(name, value, ObjectMode.Tuple, 8);
			var item1 = DefaultSynchronizer.Sync(ref sync, null, value.Item1);
			var item2 = DefaultSynchronizer.Sync(ref sync, null, value.Item2);
			var item3 = DefaultSynchronizer.Sync(ref sync, null, value.Item3);
			var item4 = DefaultSynchronizer.Sync(ref sync, null, value.Item4);
			var item5 = DefaultSynchronizer.Sync(ref sync, null, value.Item5);
			var item6 = DefaultSynchronizer.Sync(ref sync, null, value.Item6);
			var item7 = DefaultSynchronizer.Sync(ref sync, null, value.Item7);
			var rest  = DefaultSynchronizer.Sync(ref sync, null, value.Rest);
			sync.EndSubObject();
			return new Tuple<Item1, Item2, Item3, Item4, Item5, Item6, Item7, Rest>(item1, item2, item3, item4, item5, item6, item7, rest);
		}
	}
}
