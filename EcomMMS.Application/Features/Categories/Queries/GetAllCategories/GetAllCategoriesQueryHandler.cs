using MediatR;
using EcomMMS.Application.Common;
using EcomMMS.Application.DTOs;
using EcomMMS.Domain.Interfaces;

namespace EcomMMS.Application.Features.Categories.Queries.GetAllCategories
{
    public class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, Result<List<CategoryDto>>>
    {
        private readonly ICategoryRepository _categoryRepository;

        public GetAllCategoriesQueryHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Result<List<CategoryDto>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var categories = await _categoryRepository.GetAllAsync();

                var categoryDtos = categories.Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    MinimumStockQuantity = c.MinimumStockQuantity,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                }).ToList();

                var skip = (request.Page - 1) * request.PageSize;
                var paginatedCategories = categoryDtos
                    .Skip(skip)
                    .Take(request.PageSize)
                    .ToList();

                return Result<List<CategoryDto>>.Success(paginatedCategories);
            }
            catch (Exception)
            {
                return Result<List<CategoryDto>>.Failure("An error occurred while retrieving categories.");
            }
        }
    }
} 