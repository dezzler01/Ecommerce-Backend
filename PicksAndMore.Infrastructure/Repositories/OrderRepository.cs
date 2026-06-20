using Microsoft.EntityFrameworkCore;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Infrastructure.Persistence;

namespace PicksAndMore.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .Include(o => o.WalletVerification)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
    }

    public async Task<PaginationResult<Order>> GetPagedOrdersAsync(GetOrdersQueryDto queryParams)
    {
        // Build IQueryable with no-tracking for read-only dashboard queries
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .Include(o => o.WalletVerification)
            .AsQueryable();

        // --- Dynamic Filters ---

        // 1. Filter by OrderStatus
        if (queryParams.Status.HasValue)
        {
            query = query.Where(o => o.OrderStatus == queryParams.Status.Value);
        }

        // 2. Filter by Governorate (owned Address value object)
        if (!string.IsNullOrWhiteSpace(queryParams.Governorate))
        {
            var gov = queryParams.Governorate.ToLower();
            query = query.Where(o => o.ShippingAddress.Governorate.ToLower() == gov);
        }

        // 3. Free-text search: customer name, phone number, or order ID
        if (!string.IsNullOrWhiteSpace(queryParams.SearchText))
        {
            var term = queryParams.SearchText.ToLower();
            query = query.Where(o =>
                (o.User != null && o.User.FullName.ToLower().Contains(term)) ||
                (o.User != null && o.User.PhoneNumber != null && o.User.PhoneNumber.Contains(term)) ||
                o.ShippingAddress.PrimaryPhone.Contains(term) ||
                o.Id.ToString().Contains(term));
        }

        // --- Sorting: newest orders first ---
        query = query.OrderByDescending(o => o.OrderDate);

        // --- Count before pagination ---
        var totalCount = await query.CountAsync();

        // --- Pagination ---
        var items = await query
            .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync();

        return new PaginationResult<Order>(items, totalCount, queryParams.PageNumber, queryParams.PageSize);
    }
}
