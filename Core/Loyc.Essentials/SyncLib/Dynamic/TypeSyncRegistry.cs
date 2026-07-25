using Loyc.SyncLib.Impl;
using Loyc.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Loyc.SyncLib;

/// <summary>A registry that maps data types to synchronizers, to support dynamic
///   typing (polymorphic serialization): writing an object whose runtime type is
///   not known statically, and reading it back via the type tag stored in the
///   data stream.</summary>
/// <remarks>
///   This registry knows which synchronizer handles which type, and nothing
///   about tags: the tag &lt;-> type mapping, the <see cref="TypeTagAttribute"/>
///   convention, and the policy for unknown/mismatched tags all belong to
///   <see cref="TypeTagRegistry"/>. The <c>Add</c> methods do, however, record
///   the tags they discover in <see cref="TypeTagRegistry.Default"/> (the
///   instance that is ambient at the time of the call), so that one registration
///   call is enough in the typical case.
///   <para/>
///   Synchronizers are registered by passing an open generic class with a single
///   type parameter constrained to <see cref="ISyncManager"/>, such as
///   <c>MySynchronizers&lt;SM></c>. <see cref="Add(Type, bool)"/> discovers all
///   synchronizers in the class:
///   <ul>
///   <li>public static methods of the form <c>T Sync(SM sync, T? value)</c>
///     (the <see cref="SyncObjectFunc{SM,T}"/> shape, i.e. an object "body" that
///     reads/writes fields but does not call BeginSubObject itself), and</li>
///   <li>implementations of <see cref="ISyncObject{SM, T}"/>.</li>
///   </ul>
///   Reflection runs only during registration and once per (registry, SyncManager
///   type) pair to build dispatch tables of delegates; there is no reflection and
///   no runtime code generation on the per-object path, which is one dictionary
///   lookup plus a delegate call.
///   <para/>
///   <b>Ambient Service Pattern</b>: the registry used by <see cref="DynamicSync{SM, T}"/>
///   is <see cref="Default"/>, an async-local value that can be swapped temporarily:
///   <code>
///   using (SyncTypeRegistry.SetDefault(myRegistry))
///       SyncJson.Write(drawing, Drawing.Sync&lt;SyncJson.Writer>);
///   </code>
///   This also means multiple synchronizers can exist for one type: register each
///   in a different registry and swap (usually together with a paired
///   <see cref="TypeTagRegistry"/>). Alternatively, pass a specific registry to
///   the <see cref="SyncDynamicExt.SyncDynamic{SM, T}(SM, FieldId, T, TypeSyncRegistry, TypeTagRegistry?, ObjectMode)"/>
///   overloads to bypass the ambient service entirely. Registration is expected
///   to happen at startup, but late registration is supported and thread-safe.
/// </remarks>
public class TypeSyncRegistry
{
	#region Ambient Service Pattern (async-local Default)

	static readonly AmbientService<TypeSyncRegistry> _ambient =
		new AmbientService<TypeSyncRegistry>(new TypeSyncRegistry());

	/// <summary>Gets or sets the registry used by every execution context that has
	///   no ambient override from <see cref="SetDefault"/>. It can be replaced app-wide.</summary>
	public static TypeSyncRegistry GlobalDefault {
		get => _ambient.GlobalDefault;
		set => _ambient.GlobalDefault = value;
	}

	/// <summary>The ambient registry used by <see cref="DynamicSync{SM, T}"/> and
	///   <see cref="DefaultSynchronizer"/>: the current execution context's override
	///   (see <see cref="SetDefault"/>) if one is active, else <see cref="GlobalDefault"/>.
	///   See <see cref="AmbientService{T}"/> for how overrides flow across await
	///   and why the no-override case costs only a static field read.</summary>
	public static TypeSyncRegistry Default => _ambient.Value;

	/// <summary>Sets the ambient (async-local) default registry. Designed to be used
	///   in a <c>using</c> statement, which restores the old value at the end.</summary>
	public static AmbientService<TypeSyncRegistry>.Saved SetDefault(TypeSyncRegistry newValue)
		=> _ambient.Set(newValue);

	#endregion

