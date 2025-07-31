using MediatR;
using EcomMMS.Application.Common;
using EcomMMS.Application.DTOs;
using EcomMMS.Domain.Interfaces;
using System.Linq;

namespace EcomMMS.Application.Features.Products.Queries.GetFilteredProducts
{
    public class GetFilteredProductsQueryHandler : IRequestHandler<GetFilteredProductsQuery, Result<PaginatedResult<ProductDto>>>
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ICacheService _cacheService;
        private readonly ICacheKeyGenerator _cacheKeyGenerator;
        private readonly IApplicationLogger _logger;

        public GetFilteredProductsQueryHandler(
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

        public async Task<Result<PaginatedResult<ProductDto>>> Handle(GetFilteredProductsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = _cacheKeyGenerator.GenerateFilteredProductsKey(
                    request.SearchTerm,
                    request.MinStockQuantity,
                    request.MaxStockQuantity,
                    request.IsLive,
                    request.CategoryId,
                    request.Page,
                    request.PageSize);

                var cachedResult = await _cacheService.GetAsync<PaginatedResult<ProductDto>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation("Filtered products retrieved from cache - Count: {Count}, Page: {Page}, PageSize: {PageSize}", 
                        cachedResult.Data.Count(), request.Page, request.PageSize);
                    return Result<PaginatedResult<ProductDto>>.Success(cachedResult);
                }

                var products = await _productRepository.GetAllAsync();
                var categories = await _categoryRepository.GetAllAsync();

                var query = products.AsQueryable();

                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    query = query.Where(p => p.Title.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                                           (p.Description != null && p.Description.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                                           (p.Category != null && p.Category.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)));
                }

                if (request.MinStockQuantity.HasValue)
                {
                    query = query.Where(p => p.StockQuantity >= request.MinStockQuantity.Value);
                }

                if (request.MaxStockQuantity.HasValue)
                {
                    query = query.Where(p => p.StockQuantity <= request.MaxStockQuantity.Value);
                }

                if (request.IsLive.HasValue)
                {
                    query = query.Where(p => p.IsLive == request.IsLive.Value);
                }

                if (request.CategoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == request.CategoryId.Value);
                }

                var filteredProducts = query.ToList();

                var productDtos = new List<ProductDto>();
                foreach (var product in filteredProducts)
                {
                    var category = categories.FirstOrDefault(c => c.Id == product.CategoryId);
                    
                    productDtos.Add(new ProductDto
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
                    });
                }

                var totalCount = productDtos.Count;
                var skip = (request.Page - 1) * request.PageSize;
                var paginatedProducts = productDtos
                    .Skip(skip)
                    .Take(request.PageSize)
                    .ToList();

                var result = new PaginatedResult<ProductDto>
                {
                    Data = paginatedProducts,
                    Metadata = new PaginationMetadata(request.Page, request.PageSize, totalCount)
                };

                try
                {
                    await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30));
                    _logger.LogInformation("Filtered products cached successfully - Count: {Count}, Page: {Page}, PageSize: {PageSize}", 
                        paginatedProducts.Count, request.Page, request.PageSize);
                }
                catch (Exception ex)
                {
                    _logger.LogCacheError(ex, "Set", cacheKey);
                }

                _logger.LogInformation("Filtered products retrieved successfully - TotalCount: {TotalCount}, Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}", 
                    totalCount, request.Page, request.PageSize, request.SearchTerm);

                return Result<PaginatedResult<ProductDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve filtered products - SearchTerm: {SearchTerm}, Page: {Page}, PageSize: {PageSize}", 
                    request.SearchTerm, request.Page, request.PageSize);
                return Result<PaginatedResult<ProductDto>>.Failure($"An error occurred while retrieving products: {ex.Message}");
            }
        }
    }
} 