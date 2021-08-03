using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib
{
	/// <summary>Abstract types used with <see cref="ISyncManager.HasField"/>.</summary>
	/// <remarks>
	/// Designing this enum was a tradeoff between detail and simplicity. 
	/// <para/>
	/// Often, serialized data streams do not identify types in a way that we
	/// could compare a .NET Type object to them. For example, it is not possible
	/// for an ISyncManager to check whether the JSON object {"R":5,"G":99,"B":17} 
	/// matches type System.Drawing.Color, because it doesn't know what schema will
	/// be used to read the object. Therefore, all composite types are identified 
	/// simply as Object or as one of the List types.
	/// <para/>
	/// I decided instead to represent types using an enum, but I didn't want to 
	/// burden ISyncManager implementations with excessive complexity or detail,
	/// so the enum doesn't contain every primitive type (e.g. short and long 
	/// integers are both classified as Integer).
	/// </remarks>
	public enum SyncType {
		/// <summary>This is returned by <see cref="ISyncManager.HasField"/> 
		/// when the reader cannot determine whether the field exists, or when
		/// the ISyncManager is not a reader (i.e. the mode is not SyncMode.Loading)</summary>
		Unknown = -1,
		/// <summary>This is returned by <see cref="ISyncManager.HasField"/> 
		/// when the reader has determined that the field does not exist.</summary>
		Missing = 0,
		/// <summary>Indicates that the field exists but that the implementation of 
		/// ISyncManager cannot determine the data type of the field in advance.</summary>
		Exists = 1,
		Boolean = 2,
		Byte = 3,
		Unsigned = 4,
		Integer = 5,
		Float = 6,
		Char = 7,
		Object = 8,
		/// <summary>When returned from HasField(), this value indicates that the stream 
		/// contains the `null` value.</summary>
		Null = 16,
		NullableBoolean = Null | Boolean,
		NullableByte = Null | Byte,
		NullableUnsigned = Null | Unsigned,
		NullableInteger = Null | Integer,
		NullableFloat = Null | Float,
		NullableChar = Null | Char,
		NullableObject = Null | Object,
		/// <summary>When returned from HasField(), this value indicates that the stream 
		/// contains a list, when the type of the list items is dynamic or unknown.</summary>
		List = 32,
		BooleanList = List | Boolean,
		ByteList = List | Byte,
		UnsignedList = List | Unsigned,
		IntegerList = List | Integer,
		FloatList = List | Float,
		CharList = List | Char,
		ObjectList = List | Object,
		/// <summary>A synonym for CharList. ISyncManager should treat a string the
		/// same way it treats a list of characters.</summary>
		String = CharList,
	}
}