	internal sealed class Registration
	{
		public Type ValueType;
		// Exactly one of the following three "shapes" is used:
		public Type? OpenClass;        // open generic class in which the synchronizer lives
		public MethodInfo? BodyMethod; //   ...a static method T Sync(SM, T) of OpenClass, or
		public bool ViaISyncObject;    //   ...OpenClass implements ISyncObject<SM, ValueType>
		public Delegate? SimpleBody;   // a SyncObjectFunc<ISyncManager, T> ("easy mode")

		public Registration(Type valueType) => ValueType = valueType;
	}

	// The registrations are stored in immutable snapshots that are swapped
	// atomically (copy-on-write), so lookups require no locks. Dispatch tables
	// (per SyncManager type) rebuild themselves when they notice a new snapshot.
	internal sealed class State
	{
		public static readonly State Empty = new State(new Dictionary<Type, Registration>());
		// Snapshots are never mutated after construction, which is exactly the contract
		// FrozenDictionary is designed for: slower to build, faster to read.
		#if NET8_0_OR_GREATER
		public readonly System.Collections.Frozen.FrozenDictionary<Type, Registration> ByType;
		public State(Dictionary<Type, Registration> byType)
			=> ByType = System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(byType);
		#else
		public readonly Dictionary<Type, Registration> ByType;
		public State(Dictionary<Type, Registration> byType) => ByType = byType;
		#endif
	}

	readonly object _mutex = new object();
	internal volatile State _state = State.Empty;
	// typeof(SM) => Tables<SM>. Tables are lazily built and cached per registry.
	readonly ConcurrentDictionary<Type, object> _tables = new ConcurrentDictionary<Type, object>();

	#region Registration (Add methods)

	/// <summary>Registers all synchronizers found in an open generic class with a
	///   single type parameter constrained to <see cref="ISyncManager"/> (e.g.
	///   <c>Add(typeof(MySynchronizers&lt;>))</c>). See class remarks for the
	///   recognized synchronizer shapes. Each synchronizer's tag (per the ambient
	///   <see cref="TypeTagRegistry"/>'s convention, normally
	///   <see cref="TypeTagAttribute"/>) is recorded in
	///   <see cref="TypeTagRegistry.Default"/>.</summary>
	/// <exception cref="ArgumentException">The class is not an open generic class
	///   of the required shape, no synchronizers were found in it, or (when
	///   <c>replaceExisting</c> is false) a type or tag was already registered.</exception>
	public void Add(Type openGenericClass, bool replaceExisting = false)
	{
		var found = ScanClass(openGenericClass);
		if (found.Count == 0)
			throw new ArgumentException(
				$"No synchronizers were found in {openGenericClass.Name}. Expected public static " +
				"methods like `T Sync(SM sync, T value)` and/or ISyncObject<SM, T> implementations.");
		lock (_mutex) {
			foreach (var reg in found)
				AddRegistration(reg, replaceExisting);
		}
		var tags = TypeTagRegistry.Default;
		foreach (var reg in found) {
			string? tag = reg.BodyMethod != null
				? tags.AttributeTagOf(reg.BodyMethod)
				: tags.AttributeTagOf(reg.OpenClass!, reg.ValueType);
			if (tag != null)
				tags.Add(reg.ValueType, tag, replaceExisting);
		}
	}

	/// <summary>Registers the synchronizer for <c>T</c> found in an open generic
	///   class, with an explicit tag that is recorded in
	///   <see cref="TypeTagRegistry.Default"/> (pass <c>tag: null</c> for no tag,
	///   in which case values of type T can only be read when T is the statically
	///   expected type). The explicit tag takes precedence over any
	///   <see cref="TypeTagAttribute"/>.</summary>
	public void Add<T>(string? tag, Type openGenericClass, bool replaceExisting = false)
	{
		var found = ScanClass(openGenericClass).Where(r => r.ValueType == typeof(T)).ToList();
		if (found.Count == 0)
			throw new ArgumentException(
				$"No synchronizer for {typeof(T).NameWithGenericArgs()} was found in {openGenericClass.Name}.");
		lock (_mutex)
			AddRegistration(found[0], replaceExisting);
		if (tag != null)
			TypeTagRegistry.Default.Add(typeof(T), tag, replaceExisting);
	}

