using dotnet.Controllers;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Define an OpenTelemetry resource 
// A resource represents a collection of attributes describing the
// service. This collection of attributes will be associated with all
// telemetry generated from this service (traces, metrics, logs).
var resourceBuilder = ResourceBuilder
    .CreateDefault()
    .AddService("getting-started-dotnet")
    .AddTelemetrySdk();

// Configure the OpenTelemetry SDK for traces
builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
{
   tracerProviderBuilder
       .SetResourceBuilder(resourceBuilder)
       .AddSource(FibonacciController.ActivitySourceName)
       .AddAspNetCoreInstrumentation()
       .AddOtlpExporter((options =>
        {
            options.Endpoint = new Uri($"{Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")}");
            options.Headers = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS");
        }));
});

// Configure the OpenTelemetry SDK for metrics
builder.Services.AddOpenTelemetryMetrics(meterProviderBuilder =>
{
    meterProviderBuilder
        .SetResourceBuilder(resourceBuilder)
        .AddRuntimeInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter(FibonacciController.MeterName)
        .AddOtlpExporter((exporterOptions, metricReaderOptions) =>
        {
            exporterOptions.Endpoint = new Uri($"{Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")}");
            exporterOptions.Headers = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS");
            metricReaderOptions.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
        });

});

// Configure the OpenTelemetry SDK for logs
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.ParseStateValues = true;
    options.IncludeScopes = true;
    options.SetResourceBuilder(resourceBuilder)
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri($"{Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")}");
            options.Headers = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS");
        });
});

var app = builder.Build();

app.MapControllers();

app.Run();
