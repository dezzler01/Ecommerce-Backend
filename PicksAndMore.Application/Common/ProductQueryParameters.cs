namespace PicksAndMore.Application.Common;

public record ProductQueryParameters(
    string? TextTerm = null,
    string? SortBy = null,
    bool IsSortAscending = true,
    int Page = 1,
    int PageSize = 10,
    string? CollectionType = null,
    Guid? BrandId = null
);
