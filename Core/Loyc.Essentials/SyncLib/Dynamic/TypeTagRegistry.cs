using Loyc.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Loyc.SyncLib;

/// <summary>The module that registers and resolves type tags: it encapsulates
///   (1) the bidirectional dictionary between type tags and .NET types,
///   (2) the convention by which tags are attached to synchronizers (the
///   <see cref="TypeTagAttribute"/>), and (3) the policy for tags in a data
///   stream that don't match anything registered or expected. All three are
///   customizable: the dictionary via <see cref="Add"/>, and the convention and
///   policies by overriding the virtual methods in a derived class and swapping
///   it in with <see cref="SetDefault"/> (the Ambient Service Pattern).</summary>
/// <remarks>
///   This registry deliberately knows nothing about synchronizers: which
///   synchronizer handles which type is the concern of <see cref="TypeSyncRegistry"/>,
///   which records tags it discovers here (in the instance that is
///   <see cref="Default"/> at the time of the <c>Add</c> call).
/// <para/>
///   Registry behavior can be changed by writing a derived class and installing it
///   by changing the <see cref="GlobalDefault"/> or calling <see cref="SetDefault"/>.
///   For example, you could change the error-handling policies:
///   <ul>
///   <li><see cref="UnknownTagError"/> is called when reading dynamically and the tag
///     in the data stream is not in the dictionary. The default throws
///     <see cref="FormatException"/>; an override can return a substitute type
///     (which must have a registered synchronizer), or null to fall back to the
///     statically-expected type.</li>
///   <li><see cref="TagMismatchError"/> is called when reading with an explicit
///     (statically-typed) synchronizer and the data stream contains a different
///     tag than the synchronizer's. The default throws
///     <see cref="FormatException"/>; an override that returns normally causes
///     the read to proceed with the expected synchronizer anyway.</li>
///   </ul>
/// </remarks>
public class TypeTagRegistry
{
	#region Ambient Service Pattern (async-local Default)

	static readonly AmbientService<TypeTagRegistry> _ambient =
		new AmbientService<TypeTagRegistry>(new TypeTagRegistry());

	/// <summary>The registry used by every execution context that has no ambient
	///   override from <see cref="SetDefault"/>. It can be replaced, e.g. to
	///   install a subclass with a different tagging convention app-wide.</summary>
	public static TypeTagRegistry GlobalDefault {
		get => _ambient.GlobalDefault;
		set => _ambient.GlobalDefault = value;
	}

	/// <summary>The ambient tag registry: the current execution context's override
	///   (see <see cref="SetDefault"/>) if one is active, else <see cref="GlobalDefault"/>.
	///   See <see cref="AmbientService{T}"/> for how overrides flow across await
	///   and why the no-override case costs only a static field read.</summary>
	public static TypeTagRegistry Default => _ambient.Value;

	/// <summary>Sets the current async-local default registry. Designed to be used
	///   in a <c>using</c> statement, which restores the old value at the end.
	///   Caution: AsyncLocal variables are slow, causing a small performance hit when
	///   using this.</summary>
	public static AmbientService<TypeTagRegistry>.Saved SetDefault(TypeTagRegistry newValue)
		=> _ambient.Set(newValue);

	#endregion

	#region The tag <-> type dictionary

	// Immutable snapshots swapped atomically (copy-on-write): lookups need no locks.
	sealed class State
	{
		public static readonly State Empty =
			new State(new Dictionary<Type, string>(), new Dictionary<string, Type>());
		// Snapshots are never mutated after construction, which is exactly the contract
		// FrozenDictionary is designed for: slower to build, faster to read.
		#if NET8_0_OR_GREATER
		public readonly System.Collections.Frozen.FrozenDictionary<Type, string> TagByType;
		public readonly System.Collections.Frozen.FrozenDictionary<string, Type> TypeByTag;
		public State(Dictionary<Type, string> tagByType, Dictionary<string, Type> typeByTag)
		{
			TagByType = System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(tagByType);
			TypeByTag = System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(typeByTag);
		}
		#else
		public readonly Dictionary<Type, string> TagByType;
		public readonly Dictionary<string, Type> TypeByTag;
		public State(Dictionary<Type, string> tagByType, Dictionary<string, Type> typeByTag)
			{ TagByType = tagByType; TypeByTag = typeByTag; }
		#endif
	}

	readonly object _mutex = new object();
	volatile State _state = State.Empty;

	/// <summary>Associates a type tag with a type (in both directions).
	///   Re-adding an identical association is harmless; a conflicting one
	///   throws unless <c>replaceExisting</c> is true.</summary>
	public void Add(Type type, string tag, bool replaceExisting = false)
	{
		if (type == null) throw new ArgumentNullException(nameof(type));
		if (tag == null) throw new ArgumentNullException(nameof(tag));
		lock (_mutex) {
			var s = _state;
			if (s.TagByType.TryGetValue(type, out string? oldTag) && oldTag == tag
				&& s.TypeByTag.TryGetValue(tag, out Type? oldType) && oldType == type)
				return; // identical association already exists
			if (!replaceExisting) {
				if (s.TagByType.TryGetValue(type, out oldTag) && oldTag != tag)
					throw new ArgumentException(
						$"{type.NameWithGenericArgs()} already has the type tag '{oldTag}'.");
				if (s.TypeByTag.TryGetValue(tag, out Type? other) && other != type)
					throw new ArgumentException(
						$"The type tag '{tag}' is already registered (for {other.NameWithGenericArgs()}).");
			}
			var tagByType = new Dictionary<Type, string>(s.TagByType);
			var typeByTag = new Dictionary<string, Type>(s.TypeByTag);
			// When replacing, drop the old half of each association being displaced
			if (tagByType.TryGetValue(type, out oldTag) && oldTag != tag)
				typeByTag.Remove(oldTag);
			if (typeByTag.TryGetValue(tag, out Type? displaced) && displaced != type)
				tagByType.Remove(displaced);
			tagByType[type] = tag;
			typeByTag[tag] = type;
			_state = new State(tagByType, typeByTag);
		}
	}

