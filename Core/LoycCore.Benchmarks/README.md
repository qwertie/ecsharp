# LoycCore.Benchmarks

A Blazor Server web app for running and charting Loyc benchmarks on localhost.
It replaces the old WinForms/OxyPlot benchmark app.

```
cd Core/LoycCore.Benchmarks
dotnet run -c Release
```

Then open the printed URL (e.g. http://localhost:5000). **Always benchmark
Release builds** — the app shows a warning banner if it detects an unoptimized
build. The app disables tiered compilation and uses non-concurrent workstation
GC for stable measurements.

## Using the UI

- The left sidebar shows the **benchmark tree**. Tick individual benchmarks or a
  whole group, then press **Run selected**; or press **Run all**.
- Queued benchmarks run strictly one at a time (concurrent benchmarks would
  corrupt each other's timings). The queue panel shows progress and lets you
  cancel.
- Click a benchmark name to see its results: interactive charts (ApexCharts),
  the run log, and CSV/JSON export links.
- Results are persisted in `BenchmarkResults/*.json` (git-ignored) and reload
  on the next start, so you can compare charts without re-running.

## REST API (for scripting)

```
GET  /api/benchmarks              list benchmark paths
POST /api/queue {"paths":["..."]} queue benchmarks ("*" = everything)
GET  /api/status                  queue/job status
POST /api/cancel                  cancel current + clear queue
GET  /api/result?path=...         full result as JSON
GET  /api/result.csv?path=...     measurements as CSV
```

## What's here

Benchmarks are grouped by the assembly they exercise (Loyc.SyncLib is listed
as its own group because it will eventually become its own assembly):

- **Loyc.SyncLib** — SyncJson/SyncBinary vs System.Text.Json, Newtonsoft.Json,
  BinaryFormatter, protobuf-net and MessagePack, over several data shapes: the
  Calendar example from the SyncLib home page
  (`Core/Tests/SyncLib/HomePageCalendarExample.cs`), *Objects & dictionaries*
  (small-object lists, string dictionaries and one wide flat object on one set
  of charts), *Deep nesting*, and *Primitive arrays*. Each group is a single
  benchmark leaf that runs both the serialize and deserialize passes. Every
  adapter is round-trip-validated before it is timed (which also serves as its
  warm-up); serializers that can't handle a scenario are reported in the log
  instead of charted. Results are shown as grouped, stacked bar charts of
  median times normalized per item: one bar per serializer per case, split
  into write (bottom) and read (top) segments, with a companion allocations
  chart and an (unstacked) payload-size chart. SyncLib series are labeled with
  the API being measured (e.g. `SyncJson.Write`, `SyncJson.WriteI
  (ISyncManager)`) and drawn in greens/cyan; the others use red/blue/purple
  tones.
- **Loyc.Collections** — the classic AList/DList/etc. benchmarks (previously
  OxyPlot charts) plus the hashtrees (InternalSet) console benchmark.
- **Loyc.Essentials / Loyc.Math / Loyc.Utilities / .NET runtime** — the old
  console micro-benchmarks (thread-local storage, convex hull, CPTrie,
  GoInterface, LINQ overhead, byte-array access, ...), with their console
  output captured into the result log.

## Adding a benchmark

Register a leaf in a suite (see `Suites/`): give it a slash-separated path
(which defines the tree hierarchy) and an `Action<BenchmarkContext>`. Inside,
use `MicroBench.Measure` for timing, `ctx.Add(new EzDataPoint {...})` to plot
points (same GraphId ⇒ same chart; string `Parameter` ⇒ bar chart, numeric ⇒
line chart), `ctx.ConfigureGraph` for axis titles/log scale, `ctx.Log` for
text, and `ctx.Progress` for the progress bar (it also throws when the user
cancels). For a new serialization scenario, create a `Scenario<T>` in
`Suites/Serialization/SerializationSuite.cs` — the harness handles the
timing/validation/charting for all serializers.
