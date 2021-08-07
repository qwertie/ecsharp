using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib.Tests
{
	public class Person
	{
		public string? Name { get; set; }
		public int Age;
		public Person[]? Siblings { get; set; }
	}
}
