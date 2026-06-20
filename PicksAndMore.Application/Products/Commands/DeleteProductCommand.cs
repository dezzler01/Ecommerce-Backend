using MediatR;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.Interfaces;

namespace PicksAndMore.Application.Products.Commands;

public record DeleteProductCommand(Guid ProductId) : IRequest<ApiResponse<bool>>;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, ApiResponse<bool>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
        {
            return ApiResponse<bool>.Failure(false, "Product not found.");
        }

        await _productRepository.DeleteAsync(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Success(true, "Product deleted successfully.");
    }
}
