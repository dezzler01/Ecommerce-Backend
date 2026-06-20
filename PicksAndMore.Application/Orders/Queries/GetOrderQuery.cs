using MediatR;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Application.Mappings;

namespace PicksAndMore.Application.Orders.Queries;

public record GetOrderQuery(Guid OrderId) : IRequest<ApiResponse<OrderDto>>;

public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, ApiResponse<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<ApiResponse<OrderDto>> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId);
        if (order == null)
        {
            return ApiResponse<OrderDto>.Failure(null, "Order not found.");
        }

        return ApiResponse<OrderDto>.Success(order.ToDto(), "Order retrieved successfully.");
    }
}
