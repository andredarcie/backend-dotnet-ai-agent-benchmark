using CreditCardApi.Api.Middleware;
using CreditCardApi.Application.Abstractions;

namespace CreditCardApi.Api.Observability;

public class HttpContextCorrelationIdProvider(IHttpContextAccessor httpContextAccessor) : ICorrelationIdProvider
{
    public string? Current => httpContextAccessor.HttpContext?.Items[CorrelationIdMiddleware.ItemKey] as string;
}
