using MediatR;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Application.Mappings;

namespace PicksAndMore.Application.Orders.Queries;

public record GetOrdersQuery(GetOrdersQueryDto QueryParams) : IRequest<ApiResponse<PaginationResult<OrderDto>>>;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, ApiResponse<PaginationResult<OrderDto>>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<ApiResponse<PaginationResult<OrderDto>>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var pagedResult = await _orderRepository.GetPagedOrdersAsync(request.QueryParams);

        var dtoItems = pagedResult.Items.Select(o => o.ToDto()).ToList();

        var dtoPagedResult = new PaginationResult<OrderDto>(
            dtoItems,
            pagedResult.Metadata.TotalCount,
            pagedResult.Metadata.CurrentPage,
            pagedResult.Metadata.PageSize
        );

        return ApiResponse<PaginationResult<OrderDto>>.Success(dtoPagedResult, "Orders fetched successfully.");
    }
}
