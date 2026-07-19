using Benchmark;
using Benchmark.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();
// EzDataPoint keeps its data in public fields; include them in API responses
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.IncludeFields = true);

// Build the benchmark tree
var registry = new BenchmarkRegistry();
Benchmark.Serialization.SerializationSuite.Register(registry);
LegacySuite.Register(registry);
builder.Services.AddSingleton(registry);
builder.Services.AddSingleton(services => new BenchmarkQueueService(registry,
	Path.Combine(builder.Environment.ContentRootPath, "BenchmarkResults")));

var app = builder.Build();

app.UseDeveloperExceptionPage(); // it's a localhost tool; full errors are useful
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

BenchmarkApi.Map(app);

// Instantiate the queue service eagerly so persisted results load at startup
_ = app.Services.GetRequiredService<BenchmarkQueueService>();

app.Run();
