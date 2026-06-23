using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PicksAndMore.API.Authorization;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PicksAndMore.API.Controllers;

[ApiController]
[Route("api")]
public class ProductAttributesController : ControllerBase
{
    private readonly IColorOptionRepository _colorRepository;
    private readonly ISizeOptionRepository _sizeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductAttributesController(
        IColorOptionRepository colorRepository,
        ISizeOptionRepository sizeRepository,
        IUnitOfWork unitOfWork)
    {
        _colorRepository = colorRepository;
        _sizeRepository = sizeRepository;
        _unitOfWork = unitOfWork;
    }

    #region Color Endpoints

    [HttpGet("attributes/colors")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<ColorOption>>>> GetColors()
    {
        var colors = await _colorRepository.GetAllAsync();
        var sorted = colors.OrderBy(c => c.Name).ToList();
        return Ok(ApiResponse<List<ColorOption>>.Success(sorted, "Colors retrieved successfully."));
    }

    [HttpPost("admin/attributes/colors")]
    [Authorize]
    [HasPermission("Products:Update")]
    public async Task<ActionResult<ApiResponse<ColorOption>>> CreateColor([FromBody] CreateColorDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(ApiResponse<ColorOption>.Failure(null, "Color name is required."));
        if (string.IsNullOrWhiteSpace(dto.HexCode))
            return BadRequest(ApiResponse<ColorOption>.Failure(null, "Hex code is required."));

        var color = new ColorOption(Guid.NewGuid(), dto.Name.Trim(), dto.HexCode.Trim());
        await _colorRepository.AddAsync(color);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<ColorOption>.Success(color, "Color created successfully."));
    }

    [HttpPut("admin/attributes/colors/{id}")]
    [Authorize]
    [HasPermission("Products:Update")]
    public async Task<ActionResult<ApiResponse<ColorOption>>> UpdateColor(Guid id, [FromBody] CreateColorDto dto)
    {
        var color = await _colorRepository.GetByIdAsync(id);
        if (color == null)
            return NotFound(ApiResponse<ColorOption>.Failure(null, "Color not found."));

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(ApiResponse<ColorOption>.Failure(null, "Color name is required."));
        if (string.IsNullOrWhiteSpace(dto.HexCode))
            return BadRequest(ApiResponse<ColorOption>.Failure(null, "Hex code is required."));

        color.Name = dto.Name.Trim();
        color.HexCode = dto.HexCode.Trim();

        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<ColorOption>.Success(color, "Color updated successfully."));
    }

    [HttpDelete("admin/attributes/colors/{id}")]
    [Authorize]
    [HasPermission("Products:Update")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteColor(Guid id)
    {
        var color = await _colorRepository.GetByIdAsync(id);
        if (color == null)
            return NotFound(ApiResponse<bool>.Failure(false, "Color not found."));

        _colorRepository.Delete(color);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<bool>.Success(true, "Color deleted successfully."));
    }

    #endregion

    #region Size Endpoints

    [HttpGet("attributes/sizes")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<SizeOption>>>> GetSizes()
    {
        var sizes = await _sizeRepository.GetAllAsync();
        var sorted = sizes.OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToList();
        return Ok(ApiResponse<List<SizeOption>>.Success(sorted, "Sizes retrieved successfully."));
    }

    [HttpPost("admin/attributes/sizes")]
    [Authorize]
    [HasPermission("Products:Update")]
    public async Task<ActionResult<ApiResponse<SizeOption>>> CreateSize([FromBody] CreateSizeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(ApiResponse<SizeOption>.Failure(null, "Size name is required."));
        
        var targetAudience = string.IsNullOrWhiteSpace(dto.TargetAudience) ? "Both" : dto.TargetAudience.Trim();

        var size = new SizeOption(Guid.NewGuid(), dto.Name.Trim(), targetAudience, dto.SortOrder);
        await _sizeRepository.AddAsync(size);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<SizeOption>.Success(size, "Size created successfully."));
    }

    [HttpPut("admin/attributes/sizes/{id}")]
    [Authorize]
    [HasPermission("Products:Update")]
    public async Task<ActionResult<ApiResponse<SizeOption>>> UpdateSize(Guid id, [FromBody] CreateSizeDto dto)
    {
        var size = await _sizeRepository.GetByIdAsync(id);
        if (size == null)
            return NotFound(ApiResponse<SizeOption>.Failure(null, "Size not found."));

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(ApiResponse<SizeOption>.Failure(null, "Size name is required."));

        var targetAudience = string.IsNullOrWhiteSpace(dto.TargetAudience) ? "Both" : dto.TargetAudience.Trim();

        size.Name = dto.Name.Trim();
        size.TargetAudience = targetAudience;
        size.SortOrder = dto.SortOrder;

        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<SizeOption>.Success(size, "Size updated successfully."));
    }

    [HttpDelete("admin/attributes/sizes/{id}")]
    [Authorize]
    [HasPermission("Products:Update")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSize(Guid id)
    {
        var size = await _sizeRepository.GetByIdAsync(id);
        if (size == null)
            return NotFound(ApiResponse<bool>.Failure(false, "Size not found."));

        _sizeRepository.Delete(size);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<bool>.Success(true, "Size deleted successfully."));
    }

    #endregion
}

public class CreateColorDto
{
    public string Name { get; set; } = null!;
    public string HexCode { get; set; } = null!;
}

public class CreateSizeDto
{
    public string Name { get; set; } = null!;
    public string TargetAudience { get; set; } = "Both";
    public int SortOrder { get; set; }
}
