using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace Loyc.Essentials.Tests
{
	[TestFixture]
	public class UStringTests : Assert
	{
		[Test]
		public void StartsWith()
		{
			IsTrue(new UString("hello").StartsWith("hell"));
			IsFalse(new UString("hello", 1).StartsWith("hell"));
			IsTrue(new UString("hello", 1).StartsWith(""));
			IsTrue(new UString("hello", 1).StartsWith("e"));
			IsTrue(new UString("hello", 2).StartsWith("llo"));
		}

		[Test]
		public void ChangeCase()
		{
			AreEqual(new UString("Hello There!!", 3, 9).ToUpper(), (UString)"LO THERE!");
			string s = "!HELLO THERE!";
			AreEqual(new UString(s, 1, 11).ToUpper(), (UString)"HELLO THERE");
			AreEqual(new UString(s, 1, 11).ToUpper().InternalString, s);
			s = "!HELLO THERE, Sailor Joe Fitzgeraldsteinenberg!";
			AreEqual(new UString(s, 1, 4).ToUpper().InternalString, "HELL");
			AreEqual(new UString("ĈĥáráĉtérŜ!").ToUpper(), (UString)"ĈĤÁRÁĈTÉRŜ!");
		}

		[Test]
		public void Find()
		{
			UString eek = "eekeekeekeek".Slice(3);
			AreEqual(eek.Find("eekee".Slice(0, 3)), (UString)"eekeekeek");
			AreEqual(eek.Find("eekee".Slice(2, 3)), (UString)"keekeek");
			AreEqual(eek.Find("KEE", false), (UString)"");
			AreEqual(eek.Find("KEE", true), (UString)"keekeek");
			UString nowaldo = "1234567890waldo!".Slice(1, 13);
			AreEqual(nowaldo.Find("waldo"), (UString)"");
			AreEqual(nowaldo.Find("waldo").InternalStart, nowaldo.InternalStop);
			UString waldo = "1234567890waldo!".Slice(1, 14);
			AreEqual(waldo.Find("waldo"), (UString)"waldo");
			AreEqual(waldo.Find("waldo").InternalStart, 10);
		}

		[Test]
		public void Replace()
		{
			UString no = "__no__".Slice(2, 2), yes = "_Yes___".Slice(1, 3);
			UString No = "__No__".Slice(2, 2), nos = "noNoNoNoNoN_".Slice(1, 9);
			AreEqual(nos.ReplaceOne(no, yes, false), nos);
			AreEqual(nos.ReplaceOne(No, yes, false), (UString)"oYesNoNoNo");
			AreEqual(nos.Replace(No, yes, false, 2), (UString)"oYesYesNoNo");
			AreEqual(nos.ReplaceOne(no, yes, true), (UString)"oYesNoNoNo");
			AreEqual(nos.Replace(no, yes, true, 3), (UString)"oYesYesYesNo");
			AreEqual(nos.Replace("NoN", "oN", false), (UString)"ooNooNo");
			AreEqual(nos.Replace(no, yes, false), (UString)"oNoNoNoNo");
			AreEqual(nos.Replace(no, yes, true), (UString)"oYesYesYesYes");
		}
	}
}
