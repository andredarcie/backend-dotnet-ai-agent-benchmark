using System.Globalization;
using CreditCardApi.Application.Common;

namespace CreditCardApi.Api.Http;

/// <summary>
/// Exposes pagination metadata as response headers so collection bodies can stay plain arrays.
/// </summary>
internal static class PaginationHeaderExtensions
{
    public static void WritePaginationHeaders<T>(this HttpResponse response, PagedResult<T> page)
    {
        response.Headers["X-Total-Count"] = page.TotalCount.ToString(CultureInfo.InvariantCulture);
        response.Headers["X-Page"] = page.Page.ToString(CultureInfo.InvariantCulture);
        response.Headers["X-Page-Size"] = page.PageSize.ToString(CultureInfo.InvariantCulture);
        response.Headers["X-Total-Pages"] = page.TotalPages.ToString(CultureInfo.InvariantCulture);
    }
}
