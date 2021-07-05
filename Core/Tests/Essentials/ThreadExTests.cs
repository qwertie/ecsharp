using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;
using System.Threading;
using Loyc.Threading;

namespace Loyc.Essentials.Tests
{
	[TestFixture]
	public class ThreadExTests
	{
		[Test]
		public void BasicChecks()
		{
			ThreadLocalVariable<int> threadVar = new ThreadLocalVariable<int>(123);
			Thread parent = Thread.CurrentThread;
			bool eventOccurred = false;
			bool valueOk = true, eventOk = true;
			bool stop = false;
			bool started = false;

			ThreadEx t = new ThreadEx(delegate(object o)
			{
				started = true;
				try
				{
					if ((int)o != 123 || threadVar.Value != 123)
						valueOk = false;
				}
				catch
				{
					valueOk = false;
				}
				while (!stop)
					GC.KeepAlive(""); // Waste time
				started = false;
			});

			EventHandler<ThreadStartEventArgs> eh = null!;
			ThreadEx.ThreadStarting += (eh = delegate(object o, ThreadStartEventArgs e)
			{
				eventOccurred = true;
				if (e.ChildThread != t || e.ParentThread != parent)
					eventOk = false;
				ThreadEx.ThreadStarting -= eh;
			});

			Assert.IsFalse(t.IsAlive);
			Assert.AreEqual(System.Threading.ThreadState.Unstarted, t.ThreadState);
			t.Start(123);
			Assert.IsTrue(t.IsAlive);
			Assert.IsTrue(eventOccurred);
			Assert.IsTrue(eventOk);
			while (!started)
				ThreadEx.Sleep(0);
			Assert.AreEqual(System.Threading.ThreadState.Running, t.ThreadState);
			stop = true;
			Assert.IsTrue(t.Join(5000));
			Assert.IsTrue(valueOk);
			Assert.IsFalse(started);
		}
	}
}
