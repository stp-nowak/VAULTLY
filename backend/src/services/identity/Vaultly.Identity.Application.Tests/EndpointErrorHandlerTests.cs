using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using Serilog;
using Serilog.Core;
using Serilog.Events;

using Vaultly.Identity.Api;
using Vaultly.SharedKernel;

namespace Vaultly.Identity.Application.Tests;

[TestFixture]
public sealed class EndpointErrorHandlerTests
{
    [Test]
    public async Task ExecuteAsync_WhenDomainExceptionIsThrown_LogsWarningAndReturnsBadRequest()
    {
        var sink = new CollectingSink();
        var handler = CreateHandler(sink);
        var httpContext = CreateHttpContext("POST", "/token");

        var result = await handler.ExecuteAsync(
            httpContext,
            "POST /token",
            () => throw new TestDomainException("Auth code is invalid."));

        var response = await ExecuteResultAsync(result);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(response.Body.GetProperty("error").GetString(), Is.EqualTo("Auth code is invalid."));
            Assert.That(sink.Events, Has.Count.EqualTo(1));
            Assert.That(sink.Events[0].Level, Is.EqualTo(LogEventLevel.Warning));
            Assert.That(sink.Events[0].RenderMessage(), Does.Contain("POST /token"));
            Assert.That(sink.Events[0].RenderMessage(), Does.Contain("bad request"));
        });
    }

    [Test]
    public async Task ExecuteAsync_WhenUnexpectedExceptionIsThrown_LogsErrorAndReturnsProblem()
    {
        var sink = new CollectingSink();
        var handler = CreateHandler(sink);
        var httpContext = CreateHttpContext("GET", "/google/callback");

        var result = await handler.ExecuteAsync(
            httpContext,
            "GET /google/callback",
            () => throw new Exception("boom"));

        var response = await ExecuteResultAsync(result);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
            Assert.That(response.Body.GetProperty("title").GetString(), Is.EqualTo("An unexpected error occurred."));
            Assert.That(sink.Events, Has.Count.EqualTo(1));
            Assert.That(sink.Events[0].Level, Is.EqualTo(LogEventLevel.Error));
            Assert.That(sink.Events[0].RenderMessage(), Does.Contain("GET /google/callback"));
        });
    }

    [Test]
    public async Task Unauthorized_WhenCalled_LogsWarningAndReturnsUnauthorized()
    {
        var sink = new CollectingSink();
        var handler = CreateHandler(sink);
        var httpContext = CreateHttpContext("POST", "/refresh");

        var result = handler.Unauthorized(httpContext, "POST /refresh", "Refresh token cookie is missing.");
        var response = await ExecuteResultAsync(result);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
            Assert.That(sink.Events, Has.Count.EqualTo(1));
            Assert.That(sink.Events[0].Level, Is.EqualTo(LogEventLevel.Warning));
            Assert.That(sink.Events[0].RenderMessage(), Does.Contain("Refresh token cookie is missing."));
        });
    }

    /// <summary>
    /// Creates a handler backed by a sink so tests can verify emitted log events.
    /// </summary>
    private static EndpointErrorHandler CreateHandler(CollectingSink sink)
    {
        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(sink)
            .CreateLogger();

        return new EndpointErrorHandler(logger);
    }

    /// <summary>
    /// Creates an HTTP context with a writable response body for result execution.
    /// </summary>
    private static DefaultHttpContext CreateHttpContext(string method, string path)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        httpContext.Request.Path = path;
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestServices = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .ConfigureHttpJsonOptions(_ => { })
            .BuildServiceProvider();
        return httpContext;
    }

    /// <summary>
    /// Executes an ASP.NET result and parses the JSON response payload.
    /// </summary>
    private static async Task<(int StatusCode, JsonElement Body)> ExecuteResultAsync(IResult result)
    {
        var httpContext = CreateHttpContext("GET", "/test");
        await result.ExecuteAsync(httpContext);

        httpContext.Response.Body.Position = 0;
        await using var bodyStream = new MemoryStream();
        await httpContext.Response.Body.CopyToAsync(bodyStream);
        var json = bodyStream.Length == 0
            ? "{}"
            : Encoding.UTF8.GetString(bodyStream.ToArray());

        using var document = JsonDocument.Parse(json);
        return (httpContext.Response.StatusCode, document.RootElement.Clone());
    }

    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent)
        {
            Events.Add(logEvent);
        }
    }

    private sealed class TestDomainException(string message) : DomainException(message);
}


