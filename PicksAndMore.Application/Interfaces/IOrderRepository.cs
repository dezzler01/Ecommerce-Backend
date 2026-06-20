using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task AddAsync(Order order);
    Task<PaginationResult<Order>> GetPagedOrdersAsync(GetOrdersQueryDto queryParams);
}
