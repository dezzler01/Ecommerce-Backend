using MediatR;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Application.Mappings;

namespace PicksAndMore.Application.Orders.Commands;

public record ReturnOrderItemsCommand(Guid OrderId, List<OrderItemReturnDto> Returns) : IRequest<ApiResponse<OrderDto>>;

public class ReturnOrderItemsCommandHandler : IRequestHandler<ReturnOrderItemsCommand, ApiResponse<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReturnOrderItemsCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<OrderDto>> Handle(ReturnOrderItemsCommand request, CancellationToken cancellationToken)
    {
        if (request.Returns == null || !request.Returns.Any())
        {
            return ApiResponse<OrderDto>.Failure(null, "No items specified for return.");
        }

        // 1. Begin database transaction
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // 2. Fetch the target order
            var order = await _orderRepository.GetByIdAsync(request.OrderId);
            if (order == null)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<OrderDto>.Failure(null, "Order not found.");
            }

            // 3. Process each return item
            foreach (var returnInfo in request.Returns)
            {
                if (returnInfo.ReturnedQuantity <= 0)
                {
                    continue;
                }

                // Locate the order item
                var orderItem = order.Items.FirstOrDefault(i => i.ProductId == returnInfo.ProductId);
                if (orderItem == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<OrderDto>.Failure(null, $"Item with Product ID '{returnInfo.ProductId}' not found in order.");
                }

                if (returnInfo.ReturnedQuantity > orderItem.Quantity)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<OrderDto>.Failure(null, $"Cannot return {returnInfo.ReturnedQuantity} items of Product ID '{returnInfo.ProductId}'. Only {orderItem.Quantity} remains in the order.");
                }

                // Update return flags and quantities
                orderItem.IsReturnedPartially = true;
                orderItem.Quantity -= returnInfo.ReturnedQuantity;

                // Restock back to product inventory
                var product = await _productRepository.GetByIdAsync(returnInfo.ProductId);
                if (product != null)
                {
                    product.StockQuantity += returnInfo.ReturnedQuantity;
                }
            }

            // 4. Recalculate and update the order total price
            order.RecalculateTotalPrice();

            // 5. Save and commit transaction
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Fetch refreshed order to return mapped data
            var updatedOrder = await _orderRepository.GetByIdAsync(order.Id);

            var resultDto = updatedOrder != null ? updatedOrder.ToDto() : order.ToDto();
            return ApiResponse<OrderDto>.Success(resultDto, "Order items partially returned and restocked successfully.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return ApiResponse<OrderDto>.Failure(null, $"An error occurred during partial returns processing: {ex.Message}");
        }
    }
}
