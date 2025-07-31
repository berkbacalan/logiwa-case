using MediatR;
using EcomMMS.Domain.Interfaces;
using EcomMMS.Application.DTOs;
using EcomMMS.Application.Common;

namespace EcomMMS.Application.Features.Products.Queries.GetProductById
{
    public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ICacheService _cacheService;
        private readonly ICacheKeyGenerator _cacheKeyGenerator;

        public GetProductByIdQueryHandler(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            ICacheService cacheService,
            ICacheKeyGenerator cacheKeyGenerator)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _cacheService = cacheService;
            _cacheKeyGenerator = cacheKeyGenerator;
        }

        public async Task<Result<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = _cacheKeyGenerator.GenerateProductByIdKey(request.Id);

                ProductDto? cachedResult = null;
                try
                {
                    cachedResult = await _cacheService.GetAsync<ProductDto>(cacheKey);
                }
                catch (Exception){}

                if (cachedResult != null)
                {
                    return Result<ProductDto>.Success(cachedResult);
                }

                var product = await _productRepository.GetByIdAsync(request.Id);
                if (product == null)
                {
                    return Result<ProductDto>.Failure($"Product with ID {request.Id} not found.");
                }

                var category = await _categoryRepository.GetByIdAsync(product.CategoryId);
                var productDto = new ProductDto
                {
                    Id = product.Id,
                    Title = product.Title,
                    Description = product.Description,
                    CategoryId = product.CategoryId,
                    CategoryName = category?.Name ?? string.Empty,
                    StockQuantity = product.StockQuantity,
                    IsLive = product.IsLive,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt
                };

                try
                {
                    await _cacheService.SetAsync(cacheKey, productDto, TimeSpan.FromHours(1));
                }
                catch (Exception)
                {
                }

                return Result<ProductDto>.Success(productDto);
            }
            catch (Exception ex)
            {
                return Result<ProductDto>.Failure($"An error occurred while retrieving the product: {ex.Message}");
            }
        }
    }
} 