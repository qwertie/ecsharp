using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib.Tests
{
	class StringDictionarySync<SM> :
		ISyncObject<SM, IDictionary<string, string?>>
		where SM : ISyncManager
	{
		public IDictionary<string, string?> Sync(SM sm, IDictionary<string, string?>? dict)
		{
			dict ??= new Dictionary<string, string?>();
			if (sm.IsReading) {
				if (!sm.SupportsNextField || sm.NeedsIntegerIds || sm.IsWriting || sm.IsInsideList)
					throw new NotSupportedException(
						"StringDictionarySync is incompatible with this " + sm.GetType().Name);

				string? name;
				while ((name = sm.NextField.Name) != null) {
					dict[name] = sm.Sync(null, "");
				}
			} else {
				foreach (var pair in dict)
					sm.Sync(pair.Key, pair.Value);
			}
			return dict;
		}
	}
}
