using MediatR;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Application.Mappings;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Domain.Enums;

namespace PicksAndMore.Application.Products.Commands;

public record UpdateProductCommand(Guid ProductId, UpdateProductDto Dto) : IRequest<ApiResponse<ProductDto>>;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ApiResponse<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductShippingOverrideRepository _overrideRepository;

    public UpdateProductCommandHandler(
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

    public async Task<ApiResponse<ProductDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
        {
            return ApiResponse<ProductDto>.Failure(null, "Product not found.");
        }

        var dto = request.Dto;

        if (!Enum.TryParse<ShippingSize>(dto.ShippingSize, true, out var shippingSize))
        {
            return ApiResponse<ProductDto>.Failure(null, $"Invalid ShippingSize value '{dto.ShippingSize}'.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Update core product fields
            product.Title = dto.Title;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.CostPrice = dto.CostPrice;
            product.StockQuantity = dto.StockQuantity;
            product.IsVisible = dto.IsVisible;
            product.ScheduledPublishDate = dto.ScheduledPublishDate;
            product.CategoryId = dto.CategoryId;
            product.ShippingSize = shippingSize;
            product.ImageUrl = dto.ImageUrl;
            product.Age = dto.Age;
            product.BrandId = dto.BrandId;

            // Update categories (many-to-many)
            if (dto.CategoryIds != null)
            {
                var resolvedCategories = new List<Category>();
                foreach (var catId in dto.CategoryIds)
                {
                    var cat = await _categoryRepository.GetByIdAsync(catId);
                    if (cat != null)
                    {
                        resolvedCategories.Add(cat);
                    }
                }
                product.Categories.Clear();
                product.Categories.AddRange(resolvedCategories);
                if (resolvedCategories.Any())
                {
                    product.CategoryId = resolvedCategories.First().Id;
                }
            }
            else
            {
                // Fallback: Ensure primary CategoryId is in the Categories collection
                if (!product.Categories.Any(c => c.Id == product.CategoryId))
                {
                    var primaryCat = await _categoryRepository.GetByIdAsync(product.CategoryId);
                    if (primaryCat != null)
                    {
                        product.Categories.Add(primaryCat);
                    }
                }
            }

            // Update product images (replace all)
            if (dto.ImageUrls != null)
            {
                product.Images.Clear();
                for (int i = 0; i < dto.ImageUrls.Count; i++)
                {
                    var imgDto = dto.ImageUrls[i];
                    product.Images.Add(new ProductImage(
                        imgDto.Id == Guid.Empty ? Guid.NewGuid() : imgDto.Id,
                        product.Id,
                        imgDto.Url,
                        imgDto.SortOrder,
                        imgDto.AltText
                    ));
                }
                // Update legacy ImageUrl to first image for backward compatibility
                if (product.Images.Any())
                {
                    product.ImageUrl = product.Images.OrderBy(i => i.SortOrder).First().Url;
                }
            }

            // Update or create metadata
            if (product.Metadata != null)
            {
                product.Metadata.Sizes = dto.Sizes;
                product.Metadata.Colors = dto.Colors;
                product.Metadata.Materials = dto.Materials;
                product.Metadata.SeasonTags = dto.SeasonTags;
            }
            else
            {
                product.Metadata = new ProductMetadata(
                    Guid.NewGuid(),
                    product.Id,
                    dto.Sizes,
                    dto.Colors,
                    dto.Materials,
                    dto.SeasonTags
                );
            }

            var existingOverride = await _overrideRepository.GetByProductIdAsync(product.Id);
            if (dto.OverrideStandardShipping)
            {
                if (existingOverride == null)
                {
                    var newOverride = new ProductShippingOverride(
                        Guid.NewGuid(),
                        product.Id,
                        dto.IsFreeShipping,
                        dto.FixedShippingPrice
                    );
                    await _overrideRepository.AddAsync(newOverride);
                }
                else
                {
                    existingOverride.IsFreeShipping = dto.IsFreeShipping;
                    existingOverride.FixedPrice = dto.FixedShippingPrice;
                }
            }
            else
            {
                if (existingOverride != null)
                {
                    _overrideRepository.Delete(existingOverride);
                }
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Re-fetch with navigation properties for accurate mapping
            var updated = await _productRepository.GetByIdAsync(product.Id);
            var updatedDto = updated!.ToDto();
            var finalOverride = await _overrideRepository.GetByProductIdAsync(product.Id);
            if (finalOverride != null)
            {
                updatedDto.OverrideStandardShipping = true;
                updatedDto.IsFreeShipping = finalOverride.IsFreeShipping;
                updatedDto.FixedShippingPrice = finalOverride.FixedPrice;
            }
            return ApiResponse<ProductDto>.Success(updatedDto, "Product updated successfully.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return ApiResponse<ProductDto>.Failure(null, $"Failed to update product: {ex.Message}");
        }
    }
}
