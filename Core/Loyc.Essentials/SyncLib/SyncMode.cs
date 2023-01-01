using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib
{
	/// <summary>These are the four categories of <see cref="ISyncManager"/> types.</summary>
	public enum SyncMode {
		/// <summary>Data is being loaded. Your sync function should return a new 
		///   object.</summary>
		Reading = 1,
		/// <summary>Data is being saved. Your sync function should save the object 
		///   provided. This mode could also used for synchronization behaviors in 
		///   which a one-way synchronization occurs from the object to an 
		///   underlying data store, e.g. updating some kind of virtual DOM.
		///   In this mode, the return value of every Sync function must be the same
		///   value that was passed into the <c>savable</c> parameter.</summary>
		Writing = 2, 
		/// <summary>A schema is being saved. Similar to the Loading mode, your 
		///   sync function will not be given an object value in this mode. Your 
		///   sync function should call the same methods it would call if it were 
		///   loading an object, in order to teach the synchronizer about the 
		///   schema of the current type. However, the Sync methods of a schema 
		///   saver have no actual data and tend to return default/null values 
		///   instead, and it will skip subobjects whose schema is already known.</summary>
		Schema = 4 | Reading,
		/// <summary>A variation of the "saving" mode in which not all the data 
		///   will be saved because some kind of query is being used to select which
		///   parts of the data to include in the output.</summary>
		/// <remarks>This mode may also be used to gather schema information, but 
		///   unlike the Schema mode, this mode provides your code with an existing 
		///   object.</remarks>
		Query = 4 | Writing,
		/// <summary>A mode in which "synchronization" happens. You can think of this
		///   as a combination of the Saving, Loading, and Query modes. Like Saving mode,
		///   this mode provides an object to the synchronizer function. Like Loading
		///   mode, the values returned from Sync methods in <see cref="ISyncManager"/>
		///   should be stored into the object. And like Query mode, some objects in 
		///   the object graph may be ignored (not traversed).</summary>
		/// <remarks>None of the synchronizers bundled with Loyc.Serialization 
		///   use this mode.</remarks>
		Merge = Writing | Reading | Query,
	}
}
