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

        public DeleteProductCommandHandler(
            IProductRepository productRepository,
            IValidator<DeleteProductCommand> validator,
            ICacheService cacheService)
        {
            _productRepository = productRepository;
            _validator = validator;
            _cacheService = cacheService;
        }

        public async Task<Result<bool>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Result<bool>.Failure("Validation failed", errors);
            }

            var existingProduct = await _productRepository.GetByIdAsync(request.Id);
            if (existingProduct == null)
            {
                return Result<bool>.Failure($"Product with ID {request.Id} not found.");
            }

            await _productRepository.DeleteAsync(request.Id);

            await InvalidateProductCache();

            return Result<bool>.Success(true);
        }

        private async Task InvalidateProductCache()
        {
            try
            {
                await _cacheService.RemoveByPatternAsync("products:*");
            }
            catch (Exception)
            {
            }
        }
    }
} 