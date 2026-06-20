using MediatR;
using Microsoft.AspNetCore.Identity;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Application.Mappings;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Domain.Enums;
using PicksAndMore.Domain.ValueObjects;

namespace PicksAndMore.Application.Orders.Commands;

public record CreateGuestOrderCommand(CreateGuestOrderDto Dto) : IRequest<ApiResponse<OrderDto>>;

public class CreateGuestOrderCommandHandler : IRequestHandler<CreateGuestOrderCommand, ApiResponse<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPromoCodeRepository _promoCodeRepository;
    private readonly IDiscountService _discountService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShippingService _shippingService;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public CreateGuestOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IPromoCodeRepository promoCodeRepository,
        IDiscountService discountService,
        IUnitOfWork unitOfWork,
        IShippingService shippingService,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher<ApplicationUser> passwordHasher)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _promoCodeRepository = promoCodeRepository;
        _discountService = discountService;
        _unitOfWork = unitOfWork;
        _shippingService = shippingService;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApiResponse<OrderDto>> Handle(CreateGuestOrderCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        // --- Input Validation ---
        if (string.IsNullOrWhiteSpace(dto.CustomerName))
            return ApiResponse<OrderDto>.Failure(null, "Customer name is required.");

        if (string.IsNullOrWhiteSpace(dto.PrimaryPhone))
            return ApiResponse<OrderDto>.Failure(null, "Primary phone number is required.");

        if (string.IsNullOrWhiteSpace(dto.Governorate))
            return ApiResponse<OrderDto>.Failure(null, "Governorate is required.");

        if (string.IsNullOrWhiteSpace(dto.DetailedAddress))
            return ApiResponse<OrderDto>.Failure(null, "Detailed address is required.");

        if (dto.Items == null || !dto.Items.Any())
            return ApiResponse<OrderDto>.Failure(null, "Order must contain at least one item.");

        // --- 1. Resolve or Create Guest User by Phone Number ---
        var existingUser = await _userRepository.GetByPhoneAsync(dto.PrimaryPhone);
        Guid userId;

        if (existingUser != null)
        {
            // Link order to existing user account
            userId = existingUser.Id;
        }
        else
        {
            // Auto-create a new guest account from the phone number
            var guestUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = dto.CustomerName,
                UserName = dto.PrimaryPhone,
                NormalizedUserName = dto.PrimaryPhone.ToUpperInvariant(),
                PhoneNumber = dto.PrimaryPhone,
                SecondaryPhoneNumber = dto.SecondaryPhone,
                AddressDetails = dto.DetailedAddress,
                Governorate = dto.Governorate,
                IsGuest = true,
                EmailConfirmed = false,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            // Assign a default customer-like role (SupportLogistics has CreateOrder permission)
            // Look for a Customer role first, fallback to any role that exists
            var defaultRole = await _roleRepository.GetByNameAsync("Customer");
            if (defaultRole == null)
            {
                // Fallback: use SupportLogistics role which has CreateOrder permission
                defaultRole = await _roleRepository.GetByNameAsync("SupportLogistics");
            }
            if (defaultRole != null)
            {
                guestUser.RoleId = defaultRole.Id;
            }

            // Generate a secure temporary password (guest won't use it to log in)
            guestUser.PasswordHash = _passwordHasher.HashPassword(guestUser, $"Guest@{Guid.NewGuid():N}");

            await _userRepository.AddAsync(guestUser);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            userId = guestUser.Id;
        }

        // --- 2. Build Address Value Object ---
        var address = new Address(
            dto.Governorate,
            dto.DetailedAddress,
            dto.PrimaryPhone,
            dto.SecondaryPhone
        );

        // --- 3. Resolve Payment Method ---
        if (!Enum.TryParse<PaymentMethod>(dto.PaymentMethod, true, out var paymentMethod))
        {
            paymentMethod = PaymentMethod.COD;
        }

        // --- 4. Validate Digital Wallet parameters if chosen ---
        if (paymentMethod == PaymentMethod.DigitalWallet)
        {
            if (string.IsNullOrWhiteSpace(dto.WalletScreenshotUrl))
                return ApiResponse<OrderDto>.Failure(null, "Screenshot upload is required for Digital Wallet payments.");
            if (string.IsNullOrWhiteSpace(dto.WalletSenderPhoneNumberOrName))
                return ApiResponse<OrderDto>.Failure(null, "Sender identity is required for Digital Wallet payments.");
        }

        // --- 5. Calculate initial items total ---
        decimal itemsTotal = dto.Items.Sum(i => i.Quantity * i.UnitPrice);

        // --- 6. Evaluate Promo Code if supplied ---
        decimal discountAmount = 0;
        bool isFreeShipping = false;
        PromoCode? promoCodeEntity = null;

        if (!string.IsNullOrWhiteSpace(dto.PromoCode))
        {
            var promoValidation = await _discountService.ApplyPromoCodeAsync(dto.PromoCode, itemsTotal, userId);
            if (!promoValidation.IsSuccess)
                return ApiResponse<OrderDto>.Failure(null, promoValidation.Message);

            discountAmount = promoValidation.DiscountAmount;
            isFreeShipping = promoValidation.IsFreeShipping;

            if (promoValidation.PromoCodeId.HasValue)
                promoCodeEntity = await _promoCodeRepository.GetByIdAsync(promoValidation.PromoCodeId.Value);
        }

        // --- 7. Calculate shipping cost ---
        var orderItemsForShipping = dto.Items.Select(i => new OrderItemDto
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList();

        decimal shippingCost = await _shippingService.CalculateShippingCostAsync(
            userId, orderItemsForShipping, dto.Governorate, isFreeShipping);

        // --- 8. Begin Transaction ---
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var orderStatus = paymentMethod == PaymentMethod.DigitalWallet
                ? OrderStatus.PendingVerification
                : OrderStatus.ConfirmedPreparing;

            // --- 9. Create Order ---
            var order = new Order(
                Guid.NewGuid(),
                userId,
                DateTime.UtcNow,
                0,
                shippingCost,
                orderStatus,
                paymentMethod,
                address
            );

            // --- 10. Add items and lock stock ---
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
                    return ApiResponse<OrderDto>.Failure(null,
                        $"Product '{product.Title}' has insufficient stock. Available: {product.StockQuantity}, Requested: {item.Quantity}.");
                }

                product.StockQuantity -= item.Quantity;
                order.AddOrderItem(item.ProductId, item.Quantity, item.UnitPrice);
            }

            // Recalculate and apply discount
            order.RecalculateTotalPrice();
            order.TotalPrice = Math.Max(0, order.TotalPrice - discountAmount);

            // Increment promo code usage
            if (promoCodeEntity != null)
            {
                promoCodeEntity.CurrentUsage++;
            }

            // --- 11. Attach Digital Wallet Verification if applicable ---
            if (paymentMethod == PaymentMethod.DigitalWallet)
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

            // --- 12. Persist and commit ---
            await _orderRepository.AddAsync(order);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var createdOrder = await _orderRepository.GetByIdAsync(order.Id);
            var orderDtoResult = createdOrder != null ? createdOrder.ToDto() : order.ToDto();
            return ApiResponse<OrderDto>.Success(orderDtoResult, "Guest order created successfully.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return ApiResponse<OrderDto>.Failure(null, $"An error occurred during guest order submission: {ex.Message}");
        }
    }
}
