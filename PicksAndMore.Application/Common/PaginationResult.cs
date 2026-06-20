namespace PicksAndMore.Application.Common;

public class PaginationResult<T>
{
    public IReadOnlyCollection<T> Items { get; }
    public PaginationMetadata Metadata { get; }

    public PaginationResult(IReadOnlyCollection<T> items, int totalCount, int currentPage, int pageSize)
    {
        Items = items;
        Metadata = new PaginationMetadata(currentPage, pageSize, totalCount);
    }
}

public class PaginationMetadata
{
    public int CurrentPage { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages { get; }

    public PaginationMetadata(int currentPage, int pageSize, int totalCount)
    {
        CurrentPage = currentPage;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    }
}
