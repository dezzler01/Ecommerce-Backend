using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PicksAndMore.API.Authorization;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Domain.Enums;

namespace PicksAndMore.API.Controllers;

[ApiController]
[Route("api/admin/shipping")]
[Authorize]
public class ShippingAdminController : ControllerBase
{
    private readonly IShippingMatrixRepository _matrixRepository;
    private readonly IShippingComboRuleRepository _comboRuleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ShippingAdminController(
        IShippingMatrixRepository matrixRepository,
        IShippingComboRuleRepository comboRuleRepository,
        IUnitOfWork unitOfWork)
    {
        _matrixRepository = matrixRepository;
        _comboRuleRepository = comboRuleRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet("governorates")]
    [HasPermission("Shipping:Read")]
    public async Task<ActionResult<ApiResponse<List<ShippingMatrix>>>> GetGovernorates()
    {
        var list = await _matrixRepository.GetAllAsync();
        var sorted = list.OrderBy(m => m.Governorate).ToList();
        return Ok(ApiResponse<List<ShippingMatrix>>.Success(sorted, "Governorate shipping matrices retrieved."));
    }

    [HttpPut("governorates/{id}")]
    [HasPermission("Shipping:Update")]
    public async Task<ActionResult<ApiResponse<ShippingMatrix>>> UpdateGovernorate(Guid id, [FromBody] UpdateGovernorateRateDto dto)
    {
        var matrix = await _matrixRepository.GetByIdAsync(id);
        if (matrix == null)
        {
            return NotFound(ApiResponse<ShippingMatrix>.Failure(null, "Governorate matrix row not found."));
        }

        matrix.BasePriceSmall = dto.BasePriceSmall;
        matrix.BasePriceMedium = dto.BasePriceMedium;
        matrix.BasePriceLarge = dto.BasePriceLarge;

        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<ShippingMatrix>.Success(matrix, "Governorate rate matrix updated successfully."));
    }

    [HttpGet("combo-rules")]
    [HasPermission("Shipping:Read")]
    public async Task<ActionResult<ApiResponse<List<ShippingComboRule>>>> GetComboRules()
    {
        var list = await _comboRuleRepository.GetAllAsync();
        return Ok(ApiResponse<List<ShippingComboRule>>.Success(list, "Shipping combination rules retrieved."));
    }

    [HttpPost("combo-rules")]
    [HasPermission("Shipping:Create")]
    public async Task<ActionResult<ApiResponse<ShippingComboRule>>> CreateComboRule([FromBody] CreateShippingComboRuleDto dto)
    {
        if (!Enum.TryParse<ShippingSize>(dto.ResultingSize, true, out var size))
        {
            return BadRequest(ApiResponse<ShippingComboRule>.Failure(null, $"Invalid ShippingSize value '{dto.ResultingSize}'."));
        }

        var rule = new ShippingComboRule(Guid.NewGuid(), dto.InputSmallCount, dto.InputMediumCount, size);
        await _comboRuleRepository.AddAsync(rule);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<ShippingComboRule>.Success(rule, "Combination rule created successfully."));
    }

    [HttpPut("combo-rules/{id}")]
    [HasPermission("Shipping:Update")]
    public async Task<ActionResult<ApiResponse<ShippingComboRule>>> UpdateComboRule(Guid id, [FromBody] CreateShippingComboRuleDto dto)
    {
        var rule = await _comboRuleRepository.GetByIdAsync(id);
        if (rule == null)
        {
            return NotFound(ApiResponse<ShippingComboRule>.Failure(null, "Combination rule not found."));
        }

        if (!Enum.TryParse<ShippingSize>(dto.ResultingSize, true, out var size))
        {
            return BadRequest(ApiResponse<ShippingComboRule>.Failure(null, $"Invalid ShippingSize value '{dto.ResultingSize}'."));
        }

        rule.InputSmallCount = dto.InputSmallCount;
        rule.InputMediumCount = dto.InputMediumCount;
        rule.ResultingSize = size;

        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<ShippingComboRule>.Success(rule, "Combination rule updated successfully."));
    }

    [HttpDelete("combo-rules/{id}")]
    [HasPermission("Shipping:Delete")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteComboRule(Guid id)
    {
        var rule = await _comboRuleRepository.GetByIdAsync(id);
        if (rule == null)
        {
            return NotFound(ApiResponse<bool>.Failure(false, "Combination rule not found."));
        }

        _comboRuleRepository.Delete(rule);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<bool>.Success(true, "Combination rule deleted successfully."));
    }
}

public class UpdateGovernorateRateDto
{
    public decimal BasePriceSmall { get; set; }
    public decimal BasePriceMedium { get; set; }
    public decimal BasePriceLarge { get; set; }
}

public class CreateShippingComboRuleDto
{
    public int InputSmallCount { get; set; }
    public int InputMediumCount { get; set; }
    public string ResultingSize { get; set; } = string.Empty;
}
