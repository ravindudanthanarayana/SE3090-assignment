namespace SmartDesk.Application.Common;

/// <summary>
/// Uniform pagination envelope returned by every list endpoint, so React and (later) Flutter
/// can share a single deserialiser.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>Shared, validated paging/sorting inputs. Bounds are enforced here, not in each controller.</summary>
public class PagedQuery
{
    private const int MaxPageSize = 100;
    private int _page = 1;
    private int _pageSize = 20;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch { < 1 => 20, > MaxPageSize => MaxPageSize, _ => value };
    }

    public string? SortBy { get; set; }
    public string? SortDir { get; set; }

    public bool Descending =>
        string.Equals(SortDir, "desc", StringComparison.OrdinalIgnoreCase);
}
