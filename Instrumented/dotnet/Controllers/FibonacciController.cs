using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Trace;

namespace dotnet.Controllers;

[ApiController]
[Route("[controller]")]
public class FibonacciController : ControllerBase
{
    // Create a Tracer
    public const string ActivitySourceName = "FibonacciService";
    private ActivitySource activitySource = new ActivitySource(ActivitySourceName);

    // Create a Meter
    public const string MeterName = "FibonacciMeter";
    private Meter meter = new Meter(MeterName);

    // Create a Logger
    private readonly ILogger<FibonacciController> _logger;

    public FibonacciController(ILogger<FibonacciController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get(long n)
    {
        try
        {
            return Ok(new {
                n = n,
                result = Fibonacci(n)
            });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private long Fibonacci(long n)
    {
        // Start a new span using your tracer
        using var activity = activitySource.StartActivity(nameof(Fibonacci));

        // Add a span attribute to capture user input
        activity?.SetTag("fibonacci.n", n);

        // Create a custom counter
        Counter<long> counter = meter.CreateCounter<long>("FibonacciMeter.MyCounter");

        // If user input ('n') is invalid, throw an `ArgumentOutofRangeException, 
        // record an exception as an event on the span, 
        // and set the span’s status to `Error`
        try
        {
            ThrowIfOutOfRange(n);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            activity?.SetStatus(Status.Error.WithDescription(ex.Message));
            activity?.RecordException(ex);
            throw;
        }

        var result = 0L;
        if (n <= 2)
        {
            result = 1;
        }
        else
        {
            var a = 0L;
            var b = 1L;

            for (var i = 1; i < n; i++)
            {
                result = checked(a + b);
                a = b;
                b = result;
            }
        }
        // Add a span attribute to capture the result
        // if the computation was successful
        activity?.SetTag("fibonacci.result", result);
        // Increment the counter for every successful computation
        counter.Add(1);
        return result;
    }

    private void ThrowIfOutOfRange(long n)
    {

        if (n < 1 || n > 90)
        {
            _logger.LogInformation("The invalid input was {count}", n);
            throw new ArgumentOutOfRangeException(nameof(n), n, "n must be between 1 and 90");
        }
    }
}
