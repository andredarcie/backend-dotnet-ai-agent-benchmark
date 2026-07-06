using System.ComponentModel.DataAnnotations;

namespace CreditCardApi.Application.Common;

/// <summary>Standard page-based pagination parameters accepted by all collection endpoints.</summary>
public sealed record PaginationQuery
{
    /// <summary>Largest page size a client may request.</summary>
    public const int MaxPageSize = 100;

    /// <summary>1-based page number. Defaults to the first page.</summary>
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    /// <summary>Number of items per page (1–100). Defaults to 20.</summary>
    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = 20;
}
