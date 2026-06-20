using MediatR;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Application.Mappings;
using PicksAndMore.Domain.Enums;

namespace PicksAndMore.Application.Orders.Commands;

public record VerifyWalletCommand(Guid OrderId, bool Approve, string? RejectReason) : IRequest<ApiResponse<OrderDto>>;

public class VerifyWalletCommandHandler : IRequestHandler<VerifyWalletCommand, ApiResponse<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public VerifyWalletCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<OrderDto>> Handle(VerifyWalletCommand request, CancellationToken cancellationToken)
    {
        // 1. Begin database transaction
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // 2. Fetch the order including related items and wallet verification
            var order = await _orderRepository.GetByIdAsync(request.OrderId);
            if (order == null)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<OrderDto>.Failure(null, "Order not found.");
            }

            if (order.PaymentMethod != PaymentMethod.DigitalWallet || order.WalletVerification == null)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<OrderDto>.Failure(null, "Order does not require digital wallet verification.");
            }

            if (order.OrderStatus != OrderStatus.PendingVerification)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<OrderDto>.Failure(null, $"Order is not in PendingVerification status. Current: {order.OrderStatus}");
            }

            // Get admin verifier user ID from CurrentUserService
            var verifierIdString = _currentUserService.UserId;
            Guid? verifierId = null;
            if (!string.IsNullOrEmpty(verifierIdString) && Guid.TryParse(verifierIdString, out var parsedVerifierId))
            {
                verifierId = parsedVerifierId;
            }

            // 3. Update status and release/finalize stock
            if (request.Approve)
            {
                order.UpdateStatus(OrderStatus.ConfirmedPreparing);
                order.WalletVerification.IsVerified = true;
                order.WalletVerification.VerifiedByUserId = verifierId;
                order.WalletVerification.RejectionReason = null;
            }
            else
            {
                order.UpdateStatus(OrderStatus.ReturnedRejected);
                order.WalletVerification.IsVerified = false;
                order.WalletVerification.VerifiedByUserId = verifierId;
                order.WalletVerification.RejectionReason = request.RejectReason ?? "Image screenshot unverified or duplicate.";

                // Release locked inventory (increment back StockQuantity)
                foreach (var item in order.Items)
                {
                    var product = await _productRepository.GetByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                    }
                }
            }

            // 4. Save and commit transaction
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Fetch the updated order to return accurate DTO
            var updatedOrder = await _orderRepository.GetByIdAsync(order.Id);

            var resultDto = updatedOrder != null ? updatedOrder.ToDto() : order.ToDto();
            return ApiResponse<OrderDto>.Success(resultDto, request.Approve ? "Wallet payment approved successfully." : "Wallet payment rejected and stock released.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return ApiResponse<OrderDto>.Failure(null, $"An error occurred during wallet verification: {ex.Message}");
        }
    }
}
