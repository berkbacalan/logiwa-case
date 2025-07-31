using MediatR;
using EcomMMS.Domain.Entities;
using EcomMMS.Domain.Interfaces;
using EcomMMS.Application.DTOs;
using EcomMMS.Application.Common;
using FluentValidation;

namespace EcomMMS.Application.Features.Products.Commands.CreateProduct
{
    public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<ProductDto>>
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IValidator<CreateProductCommand> _validator;
        private readonly ICacheService _cacheService;
        private readonly IApplicationLogger _logger;

        public CreateProductCommandHandler(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IValidator<CreateProductCommand> validator,
            ICacheService cacheService,
            IApplicationLogger logger)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _validator = validator;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                var errorDetails = string.Join(", ", errors);
                _logger.LogValidationError("CreateProduct", errorDetails);
                return Result<ProductDto>.Failure("Validation failed", errors);
            }

            var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
            if (category == null)
            {
                _logger.LogBusinessLogicError("CreateProduct", $"Category with ID {request.CategoryId} not found");
                return Result<ProductDto>.Failure($"Category with ID {request.CategoryId} not found.");
            }

            var product = new Product(
                request.Title,
                request.Description,
                request.CategoryId,
                request.StockQuantity);

            product.SetCategory(category);

            var createdProduct = await _productRepository.AddAsync(product);

            var productDto = new ProductDto
            {
                Id = createdProduct.Id,
                Title = createdProduct.Title,
                Description = createdProduct.Description,
                CategoryId = createdProduct.CategoryId,
                CategoryName = category.Name,
                StockQuantity = createdProduct.StockQuantity,
                IsLive = createdProduct.IsLive,
                CreatedAt = createdProduct.CreatedAt,
                UpdatedAt = createdProduct.UpdatedAt
            };

            await InvalidateProductCache();

            _logger.LogInformation("Product created successfully - ProductId: {ProductId}, Title: {Title}, Category: {Category}", 
                createdProduct.Id, createdProduct.Title, category.Name);

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