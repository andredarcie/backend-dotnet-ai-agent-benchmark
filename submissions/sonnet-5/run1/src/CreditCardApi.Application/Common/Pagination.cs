namespace CreditCardApi.Application.Common;

/// <summary>Clamps caller-supplied paging parameters to sane bounds shared by every collection endpoint.</summary>
public static class Pagination
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static (int PageNumber, int PageSize) Normalize(int pageNumber, int pageSize) =>
        (pageNumber < 1 ? 1 : pageNumber,
         pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize));
}
