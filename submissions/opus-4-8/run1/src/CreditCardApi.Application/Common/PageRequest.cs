namespace CreditCardApi.Application.Common;

/// <summary>A validated, clamped pagination request derived from raw query parameters.</summary>
public sealed record PageRequest
{
    /// <summary>Largest page size a client may request.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Default page size when none is supplied.</summary>
    public const int DefaultPageSize = 20;

    private PageRequest(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    /// <summary>1-based page number.</summary>
    public int Page { get; }

    /// <summary>Number of items per page.</summary>
    public int PageSize { get; }

    /// <summary>Number of rows to skip for this page.</summary>
    public int Skip => (Page - 1) * PageSize;

    /// <summary>Builds a request from optional query values, clamping to safe bounds.</summary>
    public static PageRequest From(int? page, int? pageSize)
    {
        var safePage = page is null or < 1 ? 1 : page.Value;
        var safeSize = pageSize switch
        {
            null or < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => pageSize.Value,
        };
        return new PageRequest(safePage, safeSize);
    }
}
