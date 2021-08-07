using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib.Impl
{
	public struct SyncEnumAsString<SyncManager, E> : ISyncField<SyncManager, E>
		where SyncManager : ISyncManager
		where E : struct, Enum
	{
		public E Sync(ref SyncManager sync, FieldId name, E value)
		{
			if (sync.IsSaving) {
				sync.Sync(name, value.ToString());
				return value;
			} else {
				#if NETSTANDARD2_0
				return (E) Enum.Parse(typeof(E), sync.Sync(name, "")!);
				#else
				return Enum.Parse<E>(sync.Sync(name, "")!);
				#endif
			}
		}
	}
}
