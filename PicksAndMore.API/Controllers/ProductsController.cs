using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PicksAndMore.API.Authorization;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Products.Commands;
using PicksAndMore.Application.Products.Queries;
using Microsoft.EntityFrameworkCore;
using PicksAndMore.Infrastructure.Persistence;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Application.Interfaces;

namespace PicksAndMore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginationResult<ProductDto>>>> GetProducts([FromQuery] ProductQueryParameters queryParams)
    {
        var response = await _mediator.Send(new GetProductsQuery(queryParams));
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpPost("bulk")]
    [Authorize]
    [HasPermission("Products:Create")]
    public async Task<ActionResult<ApiResponse<BulkAddProductsResultDto>>> BulkAdd(
        [FromBody] List<ProductCreateDto> dtos,
        [FromQuery] Guid? defaultCategoryId,
        [FromQuery] string? defaultSeason)
    {
        // Extract default override settings from query or HTTP request headers
        Guid? categoryIdOverride = defaultCategoryId;
        if (!categoryIdOverride.HasValue && Request.Headers.TryGetValue("X-Default-CategoryId", out var headerCatId))
        {
            if (Guid.TryParse(headerCatId, out var parsedGuid))
            {
                categoryIdOverride = parsedGuid;
            }
        }

        string? seasonOverride = defaultSeason;
        if (string.IsNullOrEmpty(seasonOverride) && Request.Headers.TryGetValue("X-Default-Season", out var headerSeason))
        {
            seasonOverride = headerSeason.ToString();
        }

        var response = await _mediator.Send(new BulkAddProductsCommand(dtos, categoryIdOverride, seasonOverride));
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProductById(Guid id)
    {
        var response = await _mediator.Send(new GetProductByIdQuery(id));
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpPut("{id}")]
    [Authorize]
    [HasPermission("Products:Update")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(Guid id, [FromBody] UpdateProductDto dto)
    {
        var response = await _mediator.Send(new UpdateProductCommand(id, dto));
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpDelete("{id}")]
    [Authorize]
    [HasPermission("Products:Delete")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteProduct(Guid id)
    {
        var response = await _mediator.Send(new DeleteProductCommand(id));
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpPatch("{id}/toggle-visibility")]
    [Authorize]
    [HasPermission("Products:Update")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> ToggleProductVisibility(Guid id)
    {
        var response = await _mediator.Send(new ToggleProductVisibilityCommand(id));
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpGet("{id}/reviews")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<ProductReviewDto>>>> GetProductReviews(
        Guid id,
        [FromServices] ApplicationDbContext context)
    {
        var reviews = await context.ProductReviews
            .AsNoTracking()
            .Where(r => r.ProductId == id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ProductReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ReviewerName = r.ReviewerName,
                Comment = r.Comment,
                Rating = r.Rating,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<ProductReviewDto>>.Success(reviews, "Reviews fetched successfully."));
    }

    [HttpPost("{id}/reviews")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<ProductReviewDto>>> AddProductReview(
        Guid id,
        [FromBody] CreateProductReviewDto dto,
        [FromServices] ApplicationDbContext context,
        [FromServices] ICurrentUserService currentUserService)
    {
        var userIdStr = currentUserService.UserId;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(ApiResponse<ProductReviewDto>.Failure(null, "User identity not resolved."));
        }

        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse<ProductReviewDto>.Failure(null, "User not found."));
        }

        var productExists = await context.Products.AnyAsync(p => p.Id == id);
        if (!productExists)
        {
            return NotFound(ApiResponse<ProductReviewDto>.Failure(null, "Product not found."));
        }

        var review = new ProductReview(
            Guid.NewGuid(),
            id,
            userId,
            user.FullName,
            dto.Comment,
            dto.Rating
        );

        // Audit fields are handled in ApplicationDbContext.SaveChangesAsync()
        context.ProductReviews.Add(review);
        await context.SaveChangesAsync();

        var resultDto = new ProductReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            ReviewerName = review.ReviewerName,
            Comment = review.Comment,
            Rating = review.Rating,
            CreatedAt = review.CreatedAt
        };

        return Ok(ApiResponse<ProductReviewDto>.Success(resultDto, "Review submitted successfully."));
    }
}
