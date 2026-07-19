using System.Text;

namespace Benchmark
{
	/// <summary>Redirects Console.Out/Error into a benchmark's log while a legacy
	/// console benchmark runs. Safe because the queue runs benchmarks one at a time.</summary>
	public static class ConsoleCapture
	{
		public static void Run(BenchmarkContext ctx, Action action)
		{
			var oldOut = Console.Out;
			var oldError = Console.Error;
			var writer = new LineWriter(ctx);
			try {
				Console.SetOut(writer);
				Console.SetError(writer);
				action();
			} finally {
				writer.FlushPartialLine();
				Console.SetOut(oldOut);
				Console.SetError(oldError);
			}
		}

		class LineWriter : TextWriter
		{
			public LineWriter(BenchmarkContext ctx) => _ctx = ctx;
			readonly BenchmarkContext _ctx;
			readonly StringBuilder _line = new();

			public override Encoding Encoding => Encoding.UTF8;

			public override void Write(char c)
			{
				if (c == '\n') {
					_ctx.Log(_line.ToString().TrimEnd('\r'));
					_line.Clear();
					// Legacy console benchmarks never check the cancellation token, so
					// every completed output line is a cancellation point instead.
					// (FlushPartialLine, on the finally path, deliberately does not
					// check the token — it must not throw during exception unwind.)
					_ctx.CancellationToken.ThrowIfCancellationRequested();
				} else
					_line.Append(c);
			}
			public override void Write(string? s)
			{
				foreach (char c in s ?? "")
					Write(c);
			}
			public void FlushPartialLine()
			{
				if (_line.Length > 0) {
					_ctx.Log(_line.ToString());
					_line.Clear();
				}
			}
		}
	}
}
