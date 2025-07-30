using MediatR;
using EcomMMS.Domain.Entities;
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

        public UpdateProductCommandHandler(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IValidator<UpdateProductCommand> validator)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _validator = validator;
        }

        public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Result<ProductDto>.Failure("Validation failed", errors);
            }

            var existingProduct = await _productRepository.GetByIdAsync(request.Id);
            if (existingProduct == null)
            {
                return Result<ProductDto>.Failure($"Product with ID {request.Id} not found.");
            }

            var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
            if (category == null)
            {
                return Result<ProductDto>.Failure($"Category with ID {request.CategoryId} not found.");
            }

            existingProduct.UpdateTitle(request.Title);
            existingProduct.UpdateDescription(request.Description);
            existingProduct.UpdateCategory(request.CategoryId);
            existingProduct.UpdateStockQuantity(request.StockQuantity);
            existingProduct.SetCategory(category);

            await _productRepository.UpdateAsync(existingProduct);

            var productDto = new ProductDto
            {
                Id = existingProduct.Id,
                Title = existingProduct.Title,
                Description = existingProduct.Description,
                CategoryId = existingProduct.CategoryId,
                CategoryName = category.Name,
                StockQuantity = existingProduct.StockQuantity,
                IsLive = existingProduct.IsLive,
                CreatedAt = existingProduct.CreatedAt,
                UpdatedAt = existingProduct.UpdatedAt
            };

            return Result<ProductDto>.Success(productDto);
        }
    }
} 