	/// <summary>Registers a synchronizer in the form of a delegate that accepts
	///   <see cref="ISyncManager"/> itself. This is the most convenient form, but
	///   also the slowest one: each field the delegate synchronizes uses interface
	///   dispatch, and if the SyncManager is a struct, it is boxed once per object.
	///   For maximum speed, put your synchronizers in a generic class instead and
	///   use <see cref="Add(Type, bool)"/>. If <c>tag</c> is null, the ambient
	///   <see cref="TypeTagRegistry"/>'s convention (normally a
	///   <see cref="TypeTagAttribute"/> on the delegate's method) is consulted.</summary>
	public void Add<T>(string? tag, SyncObjectFunc<ISyncManager, T> body, bool replaceExisting = false)
	{
		var tags = TypeTagRegistry.Default;
		tag = tag ?? tags.AttributeTagOf(body);
		lock (_mutex)
			AddRegistration(new Registration(typeof(T)) { SimpleBody = body }, replaceExisting);
		if (tag != null)
			tags.Add(typeof(T), tag, replaceExisting);
	}

	static List<Registration> ScanClass(Type openGenericClass)
	{
		if (openGenericClass == null)
			throw new ArgumentNullException(nameof(openGenericClass));
		Type smParam;
		if (!openGenericClass.IsGenericTypeDefinition
			|| openGenericClass.GetGenericArguments().Length != 1
			|| !(smParam = openGenericClass.GetGenericArguments()[0])
				.GetGenericParameterConstraints().Any(c => c == typeof(ISyncManager)))
			throw new ArgumentException(
				$"{openGenericClass.Name} is not an open generic class with a single type " +
				"parameter constrained to ISyncManager (expected e.g. `class MySync<SM> where SM : ISyncManager`).");

		var list = new List<Registration>();

		// Static "body" methods: T Sync(SM sync, T value)
		foreach (var mi in openGenericClass.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
			if (mi.IsGenericMethodDefinition || mi.ReturnType == typeof(void))
				continue;
			var p = mi.GetParameters();
			if (p.Length == 2 && p[0].ParameterType == smParam
				&& p[1].ParameterType == mi.ReturnType && !Involves(mi.ReturnType, smParam)) {
				list.Add(new Registration(mi.ReturnType) {
					OpenClass = openGenericClass,
					BodyMethod = mi,
				});
			}
		}

		// ISyncObject<SM, T> implementations
		foreach (var iface in openGenericClass.GetInterfaces()) {
			if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(ISyncObject<,>)) {
				var args = iface.GetGenericArguments();
				if (args[0] == smParam && !Involves(args[1], smParam)
					&& !list.Any(r => r.ValueType == args[1])) {
					list.Add(new Registration(args[1]) {
						OpenClass = openGenericClass,
						ViaISyncObject = true,
					});
				}
			}
		}
		return list;
	}

	static bool Involves(Type type, Type genericParam)
	{
		if (type == genericParam)
			return true;
		if (type.IsArray || type.IsByRef || type.IsPointer)
			return Involves(type.GetElementType()!, genericParam);
		return type.IsGenericType && type.GetGenericArguments().Any(a => Involves(a, genericParam));
	}

	void AddRegistration(Registration reg, bool replaceExisting) // called within lock(_mutex)
	{
		var s = _state;
		if (!replaceExisting && s.ByType.ContainsKey(reg.ValueType))
			throw new ArgumentException(
				$"A synchronizer for {reg.ValueType.NameWithGenericArgs()} is already registered.");
		var byType = new Dictionary<Type, Registration>(s.ByType);
		byType[reg.ValueType] = reg;
		_state = new State(byType);
	}

	#endregion

	#region Lookup side

	/// <summary>Returns true if this registry has a synchronizer for the given
	///   type, or for at least one type derived from it (in which case values of
	///   this type may be readable/writable dynamically via type tags).</summary>
	public bool Handles(Type type)
	{
		var s = _state;
		if (s.ByType.ContainsKey(type))
			return true;
		foreach (var valueType in s.ByType.Keys) {
			if (type.IsAssignableFrom(valueType))
				return true;
		}
		return false;
	}

	internal Tables<SM> TablesFor<SM>() where SM : ISyncManager
	{
		if (_tables.TryGetValue(typeof(SM), out object? t))
			return (Tables<SM>) t;
		return (Tables<SM>) _tables.GetOrAdd(typeof(SM), _ => new Tables<SM>(this));
	}

	/// <summary>Per-(registry, SyncManager type) dispatch tables: runtime types
	///   map to ready-to-call delegates. Built with reflection once, then rebuilt
	///   (copy-on-write) only when the registry has changed.</summary>
	internal sealed class Tables<SM> where SM : ISyncManager
	{
		sealed class Snapshot
		{
			public readonly State Source; // for staleness detection (reference identity)
			// This is the hot dispatch table: read on every dynamically-typed field,
			// rebuilt only when the registry changes. See note on State.ByType.
			#if NET8_0_OR_GREATER
			public readonly System.Collections.Frozen.FrozenDictionary<Type, Func<SM, object?, object?>> ByType;
			public Snapshot(State source, Dictionary<Type, Func<SM, object?, object?>> byType)
				{ Source = source; ByType = System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(byType); }
			#else
			public readonly Dictionary<Type, Func<SM, object?, object?>> ByType;
			public Snapshot(State source, Dictionary<Type, Func<SM, object?, object?>> byType)
				{ Source = source; ByType = byType; }
			#endif
		}

		readonly TypeSyncRegistry _owner;
		volatile Snapshot? _snap;

		public Tables(TypeSyncRegistry owner) => _owner = owner;

		Snapshot GetSnapshot()
		{
			var snap = _snap;
			var state = _owner._state;
			if (snap == null || snap.Source != state)
				_snap = snap = Build(state); // benign race: idempotent
			return snap;
		}

		public Func<SM, object?, object?>? TryByType(Type type)
		{
			GetSnapshot().ByType.TryGetValue(type, out Func<SM, object?, object?>? body);
			return body;
		}

		static Snapshot Build(State state)
		{
			var byType = new Dictionary<Type, Func<SM, object?, object?>>(state.ByType.Count);
			foreach (var reg in state.ByType.Values)
				byType[reg.ValueType] = MakeBody(reg);
			return new Snapshot(state, byType);
		}

		// Builds the type-erased invoker for one registration. All reflection for
		// the (registry, SM, T) triple happens here, once; the returned delegate
		// contains only casts (which never box when T is a class).
		static Func<SM, object?, object?> MakeBody(Registration reg)
		{
			MethodInfo helper;
			object arg;
			if (reg.SimpleBody != null) {
				helper = typeof(Tables<SM>).GetMethod(nameof(WrapSimpleBody),
					BindingFlags.NonPublic | BindingFlags.Static)!;
				arg = reg.SimpleBody;
			} else {
				var closedClass = reg.OpenClass!.MakeGenericType(typeof(SM));
				var funcType = typeof(Func<,,>).MakeGenericType(typeof(SM), reg.ValueType, reg.ValueType);
				Delegate typed;
				if (reg.ViaISyncObject) {
					var iface = typeof(ISyncObject<,>).MakeGenericType(typeof(SM), reg.ValueType);
					var map = closedClass.GetInterfaceMap(iface);
					object instance = Activator.CreateInstance(closedClass)!;
					typed = Delegate.CreateDelegate(funcType, instance, map.TargetMethods[0]);
				} else {
					var open = reg.BodyMethod!;
					var closedMethod = closedClass.GetMethod(open.Name,
						BindingFlags.Public | BindingFlags.Static, null,
						new[] { typeof(SM), reg.ValueType }, null)
						?? throw new MissingMethodException(closedClass.Name, open.Name);
					typed = Delegate.CreateDelegate(funcType, closedMethod);
				}
				helper = typeof(Tables<SM>).GetMethod(nameof(WrapTypedBody),
					BindingFlags.NonPublic | BindingFlags.Static)!;
				arg = typed;
			}
			return (Func<SM, object?, object?>) helper
				.MakeGenericMethod(reg.ValueType).Invoke(null, new object[] { arg })!;
		}

		static Func<SM, object?, object?> WrapTypedBody<T>(Delegate typed)
		{
			var body = (Func<SM, T, T>) typed;
			return (sm, v) => body(sm, v == null ? default! : (T) v);
		}

		static Func<SM, object?, object?> WrapSimpleBody<T>(Delegate simple)
		{
			var body = (SyncObjectFunc<ISyncManager, T>) simple;
			return (sm, v) => body(sm, v == null ? default : (T) v);
		}
	}

	#endregion
}

