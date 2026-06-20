using MediatR;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Application.Mappings;

namespace PicksAndMore.Application.Products.Commands;

public record ToggleProductVisibilityCommand(Guid ProductId) : IRequest<ApiResponse<ProductDto>>;

public class ToggleProductVisibilityCommandHandler : IRequestHandler<ToggleProductVisibilityCommand, ApiResponse<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ToggleProductVisibilityCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<ProductDto>> Handle(ToggleProductVisibilityCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
        {
            return ApiResponse<ProductDto>.Failure(null, "Product not found.");
        }

        product.IsVisible = !product.IsVisible;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ProductDto>.Success(product.ToDto(),
            $"Product visibility toggled to {(product.IsVisible ? "Visible" : "Hidden")}.");
    }
}
