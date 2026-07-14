using CreditCardApi.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace CreditCardApi.UnitTests.Infrastructure;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithNoInboundHeader_GeneratesAndEchoesANewCorrelationId()
    {
        var httpContext = new DefaultHttpContext();
        var sut = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await sut.InvokeAsync(httpContext);

        Assert.True(httpContext.Response.Headers.ContainsKey(CorrelationIdMiddleware.HeaderName));
        var value = httpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        Assert.True(Guid.TryParse(value, out _));
    }

    [Fact]
    public async Task InvokeAsync_WithAnInboundHeader_ReusesTheCallersCorrelationId()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[CorrelationIdMiddleware.HeaderName] = "caller-supplied-id";
        var sut = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await sut.InvokeAsync(httpContext);

        Assert.Equal("caller-supplied-id", httpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }

    [Fact]
    public async Task InvokeAsync_CallsTheNextMiddlewareInThePipeline()
    {
        var httpContext = new DefaultHttpContext();
        var nextCalled = false;
        var sut = new CorrelationIdMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await sut.InvokeAsync(httpContext);

        Assert.True(nextCalled);
    }
}
