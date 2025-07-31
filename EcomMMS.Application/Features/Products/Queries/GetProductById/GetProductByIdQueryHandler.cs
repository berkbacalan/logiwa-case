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
        private readonly IApplicationLogger _logger;

        public GetProductByIdQueryHandler(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            ICacheService cacheService,
            ICacheKeyGenerator cacheKeyGenerator,
            IApplicationLogger logger)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _cacheService = cacheService;
            _cacheKeyGenerator = cacheKeyGenerator;
            _logger = logger;
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
                    if (cachedResult != null)
                    {
                        _logger.LogInformation("Product retrieved from cache - ProductId: {ProductId}", request.Id);
                        return Result<ProductDto>.Success(cachedResult);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCacheError(ex, "Get", cacheKey);
                }

                var product = await _productRepository.GetByIdAsync(request.Id);
                if (product == null)
                {
                    _logger.LogBusinessLogicError("GetProductById", $"Product with ID {request.Id} not found");
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
                    _logger.LogInformation("Product cached successfully - ProductId: {ProductId}", request.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogCacheError(ex, "Set", cacheKey);
                }

                _logger.LogInformation("Product retrieved successfully - ProductId: {ProductId}, Title: {Title}", 
                    request.Id, product.Title);
                return Result<ProductDto>.Success(productDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve product - ProductId: {ProductId}", request.Id);
                return Result<ProductDto>.Failure($"An error occurred while retrieving the product: {ex.Message}");
            }
        }
    }
} 