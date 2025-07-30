using MediatR;
using EcomMMS.Application.Common;
using EcomMMS.Application.DTOs;

namespace EcomMMS.Application.Features.Categories.Queries.GetAllCategories
{
    public class GetAllCategoriesQuery : IRequest<Result<List<CategoryDto>>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
} 