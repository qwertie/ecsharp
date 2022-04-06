using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib
{
	partial class SyncBinary
	{
		/// <summary>The possible values of <see cref="Options.FieldIdMode"/>.</summary>
		public enum FieldIdMode
		{
			/// <summary>Field identifiers are not read or written.</summary>
			/// <remarks>
			///   This is the most dangerous mode, but it's also the simplest, the 
			///   most efficient and the most compact. Plus, this mode is needed 
			///   for reading most 3rd-party and legacy file formats.
			/// <para/>
			///   In this mode, the field ID parameter is ignored in all calls to
			///   the Sync() methods, and only data values are written to the file.
			///   For example, suppose <c>sm</c> is a <see cref="SyncBinary.Writer"/>
			///   and you write two values like so:
			/// <example>
			///     sm.Sync("Fruit", "Lime");
			///     sm.Sync("Number", 123);
			///     sm.Sync("Happy", true);
			/// </example>
			///   The default output will be 7 bytes: 5, 'L', 'i', 'm', 'e', 123, 1.
			/// <para/>
			///   As you can see, the file format is "raw", with no delimiters or
			///   schema to help indicate the meaning of the bytes.
			/// <para/>
			///   This works perfectly fine as long as the <i>same code</i> is used 
			///   to read the stream that was used to write the stream. You can even
			///   support versioning pretty easily by explicitly storing a version
			///   number in the stream. For example, suppose that the "Number" field
			///   was added in version 2. You can support both versions 1 and 2 
			///   using code like this:
			/// <example>
			///   public class ExampleObject
			///   {
			///      public int Number;
			///      public string? Fruit;
			///      public bool Happy;
			///   }
			///   public class ExampleSync<SM> : ISyncObject<SM, ExampleObject> where SM: ISyncManager
			///   {
			///      public int Version = 2;
			///      
			///      public ExampleObject Sync(SM sm, ExampleObject? value)
			///      {
			///         value ??= new ExampleObject();
			///         
			///         Version = sm.Sync("Version", Version);
			///         if (Version > 2 || Version < 1)
			///            throw new FormatException("Unrecognized data format");
			///         
			///         sm.SyncRef("Fruit", ref value.Fruit);
			///         if (Version >= 2)
			///            sm.SyncRef("Number", ref value.Number);
			///         sm.SyncRef("Happy", ref value.Happy);
			///         
			///         return value;
			///      }
			///   }
			/// </example>
			/// </remarks>
			Off = 0,
			/// <summary>Field identifiers are read and written as strings. A side 
			///   effect is that <see cref="ISyncManager.SupportsNextField"/> will
			///   return true, although <see cref="ISyncManager.SupportsReordering"/>
			///   remains false.</summary>
			/// <remarks>
			///   As usual, list/tuple items don't have a field ID even in this mode.
			/// <para/>
			///   When reading a stream, an exception is thrown if the field ID 
			///   doesn't match the string in the FieldId, except if the FieldId is
			///   null, in which case the field ID string is read but ignored.
			/// <para/>
			///   If you write a field with a null field ID, that null value is 
			///   still written to the file, not suppressed, so that 
			///   <see cref="ISyncManager.NextField"/> still works properly when 
			///   reading the file.
			/// </remarks>
			Strings = 1,
			/// <summary>Field identifiers are read and written as integers. A side 
			///   effect is that <see cref="ISyncManager.SupportsNextField"/> and 
			///   <see cref="ISyncManager.NeedsIntegerIds"/> will both return true,
			///   although <see cref="ISyncManager.SupportsReordering"/> will remain 
			///   false.</summary>
			/// <remarks>
			///   As usual, list/tuple items don't have a field ID even in this mode.
			/// <para/>
			///   When reading a stream, an exception is thrown if the field ID 
			///   doesn't match the integer in the FieldId, except if the FieldId 
			///   lacks an integer, in which case the field ID number is read but 
			///   ignored.
			/// <para/>
			///   If no integer FieldId is provided when writing the file, the number
			///   N+1 is written instead, where N is zero if this is the first field
			///   in an object, or N is the last field number to have been written in 
			///   the same object.
			/// </remarks>
			Integers = 2,
		}
	}
}
