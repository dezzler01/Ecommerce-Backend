using MediatR;
using Microsoft.AspNetCore.SignalR;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Hubs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Application.Mappings;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Domain.Enums;
using PicksAndMore.Domain.ValueObjects;

namespace PicksAndMore.Application.Orders.Commands;

public record CreateOrderCommand(CreateOrderDto Dto) : IRequest<ApiResponse<OrderDto>>;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, ApiResponse<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPromoCodeRepository _promoCodeRepository;
    private readonly IDiscountService _discountService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IShippingService _shippingService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationSubscriptionRepository _subscriptionRepository;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IPromoCodeRepository promoCodeRepository,
        IDiscountService discountService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IShippingService shippingService,
        IHubContext<NotificationHub> hubContext,
        INotificationRepository notificationRepository,
        INotificationSubscriptionRepository subscriptionRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _promoCodeRepository = promoCodeRepository;
        _discountService = discountService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _shippingService = shippingService;
        _hubContext = hubContext;
        _notificationRepository = notificationRepository;
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<ApiResponse<OrderDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        
        if (dto.Items == null || !dto.Items.Any())
        {
            return ApiResponse<OrderDto>.Failure(new { Items = "Order must contain at least one item." }, "Empty order items.");
        }

        // 1. Build Address Value Object
        var address = new Address(
            dto.ShippingGovernorate, 
            dto.ShippingDetailedAddress, 
            dto.ShippingPrimaryPhone, 
            dto.ShippingSecondaryPhone
        );

        // 2. Resolve User ID
        var userIdString = _currentUserService.UserId;
        Guid userId = Guid.Empty;
        if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var parsedUserId))
        {
            userId = parsedUserId;
        }
        else
        {
            return ApiResponse<OrderDto>.Failure(null, "User identifier is missing or invalid in current context.");
        }

        // 3. Resolve Payment Method
        if (!Enum.TryParse<PaymentMethod>(dto.PaymentMethod, true, out var paymentMethod))
        {
            paymentMethod = PaymentMethod.COD;
        }

        // 4. Validate Digital Wallet parameters if chosen
        var isDigitalPayment = paymentMethod == PaymentMethod.DigitalWallet || paymentMethod == PaymentMethod.InstaPay || paymentMethod == PaymentMethod.VodafoneCash;
        if (isDigitalPayment)
        {
            if (string.IsNullOrWhiteSpace(dto.WalletScreenshotUrl))
            {
                return ApiResponse<OrderDto>.Failure(new { WalletScreenshotUrl = "Screenshot upload path is required for Digital Wallet payments." }, "Missing screenshot.");
            }
            if (string.IsNullOrWhiteSpace(dto.WalletSenderPhoneNumberOrName))
            {
                return ApiResponse<OrderDto>.Failure(new { WalletSenderPhoneNumberOrName = "Sender identity (number or name) is required for Digital Wallet payments." }, "Missing sender identity.");
            }
        }

        // 5. Calculate initial items total
        decimal itemsTotal = dto.Items.Sum(i => i.Quantity * i.UnitPrice);

        // 6. Evaluate Promo Code if supplied
        decimal discountAmount = 0;
        bool isFreeShipping = false;
        PromoCode? promoCodeEntity = null;

        if (!string.IsNullOrWhiteSpace(dto.PromoCode))
        {
            var promoValidation = await _discountService.ApplyPromoCodeAsync(dto.PromoCode, itemsTotal, userId);
            if (!promoValidation.IsSuccess)
            {
                return ApiResponse<OrderDto>.Failure(null, promoValidation.Message);
            }
            discountAmount = promoValidation.DiscountAmount;
            isFreeShipping = promoValidation.IsFreeShipping;
            
            if (promoValidation.PromoCodeId.HasValue)
            {
                promoCodeEntity = await _promoCodeRepository.GetByIdAsync(promoValidation.PromoCodeId.Value);
            }
        }

        // 7. Calculate shipping cost dynamically using ShippingService
        var orderItemsForShipping = dto.Items.Select(i => new OrderItemDto
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList();
        
        decimal shippingCost = await _shippingService.CalculateShippingCostAsync(userId, orderItemsForShipping, dto.ShippingGovernorate, isFreeShipping);

        // 8. Begin Transaction for stock decrement and promo code application operations
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Set initial order status
            var orderStatus = isDigitalPayment 
                ? OrderStatus.PendingVerification 
                : OrderStatus.ConfirmedPreparing;

            // 9. Create Order Aggregate Root
            var order = new Order(
                Guid.NewGuid(), 
                userId, 
                DateTime.UtcNow,
                0, // TotalPrice is calculated when adding items
                shippingCost,
                orderStatus,
                paymentMethod,
                address
            );

            // 10. Add items and lock stock inventory immediately
            foreach (var item in dto.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<OrderDto>.Failure(null, $"Product with ID '{item.ProductId}' not found.");
                }

                if (product.StockQuantity < item.Quantity)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<OrderDto>.Failure(null, $"Product '{product.Title}' has insufficient stock. Available: {product.StockQuantity}, Requested: {item.Quantity}.");
                }

                // Decrement stock quantity
                product.StockQuantity -= item.Quantity;

                order.AddOrderItem(
                    item.ProductId, 
                    item.Quantity, 
                    item.UnitPrice
                );
            }

            // Recalculate order total price
            order.RecalculateTotalPrice();
            
            // Apply discount if promo code was successfully evaluated
            order.TotalPrice = Math.Max(0, order.TotalPrice - discountAmount);

            // Increment PromoCode usage counter if applicable
            if (promoCodeEntity != null)
            {
                promoCodeEntity.CurrentUsage++;
            }

            // 11. Save Digital Wallet Verification details if applicable
            if (isDigitalPayment)
            {
                var verification = new DigitalWalletVerification(
                    Guid.NewGuid(),
                    order.Id,
                    dto.WalletScreenshotUrl!,
                    dto.WalletSenderPhoneNumberOrName!,
                    false,
                    null
                );
                order.WalletVerification = verification;
            }

            // 12. Persist and commit transaction
            await _orderRepository.AddAsync(order);

            var orderNumber = $"ORD-{order.Id.ToString().Substring(0, 8).ToUpper()}";

            // Create customer notification
            var customerNotification = new Notification(
                Guid.NewGuid(),
                userId,
                "Order Placed",
                $"Thank you! Your order {orderNumber} has been placed successfully.",
                "OrderPlaced",
                order.Id.ToString()
            );
            await _notificationRepository.AddAsync(customerNotification);

            // Retrieve staff subscriptions for NewOrder notifications
            var subscriptions = await _subscriptionRepository.GetByNotificationTypeAsync("NewOrder");
            var adminNotificationsToSend = new List<Notification>();

            foreach (var sub in subscriptions)
            {
                var adminNotification = new Notification(
                    Guid.NewGuid(),
                    sub.UserId,
                    "New Order Submitted",
                    $"Order {orderNumber} has been placed and requires verification.",
                    "NewOrder",
                    order.Id.ToString()
                );
                await _notificationRepository.AddAsync(adminNotification);
                adminNotificationsToSend.Add(adminNotification);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Broadcast customer notification in real-time
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
            {
                id = customerNotification.Id,
                userId = customerNotification.UserId,
                title = customerNotification.Title,
                message = customerNotification.Message,
                type = customerNotification.Type,
                isRead = customerNotification.IsRead,
                relatedEntityId = customerNotification.RelatedEntityId,
                createdAt = customerNotification.CreatedAt
            }, cancellationToken);

            // Broadcast admin notifications in real-time
            foreach (var note in adminNotificationsToSend)
            {
                await _hubContext.Clients.User(note.UserId.ToString()).SendAsync("ReceiveNotification", new
                {
                    id = note.Id,
                    userId = note.UserId,
                    title = note.Title,
                    message = note.Message,
                    type = note.Type,
                    isRead = note.IsRead,
                    relatedEntityId = note.RelatedEntityId,
                    createdAt = note.CreatedAt
                }, cancellationToken);
            }

            // Load related navigation objects for mapping
            var createdOrder = await _orderRepository.GetByIdAsync(order.Id);

            var orderDtoResult = createdOrder != null ? createdOrder.ToDto() : order.ToDto();
            return ApiResponse<OrderDto>.Success(orderDtoResult, "Order created successfully.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return ApiResponse<OrderDto>.Failure(null, $"An error occurred during order submission: {ex.Message}");
        }
    }
}
