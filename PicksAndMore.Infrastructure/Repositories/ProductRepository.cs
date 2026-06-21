using Microsoft.EntityFrameworkCore;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Infrastructure.Persistence;

namespace PicksAndMore.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Metadata)
            .Include(p => p.Images)
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PaginationResult<Product>> GetPagedProductsAsync(ProductQueryParameters queryParams)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Metadata)
            .Include(p => p.Images)
            .Include(p => p.Categories)
            .AsQueryable();

        // Brand filtering
        if (queryParams.BrandId.HasValue)
        {
            query = query.Where(p => p.BrandId == queryParams.BrandId.Value);
        }

        // 1. Text filtering (case-insensitive Title / Description search)
        if (!string.IsNullOrWhiteSpace(queryParams.TextTerm))
        {
            var term = queryParams.TextTerm.ToLower();
            query = query.Where(p => 
                p.Title.ToLower().Contains(term) || 
                p.Description.ToLower().Contains(term));
        }

        // 2. Collection Type Filters & Sorts
        if (!string.IsNullOrWhiteSpace(queryParams.CollectionType))
        {
            var collectionType = queryParams.CollectionType.ToLower().Trim();
            if (collectionType == "latest")
            {
                query = query.Where(p => p.CollectionType == "Latest" || string.IsNullOrEmpty(p.CollectionType))
                             .OrderByDescending(p => p.CollectionType == "Latest" ? 1 : 0)
                             .ThenByDescending(p => p.CreatedAt);
            }
            else if (collectionType == "bestsellers")
            {
                query = query.Where(p => p.CollectionType == "Bestsellers" || 
                                         (string.IsNullOrEmpty(p.CollectionType) && _context.OrderItems
                                             .Any(oi => oi.ProductId == p.Id && oi.Order.OrderStatus == PicksAndMore.Domain.Enums.OrderStatus.Delivered)))
                             .OrderByDescending(p => p.CollectionType == "Bestsellers" ? 1 : 0)
                             .ThenByDescending(p => _context.OrderItems
                                 .Where(oi => oi.ProductId == p.Id && oi.Order.OrderStatus == PicksAndMore.Domain.Enums.OrderStatus.Delivered)
                                 .Sum(oi => (int?)oi.Quantity) ?? 0)
                             .ThenByDescending(p => p.CreatedAt);
            }
            else if (collectionType == "featured")
            {
                query = query.Where(p => p.CollectionType == "Featured" || 
                                         (string.IsNullOrEmpty(p.CollectionType) && _context.ProductReviews
                                             .Where(pr => pr.ProductId == p.Id)
                                             .Average(pr => (double?)pr.Rating) >= 4.5))
                             .OrderByDescending(p => p.CollectionType == "Featured" ? 1 : 0)
                             .ThenByDescending(p => _context.ProductReviews
                                 .Where(pr => pr.ProductId == p.Id)
                                 .Average(pr => (double?)pr.Rating) ?? 0);
            }
            else if (collectionType == "on sale" || collectionType == "onsale")
            {
                query = query.Where(p => p.CollectionType == "On Sale" || 
                                         (string.IsNullOrEmpty(p.CollectionType) && p.Price < p.CostPrice))
                             .OrderByDescending(p => p.CollectionType == "On Sale" ? 1 : 0)
                             .ThenByDescending(p => p.CostPrice - p.Price);
            }
        }
        else
        {
            // Fallback to existing sorting logic if collection type not provided
            if (!string.IsNullOrWhiteSpace(queryParams.SortBy))
            {
                var sortBy = queryParams.SortBy.ToLower();
                if (sortBy == "price")
                {
                    query = queryParams.IsSortAscending 
                        ? query.OrderBy(p => p.Price) 
                        : query.OrderByDescending(p => p.Price);
                }
                else if (sortBy == "title")
                {
                    query = queryParams.IsSortAscending 
                        ? query.OrderBy(p => p.Title) 
                        : query.OrderByDescending(p => p.Title);
                }
                else
                {
                    query = query.OrderBy(p => p.Id);
                }
            }
            else
            {
                query = query.OrderBy(p => p.Id);
            }
        }

        // 3. Count Total Items before skipping
        var totalCount = await query.CountAsync();

        // 4. Pagination
        var page = queryParams.Page <= 0 ? 1 : queryParams.Page;
        var pageSize = queryParams.PageSize <= 0 ? 10 : Math.Min(queryParams.PageSize, 100);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginationResult<Product>(items, totalCount, page, pageSize);
    }

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
    }

    public async Task BulkAddAsync(IEnumerable<Product> products)
    {
        await _context.Products.AddRangeAsync(products);
    }

    public Task DeleteAsync(Product product)
    {
        _context.Products.Remove(product);
        return Task.CompletedTask;
    }
}
