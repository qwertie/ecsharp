using System;

namespace Loyc.SyncLib;

/// <summary>Assigns a "type tag" to a synchronizer, which identifies the type of
///   object it reads/writes within a data stream, enabling dynamic typing
///   (polymorphic serialization and deserialization).</summary>
/// <remarks>
///   Place this attribute on a synchronizer method (preferred), or on a synchronizer 
///   struct/class that implements <see cref="ISyncObject{SM, T}"/>. Do not put it 
///   on the data types being synchronized, as SyncLib does not expect business objects 
///   to be concerned with serialization.
/// <para/>
///   This attribute is merely data; it is read and interpreted by
///   <see cref="TypeTagRegistry"/>, whose virtual <c>AttributeTagOf</c> methods
///   define the convention (and can be overridden to change it). In brief:
///   <ul>
///   <li>When a synchronizer has a type tag, the tag is written whether the
///     object appears in a statically-typed context (an explicit synchronizer
///     was given) or a dynamically-typed one (<see cref="SyncDynamicExt.SyncDynamic{SM, T}"/>),
///     so data written statically can be read back dynamically and vice versa.</li>
///   <li>When reading statically, the tag in the data stream is checked against
///     the expected tag; a mismatch is reported to
///     <see cref="TypeTagRegistry.TagMismatchError"/>, which throws by default.</li>
///   <li><see cref="ObjectMode.NoTypeTag"/> suppresses the tag at a particular
///     call site (the same mode flag must then be used when reading it back).</li>
///   <li><see cref="TypeSyncRegistry.Add(Type, bool)"/> records discovered tags
///     in <see cref="TypeTagRegistry.Default"/>; an explicit tag argument in the
///     <c>Add&lt;T&gt;</c> overloads takes precedence over this attribute.</li>
///   </ul>
///   Synchronizers without a tag behave as before: no tag is read or written
///   unless the synchronizer calls <see cref="ISyncManager.SyncTypeTag"/>
///   manually (a low-level technique that is incompatible with dynamic typing,
///   because a reader must know the tag <i>before</i> it can choose which
///   synchronizer to invoke).
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Struct | AttributeTargets.Class,
	AllowMultiple = false, Inherited = false)]
public sealed class TypeTagAttribute : Attribute
{
	public TypeTagAttribute(string tag) => Tag = tag ?? throw new ArgumentNullException(nameof(tag));

	public string Tag { get; }
}
