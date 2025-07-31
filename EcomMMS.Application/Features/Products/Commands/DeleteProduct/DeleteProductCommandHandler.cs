using MediatR;
using EcomMMS.Domain.Interfaces;
using EcomMMS.Application.Common;
using FluentValidation;

namespace EcomMMS.Application.Features.Products.Commands.DeleteProduct
{
    public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result<bool>>
    {
        private readonly IProductRepository _productRepository;
        private readonly IValidator<DeleteProductCommand> _validator;
        private readonly ICacheService _cacheService;
        private readonly IApplicationLogger _logger;

        public DeleteProductCommandHandler(
            IProductRepository productRepository,
            IValidator<DeleteProductCommand> validator,
            ICacheService cacheService,
            IApplicationLogger logger)
        {
            _productRepository = productRepository;
            _validator = validator;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogValidationError("DeleteProduct", string.Join(", ", errors));
                return Result<bool>.Failure("Validation failed", errors);
            }

            var existingProduct = await _productRepository.GetByIdAsync(request.Id);
            if (existingProduct == null)
            {
                _logger.LogBusinessLogicError("DeleteProduct", $"Product with ID {request.Id} not found");
                return Result<bool>.Failure($"Product with ID {request.Id} not found.");
            }

            await _productRepository.DeleteAsync(request.Id);
            _logger.LogInformation("Product deleted successfully: {ProductId} - {ProductTitle}", existingProduct.Id, existingProduct.Title);

            await InvalidateProductCache();

            return Result<bool>.Success(true);
        }

        private async Task InvalidateProductCache()
        {
            try
            {
                await _cacheService.RemoveByPatternAsync("products:*");
                _logger.LogInformation("Product cache invalidated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogCacheError(ex, "RemoveByPattern", "products:*");
            }
        }
    }
} 