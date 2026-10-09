using System.Diagnostics.CodeAnalysis;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;

public sealed class PagedResult<TObject>
{
    public required IEnumerable<TObject> Items { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    [SetsRequiredMembers]
    public PagedResult(IReadOnlyCollection<TObject> items, int count, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        Items = items;
    }

    /// <summary>La misma página con cada elemento transformado.</summary>
    public PagedResult<TResult> Select<TResult>(Func<TObject, TResult> selector) =>
        new(Items.Select(selector).ToList(), TotalCount, PageNumber, PageSize);
}
