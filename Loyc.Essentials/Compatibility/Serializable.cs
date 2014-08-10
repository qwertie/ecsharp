using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Compatibility
{
#if CompactFramework
	// TODO: define a minimal Serialization implementation for .NET Compact Framework

	class SerializableAttribute : Attribute
	{
	}

	// Summary:
	//     Allows an object to control its own serialization and deserialization.
	[ComVisible(true)]
	public interface ISerializable
	{
		// Summary:
		//     Populates a System.Runtime.Serialization.SerializationInfo with the data
		//     needed to serialize the target object.
		//
		// Parameters:
		//   info:
		//     The System.Runtime.Serialization.SerializationInfo to populate with data.
		//
		//   context:
		//     The destination (see System.Runtime.Serialization.StreamingContext) for this
		//     serialization.
		//
		// Exceptions:
		//   System.Security.SecurityException:
		//     The caller does not have the required permission.
		void GetObjectData(SerializationInfo info, StreamingContext context);
	}
#endif
}
