using MediatR;
using EcomMMS.Application.Common;
using EcomMMS.Application.DTOs;
using EcomMMS.Domain.Interfaces;

namespace EcomMMS.Application.Features.Categories.Queries.GetAllCategories
{
    public class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, Result<List<CategoryDto>>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ICacheService _cacheService;
        private readonly ICacheKeyGenerator _cacheKeyGenerator;

        public GetAllCategoriesQueryHandler(
            ICategoryRepository categoryRepository,
            ICacheService cacheService,
            ICacheKeyGenerator cacheKeyGenerator)
        {
            _categoryRepository = categoryRepository;
            _cacheService = cacheService;
            _cacheKeyGenerator = cacheKeyGenerator;
        }

        public async Task<Result<List<CategoryDto>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = _cacheKeyGenerator.GenerateAllCategoriesKey();

                var cachedResult = await _cacheService.GetAsync<List<CategoryDto>>(cacheKey);
                List<CategoryDto> allCategories;

                if (cachedResult != null)
                {
                    allCategories = cachedResult;
                }
                else
                {
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
                }

                var paginatedCategories = ApplyPagination(allCategories, request.Page, request.PageSize);
                return Result<List<CategoryDto>>.Success(paginatedCategories);
            }
            catch (Exception ex)
            {
                return Result<List<CategoryDto>>.Failure($"An error occurred while retrieving categories: {ex.Message}");
            }
        }

        private static List<CategoryDto> ApplyPagination(List<CategoryDto> categories, int page, int pageSize)
        {
            var skip = (page - 1) * pageSize;
            return categories
                .Skip(skip)
                .Take(pageSize)
                .ToList();
        }
    }
} 