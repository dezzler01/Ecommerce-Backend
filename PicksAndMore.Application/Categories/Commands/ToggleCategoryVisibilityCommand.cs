using MediatR;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Application.Mappings;

namespace PicksAndMore.Application.Categories.Commands;

public record ToggleCategoryVisibilityCommand(Guid CategoryId) : IRequest<ApiResponse<CategoryDto>>;

public class ToggleCategoryVisibilityCommandHandler : IRequestHandler<ToggleCategoryVisibilityCommand, ApiResponse<CategoryDto>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ToggleCategoryVisibilityCommandHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<CategoryDto>> Handle(ToggleCategoryVisibilityCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
        if (category == null)
        {
            return ApiResponse<CategoryDto>.Failure(null, "Category not found.");
        }

        category.IsVisible = !category.IsVisible;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<CategoryDto>.Success(category.ToDto(), 
            $"Category visibility toggled to {(category.IsVisible ? "Visible" : "Hidden")}.");
    }
}
