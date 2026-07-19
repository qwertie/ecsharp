using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib.Tests;

internal class MyItemGroup
{
	public string? GroupName { get; set; }
	public List<MyItem>? Items { get; set; }
}


internal class MyItem
{
	public int Id { get; set; }
	public string? SerialNumber { get; set; }
}

internal class MyItemV1
{
	public int Id { get; set; }
	public int SerialNumber { get; set; }
}

internal class MySync<SM> : ISyncObject<SM, MyItemGroup>, ISyncObject<SM, MyItem>, ISyncObject<SM, MyItemV1>
	where SM : ISyncManager
{
	public int Version { get; set; } = 2;

	public MyItemGroup Sync(SM sm, MyItemGroup? group)
	{
		Version = sm.Sync("Version", Version);
		if (Version < 1 || Version > 2)
			throw new InvalidOperationException("Item group has unrecognized version number");

		group ??= new MyItemGroup();
		group.GroupName = sm.Sync("GroupName", group.GroupName);
		group.Items = sm.SyncList("Items", group.Items, Sync);
		return group;
	}
	
	public MyItem Sync(SM sm, MyItem? entry)
	{
		entry ??= new MyItem();
		
		entry.Id = sm.Sync("Id", entry.Id);

		if (Version <= 1) {
			// Version 1 serial numbers are integers. Support reading but not writing.
			if (sm.IsWriting)
				throw new NotSupportedException();
			entry.SerialNumber = sm.Sync("SerialNumber", 0).ToString();
		} else {
			entry.SerialNumber = sm.Sync("SerialNumber", entry.SerialNumber);
		}

		return entry;
	}

	public MyItemV1 Sync(SM sm, MyItemV1? entry)
	{
		entry ??= new MyItemV1();
		entry.Id = sm.Sync("Id", entry.Id);
		entry.SerialNumber = sm.Sync("SerialNumber", entry.SerialNumber);
		return entry;
	}
}
