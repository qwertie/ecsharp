using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib
{
	/// <summary>Lists the kinds of child objects supported by <see cref="ISyncManager"/>.
	///   The three basic kinds are Normal, List and Tuple, which are mutually 
	///   exclusive; Nullable and Deduplicate are flags that can be combined 
	///   with the three basic kinds.</summary>
	[Flags]
	public enum ObjectMode {
		/// <summary>A normal object, in which fields may have names.</summary>
		Normal = 0,
		
		/// <summary>A list object, in which there are a variable number of unnamed 
		///   fields. <see cref="ISyncManager.IsInsideList"/> will be true inside 
		///   this kind of subobject, and <see cref="ISyncManager.ReachedEndOfList"/>
		///   will be non-null in the Loading and Schema modes.</summary>
		/// <remarks>When calling one of the <see cref="ISyncManager.SyncList"/> 
		///   methods, this mode will be used implicitly if both the List and Tuple
		///   flags are missing.</remarks>
		List = 1,

		/// <summary>A tuple object, in which there are a fixed number of unnamed 
		/// fields (and the number of fields is known before starting to load the
		/// tuple). <see cref="ISyncManager.IsInsideList"/> will be true inside 
		/// this kind of subobject. This mode is used together with a constant
		/// `tupleLength` parameter that is greater than 0.</summary>
		/// <remarks>Some implementations of <see cref="ISyncManager"/> treat this
		///   mode identically to <see cref="List"/> mode, but Tuple mode gives 
		///   permission not to store the list length in the data stream, in order 
		///   to make the output more compact. When loading data that was saved in 
		///   Tuple mode (without a list length), Tuple mode must be specified by
		///   the loading code, and it must already know the list length, as 
		///   <see cref="ISyncManager.MinimumListLength"/> and 
		///   <see cref="ISyncManager.ReachedEndOfList"/> will be null.
		///   Many extension methods have a <c>tupleLength</c> parameter which 
		///   specifies this missing list length.
		/// </remarks>
		Tuple = 3,

		/// <summary>Deduplication is performed on the subobject, allowing object graphs 
		///   that contain cycles. If <see cref="ISyncManager.SupportsReordering"/> is 
		///   true, it's possible in Loading Mode that it is already known whether the 
		///   input data stream used deduplication or not. In that case, it doesn't make 
		///   a difference whether this flag is present or not.</summary>
		/// <remarks><![CDATA[Note: ObjectMode.Deduplicate is incompatible with 
		///   Memory<T> and ReadOnlyMemory<T>]]>.</remarks>
		Deduplicate = 4,
		
		/// <summary>The object is not allowed to be null. Certain serializers 
		///   may use the knowledge that a sub-object is never null to save space.
		///   Also, this flag should be used (without <see cref="Deduplicate"/>) when 
		///   reading/writing value types as a hint to the serializer to avoid boxing.
		/// </summary>
		NotNull = 8,

		/// <summary>Requests that no type tag be read or written for this object,
		///   even if its synchronizer has a <see cref="TypeTagAttribute"/> or was
		///   registered with a tag in a <see cref="TypeSyncRegistry"/>. This is
		///   useful for compactness when the object's type is known statically, or
		///   for interoperability with data formats that lack type tags.</summary>
		/// <remarks>Since most data formats are not self-describing (e.g.
		///   <see cref="SyncBinary"/>), the same flag must be used when writing an
		///   object and when reading it back; otherwise the data cannot be read
		///   correctly. Note: an object written with this flag cannot be read
		///   dynamically unless its static type is registered, because without a tag,
		///   the reader cannot choose a synchronizer based on the data alone.</remarks>
		NoTypeTag = 16,

		/// <summary>Requests that compact formatting be used when writing this object.
		///   When using <see cref="SyncJson.Writer"/>, this mode suppresses newlines.
		///   When using <see cref="SyncBinary"/>, this mode flag is ignored.</summary>
		Compact = 128,

		/// <summary>Requests that when there is a null value in the data stream and
		///   a value type is being read, the <c>default</c> value should be returned
		///   instead of throwing an exception.</summary>
		/// <remarks>Since synchronizers for primitive types (such as Int32) do not
		///   accept an <see cref="ObjectMode"/> parameter, a separate way of 
		///   indicating this mode is needed for primitive types. For example, when 
		///   using <see cref="SyncJson.Reader"/>, you can enable the 
		///   <see cref="SyncJson.Options.ForReader.ReadNullPrimitivesAsDefault"/>
		///   option to get a similar effect.</remarks>
		ReadNullAsDefault = 256,

		// This approach is probably wrong: if caller is specifying the SyncObjectFunc, 
		// it can just call it directly; if not, the desire to unwrap could be 
		// indicated somehow when registering the SyncObjectFunc?
		/// <summary>This flag requests that <see cref="ISyncManager.BeginSubObject"/>
		/// not be called before invoking the synchronizer, so that field(s) of the 
		/// child object are dumped directly into the parent object.</summary>
		//NoObject = 256,
	}
}
