using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib.Tests
{
	public class Family
	{
		public IList<Parent>? Parents;
		public IList<Child>? Children;

		public static Family DemoFamily()
		{
			var dad = new Parent { Name = "John" };
			var mum = new Parent { Name = "Mary" };

			var kid1 = new Child { Name = "Ann", Mother = mum, Father = dad };
			var kid2 = new Child { Name = "Barry", Mother = mum, Father = dad };
			var kid3 = new Child { Name = "Charlie", Mother = mum, Father = dad };

			var listOfKids = new List<Child> {kid1, kid2, kid3};
			dad.Children = listOfKids;
			mum.Children = listOfKids;

			return new Family { Parents = new List<Parent> {mum, dad}, Children = listOfKids };
		}
	}
	public class Parent
	{
		public string? Name { get; set; }
		public IList<Child>? Children { get; set; }
	}
	public class Child
	{
		public string? Name { get; set; }
		public Parent? Father { get; set; }
		public Parent? Mother { get; set; }
	}

	public struct FamilySync<SM> : 
		ISyncObject<SM, Family>, 
		ISyncObject<SM, Parent>, 
		ISyncObject<SM, Child>
		where SM : ISyncManager
	{
		ObjectMode _listMode;
		public FamilySync(ObjectMode listMode) => _listMode = listMode;

		public Family Sync(SM sync, Family? obj)
		{
			obj ??= new Family();
			obj.Parents = sync.SyncList("Parents", obj.Parents, this, ObjectMode.Deduplicate, _listMode);
			obj.Children = sync.SyncList("Children", obj.Children, this, ObjectMode.Deduplicate, _listMode);
			return obj;
		}

		public Parent Sync(SM sm, Parent? obj)
		{
			obj ??= new Parent();
			obj.Name = sm.Sync("Name", obj.Name);
			obj.Children = sm.SyncList("Children", obj.Children, this, ObjectMode.Deduplicate, _listMode);
			return obj;
		}

		public Child Sync(SM sm, Child? child)
		{
			sm.CurrentObject = child ??= new Child();
			child.Name = sm.Sync("Name", child.Name);
			child.Father = sm.Sync("Father", child.Father, this, ObjectMode.Deduplicate);
			child.Mother = sm.Sync("Mother", child.Mother, this, ObjectMode.Deduplicate);
			return child;
		}
	}
}
