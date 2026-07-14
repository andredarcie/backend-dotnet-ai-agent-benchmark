using CreditCardApi.Api.Middleware;
using CreditCardApi.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CreditCardApi.UnitTests.Infrastructure;

public class GlobalExceptionHandlerTests
{
    private readonly Mock<IProblemDetailsService> _problemDetailsService = new();
    private readonly GlobalExceptionHandler _sut;

    public GlobalExceptionHandlerTests()
    {
        _problemDetailsService
            .Setup(s => s.WriteAsync(It.IsAny<ProblemDetailsContext>()))
            .Returns(ValueTask.CompletedTask);

        _sut = new GlobalExceptionHandler(_problemDetailsService.Object, NullLogger<GlobalExceptionHandler>.Instance);
    }

    [Theory]
    [MemberData(nameof(ExceptionToStatusCodeCases))]
    public async Task TryHandleAsync_MapsExceptionTypeToTheExpectedStatusCode(Exception exception, int expectedStatusCode)
    {
        var httpContext = new DefaultHttpContext();

        var handled = await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_NeverIncludesTheExceptionStackTraceInTheProblemDetails()
    {
        var httpContext = new DefaultHttpContext();
        ProblemDetailsContext? captured = null;
        _problemDetailsService
            .Setup(s => s.WriteAsync(It.IsAny<ProblemDetailsContext>()))
            .Callback<ProblemDetailsContext>(ctx => captured = ctx)
            .Returns(ValueTask.CompletedTask);

        await _sut.TryHandleAsync(httpContext, new InvalidOperationException("boom"), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.DoesNotContain("at CreditCardApi", captured.ProblemDetails.Detail ?? "", StringComparison.Ordinal);
        Assert.DoesNotContain(".cs:line", captured.ProblemDetails.Detail ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public async Task TryHandleAsync_ForValidationException_AttachesTheFieldErrorsExtension()
    {
        var httpContext = new DefaultHttpContext();
        ProblemDetailsContext? captured = null;
        _problemDetailsService
            .Setup(s => s.WriteAsync(It.IsAny<ProblemDetailsContext>()))
            .Callback<ProblemDetailsContext>(ctx => captured = ctx)
            .Returns(ValueTask.CompletedTask);
        var exception = new ValidationException(new Dictionary<string, string[]> { ["amount"] = ["Amount must be greater than 0."] });

        await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.True(captured.ProblemDetails.Extensions.ContainsKey("errors"));
    }

    public static TheoryData<Exception, int> ExceptionToStatusCodeCases() => new()
    {
        { new NotFoundException("not found"), StatusCodes.Status404NotFound },
        { new ValidationException(new Dictionary<string, string[]>()), StatusCodes.Status400BadRequest },
        { new InvalidOperationException("unexpected"), StatusCodes.Status500InternalServerError },
    };
}
