using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PicksAndMore.API.Authorization;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;

namespace PicksAndMore.API.Controllers;

[ApiController]
[Route("api")]
public class BrandsController : ControllerBase
{
    private readonly IBrandRepository _brandRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BrandsController(IBrandRepository brandRepository, IUnitOfWork unitOfWork)
    {
        _brandRepository = brandRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet("brands")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<Brand>>>> GetVisibleBrands()
    {
        var all = await _brandRepository.GetAllAsync();
        var visible = all.Where(b => b.IsVisible).OrderBy(b => b.Name).ToList();
        return Ok(ApiResponse<List<Brand>>.Success(visible, "Visible brands retrieved."));
    }

    [HttpGet("admin/brands")]
    [Authorize]
    [HasPermission("Products:Read")]
    public async Task<ActionResult<ApiResponse<List<Brand>>>> GetAllBrands()
    {
        var all = await _brandRepository.GetAllAsync();
        var sorted = all.OrderBy(b => b.Name).ToList();
        return Ok(ApiResponse<List<Brand>>.Success(sorted, "All brands retrieved."));
    }

    [HttpPost("admin/brands")]
    [Authorize]
    [HasPermission("Products:Create")]
    public async Task<ActionResult<ApiResponse<Brand>>> CreateBrand([FromBody] CreateBrandDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(ApiResponse<Brand>.Failure(null, "Brand name is required."));
        }
        if (string.IsNullOrWhiteSpace(dto.LogoUrl))
        {
            return BadRequest(ApiResponse<Brand>.Failure(null, "Brand logo URL is required."));
        }

        var brand = new Brand(Guid.NewGuid(), dto.Name, dto.LogoUrl, dto.ShowInCards, dto.IsVisible);
        await _brandRepository.AddAsync(brand);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<Brand>.Success(brand, "Brand created successfully."));
    }

    [HttpPut("admin/brands/{id}")]
    [Authorize]
    [HasPermission("Products:Update")]
    public async Task<ActionResult<ApiResponse<Brand>>> UpdateBrand(Guid id, [FromBody] CreateBrandDto dto)
    {
        var brand = await _brandRepository.GetByIdAsync(id);
        if (brand == null)
        {
            return NotFound(ApiResponse<Brand>.Failure(null, "Brand not found."));
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(ApiResponse<Brand>.Failure(null, "Brand name is required."));
        }
        if (string.IsNullOrWhiteSpace(dto.LogoUrl))
        {
            return BadRequest(ApiResponse<Brand>.Failure(null, "Brand logo URL is required."));
        }

        brand.Name = dto.Name;
        brand.LogoUrl = dto.LogoUrl;
        brand.ShowInCards = dto.ShowInCards;
        brand.IsVisible = dto.IsVisible;

        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<Brand>.Success(brand, "Brand updated successfully."));
    }

    [HttpDelete("admin/brands/{id}")]
    [Authorize]
    [HasPermission("Products:Delete")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteBrand(Guid id)
    {
        var brand = await _brandRepository.GetByIdAsync(id);
        if (brand == null)
        {
            return NotFound(ApiResponse<bool>.Failure(false, "Brand not found."));
        }

        _brandRepository.Delete(brand);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<bool>.Success(true, "Brand deleted successfully."));
    }
}

public class CreateBrandDto
{
    public string Name { get; set; } = null!;
    public string LogoUrl { get; set; } = null!;
    public bool ShowInCards { get; set; }
    public bool IsVisible { get; set; } = true;
}
