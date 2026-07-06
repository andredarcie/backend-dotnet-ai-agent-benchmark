using System.Globalization;
using CreditCardApi.Application.Common;

namespace CreditCardApi.Api.Http;

public static class PaginationHeaderExtensions
{
    public static void AddPaginationHeaders<T>(this HttpResponse response, PagedResult<T> pagedResult)
    {
        response.Headers["X-Total-Count"] = pagedResult.TotalCount.ToString(CultureInfo.InvariantCulture);
        response.Headers["X-Page"] = pagedResult.Page.ToString(CultureInfo.InvariantCulture);
        response.Headers["X-Page-Size"] = pagedResult.PageSize.ToString(CultureInfo.InvariantCulture);
        response.Headers["X-Total-Pages"] = pagedResult.TotalPages.ToString(CultureInfo.InvariantCulture);
    }
}
