using MediatR;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Application.Mappings;

namespace PicksAndMore.Application.Products.Queries;

public record GetProductsQuery(ProductQueryParameters QueryParams) : IRequest<ApiResponse<PaginationResult<ProductDto>>>;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, ApiResponse<PaginationResult<ProductDto>>>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductShippingOverrideRepository _overrideRepository;

    public GetProductsQueryHandler(
        IProductRepository productRepository,
        IProductShippingOverrideRepository overrideRepository)
    {
        _productRepository = productRepository;
        _overrideRepository = overrideRepository;
    }

    public async Task<ApiResponse<PaginationResult<ProductDto>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var pagedResult = await _productRepository.GetPagedProductsAsync(request.QueryParams);
        
        var dtoItems = pagedResult.Items.Select(p => p.ToDto()).ToList();
        
        if (dtoItems.Any())
        {
            var productIds = dtoItems.Select(d => d.Id).ToList();
            var overrides = await _overrideRepository.GetByProductIdsAsync(productIds);
            foreach (var dto in dtoItems)
            {
                var ov = overrides.FirstOrDefault(o => o.ProductId == dto.Id);
                if (ov != null)
                {
                    dto.OverrideStandardShipping = true;
                    dto.IsFreeShipping = ov.IsFreeShipping;
                    dto.FixedShippingPrice = ov.FixedPrice;
                }
            }
        }
        
        var dtoPagedResult = new PaginationResult<ProductDto>(
            dtoItems, 
            pagedResult.Metadata.TotalCount, 
            pagedResult.Metadata.CurrentPage, 
            pagedResult.Metadata.PageSize
        );

        return ApiResponse<PaginationResult<ProductDto>>.Success(dtoPagedResult, "Products fetched successfully.");
    }
}
