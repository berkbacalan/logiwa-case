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

        public CreateProductCommandHandler(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IValidator<CreateProductCommand> validator,
            ICacheService cacheService)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _validator = validator;
            _cacheService = cacheService;
        }

        public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Result<ProductDto>.Failure("Validation failed", errors);
            }

            var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
            if (category == null)
            {
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

            return Result<ProductDto>.Success(productDto);
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