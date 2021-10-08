using Loyc.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Loyc.SyncLib.Tests
{
	public class Person : IEquatable<Person>
	{
		public string? Name { get; set; }
		public int Age;
		public Person[]? Siblings { get; set; }

		// Equals is called in testing to ensure that the deserialized version matches
		// the original. But it's hard to deep-compare without stack overflow, so compare
		// shalowly.
		public override bool Equals(object obj) => obj is Person p && Equals(p);
		public bool Equals(Person other)
			=> other != null 
			&& Name == other.Name
			&& Age == other.Age
			&& (Siblings == other.Siblings || 
			   (Siblings != null && other.Siblings != null && Siblings.Length == other.Siblings.Length));

		public override int GetHashCode()
			=> (Name?.GetHashCode() ?? 0) ^ Age ^ (Siblings?.SequenceHashCode() ?? 0); 
	}

	public class PersonSync<SM> : ISyncObject<SM, Person> where SM : ISyncManager
	{
		public Person Sync(SM sync, Person? value)
		{
			sync.CurrentObject = value ??= new Person();
			value.Name = sync.Sync(("Name", 1), value.Name);
			value.Age = sync.Sync(("Age", 2), value.Age);
			value.Siblings = sync.SyncList(("Siblings", 3), value.Siblings, this);
			return value;
		}
	}
}
