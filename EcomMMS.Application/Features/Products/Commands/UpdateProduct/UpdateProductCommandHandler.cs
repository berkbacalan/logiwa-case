using MediatR;
using EcomMMS.Domain.Interfaces;
using EcomMMS.Application.DTOs;
using EcomMMS.Application.Common;
using FluentValidation;

namespace EcomMMS.Application.Features.Products.Commands.UpdateProduct
{
    public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IValidator<UpdateProductCommand> _validator;
        private readonly ICacheService _cacheService;
        private readonly IApplicationLogger _logger;

        public UpdateProductCommandHandler(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IValidator<UpdateProductCommand> validator,
            ICacheService cacheService,
            IApplicationLogger logger)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _validator = validator;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogValidationError("UpdateProduct", string.Join(", ", errors));
                return Result<ProductDto>.Failure("Validation failed", errors);
            }

            var existingProduct = await _productRepository.GetByIdAsync(request.Id);
            if (existingProduct == null)
            {
                _logger.LogBusinessLogicError("UpdateProduct", $"Product with ID {request.Id} not found");
                return Result<ProductDto>.Failure($"Product with ID {request.Id} not found.");
            }

            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                existingProduct.UpdateTitle(request.Title);
            }

            if (request.Description != null)
            {
                existingProduct.UpdateDescription(request.Description);
            }

            if (request.CategoryId.HasValue)
            {
                var newCategory = await _categoryRepository.GetByIdAsync(request.CategoryId.Value);
                if (newCategory == null)
                {
                    _logger.LogBusinessLogicError("UpdateProduct", $"Category with ID {request.CategoryId.Value} not found");
                    return Result<ProductDto>.Failure($"Category with ID {request.CategoryId.Value} not found.");
                }
                existingProduct.UpdateCategory(request.CategoryId.Value);
            }

            if (request.StockQuantity.HasValue)
            {
                existingProduct.UpdateStockQuantity(request.StockQuantity.Value);
            }

            await _productRepository.UpdateAsync(existingProduct);

            var category = await _categoryRepository.GetByIdAsync(existingProduct.CategoryId);
            var productDto = new ProductDto
            {
                Id = existingProduct.Id,
                Title = existingProduct.Title,
                Description = existingProduct.Description,
                CategoryId = existingProduct.CategoryId,
                CategoryName = category?.Name ?? string.Empty,
                StockQuantity = existingProduct.StockQuantity,
                IsLive = existingProduct.IsLive,
                CreatedAt = existingProduct.CreatedAt,
                UpdatedAt = existingProduct.UpdatedAt
            };

            await InvalidateProductCache();
            _logger.LogInformation("Product updated successfully: {ProductId} - {ProductTitle}", existingProduct.Id, existingProduct.Title);

            return Result<ProductDto>.Success(productDto);
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