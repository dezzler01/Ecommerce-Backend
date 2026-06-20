using MediatR;
using Microsoft.AspNetCore.SignalR;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Hubs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Application.Mappings;
using PicksAndMore.Domain.Enums;

namespace PicksAndMore.Application.Orders.Commands;

public record UpdateOrderStatusCommand(Guid OrderId, OrderStatus TargetStatus) : IRequest<ApiResponse<OrderDto>>;

public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, ApiResponse<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHubContext<NotificationHub> _hubContext;

    public UpdateOrderStatusCommandHandler(
        IOrderRepository orderRepository, 
        IUnitOfWork unitOfWork,
        IHubContext<NotificationHub> hubContext)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _hubContext = hubContext;
    }

    public async Task<ApiResponse<OrderDto>> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId);
        if (order == null)
        {
            return ApiResponse<OrderDto>.Failure(null, "Order not found.");
        }

        order.UpdateStatus(request.TargetStatus);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Map TargetStatus to a user-friendly text token (Confirmed, Out For Delivery, Delivered)
        string statusText = request.TargetStatus switch
        {
            OrderStatus.PendingVerification => "Pending Verification",
            OrderStatus.ConfirmedPreparing => "Confirmed",
            OrderStatus.OutForDelivery => "Out For Delivery",
            OrderStatus.Delivered => "Delivered",
            OrderStatus.ReturnedRejected => "Returned/Rejected",
            _ => request.TargetStatus.ToString()
        };

        // Route a live targeted user-specific toast notification message matching the updated lifecycle token
        var orderNumber = $"ORD-{order.Id.ToString().Substring(0, 8).ToUpper()}";
        await _hubContext.Clients.User(order.UserId.ToString()).SendAsync("ReceiveNotification", new
        {
            type = "OrderStatusChanged",
            orderId = order.Id,
            orderNumber = orderNumber,
            status = statusText,
            message = $"Your order {orderNumber} status has been updated to: {statusText}."
        }, cancellationToken);

        // Fetch refreshed order to map navigation properties
        var refreshedOrder = await _orderRepository.GetByIdAsync(order.Id);

        var resultDto = refreshedOrder != null ? refreshedOrder.ToDto() : order.ToDto();
        return ApiResponse<OrderDto>.Success(resultDto, $"Order status transitioned to '{request.TargetStatus}'.");
    }
}
