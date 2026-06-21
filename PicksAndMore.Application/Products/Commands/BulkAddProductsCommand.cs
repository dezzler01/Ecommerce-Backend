using MediatR;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Domain.Enums;

namespace PicksAndMore.Application.Products.Commands;

public record BulkAddProductsCommand(
    List<ProductCreateDto> Dtos, 
    Guid? OverrideCategoryId = null, 
    string? OverrideSeason = null) : IRequest<ApiResponse<BulkAddProductsResultDto>>;

public class BulkAddProductsCommandHandler : IRequestHandler<BulkAddProductsCommand, ApiResponse<BulkAddProductsResultDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductShippingOverrideRepository _overrideRepository;

    public BulkAddProductsCommandHandler(
        IProductRepository productRepository, 
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IProductShippingOverrideRepository overrideRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _overrideRepository = overrideRepository;
    }

    public async Task<ApiResponse<BulkAddProductsResultDto>> Handle(BulkAddProductsCommand request, CancellationToken cancellationToken)
    {
        var result = new BulkAddProductsResultDto
        {
            TotalProcessed = request.Dtos.Count
        };

        var products = new List<Product>();
        
        for (int i = 0; i < request.Dtos.Count; i++)
        {
            var dto = request.Dtos[i];
            try
            {
                if (!Enum.TryParse<ShippingSize>(dto.ShippingSize, true, out var shipSize))
                {
                    result.Errors.Add($"Item {i}: Invalid ShippingSize '{dto.ShippingSize}'.");
                    result.ErrorsCount++;
                    continue;
                }

                // 1. Resolve Category (use override if provided, or multiple categories)
                Guid categoryId;
                var resolvedCategories = new List<Category>();

                if (request.OverrideCategoryId.HasValue)
                {
                    var catExists = await _categoryRepository.GetByIdAsync(request.OverrideCategoryId.Value);
                    if (catExists == null)
                    {
                        result.Errors.Add($"Item {i}: Overridden CategoryId '{request.OverrideCategoryId}' does not exist.");
                        result.ErrorsCount++;
                        continue;
                    }
                    categoryId = request.OverrideCategoryId.Value;
                    resolvedCategories.Add(catExists);
                }
                else if (dto.SubCategories != null && dto.SubCategories.Count > 0)
                {
                    foreach (var subName in dto.SubCategories)
                    {
                        var category = await _categoryRepository.GetByNameAndAudienceAsync(subName, dto.MainCategory);
                        if (category == null)
                        {
                            category = new Category(Guid.NewGuid(), subName, dto.MainCategory, true);
                            await _categoryRepository.AddAsync(category);
                        }
                        resolvedCategories.Add(category);
                    }
                    categoryId = resolvedCategories.First().Id;
                }
                else
                {
                    var category = await _categoryRepository.GetByNameAndAudienceAsync(dto.SubCategory, dto.MainCategory);
                    if (category == null)
                    {
                        category = new Category(Guid.NewGuid(), dto.SubCategory, dto.MainCategory, true);
                        await _categoryRepository.AddAsync(category);
                    }
                    categoryId = category.Id;
                    resolvedCategories.Add(category);
                }

                // 2. Resolve Seasons (include override if provided)
                var seasons = dto.SeasonTags ?? new List<string>();
                if (!string.IsNullOrWhiteSpace(request.OverrideSeason))
                {
                    if (!seasons.Contains(request.OverrideSeason))
                    {
                        seasons.Add(request.OverrideSeason);
                    }
                }

                // 3. Instantiate Product
                var product = new Product(
                    Guid.NewGuid(),
                    dto.Title,
                    dto.Description,
                    dto.Price,
                    dto.CostPrice,
                    dto.StockQuantity,
                    dto.IsVisible,
                    dto.ScheduledPublishDate,
                    categoryId,
                    shipSize,
                    dto.ImageUrl
                );

                product.Age = dto.Age;
                product.Categories = resolvedCategories;
                product.BrandId = dto.BrandId;
                product.CollectionType = dto.CollectionType;

                // 4. Link ProductMetadata
                product.Metadata = new ProductMetadata(
                    Guid.NewGuid(),
                    product.Id,
                    dto.Sizes,
                    dto.Colors,
                    dto.Materials ?? new List<string>(),
                    seasons
                );

                // 5. Create ProductImage entries
                if (dto.ImageUrls != null && dto.ImageUrls.Count > 0)
                {
                    for (int j = 0; j < dto.ImageUrls.Count; j++)
                    {
                        product.Images.Add(new ProductImage(
                            Guid.NewGuid(),
                            product.Id,
                            dto.ImageUrls[j],
                            j
                        ));
                    }
                }
                else if (!string.IsNullOrEmpty(dto.ImageUrl))
                {
                    // Legacy single image — create as first ProductImage
                    product.Images.Add(new ProductImage(
                        Guid.NewGuid(),
                        product.Id,
                        dto.ImageUrl,
                        0
                    ));
                }
                
                products.Add(product);

                if (dto.OverrideStandardShipping)
                {
                    var shippingOverride = new ProductShippingOverride(
                        Guid.NewGuid(),
                        product.Id,
                        dto.IsFreeShipping,
                        dto.FixedShippingPrice
                    );
                    await _overrideRepository.AddAsync(shippingOverride);
                }

                result.SuccessfullyCreated++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Item {i}: Unexpected error: {ex.Message}");
                result.ErrorsCount++;
            }
        }

        if (products.Any())
        {
            await _productRepository.BulkAddAsync(products);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse<BulkAddProductsResultDto>.Success(result, $"Bulk import processed. Success: {result.SuccessfullyCreated}, Failures: {result.ErrorsCount}.");
    }
}