	/// <summary>Gets the tag registered for a type, or null.</summary>
	public string? TagOf(Type type)
		=> _state.TagByType.TryGetValue(type, out string? tag) ? tag : null;

	/// <summary>Gets the type registered for a tag, or null.</summary>
	public Type? TypeOf(string tag)
		=> _state.TypeByTag.TryGetValue(tag, out Type? type) ? type : null;

	#endregion

	#region The tagging convention (how synchronizers declare their tags)

	// Caches for the default (attribute-based) convention. They are per-instance,
	// so a registry with a custom convention does not see stale answers.
	ConcurrentDictionary<MethodInfo, string?>? _methodTagCache;
	ConcurrentDictionary<(Type, Type), string?>? _syncObjTagCache;
	ConcurrentDictionary<Delegate, string?>? _delegateTagCache;

	/// <summary>Gets the tag declared by a synchronizer function, or null.</summary>
	/// <remarks>This is called on a hot path: <see cref="Impl.ObjectSyncher"/>
	///   resolves the tag every time a delegate-based synchronizer is used for a
	///   field. It is cached by delegate <i>value</i> because obtaining
	///   <see cref="Delegate.Method"/> is a (surprisingly slow) reflection
	///   operation that must not run per call.</remarks>
	public string? AttributeTagOf(Delegate syncFunc)
	{
		var cache = _delegateTagCache ??= new ConcurrentDictionary<Delegate, string?>();

		if (!cache.TryGetValue(syncFunc, out string? tag)) {
			tag = AttributeTagOf(syncFunc.Method);
			// It's suspicious if there are too many synchronizers in one app--could be a case  
			// of the same method on lots of new'd synchronizers that were meant to be GC'd.
			if (cache.Count < 2000)
				cache[syncFunc] = tag;
		}
		return tag;
	}

	/// <summary>Gets the tag declared by a synchronizer method: by default, a
	///   <see cref="TypeTagAttribute"/> on the method or, failing that, on its
	///   declaring type. Override to change the convention (e.g. to derive tags
	///   from type names).</summary>
	public virtual string? AttributeTagOf(MethodInfo synchronizerMethod)
	{
		var cache = _methodTagCache ??= new ConcurrentDictionary<MethodInfo, string?>();
		return cache.GetOrAdd(synchronizerMethod, m =>
			m.GetCustomAttribute<TypeTagAttribute>()?.Tag
			?? m.DeclaringType?.GetCustomAttribute<TypeTagAttribute>()?.Tag);
	}

	/// <summary>Gets the tag declared by a synchronizer type (e.g. a struct
	///   implementing <see cref="ISyncObject{SM, T}"/>) as it applies to values
	///   of type <c>valueType</c>: by default, a <see cref="TypeTagAttribute"/>
	///   on a method whose return type is valueType (so one synchronizer type
	///   can serve multiple value types with different tags), then on the type
	///   itself.</summary>
	public virtual string? AttributeTagOf(Type synchronizerType, Type valueType)
	{
		var cache = _syncObjTagCache ??= new ConcurrentDictionary<(Type, Type), string?>();
		return cache.GetOrAdd((synchronizerType, valueType), pair => {
			var (syncObjType, valType) = pair;
			var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
			foreach (var m in syncObjType.GetMethods(flags)) {
				if (m.ReturnType == valType) {
					var attr = m.GetCustomAttribute<TypeTagAttribute>();
					if (attr != null)
						return attr.Tag;
				}
			}
			return syncObjType.GetCustomAttribute<TypeTagAttribute>()?.Tag;
		});
	}

	#endregion

	#region Policy for tags in a data stream that don't match

	/// <summary>Called when a dynamically-typed read encounters a tag that is not
	///   in the dictionary. The default throws <see cref="FormatException"/>.
	///   An override may return a substitute type to synchronize instead (it must
	///   have a registered synchronizer), or null to fall back to the
	///   statically-expected type.</summary>
	public virtual Type? UnknownTagError(string tag, Type expectedType, FieldId field)
		=> throw new FormatException(
			$"'{field.Name}' contains an object tagged '{tag}', which is not registered " +
			"in the current TypeTagRegistry.");

	/// <summary>Called when a statically-typed read encounters a tag that differs
	///   from the tag of the synchronizer being used. The default throws
	///   <see cref="FormatException"/>; if an override returns normally, the read
	///   proceeds with the expected synchronizer anyway.</summary>
	public virtual void TagMismatchError(string expectedTag, string tagInStream, Type expectedType, FieldId field)
		=> throw new FormatException(
			$"Expected an object tagged '{expectedTag}' ({expectedType.NameWithGenericArgs()}), " +
			$"but the data stream contains an object tagged '{tagInStream}'. If '{field.Name}' " +
			"was written dynamically, it must also be read dynamically (e.g. with SyncDynamic).");

	#endregion
}
