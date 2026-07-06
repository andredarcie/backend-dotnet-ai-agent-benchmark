using System;
using System.Collections.Generic;

namespace CreditCardApi.Application.DTOs;

/// <summary>
/// A standardized wrapper for paginated collections returned by the API.
/// </summary>
public class PagedResponse<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public IEnumerable<T> Items { get; set; } = null!;

    public PagedResponse() { }

    public PagedResponse(IEnumerable<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}
