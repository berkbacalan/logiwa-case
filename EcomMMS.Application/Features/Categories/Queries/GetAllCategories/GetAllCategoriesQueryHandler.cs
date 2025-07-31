using MediatR;
using EcomMMS.Application.Common;
using EcomMMS.Application.DTOs;
using EcomMMS.Domain.Interfaces;

namespace EcomMMS.Application.Features.Categories.Queries.GetAllCategories
{
    public class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, Result<PaginatedResult<CategoryDto>>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ICacheService _cacheService;
        private readonly ICacheKeyGenerator _cacheKeyGenerator;
        private readonly IApplicationLogger _logger;

        public GetAllCategoriesQueryHandler(
            ICategoryRepository categoryRepository,
            ICacheService cacheService,
            ICacheKeyGenerator cacheKeyGenerator,
            IApplicationLogger logger)
        {
            _categoryRepository = categoryRepository;
            _cacheService = cacheService;
            _cacheKeyGenerator = cacheKeyGenerator;
            _logger = logger;
        }

        public async Task<Result<PaginatedResult<CategoryDto>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = _cacheKeyGenerator.GenerateAllCategoriesKey();

                var cachedResult = await _cacheService.GetAsync<List<CategoryDto>>(cacheKey);
                List<CategoryDto> allCategories;

                if (cachedResult != null)
                {
                    _logger.LogInformation("Cache hit for categories. Count: {Count}", cachedResult.Count);
                    allCategories = cachedResult;
                }
                else
                {
                    _logger.LogInformation("Cache miss for categories. Fetching from database");
                    var categories = await _categoryRepository.GetAllAsync();

                    allCategories = categories.Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        MinimumStockQuantity = c.MinimumStockQuantity,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt
                    }).ToList();

                    await _cacheService.SetAsync(cacheKey, allCategories, TimeSpan.FromHours(2));
                    _logger.LogInformation("Categories cached successfully. Count: {Count}", allCategories.Count);
                }

                var totalCount = allCategories.Count;
                var skip = (request.Page - 1) * request.PageSize;
                var paginatedCategories = allCategories
                    .Skip(skip)
                    .Take(request.PageSize)
                    .ToList();

                var result = new PaginatedResult<CategoryDto>
                {
                    Data = paginatedCategories,
                    Metadata = new PaginationMetadata(request.Page, request.PageSize, totalCount)
                };

                _logger.LogInformation("Categories retrieved successfully. Total: {Total}, Page: {Page}, PageSize: {PageSize}, Returned: {Returned}", 
                    totalCount, request.Page, request.PageSize, paginatedCategories.Count);
                
                return Result<PaginatedResult<CategoryDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving categories");
                return Result<PaginatedResult<CategoryDto>>.Failure($"An error occurred while retrieving categories: {ex.Message}");
            }
        }
    }
} 