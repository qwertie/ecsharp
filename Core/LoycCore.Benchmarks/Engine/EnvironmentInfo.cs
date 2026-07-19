using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Benchmark
{
	/// <summary>Gathers machine/runtime facts that affect benchmark results.</summary>
	public static class EnvironmentInfo
	{
		public static IEnumerable<(string Name, string Value)> Gather()
		{
			yield return ("OS", RuntimeInformation.OSDescription);
			yield return ("CPU", FindCpuModel() ?? "unknown");
			yield return ("Logical cores", Environment.ProcessorCount.ToString());
			yield return ("Runtime", RuntimeInformation.FrameworkDescription);
			yield return ("Architecture", RuntimeInformation.ProcessArchitecture.ToString());
			yield return ("GC mode", (System.Runtime.GCSettings.IsServerGC ? "Server" : "Workstation")
				+ ", " + System.Runtime.GCSettings.LatencyMode);
			yield return ("Build", "benchmarks: " + DescribeBuild(typeof(EnvironmentInfo).Assembly)
				+ "; Loyc.Essentials: " + DescribeBuild(typeof(Loyc.SyncLib.SyncJson).Assembly));
			if (Debugger.IsAttached)
				yield return ("WARNING", "A debugger is attached — timings will be misleading");
		}

		/// <summary>True if any relevant assembly was built without optimizations.</summary>
		public static bool IsDebugBuild =>
			IsUnoptimized(typeof(EnvironmentInfo).Assembly) || IsUnoptimized(typeof(Loyc.SyncLib.SyncJson).Assembly);

		static bool IsUnoptimized(Assembly asm)
			=> asm.GetCustomAttribute<DebuggableAttribute>()?.IsJITOptimizerDisabled ?? false;

		static string DescribeBuild(Assembly asm)
			=> IsUnoptimized(asm) ? "DEBUG (unoptimized!)" : "optimized";

		static string? FindCpuModel()
		{
			try {
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
					foreach (var line in File.ReadLines("/proc/cpuinfo"))
						if (line.StartsWith("model name"))
							return line.Substring(line.IndexOf(':') + 1).Trim();
				}
				return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
			} catch {
				return null;
			}
		}
	}
}
