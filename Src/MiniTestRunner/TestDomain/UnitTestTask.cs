using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Loyc.Utilities;
using Loyc.Essentials;
using Loyc.Collections.Linq;
using System.Runtime.Remoting;
using Loyc;

namespace MiniTestRunner.TestDomain
{
	class UnitTestTask : TaskBase
	{
		public MethodInfo Method { get; private set; }
		Lazy<object> _instance;
		public object Instance { get { return _instance == null ? null : _instance.Value; } }
		
		// Test configuration options
		public int? RepeatForMs { get; internal set; }
		public int? MinTrials { get; internal set; }
		int _maxThreads;
		public override int MaxThreads { get { return _maxThreads; } }
		public Type ExpectedException { get; private set; }

		public UnitTestTask(MethodInfo method, Lazy<object> instance, Attribute testAttr, Attribute expectedExceptionAttr, bool isTestSet)
		{
			Method = method;
			_instance = instance;

			_maxThreads = GetPropertyValue<int>(testAttr, "MaxParallelThreads", 0);
			if (_maxThreads <= 0)
				_maxThreads = (GetPropertyValue<bool>(testAttr, "AllowParallel", true) ? int.MaxValue : 1);
			RepeatForMs = GetPropertyValue<int?>(testAttr, "RepeatForMs", null);
			MinTrials = GetPropertyValue<int?>(testAttr, "RepeatForMs", null);
			ExpectedException = GetPropertyValue<Type>(expectedExceptionAttr, "ExpectedException", null);
			IsTestSet = isTestSet;
		}

		public override int Priority { get; set; }

		public override IEnumerable<ITask> RunOnCurrentThread()
		{
			Status = TestStatus.Running;
			LastRunAt = DateTime.Now;
			RunCore();
			return null;
		}
		protected override void RunCore()
		{
			if (!RunAndCatch(() => { var inst = Instance; }, "constructor"))
				return;

			// Try creating a delegate before starting any Stopwatch, so that for
			// benchmarking purposes we avoid measuring the cost of reflection.
			Action invokeTest = null;
			if (Method.ReturnType == typeof(void)) {
				try {
					invokeTest = (Action)Delegate.CreateDelegate(typeof(Action), Instance, Method);
				} catch { }
			}

			// TODO: Setup method

			object result = null;
			Statistic runTimes = null;
			TimeSpan runTime;
			if (MinTrials.HasValue || RepeatForMs.HasValue) {
				int minTrials = MinTrials ?? 0;
				int repeatForMs = RepeatForMs ?? 0;
				int trial = 0;
				runTimes = new Statistic();
				SimpleTimer timer = new SimpleTimer();
				do {
					result = RunOnce(invokeTest, out runTime);
					runTimes.Add(runTime.TotalMilliseconds);
				} while (++trial < minTrials && timer.Millisec < repeatForMs);
				RunTime = TimeSpan.FromMilliseconds(timer.Millisec);
			} else {
				result = RunOnce(invokeTest, out runTime);
				RunTime = runTime;
			}

			if (Status == TestStatus.Running)
				Summary = BuildSummary(result, runTimes);

			// TODO: Teardown method
		}

		private string BuildSummary(object result, Statistic runTimes)
		{
			string msg = "Passed";
			if (runTimes != null && runTimes.Count > 1)
			{
				msg = string.Format("{1} trials; average {2:0.000} seconds, min {3:0.000}, max {4:0.000}, std.dev. {5:0.000})",
					msg, runTimes.Count, runTimes.Avg(), runTimes.Min, runTimes.Max, runTimes.StdDeviation());
			}
			if (result != null)
			{
				Status = TestStatus.SuccessWithMessage;
				msg = string.Format("{0} - {1}", msg, result);
			}
			else
				Status = TestStatus.Success;
			return msg;
		}

		private object RunOnce(Action invokeTest, out TimeSpan runTime)
		{
			object result = null;
			Stopwatch timer = new Stopwatch();
			timer.Start();
			if (invokeTest != null) {
				RunAndCatch(invokeTest, null);
			} else {
				try {
					result = Method.Invoke(Instance, null);
					
					// TODO: handle test suites properly
				
				} catch(TargetInvocationException ex) {
					HandleException(ex, null);
				}
			}
			runTime = timer.Elapsed;
			return result;
		}
		private bool RunAndCatch(Action invokeTest, string setupOrTeardown)
		{
			try {
				invokeTest();
				return true;
			} catch(Exception ex) {
				HandleException(ex, setupOrTeardown);
				return false;
			}
		}

		private void HandleException(Exception exc, string setupOrTeardown)
		{
			string excType = exc.GetType().Name;
			string msg = string.Format("{0}: {1}", exc.GetType().Name, exc.Message);
			if (setupOrTeardown != null) {
				msg = string.Format("During {0}: {1}", setupOrTeardown, msg);
				Status = TestStatus.Error;
			} else if (ExpectedException != null && exc.GetType().IsSubclassOf(ExpectedException)) {
				msg = "As expected, " + msg;
				Status = TestStatus.SuccessWithMessage;
			} else if (excType == "SuccessException") {
				msg = "Passed: " + exc.Message;
				Status = TestStatus.SuccessWithMessage;
			} else if (excType == "IgnoreException")
				Status = TestStatus.Ignored;
			else if (excType == "InconclusiveException")
				Status = TestStatus.Inconclusive;
			else
				Status = TestStatus.Error;
			Summary = msg;
		}

		public bool IsTestSet { get; private set; }

		public override IEnumerable<ITask> Prerequisites(IEnumerable<ITask> concurrentTasks)
		{
			var clash = concurrentTasks.FirstOrDefault(task => {
				// no clash if different AppDomain (we can't access private members across domains anyway)
				if (RemotingServices.IsTransparentProxy(task))
					return false;
				var utt = task as UnitTestTask;
				return utt != null && utt._instance == _instance;
			});
			if (clash == null)
				return null;
			return Iterable.Single(clash);
		}
	}
}
