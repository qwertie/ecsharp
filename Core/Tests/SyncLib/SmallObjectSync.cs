using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib.Tests
{
	public class SmallObject : IEquatable<SmallObject>
	{
		public int Field1;
		public string? Field2;
		public double Field3;

		public override bool Equals(object obj) => obj is SmallObject so && Equals(so);
		public override int GetHashCode() => Field1 + (Field2?.GetHashCode() ?? 0) + Field3.GetHashCode();
		public bool Equals(SmallObject other) => 
			Field1 == other.Field1 && Field2 == other.Field2 && Field3 == other.Field3;
	}

	public class SmallObjectSync<SM> : ISyncObject<SM, SmallObject> where SM : ISyncManager
	{
		public SmallObject Sync(SM sm, SmallObject? obj)
		{
			obj ??= new SmallObject();
			if (!sm.SupportsNextField || sm.NeedsIntegerIds) {
				// Synchronize in the normal way
				obj.Field1 = sm.Sync("Field1", obj.Field1);
				obj.Field2 = sm.Sync("Field2", obj.Field2);
				obj.Field3 = sm.Sync("Field3", obj.Field3);
			} else {
				// Synchronize fields in the order they appear in the input.
				FieldId name;
				while ((name = sm.NextField) != FieldId.Missing) {
					if (name.Name == "Field1") {
						obj.Field1 = sm.Sync(null, obj.Field1);
					} else if (name.Name == "Field2") {
						obj.Field2 = sm.Sync(null, obj.Field2);
					} else if (name.Name == "Field3") {
						obj.Field3 = sm.Sync(null, obj.Field3);
					} else {
						throw new Exception("Unexpected field: " + name.Name);
					}
				}
			}
			return obj;
		}
	}
}
