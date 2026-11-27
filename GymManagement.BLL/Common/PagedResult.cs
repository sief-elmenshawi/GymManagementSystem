namespace GymManagement.BLL.Common;

/// <summary>
/// Paging metadata shared by paged list views.
/// </summary>
public sealed class PagingInfo
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    public int TotalPages => PageSize <= 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

/// <summary>
/// A single page of results plus the metadata needed to render a pager.
/// </summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public PagingInfo Paging { get; init; } = new();
}
