using System;
using System.Threading;
using System.Threading.Tasks;
using Loyc.MiniTest;
using Loyc.Threading;

namespace Loyc.Essentials.Tests
{
	/// <summary>Tests the ambient (async-local) state of <see cref="MessageSink"/>.</summary>
	[TestFixture]
	public class MessageSinkTests
	{
		[Test]
		public void SetDefaultAffectsDefaultOnSameThread()
		{
			var before = MessageSink.Default;
			var a = new MessageHolder();
			var b = new MessageHolder();
			using (MessageSink.SetDefault(a)) {
				Assert.AreSame(a, MessageSink.Default);
				using (MessageSink.SetDefault(b))
					Assert.AreSame(b, MessageSink.Default);
				Assert.AreSame(a, MessageSink.Default);
			}
			Assert.AreSame(before, MessageSink.Default);
		}

		[Test]
		public void AmbientServiceSupportsValueTypes()
		{
			var amb = new AmbientService<int>(42);
			Assert.AreEqual(42, amb.Value);
			using (amb.Set(7)) {
				Assert.AreEqual(7, amb.Value);
				using (amb.Set(0))  // default(int) is a legal override
					Assert.AreEqual(0, amb.Value);
				Assert.AreEqual(7, amb.Value);
				Assert.AreEqual(42, amb.GlobalDefault);
			}
			Assert.AreEqual(42, amb.Value);
			using (amb.Set(8, alsoSetGlobalDefault: true)) {
				Assert.AreEqual(8, amb.Value);
				Assert.AreEqual(8, amb.GlobalDefault);
			}
			Assert.AreEqual(42, amb.GlobalDefault);
			Assert.AreEqual(42, amb.Value);
		}

		[Test]
		public void ObsoletePushCurrentStillWorks()
		{
			var before = MessageSink.Default;
			var a = new MessageHolder();
			#pragma warning disable 618
			using (var pushed = MessageSink.PushCurrent(a)) {
				Assert.AreSame(before, pushed.OldValue);
				Assert.AreSame(a, MessageSink.Current);
			}
			#pragma warning restore 618
			Assert.AreSame(before, MessageSink.Default);
		}

		[Test]
		public void OverrideFlowsAcrossAwaitAndIsPerContext()
		{
			OverrideFlowsAcrossAwaitAsync().GetAwaiter().GetResult();
		}

		static async Task OverrideFlowsAcrossAwaitAsync()
		{
			var before = MessageSink.Default;
			var sinkA = new MessageHolder();
			var sinkB = new MessageHolder();
			var bInstalled = new TaskCompletionSource<bool>();
			var aChecked = new TaskCompletionSource<bool>();

			using (MessageSink.SetDefault(sinkA)) {
				await Task.Yield();	// the continuation may run on a different thread
				Assert.AreSame(sinkA, MessageSink.Default);

				var taskB = Task.Run(async () => {
					using (MessageSink.SetDefault(sinkB)) {
						bInstalled.SetResult(true);
						await aChecked.Task;
						Assert.AreSame(sinkB, MessageSink.Default);
					}
				});

				await bInstalled.Task;
				// sinkB is the global default now, but this context keeps its own override
				Assert.AreSame(sinkA, MessageSink.Default);
				aChecked.SetResult(true);
				await taskB;
				Assert.AreSame(sinkA, MessageSink.Default);
			}
			Assert.AreSame(before, MessageSink.Default);
		}

		[Test]
		public void SetDefaultAlsoChangesGlobalDefault()
		{
			// Preserves the old "autoFallback" behavior: a thread started before the
			// override has no ambient override of its own, so it sees the global default.
			var before = MessageSink.Default;
			var sink = new MessageHolder();
			IMessageSink? seenDuring = null, seenAfter = null;
			var installed = new ManualResetEventSlim();
			var checkedDuring = new ManualResetEventSlim();
			var disposed = new ManualResetEventSlim();

			var thread = new Thread(() => {
				installed.Wait();
				seenDuring = MessageSink.Default;
				checkedDuring.Set();
				disposed.Wait();
				seenAfter = MessageSink.Default;
			});
			thread.Start();

			using (MessageSink.SetDefault(sink)) {
				installed.Set();
				checkedDuring.Wait();
			}
			disposed.Set();
			thread.Join();

			Assert.AreSame(sink, seenDuring);
			Assert.AreSame(before, seenAfter);
		}

		[Test]
		public void SetContextToStringAffectsFormatMessage()
		{
			Func<object?, string?> f = ctx => "<" + ctx + ">";
			var before = MessageSink.ContextToString;
			using (MessageSink.SetContextToString(f)) {
				Assert.AreSame(f, MessageSink.ContextToString);
				Assert.AreEqual("<ctx>: Error: hi", MessageSink.FormatMessage(Severity.Error, "ctx", "hi"));
			}
			Assert.AreSame(before, MessageSink.ContextToString);
			Assert.AreEqual("ctx: Error: hi", MessageSink.FormatMessage(Severity.Error, "ctx", "hi"));
			// null means "use the default strategy"
			using (MessageSink.SetContextToString(null!))
				Assert.AreEqual("ctx: Error: hi", MessageSink.FormatMessage(Severity.Error, "ctx", "hi"));
		}

		[Test]
		public void ContextToStringFlowsAcrossAwait()
		{
			ContextToStringFlowsAcrossAwaitAsync().GetAwaiter().GetResult();
		}

		static async Task ContextToStringFlowsAcrossAwaitAsync()
		{
			var before = MessageSink.ContextToString;
			Func<object?, string?> f = ctx => "!" + ctx;
			using (MessageSink.SetContextToString(f)) {
				await Task.Yield();
				Assert.AreSame(f, MessageSink.ContextToString);
				await Task.Run(() => Assert.AreSame(f, MessageSink.ContextToString));
			}
			Assert.AreSame(before, MessageSink.ContextToString);
		}
	}
}
