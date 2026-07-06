namespace CreditCardApi.Application.Common;

/// <summary>One page of a collection, together with the metadata needed to page through it.</summary>
/// <typeparam name="T">Type of the items in the page.</typeparam>
/// <param name="Items">The items on this page.</param>
/// <param name="TotalCount">Total number of items across all pages.</param>
/// <param name="Page">The 1-based page number that was returned.</param>
/// <param name="PageSize">The page size that was applied.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    /// <summary>Total number of pages for <see cref="TotalCount"/> items at <see cref="PageSize"/> per page.</summary>
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Projects each item of the page, preserving the pagination metadata.</summary>
    /// <typeparam name="TOut">Result item type.</typeparam>
    /// <param name="selector">Projection applied to every item.</param>
    /// <returns>A page of projected items with identical metadata.</returns>
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new([.. Items.Select(selector)], TotalCount, Page, PageSize);
}
