namespace TechChallenge.Web.Domain;

/// <summary>
/// Wraps a paginated query result with metadata for UI rendering.
/// </summary>
public record PagedResult<T>(
    List<T> Items,
    int     TotalCount,
    int     Page,
    int     PageSize)
{
    public int  TotalPages  => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext     => Page < TotalPages;
}