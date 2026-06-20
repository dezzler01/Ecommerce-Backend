using PicksAndMore.Application.Common;
using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<PaginationResult<Product>> GetPagedProductsAsync(ProductQueryParameters queryParams);
    Task AddAsync(Product product);
    Task BulkAddAsync(IEnumerable<Product> products);
    Task DeleteAsync(Product product);
}
