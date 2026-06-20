using MediatR;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Application.Mappings;

namespace PicksAndMore.Application.Products.Queries;

public record GetProductByIdQuery(Guid ProductId) : IRequest<ApiResponse<ProductDto>>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ApiResponse<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductShippingOverrideRepository _overrideRepository;

    public GetProductByIdQueryHandler(
        IProductRepository productRepository,
        IProductShippingOverrideRepository overrideRepository)
    {
        _productRepository = productRepository;
        _overrideRepository = overrideRepository;
    }

    public async Task<ApiResponse<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
        {
            return ApiResponse<ProductDto>.Failure(null, "Product not found.");
        }

        var dto = product.ToDto();
        var overrideRule = await _overrideRepository.GetByProductIdAsync(product.Id);
        if (overrideRule != null)
        {
            dto.OverrideStandardShipping = true;
            dto.IsFreeShipping = overrideRule.IsFreeShipping;
            dto.FixedShippingPrice = overrideRule.FixedPrice;
        }

        return ApiResponse<ProductDto>.Success(dto, "Product retrieved successfully.");
    }
}
