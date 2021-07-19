using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib
{
	/// <summary>Lists the kinds of child objects supported by <see cref="ISyncManager"/>.
	///   The three basic kinds are Normal, Tuple and List, which are mutually 
	///   exclusive; Nullable and Deduplicate are flags that can be combined 
	///   with the three basic kinds.</summary>
	[Flags]
	public enum SubObjectMode {
		/// <summary>A normal object, in which fields may have names.</summary>
		Normal = 0,
		
		/// <summary>A list object, in which there are a variable number of unnamed 
		/// fields. <see cref="ISyncManager.IsInsideList"/> will be true inside 
		/// this kind of subobject, and <see cref="ISyncManager.ReachedEndOfList"/>
		/// will be non-null in the Loading and Schema modes.</summary>
		/// <remarks>When calling one of the <see cref="ISyncManager.SyncList"/> 
		///   methods, this mode will be used implicitly if both the List and Tuple
		///   flags are missing.</remarks>
		List = 1,
		
		/// <summary>A tuple object, in which there are a fixed number of unnamed 
		/// fields (and the number of fields is known before starting to load the
		/// tuple). <see cref="ISyncManager.IsInsideList"/> will be true inside 
		/// this kind of subobject.</summary>
		/// <remarks>Some implementations of <see cref="ISyncManager"/> treat this
		///   mode identically to <see cref="List"/> mode, but Tuple mode gives 
		///   permission not to store the list length in the data stream, in order 
		///   to make the output more compact. When loading data that was saved in 
		///   Tuple mode (without a list length), Tuple mode must be specified by
		///   the loading code, and it must already know the list length, as 
		///   <see cref="ISyncManager.MinimumListLength"/> and 
		///   <see cref="ISyncManager.ReachedEndOfList"/> will be null.</remarks>
		Tuple = 3,

		/// <summary>Deduplication is performed on the subobject, allowing object graphs 
		/// that contain cycles. If <see cref="ISyncManager.SupportsReordering"/> is 
		/// true, it's possible in Loading Mode that it is already known whether the 
		/// input data stream used deduplication or not. In that case, it doesn't make 
		/// a difference whether this flag is present or not.</summary>
		Deduplicate = 4,
		
		/// <summary>The object is not allowed to be null. Certain serializers 
		/// may use the knowledge that a sub-object is never null to save space.</summary>
		NotNull = 8,
		
		/// <summary>The object can have multiple types or derived classes, so a
		/// type identifier needs to be saved to make deserialization possible.
		/// TODO: figure out how it will work and say something about that here</summary>
		DynamicType = 16,
		
		/// <summary>Requests a fixed-size representation, if available.
		/// When using <see cref="SyncJson"/>, this has no effect.</summary>
		FixedSize = 32,

		/// <summary>Requests that compact formatting be used when writing this object.
		/// When using <see cref="SyncJson.Writer"/>, this mode suppresses newlines.</summary>
		Compact = 64,

		// This approach is probably wrong: if caller is specifying the SyncObjectFunc, 
		// it can just call it directly; if not, the desire to unwrap could be 
		// indicated somehow when registering the SyncObjectFunc?
		/// <summary>This flag requests that <see cref="ISyncManager.BeginSubObject"/>
		/// not be called before invoking the synchronizer, so that field(s) of the 
		/// child object are dumped directly into the parent object.</summary>
		//NoObject = 256,
	}
}
